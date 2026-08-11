using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single inventory slot in the UI grid.
/// Supports normal/activated slot sprites and border sprites based on item presence.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotSprite;           // Normal slot background
    [SerializeField] private Sprite activatedSlotSprite;  // Activated slot background (when item present)
    
    [Header("Border Sprites")]
    [SerializeField] private Sprite borderSprite;           // Normal border
    [SerializeField] private Sprite activatedBorderSprite;  // Activated border (when item present)
    
    [Header("UI References")]
    [SerializeField] private Image slotBackground;  // The background image
    [SerializeField] private Image slotBorder;      // The border image
    [SerializeField] private Image itemIcon;        // Legacy reference (not used in new system)
    [Tooltip("Small label showing stack count. Assign a TMP_Text child in the prefab.")]
    [SerializeField] private TMP_Text stackLabel;   // Stack count (hidden when stack == 1)
    
    private int slotIndex;
    private bool hasItem = false;

    /// <summary>Returns the inventory slot index this UI element represents.</summary>
    public int SlotIndex => slotIndex;
    
    /// <summary>
    /// Initialize the slot with its index in the inventory.
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;
        
        // Ensure slot background is raycastable for drag-and-drop detection
        if (slotBackground != null)
        {
            slotBackground.raycastTarget = true;
        }
        
        // Set initial sprites
        UpdateSlotVisuals();
        
        // Initially empty
        ClearSlot();
    }
    
    /// <summary>
    /// Update the slot visual state based on whether it has an item.
    /// </summary>
    private void UpdateSlotVisuals()
    {
        if (slotBackground != null)
        {
            slotBackground.sprite = hasItem && activatedSlotSprite != null ? activatedSlotSprite : slotSprite;
        }
        
        if (slotBorder != null)
        {
            slotBorder.sprite = hasItem && activatedBorderSprite != null ? activatedBorderSprite : borderSprite;
        }
    }
    
    /// <summary>
    /// Clear the slot (no item).
    /// </summary>
    public void ClearSlot()
    {
        hasItem = false;
        UpdateSlotVisuals();

        // Reset border color to white
        SetBorderColor(Color.white);

        if (itemIcon != null)
            itemIcon.enabled = false;

        SetStackCount(0);
    }

    /// <summary>
    /// Display or hide the stack-count label.
    /// Pass 0 or 1 to hide it; any value &gt;1 shows the number.
    /// </summary>
    public void SetStackCount(int count)
    {
        if (stackLabel == null) return;
        if (count > 1)
        {
            stackLabel.text = count.ToString();
            stackLabel.gameObject.SetActive(true);
        }
        else
        {
            stackLabel.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Mark the slot as having an item (activates the slot visuals).
    /// </summary>
    public void SetHasItem(bool hasItemState)
    {
        hasItem = hasItemState;
        UpdateSlotVisuals();
    }
    
    /// <summary>
    /// Set the border color (used for rarity coloring).
    /// </summary>
    public void SetBorderColor(Color color)
    {
        if (slotBorder != null)
        {
            slotBorder.color = color;
        }
    }
    
    /// <summary>
    /// Set an item in this slot (for future use).
    /// </summary>
    public void SetItem(Sprite icon)
    {
        if (itemIcon != null && icon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = true;
            itemIcon.preserveAspect = true;
        }
    }
    
    /// <summary>
    /// Called when the slot is clicked.
    /// </summary>
    public void OnSlotClicked()
    {
        Debug.Log($"[InventorySlotUI] Slot {slotIndex} clicked");
        // TODO: Handle item interaction
    }
}
