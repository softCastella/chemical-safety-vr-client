using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Builds the world-space UI lesson in Lesson_07: the panel, and everything needed to make it respond.
///
/// The panel is a prefab and brings its own graphic raycasters, so most of the visible half arrives with
/// it. What cannot ride along is the half that exists once per scene: an EventSystem carrying
/// XRUIInputModule. Its absence looks exactly like every other UI failure - nothing happens - which is
/// why it is worth a menu item rather than a line in a checklist.
///
/// Nested canvases are fine here. XRI raycasts them through the root canvas's TrackedDeviceGraphicRaycaster,
/// so the sorting isolation the UI prefabs use costs nothing.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class UILessonBuilder
{
    const string k_EventSystemName = "EventSystem";
    const string k_PokeInteractorGuid = "27024f5809f4a4347b9cd7f26a1bdf93";

    const string k_PanelRootName = "2D UI";
    const string k_PanelPrefabGuid = "ac58bf72acf94a549b99fd5c989371e0";

    // Where the panel sits relative to the rig's start point: a little below eye level, just over a metre
    // out, turned to face back along +X. The prefab stores whatever position it was last saved at, which
    // is not necessarily in front of anything, so placement belongs here rather than in the asset.
    static readonly Vector3 k_PanelPosition = new(0f, -0.335f, -1.08f);
    static readonly Vector3 k_PanelRotation = new(0f, -90f, 0f);

    [MenuItem("Tools/XR/Build UI Lesson", priority = 25)]
    static void Build()
    {
        // First, because EnsureRaycasters only sees canvases that are already in the scene.
        var panel = EnsurePanel();

        var eventSystem = EnsureEventSystem();
        var raycasters = EnsureRaycasters();
        var pokes = EnsurePokeInteractors();

        EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);
        Selection.activeGameObject = panel != null ? panel : eventSystem.gameObject;
        Debug.Log($"[XR] UI wired: {(panel != null ? $"panel '{panel.name}'" : "no panel")}, EventSystem + " +
            $"XRUIInputModule, {raycasters} canvas raycaster(s), {pokes} poke interactor(s).");
    }

    /// <summary>
    /// The panel already in the scene, or a fresh instance of the prefab. Reusing beats instantiating -
    /// the scene copy is the one that has been positioned, and a second would land on top of it.
    /// </summary>
    static GameObject EnsurePanel()
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform.parent == null && transform.name == k_PanelRootName)
                return transform.gameObject;
        }

        var prefab = Load<GameObject>(k_PanelPrefabGuid);
        if (prefab == null)
        {
            Debug.LogWarning($"[XR] '{k_PanelRootName}' prefab not found; wiring whatever UI the scene already has.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Build UI Lesson");

        // Only on a fresh instance. Re-running the menu after nudging the panel into place should not
        // shove it back to the default.
        instance.transform.SetLocalPositionAndRotation(k_PanelPosition, Quaternion.Euler(k_PanelRotation));
        instance.transform.localScale = Vector3.one;

        return instance;
    }

    static EventSystem EnsureEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
            eventSystem = new GameObject(k_EventSystemName).AddComponent<EventSystem>();

        var go = eventSystem.gameObject;

        // Unity adds a mouse-and-keyboard module alongside every EventSystem it creates. It knows nothing
        // about XR rays, and with both present the two modules take turns claiming the input, which reads
        // as the UI responding only sometimes.
        var standalone = go.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
            Debug.Log("[XR] Removed StandaloneInputModule; XR rays are not mouse input.");
        }

        if (go.GetComponent<XRUIInputModule>() == null)
            go.AddComponent<XRUIInputModule>();

        return eventSystem;
    }

    /// <summary>
    /// A canvas is hit by a tracked ray only through this raycaster - and "a canvas" means every one of
    /// them, nested included.
    ///
    /// It is tempting to add it to root canvases only, on the reasoning that children share the root's.
    /// They do not. UGUI registers each Graphic against its nearest enabled Canvas, and the raycaster only
    /// ever looks up the registration of the canvas on its own GameObject, so the graphics inside a nested
    /// canvas are invisible to the root's raycaster. XRI's own UI prefabs carry one on every canvas meant
    /// to be clicked, which is why the sample panels work despite the reasoning being wrong - a hand-built
    /// nested canvas silently would not.
    /// </summary>
    static int EnsureRaycasters()
    {
        var added = 0;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            // A nested canvas reports the root's render mode rather than its own serialized value; going
            // through rootCanvas says that outright instead of leaning on it.
            if (canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
                continue;

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() != null)
                continue;

            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            EditorUtility.SetDirty(canvas);
            added++;
        }

        return added;
    }

    /// <summary>
    /// Adds the poke interactor the rig from XROriginInitializer leaves out, so panels within arm's reach
    /// can be pressed with a fingertip instead of aimed at.
    /// </summary>
    static int EnsurePokeInteractors()
    {
        var origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogWarning("[XR] No XR Origin in the scene; skipping poke interactors.");
            return 0;
        }

        var prefab = Load<GameObject>(k_PokeInteractorGuid);
        if (prefab == null)
        {
            Debug.LogWarning("[XR] Poke Interactor prefab not found; UI will be usable by ray only.");
            return 0;
        }

        var added = 0;
        foreach (var name in new[] { "Left Controller", "Right Controller" })
        {
            var controller = Find(origin.transform, name);
            if (controller == null || controller.Find(prefab.name) != null)
                continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(controller, false);
            added++;
        }

        return added;
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

    static T Load<T>(string guid) where T : Object
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
