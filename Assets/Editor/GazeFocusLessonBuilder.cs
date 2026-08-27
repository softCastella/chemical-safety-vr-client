using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Builds Lesson_06 into the scene that is already open, laying Focus and Gaze out as two separate
/// areas.
///
/// The separation is the lesson. Focus lives within arm's reach because it is gained by grabbing, and
/// the gaze targets sit two metres out where hands cannot get to them - so the only way to select one
/// is to look at it. Mixing them in one pile would let a student grab a gaze target and never notice
/// which mechanism they were actually using.
///
/// Unlike Tools > Gaze > Build Gaze Test Scene, this adds to the open scene instead of replacing it.
/// The lesson is taught Focus-first even though the menu reads Gaze first: focus is already switched on
/// by default and needs no rig changes, so it is the shorter road into the pair.
/// Running it repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class GazeFocusLessonBuilder
{
    const string k_FocusRootName = "Focus Area";
    const string k_GazeRootName = "Gaze Area";
    const string k_CubeName = "Focus Cube";
    const string k_MaterialGuid = "7a3f9d21c8e64b5ea0d1f4c67b829e35";

    const float k_CubeSize = 0.1f;

    /// <summary>Within reach, side by side, so focus can be handed back and forth quickly.</summary>
    static readonly Vector3[] k_CubePositions =
    {
        new Vector3(-0.15f, 1.2f, 0.457f),
        new Vector3(0.15f, 1.2f, 0.457f),
    };

    /// <summary>
    /// Out of arm's reach on purpose - looking is the only way to reach these. The height is picked to
    /// sit at the same angle below the horizon that the gaze ray is tilted to, so the targets land where
    /// the reticle already rests instead of asking the user to crane upward.
    /// </summary>
    static readonly Vector3 k_GazeOrigin = new Vector3(0f, 1.25f, 2.5f);

    /// <summary>Degrees the gaze ray points below the horizon. Eyes rest a little below level.</summary>
    const float k_GazePitch = 8f;

    [MenuItem("Tools/XR/Build Gaze & Focus Lesson", priority = 24)]
    static void Build()
    {
        BuildFocusArea();

        var gazeRoot = GazeTargetBuilder.BuildTargets(k_GazeRootName, k_GazeOrigin);
        GazeTargetBuilder.BuildTools();
        EnsureGazeInteractor();

        EditorSceneManager.MarkSceneDirty(gazeRoot.scene);
        Selection.activeGameObject = gazeRoot;
        Debug.Log("[XR] Lesson_06 built: focus cubes within reach, gaze targets 2.5m out.");
    }

    static void BuildFocusArea()
    {
        var existing = GameObject.Find(k_FocusRootName);
        var root = existing != null ? existing : new GameObject(k_FocusRootName);
        root.transform.position = Vector3.zero;

        for (var i = 0; i < k_CubePositions.Length; i++)
            BuildCube(root.transform, i);
    }

    static void BuildCube(Transform parent, int index)
    {
        var name = $"{k_CubeName} {index + 1:00}";

        var cube = parent.Find(name);
        if (cube == null)
        {
            var created = GameObject.CreatePrimitive(PrimitiveType.Cube);
            created.name = name;
            created.transform.SetParent(parent, false);
            cube = created.transform;
        }

        cube.localPosition = k_CubePositions[index];
        cube.localRotation = Quaternion.identity;
        cube.localScale = Vector3.one * k_CubeSize;

        var grab = GetOrAdd<XRGrabInteractable>(cube.gameObject);

        // Focus Mode already defaults to Single, so nothing is switched on here. Saying so is the point
        // of the lesson: these cubes were focusable before anyone touched a setting.
        grab.GetComponent<Rigidbody>().useGravity = false;
        grab.throwOnDetach = false;

        // The property block driven by FocusHighlight needs a material with _BaseColor, and sharing one
        // across both cubes is what the block exists for.
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(k_MaterialGuid));
        if (material != null)
            cube.GetComponent<Renderer>().sharedMaterial = material;

        GetOrAdd<FocusHighlight>(cube.gameObject);
        EditorUtility.SetDirty(cube.gameObject);
    }

    /// <summary>
    /// The rig from XROriginInitializer ships with near-far interactors only, so gaze has to be added.
    /// </summary>
    static void EnsureGazeInteractor()
    {
        var origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogWarning("[XR] No XR Origin in the scene; the gaze targets will have nothing looking at them.");
            return;
        }

        var gaze = GazeTargetBuilder.EnsureInteractor(origin);
        GazeTargetBuilder.SetRayPitch(gaze, k_GazePitch);
        GazeTargetBuilder.ForceAlwaysOn(origin.gameObject);

        EnsureGroups(origin, gaze);
    }

    /// <summary>
    /// Focus is handed out per Interaction Group, and an interactor that belongs to no group never gets
    /// any - silently. The rig from XROriginInitializer has no groups, so without this the focus cubes
    /// would look completely broken while every other lesson kept working.
    /// </summary>
    static void EnsureGroups(XROrigin origin, XRGazeInteractor gaze)
    {
        foreach (var name in new[] { "Left Controller", "Right Controller" })
        {
            var controller = Find(origin.transform, name);
            if (controller == null)
                continue;

            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>(true);
            GazeTargetBuilder.EnsureGroup(controller.gameObject, interactors);
        }

        // The gaze interactor sits beside the camera rather than under a controller, so it needs a group
        // of its own for a gaze-driven selection to grant focus too.
        if (gaze != null)
            GazeTargetBuilder.EnsureGroup(gaze.gameObject, gaze);
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

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
