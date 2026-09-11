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
3. 사용자가 Meta Alpha에 code 7 업로드 완료를 확인했다. Quest 라이브러리에서 code 7을 설치하고 실제
   설치 버전을 확인한다.
4. Quest에서 한 모드를 시작한 뒤 앱 내부 `종료하기`를 누르고 라이브러리로 복귀하며 ADB에서 앱 PID가
   사라지는지 확인한다.
5. 앱을 다시 실행하지 않은 상태에서 운영 서버가 같은 세션을 `completed`,
   `endReason=application_quitting`으로 저장했는지 확인한다.
6. 한 앱 실행에서 서로 다른 시나리오·모드 두 개를 연속 완료하고 두 개의 서로 다른 `modeSessionId`와
   각각 한 개의 `mode_session_completed`가 기록됐는지 확인한다.

## Alpha 업로드 후 로컬 정리

- 사용자는 Meta Dashboard에서 code 7 Alpha 업로드 완료를 확인했다. 이는 사용자 화면 확인을 근거로 하며
  Codex가 Meta API에서 독립 조회한 결과는 아니다.
- 업로드 확인 후 `Builds/MetaHorizonAlpha`의 APK 7개를 삭제했다. 삭제량은 `2,027,034,856 bytes`이며
  남은 APK는 0개다. 여기에는 code 7 Release APK도 포함된다.
- APK는 Git 보존 대상이 아니며 로컬 삭제는 되돌릴 수 없다. code 7 바이너리는 Meta Alpha에서 다시
  설치하거나 `main`의 같은 versionCode와 출시 Keystore로 재빌드한다.
- APK 삭제는 Git에 보존된 소스, 기준 JSONL, 문서와 운영 서버 데이터를 삭제하지 않는다.

## code 7 Quest 실제 검증과 완료 세션 재전송 결함

### 실제 검증 결과

- Quest 2에 Meta Alpha 채널을 통해 설치된 앱은 package
  `com.tycheworks.immersa.safetyvr`, `versionCode=7`, installer `com.oculus.ocms`이며
  `android:debuggable`이 없는 Release 앱으로 확인했다.
- 첫 검증 세션 `bb539742…`은 로컬 복구 큐를 정리한 뒤 운영 서버에서 `eventCount=23`,
  `sequence 1~23`, `status=completed`, `endReason=application_quitting`으로 확인됐다.
- 두 번째 검증 세션 `31bddadc…`은 앱 내부 종료 버튼 실행 직후 ADB에서 PID가 사라졌고,
  종료 로그에 `Telemetry session completed on the server before quitting.`이 남았다. Quest ACK는
  `acceptedThroughSequence=32`, `completed=true`, 운영 서버는 `eventCount=32`, `sequence 1~32`,
  `status=completed`, `endReason=application_quitting`으로 일치했다.
- 두 실행은 동일한 앱 범위 Meta 사용자로 식별됐지만 정상 모드 완료 없이 카드 단계에서 종료됐다.
  따라서 `modeSessionId`와 `mode_session_completed`가 없고 다음 실행에서도 `FirstVisit` 및
  `Welcome_New`가 적용된 것은 현재의 시나리오 완료 기반 기존 사용자 계약과 일치한다.

### 추가로 발견한 근본 원인

- Meta 채널 설치 뒤 Quest에 과거 JSONL이 복원됐지만 일부 `.upload-state.json`은 없거나 실제 서버
  완료 상태보다 뒤처져 있었다.
- 해당 과거 세션들은 운영 DB에서 이미 `completed`였지만 클라이언트가 이벤트를 다시 전송하면 서버가
  완료 세션이라는 이유로 `409`를 반환했다. 업로더는 가장 오래된 실패 파일에서 순회를 중단하므로 새
  code 7 세션도 그 뒤에서 전송되지 못했다.
- Quest 원본 JSONL을 PC의 Git 제외 임시 폴더에 먼저 백업하고, 운영 DB에서 세션 ID·이벤트 수·완료
  상태가 정확히 일치한 네 과거 세션에 한해 로컬 ACK 메타데이터만 복구했다. 원본 JSONL과 운영 DB는
  수정하지 않았다.

### 서버 후속 수정과 검증 경계

- 서버 저장소 `main@1d9b9fa0a146b4a15925470182ec7a7f2ceff462`를 변경 전 기준으로, 완료 세션에도
  이미 저장된 이벤트와 ID·sequence·payload hash가 모두 같은 재전송만 중복 성공으로 응답하도록
  저장소 계약을 수정했다. 서버 코드와 테스트는
  `main@0ef1619c70d1fbfa9d448463da3d0244ad9724ab`로 GitHub에 반영했다.
- 완료 세션의 새 이벤트 또는 기존 ID·sequence와 데이터가 다른 이벤트는 계속 `409 CONFLICT`로
  거부한다. 완료된 원본을 변경하거나 뒤늦은 이벤트를 추가하지 않는다.
- 인메모리 저장소와 API 회귀 테스트를 함께 보강했고 서버 `npm test`는 93개 모두 PASS했다.
- 이 서버 수정은 아직 운영 서버에 배포하지 않았다. 따라서 현재 운영에서 확인한 code 7 종료 성공은
  Quest의 ACK 메타데이터를 제한적으로 복구한 뒤의 결과이며, 수정된 서버의 자동 복구 성공으로 확대하지
  않는다.

## 다음 실제 검증

1. GitHub에 반영된 서버 수정 커밋을 기준으로 별도 승인을 받아 운영에 배포한다.
2. 완료 세션의 ACK가 없거나 뒤처진 재현 데이터에서 수동 보정 없이 중복 응답과 최신 파일 전송 재개를
   확인한다.
3. 한 앱 실행에서 서로 다른 모드 두 회차를 정상 완료하고 서로 다른 `modeSessionId`와 각 한 개의
   `mode_session_completed`를 운영 DB에서 확인한다.
4. 정상 모드 완료 뒤 앱을 재실행해 동일 Meta 사용자가 `Returning`으로 판정되고 `Welcome_Old`가
   재생되는지 확인한다.

## 2026-09-11 Build 8 학원 Quest 2 검증 결과

- Meta Alpha 설치본 `versionCode=8`, `versionName=1.0.0`에서 앱 세션 `0ec97178…`을 새로 수집했다.
- EXIT Point로 복귀한 `Education/LeakResponse` 회차는 `mode_session_started`만 있고 같은
  `modeSessionId`의 `mode_session_completed`는 없었다. `session_ended/application_quitting`도 발생하지
  않아 중도 복귀가 정상 완료나 앱 종료로 오기록되지 않았다.
- 같은 앱 실행에서 Training과 Test를 각각 정상 완료했다. 두 회차는 서로 다른 `modeSessionId`를 사용했고
  각각 한 개의 `mode_session_completed`를 기록했다.
- 앱 내부 `종료하기` 직후 Quest PID가 사라졌고 로컬 JSONL·ACK와 운영 MySQL은 이벤트 302개,
  마지막 `sequence=302`, `completed/application_quitting`으로 일치했다. 다음 앱 실행의 durable recovery는
  필요하지 않았다.
- 같은 세션의 최초 시작 원본은 `metaWelcomeState=Returning`과
  `VO_PPE_INTRO_002_Welcome_Old` 재생을 기록했다. 사용자의 기존 사용자 안내 관찰과 일치한다.
- 서버 운영 checkout은 중복 재전송 수정 `0ef1619…`을 포함한 `094524e…`이고 PM2 프로세스도 해당 수정
  이후 재시작된 상태로 확인했다.

### 남은 제한

- EXIT Point 전용 텔레메트리 이벤트는 아직 없다. 현재는 상태 복귀와 같은 `modeSessionId`의 완료 이벤트
  부재로만 중도 복귀를 판정하므로, 대시보드에서 이를 확정 종료 원인으로 표시하지 않는다.
- 운영 HTTP 조회 token은 설정하지 않았다. 이번 운영 DB 대조는 SSH에서 서버 repository의 읽기 전용
  조회를 사용했으며 API 조회 권한 검증과는 구분한다.
- 이번 실행은 종료·연속 모드·기존 사용자 계약을 검증했다. Quest 양안 시각 품질과 전체 Education 정상
  완료 회귀는 별도 수동 검증으로 남긴다.
- Unity `DocumentationPolicyHarness.Validate`는 학원 PC의 Unity 라이선스 부재로 종료 코드 `198`을
  반환해 실행되지 않았다. 이번 문서 변경의 정적 diff에는 새 공백 오류가 없다.
