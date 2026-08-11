using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom property drawer for TraitStatModifier - shows dropdown of stats from StatTypeDatabase
/// </summary>
[CustomPropertyDrawer(typeof(TraitStatModifier))]
public class TraitStatModifierDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Calculate rects
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        
        Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float currentY = position.y + lineHeight + spacing;
            
            SerializedProperty statID = property.FindPropertyRelative("statID");
            SerializedProperty modifierType = property.FindPropertyRelative("modifierType");
            SerializedProperty value = property.FindPropertyRelative("value");
            SerializedProperty description = property.FindPropertyRelative("description");
            
            // Stat ID Dropdown
            Rect statIDRect = new Rect(position.x, currentY, position.width, lineHeight);
            DrawStatIDDropdown(statIDRect, statID);
            currentY += lineHeight + spacing;
            
            // Modifier Type
            Rect modifierTypeRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(modifierTypeRect, modifierType);
            currentY += lineHeight + spacing;
            
            // Value
            Rect valueRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(valueRect, value);
            currentY += lineHeight + spacing;
            
            // Description
            Rect descRect = new Rect(position.x, currentY, position.width, lineHeight * 2);
            EditorGUI.PropertyField(descRect, description, GUIContent.none);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    private void DrawStatIDDropdown(Rect position, SerializedProperty statIDProperty)
    {
        StatTypeDatabase database = StatTypeDatabase.Instance;
        if (database == null || database.statTypes == null || database.statTypes.Count == 0)
        {
            EditorGUI.LabelField(position, "Stat ID", "StatTypeDatabase not found!");
            return;
        }
        
        // Get all stat IDs
        var statIDs = database.statTypes.Select(s => s.statID).ToArray();
        var displayNames = database.statTypes.Select(s => $"{s.displayName} ({s.statID})").ToArray();
        
        // Find current index
        string currentStatID = statIDProperty.stringValue;
        int currentIndex = System.Array.IndexOf(statIDs, currentStatID);
        if (currentIndex < 0) currentIndex = 0;
        
        // Draw dropdown
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(position, "Stat ID", currentIndex, displayNames);
        if (EditorGUI.EndChangeCheck())
        {
            statIDProperty.stringValue = statIDs[newIndex];
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        
        // Foldout + StatID + ModifierType + Value + Description (2 lines)
        return lineHeight + (lineHeight + spacing) * 4 + lineHeight;
    }
}
