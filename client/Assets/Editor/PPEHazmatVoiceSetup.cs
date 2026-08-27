using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHazmatVoiceSetup
{
    public const string ScenePath = "Assets/Scenes/3_PPE_Room_3mode_loco.unity";

    const string UsePpeVoicePath =
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_005_UsePPE.mp3";
    const string SuitEndVoicePath =
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_006_Suit_End.mp3";
    const string SuitAlreadyVoicePath =
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_204_SuitAlready.mp3";

    static readonly string[] HazmatPanelNames =
    {
        "PPE_A_SuitHang_Clean",
        "PPE_A_SuitHang_Contam",
        "PPE_A_SuitHang_Ripped",
    };

    [MenuItem("Tools/PPE/Voice/Connect Hazmat 005, 006, and SuitAlready")]
    public static void Apply()
    {
        Scene scene = RequireTargetScene();
        PPEVoiceFlowDirector director = RequireSingleDirector(scene);
        PPEActionPanelController[] panels = RequireHazmatPanels(scene);
        AudioClip usePpeVoice = RequireClip(UsePpeVoicePath);
        AudioClip suitEndVoice = RequireClip(SuitEndVoicePath);
        AudioClip suitAlreadyVoice = RequireClip(SuitAlreadyVoicePath);

        Undo.RecordObject(director, "Connect hazmat variant voices");
        SerializedObject serialized = new(director);
        serialized.Update();

        SerializedProperty panelArray = serialized.FindProperty("m_HazmatActionPanels");
        if (panelArray == null)
            throw new MissingFieldException(typeof(PPEVoiceFlowDirector).Name, "m_HazmatActionPanels");

        panelArray.arraySize = panels.Length;
        for (int index = 0; index < panels.Length; index++)
        {
            panelArray.GetArrayElementAtIndex(index).objectReferenceValue = panels[index];
            Undo.RecordObject(panels[index], "Connect hazmat panel voice flow");
            SerializedObject panelSerialized = new(panels[index]);
            panelSerialized.FindProperty("voiceFlowDirector").objectReferenceValue = director;
            panelSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(panels[index]);
        }

        serialized.FindProperty("m_HazmatPanelVoice").objectReferenceValue = usePpeVoice;
        serialized.FindProperty("m_HazmatEquippedVoice").objectReferenceValue = suitEndVoice;
        serialized.FindProperty("m_HazmatAlreadyEquippedVoice").objectReferenceValue = suitAlreadyVoice;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("Connected hazmat Clean/Contam/Ripped grab narration to 005 UsePPE, approved wear narration to 006 Suit_End, and repeated wear narration to 204 SuitAlready.", director);
    }

    [MenuItem("Tools/PPE/Voice/Validate Hazmat 005, 006, and SuitAlready")]
    public static void Validate()
    {
        Scene scene = RequireTargetScene();
        PPEVoiceFlowDirector director = RequireSingleDirector(scene);
        PPEActionPanelController[] expectedPanels = RequireHazmatPanels(scene);
        AudioClip expected005 = RequireClip(UsePpeVoicePath);
        AudioClip expected006 = RequireClip(SuitEndVoicePath);
        AudioClip expectedSuitAlready = RequireClip(SuitAlreadyVoicePath);

        SerializedObject serialized = new(director);
        SerializedProperty panelArray = serialized.FindProperty("m_HazmatActionPanels");
        if (panelArray == null || panelArray.arraySize != expectedPanels.Length)
            throw new InvalidOperationException("Hazmat voice flow must reference exactly the three authored suit panels.");

        for (int index = 0; index < expectedPanels.Length; index++)
        {
            if (panelArray.GetArrayElementAtIndex(index).objectReferenceValue != expectedPanels[index])
            {
                throw new InvalidOperationException(
                    $"Hazmat panel slot {index} must reference '{HazmatPanelNames[index]}'.");
            }

            PPEItemIdentity identity = expectedPanels[index]
                .InspectionState?.PresentationBinding?.ItemIdentity;
            if (identity == null || identity.ItemType != PPEItemType.HazmatSuit)
                throw new InvalidOperationException($"'{HazmatPanelNames[index]}' must be authored as HazmatSuit.");

            SerializedObject panelSerialized = new(expectedPanels[index]);
            if (panelSerialized.FindProperty("voiceFlowDirector").objectReferenceValue != director)
            {
                throw new InvalidOperationException(
                    $"'{HazmatPanelNames[index]}' must reference the scene PPEVoiceFlowDirector.");
            }
        }

        if (serialized.FindProperty("m_HazmatPanelVoice").objectReferenceValue != expected005)
            throw new InvalidOperationException("Hazmat grab narration must reference 005 UsePPE.");
        if (serialized.FindProperty("m_HazmatEquippedVoice").objectReferenceValue != expected006)
            throw new InvalidOperationException("Hazmat approved-wear narration must reference 006 Suit_End.");
        if (serialized.FindProperty("m_HazmatAlreadyEquippedVoice").objectReferenceValue != expectedSuitAlready)
            throw new InvalidOperationException("Repeated hazmat use must reference 204 SuitAlready.");

        Debug.Log("[PPE Hazmat Voice Validation] PASS: three suit panels -> 005 UsePPE, approved wear -> 006 Suit_End, repeated wear -> 204 SuitAlready.", director);
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            throw new InvalidOperationException($"Open the target scene before applying hazmat voices: {ScenePath}");
        return scene;
    }

    static PPEVoiceFlowDirector RequireSingleDirector(Scene scene)
    {
        PPEVoiceFlowDirector[] directors = FindSceneComponents<PPEVoiceFlowDirector>(scene);
        if (directors.Length != 1)
            throw new InvalidOperationException($"Expected one PPEVoiceFlowDirector, found {directors.Length}.");
        return directors[0];
    }

    static PPEActionPanelController[] RequireHazmatPanels(Scene scene)
    {
        PPEActionPanelController[] allPanels = FindSceneComponents<PPEActionPanelController>(scene);
        PPEActionPanelController[] result = new PPEActionPanelController[HazmatPanelNames.Length];
        for (int index = 0; index < HazmatPanelNames.Length; index++)
        {
            PPEActionPanelController[] matches = allPanels
                .Where(panel => panel.name == HazmatPanelNames[index])
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one '{HazmatPanelNames[index]}', found {matches.Length}.");
            }

            result[index] = matches[0];
        }

        return result;
    }

    static AudioClip RequireClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"Missing authored voice clip: {path}");
        return clip;
    }

    static T[] FindSceneComponents<T>(Scene scene) where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include)
            .Where(component => component.gameObject.scene == scene)
            .ToArray();
    }
}
