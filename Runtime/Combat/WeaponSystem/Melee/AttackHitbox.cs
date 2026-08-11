using UnityEngine;
using System.Collections.Generic;

public class AttackHitbox : MonoBehaviour
{
    // Configuration - populated at runtime from MeleeAttackConfig
    protected Vector2 hitboxOffset = Vector2.zero;
    protected LayerMask enemyLayer;
    protected bool showDebugGizmos = true;
    protected float activeStartTime = 0f;
    protected float activeEndTime = 0.3f;
    
    private bool isActive = false;
    private float animationTimer = 0f;
    private Transform ownerTransform;
    private Vector2 attackDirection;
    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();
    private Collider2D hitboxCollider;
    
    public System.Action<Organism> OnEnemyHit;
    
    private void Awake()
    {
        // Try to get any collider component
        hitboxCollider = GetComponent<Collider2D>();
        
        if (hitboxCollider == null)
        {
            Debug.LogError("AttackHitbox requires a Collider2D component (BoxCollider2D, EdgeCollider2D, etc.)!");
        }
        else
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false; // Start disabled
            Debug.Log($"AttackHitbox initialized with {hitboxCollider.GetType().Name}, IsTrigger={hitboxCollider.isTrigger}");
        }
    }
    
    /// <summary>
    /// Initialize configuration from MeleeAttackConfig at runtime
    /// </summary>
    public void InitializeFromConfig(MeleeAttackConfig config)
    {
        if (config == null)
        {
            Debug.LogError("AttackHitbox.InitializeFromConfig: config is null!");
            return;
        }
        
        hitboxOffset = config.hitboxOffset;
        enemyLayer = config.enemyLayer;
        showDebugGizmos = config.showDebugGizmos;
        activeStartTime = config.hitboxActiveStartTime;
        activeEndTime = config.hitboxActiveEndTime;
    }
    
    public void Initialize(Transform owner, Vector2 direction)
    {
        ownerTransform = owner;
        attackDirection = direction.normalized;
        animationTimer = 0f;
        hitEnemies.Clear();
        
        Debug.Log($"AttackHitbox.Initialize called - Owner: {owner.name}, Direction: {direction}");
        
        // Enable immediately for testing
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
            isActive = true;
            Debug.Log($"Hitbox collider ENABLED at position {transform.position}");
        }
    }
    
    private void Update()
    {
        if (ownerTransform == null) return;
        
        animationTimer += Time.deltaTime;
        
        // DON'T update position - we're already positioned correctly by the parent AttackSprite
        // Vector2 hitboxPosition = (Vector2)ownerTransform.position + 
        //     attackDirection * hitboxOffset.x + 
        //     Vector2.up * hitboxOffset.y;
        // transform.position = hitboxPosition;
        
        // Check if hitbox should be active
        bool wasActive = isActive;
        isActive = animationTimer >= activeStartTime && animationTimer <= activeEndTime;
        
        // Enable/disable collider based on active state
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = isActive;
        }
        
        // Log activation
        if (isActive && !wasActive)
        {
            Debug.Log($"Attack hitbox ACTIVATED at time {animationTimer:F3}s, position {transform.position}");
        }
        else if (!isActive && wasActive)
        {
            Debug.Log($"Attack hitbox DEACTIVATED at time {animationTimer:F3}s");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[AttackHitbox] OnTriggerEnter2D with {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        
        // Only process hits when active
        if (!isActive)
        {
            Debug.Log($"[AttackHitbox] Hitbox not active, ignoring collision");
            return;
        }
        
        // Check if it's on the enemy layer
        int otherLayerMask = 1 << other.gameObject.layer;
        bool isOnEnemyLayer = (otherLayerMask & enemyLayer) != 0;
        
        Debug.Log($"[AttackHitbox] Layer check: otherLayer={other.gameObject.layer}, enemyLayerMask={enemyLayer.value}, isOnEnemyLayer={isOnEnemyLayer}");
        
        if (!isOnEnemyLayer)
        {
            Debug.Log($"[AttackHitbox] {other.name} is not on enemy layer, ignoring");
            return;
        }
        
        // Prevent hitting the same enemy twice
        if (hitEnemies.Contains(other))
        {
            Debug.Log($"[AttackHitbox] {other.name} already hit, ignoring");
            return;
        }
        
        Organism enemy = other.GetComponent<Organism>();
        if (enemy != null)
        {
            hitEnemies.Add(other);
            
            Debug.Log($"✓ [AttackHitbox] HIT CONFIRMED on {enemy.name}! Invoking OnEnemyHit event");
            
            // Notify the ability that an enemy was hit
            OnEnemyHit?.Invoke(enemy);
        }
        else
        {
            Debug.LogWarning($"[AttackHitbox] {other.name} has no Organism component!");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null) return;
        
        Vector3 position = transform.position;
        
        // Draw based on collider type
        if (collider is BoxCollider2D boxCollider)
        {
            Vector2 size = boxCollider.size;
            
            if (isActive)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Gizmos.DrawCube(position, size);
            }
            
            Gizmos.color = isActive ? Color.red : new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(position, size);
        }
        else if (collider is EdgeCollider2D edgeCollider)
        {
            Gizmos.color = isActive ? Color.red : new Color(1f, 1f, 0f, 0.8f);
            Vector2[] points = edgeCollider.points;
            
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 p1 = transform.TransformPoint(points[i]);
                Vector3 p2 = transform.TransformPoint(points[i + 1]);
                Gizmos.DrawLine(p1, p2);
                
                // Draw small spheres at each point
                Gizmos.DrawWireSphere(p1, 0.05f);
            }
            if (points.Length > 0)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(points[points.Length - 1]), 0.05f);
            }
        }
        
        // Draw label
            #if UNITY_EDITOR
        UnityEditor.Handles.Label(position, isActive ? "ACTIVE" : "Inactive");
        #endif
    }
}