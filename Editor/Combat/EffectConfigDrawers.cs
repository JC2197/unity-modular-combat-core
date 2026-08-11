using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for RootEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(RootEffectConfig))]
public class RootEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), applicationChance);
            yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight), duration);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing * 2;
    }
}

/// <summary>
/// Custom property drawer for SlowEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(SlowEffectConfig))]
public class SlowEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(duration)), duration);
            yPos += EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty slowAmount = property.FindPropertyRelative("slowAmount");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(slowAmount)), slowAmount);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        SerializedProperty slowAmount = property.FindPropertyRelative("slowAmount");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(duration) + EditorGUI.GetPropertyHeight(slowAmount) + EditorGUIUtility.standardVerticalSpacing * 3;
    }
}

/// <summary>
/// Custom property drawer for StunEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(StunEffectConfig))]
public class StunEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(duration)), duration);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing * 2;
    }
}


/// <summary>
/// Custom property drawer for BleedEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(BleedEffectConfig))]
public class BleedEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(damagePerTick)), damagePerTick);
            yPos += EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(tickInterval)), tickInterval);
            yPos += EditorGUI.GetPropertyHeight(tickInterval) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(duration)), duration);
            yPos += EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty bloodParticlePrefab = property.FindPropertyRelative("bloodParticlePrefab");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(bloodParticlePrefab)), bloodParticlePrefab);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
        SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        SerializedProperty bloodParticlePrefab = property.FindPropertyRelative("bloodParticlePrefab");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUI.GetPropertyHeight(tickInterval) + EditorGUI.GetPropertyHeight(duration) + EditorGUI.GetPropertyHeight(bloodParticlePrefab) + EditorGUIUtility.standardVerticalSpacing * 5;
    }
}

/// <summary>
/// Custom property drawer for BurnEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(BurnEffectConfig))]
public class BurnEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(damagePerTick)), damagePerTick);
            yPos += EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(tickInterval)), tickInterval);
            yPos += EditorGUI.GetPropertyHeight(tickInterval) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(duration)), duration);
            yPos += EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty fireParticlePrefab = property.FindPropertyRelative("fireParticlePrefab");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(fireParticlePrefab)), fireParticlePrefab);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
        SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        SerializedProperty fireParticlePrefab = property.FindPropertyRelative("fireParticlePrefab");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUI.GetPropertyHeight(tickInterval) + EditorGUI.GetPropertyHeight(duration) + EditorGUI.GetPropertyHeight(fireParticlePrefab) + EditorGUIUtility.standardVerticalSpacing * 5;
    }
}

/// <summary>
/// Custom property drawer for PoisonEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(PoisonEffectConfig))]
public class PoisonEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(damagePerTick)), damagePerTick);
            yPos += EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(tickInterval)), tickInterval);
            yPos += EditorGUI.GetPropertyHeight(tickInterval) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty duration = property.FindPropertyRelative("duration");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(duration)), duration);
            yPos += EditorGUI.GetPropertyHeight(duration) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty poisonParticlePrefab = property.FindPropertyRelative("poisonParticlePrefab");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(poisonParticlePrefab)), poisonParticlePrefab);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty damagePerTick = property.FindPropertyRelative("damagePerTick");
        SerializedProperty tickInterval = property.FindPropertyRelative("tickInterval");
        SerializedProperty duration = property.FindPropertyRelative("duration");
        SerializedProperty poisonParticlePrefab = property.FindPropertyRelative("poisonParticlePrefab");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(damagePerTick) + EditorGUI.GetPropertyHeight(tickInterval) + EditorGUI.GetPropertyHeight(duration) + EditorGUI.GetPropertyHeight(poisonParticlePrefab) + EditorGUIUtility.standardVerticalSpacing * 5;
    }
}

/// <summary>
/// Custom property drawer for KnockbackEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(KnockbackEffectConfig))]
public class KnockbackEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty force = property.FindPropertyRelative("force");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(force)), force);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty force = property.FindPropertyRelative("force");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(force) + EditorGUIUtility.standardVerticalSpacing * 2;
    }
}

/// <summary>
/// Custom property drawer for ExplosionEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(ExplosionEffectConfig))]
public class ExplosionEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(applicationChance)), applicationChance);
            yPos += EditorGUI.GetPropertyHeight(applicationChance) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty radius = property.FindPropertyRelative("radius");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(radius)), radius);
            yPos += EditorGUI.GetPropertyHeight(radius) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty damage = property.FindPropertyRelative("damage");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(damage)), damage);
            yPos += EditorGUI.GetPropertyHeight(damage) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty explosionPrefab = property.FindPropertyRelative("explosionPrefab");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(explosionPrefab)), explosionPrefab);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty applicationChance = property.FindPropertyRelative("applicationChance");
        SerializedProperty radius = property.FindPropertyRelative("radius");
        SerializedProperty damage = property.FindPropertyRelative("damage");
        SerializedProperty explosionPrefab = property.FindPropertyRelative("explosionPrefab");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(applicationChance) + EditorGUI.GetPropertyHeight(radius) + EditorGUI.GetPropertyHeight(damage) + EditorGUI.GetPropertyHeight(explosionPrefab) + EditorGUIUtility.standardVerticalSpacing * 4;
    }
}

/// <summary>
/// Custom property drawer for HealEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(HealEffectConfig))]
public class HealEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty healPerTick = property.FindPropertyRelative("healPerTick");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(healPerTick)), healPerTick);
            yPos += EditorGUI.GetPropertyHeight(healPerTick) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty healsAllies = property.FindPropertyRelative("healsAllies");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(healsAllies)), healsAllies);
            yPos += EditorGUI.GetPropertyHeight(healsAllies) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty healsSelf = property.FindPropertyRelative("healsSelf");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(healsSelf)), healsSelf);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        SerializedProperty healPerTick = property.FindPropertyRelative("healPerTick");
        SerializedProperty healsAllies = property.FindPropertyRelative("healsAllies");
        SerializedProperty healsSelf = property.FindPropertyRelative("healsSelf");
        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(healPerTick) + EditorGUI.GetPropertyHeight(healsAllies) + EditorGUI.GetPropertyHeight(healsSelf) + EditorGUIUtility.standardVerticalSpacing * 3;
    }
}

/// <summary>
/// Custom property drawer for CleanseEffectConfig.
/// </summary>
[CustomPropertyDrawer(typeof(CleanseEffectConfig))]
public class CleanseEffectConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float yPos = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty cleansesDebuffs = property.FindPropertyRelative("cleansesDebuffs");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(cleansesDebuffs)), cleansesDebuffs);
            yPos += EditorGUI.GetPropertyHeight(cleansesDebuffs) + EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty cleansesDoTs = property.FindPropertyRelative("cleansesDoTs");
            EditorGUI.PropertyField(new Rect(position.x, yPos, position.width, EditorGUI.GetPropertyHeight(cleansesDoTs)), cleansesDoTs);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
    }
}
