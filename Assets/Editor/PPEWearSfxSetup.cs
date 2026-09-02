using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Connects the project-authored PPE wear clips to the action panels that own
/// the matching UseApproved event. The open scene remains the source of truth;
/// this command does not rebuild panels, AudioSources, or gameplay state.
/// </summary>
public static class PPEWearSfxSetup
{
    public const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";

    const string FaceWearId = "Wearing Mask Glass Shield";
    const string HelmetId = "Helmet";
    const string InnerGloveId = "Nitril InnerGlove";
    const string ScbaPath = "PPE/PPE_A_SCBA";
    const string ScbaDisplayName = "SCBA";

    static readonly ClipSpec[] ClipSpecs =
    {
        new(FaceWearId, "Assets/Audio/SFX/Wearing Mask Glass Shield.ogg"),
        new(HelmetId, "Assets/Audio/SFX/Helmet.ogg"),
        new(InnerGloveId, "Assets/Audio/SFX/Nitril InnerGlove.ogg"),
    };

    static readonly PanelSpec[] PanelSpecs =
    {
        new("PPE/PPE_A_Mask_Clean", FaceWearId),
        new("PPE/PPE_A_Goggle_Clean", FaceWearId),
        new("PPE/PPE_A_FaceShield_Clean", FaceWearId),
        new("PPE/PPE_A_Helmet_Strap", HelmetId),
        new("PPE/PPE_A_InnerGlove_L", InnerGloveId),
        new("PPE/PPE_A_InnerGlove_R", InnerGloveId),
    };

    [MenuItem("Tools/PPE/Audio/Apply New Wear SFX and SCBA Label")]
    public static void Apply()
    {
        Scene scene = RequireTargetScene();
        Dictionary<string, AudioClip> clips = LoadClips();
        AudioManager manager = RequireSingleSceneComponent<AudioManager>(scene);

        SerializedObject managerSerialized = new(manager);
        SerializedProperty library = managerSerialized.FindProperty("m_Sfx")
            ?? throw new InvalidOperationException("AudioManager.m_Sfx was not found.");

        Undo.RecordObject(manager, "Connect PPE wear SFX library");
        foreach (ClipSpec spec in ClipSpecs)
            AddLibraryEntryIfMissing(library, spec.Id, clips[spec.Id]);
        managerSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        Dictionary<string, PPEActionPanelController> panels = FindPanels(scene);
        foreach (PanelSpec spec in PanelSpecs)
        {
            if (!panels.TryGetValue(spec.Path, out PPEActionPanelController panel))
                throw new InvalidOperationException($"PPE action panel was not found at '{spec.Path}'.");

            SerializedObject serializedPanel = new(panel);
            SerializedProperty useSfxId = serializedPanel.FindProperty("useSfxId")
                ?? throw new InvalidOperationException($"{spec.Path}.useSfxId was not found.");
            if (useSfxId.stringValue == spec.SfxId)
                continue;

            Undo.RecordObject(panel, "Connect PPE wear SFX");
            useSfxId.stringValue = spec.SfxId;
            serializedPanel.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
        }

        SetScbaDisplayName(panels);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("[PPE Wear SFX] Applied clean face-wear, helmet, nitrile inner-glove clips, and the SCBA display name without changing PPE state.");
    }

    [MenuItem("Tools/PPE/Audio/Validate New Wear SFX and SCBA Label")]
    public static void Validate()
    {
        Scene scene = RequireTargetScene();
        Dictionary<string, AudioClip> clips = LoadClips();
        AudioManager manager = RequireSingleSceneComponent<AudioManager>(scene);
        List<string> failures = new();

        SerializedProperty library = new SerializedObject(manager).FindProperty("m_Sfx");
        foreach (ClipSpec spec in ClipSpecs)
            ValidateLibraryEntry(library, spec.Id, clips[spec.Id], failures);

        Dictionary<string, PPEActionPanelController> panels = FindPanels(scene);
        foreach (PanelSpec spec in PanelSpecs)
        {
            if (!panels.TryGetValue(spec.Path, out PPEActionPanelController panel))
            {
                failures.Add($"Missing PPE action panel '{spec.Path}'.");
                continue;
            }

            string actual = new SerializedObject(panel).FindProperty("useSfxId")?.stringValue;
            if (actual != spec.SfxId)
                failures.Add($"{spec.Path}.useSfxId must be '{spec.SfxId}', not '{actual}'.");
        }

        ValidatePreservedPanel(panels, "PPE/PPE_A_SCBA", "harness", failures);
        ValidatePreservedPanel(panels, "PPE/PPE_A_Backplate", "harness", failures);
        ValidatePreservedPanel(panels, "PPE/PPE_A_Glove_L", "Gloves", failures);
        ValidatePreservedPanel(panels, "PPE/PPE_A_Glove_R", "Gloves", failures);
        ValidateRejectedMask(panels, library, failures);
        ValidateScbaDisplayName(panels, failures);

        if (failures.Count > 0)
        {
            string message = "PPE wear SFX validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[PPE Wear SFX] PASS: 3 clips, 6 approved-wear panels, rejected-mask feedback, preserved mappings, and SCBA label are valid.");
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before applying or validating PPE wear SFX. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static Dictionary<string, AudioClip> LoadClips()
    {
        Dictionary<string, AudioClip> clips = new(StringComparer.Ordinal);
        foreach (ClipSpec spec in ClipSpecs)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(spec.Path);
            if (clip == null)
                throw new InvalidOperationException($"Required PPE wear clip was not found: '{spec.Path}'.");
            clips.Add(spec.Id, clip);
        }

        return clips;
    }

    static T RequireSingleSceneComponent<T>(Scene scene) where T : Component
    {
        T[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected one scene {typeof(T).Name}, found {matches.Length}.");
        return matches[0];
    }

    static Dictionary<string, PPEActionPanelController> FindPanels(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEActionPanelController>(true))
            .ToDictionary(panel => GetPath(panel.transform), panel => panel, StringComparer.Ordinal);
    }

    static void AddLibraryEntryIfMissing(SerializedProperty library, string id, AudioClip clip)
    {
        SerializedProperty existing = FindLibraryEntry(library, id);
        if (existing != null)
        {
            AudioClip authoredClip = existing.FindPropertyRelative("clip")?.objectReferenceValue as AudioClip;
            if (authoredClip != clip)
            {
                throw new InvalidOperationException(
                    $"AudioManager SFX id '{id}' already exists with a different clip. Resolve it in the Inspector.");
            }

            return;
        }

        int index = library.arraySize;
        library.arraySize++;
        SerializedProperty entry = library.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("id").stringValue = id;
        entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        entry.FindPropertyRelative("volume").floatValue = 1f;
        entry.FindPropertyRelative("enabled").boolValue = true;
    }

    static void ValidateLibraryEntry(
        SerializedProperty library,
        string id,
        AudioClip expectedClip,
        List<string> failures)
    {
        SerializedProperty entry = FindLibraryEntry(library, id);
        if (entry == null)
        {
            failures.Add($"AudioManager is missing SFX id '{id}'.");
            return;
        }

        if (entry.FindPropertyRelative("clip")?.objectReferenceValue != expectedClip)
            failures.Add($"AudioManager SFX '{id}' has the wrong clip.");
        if (entry.FindPropertyRelative("enabled")?.boolValue != true)
            failures.Add($"AudioManager SFX '{id}' must be enabled.");
        if ((entry.FindPropertyRelative("volume")?.floatValue ?? 0f) <= 0f)
            failures.Add($"AudioManager SFX '{id}' must have positive volume.");
    }

    static SerializedProperty FindLibraryEntry(SerializedProperty library, string id)
    {
        if (library == null)
            return null;

        SerializedProperty match = null;
        for (int index = 0; index < library.arraySize; index++)
        {
            SerializedProperty candidate = library.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("id")?.stringValue != id)
                continue;
            if (match != null)
                throw new InvalidOperationException($"AudioManager has duplicate SFX id '{id}'.");
            match = candidate;
        }

        return match;
    }

    static void ValidatePreservedPanel(
        Dictionary<string, PPEActionPanelController> panels,
        string path,
        string expectedId,
        List<string> failures)
    {
        if (!panels.TryGetValue(path, out PPEActionPanelController panel))
        {
            failures.Add($"Preserved PPE panel '{path}' is missing.");
            return;
        }

        string actual = new SerializedObject(panel).FindProperty("useSfxId")?.stringValue;
        if (actual != expectedId)
            failures.Add($"Preserved panel {path}.useSfxId changed from '{expectedId}' to '{actual}'.");
    }

    static void SetScbaDisplayName(Dictionary<string, PPEActionPanelController> panels)
    {
        if (!panels.TryGetValue(ScbaPath, out PPEActionPanelController panel))
            throw new InvalidOperationException($"SCBA action panel was not found at '{ScbaPath}'.");

        SerializedObject serializedPanel = new(panel);
        SerializedProperty displayName = serializedPanel.FindProperty("itemDisplayName")
            ?? throw new InvalidOperationException($"{ScbaPath}.itemDisplayName was not found.");
        if (displayName.stringValue == ScbaDisplayName)
            return;

        Undo.RecordObject(panel, "Set SCBA display name");
        displayName.stringValue = ScbaDisplayName;
        serializedPanel.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
    }

    static void ValidateRejectedMask(
        Dictionary<string, PPEActionPanelController> panels,
        SerializedProperty library,
        List<string> failures)
    {
        const string path = "PPE/PPE_A_Mask_Contam";
        if (!panels.TryGetValue(path, out PPEActionPanelController panel))
        {
            failures.Add($"Rejected mask panel '{path}' is missing.");
            return;
        }

        SerializedObject serializedPanel = new(panel);
        string useId = serializedPanel.FindProperty("useSfxId")?.stringValue;
        if (!string.IsNullOrWhiteSpace(useId))
            failures.Add($"Rejected mask {path}.useSfxId must remain empty, not '{useId}'.");

        string wrongId = serializedPanel.FindProperty("wrongFeedbackSfxId")?.stringValue;
        SerializedProperty wrongEntry = FindLibraryEntry(library, wrongId);
        if (wrongId != "Wrong Answer" ||
            wrongEntry?.FindPropertyRelative("clip")?.objectReferenceValue == null ||
            wrongEntry.FindPropertyRelative("enabled")?.boolValue != true)
        {
            failures.Add("Rejected mask must retain an enabled 'Wrong Answer' SFX mapping.");
        }

        PPEItemPresentationBinding binding = panel.InspectionState?.PresentationBinding;
        SerializedProperty initialCondition = binding != null
            ? new SerializedObject(binding).FindProperty("initialCondition")
            : null;
        if (initialCondition == null ||
            initialCondition.enumValueIndex != (int)PPEItemCondition.Contaminated)
        {
            failures.Add("Rejected mask must retain its Contaminated authored condition.");
        }
    }

    static void ValidateScbaDisplayName(
        Dictionary<string, PPEActionPanelController> panels,
        List<string> failures)
    {
        if (!panels.TryGetValue(ScbaPath, out PPEActionPanelController panel))
        {
            failures.Add($"SCBA action panel '{ScbaPath}' is missing.");
            return;
        }

        string displayName = new SerializedObject(panel).FindProperty("itemDisplayName")?.stringValue;
        if (displayName != ScbaDisplayName)
            failures.Add($"{ScbaPath}.itemDisplayName must be '{ScbaDisplayName}', not '{displayName}'.");
    }

    static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    readonly struct ClipSpec
    {
        public ClipSpec(string id, string path)
        {
            Id = id;
            Path = path;
        }

        public string Id { get; }
        public string Path { get; }
    }

    readonly struct PanelSpec
    {
        public PanelSpec(string path, string sfxId)
        {
            Path = path;
            SfxId = sfxId;
        }

        public string Path { get; }
        public string SfxId { get; }
    }
}
