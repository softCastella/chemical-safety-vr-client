using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the external 34-asset naming baseline without replacing model data or GUIDs.
/// The operation is explicit because the scale-0 scene and Inspector-authored references
/// are the source of truth.
/// </summary>
public static class PPE3DAssetNamingMigration
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";

    static readonly AssetMove[] AssetMoves =
    {
        new("Assets/FBX/ppe_room_bench/ppe_room_bench.fbx", "Assets/FBX/ppe_room_bench/PPE_B_Bench.fbx"),
        new("Assets/FBX/metal_locker/metal_locker.fbx", "Assets/FBX/metal_locker/PPE_B_MetalLocker.fbx"),
        new("Assets/FBX/mask_locker/mask+locker.fbx", "Assets/FBX/mask_locker/PPE_B_MaskLocker.fbx"),
        new("Assets/FBX/safety_cabinet/safety_cabinet.fbx", "Assets/FBX/safety_cabinet/PPE_B_SafetyCabinet.fbx"),
        new("Assets/FBX/wooden crate/tripo_convert_f13f255c-09b9-4a63-b9df-ce1b2c3b6a16.fbx", "Assets/FBX/wooden crate/PPE_B_WoodenCrate.fbx"),
        new("Assets/FBX/hanger piece/hanger+piece.fbx", "Assets/FBX/hanger piece/PPE_B_WallHanger.fbx"),
        new("Assets/FBX/hand sanitizer dispenser/hand+sanitizer+dispenser.fbx", "Assets/FBX/hand sanitizer dispenser/PPE_B_HandSanitizer.fbx"),
        new("Assets/4_PPE_B_FireExtinguisher_01.fbx", "Assets/PPE_B_FireExtinguisher.fbx"),
        new("Assets/FBX/yellow trash bin/tripo_convert_5d00fc85-3cea-42f1-a92c-0830bf1117f3.fbx", "Assets/FBX/yellow trash bin/PPE_B_YellowTrashBin.fbx"),
        new("Assets/FBX/storage wall rack/storage+wall+rack.fbx", "Assets/FBX/storage wall rack/PPE_B_StorageWallRack.fbx"),
        new("Assets/FBX/hazmat_suit_hanger/hazmat_suit_hanger.fbx", "Assets/FBX/hazmat_suit_hanger/PPE_B_SuitHanger.fbx"),
        new("Assets/FBX/cleaning_cart/janitorial+cart.fbx", "Assets/FBX/cleaning_cart/PPE_B_CleaningCart.fbx"),
        new("Assets/UIs/Facilities/PPE_Room/Duct/tripo_convert_90fd59d6-1fe2-4c21-8e27-21ea64fb29a5.fbx", "Assets/UIs/Facilities/PPE_Room/Duct/PPE_B_Duct.fbx"),
        new("Assets/TripoModels/outdoor_security_camera_3d_model/outdoor_security_camera_3d_model.fbx", "Assets/TripoModels/outdoor_security_camera_3d_model/PPE_B_CCTV.fbx"),
        new("Assets/FBX/Tablet/Tablet.fbx", "Assets/FBX/Tablet/PPE_C_Tablet.fbx"),
        new("Assets/FBX/hazmat suit/hazmat+suit.fbx", "Assets/FBX/hazmat suit/PPE_A_SuitHang.fbx"),
        new("Assets/TripoModels/hazmat_suit_3d_model/hazmat_suit_3d_model.fbx", "Assets/TripoModels/hazmat_suit_3d_model/PPE_A_SuitWear.fbx"),
        new("Assets/TripoModels/rubber_boots_3d_model_Clone1/rubber_boots_3d_model_Clone1.fbx", "Assets/TripoModels/rubber_boots_3d_model_Clone1/PPE_A_Boots_L.fbx"),
        new("Assets/TripoModels/rubber_boots_3d_model/rubber_boots_3d_model.fbx", "Assets/TripoModels/rubber_boots_3d_model/PPE_A_Boots_R.fbx"),
        new("Assets/TripoModels/tactical_harness_3d_model/tactical_harness_3d_model.fbx", "Assets/TripoModels/tactical_harness_3d_model/PPE_A_Backplate.fbx"),
        new("Assets/TripoModels/gas_mask_3d_model_Clone1/gas_mask_3d_model_Clone1.fbx", "Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx"),
        new("Assets/FBX/helmet/helmet.fbx", "Assets/FBX/helmet/PPE_A_Helmet_Strap.fbx"),
        new("Assets/FBX/glove_Left/glove_Left.fbx", "Assets/FBX/glove_Left/PPE_A_Glove_L.fbx"),
        new("Assets/FBX/glove_Right/glove_right.fbx", "Assets/FBX/glove_Right/PPE_A_Glove_R.fbx"),
        new("Assets/TripoModels/orange_tape_roll_3d_model/orange_tape_roll_3d_model.fbx", "Assets/TripoModels/orange_tape_roll_3d_model/PPE_A_Tape.fbx"),
        new("Assets/FBX/taped/taped.fbx", "Assets/FBX/taped/PPE_A_Taped.fbx"),
        new("Assets/FBX/hand/BareHand/LeftHand_BareHand.fbx", "Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_L.fbx"),
        new("Assets/FBX/hand/BareHand/RightHand_BareHand.fbx", "Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_R.fbx"),
        new("Assets/FBX/hand/BareHand_Suit/LeftHand_BareHand_Suit.fbx", "Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_L.fbx"),
        new("Assets/FBX/hand/BareHand_Suit/RightHand_BareHand_Suit.fbx", "Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_R.fbx"),
        new("Assets/FBX/hand/Glove_Suit/LeftHand_Glove_Suit.fbx", "Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_L.fbx"),
        new("Assets/FBX/hand/Glove_Suit/RightHand_Glove_Suit.fbx", "Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_R.fbx"),
        new("Assets/FBX/hand/Glove_Suit_Tape/LeftHand_Glove_Suit_Tape.fbx", "Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_L.fbx"),
        new("Assets/FBX/hand/Glove_Suit_Tape/RightHand_Glove_Suit_Tape.fbx", "Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_R.fbx"),

        // Only direct, project-owned presentation prefabs are renamed. Controller and
        // hand-pose prefabs keep their functional names even though they reference the FBXs.
        new("Assets/UIs/Things/PPE_room/rubber_boots_3d_model_Clone1.prefab", "Assets/UIs/Things/PPE_room/PPE_A_Boots_L.prefab"),
        new("Assets/UIs/Things/PPE_room/rubber_boots_3d_model.prefab", "Assets/UIs/Things/PPE_room/PPE_A_Boots_R.prefab"),
        new("Assets/UIs/Things/PPE_room/tactical_harness_3d_model.prefab", "Assets/UIs/Things/PPE_room/PPE_A_Backplate.prefab"),
        new("Assets/UIs/Things/PPE_room/orange_tape_roll_3d_model.prefab", "Assets/UIs/Things/PPE_room/PPE_A_Tape.prefab"),
        new("Assets/Prefabs/helmet.prefab", "Assets/Prefabs/PPE_A_Helmet_Strap.prefab"),
        new("Assets/Prefabs/LeftHand_BareHand.prefab", "Assets/Prefabs/PPE_A_Hand_Bare_L.prefab"),
        new("Assets/Prefabs/RightHand_BareHand.prefab", "Assets/Prefabs/PPE_A_Hand_Bare_R.prefab"),
    };

    static readonly SceneRename[] SceneRenames =
    {
        new("interiorObjects/Bg/4_PPE_B_FireExtinguisher_01", "PPE_B_FireExtinguisher_01"),
        new("interiorObjects/Bg/4_PPE_B_FireExtinguisher_02", "PPE_B_FireExtinguisher_02"),
        new("interiorObjects/Bg/PPE_B_HandSanitizer_01", "PPE_B_HandSanitizer"),
        new("interiorObjects/Bg/PPE_B_WallHanger_01", "PPE_B_WallHanger"),
        new("interiorObjects/Bg/PPE_B_WallHanger_01/wooden crate", "PPE_B_WoodenCrate_01"),
        new("interiorObjects/Bg/PPE_B_WallHanger_01/wooden crate (1)", "PPE_B_WoodenCrate_02"),
        new("interiorObjects/Bg/PPE_B_Bench_01", "PPE_B_Bench"),
        new("interiorObjects/Bg/PPE_B_CleaningCart_01", "PPE_B_CleaningCart"),
        new("interiorObjects/Bg/PPE_B_Hanger_01", "PPE_B_StorageWallRack"),
        new("interiorObjects/Bg/PPE_B_MaskLocker_01", "PPE_B_MaskLocker"),
        new("interiorObjects/Bg/PPE_B_MetalLocker_01", "PPE_B_MetalLocker"),
        new("interiorObjects/Bg/PPE_B_SafetyCabinet_01", "PPE_B_SafetyCabinet"),
        new("interiorObjects/Bg/PPE_B_SuitHanger_01", "PPE_B_SuitHanger"),
        new("PPE Room/PPE_B_Duct_01", "PPE_B_Duct"),
        new("PPE/PPE_A_Boot_L_01", "PPE_A_Boots_L"),
        new("PPE/PPE_A_Boot_R_01", "PPE_A_Boots_R"),
        new("PPE/PPE_A_GasMask_01", "PPE_A_Mask"),
        new("PPE/PPE_A_Glove_L_01", "PPE_A_Glove_L"),
        new("PPE/PPE_A_Glove_R_01", "PPE_A_Glove_R"),
        new("PPE/PPE_A_Helmet_Strap_01", "PPE_A_Helmet_Strap"),
        new("PPE/PPE_A_Helmet_Strap_01/PPE_A_Helmet_NoStrap_01", "PPE_A_Helmet_NoStrap"),
        new("PPE/PPE_A_SuitHang_01", "PPE_A_SuitHang"),
        new("PPE/PPE_C_GripTape_01", "PPE_A_Tape"),
        new("PPE/PPE_C_Tablet_01", "PPE_C_Tablet"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01", "PPE_A_SuitWear"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/PPE_A_Boot_L_01", "PPE_A_Boots_L"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/PPE_A_Boot_R_01", "PPE_A_Boots_R"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/PPE_A_GasMask_01", "PPE_A_Mask"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/PPE_A_Glove_L_01", "PPE_A_Glove_L"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/PPE_A_Glove_R_01", "PPE_A_Glove_R"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/backplate_1", "PPE_A_Backplate"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/helmet_1", "PPE_A_Helmet_Strap"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/taped_boot R_1", "PPE_A_Taped_Boot_R"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/taped_boot_L_1", "PPE_A_Taped_Boot_L"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/taped_hand_R_1", "PPE_A_Taped_Hand_R"),
        new("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear_01/taped_hand_L_1", "PPE_A_Taped_Hand_L"),
        new("XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Bare_L_01", "PPE_A_Hand_Bare_L"),
        new("XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Bare_R_01", "PPE_A_Hand_Bare_R"),
        new("XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Suit_L_01", "PPE_A_Hand_Suit_L"),
        new("XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Suit_R_01", "PPE_A_Hand_Suit_R"),
        new("XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Glove_L_01", "PPE_A_Hand_GloveSuit_L"),
        new("XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Glove_R_01", "PPE_A_Hand_GloveSuit_R"),
        new("XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_L_01", "PPE_A_Hand_GloveTape_L"),
        new("XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_R_01", "PPE_A_Hand_GloveTape_R"),
    };

    [MenuItem("Tools/PPE/Apply External 34-Asset Naming")]
    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before applying the PPE asset naming migration.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Open '{ScenePath}' before applying the PPE asset naming migration.");

        Dictionary<SceneRename, GameObject> sceneTargets = PreflightScene(scene);
        Dictionary<AssetMove, string> originalGuids = PreflightAssets();
        ValidateAnimationBindingsDoNotUseRenamedSegments();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply PPE external asset naming");

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (AssetMove move in AssetMoves)
            {
                if (AssetDatabase.LoadMainAssetAtPath(move.SourcePath) == null)
                    continue;

                string error = AssetDatabase.MoveAsset(move.SourcePath, move.DestinationPath);
                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException($"Asset move failed: '{move.SourcePath}' -> '{move.DestinationPath}': {error}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        foreach (KeyValuePair<SceneRename, GameObject> entry in sceneTargets)
        {
            Undo.RecordObject(entry.Value, "Rename PPE scene object");
            entry.Value.name = entry.Key.NewName;
            EditorUtility.SetDirty(entry.Value);
        }

        VerifyGuids(originalGuids);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Undo.CollapseUndoOperations(undoGroup);

        PPE3DAssetRenameDependencyValidation.ValidateScene(scene, true);
        Debug.Log($"PPE external 34-asset naming applied: {AssetMoves.Length} asset paths and {SceneRenames.Length} scene objects checked.");
    }

    static Dictionary<AssetMove, string> PreflightAssets()
    {
        Dictionary<AssetMove, string> guids = new Dictionary<AssetMove, string>();
        foreach (AssetMove move in AssetMoves)
        {
            UnityEngine.Object source = AssetDatabase.LoadMainAssetAtPath(move.SourcePath);
            UnityEngine.Object destination = AssetDatabase.LoadMainAssetAtPath(move.DestinationPath);
            if (source != null && destination != null)
                throw new InvalidOperationException($"Both source and destination assets exist: '{move.SourcePath}', '{move.DestinationPath}'.");
            if (source == null && destination == null)
                throw new FileNotFoundException($"Neither source nor destination asset exists for '{move.DestinationPath}'.");

            string activePath = source != null ? move.SourcePath : move.DestinationPath;
            string guid = AssetDatabase.AssetPathToGUID(activePath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Asset GUID is missing: '{activePath}'.");
            guids.Add(move, guid);
        }

        return guids;
    }

    static Dictionary<SceneRename, GameObject> PreflightScene(Scene scene)
    {
        Dictionary<string, GameObject> byPath = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .GroupBy(GetPath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().gameObject, StringComparer.Ordinal);

        Dictionary<SceneRename, GameObject> result = new Dictionary<SceneRename, GameObject>();
        foreach (SceneRename rename in SceneRenames)
        {
            if (!byPath.TryGetValue(rename.OldPath, out GameObject target))
                throw new InvalidOperationException($"Required scene object is missing: '{rename.OldPath}'.");
            result.Add(rename, target);
        }

        return result;
    }

    static void ValidateAnimationBindingsDoNotUseRenamedSegments()
    {
        HashSet<string> oldNames = SceneRenames
            .Select(rename => Path.GetFileName(rename.OldPath))
            .ToHashSet(StringComparer.Ordinal);

        foreach (string dependency in AssetDatabase.GetDependencies(ScenePath, true))
        {
            if (!string.Equals(Path.GetExtension(dependency), ".anim", StringComparison.OrdinalIgnoreCase))
                continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(dependency);
            if (clip == null)
                continue;

            IEnumerable<EditorCurveBinding> bindings = AnimationUtility.GetCurveBindings(clip)
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip));
            foreach (EditorCurveBinding binding in bindings)
            {
                string affected = binding.path.Split('/').FirstOrDefault(oldNames.Contains);
                if (affected != null)
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{dependency}::{clip.name}' binds renamed path segment '{affected}' in '{binding.path}'. " +
                        "Update the authored animation path before applying this migration.");
                }
            }
        }
    }

    static void VerifyGuids(Dictionary<AssetMove, string> originalGuids)
    {
        foreach (KeyValuePair<AssetMove, string> entry in originalGuids)
        {
            string currentGuid = AssetDatabase.AssetPathToGUID(entry.Key.DestinationPath);
            if (!string.Equals(currentGuid, entry.Value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"GUID changed while moving '{entry.Key.DestinationPath}': expected {entry.Value}, found {currentGuid}.");
            }
        }
    }

    static string GetPath(Transform transform)
    {
        List<string> names = new List<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    sealed class AssetMove
    {
        public AssetMove(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public string SourcePath { get; }
        public string DestinationPath { get; }
    }

    sealed class SceneRename
    {
        public SceneRename(string oldPath, string newName)
        {
            OldPath = oldPath;
            NewName = newName;
        }

        public string OldPath { get; }
        public string NewName { get; }
    }
}
