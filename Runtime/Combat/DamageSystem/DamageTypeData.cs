using System;
using System.Collections.Generic;
using UnityEngine;

public enum DamageCategory
{
    Physical,
    Elemental,
    Magical,
    Special
}

public enum PhysicalSubcategory
{
    None,
    Piercing,
    Slashing,
    Bludgeoning,
    Bleeding,
}
 
public enum ElementalSubcategory
{
    None,
    Fire,
    Ice,
    Lightning,
    Earth,
    Poison,
    Nature
}

public enum MagicalSubcategory
{
    None,
    Arcane,
    Dark,
    Holy,
    Chaos,
}

public enum SpecialSubcategory
{
    None,
    Psychic,
    True
}

[CreateAssetMenu(fileName = "DamageType_", menuName = "Damage/Damage Type Data")]
public class DamageTypeData : ScriptableObject
{
    [Header("Damage Type Identity")]
    public string damageTypeName = "Physical";
    public string displayName = "Physical";
    [TextArea(2, 4)]
    public string description = "";
    public Color damageColor = Color.red;
    
    [Header("Category & Tags")]
    public string categoryId = "physical";
    public string categoryName = "Physical";
    public string subcategoryName = "";
    public List<string> tags = new List<string>();
    
    [Header("Critical Hits")]
    public bool canCriticalHit = true;

    [Header("Damage Interactions")]
    public List<DamageTypeInteraction> interactions = new List<DamageTypeInteraction>();

    [Header("Stat Sync")]
    public bool createResistanceStat = true;
    public bool createDamageBonusStat = true;

    [Header("Special Properties")]
    public bool ignoresShields = false;

    [SerializeField, HideInInspector] private DamageCategory category = DamageCategory.Physical;
    [SerializeField, HideInInspector] private PhysicalSubcategory physicalSubcategory = PhysicalSubcategory.None;
    [SerializeField, HideInInspector] private ElementalSubcategory elementalSubcategory = ElementalSubcategory.None;
    [SerializeField, HideInInspector] private MagicalSubcategory magicalSubcategory = MagicalSubcategory.None;
    [SerializeField, HideInInspector] private SpecialSubcategory specialSubcategory = SpecialSubcategory.None;

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

    public float CalculateDamage(float baseDamage, MainStats attackerStats, DefenseStats defenderStats)
    {
        // Damage calculation now uses the unified stat system
        // Resistances are applied via the StatContainer in IDamageable
        // This method is simplified to just return base damage
        // Actual mitigation happens in the receiving entity's damage calculation
        return baseDamage;
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
        MigrateLegacyCategoryData();
        NormalizeValues();
    }

    private void OnValidate()
    {
        MigrateLegacyCategoryData();
        NormalizeValues();
    }

    private void MigrateLegacyCategoryData()
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            categoryId = category.ToString();

        if (string.IsNullOrWhiteSpace(categoryName))
            categoryName = category.ToString();

        if (string.IsNullOrWhiteSpace(subcategoryName))
        {
            string legacySubtype = GetLegacySubcategoryName();
            if (!string.IsNullOrWhiteSpace(legacySubtype) && !string.Equals(legacySubtype, category.ToString(), StringComparison.OrdinalIgnoreCase))
                subcategoryName = legacySubtype;
        }
    }

    private string GetLegacySubcategoryName()
    {
        switch (category)
        {
            case DamageCategory.Physical:
                return physicalSubcategory != PhysicalSubcategory.None ? physicalSubcategory.ToString() : category.ToString();
            case DamageCategory.Elemental:
                return elementalSubcategory != ElementalSubcategory.None ? elementalSubcategory.ToString() : category.ToString();
            case DamageCategory.Magical:
                return magicalSubcategory != MagicalSubcategory.None ? magicalSubcategory.ToString() : category.ToString();
            case DamageCategory.Special:
                return specialSubcategory != SpecialSubcategory.None ? specialSubcategory.ToString() : category.ToString();
            default:
                return category.ToString();
        }
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