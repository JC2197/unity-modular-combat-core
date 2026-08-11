using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract base for every crafting item. The parentage is:
/// Crafting → Material / Tool / Orb.
///
/// Provides a normalized identity (itemId + display name) and the crafting
/// sub-category. Crafting items are stackable and carry NO rarity.
/// </summary>
public abstract class CraftingItemConfig : StackableItemConfig
{
    [Header("Crafting Identity")]
    [FormerlySerializedAs("materialId")]
    [SerializeField] protected string itemId = "item";

    [FormerlySerializedAs("materialDisplayName")]
    [SerializeField] protected string itemDisplayName = "Item";

    public string ItemId => NormalizeItemId(itemId);
    public string DisplayName => string.IsNullOrWhiteSpace(itemDisplayName) ? name : itemDisplayName.Trim();

    /// <summary>Which crafting sub-category (Material, Tool, or Orb) this config produces.</summary>
    public abstract CraftingItemCategory Category { get; }

    protected static string NormalizeItemId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
