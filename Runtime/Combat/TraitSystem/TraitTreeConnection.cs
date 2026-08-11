using UnityEngine;

/// <summary>
/// Represents a visual connection between trait tree nodes.
/// Stores the connection type and placement information.
/// </summary>
[System.Serializable]
public class TraitTreeConnection
{
    [Header("Connection Identity")]
    [Tooltip("Unique ID for this connection")]
    public string connectionID;
    
    [Header("Visual Settings")]
    [Tooltip("Scale multiplier for this connection")]
    public float scale = 1f;
    
    [Header("Connected Nodes")]
    [Tooltip("Node IDs this connection comes FROM (prerequisites)")]
    public string[] fromNodeIDs = new string[0];
    
    [Tooltip("Node IDs this connection goes TO (dependents)")]
    public string[] toNodeIDs = new string[0];
}
