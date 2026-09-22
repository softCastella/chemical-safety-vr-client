using System;
using UnityEngine;

/// <summary>
/// Mirrors the visible state of scene-authored worn PPE renderers onto their
/// Shadows Only counterparts. Geometry and references are authored in Edit Mode.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEWornEquipmentShadowSync : MonoBehaviour
{
    [SerializeField] private Renderer[] m_SourceRenderers;
    [SerializeField] private Renderer[] m_ShadowRenderers;

    private bool m_IsConfigured;
    private bool m_HasLoggedConfigurationError;

    public Renderer[] SourceRenderers => m_SourceRenderers;
    public Renderer[] ShadowRenderers => m_ShadowRenderers;

    private void Awake()
    {
        m_IsConfigured = ValidateConfiguration();
        if (!m_IsConfigured)
            SetAllShadowsEnabled(false);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            SetAllShadowsEnabled(false);
            return;
        }

        if (!m_IsConfigured)
            m_IsConfigured = ValidateConfiguration();

        SyncVisibleState();
    }

    private void LateUpdate()
    {
        if (m_IsConfigured)
            SyncVisibleState();
    }

    private void OnDisable()
    {
        SetAllShadowsEnabled(false);
    }

    private bool ValidateConfiguration()
    {
        if (m_SourceRenderers == null ||
            m_ShadowRenderers == null ||
            m_SourceRenderers.Length == 0 ||
            m_SourceRenderers.Length != m_ShadowRenderers.Length)
        {
            LogConfigurationErrorOnce("requires paired source and shadow Renderer arrays.");
            return false;
        }

        for (int index = 0; index < m_SourceRenderers.Length; index++)
        {
            if (m_SourceRenderers[index] == null || m_ShadowRenderers[index] == null)
            {
                LogConfigurationErrorOnce($"has a missing Renderer reference at index {index}.");
                return false;
            }
        }

        return true;
    }

    private void SyncVisibleState()
    {
        for (int index = 0; index < m_SourceRenderers.Length; index++)
        {
            Renderer source = m_SourceRenderers[index];
            Renderer shadow = m_ShadowRenderers[index];
            shadow.enabled = source.enabled && source.gameObject.activeInHierarchy;
        }
    }

    private void SetAllShadowsEnabled(bool enabled)
    {
        foreach (Renderer shadow in m_ShadowRenderers ?? Array.Empty<Renderer>())
        {
            if (shadow != null)
                shadow.enabled = enabled;
        }
    }

    private void LogConfigurationErrorOnce(string detail)
    {
        if (m_HasLoggedConfigurationError)
            return;

        Debug.LogError(
            $"{nameof(PPEWornEquipmentShadowSync)} on '{name}' {detail} " +
            "Run Tools/PPE/Configure Worn PPE Shadows in Edit Mode and save the scene.",
            this);
        m_HasLoggedConfigurationError = true;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(Renderer[] sourceRenderers, Renderer[] shadowRenderers)
    {
        m_SourceRenderers = sourceRenderers;
        m_ShadowRenderers = shadowRenderers;
        m_IsConfigured = false;
        SetAllShadowsEnabled(false);
    }
#endif
}
