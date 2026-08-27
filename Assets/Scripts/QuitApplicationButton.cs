using UnityEngine;
using UnityEngine.UI;
using Prototype.Tyche.UI.Keyboard;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public sealed class QuitApplicationButton : MonoBehaviour
{
    private Button quitButton;

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

#if UNITY_EDITOR
        if (Application.isPlaying)
            EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
