using UnityEngine;

/// <summary>
/// Keeps a world-space Canvas RectTransform at its scene-authored pose.
/// Play Mode restores the serialized snapshot so startup paths cannot move it.
/// Edit Mode pose is captured into the snapshot on scene save / via context menu.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(1000)]
public sealed class AuthoredWorldCanvasPose : MonoBehaviour
{
    [SerializeField] private Vector3 authoredAnchoredPosition3D;
    [SerializeField] private Vector3 authoredLocalEulerAngles;
    [SerializeField] private Vector3 authoredLocalScale = Vector3.one;
    [SerializeField] private Vector2 authoredSizeDelta;
    [SerializeField] private bool restoreOnPlay = true;
    [SerializeField] private bool hasAuthoredPose;

    private RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        if (restoreOnPlay && Application.isPlaying)
            ApplyAuthoredPose();
    }

    private void Start()
    {
        // Second pass after other Start placement scripts.
        if (restoreOnPlay && Application.isPlaying)
            ApplyAuthoredPose();
    }

    [ContextMenu("Capture Authored Pose From Current")]
    public void CaptureFromCurrent()
    {
        RectTransform rect = RectTransform;
        authoredAnchoredPosition3D = ResolveAuthoredPosition(rect);
        authoredLocalEulerAngles = rect.localEulerAngles;
        authoredLocalScale = rect.localScale;
        authoredSizeDelta = rect.sizeDelta;
        hasAuthoredPose = true;
    }

    [ContextMenu("Apply Authored Pose Now")]
    public void ApplyAuthoredPose()
    {
        if (!hasAuthoredPose)
            CaptureFromCurrent();

        RectTransform rect = RectTransform;
        rect.localRotation = Quaternion.Euler(authoredLocalEulerAngles);
        rect.localScale = authoredLocalScale;
        rect.sizeDelta = authoredSizeDelta;
        // Keep local and anchored XYZ in sync for root world canvases.
        rect.anchoredPosition3D = authoredAnchoredPosition3D;
        rect.localPosition = authoredAnchoredPosition3D;
    }

    public static Vector3 ResolveAuthoredPosition(RectTransform rect)
    {
        // Root world canvases often serialize XY in anchoredPosition and Z in localPosition.
        Vector2 anchored = rect.anchoredPosition;
        return new Vector3(anchored.x, anchored.y, rect.localPosition.z);
    }
}
