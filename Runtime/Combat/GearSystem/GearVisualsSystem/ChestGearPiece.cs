using UnityEngine;

/// <summary>
/// Represents a chest/body armor piece.
/// Stores Y offsets that PlayerGearManager uses to position the permanent
/// ChestHolder and HeadHolder transforms when this piece is equipped.
/// </summary>
public class ChestGearPiece : MonoBehaviour
{
    [Header("Chest Gear Settings")]
    [Tooltip("Config containing stats and modifiers for this chest gear")]
    [SerializeField] private ArmorConfig gearConfig;
    
    [Header("Holder Y Offsets")]
    [Tooltip("Y offset applied to the ChestHolder when this piece is equipped")]
    [SerializeField] private float chestHolderYOffset;
    
    [Tooltip("Y offset applied to the HeadHolder when this piece is equipped")]
    [SerializeField] private float headHolderYOffset;
    
    public ArmorConfig GearConfig => gearConfig;
    public float ChestHolderYOffset => chestHolderYOffset;
    public float HeadHolderYOffset => headHolderYOffset;
}
