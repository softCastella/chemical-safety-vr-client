using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public static class TycheTrainingTelemetryUploaderSetup
{
    const string AppScenePath = "Assets/Scenes/0_App.unity";
    const string AppRootName = "AppMain";

    [Serializable]
    sealed class DevelopmentLanConfiguration
    {
        public string serverBaseUrl;
        public string uploadToken;
    }

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
        serialized.FindProperty("productionServerBaseUrl").stringValue =
            "https://immersa.tycheworks.com";
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

        SerializedObject serializedUploader = new(uploaders[0]);
        string productionServerBaseUrl = serializedUploader
            .FindProperty("productionServerBaseUrl")?.stringValue;
        if (!TycheMetaSessionAuthenticator.TryNormalizeProductionServerBaseUrl(
                productionServerBaseUrl,
                out _,
                out string productionFailure))
        {
            Debug.LogError(
                $"[Tyche Telemetry Upload Setup] Release HTTPS 설정이 유효하지 않습니다. {productionFailure}",
                uploaders[0]);
            return;
        }

        MetaPlatformIdentityProbe identityProbe = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MetaPlatformIdentityProbe>(true))
            .SingleOrDefault();
        if (identityProbe == null)
        {
            Debug.LogError(
                "[Tyche Telemetry Upload Setup] App scene에 MetaPlatformIdentityProbe가 정확히 1개 있어야 합니다.");
            return;
        }
        SerializedObject serializedIdentity = new(identityProbe);
        if (serializedIdentity.FindProperty("logAppScopedUserId").boolValue)
        {
            Debug.LogError(
                "[Tyche Telemetry Upload Setup] Release 전에 Meta 앱 범위 사용자 ID 원문 로그를 꺼야 합니다.",
                identityProbe);
            return;
        }

        Debug.Log(
            "[Tyche Telemetry Upload Setup] PASS: 0_App의 로컬 개발 전송과 Release HTTPS·Meta 인증 설정이 분리되어 있습니다.",
            uploaders[0]);
    }

    [MenuItem("Tools/PPE/Inject Quest Development LAN Configuration")]
    public static void InjectQuestDevelopmentLanConfiguration()
    {
        string serverBaseUrl = ReadEnvironmentVariable(
            TycheTrainingTelemetryUploader.QuestLanServerBaseUrlEnvironmentVariable);
        string uploadToken = ReadEnvironmentVariable(
            TycheTrainingTelemetryUploader.UploadTokenEnvironmentVariable);
        if (!TycheTrainingTelemetryUploader.TryNormalizePrivateLanServerBaseUrl(
                serverBaseUrl,
                out string normalizedBaseUrl,
                out string addressFailure))
        {
            Debug.LogError($"[Quest LAN Setup] {addressFailure}");
            return;
        }
        if (string.IsNullOrEmpty(uploadToken) || uploadToken.Length < 16 ||
            uploadToken.Length > 512 || uploadToken.Any(character => character < '!' || character > '~'))
        {
            Debug.LogError(
                $"[Quest LAN Setup] {TycheTrainingTelemetryUploader.UploadTokenEnvironmentVariable}는 " +
                "16~512자의 공백 없는 ASCII여야 합니다.");
            return;
        }

        string packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(packageName) ||
            packageName.Any(character =>
                !(char.IsLetterOrDigit(character) || character == '.' || character == '_')))
        {
            Debug.LogError("[Quest LAN Setup] Android application identifier가 안전한 패키지 이름이 아닙니다.");
            return;
        }

        string editorDirectory = Path.GetDirectoryName(EditorApplication.applicationPath);
        string adbPath = Path.Combine(
            editorDirectory ?? string.Empty,
            "Data",
            "PlaybackEngines",
            "AndroidPlayer",
            "SDK",
            "platform-tools",
            "adb.exe");
        if (!File.Exists(adbPath))
        {
            Debug.LogError($"[Quest LAN Setup] Unity Android SDK의 adb를 찾지 못했습니다: {adbPath}");
            return;
        }

        if (!RunAdb(adbPath, $"shell am force-stop {packageName}", null, out string stopFailure))
        {
            Debug.LogError($"[Quest LAN Setup] 설치된 개발 APK를 중지하지 못했습니다: {stopFailure}");
            return;
        }

        DevelopmentLanConfiguration configuration = new()
        {
            serverBaseUrl = normalizedBaseUrl,
            uploadToken = uploadToken,
        };
        string json = JsonUtility.ToJson(configuration);
        string createFilesArguments = $"shell run-as {packageName} mkdir -p files";
        if (!RunAdb(adbPath, createFilesArguments, null, out string createFilesFailure))
        {
            Debug.LogError(
                "[Quest LAN Setup] 개발 APK의 내부 설정 디렉터리를 준비하지 못했습니다. " +
                createFilesFailure);
            return;
        }

        string injectArguments =
            $"shell run-as {packageName} tee files/{TycheTrainingTelemetryUploader.DevelopmentLanConfigurationFileName}";
        if (!RunAdb(adbPath, injectArguments, json, out string injectFailure))
        {
            Debug.LogError(
                "[Quest LAN Setup] 개발용 LAN 설정을 주입하지 못했습니다. " +
                "Quest에 같은 package identifier의 debuggable Development APK가 설치되어 있는지 확인하세요. " +
                injectFailure);
            return;
        }

        Debug.Log(
            "[Quest LAN Setup] PASS: 주소와 토큰을 명령줄·로그에 노출하지 않고, " +
            $"중지된 개발 APK 내부에 일회성 설정을 주입했습니다 ({TycheTrainingTelemetryUploader.DescribeEndpoint(normalizedBaseUrl)}). " +
            "앱이 읽은 뒤 파일은 즉시 삭제됩니다. 이제 Quest에서 앱을 직접 시작하세요.");
    }

    static string ReadEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
            value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        return value?.Trim();
    }

    static bool RunAdb(
        string adbPath,
        string arguments,
        string standardInput,
        out string failure)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = adbPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = standardInput != null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        try
        {
            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                failure = "adb 프로세스를 시작하지 못했습니다.";
                return false;
            }
            if (standardInput != null)
            {
                process.StandardInput.Write(standardInput);
                process.StandardInput.Close();
            }

            if (!process.WaitForExit(15000))
            {
                failure = "adb 응답 시간이 15초를 초과했습니다.";
                return false;
            }

            string error = process.StandardError.ReadToEnd().Trim();
            string output = process.StandardOutput.ReadToEnd().Trim();
            if (process.ExitCode != 0)
            {
                failure = string.IsNullOrEmpty(error) ? output : error;
                return false;
            }

            failure = null;
            return true;
        }
        catch (Exception exception)
        {
            failure = exception.Message;
            return false;
        }
    }
}

/// <summary>
/// Alters only the generated Gradle manifest. The authored project manifest and
/// Release build policy remain unchanged.
/// </summary>
public sealed class QuestDevelopmentLanManifestBuildProcessor :
    IPreprocessBuildWithReport,
    IPostGenerateGradleAndroidProject
{
    const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    static bool developmentAndroidBuild;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        developmentAndroidBuild = report.summary.platform == BuildTarget.Android &&
            (report.summary.options & BuildOptions.Development) != 0;
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
            throw new BuildFailedException($"생성된 AndroidManifest.xml을 찾지 못했습니다: {manifestPath}");

        XmlDocument document = new();
        document.PreserveWhitespace = true;
        document.Load(manifestPath);
        XmlElement application = document.DocumentElement?["application"];
        if (application == null)
            throw new BuildFailedException("생성된 AndroidManifest.xml에 application 요소가 없습니다.");

        application.SetAttribute(
            "usesCleartextTraffic",
            AndroidNamespace,
            developmentAndroidBuild ? "true" : "false");
        document.Save(manifestPath);
        Debug.Log(
            developmentAndroidBuild
                ? "[Quest LAN Build] Development APK의 생성 Manifest에만 cleartext LAN을 허용했습니다."
                : "[Quest LAN Build] Release 생성 Manifest에서 cleartext를 명시적으로 차단했습니다.");
    }
}
