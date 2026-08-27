using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHazmatEquipValidation
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";

    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException(
                "Exit Play Mode before validating the authored Hazmat equip configuration.");
        }

        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        PPEHazmatEquipController[] controllers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEHazmatEquipController>(true))
            .ToArray();
        if (controllers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Hazmat equip controller, found {controllers.Length}.");
        }

        PPEHazmatEquipController controller = controllers[0];
        PPEActionPanelController actionPanel = controller.ActionPanelController;
        PPEItemPresentationBinding binding = controller.PresentationBinding;
        if (controller.gameObject.name != "hazmat_suit_on" ||
            actionPanel == null ||
            binding == null ||
            binding.EquippedVisual != controller.gameObject ||
            actionPanel.InspectionState == null ||
            actionPanel.InspectionState.PresentationBinding != binding)
        {
            throw new InvalidOperationException(
                "The Hazmat equip controller must be authored on hazmat_suit_on and use the existing action-panel binding.");
        }

        if (binding.ItemIdentity == null ||
            binding.ItemIdentity.ItemType != PPEItemType.HazmatSuit ||
            binding.InspectionVisual == null ||
            binding.EquippedVisual == null ||
            !binding.InspectionVisual.activeSelf ||
            !binding.EquippedVisual.activeSelf)
        {
            throw new InvalidOperationException(
                "The inspection object and hazmat_suit_on host must remain authored active.");
        }

        Transform headParent = controller.HeadTransform != null
            ? controller.HeadTransform.parent
            : null;
        Transform xrOrigin = headParent != null ? headParent.parent : null;
        bool bodyAnchorParentOk =
            controller.BodyAnchor != null &&
            (controller.BodyAnchor.parent == xrOrigin ||
             controller.BodyAnchor.parent == headParent);

        if (controller.HeadTransform == null ||
            controller.HeadTransform.GetComponent<Camera>() == null ||
            controller.BodyAnchor == null ||
            controller.ApproachAnchor == null ||
            controller.FrontStartAnchor == null ||
            !bodyAnchorParentOk ||
            controller.ApproachAnchor.parent != controller.BodyAnchor ||
            controller.FrontStartAnchor.parent != controller.BodyAnchor ||
            controller.transform.parent != controller.BodyAnchor)
        {
            throw new InvalidOperationException(
                "hazmat_suit_on must be a direct child of the body anchor beside the authored approach anchor. " +
                "PPE Body Anchor should be under XR Origin (preferred) or Camera Offset.");
        }

        if (controller.FrontStartAnchor.localPosition.z -
            controller.transform.localPosition.z < 0.5f)
        {
            throw new InvalidOperationException(
                "The Hazmat front-start anchor must remain visibly in front of the final suit pose.");
        }

        if (controller.AnimationDuration <= 0f ||
            controller.BodyYawFollowSmoothTime < 0f ||
            controller.ApproachPhaseEnd <= 0f ||
            controller.ApproachPhaseEnd >= 1f ||
            controller.MotionCurve == null ||
            controller.MotionCurve.length < 2 ||
            !Mathf.Approximately(controller.MotionCurve.Evaluate(0f), 0f) ||
            !Mathf.Approximately(controller.MotionCurve.Evaluate(1f), 1f))
        {
            throw new InvalidOperationException(
                "Hazmat equip timing, approach phase, or authored motion curve is invalid.");
        }

        Vector3 bodyScale = controller.transform.lossyScale;
        if (bodyScale.x <= 0f || bodyScale.y <= 0f || bodyScale.z <= 0f)
        {
            throw new InvalidOperationException(
                "The authored Hazmat body scale must be positive on every axis.");
        }

        Renderer[] equippedRenderers = controller.EquippedRenderers;
        if (equippedRenderers == null ||
            equippedRenderers.Length == 0 ||
            equippedRenderers.Any(renderer =>
                renderer == null ||
                !renderer.enabled ||
                (renderer.transform != controller.transform &&
                    !renderer.transform.IsChildOf(controller.transform))))
        {
            throw new InvalidOperationException(
                "hazmat_suit_on requires serialized child Renderers visible for Edit Mode pose authoring.");
        }

        ValidateHandModelSwap(controller);

        if (controller.RuntimeEquippedVisual != null || controller.IsAnimating || controller.IsEquipped)
        {
            throw new InvalidOperationException(
                "Edit Mode validation found unexpected runtime Hazmat equip state.");
        }

        if (logSuccess)
        {
            Debug.Log(
                $"Hazmat equip stage 1 validation passed: duration={controller.AnimationDuration:0.###}s, " +
                $"equipLocalPosition={controller.transform.localPosition}, " +
                $"equipScale={controller.transform.localScale}.",
                controller);
        }
    }

    static void ValidateHandModelSwap(PPEHazmatEquipController controller)
    {
        GameObject[] unequippedHands = controller.UnequippedHandModels;
        GameObject[] equippedHands = controller.EquippedHandModels;
        if (unequippedHands == null ||
            equippedHands == null ||
            unequippedHands.Length != 2 ||
            equippedHands.Length != 2 ||
            unequippedHands.Concat(equippedHands).Any(hand => hand == null) ||
            unequippedHands.Concat(equippedHands).Distinct().Count() != 4)
        {
            throw new InvalidOperationException(
                "Hazmat equip requires two distinct unequipped and two distinct equipped hand models.");
        }

        string[] expectedUnequippedNames =
        {
            "PPE_A_Hand_Bare_L",
            "PPE_A_Hand_Bare_R"
        };
        string[] expectedEquippedNames =
        {
            "PPE_A_Hand_Suit_L",
            "PPE_A_Hand_Suit_R"
        };
        HandGripAnimator.HandSide[] expectedSides =
        {
            HandGripAnimator.HandSide.Left,
            HandGripAnimator.HandSide.Right
        };

        for (int index = 0; index < 2; index++)
        {
            GameObject unequipped = unequippedHands[index];
            GameObject equipped = equippedHands[index];
            HandGripAnimator unequippedAnimator = unequipped.GetComponent<HandGripAnimator>();
            HandGripAnimator equippedAnimator = equipped.GetComponent<HandGripAnimator>();

            if (unequipped.name != expectedUnequippedNames[index] ||
                equipped.name != expectedEquippedNames[index] ||
                !unequipped.activeSelf ||
                equipped.activeSelf ||
                unequipped.transform.parent != equipped.transform.parent ||
                (unequipped.transform.localPosition - equipped.transform.localPosition).sqrMagnitude > 0.000001f ||
                (unequipped.transform.localScale - equipped.transform.localScale).sqrMagnitude > 0.000001f ||
                Mathf.Abs(Quaternion.Dot(
                    unequipped.transform.localRotation,
                    equipped.transform.localRotation)) < 0.99999f ||
                unequippedAnimator == null ||
                equippedAnimator == null ||
                unequippedAnimator.Side != expectedSides[index] ||
                equippedAnimator.Side != expectedSides[index] ||
                unequipped.GetComponent<Animator>() == null ||
                equipped.GetComponent<Animator>() == null)
            {
                throw new InvalidOperationException(
                    $"Hazmat {expectedSides[index]} hand-model swap references or authored poses are invalid.");
            }
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
