using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for spawning items in the scene during development.
/// Finds all ItemConfig, WeaponConfig, and ArmorConfig assets and allows dragging them into the scene to spawn WorldItems.
/// </summary>
public class ItemSpawnerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ItemConfig> itemConfigs = new List<ItemConfig>();
    private List<WeaponConfig> weaponConfigs = new List<WeaponConfig>();
    private List<ArmorConfig> armorConfigs = new List<ArmorConfig>();
    private object draggedItem; // Can be ItemConfig, WeaponConfig, or ArmorConfig
    private string searchFilter = "";
    private bool autoGenerateItem = true;
    private int rarityOverride = -1; // -1 = use random
    
    // Sorting
    private enum SortMode { Name, WeaponType, ArmorClass, ArmorSlot, Tier }
    private SortMode currentSortMode = SortMode.Name;
    
    // Category foldouts
    private bool showItemConfigs = true;
    private bool showWeaponConfigs = true;
    private bool showArmorConfigs = true;
    
    [MenuItem("Tools/Item Spawner")]
    public static void ShowWindow()
    {
        var window = GetWindow<ItemSpawnerWindow>("Item Spawner");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }
    
    private void OnEnable()
    {
        RefreshItemList();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        
        // Header
        EditorGUILayout.LabelField("Item Spawner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Drag configs from the list into the Scene view to spawn items", EditorStyles.helpBox);
        
        EditorGUILayout.Space(10);
        
        // Settings
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);
        
        GUIContent autoGenLabel = new GUIContent("Auto-Generate Item", "Generate a procedural item when spawning (uses ItemConfig.GenerateItem())");
        autoGenerateItem = EditorGUILayout.Toggle(autoGenLabel, autoGenerateItem);
        
        string[] rarityOptions = new string[] { "Random", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        GUIContent rarityLabel = new GUIContent("Rarity Override", "Force a specific rarity tier (only when Auto-Generate is enabled)");
        rarityOverride = EditorGUILayout.Popup(rarityLabel, rarityOverride + 1, rarityOptions) - 1;
        
        EditorGUILayout.EndVertical();
        
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
            RefreshItemList();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUIContent sortLabel = new GUIContent("Sort By:", "Choose how to sort the item lists");
        currentSortMode = (SortMode)EditorGUILayout.EnumPopup(sortLabel, currentSortMode);
        if (GUILayout.Button("Apply Sort", GUILayout.Width(120)))
        {
            ApplySorting();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Item list
        int totalCount = itemConfigs.Count + weaponConfigs.Count + armorConfigs.Count;
        EditorGUILayout.LabelField($"Found {totalCount} Configs:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // ItemConfigs Category
        if (itemConfigs.Count > 0)
        {
            showItemConfigs = EditorGUILayout.Foldout(showItemConfigs, $"Item Configs ({itemConfigs.Count})", true, EditorStyles.foldoutHeader);
            if (showItemConfigs)
            {
                var filteredItemConfigs = string.IsNullOrEmpty(searchFilter) 
                    ? itemConfigs.Where(i => i != null).ToList()
                    : itemConfigs.Where(i => i != null && i.name.ToLower().Contains(searchFilter.ToLower())).ToList();
                
                foreach (var config in filteredItemConfigs)
                {
                    if (config != null)
                        DrawConfigEntry(config, GetInventorySprite(config));
                }
            }
            EditorGUILayout.Space(5);
        }
        
        // WeaponConfigs Category
        if (weaponConfigs.Count > 0)
        {
            showWeaponConfigs = EditorGUILayout.Foldout(showWeaponConfigs, $"Weapon Configs ({weaponConfigs.Count})", true, EditorStyles.foldoutHeader);
            if (showWeaponConfigs)
            {
                var filteredWeaponConfigs = string.IsNullOrEmpty(searchFilter) 
                    ? weaponConfigs.Where(w => w != null).ToList()
                    : weaponConfigs.Where(w => w != null && w.weaponName.ToLower().Contains(searchFilter.ToLower())).ToList();
                
                foreach (var config in filteredWeaponConfigs)
                {
                    if (config != null)
                        DrawConfigEntry(config, config.inventorySprite);
                }
            }
            EditorGUILayout.Space(5);
        }
        
        // ArmorConfigs Category
        if (armorConfigs.Count > 0)
        {
            showArmorConfigs = EditorGUILayout.Foldout(showArmorConfigs, $"Armor Configs ({armorConfigs.Count})", true, EditorStyles.foldoutHeader);
            if (showArmorConfigs)
            {
                var filteredArmorConfigs = string.IsNullOrEmpty(searchFilter) 
                    ? armorConfigs.Where(a => a != null).ToList()
                    : armorConfigs.Where(a => a != null && a.gearName.ToLower().Contains(searchFilter.ToLower())).ToList();
                
                foreach (var config in filteredArmorConfigs)
                {
                    if (config != null)
                        DrawConfigEntry(config, config.inventorySprite);
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // Instructions
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Drag any config from the list into the Scene view to spawn it at the cursor position.\n\n" +
            "Or click 'Spawn' to create at the scene view center.\n\n" +
            "Supports ItemConfigs, WeaponConfigs, and ArmorConfigs.", MessageType.Info);
    }
    
    private void RefreshItemList()
    {
        itemConfigs.Clear();
        weaponConfigs.Clear();
        armorConfigs.Clear();
        
        // Find all ItemConfig assets
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemConfig");
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemConfig config = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
            if (config != null)
            {
                itemConfigs.Add(config);
            }
        }
        
        // Find all WeaponConfig assets
        string[] weaponGuids = AssetDatabase.FindAssets("t:WeaponConfig");
        foreach (string guid in weaponGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (config != null)
            {
                weaponConfigs.Add(config);
            }
        }
        
        // Find all ArmorConfig assets
        string[] armorGuids = AssetDatabase.FindAssets("t:ArmorConfig");
        foreach (string guid in armorGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArmorConfig config = AssetDatabase.LoadAssetAtPath<ArmorConfig>(path);
            if (config != null)
            {
                armorConfigs.Add(config);
            }
        }
        
        // Apply current sorting
        ApplySorting();
        
        // Remove any null entries that may have appeared
        itemConfigs.RemoveAll(c => c == null);
        weaponConfigs.RemoveAll(c => c == null);
        armorConfigs.RemoveAll(c => c == null);
        
        int totalCount = itemConfigs.Count + weaponConfigs.Count + armorConfigs.Count;
        Debug.Log($"[ItemSpawner] Found {itemConfigs.Count} ItemConfigs, {weaponConfigs.Count} WeaponConfigs, {armorConfigs.Count} ArmorConfigs (Total: {totalCount})");
    }
    
    private void ApplySorting()
    {
        switch (currentSortMode)
        {
            case SortMode.Name:
                itemConfigs = itemConfigs.OrderBy(i => i.name).ToList();
                weaponConfigs = weaponConfigs.OrderBy(w => w.weaponName).ToList();
                armorConfigs = armorConfigs.OrderBy(a => a.gearName).ToList();
                break;
                
            case SortMode.WeaponType:
                weaponConfigs = weaponConfigs.OrderBy(w => w.weaponType).ThenBy(w => w.weaponName).ToList();
                armorConfigs = armorConfigs.OrderBy(a => a.gearName).ToList();
                itemConfigs = itemConfigs.OrderBy(i => i.name).ToList();
                break;
                
            case SortMode.ArmorClass:
                armorConfigs = armorConfigs.OrderBy(a => a.armorClass).ThenBy(a => a.gearName).ToList();
                weaponConfigs = weaponConfigs.OrderBy(w => w.weaponName).ToList();
                itemConfigs = itemConfigs.OrderBy(i => i.name).ToList();
                break;
                
            case SortMode.ArmorSlot:
                armorConfigs = armorConfigs.OrderBy(a => a.armorSlot).ThenBy(a => a.gearName).ToList();
                weaponConfigs = weaponConfigs.OrderBy(w => w.weaponName).ToList();
                itemConfigs = itemConfigs.OrderBy(i => i.name).ToList();
                break;
                
            case SortMode.Tier:
                itemConfigs = itemConfigs.OrderBy(i => i.baseTierAvailable).ThenBy(i => i.name).ToList();
                weaponConfigs = weaponConfigs.OrderBy(w => w.advancementLevel).ThenBy(w => w.weaponName).ToList();
                armorConfigs = armorConfigs.OrderBy(a => a.advancementLevel).ThenBy(a => a.gearName).ToList();
                break;
        }
    }
    
    private void DrawConfigEntry(object config, Sprite sprite)
    {
        if (config == null) return;
        
        // Check if it's a valid Unity Object
        Object unityObj = config as Object;
        if (unityObj == null) return;
        
        EditorGUILayout.BeginVertical("box");
        
        // Top row: Icon, Name/Type, Spawn Button
        EditorGUILayout.BeginHorizontal();
        
        // Item icon (if available)
        if (sprite != null)
        {
            Rect iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
            
            // Calculate the UV coordinates for the sprite within the texture
            Rect texCoords = new Rect(
                sprite.textureRect.x / sprite.texture.width,
                sprite.textureRect.y / sprite.texture.height,
                sprite.textureRect.width / sprite.texture.width,
                sprite.textureRect.height / sprite.texture.height
            );
            
            // Calculate aspect ratio and adjust rect to maintain it
            float aspectRatio = sprite.textureRect.width / sprite.textureRect.height;
            Rect adjustedRect = iconRect;
            
            if (aspectRatio > 1f)
            {
                // Wider than tall
                float height = iconRect.height / aspectRatio;
                adjustedRect.y += (iconRect.height - height) * 0.5f;
                adjustedRect.height = height;
            }
            else if (aspectRatio < 1f)
            {
                // Taller than wide
                float width = iconRect.width * aspectRatio;
                adjustedRect.x += (iconRect.width - width) * 0.5f;
                adjustedRect.width = width;
            }
            
            GUI.DrawTextureWithTexCoords(adjustedRect, sprite.texture, texCoords);
        }
        else
        {
            GUILayout.Space(32);
        }
        
        // Item name and type
        EditorGUILayout.BeginVertical();
        string displayName = GetConfigDisplayName(config);
        string typeName = config.GetType().Name;
        
        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // Select button to ping the asset in the project
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeObject = config as Object;
            EditorGUIUtility.PingObject(config as Object);
        }
        
        // Spawn button
        if (GUILayout.Button("Spawn", GUILayout.Width(60)))
        {
            SpawnItemInSceneView(config);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Handle drag and drop for the header row
        Rect headerRect = GUILayoutUtility.GetLastRect();
        HandleDragAndDrop(headerRect, config);
        
        // Editable fields section
        DrawEditableFields(config);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawEditableFields(object config)
    {
        if (config == null) return;
        
        // Ensure we have a valid Unity Object
        Object unityObj = config as Object;
        if (unityObj == null) return;
        
        EditorGUI.BeginChangeCheck();
        
        // Store original label width and set it to a smaller value
        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 45f; // Compact label width
        
        try
        {
            if (config is WeaponConfig weaponConfig && weaponConfig != null)
            {
                SerializedObject serializedWeapon = new SerializedObject(weaponConfig);
                serializedWeapon.Update();
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                SerializedProperty weaponNameProp = serializedWeapon.FindProperty("weaponName");
                SerializedProperty weaponTypeNameProp = serializedWeapon.FindProperty("weaponType");
                SerializedProperty tierProp = serializedWeapon.FindProperty("advancementLevel");
                
                EditorGUILayout.PropertyField(weaponNameProp, new GUIContent("Name"));
                EditorGUILayout.PropertyField(weaponTypeNameProp, new GUIContent("Type"));
                EditorGUILayout.PropertyField(tierProp, new GUIContent("Adv"));
                
                EditorGUILayout.EndHorizontal();
                
                if (serializedWeapon.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(weaponConfig);
                }
            }
            else if (config is ArmorConfig armorConfig && armorConfig != null)
            {
                SerializedObject serializedArmor = new SerializedObject(armorConfig);
                serializedArmor.Update();
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                SerializedProperty gearNameProp = serializedArmor.FindProperty("gearName");
                SerializedProperty armorClassProp = serializedArmor.FindProperty("armorClass");
                SerializedProperty armorSlotProp = serializedArmor.FindProperty("armorSlot");
                SerializedProperty tierProp = serializedArmor.FindProperty("advancementLevel");
                
                EditorGUILayout.PropertyField(gearNameProp, new GUIContent("Name"));
                EditorGUILayout.PropertyField(armorClassProp, new GUIContent("Class"));
                EditorGUILayout.PropertyField(armorSlotProp, new GUIContent("Slot"));
                EditorGUILayout.PropertyField(tierProp, new GUIContent("Adv"));
                
                EditorGUILayout.EndHorizontal();
                
                if (serializedArmor.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(armorConfig);
                }
            }
            else if (config is ItemConfig itemConfig && itemConfig != null)
            {
                SerializedObject serializedItem = new SerializedObject(itemConfig);
                serializedItem.Update();
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                SerializedProperty tierProp = serializedItem.FindProperty("baseTierAvailable");
                
                if (tierProp != null)
                {
                    EditorGUILayout.PropertyField(tierProp, new GUIContent("Tier Available"));
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (serializedItem.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(itemConfig);
                }
            }
        }
        finally
        {
            // Restore original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            AssetDatabase.SaveAssets();
        }
    }
    
    private string GetConfigDisplayName(object config)
    {
        if (config == null) return "Null Config";
        
        if (config is WeaponConfig weaponConfig && weaponConfig != null)
            return weaponConfig.weaponName;
        if (config is ArmorConfig armorConfig && armorConfig != null)
            return armorConfig.gearName;
        if (config is ItemConfig itemConfig && itemConfig != null)
            return itemConfig.name;
        
        return "Unknown";
    }
    
    private void HandleDragAndDrop(Rect rect, object config)
    {
        Event evt = Event.current;
        
        if (rect.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                // Start drag
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { config as Object };
                DragAndDrop.StartDrag(GetConfigDisplayName(config));
                draggedItem = config;
                evt.Use();
            }
        }
    }
    
    private void SpawnItemInSceneView(object config)
    {
        if (config == null) return;
        
        // Get spawn position (scene view center or mouse position)
        Vector3 spawnPosition = GetSceneViewSpawnPosition();
        
        // Create the world item
        CreateWorldItem(config, spawnPosition);
    }
    
    private Vector3 GetSceneViewSpawnPosition()
    {
        // Try to get scene view camera position
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            return sceneView.camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        }
        
        // Fallback to origin
        return Vector3.zero;
    }
    
    private void CreateWorldItem(object config, Vector3 position)
    {
        // If in play mode, spawn at player's position
        if (UnityEditor.EditorApplication.isPlaying)
        {
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                position = player.transform.position;
                Debug.Log($"[ItemSpawner] Play mode detected - spawning at player position: {position}");
            }
        }
        
        ItemInstance itemInstance = null;
        
        // Generate item instance based on config type
        if (config is ItemConfig itemConfig)
        {
            if (autoGenerateItem)
            {
                int contextLevel = 1;
                itemInstance = itemConfig.GenerateItem(contextLevel);
                
                // Override rarity if specified
                if (rarityOverride >= 0 && itemInstance != null)
                {
                    itemInstance.rarityTier = rarityOverride;
                    
                    // Update display name with new rarity
                    string rarityName = itemConfig.GetRarityName(rarityOverride);
                    string baseName = itemInstance.displayName;
                    
                    // Strip existing rarity prefix if present
                    foreach (int tier in System.Linq.Enumerable.Range(0, 6))
                    {
                        string prefix = itemConfig.GetRarityName(tier) + " ";
                        if (baseName.StartsWith(prefix))
                        {
                            baseName = baseName.Substring(prefix.Length);
                            break;
                        }
                    }
                    
                    itemInstance.displayName = $"{rarityName} {baseName}";
                }
            }
            else
            {
                // Create basic instance without generation
                itemInstance = new ItemInstance(itemConfig.GetType().Name.Replace("Config", ""), 
                    itemConfig.name, 0, 1);
            }
        }
        else if (config is WeaponConfig weaponConfig)
        {
            // Use ItemGenerator to create weapon item
            int rarity = rarityOverride >= 0 ? rarityOverride : 0;
            itemInstance = ItemGenerator.GenerateWeaponFromConfig(weaponConfig, rarity);
        }
        else if (config is ArmorConfig armorConfig)
        {
            // Use ItemGenerator to create armor item
            int rarity = rarityOverride >= 0 ? rarityOverride : 0;
            itemInstance = ItemGenerator.GenerateArmorFromConfig(armorConfig, rarity);
        }
        
        if (itemInstance == null)
        {
            Debug.LogError($"[ItemSpawner] Failed to generate item from {GetConfigDisplayName(config)}");
            return;
        }
        
        // Create GameObject
        string configName = GetConfigDisplayName(config);
        GameObject worldItemObj = new GameObject($"WorldItem_{configName}");
        worldItemObj.transform.position = position;
        
        // Set layer
        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer >= 0)
        {
            worldItemObj.layer = itemLayer;
        }
        
        // Add SpriteRenderer
        SpriteRenderer sr = worldItemObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Item";
        sr.sortingOrder = 5;
        
        // Add Collider
        CircleCollider2D collider = worldItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        
        // Add WorldItem component
        WorldItem worldItem = worldItemObj.AddComponent<WorldItem>();
        worldItem.Initialize(itemInstance);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(worldItemObj, "Spawn Item");
        
        // Select the created object
        Selection.activeGameObject = worldItemObj;
        
        Debug.Log($"[ItemSpawner] Spawned {itemInstance.displayName} at {position}");
    }
    
    private Sprite GetInventorySprite(ItemConfig config)
    {
        if (config == null) return null;
        
        // Check if it's a GearItemConfig (has inventorySprite field)
        if (config is GearItemConfig gearConfig)
        {
            return gearConfig.inventorySprite;
        }
        
        return null;
    }
    
    /// <summary>
    /// Handle drop from drag and drop into Scene view
    /// </summary>
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private static void OnSceneGUI(SceneView sceneView)
    {
        Event evt = Event.current;
        
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            // Check if we're dragging a config
            if (DragAndDrop.objectReferences.Length > 0)
            {
                var obj = DragAndDrop.objectReferences[0];
                if (obj is ItemConfig || obj is WeaponConfig || obj is ArmorConfig)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        // Get mouse world position
                        Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        Plane plane = new Plane(Vector3.forward, Vector3.zero);
                        
                        if (plane.Raycast(ray, out float distance))
                        {
                            Vector3 spawnPosition = ray.GetPoint(distance);
                            
                            // Find the window to get settings
                            var windows = Resources.FindObjectsOfTypeAll<ItemSpawnerWindow>();
                            if (windows.Length > 0)
                            {
                                windows[0].CreateWorldItem(obj, spawnPosition);
                            }
                            else
                            {
                                // Fallback: create with default settings
                                CreateWorldItemStatic(obj, spawnPosition);
                            }
                        }
                        
                        evt.Use();
                    }
                }
            }
        }
    }
    
    private static void CreateWorldItemStatic(object config, Vector3 position)
    {
        // If in play mode, spawn at player's position
        if (UnityEditor.EditorApplication.isPlaying)
        {
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                position = player.transform.position;
                Debug.Log($"[ItemSpawner] Play mode detected - spawning at player position: {position}");
            }
        }
        
        ItemInstance itemInstance = null;
        
        // Generate item based on config type
        if (config is ItemConfig itemConfig)
        {
            itemInstance = itemConfig.GenerateItem(1);
        }
        else if (config is WeaponConfig weaponConfig)
        {
            itemInstance = ItemGenerator.GenerateWeaponFromConfig(weaponConfig, 0);
        }
        else if (config is ArmorConfig armorConfig)
        {
            itemInstance = ItemGenerator.GenerateArmorFromConfig(armorConfig, 0);
        }
        
        if (itemInstance == null)
        {
            Debug.LogError($"[ItemSpawner] Failed to generate item from config");
            return;
        }
        
        // Create GameObject
        string configName = config is WeaponConfig wc ? wc.weaponName : 
                           config is ArmorConfig ac ? ac.gearName :
                           config is ItemConfig ic ? ic.name : "Item";
        
        GameObject worldItemObj = new GameObject($"WorldItem_{configName}");
        worldItemObj.transform.position = position;
        
        // Set layer
        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer >= 0)
        {
            worldItemObj.layer = itemLayer;
        }
        
        // Add SpriteRenderer
        SpriteRenderer sr = worldItemObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Item";
        sr.sortingOrder = 5;
        
        // Add Collider
        CircleCollider2D collider = worldItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        
        // Add WorldItem component
        WorldItem worldItem = worldItemObj.AddComponent<WorldItem>();
        worldItem.Initialize(itemInstance);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(worldItemObj, "Spawn Item");
        
        // Select the created object
        Selection.activeGameObject = worldItemObj;
        
        Debug.Log($"[ItemSpawner] Spawned {itemInstance.displayName} at {position}");
    }
}
