using UnityEngine;
using UnityEngine.UI;

public class GearSlotUI : MonoBehaviour
{
    [Header("Gear Slot Configuration")]
    [SerializeField] private GearSlot slotType;
    
    [Header("Equipped Sprite References")]
    [SerializeField] private Sprite equippedSlotSprite;
    [SerializeField] private Sprite equippedOutlineSprite;
    
    [Header("Image Components")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Image outlineImage;
    
    private bool hasGear = false;
    private Sprite originalSlotSprite;
    private Sprite originalOutlineSprite;
    
    public GearSlot SlotType => slotType;
    public bool HasGear => hasGear;
    
    private void Awake()
    {
        if (slotImage == null)
            slotImage = GetComponent<Image>();
        
        // Store original sprites configured in Unity editor
        if (slotImage != null)
        {
            originalSlotSprite = slotImage.sprite;
            slotImage.raycastTarget = true;
        }
        
        if (outlineImage != null)
        {
            originalOutlineSprite = outlineImage.sprite;
        }
    }
    
    /// <summary>
    /// Called by GearItemUI to indicate this slot now has/doesn't have gear
    /// </summary>
    public void SetHasGear(bool equipped)
    {
        hasGear = equipped;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Updates slot and outline sprites based on equipped state
    /// </summary>
    private void UpdateVisuals()
    {
        if (slotImage != null)
        {
            // Only change sprite if we have an equipped sprite and gear is equipped
            if (hasGear && equippedSlotSprite != null)
                slotImage.sprite = equippedSlotSprite;
            else
                slotImage.sprite = originalSlotSprite;
        }
        
        if (outlineImage != null)
        {
            // Only change sprite if we have an equipped sprite and gear is equipped
            if (hasGear && equippedOutlineSprite != null)
                outlineImage.sprite = equippedOutlineSprite;
            else
                outlineImage.sprite = originalOutlineSprite;
                
            // Show outline when equipped, hide when empty
            outlineImage.gameObject.SetActive(hasGear);
        }
    }
    
    /// <summary>
    /// Sets the outline color based on gear rarity
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        if (outlineImage != null)
        {
            outlineImage.color = color;
        }
    }
    
    /// <summary>
    /// Resets the slot to empty state
    /// </summary>
    public void ClearSlot()
    {
        hasGear = false;
        UpdateVisuals();
    }
}
