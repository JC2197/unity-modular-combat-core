using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A composable tool operation that applies an ordered list of upgrade steps.
/// This is the "operation container" you assign to ToolItemConfig.operations.
///
/// Example:
/// - Step 1: IncreaseRarity (+1)
/// - Step 2: IncreaseRarity (+1)
/// Result: rarity increases by 2 in one craft action.
/// </summary>
[CreateAssetMenu(fileName = "ModularUpgradeOperation", menuName = "Items/Upgrade Operations/Modular Operation")]
public class ModularUpgradeOperation : UpgradeOperation
{
    [Header("Requirements")]
    [Tooltip("Optional whitelist of item types this operation can affect (e.g. weapon, armor). Empty = any type.")]
    public List<string> allowedItemTypes = new List<string> { "weapon", "armor" };

    [Tooltip("If enabled, an orb must be present in the slot for this operation to be valid.")]
    public bool requiresOrb;

    [Header("Upgrades")]
    [Tooltip("Ordered list of upgrade steps applied in sequence.")]
    public List<UpgradeStep> upgrades = new List<UpgradeStep>
    {
        UpgradeStep.CreateIncreaseRarity(1)
    };

    public override bool CanApply(ItemInstance gear, ItemInstance orb)
    {
        if (gear == null)
            return false;

        if (requiresOrb && orb == null)
            return false;

        if (!IsAllowedItemType(gear.itemType))
            return false;

        if (upgrades == null || upgrades.Count == 0)
            return false;

        // Valid only if at least one step would actually change state.
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i].CanApply(gear))
                return true;
        }

        return false;
    }

    public override ItemInstance Apply(ItemInstance gear, ItemInstance orb)
    {
        if (!CanApply(gear, orb))
            return null;

        for (int i = 0; i < upgrades.Count; i++)
            upgrades[i].Apply(gear);

        return gear;
    }

    private bool IsAllowedItemType(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return false;

        if (allowedItemTypes == null || allowedItemTypes.Count == 0)
            return true;

        string normalized = itemType.Trim().ToLowerInvariant();
        for (int i = 0; i < allowedItemTypes.Count; i++)
        {
            string allowed = allowedItemTypes[i];
            if (string.IsNullOrWhiteSpace(allowed))
                continue;

            if (normalized == allowed.Trim().ToLowerInvariant())
                return true;
        }

        return false;
    }
}

/// <summary>
/// Supported upgrade actions in the modular operation pipeline.
/// Extend this enum as new tool behaviour is added.
/// </summary>
public enum UpgradeStepType
{
    IncreaseRarity,
    AddModifier,
    RemoveModifier,
    ReplaceModifier,
    AddTrait,
    RemoveTrait,
    ClearModifiers,
    CopyModifiers,
    CopyTraits,
    AddTraitSlot
}

/// <summary>
/// One upgrade action in a ModularUpgradeOperation.
/// </summary>
[System.Serializable]
public class UpgradeStep
{
    [Tooltip("Which upgrade action to apply.")]
    public UpgradeStepType stepType = UpgradeStepType.IncreaseRarity;

    [Tooltip("Amount used by numeric step types (e.g. IncreaseRarity).")]
    [Min(1)]
    public int amount = 1;

    public static UpgradeStep CreateIncreaseRarity(int increaseBy)
    {
        return new UpgradeStep
        {
            stepType = UpgradeStepType.IncreaseRarity,
            amount = Mathf.Max(1, increaseBy)
        };
    }

    public static UpgradeStep CreateAddModifier(int count)
    {
        return new UpgradeStep
        {
            stepType = UpgradeStepType.AddModifier,
            amount = Mathf.Max(1, count)
        };
    }

    public static UpgradeStep CreateRemoveModifier(int count)
    {
        return new UpgradeStep
        {
            stepType = UpgradeStepType.RemoveModifier,
            amount = Mathf.Max(1, count)
        };
    }

    public bool CanApply(ItemInstance gear)
    {
        if (gear == null)
            return false;

        switch (stepType)
        {
            case UpgradeStepType.IncreaseRarity:
                return gear.rarityTier < GetMaxRarityTier();
            case UpgradeStepType.AddModifier:
                return GearItemInstanceUtility.CanAddModifier(gear);
            case UpgradeStepType.RemoveModifier:
                return GearItemInstanceUtility.CanRemoveModifier(gear);
            default:
                return false;
        }
    }

    public void Apply(ItemInstance gear)
    {
        if (gear == null)
            return;
        
        switch (stepType)
        {
            case UpgradeStepType.IncreaseRarity:
                int maxTier = GetMaxRarityTier();
                int delta = Mathf.Max(1, amount);
                gear.rarityTier = Mathf.Clamp(gear.rarityTier + delta, 0, maxTier);
                break;
            case UpgradeStepType.AddModifier:
                GearItemInstanceUtility.AddRolledModifiers(gear, Mathf.Max(1, amount));
                break;
            case UpgradeStepType.RemoveModifier:
                GearItemInstanceUtility.RemoveModifiers(gear, Mathf.Max(1, amount));
                break;
        }
    }

    
    private static int GetMaxRarityTier()
    {
        // Derive a safe upper bound from whatever arrays are configured.
        RarityConfig cfg = RarityConfig.Instance;
        if (cfg == null)
            return 0;

        int maxTier = 0;
        if (cfg.rarityNames != null)
            maxTier = Mathf.Max(maxTier, cfg.rarityNames.Length - 1);
        if (cfg.rarityColors != null)
            maxTier = Mathf.Max(maxTier, cfg.rarityColors.Length - 1);
        if (cfg.rarityEmission != null)
            maxTier = Mathf.Max(maxTier, cfg.rarityEmission.Length - 1);

        return Mathf.Max(0, maxTier);
    }

}
