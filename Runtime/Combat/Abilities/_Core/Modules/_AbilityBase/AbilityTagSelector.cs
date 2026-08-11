using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable] // This is correct - NOT a MonoBehaviour
public class AbilityTagSelector
{
    [SerializeField] private List<string> selectedTags = new List<string>();
    [SerializeField] private List<DamageTypeData> selectedDamageTypes = new List<DamageTypeData>();
    
    public List<string> SelectedTags => selectedTags;
    public List<DamageTypeData> SelectedDamageTypes => selectedDamageTypes;
    
    public bool HasTag(string tagName)
    {
        // Check regular tags
        if (selectedTags.Contains(tagName)) return true;
        
        // Check damage type tags
        foreach (var damageType in selectedDamageTypes)
        {
            if (damageType == null) continue;
            
            if (damageType.GetAllTagNames().Any(tag => tagName.Equals(tag, System.StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void AddTag(string tagName)
    {
        if (!HasTag(tagName) && TagDatabase.Instance != null && TagDatabase.Instance.HasTag(tagName))
        {
            selectedTags.Add(tagName);
        }
    }
    
    public void RemoveTag(string tagName)
    {
        selectedTags.Remove(tagName);
    }
    
    public void AddDamageType(DamageTypeData damageType)
    {
        if (damageType != null && !selectedDamageTypes.Contains(damageType))
        {
            selectedDamageTypes.Add(damageType);
        }
    }
    
    public void RemoveDamageType(DamageTypeData damageType)
    {
        selectedDamageTypes.Remove(damageType);
    }
    
    public void SetTags(List<string> tags)
    {
        selectedTags = new List<string>(tags);
    }
    
    public void SetDamageTypes(List<DamageTypeData> damageTypes)
    {
        selectedDamageTypes = new List<DamageTypeData>(damageTypes);
    }
    
    public List<TagEntry> GetTagObjects()
    {
        if (TagDatabase.Instance == null) return new List<TagEntry>();
        
        return selectedTags
            .Select(tagName => TagDatabase.Instance.GetTag(tagName))
            .Where(tag => tag != null)
            .ToList();
    }
    
    public bool HasAnyTag(params string[] tags)
    {
        return tags.Any(tag => HasTag(tag));
    }
    
    public bool HasAllTags(params string[] tags)
    {
        return tags.All(tag => HasTag(tag));
    }
    
    public bool HasDamageType(string categoryName)
    {
        return selectedDamageTypes.Any(dt => dt != null && dt.MatchesCategory(categoryName));
    }
    
    public bool HasElementalDamage()
    {
        return HasDamageType("Elemental");
    }
    
    public bool HasPhysicalDamage()
    {
        return HasDamageType("Physical");
    }
    
    public bool HasMagicalDamage()
    {
        return HasDamageType("Magical");
    }
    
    public List<DamageTypeData> GetDamageTypesByCategory(string categoryName)
    {
        return selectedDamageTypes.Where(dt => dt != null && dt.MatchesCategory(categoryName)).ToList();
    }
    
    // Get all combined tags (regular + damage types)
    public List<string> GetAllTags()
    {
        var allTags = new List<string>(selectedTags);
        
        foreach (var damageType in selectedDamageTypes)
        {
            if (damageType != null)
            {
                allTags.AddRange(damageType.GetAllTagNames());
            }
        }
        
        return allTags.Distinct().ToList();
    }
}