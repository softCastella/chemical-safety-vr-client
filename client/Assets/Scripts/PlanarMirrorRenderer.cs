using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class PlanarMirrorRenderer : MonoBehaviour
{
    [SerializeField] Camera reflectionCamera;
    [SerializeField] RenderTexture targetTexture;
    [SerializeField] LayerMask reflectedLayers = -1;
    [SerializeField, Min(0.001f)] float surfaceClipOffset = 0.04f;
    [SerializeField, Min(0.5f)] float reflectedDepth = 12f;
    [SerializeField] Color clearColor = new(0.58f, 0.68f, 0.72f, 1f);
    [SerializeField] bool renderInGameView = true;
    [SerializeField] bool renderInSceneView = true;

    [Header("Performance")]
    [Tooltip("Play Mode에서 반사를 N프레임마다 한 번만 갱신한다. 1이면 매 프레임 갱신(기존 동작).")]
    [SerializeField, Min(1)] int renderFrameInterval = 1;
    [Tooltip("카메라가 거울에서 이 거리(미터)보다 멀면 반사 갱신을 건너뛴다. 0이면 거리 제한 없음(기존 동작).")]
    [SerializeField, Min(0f)] float maxSourceDistance = 0f;
    [Tooltip("Play Mode 중에는 Scene 뷰 카메라를 위한 반사 렌더링을 건너뛴다.")]
    [SerializeField] bool skipSceneViewWhilePlaying = false;

    Renderer mirrorRenderer;
    static bool isRenderingReflection;

    public Camera ReflectionCamera => reflectionCamera;
    public RenderTexture TargetTexture => targetTexture;
    public LayerMask ReflectedLayers => reflectedLayers;

    void OnEnable()
    {
        CacheReferences();
        ApplyStaticCameraSettings();
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnValidate()
    {
        CacheReferences();
        ApplyStaticCameraSettings();
    }

    void CacheReferences()
    {
        if (mirrorRenderer == null)
            mirrorRenderer = GetComponent<Renderer>();

        if (reflectionCamera == null && transform.parent != null)
            reflectionCamera = transform.parent.GetComponentInChildren<Camera>(true);
    }

    void ApplyStaticCameraSettings()
    {
        if (reflectionCamera == null)
            return;

        reflectionCamera.enabled = false;
        reflectionCamera.targetTexture = targetTexture;
        reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        reflectionCamera.backgroundColor = clearColor;
        reflectionCamera.useOcclusionCulling = false;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera sourceCamera)
    {
        if (!ShouldRenderFor(sourceCamera))
            return;

        RenderForCamera(sourceCamera, context);
    }

    public bool RenderForCamera(Camera sourceCamera)
    {
        if (!ShouldRenderFor(sourceCamera))
            return false;

        ReflectionCameraState previousState = new(reflectionCamera);
        isRenderingReflection = true;
        try
        {
            if (!ConfigureReflectionCamera(sourceCamera))
                return false;

            reflectionCamera.Render();
            return true;
        }
        finally
        {
            previousState.Restore(reflectionCamera);
            isRenderingReflection = false;
        }
    }

    bool RenderForCamera(Camera sourceCamera, ScriptableRenderContext context)
    {
        ReflectionCameraState previousState = new(reflectionCamera);
        isRenderingReflection = true;
        try
        {
            if (!ConfigureReflectionCamera(sourceCamera))
                return false;

#pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
#pragma warning restore CS0618
            return true;
        }
        finally
        {
            previousState.Restore(reflectionCamera);
            isRenderingReflection = false;
        }
    }

    bool ShouldRenderFor(Camera sourceCamera)
    {
        if (!isActiveAndEnabled || isRenderingReflection || sourceCamera == null ||
            reflectionCamera == null || targetTexture == null || sourceCamera == reflectionCamera ||
            sourceCamera.targetTexture == targetTexture)
        {
            return false;
        }

        if (sourceCamera.cameraType == CameraType.SceneView &&
            (!renderInSceneView || (skipSceneViewWhilePlaying && Application.isPlaying)))
        {
            return false;
        }

        if (sourceCamera.cameraType == CameraType.Game && !renderInGameView)
            return false;

        if (sourceCamera.cameraType == CameraType.Game && Application.isPlaying &&
            renderFrameInterval > 1 && Time.frameCount % renderFrameInterval != 0)
        {
            return false;
        }

        if (sourceCamera.cameraType == CameraType.Preview ||
            sourceCamera.cameraType == CameraType.Reflection)
        {
            return false;
        }

        CacheReferences();
        if (mirrorRenderer == null || !mirrorRenderer.enabled)
            return false;

        int mirrorLayerMask = 1 << gameObject.layer;
        if ((sourceCamera.cullingMask & mirrorLayerMask) == 0)
            return false;

        if (maxSourceDistance > 0f)
        {
            Vector3 closestPoint = mirrorRenderer.bounds.ClosestPoint(sourceCamera.transform.position);
            float sqrDistance = (closestPoint - sourceCamera.transform.position).sqrMagnitude;
            if (sqrDistance > maxSourceDistance * maxSourceDistance)
                return false;
        }

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(sourceCamera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, mirrorRenderer.bounds);
    }

    bool ConfigureReflectionCamera(Camera sourceCamera)
    {
        Vector3 planePoint = transform.position;
        Vector3 planeNormal = transform.forward.normalized;
        float signedDistance = Vector3.Dot(planeNormal, sourceCamera.transform.position - planePoint);
        float mirrorDistance = Mathf.Abs(signedDistance);
        if (mirrorDistance < surfaceClipOffset + 0.01f)
            return false;

        reflectionCamera.enabled = false;
        reflectionCamera.targetTexture = targetTexture;
        reflectionCamera.rect = new Rect(0f, 0f, 1f, 1f);
        reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
        reflectionCamera.backgroundColor = clearColor;
        reflectionCamera.useOcclusionCulling = false;
        // reflectedLayers is authoritative so mirror-only equipment can stay hidden from the HMD camera.
        reflectionCamera.cullingMask = reflectedLayers.value & ~(1 << gameObject.layer);

        Vector3 reflectedPosition = sourceCamera.transform.position - 2f * signedDistance * planeNormal;
        Vector3 forward = planeNormal * Mathf.Sign(signedDistance);
        Vector3 up = Vector3.ProjectOnPlane(transform.up, forward).normalized;
        if (up.sqrMagnitude < 0.5f)
            up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;

        reflectionCamera.transform.SetPositionAndRotation(
            reflectedPosition,
            Quaternion.LookRotation(forward, up));
        reflectionCamera.ResetWorldToCameraMatrix();

        float nearPlane = Mathf.Max(0.03f, mirrorDistance - surfaceClipOffset);
        float farPlane = Mathf.Max(nearPlane + 0.5f, mirrorDistance + reflectedDepth);
        if (!TryBuildMirrorProjection(reflectionCamera, nearPlane, farPlane, out Matrix4x4 projection))
            return false;

        reflectionCamera.nearClipPlane = nearPlane;
        reflectionCamera.farClipPlane = farPlane;
        reflectionCamera.projectionMatrix = projection;
        return true;
    }

    readonly struct ReflectionCameraState
    {
        readonly Vector3 position;
        readonly Quaternion rotation;
        readonly bool enabled;
        readonly RenderTexture targetTexture;
        readonly Rect rect;
        readonly CameraClearFlags clearFlags;
        readonly Color backgroundColor;
        readonly bool useOcclusionCulling;
        readonly int cullingMask;
        readonly float nearClipPlane;
        readonly float farClipPlane;
        readonly Matrix4x4 projectionMatrix;

        public ReflectionCameraState(Camera camera)
        {
            position = camera.transform.position;
            rotation = camera.transform.rotation;
            enabled = camera.enabled;
            targetTexture = camera.targetTexture;
            rect = camera.rect;
            clearFlags = camera.clearFlags;
            backgroundColor = camera.backgroundColor;
            useOcclusionCulling = camera.useOcclusionCulling;
            cullingMask = camera.cullingMask;
            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
            projectionMatrix = camera.projectionMatrix;
        }

        public void Restore(Camera camera)
        {
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.ResetWorldToCameraMatrix();
            camera.enabled = enabled;
            camera.targetTexture = targetTexture;
            camera.rect = rect;
            camera.clearFlags = clearFlags;
            camera.backgroundColor = backgroundColor;
            camera.useOcclusionCulling = useOcclusionCulling;
            camera.cullingMask = cullingMask;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            camera.projectionMatrix = projectionMatrix;
        }
    }

    bool TryBuildMirrorProjection(Camera cameraToUse, float nearPlane, float farPlane, out Matrix4x4 projection)
    {
        Vector3[] localCorners =
        {
            new(-0.5f, -0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(-0.5f, 0.5f, 0f),
            new(0.5f, 0.5f, 0f)
        };

        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        float top = float.NegativeInfinity;
        Matrix4x4 worldToCamera = cameraToUse.worldToCameraMatrix;

        foreach (Vector3 localCorner in localCorners)
        {
            Vector3 cameraCorner = worldToCamera.MultiplyPoint(transform.TransformPoint(localCorner));
            float depth = -cameraCorner.z;
            if (depth <= 0.001f)
            {
                projection = default;
                return false;
            }

            float projectedX = cameraCorner.x * nearPlane / depth;
            float projectedY = cameraCorner.y * nearPlane / depth;
            left = Mathf.Min(left, projectedX);
            right = Mathf.Max(right, projectedX);
            bottom = Mathf.Min(bottom, projectedY);
            top = Mathf.Max(top, projectedY);
        }

        if (right - left < 0.0001f || top - bottom < 0.0001f)
        {
            projection = default;
            return false;
        }

        projection = Matrix4x4.Frustum(left, right, bottom, top, nearPlane, farPlane);
        return true;
    }
}
