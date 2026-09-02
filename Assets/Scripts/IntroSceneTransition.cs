using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class IntroSceneTransition : MonoBehaviour
{
    [SerializeField, Min(0f)] private float visibleDuration = 3f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.8f;
    [SerializeField] private string nextSceneName = "4_PPE_Room";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    private IEnumerator Start()
    {
        if (visibleDuration > 0f)
            yield return new WaitForSecondsRealtime(visibleDuration);

        if (fadeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"IntroSceneTransition: Build Settings에 '{nextSceneName}' 씬이 없습니다.", this);
            yield break;
        }

        LoadingSceneController.LoadTarget(nextSceneName);
    }
}
