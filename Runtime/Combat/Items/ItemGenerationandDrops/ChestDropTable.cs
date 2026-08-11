using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drop table ScriptableObject used by Chest interactables.
/// Configure entries (items + chances) and a min/max drop count
/// in the Inspector, then assign the asset to a Chest component.
/// </summary>
[CreateAssetMenu(fileName = "ChestDropTable", menuName = "Items/Chest Drop Table")]
public class ChestDropTable : ScriptableObject
{
    [Header("Drop Entries")]
    [Tooltip("Items that can drop from this chest and their individual chances.")]
    public List<DropTableEntry> drops = new List<DropTableEntry>();

    [Header("Drop Count")]
    [Tooltip("Minimum number of items the chest will award.")]
    [Min(0)]
    public int minDrops = 1;

    [Tooltip("Maximum number of items the chest will award.")]
    [Min(1)]
    public int maxDrops = 3;

    /// <summary>
    /// Rolls the drop table and returns a list of generated ItemInstances.
    /// The list length is clamped between minDrops and maxDrops.
    /// </summary>
    public List<ItemInstance> RollDrops()
    {
        List<ItemInstance> results = new List<ItemInstance>();
        int target = Random.Range(minDrops, maxDrops + 1);

        // First pass: roll each entry against its drop chance
        var eligible = new List<DropTableEntry>();
        foreach (var entry in drops)
        {
            if (results.Count >= target) break;
            if (entry.itemConfig == null) continue;

            if (Random.value <= entry.dropChance)
            {
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                ItemInstance item = ItemGenerator.GenerateFromConfig(entry.itemConfig, 1);
                if (item != null)
                {
                    item.stackSize = quantity;
                    results.Add(item);
                    Debug.Log($"[ChestDropTable] Rolled {item.displayName} x{quantity}");
                }
            }
            else
            {
                eligible.Add(entry);
            }
        }

        // Second pass: if we still haven't hit minDrops, force-pick from entries that
        // failed their chance check (shuffle and pick until we reach the minimum)
        if (results.Count < minDrops && eligible.Count > 0)
        {
            // Fisher-Yates shuffle
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
            }

            foreach (var entry in eligible)
            {
                if (results.Count >= minDrops) break;
                if (entry.itemConfig == null) continue;

                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                ItemInstance item = ItemGenerator.GenerateFromConfig(entry.itemConfig, 1);
                if (item != null)
                {
                    item.stackSize = quantity;
                    results.Add(item);
                    Debug.Log($"[ChestDropTable] Guaranteed roll {item.displayName} x{quantity}");
                }
            }
        }

        return results;
    }
}
