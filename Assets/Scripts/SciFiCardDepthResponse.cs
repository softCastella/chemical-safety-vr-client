using UnityEngine;

[DisallowMultipleComponent]
public sealed class SciFiCardDepthResponse : MonoBehaviour
{
    [Header("Content Layers")]
    public Transform backgroundAnchor;
    public Transform contentAnchor;
    public Transform foregroundAnchor;

    [Header("Head Parallax")]
    public bool enableParallax = true;
    [Min(0f)] public float parallaxStrength = 0.018f;
    [Min(0f)] public float maxOffset = 0.012f;
    [Min(0.01f)] public float smoothing = 12f;
    public Camera targetCamera;

    Vector3 backgroundBase;
    Vector3 contentBase;
    Vector3 foregroundBase;

    void Awake() => CacheBasePositions();
    void OnEnable() => CacheBasePositions();

    public void CacheBasePositions()
    {
        if (backgroundAnchor != null) backgroundBase = backgroundAnchor.localPosition;
        if (contentAnchor != null) contentBase = contentAnchor.localPosition;
        if (foregroundAnchor != null) foregroundBase = foregroundAnchor.localPosition;
    }

    void LateUpdate()
    {
        if (!enableParallax)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        var localViewer = transform.InverseTransformPoint(targetCamera.transform.position);
        var distance = Mathf.Max(0.15f, Mathf.Abs(localViewer.z));
        var offset = Vector2.ClampMagnitude(new Vector2(localViewer.x, localViewer.y) / distance * parallaxStrength, maxOffset);
        var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        Move(backgroundAnchor, backgroundBase, -offset * 0.35f, blend);
        Move(contentAnchor, contentBase, offset * 0.35f, blend);
        Move(foregroundAnchor, foregroundBase, offset, blend);
    }

    static void Move(Transform target, Vector3 basePosition, Vector2 offset, float blend)
    {
        if (target == null)
            return;
        var desired = basePosition + new Vector3(offset.x, offset.y, 0f);
        target.localPosition = Vector3.Lerp(target.localPosition, desired, blend);
    }
}
