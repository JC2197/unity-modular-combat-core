using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared stack-aware slot operations used by inventory and storage.
/// This keeps slot containers focused on capacity/indexing while item-level
/// stack rules live on ItemInstance.
/// </summary>
public static class ItemSlotStackingUtility
{
    public static int FindFirstEmptySlot(IDictionary<int, ItemInstance> slots, int maxSlots)
    {
        if (slots == null || maxSlots <= 0)
            return -1;

        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots.ContainsKey(i) || slots[i] == null)
                return i;
        }

        return -1;
    }

    public static bool AddItemToSlots(IDictionary<int, ItemInstance> slots, int maxSlots, ItemInstance item)
    {
        if (slots == null || item == null)
            return false;

        int remaining = Mathf.Max(1, item.stackSize);

        if (item.IsStackable())
        {
            foreach (KeyValuePair<int, ItemInstance> kvp in slots)
            {
                ItemInstance existing = kvp.Value;
                if (existing == null)
                    continue;

                remaining -= existing.MergeFrom(item);
                if (remaining <= 0)
                    return true;
            }
        }

        while (remaining > 0)
        {
            int emptySlot = FindFirstEmptySlot(slots, maxSlots);
            if (emptySlot < 0)
                return false;

            int stackSize = item.IsStackable()
                ? Mathf.Min(remaining, item.GetMaxStackSize())
                : 1;

            slots[emptySlot] = item.CreateStackCopy(stackSize);
            remaining -= stackSize;
        }

        item.stackSize = Mathf.Max(0, remaining);
        return true;
    }

    public static bool CanStoreItem(IDictionary<int, ItemInstance> slots, int maxSlots, ItemInstance item)
    {
        if (slots == null || item == null)
            return false;

        int remaining = Mathf.Max(1, item.stackSize);

        if (item.IsStackable())
        {
            foreach (KeyValuePair<int, ItemInstance> kvp in slots)
            {
                ItemInstance existing = kvp.Value;
                if (existing == null || !existing.CanStackWith(item))
                    continue;

                remaining -= existing.GetAvailableStackSpace();
                if (remaining <= 0)
                    return true;
            }
        }

        int emptySlots = 0;
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots.ContainsKey(i) || slots[i] == null)
                emptySlots++;
        }

        remaining -= emptySlots * item.GetMaxStackSize();
        return remaining <= 0;
    }

    public static bool MoveOrMergeItem(IDictionary<int, ItemInstance> slots, int sourceIndex, int targetIndex)
    {
        if (slots == null || sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            return false;

        if (!slots.TryGetValue(sourceIndex, out ItemInstance sourceItem) || sourceItem == null)
            return false;

        slots.TryGetValue(targetIndex, out ItemInstance targetItem);

        if (targetItem != null && targetItem.MergeFrom(sourceItem) > 0)
        {
            if (sourceItem.stackSize <= 0)
                slots.Remove(sourceIndex);

            return true;
        }

        if (targetItem != null)
        {
            slots[targetIndex] = sourceItem;
            slots[sourceIndex] = targetItem;
            return true;
        }

        slots[targetIndex] = sourceItem;
        slots.Remove(sourceIndex);
        return true;
    }

    public static bool SplitOneToSlot(IDictionary<int, ItemInstance> slots, int sourceIndex, int targetIndex)
    {
        if (slots == null || sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
            return false;

        if (!slots.TryGetValue(sourceIndex, out ItemInstance sourceItem) || sourceItem == null || sourceItem.stackSize <= 1)
            return false;

        slots.TryGetValue(targetIndex, out ItemInstance targetItem);

        if (targetItem != null)
        {
            if (!targetItem.CanStackWith(sourceItem) || targetItem.GetAvailableStackSpace() <= 0)
                return false;

            targetItem.stackSize++;
        }
        else
        {
            slots[targetIndex] = sourceItem.CreateStackCopy(1);
        }

        sourceItem.stackSize--;
        if (sourceItem.stackSize <= 0)
            slots.Remove(sourceIndex);

        return true;
    }
}