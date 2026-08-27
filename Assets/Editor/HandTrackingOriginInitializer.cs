using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

/// <summary>
/// Builds the rig for hand tracking: an XR Origin whose camera follows the headset, with the XR Hands
/// tracking prefabs parented under the camera offset.
///
/// A rig of its own, deliberately - it does not adopt the controller rig from
/// <see cref="XROriginInitializer"/>. Keeping them apart means either can be switched off wholesale while
/// authoring, and neither one's camera, pose drivers or interactors end up half-rewritten by the other.
///
/// The cost is that two XR Origins in one scene fight over the headset: both cameras track, and the game
/// view picks one. Only one should be active at a time, and this warns when it finds the other one running.
///
/// The skeleton driver this adds is what Ghost Hand Recorder reads to capture a pose.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class HandTrackingOriginInitializer
{
    const string k_InputActionsGuid = "c348712bda248c246b8c49b3db54643f";

    // Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/
    const string k_LeftHandGuid = "b3ed8a0a703ebd34a9e44ed3d9f1fcf6";
    const string k_RightHandGuid = "3f7511fbc40ae7a4b89c3298a3de199d";

    const string k_OriginName = "XR Origin (Hand Tracking)";
    const string k_CameraOffsetName = "Camera Offset";
    const string k_CameraName = "Hand Tracking Camera";

    // Unity's default 0.3 near plane clips the hands off well before they reach the face - which is most of
    // what you look at with hand tracking. These are the Starter Assets values.
    const float k_NearClip = 0.01f;
    const float k_FarClip = 1000f;

    [MenuItem("Tools/XR/Initialize Hand Tracking Origin")]
    static void Initialize()
    {
        var actions = Load<InputActionAsset>(k_InputActionsGuid);
        if (actions == null)
        {
            Debug.LogError("[XR] Could not find 'XRI Default Input Actions'. " +
                "Import the XR Interaction Toolkit Starter Assets sample first.");
            return;
        }

        var references = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(actions));

        EnsureInteractionManager();

        var origin = EnsureOrigin();
        var offset = EnsureCameraOffset(origin);
        EnsureCamera(origin, offset, references);

        var left = EnsureHand(offset, k_LeftHandGuid);
        var right = EnsureHand(offset, k_RightHandGuid);

        EnsureModalityManager(origin, left, right);
        EnsureInputActionManager(origin, actions);
        WarnAboutRivalOrigins(origin);

        EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
        Selection.activeGameObject = origin.gameObject;

        var missing = (left == null ? 1 : 0) + (right == null ? 1 : 0);
        Debug.Log($"[XR] '{k_OriginName}' ready: camera + {2 - missing} tracked hand(s)." +
            (missing > 0 ? " Some hand prefabs were missing - see the warnings above." : ""), origin.gameObject);
    }

    static void EnsureInteractionManager()
    {
        if (Object.FindAnyObjectByType<XRInteractionManager>(FindObjectsInactive.Include) == null)
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
    }

    /// <summary>
    /// Matched by name rather than by type, because finding "an XROrigin" would hand back the controller rig
    /// and quietly turn this into an edit of that instead of a rig of its own.
    /// </summary>
    static XROrigin EnsureOrigin()
    {
        foreach (var candidate in Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include))
        {
            if (candidate.name == k_OriginName)
                return candidate;
        }

        var origin = new GameObject(k_OriginName).AddComponent<XROrigin>();
        Undo.RegisterCreatedObjectUndo(origin.gameObject, "Initialize Hand Tracking Origin");
        return origin;
    }

    /// <summary>
    /// Two active rigs both drive the headset pose and both render, so the game view shows whichever camera
    /// wins. Said rather than fixed - which rig should be live is the author's call, not this tool's.
    /// </summary>
    static void WarnAboutRivalOrigins(XROrigin mine)
    {
        foreach (var other in Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include))
        {
            if (other == mine || !other.gameObject.activeInHierarchy)
                continue;

            Debug.LogWarning($"[XR] '{other.name}' is also active. Two XR Origins fight over the headset - " +
                $"disable one before entering Play.", other.gameObject);
        }
    }

    static Transform EnsureCameraOffset(XROrigin origin)
    {
        var offset = origin.transform.Find(k_CameraOffsetName);
        if (offset == null)
        {
            offset = new GameObject(k_CameraOffsetName).transform;
            offset.SetParent(origin.transform, false);
        }

        origin.CameraFloorOffsetObject = offset.gameObject;
        return offset;
    }

    static void EnsureCamera(XROrigin origin, Transform offset, Object[] references)
    {
        var camera = origin.Camera;

        // Camera.main is deliberately not adopted here: on a scene that already has the controller rig, that
        // would steal its camera and leave that rig headless.
        if (camera == null)
        {
            var existing = offset.Find(k_CameraName);
            camera = existing != null ? existing.GetComponent<Camera>() : null;
        }

        if (camera == null)
        {
            var go = new GameObject(k_CameraName);
            Undo.RegisterCreatedObjectUndo(go, "Initialize Hand Tracking Origin");
            camera = go.AddComponent<Camera>();

            // Only claim these when nothing else holds them - a second MainCamera or AudioListener is a
            // warning at best and a coin flip at worst.
            if (Camera.main == null)
                go.tag = "MainCamera";

            if (Object.FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include) == null)
                go.AddComponent<AudioListener>();
        }

        camera.transform.SetParent(offset, false);
        camera.nearClipPlane = k_NearClip;
        camera.farClipPlane = k_FarClip;
        origin.Camera = camera;

        BindPose(GetOrAdd<TrackedPoseDriver>(camera.gameObject), "XRI Head", references);
    }

    /// <summary>
    /// Parents one of the XR Hands tracking prefabs under the camera offset. The prefab brings
    /// XRHandTrackingEvents, XRHandSkeletonDriver and the mesh with it, so there is nothing to wire - the
    /// tracking subsystem finds it and starts writing joint poses.
    /// </summary>
    static GameObject EnsureHand(Transform offset, string guid)
    {
        var prefab = Load<GameObject>(guid);
        if (prefab == null)
        {
            Debug.LogWarning("[XR] A hand tracking prefab is missing. Import the XR Hands 'HandVisualizer' " +
                "sample from Package Manager.");
            return null;
        }

        var existing = offset.Find(prefab.name);
        if (existing != null)
            return existing.gameObject;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Initialize Hand Tracking Origin");
        instance.transform.SetParent(offset, false);
        return instance;
    }

    /// <summary>
    /// Tells XRI which objects represent the hands so it can turn them on and off as the user puts
    /// controllers down and picks them up. Without it both sets can end up visible at once.
    /// </summary>
    static void EnsureModalityManager(XROrigin origin, GameObject left, GameObject right)
    {
        var manager = GetOrAdd<XRInputModalityManager>(origin.gameObject);

        var serialized = new SerializedObject(manager);

        // Only written when found, so re-running after the prefabs were removed does not clear a setup that
        // was assembled by hand.
        if (left != null)
            serialized.FindProperty("m_LeftHand").objectReferenceValue = left;

        if (right != null)
            serialized.FindProperty("m_RightHand").objectReferenceValue = right;

        serialized.ApplyModifiedProperties();
    }

    /// <summary>Points a driver at the Position / Rotation / Tracking State actions of one map.</summary>
    static void BindPose(TrackedPoseDriver driver, string actionMap, Object[] references)
    {
        driver.positionInput = Property(references, actionMap, "Position");
        driver.rotationInput = Property(references, actionMap, "Rotation");
        driver.trackingStateInput = Property(references, actionMap, "Tracking State");
    }

    static InputActionProperty Property(Object[] references, string actionMap, string actionName)
    {
        foreach (var asset in references)
        {
            if (asset is InputActionReference reference &&
                reference.action != null &&
                reference.action.actionMap != null &&
                reference.action.actionMap.name == actionMap &&
                reference.action.name == actionName)
            {
                return new InputActionProperty(reference);
            }
        }

        Debug.LogWarning($"[XR] Action '{actionMap}/{actionName}' not found in the input actions asset.");
        return new InputActionProperty();
    }

    static void EnsureInputActionManager(XROrigin origin, InputActionAsset actions)
    {
        var manager = GetOrAdd<InputActionManager>(origin.gameObject);

        // The backing field has no initializer, so a freshly added component can hand back null.
        if (manager.actionAssets == null)
            manager.actionAssets = new List<InputActionAsset>();

        if (!manager.actionAssets.Contains(actions))
            manager.actionAssets.Add(actions);
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
