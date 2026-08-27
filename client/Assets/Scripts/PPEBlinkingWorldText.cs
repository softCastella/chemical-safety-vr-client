using TMPro;
using UnityEngine;

/// <summary>
/// Applies an authored pulse to a world-space TMP label without changing its
/// authored text, position, scale, or color.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEBlinkingWorldText : MonoBehaviour
{
    static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Authored Label")]
    [SerializeField] TMP_Text targetText;

    [Header("Pulse")]
    [Tooltip("Optional marker pulse to share with this label so the text and its location marker stay in phase.")]
    [SerializeField] XRLocationMarkerPulse synchronizationSource;
    [SerializeField, Min(0f)] float cyclesPerSecond = 0.65f;
    [SerializeField, Range(0f, 1f)] float minimumOpacityMultiplier = 0.72f;
    [SerializeField, Range(0f, 1f)] float maximumOpacityMultiplier = 1f;

    Color authoredColor;
    Vector3 authoredScale;
    Renderer targetRenderer;
    MaterialPropertyBlock propertyBlock;
    bool hasAuthoredState;

    public TMP_Text TargetText
    {
        get => targetText;
        set => targetText = value;
    }

    void Awake() => CacheAuthoredState();

    void OnEnable()
    {
        CacheAuthoredState();
        ApplyPulse(1f);
    }

    void OnDisable()
    {
        if (!hasAuthoredState)
            return;

        if (Application.isPlaying)
            transform.localScale = authoredScale;

        ClearRendererPulse();
    }

    void Update()
    {
        if (targetText == null)
            return;

        double pulseTime = Application.isPlaying
            ? Time.time
#if UNITY_EDITOR
            : UnityEditor.EditorApplication.timeSinceStartup;
#else
            : Time.time;
#endif
        float opacity;
        if (synchronizationSource != null && Application.isPlaying)
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

    void CacheAuthoredState()
    {
        if (targetText == null)
            return;

        authoredColor = targetText.color;
        authoredScale = transform.localScale;
        targetRenderer = targetText.GetComponent<Renderer>();
        hasAuthoredState = true;
    }

    void ApplyPulse(float opacity)
    {
        if (!hasAuthoredState)
            return;

        if (targetRenderer == null)
            return;

        Color color = authoredColor;
        color.a = authoredColor.a * opacity;
        ApplyRendererColor(color);
    }

    void ApplyRendererColor(Color color)
    {
        propertyBlock ??= new MaterialPropertyBlock();

        Material[] materials = targetRenderer.sharedMaterials;
        for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
        {
            Material material = materials[materialIndex];
            if (material == null)
                continue;

            propertyBlock.Clear();
            targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);

            if (material.HasProperty(FaceColorId))
            {
                Color faceColor = Color.white;
                faceColor.a = color.a;
                propertyBlock.SetColor(FaceColorId, faceColor);
            }
            if (material.HasProperty(OutlineColorId))
            {
                Color outlineColor = Color.white;
                outlineColor.a = color.a;
                propertyBlock.SetColor(OutlineColorId, outlineColor);
            }
            if (material.HasProperty(BaseColorId))
                propertyBlock.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId))
                propertyBlock.SetColor(ColorId, color);

            targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
        }
    }

    void ClearRendererPulse()
    {
        if (targetRenderer == null)
            return;

        Material[] materials = targetRenderer.sharedMaterials;
        for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            targetRenderer.SetPropertyBlock(null, materialIndex);
    }
}
