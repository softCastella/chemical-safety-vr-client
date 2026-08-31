using UnityEngine;

/// <summary>
/// Inspector-authored tint and brightness for one glove display item or hand-model glove mesh.
/// Uses a MaterialPropertyBlock so shared materials, meshes, and Transforms stay unchanged.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("PPE/Glove Visual Appearance")]
public sealed class PPEGloveVisualAppearance : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Target Renderers")]
    [SerializeField]
    Renderer[] targetRenderers;

    [Header("Appearance")]
    [ColorUsage(true, true)]
    [SerializeField]
    Color color = Color.white;

    [SerializeField]
    [Min(0f)]
    float brightness = 1f;

    MaterialPropertyBlock propertyBlock;

    public Renderer[] TargetRenderers => targetRenderers;
    public Color Color => color;
    public float Brightness => brightness;

    void Reset()
    {
#if UNITY_EDITOR
        CaptureRendererAndMaterialDefaults();
#endif
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Glove Appearance")]
    public void Apply()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        Color adjustedColor = color * brightness;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                propertyBlock.Clear();
                targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);

                if (material.HasProperty(BaseColorId))
                    propertyBlock.SetColor(BaseColorId, adjustedColor);
                if (material.HasProperty(ColorId))
                    propertyBlock.SetColor(ColorId, adjustedColor);

                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Renderer[] configuredTargetRenderers,
        Color configuredColor,
        float configuredBrightness)
    {
        targetRenderers = configuredTargetRenderers;
        color = configuredColor;
        brightness = configuredBrightness;
        Apply();
    }

    public void CaptureRendererAndMaterialDefaults()
    {
        Renderer renderer = GetComponent<Renderer>();
        targetRenderers = renderer != null
            ? new[] { renderer }
            : System.Array.Empty<Renderer>();

        color = ReadMaterialColor(renderer, Color.white);
        brightness = 1f;
    }

    public static Color ReadMaterialColor(Renderer renderer, Color fallback)
    {
        if (renderer == null)
            return fallback;

        Material material = renderer.sharedMaterial;
        if (material == null)
            return fallback;

        if (material.HasProperty(BaseColorId))
            return material.GetColor(BaseColorId);
        if (material.HasProperty(ColorId))
            return material.GetColor(ColorId);
        return fallback;
    }
#endif
}
