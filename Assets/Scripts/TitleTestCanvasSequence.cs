using System.Collections;
using UnityEngine;

public sealed class TitleTestCanvasSequence : MonoBehaviour
{
    private const float MaximumFadeDeltaPerFrame = 1f / 30f;

    [SerializeField] private GameObject splashCanvas;
    [SerializeField] private CanvasGroup splashTitleGroup;
    [SerializeField] private CanvasGroup splashPartnerGroup;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private CanvasGroup titleLogoGroup;
    [SerializeField] private RectTransform partnerLogos;
    [SerializeField] private CanvasGroup partnerLogosGroup;
    [SerializeField] private CanvasGroup rightPanelGroup;
    [SerializeField] private CanvasGroup versionGroup;
    [SerializeField] private Vector2 titleLogoStartPosition;
    [SerializeField] private Vector2 partnerLogosStartPosition;
    [SerializeField, Min(0)] private int splashPrewarmFrames = 3;
    [SerializeField, Min(0f)] private float splashInitialDelay = 1f;
    [SerializeField, Min(0f)] private float splashFadeInDuration = 1f;
    [SerializeField, Min(0f)] private float splashPartnerDelay = 0.6f;
    [SerializeField, Min(0f)] private float splashPartnerFadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float splashHoldDuration = 1.5f;
    [SerializeField, Min(0f)] private float splashFadeOutDuration = 1.5f;

    [Header("Main Canvas Reveal")]
    [SerializeField, Min(0f)] private float mainRevealDuration = 1.35f;
    [Tooltip("RightPanelArea가 로고 등장 후 페이드인하는 시간(초)")]
    [SerializeField, Min(0f)] private float mainRightPanelRevealDuration = 0.75f;

    private Vector2 titleLogoFinalPosition;
    private Vector2 partnerLogosFinalPosition;

    private void Awake()
    {
        if (titleLogo != null)
            titleLogoFinalPosition = titleLogo.anchoredPosition;
        if (partnerLogos != null)
            partnerLogosFinalPosition = partnerLogos.anchoredPosition;

        SetAlpha(splashTitleGroup, 0f);
        SetAlpha(splashPartnerGroup, 0f);
        SetAlpha(titleLogoGroup, 0f);
        SetAlpha(partnerLogosGroup, 0f);
        SetAlpha(rightPanelGroup, 0f);
        SetAlpha(versionGroup, 0f);
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying)
            yield break;

        Canvas.ForceUpdateCanvases();
        for (int frame = 0; frame < splashPrewarmFrames; frame++)
            yield return new WaitForEndOfFrame();

        yield return Wait(splashInitialDelay);
        yield return Fade(splashTitleGroup, 0f, 1f, splashFadeInDuration);
        yield return Wait(splashPartnerDelay);
        yield return Fade(splashPartnerGroup, 0f, 1f, splashPartnerFadeInDuration);
        yield return Wait(splashHoldDuration);
        yield return FadeOutSplash();

        if (splashCanvas != null)
            splashCanvas.SetActive(false);

        yield return RevealMainCanvas();
    }

    private IEnumerator RevealMainCanvas()
    {
        SetPosition(titleLogo, titleLogoStartPosition);
        SetPosition(partnerLogos, partnerLogosStartPosition);

        float elapsed = 0f;
        while (elapsed < mainRevealDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = mainRevealDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / mainRevealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetPosition(titleLogo, Vector2.Lerp(titleLogoStartPosition, titleLogoFinalPosition, easedProgress));
            SetPosition(partnerLogos, Vector2.Lerp(partnerLogosStartPosition, partnerLogosFinalPosition, easedProgress));
            SetAlpha(titleLogoGroup, easedProgress);
            SetAlpha(partnerLogosGroup, easedProgress);
            yield return null;
        }

        SetPosition(titleLogo, titleLogoFinalPosition);
        SetPosition(partnerLogos, partnerLogosFinalPosition);
        SetAlpha(titleLogoGroup, 1f);
        SetAlpha(partnerLogosGroup, 1f);
        yield return FadeMainRightPanel();
    }

    private IEnumerator FadeMainRightPanel()
    {
        float elapsed = 0f;
        while (elapsed < mainRightPanelRevealDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = mainRightPanelRevealDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / mainRightPanelRevealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetAlpha(rightPanelGroup, easedProgress);
            SetAlpha(versionGroup, easedProgress);
            yield return null;
        }

        SetAlpha(rightPanelGroup, 1f);
        SetAlpha(versionGroup, 1f);
    }

    private IEnumerator FadeOutSplash()
    {
        float elapsed = 0f;
        while (elapsed < splashFadeOutDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / splashFadeOutDuration));
            SetAlpha(splashTitleGroup, alpha);
            SetAlpha(splashPartnerGroup, alpha);
            yield return null;
        }

        SetAlpha(splashTitleGroup, 0f);
        SetAlpha(splashPartnerGroup, 0f);
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        group.alpha = to;
    }

    private static IEnumerator Wait(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    private static void SetPosition(RectTransform rectTransform, Vector2 position)
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = position;
    }
}