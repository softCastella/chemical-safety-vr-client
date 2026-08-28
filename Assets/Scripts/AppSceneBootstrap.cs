using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public sealed class AppSceneBootstrap : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "1_Title";
    [SerializeField, Min(0f)] private float startupDelay;

    [Header("Physical HMD Readiness")]
    [SerializeField] private bool waitForXrDisplayBeforeTitle = true;
    [Tooltip("Editor Game View에서도 물리 HMD 렌더 준비를 기다려야 할 때만 켭니다.")]
    [SerializeField] private bool waitForPhysicalXrDisplayInEditor;
    [SerializeField, Min(1)] private int xrWarmupRenderFrames = 2;
    [SerializeField, Min(0f)] private float xrReadinessWarningSeconds = 15f;

    private readonly List<XRDisplaySubsystem> xrDisplays = new();
    private int readyXrRenderFrames;
    private int lastCountedRenderFrame = -1;
    private bool beforeRenderSubscribed;

    private IEnumerator Start()
    {
        // Give Unity one rendered frame to finish application-level startup
        // before the XR title scene begins loading.
        yield return null;

        if (startupDelay > 0f)
            yield return new WaitForSecondsRealtime(startupDelay);

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError($"AppSceneBootstrap: Build Settings does not contain scene '{titleSceneName}'.", this);
            yield break;
        }

        AsyncOperation titleLoad = SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Single);
        if (titleLoad == null)
        {
            Debug.LogError($"AppSceneBootstrap: Could not begin loading scene '{titleSceneName}'.", this);
            yield break;
        }

        // Keep the application scene active while the title content is loaded in the background.
        // This prevents the Game View/title flow from advancing before the physical XR display can render it.
        titleLoad.allowSceneActivation = false;
        while (titleLoad.progress < 0.9f)
            yield return null;

        if (ShouldWaitForPhysicalXrDisplay())
            yield return WaitForPhysicalXrRenderReadiness();

        titleLoad.allowSceneActivation = true;
    }

    private bool ShouldWaitForPhysicalXrDisplay()
    {
        return waitForXrDisplayBeforeTitle &&
            (!Application.isEditor || waitForPhysicalXrDisplayInEditor);
    }

    private IEnumerator WaitForPhysicalXrRenderReadiness()
    {
        readyXrRenderFrames = 0;
        lastCountedRenderFrame = -1;
        float warningDeadline = Time.realtimeSinceStartup + xrReadinessWarningSeconds;
        bool warningLogged = false;

        Application.onBeforeRender += CountReadyXrRenderFrame;
        beforeRenderSubscribed = true;

        while (readyXrRenderFrames < xrWarmupRenderFrames)
        {
            if (!warningLogged && xrReadinessWarningSeconds > 0f &&
                Time.realtimeSinceStartup >= warningDeadline)
            {
                warningLogged = true;
                Debug.LogError(
                    "AppSceneBootstrap: The physical XR display did not become render-ready before the warning deadline. " +
                    "The title scene and title audio remain blocked until the HMD is ready.",
                    this);
            }

            yield return null;
        }

        UnsubscribeBeforeRender();
    }

    private void CountReadyXrRenderFrame()
    {
        if (Time.frameCount == lastCountedRenderFrame || !HasRunningPhysicalXrDisplay())
            return;

        lastCountedRenderFrame = Time.frameCount;
        readyXrRenderFrames++;
    }

    private bool HasRunningPhysicalXrDisplay()
    {
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!headDevice.isValid)
            return false;

        xrDisplays.Clear();
        SubsystemManager.GetSubsystems(xrDisplays);
        foreach (XRDisplaySubsystem display in xrDisplays)
        {
            if (display != null && display.running)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        UnsubscribeBeforeRender();
    }

    private void UnsubscribeBeforeRender()
    {
        if (!beforeRenderSubscribed)
            return;

        Application.onBeforeRender -= CountReadyXrRenderFrame;
        beforeRenderSubscribed = false;
    }
}
