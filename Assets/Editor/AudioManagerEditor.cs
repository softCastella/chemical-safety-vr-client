using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public sealed class AudioManagerEditor : Editor
{
    private const string LegacySettingsPath = "Assets/Resources/Audio/AudioManagerSettings.asset";
    private const string SceneSettingsDirectory = "Assets/Resources/Audio/Scenes";

    private enum LibraryKind
    {
        Bgm,
        Voice,
        Sfx,
        Ambience,
    }

    private sealed class AudioCandidate
    {
        public LibraryKind library;
        public string id;
        public AudioClip clip;
        public float volume;
    }

    private readonly List<AudioClip> m_QueuedVoicePreviewClips = new();
    private int m_VoicePreviewIndex;
    private double m_NextVoicePreviewTime;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSources();
        DrawSceneStartBgm();
        DrawLibrary("BGM", serializedObject.FindProperty("m_Bgm"));
        DrawLibrary("Voice", serializedObject.FindProperty("m_Voice"), true);
        DrawLibrary("SFX", serializedObject.FindProperty("m_Sfx"));
        DrawLibrary("Ambience", serializedObject.FindProperty("m_Ambience"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSources()
    {
        EditorGUILayout.LabelField("Scene-owned Sources", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BgmSource"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_VoiceSource"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SfxSource"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AmbienceSource"));
        if (GUILayout.Button("모든 소스의 누락 오디오 가져오기"))
            ImportMissingAudioFromKnownSources();
        EditorGUILayout.Space(8f);
    }

    private void DrawSceneStartBgm()
    {
        EditorGUILayout.LabelField("This Scene: Start BGM", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "이 설정은 현재 씬 시작 시점에만 적용됩니다. 전체 앱 또는 이후 Voice Flow의 시작 설정이 아닙니다.",
            MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PlayBgmOnStart"), new GUIContent("Play on This Scene Start"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StartupBgmId"), new GUIContent("This Scene Start BGM ID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StartupBgmDelay"), new GUIContent("Start Delay"));
        EditorGUILayout.Space(8f);
    }

    private void DrawLibrary(string title, SerializedProperty library, bool supportsSequencePreview = false)
    {
        EditorGUILayout.LabelField($"{title} Library", EditorStyles.boldLabel);
        if (supportsSequencePreview)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play All Enabled Voice Clips"))
                PlayAllEnabledVoiceClips(library);
            if (GUILayout.Button("Stop Voice Preview"))
                StopPreview();
            EditorGUILayout.EndHorizontal();

        }

        if (library.arraySize == 0)
            EditorGUILayout.HelpBox($"등록된 {title} 클립이 없습니다.", MessageType.Warning);

        for (int index = 0; index < library.arraySize; index++)
        {
            SerializedProperty entry = library.GetArrayElementAtIndex(index);
            SerializedProperty id = entry.FindPropertyRelative("id");
            SerializedProperty clip = entry.FindPropertyRelative("clip");
            SerializedProperty volume = entry.FindPropertyRelative("volume");
            SerializedProperty enabled = entry.FindPropertyRelative("enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(id.stringValue) ? $"{title} {index + 1}" : id.stringValue, EditorStyles.boldLabel);
            enabled.boolValue = EditorGUILayout.ToggleLeft("Enabled", enabled.boolValue, GUILayout.Width(75f));
            using (new EditorGUI.DisabledScope(clip.objectReferenceValue is not AudioClip))
            {
                if (GUILayout.Button("Play", GUILayout.Width(48f)))
                    PlaySinglePreview(clip.objectReferenceValue as AudioClip);
            }
            if (GUILayout.Button("Stop", GUILayout.Width(48f)))
                StopPreview();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(id);
            EditorGUILayout.PropertyField(clip);
            EditorGUILayout.PropertyField(volume);
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button($"Add {title} Clip"))
        {
            library.arraySize++;
            SerializedProperty entry = library.GetArrayElementAtIndex(library.arraySize - 1);
            entry.FindPropertyRelative("volume").floatValue = 1f;
            entry.FindPropertyRelative("enabled").boolValue = true;
        }

        EditorGUILayout.Space(8f);
    }

    private void ImportMissingAudioFromKnownSources()
    {
        AudioManager manager = (AudioManager)target;
        SerializedProperty bgm = serializedObject.FindProperty("m_Bgm");
        SerializedProperty voice = serializedObject.FindProperty("m_Voice");
        SerializedProperty sfx = serializedObject.FindProperty("m_Sfx");
        SerializedProperty ambience = serializedObject.FindProperty("m_Ambience");
        List<AudioCandidate> missing = new();

        CollectLegacySettings(bgm, sfx, ambience, missing);
        CollectSceneSettings(manager, bgm, ambience, missing);
        CollectVoiceFlowClips(manager, voice, missing);
        CollectSignatureClips(manager, sfx, missing);
        CollectDirectAudioSourceClips(manager, sfx, ambience, missing);

        if (missing.Count == 0)
        {
            Debug.Log("Audio import found no missing clips. Existing library entries were kept unchanged.", target);
            return;
        }

        Undo.RecordObject(target, "Import missing scene audio clips");
        int importedBgm = 0;
        int importedVoice = 0;
        int importedSfx = 0;
        int importedAmbience = 0;
        foreach (AudioCandidate candidate in missing)
        {
            SerializedProperty library = GetLibrary(candidate.library, bgm, voice, sfx, ambience);
            AddAudioClip(library, candidate);
            switch (candidate.library)
            {
                case LibraryKind.Bgm: importedBgm++; break;
                case LibraryKind.Voice: importedVoice++; break;
                case LibraryKind.Sfx: importedSfx++; break;
                case LibraryKind.Ambience: importedAmbience++; break;
            }
        }

        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log(
            $"Imported missing scene audio clips: BGM {importedBgm}, Voice {importedVoice}, SFX {importedSfx}, Ambience {importedAmbience}. Existing settings were not changed.",
            target);
    }

    private static SerializedProperty GetLibrary(
        LibraryKind kind,
        SerializedProperty bgm,
        SerializedProperty voice,
        SerializedProperty sfx,
        SerializedProperty ambience)
    {
        return kind switch
        {
            LibraryKind.Bgm => bgm,
            LibraryKind.Voice => voice,
            LibraryKind.Sfx => sfx,
            LibraryKind.Ambience => ambience,
            _ => null,
        };
    }

    private static void CollectLegacySettings(
        SerializedProperty bgm,
        SerializedProperty sfx,
        SerializedProperty ambience,
        List<AudioCandidate> missing)
    {
        AudioManagerSettings settings = AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(LegacySettingsPath);
        if (settings == null)
            return;

        CollectSettingsLibrary(bgm, missing, settings.bgm, LibraryKind.Bgm);
        CollectSettingsLibrary(sfx, missing, settings.sfx, LibraryKind.Sfx);
        CollectSettingsLibrary(ambience, missing, settings.ambience, LibraryKind.Ambience);
    }

    private static void CollectSettingsLibrary(
        SerializedProperty library,
        List<AudioCandidate> missing,
        List<AudioManagerSettings.Sound> source,
        LibraryKind kind)
    {
        if (source == null)
            return;

        foreach (AudioManagerSettings.Sound sound in source)
        {
            if (sound != null)
                AddIfMissing(library, missing, kind, sound.id, sound.clip, sound.volume);
        }
    }

    private static void CollectSceneSettings(
        AudioManager manager,
        SerializedProperty bgm,
        SerializedProperty ambience,
        List<AudioCandidate> missing)
    {
        string sceneName = manager.gameObject.scene.name;
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        SceneAudioSettings settings = AssetDatabase.LoadAssetAtPath<SceneAudioSettings>(
            $"{SceneSettingsDirectory}/{sceneName}.asset");
        if (settings == null)
            return;

        AddIfMissing(bgm, missing, LibraryKind.Bgm, settings.bgm != null ? settings.bgm.name : null, settings.bgm, settings.bgmVolume);
        AddIfMissing(ambience, missing, LibraryKind.Ambience, settings.ambience != null ? settings.ambience.name : null, settings.ambience, settings.ambienceVolume);
    }

    private static void CollectVoiceFlowClips(
        AudioManager manager,
        SerializedProperty library,
        List<AudioCandidate> missing)
    {
        PPEVoiceFlowDirector[] directors = UnityEngine.Object.FindObjectsByType<PPEVoiceFlowDirector>(
            FindObjectsInactive.Include);
        foreach (PPEVoiceFlowDirector director in directors)
        {
            if (director.gameObject.scene != manager.gameObject.scene)
                continue;

            SerializedObject serializedDirector = new(director);
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
                    CollectSerializedClips(library, missing, LibraryKind.Voice, step.FindPropertyRelative("clips"));
                    CollectSerializedClips(library, missing, LibraryKind.Voice, step.FindPropertyRelative("repeatClip"));
                    CollectSerializedClips(library, missing, LibraryKind.Voice, step.FindPropertyRelative("controllerWrongInputClip"));
                    CollectSerializedClips(library, missing, LibraryKind.Voice, step.FindPropertyRelative("controllerCorrectInputClip"));
                    CollectSerializedClips(library, missing, LibraryKind.Voice, step.FindPropertyRelative("controllerCompletionClip"));
                }
            }
        }
    }

    private static void CollectSignatureClips(
        AudioManager manager,
        SerializedProperty library,
        List<AudioCandidate> missing)
    {
        HandwrittenSignatureSequence[] sequences = UnityEngine.Object.FindObjectsByType<HandwrittenSignatureSequence>(
            FindObjectsInactive.Include);
        foreach (HandwrittenSignatureSequence sequence in sequences)
        {
            if (sequence.gameObject.scene != manager.gameObject.scene)
                continue;

            SerializedProperty signatures = new SerializedObject(sequence).FindProperty("signatures");
            if (signatures == null)
                continue;

            for (int index = 0; index < signatures.arraySize; index++)
            {
                SerializedProperty step = signatures.GetArrayElementAtIndex(index);
                AudioClip clip = step.FindPropertyRelative("sound").objectReferenceValue as AudioClip;
                float volume = step.FindPropertyRelative("soundVolume").floatValue;
                AddIfMissing(library, missing, LibraryKind.Sfx, clip != null ? clip.name : null, clip, volume);
            }
        }
    }

    private static void CollectDirectAudioSourceClips(
        AudioManager manager,
        SerializedProperty sfx,
        SerializedProperty ambience,
        List<AudioCandidate> missing)
    {
        SerializedObject serializedManager = new(manager);
        HashSet<AudioSource> managerSources = new()
        {
            serializedManager.FindProperty("m_BgmSource").objectReferenceValue as AudioSource,
            serializedManager.FindProperty("m_VoiceSource").objectReferenceValue as AudioSource,
            serializedManager.FindProperty("m_SfxSource").objectReferenceValue as AudioSource,
            serializedManager.FindProperty("m_AmbienceSource").objectReferenceValue as AudioSource,
        };

        AudioSource[] sources = UnityEngine.Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include);
        foreach (AudioSource source in sources)
        {
            if (source.gameObject.scene != manager.gameObject.scene || source.clip == null || managerSources.Contains(source))
                continue;

            LibraryKind kind = source.loop ? LibraryKind.Ambience : LibraryKind.Sfx;
            AddIfMissing(
                kind == LibraryKind.Ambience ? ambience : sfx,
                missing,
                kind,
                source.clip.name,
                source.clip,
                source.volume);
        }
    }

    private static void CollectSerializedClips(
        SerializedProperty library,
        List<AudioCandidate> missing,
        LibraryKind kind,
        SerializedProperty clipsProperty)
    {
        if (clipsProperty == null)
            return;

        if (clipsProperty.isArray)
        {
            for (int index = 0; index < clipsProperty.arraySize; index++)
            {
                AudioClip clip = clipsProperty.GetArrayElementAtIndex(index).objectReferenceValue as AudioClip;
                AddIfMissing(library, missing, kind, clip != null ? clip.name : null, clip, 1f);
            }
            return;
        }

        AudioClip repeatClip = clipsProperty.objectReferenceValue as AudioClip;
        AddIfMissing(library, missing, kind, repeatClip != null ? repeatClip.name : null, repeatClip, 1f);
    }

    private static void AddIfMissing(
        SerializedProperty library,
        List<AudioCandidate> missing,
        LibraryKind kind,
        string id,
        AudioClip clip,
        float volume)
    {
        if (clip == null || ContainsClip(library, clip) || ContainsCandidate(missing, kind, clip))
            return;

        missing.Add(new AudioCandidate
        {
            library = kind,
            id = string.IsNullOrWhiteSpace(id) ? clip.name : id,
            clip = clip,
            volume = Mathf.Clamp01(volume),
        });
    }

    private static bool ContainsCandidate(List<AudioCandidate> candidates, LibraryKind kind, AudioClip clip)
    {
        foreach (AudioCandidate candidate in candidates)
        {
            if (candidate.library == kind && candidate.clip == clip)
                return true;
        }

        return false;
    }

    private static void AddAudioClip(SerializedProperty library, AudioCandidate candidate)
    {
        int index = library.arraySize;
        library.arraySize++;
        SerializedProperty entry = library.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("id").stringValue = candidate.id;
        entry.FindPropertyRelative("clip").objectReferenceValue = candidate.clip;
        entry.FindPropertyRelative("volume").floatValue = candidate.volume;
        entry.FindPropertyRelative("enabled").boolValue = true;
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

    private void PlayAllEnabledVoiceClips(SerializedProperty library)
    {
        StopPreview();

        for (int index = 0; index < library.arraySize; index++)
        {
            SerializedProperty entry = library.GetArrayElementAtIndex(index);
            bool enabled = entry.FindPropertyRelative("enabled").boolValue;
            AudioClip clip = entry.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
            if (enabled && clip != null)
                m_QueuedVoicePreviewClips.Add(clip);
        }

        if (m_QueuedVoicePreviewClips.Count == 0)
        {
            Debug.LogWarning("No enabled Voice clips are registered for preview.", target);
            return;
        }

        PlayNextQueuedVoiceClip();
    }

    private void PlaySinglePreview(AudioClip clip)
    {
        StopPreview();
        PlayPreviewClip(clip);
    }

    private void PlayNextQueuedVoiceClip()
    {
        if (m_VoicePreviewIndex >= m_QueuedVoicePreviewClips.Count)
        {
            StopPreview();
            return;
        }

        AudioClip clip = m_QueuedVoicePreviewClips[m_VoicePreviewIndex];
        PlayPreviewClip(clip);
        m_NextVoicePreviewTime = EditorApplication.timeSinceStartup + Mathf.Max(0.05f, clip.length);
        EditorApplication.update -= UpdateQueuedVoicePreview;
        EditorApplication.update += UpdateQueuedVoicePreview;
    }

    private void UpdateQueuedVoicePreview()
    {
        if (EditorApplication.timeSinceStartup < m_NextVoicePreviewTime)
            return;

        m_VoicePreviewIndex++;
        PlayNextQueuedVoiceClip();
    }

    private static void PlayPreviewClip(AudioClip clip)
    {
        MethodInfo playPreviewClipMethod = FindAudioUtilMethod("PlayPreviewClip");
        if (playPreviewClipMethod == null || clip == null)
        {
            Debug.LogWarning("Unity Editor AudioUtil.PlayPreviewClip is unavailable.");
            return;
        }

        try
        {
            ParameterInfo[] parameters = playPreviewClipMethod.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = clip;
            for (int index = 1; index < arguments.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                arguments[index] = type == typeof(bool)
                    ? false
                    : type == typeof(int)
                        ? 0
                        : type == typeof(float)
                            ? 1f
                            : parameters[index].HasDefaultValue ? parameters[index].DefaultValue : null;
            }

            playPreviewClipMethod.Invoke(null, arguments);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Audio preview failed for '{clip.name}': {exception.Message}");
        }
    }

    private void StopPreview()
    {
        EditorApplication.update -= UpdateQueuedVoicePreview;
        m_QueuedVoicePreviewClips.Clear();
        m_VoicePreviewIndex = 0;

        try
        {
            FindAudioUtilMethod("StopAllPreviewClips")?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Audio preview stop failed: {exception.Message}");
        }
    }

    private static MethodInfo FindAudioUtilMethod(string name)
    {
        Type audioUtil = FindAudioUtilType();
        if (audioUtil == null)
            return null;

        foreach (MethodInfo method in audioUtil.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name != name)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (name == "PlayPreviewClip" && (parameters.Length == 0 || parameters[0].ParameterType != typeof(AudioClip)))
                continue;

            return method;
        }

        return null;
    }

    private static Type FindAudioUtilType()
    {
        // In Unity 6000 Editor lives in UnityEditor.CoreModule while AudioUtil
        // is loaded from a different editor assembly. Looking only at
        // typeof(Editor).Assembly therefore makes every preview button a no-op.
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type audioUtil = assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil != null)
                return audioUtil;
        }

        return null;
    }

    private void OnDisable()
    {
        StopPreview();
    }
}
