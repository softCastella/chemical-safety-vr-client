using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Development-only proof that a real Meta app-scoped user ID and the current
/// PPE session snapshot can round-trip through the local Express server.
/// It does not create UI or alter the training flow.
/// </summary>
public sealed class TycheLocalTrainingRegistrationClient : MonoBehaviour
{
    const string DefaultLocalServerBaseUrl = "http://127.0.0.1:3000";
    const string LocalServerBaseUrlPlayerPrefsKey = "Tyche.LocalServerBaseUrl";
    const float IdentityTimeoutSeconds = 180f;

    static TycheLocalTrainingRegistrationClient instance;

    [Serializable]
    sealed class RegistrationRequest
    {
        public string metaUserId;
        public string metaAgeCategory;
        public string sessionId;
        public string timestampUtc;
        public string scene;
        public string mode;
        public string workPlan;
        public string flowState;
    }

    [Serializable]
    sealed class RegistrationData
    {
        public string metaUserId;
        public string metaAgeCategory;
        public string sessionId;
        public string timestampUtc;
        public string scene;
        public string mode;
        public string workPlan;
        public string flowState;
        public string serverReceivedAt;
    }

    [Serializable]
    sealed class RegistrationResponse
    {
        public string message;
        public RegistrationData data;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (instance != null)
            return;

        MetaPlatformIdentityProbe identityProbe =
            UnityEngine.Object.FindAnyObjectByType<MetaPlatformIdentityProbe>(
                FindObjectsInactive.Include);
        if (identityProbe == null || !identityProbe.isActiveAndEnabled ||
            !identityProbe.UsesPlatformSdkForCurrentRun)
            return;

        GameObject root = new("Tyche Local Training Registration Client");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<TycheLocalTrainingRegistrationClient>();
#endif
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    IEnumerator Start()
    {
        float identityDeadline = Time.realtimeSinceStartup + IdentityTimeoutSeconds;
        while (MetaPlatformIdentityProbe.CurrentAppScopedUserId == 0)
        {
            MetaPlatformIdentityProbe identityProbe =
                FindAnyObjectByType<MetaPlatformIdentityProbe>(FindObjectsInactive.Include);
            if (identityProbe == null || !identityProbe.UsesPlatformSdkForCurrentRun ||
                identityProbe.State == MetaPlatformIdentityProbe.ProbeState.Failed ||
                identityProbe.State == MetaPlatformIdentityProbe.ProbeState.SkippedForEditorTesting)
            {
                Debug.LogWarning(
                    "[Tyche Local Registration] 유효한 Meta 계정 식별 경로가 없어 개발용 서버 등록을 건너뜁니다. " +
                    "PPE 로컬 텔레메트리는 계속 기록됩니다.",
                    this);
                yield break;
            }

            if (Time.realtimeSinceStartup >= identityDeadline)
            {
                Debug.LogWarning(
                    "[Tyche Local Registration] Meta ID를 제한 시간 안에 확인하지 못했습니다. " +
                    "PPE 로컬 텔레메트리는 계속 기록됩니다.",
                    this);
                yield break;
            }
            yield return null;
        }

        Debug.Log(
            "[Tyche Local Registration] Meta ID 확인 완료. " +
            "PPE 활성 세션 진입을 앱 수명 동안 기다립니다.",
            this);

        while (!PPETrainingTelemetryCapture.HasActivePpeModeSession ||
            string.IsNullOrEmpty(PPETrainingTelemetryCapture.CurrentSessionId))
        {
            MetaPlatformIdentityProbe identityProbe =
                FindAnyObjectByType<MetaPlatformIdentityProbe>(FindObjectsInactive.Include);
            if (identityProbe == null || !identityProbe.UsesPlatformSdkForCurrentRun ||
                identityProbe.State == MetaPlatformIdentityProbe.ProbeState.Failed ||
                identityProbe.State == MetaPlatformIdentityProbe.ProbeState.SkippedForEditorTesting)
            {
                Debug.LogWarning(
                    "[Tyche Local Registration] PPE 세션 대기 중 Meta 계정 식별 경로가 종료되어 " +
                    "개발용 서버 등록을 건너뜁니다. PPE 로컬 텔레메트리는 계속 기록됩니다.",
                    this);
                yield break;
            }

            yield return null;
        }

        if (!TryResolveServerBaseUrl(out string baseUrl, out string configurationFailure))
        {
            Debug.LogWarning(
                $"[Tyche Local Registration] {configurationFailure} PPE 로컬 텔레메트리는 계속 기록됩니다.",
                this);
            yield break;
        }

        RegistrationRequest payload = BuildRequest();
        string collectionUrl = $"{baseUrl}/api/training-registrations";

        RegistrationResponse created = null;
        yield return SendJson(
            collectionUrl,
            UnityWebRequest.kHttpVerbPOST,
            JsonUtility.ToJson(payload),
            response => created = response);

        if (!IsMatching(created?.data, payload))
        {
            Debug.LogError(
                "[Tyche Local Registration] 서버 등록 응답이 전송한 Meta ID 또는 sessionId와 일치하지 않습니다.",
                this);
            yield break;
        }

        RegistrationResponse readBack = null;
        string readUrl = $"{collectionUrl}/{UnityWebRequest.EscapeURL(payload.sessionId)}";
        yield return SendJson(
            readUrl,
            UnityWebRequest.kHttpVerbGET,
            null,
            response => readBack = response);

        if (!IsMatching(readBack?.data, payload))
        {
            Debug.LogError(
                "[Tyche Local Registration] 서버 조회 결과가 전송한 Meta ID 또는 sessionId와 일치하지 않습니다.",
                this);
            yield break;
        }

        Debug.Log("가입이 완료되었습니다.");
    }

    static bool TryResolveServerBaseUrl(out string baseUrl, out string failure)
    {
#if UNITY_EDITOR
        string editorValue = PlayerPrefs
            .GetString(LocalServerBaseUrlPlayerPrefsKey, DefaultLocalServerBaseUrl)
            .TrimEnd('/');
        if (!Uri.TryCreate(editorValue, UriKind.Absolute, out Uri editorUri) ||
            (editorUri.Scheme != Uri.UriSchemeHttp && editorUri.Scheme != Uri.UriSchemeHttps) ||
            !editorUri.IsLoopback)
        {
            baseUrl = null;
            failure = "Editor 등록 서버 주소는 loopback HTTP(S)여야 합니다.";
            return false;
        }

        baseUrl = editorUri.AbsoluteUri.TrimEnd('/');
        failure = null;
        return true;
#elif UNITY_ANDROID && DEVELOPMENT_BUILD
        return TycheTrainingTelemetryUploader.TryGetAndroidDevelopmentLanConfiguration(
            out baseUrl,
            out _,
            out failure);
#else
        baseUrl = null;
        failure = "로컬 LAN 등록은 Unity Editor 또는 Android Development Build에서만 활성화됩니다.";
        return false;
#endif
    }

    static RegistrationRequest BuildRequest()
    {
        return new RegistrationRequest
        {
            metaUserId = MetaPlatformIdentityProbe.CurrentAppScopedUserId.ToString(),
            metaAgeCategory = MetaPlatformIdentityProbe.CurrentAgeCategory.ToString(),
            sessionId = PPETrainingTelemetryCapture.CurrentSessionId,
            timestampUtc = DateTime.UtcNow.ToString("O"),
            scene = PPETrainingTelemetryCapture.CurrentScenePath,
            mode = PPETrainingTelemetryCapture.CurrentMode,
            workPlan = PPETrainingTelemetryCapture.CurrentWorkPlan,
            flowState = PPETrainingTelemetryCapture.CurrentFlowState,
        };
    }

    IEnumerator SendJson(
        string url,
        string method,
        string json,
        Action<RegistrationResponse> onSuccess)
    {
        using UnityWebRequest request = new(url, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 15;
        if (json != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.SetRequestHeader("Content-Type", "application/json");
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[Tyche Local Registration] {method} {DescribeRequest(url)} 실패: " +
                $"HTTP {request.responseCode}, {request.error}",
                this);
            yield break;
        }

        RegistrationResponse response = JsonUtility.FromJson<RegistrationResponse>(request.downloadHandler.text);
        if (response == null || response.data == null)
        {
            Debug.LogError(
                $"[Tyche Local Registration] {method} {DescribeRequest(url)} 응답 JSON을 읽지 못했습니다.",
                this);
            yield break;
        }

        onSuccess?.Invoke(response);
    }

    static string DescribeRequest(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
            ? uri.AbsolutePath
            : "invalid_request_path";
    }

    static bool IsMatching(RegistrationData data, RegistrationRequest request)
    {
        return data != null &&
            string.Equals(data.metaUserId, request.metaUserId, StringComparison.Ordinal) &&
            string.Equals(data.sessionId, request.sessionId, StringComparison.Ordinal);
    }
}
