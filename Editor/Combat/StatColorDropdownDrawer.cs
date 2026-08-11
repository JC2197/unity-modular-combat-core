using UnityEngine;
using UnityEditor;

/// <summary>
/// Legacy property drawer for StatColorDropdownAttribute.
/// Now delegates to TagDatabase. Use [TagDropdown] for new fields.
/// </summary>
[CustomPropertyDrawer(typeof(StatColorDropdownAttribute))]
public class StatColorDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        if (property.propertyType == SerializedPropertyType.String)
        {
            TagDatabase database = TagDatabase.Instance;
            
            if (database == null || database.tags.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                string currentValue = property.stringValue;
                string[] options = database.GetAllTagNames();
                int currentIndex = System.Array.IndexOf(options, currentValue);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
                
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    property.stringValue = options[newIndex];
                }
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [StatColorDropdown] with string fields only.");
        }
        
        EditorGUI.EndProperty();
    }
}
