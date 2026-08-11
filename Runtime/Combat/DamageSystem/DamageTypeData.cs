using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "DamageType_", menuName = "Damage/Damage Type Data")]
public class DamageTypeData : ScriptableObject
{
    [Header("Damage Type Identity")]
    public string damageTypeName = "DamageType";
    public string displayName = "Damage Type";
    [TextArea(2, 4)]
    public string description = "";
    public Color damageColor = Color.red;
    
    [Header("Category & Tags")]
    public string categoryId = "";
    public string categoryName = "";
    public string subcategoryName = "";
    public List<string> tags = new List<string>();
    
    [Header("Critical Hits")]
    public bool canCriticalHit = true;

    [Header("Damage Interactions")]
    public List<DamageTypeInteraction> interactions = new List<DamageTypeInteraction>();

    [Header("Stat Sync")]
    public bool createResistanceStat = true;
    public bool createDamageBonusStat = true;

    [Header("Damage Calculation")]
    [Tooltip("Attacker stat IDs that increase this damage type. If left empty, a default ID is derived from the damage type name.")]
    public List<string> attackerModifierStatIds = new List<string>();

    [Tooltip("Defender stat IDs that reduce this damage type. If left empty, a default ID is derived from the damage type name.")]
    public List<string> defenderResistanceStatIds = new List<string>();

    [Tooltip("Whether the shared DamageBonus stat should also affect this damage type.")]
    public bool includeGenericDamageBonus = true;

    [Header("Special Properties")]
    public bool ignoresShields = false;
    
    public string GetCategoryName()
    {
        if (!string.IsNullOrWhiteSpace(categoryName))
            return categoryName.Trim();

        if (!string.IsNullOrWhiteSpace(categoryId))
            return categoryId.Trim();

        return "Uncategorized";
    }

    /// <summary>
    /// Returns the optional subtype label for filtering and tag matching.
    /// </summary>
    public string GetSubcategory()
    {
        return string.IsNullOrWhiteSpace(subcategoryName) ? GetCategoryName() : subcategoryName.Trim();
    }

    public bool MatchesCategory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return string.Equals(categoryId, value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(GetCategoryName(), value, StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<string> GetAllTagNames()
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                seen.Add(value.Trim());
        }

        Add(damageTypeName);
        Add(displayName);
        Add(categoryId);
        Add(GetCategoryName());

        string subtype = GetSubcategory();
        if (!string.Equals(subtype, GetCategoryName(), StringComparison.OrdinalIgnoreCase))
            Add(subtype);

        for (int i = 0; i < tags.Count; i++)
            Add(tags[i]);

        return seen;
    }

    public IEnumerable<string> GetAttackerModifierStatIds()
    {
        return GetNormalizedStatIds(attackerModifierStatIds, createDamageBonusStat ? BuildDefaultStatId("DamageBonus") : null);
    }

    public IEnumerable<string> GetDefenderResistanceStatIds()
    {
        return GetNormalizedStatIds(defenderResistanceStatIds, createResistanceStat ? BuildDefaultStatId("Resistance") : null);
    }

    public float CalculateDamage(float baseDamage, DefenseStats defenderStats)
    {
        return CalculateDamage(baseDamage, null, defenderStats);
    }

    public string GetFormattedDescription()
    {
        string desc = $"<color=#{ColorUtility.ToHtmlStringRGB(damageColor)}>{displayName}</color>\n";
        desc += description;
        
        if (ignoresShields)
        {
            desc += "\n• Ignores shields";
        }
        
        return desc;
    }

    private void OnEnable()
    {
        NormalizeValues();
    }

    private void OnValidate()
    {
        NormalizeValues();
    }

    private IEnumerable<string> GetNormalizedStatIds(List<string> configuredIds, string fallbackId)
    {
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (configuredIds != null)
        {
            for (int i = 0; i < configuredIds.Count; i++)
            {
                string normalized = NormalizeStatId(configuredIds[i]);
                if (!string.IsNullOrWhiteSpace(normalized) && uniqueIds.Add(normalized))
                    yield return normalized;
            }
        }

        string normalizedFallback = NormalizeStatId(fallbackId);
        if (!string.IsNullOrWhiteSpace(normalizedFallback) && uniqueIds.Add(normalizedFallback))
            yield return normalizedFallback;
    }

    private string BuildDefaultStatId(string suffix)
    {
        string baseName = string.IsNullOrWhiteSpace(damageTypeName) ? displayName : damageTypeName;
        if (string.IsNullOrWhiteSpace(baseName))
            return string.Empty;

        return $"{baseName.Trim()}{suffix}";
    }

    private static string NormalizeStatId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private void NormalizeValues()
    {
        damageTypeName = string.IsNullOrWhiteSpace(damageTypeName) ? name : damageTypeName.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? damageTypeName : displayName.Trim();
        categoryId = string.IsNullOrWhiteSpace(categoryId) ? GetCategoryName() : categoryId.Trim();
        categoryName = string.IsNullOrWhiteSpace(categoryName) ? categoryId : categoryName.Trim();
        subcategoryName = string.IsNullOrWhiteSpace(subcategoryName) ? string.Empty : subcategoryName.Trim();

        HashSet<string> uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> normalizedTags = new List<string>();
        for (int i = 0; i < tags.Count; i++)
        {
            string value = string.IsNullOrWhiteSpace(tags[i]) ? string.Empty : tags[i].Trim();
            if (string.IsNullOrWhiteSpace(value) || !uniqueTags.Add(value))
                continue;

            normalizedTags.Add(value);
        }

        if (normalizedTags.Count != tags.Count)
        {
            tags = normalizedTags;
        }

        attackerModifierStatIds = NormalizeStatIdList(attackerModifierStatIds);
        defenderResistanceStatIds = NormalizeStatIdList(defenderResistanceStatIds);
    }

    private static List<string> NormalizeStatIdList(List<string> values)
    {
        List<string> normalized = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (values == null)
            return normalized;

        for (int i = 0; i < values.Count; i++)
        {
            string value = NormalizeStatId(values[i]);
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                continue;

            normalized.Add(value);
        }

        return normalized;
    }
}

[System.Serializable]
public class DamageTypeInteraction
{
    public DamageTypeData targetDamageType;
    public InteractionType interactionType;
    public float effectMultiplier = 1f;
    public DamageTypeData resultingDamageType;
    
    public enum InteractionType
    {
    }
}

public enum StatusEffectType
{
    
}