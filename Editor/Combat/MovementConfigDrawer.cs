using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for MovementConfig to keep movement-specific conditional UI
/// separate from AbilityDataConfig inspector.
/// </summary>
[CustomPropertyDrawer(typeof(MovementConfig))]
public class MovementConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            label,
            true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty movementType = property.FindPropertyRelative("movementType");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(movementType, position, yPos);

            yPos += 2f;
            EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Direction", EditorStyles.boldLabel);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("towardMouse"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("awayFromMouse"), position, yPos);

            yPos += 2f;
            EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Dash / Evade", EditorStyles.boldLabel);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("isDashing"), position, yPos);

            yPos += 2f;
            yPos = DrawMovementTypeFields(property, movementType, position, yPos);

            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("activateAfterPrecast"), position, yPos);

            yPos += 2f;
            EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Pass-Through Damage", EditorStyles.boldLabel);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty passThruDamage = property.FindPropertyRelative("passThruDamage");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(passThruDamage, position, yPos);
            if (passThruDamage.boolValue)
            {
                EditorGUI.indentLevel++;
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("passthruDamageAmount"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("damageTypeName"), position, yPos);
                EditorGUI.indentLevel--;
            }

            if ((MovementType)movementType.enumValueIndex == MovementType.Teleport)
            {
                yPos += 2f;
                EditorGUI.LabelField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), "Teleport Visuals", EditorStyles.boldLabel);
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("teleportAnimationPrefab"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("disappearDuringTeleport"), position, yPos);
            }

            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("dashSound"), position, yPos);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            return height;

        height += EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty movementType = property.FindPropertyRelative("movementType");
        height += EditorGUI.GetPropertyHeight(movementType) + EditorGUIUtility.standardVerticalSpacing;

        // Direction label + fields
        height += 2f + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("towardMouse")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("awayFromMouse")) + EditorGUIUtility.standardVerticalSpacing;

        // Dash/Evade label + field
        height += 2f + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("isDashing")) + EditorGUIUtility.standardVerticalSpacing;

        // Movement type-specific fields
        height += 2f;
        switch ((MovementType)movementType.enumValueIndex)
        {
            case MovementType.Force:
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("forceAmount")) + EditorGUIUtility.standardVerticalSpacing;
                break;
            case MovementType.DistanceOverTime:
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("distance")) + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("duration")) + EditorGUIUtility.standardVerticalSpacing;
                break;
            case MovementType.SpeedOverTime:
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("speed")) + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("duration")) + EditorGUIUtility.standardVerticalSpacing;
                break;
            case MovementType.Teleport:
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("distance")) + EditorGUIUtility.standardVerticalSpacing;
                break;
        }

        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("activateAfterPrecast")) + EditorGUIUtility.standardVerticalSpacing;

        // Pass-through label + toggle + optional fields
        height += 2f + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty passThruDamage = property.FindPropertyRelative("passThruDamage");
        height += EditorGUI.GetPropertyHeight(passThruDamage) + EditorGUIUtility.standardVerticalSpacing;
        if (passThruDamage.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("passthruDamageAmount")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("damageTypeName")) + EditorGUIUtility.standardVerticalSpacing;
        }

        if ((MovementType)movementType.enumValueIndex == MovementType.Teleport)
        {
            height += 2f + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("teleportAnimationPrefab")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("disappearDuringTeleport")) + EditorGUIUtility.standardVerticalSpacing;
        }

        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dashSound")) + EditorGUIUtility.standardVerticalSpacing;

        return height;
    }

    private static float DrawMovementTypeFields(SerializedProperty property, SerializedProperty movementType, Rect position, float yPos)
    {
        switch ((MovementType)movementType.enumValueIndex)
        {
            case MovementType.Force:
                return DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("forceAmount"), position, yPos);
            case MovementType.DistanceOverTime:
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("distance"), position, yPos);
                return DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("duration"), position, yPos);
            case MovementType.SpeedOverTime:
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("speed"), position, yPos);
                return DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("duration"), position, yPos);
            case MovementType.Teleport:
                return DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("distance"), position, yPos);
            default:
                return yPos;
        }
    }
}
