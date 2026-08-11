using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for ConstructConfig to handle conditional field visibility
/// </summary>
[CustomPropertyDrawer(typeof(ConstructConfig))]
public class ConstructConfigDrawer : PropertyDrawer
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

            // Construct Prefab
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("constructPrefab"), position, yPos);

            // Spawn Settings
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("maxRange"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAtCaster"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAtCasterRadius"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAtMouse"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("holdToPlace"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("ghostAlpha"), position, yPos);
            SerializedProperty use8WayPlacement = property.FindPropertyRelative("use8WayPlacement");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(use8WayPlacement, position, yPos);

            if (use8WayPlacement.boolValue)
            {
                SerializedProperty dirPrefabs = property.FindPropertyRelative("directionalPrefabs");
                string[] dirLabels = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
                dirPrefabs.isExpanded = EditorGUI.Foldout(
                    new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                    dirPrefabs.isExpanded, "Directional Prefabs", true);
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (dirPrefabs.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < 8; i++)
                    {
                        SerializedProperty element = dirPrefabs.GetArrayElementAtIndex(i);
                        EditorGUI.PropertyField(
                            new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight),
                            element, new GUIContent(dirLabels[i]));
                        yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // Construct Limits
            SerializedProperty maxConstructs = property.FindPropertyRelative("maxConstructs");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(maxConstructs, position, yPos);

            if (maxConstructs.intValue > 0)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("limitBehavior"), position, yPos);
            }

            // Construct Lifetime
            SerializedProperty lifetime = property.FindPropertyRelative("lifetime");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(lifetime, position, yPos);

            if (lifetime.floatValue > 0f)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("destroyOnLifetimeEnd"), position, yPos);
            }

            // Spawn Animation
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("activeAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("destructionAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("fireAnimationName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("activationDelay"), position, yPos);

            // Spawn Effects
            SerializedProperty applySpawnKnockback = property.FindPropertyRelative("applySpawnKnockback");
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(applySpawnKnockback, position, yPos);

            if (applySpawnKnockback.boolValue)
            {
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnKnockbackForce"), position, yPos);
                yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("spawnKnockbackRadius"), position, yPos);
            }

            // Collision Settings
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("collisionRadius"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("blockMovement"), position, yPos);

            // Health & Combat
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("maxHealth"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("healthBarPrefab"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("attackSpeed"), position, yPos);

            // Construct Abilities
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("constructAbilities"), position, yPos, true);

            // Legacy
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("prefabName"), position, yPos);
            yPos = DrawerUtil.DrawPropertyAndAdvanceYPos(property.FindPropertyRelative("resourcesPath"), position, yPos);

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

        // Get all property references
        SerializedProperty constructPrefab = property.FindPropertyRelative("constructPrefab");
        SerializedProperty maxRange = property.FindPropertyRelative("maxRange");
        SerializedProperty spawnAtCaster = property.FindPropertyRelative("spawnAtCaster");
        SerializedProperty spawnAtMouse = property.FindPropertyRelative("spawnAtMouse");
        SerializedProperty holdToPlace = property.FindPropertyRelative("holdToPlace");
        SerializedProperty ghostAlpha = property.FindPropertyRelative("ghostAlpha");
        SerializedProperty use8WayPlacement = property.FindPropertyRelative("use8WayPlacement");
        SerializedProperty directionalPrefabs = property.FindPropertyRelative("directionalPrefabs");
        SerializedProperty maxConstructs = property.FindPropertyRelative("maxConstructs");
        SerializedProperty limitBehavior = property.FindPropertyRelative("limitBehavior");
        SerializedProperty lifetime = property.FindPropertyRelative("lifetime");
        SerializedProperty destroyOnLifetimeEnd = property.FindPropertyRelative("destroyOnLifetimeEnd");
        SerializedProperty spawnAnimationName = property.FindPropertyRelative("spawnAnimationName");
        SerializedProperty activeAnimationName = property.FindPropertyRelative("activeAnimationName");
        SerializedProperty destructionAnimationName = property.FindPropertyRelative("destructionAnimationName");
        SerializedProperty fireAnimationName = property.FindPropertyRelative("fireAnimationName");
        
        SerializedProperty activationDelay = property.FindPropertyRelative("activationDelay");
        SerializedProperty applySpawnKnockback = property.FindPropertyRelative("applySpawnKnockback");
        SerializedProperty spawnKnockbackForce = property.FindPropertyRelative("spawnKnockbackForce");
        SerializedProperty spawnKnockbackRadius = property.FindPropertyRelative("spawnKnockbackRadius");
        SerializedProperty collisionRadius = property.FindPropertyRelative("collisionRadius");
        SerializedProperty blockMovement = property.FindPropertyRelative("blockMovement");
        SerializedProperty maxHealth = property.FindPropertyRelative("maxHealth");
        SerializedProperty healthBarPrefab = property.FindPropertyRelative("healthBarPrefab");
        SerializedProperty attackSpeed = property.FindPropertyRelative("attackSpeed");
        SerializedProperty constructAbilities = property.FindPropertyRelative("constructAbilities");
        SerializedProperty prefabName = property.FindPropertyRelative("prefabName");
        SerializedProperty resourcesPath = property.FindPropertyRelative("resourcesPath");

        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout

        // Construct Prefab
        height += EditorGUI.GetPropertyHeight(constructPrefab) + EditorGUIUtility.standardVerticalSpacing;

        // Spawn Settings
        height += EditorGUI.GetPropertyHeight(maxRange) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(spawnAtCaster) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spawnAtCasterRadius")) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(spawnAtMouse) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(holdToPlace) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(ghostAlpha) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(use8WayPlacement) + EditorGUIUtility.standardVerticalSpacing;
        if (use8WayPlacement.boolValue)
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // foldout header
            if (directionalPrefabs.isExpanded)
            {
                for (int i = 0; i < 8; i++)
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }


        // Construct Limits
        height += EditorGUI.GetPropertyHeight(maxConstructs) + EditorGUIUtility.standardVerticalSpacing;
        if (maxConstructs.intValue > 0)
        {
            height += EditorGUI.GetPropertyHeight(limitBehavior) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Construct Lifetime
        height += EditorGUI.GetPropertyHeight(lifetime) + EditorGUIUtility.standardVerticalSpacing;
        if (lifetime.floatValue > 0f)
        {
            height += EditorGUI.GetPropertyHeight(destroyOnLifetimeEnd) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Spawn Animation
        height += EditorGUI.GetPropertyHeight(spawnAnimationName) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(activeAnimationName) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(destructionAnimationName) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(fireAnimationName) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(activationDelay) + EditorGUIUtility.standardVerticalSpacing;

        // Spawn Effects
        height += EditorGUI.GetPropertyHeight(applySpawnKnockback) + EditorGUIUtility.standardVerticalSpacing;
        if (applySpawnKnockback.boolValue)
        {
            height += EditorGUI.GetPropertyHeight(spawnKnockbackForce) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(spawnKnockbackRadius) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Collision Settings
        height += EditorGUI.GetPropertyHeight(collisionRadius) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(blockMovement) + EditorGUIUtility.standardVerticalSpacing;

        // Health & Combat
        height += EditorGUI.GetPropertyHeight(maxHealth) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(healthBarPrefab) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(attackSpeed) + EditorGUIUtility.standardVerticalSpacing;

        // Construct Abilities
        height += EditorGUI.GetPropertyHeight(constructAbilities, true) + EditorGUIUtility.standardVerticalSpacing;

        // Legacy
        height += EditorGUI.GetPropertyHeight(prefabName) + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUI.GetPropertyHeight(resourcesPath) + EditorGUIUtility.standardVerticalSpacing;

        return height;
    }
}
