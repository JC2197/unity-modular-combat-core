using UnityEngine;
using System.Collections.Generic;

public class GearPanelUI : MonoBehaviour
{
    [Header("Gear Slot References")]
    [SerializeField] private GearSlotUI headSlot;
    [SerializeField] private GearSlotUI chestSlot;
    [SerializeField] private GearSlotUI feetSlot;
    [SerializeField] private GearSlotUI handsSlot;
    [SerializeField] private GearSlotUI trinket1Slot;
    [SerializeField] private GearSlotUI trinket2Slot;
    [SerializeField] private GearSlotUI trinket3Slot;
    [SerializeField] private GearSlotUI trinket4Slot;
    [SerializeField] private GearSlotUI weaponSlot;
    [SerializeField] private GearSlotUI offHandWeaponSlot;
    [SerializeField] private GearSlotUI backpackSlot;

    private Dictionary<GearSlot, GearSlotUI> slotMap;

    private void Awake()
    {
        InitializeSlotMap();
    }

    private void Start()
    {
        // Load equipped gear from player's CharacterData
        LoadEquippedGearFromPlayer();
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    /// <summary>
    /// When the LOCAL player spawns (or reconnects), refresh gear display
    /// </summary>
    private void HandlePlayerSpawned(PlayerController player)
    {
        if (player.IsOwner)
        {
            Debug.Log("[GearPanelUI] Local player spawned — refreshing gear display");
            LoadEquippedGearFromPlayer();
        }
    }

    /// <summary>
    /// Load and display equipped gear from the player's CharacterData
    /// </summary>
    private void LoadEquippedGearFromPlayer()
    {
        Debug.Log("[GearPanelUI] ========== LoadEquippedGearFromPlayer START ==========");

        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("[GearPanelUI] No local PlayerController found to load gear from");
            return;
        }

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null || characterData.equippedGear == null)
        {
            Debug.LogWarning("[GearPanelUI] No CharacterData or equipped gear found");
            return;
        }

        Debug.Log($"[GearPanelUI] Found {characterData.equippedGear.Count} equipped items in CharacterData");

        // Create a copy of the dictionary to avoid modification during iteration
        var equippedGearCopy = new Dictionary<GearSlot, ItemInstance>(characterData.equippedGear);

        // Display each equipped item in the UI (no order dependency)
        foreach (var kvp in equippedGearCopy)
        {
            GearSlot slot = kvp.Key;
            ItemInstance item = kvp.Value;

            if (item != null)
            {
                Debug.Log($"[GearPanelUI] Loading gear for slot {slot}: {item.displayName} (instanceID: {item.instanceID})");

                // Use DisplayGearOnly to avoid re-triggering save/stat recalculation during load
                DisplayGearOnly(slot, item);
            }
            else
            {
                Debug.LogWarning($"[GearPanelUI] Null item found in slot {slot}");
            }
        }

        // After loading all slots, reconstruct offhand ghost if main-hand is 2-handed
        // (ghosts are visual-only and not persisted in equippedGear)
        if (characterData.mainHandWeaponConfig != null && characterData.mainHandWeaponConfig.is2Handed)
        {
            if (!characterData.equippedGear.ContainsKey(GearSlot.OffHandWeapon))
            {
                // Find the main-hand item to create a ghost from
                if (characterData.equippedGear.TryGetValue(GearSlot.Weapon, out ItemInstance mainItem) && mainItem != null)
                {
                    GearSlotUI offSlot = GetSlot(GearSlot.OffHandWeapon);
                    if (offSlot != null)
                    {
                        GearItemUI offGearUI = offSlot.GetComponentInChildren<GearItemUI>(true);
                        if (offGearUI != null)
                        {
                            offGearUI.InitializeAsGhost(mainItem);
                            Debug.Log("[GearPanelUI] Reconstructed 2H ghost in offhand slot");
                        }
                    }
                }
            }
        }

        Debug.Log("[GearPanelUI] ========== LoadEquippedGearFromPlayer END ==========");
    }

    /// <summary>
    /// Display gear in UI only, without triggering save or stat recalculation
    /// Used during initial load to avoid modifying data while iterating
    /// </summary>
    private void DisplayGearOnly(GearSlot slotType, ItemInstance gear)
    {
        GearSlotUI slot = GetSlot(slotType);
        if (slot == null)
        {
            Debug.LogWarning($"[GearPanelUI] No slot found for gear type: {slotType}");
            return;
        }

        // Find the GearItemUI component (should be a child of the slot)
        GearItemUI gearItemUI = slot.GetComponentInChildren<GearItemUI>(true);
        if (gearItemUI != null)
        {
            gearItemUI.Initialize(gear);
            Debug.Log($"[GearPanelUI] Displayed gear in UI: {gear.displayName} -> {slotType}");
        }
        else
        {
            Debug.LogWarning($"[GearPanelUI] No GearItemUI found for slot: {slotType}");
        }
    }

    private void InitializeSlotMap()
    {
        slotMap = new Dictionary<GearSlot, GearSlotUI>();

        Debug.Log("[GearPanelUI] ========== INITIALIZING SLOT MAP ==========");
        if (headSlot != null) { slotMap[GearSlot.Head] = headSlot; Debug.Log("[GearPanelUI] Head slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] Head slot: NOT ASSIGNED IN INSPECTOR"); }
        if (chestSlot != null) { slotMap[GearSlot.Chest] = chestSlot; Debug.Log("[GearPanelUI] Chest slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] Chest slot: NOT ASSIGNED IN INSPECTOR"); }
        if (feetSlot != null) { slotMap[GearSlot.Feet] = feetSlot; Debug.Log("[GearPanelUI] Feet slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] Feet slot: NOT ASSIGNED IN INSPECTOR"); }
        if (handsSlot != null) { slotMap[GearSlot.Hands] = handsSlot; Debug.Log("[GearPanelUI] Hands slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] Hands slot: NOT ASSIGNED IN INSPECTOR"); }
        if (trinket1Slot != null) { slotMap[GearSlot.Trinket] = trinket1Slot; Debug.Log("[GearPanelUI] Trinket1 slot: ASSIGNED"); }
        if (trinket2Slot != null) { slotMap[GearSlot.Trinket] = trinket2Slot; Debug.Log("[GearPanelUI] Trinket2 slot: ASSIGNED"); }
        if (trinket3Slot != null) { slotMap[GearSlot.Trinket] = trinket3Slot; Debug.Log("[GearPanelUI] Trinket3 slot: ASSIGNED"); }
        if (trinket4Slot != null) { slotMap[GearSlot.Trinket] = trinket4Slot; Debug.Log("[GearPanelUI] Trinket4 slot: ASSIGNED"); }
        if (weaponSlot != null) { slotMap[GearSlot.Weapon] = weaponSlot; Debug.Log("[GearPanelUI] Weapon slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] Weapon slot: NOT ASSIGNED IN INSPECTOR"); }
        if (offHandWeaponSlot != null) { slotMap[GearSlot.OffHandWeapon] = offHandWeaponSlot; Debug.Log("[GearPanelUI] OffHandWeapon slot: ASSIGNED"); } else { Debug.LogWarning("[GearPanelUI] OffHandWeapon slot: NOT ASSIGNED IN INSPECTOR"); }
        if (backpackSlot != null) { slotMap[GearSlot.Backpack] = backpackSlot; Debug.Log("[GearPanelUI] Backpack slot: ASSIGNED"); }
        Debug.Log("[GearPanelUI] ============================================");
        // Note: Backpack might use a custom slot type if needed
    }

    /// <summary>
    /// Gets the slot UI for a specific gear slot type
    /// </summary>
    public GearSlotUI GetSlot(GearSlot slotType)
    {
        if (slotMap.TryGetValue(slotType, out GearSlotUI slot))
        {
            return slot;
        }
        return null;
    }

    /// <summary>
    /// Equips a gear item to the appropriate slot
    /// </summary>
    public void EquipGear(GearSlot slotType, ItemInstance gear)
    {
        Debug.Log($"[GearPanelUI] EquipGear called - Slot: {slotType}, Item: {gear?.displayName ?? "NULL"}");

        GearSlotUI slot = GetSlot(slotType);
        if (slot == null)
        {
            Debug.LogWarning($"[GearPanelUI] No slot found for gear type: {slotType}");
            return;
        }

        Debug.Log($"[GearPanelUI] Found slot for {slotType}: {slot.name}");

        // Find the GearItemUI component (should be a child of the slot)
        GearItemUI gearItemUI = slot.GetComponentInChildren<GearItemUI>(true);
        if (gearItemUI != null)
        {
            gearItemUI.Initialize(gear);
            Debug.Log($"[GearPanelUI] Successfully initialized GearItemUI for {slotType}");
        }
        else
        {
            Debug.LogWarning($"[GearPanelUI] No GearItemUI found for slot: {slotType}");
        }

        // Find LOCAL player and update both gear manager and character data
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player != null)
        {
            CharacterData characterData = player.GetCurrentCharacterData();

            // Update CharacterData.equippedGear for persistence
            if (characterData != null)
            {
                characterData.equippedGear[slotType] = gear;
                CharacterPersistence.SaveCharacter(characterData);
                Debug.Log($"[GearPanelUI] Saved equipped gear to CharacterData: {slotType} = {gear.displayName}");
            }

            // Notify CharacterGearManager to apply stat modifiers
            CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
            if (gearManager != null)
            {
                gearManager.OnGearEquipped(slotType, gear);
            }
            else
            {
                Debug.LogWarning("[GearPanelUI] No CharacterGearManager found on player - gear stats will not be applied!");
            }
        }
    }

    /// <summary>
    /// Unequips gear from a specific slot
    /// </summary>
    public void UnequipGear(GearSlot slotType)
    {
        GearSlotUI slot = GetSlot(slotType);
        if (slot == null) return;

        GearItemUI gearItemUI = slot.GetComponentInChildren<GearItemUI>(true);
        if (gearItemUI != null)
        {
            gearItemUI.Clear();
        }

        // Find LOCAL player and update both gear manager and character data
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player != null)
        {
            CharacterData characterData = player.GetCurrentCharacterData();

            // Remove from CharacterData.equippedGear for persistence
            if (characterData != null && characterData.equippedGear.ContainsKey(slotType))
            {
                characterData.equippedGear.Remove(slotType);
                CharacterPersistence.SaveCharacter(characterData);
                Debug.Log($"[GearPanelUI] Removed equipped gear from CharacterData: {slotType}");
            }

            // Notify CharacterGearManager to remove stat modifiers
            CharacterGearManager gearManager = player.GetComponent<CharacterGearManager>();
            if (gearManager != null)
            {
                gearManager.OnGearUnequipped(slotType);
            }
        }
    }

    /// <summary>
    /// Clears all equipped gear
    /// </summary>
    public void ClearAllGear()
    {
        foreach (var kvp in slotMap)
        {
            GearItemUI gearItemUI = kvp.Value.GetComponentInChildren<GearItemUI>(true);
            if (gearItemUI != null)
            {
                gearItemUI.Clear();
            }
        }
    }

    /// <summary>
    /// Refreshes all gear displays from CharacterData
    /// Call this after equipping gear to ensure UI is up to date
    /// </summary>
    public void RefreshDisplay()
    {
        Debug.Log("[GearPanelUI] RefreshDisplay called - reloading all equipped gear");
        LoadEquippedGearFromPlayer();
    }
}
