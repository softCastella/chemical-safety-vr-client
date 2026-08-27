using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Locks the scale-0 fully equipped PPE model to the authored pose captured from
/// 3_PPE_Room_HandTest_scale_Orig. Changes are made only by the explicit restore command.
/// </summary>
public static class PPEFullSuitFloorGrounding
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string XrOriginName = "XR Origin (VR)";
    const string BodyAnchorName = "PPE Body Anchor";
    const string SuitName = "PPE_A_SuitWear";
    const float Tolerance = 0.000001f;
    const float ExpectedBodyYawFollowSmoothTime = 0.2f;
    const float ExpectedHandSwapFadeDuration = 0.4f;
    const float ExpectedAnimationDuration = 1.8f;
    const float ExpectedApproachPhaseEnd = 0.65f;

    static readonly string[] ExpectedBareHandNames =
    {
        "PPE_A_Hand_Bare_L",
        "PPE_A_Hand_Bare_R",
    };

    static readonly string[] ExpectedSuitHandNames =
    {
        "PPE_A_Hand_Suit_L",
        "PPE_A_Hand_Suit_R",
    };

    static readonly BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    sealed class PoseBaseline
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public PoseBaseline(string name, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }

    static readonly PoseBaseline BodyAnchorPose = new(
        BodyAnchorName,
        new Vector3(0.37f, 0.06f, 8.994f),
        new Quaternion(0f, 0f, 0f, 1f),
        Vector3.one);

    static readonly PoseBaseline SuitPose = new(
        SuitName,
        new Vector3(-0.017f, 0.054f, -0.169f),
        new Quaternion(0.7067602f, 0.018519262f, 0.018576182f, -0.70696676f),
        new Vector3(2f, 1.7718132f, 1.649093f));

    static readonly PoseBaseline[] ChildPoses =
    {
        new("PPE_A_Taped_Hand_R", new Vector3(0.20414f, -0.05606f, 0.41306f), new Quaternion(-0.5500253f, -0.52963907f, -0.46626252f, 0.44671467f), new Vector3(0.05f, 0.05157907f, 0.055391397f)),
        new("PPE_A_Taped_Hand_L", new Vector3(-0.2055f, -0.0598f, 0.4157f), new Quaternion(-0.5500253f, -0.52963907f, -0.46626252f, 0.44671467f), new Vector3(0.05f, 0.05157907f, 0.055391397f)),
        new("PPE_A_Taped_Boot_R", new Vector3(0.0838f, -0.02889f, -0.0079f), new Quaternion(-0.51953626f, -0.50042284f, -0.50001067f, 0.4792165f), new Vector3(0.05f, 0.05157907f, 0.07912434f)),
        new("PPE_A_Taped_Boot_L", new Vector3(-0.0758f, -0.026f, -0.0088f), new Quaternion(-0.50652754f, -0.48795435f, -0.5131846f, 0.4919065f), new Vector3(0.05f, 0.05157907f, 0.083398044f)),
        new("PPE_A_Boots_R", new Vector3(0.086f, -0.02f, -0.093f), new Quaternion(-0.7010076f, -0.09283005f, -0.09952246f, -0.7000473f), new Vector3(0.1949511f, 0.223074f, 0.21620974f)),
        new("PPE_A_Boots_L", new Vector3(-0.076f, -0.016f, -0.097f), new Quaternion(-0.68070924f, -0.19140315f, -0.19140317f, -0.68070894f), new Vector3(0.19951603f, 0.22307688f, 0.21085651f)),
        new("PPE_A_Backplate", new Vector3(0.0045729745f, 0.009892634f, 0.37003228f), new Quaternion(-0.69796884f, 0.015236208f, 0.0034383587f, -0.7159578f), new Vector3(0.41503555f, 0.4613414f, 0.4515035f)),
        new("PPE_A_Mask", new Vector3(-0.0026f, -0.035f, 0.784f), new Quaternion(0.0640864f, -0.0074455794f, 0.006044979f, -0.9978983f), new Vector3(0.15339154f, 0.1730066f, 0.17692311f)),
        new("PPE_A_Helmet_Strap", new Vector3(-0.0054f, 0.0118f, 0.7728f), new Quaternion(-0.5379912f, 0.45889658f, 0.45871624f, -0.5381067f), new Vector3(0.25f, 0.25596413f, 0.22215451f)),
        new("PPE_A_Glove_R", new Vector3(0.197f, -0.012f, 0.477f), new Quaternion(-0.9805511f, -0.16660543f, 0.018463189f, -0.10208501f), new Vector3(0.16747871f, 0.18710709f, 0.1923077f)),
        new("PPE_A_Glove_L", new Vector3(-0.188f, -0.019f, 0.477f), new Quaternion(-0.94828975f, 0.29241586f, 0.035942852f, -0.11810087f), new Vector3(0.16666898f, 0.18812819f, 0.1923077f)),
    };

    [MenuItem("Tools/PPE/Full Suit Pose/Restore Orig Baseline (Scale 0)")]
    [MenuItem("Tools/PPE/Ground Full Suit To Floor (Scale 0)")]
    public static void GroundFullSuitToFloor()
    {
        EnsureEditMode();
        Scene scene = RequireOpenScale0();
        ResolveHierarchy(scene, out Transform xrOrigin, out Transform bodyAnchor, out Transform suit);

        Undo.SetCurrentGroupName("Restore full suit Orig baseline");
        int undoGroup = Undo.GetCurrentGroup();

        RestoreParentAndPose(bodyAnchor, xrOrigin, BodyAnchorPose);
        RestoreParentAndPose(suit, bodyAnchor, SuitPose);
        foreach (PoseBaseline baseline in ChildPoses)
        {
            Transform child = RequireDirectChild(suit, baseline.Name);
            RestoreParentAndPose(child, suit, baseline);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        ValidateInternal(scene);
        Selection.activeTransform = suit;
        Debug.Log("[PPE Full Suit Pose] _Orig baseline restored and validated.", suit);
    }

    [MenuItem("Tools/PPE/Full Suit Pose/Validate Orig Baseline (Scale 0)")]
    [MenuItem("Tools/PPE/Validate Full Suit Floor Clearance (Scale 0)")]
    public static void Validate()
    {
        EnsureEditMode();
        Scene scene = RequireOpenScale0();
        Transform suit = ValidateInternal(scene);
        Debug.Log("[PPE Full Suit Pose] _Orig baseline validation passed.", suit);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateInternal(scene);
    }

    static Transform ValidateInternal(Scene scene)
    {
        ResolveHierarchy(scene, out Transform xrOrigin, out Transform bodyAnchor, out Transform suit);
        ValidateParentAndPose(bodyAnchor, xrOrigin, BodyAnchorPose);
        ValidateParentAndPose(suit, bodyAnchor, SuitPose);
        foreach (PoseBaseline baseline in ChildPoses)
            ValidateParentAndPose(RequireDirectChild(suit, baseline.Name), suit, baseline);
        ValidateControllerConfiguration(suit, bodyAnchor);
        ValidateRuntimeRegressionScenarios();
        return suit;
    }

    static void ValidateControllerConfiguration(Transform suit, Transform bodyAnchor)
    {
        PPEHazmatEquipController controller = suit.GetComponent<PPEHazmatEquipController>();
        Require(controller != null, $"{SuitName} is missing PPEHazmatEquipController.");
        Require(controller.enabled, "PPEHazmatEquipController must be authored enabled.");
        Require(controller.ActionPanelController != null, "Hazmat action panel reference is missing.");
        Require(controller.PresentationBinding != null, "Hazmat presentation binding is missing.");
        Require(
            controller.PresentationBinding.InspectionVisual != null,
            "Hazmat inspection visual reference is missing.");
        Require(
            controller.PresentationBinding.EquippedVisual == suit.gameObject,
            "Hazmat equipped visual must reference PPE_A_SuitWear itself.");
        Require(controller.HeadTransform != null, "Hazmat head transform reference is missing.");
        Require(controller.BodyAnchor == bodyAnchor, "Hazmat body anchor reference is incorrect.");
        Require(
            controller.ApproachAnchor != null && controller.ApproachAnchor.parent == bodyAnchor,
            "Hazmat approach anchor must be a direct child of PPE Body Anchor.");
        Require(
            controller.FrontStartAnchor != null && controller.FrontStartAnchor.parent == bodyAnchor,
            "Hazmat front-start anchor must be a direct child of PPE Body Anchor.");

        Require(
            Approximately(controller.BodyYawFollowSmoothTime, ExpectedBodyYawFollowSmoothTime),
            $"bodyYawFollowSmoothTime must remain {ExpectedBodyYawFollowSmoothTime}.");
        Require(
            Approximately(controller.HandSwapFadeDuration, ExpectedHandSwapFadeDuration),
            $"handSwapFadeDuration must remain {ExpectedHandSwapFadeDuration}.");
        Require(
            Approximately(controller.AnimationDuration, ExpectedAnimationDuration),
            $"animationDuration must remain {ExpectedAnimationDuration}.");
        Require(controller.UseUnscaledTime, "Hazmat animation must keep useUnscaledTime enabled.");
        Require(
            Approximately(controller.ApproachPhaseEnd, ExpectedApproachPhaseEnd),
            $"approachPhaseEnd must remain {ExpectedApproachPhaseEnd}.");

        ValidateMotionCurve(controller.MotionCurve);
        ValidateRendererReferences(controller, suit);
        ValidateHandReferences(
            controller.UnequippedHandModels,
            ExpectedBareHandNames,
            "unequipped hand");
        ValidateHandReferences(
            controller.EquippedHandModels,
            ExpectedSuitHandNames,
            "equipped hand");
    }

    static void ValidateMotionCurve(AnimationCurve curve)
    {
        Require(curve != null, "Hazmat motionCurve is missing.");
        Require(curve.length == 2, "Hazmat motionCurve must contain exactly two keys.");

        Keyframe start = curve.keys[0];
        Keyframe end = curve.keys[1];
        Require(
            Approximately(start.time, 0f) &&
            Approximately(start.value, 0f) &&
            Approximately(start.inTangent, 0f) &&
            Approximately(start.outTangent, 0f) &&
            Approximately(start.inWeight, 0f) &&
            Approximately(start.outWeight, 0.33333334f) &&
            start.weightedMode == WeightedMode.None,
            "Hazmat motionCurve start key differs from the authored baseline.");
        Require(
            Approximately(end.time, 1f) &&
            Approximately(end.value, 1f) &&
            Approximately(end.inTangent, 0f) &&
            Approximately(end.outTangent, 0f) &&
            Approximately(end.inWeight, 0.33333334f) &&
            Approximately(end.outWeight, 0f) &&
            end.weightedMode == WeightedMode.None,
            "Hazmat motionCurve end key differs from the authored baseline.");
        Require(
            curve.preWrapMode == WrapMode.Loop && curve.postWrapMode == WrapMode.Loop,
            "Hazmat motionCurve pre/post wrap modes must remain Loop.");
    }

    static void ValidateRendererReferences(
        PPEHazmatEquipController controller,
        Transform suit)
    {
        Renderer[] renderers = controller.EquippedRenderers;
        Require(renderers != null && renderers.Length == 1,
            "Hazmat equippedRenderers must contain exactly one renderer.");
        Require(renderers[0] != null, "Hazmat equipped renderer reference is missing.");
        Require(
            renderers[0].transform == suit || renderers[0].transform.IsChildOf(suit),
            "Hazmat equipped renderer must belong to PPE_A_SuitWear.");
        Require(renderers[0].enabled, "Hazmat equipped renderer must remain authored enabled.");
    }

    static void ValidateHandReferences(
        GameObject[] actual,
        string[] expectedNames,
        string label)
    {
        Require(
            actual != null && actual.Length == expectedNames.Length,
            $"Hazmat {label} references must contain exactly {expectedNames.Length} entries.");

        for (int index = 0; index < expectedNames.Length; index++)
        {
            Require(actual[index] != null, $"Hazmat {label}[{index}] reference is missing.");
            Require(
                actual[index].name == expectedNames[index],
                $"Hazmat {label}[{index}] is '{actual[index].name}', expected '{expectedNames[index]}'.");
        }

        Require(actual[0] != actual[1], $"Hazmat {label} references must be distinct.");
    }

    static void ValidateRuntimeRegressionScenarios()
    {
        GameObject root = CreateProbeObject("[PPE Full Suit Harness Probe]");
        try
        {
            GameObject bodyObject = CreateProbeObject("PPE Body Anchor Probe", root.transform);
            GameObject suitObject = CreateProbeObject("PPE Suit Probe", bodyObject.transform);
            GameObject headObject = CreateProbeObject("HMD Probe", root.transform);
            PPEHazmatEquipController controller =
                suitObject.AddComponent<PPEHazmatEquipController>();

            Vector3 authoredBodyPosition = new(0.37f, 0.06f, 8.994f);
            Quaternion authoredBodyRotation = Quaternion.Euler(0f, 17f, 0f);
            bodyObject.transform.SetLocalPositionAndRotation(
                authoredBodyPosition,
                authoredBodyRotation);
            headObject.transform.position = new Vector3(100f, 2f, -100f);

            SetPrivateField(controller, "bodyAnchor", bodyObject.transform);
            SetPrivateField(controller, "headTransform", headObject.transform);
            SetPrivateField(controller, "runtimeInitialized", true);
            InvokePrivate(controller, "LateUpdate");

            Require(
                Approximately(bodyObject.transform.localPosition, authoredBodyPosition) &&
                Approximately(bodyObject.transform.localRotation, authoredBodyRotation),
                "Regression: PPE Body Anchor moved before UseApproved/equip state.");

            SetPrivateField(controller, "authoredBodyAnchorLocalPosition", authoredBodyPosition);
            SetPrivateField(controller, "authoredBodyAnchorLocalRotation", authoredBodyRotation);
            SetPrivateField(controller, "hasAuthoredBodyAnchorPosition", true);
            bodyObject.transform.SetLocalPositionAndRotation(
                new Vector3(-50f, -50f, -50f),
                Quaternion.Euler(90f, 90f, 90f));
            InvokePrivate(controller, "OnDisable");

            Require(
                Approximately(bodyObject.transform.localPosition, authoredBodyPosition) &&
                Approximately(bodyObject.transform.localRotation, authoredBodyRotation),
                "Regression: disabling PPEHazmatEquipController did not restore the authored Body Anchor pose.");

            GameObject bareLeft = CreateProbeObject("Bare L Probe", root.transform);
            GameObject bareRight = CreateProbeObject("Bare R Probe", root.transform);
            GameObject suitLeft = CreateProbeObject("Suit L Probe", root.transform);
            GameObject suitRight = CreateProbeObject("Suit R Probe", root.transform);
            SetPrivateField(controller, "unequippedHandModels", new[] { bareLeft, bareRight });
            SetPrivateField(controller, "equippedHandModels", new[] { suitLeft, suitRight });

            bareLeft.SetActive(false);
            bareRight.SetActive(false);
            suitLeft.SetActive(false);
            suitRight.SetActive(false);
            Require(
                InvokePrivate<bool>(controller, "HasCompleteHandModelReferences"),
                "Regression: inactive controller-guide hands were treated as missing references.");

            bareLeft.SetActive(true);
            bareRight.SetActive(true);
            suitLeft.SetActive(true);
            suitRight.SetActive(true);
            Require(
                InvokePrivate<bool>(controller, "HasCompleteHandModelReferences"),
                "Regression: hand activeSelf state incorrectly affects reference validation.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static GameObject CreateProbeObject(string name, Transform parent = null)
    {
        GameObject probe = new(name)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        if (parent != null)
            probe.transform.SetParent(parent, false);
        return probe;
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
        if (field == null)
            throw new InvalidOperationException($"Missing regression field: {fieldName}.");
        field.SetValue(target, value);
    }

    static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
        if (method == null)
            throw new InvalidOperationException($"Missing regression method: {methodName}.");
        method.Invoke(target, null);
    }

    static T InvokePrivate<T>(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
        if (method == null)
            throw new InvalidOperationException($"Missing regression method: {methodName}.");
        return (T)method.Invoke(target, null);
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    static void ResolveHierarchy(
        Scene scene,
        out Transform xrOrigin,
        out Transform bodyAnchor,
        out Transform suit)
    {
        GameObject xrRoot = Array.Find(scene.GetRootGameObjects(), root => root.name == XrOriginName);
        xrOrigin = xrRoot != null ? xrRoot.transform : null;
        bodyAnchor = xrOrigin != null ? xrOrigin.Find(BodyAnchorName) : null;
        suit = bodyAnchor != null ? bodyAnchor.Find(SuitName) : null;
        if (xrOrigin == null || bodyAnchor == null || suit == null)
        {
            throw new InvalidOperationException(
                $"Required hierarchy is missing: {XrOriginName}/{BodyAnchorName}/{SuitName}.");
        }
    }

    static Transform RequireDirectChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null || child.parent != parent)
            throw new InvalidOperationException($"Missing direct child: {parent.name}/{childName}.");
        return child;
    }

    static void RestoreParentAndPose(Transform target, Transform expectedParent, PoseBaseline baseline)
    {
        if (target.parent != expectedParent)
            Undo.SetTransformParent(target, expectedParent, $"Restore {baseline.Name} parent");

        Undo.RecordObject(target, $"Restore {baseline.Name} pose");
        target.localPosition = baseline.Position;
        target.localRotation = baseline.Rotation;
        target.localScale = baseline.Scale;
        EditorUtility.SetDirty(target);
    }

    static void ValidateParentAndPose(
        Transform target,
        Transform expectedParent,
        PoseBaseline baseline)
    {
        List<string> differences = new();
        if (target.parent != expectedParent)
            differences.Add($"parent={target.parent?.name ?? "<null>"}");
        if (!Approximately(target.localPosition, baseline.Position))
            differences.Add($"localPosition={target.localPosition}");
        if (!Approximately(target.localRotation, baseline.Rotation))
            differences.Add($"localRotation={target.localRotation}");
        if (!Approximately(target.localScale, baseline.Scale))
            differences.Add($"localScale={target.localScale}");

        if (differences.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{baseline.Name}' differs from the _Orig full-suit baseline: " +
                string.Join(", ", differences));
        }
    }

    static bool Approximately(Vector3 actual, Vector3 expected)
    {
        return Mathf.Abs(actual.x - expected.x) <= Tolerance
            && Mathf.Abs(actual.y - expected.y) <= Tolerance
            && Mathf.Abs(actual.z - expected.z) <= Tolerance;
    }

    static bool Approximately(float actual, float expected)
    {
        return Mathf.Abs(actual - expected) <= Tolerance;
    }

    static bool Approximately(Quaternion actual, Quaternion expected)
    {
        return Mathf.Abs(actual.x - expected.x) <= Tolerance
            && Mathf.Abs(actual.y - expected.y) <= Tolerance
            && Mathf.Abs(actual.z - expected.z) <= Tolerance
            && Mathf.Abs(actual.w - expected.w) <= Tolerance;
    }

    static void EnsureEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before validating or restoring the full-suit pose.");
    }

    static Scene RequireOpenScale0()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' first. Current scene: '{scene.path}'.");
        }

        return scene;
    }
}
