using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(XROrigin))]
public sealed class XRStartPoseAligner : MonoBehaviour
{
    public Vector3 targetEyePosition = new Vector3(-0.472f, 1f, -12.14f);
    public Vector3 targetForward = Vector3.forward;
    [Min(1)] public int settleFrames = 3;
    [Min(1f)] public float deviceWaitSeconds = 10f;

    void Awake()
    {
        // The Starter Assets rig enables gravity and jumping by default. This
        // scene has a visual vessel floor but no walkable floor collider, so
        // those providers would make the entire rig fall forever.
        SetChildActive("Gravity", false);
        SetChildActive("Jump", false);
    }

    IEnumerator Start()
    {
        var origin = GetComponent<XROrigin>();

        for (var frame = 0; frame < settleFrames; frame++)
            yield return new WaitForEndOfFrame();

        var waitElapsed = 0f;
        while (!XRSettings.isDeviceActive && waitElapsed < deviceWaitSeconds)
        {
            waitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        var forward = Vector3.ProjectOnPlane(targetForward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        origin.MatchOriginUpCameraForward(Vector3.up, forward);
        origin.MoveCameraToWorldLocation(targetEyePosition);
    }

    void SetChildActive(string objectName, bool active)
    {
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                child.gameObject.SetActive(active);
                return;
            }
        }
    }
}
