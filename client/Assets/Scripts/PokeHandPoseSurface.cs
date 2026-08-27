using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the controller-hand Poke parameter when an authored index fingertip reaches this surface.
/// This is intentionally buttonless: the hand pose follows physical proximity to the collider.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("XR/Hand Pose/Poke Hand Pose Surface")]
public sealed class PokeHandPoseSurface : MonoBehaviour
{
    [SerializeField] Collider m_SurfaceCollider;
    [SerializeField, Min(0f)] float m_EnterDistance = 0.02f;
    [SerializeField, Min(0f)] float m_ExitDistance = 0.035f;

    readonly HashSet<HandGripAnimator> m_TouchingHands = new();
    readonly List<HandGripAnimator> m_NoLongerActive = new();
    bool m_LoggedMissingHands;

    void Reset()
    {
        m_SurfaceCollider = GetComponentInChildren<Collider>(true);
    }

    void Awake()
    {
        EnsureSurfaceCollider();
    }

    void LateUpdate()
    {
        if (m_SurfaceCollider == null || !m_SurfaceCollider.enabled)
            return;

        if (HandGripAnimator.ActiveInstances.Count == 0)
        {
            if (!m_LoggedMissingHands)
            {
                Debug.LogWarning(
                    "[HandPose] Poke_Box found no enabled HandGripAnimator components.", this);
                m_LoggedMissingHands = true;
            }
            return;
        }

        m_LoggedMissingHands = false;

        m_NoLongerActive.Clear();
        foreach (var touchingHand in m_TouchingHands)
        {
            if (!HandGripAnimator.IsActive(touchingHand))
                m_NoLongerActive.Add(touchingHand);
        }

        foreach (var inactiveHand in m_NoLongerActive)
        {
            inactiveHand.SetPokeActive(this, false);
            m_TouchingHands.Remove(inactiveHand);
        }

        foreach (var hand in HandGripAnimator.ActiveInstances)
        {
            var fingertip = hand.IndexTip;
            var touching = fingertip != null && IsWithinContactDistance(
                fingertip.position,
                m_TouchingHands.Contains(hand) ? m_ExitDistance : m_EnterDistance);

            hand.SetPokeActive(this, touching);
            if (touching)
                m_TouchingHands.Add(hand);
            else
                m_TouchingHands.Remove(hand);
        }
    }

    void OnDisable()
    {
        foreach (var hand in m_TouchingHands)
            hand.SetPokeActive(this, false);
        m_TouchingHands.Clear();
    }

    bool IsWithinContactDistance(Vector3 worldPoint, float distance)
    {
        var closestPoint = m_SurfaceCollider.ClosestPoint(worldPoint);
        return (closestPoint - worldPoint).sqrMagnitude <= distance * distance;
    }

    void EnsureSurfaceCollider()
    {
        if (m_SurfaceCollider == null)
            m_SurfaceCollider = GetComponentInChildren<Collider>(true);
    }
}
