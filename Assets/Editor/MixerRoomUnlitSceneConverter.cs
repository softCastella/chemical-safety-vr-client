using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MixerRoomUnlitSceneConverter
{
    const string TargetScenePath = "Assets/Scenes/5_MixerRoom.unity";
    const string MaterialFolder = "Assets/Materials/MixerRoom/Unlit";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";
    const string DrumMaterialPath =
        "Assets/TripoModels/blue_plastic_drum_3d_model/Materials/blue_plastic_drum_3d_model.mat";
    const string BucketMaterialPath =
        "Assets/Materials/MixerRoom/red_bucket_3d_model_SoftLit.mat";
    const string ConeMaterialPath =
        "Assets/Materials/MixerRoom/Unlit/traffic_cone_3d_model_50c7933b_2100000_Unlit.mat";

    static readonly HashSet<string> ExcludedRootNames = new(StringComparer.Ordinal)
    {
        "Directional Light",
        "GeneratedPlane",
        "Hangar_v2_6 Variant",
        "Hangar_v2_6_gate1 (1)",
        "Lights",
        "MixerRoom_Backdrop_Fog_Stage",
        "MixerRoom_Uniform_Ambient_Fill",
        "blue_plastic_drum_3d_model",
        "blue_plastic_drum_3d_model (1)",
        "red_bucket_3d_model",
        "sPipe_02",
        "sPipe_02 (1)",
        "XR Origin (XR Rig)",
    };

    static readonly HashSet<string> ExcludedObjectNames = new(StringComparer.Ordinal)
    {
        "Backdrop_Thin_Box",
        "Background_Image_Surface",
        "Fog_Back_Wide",
        "Fog_Front_SoftVeil",
        "Fog_Mid_LowBand",
        "msds",
        "work_confirm",
        "work_permission",
        "work_plan",
    };

    [MenuItem("Tools/Mixer Room/Apply Requested Unlit Prop Overrides")]
    public static void ApplyRequestedOverridesFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying the prop overrides.");
            return;
        }

        ApplyRequestedOverrides(scene);
    }

    [MenuItem("Tools/Mixer Room/Convert 5_MixerRoom Props to URP Unlit")]
    public static void ConvertFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Convert();
        Validate();
    }

    public static void ConvertBatch()
    {
        Convert();
        Validate();
    }

    [MenuItem("Tools/Mixer Room/Validate 5_MixerRoom Material Scope")]
    public static void ValidateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Validate(scene);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Validate(scene);
    }

    static void ApplyRequestedOverrides(Scene scene)
    {
        Material drumMaterial = LoadRequiredMaterial(DrumMaterialPath);
        Material bucketMaterial = LoadRequiredMaterial(BucketMaterialPath);
        Material coneMaterial = LoadRequiredMaterial(ConeMaterialPath);

        SetRootRendererMaterial(scene, "blue_plastic_drum_3d_model", drumMaterial);
        SetRootRendererMaterial(scene, "blue_plastic_drum_3d_model (1)", drumMaterial);
        SetRootRendererMaterial(scene, "red_bucket_3d_model", bucketMaterial);
        SetRootRendererMaterial(scene, "traffic_cone_3d_model", coneMaterial);
        SetRootRendererMaterial(scene, "traffic_cone_3d_model (1)", coneMaterial);

        Undo.RecordObject(coneMaterial, "Brighten Mixer Room Traffic Cones");
        if (coneMaterial.HasProperty("_BaseColor"))
            coneMaterial.SetColor("_BaseColor", new Color(1.2f, 1.2f, 1.2f, 1f));
        if (coneMaterial.HasProperty("_Color"))
            coneMaterial.SetColor("_Color", new Color(1.2f, 1.2f, 1.2f, 1f));
        EditorUtility.SetDirty(coneMaterial);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log(
            "Applied Mixer Room Unlit prop overrides without changing authored transforms. " +
            "The existing Build Settings state was preserved.");
    }

    static Material LoadRequiredMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new FileNotFoundException($"Required material was not found: {path}");
        return material;
    }

    static void SetRootRendererMaterial(Scene scene, string rootName, Material material)
    {
        GameObject root = Array.Find(
            scene.GetRootGameObjects(),
            candidate => candidate.name == rootName);
        if (root == null)
            throw new MissingReferenceException(
                $"Root object '{rootName}' was not found in '{TargetScenePath}'.");

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new MissingReferenceException($"'{rootName}' has no renderer.");

        Undo.RecordObjects(renderers, $"Set {rootName} Material");
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.sharedMaterials;
            for (int index = 0; index < slots.Length; index++)
                slots[index] = material;
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
        }
    }

    static void Convert()
    {
        RequireAsset(TargetScenePath);
        EnsureFolder(MaterialFolder);

        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        var convertedMaterials = new Dictionary<Material, Material>();
        int convertedRendererCount = 0;
        int convertedSlotCount = 0;
        int excludedRendererCount = 0;

        foreach (Renderer renderer in GetSceneRenderers(scene))
        {
            if (!IsMeshRenderer(renderer))
                continue;

            if (IsExcluded(renderer.transform))
            {
                excludedRendererCount++;
                continue;
            }

            Material[] slots = renderer.sharedMaterials;
            bool changed = false;
            for (int index = 0; index < slots.Length; index++)
            {
                Material source = slots[index];
                if (source == null || IsGeneratedUnlitMaterial(source, unlitShader))
                    continue;

                if (!convertedMaterials.TryGetValue(source, out Material target))
                {
                    target = GetOrCreateUnlitMaterial(source, unlitShader);
                    convertedMaterials.Add(source, target);
                }

                slots[index] = target;
                convertedSlotCount++;
                changed = true;
            }

            if (!changed)
                continue;

            Undo.RecordObject(renderer, "Convert Mixer Room Renderer to Unlit");
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
            convertedRendererCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"Mixer Room Unlit conversion complete. " +
            $"Scene='{TargetScenePath}', renderers={convertedRendererCount}, " +
            $"materialSlots={convertedSlotCount}, uniqueMaterials={convertedMaterials.Count}, " +
            $"excludedRenderers={excludedRendererCount}, output='{MaterialFolder}'.");
    }

    static void Validate()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Validate(scene);
    }

    static void Validate(Scene scene)
    {
        ValidateBuildSceneRoute();

        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        int includedRendererCount = 0;
        int excludedRendererCount = 0;
        int includedSlotCount = 0;
        var failures = new List<string>();

        foreach (Renderer renderer in GetSceneRenderers(scene))
        {
            if (!IsMeshRenderer(renderer))
                continue;

            bool excluded = IsExcluded(renderer.transform);
            if (excluded)
                excludedRendererCount++;
            else
                includedRendererCount++;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    failures.Add($"{GetPath(renderer.transform)} has a missing material.");
                    continue;
                }

                string materialPath = AssetDatabase.GetAssetPath(material);
                if (excluded)
                {
                    if (materialPath.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                    {
                        failures.Add(
                            $"Excluded renderer '{GetPath(renderer.transform)}' references generated " +
                            $"material '{materialPath}'.");
                    }
                }
                else
                {
                    includedSlotCount++;
                    if (material.shader != unlitShader)
                    {
                        failures.Add(
                            $"Included renderer '{GetPath(renderer.transform)}' still uses " +
                            $"shader '{material.shader?.name ?? "<missing>"}' on '{material.name}'.");
                    }
                }
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException(
                $"Mixer Room Unlit validation failed ({failures.Count} issue(s)):\n" +
                string.Join("\n", failures));

        Debug.Log(
            $"Mixer Room Unlit validation passed. Scene='{TargetScenePath}', " +
            $"includedRenderers={includedRendererCount}, includedSlots={includedSlotCount}, " +
            $"excludedRenderers={excludedRendererCount}.");
    }

    static void ValidateBuildSceneRoute()
    {
        bool targetRegistered = false;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            targetRegistered |= buildScene.path == TargetScenePath;
        }

        if (!targetRegistered)
        {
            throw new InvalidOperationException(
                "Mixer Room Unlit play route is invalid. " +
                $"Expected Build Settings to contain '{TargetScenePath}'.");
        }
    }

    static Material GetOrCreateUnlitMaterial(Material source, Shader unlitShader)
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                source, out string guid, out long localId))
        {
            throw new InvalidOperationException(
                $"Material '{source.name}' is not a persistent asset and cannot be cloned safely.");
        }

        string safeName = MakeSafeFileName(source.name);
        string shortGuid = guid.Length > 8 ? guid.Substring(0, 8) : guid;
        string localIdText = localId.ToString(CultureInfo.InvariantCulture);
        string assetPath =
            $"{MaterialFolder}/{safeName}_{shortGuid}_{localIdText}_Unlit.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existing != null)
            return existing;

        Texture baseTexture = GetBaseTexture(source, out string textureProperty);
        Color baseColor = GetBaseColor(source);
        Vector2 textureScale = textureProperty != null
            ? source.GetTextureScale(textureProperty)
            : Vector2.one;
        Vector2 textureOffset = textureProperty != null
            ? source.GetTextureOffset(textureProperty)
            : Vector2.zero;
        bool transparent = IsTransparent(source, baseColor);
        bool alphaClip = IsAlphaClipped(source);
        float cutoff = source.HasProperty("_Cutoff")
            ? source.GetFloat("_Cutoff")
            : 0.5f;

        var target = new Material(source)
        {
            name = Path.GetFileNameWithoutExtension(assetPath),
            shader = unlitShader,
            enableInstancing = source.enableInstancing,
            doubleSidedGI = source.doubleSidedGI,
        };

        if (target.HasProperty("_BaseMap"))
        {
            target.SetTexture("_BaseMap", baseTexture);
            target.SetTextureScale("_BaseMap", textureScale);
            target.SetTextureOffset("_BaseMap", textureOffset);
        }

        if (target.HasProperty("_BaseColor"))
            target.SetColor("_BaseColor", baseColor);
        if (target.HasProperty("_Color"))
            target.SetColor("_Color", baseColor);
        if (target.HasProperty("_Cutoff"))
            target.SetFloat("_Cutoff", cutoff);
        if (target.HasProperty("_AlphaClip"))
            target.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);

        ConfigureSurface(target, transparent, alphaClip);
        AssetDatabase.CreateAsset(target, assetPath);
        return target;
    }

    static void ConfigureSurface(Material material, bool transparent, bool alphaClip)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", transparent ? 1f : 0f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        }
        if (material.HasProperty("_SrcBlendAlpha"))
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (material.HasProperty("_DstBlendAlpha"))
        {
            material.SetFloat(
                "_DstBlendAlpha",
                (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        }

        CoreUtils.SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);
        CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaClip);

        if (transparent)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (alphaClip)
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
        }
    }

    static Texture GetBaseTexture(Material material, out string propertyName)
    {
        string[] candidates = { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo" };
        foreach (string candidate in candidates)
        {
            if (!material.HasProperty(candidate))
                continue;

            Texture texture = material.GetTexture(candidate);
            if (texture == null)
                continue;

            propertyName = candidate;
            return texture;
        }

        propertyName = null;
        return material.mainTexture;
    }

    static Color GetBaseColor(Material material)
    {
        string[] candidates = { "_BaseColor", "_Color", "_TintColor" };
        foreach (string candidate in candidates)
        {
            if (material.HasProperty(candidate))
                return material.GetColor(candidate);
        }

        return Color.white;
    }

    static bool IsTransparent(Material material, Color baseColor)
    {
        if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
            return true;
        if (material.HasProperty("_Mode") && material.GetFloat("_Mode") >= 2f)
            return true;
        return material.renderQueue >= (int)RenderQueue.Transparent || baseColor.a < 0.999f;
    }

    static bool IsAlphaClipped(Material material)
    {
        if (material.IsKeywordEnabled("_ALPHATEST_ON"))
            return true;
        if (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f)
            return true;
        return material.renderQueue >= (int)RenderQueue.AlphaTest
            && material.renderQueue < (int)RenderQueue.Transparent;
    }

    static bool IsGeneratedUnlitMaterial(Material material, Shader unlitShader)
    {
        string path = AssetDatabase.GetAssetPath(material);
        return material.shader == unlitShader
            && path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal);
    }

    static IEnumerable<Renderer> GetSceneRenderers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                yield return renderer;
        }
    }

    static bool IsMeshRenderer(Renderer renderer)
        => renderer is MeshRenderer || renderer is SkinnedMeshRenderer;

    static bool IsExcluded(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (ExcludedObjectNames.Contains(current.name))
                return true;
            if (current.parent == null && ExcludedRootNames.Contains(current.name))
                return true;
        }

        return false;
    }

    static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    static string MakeSafeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "Material" : value;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    static void RequireAsset(string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            throw new FileNotFoundException($"Required asset was not found: {path}");
    }
}
