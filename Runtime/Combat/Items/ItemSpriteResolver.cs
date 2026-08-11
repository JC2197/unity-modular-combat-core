using UnityEngine;

/// <summary>
/// Static utility that resolves the inventory Sprite for any ItemInstance.
/// Mirrors the lookup logic in InventoryItemUI so other systems (e.g. pickup
/// notifications) can display the correct icon without coupling to inventory UI.
/// </summary>
public static class ItemSpriteResolver
{
    /// <summary>
    /// Returns the inventory sprite for the given item, or null if none is found.
    /// </summary>
    public static Sprite Resolve(ItemInstance item)
    {
        if (item == null) return null;

        switch (item.itemType.ToLower())
        {
            case "mapkey":
                return MapKeyConfig.Instance?.inventorySprite;

            case "material":
                return MaterialItemConfig.Resolve(item)?.inventorySprite;

            case "weapon":
                if (!string.IsNullOrEmpty(item.additionalData))
                {
                    WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(item.additionalData);
                    if (weaponData != null && !string.IsNullOrEmpty(weaponData.weaponConfigName))
                    {
                        WeaponItemDropsConfig weaponConfig = WeaponItemDropsConfig.DefaultInstance;
                        if (weaponConfig != null)
                        {
                            Sprite s = weaponConfig.GetInventorySpriteForWeapon(weaponData.weaponConfigName);
                            if (s != null) return s;
                        }
                    }
                }
                return WeaponItemDropsConfig.DefaultInstance?.inventorySprite;

            case "armor":
                if (!string.IsNullOrEmpty(item.additionalData))
                {
                    ArmorGearData armorData = JsonUtility.FromJson<ArmorGearData>(item.additionalData);
                    if (armorData != null && !string.IsNullOrEmpty(armorData.armorConfigName))
                    {
                        ArmorConfig armorConfig = ArmorConfigRegistry.GetConfig(armorData.armorConfigName);
                        if (armorConfig != null && armorConfig.inventorySprite != null)
                            return armorConfig.inventorySprite;
                    }
                }
                return ArmorItemDropsConfig.DefaultInstance?.inventorySprite;

            case "craftingorb":
                if (!string.IsNullOrEmpty(item.additionalData))
                {
                    CraftingOrbData orbData = JsonUtility.FromJson<CraftingOrbData>(item.additionalData);
                    if (orbData != null && !string.IsNullOrEmpty(orbData.configAssetName))
                    {
                        OrbItemConfig orbConfig = Resources.Load<OrbItemConfig>($"CraftingOrbs/{orbData.configAssetName}");
                        if (orbConfig != null && orbConfig.inventorySprite != null)
                            return orbConfig.inventorySprite;
                    }
                }
                return null;

            case "craftingtool":
                return ToolItemConfig.Resolve(item)?.inventorySprite;

            default:
                Debug.LogWarning($"[ItemSpriteResolver] Unknown item type: {item.itemType}");
                return null;
        }
    }
}
