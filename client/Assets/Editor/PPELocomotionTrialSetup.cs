using System;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Wires the locomotion-trial copy scene only. Does not modify
/// 3_PPE_Room_Train_Test_mask.unity. Unity generates any new FileIDs.
/// </summary>
public static class PPELocomotionTrialSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity";
    const string InputActionsGuid = "c348712bda248c246b8c49b3db54643f";
    const string LocomotionRootName = "PPE Teleport-Only Locomotion";
    const string MoveChildName = "Move";
    const string DestinationName = "PPE_1 Arrival Anchor";

    [MenuItem("Tools/PPE/Setup Locomotion Trial Scene")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 로코모션 시험 씬 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Locomotion trial setup was cancelled.");
            return;
        }

        Scene scene = RequireTargetScene();
        Undo.SetCurrentGroupName("Setup locomotion trial scene");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            PPEVoiceFlowDirector director = RequireSingle<PPEVoiceFlowDirector>(scene);
            TeleportationProvider provider = RequireSingle<TeleportationProvider>(scene);
            Transform destination = RequireNamed(scene, DestinationName);
            LocomotionMediator mediator = RequireSingle<LocomotionMediator>(scene);
            XROrigin origin = RequireNamed(scene, "XR Origin (VR)").GetComponent<XROrigin>();
            if (origin == null)
                throw new InvalidOperationException("XR Origin (VR) is missing XROrigin.");
            DynamicMoveProvider move = EnsureMoveProvider(scene, mediator, origin);

            SerializedObject serialized = new(director);
            serialized.Update();
            serialized.FindProperty("m_AutoMoveToPpeThenLocomotion").boolValue = true;
            serialized.FindProperty("m_LocomotionTeleportProvider").objectReferenceValue = provider;
            serialized.FindProperty("m_LocomotionStartDestination").objectReferenceValue = destination;
            serialized.FindProperty("m_LocomotionMoveProvider").objectReferenceValue = move;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(director);

            PPEIdleLocomotionAnimatorSetup.Configure(scene, false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            Debug.Log(
                "Locomotion trial wired: EDU_001 MoveToPPE 뒤에 Teleport_0(PPE_1 Arrival Anchor)로 자동 이동하고, 텔레포트 입력은 닫힌 채 스틱 Move만 켭니다. Idle 하반신 walk도 같은 씬에 연결했습니다. 원본 mask 씬은 변경하지 않았습니다.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static DynamicMoveProvider EnsureMoveProvider(
        Scene scene,
        LocomotionMediator mediator,
        XROrigin origin)
    {
        Transform root = null;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == LocomotionRootName)
                {
                    if (root != null)
                        throw new InvalidOperationException($"'{LocomotionRootName}' must be unique.");
                    root = candidate;
                }
            }
        }

        if (root == null)
            throw new InvalidOperationException($"'{LocomotionRootName}' was not found.");

        Transform moveTransform = root.Find(MoveChildName);
        if (moveTransform == null)
        {
            GameObject moveObject = new(MoveChildName);
            Undo.RegisterCreatedObjectUndo(moveObject, "Create locomotion Move");
            moveTransform = moveObject.transform;
            Undo.SetTransformParent(moveTransform, root, "Parent locomotion Move");
            moveTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            moveTransform.localScale = Vector3.one;
        }

        DynamicMoveProvider move = moveTransform.GetComponent<DynamicMoveProvider>();
        if (move == null)
            move = Undo.AddComponent<DynamicMoveProvider>(moveTransform.gameObject);

        Undo.RecordObject(move, "Configure locomotion Move");
        move.mediator = mediator;
        move.forwardSource = origin.Camera != null ? origin.Camera.transform : null;
        move.headTransform = origin.Camera != null ? origin.Camera.transform : null;
        move.enabled = false;

        InputActionReference[] references = LoadActionReferences();
        Bind(move.leftHandMoveInput, references, "XRI Left Locomotion", "Move");
        Bind(move.rightHandMoveInput, references, "XRI Right Locomotion", "Move");
        EditorUtility.SetDirty(move);
        return move;
    }

    static void Bind(
        XRInputValueReader<Vector2> reader,
        InputActionReference[] references,
        string actionMap,
        string actionName)
    {
        InputActionReference reference = FindAction(references, actionMap, actionName);
        if (reference == null)
        {
            throw new InvalidOperationException($"Input action '{actionMap}/{actionName}' was not found.");
        }

        reader.inputSourceMode = XRInputValueReader.InputSourceMode.InputActionReference;
        reader.inputActionReference = reference;
    }

    static InputActionReference[] LoadActionReferences()
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(InputActionsGuid);
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null || assets.Length == 0)
            throw new InvalidOperationException("XRI Default Input Actions was not found.");

        System.Collections.Generic.List<InputActionReference> references = new();
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is InputActionReference reference)
                references.Add(reference);
        }

        return references.ToArray();
    }

    static InputActionReference FindAction(
        InputActionReference[] references,
        string actionMap,
        string actionName)
    {
        foreach (InputActionReference reference in references)
        {
            if (reference != null &&
                reference.action != null &&
                reference.action.actionMap != null &&
                reference.action.actionMap.name == actionMap &&
                reference.action.name == actionName)
            {
                return reference;
            }
        }

        return null;
    }

    static T RequireSingle<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (found != null && candidate != found)
                    throw new InvalidOperationException($"Expected exactly one {typeof(T).Name} in the locomotion trial scene.");
                found = candidate;
            }
        }

        if (found == null)
            throw new InvalidOperationException($"{typeof(T).Name} was not found.");
        return found;
    }

    static Transform RequireNamed(Scene scene, string objectName)
    {
        Transform match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName)
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"'{objectName}' must be unique.");
                match = candidate;
            }
        }

        if (match == null)
            throw new InvalidOperationException($"'{objectName}' was not found.");
        return match;
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before setting up the locomotion trial. Current scene: '{scene.path}'.");
        }

        return scene;
    }
}
