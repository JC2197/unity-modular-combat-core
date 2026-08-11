#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(AbilityTagSelector))]
public class AbilityTagSelectorDrawer : PropertyDrawer
{
    private const int COLUMNS = 3;
    private const float COLUMN_SPACING = 5f;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var selectedTagsProperty = property.FindPropertyRelative("selectedTags");
        var selectedDamageTypesProperty = property.FindPropertyRelative("selectedDamageTypes");
        
        if (TagDatabase.Instance == null)
        {
            EditorGUI.LabelField(position, "No TagDatabase found in Resources!");
            EditorGUI.EndProperty();
            return;
        }
        
        var availableTags = TagDatabase.Instance.GetAllTagNames();
        var availableDamageTypes = TagDatabase.Instance.AvailableDamageTypes;
        
        position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yOffset = EditorGUIUtility.singleLineHeight + 2f;
            
            // Tags section
            if (availableTags.Length > 0)
            {
                var categoryRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(categoryRect, "Tags", EditorStyles.boldLabel);
                yOffset += EditorGUIUtility.singleLineHeight + 2f;
                
                int rowCount = Mathf.CeilToInt(availableTags.Length / (float)COLUMNS);
                float columnWidth = (position.width - (COLUMN_SPACING * (COLUMNS - 1)) - 20) / COLUMNS;
                
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < COLUMNS; col++)
                    {
                        int index = row * COLUMNS + col;
                        if (index >= availableTags.Length) break;
                        
                        string tagName = availableTags[index];
                        float xPos = position.x + 20 + (col * (columnWidth + COLUMN_SPACING));
                        var tagRect = new Rect(xPos, position.y + yOffset, columnWidth, EditorGUIUtility.singleLineHeight);
                        
                        bool isSelected = IsTagSelected(selectedTagsProperty, tagName);
                        bool newSelected = EditorGUI.ToggleLeft(tagRect, tagName, isSelected);
                        
                        if (newSelected != isSelected)
                        {
                            UpdateTagSelection(selectedTagsProperty, tagName, newSelected);
                        }
                    }
                    
                    yOffset += EditorGUIUtility.singleLineHeight + 1f;
                }
                
                yOffset += 5f;
            }
            
            // Damage Types Section
            if (availableDamageTypes.Count > 0)
            {
                var categoryRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(categoryRect, "Damage Types", EditorStyles.boldLabel);
                yOffset += EditorGUIUtility.singleLineHeight + 2f;
                
                int damageRowCount = Mathf.CeilToInt(availableDamageTypes.Count / (float)COLUMNS);
                float columnWidth = (position.width - (COLUMN_SPACING * (COLUMNS - 1)) - 20) / COLUMNS;
                
                for (int row = 0; row < damageRowCount; row++)
                {
                    for (int col = 0; col < COLUMNS; col++)
                    {
                        int index = row * COLUMNS + col;
                        if (index >= availableDamageTypes.Count) break;
                        
                        var damageType = availableDamageTypes[index];
                        if (damageType == null) continue;
                        
                        float xPos = position.x + 20 + (col * (columnWidth + COLUMN_SPACING));
                        var damageRect = new Rect(xPos, position.y + yOffset, columnWidth, EditorGUIUtility.singleLineHeight);
                        
                        bool isSelected = IsDamageTypeSelected(selectedDamageTypesProperty, damageType);
                        bool newSelected = EditorGUI.ToggleLeft(damageRect, damageType.displayName, isSelected);
                        
                        if (newSelected != isSelected)
                        {
                            UpdateDamageTypeSelection(selectedDamageTypesProperty, damageType, newSelected);
                        }
                    }
                    
                    yOffset += EditorGUIUtility.singleLineHeight + 1f;
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    private bool IsTagSelected(SerializedProperty selectedTagsProperty, string tagName)
    {
        for (int i = 0; i < selectedTagsProperty.arraySize; i++)
        {
            if (selectedTagsProperty.GetArrayElementAtIndex(i).stringValue == tagName)
                return true;
        }
        return false;
    }
    
    private void UpdateTagSelection(SerializedProperty selectedTagsProperty, string tagName, bool add)
    {
        if (add)
        {
            selectedTagsProperty.arraySize++;
            selectedTagsProperty.GetArrayElementAtIndex(selectedTagsProperty.arraySize - 1).stringValue = tagName;
        }
        else
        {
            for (int i = 0; i < selectedTagsProperty.arraySize; i++)
            {
                if (selectedTagsProperty.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    selectedTagsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }
    
    private bool IsDamageTypeSelected(SerializedProperty selectedDamageTypesProperty, DamageTypeData damageType)
    {
        for (int i = 0; i < selectedDamageTypesProperty.arraySize; i++)
        {
            if (selectedDamageTypesProperty.GetArrayElementAtIndex(i).objectReferenceValue == damageType)
                return true;
        }
        return false;
    }
    
    private void UpdateDamageTypeSelection(SerializedProperty selectedDamageTypesProperty, DamageTypeData damageType, bool add)
    {
        if (add)
        {
            selectedDamageTypesProperty.arraySize++;
            selectedDamageTypesProperty.GetArrayElementAtIndex(selectedDamageTypesProperty.arraySize - 1).objectReferenceValue = damageType;
        }
        else
        {
            for (int i = 0; i < selectedDamageTypesProperty.arraySize; i++)
            {
                if (selectedDamageTypesProperty.GetArrayElementAtIndex(i).objectReferenceValue == damageType)
                {
                    selectedDamageTypesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
        
        if (TagDatabase.Instance == null) return EditorGUIUtility.singleLineHeight;
        
        var availableTags = TagDatabase.Instance.GetAllTagNames();
        var availableDamageTypes = TagDatabase.Instance.AvailableDamageTypes;
        
        float height = EditorGUIUtility.singleLineHeight; // Foldout
        
        // Tags
        if (availableTags.Length > 0)
        {
            height += EditorGUIUtility.singleLineHeight + 2f; // Header
            int rowCount = Mathf.CeilToInt(availableTags.Length / (float)COLUMNS);
            height += (rowCount * (EditorGUIUtility.singleLineHeight + 1f));
            height += 5f;
        }
        
        // Damage types
        if (availableDamageTypes.Count > 0)
        {
            height += EditorGUIUtility.singleLineHeight + 2f;
            int damageRowCount = Mathf.CeilToInt(availableDamageTypes.Count / (float)COLUMNS);
            height += (damageRowCount * (EditorGUIUtility.singleLineHeight + 1f));
        }
        
        return height;
    }
}
#endif