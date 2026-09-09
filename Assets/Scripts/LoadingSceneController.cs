using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LoadingSceneController : MonoBehaviour
{
    const string LoadingSceneName = "3_Loading";

    static string requestedSceneName;

    [Header("Scene Loading")]
    [SerializeField] string nextSceneName = "4_PPE_Room";
    [SerializeField, Min(0)] int prewarmFrames = 2;

    [Header("Progress Timing")]
    [SerializeField, Min(0f)] float minimumDisplayDuration = 1f;
    [SerializeField, Min(0.1f)]
    [Tooltip("로딩 바가 0%에서 100%까지 채워지는 최소 시간(초)입니다. 값을 늘리면 더 천천히 진행됩니다.")]
    float progressFillDuration = 4f;
    [SerializeField, Min(0f)] float completionHoldDuration = 0.15f;

    [Header("Authored UI References")]
    [SerializeField] CanvasGroup loadingContentGroup;
    [SerializeField] TMP_Text percentageText;
    [SerializeField] SegmentedGradientProgressGraphic progressBar;

    void Awake()
    {
        if (loadingContentGroup == null)
            loadingContentGroup = GetComponent<CanvasGroup>();

        SetContentVisible(prewarmFrames <= 0);
    }

    /// <summary>
    /// Routes a scene change through 3_Loading. The serialized nextSceneName is
    /// used when 3_Loading is opened directly.
    /// </summary>
    public static void LoadTarget(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("LoadingSceneController requires a non-empty target scene name.");
            return;
        }

        if (targetSceneName == LoadingSceneName
            || !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"LoadingSceneController cannot load target scene '{targetSceneName}'.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogError($"LoadingSceneController cannot find '{LoadingSceneName}' in Build Settings.");
            return;
        }

        requestedSceneName = targetSceneName;
        AsyncOperation operation = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            requestedSceneName = null;
            Debug.LogError($"LoadingSceneController failed to begin loading '{LoadingSceneName}'.");
        }
    }

    IEnumerator Start()
    {
        UpdateVisual(0f);
        Canvas.ForceUpdateCanvases();
        for (int frame = 0; frame < prewarmFrames; frame++)
            yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        SetContentVisible(true);

        string targetSceneName = string.IsNullOrWhiteSpace(requestedSceneName)
            ? nextSceneName
            : requestedSceneName;
        requestedSceneName = null;

        if (string.IsNullOrWhiteSpace(targetSceneName)
            || targetSceneName == LoadingSceneName
            || !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError($"LoadingSceneController cannot load target scene '{targetSceneName}'.", this);
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"LoadingSceneController failed to begin loading '{targetSceneName}'.", this);
            yield break;
        }

        operation.allowSceneActivation = false;
        float startedAt = Time.unscaledTime;
        float displayedProgress = 0f;

        while (true)
        {
            bool contentReady = operation.progress >= 0.9f;
            float normalizedProgress = contentReady
                ? 1f
                : Mathf.Clamp01(operation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                normalizedProgress,
                Time.unscaledDeltaTime / Mathf.Max(0.1f, progressFillDuration));
            UpdateVisual(displayedProgress);

            bool minimumTimeElapsed = Time.unscaledTime - startedAt >= minimumDisplayDuration;
            if (contentReady
                && minimumTimeElapsed
                && displayedProgress >= SegmentedGradientProgressGraphic.CompleteProgressThreshold)
                break;

            yield return null;
        }

        UpdateVisual(1f);
        if (completionHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(completionHoldDuration);

        operation.allowSceneActivation = true;
    }

    void SetContentVisible(bool visible)
    {
        if (loadingContentGroup != null)
            loadingContentGroup.alpha = visible ? 1f : 0f;
    }

    void UpdateVisual(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);
        int percent = normalizedProgress >= SegmentedGradientProgressGraphic.CompleteProgressThreshold
            ? 100
            : Mathf.FloorToInt(normalizedProgress * 100f);

        if (percentageText != null)
            percentageText.text = percent.ToString(CultureInfo.InvariantCulture) + "%";

        if (progressBar != null)
            progressBar.progress = normalizedProgress;
    }
}
