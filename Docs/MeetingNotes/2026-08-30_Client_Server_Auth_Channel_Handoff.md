# 클라이언트-서버 인증·채널·텔레메트리 인수인계 회의록

- 일자: 2026-08-30
- 클라이언트 저장소: `softCastella/chemical-safety-vr-client`
- 클라이언트 브랜치: `main`
- 클라이언트 UI 반영 커밋: `b25814a018fb2c5a3c98b77e56385ca889abd35b`
- 서버 저장소: `softCastella/chemical-safety-vr-server`
- 서버 확인 브랜치: `main`
- 서버 검증 기준 커밋: `6512f0c8e65401e5e32b88c971961cf6aea90f22`
- 서버 미러·검증 결과 커밋: `5adc9ef647fd786f918db33ba1f698e61a5ce5b3`
- 서버 작업 트리: 기존 수정 파일이 남아 있어 clean 상태가 아님
- 동기화 상태: 클라이언트 기준본·서버 미러 내용 일치 / 변경 전 Editor 통합 검증 완료 / 변경 후 Editor 단계별 회귀 검증 완료 / 동일 실행의 `180초 초과 → PPE 진입` 및 Quest·APK 미검증

## 목적

클라이언트와 서버 대화 채널의 자동 공유를 전제로 하지 않고, 현재 확인된 인증·로컬 서버·텔레메트리
상태와 서버 확인 요청을 파일 기준으로 인수인계한다. 이 문서의 기준본은 클라이언트 저장소에 두며,
서버 담당자는 같은 상대 경로로 미러링한 뒤 서버 브랜치와 커밋 SHA를 채운다.

## 현재 상태 요약

| 구분 | 상태 | 근거 또는 제한 |
|---|---|---|
| 클라이언트 로컬 텔레메트리 기록 | 확인 | 2026-08-30 Unity Editor 검증 세션에서 JSONL 240개와 연속 sequence를 확인 |
| Meta 앱 범위 사용자 ID | Editor Standalone 테스트 사용자 확인 / Quest 미검증 | Platform 초기화·entitlement·앱 범위 ID 조회 성공, 원문은 기록하지 않음 |
| 로컬 Express 실행 | 확인 | 2026-08-30 현재 TCP 3000 listen 및 `GET /` HTTP 200 확인 |
| 로컬 등록 API | 변경 전·후 Editor 왕복 확인 | 변경 후 PPE 진입 세션에서 POST/GET의 Meta ID·연령 범주·`sessionId` 일치 확인. 별도 세션에서 180초 초과 대기 유지 확인. 동일 실행의 `180초 초과 → PPE 진입`은 미검증 |
| 텔레메트리 업로드 API | 변경 전 Editor 240/240 / 변경 후 Editor 종료 복구 59/59 확인 | 실행 중 58/58 및 약 0.27초 ACK 확인. 종료 직후 로컬 59/서버 58이었고 다음 실행에서 59/59, `completed`, 누락·중복 0으로 복구 |
| 대화 채널·큐 전달 | 로컬 런처 구현 확인 / 이번 전달 미검증 | `CodexPairLauncher`는 존재하지만 클라이언트 코드·Unity 로그나 대화 표시만으로 특정 메시지의 큐 전달 성공을 판단할 수 없음 |
| 출시 Player 업로드 | 미구현 | 현재 업로더는 Editor만 허용하고 Player에서는 업로드를 시작하지 않음 |

## 로컬 Codex Pair Launcher

### 위치와 역할

- 이 PC의 런처 경로는 `C:\Users\lanoc\Tools\CodexPairLauncher`다.
- 런처는 클라이언트·서버 저장소 밖에 있는 Windows용 로컬 도구이며, 확인 시점에는 별도 Git 저장소가 아니다.
- 같은 PC에서 Codex CLI 세션 두 개를 로컬 app-server에 연결하고 사용자가 명시적으로 요청할 때만 상대 세션에 메시지를 전달한다.
- 통신 대상은 `ws://127.0.0.1`뿐이다. `--remote`는 이 구성에서 외부 서버가 아니라 같은 PC의 Codex CLI 연결을 뜻한다.
- 자동 답장이나 무한 대화 루프를 만들지 않으며 프로젝트 파일 또는 Git 저장소를 자동 수정하지 않는다.

### 주요 명령

- `Setup.cmd`: 클라이언트·서버 저장소 경로와 로컬 릴레이 설정을 작성한다.
- `Self-Test.cmd`: 모델 턴을 시작하지 않고 런처 호환성과 임시 검사 세션을 확인한다.
- `Start-CodexPair.cmd`: 숨겨진 로컬 app-server와 클라이언트·서버 Codex 창을 시작하고, 호환되는 기존 중계기와 저장 세션이 있으면 재사용한다.
- `Start-Fresh-CodexPair.cmd`: 기존 세션을 재사용할 수 없을 때 새 세션 두 개를 만든다.
- `Send-ToClient.cmd`, `Send-ToServer.cmd`: 사용자가 입력한 메시지를 상대 세션의 새 턴으로 전달한다.
- `Open-SessionManager.cmd`: 세션 관리 화면을 연다.
- `Stop-CodexPair.cmd`: 런처가 시작한 로컬 app-server만 종료한다.
- `Logout-Codex.cmd`: 공용 PC 사용 후 Codex 인증정보를 제거한다. GitHub 인증정보는 별도로 관리한다.
- `Build-Zip.cmd`: 로컬 설정과 실행 로그를 제외한 배포 ZIP을 만든다.

### 설정과 보안 제한

- PC별 경로 설정은 `relay.local.json`에 저장되며 `.gitignore`와 배포 ZIP에서 제외된다.
- `relay.local.json`에는 저장소 경로, `listenHost`, port와 세션 재사용 여부만 두고 로그인 token이나 API key를 저장하지 않는다.
- `listenHost`는 반드시 `127.0.0.1`이어야 한다. `0.0.0.0`으로 열거나 집·학원 PC를 직접 연결하지 않는다.
- 로그인 token, API key, DB 비밀번호와 실제 사용자 데이터를 전달 메시지에 넣지 않는다.
- app-server 포트를 다른 프로세스가 사용하면 해당 프로세스에 임의 연결하거나 종료하지 않고 중단한다.
- 런처는 Codex CLI의 실험적 `app-server`, `--remote`, `queue`와 세션 프로토콜을 사용하므로 CLI 업데이트 뒤에는 `Self-Test.cmd` 통과 전 사용하지 않는다.

### 기록과 검증 경계

- 런처의 `queue`는 일시적인 대화 전달 수단이며 영구 작업 기록이 아니다. 완료 사실은 코드, 테스트 결과, Git 커밋과 `Docs/SharedDocumentManifest.md`의 공용 문서로 남긴다.
- 대화가 상대 Codex 화면에 보였다는 사실은 Unity 등록·텔레메트리 API 수신, Express 로그, MariaDB 적재 또는 대시보드 조회 성공을 의미하지 않는다.
- 이번 서버 문서 보강에서는 README, 명령 파일, PowerShell 스크립트 목록, `.gitignore`와 `relay.example.json`을 정적으로 확인했다. `Self-Test.cmd` 실행과 실제 양방향 메시지 전달은 이번 검증 범위에 포함하지 않았다.

## 클라이언트에서 확인된 계약

### Meta 계정 식별

- `Assets/Scenes/0_App.unity`의 `MetaPlatformIdentityProbe`는 `runOnStart=1`,
  `useMetaPlatformSdkInEditor=1`로 작성되어 있다.
- 식별 순서는 Platform SDK 초기화 → 앱 entitlement 확인 → `Users.GetLoggedInUser()` →
  앱 범위 사용자 ID 조회다.
- Editor 테스트 계정이든 일반 계정이든 현재 앱 ID에 대한 entitlement와 Meta Platform SDK 로그인
  컨텍스트가 유효해야 앱 범위 사용자 ID를 받을 수 있다.
- Editor 업로드용 Bearer token은 Meta 사용자 인증을 대체하지 않는다. 두 인증 경로는 분리해서
  검증해야 한다.

### 개발용 로컬 등록

- `TycheLocalTrainingRegistrationClient`는 `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 설치된다.
- 기본 서버 주소는 `http://127.0.0.1:3000`이다.
- 전송 전제는 유효한 Meta 앱 범위 사용자 ID와 활성 PPE 모드 세션이다.
- 호출 계약은 다음과 같다.
  - `POST /api/training-registrations`
  - `GET /api/training-registrations/{sessionId}`
- POST 응답과 GET 재조회 결과의 `metaUserId`, `sessionId`가 클라이언트 요청과 일치해야 성공으로
  판단한다.

### Editor 텔레메트리 업로드

- `Assets/Scenes/0_App.unity`의 `TycheTrainingTelemetryUploader`는
  `enableEditorTestUpload=1`, `serverBaseUrl=http://127.0.0.1:3000`으로 작성되어 있다.
- 업로드 주소는 loopback만 허용한다.
- `TYCHE_TELEMETRY_UPLOAD_TOKEN`을 `Authorization: Bearer ...`로 전송한다. token 값은 문서나
  저장소에 기록하지 않는다.
- 호출 계약은 다음과 같다.
  - `POST /api/training-telemetry/sessions`
  - `POST /api/training-telemetry/sessions/{sessionId}/events`
  - `POST /api/training-telemetry/sessions/{sessionId}/complete`
- 현재 코드에서 Editor가 아닌 Player는 `출시 Player 인증은 아직 연결되지 않아 업로드를 시작하지
  않습니다.`를 기록하고 종료한다.

## 서버 저장소에서 확인된 상태

- 서버 검증 시작 기준은 `main@6512f0c8e65401e5e32b88c971961cf6aea90f22`이며, 미러와 검증
  결과는 `main@5adc9ef647fd786f918db33ba1f698e61a5ce5b3`에 푸시됐다.
- 서버 결과 커밋 뒤에도
  `Docs/MeetingNotes/2026-08-25_Production_Server_Meta_Horizon_Release_Plan.md`의 기존 수정은
  별도 미커밋 상태로 남아 있다.
- `src/app.js`에 `/api/training-registrations`와 `/api/training-telemetry` 라우터가 연결되어 있다.
- 등록·텔레메트리 라우트 구현과 관련 자동 테스트 파일이 존재한다.
- 서버 `.env` 파일이 있으며 다음 상태만 값 노출 없이 확인했다.
  - `ENABLE_LOCAL_TRAINING_REGISTRATION=true`
  - `ENABLE_TRAINING_TELEMETRY_INGEST=true`
  - `TRAINING_TELEMETRY_UPLOAD_TOKEN` 설정됨
  - DB 설정 키 5개 존재
- 2026-08-30 확인 시 Express가 TCP 3000에서 listen 중이었고 `GET /`은 HTTP 200을 반환했다.
- 위 확인은 서버 기동과 정적 구현 근거다. Unity 요청 수신, 인증 성공, DB 적재 및 재조회 성공을
  대신하지 않는다.

## 이번 클라이언트 UI 변경

- `Assets/Scripts/ScenarioDetailModal.cs`에서 시나리오 선택 화면과 모드 선택 화면을 표시할 때
  `EventSystem`의 현재 선택을 비우도록 변경했다.
- 맨 위 버튼을 자동으로 `.Select()`하던 경로를 제거해, 아무것도 선택되지 않은 상태에서 실제
  포인터 진입만 hover 색으로 보이게 했다.
- API, 데이터 계약, Meta 인증, 텔레메트리 및 서버 구현에는 영향이 없다.
- `Assembly-CSharp.csproj --no-restore` 정적 빌드는 오류 0개였고, 실제 Quest/OpenXR hover 표시는
  수동 확인이 남아 있다.
- 클라이언트 반영 커밋은 `b25814a018fb2c5a3c98b77e56385ca889abd35b`다.

## 서버 담당 확인 결과

- 등록 계약은 서버 구현과 자동 테스트 기준으로 클라이언트 계약과 일치한다.
  - `POST /api/training-registrations`는 `metaUserId`, `sessionId`를 포함한 요청을 저장한다.
  - `GET /api/training-registrations/{sessionId}`는 같은 `sessionId`의 등록을 재조회한다.
  - 자동 테스트에서 생성 응답 HTTP 201, `metaUserId`·`sessionId` 일치와 GET 재조회를 확인했다.
- 텔레메트리 계약도 서버 구현과 자동 테스트 기준으로 일치한다.
  - 세션 생성, 이벤트 적재, 완료 엔드포인트는 모두 Bearer 업로드 token을 요구한다.
  - `acceptedThroughSequence`, 중복 재전송, 완료 세션의 후속 이벤트 거부를 테스트한다.
- 2026-08-30 서버 `npm test` 결과는 34개 통과, 실패 0개다. 이는 서버 정적 계약과 격리 저장소
  동작의 회귀 확인이며 Unity 실제 요청이나 로컬 MariaDB 적재 성공을 대신하지 않는다.
- 서버 `src`와 `test`에는 대화 채널 또는 작업 큐 구현이 확인되지 않았다. 등록 저장소의 내부
  `writeQueue`와 MySQL 풀의 `queueLimit`은 대화 전달 큐가 아니다.
- Unity 요청의 HTTP 상태, 서버 수신 로그, MariaDB 적재와 같은 `sessionId` 재조회 근거가 없어
  통합 검증 상태는 계속 `대기`다.
- 위 결과는 서버 `main@5adc9ef647fd786f918db33ba1f698e61a5ce5b3`에서 확인됐고 이
  클라이언트 기준본에 반영했다.

## 다음 통합 검증 순서

1. 서버 브랜치/SHA와 로컬 서버 실행 로그를 기록한다.
2. Meta 앱 ID와 entitlement가 맞는 허용 계정으로 `0_App`부터 새 실행한다.
3. Unity 로그에서 Platform 초기화, entitlement, 앱 범위 사용자 ID 성공 단계를 구분해 확인한다.
4. PPE 모드 세션을 시작한 뒤 등록 POST와 GET 재조회를 확인한다.
5. 로컬 JSONL 생성과 텔레메트리 세션·이벤트·완료 API 수신을 대조한다.
6. 서버 DB의 `sessionId`, Meta 앱 범위 사용자 ID, 이벤트 순서가 원본 JSONL과 일치하는지 확인한다.
7. 위 증거가 모두 있을 때만 `통합 검증 완료`로 상태를 변경한다.

## 보안

- Meta 사용자 ID, 계정 ID, 비밀번호, access token, 업로드 token 원문을 이 문서에 기록하지 않는다.
- 사용자 식별이 필요한 검증 결과에는 마스킹 값 또는 일치 여부만 기록한다.

## 2026-08-30 사용자 전달 후속 — Meta 등록 성공 및 종료 직전 업로드 지연

> 이 절은 앞선 `통합 검증 대기` 기록 이후 전달받은 후속 실행 결과다. 기존 기록을 소급해 지우지
> 않으며, 아래 결과와 아직 필요한 코드·로그 대조를 구분한다. 사용자는 2026-08-30 후속 지시에서
> 이 이미지가 기록 전용이 아니라 실제 수정·검증 요청임을 명확히 했다.

### 전달받은 기준

- 클라이언트 기준: `main@8b1b4300349529a3ebcb260f1da614769bef6b34`
- 서버 기준: `main@e85e718529283988771cd817e29a0aa46473c80d`
- 기존 미커밋 파일은 보존하고 임의로 병합하지 않는다.
- Meta ID, 테스트 사용자 이메일·비밀번호, access token, 업로드 token 원문은 출력하거나 문서·Git에
  기록하지 않는다.
- 이 문서 외의 새 문서를 만들지 않고 본 기준 문서를 갱신한다.

### 전달받은 실제 검증 결과

1. Meta Platform Standalone 테스트 사용자 로그인이 성공했다.
2. Platform SDK 초기화, entitlement 및 앱 범위 사용자 ID 조회가 성공했다.
3. 새 검증 세션에서 등록 `POST/GET`이 성공했고 Meta ID와 `sessionId`가 일치했다.
4. 로컬 JSONL과 서버 DB의 이벤트가 최종 `240/240`으로 일치했고 서버 세션 상태가
   `completed`였다.
5. `sequence` 누락과 중복은 없었다.
6. `appVersion` 수집이 확인됐다.
7. 종료 의미는 `application_quitting`이며 교육 과정의 `Completed`로 확대 해석하지 않는다.

위 항목은 최초에는 사용자 실행 결과로 전달됐다. 이후 서버 담당이 같은 로컬 환경에서 원문 ID를
출력하지 않고 로컬 조회 API, 등록 GET과 DB 기반 텔레메트리 조회 API를 재대조해 `240/240`,
`completed`, 등록 ID·`sessionId` 일치와 sequence 누락·중복 없음까지 독립 확인했다. 다만 이 결과는
아래 클라이언트 패치 전 실행 근거이며 패치 후 Editor 회귀 또는 Quest·APK 검증으로 확대하지 않는다.

### 재현 문제 1 — 등록 대기 180초 제한

- `TycheLocalTrainingRegistrationClient`가 앱 시작부터 Meta ID와 활성 PPE 세션을 함께 최대
  180초 기다린다.
- Meta ID는 이미 성공했지만 사용자가 컨트롤러 안내 단계에 머무르는 동안 제한 시간이 먼저 끝나
  `등록 조건을 기다리다 종료`가 발생했다.
- 이후 사용자가 Meta 인증 대기 시간을 분리하고 새 실행에서는 등록 성공을 확인했다.
- 요구 동작은 Meta 인증이 terminal failure 또는 `Skipped`일 때만 명확히 중단하고, ID 성공 뒤에는
  PPE 활성 단계 진입을 앱 수명 동안 기다리는 것이다.
- PPE 단계 진입 뒤 등록은 한 번만 수행하고 기존 `POST/GET` 일치 검증은 유지한다.
- 임의 자동 진행, 교육 흐름 변경 및 입력 소비자 변경은 금지한다.

### 재현 문제 2 — 종료 직전 이벤트의 다음 실행 업로드

- 첫 종료 직후 로컬 JSONL은 마지막 `sequence=240`이었으나 서버 checkpoint는 `235`였다.
- 미전송 `236~240`의 마지막 이벤트는 `session_ended/application_quitting`이었다.
- 다음 Play에서 기존 durable upload state를 읽어 `236~240`을 재전송했고 서버 세션을
  `completed`로 변경했다.
- 다음 실행의 새 세션에서도 종료 직후 `로컬 24 / 서버 23`이 재현됐고, 빠진 것은
  `sequence 24 session_ended/application_quitting`이었다.
- 원인은 5초 스캔 뒤 종료 직전 이벤트가 기록되고, 다음 스캔 전에 Play/Application이 종료되는 생명주기
  순서로 판단한다.
- 로컬 JSONL과 체크포인트 재전송 경로는 정상 동작했다.
- `application_quitting`에서 네트워크 성공을 보장한다고 주장하거나 종료를 블로킹하는 위험한 처리는
  추가하지 않는다.

### 구현 범위와 제한

- 종료 시 이벤트를 업로더가 즉시 또는 짧은 debounce로 인지하도록 개선할 수 있는지 검토한다.
- Quest의 pause/kill에서는 종료 콜백과 네트워크가 보장되지 않는 제한을 문서에 유지한다.
- 현재 Player 업로더의 비활성 정책을 임의로 해제하거나 APK에서의 동작을 추정하지 않는다. Player 인증은
  별도 후속 작업으로 유지한다.
- 변경 전 필수 질문 7개, `PPETrainingDataContractHarness`, `DocumentationPolicyHarness`, C# 정적
  컴파일 및 가능한 Unity Editor 검증을 이번 수정에서 수행한다.
- 결과는 `정적 확인 / Unity Editor 확인 / Quest·APK 미검증`으로 구분하고 남은 수동 검증을 명시한다.

### 변경 전 필수 질문 답변

1. **기존 Inspector/씬 작성값을 보존하는가?** 등록과 업로드 수명주기 코드는 씬 UI·Transform·입력
   작성값을 변경하지 않는다. `0_App`에 작성된 로컬 서버 주소, batch size, 5초 정기 스캔, 재시도와
   timeout 값도 보존한다.
2. **단일 기준 오브젝트와 상태 소유자는 무엇인가?** Meta 인증 상태는
   `MetaPlatformIdentityProbe`, PPE 활성 여부와 JSONL은 `PPETrainingTelemetryCapture`, 서버 ACK와
   durable checkpoint는 `TycheTrainingTelemetryUploader`가 각각 소유한다. 등록 클라이언트는 이 값을
   읽고 POST/GET을 한 번 수행할 뿐 상태를 만들지 않는다.
3. **입력 전체 경로는 무엇인가?** 이번 변경은 입력 이벤트, Interactor/Caster, Raycaster, Layer,
   Collider 및 press/select action을 소비하지 않는다. 기존 카드·모달·컨트롤러 교육 입력 경로는
   변경하지 않는다.
4. **실패 시 자동 수리 대신 멈춰야 하는가?** Meta probe가 `Failed`, `SkippedForEditorTesting`이거나
   SDK 경로가 사라지면 명확한 경고 후 등록만 중단한다. 서버·파일 오류는 기존 실패 로그와 durable
   재시도를 유지하며 씬이나 사용자 흐름을 자동 수정하지 않는다.
5. **함께 영향을 받는 소비자는 무엇인가?** 개발용 등록 POST/GET과 Editor 전용 텔레메트리 업로더만
   영향받는다. UI, 텔레포트, PPE Grab, 거울, XR 양안, 음성 및 출시 Player 업로드 정책은 보존한다.
6. **변경 전후 비교 실행은 무엇인가?** 변경 전 기준은 Meta ID 성공 뒤에도 앱 시작 180초에 등록이
   종료되는 재현과 종료 직후 `로컬 N / 서버 N-1`이다. 변경 후에는 180초가 지나 PPE 모드에 들어가도
   등록이 한 번 성공하는지, JSONL append 알림 뒤 다음 5초 폴링 전 서버 ACK가 진행되는지 비교한다.
7. **검증 수준은 어디까지인가?** 소스·씬 정적 계약, Runtime/Editor C# 컴파일과 가능한 Unity Editor
   하네스까지 수행한다. Meta Standalone 재로그인, 실제 DB POST/GET, Quest pause/kill 및 APK 업로드는
   새 실행 증거 없이는 완료로 표시하지 않는다.

이번 변경이 대응하는 사용자 요청은 `Meta 인증 제한 시간과 PPE 진입 대기를 분리`하고 `JSONL 새 이벤트를
업로더가 즉시 인지`하는 것이다. 보존해야 하는 기존 동작은 등록 1회, POST/GET 일치 검증, 5초 정기 스캔,
지수 재시도, durable 체크포인트, 종료 비블로킹 및 Player 업로드 비활성 정책이다.

### 실제 적용 내용

- `TycheLocalTrainingRegistrationClient`의 180초 제한을 Meta ID 확인에만 적용하도록 분리했다.
  Meta ID 성공 뒤에는 `PPETrainingTelemetryCapture`가 활성 PPE 세션과 `sessionId`를 제공할 때까지 앱
  수명 동안 기다린다. Meta probe가 실패·SDKless Editor `Skipped` 상태로 끝나거나 식별 경로가
  사라진 경우에만 등록을 명확히 중단한다.
- PPE 단계 진입 뒤 기존 등록 요청 작성, `POST`, `GET` 재조회 및 Meta ID·`sessionId` 일치 검증은
  그대로 한 번만 수행한다. 컨트롤러 안내, PPE 진행, 입력 및 UI 상태를 자동으로 진행하는 코드는
  추가하지 않았다.
- `PPETrainingTelemetryCapture`는 JSONL `File.AppendAllText`가 성공한 직후
  `TelemetryRecordAppended` 알림을 발생시킨다. 파일 append가 먼저이므로 알림 소비자가 실패해도 원본
  JSONL의 durable 복구 근거는 보존된다.
- Editor 전용 `TycheTrainingTelemetryUploader`는 이 알림을 구독하고 새 레코드를 0.2초 debounce로
  묶어 기존 5초 정기 스캔보다 먼저 확인한다. 서버 실패 뒤에는 append 알림도 기존 지수 재시도 시각
  `retryNotBefore`보다 앞서 재요청하지 못하도록 제한했다.
- `Application.isEditor` Player 가드, loopback 서버 제한, token 검증, durable 업로드 상태,
  checkpoint 재전송 및 종료 비블로킹 정책은 변경하지 않았다. 종료 콜백에서 네트워크를 기다리거나
  성공을 보장하는 처리를 추가하지 않았다.
- `PPETrainingDataContractHarness`에 Meta ID/PPE 대기 분리, append 알림, debounce 및 재시도 하한을
  확인하는 회귀 계약을 추가했다.

### 검증 결과

- **기준 커밋 확인:** 로컬 클라이언트는
  `main@8b1b4300349529a3ebcb260f1da614769bef6b34`, 로컬 서버는
  `main@e85e718529283988771cd817e29a0aa46473c80d`로 전달 기준과 일치했다.
- **정적 확인 PASS:** 결합 제한 상수 제거, Meta ID 전용 180초 제한, PPE 세션 무기한 대기,
  JSONL append 뒤 알림, 0.2초 debounce, 지수 재시도 하한, Player 업로드 비활성 가드를 소스에서
  확인했다. `git diff --check`도 오류 없이 통과했으며 줄바꿈 변환 경고만 있었다.
- **C# 정적 컴파일 PASS:** `Assembly-CSharp.csproj --no-restore`와
  `Assembly-CSharp-Editor.csproj --no-restore`는 모두 오류 0개였다. 출력된 경고는 기존 Unity API,
  DTO 및 소스 생성기 관련 경고이며 이번 변경으로 새 컴파일 오류는 발생하지 않았다.
- **Unity Editor 메뉴 PASS:** 사용자가 Play Mode 밖에서
  `Tools > PPE > Validate Training Data Contract`와
  `Tools > Documentation > Validate Authoring Policy`를 실행했다. `Editor.log`에서 PPE 데이터 계약
  PASS와 문서 작성 원칙 검증 통과 로그를 확인했다.
- **변경 후 Editor 대기 회귀 PASS:** Meta Standalone 초기화·entitlement·앱 범위 사용자 ID 조회 뒤
  `Meta ID 확인 완료. PPE 활성 세션 진입을 앱 수명 동안 기다립니다.` 로그가 출력됐고, 180초를
  넘긴 뒤에도 기존 결합 타임아웃 경고가 새로 발생하지 않았다. 이 실행은 PPE 진입 전에 종료했으므로
  동일 실행에서 180초 초과 뒤 등록까지 완료한 증거로 확대하지 않는다.
- **변경 후 Editor 등록 왕복 PASS:** 다음 실행에서 PPE 활성 상태에 진입해 `가입이 완료되었습니다.`
  로그를 확인했다. 서버 등록 GET의 Meta ID·연령 범주·`sessionId`가 같은 JSONL의 Meta identity와
  일치했고, 등록 당시 상태는 `TeleportInstruction`, 모드는 `Education`, 작업 계획은
  `ConfinedSpace`였다. 이 실행의 등록은 앱 시작 약 103초 뒤였으므로 180초 초과 사례는 아니다.
- **변경 후 Editor 즉시 ACK PASS:** 위 등록 실행에서 로컬 JSONL과 서버 조회가 58/58로 일치했고
  sequence 누락·중복은 0이었다. 최신 JSONL 파일 기록 시각과 durable checkpoint 파일 갱신 시각의
  차이는 약 0.27초로, 5초 정기 스캔 전 append 알림 경로가 동작했다. 확인 당시 세션은 실행 중인
  `open` 상태였으며 종료 이벤트나 과정 완료를 뜻하지 않는다.
- **변경 후 Editor 종료 복구 PASS:** Play Mode 종료 직후 로컬 JSONL에는
  `sequence=59`, `session_ended`, `application_quitting`이 기록됐지만 durable checkpoint와 서버는
  58에 머물렀고 서버 상태는 `open`이었다. 다음 Play 실행 약 10초 뒤 이전 세션은 로컬/서버 59/59,
  checkpoint 59, `completed`, 종료 이유 `application_quitting`으로 복구됐다. 양쪽 sequence 누락·중복은
  0이었다. 이는 다음 실행 durable 복구를 검증한 것이며 종료 콜백의 같은 실행 전송 보장은 아니다.
- **Quest·APK 미검증:** Quest pause/kill, 양안 및 Player 업로드는 확인하지 않았다. 특히
  `application_quitting` 직후에는 다음 프레임과 네트워크 완료가 보장되지 않으므로 마지막 이벤트가
  같은 실행에서 서버에 도달한다고 주장하지 않는다. 이 경우 다음 실행의 durable checkpoint 복구가
  계속 기준 경로다.

### 남은 수동 검증

1. 같은 실행에서 Meta ID 성공 후 컨트롤러 안내에 180초 이상 머문 다음 PPE 모드에 진입해 등록이
   정확히 한 번 수행되고 기존 POST/GET 일치 검증이 성공하는지 확인한다. 180초 초과 대기 유지와
   PPE 진입 등록은 각각 확인했지만 한 실행으로 연결한 증거는 아직 없다.
2. Quest·APK에서 pause/kill 및 Player 업로드 정책을 별도 설계·검증한다. 현재 Player 업로드 비활성
   정책과 Editor의 다음 실행 복구 결과를 Quest 전송 성공으로 확대하지 않는다.

## 후속 계획: Quest APK 로컬 LAN 연동 1단계

### 상태와 목표

- **상태:** 클라이언트 1단계 구현·정적 검증 완료 / 서버 준비·통합 검증 미완료 / 실제 Quest 미검증
- **목표:** 같은 Wi-Fi에 연결된 Quest의 Android Development APK가 PC에서 실행 중인 서버로 훈련
  등록과 텔레메트리를 전송하고, 정상 종료·pause/resume·강제 종료 복구를 실제 기기에서 구분 검증한다.
- **범위 제한:** 이번 단계는 내부 개발 검증용이다. 운영 HTTPS 배포, Meta 기반 서버 인증 및 출시
  Player 정책은 이 결과만으로 완료 처리하지 않는다.

### 구현 원칙

1. 기존 Editor loopback 경로를 보존하면서 Android Development Build에만 명시적인 LAN 설정이
   있을 때 Player 전송을 허용한다. 설정이 없으면 임의 주소나 토큰으로 계속하지 않고 설정명과 실패
   지점을 보고한다.
2. PC의 LAN 주소와 개발용 업로드 토큰을 코드·씬·문서에 고정하지 않는다. 실제 값은 저장소 밖에서
   빌드 시 공급하고, 내부 테스트용·폐기 가능한 값으로만 사용한다. Release Build에서는 이 개발 경로가
   자동 활성화되지 않아야 한다.
3. Android에서 HTTP cleartext 허용이 필요하면 Development Build와 사설 LAN 검증으로 제한한다.
   출시 빌드의 HTTPS 정책을 약화하지 않는다.
4. 기존 JSONL 선기록, append 알림, durable checkpoint, 지수 재시도와 다음 실행 복구를 유지한다.
   `OnApplicationPause(true)`는 완료로 해석하지 않고 pause 전송도 best-effort로만 취급한다.
5. 코드에 실제로 연결된 정상 훈련 완료 또는 기존 종료 경로가 확인되면 앱이 살아 있는 동안
   `session_ended`와 `/complete`를 먼저 전송한다. 확인되지 않은 종료 버튼·원인·과정 완료 의미는
   새로 만들지 않는다.
6. 강제 종료 순간의 네트워크 성공을 보장한다고 주장하지 않는다. 로컬 보존과 다음 실행 재전송을
   기준으로 하고, 재실행되지 않은 장기 미갱신 세션은 향후 서버에서 `completed`와 구분되는 상태로
   다루는 방안을 별도 설계한다.
7. 실제 토큰, Meta 앱 범위 사용자 ID, 이메일과 자격 증명을 저장소·문서·로그에 남기지 않는다.

### 클라이언트 작업 범위

- `TycheTrainingTelemetryUploader`와 `TycheLocalTrainingRegistrationClient`의 Editor/Player 가드,
  서버 주소, 토큰 공급 및 Android 수명주기 처리를 조사한다.
- Android Development Build의 명시적 LAN 설정 경로를 추가하되 Editor 경로, 등록 1회, POST/GET
  일치 검증, 실행 중 즉시 ACK와 durable 복구를 보존한다.
- 정상 종료 연결점은 실제 씬·런타임 흐름을 확인한 뒤 연결한다. 기존 의미를 확인할 수 없으면 구현을
  추측하지 않고 blocker로 기록한다.
- 관련 Unity 하네스, C# 정적 컴파일과 `git diff --check`를 수행한다. 실제 기기에서 실행하지 않은
  항목은 정적 PASS와 분리한다.

### 서버 준비 범위

- 서버가 PC의 사설 LAN 인터페이스에서 수신 가능한지, Quest에서 접근할 포트와 Windows 방화벽 범위를
  확인한다. `127.0.0.1`은 Quest에서 PC를 가리키지 않는다.
- 기존 등록·텔레메트리 API 계약을 유지하고, 필요한 설정은 실제 값 없이 `.env.example`에 이름과
  용도만 기록한다. 서버 바인딩·방화벽·DB·운영 설정은 정적 확인과 실제 변경을 구분한다.
- 내부 개발용 Bearer token을 운영 인증으로 확대하지 않는다. 운영 단계에서는 APK에 장기 공용 토큰을
  넣지 않는 단기 세션 인증과 HTTPS를 별도 설계한다.

### 실제 Quest 합격 기준

1. `0_App`에서 Meta 앱 범위 ID 조회가 성공한다.
2. 등록 POST/GET의 Meta ID와 `sessionId`가 같은 기기 세션과 일치한다.
3. 실행 중 로컬 JSONL sequence와 서버 이벤트·checkpoint가 누락·중복 없이 진행된다.
4. 확인된 정상 종료 경로에서는 앱이 살아 있는 동안 서버 세션이 `completed`로 닫힌다.
5. 홈 버튼 pause/resume은 완료로 오인되지 않고 복귀 뒤 전송이 계속된다.
6. 네트워크 단절 또는 강제 종료 뒤 로컬 원본이 남고 다음 실행에서 checkpoint가 복구된다.
7. 설정이 없는 Development Build와 Release Build는 개발용 LAN 경로를 조용히 활성화하지 않는다.

검증 결과에는 클라이언트·서버 기준 브랜치와 커밋 SHA, 변경 파일, 설정 공급 방법, 정적 검증,
실제 Quest 검증 및 남은 수동 검증을 각각 기록한다. 명시적 요청 전에는 커밋·배포·운영 변경을 하지 않는다.

### 2026-08-30 클라이언트 구현 전 필수 질문

1. **기존 Inspector/씬 작성값을 보존하는가?** `0_App`의 Editor loopback 주소, batch size, 5초 정기
   스캔, 최대 재시도 및 timeout을 유지한다. Quest 주소와 token을 새 씬 필드로 저장하지 않는다.
2. **단일 기준과 상태 소유자는 무엇인가?** JSONL과 pause/resume 원본 이벤트는
   `PPETrainingTelemetryCapture`, 전송·checkpoint·재시도는 `TycheTrainingTelemetryUploader`, Meta 등록
   POST/GET은 `TycheLocalTrainingRegistrationClient`가 소유한다. Development LAN 설정은 업로더의
   프로세스 수명 정적 캐시 한 곳에서 두 소비자에게 제공한다.
3. **입력 전체 경로는 무엇인가?** 이번 변경은 XR 입력, Interactor/Caster, Raycaster, Layer, Collider,
   카드·모달 및 PPE Grab 입력을 사용하거나 변경하지 않는다. Android `OnApplicationPause` 수명주기와
   JSONL append 알림만 전송 스캔을 깨운다.
4. **실패 시 자동 수리 대신 멈추는가?** 명시적인 임시 설정이 없거나, 주소가 사설 IPv4가 아니거나,
   token 형식이 잘못됐거나, 임시 파일 삭제에 실패하면 주소 fallback 없이 등록·업로드를 중단하고 설정명과
   실패 단계만 알린다. 실제 주소·token·Meta ID 및 응답 본문은 로그에 출력하지 않는다.
5. **함께 영향을 받는 소비자는 무엇인가?** Editor loopback 등록/업로드와 Android Development APK의
   LAN 등록/업로드만 영향받는다. Release Player, 운영 HTTPS, 교육 완료 의미, UI, 음성 및 XR 렌더링은
   보존한다.
6. **변경 전후 비교 실행은 무엇인가?** 변경 전 Android Player는 업로더를 항상 중단하고 등록 주소는
   loopback 기본값에 머문다. 변경 후에는 설정 없음·공인/loopback 주소·Release를 각각 거부하고,
   명시적으로 주입한 Development APK만 사설 LAN으로 등록 1회와 durable 업로드를 수행하는지 비교한다.
7. **검증 수준은 어디까지인가?** 소스 계약, 생성 Gradle manifest의 Development gate, C# Runtime/Editor
   컴파일과 가능한 Unity 메뉴를 정적으로 검증한다. 실제 Quest Meta ID, LAN POST/GET, checkpoint,
   pause/resume, 정상 완료 및 강제 종료 복구는 기기 실행 전까지 미검증으로 둔다.

이번 구현은 앱 내부 임시 설정 파일을 사용한다. Unity Editor 메뉴가 저장소 밖 환경 변수
`TYCHE_QUEST_LAN_SERVER_BASE_URL`과 `TYCHE_TELEMETRY_UPLOAD_TOKEN`을 읽고 ADB 프로세스의 표준입력으로
Development APK의 내부 `files/.tyche-development-lan.json`에 전달한다. token은 명령행 인수, APK,
씬, PlayerPrefs와 Git 파일에 넣지 않는다. 앱은 파일을 한 번 읽고 삭제에 성공한 경우에만 메모리에
유지한다. 서버 주소는 `http(s)://사설 IPv4:port` 형식만 허용한다.

### 2026-08-30 클라이언트 1단계 적용 결과

#### 적용한 변경

- `TycheTrainingTelemetryUploader`는 기존 Unity Editor loopback 경로를 유지하고,
  `UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR`에서만 Quest LAN 설정을 읽는다. 주소는
  명시적 port가 있는 RFC1918 사설 IPv4의 HTTP(S)만 허용하며 loopback, 공인 IP, DNS 이름,
  user info, query와 별도 path는 거부한다.
- Unity 메뉴 `Tools > PPE > Inject Quest Development LAN Configuration`을 추가했다. 메뉴는 저장소 밖
  환경 변수 `TYCHE_QUEST_LAN_SERVER_BASE_URL`과 `TYCHE_TELEMETRY_UPLOAD_TOKEN`을 읽고, 설치된
  debuggable Development APK를 중지한 뒤 ADB 표준 입력으로 앱 내부 일회성 파일에 전달한다. 실제 주소와
  token은 명령행·로그에 넣지 않는다. 앱은 설정을 읽은 뒤 파일 삭제를 확인해야만 값을 메모리에 유지한다.
- Android Gradle 후처리는 Development Build의 생성 Manifest에만
  `android:usesCleartextTraffic="true"`를 쓰고, Release 생성 Manifest에는 `false`를 명시한다. 프로젝트
  원본 Manifest나 Release HTTPS 정책을 약화하지 않는다.
- `TycheLocalTrainingRegistrationClient`는 Editor에서는 기존 loopback PlayerPrefs를 사용하고,
  Android Development APK에서는 업로더와 같은 일회성 설정을 사용한다. 등록 `POST/GET`, Meta ID와
  `sessionId` 일치 검증, 등록 1회 동작은 유지했다.
- 등록과 텔레메트리 HTTP 실패 로그는 실제 서버 주소와 응답 본문 대신 API path, HTTP status와 일반 오류만
  남기도록 제한했다. Meta ID, token과 실제 LAN 주소를 새 로그에 출력하지 않는다.
- `PPETrainingTelemetryCapture.OnApplicationPause`는 `application_paused`와 `application_resumed`를
  비종료 JSONL 이벤트로 기록한다. 업로더는 pause에서 durable queue 즉시 스캔을 best-effort로 요청하고,
  기존 지수 재시도 하한을 우회하지 않는다. pause를 `session_ended`나 `/complete`로 해석하지 않는다.
- JSONL 선기록, append 알림, 0.2초 debounce, 5초 정기 스캔, durable upload-state/checkpoint, 지수 재시도,
  다음 실행 복구 및 `application_quitting` 분리 정책은 유지했다.

#### 정상 완료 연결 판단

- 현재 JSONL 세션은 앱 수명 단위이고 `PPEFinaleController`의 과정 완료 뒤에도 모드 선택으로 돌아가 같은
  앱 세션에서 다음 교육을 계속할 수 있다. 이 시점에 앱 세션을 `completed`로 닫으면 이후 이벤트가 기존
  upload-state에 의해 무시될 수 있다.
- 따라서 확인되지 않은 “정상 앱 종료” 버튼이나 완료 의미를 새로 만들지 않았고, 과정 완료에
  `session_ended`와 `/complete`를 임의 연결하지 않았다. 현재 확정된 terminal 이벤트는 기존
  `application_quitting`뿐이다. 앱 수명 세션과 과정별 세션을 분리하거나 명시적 정상 종료 UX를 확정하는
  후속 설계가 필요하다.

#### 정적 검증 결과와 서버 후속 요구

- `PPETrainingDataContractHarness`에 Development Build compile gate, 일회성 파일 삭제, 사설 IPv4 제한,
  ADB 표준 입력, Development/Release cleartext 분기, pause/resume 비종료 이벤트와 등록 설정 공유 검사를
  추가했다.
- `Assembly-CSharp.csproj --no-restore`는 경고 91개, 오류 0개였고
  `Assembly-CSharp-Editor.csproj --no-restore`는 경고 34개, 오류 0개였다. 경고는 기존 Unity API,
  source generator와 DTO 관련 항목이며 새 컴파일 오류는 없었다. `git diff --check`도 오류 없이 통과했고
  줄바꿈 변환 경고만 있었다.
- 서버 코드·DB·실행 프로세스·방화벽·배포는 이번 클라이언트 작업에서 변경하지 않았다. Quest 연결 전 서버가
  사설 LAN 인터페이스에서 수신하는지, 대상 port의 Windows 방화벽이 개발 PC의 사설 네트워크 범위에서
  허용되는지 서버 측에서 확인해야 한다. 서버가 `127.0.0.1`에만 bind되어 있으면 클라이언트에서 우회하지
  않고 서버 준비 미완료로 보고한다.
- Unity 메뉴 하네스, 실제 ADB 주입, Development APK 생성 Manifest, Meta ID 등록 POST/GET, JSONL ACK,
  pause/resume, 네트워크 단절·강제 종료 복구와 Quest 양안은 아직 실행하지 않았다. 정적 PASS를 실제 기기
  통합 성공으로 확대하지 않는다.

#### 초보자용 실제 검증 순서

1. PC와 Quest를 같은 신뢰 가능한 Wi-Fi에 연결하고 PC의 사설 IPv4를 확인한다.
2. 서버를 실행하고 사설 LAN bind 및 대상 port의 Windows 방화벽 허용 여부를 서버 담당자가 확인한다.
3. Unity를 Android Development Build로 빌드해 같은 package identifier의 debuggable APK를 Quest에
   설치한다. Release 빌드는 이 경로의 검증 대상이 아니다.
4. 실제 값은 저장소 파일에 쓰지 않고 PC 사용자 환경 변수
   `TYCHE_QUEST_LAN_SERVER_BASE_URL`, `TYCHE_TELEMETRY_UPLOAD_TOKEN`에 설정한다.
5. Unity에서 `Tools > PPE > Inject Quest Development LAN Configuration`을 실행한다. PASS 후 Quest에서
   앱을 직접 시작한다. 설정은 앱이 한 번 읽으면 삭제되므로 각 새 실행 전 다시 주입한다.
6. Meta ID 등록 POST/GET, 같은 `sessionId`, 실행 중 JSONL sequence/서버 checkpoint, 홈 버튼
   pause/resume, 네트워크 단절 후 복구와 강제 종료 다음 실행 복구를 순서대로 확인한다. 실제 ID와 token은
   화면 캡처·문서·로그에 옮기지 않는다.

### 2026-08-31 종료 시점 인수인계

#### 기준과 완료 범위

- 이 절 작성 전 서버 기준은 `main@dd9f6be462f780241eacef1d9fb9b1a10f188f0a`다. 문서 커밋 전
  클라이언트 구현 기준 HEAD는 `main@8b1b4300349529a3ebcb260f1da614769bef6b34`지만 Unity 코드·씬·하네스 변경이 작업 트리에 남아
  있으므로 HEAD만으로 오늘 클라이언트 구현 상태를 재현할 수 있다고 주장하지 않는다.
- Unity Editor Standalone 테스트 사용자로 Platform 초기화, entitlement와 앱 범위 사용자 ID 조회를
  확인했다. 로컬 등록 POST/GET과 Meta ID·`sessionId` 일치도 확인했으며 실제 식별자와 자격 값은 문서에
  기록하지 않았다.
- Meta ID 확인의 180초 제한과 PPE 활성 세션 대기를 분리한 변경 후, 180초를 넘겨도 대기가 유지되는 실행과
  PPE 진입 뒤 등록되는 실행을 각각 확인했다. 같은 한 실행에서 `180초 초과 → PPE 진입 → 등록 1회`를
  연결한 검증은 아직 남아 있다.
- Editor 종료 직전 `application_quitting` 이벤트가 같은 실행에서 서버에 도달하지 못한 사례는 다음 Play의
  durable checkpoint 재전송으로 로컬/서버 `59/59`, 누락·중복 0, 서버 `completed`까지 복구됨을 확인했다.
  이를 Android 강제 종료나 Quest kill에서의 전송 보장으로 확대하지 않는다.
- 최종 Unity 메뉴 확인에서 `PPE Training Data Contract`는 PPE 항목 binding 25개로 PASS했고,
  `Tyche Telemetry Upload Setup`은 `0_App`의 `AppMain`에 로컬 DB 업로더 1개가 연결된 상태로 PASS했다.
  이는 코드·씬·Editor 설정 검증이며 APK 실행 성공 근거는 아니다.
- 개발 PC와 Quest는 같은 사설망에서 ADB 승인 및 TCP 3000 접근을 확인했다. Windows 방화벽은 현재 개발 PC
  주소와 로컬 서브넷의 TCP 3000만 허용하도록 준비했고, Quest Development LAN 주소와 업로드 token은
  저장소 밖 사용자 환경 변수에만 두었다. Unity가 환경 변수를 다시 읽도록 설정 뒤 재시작한 상태를 기준으로
  한다.

#### 로컬 데이터와 용량 확인

2026-08-31 로컬 MySQL을 읽기 전용으로 조회한 전체 스냅샷은 참여자 2, identity 2, 세션 23,
이벤트 1,080건이다. 이벤트 `payload_json`은 평균 790바이트, 최대 1,096바이트, 합계 852,705바이트였고
텔레메트리 4개 테이블의 InnoDB data·index 할당량은 약 2MB였다. 이는 현재 로컬 DB 전체 상태이며
Quest APK 한 세션의 용량이나 운영 사용량으로 확대하지 않는다.

현재 클라이언트 씬의 batch size는 25, 정기 스캔은 5초, 최대 재시도 간격은 60초다. 서버는 이벤트 요청당
최대 50개와 JSON 요청 본문 1MB를 제한하고, `eventId`와 세션 sequence 고유 제약으로 동일 재전송을
중복 저장하지 않는다. 반면 텔레메트리 자동 보존기간·정리 작업, 호출 속도 제한, 세션당 총 이벤트 상한과
긴 세션 상세 조회 pagination은 아직 없다. 소규모 개발 검증에는 즉시 문제가 될 크기가 아니지만 운영
배포 전에는 실제 Quest 세션 표본으로 일·월·연 용량을 다시 계산하고 이 네 가지 운영 방어선을 확정한다.

#### 웹 개인정보처리방침과 운영 상태

- VR 상세페이지 푸터의 `VR PRIVACY POLICY`는 공개 GitHub Pages 정책으로 연결되고 새 창
  `target="_blank"`, `rel="noopener noreferrer"`로 운영 반영됐다. 상세페이지·정책 페이지·헬스체크
  HTTP 200과 운영 자동 테스트 21개, audit 취약점 0건, Nginx 설정 통과를 확인했다.
- 서버 `main`의 최신 링크 반영은 `25cf5aa6e7b845c9be2a90a5bb9f34df47677dae`, 운영 전용 브랜치의
  최신 배포는 `ba93ee0ebe104bae4b7ec39c56fc0d328c4200eb`다. 상세 운영 기록은 서버 저장소의
  `Docs/ProductionDeployment.md`를 기준으로 한다.
- 운영 서버는 현재 위 개인정보 링크 전용 브랜치이며 텔레메트리 route와 migration `009`~`012`가 없다.
  따라서 현재 로컬 DB 검증 데이터를 운영 서버가 수집하거나 운영 DB에 누적하는 상태가 아니다. 운영
  텔레메트리 배포, 운영 DB migration, PM2 재시작과 운영 데이터 변경은 수행하지 않았다.

#### 2026-09-01 재개 순서

1. 클라이언트 Codex 작업이 끝난 뒤 `git status`, 변경 파일과 최종 커밋 SHA를 먼저 확인한다. 현재 미커밋
   Unity 변경을 임의로 덮어쓰거나 서버 문서만 보고 구현 완료로 판단하지 않는다.
2. Unity 컴파일 오류가 없는 상태에서 Android **Development Build**를 생성한다. 아직 APK를 빌드하지
   않았으므로 기존 Editor PASS를 APK 빌드 PASS로 표시하지 않는다.
3. 승인된 Quest에 같은 package identifier의 Development APK를 설치한다. 앱을 실행하기 전
   `Tools > PPE > Inject Quest Development LAN Configuration`을 실행해 일회성 내부 설정을 주입한다.
   앱이 설정 파일을 읽고 삭제하므로 새 실행 검증 전에는 다시 주입한다.
4. HMD에서 `0_App`부터 시작해 Meta 앱 범위 ID, 등록 POST/GET, PPE 진입과 텔레메트리 전송을 진행한다.
   실제 Meta ID, token과 전체 응답 본문은 캡처·문서·Git에 남기지 않는다.
5. 실행 중에는 해당 세션의 로컬 JSONL sequence, 서버 event count와 checkpoint를 비교한다. 홈 버튼
   pause/resume이 `application_paused`·`application_resumed`로 남고 세션 완료로 오인되지 않는지 확인한다.
6. 정상 종료와 강제 종료를 구분한다. 종료 직전 꼬리 이벤트가 남으면 같은 실행 전송 성공을 강제하지 않고,
   다음 실행에서 기존 세션의 checkpoint·event count·`completed`가 누락·중복 없이 복구되는지 확인한다.
7. 실제 Quest 한 세션의 이벤트 수, JSON payload 합계와 DB 증가량을 기록한 뒤 예상 일일 세션 수를 적용해
   보존기간, 정리 방식, rate limit, 세션 이벤트 상한과 상세 조회 pagination을 결정한다.
8. Development LAN 통합이 통과한 뒤에만 운영 HTTPS, Meta User Proof 기반 단기 인증과 운영 배포 계획으로
   넘어간다. 개발용 장기 공용 token과 cleartext LAN 설정을 Release APK에 포함하지 않는다.

재개 시 완료 표시는 `클라이언트 코드 커밋`, `Development APK 빌드·설치`, `Quest 실제 수집`,
`서버·DB 조회`, `종료·재실행 복구`, `운영 배포`를 각각 분리한다.

## 2026-09-01 제출 브랜치 정리와 Quest LAN 통합 재개 기록

### 기준 브랜치와 보존 상태

- 전체 삭제 전 기준본은 클라이언트
  `archive/2026-09-01-client-full-no-delete@506664fb39a410d31e8620be59b920b7c8683bbc`에 있으며,
  로컬과 `origin`이 같은 SHA를 가리킨다. 이 보존 브랜치에는 이후 정리 작업을 추가로 푸시하지 않는다.
- 현재 정리 작업 브랜치는
  `release/2026-09-01-meta-horizon-submission@506664fb39a410d31e8620be59b920b7c8683bbc`다.
  정리 변경은 아직 커밋하거나 푸시하지 않았다.
- 씬 기준 이름은 `0_App`, `1_Title`, `2_Intro`, `3_Loading`, `4_PPE_Room`, `5_MixerRoom`,
  `6_InsideMixer`다. 앞의 다섯 씬만 Build Settings에서 활성화됐고 Mixer 두 씬은 비활성 후속 씬이다.
- 현재 작업 트리에는 삭제 1,932개와 프로젝트 설정 변경이 남아 있다. 삭제 범위는 미참조 후보 모델,
  중복 RPG 복구 백업, Unity 복구 씬 사본 `Assets/_Recovery`, `SnipAutoSaver`, 완료된 일부 PPE 일회성 도구다. `PlaneMaker`, Ghost Hand,
  HandTest, 외부 임포트 도구와 회귀 진단·복구 도구는 보존한다.
- `.codex-tmp`, 성능 테스트 설정 JSON, PPT 및 `qa` 경로의 미추적 파일은 이 정리 작업의 소유로
  판단하지 않았고 삭제·수정·커밋 대상에 포함하지 않는다.

### 삭제 의존성 확인과 정적 검증

- 삭제 `.meta` GUID의 씬·프리팹·머티리얼 참조를 조사했다. 삭제된 RPG 백업의 Hangar GUID 한 건은
  현재 유효한 원본 자산이 같은 GUID로 남아 있어 미해결 참조로 분류하지 않았다.
- `5_MixerRoom`의 건물, 바닥, Hangar, Support set, Asphalt 관련 핵심 자산과 GUID를 별도로 확인했고
  현재 원본 경로와 프리팹 연결을 보존했다.
- 이전 씬 이름 문자열은 하네스의 금지 토큰 검사 외에 남지 않았고, Build Settings의 순서·활성 상태와
  씬 전환 직렬화 참조가 현재 이름과 일치한다.
- Unity에서 `[Scene Dependency Validation] PASS`, `[Meta Quest Android Build] PASS`,
  `[PPE Training Data Contract] PASS`를 확인했다. PPE 계약은 `4_PPE_Room`의 장비 binding 25개와
  교육·훈련·테스트 데이터 계약을 확인했다. 이 결과는 정적 계약 PASS이며 실제 Quest 서버 수신 PASS는 아니다.
- C# Editor 프로젝트 정적 컴파일은 오류 0개였고 기존 경고만 남았다. HMD 미연결 상태에서 관찰했던
  AnimatorController 및 Hand Tracking/OpenXR 경고는 삭제 GUID, `MissingReference`, `NullReference` 오류와
  구분했다.

### Release Build And Run과 HMD 확인

- 첫 Build And Run은 이전 Company/Product 값으로 성공했고 이후 작성값이 원래 저장소 값이었다는 사실을
  확인했다. 빌드 실행기가 이름을 되돌린 것이 아니었다.
- `ProjectSettings/ProjectSettings.asset`의 작성값을 Company Name `Tyche works`, Product Name
  `Chemical Safety Training VR`로 변경했다. Android package identifier는 기존
  `com.softcastella.prototype.tyche.jinyoung`을 유지한다.
- 두 번째 Release Build And Run 결과물
  `Builds/Android/ChemicalSafetyVR_SubmissionCandidate_20260901.apk`는 220,538,552바이트다.
  Manifest 확인 결과 versionName `0.1.0`, minSdk 25, targetSdk 36, Quest VR category 및 필수
  head tracking 선언이 들어 있다.
- Quest 2 `1WMHHA63DE2502`에 2026-09-01 15:10:29 설치됐고 사용자가 HMD에서 앱이 플레이되는 것을
  확인했다. 이 확인은 실행·표시 확인이며 전체 PPE 기능 체크리스트와 서버 수신 검증을 모두 통과했다는 뜻은 아니다.
- 기기에서 확인한 설치 APK는 `DEBUGGABLE` 플래그가 없는 Release다. 따라서 아래 Development 전용 LAN
  업로드 경로는 이 설치본에서 활성화되지 않는다. Release 보안 정책이 유지된 결과다.
- APK 파일 크기는 약 220.5MB이고 Quest의 설치 패키지 영역은 약 333MB였다. 프로젝트 약 20GB의 주된
  구성은 `Library` 약 15.16GB와 `Builds` 약 3.48GB이며, Editor 도구·문서·전체 프로젝트가 APK에 그대로
  들어간 결과가 아니다.
- Development 빌드 재시도 전 C 드라이브 여유가 약 661MB뿐인 것을 확인했다. 사용자가 현재 로컬 APK는
  없어도 된다고 승인한 범위에서 APK 3개와 Unity 빌드 부속 폴더 5개를 직접 삭제해 약 3.48GB를 확보했다.
  Quest에 설치된 Release 앱은 유지된다. 삭제한 로컬 빌드 결과는 휴지통 복구 대상이 아니며 필요하면 같은
  소스에서 다시 빌드한다.

### 서버 준비와 교차 저장소 상태

- 2026-09-01 재확인한 서버 기준은
  `main@e20e59e5a1c209e66cda5b604f4815540d6a24a0`이다. 로컬 서버 브랜치는 `origin/main`보다 3개
  커밋 뒤에 있고 텔레메트리 서비스·테스트 등 미커밋 변경이 있으므로 HEAD만으로 현재 실행 상태를 재현했다고
  주장하지 않는다. 서버 작업 트리는 이번 클라이언트 작업에서 수정하거나 덮어쓰지 않았다.
- 현재 서버 작업 트리에서 등록·텔레메트리 대상 테스트 16개를 실행해 16개 통과, 실패 0개를 확인했다.
- 서버 `.env`에는 수집 활성화와 16자 이상 업로드 token이 값 노출 없이 확인됐다. PM2의
  `tyche-safety-training-server`를 시작했고 `http://127.0.0.1:3000/api/health`와 개발 PC 사설 LAN 주소의
  같은 health 경로가 HTTP 200을 반환했다.
- 위 결과는 서버 수신 준비와 API 계약 확인이다. 2026-09-01 Quest 실행에서 새 세션이 서버·DB에 들어왔다는
  결과는 아직 없다.

### 중단된 Development Build와 근본 원인

- 프로젝트 용량 증가를 피하려고 Development APK 출력 위치를 프로젝트 밖
  `C:/Users/Public/Documents/ESTsoft/CreatorTemp/TycheQuestLanDevelopment`로 지정했다.
- 일회성 Editor 실행기로 Scene Dependency, Meta Quest Android Build, PPE Training Data Contract를
  차례로 PASS한 뒤 `BuildOptions.Development | BuildOptions.AutoRunPlayer` 빌드를 시작했다.
- Player script assembly 생성 단계에서 `GC: PER_HEAP_ISOLATED data members initialization failed`,
  `GC heap initialization failed with error 0x80004005`, `Failed to create CoreCLR`이 반복됐다. Unity는
  `errors=11`, `warnings=3`, 결과 `Failed`로 종료했다. 같은 시각 Windows 명령에서도 페이징 파일 부족
  오류가 확인돼, 현재 근거상 삭제 자산이나 C# 코드 오류가 아니라 시스템 commit/pagefile 여유 부족이
  직접 실패 원인이다.
- Development APK와 Unity 백업 폴더는 생성되지 않았고 Quest에는 기존 Release APK가 그대로 남아 있다.
  LAN 설정 주입, Development 앱 실행, Quest JSONL 생성 대조 및 서버·DB 수신 검증도 아직 실행하지 않았다.
- 빌드에만 사용한 `OneShotQuestLanDevelopmentBuildRunner.cs`와 `.meta`는 실패 근거를 확인한 뒤 제거했다.
  런타임 코드, 씬, 프리팹과 Inspector 작성값은 이 시도로 변경하지 않았다.

### 다음 재개 순서와 완료 게이트

1. Unity와 불필요한 메모리 사용 프로세스 상태를 확인하고 Windows commit/pagefile 여유를 확보한다.
   원인 확인 없이 C# 코드나 씬을 수정해 빌드 실패를 우회하지 않는다.
2. 동일 세 하네스를 다시 실행한 뒤 프로젝트 밖 임시 경로에 Android Development APK를 빌드하고 Quest에
   설치한다. BuildReport 성공과 기기의 `DEBUGGABLE` 플래그를 각각 확인한다.
3. 서버 `.env`의 기존 token과 개발 PC 사설 LAN 주소를 값 노출 없이 사용해 Quest 내부 일회성 설정을
   주입한다. 실제 주소와 token을 씬, APK, 문서, Git 또는 명령행 출력에 남기지 않는다.
4. 사용자는 HMD에서 `0_App → 1_Title → 2_Intro → 3_Loading → 4_PPE_Room`을 플레이한다. 자동 빌드,
   설치, 설정 주입, 앱 재실행 및 서버·DB 조회는 클라이언트 작업자가 수행한다.
5. Meta 등록 POST/GET, 같은 `sessionId`, 로컬 JSONL sequence, 서버 event count·checkpoint를 대조한다.
   pause/resume과 강제 종료 뒤 다음 실행 복구는 정상 실행과 구분해 검증한다.
6. Development LAN 통합 PASS 뒤 최종 Release APK를 다시 설치하고 전체 HMD 기능·양안·오디오·입력 검증을
   마친다. 그 전에는 정리 삭제분을 커밋하거나 푸시하지 않는다.
7. 최종 통과 뒤에만 의도한 삭제와 Company/Product 설정만 선별 스테이징해 한글 제목과 번호 목록 본문으로
   커밋하고 `release/2026-09-01-meta-horizon-submission`에 푸시한다. 보존 브랜치에는 푸시하지 않는다.

이 절은 클라이언트 저장소의 공용 문서 기준본에 먼저 기록했다. 서버 저장소의 같은 상대 경로 미러는 서버
작업 트리의 미커밋 변경을 덮어쓰지 않도록 별도 동기화가 필요하다.

### 2026-09-01 Development 재빌드와 Quest 복구 전송 결과

#### 빌드 실패 원인 제거와 최종 Development 설치

- 첫 실패 뒤 프로젝트의 기존 로컬 APK 3개와 Unity 빌드 부속 폴더 5개를 승인 범위에서 제거해 약
  3.48GB를 확보했다. 두 번째 Development 빌드는 CoreCLR 단계를 통과했지만 Gradle의
  `:unityLibrary:copyDebugJniLibsProjectOnly`에서 `libil2cpp.so` 복사 중 디스크 공간 부족으로 실패했다.
- `C:/Users/Public/Documents/ESTsoft/CreatorTemp/codex_publish`의 이전 버전 클론
  `Prototype_Tyche_Jinyoung`과 `One_Loop_Original`은 사용자가 삭제해도 되는 예전 버전이라고 명시한 뒤
  제거했다. `One_Loop_Original`의 미커밋 변경 4개는 직접 삭제되어 복구할 수 없고 원격 기준본만 다시
  clone할 수 있다. 이후 사용자가 저장소 밖의 추가 공간을 정리해 C 드라이브 여유는 약 27.74GB가 됐다.
- GC 서버 모드를 빌드 프로세스에만 임시로 끄고 세 하네스를 다시 실행한 결과 모두 PASS했으며,
  Android Development Build And Run이 성공했다. Quest에 설치된 앱의 `DEBUGGABLE` 플래그를 확인했다.
- 최초 Development 실행은 생성 Manifest의 `usesCleartextTraffic=true`만으로는 충분하지 않아
  `InvalidOperationException: Insecure connection not allowed`가 발생했다. 프로젝트의
  `PlayerSettings.insecureHttpOption`을 `DevelopmentOnly`로 변경한 뒤 다시 빌드했다. Unity Editor에서
  실제 열거형 값이 `DevelopmentOnly`임을 확인했으며 Release에서는 HTTP가 계속 차단된다.
- 최종 Development APK는 프로젝트 밖
  `C:/Users/Public/Documents/ESTsoft/CreatorTemp/TycheQuestLanDevelopment/ChemicalSafetyVR_QuestLanDevelopment_20260901.apk`에
  생성됐고 크기는 246,820,521바이트다. Build And Run 성공 뒤 Quest에 재설치됐다.
- 빌드 전용 `OneShotQuestLanDevelopmentBuildRunner.cs`와 `.meta`는 최종 성공 근거를 확인한 뒤 제거했다.
  이 임시 실행기는 제출 브랜치 커밋 대상에 포함하지 않는다.

#### Quest 중간 종료와 서버 복구 검증

- 사용자는 HMD에서 `0_App → 1_Title → 2_Intro → 3_Loading → 4_PPE_Room`까지 진행하고 PPE 모드를
  시작했으나 전체 흐름 완료 전에 앱에서 나왔다. 이 실행은 최종 정상 완료 실기 검증으로 표시하지 않는다.
- 종료 직후 Quest에는 `session_ended`를 포함한 JSONL이 남았지만 프로세스 종료 전에 서버 전송을 마치지
  못했다. 일회성 LAN 설정을 다시 주입하고 앱을 재실행하자 업로더가 기존 파일을 복구 전송했다.
- 복구 후 Quest 로컬 이벤트 112개, 서버 상세 조회 이벤트 112개가 일치했다. 업로드 상태는
  `acceptedThroughSequence=112`, `completed=true`, 서버 상태는 `completed`, 종료 사유는
  `application_quitting`, 스키마는 1, 원본 프로젝트는 `chemical-safety-vr-client`였다. 실제 session ID와
  token은 출력하거나 문서에 남기지 않았다.
- 해당 세션에는 `application_paused`와 `application_resumed`가 각각 1개 들어 있고,
  `mode_session_started` 1개에는 `modeSessionId`가 포함됐다. 중간 종료로
  `quiz_answer_resolved`와 `mode_session_completed`는 0개이므로 퀴즈 원본과 모드 완료 집계의 Quest 통합
  검증은 아직 완료되지 않았다.
- Meta Platform 초기화에서는 현재 로그인 계정이 앱 ID의 개발자 entitlement를 갖지 않았다는 오류가
  별도로 발생했다. 이는 로컬 JSONL과 장기 개발 token 기반 LAN 복구 전송 성공과 구분하며, Meta 앱 범위
  사용자 ID 등록의 성공 근거로 사용하지 않는다.

#### 서버 구현 커밋과 검증 경계

- 2026-09-02 재확인한 서버 저장소 기준은
  `main@777ab453810e60b382f709541d1e4d67f493af8f`이며 `origin/main`과 일치하고 작업 트리는 clean이다.
- 앞서 미커밋이었던 `training-telemetry-service.js` 98줄, `training-telemetry.test.js` 110줄과
  PPE 대시보드 후속 문서 74줄은 서버
  `main@6ba52ed7dbb4a5b86b256035eb743b59a3432c37`의 `feat: 모드별 훈련 계측 계약 보완` 커밋으로 반영됐다.
- 해당 서버 커밋은 `modeSessionId`, `quiz_answer_resolved`, `mode_session_completed`, 퀴즈 집계와
  `modeElapsedSec` 수신 계약을 추가하며 클라이언트 전송 필드와 직접 대응한다. 커밋 본문에는 서버 자동
  테스트 65개 PASS가 기록돼 있다. 이번 2026-09-02 재확인에서는 테스트를 다시 실행하지 않았으므로 현재
  실행 결과로 확대하지 않는다.
- Quest 112건 수신 결과는 서버 커밋 전 작업 트리에서 확인한 통합 근거다. 서버 구현의 커밋·푸시는
  완료됐지만, 한 모드 완료 실행의 `quiz_answer_resolved`와 `mode_session_completed` 수신은 아직 미검증이다.
- 서버의 같은 상대 경로 공용 문서는 이 절을 포함한 클라이언트 기준본보다 175줄 뒤에 있다. 서버 커밋
  `6ba52ed` 반영과 공용 문서 미러 완료는 구분하며, 서버 미러 동기화는 아직 필요하다.

#### 남은 제출 전 수동 검증

1. Quest에서 Education·Training·Test 중 최소 한 모드를 끝까지 완료해 `quiz_answer_resolved`와
   `mode_session_completed`를 같은 `modeSessionId`로 서버에서 조회한다.
2. 최종 Release APK를 다시 빌드·설치하고 `DEBUGGABLE` 없음, cleartext HTTP 차단, 양안 렌더링, 입력,
   오디오와 주요 PPE 흐름을 실기 확인한다.
3. 이 공용 문서의 최신 절을 서버 저장소의 같은 상대 경로에 미러링하고, 서버의 별도 한글 커밋·푸시 SHA를
   기록한다. 서버 런타임 구현은 `6ba52ed7dbb4a5b86b256035eb743b59a3432c37`로 이미 반영됐다.

사용자가 2026-09-01 현재 결과를 보존하도록 요청했으므로 전체 모드 완료 실기와 최종 Release 재검증은
미완료 사실로 남긴 채 클라이언트 정리 변경을 제출 브랜치에 커밋·푸시한다. 보존 브랜치에는 추가 푸시하지 않는다.

## 2026-09-02 기준 정상 플레이 데이터 분리·보존 및 실서버 전 게이트

### 사용자 결정과 적용 범위

- 대시보드에서 실제 병목을 판단하려면 시나리오와 모드별 정상 플레이 시간이 먼저 필요하므로, 운영 데이터
  수집이나 대시보드 구현보다 기준 데이터 수집을 선행한다.
- 기준 데이터는 일반 훈련 DB와 섞지 않고 별도의 로컬 보존 요소로 관리한다. 이 결정은 운영 서버 전송이나
  운영 DB 변경 승인이 아니며, 기준 수집과 검증은 개발 PC의 로컬 환경에서만 수행한다.
- 정상 기준본은 `ConfinedSpace`, `LeakResponse` 두 시나리오와 `Education`, `Training`, `Test` 세 모드의
  조합 6개다. `Random Test`는 두 시나리오 중 하나를 선택하는 편의 기능이므로 별도 기준 조합으로 두지 않는다.
- 이번 절의 클라이언트 기준은
  `release/2026-09-01-meta-horizon-submission@518fca01980c9d8335a29f1c201e197e2806713a`, 서버 기준은
  `main@777ab453810e60b382f709541d1e4d67f493af8f`다. 양쪽 저장소의 미커밋 변경과 로컬 실행 설정은 각
  HEAD와 구분한다.

### 2026-09-02 로컬 수집 채널 복구 상태

- 로컬 Express 프로세스는 실행 중이었지만 텔레메트리 수집 플래그와 서버 업로드 토큰이 없는 상태로
  재시작되어 있었다. 이 상태에서는 health 응답만 성공하고 Unity/Quest 이벤트는 서버 DB로 전송되지 않는다.
- 개발용 64자 보안 난수 토큰을 새로 생성해 로컬 서버와 Unity Editor 전용 보조 토큰 파일에 동일하게
  설정하고, 로컬 서버의 텔레메트리 수집을 활성화한 뒤 PM2 프로세스를 재시작했다. 토큰 원문은 명령 출력,
  문서, Git, 씬, JSONL 및 APK에 기록하지 않았다.
- 재시작 후 인증된 텔레메트리 조회 API와 Quest가 사용할 사설 LAN health 경로가 각각 HTTP 200을
  반환했다. 이는 로컬 수신 준비 근거이며 기준 세션 수집 완료나 운영 전송 근거는 아니다.
- Quest Development 앱에는 현재 로컬 수집 채널용 일회성 설정이 주입돼 있고 앱은 중지 상태다. 앱이 설정을
  읽으면 설정 파일은 즉시 삭제된다. 그러나 아직 별도 기준 DB로 전환하지 않았으므로 이 상태에서 앱을
  실행해 얻은 데이터는 기준 DB 분리 조건을 충족하지 않는다.
- 서버 코드에서 훈련 텔레메트리 자동 purge, retention cleanup, 세션 DELETE API는 찾지 못했다. 이는 현재
  자동 삭제 경로가 없다는 정적 확인일 뿐이며, 별도 기준 저장소와 백업 없이 삭제 불가능하다는 뜻은 아니다.

### 별도 기준 저장소 설계

- 기준 데이터의 단일 저장 대상은 일반 로컬 DB와 분리된 MySQL schema/database
  `tyche_training_baseline`로 한다. 운영 서버와 운영 DB에는 이 기준 수집 단계에서 연결하지 않는다.
- 일반 로컬 수집 프로필과 기준 수집 프로필은 DB 이름과 실행 목적을 명확히 구분한다. Quest에는 기준 수집
  프로필이 준비된 뒤 해당 로컬 endpoint와 일회성 token을 다시 주입한다.
- 기준 DB에는 검증을 통과한 정상 완료 세션만 등록한다. 중단, pause/resume, 음성 건너뛰기, 입력 오류,
  네트워크 단절 또는 잘못된 선택이 포함된 실행은 삭제하지 않고 일반 검증 로그로 분리하며 기준 통계에는
  포함하지 않는다.
- 기준 DB에 자동 보존기간이나 정리 작업을 적용하지 않는다. 향후 일반 텔레메트리 retention을 도입할 때도
  기준 DB를 대상에서 제외하고, 기준 저장 계정과 운영 조회 계정에는 가능한 범위에서 DELETE/DROP 권한을
  부여하지 않는다.

### 정상 플레이 측정 규칙

1. 같은 앱 버전과 빌드, 같은 HMD, 같은 조작자와 같은 네트워크 조건을 사용한다.
2. 음성을 건너뛰지 않고 앱 pause, HMD 이탈, 강제 종료와 네트워크 단절 없이 진행한다.
3. 잘못된 PPE 선택 없이 시나리오의 정상 순서를 따르고 퀴즈는 가능한 한 첫 선택에서 정답으로 완료한다.
4. 측정 시작은 `mode_session_started`, 종료는 같은 `modeSessionId`의 `mode_session_completed`로 하며
   기준 총시간은 `modeElapsedSec`를 사용한다.
5. 오늘 최소 목표는 6개 조합별 정상 실행 1회다. 최종 대시보드 기준값은 가능하면 조합별 3회 이상을
   확보한 뒤 평균보다 이상치 영향이 작은 중앙값으로 결정한다.
6. 각 실행 직후 시나리오, 모드, 앱 버전, `sessionId`, `modeSessionId`, 시작·완료 시각, 퀴즈 문항·정답 수,
   PPE 오선택 수와 `modeElapsedSec`를 확인한다.

### 기준본 승인과 보존 절차

1. Quest 원본 JSONL과 기준 DB 상세 조회의 event count 및 마지막 sequence가 일치해야 한다.
2. sequence 누락·중복이 0이고, `mode_session_started`, 문제별 `quiz_answer_resolved`,
   `mode_session_completed`가 같은 `modeSessionId`로 연결돼야 한다.
3. 기준 승인 직후 Quest 원본 JSONL, 서버 상세 조회 내보내기와 기준 DB 백업을 서로 다른 보관 단위로
   남긴다. Meta 앱 범위 사용자 ID 등 식별 정보가 포함된 원본은 Git과 제출 문서에 넣지 않는다.
4. 보존 목록에는 원문 식별자 대신 내부 참조, 앱 버전, 시나리오, 모드, 측정 조건, 정상 시간, 이벤트 수와
   각 보관 파일의 SHA-256을 기록한다.
5. 승인된 6개 기준본에는 삭제·초기화·일반 테스트 데이터 정리 작업을 수행하지 않는다. 백업 복구 시험이
   끝나기 전에는 기준 수집 완료로 표시하지 않는다.

### 대시보드에서 사용할 후속 계산 기준

- `Education`, `Training`, `Test`는 음성 길이와 요구 행동이 다르므로 서로 다른 기준시간을 사용한다.
- `ConfinedSpace`와 `LeakResponse`도 필수 PPE와 흐름이 다르므로 시나리오별로 분리한다.
- 병목은 기준 총시간과 단계별 `flow_state_changed`, PPE inspection/grab/choice 및 quiz event 시각을 이용해
  `실제 소요시간 - 기준 소요시간`과 기준 대비 초과율로 계산한다.
- 단일 실행값을 정상 상한으로 확정하지 않는다. 반복 표본이 쌓이면 중앙값과 상위 분위수 기준을 별도로
  결정하고, pause·중단·오류 세션은 정상시간 분포와 구분한다.

### 운영 서버 전송 전 완료 게이트

1. `tyche_training_baseline` 생성, migration 적용, 일반 로컬 DB와의 분리 및 삭제 권한 제한을 확인한다.
2. 6개 조합의 최소 기준본을 수집하고 원본 JSONL·기준 DB·백업·보존 목록을 대조한다.
3. 운영과 같은 staging HTTPS 환경에서 인증, 배치 업로드, 중복 방지, 재전송, 완료 처리와 복구를 확인한다.
4. Development LAN 장기 공용 token과 cleartext HTTP를 Release APK에서 제거한다. 운영 인증은 Meta 검증과
   서버가 발급하는 단기 자격 증명으로 분리한다.
5. Release APK의 `DEBUGGABLE` 비활성, cleartext 차단, 비밀값 미포함, Quest 양안·입력·오디오와 6개
   시나리오/모드 흐름을 확인한다.
6. 운영 서버에는 rate limit, 세션당 이벤트 상한, 상세 조회 pagination, 일반 데이터 retention, 기준 데이터
   삭제 제외, DB 백업·모니터링·롤백 절차를 마련한다.
7. 클라이언트와 서버의 대상 브랜치·커밋 SHA, API·DB 계약, 하네스, 실제 staging 이벤트를 대조하고 공용
   문서를 양쪽 저장소에 동기화한다.
8. 허용된 테스트 계정 한 개로 제한 운영 검증을 통과한 뒤에만 일반 사용자 수집을 활성화한다.

### 현재 완료와 미완료 구분

- **완료:** 로컬 서버 수집 플래그와 개발 token 복구, 서버·Unity token 일치, PM2 재시작, 인증 API와 LAN
  health HTTP 200, Quest Development APK의 일회성 설정 주입.
- **미완료:** `tyche_training_baseline` 생성과 migration, Quest 기준 프로필 재주입, 6개 정상 실행 수집,
  원본·DB·백업 대조, staging HTTPS, Release 보안 검증 및 운영 서버 전송.
- 별도 기준 DB 전환 전에는 현재 주입 상태로 Quest 앱을 실행하지 않는다. 기준 수집을 시작할 때는 첫 조합을
  `ConfinedSpace/Test`로 하고, 완료 직후 서버 수신과 보존 조건을 확인한 뒤 다음 조합으로 진행한다.

### 2026-09-02 첫 기준 실행 시도와 APK 버전 불일치

- 기준 DB 전환 후 첫 Quest 실행에서 파트너 로고 시머링과 이전 컨트롤러 이미지가 보였다. 이는 현재 Unity
  씬·Importer·점선 링 수정이 포함되지 않은 기존 Development APK를 실행했기 때문이다. 최신 프로젝트
  변경이 런타임에서 되돌아간 것으로 해석하지 않는다.
- 해당 실행은 서버의 기준 DB에 앱 세션 1건, 이벤트 24건을 남겼으나 `mode`와 `workPlan`이 비어 있었다.
  서버 `completed` 상태는 업로드 세션 종료를 뜻할 뿐 훈련 모드 완료가 아니므로 기준 데이터에서 제외한다.
- 이 24건 세션과 원본 JSONL은 삭제하지 않고 검증 실패·비기준 후보로 보존한다. 기준 통계와 정상 기준본
  목록에는 포함하지 않는다.
- 다음 기준 실행 전 최신 Unity 프로젝트로 Development APK를 다시 빌드·설치하고, 설치 후 Quest LAN
  일회성 설정을 다시 주입한다. HMD에서 파트너 로고, 공통 `controller.png`, 버튼별 파란 점선 링을 먼저
  확인한 뒤 `ConfinedSpace/Test` 기준 측정을 시작한다.

### 2026-09-02 패키지명 정리와 이전 APK 데이터 보존

- 회사·제품 식별을 위해 Android/Standalone/iPhone application identifier를 기존
  `com.softcastella.prototype.tyche.jinyoung`에서 `com.tycheworks.immersa.safetyvr`로 변경했다.
  저장소명 `prototype`은 새 패키지명에 사용하지 않는다.
- 이전 패키지 앱의 외부 저장소에서 `tyche-training-telemetry` JSONL 1개(16개 이벤트)를 삭제 전에 PC로
  `.baseline-preservation/quest-20260902/`에 백업했다. 백업 SHA-256은
  `8EF51C7DE140C784E713448D24F07A9C543D245A7C8C3F5F7D6126FE0D4561A3`이다.
- 백업 원본은 `mode`·`workPlan`이 비어 있고 정상 모드 완료 이벤트가 없어 기준 통계에서 제외한다. 서버
  `tyche_training_baseline`의 기존 세션도 삭제하지 않고 비기준 원본으로 보존한다.
- 백업 확인 후 Quest에서 이전 앱을 삭제했다. 새 패키지 앱은 별도 앱으로 설치되며, Development APK 재빌드와
  Quest LAN 설정 재주입이 필요하다.

#### 후속작업

1. 새 패키지명으로 Unity Development APK를 재빌드·설치하고 `debuggable` 및 LAN 설정 파일 존재를 확인한다.
2. 서버 `tyche_training_baseline` 수신을 확인한 뒤 `ConfinedSpace/Test → Training → Education`, 이어서
   `LeakResponse/Test → Training → Education` 순서로 정상 기준 시간을 측정한다.
3. 각 실행의 Quest JSONL, 서버 이벤트 수·sequence·`modeSessionId`, DB 백업과 SHA-256을 대조해 승인된
   기준본만 별도 목록에 확정한다.

## 2026-09-08 Unity Editor + Quest Link 6개 조합 기준 데이터 채택

### 수집 조건과 판정 범위

- Unity `6000.4.8f1`의 `Assets/Scenes/4_PPE_Room.unity`를 Quest 2 USB3 Link로 실행했고, 촬영과 Play
  시작·종료는 사용자가 직접 수행했다. 이 결과는 Quest APK 단독 실행 기준이 아니라
  **Unity Editor + Quest Link 사용자 수행 기준 데이터**다.
- 측정 시작은 카드 선택이 런타임에 반영된 `flow_state_changed / PpeEducationSelected`, 모드 시작은
  `mode_session_started`, 종료는 같은 `modeSessionId`의 `mode_session_completed`를 사용했다. 현재 원본에는
  실제 XR PointerClick 순간의 전용 이벤트가 없으므로 `카드 선택 반영 → 완료` 시간은 클릭 시각과 소폭
  차이가 날 수 있다.
- `B20260908-01` 원본에만 앱 시작부터 Title·Intro·Loading·PPE Room 진입과 컨트롤러 안내 구간이 함께
  들어 있다. 이 선행 구간과 `session_started → session_ended` 전체 시간은 기준시간에서 제외하며, 다른
  회차와 동일하게 `PpeEducationSelected → mode_session_completed` 및 `modeElapsedSec`만 비교에 사용한다.
- 아래 6개 채택 원본은 모두 `session_started`, `mode_session_started`, `mode_session_completed`,
  `session_ended / application_quitting`을 포함하며 sequence 누락·중복이 0이다. 사용자가 재시작하거나
  스킵한 7개 이벤트 세션 2개와 15개 이벤트 세션 1개는 중단 회차로 분리해 기준 목록에서 제외했다.
- 수집 시점의 클라이언트 Git 기준은 `main@54a3169f0dd5bd2ec7e8d6e3a7ad896fedb0db6a`이지만 렌더 스케일과
  로고 Importer 등 미커밋 변경이 있는 Editor 상태였다. 서버 저장소 기준은
  `main@d3c67d424507ab04eb842eac2b897c5120abdce5`이며 서버 작업 트리에도 별도 미커밋 변경이 있었다.
  따라서 이 데이터의 실행 기준은 clean commit이나 Release APK로 확대 해석하지 않는다.

### 채택한 6개 회차

| 내부 참조 | 시나리오 | 모드 | 카드 선택 반영→완료 | 순수 모드 시간 | 최종 퀴즈 | 오답 시도 | PPE 오선택 | 빈 공간 Grab | 이벤트 |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `B20260908-01` | `ConfinedSpace` | `Test` | 103.63초 | 96.33초 | 2/5 | 3 | 0 | 2 | 155 |
| `B20260908-02` | `ConfinedSpace` | `Training` | 104.56초 | 96.11초 | 5/5 | 1 | 0 | 2 | 158 |
| `B20260908-03` | `LeakResponse` | `Test` | 105.25초 | 97.86초 | 2/5 | 3 | 1 | 3 | 153 |
| `B20260908-04` | `LeakResponse` | `Training` | 94.09초 | 88.59초 | 5/5 | 0 | 0 | 4 | 155 |
| `B20260908-05` | `ConfinedSpace` | `Education` | 237.56초 | 231.19초 | 5/5 | 3 | 0 | 2 | 200 |
| `B20260908-06` | `LeakResponse` | `Education` | 243.91초 | 235.22초 | 5/5 | 1 | 0 | 3 | 198 |

### 사용자 승인 편차와 사용 제한

- 사용자는 두 `Education` 회차에서 장화와 헬멧을 최초 Grab했을 때 음원이 재생되지 않았다고 확인했다.
  이 관찰을 알려진 편차로 명시한 상태에서 두 회차를 기준 데이터로 채택한다.
- 두 교육 회차의 시간·행동 순서·퀴즈·PPE 선택 데이터는 기준 비교에 사용한다. 다만 이 회차를 장화·헬멧
  최초 Grab 음원 재생 성공이나 정상 음성 길이의 증거로 사용하지 않는다. 누락 음원이 정상 재생되는 향후
  실행과 시간을 비교할 때는 음원 길이 차이를 별도로 표시한다.
- Test의 2/5 정답, `LeakResponse/Test`의 PPE 오선택 1회와 각 회차의 빈 공간 Grab은 삭제하거나 정상값으로
  바꾸지 않는다. 이상적 무오류 수행 기준이 아니라 실제 사용자 수행 기준의 원본값으로 유지한다.

### 원본 보존과 서버 반영 상태

- 식별정보가 들어 있는 원본은 Git에서 제외되는
  `.baseline-preservation/editor-link-20260908/`에 내부 참조 파일명으로 복사했다. 복사본 크기와 SHA-256은
  수집 원본과 일치한다.
- `B20260908-01`: `98B8E9CB944FE8453C8D3947F2278657E201197420A0859CD36A42887816227D`
- `B20260908-02`: `5CA9973EB8BCE548A3D297A967573E713B610FE5AE2EB371B794177A5A55ACFD`
- `B20260908-03`: `FA4DF806205729A8559906C86028B4A8D723E9FC0AA66E0CC80A31E360F5FEF8`
- `B20260908-04`: `5D96D206C1BEE605E5218D884569027682F42B816252258FA7B79844D143A175`
- `B20260908-05`: `98DB04E21ECAE8796D09932B771F67E7C2016E79914BB2E1E28DE5F056FCC562`
- `B20260908-06`: `836C8D5365C9D37B0EF6BFEC5E9FC3B587A55808243EA59FB7C6A31B9C8EE449`
- `ConfinedSpace/Test`의 일반 로컬 DB 전송은 종료 이벤트 전 sequence까지만 반영되어 서버 상태가 아직
  `open`이고, 나머지 PPE Room 직접 시작 회차는 자동 업로드되지 않았다. 따라서 현재 채택은 로컬 JSONL과
  보존 복사본 기준이며, 기준 DB 반영·상세 조회 대조·DB 백업은 아직 완료되지 않았다.

## 2026-09-09 Alpha 제출용 Release 인증·전송 구현 인수인계

### 이번 변경과 보존한 동작

- 이번 변경은 Alpha 제출용 Android Release 앱이 Meta 검증을 거쳐 공개 HTTPS로 원본 JSONL을 업로드하게
  한다. PPE 흐름, JSONL 우선 기록, ACK checkpoint, 지수형 재시도, 재실행 복구와 기존 Editor·Android
  Development 전송은 그대로 보존한다.
- Release에서 장기 공용 token이나 Meta App Secret을 APK에 넣지 않는다. 앱은 앱 범위 Meta ID와 일회용
  `User Proof`만 서버에 보내고, 서버가 검증 후 15분 기본 수명의 업로드 전용 Bearer token을 발급한다.
- Release token은 세션 생성·이벤트·완료 POST에만 사용할 수 있다. 텔레메트리 GET 조회는 기존 개발용
  token으로 분리하며, 검증된 Meta 사용자와 세션의 `metaUserId`가 다르면 업로드를 거부한다.

### 교차 저장소 계약

1. 클라이언트는 `Users.GetUserProof()`와 `MetaPlatformIdentityProbe.CurrentAppScopedUserId`를 사용한다.
2. `POST /api/training-telemetry/auth/meta` 요청은 `metaUserId`, `userProof`만 포함한다.
3. 서버는 Meta `user_nonce_validate`에 proof, 사용자 ID와 서버 전용 App Access Token을 HTTPS POST 폼으로
   전달하며 비밀값을 URL 쿼리에 넣지 않는다.
4. 성공 응답은 `data.accessToken`, `data.tokenType=Bearer`, `data.expiresIn`, `data.metaUserId`를 반환한다.
5. 클라이언트는 만료 60초 전 token을 갱신하고 업로드 HTTP 401이면 캐시를 폐기한다.
6. Android Release만 `productionServerBaseUrl`을 사용한다. `0_App` 작성값은
   `https://immersa.tycheworks.com`이며 기본 443 포트의 공개 HTTPS 루트만 허용한다.

서버 환경 변수 이름은 다음과 같고 실제 값은 Git·문서·클라이언트에 기록하지 않는다.

- `ENABLE_META_TRAINING_TELEMETRY_AUTH`
- `META_PLATFORM_APP_ACCESS_TOKEN`
- `TRAINING_TELEMETRY_SESSION_TOKEN_SECRET`
- `TRAINING_TELEMETRY_SESSION_TOKEN_TTL_SECONDS`

### 검증 상태

- **서버 코드·자동 테스트:** Meta proof 성공·실패, token 발급·만료·변조, Meta ID 불일치, Release GET
  차단, 공식 검증 URL 계약과 필수 설정 누락을 포함해 최신 `main`의 `npm test` 93개가 통과했다.
- **클라이언트 정적·Editor 확인:** `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드가
  오류 0으로 통과했다. Unity Editor 재컴파일 뒤 `App Startup Synchronization`,
  `PPE Training Data Contract`, `Documentation Policy` 하네스 실행도 모두 PASS했다.
- **제출 게이트:** Release 전송 차단 FAIL은 해소됐다. `MetaAlphaSubmissionGateHarness`는 code 6 Release
  APK가 아직 없어서만 `WAIT`다. 로컬 기준 Express PM2 프로세스와 health·확인 페이지·인증 조회는
  HTTP 200이다.
- **운영 반영:** 운영 HTTPS 환경 변수, migration과 POST 전용 Nginx 프록시는 적용했다. 실제 Meta proof
  왕복, Quest Release 한 세션의 서버 적재·재조회, 양안·입력·음성과 Alpha 채널 설치는 아직 수행하지 않았다.

### 다음 실행 순서

1. Meta 테스트 사용자 한 명의 실제 User Proof로 인증·세션·이벤트·완료·중복 재전송을 대조한다.
2. 그 뒤 사용자와 함께 짧은 Quest 수동 회귀를 수행하고 code 6 Release APK를 만든다.
3. code 6을 Alpha 채널에 업로드한 뒤 같은 세션의 Quest JSONL과 서버 원본을 최종 대조한다.

### 2026-09-09 운영 반영 결과

- 서버 구현 커밋 `main@5b8138865991408215011078328f74a0df229982`를 GitHub와 Vultr의
  `/home/linuxuser/workspace/chemical-safety-vr`에 fast-forward로 반영했다.
- 적용 전 `.env`를 `/home/linuxuser/.config/tycheworks/env-backups/`에 권한 `600`으로 백업했다. 운영 DB는
  `--no-tablespaces --single-transaction`으로 백업했고 압축 무결성·완료 마커와 SHA-256
  `ff524514c7ea1187cb4f496e305b5c2dc67fbd7f8e1ad92d6c694dc22262`를 확인했다. 비밀값과 DB 원문은
  문서·Git에 넣지 않았다.
- 운영 `.env`에 Meta 인증과 수집 활성화, 서버 전용 App Access Token, 임의 생성 세션 서명 비밀과 900초
  TTL을 설정했다. 개발용 장기 업로드 token은 비워 Release 업로드 권한과 운영 조회 권한을 분리했다.
- migration `009`~`012`를 새로 적용했고 전체 migration은 16개다. 적용 직후 텔레메트리 참여자·세션·
  이벤트는 모두 0건이었다.
- PM2를 새 환경으로 재시작해 `online`, 내부 health `200`, 빈 인증 요청 `400`, 비인증 조회 `401`을
  확인했다. 재시작 직후 기존 오류 로그 크기는 15초 관찰 동안 증가하지 않았다.
- IMMERSA Nginx 원본을 별도 백업하고 저장소의 POST 전용 프록시 include를 적용했다. `nginx -t` 통과 후
  reload했으며 공개 인증 POST `400`, 공개 GET `403`, 기존 health·상세페이지 `200`, TLS 검증 성공을
  확인했다.
- 이는 운영 수신 준비 완료 근거다. 실제 Meta proof 성공, Quest JSONL 업로드와 DB 원본 일치는 code 6
  이전의 실제 기기 통합 검증으로 남긴다.

## 2026-09-10 code 6 Release APK 생성과 정적 제출 검증

### 적용 결과

- 클라이언트 `main@eba9e1a8d46d964ab4d31f4b07081b28fa861ed7`에서 개인 Keystore를 로컬
  `PlayerSettings`에 지정하고 `MetaQuestAlphaBuild.BuildRelease`로 code 6 Release APK를 생성했다.
- 출력 파일은 `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_6.apk`, 파일 크기는
  `219,710,171 bytes`, SHA-256은
  `F092AA929888C713B738806E4624EA0AEB62CB0E7D8CC05289D253C1640AC4AF`다.
- Keystore 경로와 비밀번호는 개인 로컬 설정으로만 사용하며 Git·문서·APK 검증 출력에 비밀값을 기록하지
  않는다.

### 완료한 검증

- Unity BuildReport는 `[Meta Quest Alpha Build] PASS`를 기록했고 빌드 종료 뒤 Editor가 정상 상태로
  복귀했다.
- Android `aapt2`에서 package `com.tycheworks.immersa.safetyvr`, `versionCode=6`,
  `versionName=0.1.0`, `minSdkVersion=25`, `targetSdkVersion=34`, `install-location=auto`,
  `android.hardware.vr.headtracking required=true`, Meta VR category와 `arm64-v8a`를 확인했다.
- `apksigner`에서 APK Signature Scheme v2와 기존 출시 인증서 서명을 확인했다. Manifest에는
  `android:debuggable`이 없고 `usesCleartextTraffic=false`다.
- APK에는 `libil2cpp.so`, `libUnityOpenXR.so`, `libUnityOpenXRHands.so`, `libopenxr_loader.so`를 포함한
  ARM64 네이티브 라이브러리가 있다.
- `node Tools/MetaAlphaSubmissionGateHarness.mjs`는 code 6 Release·code 5 Development APK, 기준 DB
  migration 16개, 초기 수집량 `session 0 / event 0`, 로컬 기준 서버 HTTP 상태를 확인하고 `READY`를
  반환했다.

### 완료 상태와 다음 순서

- **완료:** Release 인증·전송 코드, 서버 운영 반영, code 6 APK 빌드, 서명·Manifest·ABI 정적 검증과
  제출 하네스 `READY`.
- **미검증:** 실제 Meta 테스트 사용자의 User Proof 왕복, Alpha 채널 설치, Quest 단독 실행, 양안·입력·
  오디오·시각 회귀, 같은 세션의 Quest JSONL과 운영 MySQL 원본 일치.
- 다음 사용자 단계는 검증된 code 6 APK를 Meta Alpha 채널에 업로드하고 Quest에 채널 설치하는 것이다.
  그 뒤 한 번의 짧은 Release 회차로 인증·원본·서버 적재를 대조한다. 업로드 완료를 Quest 통합 성공으로
  합쳐 쓰지 않는다.

## 2026-09-10 작업 종료 기준: Meta Alpha 업로드 이후

### 현재 확인 상태

- 사용자는 Meta 업로드 화면에서 code 6 APK의 처리 완료를 확인했다. 이 기록은 사용자의 Dashboard 화면
  확인을 근거로 하며, Codex가 Meta API나 채널 상세 화면에서 독립 조회한 결과는 아니다.
- 업로드 대상은 정적 검증을 통과한
  `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_6.apk`다. 업로드 시 연령대는 앱의 실제 대상에
  맞춰 `Teens and Adults (13+)`를 선택했다.
- Meta 목록용 512×512 불투명 PNG 아이콘은
  `Builds/MetaHorizonAlpha/SubmissionAssets/Meta_Horizon_Icon_512.png`로 준비했다. APK Android 아이콘과
  Meta App Metadata의 Store/Library 아이콘은 별도 자산이므로, 아이콘 준비를 App Metadata 저장이나
  Quest 라이브러리 반영 완료로 합쳐 쓰지 않는다.

### 다음 통합 검증 순서

1. Meta Dashboard에서 업로드된 빌드의 `versionCode=6`과 대상 채널 `ALPHA`, 테스트 사용자 할당을 확인한다.
2. Quest에서 기존 앱을 완전히 종료하고 Alpha 채널의 code 6을 설치한 뒤 단독 실행한다.
3. 타이틀·파트너 로고, 로딩 12초 연출, PPE 모달, 정상 장화·안전모 최초 Grab 음성, 입력, 양안·주변 시야와
   프레임 안정성을 확인한다.
4. 짧은 Release 회차 한 번을 완료하고 Quest 원본 JSONL의 `sessionId`, 이벤트 수와 마지막 `sequence`를
   확보한다.
5. Codex가 같은 회차의 Meta proof 인증, 단기 token, 운영 MySQL 세션·이벤트·완료·재조회와 중복 전송
   결과를 대조한다.
6. 위 결과가 모두 일치할 때만 `Release 통합 검증 완료`로 상태를 올린다. App Metadata 작성, 정식 Store
   심사 제출과 대시보드 KPI 확정은 별도 후속 단계다.

## 2026-09-10 기준 데이터 비공개 Git 원격 보존

### 보존 결정

- 2026-09-08 Unity Editor + Quest Link에서 채택한 6개 시나리오·모드 JSONL 기준본은 기존에
  `.baseline-preservation/editor-link-20260908/`의 로컬 단일 사본으로만 보존돼 있었다.
- 사용자는 해당 기준 데이터가 본인 수행 데이터임을 확인하고 PC 장애와 앱 삭제에 대비해 비공개
  `softCastella/chemical-safety-vr-client` 저장소에 원본 그대로 보존하도록 승인했다.
- 대상은 `B20260908-01`부터 `B20260908-06`까지의 기존 6개 JSONL만이며, 다른 로컬 세션과 Quest 앱
  데이터는 이번 보존 범위에 포함하지 않는다.

### 개인정보·무결성 확인

- 대상 원본에는 `metaAppScopedUserId`가 포함돼 있다. 인증 token, Meta User Proof, password, secret,
  email 필드는 없음을 값 출력 없이 필드명 기준으로 확인했다.
- GitHub 저장소의 가시성은 `PRIVATE`로 확인했다. 저장소를 공개로 전환하거나 원본을 다른 위치에
  재배포할 때는 개인정보 포함 여부를 다시 검토한다.
- 6개 파일의 SHA-256은 이 문서의 `2026-09-08 Unity Editor + Quest Link 6개 조합 기준 데이터 채택`
  절에 기록된 값과 모두 일치한다.
- `.gitattributes`에서 대상 JSONL을 `-text`로 지정해 `core.autocrlf`에 의한 줄바꿈 변환 없이 원본
  바이트를 보존한다.

### 검증 경계

- 이 원격 보존은 Quest의 code 5 앱을 삭제해도 채택한 6개 기준 원본을 복구할 수 있게 하는 조치다.
- 원격 Git 보존 완료를 기준 DB 반영, 운영 MySQL 대조 또는 code 6 Release 통합 검증 완료로 합쳐 쓰지
  않는다.

## 2026-09-10 Quest code 6 실제 연동과 앱 내부 종료 후속

### 기준 저장소와 실제 통합 결과

- 클라이언트 변경 전 기준은 `main@be7709bca652cbeb4b20babb00ccd314edd8c475`, 서버 확인 기준은
  `main@bb7595b6ea0e1c387262d10e7d3312bec746d91f`다. 서버는 `main...origin/main`으로 동기화돼 있었다.
- Quest Alpha code 6의 실제 Meta 인증이 성공했고 운영 DB 참여자 identity type은 `meta`로 확인했다.
- `LeakResponse/Training` 세션 `5df09877…`은 `mode_session_completed`를 포함해 이벤트 165개,
  `sequence 1~165`, `session_ended/application_quitting`으로 운영 MySQL에 저장됐다. 마지막 종료 데이터는
  종료 순간이 아니라 다음 앱 실행의 durable recovery에서 전송됐다.
- Quest 원본 JSONL을 ADB로 직접 추출해 파일 단위로 대조하지는 않았다. 위 결과는 운영 DB의 연속 sequence,
  이벤트 종류와 서버 세션 상태를 근거로 한다.

### 앱 내부 종료 재현과 클라이언트 수정

- 사용자가 앱 내부 `종료하기`를 눌러 Quest 라이브러리로 복귀한 최신 세션 `0cb9366f…`은 서버에서
  `eventCount=135`, `lastSequence=135`, `endedAt/endReason=null`, `status=open`으로 남았다. 마지막 수신은
  `voice_playback_ended`였고 `application_paused`와 `session_ended`는 도착하지 않았다.
- 원인은 `QuitApplicationButton`이 서버 업로드 완료를 기다리지 않고 즉시 `Application.Quit()`을 호출한
  것이다. 클라이언트는 종료 이벤트를 먼저 한 번만 기록하고 현재 세션의 이벤트와 `/complete` ACK를 최대
  5초 기다린 뒤 앱을 종료하도록 변경했다. timeout 때는 기존 durable recovery를 유지한다.
- 서버 API·DB 계약과 배포 코드는 변경하지 않았다. code 7 Quest 실제 종료 검증 전까지 정적·Editor 검증
  완료와 Release 통합 완료를 구분한다.

### 연속 시나리오·모드 분리 계약

- 한 앱 `sessionId` 안에서 사용자가 복귀 후 다른 시나리오 또는 Education·Training·Test를 연속 선택하는
  동작은 허용한다.
- 각 실행은 새 GUID `modeSessionId`로 시작하고 `mode`, `workPlan`, PPE·퀴즈·완료 이벤트를 그 ID에
  연결한다. 복귀 시 모드 추적 상태를 초기화하되 앱 전체 세션은 닫지 않는다.
- `PPETrainingDataContractHarness`에 종료 ACK 순서, 종료 이벤트 중복 방지, 새 `modeSessionId` 생성과 복귀
  초기화 검사를 추가했고 Unity Editor에서 PASS를 확인했다. 실제 Quest 검증은 code 7 Alpha 설치 후
  `앱 내부 종료 직후 서버 completed`와 `연속 실행의 서로 다른 modeSessionId`를 각각 확인해야 한다.

### code 7 Release APK 생성 결과

- 클라이언트 종료·연속 실행 분리 변경은 `main@d16c0b31d536fbb05777c06e8a927dc899141442`로 GitHub에
  푸시됐다. Android `versionCode`를 7로 올리고 서명 준비 상태를 확인한 뒤 Release APK를 생성했다.
- 출력은 `Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_7.apk`, 파일 크기는
  `219,634,992 bytes`, SHA-256은
  `A1F500B135304F81033CC738CCDADC63C6170A15CB06A2DCB687E4E7C15D7B9A`다.
- Unity BuildReport `Success`, package `com.tycheworks.immersa.safetyvr`, `versionCode=7`, Android 25/34,
  ARM64/OpenXR, Quest VR category, 필수 head tracking, `usesCleartextTraffic=false`, `android:debuggable`
  부재와 기존 출시 인증서의 APK Signature Scheme v2 서명을 확인했다.
- `MetaAlphaSubmissionGateHarness`는 code 7 APK와 기준 DB migration 16개를 확인했지만, 실행 시점에 로컬
  Express TCP 3000이 닫혀 있어 `WAIT`였다. 이는 APK 정적 검증 실패가 아니며 Alpha 업로드·Quest 실제 종료
  및 연속 회차 분리 검증은 아직 수행하지 않았다.

### code 7 Alpha 업로드와 집 후속작업 인수인계

- 사용자는 Meta Dashboard에서 code 7 Alpha 업로드 완료를 확인했다. Codex가 Meta API 또는 채널 상세에서
  독립 조회한 결과는 아니므로 Quest 라이브러리의 설치 버전 확인 전까지 배포·설치 완료로 합쳐 쓰지 않는다.
- 용량 확보 요청에 따라 `Builds/MetaHorizonAlpha` 아래 APK 7개, 총 `2,027,034,856 bytes`를 삭제했고 남은
  APK는 0개다. code 7도 로컬에서 삭제됐으며 Meta Alpha 재설치 또는 같은 출시 Keystore 재빌드로만 다시
  확보할 수 있다. Git의 소스·기준 JSONL·문서와 서버 데이터는 삭제하지 않았다.
- 클라이언트는 `main@e06af5846ad0fba66d1da6fa899550560c0047db`, 서버 문서 미러는
  `main@eebdd673532aa3e1b380992da31a27c610460b8b`에서 원격과 동기화된 상태를 기준으로 인수인계한다.
- 클라이언트 작업 트리에는 사용자가 작업 중인 `Assets/Scenes/1_Title.unity`,
  `Assets/Scenes/3_PPE_Room_3mode_loco_cam.unity`, `ProjectSettings/ProjectSettings.asset`의 preloaded asset
  변경과 미추적 PPT가 남아 있다. `Assets/Settings/Mobile_RPAsset.asset`은 상태가 수정으로 보이지만
  `git diff` 내용은 없으므로 다음 세션에서 재확인하고 임의 커밋하지 않는다.
- 현재 브랜치는 클라이언트·서버 모두 `main`이며 별도 작업 브랜치가 없어 추가 병합 대상은 없다. 관련
  한국어 커밋을 각 `origin/main`에 직접 푸시하는 것이 이번 병합 완료 기준이다.

집에서 재개할 때는 다음 순서로 검증한다.

1. Quest 라이브러리에서 Alpha code 7을 설치하고 표시 버전 또는 ADB package versionCode가 7인지 확인한다.
2. 한 모드를 시작한 뒤 앱 내부 `종료하기`를 누른다. 라이브러리 복귀 후 ADB `pidof`에서 앱 PID가 없는지
   확인한다.
3. 앱을 다시 실행하기 전에 운영 서버의 같은 `sessionId`가 `completed`,
   `endReason=application_quitting`이며 마지막 sequence까지 수신됐는지 확인한다.
4. 앱을 한 번 실행한 상태에서 서로 다른 시나리오·모드 두 회차를 연속 완료한다. 같은 앱 `sessionId`
   아래 서로 다른 `modeSessionId` 두 개와 각 1개의 `mode_session_completed`를 확인한다.
5. 강제 종료·충돌·전원 차단은 종료 이벤트를 보장할 수 없다. 마지막 이벤트 뒤 장기 `open`인 세션을
   비정상 종료로 추정하되 원인을 충돌·전원 종료로 확정하지 않는다. heartbeat 기반 세부 판정은 후속이다.
6. 로컬 APK가 의도적으로 없으므로 `MetaAlphaSubmissionGateHarness`의 APK 없음 `WAIT`는 예상 결과다.
   실제 Quest·서버 검증을 통과한 뒤 대시보드 상세 조회와 입점 신청 자료 후속으로 이동한다.

## 2026-09-10 Android Release 서명정보의 로컬 분리

### 이번 변경이 대응하는 사용자 요청과 보존 범위

- 여러 PC가 서로 다른 절대 Keystore 경로를 `ProjectSettings/ProjectSettings.asset`에 저장해 Git pull이
  반복 차단되는 문제를 제거한다.
- Android build code, 앱 식별자, OpenXR, 빌드 씬과 기존 `MetaQuestAlphaBuild`의 Release APK 출력 규칙은
  보존한다. UI·입력·텔레포트·PPE·음성·텔레메트리 런타임은 변경하지 않는다.
- 상태 소유자는 Editor 전용 `MetaQuestSigningConfiguration`과 `MetaQuestAlphaBuild`다. 필수 로컬 값이
  없으면 경로나 비밀번호를 추정하거나 자동 생성하지 않고, 누락된 환경변수 이름을 포함한 오류로 빌드를
  중단한다.

### 적용과 근본 원인

- 근본 원인은 Git이 추적하는 `ProjectSettings.asset`에 사용자별 절대 경로와 Alias가 저장돼 있던 것이다.
  추적 설정에서는 `AndroidKeystoreName`, `AndroidKeyaliasName`을 비우고 `androidUseCustomKeystore`를
  비활성화했다.
- 각 PC는 저장소 루트의 `.meta-quest-signing.example`을 `.meta-quest-signing.local`로 복사해 경로,
  Alias와 두 비밀번호를 한 번 설정한다. `.meta-quest-signing.local`은 `.gitignore` 대상이며 비밀값을
  Git·문서·로그에 출력하지 않는다. 같은 이름의 프로세스 환경변수가 있으면 로컬 파일보다 우선한다.
- `Tools > XR > Build Meta Quest Alpha Release`는 빌드 범위에서만 로컬 값을 `PlayerSettings.Android`에
  주입하고 성공·실패와 관계없이 이전 값을 `finally`에서 복원한다. 따라서 Unity 종료나 Git pull 전에
  PC별 경로를 `ProjectSettings.asset`에 다시 저장할 필요가 없다.

### 검증과 남은 수동 확인

- `Tools > XR > Validate Meta Quest Signing Isolation`은 추적 설정에 PC별 서명값이 없는지와 로컬 파일의
  Git 제외·예제 파일 계약을 검사한다. `Validate Local Meta Quest Signing`은 로컬 네 값과 Keystore 파일
  존재를 검사하되 비밀값을 출력하지 않는다.
- Unity `6000.4.8f1` 배치 실행에서 새 Editor 코드 컴파일과
  `Validate Meta Quest Signing Isolation`, `DocumentationPolicyHarness.Validate`가 종료 코드 `0`으로
  통과했다. `git diff --check`와 공용 문서의 양쪽 SHA-256 일치도 확인했다.
- 이 PC의 `.meta-quest-signing.local`에는 기존 Keystore 경로와 Alias만 옮겼고 알 수 없는 두 비밀번호는
  비워 두었다. 사용자가 두 값을 한 번 입력한 뒤 `Validate Local Meta Quest Signing`과 실제 Release APK
  빌드, 인증서 지문을 별도로 확인해야 한다. 현재 결과를 서명·Meta 업로드 성공으로 확대하지 않는다.
- 서버 API·DB·인증·텔레메트리 계약은 변경하지 않았다. 이 항목의 서버 저장소 반영은 공용 문서 미러
  동기화뿐이며 서버 코드 반영이나 클라이언트·서버 통합 검증 완료를 뜻하지 않는다.

## 2026-09-10 code 7 Quest 종료 검증과 재전송 계약 보강

### 저장소·배포 기준

- 검증 시작 기준은 클라이언트 `main@5c1204539f98e398536982043676787462df4e37`, 서버
  `main@1d9b9fa0a146b4a15925470182ec7a7f2ceff462`다.
- 운영 서버 checkout은 확인 시점에 `e3dc59b24acd6eba12724955e7dc395baa32d898`였다. 아래 서버 수정은
  `main@0ef1619c70d1fbfa9d448463da3d0244ad9724ab`로 GitHub에 반영했지만 아직 운영에 배포하지
  않았으므로 `서버 코드 수정`, `운영 반영`, `통합 검증`을 구분한다.

### code 7 실제 확인

- Quest 2 설치본은 package `com.tycheworks.immersa.safetyvr`, `versionCode=7`, Meta 설치 관리자
  `com.oculus.ocms`이며 non-debuggable Release 앱으로 확인했다.
- Meta 식별은 두 실행 모두 완료됐고 동일한 앱 범위 사용자와 연령대가 수집됐다. 개인 식별값 자체는
  문서에 기록하지 않는다.
- 앱 내부 종료 검증 세션 `bb539742…`과 `31bddadc…`은 각각 운영 DB에서 `completed`,
  `endReason=application_quitting`, 연속 sequence로 확인됐다. 두 번째 세션은 종료 버튼이 서버 완료 ACK를
  확인한 로그와 Quest의 `completed=true` 업로드 상태까지 일치했다.
- 두 세션에는 정상 모드 완료가 없어 `modeSessionId`와 `mode_session_completed`가 없었다. 같은 HMD의
  두 번째 실행이라는 사실만으로 기존 사용자로 바꾸지 않으며, 정상 모드 완료 뒤 로컬 완료 기록이 생겨야
  `Welcome_Old`를 선택하는 현재 계약과 일치한다.

### 완료 세션 재전송 결함과 수정

- Meta 설치 복원으로 과거 JSONL은 남았지만 ACK 메타데이터가 없거나 뒤처질 수 있다. 운영 DB에서 이미
  완료된 가장 오래된 세션의 이벤트 재전송이 `409`가 되면 클라이언트 큐가 그 파일에서 중단돼 최신
  세션 전송도 막혔다.
- 실제 Quest 검증에서는 원본 JSONL을 먼저 백업하고 운영 DB의 완료 상태와 이벤트 수가 일치한 과거
  세션의 로컬 ACK만 복구해 새 세션 전송을 재개했다. 서버 데이터와 JSONL 원본은 수정하지 않았다.
- 서버 저장소는 완료 세션에도 ID·sequence·payload hash가 모두 같은 기존 이벤트만 `duplicate`로
  응답하고, 새 이벤트나 다른 데이터는 계속 `409 CONFLICT`로 거부하도록 실제·인메모리 저장소를
  보강했다. API 테스트는 완료 뒤 동일 이벤트 재전송과 새 이벤트 거부를 함께 검사하며 `npm test`
  93개가 모두 PASS했다.
- 운영 배포와 수정 서버 기준의 자동 복구 재검증은 아직 하지 않았다. 배포 승인 뒤 ACK 수동 보정 없이
  대기 큐가 진행되는지 확인해야 한다.

### 다음 순서

1. 사용자 승인 뒤 서버 `main@0ef1619c70d1fbfa9d448463da3d0244ad9724ab`를 운영에 배포하고 완료
   세션 중복 재전송이 200으로 응답하는지 확인한다.
2. 한 앱 실행에서 서로 다른 모드 두 회차를 완료해 별도 `modeSessionId`와 각 1개의
   `mode_session_completed`를 확인한다.
3. 정상 완료 후 재실행에서 동일 Meta 사용자의 `Returning` 및 `Welcome_Old`를 확인한다.

## 2026-09-10 Codex Desktop 중단 대비 최종 인수인계

### Git과 실행 상태

- 클라이언트 기능·서명 분리 기준은 `main@5c1204539f98e398536982043676787462df4e37`, code 7 실제 검증
  문서 반영은 `main@4e51c8b1b95d75aa2a0c4d2306358fe22726d4b2`이며 모두 `origin/main`에
  푸시됐다.
- 서버 재전송 수정은 `main@0ef1619c70d1fbfa9d448463da3d0244ad9724ab`, 공용 문서 미러는
  `main@ab2c5147767085a679af07411fa88cb10eeb1ac0`이며 모두 `origin/main`에 푸시됐다.
- 서버 작업 트리는 깨끗하다. 클라이언트 작업 트리에는 사용자 소유 XR Simulation `.meta` 변경 2개와
  미추적 `Assets/XR/Temp`, `Assets/_Recovery`가 남아 있으며 이번 작업에서 수정·삭제·커밋하지 않았다.
- Quest 2 기기 `1WMHHA65BK2493`는 ADB에서 `device`로 인식되고 앱 프로세스는 종료된 상태다.

### 복구 자료와 주의점

- Quest에서 추출한 code 7 원본 JSONL 백업은 Git 제외 경로
  `.codex-tmp/quest-code7-telemetry-20260910`에 있다. 운영 DB와 정확히 대조한 과거 세션 ACK 복구 파일은
  `.codex-tmp/quest-code7-state-repair`에 있으며 두 폴더 모두 원격 Git 보존 대상이 아니다.
- ACK 수동 복구는 운영 DB에서 같은 세션 ID, 이벤트 수, 연속 sequence와 `completed`를 확인한 세션에만
  적용했다. 다음 세션에서 근거 없이 다른 ACK를 생성하거나 JSONL 원본을 편집하지 않는다.
- 운영 서버 checkout 확인 기준은 `e3dc59b24acd6eba12724955e7dc395baa32d898`다. GitHub의 서버 수정
  `0ef1619…`은 아직 운영에 배포하지 않았고 PM2 재시작·DB migration·운영 데이터 변경도 수행하지 않았다.
- 서버 수정은 migration 없이 애플리케이션 저장소 로직만 바꾼다. `npm test` 93개 PASS는 로컬 자동
  검증이며 운영 재전송 성공을 뜻하지 않는다.

### 재개 시 단일 다음 작업

사용자에게 운영 배포 승인을 먼저 확인한다. 승인받으면 서버 `0ef1619…`을 운영에 반영한 뒤, 이미 완료된
세션의 정확히 같은 이벤트 재전송은 `200`과 `duplicates`로 응답하고 새 이벤트는 계속 `409`인지 확인한다.
그 결과가 PASS인 뒤에만 Quest에서 연속 모드 두 회차와 정상 완료 후 `Welcome_Old`를 검증한다.

## 2026-09-11 Build 8 Alpha 배포와 중도 퇴장 확인

### 오늘 적용한 변경

- 로딩 씬의 `progressFillDuration`을 8초로 조정했다.
- Android 앱 버전을 정식 릴리즈 표기 `1.0.0`, `versionCode=8`로 맞춰 Build 8을 생성했다.
- 이전 APK와 동일하게 Unity Player Settings의 Custom Keystore, alias, 비밀번호를 직접 사용하는 방식으로
  되돌렸다. 별도 `.meta-quest-signing.local` 주입과 ProjectSettings 격리 검증은 제거했다.
- 현재 씬 구조에 존재하지 않는 `PartnerLogos` 블록을 강제하던 Scene Dependency 검증을 제거하고,
  로딩 시간 검증 기준을 8초로 갱신했다.

### 실행 및 배포 확인

- Build 8 APK가 Meta Horizon Alpha 채널에 업로드됐고, 처리 완료 후 Quest에서 자동 업데이트됐다.
- Quest에서 앱 실행과 중도 퇴장·`종료하기` 흐름을 수행했으며, 종료 후 Quest 라이브러리로 복귀하는 동작을
  확인했다.
- Unity AI Assistant의 `generators.ai.unity.com` 접속 오류 로그는 프로젝트 컴파일 오류가 아닌 별도 서비스
  로그로 분류했다.

### 후속 작업

1. 운영 조회용 토큰을 서버 환경에 설정한 뒤, 이번 Build 8 세션의 `sessionId`와 `EXIT Point 중도 중단`,
   종료 이벤트 및 서버 `completed` 상태를 운영 조회 API에서 대조한다.
2. `종료하기`에 의한 `application_quitting`과 `EXIT Point 중도 중단` 이벤트를 서로 구분해 결과를 기록한다.
3. 정상 모드 완료 1회와 동일 앱 실행 중 다른 모드 1회를 추가로 수행해 별도 `modeSessionId`와
   `mode_session_completed`가 생성되는지 확인한다.
4. 이번 변경으로 제거한 `PartnerLogos` 검증이 실제 제품 요구사항과 일치하는지 다음 씬 정리 작업에서
   재검토한다. 현재 확인은 Build 8 배포·실행 수준이며 양안 렌더링과 전체 교육 회귀 검증은 미완료다.

## 2026-09-11 학원 Quest 2 Build 8 연속 실행·종료 통합 검증

### 실행 기준

- 클라이언트 저장소는 `main@dbb2e8f7d9babaa0f039812bdcf679b6163c19c6`, 서버 저장소와 운영 checkout은
  `main@094524ec1db797a7c8c497086a868a7b0f4fb19e`을 기준으로 확인했다. 서버 checkout은
  `0ef1619c70d1fbfa9d448463da3d0244ad9724ab`의 완료 세션 중복 재전송 수정도 포함한다.
- 학원 Quest 2의 설치본은 Meta 설치 관리자 `com.oculus.ocms`를 통한 package
  `com.tycheworks.immersa.safetyvr`, `versionCode=8`, `versionName=1.0.0`이다.
- 이번 원본 앱 세션은 `0ec97178…`이며 개인 Meta ID 원문은 문서에 기록하지 않는다.

### EXIT Point와 연속 모드 결과

- 첫 `Education/LeakResponse` 회차 `4a221555…`는 `mode_session_started` 뒤 EXIT Point로 복귀했다.
  동일 `modeSessionId`의 `mode_session_completed`는 없고, 앱의 `session_ended/application_quitting`도
  기록되지 않았다. 따라서 정상 완료나 앱 종료로 오기록되지 않았음을 확인했다.
- 현재 JSONL에는 EXIT Point 전용 이벤트가 없다. 중도 복귀는 `PpeArea → PpeEducationSelected` 상태 전이와
  해당 `modeSessionId`의 완료 이벤트 부재로만 확인된다. 이 근거만으로 대시보드에서 중도 퇴장 원인을
  확정하지 않는다.
- 같은 앱 실행에서 `Training/LeakResponse` 회차 `ca2ae94dd…`와 `Test/LeakResponse` 회차
  `2ddf9b94…`를 정상 완료했다. 두 회차는 서로 다른 `modeSessionId`를 사용하고 각각 정확히 한 개의
  `mode_session_completed`로 닫혔다.
- Training은 `modeElapsedSec=96.15`, Test는 `modeElapsedSec=151.52`였고 두 회차 모두 퀴즈 5/5,
  `ppeWrongCount=0`으로 기록됐다.

### 종료·서버 대조와 기존 사용자 판정

- 앱 내부 `종료하기` 직후 Quest 앱 PID가 사라졌다. 앱을 다시 실행하기 전에 로컬 JSONL과 업로드 ACK는
  이벤트 302개, 마지막 `sequence=302`, `completed=true`로 일치했다.
- 운영 MySQL을 서버 저장소의 읽기 전용 repository 경로로 조회한 결과 같은 세션은 이벤트 302개,
  마지막 `sequence=302`, `status=completed`, `endReason=application_quitting`으로 일치했다. 다음 실행의
  durable recovery를 기다리지 않고 종료 ACK가 완료됐다.
- 오늘 최초 Build 8 실행의 원본은 `metaProbeState=Completed`, `metaWelcomeState=Returning`을 기록했고
  `VO_PPE_INTRO_002_Welcome_Old`가 시작·종료됐다. 사용자는 최초 실행과 이후 재실행 화면에서 모두 기존
  사용자 안내를 확인했다. 재실행 관찰은 사용자 실기 증거이며 별도 새 JSONL 세션으로 계측하지 않았다.

### 검증 수준과 남은 항목

- **Quest/OpenXR 확인:** Meta Alpha Build 8 설치, EXIT Point 복귀, Training·Test 정상 완료, 결과 화면
  복귀, 앱 내부 종료와 기존 사용자 안내를 실제 HMD에서 확인했다.
- **서버 통합 확인:** Quest 원본과 운영 DB의 세션 ID, 이벤트 수, 마지막 sequence, 두 완료
  `modeSessionId`, 앱 종료 상태가 일치했다.
- **후속 Education 확인:** 별도 앱 세션 `9db5c193…`에서 `LeakResponse/Education` 회차
  `99727875…`를 정상 완료했다. `mode_session_started(sequence=19)`와
  `mode_session_completed(sequence=186)`가 같은 `modeSessionId`로 연결됐고, 퀴즈 5/5,
  `ppeWrongCount=0`, `modeElapsedSec=97.32`로 기록됐다. 음성 안내는 사용자 합의에 따라 스킵했으므로
  이 회차를 음성 품질 검증 근거로 사용하지 않는다.
- **후속 종료·서버 대조:** 결과 모달 복귀 후 앱 내부 `종료하기`를 사용했다. Quest PID가 사라졌고 로컬
  JSONL과 업로드 ACK는 이벤트 195개, 마지막 `sequence=195`, `completed=true`로 일치했다. 운영 MySQL도
  같은 세션을 이벤트 195개, 마지막 `sequence=195`, `status=completed`,
  `endReason=application_quitting`, `appVersion=1.0.0`으로 저장했다.
- **미완료:** EXIT Point 전용 원본 이벤트, 운영 조회용 HTTP token과 조회 API, Quest 양안·거울 시각 품질,
  스킵하지 않은 음성 품질과 성능 체감은 이번 계측으로 확정하지 않았다.
- **문서 검증:** `git diff --check`에서 이번 문서 변경의 공백 오류는 없었다. Unity
  `DocumentationPolicyHarness.Validate`는 학원 PC의 `No valid Unity Editor license found`로 종료 코드
  `198`을 반환해 실행되지 않았으며, 문서 정책 실패로 해석하지 않는다.

## 2026-09-11 Meta Horizon Store 제출 준비와 Build 8 서버 연동 재확인

### 이번 작업의 범위

- 학원 Quest 2에 Meta Alpha 채널의 Build 8(`versionName=1.0.0`, `versionCode=8`)을 설치한 상태에서
  EXIT Point 복귀, Training·Test·Education 완료, 앱 내부 정상 종료와 기존 사용자 안내를 확인했다.
- 위 HMD 실행 결과를 근거로 Meta Horizon Store의 제출 바이너리, 무료 가격, 앱 메타데이터, 콘텐츠 등급과
  공유 설정을 준비했다.
- 이번 기록은 Store 검토 제출 준비와 서버 수신 검증을 정리한다. 운영 대시보드 연결 완료나 Store 검토
  승인까지 합쳐서 완료로 기록하지 않는다.

### Meta Horizon Store 제출 설정

- 제출 바이너리는 package `com.tycheworks.immersa.safetyvr`의 `1.0.0` Build 8이며, Alpha 채널에서 실제
  설치·실행한 같은 빌드를 Production(Store) 제출 대상으로 선택했다.
- 가격은 무료로 설정했고 콘텐츠 등급은 청소년 이상으로 지정했다.
- 앱 유형은 `Immersive`, 실행 모드는 `Native`다. 앱에서 실제 지원하는 언어는 한국어만 선택했다.
- Store 메타데이터의 기본 언어 `English (US)`는 현재 제출 화면에서 제거되지 않았다. 이를 앱의 영어 지원으로
  해석하지 않고, 영어 기본 Store 설명에는 `Korean language only.`를 명시하고 한국어 현지화 메타데이터를
  별도로 작성했다.
- 영어 검색 키워드는 `ChemicalSafety`, `PPE`, `VRTraining`, `WorkplaceSafety`, `ConfinedSpace`로 입력했다.
- 사양의 지원 입력에서는 실제 구현과 일치하도록 Touch controller만 사용하고 controller-free hand tracking인
  `Hands`는 제외했다. Social features는 `No`, 앱 지원 언어는 `Korean`으로 설정했다.
- 개발자와 퍼블리셔는 `Tyche works`, 웹사이트는
  `https://immersa.tycheworks.com/chemical-safety-training`, 개인정보처리방침은
  `https://softcastella.github.io/tycheworks-safetytrainingvr-privacy/`로 입력했다. 외부 지원 링크와 서비스
  약관은 선택 항목이므로 비워 뒀다.
- 한국어와 영어 메타데이터의 자산은 언어별 입력으로 관리했다. 아이콘은 512x512 불투명 배경 이미지로
  보완했으며, 앱 메타데이터의 이름·카테고리 분류·사양·상세 정보·자산·콘텐츠 등급은 제출 화면에서 모두
  초록색 완료 상태를 확인했다.
- 검수자 노트에는 별도 계정 로그인이 필요하지 않고 시작 화면의 입력값은 테스트용 닉네임이라는 점, 앱 UI·안내·
  음성이 한국어 전용이라는 점, PPE 시나리오와 Education·Training·Test 진입 방법을 안내했다.
- 공유 설정 확인 화면에서는 미러링, 라이브스트리밍과 동영상 녹화가 선택된 상태를 확인했다. 이 설정은
  Quest의 화면·콘텐츠 공유 기능이며 Multi-User의 앱 구매 권한 공유와 구분한다.
- Meta Horizon Store 재제출 후 제출 상태가 `제출됨`으로 표시되는 것을 확인했다. 이는 Meta가 제출물을
  접수한 초기 상태이며, `검토 중` 또는 승인 완료와는 구분한다. 현재 제출된 바이너리와 메타데이터는 Meta의
  요청이 있기 전까지 수정하거나 제출 취소하지 않는다.

### Meta 데이터 사용 확인(DUC)과 개인정보 고지

- Meta Developer Dashboard의 `데이터 사용 확인`은 `검토 중`이다. 현재 제출에는 변경·추가·삭제 항목이 없고,
  앱이 실제 사용하는 `사용자 ID`와 `사용자 연령대`가 각각 `활성`으로 표시된다.
- 앱은 Horizon 사용자 이름과 프로필 사진을 요청하거나 저장하지 않으므로 `사용자 프로필` 권한을 추가하지
  않는다. 친구, 차단된 사용자, 초대, 파티, 챌린지와 앱 내 구매 권한도 현재 기능에 필요하지 않다.
- Meta 앱 범위 사용자 ID는 신규·기존 사용자를 구분하고 가명 훈련 세션을 연결하는 데 사용한다. User Proof는
  서버에서 해당 앱 범위 ID를 검증하는 일회성 인증 자료이며 훈련 기록이나 대시보드 조회 자료로 보존하지 않는다.
- 공개 개인정보처리방침은 Meta 앱 범위 사용자 ID, 연령대, 세션·회차 ID, 과정·모드·진행·완료, 수행 시간,
  오류·재시도·힌트·종료 사유·앱 버전과 서버 접속·보안 로그의 수집 범위를 고지한다. 교육 현황 집계와 서비스
  개선 목적, Vultr 대한민국 인프라, 보유 기간과 삭제 요청 경로도 함께 명시한다.
- 운영 대시보드는 위 고지 범위 안에서 서버에 이미 저장된 훈련 기록을 인증된 관리자에게 조회·집계해 표시한다.
  새로운 Meta 권한, 사용자 프로필, 원시 XR 자세, 마이크 또는 패스스루 카메라 데이터를 추가 수집하는 기능은
  이번 대시보드 후속 범위에 포함하지 않는다.

### Build 8 실제 서버 전송 근거

- 앱 세션 `0ec97178…`은 Quest 로컬 JSONL과 업로드 ACK에서 이벤트 302개, 마지막 `sequence=302`,
  `completed=true`로 일치했다. 운영 MySQL의 같은 세션도 이벤트 302개, 마지막 `sequence=302`,
  `status=completed`, `endReason=application_quitting`으로 일치했다.
- 같은 앱 실행에서 `Training/LeakResponse`와 `Test/LeakResponse`는 서로 다른 `modeSessionId`와 각 한 개의
  `mode_session_completed`를 기록했다. Training은 96.15초, Test는 151.52초였고 두 회차 모두 퀴즈 5/5,
  `ppeWrongCount=0`이었다.
- 후속 앱 세션 `9db5c193…`의 `LeakResponse/Education` 회차도 정상 완료했다. Quest JSONL·ACK와 운영
  MySQL이 이벤트 195개, 마지막 `sequence=195`, 완료 상태에서 일치했고 서버에는 `appVersion=1.0.0`과
  `endReason=application_quitting`이 저장됐다.
- 위 결과로 **Build 8의 Quest 원본 생성 → 운영 HTTPS 업로드 → 서버 운영 MySQL 저장**은 두 실제 앱 세션에서
  통합 확인됐다. 이후 모든 네트워크 상태나 모든 미래 세션의 성공을 보장하는 결과로 확대 해석하지 않는다.

### 검증과 남은 후속 작업

- `node Tools/AgentHandoffHarness.mjs`는 PASS했다.
- `node Tools/MetaAlphaSubmissionGateHarness.mjs`는 현재 `AndroidBundleVersionCode가 code 7이 아닙니다`로
  FAIL했다. 하네스의 고정 기대값이 Build 8 이전 기준인 것이 원인이며, 위 실제 Build 8 서버 수신 실패를
  의미하지 않는다. 다음 코드 작업에서 기대 version code를 현재 제출 기준에 맞추고 정적 계약과 실제 HMD
  통합 증거를 계속 분리한다.
- EXIT Point 전용 원본 이벤트는 아직 없으므로 중도 퇴장 원인을 확정 지표로 표시하지 않는다.
- 운영 대시보드의 조회용 HTTP 인증과 실제 화면 조회는 아직 완료하지 않았다. Build 8 서버 수신 완료와
  대시보드 연결 완료를 구분한다.
- Store 제출은 `제출됨`, DUC는 `검토 중`까지 확인했다. Quest 양안·거울 시각 품질, 스킵하지 않은 전체 음성
  품질과 성능 체감은 별도 수동 확인 항목으로 남긴다.

### Meta 검토 중 진행할 운영 대시보드 후속 작업

1. 제출된 Build 8은 변경하지 않는다. 이미 검증된 운영 HTTPS 전송과 MySQL 원본을 대시보드 데이터 원천으로
   사용하고, 대시보드 표시를 위해 APK에 새로운 수집 항목이나 권한을 추가하지 않는다.
2. 서버의 읽기 전용 조회 인증을 먼저 확정한다. 관리자 브라우저에는 조회 토큰, Meta User Proof, 데이터베이스
   자격 증명을 노출하지 않고 서버 세션 또는 동등한 관리자 인증 뒤에서만 조회 API를 호출한다.
3. 운영 조회 API `GET /api/training-telemetry/sessions`,
   `GET /api/training-telemetry/sessions/{sessionId}`,
   `GET /api/training-telemetry/participants`,
   `GET /api/training-telemetry/participants/{participantId}`를 대시보드에 연결한다.
4. 첫 통합 기준은 Build 8의 검증 세션 두 건으로 고정한다. 앱 세션 `0ec97178…`은 이벤트 302개와 마지막
   `sequence=302`, 앱 세션 `9db5c193…`은 이벤트 195개와 마지막 `sequence=195`가 운영 DB 상세와 화면에서
   일치해야 한다.
5. 사용자·세션·시나리오·모드, 완료 상태, 수행 시간, 퀴즈 정답 수와 점수, PPE 오선택 수 및 원본 이벤트
   sequence를 근거가 있는 값으로 표시한다. 앱 범위 사용자 ID는 전체 원문을 불필요하게 노출하지 않고
   가명 참여자 식별 또는 마스킹된 값으로 다룬다.
6. EXIT Point는 전용 원본 이벤트가 없으므로 대시보드에서 확정 종료 원인으로 표시하지 않는다.
   `application_quitting`, 정상 `mode_session_completed`, 완료 이벤트가 없는 회차와 미종료 세션을 서로
   다른 상태로 유지한다.
7. 완료 판정은 서버 코드 존재, API 응답, 운영 DB 적재와 대시보드 화면 조회를 분리한다. 인증된 브라우저에서
   위 두 기준 세션의 이벤트 수·마지막 sequence·완료 상태·모드 결과를 상세 원본까지 추적한 뒤에만
   `운영 대시보드 연결 완료`로 기록한다.
