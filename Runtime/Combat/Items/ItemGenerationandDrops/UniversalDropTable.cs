using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal drop table shared across all enemies.
/// Contains common items that any enemy can drop.
/// </summary>
[CreateAssetMenu(fileName = "UniversalDropTable", menuName = "Items/Universal Drop Table")]
public class UniversalDropTable : ScriptableObject
{
    [Header("World Item Prefab")]
    [Tooltip("The prefab to use when spawning dropped items in the world")]
    public GameObject worldItemPrefab;
    
    [Header("Universal Drops")]
    [Tooltip("Items that all enemies can drop")]
    public List<DropTableEntry> universalDrops = new List<DropTableEntry>();
    
    [Header("Drop Settings")]
    [Tooltip("Global chance modifier for universal drops (0-1)")]
    [Range(0f, 1f)]
    public float globalDropChance = 1f;
    
    [Tooltip("Should universal drops count towards enemy maxDrops limit?")]
    public bool countsTowardsMaxDrops = true;
    
    // Singleton access
    private static UniversalDropTable instance;
    public static UniversalDropTable Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UniversalDropTable>("UniversalDropTable");
                if (instance == null)
                {
                    Debug.LogWarning("[UniversalDropTable] No UniversalDropTable found in Resources folder. Create one at Assets\\Resources\\UniversalDropTable.asset");
                }
            }
            return instance;
        }
    }
}
