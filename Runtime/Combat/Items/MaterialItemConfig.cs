using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject for crafting materials such as wood, metal, glass, and stone.
/// One of the three crafting sub-types (Crafting → Material / Tool / Orb).
/// Keep these assets in a Resources folder so runtime UI and world items can resolve them.
/// </summary>
[CreateAssetMenu(fileName = "CraftingMaterial", menuName = "Items/Crafting Material Config")]
public class MaterialItemConfig : CraftingItemConfig
{
    private static readonly Dictionary<string, MaterialItemConfig> ConfigsById = new Dictionary<string, MaterialItemConfig>();
    private static readonly Dictionary<string, MaterialItemConfig> ConfigsByAssetName = new Dictionary<string, MaterialItemConfig>();
    private static bool cacheInitialized;

    public string MaterialId => ItemId;

    public override CraftingItemCategory Category => CraftingItemCategory.Material;

    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        return GenerateStack(1);
    }

    public ItemInstance GenerateStack(int stackSize)
    {
        ItemInstance item = CreateStackInstance(CraftingClassification.MaterialItemType, DisplayName, 0, stackSize);
        item.additionalData = JsonUtility.ToJson(new CraftingItemData
        {
            materialId = MaterialId,
            configAssetName = name
        });
        return item;
    }

    public static MaterialItemConfig GetById(string materialId)
    {
        EnsureCache();
        ConfigsById.TryGetValue(NormalizeMaterialId(materialId), out MaterialItemConfig config);
        return config;
    }

    public static MaterialItemConfig Resolve(ItemInstance item)
    {
        if (!CraftingItemUtility.IsMaterialItem(item))
            return null;

        EnsureCache();

        CraftingItemData data = CraftingItemUtility.GetMaterialData(item);
        if (data != null)
        {
            if (!string.IsNullOrWhiteSpace(data.configAssetName)
                && ConfigsByAssetName.TryGetValue(data.configAssetName, out MaterialItemConfig byAssetName))
            {
                return byAssetName;
            }

            if (!string.IsNullOrWhiteSpace(data.materialId)
                && ConfigsById.TryGetValue(NormalizeMaterialId(data.materialId), out MaterialItemConfig byId))
            {
                return byId;
            }
        }

        string fallbackId = NormalizeMaterialId(item.displayName);
        ConfigsById.TryGetValue(fallbackId, out MaterialItemConfig fallback);
        return fallback;
    }

    public static string NormalizeMaterialId(string value)
    {
        return NormalizeItemId(value);
    }

    private static void EnsureCache()
    {
        if (cacheInitialized)
            return;

        cacheInitialized = true;
        ConfigsById.Clear();
        ConfigsByAssetName.Clear();

        MaterialItemConfig[] allConfigs = Resources.LoadAll<MaterialItemConfig>(string.Empty);
        foreach (MaterialItemConfig config in allConfigs)
        {
            if (config == null)
                continue;

            string normalizedId = NormalizeMaterialId(config.ItemId);
            if (!string.IsNullOrEmpty(normalizedId))
                ConfigsById[normalizedId] = config;

            if (!string.IsNullOrEmpty(config.name))
                ConfigsByAssetName[config.name] = config;
        }
    }
}

[Serializable]
public class CraftingItemData
{
    public string materialId;
    public string configAssetName;
}

public static class CraftingItemUtility
{
    public static bool IsMaterialItem(ItemInstance item)
    {
        return item != null && string.Equals(item.itemType, CraftingClassification.MaterialItemType, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMaterial(ItemInstance item, string materialId)
    {
        if (!IsMaterialItem(item))
            return false;

        return string.Equals(GetMaterialId(item), MaterialItemConfig.NormalizeMaterialId(materialId), StringComparison.Ordinal);
    }

    public static CraftingItemData GetMaterialData(ItemInstance item)
    {
        if (!IsMaterialItem(item) || string.IsNullOrWhiteSpace(item.additionalData))
            return null;

        try
        {
            return JsonUtility.FromJson<CraftingItemData>(item.additionalData);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string GetMaterialId(ItemInstance item)
    {
        CraftingItemData data = GetMaterialData(item);
        if (data != null && !string.IsNullOrWhiteSpace(data.materialId))
            return MaterialItemConfig.NormalizeMaterialId(data.materialId);

        return MaterialItemConfig.NormalizeMaterialId(item?.displayName);
    }

    public static int CountMaterial(Dictionary<int, ItemInstance> slots, string materialId)
    {
        if (slots == null || slots.Count == 0)
            return 0;

        string normalizedId = MaterialItemConfig.NormalizeMaterialId(materialId);
        int total = 0;

        foreach (KeyValuePair<int, ItemInstance> kvp in slots)
        {
            ItemInstance item = kvp.Value;
            if (IsMaterial(item, normalizedId))
                total += Mathf.Max(0, item.stackSize);
        }

        return total;
    }

    public static int ConsumeMaterial(Dictionary<int, ItemInstance> slots, string materialId, int requestedAmount)
    {
        if (slots == null || requestedAmount <= 0)
            return 0;

        string normalizedId = MaterialItemConfig.NormalizeMaterialId(materialId);
        int remaining = requestedAmount;

        List<int> slotIndices = new List<int>(slots.Keys);
        slotIndices.Sort();

        foreach (int slotIndex in slotIndices)
        {
            if (!slots.TryGetValue(slotIndex, out ItemInstance item) || !IsMaterial(item, normalizedId))
                continue;

            int consumed = Mathf.Min(item.stackSize, remaining);
            item.stackSize -= consumed;
            remaining -= consumed;

            if (item.stackSize <= 0)
                slots.Remove(slotIndex);

            if (remaining <= 0)
                break;
        }

        return requestedAmount - remaining;
    }

    public static ItemInstance CreateMaterialItem(string materialId, int stackSize)
    {
        if (stackSize <= 0)
            return null;

        MaterialItemConfig config = MaterialItemConfig.GetById(materialId);
        if (config != null)
            return config.GenerateStack(stackSize);

        string normalizedId = MaterialItemConfig.NormalizeMaterialId(materialId);
        string displayName = string.IsNullOrEmpty(normalizedId)
            ? "Material"
            : char.ToUpperInvariant(normalizedId[0]) + normalizedId.Substring(1);

        ItemInstance item = new ItemInstance(CraftingClassification.MaterialItemType, displayName, 0, Mathf.Clamp(stackSize, 1, ItemInstance.MAX_STACK_SIZE));
        item.additionalData = JsonUtility.ToJson(new CraftingItemData
        {
            materialId = normalizedId,
            configAssetName = string.Empty
        });
        return item;
    }
}
