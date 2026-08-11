using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom property drawer for EnemyStatContainer - displays stats organized by category with foldouts
/// </summary>
[CustomPropertyDrawer(typeof(EnemyStatContainer))]
public class EnemyStatContainerDrawer : PropertyDrawer
{
    private bool showBaseStats = true;
    private bool showOffensiveStats = true;
    private bool showDefensiveStats = true;
    private bool showSpecialStats = true;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // Main container foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true, EditorStyles.foldoutHeader);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            float currentY = position.y + EditorGUIUtility.singleLineHeight + 5;
            
            // Get the lists
            SerializedProperty baseStats = property.FindPropertyRelative("baseStats");
            SerializedProperty offensiveStats = property.FindPropertyRelative("offensiveStats");
            SerializedProperty defensiveStats = property.FindPropertyRelative("defensiveStats");
            SerializedProperty specialStats = property.FindPropertyRelative("specialStats");
            
            // Check if container is initialized
            int totalCount = baseStats.arraySize + offensiveStats.arraySize + defensiveStats.arraySize + specialStats.arraySize;
            
            if (totalCount == 0)
            {
                Rect helpRect = new Rect(position.x, currentY, position.width, 40);
                EditorGUI.HelpBox(helpRect, "Stat container is empty. Use 'Initialize Stats from Database' button below.", MessageType.Info);
                currentY += 45;
            }
            else
            {
                // Draw categories
                currentY = DrawStatCategory("Base Stats", baseStats, position.x, currentY, position.width, ref showBaseStats, new Color(0.5f, 0.7f, 1f));
                currentY = DrawStatCategory("Offensive Stats", offensiveStats, position.x, currentY, position.width, ref showOffensiveStats, new Color(1f, 0.5f, 0.5f));
                currentY = DrawStatCategory("Defensive Stats", defensiveStats, position.x, currentY, position.width, ref showDefensiveStats, new Color(0.5f, 1f, 0.5f));
                currentY = DrawStatCategory("Special Stats", specialStats, position.x, currentY, position.width, ref showSpecialStats, new Color(1f, 0.9f, 0.5f));
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    private float DrawStatCategory(string categoryName, SerializedProperty statList, float x, float y, float width, ref bool foldout, Color headerColor)
    {
        float currentY = y;
        
        // Category header
        Rect headerRect = new Rect(x, currentY, width, EditorGUIUtility.singleLineHeight);
        var originalBG = GUI.backgroundColor;
        GUI.backgroundColor = headerColor;
        EditorGUI.DrawRect(headerRect, headerColor * 0.3f);
        GUI.backgroundColor = originalBG;
        
        string label = $"{categoryName} ({statList.arraySize})";
        foldout = EditorGUI.Foldout(headerRect, foldout, label, true, EditorStyles.foldoutHeader);
        currentY += EditorGUIUtility.singleLineHeight + 2;
        
        if (foldout && statList.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < statList.arraySize; i++)
            {
                SerializedProperty statValue = statList.GetArrayElementAtIndex(i);
                SerializedProperty statID = statValue.FindPropertyRelative("statID");
                SerializedProperty displayName = statValue.FindPropertyRelative("displayName");
                SerializedProperty currentValue = statValue.FindPropertyRelative("currentValue");
                
                // Draw stat name and value on same line
                Rect statRect = new Rect(x, currentY, width, EditorGUIUtility.singleLineHeight);
                Rect labelRect = new Rect(statRect.x, statRect.y, statRect.width * 0.6f, statRect.height);
                Rect valueRect = new Rect(statRect.x + statRect.width * 0.6f, statRect.y, statRect.width * 0.4f, statRect.height);
                
                string displayText = string.IsNullOrEmpty(displayName.stringValue) ? statID.stringValue : displayName.stringValue;
                EditorGUI.LabelField(labelRect, displayText);
                
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUI.FloatField(valueRect, currentValue.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    currentValue.floatValue = newValue;
                }
                
                currentY += EditorGUIUtility.singleLineHeight + 2;
            }
            
            EditorGUI.indentLevel--;
        }
        
        currentY += 5; // Spacing between categories
        return currentY;
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;
        
        float height = EditorGUIUtility.singleLineHeight + 5; // Main foldout
        
        SerializedProperty baseStats = property.FindPropertyRelative("baseStats");
        SerializedProperty offensiveStats = property.FindPropertyRelative("offensiveStats");
        SerializedProperty defensiveStats = property.FindPropertyRelative("defensiveStats");
        SerializedProperty specialStats = property.FindPropertyRelative("specialStats");
        
        int totalCount = baseStats.arraySize + offensiveStats.arraySize + defensiveStats.arraySize + specialStats.arraySize;
        
        if (totalCount == 0)
        {
            height += 45; // Help box
        }
        else
        {
            // Base category header
            height += EditorGUIUtility.singleLineHeight + 2;
            if (showBaseStats && baseStats.arraySize > 0)
                height += (EditorGUIUtility.singleLineHeight + 2) * baseStats.arraySize;
            height += 5;
            
            // Offensive category header
            height += EditorGUIUtility.singleLineHeight + 2;
            if (showOffensiveStats && offensiveStats.arraySize > 0)
                height += (EditorGUIUtility.singleLineHeight + 2) * offensiveStats.arraySize;
            height += 5;
            
            // Defensive category header
            height += EditorGUIUtility.singleLineHeight + 2;
            if (showDefensiveStats && defensiveStats.arraySize > 0)
                height += (EditorGUIUtility.singleLineHeight + 2) * defensiveStats.arraySize;
            height += 5;
            
            // Special category header
            height += EditorGUIUtility.singleLineHeight + 2;
            if (showSpecialStats && specialStats.arraySize > 0)
                height += (EditorGUIUtility.singleLineHeight + 2) * specialStats.arraySize;
            height += 5;
        }
        
        return height;
    }
}
