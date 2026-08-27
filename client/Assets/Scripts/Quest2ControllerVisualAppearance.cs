using UnityEngine;

/// <summary>
/// Applies Inspector-authored tint and brightness to one rendered Quest 2 controller model.
/// This component belongs on an OculusTouchForQuest2 model root. The property blocks leave
/// the shared Oculus material and all interaction-ray transforms unchanged.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class Quest2ControllerVisualAppearance : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Target Renderers")]
    [Tooltip("The renderer references authored for this OculusTouchForQuest2 model. These are assigned by Tools > XR > Quest 2 Controller > Configure Model Appearance In PPE Scene.")]
    [SerializeField] private Renderer[] m_TargetRenderers;

    [Header("Appearance")]
    [ColorUsage(false, true)]
    [SerializeField] private Color m_Color = Color.white;
    [Min(0f)]
    [SerializeField] private float m_Brightness = 1.2f;

    private MaterialPropertyBlock m_PropertyBlock;

    // Runs only when this component is explicitly added or reset in the Unity Editor.
    // It does not run as a Play Mode repair path and does not alter transforms or hierarchy.
    private void Reset()
    {
        m_TargetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Controller Appearance")]
    public void Apply()
    {
        m_PropertyBlock ??= new MaterialPropertyBlock();
        Apply(m_TargetRenderers, m_Color, m_Brightness);
    }

    private void Apply(Renderer[] renderers, Color color, float brightness)
    {
        if (renderers == null)
            return;

        Color adjustedColor = color * brightness;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                m_PropertyBlock.Clear();
                renderer.GetPropertyBlock(m_PropertyBlock, materialIndex);

                if (material.HasProperty(BaseColorId))
                    m_PropertyBlock.SetColor(BaseColorId, adjustedColor);
                else if (material.HasProperty(ColorId))
                    m_PropertyBlock.SetColor(ColorId, adjustedColor);
                else
                    continue;

                renderer.SetPropertyBlock(m_PropertyBlock, materialIndex);
            }
        }
    }
}
