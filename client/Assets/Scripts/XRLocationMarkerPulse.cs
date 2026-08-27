using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class XRLocationMarkerPulse : MonoBehaviour
{
    [Header("Pulse Target")]
    [SerializeField] Renderer targetRenderer;

    [Header("Pulse Motion")]
    [SerializeField, Min(0f)] float cyclesPerSecond = 0.65f;
    [SerializeField, Range(0.1f, 2f)] float minimumScaleMultiplier = 0.88f;
    [SerializeField, Range(0.1f, 3f)] float maximumScaleMultiplier = 1.18f;
    [SerializeField, Range(0f, 1f)] float minimumOpacityMultiplier = 0.72f;
    [SerializeField, Range(0f, 1f)] float maximumOpacityMultiplier = 1f;

    static readonly int PulseOpacityId = Shader.PropertyToID("_PulseOpacity");
    static readonly int PulseScaleId = Shader.PropertyToID("_PulseScale");

    MaterialPropertyBlock propertyBlock;

    void Awake() => CacheRenderer();

    void OnEnable() => CacheRenderer();

    void OnDisable()
    {
        SetPulse(1f, 1f);
    }

    void Update()
    {
        if (targetRenderer == null)
            return;

        SamplePulse(GetPulseTime(), out var scale, out var opacity);
        SetPulse(scale, opacity);
    }

    static double GetPulseTime()
    {
        if (Application.isPlaying)
            return Time.time;

#if UNITY_EDITOR
        return UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.time;
#endif
    }

    public void SamplePulse(double pulseTime, out float scale, out float opacity)
    {
        var phase = (Mathf.Sin((float)(pulseTime * cyclesPerSecond * Mathf.PI * 2f)) + 1f) * 0.5f;
        scale = Mathf.Lerp(minimumScaleMultiplier, maximumScaleMultiplier, phase);
        opacity = Mathf.Lerp(minimumOpacityMultiplier, maximumOpacityMultiplier, phase);
    }

    public void CacheRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        SetPulse(1f, 1f);
    }

    void SetPulse(float scale, float opacity)
    {
        if (targetRenderer == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(PulseScaleId, scale);
        propertyBlock.SetFloat(PulseOpacityId, opacity);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
