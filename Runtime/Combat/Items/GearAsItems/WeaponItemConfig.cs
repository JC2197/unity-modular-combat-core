using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configuration for weapon gear items.
/// Extends GearItemConfig with weapon-specific properties.
/// References existing WeaponConfig assets that have prefabs and settings.
/// </summary>
[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Items/Weapons/Weapon Item Config")]
public class WeaponItemDropsConfig : GearItemConfig
{
    [Header("Weapon Configs")]
    [Tooltip("List of weapon configs that can drop (each has prefab, sprites, and settings)")]
    public List<WeaponConfig> weaponConfigs = new List<WeaponConfig>();

    [Header("Weapon Type")]
    [Tooltip("Fallback weapon type name if configs don't specify (Sword, Axe, Bow, etc.)")]
    public string weaponTypeName = "Weapon";

    // Singleton access (optional - if you want a default weapon config)
    private static WeaponItemDropsConfig defaultInstance;
    public static WeaponItemDropsConfig DefaultInstance
    {
        get
        {
            if (defaultInstance == null)
            {
                defaultInstance = Resources.Load<WeaponItemDropsConfig>("WeaponDropsConfig");
                if (defaultInstance == null)
                {
                    Debug.LogError("[WeaponItemDropsConfig] Failed to load WeaponDropsConfig from Resources! Make sure WeaponDropsConfig.asset exists in Assets/Resources/");
                }
            }
            return defaultInstance;
        }
    }

    /// <summary>
    /// Override to generate weapon items without mutating shared ScriptableObject state.
    /// Selects a random weapon config ONCE and uses it consistently throughout generation.
    /// </summary>
    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        // Pick a random weapon config ONCE and use it for the entire generation
        WeaponConfig selectedConfig = GetRandomWeaponConfig();

        if (selectedConfig == null)
        {
            Debug.LogError($"[WeaponItemDropsConfig] No weapon config available for {name}!");
            return null;
        }

        // Roll rarity (from base ItemConfig)
        int rarityTier = RollRandomRarity();
        GearAdvancementUtility.ResolveWeaponSettings(
            selectedConfig,
            out int advancementLevel,
            out TierScalingConfig scaling,
            out int baseDamageMin,
            out int baseDamageMax);
        ItemTier rolledGearTier = GearAdvancementUtility.RollGearTier(advancementLevel, scaling);

        // Get the weapon name from the selected config
        string weaponName = selectedConfig.weaponName;

        // Get modifier database
        GearModifierDatabase db = modifierDatabase != null ? modifierDatabase : GearModifierDatabase.Instance;
        GearRollResult rollResult = db != null
            ? db.RollGear(weaponName, gearSlot, rarityTier, rolledGearTier)
            : new GearRollResult { displayName = weaponName, modifiers = new List<StatModifier>() };

        // Create display name (no rarity prefix - shown in tooltip)
        string displayName = rollResult.displayName;

        // Create item instance directly (no shared state mutation)
        ItemInstance weaponInstance = new ItemInstance(GetItemType(), displayName, rarityTier, 1);

        // Roll weapon damage with tier scaling
        if (scaling == null)
        {
            // Load default tier scaling from Resources
            scaling = Resources.Load<TierScalingConfig>("TierScaling");
            if (scaling == null)
            {
                Debug.LogWarning("[WeaponItemDropsConfig] No tier scaling config found! Using default multiplier of 1.0");
            }
        }
        
        float tierMultiplier = scaling != null ? scaling.GetMultiplier(rolledGearTier) : 1.0f;
        
        // Roll base damage from resolved progression settings
        int baseDamage = UnityEngine.Random.Range(baseDamageMin, baseDamageMax + 1);
        
        // Apply tier scaling to damage
        int scaledDamage = Mathf.RoundToInt(baseDamage * tierMultiplier);

        // Store weapon-specific data including the config name for sprite lookup later
        WeaponGearData weaponData = new WeaponGearData
        {
            gearSlot = gearSlot,
            modifiers = rollResult.modifiers,
            weaponConfigName = selectedConfig.weaponName,
            weaponType = selectedConfig.weaponType,
            weaponDamage = scaledDamage,
            weaponDamageType = selectedConfig.weaponDamageType,
            itemTier = rolledGearTier,
            grantedTraitID = selectedConfig.grantedTrait != null ? selectedConfig.grantedTrait.traitID : null,
            grantedTraitName = selectedConfig.grantedTrait != null ? selectedConfig.grantedTrait.displayName : null,
            usesAmmo = selectedConfig.usesAmmo,
            magazineSize = selectedConfig.usesAmmo ? selectedConfig.ammoConfig.magazineSize : 0,
            reloadTime = selectedConfig.usesAmmo ? selectedConfig.ammoConfig.reloadTime : 0f
        };
        weaponInstance.additionalData = JsonUtility.ToJson(weaponData);

        Debug.Log($"[WeaponItemDropsConfig] Generated weapon: {displayName} (Config: {selectedConfig.weaponName}, GearTier: {rolledGearTier}, TopModifierTier: {rollResult.rolledTier}, Damage: {scaledDamage} [{baseDamage} x {tierMultiplier:F1}])");

        return weaponInstance;
    }

    /// <summary>
    /// Not used for weapon generation (we handle naming directly in GenerateItem).
    /// Kept for interface compatibility but should not be called during normal generation.
    /// </summary>
    protected override string GetGearTypeName()
    {
        // This method is not used during GenerateItem anymore.
        // Return a fallback in case it's called elsewhere.
        return weaponTypeName;
    }

    protected override string GetItemType()
    {
        return "weapon";
    }

    /// <summary>
    /// Get weapon config by name
    /// </summary>
    public WeaponConfig GetWeaponConfigByName(string configName)
    {
        if (weaponConfigs == null)
        {
            Debug.LogWarning($"[WeaponItemDropsConfig] weaponConfigs list is null!");
            return null;
        }
        foreach (var config in weaponConfigs)
        {
            if (config != null)
            {
                if (config.weaponName == configName)
                {
                    return config;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get inventory sprite for a specific weapon config name
    /// </summary>
    public Sprite GetInventorySpriteForWeapon(string weaponConfigName)
    {
        Debug.Log($"[WeaponItemDropsConfig] GetInventorySpriteForWeapon called for: {weaponConfigName}");
        WeaponConfig config = GetWeaponConfigByName(weaponConfigName);
        if (config != null)
        {
            Debug.Log($"[WeaponItemDropsConfig]   Found config, sprite: {(config.inventorySprite != null ? config.inventorySprite.name : "NULL")}");
        }
        return config?.inventorySprite;
    }

    /// <summary>
    /// Get world sprite for a specific weapon config name
    /// </summary>
    public Sprite GetWorldSpriteForWeapon(string weaponConfigName)
    {
        Debug.Log($"[WeaponItemDropsConfig] GetWorldSpriteForWeapon called for: {weaponConfigName}");
        WeaponConfig config = GetWeaponConfigByName(weaponConfigName);
        if (config != null)
        {
            Debug.Log($"[WeaponItemDropsConfig]   Found config, sprite: {(config.worldSprite != null ? config.worldSprite.name : "NULL")}");
        }
        return config?.worldSprite;
    }

    /// <summary>
    /// Get random weapon config from the list
    /// </summary>
    public WeaponConfig GetRandomWeaponConfig()
    {
        if (weaponConfigs == null || weaponConfigs.Count == 0)
        {
            Debug.LogWarning($"[WeaponItemDropsConfig] No weapon configs configured for {name}");
            return null;
        }

        return weaponConfigs[Random.Range(0, weaponConfigs.Count)];
    }

    /// <summary>
    /// Get random weapon prefab from the list (for backwards compatibility)
    /// </summary>
    public GameObject GetRandomWeaponPrefab()
    {
        WeaponConfig config = GetRandomWeaponConfig();
        return config?.weaponPrefab;
    }
}

/// <summary>
/// Extended gear data for weapons that includes the weapon config name
/// </summary>
[System.Serializable]
public class WeaponGearData : GearItemData
{
    public string weaponConfigName;
    public string weaponType;
    public int weaponDamage;
    public string weaponDamageType = "Slashing";
    public bool usesAmmo = false;
    public int magazineSize = 0;
    public float reloadTime = 0f;

}
