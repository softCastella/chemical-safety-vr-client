using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Prototype.Tyche.UI.Keyboard;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public sealed class QuitApplicationButton : MonoBehaviour
{
    const float TelemetryFlushTimeoutSeconds = 5f;

    private Button quitButton;
    private bool quitRequested;

    private void Awake()
    {
        quitButton = GetComponent<Button>();
        quitButton.onClick.AddListener(HandleQuit);
    }

    private void OnDestroy()
    {
        if (quitButton != null)
            quitButton.onClick.RemoveListener(HandleQuit);
    }

    private void HandleQuit()
    {
        Debug.Log("[Quit Button] HandleQuit invoked", this);

        if (HangulKeyboardSubmitGuard.isSuppressed)
        {
            Debug.Log("[Quit Button] Ignored because keyboard Enter submission is closing its modal.", this);
            return;
        }

        if (quitRequested)
            return;

        quitRequested = true;
        quitButton.interactable = false;
        string sessionId = PPETrainingTelemetryCapture.RecordApplicationExitRequested();

#if UNITY_EDITOR
        if (Application.isPlaying)
            EditorApplication.isPlaying = false;
#else
        StartCoroutine(QuitAfterTelemetryFlush(sessionId));
#endif
    }

#if !UNITY_EDITOR
    private IEnumerator QuitAfterTelemetryFlush(string sessionId)
    {
        bool uploaded = false;
        yield return TycheTrainingTelemetryUploader.FlushSessionAndWaitForCompletion(
            sessionId,
            TelemetryFlushTimeoutSeconds,
            success => uploaded = success);

        Debug.Log(
            uploaded
                ? "[Quit Button] Telemetry session completed on the server before quitting."
                : "[Quit Button] Telemetry flush timed out; durable local recovery remains pending.",
            this);
        Application.Quit();
    }
#endif
}
