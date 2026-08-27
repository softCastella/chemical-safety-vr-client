using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

/// <summary>
/// Relays actual XRI teleportation lifecycle events to PPEVoiceFlowDirector.
/// References are Inspector-authored; no runtime fallback is used.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEVoiceTeleportEventRelay : MonoBehaviour
{
    [SerializeField] private TeleportationProvider m_TeleportationProvider;
    [SerializeField] private PPEVoiceFlowDirector m_Director;
    [SerializeField] private TeleportationAnchor m_CenterMarker;
    [SerializeField] private TeleportationAnchor m_MirrorMarker;
    private readonly List<BaseTeleportationInteractable> m_TeleportTargets = new();

    private void OnEnable()
    {
        if (m_TeleportationProvider == null || m_Director == null)
        {
            Debug.LogError(
                "PPEVoiceTeleportEventRelay requires both TeleportationProvider and PPEVoiceFlowDirector references.",
                this);
            return;
        }

        m_TeleportationProvider.locomotionStarted += OnLocomotionStarted;
        m_TeleportationProvider.locomotionEnded += OnLocomotionEnded;

        foreach (BaseTeleportationInteractable target in FindObjectsByType<BaseTeleportationInteractable>(FindObjectsInactive.Exclude))
        {
            target.teleporting.AddListener(OnTeleportQueued);
            m_TeleportTargets.Add(target);
        }
    }

    private void OnDisable()
    {
        if (m_TeleportationProvider == null)
            return;

        m_TeleportationProvider.locomotionStarted -= OnLocomotionStarted;
        m_TeleportationProvider.locomotionEnded -= OnLocomotionEnded;

        foreach (BaseTeleportationInteractable target in m_TeleportTargets)
            if (target != null)
                target.teleporting.RemoveListener(OnTeleportQueued);
        m_TeleportTargets.Clear();
    }

    private void OnLocomotionStarted(LocomotionProvider _)
    {
        // The controller's cancel action is raised when the teleport button is
        // released, before XRI accepts and performs the request.  Treat the
        // provider lifecycle start as the authoritative accepted teleport so
        // the instruction cannot restart during or after the move.
        m_Director?.NotifyTeleportArrived();
    }

    private void OnTeleportQueued(TeleportingEventArgs args)
    {
        // This event is raised by a location marker immediately after its request is accepted.
        // Stop the active instruction source here rather than waiting for the
        // locomotion lifecycle; an instant teleport can otherwise leave 002
        // audible after the player has already moved.
        AudioManager.Instance?.StopVoice();
        m_Director?.NotifyTeleportArrived();

        // The voice director de-duplicates its one-time narration itself. The
        // finale must still receive every mirror arrival so an incomplete PPE
        // check can be retried after the player finishes dressing.
        if (args.interactableObject == null)
            return;

        if (ReferenceEquals(args.interactableObject, m_CenterMarker))
            m_Director?.NotifyCenterMarkerArrived();
        else if (ReferenceEquals(args.interactableObject, m_MirrorMarker))
            m_Director?.NotifyMirrorMarkerArrived();
    }

    private void OnLocomotionEnded(LocomotionProvider _)
    {
        // Keep this as an idempotent completion signal for provider variants
        // whose movement finishes on a later frame.
        m_Director?.NotifyTeleportArrived();
    }
}
