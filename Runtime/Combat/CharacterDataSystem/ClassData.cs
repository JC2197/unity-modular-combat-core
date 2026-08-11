using UnityEngine;
using JoeConticello.ModularCombatCore;
/// <summary>
/// Defines a character class that can be shared by multiple character instances.
/// Contains the shared appearance, animations, base stats, and equippable weapons.
/// </summary>
[CreateAssetMenu(fileName = "Class_", menuName = "Characters/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Class Identity")]
    [Tooltip("Internal identifier for this class")]
    public string className;

    [Header("Portrait")]
    [Tooltip("Character portrait sprite")]
    public Sprite characterPortrait;

    [Header("Animation")]
    [Tooltip("Animator controller for this class")]
    public RuntimeAnimatorController animatorController;

    [Header("Movement Animation Names")]
    [Tooltip("Animation name for idle (horizontal)")]
    public string idleAnimation = "Idle";
    [Tooltip("Animation name for idle (up)")]
    public string idleUpAnimation = "IdleUp";
    [Tooltip("Animation name for running (horizontal)")]
    public string runAnimation = "Run";
    [Tooltip("Animation name for running (up)")]
    public string runUpAnimation = "RunUp";

    [Header("Animation Direction Mapping")]
    [Tooltip("Direction for idle animation")]
    public WeaponSortingManager.Direction idleDirection = WeaponSortingManager.Direction.SouthEast;
    [Tooltip("Direction for idle up animation")]
    public WeaponSortingManager.Direction idleUpDirection = WeaponSortingManager.Direction.NorthEast;
    [Tooltip("Direction for run animation")]
    public WeaponSortingManager.Direction runDirection = WeaponSortingManager.Direction.SouthEast;
    [Tooltip("Direction for run up animation")]
    public WeaponSortingManager.Direction runUpDirection = WeaponSortingManager.Direction.NorthEast;

    [Tooltip("Does diagonal down movement use the same animation as horizontal run?")]
    public bool diagonalDownUsesRunAnimation = true;

    [Header("Available Weapons")]
    [Tooltip("List of weapon configs this class can equip")]
    public WeaponConfig[] availableWeapons;

    [Header("Base Stats")]
    [Tooltip("Starting stat values for Level 1 characters of this class. Use the context menu 'Initialize Base Stats from Database' if empty.")]
    public StatContainer baseStatContainer = new StatContainer();

    [ContextMenu("Initialize Base Stats from Database")]
    private void InitializeBaseStatsFromDatabase()
    {
        baseStatContainer.InitializeFromDatabase();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[ClassData] Initialized baseStatContainer for '{className}' from StatTypeDatabase.");
#endif
    }
}
