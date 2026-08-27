using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audits and migrates Mesh sub-asset names only for the approved PPE 34-asset baseline.
/// Asset paths, GUIDs, model hierarchy, bones, materials, and scene-authored values are not changed.
/// </summary>
public static class PPEMeshSubAssetNamingMigration
{
    private static readonly Regex LegacyPartPattern = new(
        "^tripo_part_(?<suffix>.+)$",
        RegexOptions.CultureInvariant);
    private static readonly Regex LegacyNodePattern = new(
        "^tripo_node_(?<suffix>.+)$",
        RegexOptions.CultureInvariant);
    private static readonly Regex LegacyMeshArrayPattern = new(
        "^meshes\\[(?<suffix>[0-9]+)\\]$",
        RegexOptions.CultureInvariant);

    internal readonly struct AssetSpec
    {
        public AssetSpec(string assetPath, string baselineName)
        {
            AssetPath = assetPath;
            BaselineName = baselineName;
        }

        public string AssetPath { get; }
        public string BaselineName { get; }
    }

    internal static readonly AssetSpec[] AssetSpecs =
    {
        new("Assets/FBX/ppe_room_bench/PPE_B_Bench.fbx", "PPE_B_Bench"),
        new("Assets/FBX/metal_locker/PPE_B_MetalLocker.fbx", "PPE_B_MetalLocker"),
        new("Assets/FBX/mask_locker/PPE_B_MaskLocker.fbx", "PPE_B_MaskLocker"),
        new("Assets/FBX/safety_cabinet/PPE_B_SafetyCabinet.fbx", "PPE_B_SafetyCabinet"),
        new("Assets/FBX/wooden crate/PPE_B_WoodenCrate.fbx", "PPE_B_WoodenCrate"),
        new("Assets/FBX/hanger piece/PPE_B_WallHanger.fbx", "PPE_B_WallHanger"),
        new("Assets/FBX/hand sanitizer dispenser/PPE_B_HandSanitizer.fbx", "PPE_B_HandSanitizer"),
        new("Assets/PPE_B_FireExtinguisher.fbx", "PPE_B_FireExtinguisher"),
        new("Assets/FBX/yellow trash bin/PPE_B_YellowTrashBin.fbx", "PPE_B_YellowTrashBin"),
        new("Assets/FBX/storage wall rack/PPE_B_StorageWallRack.fbx", "PPE_B_StorageWallRack"),
        new("Assets/FBX/hazmat_suit_hanger/PPE_B_SuitHanger.fbx", "PPE_B_SuitHanger"),
        new("Assets/FBX/cleaning_cart/PPE_B_CleaningCart.fbx", "PPE_B_CleaningCart"),
        new("Assets/UIs/Facilities/PPE_Room/Duct/PPE_B_Duct.fbx", "PPE_B_Duct"),
        new("Assets/TripoModels/outdoor_security_camera_3d_model/PPE_B_CCTV.fbx", "PPE_B_CCTV"),
        new("Assets/FBX/Tablet/PPE_C_Tablet.fbx", "PPE_C_Tablet"),
        new("Assets/FBX/hazmat suit/PPE_A_SuitHang.fbx", "PPE_A_SuitHang"),
        new("Assets/TripoModels/hazmat_suit_3d_model/PPE_A_SuitWear.fbx", "PPE_A_SuitWear"),
        new("Assets/TripoModels/rubber_boots_3d_model_Clone1/PPE_A_Boots_L.fbx", "PPE_A_Boots_L"),
        new("Assets/TripoModels/rubber_boots_3d_model/PPE_A_Boots_R.fbx", "PPE_A_Boots_R"),
        new("Assets/TripoModels/tactical_harness_3d_model/PPE_A_Backplate.fbx", "PPE_A_Backplate"),
        new("Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx", "PPE_A_Mask"),
        new("Assets/FBX/helmet/PPE_A_Helmet_Strap.fbx", "PPE_A_Helmet_Strap"),
        new("Assets/FBX/glove_Left/PPE_A_Glove_L.fbx", "PPE_A_Glove_L"),
        new("Assets/FBX/glove_Right/PPE_A_Glove_R.fbx", "PPE_A_Glove_R"),
        new("Assets/TripoModels/orange_tape_roll_3d_model/PPE_A_Tape.fbx", "PPE_A_Tape"),
        new("Assets/FBX/taped/PPE_A_Taped.fbx", "PPE_A_Taped"),
        new("Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_L.fbx", "PPE_A_Hand_Bare_L"),
        new("Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_R.fbx", "PPE_A_Hand_Bare_R"),
        new("Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_L.fbx", "PPE_A_Hand_Suit_L"),
        new("Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_R.fbx", "PPE_A_Hand_Suit_R"),
        new("Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_L.fbx", "PPE_A_Hand_GloveSuit_L"),
        new("Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_R.fbx", "PPE_A_Hand_GloveSuit_R"),
        new("Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_L.fbx", "PPE_A_Hand_GloveTape_L"),
        new("Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_R.fbx", "PPE_A_Hand_GloveTape_R"),
    };

    [MenuItem("Tools/PPE/Audit 34-Asset Mesh Names")]
    public static void Audit()
    {
        int existingAssetCount = 0;
        int missingAssetCount = 0;
        int meshCount = 0;

        foreach (AssetSpec spec in AssetSpecs)
        {
            if (AssetDatabase.LoadMainAssetAtPath(spec.AssetPath) == null)
            {
                missingAssetCount++;
                Debug.Log($"[PPE Mesh Audit] missing|{spec.BaselineName}|{spec.AssetPath}");
                continue;
            }

            existingAssetCount++;
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(spec.AssetPath)
                .OfType<Mesh>()
                .OrderBy(mesh => mesh.name, StringComparer.Ordinal)
                .ToArray();
            meshCount += meshes.Length;
            string names = string.Join("|", meshes.Select(mesh => mesh.name));
            Debug.Log(
                $"[PPE Mesh Audit] asset|{spec.BaselineName}|{spec.AssetPath}|" +
                $"meshes={meshes.Length}|{names}");
        }

        Debug.Log(
            $"[PPE Mesh Audit] summary|baseline={AssetSpecs.Length}|existing={existingAssetCount}|" +
            $"missing={missingAssetCount}|meshes={meshCount}");
    }

    [MenuItem("Tools/PPE/Apply 34-Asset Mesh Names")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before applying PPE Mesh names.");

        Dictionary<string, AssetSnapshot> snapshots = AssetSpecs
            .Where(spec => AssetDatabase.LoadMainAssetAtPath(spec.AssetPath) != null)
            .ToDictionary(
                spec => spec.AssetPath,
                spec => new AssetSnapshot(spec.AssetPath),
                StringComparer.Ordinal);

        int skippedMissingCount = AssetSpecs.Length - snapshots.Count;
        foreach (AssetSpec spec in AssetSpecs)
        {
            if (!snapshots.ContainsKey(spec.AssetPath))
            {
                Debug.Log($"[PPE Mesh Naming] skipped missing asset|{spec.BaselineName}|{spec.AssetPath}");
                continue;
            }

            AssetDatabase.ImportAsset(
                spec.AssetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        List<string> failures = new();
        foreach (AssetSnapshot snapshot in snapshots.Values)
            snapshot.ValidateUnchangedIdentity(failures);
        ValidateNames(failures, out int existingAssetCount, out int meshCount);
        ThrowIfFailed(failures);

        Debug.Log(
            $"[PPE Mesh Naming] applied|baseline={AssetSpecs.Length}|existing={existingAssetCount}|" +
            $"skippedMissing={skippedMissingCount}|meshes={meshCount}|identityChanges=0");
    }

    [MenuItem("Tools/PPE/Validate 34-Asset Mesh Names")]
    public static void Validate()
    {
        List<string> failures = new();
        ValidateNames(failures, out int existingAssetCount, out int meshCount);
        ThrowIfFailed(failures);

        Debug.Log(
            $"[PPE Mesh Naming] validation passed|baseline={AssetSpecs.Length}|" +
            $"existing={existingAssetCount}|missing={AssetSpecs.Length - existingAssetCount}|meshes={meshCount}");
    }

    internal static bool TryGetSpec(string assetPath, out AssetSpec spec)
    {
        foreach (AssetSpec candidate in AssetSpecs)
        {
            if (string.Equals(candidate.AssetPath, assetPath, StringComparison.Ordinal))
            {
                spec = candidate;
                return true;
            }
        }

        spec = default;
        return false;
    }

    internal static string GetExpectedMeshName(
        AssetSpec spec,
        string sourceName,
        int meshCount,
        int fallbackIndex)
    {
        if (sourceName == spec.BaselineName ||
            sourceName.StartsWith(spec.BaselineName + "_", StringComparison.Ordinal))
        {
            return sourceName;
        }

        Match partMatch = LegacyPartPattern.Match(sourceName);
        if (partMatch.Success)
            return spec.BaselineName + "_Part_" + partMatch.Groups["suffix"].Value;

        Match nodeMatch = LegacyNodePattern.Match(sourceName);
        if (nodeMatch.Success)
            return spec.BaselineName + "_Node_" + nodeMatch.Groups["suffix"].Value;

        Match meshArrayMatch = LegacyMeshArrayPattern.Match(sourceName);
        if (meshArrayMatch.Success)
            return spec.BaselineName + "_Mesh_" + meshArrayMatch.Groups["suffix"].Value;

        if (meshCount == 1)
            return spec.BaselineName;

        string suffix = string.IsNullOrWhiteSpace(sourceName)
            ? "Mesh_" + fallbackIndex
            : sourceName;
        return spec.BaselineName + "_" + suffix;
    }

    private static void ValidateNames(
        List<string> failures,
        out int existingAssetCount,
        out int meshCount)
    {
        existingAssetCount = 0;
        meshCount = 0;

        foreach (AssetSpec spec in AssetSpecs)
        {
            if (AssetDatabase.LoadMainAssetAtPath(spec.AssetPath) == null)
                continue;

            existingAssetCount++;
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(spec.AssetPath)
                .OfType<Mesh>()
                .ToArray();
            meshCount += meshes.Length;
            if (meshes.Length == 0)
            {
                failures.Add($"No Mesh sub-assets found: '{spec.AssetPath}'.");
                continue;
            }

            string[] duplicateNames = meshes
                .GroupBy(mesh => mesh.name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateNames.Length > 0)
            {
                failures.Add(
                    $"Duplicate Mesh names in '{spec.AssetPath}': " +
                    string.Join(", ", duplicateNames));
            }

            foreach (Mesh mesh in meshes)
            {
                bool matchesBaseline = mesh.name == spec.BaselineName ||
                    mesh.name.StartsWith(spec.BaselineName + "_", StringComparison.Ordinal);
                if (!matchesBaseline)
                {
                    failures.Add(
                        $"Mesh name is outside the 34-asset baseline: " +
                        $"'{spec.AssetPath}'::{mesh.name} expected prefix '{spec.BaselineName}'.");
                }
            }
        }
    }

    private static void ThrowIfFailed(IReadOnlyCollection<string> failures)
    {
        if (failures.Count == 0)
            return;

        string message = "PPE 34-asset Mesh naming validation failed:\n- " +
            string.Join("\n- ", failures);
        Debug.LogError(message);
        throw new InvalidOperationException(message);
    }

    private sealed class AssetSnapshot
    {
        private readonly string assetPath;
        private readonly string guid;
        private readonly long[] meshLocalIds;

        public AssetSnapshot(string assetPath)
        {
            this.assetPath = assetPath;
            guid = AssetDatabase.AssetPathToGUID(assetPath);
            meshLocalIds = GetMeshLocalIds(assetPath);
        }

        public void ValidateUnchangedIdentity(List<string> failures)
        {
            string currentGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.Equals(guid, currentGuid, StringComparison.Ordinal))
                failures.Add($"GUID changed for '{assetPath}': {guid} -> {currentGuid}.");

            long[] currentLocalIds = GetMeshLocalIds(assetPath);
            if (!meshLocalIds.SequenceEqual(currentLocalIds))
            {
                failures.Add(
                    $"Mesh local file IDs changed for '{assetPath}': " +
                    $"[{string.Join(",", meshLocalIds)}] -> [{string.Join(",", currentLocalIds)}].");
            }
        }

        private static long[] GetMeshLocalIds(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .Select(mesh =>
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string _, out long localId))
                        throw new InvalidOperationException($"Unable to read Mesh local file ID: '{path}'::{mesh.name}.");
                    return localId;
                })
                .OrderBy(localId => localId)
                .ToArray();
        }
    }
}

internal sealed class PPEMeshSubAssetNamePostprocessor : AssetPostprocessor
{
    private void OnPostprocessModel(GameObject root)
    {
        if (!PPEMeshSubAssetNamingMigration.TryGetSpec(assetPath, out var spec))
            return;

        HashSet<Mesh> meshes = new();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null)
                meshes.Add(filter.sharedMesh);
        }

        foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh != null)
                meshes.Add(renderer.sharedMesh);
        }

        foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
        {
            if (collider.sharedMesh != null)
                meshes.Add(collider.sharedMesh);
        }

        Mesh[] orderedMeshes = meshes
            .OrderBy(mesh => mesh.name, StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < orderedMeshes.Length; index++)
        {
            Mesh mesh = orderedMeshes[index];
            mesh.name = PPEMeshSubAssetNamingMigration.GetExpectedMeshName(
                spec,
                mesh.name,
                orderedMeshes.Length,
                index);
        }
    }
}
