# Quest 앱 내부 종료 버튼의 서버 세션 미종료

## 재현 결과

- 대상은 Meta Alpha `versionCode=6`을 Quest에서 단독 실행한 Release 앱이다.
- 사용자가 앱 내부 `종료하기` 버튼을 눌러 Quest 라이브러리로 복귀했지만, 운영 서버의 최신 Meta 세션
  `0cb9366f…`은 `open`으로 남았다.
- 서버가 받은 마지막 데이터는 `sequence=135`, `voice_playback_ended`였고 `application_paused`와
  `session_ended`는 수신되지 않았다.
- 이전 세션 `5df09877…`은 다음 앱 실행에서 로컬 미전송 데이터가 복구된 뒤에야
  `application_quitting`, `eventCount=165`, `lastSequence=165`로 완료됐다.

## 근본 원인

- `QuitApplicationButton.HandleQuit()`이 앱 내부 종료 입력 직후 `Application.Quit()`을 호출했다.
- `PPETrainingTelemetryCapture`는 `Application.quitting`에서 종료 이벤트를 로컬 JSONL에 기록했지만,
  `TycheTrainingTelemetryUploader`가 마지막 이벤트와 `/complete` 응답을 받기 전에 Android 프로세스가
  종료될 수 있었다.
- 따라서 앱 종료 자체는 성공했지만 서버 상태는 다음 실행의 durable recovery 전까지 `open`이었다.

## 변경 전 판단

1. 기존 Inspector·씬 작성값과 `Quit Button`의 위치·크기·입력 연결은 보존한다.
2. 앱 세션 종료 상태의 소유자는 `PPETrainingTelemetryCapture`, 서버 ACK 상태의 소유자는
   `TycheTrainingTelemetryUploader`로 유지한다.
3. 기존 Unity UI Button → `QuitApplicationButton.HandleQuit()` 입력 경로는 변경하지 않는다.
4. 필수 객체를 런타임 생성하거나 자동 수리하지 않으며, 업로더가 없거나 전송이 실패하면 제한시간 후
   durable recovery를 남기고 종료한다.
5. 종료 버튼, 로컬 JSONL, Meta 인증, 운영 HTTPS, 서버 세션 완료와 연속 모드 데이터가 영향을 받는다.
6. 변경 전 기준은 code 6 종료 직후 서버 `open`; 변경 후 기준은 종료 버튼이 서버 완료 ACK를 확인한 뒤
   앱을 종료하는 것이다.
7. 정적 컴파일과 Unity Editor 하네스를 먼저 수행하며, Quest Release 실제 종료 검증은 code 7 설치 후
   별도로 수행한다.

## 적용한 변경

- 종료 버튼은 현재 앱 세션의 `session_ended/application_quitting`을 명시적으로 먼저 기록한다.
- 동일 종료 이벤트가 명시적 종료 요청과 `Application.quitting`에서 중복 기록되지 않도록 막았다.
- 종료 이벤트 뒤 대기 시간에 발생하는 음성·입력 이벤트가 같은 세션에 추가되지 않도록 기록 경계를 닫았다.
- 업로더는 현재 세션의 `.upload-state.json`에서 서버 완료 ACK를 최대 5초 기다린다. ACK를 확인하면 즉시
  앱을 종료하고, 실패나 timeout이면 기존 로컬 복구 파일을 보존한 채 종료한다.
- 씬 YAML, UI 작성값, 카드·모달·텔레포트·PPE 입력 경로는 변경하지 않았다.

## 연속 실행 데이터 분리

- 앱을 종료하지 않고 복귀해 다른 시나리오 또는 Education·Training·Test를 선택하면 앱 `sessionId`는
  유지된다.
- 각 선택은 `RecordModeSessionStarted()`에서 새 GUID `modeSessionId`를 생성한다. 완료 이벤트는 해당
  `modeSessionId`, `mode`, `workPlan`에 연결되므로 여러 교육 실행이 서로 합쳐지지 않는다.
- 정상 완료·복귀 후 `ResetModeSessionForNextSelection()`이 PPE, 퀴즈, 음성 one-shot과 모드 추적 상태를
  초기화한다. 앱 전체 세션은 사용자가 앱 내부 `종료하기`를 눌렀을 때만 닫힌다.

## 검증

- `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0개. imported/sample 경고만 존재한다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 오류 0개, 경고 0개.
- Unity `PPETrainingDataContractHarness.Validate`: 종료 기록 → 서버 ACK 대기 → 앱 종료 순서, 종료 중복 방지,
  새 `modeSessionId` 생성과 복귀 초기화 계약 PASS.
- 과거 `HangulKeyboardSubmitSafetyHarness`는 저장소에 없는
  `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`를 참조해 실행되지 않았다. 현재 종료 버튼의 기존
  `HangulKeyboardSubmitGuard`와 Editor 종료 분기는 코드에서 보존했다.

## 남은 실제 검증

1. Android `versionCode=7` Release APK는
   `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_7.apk`로 생성했다. Unity BuildReport는
   `Success`, 실제 파일 크기는 `219,634,992 bytes`, SHA-256은
   `A1F500B135304F81033CC738CCDADC63C6170A15CB06A2DCB687E4E7C15D7B9A`다.
2. `aapt2`에서 package `com.tycheworks.immersa.safetyvr`, `versionCode=7`, Android 25/34,
   ARM64, Quest VR category와 필수 head tracking을 확인했다. Manifest의 `usesCleartextTraffic=false`,
   `android:debuggable` 부재와 `apksigner`의 출시 인증서 APK Signature Scheme v2 검증도 통과했다.
3. code 7 APK를 Alpha 채널에 올리고 Quest에 설치한다.
4. Quest에서 한 모드를 시작한 뒤 앱 내부 `종료하기`를 누르고 라이브러리로 복귀하며 ADB에서 앱 PID가
   사라지는지 확인한다.
5. 앱을 다시 실행하지 않은 상태에서 운영 서버가 같은 세션을 `completed`,
   `endReason=application_quitting`으로 저장했는지 확인한다.
6. 한 앱 실행에서 서로 다른 시나리오·모드 두 개를 연속 완료하고 두 개의 서로 다른 `modeSessionId`와
   각각 한 개의 `mode_session_completed`가 기록됐는지 확인한다.
