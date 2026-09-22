using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public static class PPEControllerModelRayAlignmentSetup
{
    private const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    private const float PositionTolerance = 0.0001f;
    private const float RotationTolerance = 0.05f;

    private readonly struct ControllerDefinition
    {
        public ControllerDefinition(
            string side,
            string modelName,
            string markerName,
            bool useMarkerRotation)
        {
            Side = side;
            ModelName = modelName;
            MarkerName = markerName;
            UseMarkerRotation = useMarkerRotation;
        }

        public string Side { get; }
        public string ModelName { get; }
        public string MarkerName { get; }
        public bool UseMarkerRotation { get; }
        public string ControllerPath => $"XR Origin (VR)/Camera Offset/{Side} Controller";
        public string InteractorName => $"{Side}_NearFarInteractor";
        public string ModelPath => $"Visual/Hand Offset/{ModelName}";
        public string OriginName => $"{Side} Controller Ray Origin";
    }

    private static readonly ControllerDefinition[] Controllers =
    {
        new("Left", "OculusTouchForQuest2_Left", "left_laser_begin", true),
        // The imported right marker points about 22.5 degrees above the authored
        // XRI aim direction. Use it only for the visible muzzle position.
        new("Right", "OculusTouchForQuest2_Right", "right_laser_begin", false),
    };

    [MenuItem("Tools/PPE/Align Controller Model Rays")]
    public static void Align()
    {
        Scene scene = RequireCleanTargetScene("aligning controller model rays");

        foreach (ControllerDefinition definition in Controllers)
            AlignController(scene, definition);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    [MenuItem("Tools/PPE/Align Controller Models To Ray Origins")]
    public static void AlignModelsToRayOrigins()
    {
        Scene scene = RequireCleanTargetScene("aligning controller models to their ray origins");

        foreach (ControllerDefinition definition in Controllers)
            AlignModelToRayOrigin(scene, definition);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    [MenuItem("Tools/PPE/Validate Controller Model Rays")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before validating controller model rays.");

        List<string> failures = new();
        foreach (ControllerDefinition definition in Controllers)
            ValidateController(scene, definition, failures);

        if (failures.Count > 0)
        {
            string message = "PPE controller model ray validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[PPE Controller Rays] PASS: both CurveInteractionCaster origins start at the " +
            "controller-model laser markers, authored model and ray rotations are preserved, " +
            "the right ray keeps its XRI aim direction, and Near Grab and attach origins remain unchanged.");
    }

    private static void AlignModelToRayOrigin(Scene scene, ControllerDefinition definition)
    {
        Transform controller = RequireTransformAtPath(scene, definition.ControllerPath);
        Transform model = controller.Find(definition.ModelPath);
        Transform origin = controller.Find(definition.OriginName);
        Transform marker = FindDescendant(model, definition.MarkerName);
        if (model == null || marker == null || origin == null)
        {
            throw new InvalidOperationException(
                $"{definition.Side} controller requires its model, laser marker, and authored ray origin.");
        }

        Vector3 translation = origin.position - marker.position;
        if (translation.sqrMagnitude <= PositionTolerance * PositionTolerance)
            return;

        Undo.RecordObject(model, $"Align {definition.Side} controller model to ray origin");
        model.position += translation;
        PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        EditorUtility.SetDirty(model);
    }

    private static void AlignController(Scene scene, ControllerDefinition definition)
    {
        Transform controller = RequireTransformAtPath(scene, definition.ControllerPath);
        Transform model = controller.Find(definition.ModelPath);
        if (model == null)
            throw new InvalidOperationException($"Missing controller model '{definition.ModelPath}'.");

        Transform marker = FindDescendant(model, definition.MarkerName);
        if (marker == null)
            throw new InvalidOperationException($"Missing controller laser marker '{definition.MarkerName}'.");

        Transform interactor = controller.Find(definition.InteractorName);
        if (interactor == null)
            throw new InvalidOperationException($"Missing interactor '{definition.InteractorName}'.");

        CurveInteractionCaster curveCaster = interactor.GetComponent<CurveInteractionCaster>();
        if (curveCaster == null)
            throw new InvalidOperationException($"'{definition.InteractorName}' requires CurveInteractionCaster.");

        Transform origin = controller.Find(definition.OriginName);
        bool createdOrigin = origin == null;
        if (origin == null)
        {
            GameObject originObject = new(definition.OriginName);
            Undo.RegisterCreatedObjectUndo(originObject, $"Create {definition.OriginName}");
            origin = originObject.transform;
            Undo.SetTransformParent(origin, controller, $"Parent {definition.OriginName}");
        }

        Undo.RecordObject(origin, $"Align {definition.OriginName}");
        Quaternion targetRotation = createdOrigin
            ? (definition.UseMarkerRotation ? marker.rotation : interactor.rotation)
            : origin.rotation;
        origin.SetPositionAndRotation(marker.position, targetRotation);
        origin.localScale = Vector3.one;
        EditorUtility.SetDirty(origin);

        SerializedObject serializedCaster = new(curveCaster);
        SerializedProperty castOrigin = RequireProperty(serializedCaster, "m_CastOrigin");
        Undo.RecordObject(curveCaster, $"Assign {definition.Side} far ray origin");
        castOrigin.objectReferenceValue = origin;
        serializedCaster.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(curveCaster);
        EditorUtility.SetDirty(curveCaster);
    }

    private static void ValidateController(
        Scene scene,
        ControllerDefinition definition,
        List<string> failures)
    {
        Transform controller = FindTransformAtPath(scene, definition.ControllerPath);
        if (controller == null)
        {
            failures.Add($"Missing '{definition.ControllerPath}'.");
            return;
        }

        Transform model = controller.Find(definition.ModelPath);
        Transform marker = FindDescendant(model, definition.MarkerName);
        Transform interactor = controller.Find(definition.InteractorName);
        Transform origin = controller.Find(definition.OriginName);
        if (model == null || marker == null || interactor == null || origin == null)
        {
            failures.Add($"{definition.Side} controller alignment hierarchy is incomplete.");
            return;
        }

        if (origin.parent != controller)
            failures.Add($"{definition.OriginName} must remain a direct controller child.");
        if (Vector3.Distance(origin.position, marker.position) > PositionTolerance)
            failures.Add($"{definition.Side} far ray origin position does not match its model marker.");
        if (!definition.UseMarkerRotation &&
            Quaternion.Angle(origin.rotation, interactor.rotation) > RotationTolerance)
        {
            failures.Add(
                $"{definition.Side} far ray origin rotation does not match its authored XRI aim.");
        }
        if (Vector3.Distance(origin.localScale, Vector3.one) > PositionTolerance)
            failures.Add($"{definition.Side} far ray origin scale must be one.");

        CurveInteractionCaster curveCaster = interactor.GetComponent<CurveInteractionCaster>();
        SphereInteractionCaster sphereCaster = interactor.GetComponent<SphereInteractionCaster>();
        InteractionAttachController attachController = interactor.GetComponent<InteractionAttachController>();
        if (curveCaster == null || sphereCaster == null || attachController == null)
        {
            failures.Add($"{definition.InteractorName} is missing an authored caster or attach component.");
            return;
        }

        if (GetObjectReference(curveCaster, "m_CastOrigin") != origin)
            failures.Add($"{definition.Side} CurveInteractionCaster does not use the aligned origin.");
        if (GetObjectReference(sphereCaster, "m_CastOrigin") != interactor)
            failures.Add($"{definition.Side} Near Grab origin changed unexpectedly.");
        if (GetObjectReference(attachController, "m_TransformToFollow") != interactor)
            failures.Add($"{definition.Side} interaction attach origin changed unexpectedly.");
    }

    private static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string propertyName)
    {
        SerializedObject serializedObject = new(target);
        return RequireProperty(serializedObject, propertyName).objectReferenceValue;
    }

    private static SerializedProperty RequireProperty(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyName);
        return property;
    }

    private static Scene RequireCleanTargetScene(string operation)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException($"Stop Play Mode before {operation}.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before {operation}.");
        if (scene.isDirty)
            throw new InvalidOperationException("The active PPE scene has unsaved changes. Review or save them first.");
        return scene;
    }

    private static Transform RequireTransformAtPath(Scene scene, string path)
    {
        Transform target = FindTransformAtPath(scene, path);
        if (target == null)
            throw new InvalidOperationException($"Missing scene object '{path}'.");
        return target;
    }

    private static Transform FindTransformAtPath(Scene scene, string path)
    {
        string[] names = path.Split('/');
        GameObject root = Array.Find(scene.GetRootGameObjects(), candidate => candidate.name == names[0]);
        if (root == null)
            return null;

        Transform current = root.transform;
        for (int index = 1; index < names.Length; index++)
        {
            current = current.Find(names[index]);
            if (current == null)
                return null;
        }
        return current;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            Transform found = FindDescendant(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
