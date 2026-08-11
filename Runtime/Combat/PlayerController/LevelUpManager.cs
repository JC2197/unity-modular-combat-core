using UnityEngine;
using System;

/// <summary>
/// Manages player level progression and skill point rewards.
/// Works with ExperienceManager for XP tracking.
/// Attach this to the Player GameObject.
/// </summary>
public class LevelUpManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerExperienceConfig config;
    
    [Header("Current Progress (Runtime Only)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int availableSkillPoints = 0;
    [SerializeField] private int totalSkillPointsEarned = 0;
    
    // Stat growth is now loaded from ClassData, not hardcoded here
    
    // Events
    public static event Action<int> OnLevelUp; // newLevel
    public static event Action<int, int> OnSkillPointsGained; // pointsGained, totalAvailable
    
    // Properties
    public int CurrentLevel => currentLevel;
    public int MaxLevel => config != null ? config.MaxLevel : 100;
    public int AvailableSkillPoints => availableSkillPoints;
    public int TotalSkillPointsEarned => totalSkillPointsEarned;
    public bool IsMaxLevel => currentLevel >= MaxLevel;
    
    private ExperienceManager experienceManager;
    private bool isInitialized = false;

    /// <summary>
    /// Returns the authoritative CharacterData for this player instance.
    /// Prefers PlayerController.currentCharacterData (correct for both single-player
    /// and multiplayer) over the singleton SelectedCharacter, which may refer to a
    /// different character instance when using per-player assignment in multiplayer.
    /// Falls back to the singleton only during Awake() timing before the
    /// PlayerController has finished loading its character.
    /// </summary>
    private CharacterData GetCharacterData()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            CharacterData data = player.GetCurrentCharacterData();
            if (data != null) return data;
        }
        return CharacterSelectionManager.SelectedCharacter;
    }
    
    /// <summary>
    /// Initialize level manager and load character progression data.
    /// This MUST complete before subscribing to XP events to prevent false level-ups.
    /// </summary>
    private void Awake()
    {
        Debug.Log("[LevelUpManager] ========== LEVEL MANAGER INITIALIZATION START ==========");
        
        experienceManager = GetComponent<ExperienceManager>();
        
        // STEP 1: Load level and XP from CharacterData
        Debug.Log("[LevelUpManager] Loading character level/XP data...");
        LoadProgressFromCharacterData();
        
        // STEP 2: Subscribe to XP events AFTER data is loaded
        // This prevents HandleXPThresholdReached from firing incorrectly during initialization
        Debug.Log("[LevelUpManager] Subscribing to XP threshold events...");
        ExperienceManager.OnXPThresholdReached += HandleXPThresholdReached;
        
        isInitialized = true;
        
        Debug.Log($"[LevelUpManager] Initialized at level {currentLevel}");
        Debug.Log("[LevelUpManager] ========== LEVEL MANAGER INITIALIZATION COMPLETE ==========");
    }
    
    private void OnDisable()
    {
        ExperienceManager.OnXPThresholdReached -= HandleXPThresholdReached;
    }
    
    private void HandleXPThresholdReached()
    {
        // Safety check: ignore threshold events until fully initialized
        if (!isInitialized)
        {
            Debug.LogWarning("[LevelUpManager] Ignoring XP threshold event - not yet initialized");
            return;
        }

        // OnXPThresholdReached is a static event, so every LevelUpManager in the scene
        // (one per networked player) receives it — even when it was fired by a different
        // player's ExperienceManager. Only the locally-owned player should process it.
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && !pc.IsOwner)
        {
            return;
        }
        
        LevelUp();
    }
    
    private void LevelUp()
    {
        if (config == null)
        {
            Debug.LogError("[LevelUpManager] PlayerExperienceConfig is not assigned!");
            return;
        }
        
        if (currentLevel >= config.MaxLevel)
        {
            Debug.Log("[LevelUpManager] Already at max level!");
            return;
        }
        
        // Store old level before incrementing
        int oldLevel = currentLevel;
        currentLevel++;
        
        // Grant skill points
        int skillPointsGained = config.SkillPointsPerLevel;
        availableSkillPoints += skillPointsGained;
        totalSkillPointsEarned += skillPointsGained;
        
        // Sync level with CharacterData. SaveProgress is NOT called here —
        // AddTraitPoints (called after ApplyLevelUpBonuses) performs the authoritative save.
        CharacterData syncData = GetCharacterData();
        if (syncData != null)
        {
            if (currentLevel < syncData.characterLevel)
            {
                // CharacterData is ahead (loaded from save) — sync local to it
                Debug.LogWarning($"[LevelUpManager] Local level ({currentLevel}) behind CharacterData ({syncData.characterLevel}), syncing");
                currentLevel = syncData.characterLevel;
            }
            else
            {
                // Normal level-up — write to CharacterData (no disk save yet)
                syncData.characterLevel = currentLevel;
            }
        }
        
        // Check if we actually leveled up or just synced to a higher saved level
        bool isRealLevelUp = (currentLevel == oldLevel + 1);
        bool wasSyncedFromSave = (currentLevel > oldLevel + 1);
        
        if (wasSyncedFromSave)
        {
            Debug.LogWarning($"[LevelUpManager] Level synced from save (local {oldLevel} → save {currentLevel}), skipping stat growth application");
            // Don't apply stat growth - stats are already in saved CharacterData
            // This prevents double-applying bonuses when loading a saved character
            return;
        }
        
        // Only apply level up bonuses if this is a genuine level up
        if (isRealLevelUp)
        {
            Debug.Log($"[LevelUpManager] Real level up {oldLevel} → {currentLevel}, applying stat growth");
            ApplyLevelUpBonuses();

            OnLevelUp?.Invoke(currentLevel);
            OnSkillPointsGained?.Invoke(skillPointsGained, availableSkillPoints);
        }
    }
    
    private void ApplyLevelUpBonuses()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player == null) return;

        CharacterData characterData = GetCharacterData();
        if (characterData == null || characterData.classData == null)
        {
            Debug.LogError("[LevelUpManager] Cannot apply level up bonuses - CharacterData or ClassData is null!");
            return;
        }
        
        ClassData.StatGrowthPerLevel growth = characterData.classData.statGrowth;
        
        Debug.Log($"[LevelUpManager] Applying level {currentLevel} stat growth from {characterData.classData.className}");
        
        // Increase current stats in statContainer (NOT baseStatContainer - that's immutable)
        // statContainer tracks accumulated level bonuses
        if (growth.power > 0)
        {
            float oldStr = characterData.statContainer.GetStat("POWER");
            float newStr = oldStr + growth.power;
            characterData.statContainer.SetStat("POWER", newStr);
            Debug.Log($"[LevelUpManager] POWER: {oldStr} → {newStr} (+{growth.power})");
        }
        
        if (growth.body > 0)
        {
            float oldVig = characterData.statContainer.GetStat("BODY");
            float newVig = oldVig + growth.body;
            characterData.statContainer.SetStat("BODY", newVig);
            Debug.Log($"[LevelUpManager] BODY: {oldVig} → {newVig} (+{growth.body})");
        }
        
        if (growth.survival > 0)
        {
            float oldDex = characterData.statContainer.GetStat("SURVIVAL");
            float newDex = oldDex + growth.survival;
            characterData.statContainer.SetStat("SURVIVAL", newDex);
            Debug.Log($"[LevelUpManager] SURVIVAL: {oldDex} → {newDex} (+{growth.survival})");
        }
        
        if (growth.mind > 0)
        {
            float oldInt = characterData.statContainer.GetStat("MIND");
            float newInt = oldInt + growth.mind;
            characterData.statContainer.SetStat("MIND", newInt);
            Debug.Log($"[LevelUpManager] MIND: {oldInt} → {newInt} (+{growth.mind})");
        }
        
        if (growth.skill > 0)
        {
            float oldTal = characterData.statContainer.GetStat("SKILL");
            float newTal = oldTal + growth.skill;
            characterData.statContainer.SetStat("SKILL", newTal);
            Debug.Log($"[LevelUpManager] SKILL: {oldTal} → {newTal} (+{growth.skill})");
        }
        
        if (growth.faith > 0)
        {
            float oldFai = characterData.statContainer.GetStat("FAITH");
            float newFai = oldFai + growth.faith;
            characterData.statContainer.SetStat("FAITH", newFai);
            Debug.Log($"[LevelUpManager] FAITH: {oldFai} → {newFai} (+{growth.faith})");
        }
        
        // Increase max health/energy in statContainer (accumulates with levels)
        if (growth.baseMaxHealth > 0)
        {
            float oldMax = characterData.statContainer.GetStat("MaxHealth");
            float newMax = oldMax + growth.baseMaxHealth;
            characterData.statContainer.SetStat("MaxHealth", newMax);
            Debug.Log($"[LevelUpManager] MaxHealth: {oldMax} → {newMax} (+{growth.baseMaxHealth})");
        }
        
        if (growth.baseMaxEnergy > 0)
        {
            float oldMax = characterData.statContainer.GetStat("MaxEnergy");
            float newMax = oldMax + growth.baseMaxEnergy;
            characterData.statContainer.SetStat("MaxEnergy", newMax);
            Debug.Log($"[LevelUpManager] MaxEnergy: {oldMax} → {newMax} (+{growth.baseMaxEnergy})");
        }
        
        // Trigger recalculation: apply conversions and traits to updated statContainer
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // This will: 1) Apply conversions to statContainer, 2) Apply traits, 3) Update AllStats
            playerController.SendMessage("RecalculateStatsWithTraits", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[LevelUpManager] Recalculated stats with traits after level-up");
        }
        
        // Update runtime values from recalculated stats
        float finalMaxHealth = characterData.statContainer.GetStat("MaxHealth");
        float finalMaxEnergy = characterData.statContainer.GetStat("MaxEnergy");
        
        player.AllStats.SetStat("MaxHealth", finalMaxHealth);
        player.AllStats.SetStat("MaxEnergy", finalMaxEnergy);
        
        // Full heal on level up
        if (config != null && config.FullHealOnLevelUp)
        {
            player.ModifyHealth(finalMaxHealth - player.CurrentHealth);
        }

        if (config != null && config.FullEnergyRestoreOnLevelUp)
        {
            player.ModifyEnergy(finalMaxEnergy - player.CurrentEnergy);
        }

        // Research points replaced per-level trait point grants.
        // Trait points are no longer awarded on level-up; research points are earned
        // from survival timer percentage at end-of-run via ResearchPointManager.

        Debug.Log($"[LevelUpManager] Level-up stat growth applied. Final stats - HP: {finalMaxHealth}, MP: {finalMaxEnergy}");
        // Note: HUD automatically updates via StatContainer.OnAnyStatChanged event
    }
    
    /// <summary>
    /// Spend skill points (for talent tree)
    /// </summary>
    public bool SpendSkillPoints(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[LevelUpManager] Cannot spend 0 or negative skill points");
            return false;
        }
        
        if (availableSkillPoints < amount)
        {
            Debug.LogWarning($"[LevelUpManager] Not enough skill points! Need {amount}, have {availableSkillPoints}");
            return false;
        }
        
        availableSkillPoints -= amount;
        SaveProgress();
        Debug.Log($"[LevelUpManager] Spent {amount} skill point(s). {availableSkillPoints} remaining.");
        return true;
    }
    
    /// <summary>
    /// Refund skill points (for talent tree respec)
    /// </summary>
    public void RefundSkillPoints(int amount)
    {
        availableSkillPoints += amount;
        SaveProgress();
        Debug.Log($"[LevelUpManager] Refunded {amount} skill point(s). {availableSkillPoints} available.");
    }
    
    /// <summary>
    /// Get total XP required to reach a specific level from level 1
    /// </summary>
    public int GetTotalXPForLevel(int level)
    {
        if (experienceManager == null) return 0;
        
        int totalXP = 0;
        for (int i = 1; i < level; i++)
        {
            experienceManager.CalculateXPRequiredForLevel(i);
            totalXP += experienceManager.XPRequiredForNextLevel;
        }
        return totalXP;
    }
    
    /// <summary>
    /// Level/XP are non-permanent (reset every GameScene run), so we never persist them.
    /// This method is intentionally a no-op.
    /// </summary>
    private void SaveProgress()
    {
        // No-op: level data is transient and resets every run
    }
    
    /// <summary>
    /// Level is always 1 at start — progression is non-permanent and resets each run.
    /// </summary>
    private void LoadProgressFromCharacterData()
    {
        currentLevel = 1;
        availableSkillPoints = 0;
        totalSkillPointsEarned = 0;
        Debug.Log("[LevelUpManager] Level initialised to 1 (non-permanent progression)");
    }
    
    /// <summary>
    /// Reset this player to level 1 with 0 XP. Called when entering GameScene
    /// so every run starts fresh. Also resets the companion ExperienceManager.
    /// </summary>
    public void ResetToLevel1()
    {
        Debug.Log($"[LevelUpManager] Resetting from level {currentLevel} to level 1");

        currentLevel = 1;
        availableSkillPoints = 0;
        totalSkillPointsEarned = 0;

        // Update in-memory CharacterData (no disk save — level is transient)
        CharacterData characterData = GetCharacterData();
        if (characterData != null)
        {
            characterData.characterLevel = 1;
            characterData.currentExperience = 0;
            characterData.xpRequiredForNextLevel = 0;
        }

        // Reset companion ExperienceManager
        if (experienceManager != null)
            experienceManager.ResetXP();

        Debug.Log("[LevelUpManager] Level reset complete — level 1, 0 XP");
    }

    // Debug methods
    [ContextMenu("Force Level Up")]
    private void DebugForceLevelUp()
    {
        LevelUp();
    }
    
    [ContextMenu("Add 10 Skill Points")]
    private void DebugAdd10SkillPoints()
    {
        availableSkillPoints += 10;
        totalSkillPointsEarned += 10;
        Debug.Log($"[LevelUpManager] Debug: Added 10 skill points. Total: {availableSkillPoints}");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        ExperienceManager.OnXPThresholdReached -= HandleXPThresholdReached;
        Debug.Log($"[LevelUpManager] OnDestroy at level {currentLevel} (not saved — transient)");
    }
}
