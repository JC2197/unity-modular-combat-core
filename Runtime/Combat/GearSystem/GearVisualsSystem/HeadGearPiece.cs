using UnityEngine;

/// <summary>
/// Represents a head/helmet armor piece.
/// </summary>
public class HeadGearPiece : MonoBehaviour
{
    [Header("Head Gear Settings")]
    [Tooltip("Config containing stats and modifiers for this head gear")]
    [SerializeField] private ArmorConfig gearConfig;
    
    public ArmorConfig GearConfig => gearConfig;
}
