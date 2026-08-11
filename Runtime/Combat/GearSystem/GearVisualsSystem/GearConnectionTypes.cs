using UnityEngine;

/// <summary>
/// Defines the type of gear connection point.
/// Used by both GearHolder (female socket) and GearLockPoint (male plug).
/// Like Lego pieces - specific plugs connect to specific sockets.
/// </summary>
public enum GearConnectionType
{
    /// <summary>Socket on Legs where Chest's lockpoint connects</summary>
    LegsToChest,
    
    /// <summary>Plug on Chest that connects to Legs' socket</summary>
    ChestToLegs,
    
    /// <summary>Socket on Chest where Head's lockpoint connects</summary>
    ChestToHead,
    
    /// <summary>Plug on Head that connects to Chest's socket</summary>
    HeadToChest,
    
    /// <summary>Socket on Chest where Weapon's lockpoint connects</summary>
    ChestToWeapon,
    
    /// <summary>Plug on Weapon that connects to Chest's socket</summary>
    WeaponToChest,
    
    /// <summary>Socket on Chest where Backpack's lockpoint connects</summary>
    ChestToBackpack,
    
    /// <summary>Plug on Backpack that connects to Chest's socket</summary>
    BackpackToChest,
    
    /// <summary>Socket on Legs where Hands' lockpoint connects</summary>
    LegsToHands,
    
    /// <summary>Plug on Hands that connects to Legs' socket</summary>
    HandsToLegs,
    
    /// <summary>Socket on Weapon where Left Hand positions (no lockpoint needed)</summary>
    WeaponToLeftHand,
    
    /// <summary>Socket on Weapon where Right Hand positions (no lockpoint needed)</summary>
    WeaponToRightHand
}

/// <summary>
/// Helper class to determine compatible connection pairs
/// </summary>
public static class GearConnectionHelper
{
    /// <summary>
    /// Check if a lock point type is compatible with a holder type.
    /// Lock points (male plugs) fit into holders (female sockets).
    /// </summary>
    public static bool AreCompatible(GearConnectionType lockPointType, GearConnectionType holderType)
    {
        return (lockPointType, holderType) switch
        {
            (GearConnectionType.ChestToLegs, GearConnectionType.LegsToChest) => true,
            (GearConnectionType.HeadToChest, GearConnectionType.ChestToHead) => true,
            (GearConnectionType.WeaponToChest, GearConnectionType.ChestToWeapon) => true,
            (GearConnectionType.BackpackToChest, GearConnectionType.ChestToBackpack) => true,
            (GearConnectionType.HandsToLegs, GearConnectionType.LegsToHands) => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Get the expected holder type for a given lock point type
    /// </summary>
    public static GearConnectionType GetExpectedHolder(GearConnectionType lockPointType)
    {
        return lockPointType switch
        {
            GearConnectionType.ChestToLegs => GearConnectionType.LegsToChest,
            GearConnectionType.HeadToChest => GearConnectionType.ChestToHead,
            GearConnectionType.WeaponToChest => GearConnectionType.ChestToWeapon,
            GearConnectionType.BackpackToChest => GearConnectionType.ChestToBackpack,
            GearConnectionType.HandsToLegs => GearConnectionType.LegsToHands,
            _ => lockPointType
        };
    }
    
    /// <summary>
    /// Get the expected lock point type for a given holder type
    /// </summary>
    public static GearConnectionType GetExpectedLockPoint(GearConnectionType holderType)
    {
        return holderType switch
        {
            GearConnectionType.LegsToChest => GearConnectionType.ChestToLegs,
            GearConnectionType.ChestToHead => GearConnectionType.HeadToChest,
            GearConnectionType.ChestToWeapon => GearConnectionType.WeaponToChest,
            GearConnectionType.ChestToBackpack => GearConnectionType.BackpackToChest,
            GearConnectionType.LegsToHands => GearConnectionType.HandsToLegs,
            _ => holderType
        };
    }
    
    /// <summary>
    /// Returns true if this connection type is a holder (socket/female)
    /// </summary>
    public static bool IsHolder(GearConnectionType type)
    {
        return type switch
        {
            GearConnectionType.LegsToChest => true,
            GearConnectionType.ChestToHead => true,
            GearConnectionType.ChestToWeapon => true,
            GearConnectionType.ChestToBackpack => true,
            GearConnectionType.LegsToHands => true,
            GearConnectionType.WeaponToLeftHand => true,
            GearConnectionType.WeaponToRightHand => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Returns true if this connection type is a lock point (plug/male)
    /// </summary>
    public static bool IsLockPoint(GearConnectionType type)
    {
        return type switch
        {
            GearConnectionType.ChestToLegs => true,
            GearConnectionType.HeadToChest => true,
            GearConnectionType.WeaponToChest => true,
            GearConnectionType.BackpackToChest => true,
            GearConnectionType.HandsToLegs => true,
            _ => false
        };
    }
}
