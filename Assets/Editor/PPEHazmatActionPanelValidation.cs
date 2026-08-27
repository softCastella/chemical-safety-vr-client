using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class PPEHazmatActionPanelValidation
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";

    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Exit Play Mode before validating the authored Hazmat action panel.");
        }

        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateEquipStage1()
    {
        PPEHazmatEquipValidation.Validate();
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        PPEActionPanelController[] controllers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEActionPanelController>(true))
            .ToArray();

        PPEActionPanelController[] hazmatControllers = controllers
            .Where(candidate =>
                candidate.InspectionState != null &&
                candidate.InspectionState.PresentationBinding != null &&
                candidate.InspectionState.PresentationBinding.ItemIdentity != null &&
                candidate.InspectionState.PresentationBinding.ItemIdentity.ItemType ==
                    PPEItemType.HazmatSuit)
            .ToArray();
        if (hazmatControllers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Hazmat action panel controller, found {hazmatControllers.Length}.");
        }

        PPEActionPanelController controller = hazmatControllers[0];
        PPEInspectionState inspectionState = controller.InspectionState;
        if (inspectionState == null ||
            inspectionState.gameObject != controller.gameObject ||
            inspectionState.PresentationBinding == null ||
            inspectionState.PresentationBinding.ItemIdentity == null ||
            inspectionState.PresentationBinding.ItemIdentity.ItemType != PPEItemType.HazmatSuit)
        {
            throw new InvalidOperationException(
                "The action panel must be serialized on the HazmatSuit inspection object.");
        }

        if (!inspectionState.gameObject.activeSelf ||
            !inspectionState.PresentationBinding.InspectionVisual.activeSelf)
        {
            throw new InvalidOperationException(
                "hazmat_suit_off and its inspection visual must remain authored active.");
        }

        if (inspectionState.PresentationBinding.InitialCondition !=
            PPEItemCondition.Contaminated)
        {
            throw new InvalidOperationException(
                "hazmat_suit_off must start Contaminated before the discard-to-Clean flow.");
        }

        if (controller.PanelRoot == null ||
            controller.PanelPose != controller.PanelRoot.transform ||
            controller.ItemNameLabel == null ||
            controller.ItemDisplayName != "방호복" ||
            controller.UseButton == null ||
            controller.DiscardButton == null ||
            controller.FeedbackRoot == null ||
            controller.FeedbackLabel == null ||
            controller.UsePassIconRoot == null ||
            controller.UseErrorIconRoot == null ||
            controller.DiscardPassIconRoot == null ||
            controller.DiscardErrorIconRoot == null)
        {
            throw new InvalidOperationException(
                "The Hazmat action panel has incomplete shared UI, pose, or item-label references.");
        }

        if (controller.PanelRoot.activeSelf)
        {
            throw new InvalidOperationException(
                "The authored Hazmat action panel visual must start inactive.");
        }

        if (!controller.PanelRoot.transform.IsChildOf(inspectionState.transform))
        {
            throw new InvalidOperationException(
                "The Hazmat action panel visual must be authored under hazmat_suit_off.");
        }

        Canvas canvas = controller.PanelRoot.GetComponent<Canvas>();
        TrackedDeviceGraphicRaycaster trackedRaycaster =
            controller.PanelRoot.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (canvas == null ||
            canvas.renderMode != RenderMode.WorldSpace ||
            trackedRaycaster == null)
        {
            throw new InvalidOperationException(
                "The Hazmat action panel requires a World Space Canvas and tracked-device raycaster.");
        }

        if (controller.UseButton == controller.DiscardButton ||
            !controller.UseButton.transform.IsChildOf(controller.PanelRoot.transform) ||
            !controller.DiscardButton.transform.IsChildOf(controller.PanelRoot.transform) ||
            !controller.UseButton.gameObject.activeSelf ||
            !controller.DiscardButton.gameObject.activeSelf)
        {
            throw new InvalidOperationException(
                "The visible Use and Discard buttons must be active, distinct panel children.");
        }

        if (controller.EnableInspectChoice)
        {
            throw new InvalidOperationException(
                "Hazmat must keep Inspect (확인하기) disabled; only the mask enables that choice.");
        }

        if (controller.InspectButton != null && controller.InspectButton.gameObject.activeSelf)
        {
            throw new InvalidOperationException(
                "The shared Inspect Button must start inactive for Hazmat (mask enables it at runtime).");
        }

        GameObject[] statusIcons =
        {
            controller.UsePassIconRoot,
            controller.UseErrorIconRoot,
            controller.DiscardPassIconRoot,
            controller.DiscardErrorIconRoot
        };
        if (statusIcons.Distinct().Count() != statusIcons.Length ||
            statusIcons.Any(icon =>
                !icon.transform.IsChildOf(controller.PanelRoot.transform) || icon.activeSelf))
        {
            throw new InvalidOperationException(
                "The four distinct panel-result icons must start inactive.");
        }

        TMP_Text[] labels = controller.PanelRoot.GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length < 5 || labels.Any(label => label.font == null))
        {
            throw new InvalidOperationException(
                "The Hazmat action panel labels or Korean font references are incomplete.");
        }

        EventSystem[] eventSystems = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
            .ToArray();
        if (eventSystems.Length != 1 ||
            eventSystems[0].GetComponent<XRUIInputModule>() == null)
        {
            throw new InvalidOperationException(
                "The scene must contain exactly one EventSystem with XRUIInputModule.");
        }

        XRBaseInputInteractor[] inputInteractors = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<XRBaseInputInteractor>(true))
            .ToArray();
        if (!inputInteractors.Any(interactor =>
                interactor.handedness == InteractorHandedness.Left) ||
            !inputInteractors.Any(interactor =>
                interactor.handedness == InteractorHandedness.Right))
        {
            throw new InvalidOperationException(
                "The scene requires Left and Right input interactors for Trigger/A actions.");
        }

        if (!Mathf.Approximately(controller.ShowDelaySeconds, 0f))
        {
            throw new InvalidOperationException(
                "The Hazmat action panel must appear immediately when inspection starts (delay 0).");
        }

        if (controller.ApprovedRemovalDelaySeconds < 0f)
            throw new InvalidOperationException("The approved-result removal delay cannot be negative.");

        RectTransform panelRect = controller.PanelRoot.transform as RectTransform;
        RectTransform useButtonRect = controller.UseButton.transform as RectTransform;
        RectTransform discardButtonRect = controller.DiscardButton.transform as RectTransform;
        RectTransform usePassIconRect = controller.UsePassIconRoot.transform as RectTransform;
        RectTransform useErrorIconRect = controller.UseErrorIconRoot.transform as RectTransform;
        RectTransform discardPassIconRect =
            controller.DiscardPassIconRoot.transform as RectTransform;
        RectTransform discardErrorIconRect =
            controller.DiscardErrorIconRoot.transform as RectTransform;
        if (panelRect == null ||
            useButtonRect == null ||
            discardButtonRect == null ||
            panelRect.sizeDelta.y <= panelRect.sizeDelta.x ||
            useButtonRect.anchoredPosition.y <= discardButtonRect.anchoredPosition.y)
        {
            throw new InvalidOperationException(
                "The Hazmat action panel must use a portrait layout with Use above Discard.");
        }

        if (usePassIconRect == null ||
            useErrorIconRect == null ||
            discardPassIconRect == null ||
            discardErrorIconRect == null ||
            !Approximately(usePassIconRect.anchoredPosition.y, useErrorIconRect.anchoredPosition.y) ||
            !Approximately(
                discardPassIconRect.anchoredPosition.y,
                discardErrorIconRect.anchoredPosition.y))
        {
            throw new InvalidOperationException(
                "Each action row must have aligned Pass and Error icon RectTransforms.");
        }

        if (logSuccess)
        {
            Debug.Log(
                $"Hazmat action panel validation passed: item={inspectionState.name}, " +
                $"panel={controller.PanelRoot.name}, labels={labels.Length}, " +
                $"delay={controller.ShowDelaySeconds:0.###}s.",
                controller);
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

    static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= 0.5f;
    }
}
