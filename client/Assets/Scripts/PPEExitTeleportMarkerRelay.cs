using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Starts the finale return sequence from the authored exit marker. The original
/// scene still uses the marker's teleport request. The locomotion copy can instead
/// return when the player walks into the marker's authored collider footprint.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEExitTeleportMarkerRelay : MonoBehaviour
{
    [SerializeField] private BaseTeleportationInteractable m_ExitMarker;
    [SerializeField] private PPEFinaleController m_FinaleController;
    [Tooltip("켜면 텔레포트 요청 대신, 플레이어가 작성된 콜라이더 XZ 발자국 안으로 걸을 때 복귀합니다.")]
    [SerializeField] private bool m_ReturnOnWalkEnter;
    [SerializeField] private Transform m_Player;
    [SerializeField] private Collider m_EnterVolume;

    private bool m_HasLoggedMissingReferences;
    private bool m_Subscribed;

    private void OnEnable()
    {
        if (!HasRequiredReferences())
            return;

        if (m_ReturnOnWalkEnter || m_ExitMarker == null)
            return;

        m_ExitMarker.teleporting.AddListener(OnExitMarkerTeleporting);
        m_Subscribed = true;
    }

    private void OnDisable()
    {
        if (!m_Subscribed)
            return;

        m_Subscribed = false;
        if (m_ExitMarker != null)
            m_ExitMarker.teleporting.RemoveListener(OnExitMarkerTeleporting);
    }

    private void Update()
    {
        if (!m_ReturnOnWalkEnter || !HasRequiredReferences())
            return;

        if (!IsPlayerInsideEnterVolume())
            return;

        m_FinaleController.RequestExitReturn();
    }

    private void OnExitMarkerTeleporting(TeleportingEventArgs _)
    {
        m_FinaleController.RequestExitReturn();
    }

    private bool IsPlayerInsideEnterVolume()
    {
        Bounds bounds = m_EnterVolume.bounds;
        Vector3 position = m_Player.position;
        return position.x >= bounds.min.x &&
            position.x <= bounds.max.x &&
            position.z >= bounds.min.z &&
            position.z <= bounds.max.z;
    }

    private bool HasRequiredReferences()
    {
        bool hasFinale = m_FinaleController != null;
        bool hasWalkEnter = !m_ReturnOnWalkEnter || (m_Player != null && m_EnterVolume != null);
        bool hasTeleportEnter = m_ReturnOnWalkEnter || m_ExitMarker != null;
        if (hasFinale && hasWalkEnter && hasTeleportEnter)
            return true;

        if (!m_HasLoggedMissingReferences)
        {
            m_HasLoggedMissingReferences = true;
            Debug.LogError(
                m_ReturnOnWalkEnter
                    ? $"{nameof(PPEExitTeleportMarkerRelay)} on '{name}' requires the authored finale controller, player, and enter-volume references."
                    : $"{nameof(PPEExitTeleportMarkerRelay)} on '{name}' requires the authored exit marker and finale controller references.",
                this);
        }

        return false;
    }
}
