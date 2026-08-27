using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class AuthoredWorldCanvasPoseSaver
{
    static AuthoredWorldCanvasPoseSaver()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    static void OnSceneSaving(Scene scene, string path)
    {
        if (!scene.IsValid())
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (AuthoredWorldCanvasPose pose in root.GetComponentsInChildren<AuthoredWorldCanvasPose>(true))
            {
                if (pose == null)
                    continue;

                pose.CaptureFromCurrent();
                EditorUtility.SetDirty(pose);
            }
        }
    }

    [MenuItem("Tools/UI/Capture Authored World Canvas Poses")]
    static void CaptureAllInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("No active scene to capture authored canvas poses.");
            return;
        }

        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (AuthoredWorldCanvasPose pose in root.GetComponentsInChildren<AuthoredWorldCanvasPose>(true))
            {
                pose.CaptureFromCurrent();
                EditorUtility.SetDirty(pose);
                count++;
            }
        }

        if (count == 0)
            Debug.LogWarning("No AuthoredWorldCanvasPose components found in the active scene.");
        else
            Debug.Log($"Captured authored pose on {count} world canvas object(s).");
    }
}
