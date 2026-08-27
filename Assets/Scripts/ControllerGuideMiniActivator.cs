using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles the authored mini controller guide when either controller's thumbstick is clicked.
/// Attach this to an always-active scene object, such as Window Canvas.
/// </summary>
[DisallowMultipleComponent]
public sealed class ControllerGuideMiniActivator : MonoBehaviour
{
    [Header("Authored Scene Reference")]
    [Tooltip("Assign Window Canvas/ControllerGuide_mini. This component only activates it; it never changes its authored layout or visibility at startup.")]
    [SerializeField] private GameObject m_ControllerGuideMini;

    [Tooltip("Assign the authored XRI Default Input Actions asset. The guide uses its registered left/right Scale Toggle actions for headset thumbstick clicks.")]
    [SerializeField] private InputActionAsset m_XriInputActions;

    private InputAction m_LeftThumbstickClick;
    private InputAction m_RightThumbstickClick;
    private InputAction m_SimulatorThumbstickClick;
    private bool m_HasReportedMissingReference;
    private int m_LastToggleFrame = -1;

    private void Awake()
    {
        if (m_XriInputActions != null)
        {
            m_LeftThumbstickClick = m_XriInputActions.FindAction(
                "XRI Left Interaction/Scale Toggle",
                throwIfNotFound: false);
            m_RightThumbstickClick = m_XriInputActions.FindAction(
                "XRI Right Interaction/Scale Toggle",
                throwIfNotFound: false);
        }

        if (m_XriInputActions == null
            || m_LeftThumbstickClick == null
            || m_RightThumbstickClick == null)
        {
            Debug.LogError(
                $"{nameof(ControllerGuideMiniActivator)} on '{name}' requires the authored XRI Default Input Actions asset with left/right Scale Toggle actions.",
                this);
        }

        // The Game View simulator panel emits a virtual Gamepad input. Keep this
        // separate from the real XR bindings so headset input remains unchanged.
        m_SimulatorThumbstickClick = new InputAction(
            "Show Controller Guide Mini Simulator",
            InputActionType.Button,
            "<Gamepad>/leftStickPress");
    }

    private void OnEnable()
    {
        SetHeadsetActionCallbacks(true);
        m_SimulatorThumbstickClick.performed += OnThumbstickClicked;
        m_SimulatorThumbstickClick?.Enable();
    }

    private void OnDisable()
    {
        SetHeadsetActionCallbacks(false);
        m_SimulatorThumbstickClick.performed -= OnThumbstickClicked;
        m_SimulatorThumbstickClick?.Disable();
    }

    private void OnDestroy()
    {
        DisposeAction(ref m_SimulatorThumbstickClick);
    }

    private void OnThumbstickClicked(InputAction.CallbackContext context)
    {
        if (m_ControllerGuideMini == null)
        {
            if (!m_HasReportedMissingReference)
            {
                Debug.LogError(
                    $"{nameof(ControllerGuideMiniActivator)} on '{name}' requires the authored ControllerGuide_mini reference.",
                    this);
                m_HasReportedMissingReference = true;
            }

            return;
        }

        // Quest Link can report the same physical stick click through both the
        // generic XRController and OculusTouchController bindings. Treat those
        // same-frame callbacks as one authored toggle; otherwise the mini guide
        // is enabled and immediately disabled, which looks like an ignored click.
        if (m_LastToggleFrame == Time.frameCount)
            return;

        m_LastToggleFrame = Time.frameCount;
        m_ControllerGuideMini.SetActive(!m_ControllerGuideMini.activeSelf);
    }

    private void SetHeadsetActionCallbacks(bool add)
    {
        SetActionCallback(m_LeftThumbstickClick, add);
        SetActionCallback(m_RightThumbstickClick, add);
    }

    private void SetActionCallback(InputAction action, bool add)
    {
        if (action == null)
            return;

        if (add)
            action.performed += OnThumbstickClicked;
        else
            action.performed -= OnThumbstickClicked;
    }

    private void DisposeAction(ref InputAction action)
    {
        if (action == null)
            return;

        action.performed -= OnThumbstickClicked;
        action.Dispose();
        action = null;
    }
}
