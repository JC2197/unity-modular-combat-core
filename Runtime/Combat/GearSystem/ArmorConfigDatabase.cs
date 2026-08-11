using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Database of all ArmorConfigs in the game.
/// Create one instance in your project and drag all ArmorConfigs into the list.
/// The registry uses this database for lookups.
/// 
/// IMPORTANT: This asset must be in a Resources folder to be loaded at runtime.
/// Recommended path: Assets/Resources/ArmorConfigDatabase.asset
/// </summary>
[CreateAssetMenu(fileName = "ArmorConfigDatabase", menuName = "Armor/Armor Config Database")]
public class ArmorConfigDatabase : ScriptableObject
{
    [Tooltip("List of all armor configs in the game. DO NOT remove entries manually - use the 'Clean Null Entries' button below.")]
    [SerializeField] // Use SerializeField instead of public to protect the list
    private List<ArmorConfig> allArmorConfigs = new List<ArmorConfig>();
    
    // Public read-only access
    public List<ArmorConfig> AllArmorConfigs => allArmorConfigs ?? new List<ArmorConfig>();
    
    private static ArmorConfigDatabase instance;
    
    public static ArmorConfigDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ArmorConfigDatabase>("ArmorConfigDatabase");
                if (instance == null)
                {
                    Debug.LogError("[ArmorConfigDatabase] No ArmorConfigDatabase found in Resources folder! Create one at Assets/Resources/ArmorConfigDatabase.asset");
                }
            }
            return instance;
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Add a config to the database (Editor only)
    /// </summary>
    public void AddConfig(ArmorConfig config)
    {
        if (config == null) return;
        if (allArmorConfigs == null) allArmorConfigs = new List<ArmorConfig>();
        if (!allArmorConfigs.Contains(config))
        {
            allArmorConfigs.Add(config);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
    
    /// <summary>
    /// Remove a config from the database (Editor only)
    /// </summary>
    public void RemoveConfig(ArmorConfig config)
    {
        if (allArmorConfigs == null) return;
        if (allArmorConfigs.Remove(config))
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
    
    /// <summary>
    /// Clean null entries (Editor only)
    /// </summary>
    public void CleanNullEntries()
    {
        if (allArmorConfigs == null)
        {
            allArmorConfigs = new List<ArmorConfig>();
            return;
        }
        
        int beforeCount = allArmorConfigs.Count;
        allArmorConfigs.RemoveAll(config => config == null);
        int afterCount = allArmorConfigs.Count;
        
        if (beforeCount != afterCount)
        {
            Debug.LogWarning($"[ArmorConfigDatabase] Removed {beforeCount - afterCount} null entries.");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
    
    private void OnEnable()
    {
        // Ensure list is initialized
        if (allArmorConfigs == null)
        {
            allArmorConfigs = new List<ArmorConfig>();
        }
    }
#endif
}
