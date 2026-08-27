using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(XROrigin))]
public sealed class XRScaleTestEyeHeightAligner : MonoBehaviour
{
    [SerializeField] private bool alignOnStart = true;
    [SerializeField] private float floorWorldY;
    [SerializeField, Min(0.1f)] private float targetEyeHeight = 1.65f;
    [SerializeField, Min(0)] private int trackingWarmupFrames = 3;
    [SerializeField, Min(0f)] private float deviceWaitSeconds = 10f;

    private IEnumerator Start()
    {
        if (!alignOnStart)
            yield break;

        for (int frame = 0; frame < trackingWarmupFrames; frame++)
            yield return new WaitForEndOfFrame();

        float elapsed = 0f;
        while (!XRSettings.isDeviceActive && elapsed < deviceWaitSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        XROrigin origin = GetComponent<XROrigin>();
        Camera xrCamera = origin.Camera != null
            ? origin.Camera
            : GetComponentInChildren<Camera>(true);
        if (xrCamera == null)
        {
            Debug.LogError("XR 스케일 시험 눈높이를 맞출 카메라를 찾지 못했습니다.", this);
            yield break;
        }

        Vector3 currentEyePosition = xrCamera.transform.position;
        Vector3 targetEyePosition = new(
            currentEyePosition.x,
            floorWorldY + targetEyeHeight,
            currentEyePosition.z);
        origin.MoveCameraToWorldLocation(targetEyePosition);

        Debug.Log(
            $"XR 스케일 시험 눈높이 정렬 완료: 바닥 Y={floorWorldY:F3}m, "
            + $"눈높이={targetEyeHeight:F2}m, 카메라 Y={xrCamera.transform.position.y:F3}m",
            this);
    }
}
