using UnityEngine;

/// <summary>
/// Holder component (socket/female connector) that can have gear pieces attached to it.
/// Like a Lego socket - specific lock points (plugs) snap into specific holders (sockets).
/// Example: LegsToChest holder on legs receives ChestToLegs lock point from chest.
/// </summary>
public class GearHolder : MonoBehaviour
{
    [Header("Holder Settings")]
    [Tooltip("LEGACY: The type of gear that can attach to this holder")]
    [SerializeField] protected GearSlot holderType;
    
    [Tooltip("Connection type for this holder (socket). Must match the lock point's connection type.")]
    [SerializeField] protected GearConnectionType connectionType;
    
    [Tooltip("Current gear piece attached to this holder")]
    protected GameObject attachedGear;
    
    [Tooltip("The lock point currently connected to this holder")]
    protected GearLockPoint connectedLockPoint;
    
    public GearSlot HolderType => holderType;
    public GearConnectionType ConnectionType => connectionType;
    public bool HasGear => attachedGear != null;
    public GameObject AttachedGear => attachedGear;
    public GearLockPoint ConnectedLockPoint => connectedLockPoint;
    
    /// <summary>
    /// Attach a gear piece to this holder using its lock point for alignment.
    /// The lock point on the gear will "snap" into this holder.
    /// </summary>
    public virtual void AttachGear(GameObject gearPrefab)
    {
        if (attachedGear != null)
        {
            Debug.LogWarning($"[{GetType().Name}] Holder already has gear attached, detaching old gear first");
            DetachGear();
        }
        
        if (gearPrefab == null)
        {
            Debug.LogError($"[{GetType().Name}] Cannot attach null gear prefab!");
            return;
        }
        
        // Instantiate gear as child of this holder
        attachedGear = Instantiate(gearPrefab, transform);
        attachedGear.transform.localRotation = Quaternion.identity;
        attachedGear.transform.localScale = Vector3.one;
        
        // Find the lock point on the gear for proper alignment
        GearLockPoint lockPoint = attachedGear.GetComponentInChildren<GearLockPoint>();
        if (lockPoint != null)
        {
            // Verify connection compatibility
            if (!GearConnectionHelper.AreCompatible(lockPoint.ConnectionType, connectionType))
            {
                Debug.LogWarning($"[{GetType().Name}] Lock point type {lockPoint.ConnectionType} may not be compatible with holder type {connectionType}");
            }
            
            // Calculate offset so lock point aligns with holder origin
            Vector3 lockPointLocalPos = lockPoint.transform.localPosition;
            attachedGear.transform.localPosition = -lockPointLocalPos;
            
            connectedLockPoint = lockPoint;
            lockPoint.OnConnected(this);
            
            Debug.Log($"[{GetType().Name}] Attached gear: {gearPrefab.name} with lock point alignment (offset: {-lockPointLocalPos})");
        }
        else
        {
            attachedGear.transform.localPosition = Vector3.zero;
            Debug.Log($"[{GetType().Name}] Attached gear: {gearPrefab.name} (no lock point found)");
        }
        
        OnGearAttached(attachedGear);
    }
    
    /// <summary>
    /// Detach the current gear piece
    /// </summary>
    public virtual void DetachGear()
    {
        if (attachedGear == null)
        {
            Debug.LogWarning($"[{GetType().Name}] No gear to detach");
            return;
        }
        
        if (connectedLockPoint != null)
        {
            connectedLockPoint.OnDisconnected();
            connectedLockPoint = null;
        }
        
        OnGearDetached(attachedGear);
        
        Destroy(attachedGear);
        attachedGear = null;
        
        Debug.Log($"[{GetType().Name}] Detached gear");
    }
    
    /// <summary>
    /// Called when gear is attached, for subclasses to override
    /// </summary>
    protected virtual void OnGearAttached(GameObject gear) { }
    
    /// <summary>
    /// Called when gear is detached, for subclasses to override
    /// </summary>
    protected virtual void OnGearDetached(GameObject gear) { }
}
