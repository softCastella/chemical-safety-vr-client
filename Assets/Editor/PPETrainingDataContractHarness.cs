using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Validates the source-of-truth PPE inventory and work-plan requirements used
/// by the training client. This is intentionally read-only: it does not repair
/// scene data or invent dashboard telemetry.
/// </summary>
public static class PPETrainingDataContractHarness
{
    const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    const string ExitRelaySourcePath = "Assets/Scripts/PPEExitTeleportMarkerRelay.cs";
    const string FinaleSourcePath = "Assets/Scripts/PPEFinaleController.cs";
    const string QuitButtonSourcePath = "Assets/Scripts/QuitApplicationButton.cs";
    const string TelemetrySourcePath = "Assets/Scripts/PPETrainingTelemetryCapture.cs";
    const string TelemetryUploaderSourcePath = "Assets/Scripts/TycheTrainingTelemetryUploader.cs";
    const string VoiceFlowDirectorSourcePath = "Assets/Scripts/PPEVoiceFlowDirector.cs";
    const string QuizControllerSourcePath = "Assets/Scripts/PPEQuizController.cs";
    const string LocalRegistrationClientSourcePath = "Assets/Scripts/TycheLocalTrainingRegistrationClient.cs";
    const string TelemetryUploaderSetupSourcePath = "Assets/Editor/TycheTrainingTelemetryUploaderSetup.cs";
    const string XriInputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
    const string DirectorRequiredConfined = "m_ConfinedSpaceRequiredItemTypes";
    const string DirectorRequiredLeak = "m_LeakResponseRequiredItemTypes";

    static readonly PPEItemType[] ExpectedConfined =
    {
        PPEItemType.HazmatSuit,
        PPEItemType.RubberBootLeft,
        PPEItemType.RubberBootRight,
        PPEItemType.TacticalHarness,
        PPEItemType.GasMask,
        PPEItemType.ConstructionHelmet,
        PPEItemType.NitrileInnerGloveLeft,
        PPEItemType.NitrileInnerGloveRight,
        PPEItemType.RubberGloveLeft,
        PPEItemType.RubberGloveRight,
    };

    static readonly PPEItemType[] ExpectedLeak =
    {
        PPEItemType.HazmatSuit,
        PPEItemType.RubberBootLeft,
        PPEItemType.RubberBootRight,
        PPEItemType.NitrileInnerGloveLeft,
        PPEItemType.NitrileInnerGloveRight,
        PPEItemType.RubberGloveLeft,
        PPEItemType.RubberGloveRight,
        PPEItemType.SafetyGoggles,
        PPEItemType.FaceShield,
        PPEItemType.ConstructionHelmet,
    };

    [MenuItem("Tools/PPE/Validate Training Data Contract")]
    public static void ValidateFromMenu()
    {
        try
        {
            Validate();
            EditorUtility.DisplayDialog(
                "PPE 데이터 계약 PASS",
                "PPEItemType, 씬 장비 인스턴스·패널, 시나리오별 필수 장비 연결을 확인했습니다.",
                "확인");
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("PPE 데이터 계약 FAIL", exception.Message, "확인");
            throw;
        }
    }

    public static void ValidateBatch()
    {
        try
        {
            Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("PPE 데이터 계약 검증은 Play Mode 밖에서 실행해야 합니다.");

        Scene previewScene = default;
        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);
            List<string> failures = new();
            ValidateEnumContract(failures);

            List<PPEItemPresentationBinding> bindings = FindAllInScene<PPEItemPresentationBinding>(previewScene);
            ValidateBindings(bindings, failures);

            PPEVoiceFlowDirector director = FindSingleInScene<PPEVoiceFlowDirector>(previewScene, failures);
            if (director != null)
                ValidateWorkPlanRequirements(director, bindings, failures);

            PPEFinaleController finaleController = FindSingleInScene<PPEFinaleController>(previewScene, failures);
            ValidateExitRouteContract(previewScene, finaleController, failures);
            ValidateGripTelemetryContract(previewScene, failures);

            if (failures.Count > 0)
            {
                string message = "PPE training data contract validation failed:\n- " +
                    string.Join("\n- ", failures);
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            string inventory = string.Join(", ", bindings
                .Select(binding => binding.ItemIdentity.ItemType.ToString())
                .Distinct()
                .OrderBy(value => value));
            Debug.Log(
                $"[PPE Training Data Contract] PASS '{ScenePath}': " +
                $"{bindings.Count} item bindings, {inventory}, " +
                "confined/leak required item arrays, action-panel identity links, " +
                "and authored mid-exit/quit routes are valid. " +
                "Telemetry currently proves application_quitting only; do not claim a detailed exit cause without a route event.");
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    static void ValidateEnumContract(List<string> failures)
    {
        PPEItemType[] values = (PPEItemType[])Enum.GetValues(typeof(PPEItemType));
        if (values.Length != 14)
            failures.Add($"PPEItemType 개수가 14가 아니라 {values.Length}개입니다. 대시보드 카탈로그와 동기화가 필요합니다.");

        if (values.Distinct().Count() != values.Length)
            failures.Add("PPEItemType enum 값이 중복됩니다.");
    }

    static void ValidateBindings(
        List<PPEItemPresentationBinding> bindings,
        List<string> failures)
    {
        if (bindings.Count == 0)
        {
            failures.Add("PPEItemPresentationBinding이 하나도 없습니다.");
            return;
        }

        foreach (PPEItemPresentationBinding binding in bindings)
        {
            string path = GetPath(binding.transform);
            PPEItemIdentity identity = binding.ItemIdentity;
            if (identity == null)
            {
                failures.Add($"{path}: PPEItemIdentity가 없습니다.");
                continue;
            }

            if (!binding.HasCompleteReferences)
                failures.Add($"{path} [{identity.ItemType}]: inspection/equipped visual 참조가 완전하지 않습니다.");

            PPEActionPanelController panel = binding.GetComponent<PPEActionPanelController>() ??
                binding.GetComponentInParent<PPEActionPanelController>() ??
                binding.GetComponentInChildren<PPEActionPanelController>(true);
            if (panel == null)
            {
                failures.Add($"{path} [{identity.ItemType}]: PPEActionPanelController가 연결되지 않았습니다.");
            }
            else if (panel.InspectionState == null || panel.InspectionState.PresentationBinding != binding)
            {
                failures.Add($"{path} [{identity.ItemType}]: ActionPanel의 InspectionState가 같은 PPE binding을 가리키지 않습니다.");
            }
        }

        foreach (PPEItemType itemType in Enum.GetValues(typeof(PPEItemType)))
        {
            if (!bindings.Any(binding => binding.ItemIdentity != null && binding.ItemIdentity.ItemType == itemType))
                failures.Add($"씬에 {itemType} PPE 인스턴스가 없습니다.");
        }
    }

    static void ValidateWorkPlanRequirements(
        PPEVoiceFlowDirector director,
        List<PPEItemPresentationBinding> bindings,
        List<string> failures)
    {
        SerializedObject serialized = new(director);
        ValidateRequiredArray(serialized, DirectorRequiredConfined, ExpectedConfined, bindings, failures);
        ValidateRequiredArray(serialized, DirectorRequiredLeak, ExpectedLeak, bindings, failures);
    }

    static void ValidateExitRouteContract(
        Scene scene,
        PPEFinaleController finaleController,
        List<string> failures)
    {
        List<PPEExitTeleportMarkerRelay> relays = FindAllInScene<PPEExitTeleportMarkerRelay>(scene);
        if (relays.Count != 1)
        {
            failures.Add($"PPEExitTeleportMarkerRelay가 씬에 {relays.Count}개입니다. EXIT Point 중도 중단 경로를 하나로 확정해야 합니다.");
        }
        else
        {
            SerializedObject relay = new(relays[0]);
            SerializedProperty linkedFinale = relay.FindProperty("m_FinaleController");
            SerializedProperty returnOnWalkEnter = relay.FindProperty("m_ReturnOnWalkEnter");
            SerializedProperty player = relay.FindProperty("m_Player");
            SerializedProperty enterVolume = relay.FindProperty("m_EnterVolume");

            if (linkedFinale == null || linkedFinale.objectReferenceValue != finaleController)
                failures.Add("EXIT Point relay가 씬의 단일 PPEFinaleController를 가리키지 않습니다.");
            if (returnOnWalkEnter == null || !returnOnWalkEnter.boolValue)
                failures.Add("현재 locomotion 씬의 EXIT Point가 walk-enter 중도 중단으로 설정되지 않았습니다.");
            if (player == null || player.objectReferenceValue == null)
                failures.Add("EXIT Point walk-enter 판정의 Player 참조가 없습니다.");
            if (enterVolume == null || enterVolume.objectReferenceValue == null)
                failures.Add("EXIT Point walk-enter 판정의 EnterVolume 참조가 없습니다.");
        }

        List<QuitApplicationButton> quitButtons = FindAllInScene<QuitApplicationButton>(scene);
        if (quitButtons.Count != 1)
        {
            failures.Add($"QuitApplicationButton이 씬에 {quitButtons.Count}개입니다. 시나리오 카드 종료 경로를 하나로 확정해야 합니다.");
        }
        else if (quitButtons[0].GetComponent<Button>() == null)
        {
            failures.Add("QuitApplicationButton 오브젝트에 Unity UI Button이 없습니다.");
        }

        ValidateSourceContains(
            ExitRelaySourcePath,
            "m_FinaleController.RequestExitReturn();",
            "EXIT Point relay가 RequestExitReturn 중도 중단 진입점을 호출하지 않습니다.",
            failures);
        ValidateSourceContains(
            FinaleSourcePath,
            "StartCoroutine(ReturnAfterMidExitVoice());",
            "PPEFinaleController의 EXIT 경로가 ReturnAfterMidExitVoice로 연결되지 않습니다.",
            failures);
        ValidateSourceContains(
            FinaleSourcePath,
            "m_VoiceFlowDirector.NotifyMidExitArrived();",
            "PPEFinaleController가 EXIT 도착을 중도 중단으로 통지하지 않습니다.",
            failures);
        ValidateSourceContains(
            QuitButtonSourcePath,
            "Application.Quit();",
            "시나리오 카드 종료 버튼의 앱 종료 호출을 확인할 수 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "Write(\"session_ended\", note:\"application_quitting\");",
            "현재 텔레메트리의 application_quitting 종료 시점 기록을 확인할 수 없습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "/api/training-registrations",
            "Unity 로컬 등록 클라이언트의 training-registrations API 경로를 확인할 수 없습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "Debug.Log(\"가입이 완료되었습니다.\");",
            "Unity 로컬 등록 클라이언트의 왕복 검증 성공 로그를 확인할 수 없습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "RuntimeInitializeLoadType.AfterSceneLoad",
            "Unity 로컬 등록 클라이언트가 초기 씬의 Meta identity probe를 확인하기 전에 설치됩니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "FindAnyObjectByType<MetaPlatformIdentityProbe>",
            "Unity 로컬 등록 클라이언트가 Meta identity probe 없는 직접 씬 실행을 차단하지 않습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "UsesPlatformSdkForCurrentRun",
            "Unity 로컬 등록 클라이언트가 SDKless Editor 실행을 서버 등록에서 제외하지 않습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "IdentityTimeoutSeconds = 180f",
            "Unity 로컬 등록 클라이언트의 Meta ID 제한 시간이 PPE 세션 대기와 분리되지 않았습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "while (!PPETrainingTelemetryCapture.HasActivePpeModeSession",
            "Meta ID 성공 뒤 PPE 활성 세션을 앱 수명 동안 기다리는 경로가 없습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "PPE 활성 세션 진입을 앱 수명 동안 기다립니다.",
            "PPE 세션 무기한 대기 상태를 구분하는 진단 로그가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "public const string SourceProject = \"chemical-safety-vr-client\";",
            "새 클라이언트 JSONL에 이전 모노리포와 구분할 sourceProject가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "eventId = $\"{sessionId}:{sequence:D8}\"",
            "새 클라이언트 JSONL에 재전송 중복 제거용 eventId가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "UploadTokenEnvironmentVariable = \"TYCHE_TELEMETRY_UPLOAD_TOKEN\"",
            "로컬 업로더가 저장소 밖 환경 변수에서 테스트 토큰을 읽지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "EnvironmentVariableTarget.User",
            "Windows Editor가 부모 프로세스의 오래된 환경 때문에 사용자 범위 테스트 토큰을 놓칠 수 있습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "RegGetValue(",
            "실행 중인 Unity가 사용자 환경 변수의 최신 레지스트리 값을 직접 읽는 보완 경로가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "EditorUploadTokenFileName = \".editor-upload-token\"",
            "Unity 프로세스가 환경 변수를 읽지 못할 때 사용할 저장소 밖 Editor 전용 토큰 경로가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "character < '!' || character > '~'",
            "로컬 업로더가 HTTP 헤더에 사용할 수 없는 토큰 문자를 차단하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "!uri.IsLoopback",
            "Editor 테스트 업로더가 로컬 서버 주소로 제한되지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "Old monorepo JSONL intentionally",
            "이전 모노리포 JSONL을 제외하는 출처·스키마 검사가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "/api/training-telemetry/sessions",
            "새 서버 텔레메트리 세션 API 경로가 업로더에 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "client-instance-id.txt",
            "Meta ID가 없는 테스트 사용자를 식별할 새 클라이언트 설치 ID가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "metaUserId = metaUserId",
            "Meta 테스트 ID를 선택적으로 세션 요청에 포함하는 계약이 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "record.eventType == \"session_ended\"",
            "Meta SDK가 응답하지 않아도 종료된 무ID 세션을 익명 사용자로 적재하는 보완이 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "internal static event Action TelemetryRecordAppended;",
            "JSONL append 직후 업로더에 알리는 텔레메트리 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "TelemetryRecordAppended?.Invoke();",
            "JSONL 기록 완료 뒤 텔레메트리 append 알림을 발생시키지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "PPETrainingTelemetryCapture.TelemetryRecordAppended += OnTelemetryRecordAppended;",
            "Editor 업로더가 새 JSONL 레코드 알림을 구독하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "RecordScanDebounceSeconds = 0.2f",
            "새 텔레메트리 이벤트를 짧게 묶어 스캔하는 debounce 기준이 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "Mathf.Max(requestedScanAt, retryNotBefore)",
            "append 알림이 서버 오류의 지수 재시도 대기보다 빠르게 재요청할 수 있습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "QuestLanServerBaseUrlEnvironmentVariable = \"TYCHE_QUEST_LAN_SERVER_BASE_URL\"",
            "Quest 개발 APK의 LAN 서버 주소를 저장소 밖에서 전달하는 설정명이 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "DevelopmentLanConfigurationFileName = \".tyche-development-lan.json\"",
            "Quest 개발 APK의 일회성 LAN 설정 파일 계약이 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR",
            "Quest LAN 전송 경로가 Android Development Build로 제한되지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "File.Delete(path);",
            "Quest 개발 APK가 LAN 설정을 읽은 뒤 일회성 파일을 삭제하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "IsPrivateIpv4(address)",
            "Quest LAN 주소가 RFC1918 사설 IPv4로 제한되지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "void OnApplicationPause(bool paused)",
            "모바일 pause/resume 시 durable queue 스캔을 요청하는 업로더 경로가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "Write(paused ? \"application_paused\" : \"application_resumed\");",
            "pause/resume가 비종료 텔레메트리 이벤트로 구분되지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "\"mode_session_started\"",
            "세 모드의 실행 시작 원본 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "\"quiz_answer_resolved\"",
            "모드별 퀴즈 선택과 정오 원본 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "\"mode_session_completed\"",
            "세 모드의 실행 완료 원본 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            VoiceFlowDirectorSourcePath,
            "BeginModeSessionTracking();",
            "모드 선택을 모드 실행 시작 계측에 연결하지 않았습니다.",
            failures);
        ValidateSourceContains(
            QuizControllerSourcePath,
            "RecordQuizAnswer(optionIndex, correct);",
            "퀴즈 선택을 원본 계측에 연결하지 않았습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSourcePath,
            "modeSessionId = record.modeSessionId",
            "모드 실행 ID를 서버 업로드 이벤트로 전달하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "modeSessionId ?? director?.ActiveModeSessionId",
            "모드 실행 중 공통 PPE·음성 이벤트를 현재 모드 실행 ID에 연결하지 않습니다.",
            failures);
        ValidateSourceContains(
            LocalRegistrationClientSourcePath,
            "TryGetAndroidDevelopmentLanConfiguration(",
            "Quest 개발 APK 등록 클라이언트가 텔레메트리와 같은 LAN 설정을 사용하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "Inject Quest Development LAN Configuration",
            "주소·토큰을 APK 밖에서 주입하는 Unity Editor 메뉴가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "RedirectStandardInput = standardInput != null",
            "Quest 개발 설정의 민감값을 adb 표준 입력으로 전달하는 경로가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "shell run-as {packageName} mkdir -p files",
            "Quest 개발 설정 디렉터리 생성이 Windows에서 안전한 adb 인수 경로로 분리되지 않았습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "shell run-as {packageName} tee files/",
            "Quest 개발 설정 주입이 sh -c 인수 분리 없이 adb 표준 입력을 파일로 기록하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "(report.summary.options & BuildOptions.Development) != 0",
            "Android cleartext 허용 여부가 Development Build 옵션과 연결되지 않았습니다.",
            failures);
        ValidateSourceContains(
            TelemetryUploaderSetupSourcePath,
            "developmentAndroidBuild ? \"true\" : \"false\"",
            "Release 생성 Manifest에서 cleartext를 다시 차단하는 경로가 없습니다.",
            failures);
    }

    static void ValidateGripTelemetryContract(Scene scene, List<string> failures)
    {
        List<NearFarInteractor> interactors = FindAllInScene<NearFarInteractor>(scene);
        if (!interactors.Any(interactor => interactor.handedness == InteractorHandedness.Left))
            failures.Add("왼손 NearFarInteractor가 없어 왼손 Grip 시도를 PPE hover/select와 연결할 수 없습니다.");
        if (!interactors.Any(interactor => interactor.handedness == InteractorHandedness.Right))
            failures.Add("오른손 NearFarInteractor가 없어 오른손 Grip 시도를 PPE hover/select와 연결할 수 없습니다.");

        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriInputActionsPath);
        if (inputActions == null)
        {
            failures.Add($"XRI Input Actions를 찾을 수 없습니다: {XriInputActionsPath}");
        }
        else
        {
            ValidateGripSelectBinding(inputActions, "XRI Left Interaction", failures);
            ValidateGripSelectBinding(inputActions, "XRI Right Interaction", failures);
        }

        ValidateSourceContains(
            TelemetrySourcePath,
            "InputSystem.onActionChange += OnInputActionChange;",
            "그랩 계측이 실제 Input System action change를 관찰하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "director.CurrentState != PPEVoiceFlowDirector.FlowState.PpeArea",
            "그랩 계측이 PPE 구역 상태로 제한되지 않아 다른 단계 Grip이 섞일 수 있습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "controlPath.IndexOf(\"grip\"",
            "그랩 계측이 실제 Grip control만 필터링하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "ppe_grab_attempted",
            "그랩 시도 원본 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "ppe_grab_attempt_resolved",
            "그랩 시도 결과 이벤트가 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "no_ppe_hover",
            "PPE hover가 없는 그랩 실패 결과를 기록하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "hover_without_select",
            "PPE hover 후 select 실패 결과를 기록하지 않습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "public string appVersion;",
            "업데이트 전후 비교에 필요한 앱 버전 필드가 텔레메트리 계약에 없습니다.",
            failures);
        ValidateSourceContains(
            TelemetrySourcePath,
            "appVersion = eventType == \"session_started\" ? Application.version : null,",
            "session_started 이벤트가 실제 Application.version을 기록하지 않습니다.",
            failures);
    }

    static void ValidateGripSelectBinding(
        InputActionAsset inputActions,
        string mapName,
        List<string> failures)
    {
        InputActionMap map = inputActions.FindActionMap(mapName, false);
        InputAction select = map?.FindAction("Select", false);
        if (select == null)
        {
            failures.Add($"{mapName}/Select 액션을 찾을 수 없습니다.");
            return;
        }

        bool hasControllerGrip = select.bindings.Any(binding =>
            binding.path != null &&
            binding.path.Contains("<XRController>", StringComparison.Ordinal) &&
            binding.path.Contains("{GripButton}", StringComparison.Ordinal));
        if (!hasControllerGrip)
            failures.Add($"{mapName}/Select가 XRController GripButton에 연결되지 않았습니다.");
    }

    static void ValidateSourceContains(
        string assetPath,
        string requiredText,
        string failureMessage,
        List<string> failures)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            failures.Add($"검증 대상 소스가 없습니다: {assetPath}");
            return;
        }

        string source = File.ReadAllText(fullPath);
        if (!source.Contains(requiredText, StringComparison.Ordinal))
            failures.Add(failureMessage);
    }

    static void ValidateRequiredArray(
        SerializedObject serialized,
        string propertyName,
        PPEItemType[] expected,
        List<PPEItemPresentationBinding> bindings,
        List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            failures.Add($"{propertyName} 배열을 찾을 수 없습니다.");
            return;
        }

        List<PPEItemType> actual = new();
        for (int index = 0; index < property.arraySize; index++)
            actual.Add((PPEItemType)property.GetArrayElementAtIndex(index).intValue);

        if (!actual.SequenceEqual(expected))
            failures.Add($"{propertyName}: 씬 필수 PPE 배열이 PPEVoiceFlowDirector 기준과 다릅니다. 실제=[{string.Join(", ", actual)}], 기준=[{string.Join(", ", expected)}]");

        if (actual.Count != actual.Distinct().Count())
            failures.Add($"{propertyName}: 필수 PPE가 중복됩니다. 좌/우 장비를 의도한 중복인지 확인해야 합니다.");

        foreach (PPEItemType itemType in actual.Distinct())
        {
            if (!bindings.Any(binding => binding.ItemIdentity != null && binding.ItemIdentity.ItemType == itemType))
                failures.Add($"{propertyName}: {itemType} 필수 장비에 씬 인스턴스가 없습니다.");
        }
    }

    static T FindSingleInScene<T>(Scene scene, List<string> failures) where T : Component
    {
        List<T> matches = FindAllInScene<T>(scene);
        if (matches.Count != 1)
        {
            failures.Add($"{typeof(T).Name}이 씬에 {matches.Count}개입니다. 단일 상태 소유자를 확정해야 합니다.");
            return null;
        }

        return matches[0];
    }

    static List<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    static string GetPath(Transform current)
    {
        List<string> names = new();
        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
