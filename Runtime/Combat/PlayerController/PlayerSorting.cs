using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSorting : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrderBase = 0;
    
    [Header("Y-Sorting Precision")]
    [Tooltip("Negative values = lower Y positions render in front (typical for top-down). Recommended: -100 to -10")]
    [SerializeField] private float sortingOrderMultiplier = -100f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private SpriteRenderer spriteRenderer;
    private int lastSortingOrder = int.MinValue;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = sortingLayerName;
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerSorting] Initialized with base={sortingOrderBase}, multiplier={sortingOrderMultiplier}");
        }
    }
    
    private void LateUpdate()
    {
        // Update sorting order based on Y position
        int newSortingOrder = sortingOrderBase + Mathf.RoundToInt(transform.position.y * sortingOrderMultiplier);
        
        if (newSortingOrder != lastSortingOrder)
        {
            spriteRenderer.sortingOrder = newSortingOrder;
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerSorting] Y={transform.position.y:F2} -> SortingOrder={newSortingOrder}");
            }
            
            lastSortingOrder = newSortingOrder;
        }
    }
}