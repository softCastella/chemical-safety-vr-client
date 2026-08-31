using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Chooses a Game View XR simulator after allowing OpenXR time to register a physical HMD,
/// or immediately when the author explicitly enables the Game View test-mode override.
/// The mode is fixed before the simulator activates because the simulator removes physical
/// HMD devices while it is active.
/// </summary>
[DefaultExecutionOrder(-32000)]
[DisallowMultipleComponent]
public sealed class PhysicalHmdSimulatorGate : MonoBehaviour
{
    [Header("Test Simulator")]
    [Tooltip("The disabled-by-default XR Device Simulator child created for Game View testing.")]
    [SerializeField] private GameObject m_SimulatorRoot;

    [Tooltip("When enabled, starts the simulator only if no physical XRHMD exists at Play start. Disable this component's GameObject to turn off this automatic test path entirely.")]
    [SerializeField] private bool m_EnableSimulatorWhenNoPhysicalHmd = true;

    [Tooltip("Automatic Game View fallback only: maximum realtime seconds to wait for OpenXR to register a physical HMD before enabling the simulator.")]
    [SerializeField, Min(0f)] private float m_PhysicalHmdDetectionTimeout = 3f;

    [Tooltip("Game View test mode only. When checked, force the mouse/keyboard XR Device Simulator even if Quest Link or another physical HMD is connected. Do not use this for headset testing; the simulator takes over the physical XR input for this Play session.")]
    [InspectorName("Game View Test Mode")]
    [SerializeField] private bool m_ForceGameViewTestMode;

    [Tooltip("Game View simulator only: the initial camera height above the XR Origin floor, in metres. Physical HMD sessions keep their authored offset.")]
    [SerializeField, Min(0f)] private float m_GameViewStartEyeHeight = 1.56f;

    [Tooltip("XR Origin whose Camera Offset is used by the Game View simulator.")]
    [SerializeField] private XROrigin m_GameViewXrOrigin;

    [Tooltip("XR Device Simulator used only by the Game View fallback path.")]
    [SerializeField] private XRDeviceSimulator m_GameViewSimulator;

    [Tooltip("Game View simulator only: vertical offset applied to both controller starting poses. This keeps the hands at PPE marker height without affecting a physical HMD.")]
    [SerializeField] private float m_GameViewControllerStartHeightOffset = -1.15f;

    [Header("Game View Navigation")]
    [Tooltip("Game View simulator only: yaw rotation speed for the Left/Right arrow keys, in degrees per second.")]
    [SerializeField, Min(0f)] private float m_GameViewTurnSpeedDegrees = 120f;

    [Tooltip("Scene-authored PPE hand visual roots hidden only while the Game View simulator is active. Their active state and equipment state are not changed.")]
    [SerializeField] private Transform[] m_GameViewHandVisualRoots;

    [Header("Game View Mouse UI")]
    [Tooltip("The scene XR UI Input Module. Game View mouse actions are assigned only when no physical HMD is present.")]
    [SerializeField] private XRUIInputModule m_XrUiInputModule;

    [Tooltip("Project UI Point action containing the Mouse position binding.")]
    [SerializeField] private InputActionReference m_MousePointAction;

    [Tooltip("Project UI Click action containing the Mouse left-button binding.")]
    [SerializeField] private InputActionReference m_MouseLeftClickAction;

    [Tooltip("The authored right-hand Near-Far Interactor used for Game View mouse selection of PPE grab markers.")]
    [SerializeField] private NearFarInteractor m_GameViewMouseSelectInteractor;

    [Tooltip("Scene-authored Game View virtual-hand anchor. Body-proximity PPE attaches here while selected by the mouse pointer.")]
    [SerializeField] private Transform m_GameViewMouseGrabAnchor;

    [Header("Game View Voice Skip")]
    [Tooltip("Scene-authored voice flow. Background clicks use its existing state-safe voice skip rules.")]
    [SerializeField] private PPEVoiceFlowDirector m_GameViewVoiceFlowDirector;

    [Tooltip("Scene-authored hazmat wearer. Game View temporarily uses the simulator camera as its body/head reference.")]
    [SerializeField] private PPEHazmatEquipController m_GameViewHazmatEquipController;

    private readonly List<(CurveVisualController curve, bool curveEnabled, LineRenderer line, bool lineEnabled)> m_RayVisualStates = new();
    private readonly List<(Renderer renderer, bool forceRenderingOff)> m_HandRendererStates = new();
    private readonly List<RaycastResult> m_UiRaycastResults = new();
    private bool m_UseGameViewSimulator;
    private bool m_GameViewMouseUiConfigured;
    private bool m_HasReportedMissingMouseTeleportCamera;
    private bool m_HasReportedMissingMouseSelectInteractor;
    private bool m_HasReportedMissingMouseGrabAnchor;
    private bool m_HasReportedMissingGameViewVoiceFlow;
    private bool m_HasReportedMissingGameViewHazmatEquip;
    private XRGrabInteractable m_MouseSelectedGrab;
    private PPEActionPanelController m_MouseSelectedPanel;
    private Transform m_PreviousMouseInteractorAttach;
    private Transform m_PreviousMouseGrabAttach;
    private bool m_PreviousMouseGrabUseDynamicAttach;
    private bool m_MouseGrabOverridesApplied;
    private Plane m_MousePointerDragPlane;
    private bool m_MousePointerDragPlaneActive;
    private InputActionReference m_PreviousPointAction;
    private InputActionReference m_PreviousLeftClickAction;
    private bool m_PreviousEnableMouseInput;
    private bool m_GameViewEyeHeightApplied;
    private bool m_GameViewHazmatHeadOverrideApplied;
    private bool m_StartCompleted;
    private Coroutine m_HmdDetectionRoutine;
    private Coroutine m_ControllerHeightAfterActivationRoutine;
    private Vector3 m_PreviousGameViewCameraOffsetLocalPosition;

    private static readonly List<XRDisplaySubsystem> s_XrDisplaySubsystems = new();

    public bool IsGameViewSimulatorActive => m_UseGameViewSimulator;
    public event Action<bool> SimulatorModeChanged;
    public Transform GameViewForwardSource =>
        m_GameViewXrOrigin != null && m_GameViewXrOrigin.Camera != null
            ? m_GameViewXrOrigin.Camera.transform
            : null;
    public Transform GameViewMouseGrabAnchor => m_GameViewMouseGrabAnchor;

    private static readonly FieldInfo s_LeftControllerStateField = typeof(XRDeviceSimulator).GetField(
        "m_LeftControllerState",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo s_RightControllerStateField = typeof(XRDeviceSimulator).GetField(
        "m_RightControllerState",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        if (m_SimulatorRoot == null)
        {
            Debug.LogError($"{nameof(PhysicalHmdSimulatorGate)} on '{name}' requires a simulator root reference.", this);
            return;
        }

        m_SimulatorRoot.SetActive(false);

#if !UNITY_EDITOR
        // Game View simulation is an Editor-only authoring aid. A delayed
        // OpenXR startup in an Android player must never activate simulated
        // HMD/controller devices or replace the physical Quest input path.
        return;
#endif

        if (m_ForceGameViewTestMode)
        {
            ActivateGameViewSimulator();
            return;
        }

        if (!m_EnableSimulatorWhenNoPhysicalHmd || HasPhysicalHmdOrRunningDisplay())
            return;

        m_HmdDetectionRoutine = StartCoroutine(ResolveAutomaticSimulatorMode());
    }

    private void Update()
    {
        if (!m_UseGameViewSimulator)
            return;

        UpdateGameViewArrowTurn();

        SynchronizeMouseManualInteraction();

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        UpdateMouseGrabAnchor(mousePosition);

        bool cancelMouseGrab = Mouse.current.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
        if (cancelMouseGrab && m_MouseSelectedGrab != null)
        {
            EndMouseManualInteraction();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // UI owns the click before the direct teleport fallback. This check uses
        // the current mouse position instead of relying on the EventSystem's
        // previous-frame pointer state.
        if (IsPointerOverInteractiveUi(mousePosition))
            return;

        // A body-proximity PPE keeps its manual Select latched. The next click
        // is the Game View equivalent of controller Activate; it never toggles
        // Select off merely because the moved marker is still under the cursor.
        if (TryActivateMouseHeldBodyProximityPpe())
            return;

        Camera camera = Camera.main;
        if (camera == null)
        {
            if (!m_HasReportedMissingMouseTeleportCamera)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires a MainCamera for Game View marker teleport.",
                    this);
                m_HasReportedMissingMouseTeleportCamera = true;
            }

            return;
        }

        Ray ray = camera.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            TrySkipVoiceFromGameViewBackgroundClick();
            return;
        }

        TeleportationAnchor anchor = hit.collider.GetComponentInParent<TeleportationAnchor>();
        if (anchor != null && anchor.isActiveAndEnabled)
        {
            AudioManager.Instance?.StopVoice();
            anchor.RequestTeleport();
            return;
        }

        PPETabletChecklistController tabletChecklist = hit.collider.GetComponentInParent<PPETabletChecklistController>();
        XRGrabInteractable tabletGrab = tabletChecklist != null
            ? tabletChecklist.GetComponent<XRGrabInteractable>()
            : null;
        if (tabletChecklist != null && tabletChecklist.isActiveAndEnabled &&
            tabletGrab != null && m_GameViewMouseSelectInteractor != null &&
            m_GameViewMouseSelectInteractor.IsSelecting(tabletGrab) &&
            !tabletChecklist.IsDocumentCompleted)
        {
            tabletChecklist.CompleteForTesting();
            return;
        }

        if (TryToggleMouseGrab(hit.collider, mousePosition))
            return;

        TrySkipVoiceFromGameViewBackgroundClick();
    }

    private void Start()
    {
        m_StartCompleted = true;
        if (m_UseGameViewSimulator)
            ApplyGameViewControllerStartHeight();
    }

    private void OnDisable()
    {
        if (m_HmdDetectionRoutine != null)
        {
            StopCoroutine(m_HmdDetectionRoutine);
            m_HmdDetectionRoutine = null;
        }
        if (m_ControllerHeightAfterActivationRoutine != null)
        {
            StopCoroutine(m_ControllerHeightAfterActivationRoutine);
            m_ControllerHeightAfterActivationRoutine = null;
        }

        EndMouseManualInteraction();
        RestoreGameViewMouseUi();
        RestoreGameViewStartEyeHeight();
        RestoreGameViewHazmatHeadOverride();
        RestoreRayVisuals();
        RestoreHandVisuals();

        bool wasUsingGameViewSimulator = m_UseGameViewSimulator;
        m_UseGameViewSimulator = false;
        if (m_SimulatorRoot != null && m_SimulatorRoot.activeSelf)
            m_SimulatorRoot.SetActive(false);
        if (wasUsingGameViewSimulator)
            SimulatorModeChanged?.Invoke(false);
    }

    private IEnumerator ResolveAutomaticSimulatorMode()
    {
        float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, m_PhysicalHmdDetectionTimeout);
        while (Time.realtimeSinceStartup < deadline)
        {
            if (HasPhysicalHmdOrRunningDisplay())
            {
                m_HmdDetectionRoutine = null;
                yield break;
            }

            yield return null;
        }

        m_HmdDetectionRoutine = null;
        if (!HasPhysicalHmdOrRunningDisplay())
            ActivateGameViewSimulator();
    }

    private void ActivateGameViewSimulator()
    {
        if (m_UseGameViewSimulator || m_SimulatorRoot == null)
            return;

        m_UseGameViewSimulator = true;
        m_SimulatorRoot.SetActive(true);
        SimulatorModeChanged?.Invoke(true);

        // With no tracked physical pose, XRI's CurveVisualController can submit
        // non-finite fallback points before the simulator has a usable device
        // pose. The interactor itself remains active; only its line rendering
        // is suppressed for the Game View fallback session.
        ApplyGameViewStartEyeHeight();
        ApplyGameViewHazmatHeadOverride();
        ConfigureGameViewMouseUi();
        DisableRayVisualsForGameViewSimulator();
        HideHandVisualsForGameViewSimulator();

        if (m_StartCompleted)
            m_ControllerHeightAfterActivationRoutine = StartCoroutine(ApplyControllerHeightAfterActivation());
    }

    private IEnumerator ApplyControllerHeightAfterActivation()
    {
        yield return null;
        m_ControllerHeightAfterActivationRoutine = null;
        if (m_UseGameViewSimulator)
            ApplyGameViewControllerStartHeight();
    }

    private void UpdateGameViewArrowTurn()
    {
        if (Keyboard.current == null || m_GameViewXrOrigin == null || m_GameViewXrOrigin.Origin == null)
            return;

        float turnInput = 0f;
        if (Keyboard.current.leftArrowKey.isPressed)
            turnInput -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed)
            turnInput += 1f;
        if (Mathf.Approximately(turnInput, 0f))
            return;

        Transform origin = m_GameViewXrOrigin.Origin.transform;
        Transform cameraTransform = m_GameViewXrOrigin.Camera != null
            ? m_GameViewXrOrigin.Camera.transform
            : null;
        Vector3 pivot = cameraTransform != null ? cameraTransform.position : origin.position;
        origin.RotateAround(
            pivot,
            Vector3.up,
            turnInput * Mathf.Max(0f, m_GameViewTurnSpeedDegrees) * Time.deltaTime);
    }

    private static bool HasPhysicalHmd()
    {
        foreach (UnityEngine.InputSystem.InputDevice device in InputSystem.devices)
        {
            if (device is XRHMD && device is not XRSimulatedHMD)
                return true;
        }

        return false;
    }

    private static bool HasPhysicalHmdOrRunningDisplay()
    {
        if (HasPhysicalHmd())
            return true;

        s_XrDisplaySubsystems.Clear();
        SubsystemManager.GetSubsystems(s_XrDisplaySubsystems);
        foreach (XRDisplaySubsystem display in s_XrDisplaySubsystems)
        {
            if (display != null && display.running)
                return true;
        }

        return false;
    }

    private void ApplyGameViewStartEyeHeight()
    {
        if (m_GameViewXrOrigin == null || m_GameViewXrOrigin.CameraFloorOffsetObject == null)
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} requires the Game View XR Origin and its Camera Offset reference.",
                this);
            return;
        }

        Transform cameraOffset = m_GameViewXrOrigin.CameraFloorOffsetObject.transform;
        m_PreviousGameViewCameraOffsetLocalPosition = cameraOffset.localPosition;
        cameraOffset.localPosition = new Vector3(
            m_PreviousGameViewCameraOffsetLocalPosition.x,
            m_GameViewStartEyeHeight,
            m_PreviousGameViewCameraOffsetLocalPosition.z);
        m_GameViewEyeHeightApplied = true;
    }

    private void RestoreGameViewStartEyeHeight()
    {
        if (!m_GameViewEyeHeightApplied)
            return;

        if (m_GameViewXrOrigin != null && m_GameViewXrOrigin.CameraFloorOffsetObject != null)
            m_GameViewXrOrigin.CameraFloorOffsetObject.transform.localPosition = m_PreviousGameViewCameraOffsetLocalPosition;

        m_GameViewEyeHeightApplied = false;
    }

    private void ApplyGameViewHazmatHeadOverride()
    {
        Transform gameViewHead = GameViewForwardSource;
        if (m_GameViewHazmatEquipController == null || gameViewHead == null)
        {
            if (!m_HasReportedMissingGameViewHazmatEquip)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires the scene-authored hazmat equip controller " +
                    "and XR Origin Camera for Game View body-proximity wear.",
                    this);
                m_HasReportedMissingGameViewHazmatEquip = true;
            }

            return;
        }

        m_GameViewHazmatEquipController.SetGameViewHeadOverride(gameViewHead);
        m_GameViewHazmatHeadOverrideApplied = true;
    }

    private void RestoreGameViewHazmatHeadOverride()
    {
        if (!m_GameViewHazmatHeadOverrideApplied)
            return;

        if (m_GameViewHazmatEquipController != null)
            m_GameViewHazmatEquipController.ClearGameViewHeadOverride();

        m_GameViewHazmatHeadOverrideApplied = false;
    }

    private void ApplyGameViewControllerStartHeight()
    {
        if (m_GameViewSimulator == null)
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} requires the Game View XR Device Simulator reference.",
                this);
            return;
        }

        if (!ApplyControllerStartHeight(s_LeftControllerStateField)
            || !ApplyControllerStartHeight(s_RightControllerStateField))
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} could not apply the Game View controller start height to XR Device Simulator.",
                this);
        }
    }

    private bool ApplyControllerStartHeight(FieldInfo stateField)
    {
        if (stateField == null)
            return false;

        object controllerState = stateField.GetValue(m_GameViewSimulator);
        if (controllerState == null)
            return false;

        FieldInfo positionField = controllerState.GetType().GetField("devicePosition");
        if (positionField == null || positionField.FieldType != typeof(Vector3))
            return false;

        Vector3 position = (Vector3)positionField.GetValue(controllerState);
        position.y += m_GameViewControllerStartHeightOffset;
        positionField.SetValue(controllerState, position);
        stateField.SetValue(m_GameViewSimulator, controllerState);
        return true;
    }

    private void DisableRayVisualsForGameViewSimulator()
    {
        m_RayVisualStates.Clear();

        foreach (CurveVisualController curve in FindObjectsByType<CurveVisualController>(FindObjectsInactive.Exclude))
        {
            LineRenderer line = curve.lineRenderer;
            m_RayVisualStates.Add((curve, curve.enabled, line, line != null && line.enabled));
            curve.enabled = false;
            if (line != null)
                line.enabled = false;
        }
    }

    private void HideHandVisualsForGameViewSimulator()
    {
        m_HandRendererStates.Clear();
        if (m_GameViewHandVisualRoots == null || m_GameViewHandVisualRoots.Length == 0)
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} requires scene-authored Game View hand visual roots.",
                this);
            return;
        }

        HashSet<Renderer> captured = new();
        foreach (Transform handRoot in m_GameViewHandVisualRoots)
        {
            if (handRoot == null)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} has a missing Game View hand visual root reference.",
                    this);
                continue;
            }

            foreach (Renderer renderer in handRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !captured.Add(renderer))
                    continue;

                m_HandRendererStates.Add((renderer, renderer.forceRenderingOff));
                renderer.forceRenderingOff = true;
            }
        }
    }

    private void ConfigureGameViewMouseUi()
    {
        if (m_XrUiInputModule == null || m_MousePointAction == null || m_MouseLeftClickAction == null)
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} requires XRUIInputModule, UI Point, and UI Click references for Game View mouse input.",
                this);
            return;
        }

        if (m_MousePointAction.action == null || m_MouseLeftClickAction.action == null)
        {
            Debug.LogError(
                $"{nameof(PhysicalHmdSimulatorGate)} has invalid Game View mouse action references.",
                this);
            return;
        }

        m_PreviousPointAction = m_XrUiInputModule.pointAction;
        m_PreviousLeftClickAction = m_XrUiInputModule.leftClickAction;
        m_PreviousEnableMouseInput = m_XrUiInputModule.enableMouseInput;

        m_XrUiInputModule.pointAction = m_MousePointAction;
        m_XrUiInputModule.leftClickAction = m_MouseLeftClickAction;
        m_XrUiInputModule.enableMouseInput = true;
        m_GameViewMouseUiConfigured = true;
    }

    private void RestoreGameViewMouseUi()
    {
        if (!m_GameViewMouseUiConfigured || m_XrUiInputModule == null)
            return;

        m_XrUiInputModule.pointAction = m_PreviousPointAction;
        m_XrUiInputModule.leftClickAction = m_PreviousLeftClickAction;
        m_XrUiInputModule.enableMouseInput = m_PreviousEnableMouseInput;
        m_GameViewMouseUiConfigured = false;
    }

    private bool IsPointerOverInteractiveUi(Vector2 mousePosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData pointer = new(eventSystem)
        {
            position = mousePosition,
        };

        m_UiRaycastResults.Clear();
        eventSystem.RaycastAll(pointer, m_UiRaycastResults);

        foreach (RaycastResult result in m_UiRaycastResults)
        {
            GameObject target = result.gameObject;
            if (target == null)
                continue;

            // Some authored world-space UI (notably the inactive spatial
            // keyboard keys) keeps raycastTarget enabled while its Graphic is
            // fully transparent. It must not hide a visible PPE marker from
            // the Game View mouse ray.
            UnityEngine.UI.Graphic graphic = target.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null &&
                (!graphic.enabled || !graphic.raycastTarget ||
                 graphic.color.a <= 0f || graphic.canvasRenderer.GetAlpha() <= 0f))
            {
                continue;
            }

            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IPointerDownHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IDragHandler>(target) != null)
                return true;
        }

        return false;
    }

    private bool TryToggleMouseGrab(Collider hitCollider, Vector2 mousePosition)
    {
        XRGrabInteractable grab = hitCollider.GetComponentInParent<XRGrabInteractable>();
        if (grab == null || !grab.isActiveAndEnabled)
            return false;

        if (m_GameViewMouseSelectInteractor == null)
        {
            if (!m_HasReportedMissingMouseSelectInteractor)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires the authored right-hand Near-Far Interactor for Game View PPE marker selection.",
                    this);
                m_HasReportedMissingMouseSelectInteractor = true;
            }

            return true;
        }

        if (m_GameViewMouseSelectInteractor.IsSelecting(grab))
        {
            EndMouseManualInteraction();
            return true;
        }

        EndMouseManualInteraction();

        PPEActionPanelController panel = grab.GetComponent<PPEActionPanelController>();
        PPETabletChecklistController tablet = grab.GetComponent<PPETabletChecklistController>();
        if (tablet != null &&
            !TryBeginTabletMouseGrabOverrides(grab, mousePosition))
        {
            return true;
        }

        if (tablet == null && panel != null && panel.ApproveUseByBodyProximity &&
            !TryBeginMouseGrabOverrides(grab, panel, mousePosition))
        {
            return true;
        }

        m_GameViewMouseSelectInteractor.StartManualInteraction((IXRSelectInteractable)grab);
        if (m_GameViewMouseSelectInteractor.IsSelecting(grab))
        {
            m_MouseSelectedGrab = grab;
            m_MouseSelectedPanel = panel;
        }
        else
        {
            RestoreMouseGrabOverrides(grab);
        }

        return true;
    }

    private bool TryBeginMouseGrabOverrides(
        XRGrabInteractable grab,
        PPEActionPanelController panel,
        Vector2 mousePosition)
    {
        if (m_GameViewMouseGrabAnchor == null)
        {
            if (!m_HasReportedMissingMouseGrabAnchor)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires the scene-authored Game View mouse grab anchor.",
                    this);
                m_HasReportedMissingMouseGrabAnchor = true;
            }

            return false;
        }

        PPEMarkerToggleGrab markerGrab = grab.GetComponent<PPEMarkerToggleGrab>();
        Transform marker = markerGrab != null ? markerGrab.InteractionMarker : null;
        if (marker == null)
        {
            Debug.LogError(
                $"Game View mouse grab requires the authored PPE interaction marker on '{grab.name}'.",
                grab);
            return false;
        }

        if (!TryPositionMouseGrabAnchor(panel, mousePosition))
            return false;

        m_GameViewMouseGrabAnchor.rotation = marker.rotation;
        m_PreviousMouseInteractorAttach = m_GameViewMouseSelectInteractor.attachTransform;
        m_PreviousMouseGrabAttach = grab.attachTransform;
        m_PreviousMouseGrabUseDynamicAttach = grab.useDynamicAttach;
        m_GameViewMouseSelectInteractor.attachTransform = m_GameViewMouseGrabAnchor;
        grab.attachTransform = marker;
        grab.useDynamicAttach = false;
        m_MouseGrabOverridesApplied = true;
        return true;
    }

    private bool TryBeginTabletMouseGrabOverrides(
        XRGrabInteractable grab,
        Vector2 mousePosition)
    {
        if (m_GameViewMouseGrabAnchor == null)
        {
            if (!m_HasReportedMissingMouseGrabAnchor)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires the scene-authored Game View mouse grab anchor.",
                    this);
                m_HasReportedMissingMouseGrabAnchor = true;
            }

            return false;
        }

        PPEMarkerToggleGrab markerGrab = grab.GetComponent<PPEMarkerToggleGrab>();
        Transform marker = markerGrab != null ? markerGrab.InteractionMarker : null;
        if (marker == null)
        {
            Debug.LogError(
                $"Game View tablet mouse grab requires the authored interaction marker on '{grab.name}'.",
                grab);
            return false;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            if (!m_HasReportedMissingMouseTeleportCamera)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires a MainCamera for Game View tablet mouse grab.",
                    this);
                m_HasReportedMissingMouseTeleportCamera = true;
            }

            return false;
        }

        Plane pointerPlane = new(camera.transform.forward, marker.position);
        if (!TryPositionMouseGrabAnchor(pointerPlane, mousePosition))
            return false;

        m_GameViewMouseGrabAnchor.rotation = marker.rotation;
        m_PreviousMouseInteractorAttach = m_GameViewMouseSelectInteractor.attachTransform;
        m_PreviousMouseGrabAttach = grab.attachTransform;
        m_PreviousMouseGrabUseDynamicAttach = grab.useDynamicAttach;
        m_GameViewMouseSelectInteractor.attachTransform = m_GameViewMouseGrabAnchor;
        grab.attachTransform = marker;
        grab.useDynamicAttach = false;
        m_MousePointerDragPlane = pointerPlane;
        m_MousePointerDragPlaneActive = true;
        m_MouseGrabOverridesApplied = true;
        return true;
    }

    private void UpdateMouseGrabAnchor(Vector2 mousePosition)
    {
        if (m_MouseSelectedGrab == null ||
            !m_MouseGrabOverridesApplied)
        {
            return;
        }

        if (m_MouseSelectedPanel != null)
        {
            TryPositionMouseGrabAnchor(m_MouseSelectedPanel, mousePosition);
            return;
        }

        if (m_MousePointerDragPlaneActive)
            TryPositionMouseGrabAnchor(m_MousePointerDragPlane, mousePosition);
    }

    private bool TryPositionMouseGrabAnchor(
        PPEActionPanelController panel,
        Vector2 mousePosition)
    {
        if (m_GameViewMouseGrabAnchor == null ||
            panel == null ||
            !panel.TryGetGameViewBodyAttachTarget(out Vector3 bodyTarget))
        {
            return false;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            if (!m_HasReportedMissingMouseTeleportCamera)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires a MainCamera for Game View mouse grab.",
                    this);
                m_HasReportedMissingMouseTeleportCamera = true;
            }

            return false;
        }

        Plane bodyPlane = new(camera.transform.forward, bodyTarget);
        return TryPositionMouseGrabAnchor(bodyPlane, mousePosition);
    }

    private bool TryPositionMouseGrabAnchor(
        Plane pointerPlane,
        Vector2 mousePosition)
    {
        if (m_GameViewMouseGrabAnchor == null)
            return false;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray pointerRay = camera.ScreenPointToRay(mousePosition);
        if (!pointerPlane.Raycast(pointerRay, out float distance) || distance < 0f)
            return false;

        m_GameViewMouseGrabAnchor.position = pointerRay.GetPoint(distance);
        return true;
    }

    private bool TryActivateMouseHeldBodyProximityPpe()
    {
        if (m_MouseSelectedGrab == null ||
            m_MouseSelectedPanel == null ||
            !m_MouseSelectedPanel.ApproveUseByBodyProximity)
        {
            return false;
        }

        bool resolved = m_MouseSelectedPanel.TryActivateBodyProximityFromGameView(
            m_GameViewMouseSelectInteractor);
        if (resolved && m_MouseSelectedPanel.LastResult == PPEActionResult.UseApproved)
            EndMouseManualInteraction();

        // Keep the selected PPE latched when the pointer is outside the body
        // range or when existing condition/scenario rules reject the use.
        return true;
    }

    private void SynchronizeMouseManualInteraction()
    {
        if (m_MouseSelectedGrab == null)
            return;

        if (m_GameViewMouseSelectInteractor != null &&
            m_GameViewMouseSelectInteractor.IsSelecting(m_MouseSelectedGrab))
        {
            return;
        }

        XRGrabInteractable previousGrab = m_MouseSelectedGrab;
        m_MouseSelectedGrab = null;
        m_MouseSelectedPanel = null;
        RestoreMouseGrabOverrides(previousGrab);
    }

    private void TrySkipVoiceFromGameViewBackgroundClick()
    {
        if (AudioManager.Instance == null ||
            (!AudioManager.Instance.IsVoicePlaying && !AudioManager.Instance.IsVoiceLoading))
        {
            return;
        }

        if (m_GameViewVoiceFlowDirector == null)
        {
            if (!m_HasReportedMissingGameViewVoiceFlow)
            {
                Debug.LogError(
                    $"{nameof(PhysicalHmdSimulatorGate)} requires the scene-authored Game View voice flow reference.",
                    this);
                m_HasReportedMissingGameViewVoiceFlow = true;
            }

            return;
        }

        m_GameViewVoiceFlowDirector.TrySkipVoiceFromGameViewBackgroundClick();
    }

    private void EndMouseManualInteraction()
    {
        XRGrabInteractable selectedGrab = m_MouseSelectedGrab;
        if (selectedGrab == null || m_GameViewMouseSelectInteractor == null)
        {
            m_MouseSelectedGrab = null;
            m_MouseSelectedPanel = null;
            RestoreMouseGrabOverrides(selectedGrab);
            return;
        }

        if (m_GameViewMouseSelectInteractor.IsSelecting(selectedGrab))
            m_GameViewMouseSelectInteractor.EndManualInteraction();

        m_MouseSelectedGrab = null;
        m_MouseSelectedPanel = null;
        RestoreMouseGrabOverrides(selectedGrab);
    }

    private void RestoreMouseGrabOverrides(XRGrabInteractable grab)
    {
        m_MousePointerDragPlaneActive = false;

        if (!m_MouseGrabOverridesApplied)
            return;

        if (m_GameViewMouseSelectInteractor != null)
            m_GameViewMouseSelectInteractor.attachTransform = m_PreviousMouseInteractorAttach;

        if (grab != null)
        {
            grab.attachTransform = m_PreviousMouseGrabAttach;
            grab.useDynamicAttach = m_PreviousMouseGrabUseDynamicAttach;
        }

        m_PreviousMouseInteractorAttach = null;
        m_PreviousMouseGrabAttach = null;
        m_PreviousMouseGrabUseDynamicAttach = false;
        m_MouseGrabOverridesApplied = false;
    }

    private void RestoreRayVisuals()
    {
        foreach ((CurveVisualController curve, bool curveEnabled, LineRenderer line, bool lineEnabled) in m_RayVisualStates)
        {
            if (curve != null)
                curve.enabled = curveEnabled;
            if (line != null)
                line.enabled = lineEnabled;
        }

        m_RayVisualStates.Clear();
    }

    private void RestoreHandVisuals()
    {
        foreach ((Renderer renderer, bool forceRenderingOff) in m_HandRendererStates)
        {
            if (renderer != null)
                renderer.forceRenderingOff = forceRenderingOff;
        }

        m_HandRendererStates.Clear();
    }
}
