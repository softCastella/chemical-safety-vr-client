using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PPEMaskGlassSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string Mask1ModelPath = "Assets/Generated/PPE/MaskGlass/Mask1_GlassReady.fbx";
    const string Mask2ModelPath = "Assets/Generated/PPE/MaskGlass/Mask2_GlassReady.fbx";
    const string GlassMaterialPath = "Assets/Generated/PPE/MaskGlass/PPE_Mask_Glass.mat";
    const string Mask1UnlitMaterialFolder = "Assets/Generated/PPE/MaskGlass/Mask1UnlitMaterials";
    const string Mask1VisualName = "Mask1 Glass Ready Visual";
    const string Mask2VisualName = "Mask2 Glass Ready Visual";
    const string MenuRoot = "Tools/PPE/Mask Glass/";

    static readonly HashSet<string> Mask1GlassRendererNames = new()
    {
        "tripo_part_5",
        "tripo_part_29",
    };

    static readonly HashSet<string> Mask2GlassRendererNames = new()
    {
        "Mask2_Glass_Lens",
    };

    [MenuItem(MenuRoot + "Install In Open PPE Scene")]
    static void InstallInOpenPpeScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateTargetScene(scene))
            return;

        Material glassMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
        GameObject mask1Model = AssetDatabase.LoadAssetAtPath<GameObject>(Mask1ModelPath);
        GameObject mask2Model = AssetDatabase.LoadAssetAtPath<GameObject>(Mask2ModelPath);
        var errors = new List<string>();
        if (glassMaterial == null)
            errors.Add($"Missing glass material: {GlassMaterialPath}");
        if (mask1Model == null)
            errors.Add($"Missing generated model: {Mask1ModelPath}");
        if (mask2Model == null)
            errors.Add($"Missing generated model: {Mask2ModelPath}");

        GameObject mask1 = FindUniqueSceneObject(scene, "mask1", errors);
        GameObject mask2 = FindUniqueSceneObject(scene, "mask2", errors);
        if (errors.Count > 0)
        {
            Debug.LogError("[PPE Mask Glass] Installation prerequisites failed:\n- " + string.Join("\n- ", errors));
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install PPE Mask Glass Visuals");
        bool changed = false;
        changed |= InstallVisual(
            mask1,
            mask1Model,
            Mask1VisualName,
            Mask1GlassRendererNames,
            true,
            glassMaterial,
            errors);
        changed |= InstallVisual(
            mask2,
            mask2Model,
            Mask2VisualName,
            Mask2GlassRendererNames,
            false,
            glassMaterial,
            errors);
        Undo.CollapseUndoOperations(undoGroup);

        if (errors.Count > 0)
        {
            Debug.LogError("[PPE Mask Glass] Installation stopped:\n- " + string.Join("\n- ", errors));
            return;
        }

        if (!changed)
        {
            Debug.Log("[PPE Mask Glass] Both authored glass-ready visuals already exist. No scene values were changed.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = mask1;
        Debug.Log(
            "[PPE Mask Glass] Installed both glass-ready visuals. Original roots, Transforms, Colliders, " +
            "and interaction components were preserved. Review both masks in Scene/Game view, then save the scene manually.");
    }

    [MenuItem(MenuRoot + "Validate Open PPE Scene")]
    static void ValidateOpenPpeScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateTargetScene(scene))
            return;

        var errors = new List<string>();
        Material glassMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
        if (glassMaterial == null)
            errors.Add($"Missing glass material: {GlassMaterialPath}");

        GameObject mask1 = FindUniqueSceneObject(scene, "mask1", errors);
        GameObject mask2 = FindUniqueSceneObject(scene, "mask2", errors);
        if (mask1 != null)
            ValidateInstalledVisual(mask1, Mask1VisualName, Mask1GlassRendererNames, true, glassMaterial, errors);
        if (mask2 != null)
            ValidateInstalledVisual(mask2, Mask2VisualName, Mask2GlassRendererNames, false, glassMaterial, errors);

        if (errors.Count > 0)
        {
            Debug.LogError("[PPE Mask Glass] Validation failed:\n- " + string.Join("\n- ", errors));
            return;
        }

        Debug.Log(
            "[PPE Mask Glass] Static validation passed: original mask roots are preserved, source renderers are disabled, " +
            "and the generated visor/lens renderers use the authored URP glass material. Quest/OpenXR stereo rendering " +
            "still requires headset validation.");
    }

    static bool InstallVisual(
        GameObject sourceRoot,
        GameObject generatedModel,
        string visualName,
        ISet<string> glassRendererNames,
        bool useUnlitBodyMaterials,
        Material glassMaterial,
        ICollection<string> errors)
    {
        Transform existing = sourceRoot.transform.Find(visualName);
        Renderer[] sourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => existing == null || !renderer.transform.IsChildOf(existing))
            .ToArray();
        if (sourceRenderers.Length == 0)
        {
            errors.Add($"No source Renderer found under {GetPath(sourceRoot.transform)}.");
            return false;
        }

        var sourceMaterialsByName = sourceRenderers
            .GroupBy(renderer => renderer.name)
            .ToDictionary(group => group.Key, group => group.First().sharedMaterials);

        GameObject visual = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(generatedModel, sourceRoot.scene);
        if (visual == null)
        {
            errors.Add($"Could not instantiate generated model for {GetPath(sourceRoot.transform)}.");
            return false;
        }

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(visual, $"Install {visualName}");
            visual.name = visualName;
        }

        Renderer[] generatedRenderers = visual.GetComponentsInChildren<Renderer>(true);
        Renderer[] generatedBodyRenderers = generatedRenderers
            .Where(renderer => !glassRendererNames.Contains(renderer.name))
            .ToArray();
        if (generatedBodyRenderers.Length == 0)
        {
            if (existing == null)
                Undo.DestroyObjectImmediate(visual);
            errors.Add($"Generated model has no body Renderer for {GetPath(sourceRoot.transform)}.");
            return false;
        }

        Renderer sourceAlignmentRenderer = sourceRenderers
            .OrderByDescending(GetBoundsVolume)
            .First();
        Renderer generatedAlignmentRenderer = generatedBodyRenderers
            .FirstOrDefault(renderer => renderer.name == sourceAlignmentRenderer.name)
            ?? generatedBodyRenderers.OrderByDescending(GetBoundsVolume).First();
        AlignVisualWithSourceRenderer(
            sourceRoot.transform,
            visual.transform,
            sourceAlignmentRenderer.transform,
            generatedAlignmentRenderer.transform);
        FitVisualBoundsToSource(
            visual.transform,
            sourceAlignmentRenderer,
            generatedAlignmentRenderer,
            errors);

        bool foundGlassRenderer = false;
        foreach (Renderer renderer in generatedRenderers)
        {
            Undo.RecordObject(renderer, "Author PPE mask materials");
            if (glassRendererNames.Contains(renderer.name))
            {
                renderer.sharedMaterial = glassMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                foundGlassRenderer = true;
            }
            else if (sourceMaterialsByName.TryGetValue(renderer.name, out Material[] sourceMaterials))
            {
                renderer.sharedMaterials = useUnlitBodyMaterials
                    ? sourceMaterials.Select(material => GetOrCreateMask1UnlitMaterial(material, errors)).ToArray()
                    : sourceMaterials;
            }
        }

        if (!foundGlassRenderer)
        {
            Undo.DestroyObjectImmediate(visual);
            errors.Add($"Generated model has no expected glass Renderer for {GetPath(sourceRoot.transform)}.");
            return false;
        }

        foreach (Renderer sourceRenderer in sourceRenderers)
        {
            Undo.RecordObject(sourceRenderer, "Disable replaced mask source Renderer");
            sourceRenderer.enabled = false;
        }

        return true;
    }

    static void ValidateInstalledVisual(
        GameObject sourceRoot,
        string visualName,
        ISet<string> glassRendererNames,
        bool requireUnlitBodyMaterials,
        Material glassMaterial,
        ICollection<string> errors)
    {
        Transform visual = sourceRoot.transform.Find(visualName);
        if (visual == null)
        {
            errors.Add($"Installed visual is missing: {GetPath(sourceRoot.transform)}/{visualName}");
            return;
        }

        Renderer[] generatedRenderers = visual.GetComponentsInChildren<Renderer>(true);
        Renderer[] glassRenderers = generatedRenderers
            .Where(renderer => glassRendererNames.Contains(renderer.name))
            .ToArray();
        if (glassRenderers.Length == 0)
            errors.Add($"No glass Renderer found under {GetPath(visual)}.");
        foreach (Renderer renderer in glassRenderers)
        {
            if (renderer.sharedMaterial != glassMaterial)
                errors.Add($"Glass material mismatch: {GetPath(renderer.transform)}");
        }

        foreach (Renderer renderer in generatedRenderers)
        {
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(material => material == null))
                errors.Add($"Missing material on generated Renderer: {GetPath(renderer.transform)}");
            if (requireUnlitBodyMaterials && !glassRendererNames.Contains(renderer.name) &&
                renderer.sharedMaterials.Any(material => material != null &&
                    material.shader != null && material.shader.name != "Universal Render Pipeline/Unlit"))
            {
                errors.Add($"Mask1 body Renderer is not Unlit: {GetPath(renderer.transform)}");
            }
        }

        Renderer[] enabledSourceRenderers = sourceRoot.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !renderer.transform.IsChildOf(visual) && renderer.enabled)
            .ToArray();
        foreach (Renderer renderer in enabledSourceRenderers)
            errors.Add($"Replaced source Renderer is still enabled: {GetPath(renderer.transform)}");
    }

    static void AlignVisualWithSourceRenderer(
        Transform sourceRoot,
        Transform visual,
        Transform sourceRenderer,
        Transform generatedRenderer)
    {
        Undo.RecordObject(visual, "Align generated mask visual");

        Matrix4x4 rendererRelativeToVisual =
            visual.worldToLocalMatrix * generatedRenderer.localToWorldMatrix;
        Matrix4x4 targetVisualWorld =
            sourceRenderer.localToWorldMatrix * rendererRelativeToVisual.inverse;

        visual.SetParent(null, true);
        visual.SetPositionAndRotation(
            targetVisualWorld.GetColumn(3),
            targetVisualWorld.rotation);
        visual.localScale = targetVisualWorld.lossyScale;
        visual.SetParent(sourceRoot, true);
    }

    static float GetBoundsVolume(Renderer renderer)
    {
        Vector3 size = renderer.bounds.size;
        return size.x * size.y * size.z;
    }

    static void FitVisualBoundsToSource(
        Transform visual,
        Renderer sourceRenderer,
        Renderer generatedRenderer,
        ICollection<string> errors)
    {
        float sourceMagnitude = sourceRenderer.bounds.size.magnitude;
        float generatedMagnitude = generatedRenderer.bounds.size.magnitude;
        if (sourceMagnitude <= Mathf.Epsilon || generatedMagnitude <= Mathf.Epsilon)
        {
            errors.Add($"Cannot fit zero-sized Renderer bounds: {GetPath(generatedRenderer.transform)}");
            return;
        }

        Undo.RecordObject(visual, "Fit generated mask visual bounds");
        float uniformScale = sourceMagnitude / generatedMagnitude;
        visual.localScale *= uniformScale;
        visual.position += sourceRenderer.bounds.center - generatedRenderer.bounds.center;
    }

    static Material GetOrCreateMask1UnlitMaterial(Material source, ICollection<string> errors)
    {
        if (source == null)
            return null;
        if (source.shader != null && source.shader.name == "Universal Render Pipeline/Unlit")
            return source;

        EnsureAssetFolder(Mask1UnlitMaterialFolder);
        string safeName = string.Concat(source.name.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        string assetPath = $"{Mask1UnlitMaterialFolder}/{safeName}_Unlit.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            errors.Add("Universal Render Pipeline/Unlit shader was not found.");
            return source;
        }

        var material = new Material(shader)
        {
            name = safeName + "_Unlit",
            enableInstancing = true,
        };
        if (source.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            material.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap"));
            material.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap"));
        }
        else if (source.HasProperty("_MainTex"))
        {
            material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
            material.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
            material.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
        }

        Color baseColor = source.HasProperty("_BaseColor")
            ? source.GetColor("_BaseColor")
            : source.HasProperty("_Color")
                ? source.GetColor("_Color")
                : Color.white;
        material.SetColor("_BaseColor", baseColor);
        AssetDatabase.CreateAsset(material, assetPath);
        Undo.RegisterCreatedObjectUndo(material, "Create mask1 Unlit material");
        return material;
    }

    static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    static GameObject FindUniqueSceneObject(Scene scene, string objectName, ICollection<string> errors)
    {
        GameObject[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name == objectName)
            .Select(transform => transform.gameObject)
            .ToArray();
        if (matches.Length == 1)
            return matches[0];

        errors.Add(matches.Length == 0
            ? $"Scene object not found: {objectName}. Add and save the authored mask instance first."
            : $"Expected one scene object named '{objectName}', found {matches.Length}.");
        return null;
    }

    static bool ValidateTargetScene(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded && scene.path == TargetScenePath)
            return true;

        Debug.LogError(
            $"[PPE Mask Glass] Open the single target scene '{TargetScenePath}' before running this command. " +
            $"Active scene: '{scene.path}'.");
        return false;
    }

    static string GetPath(Transform transform)
    {
        if (transform == null)
            return "<missing>";

        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
