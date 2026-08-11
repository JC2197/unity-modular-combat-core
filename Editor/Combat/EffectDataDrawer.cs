using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for EffectData to handle conditional effect configuration visibility.
/// Updated to work with ScriptableObject-based effect system.
/// </summary>
[CustomPropertyDrawer(typeof(EffectData))]
public class EffectDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            // Root
            SerializedProperty canRoot = property.FindPropertyRelative("canRoot");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canRoot)), canRoot);
            yPos += EditorGUI.GetPropertyHeight(canRoot) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canRoot.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty rootEffect = property.FindPropertyRelative("rootEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(rootEffect)), rootEffect, new GUIContent("Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(rootEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty rootDuration = property.FindPropertyRelative("rootDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(rootDuration)), rootDuration, new GUIContent("Duration Override"));
                yPos += EditorGUI.GetPropertyHeight(rootDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty rootApplicationChance = property.FindPropertyRelative("rootApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(rootApplicationChance)), rootApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(rootApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }
            
            // Slow
            SerializedProperty canSlow = property.FindPropertyRelative("canSlow");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canSlow)), canSlow);
            yPos += EditorGUI.GetPropertyHeight(canSlow) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canSlow.boolValue)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty slowEffect = property.FindPropertyRelative("slowEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(slowEffect)), slowEffect, new GUIContent("Slow Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(slowEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty slowDuration = property.FindPropertyRelative("slowDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(slowDuration)), slowDuration, new GUIContent("Slow Duration"));
                yPos += EditorGUI.GetPropertyHeight(slowDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty slowApplicationChance = property.FindPropertyRelative("slowApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(slowApplicationChance)), slowApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(slowApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                
                EditorGUI.indentLevel--;
            }
            
            // Stun
            SerializedProperty canStun = property.FindPropertyRelative("canStun");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canStun)), canStun);
            yPos += EditorGUI.GetPropertyHeight(canStun) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canStun.boolValue)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty stunEffect = property.FindPropertyRelative("stunEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(stunEffect)), stunEffect, new GUIContent("Stun Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(stunEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty stunDuration = property.FindPropertyRelative("stunDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(stunDuration)), stunDuration, new GUIContent("Stun Duration"));
                yPos += EditorGUI.GetPropertyHeight(stunDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty stunApplicationChance = property.FindPropertyRelative("stunApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(stunApplicationChance)), stunApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(stunApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                
                EditorGUI.indentLevel--;
            }
            
            // Bleed
            SerializedProperty canBleed = property.FindPropertyRelative("canBleed");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canBleed)), canBleed);
            yPos += EditorGUI.GetPropertyHeight(canBleed) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canBleed.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty bleedEffect = property.FindPropertyRelative("bleedEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(bleedEffect)), bleedEffect, new GUIContent("Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(bleedEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty bleedDamage = property.FindPropertyRelative("bleedDamage");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(bleedDamage)), bleedDamage, new GUIContent("Damage Per Tick"));
                yPos += EditorGUI.GetPropertyHeight(bleedDamage) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty bleedDuration = property.FindPropertyRelative("bleedDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(bleedDuration)), bleedDuration, new GUIContent("Duration"));
                yPos += EditorGUI.GetPropertyHeight(bleedDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty bleedApplicationChance = property.FindPropertyRelative("bleedApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(bleedApplicationChance)), bleedApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(bleedApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }
            
            // Burn
            SerializedProperty canBurn = property.FindPropertyRelative("canBurn");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canBurn)), canBurn);
            yPos += EditorGUI.GetPropertyHeight(canBurn) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canBurn.boolValue)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty burnEffect = property.FindPropertyRelative("burnEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(burnEffect)), burnEffect, new GUIContent("Burning Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(burnEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty burnDamage = property.FindPropertyRelative("burnDamage");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(burnDamage)), burnDamage, new GUIContent("Burn Damage"));
                yPos += EditorGUI.GetPropertyHeight(burnDamage) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty burnDuration = property.FindPropertyRelative("burnDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(burnDuration)), burnDuration, new GUIContent("Burn Duration"));
                yPos += EditorGUI.GetPropertyHeight(burnDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty burnApplicationChance = property.FindPropertyRelative("burnApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(burnApplicationChance)), burnApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(burnApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                
                EditorGUI.indentLevel--;
            }
            
            // Poison
            SerializedProperty canPoison = property.FindPropertyRelative("canPoison");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canPoison)), canPoison);
            yPos += EditorGUI.GetPropertyHeight(canPoison) + EditorGUIUtility.standardVerticalSpacing;
            
            if (canPoison.boolValue)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty poisonEffect = property.FindPropertyRelative("poisonEffect");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(poisonEffect)), poisonEffect, new GUIContent("Poison Effect Asset"));
                yPos += EditorGUI.GetPropertyHeight(poisonEffect) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty poisonDamage = property.FindPropertyRelative("poisonDamage");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(poisonDamage)), poisonDamage, new GUIContent("Poison Damage"));
                yPos += EditorGUI.GetPropertyHeight(poisonDamage) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty poisonDuration = property.FindPropertyRelative("poisonDuration");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(poisonDuration)), poisonDuration, new GUIContent("Poison Duration"));
                yPos += EditorGUI.GetPropertyHeight(poisonDuration) + EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty poisonApplicationChance = property.FindPropertyRelative("poisonApplicationChance");
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(poisonApplicationChance)), poisonApplicationChance, new GUIContent("Application Chance"));
                yPos += EditorGUI.GetPropertyHeight(poisonApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
                
                EditorGUI.indentLevel--;
            }

            // Stat Buffs
            SerializedProperty canApplyStatBuffs = property.FindPropertyRelative("canApplyStatBuffs");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canApplyStatBuffs)), canApplyStatBuffs);
            yPos += EditorGUI.GetPropertyHeight(canApplyStatBuffs) + EditorGUIUtility.standardVerticalSpacing;

            if (canApplyStatBuffs.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty statBuffApplications = property.FindPropertyRelative("statBuffApplications");
                float statBuffHeight = EditorGUI.GetPropertyHeight(statBuffApplications, true);
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, statBuffHeight), statBuffApplications, true);
                yPos += statBuffHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }
            
            
            // Triggered Ability
            SerializedProperty canTriggerAbility = property.FindPropertyRelative("canTriggerAbility");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(canTriggerAbility)), canTriggerAbility, new GUIContent("Trigger Abilities"));
            yPos += EditorGUI.GetPropertyHeight(canTriggerAbility) + EditorGUIUtility.standardVerticalSpacing;

            if (canTriggerAbility.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty triggeredAbilityConfigs = property.FindPropertyRelative("triggeredAbilityConfigs");
                float triggeredAbilityHeight = EditorGUI.GetPropertyHeight(triggeredAbilityConfigs, true);
                EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, triggeredAbilityHeight), triggeredAbilityConfigs, true);
                yPos += triggeredAbilityHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout
        
        // Crowd Control Header
        height += 5 + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Root
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canRoot = property.FindPropertyRelative("canRoot");
        if (canRoot != null && canRoot.boolValue)
        {
            SerializedProperty rootEffect = property.FindPropertyRelative("rootEffect");
            SerializedProperty rootDuration = property.FindPropertyRelative("rootDuration");
            SerializedProperty rootApplicationChance = property.FindPropertyRelative("rootApplicationChance");
            
            if (rootEffect != null)
                height += EditorGUI.GetPropertyHeight(rootEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (rootDuration != null)
                height += EditorGUI.GetPropertyHeight(rootDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (rootApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(rootApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Slow
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canSlow = property.FindPropertyRelative("canSlow");
        if (canSlow != null && canSlow.boolValue)
        {
            SerializedProperty slowEffect = property.FindPropertyRelative("slowEffect");
            SerializedProperty slowDuration = property.FindPropertyRelative("slowDuration");
            SerializedProperty slowApplicationChance = property.FindPropertyRelative("slowApplicationChance");
            
            if (slowEffect != null)
                height += EditorGUI.GetPropertyHeight(slowEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (slowDuration != null)
                height += EditorGUI.GetPropertyHeight(slowDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (slowApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(slowApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Stun
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canStun = property.FindPropertyRelative("canStun");
        if (canStun != null && canStun.boolValue)
        {
            SerializedProperty stunEffect = property.FindPropertyRelative("stunEffect");
            SerializedProperty stunDuration = property.FindPropertyRelative("stunDuration");
            SerializedProperty stunApplicationChance = property.FindPropertyRelative("stunApplicationChance");
            
            if (stunEffect != null)
                height += EditorGUI.GetPropertyHeight(stunEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (stunDuration != null)
                height += EditorGUI.GetPropertyHeight(stunDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (stunApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(stunApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Damage Over Time Header
        height += 5 + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // Bleed
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canBleed = property.FindPropertyRelative("canBleed");
        if (canBleed != null && canBleed.boolValue)
        {
            SerializedProperty bleedEffect = property.FindPropertyRelative("bleedEffect");
            SerializedProperty bleedDamage = property.FindPropertyRelative("bleedDamage");
            SerializedProperty bleedDuration = property.FindPropertyRelative("bleedDuration");
            SerializedProperty bleedApplicationChance = property.FindPropertyRelative("bleedApplicationChance");
            
            if (bleedEffect != null)
                height += EditorGUI.GetPropertyHeight(bleedEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (bleedDamage != null)
                height += EditorGUI.GetPropertyHeight(bleedDamage) + EditorGUIUtility.standardVerticalSpacing;
            if (bleedDuration != null)
                height += EditorGUI.GetPropertyHeight(bleedDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (bleedApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(bleedApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Burn
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canBurn = property.FindPropertyRelative("canBurn");
        if (canBurn != null && canBurn.boolValue)
        {
            SerializedProperty burnEffect = property.FindPropertyRelative("burnEffect");
            SerializedProperty burnDamage = property.FindPropertyRelative("burnDamage");
            SerializedProperty burnDuration = property.FindPropertyRelative("burnDuration");
            SerializedProperty burnApplicationChance = property.FindPropertyRelative("burnApplicationChance");
            
            if (burnEffect != null)
                height += EditorGUI.GetPropertyHeight(burnEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (burnDamage != null)
                height += EditorGUI.GetPropertyHeight(burnDamage) + EditorGUIUtility.standardVerticalSpacing;
            if (burnDuration != null)
                height += EditorGUI.GetPropertyHeight(burnDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (burnApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(burnApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Poison
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canPoison = property.FindPropertyRelative("canPoison");
        if (canPoison != null && canPoison.boolValue)
        {
            SerializedProperty poisonEffect = property.FindPropertyRelative("poisonEffect");
            SerializedProperty poisonDamage = property.FindPropertyRelative("poisonDamage");
            SerializedProperty poisonDuration = property.FindPropertyRelative("poisonDuration");
            SerializedProperty poisonApplicationChance = property.FindPropertyRelative("poisonApplicationChance");
            
            if (poisonEffect != null)
                height += EditorGUI.GetPropertyHeight(poisonEffect) + EditorGUIUtility.standardVerticalSpacing;
            if (poisonDamage != null)
                height += EditorGUI.GetPropertyHeight(poisonDamage) + EditorGUIUtility.standardVerticalSpacing;
            if (poisonDuration != null)
                height += EditorGUI.GetPropertyHeight(poisonDuration) + EditorGUIUtility.standardVerticalSpacing;
            if (poisonApplicationChance != null)
                height += EditorGUI.GetPropertyHeight(poisonApplicationChance) + EditorGUIUtility.standardVerticalSpacing;
        }

        // Stat Buffs
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canApplyStatBuffs = property.FindPropertyRelative("canApplyStatBuffs");
        if (canApplyStatBuffs != null && canApplyStatBuffs.boolValue)
        {
            SerializedProperty statBuffApplications = property.FindPropertyRelative("statBuffApplications");
            if (statBuffApplications != null)
                height += EditorGUI.GetPropertyHeight(statBuffApplications, true) + EditorGUIUtility.standardVerticalSpacing;
        }
        
        // Triggered Ability
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty canTriggerAbility = property.FindPropertyRelative("canTriggerAbility");
        if (canTriggerAbility != null && canTriggerAbility.boolValue)
        {
            SerializedProperty triggeredAbilityConfigs = property.FindPropertyRelative("triggeredAbilityConfigs");
            if (triggeredAbilityConfigs != null)
                height += EditorGUI.GetPropertyHeight(triggeredAbilityConfigs, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        return height += 75;
    }
}
