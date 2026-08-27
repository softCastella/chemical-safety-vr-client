using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Read-only regression validation for the standardized PPE 3D asset names in
/// the scale-0 build scene. This validator never changes scene objects, assets,
/// component state, or authored Transform values.
/// </summary>
public static class PPE3DAssetRenameDependencyValidation
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";

    static readonly string[] ExpectedPaths =
    {
        "interiorObjects/Bg/PPE_B_FireExtinguisher_01",
        "interiorObjects/Bg/PPE_B_FireExtinguisher_02",
        "interiorObjects/Bg/PPE_B_HandSanitizer",
        "interiorObjects/Bg/PPE_B_WallHanger",
        "interiorObjects/Bg/PPE_B_WallHanger/PPE_B_WoodenCrate_01",
        "interiorObjects/Bg/PPE_B_WallHanger/PPE_B_WoodenCrate_02",
        "interiorObjects/Bg/PPE_B_Bench",
        "interiorObjects/Bg/PPE_B_CCTV_01",
        "interiorObjects/Bg/PPE_B_CCTV_02",
        "interiorObjects/Bg/PPE_B_CleaningCart",
        "interiorObjects/Bg/PPE_B_StorageWallRack",
        "interiorObjects/Bg/PPE_B_MaskLocker",
        "interiorObjects/Bg/PPE_B_MetalLocker",
        "interiorObjects/Bg/PPE_B_SafetyCabinet",
        "interiorObjects/Bg/PPE_B_SuitHanger",
        "interiorObjects/Bg/PPE_B_YellowTrashBin_01",
        "interiorObjects/Bg/PPE_B_YellowTrashBin_02",
        "interiorObjects/Bg/PPE_B_YellowTrashBin_03",
        "PPE Room/PPE_B_Duct",
        "PPE/PPE_A_Boots_L",
        "PPE/PPE_A_Boots_R",
        "PPE/PPE_A_Mask",
        "PPE/PPE_A_Glove_L",
        "PPE/PPE_A_Glove_R",
        "PPE/PPE_A_Helmet_Strap",
        "PPE/PPE_A_Helmet_Strap/PPE_A_Helmet_NoStrap",
        "PPE/PPE_A_Backplate",
        "PPE/PPE_A_ScubaGear_01",
        "PPE/PPE_A_SuitHang",
        "PPE/PPE_A_Tape",
        "PPE/PPE_C_Tablet",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Boots_L",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Boots_R",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Mask",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Glove_L",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Glove_R",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Backplate",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Helmet_Strap",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Taped_Boot_L",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Taped_Boot_R",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Taped_Hand_L",
        "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Taped_Hand_R",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Bare_L",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Bare_R",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Suit_L",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Suit_R",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_GloveSuit_L",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_GloveSuit_R",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_L",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_R",
    };

    static readonly string[] ExpectedAssetPaths =
    {
        "Assets/FBX/ppe_room_bench/PPE_B_Bench.fbx",
        "Assets/FBX/metal_locker/PPE_B_MetalLocker.fbx",
        "Assets/FBX/mask_locker/PPE_B_MaskLocker.fbx",
        "Assets/FBX/safety_cabinet/PPE_B_SafetyCabinet.fbx",
        "Assets/FBX/wooden crate/PPE_B_WoodenCrate.fbx",
        "Assets/FBX/hanger piece/PPE_B_WallHanger.fbx",
        "Assets/FBX/hand sanitizer dispenser/PPE_B_HandSanitizer.fbx",
        "Assets/PPE_B_FireExtinguisher.fbx",
        "Assets/FBX/yellow trash bin/PPE_B_YellowTrashBin.fbx",
        "Assets/FBX/storage wall rack/PPE_B_StorageWallRack.fbx",
        "Assets/FBX/hazmat_suit_hanger/PPE_B_SuitHanger.fbx",
        "Assets/FBX/cleaning_cart/PPE_B_CleaningCart.fbx",
        "Assets/UIs/Facilities/PPE_Room/Duct/PPE_B_Duct.fbx",
        "Assets/TripoModels/outdoor_security_camera_3d_model/PPE_B_CCTV.fbx",
        "Assets/FBX/Tablet/PPE_C_Tablet.fbx",
        "Assets/FBX/hazmat suit/PPE_A_SuitHang.fbx",
        "Assets/TripoModels/hazmat_suit_3d_model/PPE_A_SuitWear.fbx",
        "Assets/TripoModels/rubber_boots_3d_model_Clone1/PPE_A_Boots_L.fbx",
        "Assets/TripoModels/rubber_boots_3d_model/PPE_A_Boots_R.fbx",
        "Assets/TripoModels/tactical_harness_3d_model/PPE_A_Backplate.fbx",
        "Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx",
        "Assets/FBX/helmet/PPE_A_Helmet_Strap.fbx",
        "Assets/FBX/glove_Left/PPE_A_Glove_L.fbx",
        "Assets/FBX/glove_Right/PPE_A_Glove_R.fbx",
        "Assets/TripoModels/orange_tape_roll_3d_model/PPE_A_Tape.fbx",
        "Assets/FBX/taped/PPE_A_Taped.fbx",
        "Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_L.fbx",
        "Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_R.fbx",
        "Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_L.fbx",
        "Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_R.fbx",
        "Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_L.fbx",
        "Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_R.fbx",
        "Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_L.fbx",
        "Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_R.fbx",
        "Assets/UIs/Things/PPE_room/PPE_A_Boots_L.prefab",
        "Assets/UIs/Things/PPE_room/PPE_A_Boots_R.prefab",
        "Assets/UIs/Things/PPE_room/PPE_A_Backplate.prefab",
        "Assets/UIs/Things/PPE_room/PPE_A_Tape.prefab",
        "Assets/Prefabs/PPE_A_Helmet_Strap.prefab",
        "Assets/Prefabs/PPE_A_Hand_Bare_L.prefab",
        "Assets/Prefabs/PPE_A_Hand_Bare_R.prefab",
    };

    static readonly string[] LegacyNames =
    {
        "4_PPE_B_FireExtinguisher_01",
        "4_PPE_B_FireExtinguisher_02",
        "PPE_B_HandSanitizer_01",
        "PPE_B_WallHanger_01",
        "PPE_B_Bench_01",
        "PPE_B_CleaningCart_01",
        "PPE_B_Hanger_01",
        "PPE_B_MaskLocker_01",
        "PPE_B_MetalLocker_01",
        "PPE_B_SafetyCabinet_01",
        "PPE_B_SuitHanger_01",
        "PPE_B_Duct_01",
        "PPE_A_Boot_L_01",
        "PPE_A_Boot_R_01",
        "PPE_A_GasMask_01",
        "PPE_A_Glove_L_01",
        "PPE_A_Glove_R_01",
        "PPE_A_Helmet_Strap_01",
        "PPE_A_Helmet_NoStrap_01",
        "PPE_A_SuitHang_01",
        "PPE_A_SuitWear_01",
        "PPE_C_GripTape_01",
        "PPE_C_Tablet_01",
        "PPE_A_Hand_Bare_L_01",
        "PPE_A_Hand_Bare_R_01",
        "PPE_A_Hand_Suit_L_01",
        "PPE_A_Hand_Suit_R_01",
        "PPE_A_Hand_Glove_L_01",
        "PPE_A_Hand_Glove_R_01",
        "PPE_A_Hand_GloveTape_L_01",
        "PPE_A_Hand_GloveTape_R_01",
        "glove_R_1",
        "PPE_wall_hanger",
        "boots_R_1",
        "boots_L_1",
        "glove_L_1",
        "hazmat_suit_on_10",
        "fire_extinguisher_1",
        "fire_extinguisher_1 (1)",
        "mask_1",
        "scuba_gear",
        "hand_sanitizer_dispenser",
        "glove_right",
        "glove_Left",
        "mask",
        "boots_R",
        "backplate",
        "boots_L",
        "tape",
        "hazmat_suit_off",
        "helmet",
        "helmet_wrong",
        "Tablet",
        "LeftHand_BareHand",
        "RightHand_BareHand",
        "LeftHand_BareHand_Suit",
        "RightHand_BareHand_Suit",
        "LeftHand_Glove_Suit",
        "RightHand_Glove_Suit",
        "LeftHand_Glove_Suit_Tape",
        "RightHand_Glove_Suit_Tape",
        "cleaning_cart",
        "duct",
        "hazmat_suit_hanger",
        "hanger",
        "mask_locker",
        "metal_locker",
        "outdoor_security_camera_0",
        "outdoor_security_camera_1",
        "ppe_room_bench_2",
        "safety_cabinet",
        "yellow_trash_bin_0",
        "yellow_trash_bin_1",
    };

    static readonly string[] TargetNameDependentSourcePaths =
    {
        "Assets/Editor/PPEDefectVisualSetup.cs",
        "Assets/Editor/PPEFullSuitFloorGrounding.cs",
        "Assets/Editor/PPEHelmetFbxUnlitSetup.cs",
        "Assets/Editor/PPETapedFbxUnlitSetup.cs",
        "Assets/Editor/StorageWallRackMaterialPostprocessor.cs",
        "Assets/Scripts/PPEBackgroundRoom.cs",
        "Assets/Scripts/PPETabletChecklistController.cs",
    };

    // User-authored, non-baseline model intentionally kept outside the 34 PPE assets.
    // Restrict the exception to this exact root path so legacy child objects named
    // "mask" elsewhere are still reported.
    static readonly HashSet<string> AllowedOutOfScopeLegacyPaths = new HashSet<string>(StringComparer.Ordinal)
    {
        "mask",
    };

    static readonly InspectionExpectation[] InspectionExpectations =
    {
        new InspectionExpectation(
            "PPE/PPE_A_SuitHang",
            PPEItemType.HazmatSuit,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear"),
        new InspectionExpectation(
            "PPE/PPE_A_Glove_L",
            PPEItemType.RubberGloveLeft,
            null),
        new InspectionExpectation(
            "PPE/PPE_A_Glove_R",
            PPEItemType.RubberGloveRight,
            null),
        new InspectionExpectation(
            "PPE/PPE_A_Boots_L",
            PPEItemType.RubberBootLeft,
            null),
        new InspectionExpectation(
            "PPE/PPE_A_Boots_R",
            PPEItemType.RubberBootRight,
            null),
        new InspectionExpectation(
            "PPE/PPE_A_Mask",
            PPEItemType.GasMask,
            null),
        new InspectionExpectation(
            "PPE/PPE_A_Helmet_Strap",
            PPEItemType.ConstructionHelmet,
            null),
        new InspectionExpectation("PPE/PPE_A_Backplate", PPEItemType.TacticalHarness, null),
        new InspectionExpectation("PPE/PPE_A_Tape", PPEItemType.PackingTape, null),
    };

    static readonly EquippedExpectation[] EquippedExpectations =
    {
        new EquippedExpectation(
            PPEItemType.RubberGloveLeft,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Glove_L"),
        new EquippedExpectation(
            PPEItemType.RubberGloveRight,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Glove_R"),
        new EquippedExpectation(
            PPEItemType.RubberBootLeft,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Boots_L"),
        new EquippedExpectation(
            PPEItemType.RubberBootRight,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Boots_R"),
        new EquippedExpectation(
            PPEItemType.GasMask,
            "XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear/PPE_A_Mask"),
    };

    [MenuItem("Tools/PPE/Validate 3D Asset Rename Dependencies")]
    public static void ValidateFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before validating 3D asset rename dependencies.");
        }

        ValidateScene(scene, true);
    }

    /// <summary>Unity command-line entry point for deterministic validation.</summary>
    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    public static void ValidateScene(Scene scene, bool logSuccess)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"3D asset rename validation requires '{ScenePath}'. Current scene: '{scene.path}'.");
        }

        ValidationContext context = new ValidationContext(scene);
        ValidateExpectedAssets(context);
        ValidateExpectedHierarchy(context);
        ValidateLegacyNames(context);
        ValidateInspectionBindings(context);
        ValidateEquippedVisuals(context);
        ValidateHazmatController(context);
        ValidateEquipmentSlots(context);
        ValidateFinaleQuizPresentation(context);
        ValidateTabletHazmatScope(context);
        ValidateDefectTargets(context);
        ValidateAnimationBindings(context);
        ValidateTargetSourceStrings(context);
        ValidateSubmissionMeshGroups(context);
        AppendPrefabStatus(context);

        if (context.Failures.Count > 0)
        {
            string failureMessage =
                "PPE 3D asset rename dependency validation failed:\n- " +
                string.Join("\n- ", context.Failures);
            Debug.LogError(failureMessage);
            throw new InvalidOperationException(failureMessage);
        }

        if (logSuccess)
        {
            string notes = context.Notes.Count == 0
                ? string.Empty
                : "\n" + string.Join("\n", context.Notes.Select(note => "- " + note));
            Debug.Log(
                $"PPE 3D asset rename dependency validation passed: " +
                $"expectedPaths={ExpectedPaths.Length}, legacyNames=0, " +
                $"inspectionBindings={InspectionExpectations.Length}, " +
                $"equippedSlots={EquippedExpectations.Length}." + notes);
        }
    }

    static void ValidateExpectedHierarchy(ValidationContext context)
    {
        foreach (string path in ExpectedPaths)
            context.Require(path);

        Dictionary<string, int> expectedCounts = ExpectedPaths
            .Select(path => path.Substring(path.LastIndexOf('/') + 1))
            .GroupBy(name => name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (KeyValuePair<string, int> pair in expectedCounts)
        {
            int actualCount = context.AllTransforms.Count(transform => transform.name == pair.Key);
            if (actualCount != pair.Value)
            {
                context.Fail(
                    $"Expected {pair.Value} scene object(s) named '{pair.Key}', found {actualCount}.");
            }
        }
    }

    static void ValidateExpectedAssets(ValidationContext context)
    {
        foreach (string assetPath in ExpectedAssetPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                context.Fail($"Required standardized Project asset is missing: '{assetPath}'.");
        }
    }

    static void ValidateLegacyNames(ValidationContext context)
    {
        foreach (string legacyName in LegacyNames)
        {
            Transform[] matches = context.AllTransforms
                .Where(transform =>
                    transform.name == legacyName &&
                    !AllowedOutOfScopeLegacyPaths.Contains(GetPath(transform)))
                .ToArray();
            if (matches.Length == 0)
                continue;

            context.Fail(
                $"Legacy name '{legacyName}' remains at: " +
                string.Join(", ", matches.Select(GetPath)));
        }
    }

    static void ValidateInspectionBindings(ValidationContext context)
    {
        foreach (InspectionExpectation expectation in InspectionExpectations)
        {
            GameObject target = context.Require(expectation.InspectionPath);
            if (target == null)
                continue;

            PPEItemIdentity identity = target.GetComponent<PPEItemIdentity>();
            PPEItemPresentationBinding binding = target.GetComponent<PPEItemPresentationBinding>();
            PPEInspectionState state = target.GetComponent<PPEInspectionState>();
            PPEActionPanelController panel = target.GetComponent<PPEActionPanelController>();
            XRGrabInteractable grab = target.GetComponent<XRGrabInteractable>();

            if (identity == null || identity.ItemType != expectation.ItemType)
                context.Fail($"'{expectation.InspectionPath}' has an incorrect or missing PPEItemIdentity.");
            if (binding == null || !binding.HasCompleteReferences)
                context.Fail($"'{expectation.InspectionPath}' has an incomplete presentation binding.");
            else
            {
                if (binding.ItemIdentity != identity || binding.InspectionVisual != target)
                    context.Fail($"'{expectation.InspectionPath}' binding does not own its inspection identity/visual.");

                if (!string.IsNullOrEmpty(expectation.EquippedPath))
                {
                    GameObject expectedEquipped = context.Require(expectation.EquippedPath);
                    if (expectedEquipped != null && binding.EquippedVisual != expectedEquipped)
                    {
                        context.Fail(
                            $"'{expectation.InspectionPath}' points to the wrong equipped visual. " +
                            $"Expected '{expectation.EquippedPath}'.");
                    }
                }
                else if (binding.EquippedVisual.transform.parent != target.transform ||
                         binding.EquippedVisual.name != "equipped_placeholder")
                {
                    context.Fail(
                        $"'{expectation.InspectionPath}' must keep its local equipped_placeholder binding; " +
                        "the real worn Mesh is owned by PPEEquipmentVisualController.slots.");
                }
            }

            if (grab == null || state == null || state.GrabInteractable != grab ||
                state.PresentationBinding != binding)
            {
                context.Fail($"'{expectation.InspectionPath}' has an incomplete inspection/Grab chain.");
            }

            if (panel == null || panel.InspectionState != state)
                context.Fail($"'{expectation.InspectionPath}' has an incomplete action-panel state reference.");
            if (target.GetComponent<Rigidbody>() == null)
                context.Fail($"'{expectation.InspectionPath}' is missing its authored Rigidbody.");
            if (target.GetComponent<PPEMarkerToggleGrab>() == null)
                context.Fail($"'{expectation.InspectionPath}' is missing its authored marker Grab owner.");
        }
    }

    static void ValidateEquippedVisuals(ValidationContext context)
    {
        foreach (EquippedExpectation expectation in EquippedExpectations)
        {
            GameObject target = context.Require(expectation.Path);
            if (target == null)
                continue;

            if (target.GetComponent<Rigidbody>() != null ||
                target.GetComponent<XRGrabInteractable>() != null ||
                target.GetComponent<PPEMarkerToggleGrab>() != null ||
                target.GetComponent<PPEItemIdentity>() != null ||
                target.GetComponent<PPEItemPresentationBinding>() != null ||
                target.GetComponent<PPEInspectionState>() != null ||
                target.GetComponent<PPEActionPanelController>() != null)
            {
                context.Fail(
                    $"Equipped visual '{expectation.Path}' must remain visual-only and cannot own Grab/physics/inspection state.");
            }
        }
    }

    static void ValidateHazmatController(ValidationContext context)
    {
        GameObject inspection = context.Require("PPE/PPE_A_SuitHang");
        GameObject equipped = context.Require("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear");
        if (inspection == null || equipped == null)
            return;

        PPEItemPresentationBinding binding = inspection.GetComponent<PPEItemPresentationBinding>();
        PPEHazmatEquipController controller = equipped.GetComponent<PPEHazmatEquipController>();
        if (controller == null)
        {
            context.Fail("PPE_A_SuitWear is missing PPEHazmatEquipController.");
            return;
        }

        if (controller.PresentationBinding != binding ||
            controller.ActionPanelController == null ||
            controller.HeadTransform == null ||
            controller.BodyAnchor == null ||
            controller.ApproachAnchor == null ||
            controller.FrontStartAnchor == null ||
            controller.EquippedRenderers == null ||
            controller.EquippedRenderers.Length == 0)
        {
            context.Fail("PPE_A_SuitWear has incomplete serialized Hazmat equip references.");
        }

        if (equipped.GetComponent<Rigidbody>() != null ||
            equipped.GetComponent<XRGrabInteractable>() != null ||
            equipped.GetComponent<PPEItemIdentity>() != null)
        {
            context.Fail("PPE_A_SuitWear must remain a non-physical equipped visual host.");
        }
    }

    static void ValidateEquipmentSlots(ValidationContext context)
    {
        GameObject suit = context.Require("XR Origin (VR)/PPE Body Anchor/PPE_A_SuitWear");
        if (suit == null)
            return;

        PPEEquipmentVisualController[] controllers = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEEquipmentVisualController>(true))
            .Where(controller => controller.EquipmentRoot == suit.transform)
            .ToArray();
        if (controllers.Length != 1)
        {
            context.Fail(
                $"Expected one equipment visual controller for PPE_A_SuitWear, found {controllers.Length}.");
            return;
        }

        PPEEquipmentVisualController controller = controllers[0];
        PPEEquipmentVisualSlot[] slots = controller.Slots ?? Array.Empty<PPEEquipmentVisualSlot>();
        foreach (EquippedExpectation expectation in EquippedExpectations)
        {
            PPEEquipmentVisualSlot[] matches = slots
                .Where(slot => slot?.ItemBinding?.ItemIdentity != null &&
                    slot.ItemBinding.ItemIdentity.ItemType == expectation.ItemType)
                .ToArray();
            if (matches.Length != 1)
            {
                context.Fail(
                    $"Expected one equipped slot for {expectation.ItemType}, found {matches.Length}.");
                continue;
            }

            GameObject expectedChild = context.Require(expectation.Path);
            if (expectedChild != null && matches[0].EquippedChild != expectedChild)
            {
                context.Fail(
                    $"The {expectation.ItemType} equipment slot points to the wrong object. " +
                    $"Expected '{expectation.Path}'.");
            }
        }
    }

    static void ValidateTabletHazmatScope(ValidationContext context)
    {
        PPEItemIdentity[] hazmatIdentities = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEItemIdentity>(true))
            .Where(identity => identity.ItemType == PPEItemType.HazmatSuit)
            .ToArray();
        GameObject expected = context.Require("PPE/PPE_A_SuitHang");
        if (hazmatIdentities.Length != 1 || expected == null || hazmatIdentities[0].gameObject != expected)
        {
            context.Fail(
                $"Tablet Hazmat blocking requires exactly one HazmatSuit identity on PPE_A_SuitHang; " +
                $"found {hazmatIdentities.Length}.");
        }

        GameObject expectedTablet = context.Require("PPE/PPE_C_Tablet");
        PPETabletChecklistController[] tablets = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPETabletChecklistController>(true))
            .ToArray();
        if (tablets.Length != 1 || tablets[0].GrabInteractable == null ||
            tablets[0].SignatureSequence == null ||
            (expectedTablet != null && tablets[0].gameObject != expectedTablet))
        {
            context.Fail(
                $"Expected one PPE_C_Tablet checklist with complete Grab/signature references, found {tablets.Length}.");
        }
    }

    static void ValidateFinaleQuizPresentation(ValidationContext context)
    {
        PPEFinaleController[] finales = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEFinaleController>(true))
            .ToArray();
        PPEQuizController[] quizzes = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEQuizController>(true))
            .ToArray();
        PPEVoiceFlowDirector[] directors = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEVoiceFlowDirector>(true))
            .ToArray();

        if (finales.Length != 1 || quizzes.Length != 1 || directors.Length != 1)
        {
            context.Fail(
                $"Expected one Finale, Quiz, and Voice controller; found " +
                $"finale={finales.Length}, quiz={quizzes.Length}, voice={directors.Length}.");
            return;
        }

        PPEQuizController quiz = quizzes[0];
        if (!quiz.gameObject.activeSelf)
        {
            context.Fail(
                "The PPE Quiz controller Canvas root must remain authored active. " +
                "PPEQuizController.Awake hides only its serialized quizRoot child.");
        }

        SerializedObject quizObject = new SerializedObject(quiz);
        GameObject quizRoot = quizObject.FindProperty("quizRoot")?.objectReferenceValue as GameObject;
        PPEVoiceFlowDirector quizDirector =
            quizObject.FindProperty("voiceFlowDirector")?.objectReferenceValue as PPEVoiceFlowDirector;
        if (quizRoot == null || !quizRoot.transform.IsChildOf(quiz.transform) || quizDirector != directors[0])
        {
            context.Fail("PPE Quiz has incomplete authored root or Voice Flow references.");
        }

        SerializedObject voiceObject = new SerializedObject(directors[0]);
        PPEQuizController voiceQuiz =
            voiceObject.FindProperty("m_QuizController")?.objectReferenceValue as PPEQuizController;
        PPEFinaleController voiceFinale =
            voiceObject.FindProperty("m_FinaleController")?.objectReferenceValue as PPEFinaleController;
        if (voiceQuiz != quiz || voiceFinale != finales[0])
        {
            context.Fail("PPE Voice Flow must reference the authored Quiz and Finale controllers.");
        }
    }

    static void ValidateDefectTargets(ValidationContext context)
    {
        GameObject mask = context.Require("PPE/PPE_A_Mask");
        if (mask != null &&
            (mask.GetComponent<PPEConditionVisualAppearance>() == null ||
             mask.transform.Find("Mask_Crack_Visual") == null))
        {
            context.Fail("Inspection gas mask is missing its authored crack visual binding.");
        }

        foreach (string bootPath in new[] { "PPE/PPE_A_Boots_L", "PPE/PPE_A_Boots_R" })
        {
            GameObject boot = context.Require(bootPath);
            if (boot != null &&
                (boot.GetComponent<PPEConditionAppearance>() == null ||
                 boot.transform.Find("PPE_Defect_Visuals") == null))
            {
                context.Fail($"Inspection boot '{bootPath}' is missing its authored contamination visual binding.");
            }
        }
    }

    static void ValidateAnimationBindings(ValidationContext context)
    {
        string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);
        HashSet<AnimationClip> clips = new HashSet<AnimationClip>();
        foreach (string dependency in dependencies)
        {
            string extension = Path.GetExtension(dependency);
            if (string.Equals(extension, ".anim", StringComparison.OrdinalIgnoreCase))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(dependency);
                if (clip != null)
                    clips.Add(clip);

                continue;
            }

            // Load sub-assets only from imported model files. Calling
            // LoadAllAssetsAtPath for the scene itself attempts to deserialize
            // live scene objects and floods the Console with ReadObjectThreaded
            // errors even though the validation is read-only.
            if (!(AssetImporter.GetAtPath(dependency) is ModelImporter))
                continue;

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(dependency))
            {
                if (asset is AnimationClip clip)
                    clips.Add(clip);
            }
        }

        foreach (AnimationClip clip in clips)
        {
            IEnumerable<EditorCurveBinding> bindings = AnimationUtility.GetCurveBindings(clip)
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip));
            foreach (EditorCurveBinding binding in bindings)
            {
                string legacySegment = LegacyNames.FirstOrDefault(name =>
                    HasPathSegment(binding.path, name));
                if (legacySegment == null)
                    continue;

                context.Fail(
                    $"AnimationClip '{AssetDatabase.GetAssetPath(clip)}::{clip.name}' still binds legacy path " +
                    $"segment '{legacySegment}' in '{binding.path}'.");
            }
        }
    }

    static void ValidateTargetSourceStrings(ValidationContext context)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        List<string> sourcePaths = new List<string>();
        string runtimeRoot = Path.Combine(Application.dataPath, "Scripts");
        if (Directory.Exists(runtimeRoot))
            sourcePaths.AddRange(Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories));

        sourcePaths.AddRange(TargetNameDependentSourcePaths.Select(path =>
            Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar))));

        foreach (string sourcePath in sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(sourcePath))
            {
                context.Fail($"Name-dependent source file is missing: '{sourcePath}'.");
                continue;
            }

            string source = File.ReadAllText(sourcePath);
            foreach (string legacyName in LegacyNames)
            {
                string exactLiteral = "\"" + legacyName + "\"";
                if (source.IndexOf(exactLiteral, StringComparison.Ordinal) < 0)
                    continue;

                context.Fail(
                    $"Source '{MakeProjectRelative(projectRoot, sourcePath)}' still contains exact legacy " +
                    $"name literal {exactLiteral}.");
            }
        }
    }

    static void ValidateSubmissionMeshGroups(ValidationContext context)
    {
        ValidateSameMeshSet(
            context,
            "PPE_B_FireExtinguisher_01",
            "interiorObjects/Bg/PPE_B_FireExtinguisher_01",
            "interiorObjects/Bg/PPE_B_FireExtinguisher_02");

        foreach (EquippedExpectation equippedExpectation in EquippedExpectations)
        {
            InspectionExpectation inspectionExpectation = InspectionExpectations.Single(item =>
                item.ItemType == equippedExpectation.ItemType);
            GameObject inspection = context.Require(inspectionExpectation.InspectionPath);
            GameObject equipped = context.Require(equippedExpectation.Path);
            if (inspection == null || equipped == null)
                continue;

            HashSet<string> inspectionMeshes = GetExternalMeshKeys(inspection);
            HashSet<string> equippedMeshes = GetExternalMeshKeys(equipped);
            if (equippedMeshes.Count == 0 || !equippedMeshes.IsSubsetOf(inspectionMeshes))
            {
                context.Fail(
                    $"{equippedExpectation.ItemType} inspection/worn objects no longer share the same submitted base mesh set.");
            }

            context.Note(
                $"Package {inspection.name}: scene uses 2 objects " +
                $"(inspection 1, worn visual 1; inspection-only marker/defect meshes are allowed).");
        }
    }

    static void ValidateSameMeshSet(
        ValidationContext context,
        string packageName,
        string firstPath,
        string secondPath)
    {
        GameObject first = context.Require(firstPath);
        GameObject second = context.Require(secondPath);
        if (first == null || second == null)
            return;

        HashSet<string> firstMeshes = GetExternalMeshKeys(first);
        HashSet<string> secondMeshes = GetExternalMeshKeys(second);
        if (firstMeshes.Count == 0 || !firstMeshes.SetEquals(secondMeshes))
        {
            context.Fail(
                $"'{firstPath}' and '{secondPath}' must remain instances/usages of one {packageName} mesh package.");
            return;
        }

        context.Note($"Package {packageName}: scene use count=2, submitted Prefab count=1.");
    }

    static void AppendPrefabStatus(ValidationContext context)
    {
        int prefabInstanceCount = ExpectedPaths
            .Select(context.Find)
            .Where(gameObject => gameObject != null && PrefabUtility.IsPartOfPrefabInstance(gameObject))
            .Count();
        context.Note(
            prefabInstanceCount == 0
                ? "Prefab phase pending: all validated objects are currently scene-authored objects."
                : $"Prefab-linked validated scene objects: {prefabInstanceCount}/{ExpectedPaths.Length}.");
    }

    static HashSet<string> GetExternalMeshKeys(GameObject root)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        IEnumerable<Mesh> meshes = root.GetComponentsInChildren<MeshFilter>(true)
            .Select(filter => filter.sharedMesh)
            .Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => renderer.sharedMesh));

        foreach (Mesh mesh in meshes.Where(mesh => mesh != null))
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId) ||
                string.IsNullOrEmpty(guid))
            {
                continue;
            }

            keys.Add(guid + ":" + localId);
        }

        return keys;
    }

    static bool HasPathSegment(string path, string segment)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        return path.Split('/').Any(part => part == segment);
    }

    static string MakeProjectRelative(string projectRoot, string absolutePath)
    {
        string normalizedRoot = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return absolutePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? absolutePath.Substring(normalizedRoot.Length).Replace('\\', '/')
            : absolutePath;
    }

    static string GetPath(Transform transform)
    {
        List<string> names = new List<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    sealed class ValidationContext
    {
        readonly Dictionary<string, GameObject> objectsByPath;

        public ValidationContext(Scene scene)
        {
            Scene = scene;
            AllTransforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();
            objectsByPath = AllTransforms
                .GroupBy(GetPath, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().gameObject, StringComparer.Ordinal);
        }

        public Scene Scene { get; }
        public Transform[] AllTransforms { get; }
        public List<string> Failures { get; } = new List<string>();
        public List<string> Notes { get; } = new List<string>();

        public GameObject Find(string path)
        {
            objectsByPath.TryGetValue(path, out GameObject result);
            return result;
        }

        public GameObject Require(string path)
        {
            GameObject result = Find(path);
            if (result == null)
                Fail($"Required object path is missing: '{path}'.");
            return result;
        }

        public void Fail(string message)
        {
            if (!Failures.Contains(message))
                Failures.Add(message);
        }

        public void Note(string message)
        {
            if (!Notes.Contains(message))
                Notes.Add(message);
        }
    }

    sealed class InspectionExpectation
    {
        public InspectionExpectation(string inspectionPath, PPEItemType itemType, string equippedPath)
        {
            InspectionPath = inspectionPath;
            ItemType = itemType;
            EquippedPath = equippedPath;
        }

        public string InspectionPath { get; }
        public PPEItemType ItemType { get; }
        public string EquippedPath { get; }
    }

    sealed class EquippedExpectation
    {
        public EquippedExpectation(PPEItemType itemType, string path)
        {
            ItemType = itemType;
            Path = path;
        }

        public PPEItemType ItemType { get; }
        public string Path { get; }
    }
}
