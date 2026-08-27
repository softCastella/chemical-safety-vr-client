using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using ProjectHandPoseData = ThreeDUI.HandPoses.HandPoseData;

/// <summary>
/// Puts a captured <see cref="ProjectHandPoseData"/> onto the ghost hand skeleton it is attached to.
///
/// The component carries the pose asset rather than the pose being baked into the prefab's transforms, so
/// the shape stays editable after the fact: retouch the asset, hit Apply, and every ghost using it follows.
/// Baked transforms would have to be re-captured.
///
/// Bones are matched by name - "L_" or "R_" plus the joint id - which is the convention the GhostLeftHand
/// and GhostRightHand skeletons already use.
/// </summary>
[DisallowMultipleComponent]
public class GhostHandPose : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The captured pose this ghost shows.")]
    ProjectHandPoseData m_Pose;

    public ProjectHandPoseData pose
    {
        get => m_Pose;
        set => m_Pose = value;
    }

    void Reset() => Apply();

    /// <summary>
    /// Bends the fingers to the captured rotations, then puts the wrist where it was relative to the object
    /// being grabbed.
    ///
    /// The wrist is placed in world space rather than by posing this transform, because the wrist is a
    /// child: writing the root would move the hand by whatever offset the skeleton happens to carry. When
    /// there is no parent - a prefab opened on its own - the attach step is skipped and only the finger
    /// shape shows, which is the honest result since "relative to the grabbed object" has no meaning there.
    /// </summary>
    [ContextMenu("Apply Pose")]
    public void Apply()
    {
        if (m_Pose == null)
            return;

        var bones = new Dictionary<string, Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
            bones[child.name] = child;

        var prefix = m_Pose.BonePrefix;
        var applied = 0;

        foreach (var joint in m_Pose.joints)
        {
            // The wrist comes from the attach pose below; driving it from the joint list as well would fight
            // that and leave the hand wherever the tracking origin happened to be.
            if (joint.jointID == XRHandJointID.Wrist)
                continue;

            if (bones.TryGetValue(prefix + joint.jointID, out var bone))
            {
                bone.localRotation = joint.localRotation;
                applied++;
            }
        }

        if (applied == 0)
        {
            Debug.LogWarning($"[GhostHandPose] No bones matched '{prefix}<JointID>' on {name}. " +
                "Check that the skeleton's bone names follow that convention.", this);
            return;
        }

        var target = transform.parent;
        if (target == null || !m_Pose.hasAttachPose)
            return;

        if (bones.TryGetValue(prefix + XRHandJointID.Wrist, out var wrist))
        {
            wrist.position = target.TransformPoint(m_Pose.attachLocalPosition);
            wrist.rotation = target.rotation * m_Pose.attachLocalRotation;
        }
    }
}
