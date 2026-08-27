using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class XRLineVisualTrackingGate : MonoBehaviour
{
    [SerializeField] private XRNode xrNode = XRNode.RightHand;
    [SerializeField] private LineRenderer lineRenderer;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        xrNode = ResolveHandFromHierarchy(transform);
    }

    private void LateUpdate()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(xrNode);
        bool isTracked = device.isValid
            && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked)
            && tracked;

        if (lineRenderer != null)
            lineRenderer.enabled = isTracked;
    }

    private static XRNode ResolveHandFromHierarchy(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            string name = current.name;
            if (name.Contains("Left"))
                return XRNode.LeftHand;
            if (name.Contains("Right"))
                return XRNode.RightHand;
        }

        return XRNode.RightHand;
    }
}
