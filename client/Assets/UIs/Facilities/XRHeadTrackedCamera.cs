using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class XRHeadTrackedCamera : MonoBehaviour
{
    Vector3 originPosition;
    Quaternion originRotation;

    void OnEnable()
    {
        originPosition = transform.localPosition;
        originRotation = transform.localRotation;
        Application.onBeforeRender += UpdatePose;
    }

    void OnDisable() => Application.onBeforeRender -= UpdatePose;

    void Update() => UpdatePose();

    void UpdatePose()
    {
        if (!XRSettings.isDeviceActive)
            return;

        var head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
        if (!head.isValid)
            return;

        if (head.TryGetFeatureValue(CommonUsages.centerEyePosition, out var position))
            transform.localPosition = originPosition + originRotation * position;

        if (head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out var rotation))
            transform.localRotation = originRotation * rotation;
    }
}
