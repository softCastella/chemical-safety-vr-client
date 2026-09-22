using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEWearHapticTuningSetup
{
    private const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    internal const float WearAmplitude = 0.85f;
    internal const float WearDuration = 0.08f;

    [MenuItem("Tools/PPE/Tune Wear Haptics")]
    public static void Configure()
    {
        Scene scene = RequireCleanTargetScene("tuning wear haptics");
        PPEActionPanelController[] panels = FindComponents<PPEActionPanelController>(scene);
        if (panels.Length == 0)
            throw new InvalidOperationException("The PPE room has no PPEActionPanelController components.");

        foreach (PPEActionPanelController panel in panels)
        {
            Undo.RecordObject(panel, "Tune PPE wear haptics");
            SerializedObject serializedPanel = new(panel);
            serializedPanel.FindProperty("playWearHaptics").boolValue = true;
            serializedPanel.FindProperty("wearHapticAmplitude").floatValue = WearAmplitude;
            serializedPanel.FindProperty("wearHapticDuration").floatValue = WearDuration;
            serializedPanel.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
    }

    [MenuItem("Tools/PPE/Validate Wear Haptic Tuning")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before validating wear haptics.");

        List<string> failures = new();
        PPEActionPanelController[] panels = FindComponents<PPEActionPanelController>(scene);
        if (panels.Length == 0)
            failures.Add("The PPE room has no PPEActionPanelController components.");

        foreach (PPEActionPanelController panel in panels)
        {
            if (!panel.PlayWearHaptics)
                failures.Add($"{panel.name}: wear haptics are disabled.");
            if (Mathf.Abs(panel.WearHapticAmplitude - WearAmplitude) > 0.001f)
                failures.Add($"{panel.name}: wear haptic amplitude must be {WearAmplitude:0.##}.");
            if (Mathf.Abs(panel.WearHapticDuration - WearDuration) > 0.001f)
                failures.Add($"{panel.name}: wear haptic duration must remain {WearDuration:0.##} seconds.");
        }

        if (failures.Count > 0)
        {
            string message = "PPE wear haptic tuning validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Wear Haptics] PASS: {panels.Length} panels use amplitude {WearAmplitude:0.##} " +
            $"for {WearDuration:0.##} seconds without changing the approved-wear trigger.");
    }

    private static Scene RequireCleanTargetScene(string operation)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException($"Stop Play Mode before {operation}.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before {operation}.");
        if (scene.isDirty)
            throw new InvalidOperationException("The active PPE scene has unsaved changes. Review or save them first.");
        return scene;
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        List<T> components = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            components.AddRange(root.GetComponentsInChildren<T>(true));
        return components.ToArray();
    }
}
