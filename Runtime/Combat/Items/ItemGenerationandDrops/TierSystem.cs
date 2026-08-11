using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tier-based drop system that filters item drops based on map tier.
/// Allows configuration of tier-specific drop tables for weapons, armor, and other items.
/// </summary>
[CreateAssetMenu(fileName = "TierSystem", menuName = "Items/Tier System")]
public class TierSystem : ScriptableObject
{
    [Header("Current Map Tier")]
    [Tooltip("The current map's tier level (set at runtime)")]
    public ItemTier currentMapTier = ItemTier.I;
    
    [Header("Tier-Based Drop Tables")]
    [Tooltip("Drop tables organized by tier. Only items matching the current map tier will be eligible for drops.")]
    public List<TierDropTableEntry> tierDropTables = new List<TierDropTableEntry>();
    
    /// <summary>
    /// Set the current map tier (call this when entering a map/arena)
    /// </summary>
    public void SetMapTier(ItemTier tier)
    {
        currentMapTier = tier;
        Debug.Log($"[TierSystem] Map tier set to: {tier}");
    }
    
    /// <summary>
    /// Get all drop tables valid for the current map tier
    /// </summary>
    public List<UniversalDropTable> GetValidDropTablesForCurrentTier()
    {
        return tierDropTables
            .Where(t => t.tier == currentMapTier)
            .SelectMany(t => t.dropTables)
            .Where(dt => dt != null)
            .ToList();
    }
    
    /// <summary>
    /// Check if an item config is valid for the current map tier
    /// </summary>
    public bool IsItemValidForCurrentTier(ItemConfig itemConfig)
    {
        if (itemConfig == null)
            return false;
            
        return (int)itemConfig.baseTierAvailable <= (int)currentMapTier;
    }
    
    /// <summary>
    /// Filter a list of drop table entries to only include items valid for current tier
    /// </summary>
    public List<DropTableEntry> FilterDropsByTier(List<DropTableEntry> entries)
    {
        return entries.Where(entry => IsItemValidForCurrentTier(entry.itemConfig)).ToList();
    }
    
    // Singleton access
    private static TierSystem instance;
    public static TierSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<TierSystem>("TierSystem");
                if (instance == null)
                {
                    Debug.LogWarning("[TierSystem] No TierSystem found in Resources folder. Create one at Assets\\Resources\\TierSystem.asset");
                }
            }
            return instance;
        }
    }
}

/// <summary>
/// Drop table entry for a specific tier
/// </summary>
[System.Serializable]
public class TierDropTableEntry
{
    [Tooltip("The tier level for these drop tables")]
    public ItemTier tier = ItemTier.I;
    
    [Tooltip("Drop tables active for this tier")]
    public List<UniversalDropTable> dropTables = new List<UniversalDropTable>();
}
