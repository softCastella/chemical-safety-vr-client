using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Builds the movement lessons on top of the rig from <see cref="XROriginInitializer"/>.
///
/// The two menu items layer, the same way the lessons do. Lesson_02 is movement by stick: input goes in
/// and the rig moves. Lesson_03 adds teleport, which stacks an interactor and an interactable on top of
/// that - the aiming ray has to hit something that declares itself a destination, so it reuses what
/// Lesson_01 taught about interactables.
///
/// Keeping them separate means a lesson scene holds only the components that lesson talks about. A
/// student opening Lesson_02 should not find a teleport provider there and wonder what it is.
///
/// Several pieces fail silently when left unwired: a provider with no mediator never moves anything, and
/// a vignette with an empty provider list never triggers. Neither logs a warning.
/// Running either item repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class LocomotionLessonBuilder
{
    const string k_InputActionsGuid = "c348712bda248c246b8c49b3db54643f";
    const string k_TeleportInteractorGuid = "c1800acf6366418a9b5f610249000331";
    const string k_VignetteGuid = "6c8af5c8012f01440af6cb2bc3eb987c";

    const string k_LocomotionName = "Locomotion";
    const string k_FloorName = "Floor";

    /// <summary>Interaction layer the Starter Assets teleport ray is restricted to.</summary>
    const string k_TeleportLayerName = "Teleport";

    /// <summary>
    /// Sized for a standing adult. GravityProvider rewrites the height and centre every frame from the
    /// camera, so these only matter for the first frame and for what the Scene view draws.
    /// </summary>
    const float k_BodyHeight = 1.36f;
    const float k_BodyRadius = 0.1f;

    // Asymmetric on purpose, matching the Starter Assets rig: the left stick walks while the right one
    // turns. One stick cannot do both, so splitting the job across hands is what makes both available.
    const bool k_LeftSmoothMotion = true;
    const bool k_RightSmoothMotion = false;

    // ------------------------------------------------------------------ Lesson_02

    [MenuItem("Tools/XR/Build Locomotion Lesson", priority = 20)]
    static void BuildLocomotion()
    {
        if (!TryBegin(out var origin, out var references))
            return;

        BuildLocomotionCore(origin, references);

        Finish(origin, "Locomotion built: continuous move on the left hand, snap turn on the right.");
    }

    static void BuildLocomotionCore(XROrigin origin, Object[] references)
    {
        EnsureBody(origin);
        EnsureFloor();

        var mediator = EnsureMediator(origin);
        GetOrAdd<GravityProvider>(EnsureChild(mediator.transform, "Gravity").gameObject).mediator = mediator;

        var move = GetOrAdd<DynamicMoveProvider>(EnsureChild(mediator.transform, "Move").gameObject);
        move.mediator = mediator;
        move.forwardSource = origin.Camera != null ? origin.Camera.transform : null;
        Bind(move.leftHandMoveInput, references, "XRI Left Locomotion", "Move");
        Bind(move.rightHandMoveInput, references, "XRI Right Locomotion", "Move");

        var turn = EnsureChild(mediator.transform, "Turn");

        var snapTurn = GetOrAdd<SnapTurnProvider>(turn.gameObject);
        snapTurn.mediator = mediator;
        Bind(snapTurn.leftHandTurnInput, references, "XRI Left Locomotion", "Snap Turn");
        Bind(snapTurn.rightHandTurnInput, references, "XRI Right Locomotion", "Snap Turn");

        // Present but idle until the Smooth Turn toggle asks for it.
        var continuousTurn = GetOrAdd<ContinuousTurnProvider>(turn.gameObject);
        continuousTurn.mediator = mediator;
        Bind(continuousTurn.leftHandTurnInput, references, "XRI Left Locomotion", "Turn");
        Bind(continuousTurn.rightHandTurnInput, references, "XRI Right Locomotion", "Turn");

        // Move, Turn and Snap Turn are all bound to the same stick. Without this manager deciding which
        // one is live, they would all fire at once.
        SetUpController(origin, references, "Left Controller", "XRI Left Locomotion", k_LeftSmoothMotion);
        SetUpController(origin, references, "Right Controller", "XRI Right Locomotion", k_RightSmoothMotion);

        BuildVignette(origin);
    }

    // ------------------------------------------------------------------ Lesson_03

    [MenuItem("Tools/XR/Build Teleport Lesson", priority = 21)]
    static void BuildTeleport()
    {
        if (!TryBegin(out var origin, out var references))
            return;

        // Teleport sits on top of Lesson_02 rather than replacing it, so a scene built straight from
        // Lesson_01 still ends up complete.
        BuildLocomotionCore(origin, references);

        var mediator = EnsureMediator(origin);
        GetOrAdd<TeleportationProvider>(EnsureChild(mediator.transform, "Teleportation").gameObject).mediator = mediator;

        // A floor with a collider is somewhere to stand, not somewhere to go. This is what makes it a
        // destination, and its absence is the single most common reason teleport "does not work".
        var floor = GameObject.Find(k_FloorName);
        if (floor != null)
            MakeDestination(GetOrAdd<TeleportationArea>(floor));

        AddTeleportRay(origin, references, "Left Controller", "XRI Left Locomotion", k_LeftSmoothMotion);
        AddTeleportRay(origin, references, "Right Controller", "XRI Right Locomotion", k_RightSmoothMotion);

        Finish(origin, "Teleport built: provider, floor destination and both teleport rays.");
    }

    // ------------------------------------------------------------------ pieces

    static bool TryBegin(out XROrigin origin, out Object[] references)
    {
        references = null;
        origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogError("[XR] No XR Origin in the scene. Run Tools > XR > Initialize XR Origin first.");
            return false;
        }

        var actions = Load<InputActionAsset>(k_InputActionsGuid);
        if (actions == null)
        {
            Debug.LogError("[XR] Could not find 'XRI Default Input Actions'. " +
                "Import the XR Interaction Toolkit Starter Assets sample first.");
            return false;
        }

        references = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(actions));
        return true;
    }

    static void Finish(XROrigin origin, string message)
    {
        EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
        Debug.Log($"[XR] {message}");
    }

    /// <summary>The Character Controller is what stops the rig at walls and floors.</summary>
    static void EnsureBody(XROrigin origin)
    {
        var body = GetOrAdd<CharacterController>(origin.gameObject);
        body.height = k_BodyHeight;
        body.radius = k_BodyRadius;
        body.center = new Vector3(0f, k_BodyHeight * 0.5f, 0f);
    }

    /// <summary>
    /// Puts a destination on the interaction layer the teleport ray actually looks at.
    ///
    /// The Starter Assets teleport interactor is restricted to layer 31, "Teleport", so that aiming to
    /// move never picks up something meant to be grabbed. A freshly added TeleportationArea sits on
    /// "Default" instead, and layers that do not overlap mean no interaction at all - the ray reaches the
    /// floor, refuses it, and shows the blocked reticle. It reads as "teleport is broken" rather than
    /// "these two are on different layers".
    /// </summary>
    static void MakeDestination(BaseTeleportationInteractable destination)
    {
        var layers = InteractionLayerMask.GetMask(k_TeleportLayerName);
        if (layers == 0)
        {
            Debug.LogWarning(
                $"[XR] No '{k_TeleportLayerName}' interaction layer exists, so {destination.name} stays on its " +
                "current layer. Add one in Project Settings > XR Interaction Toolkit, or match the layer by hand.");
            return;
        }

        destination.interactionLayers = layers;
        EditorUtility.SetDirty(destination);
    }

    /// <summary>Continuous movement needs something to stand on, or gravity drops the rig forever.</summary>
    static void EnsureFloor()
    {
        if (GameObject.Find(k_FloorName) != null)
            return;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = k_FloorName;
        floor.transform.position = Vector3.zero;
    }

    /// <summary>
    /// Providers never move the rig themselves. They ask the mediator for permission and hand the
    /// movement to the body transformer, which applies everything once per frame. Splitting the
    /// providers onto their own child objects keeps the inspector readable.
    /// </summary>
    static LocomotionMediator EnsureMediator(XROrigin origin)
    {
        var root = EnsureChild(origin.transform, k_LocomotionName);
        GetOrAdd<XRBodyTransformer>(root.gameObject);
        return GetOrAdd<LocomotionMediator>(root.gameObject);
    }

    /// <summary>
    /// Narrows the edges of vision while moving. Peripheral vision is what reacts most strongly to the
    /// flow of a moving image, so covering it is the cheapest comfort win available.
    /// </summary>
    static void BuildVignette(XROrigin origin)
    {
        var camera = origin.Camera;
        if (camera == null)
            return;

        var prefab = Load<GameObject>(k_VignetteGuid);
        if (prefab == null)
        {
            Debug.LogWarning("[XR] TunnelingVignette prefab not found; continuous movement will have no comfort option.");
            return;
        }

        var vignette = camera.transform.Find(prefab.name);
        if (vignette == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(camera.transform, false);
            vignette = instance.transform;
        }

        var controller = vignette.GetComponentInChildren<TunnelingVignetteController>();
        if (controller == null)
            return;

        // The vignette fades in for the providers listed here and no others. Leaving the list empty is
        // the quiet failure - the effect is present, correctly configured, and never triggers.
        controller.locomotionVignetteProviders.Clear();
        foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>(true))
        {
            // Teleport has no motion to cover, and including it would blink the screen on arrival.
            if (provider is TeleportationProvider)
                continue;

            controller.locomotionVignetteProviders.Add(new LocomotionVignetteProvider
            {
                locomotionProvider = provider,
                enabled = true,
            });
        }

        EditorUtility.SetDirty(controller);
    }

    /// <summary>
    /// Decides which of the actions sharing this hand's stick is live. Safe to run before a teleport ray
    /// exists - the manager null-checks its interactor, so Lesson_02 simply has nothing to swap in.
    /// </summary>
    static void SetUpController(XROrigin origin, Object[] references, string controllerName, string actionMap, bool smoothMotion)
    {
        var controller = Find(origin.transform, controllerName);
        if (controller == null)
        {
            Debug.LogWarning($"[XR] No '{controllerName}' found; skipping its locomotion input.");
            return;
        }

        var manager = GetOrAdd<ControllerInputActionManager>(controller.gameObject);
        var serialized = new SerializedObject(manager);
        serialized.FindProperty("m_TeleportMode").objectReferenceValue = Reference(references, actionMap, "Teleport Mode");
        serialized.FindProperty("m_TeleportModeCancel").objectReferenceValue = Reference(references, actionMap, "Teleport Mode Cancel");
        serialized.FindProperty("m_Turn").objectReferenceValue = Reference(references, actionMap, "Turn");
        serialized.FindProperty("m_SnapTurn").objectReferenceValue = Reference(references, actionMap, "Snap Turn");
        serialized.FindProperty("m_Move").objectReferenceValue = Reference(references, actionMap, "Move");
        serialized.FindProperty("m_SmoothMotionEnabled").boolValue = smoothMotion;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// The teleport ray is a second interactor that only exists while the stick is held forward. The
    /// manager built above is what switches it on and off.
    /// </summary>
    static void AddTeleportRay(XROrigin origin, Object[] references, string controllerName, string actionMap, bool smoothMotion)
    {
        var controller = Find(origin.transform, controllerName);
        if (controller == null)
            return;

        var prefab = Load<GameObject>(k_TeleportInteractorGuid);
        if (prefab == null)
        {
            Debug.LogError("[XR] Teleport Interactor prefab not found in the Starter Assets sample.");
            return;
        }

        var interactor = controller.Find(prefab.name);
        if (interactor == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(controller, false);
            interactor = instance.transform;
        }

        // The field is typed as the ray interactor, not the object holding it.
        var ray = interactor.GetComponent<XRRayInteractor>();
        if (ray == null)
        {
            Debug.LogError($"[XR] '{interactor.name}' has no XRRayInteractor; the teleport ray cannot be wired.");
            return;
        }

        // The prefab ships with an empty select input; the rig fills it in per instance. Holding the
        // stick forward is what selects, and letting go is what teleports, so without this the ray
        // appears and the reticle turns valid but releasing does nothing at all.
        var teleportMode = Reference(references, actionMap, "Teleport Mode");
        if (teleportMode != null)
        {
            ray.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputActionReference;
            ray.selectInput.inputActionReferencePerformed = teleportMode;
            ray.selectInput.inputActionReferenceValue = teleportMode;
            EditorUtility.SetDirty(ray);
        }

        SetUpController(origin, references, controllerName, actionMap, smoothMotion);

        var manager = controller.GetComponent<ControllerInputActionManager>();
        var serialized = new SerializedObject(manager);
        serialized.FindProperty("m_TeleportInteractor").objectReferenceValue = ray;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Bind(XRInputValueReader<Vector2> reader, Object[] references, string actionMap, string actionName)
    {
        var reference = Reference(references, actionMap, actionName);
        if (reference == null)
            return;

        reader.inputSourceMode = XRInputValueReader.InputSourceMode.InputActionReference;
        reader.inputActionReference = reference;
    }

    static InputActionReference Reference(Object[] references, string actionMap, string actionName)
    {
        foreach (var asset in references)
        {
            if (asset is InputActionReference reference &&
                reference.action != null &&
                reference.action.actionMap != null &&
                reference.action.actionMap.name == actionMap &&
                reference.action.name == actionName)
            {
                return reference;
            }
        }

        Debug.LogWarning($"[XR] Action '{actionMap}/{actionName}' not found in the input actions asset.");
        return null;
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == name)
                return transform;
        }

        return null;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
            return child;

        var created = new GameObject(name).transform;
        created.SetParent(parent, false);
        return created;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static T Load<T>(string guid) where T : Object
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
