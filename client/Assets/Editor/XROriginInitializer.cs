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
/// Completes the bare rig that GameObject > XR > XR Origin (VR) creates: it has a camera but no
/// interaction manager, no controllers and no interactors, so nothing can be grabbed.
/// The skeleton is built by hand so the structure stays visible, while the interactors come from the
/// Starter Assets prefabs - those are too intricate to wire correctly from script.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class XROriginInitializer
{
    const string k_InputActionsGuid = "c348712bda248c246b8c49b3db54643f";
    const string k_LeftInteractorGuid = "3df3e1220f2164f448701a6de8084f92";
    const string k_RightInteractorGuid = "b200f6587d118224eba8467281481800";

    const string k_CameraOffsetName = "Camera Offset";
    const string k_CameraName = "Main Camera";

    // Unity's default 0.3 near plane cuts the hands off well before they reach the face. These are the
    // values the Starter Assets rig ships with.
    const float k_NearClip = 0.01f;
    const float k_FarClip = 1000f;

    [MenuItem("Tools/XR/Initialize XR Origin")]
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

        EnsureController(offset, "Left Controller", "XRI Left", k_LeftInteractorGuid, references);
        EnsureController(offset, "Right Controller", "XRI Right", k_RightInteractorGuid, references);

        EnsureInputActionManager(origin, actions);

        EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
        Selection.activeGameObject = origin.gameObject;
        Debug.Log("[XR] XR Origin initialized: interaction manager, camera, both controllers and near-far interactors.");
    }

    static void EnsureInteractionManager()
    {
        if (Object.FindAnyObjectByType<XRInteractionManager>() != null)
            return;

        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
    }

    static XROrigin EnsureOrigin()
    {
        var origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin != null)
            return origin;

        return new GameObject("XR Origin (VR)").AddComponent<XROrigin>();
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

        // A scene created from the default template already has a camera; adopt it instead of
        // ending up with two, which would make the game view pick one arbitrarily.
        if (camera == null)
            camera = Camera.main;

        if (camera == null)
        {
            var go = new GameObject(k_CameraName) { tag = "MainCamera" };
            camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        camera.transform.SetParent(offset, false);
        camera.nearClipPlane = k_NearClip;
        camera.farClipPlane = k_FarClip;
        origin.Camera = camera;

        BindPose(GetOrAdd<TrackedPoseDriver>(camera.gameObject), "XRI Head", references);
    }

    static void EnsureController(Transform offset, string name, string actionMap, string interactorGuid, Object[] references)
    {
        var controller = offset.Find(name);
        if (controller == null)
        {
            controller = new GameObject(name).transform;
            controller.SetParent(offset, false);
        }

        BindPose(GetOrAdd<TrackedPoseDriver>(controller.gameObject), actionMap, references);

        var interactorPrefab = Load<GameObject>(interactorGuid);
        if (interactorPrefab == null)
        {
            Debug.LogWarning($"[XR] Near-far interactor prefab missing for {name}; the controller will track but not interact.");
            return;
        }

        // The prefab carries the caster and filter setup, so only add it when it is not there yet.
        if (controller.Find(interactorPrefab.name) != null)
            return;

        var interactor = (GameObject)PrefabUtility.InstantiatePrefab(interactorPrefab);
        interactor.transform.SetParent(controller, false);
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
