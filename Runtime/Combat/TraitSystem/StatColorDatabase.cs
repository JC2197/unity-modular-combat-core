using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Legacy color database. Now delegates to TagDatabase for all lookups.
/// Kept for backward compatibility with existing assets.
/// Use TagDatabase directly for new code.
/// </summary>
[CreateAssetMenu(fileName = "StatColorDatabase", menuName = "Traits/Stat Color Database")]
public class StatColorDatabase : ScriptableObject
{
    [Header("Color Palette (Legacy — use TagDatabase instead)")]
    public List<StatColorEntry> colorEntries = new List<StatColorEntry>();
    
    private static StatColorDatabase instance;
    public static StatColorDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<StatColorDatabase>("StatColorDatabase");
            }
            return instance;
        }
    }
    
    public StatColorEntry GetColorEntry(string statName)
    {
        if (string.IsNullOrEmpty(statName))
            return null;
        return colorEntries.Find(entry => entry.statName.Equals(statName, System.StringComparison.OrdinalIgnoreCase));
    }
    
    public Color GetPrimaryColor(string statName)
    {
        // Delegate to TagDatabase first
        TagDatabase tagDB = TagDatabase.Instance;
        if (tagDB != null)
        {
            var tag = tagDB.GetTag(statName);
            if (tag != null) return tag.primaryColor;
        }
        // Fallback to local entries
        StatColorEntry entry = GetColorEntry(statName);
        return entry != null ? entry.primaryColor : Color.white;
    }
    
    public Color GetSecondaryColor(string statName)
    {
        TagDatabase tagDB = TagDatabase.Instance;
        if (tagDB != null)
        {
            var tag = tagDB.GetTag(statName);
            if (tag != null) return tag.secondaryColor;
        }
        StatColorEntry entry = GetColorEntry(statName);
        return entry != null ? entry.secondaryColor : Color.white;
    }
    
    public string[] GetAllStatNames()
    {
        // Delegate to TagDatabase
        TagDatabase tagDB = TagDatabase.Instance;
        if (tagDB != null) return tagDB.GetAllTagNames();
        
        string[] names = new string[colorEntries.Count];
        for (int i = 0; i < colorEntries.Count; i++)
            names[i] = colorEntries[i].statName;
        return names;
    }
}

/// <summary>
/// Single entry in the stat color database
/// </summary>
[System.Serializable]
public class StatColorEntry
{
    [Tooltip("Name of the stat/element (e.g., 'Fire', 'Ice', 'Physical')")]
    public string statName;
    
    [Tooltip("Primary color for this stat")]
    public Color primaryColor = Color.white;
    
    [Tooltip("Secondary color for gradients or highlights")]
    public Color secondaryColor = Color.white;
}
