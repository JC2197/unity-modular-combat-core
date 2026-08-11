using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor window for creating new armor pieces.
/// Creates the prefab, ArmorConfig asset, and AnimatorOverrideController for a given name.
/// </summary>
public class ArmorCreatorWindow : EditorWindow
{
    private string armorName = "";
    private ArmorSlot armorSlot = ArmorSlot.Legs;
    private ArmorClass armorClass = ArmorClass.Medium;
    
    // Base controller GUIDs for each slot type
    private const string MEDIUM_FEET_CONTROLLER_GUID = "eded09eafcf474d46b80e15344631154";
    private const string MEDIUM_CHEST_CONTROLLER_GUID = ""; // Will need to be found
    private const string MEDIUM_HEAD_CONTROLLER_GUID = ""; // Will need to be found
    
    // Default material GUID (Sprite-Lit-Default or similar)
    private const string DEFAULT_SPRITE_MATERIAL_GUID = "d48ae462817865e4581179605edfc750";
    
    private Vector2 scrollPosition;
    private string statusMessage = "";
    private MessageType statusType = MessageType.None;

    [MenuItem("Tools/Armor Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<ArmorCreatorWindow>("Armor Creator");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Create New Armor Piece", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "Enter a name and select the armor slot/class. This will create:\n" +
            "• ArmorConfig ScriptableObject (.asset)\n" +
            "• Prefab with appropriate GearPiece component (.prefab)\n" +
            "• AnimatorOverrideController (.overrideController)",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Input fields
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        armorName = EditorGUILayout.TextField("Armor Name", armorName);
        armorSlot = (ArmorSlot)EditorGUILayout.EnumPopup("Armor Slot", armorSlot);
        armorClass = (ArmorClass)EditorGUILayout.EnumPopup("Armor Class", armorClass);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Show where files will be created
        if (!string.IsNullOrEmpty(armorName))
        {
            string sanitizedName = SanitizeName(armorName);
            string basePath = GetBasePath();
            
            EditorGUILayout.LabelField("Files to be created:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Config: {basePath}/{sanitizedName}.asset");
            EditorGUILayout.LabelField($"Prefab: {basePath}/{sanitizedName}.prefab");
            EditorGUILayout.LabelField($"Override: {basePath}/{sanitizedName}Override.overrideController");
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(10);
        
        // Create button
        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(armorName));
        
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        
        if (GUILayout.Button("Create New Armor Piece", GUILayout.Height(40)))
        {
            CreateArmorPiece();
        }
        
        GUI.backgroundColor = originalColor;
        EditorGUI.EndDisabledGroup();
        
        // Status message
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
    }

    private string GetBasePath()
    {
        string slotFolder = armorSlot switch
        {
            ArmorSlot.Head => "Head",
            ArmorSlot.Chest => "Chest",
            ArmorSlot.Legs => "Legs",
            ArmorSlot.Hands => "Hands",
            ArmorSlot.Backpack => "Backpack",
            _ => "Other"
        };
        
        string classFolder = armorClass switch
        {
            ArmorClass.Light => "Light",
            ArmorClass.Medium => "Medium",
            ArmorClass.Heavy => "Heavy",
            _ => "Medium"
        };
        
        string sanitizedName = SanitizeName(armorName);
        return $"Assets/Items/Gear/Armor/{classFolder}/{sanitizedName}Set/{slotFolder}";
    }

    private string SanitizeName(string name)
    {
        // Remove spaces and special characters, keep PascalCase
        string result = "";
        bool capitalizeNext = true;
        
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                result += capitalizeNext ? char.ToUpper(c) : c;
                capitalizeNext = false;
            }
            else if (c == ' ')
            {
                capitalizeNext = true;
            }
        }
        
        return result;
    }

    private void CreateArmorPiece()
    {
        string sanitizedName = SanitizeName(armorName);
        
        if (string.IsNullOrEmpty(sanitizedName))
        {
            statusMessage = "Please enter a valid armor name.";
            statusType = MessageType.Error;
            return;
        }

        string basePath = GetBasePath();
        
        // Create directory if it doesn't exist
        string fullDirectoryPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), basePath);
        if (!Directory.Exists(fullDirectoryPath))
        {
            Directory.CreateDirectory(fullDirectoryPath);
            AssetDatabase.Refresh();
        }

        try
        {
            // 1. Create AnimatorOverrideController
            AnimatorOverrideController overrideController = CreateOverrideController(sanitizedName, basePath);
            
            // 2. Create Prefab
            GameObject prefab = CreatePrefab(sanitizedName, basePath, overrideController);
            
            // 3. Create ArmorConfig
            ArmorConfig config = CreateArmorConfig(sanitizedName, basePath, prefab);
            
            // 4. Link config to prefab's GearPiece component
            LinkConfigToPrefab(prefab, config);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created config in the Project window
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            statusMessage = $"Successfully created armor piece '{armorName}'!\n" +
                           $"Config: {sanitizedName}.asset\n" +
                           $"Prefab: {sanitizedName}.prefab\n" +
                           $"Override: {sanitizedName}Override.overrideController";
            statusType = MessageType.Info;
        }
        catch (System.Exception e)
        {
            statusMessage = $"Error creating armor piece: {e.Message}";
            statusType = MessageType.Error;
            Debug.LogError($"[ArmorCreator] Error: {e}");
        }
    }

    private AnimatorOverrideController CreateOverrideController(string sanitizedName, string basePath)
    {
        string overridePath = $"{basePath}/{sanitizedName}Override.overrideController";
        
        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (existing != null)
        {
            Debug.Log($"[ArmorCreator] Override controller already exists at {overridePath}");
            return existing;
        }
        
        // Find the base controller for this slot
        RuntimeAnimatorController baseController = FindBaseController();
        
        if (baseController == null)
        {
            Debug.LogWarning("[ArmorCreator] Could not find base controller, creating override without base.");
        }
        
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        overrideController.runtimeAnimatorController = baseController;
        
        AssetDatabase.CreateAsset(overrideController, overridePath);
        Debug.Log($"[ArmorCreator] Created override controller at {overridePath}");
        
        return overrideController;
    }

    private RuntimeAnimatorController FindBaseController()
    {
        string controllerGuid = armorSlot switch
        {
            ArmorSlot.Legs => MEDIUM_FEET_CONTROLLER_GUID,
            ArmorSlot.Chest => FindControllerGuid("MediumChestController"),
            ArmorSlot.Head => FindControllerGuid("MediumHeadController"),
            ArmorSlot.Hands => MEDIUM_FEET_CONTROLLER_GUID, // Default to feet for now
            _ => MEDIUM_FEET_CONTROLLER_GUID
        };
        
        if (!string.IsNullOrEmpty(controllerGuid))
        {
            string path = AssetDatabase.GUIDToAssetPath(controllerGuid);
            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            }
        }
        
        // Fallback: Search by name
        string searchName = armorSlot switch
        {
            ArmorSlot.Legs => "MediumFeetController",
            ArmorSlot.Chest => "MediumChestController",
            ArmorSlot.Head => "MediumHeadController",
            _ => "MediumFeetController"
        };
        
        string[] guids = AssetDatabase.FindAssets($"t:AnimatorController {searchName}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        }
        
        return null;
    }

    private string FindControllerGuid(string controllerName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimatorController {controllerName}");
        return guids.Length > 0 ? guids[0] : "";
    }

    private GameObject CreatePrefab(string sanitizedName, string basePath, AnimatorOverrideController overrideController)
    {
        string prefabPath = $"{basePath}/{sanitizedName}.prefab";
        
        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log($"[ArmorCreator] Prefab already exists at {prefabPath}");
            return existing;
        }
        
        // Create new GameObject
        GameObject go = new GameObject(sanitizedName);
        
        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
        
        // Try to load default material
        string materialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SPRITE_MATERIAL_GUID);
        if (!string.IsNullOrEmpty(materialPath))
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat != null)
            {
                spriteRenderer.material = mat;
            }
        }
        
        // Add Animator
        Animator animator = go.AddComponent<Animator>();
        
        // If we have an override controller, use it; otherwise find base controller
        if (overrideController != null)
        {
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            animator.runtimeAnimatorController = FindBaseController();
        }
        
        // Add appropriate GearPiece component and connection points based on slot
        AddGearPieceComponent(go, spriteRenderer, animator);
        
        // Add SortingGroup
        go.AddComponent<UnityEngine.Rendering.SortingGroup>();
        
        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        
        // Destroy temporary object
        DestroyImmediate(go);
        
        Debug.Log($"[ArmorCreator] Created prefab at {prefabPath}");
        return prefab;
    }

    private void AddGearPieceComponent(GameObject go, SpriteRenderer spriteRenderer, Animator animator)
    {
        switch (armorSlot)
        {
            case ArmorSlot.Head:
                CreateHeadPrefabStructure(go, spriteRenderer, animator);
                break;
                
            case ArmorSlot.Chest:
                CreateChestPrefabStructure(go, spriteRenderer, animator);
                break;
                
            case ArmorSlot.Legs:
                CreateLegsPrefabStructure(go, spriteRenderer, animator);
                break;
                
            case ArmorSlot.Hands:
                // Add a generic component or create HandsGearPiece if it exists
                var handsLeg = go.AddComponent<LegGearPiece>(); // Using LegGearPiece as fallback
                SetPrivateField(handsLeg, "spriteRenderer", spriteRenderer);
                SetPrivateField(handsLeg, "animator", animator);
                break;
        }
    }

    /// <summary>
    /// Creates the proper structure for a Legs prefab:
    /// - LegGearPiece component
    /// </summary>
    private void CreateLegsPrefabStructure(GameObject go, SpriteRenderer spriteRenderer, Animator animator)
    {
        var legPiece = go.AddComponent<LegGearPiece>();
        SetPrivateField(legPiece, "spriteRenderer", spriteRenderer);
        SetPrivateField(legPiece, "animator", animator);
        
        Debug.Log("[ArmorCreator] Created Legs prefab");
    }

    /// <summary>
    /// Creates the proper structure for a Chest prefab:
    /// - ChestGearPiece component with Y offsets
    /// </summary>
    private void CreateChestPrefabStructure(GameObject go, SpriteRenderer spriteRenderer, Animator animator)
    {
        var chestPiece = go.AddComponent<ChestGearPiece>();
        SetPrivateField(chestPiece, "chestHolderYOffset", 0f);
        SetPrivateField(chestPiece, "headHolderYOffset", 0f);
        
        Debug.Log("[ArmorCreator] Created Chest prefab");
    }

    /// <summary>
    /// Creates the proper structure for a Head prefab:
    /// - HeadGearPiece component
    /// </summary>
    private void CreateHeadPrefabStructure(GameObject go, SpriteRenderer spriteRenderer, Animator animator)
    {
        var headPiece = go.AddComponent<HeadGearPiece>();
        
        Debug.Log("[ArmorCreator] Created Head prefab");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private ArmorConfig CreateArmorConfig(string sanitizedName, string basePath, GameObject prefab)
    {
        string configPath = $"{basePath}/{sanitizedName}.asset";
        
        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<ArmorConfig>(configPath);
        if (existing != null)
        {
            Debug.Log($"[ArmorCreator] Config already exists at {configPath}");
            return existing;
        }
        
        // Create new ArmorConfig
        ArmorConfig config = ScriptableObject.CreateInstance<ArmorConfig>();
        config.gearName = armorName;
        config.armorClass = armorClass;
        config.armorSlot = armorSlot;
        config.rarityTier = 0;
        
        // Set the appropriate prefab field based on slot
        switch (armorSlot)
        {
            case ArmorSlot.Head:
                config.headGearPrefab = prefab;
                break;
            case ArmorSlot.Chest:
                config.chestGearPrefab = prefab;
                break;
            case ArmorSlot.Legs:
                config.legGearPrefab = prefab;
                break;
            case ArmorSlot.Hands:
                config.handsGearPrefab = prefab;
                break;
        }
        
        AssetDatabase.CreateAsset(config, configPath);
        Debug.Log($"[ArmorCreator] Created config at {configPath}");
        
        return config;
    }

    private void LinkConfigToPrefab(GameObject prefab, ArmorConfig config)
    {
        // Load the prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            // Find the GearPiece component and link the config
            switch (armorSlot)
            {
                case ArmorSlot.Head:
                    var headPiece = prefabRoot.GetComponent<HeadGearPiece>();
                    if (headPiece != null)
                    {
                        SetPrivateField(headPiece, "gearConfig", config);
                    }
                    break;
                    
                case ArmorSlot.Chest:
                    var chestPiece = prefabRoot.GetComponent<ChestGearPiece>();
                    if (chestPiece != null)
                    {
                        SetPrivateField(chestPiece, "gearConfig", config);
                    }
                    break;
                    
                case ArmorSlot.Legs:
                    var legPiece = prefabRoot.GetComponent<LegGearPiece>();
                    if (legPiece != null)
                    {
                        SetPrivateField(legPiece, "gearConfig", config);
                    }
                    break;
                    
                case ArmorSlot.Hands:
                    var handsPiece = prefabRoot.GetComponent<LegGearPiece>();
                    if (handsPiece != null)
                    {
                        SetPrivateField(handsPiece, "gearConfig", config);
                    }
                    break;
            }
            
            // Save changes back to prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            // Unload the prefab contents
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        
        Debug.Log($"[ArmorCreator] Linked config to prefab");
    }
}
