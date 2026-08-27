using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PPEHelmetEquipValidation
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string MirrorOnlyLayerName = "Mirror Only";

    [MenuItem("Tools/PPE/Validate Helmet Equip (HandTest Scale)")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before validating the Helmet setup.");

        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        PPEHelmetEquipController[] equipControllers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEHelmetEquipController>(true))
            .ToArray();
        if (equipControllers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Helmet equip controller, found {equipControllers.Length}.");
        }

        PPEHelmetEquipController equipController = equipControllers[0];
        PPEItemPresentationBinding binding = equipController.PresentationBinding;
        PPEActionPanelController actionPanel = equipController.ActionPanelController;
        if (equipController.gameObject.name != "helmet_on" ||
            binding == null ||
            binding.ItemIdentity == null ||
            binding.ItemIdentity.ItemType != PPEItemType.ConstructionHelmet ||
            binding.InspectionVisual == null ||
            binding.InspectionVisual.name != "helmet" ||
            binding.EquippedVisual != equipController.gameObject ||
            actionPanel == null ||
            actionPanel.gameObject != binding.InspectionVisual ||
            actionPanel.InspectionState == null ||
            actionPanel.InspectionState.PresentationBinding != binding)
        {
            throw new InvalidOperationException(
                "Helmet inspection, shared action-panel, and equipped-visual role references are invalid.");
        }

        if (!binding.InspectionVisual.activeSelf ||
            !binding.EquippedVisual.activeSelf ||
            binding.InitialCondition != PPEItemCondition.Contaminated)
        {
            throw new InvalidOperationException(
                "Helmet inspection/equipped hosts must be authored active and start Contaminated.");
        }

        PPEInspectionState inspectionState = actionPanel.InspectionState;
        XRGrabInteractable grabInteractable = binding.InspectionVisual.GetComponent<XRGrabInteractable>();
        PPEMarkerToggleGrab toggleGrab = binding.InspectionVisual.GetComponent<PPEMarkerToggleGrab>();
        SphereCollider markerCollider = binding.InspectionVisual
            .GetComponentsInChildren<SphereCollider>(true)
            .FirstOrDefault(candidate => candidate.gameObject.name == "XR Item Marker_small");
        if (inspectionState.GrabInteractable != grabInteractable ||
            grabInteractable == null ||
            toggleGrab == null ||
            !toggleGrab.UseToggleGrip ||
            !toggleGrab.ReturnToAuthoredPoseOnRelease ||
            !toggleGrab.HoldHandGripWhileSelected ||
            markerCollider == null ||
            !grabInteractable.colliders.Contains(markerCollider))
        {
            throw new InvalidOperationException(
                "Helmet Toggle Grab, authored return, hand Grip, or marker-collider setup is invalid.");
        }

        PPEColorConditionAppearance appearance =
            binding.InspectionVisual.GetComponent<PPEColorConditionAppearance>();
        Renderer[] modelRenderers = binding.InspectionVisual
            .GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !renderer.gameObject.name.StartsWith("XR Item Marker", StringComparison.Ordinal))
            .ToArray();
        if (appearance == null ||
            appearance.InspectionState != inspectionState ||
            appearance.TargetRenderers == null ||
            appearance.TargetRenderers.Length != modelRenderers.Length ||
            appearance.TargetRenderers.Distinct().Count() != modelRenderers.Length ||
            modelRenderers.Any(renderer => !appearance.TargetRenderers.Contains(renderer)) ||
            appearance.ColorProperty != "_BaseColor" ||
            appearance.CleanColor != Color.white ||
            appearance.ContaminatedColor == appearance.CleanColor ||
            modelRenderers.Any(renderer =>
                renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(appearance.ColorProperty)))
        {
            throw new InvalidOperationException(
                "Helmet Clean/Contaminated color appearance references are incomplete.");
        }

        if (actionPanel.PanelRoot == null ||
            actionPanel.PanelPose == null ||
            !actionPanel.PanelPose.IsChildOf(binding.InspectionVisual.transform) ||
            actionPanel.ItemNameLabel == null ||
            actionPanel.ItemDisplayName != "안전모" ||
            actionPanel.UseButton == null ||
            actionPanel.DiscardButton == null)
        {
            throw new InvalidOperationException(
                "Helmet shared action-panel UI, authored pose, or item-label references are incomplete.");
        }

        int mirrorOnlyLayer = LayerMask.NameToLayer(MirrorOnlyLayerName);
        PlanarMirrorRenderer mirror = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlanarMirrorRenderer>(true))
            .SingleOrDefault();
        Camera[] hmdCameras = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .Where(camera => camera.targetTexture == null)
            .ToArray();
        if (mirrorOnlyLayer < 0 ||
            equipController.MirrorOnlyLayer != mirrorOnlyLayer ||
            equipController.gameObject.layer != mirrorOnlyLayer ||
            equipController.HeadTransform == null ||
            equipController.HeadTransform.GetComponent<Camera>() == null ||
            equipController.transform.parent != equipController.HeadTransform ||
            equipController.ApproachAnchor == null ||
            equipController.ApproachAnchor.parent != equipController.HeadTransform ||
            mirror == null ||
            (mirror.ReflectedLayers.value & (1 << mirrorOnlyLayer)) == 0 ||
            hmdCameras.Any(camera => (camera.cullingMask & (1 << mirrorOnlyLayer)) != 0))
        {
            throw new InvalidOperationException(
                "Helmet mirror-only layer, HMD camera exclusion, mirror inclusion, or head anchors are invalid.");
        }

        string equippedPrefabPath = equipController.EquippedVisualPrefab != null
            ? AssetDatabase.GetAssetPath(equipController.EquippedVisualPrefab)
            : string.Empty;
        bool hasValidEquippedPrefab =
            equipController.EquippedVisualPrefab != null &&
            !string.IsNullOrEmpty(equippedPrefabPath) &&
            PrefabUtility.IsPartOfPrefabAsset(equipController.EquippedVisualPrefab) &&
            equipController.EquippedVisualPrefab.GetComponentsInChildren<Renderer>(true).Length > 0;
        AnimationCurve motionCurve = equipController.MotionCurve;
        float curveStart = motionCurve != null && motionCurve.length > 0
            ? motionCurve.Evaluate(0f)
            : float.NaN;
        float curveEnd = motionCurve != null && motionCurve.length > 0
            ? motionCurve.Evaluate(1f)
            : float.NaN;

        var motionFailures = new System.Collections.Generic.List<string>();
        if (!hasValidEquippedPrefab)
        {
            motionFailures.Add(
                $"equippedVisualPrefab is not a valid rendered prefab asset; path='{equippedPrefabPath}'");
        }
        if (equipController.AnimationDuration <= 0f)
            motionFailures.Add($"animationDuration={equipController.AnimationDuration}");
        if (equipController.ApproachPhaseEnd <= 0f || equipController.ApproachPhaseEnd >= 1f)
            motionFailures.Add($"approachPhaseEnd={equipController.ApproachPhaseEnd}");
        if (motionCurve == null || motionCurve.length < 2)
            motionFailures.Add($"motionCurve.length={motionCurve?.length ?? 0}");
        if (!Mathf.Approximately(curveStart, 0f))
            motionFailures.Add($"motionCurve(0)={curveStart:R}");
        if (!Mathf.Approximately(curveEnd, 1f))
            motionFailures.Add($"motionCurve(1)={curveEnd:R}");

        if (motionFailures.Count > 0)
        {
            throw new InvalidOperationException(
                "Helmet equipped prefab, animation timing, or motion curve is invalid:\n- " +
                string.Join("\n- ", motionFailures));
        }

        if (equipController.RuntimeEquippedVisual != null ||
            equipController.IsAnimating ||
            equipController.IsEquipped)
        {
            throw new InvalidOperationException(
                "Edit Mode validation found unexpected runtime Helmet equip state.");
        }

        if (logSuccess)
        {
            Debug.Log(
                $"Helmet equip validation passed: inspection={binding.InspectionVisual.name}, " +
                $"renderers={modelRenderers.Length}, equipLocalPosition={equipController.transform.localPosition}, " +
                $"mirrorLayer={MirrorOnlyLayerName}.",
                equipController);
        }
    }

    static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before validating. Current scene: '{scene.path}'.");
        }

        return scene;
    }
}
