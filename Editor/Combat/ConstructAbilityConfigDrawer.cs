using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for ConstructAbilityConfig to handle conditional field visibility
/// </summary>
[CustomPropertyDrawer(typeof(ConstructAbilityConfig))]
public class ConstructAbilityConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Ability Type
            SerializedProperty abilityType = property.FindPropertyRelative("abilityType");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(abilityType, position, yPos);

            // Show config based on ability type
            ConstructAbilityConfig.AbilityType type = (ConstructAbilityConfig.AbilityType)abilityType.enumValueIndex;

            switch (type)
            {
                case ConstructAbilityConfig.AbilityType.Area:
                    yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("areaConfig"), position, yPos, true);
                    break;

                case ConstructAbilityConfig.AbilityType.Projectile:
                    yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("projectileConfig"), position, yPos, true);
                    break;

                case ConstructAbilityConfig.AbilityType.Beam:
                    // yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("beamConfig"), position, yPos, true);
                    EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Beam abilities not yet implemented");
                    yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ConstructAbilityConfig.AbilityType.Channel:
                    // yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("channelConfig"), position, yPos, true);
                    EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Channel abilities not yet implemented");
                    yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        SerializedProperty abilityType = property.FindPropertyRelative("abilityType");
        ConstructAbilityConfig.AbilityType type = (ConstructAbilityConfig.AbilityType)abilityType.enumValueIndex;

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout
        height += EditorGUI.GetPropertyHeight(abilityType) + EditorGUIUtility.standardVerticalSpacing; // Ability Type

        // Add height for visible config
        switch (type)
        {
            case ConstructAbilityConfig.AbilityType.Area:
                SerializedProperty areaConfig = property.FindPropertyRelative("areaConfig");
                height += EditorGUI.GetPropertyHeight(areaConfig, true) + EditorGUIUtility.standardVerticalSpacing;
                break;

            case ConstructAbilityConfig.AbilityType.Projectile:
                SerializedProperty projectileConfig = property.FindPropertyRelative("projectileConfig");
                height += EditorGUI.GetPropertyHeight(projectileConfig, true) + EditorGUIUtility.standardVerticalSpacing;
                break;

            case ConstructAbilityConfig.AbilityType.Beam:
            case ConstructAbilityConfig.AbilityType.Channel:
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // "Not yet implemented" message
                break;
        }

        return height;
    }
}
