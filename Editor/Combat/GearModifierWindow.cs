using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for managing GearModifier assets.
/// Finds all GearModifier ScriptableObjects and allows easy viewing and editing.
/// </summary>
public class GearModifierWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<GearModifier> allModifiers = new List<GearModifier>();
    private GearModifierDatabase database;
    private string searchFilter = "";
    
    // Sorting
    private enum SortMode { Name, Tier, ColorTheme }
    private SortMode currentSortMode = SortMode.Name;
    
    // Foldouts
    private bool showOnlyInDatabase = false;
    
    // Track expanded/collapsed state for each modifier
    private Dictionary<int, bool> modifierExpandedState = new Dictionary<int, bool>();
    
    [MenuItem("Tools/Gear Modifier Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<GearModifierWindow>("Gear Modifiers");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    private void OnEnable()
    {
        RefreshModifierList();
        LoadDatabase();
    }
    
    private void LoadDatabase()
    {
        // Try to find the GearModifierDatabase in Resources
        database = Resources.Load<GearModifierDatabase>("GearModifierDatabase");
        
        if (database == null)
        {
            // Try to find it anywhere in the project
            string[] guids = AssetDatabase.FindAssets("t:GearModifierDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<GearModifierDatabase>(path);
            }
        }
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        
        // Header
        EditorGUILayout.LabelField("Gear Modifier Manager", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Manage all gear modifiers", EditorStyles.helpBox);
        
        EditorGUILayout.Space(10);
        
        // Search and Sort bar
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            searchFilter = "";
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshModifierList();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUIContent sortLabel = new GUIContent("Sort By:", "Choose how to sort the modifier lists");
        currentSortMode = (SortMode)EditorGUILayout.EnumPopup(sortLabel, currentSortMode);
        if (GUILayout.Button("Apply Sort", GUILayout.Width(120)))
        {
            ApplySorting();
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Expand All", GUILayout.Width(80)))
        {
            ExpandCollapseAll(true);
        }
        if (GUILayout.Button("Collapse All", GUILayout.Width(90)))
        {
            ExpandCollapseAll(false);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Create New button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Gear Modifier", GUILayout.Height(25), GUILayout.Width(200)))
        {
            CreateNewModifier();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Database section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Gear Modifier Database", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Database:", database, typeof(GearModifierDatabase), false);
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            if (database != null)
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
            }
        }
        
        if (GUILayout.Button("Reload", GUILayout.Width(60)))
        {
            LoadDatabase();
        }
        EditorGUILayout.EndHorizontal();
        
        if (database != null)
        {
            int modifierCount = database.modifiers != null ? database.modifiers.Count : 0;
            EditorGUILayout.LabelField($"Database contains: {modifierCount} modifiers", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Add All to Database", GUILayout.Height(25)))
            {
                AddAllToDatabase();
            }
            
            showOnlyInDatabase = EditorGUILayout.Toggle("Show only in database", showOnlyInDatabase);
        }
        else
        {
            EditorGUILayout.HelpBox("No GearModifierDatabase found! Create one at Assets/Resources/GearModifierDatabase.asset", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Modifier list
        var displayModifiers = allModifiers.Where(m => m != null).ToList();
        
        // Filter by database if needed
        if (showOnlyInDatabase && database != null)
        {
            displayModifiers = displayModifiers.Where(m => database.modifiers != null && database.modifiers.Contains(m)).ToList();
        }
        
        // Apply search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            displayModifiers = displayModifiers.Where(m => m.label.ToLower().Contains(searchFilter.ToLower()) || 
                                      m.colorTheme.ToLower().Contains(searchFilter.ToLower())).ToList();
        }
        
        EditorGUILayout.LabelField($"Showing {displayModifiers.Count} of {allModifiers.Count} Modifiers:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var modifier in displayModifiers)
        {
            if (modifier != null)
                DrawModifierEntry(modifier);
        }
        
        EditorGUILayout.EndScrollView();
        
        // Instructions
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Click 'Select' to ping a modifier in the Project view.\n" +
            "Edit fields directly in the window - changes are saved automatically.", MessageType.Info);
    }
    
    private void RefreshModifierList()
    {
        allModifiers.Clear();
        
        // Find all GearModifier assets
        string[] guids = AssetDatabase.FindAssets("t:GearModifier");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GearModifier modifier = AssetDatabase.LoadAssetAtPath<GearModifier>(path);
            if (modifier != null)
            {
                allModifiers.Add(modifier);
            }
        }
        
        // Apply current sorting
        ApplySorting();
        
        // Remove any null entries that may have appeared
        allModifiers.RemoveAll(m => m == null);
        
        Debug.Log($"[GearModifierWindow] Found {allModifiers.Count} GearModifiers");
    }
    
    private void ApplySorting()
    {
        switch (currentSortMode)
        {
            case SortMode.Name:
                allModifiers = allModifiers.OrderBy(m => m.label).ToList();
                break;
                
            case SortMode.Tier:
                allModifiers = allModifiers.OrderBy(m => m.baseTierAvailable).ThenBy(m => m.label).ToList();
                break;
                
            case SortMode.ColorTheme:
                allModifiers = allModifiers.OrderBy(m => m.colorTheme).ThenBy(m => m.label).ToList();
                break;
        }
    }
    
    private void DrawModifierEntry(GearModifier modifier)
    {
        if (modifier == null) return;
        
        int instanceID = modifier.GetInstanceID();
        if (!modifierExpandedState.ContainsKey(instanceID))
        {
            modifierExpandedState[instanceID] = false; // Default to collapsed
        }
        
        EditorGUILayout.BeginVertical("box");
        
        // Top row: Color indicator, Name/Type, Select Button
        EditorGUILayout.BeginHorizontal();
        
        // Foldout arrow
        bool isExpanded = modifierExpandedState[instanceID];
        bool newExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
        if (newExpanded != isExpanded)
        {
            modifierExpandedState[instanceID] = newExpanded;
        }
        
        // Color indicator
        Color modifierColor = modifier.GetColor();
        Rect colorRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
        EditorGUI.DrawRect(colorRect, modifierColor);
        
        // Modifier label and type
        EditorGUILayout.BeginVertical();
        
        // Show database status indicator
        bool inDatabase = IsInDatabase(modifier);
        string statusIcon = inDatabase ? "✓" : "○";
        Color statusColor = inDatabase ? Color.green : Color.gray;
        
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = statusColor;
        
        EditorGUILayout.LabelField($"{statusIcon} {modifier.label}", labelStyle);
        EditorGUILayout.LabelField($"{modifier.colorTheme}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // Stat count
        EditorGUILayout.LabelField($"{modifier.modifiers.Count} stats", EditorStyles.miniLabel, GUILayout.Width(60));
        
        // Add to database button (if not already in)
        if (database != null && !inDatabase)
        {
            if (GUILayout.Button("Add to DB", GUILayout.Width(70)))
            {
                AddSingleToDatabase(modifier);
            }
        }
        
        // Select button to ping the asset in the project
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeObject = modifier;
            EditorGUIUtility.PingObject(modifier);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Editable fields section (only show if expanded)
        if (modifierExpandedState[instanceID])
        {
            DrawEditableFields(modifier);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawEditableFields(GearModifier modifier)
    {
        if (modifier == null) return;
        
        EditorGUI.BeginChangeCheck();
        
        // Store original label width and set it to a smaller value
        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 80f; // Compact label width
        
        try
        {
            SerializedObject serializedModifier = new SerializedObject(modifier);
            serializedModifier.Update();
            
            // First row: Label and Color Theme
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            SerializedProperty labelProp = serializedModifier.FindProperty("label");
            SerializedProperty colorThemeProp = serializedModifier.FindProperty("colorTheme");
            
            EditorGUILayout.PropertyField(labelProp, new GUIContent("Label"), GUILayout.MinWidth(150));
            EditorGUILayout.PropertyField(colorThemeProp, new GUIContent("Color"), GUILayout.MinWidth(150));
            
            EditorGUILayout.EndHorizontal();
            
            // Second row: Tier
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            SerializedProperty tierProp = serializedModifier.FindProperty("baseTierAvailable");
            
            EditorGUILayout.PropertyField(tierProp, new GUIContent("Min Tier"), GUILayout.MinWidth(100));
            
            EditorGUILayout.EndHorizontal();
            
            // Third row: Tier Scaling Config
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            SerializedProperty tierScalingProp = serializedModifier.FindProperty("tierScalingConfig");
            EditorGUILayout.PropertyField(tierScalingProp, new GUIContent("Scaling Config"));
            
            // Recalculate button
            if (modifier.tierScalingConfig != null)
            {
                if (GUILayout.Button("Recalc", GUILayout.Width(60)))
                {
                    EditorUtility.SetDirty(modifier);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Fourth row: Applicable Slots (compact)
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            SerializedProperty slotsProp = serializedModifier.FindProperty("applicableSlots");
            EditorGUILayout.PropertyField(slotsProp, new GUIContent("Slots"), true);
            
            EditorGUILayout.EndHorizontal();
            
            // Fifth section: Stat Modifiers (editable list)
            SerializedProperty modifiersProp = serializedModifier.FindProperty("modifiers");
            if (modifiersProp != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(modifiersProp, new GUIContent("Stat Modifiers"), true);
                EditorGUILayout.EndVertical();
            }
            
            if (serializedModifier.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(modifier);
            }
        }
        finally
        {
            // Always restore the original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(modifier);
        }
    }
    
    private void AddAllToDatabase()
    {
        if (database == null)
        {
            EditorUtility.DisplayDialog("No Database", "No GearModifierDatabase found!", "OK");
            return;
        }
        
        if (database.modifiers == null)
        {
            database.modifiers = new List<GearModifier>();
        }
        
        int added = 0;
        foreach (var modifier in allModifiers)
        {
            if (modifier != null && !database.modifiers.Contains(modifier))
            {
                database.modifiers.Add(modifier);
                added++;
            }
        }
        
        if (added > 0)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GearModifierWindow] Added {added} modifiers to database");
        }
        
        EditorUtility.DisplayDialog("Import Complete", $"Added {added} new modifier(s) to the database.\n\nTotal in database: {database.modifiers.Count}", "OK");
    }
    
    private void AddSingleToDatabase(GearModifier modifier)
    {
        if (database == null || modifier == null) return;
        
        if (database.modifiers == null)
        {
            database.modifiers = new List<GearModifier>();
        }
        
        if (!database.modifiers.Contains(modifier))
        {
            database.modifiers.Add(modifier);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GearModifierWindow] Added '{modifier.label}' to database");
        }
    }
    
    private bool IsInDatabase(GearModifier modifier)
    {
        if (database == null || modifier == null) return false;
        
        return database.modifiers != null && database.modifiers.Contains(modifier);
    }
    
    private void CreateNewModifier()
    {
        // Create a new GearModifier asset
        GearModifier newModifier = CreateInstance<GearModifier>();
        
        // Prompt for save location
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Gear Modifier",
            "New Gear Modifier",
            "asset",
            "Choose where to save the new modifier");
        
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(newModifier, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            RefreshModifierList();
            
            // Select the newly created asset
            Selection.activeObject = newModifier;
            EditorGUIUtility.PingObject(newModifier);
        }
    }
    
    private void ExpandCollapseAll(bool expand)
    {
        foreach (var modifier in allModifiers)
        {
            if (modifier != null)
            {
                modifierExpandedState[modifier.GetInstanceID()] = expand;
            }
        }
    }
}
