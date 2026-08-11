using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A modifier that can be applied to gear.
/// Each modifier is its own ScriptableObject for easy management and reusability.
/// </summary>
[CreateAssetMenu(fileName = "New Gear Modifier", menuName = "Items/Gear Modifier")]
public class GearModifier : ScriptableObject
{
    [Tooltip("Modifier label (for future use)")]
    public string label = "Commander's";
    
    [Tooltip("Color theme from TagDatabase (used in UI/tooltips)")]
    [TagDropdown]
    public string colorTheme = "";
    
    [Tooltip("Color type that pairs this modifier with crafting orbs (e.g. a Red orb produces Red-type modifiers)")]
    public GearColorType colorType;
    
    [Tooltip("Minimum tier required for this modifier to appear")]
    public ItemTier baseTierAvailable = ItemTier.I;
    
    [Tooltip("Tier scaling configuration (defines multipliers for each tier)")]
    public TierScalingConfig tierScalingConfig;
    
    [Tooltip("Which gear slots can have this modifier (empty = all slots)")]
    public List<GearSlot> applicableSlots = new List<GearSlot>();
    
    [Tooltip("Stat modifiers this grants (with tiered values)")]
    public List<TieredStatModifier> modifiers = new List<TieredStatModifier>();
    
    /// <summary>
    /// Check if this modifier can be applied to the given slot
    /// </summary>
    public bool IsValidForSlot(GearSlot slot)
    {
        return applicableSlots.Count == 0 || applicableSlots.Contains(slot);
    }
    
    /// <summary>
    /// Check if this modifier is available at the given tier
    /// </summary>
    public bool IsValidForTier(ItemTier tier)
    {
        return baseTierAvailable <= tier;
    }
    
    /// <summary>
    /// Get the resolved color from the database
    /// </summary>
    public Color GetColor()
    {
        if (string.IsNullOrEmpty(colorTheme))
            return Color.white;
            
        TagDatabase tagDB = TagDatabase.Instance;
        if (tagDB == null)
            return Color.white;
            
        return tagDB.GetPrimaryColor(colorTheme);
    }
}
