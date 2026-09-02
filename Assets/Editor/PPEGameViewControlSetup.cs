using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicitly authors and validates the PC Game View navigation overlay for the
/// current PPE locomotion scene. Quest/OpenXR input and authored hand states are
/// intentionally left unchanged.
/// </summary>
public static class PPEGameViewControlSetup
{
    public const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";

    const float GameViewSpeedMultiplier = 2.4f;
    const float GameViewTurnSpeedDegrees = 120f;
    const float PhysicalHmdDetectionTimeout = 3f;
    const string MouseGrabAnchorName = "Game View Mouse Grab Anchor";
    const string ActiveCameraPath = "XR Origin (VR)/Camera Offset/Main Camera";

    static readonly string[] HandVisualPaths =
    {
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/LeftHand",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Bare_L",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_Suit_L",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_GloveSuit_L",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_InnerGloveSuit_L",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_InnerGlove_L",
        "XR Origin (VR)/Camera Offset/Left Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_L",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/RightHand",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Bare_R",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_Suit_R",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_GloveSuit_R",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_InnerGloveSuit_R",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_InnerGlove_R",
        "XR Origin (VR)/Camera Offset/Right Controller/Visual/Hand Offset/PPE_A_Hand_GloveTape_R",
    };

    [MenuItem("Tools/PPE/Game View/Apply Arrow Navigation and Hidden Hands")]
    public static void Apply()
    {
        Scene scene = RequireTargetScene();
        PhysicalHmdSimulatorGate gate = RequireSingle<PhysicalHmdSimulatorGate>(scene);
        PPEConfigurableDynamicMoveProvider move = RequireSingle<PPEConfigurableDynamicMoveProvider>(scene);
        PPEVoiceFlowDirector voiceFlow = RequireSingle<PPEVoiceFlowDirector>(scene);
        PPEHazmatEquipController hazmatEquip = RequireSingle<PPEHazmatEquipController>(scene);
        Dictionary<string, Transform> transforms = FindTransforms(scene);
        Transform mouseGrabAnchor = GetOrCreateMouseGrabAnchor(gate);
        if (!transforms.TryGetValue(ActiveCameraPath, out Transform activeCamera))
            throw new InvalidOperationException($"Missing authored active XR camera '{ActiveCameraPath}'.");

        Transform[] handRoots = new Transform[HandVisualPaths.Length];
        for (int index = 0; index < HandVisualPaths.Length; index++)
        {
            if (!transforms.TryGetValue(HandVisualPaths[index], out handRoots[index]))
                throw new InvalidOperationException($"Missing authored hand visual root '{HandVisualPaths[index]}'.");
        }

        Undo.RecordObject(gate, "Configure Game View navigation");
        SerializedObject gateSerialized = new(gate);
        gateSerialized.FindProperty("m_GameViewTurnSpeedDegrees").floatValue = GameViewTurnSpeedDegrees;
        gateSerialized.FindProperty("m_PhysicalHmdDetectionTimeout").floatValue = PhysicalHmdDetectionTimeout;
        gateSerialized.FindProperty("m_GameViewVoiceFlowDirector").objectReferenceValue = voiceFlow;
        gateSerialized.FindProperty("m_GameViewHazmatEquipController").objectReferenceValue = hazmatEquip;
        gateSerialized.FindProperty("m_GameViewMouseGrabAnchor").objectReferenceValue = mouseGrabAnchor;
        SerializedProperty roots = gateSerialized.FindProperty("m_GameViewHandVisualRoots");
        roots.arraySize = handRoots.Length;
        for (int index = 0; index < handRoots.Length; index++)
            roots.GetArrayElementAtIndex(index).objectReferenceValue = handRoots[index];
        gateSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(gate);

        Undo.RecordObject(move, "Configure head-relative locomotion");
        move.forwardSource = activeCamera;
        move.headTransform = activeCamera;
        move.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
        move.rightHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
        SerializedObject moveSerialized = new(move);
        moveSerialized.FindProperty("m_GameViewSimulatorGate").objectReferenceValue = gate;
        moveSerialized.FindProperty("m_GameViewSpeedMultiplier").floatValue = GameViewSpeedMultiplier;
        moveSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(move);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log(
            "[PPE Game View] Applied Up/Down movement, Left/Right yaw, 2.4x Game View speed, and hidden PPE hand renderers.");
    }

    [MenuItem("Tools/PPE/Game View/Validate Arrow Navigation and Hidden Hands")]
    public static void Validate()
    {
        Scene scene = RequireTargetScene();
        List<string> failures = new();
        PhysicalHmdSimulatorGate gate = FindAll<PhysicalHmdSimulatorGate>(scene).SingleOrDefault();
        PPEConfigurableDynamicMoveProvider move = FindAll<PPEConfigurableDynamicMoveProvider>(scene).SingleOrDefault();
        ControllerGuideMiniActivator mini = FindAll<ControllerGuideMiniActivator>(scene).SingleOrDefault();
        PPEVoiceFlowDirector voiceFlow = FindAll<PPEVoiceFlowDirector>(scene).SingleOrDefault();
        PPEHazmatEquipController hazmatEquip = FindAll<PPEHazmatEquipController>(scene).SingleOrDefault();

        if (gate == null)
            failures.Add("Expected one PhysicalHmdSimulatorGate.");
        if (move == null)
            failures.Add("Expected one PPEConfigurableDynamicMoveProvider.");

        if (gate != null)
        {
            SerializedObject serialized = new(gate);
            if (!Mathf.Approximately(
                    serialized.FindProperty("m_GameViewTurnSpeedDegrees")?.floatValue ?? -1f,
                    GameViewTurnSpeedDegrees))
            {
                failures.Add($"Game View turn speed must be {GameViewTurnSpeedDegrees} degrees/second.");
            }
            if (!Mathf.Approximately(
                    serialized.FindProperty("m_PhysicalHmdDetectionTimeout")?.floatValue ?? -1f,
                    PhysicalHmdDetectionTimeout))
            {
                failures.Add($"Physical HMD detection timeout must be {PhysicalHmdDetectionTimeout} seconds.");
            }
            if (serialized.FindProperty("m_ForceGameViewTestMode")?.boolValue == true)
                failures.Add("Game View Test Mode must remain off so HMD sessions keep the original XR path.");
            SerializedProperty roots = serialized.FindProperty("m_GameViewHandVisualRoots");
            if (roots == null || roots.arraySize != HandVisualPaths.Length)
            {
                failures.Add($"Game View hand roots must contain {HandVisualPaths.Length} authored roots.");
            }
            else
            {
                for (int index = 0; index < roots.arraySize; index++)
                {
                    Transform root = roots.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
                    if (root == null || GetPath(root) != HandVisualPaths[index])
                        failures.Add($"Game View hand root [{index}] must be '{HandVisualPaths[index]}'.");
                    else if (root.GetComponentsInChildren<Renderer>(true).Length == 0)
                        failures.Add($"Game View hand root '{HandVisualPaths[index]}' has no Renderer.");
                }
            }

            if (serialized.FindProperty("m_XrUiInputModule")?.objectReferenceValue == null ||
                serialized.FindProperty("m_MousePointAction")?.objectReferenceValue == null ||
                serialized.FindProperty("m_MouseLeftClickAction")?.objectReferenceValue == null ||
                serialized.FindProperty("m_GameViewMouseSelectInteractor")?.objectReferenceValue == null)
            {
                failures.Add("Existing Game View mouse UI/Grab references must remain assigned.");
            }

            Transform mouseGrabAnchor = serialized
                .FindProperty("m_GameViewMouseGrabAnchor")?.objectReferenceValue as Transform;
            if (mouseGrabAnchor == null ||
                mouseGrabAnchor.name != MouseGrabAnchorName ||
                mouseGrabAnchor.parent != gate.transform)
            {
                failures.Add(
                    $"Game View gate must reference its direct child '{MouseGrabAnchorName}'.");
            }

            if (serialized.FindProperty("m_GameViewVoiceFlowDirector")?.objectReferenceValue != voiceFlow)
                failures.Add("Game View gate must reference the scene PPE voice flow.");
            if (serialized.FindProperty("m_GameViewHazmatEquipController")?.objectReferenceValue != hazmatEquip)
                failures.Add("Game View gate must reference the scene hazmat equip controller.");
        }

        if (move != null)
        {
            SerializedObject serialized = new(move);
            if (serialized.FindProperty("m_GameViewSimulatorGate")?.objectReferenceValue != gate)
                failures.Add("Move Provider must reference the scene Game View simulator gate.");
            if (!Mathf.Approximately(
                    serialized.FindProperty("m_GameViewSpeedMultiplier")?.floatValue ?? -1f,
                    GameViewSpeedMultiplier))
            {
                failures.Add($"Game View speed multiplier must be {GameViewSpeedMultiplier}.");
            }
            if (!Mathf.Approximately(move.moveSpeed, 1f))
                failures.Add("The authored Quest/OpenXR move speed must remain 1 m/s.");
            Transform activeCamera = FindTransforms(scene).GetValueOrDefault(ActiveCameraPath);
            if (activeCamera == null || move.forwardSource != activeCamera || move.headTransform != activeCamera)
                failures.Add($"Move Provider forward/head source must be the active XR camera '{ActiveCameraPath}'.");
            if (move.leftHandMovementDirection != DynamicMoveProvider.MovementDirection.HeadRelative)
                failures.Add("Left-hand movement must remain head-relative.");
            if (move.rightHandMovementDirection != DynamicMoveProvider.MovementDirection.HeadRelative)
                failures.Add("Right-hand movement must remain head-relative.");
            if (gate == null || gate.GameViewForwardSource == null)
                failures.Add("Game View movement requires the authored XR Origin Camera forward source.");
        }

        if (mini == null ||
            new SerializedObject(mini).FindProperty("m_ControllerGuideMini")?.objectReferenceValue == null)
        {
            failures.Add("ControllerGuideMiniActivator must retain its authored mini-guide reference.");
        }

        OnScreenButton[] stickButtons = FindAll<OnScreenButton>(scene)
            .Where(button => button.gameObject.name == "StickClick")
            .ToArray();
        if (stickButtons.Length != 1 || stickButtons[0].controlPath != "<Gamepad>/leftStickPress")
            failures.Add("Game View must retain one StickClick button bound to <Gamepad>/leftStickPress.");

        CharacterController characterController = FindAll<CharacterController>(scene)
            .SingleOrDefault(controller => controller.gameObject.name == "XR Origin (VR)");
        if (characterController == null || !characterController.enabled)
            failures.Add("XR Origin (VR) must retain its enabled CharacterController collision path.");

        if (failures.Count > 0)
        {
            string message = "PPE Game View control validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message, gate);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[PPE Game View] PASS: arrow navigation, 2.4x test speed, 14 hidden hand roots, mouse selection, stick-click mini guide, and CharacterController are wired.",
            gate);
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before applying or validating Game View controls. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static T RequireSingle<T>(Scene scene) where T : Component
    {
        T[] matches = FindAll<T>(scene).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected one scene {typeof(T).Name}, found {matches.Length}.");
        return matches[0];
    }

    static Transform GetOrCreateMouseGrabAnchor(PhysicalHmdSimulatorGate gate)
    {
        Transform existing = gate.transform.Find(MouseGrabAnchorName);
        if (existing != null)
            return existing;

        GameObject anchorObject = new(MouseGrabAnchorName);
        Undo.RegisterCreatedObjectUndo(anchorObject, "Create Game View mouse grab anchor");
        anchorObject.transform.SetParent(gate.transform, false);
        return anchorObject.transform;
    }

    static IEnumerable<T> FindAll<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    static Dictionary<string, Transform> FindTransforms(Scene scene)
    {
        HashSet<string> requestedPaths = new(HandVisualPaths, StringComparer.Ordinal);
        Dictionary<string, Transform> matches = new(StringComparer.Ordinal);
        foreach (Transform transform in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
        {
            string path = GetPath(transform);
            if (!requestedPaths.Contains(path))
                continue;
            if (!matches.TryAdd(path, transform))
                throw new InvalidOperationException($"More than one authored object uses hand path '{path}'.");
        }

        return matches;
    }

    static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
