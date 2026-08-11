using UnityEngine;
using UnityEngine.InputSystem;
using System;
using FishNet;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Component.Animating;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using JoeConticello.ModularCombatCore;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController : Organism
{
    private static readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);

    public static event Action<PlayerController> OnPlayerSpawned;
    /// <summary>
    /// Fired on the local owner when the player's health reaches 0.
    /// Listeners (e.g. PlayerDeathSequencer) use this to drive death VFX and the end screen
    /// before the scene transition occurs.
    /// </summary>
    public static event Action OnLocalPlayerDeath;

    /// <summary>
    /// Cached reference to the local player. Set when ownership is confirmed so
    /// late-loading scenes (e.g. TraitTree) can find the player instantly without
    /// polling via FindObjectsByType during the network-init window.
    /// </summary>
    public static PlayerController LocalPlayer { get; private set; }

    /// <summary>
    /// Get the local player (owner in multiplayer, any player in single-player).
    /// Returns the cached LocalPlayer if available, otherwise searches all instances.
    /// </summary>
    public static PlayerController GetLocalPlayer()
    {
        if (LocalPlayer != null) return LocalPlayer;

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            bool isNetworkActive = player.IsServerStarted || player.IsClientStarted;
            bool isLocalPlayer = !isNetworkActive || player.IsOwner;
            if (isLocalPlayer)
            {
                LocalPlayer = player;
                return player;
            }
        }
        return null;
    }

    // Synced character selection index (FishNet 4.x syntax)
    // CRITICAL: SyncVars use WritePermission.ServerOnly (default). Clients CANNOT write SyncVars
    // directly — FishNet's ClientUnsynchronized only writes locally and does NOT sync to others.
    // All client writes go through [ServerRpc] methods below, which set the value on the server,
    // and FishNet automatically propagates the change to all clients.
    private readonly SyncVar<int> _syncSelectedCharacterIndex = new SyncVar<int>();
    private readonly SyncVar<string> _syncCharacterName = new SyncVar<string>();
    private readonly SyncVar<string> _syncClassName = new SyncVar<string>(); // Synced class name for remote gear loading
    private readonly SyncVar<float> _syncAimAngle = new SyncVar<float>(); // Synced aim angle (float degrees for smooth remote weapon rotation)
    private readonly SyncVar<bool> _syncIsFacingLeft = new SyncVar<bool>(); // Synced character facing direction for correct remote flip on late-join

    // Tracks FishNet-spawned weapon NetworkObjects so they can be despawned on re-equip
    private NetworkObject _currentWeaponNOB;
    private NetworkObject _currentOffHandWeaponNOB;

    /// <summary>
    /// Lightweight visual-only snapshot for network sync.
    /// ~200 bytes instead of ~2-5KB full CharacterData JSON.
    /// Remote clients use this to render gear visuals only - no stats, no traits, no combat data.
    /// </summary>
    [System.Serializable]
    public struct PlayerVisualSnapshot
    {
        public string characterName;      // For floating name display
        public string className;          // For class-based animations/icons
        public string weaponConfigName;
        public string offhandConfigName;
        public string headArmorConfig;
        public string chestArmorConfig;
        public string handsArmorConfig;
        public string feetArmorConfig;
        public string backpackArmorConfig;
    }

    // Assigned character name (set by NetworkSpawner for each individual player)
    // This allows each player to have their own character instead of all sharing the singleton
    private string assignedCharacterName = null;

    // Static flag to disable input during loading screens
    private static bool inputEnabled = true;
    public static bool InputEnabled
    {
        get => inputEnabled;
        set
        {
            inputEnabled = value;
            Debug.Log($"[PlayerController] Input {(value ? "Enabled" : "Disabled")}");
        }
    }

    [Header("Player-Specific Settings")]
    [SerializeField] private bool flipSpriteOnMove = true;

    [Header("Aim Stability")]
    [SerializeField, Tooltip("Keeps the previous aim angle while the cursor is very close to the player. Prevents rapid left/right flip jitter when root-motion moves the character across the cursor.")]
    private float closeAimStabilityRadius = 3f;

    [Header("Pet")]
    private Pet currentPet;

    private Vector2 movement;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction weaponAbilityAction;
    private InputAction dashAbilityAction;
    private bool inputSystemReady = false;
    private bool networkInitialized = false; // Prevents duplicate character creation during SyncVar init
    private bool _statsDirty = false;          // Dirty flag: set by RequestStatsRecalculation(), flushed once per LateUpdate
    private bool _pendingTraitRecalc = false;  // Set when trait coroutine deferred due to inactive GO
    private Coroutine _loadAnimationsRetryCoroutine;
    private bool isGearAnimationReady = false;
    private bool hasQueuedAnimationResume = false;
    private string queuedResumeAnimationName = "";
    private float queuedResumeNormalizedTime = 0f;

    private Animator animator;
    private Animator legsAnimator;
    private NetworkAnimator networkAnimator; // FishNet NetworkAnimator on this root — used by PlayAnimation for reliable cross-client sync
    private new SpriteRenderer spriteRenderer;
    private PlayerGearManager gearManager;
    private CharacterAbilityManager abilityManager;
    private CharacterData currentCharacterData;
    private bool _loggedMissingCharacterData;
    private CharacterTraitManager traitManager;
    private CharacterGearManager characterGearManager;  // Manages stat modifiers from equipped gear
    private StatConverter statConverter;
    private EffectManager effectManager;

    private string currentAnimationPlaying = "";
    private bool isFacingLeft = false;
    private bool wasMovingUp = false;
    private bool wasMovementBlockedLastFrame = false;
    private bool footstepParticlesPlaying = false;
    private float footstepTimer = 0f;
    private int currentAnimationFrame = 0;
    private ParticleSystem footstepParticles;

    // Throttle weapon/character rotation updates to reduce CPU load
    private float lastRotationUpdateTime = 0f;
    private const float rotationUpdateInterval = 0.05f; // Update every 0.05 seconds (20 times per second)

    private WeaponSortingManager mainHandSortingManager;
    private WeaponSortingManager offHandSortingManager;
    private Transform backpackHolder;
    private Camera mainCamera;
    private Transform flashlightTransform;
    private float nextFlashlightLookupTime = 0f;

    // Smooth remote weapon rotation: track a separate float to avoid rotation accumulation
    // bugs that occurred with reading back eulerAngles and LerpAngle-ing toward the target.
    private float _smoothedRemoteAimAngle;
    private bool _smoothedRemoteAngleInitialized = false;

    // Local-only cached angle used to stabilize close-range cursor aiming.
    private float _lastStableLocalAimAngle;
    private bool _hasLastStableLocalAimAngle;

    // Remote-player movement detection: track position change per frame
    // because the local 'movement' input vector is always zero for non-owners.
    private Vector3 _prevRemotePosition;
    private bool _remoteIsMoving;

    // NOTE: We previously had _smoothedRemoteAimAngle / LerpAngle interpolation here
    // to smooth tick-rate choppiness. This caused rotation accumulation bugs where the
    // weapon kept spinning. Removed in favour of using _syncAimAngle.Value directly —
    // FishNet SyncVar updates are frequent enough and correctness > smoothness.

    public bool IsMoving()
    {
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        if (isNetworkActive && !IsOwner)
            return _remoteIsMoving;

        if (IsMovementBlockedByEffects())
            return false;

        return movement.magnitude > 0.1f;
    }

    private float GetStableLocalAimAngle(Vector3 aimOrigin, Vector3 mouseWorldPos)
    {
        Vector2 toMouse = (Vector2)(mouseWorldPos - aimOrigin);
        float stabilityRadius = Mathf.Max(0.01f, closeAimStabilityRadius);

        if (toMouse.sqrMagnitude >= stabilityRadius * stabilityRadius)
        {
            float stableAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            _lastStableLocalAimAngle = stableAngle;
            _hasLastStableLocalAimAngle = true;
            return stableAngle;
        }

        if (_hasLastStableLocalAimAngle)
        {
            return _lastStableLocalAimAngle;
        }

        float fallbackAngle = isFacingLeft ? 180f : 0f;
        _lastStableLocalAimAngle = fallbackAngle;
        _hasLastStableLocalAimAngle = true;
        return fallbackAngle;
    }

    /// <summary>
    /// Finds and caches a child transform named "Flashlight" under this player.
    /// Retries at a low rate because the object may be instantiated after startup.
    /// </summary>
    private Transform GetFlashlightTransform()
    {
        if (flashlightTransform != null)
            return flashlightTransform;

        if (Time.time < nextFlashlightLookupTime)
            return null;

        nextFlashlightLookupTime = Time.time + 0.5f;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == "Flashlight")
            {
                flashlightTransform = child;
                break;
            }
        }

        return flashlightTransform;
    }

    /// <summary>
    /// Rotates the flashlight to match the provided world-space aim angle.
    /// </summary>
    private void UpdateFlashlightRotation(float aimAngle)
    {
        Transform flashlight = GetFlashlightTransform();
        if (flashlight == null)
            return;

        flashlight.rotation = Quaternion.Euler(0f, 0f, aimAngle);
    }

    public Vector2 GetMovementInput() => movement;

    /// <summary>
    /// Get the synced character name (works for both owner and remote players)
    /// </summary>
    public string GetSyncedCharacterName()
    {
        return _syncCharacterName.Value;
    }

    /// <summary>
    /// Get the character's unique save/persistence name (characterName, not displayName).
    /// Tries currentCharacterData first, then assignedCharacterName (set by NetworkSpawner
    /// before spawn so it's available even before character data finishes loading),
    /// then the singleton SelectedCharacter as a last resort.
    /// </summary>
    public string GetCharacterSaveName()
    {
        if (currentCharacterData != null && !string.IsNullOrEmpty(currentCharacterData.characterName))
            return currentCharacterData.characterName;

        if (!string.IsNullOrEmpty(assignedCharacterName))
            return assignedCharacterName;

        if (CharacterSelectionManager.SelectedCharacter != null &&
            !string.IsNullOrEmpty(CharacterSelectionManager.SelectedCharacter.characterName))
            return CharacterSelectionManager.SelectedCharacter.characterName;

        return null;
    }
    public string GetCurrentAnimation() => currentAnimationPlaying;
    public CharacterData GetCurrentCharacterData()
    {
        if (currentCharacterData != null)
        {
            _loggedMissingCharacterData = false;
            Debug.Log($"[PlayerController] Returning CharacterData: {currentCharacterData.displayName}");
        }
        else
        {
            bool isNetworkActive = IsServerStarted || IsClientStarted;
            bool shouldWarn = !isNetworkActive || IsOwner;

            // During network init / sync windows this can be temporarily null.
            if (shouldWarn && !_loggedMissingCharacterData)
            {
                _loggedMissingCharacterData = true;
                Debug.LogWarning($"[PlayerController] currentCharacterData is NULL on instance {gameObject.GetInstanceID()}!");
            }
        }
        return currentCharacterData;
    }

    /// <summary>
    /// Call this after gear is equipped to refresh animator reference
    /// </summary>
    public void RefreshGearAnimators()
    {
        if (gearManager != null)
        {
            legsAnimator = gearManager.LegsAnimator;

            spriteRenderer = gearManager.LegsSpriteRenderer;
        }
        else
        {
            Debug.LogError("[PlayerController] GearManager is null, cannot refresh animator!");
        }
    }

    /// <summary>
    /// Gate animation playback until core gear visuals are fully equipped.
    /// </summary>
    public void SetGearAnimationReady(bool ready)
    {
        if (isGearAnimationReady == ready) return;

        if (!ready)
        {
            CaptureCurrentAnimationForGearResume();
        }

        isGearAnimationReady = ready;
        Debug.Log($"[PlayerController] Gear animation ready: {isGearAnimationReady} on {gameObject.name}");

        if (!isGearAnimationReady) return;

        RefreshGearAnimators();

        if (!RestartQueuedAnimationOnGear())
        {
            // If there is no prior animation snapshot, compute a valid pose now.
            ForceAnimationUpdate();
            return;
        }

        // Keep weapon sorting/aim visuals in sync after resuming gear animation.
        if (currentCharacterData != null)
        {
            ForceAnimationUpdate();
        }
    }

    private void CaptureCurrentAnimationForGearResume()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalized = stateInfo.normalizedTime % 1f;
        if (normalized < 0f)
        {
            normalized += 1f;
        }

        string animName = currentAnimationPlaying;
        if (string.IsNullOrEmpty(animName))
        {
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                animName = clipInfo[0].clip.name;
            }
        }

        if (string.IsNullOrEmpty(animName)) return;

        queuedResumeAnimationName = animName;
        queuedResumeNormalizedTime = normalized;
        hasQueuedAnimationResume = true;
    }

    private bool RestartQueuedAnimationOnGear()
    {
        if (!hasQueuedAnimationResume) return false;
        if (string.IsNullOrEmpty(queuedResumeAnimationName)) return false;

        // Replaying from time 0 ensures newly-instantiated gear animators (head/chest)
        // start in sync immediately after equip/load.
        string animationToRestart = queuedResumeAnimationName;

        hasQueuedAnimationResume = false;
        queuedResumeAnimationName = string.Empty;
        queuedResumeNormalizedTime = 0f;

        PlayAnimation(animationToRestart, 0f);

        if (animator != null)
        {
            animator.Update(0f);
        }

        if (gearManager != null)
        {
            Animator[] gearAnimators = gearManager.GetAllGearAnimators();
            foreach (Animator gearAnimator in gearAnimators)
            {
                if (gearAnimator == null || gearAnimator == animator) continue;
                gearAnimator.Update(0f);
            }
        }

        return true;
    }

    protected override void Awake()
    {
        base.Awake();
        gearManager = GetComponent<PlayerGearManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Animator will be set after gear is equipped
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        abilityManager = GetComponent<CharacterAbilityManager>();
        traitManager = GetComponent<CharacterTraitManager>();
        effectManager = GetComponent<EffectManager>();

        if (effectManager == null)
        {
            effectManager = GetComponentInChildren<EffectManager>();
        }

        LevelUpRewardDirector rewardDirector = GetComponent<LevelUpRewardDirector>();
        if (rewardDirector == null)
        {
            rewardDirector = gameObject.AddComponent<LevelUpRewardDirector>();
        }

        // Get or add CharacterGearManager for gear stat modifiers
        characterGearManager = GetComponent<CharacterGearManager>();
        if (characterGearManager == null)
        {
            characterGearManager = gameObject.AddComponent<CharacterGearManager>();
        }

        // Ensure StatConverter is present for dynamic stat conversions
        statConverter = GetComponent<StatConverter>();
        if (statConverter == null)
        {
            statConverter = gameObject.AddComponent<StatConverter>();
        }

        // Subscribe to trait changes for dynamic stat updates
        if (traitManager != null)
        {
            traitManager.OnTraitsChanged += OnTraitsChanged;
        }

        // Subscribe to gear changes for dynamic stat updates
        if (characterGearManager != null)
        {
            characterGearManager.OnGearModifiersChanged += OnGearModifiersChanged;
        }

        // Initialize sorting managers (only if not already present)
        if (mainHandSortingManager == null)
            mainHandSortingManager = gameObject.AddComponent<WeaponSortingManager>();
        if (offHandSortingManager == null)
            offHandSortingManager = gameObject.AddComponent<WeaponSortingManager>();

        // Find existing backpack holder in hierarchy (typically child of ChestHolder)
        if (gearManager != null)
        {
            // BackpackHolder is a Transform, get it from the GearManager's serialized field
            // It's assigned in the PlayerCharacter prefab inspector
            backpackHolder = transform.Find("FeetHolder/ChestHolder/BackpackHolder");
            if (backpackHolder == null)
            {
                Debug.LogWarning("[PlayerController] BackpackHolder not found at FeetHolder/ChestHolder/BackpackHolder!");
            }
        }

        if (abilityManager == null)
        {
            abilityManager = gameObject.AddComponent<CharacterAbilityManager>();
        }

        // CRITICAL: Apply character data BEFORE notifying listeners
        // NOTE: In multiplayer, ApplySelectedCharacter will be called in OnStartNetwork when ownership is established
        // In single-player, network won't be active so we can call it here
        if (!IsServerStarted && !IsClientStarted)
        {
            ApplySelectedCharacter();
        }
        else
        {
            Debug.Log("[PlayerController] Deferring ApplySelectedCharacter until OnStartNetwork (network active)");
        }

        InitializeInputSystem();

        // Initialize footstep particles
        if (footstepParticles != null)
        {
            footstepParticles.Stop();
        }

        // Fire event AFTER character data is loaded
        // Single-player path: network not active yet, so we are definitely the local player.
        if (!IsServerStarted && !IsClientStarted)
        {
            Debug.Log($"[PlayerController] Single-player mode - setting LocalPlayer and firing OnPlayerSpawned");
            LocalPlayer = this;
            // Only fire in single-player — character data is loaded in Awake.
            // In multiplayer, character loading is deferred to OnStartNetwork,
            // and OnPlayerSpawned fires there after ApplySelectedCharacter completes.
            OnPlayerSpawned?.Invoke(this);
        }

        if (gearManager != null)
        {
            SetGearAnimationReady(gearManager.IsCoreGearReady);
            gearManager.OnCoreGearReadyChanged += SetGearAnimationReady;
        }
    }

    private void OnEnable()
    {
        // If LoadCharacterTraits deferred its coroutine because the GO was inactive
        // (happens on FishNet client-side spawns), run it now.
        if (_pendingTraitRecalc)
        {
            _pendingTraitRecalc = false;
            StartCoroutine(RecalculateStatsAfterTraitLoad());
        }
    }

    /// <summary>
    /// Assign a specific character to this player instance (called by NetworkSpawner before spawn)
    /// This allows each player networkObject to load a different character
    /// </summary>
    public void AssignCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("[PlayerController] Cannot assign null/empty character name!");
            return;
        }

        assignedCharacterName = characterName;
        Debug.Log($"[PlayerController] Character '{characterName}' assigned to this player instance");

        // If character selection manager exists, load the character immediately
        // This handles the case where AssignCharacter is called after Awake (e.g., by NetworkSpawner)
        if (CharacterSelectionManager.Instance != null)
        {
            Debug.Log($"[PlayerController] Loading character immediately after assignment");
            LoadCharacterByIndex(0); // Index doesn't matter, assigned name takes priority
        }
        else
        {
            Debug.LogWarning($"[PlayerController] CharacterSelectionManager not available yet, character will load in OnStartNetwork or ApplySelectedCharacter");
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // DontDestroyOnLoad is called by NetworkSpawner before Spawn() on the server.
        // FishNet propagates that persistence to all client-side instances automatically,
        // so it must NOT be called here (FishNet error FN0002 forbids it in NetworkBehaviour).

        bool isLocalClient = base.Owner.IsLocalClient;

        // Subscribe to SyncVar changes
        _syncSelectedCharacterIndex.OnChange += OnCharacterIndexChanged;
        _syncCharacterName.OnChange += OnCharacterNameChanged;
        _syncClassName.OnChange += OnClassNameChanged;

        // Mark network as initialized - allows OnCharacterIndexChanged to process changes
        networkInitialized = true;
        Debug.Log($"[NET] [PlayerController] Network initialization complete, calling ApplySelectedCharacter (IsOwner={base.Owner.IsLocalClient})");

        // Apply character data now that ownership is established
        ApplySelectedCharacter();

        // Re-initialize input now that network ownership is established
        if (isLocalClient && !inputSystemReady)
        {
            Debug.Log($"[NET] [PlayerController] Re-initializing input system for network owner");
            InitializeInputSystem();
        }

        // Cache the local player reference now that ownership is confirmed.
        // Fire OnPlayerSpawned again so any UI scene that loaded after the initial
        // Awake-time fire (e.g. TraitTree loaded additively mid-session) gets the event.
        if (isLocalClient)
        {
            LocalPlayer = this;
            Debug.Log($"[NET] [PlayerController] LocalPlayer cached and OnPlayerSpawned re-fired for late subscribers");
            OnPlayerSpawned?.Invoke(this);
        }

        // Remote players (non-owners) must have a kinematic Rigidbody2D so that local
        // physics collisions cannot impart velocity on them. Their position is driven
        // entirely by NetworkTransform; a non-kinematic rb would accumulate impulses
        // from collisions and slide uncontrollably since HandleMovement() never runs
        // for non-owners (the update loop returns early for remote players).
        if (rb != null)
        {
            rb.isKinematic = !base.Owner.IsLocalClient;
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        // Unsubscribe SyncVar change handlers to prevent leaks on reconnect/scene change
        _syncSelectedCharacterIndex.OnChange -= OnCharacterIndexChanged;
        _syncCharacterName.OnChange -= OnCharacterNameChanged;
        _syncClassName.OnChange -= OnClassNameChanged;

        networkInitialized = false;
    }

    private void ApplySelectedCharacter()
    {
        // In single-player or if we're the owner, set character selection
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        bool shouldSetCharacter = !isNetworkActive || IsOwner;

        if (shouldSetCharacter)
        {
            // Read the selected character from local PlayerPrefs
            int selectedChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
            // Route through ServerRpc so the server writes the SyncVar and it propagates to all clients
            if (isNetworkActive)
            {
                ServerRpcSetCharacterIndex(selectedChar);
            }
            else
            {
                _syncSelectedCharacterIndex.Value = selectedChar; // Single-player: write directly (we ARE the server)
            }

            // CRITICAL FIX: Don't sync character name here from singleton - wait until actual character data loads
            // The name will be synced in SetupCharacter() after currentCharacterData is properly loaded

            // CRITICAL: If we don't have an assigned character yet, register our selection with NetworkSpawner
            // This ensures each player gets their own character instead of all sharing the singleton
            if (string.IsNullOrEmpty(assignedCharacterName) && isNetworkActive && CharacterSelectionManager.SelectedCharacter != null)
            {
                assignedCharacterName = CharacterSelectionManager.SelectedCharacter.characterName;

                // Get our connection and register our character choice
                NetworkConnection conn = base.Owner;
                if (conn != null)
                {
                    NetworkSpawner.RegisterCharacterForConnection(conn.ClientId, assignedCharacterName);
                    Debug.Log($"[PlayerController] Registered character '{assignedCharacterName}' for our connection (ClientId: {conn.ClientId})");
                }
            }

            Debug.Log($"[PlayerController] Owner set selectedCharacterIndex to {selectedChar} (NetworkActive={isNetworkActive})");
        }

        // Apply the character (will be called again via OnCharacterIndexChanged when synced)
        LoadCharacterByIndex(_syncSelectedCharacterIndex.Value);
    }

    /// <summary>
    /// Callback when character name changes (called on all clients when synced)
    /// </summary>
    private void OnCharacterNameChanged(string prev, string next, bool asServer)
    {
        Debug.Log($"[NET] OnCharacterNameChanged: '{prev}' -> '{next}' (asServer={asServer}, IsOwner={IsOwner}, obj={gameObject.name});");
        if (currentCharacterData != null)
        {
            currentCharacterData.characterName = next;
        }

        // Always trigger floating name update — FloatingCharacterName falls back to
        // GetSyncedCharacterName() when currentCharacterData is null (remote players).
        FloatingCharacterName nameDisplay = GetComponentInChildren<FloatingCharacterName>();
        if (nameDisplay != null)
        {
            nameDisplay.SendMessage("FindPlayerAndSetup", SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// Callback when class name changes (called on all clients when synced).
    /// Used for logging and class-based visual setup.
    /// </summary>
    private void OnClassNameChanged(string prev, string next, bool asServer)
    {
        Debug.Log($"[NET] OnClassNameChanged: '{prev}' -> '{next}' (asServer={asServer}, IsOwner={IsOwner}, obj={gameObject.name})");
        // Visual setup for remote players is now handled by ObserversRpcSyncVisuals.
    }

    /// <summary>
    /// Callback when selectedCharacterIndex changes (called on all clients when synced)
    /// </summary>
    private void OnCharacterIndexChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[NET] OnCharacterIndexChanged: {prev} -> {next} (asServer={asServer}, IsOwner={IsOwner}, networkInit={networkInitialized}, obj={gameObject.name})");

        // CRITICAL: Skip during initial SyncVar synchronization (before OnStartNetwork completes)
        // At this point, IsOwner is NOT reliable yet and will cause duplicate character creation
        // ApplySelectedCharacter() in Awake already handles initial character loading
        if (!networkInitialized)
        {
            Debug.Log($"[NET] SKIPPING OnCharacterIndexChanged - network not initialized yet");
            return;
        }

        Debug.Log($"[Joe123] [PlayerController] Processing character index change (network initialized, IsOwner={IsOwner})");
        int newValue = next;
        LoadCharacterByIndex(newValue);

        // After character loads, ensure gear is equipped (important for non-owners who missed initial gear setup)
        // Don't run for host (IsServerStarted) even if IsOwner not set yet due to timing
        if (!IsOwner && !IsServerStarted && currentCharacterData != null)
        {
            Debug.Log($"[PlayerController] Scheduling RefreshGearAfterCharacterSync for remote player");
            StartCoroutine(RefreshGearAfterCharacterSync());
        }
        else
        {
            Debug.Log($"[PlayerController] Skipping RefreshGearAfterCharacterSync - IsOwner:{IsOwner}, IsServer:{IsServerStarted}");
        }
    }

    /// <summary>
    /// Refresh gear after character data syncs to remote clients
    /// </summary>
    private System.Collections.IEnumerator RefreshGearAfterCharacterSync()
    {
        yield return new WaitForSeconds(0.5f); // Wait for SetupCharacter to complete

        if (currentCharacterData != null)
        {
            Debug.Log($"[PlayerController] Refreshing gear for remote character: {currentCharacterData.displayName}");
            PlayerGearManager gearManager = GetComponent<PlayerGearManager>();

            if (gearManager != null && currentCharacterData.classData != null)
            {
                // Equip starting gear visually (stats don't matter for remote players)
                gearManager.EquipStartingGear(currentCharacterData.classData);
                RefreshGearAnimators();
            }
        }
    }

    /// <summary>
    /// Load character data by index from CharacterSelectionManager
    /// </summary>
    private void LoadCharacterByIndex(int characterIndex)
    {
        Debug.Log($"[ownership] ========== LoadCharacterByIndex CALLED ==========");
        Debug.Log($"[ownership] characterIndex: {characterIndex}, IsOwner: {IsOwner}, assignedCharacterName: '{assignedCharacterName}', obj: {gameObject.name}");
        Debug.Log($"[Joe123] [PlayerController] assignedCharacterName: '{assignedCharacterName}'");

        // Remote players should wait for network sync
        // They don't need CharacterSelectionManager - visuals arrive via ObserversRpcSyncVisuals
        if (!IsOwner)
        {
            Debug.Log("[PlayerController] Remote player - waiting for ObserversRpcSyncVisuals, not loading from CharacterSelectionManager");
            return;
        }

        // Early-out: skip if this named character is already fully loaded.
        // AssignCharacter, ApplySelectedCharacter, and OnCharacterIndexChanged all route here
        // during initialization — only the FIRST successful load is needed. Every subsequent
        // call creates a NEW CharacterData object that replaces the shared reference held by
        // CTM and TSM, causing object divergence (stale TSM.currentCharacterData != PC's live
        // object) and trait data loss on the next periodic save.
        if (currentCharacterData != null
            && !string.IsNullOrEmpty(assignedCharacterName)
            && currentCharacterData.characterName == assignedCharacterName)
        {
            Debug.Log($"[PlayerController] LoadCharacterByIndex: '{assignedCharacterName}' already loaded — skipping redundant load.");
            return;
        }

        // Owner needs CharacterSelectionManager
        if (CharacterSelectionManager.Instance == null)
        {
            Debug.LogError("[PlayerController] CharacterSelectionManager.Instance is null! Creating bootstrap...");

            // Bootstrap CharacterSelectionManager if it doesn't exist
            GameObject bootstrapObj = new GameObject("CharacterSelectionManager");
            CharacterSelectionManager manager = bootstrapObj.AddComponent<CharacterSelectionManager>();

            // Load config from Resources
            CharacterSelectionConfig config = Resources.Load<CharacterSelectionConfig>("CharacterSelectionConfig");
            if (config != null)
            {
                var configField = typeof(CharacterSelectionManager).GetField("config",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(manager, config);
                Debug.Log("[PlayerController] CharacterSelectionManager bootstrapped successfully");
            }
            else
            {
                Debug.LogError("[PlayerController] Could not load CharacterSelectionConfig from Resources!");
                return;
            }
        }

        Debug.Log($"[Joe123] [PlayerController] SelectedCharacter status: {(CharacterSelectionManager.SelectedCharacter == null ? "NULL" : CharacterSelectionManager.SelectedCharacter.characterName)}");

        // PRIORITY 1: Use assigned character (set by NetworkSpawner for per-player assignment)
        if (!string.IsNullOrEmpty(assignedCharacterName))
        {
            Debug.Log($"[PlayerController] Loading assigned character: '{assignedCharacterName}'");

            // Load character by name from persistence
            CharacterSelectionConfig config = Resources.Load<CharacterSelectionConfig>("CharacterSelectionConfig");
            if (config == null)
            {
                Debug.LogError("[PlayerController] Could not load CharacterSelectionConfig from Resources!");
            }
            else
            {
                CharacterData loadedChar = CharacterPersistence.LoadCharacter(assignedCharacterName, config);

                if (loadedChar != null)
                {
                    currentCharacterData = loadedChar;
                    Debug.Log($"[PlayerController] Successfully loaded assigned character: {currentCharacterData.characterName}");

                    // Validate and setup
                    if (currentCharacterData != null)
                    {
                        float maxHealth = currentCharacterData.statContainer.GetStat("MaxHealth");
                        if (maxHealth == 0f)
                        {
                            Debug.LogError($"[PlayerController] Character {currentCharacterData.displayName} has invalid stats! Conversions may be missing.");
                        }

                        // Sync class name for remote players
                        if (IsOwner && (IsServerStarted || IsClientStarted))
                        {
                            ServerRpcSetClassName(currentCharacterData.classData.className);
                            Debug.Log($"[PlayerController] Synced class name via ServerRpc: {currentCharacterData.classData.className}");
                        }

                        SetupCharacter();
                        BroadcastVisuals();
                    }
                    return;
                }
                else
                {
                    Debug.LogError($"[PlayerController] Failed to load assigned character '{assignedCharacterName}'! Falling back to singleton.");
                }
            }
        }

        // PRIORITY 2: Use CharacterSelectionManager.SelectedCharacter (singleton fallback)
        if (CharacterSelectionManager.SelectedCharacter == null)
        {
            Debug.LogError("[Joe123] [PlayerController] ========== CRITICAL ERROR ==========");
            Debug.LogError("[Joe123] [PlayerController] CharacterSelectionManager.SelectedCharacter is NULL and no assigned character!");
            Debug.LogError("[PlayerController] No character selected! Please select a character from CharacterSelection scene.");
            Debug.LogError("[Joe123] [PlayerController] This means the character was not properly set before loading CommandScene.");
            return;
        }

        Debug.Log($"[Joe123] [PlayerController] Using SelectedCharacter: '{CharacterSelectionManager.SelectedCharacter.characterName}'");
        currentCharacterData = CharacterSelectionManager.SelectedCharacter;
        Debug.Log($"[Joe123] [PlayerController] currentCharacterData SET - name: '{currentCharacterData.characterName}', equippedGear: {(currentCharacterData.equippedGear == null ? "NULL" : currentCharacterData.equippedGear.Count.ToString())}");

        Debug.Log($"[Joe123] [PlayerController] Character loaded: {currentCharacterData.characterName} (IsOwner={IsOwner})");
        Debug.Log($"[PlayerController] Loaded character: {currentCharacterData.displayName} (IsOwner={IsOwner})");

        // Validate stats
        if (currentCharacterData != null)
        {
            float maxHealth = currentCharacterData.statContainer.GetStat("MaxHealth");
            if (maxHealth == 0f)
            {
                Debug.LogError($"[PlayerController] Character {currentCharacterData.displayName} has invalid stats! Conversions may be missing.");
            }

            // Sync class name for remote players
            if (IsOwner && (IsServerStarted || IsClientStarted))
            {
                ServerRpcSetClassName(currentCharacterData.classData.className);
                Debug.Log($"[PlayerController] Synced class name via ServerRpc: {currentCharacterData.classData.className}");
            }

            SetupCharacter();
            BroadcastVisuals();
        }
    }

    /// <summary>
    /// Master initialization sequence for character loading.
    /// All loading steps are centralized here in explicit order for visibility and debugging.
    /// Runs for BOTH owners (locally loaded) and non-owners (deserialized from network).
    /// </summary>
    private void SetupCharacter()
    {
        Debug.Log($"[ownership] ========== SetupCharacter STARTED for '{(currentCharacterData == null ? "NULL" : currentCharacterData.characterName)}' ==========");
        Debug.Log($"[ownership] currentCharacterData: {(currentCharacterData == null ? "NULL" : currentCharacterData.characterName)}, IsOwner: {IsOwner}, obj: {gameObject.name}");

        if (currentCharacterData == null)
        {
            Debug.LogError("[PlayerController] ABORTED SetupCharacter - currentCharacterData is NULL!");
            return;
        }

        Debug.Log($"[PlayerController] Character details - name: '{currentCharacterData.characterName}', class: '{currentCharacterData.classData?.className}', equippedGear count: {(currentCharacterData.equippedGear == null ? "NULL" : currentCharacterData.equippedGear.Count.ToString())}");

        // Sync the actual character's display name (owner only)
        if (IsOwner && (IsServerStarted || IsClientStarted))
        {
            ServerRpcSetCharacterName(currentCharacterData.displayName);
            Debug.Log($"[PlayerController] Synced character display name via ServerRpc: '{currentCharacterData.displayName}'");
        }

        // ========== PHASE 1: CORE DATA LOADING ==========
        // 1.1 - Load character stats from saved data
        LoadCharacterStats();
        // 1.2 - Initialize sorting managers for weapon rendering
        InitializeSortingManagers();

        // ========== PHASE 2: TRAIT & STAT SYSTEM ==========
        // 2.1 - Load character traits (async, will recalculate stats when done)
        LoadCharacterTraits();
        // 2.2 - Subscribe to stat change events for dynamic updates
        SubscribeToStatChanges();

        // ========== PHASE 3: VISUAL & ANIMATION ==========
        // 3.1 - Load character animations from ClassData
        LoadCharacterAnimations();
        // 3.2 - Setup footstep particle effects
        SetupFootstepParticles();
        // 3.3 - Defer animation start until gear visuals are fully equipped
        SetGearAnimationReady(false);

        // ========== PHASE 4: ABILITIES & COMBAT ==========
        // 4.1 - Load character abilities from ability loadout
        LoadCharacterAbilities();

        LevelUpRewardDirector rewardDirector = GetComponent<LevelUpRewardDirector>();
        rewardDirector?.SetCharacterData(currentCharacterData);

        // ========== PHASE 5: EQUIPMENT & GEAR ==========
        // 5.1 - Load equipped gear modifiers from CharacterData (stat bonuses)
        LoadEquippedGearModifiers();
        // 5.1b - Recalculate stats now so gear modifiers are included.
        //        LoadEquippedGear intentionally skips OnGearModifiersChanged to avoid
        //        firing before the character is fully built, so we trigger it here.
        RecalculateStatsWithTraits();
        // 5.2 - Load visual gear from CharacterData
        //       If we have equipped gear data (owner or remote with full CharacterData), use it.
        //       Otherwise, fall back to starter gear from ClassData.
        if (currentCharacterData.equippedGear != null && currentCharacterData.equippedGear.Count > 0)
        {
            Debug.Log($"[ownership] Loading saved visual gear for '{currentCharacterData.characterName}' ({currentCharacterData.equippedGear.Count} items, IsOwner={IsOwner})");
            LoadSavedVisualGear();
        }
        else
        {
            Debug.Log($"[ownership] No equipped gear data, loading starter gear for '{currentCharacterData.characterName}' (IsOwner={IsOwner})");
            LoadStarterGearNow();
        }

        // 5.3 - Refresh gear UI for local player (ensures UI displays after death/respawn)
        if (IsOwner)
        {
            GearPanelUI gearPanel = FindFirstObjectByType<GearPanelUI>();
            if (gearPanel != null)
            {
                Debug.Log($"[PlayerController] Refreshing GearPanelUI for '{currentCharacterData.characterName}'");
                gearPanel.RefreshDisplay();
            }
        }

        // 5.4 - Refresh sprite renderers for damage flash now that gear is equipped
        RefreshSpriteRenderers();

        // Start animation only after gear manager confirms core visual pieces are ready.
        if (gearManager != null)
        {
            SetGearAnimationReady(gearManager.IsCoreGearReady);
        }

        // ========== PHASE 6: COMPANIONS ==========
        // 6.1 - Spawn pet if character has one
        SpawnPet();

        // ========== PHASE 7: INITIAL PERSIST ==========
        // Write character data to PlayerPrefs only for the owning player.
        // Remote instances (deserialized from network) must NOT save to local PlayerPrefs.
        if (IsOwner && currentCharacterData != null)
        {
            CharacterPersistence.SaveCharacter(currentCharacterData);
            Debug.Log($"[ownership] Initial save written for '{currentCharacterData.characterName}' (owner-only)");
        }

        Debug.Log($"[ownership] ========== SetupCharacter COMPLETE for '{currentCharacterData.characterName}' (IsOwner={IsOwner}, obj={gameObject.name}) ==========");
    }

    /// <summary>
    /// 1.1 - Load character stats from CharacterData.statContainer
    /// Stats are already calculated and saved, just copy them to runtime AllStats
    /// </summary>
    private void LoadCharacterStats()
    {
        Debug.Log("[PlayerController] [1.1] Loading character stats...");
        ApplyCharacterStats();
    }

    /// <summary>
    /// 1.2 - Initialize weapon sorting managers for proper rendering order
    /// </summary>
    private void InitializeSortingManagers()
    {
        mainHandSortingManager?.Initialize(spriteRenderer, currentCharacterData);
        offHandSortingManager?.Initialize(spriteRenderer, currentCharacterData);
    }

    /// <summary>
    /// 2.1 - Load character traits and schedule stat recalculation
    /// Traits modify stats, so they must be loaded before final stat calculations
    /// </summary>
    private void LoadCharacterTraits()
    {

        if (traitManager != null)
        {
            traitManager.SetCharacterData(currentCharacterData);

            // Traits load asynchronously, recalculate stats after load completes.
            // Guard: FishNet may invoke OnStartNetwork on a client-side clone that
            // is still inactive — StartCoroutine would throw. Defer until active.
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(RecalculateStatsAfterTraitLoad());
            }
            else
            {
                Debug.LogWarning("[PlayerController] Skipping trait coroutine — GameObject is inactive, will recalculate when enabled");
                _pendingTraitRecalc = true;
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] CharacterTraitManager not found!");
        }
    }

    /// <summary>
    /// 3.1 - Load character animations from ClassData animator controller
    /// </summary>
    private void LoadCharacterAnimations()
    {
        if (TryLoadCharacterAnimationsNow())
        {
            if (_loadAnimationsRetryCoroutine != null)
            {
                StopCoroutine(_loadAnimationsRetryCoroutine);
                _loadAnimationsRetryCoroutine = null;
            }
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[PlayerController] Delaying animation load - GameObject is inactive");
            return;
        }

        if (_loadAnimationsRetryCoroutine != null)
        {
            StopCoroutine(_loadAnimationsRetryCoroutine);
        }
        _loadAnimationsRetryCoroutine = StartCoroutine(RetryLoadCharacterAnimations());
    }

    private bool TryLoadCharacterAnimationsNow()
    {
        if (currentCharacterData == null)
        {
            return false;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        RuntimeAnimatorController controller = currentCharacterData.GetAnimatorController();
        if (animator == null || controller == null)
        {
            return false;
        }

        animator.runtimeAnimatorController = controller;
        return true;
    }

    private System.Collections.IEnumerator RetryLoadCharacterAnimations()
    {
        const int maxFrames = 10;
        for (int i = 0; i < maxFrames; i++)
        {
            if (TryLoadCharacterAnimationsNow())
            {
                _loadAnimationsRetryCoroutine = null;
                yield break;
            }
            yield return null;
        }

        string className = currentCharacterData?.classData != null ? currentCharacterData.classData.className : "(null classData)";
        Debug.LogWarning($"[PlayerController] Cannot load animations - animator or controller missing (animator={(animator != null)}, class={className})");
        _loadAnimationsRetryCoroutine = null;
    }

    /// <summary>
    /// 4.1 - Load character abilities from ability loadout
    /// </summary>
    private void LoadCharacterAbilities()
    {
        if (abilityManager != null)
        {
            LoadCharacterAbilitiesInternal();
        }
        else
        {
            Debug.LogWarning("[PlayerController] CharacterAbilityManager not found!");
        }
    }

    /// <summary>
    /// 5.2 - Load equipped gear modifiers from CharacterData
    /// This ensures gear stat bonuses are applied on character initialization
    /// </summary>
    private void LoadEquippedGearModifiers()
    {

        if (characterGearManager != null && currentCharacterData != null)
        {
            if (currentCharacterData.equippedGear != null && currentCharacterData.equippedGear.Count > 0)
            {
                characterGearManager.LoadEquippedGear(currentCharacterData.equippedGear);
            }
            else
            {
                Debug.Log("[PlayerController] No equipped gear found in CharacterData");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] Cannot load gear modifiers - CharacterGearManager or CharacterData is null");
        }

    }

    /// <summary>
    /// 5.3 - Load saved visual gear from CharacterData
    /// Uses the SAME equipping logic as manual inventory→gear slot dragging for consistency
    /// </summary>
    private void LoadSavedVisualGear()
    {
        if (currentCharacterData == null)
        {
            Debug.LogError("[PlayerController] Cannot load visual gear - no character data");
            return;
        }

        if (currentCharacterData.equippedGear == null || currentCharacterData.equippedGear.Count == 0)
        {
            Debug.Log($"[PlayerController] No equipped gear found for {currentCharacterData.characterName}, loading starter gear");
            LoadStarterGearNow();
            return;
        }

        Debug.Log($"[PlayerController] Loading {currentCharacterData.equippedGear.Count} equipped items for {currentCharacterData.characterName}");

        // ---------- PRE-PASS: resolve weapon configs BEFORE spawning ----------
        // First resolve main hand to check if it's 2-handed
        WeaponConfig mainHandConfig = null;
        if (currentCharacterData.equippedGear.TryGetValue(GearSlot.Weapon, out ItemInstance mainHandItem)
            && mainHandItem != null && mainHandItem.itemType.ToLower() == "weapon"
            && !string.IsNullOrEmpty(mainHandItem.additionalData))
        {
            WeaponGearData mainHandData = JsonUtility.FromJson<WeaponGearData>(mainHandItem.additionalData);
            if (mainHandData != null && !string.IsNullOrEmpty(mainHandData.weaponConfigName))
            {
                WeaponItemDropsConfig wic = WeaponItemDropsConfig.DefaultInstance;
                mainHandConfig = wic?.GetWeaponConfigByName(mainHandData.weaponConfigName);
            }
        }

        // Only resolve offhand if main hand is NOT 2-handed
        if (mainHandConfig != null && mainHandConfig.is2Handed)
        {
            Debug.Log($"[PlayerController] Main weapon '{mainHandConfig.weaponName}' is 2-handed, skipping offhand config");
            currentCharacterData.hasDualWeapons = false;
            currentCharacterData.offHandWeaponConfig = null;
        }
        else if (currentCharacterData.equippedGear.TryGetValue(GearSlot.OffHandWeapon, out ItemInstance offhandItem)
            && offhandItem != null && offhandItem.itemType.ToLower() == "weapon"
            && !string.IsNullOrEmpty(offhandItem.additionalData))
        {
            WeaponGearData offhandData = JsonUtility.FromJson<WeaponGearData>(offhandItem.additionalData);
            if (offhandData != null && !string.IsNullOrEmpty(offhandData.weaponConfigName))
            {
                WeaponItemDropsConfig wic = WeaponItemDropsConfig.DefaultInstance;
                WeaponConfig offhandConfig = wic?.GetWeaponConfigByName(offhandData.weaponConfigName);
                if (offhandConfig != null && offhandConfig.isOffhand)
                {
                    currentCharacterData.offHandWeaponConfig = offhandConfig;
                    currentCharacterData.hasDualWeapons = true;
                    Debug.Log($"[PlayerController] Pre-loaded offhand weapon config: {offhandConfig.weaponName}");
                }
            }
        }

        // ---------- MAIN PASS: equip each slot ----------
        foreach (var kvp in currentCharacterData.equippedGear)
        {
            GearSlot slot = kvp.Key;
            ItemInstance item = kvp.Value;

            if (item == null)
            {
                Debug.Log($"[PlayerController] Slot {slot} is empty, loading starter for this slot");
                LoadStarterGearForSlot(slot);
                continue;
            }

            // Skip offhand weapons — already handled in pre-pass; spawned by SpawnWeaponPairOnServer
            if (slot == GearSlot.OffHandWeapon) continue;

            Debug.Log($"[PlayerController] Equipping {item.displayName} to {slot}");

            // Use the SAME equipping logic as InventoryItemUI.EquipToGearSlot
            if (item.itemType.ToLower() == "weapon")
            {
                EquipWeaponVisual(item);
            }
            else if (item.itemType.ToLower() == "armor")
            {
                EquipArmorVisual(item, slot);
            }
        }

        Debug.Log($"[PlayerController] Visual gear loading complete");

        // Broadcast visual snapshot to all observers
        BroadcastVisuals();
    }

    /// <summary>
    /// Called by InventoryItemUI after equipping gear mid-session to push the new
    /// loadout to all connected clients via the gear SyncVar.
    /// </summary>
    public void NotifyGearChanged()
    {
        BroadcastVisuals();
    }

    /// <summary>
    /// Called by TraitSystemManager after a trait is unlocked to push the updated
    /// CharacterData (including the new unlockedNodeIDs) to the network SyncVar.
    /// Without this, any save triggered after the trait spend (level-up, item pickup, etc.)
    /// would re-serialize the SyncVar's stale snapshot and lose the spent trait on reload.
    /// </summary>
    public void NotifyTraitChanged()
    {
        // Traits affect stats which are server-authoritative, but some traits may affect visuals
        // For now, broadcast visuals in case traits grant visual changes
        BroadcastVisuals();
    }

    /// <summary>
    /// ServerRpc proxy for DataDrivenAbility projectile spawning.
    /// DataDrivenAbility is added via AddComponent at runtime so FishNet never registers its
    /// NetworkObject — meaning [ServerRpc] on it is a silent no-op on clients. Routing through
    /// PlayerController (registered at spawn time) ensures the RPC reaches the server.
    /// The server looks up the ability at the given slot and executes the spawn there.
    /// firedFromOffhand mirrors the client's alternating dual-wield hand choice for this shot,
    /// so the server resolves the same weapon/launch-zone overrides the client used.
    /// </summary>
    [ServerRpc]
    public void ServerRpcSpawnAbilityProjectile(int abilitySlot, string abilityName, Vector3 spawnPos, Vector3 direction, float damageMultiplier, uint tick, bool firedFromOffhand = false)
    {
        Debug.Log($"[NET] ServerRpcSpawnAbilityProjectile received on server - slot={abilitySlot}, ability={abilityName}, owner={gameObject.name}, tick={tick}, firedFromOffhand={firedFromOffhand}");
        CharacterAbilityManager mgr = GetComponent<CharacterAbilityManager>();
        if (mgr == null)
        {
            Debug.LogError($"[NET] ServerRpcSpawnAbilityProjectile FAILED - no CharacterAbilityManager on {gameObject.name}");
            return;
        }
        DataDrivenAbility ability = mgr.FindDataDrivenAbility(abilitySlot, abilityName);
        if (ability == null)
        {
            Debug.LogWarning($"[NET] ServerRpcSpawnAbilityProjectile: no DataDrivenAbility at slot {abilitySlot} / ability '{abilityName}' on {gameObject.name}");
            return;
        }
        Debug.Log($"[NET] ServerRpcSpawnAbilityProjectile executing spawn for ability '{ability.AbilityName}'");
        ability.ExecuteServerSpawn(spawnPos, direction, damageMultiplier, tick, firedFromOffhand);
    }

    /// <summary>
    /// ServerRpc proxy for ChannelAbility channel object spawning.
    /// Same rationale as ServerRpcSpawnAbilityProjectile — ChannelAbility is added via AddComponent
    /// and has no registered NetworkObject, so its own [ServerRpc] would be a silent no-op.
    /// Server spawns the channel object as a NetworkObject and sends the reference back to the
    /// owner client via TargetRpcReceiveChannelObject so ChannelAbility can use it locally.
    /// </summary>
    [ServerRpc]
    public void ServerRpcSpawnChannelObject(int abilitySlot, Vector3 spawnPos, Quaternion rotation)
    {
        Debug.Log($"[NET] ServerRpcSpawnChannelObject received on server - slot={abilitySlot}, owner={gameObject.name}");
        CharacterAbilityManager mgr = GetComponent<CharacterAbilityManager>();
        if (mgr == null)
        {
            Debug.LogError($"[NET] ServerRpcSpawnChannelObject FAILED - no CharacterAbilityManager on {gameObject.name}");
            return;
        }
        DataDrivenAbility ability = mgr.GetDataDrivenAbilityAtSlot(abilitySlot);
        if (ability == null)
        {
            Debug.LogWarning($"[NET] ServerRpcSpawnChannelObject: no DataDrivenAbility at slot {abilitySlot} on {gameObject.name}");
            return;
        }

        // Server spawns the channel object and returns a reference via TargetRpc
        GameObject channelObj = ability.SpawnChannelObjectOnServer(spawnPos, rotation);
        if (channelObj == null) return;

        NetworkObject netObj = channelObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Transfer ownership to the requesting player so their NetworkTransform has authority.
            // The owner client drives position/rotation every frame via UpdateChannelObjectPosition(),
            // and NetworkTransform broadcasts those changes to all other clients.
            netObj.GiveOwnership(Owner);
            Debug.Log($"[NET] ServerRpcSpawnChannelObject: ownership given to {Owner}");

            // Send the spawned NetworkObject reference back to the owning client
            TargetRpcReceiveChannelObject(Owner, abilitySlot, netObj);
            Debug.Log($"[NET] ServerRpcSpawnChannelObject: channel object spawned and TargetRpc dispatched to owner");
        }
        else
        {
            // Prefab has no NetworkObject — the server-side ChannelAbility already has the reference
            // from SpawnChannelObjectOnServer(); no need to TargetRpc
            Debug.Log($"[NET] ServerRpcSpawnChannelObject: channel object has no NetworkObject, server-side setup only");
        }
    }

    /// <summary>
    /// TargetRpc sent to the owner client after the server has spawned the channel object.
    /// The owner client's ChannelAbility receives the spawned object reference so it can
    /// track position and run collider-based damage detection locally.
    /// </summary>
    [TargetRpc]
    private void TargetRpcReceiveChannelObject(NetworkConnection conn, int abilitySlot, NetworkObject channelNetObj)
    {
        Debug.Log($"[NET] TargetRpcReceiveChannelObject received on client - slot={abilitySlot}, obj={channelNetObj?.gameObject.name}");
        CharacterAbilityManager mgr = GetComponent<CharacterAbilityManager>();
        DataDrivenAbility ability = mgr?.GetDataDrivenAbilityAtSlot(abilitySlot);
        if (ability == null)
        {
            Debug.LogWarning($"[NET] TargetRpcReceiveChannelObject: no DataDrivenAbility at slot {abilitySlot}");
            return;
        }
        ability.ReceiveChannelObjectFromServer(channelNetObj?.gameObject);
    }

    // ===================== SYNCVAR SERVER-RPC SETTERS =====================
    // FishNet SyncVars are WritePermission.ServerOnly — only the server can write.
    // Clients call these ServerRpc methods to request the server to update the value.
    // The server writes the SyncVar, and FishNet propagates the change to all clients.

    [ServerRpc]
    private void ServerRpcSetCharacterIndex(int index)
    {
        _syncSelectedCharacterIndex.Value = index;
        Debug.Log($"[NET] Server set _syncSelectedCharacterIndex = {index} for {gameObject.name}");
    }

    [ServerRpc]
    private void ServerRpcSetCharacterName(string name)
    {
        _syncCharacterName.Value = name;
        Debug.Log($"[NET] Server set _syncCharacterName = '{name}' for {gameObject.name}");
    }

    [ServerRpc]
    private void ServerRpcSetClassName(string className)
    {
        _syncClassName.Value = className;
        Debug.Log($"[NET] Server set _syncClassName = '{className}' for {gameObject.name}");
    }

    [ServerRpc]
    private void ServerRpcSetAimAngle(float angle)
    {
        _syncAimAngle.Value = angle;
    }

    [ServerRpc]
    private void ServerRpcSetFacingLeft(bool facingLeft)
    {
        _syncIsFacingLeft.Value = facingLeft;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NETWORK WEAPON SPAWNING
    // Weapons are FishNet NetworkObjects so their position and animations sync
    // correctly across all clients. Only the server spawns them; an ObserversRpc
    // (BufferLast = true) configures visuals on every client including late joiners.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by a guest client (owner, non-host) to request weapon spawn.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void ServerRpcSpawnWeapon(string weaponConfigName)
    {
        WeaponItemDropsConfig WeaponItemDropsConfig = WeaponItemDropsConfig.DefaultInstance;
        if (WeaponItemDropsConfig == null)
        {
            Debug.LogError("[NET][PlayerController] ServerRpcSpawnWeapon: WeaponItemDropsConfig not found on server");
            return;
        }
        WeaponConfig weaponConfig = WeaponItemDropsConfig.GetWeaponConfigByName(weaponConfigName);
        if (weaponConfig == null)
        {
            Debug.LogError($"[NET][PlayerController] ServerRpcSpawnWeapon: config '{weaponConfigName}' not found on server");
            return;
        }
        SpawnWeaponPairOnServer(weaponConfig);
    }

    /// <summary>
    /// Server-side: instantiate + Spawn the main (and optional offhand) weapon,
    /// parent it to this player's NetworkObject, then tell all clients to set up visuals.
    /// </summary>
    private void SpawnWeaponPairOnServer(WeaponConfig mainHandConfig)
    {
        // Despawn previous weapons so stale objects don't linger
        if (_currentWeaponNOB != null)
        {
            InstanceFinder.ServerManager.Despawn(_currentWeaponNOB);
            _currentWeaponNOB = null;
        }
        if (_currentOffHandWeaponNOB != null)
        {
            InstanceFinder.ServerManager.Despawn(_currentOffHandWeaponNOB);
            _currentOffHandWeaponNOB = null;
        }

        WeaponSettings mainSettings = mainHandConfig.ToWeaponSettings();
        if (mainSettings.weaponPrefab == null)
        {
            Debug.LogWarning($"[NET][PlayerController] SpawnWeaponPairOnServer: no prefab for '{mainHandConfig.weaponName}'");
            return;
        }

        // Instantiate standalone (FishNet requires no parent at spawn time)
        GameObject mainWeapon = Instantiate(mainSettings.weaponPrefab);
        _currentWeaponNOB = mainWeapon.GetComponent<NetworkObject>();
        if (_currentWeaponNOB == null)
        {
            Debug.LogError($"[NET][PlayerController] Weapon prefab '{mainSettings.weaponPrefab.name}' has no NetworkObject! Add one to the prefab.");
            Destroy(mainWeapon);
            return;
        }

        InstanceFinder.ServerManager.Spawn(mainWeapon, Owner);
        // Parent to this player's NetworkObject — FishNet syncs this hierarchy change
        // to all clients including late joiners.
        _currentWeaponNOB.SetParent(this.NetworkObject);
        mainWeapon.transform.localPosition = Vector3.zero;
        mainWeapon.transform.localRotation = Quaternion.identity;

        // ── Offhand ──────────────────────────────────────────────────────────
        NetworkObject offHandNOB = null;
        WeaponConfig offHandConfig = null;

        // Skip offhand if main weapon is 2-handed
        if (mainHandConfig.is2Handed)
        {
            Debug.Log($"[NET][PlayerController] Main weapon '{mainHandConfig.weaponName}' is 2-handed, skipping offhand spawn");
        }
        else
        {
            offHandConfig = mainHandConfig.offhandWeaponConfig;
            if (offHandConfig == null && currentCharacterData != null
                && currentCharacterData.hasDualWeapons
                && currentCharacterData.offHandWeaponConfig != null)
            {
                offHandConfig = currentCharacterData.offHandWeaponConfig;
            }

            if (offHandConfig != null)
            {
                WeaponSettings offHandSettings = offHandConfig.ToOffhandWeaponSettings();
                if (offHandSettings.weaponPrefab != null)
                {
                    GameObject offHandWeapon = Instantiate(offHandSettings.weaponPrefab);
                    offHandNOB = offHandWeapon.GetComponent<NetworkObject>();
                    if (offHandNOB == null)
                    {
                        Debug.LogError($"[NET][PlayerController] OffHand prefab '{offHandSettings.weaponPrefab.name}' has no NetworkObject!");
                        Destroy(offHandWeapon);
                    }
                    else
                    {
                        _currentOffHandWeaponNOB = offHandNOB;
                        InstanceFinder.ServerManager.Spawn(offHandWeapon, Owner);
                        offHandNOB.SetParent(this.NetworkObject);
                        offHandWeapon.transform.localPosition = Vector3.zero;
                        offHandWeapon.transform.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        // Update server-side character data
        if (currentCharacterData != null)
            currentCharacterData.mainHandWeaponConfig = mainHandConfig;

        // Handle weapon-granted primary ability
        if (mainHandConfig.grantedPrimaryAbility != null)
        {
            CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
            if (currentCharacterData != null)
                currentCharacterData.abilityLoadout.SetWeaponAbility(mainHandConfig.grantedPrimaryAbility);
            if (abilityManager != null)
                abilityManager.SetWeaponAbility(mainHandConfig.grantedPrimaryAbility);

            // Handle offhand weapon's ability
            if (abilityManager != null && offHandConfig != null && offHandConfig.grantedPrimaryAbility != null)
            {
                abilityManager.SetOffhandAbility(offHandConfig.grantedPrimaryAbility, mainHandConfig, offHandConfig);
            }
            else if (abilityManager != null)
            {
                abilityManager.ClearOffhandAbility();
            }
        }

        // Tell all clients (including self via RunLocally) to configure visuals.
        // BufferLast = true means late-joining clients also run this automatically.
        ObserversRpcSetupWeaponVisuals(_currentWeaponNOB, offHandNOB);

        Debug.Log($"[NET][PlayerController] SpawnWeaponPairOnServer complete: '{mainHandConfig.weaponName}'");
    }

    /// <summary>
    /// Runs on ALL clients (including the server/host via RunLocally) to parent the
    /// FishNet-spawned weapon under the WeaponHolder child and configure visuals.
    /// BufferLast = true ensures newly connected clients receive the latest call.
    /// </summary>
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void ObserversRpcSetupWeaponVisuals(
        NetworkObject mainWeaponNOB,
        NetworkObject offHandWeaponNOB)
    {
        Debug.Log($"[NET] ObserversRpcSetupWeaponVisuals: main={(mainWeaponNOB != null ? mainWeaponNOB.name : "null")}, " +
                  $"offHand={(offHandWeaponNOB != null ? offHandWeaponNOB.name : "null")}");

        if (mainWeaponNOB != null)
        {
            WeaponHolder weaponHolder = GetComponent<WeaponHolder>();
            if (weaponHolder == null) weaponHolder = gameObject.AddComponent<WeaponHolder>();
            weaponHolder.SetupNetworkWeapon(mainWeaponNOB.gameObject);
            float mainHandRotOffset = currentCharacterData?.mainHandWeaponConfig?.handRotationOffset ?? 0f;
            weaponHolder.ApplyHandRotationOffset(mainHandRotOffset);
        }

        if (offHandWeaponNOB != null)
        {
            OffHandWeaponHolder offHandHolder = GetComponent<OffHandWeaponHolder>();
            if (offHandHolder == null) offHandHolder = gameObject.AddComponent<OffHandWeaponHolder>();
            offHandHolder.SetupNetworkWeapon(offHandWeaponNOB.gameObject);
            WeaponConfig offHandConfig = currentCharacterData?.mainHandWeaponConfig?.offhandWeaponConfig
                ?? (currentCharacterData?.hasDualWeapons == true ? currentCharacterData?.offHandWeaponConfig : null);
            float offHandRotOffset = offHandConfig?.handRotationOffset ?? 0f;
            offHandHolder.ApplyHandRotationOffset(offHandRotOffset);
        }

        // Rebind the player animator to pick up new weapon children
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    /// <summary>
    /// Tells all non-server clients to spawn a muzzle flash for this player.
    /// ExcludeServer=true because the server already spawned it locally via ProjectileSpawner.SpawnMuzzleFlash().
    /// Clients use DataDrivenAbility.SpawnMuzzleFlashLocally which reads the ScriptableObject muzzleFlashPrefab
    /// that is present on every machine, bypassing the null-ref in Projectile.RpcSpawnMuzzleFlash.
    /// </summary>
    [ObserversRpc]
    public void ObserversRpcSpawnMuzzleFlash(int abilitySlot, string abilityName, Vector3 position, float angle)
    {
        // The owner already spawned their muzzle flash immediately (zero-latency) in
        // DataDrivenAbility before the ServerRpc was sent.  Skip here to avoid a
        // duplicate flash appearing after the round-trip delay.
        if (IsOwner) return;

        CharacterAbilityManager mgr = GetComponent<CharacterAbilityManager>();
        if (mgr == null) return;
        DataDrivenAbility ability = mgr.FindDataDrivenAbility(abilitySlot, abilityName);
        ability?.SpawnMuzzleFlashLocally(position, angle);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VISUAL SNAPSHOT BROADCASTING (Client-Authoritative Visuals)
    // Each owner loads their CharacterData locally, then broadcasts a lightweight
    // PlayerVisualSnapshot (~200 bytes) to all observers via ObserversRpc.
    // Remote players use this snapshot for visual-only setup (gear sprites, animations).
    // Combat stats remain server-authoritative - server reads AllStats for damage calc.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build a visual snapshot from the current character data and broadcast it to all observers.
    /// Call this whenever equipped gear or character visuals change.
    /// </summary>
    private void BroadcastVisuals()
    {
        if (!IsOwner || (!IsServerStarted && !IsClientStarted)) return;
        if (currentCharacterData == null) return;

        var snapshot = BuildVisualSnapshot();

        if (IsServerStarted)
        {
            // Host: call ObserversRpc directly
            ObserversRpcSyncVisuals(snapshot);
        }
        else
        {
            // Guest client: route through ServerRpc so server can relay
            ServerRpcBroadcastVisuals(snapshot);
        }

        Debug.Log($"[ownership] Broadcast visual snapshot for '{currentCharacterData.characterName}' (obj={gameObject.name})");
    }

    /// <summary>
    /// Build a PlayerVisualSnapshot from the current character data.
    /// </summary>
    private PlayerVisualSnapshot BuildVisualSnapshot()
    {
        var snapshot = new PlayerVisualSnapshot
        {
            characterName = currentCharacterData?.characterName ?? "",
            className = currentCharacterData?.classData?.className ?? ""
        };

        // Extract weapon config names
        if (currentCharacterData?.equippedGear != null)
        {
            if (currentCharacterData.equippedGear.TryGetValue(GearSlot.Weapon, out var mainHandItem))
            {
                var wd = JsonUtility.FromJson<WeaponGearData>(mainHandItem?.additionalData);
                snapshot.weaponConfigName = wd?.weaponConfigName ?? "";
            }
            if (currentCharacterData.equippedGear.TryGetValue(GearSlot.OffHandWeapon, out var offhandItem))
            {
                var wd = JsonUtility.FromJson<WeaponGearData>(offhandItem?.additionalData);
                snapshot.offhandConfigName = wd?.weaponConfigName ?? "";
            }

            // Extract armor config names
            foreach (var kvp in currentCharacterData.equippedGear)
            {
                if (kvp.Value?.itemType?.ToLower() != "armor") continue;
                var ad = JsonUtility.FromJson<ArmorGearData>(kvp.Value.additionalData);
                if (ad == null) continue;

                switch (kvp.Key)
                {
                    case GearSlot.Head: snapshot.headArmorConfig = ad.armorConfigName; break;
                    case GearSlot.Chest: snapshot.chestArmorConfig = ad.armorConfigName; break;
                    case GearSlot.Hands: snapshot.handsArmorConfig = ad.armorConfigName; break;
                    case GearSlot.Feet: snapshot.feetArmorConfig = ad.armorConfigName; break;
                    case GearSlot.Backpack: snapshot.backpackArmorConfig = ad.armorConfigName; break;
                }
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Guest client routes visual snapshot through server.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void ServerRpcBroadcastVisuals(PlayerVisualSnapshot snapshot)
    {
        // Server relays to all observers
        ObserversRpcSyncVisuals(snapshot);
    }

    /// <summary>
    /// Broadcast visual snapshot to all observers. BufferLast ensures late joiners
    /// receive the most recent snapshot. RunLocally=false because owner already has visuals.
    /// </summary>
    [ObserversRpc(BufferLast = true, RunLocally = false)]
    private void ObserversRpcSyncVisuals(PlayerVisualSnapshot snapshot)
    {
        Debug.Log($"[NET] ObserversRpcSyncVisuals received for '{snapshot.characterName}' class='{snapshot.className}' (IsOwner={IsOwner}, obj={gameObject.name})");

        // Owner already has visuals set up locally
        if (IsOwner) return;

        // Apply visuals for remote player
        SetupCharacterVisualsOnly(snapshot);
    }

    /// <summary>
    /// Setup character visuals for a remote player using the lightweight snapshot.
    /// This does NOT load stats/traits/abilities - those are never needed on remote clients
    /// since combat is server-authoritative.
    /// </summary>
    private void SetupCharacterVisualsOnly(PlayerVisualSnapshot snapshot)
    {
        Debug.Log($"[NET] SetupCharacterVisualsOnly: character='{snapshot.characterName}', class='{snapshot.className}'");

        // Weapon visual (main hand) - routes through network spawn path
        if (!string.IsNullOrEmpty(snapshot.weaponConfigName))
        {
            var wd = new WeaponGearData { weaponConfigName = snapshot.weaponConfigName };
            var item = new ItemInstance("weapon", snapshot.weaponConfigName, 0);
            item.additionalData = JsonUtility.ToJson(wd);
            EquipWeaponVisual(item);
        }

        // Offhand weapon (if not blocked by 2-handed main)
        if (!string.IsNullOrEmpty(snapshot.offhandConfigName))
        {
            // 2-handed check happens inside EquipWeaponVisual
            var wd = new WeaponGearData { weaponConfigName = snapshot.offhandConfigName };
            var item = new ItemInstance("weapon", snapshot.offhandConfigName, 0);
            item.additionalData = JsonUtility.ToJson(wd);
            // Note: For offhand we'd need a separate equip path, but weapons are server-spawned
            // so the offhand spawn is handled by SpawnWeaponPairOnServer
        }

        // Armor visuals
        ApplyNetworkArmor(snapshot.headArmorConfig, GearSlot.Head);
        ApplyNetworkArmor(snapshot.chestArmorConfig, GearSlot.Chest);
        ApplyNetworkArmor(snapshot.handsArmorConfig, GearSlot.Hands);
        ApplyNetworkArmor(snapshot.feetArmorConfig, GearSlot.Feet);
        ApplyNetworkArmor(snapshot.backpackArmorConfig, GearSlot.Backpack);

        RefreshGearAnimators();
        Debug.Log($"[NET] Finished visual setup for remote player '{snapshot.characterName}'");
    }

    /// <summary>
    /// Apply network armor - delegates to PlayerGearManager.
    /// </summary>
    private void ApplyNetworkArmor(string configName, GearSlot slot)
    {
        if (string.IsNullOrEmpty(configName)) return;
        if (gearManager != null)
        {
            gearManager.EquipArmorByConfigName(configName, slot);
        }
    }

    /// <summary>
    /// Public API: equip a weapon by its WeaponConfig name.  Routes through the
    /// network-aware path (EquipWeaponVisual → SpawnWeaponPairOnServer) so the weapon
    /// prefab is properly Spawned as a NetworkObject visible to all clients.
    /// Call this from InventoryItemUI, GearItemUI, or any other runtime equip trigger
    /// instead of calling WeaponHolder.EquipWeapon() directly.
    /// </summary>
    public void NetworkEquipWeapon(string weaponConfigName)
    {
        if (string.IsNullOrEmpty(weaponConfigName))
        {
            Debug.LogError("[PlayerController] NetworkEquipWeapon: weaponConfigName is null/empty");
            return;
        }

        var wd = new WeaponGearData { weaponConfigName = weaponConfigName };
        var item = new ItemInstance("weapon", weaponConfigName, 0);
        item.additionalData = JsonUtility.ToJson(wd);
        EquipWeaponVisual(item);

        // Sync visual snapshot to all observers
        BroadcastVisuals();
    }

    /// <summary>
    /// Equip weapon visual. When FishNet is active this routes through the server-authoritative
    /// spawn path so the weapon is a proper NetworkObject visible to all clients.
    /// When FishNet is not yet active (Awake before OnStartNetwork) the old Instantiate
    /// path is used; that pre-network weapon is cleaned up by WeaponHolder.SetupNetworkWeapon
    /// when the real network spawn arrives shortly after.
    /// </summary>
    private void EquipWeaponVisual(ItemInstance weaponItem)
    {
        if (string.IsNullOrEmpty(weaponItem.additionalData))
        {
            Debug.LogError($"[PlayerController] Cannot equip weapon - no additionalData");
            return;
        }

        WeaponGearData weaponData = JsonUtility.FromJson<WeaponGearData>(weaponItem.additionalData);
        if (weaponData == null || string.IsNullOrEmpty(weaponData.weaponConfigName))
        {
            Debug.LogError($"[PlayerController] Cannot equip weapon - invalid WeaponGearData");
            return;
        }

        WeaponItemDropsConfig WeaponItemDropsConfig = WeaponItemDropsConfig.DefaultInstance;
        if (WeaponItemDropsConfig == null)
        {
            Debug.LogError($"[PlayerController] Cannot equip weapon - WeaponItemDropsConfig not found");
            return;
        }

        WeaponConfig weaponConfig = WeaponItemDropsConfig.GetWeaponConfigByName(weaponData.weaponConfigName);
        if (weaponConfig == null)
        {
            Debug.LogError($"[PlayerController] Weapon config '{weaponData.weaponConfigName}' not found");
            return;
        }

        bool isNetworkActive = IsServerStarted || IsClientStarted;

        // ── Remote non-owner (network active) ────────────────────────────────
        // The weapon arrives via FishNet spawn + ObserversRpcSetupWeaponVisuals.
        // Do NOT Instantiate locally — that would create an unspawned NetworkObject
        // which FishNet's observer system immediately disables.
        if (isNetworkActive && !IsOwner)
        {
            Debug.Log($"[PlayerController] Remote player '{gameObject.name}': weapon handled by network spawn, skipping local Instantiate");
            return;
        }

        // ── Owner with active network ─────────────────────────────────────────
        // Route through FishNet spawn so all clients see an active NetworkObject.
        if (isNetworkActive && IsOwner)
        {
            if (currentCharacterData != null)
                currentCharacterData.mainHandWeaponConfig = weaponConfig;

            // Determine which path to take for spawning:
            // - Host server context: call SpawnWeaponPairOnServer directly
            // - Guest client: call ServerRpc to request spawn from server
            // - Host client context: do nothing, server context will handle it
            if (base.IsServerInitialized)
            {
                SpawnWeaponPairOnServer(weaponConfig);           // host server context: direct call
            }
            else if (!IsServerStarted)
            {
                ServerRpcSpawnWeapon(weaponConfig.weaponName);  // pure guest client: via ServerRpc
            }
            // else: host client context - server side handles spawn, skip here to avoid duplicate

            // Ability loadout is owner-local and does not require a network round-trip
            if (weaponConfig.grantedPrimaryAbility != null)
            {
                CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
                if (currentCharacterData != null)
                    currentCharacterData.abilityLoadout.SetWeaponAbility(weaponConfig.grantedPrimaryAbility);
                if (abilityManager != null)
                {
                    abilityManager.SetWeaponAbility(weaponConfig.grantedPrimaryAbility);

                    // Skip offhand ability wiring if main weapon is 2-handed
                    if (weaponConfig.is2Handed)
                    {
                        abilityManager.ClearOffhandAbility();
                    }
                    else
                    {
                        // Wire offhand ability from offhand weapon config
                        WeaponConfig offHandCfg = weaponConfig.offhandWeaponConfig;
                        if (offHandCfg == null && currentCharacterData != null
                            && currentCharacterData.hasDualWeapons
                            && currentCharacterData.offHandWeaponConfig != null)
                            offHandCfg = currentCharacterData.offHandWeaponConfig;

                        if (offHandCfg != null && offHandCfg.grantedPrimaryAbility != null)
                            abilityManager.SetOffhandAbility(offHandCfg.grantedPrimaryAbility, weaponConfig, offHandCfg);
                        else
                            abilityManager.ClearOffhandAbility();
                    }
                }
            }

            Debug.Log($"[ownership] ✓ Network weapon equip requested: {weaponConfig.weaponName} for '{currentCharacterData?.characterName}' (obj={gameObject.name})");
            return;
        }

        // ── Pre-network / single-player (Awake before OnStartNetwork) ─────────
        // FishNet is not running yet. Use the old Instantiate path. The weapon will
        // be cleaned up by SetupNetworkWeapon() when the real network spawn arrives.
        if (currentCharacterData != null)
            currentCharacterData.mainHandWeaponConfig = weaponConfig;

        WeaponHolder weaponHolder = GetComponent<WeaponHolder>();
        if (weaponHolder == null) weaponHolder = gameObject.AddComponent<WeaponHolder>();

        WeaponSettings weaponSettings = weaponConfig.ToWeaponSettings();
        weaponHolder.EquipWeapon(weaponSettings.weaponPrefab, weaponSettings.animatorController);
        weaponHolder.ApplyHandRotationOffset(weaponSettings.handRotationOffset);

        if (weaponConfig.grantedPrimaryAbility != null)
        {
            CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
            if (currentCharacterData != null)
                currentCharacterData.abilityLoadout.SetWeaponAbility(weaponConfig.grantedPrimaryAbility);
            if (abilityManager != null)
                abilityManager.SetWeaponAbility(weaponConfig.grantedPrimaryAbility);
        }

        // Skip offhand if main weapon is 2-handed
        if (weaponConfig.is2Handed)
        {
            Debug.Log($"[PlayerController] Main weapon '{weaponConfig.weaponName}' is 2-handed, skipping offhand equip");
        }
        else
        {
            WeaponConfig offHandConfig = weaponConfig.offhandWeaponConfig;

            // Wire offhand ability in pre-network path
            if (offHandConfig != null && offHandConfig.grantedPrimaryAbility != null)
            {
                CharacterAbilityManager abilityManager = GetComponent<CharacterAbilityManager>();
                if (abilityManager != null)
                    abilityManager.SetOffhandAbility(offHandConfig.grantedPrimaryAbility, weaponConfig, offHandConfig);
            }

            if (offHandConfig != null)
            {
                WeaponSettings offHandSettings = offHandConfig.ToWeaponSettings();
                if (offHandSettings.weaponPrefab != null)
                {
                    OffHandWeaponHolder offHandHolder = GetComponent<OffHandWeaponHolder>();
                    if (offHandHolder == null) offHandHolder = gameObject.AddComponent<OffHandWeaponHolder>();
                    offHandHolder.EquipWeapon(offHandSettings.weaponPrefab, offHandSettings.animatorController);
                    offHandHolder.ApplyHandRotationOffset(offHandSettings.handRotationOffset);
                }
            }
        }

        Debug.Log($"[ownership] ✓ Equipped weapon (pre-network): {weaponConfig.weaponName} for '{currentCharacterData?.characterName}'");
    }

    /// <summary>
    /// Equip armor visual (same logic as InventoryItemUI.EquipArmorOnPlayer)
    /// </summary>
    /// <summary>
    /// Equip armor visual - delegates to PlayerGearManager.
    /// </summary>
    private void EquipArmorVisual(ItemInstance armorItem, GearSlot slotType)
    {
        if (gearManager == null)
        {
            Debug.LogError("[PlayerController] Cannot equip armor - PlayerGearManager not found");
            return;
        }
        gearManager.EquipArmorVisual(armorItem, slotType);
    }

    /// <summary>
    /// Load starter gear for all slots (used when character has no saved gear)
    /// </summary>
    private void LoadStarterGearNow()
    {
        if (currentCharacterData == null || currentCharacterData.classData == null)
        {
            Debug.LogError("[PlayerController] Cannot load starter gear - missing class data");
            return;
        }

        PlayerGearManager gearManager = GetComponent<PlayerGearManager>();
        if (gearManager != null)
        {
            gearManager.EquipStartingGear(currentCharacterData.classData);
            Debug.Log($"[PlayerController] Loaded starter gear for {currentCharacterData.classData.className}");
        }
    }

    /// <summary>
    /// Load starter gear for a specific slot - delegates to PlayerGearManager.
    /// </summary>
    private void LoadStarterGearForSlot(GearSlot slot)
    {
        if (currentCharacterData == null || currentCharacterData.classData == null) return;
        if (gearManager == null) return;
        gearManager.LoadStarterGearForSlot(slot, currentCharacterData.classData);
    }

    /// <summary>
    /// Load starter gear with a delay (for remote players)
    /// </summary>
    private System.Collections.IEnumerator LoadStarterGearDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentCharacterData != null && currentCharacterData.classData != null)
        {
            PlayerGearManager gearManager = GetComponent<PlayerGearManager>();
            if (gearManager != null)
            {
                gearManager.EquipStartingGear(currentCharacterData.classData);
                RefreshGearAnimators();
                Debug.Log($"[PlayerController] Loaded starter gear for remote player");
            }
        }
    }

    /// <summary>
    /// Generate starter gear ItemInstances and populate CharacterData.equippedGear.
    /// Called on death to reset gear inventory without doing visual equipping.
    /// Visual gear is handled by LoadSavedVisualGear when the character respawns.
    /// Delegates to PlayerGearManager.PopulateStarterGearItems.
    /// </summary>
    private void PopulateStarterGearItems(CharacterData characterData)
    {
        PlayerGearManager.PopulateStarterGearItems(characterData);
    }

    private System.Collections.IEnumerator LoadWeaponDelayed()
    {
        yield return null; // Wait one frame
        LoadWeapon();
    }

    /// <summary>
    /// Legacy weapon loader — now routes through the network-aware path so weapons
    /// are FishNet-Spawned instead of locally Instantiated.
    /// </summary>
    private void LoadWeapon()
    {
        if (currentCharacterData == null)
        {
            Debug.LogWarning("[PlayerController] Cannot load weapon: currentCharacterData is null");
            return;
        }

        if (currentCharacterData.mainHandWeaponConfig == null)
        {
            Debug.LogWarning("[PlayerController] Cannot load weapon: mainHandWeaponConfig is null");
            return;
        }

        // Route through the network-aware path
        NetworkEquipWeapon(currentCharacterData.mainHandWeaponConfig.weaponName);
    }

    private void SetupFootstepParticles()
    {
        // Clean up existing footstep particles
        if (footstepParticles != null)
        {
            Destroy(footstepParticles.gameObject);
            footstepParticles = null;
        }

        // Spawn new footstep particles if character has them
        if (currentCharacterData != null && currentCharacterData.GetFootstepSettings() != null && currentCharacterData.GetFootstepSettings().particlesPrefab != null)
        {
            Vector3 spawnPosition = transform.position + (Vector3)currentCharacterData.GetFootstepSettings().offset;
            footstepParticles = Instantiate(currentCharacterData.GetFootstepSettings().particlesPrefab, spawnPosition, Quaternion.identity, transform);
            footstepParticles.transform.localPosition = currentCharacterData.GetFootstepSettings().offset;
            footstepParticles.Stop(); // Ensure it starts stopped
        }
    }
    private void LoadCharacterAnimationsInternal()
    {
        animator.runtimeAnimatorController = currentCharacterData.GetAnimatorController();
    }

    private void LoadCharacterAbilitiesInternal()
    {
        // Reset all existing ability states BEFORE destroying them to prevent stuck movement/playerControl
        ResetAllAbilityStates();

        if (currentCharacterData != null && abilityManager != null)
        {
            abilityManager.LoadCharacterAbilities(currentCharacterData);
        }
        else
        {
            Debug.LogWarning("Cannot load abilities - missing character data or ability manager");
        }
    }

    private void ApplyCharacterStats()
    {
        if (currentCharacterData == null) return;

        // Initialize stat container from database first
        AllStats.InitializeFromDatabase();

        // Load from statContainer (which has conversions already applied)
        // baseStatContainer is ONLY used for recalculation when traits/stats change
        StatContainer sourceStats = currentCharacterData.statContainer;

        if (sourceStats != null)
        {
            var savedStats = sourceStats.GetAllStats();

            foreach (var stat in savedStats)
            {
                AllStats.SetStat(stat.statID, stat.currentValue);
            }

            moveSpeed = AllStats.GetStat("MoveSpeed");
        }
        else
        {
            Debug.LogError("[PlayerController] CharacterData has no stat containers!");
            return;
        }

        // Initialize to full health when spawning (e.g., after death)
        ModifyHealth(MaxHealth);
        ModifyEnergy(MaxEnergy);
    }

    /// <summary>
    /// Recalculate all stats by applying trait and gear modifiers to base values.
    /// Called on initialization and whenever traits or gear change.
    /// 
    /// Architecture:
    /// - baseStatContainer = immutable class defaults (never changes after creation)
    /// - statContainer = mutable current base (accumulates level bonuses)
    /// - AllStats = final calculated values (statContainer + conversions + traits + gear)
    /// 
    /// IMPORTANT: This method does NOT recalculate conversions - statContainer already has them from save or from level-up.
    /// Conversions are ONLY applied when:
    /// 1. Creating a new character (CharacterSelectionConfig.CreateCharacterFromClass)
    /// 2. Leveling up (LevelUpManager.ApplyLevelUpBonuses)
    /// </summary>
    /// <remarks>
    /// Calling this sets a dirty flag; the actual work runs once in LateUpdate via
    /// DoRecalculateStatsWithTraits(). This coalesces multiple same-frame triggers
    /// (level-up, gear change, trait change) into a single pass.
    /// </remarks>
    private void RecalculateStatsWithTraits()
    {
        _statsDirty = true;
    }

    /// <summary>
    /// Flushes the dirty flag set by RecalculateStatsWithTraits(). Called once per LateUpdate.
    /// </summary>
    private void LateUpdate()
    {
        if (!_statsDirty) return;
        _statsDirty = false;
        DoRecalculateStatsWithTraits();
    }

    /// <summary>
    /// The actual stat recalculation work – called only once per frame by LateUpdate when dirty.
    /// </summary>
    private void DoRecalculateStatsWithTraits()
    {
        Debug.Log($"[PlayerController] >>> DoRecalculateStatsWithTraits START (traitManager={traitManager != null}, gearManager={characterGearManager != null})");

        if (currentCharacterData == null || AllStats == null)
        {
            Debug.LogWarning("[PlayerController] Cannot recalculate stats - missing data");
            return;
        }

        // Ensure both the saved character container and the runtime AllStats have every stat
        // currently in StatTypeDatabase. This auto-heals characters created before a new stat
        // was added — no manual "reinitialize" step required.
        currentCharacterData.statContainer.MigrateFromDatabase();
        AllStats.MigrateFromDatabase();

        // Recovery guard: MigrateFromDatabase adds newly-tracked stats at value 0.
        // If critical resource stats ended up as 0 in statContainer (e.g. an old save
        // created before MaxEnergy was in the database), restore them from baseStatContainer
        // so Step 1 below doesn't copy 0 → AllStats, which would then clamp current
        // energy/health down to 0 via ClampCurrentResourcesToMax.
        if (currentCharacterData.baseStatContainer != null)
        {
            string[] criticalResourceStats = { "MaxHealth", "MaxEnergy", "MoveSpeed" };
            foreach (string resourceStat in criticalResourceStats)
            {
                if (currentCharacterData.statContainer.GetStat(resourceStat) == 0f)
                {
                    float baseValue = currentCharacterData.baseStatContainer.GetStat(resourceStat);
                    if (baseValue > 0f)
                    {
                        currentCharacterData.statContainer.SetStat(resourceStat, baseValue);
                        Debug.LogWarning($"[PlayerController] Recovered {resourceStat}={baseValue} in statContainer from baseStatContainer (was 0 — likely a newly-migrated stat).");
                    }
                }
            }
        }

        // Log trait modifier state before we start
        if (traitManager != null)
        {
            Debug.Log($"[PlayerController] Trait modifiers: MIND={traitManager.GetFlatModifier("MIND")}, ForceField={traitManager.GetFlatModifier("ForceField")}");
        }

        // STEP 1: Copy statContainer (already has conversions from save/level-up) to AllStats
        // DO NOT recalculate conversions here - that would double-apply them!
        var statsWithConversions = currentCharacterData.statContainer.GetAllStats();
        foreach (var stat in statsWithConversions)
        {
            AllStats.SetStat(stat.statID, stat.currentValue);
        }

        // STEP 2: Apply trait modifiers on top of converted stats (from statContainer)
        // STEP 3: Apply gear modifiers on top of trait-modified stats
        if (traitManager != null || characterGearManager != null)
        {
            int modifiedCount = 0;
            var allStats = AllStats.GetAllStats();

            foreach (var stat in allStats)
            {
                // Get base value (already includes conversions from save/level-up)
                float baseValue = stat.currentValue;

                // Aggregate modifiers from both traits and gear
                float totalFlat = 0f;
                float totalPercentage = 0f;

                // Add trait modifiers
                if (traitManager != null)
                {
                    totalFlat += traitManager.GetFlatModifier(stat.statID);
                    totalPercentage += traitManager.GetPercentageModifier(stat.statID);
                }

                // Add gear modifiers
                if (characterGearManager != null)
                {
                    float gearFlat = characterGearManager.GetFlatModifier(stat.statID);
                    float gearPercent = characterGearManager.GetPercentageModifier(stat.statID);

                    // Log specifically for MaxHealth to debug the issue
                    if (stat.statID == "MaxHealth" && (gearFlat != 0f || gearPercent != 0f))
                    {
                        Debug.Log($"[PlayerController] MaxHealth gear modifiers: flat={gearFlat}, percent={gearPercent}");
                    }

                    totalFlat += gearFlat;
                    totalPercentage += gearPercent;
                }

                // Calculate final value with combined modifiers
                // Formula: (base + flat) * (1 + percentage/100)
                // For percentage-based stats (AttackSpeed, etc.), flat values are divided by 100 first
                float finalValue;

                // Check if this is a percentage-based stat (same logic as CharacterTraitManager)
                bool isPercentageStat = IsPercentageStat(stat.statID);

                if (isPercentageStat)
                {
                    // Percentage stats: flat value represents percentage points (15 becomes 0.15)
                    finalValue = (baseValue + totalFlat / 100f) * (1f + totalPercentage / 100f);
                }
                else
                {
                    // Absolute stats: flat is added as-is
                    finalValue = (baseValue + totalFlat) * (1f + totalPercentage / 100f);
                }

                if (finalValue != baseValue)
                {
                    modifiedCount++;

                    // Always log MaxHealth for debugging
                    if (stat.statID == "MaxHealth")
                    {
                        Debug.Log($"[PlayerController] !!! MaxHealth MODIFIED !!! (isPercent={isPercentageStat}): base={baseValue}, flat=+{totalFlat}, percent=+{totalPercentage}%, final={finalValue}");
                    }
                    else
                    {
                        Debug.Log($"[PlayerController] {stat.statID} (isPercent={isPercentageStat}): base={baseValue}, flat=+{totalFlat}, percent=+{totalPercentage}%, final={finalValue}");
                    }
                }
                else if (stat.statID == "MaxHealth")
                {
                    // Log even if not modified, to see what's happening
                    Debug.Log($"[PlayerController] MaxHealth NOT modified: base={baseValue}, flat={totalFlat}, percent={totalPercentage}");
                }

                AllStats.SetStat(stat.statID, finalValue);
            }

        }
        else
        {
            Debug.LogWarning("[PlayerController] No trait or gear manager found - skipping modifiers");
        }

        // NOTE: We do NOT sync AllStats back to statContainer!
        // statContainer should only have base + conversions (no traits/gear)
        // Traits and gear are applied to AllStats at runtime only
        // When saving, we save statContainer as-is (without trait/gear modifiers)

        // Clamp current resource values to their new max values.
        // This handles both increases (fill up to new max) and decreases
        // (e.g. removing gear that added +MaxHealth shouldn't leave health above max).
        ClampCurrentResourcesToMax();

        // NOTE: We intentionally do NOT call SaveCharacter here.
        // RecalculateStatsWithTraits is a runtime recalculation of AllStats —
        // saving is the responsibility of the caller that triggered the change
        // (SpendTraitPoint, AddTraitPoints, ApplyLevelUpBonuses, etc.).
        // Saving here caused a pre-decrement overwrite: OnTraitsChanged fires
        // before SpendTraitPoint runs, so trait points were saved at the old
        // (pre-spend) value, stomping the correct value written moments later.

        // Note: HUD automatically updates via StatContainer.OnAnyStatChanged event
    }

    /// <summary>
    /// Called when traits change (trait unlocked/removed) to recalculate stats dynamically.
    /// </summary>
    private void OnTraitsChanged()
    {
        Debug.Log($"[PlayerController] OnTraitsChanged fired (event subscriber) — setting _statsDirty=true");
        RecalculateStatsWithTraits();
    }

    /// <summary>
    /// Public entry-point for external callers (e.g. CharacterTraitManager) that need to
    /// trigger a full stat recalculation.  Sets the same dirty flag used by the event path
    /// so the work is coalesced into a single LateUpdate pass.
    /// </summary>
    public void RequestStatsRecalculation()
    {
        Debug.Log($"[PlayerController] RequestStatsRecalculation called — setting _statsDirty=true");
        RecalculateStatsWithTraits();
    }

    /// <summary>
    /// Called when gear changes (equipped/unequipped) to recalculate stats dynamically.
    /// </summary>
    private void OnGearModifiersChanged()
    {
        Debug.Log("[PlayerController] ========== OnGearModifiersChanged CALLED ==========");
        RecalculateStatsWithTraits();
    }

    /// <summary>
    /// Clamp all current resource values (health, energy, force field) to their max.
    /// Prevents overflow when gear/traits that boosted a max value are removed.
    /// Also fills resources up when the max increases (e.g. equipping +MaxHealth gear).
    /// </summary>
    private void ClampCurrentResourcesToMax()
    {
        float maxHealth = MaxHealth;
        float maxEnergy = MaxEnergy;
        float maxForceField = MaxForceField;

        // Health: clamp down if over max, but don't reduce if already at/below max.
        // Guard: only clamp if maxHealth > 0 — a zero max means stats weren't ready yet
        // (e.g. a newly-migrated stat in statContainer) and we must not destroy the player's HP.
        if (maxHealth > 0f && CurrentHealth > maxHealth)
        {
            Debug.Log($"[PlayerController] Clamping health: {CurrentHealth} -> {maxHealth} (max reduced)");
            // Use ModifyHealth to go through the proper synced path
            ModifyHealth(-(CurrentHealth - maxHealth));
        }
        else if (maxHealth <= 0f && CurrentHealth > 0f)
        {
            Debug.LogWarning($"[PlayerController] MaxHealth is {maxHealth} — skipping health clamp to avoid zeroing HP. Check statContainer.");
        }

        // Energy: same logic with the same zero-max guard.
        if (maxEnergy > 0f && CurrentEnergy > maxEnergy)
        {
            Debug.Log($"[PlayerController] Clamping energy: {CurrentEnergy} -> {maxEnergy} (max reduced)");
            ModifyEnergy(-(CurrentEnergy - maxEnergy));
        }
        else if (maxEnergy <= 0f && CurrentEnergy > 0f)
        {
            Debug.LogWarning($"[PlayerController] MaxEnergy is {maxEnergy} — skipping energy clamp to avoid zeroing energy. Check statContainer.");
        }

        // Force field: clamp down if over, fill up if max increased
        if (CurrentForceField > maxForceField)
        {
            Debug.Log($"[PlayerController] Clamping force field: {CurrentForceField} -> {maxForceField} (max reduced)");
            ModifyForceField(-(CurrentForceField - maxForceField));
        }
        else if (CurrentForceField < maxForceField)
        {
            // Force field fills to max when max increases (same as old ReinitializeForceField)
            Debug.Log($"[PlayerController] Force field max increased, filling: {CurrentForceField} -> {maxForceField}");
            ModifyForceField(maxForceField - CurrentForceField);
        }
    }

    /// <summary>
    /// Determine if a stat uses percentage-based calculations (flat/100) or absolute values (flat as-is)
    /// Uses StatTypeDatabase as the source of truth for stat definitions
    /// </summary>
    private bool IsPercentageStat(string statID)
    {
        // Check StatTypeDatabase first - it's the source of truth
        var db = StatTypeDatabase.Instance;
        if (db != null)
        {
            var statType = db.GetStatType(statID);
            if (statType != null)
            {
                return statType.isPercentage;
            }
        }

        // Fallback for undefined stats
        string lowerID = statID.ToLower();
        return lowerID.Contains("speed") ||
               lowerID.Contains("crit") ||
               lowerID.Contains("dodge") ||
               lowerID.Contains("block") ||
               lowerID.Contains("lifesteal") ||
               lowerID.Contains("resistance") ||
               lowerID.Contains("damagebonus") ||
               lowerID.Contains("reduction") ||
               lowerID.Contains("chance") ||
               lowerID.Contains("rate") ||
               lowerID.Contains("regen") ||
               lowerID.Contains("distance");
    }

    /// <summary>
    /// Wait for traits to load from CharacterData, then recalculate stats.
    /// This ensures trait modifiers are applied after scene load.
    /// </summary>
    private System.Collections.IEnumerator RecalculateStatsAfterTraitLoad()
    {
        // Fallback recalculation in case OnTraitsChanged doesn't fire after the
        // trait-load coroutine completes. The load coroutine now only yields one
        // frame (yield return null), so 3 frames is more than enough.
        yield return null;
        yield return null;
        yield return null;


        if (traitManager != null && currentCharacterData != null)
        {
            var activeTraits = traitManager.GetActiveTraits();
            var unlockedNodes = traitManager.GetUnlockedNodeIDs();

            if (activeTraits.Count > 0)
            {
                RecalculateStatsWithTraits();
            }
            else
            {
                Debug.LogWarning("[PlayerController] No traits loaded - character may not have any traits unlocked");
            }
        }
    }

    /// <summary>
    /// Subscribe to stat changes on the statContainer to trigger recalculation.
    /// When stats change (e.g., level up, gear change), we need to recalculate derived stats.
    /// </summary>
    private void SubscribeToStatChanges()
    {
        if (currentCharacterData?.statContainer != null)
        {
            // Listen to statContainer changes (not baseStatContainer - that's immutable)
            // When someone modifies a stat (like during level up), recalculate everything
            // we automatically recalculate conversions and traits
            Debug.Log("[PlayerController] Subscribed to base stat changes for automatic recalculation");
        }
    }

    /// <summary>
    /// Called when a base stat is changed (e.g., stat point allocation, level up).
    /// This triggers a full recalculation from baseStatContainer.
    /// </summary>
    public void OnBaseStatChanged(string statID, float newValue)
    {
        RecalculateStatsWithTraits();
    }

    private void InitializeInputSystem()
    {

        bool isNetworkActive = IsServerStarted || IsClientStarted;
        bool shouldInitializeInput = !isNetworkActive || IsOwner;


        if (!shouldInitializeInput)
        {
            return;
        }

        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();

            // Load the InputActionAsset if not assigned
            if (playerInput.actions == null)
            {
#if UNITY_EDITOR
                var inputAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    "Assets/Scripts/PlayerController/PlayerInputActions.inputactions");

                if (inputAsset != null)
                {
                    playerInput.actions = inputAsset;
                    Debug.Log("PlayerInput actions assigned from asset");
                }
                else
                {
                    Debug.LogError("Could not find PlayerInputActions asset at Assets/Scripts/PlayerController/PlayerInputActions.inputactions");
                }
#else
                Debug.LogError("PlayerInput component missing actions asset! Ensure PlayerInput is properly configured in the prefab.");
#endif
            }
        }

        // IMPORTANT: Set behavior to avoid automatic method discovery
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        SetupInputActions();
    }

    protected override void Start()
    {
        base.Start();

        if (!inputSystemReady)
        {
            InitializeInputSystem();
        }
    }

    private void SetupInputActions()
    {
        if (playerInput?.actions == null)
        {
            Debug.LogError("PlayerInput actions is null!");
            return;
        }

        // Setup Move action
        moveAction = playerInput.actions.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.Enable();
        }
        else
        {
            Debug.LogError("Move action not found!");
        }

        // Setup WeaponAbility action (Left Mouse Button)
        weaponAbilityAction = playerInput.actions.FindAction("WeaponAbility");
        if (weaponAbilityAction != null)
        {
            weaponAbilityAction.performed += OnWeaponAbilityPerformed;
            weaponAbilityAction.Enable();
        }
        else
        {
            Debug.LogWarning("WeaponAbility action not found in Input Actions!");
        }

        // Setup DashAbility action (Shift key)
        dashAbilityAction = playerInput.actions.FindAction("DashAbility");
        if (dashAbilityAction != null)
        {
            dashAbilityAction.performed += OnDashAbilityPerformed;
            dashAbilityAction.Enable();
        }
        else
        {
            Debug.LogWarning("DashAbility action not found in Input Actions!");
        }

        inputSystemReady = true;
    }

    private void OnDestroy()
    {
        // Clear cached local player reference to prevent stale references after destruction
        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }

        // Clean up pet to prevent duplication during arena transitions
        if (currentPet != null)
        {
            Destroy(currentPet.gameObject);
            currentPet = null;
        }

        // Unsubscribe from trait changes
        if (traitManager != null)
        {
            traitManager.OnTraitsChanged -= OnTraitsChanged;
        }

        // Unsubscribe to prevent memory leaks
        if (weaponAbilityAction != null)
            weaponAbilityAction.performed -= OnWeaponAbilityPerformed;
        if (dashAbilityAction != null)
            dashAbilityAction.performed -= OnDashAbilityPerformed;

        if (gearManager != null)
        {
            gearManager.OnCoreGearReadyChanged -= SetGearAnimationReady;
        }
    }

    protected override void HandleUpdate()
    {
        // Non-owner remote instances: skip input/movement (synced via NetworkTransform)
        // but still update weapon aim from synced angle
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        if (isNetworkActive && !IsOwner)
        {
            // Clear any residual velocity from physics collisions each frame.
            // HandleMovement() never runs for non-owners; without this the rb retains
            // collision impulses and the character slides indefinitely.
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Detect movement by position delta (non-owners never have input, so movement==zero).
            // NetworkTransform updates the position each tick; we compare to previous frame.
            float posDelta = Vector3.Distance(transform.position, _prevRemotePosition);
            _remoteIsMoving = posDelta > 0.005f;
            _prevRemotePosition = transform.position;

            // Drive animations, weapon sorting, character flip, and weapon positions for remote players
            // through the same UpdateMovementAnimation() path that owners use. The non-owner branch
            // inside UpdateMovementAnimation reads _syncAimAngle and applies everything correctly.
            // IMPORTANT: Do NOT call PlayAnimation() directly here — it calls animator.Play(name,0,0f)
            // which resets normalizedTime to 0 every frame, causing the animation to freeze at frame 0.
            // Throttle remote player updates to match owner update rate
            if (Time.time >= lastRotationUpdateTime + rotationUpdateInterval)
            {
                lastRotationUpdateTime = Time.time;
                UpdateMovementAnimation();
            }
            return;
        }

        if (!inputSystemReady) return;

        HandleInput();
        HandleMovement();
        UpdateFootstepParticles();
    }

    private void HandleInput()
    {
        // Check if this player should process input
        // In single-player (no network active), always true
        // In multiplayer, only the owner processes input
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        bool shouldProcessInput = !isNetworkActive || IsOwner;

        if (!shouldProcessInput)
        {
            movement = Vector2.zero;
            return;
        }

        // Check if input is globally disabled (e.g., during loading)
        if (!InputEnabled)
        {
            movement = Vector2.zero;
            return;
        }

        if (moveAction != null)
        {
            movement = moveAction.ReadValue<Vector2>();
        }

        if (IsMovementBlockedByEffects())
        {
            movement = Vector2.zero;
        }
        //right click for offhand Ability

        // Right click: activate offhand ability
        if (abilityManager != null && InputHelper.GetOffhandAbility)
        {
            //abilityManager.ActivateOffhandAbility();
        }
        // CTRL toggle: switch between primary and offhand ability
        if (abilityManager != null)
        {
            bool ctrlHeld = InputHelper.GetCrouch;
            abilityManager.SetOffhandToggle(ctrlHeld);
        }
    }

    private void HandleMovement()
    {
        if (!inputSystemReady || rb == null) return;

        // Check if any movement ability is actively executing (e.g., dash)
        // If so, let the ability control velocity — don't interfere
        if (IsAnyMovementAbilityExecuting())
        {
            // Animations still need updating during ability movement
            if (Time.time >= lastRotationUpdateTime + rotationUpdateInterval)
            {
                lastRotationUpdateTime = Time.time;
                UpdateMovementAnimation();
            }
            return;
        }

        // Check if any ability is blocking player input (but not actively moving)
        bool isBlocked = IsAnyAbilityBlockingMovement();

        if (IsMovementBlockedByEffects())
        {
            rb.linearVelocity = Vector2.zero;
            wasMovementBlockedLastFrame = true;
            return;
        }

        if (isBlocked)
        {
            // Zero out velocity while blocked (ability is blocking input, not controlling movement)
            rb.linearVelocity = Vector2.zero;
            wasMovementBlockedLastFrame = true;
            return;
        }
        wasMovementBlockedLastFrame = false;
        // Normal movement when no ability is controlling
        Vector2 normalizedMovement = movement.normalized;
        float slowMultiplier = GetMovementSpeedMultiplierFromEffects();
        Vector2 moveVelocity = normalizedMovement * MoveSpeed * slowMultiplier;
        rb.linearVelocity = moveVelocity;

        // Throttle animation/rotation updates to reduce CPU load (every 0.1s instead of every frame)
        if (Time.time >= lastRotationUpdateTime + rotationUpdateInterval)
        {
            lastRotationUpdateTime = Time.time;
            UpdateMovementAnimation();
        }
    }

    /// <summary>
    /// Returns true if any DataDrivenAbility has a MovementAbility that is actively executing.
    /// Used by HandleMovement to avoid interfering with ability-driven movement (e.g., dash).
    /// </summary>
    private bool IsAnyMovementAbilityExecuting()
    {
        var abilities = GetComponents<DataDrivenAbility>();
        foreach (var ability in abilities)
        {
            if (ability != null && ability.IsMovementAbilityExecuting)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsMovementBlockedByEffects()
    {
        return effectManager != null && effectManager.HasAnyMovementBlockingEffect();
    }

    private float GetMovementSpeedMultiplierFromEffects()
    {
        if (effectManager == null) return 1f;
        return effectManager.GetMovementSpeedMultiplier();
    }

    
    private void UpdateMovementAnimation()
    {
        if (!isGearAnimationReady)
        {
            return;
        }

        if (currentCharacterData == null)
        {
            bool isNetworkActive = IsServerStarted || IsClientStarted;
            bool shouldWarn = !isNetworkActive || IsOwner;
            if (shouldWarn && !_loggedMissingCharacterData)
            {
                _loggedMissingCharacterData = true;
                Debug.LogWarning("[PlayerController] UpdateMovementAnimation: currentCharacterData is null!");
            }
            return;
        }

        _loggedMissingCharacterData = false;

        if (animator == null)
        {
            Debug.LogWarning("[PlayerController] UpdateMovementAnimation: animator is null!");
            return;
        }

        Transform mainHand = transform.Find("WeaponHolder/Weapon");
        Transform offHand = transform.Find("OffHandWeaponHolder/OffHandWeapon");

        // Skip character animation updates if player doesn't have control
        if (!IsAnyAbilityBlockingMovement())
        {
            // Only process aiming for the local player (owner)
            bool isNetworkActive = IsServerStarted || IsClientStarted;
            bool shouldProcessAiming = !isNetworkActive || IsOwner;

            if (!shouldProcessAiming)
            {
                // Remote players: smoothly interpolate toward the synced aim angle.
                // We maintain our own tracked float (_smoothedRemoteAimAngle) instead of
                // reading back eulerAngles from the weapon transform, which avoids the
                // rotation accumulation bugs that occurred with LerpAngle smoothing.
                float targetRemoteAngle = _syncAimAngle.Value;

                if (!_smoothedRemoteAngleInitialized)
                {
                    _smoothedRemoteAimAngle = targetRemoteAngle;
                    _smoothedRemoteAngleInitialized = true;
                }
                else
                {
                    _smoothedRemoteAimAngle = Mathf.LerpAngle(_smoothedRemoteAimAngle, targetRemoteAngle, Time.deltaTime * 18f);
                }

                float remoteAngle = _smoothedRemoteAimAngle;
                UpdateFlashlightRotation(remoteAngle);

                WeaponSortingManager.Direction aimDirection = GetAimDirectionEnum(remoteAngle);
                string targetAnimation = GetAnimationForAimDirection(aimDirection);

                if (animator != null)
                {
                    AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    string actuallyPlaying = clipInfo.Length > 0 ? clipInfo[0].clip.name : "";

                    if (targetAnimation != actuallyPlaying)
                    {
                        PlayAnimation(targetAnimation);
                    }
                }

                // Update remote player weapon positioning based on synced angle
                WeaponSettings mainHandSettings = null;
                WeaponSettings offHandSettings = null;

                if (mainHand != null)
                {
                    mainHandSettings = currentCharacterData.mainHandWeaponConfig?.ToWeaponSettings();
                    if (mainHandSettings != null)
                    {
                        mainHandSortingManager.UpdateActiveAimingWeapon(
                            mainHand, mainHandSettings, "Weapon", aimDirection, transform, mainCamera, spriteRenderer,
                            flipSpriteOnMove, backpackHolder,
                            () => _syncIsFacingLeft.Value, (val) => { }, (val) => ServerRpcSetFacingLeft(val),
                            isNetworkActive, IsOwner, remoteAngle);
                    }

                    if (offHand != null)
                    {
                        WeaponConfig offHandConfig = currentCharacterData.mainHandWeaponConfig?.offhandWeaponConfig;
                        if (offHandConfig == null && currentCharacterData.hasDualWeapons)
                        {
                            offHandConfig = currentCharacterData.offHandWeaponConfig;
                        }

                        if (offHandConfig != null)
                        {
                            offHandSettings = offHandConfig.ToWeaponSettings();
                            if (offHandSettings != null)
                            {
                                offHandSortingManager.UpdateActiveAimingWeapon(
                                    offHand, offHandSettings, "OffHandWeapon", aimDirection, transform, mainCamera, spriteRenderer,
                                    flipSpriteOnMove, backpackHolder,
                                    () => _syncIsFacingLeft.Value, (val) => { }, (val) => ServerRpcSetFacingLeft(val),
                                    isNetworkActive, IsOwner, remoteAngle);
                            }
                        }
                    }

                    // Apply dual-wield sorting adjustment based on facing direction
                    if (offHand != null && mainHandSortingManager != null && mainHandSettings != null && offHandSettings != null)
                    {
                        mainHandSortingManager.ApplyDualWieldSorting(mainHand, offHand, _syncIsFacingLeft.Value, aimDirection, mainHandSettings, offHandSettings);
                    }
                }
                return;
            }

            // LOCAL PLAYER: Update CHARACTER animations based on aim direction FIRST
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null && Mouse.current != null)
            {
                // Calculate aim direction from player center (NOT weapon position to avoid feedback loop)
                Vector3 aimOrigin = transform.position;
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mouseWorldPos.z = 0;
                float characterAngle = GetStableLocalAimAngle(aimOrigin, mouseWorldPos);
                UpdateFlashlightRotation(characterAngle); // Flashlight is offset by 90 degrees
                Vector2 aimDir = new Vector2(
                    Mathf.Cos(characterAngle * Mathf.Deg2Rad),
                    Mathf.Sin(characterAngle * Mathf.Deg2Rad));

                // Sync float angle for smooth remote rotation (no integer rounding)
                if (isNetworkActive && IsOwner)
                {
                    ServerRpcSetAimAngle(characterAngle);
                }

                // Get direction enum (single source of truth for both weapon and character)
                WeaponSortingManager.Direction currentAimDirection = GetAimDirectionEnum(characterAngle);
                string targetAnimation = GetAnimationForAimDirection(currentAimDirection);

                // Get what animator is ACTUALLY playing
                string actuallyPlaying = "";
                if (animator != null)
                {
                    AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    if (clipInfo.Length > 0)
                    {
                        actuallyPlaying = clipInfo[0].clip.name;
                    }
                }

                // Check what's ACTUALLY playing, not what we think is playing
                if (targetAnimation != actuallyPlaying)
                {
                    PlayAnimation(targetAnimation);
                }

                // NOW update weapon sorting/positioning with the CURRENT direction
                WeaponSettings mainHandSettings = null;
                WeaponSettings offHandSettings = null;

                if (mainHand != null)
                {
                    mainHandSettings = currentCharacterData.mainHandWeaponConfig?.ToWeaponSettings();
                    if (mainHandSettings != null)
                    {
                        mainHandSortingManager.UpdateActiveAimingWeapon(
                            mainHand, mainHandSettings, "Weapon", currentAimDirection, transform, mainCamera, spriteRenderer,
                            flipSpriteOnMove, backpackHolder,
                            () => isFacingLeft, (val) => isFacingLeft = val, (val) => ServerRpcSetFacingLeft(val),
                            isNetworkActive, IsOwner);
                    }

                    // Update off-hand weapon if dual-wielding
                    if (offHand != null)
                    {
                        // Check weapon-level offhand first, then character-level
                        WeaponConfig offHandConfig = currentCharacterData.mainHandWeaponConfig?.offhandWeaponConfig;
                        if (offHandConfig == null && currentCharacterData.hasDualWeapons)
                        {
                            offHandConfig = currentCharacterData.offHandWeaponConfig;
                        }

                        if (offHandConfig != null)
                        {
                            offHandSettings = offHandConfig.ToOffhandWeaponSettings();
                            if (offHandSettings != null)
                            {
                                offHandSortingManager.UpdateActiveAimingWeapon(
                                    offHand, offHandSettings, "OffHandWeapon", currentAimDirection, transform, mainCamera, spriteRenderer,
                                    flipSpriteOnMove, backpackHolder,
                                    () => isFacingLeft, (val) => isFacingLeft = val, (val) => ServerRpcSetFacingLeft(val),
                                    isNetworkActive, IsOwner);
                            }
                        }
                    }

                    // Apply dual-wield sorting adjustment based on facing direction
                    if (offHand != null && mainHandSortingManager != null && mainHandSettings != null && offHandSettings != null)
                    {
                        mainHandSortingManager.ApplyDualWieldSorting(mainHand, offHand, isFacingLeft, currentAimDirection, mainHandSettings, offHandSettings);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Force an immediate animation update - used when abilities end and control returns
    /// This bypasses the throttle timer for immediate visual feedback
    /// </summary>
    public void ForceAnimationUpdate()
    {
        lastRotationUpdateTime = Time.time; // Update timer to prevent double-update on next frame
        UpdateMovementAnimation();
    }

    /// <summary>
    /// Reset movement/rotation state after an arena transition.
    /// Called by ArenaManager.ReenablePlayerComponentsInternal after re-enabling
    /// components to prevent stale velocity (running-left bug) and stale aim angle
    /// (weapon rotation frozen until first input).
    /// </summary>
    public void ResetAfterArenaTransition()
    {
        // Zero physics velocity so the player doesn't shoot off in a stale direction
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Clear local movement input
        movement = Vector2.zero;

        // Reset all ability states to prevent stuck movement abilities / playerControl flags
        ResetAllAbilityStates();

        // Force remote-angle smoothing to re-snap on next frame (avoids stale rotation)
        _smoothedRemoteAngleInitialized = false;

        // Store current position so remote-movement detection doesn't see a huge delta
        _prevRemotePosition = transform.position;
        _remoteIsMoving = false;

        // Re-initialize sorting managers so weapon sorting layers are correct
        if (spriteRenderer != null && currentCharacterData != null)
        {
            mainHandSortingManager?.Initialize(spriteRenderer, currentCharacterData);
            offHandSortingManager?.Initialize(spriteRenderer, currentCharacterData);
        }

        // Force one animation + sorting update so the player renders correctly
        ForceAnimationUpdate();

        Debug.Log($"[PlayerController] ResetAfterArenaTransition complete for {gameObject.name} (IsOwner={IsOwner})");
    }

    /// <summary>
    /// Force-reset all DataDrivenAbility states on this player.
    /// Called during arena transitions, scene loads, or any flow that might interrupt abilities mid-execution.
    /// </summary>
    public void ResetAllAbilityStates()
    {
        var abilities = GetComponents<DataDrivenAbility>();
        foreach (var ability in abilities)
        {
            if (ability != null && ability.enabled)
            {
                ability.ForceResetAbilityState();
            }
        }
        Debug.Log($"[PlayerController] ResetAllAbilityStates: reset {abilities.Length} abilities on {gameObject.name}");
    }

    /// <summary>
    /// Update weapon position, rotation, flipping, and sorting for the given aim direction.
    /// Owner: calculates angle from mouse / ability lock.
    /// Remote: pass overrideAngle (from synced _syncAimAngle) to skip mouse/ability-lock lookups.
    /// </summary>
    private WeaponSortingManager.Direction GetAimDirectionEnum(float angle)
    {
        if (angle < 0) angle += 360;
        if (angle >= 22.5f && angle < 90f)
            return WeaponSortingManager.Direction.NorthEast;
        else if (angle >= 90f && angle < 156.5f)
            return WeaponSortingManager.Direction.NorthWest;
        else if (angle >= 156.5f && angle < 270f)
            return WeaponSortingManager.Direction.SouthWest;
        else
            return WeaponSortingManager.Direction.SouthEast;
    }

    private string GetAnimationForAimDirection(WeaponSortingManager.Direction aimDirection)
    {
        bool isMoving = IsMoving();

        // Map 4 directions to 2 animation zones (up vs down)
        switch (aimDirection)
        {
            case WeaponSortingManager.Direction.NorthEast:
            case WeaponSortingManager.Direction.NorthWest:
                // Up/north animations
                return isMoving ? currentCharacterData.GetRunUpAnimation() : currentCharacterData.GetIdleUpAnimation();

            case WeaponSortingManager.Direction.SouthEast:
            case WeaponSortingManager.Direction.SouthWest:
            default:
                // Down/south animations  
                return isMoving ? currentCharacterData.GetRunAnimation() : currentCharacterData.GetIdleAnimation();
        }
    }
    public void PlayAnimation(string animationName, float normalizedTime = 0f)
    {
        if (string.IsNullOrEmpty(animationName)) return;
        if (!isGearAnimationReady) return;
        // Drive the player animator through NetworkAnimator.Play() when on the network so the state
        // is immediately queued for broadcast rather than relying on the polling interval, which can
        // miss rapid state transitions fired via Animator.Play() directly.
        if (animator != null)
        {
            animator.speed = 1f; // ensure speed is not left dirty from a weapon animation call
            if (networkAnimator != null && (IsServerStarted || IsClientStarted) && IsOwner)
                networkAnimator.Play(animationName);
            else
                animator.Play(animationName, 0, normalizedTime);
        }
        // Play animation on all gear pieces (these are local-only child animators, not networked)
        if (gearManager != null)
        {
            Animator[] gearAnimators = gearManager.GetAllGearAnimators();
            foreach (Animator gearAnimator in gearAnimators)
            {
                // Avoid double-playing on the same animator
                if (gearAnimator != null && gearAnimator != animator)
                {
                    gearAnimator.Play(animationName, 0, normalizedTime);
                }
            }
        }
        currentAnimationPlaying = animationName;
    }

    public bool HasAnimation(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsAnyAbilityPerforming()
    {
        Ability[] abilities = GetComponents<Ability>();
        foreach (Ability ability in abilities)
        {
            DataDrivenAbility dataDriven = ability as DataDrivenAbility;
            if (dataDriven != null && dataDriven.IsPerformingAbility)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsAnyAbilityBlockingMovement()
    {
        Ability[] abilities = GetComponents<Ability>();
        foreach (Ability ability in abilities)
        {
            DataDrivenAbility dataDriven = ability as DataDrivenAbility;
            if (dataDriven != null && !dataDriven.HasPlayerControl)
            {
                return true;
            }
        }
        return false;
    }

    // These are the actual callback methods that get invoked by InputAction events
    private void OnWeaponAbilityPerformed(InputAction.CallbackContext context)
    {
        // Only process abilities for owner (or in single-player)
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        if (isNetworkActive && !IsOwner) return;

        if (!InputEnabled) return;

        // Trait roller consumes left-click selection input.
        if (TraitRollerUI.IsSessionActive) return;

        // Don't use abilities when in UI mode (inventory/menus open)
        if (CursorManager.Instance != null && CursorManager.Instance.IsInUIMode) return;

        // Ignore world attacks when pointer is currently over UI.
        // Avoid EventSystem.IsPointerOverGameObject() here because this callback can run
        // during input event processing where Unity reports stale last-frame UI state.
        if (IsPointerCurrentlyOverUI()) return;

        if (abilityManager != null)
        {
            // Use the active weapon ability (respects offhand toggle & alternation)
            abilityManager.GetActiveWeaponAbility()?.TryUseAbility();
        }
    }

    private static bool IsPointerCurrentlyOverUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        Pointer pointer = Pointer.current;
        if (pointer == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = pointer.position.ReadValue()
        };

        _uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }

    private void OnDashAbilityPerformed(InputAction.CallbackContext context)
    {
        // Only process abilities for owner (or in single-player)
        bool isNetworkActive = IsServerStarted || IsClientStarted;
        if (isNetworkActive && !IsOwner) return;

        if (!InputEnabled) return;

        if (abilityManager != null)
        {
            abilityManager.GetDashAbility()?.TryUseAbility();
        }
    }

    public event Action<AbilityDataConfig> OnAttack;
    public event Action<AbilityDataConfig, GameObject, float, string> OnAttackDamage;

    public void NotifyAttack(AbilityDataConfig abilityConfig)
    {
        if (abilityConfig == null || !abilityConfig.isAttack)
            return;

        OnAttack?.Invoke(abilityConfig);
    }

    public void NotifyAttackDamage(AbilityDataConfig abilityConfig, GameObject target, float damage, string damageType)
    {
        if (abilityConfig == null || !abilityConfig.isAttack)
            return;

        if (damage <= 0f)
            return;

        OnAttackDamage?.Invoke(abilityConfig, target, damage, damageType);
    }

    public Vector2 GetMovementDirection() => movement.normalized;

    protected override void HandleDeath()
    {
        Debug.Log($"[DEATH-DIAG] [HandleDeath] START — player={gameObject.name}, characterData={(currentCharacterData != null ? currentCharacterData.displayName : "NULL")}");
        Debug.Log($"[DEATH-DIAG] [HandleDeath] IsServerStarted={IsServerStarted}, IsClientStarted={IsClientStarted}, IsOwner={IsOwner}");

        // Clear all run-specific traits before saving so they don't persist
        if (traitManager != null)
        {
            traitManager.ResetAllTraits();
            Debug.Log("[DEATH-DIAG] [HandleDeath] Cleared all run traits");
        }

        // Disable input immediately
        SetInputEnabled(false);
        Debug.Log("[DEATH-DIAG] [HandleDeath] Input disabled");

        // Branch: single-player vs multiplayer
        bool isMultiplayer = IsServerStarted || IsClientStarted;
        Debug.Log($"[DEATH-DIAG] [HandleDeath] isMultiplayer={isMultiplayer}");
        if (!isMultiplayer)
        {
            // Notify sequencer/UI so they can show VFX + end screen.
            // The end screen's "Return to Command" button calls ExecuteReturnToCommandScene().
            // If nothing is listening (no sequencer present), fall back to the direct coroutine.
            int listenerCount = OnLocalPlayerDeath != null ? OnLocalPlayerDeath.GetInvocationList().Length : 0;
            Debug.Log($"[DEATH-DIAG] [HandleDeath] Firing OnLocalPlayerDeath — subscriber count: {listenerCount}");
            if (OnLocalPlayerDeath != null)
            {
                OnLocalPlayerDeath.Invoke();
                Debug.Log("[DEATH-DIAG] [HandleDeath] OnLocalPlayerDeath.Invoke() completed");
            }
            else
            {
                Debug.LogWarning("[DEATH-DIAG] [HandleDeath] No OnLocalPlayerDeath listeners found — PlayerDeathSequencer is likely missing or not enabled. Falling back to direct scene transition.");
                StartCoroutine(ReturnToCommandSceneOnDeath(skipDelay: false, keepAcquiredInventoryAndGear: false));
            }
        }
        else
        {
            // Multiplayer: disable this player's visuals/collision but keep the
            // NetworkObject alive so the session continues for other players.
            Debug.Log("[DEATH-DIAG] [HandleDeath] Multiplayer mode — calling DisableDeadPlayer");
            DisableDeadPlayer();

            // Fire death event for the local owner so the sequencer (VFX + end screen)
            // runs regardless of whether FishNet is active.
            if (IsOwner)
            {
                int listenerCount2 = OnLocalPlayerDeath != null ? OnLocalPlayerDeath.GetInvocationList().Length : 0;
                Debug.Log($"[DEATH-DIAG] [HandleDeath] Multiplayer owner — firing OnLocalPlayerDeath (subscribers={listenerCount2})");
                if (OnLocalPlayerDeath != null)
                    OnLocalPlayerDeath.Invoke();
                else
                    Debug.LogWarning("[DEATH-DIAG] [HandleDeath] No OnLocalPlayerDeath listeners in multiplayer path — PlayerDeathSequencer missing.");
            }

            // Server checks if ALL players are now dead → coordinated return
            if (IsServerStarted)
            {
                Debug.Log("[DEATH-DIAG] [HandleDeath] Server checking if all players dead");
                CheckAllPlayersDead();
            }
        }
    }

    /// <summary>
    /// Disable a dead player's visuals and collision in multiplayer so the game
    /// continues for surviving players. The NetworkObject stays alive.
    /// </summary>
    private void DisableDeadPlayer()
    {
        // Hide sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // Disable collider so dead player doesn't block anything
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Disable weapon visuals
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in childRenderers) r.enabled = false;

        Debug.Log($"[PlayerController] Player '{gameObject.name}' disabled (dead in multiplayer)");
    }

    /// <summary>
    /// Server-only: check if every PlayerController in the session is dead.
    /// If so, transition all players back to CommandScene together.
    /// </summary>
    private void CheckAllPlayersDead()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController p in allPlayers)
        {
            if (p.IsAlive)
            {
                Debug.Log($"[PlayerController] Player '{p.gameObject.name}' is still alive — game continues");
                return;
            }
        }

        // All players dead — trigger coordinated return
        Debug.Log("[PlayerController] All players are dead! Returning everyone to CommandScene...");
        StartCoroutine(ReturnAllPlayersToCommandScene());
    }

    /// <summary>
    /// Server-only coroutine: waits briefly then revives all players and transitions
    /// them back to CommandScene using FishNet's scene management.
    /// </summary>
    private System.Collections.IEnumerator ReturnAllPlayersToCommandScene()
    {
        Debug.Log("[DEATH-DIAG] [ReturnAllPlayersToCommandScene] START — waiting for end screen to be dismissed.");

        // Wait for the end screen to be dismissed (player clicks the button).
        // Fall back to a 5-second timeout so the game never gets permanently stuck
        // if the end screen UI is missing or not assigned.
        // Phase 1: Wait for the end screen to appear (VFX delay may not have elapsed yet)
        float timeout = 30f;
        float elapsed = 0f;
        while (!EndScreenUI.IsVisible && elapsed < timeout)
        {
            elapsed += UnityEngine.Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
            Debug.LogWarning("[DEATH-DIAG] [ReturnAllPlayersToCommandScene] Timed out waiting for end screen to appear.");
        else
            Debug.Log("[DEATH-DIAG] [ReturnAllPlayersToCommandScene] End screen appeared — now waiting for dismissal.");

        // Phase 2: Wait for the player to dismiss the end screen
        elapsed = 0f;
        while (EndScreenUI.IsVisible && elapsed < timeout)
        {
            elapsed += UnityEngine.Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
            Debug.LogWarning("[DEATH-DIAG] [ReturnAllPlayersToCommandScene] End screen timeout reached — transitioning anyway.");
        else
            Debug.Log("[DEATH-DIAG] [ReturnAllPlayersToCommandScene] End screen dismissed — proceeding to CommandScene.");

        // Revive and re-enable all dead players before scene transition
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController p in allPlayers)
        {
            p.ReviveAndReEnable();
        }

        NetworkSceneTransition sceneTransition = FindFirstObjectByType<NetworkSceneTransition>();
        if (sceneTransition != null)
        {
            sceneTransition.TransitionAllPlayersToCommandScene();
        }
        else
        {
            Debug.LogError("[PlayerController] NetworkSceneTransition not found! Cannot return to CommandScene.");
        }
    }

    public void PrepareCharacterForCommandRespawn(bool keepAcquiredInventoryAndGear)
    {
        Debug.Log($"[RESPAWN] PrepareCharacterForCommandRespawn START — IsOwner={IsOwner}, keepAcquiredInventoryAndGear={keepAcquiredInventoryAndGear}");

        // Only the owning client should persist character data to disk
        if (!IsOwner)
        {
            Debug.Log("[RESPAWN] Not owner, skipping character persistence");
            return;
        }

        // Use the authoritative runtime CharacterData — NOT the singleton which may
        // be a different object (created during character selection vs. loaded by
        // LoadCharacterByIndex during spawn).
        CharacterData characterData = currentCharacterData;
        if (characterData == null)
            characterData = CharacterSelectionManager.SelectedCharacter;

        Debug.Log($"[RESPAWN] characterData={(characterData != null ? characterData.displayName : "NULL")}");
        if (characterData != null)
        {
            // Update character level and XP
            LevelUpManager levelManager = GetComponent<LevelUpManager>();
            ExperienceManager expManager = GetComponent<ExperienceManager>();

            if (levelManager != null)
            {
                characterData.characterLevel = levelManager.CurrentLevel;
                Debug.Log($"[RESPAWN] Saved level: {levelManager.CurrentLevel}");
            }

            if (expManager != null)
            {
                characterData.currentExperience = expManager.CurrentXP;
                Debug.Log($"[RESPAWN] Saved XP: {expManager.CurrentXP}");
            }

            // NOTE: Do NOT sync AllStats back to statContainer!
            // statContainer already has correct values (base + conversions)
            // Syncing AllStats would add trait modifiers, causing stacking on reload

            if (!keepAcquiredInventoryAndGear)
            {
                // Clear inventory and gear so the character resets to starter values on next spawn.
                int gearCount = characterData.equippedGear?.Count ?? 0;
                int invCount = characterData.inventorySlots?.Count ?? 0;
                if (characterData.equippedGear != null)
                    characterData.equippedGear.Clear();
                if (characterData.inventorySlots != null)
                    characterData.inventorySlots.Clear();
                Debug.Log($"[RESPAWN] Cleared acquired items: {gearCount} gear and {invCount} inventory entries");

                // Re-equip starter gear as ItemInstances so stats are correct on respawn.
                PopulateStarterGearItems(characterData);
                Debug.Log("[RESPAWN] Re-populated starter gear ItemInstances");
            }
            else
            {
                Debug.Log("[RESPAWN] Preserving acquired inventory and gear for completed round");

                // Only a proper "completed map + Return to Command" clears inMap. Death/quit
                // paths (keepAcquiredInventoryAndGear=false) intentionally leave it set so an
                // abnormal run termination (crash/force-quit) is caught on next load.
                characterData.inMap = false;
                Debug.Log("[RESPAWN] Cleared inMap flag — run completed normally");
            }

            // Reset statContainer to base values (undo level-up bonuses)
            if (characterData.baseStatContainer != null)
            {
                var baseStats = characterData.baseStatContainer.GetAllStats();
                Debug.Log($"[DEATH-DIAG] [SaveCharacterProgressOnDeath] Resetting {baseStats.Count} stats to base values");
                foreach (var stat in baseStats)
                {
                    characterData.statContainer.SetStat(stat.statID, stat.currentValue);
                }
                CharacterStatConverter.ApplyConversions(characterData);
                Debug.Log("[RESPAWN] Stats reset to base with conversions applied");
            }

            // Save to disk
            CharacterPersistence.SaveCharacter(characterData);
            Debug.Log($"[RESPAWN] Character saved: {characterData.displayName}");
        }
        else
        {
            Debug.LogWarning("[RESPAWN] No character data to save!");
        }
    }

    /// <summary>
    /// Called by EndScreenUI "Return to Command" button.  Revives, re-enables input,
    /// unlocks the cursor, and loads CommandScene — the same steps as the old automatic
    /// death coroutine, just initiated by the player via button press instead of a timer.
    /// </summary>
    public void ExecuteReturnToCommandScene(bool keepAcquiredInventoryAndGear = false)
    {
        if (SceneTransitioner.Instance != null)
        {
            SceneTransitioner.Instance.TransitionWithExternalLoad(
                () => ReturnToCommandSceneOnDeath(skipDelay: true, keepAcquiredInventoryAndGear: keepAcquiredInventoryAndGear));
            return;
        }

        StartCoroutine(ReturnToCommandSceneOnDeath(skipDelay: true, keepAcquiredInventoryAndGear: keepAcquiredInventoryAndGear));
    }

    private System.Collections.IEnumerator ReturnToCommandSceneOnDeath(bool skipDelay = false, bool keepAcquiredInventoryAndGear = false)
    {
        if (!skipDelay)
        {
            Debug.Log($"[DEATH-DIAG] [ReturnToCommandSceneOnDeath] START — waiting 2s before transition");
            yield return new UnityEngine.WaitForSeconds(2f);
        }

        PrepareCharacterForCommandRespawn(keepAcquiredInventoryAndGear);

        bool hasNetworkSession = IsServerStarted || IsClientStarted;
        if (hasNetworkSession)
        {
            // In network sessions, always use FishNet scene transition so scene NetworkBehaviours
            // (teleporter, crafting bench interactables, etc.) are reloaded correctly.
            if (IsServerStarted)
            {
                NetworkSceneTransition sceneTransition = FindFirstObjectByType<NetworkSceneTransition>();
                if (sceneTransition != null)
                {
                    Debug.Log("[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Network session detected — using NetworkSceneTransition");
                    sceneTransition.TransitionAllPlayersToCommandScene();
                    while (sceneTransition.IsTransitioningToCommandScene)
                        yield return null;
                }
                else
                {
                    Debug.LogError("[DEATH-DIAG] [ReturnToCommandSceneOnDeath] NetworkSceneTransition missing in network session");
                }
            }
            else
            {
                Debug.Log("[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Client waiting for host-driven CommandScene transition");
            }

            yield break;
        }

        Debug.Log($"[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Wait complete, calling ReviveAndReEnable");
        // Revive the player so they are alive when CommandScene loads
        ReviveAndReEnable();

        // Re-enable player input before leaving scene
        PlayerController.InputEnabled = true;
        Debug.Log($"[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Input re-enabled, cursor unlocked");

        // Show and unlock the cursor for UI navigation
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Returning to CommandScene (current={currentScene})");

        if (!IsServerStarted && !IsClientStarted && currentScene == "GameScene")
        {
            yield return ReturnToCommandSceneSinglePlayerFromArena();
            yield break;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("CommandScene");
    }

    private System.Collections.IEnumerator ReturnToCommandSceneSinglePlayerFromArena()
    {
        AsyncOperation commandLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
            "CommandScene",
            UnityEngine.SceneManagement.LoadSceneMode.Additive);

        while (!commandLoad.isDone)
            yield return null;

        UnityEngine.SceneManagement.Scene commandScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("CommandScene");
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(commandScene);
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("GameScene");

        Debug.Log("[DEATH-DIAG] [ReturnToCommandSceneOnDeath] Returned to CommandScene via additive single-player path");
    }

    /// <summary>
    /// Revive a dead player: restore health, re-enable visuals/collision,
    /// and mark as alive. Called before returning to CommandScene after death.
    /// </summary>
    public void ReviveAndReEnable()
    {
        Debug.Log($"[DEATH-DIAG] [ReviveAndReEnable] START for {gameObject.name}");
        // Restore isAlive + health/energy from Organism base
        Revive();

        // Re-enable sprite renderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            Debug.Log($"[DEATH-DIAG] [ReviveAndReEnable] Sprite renderer re-enabled");
        }

        // Re-enable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log($"[DEATH-DIAG] [ReviveAndReEnable] Collider re-enabled");
        }

        // Re-enable weapon/child visuals
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"[DEATH-DIAG] [ReviveAndReEnable] Re-enabling {childRenderers.Length} child renderers");
        foreach (var r in childRenderers) r.enabled = true;

        // Re-enable input for this player
        SetInputEnabled(true);

        Debug.Log($"[DEATH-DIAG] [ReviveAndReEnable] COMPLETE for {gameObject.name} — isAlive={IsAlive}");
    }

    /// <summary>
    /// Legacy level up method - triggers level up through ExperienceManager/LevelUpManager.
    /// Use ExperienceManager.AddExperience() for normal gameplay.
    /// </summary>
    public void LevelUp()
    {
        ExperienceManager expManager = GetComponent<ExperienceManager>();
        if (expManager != null)
        {
            // Grant enough XP to reach next level threshold
            int currentXP = expManager.CurrentXP;
            int nextThreshold = expManager.XPRequiredForNextLevel;
            int xpNeeded = nextThreshold - currentXP;

            if (xpNeeded > 0)
            {
                expManager.AddExperience(xpNeeded);
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] LevelUp called but no ExperienceManager found!");
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        if (playerInput != null)
        {
            playerInput.enabled = enabled;
        }

        inputSystemReady = enabled;
    }
    private void SpawnPet()
    {
        if (currentPet != null)
        {
            Destroy(currentPet.gameObject);
            currentPet = null;
        }

        if (currentCharacterData != null && currentCharacterData.petPrefab != null)
        {
            GameObject petObject = Instantiate(
                currentCharacterData.petPrefab,
                transform.position + new Vector3(-1.5f, 0.5f, 0),
                Quaternion.identity
            );

            currentPet = petObject.GetComponent<Pet>();
            currentPet?.SetOwner(transform);

        }
    }
}