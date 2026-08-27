using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PPEHelmetFbxUnlitSetup
{
    const string HelmetFbxPath = "Assets/FBX/helmet/PPE_A_Helmet_Strap.fbx";
    const string TextureFolder = "Assets/FBX/helmet";
    const string MaterialFolder = "Assets/Materials/PPE/Scene Unlit/Helmet";
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string HelmetRootName = "PPE_A_Helmet_Strap";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    [MenuItem("Tools/PPE/Convert Helmet FBX Materials to URP Unlit")]
    public static void Convert()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Helmet FBX conversion was cancelled.");
            return;
        }

        ConvertInternal(false);
        int repaired = RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);

        EditorUtility.DisplayDialog(
            "Helmet Unlit",
            $"Helmet FBX materials converted and scene references repaired.\n" +
            $"Scene renderers repaired: {repaired}",
            "OK");
    }

    public static void ConvertBatch()
    {
        ConvertInternal(false);
        RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);
    }

    [MenuItem("Tools/PPE/Repair Helmet Scene Material References")]
    public static void RepairScene()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Helmet scene material repair was cancelled.");
            return;
        }

        int repaired = RepairSceneMaterialReferences(ScenePath);
        Debug.Log(
            $"Helmet scene material repair complete: {repaired} renderer(s) updated.");
    }

    [MenuItem("Tools/PPE/Validate Helmet FBX Unlit Materials")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Exit Play Mode before validating Helmet FBX materials.");

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Helmet FBX material validation was cancelled.");
            return;
        }

        ValidateInternal(true);
    }

    public static void ValidateBatch()
    {
        ValidateInternal(true);
    }

    static void ConvertInternal(bool interactive)
    {
        GameObject helmetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HelmetFbxPath);
        if (helmetPrefab == null)
            throw new InvalidOperationException($"Helmet FBX was not found at '{HelmetFbxPath}'.");

        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        EnsureFolder(MaterialFolder);

        ModelImporter importer = AssetImporter.GetAtPath(HelmetFbxPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"ModelImporter was not found for '{HelmetFbxPath}'.");

        Dictionary<string, Texture2D> texturesByPart = LoadPartTextures();
        if (texturesByPart.Count == 0)
        {
            throw new InvalidOperationException(
                $"No helmet_part_*_basecolor textures were found under '{TextureFolder}'.");
        }

        Material[] sourceMaterials = CollectSourceMaterials(HelmetFbxPath);
        var remaps = new List<AssetImporter.SourceAssetIdentifier>();
        var remapMaterials = new List<UnityEngine.Object>();
        int createdOrUpdated = 0;
        var remappedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (Material source in sourceMaterials)
        {
            if (source == null)
                continue;

            int? partIndex = TryParsePartIndex(source.name);
            Texture2D texture = null;
            if (partIndex.HasValue && texturesByPart.TryGetValue(partIndex.Value.ToString(), out Texture2D mapped))
                texture = mapped;
            if (texture == null)
                texture = GetBaseTexture(source) as Texture2D;

            string partKey = partIndex.HasValue
                ? partIndex.Value.ToString()
                : MakeSafeFileName(source.name);
            string materialPath = $"{MaterialFolder}/helmet_part_{partKey}_Unlit.mat";
            Material unlit = CreateOrUpdateUnlitMaterial(materialPath, unlitShader, source, texture);
            createdOrUpdated++;

            remaps.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name));
            remapMaterials.Add(unlit);
            remappedNames.Add(source.name);
        }

        // FBX authored names are Material_tripo_part_N. Remap those even when Unity
        // has not yet extracted embedded materials as sub-assets.
        foreach (KeyValuePair<string, Texture2D> pair in texturesByPart)
        {
            string fbxMaterialName = "Material_tripo_part_" + pair.Key;
            if (remappedNames.Contains(fbxMaterialName))
                continue;

            string materialPath = $"{MaterialFolder}/helmet_part_{pair.Key}_Unlit.mat";
            Material unlit = CreateOrUpdateUnlitMaterial(materialPath, unlitShader, null, pair.Value);
            createdOrUpdated++;
            remaps.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), fbxMaterialName));
            remapMaterials.Add(unlit);
            remappedNames.Add(fbxMaterialName);
        }

        if (remaps.Count == 0)
        {
            throw new InvalidOperationException(
                $"No helmet materials could be remapped for '{HelmetFbxPath}'.");
        }

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        for (int index = 0; index < remaps.Count; index++)
            importer.AddRemap(remaps[index], remapMaterials[index]);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report =
            $"Helmet FBX Unlit conversion complete.\n" +
            $"FBX: {HelmetFbxPath}\n" +
            $"Materials converted: {createdOrUpdated}\n" +
            $"Output folder: {MaterialFolder}\n" +
            $"Shader: {UnlitShaderName}";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/PPEHelmetFbxUnlitSetup.txt", report);
        Debug.Log(report);

        if (interactive)
            EditorUtility.DisplayDialog("Helmet Unlit", report, "OK");
    }

    static int RepairSceneMaterialReferences(string scenePath)
    {
        if (!File.Exists(scenePath))
            throw new InvalidOperationException($"Helmet scene was not found at '{scenePath}'.");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Transform helmetRoot = FindRoot(scene, HelmetRootName);
        if (helmetRoot == null)
            throw new InvalidOperationException(
                $"Scene '{scenePath}' has no root object named '{HelmetRootName}'.");

        int repaired = 0;
        var failures = new List<string>();
        foreach (Renderer renderer in helmetRoot.GetComponentsInChildren<Renderer>(true))
        {
            int? partIndex = TryParsePartIndex(renderer.gameObject.name);
            if (!partIndex.HasValue)
                continue;

            string materialPath =
                $"{MaterialFolder}/helmet_part_{partIndex.Value}_Unlit.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                failures.Add(
                    $"{renderer.gameObject.name}: missing material '{materialPath}'.");
                continue;
            }

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

            if (changed)
            {
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                repaired++;
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Helmet scene material repair failed:\n- " +
                string.Join("\n- ", failures));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException(
                $"Failed to save repaired helmet scene '{scenePath}'.");

        return repaired;
    }

    static void ValidateInternal(bool logSuccess)
    {
        var failures = new List<string>();

        ModelImporter importer = AssetImporter.GetAtPath(HelmetFbxPath) as ModelImporter;
        if (importer == null)
        {
            failures.Add($"Missing ModelImporter for '{HelmetFbxPath}'.");
        }
        else
        {
            if (importer.materialLocation != ModelImporterMaterialLocation.External)
                failures.Add("Helmet FBX materialLocation is not External.");
        }

        Dictionary<string, Texture2D> texturesByPart = LoadPartTextures();
        if (texturesByPart.Count == 0)
            failures.Add($"No helmet basecolor textures found under '{TextureFolder}'.");

        foreach (KeyValuePair<string, Texture2D> pair in texturesByPart)
        {
            string materialPath =
                $"{MaterialFolder}/helmet_part_{pair.Key}_Unlit.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                failures.Add($"Missing helmet material '{materialPath}'.");
                continue;
            }

            if (material.shader == null || material.shader.name != UnlitShaderName)
                failures.Add(
                    $"'{materialPath}' does not use '{UnlitShaderName}'.");

            Texture texture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture != pair.Value)
                failures.Add(
                    $"'{materialPath}' does not reference '{AssetDatabase.GetAssetPath(pair.Value)}'.");
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform helmetRoot = FindRoot(scene, HelmetRootName);
        if (helmetRoot == null)
        {
            failures.Add(
                $"Scene '{ScenePath}' has no root object named '{HelmetRootName}'.");
        }
        else
        {
            foreach (Renderer renderer in helmetRoot.GetComponentsInChildren<Renderer>(true))
            {
                int? partIndex = TryParsePartIndex(renderer.gameObject.name);
                if (!partIndex.HasValue)
                    continue;

                string expectedPath =
                    $"{MaterialFolder}/helmet_part_{partIndex.Value}_Unlit.mat";
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    failures.Add($"'{renderer.gameObject.name}' has no material.");
                    continue;
                }

                foreach (Material material in materials)
                {
                    if (material == null || AssetDatabase.GetAssetPath(material) != expectedPath)
                    {
                        failures.Add(
                            $"'{renderer.gameObject.name}' does not reference '{expectedPath}'.");
                        break;
                    }
                }
            }
        }

        if (failures.Count > 0)
        {
            string message = "Helmet FBX Unlit validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            Debug.Log(
                $"Helmet FBX Unlit validation passed: textures={texturesByPart.Count}, " +
                $"scene='{ScenePath}'.");
        }
    }

    static Transform FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindTransform(root.transform, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static Transform FindTransform(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        foreach (Transform child in current)
        {
            Transform match = FindTransform(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static Material[] CollectSourceMaterials(string fbxPath)
    {
        var materials = new List<Material>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Material material && !materials.Contains(material))
                materials.Add(material);
        }

        string materialsFolder = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/') + "/Materials";
        if (AssetDatabase.IsValidFolder(materialsFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolder });
            foreach (string guid in guids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && !materials.Contains(material))
                    materials.Add(material);
            }
        }

        materials.Sort((left, right) =>
            string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty));
        return materials.ToArray();
    }

    static Dictionary<string, Texture2D> LoadPartTextures()
    {
        var map = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            Match match = Regex.Match(fileName, @"helmet_part_(\d+)_basecolor", RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                map[match.Groups[1].Value] = texture;
        }

        return map;
    }

    static Material CreateOrUpdateUnlitMaterial(
        string path,
        Shader unlitShader,
        Material source,
        Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(unlitShader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = unlitShader;
        }

        Color baseColor = GetBaseColor(source);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);

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

    static Texture GetBaseTexture(Material material)
    {
        string[] candidates = { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo" };
        foreach (string candidate in candidates)
        {
            if (!material.HasProperty(candidate))
                continue;

            Texture texture = material.GetTexture(candidate);
            if (texture != null)
                return texture;
        }

        return material.mainTexture;
    }

    static Color GetBaseColor(Material material)
    {
        if (material == null)
            return Color.white;

        string[] candidates = { "_BaseColor", "_Color", "_TintColor" };
        foreach (string candidate in candidates)
        {
            if (material.HasProperty(candidate))
                return material.GetColor(candidate);
        }

        return Color.white;
    }

    static int? TryParsePartIndex(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return null;

        Match match = Regex.Match(materialName, @"part[_\s-]?(\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        return int.Parse(match.Groups[1].Value);
    }

    static string MakeSafeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace(' ', '_');
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
