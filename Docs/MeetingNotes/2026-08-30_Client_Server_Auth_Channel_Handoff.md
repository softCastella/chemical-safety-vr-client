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
- 동기화 상태: 서버 미러·정적 계약 검증 완료 / 서버 결과의 클라이언트 반영 완료 / 통합 검증 대기

## 목적

클라이언트와 서버 대화 채널의 자동 공유를 전제로 하지 않고, 현재 확인된 인증·로컬 서버·텔레메트리
상태와 서버 확인 요청을 파일 기준으로 인수인계한다. 이 문서의 기준본은 클라이언트 저장소에 두며,
서버 담당자는 같은 상대 경로로 미러링한 뒤 서버 브랜치와 커밋 SHA를 채운다.

## 현재 상태 요약

| 구분 | 상태 | 근거 또는 제한 |
|---|---|---|
| 클라이언트 로컬 텔레메트리 기록 | 확인 | 2026-08-29 Unity Editor 로그에 PPE 이벤트 JSONL 기록이 남음 |
| Meta 앱 범위 사용자 ID | 이번 실행에서 미확인 | 로컬 등록이 Meta ID 또는 활성 PPE 세션을 제한 시간 안에 확인하지 못하고 종료됨 |
| 로컬 Express 실행 | 확인 | 2026-08-30 현재 TCP 3000 listen 및 `GET /` HTTP 200 확인 |
| 로컬 등록 API | 서버 구현 확인 / 왕복 미검증 | 서버 라우트·테스트는 존재하지만 이번 Unity POST/GET 성공 근거가 없음 |
| 텔레메트리 업로드 API | 서버 구현·설정 확인 / 통합 미검증 | ingest 활성화와 서버용 token 설정은 확인했지만 Unity 수신·DB 적재 근거가 없음 |
| 대화 채널·큐 전달 | 미검증 | 클라이언트 코드·Unity 로그만으로 대화가 서버 큐에 전달됐다고 판단할 수 없음 |
| 출시 Player 업로드 | 미구현 | 현재 업로더는 Editor만 허용하고 Player에서는 업로드를 시작하지 않음 |

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
