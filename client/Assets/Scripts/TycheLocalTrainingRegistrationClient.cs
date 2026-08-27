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
    const float IdentityAndSessionTimeoutSeconds = 180f;

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
            UnityEngine.Object.FindFirstObjectByType<MetaPlatformIdentityProbe>(
                FindObjectsInactive.Include);
        if (identityProbe == null || !identityProbe.isActiveAndEnabled)
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
        float deadline = Time.realtimeSinceStartup + IdentityAndSessionTimeoutSeconds;
        while (!CanSendRegistration())
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Debug.LogError(
                    "[Tyche Local Registration] Meta ID 또는 활성 PPE 세션을 제한 시간 안에 확인하지 못했습니다.",
                    this);
                yield break;
            }
            yield return null;
        }

        RegistrationRequest payload = BuildRequest();
        string baseUrl = PlayerPrefs
            .GetString(LocalServerBaseUrlPlayerPrefsKey, DefaultLocalServerBaseUrl)
            .TrimEnd('/');
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

    static bool CanSendRegistration()
    {
        return MetaPlatformIdentityProbe.CurrentAppScopedUserId != 0 &&
            !string.IsNullOrEmpty(PPETrainingTelemetryCapture.CurrentSessionId) &&
            PPETrainingTelemetryCapture.HasActivePpeModeSession;
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
        if (json != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.SetRequestHeader("Content-Type", "application/json");
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[Tyche Local Registration] {method} {url} 실패: " +
                $"HTTP {request.responseCode}, {request.error}, {request.downloadHandler?.text}",
                this);
            yield break;
        }

        RegistrationResponse response = JsonUtility.FromJson<RegistrationResponse>(request.downloadHandler.text);
        if (response == null || response.data == null)
        {
            Debug.LogError(
                $"[Tyche Local Registration] {method} {url} 응답 JSON을 읽지 못했습니다: {request.downloadHandler.text}",
                this);
            yield break;
        }

        onSuccess?.Invoke(response);
    }

    static bool IsMatching(RegistrationData data, RegistrationRequest request)
    {
        return data != null &&
            string.Equals(data.metaUserId, request.metaUserId, StringComparison.Ordinal) &&
            string.Equals(data.sessionId, request.sessionId, StringComparison.Ordinal);
    }
}
