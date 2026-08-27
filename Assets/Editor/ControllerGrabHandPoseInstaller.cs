using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectControllerGrabHandPose = ThreeDUI.HandPoses.ControllerGrabHandPose;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Puts <see cref="ProjectControllerGrabHandPose"/> on the controller hands and fills in its references, so a
/// captured grab pose shows up on the real hand without anyone hunting through the rig.
///
/// A menu item rather than a button in Ghost Hand Recorder: this is rig setup done once per scene, while
/// that window is used once per pose. Keeping them apart is what lets the recorder stay at three fields.
///
/// The one thing worth checking by hand afterwards is bone naming - see the warning this logs.
/// Running this repeatedly is safe; existing components are reused rather than duplicated.
/// </summary>
static class ControllerGrabHandPoseInstaller
{
    [MenuItem("Tools/Hand Pose/Setup Controller Grab Poses")]
    static void SetupFromMenu()
    {
        if (Setup() == 0)
            Debug.LogWarning("[HandPose] Nothing was set up.");
    }

    /// <summary>
    /// Wires the controller side of a grab pose and reports how many hands were done. Exposed so Ghost Hand
    /// Recorder can finish the job in one press - the object half and the hand half are useless apart, and
    /// splitting them across two menus is how one of them ends up forgotten.
    /// </summary>
    internal static int Setup()
    {
        var installed = 0;

        foreach (var (name, handedness) in new[]
                 {
                     ("Left Controller", Handedness.Left),
                     ("Right Controller", Handedness.Right),
                 })
        {
            var controller = FindInScene(name);
            if (controller == null)
            {
                Debug.LogWarning($"[HandPose] No '{name}' in the scene. Run Tools > XR > Initialize XR Origin first.");
                continue;
            }

            if (Install(controller, handedness))
                installed++;
        }

        if (installed > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        return installed;
    }

    static bool Install(Transform controller, Handedness handedness)
    {
        var poser = controller.GetComponent<ProjectControllerGrabHandPose>();
        if (poser == null)
            poser = Undo.AddComponent<ProjectControllerGrabHandPose>(controller.gameObject);

        var interactor = controller.GetComponentInChildren<XRBaseInteractor>(true);
        var animator = controller.GetComponentInChildren<Animator>(true);
        var handRoot = animator != null ? animator.transform : null;

        var serialized = new SerializedObject(poser);
        serialized.FindProperty("m_Handedness").enumValueIndex = (int)handedness;

        // Only written when found, so re-running after a rig change does not blank out references that were
        // set by hand.
        if (interactor != null)
            serialized.FindProperty("m_Interactor").objectReferenceValue = interactor;

        if (animator != null)
            serialized.FindProperty("m_HandAnimator").objectReferenceValue = animator;

        if (handRoot != null)
            serialized.FindProperty("m_HandRoot").objectReferenceValue = handRoot;

        serialized.ApplyModifiedProperties();

        if (interactor == null)
            Debug.LogWarning($"[HandPose] '{controller.name}' has no interactor under it, so nothing will " +
                "trigger the pose.", controller);

        if (animator == null)
            Debug.LogWarning($"[HandPose] '{controller.name}' has no hand Animator under it. Without one " +
                "there is no grip animation to switch off - fine if the hand is not animated, wrong if it is.",
                controller);

        AlignInteractorAttachToWrist(interactor, handedness);
        WarnAboutBoneNames(controller, handedness, handRoot);
        return true;
    }

    /// <summary>
    /// Points the interactor's attach point at the hand model's wrist bone.
    ///
    /// This is what makes a captured pose land correctly, and it is not obvious. Grabbing aligns the
    /// object's attach transform to the interactor's, and the object's was captured as "where the wrist was"
    /// - so unless the interactor's attach point is also the wrist, the two are measured from different
    /// places and the object ends up held at the controller origin rather than in the hand.
    ///
    /// The bone is referenced directly instead of copying its pose into a helper object, so the attach point
    /// tracks the live wrist rather than freezing wherever it happened to be when this was run.
    ///
    /// NearFarInteractor needs both writes. Its attach transform is regenerated at runtime by
    /// InteractionAttachController, so setting only the serialized field is silently discarded - the anchor
    /// follows Transform To Follow instead.
    /// </summary>
    static void AlignInteractorAttachToWrist(XRBaseInteractor interactor, Handedness handedness)
    {
        if (interactor == null)
            return;

        var wrist = FindWrist(interactor.transform, handedness);
        if (wrist == null)
        {
            Debug.LogWarning($"[HandPose] No '{(handedness == Handedness.Right ? "R_" : "L_")}Wrist' under " +
                $"'{interactor.name}'. The grab point cannot be aligned to the hand, so grabbed objects will " +
                "sit at the controller origin instead of in the hand.", interactor);
            return;
        }

        var serialized = new SerializedObject(interactor);
        var property = serialized.FindProperty("m_AttachTransform");
        if (property != null)
        {
            property.objectReferenceValue = wrist;
            serialized.ApplyModifiedProperties();
        }

        // Found by type name rather than referenced directly: not every interactor has one, and a rig
        // without it should still get the field above.
        var wired = false;
        foreach (var component in interactor.GetComponents<Component>())
        {
            if (component == null || component.GetType().Name != "InteractionAttachController")
                continue;

            var controllerSerialized = new SerializedObject(component);
            var follow = controllerSerialized.FindProperty("m_TransformToFollow");
            if (follow == null)
                continue;

            follow.objectReferenceValue = wrist;
            controllerSerialized.ApplyModifiedProperties();
            wired = true;
            break;
        }

        if (!wired && interactor is NearFarInteractor)
            Debug.LogWarning($"[HandPose] '{interactor.name}' is a NearFarInteractor but has no " +
                "InteractionAttachController, so Transform To Follow could not be set. Its attach point is " +
                "rebuilt at runtime and will ignore the field that was just assigned.", interactor);
    }

    static Transform FindWrist(Transform interactor, Handedness handedness)
    {
        var wristName = (handedness == Handedness.Right ? "R_" : "L_") + "Wrist";

        // Walks up from the interactor because the hand model is usually a sibling under the controller
        // rather than a child of the interactor itself.
        for (var current = interactor; current != null; current = current.parent)
        {
            foreach (var bone in current.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == wristName)
                    return bone;
            }
        }

        return null;
    }

    /// <summary>
    /// The pose is applied by matching bone names against "L_"/"R_" plus the joint id, which is what the
    /// ghost skeletons use. A controller hand rigged to any other convention silently matches nothing, so it
    /// is worth saying now rather than after a confusing play test.
    /// </summary>
    static void WarnAboutBoneNames(Transform controller, Handedness handedness, Transform handRoot)
    {
        if (handRoot == null)
            return;

        var prefix = handedness == Handedness.Right ? "R_" : "L_";
        var probe = prefix + XRHandJointID.IndexProximal;

        foreach (var bone in handRoot.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == probe)
                return;
        }

        Debug.LogWarning($"[HandPose] No bone named '{probe}' under '{handRoot.name}'. The hand on " +
            $"'{controller.name}' uses different bone names from the ghost, so poses will not apply. " +
            "Rename the bones or add a mapping.", handRoot);
    }

    static Transform FindInScene(string name)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform.name == name)
                return transform;
        }

        return null;
    }
}
