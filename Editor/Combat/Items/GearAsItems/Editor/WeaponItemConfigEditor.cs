using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(WeaponItemDropsConfig))]
public class WeaponItemDropsConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WeaponItemDropsConfig WeaponItemDropsConfig = (WeaponItemDropsConfig)target;
        
        serializedObject.Update();
        
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Add some spacing
        EditorGUILayout.Space(10);
        
        // Add the "Find All Weapons" button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Find All Weapons", GUILayout.Height(30), GUILayout.Width(200)))
        {
            FindAllWeapons(WeaponItemDropsConfig);
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        // Show count of weapons
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox($"Currently configured with {WeaponItemDropsConfig.weaponConfigs.Count} weapon(s)", MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void FindAllWeapons(WeaponItemDropsConfig config)
    {
        // Find all WeaponConfig assets in the project
        string[] guids = AssetDatabase.FindAssets("t:WeaponConfig");
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Weapons Found", 
                "No WeaponConfig assets were found in the project.", "OK");
            return;
        }
        
        // Load all weapon configs
        List<WeaponConfig> foundWeapons = new List<WeaponConfig>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponConfig weaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            
            if (weaponConfig != null)
            {
                foundWeapons.Add(weaponConfig);
            }
        }
        
        // Sort by name for consistency
        foundWeapons = foundWeapons.OrderBy(w => w.weaponName).ToList();
        
        // Ask user if they want to replace or add to existing list
        int option = EditorUtility.DisplayDialogComplex(
            "Found " + foundWeapons.Count + " Weapons",
            $"Found {foundWeapons.Count} WeaponConfig asset(s) in the project.\n\n" +
            "Replace: Clear existing list and add all found weapons\n" +
            "Add: Keep existing weapons and add new ones (no duplicates)\n" +
            "Cancel: Do nothing",
            "Replace",
            "Cancel",
            "Add"
        );
        
        if (option == 1) // Cancel
        {
            return;
        }
        
        // Record undo
        Undo.RecordObject(config, "Find All Weapons");
        
        if (option == 0) // Replace
        {
            config.weaponConfigs = foundWeapons;
            Debug.Log($"[WeaponItemDropsConfigEditor] Replaced weapon list with {foundWeapons.Count} weapons");
        }
        else if (option == 2) // Add
        {
            // Add only new weapons that aren't already in the list
            int addedCount = 0;
            foreach (WeaponConfig weapon in foundWeapons)
            {
                if (!config.weaponConfigs.Contains(weapon))
                {
                    config.weaponConfigs.Add(weapon);
                    addedCount++;
                }
            }
            Debug.Log($"[WeaponItemDropsConfigEditor] Added {addedCount} new weapon(s) to the list");
        }
        
        // Mark as dirty and save
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        
        // Show success message
        EditorUtility.DisplayDialog("Success", 
            $"Weapon list updated!\nTotal weapons: {config.weaponConfigs.Count}", "OK");
    }
}
