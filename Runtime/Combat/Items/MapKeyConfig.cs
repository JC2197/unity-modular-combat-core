using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configuration for procedurally generating map key items.
/// Holds settings for level ranges and available arenas.
/// </summary>
[CreateAssetMenu(fileName = "MapKeyConfig", menuName = "Items/Map Key Config")]
public class MapKeyConfig : ItemConfig
{    [Header("Sprites")]
    [Tooltip("Sprite shown in inventory")]
    public Sprite inventorySprite;
    
    [Tooltip("Sprite shown on ground")]
    public Sprite worldSprite;
    
    [Header("Particle System Override")]
    [Tooltip("Optional custom particle system (ignores rarity colors/emission if set)")]
    public ParticleSystem particleSystemOverride;
        [Header("Level Configuration")]
    [Tooltip("Maximum level a dropped map can be")]
    public int maxLevel = 10;
    
    [Tooltip("Level range modifier (e.g., 1 means you can get ±1 level from current map level)")]
    public int levelRange = 1;
    
    [Header("Arena Scenes")]
    [Tooltip("List of arena scene names that can be randomly selected for drops")]
    public List<string> arenaSceneNames = new List<string>()
    {
        "Forest Arena",
        "Desert Arena",
        "Ice Arena",
        "Volcano Arena",
        "Shadow Arena"
    };
    
    // Singleton access
    private static MapKeyConfig instance;
    public static MapKeyConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<MapKeyConfig>("MapKeyConfig");
                if (instance == null)
                {
                    Debug.LogError("[MapKeyConfig] No MapKeyConfig found in Resources folder! Create one at Assets\\Resources\\MapKeyConfig.asset");
                }
            }
            return instance;
        }
    }
    
    /// <summary>
    /// Generate a random map key item instance
    /// </summary>
    public override ItemInstance GenerateItem(int currentMapLevel = 1)
    {
        // Calculate level range
        int minLevel = Mathf.Max(1, currentMapLevel - levelRange);
        int maxLevelClamped = Mathf.Min(maxLevel, currentMapLevel + levelRange);
        int randomLevel = Random.Range(minLevel, maxLevelClamped + 1);
        
        // Pick random arena
        string randomArena = arenaSceneNames[Random.Range(0, arenaSceneNames.Count)];
        
        // Roll rarity
        int rarityTier = RollRandomRarity();
        
        // Create display name: "SceneName MapKey Lv. X"
        string displayName = $"{randomArena} MapKey Lv. {randomLevel}";
        
        // Create item instance
        ItemInstance mapKey = new ItemInstance("mapkey", displayName, rarityTier, 1);
        
        // Store additional data as JSON
        MapKeyData data = new MapKeyData
        {
            level = randomLevel,
            sceneName = randomArena
        };
        mapKey.additionalData = JsonUtility.ToJson(data);
        
        Debug.Log($"[MapKeyConfig] Generated: {displayName} ({GetRarityName(rarityTier)})");
        
        return mapKey;
    }
    
    /// <summary>
    /// Get map key data from an item instance
    /// </summary>
    public static MapKeyData GetMapKeyData(ItemInstance item)
    {
        if (string.IsNullOrEmpty(item.additionalData))
            return null;
        
        return JsonUtility.FromJson<MapKeyData>(item.additionalData);
    }
}

/// <summary>
/// Additional data stored in MapKey items
/// </summary>
[System.Serializable]
public class MapKeyData
{
    public int level;
    public string sceneName;
}
