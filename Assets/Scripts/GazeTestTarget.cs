using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Color feedback for a gaze target in the gaze test scene. Uses a MaterialPropertyBlock so every
/// target can share one material without instancing it per object.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class GazeTestTarget : MonoBehaviour
{
    static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    Color m_IdleColor = new Color(0.55f, 0.57f, 0.62f);

    [SerializeField]
    Color m_HoverColor = new Color(0.2f, 0.6f, 0.95f);

    [SerializeField]
    Color m_SelectedColor = new Color(0.25f, 0.85f, 0.4f);

    Renderer m_Renderer;
    MaterialPropertyBlock m_Block;
    XRBaseInteractable m_Interactable;
    bool m_Hovered;
    bool m_Selected;

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_Block = new MaterialPropertyBlock();
        m_Interactable = GetComponent<XRBaseInteractable>();
        Apply();
    }

    void OnEnable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.hoverEntered.AddListener(OnHoverEntered);
        m_Interactable.hoverExited.AddListener(OnHoverExited);
        m_Interactable.selectEntered.AddListener(OnSelectEntered);
        m_Interactable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.hoverEntered.RemoveListener(OnHoverEntered);
        m_Interactable.hoverExited.RemoveListener(OnHoverExited);
        m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactable.selectExited.RemoveListener(OnSelectExited);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        m_Hovered = true;
        Apply();
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        m_Hovered = false;
        Apply();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        m_Selected = true;
        Apply();
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        m_Selected = false;
        Apply();
    }

    void Apply()
    {
        var color = m_Selected ? m_SelectedColor : m_Hovered ? m_HoverColor : m_IdleColor;
        m_Block.SetColor(k_BaseColor, color);
        m_Renderer.SetPropertyBlock(m_Block);
    }
}
