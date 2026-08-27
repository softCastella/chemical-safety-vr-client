#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Static gate for the HandTest keyboard submit path. This intentionally does not enter Play Mode;
/// runtime Enter/quit ordering still requires a logged Unity/XR reproduction.
/// </summary>
public static class HangulKeyboardSubmitSafetyHarness
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string ControllerPath = "Assets/Scripts/HangulKeyboard/HangulKeyboardController.cs";
    const string QuitPath = "Assets/Scripts/QuitApplicationButton.cs";

    [MenuItem("Tools/XR/Hangul Keyboard/Validate Submit Safety")]
    public static void Validate()
    {
        var sceneText = File.ReadAllText(ScenePath);
        var controllerText = File.ReadAllText(ControllerPath);
        var quitText = File.ReadAllText(QuitPath);

        Require(sceneText.Contains("m_Name: Modal  Keyboard Canvas"), "Modal keyboard canvas is missing.");
        Require(sceneText.Contains("m_SubmitOnEnter: 1"), "XRKeyboard submitOnEnter is not enabled.");
        Require(sceneText.Contains("m_CloseOnSubmit: 1"), "XRKeyboard closeOnSubmit is not enabled.");
        Require(sceneText.Contains("m_CanvasToDisableOnSubmit: {fileID: 2103816839}"),
            "Submit target is not the authored Modal Keyboard Canvas root.");
        Require(sceneText.Contains("m_DisableCanvasOnSubmit: 1"),
            "Submit-to-modal-disable is not enabled in the scene.");
        Require(controllerText.Contains("m_CanvasToDisableOnSubmit.SetActive(false)"),
            "Controller does not disable the serialized submit target.");
        Require(controllerText.Contains("string.IsNullOrWhiteSpace(submittedText)"),
            "Controller does not reject empty name submit.");
        Require(controllerText.Contains("PreventCloseAfterRejectedSubmit()"),
            "Empty name submit would still close the XRI keyboard.");
        Require(quitText.Contains("EditorApplication.isPlaying = false"),
            "QuitApplicationButton direct Play Mode exit path changed; inspect before modifying input.");

        Debug.Log("[Hangul Keyboard] Submit safety static validation passed. Runtime XR ordering remains unverified.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
#endif
