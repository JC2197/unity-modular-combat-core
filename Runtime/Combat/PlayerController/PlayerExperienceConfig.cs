using UnityEngine;

/// <summary>
/// Configuration for player experience and leveling system.
/// Centralized config to manage XP progression, skill points, and level-up settings.
/// </summary>
[CreateAssetMenu(fileName = "PlayerExperienceConfig", menuName = "Characters/Player Experience Config")]
public class PlayerExperienceConfig : ScriptableObject
{
    [Header("XP Progression")]
    [Tooltip("Starting XP required for level 2")]
    [SerializeField] private int startingXP = 5;
    
    [Tooltip("Multiplier for XP scaling per level (e.g., 1.3 = 30% increase per level)")]
    [SerializeField] private float xpMultiplier = 1.3f;
    
    [Header("Level Settings")]
    [Tooltip("Maximum level the player can reach")]
    [SerializeField] private int maxLevel = 100;
    
    [Header("Skill Points")]
    [Tooltip("Skill points awarded per level")]
    [SerializeField] private int skillPointsPerLevel = 1;
    
    [Header("Level Up Bonuses")]
    [Tooltip("Fully restore health on level up")]
    [SerializeField] private bool fullHealOnLevelUp = true;
    
    [Tooltip("Fully restore energy on level up")]
    [SerializeField] private bool fullEnergyRestoreOnLevelUp = true;
    
    [Header("XP Rewards")]
    [Tooltip("XP awarded per kill = enemy MaxHealth * this ratio")]
    [SerializeField] private float xpRewardRatio = 0.1f;

    // Properties
    public int StartingXP => startingXP;
    public float XPMultiplier => xpMultiplier;
    public int MaxLevel => maxLevel;
    public int SkillPointsPerLevel => skillPointsPerLevel;
    public bool FullHealOnLevelUp => fullHealOnLevelUp;
    public bool FullEnergyRestoreOnLevelUp => fullEnergyRestoreOnLevelUp;
    public float XPRewardRatio => xpRewardRatio;
    
    /// <summary>
    /// Calculate XP required to reach a specific level from the previous level.
    /// </summary>
    public int CalculateXPRequiredForLevel(int level)
    {
        if (level >= maxLevel)
        {
            return 0;
        }
        
        // Calculate XP required to go FROM 'level' TO 'level + 1'
        // Level 1→2: startingXP (no multiplier)
        // Level 2→3: startingXP * multiplier^1
        // Level N→N+1: startingXP * multiplier^(N-1)
        return Mathf.RoundToInt(startingXP * Mathf.Pow(xpMultiplier, level - 1));
    }
    
    /// <summary>
    /// Get total XP required to reach a specific level from level 1.
    /// </summary>
    public int GetTotalXPForLevel(int targetLevel)
    {
        int totalXP = 0;
        for (int i = 1; i < targetLevel; i++)
        {
            totalXP += CalculateXPRequiredForLevel(i);
        }
        return totalXP;
    }
    
    /// <summary>
    /// Calculate XP reward for killing an enemy with the given max health.
    /// Formula: max(1, round(maxHealth * xpRewardRatio))
    /// </summary>
    public int CalculateXPReward(float maxHealth)
    {
        return Mathf.Max(1, Mathf.RoundToInt(maxHealth * xpRewardRatio));
    }
    
    private void OnValidate()
    {
        // Ensure values are reasonable
        startingXP = Mathf.Max(1, startingXP);
        xpMultiplier = Mathf.Max(1.0f, xpMultiplier);
        maxLevel = Mathf.Max(1, maxLevel);
        skillPointsPerLevel = Mathf.Max(0, skillPointsPerLevel);
        xpRewardRatio = Mathf.Max(0f, xpRewardRatio);
    }
}
