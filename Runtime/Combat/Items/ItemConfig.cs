using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Base configuration for all items in the game.
/// Holds shared properties like rarity settings and sprites.
/// Create one of these and reference it globally.
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Items/Item Config")]
public class ItemConfig : ScriptableObject
{
    [Tooltip("Minimum map tier required for this item to drop (I-VI)")]
    public ItemTier baseTierAvailable = ItemTier.I;

    [TextArea(3, 5)]
    public string description = "No description available.";
    /// <summary>
    /// Generate a procedural item instance. Override in subclasses for specific item types.
    /// </summary>
    public virtual ItemInstance GenerateItem(int contextLevel = 1)
    {
        Debug.LogWarning($"[ItemConfig] GenerateItem() not implemented for {name}. Override this method in subclasses.");
        return null;
    }

    /// <summary>
    /// Rarity source for this item. Null means the item carries no rarity (e.g. crafting items).
    /// Rarity-bearing items provide one via <see cref="RarityItemConfig"/>.
    /// </summary>
    protected virtual RarityConfig ResolvedRarityConfig => null;

    /// <summary>
    /// Get color for a specific rarity tier. Returns white for items without rarity.
    /// </summary>
    public Color GetRarityColor(int rarityTier)
    {
        RarityConfig rarity = ResolvedRarityConfig;
        return rarity != null ? rarity.GetRarityColor(rarityTier) : Color.white;
    }

    /// <summary>
    /// Get emission rate for a specific rarity tier. Returns a neutral default for items without rarity.
    /// </summary>
    public float GetRarityEmission(int rarityTier)
    {
        RarityConfig rarity = ResolvedRarityConfig;
        return rarity != null ? rarity.GetRarityEmission(rarityTier) : 5f;
    }

    /// <summary>
    /// Get rarity name for a specific tier. Returns "Common" for items without rarity.
    /// </summary>
    public string GetRarityName(int rarityTier)
    {
        RarityConfig rarity = ResolvedRarityConfig;
        return rarity != null ? rarity.GetRarityName(rarityTier) : "Common";
    }

    /// <summary>
    /// Roll a random rarity. Items without rarity always roll common (0).
    /// Override in <see cref="RarityItemConfig"/> to roll based on per-tier chances.
    /// </summary>
    public virtual int RollRandomRarity()
    {
        return 0;
    }
}

/// <summary>
/// Runtime instance of an item in inventory or world.
/// Holds generated/procedural item data.
/// </summary>
[System.Serializable]
public class ItemInstance
{
    /// <summary>Maximum items allowed in one inventory stack.</summary>
    public const int MAX_STACK_SIZE = 64;

    [Tooltip("Item type identifier (e.g., 'mapkey', 'potion')")]
    public string itemType;

    [Tooltip("Unique instance identifier")]
    public string instanceID;

    [Tooltip("Display name of this item")]
    public string displayName;

    [Tooltip("Description of this item (shown in tooltip)")]
    public string description;

    [Tooltip("Rarity tier (0=Common, 1=Uncommon, etc.)")]
    public int rarityTier;

    [Tooltip("Number of items in this stack")]
    public int stackSize = 1;

    [Tooltip("Slot index in inventory (-1 = not in inventory)")]
    public int slotIndex = -1;

    [Tooltip("Additional data specific to item type (JSON string)")]
    public string additionalData;

    /// <summary>
    /// Constructor for creating new item instances
    /// </summary>
    public ItemInstance(string itemType, string displayName, int rarityTier, int stackSize = 1)
    {
        this.itemType = itemType;
        this.instanceID = System.Guid.NewGuid().ToString();
        this.displayName = displayName;
        this.rarityTier = rarityTier;
        this.stackSize = stackSize;
    }

    /// <summary>
    /// Returns true for item types that can stack in a single inventory slot.
    /// </summary>
    public bool IsStackable()
    {
        return !string.IsNullOrEmpty(GetStackKey());
    }

    /// <summary>
    /// Unique identity used to decide whether two item instances belong to the same stack.
    /// Empty means the item is non-stackable.
    /// </summary>
    public string GetStackKey()
    {
        switch (itemType?.ToLowerInvariant())
        {
            case "craftingorb":
                if (!string.IsNullOrWhiteSpace(additionalData))
                {
                    try
                    {
                        CraftingOrbData orbData = JsonUtility.FromJson<CraftingOrbData>(additionalData);
                        if (!string.IsNullOrWhiteSpace(orbData?.configAssetName))
                            return $"craftingorb:{orbData.configAssetName}";
                    }
                    catch (Exception)
                    {
                    }
                }
                return $"craftingorb:{displayName}";

            case "mapkey":
                return $"mapkey:{displayName}";

            case "material":
                string materialId = CraftingItemUtility.GetMaterialId(this);
                return string.IsNullOrWhiteSpace(materialId) ? string.Empty : $"material:{materialId}";

            case "craftingtool":
                if (!string.IsNullOrWhiteSpace(additionalData))
                {
                    try
                    {
                        ToolItemData toolData = JsonUtility.FromJson<ToolItemData>(additionalData);
                        if (!string.IsNullOrWhiteSpace(toolData?.configAssetName))
                            return $"craftingtool:{toolData.configAssetName}";
                        if (!string.IsNullOrWhiteSpace(toolData?.toolId))
                            return $"craftingtool:{toolData.toolId}";
                    }
                    catch (Exception)
                    {
                    }
                }
                return $"craftingtool:{displayName}";

            default:
                return string.Empty;
        }
    }

    public int GetMaxStackSize()
    {
        return IsStackable() ? MAX_STACK_SIZE : 1;
    }

    public int GetAvailableStackSpace()
    {
        return Mathf.Max(0, GetMaxStackSize() - Mathf.Max(0, stackSize));
    }

    /// <summary>
    /// Returns true when this item can be combined into the same stack as <paramref name="other"/>.
    /// Both must be stackable and share the same stack identity.
    /// </summary>
    public bool CanStackWith(ItemInstance other)
    {
        if (other == null)
            return false;

        return IsStackable()
            && other.IsStackable()
            && string.Equals(GetStackKey(), other.GetStackKey(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Moves as many items as possible from <paramref name="source"/> into this stack.
    /// Returns the number of items moved.
    /// </summary>
    public int MergeFrom(ItemInstance source)
    {
        if (source == null || !CanStackWith(source))
            return 0;

        int toMove = Mathf.Min(source.stackSize, GetAvailableStackSpace());
        if (toMove <= 0)
            return 0;

        stackSize += toMove;
        source.stackSize -= toMove;
        return toMove;
    }

    /// <summary>
    /// Creates a new item instance carrying the same stack identity and data.
    /// </summary>
    public ItemInstance CreateStackCopy(int newStackSize)
    {
        ItemInstance copy = new ItemInstance(itemType, displayName, rarityTier, Mathf.Clamp(newStackSize, 1, GetMaxStackSize()))
        {
            description = description,
            additionalData = additionalData
        };

        return copy;
    }

    /// <summary>
    /// Get the full display name with rarity
    /// </summary>
    public string GetFullDisplayName()
    {
        // Try to get config based on item type
        ItemConfig config = GetConfigForItemType(itemType);
        if (config != null)
        {
            return $"{config.GetRarityName(rarityTier)} {displayName}";
        }
        return displayName;
    }

    /// <summary>
    /// Helper to get ItemConfig based on item type
    /// </summary>
    private static ItemConfig GetConfigForItemType(string itemType)
    {
        switch (itemType?.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance;

            case "material":
                return null;

            default:
                return null;
        }
    }
}