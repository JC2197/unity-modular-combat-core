using UnityEngine;
using System;

/// <summary>
/// Manages player experience and XP calculations.
/// Works with LevelUpManager to handle leveling.
/// Attach this to the Player GameObject.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerExperienceConfig config;
    
    [Header("Current Stats (Runtime Only)")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpRequiredForNextLevel = 5;
    
    // Events
    public static event Action<int, int, int> OnXPGained; // currentXP, requiredXP, xpGained
    public static event Action OnXPThresholdReached; // Triggered when enough XP for level up
    
    // Properties
    public int CurrentXP => currentXP;
    public int XPRequiredForNextLevel => xpRequiredForNextLevel;
    public float XPProgress => xpRequiredForNextLevel > 0 ? (float)currentXP / xpRequiredForNextLevel : 0f;
    
    private LevelUpManager levelUpManager;
    private PlayerController playerController;
    private bool initialized = false;

    /// <summary>
    /// True when this instance belongs to the local player.
    /// Falls back to true in single-player (no network spawned yet).
    /// </summary>
    private bool IsOwner
    {
        get
        {
            if (playerController != null && playerController.IsSpawned)
                return playerController.IsOwner;
            return true; // single-player or pre-spawn
        }
    }
    
    // Save batching to prevent framerate drops
    private float timeSinceLastSave = 0f;
    private const float SAVE_INTERVAL = 5f; // Save every 5 seconds max
    private bool hasPendingSave = false;
    
    private void Awake()
    {
        levelUpManager = GetComponent<LevelUpManager>();
        playerController = GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        InitializeXPSystem();
    }
    
    private void InitializeXPSystem()
    {
        if (initialized) return;
        if (!IsOwner) return; // Remote players manage their own XP on their own client
        
        if (levelUpManager != null)
        {
            // Load progress - also restores xpRequiredForNextLevel if it was saved
            LoadProgress();
            
            // Only recalculate threshold if we didn't get a valid value from the save
            if (xpRequiredForNextLevel <= 0)
            {
                CalculateXPRequiredForLevel(levelUpManager.CurrentLevel);
            }
            
            Debug.Log($"[ExperienceManager] Initialized - Level: {levelUpManager.CurrentLevel}, XP: {currentXP}/{xpRequiredForNextLevel}");
            
            // Trigger UI update
            OnXPGained?.Invoke(currentXP, xpRequiredForNextLevel, 0);
            
            // Defer pending level-up check so LevelUpManager finishes loading first
            StartCoroutine(CheckForPendingLevelUpsDelayed());

            // Only mark initialized if CharacterData was actually available.
            // If it wasn't, HandlePlayerSpawned will re-run this once data is loaded.
            initialized = GetCharacterData() != null;
            if (!initialized)
                Debug.Log("[ExperienceManager] CharacterData not available yet - will re-initialize on player spawn");
        }
    }
    
    /// <summary>
    /// Check for pending level ups after a frame delay.
    /// This ensures LevelUpManager has fully initialized with correct level from CharacterData.
    /// </summary>
    private System.Collections.IEnumerator CheckForPendingLevelUpsDelayed()
    {
        // Wait one frame for LevelUpManager to finish loading level from CharacterData.
        yield return null;

        // Re-fire UI update now that level is confirmed correct.
        OnXPGained?.Invoke(currentXP, xpRequiredForNextLevel, 0);

        Debug.Log($"[ExperienceManager] Checking for pending level ups - Level: {levelUpManager.CurrentLevel}, XP: {currentXP}/{xpRequiredForNextLevel}");

        CheckForPendingLevelUps();
    }
    
    /// <summary>
    /// Check if there's excess XP that should trigger level ups (called on load)
    /// </summary>
    private void CheckForPendingLevelUps()
    {
        if (levelUpManager == null) return;
        
        int safetyCounter = 0;
        while (currentXP >= xpRequiredForNextLevel && !levelUpManager.IsMaxLevel && safetyCounter < 100)
        {
            Debug.Log($"[ExperienceManager] Pending level up detected! XP: {currentXP}/{xpRequiredForNextLevel}");
            TriggerLevelUp();
            safetyCounter++;
        }
        
        if (safetyCounter > 0)
        {
            Debug.Log($"[ExperienceManager] Processed {safetyCounter} pending level up(s) on load");
        }
    }
    
    private void OnEnable()
    {
        if (levelUpManager != null)
        {
            LevelUpManager.OnLevelUp += HandleLevelUp;
        }
        PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
    }
    
    private void OnDisable()
    {
        if (levelUpManager != null)
        {
            LevelUpManager.OnLevelUp -= HandleLevelUp;
        }
        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    /// <summary>
    /// Called when the local player is fully spawned (including network ownership confirmed).
    /// Re-initializes the XP system so it reads CharacterData after it has been loaded.
    /// </summary>
    private void HandlePlayerSpawned(PlayerController spawnedPlayer)
    {
        // Only react to the player this component is attached to
        if (spawnedPlayer.gameObject != gameObject) return;
        // Only care about the network-ownership confirmation spawn (IsOwner now reliable)
        if (!spawnedPlayer.IsSpawned) return;

        Debug.Log($"[ExperienceManager] HandlePlayerSpawned - resetting and re-initializing with correct CharacterData");
        initialized = false;
        InitializeXPSystem();
    }
    
    private void Update()
    {
        // Level/XP are transient — no periodic saving needed
    }
    
    /// <summary>
    /// Add experience to the player. Only processes for the owning client.
    /// </summary>
    public void AddExperience(int amount)
    {
        if (!IsOwner) return;
        if (levelUpManager != null && levelUpManager.IsMaxLevel)
        {
            Debug.Log("[ExperienceManager] Already at max level!");
            return;
        }
        
        currentXP += amount;
        
        // Mark that we have unsaved progress (will save in Update)
        hasPendingSave = true;
        
        OnXPGained?.Invoke(currentXP, xpRequiredForNextLevel, amount);
        
        // Check for level up
        while (currentXP >= xpRequiredForNextLevel && levelUpManager != null && !levelUpManager.IsMaxLevel)
        {
            TriggerLevelUp();
        }
    }
    
    private void TriggerLevelUp()
    {
        // Carry over excess XP
        currentXP -= xpRequiredForNextLevel;
        
        // Notify LevelUpManager
        OnXPThresholdReached?.Invoke();
        
        // LevelUpManager will handle the level up and trigger HandleLevelUp callback
    }
    
    private void HandleLevelUp(int newLevel)
    {
        // OnLevelUp is a static event — every ExperienceManager in the scene hears it.
        // Only the locally-owned player should update their XP requirements.
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && !pc.IsOwner)
        {
            return;
        }

        // Recalculate XP requirement for new level
        CalculateXPRequiredForLevel(newLevel);
        
        Debug.Log($"[ExperienceManager] XP requirement updated. Next level requires {xpRequiredForNextLevel} XP.");
    }
    
    public void CalculateXPRequiredForLevel(int level)
    {
        if (config == null)
        {
            Debug.LogError("[ExperienceManager] PlayerExperienceConfig is not assigned!");
            xpRequiredForNextLevel = 999999;
            return;
        }
        
        if (levelUpManager != null && level >= levelUpManager.MaxLevel)
        {
            xpRequiredForNextLevel = 0;
            return;
        }
        
        xpRequiredForNextLevel = config.CalculateXPRequiredForLevel(level);
    }
    
    /// <summary>
    /// Get total XP required to reach a specific level from level 1
    /// </summary>
    public int GetTotalXPForLevel(int level)
    {
        if (config == null)
        {
            Debug.LogError("[ExperienceManager] PlayerExperienceConfig is not assigned!");
            return 0;
        }
        
        return config.GetTotalXPForLevel(level);
    }
    
    /// <summary>
    /// Calculate XP reward for killing an enemy. Delegates to PlayerExperienceConfig
    /// so the multiplier is data-driven rather than hardcoded.
    /// </summary>
    public int CalculateXPReward(float maxHealth)
    {
        if (config != null)
            return config.CalculateXPReward(maxHealth);
        // Fallback if config not assigned
        return Mathf.Max(1, Mathf.RoundToInt(maxHealth * 0.1f));
    }
    
    private void OnApplicationQuit()
    {
        // Level/XP are transient — nothing to save
        #if UNITY_EDITOR
        CharacterPersistence.FlushDirtyCharacterAssets();
        #endif
    }
    
    /// <summary>
    /// Get the player's actual CharacterData instance (not the singleton).
    /// </summary>
    private CharacterData GetCharacterData()
    {
        if (playerController != null)
        {
            CharacterData cd = playerController.GetCurrentCharacterData();
            if (cd != null) return cd;
        }
        // Fallback to singleton (single-player / editor without network)
        return CharacterSelectionManager.SelectedCharacter;
    }

    /// <summary>
    /// Level/XP are non-permanent (reset every GameScene run), so we never persist them.
    /// This method is intentionally a no-op.
    /// </summary>
    private void SaveProgress()
    {
        // No-op: XP data is transient and resets every run
    }
    
    /// <summary>
    /// Load XP progress — always starts at 0 (non-permanent progression)
    /// </summary>
    private void LoadProgress()
    {
        currentXP = 0;
        CalculateXPRequiredForLevel(1);
        Debug.Log($"[ExperienceManager] XP initialised to 0/{xpRequiredForNextLevel} (non-permanent progression)");
    }
    
    /// <summary>
    /// Reset XP to 0 and recalculate threshold for level 1.
    /// Called by LevelUpManager.ResetToLevel1().
    /// </summary>
    public void ResetXP()
    {
        currentXP = 0;
        CalculateXPRequiredForLevel(1);
        OnXPGained?.Invoke(currentXP, xpRequiredForNextLevel, 0);
        Debug.Log($"[ExperienceManager] XP reset — 0/{xpRequiredForNextLevel}");
    }

    [ContextMenu("Level Up")]
    private void DebugLevelUp()
    {
        AddExperience(xpRequiredForNextLevel);
    }
    
    
}
