using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Migrates the PPE hand test scene to the XRI Starter Assets teleport ray workflow.
/// </summary>
public static class PPEOfficialTeleportationSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest.unity";
    const string TeleportInteractorPrefabPath =
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/Interactors/Teleport Interactor.prefab";
    const string InputActionsPath =
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
    const string StarterControllerManagerTypeName =
        "UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager, " +
        "Unity.XR.Interaction.Toolkit.Samples.StarterAssets";
    const string TeleportLayerName = "Teleport";
    const string TeleportPhysicsLayerName = "Teleport Target";

    static readonly ControllerDefinition[] Controllers =
    {
        new("LeftController", "Left_NearFarInteractor", "PPE_A_Hand_GloveSuit_L", "XRI Left Locomotion", "XRI Left Interaction", InteractorHandedness.Left),
        new("RightController", "Right_NearFarInteractor", "PPE_A_Hand_GloveSuit_R", "XRI Right Locomotion", "XRI Right Interaction", InteractorHandedness.Right),
    };

    static readonly DestinationDefinition[] Destinations =
    {
        new("XR Location Marker_big_PPE_1", "PPE_1 Arrival Anchor"),
        new("XR Location Marker_big_PPE_2", "PPE_2 Arrival Anchor"),
    };

    readonly struct ControllerDefinition
    {
        public ControllerDefinition(
            string controllerName,
            string nearFarName,
            string authoredHandRootName,
            string locomotionActionMap,
            string interactionActionMap,
            InteractorHandedness handedness)
        {
            ControllerName = controllerName;
            NearFarName = nearFarName;
            AuthoredHandRootName = authoredHandRootName;
            LocomotionActionMap = locomotionActionMap;
            InteractionActionMap = interactionActionMap;
            Handedness = handedness;
        }

        public string ControllerName { get; }
        public string NearFarName { get; }
        public string AuthoredHandRootName { get; }
        public string LocomotionActionMap { get; }
        public string InteractionActionMap { get; }
        public InteractorHandedness Handedness { get; }
    }

    readonly struct DestinationDefinition
    {
        public DestinationDefinition(string markerName, string anchorName)
        {
            MarkerName = markerName;
            AnchorName = anchorName;
        }

        public string MarkerName { get; }
        public string AnchorName { get; }
    }

    [MenuItem("Tools/PPE/Setup Official Teleportation (HandTest)")]
    public static void SetupFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SetupAndValidate();
    }

    [MenuItem("Tools/PPE/Validate Official Teleportation (HandTest)")]
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

    static void SetupAndValidate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TeleportationProvider provider = FindSingle<TeleportationProvider>(scene);
        if (provider == null)
            throw new InvalidOperationException($"'{ScenePath}' has no TeleportationProvider.");

        GameObject teleportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeleportInteractorPrefabPath);
        if (teleportPrefab == null)
            throw new InvalidOperationException($"Missing teleport interactor prefab: '{TeleportInteractorPrefabPath}'.");

        foreach (ControllerDefinition definition in Controllers)
            ConfigureController(scene, definition, teleportPrefab);

        foreach (DestinationDefinition definition in Destinations)
            ConfigureDestination(scene, definition, provider);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        ValidateScene(scene);
        Debug.Log("HandTest official XRI teleportation setup complete: both controller rays and both PPE anchors are configured.");
    }

    static void ConfigureController(
        Scene scene,
        ControllerDefinition definition,
        GameObject teleportPrefab)
    {
        GameObject controller = FindSceneObject(scene, definition.ControllerName);
        if (controller == null)
            throw new InvalidOperationException($"Missing controller '{definition.ControllerName}'.");

        NearFarInteractor nearFar = FindNamedComponentInChildren<NearFarInteractor>(controller, definition.NearFarName);
        if (nearFar == null)
            throw new InvalidOperationException($"Missing NearFarInteractor '{definition.NearFarName}'.");

        Transform authoredHandRoot = FindNamedTransformInChildren(controller, definition.AuthoredHandRootName);
        if (authoredHandRoot == null)
            throw new InvalidOperationException($"Missing authored hand root '{definition.AuthoredHandRootName}'.");

        Vector3 rayLocalPosition = controller.transform.InverseTransformPoint(authoredHandRoot.position);
        Quaternion rayLocalRotation = Quaternion.Inverse(controller.transform.rotation) * authoredHandRoot.rotation;

        Undo.RecordObject(nearFar.transform, "Align Near-Far interactor to authored hand");
        nearFar.transform.SetLocalPositionAndRotation(rayLocalPosition, rayLocalRotation);
        EditorUtility.SetDirty(nearFar.transform);

        RemoveDirectChild(controller.transform, "Ray Interactor");

        XRRayInteractor teleportInteractor = EnsureTeleportInteractor(
            scene,
            controller.transform,
            teleportPrefab,
            rayLocalPosition,
            rayLocalRotation);
        ConfigureTeleportInteractor(teleportInteractor, definition);
        ConfigureTeleportLineVisual(teleportInteractor);

        RemoveStarterAssetsControllerManager(controller);

        PPEControllerTeleportModeManager manager = controller.GetComponent<PPEControllerTeleportModeManager>();
        if (manager == null)
            manager = Undo.AddComponent<PPEControllerTeleportModeManager>(controller);

        SerializedObject serializedManager = new(manager);
        SetObjectReference(serializedManager, "m_NearFarInteractor", nearFar, true);
        SetObjectReference(serializedManager, "m_TeleportInteractor", teleportInteractor, true);
        SetObjectReference(
            serializedManager,
            "m_TeleportMode",
            FindInputActionReference(definition.LocomotionActionMap, "Teleport Mode"),
            true);
        SetObjectReference(
            serializedManager,
            "m_TeleportModeCancel",
            FindInputActionReference(definition.LocomotionActionMap, "Teleport Mode Cancel"),
            true);
        SetBool(serializedManager, "m_RequireScenarioSelection", true);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        // PPEControllerTeleportModeManager owns this active state at runtime.
        teleportInteractor.gameObject.SetActive(false);
        EditorUtility.SetDirty(teleportInteractor.gameObject);
    }

    static XRRayInteractor EnsureTeleportInteractor(
        Scene scene,
        Transform controller,
        GameObject teleportPrefab,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        List<GameObject> matches = new();
        for (int childIndex = 0; childIndex < controller.childCount; childIndex++)
        {
            GameObject child = controller.GetChild(childIndex).gameObject;
            if (child.name == "Teleport Interactor")
                matches.Add(child);
        }

        GameObject instance = matches.Count > 0 ? matches[0] : null;
        for (int duplicateIndex = 1; duplicateIndex < matches.Count; duplicateIndex++)
            Undo.DestroyObjectImmediate(matches[duplicateIndex]);

        if (instance == null)
        {
            instance = PrefabUtility.InstantiatePrefab(teleportPrefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Failed to instantiate the XRI Teleport Interactor prefab.");
            Undo.RegisterCreatedObjectUndo(instance, "Create official teleport interactor");
            Undo.SetTransformParent(instance.transform, controller, "Parent official teleport interactor");
        }

        Undo.RecordObject(instance.transform, "Align official teleport interactor");
        instance.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        instance.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(instance.transform);

        XRRayInteractor interactor = instance.GetComponent<XRRayInteractor>();
        if (interactor == null)
            throw new InvalidOperationException($"'{GetPath(instance.transform)}' has no XRRayInteractor.");
        return interactor;
    }

    static void ConfigureTeleportInteractor(XRRayInteractor interactor, ControllerDefinition definition)
    {
        Undo.RecordObject(interactor, "Configure official teleport interactor");
        interactor.handedness = definition.Handedness;
        interactor.interactionLayers = InteractionLayerMask.GetMask(TeleportLayerName);

        int teleportPhysicsLayer = LayerMask.NameToLayer(TeleportPhysicsLayerName);
        if (teleportPhysicsLayer < 0)
            throw new InvalidOperationException($"Missing physics layer '{TeleportPhysicsLayerName}'.");

        InputActionReference teleportMode =
            FindInputActionReference(definition.LocomotionActionMap, "Teleport Mode");
        InputActionReference activate =
            FindInputActionReference(definition.InteractionActionMap, "Activate");
        InputActionReference activateValue =
            FindInputActionReference(definition.InteractionActionMap, "Activate Value");

        SerializedObject serializedInteractor = new(interactor);
        SetInteger(serializedInteractor, "m_SelectInput.m_InputSourceMode", 2, true);
        SetObjectReference(
            serializedInteractor,
            "m_SelectInput.m_InputActionReferencePerformed",
            teleportMode,
            true);
        SetObjectReference(
            serializedInteractor,
            "m_SelectInput.m_InputActionReferenceValue",
            teleportMode,
            true);
        SetInteger(serializedInteractor, "m_ActivateInput.m_InputSourceMode", 2, true);
        SetObjectReference(
            serializedInteractor,
            "m_ActivateInput.m_InputActionReferencePerformed",
            activate,
            true);
        SetObjectReference(
            serializedInteractor,
            "m_ActivateInput.m_InputActionReferenceValue",
            activateValue,
            true);
        SetBool(serializedInteractor, "m_EnableUIInteraction", false);
        SetInteger(serializedInteractor, "m_RaycastMask.m_Bits", 1 << teleportPhysicsLayer, true);
        serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
        EditorUtility.SetDirty(interactor);
    }

    static void ConfigureTeleportLineVisual(XRRayInteractor interactor)
    {
        XRInteractorLineVisual lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
        if (lineVisual == null)
            throw new InvalidOperationException($"'{GetPath(interactor.transform)}' has no XRInteractorLineVisual.");

        Undo.RecordObject(lineVisual, "Treat teleport selection as valid line state");
        lineVisual.treatSelectionAsValidState = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(lineVisual);
        EditorUtility.SetDirty(lineVisual);
    }

    static void ConfigureDestination(
        Scene scene,
        DestinationDefinition definition,
        TeleportationProvider provider)
    {
        GameObject marker = FindSceneObject(scene, definition.MarkerName);
        GameObject arrivalObject = FindSceneObject(scene, definition.AnchorName);
        if (marker == null || arrivalObject == null)
            throw new InvalidOperationException(
                $"Missing marker or arrival anchor: '{definition.MarkerName}' / '{definition.AnchorName}'.");

        Transform arrival = arrivalObject.transform;
        int teleportPhysicsLayer = LayerMask.NameToLayer(TeleportPhysicsLayerName);
        if (teleportPhysicsLayer < 0)
            throw new InvalidOperationException($"Missing physics layer '{TeleportPhysicsLayerName}'.");

        Undo.RecordObject(marker, "Assign PPE teleport target physics layer");
        marker.layer = teleportPhysicsLayer;
        EditorUtility.SetDirty(marker);

        XRLocationTeleportTarget legacyTarget = marker.GetComponent<XRLocationTeleportTarget>();
        if (legacyTarget != null)
        {
            SerializedObject serializedLegacy = new(legacyTarget);
            float arrivalOffset = serializedLegacy.FindProperty("arrivalForwardOffset")?.floatValue ?? 0f;
            if (arrivalOffset > 0f)
            {
                Undo.RecordObject(arrival, "Preserve PPE teleport arrival offset");
                arrival.position = marker.transform.position + arrival.forward * arrivalOffset;
                EditorUtility.SetDirty(arrival);
            }

            Undo.DestroyObjectImmediate(legacyTarget);
        }

        TeleportationAnchor teleportAnchor = marker.GetComponent<TeleportationAnchor>();
        if (teleportAnchor == null)
            teleportAnchor = Undo.AddComponent<TeleportationAnchor>(marker);

        Undo.RecordObject(teleportAnchor, "Configure official PPE teleport anchor");
        teleportAnchor.teleportationProvider = provider;
        teleportAnchor.teleportAnchorTransform = arrival;
        teleportAnchor.matchOrientation = MatchOrientation.TargetUpAndForward;
        teleportAnchor.matchDirectionalInput = false;
        teleportAnchor.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
        teleportAnchor.interactionLayers = InteractionLayerMask.GetMask(TeleportLayerName);
        EditorUtility.SetDirty(teleportAnchor);
    }

    static void ValidateScene(Scene scene)
    {
        List<string> failures = new();
        TeleportationProvider provider = FindSingle<TeleportationProvider>(scene);
        int teleportMask = InteractionLayerMask.GetMask(TeleportLayerName);
        int teleportPhysicsLayer = LayerMask.NameToLayer(TeleportPhysicsLayerName);

        if (provider == null)
            failures.Add("Missing TeleportationProvider.");
        if (teleportPhysicsLayer < 0)
            failures.Add($"Missing physics layer '{TeleportPhysicsLayerName}'.");

        foreach (ControllerDefinition definition in Controllers)
        {
            GameObject controller = FindSceneObject(scene, definition.ControllerName);
            if (controller == null)
            {
                failures.Add($"Missing controller '{definition.ControllerName}'.");
                continue;
            }

            if (FindDirectChild(controller.transform, "Ray Interactor") != null)
                failures.Add($"'{definition.ControllerName}' still has a separate general Ray Interactor.");

            NearFarInteractor nearFar = FindNamedComponentInChildren<NearFarInteractor>(controller, definition.NearFarName);
            Transform authoredHandRoot = FindNamedTransformInChildren(controller, definition.AuthoredHandRootName);
            Transform teleportTransform = FindDirectChild(controller.transform, "Teleport Interactor");
            XRRayInteractor teleportInteractor =
                teleportTransform != null ? teleportTransform.GetComponent<XRRayInteractor>() : null;
            PPEControllerTeleportModeManager manager = controller.GetComponent<PPEControllerTeleportModeManager>();
            Type starterManagerType = Type.GetType(StarterControllerManagerTypeName);
            if (starterManagerType != null && controller.GetComponent(starterManagerType) != null)
                failures.Add($"'{definition.ControllerName}' still has the Starter Assets ControllerInputActionManager.");

            if (nearFar == null)
                failures.Add($"'{definition.ControllerName}' has no '{definition.NearFarName}'.");
            if (authoredHandRoot == null)
                failures.Add($"'{definition.ControllerName}' has no '{definition.AuthoredHandRootName}'.");
            if (teleportInteractor == null)
            {
                failures.Add($"'{definition.ControllerName}' has no official Teleport Interactor.");
                continue;
            }

            if (teleportInteractor.gameObject.activeSelf)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' must start inactive.");
            Vector3 expectedRayPosition = authoredHandRoot != null
                ? controller.transform.InverseTransformPoint(authoredHandRoot.position)
                : Vector3.zero;
            Quaternion expectedRayRotation = authoredHandRoot != null
                ? Quaternion.Inverse(controller.transform.rotation) * authoredHandRoot.rotation
                : Quaternion.identity;
            if (Vector3.SqrMagnitude(nearFar.transform.localPosition - expectedRayPosition) > 0.000001f ||
                Quaternion.Angle(nearFar.transform.localRotation, expectedRayRotation) > 0.01f)
            {
                failures.Add($"'{GetPath(nearFar.transform)}' is not aligned to the authored hand root.");
            }
            if (Vector3.SqrMagnitude(teleportInteractor.transform.localPosition - expectedRayPosition) > 0.000001f ||
                Quaternion.Angle(teleportInteractor.transform.localRotation, expectedRayRotation) > 0.01f)
            {
                failures.Add($"'{GetPath(teleportInteractor.transform)}' is not aligned to the authored hand root.");
            }
            if (teleportInteractor.handedness != definition.Handedness)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' has the wrong handedness.");
            if ((int)teleportInteractor.interactionLayers != teleportMask)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' is not restricted to the Teleport interaction layer.");

            XRInteractorLineVisual lineVisual = teleportInteractor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual == null)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' has no XRInteractorLineVisual.");
            else if (!lineVisual.treatSelectionAsValidState)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' does not treat an active teleport selection as a valid line state.");

            InputActionReference expectedTeleportMode =
                FindInputActionReference(definition.LocomotionActionMap, "Teleport Mode");
            SerializedObject serializedInteractor = new(teleportInteractor);
            int raycastMask = serializedInteractor.FindProperty("m_RaycastMask.m_Bits")?.intValue ?? 0;
            if (teleportPhysicsLayer >= 0 && raycastMask != (1 << teleportPhysicsLayer))
                failures.Add($"'{GetPath(teleportInteractor.transform)}' does not raycast only against '{TeleportPhysicsLayerName}'.");
            UnityEngine.Object selectAction = serializedInteractor
                .FindProperty("m_SelectInput.m_InputActionReferencePerformed")?.objectReferenceValue;
            if (selectAction != expectedTeleportMode)
                failures.Add($"'{GetPath(teleportInteractor.transform)}' has the wrong Select action.");

            if (manager == null)
            {
                failures.Add($"'{definition.ControllerName}' has no PPEControllerTeleportModeManager.");
                continue;
            }

            SerializedObject serializedManager = new(manager);
            if (GetObjectReference(serializedManager, "m_NearFarInteractor") != nearFar)
                failures.Add($"'{definition.ControllerName}' manager has the wrong NearFarInteractor.");
            if (GetObjectReference(serializedManager, "m_TeleportInteractor") != teleportInteractor)
                failures.Add($"'{definition.ControllerName}' manager has the wrong Teleport Interactor.");
            if (GetObjectReference(serializedManager, "m_TeleportMode") != expectedTeleportMode)
                failures.Add($"'{definition.ControllerName}' manager has the wrong Teleport Mode action.");
            InputActionReference expectedCancel =
                FindInputActionReference(definition.LocomotionActionMap, "Teleport Mode Cancel");
            if (GetObjectReference(serializedManager, "m_TeleportModeCancel") != expectedCancel)
                failures.Add($"'{definition.ControllerName}' manager has the wrong Teleport Mode Cancel action.");
            if (!(serializedManager.FindProperty("m_RequireScenarioSelection")?.boolValue ?? false))
                failures.Add($"'{definition.ControllerName}' does not require a scenario selection before teleporting.");
        }

        foreach (DestinationDefinition definition in Destinations)
        {
            GameObject marker = FindSceneObject(scene, definition.MarkerName);
            GameObject arrivalObject = FindSceneObject(scene, definition.AnchorName);
            if (marker == null || arrivalObject == null)
            {
                failures.Add($"Missing marker or arrival anchor '{definition.MarkerName}' / '{definition.AnchorName}'.");
                continue;
            }

            if (marker.GetComponent<XRLocationTeleportTarget>() != null)
                failures.Add($"'{definition.MarkerName}' still has the legacy XRLocationTeleportTarget.");
            if (teleportPhysicsLayer >= 0 && marker.layer != teleportPhysicsLayer)
                failures.Add($"'{definition.MarkerName}' is not on physics layer '{TeleportPhysicsLayerName}'.");

            TeleportationAnchor teleportAnchor = marker.GetComponent<TeleportationAnchor>();
            if (teleportAnchor == null)
            {
                failures.Add($"'{definition.MarkerName}' has no TeleportationAnchor.");
                continue;
            }

            if (teleportAnchor.teleportationProvider != provider)
                failures.Add($"'{definition.MarkerName}' has the wrong TeleportationProvider.");
            if (teleportAnchor.teleportAnchorTransform != arrivalObject.transform)
                failures.Add($"'{definition.MarkerName}' has the wrong destination Transform.");
            if (teleportAnchor.matchOrientation != MatchOrientation.TargetUpAndForward)
                failures.Add($"'{definition.MarkerName}' must match target up and forward.");
            if ((int)teleportAnchor.interactionLayers != teleportMask)
                failures.Add($"'{definition.MarkerName}' is not on the Teleport interaction layer.");
        }

        if (failures.Count > 0)
        {
            string message = "HandTest official teleportation validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log("HandTest official teleportation validation passed.");
    }

    static void RemoveDirectChild(Transform parent, string childName)
    {
        for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = parent.GetChild(childIndex);
            if (child.name == childName)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    static void RemoveStarterAssetsControllerManager(GameObject controller)
    {
        Type managerType = Type.GetType(StarterControllerManagerTypeName);
        Component manager = managerType != null ? controller.GetComponent(managerType) : null;
        if (manager != null)
            Undo.DestroyObjectImmediate(manager);
    }

    static InputActionReference FindInputActionReference(string actionMapName, string actionName)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
        {
            if (asset is InputActionReference reference && reference.action?.actionMap?.name == actionMapName &&
                reference.action.name == actionName)
            {
                return reference;
            }
        }

        throw new InvalidOperationException(
            $"Missing InputActionReference '{actionMapName}/{actionName}' in '{InputActionsPath}'.");
    }

    static T FindSingle<T>(Scene scene) where T : Component
    {
        T result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (result != null)
                    throw new InvalidOperationException($"Expected one {typeof(T).Name}, found multiple instances.");
                result = component;
            }
        }

        return result;
    }

    static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject result = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name != objectName)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Expected one scene object named '{objectName}'.");
                result = transform.gameObject;
            }
        }

        return result;
    }

    static T FindNamedComponentInChildren<T>(GameObject root, string objectName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == objectName)
                return component;
        }

        return null;
    }

    static Transform FindNamedTransformInChildren(GameObject root, string objectName)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            if (transform.name == objectName)
                return transform;

        return null;
    }

    static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
        {
            Transform child = parent.GetChild(childIndex);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyPath,
        UnityEngine.Object value,
        bool required)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
        {
            if (required)
                throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyPath);
            return;
        }

        property.objectReferenceValue = value;
    }

    static UnityEngine.Object GetObjectReference(SerializedObject serializedObject, string propertyPath)
    {
        return serializedObject.FindProperty(propertyPath)?.objectReferenceValue;
    }

    static void SetBool(SerializedObject serializedObject, string propertyPath, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyPath);
        property.boolValue = value;
    }

    static void SetInteger(SerializedObject serializedObject, string propertyPath, int value, bool required)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
        {
            if (required)
                throw new MissingFieldException(serializedObject.targetObject.GetType().Name, propertyPath);
            return;
        }

        property.intValue = value;
    }

    static string GetPath(Transform transform)
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
