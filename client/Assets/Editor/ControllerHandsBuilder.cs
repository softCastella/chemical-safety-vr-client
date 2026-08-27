using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Hangs an animated hand off each controller of the rig that <see cref="XROriginInitializer"/> builds.
/// The steps are the ones written out in ControllerHands.md; having them here as well means a lesson
/// scene can be recreated in one click instead of a dozen inspector edits.
/// Running this repeatedly is safe, and a hand whose offset was already tuned in the headset keeps it.
/// </summary>
static class ControllerHandsBuilder
{
    // The hand models ship with the XR Hands HandVisualizer sample. Referencing the FBX directly rather
    // than the sample's tracking prefab is deliberate: that prefab carries an XRHandSkeletonDriver, and
    // the driver rewrites every bone each frame, which would fight the Animator.
    const string k_LeftModelGuid = "bf7151579c38e2a44be94ba8773876c1";
    const string k_RightModelGuid = "56186ccf27ad7864681108ed88349071";
    const string k_MaterialGuid = "4d81b60fa9c34e2b8f5701cd6a2e93b7";

    const string k_ControllerFolder = "Assets/HandPoses";
    const string k_VisualName = "Visual";
    const string k_OffsetName = "Hand Offset";

    /// <summary>
    /// Where the model has to sit so the hand lines up with the controller actually in the user's hand.
    /// OpenXR reports a grip pose - roughly the axis of the held controller - while the hand mesh is
    /// authored around the hand-joint pose, so the two are always a fixed rotation apart. These numbers
    /// were read off a tuned scene rather than derived, so treat them as a starting point.
    /// </summary>
    static readonly Vector3 k_LeftOffsetPosition = new Vector3(0.01f, 0.008f, 0.008f);
    static readonly Vector3 k_LeftOffsetEuler = new Vector3(-12.527f, 11.224f, 82.879f);
    static readonly Vector3 k_RightOffsetPosition = new Vector3(-0.0098f, 0.0005f, 0.0072f);
    static readonly Vector3 k_RightOffsetEuler = new Vector3(-12.015f, -14.724f, -93.536f);

    [MenuItem("Tools/XR/Build Controller Hands")]
    static void Build()
    {
        var built = 0;
        built += BuildHand("Left Controller", "LeftHand", k_LeftModelGuid, "L",
            HandGripAnimator.HandSide.Left, k_LeftOffsetPosition, k_LeftOffsetEuler) ? 1 : 0;
        built += BuildHand("Right Controller", "RightHand", k_RightModelGuid, "R",
            HandGripAnimator.HandSide.Right, k_RightOffsetPosition, k_RightOffsetEuler) ? 1 : 0;

        if (built == 0)
            return;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[XR] Built {built} controller hand(s).");
    }

    static bool BuildHand(string controllerName, string handName, string modelGuid, string side,
        HandGripAnimator.HandSide handSide, Vector3 offsetPosition, Vector3 offsetEuler)
    {
        var controller = FindController(controllerName);
        if (controller == null)
        {
            Debug.LogError($"[XR] No '{controllerName}' in the scene. Run Tools > XR > Initialize XR Origin first.");
            return false;
        }

        var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            $"{k_ControllerFolder}/HandAnimator_{side}.controller");
        if (animator == null)
        {
            Debug.LogError(
                $"[XR] {k_ControllerFolder}/HandAnimator_{side}.controller is missing. " +
                "Record the poses and run Tools > Hand Pose > Build Hand Animator first.");
            return false;
        }

        // Visual has a single child today. It is split out anyway so a controller mesh has somewhere to
        // go later without moving the hand and invalidating the offset tuned below it.
        var visual = EnsureChild(controller, k_VisualName, out _);
        var offset = EnsureChild(visual, k_OffsetName, out var offsetIsNew);

        // Only seed the offset on a node we just made. Overwriting would silently undo a tuning pass
        // that can only be done in the headset.
        if (offsetIsNew)
        {
            offset.localPosition = offsetPosition;
            offset.localEulerAngles = offsetEuler;
        }

        var hand = offset.Find(handName);
        if (hand == null)
        {
            var model = LoadModel(modelGuid);
            if (model == null)
            {
                Debug.LogError(
                    $"[XR] {handName} model not found. Import the HandVisualizer sample of the XR Hands package.");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = handName;
            hand = instance.transform;
            hand.SetParent(offset, false);
            hand.localPosition = Vector3.zero;
            hand.localRotation = Quaternion.identity;
        }

        ApplyMaterial(hand);

        GetOrAdd<Animator>(hand.gameObject).runtimeAnimatorController = animator;
        ApplySide(GetOrAdd<HandGripAnimator>(hand.gameObject), handSide);
        return true;
    }

    static Transform FindController(string name)
    {
        var origin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin == null)
            return null;

        foreach (var transform in origin.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == name)
                return transform;
        }

        return null;
    }

    /// <summary>
    /// Matches on the trimmed name so a node that picked up a stray space while being renamed in the
    /// hierarchy is repaired rather than shadowed by a second one.
    /// </summary>
    static Transform EnsureChild(Transform parent, string name, out bool created)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name.Trim() != name)
                continue;

            if (child.name != name)
            {
                Debug.Log($"[XR] Renamed '{child.name}' to '{name}'.");
                child.name = name;
            }

            created = false;
            return child;
        }

        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        created = true;
        return node;
    }

    static void ApplyMaterial(Transform hand)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(k_MaterialGuid));
        if (material == null)
        {
            Debug.LogWarning("[XR] Assets/Materials/Hand.mat is missing; the hand keeps the model's own material.");
            return;
        }

        foreach (var renderer in hand.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
    }

    /// <summary>The side is private on the component, so reach it the way the inspector does.</summary>
    static void ApplySide(HandGripAnimator component, HandGripAnimator.HandSide side)
    {
        var serialized = new SerializedObject(component);
        serialized.FindProperty("m_Side").enumValueIndex = (int)side;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject LoadModel(string guid)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
