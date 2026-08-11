using UnityEngine;
using System.Collections.Generic;
using JoeConticello.ModularCombatCore;

/// <summary>
/// Utility class for calculating final damage with bonuses from attacker's stats
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Calculate final damage by applying the attacker's damage modifiers.
    /// </summary>
    /// <param name="baseDamage">Base damage before bonuses</param>
    /// <param name="damageTypeName">Configured damage type identifier</param>
    /// <param name="attacker">The GameObject dealing the damage</param>
    /// <returns>Final damage with bonuses applied</returns>
    private static float GetDamageBonusForType(StatContainer stats, DamageTypeData damageType)
    {
        if (stats == null)
            return 0f;

        float total = 0f;

        if (damageType != null)
        {
            foreach (string statId in damageType.GetAttackerModifierStatIds())
            {
                if (stats.HasStat(statId))
                    total += stats.GetStat(statId);
            }
        }

        if ((damageType == null || damageType.includeGenericDamageBonus) && stats.HasStat("DamageBonus"))
            total += stats.GetStat("DamageBonus");

        return total;
    }

    private static DamageTypeData ResolveDamageType(string damageTypeName)
    {
        if (string.IsNullOrWhiteSpace(damageTypeName))
            return null;

        return DamageTypeDatabase.Instance?.GetDamageType(damageTypeName)
            ?? DamageTypeRegistry.GetDamageType(damageTypeName);
    }

    public static float CalculateFinalDamage(float baseDamage, string damageTypeName, GameObject attacker)
    {
        if (attacker == null)
        {
            return baseDamage;
        }
        
        // Try to get the attacker's stat container
        Organism organism = attacker.GetComponent<Organism>();
        if (organism == null || organism.AllStats == null)
        {
            return baseDamage;
        }

        DamageTypeData damageType = ResolveDamageType(damageTypeName);
        float damageBonus = GetDamageBonusForType(organism.AllStats, damageType);
        
        float finalDamage = baseDamage * (1f + damageBonus);
        
        if (damageBonus > 0f)
        {
            Debug.Log($"[DamageCalculator] {damageTypeName} damage: {baseDamage} → {finalDamage} (+{damageBonus * 100f:F1}% bonus)");
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// Calculate final damage with ability tag modifiers (e.g., "Projectile abilities deal 10% more damage").
    /// Use this when you know the ability name and tags.
    /// </summary>
    /// <param name="baseDamage">Base damage before bonuses</param>
    /// <param name="damageTypeName">Type of damage (Fire, Lightning, Physical, etc.)</param>
    /// <param name="abilityName">Name of the ability dealing damage</param>
    /// <param name="abilityTags">Tags of the ability (e.g., "Projectile", "Area")</param>
    /// <param name="attacker">The GameObject dealing the damage</param>
    /// <returns>Final damage with all bonuses applied</returns>
    public static float CalculateFinalDamageWithAbilityModifiers(
        float baseDamage, 
        string damageTypeName, 
        string abilityName,
        System.Collections.Generic.List<string> abilityTags,
        GameObject attacker)
    {
        if (attacker == null || string.IsNullOrEmpty(damageTypeName))
        {
            return baseDamage;
        }
        
        return CalculateFinalDamage(baseDamage, damageTypeName, attacker);
    }
    
    /// <summary>
    /// Full damage calculation with trait effect processing.
    /// Creates a DamageContext with final damage calculation including crit.
    /// 
    /// Crit is rolled per-hit using the attacker's CritChance/CritDamage stats,
    /// plus the ability's baseCritChance and any trait CritChance modifiers.
    /// </summary>
    public static DamageContext CalculateDamageWithTraitEffects(
        float baseDamage,
        string damageTypeName,
        string abilityName,
        System.Collections.Generic.List<string> abilityTags,
        GameObject attacker,
        GameObject target,
        Vector3? hitPosition = null,
        AbilityDataConfig abilityConfig = null)
    {
        // Roll crit per-hit
        float critMultiplier = RollCrit(attacker, abilityName, abilityTags, abilityConfig);

        // Create damage context
        DamageContext context = DamageContext.Create(
            attacker,
            target,
            baseDamage,
            damageTypeName,
            abilityName,
            abilityTags,
            hitPosition,
            critMultiplier
        );
        
        if (attacker == null)
            return context;
        
        // Apply base damage calculation
        context.FinalDamage = CalculateFinalDamageWithAbilityModifiers(
            baseDamage,
            damageTypeName,
            abilityName,
            abilityTags,
            attacker
        );
        
        // Apply crit multiplier (rolled above)
        if (critMultiplier > 1f)
        {
            context.FinalDamage *= critMultiplier;
        }
        
        return context;
    }

    /// <summary>
    /// Roll crit per-hit using: character CritChance stat + ability baseCritChance + trait CritChance modifiers.
    /// Returns the CritDamage multiplier on success, or 1f on miss.
    /// </summary>
    private static float RollCrit(
        GameObject attacker,
        string abilityName,
        System.Collections.Generic.List<string> abilityTags,
        AbilityDataConfig abilityConfig)
    {
        if (attacker == null) return 1f;

        Organism organism = attacker.GetComponent<Organism>();
        if (organism == null || organism.AllStats == null) return 1f;

        StatContainer stats = organism.AllStats;

        // 1. Character base crit chance (fraction: 0.03 = 3%)
        float critChance = stats.HasStat("CritChance") ? stats.GetStat("CritChance") : 0f;

        // 2. Ability base crit chance (fraction: 0.05 = 5%)
        if (abilityConfig != null)
            critChance += abilityConfig.baseCritChance;

        critChance = Mathf.Clamp(critChance, 0f, 0.95f);

        if (critChance <= 0f) return 1f;

        // Crit damage multiplier from stats (e.g. 1.5 = 150%)
        float critDamage = stats.HasStat("CritDamage") ? stats.GetStat("CritDamage") : 1.5f;

        // Ability-specific bonus crit damage (e.g. baseCritDamageMultiplier = 0.5 → +50%)
        if (abilityConfig != null && abilityConfig.baseCritDamageMultiplier != 0f)
            critDamage += abilityConfig.baseCritDamageMultiplier;

        float roll = Random.Range(0f, 1f);
        if (roll < critChance)
        {
            Debug.Log($"[DamageCalculator] CRIT! Roll {roll:F3} < {critChance * 100f:F1}% → {critDamage:F2}x (ability={abilityName})");
            return critDamage;
        }

        return 1f;
    }
}
