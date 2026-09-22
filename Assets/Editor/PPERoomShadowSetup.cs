using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class PPERoomShadowSetup
{
    public const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    public const string LightName = "PPE Main Shadow Light";
    public const string ProxyName = "XR Player Shadow Proxy";
    public const string FurnitureProxyRootName = "PPE Furniture Shadow Proxies";
    public const string ReceiverShaderName = "Chemical Safety VR/PPE Unlit Shadow Receiver";

    private const string RoomPath = "PPE Room/Room";
    private const string XrOriginPath = "XR Origin (VR)";
    private const string HeadPath = "XR Origin (VR)/Camera Offset/Main Camera";
    private const string LeftControllerPath = "XR Origin (VR)/Camera Offset/Left Controller";
    private const string RightControllerPath = "XR Origin (VR)/Camera Offset/Right Controller";
    private const string FloorPath = "PPE Room/Room/Floor";
    private const string MobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string ShadowMaterialFolder = "Assets/Materials/PPE/Shadows";
    private const string FloorReceiverMaterialPath =
        ShadowMaterialFolder + "/PPE_Room_Floor_ShadowReceiver.mat";
    private const string ShadowCasterMaterialPath =
        ShadowMaterialFolder + "/PPE_ShadowCaster.mat";

    private static readonly string[] ShadowPartNames =
    {
        "Head Shadow",
        "Torso Shadow",
        "Left Arm Shadow",
        "Right Arm Shadow",
        "Left Leg Shadow",
        "Right Leg Shadow",
    };

    internal static readonly string[] FurnitureShadowTargetPaths =
    {
        "interiorObjects/Bg/PPE_B_YellowTrashBin_01",
        "interiorObjects/Bg/PPE_B_YellowTrashBin_02",
        "interiorObjects/Bg/PPE_B_CleaningCart",
        "interiorObjects/Bg/PPE_B_MaskLocker",
        "interiorObjects/Bg/PPE_B_SuitHanger",
        "interiorObjects/Bg/PPE_B_SafetyCabinet",
        "interiorObjects/Bg/PPE_B_MetalLocker",
        "interiorObjects/Bg/metal_locker (1)",
        "interiorObjects/Bg/PPE_B_Bench",
        "interiorObjects/Bg/PPE_B_StorageWallRack",
        "interiorObjects/Bg/PPE_B_WallHanger/PPE_B_WoodenCrate_02",
        "interiorObjects/Bg/PPE_B_FireExtinguisher_01",
        "interiorObjects/Bg/PPE_B_FireExtinguisher_02",
        "interiorObjects/Bg/PPE_B_MetalShelving",
    };

    [MenuItem("Tools/PPE/Configure Room Shadows")]
    public static void Configure()
    {
        Scene scene = RequireTargetScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before configuring room shadows.");
        if (scene.isDirty)
        {
            throw new InvalidOperationException(
                "The active PPE scene has unsaved changes. Review or save them before configuring room shadows.");
        }

        Transform room = RequireTransformAtPath(scene, RoomPath);
        Transform xrOrigin = RequireTransformAtPath(scene, XrOriginPath);
        Transform head = RequireTransformAtPath(scene, HeadPath);
        Transform leftController = RequireTransformAtPath(scene, LeftControllerPath);
        Transform rightController = RequireTransformAtPath(scene, RightControllerPath);
        Renderer floorRenderer = RequireTransformAtPath(scene, FloorPath).GetComponent<Renderer>();
        if (floorRenderer == null)
            throw new InvalidOperationException($"'{FloorPath}' requires an authored Renderer.");

        Material shadowCasterMaterial = EnsureShadowMaterials();
        ConfigureInteriorShell(room);
        ConfigureMainLight(room);
        ConfigureFurnitureShadowProxies(scene, shadowCasterMaterial);
        ConfigurePlayerShadowProxy(
            xrOrigin,
            head,
            leftController,
            rightController,
            floorRenderer);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        PPERoomShadowValidationHarness.Validate();
    }

    private static void ConfigureInteriorShell(Transform room)
    {
        Material floorReceiver =
            AssetDatabase.LoadAssetAtPath<Material>(FloorReceiverMaterialPath);
        Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/PPE/PPE_Room_Wall.mat");
        Material ceilingMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/PPE/PPE_Room_Ceiling.mat");

        foreach (Transform child in room)
        {
            if (!IsInteriorShellSurface(child.name))
                continue;

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer == null)
                throw new InvalidOperationException($"Room shell surface '{child.name}' requires a Renderer.");

            Undo.RecordObject(renderer, "Configure PPE room interior shadow receiver");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.sharedMaterial = GetReceiverMaterial(
                child.name,
                floorReceiver,
                wallMaterial,
                ceilingMaterial);
            EditorUtility.SetDirty(renderer);
        }
    }

    private static Material GetReceiverMaterial(
        string surfaceName,
        Material floorReceiver,
        Material wallMaterial,
        Material ceilingMaterial)
    {
        if (surfaceName == "Floor" || surfaceName.StartsWith("Floor (", StringComparison.Ordinal))
            return floorReceiver;
        if (surfaceName == "Ceiling" || surfaceName.StartsWith("Ceiling (", StringComparison.Ordinal))
            return ceilingMaterial;
        return wallMaterial;
    }

    private static Material EnsureShadowMaterials()
    {
        Shader receiverShader = Shader.Find(ReceiverShaderName);
        if (receiverShader == null)
        {
            throw new InvalidOperationException(
                $"Required shadow receiver shader was not found: '{ReceiverShaderName}'.");
        }

        EnsureAssetFolder("Assets/Materials/PPE", "Shadows");
        EnsureReceiverMaterial(
            FloorReceiverMaterialPath,
            "Assets/Materials/PPE/PPE_Room_Floor.mat",
            receiverShader);

        Material shadowCaster =
            AssetDatabase.LoadAssetAtPath<Material>(ShadowCasterMaterialPath);
        if (shadowCaster != null)
            return shadowCaster;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
            throw new InvalidOperationException("Universal Render Pipeline/Lit shader was not found.");

        shadowCaster = new Material(litShader)
        {
            name = "PPE_ShadowCaster",
        };
        if (shadowCaster.HasProperty("_BaseColor"))
            shadowCaster.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(shadowCaster, ShadowCasterMaterialPath);
        AssetDatabase.SaveAssets();
        return shadowCaster;
    }

    private static void EnsureReceiverMaterial(
        string materialPath,
        string sourceMaterialPath,
        Shader receiverShader)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
            return;

        Material source = AssetDatabase.LoadAssetAtPath<Material>(sourceMaterialPath);
        if (source == null)
            throw new InvalidOperationException($"Required room material is missing: '{sourceMaterialPath}'.");

        Material receiver = new(receiverShader)
        {
            name = source.name + "_ShadowReceiver",
        };
        receiver.CopyPropertiesFromMaterial(source);
        receiver.shader = receiverShader;
        receiver.SetColor("_ShadowTint", new Color(0.42f, 0.44f, 0.48f, 1f));
        receiver.SetFloat("_ShadowStrength", 0.6f);
        AssetDatabase.CreateAsset(receiver, materialPath);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureAssetFolder(string parentPath, string folderName)
    {
        string folderPath = parentPath + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static void ConfigureFurnitureShadowProxies(
        Scene scene,
        Material shadowCasterMaterial)
    {
        Transform existingRoot = FindTransformAtPath(scene, FurnitureProxyRootName);
        if (existingRoot != null)
            return;

        GameObject proxyRoot = new(FurnitureProxyRootName);
        Undo.RegisterCreatedObjectUndo(proxyRoot, "Create PPE furniture shadow proxies");
        SceneManager.MoveGameObjectToScene(proxyRoot, scene);
        proxyRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        proxyRoot.transform.localScale = Vector3.one;

        foreach (string targetPath in FurnitureShadowTargetPaths)
        {
            Transform target = RequireTransformAtPath(scene, targetPath);
            if (!TryGetActiveRendererBounds(target, out Bounds bounds))
            {
                throw new InvalidOperationException(
                    $"Furniture shadow target has no active Renderer bounds: '{targetPath}'.");
            }

            GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proxy.name = target.name + " Shadow Proxy";
            Undo.RegisterCreatedObjectUndo(proxy, $"Create {proxy.name}");
            proxy.transform.SetParent(proxyRoot.transform, false);
            proxy.transform.position = bounds.center;
            proxy.transform.rotation = Quaternion.identity;
            proxy.transform.localScale = new Vector3(
                Mathf.Max(0.08f, bounds.size.x * 0.9f),
                bounds.size.y,
                Mathf.Max(0.08f, bounds.size.z * 0.9f));

            Collider collider = proxy.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);

            Renderer renderer = proxy.GetComponent<Renderer>();
            renderer.sharedMaterial = shadowCasterMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    internal static bool TryGetActiveRendererBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void ConfigureMainLight(Transform room)
    {
        Transform existing = room.Find(LightName);
        if (existing != null)
        {
            if (existing.GetComponent<Light>() == null)
                throw new InvalidOperationException($"Existing '{LightName}' object requires a Light component.");

            return;
        }

        GameObject lightObject = new(LightName);
        Undo.RegisterCreatedObjectUndo(lightObject, "Create PPE main shadow light");
        lightObject.transform.SetParent(room, false);
        lightObject.transform.localPosition = Vector3.zero;
        lightObject.transform.rotation = Quaternion.Euler(72f, -30f, 0f);

        Light light = Undo.AddComponent<Light>(lightObject);
        light.type = LightType.Directional;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.color = new Color(1f, 0.95f, 0.87f, 1f);
        light.intensity = 0.65f;
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 0.72f;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.35f;
        light.cullingMask = ~0;
        light.renderMode = LightRenderMode.Auto;
    }

    private static void ConfigurePlayerShadowProxy(
        Transform xrOrigin,
        Transform head,
        Transform leftController,
        Transform rightController,
        Renderer floorRenderer)
    {
        Transform existing = xrOrigin.Find(ProxyName);
        if (existing != null)
        {
            if (existing.GetComponent<XRPlayerShadowProxy>() == null)
            {
                throw new InvalidOperationException(
                    $"Existing '{ProxyName}' object requires an {nameof(XRPlayerShadowProxy)} component.");
            }

            return;
        }

        GameObject proxyObject = new(ProxyName);
        Undo.RegisterCreatedObjectUndo(proxyObject, "Create XR player shadow proxy");
        proxyObject.transform.SetParent(xrOrigin, false);
        proxyObject.transform.localPosition = Vector3.zero;
        proxyObject.transform.localRotation = Quaternion.identity;
        proxyObject.transform.localScale = Vector3.one;

        XRPlayerShadowProxy proxy = Undo.AddComponent<XRPlayerShadowProxy>(proxyObject);
        Transform[] parts = new Transform[ShadowPartNames.Length];
        for (int index = 0; index < ShadowPartNames.Length; index++)
        {
            PrimitiveType primitiveType = index == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = ShadowPartNames[index];
            Undo.RegisterCreatedObjectUndo(part, $"Create {part.name}");
            part.transform.SetParent(proxyObject.transform, false);

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);

            Renderer renderer = part.GetComponent<Renderer>();
            renderer.enabled = false;
            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            parts[index] = part.transform;
        }

        Undo.RecordObject(proxy, "Configure XR player shadow proxy");
        proxy.ConfigureForEditor(
            head,
            leftController,
            rightController,
            floorRenderer,
            parts[0],
            parts[1],
            parts[2],
            parts[3],
            parts[4],
            parts[5]);
        EditorUtility.SetDirty(proxy);
    }

    internal static bool IsInteriorShellSurface(string objectName)
    {
        return objectName == "Front Wall" ||
               objectName == "Left Wall" ||
               objectName == "Right Wall" ||
               objectName == "Rear Wall" ||
               objectName.StartsWith("Rear Wall (", StringComparison.Ordinal) ||
               objectName == "Floor" ||
               objectName.StartsWith("Floor (", StringComparison.Ordinal) ||
               objectName == "Ceiling" ||
               objectName.StartsWith("Ceiling (", StringComparison.Ordinal);
    }

    internal static Transform FindTransformAtPath(Scene scene, string path)
    {
        string[] segments = path.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != segments[0])
                continue;

            Transform current = root.transform;
            for (int index = 1; index < segments.Length && current != null; index++)
                current = current.Find(segments[index]);
            return current;
        }

        return null;
    }

    private static Transform RequireTransformAtPath(Scene scene, string path)
    {
        Transform found = FindTransformAtPath(scene, path);
        if (found == null)
            throw new InvalidOperationException($"Required PPE scene path is missing: '{path}'.");
        return found;
    }

    private static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open the target scene before configuring room shadows: '{ScenePath}'. " +
                $"Current scene: '{scene.path}'.");
        }

        return scene;
    }
}

public static class PPERoomShadowValidationHarness
{
    [MenuItem("Tools/PPE/Validate Room Shadows")]
    public static void ValidateFromMenu()
    {
        try
        {
            Validate();
            EditorUtility.DisplayDialog(
                "PPE 룸 그림자 검증 PASS",
                "단일 주광원, 실내 표면, 가구 그림자 및 사용자 Shadows Only 프록시 구성을 확인했습니다.",
                "확인");
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("PPE 룸 그림자 검증 FAIL", exception.Message, "확인");
            throw;
        }
    }

    public static void Validate()
    {
        Scene previewScene = default;
        List<string> failures = new();
        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(PPERoomShadowSetup.ScenePath);
            ValidateMainLight(previewScene, failures);
            ValidateInteriorShell(previewScene, failures);
            ValidateFurniture(previewScene, failures);
            ValidatePlayerProxy(previewScene, failures);
            ValidateMobilePipeline(failures);
            ValidateReceiverShader(failures);
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }

        if (failures.Count > 0)
        {
            string message = "PPE room shadow validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Room Shadow Validation] PASS '{PPERoomShadowSetup.ScenePath}': " +
            "one realtime hard-shadow directional light, receiving interior shell, furniture casters, " +
            "and a collider-free Shadows Only tracked-player proxy are valid for the Mobile URP main-light path.");
    }

    private static void ValidateMainLight(Scene scene, List<string> failures)
    {
        Transform room = PPERoomShadowSetup.FindTransformAtPath(scene, "PPE Room/Room");
        if (room == null)
        {
            failures.Add("PPE Room/Room is missing.");
            return;
        }

        Transform lightTransform = room.Find(PPERoomShadowSetup.LightName);
        Light light = lightTransform != null ? lightTransform.GetComponent<Light>() : null;
        if (light == null)
        {
            failures.Add($"{PPERoomShadowSetup.LightName} is missing its authored Light.");
            return;
        }

        if (!light.gameObject.activeInHierarchy || !light.enabled)
            failures.Add($"{PPERoomShadowSetup.LightName} must be active and enabled.");
        if (light.type != LightType.Directional)
            failures.Add($"{PPERoomShadowSetup.LightName} must be Directional.");
        if (light.lightmapBakeType != LightmapBakeType.Realtime)
            failures.Add($"{PPERoomShadowSetup.LightName} must be Realtime.");
        if (light.shadows != LightShadows.Hard)
            failures.Add($"{PPERoomShadowSetup.LightName} must use hard shadows for the Mobile URP path.");
        if (light.intensity <= 0f || light.shadowStrength <= 0f)
            failures.Add($"{PPERoomShadowSetup.LightName} requires positive intensity and shadow strength.");

        int activeRealtimeShadowLights = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light sceneLight in root.GetComponentsInChildren<Light>(true))
            {
                if (sceneLight.gameObject.activeInHierarchy &&
                    sceneLight.enabled &&
                    sceneLight.lightmapBakeType == LightmapBakeType.Realtime &&
                    sceneLight.shadows != LightShadows.None)
                {
                    activeRealtimeShadowLights++;
                }
            }
        }

        if (activeRealtimeShadowLights != 1)
        {
            failures.Add(
                $"PPE Room must have exactly one active realtime shadow light; found {activeRealtimeShadowLights}.");
        }
    }

    private static void ValidateInteriorShell(Scene scene, List<string> failures)
    {
        Transform room = PPERoomShadowSetup.FindTransformAtPath(scene, "PPE Room/Room");
        if (room == null)
            return;

        int checkedSurfaceCount = 0;
        foreach (Transform child in room)
        {
            if (!PPERoomShadowSetup.IsInteriorShellSurface(child.name))
                continue;

            checkedSurfaceCount++;
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                failures.Add($"Room shell surface '{child.name}' requires a Renderer.");
                continue;
            }

            if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                failures.Add($"Interior shell '{child.name}' must not block the room's main directional light.");
            if (!renderer.receiveShadows)
                failures.Add($"Interior shell '{child.name}' must receive furniture and player shadows.");
            bool isFloor = child.name == "Floor" ||
                           child.name.StartsWith("Floor (", StringComparison.Ordinal);
            bool usesReceiver = renderer.sharedMaterial != null &&
                                renderer.sharedMaterial.shader != null &&
                                renderer.sharedMaterial.shader.name == PPERoomShadowSetup.ReceiverShaderName;
            if (isFloor && !usesReceiver)
            {
                failures.Add(
                    $"Interior floor '{child.name}' must use the PPE Unlit shadow-receiver material.");
            }
            else if (!isFloor && usesReceiver)
            {
                failures.Add(
                    $"Interior shell '{child.name}' must keep its authored Unlit material; only floors receive shadows.");
            }
        }

        if (checkedSurfaceCount < 10)
            failures.Add($"Expected at least 10 authored room shell surfaces; found {checkedSurfaceCount}.");
    }

    private static void ValidateFurniture(Scene scene, List<string> failures)
    {
        Transform proxyRoot = PPERoomShadowSetup.FindTransformAtPath(
            scene,
            PPERoomShadowSetup.FurnitureProxyRootName);
        if (proxyRoot == null)
        {
            failures.Add($"{PPERoomShadowSetup.FurnitureProxyRootName} is missing.");
            return;
        }

        int proxyCount = 0;
        foreach (Renderer renderer in proxyRoot.GetComponentsInChildren<Renderer>(true))
        {
            proxyCount++;
            if (renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                failures.Add($"Furniture shadow proxy '{renderer.name}' must use Shadows Only rendering.");
            if (renderer.receiveShadows)
                failures.Add($"Furniture shadow proxy '{renderer.name}' must not receive shadows.");
            if (renderer.GetComponent<Collider>() != null)
                failures.Add($"Furniture shadow proxy '{renderer.name}' must not have a Collider.");
            if (renderer.sharedMaterial == null ||
                renderer.sharedMaterial.FindPass("ShadowCaster") < 0)
            {
                failures.Add($"Furniture shadow proxy '{renderer.name}' requires a ShadowCaster material.");
            }
        }

        if (proxyCount != PPERoomShadowSetup.FurnitureShadowTargetPaths.Length)
        {
            failures.Add(
                $"Expected {PPERoomShadowSetup.FurnitureShadowTargetPaths.Length} furniture shadow proxies; " +
                $"found {proxyCount}.");
        }

        foreach (string targetPath in PPERoomShadowSetup.FurnitureShadowTargetPaths)
        {
            Transform target = PPERoomShadowSetup.FindTransformAtPath(scene, targetPath);
            if (target == null ||
                !PPERoomShadowSetup.TryGetActiveRendererBounds(target, out Bounds targetBounds))
            {
                failures.Add($"Furniture shadow target is missing active Renderer bounds: '{targetPath}'.");
                continue;
            }

            Transform proxy = proxyRoot.Find(target.name + " Shadow Proxy");
            if (proxy == null)
            {
                failures.Add($"Furniture shadow proxy is missing for '{targetPath}'.");
                continue;
            }

            Vector3 expectedScale = new(
                Mathf.Max(0.08f, targetBounds.size.x * 0.9f),
                targetBounds.size.y,
                Mathf.Max(0.08f, targetBounds.size.z * 0.9f));
            if (Vector3.Distance(proxy.position, targetBounds.center) > 0.001f ||
                Vector3.Distance(proxy.lossyScale, expectedScale) > 0.001f ||
                Quaternion.Angle(proxy.rotation, Quaternion.identity) > 0.01f)
            {
                failures.Add($"Furniture shadow proxy is not aligned to authored bounds: '{targetPath}'.");
            }
        }
    }

    private static void ValidatePlayerProxy(Scene scene, List<string> failures)
    {
        Transform proxyTransform = PPERoomShadowSetup.FindTransformAtPath(
            scene,
            $"XR Origin (VR)/{PPERoomShadowSetup.ProxyName}");
        XRPlayerShadowProxy proxy =
            proxyTransform != null ? proxyTransform.GetComponent<XRPlayerShadowProxy>() : null;
        if (proxy == null)
        {
            failures.Add($"XR Origin (VR)/{PPERoomShadowSetup.ProxyName} is missing.");
            return;
        }

        if (proxy.Head == null ||
            proxy.LeftController == null ||
            proxy.RightController == null ||
            proxy.FloorRenderer == null)
        {
            failures.Add("XR player shadow proxy requires authored tracked-source and floor references.");
        }

        Transform[] parts = proxy.ShadowParts;
        if (parts.Length != 6)
            failures.Add($"XR player shadow proxy requires six authored parts; found {parts.Length}.");

        foreach (Transform part in parts)
        {
            if (part == null)
            {
                failures.Add("XR player shadow proxy has a missing part reference.");
                continue;
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null || renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                failures.Add($"Player shadow part '{part.name}' must use Shadows Only rendering.");
            if (renderer != null && renderer.enabled)
                failures.Add($"Player shadow part '{part.name}' must stay disabled outside Play Mode.");
            if (renderer != null &&
                (renderer.sharedMaterial == null ||
                 renderer.sharedMaterial.FindPass("ShadowCaster") < 0))
            {
                failures.Add($"Player shadow part '{part.name}' requires a ShadowCaster material.");
            }
            if (part.GetComponent<Collider>() != null)
                failures.Add($"Player shadow part '{part.name}' must not have a Collider.");
        }
    }

    private static void ValidateMobilePipeline(List<string> failures)
    {
        UniversalRenderPipelineAsset mobilePipeline =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/Mobile_RPAsset.asset");
        if (mobilePipeline == null)
        {
            failures.Add("Mobile_RPAsset is missing.");
            return;
        }

        if (!mobilePipeline.supportsMainLightShadows)
            failures.Add("Mobile_RPAsset must support main-light shadows.");
        if (mobilePipeline.supportsAdditionalLightShadows)
            failures.Add("Mobile_RPAsset additional-light shadows must remain disabled for Quest.");
        if (mobilePipeline.supportsSoftShadows)
            failures.Add("Mobile_RPAsset soft shadows must remain disabled for Quest.");
        if (mobilePipeline.mainLightShadowmapResolution < 1024)
            failures.Add("Mobile_RPAsset main-light shadow resolution must be at least 1024.");
    }

    private static void ValidateReceiverShader(List<string> failures)
    {
        Shader shader = Shader.Find(PPERoomShadowSetup.ReceiverShaderName);
        if (shader == null)
        {
            failures.Add($"Shader '{PPERoomShadowSetup.ReceiverShaderName}' is missing or failed to compile.");
            return;
        }

        string shaderPath = AssetDatabase.GetAssetPath(shader);
        if (string.IsNullOrEmpty(shaderPath) || !File.Exists(shaderPath))
        {
            failures.Add("PPE shadow-receiver shader source path is missing.");
            return;
        }

        string source = File.ReadAllText(shaderPath);
        string[] requiredStereoTokens =
        {
            "UNITY_VERTEX_INPUT_INSTANCE_ID",
            "UNITY_VERTEX_OUTPUT_STEREO",
            "UNITY_SETUP_INSTANCE_ID(input)",
            "UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)",
            "UNITY_TRANSFER_INSTANCE_ID(input, output)",
            "UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)",
        };
        foreach (string token in requiredStereoTokens)
        {
            if (!source.Contains(token, StringComparison.Ordinal))
                failures.Add($"PPE shadow-receiver shader is missing XR stereo token '{token}'.");
        }
    }
}
