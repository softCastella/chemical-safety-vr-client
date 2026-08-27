using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MixerRoomAtmosphereBuilder
{
    const string ScenePath = "Assets/Scenes/5_MixerRoom.unity";
    const string RootName = "MixerRoom_Backdrop_Fog_Stage";
    const string MaterialFolder = "Assets/Materials/MixerRoom";
    const string SourceImagePath = "Img/혼합기동 화면.png";
    const string BackgroundTexturePath = MaterialFolder + "/MixerRoom_Background.png";

    [MenuItem("Tools/Environment/Build Mixer Room Backdrop Fog Stage")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject mixer = GameObject.Find("mixer_room_A");
        if (mixer == null)
        {
            Debug.LogError("mixer_room_A was not found in 5_MixerRoom.");
            return;
        }

        Camera camera = Camera.main;
        Vector3 mixerCenter = GetRendererBounds(mixer).center;
        Vector3 cameraPosition = camera != null ? camera.transform.position : new Vector3(0f, 1f, -10f);
        Vector3 backdropDirection = Vector3.ProjectOnPlane(mixerCenter - cameraPosition, Vector3.up).normalized;
        if (backdropDirection.sqrMagnitude < 0.001f)
            backdropDirection = Vector3.forward;

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        Texture2D backgroundTexture = ImportBackgroundTexture();

        Material boxMaterial = CreateOrUpdateSolidMaterial(
            MaterialFolder + "/MixerRoom_BackdropBox.mat",
            new Color(0.055f, 0.07f, 0.08f, 1f));
        Material imageMaterial = CreateOrUpdateImageMaterial(
            MaterialFolder + "/MixerRoom_BackgroundImage.mat",
            backgroundTexture);
        Material fogMaterial = CreateOrUpdateFogMaterial(
            MaterialFolder + "/MixerRoom_LocalFog.mat");

        GameObject root = GetOrCreateRoot();
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        float width = 14f;
        float height = 7.8f;
        Vector3 center = mixerCenter + backdropDirection * 6.4f;
        center.y = 1.2f;
        Quaternion facingCamera = Quaternion.LookRotation(-backdropDirection, Vector3.up);

        GameObject box = GetOrCreatePrimitive(root.transform, "Backdrop_Thin_Box", PrimitiveType.Cube);
        box.transform.SetPositionAndRotation(center, facingCamera);
        box.transform.localScale = new Vector3(width, height, 0.16f);
        ConfigureRenderer(box, boxMaterial, true);

        GameObject imagePlane = GetOrCreatePrimitive(root.transform, "Background_Image_Surface", PrimitiveType.Quad);
        imagePlane.transform.SetPositionAndRotation(center - backdropDirection * 0.095f, facingCamera);
        imagePlane.transform.localScale = new Vector3(width * 0.96f, height * 0.96f, 1f);
        ConfigureRenderer(imagePlane, imageMaterial, false);

        Vector3 right = Vector3.Cross(Vector3.up, backdropDirection).normalized;
        CreateFogLayer(root.transform, "Fog_Back_Wide", center - backdropDirection * 0.72f + Vector3.up * -0.15f, facingCamera,
            new Vector3(width * 0.94f, height * 0.72f, 1f), fogMaterial);
        CreateFogLayer(root.transform, "Fog_Mid_LowBand", center - backdropDirection * 1.08f + Vector3.up * -1.05f + right * 0.35f, facingCamera,
            new Vector3(width * 0.78f, height * 0.34f, 1f), fogMaterial);
        CreateFogLayer(root.transform, "Fog_Front_SoftVeil", center - backdropDirection * 1.42f + Vector3.up * 0.35f + right * -0.28f, facingCamera,
            new Vector3(width * 0.66f, height * 0.48f, 1f), fogMaterial);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Built MixerRoom backdrop and local fog stage.", root);
    }

    static GameObject GetOrCreateRoot()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            return existing;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Mixer Room Backdrop Fog Stage");
        return root;
    }

    static GameObject GetOrCreatePrimitive(Transform parent, string name, PrimitiveType primitiveType)
    {
        Transform existing = parent.Find(name);
        GameObject target = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitiveType);
        target.name = name;
        target.transform.SetParent(parent, true);
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        return target;
    }

    static void CreateFogLayer(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject layer = GetOrCreatePrimitive(parent, name, PrimitiveType.Quad);
        layer.transform.SetPositionAndRotation(position, rotation);
        layer.transform.localScale = scale;
        ConfigureRenderer(layer, material, false);
    }

    static void ConfigureRenderer(GameObject target, Material material, bool receiveShadows)
    {
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = receiveShadows;
    }

    static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.transform.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
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

        return bounds;
    }

    static Texture2D ImportBackgroundTexture()
    {
        string absoluteSource = Path.GetFullPath(SourceImagePath);
        if (File.Exists(absoluteSource) && !File.Exists(BackgroundTexturePath))
            File.Copy(absoluteSource, BackgroundTexturePath);

        AssetDatabase.ImportAsset(BackgroundTexturePath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(BackgroundTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundTexturePath);
    }

    static Material CreateOrUpdateSolidMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("3D UI Test/XR/Unlit Base Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }

    static Material CreateOrUpdateImageMaterial(string path, Texture2D texture)
    {
        Material material = CreateOrUpdateSolidMaterial(path, Color.white);
        if (texture != null && material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        return material;
    }

    static Material CreateOrUpdateFogMaterial(string path)
    {
        Shader shader = Shader.Find("3D UI Test/XR/Mixer Room Local Fog");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_FogColor", new Color(0.64f, 0.78f, 0.92f, 0.62f));
        material.SetFloat("_Opacity", 0.48f);
        material.SetFloat("_NoiseScale", 4.1f);
        material.SetFloat("_NoiseStrength", 0.58f);
        material.SetFloat("_SoftEdge", 0.2f);
        material.SetFloat("_VerticalFade", 0.42f);
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
