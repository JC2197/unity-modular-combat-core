using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Directly adds loot to the local player's inventory on enemy death.
/// Replaces world-item spawning for regular enemy kills — items go straight
/// into the player's CharacterData without requiring pickup.
/// </summary>
public static class LootRewarder
{
    /// <summary>
    /// Roll the drop tables for the given enemy and award any dropped items
    /// directly to the local player's inventory.
    /// </summary>
    public static void RewardLoot(EnemyConfig enemyConfig)
    {
        PlayerController player = PlayerController.GetLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("[LootRewarder] No local player found — cannot reward loot");
            return;
        }

        CharacterData characterData = player.GetCurrentCharacterData();
        if (characterData == null)
        {
            Debug.LogWarning("[LootRewarder] Local player has no CharacterData — cannot reward loot");
            return;
        }

        List<RewardEntry> rewards = new List<RewardEntry>();
        int maxDrops = enemyConfig != null ? enemyConfig.maxDrops : 3;

        // Phase 1: Roll universal drop table
        UniversalDropTable universalTable = UniversalDropTable.Instance;
        if (universalTable != null && universalTable.universalDrops.Count > 0)
        {
            List<RewardEntry> universalRewards = RollDropTable(
                universalTable.universalDrops,
                maxDrops,
                universalTable.globalDropChance
            );
            rewards.AddRange(universalRewards);
            Debug.Log($"[LootRewarder] Universal table rolled {universalRewards.Count} item(s)");
        }

        // Phase 2: Roll enemy-specific drop table
        if (enemyConfig != null && enemyConfig.dropTable != null && enemyConfig.dropTable.Count > 0)
        {
            int remainingSlots = maxDrops - rewards.Count;
            if (remainingSlots > 0)
            {
                List<RewardEntry> enemyRewards = RollDropTable(
                    enemyConfig.dropTable,
                    remainingSlots,
                    1f
                );
                rewards.AddRange(enemyRewards);
                Debug.Log($"[LootRewarder] Enemy-specific table rolled {enemyRewards.Count} item(s)");
            }
        }

        if (rewards.Count == 0)
        {
            Debug.Log("[LootRewarder] No items rolled");
            return;
        }

        // Phase 3: Add directly to inventory
        bool anyAdded = false;
        foreach (var reward in rewards)
        {
            bool added = characterData.AddItemToInventory(reward.item);
            if (added)
            {
                Debug.Log($"[LootRewarder] ✓ Added {reward.item.displayName} x{reward.quantity} to {characterData.characterName}'s inventory");
                ItemPickupHUD.ShowPickup(reward.item.displayName, ItemSpriteResolver.Resolve(reward.item), reward.item.stackSize);
                anyAdded = true;
            }
            else
            {
                Debug.LogWarning($"[LootRewarder] Inventory full — could not add {reward.item.displayName}");
            }
        }

        if (anyAdded)
        {
            CharacterPersistence.SaveCharacter(characterData);
            InventoryManager.RefreshInventoryDisplay();
        }
    }

    private static List<RewardEntry> RollDropTable(List<DropTableEntry> dropTable, int maxDrops, float globalChanceModifier = 1f)
    {
        List<RewardEntry> rewards = new List<RewardEntry>();

        foreach (var entry in dropTable)
        {
            if (entry.itemConfig == null) continue;

            float modifiedChance = entry.dropChance * globalChanceModifier;
            float roll = Random.value;

            if (roll <= modifiedChance)
            {
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                ItemInstance item = ItemGenerator.GenerateFromConfig(entry.itemConfig, 1);

                if (item != null)
                {
                    rewards.Add(new RewardEntry { item = item, quantity = quantity });
                    Debug.Log($"[LootRewarder] Rolled {item.displayName} x{quantity} (chance {modifiedChance * 100f:F1}%, roll {roll * 100f:F1}%)");

                    if (rewards.Count >= maxDrops)
                        break;
                }
            }
        }

        return rewards;
    }

    private struct RewardEntry
    {
        public ItemInstance item;
        public int quantity;
    }
}
