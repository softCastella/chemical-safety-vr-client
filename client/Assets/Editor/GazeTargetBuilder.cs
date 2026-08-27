using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// The gaze targets themselves, shared by the standalone gaze scene and by Lesson_06.
///
/// Split out so the two entry points cannot drift apart: one replaces the open scene and the other adds
/// to it, but what a "dwell select 2s" target means has to stay the same in both.
/// </summary>
static class GazeTargetBuilder
{
    const string k_FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string k_MaterialFolder = "Assets/Materials";
    const string k_GazeInteractorGuid = "b84cd05e1160fe34cab2585022c8cd99";

    const string k_RayOriginName = "Ray Origin";

    const float k_TargetScale = 0.3f;

    /// <summary>
    /// Roughly 8cm tall in the world. The label used to be parented to the sphere and scaled back up to
    /// undo the sphere's own scale, which made the real size hard to reason about and far too large;
    /// hanging it off the area root instead means this number means what it says.
    /// </summary>
    const float k_FontSize = 0.08f;

    /// <summary>
    /// Builds the four targets, one per gaze mode, as children of a new root at <paramref name="origin"/>.
    /// </summary>
    public static GameObject BuildTargets(string rootName, Vector3 origin)
    {
        var existing = GameObject.Find(rootName);
        var root = existing != null ? existing : new GameObject(rootName);
        root.transform.position = origin;

        var material = GetOrCreateMaterial("Mat_GazeTarget", Color.white);

        // name, x offset, allowGazeSelect, dwell override (0 = use interactor default), assistance
        CreateTarget(root.transform, material, "01 Hover Only", -1.35f, false, 0f, false,
            "Hover Only\nno select");
        CreateTarget(root.transform, material, "02 Dwell Select 1s", -0.45f, true, 0f, false,
            "Dwell Select\n1s (interactor default)");
        CreateTarget(root.transform, material, "03 Dwell Select 2s", 0.45f, true, 2f, false,
            "Dwell Select 2s\nauto-deselect 4s");
        CreateTarget(root.transform, material, "04 Gaze Assistance", 1.35f, false, 0f, true,
            "Gaze Assistance\nsnaps controller ray");

        return root;
    }

    /// <summary>
    /// The reticle and the logger only make sense where a gaze interactor exists, so they live in the
    /// scene as ordinary objects instead of attaching themselves to whatever scene happens to run.
    /// </summary>
    public static void BuildTools()
    {
        if (GameObject.Find("Gaze Tools") != null)
            return;

        var go = new GameObject("Gaze Tools");
        go.AddComponent<GazeReticle>();
        go.AddComponent<GazeDebugLogger>();
    }

    /// <summary>
    /// In the DemoScene the gaze interactor starts disabled and a trigger volume switches it on.
    /// A lesson should not require walking anywhere, so enable it up front.
    /// </summary>
    public static void ForceAlwaysOn(GameObject rig)
    {
        var interactor = rig.GetComponentInChildren<XRGazeInteractor>(true);
        if (interactor != null)
        {
            interactor.gameObject.SetActive(true);

            // The ray origin lives on a sibling stabilizer object that is also disabled by default.
            if (interactor.rayOriginTransform != null)
                interactor.rayOriginTransform.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[GAZE] No XRGazeInteractor found under the rig.");
        }

        // Needed for the gaze-assistance target to actually do anything; ships disabled.
        var assistance = rig.GetComponentInChildren<XRGazeAssistance>(true);
        if (assistance != null)
            assistance.enabled = true;
    }

    /// <summary>
    /// Adds a gaze interactor to a rig that has none - the one built by XROriginInitializer has only the
    /// near-far interactors.
    ///
    /// It belongs beside the camera, not under it. The prefab carries its own TrackedPoseDriver reading
    /// XRI Head Position and Rotation, so parenting it to the camera applies the head pose twice and the
    /// ray ends up pointing somewhere off to the side of where the user is actually looking. Camera
    /// Offset is the shared parent the Starter Assets rig uses for exactly this reason.
    /// </summary>
    public static XRGazeInteractor EnsureInteractor(XROrigin origin)
    {
        var parent = origin != null && origin.CameraFloorOffsetObject != null
            ? origin.CameraFloorOffsetObject.transform
            : null;

        if (parent == null)
        {
            Debug.LogError("[GAZE] The XR Origin has no Camera Offset, so there is nowhere to put the gaze interactor.");
            return null;
        }

        var existing = origin.GetComponentInChildren<XRGazeInteractor>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);

            // An interactor parented to the camera by an earlier version of this builder would track the
            // head twice; move it back beside the camera rather than leaving a scene that aims wrong.
            if (existing.transform.parent != parent)
            {
                existing.transform.SetParent(parent, false);
                existing.transform.localPosition = Vector3.zero;
                existing.transform.localRotation = Quaternion.identity;
                Debug.Log("[GAZE] Moved the gaze interactor from the camera to Camera Offset.");
            }

            return existing;
        }

        var path = AssetDatabase.GUIDToAssetPath(k_GazeInteractorGuid);
        var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("[GAZE] Gaze Interactor prefab not found. Import the Starter Assets sample first.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.SetActive(true);

        return instance.GetComponent<XRGazeInteractor>();
    }

    /// <summary>
    /// Tilts the gaze ray down a few degrees by casting from a child transform rather than the
    /// interactor itself.
    ///
    /// The reticle always sits at the centre of the ray, so the only way to move it off the exact centre
    /// of the view is to aim the ray. A head held perfectly level is not a resting posture - eyes settle
    /// slightly below the horizon - so a dead-centre reticle means holding the neck up the whole time.
    /// The interactor's own rotation cannot carry this: its TrackedPoseDriver overwrites it every frame.
    /// </summary>
    public static void SetRayPitch(XRGazeInteractor interactor, float degreesDown)
    {
        if (interactor == null)
            return;

        var origin = interactor.transform.Find(k_RayOriginName);
        if (origin == null)
        {
            origin = new GameObject(k_RayOriginName).transform;
            origin.SetParent(interactor.transform, false);
        }

        origin.localPosition = Vector3.zero;
        origin.localRotation = Quaternion.Euler(degreesDown, 0f, 0f);

        interactor.rayOriginTransform = origin;
        EditorUtility.SetDirty(interactor);
    }

    /// <summary>
    /// Puts the interactor in an Interaction Group, without which focus never happens.
    ///
    /// XRInteractionManager gives up early when the interactor has no group:
    /// <code>
    /// var group = interactor is IXRGroupMember groupMember ? groupMember.containingGroup : null;
    /// if (group == null)
    ///     return;
    /// </code>
    /// No warning, no error - focusEntered simply never fires. The Starter Assets rig has a group on each
    /// controller, but the rig XROriginInitializer builds does not, so Lesson_06 has to add them.
    /// </summary>
    public static void EnsureGroup(GameObject holder, params Component[] members)
    {
        var group = holder.GetComponent<XRInteractionGroup>();
        if (group == null)
            group = holder.AddComponent<XRInteractionGroup>();

        var list = new List<Object>();
        foreach (var member in members)
        {
            if (member != null && !list.Contains(member))
                list.Add(member);
        }

        if (list.Count == 0)
            return;

        group.startingGroupMembers = list;
        EditorUtility.SetDirty(group);
    }

    static void CreateTarget(Transform parent, Material material, string name, float x,
        bool allowGazeSelect, float dwellOverride, bool assistance, string label)
    {
        var existing = parent.Find(name);
        GameObject sphere;
        if (existing != null)
        {
            sphere = existing.gameObject;
        }
        else
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
        }

        sphere.transform.localPosition = new Vector3(x, 0f, 0f);
        sphere.transform.localScale = Vector3.one * k_TargetScale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        var interactable = GetOrAdd<XRSimpleInteractable>(sphere);
        interactable.allowGazeInteraction = true;
        interactable.allowGazeSelect = allowGazeSelect;
        interactable.allowGazeAssistance = assistance;

        if (dwellOverride > 0f)
        {
            interactable.overrideGazeTimeToSelect = true;
            interactable.gazeTimeToSelect = dwellOverride;
            interactable.overrideTimeToAutoDeselectGaze = true;
            interactable.timeToAutoDeselectGaze = 4f;
        }

        GetOrAdd<GazeTestTarget>(sphere);

        // Focus is gained by whoever selects, gaze included. Highlighting it here is what lets the two
        // halves of Lesson_06 be compared side by side.
        GetOrAdd<FocusHighlight>(sphere);

        CreateLabel(parent, name, new Vector3(x, -0.3f, 0f), label);
        EditorUtility.SetDirty(sphere);
    }

    /// <summary>
    /// Sibling of the sphere rather than a child of it, so the text is not fighting the sphere's scale.
    /// </summary>
    static void CreateLabel(Transform parent, string targetName, Vector3 position, string text)
    {
        var name = $"{targetName} Label";
        var existing = parent.Find(name);

        var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(k_FontPath);
        if (font == null)
        {
            // Labels are a convenience, not a requirement - the target names still identify each mode.
            Debug.LogWarning("[GAZE] TMP font asset not found; skipping target labels.");
            return;
        }

        TMPro.TextMeshPro tmp;
        if (existing != null)
        {
            tmp = existing.GetComponent<TMPro.TextMeshPro>();
        }
        else
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(0.8f, 0.3f);
            tmp = go.AddComponent<TMPro.TextMeshPro>();
        }

        tmp.transform.localPosition = position;
        tmp.transform.localRotation = Quaternion.identity;
        tmp.transform.localScale = Vector3.one;
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = k_FontSize;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    /// <summary>
    /// Kept in the shared Materials folder rather than beside a scene, because two scenes now point at
    /// it - deleting and recreating it on every rebuild would leave the other one pink.
    /// </summary>
    public static Material GetOrCreateMaterial(string name, Color color)
    {
        var path = $"{k_MaterialFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(k_MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = name };

        // URP Lit exposes _BaseColor; the built-in fallback exposes _Color.
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        AssetDatabase.CreateAsset(material, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
