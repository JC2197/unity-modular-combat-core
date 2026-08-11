using UnityEditor;
using UnityEngine;
using System.Linq;
using JoeConticello.ModularCombatCore;
/// <summary>
/// Draws StatModifier with a StatTypeDatabase-backed stat dropdown.
/// This applies anywhere List<StatModifier> is used (traits, armor base stats, etc.).
/// </summary>
[CustomPropertyDrawer(typeof(StatModifier))]
public class StatModifierDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            float y = position.y + lineHeight + spacing;

            SerializedProperty statID = property.FindPropertyRelative("statID");
            SerializedProperty modifierType = property.FindPropertyRelative("modifierType");
            SerializedProperty value = property.FindPropertyRelative("value");

            Rect statRect = new Rect(position.x, y, position.width, lineHeight);
            DrawStatIDDropdown(statRect, statID);
            y += lineHeight + spacing;

            Rect typeRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(typeRect, modifierType);
            y += lineHeight + spacing;

            Rect valueRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(valueRect, value);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawStatIDDropdown(Rect position, SerializedProperty statIDProperty)
    {
        StatTypeDatabase database = StatTypeDatabase.Instance;

        if (database == null || database.statTypes == null || database.statTypes.Count == 0)
        {
            EditorGUI.PropertyField(position, statIDProperty, new GUIContent("Stat ID"));
            return;
        }

        string[] statIDs = database.statTypes.Select(s => s.statID).ToArray();
        string[] displayNames = database.statTypes.Select(s => $"{s.displayName} ({s.statID})").ToArray();

        int currentIndex = System.Array.IndexOf(statIDs, statIDProperty.stringValue);
        if (currentIndex < 0)
            currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(position, "Stat ID", currentIndex, displayNames);
        if (EditorGUI.EndChangeCheck())
        {
            statIDProperty.stringValue = statIDs[newIndex];
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        return lineHeight + (lineHeight + spacing) * 3f;
    }
}
