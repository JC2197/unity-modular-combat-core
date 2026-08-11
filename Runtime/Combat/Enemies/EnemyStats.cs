using UnityEngine;

/// <summary>
/// Simplified stat structure for enemies with only the stats needed for combat
/// </summary>
[System.Serializable]
public class EnemyStats
{
    [Header("Core Stats")]
    [Tooltip("Maximum health")]
    public float maxHealth = 100f;
    
    [Tooltip("Movement speed")]
    public float movementSpeed = 1.5f;
    
    [Tooltip("Attack speed multiplier")]
    public float attackSpeed = 1f;
    
    [Header("Defense Stats")]
    [Tooltip("Flat damage reduction")]
    public float armor = 0f;
    
    [Tooltip("Shield that absorbs damage before health")]
    public float forceField = 0f;
    
    [Tooltip("Chance to dodge attacks (0-1)")]
    [Range(0f, 1f)]
    public float dodgeChance = 0f;
    
    [Tooltip("Chance to block attacks (0-1)")]
    [Range(0f, 1f)]
    public float blockChance = 0f;
    
    [Header("Resistances")]
    [Tooltip("Piercing damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float piercingResistance = 0f;
    
    [Tooltip("Slashing damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float slashingResistance = 0f;
    
    [Tooltip("Bludgeoning damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float bludgeoningResistance = 0f;
    
    [Tooltip("Fire damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float fireResistance = 0f;
    
    [Tooltip("Frost damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float frostResistance = 0f;
    
    [Tooltip("Lightning damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float lightningResistance = 0f;
    
    [Tooltip("Light damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float lightResistance = 0f;
    
    [Tooltip("Dark damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float darkResistance = 0f;
    
    [Tooltip("Nature damage resistance (0-1, where 0.5 = 50% reduction)")]
    [Range(0f, 1f)]
    public float natureResistance = 0f;
    
    /// <summary>
    /// Copy stat values to a runtime StatContainer for compatibility with existing systems
    /// </summary>
    public void CopyToStatContainer(StatContainer destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("[EnemyStats] Cannot copy to null StatContainer!");
            return;
        }
        
        // Initialize destination from database first
        destination.InitializeFromDatabase();
        
        // Copy core stats
        destination.SetStat("MaxHealth", maxHealth);
        destination.SetStat("MoveSpeed", movementSpeed);
        destination.SetStat("AttackSpeed", attackSpeed);
        
        // Copy defense stats
        destination.SetStat("Armor", armor);
        destination.SetStat("ForceField", forceField);
        destination.SetStat("DodgeChance", dodgeChance);
        destination.SetStat("BlockChance", blockChance);
        
        // Copy resistances
        destination.SetStat("PiercingResistance", piercingResistance);
        destination.SetStat("SlashingResistance", slashingResistance);
        destination.SetStat("BludgeoningResistance", bludgeoningResistance);
        destination.SetStat("FireResistance", fireResistance);
        destination.SetStat("FrostResistance", frostResistance);
        destination.SetStat("LightningResistance", lightningResistance);
        destination.SetStat("LightResistance", lightResistance);
        destination.SetStat("DarkResistance", darkResistance);
        destination.SetStat("NatureResistance", natureResistance);
    }
}
