using System;
using System.Security.Cryptography;
using System.Text;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Tyche/Diagnostics/Meta Platform Identity Probe")]
public sealed class MetaPlatformIdentityProbe : MonoBehaviour
{
    public enum AccountWelcomeState
    {
        Unknown,
        FirstVisit,
        Returning
    }

    public enum ProbeState
    {
        Idle,
        Initializing,
        CheckingEntitlement,
        LoadingUser,
        LoadingAgeCategory,
        Completed,
        CompletedWithoutAgeCategory,
        SkippedForEditorTesting,
        Failed
    }

    [Header("Execution")]
    [SerializeField] private bool runOnStart = true;

    [Header("Editor Testing")]
    [Tooltip("Editor에서 Meta Platform SDK와 실제 테스트 계정을 검증할 때만 켭니다. 끄면 Game View는 Meta ID 없이 진행합니다.")]
    [SerializeField] private bool useMetaPlatformSdkInEditor;

    [Header("Diagnostics")]
    [Tooltip("개발 진단에서만 사용합니다. 운영 빌드 전에는 끄거나 이 컴포넌트를 제거하세요.")]
    [SerializeField] private bool logAppScopedUserId = true;

    public ProbeState State { get; private set; } = ProbeState.Idle;
    public ulong AppScopedUserId { get; private set; }
    public AccountAgeCategory AgeCategory { get; private set; } = AccountAgeCategory.Unknown;
    public bool UsesPlatformSdkForCurrentRun => !UnityEngine.Application.isEditor || useMetaPlatformSdkInEditor;

    public static ulong CurrentAppScopedUserId { get; private set; }
    public static AccountAgeCategory CurrentAgeCategory { get; private set; } = AccountAgeCategory.Unknown;
    public static AccountWelcomeState CurrentWelcomeState { get; private set; } =
        AccountWelcomeState.Unknown;
    public static bool IsIdentityRequestInFlight =>
        s_Instance != null && s_Instance._requestInFlight;

    private const string WelcomeSeenKeyPrefix = "Tyche.MetaWelcomeSeen.";
    private static MetaPlatformIdentityProbe s_Instance;
    private bool _requestInFlight;
    private bool _acceptCallbacks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_Instance = null;
        CurrentAppScopedUserId = 0;
        CurrentAgeCategory = AccountAgeCategory.Unknown;
        CurrentWelcomeState = AccountWelcomeState.Unknown;
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Debug.LogWarning(
                "[Meta Identity Probe] A persistent probe already owns the Meta identity session. " +
                "This duplicate probe will remain inactive.",
                this);
            enabled = false;
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (runOnStart)
            RunProbe();
    }

    private void OnDisable()
    {
        _acceptCallbacks = false;
        _requestInFlight = false;
    }

    [ContextMenu("Run Meta Platform Identity Probe")]
    public void RunProbe()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[Meta Identity Probe] 활성화된 컴포넌트에서만 진단을 실행할 수 있습니다.", this);
            return;
        }

        if (_requestInFlight)
        {
            Debug.LogWarning($"[Meta Identity Probe] 이미 진단 중입니다. State={State}", this);
            return;
        }

        AppScopedUserId = 0;
        AgeCategory = AccountAgeCategory.Unknown;
        CurrentAppScopedUserId = 0;
        CurrentWelcomeState = AccountWelcomeState.Unknown;

        if (!UsesPlatformSdkForCurrentRun)
        {
            State = ProbeState.SkippedForEditorTesting;
            _requestInFlight = false;
            _acceptCallbacks = false;
            Debug.Log(
                "[Meta Identity Probe] Editor SDKless 테스트 모드입니다. " +
                "Meta ID 없이 Game View 흐름과 로컬 익명 텔레메트리를 계속합니다.",
                this);
            return;
        }

        _requestInFlight = true;
        _acceptCallbacks = true;

        if (Core.IsInitialized())
        {
            Debug.Log("[Meta Identity Probe] Platform SDK가 이미 초기화되어 있습니다.", this);
            CheckEntitlement();
            return;
        }

        State = ProbeState.Initializing;
        Debug.Log("[Meta Identity Probe] Platform SDK 초기화를 시작합니다.", this);

        try
        {
            Core.AsyncInitialize().OnComplete(OnPlatformInitialized);
        }
        catch (Exception exception)
        {
            Fail("Platform SDK 초기화 요청", exception.Message);
        }
    }

    private void OnPlatformInitialized(Message<PlatformInitialize> message)
    {
        if (!CanHandleCallback())
            return;

        if (message == null || message.IsError || message.Data == null ||
            message.Data.Result != PlatformInitializeResult.Success)
        {
            string result = message?.Data == null ? "NoResult" : message.Data.Result.ToString();
            Fail("Platform SDK 초기화", $"Result={result}, {DescribeError(message)}");
            return;
        }

        Debug.Log("[Meta Identity Probe] Platform SDK 초기화 성공.", this);
        CheckEntitlement();
    }

    private void CheckEntitlement()
    {
        State = ProbeState.CheckingEntitlement;
        Debug.Log("[Meta Identity Probe] 앱 entitlement를 확인합니다.", this);

        try
        {
            Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlementChecked);
        }
        catch (Exception exception)
        {
            Fail("entitlement 확인 요청", exception.Message);
        }
    }

    private void OnEntitlementChecked(Message message)
    {
        if (!CanHandleCallback())
            return;

        if (message == null || message.IsError)
        {
            Fail("entitlement 확인", DescribeError(message));
            return;
        }

        Debug.Log("[Meta Identity Probe] 앱 entitlement 확인 성공.", this);
        LoadLoggedInUser();
    }

    private void LoadLoggedInUser()
    {
        State = ProbeState.LoadingUser;
        Debug.Log("[Meta Identity Probe] 로그인한 Meta 사용자의 앱 범위 ID를 조회합니다.", this);

        try
        {
            Users.GetLoggedInUser().OnComplete(OnLoggedInUserLoaded);
        }
        catch (Exception exception)
        {
            Fail("사용자 ID 조회 요청", exception.Message);
        }
    }

    private void OnLoggedInUserLoaded(Message<User> message)
    {
        if (!CanHandleCallback())
            return;

        if (message == null || message.IsError || message.Data == null)
        {
            Fail("사용자 ID 조회", DescribeError(message));
            return;
        }

        if (message.Data.ID == 0)
        {
            Fail("사용자 ID 조회", "Meta Platform SDK가 유효한 앱 범위 사용자 ID를 반환하지 않았습니다.");
            return;
        }

        AppScopedUserId = message.Data.ID;
        CurrentAppScopedUserId = AppScopedUserId;
        CurrentWelcomeState = PlayerPrefs.GetInt(BuildWelcomeSeenKey(AppScopedUserId), 0) == 1
            ? AccountWelcomeState.Returning
            : AccountWelcomeState.FirstVisit;
        string userIdResult = logAppScopedUserId ? AppScopedUserId.ToString() : "<redacted>";
        Debug.Log($"[Meta Identity Probe] 앱 범위 사용자 ID 조회 성공. UserId={userIdResult}", this);
        LoadAgeCategory();
    }

    private void LoadAgeCategory()
    {
        State = ProbeState.LoadingAgeCategory;
        Debug.Log("[Meta Identity Probe] 사용자 연령대를 조회합니다.", this);

        try
        {
            UserAgeCategory.Get().OnComplete(OnAgeCategoryLoaded);
        }
        catch (Exception exception)
        {
            CompleteWithoutAgeCategory($"연령대 조회 요청 예외: {exception.Message}");
        }
    }

    private void OnAgeCategoryLoaded(Message<UserAccountAgeCategory> message)
    {
        if (!CanHandleCallback())
            return;

        if (message == null || message.IsError || message.Data == null)
        {
            CompleteWithoutAgeCategory($"연령대 조회 실패: {DescribeError(message)}");
            return;
        }

        AgeCategory = message.Data.AgeCategory;
        CurrentAgeCategory = AgeCategory;
        State = ProbeState.Completed;
        _requestInFlight = false;
        Debug.Log($"[Meta Identity Probe] 1차 사용자 식별 진단 완료. AgeCategory={AgeCategory}", this);
    }

    private void CompleteWithoutAgeCategory(string reason)
    {
        AgeCategory = AccountAgeCategory.Unknown;
        CurrentAgeCategory = AccountAgeCategory.Unknown;
        State = ProbeState.CompletedWithoutAgeCategory;
        _requestInFlight = false;
        Debug.LogWarning(
            $"[Meta Identity Probe] 사용자 ID는 확인했지만 연령대는 확인하지 못했습니다. " +
            $"DUC 검토 상태와 테스트 계정 권한을 확인하세요. {reason}",
            this);
    }

    private void Fail(string stage, string detail)
    {
        State = ProbeState.Failed;
        _requestInFlight = false;
        Debug.LogError($"[Meta Identity Probe] {stage} 실패. {detail}", this);
    }

    private bool CanHandleCallback()
    {
        return this != null && isActiveAndEnabled && _acceptCallbacks;
    }

    public static bool MarkWelcomePlayedForCurrentUser()
    {
        if (CurrentAppScopedUserId == 0 || CurrentWelcomeState == AccountWelcomeState.Unknown)
            return false;

        PlayerPrefs.SetInt(BuildWelcomeSeenKey(CurrentAppScopedUserId), 1);
        PlayerPrefs.Save();
        CurrentWelcomeState = AccountWelcomeState.Returning;
        return true;
    }

    private static string BuildWelcomeSeenKey(ulong appScopedUserId)
    {
        byte[] source = Encoding.UTF8.GetBytes(appScopedUserId.ToString());
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(source);
        StringBuilder key = new StringBuilder(WelcomeSeenKeyPrefix, WelcomeSeenKeyPrefix.Length + 64);
        foreach (byte value in hash)
            key.Append(value.ToString("x2"));
        return key.ToString();
    }

    private static string DescribeError(Message message)
    {
        if (message == null)
            return "Meta SDK 응답이 없습니다.";

        Error error = message.GetError();
        if (error == null)
            return $"StatusCode={message.status.Code()}";

        return $"Code={error.Code}, HttpCode={error.HttpCode}, Message={error.Message}";
    }
}
