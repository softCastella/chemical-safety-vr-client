using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

[DisallowMultipleComponent]
public sealed class XRNearFarReticleVisual : MonoBehaviour
{
    const float MinimumDirectionSqrMagnitude = 0.000001f;
    const float LineDistanceSafetyMultiplier = 4f;
    static bool s_FinaleRayVisualsVisible = true;

    [Header("References")]
    [SerializeField] NearFarInteractor nearFarInteractor;
    [SerializeField] CurveVisualController curveVisualController;
    [SerializeField] GameObject reticlePrefab;

    [Header("Ray Appearance")]
    [SerializeField] bool overrideRayAppearance = true;
    [SerializeField] Material rayMaterial;
    [SerializeField] Gradient rayGradient;
    [SerializeField, Min(0.1f)] float rayDistance = 40f;
    [SerializeField] bool extendRayToEmptyHit = true;

    [Header("Reticle Behavior")]
    [SerializeField] bool showOnEmptyGeometry = true;
    [SerializeField] bool showWhileSelecting;
    [SerializeField] bool scaleWithDistance = true;
    [SerializeField, Min(0.01f)] float minimumDistanceScale = 0.5f;
    [SerializeField, Min(0.01f)] float maximumDistanceScale = 4f;
    [SerializeField, Min(0f)] float surfaceOffset = 0.003f;

    GameObject reticleInstance;
    Vector3 authoredReticleScale;
    CurveVisualController appearanceTarget;
    LineRenderer authoredLineRenderer;
    Vector3[] linePositionBuffer;
    float authoredLineStartWidth;
    float authoredLineEndWidth;
    float authoredLineWidthMultiplier;
    LineRenderer finaleVisibilityLineRenderer;
    bool finaleVisibilityLineEnabled;
    bool hasFinaleVisibilityLineState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetFinaleRayVisibility()
    {
        s_FinaleRayVisualsVisible = true;
    }

    /// <summary>
    /// Hides only the authored ray visuals and reticle for the PPE mirror
    /// observation. Interactor and UI input remain active for the quiz.
    /// </summary>
    public static void SetFinaleRayVisualsVisible(bool visible)
    {
        s_FinaleRayVisualsVisible = visible;
    }

    void Awake()
    {
        ResolveActiveInteractor();
        CreateReticleInstance();
        ApplyRayAppearance();
    }

    void OnEnable()
    {
        Application.onBeforeRender += UpdateReticle;
    }

    void Start()
    {
        // Re-apply after every component in the imported interactor prefab has completed Awake.
        ResolveActiveInteractor();
        ApplyRayAppearance();
        UpdateReticle();
    }

    void LateUpdate()
    {
        if (nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
        {
            ResolveActiveInteractor();
            ApplyRayAppearance();
        }

        UpdateReticle();
    }

    void OnDisable()
    {
        Application.onBeforeRender -= UpdateReticle;
        SetReticleActive(false);
    }

    void OnDestroy()
    {
        Application.onBeforeRender -= UpdateReticle;
        if (reticleInstance != null)
            Destroy(reticleInstance);
    }

    void ResolveActiveInteractor()
    {
        if (nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
            nearFarInteractor = GetComponentInChildren<NearFarInteractor>();

        if (nearFarInteractor == null)
            return;

        if (curveVisualController == null || !curveVisualController.gameObject.activeInHierarchy)
            curveVisualController = nearFarInteractor.GetComponentInChildren<CurveVisualController>();
    }

    void CreateReticleInstance()
    {
        if (reticleInstance != null || reticlePrefab == null)
            return;

        reticleInstance = Instantiate(reticlePrefab);
        reticleInstance.name = $"{gameObject.name} Ray Reticle";
        authoredReticleScale = reticleInstance.transform.localScale;
        reticleInstance.SetActive(false);
    }

    void ApplyRayAppearance()
    {
        if (curveVisualController == null)
            return;

        curveVisualController.maxVisualCurveDistance = rayDistance;
        curveVisualController.extendLineToEmptyHit = extendRayToEmptyHit;

        if (nearFarInteractor != null
            && nearFarInteractor.farInteractionCaster is CurveInteractionCaster curveInteractionCaster)
        {
            curveInteractionCaster.castDistance = rayDistance;
        }

        if (appearanceTarget == curveVisualController)
            return;

        if (!overrideRayAppearance)
        {
            appearanceTarget = curveVisualController;
            return;
        }

        LineRenderer lineRenderer = curveVisualController.lineRenderer;
        if (lineRenderer != null)
        {
            CacheAuthoredLineState(lineRenderer);
            if (rayMaterial != null)
                lineRenderer.sharedMaterial = rayMaterial;
            if (rayGradient != null)
                lineRenderer.colorGradient = rayGradient;
        }

        if (rayGradient != null)
        {
            ApplyGradient(curveVisualController.noValidHitProperties);
            ApplyGradient(curveVisualController.uiHitProperties);
            ApplyGradient(curveVisualController.uiPressHitProperties);
            ApplyGradient(curveVisualController.selectHitProperties);
            ApplyGradient(curveVisualController.hoverHitProperties);
        }

        appearanceTarget = curveVisualController;
    }

    void ApplyGradient(LineProperties properties)
    {
        if (properties == null)
            return;

        properties.adjustGradient = true;
        properties.gradient = rayGradient;
    }

    [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderLineVisual + 1)]
    void UpdateReticle()
    {
        if (!s_FinaleRayVisualsVisible)
        {
            SetFinaleRayVisualVisible(false);
            SetReticleActive(false);
            return;
        }

        SetFinaleRayVisualVisible(true);
        SanitizeCurveLineRenderer();

        if (reticleInstance == null)
            CreateReticleInstance();

        if (reticleInstance == null || nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
        {
            SetReticleActive(false);
            return;
        }

        var curveProvider = (ICurveInteractionDataProvider)nearFarInteractor;
        if (!curveProvider.isActive || (!showWhileSelecting && nearFarInteractor.hasSelection))
        {
            SetReticleActive(false);
            return;
        }

        EndPointType endPointType = nearFarInteractor.TryGetCurveEndPoint(
            out Vector3 endPoint, snapToSelectedAttachIfAvailable: true, snapToSnapVolumeIfAvailable: true);
        if (endPointType == EndPointType.None
            || (endPointType == EndPointType.EmptyCastHit && !showOnEmptyGeometry)
            || !IsFinite(endPoint))
        {
            SetReticleActive(false);
            return;
        }

        Vector3 origin = curveProvider.curveOrigin != null
            ? curveProvider.curveOrigin.position
            : nearFarInteractor.transform.position;
        if (!IsFinite(origin))
        {
            SetReticleActive(false);
            return;
        }

        if (!TryNormalize(origin - endPoint, out Vector3 towardOrigin)
            && !TryNormalize(-nearFarInteractor.transform.forward, out towardOrigin))
        {
            SetReticleActive(false);
            return;
        }

        EndPointType normalType = nearFarInteractor.TryGetCurveEndNormal(
            out Vector3 endNormal, snapToSelectedAttachIfAvailable: true);
        if (normalType == EndPointType.None || !TryNormalize(endNormal, out endNormal))
            endNormal = towardOrigin;

        float normalAlignment = Vector3.Dot(endNormal, towardOrigin);
        if (!IsFinite(normalAlignment))
        {
            SetReticleActive(false);
            return;
        }

        if (normalAlignment < 0f)
            endNormal = -endNormal;

        if (!TryNormalize(Vector3.ProjectOnPlane(Vector3.up, endNormal), out Vector3 reticleUp)
            && !TryNormalize(Vector3.ProjectOnPlane(nearFarInteractor.transform.up, endNormal), out reticleUp))
        {
            Vector3 fallbackUp = Mathf.Abs(endNormal.y) < 0.999f
                ? Vector3.up
                : Vector3.forward;
            if (!TryNormalize(Vector3.ProjectOnPlane(fallbackUp, endNormal), out reticleUp))
            {
                SetReticleActive(false);
                return;
            }
        }

        if (!IsFinite(surfaceOffset))
        {
            SetReticleActive(false);
            return;
        }

        Vector3 reticlePosition = endPoint + endNormal * surfaceOffset;
        Quaternion reticleRotation = Quaternion.LookRotation(endNormal, reticleUp);
        if (!IsFinite(reticlePosition) || !IsFinite(reticleRotation) || !IsFinite(authoredReticleScale))
        {
            SetReticleActive(false);
            return;
        }

        float distanceScale = 1f;
        if (scaleWithDistance)
        {
            float distance = Vector3.Distance(origin, endPoint);
            float min = Mathf.Min(minimumDistanceScale, maximumDistanceScale);
            float max = Mathf.Max(minimumDistanceScale, maximumDistanceScale);
            if (!IsFinite(distance) || !IsFinite(min) || !IsFinite(max) || min <= 0f || max <= 0f)
            {
                SetReticleActive(false);
                return;
            }

            distanceScale = Mathf.Clamp(distance, min, max);
        }

        Vector3 reticleScale = authoredReticleScale * distanceScale;
        if (!IsFinite(reticleScale))
        {
            SetReticleActive(false);
            return;
        }

        Transform reticleTransform = reticleInstance.transform;
        reticleTransform.SetPositionAndRotation(reticlePosition, reticleRotation);
        reticleTransform.localScale = reticleScale;
        SetReticleActive(true);
    }

    void SetFinaleRayVisualVisible(bool visible)
    {
        if (curveVisualController == null)
            return;

        LineRenderer lineRenderer = curveVisualController.lineRenderer;
        if (!visible)
        {
            if (lineRenderer != null &&
                (!hasFinaleVisibilityLineState || finaleVisibilityLineRenderer != lineRenderer))
            {
                finaleVisibilityLineRenderer = lineRenderer;
                finaleVisibilityLineEnabled = lineRenderer.enabled;
                hasFinaleVisibilityLineState = true;
            }

            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        if (!hasFinaleVisibilityLineState)
            return;

        if (finaleVisibilityLineRenderer != null)
            finaleVisibilityLineRenderer.enabled = finaleVisibilityLineEnabled;

        finaleVisibilityLineRenderer = null;
        hasFinaleVisibilityLineState = false;
    }

    void CacheAuthoredLineState(LineRenderer lineRenderer)
    {
        if (lineRenderer == null || authoredLineRenderer == lineRenderer)
            return;

        authoredLineRenderer = lineRenderer;
        authoredLineStartWidth = lineRenderer.startWidth;
        authoredLineEndWidth = lineRenderer.endWidth;
        authoredLineWidthMultiplier = lineRenderer.widthMultiplier;
    }

    void SanitizeCurveLineRenderer()
    {
        if (curveVisualController == null)
            return;

        LineRenderer lineRenderer = curveVisualController.lineRenderer;
        if (lineRenderer == null || !lineRenderer.enabled)
            return;

        CacheAuthoredLineState(lineRenderer);

        bool invalidWidth = !IsFinite(lineRenderer.startWidth)
            || !IsFinite(lineRenderer.endWidth)
            || !IsFinite(lineRenderer.widthMultiplier);

        int positionCount = lineRenderer.positionCount;
        bool invalidPositions = positionCount < 0;
        Vector3 safePoint = Vector3.zero;

        if (positionCount > 0)
        {
            if (linePositionBuffer == null || linePositionBuffer.Length != positionCount)
                linePositionBuffer = new Vector3[positionCount];

            lineRenderer.GetPositions(linePositionBuffer);

            Vector3 worldOrigin = nearFarInteractor != null
                ? nearFarInteractor.transform.position
                : transform.position;
            bool validOrigin = IsFinite(worldOrigin);
            bool validRayDistance = IsFinite(rayDistance) && rayDistance > 0f;
            float maximumDistance = validRayDistance
                ? rayDistance * LineDistanceSafetyMultiplier
                : 0f;
            float maximumSqrDistance = maximumDistance * maximumDistance;
            if (!IsFinite(maximumSqrDistance))
                validRayDistance = false;

            safePoint = lineRenderer.useWorldSpace && validOrigin
                ? worldOrigin
                : Vector3.zero;

            for (int index = 0; index < positionCount; index++)
            {
                Vector3 point = linePositionBuffer[index];
                if (!IsFinite(point) || !validOrigin || !validRayDistance)
                {
                    invalidPositions = true;
                    break;
                }

                Vector3 worldPoint = lineRenderer.useWorldSpace
                    ? point
                    : lineRenderer.transform.TransformPoint(point);
                if (!IsWorldLinePointUsable(worldPoint, worldOrigin, maximumSqrDistance))
                {
                    invalidPositions = true;
                    break;
                }
            }
        }

        if (invalidPositions)
        {
            for (int index = 0; index < positionCount; index++)
                linePositionBuffer[index] = safePoint;
            lineRenderer.SetPositions(linePositionBuffer);
        }

        if (invalidWidth)
        {
            lineRenderer.startWidth = authoredLineStartWidth;
            lineRenderer.endWidth = authoredLineEndWidth;
            lineRenderer.widthMultiplier = authoredLineWidthMultiplier;
        }
    }

    static bool IsWorldLinePointUsable(
        Vector3 worldPoint,
        Vector3 worldOrigin,
        float maximumSqrDistance)
    {
        if (!IsFinite(worldPoint)
            || !IsFinite(worldOrigin)
            || !IsFinite(maximumSqrDistance)
            || maximumSqrDistance < 0f)
        {
            return false;
        }

        float sqrDistance = (worldPoint - worldOrigin).sqrMagnitude;
        return IsFinite(sqrDistance) && sqrDistance <= maximumSqrDistance;
    }

    static bool TryNormalize(Vector3 value, out Vector3 normalized)
    {
        normalized = default;
        if (!IsFinite(value))
            return false;

        float sqrMagnitude = value.sqrMagnitude;
        if (!IsFinite(sqrMagnitude) || sqrMagnitude < MinimumDirectionSqrMagnitude)
            return false;

        normalized = value / Mathf.Sqrt(sqrMagnitude);
        return IsFinite(normalized);
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }

    void SetReticleActive(bool active)
    {
        if (reticleInstance != null && reticleInstance.activeSelf != active)
            reticleInstance.SetActive(active);
    }
}
