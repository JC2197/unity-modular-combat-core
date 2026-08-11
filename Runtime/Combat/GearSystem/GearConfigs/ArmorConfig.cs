using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Defines a range for a defensive stat on armor.
/// When the item is generated, a value is rolled between min and max.
/// </summary>
[System.Serializable]
public class DefensiveStatRange
{
    [Tooltip("Type of defensive stat")]
    public DefensiveStatType statType = DefensiveStatType.Armor;
    
    [Tooltip("Minimum value (inclusive)")]
    public float minValue = 0f;
    
    [Tooltip("Maximum value (inclusive)")]
    public float maxValue = 10f;
    
    /// <summary>
    /// Roll a random value within the range
    /// </summary>
    public float RollValue()
    {
        return Random.Range(minValue, maxValue);
    }
    
    /// <summary>
    /// Get the stat ID used in the stat system
    /// </summary>
    public string GetStatID()
    {
        switch (statType)
        {
            case DefensiveStatType.Armor:
                return "Armor";
            case DefensiveStatType.ForceField:
                return "ForceField";
            case DefensiveStatType.DodgeChance:
                return "DodgeChance";
            default:
                return "Armor";
        }
    }
}

public enum DefensiveStatType
{
    Armor,          // Flat damage reduction
    ForceField,     // Energy shield (absorbs damage before health)
    DodgeChance     // Chance to avoid damage entirely (0-1 as percentage)
}

[System.Serializable]
public class StatModifierRange
{
    [Tooltip("Stat to modify (from StatTypeDatabase)")]
    public string statID = "Armor";

    [Tooltip("How to apply the modifier")]
    public ModifierType modifierType = ModifierType.Flat;

    [Tooltip("Minimum rolled value (inclusive)")]
    public float minValue = 1f;

    [Tooltip("Maximum rolled value (inclusive)")]
    public float maxValue = 2f;

    public float RollValue()
    {
        if (maxValue < minValue)
            maxValue = minValue;

        return Random.Range(minValue, maxValue);
    }
}

/// <summary>
/// ScriptableObject configuration for chest/body armor pieces.
/// Defines visuals, animations, and stat modifiers.
/// </summary>
[CreateAssetMenu(fileName = "NewArmorConfig", menuName = "Armor/Armor Config")]
public class ArmorConfig : ScriptableObject
{
    [FormerlySerializedAs("baseTierAvailable")]
    [Tooltip("Advancement level for this armor (1-6). Used as the minimum rolled gear tier.")]
    [Range(1, 6)]
    public int advancementLevel = 1;

    [Tooltip("Optional per-item tier scaling config. If empty, progression profile/default scaling is used.")]
    public TierScalingConfig tierScalingConfig;
    
    [Tooltip("Display name of armor")]
    public string gearName = "New Armor";

    [Tooltip("Armor class: Light, Medium, or Heavy")]
    public ArmorClass armorClass = ArmorClass.Light;

    public ArmorSlot armorSlot = ArmorSlot.Chest;

    [Tooltip("Trait granted by this armor")]
    public TraitData grantedTrait;

    [Tooltip("Rarity tier (0-5)")]
    [Range(0, 5)]
    public int rarityTier = 0;

    [Tooltip("The chest gear prefab to instantiate")]
    public GameObject chestGearPrefab;

    [Tooltip("The head gear prefab to instantiate")]
    public GameObject headGearPrefab;

    [Tooltip("The leg gear prefab to instantiate")]
    public GameObject legGearPrefab;

    [Tooltip("The hands gear prefab to instantiate")]
    public GameObject handsGearPrefab;

    [Tooltip("The backpack gear prefab to instantiate")]
    public GameObject backpackGearPrefab;

    [Tooltip("Animator Override Controller for this chest gear's animations")]
    public AnimatorOverrideController animatorOverride;

    [Tooltip("Sprite for inventory/UI")]
    public Sprite inventorySprite;

    [Tooltip("Sprite for world item drop")]
    public Sprite worldSprite;

    // treeSprite, treeSpriteColorTag, craftingCost, and researchPointCost are inherited from CraftableConfig

    [Header("Base Stats")]

    [Tooltip("Base stat ranges for this armor. A value is rolled between min and max for each stat when generated.")]
    public List<StatModifierRange> baseStatRanges = new List<StatModifierRange>();

    [FormerlySerializedAs("baseStats")]
    [SerializeField, HideInInspector]
    private List<StatModifier> legacyBaseStats = new List<StatModifier>();

    [FormerlySerializedAs("baseDefensiveStats")]
    [SerializeField, HideInInspector]
    private List<DefensiveStatRange> legacyBaseDefensiveStats = new List<DefensiveStatRange>();

    [Tooltip("Additional stat modifiers (e.g., +10 Strength, +5% Fire Resistance)")]
    public List<StatModifier> modifiers = new List<StatModifier>();

    [Tooltip("Movement speed modifier (e.g., -0.1 for 10% slower)")]
    [Range(-0.5f, 0.5f)]
    public float movementSpeedModifier = 0f;
    
    /// <summary>
    /// Convert configured base stats to flat stat modifiers.
    /// Falls back to legacy defensive ranges if needed.
    /// </summary>
    public List<StatModifier> RollBaseStats()
    {
        List<StatModifier> result = new List<StatModifier>();

        if (baseStatRanges != null)
        {
            for (int i = 0; i < baseStatRanges.Count; i++)
            {
                StatModifierRange statRange = baseStatRanges[i];
                if (statRange == null || string.IsNullOrWhiteSpace(statRange.statID))
                    continue;

                float rolledValue = statRange.RollValue();
                if (Mathf.Approximately(rolledValue, 0f))
                    continue;

                result.Add(new StatModifier
                {
                    statID = statRange.statID,
                    value = rolledValue,
                    modifierType = statRange.modifierType
                });
            }

            if (result.Count > 0)
                return result;
        }

        if (legacyBaseStats != null && legacyBaseStats.Count > 0)
        {
            for (int i = 0; i < legacyBaseStats.Count; i++)
            {
                StatModifier statValue = legacyBaseStats[i];
                if (statValue == null || string.IsNullOrWhiteSpace(statValue.statID))
                    continue;

                if (Mathf.Approximately(statValue.value, 0f))
                    continue;

                result.Add(new StatModifier
                {
                    statID = statValue.statID,
                    value = statValue.value,
                    modifierType = statValue.modifierType
                });
            }

            if (result.Count > 0)
                return result;
        }

        if (legacyBaseDefensiveStats != null && legacyBaseDefensiveStats.Count > 0)
        {
            for (int i = 0; i < legacyBaseDefensiveStats.Count; i++)
            {
                DefensiveStatRange defensiveStat = legacyBaseDefensiveStats[i];
                if (defensiveStat == null)
                    continue;

                float rolledValue = defensiveStat.RollValue();
                if (Mathf.Approximately(rolledValue, 0f))
                    continue;

                result.Add(new StatModifier
                {
                    statID = defensiveStat.GetStatID(),
                    value = rolledValue,
                    modifierType = ModifierType.Flat
                });
            }
        }

        return result;
    }

    public List<StatModifier> RollDefensiveStats()
    {
        return RollBaseStats();
    }

    public List<StatModifier> GetLegacyBaseStats()
    {
        return legacyBaseStats;
    }
}

public enum ArmorClass
{
    Light,
    Medium,
    Heavy
}

public enum ArmorSlot
{
    Head,
    Chest,
    Legs,
    Hands,
    Backpack
}

