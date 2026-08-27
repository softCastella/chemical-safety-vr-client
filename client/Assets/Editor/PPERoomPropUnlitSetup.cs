using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts SCBA, wooden plank, and metal shelving FBX materials to URP Unlit
/// and repairs the mask-scene references. Procedural rebuild is not used.
/// </summary>
public static class PPERoomPropUnlitSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string MaterialRoot = "Assets/Materials/PPE/Scene Unlit";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";
    const string ScbaFbxPath = "Assets/FBX/PPE_A_SCBA_Cylinder/PPE_A_SCBA_Cylinder.fbx";
    const string ScbaTexturePath = "Assets/FBX/PPE_A_SCBA_Cylinder/PPE_A_SCBA_Cylinder_basecolor.jpg";
    const string ScbaMaterialPath = MaterialRoot + "/SCBA/PPE_A_SCBA_Cylinder_Unlit.mat";
    const string WoodenPlankFbxPath = "Assets/FBX/PPE_B_WoodenPlank/PPE_B_WoodenPlank.fbx";
    const string WoodenPlankTexturePath = "Assets/FBX/PPE_B_WoodenPlank/PPE_B_WoodenPlank.jpg";
    const string WoodenPlankMaterialPath = MaterialRoot + "/WoodenPlank/PPE_B_WoodenPlank_Unlit.mat";
    const string MetalShelvingFbxPath = "Assets/FBX/PPE_B_MetalShelving/PPE_B_MetalShelving.fbx";
    const string MetalShelvingTextureFolder = "Assets/FBX/PPE_B_MetalShelving";
    const string MetalShelvingMaterialFolder = MaterialRoot + "/MetalShelving";
    const string MetalShelvingRootName = "PPE_B_MetalShelving";
    const string ScbaVisualName = "PPE_A_SCBA_Cylinder";
    const string WoodenPlankName = "PPE_B_WoodenPlank";
    const string PlayerIdleFbxPath = "Assets/FBX/PPE_D_Player_Idle/PPE_D_Player_Idle.fbx";
    const string PlayerIdleTexturePath = "Assets/FBX/PPE_D_Player_Idle/PPE_D_Player_Idle.jpg";
    const string PlayerIdleMaterialPath = MaterialRoot + "/PlayerIdle/PPE_D_Player_Idle_Unlit.mat";
    const string PlayerIdleVisualName = "PPE_D_Player_Idle";
    const int MetalShelvingPartCount = 30;

    static readonly Regex PartNamePattern = new Regex(
        @"^tripo_part_(\d+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [MenuItem("Tools/PPE/Convert SCBA Shelving Plank Materials to URP Unlit")]
    public static void Convert()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE room prop Unlit conversion was cancelled.");
            return;
        }

        ConvertInternal();
        int repaired = RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);
        EditorUtility.DisplayDialog(
            "PPE Prop Unlit",
            "SCBA, wooden plank, and metal shelving materials converted to URP Unlit.\n" +
            $"Scene renderers repaired: {repaired}",
            "OK");
    }

    public static void ConvertBatch()
    {
        ConvertInternal();
        RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);
    }

    [MenuItem("Tools/PPE/Repair SCBA Shelving Plank Scene Materials")]
    public static void RepairScene()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE room prop scene material repair was cancelled.");
            return;
        }

        int repaired = RepairSceneMaterialReferences(ScenePath);
        Debug.Log($"PPE room prop scene material repair complete: {repaired} renderer(s) updated.");
    }

    [MenuItem("Tools/PPE/Validate SCBA Shelving Plank Unlit Materials")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Exit Play Mode before validating PPE room prop materials.");

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE room prop Unlit validation was cancelled.");
            return;
        }

        ValidateInternal(true);
    }

    public static void ValidateBatch()
    {
        ValidateInternal(true);
    }

    static void ConvertInternal()
    {
        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        EnsureFolder(MaterialRoot + "/SCBA");
        EnsureFolder(MaterialRoot + "/WoodenPlank");
        EnsureFolder(MaterialRoot + "/PlayerIdle");
        EnsureFolder(MetalShelvingMaterialFolder);

        Texture2D scbaTexture = LoadRequiredTexture(ScbaTexturePath);
        Material scbaMaterial = CreateOrUpdateUnlitMaterial(
            ScbaMaterialPath, unlitShader, scbaTexture);
        RemapFbxMaterials(
            ScbaFbxPath,
            scbaMaterial,
            "Material",
            "No Name",
            "ppe_a_scba_cylinder_basecolor",
            "PPE_A_SCBA_Cylinder_basecolor",
            "tripo_node_a2a2c560-8eca-45fb-bfc4-954d48823762_material");

        Texture2D plankTexture = LoadRequiredTexture(WoodenPlankTexturePath);
        Material plankMaterial = CreateOrUpdateUnlitMaterial(
            WoodenPlankMaterialPath, unlitShader, plankTexture);
        RemapFbxMaterials(
            WoodenPlankFbxPath,
            plankMaterial,
            "Material",
            "No Name",
            "tripo_mat_68cb379c",
            "PPE_B_WoodenPlank",
            "tripo_image_68cb379c_0");

        Texture2D playerIdleTexture = LoadRequiredTexture(PlayerIdleTexturePath);
        Material playerIdleMaterial = CreateOrUpdateUnlitMaterial(
            PlayerIdleMaterialPath, unlitShader, playerIdleTexture);
        RemapFbxMaterials(
            PlayerIdleFbxPath,
            playerIdleMaterial,
            "Material",
            "No Name",
            "tripo_mat_14eb65d6",
            "tripo_node_14eb65d6",
            "tripo_node_14eb65d6_material",
            "PPE_D_Player_Idle",
            "base_color_texture");

        var shelvingNames = new List<AssetImporter.SourceAssetIdentifier>();
        var shelvingMaterials = new List<UnityEngine.Object>();
        for (int partIndex = 0; partIndex < MetalShelvingPartCount; partIndex++)
        {
            Texture2D partTexture = LoadRequiredTexture(GetMetalShelvingTexturePath(partIndex));
            Material partMaterial = CreateOrUpdateUnlitMaterial(
                GetMetalShelvingMaterialPath(partIndex), unlitShader, partTexture);
            foreach (string sourceName in GetMetalShelvingSourceNames(partIndex))
            {
                shelvingNames.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName));
                shelvingMaterials.Add(partMaterial);
            }
        }

        ApplyRemaps(MetalShelvingFbxPath, shelvingNames, shelvingMaterials);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report =
            "PPE room prop Unlit conversion complete.\n" +
            $"Scene: {ScenePath}\n" +
            $"SCBA: {ScbaMaterialPath}\n" +
            $"Wooden plank: {WoodenPlankMaterialPath}\n" +
            $"Player idle: {PlayerIdleMaterialPath}\n" +
            $"Metal shelving parts: {MetalShelvingPartCount}\n" +
            $"Shader: {UnlitShaderName}";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/PPERoomPropUnlitSetup.txt", report);
        Debug.Log(report);
    }

    static int RepairSceneMaterialReferences(string scenePath)
    {
        if (!File.Exists(scenePath))
            throw new InvalidOperationException($"Scene was not found at '{scenePath}'.");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int repaired = 0;

        repaired += AssignNamedRenderers(scene, ScbaVisualName, ScbaMaterialPath);
        repaired += AssignNamedRenderers(scene, WoodenPlankName, WoodenPlankMaterialPath);
        repaired += AssignNamedRenderers(scene, PlayerIdleVisualName, PlayerIdleMaterialPath);

        Transform shelvingRoot = FindNamed(scene, MetalShelvingRootName);
        if (shelvingRoot == null)
            throw new InvalidOperationException(
                $"Scene '{scenePath}' has no object named '{MetalShelvingRootName}'.");

        foreach (Renderer renderer in shelvingRoot.GetComponentsInChildren<Renderer>(true))
        {
            Match match = PartNamePattern.Match(renderer.gameObject.name);
            if (!match.Success)
                continue;

            int partIndex = int.Parse(match.Groups[1].Value);
            repaired += AssignRendererMaterial(renderer, GetMetalShelvingMaterialPath(partIndex));
        }

        if (repaired > 0)
            EditorSceneManager.SaveScene(scene);

        return repaired;
    }

    static void ValidateInternal(bool logSuccess)
    {
        var failures = new List<string>();
        ValidateUnlitMaterial(ScbaMaterialPath, ScbaTexturePath, failures);
        ValidateUnlitMaterial(WoodenPlankMaterialPath, WoodenPlankTexturePath, failures);
        ValidateUnlitMaterial(PlayerIdleMaterialPath, PlayerIdleTexturePath, failures);
        for (int partIndex = 0; partIndex < MetalShelvingPartCount; partIndex++)
        {
            ValidateUnlitMaterial(
                GetMetalShelvingMaterialPath(partIndex),
                GetMetalShelvingTexturePath(partIndex),
                failures);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateNamedRenderers(scene, ScbaVisualName, ScbaMaterialPath, failures);
        ValidateNamedRenderers(scene, WoodenPlankName, WoodenPlankMaterialPath, failures);
        if (CollectNamed(scene, PlayerIdleVisualName).Count > 0)
            ValidateNamedRenderers(scene, PlayerIdleVisualName, PlayerIdleMaterialPath, failures);

        Transform shelvingRoot = FindNamed(scene, MetalShelvingRootName);
        if (shelvingRoot == null)
        {
            failures.Add($"Scene '{ScenePath}' has no object named '{MetalShelvingRootName}'.");
        }
        else
        {
            var foundParts = new HashSet<int>();
            foreach (Renderer renderer in shelvingRoot.GetComponentsInChildren<Renderer>(true))
            {
                Match match = PartNamePattern.Match(renderer.gameObject.name);
                if (!match.Success)
                    continue;

                int partIndex = int.Parse(match.Groups[1].Value);
                foundParts.Add(partIndex);
                ValidateRendererMaterial(
                    renderer,
                    GetMetalShelvingMaterialPath(partIndex),
                    failures);
            }

            for (int partIndex = 0; partIndex < MetalShelvingPartCount; partIndex++)
            {
                if (!foundParts.Contains(partIndex))
                    failures.Add($"'{MetalShelvingRootName}' is missing 'tripo_part_{partIndex}'.");
            }
        }

        if (failures.Count > 0)
        {
            string message = "PPE room prop Unlit validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            Debug.Log(
                $"PPE room prop Unlit validation passed for scene '{ScenePath}'.");
        }
    }

    static int AssignNamedRenderers(Scene scene, string objectName, string materialPath)
    {
        int repaired = 0;
        foreach (Transform root in CollectNamed(scene, objectName))
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                repaired += AssignRendererMaterial(renderer, materialPath);
        }

        return repaired;
    }

    static void ValidateNamedRenderers(
        Scene scene,
        string objectName,
        string materialPath,
        List<string> failures)
    {
        List<Transform> matches = CollectNamed(scene, objectName);
        if (matches.Count == 0)
        {
            failures.Add($"Scene '{ScenePath}' has no object named '{objectName}'.");
            return;
        }

        foreach (Transform root in matches)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                failures.Add($"'{GetHierarchyPath(root)}' has no renderer.");
                continue;
            }

            foreach (Renderer renderer in renderers)
                ValidateRendererMaterial(renderer, materialPath, failures);
        }
    }

    static int AssignRendererMaterial(Renderer renderer, string materialPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
            throw new InvalidOperationException($"Missing material '{materialPath}'.");

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            materials = new[] { material };

        bool changed = false;
        for (int index = 0; index < materials.Length; index++)
        {
            if (materials[index] != material)
            {
                materials[index] = material;
                changed = true;
            }
        }

        if (!changed)
            return 0;

        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);
        return 1;
    }

    static void ValidateRendererMaterial(
        Renderer renderer,
        string expectedPath,
        List<string> failures)
    {
        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            failures.Add($"'{GetHierarchyPath(renderer.transform)}' has no material.");
            return;
        }

        foreach (Material material in materials)
        {
            if (material == null || AssetDatabase.GetAssetPath(material) != expectedPath)
            {
                failures.Add(
                    $"'{GetHierarchyPath(renderer.transform)}' does not reference '{expectedPath}'.");
                break;
            }
        }
    }

    static void ValidateUnlitMaterial(string materialPath, string texturePath, List<string> failures)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            failures.Add($"Missing material '{materialPath}'.");
            return;
        }

        if (material.shader == null || material.shader.name != UnlitShaderName)
            failures.Add($"'{materialPath}' does not use '{UnlitShaderName}'.");

        Texture2D expected = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Texture actual = material.HasProperty("_BaseMap")
            ? material.GetTexture("_BaseMap")
            : material.mainTexture;
        if (expected == null || actual != expected)
            failures.Add($"'{materialPath}' does not reference '{texturePath}'.");
    }

    static void RemapFbxMaterials(string fbxPath, Material material, params string[] sourceNames)
    {
        var identifiers = new List<AssetImporter.SourceAssetIdentifier>();
        var materials = new List<UnityEngine.Object>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sourceName in sourceNames)
        {
            if (string.IsNullOrEmpty(sourceName) || !seen.Add(sourceName))
                continue;
            identifiers.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName));
            materials.Add(material);
        }

        ApplyRemaps(fbxPath, identifiers, materials);
    }

    static void ApplyRemaps(
        string fbxPath,
        List<AssetImporter.SourceAssetIdentifier> identifiers,
        List<UnityEngine.Object> materials)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"ModelImporter was not found for '{fbxPath}'.");

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        for (int index = 0; index < identifiers.Count; index++)
            importer.AddRemap(identifiers[index], materials[index]);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static Material CreateOrUpdateUnlitMaterial(
        string path,
        Shader unlitShader,
        Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(unlitShader)
            {
                name = Path.GetFileNameWithoutExtension(path),
                enableInstancing = false
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = unlitShader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 2f);

        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Texture2D LoadRequiredTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException($"Texture was not found at '{path}'.");
        return texture;
    }

    static string GetMetalShelvingTexturePath(int partIndex)
    {
        string tripoName =
            $"{MetalShelvingTextureFolder}/PPE_B_MetalShelving_tripo_part_{partIndex}_basecolor.JPEG";
        if (File.Exists(tripoName))
            return tripoName;

        string shortName =
            $"{MetalShelvingTextureFolder}/PPE_B_MetalShelving_part_{partIndex}_basecolor.JPEG";
        if (File.Exists(shortName))
            return shortName;

        throw new InvalidOperationException(
            $"Metal shelving basecolor for part {partIndex} was not found.");
    }

    static string GetMetalShelvingMaterialPath(int partIndex)
    {
        return $"{MetalShelvingMaterialFolder}/PPE_B_MetalShelving_part_{partIndex}_Unlit.mat";
    }

    static IEnumerable<string> GetMetalShelvingSourceNames(int partIndex)
    {
        yield return $"tripo_part_{partIndex}";
        yield return $"tripo_part_{partIndex}_material";
        yield return $"Material_tripo_part_{partIndex}";
        yield return $"PPE_B_MetalShelving_tripo_part_{partIndex}_basecolor";
        yield return $"PPE_B_MetalShelving_part_{partIndex}_basecolor";
    }

    static List<Transform> CollectNamed(Scene scene, string objectName)
    {
        var matches = new List<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectNamed(root.transform, objectName, matches);
        return matches;
    }

    static void CollectNamed(Transform current, string objectName, List<Transform> matches)
    {
        if (current.name == objectName)
            matches.Add(current);

        foreach (Transform child in current)
            CollectNamed(child, objectName, matches);
    }

    static Transform FindNamed(Scene scene, string objectName)
    {
        List<Transform> matches = CollectNamed(scene, objectName);
        return matches.Count > 0 ? matches[0] : null;
    }

    static string GetHierarchyPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}
