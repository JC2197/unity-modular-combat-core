using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Configuration for a single crafting orb. One of the three crafting sub-types
/// (Crafting → Material / Tool / Orb).
/// Place OrbItemConfig assets in a Resources/CraftingOrbs/ folder so UI and world
/// items can resolve them at runtime by the config's asset name.
/// </summary>
[CreateAssetMenu(fileName = "CraftingOrbConfig", menuName = "Items/Crafting Orb Config")]
public class OrbItemConfig : CraftingItemConfig
{
    [Header("Orb")]
    [Tooltip("Color type that determines which gear modifiers this orb biases toward (e.g. Red orb → Red-type modifiers).")]
    [FormerlySerializedAs("orbType")]
    public GearColorType colorType;

    [Tooltip("Color theme from TagDatabase (used in UI/tooltips)")]
    [TagDropdown]
    public string colorTheme = "";

    public override CraftingItemCategory Category => CraftingItemCategory.Orb;

    /// <summary>Returns the UI color for this orb, sourced from TagDatabase via colorTheme.</summary>
    public Color GetOrbColor()
    {
        if (!string.IsNullOrEmpty(colorTheme) && TagDatabase.Instance != null)
            return TagDatabase.Instance.GetPrimaryColor(colorTheme);
        return Color.white;
    }

    private void OnValidate()
    {
        itemId = colorType.ToString();
        if (string.IsNullOrWhiteSpace(itemDisplayName) || itemDisplayName == "Item")
            itemDisplayName = colorType + " Orb";
    }

    public override ItemInstance GenerateItem(int contextLevel = 1)
    {
        string colorTypeName = colorType.ToString();
        itemId = colorTypeName;
        if (string.IsNullOrWhiteSpace(itemDisplayName) || itemDisplayName == "Item")
            itemDisplayName = colorTypeName + " Orb";

        // Crafting orbs are always common appearance (color comes from orb color type, not item tier)
        ItemInstance item = CreateStackInstance(CraftingClassification.OrbItemType, DisplayName, 0, 1);
        item.additionalData = JsonUtility.ToJson(new CraftingOrbData
        {
            orbTypeIndex = (int)colorType,
            orbTypeName = colorTypeName,
            configAssetName = name   // SO asset name, used for Resources.Load lookup
        });
        return item;
    }
}

/// <summary>
/// JSON payload stored in ItemInstance.additionalData for crafting orbs.
/// </summary>
[System.Serializable]
public class CraftingOrbData
{
    public int orbTypeIndex;
    public string orbTypeName;
    /// <summary>Name of the OrbItemConfig asset (used for Resources.Load)</summary>
    public string configAssetName;
}

