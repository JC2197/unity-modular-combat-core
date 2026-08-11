using UnityEngine;
using System.Collections.Generic;
using FishNet;

/// <summary>
/// Handles spawning item drops when enemies die.
/// Network-spawns items so they're visible to all players in multiplayer.
/// Attach this to enemies or call statically from Enemy.cs
/// </summary>
public static class LootDropper
{
    /// <summary>
    /// Process drop table and spawn items at enemy death position
    /// Uses hybrid system: Universal drops + Enemy-specific drops
    /// </summary>
    public static void DropLoot(EnemyConfig enemyConfig, Vector3 dropPosition)
    {
        List<ItemDrop> allDrops = new List<ItemDrop>();
        int maxDrops = enemyConfig != null ? enemyConfig.maxDrops : 3;
        
        // PHASE 1: Roll universal drop table
        UniversalDropTable universalTable = UniversalDropTable.Instance;
        if (universalTable != null && universalTable.universalDrops.Count > 0)
        {
            Debug.Log($"[LootDropper] Rolling universal drop table with {universalTable.universalDrops.Count} entries");
            List<ItemDrop> universalDrops = RollDropTable(
                universalTable.universalDrops, 
                maxDrops,
                universalTable.globalDropChance
            );
            allDrops.AddRange(universalDrops);
            
            Debug.Log($"[LootDropper] Universal table rolled {universalDrops.Count} items");
        }
        
        // PHASE 2: Roll enemy-specific drop table
        if (enemyConfig != null && enemyConfig.dropTable != null && enemyConfig.dropTable.Count > 0)
        {
            int remainingSlots = maxDrops - allDrops.Count;
            if (remainingSlots > 0)
            {
                Debug.Log($"[LootDropper] Rolling enemy-specific table with {enemyConfig.dropTable.Count} entries ({remainingSlots} slots remaining)");
                List<ItemDrop> enemyDrops = RollDropTable(
                    enemyConfig.dropTable,
                    remainingSlots,
                    1f // No global modifier for enemy-specific drops
                );
                allDrops.AddRange(enemyDrops);
                
                Debug.Log($"[LootDropper] Enemy table rolled {enemyDrops.Count} items");
            }
            else
            {
                Debug.Log($"[LootDropper] Max drops reached from universal table, skipping enemy-specific drops");
            }
        }
        
        // PHASE 3: Spawn all dropped items
        if (allDrops.Count == 0)
        {
            Debug.Log($"[LootDropper] No items rolled from any drop table");
            return;
        }
        
        Debug.Log($"[LootDropper] Dropping {allDrops.Count} total items at {dropPosition}");
        foreach (var drop in allDrops)
        {
            SpawnWorldItem(drop.item, drop.quantity, dropPosition);
        }
    }
    
    /// <summary>
    /// Roll the drop table and determine which items drop
    /// </summary>
    /// <param name="dropTable">The table to roll</param>
    /// <param name="maxDrops">Maximum items that can drop</param>
    /// <param name="globalChanceModifier">Multiplier for all drop chances (0-1)</param>
    private static List<ItemDrop> RollDropTable(List<DropTableEntry> dropTable, int maxDrops, float globalChanceModifier = 1f)
    {
        List<ItemDrop> drops = new List<ItemDrop>();
        
        foreach (var entry in dropTable)
        {
            if (entry.itemConfig == null) continue;
            
            // Apply global chance modifier
            float modifiedChance = entry.dropChance * globalChanceModifier;
            
            // Roll for drop chance
            float roll = Random.value;
            if (roll <= modifiedChance)
            {
                // Determine quantity
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                
                // Generate item using ItemGenerator
                ItemInstance generatedItem = ItemGenerator.GenerateFromConfig(entry.itemConfig, 1); // TODO: Pass current map level
                
                if (generatedItem != null)
                {
                    drops.Add(new ItemDrop
                    {
                        item = generatedItem,
                        quantity = quantity
                    });
                    
                    Debug.Log($"[LootDropper] ✓ Rolled {generatedItem.displayName} x{quantity} (chance: {modifiedChance * 100:F1}%, rolled: {roll * 100:F1}%)");
                }
                
                // Check max drops limit
                if (drops.Count >= maxDrops)
                    break;
            }
        }
        
        return drops;
    }
    

    
    /// <summary>
    /// Spawn a world item at the specified position
    /// </summary>
    private static void SpawnWorldItem(ItemInstance item, int quantity, Vector3 position)
    {
        // Create world item (either from prefab or generate one)
        GameObject worldItemObj = CreateWorldItemObject(position);
        
        if (worldItemObj == null)
        {
            Debug.LogError("[LootDropper] Failed to create world item object!");
            return;
        }
        
        // Get or add WorldItem component
        WorldItem worldItem = worldItemObj.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = worldItemObj.AddComponent<WorldItem>();
        }
        
        // Initialize the world item with the item instance
        worldItem.Initialize(item);
        
        // Add NetworkObject for multiplayer support if not already present
        FishNet.Object.NetworkObject networkObject = worldItemObj.GetComponent<FishNet.Object.NetworkObject>();
        if (networkObject == null)
        {
            networkObject = worldItemObj.AddComponent<FishNet.Object.NetworkObject>();
        }
        
        // Add random offset so items don't stack on top of each other
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.3f, 0.3f),
            0f
        );
        worldItemObj.transform.position += randomOffset;
        
        // Network spawn the item if server is active
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null && networkManager.IsServerStarted)
        {
            networkManager.ServerManager.Spawn(worldItemObj);
            Debug.Log($"[LootDropper] Network-spawned {item.displayName} x{quantity} at {worldItemObj.transform.position}");
        }
        else
        {
            Debug.Log($"[LootDropper] Spawned {item.displayName} x{quantity} at {worldItemObj.transform.position} (no network - server not active)");
        }
    }
    
    /// <summary>
    /// Create a world item GameObject (from prefab or generated)
    /// </summary>
    private static GameObject CreateWorldItemObject(Vector3 position)
    {
        GameObject worldItemObj;
        
        // Get prefab from UniversalDropTable
        GameObject worldItemPrefab = UniversalDropTable.Instance?.worldItemPrefab;
        
        if (worldItemPrefab != null)
        {
            // Use prefab
            worldItemObj = Object.Instantiate(worldItemPrefab, position, Quaternion.identity);
        }
        else
        {
            // Generate simple world item (fallback if no prefab assigned)
            Debug.LogWarning("[LootDropper] No worldItemPrefab assigned in UniversalDropTable! Using fallback generation.");
            worldItemObj = new GameObject("WorldItem");
            worldItemObj.transform.position = position;
            worldItemObj.layer = LayerMask.NameToLayer("Item");
            
            // Add SpriteRenderer
            SpriteRenderer sr = worldItemObj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Item";
            sr.sortingOrder = 5;
            
            // Add CircleCollider2D for pickup
            CircleCollider2D collider = worldItemObj.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            
            // WorldItem will add particle system and light in SetupParticleSystem()
            
            Debug.Log("[LootDropper] Generated world item object (no prefab assigned)");
        }
        
        return worldItemObj;
    }
    
    /// <summary>
    /// Simple struct to hold dropped items
    /// </summary>
    private struct ItemDrop
    {
        public ItemInstance item;
        public int quantity;
    }
}
