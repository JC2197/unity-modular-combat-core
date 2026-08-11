using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Container for enemy stats, organized by category.
/// Duplicate of StatContainer but for enemies to keep them separate from player stats.
/// </summary>
[System.Serializable]
public class EnemyStatContainer
{
    [Header("Stat Collections")]
    [SerializeField] private List<EnemyStatValue> baseStats = new List<EnemyStatValue>();
    [SerializeField] private List<EnemyStatValue> offensiveStats = new List<EnemyStatValue>();
    [SerializeField] private List<EnemyStatValue> defensiveStats = new List<EnemyStatValue>();
    [SerializeField] private List<EnemyStatValue> specialStats = new List<EnemyStatValue>();
    
    private Dictionary<string, EnemyStatValue> statLookup;
    
    /// <summary>
    /// Initialize the stat container from the StatTypeDatabase
    /// </summary>
    public void InitializeFromDatabase()
    {
        StatTypeDatabase database = StatTypeDatabase.Instance;
        if (database == null)
        {
            Debug.LogError("[EnemyStatContainer] StatTypeDatabase not found!");
            return;
        }
        
        baseStats.Clear();
        offensiveStats.Clear();
        defensiveStats.Clear();
        specialStats.Clear();
        
        foreach (var statType in database.statTypes)
        {
            EnemyStatValue stat = new EnemyStatValue(statType.statID, statType.displayName, 0f);
            
            switch (statType.category)
            {
                case StatCategory.Base:
                    baseStats.Add(stat);
                    break;
                case StatCategory.Offensive:
                    offensiveStats.Add(stat);
                    break;
                case StatCategory.Defensive:
                    defensiveStats.Add(stat);
                    break;
                case StatCategory.Special:
                    specialStats.Add(stat);
                    break;
            }
        }
        
        RebuildLookup();
    }
    
    /// <summary>
    /// Rebuild the lookup dictionary for fast stat access
    /// </summary>
    private void RebuildLookup()
    {
        statLookup = new Dictionary<string, EnemyStatValue>();
        
        foreach (var stat in baseStats)
            statLookup[stat.statID.ToLower()] = stat;
        foreach (var stat in offensiveStats)
            statLookup[stat.statID.ToLower()] = stat;
        foreach (var stat in defensiveStats)
            statLookup[stat.statID.ToLower()] = stat;
        foreach (var stat in specialStats)
            statLookup[stat.statID.ToLower()] = stat;
    }
    
    /// <summary>
    /// Get a stat value by ID
    /// </summary>
    public float GetStat(string statID)
    {
        if (statLookup == null)
            RebuildLookup();
        
        string key = statID.ToLower();
        if (statLookup.TryGetValue(key, out EnemyStatValue stat))
        {
            return stat.currentValue;
        }
        
        Debug.LogWarning($"[EnemyStatContainer] Stat '{statID}' not found!");
        return 0f;
    }
    
    /// <summary>
    /// Set a stat value by ID
    /// </summary>
    public void SetStat(string statID, float value)
    {
        if (statLookup == null)
            RebuildLookup();
        
        string key = statID.ToLower();
        if (statLookup.TryGetValue(key, out EnemyStatValue stat))
        {
            stat.currentValue = value;
        }
    }
    
    /// <summary>
    /// Add to a stat value by ID
    /// </summary>
    public void ModifyStat(string statID, float amount)
    {
        if (statLookup == null)
            RebuildLookup();
        
        string key = statID.ToLower();
        if (statLookup.TryGetValue(key, out EnemyStatValue stat))
        {
            stat.currentValue += amount;
        }
        else
        {
            Debug.LogWarning($"[EnemyStatContainer] Stat '{statID}' not found!");
        }
    }
    
    /// <summary>
    /// Check if a stat exists
    /// </summary>
    public bool HasStat(string statID)
    {
        if (statLookup == null)
            RebuildLookup();
        
        return statLookup.ContainsKey(statID.ToLower());
    }
    
    /// <summary>
    /// Get all stats in a specific category
    /// </summary>
    public List<EnemyStatValue> GetStatsByCategory(StatCategory category)
    {
        switch (category)
        {
            case StatCategory.Base:
                return new List<EnemyStatValue>(baseStats);
            case StatCategory.Offensive:
                return new List<EnemyStatValue>(offensiveStats);
            case StatCategory.Defensive:
                return new List<EnemyStatValue>(defensiveStats);
            case StatCategory.Special:
                return new List<EnemyStatValue>(specialStats);
            default:
                return new List<EnemyStatValue>();
        }
    }
    
    /// <summary>
    /// Get all stats as a flat list
    /// </summary>
    public List<EnemyStatValue> GetAllStats()
    {
        List<EnemyStatValue> allStats = new List<EnemyStatValue>();
        allStats.AddRange(baseStats);
        allStats.AddRange(offensiveStats);
        allStats.AddRange(defensiveStats);
        allStats.AddRange(specialStats);
        return allStats;
    }
    
    /// <summary>
    /// Copy all stat values to a runtime StatContainer (for enemies)
    /// </summary>
    public void CopyToStatContainer(StatContainer destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("[EnemyStatContainer] Cannot copy to null StatContainer!");
            return;
        }
        
        // Initialize destination from database first to have all stat structures
        destination.InitializeFromDatabase();
        
        // Copy values from this enemy stat container
        foreach (var stat in GetAllStats())
        {
            destination.SetStat(stat.statID, stat.currentValue);
        }
    }
}

/// <summary>
/// Represents a single enemy stat with its ID, name, and current value
/// </summary>
[System.Serializable]
public class EnemyStatValue
{
    public string statID;
    public string displayName;
    public float currentValue;
    
    public EnemyStatValue(string id, string name, float value)
    {
        statID = id;
        displayName = name;
        currentValue = value;
    }
}
