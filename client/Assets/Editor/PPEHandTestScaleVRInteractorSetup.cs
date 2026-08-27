using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

/// <summary>
/// Copies the authored controller-ray and teleportation setup from the disabled
/// VR origin in the scale test scene to the enabled VR origin. The hand-tracking
/// origin and the enabled origin's authored hand visuals are intentionally left alone.
/// </summary>
public static class PPEHandTestScaleVRInteractorSetup
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    private const string VROriginName = "XR Origin (VR)";
    private const string LocomotionName = "PPE Teleport-Only Locomotion";
    private const string EventSystemName = "EventSystem";
    private const string TeleportInteractorName = "Teleport Interactor";

    private static readonly ControllerDefinition[] Controllers =
    {
        new("Left", "Left_NearFarInteractor"),
        new("Right", "Right_NearFarInteractor"),
    };

    private readonly struct ControllerDefinition
    {
        public ControllerDefinition(string side, string nearFarName)
        {
            Side = side;
            NearFarName = nearFarName;
        }

        public string Side { get; }
        public string NearFarName { get; }
    }

    [MenuItem("Tools/PPE/Copy VR Rays To Active Origin (HandTest Scale) %#&t")]
    public static void SetupFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SetupAndValidate();
    }

    [MenuItem("Tools/PPE/Validate Active VR Rays (HandTest Scale)")]
    public static void ValidateFromMenu()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        try
        {
            ValidateScene(scene);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static void SetupBatch()
    {
        try
        {
            SetupAndValidate();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void ValidateBatch()
    {
        try
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void SetupAndValidate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GetVROrigins(scene, out XROrigin sourceOrigin, out XROrigin targetOrigin);

        CopyEventSystem(sourceOrigin.transform, targetOrigin.transform);
        TeleportationProvider targetProvider = CopyLocomotion(
            scene,
            sourceOrigin,
            targetOrigin,
            out TeleportationProvider sourceProvider);

        foreach (ControllerDefinition definition in Controllers)
            CopyControllerInteractors(sourceOrigin, targetOrigin, definition);

        RetargetTeleportAnchors(scene, sourceProvider, targetProvider);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        ValidateScene(scene);
        Debug.Log(
            "HandTest scale VR interactor copy complete: active VR origin now has both " +
            "Near-Far and teleport rays, controller switching, locomotion, and XR UI input.");
    }

    private static void CopyControllerInteractors(
        XROrigin sourceOrigin,
        XROrigin targetOrigin,
        ControllerDefinition definition)
    {
        GameObject sourceController = FindController(sourceOrigin, definition.Side);
        GameObject targetController = FindController(targetOrigin, definition.Side);

        NearFarInteractor sourceNearFar = FindDirectChildComponent<NearFarInteractor>(
            sourceController.transform,
            definition.NearFarName);
        XRRayInteractor sourceTeleport = FindDirectChildComponent<XRRayInteractor>(
            sourceController.transform,
            TeleportInteractorName);
        if (sourceNearFar == null || sourceTeleport == null)
        {
            throw new InvalidOperationException(
                $"Disabled VR origin '{definition.Side}' controller is missing its authored ray setup.");
        }

        RemoveDirectChildComponent<NearFarInteractor>(
            targetController.transform,
            definition.NearFarName);
        RemoveDirectChildComponent<XRRayInteractor>(
            targetController.transform,
            TeleportInteractorName);

        GameObject nearFarObject = CloneHierarchy(
            sourceNearFar.gameObject,
            targetController.transform);
        GameObject teleportObject = CloneHierarchy(
            sourceTeleport.gameObject,
            targetController.transform);

        NearFarInteractor targetNearFar = nearFarObject.GetComponent<NearFarInteractor>();
        XRRayInteractor targetTeleport = teleportObject.GetComponent<XRRayInteractor>();
        if (targetNearFar == null || targetTeleport == null)
            throw new InvalidOperationException("Copied controller interactor hierarchy is incomplete.");

        nearFarObject.SetActive(true);
        teleportObject.SetActive(false);

        CopyNearFarVisual(sourceController, targetController, targetNearFar);
        CopyTeleportModeManager(
            sourceController,
            targetController,
            targetNearFar,
            targetTeleport);
    }

    private static void CopyNearFarVisual(
        GameObject sourceController,
        GameObject targetController,
        NearFarInteractor targetNearFar)
    {
        XRNearFarReticleVisual sourceVisual = sourceController.GetComponent<XRNearFarReticleVisual>();
        if (sourceVisual == null)
            throw new InvalidOperationException($"'{GetPath(sourceController.transform)}' has no ray visual setup.");

        XRNearFarReticleVisual targetVisual = targetController.GetComponent<XRNearFarReticleVisual>();
        if (targetVisual == null)
            targetVisual = Undo.AddComponent<XRNearFarReticleVisual>(targetController);

        EditorUtility.CopySerialized(sourceVisual, targetVisual);

        CurveVisualController targetCurve = targetNearFar.GetComponentInChildren<CurveVisualController>(true);
        if (targetCurve == null)
            throw new InvalidOperationException($"'{GetPath(targetNearFar.transform)}' has no CurveVisualController.");

        SerializedObject serializedVisual = new(targetVisual);
        SetObjectReference(serializedVisual, "nearFarInteractor", targetNearFar);
        SetObjectReference(serializedVisual, "curveVisualController", targetCurve);
        serializedVisual.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(targetVisual);
    }

    private static void CopyTeleportModeManager(
        GameObject sourceController,
        GameObject targetController,
        NearFarInteractor targetNearFar,
        XRRayInteractor targetTeleport)
    {
        PPEControllerTeleportModeManager sourceManager =
            sourceController.GetComponent<PPEControllerTeleportModeManager>();
        if (sourceManager == null)
            throw new InvalidOperationException($"'{GetPath(sourceController.transform)}' has no teleport mode manager.");

        PPEControllerTeleportModeManager targetManager =
            targetController.GetComponent<PPEControllerTeleportModeManager>();
        if (targetManager == null)
            targetManager = Undo.AddComponent<PPEControllerTeleportModeManager>(targetController);

        EditorUtility.CopySerialized(sourceManager, targetManager);
        SerializedObject serializedManager = new(targetManager);
        SetObjectReference(serializedManager, "m_NearFarInteractor", targetNearFar);
        SetObjectReference(serializedManager, "m_TeleportInteractor", targetTeleport);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(targetManager);
    }

    private static TeleportationProvider CopyLocomotion(
        Scene scene,
        XROrigin sourceOrigin,
        XROrigin targetOrigin,
        out TeleportationProvider sourceProvider)
    {
        Transform sourceLocomotion = FindDirectChild(sourceOrigin.transform, LocomotionName);
        if (sourceLocomotion == null)
            throw new InvalidOperationException("Disabled VR origin has no teleport locomotion hierarchy.");

        sourceProvider = sourceLocomotion.GetComponentInChildren<TeleportationProvider>(true);
        if (sourceProvider == null)
            throw new InvalidOperationException("Disabled VR origin has no TeleportationProvider.");

        Transform existing = FindDirectChild(targetOrigin.transform, LocomotionName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject copiedLocomotion = CloneHierarchy(sourceLocomotion.gameObject, targetOrigin.transform);
        copiedLocomotion.SetActive(true);

        XRBodyTransformer bodyTransformer = copiedLocomotion.GetComponent<XRBodyTransformer>();
        TeleportationProvider targetProvider =
            copiedLocomotion.GetComponentInChildren<TeleportationProvider>(true);
        if (bodyTransformer == null || targetProvider == null)
            throw new InvalidOperationException("Copied teleport locomotion hierarchy is incomplete.");

        SerializedObject serializedTransformer = new(bodyTransformer);
        SetObjectReference(serializedTransformer, "m_XROrigin", targetOrigin);
        serializedTransformer.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bodyTransformer);
        EditorUtility.SetDirty(targetProvider);
        return targetProvider;
    }

    private static void CopyEventSystem(Transform sourceOrigin, Transform targetOrigin)
    {
        Transform sourceEventSystem = FindDirectChild(sourceOrigin, EventSystemName);
        if (sourceEventSystem == null)
            throw new InvalidOperationException("Disabled VR origin has no XR EventSystem.");

        Transform existing = FindDirectChild(targetOrigin, EventSystemName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject copiedEventSystem = CloneHierarchy(sourceEventSystem.gameObject, targetOrigin);
        copiedEventSystem.SetActive(true);
    }

    private static void RetargetTeleportAnchors(
        Scene scene,
        TeleportationProvider sourceProvider,
        TeleportationProvider targetProvider)
    {
        int retargeted = 0;
        foreach (TeleportationAnchor anchor in FindSceneComponents<TeleportationAnchor>(scene))
        {
            if (anchor.teleportationProvider != sourceProvider)
                continue;

            Undo.RecordObject(anchor, "Retarget teleport anchor to active VR origin");
            anchor.teleportationProvider = targetProvider;
            EditorUtility.SetDirty(anchor);
            retargeted++;
        }

        if (retargeted == 0)
            throw new InvalidOperationException("No teleport anchors referenced the disabled VR origin provider.");
    }

    private static GameObject CloneHierarchy(GameObject source, Transform parent)
    {
        GameObject prefabAsset = PrefabUtility.IsAnyPrefabInstanceRoot(source)
            ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(source)
            : null;
        GameObject clone;
        if (prefabAsset != null)
        {
            clone = PrefabUtility.InstantiatePrefab(prefabAsset, parent.gameObject.scene) as GameObject;
            if (clone == null)
                throw new InvalidOperationException($"Failed to instantiate prefab for '{source.name}'.");

            Undo.RegisterCreatedObjectUndo(clone, $"Copy {source.name}");
            Undo.SetTransformParent(clone.transform, parent, $"Parent {source.name}");

            PropertyModification[] sourceModifications =
                PrefabUtility.GetPropertyModifications(source);
            if (sourceModifications != null)
                PrefabUtility.SetPropertyModifications(clone, sourceModifications);
        }
        else
        {
            clone = UnityEngine.Object.Instantiate(source, parent, false);
            Undo.RegisterCreatedObjectUndo(clone, $"Copy {source.name}");
        }

        clone.name = source.name;
        clone.transform.SetLocalPositionAndRotation(
            source.transform.localPosition,
            source.transform.localRotation);
        clone.transform.localScale = source.transform.localScale;
        clone.SetActive(source.activeSelf);
        if (PrefabUtility.IsPartOfPrefabInstance(clone))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(clone);
            PrefabUtility.RecordPrefabInstancePropertyModifications(clone.transform);
        }
        EditorUtility.SetDirty(clone);
        return clone;
    }

    private static void ValidateScene(Scene scene)
    {
        GetVROrigins(scene, out XROrigin sourceOrigin, out XROrigin targetOrigin);
        List<string> failures = new();

        if (sourceOrigin.gameObject.activeSelf)
            failures.Add("Source VR origin must remain disabled.");
        if (!targetOrigin.gameObject.activeSelf)
            failures.Add("Target VR origin must remain enabled.");

        Transform eventSystemTransform = FindDirectChild(targetOrigin.transform, EventSystemName);
        if (eventSystemTransform == null || !eventSystemTransform.gameObject.activeSelf)
        {
            failures.Add("Active VR origin has no enabled EventSystem.");
        }
        else
        {
            if (eventSystemTransform.GetComponent<EventSystem>() == null)
                failures.Add("Copied EventSystem is missing EventSystem component.");
            if (eventSystemTransform.GetComponent<XRUIInputModule>() == null)
                failures.Add("Copied EventSystem is missing XRUIInputModule.");
        }

        Transform locomotionTransform = FindDirectChild(targetOrigin.transform, LocomotionName);
        TeleportationProvider targetProvider = locomotionTransform != null
            ? locomotionTransform.GetComponentInChildren<TeleportationProvider>(true)
            : null;
        XRBodyTransformer bodyTransformer = locomotionTransform != null
            ? locomotionTransform.GetComponent<XRBodyTransformer>()
            : null;
        if (locomotionTransform == null || !locomotionTransform.gameObject.activeSelf)
            failures.Add("Active VR origin has no enabled teleport locomotion hierarchy.");
        if (targetProvider == null)
            failures.Add("Active VR origin has no TeleportationProvider.");
        if (bodyTransformer == null)
        {
            failures.Add("Active VR origin has no XRBodyTransformer.");
        }
        else
        {
            SerializedObject serializedTransformer = new(bodyTransformer);
            if (GetObjectReference(serializedTransformer, "m_XROrigin") != targetOrigin)
                failures.Add("XRBodyTransformer does not reference the active VR origin.");
        }

        foreach (ControllerDefinition definition in Controllers)
            ValidateController(targetOrigin, definition, failures);

        List<TeleportationAnchor> anchors = FindSceneComponents<TeleportationAnchor>(scene);
        if (anchors.Count == 0)
        {
            failures.Add("Scene has no TeleportationAnchor.");
        }
        else if (targetProvider != null)
        {
            foreach (TeleportationAnchor anchor in anchors)
            {
                if (anchor.teleportationProvider != targetProvider)
                {
                    failures.Add(
                        $"'{GetPath(anchor.transform)}' does not reference the active VR provider.");
                }
            }
        }

        if (failures.Count > 0)
        {
            string message = "HandTest scale active VR ray validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "HandTest scale active VR ray validation passed: Hand Tracking origin excluded; " +
            "both controllers have Near-Far and teleport rays.");
    }

    private static void ValidateController(
        XROrigin targetOrigin,
        ControllerDefinition definition,
        List<string> failures)
    {
        GameObject controller;
        try
        {
            controller = FindController(targetOrigin, definition.Side);
        }
        catch (Exception exception)
        {
            failures.Add(exception.Message);
            return;
        }

        NearFarInteractor nearFar = FindDirectChildComponent<NearFarInteractor>(
            controller.transform,
            definition.NearFarName);
        XRRayInteractor teleport = FindDirectChildComponent<XRRayInteractor>(
            controller.transform,
            TeleportInteractorName);
        if (nearFar == null)
            failures.Add($"Active {definition.Side} controller has no NearFarInteractor.");
        else if (!nearFar.gameObject.activeSelf)
            failures.Add($"Active {definition.Side} NearFarInteractor must start enabled.");
        if (teleport == null)
            failures.Add($"Active {definition.Side} controller has no Teleport Interactor.");
        else if (teleport.gameObject.activeSelf)
            failures.Add($"Active {definition.Side} Teleport Interactor must start disabled.");

        XRNearFarReticleVisual visual = controller.GetComponent<XRNearFarReticleVisual>();
        if (visual == null)
        {
            failures.Add($"Active {definition.Side} controller has no near-far ray visual.");
        }
        else if (nearFar != null)
        {
            SerializedObject serializedVisual = new(visual);
            if (GetObjectReference(serializedVisual, "nearFarInteractor") != nearFar)
                failures.Add($"Active {definition.Side} ray visual references the wrong interactor.");
        }

        PPEControllerTeleportModeManager manager =
            controller.GetComponent<PPEControllerTeleportModeManager>();
        if (manager == null)
        {
            failures.Add($"Active {definition.Side} controller has no teleport mode manager.");
            return;
        }

        SerializedObject serializedManager = new(manager);
        if (nearFar != null && GetObjectReference(serializedManager, "m_NearFarInteractor") != nearFar)
            failures.Add($"Active {definition.Side} manager references the wrong NearFarInteractor.");
        if (teleport != null && GetObjectReference(serializedManager, "m_TeleportInteractor") != teleport)
            failures.Add($"Active {definition.Side} manager references the wrong Teleport Interactor.");
        if (GetObjectReference(serializedManager, "m_TeleportMode") == null)
            failures.Add($"Active {definition.Side} manager has no Teleport Mode action.");
        if (GetObjectReference(serializedManager, "m_TeleportModeCancel") == null)
            failures.Add($"Active {definition.Side} manager has no Teleport Mode Cancel action.");
    }

    private static void GetVROrigins(
        Scene scene,
        out XROrigin sourceOrigin,
        out XROrigin targetOrigin)
    {
        List<XROrigin> vrOrigins = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            XROrigin origin = root.GetComponent<XROrigin>();
            if (origin != null && root.name == VROriginName)
                vrOrigins.Add(origin);
        }

        if (vrOrigins.Count != 2)
        {
            throw new InvalidOperationException(
                $"Expected exactly two root '{VROriginName}' objects, found {vrOrigins.Count}. " +
                "The Hand Tracking origin is intentionally excluded.");
        }

        sourceOrigin = null;
        targetOrigin = null;
        foreach (XROrigin origin in vrOrigins)
        {
            if (origin.gameObject.activeSelf)
            {
                if (targetOrigin != null)
                    throw new InvalidOperationException("More than one VR origin is enabled.");
                targetOrigin = origin;
            }
            else
            {
                if (sourceOrigin != null)
                    throw new InvalidOperationException("More than one VR origin is disabled.");
                sourceOrigin = origin;
            }
        }

        if (sourceOrigin == null || targetOrigin == null)
            throw new InvalidOperationException("Could not identify one disabled and one enabled VR origin.");
    }

    private static GameObject FindController(XROrigin origin, string side)
    {
        Transform cameraOffset = origin.CameraFloorOffsetObject != null
            ? origin.CameraFloorOffsetObject.transform
            : null;
        if (cameraOffset == null)
            throw new InvalidOperationException($"'{GetPath(origin.transform)}' has no Camera Offset.");

        string expected = NormalizeName(side + "Controller");
        GameObject result = null;
        for (int index = 0; index < cameraOffset.childCount; index++)
        {
            Transform child = cameraOffset.GetChild(index);
            if (NormalizeName(child.name) != expected)
                continue;
            if (result != null)
                throw new InvalidOperationException($"'{GetPath(origin.transform)}' has duplicate {side} controllers.");
            result = child.gameObject;
        }

        if (result == null)
            throw new InvalidOperationException($"'{GetPath(origin.transform)}' has no {side} controller.");
        return result;
    }

    private static T FindDirectChildComponent<T>(Transform parent, string childName)
        where T : Component
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name != childName)
                continue;
            T component = child.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private static void RemoveDirectChildComponent<T>(Transform parent, string childName)
        where T : Component
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName && child.GetComponent<T>() != null)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> results = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyPath,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
        {
            throw new MissingFieldException(
                serializedObject.targetObject.GetType().Name,
                propertyPath);
        }

        property.objectReferenceValue = value;
    }

    private static UnityEngine.Object GetObjectReference(
        SerializedObject serializedObject,
        string propertyPath)
    {
        return serializedObject.FindProperty(propertyPath)?.objectReferenceValue;
    }

    private static string NormalizeName(string value)
    {
        return value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    private static string GetPath(Transform transform)
    {
        List<string> names = new();
        while (transform != null)
        {
            names.Add(transform.name);
            transform = transform.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
