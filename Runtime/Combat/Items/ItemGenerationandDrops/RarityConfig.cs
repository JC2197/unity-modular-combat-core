using UnityEngine;

/// <summary>
/// Global configuration for item rarity visual appearance.
/// Defines rarity names, colors, emissions, and shared particle systems.
/// All items reference this for consistent rarity presentation.
/// </summary>
[CreateAssetMenu(fileName = "RarityConfig", menuName = "Items/Rarity Config")]
public class RarityConfig : ScriptableObject
{
    [Header("Rarity Names")]
    [Tooltip("Rarity tier names (order matters: 0=Common, 1=Uncommon, etc.)")]
    public string[] rarityNames = new string[] { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
    
    [Header("Visual Settings")]
    [Tooltip("Color for each rarity tier")]
    public Color[] rarityColors = new Color[]
    {
        new Color(0.7f, 0.7f, 0.7f), // Common - Gray
        new Color(0.2f, 1f, 0.2f),   // Uncommon - Green
        new Color(0.2f, 0.5f, 1f),   // Rare - Blue
        new Color(0.8f, 0.2f, 1f),   // Epic - Purple
        new Color(1f, 0.6f, 0f),     // Legendary - Orange
        new Color(1f, 0.2f, 0.2f)    // Mythic - Red
    };
    
    [Tooltip("Particle emission rate for each rarity")]
    public float[] rarityEmission = new float[] { 2f, 5f, 10f, 15f, 25f, 40f };
    
    [Header("Generic Particle System")]
    [Tooltip("Particle system used by all items (color and emission modified by rarity)")]
    public ParticleSystem genericParticleSystem;
    
    // Singleton access
    private static RarityConfig instance;
    public static RarityConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<RarityConfig>("RarityConfig");
                if (instance == null)
                {
                    Debug.LogError("[RarityConfig] No RarityConfig found in Resources folder! Create one at Assets\\Resources\\RarityConfig.asset");
                }
            }
            return instance;
        }
    }
    
    /// <summary>
    /// Get rarity name for a tier
    /// </summary>
    public string GetRarityName(int tier)
    {
        if (tier < 0 || tier >= rarityNames.Length)
            return "Unknown";
        return rarityNames[tier];
    }
    
    /// <summary>
    /// Get rarity color for a tier
    /// </summary>
    public Color GetRarityColor(int tier)
    {
        if (tier < 0 || tier >= rarityColors.Length)
            return Color.white;
        return rarityColors[tier];
    }
    
    /// <summary>
    /// Get particle emission rate for a tier
    /// </summary>
    public float GetRarityEmission(int tier)
    {
        if (tier < 0 || tier >= rarityEmission.Length)
            return 5f;
        return rarityEmission[tier];
    }
}
