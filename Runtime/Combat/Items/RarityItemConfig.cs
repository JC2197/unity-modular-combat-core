using UnityEngine;

/// <summary>
/// Base config for items that carry rarity: a rarity configuration plus per-tier
/// drop chances. Gear and other rarity-bearing items extend this.
/// Stackable / crafting items deliberately do NOT, so they never require rarity data.
/// </summary>
public abstract class RarityItemConfig : ItemConfig
{
    [Header("Rarity")]
    [Tooltip("Reference to global rarity configuration (names, colors, particles)")]
    public RarityConfig rarityConfig;

    [Tooltip("Base drop chance for each rarity tier (0-1). Item-specific!")]
    public float[] rarityBaseChance = new float[] { 0.6f, 0.25f, 0.1f, 0.04f, 0.009f, 0.001f };

    protected override RarityConfig ResolvedRarityConfig => rarityConfig;

    /// <summary>
    /// Roll a random rarity tier based on this item's per-tier base chances.
    /// </summary>
    public override int RollRandomRarity()
    {
        float roll = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < rarityBaseChance.Length; i++)
        {
            cumulative += rarityBaseChance[i];
            if (roll <= cumulative)
                return i;
        }

        return 0; // Default to common
    }
}
