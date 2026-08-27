using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HangarColorVariantController : MonoBehaviour
{
    public enum ColorVariant
    {
        Original,
        White,
        Blue,
        Custom,
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColorId = Shader.PropertyToID("_Color");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

    [SerializeField]
    [Tooltip("Select the color version shown on this Hangar instance.")]
    ColorVariant variant = ColorVariant.Original;

    [SerializeField]
    [ColorUsage(false, false)]
    [Tooltip("Color used by the White preset.")]
    Color whiteColor = new(0.94f, 0.96f, 1f, 1f);

    [SerializeField]
    [ColorUsage(false, false)]
    [Tooltip("Color used by the Blue preset.")]
    Color blueColor = new(0.16f, 0.42f, 0.95f, 1f);

    [SerializeField]
    [ColorUsage(false, false)]
    [Tooltip("Color used by the Custom preset.")]
    Color customColor = Color.white;

    [SerializeField]
    [Tooltip(
        "Keep the source albedo texture and multiply it by the selected color. " +
        "Turn this off for a clear solid-color structure.")]
    bool preserveSourceTexture;

    [SerializeField]
    [Tooltip("Only these renderers are recolored. The setup tool fills this list once.")]
    Renderer[] targetRenderers = System.Array.Empty<Renderer>();

    MaterialPropertyBlock propertyBlock;

    public ColorVariant Variant => variant;
    public int TargetRendererCount => targetRenderers?.Length ?? 0;

    void OnEnable()
    {
        ApplyNow();
    }

    void OnValidate()
    {
        ApplyNow();
    }

    void OnDisable()
    {
        RestoreOriginalAppearance();
    }

    void OnDestroy()
    {
        RestoreOriginalAppearance();
    }

    public void ApplyNow()
    {
        Renderer[] renderers = GetRenderers();
        Color selectedColor = GetSelectedColor();
        bool showOriginal = variant == ColorVariant.Original;

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material sourceMaterial = materials[materialIndex];
                if (sourceMaterial == null)
                    continue;

                MaterialPropertyBlock block = GetPropertyBlock();
                block.Clear();
                targetRenderer.GetPropertyBlock(block, materialIndex);

                if (showOriginal)
                    ApplyOriginalProperties(sourceMaterial, block);
                else
                    ApplyVariantProperties(sourceMaterial, block, selectedColor);

                targetRenderer.SetPropertyBlock(block, materialIndex);
            }
        }
    }

    public void RestoreOriginalAppearance()
    {
        Renderer[] renderers = GetRenderers();
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material sourceMaterial = materials[materialIndex];
                if (sourceMaterial == null)
                    continue;

                MaterialPropertyBlock block = GetPropertyBlock();
                block.Clear();
                targetRenderer.GetPropertyBlock(block, materialIndex);
                ApplyOriginalProperties(sourceMaterial, block);
                targetRenderer.SetPropertyBlock(block, materialIndex);
            }
        }
    }

    public void CollectChildRenderers()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    Color GetSelectedColor()
    {
        return variant switch
        {
            ColorVariant.White => whiteColor,
            ColorVariant.Blue => blueColor,
            ColorVariant.Custom => customColor,
            _ => Color.white,
        };
    }

    Renderer[] GetRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
            return targetRenderers;

        return GetComponentsInChildren<Renderer>(true);
    }

    MaterialPropertyBlock GetPropertyBlock()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        return propertyBlock;
    }

    void ApplyVariantProperties(
        Material sourceMaterial,
        MaterialPropertyBlock block,
        Color selectedColor)
    {
        SetSupportedColorProperties(sourceMaterial, block, selectedColor);

        Texture replacementTexture = preserveSourceTexture
            ? null
            : Texture2D.whiteTexture;

        SetSupportedTextureProperty(
            sourceMaterial,
            block,
            BaseMapId,
            replacementTexture);
        SetSupportedTextureProperty(
            sourceMaterial,
            block,
            MainTextureId,
            replacementTexture);
    }

    static void ApplyOriginalProperties(
        Material sourceMaterial,
        MaterialPropertyBlock block)
    {
        if (sourceMaterial.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, sourceMaterial.GetColor(BaseColorId));
        if (sourceMaterial.HasProperty(LegacyColorId))
            block.SetColor(LegacyColorId, sourceMaterial.GetColor(LegacyColorId));
        if (sourceMaterial.HasProperty(BaseMapId))
            block.SetTexture(BaseMapId, sourceMaterial.GetTexture(BaseMapId));
        if (sourceMaterial.HasProperty(MainTextureId))
            block.SetTexture(MainTextureId, sourceMaterial.GetTexture(MainTextureId));
    }

    static void SetSupportedColorProperties(
        Material sourceMaterial,
        MaterialPropertyBlock block,
        Color color)
    {
        if (sourceMaterial.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, color);
        if (sourceMaterial.HasProperty(LegacyColorId))
            block.SetColor(LegacyColorId, color);
    }

    static void SetSupportedTextureProperty(
        Material sourceMaterial,
        MaterialPropertyBlock block,
        int propertyId,
        Texture replacementTexture)
    {
        if (!sourceMaterial.HasProperty(propertyId))
            return;

        Texture texture = replacementTexture != null
            ? replacementTexture
            : sourceMaterial.GetTexture(propertyId);
        block.SetTexture(propertyId, texture);
    }
}
