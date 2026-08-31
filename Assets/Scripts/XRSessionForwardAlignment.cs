using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-31000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(XROrigin))]
public sealed class XRSessionForwardAlignment : MonoBehaviour
{
    [Header("Authored Scene Direction")]
    [SerializeField] private Vector3 m_AuthoredForward = Vector3.forward;

    [Header("Tracking Readiness")]
    [SerializeField, Min(1)] private int m_TrackingWarmupFrames = 3;
    [SerializeField, Min(0f)] private float m_TrackingWaitSeconds = 10f;

    private static bool s_HasSessionAlignment;
    private static Quaternion s_TrackingSpaceToWorldYaw = Quaternion.identity;

    private XROrigin m_Origin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionAlignment()
    {
        s_HasSessionAlignment = false;
        s_TrackingSpaceToWorldYaw = Quaternion.identity;
    }

    private IEnumerator Start()
    {
        m_Origin = GetComponent<XROrigin>();
        if (m_Origin.Camera == null)
        {
            Debug.LogError(
                $"{nameof(XRSessionForwardAlignment)} on '{name}' requires the authored XROrigin camera reference.",
                this);
            yield break;
        }

        for (int frame = 0; frame < m_TrackingWarmupFrames; frame++)
            yield return null;

        if (!s_HasSessionAlignment)
        {
            float deadline = Time.realtimeSinceStartup + m_TrackingWaitSeconds;
            while (!TryCaptureSessionAlignment())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Debug.LogError(
                        $"{nameof(XRSessionForwardAlignment)} could not read a valid head rotation within " +
                        $"{m_TrackingWaitSeconds:0.##} seconds. The authored XR Origin rotation was preserved.",
                        this);
                    yield break;
                }

                yield return null;
            }
        }

        m_Origin.transform.rotation = s_TrackingSpaceToWorldYaw;
        Debug.Log(
            $"[XR Session Forward] Applied session yaw {s_TrackingSpaceToWorldYaw.eulerAngles.y:0.##}° " +
            $"to scene '{gameObject.scene.name}'.",
            this);
    }

    private bool TryCaptureSessionAlignment()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!head.isValid ||
            !head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation) ||
            !IsFinite(headRotation) ||
            Quaternion.Dot(headRotation, headRotation) < 0.5f)
        {
            return false;
        }

        Vector3 trackingForward = Vector3.ProjectOnPlane(headRotation * Vector3.forward, Vector3.up);
        Vector3 authoredForward = Vector3.ProjectOnPlane(m_AuthoredForward, Vector3.up);
        if (trackingForward.sqrMagnitude < 0.001f || authoredForward.sqrMagnitude < 0.001f)
        {
            Debug.LogError(
                $"{nameof(XRSessionForwardAlignment)} on '{name}' requires a non-vertical authored forward vector.",
                this);
            return false;
        }

        trackingForward.Normalize();
        authoredForward.Normalize();
        s_TrackingSpaceToWorldYaw = Quaternion.FromToRotation(trackingForward, authoredForward);
        s_HasSessionAlignment = true;
        return true;
    }

    private static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
