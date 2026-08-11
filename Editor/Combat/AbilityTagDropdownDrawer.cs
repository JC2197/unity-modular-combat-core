#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Legacy property drawer for AbilityTagDropdownAttribute.
/// Now delegates to TagDatabase. Use [TagDropdown] for new fields.
/// </summary>
[CustomPropertyDrawer(typeof(AbilityTagDropdownAttribute))]
public class AbilityTagDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use AbilityTagDropdown with string fields only.");
            return;
        }
        
        TagDatabase database = TagDatabase.Instance;
        if (database == null || database.tags.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        
        var tagNames = database.GetAllTagNames();
        string currentValue = property.stringValue;
        int selectedIndex = System.Array.IndexOf(tagNames, currentValue);
        
        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, tagNames);
        
        if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < tagNames.Length)
        {
            property.stringValue = tagNames[selectedIndex];
        }
    }
}
#endif
