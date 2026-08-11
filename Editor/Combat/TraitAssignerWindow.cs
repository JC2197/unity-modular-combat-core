using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for assigning/removing traits on the local player at runtime.
/// Open via Tools → Trait Assigner.
/// </summary>
public class TraitAssignerWindow : EditorWindow
{
    private TraitDataList traitDataList;
    private Vector2 availableScroll;
    private Vector2 activeScroll;
    private string searchFilter = "";
    private TraitType? typeFilter = null;
    private int stackCount = 1;

    [MenuItem("Tools/Trait Assigner")]
    public static void ShowWindow()
    {
        var window = GetWindow<TraitAssignerWindow>("Trait Assigner");
        window.minSize = new Vector2(420, 500);
    }

    private void OnEnable()
    {
        traitDataList = Resources.Load<TraitDataList>("TraitDataList");
    }

    private void OnGUI()
    {
        // Header
        EditorGUILayout.LabelField("Trait Assigner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign or remove traits on the local player at runtime. Must be in Play Mode.", MessageType.Info);
        EditorGUILayout.Space(4);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Warning);
            return;
        }

        PlayerController localPlayer = PlayerController.GetLocalPlayer();
        if (localPlayer == null)
        {
            EditorGUILayout.HelpBox("No local player found.", MessageType.Warning);
            return;
        }

        CharacterTraitManager traitManager = localPlayer.GetComponent<CharacterTraitManager>();
        if (traitManager == null)
        {
            EditorGUILayout.HelpBox("Local player has no CharacterTraitManager.", MessageType.Error);
            return;
        }

        if (traitDataList == null)
        {
            EditorGUILayout.HelpBox("TraitDataList not found in Resources. Create one at Resources/TraitDataList.", MessageType.Error);
            if (GUILayout.Button("Retry Load"))
                traitDataList = Resources.Load<TraitDataList>("TraitDataList");
            return;
        }

        // Active traits section
        DrawActiveTraits(traitManager);

        EditorGUILayout.Space(8);

        // Available traits section
        DrawAvailableTraits(traitManager);
    }

    private void DrawActiveTraits(CharacterTraitManager traitManager)
    {
        EditorGUILayout.LabelField("Active Traits", EditorStyles.boldLabel);

        List<TraitData> active = traitManager.GetActiveTraits();
        if (active.Count == 0)
        {
            EditorGUILayout.LabelField("  (none)", EditorStyles.miniLabel);
            return;
        }

        activeScroll = EditorGUILayout.BeginScrollView(activeScroll);

        // Group by trait to show counts
        var grouped = active.GroupBy(t => t.traitID).ToList();
        foreach (var group in grouped)
        {
            TraitData trait = group.First();
            int count = group.Count();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Icon
            if (trait.traitIcon != null)
            {
                Texture2D tex = AssetPreview.GetAssetPreview(trait.traitIcon);
                if (tex != null)
                    GUILayout.Label(tex, GUILayout.Width(28), GUILayout.Height(28));
            }

            // Name + count
            string label = count > 1 ? $"{trait.displayName} x{count}" : trait.displayName;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            // Type badge
            EditorGUILayout.LabelField(trait.traitType.ToString(), EditorStyles.miniLabel, GUILayout.Width(80));

            // Remove button
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                traitManager.RemoveTrait(trait);
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAvailableTraits(CharacterTraitManager traitManager)
    {
        EditorGUILayout.LabelField("Available Traits", EditorStyles.boldLabel);

        // Search and filter bar
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search", searchFilter);
        if (GUILayout.Button("✕", GUILayout.Width(22)))
            searchFilter = "";
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type Filter", GUILayout.Width(70));
        if (GUILayout.Button(typeFilter.HasValue ? typeFilter.Value.ToString() : "All", EditorStyles.popup))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), !typeFilter.HasValue, () => { typeFilter = null; Repaint(); });
            foreach (TraitType t in System.Enum.GetValues(typeof(TraitType)))
            {
                TraitType captured = t;
                menu.AddItem(new GUIContent(t.ToString()), typeFilter == t, () => { typeFilter = captured; Repaint(); });
            }
            menu.ShowAsContext();
        }

        EditorGUILayout.LabelField("Stack", GUILayout.Width(35));
        stackCount = EditorGUILayout.IntField(stackCount, GUILayout.Width(30));
        stackCount = Mathf.Max(1, stackCount);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Filtered trait list
        IEnumerable<TraitData> filtered = traitDataList.AllTraits.Where(t => t != null);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            string lower = searchFilter.ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.displayName != null && t.displayName.ToLowerInvariant().Contains(lower)) ||
                (t.traitID != null && t.traitID.ToLowerInvariant().Contains(lower)) ||
                (t.description != null && t.description.ToLowerInvariant().Contains(lower)));
        }

        if (typeFilter.HasValue)
            filtered = filtered.Where(t => t.traitType == typeFilter.Value);

        var traitList = filtered.OrderBy(t => t.displayName).ToList();

        EditorGUILayout.LabelField($"  {traitList.Count} traits", EditorStyles.miniLabel);

        availableScroll = EditorGUILayout.BeginScrollView(availableScroll);

        foreach (TraitData trait in traitList)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Icon
            if (trait.traitIcon != null)
            {
                Texture2D tex = AssetPreview.GetAssetPreview(trait.traitIcon);
                if (tex != null)
                    GUILayout.Label(tex, GUILayout.Width(24), GUILayout.Height(24));
            }

            // Name
            EditorGUILayout.LabelField(trait.displayName, GUILayout.MinWidth(120));

            // Type badge
            EditorGUILayout.LabelField(trait.traitType.ToString(), EditorStyles.miniLabel, GUILayout.Width(80));

            // Add button
            if (GUILayout.Button($"Add{(stackCount > 1 ? $" x{stackCount}" : "")}", GUILayout.Width(70)))
            {
                for (int i = 0; i < stackCount; i++)
                {
                    string nodeID = $"debug_{trait.traitID}";
                    int idx = 1;
                    while (traitManager.IsNodeUnlocked(nodeID))
                    {
                        nodeID = $"debug_{trait.traitID}_{++idx}";
                    }
                    traitManager.UnlockTrait(nodeID, trait);
                }
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }
}
