# 화학물질 안전교육 VR 기능 정의서

문서 버전: 1.0  
작성일: 2026-09-15  
적용 범위: Meta Quest/OpenXR 클라이언트, Express 서버, 운영 대시보드

## 1. 제품 범위

독립형 Meta Quest 환경에서 교육생이 작업계획을 선택하고 PPE를 점검·착용한 뒤 교육·훈련·테스트를 수행한다. 클라이언트는 세션 이벤트를 로컬 JSONL로 보존하고 HTTPS API로 서버에 전송한다. 서버는 참가자·식별자·세션·이벤트 원본을 저장하며 운영 대시보드는 인증된 관리자에게 사용자·플레이·세션 결과를 제공한다.

현재 실행 범위는 `ConfinedSpace`와 `LeakResponse` 두 작업계획의 `Education`, `Training`, `Test` 세 모드다. 혼합기동 전체 절차, 혼합기 내부 사고 체험, LMS 자동 연동은 후속 범위다.

## 2. 클라이언트 기능

| ID | 기능 | 입력 | 처리 및 상태 | 결과·기록 | 주요 근거 |
|---|---|---|---|---|---|
| C-01 | 앱 초기화·씬 진입 | 앱 실행, XR 런타임 | `0_App → 1_Title → 2_Intro → 3_Loading → 4_PPE_Room` 순서로 초기화 | 현재 씬, 앱 버전 | `Assets/Scripts/AppSceneBootstrap.cs`, `LoadingSceneController.cs` |
| C-02 | 컨트롤러 교육 | Trigger, Grip, 이동 입력 | 안내 단계와 조작 성공 상태를 순서대로 판정 | 안내 완료 이벤트 | `PPEControllerEducationEntry.cs`, `ControllerGuideMiniActivator.cs` |
| C-03 | 작업계획·모드 선택 | 카드 Trigger | 작업계획과 모드를 세션 프로필에 저장하고 PPE 흐름 시작 | 선택된 `workPlan`, `mode` | `PPEVoiceFlowDirector.cs`, 관련 씬 카드 |
| C-04 | PPE 선택·잡기 | PPE marker Direct Grab, Grip | 마커 대상만 선택하고 Action Panel을 표시 | 선택·잡기 시도·실패 이벤트 | `PPEMarkerToggleGrab.cs`, `PPEItemIdentity.cs` |
| C-05 | PPE 점검·결함 판정 | 정상/불량 PPE 선택, 사용·폐기 Trigger | 점검 상태를 갱신하고 불량 장비는 폐기 후 재선택 가능하게 복원 | PPE별 검사·판정 결과 | `PPEActionPanelController.cs`, `PPEInspectionState.cs` |
| C-06 | PPE 착용·거울 확인 | 사용 확정, 장비별 착용 조건 | `PPE Body Anchor → hazmat_suit_on_10` 기준으로 착용 상태를 구성하고 필수 조건·거울 확인 시간을 판정 | PPE 완료, 거울 확인 이벤트 | `PPEEquipmentVisualController.cs`, `PPEHazmatEquipController.cs` |
| C-07 | 모드별 음성·안내 | 단계 전환, 오류, 완료 | Education은 상세 설명·교정, Training은 기본 지시, Test는 수행 중 정답 안내 없이 결과만 제공 | VOICE/SFX 및 상태 이벤트 | `PPEVoiceFlowDirector.cs`, `AudioManager.cs` |
| C-08 | 퀴즈·테스트 결과 | 문항 선택 | 작업계획별 문항을 채점하고 점수·정답 수·오선택·소요시간을 계산 | 결과 화면, `mode_session_completed` | `PPEQuizController.cs`, `PPEQuizQuestionCatalog.cs` |
| C-09 | 완료·복귀·종료 | 완료 Trigger, EXIT Point, 앱 종료 | 완료 상태를 확정하고 모드 선택 또는 시작 경로로 복귀. 종료 시 마지막 이벤트를 flush | 완료·종료 사유·세션 종료 | `PPEFinaleController.cs`, `PPEExitTeleportMarkerRelay.cs` |
| C-10 | 로컬 텔레메트리 | 기능 상태 변화 | 세션 ID와 sequence를 부여해 JSONL 원본에 기록하고 재전송 큐를 관리 | 원본 JSONL | 텔레메트리 관련 `Assets/Scripts` 코드 및 QA 문서 |

## 3. 서버·대시보드 기능

| ID | 기능 | API 또는 화면 | 처리 결과 | 주요 근거 |
|---|---|---|---|---|
| S-01 | Meta 인증 | `POST /api/training-telemetry/auth/meta` | 앱 범위 사용자 식별자를 검증하고 업로드 권한 발급 | `training-telemetry-routes.js` |
| S-02 | 세션 생성 | `POST /api/training-telemetry/sessions` | 참가자와 세션을 생성하고 앱 버전·씬·모드·작업계획 저장 | `training-telemetry-service.js`, migration 009~011 |
| S-03 | 이벤트 배치 저장 | `POST /api/training-telemetry/sessions/{sessionId}/events` | 1~50개 이벤트의 ID·sequence 중복과 payload 무결성 검증 후 저장 | migration 012, `training-telemetry-repository.js` |
| S-04 | 세션 완료 | `POST /api/training-telemetry/sessions/{sessionId}/complete` | 완료 상태, 종료 시각, 완료 이벤트를 반영 | `training-telemetry-routes.js` |
| S-05 | 원본 조회 | `GET /api/training-telemetry/sessions`, `/{sessionId}` | 세션 목록·상세·이벤트 원본 조회 | `training-telemetry-repository.js` |
| S-06 | 사용자·참가자 조회 | `GET /api/training-telemetry/participants`, `/{participantId}` | Meta 식별자와 사용자별 플레이 이력·완료 결과 조회 | `training-telemetry-routes.js` |
| S-07 | 관리자 인증·보안 | `/api/server-admin/login`, `/session`, `/logout` | 관리자 세션, 역할, 만료, 감사 로그와 허용 IP 관리 | `server-admin-routes.js`, migration 003~008 |
| S-08 | 운영 대시보드 | 관리자 웹 화면 | 사용자 목록, 플레이 이력, 세션 상세, 완료·소요시간·PPE 결과와 집계 지표 표시 | `public/dashboard`, `public/site` |

## 4. 데이터 구조

```text
training_telemetry_participants
 ├─ training_telemetry_identities
 └─ training_telemetry_sessions
     └─ training_telemetry_events
```

- `participants`: 프로젝트별 참가자 기준과 최초·최근 확인 시각
- `identities`: `source_project`, `identity_type`, `identity_value`로 외부 식별자 연결
- `sessions`: `session_id`, `schema_version`, `app_version`, `scene`, `mode`, `work_plan`, `status`, `end_reason`
- `events`: `event_id`, `sequence`, `event_type`, `payload_json`, `payload_sha256`, 서버 수신 시각
- 관리자 계정·세션·권한·감사 로그·허용 IP·알림 구독은 별도 관리자 영역에서 관리

## 5. 공통 오류 처리

| 상황 | 처리 | 사용자 또는 운영자 표시 |
|---|---|---|
| 부적합 PPE 선택 | 판정 실패, 재시도 안내 | 현재 단계 안내와 오류 음성 |
| 점검·착용 누락 | 다음 단계 진입 차단 | 누락 PPE와 필요한 행동 표시 |
| 중복 이벤트·sequence 오류 | 서버 저장 거부 또는 멱등 처리 | 업로드 오류 로그, 재전송 대상 유지 |
| 인증 실패 | 세션·업로드 차단 | 앱 오류 상태, 서버 보안 로그 |
| 완료 후 이벤트 전송 | 서버에서 추가 이벤트 거부 | 세션 완료 상태 유지 |

## 6. 구현 및 검증 기준

- 구현 기준은 클라이언트 `main@1e8e8a5`와 서버 `main@847b349`의 코드·마이그레이션을 사용한다.
- 정적 코드·씬 확인, Unity Editor 확인, Quest/OpenXR 확인, 운영 서버·대시보드 조회는 서로 다른 검증 단계로 기록한다.
- 운영 대시보드의 집계값은 `training_telemetry_events` 원본 이벤트를 계산 근거로 사용한다.
- 본 문서는 기능 정의 기준이며, 실제 실행·배포·시연 결과는 별도 테스트 기록과 운영 로그로 관리한다.

