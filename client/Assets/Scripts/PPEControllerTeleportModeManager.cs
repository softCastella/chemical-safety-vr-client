using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Switches a PPE controller between its normal Near-Far interactor and its
/// teleport interactor without allowing a hovered card or UI element to disable
/// the teleport-mode input action.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEControllerTeleportModeManager : MonoBehaviour
{
    [Header("Interactors")]
    [SerializeField] private NearFarInteractor m_NearFarInteractor;
    [SerializeField] private XRRayInteractor m_TeleportInteractor;

    [Header("Teleport Actions")]
    [SerializeField] private InputActionReference m_TeleportMode;
    [SerializeField] private InputActionReference m_TeleportModeCancel;

    [Header("Scenario Context")]
    [SerializeField] private bool m_RequireScenarioSelection;

    private bool m_PostponedDeactivateTeleport;
    private bool m_TeleportAvailable;

    private static bool s_HasScenarioSelection;
    private static bool s_VoiceMovementGateOpen = true;
    private static event Action s_ScenarioSelected;
    public static event Action TeleportModeStartedGlobal;
    public static event Action TeleportModeCanceledGlobal;

    public event Action TeleportModeStarted;
    public event Action TeleportModeCanceled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScenarioSelectionState()
    {
        s_HasScenarioSelection = false;
        s_VoiceMovementGateOpen = true;
        s_ScenarioSelected = null;
        TeleportModeStartedGlobal = null;
        TeleportModeCanceledGlobal = null;
    }

    public static void SetVoiceMovementGate(bool open)
    {
        if (s_VoiceMovementGateOpen == open)
            return;

        s_VoiceMovementGateOpen = open;
        s_ScenarioSelected?.Invoke();
    }

    public static void NotifyScenarioSelected(int scenarioIndex)
    {
        if (scenarioIndex < 0)
            return;

        s_HasScenarioSelection = true;
        s_ScenarioSelected?.Invoke();
    }

    public static void NotifyScenarioReadyForMovement()
    {
        s_HasScenarioSelection = true;
        s_ScenarioSelected?.Invoke();
    }

    private void OnEnable()
    {
        s_ScenarioSelected += OnScenarioSelected;
        m_TeleportAvailable = IsTeleportAvailable();
        SetInitialInteractorState();
        SetActionCallbacks(true);
        EnableTeleportActions();
    }

    private void Start()
    {
        // InputActionManager can update action states later in its own OnEnable.
        // Reassert these two project-owned locomotion actions after initialization.
        EnableTeleportActions();
    }

    private void OnDisable()
    {
        s_ScenarioSelected -= OnScenarioSelected;
        SetActionCallbacks(false);
        m_PostponedDeactivateTeleport = false;

        if (m_TeleportInteractor != null)
            m_TeleportInteractor.gameObject.SetActive(false);

        if (m_NearFarInteractor != null)
            m_NearFarInteractor.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!m_PostponedDeactivateTeleport)
            return;

        // Keep the teleport interactor alive through this frame's XRI selection
        // exit so TeleportationAnchor can process teleport-on-release first.
        if (m_TeleportInteractor != null)
            m_TeleportInteractor.gameObject.SetActive(false);

        m_PostponedDeactivateTeleport = false;
    }

    private void SetInitialInteractorState()
    {
        m_PostponedDeactivateTeleport = false;

        if (m_TeleportInteractor != null)
            m_TeleportInteractor.gameObject.SetActive(false);

        if (m_NearFarInteractor != null)
            m_NearFarInteractor.gameObject.SetActive(true);
    }

    private void SetActionCallbacks(bool add)
    {
        InputAction teleportModeAction = m_TeleportMode != null ? m_TeleportMode.action : null;
        InputAction cancelAction = m_TeleportModeCancel != null ? m_TeleportModeCancel.action : null;

        if (teleportModeAction != null)
        {
            if (add)
            {
                teleportModeAction.performed += OnStartTeleport;
                teleportModeAction.canceled += OnCancelTeleport;
            }
            else
            {
                teleportModeAction.performed -= OnStartTeleport;
                teleportModeAction.canceled -= OnCancelTeleport;
            }
        }

        if (cancelAction != null)
        {
            if (add)
                cancelAction.performed += OnCancelTeleport;
            else
                cancelAction.performed -= OnCancelTeleport;
        }
    }

    private void EnableTeleportActions()
    {
        m_TeleportMode?.action?.Enable();
        m_TeleportModeCancel?.action?.Enable();
    }

    private void OnStartTeleport(InputAction.CallbackContext context)
    {
        if (!m_TeleportAvailable)
            return;

        m_PostponedDeactivateTeleport = false;

        if (m_NearFarInteractor != null)
            m_NearFarInteractor.gameObject.SetActive(false);

        if (m_TeleportInteractor != null)
            m_TeleportInteractor.gameObject.SetActive(true);

        TeleportModeStarted?.Invoke();
        TeleportModeStartedGlobal?.Invoke();
    }

    private void OnScenarioSelected()
    {
        m_TeleportAvailable = IsTeleportAvailable();

        if (!m_TeleportAvailable && m_TeleportInteractor != null && m_TeleportInteractor.gameObject.activeSelf)
        {
            m_TeleportInteractor.gameObject.SetActive(false);
            m_NearFarInteractor?.gameObject.SetActive(true);
        }
    }

    private void OnCancelTeleport(InputAction.CallbackContext context)
    {
        if (m_NearFarInteractor != null)
            m_NearFarInteractor.gameObject.SetActive(true);

        m_PostponedDeactivateTeleport = true;
        TeleportModeCanceled?.Invoke();
        TeleportModeCanceledGlobal?.Invoke();
    }

    private bool IsTeleportAvailable()
    {
        return s_VoiceMovementGateOpen
            && (!m_RequireScenarioSelection || s_HasScenarioSelection);
    }
}
