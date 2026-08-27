using UnityEngine;

[DisallowMultipleComponent]
public sealed class PPEBareHandAppearance : MonoBehaviour
{
    [Header("Scene-Authored Hand Renderers")]
    [SerializeField] Renderer leftHandRenderer;
    [SerializeField] Renderer rightHandRenderer;

    [Header("Unlit Hand Colors")]
    [SerializeField] Color leftHandColor = new(0.9453f, 0.8434f, 0.7736f, 1f);
    [SerializeField] Color rightHandColor = new(0.9445f, 0.8425f, 0.7741f, 1f);

    [Header("Finger Definition")]
    [SerializeField, Range(0f, 0.8f)] float formShading = 0.32f;
    [SerializeField, Range(0f, 0.8f)] float edgeDarkening = 0.38f;
    [SerializeField, Range(0.5f, 8f)] float edgePower = 3f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int FormShadingId = Shader.PropertyToID("_FormShading");
    static readonly int EdgeDarkeningId = Shader.PropertyToID("_EdgeDarkening");
    static readonly int EdgePowerId = Shader.PropertyToID("_EdgePower");

    public Renderer LeftHandRenderer => leftHandRenderer;
    public Renderer RightHandRenderer => rightHandRenderer;
    public Color LeftHandColor => leftHandColor;
    public Color RightHandColor => rightHandColor;
    public float FormShading => formShading;
    public float EdgeDarkening => edgeDarkening;
    public float EdgePower => edgePower;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    [ContextMenu("Apply Hand Colors")]
    public void Apply()
    {
        ApplyAppearance(leftHandRenderer, leftHandColor);
        ApplyAppearance(rightHandRenderer, rightHandColor);
    }

    void ApplyAppearance(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        MaterialPropertyBlock properties = new();
        renderer.GetPropertyBlock(properties);
        properties.SetColor(BaseColorId, color);
        properties.SetColor(ColorId, color);
        properties.SetFloat(FormShadingId, formShading);
        properties.SetFloat(EdgeDarkeningId, edgeDarkening);
        properties.SetFloat(EdgePowerId, edgePower);
        renderer.SetPropertyBlock(properties);
    }
}
