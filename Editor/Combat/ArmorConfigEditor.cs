using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for ArmorConfig
/// </summary>
[CustomEditor(typeof(ArmorConfig), true)]
public class ArmorConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var advancementLevelProp = serializedObject.FindProperty("advancementLevel");
        var armorSlotProp = serializedObject.FindProperty("armorSlot");
        var gearNameProp = serializedObject.FindProperty("gearName");
        var grantedTraitProp = serializedObject.FindProperty("grantedTrait");
        var armorClassProp = serializedObject.FindProperty("armorClass");
        var rarityTierProp = serializedObject.FindProperty("rarityTier");
        var animatorOverrideProp = serializedObject.FindProperty("animatorOverride");
        var inventorySpriteProp = serializedObject.FindProperty("inventorySprite");
        var worldSpriteProp = serializedObject.FindProperty("worldSprite");
        var baseStatsProp = serializedObject.FindProperty("baseStatRanges");
        var modifiersProp = serializedObject.FindProperty("modifiers");
        var movementSpeedModifierProp = serializedObject.FindProperty("movementSpeedModifier");
        
        EditorGUILayout.PropertyField(advancementLevelProp);
        EditorGUILayout.PropertyField(gearNameProp);
        EditorGUILayout.PropertyField(grantedTraitProp);
        EditorGUILayout.PropertyField(armorClassProp);
        EditorGUILayout.PropertyField(armorSlotProp);
        EditorGUILayout.PropertyField(rarityTierProp);

        // Show only the prefab field for the selected slot
        ArmorSlot slot = (ArmorSlot)armorSlotProp.enumValueIndex;
        switch (slot)
        {
            case ArmorSlot.Head:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("headGearPrefab"));
                break;
            case ArmorSlot.Chest:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chestGearPrefab"));
                break;
            case ArmorSlot.Legs:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("legGearPrefab"));
                break;
            case ArmorSlot.Hands:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("handsGearPrefab"));
                break;
            case ArmorSlot.Backpack:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backpackGearPrefab"));
                break;
        }

        EditorGUILayout.PropertyField(animatorOverrideProp);
        EditorGUILayout.PropertyField(inventorySpriteProp);
        EditorGUILayout.PropertyField(worldSpriteProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Configure armor base stats as ranges (min/max). Each stat rolls a value when gear is generated.",
            MessageType.Info);
        EditorGUILayout.PropertyField(baseStatsProp, true);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Additional Modifiers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(modifiersProp, true);
        EditorGUILayout.PropertyField(movementSpeedModifierProp);

        serializedObject.ApplyModifiedProperties();
    }
}
