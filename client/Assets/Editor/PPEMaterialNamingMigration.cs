using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renames project-owned Material assets that still expose Tripo-generated names.
/// The operation moves .mat assets with AssetDatabase.MoveAsset, preserves GUIDs,
/// and changes only the asset filename and Material.name.
/// </summary>
public static class PPEMaterialNamingMigration
{
    private const string MenuRoot = "Tools/PPE/Material Naming/";

    private static readonly Regex StorageWallRackPattern = new(
        @"^Assets/Materials/PPE/Storage Wall Rack/StorageWallRack_tripo_part_(?<index>\d+)\.mat$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex MaskGlassPattern = new(
        @"^Assets/Generated/PPE/MaskGlass/Mask1UnlitMaterials/Material_tripo_part_(?<index>\d+)(?<suffix>\.001_Unlit)\.mat$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex HelmetPattern = new(
        @"^Assets/FBX/helmet/Materials/construction\+helmet\+3d\+model_Clone1_tripo_part_(?<index>\d+)_basecolor\.mat$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex MixerRoomPattern = new(
        @"^Assets/Materials/MixerRoom/Unlit/tripo_node_(?<suffix>.+)\.mat$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> ExtractedMaterialNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Assets/FBX/cleaning_cart/tripo_node_b49a85f0-aa3e-4056-8572-1d94a1e0a86f_material.mat"] = "PPE_B_CleaningCart_Material.mat",
            ["Assets/FBX/hand sanitizer dispenser/tripo_node_2462093d-153f-494f-b683-209107675506_material.mat"] = "PPE_B_HandSanitizer_Material.mat",
            ["Assets/FBX/hazmat suit/tripo_node_3714baa9-6328-444d-8f9d-eb28c31df685_material.mat"] = "PPE_A_SuitHang_Material.mat",
            ["Assets/FBX/hazmat_suit_hanger/hazmat_suit_hanger.fbm/tripo_node_7bc83317-57ff-42e1-bf12-f2039d5c5c86_material.mat"] = "PPE_B_SuitHanger_Material.mat",
            ["Assets/FBX/mask_locker/tripo_node_38071929-f8b8-4da5-b2dc-6c3f8a58e779_material.mat"] = "PPE_B_MaskLocker_Material.mat",
            ["Assets/FBX/metal_locker/tripo_node_7c300fa3.mat"] = "PPE_B_MetalLocker_Material.mat",
            ["Assets/FBX/ppe_room_bench/tripo_node_2a0ef0a3-fe60-47cf-b0c0-98212291dfaf_material.mat"] = "PPE_B_Bench_Material.mat",
            ["Assets/FBX/safety_cabinet/tripo_node_95058666.mat"] = "PPE_B_SafetyCabinet_Material.mat",
            ["Assets/TripoModels/tactical_harness_3d_model/Materials/tactical_harness_3d_model.mat"] = "PPE_A_Backplate_Material.mat",
            ["Assets/Materials/PPE/Scene Unlit/tactical_harness_3d_model_Unlit.mat"] = "PPE_A_Backplate_Unlit.mat",
        };

    [MenuItem(MenuRoot + "Audit Legacy Material Names")]
    public static void Audit()
    {
        List<RenameEntry> entries = CollectEntries();
        Debug.Log(
            $"[PPE Material Naming] audit|legacy={entries.Count}|" +
            $"storageWallRack={entries.Count(entry => entry.Kind == "StorageWallRack")}|" +
            $"maskGlass={entries.Count(entry => entry.Kind == "MaskGlass")}|" +
            $"helmet={entries.Count(entry => entry.Kind == "Helmet")}|" +
            $"extracted={entries.Count(entry => entry.Kind == "Extracted")}|" +
            $"mixerRoom={entries.Count(entry => entry.Kind == "MixerRoom")}");

        foreach (RenameEntry entry in entries.OrderBy(entry => entry.SourcePath, StringComparer.Ordinal))
            Debug.Log($"[PPE Material Naming] candidate|{entry.Kind}|{entry.SourcePath}|{entry.DestinationPath}");
    }

    [MenuItem(MenuRoot + "Apply Material Names")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before applying PPE Material names.");

        List<RenameEntry> entries = CollectEntries();
        ValidatePreflight(entries);
        Dictionary<string, string> originalGuids = entries.ToDictionary(
            entry => entry.SourcePath,
            entry => AssetDatabase.AssetPathToGUID(entry.SourcePath),
            StringComparer.Ordinal);

        int movedCount = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (RenameEntry entry in entries)
            {
                if (string.Equals(entry.SourcePath, entry.DestinationPath, StringComparison.Ordinal))
                    continue;

                string error = AssetDatabase.MoveAsset(entry.SourcePath, entry.DestinationPath);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"Material move failed: '{entry.SourcePath}' -> '{entry.DestinationPath}': {error}");
                }

                movedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (RenameEntry entry in entries)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(entry.DestinationPath);
            if (material == null)
                throw new InvalidOperationException($"Renamed material could not be loaded: '{entry.DestinationPath}'.");

            if (!string.Equals(material.name, entry.DestinationName, StringComparison.Ordinal))
            {
                material.name = entry.DestinationName;
                EditorUtility.SetDirty(material);
            }

            string currentGuid = AssetDatabase.AssetPathToGUID(entry.DestinationPath);
            if (!originalGuids[entry.SourcePath].Equals(currentGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Material GUID changed for '{entry.SourcePath}' -> '{entry.DestinationPath}'.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateNoLegacyNames();
        Debug.Log($"[PPE Material Naming] applied|renamed={movedCount}|identityChanges=0");
    }

    [MenuItem(MenuRoot + "Validate Material Names")]
    public static void Validate()
    {
        ValidateNoLegacyNames();
        Debug.Log("[PPE Material Naming] validation passed|legacy=0");
    }

    private static List<RenameEntry> CollectEntries()
    {
        var entries = new List<RenameEntry>();
        foreach (string assetGuid in AssetDatabase.FindAssets("t:Material"))
        {
            string path = NormalizePath(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (string.IsNullOrEmpty(path))
                continue;

            if (TryBuildRegexEntry(path, StorageWallRackPattern, "StorageWallRack", match =>
                    $"PPE_B_StorageWallRack_Part_{match.Groups["index"].Value}.mat", out RenameEntry storageEntry))
            {
                entries.Add(storageEntry);
                continue;
            }

            if (TryBuildRegexEntry(path, MaskGlassPattern, "MaskGlass", match =>
                    $"PPE_A_Mask_Part_{match.Groups["index"].Value}{match.Groups["suffix"].Value}.mat", out RenameEntry maskEntry))
            {
                entries.Add(maskEntry);
                continue;
            }

            if (TryBuildRegexEntry(path, HelmetPattern, "Helmet", match =>
                    $"PPE_A_Helmet_Strap_Part_{match.Groups["index"].Value}_BaseColor.mat", out RenameEntry helmetEntry))
            {
                entries.Add(helmetEntry);
                continue;
            }

            if (TryBuildRegexEntry(path, MixerRoomPattern, "MixerRoom", match =>
                    $"MixerRoom_Node_{match.Groups["suffix"].Value}.mat", out RenameEntry mixerEntry))
            {
                entries.Add(mixerEntry);
                continue;
            }

            if (!ExtractedMaterialNames.TryGetValue(path, out string destinationName))
                continue;

            entries.Add(new RenameEntry(
                path,
                CombineAssetDirectory(path, destinationName),
                Path.GetFileNameWithoutExtension(destinationName),
                "Extracted"));
        }

        return entries;
    }

    private static bool TryBuildRegexEntry(
        string path,
        Regex pattern,
        string kind,
        Func<Match, string> destinationName,
        out RenameEntry entry)
    {
        Match match = pattern.Match(path);
        if (!match.Success)
        {
            entry = default;
            return false;
        }

        string destinationFileName = destinationName(match);
        entry = new RenameEntry(
            path,
            CombineAssetDirectory(path, destinationFileName),
            Path.GetFileNameWithoutExtension(destinationFileName),
            kind);
        return true;
    }

    private static void ValidatePreflight(IReadOnlyList<RenameEntry> entries)
    {
        var failures = new List<string>();
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (RenameEntry entry in entries)
        {
            if (!destinations.Add(entry.DestinationPath))
                failures.Add($"Duplicate destination: '{entry.DestinationPath}'.");

            UnityEngine.Object destination = AssetDatabase.LoadMainAssetAtPath(entry.DestinationPath);
            if (destination != null &&
                !string.Equals(entry.SourcePath, entry.DestinationPath, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"Destination already exists: '{entry.DestinationPath}'.");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("PPE Material naming preflight failed:\n- " + string.Join("\n- ", failures));
    }

    private static void ValidateNoLegacyNames()
    {
        List<RenameEntry> remaining = CollectEntries();
        if (remaining.Count == 0)
            return;

        string message = "PPE Material naming validation failed; legacy materials remain:\n- " +
                         string.Join("\n- ", remaining.Select(entry => entry.SourcePath));
        Debug.LogError(message);
        throw new InvalidOperationException(message);
    }

    private static string CombineAssetDirectory(string path, string fileName)
    {
        string directory = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
        return string.IsNullOrEmpty(directory) ? fileName : directory + "/" + fileName;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private readonly struct RenameEntry
    {
        public RenameEntry(string sourcePath, string destinationPath, string destinationName, string kind)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            DestinationName = destinationName;
            Kind = kind;
        }

        public string SourcePath { get; }
        public string DestinationPath { get; }
        public string DestinationName { get; }
        public string Kind { get; }
    }
}
