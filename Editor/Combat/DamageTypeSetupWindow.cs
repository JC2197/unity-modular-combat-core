using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using JoeConticello.ModularCombatCore;

/// <summary>
/// Editor window to create and configure damage types with their associated status effects
/// </summary>
public class DamageTypeSetupWindow : EditorWindow
{
    private const string DefaultFolderPath = "Assets/Data/DamageTypes";

    private string folderPath = DefaultFolderPath;
    private string damageTypeName = "";
    private string displayName = "";
    private string categoryId = "";
    private string categoryName = "";
    private string subcategoryName = "";
    private string tagsCsv = "";
    private string description = "";
    private Color damageColor = Color.white;
    private bool canCriticalHit = true;
    private bool createResistanceStat = true;
    private bool createDamageBonusStat = true;
    private bool ignoreShields = false;

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
            "Create damage types from freeform names, categories, and tags. " +
            "These assets drive dropdowns and stat sync without relying on hardcoded enums.",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);

        DrawCreateForm();

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
    
    private void DrawCreateForm()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Create Damage Type", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Folder", folderPath);
        damageTypeName = EditorGUILayout.TextField("Damage Type Name", damageTypeName);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        categoryId = EditorGUILayout.TextField("Category ID", categoryId);
        categoryName = EditorGUILayout.TextField("Category Name", categoryName);
        subcategoryName = EditorGUILayout.TextField("Subtype", subcategoryName);
        damageColor = EditorGUILayout.ColorField("Color", damageColor);
        canCriticalHit = EditorGUILayout.Toggle("Can Critical Hit", canCriticalHit);
        createResistanceStat = EditorGUILayout.Toggle("Create Resistance Stat", createResistanceStat);
        createDamageBonusStat = EditorGUILayout.Toggle("Create Damage Bonus Stat", createDamageBonusStat);
        ignoreShields = EditorGUILayout.Toggle("Ignores Shields", ignoreShields);
        EditorGUILayout.LabelField("Tags (comma separated)");
        tagsCsv = EditorGUILayout.TextField(tagsCsv);
        EditorGUILayout.LabelField("Description");
        description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(60f));

        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(damageTypeName));
        if (GUILayout.Button("Create Damage Type", GUILayout.Height(32)))
        {
            CreateDamageType();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void CreateDamageType()
    {
        string targetFolderPath = string.IsNullOrWhiteSpace(folderPath) ? DefaultFolderPath : folderPath.Trim();
        
        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "DamageTypes");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DamageTypeData createdType = CreateDamageTypeAsset(targetFolderPath);
        if (createdType == null)
            return;
        
        // Add to DamageTypeDatabase
        DamageTypeDatabase db = Resources.Load<DamageTypeDatabase>("DamageTypeDatabase");
        if (db != null)
        {
            if (!db.damageTypes.Contains(createdType))
            {
                db.damageTypes.Add(createdType);
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }
        
        ResetCreateForm();
        Debug.Log($"Created damage type '{createdType.damageTypeName}' and added it to DamageTypeDatabase.");
    }
    
    private DamageTypeData CreateDamageTypeAsset(string targetFolderPath)
    {
        string normalizedName = damageTypeName.Trim();
        string assetPath = $"{targetFolderPath}/{normalizedName}Damage.asset";
        
        // Check if already exists
        DamageTypeData existing = AssetDatabase.LoadAssetAtPath<DamageTypeData>(assetPath);
        if (existing != null)
        {
            Debug.Log($"Damage type '{normalizedName}' already exists, skipping.");
            return existing;
        }
        
        var damageType = ScriptableObject.CreateInstance<DamageTypeData>();
        damageType.damageTypeName = normalizedName;
        damageType.displayName = string.IsNullOrWhiteSpace(displayName) ? normalizedName : displayName.Trim();
        damageType.categoryId = string.IsNullOrWhiteSpace(categoryId) ? normalizedName : categoryId.Trim();
        damageType.categoryName = string.IsNullOrWhiteSpace(categoryName) ? damageType.categoryId : categoryName.Trim();
        damageType.subcategoryName = string.IsNullOrWhiteSpace(subcategoryName) ? string.Empty : subcategoryName.Trim();
        damageType.description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        damageType.damageColor = damageColor;
        damageType.canCriticalHit = canCriticalHit;
        damageType.createResistanceStat = createResistanceStat;
        damageType.createDamageBonusStat = createDamageBonusStat;
        damageType.ignoresShields = ignoreShields;
        damageType.tags = ParseTags(tagsCsv);
        
        AssetDatabase.CreateAsset(damageType, assetPath);
        EditorUtility.SetDirty(damageType);
        Debug.Log($"Created damage type: {normalizedName}");
        
        return damageType;
    }

    private static List<string> ParseTags(string csv)
    {
        List<string> parsedTags = new List<string>();
        if (string.IsNullOrWhiteSpace(csv))
            return parsedTags;

        HashSet<string> uniqueTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string[] values = csv.Split(',');
        for (int i = 0; i < values.Length; i++)
        {
            string tag = values[i].Trim();
            if (string.IsNullOrWhiteSpace(tag) || !uniqueTags.Add(tag))
                continue;

            parsedTags.Add(tag);
        }

        return parsedTags;
    }

    private void ResetCreateForm()
    {
        damageTypeName = string.Empty;
        displayName = string.Empty;
        categoryId = string.Empty;
        categoryName = string.Empty;
        subcategoryName = string.Empty;
        tagsCsv = string.Empty;
        description = string.Empty;
        damageColor = Color.white;
        canCriticalHit = true;
        createResistanceStat = true;
        createDamageBonusStat = true;
        ignoreShields = false;
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
            EditorGUILayout.LabelField($"Category: {dt.GetCategoryName()}");
            if (!string.IsNullOrWhiteSpace(dt.subcategoryName))
            {
                EditorGUILayout.LabelField($"Subtype: {dt.subcategoryName}");
            }
            if (dt.tags != null && dt.tags.Count > 0)
            {
                EditorGUILayout.LabelField($"Tags: {string.Join(", ", dt.tags)}");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
