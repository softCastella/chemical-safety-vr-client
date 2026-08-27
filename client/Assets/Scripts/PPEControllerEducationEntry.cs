using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Routes the authored keyboard-canvas A entry to the controller guide state owner.
/// Only an XR tracked-device pointer may activate this UI entry.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEControllerEducationEntry : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PPEVoiceFlowDirector m_VoiceFlowDirector;

    private bool m_HasLoggedMissingDirector;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData is not TrackedDeviceEventData)
            return;

        if (m_VoiceFlowDirector == null)
        {
            if (!m_HasLoggedMissingDirector)
            {
                m_HasLoggedMissingDirector = true;
                Debug.LogError(
                    $"{nameof(PPEControllerEducationEntry)} on '{name}' requires an authored {nameof(PPEVoiceFlowDirector)} reference.",
                    this);
            }

            return;
        }

        m_VoiceFlowDirector.NotifyControllerEducationRequested();
    }
}
