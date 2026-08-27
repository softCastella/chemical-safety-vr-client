using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Builds the 3D UI lesson in Lesson_08: a stand of levers, knobs, sliders and buttons.
///
/// Almost the opposite of <see cref="UILessonBuilder"/>, and that contrast is the lesson. These controls
/// are XRBaseInteractable subclasses reached through colliders, so there is no canvas, no graphic
/// raycaster and no EventSystem to wire - what has to exist instead is an XRInteractionManager, which is
/// the one piece that lives once per scene and therefore cannot ride along on the prefab.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class UI3DLessonBuilder
{
    const string k_RootName = "3D UI";
    const string k_OffsetName = "Offset";

    /// <summary>Assets/Prefabs/Simple Example Controls.prefab - the stand and its seven controls.</summary>
    const string k_ControlsPrefabGuid = "b34fba54224585241b1a829c75020d3f";

    // The root stays at the origin and the Offset carries the placement, so the lesson can be re-aimed by
    // moving one object without the root's frame drifting out from under it.
    static readonly Vector3 k_OffsetPosition = new(1.94f, -0.43f, 1.87f);
    static readonly Vector3 k_OffsetRotation = new(0f, -177.326f, 0f);

    [MenuItem("Tools/XR/Build 3D UI Lesson", priority = 26)]
    static void Build()
    {
        var manager = EnsureInteractionManager();
        var controls = EnsureControls();

        WarnIfRigMissing();
        NoteRedundantEventSystem();

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Selection.activeGameObject = controls != null ? controls : manager.gameObject;

        Debug.Log($"[XR] 3D UI wired: {(controls != null ? $"controls '{controls.name}'" : "no controls")}, " +
            "XR Interaction Manager present. The controls carry their own colliders and events - " +
            "nothing else to connect.");
    }

    /// <summary>
    /// The piece that cannot come from a prefab. Every interactable registers with it, so without one in
    /// the scene nothing is grabbable and the failure is silent.
    /// </summary>
    static XRInteractionManager EnsureInteractionManager()
    {
        var manager = Object.FindAnyObjectByType<XRInteractionManager>(FindObjectsInactive.Include);
        if (manager != null)
            return manager;

        manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        Undo.RegisterCreatedObjectUndo(manager.gameObject, "Build 3D UI Lesson");
        Debug.Log("[XR] Added the missing XR Interaction Manager.", manager);
        return manager;
    }

    /// <summary>
    /// The control stand, under a "3D UI/Offset" pair so the lesson can be moved as a whole without
    /// touching the prefab. Reuses whatever is already there - the scene copy is the positioned one.
    /// </summary>
    static GameObject EnsureControls()
    {
        var prefab = Load<GameObject>(k_ControlsPrefabGuid);
        if (prefab == null)
        {
            Debug.LogError($"[XR] Control prefab (guid {k_ControlsPrefabGuid}) could not be loaded.");
            return null;
        }

        var offset = EnsureOffset();

        // Matched by name rather than by prefab link, so a stand that was unpacked still counts as present
        // and does not get a duplicate dropped on top of it.
        var existing = offset.Find(prefab.name);
        if (existing != null)
            return existing.gameObject;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Build 3D UI Lesson");
        instance.transform.SetParent(offset, false);
        return instance;
    }

    static Transform EnsureOffset()
    {
        var root = FindSceneRoot(k_RootName);
        if (root == null)
        {
            root = new GameObject(k_RootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build 3D UI Lesson");
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        var offset = root.Find(k_OffsetName);
        if (offset == null)
        {
            offset = new GameObject(k_OffsetName).transform;
            Undo.RegisterCreatedObjectUndo(offset.gameObject, "Build 3D UI Lesson");
            offset.SetParent(root, false);

            // Only on a fresh one. Re-running the menu after nudging the stand into place should not shove
            // it back to the default.
            offset.SetLocalPositionAndRotation(k_OffsetPosition, Quaternion.Euler(k_OffsetRotation));
        }

        return offset;
    }

    static void WarnIfRigMissing()
    {
        if (Object.FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("[XR] No XR Origin in the scene; run Tools > XR > Initialize XR Origin first.");
    }

    /// <summary>
    /// Said out loud because it is the thing a reader of the 2D UI lesson will reach for first. An
    /// EventSystem here is not harmful, just inert - these controls never travel through it.
    /// </summary>
    static void NoteRedundantEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem != null)
            Debug.Log("[XR] An EventSystem is present. 3D UI does not use it - the controls are " +
                "interactables reached through colliders. Harmless, but it wires up nothing here.", eventSystem);
    }

    static Transform FindSceneRoot(string name)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform.parent == null && transform.name == name)
                return transform;
        }

        return null;
    }

    static T Load<T>(string guid) where T : Object
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
