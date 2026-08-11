using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using FishNet;

/// <summary>
/// UI representation of an item in the inventory - displays sprite and uses slot border for rarity color.
/// Similar to WorldItem but for UI display. Supports drag and drop to world.
/// Network-spawns dropped items so they're visible to all players in multiplayer.
/// </summary>
public class InventoryItemUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;

    private ItemInstance itemInstance;
    private RectTransform rectTransform;
    private InventorySlotUI parentSlot;  // Reference to parent slot for border coloring

    // Drag and drop
    private GameObject dragVisual;
    private Canvas dragCanvas;
    private RectTransform dragImageRect;
    private bool isDragging = false;
    // When true, the drag represents a single item split from a larger stack (Alt+drag).
    private bool _isDraggingSingle = false;
    private UIElement _uiElement;
    
    // Double-click detection
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    // Proximity radius (screen pixels) used as fallback when pointer lands in a gap
    private const float DROP_PROXIMITY_RADIUS = 60f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();


        // Get parent slot reference
        parentSlot = GetComponentInParent<InventorySlotUI>();
        if (parentSlot == null)
        {
            Debug.LogWarning("[InventoryItemUI] Could not find parent InventorySlotUI!");
        }

        // If itemIcon isn't assigned, try to find it
        if (itemIcon == null)
        {
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
            Debug.Log($"[InventoryItemUI] Found ItemIcon: {itemIcon != null}");
        }

        // Ensure we have an Image component for raycasting (or add one)
        Image mainImage = GetComponent<Image>();
        if (mainImage == null)
        {
            mainImage = gameObject.AddComponent<Image>();
            mainImage.color = new Color(0, 0, 0, 0); // Transparent
        }

        // Ensure raycast target is enabled
        if (mainImage != null && !mainImage.raycastTarget)
        {
            mainImage.raycastTarget = true;
        }

        _uiElement = gameObject.AddComponent<UIElement>();
    }

    /// <summary>
    /// Initialize this UI item with an ItemInstance
    /// </summary>
    public void Initialize(ItemInstance item)
    {
        itemInstance = item;

        // Notify parent slot that it has an item
        if (parentSlot != null)
        {
            parentSlot.SetHasItem(true);
            parentSlot.SetStackCount(item != null ? item.stackSize : 0);
        }

        if (_uiElement != null)
        {
            _uiElement.OnTooltipShow = () => ItemTooltipHelper.Show(itemInstance);
            _uiElement.OnTooltipHide = ItemTooltipHelper.HideAll;
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Update the visual display based on item data
    /// </summary>
    private void UpdateVisuals()
    {
        if (itemInstance == null)
        {
            Debug.LogWarning("[InventoryItemUI] UpdateVisuals called but itemInstance is null!");
            return;
        }

        // Set item sprite
        if (itemIcon != null)
        {
            Sprite inventorySprite = GetInventorySprite(itemInstance.itemType);
            if (inventorySprite != null)
            {
                itemIcon.sprite = inventorySprite;
                itemIcon.enabled = true;
                itemIcon.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[InventoryItemUI] No inventory sprite found for: {itemInstance.itemType}");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryItemUI] itemIcon is null in UpdateVisuals!");
        }

        // Set rarity color on parent slot's border
        if (parentSlot != null)
        {
            ItemConfig itemConfig = GetItemConfig(itemInstance.itemType);
            if (itemConfig != null)
            {
                Color rarityColor = itemConfig.GetRarityColor(itemInstance.rarityTier);
                parentSlot.SetBorderColor(rarityColor);
            }
            else
            {
                parentSlot.SetBorderColor(Color.white);
            }
        }
    }

    /// <summary>
    /// Get the inventory sprite for a specific item type
    /// </summary>
    private Sprite GetInventorySprite(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance?.inventorySprite;

            case "material":
                return MaterialItemConfig.Resolve(itemInstance)?.inventorySprite;

            case "weapon":
                // Get weapon config name from item data
                if (itemInstance != null && !string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(itemInstance.additionalData);
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
                if (itemInstance != null && !string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(itemInstance.additionalData);
                    if (armorData != null && !string.IsNullOrEmpty(armorData.armorConfigName))
                    {
                        // Look up armor config directly by name using registry
                        ArmorConfig armorConfig = ArmorConfigRegistry.GetConfig(armorData.armorConfigName);
                        if (armorConfig != null && armorConfig.inventorySprite != null)
                        {
                            return armorConfig.inventorySprite;
                        }
                    }
                }
                // Fallback
                return ArmorItemDropsConfig.DefaultInstance?.inventorySprite;

            case "craftingorb":
                if (itemInstance != null && !string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    CraftingOrbData orbData = JsonUtility.FromJson<CraftingOrbData>(itemInstance.additionalData);
                    if (orbData != null && !string.IsNullOrEmpty(orbData.configAssetName))
                    {
                        OrbItemConfig orbConfig = Resources.Load<OrbItemConfig>($"CraftingOrbs/{orbData.configAssetName}");
                        if (orbConfig != null && orbConfig.inventorySprite != null)
                            return orbConfig.inventorySprite;
                    }
                }
                return null;

            case "craftingtool":
                return ToolItemConfig.Resolve(itemInstance)?.inventorySprite;

            default:
                Debug.LogWarning($"[InventoryItemUI] Unknown item type: {itemType}");
                return null;
        }
    }

    /// <summary>
    /// Get the ItemConfig for a specific item type
    /// </summary>
    private ItemConfig GetItemConfig(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance;

            case "material":
                return MaterialItemConfig.Resolve(itemInstance);

            case "craftingtool":
                return ToolItemConfig.Resolve(itemInstance);

            case "weapon":
                return WeaponItemDropsConfig.DefaultInstance;

            case "armor":
                return ArmorItemDropsConfig.DefaultInstance;

            case "craftingorb":
                if (itemInstance != null && !string.IsNullOrEmpty(itemInstance.additionalData))
                {
                    CraftingOrbData orbData = JsonUtility.FromJson<CraftingOrbData>(itemInstance.additionalData);
                    if (orbData != null && !string.IsNullOrEmpty(orbData.configAssetName))
                        return Resources.Load<OrbItemConfig>($"CraftingOrbs/{orbData.configAssetName}");
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Mouse click - item interaction (double-click to auto-equip)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemInstance == null) return;

        // Detect double-click
        float timeSinceLastClick = Time.unscaledTime - lastClickTime;
        lastClickTime = Time.unscaledTime;
        
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // Double-click detected - auto-equip to appropriate slot
            AutoEquipItem();
            return;
        }

        // Ctrl+click — deposit to account storage (only when storage panel is open)
        if (StorageUI.IsOpen && eventData.button == PointerEventData.InputButton.Left)
        {
            bool isCtrlHeld = UnityEngine.InputSystem.Keyboard.current != null &&
                              (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed ||
                               UnityEngine.InputSystem.Keyboard.current.rightCtrlKey.isPressed);
            Debug.Log($"[InventoryItemUI] Click while storage open — ctrlHeld={isCtrlHeld}, parentSlot={parentSlot != null}");
            if (isCtrlHeld)
            {
                if (parentSlot == null)
                {
                    Debug.LogWarning("[InventoryItemUI] Ctrl+click deposit failed: parentSlot is null");
                }
                else
                {
                    int slotIndex = parentSlot.SlotIndex;
                    Debug.Log($"[InventoryItemUI] Ctrl+click: depositing '{itemInstance.displayName}' from slot {slotIndex}");
                    StorageUI.Instance.TryDepositFromInventory(itemInstance, slotIndex);
                }
            }
        }
    }

    /// <summary>
    /// Begin dragging the item
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInstance == null) return;

        isDragging = true;
        GlobalTooltipCanvas.IsDragging = true;

        // Alt+drag: split a single item from the stack.
        bool altHeld = UnityEngine.InputSystem.Keyboard.current != null &&
                       UnityEngine.InputSystem.Keyboard.current.altKey.isPressed;
        _isDraggingSingle = altHeld && itemInstance.IsStackable() && itemInstance.stackSize > 1;

        // Hide tooltip during drag
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.HideTooltip();

        // Create drag visual
        CreateDragVisual();
        UpdateDragVisualPosition(eventData.position);
    }

    /// <summary>
    /// Update drag visual position
    /// </summary>
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
        if (dragVisual == null || dragCanvas == null || dragImageRect == null) return;

        if (dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransform canvasRect = dragCanvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;
            Vector2 localPoint;
            bool result = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out localPoint);
            if (result)
                dragImageRect.anchoredPosition = localPoint;
            else
                dragImageRect.position = screenPosition;
        }
        else if (dragCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            RectTransform canvasRect = dragCanvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                return;
            }
            Vector2 localPoint;
            bool result = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                dragCanvas.worldCamera,
                out localPoint);
            if (result)
            {
                dragImageRect.anchoredPosition = localPoint;
            }
            else
            {
                dragImageRect.position = screenPosition;
            }
        }
        else
        {
            dragImageRect.position = screenPosition;
        }
    }

    /// <summary>
    /// End drag - drop item into world or move to another slot
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        GlobalTooltipCanvas.IsDragging = false;

        // Clean up drag visual
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        if (itemInstance == null) return;

        // First check if we're over a gear slot
        GearSlotUI targetGearSlot = GetTargetGearSlot(eventData);
        if (targetGearSlot != null)
        {
            // Try to equip to gear slot
            EquipToGearSlot(targetGearSlot);
            return;
        }

        // Check if we're dragging onto a crafting bench orb slot
        if (CraftingBenchUI.IsOpen && itemInstance.itemType?.ToLower() == "craftingorb")
        {
            CraftingOrbSlot orbSlot = GetTargetCraftingOrbSlot(eventData);
            if (orbSlot != null)
            {
                PlaceOrbInCraftingSlot();
                return;
            }
        }

        // Check if we're dropping onto the storage panel (deposit to storage)
        if (StorageUI.IsOpen)
        {
            InventorySlotUI storageSlot = GetTargetStorageSlot(eventData);
            if (storageSlot != null)
            {
                int srcSlot = parentSlot != null ? parentSlot.SlotIndex : -1;
                Debug.Log($"[InventoryItemUI] Drag-deposit: '{itemInstance.displayName}' from inventory slot {srcSlot} onto storage slot {storageSlot.SlotIndex}");
                if (srcSlot >= 0)
                    StorageUI.Instance.TryDepositFromInventory(itemInstance, srcSlot);
                else
                    Debug.LogWarning("[InventoryItemUI] Drag-deposit: parentSlot is null, cannot resolve inventory slot index");
                return;
            }
        }

        // Then check if we're over another inventory slot
        InventorySlotUI targetSlot = GetTargetSlot(eventData);
        if (targetSlot != null)
        {
            if (_isDraggingSingle)
                SplitOneToSlot(targetSlot);
            else
                MoveToSlot(targetSlot);
        }
        // Then check if we're dropping outside the inventory UI
        else if (IsDroppedOutsideInventory(eventData))
        {
            DropItemToWorld(eventData.position);
        }
        else
        {
            Debug.Log("[InventoryItemUI] Dropped inside inventory but not on a slot - cancelled");
        }

        _isDraggingSingle = false;
    }

    /// <summary>
    /// Get the inventory slot that the pointer is currently over
    /// </summary>
    private InventorySlotUI GetTargetSlot(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            InventorySlotUI slot = result.gameObject.GetComponent<InventorySlotUI>();
            if (slot == null)
                slot = result.gameObject.GetComponentInParent<InventorySlotUI>();

            if (slot != null && slot.transform != transform.parent)
                return slot;
        }

        // Proximity fallback: catches drops that land in the gaps between slot cells
        InventorySlotUI[] allSlots = Object.FindObjectsByType<InventorySlotUI>(FindObjectsSortMode.None);
        InventorySlotUI best = null;
        float bestDist = DROP_PROXIMITY_RADIUS;
        foreach (InventorySlotUI candidate in allSlots)
        {
            if (candidate.transform == transform.parent) continue;
            if (StorageUI.Instance != null && candidate.transform.IsChildOf(StorageUI.Instance.transform)) continue;
            RectTransform rt = candidate.GetComponent<RectTransform>();
            if (rt == null) continue;
            Canvas c = rt.GetComponentInParent<Canvas>();
            if (c == null) continue;
            Camera cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
            float dist = Vector2.Distance(eventData.position, RectTransformUtility.WorldToScreenPoint(cam, rt.position));
            if (dist < bestDist) { bestDist = dist; best = candidate; }
        }
        return best;
    }

    /// <summary>
    /// Returns the storage InventorySlotUI under the pointer, or null if none.
    /// Storage slots are children of the StorageUI panel, so we distinguish them
    /// by checking whether they live under the StorageUI instance's transform.
    /// </summary>
    private InventorySlotUI GetTargetStorageSlot(PointerEventData eventData)
    {
        if (StorageUI.Instance == null) return null;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"[InventoryItemUI] GetTargetStorageSlot: checking {results.Count} raycast hit(s)");

        foreach (var result in results)
        {
            Debug.Log($"[InventoryItemUI]   hit: {result.gameObject.name} (parent: {result.gameObject.transform.parent?.name})");

            InventorySlotUI slot = result.gameObject.GetComponent<InventorySlotUI>();
            if (slot == null)
                slot = result.gameObject.GetComponentInParent<InventorySlotUI>();

            if (slot != null)
            {
                // Verify it lives under the StorageUI hierarchy — not the regular inventory
                bool isStorageSlot = slot.transform.IsChildOf(StorageUI.Instance.transform);
                Debug.Log($"[InventoryItemUI]   found InventorySlotUI on '{slot.gameObject.name}', isStorageSlot={isStorageSlot}");
                if (isStorageSlot)
                    return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the gear slot that the pointer is currently over
    /// </summary>
    private GearSlotUI GetTargetGearSlot(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GearSlotUI gearSlot = result.gameObject.GetComponent<GearSlotUI>();
            if (gearSlot == null)
                gearSlot = result.gameObject.GetComponentInParent<GearSlotUI>();

            if (gearSlot != null)
                return gearSlot;
        }

        // Proximity fallback
        GearSlotUI[] allGear = Object.FindObjectsByType<GearSlotUI>(FindObjectsSortMode.None);
        GearSlotUI bestGear = null;
        float bestDist = DROP_PROXIMITY_RADIUS;
        foreach (GearSlotUI candidate in allGear)
        {
            RectTransform rt = candidate.GetComponent<RectTransform>();
            if (rt == null) continue;
            Canvas c = rt.GetComponentInParent<Canvas>();
            if (c == null) continue;
            Camera cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
            float dist = Vector2.Distance(eventData.position, RectTransformUtility.WorldToScreenPoint(cam, rt.position));
            if (dist < bestDist) { bestDist = dist; bestGear = candidate; }
        }
        return bestGear;
    }

    /// <summary>
    /// Equip item to a gear slot
    /// </summary>
    private void EquipToGearSlot(GearSlotUI targetGearSlot)
    {
        // Store the item we're equipping (important: do this before any data changes)
        ItemInstance itemToEquip = itemInstance;

        // Validate item type matches slot type
        if (!CanEquipToSlot(itemToEquip, targetGearSlot.SlotType))
        {
            return;
        }

        // Get player and inventory references up front (multiplayer-safe: always the local owner)
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null)
        {
            Debug.LogError("[InventoryItemUI] Cannot equip - PlayerController not found!");
            return;
        }
        
        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogError("[InventoryItemUI] Cannot equip - CharacterData not found!");
            return;
        }

        // Get the GearItemUI component in the slot
        GearItemUI gearItemUI = targetGearSlot.GetComponentInChildren<GearItemUI>();
        if (gearItemUI == null)
        {
            Debug.LogError($"[InventoryItemUI] No GearItemUI found in {targetGearSlot.SlotType} slot!");
            return;
        }

        // Check if there's already an item equipped in this slot
        ItemInstance currentEquippedItem = GetEquippedItemInSlot(targetGearSlot.SlotType);
        bool isSwapping = false;
        int sourceIndex = -1;
        
        if (currentEquippedItem != null)
        {
            // SWAP: Directly swap the equipped item into this inventory slot
            Debug.Log($"[InventoryItemUI] Swapping {currentEquippedItem.displayName} with {itemToEquip.displayName}");
            isSwapping = true;
            
            // Get the source slot index (where the new item is coming from)
            InventorySlotUI sourceSlot = transform.parent.GetComponent<InventorySlotUI>();
            if (sourceSlot == null)
            {
                Debug.LogError("[InventoryItemUI] Cannot swap - source slot not found!");
                return;
            }
            
            sourceIndex = GetSlotIndex(sourceSlot);
            if (sourceIndex < 0)
            {
                Debug.LogError($"[InventoryItemUI] Invalid source slot index: {sourceIndex}");
                return;
            }
            
            // Unequip the visual gear from player FIRST
            UnequipVisualGearForSwap(player.gameObject, targetGearSlot.SlotType, currentEquippedItem);
            
            // Now swap in inventory data: put the old equipped item into the inventory slot
            characterData.SetItemAtSlot(sourceIndex, currentEquippedItem);
        }

        // Initialize the gear item UI with the new item
        gearItemUI.Initialize(itemToEquip);

        // For weapon slot, actually equip the weapon on the player
        if (targetGearSlot.SlotType == GearSlot.Weapon && itemToEquip.itemType.ToLower() == "weapon")
        {
            EquipWeaponOnPlayer(itemToEquip);

            // If this weapon is 2-handed, clear any existing offhand and show ghost
            WeaponConfig equippedConfig = ResolveWeaponConfig(itemToEquip);
            if (equippedConfig != null && equippedConfig.is2Handed)
            {
                // Unequip real offhand weapon if one exists
                UnequipExistingOffhand(player, characterData);

                PopulateOffhandGhost(itemToEquip);
            }
        }
        // For offhand weapon slot, equip as offhand
        else if (targetGearSlot.SlotType == GearSlot.OffHandWeapon && itemToEquip.itemType.ToLower() == "weapon")
        {
            // Handle 2-handed main weapon conflicts
            if (characterData.mainHandWeaponConfig != null && characterData.mainHandWeaponConfig.is2Handed)
            {
                WeaponConfig offhandConfig = ResolveWeaponConfig(itemToEquip);

                if (offhandConfig != null && offhandConfig.isMainHand && offhandConfig.isOffhand)
                {
                    // Dual-eligible weapon dragged to offhand while 2H is equipped:
                    // return the 2H to inventory and redirect this weapon to the mainhand slot.
                    ItemInstance current2H = characterData.equippedGear.TryGetValue(GearSlot.Weapon, out var h) ? h : null;
                    if (current2H != null)
                    {
                        int slot2H = characterData.FindEmptySlot();
                        if (slot2H >= 0)
                            characterData.SetItemAtSlot(slot2H, current2H);
                        else
                            Debug.LogWarning($"[InventoryItemUI] No empty slot for 2H weapon '{current2H.displayName}' - item lost!");
                    }

                    ClearOffhandGhostIfNeeded();
                    gearItemUI.Clear(); // Remove item from offhand slot UI (was placed there by Initialize above)

                    // Show the item in the mainhand slot UI
                    GearPanelUI gearPanel2H = FindFirstObjectByType<GearPanelUI>();
                    if (gearPanel2H != null)
                    {
                        GearSlotUI weaponSlot2H = gearPanel2H.GetSlot(GearSlot.Weapon);
                        GearItemUI weaponGearUI2H = weaponSlot2H?.GetComponentInChildren<GearItemUI>(true);
                        weaponGearUI2H?.Initialize(itemToEquip);
                    }

                    EquipWeaponOnPlayer(itemToEquip);
                }
                else if (offhandConfig != null && offhandConfig.isOffhand && !offhandConfig.isMainHand)
                {
                    // Offhand-only weapon: unequip the 2H mainhand first, then equip to offhand.
                    UnequipExistingMainhand(player, characterData);
                    EquipOffhandWeaponOnPlayer(itemToEquip);
                }
                else
                {
                    Debug.LogWarning("[InventoryItemUI] Cannot equip offhand - main hand weapon is 2-handed!");
                    gearItemUI.Clear();
                    return;
                }
            }
            else
            {
                EquipOffhandWeaponOnPlayer(itemToEquip);
            }
        }
        // For armor slots, equip the armor on the player
        else if (itemToEquip.itemType.ToLower() == "armor")
        {
            EquipArmorOnPlayer(itemToEquip, targetGearSlot.SlotType);
        }

        // Only remove from inventory if we didn't swap (in swap case, item already replaced)
        if (!isSwapping)
        {
            Debug.Log($"[InventoryItemUI] Removing item from inventory (not a swap)");
            RemoveFromInventory();
        }
        else
        {
            Debug.Log($"[InventoryItemUI] Skipping inventory removal (swap scenario - item already replaced in inventory)");
        }
        
        // Save and refresh at the END after all equipping is complete
        CharacterPersistence.SaveCharacter(characterData);

        // Broadcast the updated gear loadout to all connected clients
        player.NotifyGearChanged();

        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshInventory(characterData);
        }

        Debug.Log($"[InventoryItemUI] Successfully equipped {itemToEquip.displayName} to {targetGearSlot.SlotType} slot");
    }

    /// <summary>
    /// Equip a weapon item on the player character.
    /// Routes through PlayerController.NetworkEquipWeapon() so the weapon prefab is
    /// properly FishNet-Spawned as a NetworkObject visible to all clients.
    /// </summary>
    private void EquipWeaponOnPlayer(ItemInstance weaponItem)
    {
        // Parse weapon data
        if (string.IsNullOrEmpty(weaponItem.additionalData))
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip weapon - no additionalData found!");
            return;
        }

        WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(weaponItem.additionalData);
        if (weaponData == null || string.IsNullOrEmpty(weaponData.weaponConfigName))
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip weapon - invalid WeaponGearData!");
            return;
        }

        // Find the local player (multiplayer-safe)
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null)
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip weapon - PlayerController not found!");
            return;
        }

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip weapon - CharacterData not found!");
            return;
        }

        // Route through the network-aware path on PlayerController.
        // This handles server-Spawn, offhand, ObserversRpc, and gear broadcast.
        player.NetworkEquipWeapon(weaponData.weaponConfigName);

        Debug.Log($"[InventoryItemUI] Requested network weapon equip: '{weaponData.weaponConfigName}'");

        // Save equipped weapon to character data
        characterData.equippedGear[GearSlot.Weapon] = weaponItem;
        
        // Notify CharacterGearManager to apply stat modifiers
        CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
        if (gearManager != null)
        {
            gearManager.OnGearEquipped(GearSlot.Weapon, weaponItem);
            Debug.Log($"[InventoryItemUI] Notified CharacterGearManager: weapon equipped");
        }

        // Save character data
        CharacterPersistence.SaveCharacter(characterData);
        Debug.Log($"[InventoryItemUI] Saved character data with equipped weapon");
    }

    /// <summary>
    /// Equip a weapon into the offhand slot.
    /// Sets characterData.offHandWeaponConfig and spawns via the network path.
    /// </summary>
    private void EquipOffhandWeaponOnPlayer(ItemInstance weaponItem)
    {
        if (string.IsNullOrEmpty(weaponItem.additionalData))
        {
            Debug.LogError("[InventoryItemUI] Cannot equip offhand weapon - no additionalData!");
            return;
        }

        WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(weaponItem.additionalData);
        if (weaponData == null || string.IsNullOrEmpty(weaponData.weaponConfigName))
        {
            Debug.LogError("[InventoryItemUI] Cannot equip offhand weapon - invalid WeaponGearData!");
            return;
        }

        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null) { Debug.LogError("[InventoryItemUI] Cannot equip offhand - no player!"); return; }

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null) { Debug.LogError("[InventoryItemUI] Cannot equip offhand - no CharacterData!"); return; }

        // Resolve the actual WeaponConfig SO
        WeaponConfig weaponConfig = ResolveWeaponConfig(weaponItem);
        if (weaponConfig == null)
        {
            Debug.LogError($"[InventoryItemUI] WeaponConfig '{weaponData.weaponConfigName}' not found!");
            return;
        }

        // Set offhand data on CharacterData so SpawnWeaponPairOnServer sees it
        characterData.hasDualWeapons = true;
        characterData.offHandWeaponConfig = weaponConfig;

        // Re-equip the main-hand weapon via the network path.
        // SpawnWeaponPairOnServer will pick up offHandWeaponConfig and spawn both.
        if (characterData.mainHandWeaponConfig != null)
        {
            player.NetworkEquipWeapon(characterData.mainHandWeaponConfig.weaponName);
        }

        Debug.Log($"[InventoryItemUI] Equipped offhand weapon: '{weaponData.weaponConfigName}'");

        // Persist
        characterData.equippedGear[GearSlot.OffHandWeapon] = weaponItem;

        // Apply stat modifiers
        CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
        if (gearManager != null)
        {
            gearManager.OnGearEquipped(GearSlot.OffHandWeapon, weaponItem);
        }

        CharacterPersistence.SaveCharacter(characterData);
    }

    /// <summary>
    /// Populate the OffHandWeapon gear slot UI with a ghost (alpha 0.6) sprite
    /// when a 2-handed weapon is equipped in the main hand.
    /// </summary>
    private void PopulateOffhandGhost(ItemInstance mainHandItem)
    {
        GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
        if (gearPanel == null) return;

        GearSlotUI offhandSlot = gearPanel.GetSlot(GearSlot.OffHandWeapon);
        if (offhandSlot == null) return;

        GearItemUI offhandGearUI = offhandSlot.GetComponentInChildren<GearItemUI>(true);
        if (offhandGearUI == null) return;

        offhandGearUI.InitializeAsGhost(mainHandItem);
        Debug.Log("[InventoryItemUI] Populated offhand slot with 2H ghost sprite");
    }

    /// <summary>
    /// If the currently equipped main-hand weapon is 2-handed, clear the offhand ghost.
    /// </summary>
    private void ClearOffhandGhostIfNeeded()
    {
        PlayerController player = PlayerController.GetLocalPlayer();
        CharacterData cd = player?.GetCurrentCharacterData();
        if (cd == null) return;

        // Check if the current main-hand weapon is 2-handed
        if (cd.mainHandWeaponConfig != null && cd.mainHandWeaponConfig.is2Handed)
        {
            GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
            if (gearPanel == null) return;

            GearSlotUI offhandSlot = gearPanel.GetSlot(GearSlot.OffHandWeapon);
            if (offhandSlot == null) return;

            GearItemUI offhandGearUI = offhandSlot.GetComponentInChildren<GearItemUI>(true);
            if (offhandGearUI != null)
            {
                offhandGearUI.Clear();
            }

            // Remove ghost from equipped gear data
            cd.equippedGear.Remove(GearSlot.OffHandWeapon);
        }
    }

    /// <summary>
    /// Unequip the mainhand weapon, returning it to inventory if possible.
    /// Called when equipping an offhand-only weapon while a 2-handed weapon is equipped.
    /// </summary>
    private void UnequipExistingMainhand(PlayerController player, CharacterData characterData)
    {
        if (!characterData.equippedGear.TryGetValue(GearSlot.Weapon, out ItemInstance mainhandItem))
            return;
        if (mainhandItem == null) return;

        // Try to return mainhand to inventory
        int emptySlot = characterData.FindEmptySlot();
        if (emptySlot >= 0)
        {
            characterData.SetItemAtSlot(emptySlot, mainhandItem);
            Debug.Log($"[InventoryItemUI] Returned mainhand '{mainhandItem.displayName}' to inventory slot {emptySlot}");
        }
        else
        {
            Debug.LogWarning($"[InventoryItemUI] No empty inventory slot for mainhand '{mainhandItem.displayName}' - item lost!");
        }

        // Clear mainhand data and any ghost offhand entry
        characterData.equippedGear.Remove(GearSlot.Weapon);
        characterData.equippedGear.Remove(GearSlot.OffHandWeapon);
        characterData.mainHandWeaponConfig = null;
        characterData.hasDualWeapons = false;
        characterData.offHandWeaponConfig = null;

        // Remove mainhand stat modifiers
        CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
        if (gearManager != null)
            gearManager.OnGearUnequipped(GearSlot.Weapon);

        // Unequip visual weapon
        WeaponHolder weaponHolder = player.GetComponent<WeaponHolder>();
        if (weaponHolder != null)
            weaponHolder.UnequipWeapon();

        // Clear mainhand slot UI (offhand slot UI is left as-is; the new item was already placed
        // there by gearItemUI.Initialize before entering this branch)
        GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
        if (gearPanel != null)
        {
            GearSlotUI weaponSlot = gearPanel.GetSlot(GearSlot.Weapon);
            GearItemUI weaponGearUI = weaponSlot?.GetComponentInChildren<GearItemUI>(true);
            weaponGearUI?.Clear();
        }
    }

    /// <summary>
    /// Unequip any existing real offhand weapon, returning it to inventory if possible.
    /// Called when equipping a 2-handed weapon that claims the offhand slot.
    /// </summary>
    private void UnequipExistingOffhand(PlayerController player, CharacterData characterData)
    {
        if (!characterData.equippedGear.TryGetValue(GearSlot.OffHandWeapon, out ItemInstance offhandItem))
            return;
        if (offhandItem == null) return;

        // Try to return the offhand item to inventory
        int emptySlot = characterData.FindEmptySlot();
        if (emptySlot >= 0)
        {
            characterData.SetItemAtSlot(emptySlot, offhandItem);
            Debug.Log($"[InventoryItemUI] Returned offhand '{offhandItem.displayName}' to inventory slot {emptySlot}");
        }
        else
        {
            Debug.LogWarning($"[InventoryItemUI] No empty inventory slot for offhand '{offhandItem.displayName}' - item lost!");
        }

        // Clear offhand data
        characterData.equippedGear.Remove(GearSlot.OffHandWeapon);
        characterData.hasDualWeapons = false;
        characterData.offHandWeaponConfig = null;

        // Remove stat modifiers
        CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
        if (gearManager != null)
            gearManager.OnGearUnequipped(GearSlot.OffHandWeapon);

        // Clear offhand slot UI
        GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
        if (gearPanel != null)
        {
            GearSlotUI offhandSlot = gearPanel.GetSlot(GearSlot.OffHandWeapon);
            if (offhandSlot != null)
            {
                GearItemUI offhandGearUI = offhandSlot.GetComponentInChildren<GearItemUI>(true);
                if (offhandGearUI != null) offhandGearUI.Clear();
            }
        }

        // Refresh inventory UI
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
            inventoryUI.RefreshInventory(characterData);
    }

    /// <summary>
    /// Equip an armor item on the player character
    /// </summary>
    private void EquipArmorOnPlayer(ItemInstance armorItem, GearSlot slotType)
    {

        // Parse armor data
        if (string.IsNullOrEmpty(armorItem.additionalData))
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip armor - no additionalData found!");
            return;
        }

        ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(armorItem.additionalData);
        if (armorData == null || string.IsNullOrEmpty(armorData.armorConfigName))
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip armor - invalid ArmorGearData!");
            return;
        }

        // Find the local player (multiplayer-safe)
        PlayerController player = PlayerController.GetLocalPlayer();

        if (player == null)
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip armor - PlayerController not found!");
            return;
        }
        CharacterData characterData = player.GetCurrentCharacterData();
        
        // Look up armor config directly by name using registry
        ArmorConfig armorConfig = ArmorConfigRegistry.GetConfig(armorData.armorConfigName);

        if (armorConfig == null)
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip armor - ArmorConfig '{armorData.armorConfigName}' not found in registry!");
            return;
        }

        // Get the player gear manager
        PlayerGearManager gearManager = player.GetComponent<PlayerGearManager>();
        if (gearManager == null)
        {
            Debug.LogError($"[InventoryItemUI] Cannot equip armor - PlayerGearManager not found!");
            return;
        }

        // Equip the armor based on slot type
        switch (slotType)
        {
            case GearSlot.Head:
                if (armorConfig.headGearPrefab != null)
                {
                    gearManager.EquipHead(armorConfig.headGearPrefab, armorConfig);
                    Debug.Log($"[InventoryItemUI] Equipped head armor '{armorConfig.gearName}' on player");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemUI] Head armor '{armorConfig.gearName}' has no prefab!");
                }
                break;

            case GearSlot.Chest:
                if (armorConfig.chestGearPrefab != null)
                {
                    gearManager.EquipChest(armorConfig.chestGearPrefab, armorConfig);
                    Debug.Log($"[InventoryItemUI] Equipped chest armor '{armorConfig.gearName}' on player");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemUI] Chest armor '{armorConfig.gearName}' has no prefab!");
                }
                break;

            case GearSlot.Hands:
                if (armorConfig.handsGearPrefab != null)
                {
                    // Equip hands gear properly using EquipHands method
                    gearManager.EquipHands(armorConfig.handsGearPrefab, armorConfig);
                    Debug.Log($"[InventoryItemUI] Equipped hands gear '{armorConfig.gearName}' on player");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemUI] Hands armor '{armorConfig.gearName}' has no prefab!");
                }
                break;

            case GearSlot.Feet:
                if (armorConfig.legGearPrefab != null)
                {
                    gearManager.EquipLegs(armorConfig.legGearPrefab, armorConfig);
                    Debug.Log($"[InventoryItemUI] Equipped leg armor '{armorConfig.gearName}' on player");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemUI] Leg armor '{armorConfig.gearName}' has no prefab!");
                }
                break;

            case GearSlot.Backpack:
                if (armorConfig.backpackGearPrefab != null)
                {
                    gearManager.EquipBackpack(armorConfig.backpackGearPrefab, armorConfig);
                    Debug.Log($"[InventoryItemUI] Equipped backpack '{armorConfig.gearName}' on player");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemUI] Backpack '{armorConfig.gearName}' has no prefab!");
                }
                break;

            default:
                Debug.LogWarning($"[InventoryItemUI] Unsupported armor slot type: {slotType}");
                break;
        }

        // Save equipped armor to character data (reuse player variable from above)
        characterData = player.GetCurrentCharacterData();
        if (characterData != null)
        {
            characterData.equippedGear[slotType] = armorItem;
            
            // Notify CharacterGearManager to apply stat modifiers
            CharacterGearManager characterGearManager = player.GetComponent<CharacterGearManager>();
            if (characterGearManager != null)
            {
                characterGearManager.OnGearEquipped(slotType, armorItem);
                Debug.Log($"[InventoryItemUI] Notified CharacterGearManager: armor equipped to {slotType}");
            }

            // Save character data
            CharacterPersistence.SaveCharacter(characterData);
            Debug.Log($"[InventoryItemUI] Saved character data with equipped armor in slot: {slotType}");
        }
    }

    /// <summary>
    /// Auto-equip item to its appropriate gear slot (called on double-click)
    /// </summary>
    private void AutoEquipItem()
    {
        GearSlotUI appropriateSlot = FindAppropriateGearSlot();
        if (appropriateSlot != null)
        {
            EquipToGearSlot(appropriateSlot);
            // After equipping the inventory refreshes and UIElement references are
            // invalidated. Force GlobalTooltipCanvas to re-check what is under the
            // pointer so the tooltip reflects the new state.
            GlobalTooltipCanvas.Invalidate();
        }
        else
        {
            Debug.LogWarning($"[InventoryItemUI] Cannot auto-equip {itemInstance.displayName} - no appropriate gear slot found");
        }
    }
    
    /// <summary>
    /// Find the appropriate GearSlotUI for this item based on its type
    /// </summary>
    private GearSlotUI FindAppropriateGearSlot()
    {
        GearSlotUI[] allGearSlots = FindObjectsByType<GearSlotUI>(FindObjectsSortMode.None);
        
        // For weapons, choose slot based on WeaponConfig flags
        if (itemInstance.itemType.ToLower() == "weapon")
        {
            WeaponConfig weaponConfig = ResolveWeaponConfig(itemInstance);

            // Collect candidate slots
            GearSlotUI weaponSlot = null;
            GearSlotUI offhandSlot = null;
            foreach (var slot in allGearSlots)
            {
                if (slot.SlotType == GearSlot.Weapon) weaponSlot = slot;
                if (slot.SlotType == GearSlot.OffHandWeapon) offhandSlot = slot;
            }

            if (weaponConfig != null)
            {
                // Offhand-only weapons always go to offhand
                if (weaponConfig.isOffhand && !weaponConfig.isMainHand)
                    return offhandSlot;

                // Mainhand-only or 2H always go to weapon
                if (!weaponConfig.isOffhand || weaponConfig.is2Handed)
                    return weaponSlot;

                // Dual-eligible (isMainHand && isOffhand): prefer weapon, but if
                // weapon is already occupied try offhand
                PlayerController player = PlayerController.GetLocalPlayer();
                CharacterData cd = player?.GetCurrentCharacterData();
                if (cd != null && cd.equippedGear.ContainsKey(GearSlot.Weapon) && offhandSlot != null)
                {
                    // If offhand is also full, fall back to weapon (swap)
                    if (!cd.equippedGear.ContainsKey(GearSlot.OffHandWeapon))
                        return offhandSlot;
                }
                return weaponSlot;
            }

            // Fallback: weapon slot
            return weaponSlot;
        }
        // For armor, find the matching armor slot
        else if (itemInstance.itemType.ToLower() == "armor")
        {
            if (!string.IsNullOrEmpty(itemInstance.additionalData))
            {
                ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(itemInstance.additionalData);
                if (armorData != null)
                {
                    foreach (var slot in allGearSlots)
                    {
                        if (slot.SlotType == armorData.armorSlotType)
                        {
                            return slot;
                        }
                    }
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Check if an item can be equipped to a specific gear slot
    /// </summary>
    private bool CanEquipToSlot(ItemInstance item, GearSlot slotType)
    {
        // Weapons — delegate to WeaponConfig.CanEquipToSlot()
        if (item.itemType.ToLower() == "weapon")
        {
            WeaponConfig weaponConfig = ResolveWeaponConfig(item);
            if (weaponConfig != null)
                return weaponConfig.CanEquipToSlot(slotType);

            // Fallback: if we can't resolve the config, allow Weapon slot only
            return slotType == GearSlot.Weapon;
        }

        // Armor items
        if (item.itemType.ToLower() == "armor")
        {
            // Parse armor data to check slot type
            if (!string.IsNullOrEmpty(item.additionalData))
            {
                ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(item.additionalData);
                if (armorData != null && armorData.armorSlotType == slotType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Resolve a WeaponConfig ScriptableObject from an ItemInstance.
    /// </summary>
    private WeaponConfig ResolveWeaponConfig(ItemInstance weaponItem)
    {
        if (string.IsNullOrEmpty(weaponItem.additionalData)) return null;
        WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(weaponItem.additionalData);
        if (weaponData == null || string.IsNullOrEmpty(weaponData.weaponConfigName)) return null;
        WeaponItemDropsConfig WeaponItemDropsConfig = WeaponItemDropsConfig.DefaultInstance;
        return WeaponItemDropsConfig?.GetWeaponConfigByName(weaponData.weaponConfigName);
    }

    /// <summary>
    /// Move this item to another inventory slot
    /// </summary>
    private void MoveToSlot(InventorySlotUI targetSlot)
    {
        Debug.Log($"[InventoryItemUI] Moving {itemInstance.displayName} to slot {targetSlot.name}");

        // Get the source slot index (current slot)
        InventorySlotUI sourceSlot = transform.parent.GetComponent<InventorySlotUI>();
        if (sourceSlot == null)
        {
            Debug.LogError("[InventoryItemUI] Cannot move item - source slot not found!");
            return;
        }

        int sourceIndex = GetSlotIndex(sourceSlot);
        int targetIndex = GetSlotIndex(targetSlot);

        if (sourceIndex < 0 || targetIndex < 0)
        {
            Debug.LogError($"[InventoryItemUI] Invalid slot indices: source={sourceIndex}, target={targetIndex}");
            return;
        }

        Debug.Log($"[InventoryItemUI] Moving from slot {sourceIndex} to slot {targetIndex}");

        // Get player's character data
        PlayerController playerController = PlayerController.GetLocalPlayer();
        if (playerController == null)
        {
            Debug.LogWarning("[InventoryItemUI] Cannot move item - player not found!");
            return;
        }

        CharacterData characterData = playerController.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[InventoryItemUI] Cannot move item - CharacterData not found!");
            return;
        }

        // Get items at source and target
        ItemInstance sourceItem = characterData.GetItemAtSlot(sourceIndex);
        ItemInstance targetItem = characterData.GetItemAtSlot(targetIndex);

        if (sourceItem == null)
        {
            Debug.LogError("[InventoryItemUI] Source slot is empty!");
            return;
        }

        bool changed = ItemSlotStackingUtility.MoveOrMergeItem(characterData.inventorySlots, sourceIndex, targetIndex);
        if (!changed)
        {
            Debug.Log("[InventoryItemUI] Move/merge cancelled");
            return;
        }

        // Save and refresh
        CharacterPersistence.SaveCharacter(characterData);
        InventoryManager.RefreshInventoryDisplay();
    }

    /// <summary>
    /// Alt+drag: split exactly one item from this stack and place it in <paramref name="targetSlot"/>.
    /// If the target already holds a matching stackable item the count is incremented there;
    /// otherwise the single item occupies the empty slot.
    /// </summary>
    private void SplitOneToSlot(InventorySlotUI targetSlot)
    {
        InventorySlotUI sourceSlot = transform.parent.GetComponent<InventorySlotUI>();
        if (sourceSlot == null) return;

        int sourceIndex = GetSlotIndex(sourceSlot);
        int targetIndex = GetSlotIndex(targetSlot);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        PlayerController playerController = PlayerController.GetLocalPlayer();
        CharacterData characterData = playerController?.GetCurrentCharacterData();
        if (characterData == null) return;

        ItemInstance sourceItem = characterData.GetItemAtSlot(sourceIndex);
        ItemInstance targetItem = characterData.GetItemAtSlot(targetIndex);

        if (sourceItem == null || sourceItem.stackSize <= 1) return;

        if (!ItemSlotStackingUtility.SplitOneToSlot(characterData.inventorySlots, sourceIndex, targetIndex))
            return;

        Debug.Log($"[InventoryItemUI] Split 1x {sourceItem.displayName} from slot {sourceIndex} to slot {targetIndex}");
        CharacterPersistence.SaveCharacter(characterData);
        InventoryManager.RefreshInventoryDisplay();
    }

    /// <summary>
    /// Get the slot index from a slot UI component
    /// </summary>
    private int GetSlotIndex(InventorySlotUI slot)
    {
        // Get the slot's sibling index in its parent
        return slot.transform.GetSiblingIndex();
    }

    /// <summary>
    /// Create visual representation of item being dragged
    /// </summary>
    private void CreateDragVisual()
    {
        // 1. Create the canvas
        dragVisual = new GameObject("DragVisualCanvas");
        dragCanvas = dragVisual.AddComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dragCanvas.sortingOrder = 47;
        dragVisual.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 2. Create the image as a child
        GameObject dragImageObj = new GameObject("DragImage");
        dragImageObj.transform.SetParent(dragVisual.transform, false);
        dragImageRect = dragImageObj.AddComponent<RectTransform>();
        dragImageRect.sizeDelta = new Vector2(64f, 64f);
        dragImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragImageRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image dragImage = dragImageObj.AddComponent<Image>();
        dragImage.sprite = itemIcon != null ? itemIcon.sprite : null;
        dragImage.raycastTarget = false;
        dragImage.color = new Color(1, 1, 1, 1f);
        dragImage.preserveAspect = true;

        // sizeDelta can be 0 on stretch/layout-driven UI; use rendered rect size instead.
        Vector2 dragSize = Vector2.zero;
        if (itemIcon != null)
            dragSize = itemIcon.rectTransform.rect.size;

        if (dragSize.x <= 0f || dragSize.y <= 0f)
            dragSize = rectTransform != null ? rectTransform.rect.size : Vector2.zero;

        if ((dragSize.x <= 0f || dragSize.y <= 0f) && dragImage.sprite != null)
            dragSize = dragImage.sprite.rect.size;

        if (dragSize.x <= 0f || dragSize.y <= 0f)
            dragSize = new Vector2(64f, 64f);

        dragImageRect.sizeDelta = dragSize;

        // 3. Set initial position
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        RectTransform canvasRect = dragCanvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mouseScreenPos,
            null, // null for Overlay
            out localPoint
        );
        if (converted)
            dragImageRect.anchoredPosition = localPoint;
        else
            dragImageRect.position = mouseScreenPos;
    }

    /// <summary>
    /// Check if pointer is outside inventory UI bounds
    /// </summary>
    private bool IsDroppedOutsideInventory(PointerEventData eventData)
    {
        // Find the inventory container
        Canvas inventoryCanvas = GetComponentInParent<Canvas>();
        if (inventoryCanvas == null) return false;

        // Check if pointer is over any UI element
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // If pointer is over inventory UI, don't drop to world
        foreach (var result in results)
        {
            if (result.gameObject.GetComponentInParent<InventoryUI>() != null)
            {
                return false; // Still over inventory UI
            }
        }

        return true; // Outside inventory UI
    }

    /// <summary>
    /// Drop item from inventory to world at mouse position
    /// </summary>
    private void DropItemToWorld(Vector2 screenPosition)
    {
        Debug.Log($"[InventoryItemUI] Dropping {itemInstance.displayName} to world");

        // Get player position and drop at player's feet (Y - 0.5)
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("[InventoryItemUI] Cannot drop item - player not found!");
            return;
        }

        Vector3 worldPosition = player.transform.position;
        worldPosition.y -= 0.5f; // Drop at player's feet
        worldPosition.z = 0f; // Ensure 2D position

        // Use the same prefab-based approach as LootDropper to avoid NetworkObject initialization errors
        GameObject worldItemObj;
        GameObject worldItemPrefab = UniversalDropTable.Instance?.worldItemPrefab;
        
        if (worldItemPrefab != null)
        {
            // Use prefab (already has NetworkObject configured properly)
            worldItemObj = Instantiate(worldItemPrefab, worldPosition, Quaternion.identity);
            Debug.Log($"[InventoryItemUI] Instantiated WorldItem from prefab at {worldPosition}");
        }
        else
        {
            // Fallback: Create manually (for backwards compatibility)
            Debug.LogWarning("[InventoryItemUI] No worldItemPrefab assigned in UniversalDropTable! Using fallback generation.");
            worldItemObj = new GameObject($"WorldItem_{itemInstance.displayName}");
            worldItemObj.transform.position = worldPosition;
            worldItemObj.layer = LayerMask.NameToLayer("Item");

            // Add required SpriteRenderer
            SpriteRenderer spriteRenderer = worldItemObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Item";
            spriteRenderer.sortingOrder = 5;

            // Add required Collider2D (CircleCollider2D)
            CircleCollider2D collider = worldItemObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;
        }

        // Initialize WorldItem component with our item data
        WorldItem worldItem = worldItemObj.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = worldItemObj.AddComponent<WorldItem>();
        }
        worldItem.Initialize(itemInstance);

        // Network spawn the item if server is active
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null && networkManager.IsServerStarted)
        {
            networkManager.ServerManager.Spawn(worldItemObj);
            Debug.Log($"[InventoryItemUI] Network-spawned WorldItem at {worldPosition}");
        }
        else
        {
            Debug.Log($"[InventoryItemUI] Created WorldItem at {worldPosition} (no network spawning - server not active)");
        }

        // Remove from inventory
        RemoveFromInventory();
    }

    /// <summary>
    /// Remove this item from the player's inventory
    /// </summary>
    /// <summary>
    /// Returns the CraftingOrbSlot under the pointer, or null if none.
    /// </summary>
    private CraftingOrbSlot GetTargetCraftingOrbSlot(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            CraftingOrbSlot slot = result.gameObject.GetComponent<CraftingOrbSlot>();
            if (slot == null)
                slot = result.gameObject.GetComponentInParent<CraftingOrbSlot>();
            if (slot != null)
                return slot;
        }

        // Proximity fallback — handles cases where the orb slot container has no raycast-blocking Image
        CraftingOrbSlot[] allOrbs = Object.FindObjectsByType<CraftingOrbSlot>(FindObjectsSortMode.None);
        CraftingOrbSlot bestOrb = null;
        float bestDist = DROP_PROXIMITY_RADIUS;
        foreach (CraftingOrbSlot candidate in allOrbs)
        {
            RectTransform rt = candidate.GetComponent<RectTransform>();
            if (rt == null) continue;
            Canvas c = rt.GetComponentInParent<Canvas>();
            if (c == null) continue;
            Camera cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
            float dist = Vector2.Distance(eventData.position, RectTransformUtility.WorldToScreenPoint(cam, rt.position));
            if (dist < bestDist) { bestDist = dist; bestOrb = candidate; }
        }
        return bestOrb;
    }

    /// <summary>
    /// Removes this orb from inventory and hands it to CraftingBenchUI.
    /// </summary>
    private void PlaceOrbInCraftingSlot()
    {
        if (itemInstance == null || CraftingBenchUI.Instance == null) return;

        InventorySlotUI slot = transform.parent.GetComponent<InventorySlotUI>();
        if (slot == null)
        {
            Debug.LogWarning("[InventoryItemUI] PlaceOrbInCraftingSlot: parent slot not found");
            return;
        }

        int slotIndex = GetSlotIndex(slot);
        PlayerController player = PlayerController.GetLocalPlayer();
        CharacterData characterData = player?.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[InventoryItemUI] PlaceOrbInCraftingSlot: CharacterData not found");
            return;
        }

        // If a previous orb is already in the slot, swap: old orb goes back into
        // the inventory slot that held the new orb, new orb goes into the bench.
        ItemInstance existing = CraftingBenchUI.Instance.CurrentOrb;
        if (existing != null)
        {
            characterData.SetItemAtSlot(slotIndex, existing);
        }
        else
        {
            characterData.RemoveItemFromSlot(slotIndex);
        }

        CraftingBenchUI.Instance.AcceptOrb(itemInstance);
        CharacterPersistence.SaveCharacter(characterData);
        InventoryManager.RefreshInventoryDisplay();
        Debug.Log($"[InventoryItemUI] Placed {itemInstance.displayName} into crafting orb slot");
    }

    private void RemoveFromInventory()
    {
        Debug.Log($"[InventoryItemUI] RemoveFromInventory called for: {(itemInstance != null ? itemInstance.displayName : "NULL")}");

        if (itemInstance == null)
        {
            Debug.LogError("[InventoryItemUI] Cannot remove item - itemInstance is null!");
            return;
        }

        // Get the slot index
        InventorySlotUI slot = transform.parent.GetComponent<InventorySlotUI>();
        if (slot == null)
        {
            Debug.LogError("[InventoryItemUI] Cannot remove item - parent slot not found!");
            return;
        }

        int slotIndex = GetSlotIndex(slot);
        if (slotIndex < 0)
        {
            Debug.LogError($"[InventoryItemUI] Invalid slot index: {slotIndex}");
            return;
        }

        // Find player and their character data
        PlayerController playerController = PlayerController.GetLocalPlayer();
        if (playerController == null)
        {
            Debug.LogWarning("[InventoryItemUI] Cannot remove item - player not found!");
            return;
        }

        CharacterData characterData = playerController.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[InventoryItemUI] Cannot remove item - CharacterData not found!");
            return;
        }

        Debug.Log($"[InventoryItemUI] Attempting to remove from slot {slotIndex}. Current inventory count: {characterData.inventorySlots.Count}");

        // Remove item from inventory slot
        bool removed = characterData.RemoveItemFromSlot(slotIndex);
        if (removed)
        {
            Debug.Log($"[InventoryItemUI] Successfully removed {itemInstance.displayName} from slot {slotIndex}. New count: {characterData.inventorySlots.Count}");

            // Save character data
            CharacterPersistence.SaveCharacter(characterData);
            Debug.Log($"[InventoryItemUI] Character data saved");

            // Refresh inventory display
            InventoryManager.RefreshInventoryDisplay();
            Debug.Log($"[InventoryItemUI] Inventory display refreshed");
        }
        else
        {
            Debug.LogWarning($"[InventoryItemUI] Failed to remove {itemInstance.displayName} from slot {slotIndex}! Slot was empty.");
        }
    }
    
    /// <summary>
    /// Get the currently equipped item in a specific gear slot
    /// </summary>
    private ItemInstance GetEquippedItemInSlot(GearSlot slotType)
    {
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null) return null;

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null) return null;
        
        if (characterData.equippedGear.TryGetValue(slotType, out ItemInstance equippedItem))
        {
            return equippedItem;
        }
        
        return null;
    }
    
    /// <summary>
    /// Unequip visual gear from player when swapping (doesn't re-equip starter gear)
    /// </summary>
    private void UnequipVisualGearForSwap(GameObject player, GearSlot slotType, ItemInstance gearItem)
    {
        PlayerGearManager gearManager = player.GetComponent<PlayerGearManager>();
        if (gearManager == null)
        {
            Debug.LogWarning("[InventoryItemUI] PlayerGearManager not found!");
            return;
        }

        // Remove the old item's stat contributions BEFORE equipping the new one
        CharacterGearManager characterGearManager = player.GetComponent<CharacterGearManager>();
        if (characterGearManager != null)
        {
            characterGearManager.OnGearUnequipped(slotType);
            Debug.Log($"[InventoryItemUI] Removed stat modifiers for {slotType} before swap");
        }

        // Unequip the visual gear piece based on type
        switch (slotType)
        {
            case GearSlot.Head:
                // Head visual will be replaced by new item, no action needed
                Debug.Log("[InventoryItemUI] Preparing to swap head gear");
                break;
                
            case GearSlot.Chest:
                // Chest visual will be replaced by new item, no action needed
                Debug.Log("[InventoryItemUI] Preparing to swap chest gear");
                break;
                
            case GearSlot.Feet:
                // Legs visual will be replaced by new item, no action needed
                Debug.Log("[InventoryItemUI] Preparing to swap leg gear");
                break;
                
            case GearSlot.Weapon:
                WeaponHolder weaponHolder = player.GetComponent<WeaponHolder>();
                if (weaponHolder != null)
                {
                    weaponHolder.UnequipWeapon();
                    Debug.Log("[InventoryItemUI] Unequipped weapon for swap");
                }
                // If the weapon being swapped out is 2-handed, also clear the offhand ghost
                ClearOffhandGhostIfNeeded();
                break;

            case GearSlot.OffHandWeapon:
                OffHandWeaponHolder offHandHolder = player.GetComponent<OffHandWeaponHolder>();
                if (offHandHolder != null)
                {
                    offHandHolder.UnequipWeapon();
                    Debug.Log("[InventoryItemUI] Unequipped offhand weapon for swap");
                }
                break;
        }
    }
}
