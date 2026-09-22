using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PPEWornEquipmentShadowSetup
{
    private const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    private const string SuitRootPath = "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear";
    private const string ShadowMaterialPath =
        "Assets/Materials/PPE/Shadows/PPE_ShadowCaster.mat";
    private const string ProxySuffix = " Worn Shadow Proxy";

    private static readonly string[] SourcePaths =
    {
        SuitRootPath,
        SuitRootPath + "/PPE_A_Boots_L",
        SuitRootPath + "/PPE_A_Boots_R",
        SuitRootPath + "/PPE_A_Taped_Boot_L",
        SuitRootPath + "/PPE_A_Taped_Boot_R",
    };

    [MenuItem("Tools/PPE/Configure Worn PPE Shadows")]
    public static void Configure()
    {
        Scene scene = RequireCleanTargetScene("configuring worn PPE shadows");
        Material shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
        if (shadowMaterial == null)
            throw new InvalidOperationException($"Missing shadow material '{ShadowMaterialPath}'.");

        Renderer[] sources = new Renderer[SourcePaths.Length];
        Renderer[] shadows = new Renderer[SourcePaths.Length];
        for (int index = 0; index < SourcePaths.Length; index++)
        {
            Transform sourceTransform = RequireTransformAtPath(scene, SourcePaths[index]);
            MeshFilter sourceFilter = sourceTransform.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = sourceTransform.GetComponent<MeshRenderer>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer == null)
            {
                throw new InvalidOperationException(
                    $"Worn PPE source '{SourcePaths[index]}' requires an authored MeshFilter and MeshRenderer.");
            }

            sources[index] = sourceRenderer;
            shadows[index] = ConfigureProxy(sourceTransform, sourceFilter.sharedMesh, shadowMaterial);
        }

        Transform suitRoot = RequireTransformAtPath(scene, SuitRootPath);
        PPEWornEquipmentShadowSync sync = suitRoot.GetComponent<PPEWornEquipmentShadowSync>();
        if (sync == null)
            sync = Undo.AddComponent<PPEWornEquipmentShadowSync>(suitRoot.gameObject);

        Undo.RecordObject(sync, "Connect worn PPE shadow visibility");
        sync.ConfigureForEditor(sources, shadows);
        EditorUtility.SetDirty(sync);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    [MenuItem("Tools/PPE/Validate Worn PPE Shadows")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before validating worn PPE shadows.");

        Material shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
        List<string> failures = new();
        Transform suitRoot = FindTransformAtPath(scene, SuitRootPath);
        PPEWornEquipmentShadowSync sync =
            suitRoot != null ? suitRoot.GetComponent<PPEWornEquipmentShadowSync>() : null;
        if (sync == null)
        {
            failures.Add("Missing PPEWornEquipmentShadowSync on the authored suit root.");
        }

        for (int index = 0; index < SourcePaths.Length; index++)
        {
            Transform source = FindTransformAtPath(scene, SourcePaths[index]);
            ValidateProxy(source, shadowMaterial, failures);
            if (sync != null)
                ValidateSyncPair(sync, index, source, failures);
        }

        if (sync != null &&
            (sync.SourceRenderers == null ||
             sync.ShadowRenderers == null ||
             sync.SourceRenderers.Length != SourcePaths.Length ||
             sync.ShadowRenderers.Length != SourcePaths.Length))
        {
            failures.Add("Worn PPE shadow sync arrays do not match the five approved silhouette sources.");
        }

        if (failures.Count > 0)
        {
            string message = "PPE worn equipment shadow validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[PPE Worn Shadows] PASS: suit and boot Shadows Only meshes follow authored transforms " +
            "and mirror the existing equipped Renderer visibility without runtime geometry creation.");
    }

    private static MeshRenderer ConfigureProxy(
        Transform source,
        Mesh mesh,
        Material shadowMaterial)
    {
        string proxyName = source.name + ProxySuffix;
        Transform proxy = source.Find(proxyName);
        if (proxy == null)
        {
            GameObject proxyObject = new(proxyName);
            Undo.RegisterCreatedObjectUndo(proxyObject, $"Create {proxyName}");
            proxy = proxyObject.transform;
            Undo.SetTransformParent(proxy, source, $"Parent {proxyName}");
        }

        Undo.RecordObject(proxy, $"Align {proxyName}");
        proxy.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        proxy.localScale = Vector3.one;
        proxy.gameObject.layer = source.gameObject.layer;
        EditorUtility.SetDirty(proxy);

        MeshFilter filter = proxy.GetComponent<MeshFilter>();
        if (filter == null)
            filter = Undo.AddComponent<MeshFilter>(proxy.gameObject);
        Undo.RecordObject(filter, $"Assign {proxyName} mesh");
        filter.sharedMesh = mesh;
        EditorUtility.SetDirty(filter);

        MeshRenderer renderer = proxy.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(proxy.gameObject);
        Undo.RecordObject(renderer, $"Configure {proxyName} renderer");
        renderer.sharedMaterial = shadowMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        renderer.enabled = false;
        EditorUtility.SetDirty(renderer);

        return renderer;
    }

    private static void ValidateProxy(
        Transform source,
        Material shadowMaterial,
        List<string> failures)
    {
        if (source == null)
        {
            failures.Add("Missing approved worn PPE source.");
            return;
        }

        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        Transform proxy = source.Find(source.name + ProxySuffix);
        if (sourceFilter == null || sourceRenderer == null || proxy == null)
        {
            failures.Add($"'{source.name}' is missing its authored source or shadow proxy components.");
            return;
        }

        MeshFilter filter = proxy.GetComponent<MeshFilter>();
        MeshRenderer renderer = proxy.GetComponent<MeshRenderer>();
        if (filter == null || renderer == null)
        {
            failures.Add($"'{proxy.name}' requires MeshFilter and MeshRenderer.");
            return;
        }

        if (proxy.parent != source ||
            Vector3.Distance(proxy.localPosition, Vector3.zero) > 0.0001f ||
            Quaternion.Angle(proxy.localRotation, Quaternion.identity) > 0.01f ||
            Vector3.Distance(proxy.localScale, Vector3.one) > 0.0001f)
        {
            failures.Add($"'{proxy.name}' does not preserve the source's authored local pose.");
        }
        if (filter.sharedMesh != sourceFilter.sharedMesh)
            failures.Add($"'{proxy.name}' does not use the source mesh.");
        if (renderer.sharedMaterial != shadowMaterial ||
            renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly ||
            renderer.receiveShadows)
        {
            failures.Add($"'{proxy.name}' does not use the approved Shadows Only renderer settings.");
        }
        if (renderer.enabled)
            failures.Add($"'{proxy.name}' must remain disabled in Edit Mode.");
        if (proxy.GetComponents<Collider>().Length != 0)
            failures.Add($"'{proxy.name}' must not add an interaction or physics Collider.");
        if (sourceRenderer.shadowCastingMode != ShadowCastingMode.On)
            failures.Add($"'{source.name}' source shadow mode changed unexpectedly.");
    }

    private static void ValidateSyncPair(
        PPEWornEquipmentShadowSync sync,
        int index,
        Transform source,
        List<string> failures)
    {
        if (source == null ||
            sync.SourceRenderers == null ||
            sync.ShadowRenderers == null ||
            index >= sync.SourceRenderers.Length ||
            index >= sync.ShadowRenderers.Length)
        {
            return;
        }

        Renderer sourceRenderer = source.GetComponent<Renderer>();
        Transform proxy = source.Find(source.name + ProxySuffix);
        Renderer shadowRenderer = proxy != null ? proxy.GetComponent<Renderer>() : null;
        if (sync.SourceRenderers[index] != sourceRenderer ||
            sync.ShadowRenderers[index] != shadowRenderer)
        {
            failures.Add($"Worn PPE shadow sync pair {index} does not match '{source.name}'.");
        }
    }

    private static Scene RequireCleanTargetScene(string operation)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException($"Stop Play Mode before {operation}.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before {operation}.");
        if (scene.isDirty)
            throw new InvalidOperationException("The active PPE scene has unsaved changes. Review or save them first.");
        return scene;
    }

    private static Transform RequireTransformAtPath(Scene scene, string path)
    {
        Transform target = FindTransformAtPath(scene, path);
        if (target == null)
            throw new InvalidOperationException($"Missing scene object '{path}'.");
        return target;
    }

    private static Transform FindTransformAtPath(Scene scene, string path)
    {
        string[] names = path.Split('/');
        GameObject root = Array.Find(scene.GetRootGameObjects(), candidate => candidate.name == names[0]);
        if (root == null)
            return null;

        Transform current = root.transform;
        for (int index = 1; index < names.Length; index++)
        {
            current = current.Find(names[index]);
            if (current == null)
                return null;
        }
        return current;
    }
}
