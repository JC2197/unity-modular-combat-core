using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GearItemUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image gearIconImage;
    private GearSlotUI parentSlot;
    private Canvas canvas;
    
    // Current gear data
    private ItemInstance currentGear;
    private UIElement _uiElement;
    
    /// <summary>
    /// True when this slot shows a ghost sprite for a 2-handed weapon.
    /// Ghost items cannot be dragged or unequipped independently.
    /// </summary>
    public bool isGhost { get; private set; } = false;
    
    private void Awake()
    {
        if (gearIconImage == null)
            gearIconImage = GetComponent<Image>();
            
        // Start with icon disabled until gear is equipped
        if (gearIconImage != null)
        {
            gearIconImage.enabled = false;
            gearIconImage.raycastTarget = true;
        }
            
        parentSlot = GetComponentInParent<GearSlotUI>();
        canvas = GetComponentInParent<Canvas>();
        _uiElement = gameObject.AddComponent<UIElement>();
    }
    
    /// <summary>
    /// Initializes the gear item display
    /// </summary>
    public void Initialize(ItemInstance gear)
    {
        currentGear = gear;
        
        if (gearIconImage != null && gear != null)
        {
            // Get sprite based on item type
            Sprite gearSprite = GetGearSprite(gear.itemType);
            if (gearSprite != null)
            {
                gearIconImage.sprite = gearSprite;
                gearIconImage.color = Color.white;
                gearIconImage.enabled = true;
            }
            
            // Update parent slot visuals
            if (parentSlot != null)
            {
                parentSlot.SetHasGear(true);
                
                // Set outline color based on rarity tier using ItemConfig system
                ItemConfig itemConfig = GetItemConfig(gear.itemType);
                if (itemConfig != null)
                {
                    Color rarityColor = itemConfig.GetRarityColor(gear.rarityTier);
                    parentSlot.SetOutlineColor(rarityColor);
                }
                else
                {
                    parentSlot.SetOutlineColor(Color.white);
                }
            }
        }

        if (_uiElement != null)
        {
            _uiElement.OnTooltipShow = () => ItemTooltipHelper.Show(currentGear);
            _uiElement.OnTooltipHide = ItemTooltipHelper.HideAll;
        }
    }

    /// <summary>
    /// Display this slot as a ghost of the given item (used for 2-handed weapons).
    /// Shows the weapon sprite at 0.6 alpha and blocks dragging/unequipping.
    /// </summary>
    public void InitializeAsGhost(ItemInstance gear)
    {
        currentGear = gear;
        isGhost = true;

        if (gearIconImage != null && gear != null)
        {
            Sprite gearSprite = GetGearSprite(gear.itemType);
            if (gearSprite != null)
            {
                gearIconImage.sprite = gearSprite;
                gearIconImage.color = new Color(1f, 1f, 1f, 0.6f);
                gearIconImage.enabled = true;
            }

            if (parentSlot != null)
            {
                parentSlot.SetHasGear(true);
            }
        }
    }
    
    /// <summary>
    /// Clears the gear display
    /// </summary>
    public void Clear()
    {
        currentGear = null;
        isGhost = false;
        
        if (gearIconImage != null)
        {
            gearIconImage.sprite = null;
            gearIconImage.enabled = false;
            gearIconImage.color = Color.white;
        }
        
        if (parentSlot != null)
        {
            parentSlot.SetHasGear(false);
        }
    }
    
    #region IPointerHandler Implementation

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentGear != null)
        {
            // Right-click to unequip
            UnequipGear();
        }
    }

    #endregion

    #region Drag Handler Implementation

    private GameObject dragVisual;
    private RectTransform dragImageRect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentGear == null) return;

        // Ghost items (2H offhand display) cannot be dragged
        if (isGhost) return;

        GlobalTooltipCanvas.IsDragging = true;

        // Create drag visual
        dragVisual = new GameObject("DragVisual");
        dragVisual.transform.SetParent(canvas.transform, false);
        dragVisual.transform.SetAsLastSibling();

        Image dragImage = dragVisual.AddComponent<Image>();
        dragImage.sprite = gearIconImage.sprite;
        dragImage.raycastTarget = false;
        dragImage.color = new Color(1, 1, 1, 1f);
        dragImage.canvas.sortingOrder = 47;
        dragImage.preserveAspect = true;
        dragImageRect = dragVisual.GetComponent<RectTransform>();
        dragImageRect.sizeDelta = ((RectTransform)transform).sizeDelta;
        
        // Position at cursor immediately
        UpdateDragVisualPosition(eventData.position);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            UpdateDragVisualPosition(eventData.position);
        }
    }
    
    /// <summary>
    /// Update drag visual position based on screen coordinates
    /// </summary>
    private void UpdateDragVisualPosition(Vector2 screenPosition)
    {
        if (dragVisual == null || canvas == null || dragImageRect == null) return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out localPoint);
            dragImageRect.anchoredPosition = localPoint;
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;
            
            Vector2 localPoint;
            bool result = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvas.worldCamera,
                out localPoint);
            if (result)
            {
                dragImageRect.anchoredPosition = localPoint;
            }
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        GlobalTooltipCanvas.IsDragging = false;

        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
            dragImageRect = null;
        }
        
        if (currentGear == null) return;
        
        // Check if we're over an inventory slot
        InventorySlotUI targetInventorySlot = GetTargetInventorySlot(eventData);
        if (targetInventorySlot != null)
        {
            // Move gear to inventory
            MoveToInventorySlot(targetInventorySlot);
            return;
        }
        
        // Check if we're over another gear slot (for swapping)
        GearSlotUI targetGearSlot = GetTargetGearSlot(eventData);
        if (targetGearSlot != null && targetGearSlot != parentSlot)
        {
            // TODO: Implement gear slot swapping if needed
            Debug.Log($"[GearItemUI] Dropped on another gear slot: {targetGearSlot.SlotType}");
            return;
        }
        
        // Check if dropped outside UI (to world)
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            transform as RectTransform,
            eventData.position,
            canvas.worldCamera))
        {
            UnequipGear();
        }
    }
    
    /// <summary>
    /// Get the inventory slot that the pointer is currently over
    /// </summary>
    private InventorySlotUI GetTargetInventorySlot(UnityEngine.EventSystems.PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Check the GameObject itself
            InventorySlotUI slot = result.gameObject.GetComponent<InventorySlotUI>();
            if (slot == null)
            {
                // Check parent (in case we hit a child Image)
                slot = result.gameObject.GetComponentInParent<InventorySlotUI>();
            }
            
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Get the gear slot that the pointer is currently over
    /// </summary>
    private GearSlotUI GetTargetGearSlot(UnityEngine.EventSystems.PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GearSlotUI slot = result.gameObject.GetComponent<GearSlotUI>();
            if (slot == null)
            {
                slot = result.gameObject.GetComponentInParent<GearSlotUI>();
            }
            
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Move gear item to an inventory slot
    /// </summary>
    private void MoveToInventorySlot(InventorySlotUI targetSlot)
    {
        Debug.Log($"[GearItemUI] Moving {currentGear.displayName} from {parentSlot.SlotType} to inventory");
        
        // Get player's character data
        PlayerController localPlayer = PlayerController.GetLocalPlayer();
        GameObject player = localPlayer != null ? localPlayer.gameObject : null;
        if (player == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot move to inventory - player not found!");
            return;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot move to inventory - PlayerController not found!");
            return;
        }

        CharacterData characterData = playerController.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot move to inventory - CharacterData not found!");
            return;
        }
        
        // Get target slot index
        int targetIndex = GetSlotIndex(targetSlot);
        if (targetIndex < 0)
        {
            Debug.LogError("[GearItemUI] Invalid target slot index!");
            return;
        }
        
        // Check if target slot is occupied
        ItemInstance existingItem = characterData.GetItemAtSlot(targetIndex);
        if (existingItem != null)
        {
            Debug.LogWarning($"[GearItemUI] Target slot {targetIndex} is occupied by {existingItem.displayName}");
            // TODO: Implement swap logic if needed
            return;
        }
        
        // Add item to inventory
        characterData.SetItemAtSlot(targetIndex, currentGear);
        
        // Unequip visual gear from player
        UnequipVisualGear(player, parentSlot.SlotType, currentGear);
        
        // Remove from equipped gear
        if (parentSlot != null)
        {
            characterData.equippedGear.Remove(parentSlot.SlotType);
            
            // Notify CharacterGearManager to remove stat modifiers
            CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
            if (gearManager != null)
            {
                gearManager.OnGearUnequipped(parentSlot.SlotType);
                Debug.Log($"[GearItemUI] Notified CharacterGearManager: gear unequipped from {parentSlot.SlotType}");
            }
            
            // Save character data
            CharacterPersistence.SaveCharacter(characterData);
            Debug.Log($"[GearItemUI] Saved character data with unequipped gear from slot: {parentSlot.SlotType}");
        }
        
        // Update inventory UI
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshInventory(characterData);
        }
        
        // Clear this gear slot
        Clear();
        
        Debug.Log($"[GearItemUI] Successfully moved {currentGear.displayName} to inventory slot {targetIndex}");
    }
    
    /// <summary>
    /// Unequip visual gear from the player character
    /// </summary>
    private void UnequipVisualGear(GameObject player, GearSlot slotType, ItemInstance gearItem)
    {
        PlayerGearManager gearManager = player.GetComponent<PlayerGearManager>();
        if (gearManager == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot unequip visual gear - PlayerGearManager not found!");
            return;
        }
        
        // For armor, we need to re-equip the starter gear from the class
        PlayerController playerController = player.GetComponent<PlayerController>();
        CharacterData characterData = playerController?.GetCurrentCharacterData();
        
        if (characterData?.classData != null)
        {
            // Re-equip starter gear for the slot being unequipped
            switch (slotType)
            {
                case GearSlot.Head:
                    if (characterData.classData.startingHeadPrefab != null)
                    {
                        gearManager.EquipHead(characterData.classData.startingHeadPrefab, characterData.classData.startingHeadConfig);
                        Debug.Log("[GearItemUI] Re-equipped starting head gear");
                    }
                    break;
                    
                case GearSlot.Chest:
                    if (characterData.classData.startingChestPrefab != null)
                    {
                        gearManager.EquipChest(characterData.classData.startingChestPrefab, characterData.classData.startingChestConfig);
                        Debug.Log("[GearItemUI] Re-equipped starting chest gear");
                    }
                    break;
                    
                case GearSlot.Feet:
                    if (characterData.classData.startingFeetPrefab != null)
                    {
                        gearManager.EquipLegs(characterData.classData.startingFeetPrefab, characterData.classData.startingFeetConfig);
                        Debug.Log("[GearItemUI] Re-equipped starting leg gear");
                    }
                    break;
                case GearSlot.Hands:
                    if (characterData.classData.startingHandsPrefab != null)
                    {
                        gearManager.EquipHands(characterData.classData.startingHandsPrefab, characterData.classData.startingHandsConfig);
                        Debug.Log("[GearItemUI] Re-equipped starting hand gear");
                    }
                    break;
                case GearSlot.Weapon:
                    // For weapons, set back to the class default weapon via the network-aware path
                    if (characterData.classData.availableWeapons != null && characterData.classData.availableWeapons.Length > 0)
                    {
                        WeaponConfig defaultWeapon = characterData.classData.availableWeapons[0];

                        // Route through PlayerController so FishNet spawns the weapon properly
                        playerController.NetworkEquipWeapon(defaultWeapon.weaponName);
                        Debug.Log("[GearItemUI] Re-equipped starting weapon via network path");
                    }

                    // If the weapon being unequipped is 2-handed, also clear the offhand ghost
                    if (characterData.mainHandWeaponConfig != null && characterData.mainHandWeaponConfig.is2Handed)
                    {
                        GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
                        if (gearPanel != null)
                        {
                            GearSlotUI ohSlot = gearPanel.GetSlot(GearSlot.OffHandWeapon);
                            if (ohSlot != null)
                            {
                                GearItemUI ohUI = ohSlot.GetComponentInChildren<GearItemUI>(true);
                                if (ohUI != null) ohUI.Clear();
                            }
                        }
                        characterData.equippedGear.Remove(GearSlot.OffHandWeapon);
                        Debug.Log("[GearItemUI] Cleared 2H offhand ghost on weapon unequip");
                    }
                    break;

                case GearSlot.OffHandWeapon:
                    // Unequip offhand weapon visual and clear dual-wield data
                    OffHandWeaponHolder offHandHolder = playerController.GetComponent<OffHandWeaponHolder>();
                    if (offHandHolder != null)
                    {
                        offHandHolder.UnequipWeapon();
                    }
                    characterData.hasDualWeapons = false;
                    characterData.offHandWeaponConfig = null;
                    
                    // Re-equip mainhand so SpawnWeaponPairOnServer drops the offhand
                    if (characterData.mainHandWeaponConfig != null)
                    {
                        playerController.NetworkEquipWeapon(characterData.mainHandWeaponConfig.weaponName);
                    }
                    Debug.Log("[GearItemUI] Unequipped offhand weapon");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Get slot index from InventorySlotUI
    /// </summary>
    private int GetSlotIndex(InventorySlotUI slot)
    {
        // Try to parse from name (assumes slots are named "Slot_0", "Slot_1", etc.)
        string slotName = slot.gameObject.name;
        if (slotName.Contains("_"))
        {
            string[] parts = slotName.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[1], out int index))
            {
                return index;
            }
        }
        
        // Fallback: get sibling index
        return slot.transform.GetSiblingIndex();
    }
    
    #endregion
    
    /// <summary>
    /// Unequips the gear from this slot
    /// </summary>
    private void UnequipGear()
    {
        if (currentGear == null || parentSlot == null) return;
        if (isGhost) return;

        PlayerController localPlayer = PlayerController.GetLocalPlayer();
        GameObject player = localPlayer != null ? localPlayer.gameObject : null;
        if (player == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot unequip gear - player not found!");
            return;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        CharacterData characterData = playerController?.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[GearItemUI] Cannot unequip gear - CharacterData not found!");
            return;
        }

        int emptySlot = characterData.FindEmptySlot();
        if (emptySlot < 0)
        {
            Debug.LogWarning("[GearItemUI] Cannot unequip - inventory is full!");
            return;
        }

        ItemInstance itemToReturn = currentGear;
        GearSlot slotType = parentSlot.SlotType;

        // Add to inventory
        characterData.SetItemAtSlot(emptySlot, itemToReturn);

        // Unequip visual gear
        UnequipVisualGear(player, slotType, itemToReturn);

        // Remove from equipped gear and notify gear manager
        characterData.equippedGear.Remove(slotType);
        CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
        if (gearManager != null)
            gearManager.OnGearUnequipped(slotType);

        // Save and refresh UI
        CharacterPersistence.SaveCharacter(characterData);
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
            inventoryUI.RefreshInventory(characterData);

        Clear();
        Debug.Log($"[GearItemUI] Unequipped {itemToReturn.displayName} from {slotType}, returned to inventory slot {emptySlot}");
    }
    
    /// <summary>
    /// Get the gear sprite based on item type
    /// </summary>
    private Sprite GetGearSprite(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "weapon":
                // Get weapon config name from item data
                if (currentGear != null && !string.IsNullOrEmpty(currentGear.additionalData))
                {
                    WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(currentGear.additionalData);
                    if (weaponData != null && !string.IsNullOrEmpty(weaponData.weaponConfigName))
                    {
                        WeaponItemDropsConfig weaponConfig = WeaponItemDropsConfig.DefaultInstance;
                        if (weaponConfig != null)
                        {
                            Sprite sprite = weaponConfig.GetInventorySpriteForWeapon(weaponData.weaponConfigName);
                            if (sprite != null)
                                return sprite;
                        }
                    }
                }
                // Fallback
                return WeaponItemDropsConfig.DefaultInstance?.inventorySprite;
            
            case "armor":
                // Get armor config name from item data
                if (currentGear != null && !string.IsNullOrEmpty(currentGear.additionalData))
                {
                    ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(currentGear.additionalData);
                    if (armorData != null && !string.IsNullOrEmpty(armorData.armorConfigName))
                    {
                        // Look up armor config directly by name using registry
                        ArmorConfig armorConfig = ArmorConfigRegistry.GetConfig(armorData.armorConfigName);
                        if (armorConfig != null && armorConfig.inventorySprite != null)
                        {
                            return armorConfig.inventorySprite;
                        }
                        else if (armorConfig == null)
                        {
                            Debug.LogWarning($"[GearItemUI] ArmorConfig not found in registry: {armorData.armorConfigName}");
                        }
                    }
                }
                // Fallback
                return ArmorItemDropsConfig.DefaultInstance?.inventorySprite;
            default:
                Debug.LogWarning($"[GearItemUI] Unknown gear type: {itemType}");
                return null;
        }
    }
    
    /// <summary>
    /// Gets the appropriate ItemConfig for the given item type
    /// </summary>
    private ItemConfig GetItemConfig(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "weapon":
                return WeaponItemDropsConfig.DefaultInstance;
            case "armor":
                return ArmorItemDropsConfig.DefaultInstance;
            case "mapkey":
                return MapKeyConfig.Instance;
            default:
                Debug.LogWarning($"[GearItemUI] No ItemConfig found for: {itemType}");
                return null;
        }
    }
}
