using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Connects the project-owned HangulKeyboardController TextSubmitted event without
/// making Assembly-CSharp depend directly on the keyboard asmdef.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEVoiceKeyboardEventRelay : MonoBehaviour
{
    [SerializeField] private MonoBehaviour m_KeyboardController;
    [SerializeField] private PPEVoiceFlowDirector m_Director;

    private EventInfo m_TextSubmittedEvent;
    private Action<string> m_Handler;

    private void OnEnable()
    {
        if (m_KeyboardController == null || m_Director == null)
        {
            Debug.LogError(
                "PPEVoiceKeyboardEventRelay requires both keyboard controller and director references.",
                this);
            return;
        }

        m_TextSubmittedEvent = m_KeyboardController.GetType().GetEvent(
            "TextSubmitted",
            BindingFlags.Instance | BindingFlags.Public);
        if (m_TextSubmittedEvent == null || m_TextSubmittedEvent.EventHandlerType != typeof(Action<string>))
        {
            Debug.LogError(
                $"PPEVoiceKeyboardEventRelay could not find public Action<string> TextSubmitted on " +
                $"{m_KeyboardController.GetType().FullName}.", this);
            m_TextSubmittedEvent = null;
            return;
        }

        m_Handler = m_Director.NotifyNameSubmitted;
        m_TextSubmittedEvent.AddEventHandler(m_KeyboardController, m_Handler);
    }

    private void OnDisable()
    {
        if (m_TextSubmittedEvent != null && m_KeyboardController != null && m_Handler != null)
            m_TextSubmittedEvent.RemoveEventHandler(m_KeyboardController, m_Handler);

        m_TextSubmittedEvent = null;
        m_Handler = null;
    }
}
