using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for DamageTypeDatabase showing sync status with StatTypeDatabase
/// </summary>
[CustomEditor(typeof(DamageTypeDatabase))]
public class DamageTypeDatabaseEditor : Editor
{
    private Vector2 scrollPosition;
    
    public override void OnInspectorGUI()
    {
        DamageTypeDatabase database = (DamageTypeDatabase)target;
        
        // Draw script field
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject(database), typeof(DamageTypeDatabase), false);
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        // Sync status section
        DrawSyncStatus(database);
        
        EditorGUILayout.Space(10);
        
        // Draw default inspector for damage types list
        DrawDefaultInspector();
        
        // Save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(database);
        }
    }
    
    private void DrawSyncStatus(DamageTypeDatabase database)
    {
        EditorGUILayout.LabelField("Stat Sync Status", EditorStyles.boldLabel);
        
        StatTypeDatabase statDB = Resources.Load<StatTypeDatabase>("StatTypeDatabase");
        
        if (statDB == null)
        {
            EditorGUILayout.HelpBox("StatTypeDatabase not found in Resources folder!", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        int syncedCount = 0;
        int totalCount = database.damageTypes.Count;
        
        foreach (var damageType in database.damageTypes)
        {
            if (damageType == null) continue;
            
            string resistanceID = $"{damageType.damageTypeName}Resistance";
            bool hasResistance = statDB.statTypes.Exists(s => s.statID == resistanceID);
            
            string damageID = $"{damageType.damageTypeName}DamageBonus";
            bool hasDamage = damageType.category == DamageCategory.Special && damageType.specialSubcategory == SpecialSubcategory.True
                ? true // True damage doesn't need damage bonus
                : statDB.statTypes.Exists(s => s.statID == damageID);
            
            if (hasResistance && hasDamage)
                syncedCount++;
        }
        
        // Status indicator
        Color statusColor = syncedCount == totalCount ? Color.green : Color.yellow;
        GUI.backgroundColor = statusColor;
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;
        
        if (syncedCount == totalCount)
        {
            EditorGUILayout.LabelField($"✓ All {totalCount} damage types synced with StatTypeDatabase", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"⚠ {syncedCount}/{totalCount} damage types synced", EditorStyles.boldLabel);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Sync button
        if (syncedCount < totalCount)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"{totalCount - syncedCount} damage type(s) are missing corresponding stats in StatTypeDatabase.",
                MessageType.Info
            );
            
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Open Stat Database Manager", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow<StatDatabaseManagerWindow>("Stat DB Manager");
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndVertical();
    }
}
