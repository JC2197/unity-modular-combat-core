using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("Category & Subcategories")]
    public DamageCategory category = DamageCategory.Physical;
    public PhysicalSubcategory physicalSubcategory = PhysicalSubcategory.None;
    public ElementalSubcategory elementalSubcategory = ElementalSubcategory.None;
    public MagicalSubcategory magicalSubcategory = MagicalSubcategory.None;
    public SpecialSubcategory specialSubcategory = SpecialSubcategory.None;
    
    [Header("Critical Hits")]
    public bool canCriticalHit = true;

    [Header("Damage Interactions")]
    public List<DamageTypeInteraction> interactions = new List<DamageTypeInteraction>();

    [Header("Special Properties")]
    public bool ignoresShields = false;

    /// <summary>
    /// Get the subcategory as a string based on the main category
    /// </summary>
    public string GetSubcategory()
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