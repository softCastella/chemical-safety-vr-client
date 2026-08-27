using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneAudioManagerSetup
{
    private const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    private const string AppScenePath = "Assets/Scenes/0_App.unity";
    private const string TitleScenePath = "Assets/Scenes/1_Title.unity";
    private const string LegacySettingsPath = "Assets/Resources/Audio/AudioManagerSettings.asset";
    private const string TitleBgmPath = "Assets/Audio/BGM/XR-Horizon-Interface.ogg";
    private const string PpeRoomBgmPath = "Assets/Audio/BGM/Safe-Horizons-_VR-Training-Theme_.ogg";

    [MenuItem("Tools/Audio/Setup HandTest Scale 0 Scene Audio Manager")]
    public static void SetupHandTestScale0()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Exit Play Mode before configuring the scene AudioManager.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError($"Open '{TargetScenePath}' before configuring its scene AudioManager.");
            return;
        }

        AudioManager manager = GetOrCreateManager();

        AudioSource bgmSource = GetOrCreateSource(manager.transform, "BGM", true);
        AudioSource voiceSource = GetOrCreateSource(manager.transform, "Voice", false);
        AudioSource sfxSource = GetOrCreateSource(manager.transform, "SFX", false);
        AudioSource ambienceSource = GetOrCreateSource(manager.transform, "Ambience", true);

        ConfigureManager(manager, bgmSource, voiceSource, sfxSource, ambienceSource);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateHandTestScale0();
        Debug.Log(
            "Scene AudioManager setup completed. Review the Hierarchy and validation result before saving the scene.",
            manager);
    }

    [MenuItem("Tools/Audio/Validate HandTest Scale 0 Scene Audio Manager")]
    public static void ValidateHandTestScale0()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError($"Open '{TargetScenePath}' before validating its scene AudioManager.");
            return;
        }

        AudioManager[] managers = Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length != 1)
        {
            Debug.LogError($"Expected exactly one scene AudioManager, found {managers.Length}.");
            return;
        }

        SerializedObject serializedManager = new(managers[0]);
        bool validSources = serializedManager.FindProperty("m_BgmSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_VoiceSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_SfxSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue != null;
        bool startsBgm = serializedManager.FindProperty("m_PlayBgmOnStart").boolValue
            && !string.IsNullOrWhiteSpace(serializedManager.FindProperty("m_StartupBgmId").stringValue);

        if (!validSources || !startsBgm)
        {
            Debug.LogError(
                "Scene AudioManager validation failed. Check serialized source references and startup BGM.",
                managers[0]);
            return;
        }

        Debug.Log(
            "Scene AudioManager validation passed: one serialized manager owns BGM, Voice, SFX, and Ambience sources.",
            managers[0]);
    }

    [MenuItem("Tools/Audio/Setup App Scene Persistent Title BGM")]
    public static void SetupAppScenePersistentTitleBgm()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Exit Play Mode before configuring the scene AudioManager.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != AppScenePath)
        {
            Debug.LogError($"Open '{AppScenePath}' before configuring its scene AudioManager.");
            return;
        }

        AudioManager manager = GetOrCreateManager();
        AudioSource bgmSource = GetOrCreateSource(manager.transform, "BGM", true);
        AudioSource voiceSource = GetOrCreateSource(manager.transform, "Voice", false);
        AudioSource sfxSource = GetOrCreateSource(manager.transform, "SFX", false);
        AudioSource ambienceSource = GetOrCreateSource(manager.transform, "Ambience", true);

        ConfigureAppTitleBgmManager(manager, bgmSource, voiceSource, sfxSource, ambienceSource);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateAppScenePersistentTitleBgm();
        Debug.Log("App scene title BGM setup completed. Save the scene before testing.", manager);
    }

    [MenuItem("Tools/Audio/Validate App Scene Persistent Title BGM")]
    public static void ValidateAppScenePersistentTitleBgm()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != AppScenePath)
        {
            Debug.LogError($"Open '{AppScenePath}' before validating its AudioManager.");
            return;
        }

        AudioManager[] managers = Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length != 1)
        {
            Debug.LogError($"Expected exactly one scene AudioManager, found {managers.Length}.");
            return;
        }

        SerializedObject serializedManager = new(managers[0]);
        bool validSources = serializedManager.FindProperty("m_BgmSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_VoiceSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_SfxSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue != null;
        bool defersTitleBgm = !serializedManager.FindProperty("m_PlayBgmOnStart").boolValue
            && serializedManager.FindProperty("m_StartupBgmId").stringValue == "title";
        bool persistsToTitle = serializedManager.FindProperty("m_PersistAcrossSceneLoads").boolValue;

        if (!validSources || !defersTitleBgm || !persistsToTitle)
        {
            Debug.LogError(
                "App scene title BGM validation failed. Check serialized sources, deferred title BGM, and persistence.",
                managers[0]);
            return;
        }

        Debug.Log("App scene title BGM validation passed.", managers[0]);
    }

    [MenuItem("Tools/Audio/Setup Title Scene Audio Manager")]
    public static void SetupTitleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Exit Play Mode before configuring the scene AudioManager.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TitleScenePath)
        {
            Debug.LogError($"Open '{TitleScenePath}' before configuring its scene AudioManager.");
            return;
        }

        AudioManager manager = GetOrCreateManager();
        AudioSource bgmSource = GetOrCreateSource(manager.transform, "BGM", true);
        AudioSource voiceSource = GetOrCreateSource(manager.transform, "Voice", false);
        AudioSource sfxSource = GetOrCreateSource(manager.transform, "SFX", false);
        AudioSource ambienceSource = GetOrCreateSource(manager.transform, "Ambience", true);

        ConfigureTitleManager(manager, bgmSource, voiceSource, sfxSource, ambienceSource);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateTitleScene();
        Debug.Log(
            "Title scene AudioManager setup completed. Review the Hierarchy and save the scene.",
            manager);
    }

    public static void SetupAndSaveTitleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Exit Play Mode before configuring the scene AudioManager.");
            return;
        }

        Scene titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        SetupTitleScene();

        if (titleScene.isDirty)
            EditorSceneManager.SaveScene(titleScene);
    }

    [MenuItem("Tools/Audio/Validate Title Scene Audio Manager")]
    public static void ValidateTitleScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TitleScenePath)
        {
            Debug.LogError($"Open '{TitleScenePath}' before validating its scene AudioManager.");
            return;
        }

        AudioManager[] managers = Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length != 1)
        {
            Debug.LogError($"Expected exactly one scene AudioManager, found {managers.Length}.");
            return;
        }

        SerializedObject serializedManager = new(managers[0]);
        bool validSources = serializedManager.FindProperty("m_BgmSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_VoiceSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_SfxSource").objectReferenceValue != null
            && serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue != null;
        bool startsTitleBgm = serializedManager.FindProperty("m_PlayBgmOnStart").boolValue
            && serializedManager.FindProperty("m_StartupBgmId").stringValue == "title";

        if (!validSources || !startsTitleBgm)
        {
            Debug.LogError(
                "Title scene AudioManager validation failed. Check serialized source references and startup BGM.",
                managers[0]);
            return;
        }

        Debug.Log(
            "Title scene AudioManager validation passed: one serialized manager starts the title BGM.",
            managers[0]);
    }

    private static AudioManager GetOrCreateManager()
    {
        AudioManager[] managers = Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 1)
            throw new System.InvalidOperationException("The target scene already has more than one AudioManager.");

        if (managers.Length == 1)
            return managers[0];

        GameObject managerObject = new("AudioManager");
        return managerObject.AddComponent<AudioManager>();
    }

    private static AudioSource GetOrCreateSource(Transform parent, string sourceName, bool loop)
    {
        Transform child = parent.Find(sourceName);
        GameObject sourceObject = child != null ? child.gameObject : new GameObject(sourceName);
        if (child == null)
            sourceObject.transform.SetParent(parent, false);

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
            source = sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.mute = false;
        return source;
    }

    private static void ConfigureManager(
        AudioManager manager,
        AudioSource bgmSource,
        AudioSource voiceSource,
        AudioSource sfxSource,
        AudioSource ambienceSource)
    {
        SerializedObject serializedManager = new(manager);
        serializedManager.FindProperty("m_BgmSource").objectReferenceValue = bgmSource;
        serializedManager.FindProperty("m_VoiceSource").objectReferenceValue = voiceSource;
        serializedManager.FindProperty("m_SfxSource").objectReferenceValue = sfxSource;
        serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue = ambienceSource;

        SerializedProperty bgmLibrary = serializedManager.FindProperty("m_Bgm");
        bool isFirstBgmSetup = bgmLibrary.arraySize == 0;
        if (isFirstBgmSetup)
        {
            serializedManager.FindProperty("m_PlayBgmOnStart").boolValue = true;
            serializedManager.FindProperty("m_StartupBgmId").stringValue = "ppe_room";
            serializedManager.FindProperty("m_StartupBgmDelay").floatValue = 0.2f;
        }

        ConfigureBgmLibrary(bgmLibrary);
        ConfigureVoiceLibrary(serializedManager.FindProperty("m_Voice"));
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Sfx"), LoadLegacySettings()?.sfx);
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Ambience"), LoadLegacySettings()?.ambience);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureAppTitleBgmManager(
        AudioManager manager,
        AudioSource bgmSource,
        AudioSource voiceSource,
        AudioSource sfxSource,
        AudioSource ambienceSource)
    {
        SerializedObject serializedManager = new(manager);
        serializedManager.FindProperty("m_BgmSource").objectReferenceValue = bgmSource;
        serializedManager.FindProperty("m_VoiceSource").objectReferenceValue = voiceSource;
        serializedManager.FindProperty("m_SfxSource").objectReferenceValue = sfxSource;
        serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue = ambienceSource;
        serializedManager.FindProperty("m_PersistAcrossSceneLoads").boolValue = true;
        serializedManager.FindProperty("m_PlayBgmOnStart").boolValue = false;
        serializedManager.FindProperty("m_StartupBgmId").stringValue = "title";
        serializedManager.FindProperty("m_StartupBgmDelay").floatValue = 0f;

        SerializedProperty bgmLibrary = serializedManager.FindProperty("m_Bgm");
        ConfigureBgmLibrary(bgmLibrary);
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Sfx"), LoadLegacySettings()?.sfx);
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Ambience"), LoadLegacySettings()?.ambience);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTitleManager(
        AudioManager manager,
        AudioSource bgmSource,
        AudioSource voiceSource,
        AudioSource sfxSource,
        AudioSource ambienceSource)
    {
        SerializedObject serializedManager = new(manager);
        serializedManager.FindProperty("m_BgmSource").objectReferenceValue = bgmSource;
        serializedManager.FindProperty("m_VoiceSource").objectReferenceValue = voiceSource;
        serializedManager.FindProperty("m_SfxSource").objectReferenceValue = sfxSource;
        serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue = ambienceSource;

        SerializedProperty bgmLibrary = serializedManager.FindProperty("m_Bgm");
        if (bgmLibrary.arraySize == 0)
        {
            serializedManager.FindProperty("m_PlayBgmOnStart").boolValue = true;
            serializedManager.FindProperty("m_StartupBgmId").stringValue = "title";
            serializedManager.FindProperty("m_StartupBgmDelay").floatValue = 0.2f;
        }

        ConfigureBgmLibrary(bgmLibrary);
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Sfx"), LoadLegacySettings()?.sfx);
        ConfigureSoundLibrary(serializedManager.FindProperty("m_Ambience"), LoadLegacySettings()?.ambience);
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBgmLibrary(SerializedProperty library)
    {
        AudioClip title = AssetDatabase.LoadAssetAtPath<AudioClip>(TitleBgmPath);
        AudioClip ppeRoom = AssetDatabase.LoadAssetAtPath<AudioClip>(PpeRoomBgmPath);
        if (title == null || ppeRoom == null)
            throw new System.InvalidOperationException("Required BGM clips could not be loaded.");

        AddBgmIfMissing(library, "title", title, 1f, true);
        AddBgmIfMissing(library, "ppe_room", ppeRoom, 0.3f, true);
    }

    private static void ConfigureSoundLibrary(
        SerializedProperty library,
        List<AudioManagerSettings.Sound> sounds)
    {
        if (sounds == null)
            return;

        foreach (AudioManagerSettings.Sound sound in sounds)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.id) || ContainsId(library, sound.id))
                continue;

            int index = library.arraySize;
            library.arraySize++;
            SerializedProperty entry = library.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = sound.id;
            entry.FindPropertyRelative("clip").objectReferenceValue = sound.clip;
            entry.FindPropertyRelative("volume").floatValue = sound.volume;
            entry.FindPropertyRelative("enabled").boolValue = true;
        }
    }

    private static void ConfigureVoiceLibrary(SerializedProperty library)
    {
        PPEVoiceFlowDirector[] directors = Object.FindObjectsByType<PPEVoiceFlowDirector>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (directors.Length != 1)
        {
            Debug.LogWarning($"Expected one PPEVoiceFlowDirector while importing Voice clips, found {directors.Length}.");
            return;
        }

        SerializedObject serializedDirector = new(directors[0]);
        string[] stepGroupNames =
        {
            "m_VoiceSteps",
            "m_ControllerEduVoiceSteps",
            "m_ControllerSimpVoiceSteps",
        };
        foreach (string stepGroupName in stepGroupNames)
        {
            SerializedProperty steps = serializedDirector.FindProperty(stepGroupName);
            if (steps == null)
                continue;

            for (int stepIndex = 0; stepIndex < steps.arraySize; stepIndex++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(stepIndex);
                SerializedProperty clips = step.FindPropertyRelative("clips");
                for (int clipIndex = 0; clipIndex < clips.arraySize; clipIndex++)
                    AddSoundIfClipMissing(library, clips.GetArrayElementAtIndex(clipIndex).objectReferenceValue as AudioClip);

                AddSoundIfClipMissing(
                    library,
                    step.FindPropertyRelative("repeatClip").objectReferenceValue as AudioClip);
            }
        }

        foreach (PPEActionPanelController panel in Object.FindObjectsByType<PPEActionPanelController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            SerializedObject serializedPanel = new(panel);
            AddSoundIfClipMissing(
                library,
                serializedPanel.FindProperty("contaminatedUseRejectedVoice")?.objectReferenceValue as AudioClip);
            AddSoundIfClipMissing(
                library,
                serializedPanel.FindProperty("cleanDiscardRejectedVoice")?.objectReferenceValue as AudioClip);
        }
    }

    private static void AddSoundIfClipMissing(SerializedProperty library, AudioClip clip)
    {
        if (clip == null || ContainsClip(library, clip))
            return;

        int index = library.arraySize;
        library.arraySize++;
        SerializedProperty entry = library.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("id").stringValue = clip.name;
        entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        entry.FindPropertyRelative("volume").floatValue = 1f;
        entry.FindPropertyRelative("enabled").boolValue = true;
    }

    private static void AddBgmIfMissing(
        SerializedProperty library,
        string id,
        AudioClip clip,
        float volume,
        bool enabled)
    {
        if (ContainsId(library, id))
            return;

        int index = library.arraySize;
        library.arraySize++;
        SetBgm(library.GetArrayElementAtIndex(index), id, clip, volume, enabled);
    }

    private static bool ContainsId(SerializedProperty library, string id)
    {
        for (int index = 0; index < library.arraySize; index++)
        {
            if (library.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue == id)
                return true;
        }

        return false;
    }

    private static bool ContainsClip(SerializedProperty library, AudioClip clip)
    {
        for (int index = 0; index < library.arraySize; index++)
        {
            if (library.GetArrayElementAtIndex(index).FindPropertyRelative("clip").objectReferenceValue == clip)
                return true;
        }

        return false;
    }

    private static void SetBgm(
        SerializedProperty entry,
        string id,
        AudioClip clip,
        float volume,
        bool enabled)
    {
        entry.FindPropertyRelative("id").stringValue = id;
        entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        entry.FindPropertyRelative("volume").floatValue = volume;
        entry.FindPropertyRelative("enabled").boolValue = enabled;
    }

    private static AudioManagerSettings LoadLegacySettings()
    {
        return AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(LegacySettingsPath);
    }

}
