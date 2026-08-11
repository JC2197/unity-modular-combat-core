using UnityEngine;

/// <summary>
/// Helper class with extension methods for easy trait integration.
/// Makes it simpler to query traits from abilities and other systems.
/// </summary>
public static class TraitSystemHelpers
{
    /// <summary>
    /// Get the final stat value with trait modifiers applied
    /// </summary>
    public static float GetModifiedStat(this GameObject character, string statID, float baseValue)
    {
        var traitManager = character.GetComponent<CharacterTraitManager>();
        if (traitManager != null)
        {
            return traitManager.CalculateFinalStat(statID, baseValue);
        }
        return baseValue;
    }
    
    /// <summary>
    /// Check if an ability has been replaced by a trait
    /// </summary>
    public static AbilityConfig GetAbilityReplacement(this GameObject character, string abilityName)
    {
        var traitManager = character.GetComponent<CharacterTraitManager>();
        if (traitManager != null)
        {
            return traitManager.GetAbilityReplacement(abilityName);
        }
        return null;
    }
    
    /// <summary>
    /// Check if character has a specific trait
    /// </summary>
    public static bool HasTrait(this GameObject character, TraitData traitData)
    {
        var traitManager = character.GetComponent<CharacterTraitManager>();
        if (traitManager != null)
        {
            return traitManager.HasTrait(traitData);
        }
        return false;
    }
    
    /// <summary>
    /// Check if character has a trait by ID
    /// </summary>
    public static bool HasTraitByID(this GameObject character, string traitID)
    {
        var traitManager = character.GetComponent<CharacterTraitManager>();
        if (traitManager != null)
        {
            var activeTraits = traitManager.GetActiveTraits();
            foreach (var trait in activeTraits)
            {
                if (trait.traitID == traitID)
                    return true;
            }
        }
        return false;
    }
}
