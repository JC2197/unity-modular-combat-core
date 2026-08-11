using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for EnemyActionConfig to show contextual fields based on action type
/// </summary>
[CustomPropertyDrawer(typeof(EnemyActionConfig))]
public class EnemyActionConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Get properties
        SerializedProperty actionType = property.FindPropertyRelative("actionType");
        SerializedProperty minDistance = property.FindPropertyRelative("minDistance");
        SerializedProperty maxDistance = property.FindPropertyRelative("maxDistance");
        SerializedProperty healthPercentThreshold = property.FindPropertyRelative("healthPercentThreshold");
        SerializedProperty movementSpeedMultiplier = property.FindPropertyRelative("movementSpeedMultiplier");
        SerializedProperty movementDuration = property.FindPropertyRelative("movementDuration");
        SerializedProperty strafeDistance = property.FindPropertyRelative("strafeDistance");
        SerializedProperty strafeClockwise = property.FindPropertyRelative("strafeClockwise");
        SerializedProperty patrolRadius = property.FindPropertyRelative("patrolRadius");
        SerializedProperty patrolWaitTime = property.FindPropertyRelative("patrolWaitTime");
        SerializedProperty attackCooldownMin = property.FindPropertyRelative("attackCooldownMin");
        SerializedProperty attackCooldownMax = property.FindPropertyRelative("attackCooldownMax");
        SerializedProperty abilityIndex = property.FindPropertyRelative("abilityIndex");
        
        EnemyActionType currentActionType = (EnemyActionType)actionType.enumValueIndex;
        
        // Calculate height dynamically
        float y = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Show foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, lineHeight), 
            property.isExpanded, $"Action: {currentActionType}", true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            y += lineHeight + spacing;
            
            // Always show action type
            y = DrawerUtil.DrawPropertyAndAdvanceYPos(actionType, position, y);
            
            // Trigger Conditions (hidden for Chase as it uses weaponAbilityRange)
            if (currentActionType != EnemyActionType.Chase)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, lineHeight), "Trigger Conditions", EditorStyles.boldLabel);
                y += lineHeight + spacing;
                
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(minDistance, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(maxDistance, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(healthPercentThreshold, position, y);
            }
            
            // Show contextual fields based on action type
            bool isMovementAction = currentActionType == EnemyActionType.Chase || 
                                   currentActionType == EnemyActionType.Retreat || 
                                   currentActionType == EnemyActionType.Strafe || 
                                   currentActionType == EnemyActionType.Patrol;
            
            // Movement Parameters
            if (isMovementAction)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, lineHeight), "Movement Parameters", EditorStyles.boldLabel);
                y += lineHeight + spacing;
                
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(movementSpeedMultiplier, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(movementDuration, position, y);
            }
            
            // Strafe-specific parameters
            if (currentActionType == EnemyActionType.Strafe)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, lineHeight), "Strafe Parameters", EditorStyles.boldLabel);
                y += lineHeight + spacing;
                
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(strafeDistance, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(strafeClockwise, position, y);
            }
            
            // Patrol-specific parameters
            if (currentActionType == EnemyActionType.Patrol)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, lineHeight), "Patrol Parameters", EditorStyles.boldLabel);
                y += lineHeight + spacing;
                
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(patrolRadius, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(patrolWaitTime, position, y);
            }
            
            // Attack parameters
            if (currentActionType == EnemyActionType.Attack)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, lineHeight), "Attack Parameters", EditorStyles.boldLabel);
                y += lineHeight + spacing;
                
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(attackCooldownMin, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(attackCooldownMax, position, y);
                y = DrawerUtil.DrawPropertyAndAdvanceYPos(abilityIndex, position, y);
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
        
        SerializedProperty actionType = property.FindPropertyRelative("actionType");
        SerializedProperty minDistance = property.FindPropertyRelative("minDistance");
        SerializedProperty maxDistance = property.FindPropertyRelative("maxDistance");
        SerializedProperty healthPercentThreshold = property.FindPropertyRelative("healthPercentThreshold");
        SerializedProperty movementSpeedMultiplier = property.FindPropertyRelative("movementSpeedMultiplier");
        SerializedProperty movementDuration = property.FindPropertyRelative("movementDuration");
        SerializedProperty strafeDistance = property.FindPropertyRelative("strafeDistance");
        SerializedProperty strafeClockwise = property.FindPropertyRelative("strafeClockwise");
        SerializedProperty patrolRadius = property.FindPropertyRelative("patrolRadius");
        SerializedProperty patrolWaitTime = property.FindPropertyRelative("patrolWaitTime");
        SerializedProperty attackCooldownMin = property.FindPropertyRelative("attackCooldownMin");
        SerializedProperty attackCooldownMax = property.FindPropertyRelative("attackCooldownMax");
        SerializedProperty abilityIndex = property.FindPropertyRelative("abilityIndex");
        
        EnemyActionType currentActionType = (EnemyActionType)actionType.enumValueIndex;
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Base: foldout + actionType
        float height = lineHeight + spacing; // Foldout
        height += EditorGUI.GetPropertyHeight(actionType) + spacing;
        
        // Trigger Conditions (hidden for Chase)
        if (currentActionType != EnemyActionType.Chase)
        {
            height += lineHeight + spacing; // "Trigger Conditions" label
            height += EditorGUI.GetPropertyHeight(minDistance) + spacing;
            height += EditorGUI.GetPropertyHeight(maxDistance) + spacing;
            height += EditorGUI.GetPropertyHeight(healthPercentThreshold) + spacing;
        }
        
        // Movement actions
        bool isMovementAction = currentActionType == EnemyActionType.Chase || 
                               currentActionType == EnemyActionType.Retreat || 
                               currentActionType == EnemyActionType.Strafe || 
                               currentActionType == EnemyActionType.Patrol;
        
        if (isMovementAction)
        {
            height += lineHeight + spacing; // "Movement Parameters" label
            height += EditorGUI.GetPropertyHeight(movementSpeedMultiplier) + spacing;
            height += EditorGUI.GetPropertyHeight(movementDuration) + spacing;
        }
        
        // Strafe-specific
        if (currentActionType == EnemyActionType.Strafe)
        {
            height += lineHeight + spacing; // "Strafe Parameters" label
            height += EditorGUI.GetPropertyHeight(strafeDistance) + spacing;
            height += EditorGUI.GetPropertyHeight(strafeClockwise) + spacing;
        }
        
        // Patrol-specific
        if (currentActionType == EnemyActionType.Patrol)
        {
            height += lineHeight + spacing; // "Patrol Parameters" label
            height += EditorGUI.GetPropertyHeight(patrolRadius) + spacing;
            height += EditorGUI.GetPropertyHeight(patrolWaitTime) + spacing;
        }
        
        // Attack parameters
        if (currentActionType == EnemyActionType.Attack)
        {
            height += lineHeight + spacing; // "Attack Parameters" label
            height += EditorGUI.GetPropertyHeight(attackCooldownMin) + spacing;
            height += EditorGUI.GetPropertyHeight(attackCooldownMax) + spacing;
            height += EditorGUI.GetPropertyHeight(abilityIndex) + spacing;
        }
        
        return height;
    }
}
