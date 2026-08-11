using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a trait tree for a specific character class.
/// Contains all nodes and their connections for visual representation.
/// </summary>
[CreateAssetMenu(fileName = "TraitTree_", menuName = "Traits/Trait Tree")]
public class TraitTreeData : ScriptableObject
{
    [Header("Tree Identity")]
    [Tooltip("Which character this tree belongs to")]
    public string characterName;
    
    [Tooltip("Display name for this tree (e.g., 'Combat', 'Survival', 'Archery')")]
    public string treeName;
    
    [TextArea(2, 3)]
    public string description;
    
    [Header("Tree Nodes")]
    [Tooltip("All nodes in this tree")]
    public List<TraitNode> nodes = new List<TraitNode>();
    
    [Header("Tree Connections")]
    [Tooltip("Visual connections between nodes (LineRenderer-based)")]
    public List<TraitTreeConnection> connections = new List<TraitTreeConnection>();
    
    [Header("Visual Settings")]
    [Tooltip("Spacing between nodes")]
    public float nodeSpacing = 100f;
    
    [Tooltip("Color for active/unlocked nodes")]
    public Color unlockedColor = Color.yellow;
    
    [Tooltip("Color for locked nodes")]
    public Color lockedColor = Color.gray;
    
    [Tooltip("Color for available nodes (requirements met)")]
    public Color availableColor = Color.white;
}

/// <summary>
/// Represents a single node in the trait tree
/// </summary>
[System.Serializable]
public class TraitNode
{
    [Header("Node Identity")]
    [Tooltip("Unique ID for this node")]
    public string nodeID;
    
    [Header("Node Type")]
    [Tooltip("Type of node - Minor, Major, or Keystone")]
    public TraitNodeType nodeType = TraitNodeType.Minor;
    
    [Header("Trait Reference")]
    [Tooltip("The trait this node grants when unlocked")]
    public TraitData traitData;
    
    [Header("Position")]
    [Tooltip("Position in the tree UI (x, y coordinates)")]
    public Vector2 position;
    
    [Header("Connections")]
    [Tooltip("IDs of nodes this connects to (draws paths)")]
    public List<string> connectedNodeIDs = new List<string>();
    
    [Header("Visual Override")]
    [Tooltip("Optional: Override the node background sprite for this specific node")]
    public Sprite customBackgroundSprite;
    
    [Tooltip("Optional: Custom size multiplier for this node")]
    public float sizeMultiplier = 1f;
}

/// <summary>
/// Types of trait nodes
/// </summary>
public enum TraitNodeType
{
    Minor,      // Small circular nodes
    Major,      // Medium hexagonal nodes
    Keystone    // Large distinctive nodes
}
