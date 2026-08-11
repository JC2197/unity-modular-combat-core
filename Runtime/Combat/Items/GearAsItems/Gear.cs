using UnityEngine;
using System.Collections.Generic;

public enum GearRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

public enum GearSlot
{
    Head,
    Chest,
    Feet,
    Hands,
    Trinket,
    Weapon,
    OffHandWeapon,
    Backpack
}

public abstract class Gear : MonoBehaviour
{
    [Header("Gear Information")]
    [SerializeField] protected string gearName = "Unknown Item";
    [SerializeField] protected string description = "";
    [SerializeField] protected GearSlot slotType;
    [SerializeField] protected GearRarity rarity = GearRarity.Common;
    [SerializeField] protected int itemLevel = 1;
    
    // Properties for easy access
    public string GearName => gearName;
    public string Description => description;
    public GearSlot SlotType => slotType;
    public GearRarity Rarity => rarity;
    public int ItemLevel => itemLevel;
    
    // Abstract methods for specific gear types to implement
    protected abstract void OnEquip(Organism organism);
    protected abstract void OnUnequip(Organism organism);
    public abstract bool CanEquip(Organism organism);
    
    // Utility method for rarity colors
    public Color GetRarityColor()
    {
        return rarity switch
        {
            GearRarity.Common => Color.white,
            GearRarity.Uncommon => Color.green,
            GearRarity.Rare => Color.blue,
            GearRarity.Epic => new Color(0.6f, 0f, 1f), // Purple
            GearRarity.Legendary => new Color(1f, 0.6f, 0f), // Orange
            GearRarity.Mythic => Color.red,
            _ => Color.white
        };
    }
}