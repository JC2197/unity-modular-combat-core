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
            
            if (tagName.Equals(damageType.displayName, System.StringComparison.OrdinalIgnoreCase) ||
                tagName.Equals(damageType.GetSubcategory(), System.StringComparison.OrdinalIgnoreCase) ||
                tagName.Equals(damageType.category.ToString(), System.StringComparison.OrdinalIgnoreCase))
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
    
    public bool HasDamageType(DamageCategory category)
    {
        return selectedDamageTypes.Any(dt => dt.category == category);
    }
    
    public bool HasElementalDamage()
    {
        return HasDamageType(DamageCategory.Elemental);
    }
    
    public bool HasPhysicalDamage()
    {
        return HasDamageType(DamageCategory.Physical);
    }
    
    public bool HasMagicalDamage()
    {
        return HasDamageType(DamageCategory.Magical);
    }
    
    public List<DamageTypeData> GetDamageTypesByCategory(DamageCategory category)
    {
        return selectedDamageTypes.Where(dt => dt.category == category).ToList();
    }
    
    // Get all combined tags (regular + damage types)
    public List<string> GetAllTags()
    {
        var allTags = new List<string>(selectedTags);
        
        foreach (var damageType in selectedDamageTypes)
        {
            if (damageType != null)
            {
                allTags.Add(damageType.displayName);
                allTags.Add(damageType.category.ToString());
                
                string subcategory = damageType.GetSubcategory();
                if (subcategory != damageType.category.ToString())
                {
                    allTags.Add(subcategory);
                }
            }
        }
        
        return allTags.Distinct().ToList();
    }
}