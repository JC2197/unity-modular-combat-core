using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor for GearModifierDatabase with recalculate tier button
/// </summary>
[CustomEditor(typeof(GearModifierDatabase))]
public class GearModifierDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GearModifierDatabase database = (GearModifierDatabase)target;

        serializedObject.Update();

        // Quick actions at the top
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        // Show current modifier count
        int modifierCount = database.modifiers != null ? database.modifiers.Count : 0;
        EditorGUILayout.LabelField($"Modifiers in Database: {modifierCount}", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Import All Gear Modifiers", GUILayout.Height(30)))
        {
            ImportAllModifiers(database);
        }

        if (GUILayout.Button("Open Gear Modifier Manager", GUILayout.Height(30)))
        {
            GearModifierWindow.ShowWindow();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // Modifier breakdown by tier and slot
        if (database.modifiers != null && database.modifiers.Count > 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Database Statistics", EditorStyles.boldLabel);

            // Count by tier
            int[] tierCounts = new int[6];
            for (int i = 0; i < tierCounts.Length; i++)
            {
                ItemTier tier = (ItemTier)(i + 1);
                tierCounts[i] = 0;
                foreach (var mod in database.modifiers)
                {
                    if (mod != null && mod.IsValidForTier(tier))
                        tierCounts[i]++;
                }
            }

            EditorGUILayout.LabelField("Available by Map Tier:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < tierCounts.Length; i++)
            {
                ItemTier tier = (ItemTier)(i + 1);
                EditorGUILayout.LabelField($"Tier {tier}: {tierCounts[i]} modifiers");
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // Count by slot
            var slotCounts = new Dictionary<GearSlot, int>();
            foreach (GearSlot slot in System.Enum.GetValues(typeof(GearSlot)))
            {
                int count = 0;
                foreach (var mod in database.modifiers)
                {
                    if (mod != null && mod.IsValidForSlot(slot))
                        count++;
                }
                if (count > 0)
                    slotCounts[slot] = count;
            }

            EditorGUILayout.LabelField("Available by Slot:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var kvp in slotCounts)
            {
                EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value} modifiers");
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        else if (database.modifiers == null || database.modifiers.Count == 0)
        {
            EditorGUILayout.HelpBox("⚠ Database is empty! Click 'Import All Gear Modifiers' to populate it.", MessageType.Warning);
            EditorGUILayout.Space(10);
        }

        // Recalculate button
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tier Recalculation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click this button to recalculate all Tier II-VI values for all modifiers in this database. Each modifier uses its own TierScalingConfig. This will overwrite existing tier values!", MessageType.Info);

        if (GUILayout.Button("Recalculate All Tier Values", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Recalculate Tier Values",
                "This will recalculate all Tier II-VI values for all prefixes and suffixes in this database.\n\nEach modifier will use its assigned TierScalingConfig.\n\nThis action cannot be undone. Continue?",
                "Recalculate", "Cancel"))
            {
                EditorUtility.SetDirty(database);
            }
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void ImportAllModifiers(GearModifierDatabase database)
    {
        // Find all GearModifier assets
        string[] guids = AssetDatabase.FindAssets("t:GearModifier");

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Modifiers Found",
                "No GearModifier assets found in the project.\n\nCreate modifiers using the Gear Modifier Manager window.",
                "OK");
            return;
        }

        // Initialize list if needed
        if (database.modifiers == null)
        {
            database.modifiers = new System.Collections.Generic.List<GearModifier>();
        }

        int addedCount = 0;
        int skippedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GearModifier modifier = AssetDatabase.LoadAssetAtPath<GearModifier>(path);

            if (modifier != null)
            {
                // Check if already in database
                if (!database.modifiers.Contains(modifier))
                {
                    database.modifiers.Add(modifier);
                    addedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        // Show result
        string message = $"Import Complete!\n\n" +
                        $"Added: {addedCount} modifiers\n" +
                        $"Skipped (already in database): {skippedCount}\n" +
                        $"Total in database: {database.modifiers.Count}";

        EditorUtility.DisplayDialog("Import Complete", message, "OK");

        Debug.Log($"[GearModifierDatabase] Imported {addedCount} new modifiers. Total: {database.modifiers.Count}");
    }
}