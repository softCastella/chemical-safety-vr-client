using UnityEngine;

/// <summary>
/// Pulses an authored world-space sprite and optionally shares the pulse clock
/// with its location marker.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEBlinkingWorldSprite : MonoBehaviour
{
    static readonly int PulseOpacityId = Shader.PropertyToID("_PulseOpacity");

    [Header("Authored Image")]
    [SerializeField] SpriteRenderer targetRenderer;

    [Header("Pulse")]
    [SerializeField] XRLocationMarkerPulse synchronizationSource;
    [SerializeField, Min(0f)] float cyclesPerSecond = 0.65f;
    [SerializeField, Range(0f, 1f)] float minimumOpacityMultiplier = 0.72f;
    [SerializeField, Range(0f, 1f)] float maximumOpacityMultiplier = 1f;

    MaterialPropertyBlock propertyBlock;

    void Awake() => CacheAuthoredState();

    void OnEnable()
    {
        CacheAuthoredState();
        ApplyPulse(1f);
    }

    void OnDisable()
    {
        if (targetRenderer != null)
            targetRenderer.SetPropertyBlock(null);

    }

    void Update()
    {
        if (targetRenderer == null)
            return;

        double pulseTime = GetPulseTime();
        float opacity;
        if (synchronizationSource != null)
        {
            synchronizationSource.SamplePulse(pulseTime, out _, out opacity);
        }
        else
        {
            float phase = (Mathf.Sin((float)(pulseTime * cyclesPerSecond * Mathf.PI * 2f)) + 1f) * 0.5f;
            opacity = Mathf.Lerp(minimumOpacityMultiplier, maximumOpacityMultiplier, phase);
        }

        ApplyPulse(opacity);
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

    void CacheAuthoredState()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

    }

    void ApplyPulse(float opacity)
    {
        if (targetRenderer == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(PulseOpacityId, opacity);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
