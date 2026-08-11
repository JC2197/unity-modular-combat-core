using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Configuration for armor gear items.
/// Extends GearItemConfig with armor-specific properties.
/// References existing armor config assets with slot determined by ArmorConfig.armorSlot.
/// </summary>
[CreateAssetMenu(fileName = "ArmorItemDropsConfig", menuName = "Armor/Armor Item Config")]
public class ArmorItemDropsConfig : GearItemConfig
{
    [Header("Armor Configs")]
    [Tooltip("List of all armor configs that can drop (slot determined by each config's armorSlot field)")]
    public List<ArmorConfig> armorConfigs = new List<ArmorConfig>();
    
    [Header("Armor Type")]
    [Tooltip("Fallback armor type name if configs don't specify")]
    public string armorTypeName = "Armor";
    
    // Singleton access
    private static ArmorItemDropsConfig defaultInstance;
    public static ArmorItemDropsConfig DefaultInstance
    {
        get
        {
            if (defaultInstance == null)
            {
                defaultInstance = Resources.Load<ArmorItemDropsConfig>("ArmorDropsConfig");
                if (defaultInstance == null)
                {
                    Debug.LogError("[ArmorItemDropsConfig] Failed to load ArmorDropsConfig from Resources! Make sure ArmorDropsConfig.asset exists in Assets/Resources/");
                }
            }
            return defaultInstance;
        }
    }
    
    /// <summary>
    /// Override to generate armor items without mutating shared ScriptableObject state.
    /// Selects a random armor config ONCE and uses it consistently throughout generation.
    /// </summary>
    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        // Pick a random armor config ONCE from the unified list
        ArmorConfig selectedConfig = GetRandomArmorConfig();
        
        if (selectedConfig == null)
        {
            Debug.LogError($"[ArmorItemDropsConfig] No armor config available for {name}!");
            return null;
        }
        
        // Get slot from the armor config itself
        GearSlot selectedSlot = ConvertArmorSlotToGearSlot(selectedConfig.armorSlot);
        
        // Roll rarity (from base ItemConfig)
        int rarityTier = RollRandomRarity();
        GearAdvancementUtility.ResolveArmorSettings(
            selectedConfig,
            out int advancementLevel,
            out TierScalingConfig scaling,
            out List<StatModifierRange> baseStatRanges,
            out List<StatModifier> legacyBaseStats,
            out List<DefensiveStatRange> legacyDefensiveRanges);
        ItemTier rolledGearTier = GearAdvancementUtility.RollGearTier(advancementLevel, scaling);
        
        // Get modifier database
        GearModifierDatabase db = modifierDatabase != null ? modifierDatabase : GearModifierDatabase.Instance;
        GearRollResult rollResult = db != null 
            ? db.RollGear(selectedConfig.gearName, selectedSlot, rarityTier, rolledGearTier)
            : new GearRollResult { displayName = selectedConfig.gearName, modifiers = new List<StatModifier>() };
        
        // Roll base defensive stats from config (armor, force field, dodge chance)
        // These are stored separately so the tooltip can distinguish them from rolled gear modifiers.
        List<StatModifier> defensiveStats = GearAdvancementUtility.RollBaseStats(baseStatRanges, legacyBaseStats);
        if (defensiveStats.Count == 0)
        {
            defensiveStats = GearAdvancementUtility.RollLegacyDefensiveStats(legacyDefensiveRanges);
        }
        List<StatModifier> scaledDefensiveStats = new List<StatModifier>();
        foreach (var defensiveStat in defensiveStats)
        {
            float scaledValue = TierScaler.ScaleValue(defensiveStat.value, rolledGearTier, scaling);
            scaledDefensiveStats.Add(new StatModifier
            {
                statID = defensiveStat.statID,
                value = scaledValue,
                modifierType = defensiveStat.modifierType
            });
            Debug.Log($"[ArmorItemDropsConfig] Added base {defensiveStat.statID}: {scaledValue} (rolled: {defensiveStat.value}, gearTier: {rolledGearTier})");
        }
        
        // Create display name (no rarity prefix - shown in tooltip)
        string displayName = rollResult.displayName;
        
        // Create item instance directly (no shared state mutation)
        ItemInstance armorInstance = new ItemInstance(GetItemType(), displayName, rarityTier, 1);
        
        // Store armor-specific data including the config name for sprite lookup later
        ArmorGearData armorData = new ArmorGearData
        {
            gearSlot = selectedSlot,
            modifiers = rollResult.modifiers,
            baseStatModifiers = scaledDefensiveStats,
            armorConfigName = selectedConfig.gearName,
            armorSlotType = selectedSlot,
            itemTier = rolledGearTier,
            grantedTraitID = selectedConfig.grantedTrait != null ? selectedConfig.grantedTrait.traitID : null,
            grantedTraitName = selectedConfig.grantedTrait != null ? selectedConfig.grantedTrait.displayName : null
        };
        armorInstance.additionalData = JsonUtility.ToJson(armorData);
        
        Debug.Log($"[ArmorItemDropsConfig] Generated armor: {displayName} (Config: {selectedConfig.gearName}, Slot: {selectedSlot}, GearTier: {rolledGearTier}, TopModifierTier: {rollResult.rolledTier})");
        
        return armorInstance;
    }
    
    /// <summary>
    /// Not used for armor generation (we handle naming directly in GenerateItem).
    /// Kept for interface compatibility but should not be called during normal generation.
    /// </summary>
    protected override string GetGearTypeName()
    {
        // This method is not used during GenerateItem anymore.
        // Return a fallback in case it's called elsewhere.
        return armorTypeName;
    }
    
    protected override string GetItemType()
    {
        return "armor";
    }
    
    /// <summary>
    /// Select a random armor config from the unified list
    /// </summary>
    private ArmorConfig GetRandomArmorConfig()
    {
        if (armorConfigs == null || armorConfigs.Count == 0)
        {
            Debug.LogWarning($"[ArmorItemDropsConfig] No armor configs configured for {name}");
            return null;
        }
        
        return armorConfigs[Random.Range(0, armorConfigs.Count)];
    }
    
    /// <summary>
    /// Convert ArmorSlot to GearSlot enum
    /// </summary>
    private GearSlot ConvertArmorSlotToGearSlot(ArmorSlot armorSlot)
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
                Debug.LogWarning($"[ArmorItemDropsConfig] Unknown ArmorSlot: {armorSlot}, defaulting to Chest");
                return GearSlot.Chest;
        }
    }
    
    /// <summary>
    /// Get armor config by name and slot type
    /// </summary>
    public ArmorConfig GetArmorConfigByName(string configName, GearSlot slotType)
    {
        if (armorConfigs == null)
        {
            Debug.LogWarning($"[ArmorItemDropsConfig] armorConfigs list is null!");
            return null;
        }
        
        foreach (var config in armorConfigs)
        {
            if (config != null && config.gearName == configName)
            {
                // Verify slot type matches
                GearSlot configSlot = ConvertArmorSlotToGearSlot(config.armorSlot);
                if (configSlot == slotType)
                {
                    return config;
                }
            }
        }
        
        Debug.LogWarning($"[ArmorItemDropsConfig] No armor config found with name '{configName}' and slot type {slotType}");
        return null;
    }
    
    /// <summary>
    /// Get inventory sprite for a specific armor config
    /// </summary>
    public Sprite GetInventorySpriteForArmor(string armorConfigName, GearSlot slotType)
    {
        // Use registry for more reliable lookup
        ArmorConfig config = ArmorConfigRegistry.GetConfig(armorConfigName);
        if (config != null)
        {
            // Verify slot type matches
            GearSlot configSlot = ConvertArmorSlotToGearSlot(config.armorSlot);
            if (configSlot == slotType)
            {
                return config.inventorySprite;
            }
            else
            {
                Debug.LogWarning($"[ArmorItemDropsConfig] Armor '{armorConfigName}' slot mismatch. Expected {slotType}, found {configSlot}");
            }
        }
        
        // Fallback to old list-based lookup
        ArmorConfig listConfig = GetArmorConfigByName(armorConfigName, slotType);
        return listConfig?.inventorySprite;
    }
    
    /// <summary>
    /// Get world sprite for a specific armor config
    /// </summary>
    public Sprite GetWorldSpriteForArmor(string armorConfigName, GearSlot slotType)
    {
        // Use registry for more reliable lookup
        ArmorConfig config = ArmorConfigRegistry.GetConfig(armorConfigName);
        if (config != null)
        {
            // Verify slot type matches
            GearSlot configSlot = ConvertArmorSlotToGearSlot(config.armorSlot);
            if (configSlot == slotType)
            {
                return config.worldSprite;
            }
            else
            {
                Debug.LogWarning($"[ArmorItemDropsConfig] Armor '{armorConfigName}' slot mismatch. Expected {slotType}, found {configSlot}");
            }
        }
        
        // Fallback to old list-based lookup
        ArmorConfig listConfig = GetArmorConfigByName(armorConfigName, slotType);
        return listConfig?.worldSprite;
    }

}

/// <summary>
/// Extended gear data for armor that includes the armor config name and slot type
/// </summary>
[System.Serializable]
public class ArmorGearData : GearItemData
{
    public string armorConfigName;
    public GearSlot armorSlotType;

    [FormerlySerializedAs("baseDefensiveModifiers")]
    public List<StatModifier> baseStatModifiers = new List<StatModifier>();
}
