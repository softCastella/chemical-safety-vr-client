using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Uploads only schema-versioned JSONL records created by the separated
/// chemical-safety-vr-client project. The current authentication path is for
/// local Unity Editor integration testing and is never enabled in a player.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[AddComponentMenu("Tyche/Training/Training Telemetry Uploader")]
public sealed class TycheTrainingTelemetryUploader : MonoBehaviour
{
    public const string UploadTokenEnvironmentVariable = "TYCHE_TELEMETRY_UPLOAD_TOKEN";
    const string ExpectedSourceProject = "chemical-safety-vr-client";
    const string EditorUploadTokenFileName = ".editor-upload-token";
#if UNITY_EDITOR_WIN
    static readonly IntPtr HkeyCurrentUser = new(unchecked((int)0x80000001));
    const uint RegistryStringTypes = 0x00000002 | 0x00000004;

    [System.Runtime.InteropServices.DllImport(
        "advapi32.dll",
        CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int RegGetValue(
        IntPtr key,
        string subKey,
        string value,
        uint flags,
        out uint valueType,
        [System.Runtime.InteropServices.Out] StringBuilder data,
        ref uint dataSize);
#endif

    [Header("Local Unity Editor Test Only")]
    [SerializeField] private bool enableEditorTestUpload = true;
    [SerializeField] private string serverBaseUrl = "http://127.0.0.1:3000";
    [SerializeField, Range(1, 50)] private int batchSize = 25;
    [SerializeField, Min(1f)] private float scanIntervalSeconds = 5f;
    [SerializeField, Min(5f)] private float maximumRetrySeconds = 60f;
    [SerializeField, Min(1)] private int requestTimeoutSeconds = 15;

    [Serializable]
    sealed class LocalRecord
    {
        public int schemaVersion;
        public string sourceProject;
        public string sessionId;
        public string eventId;
        public int sequence;
        public string timestampUtc;
        public string eventType;
        public string appVersion;
        public string scene;
        public string mode;
        public string workPlan;
        public string flowState;
        public string itemType;
        public string itemName;
        public string condition;
        public string choice;
        public string result;
        public string note;
        public string metaProbeState;
        public string metaWelcomeState;
        public string metaAppScopedUserId;
        public string metaAgeCategory;
        public string requiredPpeCheck;
        public string missingRequiredPpe;
        public string audioClip;
        public float audioLengthSec;
        public float audioElapsedSec;
        public string attemptId;
        public string hand;
        public string inputControl;
        public int hoveredPpeCount;
        public string hoveredPpeItems;
        public string attemptOutcome;
        public float attemptElapsedSec;
    }

    [Serializable]
    sealed class SessionRequest
    {
        public int schemaVersion;
        public string sourceProject;
        public string clientInstanceId;
        public string metaUserId;
        public string sessionId;
        public string startedAtUtc;
        public string appVersion;
        public string scene;
        public string mode;
        public string workPlan;
    }

    [Serializable]
    sealed class UploadEvent
    {
        public int schemaVersion;
        public string sessionId;
        public string eventId;
        public int sequence;
        public string timestampUtc;
        public string eventType;
        public string appVersion;
        public string scene;
        public string mode;
        public string workPlan;
        public string flowState;
        public string itemType;
        public string itemName;
        public string condition;
        public string choice;
        public string result;
        public string note;
        public string metaProbeState;
        public string metaWelcomeState;
        public string requiredPpeCheck;
        public string missingRequiredPpe;
        public string audioClip;
        public float audioLengthSec;
        public float audioElapsedSec;
        public string attemptId;
        public string hand;
        public string inputControl;
        public int hoveredPpeCount;
        public string hoveredPpeItems;
        public string attemptOutcome;
        public float attemptElapsedSec;
    }

    [Serializable]
    sealed class EventBatchRequest
    {
        public UploadEvent[] events;
    }

    [Serializable]
    sealed class EventBatchResponse
    {
        public int accepted;
        public int duplicates;
        public int rejected;
        public int acceptedThroughSequence;
    }

    [Serializable]
    sealed class CompletionRequest
    {
        public int schemaVersion;
        public string endedAtUtc;
        public string reason;
    }

    [Serializable]
    sealed class UploadState
    {
        public string sessionId;
        public bool sessionCreated;
        public int acceptedThroughSequence;
        public bool completed;
    }

    static TycheTrainingTelemetryUploader instance;
    bool uploadFailedThisScan;
    string lastFailure;
    string clientInstanceId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        instance = null;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "[Tyche Telemetry Upload] 이미 활성 업로더가 있어 중복 컴포넌트를 비활성화합니다.",
                this);
            enabled = false;
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        if (!Application.isEditor)
        {
            Debug.Log(
                "[Tyche Telemetry Upload] 출시 Player 인증은 아직 연결되지 않아 업로드를 시작하지 않습니다.",
                this);
            yield break;
        }

        if (!enableEditorTestUpload)
            yield break;

        if (!TryGetLoopbackServerBaseUrl(out string normalizedBaseUrl))
        {
            ReportFailure(
                "serverBaseUrl은 로컬 Unity 테스트에서 http://127.0.0.1 또는 localhost 주소여야 합니다.");
            yield break;
        }

        string uploadToken = GetEditorUploadToken();
        if (string.IsNullOrEmpty(uploadToken))
        {
            ReportFailure(
                $"환경 변수 {UploadTokenEnvironmentVariable}가 없어 로컬 DB 업로드를 시작하지 않습니다.");
            yield break;
        }
        if (uploadToken.Length < 16 || uploadToken.Length > 512 ||
            uploadToken.Any(character => character < '!' || character > '~'))
        {
            ReportFailure(
                $"환경 변수 {UploadTokenEnvironmentVariable}는 16~512자의 공백 없는 ASCII여야 합니다.");
            yield break;
        }

        if (!TryGetOrCreateClientInstanceId(out clientInstanceId))
        {
            ReportFailure("새 클라이언트 자체 ID 파일을 만들 수 없어 업로드를 시작하지 않습니다.");
            yield break;
        }

        float retrySeconds = scanIntervalSeconds;
        while (enabled)
        {
            uploadFailedThisScan = false;
            yield return UploadPendingFiles(normalizedBaseUrl, uploadToken);
            retrySeconds = uploadFailedThisScan
                ? Mathf.Min(maximumRetrySeconds, Mathf.Max(scanIntervalSeconds, retrySeconds * 2f))
                : scanIntervalSeconds;
            yield return new WaitForSecondsRealtime(retrySeconds);
        }
    }

    bool TryGetLoopbackServerBaseUrl(out string normalizedBaseUrl)
    {
        normalizedBaseUrl = null;
        if (!Uri.TryCreate(serverBaseUrl?.TrimEnd('/'), UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !uri.IsLoopback)
        {
            return false;
        }

        normalizedBaseUrl = uri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    static string GetEditorUploadToken()
    {
        string uploadToken = Environment.GetEnvironmentVariable(
            UploadTokenEnvironmentVariable);
#if UNITY_EDITOR_WIN
        if (string.IsNullOrEmpty(uploadToken))
        {
            uploadToken = Environment.GetEnvironmentVariable(
                UploadTokenEnvironmentVariable,
                EnvironmentVariableTarget.User);
        }
        if (string.IsNullOrEmpty(uploadToken))
        {
            // Unity/Hub can retain an environment snapshot from before the user
            // variable was created. Read the current-user value directly so a
            // full Windows sign-out is not required for local Editor testing.
            uploadToken = ReadCurrentUserEnvironmentVariable(
                UploadTokenEnvironmentVariable);
        }
#endif
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(uploadToken))
            uploadToken = ReadEditorUploadTokenFile();
#endif
        return uploadToken;
    }

#if UNITY_EDITOR
    static string ReadEditorUploadTokenFile()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            "tyche-training-telemetry",
            EditorUploadTokenFileName);
        try
        {
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
#endif

#if UNITY_EDITOR_WIN
    static string ReadCurrentUserEnvironmentVariable(string name)
    {
        const int Success = 0;
        StringBuilder value = new(513);
        uint sizeInBytes = (uint)(value.Capacity * sizeof(char));
        int result = RegGetValue(
            HkeyCurrentUser,
            "Environment",
            name,
            RegistryStringTypes,
            out _,
            value,
            ref sizeInBytes);
        return result == Success ? value.ToString() : null;
    }
#endif

    IEnumerator UploadPendingFiles(string baseUrl, string token)
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "tyche-training-telemetry");
        if (!Directory.Exists(directory))
            yield break;

        string[] files;
        try
        {
            files = Directory.GetFiles(directory, "session-*.jsonl")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception)
        {
            ReportFailure($"텔레메트리 폴더를 열 수 없습니다: {exception.Message}");
            uploadFailedThisScan = true;
            yield break;
        }

        foreach (string path in files)
        {
            if (!TryReadNewClientRecords(path, out List<LocalRecord> records))
                continue;

            yield return UploadFile(baseUrl, token, path, records);
            if (uploadFailedThisScan)
                yield break;
        }
    }

    bool TryReadNewClientRecords(string path, out List<LocalRecord> records)
    {
        records = new List<LocalRecord>();
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path, Encoding.UTF8);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            ReportFailure($"JSONL 파일을 읽을 권한이 없습니다: {exception.Message}");
            uploadFailedThisScan = true;
            return false;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            LocalRecord record;
            try
            {
                record = JsonUtility.FromJson<LocalRecord>(line);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (record == null)
                return false;
            records.Add(record);
        }

        if (records.Count == 0)
            return false;

        LocalRecord first = records[0];
        if (first.schemaVersion != PPETrainingTelemetryCapture.CurrentSchemaVersion ||
            first.sourceProject != ExpectedSourceProject ||
            first.eventType != "session_started" ||
            string.IsNullOrEmpty(first.sessionId))
        {
            // Old monorepo JSONL intentionally has no schema/source marker and is ignored.
            records.Clear();
            return false;
        }

        for (int index = 0; index < records.Count; index++)
        {
            LocalRecord record = records[index];
            int expectedSequence = index + 1;
            string expectedEventId = $"{first.sessionId}:{expectedSequence:D8}";
            if (record.schemaVersion != first.schemaVersion ||
                record.sourceProject != ExpectedSourceProject ||
                record.sessionId != first.sessionId ||
                record.sequence != expectedSequence ||
                record.eventId != expectedEventId ||
                string.IsNullOrEmpty(record.timestampUtc) ||
                string.IsNullOrEmpty(record.eventType))
            {
                ReportFailure(
                    $"새 클라이언트 JSONL 계약이 일치하지 않습니다: {Path.GetFileName(path)}, sequence={expectedSequence}");
                uploadFailedThisScan = true;
                records.Clear();
                return false;
            }
        }

        return true;
    }

    IEnumerator UploadFile(
        string baseUrl,
        string token,
        string jsonlPath,
        List<LocalRecord> records)
    {
        LocalRecord first = records[0];
        if (!TryResolveMetaIdentity(records, out string metaUserId))
            yield break;
        string statePath = jsonlPath + ".upload-state.json";
        UploadState state = LoadState(statePath, first.sessionId);
        if (state.completed)
            yield break;

        if (!state.sessionCreated)
        {
            SessionRequest session = new()
            {
                schemaVersion = first.schemaVersion,
                sourceProject = ExpectedSourceProject,
                clientInstanceId = clientInstanceId,
                metaUserId = metaUserId,
                sessionId = first.sessionId,
                startedAtUtc = first.timestampUtc,
                appVersion = first.appVersion,
                scene = first.scene,
                mode = first.mode,
                workPlan = first.workPlan,
            };
            bool created = false;
            yield return SendJson(
                baseUrl + "/api/training-telemetry/sessions",
                token,
                JsonUtility.ToJson(session),
                (success, _) => created = success);
            if (!created)
            {
                uploadFailedThisScan = true;
                yield break;
            }
            state.sessionCreated = true;
            SaveState(statePath, state);
        }

        while (state.acceptedThroughSequence < records.Count)
        {
            UploadEvent[] batch = records
                .Where(record => record.sequence > state.acceptedThroughSequence)
                .Take(Mathf.Clamp(batchSize, 1, 50))
                .Select(ToUploadEvent)
                .ToArray();
            if (batch.Length == 0)
                break;

            EventBatchResponse response = null;
            yield return SendJson(
                $"{baseUrl}/api/training-telemetry/sessions/{UnityWebRequest.EscapeURL(first.sessionId)}/events",
                token,
                JsonUtility.ToJson(new EventBatchRequest { events = batch }),
                (success, body) =>
                {
                    if (success)
                        response = JsonUtility.FromJson<EventBatchResponse>(body);
                });
            if (response == null || response.rejected != 0 ||
                response.acceptedThroughSequence <= state.acceptedThroughSequence)
            {
                ReportFailure(
                    $"서버 ACK가 진행되지 않았습니다: session={first.sessionId}, sequence={state.acceptedThroughSequence}");
                uploadFailedThisScan = true;
                yield break;
            }

            state.acceptedThroughSequence = response.acceptedThroughSequence;
            SaveState(statePath, state);
        }

        LocalRecord ended = records.LastOrDefault(record => record.eventType == "session_ended");
        if (ended != null && state.acceptedThroughSequence >= ended.sequence)
        {
            bool completed = false;
            CompletionRequest completion = new()
            {
                schemaVersion = ended.schemaVersion,
                endedAtUtc = ended.timestampUtc,
                reason = string.IsNullOrEmpty(ended.note) ? "application_quitting" : ended.note,
            };
            yield return SendJson(
                $"{baseUrl}/api/training-telemetry/sessions/{UnityWebRequest.EscapeURL(first.sessionId)}/complete",
                token,
                JsonUtility.ToJson(completion),
                (success, _) => completed = success);
            if (!completed)
            {
                uploadFailedThisScan = true;
                yield break;
            }
            state.completed = true;
            SaveState(statePath, state);
        }

        if (lastFailure != null)
        {
            Debug.Log(
                $"[Tyche Telemetry Upload] 서버 저장이 복구되었습니다. session={first.sessionId}, " +
                $"acceptedThrough={state.acceptedThroughSequence}",
                this);
            lastFailure = null;
        }
    }

    IEnumerator SendJson(
        string url,
        string token,
        string json,
        Action<bool, string> completed)
    {
        using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Max(1, requestTimeoutSeconds);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 && request.responseCode < 300;
        string body = request.downloadHandler?.text ?? "";
        if (!success)
        {
            ReportFailure(
                $"POST {url} 실패: HTTP {request.responseCode}, {request.error}, {body}");
        }
        completed(success, body);
    }

    static UploadEvent ToUploadEvent(LocalRecord record)
    {
        return new UploadEvent
        {
            schemaVersion = record.schemaVersion,
            sessionId = record.sessionId,
            eventId = record.eventId,
            sequence = record.sequence,
            timestampUtc = record.timestampUtc,
            eventType = record.eventType,
            appVersion = record.appVersion,
            scene = record.scene,
            mode = record.mode,
            workPlan = record.workPlan,
            flowState = record.flowState,
            itemType = record.itemType,
            itemName = record.itemName,
            condition = record.condition,
            choice = record.choice,
            result = record.result,
            note = SanitizeUploadNote(record.note),
            metaProbeState = record.metaProbeState,
            metaWelcomeState = record.metaWelcomeState,
            requiredPpeCheck = record.requiredPpeCheck,
            missingRequiredPpe = record.missingRequiredPpe,
            audioClip = record.audioClip,
            audioLengthSec = record.audioLengthSec,
            audioElapsedSec = record.audioElapsedSec,
            attemptId = record.attemptId,
            hand = record.hand,
            inputControl = record.inputControl,
            hoveredPpeCount = record.hoveredPpeCount,
            hoveredPpeItems = record.hoveredPpeItems,
            attemptOutcome = record.attemptOutcome,
            attemptElapsedSec = record.attemptElapsedSec,
        };
    }

    static string SanitizeUploadNote(string note)
    {
        if (string.IsNullOrEmpty(note))
            return null;
        return note.IndexOf(":\\", StringComparison.Ordinal) >= 0 ||
            note.IndexOf(":/", StringComparison.Ordinal) >= 0
            ? "local_path_redacted"
            : note;
    }

    static bool TryResolveMetaIdentity(
        IEnumerable<LocalRecord> records,
        out string metaUserId)
    {
        metaUserId = records
            .Select(record => record.metaAppScopedUserId)
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));
        if (!string.IsNullOrEmpty(metaUserId))
            return true;

        return records.Any(record =>
            record.metaProbeState == "SkippedForEditorTesting" ||
            record.metaProbeState == "Completed" ||
            record.metaProbeState == "CompletedWithoutAgeCategory" ||
            record.metaProbeState == "Failed" ||
            record.eventType == "session_ended");
    }

    static bool TryGetOrCreateClientInstanceId(out string value)
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "tyche-training-telemetry");
        string path = Path.Combine(directory, "client-instance-id.txt");
        try
        {
            Directory.CreateDirectory(directory);
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (existing.Length == 32 && existing.All(IsLowerHex))
                {
                    value = existing;
                    return true;
                }
            }

            value = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, value, new UTF8Encoding(false));
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }

    static bool IsLowerHex(char character)
    {
        return (character >= '0' && character <= '9') ||
            (character >= 'a' && character <= 'f');
    }

    static UploadState LoadState(string path, string sessionId)
    {
        if (!File.Exists(path))
            return new UploadState { sessionId = sessionId };
        try
        {
            UploadState state = JsonUtility.FromJson<UploadState>(
                File.ReadAllText(path, Encoding.UTF8));
            return state != null && state.sessionId == sessionId
                ? state
                : new UploadState { sessionId = sessionId };
        }
        catch (Exception)
        {
            // Server writes are idempotent, so a broken local ACK file safely restarts at sequence zero.
            return new UploadState { sessionId = sessionId };
        }
    }

    static void SaveState(string path, UploadState state)
    {
        string temporaryPath = path + ".tmp";
        File.WriteAllText(
            temporaryPath,
            JsonUtility.ToJson(state),
            new UTF8Encoding(false));
        if (File.Exists(path))
            File.Delete(path);
        File.Move(temporaryPath, path);
    }

    void ReportFailure(string message)
    {
        if (lastFailure == message)
            return;
        lastFailure = message;
        Debug.LogWarning($"[Tyche Telemetry Upload] {message}", this);
    }
}
