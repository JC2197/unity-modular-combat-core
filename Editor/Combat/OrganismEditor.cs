using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for Organism to show stat initialization status
/// </summary>
[CustomEditor(typeof(Organism), true)]
[CanEditMultipleObjects]
public class OrganismEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Organism organism = (Organism)target;
        
        if (organism.AllStats != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stat Container Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var allStats = organism.AllStats.GetAllStats();
            
            if (allStats.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "⚠ Stat container is empty! Stats will be initialized when the game starts (Awake).",
                    MessageType.Warning
                );
                
                if (GUILayout.Button("Initialize Stats Now (Editor Only)"))
                {
                    organism.AllStats.InitializeFromDatabase();
                    
                    // Set default crit stats
                    if (organism.AllStats.HasStat("CritChance"))
                    {
                        organism.AllStats.SetStat("CritChance", 0f);
                    }
                    if (organism.AllStats.HasStat("CritDamage"))
                    {
                        organism.AllStats.SetStat("CritDamage", 1.5f);
                    }
                    
                    EditorUtility.SetDirty(organism);
                    Debug.Log($"[OrganismEditor] Initialized stats for {organism.gameObject.name}");
                }
            }
            else
            {
                EditorGUILayout.LabelField($"✓ Stat container initialized with {allStats.Count} stats");
                
                // Show breakdown by category
                int baseCount = organism.AllStats.GetStatsByCategory(StatCategory.Base).Count;
                int offensiveCount = organism.AllStats.GetStatsByCategory(StatCategory.Offensive).Count;
                int defensiveCount = organism.AllStats.GetStatsByCategory(StatCategory.Defensive).Count;
                int specialCount = organism.AllStats.GetStatsByCategory(StatCategory.Special).Count;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  Base: {baseCount}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Offensive: {offensiveCount}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Defensive: {defensiveCount}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Special: {specialCount}");
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Reinitialize from Database"))
                {
                    organism.AllStats.InitializeFromDatabase();
                    
                    // Set default crit stats
                    if (organism.AllStats.HasStat("CritChance"))
                    {
                        organism.AllStats.SetStat("CritChance", 0f);
                    }
                    if (organism.AllStats.HasStat("CritDamage"))
                    {
                        organism.AllStats.SetStat("CritDamage", 1.5f);
                    }
                    
                    EditorUtility.SetDirty(organism);
                    Debug.Log($"[OrganismEditor] Reinitialized stats for {organism.gameObject.name}");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.HelpBox(
                "Stats are automatically initialized from StatTypeDatabase on Awake(). " +
                "All organisms get the same stat structure, ensuring equal footing.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox("Stat container is null!", MessageType.Error);
        }
    }
}
