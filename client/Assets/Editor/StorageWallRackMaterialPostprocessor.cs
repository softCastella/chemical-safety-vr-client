using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public sealed class StorageWallRackMaterialPostprocessor : AssetPostprocessor
{
    const string ModelPath = "Assets/FBX/storage wall rack/PPE_B_StorageWallRack.fbx";
    const string TextureDirectory = "Assets/FBX/storage wall rack/storage+wall+rack.fbm";
    const string MaterialDirectory = "Assets/Materials/PPE/Storage Wall Rack";
    const string AutoReimportSessionKey = "StorageWallRackMaterialPostprocessor.AutoReimported";
    const string ReimportInProgressSessionKey = "StorageWallRackMaterialPostprocessor.ReimportInProgress";
    const int ExpectedPartCount = 106;

    static readonly Regex PartIndexPattern = new Regex(
        @"tripo[_+]part[_+](\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static StorageWallRackMaterialPostprocessor()
    {
        EditorApplication.delayCall += AutoReimportIfMaterialsAreMissing;
    }

    public Material OnAssignMaterialModel(Material sourceMaterial, Renderer renderer)
    {
        if (NormalizePath(assetPath) != ModelPath)
            return null;

        if (!TryGetPartIndex(sourceMaterial != null ? sourceMaterial.name : null, out int partIndex) &&
            !TryGetPartIndex(renderer != null ? renderer.name : null, out partIndex) &&
            !TryGetPartIndex(renderer != null && renderer.gameObject != null ? renderer.gameObject.name : null, out partIndex))
        {
            return null;
        }

        return GetOrCreatePartMaterial(partIndex);
    }

    [MenuItem("Tools/PPE/Reimport Storage Wall Rack Materials")]
    public static void ReimportStorageWallRack()
    {
        if (SessionState.GetBool(ReimportInProgressSessionKey, false))
            return;

        SessionState.SetBool(ReimportInProgressSessionKey, true);
        try
        {
            EnsureAllPartMaterials();
            ApplyImporterMaterialRemaps();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            SessionState.SetBool(ReimportInProgressSessionKey, false);
        }
    }

    static void AutoReimportIfMaterialsAreMissing()
    {
        if (SessionState.GetBool(ReimportInProgressSessionKey, false))
            return;

        if (SessionState.GetBool(AutoReimportSessionKey, false))
            return;

        if (!File.Exists(ModelPath) ||
            CountExistingPartMaterials() >= ExpectedPartCount ||
            CountExistingLegacyPartMaterials() >= ExpectedPartCount)
            return;

        SessionState.SetBool(AutoReimportSessionKey, true);
        ReimportStorageWallRack();
    }

    static void EnsureAllPartMaterials()
    {
        EnsureMaterialDirectory();

        string absoluteTextureDirectory = Path.GetFullPath(TextureDirectory);
        if (!Directory.Exists(absoluteTextureDirectory))
            return;

        foreach (string absoluteTexturePath in Directory.GetFiles(
            absoluteTextureDirectory,
            "storage+wall+rack_tripo_part_*_basecolor.jpg"))
        {
            string texturePath = NormalizePath(absoluteTexturePath);
            int assetsIndex = texturePath.IndexOf("Assets/", System.StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                texturePath = texturePath.Substring(assetsIndex);

            if (!TryGetPartIndex(Path.GetFileNameWithoutExtension(texturePath), out int partIndex))
                continue;

            GetOrCreatePartMaterial(partIndex);
        }
    }

    static Material GetOrCreatePartMaterial(int partIndex)
    {
        EnsureMaterialDirectory();

        string materialPath = GetMaterialPath(partIndex);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader)
            {
                name = $"PPE_B_StorageWallRack_Part_{partIndex}",
                enableInstancing = true
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(GetTexturePath(partIndex));
        if (baseColor != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", baseColor);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        EditorUtility.SetDirty(material);
        return material;
    }

    static void ApplyImporterMaterialRemaps()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        bool importerChanged = false;
        var existingRemaps = importer.GetExternalObjectMap();

        for (int partIndex = 0; partIndex < ExpectedPartCount; partIndex++)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialPath(partIndex));
            if (material == null)
                continue;

            importerChanged |= AddMaterialRemapIfNeeded(
                importer,
                existingRemaps,
                $"tripo_part_{partIndex}",
                material);
            importerChanged |= AddMaterialRemapIfNeeded(
                importer,
                existingRemaps,
                $"storage_wall_rack_tripo_part_{partIndex}_basecolor",
                material);
            importerChanged |= AddMaterialRemapIfNeeded(
                importer,
                existingRemaps,
                $"storage+wall+rack_tripo_part_{partIndex}_basecolor",
                material);
        }

        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importerChanged = true;
        }

        if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
        {
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importerChanged = true;
        }

        if (!importerChanged)
            return;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static bool AddMaterialRemapIfNeeded(
        ModelImporter importer,
        System.Collections.Generic.IDictionary<AssetImporter.SourceAssetIdentifier, Object> existingRemaps,
        string sourceMaterialName,
        Material material)
    {
        var sourceIdentifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterialName);
        if (existingRemaps.TryGetValue(sourceIdentifier, out Object existingObject) && existingObject == material)
            return false;

        importer.AddRemap(sourceIdentifier, material);
        return true;
    }

    static bool TryGetPartIndex(string value, out int partIndex)
    {
        partIndex = -1;
        if (string.IsNullOrEmpty(value))
            return false;

        Match match = PartIndexPattern.Match(value);
        return match.Success && int.TryParse(match.Groups[1].Value, out partIndex);
    }

    static void EnsureMaterialDirectory()
    {
        if (AssetDatabase.IsValidFolder(MaterialDirectory))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/PPE"))
            AssetDatabase.CreateFolder("Assets/Materials", "PPE");
        if (!AssetDatabase.IsValidFolder(MaterialDirectory))
            AssetDatabase.CreateFolder("Assets/Materials/PPE", "Storage Wall Rack");
    }

    static int CountExistingPartMaterials()
    {
        string absoluteMaterialDirectory = Path.GetFullPath(MaterialDirectory);
        if (!Directory.Exists(absoluteMaterialDirectory))
            return 0;

        return Directory.GetFiles(absoluteMaterialDirectory, "PPE_B_StorageWallRack_Part_*.mat").Length;
    }

    static int CountExistingLegacyPartMaterials()
    {
        string absoluteMaterialDirectory = Path.GetFullPath(MaterialDirectory);
        if (!Directory.Exists(absoluteMaterialDirectory))
            return 0;

        return Directory.GetFiles(absoluteMaterialDirectory, "StorageWallRack_tripo_part_*.mat").Length;
    }

    static string GetMaterialPath(int partIndex)
    {
        return $"{MaterialDirectory}/PPE_B_StorageWallRack_Part_{partIndex}.mat";
    }

    static string GetTexturePath(int partIndex)
    {
        return $"{TextureDirectory}/storage+wall+rack_tripo_part_{partIndex}_basecolor.jpg";
    }

    static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }
}
