using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

/// <summary>
/// Adds Inspector-authored input sensitivity and acceleration/deceleration to the
/// Starter Assets dynamic move provider used by the PPE locomotion trial.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEConfigurableDynamicMoveProvider : DynamicMoveProvider
{
    [Header("PPE Locomotion Tuning")]
    [SerializeField, Min(0f)]
    [Tooltip("Thumbstick input multiplier. Values above 1 reach full speed with less stick travel.")]
    float m_InputSensitivity = 1f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly movement reaches the requested input. Zero keeps the original immediate response.")]
    float m_Acceleration = 8f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly movement stops after the thumbstick is released. Zero keeps the original immediate response.")]
    float m_Deceleration = 12f;

    [Header("Game View Navigation")]
    [Tooltip("Scene-authored Game View simulator gate. Physical headset input is unchanged when this gate is inactive.")]
    [SerializeField]
    PhysicalHmdSimulatorGate m_GameViewSimulatorGate;

    [SerializeField, Min(1f)]
    [Tooltip("Game View simulator only: multiplier applied after the authored Quest move speed.")]
    float m_GameViewSpeedMultiplier = 2.4f;

    [Header("Footstep SFX")]
    [SerializeField]
    string m_FootstepSfxId = "Foot Step";

    [SerializeField]
    [Tooltip("Dedicated scene-authored source used only for the constant-speed footstep loop.")]
    AudioSource m_FootstepSource;

    Vector2 m_SmoothedInput;
    bool m_LoggedMissingFootstepManager;
    bool m_LoggedMissingFootstepSource;
    bool m_LoggedMissingGameViewForwardSource;
    GameViewArrowMoveReader m_GameViewArrowMoveReader;
    IXRInputValueReader<Vector2> m_PreviousLeftHandMoveBypass;
    bool m_HasGameViewArrowMoveOverride;
    Transform m_PreviousHeadTransform;
    bool m_HasGameViewHeadTransformOverride;

    protected override void OnEnable()
    {
        m_SmoothedInput = Vector2.zero;
        StopFootsteps();
        base.OnEnable();

        if (m_GameViewSimulatorGate == null)
        {
            Debug.LogError(
                $"{nameof(PPEConfigurableDynamicMoveProvider)} on '{name}' requires the scene-authored Game View simulator gate.",
                this);
            return;
        }

        m_GameViewSimulatorGate.SimulatorModeChanged += OnGameViewSimulatorModeChanged;
        UpdateGameViewArrowMoveOverride();
        UpdateGameViewHeadTransformOverride();
    }

    protected override void OnDisable()
    {
        if (m_GameViewSimulatorGate != null)
            m_GameViewSimulatorGate.SimulatorModeChanged -= OnGameViewSimulatorModeChanged;

        RestoreGameViewArrowMoveOverride();
        RestoreGameViewHeadTransformOverride();

        m_SmoothedInput = Vector2.zero;
        StopFootsteps();
        base.OnDisable();
    }

    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        UpdateGameViewArrowMoveOverride();
        UpdateGameViewHeadTransformOverride();

        Vector2 targetInput = Vector2.ClampMagnitude(input * Mathf.Max(0f, m_InputSensitivity), 1f);
        Vector2 leftInput = leftHandMoveInput.ReadValue();
        Vector2 rightInput = rightHandMoveInput.ReadValue();
        bool hasLocomotionInput = leftInput != Vector2.zero || rightInput != Vector2.zero;
        UpdateFootsteps(hasLocomotionInput);

        bool accelerating = targetInput.sqrMagnitude > m_SmoothedInput.sqrMagnitude;
        float response = accelerating ? m_Acceleration : m_Deceleration;

        if (response <= 0f)
            m_SmoothedInput = targetInput;
        else
            m_SmoothedInput = Vector2.MoveTowards(
                m_SmoothedInput,
                targetInput,
                response * Time.deltaTime);

        Vector3 desiredMove = base.ComputeDesiredMove(m_SmoothedInput);
        if (m_GameViewSimulatorGate != null && m_GameViewSimulatorGate.IsGameViewSimulatorActive)
            desiredMove *= Mathf.Max(1f, m_GameViewSpeedMultiplier);

        return desiredMove;
    }

    void OnGameViewSimulatorModeChanged(bool isActive)
    {
        UpdateGameViewArrowMoveOverride();
        UpdateGameViewHeadTransformOverride();
    }

    void UpdateGameViewArrowMoveOverride()
    {
        bool shouldUseGameViewInput = m_GameViewSimulatorGate != null &&
            m_GameViewSimulatorGate.IsGameViewSimulatorActive;
        if (!shouldUseGameViewInput)
        {
            RestoreGameViewArrowMoveOverride();
            return;
        }

        if (m_HasGameViewArrowMoveOverride)
            return;

        m_PreviousLeftHandMoveBypass = leftHandMoveInput.bypass;
        m_GameViewArrowMoveReader = new GameViewArrowMoveReader(
            leftHandMoveInput,
            m_PreviousLeftHandMoveBypass,
            m_GameViewSimulatorGate);
        leftHandMoveInput.bypass = m_GameViewArrowMoveReader;
        m_HasGameViewArrowMoveOverride = true;
    }

    void RestoreGameViewArrowMoveOverride()
    {
        if (!m_HasGameViewArrowMoveOverride)
            return;

        if (leftHandMoveInput.bypass == m_GameViewArrowMoveReader)
            leftHandMoveInput.bypass = m_PreviousLeftHandMoveBypass;

        m_PreviousLeftHandMoveBypass = null;
        m_GameViewArrowMoveReader = null;
        m_HasGameViewArrowMoveOverride = false;
    }

    void UpdateGameViewHeadTransformOverride()
    {
        bool shouldUseGameViewSource = m_GameViewSimulatorGate != null &&
            m_GameViewSimulatorGate.IsGameViewSimulatorActive;
        if (!shouldUseGameViewSource)
        {
            RestoreGameViewHeadTransformOverride();
            return;
        }

        if (m_HasGameViewHeadTransformOverride)
            return;

        Transform gameViewForwardSource = m_GameViewSimulatorGate.GameViewForwardSource;
        if (gameViewForwardSource == null)
        {
            if (!m_LoggedMissingGameViewForwardSource)
            {
                Debug.LogError(
                    $"{nameof(PPEConfigurableDynamicMoveProvider)} on '{name}' requires the authored XR Origin Camera for Game View movement.",
                    this);
                m_LoggedMissingGameViewForwardSource = true;
            }

            return;
        }

        m_PreviousHeadTransform = headTransform;
        m_HasGameViewHeadTransformOverride = true;
        headTransform = gameViewForwardSource;
    }

    void RestoreGameViewHeadTransformOverride()
    {
        if (!m_HasGameViewHeadTransformOverride)
            return;

        headTransform = m_PreviousHeadTransform;
        m_PreviousHeadTransform = null;
        m_HasGameViewHeadTransformOverride = false;
    }

    void UpdateFootsteps(bool hasLocomotionInput)
    {
        if (!hasLocomotionInput || string.IsNullOrWhiteSpace(m_FootstepSfxId))
        {
            StopFootsteps();
            return;
        }

        if (m_FootstepSource == null)
        {
            if (!m_LoggedMissingFootstepSource)
            {
                Debug.LogError(
                    "PPEConfigurableDynamicMoveProvider requires a dedicated scene-authored Footstep AudioSource.",
                    this);
                m_LoggedMissingFootstepSource = true;
            }

            return;
        }

        if (AudioManager.Instance == null)
        {
            if (!m_LoggedMissingFootstepManager)
            {
                Debug.LogError(
                    "PPEConfigurableDynamicMoveProvider requires a scene AudioManager to play authored footstep SFX.",
                    this);
                m_LoggedMissingFootstepManager = true;
            }

            return;
        }

        AudioManager.Instance.PlayLoopingSfx(m_FootstepSfxId, m_FootstepSource);
    }

    void StopFootsteps()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopLoopingSfx(m_FootstepSource);
        else if (m_FootstepSource != null)
        {
            m_FootstepSource.Stop();
            m_FootstepSource.loop = false;
            m_FootstepSource.clip = null;
        }
    }

    sealed class GameViewArrowMoveReader : IXRInputValueReader<Vector2>
    {
        readonly XRInputValueReader<Vector2> m_Original;
        readonly IXRInputValueReader<Vector2> m_PreviousBypass;
        readonly PhysicalHmdSimulatorGate m_Gate;

        public GameViewArrowMoveReader(
            XRInputValueReader<Vector2> original,
            IXRInputValueReader<Vector2> previousBypass,
            PhysicalHmdSimulatorGate gate)
        {
            m_Original = original;
            m_PreviousBypass = previousBypass;
            m_Gate = gate;
        }

        public Vector2 ReadValue()
        {
            Vector2 rawValue = m_PreviousBypass != null
                ? m_PreviousBypass.ReadValue()
                : m_Original.ReadValue();
            if (m_Gate == null || !m_Gate.IsGameViewSimulatorActive)
                return rawValue;

            Vector2 arrowValue = ReadArrowInput();
            return arrowValue != Vector2.zero ? arrowValue : rawValue;
        }

        public bool TryReadValue(out Vector2 value)
        {
            bool hasRawValue = m_PreviousBypass != null
                ? m_PreviousBypass.TryReadValue(out Vector2 rawValue)
                : m_Original.TryReadValue(out rawValue);
            if (m_Gate == null || !m_Gate.IsGameViewSimulatorActive)
            {
                value = rawValue;
                return hasRawValue;
            }

            Vector2 arrowValue = ReadArrowInput();
            if (arrowValue != Vector2.zero)
            {
                value = arrowValue;
                return true;
            }

            value = rawValue;
            return hasRawValue;
        }

        static Vector2 ReadArrowInput()
        {
            if (Keyboard.current == null)
                return Vector2.zero;

            float forward = 0f;
            if (Keyboard.current.upArrowKey.isPressed)
                forward += 1f;
            if (Keyboard.current.downArrowKey.isPressed)
                forward -= 1f;
            return new Vector2(0f, Mathf.Clamp(forward, -1f, 1f));
        }
    }
}
