using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AppStartupSynchronizationHarness
{
    private const string AppScenePath = "Assets/Scenes/0_App.unity";
    private const string TitleScenePath = "Assets/Scenes/1_Title.unity";

    [MenuItem("Tools/XR/Validate App Startup Synchronization")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before validating app startup synchronization.");

        Scene appScene = default;
        Scene titleScene = default;
        bool closeAppScene = false;
        bool closeTitleScene = false;

        try
        {
            appScene = OpenForValidation(AppScenePath, out closeAppScene);
            titleScene = OpenForValidation(TitleScenePath, out closeTitleScene);

            ValidateBuildOrder();
            ValidateAppScene(appScene);
            ValidateTitleScene(titleScene);

            Debug.Log(
                "[App Startup Synchronization] PASS: builds gate title loading on physical XR readiness, " +
                "Editor Game View may bypass that gate, Meta SDK Editor testing is opt-in, " +
                "0_App does not auto-play BGM, 1_Title owns title BGM start, " +
                "and 2_Intro is preloaded before the title fade completes.");
        }
        finally
        {
            if (closeTitleScene && titleScene.IsValid() && titleScene.isLoaded)
                EditorSceneManager.CloseScene(titleScene, true);
            if (closeAppScene && appScene.IsValid() && appScene.isLoaded)
                EditorSceneManager.CloseScene(appScene, true);
        }
    }

    private static Scene OpenForValidation(string path, out bool shouldClose)
    {
        Scene loaded = SceneManager.GetSceneByPath(path);
        if (loaded.IsValid() && loaded.isLoaded)
        {
            shouldClose = false;
            return loaded;
        }

        shouldClose = true;
        return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    private static void ValidateBuildOrder()
    {
        EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .ToArray();
        Require(enabledScenes.Length >= 2, "Build Settings must contain at least the app and title scenes.");
        Require(enabledScenes[0].path == AppScenePath, $"First enabled scene must be {AppScenePath}.");
        Require(enabledScenes[1].path == TitleScenePath, $"Second enabled scene must be {TitleScenePath}.");
    }

    private static void ValidateAppScene(Scene scene)
    {
        AppSceneBootstrap bootstrap = FindSingle<AppSceneBootstrap>(scene);
        SerializedObject serializedBootstrap = new(bootstrap);
        Require(serializedBootstrap.FindProperty("waitForXrDisplayBeforeTitle").boolValue,
            "AppSceneBootstrap must wait for the physical XR display.");
        Require(!serializedBootstrap.FindProperty("waitForPhysicalXrDisplayInEditor").boolValue,
            "Editor Game View must bypass physical HMD readiness unless explicitly enabled.");
        Require(serializedBootstrap.FindProperty("xrWarmupRenderFrames").intValue >= 2,
            "AppSceneBootstrap must wait for at least two XR render callbacks.");
        Require(serializedBootstrap.FindProperty("xrReadinessWarningSeconds").floatValue > 0f,
            "AppSceneBootstrap must report prolonged XR readiness waits.");

        AudioManager audioManager = FindSingle<AudioManager>(scene);
        SerializedObject serializedAudio = new(audioManager);
        Require(!serializedAudio.FindProperty("m_PlayBgmOnStart").boolValue,
            "0_App must not start BGM before HMD readiness.");
        Require(serializedAudio.FindProperty("m_PersistAcrossSceneLoads").boolValue,
            "0_App AudioManager must persist into 1_Title.");
        Require(serializedAudio.FindProperty("m_StartupBgmId").stringValue == "title",
            "0_App title BGM library ID must remain 'title'.");
        SerializedProperty bgmLibrary = serializedAudio.FindProperty("m_Bgm");
        SerializedProperty titleBgm = FindBgm(bgmLibrary, "title");
        Require(titleBgm != null, "0_App must contain the 'title' BGM library entry.");
        AudioClip titleClip = titleBgm.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
        Require(titleClip != null, "0_App title BGM must reference an AudioClip.");
        Require(
            AssetDatabase.GetAssetPath(titleClip) ==
                "Assets/Audio/BGM/XR Horizon Interface (Remastered).mp3",
            "0_App title BGM must reference XR Horizon Interface (Remastered).mp3.");

        MetaPlatformIdentityProbe identityProbe = FindSingle<MetaPlatformIdentityProbe>(scene);
        SerializedObject serializedIdentity = new(identityProbe);
        Require(!serializedIdentity.FindProperty("useMetaPlatformSdkInEditor").boolValue,
            "Meta Platform SDK use must remain opt-in for Editor Game View tests.");
        Require(!serializedIdentity.FindProperty("logAppScopedUserId").boolValue,
            "Release app scene must not log the raw Meta app-scoped user ID.");

        TycheTrainingTelemetryUploader uploader = FindSingle<TycheTrainingTelemetryUploader>(scene);
        SerializedObject serializedUploader = new(uploader);
        string productionServerBaseUrl = serializedUploader
            .FindProperty("productionServerBaseUrl").stringValue;
        Require(TycheMetaSessionAuthenticator.TryNormalizeProductionServerBaseUrl(
                productionServerBaseUrl,
                out _,
                out _),
            "Release telemetry endpoint must be an authored public HTTPS root URL.");
    }

    private static void ValidateTitleScene(Scene scene)
    {
        TitleSplashController title = FindSingle<TitleSplashController>(scene);
        SerializedObject serializedTitle = new(title);
        Require(serializedTitle.FindProperty("playTitleBgmOnStart").boolValue,
            "1_Title must start title BGM after its gated activation.");
        Require(serializedTitle.FindProperty("titleBgmId").stringValue == "title",
            "1_Title title BGM ID must be 'title'.");
        Require(serializedTitle.FindProperty("titleBgmFadeInDuration").floatValue == 1f,
            "1_Title title BGM fade-in duration must remain 1 second.");
        Require(serializedTitle.FindProperty("preloadNextScene").boolValue,
            "1_Title must preload 2_Intro before completing the title transition.");
    }

    private static SerializedProperty FindBgm(SerializedProperty library, string id)
    {
        for (int index = 0; index < library.arraySize; index++)
        {
            SerializedProperty entry = library.GetArrayElementAtIndex(index);
            if (entry.FindPropertyRelative("id").stringValue == id)
                return entry;
        }

        return null;
    }

    private static T FindSingle<T>(Scene scene) where T : Component
    {
        T[] components = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
        Require(components.Length == 1,
            $"Expected one {typeof(T).Name} in {scene.path}, found {components.Length}.");
        return components[0];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"[App Startup Synchronization] {message}");
    }
}
