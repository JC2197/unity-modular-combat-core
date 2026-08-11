using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the inventory system - opening/closing UI and handling input.
/// Attach this to a GameObject in your game scene.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // Singleton for static access
    private static InventoryManager instance;
    public static InventoryManager Instance => instance;
    
    [Header("References")]
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private GameObject hudCanvas; // HUD to disable during inventory
    [SerializeField] private CharacterStatsPanel statsPanel; // Stats panel that can be opened separately
    
    private bool isInventoryOpen = false;
    public bool IsInventoryOpen => isInventoryOpen;
    private bool isStatsOpen = false;
    private bool isInitialized = false;
    private bool inventoryPopulated = false; // Track if inventory items have been created
    
    private void Awake()
    {
        // Singleton guard — prevent duplicates on scene reload
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Set singleton
        instance = this;
        
        // Auto-find InventoryUI if not assigned
        if (inventoryUI == null)
        {
            Debug.Log("[InventoryManager] InventoryUI not assigned, searching in scene...");
            inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include); // Include inactive objects
            
            if (inventoryUI != null)
            {
                Debug.Log($"[InventoryManager] Found InventoryUI on: {inventoryUI.gameObject.name}");
            }
            else
            {
                Debug.LogError("[InventoryManager] InventoryUI not found in scene!");
            }
        }
        
        // Auto-find canvas if not assigned
        if (inventoryCanvas == null && inventoryUI != null)
        {
            inventoryCanvas = inventoryUI.transform.root.gameObject;
            Debug.Log($"[InventoryManager] Auto-assigned inventory canvas: {inventoryCanvas.name}");
        }
        
        // Ensure inventory starts closed
        if (inventoryCanvas != null)
        {
            inventoryCanvas.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Initialize the inventory UI
        if (inventoryUI != null && !isInitialized)
        {
            inventoryUI.Initialize();
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    private void HandlePlayerSpawned(PlayerController player)
    {
        if (!player.IsOwner) return;

        // If inventory is already open but couldn't be populated earlier (player wasn't
        // ready yet), populate it now that we have a valid CharacterData.
        if (isInventoryOpen && !inventoryPopulated && inventoryUI != null)
        {
            CharacterData characterData = player.GetCurrentCharacterData();
            if (characterData != null)
            {
                inventoryUI.PopulateInventory(characterData);
                inventoryPopulated = true;
                Debug.Log("[InventoryManager] Late-populated inventory after player spawned.");
            }
        }
    }
    
    private void Update()
    {
        // Check for Tab key press to toggle inventory
        if (InputHelper.GetKeyDown(Key.Tab))
        {
            if (isInventoryOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
        
        // Check for C key press to toggle stats panel
        if (InputHelper.GetKeyDown(Key.C))
        {
            if (isStatsOpen)
            {
                CloseStatsPanel();
            }
            else
            {
                OpenStatsPanel();
            }
        }
        
        // ESC is handled centrally by PauseMenuManager via CursorManager.TryCloseTopPanel()
    }
    
    /// <summary>
    /// Open the inventory UI.
    /// </summary>
    public void OpenInventory()
    {
        if (isInventoryOpen)
        {
            Debug.Log("[InventoryManager] Inventory already open");
            return;
        }
        
        Debug.Log("[InventoryManager] Opening inventory");
        
        // Initialize if needed
        if (!isInitialized && inventoryUI != null)
        {
            inventoryUI.Initialize();
            isInitialized = true;
        }
        
        // Show inventory
        if (inventoryCanvas != null)
        {
            inventoryCanvas.SetActive(true);
        }
        
        // Load character data and populate inventory (only first time)
        CharacterData characterData = GetPlayerCharacterData();
        if (characterData != null && inventoryUI != null)
        {
            if (!inventoryPopulated)
            {
                inventoryUI.PopulateInventory(characterData);
                inventoryPopulated = true;
                Debug.Log($"[InventoryManager] Populated inventory with {characterData.inventory.Count} items (first time)");
            }
            else
            {
                // Refresh to show any items picked up while inventory was closed
                inventoryUI.RefreshInventory(characterData);
                Debug.Log($"[InventoryManager] Refreshed inventory with {characterData.inventorySlots.Count} items");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Could not populate inventory - missing data or UI");
        }
        
        isInventoryOpen = true;
        
        // Switch to UI cursor mode and register ESC close handler
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.PushPanel(CloseInventory);
        }
        
        Debug.Log("[InventoryManager] Inventory opened - Player can still move and use abilities");
    }
    
    /// <summary>
    /// Close the inventory UI.
    /// </summary>
    public void CloseInventory()
    {
        if (!isInventoryOpen)
        {
            return;
        }
        
        Debug.Log("[InventoryManager] Closing inventory");
        
        // Hide inventory
        if (inventoryCanvas != null)
        {
            inventoryCanvas.SetActive(false);
        }
        
        isInventoryOpen = false;
        
        // Deregister from ESC stack and switch back to gameplay cursor mode
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.PopPanel();
        }
        
        Debug.Log("[InventoryManager] Inventory closed");
    }
    
    /// <summary>
    /// Open the stats panel independently.
    /// </summary>
    public void OpenStatsPanel()
    {
        if (isStatsOpen)
        {
            Debug.Log("[InventoryManager] Stats panel already open");
            return;
        }
        
        Debug.Log("[InventoryManager] Opening stats panel");
        
        // Show stats panel
        if (statsPanel != null)
        {
            statsPanel.SetVisible(true);
        }
        
        isStatsOpen = true;
        
        Debug.Log("[InventoryManager] Stats panel opened");
    }
    
    /// <summary>
    /// Close the stats panel.
    /// </summary>
    public void CloseStatsPanel()
    {
        if (!isStatsOpen)
        {
            return;
        }
        
        Debug.Log("[InventoryManager] Closing stats panel");
        
        // Hide stats panel
        if (statsPanel != null)
        {
            statsPanel.SetVisible(false);
        }
        
        isStatsOpen = false;
        
        Debug.Log("[InventoryManager] Stats panel closed");
    }
    
    /// <summary>
    /// Get CharacterData from the player in the scene
    /// </summary>
    private CharacterData GetPlayerCharacterData()
    {
        PlayerController localPlayer = PlayerController.GetLocalPlayer();
        GameObject player = localPlayer != null ? localPlayer.gameObject : null;
        if (player == null)
        {
            Debug.LogWarning("[InventoryManager] Could not find Player!");
            return null;
        }
        
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            return playerController.GetCurrentCharacterData();
        }
        
        Debug.LogWarning("[InventoryManager] Could not find PlayerController on player!");
        return null;
    }
    
    /// <summary>
    /// Refresh the inventory display (e.g., after picking up an item)
    /// </summary>
    public static void RefreshInventoryDisplay()
    {
        if (instance != null && instance.isInventoryOpen && instance.inventoryUI != null)
        {
            CharacterData characterData = instance.GetPlayerCharacterData();
            if (characterData != null)
            {
                instance.inventoryUI.RefreshInventory(characterData);
                Debug.Log("[InventoryManager] Refreshed inventory display");
            }
        }
    }
}
