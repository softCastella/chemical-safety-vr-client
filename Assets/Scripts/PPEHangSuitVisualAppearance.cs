using UnityEngine;

/// <summary>
/// Inspector-authored tint and brightness for one hung hazmat suit.
/// Uses a MaterialPropertyBlock so shared materials, meshes, and Transforms stay unchanged.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEHangSuitVisualAppearance : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int ContaminationStrengthId = Shader.PropertyToID("_ContaminationStrength");

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

    [Header("Contamination")]
    [SerializeField]
    bool applyContaminationStrength;

    [SerializeField]
    [Range(0f, 1f)]
    float contaminationStrength = 1f;

    MaterialPropertyBlock propertyBlock;

    public Renderer[] TargetRenderers => targetRenderers;
    public Color Color => color;
    public float Brightness => brightness;
    public bool ApplyContaminationStrength => applyContaminationStrength;
    public float ContaminationStrength => contaminationStrength;

    void Reset()
    {
        CaptureRendererAndMaterialDefaults();
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Hang Suit Appearance")]
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

                if (applyContaminationStrength && material.HasProperty(ContaminationStrengthId))
                    propertyBlock.SetFloat(ContaminationStrengthId, contaminationStrength);

                targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Renderer[] configuredTargetRenderers,
        Color configuredColor,
        float configuredBrightness,
        bool configuredApplyContaminationStrength,
        float configuredContaminationStrength)
    {
        targetRenderers = configuredTargetRenderers;
        color = configuredColor;
        brightness = configuredBrightness;
        applyContaminationStrength = configuredApplyContaminationStrength;
        contaminationStrength = configuredContaminationStrength;
        Apply();
    }

    public void CaptureRendererAndMaterialDefaults()
    {
        Renderer renderer = GetComponent<Renderer>();
        targetRenderers = renderer != null
            ? new[] { renderer }
            : GetComponentsInChildren<Renderer>(true);

        Material material = renderer != null ? renderer.sharedMaterial : null;
        if (material != null && material.HasProperty(BaseColorId))
            color = material.GetColor(BaseColorId);
        else if (material != null && material.HasProperty(ColorId))
            color = material.GetColor(ColorId);

        bool ripped = name.Contains("SuitHang_Ripped") ||
                      (material != null && material.name.Contains("ArmTear"));
        if (ripped)
            brightness = 0.7f;

        applyContaminationStrength = name.Contains("SuitHang_Contam");
        if (applyContaminationStrength)
            contaminationStrength = 1f;
    }
#endif
}
