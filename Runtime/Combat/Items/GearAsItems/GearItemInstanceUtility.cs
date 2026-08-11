using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility helpers for reading and mutating gear-specific payload data stored in
/// ItemInstance.additionalData (GearItemData / WeaponGearData / ArmorGearData).
///
/// ItemInstance intentionally stays generic; gear-only operations should route
/// through this helper to avoid leaking gear assumptions into the core item model.
/// </summary>
public static class GearItemInstanceUtility
{
    public static bool IsGearItem(ItemInstance item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.itemType))
            return false;

        string type = item.itemType.Trim().ToLowerInvariant();
        return type == "weapon" || type == "armor";
    }

    public static bool TryGetGearData(ItemInstance item, out GearItemData data)
    {
        data = null;

        if (!IsGearItem(item) || string.IsNullOrWhiteSpace(item.additionalData))
            return false;

        try
        {
            string type = item.itemType.Trim().ToLowerInvariant();
            switch (type)
            {
                case "weapon":
                    data = JsonUtility.FromJson<WeaponGearData>(item.additionalData);
                    break;
                case "armor":
                    data = JsonUtility.FromJson<ArmorGearData>(item.additionalData);
                    break;
                default:
                    data = JsonUtility.FromJson<GearItemData>(item.additionalData);
                    break;
            }

            if (data == null)
                return false;

            if (data.modifiers == null)
                data.modifiers = new List<StatModifier>();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void WriteGearData(ItemInstance item, GearItemData data)
    {
        if (item == null || data == null)
            return;

        if (data.modifiers == null)
            data.modifiers = new List<StatModifier>();

        item.additionalData = JsonUtility.ToJson(data);
    }

    public static int GetModifierCount(ItemInstance item)
    {
        return TryGetGearData(item, out GearItemData data) ? data.modifiers.Count : 0;
    }

    public static int GetMaxModifierCountForRarity(int rarityTier)
    {
        GearModifierDatabase db = GearModifierDatabase.Instance;
        if (db == null || db.maxModifiersPerRarity == null || db.maxModifiersPerRarity.Length == 0)
            return 0;

        int clampedTier = Mathf.Clamp(rarityTier, 0, db.maxModifiersPerRarity.Length - 1);
        return Mathf.Max(0, db.maxModifiersPerRarity[clampedTier]);
    }

    public static int GetRemainingModifierCapacity(ItemInstance item)
    {
        if (item == null)
            return 0;

        return Mathf.Max(0, GetMaxModifierCountForRarity(item.rarityTier) - GetModifierCount(item));
    }

    public static bool CanAddModifier(ItemInstance item)
    {
        return IsGearItem(item) && GetRemainingModifierCapacity(item) > 0;
    }

    public static bool CanRemoveModifier(ItemInstance item)
    {
        return IsGearItem(item) && GetModifierCount(item) > 0;
    }

    public static int AddRolledModifiers(ItemInstance item, int count)
    {
        if (item == null || count <= 0)
            return 0;

        if (!TryGetGearData(item, out GearItemData data))
            return 0;

        int remaining = GetRemainingModifierCapacity(item);
        if (remaining <= 0)
            return 0;

        int toAdd = Mathf.Min(count, remaining);
        GearModifierDatabase db = GearModifierDatabase.Instance;
        if (db == null)
            return 0;

        int added = 0;
        for (int i = 0; i < toAdd; i++)
        {
            GearRollResult roll = db.RollGear(item.displayName, data.gearSlot, item.rarityTier, data.itemTier);
            if (roll == null || roll.modifiers == null || roll.modifiers.Count == 0)
                continue;

            // RollGear can return multiple StatModifiers; for "add one modifier" behavior,
            // append just one generated stat modifier per requested count.
            StatModifier source = roll.modifiers[Random.Range(0, roll.modifiers.Count)];
            if (source == null)
                continue;

            data.modifiers.Add(new StatModifier
            {
                statID = source.statID,
                modifierType = source.modifierType,
                value = source.value
            });
            added++;
        }

        if (added > 0)
            WriteGearData(item, data);

        return added;
    }

    public static int RemoveModifiers(ItemInstance item, int count)
    {
        if (item == null || count <= 0)
            return 0;

        if (!TryGetGearData(item, out GearItemData data) || data.modifiers.Count == 0)
            return 0;

        int removed = 0;
        int toRemove = Mathf.Min(count, data.modifiers.Count);
        for (int i = 0; i < toRemove; i++)
        {
            int idx = data.modifiers.Count - 1;
            data.modifiers.RemoveAt(idx);
            removed++;
        }

        if (removed > 0)
            WriteGearData(item, data);

        return removed;
    }
}
