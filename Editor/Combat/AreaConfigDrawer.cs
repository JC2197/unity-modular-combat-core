using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for AreaConfig to handle conditional field visibility
/// </summary>
[CustomPropertyDrawer(typeof(AreaConfig))]
public class AreaConfigDrawer : PropertyDrawer
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

            // Hitbox (prefab, scale, layers, damage, weapon damage, knockback, pull, on-hit effects, life steal, feedback)
            SerializedProperty hitbox = property.FindPropertyRelative("hitbox");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(hitbox, true)), hitbox, true);
            yPos += EditorGUI.GetPropertyHeight(hitbox, true) + EditorGUIUtility.standardVerticalSpacing;

            // Area Settings
            SerializedProperty isPointBlank = property.FindPropertyRelative("isPointBlank");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(isPointBlank, position, yPos);
            if (!isPointBlank.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("range"), position, yPos);
            }

            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("areaCount"), position, yPos);
            
            SerializedProperty isAura = property.FindPropertyRelative("isAura");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(isAura, position, yPos);
            if (isAura.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("followCaster"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("auraDelay"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("duration"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("enabled"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("offset"), position, yPos);
            }
            else
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("duration"), position, yPos);
            }

            // Damage
            if (isAura.boolValue)
            {
                yPos = DrawPropertyWithLabel(property.FindPropertyRelative("damageInterval"), position, yPos, "Tick Rate");
                yPos = DrawPropertyWithLabel(property.FindPropertyRelative("hasDamageTick"), position, yPos, "Tick Particles");
            }
            else
            {
                SerializedProperty hasDamageTick = property.FindPropertyRelative("hasDamageTick");
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(hasDamageTick, position, yPos);
                if (hasDamageTick.boolValue)
                {
                    yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("damageInterval"), position, yPos);
                }
            }
            
            SerializedProperty dealsDamageOverTime = property.FindPropertyRelative("dealsDamageOverTime");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(dealsDamageOverTime, position, yPos);
            if (dealsDamageOverTime.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("damagePerSecond"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("dotInterval"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("dotDuration"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("dotParticleEffectPrefab"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("startParticlesFromFeet"), position, yPos);
            }

            SerializedProperty hasFadeIn = property.FindPropertyRelative("hasFadeIn");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(hasFadeIn, position, yPos);
            if (hasFadeIn.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("fadeInDuration"), position, yPos);
            }

            // Effects
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnSound"), position, yPos);

            // Light
            SerializedProperty hasLight = property.FindPropertyRelative("hasLight");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(hasLight, position, yPos);
            if (hasLight.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("lightColor"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("lightIntensity"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("lightRadius"), position, yPos);
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

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout

        // Hitbox
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("hitbox"), true) + EditorGUIUtility.standardVerticalSpacing;

        // Area Settings
        SerializedProperty isPointBlank = property.FindPropertyRelative("isPointBlank");
        height += EditorGUI.GetPropertyHeight(isPointBlank) + EditorGUIUtility.standardVerticalSpacing;
        if (!isPointBlank.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("range")) + EditorGUIUtility.standardVerticalSpacing;
        }

        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("areaCount")) + EditorGUIUtility.standardVerticalSpacing;
        
        SerializedProperty isAura = property.FindPropertyRelative("isAura");
        height += EditorGUI.GetPropertyHeight(isAura) + EditorGUIUtility.standardVerticalSpacing;
        if (isAura.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("followCaster")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("auraDelay")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("duration")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("enabled")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("offset")) + EditorGUIUtility.standardVerticalSpacing;
        }
        else
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("duration")) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Damage
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("hasDamageTick")) + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty hasDamagetick2 = property.FindPropertyRelative("hasDamageTick");
        bool auraMode2 = property.FindPropertyRelative("isAura").boolValue;
        if (auraMode2)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("damageInterval")) + EditorGUIUtility.standardVerticalSpacing;
        }
        else if (hasDamagetick2.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("damageInterval")) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        SerializedProperty dealsDamageOverTime = property.FindPropertyRelative("dealsDamageOverTime");
        height += EditorGUI.GetPropertyHeight(dealsDamageOverTime) + EditorGUIUtility.standardVerticalSpacing;
        if (dealsDamageOverTime.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("damagePerSecond")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dotInterval")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dotDuration")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dotParticleEffectPrefab")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("startParticlesFromFeet")) + EditorGUIUtility.standardVerticalSpacing;
        }

        SerializedProperty hasFadeIn = property.FindPropertyRelative("hasFadeIn");
        height += EditorGUI.GetPropertyHeight(hasFadeIn) + EditorGUIUtility.standardVerticalSpacing;
        if (hasFadeIn.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("fadeInDuration")) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Effects
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spawnSound")) + EditorGUIUtility.standardVerticalSpacing;

        // Light
        SerializedProperty hasLight = property.FindPropertyRelative("hasLight");
        height += EditorGUI.GetPropertyHeight(hasLight) + EditorGUIUtility.standardVerticalSpacing;
        if (hasLight.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("lightColor")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("lightIntensity")) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("lightRadius")) + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    private static float DrawPropertyWithLabel(SerializedProperty property, Rect position, float yPos, string label)
    {
        float propertyHeight = EditorGUI.GetPropertyHeight(property, true);
        EditorGUI.PropertyField(
            new Rect(position.x, yPos, position.width, propertyHeight),
            property,
            new GUIContent(label),
            true);
        return yPos + propertyHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
