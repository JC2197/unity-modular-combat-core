using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the inventory UI - displays a grid of inventory slots.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 8;
    [SerializeField] private int rows = 4;
    [SerializeField] private float slotSize = 56f;  // 26px sprite scaled to 56px
    [SerializeField] private float horizontalSpacing = 4f;  // 2px * 2 scale
    [SerializeField] private float verticalSpacing = 2f;    // 1px * 2 scale

    [Header("Prefab")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Icon Settings")]
    [Tooltip("Width of the item icon in pixels.")]
    [SerializeField] private float iconWidth = 40f;
    [Tooltip("Height of the item icon in pixels.")]
    [SerializeField] private float iconHeight = 40f;

    [Header("Container")]
    [SerializeField] private RectTransform gridContainer;

    private InventorySlotUI[] slots;
    private int totalSlots;
    private bool isPopulated = false; // Track if inventory has been populated with items

    private void Awake()
    {
        totalSlots = columns * rows;

        // Ensure we have a grid container
        if (gridContainer == null)
        {
            Debug.LogError("[InventoryUI] Grid container not assigned!");
            return;
        }
    }

    /// <summary>
    /// Initialize the inventory UI and create the grid.
    /// </summary>
    public void Initialize()
    {
        // Recalculate totalSlots in case it wasn't set properly
        totalSlots = columns * rows;


        if (totalSlots <= 0)
        {
            Debug.LogError($"[InventoryUI] Invalid grid size! columns={columns}, rows={rows}, totalSlots={totalSlots}");
            return;
        }

        if (gridContainer == null)
        {
            Debug.LogError("[InventoryUI] Cannot initialize - gridContainer is null!");
            return;
        }
        // Clear existing slots if any
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        // Create slot array
        slots = new InventorySlotUI[totalSlots];

        // Setup GridLayoutGroup if present
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;
            gridLayout.cellSize = new Vector2(slotSize, slotSize);
            gridLayout.spacing = new Vector2(horizontalSpacing, verticalSpacing);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            // Padding: 14 from sides (7px * 2 scale), 8 from top (4px * 2 scale), 8 from bottom (4px * 2 scale)
            gridLayout.padding = new RectOffset(14, 14, 8, 8);
        }
        else
        {
            Debug.LogWarning("[InventoryUI] No GridLayoutGroup found on container!");
        }

        // Create slots
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }
    }

    private void CreateSlot(int index)
    {
        GameObject slotObj;

        if (slotPrefab != null)
        {
            // Use prefab
            slotObj = Instantiate(slotPrefab, gridContainer);
        }
        else
        {
            // Create simple slot programmatically
            slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(gridContainer, false);

            // Add Image component for background
            Image bgImage = slotObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add Button component for interaction
            Button button = slotObj.AddComponent<Button>();

            // Create item icon child
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(slotObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image itemIcon = iconObj.AddComponent<Image>();
            itemIcon.enabled = false; // Hidden until item is set

            // Add InventorySlotUI component
            InventorySlotUI slotUI = slotObj.AddComponent<InventorySlotUI>();

            // Set references
            var slotUIType = typeof(InventorySlotUI);
            var bgField = slotUIType.GetField("slotBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconField = slotUIType.GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (bgField != null) bgField.SetValue(slotUI, bgImage);
            if (iconField != null) iconField.SetValue(slotUI, itemIcon);

            // Connect button to slot
            button.onClick.AddListener(() => slotUI.OnSlotClicked());
        }

        // Get or add InventorySlotUI component
        InventorySlotUI slotComponent = slotObj.GetComponent<InventorySlotUI>();
        if (slotComponent == null)
        {
            slotComponent = slotObj.AddComponent<InventorySlotUI>();
        }

        slotComponent.Initialize(index);
        slots[index] = slotComponent;
    }

    /// <summary>
    /// Populate inventory UI with items from CharacterData.
    /// Only creates new item GameObjects on first call - subsequent calls update existing items.
    /// </summary>
    public void PopulateInventory(CharacterData character)
    {
        if (character == null || slots == null)
        {
            Debug.LogWarning("[InventoryUI] Cannot populate - character or slots is null!");
            return;
        }

        // Only clear and recreate on first population
        if (!isPopulated)
        {
            ClearAllSlots();
            
            // Display each item in the inventory using slot indices
            foreach (var kvp in character.inventorySlots)
            {
                int slotIndex = kvp.Key;
                ItemInstance item = kvp.Value;
                
                if (item != null && slotIndex >= 0 && slotIndex < totalSlots && slots[slotIndex] != null)
                {
                    DisplayItemInSlot(item, slotIndex);
                }
            }
            
            isPopulated = true;
        }
        else
        {
            Debug.Log("[InventoryUI] Inventory already populated - use RefreshInventory() for updates");
        }
    }

    /// <summary>
    /// Display an item in a specific slot
    /// </summary>
    private void DisplayItemInSlot(ItemInstance item, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
        {
            Debug.LogWarning($"[InventoryUI] Slot index {slotIndex} out of range!");
            return;
        }

        Transform slotTransform = slots[slotIndex].transform;

        // Check if there's already an InventoryItemUI in this slot
        InventoryItemUI existingItemUI = slotTransform.GetComponentInChildren<InventoryItemUI>();
        if (existingItemUI != null)
        {
            // Reuse existing item UI
            existingItemUI.Initialize(item);
            return;
        }
        
        // Create new InventoryItemUI GameObject
        GameObject itemObj = new GameObject($"Item_{item.displayName}");
        itemObj.transform.SetParent(slotTransform, false);

        // Setup RectTransform to fill slot
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = Vector2.zero;
        itemRect.anchorMax = Vector2.one;
        itemRect.offsetMin = Vector2.zero;
        itemRect.offsetMax = Vector2.zero;

        // Create item icon
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(itemObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(iconWidth, iconHeight);

        Image itemIcon = iconObj.AddComponent<Image>();
        itemIcon.raycastTarget = false; // Parent handles clicks
        itemIcon.preserveAspect = true; // Maintain icon aspect ratio

        // Add InventoryItemUI component
        InventoryItemUI itemUI = itemObj.AddComponent<InventoryItemUI>();

        // Set references using reflection
        var itemUIType = typeof(InventoryItemUI);
        var iconField = itemUIType.GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (iconField != null) iconField.SetValue(itemUI, itemIcon);

        // Initialize with item data
        itemUI.Initialize(item);
    }

    /// <summary>
    /// Clear all items from inventory display
    /// </summary>
    private void ClearAllSlots()
    {
        if (slots == null) return;
        
        int clearedCount = 0;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            // Find and destroy any InventoryItemUI children
            InventoryItemUI itemUI = slot.GetComponentInChildren<InventoryItemUI>();
            if (itemUI != null)
            {
                // Use DestroyImmediate to ensure it's gone before creating new items
                DestroyImmediate(itemUI.gameObject);
                clearedCount++;
            }

            // Clear the slot UI
            slot.ClearSlot();
        }
        
    }
    
    /// <summary>
    /// Add or update a single item in a specific slot (for item pickups)
    /// </summary>
    public void AddItemToSlot(ItemInstance item, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= totalSlots)
        {
            Debug.LogWarning($"[InventoryUI] Cannot add item - slot index {slotIndex} out of range!");
            return;
        }
        
        if (slots == null || slots[slotIndex] == null)
        {
            Debug.LogWarning($"[InventoryUI] Cannot add item - slot {slotIndex} is null!");
            return;
        }
        
        Debug.Log($"[InventoryUI] Adding/updating item in slot {slotIndex}: {item.displayName}");
        DisplayItemInSlot(item, slotIndex);
    }

    /// <summary>
    /// Refresh inventory display with updated data.
    /// Smart update - only changes slots that have been added/removed/modified.
    /// </summary>
    public void RefreshInventory(CharacterData character)
    {
        if (character == null || slots == null)
        {
            Debug.LogWarning("[InventoryUI] Cannot refresh - character or slots is null!");
            return;
        }
        
        Debug.Log($"[InventoryUI] Refreshing inventory with {character.inventorySlots.Count} items");
        
        // Update all slots
        for (int i = 0; i < totalSlots; i++)
        {
            if (slots[i] == null) continue;
            
            Transform slotTransform = slots[i].transform;
            InventoryItemUI existingItemUI = slotTransform.GetComponentInChildren<InventoryItemUI>();
            ItemInstance itemAtSlot = character.GetItemAtSlot(i);
            
            if (itemAtSlot != null)
            {
                // There should be an item here - add or update it
                DisplayItemInSlot(itemAtSlot, i);
            }
            else
            {
                // No item should be here - clear the slot if it has an item
                if (existingItemUI != null)
                {
                    Debug.Log($"[InventoryUI] Removing item from slot {i}");
                    DestroyImmediate(existingItemUI.gameObject);
                    slots[i].ClearSlot();
                }
            }
        }
        
        Debug.Log("[InventoryUI] Refresh complete");
    }
}
