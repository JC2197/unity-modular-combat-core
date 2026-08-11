using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main manager for the trait system. Coordinates between UI and character trait management.
/// Attach this to a persistent game object or UI canvas.
/// </summary>
public class TraitSystemManager : MonoBehaviour
{
    /// <summary>Scene-wide singleton — set on Awake, cleared on destroy.</summary>
    public static TraitSystemManager Instance { get; private set; }
    [Header("References")]
    [SerializeField] private TraitTreeUI traitTreeUI;
    
    [Header("Available Trees")]
    [SerializeField] private List<TraitTreeData> characterTrees = new List<TraitTreeData>();
    
    [Header("Trait Points")]
    [SerializeField] private int startingTraitPoints = 0;
    
    private CharacterTraitManager currentCharacterTraitManager;
    private TraitTreeData currentTree;
    private int availableTraitPoints;
    private CharacterData currentCharacterData; // Set when trait tree is opened - the actual player's CharacterData
    
    // Events
    public System.Action<int> OnTraitPointsChanged;
    public System.Action<TraitData> OnTraitUnlocked;
    
    private void Awake()
    {
        Instance = this;
        availableTraitPoints = startingTraitPoints;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    private void OnEnable()
    {
        // OnLevelUp is kept for other subscribers (UI notifications, etc.).
        // Trait point granting is now handled directly by LevelUpManager calling
        // AddTraitPoints rather than via this event, so HandleLevelUp is a no-op
        // backup only.
        LevelUpManager.OnLevelUp += HandleLevelUp;
    }
    
    private void OnDisable()
    {
        LevelUpManager.OnLevelUp -= HandleLevelUp;
    }
    
    /// <summary>
    /// Reaction to the OnLevelUp static event. Trait point granting is now the direct
    /// responsibility of LevelUpManager (which calls AddTraitPoints), so this handler
    /// exists only as a no-op hook for future UI notification subscribers.
    /// </summary>
    private void HandleLevelUp(int newLevel)
    {
        // Intentionally empty — LevelUpManager.LevelUp() calls AddTraitPoints(1) directly.
        // Keeping the subscription so other callers of OnLevelUp still have a receiver.
    }
    
    /// <summary>
    /// Open trait tree for a specific character
    /// </summary>
    public void OpenTraitTree(GameObject characterObject, string characterName)
    {
        
        // Auto-find TraitTreeUI if not assigned
        if (traitTreeUI == null)
        {
            traitTreeUI = FindFirstObjectByType<TraitTreeUI>(FindObjectsInactive.Include);
            if (traitTreeUI != null)
                Debug.Log($"[TraitSystemManager] Found TraitTreeUI via FindFirstObjectByType: {traitTreeUI.gameObject.name}");
            else
                Debug.LogWarning("[TraitSystemManager] FindFirstObjectByType<TraitTreeUI> returned null — no TraitTreeUI exists in any loaded scene.");
        }

        // Fallback: search inside TraitTreeSceneManager's traitTreeCanvas reference
        if (traitTreeUI == null)
        {
            if (TraitTreeSceneManager.Instance == null)
                Debug.LogWarning("[TraitSystemManager] TraitTreeSceneManager.Instance is null — is the TraitTreeSceneManager in the scene?");
            else
            {
                traitTreeUI = TraitTreeSceneManager.Instance.GetTraitTreeUI();
                if (traitTreeUI != null)
                    Debug.Log($"[TraitSystemManager] Found TraitTreeUI via TraitTreeSceneManager canvas: {traitTreeUI.gameObject.name}");
                else
                    Debug.LogWarning("[TraitSystemManager] TraitTreeSceneManager.GetTraitTreeUI() returned null — is 'Trait Tree Canvas' assigned in the TraitTreeSceneManager Inspector, and does it contain a TraitTreeUI component?");
            }
        }
        
        // Get or add trait manager to character
        currentCharacterTraitManager = characterObject.GetComponent<CharacterTraitManager>();
        if (currentCharacterTraitManager == null)
        {
            currentCharacterTraitManager = characterObject.AddComponent<CharacterTraitManager>();
        }

        // Prefer the CharacterData already held by CTM — it's the same object that
        // SetCharacterData() was called with during SetupCharacter, so UpdateCharacterDataTraitList
        // and SpendTraitPoint will always operate on the same reference.
        PlayerController localPlayer = characterObject.GetComponent<PlayerController>();
        currentCharacterData = currentCharacterTraitManager.GetCharacterData();

        // Fall back to PC's live data, then to the singleton
        if (currentCharacterData == null)
            currentCharacterData = localPlayer != null ? localPlayer.GetCurrentCharacterData() : null;
        if (currentCharacterData == null)
            currentCharacterData = CharacterSelectionManager.SelectedCharacter;

        // If there's still a divergence (CTM had null data), ensure CTM is synced to what we found
        if (currentCharacterTraitManager.GetCharacterData() == null && currentCharacterData != null)
        {
            currentCharacterTraitManager.SetCharacterData(currentCharacterData);
        }

        if (currentCharacterData != null)
        {
            // Always load trait points fresh from disk — the in-memory field can lag
            // behind if AddTraitPoints ran while the trait tree was closed (e.g. on level-up).
            int savedPoints = CharacterPersistence.LoadTraitPoints(currentCharacterData.characterName);
            if (savedPoints >= 0)
            {
                // Sync in-memory object so saves later in this session write the right value
                currentCharacterData.availableTraitPoints = savedPoints;
            }
            availableTraitPoints = currentCharacterData.availableTraitPoints;
            Debug.Log($"[TraitSystemManager] Loaded {availableTraitPoints} trait points from CharacterData (disk-synced)");
        }
        else
        {
            availableTraitPoints = startingTraitPoints;
            Debug.LogWarning($"[TraitSystemManager] No CharacterData found, using starting trait points: {startingTraitPoints}");
        }
        
        OnTraitPointsChanged?.Invoke(availableTraitPoints);
        
        Debug.Log($"[TraitSystemManager] CharacterData: {(currentCharacterData != null ? currentCharacterData.characterName : "null")}");
        
        if (currentCharacterData != null && currentCharacterData.traitTree != null)
        {
            currentTree = currentCharacterData.traitTree;
            Debug.Log($"[TraitSystemManager] Loaded trait tree '{currentTree.name}' with {currentTree.nodes.Count} nodes for character '{characterName}'");
        }
        else
        {
            if (currentCharacterData == null)
            {
                Debug.LogError($"[TraitSystemManager] CharacterData is null! Character: {characterName}");
            }
            else if (currentCharacterData.traitTree == null)
            {
                Debug.LogError($"[TraitSystemManager] Character '{characterName}' has no trait tree. ClassData: {(currentCharacterData.classData != null ? currentCharacterData.classData.className : "null")}. Make sure the ClassData has a trait tree assigned.");
            }
            currentCharacterData = null;
            return;
        }
        
        // Initialize UI
        if (traitTreeUI != null)
        {
            Debug.Log($"[TraitSystemManager] Initializing TraitTreeUI at: {traitTreeUI.gameObject.name}");
            traitTreeUI.Initialize(currentTree, currentCharacterTraitManager);
            traitTreeUI.OnTraitUnlockRequested += OnTraitUnlockRequested;
        }
        else
        {
            Debug.LogError($"[TraitSystemManager] TraitTreeUI is null!");
        }
        
        // Show UI — activate parent canvas first (mirrors WeaponCraftingSystemManager pattern)
        if (traitTreeUI != null)
        {
            Canvas parentCanvas = traitTreeUI.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
                parentCanvas.gameObject.SetActive(true);
            traitTreeUI.gameObject.SetActive(true);
        }
        
        // Disable player input
        PlayerController.InputEnabled = false;
        
        // Switch to UI cursor and register ESC close handler
        if (CursorManager.Instance != null)
            CursorManager.Instance.PushPanel(CloseTraitTree);
    }
    
    /// <summary>
    /// Close the trait tree UI
    /// </summary>
    public void CloseTraitTree()
    {
        if (traitTreeUI != null)
        {
            traitTreeUI.OnTraitUnlockRequested -= OnTraitUnlockRequested;
            traitTreeUI.gameObject.SetActive(false);
            
            // Deactivate parent canvas (mirrors WeaponCraftingSystemManager pattern)
            Canvas parentCanvas = traitTreeUI.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
                parentCanvas.gameObject.SetActive(false);
        }
        
        // Re-enable player input
        PlayerController.InputEnabled = true;
        
        // Deregister from ESC stack and switch back to gameplay cursor
        if (CursorManager.Instance != null)
            CursorManager.Instance.PopPanel();
        
        currentCharacterTraitManager = null;
        currentTree = null;
        currentCharacterData = null;
    }
    
    /// <summary>
    /// Handle trait unlock request from UI
    /// </summary>
    private void OnTraitUnlockRequested(string nodeID, TraitData traitData)
    {
        if (currentCharacterTraitManager == null || traitData == null || string.IsNullOrEmpty(nodeID))
            return;
        
        // Check if we have points (if using a point system)
        if (availableTraitPoints <= 0)
        {
            Debug.Log("Not enough trait points!");
            // You could show a UI message here
            return;
        }
        
        // Unlock the node first (TraitSystemManager owns the save + network broadcast via SpendTraitPoint)
        if (currentCharacterTraitManager.UnlockTrait(nodeID, traitData))
        {
            SpendTraitPoint();
            OnTraitUnlocked?.Invoke(traitData);
            Debug.Log($"[TraitSystemManager] Unlocked trait: {traitData.displayName} from node: {nodeID}");
        }
    }
    
    /// <summary>
    /// Grant trait points to the character (e.g. on level-up).
    /// Always reads the current saved count from CharacterData before adding so
    /// the in-memory total is never stale when the trait tree hasn't been opened yet.
    /// Optionally accepts an explicit charData reference (avoids a second GetComponent lookup
    /// when the caller already has it, e.g. HandleLevelUp).
    /// </summary>
    public void AddTraitPoints(int points, CharacterData charData = null)
    {
        // Resolve the authoritative CharacterData
        if (charData == null)
            charData = currentCharacterData;
        if (charData == null)
        {
            PlayerController lp = PlayerController.GetLocalPlayer();
            charData = lp != null ? lp.GetCurrentCharacterData() : CharacterSelectionManager.SelectedCharacter;
        }

        // Sync in-memory count from saved value so we never overwrite a real accumulated total
        if (charData != null)
            availableTraitPoints = charData.availableTraitPoints;

        availableTraitPoints += points;
        OnTraitPointsChanged?.Invoke(availableTraitPoints);

        if (charData != null)
        {
            charData.availableTraitPoints = availableTraitPoints;
            CharacterPersistence.SaveCharacter(charData);
            Debug.Log($"[TraitSystemManager] +{points} trait point(s) granted. Total now: {availableTraitPoints} (char: {charData.characterName})");
        }
    }

    /// <summary>
    /// Spend exactly one trait point for the current session character.
    /// This is the single authoritative path for every trait unlock — it decrements,
    /// writes to CharacterData, saves to disk, and notifies the network.
    /// Returns false (and logs a warning) if no points are available.
    /// </summary>
    public bool SpendTraitPoint()
    {
        // Re-derive currentCharacterData from CTM before operating.
        // CTM.characterData is always set to PC.currentCharacterData in SetupCharacter, so
        // this neutralises any residual object drift if the guard in LoadCharacterByIndex was
        // not present (e.g. first session before fix was deployed, or edge-case re-load).
        if (currentCharacterTraitManager != null)
        {
            CharacterData fresh = currentCharacterTraitManager.GetCharacterData();
            if (fresh != null) currentCharacterData = fresh;
        }

        if (availableTraitPoints <= 0)
        {
            Debug.LogWarning("[TraitSystemManager] SpendTraitPoint: no available trait points!");
            return false;
        }

        availableTraitPoints--;
        OnTraitPointsChanged?.Invoke(availableTraitPoints);

        if (currentCharacterData != null)
        {
            currentCharacterData.availableTraitPoints = availableTraitPoints;

            // Defensive: sync unlocked nodes from CTM into currentCharacterData before saving.
            // Normally they share the same object reference (fixed in OpenTraitTree), but this
            // guard ensures correctness even if a reference drift occurs (e.g. hot-reload).
            if (currentCharacterTraitManager != null)
            {
                var latestNodes = currentCharacterTraitManager.GetUnlockedNodeIDs();
                currentCharacterData.unlockedNodeIDs.Clear();
                currentCharacterData.unlockedNodeIDs.AddRange(latestNodes);
            }

            CharacterPersistence.SaveCharacter(currentCharacterData);
            Debug.Log($"[TraitSystemManager] Trait point spent. Remaining: {availableTraitPoints}, nodes saved: {currentCharacterData.unlockedNodeIDs?.Count ?? 0}");

            // Sync availableTraitPoints to PC.currentCharacterData as well.
            // If a reference drift occurred (CTM.characterData != PC.currentCharacterData),
            // ExperienceManager.SaveProgress would otherwise later overwrite availableTraitPoints
            // back to 1 when it next saves PC's object.
            PlayerController lp = PlayerController.GetLocalPlayer();
            CharacterData pcData = lp?.GetCurrentCharacterData();
            if (pcData != null && pcData != currentCharacterData)
            {
                // Patch BOTH fields so the next SaveCharacter on PC's copy is complete.
                // Without the node sync, ExperienceManager's periodic save would overwrite
                // PlayerPrefs with nodes=[] causing data loss on re-entry.
                pcData.availableTraitPoints = availableTraitPoints;
                pcData.unlockedNodeIDs.Clear();
                pcData.unlockedNodeIDs.AddRange(currentCharacterData.unlockedNodeIDs);
                Debug.LogWarning($"[TraitSystemManager] SpendTraitPoint: PC.currentCharacterData diverged — patched pts={availableTraitPoints} and {pcData.unlockedNodeIDs.Count} nodes onto PC's copy.");
            }

            lp?.NotifyTraitChanged();
        }

        return true;
    }
    
    /// <summary>
    /// Get current available trait points
    /// </summary>
    public int GetAvailablePoints()
    {
        return availableTraitPoints;
    }
    
    /// <summary>
    /// Reset all traits for current character
    /// </summary>
    public void ResetAllTraits()
    {
        if (currentCharacterTraitManager != null)
        {
            // Get count of unlocked traits to refund points
            int unlockedCount = currentCharacterTraitManager.GetActiveTraits().Count;
            
            // Reset
            currentCharacterTraitManager.ResetAllTraits();
            
            // Refund points
            availableTraitPoints += unlockedCount;
            OnTraitPointsChanged?.Invoke(availableTraitPoints);
            
            // Save trait points back to the actual player's CharacterData
            if (currentCharacterData != null)
            {
                currentCharacterData.availableTraitPoints = availableTraitPoints;
                CharacterPersistence.SaveCharacter(currentCharacterData);
                Debug.Log($"[TraitSystemManager] Reset traits, refunded {unlockedCount} points, total: {availableTraitPoints}");

                // Push updated CharacterData to network so remote clients see the cleared nodes
                PlayerController lp = PlayerController.GetLocalPlayer();
                lp?.NotifyTraitChanged();
            }
            
            Debug.Log("All traits reset!");
        }
    }
    
}
