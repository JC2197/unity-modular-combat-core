using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages stat modifiers from equipped gear.
/// Works alongside CharacterTraitManager to provide total stat bonuses.
/// Also handles traits granted by equipped gear.
/// Attach this to the player character GameObject.
/// </summary>
public class CharacterGearManager : MonoBehaviour
{
    // Cached stat modifiers from equipped gear.
    // OrdinalIgnoreCase so statIDs match regardless of capitalisation (consistent with CharacterTraitManager).
    private Dictionary<string, float> cachedFlatModifiers = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, float> cachedPercentageModifiers = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);

    // Track currently equipped gear
    private Dictionary<GearSlot, ItemInstance> equippedGear = new Dictionary<GearSlot, ItemInstance>();
    
    // Track traits granted by equipped gear (slot -> traitID)
    private Dictionary<GearSlot, string> grantedTraitsFromGear = new Dictionary<GearSlot, string>();
    
    // Reference to TraitDataList for looking up traits by ID
    [SerializeField] private TraitDataList traitDataList;
    
    // Reference to CharacterTraitManager for granting/removing traits
    private CharacterTraitManager traitManager;

    // Events
    public System.Action OnGearModifiersChanged;
    
    private void Awake()
    {
        traitManager = GetComponent<CharacterTraitManager>();
        
        // Load TraitDataList from Resources if not assigned
        if (traitDataList == null)
        {
            traitDataList = Resources.Load<TraitDataList>("TraitDataList");
            if (traitDataList == null)
            {
                Debug.LogWarning("[CharacterGearManager] No TraitDataList found in Resources! Gear traits will not work.");
            }
        }
    }

    /// <summary>
    /// Called when a gear item is equipped
    /// </summary>
    public void OnGearEquipped(GearSlot slot, ItemInstance item)
    {
        if (item == null)
        {
            Debug.LogWarning($"[CharacterGearManager] Attempted to equip null item to slot {slot}");
            return;
        }

        // Store equipped gear reference
        equippedGear[slot] = item;
        
        // Grant trait if this gear has one
        GrantTraitFromGear(slot, item);
        
        // Recalculate all modifiers
        RecalculateModifiers();

        // Notify listeners (e.g., PlayerController to recalculate stats)
        OnGearModifiersChanged?.Invoke();
    }

    /// <summary>
    /// Called when a gear item is unequipped
    /// </summary>
    public void OnGearUnequipped(GearSlot slot)
    {
        // Remove trait if this gear granted one
        RemoveTraitFromGear(slot);
        
        if (equippedGear.ContainsKey(slot))
        {
            equippedGear.Remove(slot);
            RecalculateModifiers();
            // Notify listeners
            OnGearModifiersChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[CharacterGearManager] Attempted to unequip from slot {slot} but no gear was equipped there");
        }
    }

    /// <summary>
    /// Recalculate all stat modifiers from equipped gear
    /// </summary>
    private void RecalculateModifiers()
    {
        cachedFlatModifiers.Clear();
        cachedPercentageModifiers.Clear();

        Debug.Log($"[CharacterGearManager] Recalculating modifiers from {equippedGear.Count} equipped items");

        foreach (var kvp in equippedGear)
        {
            GearSlot slot = kvp.Key;
            ItemInstance item = kvp.Value;

            // Parse gear data from additionalData JSON
            if (string.IsNullOrEmpty(item.additionalData))
            {
                Debug.LogWarning($"[CharacterGearManager] Item {item.displayName} has no additionalData");
                continue;
            }

            GearItemData gearData = null;
            try
            {
                gearData = JsonUtility.FromJson<GearItemData>(item.additionalData);

                if (gearData != null)
                {
                    if (gearData.modifiers != null && gearData.modifiers.Count > 0)
                    {
                        for (int i = 0; i < gearData.modifiers.Count; i++)
                        {
                            var mod = gearData.modifiers[i];
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterGearManager] JsonUtility returned null for {item.displayName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterGearManager] Failed to parse gear data for {item.displayName}: {e.Message}");
                Debug.LogError($"[CharacterGearManager] Stack trace: {e.StackTrace}");
                continue;
            }

            if (gearData == null || gearData.modifiers == null || gearData.modifiers.Count == 0)
            {
                // Armor items may only contribute base stat modifiers (no rolled modifiers is valid)
                if (item.itemType?.ToLower() == "armor")
                {
                    ArmorGearData armorOnly = JsonUtility.FromJson<ArmorGearData>(item.additionalData);
                    if (armorOnly?.baseStatModifiers != null)
                    {
                        foreach (var mod in armorOnly.baseStatModifiers)
                        {
                            if (mod.modifierType == ModifierType.Flat)
                            {
                                if (!cachedFlatModifiers.ContainsKey(mod.statID)) cachedFlatModifiers[mod.statID] = 0f;
                                cachedFlatModifiers[mod.statID] += mod.value;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"[CharacterGearManager] Item {item.displayName} has no modifiers (gearData null: {gearData == null}, modifiers null: {gearData?.modifiers == null}, count: {gearData?.modifiers?.Count ?? 0})");
                }
                continue;
            }

            // For armor, also apply base stat modifiers (stored separately from rolled modifiers)
            if (item.itemType?.ToLower() == "armor")
            {
                ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(item.additionalData);
                if (armorData?.baseStatModifiers != null)
                {
                    foreach (var mod in armorData.baseStatModifiers)
                    {
                        if (mod.modifierType == ModifierType.Flat)
                        {
                            if (!cachedFlatModifiers.ContainsKey(mod.statID)) cachedFlatModifiers[mod.statID] = 0f;
                            cachedFlatModifiers[mod.statID] += mod.value;
                            Debug.Log($"[CharacterGearManager]     -> Added base stat Flat: {mod.statID} now = {cachedFlatModifiers[mod.statID]}");
                        }
                    }
                }
            }

            // Migrate legacy percentage modifiers (values < 1 stored as decimals like 0.047 instead of 4.7)
            foreach (var modifier in gearData.modifiers)
            {
                if (modifier.modifierType == ModifierType.Percentage && modifier.value < 1f && modifier.value > 0f)
                {
                    float oldValue = modifier.value;
                    modifier.value = modifier.value * 100f;
                }
            }

            // Aggregate all modifiers
            foreach (var modifier in gearData.modifiers)
            {
                string modTypeStr = modifier.modifierType == ModifierType.Flat ? "Flat" :
                                   modifier.modifierType == ModifierType.Percentage ? "Percentage" : "Override";

                switch (modifier.modifierType)
                {
                    case ModifierType.Flat:
                        if (!cachedFlatModifiers.ContainsKey(modifier.statID))
                            cachedFlatModifiers[modifier.statID] = 0f;
                        cachedFlatModifiers[modifier.statID] += modifier.value;
                        Debug.Log($"[CharacterGearManager]     -> Added to Flat modifiers: {modifier.statID} now = {cachedFlatModifiers[modifier.statID]}");
                        break;

                    case ModifierType.Percentage:
                        if (!cachedPercentageModifiers.ContainsKey(modifier.statID))
                            cachedPercentageModifiers[modifier.statID] = 0f;
                        cachedPercentageModifiers[modifier.statID] += modifier.value;
                        Debug.Log($"[CharacterGearManager]     -> Added to Percentage modifiers: {modifier.statID} now = {cachedPercentageModifiers[modifier.statID]}%");
                        break;

                    case ModifierType.Override:
                        Debug.LogWarning($"[CharacterGearManager] Override modifier type not supported for gear: {modifier.statID}");
                        break;
                }
            }
        }
        // Log final modifiers
        if (cachedFlatModifiers.Count > 0)
        {
            Debug.Log($"[CharacterGearManager] Flat Modifiers:");
            foreach (var kvp in cachedFlatModifiers)
            {
                Debug.Log($"[CharacterGearManager]   {kvp.Key}: +{kvp.Value}");
            }
        }
        if (cachedPercentageModifiers.Count > 0)
        {
            Debug.Log($"[CharacterGearManager] Percentage Modifiers:");
            foreach (var kvp in cachedPercentageModifiers)
            {
                Debug.Log($"[CharacterGearManager]   {kvp.Key}: +{kvp.Value}%");
            }
        }
    }

    /// <summary>
    /// Get the total flat modifier for a stat from all equipped gear
    /// </summary>
    public float GetFlatModifier(string statID)
    {
        return cachedFlatModifiers.ContainsKey(statID) ? cachedFlatModifiers[statID] : 0f;
    }

    /// <summary>
    /// Get the total percentage modifier for a stat from all equipped gear (additive)
    /// </summary>
    public float GetPercentageModifier(string statID)
    {
        return cachedPercentageModifiers.ContainsKey(statID) ? cachedPercentageModifiers[statID] : 0f;
    }



    /// <summary>
    /// Load equipped gear from CharacterData on initialization.
    /// Populates the internal equippedGear dictionary and recalculates cached modifiers,
    /// but does NOT fire OnGearModifiersChanged event (to avoid triggering stat recalc
    /// before the character is fully built). PlayerController.SetupCharacter() calls
    /// RecalculateStatsWithTraits() explicitly after this to apply modifiers.
    /// Called by PlayerController after CharacterData is assigned.
    /// </summary>
    public void LoadEquippedGear(Dictionary<GearSlot, ItemInstance> loadedGear)
    {
        if (loadedGear == null || loadedGear.Count == 0)
        {
            Debug.Log("[CharacterGearManager] No equipped gear to load");
            return;
        }
        
        Debug.Log($"[CharacterGearManager] Loading {loadedGear.Count} equipped items from CharacterData");
        
        equippedGear.Clear();
        foreach (var kvp in loadedGear)
        {
            equippedGear[kvp.Key] = kvp.Value;
        }

        // Restore traits from loaded gear.  We use isRestoring: true so the ownership
        // guard inside UnlockTrait is bypassed — the character is being rebuilt from
        // saved data, not responding to a live player action.
        foreach (var kvp in equippedGear)
            GrantTraitFromGearRestoring(kvp.Key, kvp.Value);

        // Recalculate modifiers from loaded gear into internal cache.
        // This makes GetFlatModifier/GetPercentageModifier return correct values,
        // but doesn't yet apply them to the player's AllStats (that happens when
        // PlayerController calls RecalculateStatsWithTraits after character build).
        RecalculateModifiers();
        
        // Deliberately skip OnGearModifiersChanged?.Invoke() here - see summary comment above.
    }

    /// <summary>
    /// Get all currently equipped gear
    /// </summary>
    public Dictionary<GearSlot, ItemInstance> GetEquippedGear()
    {
        return new Dictionary<GearSlot, ItemInstance>(equippedGear);
    }
    
    /// <summary>
    /// Grant a trait from equipped gear
    /// </summary>
    private void GrantTraitFromGear(GearSlot slot, ItemInstance item)
    {
        if (traitManager == null || traitDataList == null) return;
        
        // Parse gear data to find granted trait
        if (string.IsNullOrEmpty(item.additionalData))
            return;
            
        try
        {
            GearItemData gearData = JsonUtility.FromJson<GearItemData>(item.additionalData);
            
            if (gearData == null || string.IsNullOrEmpty(gearData.grantedTraitID))
                return;
                
            // Find the trait in the global trait list
            TraitData trait = FindTraitByID(gearData.grantedTraitID);
            
            if (trait == null)
            {
                Debug.LogWarning($"[CharacterGearManager] Gear '{item.displayName}' references unknown trait ID: {gearData.grantedTraitID}");
                return;
            }
            
            // Generate a unique node ID for gear-granted traits (includes slot to make it unique)
            string gearTraitNodeID = $"gear_{slot}_{gearData.grantedTraitID}";
            
            // Unlock the trait (this will add it to active traits and trigger stat recalculation)
            bool success = traitManager.UnlockTrait(gearTraitNodeID, trait, isRestoring: false);
            
            if (success)
            {
                grantedTraitsFromGear[slot] = gearTraitNodeID;
                Debug.Log($"[CharacterGearManager] Granted trait '{trait.displayName}' from gear '{item.displayName}' in slot {slot}");
            }
            else
            {
                Debug.LogWarning($"[CharacterGearManager] Failed to grant trait '{trait.displayName}' from gear '{item.displayName}'");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterGearManager] Error granting trait from gear: {e.Message}");
        }
    }
    
    /// <summary>
    /// Same as GrantTraitFromGear but passes isRestoring:true to UnlockTrait so the
    /// ownership guard is bypassed.  Used exclusively by LoadEquippedGear (spawn/reload path).
    /// </summary>
    private void GrantTraitFromGearRestoring(GearSlot slot, ItemInstance item)
    {
        if (traitManager == null || traitDataList == null) return;
        if (string.IsNullOrEmpty(item?.additionalData)) return;

        try
        {
            GearItemData gearData = JsonUtility.FromJson<GearItemData>(item.additionalData);
            if (gearData == null || string.IsNullOrEmpty(gearData.grantedTraitID)) return;

            TraitData trait = FindTraitByID(gearData.grantedTraitID);
            if (trait == null)
            {
                Debug.LogWarning($"[CharacterGearManager] Load: gear '{item.displayName}' references unknown trait ID: {gearData.grantedTraitID}");
                return;
            }

            string gearTraitNodeID = $"gear_{slot}_{gearData.grantedTraitID}";
            bool success = traitManager.UnlockTrait(gearTraitNodeID, trait, isRestoring: true);
            if (success)
            {
                grantedTraitsFromGear[slot] = gearTraitNodeID;
                Debug.Log($"[CharacterGearManager] Restored trait '{trait.displayName}' from gear slot {slot}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterGearManager] Error restoring trait from gear: {e.Message}");
        }
    }

    /// <summary>
    /// Remove a trait granted by unequipped gear
    /// </summary>
    private void RemoveTraitFromGear(GearSlot slot)
    {
        if (traitManager == null) return;
        
        // Check if this slot had a granted trait
        if (!grantedTraitsFromGear.TryGetValue(slot, out string traitNodeID))
            return;
            
        // Remove the trait by node ID
        bool success = traitManager.RemoveTraitByNode(traitNodeID);
        
        if (success)
        {
            grantedTraitsFromGear.Remove(slot);
            Debug.Log($"[CharacterGearManager] Removed trait from gear slot {slot}");
        }
        else
        {
            Debug.LogWarning($"[CharacterGearManager] Failed to remove trait from gear slot {slot}");
        }
    }
    
    /// <summary>
    /// Find a trait by its ID in the global trait list
    /// </summary>
    private TraitData FindTraitByID(string traitID)
    {
        if (traitDataList == null || traitDataList.traitGroups == null)
            return null;
            
        foreach (var trait in traitDataList.AllTraits)
        {
            if (trait != null && trait.traitID == traitID)
                return trait;
        }
        
        return null;
    }
}
