using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Registry for looking up ArmorConfig by name.
/// Loads from ArmorConfigDatabase ScriptableObject.
/// This allows armor configs to exist independently without needing to be in ArmorItemDropsConfig lists.
/// </summary>
public static class ArmorConfigRegistry
{
    private static Dictionary<string, ArmorConfig> configsByName;
    private static bool isInitialized = false;

    /// <summary>
    /// Initialize the registry by loading all ArmorConfigs from the database
    /// </summary>
    private static void Initialize()
    {
        if (isInitialized) return;

        configsByName = new Dictionary<string, ArmorConfig>();
        
        ArmorConfigDatabase database = ArmorConfigDatabase.Instance;
        if (database == null)
        {
            Debug.LogError("[ArmorConfigRegistry] Cannot initialize - ArmorConfigDatabase not found!");
            isInitialized = true;
            return;
        }

        // Filter out null configs before processing
        int nullCount = 0;
        foreach (var config in database.AllArmorConfigs)
        {
            if (config == null)
            {
                nullCount++;
                continue;
            }
            
            if (string.IsNullOrEmpty(config.gearName))
            {
                Debug.LogWarning($"[ArmorConfigRegistry] Skipping config with empty gearName");
                continue;
            }
            
            if (!configsByName.ContainsKey(config.gearName))
            {
                configsByName[config.gearName] = config;
                Debug.Log($"[ArmorConfigRegistry] Registered: {config.gearName}");
            }
            else
            {
                Debug.LogWarning($"[ArmorConfigRegistry] Duplicate armor name found: {config.gearName}. Using first instance.");
            }
        }
        
        if (nullCount > 0)
        {
            Debug.LogWarning($"[ArmorConfigRegistry] Found {nullCount} null configs in database. Consider cleaning up the database.");
        }

        isInitialized = true;
        Debug.Log($"[ArmorConfigRegistry] Initialized with {configsByName.Count} armor configs");
    }

    /// <summary>
    /// Get an ArmorConfig by its gear name
    /// </summary>
    public static ArmorConfig GetConfig(string gearName)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (string.IsNullOrEmpty(gearName))
        {
            Debug.LogWarning("[ArmorConfigRegistry] Cannot get config - gearName is null or empty");
            return null;
        }

        if (configsByName.TryGetValue(gearName, out ArmorConfig config))
        {
            return config;
        }

        Debug.LogWarning($"[ArmorConfigRegistry] No ArmorConfig found with name: {gearName}");
        return null;
    }

    /// <summary>
    /// Check if a config exists
    /// </summary>
    public static bool HasConfig(string gearName)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return configsByName.ContainsKey(gearName);
    }

    /// <summary>
    /// Get all registered config names
    /// </summary>
    public static string[] GetAllConfigNames()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return configsByName.Keys.ToArray();
    }

    /// <summary>
    /// Force reload all configs (useful for editor refresh)
    /// </summary>
    public static void Reload()
    {
        isInitialized = false;
        configsByName?.Clear();
        Initialize();
    }
}
