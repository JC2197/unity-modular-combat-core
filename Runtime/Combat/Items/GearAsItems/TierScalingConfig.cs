using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tier scaling configuration for stat values.
/// Defines multipliers for each item tier based on Tier I values.
/// </summary>
[CreateAssetMenu(fileName = "New Tier Scaling Config", menuName = "Items/Tier Scaling Config")]
public class TierScalingConfig : ScriptableObject
{

    [Tooltip("Scaling and roll weight configuration for each tier")]
    public List<TierScalingEntry> tierScalingEntries = new List<TierScalingEntry>()
    {
        new TierScalingEntry { tier = ItemTier.I, multiplier = 1.0f, rollWeight = 35f },
        new TierScalingEntry { tier = ItemTier.II, multiplier = 1.5f, rollWeight = 25f },
        new TierScalingEntry { tier = ItemTier.III, multiplier = 2.5f, rollWeight = 12f },
        new TierScalingEntry { tier = ItemTier.IV, multiplier = 4.0f, rollWeight = 8f },
        new TierScalingEntry { tier = ItemTier.V, multiplier = 6.0f, rollWeight = 4f },
        new TierScalingEntry { tier = ItemTier.VI, multiplier = 10.0f, rollWeight = 1f }
    };

    [System.Serializable]
    public class TierScalingEntry
    {
        public ItemTier tier;
        
        [Tooltip("Multiplier applied to base values at this tier")]
        public float multiplier = 1.0f;
        
        [Tooltip("Relative weight for rolling this tier (higher = more common)")]
        [Min(0f)]
        public float rollWeight = 100f;
    }

    private Dictionary<ItemTier, TierScalingEntry> entryLookup;


    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }
    private void RebuildLookup()
    {
        entryLookup = new Dictionary<ItemTier, TierScalingEntry>();
        foreach (var entry in tierScalingEntries)
        {
            if (!entryLookup.ContainsKey(entry.tier))
            {
                entryLookup[entry.tier] = entry;
            }
        }
    }
    public float GetMultiplier(ItemTier tier)
    {
        if (entryLookup != null && entryLookup.TryGetValue(tier, out var entry))
        {
            return entry.multiplier;
        }
        return 1.0f;
    }

    public float GetRollWeight(ItemTier tier)
    {
        if (entryLookup != null && entryLookup.TryGetValue(tier, out var entry))
        {
            return entry.rollWeight;
        }
        return 0f;
    }

    public float GetTotalRollWeight()
    {
        float total = 0f;
        foreach (var entry in tierScalingEntries)
        {
            total += entry.rollWeight;
        }
        return total;
    }
}
