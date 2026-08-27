using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives a controller-held hand model between its open and gripped poses from an analog controller
/// axis. The action is built in code rather than referenced from an input asset so the component
/// works as soon as it is dropped onto a hand, with nothing to wire up in the inspector.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HandGripAnimator : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right,
    }

    /// <summary>Float parameters of the controller built by HandAnimatorBuilder.</summary>
    const string k_GripParameter = "Grip";
    const string k_PokeParameter = "Poke";

    /// <summary>
    /// Seconds to reach the target value. The raw axis jitters around its extremes, which reads as a
    /// twitching hand, so the value is always eased rather than left to the inspector.
    /// </summary>
    const float k_SmoothTime = 0.05f;

    static readonly HashSet<HandGripAnimator> s_ActiveInstances = new();

    [SerializeField] HandSide m_Side = HandSide.Left;

    Animator m_Animator;
    InputAction m_Action;
    int m_GripParameterHash;
    int m_PokeParameterHash;
    float m_GripValue;
    float m_GripVelocity;
    float m_PokeValue;
    float m_PokeVelocity;
    bool m_HasPokeParameter;
    Transform m_IndexTip;
    readonly HashSet<Object> m_GripSources = new();
    readonly HashSet<Object> m_PokeSources = new();

    /// <summary>Currently enabled controller-hand animators in the loaded scenes.</summary>
    public static IReadOnlyCollection<HandGripAnimator> ActiveInstances => s_ActiveInstances;

    /// <summary>Whether the given controller-hand animator is currently enabled.</summary>
    public static bool IsActive(HandGripAnimator hand) => s_ActiveInstances.Contains(hand);

    /// <summary>The controller side represented by this hand.</summary>
    public HandSide Side => m_Side;

    /// <summary>The authored index fingertip used as the buttonless poke contact probe.</summary>
    public Transform IndexTip
    {
        get
        {
            if (m_IndexTip == null)
                m_IndexTip = FindIndexTip();
            return m_IndexTip;
        }
    }

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_GripParameterHash = Animator.StringToHash(k_GripParameter);
        m_PokeParameterHash = Animator.StringToHash(k_PokeParameter);
        m_HasPokeParameter = HasFloatParameter(m_PokeParameterHash);
        m_IndexTip = FindIndexTip();
    }

    void OnEnable()
    {
        s_ActiveInstances.Add(this);

        var usage = m_Side == HandSide.Left ? "LeftHand" : "RightHand";

        m_Action = new InputAction(
            name: $"HandGrip_{usage}",
            type: InputActionType.Value,
            binding: $"<XRController>{{{usage}}}/grip",
            expectedControlType: "Axis");

        m_Action.Enable();
    }

    void Start()
    {
        if (!m_HasPokeParameter)
        {
            Debug.LogWarning(
                $"[HandPose] '{name}' Animator has no float parameter named '{k_PokeParameter}'.", this);
        }

        if (IndexTip == null)
        {
            var expectedName = m_Side == HandSide.Left ? "L_IndexTip" : "R_IndexTip";
            Debug.LogWarning(
                $"[HandPose] '{name}' could not find index-tip transform '{expectedName}'.", this);
        }
    }

    void OnDisable()
    {
        s_ActiveInstances.Remove(this);
        m_GripSources.Clear();
        m_GripValue = 0f;
        m_GripVelocity = 0f;
        m_PokeSources.Clear();
        m_PokeValue = 0f;
        m_PokeVelocity = 0f;
        if (m_Animator != null)
            m_Animator.SetFloat(m_GripParameterHash, 0f);
        if (m_Animator != null && m_HasPokeParameter)
            m_Animator.SetFloat(m_PokeParameterHash, 0f);

        m_Action?.Disable();
        m_Action?.Dispose();
        m_Action = null;
    }

    void Update()
    {
        if (m_Action != null)
        {
            var gripTarget = m_GripSources.Count > 0
                ? 1f
                : m_Action.ReadValue<float>();
            m_GripValue = Mathf.SmoothDamp(
                m_GripValue, gripTarget, ref m_GripVelocity, k_SmoothTime);
            m_Animator.SetFloat(m_GripParameterHash, m_GripValue);
        }

        if (!m_HasPokeParameter)
            return;

        var pokeTarget = m_PokeSources.Count > 0 ? 1f : 0f;
        m_PokeValue = Mathf.SmoothDamp(
            m_PokeValue, pokeTarget, ref m_PokeVelocity, k_SmoothTime);
        m_Animator.SetFloat(m_PokeParameterHash, m_PokeValue);
    }

    /// <summary>
    /// Keeps this hand visually gripped while a toggle-style selection remains active, even after the
    /// user releases the physical grip button. Sources are tracked independently so other held objects
    /// cannot clear the pose prematurely.
    /// </summary>
    public void SetGripActive(Object source, bool active)
    {
        if (source == null)
            return;

        if (active)
            m_GripSources.Add(source);
        else
            m_GripSources.Remove(source);
    }

    /// <summary>
    /// Registers or clears one poke surface. Tracking sources independently prevents leaving one surface
    /// from canceling the pose while the fingertip is still touching another.
    /// </summary>
    public void SetPokeActive(Object source, bool active)
    {
        if (source == null)
            return;

        if (active)
            m_PokeSources.Add(source);
        else
            m_PokeSources.Remove(source);
    }

    Transform FindIndexTip()
    {
        var expectedName = m_Side == HandSide.Left ? "L_IndexTip" : "R_IndexTip";
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == expectedName || child.name == "IndexTip")
                return child;
        }

        return null;
    }

    bool HasFloatParameter(int parameterHash)
    {
        if (m_Animator == null)
            return false;

        foreach (var parameter in m_Animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
                return true;
        }

        return false;
    }
}
