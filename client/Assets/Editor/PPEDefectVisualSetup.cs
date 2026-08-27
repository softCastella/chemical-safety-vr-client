using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PPEDefectVisualSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string MaterialFolder = "Assets/Materials/PPE/Defects";
    const string CrackMaterialPath = MaterialFolder + "/PPE_Defect_Crack_Unlit.mat";
    const string CrackHighlightMaterialPath = MaterialFolder + "/PPE_Defect_CrackHighlight_Unlit.mat";
    const string BootContaminationTemplateMaterialPath =
        "Assets/Materials/PPE/Boots_Inspection_Contamination.mat";
    const string BootContaminationMaskPath =
        "Assets/Textures/PPE/HazmatSuit_ChemicalStainMask.png";
    const string HoleRimMaterialPath = MaterialFolder + "/PPE_Defect_BootHoleRim_Unlit.mat";
    const string HoleMaterialPath = MaterialFolder + "/PPE_Defect_BootHole_Unlit.mat";
    const string CementMaterialPath = MaterialFolder + "/PPE_Defect_Cement_Unlit.mat";
    const string GravelMaterialPath = MaterialFolder + "/PPE_Defect_Gravel_Unlit.mat";
    const string GeneratedRootName = "PPE_Defect_Visuals";
    const string GasMaskName = "PPE_A_Mask";
    const string RightBootName = "PPE_A_Boots_R";
    const string LeftBootName = "PPE_A_Boots_L";
    const string HelmetStrapName = "PPE_A_Helmet_Strap";
    const string HelmetNoStrapName = "PPE_A_Helmet_NoStrap";

    [MenuItem("Tools/PPE/Apply Defect Visuals (HandTest Scale)")]
    public static void ApplyFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying PPE defect visuals.");
            return;
        }

        Apply(scene);
    }

    [MenuItem("Tools/PPE/Apply Boot Contamination Only (HandTest Scale)")]
    public static void ApplyBootDefectsOnlyFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying boot defect visuals.");
            return;
        }

        ApplyBootDefectsOnly(scene);
    }

    [MenuItem("Tools/PPE/Apply Mask Glass Crack Only (HandTest Scale)")]
    public static void ApplyMaskGlassCrackOnlyFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying the mask glass crack.");
            return;
        }

        ApplyMaskGlassCrackOnly(scene);
    }

    public static void ApplyFromBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Apply(scene);
    }

    /// <summary>
    /// Repairs only the helmet interaction ownership. This leaves mask, boot,
    /// and other PPE presentation values untouched.
    /// </summary>
    public static void ApplyHelmetInteractionHostOnly(Scene scene)
    {
        GameObject helmet = FindUnique(scene, HelmetStrapName);
        GameObject helmetWrong = FindUnique(scene, HelmetNoStrapName);
        ConfigureHelmet(helmet, helmetWrong);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        Validate(scene);
        Debug.Log("PPE helmet interaction ownership applied: PPE_A_Helmet_Strap is the sole grab host and PPE_A_Helmet_NoStrap is visual-only.");
    }

    [MenuItem("Tools/PPE/Validate Defect Visuals (HandTest Scale)")]
    public static void ValidateFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before validating PPE defect visuals.");
            return;
        }

        Validate(scene);
    }

    [MenuItem("Tools/PPE/Clear Generated Defect Visuals (HandTest Scale)")]
    public static void ClearFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before clearing generated PPE defect visuals.");
            return;
        }

        ClearGeneratedVisuals(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        Debug.Log("Cleared only generated PPE defect visual children. Original PPE mesh and interaction objects were preserved.");
    }

    public static void Apply(Scene scene)
    {
        EnsureFolder(MaterialFolder);

        Material crackMaterial = GetOrCreateMaterial(
            CrackMaterialPath,
            new Color(0.38f, 0.68f, 0.84f, 0.30f),
            true);
        Material crackHighlightMaterial = GetOrCreateMaterial(
            CrackHighlightMaterialPath,
            new Color(0.88f, 0.98f, 1f, 0.62f),
            true);
        GameObject mask = FindInspectionPpeItem(scene, GasMaskName);
        GameObject bootsRight = FindInspectionPpeItem(scene, RightBootName);
        GameObject bootsLeft = FindInspectionPpeItem(scene, LeftBootName);
        GameObject helmet = FindUnique(scene, HelmetStrapName);
        GameObject helmetWrong = FindUnique(scene, HelmetNoStrapName);

        ConfigureMask(mask, crackMaterial, crackHighlightMaterial);
        ConfigureBoot(bootsRight);
        ConfigureBoot(bootsLeft);
        ConfigureHelmet(helmet, helmetWrong);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log("PPE defect visuals applied: glass-style mask crack, boot surface contamination, and helmet visual swap.");
    }

    /// <summary>
    /// Restores only the boot contamination state setup. The contamination uses
    /// the same chemical-stain mask shader as the inspection hazmat suit and is
    /// applied directly to each boot's authored surface material.
    /// </summary>
    public static void ApplyBootDefectsOnly(Scene scene)
    {
        EnsureFolder(MaterialFolder);

        ConfigureBoot(FindInspectionPpeItem(scene, RightBootName));
        ConfigureBoot(FindInspectionPpeItem(scene, LeftBootName));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        AssetDatabase.SaveAssets();
        ValidateBootDefects(scene);
        Debug.Log("Boot contamination setup applied: authored boot surfaces use the contaminated state only.");
    }

    public static void ApplyMaskGlassCrackOnly(Scene scene)
    {
        EnsureFolder(MaterialFolder);

        Material crackMaterial = GetOrCreateMaterial(
            CrackMaterialPath,
            new Color(0.38f, 0.68f, 0.84f, 0.30f),
            true);
        Material crackHighlightMaterial = GetOrCreateMaterial(
            CrackHighlightMaterialPath,
            new Color(0.88f, 0.98f, 1f, 0.62f),
            true);
        ConfigureMask(FindInspectionPpeItem(scene, GasMaskName), crackMaterial, crackHighlightMaterial);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        AssetDatabase.SaveAssets();
        ValidateMaskGlassCrack(scene);
        Debug.Log("Mask glass crack applied with transparent, double-sided URP materials.");
    }

    static void ClearGeneratedVisuals(Scene scene)
    {
        foreach (string itemName in new[] { GasMaskName, RightBootName, LeftBootName })
        {
            GameObject item = FindInspectionPpeItem(scene, itemName);
            string rootName = itemName == GasMaskName
                ? "Mask_Crack_Visual"
                : GeneratedRootName;
            Transform root = item.transform.Find(rootName);
            if (root != null)
                ClearGeneratedChildren(root);
        }
    }

    static void ConfigureMask(GameObject mask, Material crackMaterial, Material highlightMaterial)
    {
        PPEInspectionState state = RequireComponent<PPEInspectionState>(mask);
        GameObject root = GetOrCreateVisualRoot(mask.transform, "Mask_Crack_Visual");
        ClearGeneratedChildren(root.transform);

        Renderer[] lensRenderers = FindMaskLensRenderers(mask.transform);
        if (lensRenderers.Length == 0)
        {
            throw new InvalidOperationException(
                $"No mask lens Renderer was found under '{GetPath(mask.transform)}'. " +
                "Expected a Glass, Visor, or Lens material; crack placement was not generated.");
        }

        Bounds bounds = GetCombinedLocalBounds(mask.transform, lensRenderers);
        float surfaceOffset = Mathf.Clamp(bounds.size.z * 0.0125f, 0.001f, 0.01f);
        float lineWidth = Mathf.Clamp(
            Mathf.Min(bounds.size.x, bounds.size.y) * 0.035f,
            0.003f,
            0.02f);

        foreach (int side in new[] { -1, 1 })
        {
            float surfaceZ = side > 0
                ? bounds.max.z + surfaceOffset
                : bounds.min.z - surfaceOffset;
            Vector3[][] branches = BuildMaskCrackBranches(bounds, surfaceZ);
            for (int index = 0; index < branches.Length; index++)
            {
                CreateCrackLine(
                    root.transform,
                    $"Crack_Highlight_{(side > 0 ? "Front" : "Back")}_{index + 1}",
                    branches[index],
                    highlightMaterial,
                    lineWidth);
                CreateCrackLine(
                    root.transform,
                    $"Crack_Edge_{(side > 0 ? "Front" : "Back")}_{index + 1}",
                    branches[index],
                    crackMaterial,
                    lineWidth * 0.55f);
            }
        }

        PPEConditionVisualAppearance appearance =
            GetOrAddComponent<PPEConditionVisualAppearance>(mask);
        appearance.ConfigureForEditor(state, Array.Empty<GameObject>(), new[] { root });
        root.SetActive(state.CurrentCondition == PPEItemCondition.Contaminated);
        EditorUtility.SetDirty(mask);
    }

    static void ConfigureBoot(GameObject boot)
    {
        PPEInspectionState state = RequireComponent<PPEInspectionState>(boot);
        PPEItemPresentationBinding binding = RequireComponent<PPEItemPresentationBinding>(boot);
        if (binding.InitialCondition != PPEItemCondition.Contaminated)
        {
            binding.ConfigureForEditor(
                binding.ItemIdentity,
                binding.InspectionVisual,
                binding.EquippedVisual,
                PPEItemCondition.Contaminated);
            EditorUtility.SetDirty(binding);
        }

        GameObject root = GetOrCreateVisualRoot(boot.transform, GeneratedRootName);
        ClearGeneratedChildren(root.transform);

        Renderer bootRenderer = FindBootSurfaceRenderer(boot);
        Material contaminationMaterial = GetOrCreateBootContaminationMaterial(
            boot.name,
            bootRenderer.sharedMaterial);
        bootRenderer.sharedMaterial = contaminationMaterial;
        EditorUtility.SetDirty(bootRenderer);

        PPEConditionVisualAppearance previousSurfaceTint =
            boot.GetComponent<PPEConditionVisualAppearance>();
        if (previousSurfaceTint != null)
            Undo.DestroyObjectImmediate(previousSurfaceTint);

        PPEConditionAppearance appearance = GetOrAddComponent<PPEConditionAppearance>(boot);
        appearance.ConfigureForEditor(
            state,
            new[] { bootRenderer },
            0f,
            1f);
        root.SetActive(false);
        EditorUtility.SetDirty(boot);
    }

    static void ConfigureHelmet(GameObject helmet, GameObject helmetWrong)
    {
        // The strap helmet is the one interaction host. The no-strap helmet is a visual child
        // selected by the host's contaminated condition; it must never own its
        // own Rigidbody, Grab, marker, action panel, or replacement flow.
        PPEInspectionState state = RequireComponent<PPEInspectionState>(helmet);
        PPEItemPresentationBinding binding = RequireComponent<PPEItemPresentationBinding>(helmet);
        binding.ConfigureForEditor(
            binding.ItemIdentity,
            helmet,
            binding.EquippedVisual,
            PPEItemCondition.Contaminated);

        if (helmetWrong.transform.parent != helmet.transform)
            helmetWrong.transform.SetParent(helmet.transform, true);

        PPEHelmetDefectReplacement replacement =
            helmetWrong.GetComponent<PPEHelmetDefectReplacement>();
        if (replacement != null)
            Undo.DestroyObjectImmediate(replacement);

        // Do not leave disabled XRI physics behind on the visual subtree. XRI can
        // initialize a required Rigidbody while entering Play Mode, which makes a
        // disabled duplicate body simulate against the actual helmet host. The
        // defective model is a render-only child, so it owns no grab, marker,
        // collider, or Rigidbody at all.
        // The nested legacy state stack has RequireComponent links all the way
        // down to XRGrabInteractable. Remove it from its consumers to its
        // dependencies, then remove the physical components.
        foreach (PPEActionPanelController panel in
                 helmetWrong.GetComponentsInChildren<PPEActionPanelController>(true))
            Undo.DestroyObjectImmediate(panel);

        foreach (PPEConditionVisualAppearance duplicateAppearance in
                 helmetWrong.GetComponentsInChildren<PPEConditionVisualAppearance>(true))
            Undo.DestroyObjectImmediate(duplicateAppearance);

        foreach (PPEMarkerToggleGrab marker in
                 helmetWrong.GetComponentsInChildren<PPEMarkerToggleGrab>(true))
            Undo.DestroyObjectImmediate(marker);

        foreach (PPEInspectionState duplicateState in
                 helmetWrong.GetComponentsInChildren<PPEInspectionState>(true))
            Undo.DestroyObjectImmediate(duplicateState);

        foreach (PPEItemPresentationBinding duplicateBinding in
                 helmetWrong.GetComponentsInChildren<PPEItemPresentationBinding>(true))
            Undo.DestroyObjectImmediate(duplicateBinding);

        foreach (PPEItemIdentity duplicateIdentity in
                 helmetWrong.GetComponentsInChildren<PPEItemIdentity>(true))
            Undo.DestroyObjectImmediate(duplicateIdentity);

        foreach (XRGrabInteractable grab in
                 helmetWrong.GetComponentsInChildren<XRGrabInteractable>(true))
            Undo.DestroyObjectImmediate(grab);

        foreach (Rigidbody rigidbody in helmetWrong.GetComponentsInChildren<Rigidbody>(true))
            Undo.DestroyObjectImmediate(rigidbody);

        foreach (Collider collider in helmetWrong.GetComponentsInChildren<Collider>(true))
            Undo.DestroyObjectImmediate(collider);

        foreach (Behaviour behaviour in helmetWrong.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour is PPEConditionVisualAppearance)
                continue;

            behaviour.enabled = false;
            EditorUtility.SetDirty(behaviour);
        }

        foreach (string childName in new[] { "XR Item Marker_small", "Action Panel Pose", "equipped_placeholder" })
        {
            Transform child = helmetWrong.transform.Find(childName);
            if (child != null)
                child.gameObject.SetActive(false);
        }

        foreach (PPEActionPanelSharedPresentation presentation in
                 helmetWrong.GetComponentsInChildren<PPEActionPanelSharedPresentation>(true))
        {
            presentation.gameObject.SetActive(false);
        }

        foreach (Transform visualTransform in helmetWrong.GetComponentsInChildren<Transform>(true))
        {
            if (visualTransform.name == "Helmet_Normal_Visual")
                visualTransform.gameObject.SetActive(false);
            else if (visualTransform.name == "Helmet_Defect_Visual")
                visualTransform.gameObject.SetActive(true);
        }

        Transform normalVisual = helmet.transform.Find("Helmet_Normal_Visual");
        if (normalVisual == null)
        {
            GameObject normal = new GameObject("Helmet_Normal_Visual");
            normalVisual = normal.transform;
            normalVisual.SetParent(helmet.transform, false);

            List<Transform> meshChildren = new();
            foreach (Transform child in helmet.transform)
            {
                if (child == normalVisual || child == helmetWrong.transform)
                    continue;
                if (child.name.StartsWith("tripo_part_", StringComparison.Ordinal))
                    meshChildren.Add(child);
            }

            foreach (Transform child in meshChildren)
                child.SetParent(normalVisual, true);
        }

        PPEConditionVisualAppearance appearance =
            GetOrAddComponent<PPEConditionVisualAppearance>(helmet);
        appearance.ConfigureForEditor(
            state,
            new[] { normalVisual.gameObject },
            new[] { helmetWrong });
        // Keep the Inspector preview aligned with the authored initial condition.
        // PPEInspectionState reapplies this same condition when Play Mode starts.
        normalVisual.gameObject.SetActive(false);
        helmetWrong.SetActive(true);
        helmet.SetActive(true);
        EditorUtility.SetDirty(helmet);
    }

    static GameObject CreateCrackLine(
        Transform parent,
        string name,
        Vector3[] positions,
        Material material,
        float width)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.alignment = LineAlignment.TransformZ;
        line.positionCount = positions.Length;
        line.SetPositions(positions);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return lineObject;
    }

    static Vector3[][] BuildMaskCrackBranches(Bounds bounds, float z)
    {
        Vector3 Point(float x, float y) => new(
            bounds.center.x + bounds.size.x * x,
            bounds.center.y + bounds.size.y * y,
            z);

        return new[]
        {
            new[] { Point(0.00f, 0.30f), Point(-0.035f, 0.16f), Point(0.025f, 0.03f), Point(-0.005f, -0.10f), Point(0.06f, -0.24f) },
            new[] { Point(-0.02f, 0.16f), Point(-0.25f, 0.22f), Point(-0.39f, 0.16f) },
            new[] { Point(0.025f, 0.03f), Point(0.25f, 0.10f), Point(0.40f, 0.07f) },
            new[] { Point(-0.005f, -0.10f), Point(-0.20f, -0.22f), Point(-0.34f, -0.24f) },
        };
    }

    static void CreateBootSurfaceVisual(
        Transform root,
        string sideName,
        int side,
        Bounds bounds,
        float width,
        float height,
        float surfaceOffset,
        int seed,
        Material contaminationMaterial)
    {
        float surfaceZ = side > 0
            ? bounds.max.z + surfaceOffset
            : bounds.min.z - surfaceOffset;
        float depth = Mathf.Max(bounds.size.z, 0.001f);
        GameObject surface = new GameObject($"Boot_Surface_{sideName}");
        surface.transform.SetParent(root, false);
        surface.transform.localPosition = new Vector3(0f, 0f, surfaceZ);

        GameObject stains = new GameObject("Boot_Contamination_Stains");
        stains.transform.SetParent(surface.transform, false);
        float[] horizontalOffsets = { -0.15f, 0.02f, 0.17f, -0.05f };
        float[] verticalOffsets = { 0.10f, 0.15f, 0.08f, 0.22f };
        for (int index = 0; index < horizontalOffsets.Length; index++)
        {
            float variation = 0.85f + ((seed + index * 3) % 5) * 0.07f;
            CreateBlob(
                stains.transform,
                $"Contamination_Stain_{index + 1}",
                new Vector3(
                    bounds.center.x + width * horizontalOffsets[index],
                    bounds.min.y + height * verticalOffsets[index],
                    side * depth * 0.018f),
                new Vector3(
                    width * 0.14f * variation,
                    height * 0.045f * variation,
                    Mathf.Max(depth * 0.025f, 0.0025f)),
                contaminationMaterial);
        }
    }

    static GameObject CreateBlob(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blob.name = name;
        blob.transform.SetParent(parent, false);
        blob.transform.localPosition = localPosition;
        blob.transform.localScale = localScale;
        Collider collider = blob.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        MeshRenderer renderer = blob.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return blob;
    }

    static GameObject GetOrCreateVisualRoot(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        return root;
    }

    static void ClearGeneratedChildren(Transform root)
    {
        for (int index = root.childCount - 1; index >= 0; index--)
            UnityEngine.Object.DestroyImmediate(root.GetChild(index).gameObject);
    }

    static Renderer[] FindMaskLensRenderers(Transform mask)
    {
        List<Renderer> matches = new();
        foreach (Renderer renderer in mask.GetComponentsInChildren<Renderer>(true))
        {
            if (!IsSourceVisualRenderer(renderer, mask))
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName = material.name.ToLowerInvariant();
                if (materialName.Contains("glass") ||
                    materialName.Contains("visor") ||
                    materialName.Contains("lens"))
                {
                    matches.Add(renderer);
                    break;
                }
            }
        }

        return matches.ToArray();
    }

    static Bounds GetCombinedLocalBounds(Transform root)
    {
        return GetCombinedLocalBounds(
            root,
            root.GetComponentsInChildren<Renderer>(true));
    }

    static Bounds GetCombinedLocalBounds(Transform root, Renderer[] renderers)
    {
        if (renderers.Length == 0)
            throw new InvalidOperationException($"No renderer was found under '{GetPath(root)}'.");

        bool initialized = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (!IsSourceVisualRenderer(renderer, root))
                continue;

            // Do not convert Renderer.bounds (a world-space AABB) back into the
            // boot's local space. Rotated boots inflate that AABB and place the
            // visual overlay well off the surface. Transform the mesh-local
            // corners directly into the requested root space instead.
            Bounds rendererLocalBounds = renderer.localBounds;
            Vector3 center = rendererLocalBounds.center;
            Vector3 extents = rendererLocalBounds.extents;
            Matrix4x4 rendererToRoot = root.worldToLocalMatrix * renderer.localToWorldMatrix;
            Vector3[] corners =
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = rendererToRoot.MultiplyPoint3x4(corner);
                if (!initialized)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    initialized = true;
                }
                else
                    bounds.Encapsulate(localCorner);
            }
        }

        return bounds;
    }

    static bool IsSourceVisualRenderer(Renderer renderer, Transform root)
    {
        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
            return false;

        Transform current = renderer.transform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("marker") ||
                name.Contains("action panel") ||
                name.Contains("panel pose") ||
                name.Contains("canvas") ||
                name.Contains("label") ||
                name.Contains("button") ||
                name.Contains("feedback") ||
                name.Contains("equipped_placeholder") ||
                name.Contains("ppe_defect_visuals"))
            {
                return false;
            }

            if (current == root)
                break;
            current = current.parent;
        }

        return true;
    }

    static Material GetOrCreateMaterial(string path, Color color, bool transparent)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("Universal Render Pipeline/Unlit shader was not found.");

        if (material == null)
        {
            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", transparent ? 1f : 0f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", transparent ? (float)BlendMode.SrcAlpha : (float)BlendMode.One);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);
        if (transparent)
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = transparent
            ? (int)RenderQueue.Transparent
            : (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Renderer FindBootSurfaceRenderer(GameObject boot)
    {
        Renderer[] candidates = boot.GetComponentsInChildren<Renderer>(true)
            .Where(candidate =>
                candidate.gameObject.name == boot.name &&
                IsSourceVisualRenderer(candidate, boot.transform))
            .ToArray();
        if (candidates.Length != 1)
        {
            throw new InvalidOperationException(
                $"'{GetPath(boot.transform)}' must have exactly one authored boot Renderer. " +
                $"Found {candidates.Length}; the contamination material was not assigned.");
        }

        return candidates[0];
    }

    static Material GetOrCreateBootContaminationMaterial(string bootName, Material authoredBootMaterial)
    {
        if (authoredBootMaterial == null)
            throw new MissingReferenceException($"'{bootName}' has no authored boot material.");

        Material template = AssetDatabase.LoadAssetAtPath<Material>(BootContaminationTemplateMaterialPath);
        Texture contaminationMask = AssetDatabase.LoadAssetAtPath<Texture>(BootContaminationMaskPath);
        if (template == null || contaminationMask == null)
        {
            throw new MissingReferenceException(
                "The hazmat chemical-stain material template or mask is missing. " +
                "Boot contamination was not changed.");
        }

        Texture bootBaseMap = GetBaseTexture(authoredBootMaterial);
        if (bootBaseMap == null)
        {
            throw new MissingReferenceException(
                $"'{authoredBootMaterial.name}' has no base texture. " +
                "Boot contamination must keep the authored boot texture.");
        }

        string materialPath = MaterialFolder + "/PPE_" + bootName + "_ChemicalContamination.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(template)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(materialPath),
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = template.shader;
        material.SetTexture("_BaseMap", bootBaseMap);
        material.SetTexture("_ContaminationMask", contaminationMask);
        material.SetFloat("_ContaminationStrength", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture GetBaseTexture(Material material)
    {
        if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            return material.GetTexture("_BaseMap");
        if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
            return material.GetTexture("_MainTex");
        return null;
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

    static T RequireComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            throw new MissingComponentException($"'{GetPath(target.transform)}' requires {typeof(T).Name}.");
        return component;
    }

    static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    static GameObject FindUnique(Scene scene, string objectName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName)
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"More than one '{objectName}' exists in '{scene.path}'.");
                match = candidate.gameObject;
            }
        }

        if (match == null)
            throw new MissingReferenceException($"'{objectName}' was not found in '{scene.path}'.");
        return match;
    }

    /// <summary>
    /// The inspection rack and worn body intentionally use the same standardized
    /// asset names. Defect visuals belong only to the inspection-rack instance.
    /// </summary>
    static GameObject FindInspectionPpeItem(Scene scene, string objectName)
    {
        GameObject ppeRoot = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "PPE");
        if (ppeRoot == null)
            throw new MissingReferenceException($"Inspection PPE root was not found in '{scene.path}'.");

        Transform item = ppeRoot.transform.Find(objectName);
        if (item == null)
        {
            throw new MissingReferenceException(
                $"Inspection PPE item 'PPE/{objectName}' was not found in '{scene.path}'.");
        }

        return item.gameObject;
    }

    static void ValidateBootDefects(Scene scene)
    {
        foreach (string name in new[] { RightBootName, LeftBootName })
        {
            GameObject boot = FindInspectionPpeItem(scene, name);
            Transform root = boot.transform.Find(GeneratedRootName);
            PPEConditionAppearance appearance = boot.GetComponent<PPEConditionAppearance>();
            Renderer bootRenderer = FindBootSurfaceRenderer(boot);
            Material material = bootRenderer.sharedMaterial;
            Texture contaminationMask = AssetDatabase.LoadAssetAtPath<Texture>(BootContaminationMaskPath);
            if (root == null || root.childCount != 0 || appearance == null ||
                appearance.InspectionState == null || appearance.TargetRenderers == null ||
                appearance.TargetRenderers.Length != 1 || appearance.TargetRenderers[0] != bootRenderer ||
                material == null || material.shader == null ||
                material.shader.name != "Tyche/PPE/Hazmat Contamination" ||
                material.GetTexture("_ContaminationMask") != contaminationMask ||
                Mathf.Abs(appearance.CleanStrength) > 0.001f ||
                appearance.ContaminatedStrength < 0.999f)
            {
                throw new InvalidOperationException(
                    $"'{GetPath(boot.transform)}' has no complete hazmat-mask boot contamination binding.");
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                throw new InvalidOperationException(
                    $"'{GetPath(collider.transform)}' must not contain a Collider. " +
                    "Boot contamination is visual-only and must not affect PPE Grab.");
            }
        }

        Debug.Log("Boot contamination validation passed: both boots use the hazmat chemical-stain mask on their authored surfaces.");
    }

    static void ValidateMaskGlassCrack(Scene scene)
    {
        GameObject mask = FindInspectionPpeItem(scene, GasMaskName);
        Transform root = mask.transform.Find("Mask_Crack_Visual");
        PPEConditionVisualAppearance appearance = mask.GetComponent<PPEConditionVisualAppearance>();
        LineRenderer[] cracks = root == null
            ? Array.Empty<LineRenderer>()
            : root.GetComponentsInChildren<LineRenderer>(true);

        if (appearance == null || appearance.InspectionState == null ||
            appearance.ContaminatedVisuals == null || appearance.ContaminatedVisuals.Length != 1 ||
            appearance.ContaminatedVisuals[0] != root.gameObject || cracks.Length == 0)
        {
            throw new InvalidOperationException(
                $"'{GetPath(mask.transform)}' has no complete transparent mask-glass crack binding.");
        }

        foreach (LineRenderer crack in cracks)
        {
            Material material = crack.sharedMaterial;
            if (material == null || material.renderQueue < (int)RenderQueue.Transparent ||
                material.HasProperty("_Cull") && !Mathf.Approximately(material.GetFloat("_Cull"), (float)CullMode.Off))
            {
                throw new InvalidOperationException(
                    $"'{GetPath(crack.transform)}' must use a transparent, double-sided glass-crack material.");
            }
        }

        Debug.Log("Mask glass crack validation passed: transparent, double-sided crack lines are bound to the contaminated state.");
    }

    static void Validate(Scene scene)
    {
        foreach (string name in new[] { GasMaskName, HelmetStrapName })
        {
            GameObject target = name == HelmetStrapName
                ? FindUnique(scene, name)
                : FindInspectionPpeItem(scene, name);
            PPEConditionVisualAppearance appearance =
                target.GetComponent<PPEConditionVisualAppearance>();
            if (appearance == null ||
                appearance.InspectionState == null ||
                appearance.ContaminatedVisuals == null ||
                appearance.ContaminatedVisuals.Length == 0)
            {
                throw new InvalidOperationException($"'{GetPath(target.transform)}' has no complete PPE defect visual binding.");
            }
        }

        ValidateBootDefects(scene);
        ValidateMaskGlassCrack(scene);

        GameObject helmetWrong = FindUnique(scene, HelmetNoStrapName);
        if (helmetWrong.transform.parent == null || helmetWrong.transform.parent.name != HelmetStrapName)
            throw new InvalidOperationException("PPE_A_Helmet_NoStrap must be a visual child of PPE_A_Helmet_Strap.");

        foreach (Behaviour behaviour in helmetWrong.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour.enabled)
                throw new InvalidOperationException(
                    $"'{GetPath(behaviour.transform)}' still has an enabled interaction component under helmet_wrong.");
        }

        if (helmetWrong.GetComponentsInChildren<XRGrabInteractable>(true).Length != 0 ||
            helmetWrong.GetComponentsInChildren<PPEMarkerToggleGrab>(true).Length != 0)
        {
            throw new InvalidOperationException(
                "helmet_wrong must contain no XR grab or marker component. " +
                "Grab belongs only to the parent helmet host.");
        }

        if (helmetWrong.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
            helmetWrong.GetComponentsInChildren<Collider>(true).Length != 0)
        {
            throw new InvalidOperationException(
                "helmet_wrong must contain no Rigidbody or Collider. " +
                "A visual-only child cannot participate in physics.");
        }

        if (helmetWrong.GetComponentsInChildren<PPEInspectionState>(true).Length != 0 ||
            helmetWrong.GetComponentsInChildren<PPEActionPanelController>(true).Length != 0 ||
            helmetWrong.GetComponentsInChildren<PPEConditionVisualAppearance>(true).Length != 0)
        {
            throw new InvalidOperationException(
                "helmet_wrong must contain no independent PPE state, panel, or condition visual controller. " +
                "The parent helmet owns the complete interaction and condition state.");
        }

        foreach (string childName in new[] { "XR Item Marker_small", "Action Panel Pose", "equipped_placeholder" })
        {
            Transform child = helmetWrong.transform.Find(childName);
            if (child != null && child.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    $"'{GetPath(child)}' must stay inactive. " +
                    "Grab and the action panel belong only on the parent 'helmet' host. " +
                    "An active marker under helmet_wrong steals near hits without an enabled XRGrabInteractable.");
            }
        }

        ValidateNoLegacyGeneratedVisuals(scene);

        Debug.Log("PPE defect visual validation passed: authored host, visual groups, and no duplicate helmet interaction components.");
    }

    static void ValidateNoLegacyGeneratedVisuals(Scene scene)
    {
        foreach (string itemName in new[] { GasMaskName, RightBootName, LeftBootName })
        {
            GameObject item = FindInspectionPpeItem(scene, itemName);
            foreach (Transform child in item.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Cement_Patch" ||
                    child.name == "Crack_Highlight_1" ||
                    child.name == "Crack_Edge_1")
                {
                    throw new InvalidOperationException(
                        $"Legacy PPE defect visual '{GetPath(child)}' remains. " +
                        "Run Clear Generated Defect Visuals before applying the current generator.");
                }
            }
        }
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
}
