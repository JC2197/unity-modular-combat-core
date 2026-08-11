#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Property drawer for [TagDropdown] — shows a dropdown of all tags from TagDatabase.
/// </summary>
[CustomPropertyDrawer(typeof(TagDropdownAttribute))]
public class TagDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [TagDropdown] with string fields only.");
            EditorGUI.EndProperty();
            return;
        }

        TagDatabase database = TagDatabase.Instance;
        if (database == null || database.tags.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndProperty();
            return;
        }

        // Build options with "None" first
        string[] tagNames = database.GetAllTagNames();
        string[] options = new string[tagNames.Length + 1];
        options[0] = "None";
        System.Array.Copy(tagNames, 0, options, 1, tagNames.Length);

        string currentValue = property.stringValue;
        int currentIndex = 0;
        for (int i = 1; i < options.Length; i++)
        {
            if (options[i] == currentValue)
            {
                currentIndex = i;
                break;
            }
        }

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);

        if (newIndex == 0)
            property.stringValue = "";
        else if (newIndex > 0 && newIndex < options.Length)
            property.stringValue = options[newIndex];

        EditorGUI.EndProperty();
    }
}
#endif
