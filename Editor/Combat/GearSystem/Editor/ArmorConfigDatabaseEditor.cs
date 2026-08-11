#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(ArmorConfigDatabase))]
public class ArmorConfigDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ArmorConfigDatabase database = (ArmorConfigDatabase)target;
        
        EditorGUILayout.HelpBox(
            "This database must be located at: Assets/Resources/ArmorConfigDatabase.asset\n" +
            "Add ArmorConfig assets to the list below. DO NOT manually delete entries.",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Draw default list inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // Utility buttons
        EditorGUILayout.LabelField("Maintenance Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Clean Null Entries", GUILayout.Height(30)))
        {
            database.CleanNullEntries();
            EditorUtility.DisplayDialog("Clean Complete", 
                "Removed all null entries from the database.", "OK");
        }
        
        if (GUILayout.Button("Find All ArmorConfigs in Project", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Auto-Populate Database?",
                "This will search the entire project for ArmorConfig assets and add any missing ones to the database. Continue?",
                "Yes", "Cancel"))
            {
                AutoPopulateDatabase(database);
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Total Configs: {database.AllArmorConfigs.Count}", EditorStyles.miniLabel);
    }
    
    private void AutoPopulateDatabase(ArmorConfigDatabase database)
    {
        // Find all ArmorConfig assets in the project
        string[] guids = AssetDatabase.FindAssets("t:ArmorConfig");
        int addedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArmorConfig config = AssetDatabase.LoadAssetAtPath<ArmorConfig>(path);
            
            if (config != null && !database.AllArmorConfigs.Contains(config))
            {
                database.AddConfig(config);
                addedCount++;
            }
        }
        
        if (addedCount > 0)
        {
            EditorUtility.DisplayDialog("Auto-Populate Complete",
                $"Added {addedCount} ArmorConfig(s) to the database.\n" +
                $"Total configs: {database.AllArmorConfigs.Count}",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Auto-Populate Complete",
                "All ArmorConfigs are already in the database.",
                "OK");
        }
    }
}
#endif
