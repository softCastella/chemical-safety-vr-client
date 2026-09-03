using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public sealed class TitleSplashController : MonoBehaviour
{
    private const float MaximumFadeDeltaPerFrame = 1f / 30f;

    [SerializeField] private GameObject primaryLogo;
    [SerializeField] private GameObject partnerLogos;

    [Header("Primary Logo (logo2d)")]
    [SerializeField, Min(1)] private int prewarmFrames = 3;
    [SerializeField, Min(0f)] private float initialDelay = 1f;
    [SerializeField, Min(0f)] private float primaryFadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float versionDelay = 0.25f;
    [SerializeField, Min(0f)] private float partnerLogoDelay = 0.3f;

    [Header("Version (Top Right)")]
    [SerializeField, Min(0f)] private float versionFadeInDuration = 0.5f;
    [SerializeField] private string versionPrefix = "v";
    [SerializeField, Min(0.1f)] private float versionFontSize = 3f;
    [SerializeField] private Vector2 versionOffset = new(-2f, -2f);
    [SerializeField] private Vector2 versionSize = new(36f, 8f);
    [SerializeField] private Color versionTextColor = new(0.24528301f, 0.24528301f, 0.24528301f, 1f);
    [SerializeField, Range(0f, 1f)] private float versionOpacity = 0.85f;

    [Header("Partner Logos (Logos)")]
    [SerializeField, Min(0f)] private float partnerFadeInDuration = 0.6f;

    [Header("Scene Transition")]
    [SerializeField, Min(0f)] private float visibleDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.6f;
    [SerializeField] private string nextSceneName = "2_Intro";
    [SerializeField] private bool preloadNextScene;

    [Header("Title Audio")]
    [SerializeField] private bool playTitleBgmOnStart = true;
    [SerializeField] private string titleBgmId = "title";
    [SerializeField, Min(0f)] private float titleBgmFadeInDuration = 1f;

    private CanvasGroup primaryGroup;
    private CanvasGroup versionGroup;
    private CanvasGroup partnerGroup;
    private AsyncOperation nextSceneLoad;

    private void Awake()
    {
        primaryGroup = GetOrAddCanvasGroup(primaryLogo);
        versionGroup = EnsureVersionLabel();
        partnerGroup = GetOrAddCanvasGroup(partnerLogos);

        SetAlpha(primaryGroup, 0f);
        SetAlpha(versionGroup, 0f);
        SetAlpha(partnerGroup, 0f);
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying)
            yield break;

        if (playTitleBgmOnStart)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("TitleSplashController: AudioManager is missing when the title scene starts.", this);
            }
            else if (string.IsNullOrWhiteSpace(titleBgmId))
            {
                Debug.LogError("TitleSplashController: Title BGM ID is empty.", this);
            }
            else
            {
                AudioManager.Instance.PlayBgm(titleBgmId, titleBgmFadeInDuration);
            }
        }

        if (preloadNextScene && Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            nextSceneLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            if (nextSceneLoad == null)
            {
                Debug.LogError(
                    $"TitleSplashController: Could not begin preloading scene '{nextSceneName}'.",
                    this);
            }
            else
            {
                nextSceneLoad.allowSceneActivation = false;
                nextSceneLoad.priority = -1;
            }
        }

        Canvas.ForceUpdateCanvases();
        for (int frame = 0; frame < prewarmFrames; frame++)
            yield return new WaitForEndOfFrame();

        yield return Wait(initialDelay);
        yield return Fade(primaryGroup, 0f, 1f, primaryFadeInDuration);
        yield return Wait(versionDelay);
        yield return Fade(versionGroup, 0f, 1f, versionFadeInDuration);
        yield return Wait(partnerLogoDelay);

        yield return Fade(partnerGroup, 0f, 1f, partnerFadeInDuration);
        yield return Wait(visibleDuration);
        if (AudioManager.Instance != null)
            AudioManager.Instance.FadeOutBgm(fadeOutDuration);
        yield return FadeOutLogos();
        AudioManager.Instance?.StopAndReleasePersistentPlayback();

        if (nextSceneLoad != null)
        {
            nextSceneLoad.allowSceneActivation = true;
            yield return nextSceneLoad;
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"TitleSplashController: Build Settings에 '{nextSceneName}' 씬이 없습니다.", this);
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
    }

    private IEnumerator FadeOutLogos()
    {
        float elapsed = 0f;
        if (fadeOutDuration <= 0f)
        {
            SetAlpha(primaryGroup, 0f);
            SetAlpha(versionGroup, 0f);
            SetAlpha(partnerGroup, 0f);
            yield break;
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            SetAlpha(primaryGroup, alpha);
            SetAlpha(versionGroup, alpha);
            SetAlpha(partnerGroup, alpha);
            yield return null;
        }

        SetAlpha(primaryGroup, 0f);
        SetAlpha(versionGroup, 0f);
        SetAlpha(partnerGroup, 0f);
    }

    private CanvasGroup EnsureVersionLabel()
    {
        Transform existing = transform.Find("Version");
        bool isNew = existing == null;
        GameObject versionObject = isNew
            ? new GameObject("Version", typeof(RectTransform))
            : existing.gameObject;
        versionObject.layer = gameObject.layer;

        RectTransform rect = versionObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("Version UI object requires a RectTransform.", versionObject);
            return null;
        }
        if (isNew)
        {
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = versionOffset;
            rect.sizeDelta = versionSize;
        }

        TextMeshProUGUI text = versionObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = versionObject.AddComponent<TextMeshProUGUI>();
        text.text = versionPrefix + Application.version;
        text.fontSize = versionFontSize;
        text.font = TMP_Settings.defaultFontAsset;
        text.color = versionTextColor;
        text.alpha = versionOpacity;
        text.alignment = TextAlignmentOptions.TopRight;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        CanvasGroup group = versionObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = versionObject.AddComponent<CanvasGroup>();

#if UNITY_EDITOR
        if (isNew)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(versionObject, "Create version label");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
        return group;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = from;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // XR 초기화나 셰이더 준비로 첫 프레임이 길어져도 페이드가
            // 한 프레임 만에 끝나지 않도록 진행량을 제한한다.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            group.alpha = Mathf.Lerp(from, to, easedProgress);
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
}
