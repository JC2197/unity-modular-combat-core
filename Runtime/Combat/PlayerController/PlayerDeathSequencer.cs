using UnityEngine;
using System.Collections;
using JoeConticello.VisualEffects;

/// <summary>
/// Drives the death sequence for the local player in order:
///   1. Spawn death VFX prefab as a child of the player.
///   2. Wait for <see cref="vfxDuration"/> seconds.
///   3. Show the End Screen UI.
///
/// Attach this to the Player GameObject.
/// The End Screen's "Return to Command" button calls
/// <see cref="PlayerController.ExecuteReturnToCommandScene"/> to do the actual transition.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerDeathSequencer : MonoBehaviour
{
    [Header("VFX")]
    [Tooltip("Particle-system prefab spawned at the player's position on death. Should auto-play and self-destruct.")]
    [SerializeField] private GameObject deathVFXPrefab;

    [Header("Timing")]
    [Tooltip("Seconds to wait after spawning the VFX before showing the End Screen.")]
    [SerializeField] private float vfxDuration = 2f;

    private void Awake()
    {
        Debug.Log($"[DEATH-DIAG] [PlayerDeathSequencer] Awake on '{gameObject.name}'. deathVFXPrefab={(deathVFXPrefab != null ? deathVFXPrefab.name : "NULL")}");
    }

    private void OnEnable()
    {
        PlayerController.OnLocalPlayerDeath += HandleLocalPlayerDeath;
        Debug.Log("[DEATH-DIAG] [PlayerDeathSequencer] Subscribed to OnLocalPlayerDeath.");
    }

    private void OnDisable()
    {
        PlayerController.OnLocalPlayerDeath -= HandleLocalPlayerDeath;
        Debug.Log("[DEATH-DIAG] [PlayerDeathSequencer] Unsubscribed from OnLocalPlayerDeath.");
    }

    private void HandleLocalPlayerDeath()
    {
        Debug.Log("[DEATH-DIAG] [PlayerDeathSequencer] HandleLocalPlayerDeath called — starting DeathSequence coroutine.");
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log($"[DEATH-DIAG] [PlayerDeathSequencer] DeathSequence START. vfxDuration={vfxDuration}s");

        // Step 1: Spawn death VFX as a child so it follows the player position.
        if (deathVFXPrefab != null)
        {
            GameObject effect = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity, transform);
            AutoDestroyEffect.SetupAutoDestroy(effect, vfxDuration);
            Debug.Log($"[DEATH-DIAG] [PlayerDeathSequencer] Spawned deathVFX '{deathVFXPrefab.name}'.");
        }
        else
        {
            Debug.LogWarning("[DEATH-DIAG] [PlayerDeathSequencer] No deathVFXPrefab assigned — skipping VFX.");
        }

        // Step 2: Wait for VFX to play out.
        Debug.Log($"[DEATH-DIAG] [PlayerDeathSequencer] Waiting {vfxDuration}s (realtime) before showing end screen...");
        yield return new WaitForSecondsRealtime(vfxDuration);
        Debug.Log("[DEATH-DIAG] [PlayerDeathSequencer] Wait complete.");

        // Step 3: Show end screen with research points.
        if (EndScreenUI.Instance != null)
        {
            Debug.Log("[DEATH-DIAG] [PlayerDeathSequencer] Calling EndScreenUI.Instance.Show().");

            // Calculate and award research points based on timer survival %
            int earned = 0;
            if (ResearchPointManager.Instance != null)
            {
                ArenaTimer timer = Object.FindFirstObjectByType<ArenaTimer>();
                ArenaConfig config = null;
                ArenaManager arenaManager = Object.FindFirstObjectByType<ArenaManager>();
                if (arenaManager != null) config = arenaManager.CurrentArena;

                earned = ResearchPointManager.Instance.CalculateResearchPoints(timer, config);
                ResearchPointManager.Instance.AwardResearchPoints(earned);
            }

            // Award crafting materials
            AwardEndOfRunMaterials();

            EndScreenUI.Instance.Show("Defeated", earned, keepAcquiredInventoryAndGearOnReturn: false);
        }
        else
        {
            Debug.LogError("[DEATH-DIAG] [PlayerDeathSequencer] EndScreenUI.Instance is NULL — ensure EndScreenUI exists in the HUD scene.");
        }
    }

    private void AwardEndOfRunMaterials()
    {
        PlayerController localPlayer = PlayerController.GetLocalPlayer();
        if (localPlayer == null) return;

        WeaponCraftingManager craftingManager = localPlayer.GetComponent<WeaponCraftingManager>();
        if (craftingManager == null && WeaponCraftingSystemManager.Instance != null)
            craftingManager = WeaponCraftingSystemManager.Instance.GetOrCreateCraftingManager(localPlayer.gameObject);

        if (craftingManager != null)
        {
            craftingManager.AddMaterials(wood: 1, metal: 1, stone: 1, glass: 1);
            Debug.Log("[PlayerDeathSequencer] Awarded end-of-run materials: 1 Wood, 1 Metal, 1 Stone, 1 Glass");
        }
    }
}
