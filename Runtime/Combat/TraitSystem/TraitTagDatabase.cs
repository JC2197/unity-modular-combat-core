using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject database for trait tags with associated colors.
/// Contains two zones: Core Tags and Specialized Tags.
/// Weapon type strings are handled directly via [WeaponTypeDropdown] and do not live here.
/// </summary>
[CreateAssetMenu(fileName = "TraitTagDatabase", menuName = "Traits/Trait Tag Database")]
public class TraitTagDatabase : ScriptableObject
{
    [Header("Core Trait Tags")]
    [Tooltip("Core trait tags (Body, Mind, Skill, Survival, Power, Faith) - determines primary categorization")]
    public List<TraitTagEntry> coreTags = new List<TraitTagEntry>();

    [Header("Specialized Trait Tags")]
    [Tooltip("Specialized trait tags (Fire, Ice, Lightning, etc.) - for synergy weighting")]
    public List<TraitTagEntry> specializedTags = new List<TraitTagEntry>();

    // Singleton access
    private static TraitTagDatabase instance;
    public static TraitTagDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<TraitTagDatabase>("TraitTagDatabase");
                if (instance == null)
                {
                    Debug.LogError("[TraitTagDatabase] No TraitTagDatabase found in Resources folder! Create one at Assets/Resources/TraitTagDatabase.asset");
                }
            }
            return instance;
        }
    }

    // Cached lookups for O(1) access
    private Dictionary<string, TraitTagEntry> coreTagLookup;
    private Dictionary<string, TraitTagEntry> specializedTagLookup;

    private void OnEnable()
    {
        RebuildLookups();
    }

    private void OnValidate()
    {
        RebuildLookups();
    }

    /// <summary>
    /// Rebuild internal lookup dictionaries for fast access.
    /// </summary>
    public void RebuildLookups()
    {
        coreTagLookup = new Dictionary<string, TraitTagEntry>();
        specializedTagLookup = new Dictionary<string, TraitTagEntry>();

        foreach (var entry in coreTags)
        {
            if (!string.IsNullOrEmpty(entry.tagName) && !coreTagLookup.ContainsKey(entry.tagName))
                coreTagLookup[entry.tagName] = entry;
        }

        foreach (var entry in specializedTags)
        {
            if (!string.IsNullOrEmpty(entry.tagName) && !specializedTagLookup.ContainsKey(entry.tagName))
                specializedTagLookup[entry.tagName] = entry;
        }
    }

    // ============================================================================
    // Tag Name Arrays for Dropdowns
    // ============================================================================

    /// <summary>
    /// Get all core tag names for dropdown population.
    /// </summary>
    public string[] GetCoreTagNames()
    {
        string[] names = new string[coreTags.Count + 1];
        names[0] = ""; // None option
        for (int i = 0; i < coreTags.Count; i++)
        {
            names[i + 1] = coreTags[i].tagName;
        }
        return names;
    }

    /// <summary>
    /// Get all specialized tag names for dropdown population.
    /// </summary>
    public string[] GetSpecializedTagNames()
    {
        string[] names = new string[specializedTags.Count + 1];
        names[0] = ""; // None option
        for (int i = 0; i < specializedTags.Count; i++)
        {
            names[i + 1] = specializedTags[i].tagName;
        }
        return names;
    }

    // ============================================================================
    // Color Lookups
    // ============================================================================

    /// <summary>
    /// Get a tag entry by name (searches all categories).
    /// </summary>
    public TraitTagEntry GetTagEntry(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return null;

        if (coreTagLookup != null && coreTagLookup.TryGetValue(tagName, out var coreEntry))
            return coreEntry;
        if (specializedTagLookup != null && specializedTagLookup.TryGetValue(tagName, out var specEntry))
            return specEntry;
        return null;
    }


    /// <summary>
    /// Get the color for a tag by name.
    /// Delegates to TagDatabase for color lookup.
    /// </summary>
    public Color GetTagColor(string tagName)
    {
        TraitTagEntry entry = GetTagEntry(tagName);
        if (entry == null)
            return Color.white;

        // Get color from TagDatabase using the configured colorTheme
        TagDatabase tagDB = TagDatabase.Instance;
        if (tagDB != null && !string.IsNullOrEmpty(entry.colorTheme))
        {
            return tagDB.GetPrimaryColor(entry.colorTheme);
        }

        return Color.white;
    }

    /// <summary>
    /// Check if a tag exists in any category.
    /// </summary>
    public bool HasTag(string tagName)
    {
        return GetTagEntry(tagName) != null;
    }
}

/// <summary>
/// Single entry in the trait tag database.
/// </summary>
[System.Serializable]
public class TraitTagEntry
{
    [Tooltip("Name of the tag (e.g., 'Fire', 'Body', 'Pistol')")]
    // Reusing weapon type dropdown for tag names, can be customized later s
    public string tagName;

    [Tooltip("Color theme from TagDatabase to use for this tag")]
    [TagDropdown]
    public string colorTheme = "";

    [Tooltip("Optional description of this tag")]
    [TextArea(1, 2)]
    public string description;
}


