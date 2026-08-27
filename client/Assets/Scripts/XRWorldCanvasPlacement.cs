using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class XRWorldCanvasPlacement : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0.1f)] private float distance = 1.2f;
    [SerializeField, Min(0)] private int trackingWarmupFrames = 3;
    [SerializeField] private bool keepAtEyeHeight = true;
    [SerializeField] private float verticalOffset;

    public void PlaceFromCamera()
    {
        StartCoroutine(PlaceAfterTrackingWarmup());
    }

    private IEnumerator PlaceAfterTrackingWarmup()
    {
        for (int frame = 0; frame < trackingWarmupFrames; frame++)
            yield return new WaitForEndOfFrame();

        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            Debug.LogError("XRWorldCanvasPlacement: Main Camera를 찾을 수 없습니다.", this);
            yield break;
        }

        Transform cameraTransform = cameraToUse.transform;
        Vector3 forward = cameraTransform.forward;
        Vector3 up = cameraTransform.up;
        if (keepAtEyeHeight)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            up = Vector3.up;
        }

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(
            cameraTransform.position + forward * distance + Vector3.up * verticalOffset,
            Quaternion.LookRotation(forward, up));
    }
}
