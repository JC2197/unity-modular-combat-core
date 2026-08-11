using UnityEngine;

/// <summary>
/// Represents hand/glove armor pieces.
/// </summary>
public class HandsGearPiece : MonoBehaviour
{
    [Header("Hands Gear Settings")]
    [Tooltip("Config containing stats and modifiers for this hands gear")]
    [SerializeField] private ArmorConfig gearConfig;
    
    public ArmorConfig GearConfig => gearConfig;
}
