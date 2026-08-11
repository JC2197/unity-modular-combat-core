using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GearProgressionDatabase))]
public class GearProgressionDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty weaponEntries = serializedObject.FindProperty("weaponEntries");
        SerializedProperty armorEntries = serializedObject.FindProperty("armorEntries");

        EditorGUILayout.LabelField("Gear Progression Database", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Entries are labeled by class so you can quickly scan MainHand / OffHand / TwoHanded and armor class + slot.", MessageType.Info);

        DrawWeaponEntries(weaponEntries);
        EditorGUILayout.Space(8);
        DrawArmorEntries(armorEntries);

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawWeaponEntries(SerializedProperty list)
    {
        if (list == null)
            return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Weapon Entries ({list.arraySize})", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        EditorGUI.indentLevel++;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty weaponClass = element.FindPropertyRelative("weaponClass");

            string className = weaponClass != null
                ? weaponClass.enumDisplayNames[weaponClass.enumValueIndex]
                : "Unknown";
            SerializedProperty advLevelProp = element.FindPropertyRelative("advancementLevel");
            string advLevel = advLevelProp != null ? advLevelProp.intValue.ToString() : "1";

            element.isExpanded = EditorGUILayout.Foldout(
                element.isExpanded,
                $"{className} {advLevel}",
                true);

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Weapon Entry"))
        {
            list.arraySize++;
        }
        if (GUILayout.Button("Remove Last") && list.arraySize > 0)
        {
            list.arraySize--;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private static void DrawArmorEntries(SerializedProperty list)
    {
        if (list == null)
            return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Armor Entries ({list.arraySize})", EditorStyles.boldLabel);

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty armorClass = element.FindPropertyRelative("armorClass");
            SerializedProperty armorSlot = element.FindPropertyRelative("armorSlot");

            string className = armorClass != null
                ? armorClass.enumDisplayNames[armorClass.enumValueIndex]
                : "Unknown";
            string slotName = armorSlot != null
                ? armorSlot.enumDisplayNames[armorSlot.enumValueIndex]
                : "Any";
            SerializedProperty advLevelProp = element.FindPropertyRelative("advancementLevel");
            string advLevel = advLevelProp != null ? advLevelProp.intValue.ToString() : "1";

            element.isExpanded = EditorGUILayout.Foldout(
                element.isExpanded,
                $"{className} {slotName} {advLevel}",
                true);

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(element, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Armor Entry"))
        {
            list.arraySize++;
        }
        if (GUILayout.Button("Remove Last") && list.arraySize > 0)
        {
            list.arraySize--;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}
