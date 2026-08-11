using UnityEngine;

/// <summary>
/// Manages player persistence across scenes.
/// Place this on the Player GameObject in CommandScene.
/// Keeps player (with all stats, equipment, abilities) persistent across scene loads.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    private PlayerController playerController;
    
    void Awake()
    {
        Debug.Log($"[PlayerManager] Awake() called at {Time.realtimeSinceStartup:F3}s");
        
        if (Instance == null)
        {
            Instance = this;
            
            // Make this GameObject a root object before DontDestroyOnLoad
            if (transform.parent != null)
            {
                Debug.Log($"[PlayerManager] GameObject is not root, making it root before DontDestroyOnLoad");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            
            Debug.Log($"[PlayerManager] Set as Instance and marked DontDestroyOnLoad");
            
            playerController = GetComponent<PlayerController>();
            
            if (playerController == null)
            {
                Debug.LogError("[PlayerManager] PlayerController not found on this GameObject!");
            }
            else
            {
                Debug.Log($"[PlayerManager] PlayerController found: {playerController.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerManager] Duplicate instance found, destroying");
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Reposition the player to a specific location (called when loading new arena)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        if (playerController != null)
        {
            transform.position = position;
            Debug.Log($"[DEATH-DIAG] [PlayerManager.SetPosition] Player repositioned to {position} — isAlive={playerController.IsAlive}");
        }
        else
        {
            Debug.LogWarning($"[DEATH-DIAG] [PlayerManager.SetPosition] PlayerController is NULL!");
        }
    }
    
    /// <summary>
    /// Get the player controller
    /// </summary>
    public PlayerController GetPlayerController()
    {
        return playerController;
    }
}
