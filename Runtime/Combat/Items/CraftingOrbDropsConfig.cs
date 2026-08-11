using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Entry in the crafting-orb drop pool. All orbs share equal drop probability.
/// </summary>
[System.Serializable]
public class CraftingOrbDropEntry
{
    [Tooltip("The crafting orb config that can drop.")]
    public OrbItemConfig orbConfig;
}

/// <summary>
/// Top-level ScriptableObject drop config for crafting orbs.
/// Place one asset at Assets/Resources/CraftingOrbDropsConfig.asset so
/// LootRewarder/UniversalDropTable can reference it via the singleton.
///
/// Add this asset's single DropTableEntry to UniversalDropTable.universalDrops
/// (or an enemy's dropTable) in the Inspector.  When the drop fires,
/// GenerateItem() picks a random orb from the weighted list.
/// </summary>
[CreateAssetMenu(fileName = "CraftingOrbDropsConfig", menuName = "Items/Crafting Orb Drops Config")]
public class CraftingOrbDropsConfig : ItemConfig
{
    [Header("Orb Pool")]
    [Tooltip("All orb types that can drop. Each has an equal chance of being selected.")]
    public List<CraftingOrbDropEntry> orbPool = new List<CraftingOrbDropEntry>();

    // ── Singleton ─────────────────────────────────────────────────────────

    private static CraftingOrbDropsConfig _instance;
    public static CraftingOrbDropsConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<CraftingOrbDropsConfig>("CraftingOrbDropsConfig");
                if (_instance == null)
                    Debug.LogError("[CraftingOrbDropsConfig] No CraftingOrbDropsConfig found in Resources! " +
                                   "Create one at Assets/Resources/CraftingOrbDropsConfig.asset");
            }
            return _instance;
        }
    }

    // ── Item generation ───────────────────────────────────────────────────

    /// <summary>
    /// Picks a random orb from the weighted pool and generates its ItemInstance.
    /// </summary>
    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        OrbItemConfig selected = PickRandomOrb();
        if (selected == null)
        {
            Debug.LogError($"[CraftingOrbDropsConfig] orbPool is empty or all weights are zero on '{name}'!");
            return null;
        }

        return selected.GenerateItem(contextLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Picks a uniformly random orb from orbPool (all orbs have equal probability).
    /// </summary>
    public OrbItemConfig PickRandomOrb()
    {
        // Build a list of valid entries first so null slots are ignored.
        var valid = new System.Collections.Generic.List<OrbItemConfig>();
        foreach (var entry in orbPool)
            if (entry.orbConfig != null) valid.Add(entry.orbConfig);

        if (valid.Count == 0) return null;

        return valid[Random.Range(0, valid.Count)];
    }
}
