using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LoadingLogoSpin : MonoBehaviour
{
    [Header("Authored References")]
    [SerializeField] RectTransform logo;
    [SerializeField] CanvasGroup loadingContentGroup;

    [Header("Spin Timing")]
    [SerializeField, Min(0f)] float delayAfterVisible = 0.2f;
    [SerializeField, Min(0.05f)] float spinDuration = 0.55f;
    [SerializeField, Min(0f)] float pauseBetweenSpins = 2f;
    [SerializeField, Range(1f, 90f)] float edgeAngle = 90f;
    [SerializeField] bool reverseDirection;
    [SerializeField, Min(0.001f)] float maxAnimationFrameStep = 1f / 30f;
    [SerializeField] AnimationCurve spinProgress = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    Quaternion authoredLocalRotation;
    bool hasAuthoredRotation;

    void Awake()
    {
        if (logo == null || loadingContentGroup == null)
        {
            Debug.LogError(
                "LoadingLogoSpin requires authored Logo and Loading Content Group references.",
                this);
            enabled = false;
            return;
        }

        authoredLocalRotation = logo.localRotation;
        hasAuthoredRotation = true;
    }

    IEnumerator Start()
    {
        while (loadingContentGroup.alpha <= 0.001f)
            yield return null;

        yield return WaitUnscaled(delayAfterVisible);

        while (isActiveAndEnabled)
        {
            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, maxAnimationFrameStep);
                float normalizedTime = Mathf.Clamp01(elapsed / spinDuration);
                float progress = spinProgress.Evaluate(normalizedTime);
                float angle = progress < 0.5f
                    ? Mathf.Lerp(0f, edgeAngle, progress * 2f)
                    : Mathf.Lerp(-edgeAngle, 0f, (progress - 0.5f) * 2f);
                if (reverseDirection)
                    angle = -angle;
                logo.localRotation = authoredLocalRotation * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            logo.localRotation = authoredLocalRotation;
            if (!isActiveAndEnabled)
                yield break;

            yield return WaitUnscaled(pauseBetweenSpins);
        }
    }

    void OnDisable()
    {
        if (hasAuthoredRotation && logo != null)
            logo.localRotation = authoredLocalRotation;
    }

    static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
