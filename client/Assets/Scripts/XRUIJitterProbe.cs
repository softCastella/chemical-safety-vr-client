using System;
using UnityEngine;

/// <summary>
/// Runtime-only diagnostic for a selected world-space UI root.
/// It records whether the UI Transform changes during Update, LateUpdate, or before rendering
/// while the HMD camera moves. It does not alter the target UI pose or presentation.
/// </summary>
[DisallowMultipleComponent]
public sealed class XRUIJitterProbe : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField, Min(1f)] float captureDurationSeconds = 6f;
    [SerializeField] bool captureOnEnable;

    bool isCapturing;
    float captureEndTime;
    int frameCount;
    float totalFrameMilliseconds;
    float peakFrameMilliseconds;
    float peakCameraDegreesPerSecond;
    float peakUpdateToLateMillimeters;
    float peakLateToBeforeRenderMillimeters;
    float peakUpdateToLateDegrees;
    float peakLateToBeforeRenderDegrees;
    Pose updatePose;
    Pose latePose;
    Quaternion lastCameraRotation;
    bool hasCameraRotation;

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
        if (captureOnEnable && Application.isPlaying)
            BeginCapture();
    }

    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    void Update()
    {
        if (!isCapturing)
            return;

        updatePose = new Pose(transform.position, transform.rotation);
        SampleCameraAndFrameTime();
    }

    void LateUpdate()
    {
        if (!isCapturing)
            return;

        latePose = new Pose(transform.position, transform.rotation);
        TrackPhaseDelta(updatePose, latePose, ref peakUpdateToLateMillimeters, ref peakUpdateToLateDegrees);
    }

    void OnBeforeRender()
    {
        if (!isCapturing)
            return;

        var beforeRenderPose = new Pose(transform.position, transform.rotation);
        TrackPhaseDelta(latePose, beforeRenderPose, ref peakLateToBeforeRenderMillimeters, ref peakLateToBeforeRenderDegrees);

        if (Time.unscaledTime >= captureEndTime)
            EndCapture();
    }

    [ContextMenu("Begin Jitter Capture")]
    public void BeginCapture()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[XR UI Jitter] Enter Play Mode before starting a capture.", this);
            return;
        }

        targetCamera = targetCamera != null ? targetCamera : Camera.main;
        if (targetCamera == null)
        {
            Debug.LogError("[XR UI Jitter] No target camera is assigned or tagged MainCamera.", this);
            return;
        }

        isCapturing = true;
        captureEndTime = Time.unscaledTime + captureDurationSeconds;
        frameCount = 0;
        totalFrameMilliseconds = 0f;
        peakFrameMilliseconds = 0f;
        peakCameraDegreesPerSecond = 0f;
        peakUpdateToLateMillimeters = 0f;
        peakLateToBeforeRenderMillimeters = 0f;
        peakUpdateToLateDegrees = 0f;
        peakLateToBeforeRenderDegrees = 0f;
        hasCameraRotation = false;

        Debug.Log($"[XR UI Jitter] Capture started for '{name}' ({captureDurationSeconds:0.0}s). Rotate the HMD left and right normally.", this);
    }

    [ContextMenu("End Jitter Capture")]
    public void EndCapture()
    {
        if (!isCapturing)
            return;

        isCapturing = false;
        var averageFrameMilliseconds = frameCount > 0 ? totalFrameMilliseconds / frameCount : 0f;
        Debug.Log(
            $"[XR UI Jitter] Capture complete | target='{name}' | frames={frameCount} | " +
            $"frame ms avg/peak={averageFrameMilliseconds:0.00}/{peakFrameMilliseconds:0.00} | " +
            $"camera peak deg/s={peakCameraDegreesPerSecond:0.0} | " +
            $"UI Update→Late mm/deg={peakUpdateToLateMillimeters:0.000}/{peakUpdateToLateDegrees:0.000} | " +
            $"UI Late→BeforeRender mm/deg={peakLateToBeforeRenderMillimeters:0.000}/{peakLateToBeforeRenderDegrees:0.000}",
            this);
    }

    void SampleCameraAndFrameTime()
    {
        var frameMilliseconds = Time.unscaledDeltaTime * 1000f;
        frameCount++;
        totalFrameMilliseconds += frameMilliseconds;
        peakFrameMilliseconds = Mathf.Max(peakFrameMilliseconds, frameMilliseconds);

        var currentCameraRotation = targetCamera.transform.rotation;
        if (hasCameraRotation && Time.unscaledDeltaTime > Mathf.Epsilon)
        {
            var degreesPerSecond = Quaternion.Angle(lastCameraRotation, currentCameraRotation) / Time.unscaledDeltaTime;
            peakCameraDegreesPerSecond = Mathf.Max(peakCameraDegreesPerSecond, degreesPerSecond);
        }

        lastCameraRotation = currentCameraRotation;
        hasCameraRotation = true;
    }

    static void TrackPhaseDelta(Pose from, Pose to, ref float peakMillimeters, ref float peakDegrees)
    {
        peakMillimeters = Mathf.Max(peakMillimeters, Vector3.Distance(from.position, to.position) * 1000f);
        peakDegrees = Mathf.Max(peakDegrees, Quaternion.Angle(from.rotation, to.rotation));
    }
}
