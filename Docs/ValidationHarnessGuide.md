# 에이전트 검증 하네스 라우팅 가이드

## 목적

이 문서는 Codex, Claude Code 및 이후 자동화 에이전트가 저장소의 기존 검증 하네스를 같은 기준으로
선택하고 실행하도록 하는 공통 진입점이다. `AGENTS.md`가 작업 원칙의 기준본이며, 이 문서는 원칙을
복제하지 않고 하네스 선택과 실행 순서만 정의한다.

## 세션 시작 게이트

1. 저장소 루트에서 `node Tools/AgentHandoffHarness.mjs`를 실행한다.
2. `git status --short --branch`, 현재 브랜치와 `HEAD`를 기록한다.
3. 미커밋 파일은 사용자 작업으로 취급한다. 소유권과 범위를 확인하기 전에는 `checkout`, `restore`,
   `reset`, 일괄 포맷 또는 생성기 재실행을 하지 않는다.
4. `ProjectSettings/EditorBuildSettings.asset`에서 현재 활성 빌드 씬을 확인한다.
5. Unity Editor가 실행 중이면 열린 씬 경로, `Scene.isDirty`, Play Mode, 컴파일 및 Import 상태를 확인한다.
6. 작업과 직접 관련된 기존 문서와 하네스 소스를 읽고, 변경 전 기준 하네스를 먼저 실행한다.

## 실행 안전 규칙

- `Validate`, `Diagnose`, `Report` 메뉴와 `Configure`, `Apply`, `Build`, `Setup`, `Rebuild`, `Repair`,
  `Install`, `Place`, `Wire`, `Sync`, `Unify` 메뉴를 구분한다. 후자는 씬이나 에셋을 변경할 수 있으므로
  사용자가 요청한 변경에 직접 필요한 경우에만 실행한다.
- 이름이 `Validate`인 메서드도 실행 전에 구현을 읽어 저장, 씬 전환, Play Mode 요구 여부를 확인한다.
- 같은 프로젝트를 Unity Editor가 열고 있으면 별도 `-batchmode` Unity 인스턴스를 동시에 실행하지 않는다.
- `ValidateBatch` 메서드는 `EditorApplication.Exit`을 호출할 수 있다. 열린 Editor 또는 Unity MCP에서
  호출하지 않고, Editor가 완전히 종료된 독립 배치 검증에서만 사용한다.
- Unity MCP 연결 파일에는 머신별 경로·승인 우회 값을 추측해 기록하지 않는다. 연결 도구가 없으면
  Unity 메뉴를 사용하거나 Editor 종료 후 검증 가능한 `ValidateBatch`만 실행한다.
- 컴파일이나 Shader Import 중에는 Play Mode와 Quest/OpenXR 로더를 시작하지 않는다.
- 하네스 PASS는 그 하네스가 검사한 정적·직렬화 계약의 성공이다. Game View, Play Mode, Quest 양안,
  실제 입력·오디오·네트워크 성공으로 확대 해석하지 않는다.

## 핵심 하네스 라우팅

| 변경 또는 진단 범위 | 우선 실행할 메뉴/명령 | 추가 확인 |
| --- | --- | --- |
| 에이전트 지침·하네스 인수인계 | `node Tools/AgentHandoffHarness.mjs` | `CLAUDE.md`가 `AGENTS.md`와 이 문서를 가져오는지 확인 |
| 프로젝트 문서 정책 | `Tools > Documentation > Validate Authoring Policy` | `DocumentationPolicyHarness.Validate` |
| Git 커밋·푸시 메시지 | `node Tools/KoreanCommitMessageHarness.mjs --self-test` | 실제 커밋·푸시는 `.githooks/commit-msg`, `.githooks/pre-push`, `core.hooksPath` 확인 |
| 빌드 씬 순서·GUID·전환 참조 | `Tools > Build > Validate Scene Dependencies` | `SceneDependencyValidationHarness.Validate` |
| App→Title→Intro→Loading 시작 동기화 | `Tools > XR > Validate App Startup Synchronization` | 실제 전체 시작 경로 Play |
| PPE 룸 통합 회귀 | `Tools > PPE > Validate Locomotion PPE Regressions` | `PPELocomotionPpeRegressionValidationHarness.Validate`; 변경 소비자별 세부 하네스 추가 |
| PPE 카드·XR UI 입력·마커·텔레포트 참조 | `Tools > PPE > Validate Card Ray Selection` | `PPERoomCardRaySelectionHarness.Validate`; Quest Trigger는 별도 실기 검증 |
| Train/Test·퀴즈·완료 복귀 | `Tools > PPE > Validate Train Test Modes` | `PPETrainTestModeValidationHarness.Validate`; 각 모드 Play 및 Quest 확인 |
| 시나리오 퀴즈 데이터 | `Tools > PPE > Validate Scenario Quiz Pools` | 보기·정답 원본과 화면 표시 대조 |
| 환경 Collider·진열장 접근·Marker 격리 | `Tools > PPE > Validate Room Environment Collision` | `PPERoomEnvironmentCollisionValidationHarness.Validate`; 정적 PASS 뒤 paused Play Probe와 Quest 이동·Grab |
| 가구 Collider와 현재 Renderer 정렬 | `Tools > PPE > Validate Furniture Collider Alignment` | 사용자 이동 Transform 보존 여부 확인 |
| 진열장 각 단 PPE 접근 | `Tools > PPE > Validate Interactive Shelf Access Collision` | Quest에서 각 단 Hover/Select/Grab 확인 |
| 룸 표면 머티리얼 복구 | `Tools > PPE > Validate Room Surface Materials` | live Renderer 및 Quest 양안 확인 |
| 공간 UI·카드·모달 비표시 선행 진단 | 문제 오브젝트 선택 후 `Tools > PPE > Diagnose Selected Object Spatial Context` | 진단 전용이며 자동 수정 금지 |
| XR UI Play Mode Transform 덮어쓰기 | `Tools > UI > Validate XR UI Canvas Play Mode Transform` | Play 진입 전후 직렬화 값 비교 |
| Near/Far 레티클·입력 안전 | `Tools > XR > Validate Near-Far Reticle Safety` | `XRNearFarReticleSafetyHarness.Validate`; Hover와 Click/Select 성공 구분 |
| XR 세션 시작 방향 | `Tools > XR > Validate Session Forward Alignment` | `XRSessionForwardAlignmentValidationHarness.Validate`; Quest 시작 방향 별도 확인 |
| Meta Quest Android 설정 | `Tools > XR > Validate Meta Quest Android Build` | 생성 APK Manifest와 기기 실행 별도 확인 |
| Quest 72Hz 설정 | `Tools > XR > Validate Meta Quest 72 Hz` | 실제 런타임 refresh rate와 성능 확인 |
| 텔레메트리·CSV·대시보드·보고자료 | `Tools > PPE > Validate Training Data Contract` | `PPETrainingDataContractHarness.Validate`; 최신 JSONL, 서버 적재와 조회를 각각 확인 |
| 로컬 텔레메트리 업로드 | `Tools > PPE > Validate Local Telemetry DB Upload` | 서버 브랜치·SHA·응답 로그 확인 |
| 한글 키보드 | `Tools > XR > Hangul Keyboard > Validate Composer`, `Validate Submit Safety` | 컨트롤러와 양손 Poke 실기 확인 |

## 세부 PPE 하네스 선택

- PPE 착용·장비 순서: `PPEHazmatEquipValidation`, `PPEHelmetEquipValidation`,
  `PPEFaceShieldGoggleWearSetup.ValidateFromMenu`, `PPENitrileInnerGloveWearSetup.ValidateFromMenu`
- 방호복·손 모델: `PPEFullSuitFloorGrounding.Validate`, `PPEHandBoneReferenceRepair.ValidateFromMenu`,
  `PPEGloveSuitAppearanceSetup.ValidateFromMenu`
- 결함 시각물: `PPEDefectVisualSetup.ValidateFromMenu`; Game View와 Quest 양안의 Bounds·대비 확인 필수
- 텔레포트·이동: `PPEOfficialTeleportationSetup.ValidateFromMenu`, `PPERoomTeleportationSetup.Validate`
- 태블릿·체크·서명: `PPETabletInteractionSetup.Validate`, `PPETabletSignatureSequenceSync.Validate`,
  `PPETabletSignatureSequenceSync.ValidateImmediateChecklistReveal`
- 음성·SFX: `PPEHazmatVoiceSetup.Validate`, `PPEWearSfxSetup.Validate`, `SceneAudioManagerSetup`의 대상 씬별
  `Validate` 메뉴; 실제 `AudioSource.isPlaying`, `AudioListener`, 출력 장치와 청취 결과는 별도 확인
- 렌더링·머티리얼: `PPEHelmetFbxUnlitSetup.Validate`, `PPEFaceShieldGoggleUnlitSetup.Validate`,
  `PPERoomPropUnlitSetup.Validate`; 커스텀 Shader는 Quest/OpenXR Single Pass Instanced 양안을 별도 확인

## 표준 검증 순서

1. **변경 전 기준:** 대상 씬·코드·Inspector 경로를 확인하고 가장 좁은 관련 하네스를 실행한다.
2. **단일 변경:** 한 번에 한 입력 소비자, 상태 소유자 또는 렌더링 소비자만 변경한다.
3. **정적 확인:** `git diff --check`, 의도한 파일 diff, C# 컴파일 오류와 Unity Console 신규 오류를 확인한다.
4. **Editor 하네스:** 변경 전과 같은 하네스와 필요한 통합 회귀 하네스를 실행한다.
5. **Play Mode:** 필요할 때만 입력·상태 전이·직렬화 덮어쓰기·오디오 실제 상태를 확인한다.
6. **Quest/OpenXR:** Trigger/Grip, 양안 렌더링, 룸스케일 이동, 실제 출력과 성능을 확인한다.
7. **기록:** `정적 확인`, `Unity Editor 확인`, `Play Mode 확인`, `Quest/OpenXR 확인`을 합치지 않고
   완료·미완료로 구분해 관련 기존 문서에 기록한다.

## 배치 실행 템플릿

Unity Editor가 같은 프로젝트를 열고 있지 않고 대상 클래스에 `public static ValidateBatch()`가 실제로
존재할 때만 다음 형식을 사용한다.

```powershell
& '<Unity.exe 절대 경로>' -batchmode -quit `
  -projectPath '<저장소 절대 경로>' `
  -executeMethod '<클래스명>.ValidateBatch' `
  -logFile '<작업별 로그 절대 경로>'
```

종료 코드와 로그의 명시적 PASS를 모두 확인한다. Unity 라이선스 실패, 프로젝트 점유 또는 Import 중단을
하네스 실패나 기능 실패와 합치지 않는다.

## 현재 작업 인수인계 찾기

현재 작업 상태는 이 가이드에 복제하지 않는다. `git status`, 최근 커밋과 관련 기존 `Docs/MeetingNotes`
또는 `Docs/Bug` 문서를 사실 기준으로 사용한다. PPE Room의 최신 장기 인수인계는
`Docs/MeetingNotes/2026-08-25_Production_Server_Meta_Horizon_Release_Plan.md`에서 확인한다.
