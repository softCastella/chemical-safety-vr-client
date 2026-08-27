using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Inspector-authored tint and brightness for the unpacked metal shelving instance.
/// Uses a MaterialPropertyBlock so shared Unlit materials, meshes, and Transforms stay unchanged.
/// Wooden plank children are not targeted.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("PPE/Metal Shelving Visual Appearance")]
public sealed class PPEMetalShelvingVisualAppearance : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly Regex PartNamePattern = new Regex(
        @"^tripo_part_\d+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
        targetRenderers = CollectPartRenderers(transform);
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Metal Shelving Appearance")]
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

    public static Renderer[] CollectPartRenderers(Transform root)
    {
        if (root == null)
            return System.Array.Empty<Renderer>();

        var matches = new List<Renderer>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && PartNamePattern.IsMatch(renderer.gameObject.name))
                matches.Add(renderer);
        }

        return matches.ToArray();
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(Renderer[] configuredTargetRenderers, Color configuredColor, float configuredBrightness)
    {
        targetRenderers = configuredTargetRenderers;
        color = configuredColor;
        brightness = configuredBrightness;
        Apply();
    }
#endif
}
