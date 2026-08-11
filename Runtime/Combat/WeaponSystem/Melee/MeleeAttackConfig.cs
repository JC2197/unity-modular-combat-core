using UnityEngine;

/// <summary>
/// Configuration for a melee attack sprite and hitbox.
/// Serializable for inline editing in the Inspector.
/// </summary>
[System.Serializable]
public class MeleeAttackConfig
{
    [Header("Attack Sprite Prefab")]
    [Tooltip("The prefab containing the attack sprite, animator, and hitbox")]
    public GameObject attackSpritePrefab;
    
    [Header("Sprite Settings")]
    [Tooltip("Rotation offset applied to the sprite (e.g., -90 for up-facing sprites)")]
    public float rotationOffset = -90f;
    
    [Tooltip("Whether to flip the sprite when facing left")]
    public bool flipWhenFacingLeft = true;
    
    [Tooltip("Scale multiplier for the attack sprite")]
    public float spriteScale = 1f;
    
    [Header("Positioning")]
    [Tooltip("Offset from owner position")]
    public Vector2 offsetFromOwner = new Vector2(0.5f, 0f);
    
    [Tooltip("Whether the sprite should follow the owner's position")]
    public bool followOwner = true;
    
    [Header("Lifetime Settings")]
    [Tooltip("Whether to destroy when animation ends")]
    public bool destroyOnAnimationEnd = true;
    
    [Tooltip("Maximum lifetime before auto-destroy (fallback)")]
    public float maxLifetime = 1f;
    
    [Header("Hitbox Settings")]
    [Tooltip("Offset for the hitbox position")]
    public Vector2 hitboxOffset = Vector2.zero;
    
    [Tooltip("Layer mask for enemy detection")]
    public LayerMask enemyLayer;
    
    [Tooltip("When the hitbox becomes active (in seconds)")]
    public float hitboxActiveStartTime = 0f;
    
    [Tooltip("When the hitbox becomes inactive (in seconds)")]
    public float hitboxActiveEndTime = 0.3f;
    
    [Header("Debug")]
    [Tooltip("Show debug gizmos in Scene view")]
    public bool showDebugGizmos = true;
}
