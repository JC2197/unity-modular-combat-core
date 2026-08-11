using UnityEngine;

/// <summary>
/// Drop table entry for enemy loot drops
/// </summary>
[System.Serializable]
public class DropTableEntry
{
    [Tooltip("Item configuration to drop (drag ItemConfig or subclass like MapKeyConfig)")]
    public ItemConfig itemConfig;
    
    [Tooltip("Chance to drop (0-1, where 0.5 = 50% chance)")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
    
    [Tooltip("Minimum quantity to drop")]
    public int minQuantity = 1;
    
    [Tooltip("Maximum quantity to drop")]
    public int maxQuantity = 1;
}


