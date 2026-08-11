using UnityEngine;

/// <summary>
/// Lock point (plug/male connector) on gear pieces that snap into holders (sockets).
/// Like a Lego plug - this connection point snaps into a matching holder.
/// Example: ChestToLegs lock point on chest snaps into LegsToChest holder on legs.
/// </summary>
public class GearLockPoint : MonoBehaviour
{
    [Header("Lock Point Settings")]
    [Tooltip("LEGACY: What type of holder this lock point attaches to")]
    [SerializeField] private GearSlot targetHolderType;
    
    [Tooltip("Connection type for this lock point (plug). Must match the holder's connection type.")]
    [SerializeField] private GearConnectionType connectionType = GearConnectionType.ChestToLegs;
    
    [Header("State")]
    [SerializeField] private GearHolder connectedHolder;
    
    public GearSlot TargetHolderType => targetHolderType;
    public GearConnectionType ConnectionType => connectionType;
    public GearHolder ConnectedHolder => connectedHolder;
    public bool IsConnected => connectedHolder != null;
    
    /// <summary>
    /// Called when this lock point is connected to a holder
    /// </summary>
    public void OnConnected(GearHolder holder)
    {
        connectedHolder = holder;
        Debug.Log($"[GearLockPoint] {name} ({connectionType}) connected to {holder.name} ({holder.ConnectionType})");
    }
    
    /// <summary>
    /// Called when this lock point is disconnected from its holder
    /// </summary>
    public void OnDisconnected()
    {
        Debug.Log($"[GearLockPoint] {name} ({connectionType}) disconnected");
        connectedHolder = null;
    }
    
    /// <summary>
    /// Attach this lock point to a target holder (legacy method, prefer using GearHolder.AttachGear)
    /// </summary>
    public void AttachToHolder(GearHolder holder)
    {
        if (holder == null)
        {
            Debug.LogError("[GearLockPoint] Cannot attach to null holder!");
            return;
        }
        
        Debug.Log($"[GearLockPoint] Attempting to attach. Lock point: {name} ({connectionType}), Holder: {holder.name} ({holder.ConnectionType})");
        
        // Check compatibility using new connection system
        if (!GearConnectionHelper.AreCompatible(connectionType, holder.ConnectionType))
        {
            Debug.LogWarning($"[GearLockPoint] Connection type mismatch! Lock point {connectionType} is not compatible with holder {holder.ConnectionType}");
            
            // Fall back to legacy check
            if (holder.HolderType != targetHolderType)
            {
                Debug.LogError($"[GearLockPoint] Legacy check also failed: {targetHolderType} doesn't match {holder.HolderType}");
                return;
            }
        }
        
        // Get the root gear piece (parent of this lock point)
        Transform gearPieceRoot = transform.parent;
        if (gearPieceRoot == null)
        {
            Debug.LogError("[GearLockPoint] Lock point has no parent! It should be a child of the gear piece.");
            return;
        }
        
        // Parent the entire gear piece to the holder
        gearPieceRoot.SetParent(holder.transform);
        
        // Position the gear piece so the lock point aligns with the holder
        Vector3 offset = gearPieceRoot.position - transform.position;
        gearPieceRoot.position = holder.transform.position + offset;
        
        // Reset local position Z to 0 - Sorting Group handles all sorting
        Vector3 localPos = gearPieceRoot.localPosition;
        localPos.z = 0;
        gearPieceRoot.localPosition = localPos;
        
        // Track connection
        OnConnected(holder);
        
        Debug.Log($"[GearLockPoint] ✓ Attached {gearPieceRoot.name} to {holder.name}");
    }
    
    /// <summary>
    /// Detach from current holder
    /// </summary>
    public void DetachFromHolder()
    {
        Transform gearPieceRoot = transform.parent;
        if (gearPieceRoot != null)
        {
            gearPieceRoot.SetParent(null);
            OnDisconnected();
            Debug.Log($"[GearLockPoint] Detached {gearPieceRoot.name}");
        }
    }
}
