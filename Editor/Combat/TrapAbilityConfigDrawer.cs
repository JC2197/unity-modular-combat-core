using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for TrapAbilityConfig to handle conditional field visibility
/// </summary>
[CustomPropertyDrawer(typeof(TrapAbilityConfig))]
public class TrapAbilityConfigDrawer : PropertyDrawer
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

            // Basic Settings
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("trapPrefab"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("maxRange"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAtCaster"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAtMouse"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("maxTraps"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("limitBehavior"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("lifetime"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("destroyOnLifetimeEnd"), position, yPos);

            // Trigger Settings
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("triggerRange"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("armingDelay"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("triggerLayers"), position, yPos);
            
            SerializedProperty singleTrigger = property.FindPropertyRelative("singleTrigger");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(singleTrigger, position, yPos);
            if (!singleTrigger.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("retriggerCooldown"), position, yPos);
            }

            // Animation
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("idleAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("triggerAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("destroyDelay"), position, yPos);

            // Triggered Ability - conditional based on abilityType
            SerializedProperty abilityType = property.FindPropertyRelative("abilityType");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(abilityType, position, yPos);

            TrapAbilityType typeValue = (TrapAbilityType)abilityType.enumValueIndex;

            if (typeValue == TrapAbilityType.Area)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("areaConfig"), position, yPos);
            }
            else if (typeValue == TrapAbilityType.Projectile)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("projectileConfig"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("projectileCount"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("projectileSpread"), position, yPos);
            }
            else if (typeValue == TrapAbilityType.Explosion)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("explosionConfig"), position, yPos);
            }

            // Visual Effects
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnEffect"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("triggerEffect"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("showTriggerRadius"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("triggerRadiusColor"), position, yPos);

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

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout

        // Basic Settings
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("trapPrefab")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("maxRange")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spawnAtCaster")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spawnAtMouse")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("maxTraps")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("limitBehavior")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("lifetime")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("destroyOnLifetimeEnd")) + EditorGUIUtility.standardVerticalSpacing;

        // Trigger Settings
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("triggerRange")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("armingDelay")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("triggerLayers")) + EditorGUIUtility.standardVerticalSpacing;
        
        SerializedProperty singleTrigger = property.FindPropertyRelative("singleTrigger");
        height += EditorGUI.GetPropertyHeight(singleTrigger) + EditorGUIUtility.standardVerticalSpacing;
        if (!singleTrigger.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("retriggerCooldown")) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Animation
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("idleAnimationName")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("triggerAnimationName")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("destroyDelay")) + EditorGUIUtility.standardVerticalSpacing;

        // Triggered Ability - conditional based on abilityType
        SerializedProperty abilityType = property.FindPropertyRelative("abilityType");
        height += EditorGUI.GetPropertyHeight(abilityType) + EditorGUIUtility.standardVerticalSpacing;

        TrapAbilityType typeValue = (TrapAbilityType)abilityType.enumValueIndex;

        if (typeValue == TrapAbilityType.Area)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("areaConfig")) + EditorGUIUtility.standardVerticalSpacing;
        }
        else if (typeValue == TrapAbilityType.Projectile)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("projectileConfig")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("projectileCount")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("projectileSpread")) + EditorGUIUtility.standardVerticalSpacing;
        }
        else if (typeValue == TrapAbilityType.Explosion)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("explosionConfig")) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Visual Effects
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spawnEffect")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("triggerEffect")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("showTriggerRadius")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("triggerRadiusColor")) + EditorGUIUtility.standardVerticalSpacing;

        return height;
    }
}
