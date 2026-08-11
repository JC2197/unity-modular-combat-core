using System;

/// <summary>
/// The sub-categories that live under the shared "Crafting" item parentage.
/// Mirrors the in-game grouping: Crafting → Material / Tool / Orb.
/// Stone, glass, wood, etc. are <see cref="Material"/>; upgrade orbs are
/// <see cref="Orb"/>; crafting tools are <see cref="Tool"/>.
/// </summary>
public enum CraftingItemCategory
{
    Material,
    Tool,
    Orb
}

/// <summary>
/// Single source of truth for the <see cref="ItemInstance.itemType"/> strings used by
/// crafting items, plus helpers to classify any item into its <see cref="CraftingItemCategory"/>.
/// Use these constants instead of raw string literals so all crafting code stays in sync.
/// </summary>
public static class CraftingClassification
{
    /// <summary>itemType for materials (stone, glass, wood, metal, ...).</summary>
    public const string MaterialItemType = "material";

    /// <summary>itemType for crafting tools.</summary>
    public const string ToolItemType = "craftingtool";

    /// <summary>itemType for upgrade orbs.</summary>
    public const string OrbItemType = "craftingorb";

    /// <summary>True when the item belongs to the Crafting parentage (Material, Tool, or Orb).</summary>
    public static bool IsCraftingItem(ItemInstance item)
    {
        return TryGetCategory(item, out _);
    }

    /// <summary>True when the item is a crafting item of the requested category.</summary>
    public static bool IsCategory(ItemInstance item, CraftingItemCategory category)
    {
        return TryGetCategory(item, out CraftingItemCategory resolved) && resolved == category;
    }

    /// <summary>
    /// Resolves the crafting category for an item from its <see cref="ItemInstance.itemType"/>.
    /// Returns false (and Material as the out default) for non-crafting items.
    /// </summary>
    public static bool TryGetCategory(ItemInstance item, out CraftingItemCategory category)
    {
        category = CraftingItemCategory.Material;

        if (item == null || string.IsNullOrEmpty(item.itemType))
            return false;

        if (item.itemType.Equals(MaterialItemType, StringComparison.OrdinalIgnoreCase))
        {
            category = CraftingItemCategory.Material;
            return true;
        }

        if (item.itemType.Equals(ToolItemType, StringComparison.OrdinalIgnoreCase))
        {
            category = CraftingItemCategory.Tool;
            return true;
        }

        if (item.itemType.Equals(OrbItemType, StringComparison.OrdinalIgnoreCase))
        {
            category = CraftingItemCategory.Orb;
            return true;
        }

        return false;
    }
}
