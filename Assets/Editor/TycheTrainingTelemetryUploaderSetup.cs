using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TycheTrainingTelemetryUploaderSetup
{
    const string AppScenePath = "Assets/Scenes/0_App.unity";
    const string AppRootName = "AppMain";

    [MenuItem("Tools/PPE/Configure Local Telemetry DB Upload")]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != AppScenePath)
        {
            EditorUtility.DisplayDialog(
                "Local Telemetry DB Upload",
                $"'{AppScenePath}' 씬을 연 뒤 다시 실행하세요. 현재 씬이나 미저장 변경을 자동으로 바꾸지 않습니다.",
                "확인");
            return;
        }

        GameObject appRoot = scene.GetRootGameObjects()
            .FirstOrDefault(candidate => candidate.name == AppRootName);
        if (appRoot == null)
        {
            Debug.LogError(
                $"[Tyche Telemetry Upload Setup] '{AppScenePath}'에서 '{AppRootName}'을 찾지 못했습니다.");
            return;
        }

        TycheTrainingTelemetryUploader existing =
            appRoot.GetComponent<TycheTrainingTelemetryUploader>();
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log(
                "[Tyche Telemetry Upload Setup] 기존 업로더와 Inspector 작성값을 보존했습니다.",
                existing);
            return;
        }

        TycheTrainingTelemetryUploader uploader =
            Undo.AddComponent<TycheTrainingTelemetryUploader>(appRoot);
        SerializedObject serialized = new(uploader);
        serialized.FindProperty("enableEditorTestUpload").boolValue = true;
        serialized.FindProperty("serverBaseUrl").stringValue = "http://127.0.0.1:3000";
        serialized.FindProperty("batchSize").intValue = 25;
        serialized.FindProperty("scanIntervalSeconds").floatValue = 5f;
        serialized.FindProperty("maximumRetrySeconds").floatValue = 60f;
        serialized.FindProperty("requestTimeoutSeconds").intValue = 15;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = uploader;
        Debug.Log(
            "[Tyche Telemetry Upload Setup] AppMain에 로컬 Editor 테스트 업로더를 추가했습니다. " +
            "씬을 저장하고 Unity를 시작하기 전에 TYCHE_TELEMETRY_UPLOAD_TOKEN 환경 변수를 설정하세요.",
            uploader);
    }

    [MenuItem("Tools/PPE/Validate Local Telemetry DB Upload")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != AppScenePath)
        {
            Debug.LogError(
                $"[Tyche Telemetry Upload Setup] 검증하려면 '{AppScenePath}' 씬을 여세요.");
            return;
        }

        TycheTrainingTelemetryUploader[] uploaders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<TycheTrainingTelemetryUploader>(true))
            .ToArray();
        if (uploaders.Length != 1 || uploaders[0].gameObject.name != AppRootName)
        {
            Debug.LogError(
                $"[Tyche Telemetry Upload Setup] {AppRootName}에 업로더가 정확히 1개 있어야 합니다. 현재 {uploaders.Length}개입니다.");
            return;
        }

        Debug.Log(
            "[Tyche Telemetry Upload Setup] PASS: 새 클라이언트 0_App의 AppMain에 로컬 DB 업로더가 1개 연결되어 있습니다.",
            uploaders[0]);
    }
}
