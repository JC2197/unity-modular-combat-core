using UnityEngine;
using System.Collections.Generic;
using JoeConticello.ModularCombatCore;

/// <summary>
/// Base configuration for all gear items (equipment).
/// Extends RarityItemConfig to add gear-specific properties like slot and modifiers.
/// </summary>
public abstract class GearItemConfig : RarityItemConfig
{
    [Header("Gear Properties")]
    [Tooltip("Which equipment slot this gear occupies")]
    public GearSlot gearSlot = GearSlot.Weapon;
    
    [Header("Sprites")]
    [Tooltip("Sprite shown in inventory")]
    public Sprite inventorySprite;
    
    [Tooltip("Sprite shown on ground")]
    public Sprite worldSprite;
    
    [Header("Visuals")]
    [Tooltip("Optional custom particle system (ignores rarity colors if set)")]
    public ParticleSystem particleSystemOverride;
    
    [Header("Modifier Generation")]
    [Tooltip("Reference to modifier database (defaults to singleton if null)")]
    public GearModifierDatabase modifierDatabase;
    
    [Header("Trait Grant")]
    [Tooltip("Trait granted when this gear is equipped (optional)")]
    public TraitData grantedTrait;
    
    /// <summary>
    /// Generate a procedural gear item instance
    /// </summary>
    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        // Roll rarity
        int rarityTier = RollRandomRarity();
        
        // Get base gear name from subclass
        string baseGearName = GetGearTypeName();

        // Roll item tier independently from modifier tier rolls.
        ItemTier rolledGearTier = TierScaler.RollTier();
        
        // Roll prefix/suffix and get modifiers
        GearModifierDatabase db = modifierDatabase != null ? modifierDatabase : GearModifierDatabase.Instance;
        GearRollResult rollResult = db != null 
            ? db.RollGear(baseGearName, gearSlot, rarityTier, rolledGearTier)
            : new GearRollResult { displayName = baseGearName, modifiers = new List<StatModifier>() };
        
        // Create display name with rarity prefix
        string rarityName = GetRarityName(rarityTier);
        string displayName = $"{rarityName} {rollResult.displayName}";
        
        // Create item instance
        ItemInstance gearInstance = new ItemInstance(GetItemType(), displayName, rarityTier, 1);
        
        // Store gear data as JSON
        GearItemData gearData = new GearItemData
        {
            gearSlot = gearSlot,
            modifiers = rollResult.modifiers,
            itemTier = rolledGearTier,
            grantedTraitID = grantedTrait != null ? grantedTrait.traitID : null
        };
        gearInstance.additionalData = JsonUtility.ToJson(gearData);
        
        return gearInstance;
    }
    
    /// <summary>
    /// Get the gear type name for display (e.g., "Sword", "Helmet")
    /// Override in subclasses
    /// </summary>
    protected abstract string GetGearTypeName();
    
    /// <summary>
    /// Get the item type string for ItemInstance
    /// Override in subclasses
    /// </summary>
    protected abstract string GetItemType();
}

/// <summary>
/// Serializable data stored in ItemInstance.additionalData for gear
/// </summary>
[System.Serializable]
public class GearItemData
{
    public GearSlot gearSlot;
    public List<StatModifier> modifiers;
    public ItemTier itemTier = ItemTier.I;
    
    [Tooltip("Trait granted when this gear is equipped")]
    public string grantedTraitID;
    public string grantedTraitName;
}
