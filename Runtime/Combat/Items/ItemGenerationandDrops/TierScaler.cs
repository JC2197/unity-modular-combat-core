using UnityEngine;

/// <summary>
/// Utility class for applying tier scaling to values (both fixed and randomized).
/// Centralizes tier scaling logic for items, traits, and any other tier-based systems.
/// </summary>
public static class TierScaler
{

    private static TierScalingConfig cachedConfig;
    /// <summary>
    /// Scale a fixed value by the tier multiplier
    /// </summary>
    /// <param name="baseValue">The base value to scale (typically a Tier I value)</param>
    /// <param name="tier">The item tier to scale to</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>The scaled value</returns>
    public static float ScaleValue(float baseValue, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning unscaled value");
            return baseValue;
        }

        float multiplier = tierConfig.GetMultiplier(tier);
        return baseValue * multiplier;
    }

    /// <summary>
    /// Scale a random value within a range by the tier multiplier
    /// </summary>
    /// <param name="minValue">Minimum value of the range (Tier I)</param>
    /// <param name="maxValue">Maximum value of the range (Tier I)</param>
    /// <param name="tier">The item tier to scale to</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>A random value within the scaled range</returns>
    public static float ScaleRandomValue(float minValue, float maxValue, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning unscaled random value");
            return Random.Range(minValue, maxValue);
        }

        float multiplier = tierConfig.GetMultiplier(tier);
        float scaledMin = minValue * multiplier;
        float scaledMax = maxValue * multiplier;

        return Random.Range(scaledMin, scaledMax);
    }

    /// <summary>
    /// Scale an integer value by the tier multiplier (rounds to nearest int)
    /// </summary>
    /// <param name="baseValue">The base value to scale (typically a Tier I value)</param>
    /// <param name="tier">The item tier to scale to</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>The scaled integer value</returns>
    public static int ScaleValueInt(int baseValue, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        return Mathf.RoundToInt(ScaleValue(baseValue, tier, tierConfig));
    }

    /// <summary>
    /// Scale a random integer within a range by the tier multiplier
    /// </summary>
    /// <param name="minValue">Minimum value of the range (Tier I)</param>
    /// <param name="maxValue">Maximum value of the range (Tier I inclusive)</param>
    /// <param name="tier">The item tier to scale to</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>A random integer within the scaled range</returns>
    public static int ScaleRandomValueInt(int minValue, int maxValue, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning unscaled random value");
            return Random.Range(minValue, maxValue + 1);
        }

        float multiplier = tierConfig.GetMultiplier(tier);
        int scaledMin = Mathf.RoundToInt(minValue * multiplier);
        int scaledMax = Mathf.RoundToInt(maxValue * multiplier);

        return Random.Range(scaledMin, scaledMax + 1);
    }

    /// <summary>
    /// Get the multiplier for a specific tier
    /// </summary>
    /// <param name="tier">The item tier</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>The multiplier for the tier (e.g., 1.0 for Tier I, 1.5 for Tier II)</returns>
    public static float GetMultiplier(ItemTier tier, TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning 1.0");
            return 1.0f;
        }

        return tierConfig.GetMultiplier(tier);
    }

    /// <summary>
    /// Load the default tier scaling config from Resources
    /// </summary>
    private static TierScalingConfig LoadDefaultTierConfig()
    {
        if (cachedConfig == null)
        {
            cachedConfig = Resources.Load<TierScalingConfig>("TierScaling");
            if (cachedConfig == null)
            {
                Debug.LogWarning("[TierScaler] No default TierScaling config found in Resources folder! Create one at Assets/Resources/TierScaling.asset");
            }
        }
        return cachedConfig;
    }

    public static void ClearCache()
    {
        cachedConfig = null;
    }



    /// <summary>
    /// Calculate what the scaled value would be at a different tier (for preview/comparison)
    /// </summary>
    /// <param name="currentValue">The value at the current tier</param>
    /// <param name="currentTier">The current tier</param>
    /// <param name="targetTier">The tier to scale to</param>
    /// <param name="tierConfig">Optional tier scaling config (loads default if null)</param>
    /// <returns>The value as it would be at the target tier</returns>
    public static float ConvertBetweenTiers(float currentValue, ItemTier currentTier, ItemTier targetTier, TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning unscaled value");
            return currentValue;
        }

        float currentMultiplier = tierConfig.GetMultiplier(currentTier);
        float targetMultiplier = tierConfig.GetMultiplier(targetTier);

        // Convert back to base (Tier I), then scale to target
        float baseValue = currentValue / currentMultiplier;
        return baseValue * targetMultiplier;
    }

    public static ItemTier RollTier(TierScalingConfig tierConfig = null)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            Debug.LogWarning("[TierScaler] No tier config available, returning Tier I");
            return ItemTier.I;
        }

        float totalWeight = tierConfig.GetTotalRollWeight();
        if (totalWeight <= 0f)
        {
            return ItemTier.I;
        }
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var entry in tierConfig.tierScalingEntries)
        {
            cumulative += entry.rollWeight;
            if (roll < cumulative)
            {
                return entry.tier;
            }
        }
        return ItemTier.I;
    }

    public static float GetTierRollProbability(ItemTier tier, TierScalingConfig tierConfig)
    {
        if (tierConfig == null)
        {
            tierConfig = LoadDefaultTierConfig();
        }

        if (tierConfig == null)
        {
            return tier == ItemTier.I ? 1f : 0f;
        }

        float totalWeight = tierConfig.GetTotalRollWeight();
        if (totalWeight <= 0f)
        {
            return tier == ItemTier.I ? 1f : 0f;
        }
        return tierConfig.GetRollWeight(tier) / totalWeight;
    }

    public static ValueRange ScaleRange(ValueRange baseRange, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        float multiplier = GetMultiplier(tier, tierConfig);
        return new ValueRange(baseRange.min * multiplier, baseRange.max * multiplier);
    }

    public static ValueRange ScaleRangeFloored(ValueRange baseRange, ItemTier tier, TierScalingConfig tierConfig = null)
    {
        float multiplier = GetMultiplier(tier, tierConfig);
        return new ValueRange(
            Mathf.Floor(baseRange.min * multiplier),
            Mathf.Floor(baseRange.max * multiplier)
        );
    }
}
