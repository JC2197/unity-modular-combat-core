using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// World item that displays an Item ScriptableObject on the ground.
/// Handles visual appearance (sprite + particles) and pickup interaction.
/// Attach this to a prefab with SpriteRenderer and ParticleSystem.
/// Player can press interact key to pick up items.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WorldItem : Interactable
{
    // Static tracking of all active tooltips to prevent overlap
    private static List<WorldItem> activeTooltips = new List<WorldItem>();
    [Header("Item Data")]
    [Tooltip("The item instance this world item represents")]
    [SerializeField] private ItemInstance itemInstance;
    
    // SyncVar to replicate item data to all clients
    private readonly SyncVar<string> _syncItemJson = new SyncVar<string>();
    
    [Header("Components (Auto-assigned)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D itemCollider;
    
    // Particle system - instantiated at runtime
    private ParticleSystem particles;
    
    // Label - instantiated at runtime
    private GameObject itemLabel;
    
    [Header("Pickup Settings")]
    [Tooltip("Minimum distance for automatic pickup")]
    [SerializeField] private float pickupRadius = 3.0f;
    
    [Tooltip("Layer mask for detecting the player")]
    [SerializeField] private LayerMask playerLayer = 1; // Default layer
    
    [Header("Tooltip Settings")]
    [Tooltip("Prefab for hover tooltip (optional)")]
    [SerializeField] private GameObject tooltipPrefab;
    
    private GameObject activeTooltip;
    private GameObject tooltipBackground; // Reference to clickable background
    private bool isPickedUp = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Auto-assign components if not set
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (itemCollider == null)
            itemCollider = GetComponent<Collider2D>();
        
        // Ensure collider is a trigger for interaction system
        if (itemCollider != null)
            itemCollider.isTrigger = true;
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _syncItemJson.OnChange += OnSyncItemJsonChanged;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _syncItemJson.OnChange -= OnSyncItemJsonChanged;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        // When a client first sees this object, apply item data from the SyncVar
        if (!IsServerStarted && !string.IsNullOrEmpty(_syncItemJson.Value))
        {
            itemInstance = JsonUtility.FromJson<ItemInstance>(_syncItemJson.Value);
            UpdateVisuals();
            if (itemInstance != null)
                interactionMessage = $"Pick up {itemInstance.displayName}";
        }
    }
    
    /// <summary>
    /// Called when the item JSON SyncVar changes (e.g., late joiner receives data)
    /// </summary>
    private void OnSyncItemJsonChanged(string prev, string next, bool asServer)
    {
        if (asServer) return; // Server already handled in Initialize()
        if (string.IsNullOrEmpty(next)) return;
        
        itemInstance = JsonUtility.FromJson<ItemInstance>(next);
        UpdateVisuals();
        if (itemInstance != null)
            interactionMessage = $"Pick up {itemInstance.displayName}";
    }
    
    private void Start()
    {
        // Visual setup is handled by Initialize()
        // Only call UpdateVisuals if item was set in inspector but not initialized
        if (itemInstance != null && spriteRenderer != null && spriteRenderer.sprite == null)
        {
            UpdateVisuals();
        }
        else
        {
            Debug.Log("[WorldItem] Start() skipped UpdateVisuals - already initialized");
        }
    }
    
    private void Update()
    {
        if (isPickedUp) return;
        
        // Check for mouse hover to show tooltip
        CheckForHover();
    }
    
    #region Interactable Implementation
    
    /// <summary>
    /// Called when player presses interact key while near this item
    /// </summary>
    public override void OnInteract(GameObject player)
    {
        if (isPickedUp || itemInstance == null) return;
        
        // Attempt to pick up the item
        CharacterData characterData = GetPlayerCharacterData(player);
        
        if (characterData == null)
        {
            Debug.LogWarning("[WorldItem] Could not find CharacterData on player!");
            return;
        }
        
        // Add item to inventory
        bool success = AddToInventory(characterData);
        
        if (success)
        {
            isPickedUp = true;
            
            // Play pickup effect
            StartCoroutine(PickupEffect());
        }
    }
    
    #endregion
    
    /// <summary>
    /// Initialize this world item with an ItemInstance
    /// </summary>
    public void Initialize(ItemInstance item)
    {
        itemInstance = item;
        
        // Sync item data to all clients via SyncVar
        // This works even if called before ServerManager.Spawn() — FishNet includes
        // the initial SyncVar value in the spawn message.
        if (item != null)
            _syncItemJson.Value = JsonUtility.ToJson(item);
        
        UpdateVisuals();
        
        // Set interaction message with item name
        if (itemInstance != null)
        {
            interactionMessage = $"Pick up {itemInstance.displayName}";
        }
    }
    
    /// <summary>
    /// Update sprite and particle effects based on item data
    /// </summary>
    private void UpdateVisuals()
    {
        if (itemInstance == null)
        {
            Debug.LogWarning("[WorldItem] UpdateVisuals called but itemInstance is null!");
            return;
        }
        
        // Scale the item
        transform.localScale = Vector3.one * 0.75f;
        
        // Set sprite based on item type
        if (spriteRenderer != null)
        {
            Sprite worldSprite = GetWorldSprite(itemInstance.itemType);
            if (worldSprite != null)
            {
                spriteRenderer.sprite = worldSprite;
                Debug.Log($"[WorldItem] Set sprite for {itemInstance.itemType}: {worldSprite.name}");
            }
            else
            {
                Debug.LogWarning($"[WorldItem] No sprite found for item type: {itemInstance.itemType}");
            }
        }
        else
        {
            Debug.LogError("[WorldItem] SpriteRenderer is null!");
        }
        
        // Setup particle system
        SetupParticleSystem(itemInstance.itemType, itemInstance.rarityTier);
    }
    
    /// <summary>
    /// Setup particle system - use override if available, otherwise use generic with rarity modifications
    /// </summary>
    private void SetupParticleSystem(string itemType, int rarityTier)
    {
        // Clean up any existing particles first
        if (particles != null)
        {
            Destroy(particles.gameObject);
            particles = null;
        }
        
        // Check for particle system override
        ParticleSystem overrideParticle = GetParticleOverride(itemType);
        
        if (overrideParticle != null)
        {
            Debug.Log($"[WorldItem] Found particle override for {itemType}, using custom particle");
            
            // Use custom particle system without modifications
            particles = Instantiate(overrideParticle, transform);
            particles.transform.localPosition = Vector3.zero;
            
            // Ensure renderer settings are preserved
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var sourceRenderer = overrideParticle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && sourceRenderer != null)
            {
                renderer.material = sourceRenderer.material;
                renderer.sortingLayerName = "Item";
                renderer.sortingOrder = 6;
            }
            
            particles.Play();
            
            Debug.Log($"[WorldItem] Using particle override for {itemType}");
        }
        else
        {
            Debug.Log($"[WorldItem] No particle override for {itemType}, using generic particle from RarityConfig");
            // Use generic particle system with rarity modifications
            ItemConfig itemConfig = GetItemConfig(itemType);
            RarityConfig rarityConfig = RarityConfig.Instance;
            
            if (rarityConfig == null || rarityConfig.genericParticleSystem == null)
            {
                Debug.LogWarning("[WorldItem] No generic particle system configured in RarityConfig!");
                return;
            }
            
            particles = Instantiate(rarityConfig.genericParticleSystem, transform);
            particles.transform.localPosition = Vector3.zero;
            
            // Ensure renderer settings are preserved and use GlowMega material
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var sourceRenderer = rarityConfig.genericParticleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // Try to load GlowMega material
                Material glowMaterial = Resources.Load<Material>("Materials/GlowMega");
                if (glowMaterial == null)
                {
                    glowMaterial = Resources.Load<Material>("GlowMega");
                }
                
                if (glowMaterial != null)
                {
                    renderer.material = glowMaterial;
                }
                else if (sourceRenderer != null)
                {
                    renderer.material = sourceRenderer.material;
                    Debug.LogWarning("[WorldItem] GlowMega material not found in Resources, using prefab material");
                }
                
                renderer.sortingLayerName = "Item";
                renderer.sortingOrder = 6;
            }
            
            // Modify based on rarity (get color/emission from item config)
            if (itemConfig != null)
            {
                var main = particles.main;
                main.startColor = itemConfig.GetRarityColor(rarityTier);
                
                var emission = particles.emission;
                emission.rateOverTime = itemConfig.GetRarityEmission(rarityTier);
            }
            
            particles.Play();
            
            Debug.Log($"[WorldItem] Using generic particle for {itemType} with rarity tier {rarityTier}");
        }
    }
    
    /// <summary>
    /// Get particle system override for specific item type
    /// </summary>
    private ParticleSystem GetParticleOverride(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance?.particleSystemOverride;

            case "material":
                return MaterialItemConfig.Resolve(itemInstance)?.particleSystemOverride;
            
            case "weapon":
                return WeaponItemDropsConfig.DefaultInstance?.particleSystemOverride;
            
            case "armor":
                return ArmorItemDropsConfig.DefaultInstance?.particleSystemOverride;
            
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Get world sprite based on item type
    /// </summary>
    private Sprite GetWorldSprite(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "mapkey":
                MapKeyConfig mapKeyConfig = MapKeyConfig.Instance;
                if (mapKeyConfig == null)
                {
                    Debug.LogError("[WorldItem] MapKeyConfig.Instance is null! Make sure MapKeyConfig.asset exists in Assets/Resources/");
                    return null;
                }
                if (mapKeyConfig.worldSprite == null)
                {
                    Debug.LogWarning("[WorldItem] MapKeyConfig.worldSprite is not assigned in the asset!");
                }
                return mapKeyConfig.worldSprite;

            case "material":
                return MaterialItemConfig.Resolve(itemInstance)?.worldSprite;
            
            case "weapon":
                // Get weapon config name from item data
                if (!string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    Debug.Log($"[WorldItem] Parsing weapon data from additionalData: {itemInstance.additionalData}");
                    WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(itemInstance.additionalData);
                    if (weaponData != null && !string.IsNullOrEmpty(weaponData.weaponConfigName))
                    {
                        Debug.Log($"[WorldItem] Weapon config name: {weaponData.weaponConfigName}");
                        WeaponItemDropsConfig weaponConfig = WeaponItemDropsConfig.DefaultInstance;
                        if (weaponConfig != null)
                        {
                            Sprite sprite = weaponConfig.GetWorldSpriteForWeapon(weaponData.weaponConfigName);
                            if (sprite != null)
                            {
                                Debug.Log($"[WorldItem] Found world sprite for weapon: {sprite.name}");
                                return sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[WorldItem] GetWorldSpriteForWeapon returned null for: {weaponData.weaponConfigName}");
                            }
                        }
                        else
                        {
                            Debug.LogError("[WorldItem] WeaponItemDropsConfig.DefaultInstance is null!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[WorldItem] WeaponGearData parsing failed or weaponConfigName is empty. weaponData null: {weaponData == null}");
                    }
                }
                else
                {
                    Debug.LogWarning("[WorldItem] additionalData is null or empty for weapon!");
                }
                // Fallback
                Debug.LogWarning("[WorldItem] Using fallback sprite for weapon!");
                return WeaponItemDropsConfig.DefaultInstance?.worldSprite;
            
            case "armor":
                // Get armor config name and slot from item data
                if (!string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    Debug.Log($"[WorldItem] Parsing armor data from additionalData: {itemInstance.additionalData}");
                    ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(itemInstance.additionalData);
                    if (armorData != null && !string.IsNullOrEmpty(armorData.armorConfigName))
                    {
                        Debug.Log($"[WorldItem] Armor config name: {armorData.armorConfigName}, slot: {armorData.armorSlotType}");
                        ArmorItemDropsConfig armorConfig = ArmorItemDropsConfig.DefaultInstance;
                        if (armorConfig != null)
                        {
                            Sprite sprite = armorConfig.GetWorldSpriteForArmor(armorData.armorConfigName, armorData.armorSlotType);
                            if (sprite != null)
                            {
                                Debug.Log($"[WorldItem] Found world sprite for armor: {sprite.name}");
                                return sprite;
                            }
                            else
                            {
                                Debug.LogWarning($"[WorldItem] GetWorldSpriteForArmor returned null for: {armorData.armorConfigName}");
                            }
                        }
                        else
                        {
                            Debug.LogError("[WorldItem] ArmorItemDropsConfig.DefaultInstance is null!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[WorldItem] ArmorGearData parsing failed or armorConfigName is empty. armorData null: {armorData == null}");
                    }
                }
                else
                {
                    Debug.LogWarning("[WorldItem] additionalData is null or empty for armor!");
                }
                // Fallback
                Debug.LogWarning("[WorldItem] Using fallback sprite for armor!");
                return ArmorItemDropsConfig.DefaultInstance?.worldSprite;
            
            default:
                Debug.LogWarning($"[WorldItem] Unknown item type: {itemType}");
                return null;
        }
    }
    
    /// <summary>
    /// Get item config for specific item type
    /// </summary>
    private ItemConfig GetItemConfig(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance;

            case "material":
                return MaterialItemConfig.Resolve(itemInstance);
            
            case "weapon":
                return WeaponItemDropsConfig.DefaultInstance;
            
            case "armor":
                return ArmorItemDropsConfig.DefaultInstance;
            
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Check for mouse hover to show tooltip
    /// </summary>
    private void CheckForHover()
    {
        if (Camera.main == null) return;
        
        // Use Input System for mouse position
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        mousePos.z = transform.position.z;
        
        float distance = Vector2.Distance(mousePos, transform.position);
        
        if (distance <= 1f) // Hover radius
        {
            ShowTooltip();
        }
        else
        {
            HideTooltip();
        }
    }
    
    /// <summary>
    /// Show tooltip above item
    /// </summary>
    private void ShowTooltip()
    {
        if (activeTooltip != null || itemInstance == null) return;
        
        // Calculate tooltip position with overlap prevention
        Vector3 tooltipPosition = CalculateTooltipPosition();
        
        // Create tooltip container
        activeTooltip = new GameObject("ItemTooltip");
        activeTooltip.transform.position = tooltipPosition;
        activeTooltip.transform.SetParent(transform);
        activeTooltip.transform.localScale = Vector3.one * 3f; // Counter-scale for readability
        
        // Add TextMesh
        var textMesh = activeTooltip.AddComponent<TextMesh>();
        textMesh.text = itemInstance.displayName;
        textMesh.fontSize = 20;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Set color based on rarity
        ItemConfig itemConfig = GetItemConfig(itemInstance.itemType);
        if (itemConfig != null)
        {
            textMesh.color = itemConfig.GetRarityColor(itemInstance.rarityTier);
        }
        else
        {
            textMesh.color = Color.white;
        }
        
        // Set text renderer sorting
        var renderer = activeTooltip.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Item";
            renderer.sortingOrder = 100;
        }
        
        // Add translucent background using SpriteRenderer instead of Quad for better 2D transparency
        GameObject background = new GameObject("Background");
        background.transform.SetParent(activeTooltip.transform);
        background.transform.localPosition = new Vector3(0f, 0f, 0.1f); // Behind text
        
        // Store reference and add collider for clicking
        tooltipBackground = background;
        
        // Create a simple 1x1 white sprite
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite backgroundSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        
        // Add SpriteRenderer
        SpriteRenderer bgSpriteRenderer = background.AddComponent<SpriteRenderer>();
        bgSpriteRenderer.sprite = backgroundSprite;
        bgSpriteRenderer.color = new Color(0f, 0f, 0f, 0.6f); // Semi-transparent black
        bgSpriteRenderer.sortingLayerName = "Item";
        bgSpriteRenderer.sortingOrder = 99; // Behind text
        
        // Add 2D BoxCollider for clicking
        BoxCollider2D boxCollider = background.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = Vector2.one;
        
        // Calculate background size based on text bounds (stretch to fit)
        Bounds textBounds = renderer.bounds;
        float padding = 0.1f; // Add some padding
        float worldToLocalScale = 1f / activeTooltip.transform.lossyScale.x;
        background.transform.localScale = new Vector3(
            (textBounds.size.x + padding) * worldToLocalScale,
            (textBounds.size.y + padding) * worldToLocalScale,
            1f
        );
        
        // Register this tooltip as active
        if (!activeTooltips.Contains(this))
        {
            activeTooltips.Add(this);
        }
    }
    
    /// <summary>
    /// Calculate tooltip position with overlap prevention
    /// </summary>
    private Vector3 CalculateTooltipPosition()
    {
        Vector3 basePosition = transform.position + Vector3.up * 0.5f;
        float minDistance = 0.6f; // Minimum distance between tooltips
        float verticalOffset = 0.35f; // How much to offset vertically per collision
        
        // Check against all active tooltips
        int attempts = 0;
        int maxAttempts = 10;
        Vector3 testPosition = basePosition;
        
        while (attempts < maxAttempts)
        {
            bool hasOverlap = false;
            
            foreach (WorldItem other in activeTooltips)
            {
                if (other == this || other == null || other.activeTooltip == null)
                    continue;
                
                float distance = Vector3.Distance(testPosition, other.activeTooltip.transform.position);
                
                if (distance < minDistance)
                {
                    hasOverlap = true;
                    break;
                }
            }
            
            if (!hasOverlap)
            {
                return testPosition;
            }
            
            // Try offsetting upward
            attempts++;
            testPosition = basePosition + Vector3.up * (verticalOffset * attempts);
        }
        
        // If still overlapping after max attempts, use last calculated position
        return testPosition;
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    private void HideTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip);
            activeTooltip = null;
            tooltipBackground = null;
            
            // Unregister from active tooltips
            activeTooltips.Remove(this);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up when WorldItem is destroyed
        HideTooltip();
    }
    
    
    /// <summary>
    /// Check if a position is within pickup range
    /// </summary>
    public bool IsInPickupRange(Vector3 position)
    {
        return Vector2.Distance(position, transform.position) <= pickupRadius;
    }
    
    /// <summary>
    /// Get the pickup radius for this item
    /// </summary>
    public float GetPickupRadius()
    {
        return pickupRadius;
    }
    
    
    
    /// <summary>
    /// Add item to character's inventory
    /// </summary>
    private bool AddToInventory(CharacterData character)
    {
        // Use new slot-based inventory system
        bool success = character.AddItemToInventory(itemInstance);
        
        if (success)
        {
            Debug.Log($"[WorldItem] Added {itemInstance.displayName} to inventory");
            
            // Show pickup notification
            ItemPickupHUD.ShowPickup(itemInstance.displayName, ItemSpriteResolver.Resolve(itemInstance));
            
            // Save character data
            CharacterPersistence.SaveCharacter(character);
            
            // Refresh inventory display if it's open
            InventoryManager.RefreshInventoryDisplay();
            
            return true;
        }
        else
        {
            Debug.LogWarning($"[WorldItem] Failed to add {itemInstance.displayName} - inventory full!");
            return false;
        }
    }
    
    /// <summary>
    /// Get CharacterData from player GameObject
    /// </summary>
    private CharacterData GetPlayerCharacterData(GameObject player)
    {
        // Get PlayerController component and retrieve CharacterData
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            return playerController.GetCurrentCharacterData();
        }
        
        Debug.LogWarning("[WorldItem] Could not find PlayerController on player GameObject!");
        return null;
    }
    
    /// <summary>
    /// Visual and audio effect when picking up item
    /// </summary>
    private IEnumerator PickupEffect()
    {
        // Disable collider to prevent double pickup
        if (itemCollider != null)
            itemCollider.enabled = false;
        
        // Play pickup animation (scale up and fade out)
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Scale up
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            // Fade out sprite
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - t;
                spriteRenderer.color = color;
            }
            
            // Move up slightly
            transform.position += Vector3.up * Time.deltaTime * 2f;
            
            yield return null;
        }
        
        // Destroy the world item — use Despawn for networked objects
        if (IsSpawned && IsServerStarted)
            Despawn();
        else
            Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize pickup radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
