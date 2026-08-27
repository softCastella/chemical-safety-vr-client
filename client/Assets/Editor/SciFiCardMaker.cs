using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SciFiCardMaker : EditorWindow
{
    const string DefaultMaterialFolder = "Assets/Generated/SciFiCards/Materials";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";
    const string LitShaderName = "Universal Render Pipeline/Lit";

    enum CardPreset
    {
        GlassBasic,
        NeonEdge,
        FloatingShadow,
        HologramPanel,
        DenseControlButton,
        WarningAlertCard,
        CornerMarkerCard,
        LayeredDepthCard
    }

    string rootName = "SciFiCards_Preview";
    string materialFolder = DefaultMaterialFolder;
    CardPreset selectedPreset = CardPreset.GlassBasic;
    Vector2 cardSize = new Vector2(0.72f, 0.28f);
    float cornerRadius = 0.045f;
    float spacing = 0.9f;
    bool placeAtSelection = true;
    bool selectCreatedRoot = true;

    [MenuItem("Tools/XR UI/Sci-Fi Card Maker")]
    public static void Open()
    {
        var window = GetWindow<SciFiCardMaker>("Sci-Fi Cards");
        window.minSize = new Vector2(390f, 360f);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("VR Sci-Fi Card Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        rootName = EditorGUILayout.TextField("Root Name", rootName);

        using (new EditorGUILayout.HorizontalScope())
        {
            materialFolder = EditorGUILayout.TextField("Material Folder", materialFolder);
            if (GUILayout.Button("Pick", GUILayout.Width(56f)))
                PickFolder();
        }

        EditorGUILayout.Space(8f);
        selectedPreset = (CardPreset)EditorGUILayout.EnumPopup("Preset", selectedPreset);
        cardSize = EditorGUILayout.Vector2Field("Card Size", cardSize);
        cornerRadius = EditorGUILayout.Slider("Corner Radius", cornerRadius, 0f, GetMaxCornerRadius());
        spacing = EditorGUILayout.Slider("Preview Spacing", spacing, 0.45f, 1.8f);

        EditorGUILayout.Space(8f);
        placeAtSelection = EditorGUILayout.ToggleLeft("Place at selected GameObject", placeAtSelection);
        selectCreatedRoot = EditorGUILayout.ToggleLeft("Select created root", selectCreatedRoot);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Selected Preset", GUILayout.Height(30f)))
                CreateSelectedPreset();

            if (GUILayout.Button("Create All Example Cards", GUILayout.Height(34f)))
                CreateAllPresets();
        }
    }

    void PickFolder()
    {
        var selected = EditorUtility.OpenFolderPanel("Card Material Folder", "Assets", string.Empty);
        if (string.IsNullOrEmpty(selected))
            return;

        var projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
        selected = selected.Replace('\\', '/');

        if (!selected.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Choose a folder inside this Unity project.", "OK");
            return;
        }

        materialFolder = selected.Substring(projectPath.Length + 1);
    }

    bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(rootName)
            && !string.IsNullOrWhiteSpace(materialFolder)
            && materialFolder.StartsWith("Assets")
            && cardSize.x > 0f
            && cardSize.y > 0f;
    }

    float GetMaxCornerRadius()
    {
        return Mathf.Max(0f, Mathf.Min(cardSize.x, cardSize.y) * 0.5f);
    }

    void CreateSelectedPreset()
    {
        EnsureFolder(materialFolder);
        var root = CreateRoot(SanitizeFileName(rootName + "_" + selectedPreset));
        CreateCard(selectedPreset, root.transform, Vector3.zero, 0);
        FinishCreate(root);
    }

    void CreateAllPresets()
    {
        EnsureFolder(materialFolder);
        var root = CreateRoot(SanitizeFileName(rootName));
        var presets = (CardPreset[])System.Enum.GetValues(typeof(CardPreset));

        for (var i = 0; i < presets.Length; i++)
        {
            var column = i % 4;
            var row = i / 4;
            var position = new Vector3((column - 1.5f) * spacing, -row * spacing * 0.46f, 0f);
            CreateCard(presets[i], root.transform, position, i);
        }

        FinishCreate(root);
    }

    GameObject CreateRoot(string name)
    {
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create Sci-Fi Cards");

        if (placeAtSelection && Selection.activeTransform != null)
            root.transform.position = Selection.activeTransform.position;

        return root;
    }

    void FinishCreate(GameObject root)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectCreatedRoot)
            Selection.activeGameObject = root;

        EditorGUIUtility.PingObject(root);
        Debug.Log($"Created VR sci-fi card UI examples under {root.name}");
    }

    void CreateCard(CardPreset preset, Transform parent, Vector3 localPosition, int index)
    {
        var style = GetStyle(preset);
        var root = new GameObject("SciFiCard_" + style.name);
        Undo.RegisterCreatedObjectUndo(root, "Create Sci-Fi Card");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;

        var visual = root.AddComponent<SciFiCardVisual>();
        var collider = root.AddComponent<BoxCollider>();

        visual.size = cardSize * style.sizeMultiplier;
        visual.cornerRadius = Mathf.Min(cornerRadius * style.radiusMultiplier, Mathf.Min(visual.size.x, visual.size.y) * 0.5f);
        visual.cornerSegments = style.cornerSegments;
        visual.glassColor = style.glassColor;
        visual.glassOpacity = style.glassOpacity;
        visual.backplateColor = style.backplateColor;
        visual.backplateOpacity = style.backplateOpacity;
        visual.backplateExpansion = style.backplateExpansion;
        visual.edgeColor = style.edgeColor;
        visual.edgeOpacity = style.edgeOpacity;
        visual.edgeExpansion = style.edgeExpansion;
        visual.shadowColor = style.shadowColor;
        visual.shadowOpacity = style.shadowOpacity;
        visual.shadowOffset = style.shadowOffset;
        visual.shadowScale = style.shadowScale;
        visual.cornerColor = style.cornerColor;
        visual.cornerOpacity = style.cornerOpacity;
        visual.cornerLength = style.cornerLength;
        visual.cornerThickness = style.cornerThickness;
        visual.cornerInset = style.cornerInset;
        visual.showBackplate = style.showBackplate;
        visual.showEdgeGlow = style.showEdgeGlow;
        visual.showShadow = style.showShadow;
        visual.showCornerMarkers = style.showCornerMarkers;
        visual.glassDepth = 0f;
        visual.edgeDepth = style.edgeDepth;
        visual.cornerDepth = style.cornerDepth;
        visual.backplateDepth = style.backplateDepth;
        visual.shadowDepth = style.shadowDepth;
        visual.frameColor = Color.Lerp(style.backplateColor, style.edgeColor, 0.32f);
        visual.frameColor = new Color(visual.frameColor.r, visual.frameColor.g, visual.frameColor.b, 1f);
        visual.frameWidth = Mathf.Clamp(Mathf.Min(visual.size.x, visual.size.y) * 0.055f, 0.012f, 0.026f);
        visual.frameThickness = Mathf.Clamp(Mathf.Min(visual.size.x, visual.size.y) * 0.065f, 0.014f, 0.032f);
        visual.interactionCollider = collider;

        var materialPrefix = $"{index:00}_{style.name}";
        CreateLayer(root.transform, "ShadowPlate", style.shadowColor, style.shadowOpacity, materialPrefix, "Shadow",
            out visual.shadowFilter, out visual.shadowRenderer);
        CreateLayer(root.transform, "Backplate", style.backplateColor, style.backplateOpacity, materialPrefix, "Backplate",
            out visual.backplateFilter, out visual.backplateRenderer);
        CreateLayer(root.transform, "GlassPanel", style.glassColor, style.glassOpacity, materialPrefix, "Glass",
            out visual.glassFilter, out visual.glassRenderer);
        CreateLayer(root.transform, "EdgeGlow", style.edgeColor, style.edgeOpacity, materialPrefix, "EdgeGlow",
            out visual.edgeFilter, out visual.edgeRenderer);
        CreateLayer(root.transform, "SolidFrame", visual.frameColor, 1f, materialPrefix, "Frame",
            out visual.frameFilter, out visual.frameRenderer, true);

        var markerRoot = new GameObject("CornerMarkers");
        Undo.RegisterCreatedObjectUndo(markerRoot, "Create Corner Markers");
        markerRoot.transform.SetParent(root.transform, false);
        visual.cornerMarkerFilters = new MeshFilter[8];
        visual.cornerMarkerRenderers = new MeshRenderer[8];

        for (var i = 0; i < 8; i++)
        {
            CreateLayer(markerRoot.transform, "CornerMarker_" + (i + 1), style.cornerColor, style.cornerOpacity,
                materialPrefix, "CornerMarker_" + (i + 1), out visual.cornerMarkerFilters[i], out visual.cornerMarkerRenderers[i]);
        }

        var depthResponse = root.AddComponent<SciFiCardDepthResponse>();
        depthResponse.backgroundAnchor = CreateAnchor(root.transform, "BackgroundContent", 0.012f);
        depthResponse.contentAnchor = CreateAnchor(root.transform, "MainContent", -0.024f);
        depthResponse.foregroundAnchor = CreateAnchor(root.transform, "ForegroundContent", -0.042f);
        depthResponse.CacheBasePositions();

        visual.Apply();
        EditorUtility.SetDirty(visual);
    }

    void CreateLayer(Transform parent, string name, Color color, float opacity, string prefix, string materialSuffix,
        out MeshFilter meshFilter, out MeshRenderer meshRenderer, bool lit = false)
    {
        var gameObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Card Layer");
        gameObject.transform.SetParent(parent, false);

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial($"{prefix}_{materialSuffix}", color, opacity, lit);
    }

    static Transform CreateAnchor(Transform parent, string name, float depth)
    {
        var anchor = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(anchor, "Create Card Content Anchor");
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = new Vector3(0f, 0f, depth);
        return anchor.transform;
    }

    Material CreateMaterial(string name, Color color, float opacity, bool lit = false)
    {
        var shader = Shader.Find(lit ? LitShaderName : UnlitShaderName)
            ?? Shader.Find(LitShaderName)
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");

        if (shader == null)
            throw new System.InvalidOperationException("No compatible shader was found for sci-fi card materials.");

        var material = new Material(shader)
        {
            name = name
        };

        if (lit)
            ConfigureFrameMaterial(material, color);
        else
            ConfigureTransparentMaterial(material, color, opacity);

        var path = AssetDatabase.GenerateUniqueAssetPath($"{materialFolder}/{SanitizeFileName(name)}.mat");
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void ConfigureFrameMaterial(Material material, Color color)
    {
        color.a = 1f;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        SetFloat(material, "_Surface", 0f);
        SetFloat(material, "_Metallic", 0.72f);
        SetFloat(material, "_Smoothness", 0.82f);
        SetFloat(material, "_ZWrite", 1f);
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    static void ConfigureTransparentMaterial(Material material, Color color, float opacity)
    {
        color.a = opacity;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color);

        SetFloat(material, "_Surface", 1f);
        SetFloat(material, "_Blend", 0f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloat(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_AlphaClip", 0f);

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    static CardStyle GetStyle(CardPreset preset)
    {
        switch (preset)
        {
            case CardPreset.NeonEdge:
                return new CardStyle("NeonEdge")
                {
                    glassColor = new Color(0.08f, 0.44f, 0.72f, 0.28f),
                    edgeColor = new Color(0.16f, 1f, 0.95f, 0.95f),
                    cornerColor = new Color(0.6f, 1f, 0.94f, 1f),
                    edgeOpacity = 0.95f,
                    edgeExpansion = 0.045f,
                    shadowOpacity = 0.28f,
                    showCornerMarkers = false
                };

            case CardPreset.FloatingShadow:
                return new CardStyle("FloatingShadow")
                {
                    glassColor = new Color(0.18f, 0.74f, 0.96f, 0.28f),
                    backplateOpacity = 0.48f,
                    shadowOpacity = 0.58f,
                    shadowScale = 1.24f,
                    shadowOffset = new Vector2(0.048f, -0.05f),
                    shadowDepth = 0.07f,
                    edgeOpacity = 0.46f,
                    showCornerMarkers = false
                };

            case CardPreset.HologramPanel:
                return new CardStyle("HologramPanel")
                {
                    glassColor = new Color(0.12f, 0.9f, 1f, 0.22f),
                    backplateColor = new Color(0.02f, 0.05f, 0.075f, 0.32f),
                    backplateOpacity = 0.32f,
                    edgeColor = new Color(0.6f, 1f, 0.92f, 0.82f),
                    edgeOpacity = 0.82f,
                    edgeExpansion = 0.032f,
                    cornerColor = new Color(0.78f, 1f, 0.95f, 0.82f),
                    cornerOpacity = 0.82f,
                    cornerLength = 0.095f,
                    shadowOpacity = 0.2f
                };

            case CardPreset.DenseControlButton:
                return new CardStyle("DenseControlButton")
                {
                    sizeMultiplier = new Vector2(0.82f, 0.72f),
                    radiusMultiplier = 0.62f,
                    glassColor = new Color(0.13f, 0.58f, 0.76f, 0.4f),
                    backplateOpacity = 0.62f,
                    edgeOpacity = 0.58f,
                    edgeExpansion = 0.016f,
                    cornerLength = 0.052f,
                    cornerThickness = 0.0045f,
                    shadowOpacity = 0.26f
                };

            case CardPreset.WarningAlertCard:
                return new CardStyle("WarningAlertCard")
                {
                    glassColor = new Color(1f, 0.38f, 0.12f, 0.32f),
                    backplateColor = new Color(0.12f, 0.035f, 0.02f, 0.56f),
                    edgeColor = new Color(1f, 0.56f, 0.16f, 0.92f),
                    cornerColor = new Color(1f, 0.74f, 0.28f, 1f),
                    shadowColor = new Color(0.16f, 0.02f, 0f, 0.42f),
                    edgeOpacity = 0.92f,
                    cornerOpacity = 1f,
                    shadowOpacity = 0.42f
                };

            case CardPreset.CornerMarkerCard:
                return new CardStyle("CornerMarkerCard")
                {
                    glassColor = new Color(0.14f, 0.62f, 0.84f, 0.26f),
                    edgeOpacity = 0.18f,
                    cornerOpacity = 1f,
                    cornerLength = 0.12f,
                    cornerThickness = 0.0075f,
                    cornerInset = 0.016f,
                    shadowOpacity = 0.28f
                };

            case CardPreset.LayeredDepthCard:
                return new CardStyle("LayeredDepthCard")
                {
                    glassColor = new Color(0.2f, 0.82f, 1f, 0.36f),
                    backplateColor = new Color(0.015f, 0.04f, 0.065f, 0.68f),
                    edgeColor = new Color(0.46f, 1f, 0.86f, 0.7f),
                    backplateExpansion = 0.034f,
                    edgeExpansion = 0.022f,
                    backplateDepth = 0.032f,
                    shadowDepth = 0.085f,
                    edgeDepth = -0.014f,
                    cornerDepth = -0.024f,
                    shadowScale = 1.18f,
                    shadowOpacity = 0.44f
                };

            default:
                return new CardStyle("GlassBasic");
        }
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Trim();
    }

    sealed class CardStyle
    {
        public readonly string name;
        public Vector2 sizeMultiplier = Vector2.one;
        public float radiusMultiplier = 1f;
        public int cornerSegments = 10;
        public Color glassColor = new Color(0.17f, 0.76f, 1f, 0.34f);
        public float glassOpacity = 0.34f;
        public Color backplateColor = new Color(0.025f, 0.055f, 0.08f, 0.58f);
        public float backplateOpacity = 0.58f;
        public float backplateExpansion = 0.018f;
        public Color edgeColor = new Color(0.36f, 1f, 0.92f, 0.72f);
        public float edgeOpacity = 0.72f;
        public float edgeExpansion = 0.026f;
        public Color shadowColor = new Color(0f, 0.02f, 0.04f, 0.38f);
        public float shadowOpacity = 0.38f;
        public Vector2 shadowOffset = new Vector2(0.025f, -0.026f);
        public float shadowScale = 1.12f;
        public Color cornerColor = new Color(0.62f, 1f, 0.92f, 0.9f);
        public float cornerOpacity = 0.9f;
        public float cornerLength = 0.075f;
        public float cornerThickness = 0.006f;
        public float cornerInset = 0.012f;
        public bool showBackplate = true;
        public bool showEdgeGlow = true;
        public bool showShadow = true;
        public bool showCornerMarkers = true;
        public float edgeDepth = -0.008f;
        public float cornerDepth = -0.014f;
        public float backplateDepth = 0.018f;
        public float shadowDepth = 0.045f;

        public CardStyle(string name)
        {
            this.name = name;
        }
    }
}
