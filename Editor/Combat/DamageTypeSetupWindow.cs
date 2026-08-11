using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window to create and configure damage types with their associated status effects
/// </summary>
public class DamageTypeSetupWindow : EditorWindow
{
    [MenuItem("Tools/Damage Type Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<DamageTypeSetupWindow>("Damage Type Setup");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }
    
    private Vector2 scrollPosition;
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Damage Type Configuration", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "This tool creates the damage types with their associated status effects:\n\n" +
            "PHYSICAL:\n" +
            "• Piercing - Slow → Root\n" +
            "• Bludgeoning - Daze → Stun\n" +
            "• Slashing + Bleed (DoT)\n\n" +
            "ELEMENTAL:\n" +
            "• Fire + Burn (DoT)\n" +
            "• Frost - Slow → Root\n" +
            "• Lightning - Daze → Stun\n\n" +
            "MAGICAL:\n" +
            "• Light - Daze → Stun\n" +
            "• Dark - Slow → Root\n" +
            "• Nature + Poison (DoT)",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Create All Damage Types", GUILayout.Height(40)))
        {
            CreateAllDamageTypes();
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sync Stats with Damage Types", GUILayout.Height(30)))
        {
            SyncStatsWithDamageTypes();
        }
        
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Remove All Damage Types", GUILayout.Height(30), GUILayout.Width(200)))
        {
            RemoveAllDamageTypes();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawDamageTypePreview();
        EditorGUILayout.EndScrollView();
    }
    
    private void CreateAllDamageTypes()
    {
        string folderPath = "Assets/Data/DamageTypes";
        
        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "DamageTypes");
        }
        
        List<DamageTypeData> createdTypes = new List<DamageTypeData>();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Add to DamageTypeDatabase
        DamageTypeDatabase db = Resources.Load<DamageTypeDatabase>("DamageTypeDatabase");
        if (db != null)
        {
            foreach (var damageType in createdTypes)
            {
                if (!db.damageTypes.Contains(damageType))
                {
                    db.damageTypes.Add(damageType);
                }
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }
        
        Debug.Log($"Created {createdTypes.Count} damage types and added to DamageTypeDatabase!");
    }
    
    private DamageTypeData CreateDamageType(string folderPath, string name, DamageCategory category,
        PhysicalSubcategory physical, ElementalSubcategory elemental, MagicalSubcategory magical, SpecialSubcategory special,
        StatusEffectType statusEffect, float statusChance, string description, bool ignoreShields)
    {
        string assetPath = $"{folderPath}/{name}Damage.asset";
        
        // Check if already exists
        DamageTypeData existing = AssetDatabase.LoadAssetAtPath<DamageTypeData>(assetPath);
        if (existing != null)
        {
            Debug.Log($"Damage type '{name}' already exists, skipping.");
            return existing;
        }
        
        var damageType = ScriptableObject.CreateInstance<DamageTypeData>();
        damageType.damageTypeName = name;
        damageType.displayName = name;
        damageType.description = description;
        damageType.category = category;
        damageType.physicalSubcategory = physical;
        damageType.elementalSubcategory = elemental;
        damageType.magicalSubcategory = magical;
        damageType.specialSubcategory = special;
        damageType.ignoresShields = ignoreShields;
        
        // Set colors based on category
        switch (category)
        {
            case DamageCategory.Physical:
                damageType.damageColor = new Color(0.8f, 0.4f, 0.2f); // Brown/Orange
                break;
            case DamageCategory.Elemental:
                if (elemental == ElementalSubcategory.Fire)
                    damageType.damageColor = new Color(1f, 0.3f, 0f); // Red/Orange
                else if (elemental == ElementalSubcategory.Ice)
                    damageType.damageColor = new Color(0.3f, 0.8f, 1f); // Cyan
                else if (elemental == ElementalSubcategory.Lightning)
                    damageType.damageColor = new Color(1f, 1f, 0.3f); // Yellow
                else if (elemental == ElementalSubcategory.Poison)
                    damageType.damageColor = new Color(0.4f, 0.8f, 0.2f); // Green
                break;
            case DamageCategory.Magical:
                if (magical == MagicalSubcategory.Holy)
                    damageType.damageColor = new Color(1f, 1f, 0.8f); // Light Yellow
                else if (magical == MagicalSubcategory.Dark)
                    damageType.damageColor = new Color(0.4f, 0.2f, 0.6f); // Purple
                break;
        }
        
        AssetDatabase.CreateAsset(damageType, assetPath);
        Debug.Log($"Created damage type: {name}");
        
        return damageType;
    }
    
    private void SyncStatsWithDamageTypes()
    {
        StatTypeDatabase statDB = Resources.Load<StatTypeDatabase>("StatTypeDatabase");
        if (statDB != null)
        {
            statDB.SyncWithDamageTypes();
            AssetDatabase.SaveAssets();
            Debug.Log("Synced StatTypeDatabase with damage types!");
        }
        else
        {
            Debug.LogError("StatTypeDatabase not found in Resources!");
        }
    }
    
    private void RemoveAllDamageTypes()
    {
        if (!EditorUtility.DisplayDialog("Remove All Damage Types?",
            "This will remove all damage types from the database and delete their asset files. This action cannot be undone!",
            "Remove All", "Cancel"))
        {
            return;
        }
        
        DamageTypeDatabase db = Resources.Load<DamageTypeDatabase>("DamageTypeDatabase");
        if (db == null)
        {
            Debug.LogError("DamageTypeDatabase not found in Resources!");
            return;
        }
        
        int removedCount = 0;
        List<DamageTypeData> typesToRemove = new List<DamageTypeData>(db.damageTypes);
        
        foreach (var damageType in typesToRemove)
        {
            if (damageType != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(damageType);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    removedCount++;
                }
            }
        }
        
        db.damageTypes.Clear();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Removed {removedCount} damage types and cleared DamageTypeDatabase!");
    }
    
    private void DrawDamageTypePreview()
    {
        EditorGUILayout.LabelField("Damage Type Configuration Preview", EditorStyles.boldLabel);
        
        DamageTypeDatabase db = Resources.Load<DamageTypeDatabase>("DamageTypeDatabase");
        if (db == null)
        {
            EditorGUILayout.HelpBox("DamageTypeDatabase not found in Resources!", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField($"Current Damage Types: {db.damageTypes.Count}");
        
        foreach (var dt in db.damageTypes)
        {
            if (dt == null) continue;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(dt.displayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Category: {dt.category}");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
