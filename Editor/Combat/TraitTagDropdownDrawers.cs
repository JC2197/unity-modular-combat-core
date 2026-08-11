using UnityEngine;
using UnityEditor;

/// <summary>
/// Legacy property drawer for CoreTraitTagDropdown.
/// Now delegates to TagDatabase. Use [TagDropdown] for new fields.
/// </summary>
[CustomPropertyDrawer(typeof(CoreTraitTagDropdownAttribute))]
public class CoreTraitTagDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        if (property.propertyType == SerializedPropertyType.String)
        {
            TagDatabase database = TagDatabase.Instance;
            
            if (database == null)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                string[] tagNames = database.GetAllTagNames();
                string[] options = new string[tagNames.Length + 1];
                options[0] = "None";
                System.Array.Copy(tagNames, 0, options, 1, tagNames.Length);

                string currentValue = property.stringValue;
                int currentIndex = 0;
                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i] == currentValue) { currentIndex = i; break; }
                }
                
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
                
                if (newIndex == 0)
                    property.stringValue = "";
                else if (newIndex > 0 && newIndex < options.Length)
                    property.stringValue = options[newIndex];
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [CoreTraitTagDropdown] with string fields only.");
        }
        
        EditorGUI.EndProperty();
    }
}

/// <summary>
/// Legacy property drawer for SpecializedTraitTagDropdown.
/// Now delegates to TagDatabase. Use [TagDropdown] for new fields.
/// </summary>
[CustomPropertyDrawer(typeof(SpecializedTraitTagDropdownAttribute))]
public class SpecializedTraitTagDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        if (property.propertyType == SerializedPropertyType.String)
        {
            TagDatabase database = TagDatabase.Instance;
            
            if (database == null)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                string[] tagNames = database.GetAllTagNames();
                string[] options = new string[tagNames.Length + 1];
                options[0] = "None";
                System.Array.Copy(tagNames, 0, options, 1, tagNames.Length);

                string currentValue = property.stringValue;
                int currentIndex = 0;
                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i] == currentValue) { currentIndex = i; break; }
                }
                
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
                
                if (newIndex == 0)
                    property.stringValue = "";
                else if (newIndex > 0 && newIndex < options.Length)
                    property.stringValue = options[newIndex];
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [SpecializedTraitTagDropdown] with string fields only.");
        }
        
        EditorGUI.EndProperty();
    }
}

