using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using ProjectHandPoseData = ThreeDUI.HandPoses.HandPoseData;

namespace ThreeDUI.HandPoses
{
/// <summary>
/// Bends the controller hand into the captured grab pose while it is holding something, so the live hand
/// matches the ghost that was recorded on that object.
///
/// The pose is read from the ghost itself: <see cref="GhostHandPose"/> sits on a ghost hand parented under
/// the grabbable object, and the one whose handedness matches wins. That means capturing a pose is the only
/// authoring step - there is no second list of poses to keep in sync with the ghosts, and an object with no
/// ghost for this hand simply keeps its normal animation.
/// </summary>
[MovedFrom(true, null, "Assembly-CSharp", "ControllerGrabHandPose")]
[DisallowMultipleComponent]
public class ControllerGrabHandPose : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Interactor whose grabs drive the pose. Found in children when empty.")]
    XRBaseInteractor m_Interactor;

    [SerializeField]
    [Tooltip("Animator driving the hand. Switched off while a pose is applied, then back on.")]
    Animator m_HandAnimator;

    [SerializeField]
    [Tooltip("Root containing the L_/R_ bones. Falls back to the animator's object.")]
    Transform m_HandRoot;

    [SerializeField]
    [Tooltip("Which hand's bones this drives. Decides the L_/R_ bone prefix.")]
    Handedness m_Handedness = Handedness.Left;

    bool m_PoseApplied;

    string BonePrefix => m_Handedness == Handedness.Right ? "R_" : "L_";

    void Reset()
    {
        ResolveReferences();

        // A sensible first guess so the component is usually right the moment it is added; the field stays
        // editable because rig naming is a convention, not a guarantee.
        if (name.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
            m_Handedness = Handedness.Right;
    }

    void OnEnable()
    {
        ResolveReferences();

        if (m_Interactor == null)
        {
            Debug.LogWarning("[ControllerGrabHandPose] No interactor found; grab poses will not be applied.", this);
            return;
        }

        m_Interactor.selectEntered.AddListener(OnSelectEntered);
        m_Interactor.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (m_Interactor != null)
        {
            m_Interactor.selectEntered.RemoveListener(OnSelectEntered);
            m_Interactor.selectExited.RemoveListener(OnSelectExited);
        }

        RestoreAnimator();
    }

    void ResolveReferences()
    {
        if (m_Interactor == null)
            m_Interactor = GetComponentInChildren<XRBaseInteractor>(true);

        if (m_HandAnimator == null)
            m_HandAnimator = GetComponentInChildren<Animator>(true);

        if (m_HandRoot == null)
            m_HandRoot = m_HandAnimator != null ? m_HandAnimator.transform : transform;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var pose = FindPose(args.interactableObject);
        if (pose != null)
            ApplyPose(pose);
    }

    void OnSelectExited(SelectExitEventArgs args) => RestoreAnimator();

    /// <summary>
    /// Looks for a ghost hand under the grabbed object that was captured for this hand. Searched including
    /// inactive objects, because a ghost is usually left hidden - it is a marker for authoring, not
    /// something the player should see.
    /// </summary>
    ProjectHandPoseData FindPose(IXRSelectInteractable interactable)
    {
        if (interactable?.transform == null)
            return null;

        foreach (var ghost in interactable.transform.GetComponentsInChildren<GhostHandPose>(true))
        {
            var pose = ghost.pose;
            if (pose != null && pose.handedness == m_Handedness)
                return pose;
        }

        return null;
    }

    void ApplyPose(ProjectHandPoseData pose)
    {
        if (m_HandRoot == null)
        {
            Debug.LogWarning("[ControllerGrabHandPose] No hand root, so the pose cannot be applied.", this);
            return;
        }

        // Off first, and this is the whole trick: the grip animation writes the same bones every frame, so
        // leaving it on means the pose is overwritten before it is ever seen.
        if (m_HandAnimator != null)
            m_HandAnimator.enabled = false;

        var bones = new Dictionary<string, Transform>();
        foreach (var bone in m_HandRoot.GetComponentsInChildren<Transform>(true))
            bones[bone.name] = bone;

        var prefix = BonePrefix;
        var applied = 0;

        foreach (var joint in pose.joints)
        {
            // The wrist is where the hand meets the object, and that is the interactable's attach transform
            // to decide. Writing it here would drag the whole hand off the grab point.
            if (joint.jointID == XRHandJointID.Wrist)
                continue;

            if (bones.TryGetValue(prefix + joint.jointID, out var target))
            {
                target.localRotation = joint.localRotation;
                applied++;
            }
        }

        if (applied == 0)
        {
            Debug.LogWarning($"[ControllerGrabHandPose] No bones matched '{prefix}<JointID>' under " +
                $"{m_HandRoot.name}. The controller hand's bone names differ from the ghost's.", this);
            RestoreAnimator();
            return;
        }

        m_PoseApplied = true;
    }

    void RestoreAnimator()
    {
        if (!m_PoseApplied)
            return;

        if (m_HandAnimator != null)
            m_HandAnimator.enabled = true;

        m_PoseApplied = false;
    }
}
}
