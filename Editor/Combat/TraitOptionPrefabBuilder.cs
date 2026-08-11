#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor utility to generate the TraitOption prefab hierarchy and the full
/// TraitRollerCanvas with all 3 options + timer wired up.
/// 
/// Menu: GameObject > UI > Trait Roller > Create Trait Option
/// Menu: GameObject > UI > Trait Roller > Create Full Trait Roller Canvas
/// </summary>
public static class TraitOptionPrefabBuilder
{
    // ── Colours / sizes ──────────────────────────────────────────────────
    private static readonly Color panelBg        = new Color(0.12f, 0.12f, 0.15f, 0.95f);
    private static readonly Color outlineColor   = new Color(0.4f, 0.4f, 0.5f, 1f);
    private static readonly Color tierBoxColor   = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color abilityBoxColor= new Color(0.15f, 0.25f, 0.4f, 1f);
    private static readonly Color buttonHover    = new Color(0.25f, 0.7f, 0.35f, 1f);
    private static readonly Color timerBg        = new Color(0.1f, 0.1f, 0.12f, 0.85f);

    private const float optionWidth  = 320f;
    private const float optionHeight = 420f;
    private const float optionGap    = 40f;

    // =====================================================================
    //  Create a single TraitOption GameObject (no canvas)
    // =====================================================================
    [MenuItem("GameObject/UI/Trait Roller/Create Trait Option", false, 10)]
    public static void CreateSingleTraitOption()
    {
        Transform parent = Selection.activeTransform;
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;

        if (canvas == null)
        {
            Debug.LogWarning("[TraitOptionPrefabBuilder] Select a Canvas or child of a Canvas first.");
            return;
        }

        GameObject option = BuildTraitOption("TraitOption", parent);
        Selection.activeGameObject = option;
        Undo.RegisterCreatedObjectUndo(option, "Create Trait Option");
    }

    // =====================================================================
    //  Create the full TraitRollerCanvas with 3 options + timer
    // =====================================================================
    [MenuItem("GameObject/UI/Trait Roller/Create Full Trait Roller Canvas", false, 11)]
    public static void CreateFullTraitRollerCanvas()
    {
        // ── Canvas ───────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("TraitRollerCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel (full-screen dark overlay) ─────────────────────────────
        GameObject panel = CreateUIObject("TraitRollerPanel", canvasGO.transform);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        StretchFull(panelRT);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f);
        panelImg.raycastTarget = true;

        TraitRollerUI rollerUI = panel.AddComponent<TraitRollerUI>();

        // ── Timer ────────────────────────────────────────────────────────
        GameObject timerGO = CreateUIObject("TraitRollerTimer", panel.transform);
        RectTransform timerRT = timerGO.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0.5f, 1f);
        timerRT.anchorMax = new Vector2(0.5f, 1f);
        timerRT.pivot = new Vector2(0.5f, 1f);
        timerRT.anchoredPosition = new Vector2(0f, -40f);
        timerRT.sizeDelta = new Vector2(120f, 60f);

        Image timerBgImg = timerGO.AddComponent<Image>();
        timerBgImg.color = timerBg;

        GameObject timerTextGO = CreateUIObject("TimerText", timerGO.transform);
        RectTransform timerTextRT = timerTextGO.GetComponent<RectTransform>();
        StretchFull(timerTextRT);
        TMP_Text timerText = timerTextGO.AddComponent<TextMeshProUGUI>();
        timerText.text = "10";
        timerText.fontSize = 36;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;

        // ── Three Trait Options (Left / Middle / Right) ──────────────────
        float totalWidth = (optionWidth * 3) + (optionGap * 2);
        float startX = -totalWidth / 2f + optionWidth / 2f;

        string[] names = { "LeftTraitOption", "MiddleTraitOption", "RightTraitOption" };
        TraitOptionUI[] options = new TraitOptionUI[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject opt = BuildTraitOption(names[i], panel.transform);
            RectTransform optRT = opt.GetComponent<RectTransform>();
            optRT.anchorMin = new Vector2(0.5f, 0.5f);
            optRT.anchorMax = new Vector2(0.5f, 0.5f);
            optRT.pivot = new Vector2(0.5f, 0.5f);
            optRT.anchoredPosition = new Vector2(startX + i * (optionWidth + optionGap), 0f);
            optRT.sizeDelta = new Vector2(optionWidth, optionHeight);

            options[i] = opt.GetComponent<TraitOptionUI>();
        }

        // ── Wire TraitRollerUI serialized fields ─────────────────────────
        SerializedObject so = new SerializedObject(rollerUI);
        so.Update();

        SetProp(so, "rollerPanel",       panel);
        SetProp(so, "traitRollerTimer",  timerText);

        SerializedProperty optionsProp = so.FindProperty("traitOptions");
        if (optionsProp != null)
        {
            optionsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                optionsProp.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
        }

        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasGO;
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Trait Roller Canvas");

        Debug.Log("[TraitOptionPrefabBuilder] Full TraitRollerCanvas created with 3 options + timer. " +
                  "Drag to Project to save as prefab.");
    }

    // =====================================================================
    //  Build a single TraitOption hierarchy and wire its SerializedObject
    // =====================================================================
    private static GameObject BuildTraitOption(string name, Transform parent)
    {
        // ── Root (entire option is clickable) ────────────────────────────
        GameObject root = CreateUIObject(name, parent);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(optionWidth, optionHeight);

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = panelBg;

        TraitOptionUI optionUI = root.AddComponent<TraitOptionUI>();

        // Button on root — whole card is clickable
        Button button = root.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.highlightedColor = buttonHover;
        button.colors = cb;
        button.targetGraphic = rootBg;

        // ── Outline (border image behind content) ────────────────────────
        GameObject outlineGO = CreateUIObject("Outline", root.transform);
        RectTransform outlineRT = outlineGO.GetComponent<RectTransform>();
        StretchFull(outlineRT);
        outlineRT.offsetMin = new Vector2(-3f, -3f);
        outlineRT.offsetMax = new Vector2(3f, 3f);
        Image outlineImg = outlineGO.AddComponent<Image>();
        outlineImg.color = outlineColor;
        outlineGO.transform.SetAsFirstSibling();

        // ── Trait Type Label (top) ───────────────────────────────────────
        GameObject typeLabelGO = CreateUIObject("TraitTypeLabel", root.transform);
        RectTransform typeLabelRT = typeLabelGO.GetComponent<RectTransform>();
        typeLabelRT.anchorMin = new Vector2(0f, 1f);
        typeLabelRT.anchorMax = new Vector2(1f, 1f);
        typeLabelRT.pivot = new Vector2(0.5f, 1f);
        typeLabelRT.anchoredPosition = new Vector2(0f, -10f);
        typeLabelRT.sizeDelta = new Vector2(0f, 30f);
        TMP_Text typeLabel = typeLabelGO.AddComponent<TextMeshProUGUI>();
        typeLabel.text = "Stat";
        typeLabel.fontSize = 16;
        typeLabel.alignment = TextAlignmentOptions.Center;
        typeLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        typeLabel.raycastTarget = false;

        // ── Trait Name ──────────────────────────────────────────────────
        GameObject nameGO = CreateUIObject("TraitName", root.transform);
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -45f);
        nameRT.sizeDelta = new Vector2(-20f, 40f);
        TMP_Text nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = "Trait Name";
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.raycastTarget = false;

        // ── Trait Description ────────────────────────────────────────────
        GameObject descGO = CreateUIObject("TraitDescription", root.transform);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 1f);
        descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot = new Vector2(0.5f, 1f);
        descRT.anchoredPosition = new Vector2(0f, -95f);
        descRT.sizeDelta = new Vector2(-20f, 120f);
        TMP_Text descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = "Description of what this trait does.";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        descText.enableWordWrapping = true;
        descText.raycastTarget = false;

        // ── Tier Box ─────────────────────────────────────────────────────
        GameObject tierBoxGO = CreateUIObject("TierBox", root.transform);
        RectTransform tierBoxRT = tierBoxGO.GetComponent<RectTransform>();
        tierBoxRT.anchorMin = new Vector2(0.5f, 0f);
        tierBoxRT.anchorMax = new Vector2(0.5f, 0f);
        tierBoxRT.pivot = new Vector2(0.5f, 0f);
        tierBoxRT.anchoredPosition = new Vector2(0f, 80f);
        tierBoxRT.sizeDelta = new Vector2(140f, 40f);

        GameObject tierOutlineGO = CreateUIObject("TierBoxOutline", tierBoxGO.transform);
        RectTransform tierOutlineRT = tierOutlineGO.GetComponent<RectTransform>();
        StretchFull(tierOutlineRT);
        Image tierOutlineImg = tierOutlineGO.AddComponent<Image>();
        tierOutlineImg.color = tierBoxColor;
        tierOutlineImg.raycastTarget = false;

        GameObject tierTextGO = CreateUIObject("TierBoxText", tierBoxGO.transform);
        RectTransform tierTextRT = tierTextGO.GetComponent<RectTransform>();
        StretchFull(tierTextRT);
        TMP_Text tierText = tierTextGO.AddComponent<TextMeshProUGUI>();
        tierText.text = "Tier I";
        tierText.fontSize = 18;
        tierText.alignment = TextAlignmentOptions.Center;
        tierText.color = Color.white;
        tierText.raycastTarget = false;

        // ── Ability Box (hidden by default) ──────────────────────────────
        GameObject abilityBoxGO = CreateUIObject("AbilityBox", root.transform);
        RectTransform abilityBoxRT = abilityBoxGO.GetComponent<RectTransform>();
        abilityBoxRT.anchorMin = new Vector2(0.5f, 0f);
        abilityBoxRT.anchorMax = new Vector2(0.5f, 0f);
        abilityBoxRT.pivot = new Vector2(0.5f, 0f);
        abilityBoxRT.anchoredPosition = new Vector2(0f, 80f);
        abilityBoxRT.sizeDelta = new Vector2(70f, 70f);
        abilityBoxGO.SetActive(false);

        // AbilityBoxOutline
        GameObject abOutlineGO = CreateUIObject("AbilityBoxOutline", abilityBoxGO.transform);
        RectTransform abOutlineRT = abOutlineGO.GetComponent<RectTransform>();
        StretchFull(abOutlineRT);
        Image abOutlineImg = abOutlineGO.AddComponent<Image>();
        abOutlineImg.color = abilityBoxColor;
        abOutlineImg.raycastTarget = false;

        // AbilityBoxInside
        GameObject abInsideGO = CreateUIObject("AbilityBoxInside", abilityBoxGO.transform);
        RectTransform abInsideRT = abInsideGO.GetComponent<RectTransform>();
        StretchFull(abInsideRT);
        abInsideRT.offsetMin = new Vector2(3f, 3f);
        abInsideRT.offsetMax = new Vector2(-3f, -3f);
        Image abInsideImg = abInsideGO.AddComponent<Image>();
        abInsideImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        abInsideImg.raycastTarget = false;

        // AbilityBoxIcon
        GameObject abIconGO = CreateUIObject("AbilityBoxIcon", abilityBoxGO.transform);
        RectTransform abIconRT = abIconGO.GetComponent<RectTransform>();
        StretchFull(abIconRT);
        abIconRT.offsetMin = new Vector2(8f, 8f);
        abIconRT.offsetMax = new Vector2(-8f, -8f);
        Image abIconImg = abIconGO.AddComponent<Image>();
        abIconImg.color = Color.white;
        abIconImg.preserveAspect = true;
        abIconImg.raycastTarget = false;

        // ── Wire TraitOptionUI serialized fields ─────────────────────────
        SerializedObject so = new SerializedObject(optionUI);
        so.Update();

        SetProp(so, "traitNameText",        nameText);
        SetProp(so, "traitDescriptionText", descText);
        SetProp(so, "traitTypeLabel",       typeLabel);
        SetProp(so, "outlineImage",         outlineImg);
        SetProp(so, "tierBox",              tierBoxGO);
        SetProp(so, "tierBoxOutline",       tierOutlineImg);
        SetProp(so, "tierBoxText",          tierText);
        SetProp(so, "abilityBox",           abilityBoxGO);
        SetProp(so, "abilityBoxOutline",    abOutlineImg);
        SetProp(so, "abilityBoxInside",     abInsideImg);
        SetProp(so, "abilityBoxIcon",       abIconImg);

        so.ApplyModifiedProperties();

        return root;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetProp(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }
}
#endif
