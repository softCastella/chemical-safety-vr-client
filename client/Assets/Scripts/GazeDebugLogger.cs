using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Runtime diagnostics for the gaze station. Every message is prefixed with <see cref="k_Tag"/> so
/// the Editor log can be filtered down to just gaze activity.
///
/// Place it in the scene (Tools > Gaze > Build Gaze Test Scene does this) rather than having it
/// bootstrap itself - self-attaching made it run in lessons that have nothing to do with gaze.
/// </summary>
public class GazeDebugLogger : MonoBehaviour
{
    const string k_Tag = "[GAZE]";

    XRGazeInteractor m_Interactor;
    bool m_WasActive;
    float m_HoverStartTime;
    string m_HoverTarget;

    void Start()
    {
        m_Interactor = FindAnyObjectByType<XRGazeInteractor>(FindObjectsInactive.Include);
        if (m_Interactor == null)
        {
            Debug.LogWarning($"{k_Tag} No XRGazeInteractor in the scene. Nothing to monitor.");
            enabled = false;
            return;
        }

        LogEyeTrackingSupport();
        LogInteractorConfig();
        LogGazeInteractables();

        m_Interactor.hoverEntered.AddListener(OnHoverEntered);
        m_Interactor.hoverExited.AddListener(OnHoverExited);
        m_Interactor.selectEntered.AddListener(OnSelectEntered);
        m_Interactor.selectExited.AddListener(OnSelectExited);

        m_WasActive = m_Interactor.isActiveAndEnabled;
        Debug.Log($"{k_Tag} Ready. Gaze Interactor is currently {(m_WasActive ? "ACTIVE" : "INACTIVE")}." +
            (m_WasActive ? string.Empty : " Walk into the GazeActivationZone by the gaze table to turn it on."));
    }

    void OnDestroy()
    {
        if (m_Interactor == null)
            return;

        m_Interactor.hoverEntered.RemoveListener(OnHoverEntered);
        m_Interactor.hoverExited.RemoveListener(OnHoverExited);
        m_Interactor.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactor.selectExited.RemoveListener(OnSelectExited);
    }

    void Update()
    {
        // The demo's ToggleComponentZone flips the interactor on and off as the player enters
        // and leaves the trigger volume, so surface that transition explicitly.
        var isActive = m_Interactor.isActiveAndEnabled;
        if (isActive == m_WasActive)
            return;

        m_WasActive = isActive;
        Debug.Log($"{k_Tag} {Stamp()} Gaze Interactor -> {(isActive ? "ACTIVE (entered zone)" : "INACTIVE (left zone)")}");
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        m_HoverStartTime = Time.time;
        m_HoverTarget = Name(args.interactableObject);

        var detail = string.Empty;
        if (args.interactableObject is XRBaseInteractable interactable)
        {
            detail = $" | allowGazeSelect={interactable.allowGazeSelect}" +
                $" gazeAssistance={interactable.allowGazeAssistance}" +
                $" timeToSelect={SelectTimeFor(interactable):0.##}s";
        }

        Debug.Log($"{k_Tag} {Stamp()} HOVER ENTER  {m_HoverTarget}{detail}");
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"{k_Tag} {Stamp()} HOVER EXIT   {Name(args.interactableObject)} | dwelled {Time.time - m_HoverStartTime:0.##}s");
        m_HoverTarget = null;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"{k_Tag} {Stamp()} SELECT       {Name(args.interactableObject)} | after {Time.time - m_HoverStartTime:0.##}s of dwell");
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"{k_Tag} {Stamp()} DESELECT     {Name(args.interactableObject)}");
    }

    void LogEyeTrackingSupport()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.EyeTracking, devices);

        if (devices.Count > 0)
        {
            Debug.Log($"{k_Tag} Eye tracking device found: {devices[0].name}. Using real eye gaze.");
            return;
        }

        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device.layout == "EyeGaze")
            {
                Debug.Log($"{k_Tag} Eye gaze device found: {device.displayName}. Using real eye gaze.");
                return;
            }
        }

        Debug.Log($"{k_Tag} No eye tracking device. Running in HEAD GAZE fallback " +
            "(the OpenXR Eye Gaze Interaction Profile is disabled in this project).");
    }

    void LogInteractorConfig()
    {
        Debug.Log($"{k_Tag} Interactor config: hoverToSelect={m_Interactor.hoverToSelect}" +
            $" hoverTimeToSelect={m_Interactor.hoverTimeToSelect:0.##}s" +
            $" autoDeselect={m_Interactor.autoDeselect}" +
            $" timeToAutoDeselect={m_Interactor.timeToAutoDeselect:0.##}s" +
            $" maxRaycastDistance={m_Interactor.maxRaycastDistance:0.##}");

        var assistance = FindAnyObjectByType<XRGazeAssistance>(FindObjectsInactive.Include);
        Debug.Log($"{k_Tag} XRGazeAssistance: {(assistance == null ? "not present" : assistance.isActiveAndEnabled ? "enabled" : "present but disabled")}");
    }

    void LogGazeInteractables()
    {
        var interactables = FindObjectsByType<XRBaseInteractable>(FindObjectsInactive.Include);
        var count = 0;
        foreach (var interactable in interactables)
        {
            if (!interactable.allowGazeInteraction)
                continue;

            count++;
            Debug.Log($"{k_Tag}   gaze target: {interactable.name}" +
                $" | allowGazeSelect={interactable.allowGazeSelect}" +
                $" gazeAssistance={interactable.allowGazeAssistance}" +
                $" timeToSelect={SelectTimeFor(interactable):0.##}s", interactable);
        }

        Debug.Log($"{k_Tag} {count} interactable(s) in the scene have Allow Gaze Interaction enabled.");
    }

    float SelectTimeFor(XRBaseInteractable interactable) =>
        interactable.overrideGazeTimeToSelect ? interactable.gazeTimeToSelect : m_Interactor.hoverTimeToSelect;

    static string Name(IXRInteractable interactable) =>
        interactable?.transform != null ? interactable.transform.name : "<null>";

    static string Stamp() => $"t={Time.time:0.00}";
}
