using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Entry in a crafting-tool drop pool.
/// Each tool has an individual drop weight — higher weight = more likely to be chosen.
/// A weight of 0 disables that entry without removing it from the pool.
/// </summary>
[System.Serializable]
public class ToolDropEntry
{
    [Tooltip("The crafting tool config that can drop.")]
    public ToolItemConfig toolConfig;

    [Tooltip("Relative drop weight. Higher values appear more often. 0 = disabled.")]
    [Min(0f)]
    public float weight = 1f;
}

/// <summary>
/// ScriptableObject drop config for crafting tools.
/// Place one asset at Assets/Resources/ToolDropsConfig.asset so it can be accessed
/// as a singleton, or create named variants (e.g. ToolDropsConfig_Common) for
/// different loot contexts and reference them directly on chests/enemies.
///
/// Implements <see cref="ItemConfig.GenerateItem"/> so this asset can be dropped
/// directly into any UniversalDropTable or ChestDropTable entry just like any other
/// ItemConfig.
/// </summary>
[CreateAssetMenu(fileName = "ToolDropsConfig", menuName = "Items/Tool Drops Config")]
public class ToolDropsConfig : ItemConfig
{
    [Header("Tool Pool")]
    [Tooltip("All tools that can drop from this config, each with an individual weight.")]
    public List<ToolDropEntry> toolPool = new List<ToolDropEntry>();

    // ── Singleton ─────────────────────────────────────────────────────────

    private static ToolDropsConfig _instance;
    /// <summary>
    /// Default singleton loaded from Resources/ToolDropsConfig.asset.
    /// Use this for a global pool; reference specific assets directly for
    /// per-chest or per-enemy pools.
    /// </summary>
    public static ToolDropsConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ToolDropsConfig>("ToolDropsConfig");
                if (_instance == null)
                    Debug.LogWarning("[ToolDropsConfig] No ToolDropsConfig found at Resources/ToolDropsConfig.asset. " +
                                     "Create one via Assets > Create > Items > Tool Drops Config.");
            }
            return _instance;
        }
    }

    // ── Item generation ───────────────────────────────────────────────────

    /// <summary>
    /// Picks a weighted-random tool from the pool and returns its ItemInstance.
    /// Returns null if the pool is empty or all weights are zero.
    /// </summary>
    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        ToolItemConfig selected = PickRandomTool();
        if (selected == null)
        {
            Debug.LogWarning($"[ToolDropsConfig] toolPool is empty or all weights are zero on '{name}'.");
            return null;
        }

        return selected.GenerateItem(contextLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Weighted-random pick from the pool. Entries with weight 0 or a null config
    /// are skipped. Returns null if no valid entry exists.
    /// </summary>
    public ToolItemConfig PickRandomTool()
    {
        float totalWeight = 0f;
        foreach (ToolDropEntry entry in toolPool)
        {
            if (entry.toolConfig != null && entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (ToolDropEntry entry in toolPool)
        {
            if (entry.toolConfig == null || entry.weight <= 0f)
                continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.toolConfig;
        }

        return null;
    }
}
