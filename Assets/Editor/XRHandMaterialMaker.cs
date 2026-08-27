using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class XRHandMaterialMaker : EditorWindow
{
    const string DefaultFolder = "Assets/Shaders";
    const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    const string GhostHandShaderName = "Custom/GhostHand";
    const string HandGlowShaderName = "Custom/HandGlow";

    enum MaterialKind
    {
        Skin,
        GhostHologram,
        Glow
    }

    MaterialKind materialKind = MaterialKind.Skin;
    string materialName = "HandSkin_Custom";
    string folderPath = DefaultFolder;

    Color baseColor = new Color(1f, 0.725f, 0.58f, 1f);
    Color rimColor = new Color(0.68f, 1f, 0.92f, 0.9f);
    float opacity = 0.48f;
    float smoothness = 0.42f;
    float rimPower = 1.65f;
    float rimIntensity = 1.85f;
    float pulseSpeed = 1.8f;
    float pulseAmount = 0.22f;
    float flickerSpeed = 9f;
    float flickerAmount = 0.11f;
    bool applyToSelection;

    [MenuItem("Tools/XR Hands/Hand Material Maker")]
    public static void Open()
    {
        var window = GetWindow<XRHandMaterialMaker>("Hand Materials");
        window.minSize = new Vector2(360f, 430f);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("XR Hands Material Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUI.BeginChangeCheck();
        materialKind = (MaterialKind)EditorGUILayout.EnumPopup("Material Type", materialKind);
        if (EditorGUI.EndChangeCheck())
            ApplyPreset(materialKind);

        materialName = EditorGUILayout.TextField("Material Name", materialName);

        using (new EditorGUILayout.HorizontalScope())
        {
            folderPath = EditorGUILayout.TextField("Folder", folderPath);
            if (GUILayout.Button("Pick", GUILayout.Width(56f)))
                PickFolder();
        }

        EditorGUILayout.Space(8f);
        DrawPresetButtons();
        EditorGUILayout.Space(8f);
        DrawMaterialSettings();

        EditorGUILayout.Space(8f);
        applyToSelection = EditorGUILayout.ToggleLeft("Apply to selected Renderer or GameObject after creation", applyToSelection);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Material", GUILayout.Height(32f)))
                CreateMaterial();
        }
    }

    void DrawPresetButtons()
    {
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Light Skin"))
            {
                materialKind = MaterialKind.Skin;
                ApplyPreset(materialKind);
            }

            if (GUILayout.Button("Hologram"))
            {
                materialKind = MaterialKind.GhostHologram;
                ApplyPreset(materialKind);
            }

            if (GUILayout.Button("Warm Glow"))
            {
                materialKind = MaterialKind.Glow;
                ApplyPreset(materialKind);
            }
        }
    }

    void DrawMaterialSettings()
    {
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        baseColor = EditorGUILayout.ColorField("Base Color", baseColor);

        if (materialKind != MaterialKind.Skin)
            rimColor = EditorGUILayout.ColorField(materialKind == MaterialKind.GhostHologram ? "Rim Color" : "Fresnel Color", rimColor);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);

        if (materialKind == MaterialKind.Skin)
        {
            smoothness = EditorGUILayout.Slider("Smoothness", smoothness, 0f, 1f);
        }
        else
        {
            opacity = EditorGUILayout.Slider("Opacity", opacity, 0f, 1f);
            rimPower = EditorGUILayout.Slider("Rim Power", rimPower, 0.5f, 8f);
            rimIntensity = EditorGUILayout.Slider("Rim Intensity", rimIntensity, 0f, 3f);
        }

        if (materialKind == MaterialKind.GhostHologram)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Hologram Motion", EditorStyles.boldLabel);
            pulseSpeed = EditorGUILayout.FloatField("Pulse Speed", pulseSpeed);
            pulseAmount = EditorGUILayout.Slider("Pulse Amount", pulseAmount, 0f, 0.5f);
            flickerSpeed = EditorGUILayout.FloatField("Flicker Speed", flickerSpeed);
            flickerAmount = EditorGUILayout.Slider("Flicker Amount", flickerAmount, 0f, 0.35f);
        }
    }

    void ApplyPreset(MaterialKind kind)
    {
        switch (kind)
        {
            case MaterialKind.Skin:
                materialName = "HandSkin_Custom";
                baseColor = new Color(1f, 0.725f, 0.58f, 1f);
                smoothness = 0.42f;
                break;

            case MaterialKind.GhostHologram:
                materialName = "GhostHand_Hologram_Custom";
                baseColor = new Color(0.18f, 0.78f, 1f, 0.26f);
                rimColor = new Color(0.68f, 1f, 0.92f, 0.9f);
                opacity = 0.48f;
                rimPower = 1.65f;
                rimIntensity = 1.85f;
                pulseSpeed = 1.8f;
                pulseAmount = 0.22f;
                flickerSpeed = 9f;
                flickerAmount = 0.11f;
                break;

            case MaterialKind.Glow:
                materialName = "HandGlow_Custom";
                baseColor = new Color(0.9f, 0.7f, 0.52f, 1f);
                rimColor = new Color(1f, 0.92f, 0.84f, 1f);
                opacity = 0.8f;
                rimPower = 2.5f;
                rimIntensity = 1.2f;
                break;
        }
    }

    void PickFolder()
    {
        var selected = EditorUtility.OpenFolderPanel("Material Folder", "Assets", string.Empty);
        if (string.IsNullOrEmpty(selected))
            return;

        var projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
        selected = selected.Replace('\\', '/');

        if (!selected.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Choose a folder inside this Unity project.", "OK");
            return;
        }

        folderPath = selected.Substring(projectPath.Length + 1);
    }

    bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(materialName)
            && !string.IsNullOrWhiteSpace(folderPath)
            && folderPath.StartsWith("Assets");
    }

    void CreateMaterial()
    {
        EnsureFolder(folderPath);
        var selectionBeforeCreate = Selection.objects;

        var shader = GetShaderForKind(materialKind);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Shader Missing", $"Could not find the shader for {materialKind}.", "OK");
            return;
        }

        var material = new Material(shader)
        {
            name = materialName
        };

        ConfigureMaterial(material);

        var path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{SanitizeFileName(materialName)}.mat");
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (applyToSelection)
            ApplyMaterialToSelection(selectionBeforeCreate, material);

        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
        Debug.Log($"Created hand material: {path}");
    }

    Shader GetShaderForKind(MaterialKind kind)
    {
        switch (kind)
        {
            case MaterialKind.Skin:
                return Shader.Find(UrpLitShaderName) ?? FindShaderAsset("Lit");
            case MaterialKind.GhostHologram:
                return Shader.Find(GhostHandShaderName) ?? FindShaderAsset("GhostHand");
            case MaterialKind.Glow:
                return Shader.Find(HandGlowShaderName) ?? FindShaderAsset("HandGlow");
            default:
                return null;
        }
    }

    static Shader FindShaderAsset(string shaderAssetName)
    {
        var guids = AssetDatabase.FindAssets($"{shaderAssetName} t:Shader", new[] { "Assets", "Packages" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null && shader.name.EndsWith(shaderAssetName))
                return shader;
        }

        return null;
    }

    void ConfigureMaterial(Material material)
    {
        switch (materialKind)
        {
            case MaterialKind.Skin:
                SetColor(material, "_BaseColor", baseColor);
                SetColor(material, "_Color", baseColor);
                SetColor(material, "_SpecColor", new Color(0.22f, 0.16f, 0.13f, 1f));
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", smoothness);
                SetFloat(material, "_Glossiness", smoothness);
                SetFloat(material, "_GlossMapScale", smoothness);
                SetFloat(material, "_Surface", 0f);
                SetFloat(material, "_ZWrite", 1f);
                material.renderQueue = -1;
                material.SetOverrideTag("RenderType", "Opaque");
                break;

            case MaterialKind.GhostHologram:
                SetColor(material, "_BaseColor", baseColor);
                SetColor(material, "_RimColor", rimColor);
                SetFloat(material, "_Opacity", opacity);
                SetFloat(material, "_RimPower", rimPower);
                SetFloat(material, "_RimIntensity", rimIntensity);
                SetFloat(material, "_PulseSpeed", pulseSpeed);
                SetFloat(material, "_PulseAmount", pulseAmount);
                SetFloat(material, "_FlickerSpeed", flickerSpeed);
                SetFloat(material, "_FlickerAmount", flickerAmount);
                material.SetOverrideTag("RenderType", "Transparent");
                break;

            case MaterialKind.Glow:
                SetColor(material, "_BaseColor", baseColor);
                SetColor(material, "_FresnelColor", rimColor);
                SetFloat(material, "_Opacity", opacity);
                SetFloat(material, "_FresnelPower", rimPower);
                SetFloat(material, "_FresnelIntensity", rimIntensity);
                material.SetOverrideTag("RenderType", "Transparent");
                break;
        }
    }

    static void ApplyMaterialToSelection(Object[] selectedObjects, Material material)
    {
        var applied = false;

        foreach (var selected in selectedObjects)
        {
            if (selected is Renderer renderer)
            {
                Undo.RecordObject(renderer, "Apply Hand Material");
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                applied = true;
                continue;
            }

            if (selected is GameObject gameObject)
            {
                foreach (var childRenderer in gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    Undo.RecordObject(childRenderer, "Apply Hand Material");
                    childRenderer.sharedMaterial = material;
                    EditorUtility.SetDirty(childRenderer);
                    applied = true;
                }
            }
        }

        if (!applied)
            Debug.LogWarning("No Renderer found in the current selection.");
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

    static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }
}
