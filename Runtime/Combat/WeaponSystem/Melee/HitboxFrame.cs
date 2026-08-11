using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class HitboxFrame
{
    public float frameTime;          // When this hitbox is active (0-1, normalized animation time)
    public Vector2 offset;           // Position offset from player
    public Vector2 size;             // Hitbox dimensions
    public Color debugColor = Color.red;
}

public class AnimatedAttackHitbox : MonoBehaviour
{
    [Header("Hitbox Frames")]
    [SerializeField] private List<HitboxFrame> hitboxFrames = new List<HitboxFrame>();
    
    [Header("Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float animationDuration = 0.42f;
    [SerializeField] private bool showDebugGizmos = true;
    
    private Transform ownerTransform;
    private Vector2 attackDirection;
    private float animationTimer = 0f;
    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>(); // Prevent hitting same enemy twice
    
    // Event that fires when an enemy is hit
    public System.Action<Organism> OnEnemyHit;
    
    public void Initialize(Transform owner, Vector2 direction)
    {
        ownerTransform = owner;
        attackDirection = direction.normalized;
        animationTimer = 0f;
        hitEnemies.Clear();
    }
    
    private void Update()
    {
        if (ownerTransform == null) return;
        
        animationTimer += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(animationTimer / animationDuration);
        
        // Check current frame's hitbox
        HitboxFrame currentFrame = GetCurrentHitboxFrame(normalizedTime);
        if (currentFrame != null)
        {
            CheckHitboxFrame(currentFrame);
        }
    }
    
    private HitboxFrame GetCurrentHitboxFrame(float normalizedTime)
    {
        foreach (var frame in hitboxFrames)
        {
            if (Mathf.Abs(normalizedTime - frame.frameTime) < 0.05f) // 5% tolerance
            {
                return frame;
            }
        }
        return null;
    }
    
    private void CheckHitboxFrame(HitboxFrame frame)
    {
        if (ownerTransform == null) return;
        
        Vector2 hitboxPosition = (Vector2)ownerTransform.position + 
            attackDirection * frame.offset.x + 
            Vector2.up * frame.offset.y;
        
        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxPosition, frame.size, 0f, enemyLayer);
        
        foreach (Collider2D hit in hits)
        {
            // Only hit each enemy once per attack
            if (hitEnemies.Contains(hit)) continue;
            
            Organism enemy = hit.GetComponent<Organism>();
            if (enemy != null)
            {
                hitEnemies.Add(hit);
                
                // Notify the ability that an enemy was hit
                OnEnemyHit?.Invoke(enemy);
                
                Debug.Log($"Animated hitbox detected {enemy.name}");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || ownerTransform == null) return;
        
        float normalizedTime = Mathf.Clamp01(animationTimer / animationDuration);
        
        foreach (var frame in hitboxFrames)
        {
            Vector2 hitboxPosition = (Vector2)ownerTransform.position + 
                attackDirection * frame.offset.x + 
                Vector2.up * frame.offset.y;
            
            // Highlight current frame
            if (Mathf.Abs(normalizedTime - frame.frameTime) < 0.05f)
            {
                Gizmos.color = new Color(frame.debugColor.r, frame.debugColor.g, frame.debugColor.b, 0.7f);
                Gizmos.DrawCube(hitboxPosition, frame.size);
            }
            
            Gizmos.color = frame.debugColor;
            Gizmos.DrawWireCube(hitboxPosition, frame.size);
        }
    }
}