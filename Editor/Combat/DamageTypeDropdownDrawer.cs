#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(DamageTypeDropdownAttribute))]
public class DamageTypeDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [DamageTypeDropdown] with string fields only.");
            return;
        }
        
        // Get the damage type database
        DamageTypeDatabase database = DamageTypeDatabase.Instance;
        
        if (database == null || database.damageTypes == null || database.damageTypes.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.LabelField(position, label.text, "DamageTypeDatabase not found or empty");
            return;
        }
        
        // Get all damage type names
        string[] damageTypeNames = database.GetDamageTypeNames();
        
        if (damageTypeNames.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        
        // Find current index
        int currentIndex = database.GetDamageTypeIndex(property.stringValue);
        if (currentIndex == -1)
        {
            currentIndex = 0; // Default to first option if not found
            property.stringValue = damageTypeNames[0]; // Update the property to match
        }
        
        // Draw dropdown
        EditorGUI.BeginProperty(position, label, property);
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, damageTypeNames);
        
        if (newIndex != currentIndex)
        {
            property.stringValue = damageTypeNames[newIndex];
            property.serializedObject.ApplyModifiedProperties(); // Force save
        }
        
        EditorGUI.EndProperty();
    }
}
#endif
