using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PPERoomMirrorBuilder
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string RootName = "PPE_Room_Planar_Mirror";
    const string MaterialFolder = "Assets/Materials/PPE/Mirror";
    const string RenderTexturePath = MaterialFolder + "/PPE_Room_Mirror_RT.renderTexture";
    const string MirrorMaterialPath = MaterialFolder + "/PPE_Room_Mirror_Surface.mat";
    const string FrameMaterialPath = MaterialFolder + "/PPE_Room_Mirror_Frame.mat";
    static readonly Vector3 MirrorLocalPosition = new(5.03f, 2.46f, 7.014f);
    static readonly Vector2 MirrorSize = new(1.44f, 3.3125f);
    const float FrameThickness = 0.065f;
    const float FrameDepth = 0.045f;
    const int MirrorLayer = 2;

    [MenuItem("Tools/PPE/Rebuild Planar Mirror From Scratch")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform room = GameObject.Find("PPE Background Room")?.transform;
        if (room == null)
        {
            Debug.LogError("Cannot rebuild PPE mirror because 'PPE Background Room' was not found.");
            return;
        }

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Materials/PPE");
        EnsureFolder(MaterialFolder);

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot);

        RenderTexture renderTexture = CreateOrUpdateRenderTexture();
        Material mirrorMaterial = CreateOrUpdateMirrorMaterial(renderTexture);
        Material frameMaterial = CreateOrUpdateFrameMaterial();

        GameObject root = new(RootName);
        root.layer = MirrorLayer;
        root.transform.SetParent(room, false);
        root.transform.localPosition = MirrorLocalPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject surface = CreatePrimitive(root.transform, "Mirror_Surface", PrimitiveType.Quad, MirrorLayer);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(MirrorSize.x, MirrorSize.y, 1f);
        ConfigureRenderer(surface, mirrorMaterial);

        float halfWidth = MirrorSize.x * 0.5f;
        float halfHeight = MirrorSize.y * 0.5f;
        CreateFramePart(root.transform, "Frame_Left",
            new Vector3(-halfWidth - FrameThickness * 0.5f, 0f, -0.012f),
            new Vector3(FrameThickness, MirrorSize.y + FrameThickness * 2f, FrameDepth), frameMaterial);
        CreateFramePart(root.transform, "Frame_Right",
            new Vector3(halfWidth + FrameThickness * 0.5f, 0f, -0.012f),
            new Vector3(FrameThickness, MirrorSize.y + FrameThickness * 2f, FrameDepth), frameMaterial);
        CreateFramePart(root.transform, "Frame_Top",
            new Vector3(0f, halfHeight + FrameThickness * 0.5f, -0.012f),
            new Vector3(MirrorSize.x + FrameThickness * 2f, FrameThickness, FrameDepth), frameMaterial);
        CreateFramePart(root.transform, "Frame_Bottom",
            new Vector3(0f, -halfHeight - FrameThickness * 0.5f, -0.012f),
            new Vector3(MirrorSize.x + FrameThickness * 2f, FrameThickness, FrameDepth), frameMaterial);

        GameObject cameraObject = new("Mirror_Reflection_Camera");
        cameraObject.layer = MirrorLayer;
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, 0.25f);
        cameraObject.transform.localRotation = Quaternion.identity;
        cameraObject.transform.localScale = Vector3.one;

        Camera reflectionCamera = cameraObject.AddComponent<Camera>();
        reflectionCamera.enabled = false;
        reflectionCamera.targetTexture = renderTexture;
        reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        reflectionCamera.backgroundColor = new Color(0.58f, 0.68f, 0.72f, 1f);
        reflectionCamera.nearClipPlane = 0.03f;
        reflectionCamera.farClipPlane = 30f;
        reflectionCamera.cullingMask = ~(1 << MirrorLayer);
        reflectionCamera.useOcclusionCulling = false;
        reflectionCamera.allowMSAA = false;

        SerializedObject serializedCamera = new(reflectionCamera);
        SerializedProperty targetEye = serializedCamera.FindProperty("m_TargetEye");
        if (targetEye != null)
        {
            targetEye.intValue = 0;
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();
        }

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = true;
        cameraData.allowXRRendering = false;
        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;

        PlanarMirrorRenderer mirror = surface.AddComponent<PlanarMirrorRenderer>();
        SerializedObject serializedMirror = new(mirror);
        serializedMirror.FindProperty("reflectionCamera").objectReferenceValue = reflectionCamera;
        serializedMirror.FindProperty("targetTexture").objectReferenceValue = renderTexture;
        serializedMirror.FindProperty("reflectedLayers").intValue = ~(1 << MirrorLayer);
        serializedMirror.FindProperty("surfaceClipOffset").floatValue = 0.04f;
        serializedMirror.FindProperty("reflectedDepth").floatValue = 20f;
        serializedMirror.FindProperty("clearColor").colorValue = new Color(0.58f, 0.68f, 0.72f, 1f);
        serializedMirror.FindProperty("renderInGameView").boolValue = true;
        serializedMirror.FindProperty("renderInSceneView").boolValue = true;
        serializedMirror.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt PPE planar mirror from scratch. worldPosition={root.transform.position} worldSize={surface.GetComponent<Renderer>().bounds.size}", root);
    }

    static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType primitiveType, int layer)
    {
        GameObject target = GameObject.CreatePrimitive(primitiveType);
        target.name = name;
        target.layer = layer;
        target.transform.SetParent(parent, false);
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        return target;
    }

    static void CreateFramePart(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject part = CreatePrimitive(parent, name, PrimitiveType.Cube, MirrorLayer);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        ConfigureRenderer(part, material);
    }

    static void ConfigureRenderer(GameObject target, Material material)
    {
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static RenderTexture CreateOrUpdateRenderTexture()
    {
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(640, 1472, 24, RenderTextureFormat.ARGB32)
            {
                name = "PPE_Room_Mirror_RT"
            };
            AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
        }

        renderTexture.Release();
        renderTexture.width = 640;
        renderTexture.height = 1472;
        renderTexture.depth = 24;
        renderTexture.antiAliasing = 1;
        renderTexture.useMipMap = false;
        renderTexture.autoGenerateMips = false;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;
        EditorUtility.SetDirty(renderTexture);
        return renderTexture;
    }

    static Material CreateOrUpdateMirrorMaterial(RenderTexture renderTexture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MirrorMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MirrorMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", renderTexture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", renderTexture);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);
        material.renderQueue = (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CreateOrUpdateFrameMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, FrameMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.78f, 0.82f, 0.84f, 1f));
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.15f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.72f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
