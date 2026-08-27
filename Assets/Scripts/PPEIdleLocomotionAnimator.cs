using UnityEngine;

/// <summary>
/// Drives the idle body Animator Speed from actual XR Origin movement.
/// Walk plays only while stick locomotion is enabled and the origin is moving.
/// Teleport jumps are ignored so the body does not walk in place.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEIdleLocomotionAnimator : MonoBehaviour
{
    const string DefaultSpeedParameter = "Speed";

    [SerializeField] Animator m_Animator;
    [SerializeField] Transform m_MovementSource;
    [SerializeField] Behaviour m_MoveProvider;
    [SerializeField] string m_SpeedParameter = DefaultSpeedParameter;
    [SerializeField, Min(0f)] float m_WalkSpeedThreshold = 0.2f;
    [SerializeField, Min(0f)] float m_TeleportDeltaIgnore = 0.75f;
    [SerializeField, Min(0f)] float m_SpeedDampTime = 0.12f;

    Vector3 m_LastPosition;
    bool m_HasLastPosition;
    int m_SpeedParameterHash;
    bool m_HasSpeedParameter;

    void Awake()
    {
        CacheParameter();
        CapturePosition();
    }

    void OnEnable()
    {
        CapturePosition();
        SetSpeedImmediate(0f);
    }

    void Update()
    {
        if (m_MoveProvider != null && !m_MoveProvider.isActiveAndEnabled)
        {
            CapturePosition();
            SetSpeed(0f);
            return;
        }

        Vector3 position = ReadPosition();
        if (!m_HasLastPosition)
        {
            m_LastPosition = position;
            m_HasLastPosition = true;
            SetSpeed(0f);
            return;
        }

        Vector3 delta = position - m_LastPosition;
        delta.y = 0f;
        m_LastPosition = position;

        if (delta.magnitude >= m_TeleportDeltaIgnore)
        {
            SetSpeed(0f);
            return;
        }

        float planarSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        bool walking = planarSpeed >= m_WalkSpeedThreshold;
        SetSpeed(walking ? 1f : 0f);
    }

    void CacheParameter()
    {
        m_SpeedParameterHash = Animator.StringToHash(
            string.IsNullOrEmpty(m_SpeedParameter) ? DefaultSpeedParameter : m_SpeedParameter);
        m_HasSpeedParameter = m_Animator != null && HasFloatParameter(m_Animator, m_SpeedParameterHash);
    }

    void CapturePosition()
    {
        m_LastPosition = ReadPosition();
        m_HasLastPosition = m_MovementSource != null;
    }

    Vector3 ReadPosition()
    {
        return m_MovementSource != null ? m_MovementSource.position : transform.position;
    }

    void SetSpeed(float speed)
    {
        if (m_Animator == null || !m_Animator.isActiveAndEnabled || !m_HasSpeedParameter)
            return;

        m_Animator.SetFloat(m_SpeedParameterHash, speed, m_SpeedDampTime, Time.deltaTime);
    }

    void SetSpeedImmediate(float speed)
    {
        if (m_Animator == null || !m_HasSpeedParameter)
            return;

        m_Animator.SetFloat(m_SpeedParameterHash, speed);
    }

    static bool HasFloatParameter(Animator animator, int hash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == hash)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Animator animator,
        Transform movementSource,
        Behaviour moveProvider)
    {
        m_Animator = animator;
        m_MovementSource = movementSource;
        m_MoveProvider = moveProvider;
        if (string.IsNullOrEmpty(m_SpeedParameter))
            m_SpeedParameter = DefaultSpeedParameter;
    }
#endif
}
