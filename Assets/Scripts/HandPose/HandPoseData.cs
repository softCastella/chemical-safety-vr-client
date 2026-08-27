using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Hands;

namespace ThreeDUI.HandPoses
{
    /// <summary>
    /// One captured hand shape: the local rotation of every tracked joint, plus where the wrist sat relative
    /// to the object being grabbed.
    ///
    /// Rotations rather than world positions, because the point of the asset is to survive being applied to a
    /// different skeleton instance at a different place in the scene - a grab pose is a shape, not a location.
    /// The wrist is the exception and is stored separately, in the grabbed object's local space, since that is
    /// what decides how the hand meets the object.
    /// </summary>
    [MovedFrom(true, null, "Assembly-CSharp", "HandPoseData")]
    [CreateAssetMenu(fileName = "HandPose", menuName = "Hand Pose/Hand Pose Data")]
    public class HandPoseData : ScriptableObject
    {
        public Handedness handedness = Handedness.Left;

        [Header("Wrist, in the grabbed object's local space")]
        [Tooltip("False when the pose was captured without a target, in which case only the finger shape is usable.")]
        public bool hasAttachPose;
        public Vector3 attachLocalPosition;
        public Quaternion attachLocalRotation = Quaternion.identity;

        public List<JointPose> joints = new List<JointPose>();

        /// <summary>An XR Hands joint and the local pose of the transform driving it.</summary>
        [Serializable]
        public struct JointPose
        {
            public XRHandJointID jointID;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        /// <summary>The bone-name prefix the ghost hand skeletons use, keyed off which hand this is.</summary>
        public string BonePrefix => handedness == Handedness.Left ? "L_" : "R_";
    }
}
