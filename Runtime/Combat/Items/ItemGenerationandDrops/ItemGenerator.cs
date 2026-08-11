using UnityEngine;
using System.Collections.Generic;
using JoeConticello.ModularCombatCore;

/// <summary>
/// Centralized system for generating ItemInstances from configs.
/// Handles both procedural drops and direct config-based generation (like starter gear).
/// </summary>
public static class ItemGenerator
{
    /// <summary>
    /// Generate an item from an ItemConfig (for procedural drops from drop tables)
    /// </summary>
    public static ItemInstance GenerateFromConfig(ItemConfig itemConfig, int contextLevel = 1)
    {
        if (itemConfig == null)
        {
            Debug.LogError("[ItemGenerator] Cannot generate item - itemConfig is null!");
            return null;
        }

        // Use the config's GenerateItem method (handles procedural generation with rarity rolls, etc.)
        ItemInstance generatedItem = itemConfig.GenerateItem(contextLevel);
        
        if (generatedItem != null)
        {
            Debug.Log($"[ItemGenerator] Generated item from config: {generatedItem.displayName} (Type: {generatedItem.itemType})");
        }
        else
        {
            Debug.LogWarning($"[ItemGenerator] Failed to generate item from config: {itemConfig.name}");
        }

        return generatedItem;
    }

    /// <summary>
    /// Generate a weapon item directly from a WeaponConfig (for starter gear, quest rewards, etc.)
    /// </summary>
    public static ItemInstance GenerateWeaponFromConfig(WeaponConfig weaponConfig, int rarityTier = 0)
    {
        if (weaponConfig == null)
        {
            Debug.LogError("[ItemGenerator] Cannot generate weapon - weaponConfig is null!");
            return null;
        }

        // Roll modifiers using GearModifierDatabase
        GearModifierDatabase db = GearModifierDatabase.Instance;
        GearAdvancementUtility.ResolveWeaponSettings(
            weaponConfig,
            out int advancementLevel,
            out TierScalingConfig scaling,
            out int baseDamageMin,
            out int baseDamageMax);
        ItemTier rolledGearTier = GearAdvancementUtility.RollGearTier(advancementLevel, scaling);
        GearRollResult rollResult = db != null
            ? db.RollGear(weaponConfig.weaponName, GearSlot.Weapon, rarityTier, rolledGearTier)
            : new GearRollResult { displayName = weaponConfig.weaponName, modifiers = new List<StatModifier>() };

        // Create display name (no rarity prefix - shown in tooltip)
        string displayName = rollResult.displayName;

        // Create item instance
        ItemInstance weaponItem = new ItemInstance("weapon", displayName, rarityTier, 1);

        // Roll weapon damage with tier scaling
        if (scaling == null)
        {
            // Load default tier scaling from Resources
            scaling = Resources.Load<TierScalingConfig>("TierScaling");
            if (scaling == null)
            {
                Debug.LogWarning("[ItemGenerator] No tier scaling config found! Using default multiplier of 1.0");
            }
        }
        
        float tierMultiplier = scaling != null ? scaling.GetMultiplier(rolledGearTier) : 1.0f;
        
        // Roll base damage from resolved progression settings
        int baseDamage = UnityEngine.Random.Range(baseDamageMin, baseDamageMax + 1);
        
        // Apply tier scaling to damage
        int scaledDamage = Mathf.RoundToInt(baseDamage * tierMultiplier);

        // Store weapon config data with rolled modifiers
        WeaponGearData weaponData = new WeaponGearData
        {
            gearSlot = GearSlot.Weapon,
            modifiers = rollResult.modifiers,
            weaponConfigName = weaponConfig.weaponName,
            weaponType = weaponConfig.weaponType,
            weaponDamage = scaledDamage,
            weaponDamageType = weaponConfig.weaponDamageType,
            itemTier = rolledGearTier,
            grantedTraitID = weaponConfig.grantedTrait != null ? weaponConfig.grantedTrait.traitID : null,
            grantedTraitName = weaponConfig.grantedTrait != null ? weaponConfig.grantedTrait.displayName : null
        };
        weaponItem.additionalData = JsonUtility.ToJson(weaponData);

        Debug.Log($"[ItemGenerator] Generated weapon from config: {displayName} (GearTier: {rolledGearTier}, TopModifierTier: {rollResult.rolledTier}, Damage: {scaledDamage} [{baseDamage} x {tierMultiplier:F1}]) with {rollResult.modifiers.Count} modifiers");
        return weaponItem;
    }

    /// <summary>
    /// Get rarity name for display (matches RarityConfig naming)
    /// </summary>
    private static string GetRarityName(int rarityTier)
    {
        string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        return rarityTier >= 0 && rarityTier < rarityNames.Length ? rarityNames[rarityTier] : "Common";
    }

    /// <summary>
    /// Generate an armor item directly from an ArmorConfig (for starter gear, quest rewards, etc.)
    /// </summary>
    public static ItemInstance GenerateArmorFromConfig(ArmorConfig armorConfig, int rarityTier = 0)
    {
        if (armorConfig == null)
        {
            Debug.LogError("[ItemGenerator] Cannot generate armor - armorConfig is null!");
            return null;
        }

        // Determine gear slot from armor config
        GearSlot gearSlot = ConvertArmorSlotToGearSlot(armorConfig.armorSlot);

        // Roll modifiers using GearModifierDatabase
        GearModifierDatabase db = GearModifierDatabase.Instance;
        GearAdvancementUtility.ResolveArmorSettings(
            armorConfig,
            out int armorAdvancementLevel,
            out TierScalingConfig armorScaling,
            out List<StatModifierRange> baseStatRanges,
            out List<StatModifier> legacyBaseStats,
            out List<DefensiveStatRange> legacyDefensiveRanges);
        ItemTier rolledGearTier = GearAdvancementUtility.RollGearTier(armorAdvancementLevel, armorScaling);
        GearRollResult rollResult = db != null
            ? db.RollGear(armorConfig.gearName, gearSlot, rarityTier, rolledGearTier)
            : new GearRollResult { displayName = armorConfig.gearName, modifiers = new List<StatModifier>() };

        // Roll defensive stats from ArmorConfig — stored separately so the tooltip can
        // distinguish base armor values from randomly rolled gear modifiers.
        List<StatModifier> defensiveStats = GearAdvancementUtility.RollBaseStats(baseStatRanges, legacyBaseStats);
        if (defensiveStats.Count == 0)
        {
            defensiveStats = GearAdvancementUtility.RollLegacyDefensiveStats(legacyDefensiveRanges);
        }
        List<StatModifier> scaledDefensiveStats = new List<StatModifier>();
        foreach (var defensiveStat in defensiveStats)
        {
            float scaledValue = TierScaler.ScaleValue(defensiveStat.value, rolledGearTier, armorScaling);
            scaledDefensiveStats.Add(new StatModifier
            {
                statID = defensiveStat.statID,
                value = scaledValue,
                modifierType = defensiveStat.modifierType
            });
        }

        // Create display name (no rarity prefix - shown in tooltip)
        string displayName = rollResult.displayName;

        // Create item instance
        ItemInstance armorItem = new ItemInstance("armor", displayName, rarityTier, 1);

        // Store armor config data with rolled modifiers
        ArmorGearData armorData = new ArmorGearData
        {
            gearSlot = gearSlot,
            modifiers = rollResult.modifiers,
            baseStatModifiers = scaledDefensiveStats,
            armorConfigName = armorConfig.gearName,
            armorSlotType = gearSlot,
            itemTier = rolledGearTier,
            grantedTraitID = armorConfig.grantedTrait != null ? armorConfig.grantedTrait.traitID : null,
            grantedTraitName = armorConfig.grantedTrait != null ? armorConfig.grantedTrait.displayName : null
        };
        armorItem.additionalData = JsonUtility.ToJson(armorData);

        Debug.Log($"[ItemGenerator] Generated armor from config: {displayName} (Slot: {gearSlot}) with {rollResult.modifiers.Count} modifiers");
        return armorItem;
    }

    /// <summary>
    /// Generate a procedural map key from MapKeyConfig (uses config's procedural generation logic)
    /// For specific level/arena keys, use MapKeyConfig.GenerateItem() directly
    /// </summary>
    public static ItemInstance GenerateMapKeyFromConfig(MapKeyConfig mapKeyConfig, int currentMapLevel = 1)
    {
        if (mapKeyConfig == null)
        {
            Debug.LogError("[ItemGenerator] Cannot generate map key - mapKeyConfig is null!");
            return null;
        }

        // Use the config's procedural generation logic (random arena, level range, etc.)
        ItemInstance keyItem = mapKeyConfig.GenerateItem(currentMapLevel);

        if (keyItem != null)
        {
            Debug.Log($"[ItemGenerator] Generated procedural map key: {keyItem.displayName}");
        }
        
        return keyItem;
    }

    /// <summary>
    /// Helper to convert ArmorSlot to GearSlot enum
    /// </summary>
    private static GearSlot ConvertArmorSlotToGearSlot(ArmorSlot armorSlot)
    {
        switch (armorSlot)
        {
            case ArmorSlot.Head:
                return GearSlot.Head;
            case ArmorSlot.Chest:
                return GearSlot.Chest;
            case ArmorSlot.Hands:
                return GearSlot.Hands;
            case ArmorSlot.Legs:
                return GearSlot.Feet;
            case ArmorSlot.Backpack:
                return GearSlot.Backpack;
            default:
                Debug.LogWarning($"[ItemGenerator] Unknown ArmorSlot: {armorSlot}, defaulting to Chest");
                return GearSlot.Chest;
        }
    }

}
