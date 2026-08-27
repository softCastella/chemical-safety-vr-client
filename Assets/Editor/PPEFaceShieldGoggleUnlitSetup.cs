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
/// Converts FaceShield and Goggle frame materials to URP Unlit so basecolor
/// shows in the Unlit PPE room. Glass parts keep the authored transparent material.
/// </summary>
public static class PPEFaceShieldGoggleUnlitSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";
    const string GlassMaterialPath = "Assets/Generated/PPE/MaskGlass/PPE_Mask_Glass.mat";
    const string FaceShieldFbxPath = "Assets/FBX/PPE_A_FaceShield/PPE_A_FaceShield.fbx";
    const string FaceShieldTextureFolder = "Assets/FBX/PPE_A_FaceShield";
    const string FaceShieldMaterialFolder = "Assets/Materials/PPE/Scene Unlit/FaceShield";
    const string FaceShieldHeadUnlitPath =
        FaceShieldMaterialFolder + "/PPE_A_FaceShield_part_24_Unlit.mat";
    const string FaceShieldHeadTexturePath =
        FaceShieldTextureFolder + "/PPE_A_FaceShield_part_24_basecolor.JPEG";
    const string FaceShieldRootName = "PPE_A_FaceShield";
    const string FaceShieldHeadName = "PPE_A_FaceShield_Head";
    const string FaceShieldGlassName = "PPE_A_FaceShield_Glass";
    const string GoggleFbxPath = "Assets/FBX/PPE_A_Goggle/PPE_A_Goggle.fbx";
    const string GoggleTextureFolder = "Assets/FBX/PPE_A_Goggle";
    const string GoggleMaterialFolder = "Assets/Materials/PPE/Scene Unlit/Goggle";
    const string GoggleRootName = "PPE_A_Goggle";
    const string GoggleContamRootName = "PPE_A_Goggle_Contam";
    const string GoggleGlassObjectName = "Glass_Lens";
    const string GoggleContamGlassMaterialPath =
        "Assets/Materials/PPE/Defects/PPE_A_Goggle_Glass_Contam.mat";

    static readonly int[] GoggleBodyParts = { 1, 2, 6, 8, 13, 15 };

    static readonly Regex GogglePartPattern = new Regex(
        @"^tripo_part_(\d+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [MenuItem("Tools/PPE/Convert FaceShield Goggle Materials to URP Unlit")]
    public static void Convert()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("FaceShield/Goggle Unlit conversion was cancelled.");
            return;
        }

        ConvertInternal();
        int repaired = RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);
        EditorUtility.DisplayDialog(
            "FaceShield Goggle Unlit",
            "FaceShield and Goggle frame materials converted to URP Unlit. Glass stays transparent.\n" +
            $"Scene renderers repaired: {repaired}",
            "OK");
    }

    public static void ConvertBatch()
    {
        ConvertInternal();
        RepairSceneMaterialReferences(ScenePath);
        ValidateInternal(false);
    }

    [MenuItem("Tools/PPE/Repair FaceShield Goggle Scene Materials")]
    public static void RepairScene()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("FaceShield/Goggle scene material repair was cancelled.");
            return;
        }

        int repaired = RepairSceneMaterialReferences(ScenePath);
        Debug.Log($"FaceShield/Goggle scene material repair complete: {repaired} renderer(s) updated.");
    }

    [MenuItem("Tools/PPE/Place Goggle Beside FaceShield")]
    public static void PlaceGoggleBesideFaceShield()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Goggle placement was cancelled.");
            return;
        }

        if (!File.Exists(ScenePath))
            throw new InvalidOperationException($"Scene was not found at '{ScenePath}'.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform faceShield = FindNamed(scene, FaceShieldRootName);
        if (faceShield == null)
            throw new InvalidOperationException(
                $"Scene '{ScenePath}' has no object named '{FaceShieldRootName}'.");

        Transform existing = FindNamed(scene, GoggleRootName);
        if (existing != null)
        {
            Selection.activeTransform = existing;
            Debug.Log($"'{GoggleRootName}' already exists at '{GetHierarchyPath(existing)}'. Position was not changed.");
            return;
        }

        GameObject gogglePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoggleFbxPath);
        if (gogglePrefab == null)
            throw new InvalidOperationException($"Goggle FBX was not found at '{GoggleFbxPath}'.");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gogglePrefab, faceShield.parent);
        Undo.RegisterCreatedObjectUndo(instance, "Place PPE_A_Goggle");
        instance.name = GoggleRootName;
        instance.transform.SetPositionAndRotation(
            faceShield.position + faceShield.right * 0.35f,
            faceShield.rotation);
        instance.transform.localScale = faceShield.localScale;
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = instance;
        Debug.Log(
            $"Placed '{GoggleRootName}' beside '{FaceShieldRootName}'. " +
            "This is a recovery pose copied from the face shield, not the original unsaved placement. Nudge it, then save the scene.");
    }

    [MenuItem("Tools/PPE/Validate FaceShield Goggle Unlit Materials")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Exit Play Mode before validating FaceShield/Goggle materials.");

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("FaceShield/Goggle Unlit validation was cancelled.");
            return;
        }

        ValidateInternal(true);
    }

    static void ConvertInternal()
    {
        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        Material glassMaterial = LoadRequiredMaterial(GlassMaterialPath);
        EnsureFolder(FaceShieldMaterialFolder);
        EnsureFolder(GoggleMaterialFolder);

        Texture2D faceShieldHeadTexture = LoadRequiredTexture(FaceShieldHeadTexturePath);
        Material faceShieldHead = CreateOrUpdateUnlitMaterial(
            FaceShieldHeadUnlitPath, unlitShader, faceShieldHeadTexture);
        RemapFbxMaterials(
            FaceShieldFbxPath,
            faceShieldHead,
            "Material_tripo_part_24.001",
            "Material_tripo_part_24",
            "tripo_part_24",
            "tripo_part_24_material",
            "PPE_A_FaceShield_tripo_part_24_basecolor",
            "PPE_A_FaceShield_part_24_basecolor");
        RemapFbxMaterials(
            FaceShieldFbxPath,
            glassMaterial,
            "Material_tripo_part_14.001",
            "Material_tripo_part_14",
            "tripo_part_14",
            "tripo_part_14_material",
            "PPE_A_FaceShield_tripo_part_14_basecolor",
            "PPE_A_FaceShield_part_14_basecolor");

        var goggleNames = new List<AssetImporter.SourceAssetIdentifier>();
        var goggleMaterials = new List<UnityEngine.Object>();
        foreach (int partIndex in GoggleBodyParts)
        {
            Texture2D partTexture = LoadRequiredTexture(GetGoggleTexturePath(partIndex));
            Material partMaterial = CreateOrUpdateUnlitMaterial(
                GetGoggleMaterialPath(partIndex), unlitShader, partTexture);
            foreach (string sourceName in GetGoggleSourceNames(partIndex))
            {
                goggleNames.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName));
                goggleMaterials.Add(partMaterial);
            }
        }

        goggleNames.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Glass_Lens"));
        goggleMaterials.Add(glassMaterial);
        goggleNames.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Glass_Lens_Material"));
        goggleMaterials.Add(glassMaterial);
        ApplyRemaps(GoggleFbxPath, goggleNames, goggleMaterials);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report =
            "FaceShield/Goggle Unlit conversion complete.\n" +
            $"Scene: {ScenePath}\n" +
            $"FaceShield head: {FaceShieldHeadUnlitPath}\n" +
            $"Glass: {GlassMaterialPath}\n" +
            $"Goggle body parts: {GoggleBodyParts.Length}\n" +
            $"Shader: {UnlitShaderName}";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/PPEFaceShieldGoggleUnlitSetup.txt", report);
        Debug.Log(report);
    }

    static int RepairSceneMaterialReferences(string scenePath)
    {
        if (!File.Exists(scenePath))
            throw new InvalidOperationException($"Scene was not found at '{scenePath}'.");

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int repaired = 0;

        Transform faceShield = FindNamed(scene, FaceShieldRootName);
        if (faceShield == null)
            throw new InvalidOperationException(
                $"Scene '{scenePath}' has no object named '{FaceShieldRootName}'.");

        Transform head = FindChildNamed(faceShield, FaceShieldHeadName);
        Transform glass = FindChildNamed(faceShield, FaceShieldGlassName);
        if (head == null)
            throw new InvalidOperationException($"'{FaceShieldRootName}' has no '{FaceShieldHeadName}'.");
        if (glass == null)
            throw new InvalidOperationException($"'{FaceShieldRootName}' has no '{FaceShieldGlassName}'.");

        // Head is the visor mesh (part_14 UVs). Frame atlas on it looks like noise.
        // Glass is the frame mesh (part_24) despite the name.
        repaired += AssignNamedRenderers(head, GlassMaterialPath);
        repaired += AssignNamedRenderers(glass, FaceShieldHeadUnlitPath);

        Transform goggle = FindNamed(scene, GoggleRootName);
        if (goggle != null)
        {
            foreach (Renderer renderer in goggle.GetComponentsInChildren<Renderer>(true))
            {
                if (IsGoggleGlassRenderer(renderer))
                {
                    repaired += AssignRendererMaterial(renderer, GlassMaterialPath);
                    continue;
                }

                Match match = GogglePartPattern.Match(renderer.gameObject.name);
                if (!match.Success)
                    continue;

                int partIndex = int.Parse(match.Groups[1].Value);
                if (Array.IndexOf(GoggleBodyParts, partIndex) < 0)
                    continue;

                repaired += AssignRendererMaterial(renderer, GetGoggleMaterialPath(partIndex));
            }
        }

        Transform goggleContam = FindNamed(scene, GoggleContamRootName);
        if (goggleContam != null)
        {
            foreach (Renderer renderer in goggleContam.GetComponentsInChildren<Renderer>(true))
            {
                if (IsGoggleGlassRenderer(renderer))
                    repaired += AssignRendererMaterial(renderer, GoggleContamGlassMaterialPath);
            }
        }

        if (repaired > 0)
            EditorSceneManager.SaveScene(scene);

        return repaired;
    }

    static void ValidateInternal(bool logSuccess)
    {
        var failures = new List<string>();
        ValidateUnlitMaterial(FaceShieldHeadUnlitPath, FaceShieldHeadTexturePath, failures);
        Material glass = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
        if (glass == null)
            failures.Add($"Missing glass material '{GlassMaterialPath}'.");
        else if (glass.shader == null || glass.shader.name != "Universal Render Pipeline/Lit")
            failures.Add($"'{GlassMaterialPath}' should stay URP Lit transparent glass.");

        foreach (int partIndex in GoggleBodyParts)
        {
            ValidateUnlitMaterial(
                GetGoggleMaterialPath(partIndex),
                GetGoggleTexturePath(partIndex),
                failures);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform faceShield = FindNamed(scene, FaceShieldRootName);
        if (faceShield == null)
        {
            failures.Add($"Scene '{ScenePath}' has no object named '{FaceShieldRootName}'.");
        }
        else
        {
            Transform head = FindChildNamed(faceShield, FaceShieldHeadName);
            Transform glassTransform = FindChildNamed(faceShield, FaceShieldGlassName);
            if (head == null)
                failures.Add($"'{FaceShieldRootName}' has no '{FaceShieldHeadName}'.");
            else
                ValidateNamedRenderers(head, GlassMaterialPath, failures);

            if (glassTransform == null)
                failures.Add($"'{FaceShieldRootName}' has no '{FaceShieldGlassName}'.");
            else
                ValidateNamedRenderers(glassTransform, FaceShieldHeadUnlitPath, failures);
        }

        Transform goggle = FindNamed(scene, GoggleRootName);
        if (goggle != null)
        {
            var foundParts = new HashSet<int>();
            bool foundGlass = false;
            foreach (Renderer renderer in goggle.GetComponentsInChildren<Renderer>(true))
            {
                if (IsGoggleGlassRenderer(renderer))
                {
                    foundGlass = true;
                    ValidateRendererMaterial(renderer, GlassMaterialPath, failures);
                    continue;
                }

                Match match = GogglePartPattern.Match(renderer.gameObject.name);
                if (!match.Success)
                    continue;

                int partIndex = int.Parse(match.Groups[1].Value);
                if (Array.IndexOf(GoggleBodyParts, partIndex) < 0)
                    continue;

                foundParts.Add(partIndex);
                ValidateRendererMaterial(renderer, GetGoggleMaterialPath(partIndex), failures);
            }

            if (!foundGlass)
                failures.Add($"'{GoggleRootName}' has no glass renderer named '{GoggleGlassObjectName}'.");

            foreach (int partIndex in GoggleBodyParts)
            {
                if (!foundParts.Contains(partIndex))
                    failures.Add($"'{GoggleRootName}' is missing 'tripo_part_{partIndex}'.");
            }
        }

        Material contamGlass = AssetDatabase.LoadAssetAtPath<Material>(GoggleContamGlassMaterialPath);
        if (contamGlass == null)
            failures.Add($"Missing contaminated goggle glass material '{GoggleContamGlassMaterialPath}'.");
        else if (contamGlass.shader == null || contamGlass.shader.name != "Tyche/PPE/Glass Contamination")
            failures.Add($"'{GoggleContamGlassMaterialPath}' should use 'Tyche/PPE/Glass Contamination'.");

        Transform goggleContam = FindNamed(scene, GoggleContamRootName);
        if (goggleContam == null)
        {
            failures.Add($"Scene '{ScenePath}' has no object named '{GoggleContamRootName}'.");
        }
        else
        {
            bool foundContamGlass = false;
            foreach (Renderer renderer in goggleContam.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsGoggleGlassRenderer(renderer))
                    continue;

                foundContamGlass = true;
                ValidateRendererMaterial(renderer, GoggleContamGlassMaterialPath, failures);
            }

            if (!foundContamGlass)
                failures.Add($"'{GoggleContamRootName}' has no glass renderer named '{GoggleGlassObjectName}'.");
        }

        if (failures.Count > 0)
        {
            string message = "FaceShield/Goggle Unlit validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            Debug.Log(
                $"FaceShield/Goggle Unlit validation passed for scene '{ScenePath}'.");
        }
    }

    static bool IsGoggleGlassRenderer(Renderer renderer)
    {
        return renderer != null &&
               (renderer.gameObject.name == GoggleGlassObjectName ||
                renderer.gameObject.name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static int AssignNamedRenderers(Transform root, string materialPath)
    {
        int repaired = 0;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            repaired += AssignRendererMaterial(renderer, materialPath);
        return repaired;
    }

    static void ValidateNamedRenderers(Transform root, string materialPath, List<string> failures)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            failures.Add($"'{GetHierarchyPath(root)}' has no renderer.");
            return;
        }

        foreach (Renderer renderer in renderers)
            ValidateRendererMaterial(renderer, materialPath, failures);
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
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"ModelImporter was not found for '{fbxPath}'.");

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

    static Material LoadRequiredMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new InvalidOperationException($"Material was not found at '{path}'.");
        return material;
    }

    static string GetGoggleTexturePath(int partIndex)
    {
        string shortName = $"{GoggleTextureFolder}/PPE_A_Goggle_part_{partIndex}_basecolor.JPEG";
        if (File.Exists(shortName))
            return shortName;

        throw new InvalidOperationException(
            $"Goggle basecolor for part {partIndex} was not found.");
    }

    static string GetGoggleMaterialPath(int partIndex)
    {
        return $"{GoggleMaterialFolder}/PPE_A_Goggle_part_{partIndex}_Unlit.mat";
    }

    static IEnumerable<string> GetGoggleSourceNames(int partIndex)
    {
        yield return $"tripo_part_{partIndex}";
        yield return $"tripo_part_{partIndex}_material";
        yield return $"Material_tripo_part_{partIndex}";
        yield return $"Material_tripo_part_{partIndex}.001";
        yield return $"PPE_A_Goggle_part_{partIndex}_basecolor";
        yield return $"PPE_A_Goggle_tripo_part_{partIndex}_basecolor";
    }

    static Transform FindNamed(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindChildNamed(root.transform, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static Transform FindChildNamed(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        foreach (Transform child in current)
        {
            Transform match = FindChildNamed(child, objectName);
            if (match != null)
                return match;
        }

        return null;
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
