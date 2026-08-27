using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Colors an interactable while it holds focus, so a state that is otherwise invisible can be seen.
///
/// Focus arrives with selection and, unlike selection, stays after the object is released - it only
/// clears when the same hand grabs at nothing. Watching the colour survive a release is the whole
/// point of the lesson, so this deliberately reacts to focus alone and ignores hover and select.
/// Uses a MaterialPropertyBlock so several objects can share one material.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class FocusHighlight : MonoBehaviour
{
    static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    Color m_IdleColor = new Color(0.55f, 0.57f, 0.62f);

    [SerializeField]
    Color m_FocusedColor = new Color(0.95f, 0.72f, 0.2f);

    Renderer m_Renderer;
    MaterialPropertyBlock m_Block;
    XRBaseInteractable m_Interactable;
    bool m_Focused;

    void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_Block = new MaterialPropertyBlock();
        m_Interactable = GetComponentInParent<XRBaseInteractable>();
        Apply();
    }

    void OnEnable()
    {
        if (m_Interactable == null)
        {
            Debug.LogWarning($"[Focus] {name} has no interactable above it, so it can never gain focus.", this);
            return;
        }

        m_Interactable.focusEntered.AddListener(OnFocusEntered);
        m_Interactable.focusExited.AddListener(OnFocusExited);
    }

    void OnDisable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.focusEntered.RemoveListener(OnFocusEntered);
        m_Interactable.focusExited.RemoveListener(OnFocusExited);
    }

    void OnFocusEntered(FocusEnterEventArgs args)
    {
        m_Focused = true;
        Apply();
    }

    void OnFocusExited(FocusExitEventArgs args)
    {
        m_Focused = false;
        Apply();
    }

    void Apply()
    {
        m_Block.SetColor(k_BaseColor, m_Focused ? m_FocusedColor : m_IdleColor);
        m_Renderer.SetPropertyBlock(m_Block);
    }
}
