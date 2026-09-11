using System;
using System.Collections;
using System.Text;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Exchanges the current Meta app-scoped user ID and one-time User Proof for a
/// short-lived Tyche upload token. Secrets remain on the server.
/// </summary>
public sealed class TycheMetaSessionAuthenticator
{
    const float IdentityTimeoutSeconds = 180f;
    const float TokenRefreshMarginSeconds = 60f;

    readonly string serverBaseUrl;
    readonly int requestTimeoutSeconds;
    Authorization cachedAuthorization;
    float cachedAuthorizationExpiresAt;

    [Serializable]
    sealed class MetaAuthenticationRequest
    {
        public string metaUserId;
        public string userProof;
    }

    [Serializable]
    sealed class MetaAuthenticationResponse
    {
        public string accessToken;
        public string tokenType;
        public int expiresIn;
        public string metaUserId;
    }

    [Serializable]
    sealed class MetaAuthenticationEnvelope
    {
        public MetaAuthenticationResponse data;
    }

    public sealed class Authorization
    {
        public string ServerBaseUrl { get; }
        public string AccessToken { get; }
        public string MetaUserId { get; }

        public Authorization(string serverBaseUrl, string accessToken, string metaUserId)
        {
            ServerBaseUrl = serverBaseUrl;
            AccessToken = accessToken;
            MetaUserId = metaUserId;
        }
    }

    public TycheMetaSessionAuthenticator(string serverBaseUrl, int requestTimeoutSeconds)
    {
        if (!TryNormalizeProductionServerBaseUrl(
                serverBaseUrl,
                out this.serverBaseUrl,
                out string failure))
        {
            throw new ArgumentException(failure, nameof(serverBaseUrl));
        }
        this.requestTimeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
    }

    public IEnumerator AcquireAuthorization(Action<Authorization, string> completed)
    {
        if (cachedAuthorization != null &&
            Time.realtimeSinceStartup < cachedAuthorizationExpiresAt - TokenRefreshMarginSeconds)
        {
            completed(cachedAuthorization, null);
            yield break;
        }

        float identityDeadline = Time.realtimeSinceStartup + IdentityTimeoutSeconds;
        while (MetaPlatformIdentityProbe.CurrentAppScopedUserId == 0 &&
               !IsTerminalIdentityState(MetaPlatformIdentityProbe.CurrentState) &&
               Time.realtimeSinceStartup < identityDeadline)
        {
            yield return null;
        }

        ulong metaUserIdValue = MetaPlatformIdentityProbe.CurrentAppScopedUserId;
        if (metaUserIdValue == 0)
        {
            completed(
                null,
                $"Meta 앱 범위 사용자 ID를 확인하지 못했습니다. State={MetaPlatformIdentityProbe.CurrentState}");
            yield break;
        }

        bool proofCompleted = false;
        string userProof = null;
        string proofFailure = null;
        try
        {
            Users.GetUserProof().OnComplete(message =>
            {
                if (message == null || message.IsError || message.Data == null ||
                    string.IsNullOrEmpty(message.Data.Value))
                {
                    proofFailure = DescribeMetaError(message);
                }
                else
                {
                    userProof = message.Data.Value;
                }
                proofCompleted = true;
            });
        }
        catch (Exception exception)
        {
            completed(null, $"Meta User Proof 요청 실패: {exception.GetType().Name}");
            yield break;
        }

        float proofDeadline = Time.realtimeSinceStartup + IdentityTimeoutSeconds;
        while (!proofCompleted && Time.realtimeSinceStartup < proofDeadline)
            yield return null;
        if (!proofCompleted || string.IsNullOrEmpty(userProof))
        {
            completed(
                null,
                proofCompleted
                    ? $"Meta User Proof 응답 실패: {proofFailure}"
                    : "Meta User Proof 응답 시간이 초과되었습니다.");
            yield break;
        }

        string metaUserId = metaUserIdValue.ToString();
        MetaAuthenticationRequest payload = new()
        {
            metaUserId = metaUserId,
            userProof = userProof,
        };
        using UnityWebRequest request = new(
            serverBaseUrl + "/api/training-telemetry/auth/meta",
            UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = requestTimeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success ||
            request.responseCode < 200 || request.responseCode >= 300)
        {
            completed(
                null,
                $"Release 인증 실패: HTTP {request.responseCode}, {request.error}");
            yield break;
        }

        MetaAuthenticationEnvelope envelope;
        try
        {
            envelope = JsonUtility.FromJson<MetaAuthenticationEnvelope>(
                request.downloadHandler?.text ?? string.Empty);
        }
        catch (ArgumentException)
        {
            envelope = null;
        }
        MetaAuthenticationResponse response = envelope?.data;
        if (response == null ||
            response.tokenType != "Bearer" ||
            response.metaUserId != metaUserId ||
            response.expiresIn < 60 || response.expiresIn > 3600 ||
            string.IsNullOrEmpty(response.accessToken) ||
            response.accessToken.Length > 2048)
        {
            completed(null, "Release 인증 서버 응답 계약이 일치하지 않습니다.");
            yield break;
        }

        cachedAuthorization = new Authorization(
            serverBaseUrl,
            response.accessToken,
            response.metaUserId);
        cachedAuthorizationExpiresAt = Time.realtimeSinceStartup + response.expiresIn;
        completed(cachedAuthorization, null);
    }

    public void InvalidateAuthorization()
    {
        cachedAuthorization = null;
        cachedAuthorizationExpiresAt = 0f;
    }

    public static bool TryNormalizeProductionServerBaseUrl(
        string value,
        out string normalizedBaseUrl,
        out string failure)
    {
        normalizedBaseUrl = null;
        failure = null;
        if (!Uri.TryCreate(value?.TrimEnd('/'), UriKind.Absolute, out Uri uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/" ||
            !uri.IsDefaultPort)
        {
            failure = "Release 텔레메트리 서버는 기본 443 포트의 공개 HTTPS 루트 주소여야 합니다.";
            return false;
        }

        normalizedBaseUrl = uri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    static bool IsTerminalIdentityState(MetaPlatformIdentityProbe.ProbeState state)
    {
        return state == MetaPlatformIdentityProbe.ProbeState.Completed ||
            state == MetaPlatformIdentityProbe.ProbeState.SkippedForEditorTesting ||
            state == MetaPlatformIdentityProbe.ProbeState.Failed;
    }

    static string DescribeMetaError(Message<UserProof> message)
    {
        if (message == null)
            return "Meta SDK 응답 없음";
        Error error = message.GetError();
        return error == null
            ? $"StatusCode={message.status.Code()}"
            : $"Code={error.Code}, HttpCode={error.HttpCode}";
    }
}
