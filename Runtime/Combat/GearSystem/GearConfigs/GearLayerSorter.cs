using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dynamically sorts gear layers based on Y-position while maintaining proper layering.
/// Attach to the PlayerCharacter with PlayerGearManager to sort all equipped gear.
/// </summary>
public class GearLayerSorter : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float sortingOrderMultiplier = 100f;
    [SerializeField] private bool updateEveryFrame = true;
    
    [Header("Gear Layer Offsets")]
    [Tooltip("Base layer (legs sprite)")]
    [SerializeField] private int legsOffset = 0;
    [Tooltip("Chest gear offset from base")]
    [SerializeField] private int chestOffset = 1;
    [Tooltip("Head gear offset from base")]
    [SerializeField] private int headOffset = 2;
    [Tooltip("Hands/gloves offset from base")]
    [SerializeField] private int handsOffset = 3;
    [Tooltip("Backpack offset from base")]
    [SerializeField] private int backpackOffset = -1; // Behind chest
    
    private PlayerGearManager gearManager;
    private float lastY;
    
    private void Awake()
    {
        gearManager = GetComponent<PlayerGearManager>();
        
        if (gearManager == null)
        {
            Debug.LogWarning("[GearLayerSorter] No PlayerGearManager found!");
        }
    }
    
    private void Start()
    {
        UpdateSortingOrders();
    }
    
    private void LateUpdate()
    {
        if (updateEveryFrame || Mathf.Abs(transform.position.y - lastY) > 0.01f)
        {
            UpdateSortingOrders();
            lastY = transform.position.y;
        }
    }
    
    /// <summary>
    /// Updates sorting orders for all equipped gear based on current Y position
    /// </summary>
    public void UpdateSortingOrders()
    {
        if (gearManager == null) return;
        
        // Calculate base sorting order from Y position
        // Lower Y = higher sorting order (further back)
        int baseSortingOrder = Mathf.RoundToInt(-transform.position.y * sortingOrderMultiplier);
        
        // Update all gear sprite renderers
        SpriteRenderer[] gearRenderers = gearManager.GetAllGearSpriteRenderers();
        foreach (SpriteRenderer renderer in gearRenderers)
        {
            if (renderer != null)
            {
                // Determine gear type from GameObject name or component
                int offset = GetOffsetForRenderer(renderer);
                renderer.sortingOrder = baseSortingOrder + offset;
            }
        }
    }
    
    /// <summary>
    /// Get the sorting order offset for a sprite renderer based on its gear piece type
    /// </summary>
    private int GetOffsetForRenderer(SpriteRenderer renderer)
    {
        // Check what type of gear piece this renderer belongs to
        if (renderer.GetComponent<LegGearPiece>() != null)
            return legsOffset;
        if (renderer.GetComponent<ChestGearPiece>() != null)
            return chestOffset;
        if (renderer.GetComponent<HeadGearPiece>() != null)
            return headOffset;
        
        return 0;
    }
    
    /// <summary>
    /// Force an immediate update of all sorting orders
    /// Call this when gear is equipped/unequipped
    /// </summary>
    public void ForceUpdate()
    {
        UpdateSortingOrders();
    }
}
