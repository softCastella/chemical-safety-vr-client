using UnityEngine;

/// <summary>
/// Inspector-authored color and brightness for a floor renderer.
/// Uses a per-renderer property block so the shared floor material is not
/// modified and authored scene values remain the source of truth.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloorAppearanceController : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [ColorUsage(false, true)]
    [SerializeField] Color floorColor = Color.white;
    [Min(0f)]
    [SerializeField] float brightness = 1f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    MaterialPropertyBlock propertyBlock;

    void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            Apply();
    }

    public void Apply()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(propertyBlock);

        Color adjustedColor = floorColor * brightness;
        Material material = targetRenderer.sharedMaterial;
        if (material != null && material.HasProperty(BaseColorId))
            propertyBlock.SetColor(BaseColorId, adjustedColor);
        else if (material != null && material.HasProperty(ColorId))
            propertyBlock.SetColor(ColorId, adjustedColor);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
