using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject for crafting tools used in the Upgrade tab.
/// Tools sit under the Crafting parentage alongside materials and orbs
/// (Crafting → Material / Tool / Orb) and are identified by a free-form id,
/// the same way stone/glass identify themselves as materials.
///
/// Keep these assets in a Resources/CraftingTools/ folder so runtime UI and
/// world items can resolve them by id or asset name.
/// </summary>
[CreateAssetMenu(fileName = "CraftingTool", menuName = "Items/Crafting Tool Config")]
public class ToolItemConfig : CraftingItemConfig
{
    private static readonly Dictionary<string, ToolItemConfig> ConfigsById = new Dictionary<string, ToolItemConfig>();
    private static readonly Dictionary<string, ToolItemConfig> ConfigsByAssetName = new Dictionary<string, ToolItemConfig>();
    private static bool cacheInitialized;

    [Header("Tool")]
    [Tooltip("If true, this tool accepts an item slotted into it (of the type below).")]
    public bool slottable;

    [Tooltip("What kind of item can be slotted into this tool when Slottable is enabled.")]
    public ToolSlotType slotType;

    [Tooltip("One or more upgrade operations this tool can perform. " +
             "Each operation independently validates and executes; " +
             "the first CanApply() that returns true will be used by the Craft button.")]
    public UpgradeOperation[] operations = new UpgradeOperation[0];

    public string ToolId => ItemId;

    public override CraftingItemCategory Category => CraftingItemCategory.Tool;

    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        return GenerateStack(1);
    }

    public ItemInstance GenerateStack(int stackSize)
    {
        ItemInstance item = CreateStackInstance(CraftingClassification.ToolItemType, DisplayName, 0, stackSize);
        item.additionalData = JsonUtility.ToJson(new ToolItemData
        {
            toolId = ToolId,
            configAssetName = name
        });
        return item;
    }

    public static ToolItemConfig GetById(string toolId)
    {
        EnsureCache();
        ConfigsById.TryGetValue(NormalizeItemId(toolId), out ToolItemConfig config);
        return config;
    }

    public static ToolItemConfig Resolve(ItemInstance item)
    {
        if (!ToolItemUtility.IsToolItem(item))
            return null;

        EnsureCache();

        ToolItemData data = ToolItemUtility.GetToolData(item);
        if (data != null)
        {
            if (!string.IsNullOrWhiteSpace(data.configAssetName)
                && ConfigsByAssetName.TryGetValue(data.configAssetName, out ToolItemConfig byAssetName))
            {
                return byAssetName;
            }

            if (!string.IsNullOrWhiteSpace(data.toolId)
                && ConfigsById.TryGetValue(NormalizeItemId(data.toolId), out ToolItemConfig byId))
            {
                return byId;
            }
        }

        string fallbackId = NormalizeItemId(item.displayName);
        ConfigsById.TryGetValue(fallbackId, out ToolItemConfig fallback);
        return fallback;
    }

    private static void EnsureCache()
    {
        if (cacheInitialized)
            return;

        cacheInitialized = true;
        ConfigsById.Clear();
        ConfigsByAssetName.Clear();

        ToolItemConfig[] allConfigs = Resources.LoadAll<ToolItemConfig>(string.Empty);
        foreach (ToolItemConfig config in allConfigs)
        {
            if (config == null)
                continue;

            string normalizedId = NormalizeItemId(config.ItemId);
            if (!string.IsNullOrEmpty(normalizedId))
                ConfigsById[normalizedId] = config;

            if (!string.IsNullOrEmpty(config.name))
                ConfigsByAssetName[config.name] = config;
        }
    }
}

[Serializable]
public class ToolItemData
{
    public string toolId;
    public string configAssetName;
}

/// <summary>
/// The kind of item a slottable tool accepts.
/// </summary>
public enum ToolSlotType
{
    Orb,
    Item
}

public static class ToolItemUtility
{
    public static bool IsToolItem(ItemInstance item)
    {
        return CraftingClassification.IsCategory(item, CraftingItemCategory.Tool);
    }

    public static ToolItemData GetToolData(ItemInstance item)
    {
        if (!IsToolItem(item) || string.IsNullOrWhiteSpace(item.additionalData))
            return null;

        try
        {
            return JsonUtility.FromJson<ToolItemData>(item.additionalData);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
