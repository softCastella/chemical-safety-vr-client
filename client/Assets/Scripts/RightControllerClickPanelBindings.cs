using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// Connects the Game View StickClick button to the XRI Device Simulator's right controller.
/// The OnScreen control creates a virtual Gamepad at runtime.
/// </summary>
[DefaultExecutionOrder(-31000)]
[DisallowMultipleComponent]
public sealed class RightControllerClickPanelBindings : MonoBehaviour
{
    [Tooltip("The XR Device Simulator on this same test object. Assigned by the explicit scene setup command.")]
    [SerializeField] private XRDeviceSimulator m_Simulator;

    private bool m_Configured;
    private int m_MouseTriggerBindingIndex = -1;
    private int m_MouseDeltaBindingIndex = -1;
    private int m_MouseHeadBindingIndex = -1;
    private CursorLockMode m_PreviousCursorLockMode;
    private bool m_PreviousCursorVisible;

    private void Awake()
    {
        if (m_Configured)
            return;

        if (m_Simulator == null)
        {
            Debug.LogError($"{nameof(RightControllerClickPanelBindings)} on '{name}' requires an XRDeviceSimulator reference.", this);
            return;
        }

        const string stickClickPath = "<Gamepad>/leftStickPress";
        AddBinding(m_Simulator.toggleManipulateRightAction, stickClickPath, "Toggle Right Controller");
        AddBinding(m_Simulator.primary2DAxisClickAction, stickClickPath, "Primary 2D Axis Click");

        m_Configured = true;
    }

    private void OnEnable()
    {
        if (m_Simulator == null)
            return;

        m_PreviousCursorLockMode = Cursor.lockState;
        m_PreviousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DisablePhysicalMouseTriggerBinding();
        DisablePhysicalMousePoseBindings();
    }

    private void OnDisable()
    {
        RestorePhysicalMouseTriggerBinding();
        RestorePhysicalMousePoseBindings();
        Cursor.lockState = m_PreviousCursorLockMode;
        Cursor.visible = m_PreviousCursorVisible;
    }

    private void AddBinding(InputActionReference actionReference, string controlPath, string controlLabel)
    {
        InputAction action = actionReference != null ? actionReference.action : null;
        if (action == null)
        {
            Debug.LogError(
                $"{nameof(RightControllerClickPanelBindings)} could not bind '{controlLabel}' because its XR Device Simulator action reference is missing.",
                this);
            return;
        }

        if (HasBinding(action, controlPath))
            return;

        action.AddBinding(controlPath);
    }

    private static bool HasBinding(InputAction action, string controlPath)
    {
        for (int index = 0; index < action.bindings.Count; index++)
        {
            if (action.bindings[index].path == controlPath)
                return true;
        }

        return false;
    }

    private void DisablePhysicalMouseTriggerBinding()
    {
        InputAction triggerAction = m_Simulator.triggerAction != null ? m_Simulator.triggerAction.action : null;
        if (triggerAction == null || m_MouseTriggerBindingIndex >= 0)
            return;

        m_MouseTriggerBindingIndex = DisableBinding(triggerAction, "<Mouse>/leftButton");
    }

    private void RestorePhysicalMouseTriggerBinding()
    {
        InputAction triggerAction = m_Simulator != null && m_Simulator.triggerAction != null
            ? m_Simulator.triggerAction.action
            : null;
        if (triggerAction != null && m_MouseTriggerBindingIndex >= 0)
            triggerAction.RemoveBindingOverride(m_MouseTriggerBindingIndex);

        m_MouseTriggerBindingIndex = -1;
    }

    private void DisablePhysicalMousePoseBindings()
    {
        InputAction mouseDeltaAction = m_Simulator.mouseDeltaAction != null ? m_Simulator.mouseDeltaAction.action : null;
        if (mouseDeltaAction != null && m_MouseDeltaBindingIndex < 0)
            m_MouseDeltaBindingIndex = DisableBinding(mouseDeltaAction, "<Mouse>/delta");

        InputAction manipulateHeadAction = m_Simulator.manipulateHeadAction != null ? m_Simulator.manipulateHeadAction.action : null;
        if (manipulateHeadAction != null && m_MouseHeadBindingIndex < 0)
            m_MouseHeadBindingIndex = DisableBinding(manipulateHeadAction, "<Mouse>/rightButton");
    }

    private void RestorePhysicalMousePoseBindings()
    {
        InputAction mouseDeltaAction = m_Simulator != null && m_Simulator.mouseDeltaAction != null
            ? m_Simulator.mouseDeltaAction.action
            : null;
        RestoreBinding(mouseDeltaAction, ref m_MouseDeltaBindingIndex);

        InputAction manipulateHeadAction = m_Simulator != null && m_Simulator.manipulateHeadAction != null
            ? m_Simulator.manipulateHeadAction.action
            : null;
        RestoreBinding(manipulateHeadAction, ref m_MouseHeadBindingIndex);
    }

    private static int DisableBinding(InputAction action, string controlPath)
    {
        for (int index = 0; index < action.bindings.Count; index++)
        {
            if (action.bindings[index].effectivePath != controlPath)
                continue;

            action.ApplyBindingOverride(index, string.Empty);
            return index;
        }

        return -1;
    }

    private static void RestoreBinding(InputAction action, ref int bindingIndex)
    {
        if (action != null && bindingIndex >= 0)
            action.RemoveBindingOverride(bindingIndex);

        bindingIndex = -1;
    }
}
