using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Orchestrates the level-up sequence in order:
///   1. LevelUpNotification shows its message (driven by its own OnLevelUp subscription).
///   2. A VFX prefab is instantiated at the player's position and plays automatically.
///   3. After <see cref="vfxToTraitRollerDelay"/> seconds, the TraitRoller is triggered
///      and the Trait Roller screen opens.
///
/// Incoming reward rounds are queued so that rapid/simultaneous level-ups are handled
/// one at a time instead of spawning overlapping coroutines.
///
/// Attach this to the Player GameObject alongside LevelUpRewardDirector and TraitRoller.
/// When this component is present, it disables the director's built-in auto-dispatch so
/// the timing is fully controlled here.
/// </summary>
[RequireComponent(typeof(LevelUpRewardDirector))]
[RequireComponent(typeof(TraitRoller))]
public class LevelUpSequencer : MonoBehaviour
{
    [Header("VFX")]
    [Tooltip("Prefab spawned at the player's world position on level up. Should auto-play and self-destruct.")]
    [SerializeField] private GameObject levelUpVFXPrefab;

    [Header("Timing")]
    [Tooltip("Seconds to wait after spawning the VFX before opening the Trait Roller screen. " +
             "Should be at least as long as the VFX animation duration.")]
    [SerializeField] private float vfxToTraitRollerDelay = 2f;

    private LevelUpRewardDirector rewardDirector;
    private TraitRoller traitRoller;

    private readonly Queue<LevelUpRewardRoundContext> _pendingRounds = new Queue<LevelUpRewardRoundContext>();
    private bool _sequenceRunning = false;

    private void Awake()
    {
        rewardDirector = GetComponent<LevelUpRewardDirector>();
        traitRoller = GetComponent<TraitRoller>();

        // Disable the director's own dispatch so we control when the trait roll fires.
        if (rewardDirector != null)
            rewardDirector.AutoDispatchToTraitRoller = false;
    }

    private void OnEnable()
    {
        if (rewardDirector != null)
            rewardDirector.OnRewardRoundRequested += HandleRewardRoundRequested;
    }

    private void OnDisable()
    {
        if (rewardDirector != null)
            rewardDirector.OnRewardRoundRequested -= HandleRewardRoundRequested;
    }

    private void HandleRewardRoundRequested(LevelUpRewardRoundContext context)
    {
        _pendingRounds.Enqueue(context);

        if (!_sequenceRunning)
            StartCoroutine(DrainQueue());
    }

    /// <summary>
    /// Processes queued reward rounds one at a time, waiting for each trait-roller
    /// session to complete before beginning the next.
    /// </summary>
    private IEnumerator DrainQueue()
    {
        _sequenceRunning = true;

        while (_pendingRounds.Count > 0)
        {
            LevelUpRewardRoundContext context = _pendingRounds.Dequeue();
            yield return StartCoroutine(LevelUpSequence(context));
        }

        _sequenceRunning = false;
    }

    private IEnumerator LevelUpSequence(LevelUpRewardRoundContext context)
    {
        // Step 1: LevelUpNotification already handles showing the message via its own
        //         LevelUpManager.OnLevelUp subscription — nothing to do here.

        // Step 2: Spawn level-up VFX as a child of the player so it follows movement.
        if (levelUpVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(levelUpVFXPrefab, transform.position, Quaternion.identity, transform);
            // Fallback destroy in case the prefab lacks its own self-destruct logic.
            Destroy(vfxInstance, vfxToTraitRollerDelay + 2f);
        }

        // Step 3: Wait for the VFX / animation to finish.
        // Use WaitForSecondsRealtime so this delay runs even while Time.timeScale = 0
        // (e.g. when a previous trait-roller session has paused the game).
        yield return new WaitForSecondsRealtime(vfxToTraitRollerDelay);

        // Step 4: Roll traits and open the Trait Roller screen.
        if (traitRoller != null)
            traitRoller.RollTraitsForLevelUp(context);

        // Step 5: Wait until the trait roller session is finished before processing the
        // next queued round.  TraitRoller.OnTraitsRolled fires when a roll opens;
        // we use a simple flag polled each frame to know when the UI is done.
        yield return new WaitUntil(() => !TraitRollerUI.IsSessionActive);
    }
}
