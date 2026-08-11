using UnityEngine;

/// <summary>
/// Represents a leg/feet armor piece.
/// Contains a ChestHolder (LegsToChest) where the body armor attaches.
/// This is the "base" piece of the modular armor system.
/// </summary>
[RequireComponent(typeof(Animator))]
public class LegGearPiece : MonoBehaviour
{
    [Header("Leg Gear Settings")]
    [Tooltip("Config containing stats and modifiers for this leg gear")]
    [SerializeField] private ArmorConfig gearConfig;
    
    [Header("Visuals")]
    [Tooltip("Sprite renderer for the leg gear")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Tooltip("Animator for leg animations")]
    [SerializeField] private Animator animator;
    
    private Animator parentAnimator; // Reference to player's animator
    
    public ArmorConfig GearConfig => gearConfig;
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    /// <summary>
    /// Initialize this leg gear piece
    /// </summary>
    public void Initialize(ArmorConfig config, Animator playerAnimator)
    {
        gearConfig = config;
        parentAnimator = playerAnimator;
        
        if (gearConfig == null)
        {
            Debug.LogError("[LegGearPiece] Cannot initialize with null config!");
            return;
        }
        
        // Apply visual settings
        ApplyVisuals();
        
        Debug.Log($"[LegGearPiece] Initialized with config: {gearConfig.gearName}");
    }
    
    /// <summary>
    /// Apply visual settings from config
    /// </summary>
    private void ApplyVisuals()
    {
        if (gearConfig == null) return;
        
        // Apply animator override controller if provided
        if (gearConfig.animatorOverride != null && animator != null)
        {
            animator.runtimeAnimatorController = gearConfig.animatorOverride;
            Debug.Log($"[LegGearPiece] Applied animator override: {gearConfig.animatorOverride.name}");
        }
        
        // Set sorting layer and order
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Player";
            spriteRenderer.sortingOrder = -1; // Behind body
        }
    }
    
    /// <summary>
    /// Synchronize animations with parent animator (only if parentAnimator is set)
    /// For legs, parentAnimator should be null since legs ARE the master animator
    /// </summary>
    private void Update()
    {
        // Throttled diagnostics: log animation chain state once per second.
        // Helps verify that parentAnimator (root) is actually playing on remote clients
        // and that legs are mirroring it correctly.
        if (Time.frameCount % 60 == 0)
        {
            if (parentAnimator == null)
                Debug.Log("[GearAnim] " + gameObject.name + " Update: parentAnimator=NULL -- legs will NOT mirror root");
            else
            {
                AnimatorClipInfo[] clips = parentAnimator.GetCurrentAnimatorClipInfo(0);
                string rootClip = clips.Length > 0 ? clips[0].clip.name : "(none)";
                AnimatorClipInfo[] legClips = animator != null ? animator.GetCurrentAnimatorClipInfo(0) : null;
                string legClip = legClips != null && legClips.Length > 0 ? legClips[0].clip.name : "(none)";
            }
        }

        // Legs mirror the root animator every frame so remote clients see the same animation
        // without needing a NetworkAnimator on the instantiated gear prefab.
        if (parentAnimator == null || animator == null) return;
        
        // If parentAnimator is set (shouldn't be for legs), sync with it
        AnimatorStateInfo stateInfo = parentAnimator.GetCurrentAnimatorStateInfo(0);
        animator.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
        
        // Copy parameters
        foreach (AnimatorControllerParameter param in parentAnimator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.name, parentAnimator.GetFloat(param.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.name, parentAnimator.GetInteger(param.name));
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(param.name, parentAnimator.GetBool(param.name));
                    break;
            }
        }
    }
}
