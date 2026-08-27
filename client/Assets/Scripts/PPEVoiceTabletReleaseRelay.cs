using UnityEngine;

[DisallowMultipleComponent]
public sealed class PPEVoiceTabletReleaseRelay : MonoBehaviour
{
    [SerializeField] private PPEMarkerToggleGrab m_TabletGrab;
    [SerializeField] private PPEVoiceFlowDirector m_Director;

    private void OnEnable()
    {
        if (m_TabletGrab != null)
            m_TabletGrab.Released += OnTabletReleased;
    }

    private void OnDisable()
    {
        if (m_TabletGrab != null)
            m_TabletGrab.Released -= OnTabletReleased;
    }

    private void OnTabletReleased() => m_Director?.NotifyTabletReleased();
}
