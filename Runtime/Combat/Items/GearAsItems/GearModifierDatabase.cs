using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Database of prefixes and suffixes that can roll on gear items.
/// Prefixes add to the start of the name (e.g., "Commander's Sword")
/// Suffixes add to the end of the name (e.g., "Sword of the Bear")
/// </summary>
[CreateAssetMenu(fileName = "GearModifierDatabase", menuName = "Items/Gear Modifier Database")]
public class GearModifierDatabase : ScriptableObject
{
    [Header("Modifier Pool")]
    [Tooltip("All possible modifiers that can roll on gear")]
    public List<GearModifier> modifiers = new List<GearModifier>();

    [Header("Roll Configuration")]
    [Tooltip("Guaranteed number of modifiers per rarity tier [Common, Uncommon, Rare, Epic, Legendary, Mythic]")]
    public int[] maxModifiersPerRarity = new int[] { 1, 2, 3, 4, 5, 6 };

    [Tooltip("Value multiplier per rarity tier (higher rarity = stronger mods)")]
    public float[] rarityValueMultiplier = new float[] { 1.0f, 1.2f, 1.5f, 2.0f, 3.0f, 5.0f };

    // Singleton access
    private static GearModifierDatabase instance;
    public static GearModifierDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GearModifierDatabase>("GearModifierDatabase");
                if (instance == null)
                {
                    Debug.LogWarning("[GearModifierDatabase] No database found in Resources folder! Create one at Assets\\Resources\\GearModifierDatabase.asset");
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Rolls modifiers for a gear piece, returns the modifiers
    /// </summary>
    public GearRollResult RollGear(string baseGearName, GearSlot slot, int rarityTier, ItemTier gearTier)
    {
        GearRollResult result = new GearRollResult
        {
            displayName = baseGearName,
            modifiers = new List<StatModifier>(),
            rolledTier = ItemTier.I
        };

        // Check if database is empty
        if (modifiers == null || modifiers.Count == 0)
        {
            Debug.LogWarning($"[GearModifierDatabase] Cannot roll modifiers - database is empty! Use 'Import All Gear Modifiers' button in the inspector.");
            return result;
        }

        float valueMultiplier = rarityTier < rarityValueMultiplier.Length ? rarityValueMultiplier[rarityTier] : 1.0f;
        int targetModifiers = rarityTier < maxModifiersPerRarity.Length ? maxModifiersPerRarity[rarityTier] : 1;

        // Roll exactly the target number of modifiers for this rarity
        List<GearModifier> rolledModifiers = new List<GearModifier>();

        for (int i = 0; i < targetModifiers; i++)
        {
            GearModifier modifier = RollModifier(slot, rolledModifiers);
            if (modifier != null)
            {
                rolledModifiers.Add(modifier);
                ItemTier rolledTier = RollModifierTier(modifier);
                result.rolledTier = (ItemTier)Mathf.Max((int)result.rolledTier, (int)rolledTier);
                result.modifiers.AddRange(GenerateTieredModifiers(modifier, rolledTier, valueMultiplier));
            }
            else
            {
                // Could not find a valid modifier for this slot/tier combination
                Debug.LogWarning($"[GearModifierDatabase] Could not roll modifier {i + 1}/{targetModifiers} for {baseGearName} (Slot: {slot}, GearTier: {gearTier}). " +
                               $"No more compatible modifiers available. Rolled {rolledModifiers.Count} total.");
                break;
            }
        }

        // Debug logging if no modifiers rolled
        if (result.modifiers.Count == 0)
        {
            Debug.LogWarning($"[GearModifierDatabase] No modifiers rolled for {baseGearName} (Rarity: {rarityTier}, Slot: {slot}, GearTier: {gearTier}). " +
                           $"Target: {targetModifiers}. Check if modifiers are available for this slot/tier combination.");
        }

        return result;
    }

    /// <summary>
    /// Roll a random modifier for the given slot.
    /// Map tier is intentionally ignored for availability filtering.
    /// </summary>
    private GearModifier RollModifier(GearSlot slot, List<GearModifier> alreadyRolled)
    {
        List<GearModifier> available = new List<GearModifier>();

        foreach (var modifier in modifiers)
        {
            if (modifier == null) continue;

            // Don't roll the same modifier twice
            if (alreadyRolled.Contains(modifier)) continue;

            // Check slot compatibility only. Tier is now rolled per modifier scaler.
            if (modifier.IsValidForSlot(slot))
            {
                available.Add(modifier);
            }
        }

        if (available.Count == 0)
        {
            Debug.LogWarning($"[GearModifierDatabase] No available modifiers for Slot: {slot}. Already rolled: {alreadyRolled.Count}");
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }

    /// <summary>
    /// Generate stat modifiers from tiered modifiers based on current map tier
    /// </summary>
    private List<StatModifier> GenerateTieredModifiers(GearModifier sourceModifier, ItemTier rolledTier, float rarityMultiplier)
    {
        List<StatModifier> result = new List<StatModifier>();
        if (sourceModifier == null || sourceModifier.modifiers == null)
            return result;

        // Each GearModifier can define its own tier scaling curve.
        // Fall back to the default TierScaling config when unassigned.
        TierScalingConfig tierConfig = sourceModifier.tierScalingConfig;

        foreach (var tieredMod in sourceModifier.modifiers)
        {
            ValueRange scaledRange = TierScaler.ScaleRange(tieredMod.baseRange, rolledTier, tierConfig);
            float randomValue = Random.Range(scaledRange.min, scaledRange.max) * rarityMultiplier;
            randomValue = Mathf.Max(1f, Mathf.Floor(randomValue));
            result.Add(new StatModifier
            {
                statID = tieredMod.statID,
                modifierType = tieredMod.modifierType,
                value = randomValue
            });
        }
        return result;
    }

    private static ItemTier RollModifierTier(GearModifier sourceModifier)
    {
        if (sourceModifier == null)
            return ItemTier.I;

        ItemTier rolledTier = TierScaler.RollTier(sourceModifier.tierScalingConfig);
        if ((int)rolledTier < (int)sourceModifier.baseTierAvailable)
            rolledTier = sourceModifier.baseTierAvailable;

        return rolledTier;
    }

    /// <summary>
    /// Scale modifiers by rarity multiplier (legacy support)
    /// </summary>
    private List<StatModifier> ScaleModifiers(List<StatModifier> modifiers, float multiplier)
    {
        List<StatModifier> scaled = new List<StatModifier>();

        foreach (var mod in modifiers)
        {
            scaled.Add(new StatModifier
            {
                statID = mod.statID,
                modifierType = mod.modifierType,
                value = mod.value * multiplier
            });
        }

        return scaled;
    }
}

/// <summary>
/// Result of rolling gear with modifiers
/// </summary>
[System.Serializable]
public class GearRollResult
{
    public string displayName;
    public List<StatModifier> modifiers;
    public ItemTier rolledTier = ItemTier.I;
}

/// <summary>
/// Value range with min and max
/// </summary>
[System.Serializable]
public struct ValueRange
{
    public float min;
    public float max;

    public ValueRange(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    public ValueRange MultiplyBy(float multiplier)
    {
        return new ValueRange(min * multiplier, max * multiplier);
    }

    public ValueRange MultiplyByAndFloor(float multiplier)
    {
        return new ValueRange(Mathf.Floor(min * multiplier), Mathf.Floor(max * multiplier));
    }
}

/// <summary>
/// Stat modifier with value ranges for each tier
/// </summary>
[System.Serializable]
public class TieredStatModifier
{
    [Tooltip("Stat identifier")]
    public string statID = "health";

    [Tooltip("Type of modification (Flat or Percentage)")]
    public ModifierType modifierType = ModifierType.Flat;

    [Tooltip("Value range for Tier I (base tier)")]
    public ValueRange baseRange = new ValueRange(1f, 3f);

    
}
