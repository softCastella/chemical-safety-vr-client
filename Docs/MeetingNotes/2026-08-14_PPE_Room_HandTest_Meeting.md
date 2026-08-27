# 2026-08-14 PPE Room HandTest 회의록

## 1. 작업 기준

- 계속 작업할 기준 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`이다.
- 사용자가 현재 상태 보존용 씬 복사본을 직접 만들었으며, 이후 작업은 복사본이 아니라 `_scale_0`에서 계속한다.
- `_scale`, `_scale_1` 등 다른 variant 씬에는 이번 작업을 확장하지 않는다.
- 씬과 Inspector에 저장된 UI 배치·크기·색·참조를 기준값으로 사용하며, 런타임에서 작성값을 덮어쓰는 경로를 함께 확인한다.

## 2. Wall_MaskLocker 벽면 표현

### 요청과 변경 방향

- 마스크 보관함 뒤의 `Wall_MaskLocker` Plane을 벽을 막는 면으로 사용하기 위해 색과 재질을 적용했다.
- 완전한 원색 표현도 시험했지만, 사용자가 그림자가 표현되는 질감을 선호해 조명을 받는 재질 방향으로 되돌리고 더 밝은 색으로 조정했다.

### 관련 자산

- `Assets/Materials/PPE/Wall_MaskLocker.mat`
- `Assets/Materials/PPE/Wall_MaskLocker_Solid.mat`
- `Assets/Generated/Planes/GeneratedPlane_Mesh 9.asset`
- `Assets/Generated/Planes/GeneratedPlane_Mesh 10.asset`
- `Assets/Generated/Planes/GeneratedPlane_Mesh 11.asset`

### 남은 확인

- Scene View의 밝기만으로 완료 판정하지 않고 Game View와 Quest 양안에서 보관함 내부 대비, 그림자, 벽면 밝기를 확인한다.

## 3. 시작 입력 장치와 음성 스킵 정책

### 정리된 흐름

- 시작 시 컨트롤러를 사용하고, 필요한 컨트롤러 안내가 끝난 뒤 맨손 단계로 전환하는 방향으로 정리했다.
- Trigger 음성 스킵은 패널·카드·모달처럼 Trigger 선택을 소비하는 UI가 없는 상황에서만 동작하도록 구분했다.
- 카드나 모달 버튼 위에서의 Trigger는 선택 입력이 우선이며, 음성 스킵이 해당 클릭을 가로채지 않아야 한다.

### 보존 조건

- 마우스 fallback이나 일반 Pointer 허용을 Quest Trigger 연결의 해결책으로 추가하지 않는다.
- 컨트롤러, Interactor, Ray/Caster, Raycaster, `XRUIInputModule`, press/select action, handler의 기존 XR 입력 경로를 유지한다.

## 4. 카드와 모달 교육 선택 흐름

### 최종 확인한 UI 구조

- 카드 선택 뒤 표시되는 화면은 별도의 `Modal Canvas/Scenario Detail Modal`이다.
- 기존 `1_EduChoice`와 새 `2_Mode` 그룹을 구분한다.
- 새 모드 버튼은 `PPE Edu Mode`, `PPE Training Mode`, `PPE Test Mode`다.
- 현재 연결 범위는 교육 모드이며, 훈련·테스트 버튼은 존재하는 범위만 유지하고 임의의 진행 로직을 추가하지 않는다.

### 상태 순서

1. 카드 선택
2. 카드 안내 뒤 시나리오 상세 모달 표시
3. PPE 착용교육 선택
4. `2_Mode` 표시
5. 교육 모드 선택
6. 모달 안내 및 PPE 구역 이동 안내
7. 텔레포트 도착 후 PPE 교육 시작

### 중요한 변경

- PPE 착용교육 버튼을 누르는 즉시 모달을 닫고 텔레포트로 넘기지 않는다.
- 교육 모드 버튼이 선택된 뒤에만 모달을 닫고 이동 단계로 진행한다.
- 카드 Trigger, 모달 XRI 버튼, 텔레포트 입력 소비자를 한 패치로 우회하거나 자동 완료하지 않는다.

## 5. 키보드와 컨트롤러 교육 진입 표시

### 적용 내용

- 이름 입력 키보드의 Enter 오른쪽 바깥에 기존 키 외형을 활용한 A 표시와 `컨트롤러 교육` TMP를 배치했다.
- A 표시는 현재 기능이 없는 시각 안내다. `XRKeyboardKey`, Collider, Graphic Raycast를 통해 글자 입력이나 클릭을 발생시키지 않는다.
- 키보드의 `VerticalLayoutGroup`에 끌려가지 않도록 레이아웃 자식이 아닌 Canvas 기준으로 배치했다.
- B 표시보다 A 표시가 적합하다는 사용자 결정에 따라 A로 정리했다.

### 시작 표시 순서

- 키보드는 Welcome보다 먼저 보이면 안 된다.
- `_scale_0`의 키보드 Canvas 시작값을 비활성으로 두고, Welcome 종료 후 `NameInput` 상태에서 표시한다.
- 기대 순서는 `Welcome → 이름 입력 키보드 → Enter → 짧은 컨트롤러 가이드 → 카드`다.

## 6. Controller Edu와 Controller Simp 분리

### 목적

- 기존 긴 컨트롤러 교육과 이름 입력 뒤의 짧은 필수 가이드를 서로 다른 Voice 그룹으로 관리한다.
- 긴 버전은 `Controller Edu`, 짧은 버전은 `Controller Simp`가 소유한다.

### 짧은 가이드 음성

- `VO_PPE_CTRL_SIMP_001_Start.mp3`
- `VO_PPE_CTRL_SIMP_002_RayTrigger.mp3`
- `VO_PPE_CTRL_SIMP_003_GripGrab_Release.mp3`
- `VO_PPE_CTRL_SIMP_004_Joystick.mp3`
- `VO_PPE_CTRL_SIMP_005_GuideFollow.mp3`

### 짧은 가이드 시각 매핑

- 001·002: `ControllerGuide/Context/1_Ray`
- `1_Ray` 아래의 기존 Ray 화면: `Card`
- `1_Ray` 아래의 기존 Panel 화면: `Panel`
- 003: `2_Marker`
- 004: `3_Ray_T`
- 005: 004의 `3_Ray_T` 화면 유지

### 아직 연결하지 않은 범위

- 키보드 옆 A 표시를 눌러 긴 `Controller Edu`로 진입하고 다시 원래 흐름으로 복귀하는 동작은 아직 정의·연결하지 않았다.

## 7. 단계별 Voice 폴더 재구성과 재할당

### 폴더 구성

- `Assets/Audio/Voice/0_Intro`
- `Assets/Audio/Voice/1_1_ContSimp`
- `Assets/Audio/Voice/1_2_ContDetail`
- `Assets/Audio/Voice/2_Card`
- `Assets/Audio/Voice/3_Modal`
- `Assets/Audio/Voice/PPE`

### 근본 원인과 대응

- 음원이 단계별 폴더로 이동·교체되면서 일부 `.meta` GUID와 경로가 달라졌고, 씬이 이전 참조를 유지해 Missing Clip이 생겼다.
- Welcome Clip이 Missing이면 Voice Flow가 Welcome을 건너뛰어 키보드부터 표시되는 것처럼 보일 수 있었다.
- Voice Flow, Editor 설정 도구, AudioManager 수집/검증 경로를 새 단계별 음원 구조에 맞춰 갱신했다.
- Welcome 자체에 이름 입력과 Enter 안내가 포함되어 있어 별도 `name.ogg` 단계는 테스트를 위해 비웠다.

### 현재 의도한 음성·상태 순서

1. `Welcome`: 이름 입력과 Enter 안내
2. Enter 제출
3. Controller Simp 001~005
4. `CARD_001`
5. 카드 선택 후 `CARD_002`
6. 모달 표시와 `MODAL_001`
7. PPE 착용교육 선택 후 `MODAL_003 → MODAL_002`
8. 교육 모드 선택 후 `MODAL_004 → PPE_001_MoveToPPE`
9. 텔레포트 도착 후 `PPE_002-2_PPE_Start → PPE_003_Tablet`
10. 태블릿을 처음 내려놓은 뒤 `PPE_EDU_004`

## 8. 컨트롤러 미니 가이드

### 적용 내용

- `ControllerGuide_mini`를 사용자 시야 정면의 왼쪽에 고정하기 위해 `Main Camera` 자식 World Space Canvas로 구성했다.
- 작성 위치는 카메라 로컬 좌표 `(-0.30, 0.02, 1.20)`이다.
- 최초 크기가 너무 작아 사용자 요청에 따라 스케일을 2배인 `0.0008`로 조정했다.
- 조이스틱 클릭 토글과 시작 비활성 상태는 유지한다.

### 남은 확인

- Play Mode와 Quest에서 실제 가독성, 정면 왼쪽 위치, 머리 회전 추적, 양안 표시를 확인한다.
- 위치와 크기 조정이 필요하면 `ControllerGuide_mini` Inspector 작성값을 수정하며 런타임 하드코딩으로 덮어쓰지 않는다.

## 9. PPE 구역과 거울 게이지

### 적용 내용

- PPE 구역 도착 뒤 EDU 002에서 EDU 003까지 순차 재생한 후 태블릿 상호작용으로 이어지도록 구성했다.
- 태블릿을 처음 내려놓을 때 EDU 004가 한 번 재생되도록 기존 Release 이벤트에 연결했다.
- 거울 게이지가 보이지 않던 문제는 상위 `PPE Mirror Gauge Canvas`가 비활성이고 자식만 제어되던 상태로 확인했다.
- 부모 Canvas는 활성, 자식 `Mirror Observation Gauge`는 시작 비활성으로 저장하여 `PPEFinaleController`가 관찰 시점에 자식만 표시하도록 했다.

### 남은 확인

- 태블릿 Release 음성이 첫 Release에만 재생되는지 확인한다.
- 거울 위치마커 도착과 안내 음성 종료 뒤 5초 게이지가 Game View와 Quest 양안에서 표시되는지 확인한다.

## 10. 마지막 요청 3건의 진단 상태

이 절의 세 항목은 문서 작성 시점에 원인 확인까지만 완료했으며 코드·씬 수정은 아직 적용하지 않았다.

### 10.1 좌·우 장갑 안내 중복

- 원인은 `PPEVoiceFlowDirector.OnGloveGrabbed()`가 좌·우 장갑의 Clean 상태만 검사하고 공유 재생 상태를 갖지 않아, 각 장갑을 잡을 때마다 같은 `m_GloveGrabVoice`를 재생한 것이다.
- `3_PPE_Room_Train_Test_1.unity`에만 `m_PlayGloveGrabVoiceOncePerSession`을 켜 좌·우 장갑이 교육 모드 세션의 재생 상태를 공유하게 했다.
- 첫 Clean 장갑 잡기만 안내를 시작하고 두 번째 장갑과 같은 세션의 재잡기는 안내를 다시 시작하지 않는다. 오염 장갑과 훈련·테스트 모드는 이 교육 안내의 재생 상태를 소비하지 않는다.
- 새 모드 세션이 시작되면 공유 상태를 초기화해 그 세션의 첫 Clean 장갑에서 다시 한 번 안내할 수 있다.
- 장갑 Grab, Action Panel, SFX, 착용 슬롯과 PPE 완료 판정은 변경하지 않았다.
- `Tools > PPE > Validate Glove Grab Narration Once (_1)`에 첫 Clean 잡기 허용, 두 번째 및 오염 잡기 차단, 세션 초기화, 훈련 모드 제외를 확인하는 회귀 검사를 추가했다.

### 10.2 모든 PPE 장착 후 EDU 011 누락

- `_scale_0`의 `m_AllPpeCompleteMoveMirrorVoice` 참조는 `4_VO_PPE_EDU_011_MoveToMirror.ogg`에 연결되어 있다.
- 현재는 모든 PPE 완료를 감지하자마자 EDU 011을 재생해, 마지막 장비의 Use 승인 음성 및 시각 슬롯 완료 순서와 음성 교체가 충돌할 가능성이 있다.
- 후속 변경은 현재 재생·로딩 중인 장비 음성이 끝난 뒤 완료 조건과 거울 미도착 상태를 다시 확인하고 EDU 011을 한 번 재생해야 한다.

### 10.3 거울 종료 시 태블릿 체크 안내

- 새 음원 `Assets/Audio/Voice/PPE/4_VO_PPE_EDU_016_CheckTablet.mp3`가 추가됐다.
- 확인한 Asset GUID는 `c8fe1ace3f93aa8448d3e3380bfa1da1`이다.
- `PPEFinaleController`는 `m_TabletChecklistController` 참조를 이미 갖지만 현재 종료 조건은 방호복과 PPE 슬롯만 검사한다.
- 후속 변경은 `PPETabletChecklistController.IsDocumentCompleted`를 별도 완료 조건으로 검사해야 한다.
- PPE만 미완료이면 기존 PPE 미완료 안내, 태블릿만 미완료이면 EDU 016, 둘 다 미완료이면 두 안내를 순차 재생해야 한다.
- 두 조건이 모두 완료된 경우에만 기존 퀴즈 단계로 진행해야 한다.

## 11. 영향 범위

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 주요 런타임 코드:
  - `Assets/Scripts/PPEVoiceFlowDirector.cs`
  - `Assets/Scripts/ScenarioDetailModal.cs`
  - `Assets/Scripts/PPEEquipmentVisualController.cs`
- 주요 Editor/검증 코드:
  - `Assets/Editor/PPEVoiceFlowSetup.cs`
  - `Assets/Editor/PPEVoiceFlowDirectorEditor.cs`
  - `Assets/Editor/PPERoomCardRaySelectionHarness.cs`
  - `Assets/Editor/AudioManagerEditor.cs`
  - `Assets/Editor/SceneAudioManagerSetup.cs`
- 기준 설계 문서: `Docs/PPE_Room_Voice_Narration_Flow_Design.md`

## 12. 완료한 검증과 남은 검증

### 완료한 검증

- 기존 작업 과정에서 Runtime 및 Editor C# 빌드를 실행해 오류 0개를 확인했다.
- 기존 Voice Flow 정적 검증과 `_scale_0` 컨트롤러 테스트 입력 검증을 실행했다.
- 단계별 음원 재배정 뒤 씬 AudioClip 참조 GUID를 대조해 Missing 참조가 없음을 확인했다.
- 미니 가이드의 Camera 자식 구성, 거울 게이지 부모/자식 시작 상태, A 시각 표시의 비상호작용 구성을 정적으로 확인했다.

### 아직 필요한 수동 검증

- 전체 순서 `Welcome → NameInput → Controller Simp → Card → Modal → 교육 모드 → Teleport → PPE`를 Play Mode에서 처음부터 청취한다.
- 카드·모달 Trigger가 음성 스킵에 소비되지 않는지 확인한다.
- Controller Simp 001~005의 화면 매핑과 끊김 없는 재생을 확인한다.
- 미니 가이드와 거울 게이지를 Quest/OpenXR 양안에서 확인한다.
- 마지막 요청 3건은 아직 미적용이므로 수정 후 `장갑 1회`, `EDU 011`, `PPE/태블릿 네 가지 종료 조건`을 별도로 검증한다.

## 13. 환경 판단 기록

- 작업 경로 문자열에 `OneDrive`가 포함된 사실만으로 동기화나 파일 잠금을 원인으로 단정하지 않는다.
- 사용자는 이 프로젝트를 원드라이브로 동기화한 적이 없다고 확인했다.
- 일부 파일 편집 호출이 일시적으로 응답하지 않았지만, 확인되지 않은 환경 원인을 기능 결함과 연결하지 않는다.
- 이후에도 최근 변경 파일·씬 직렬화·이벤트 순서를 먼저 비교하고, 그 근거로 설명되지 않을 때만 실행 환경 범위로 확장한다.

## 14. 훈련·테스트 음원 통합 사전 회의

### 14.1 이번 요청과 보존 범위

- 이번 요청은 새로 임포트한 훈련·테스트 음원을 `Assets/Scenes/3_PPE_Room_Train_Test.unity`에서 시험 구현하는 것이다.
- 이 절의 대상 씬은 기존 기준 씬 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`를 대체하지 않는 별도 실험 씬이다.
- 기존 교육 모드의 카드, 모달, 텔레포트, PPE 착용, 거울, 퀴즈 흐름과 Inspector 작성값은 변경하지 않는다.
- 훈련·테스트 구현을 위해 마우스 fallback, 자동 선택, 자동 완료, 런타임 참조 생성 또는 입력 범위 확대를 추가하지 않는다.
- 상태와 음성 재생의 단일 소유자는 기존 `PPEVoiceFlowDirector`로 유지하고, 실제 AudioClip 배정은 씬 Inspector 직렬화값을 기준으로 한다.

### 14.2 현재 임포트된 음원 11개

| 구분 | 파일 | 현재 의미 또는 예정 용도 | 확정 상태 |
| --- | --- | --- | --- |
| 훈련 모드 선택 | `4_VO_PPE_MODAL_002_TrainSelect.mp3` | 훈련 모드 선택 확인 | 선택 직후 재생 확정 |
| 훈련 이동 | `4_VO_PPE_TRAIN_001_PPE_MoveToPPE.mp3` | 모달 종료 후 PPE 구역 이동 안내 | 재생 순서 확정 |
| 훈련 작업 확인 | `4_VO_PPE_TRAIN_002_CheckPPE_Tablet.mp3` | PPE 진열장 앞 도착 후 태블릿 작업정보 확인과 PPE 착용 지시 | 파일명·발생 이벤트 확정 |
| 훈련 오답 | `4_VO_PPE_TRAIN_003_WrongButton.mp3` | PPE Action Panel의 잘못된 버튼 선택 안내 | 피드백 범위 확정 |
| 훈련 거울 | `4_VO_PPE_TRAIN_004_MirrorCheckPPE.mp3` | 전체 PPE 완료 후 거울에서 착용 상태 확인 | 재임포트 후 발생 이벤트 확정 |
| 훈련 미완료 | `4_VO_PPE_TRAIN_005_UnEnoughPpeTablet.mp3` | PPE 전체 착용 또는 태블릿 체크 중 하나라도 미완료일 때 보완 지시 | 조건 확정 |
| 훈련 퀴즈 | `4_VO_PPE_TRAIN_006_Quiz.mp3` | 거울 확인 통과 후 퀴즈 진행 안내 | 발생 이벤트 확정 |
| 훈련 종료 | `4_VO_PPE_TRAIN_007_TrainEnd.mp3` | 최종 퀴즈 통과 후 훈련 종료와 복귀 안내 | 완료 판정 확정 |
| 테스트 모드 선택 | `4_VO_PPE_MODAL_003_TestSelect.mp3` | 테스트 모드 선택 확인 | 선택 직후 재생 확정 |
| 테스트 이동 | `4_VO_PPE_TEST_001_PPE_MoveToPPE.mp3` | 모달 종료 후 PPE 구역 이동과 테스트 시작 안내 | 재생 순서 확정 |
| 테스트 종료 | `4_VO_PPE_TEST_002_TestEnd.mp3` | 최종 퀴즈 완료 후 테스트 종료 안내, 결과 오버레이 표시 전에 재생 | 완료 판정·재생 순서 확정 |

- `TRAIN_004`는 처음에 잘못된 `WrongButton` 음원으로 들어왔으나 사용자가 다시 임포트했다.
- 재임포트 뒤 `TRAIN_003=오답 버튼`, `TRAIN_004=거울 PPE 확인`으로 역할을 분리했다.
- Unity Editor 진단에서 `TRAIN_004_MirrorCheckPPE`가 정상 임포트된 약 1.57초 모노 AudioClip임을 확인했다.

### 14.3 현재 모드 선택 이벤트 경로

훈련·테스트 버튼의 기존 입력 경로는 다음과 같다.

`Quest Trigger → Near-Far Interactor/UI Ray → TrackedDeviceGraphicRaycaster → EventSystem/XRUIInputModule → PPE Training 또는 Test Button → ScenarioDetailModal.SelectPpeTrainingMode()/SelectPpeTestMode() → PpeLearningModeSelected → PPEVoiceFlowDirector.NotifyPpeLearningModeSelected()`

- `ScenarioDetailModal`은 `Education`, `Training`, `Test` 선택 이벤트를 모두 발생시킨다.
- 현재 `ScenarioDetailModal.NotifyPpeLearningModeSelected()`는 교육 모드만 다음 흐름으로 진행시키고, 훈련·테스트는 선택 이벤트를 알린 뒤 이동을 해제하지 않고 반환한다.
- 현재 `PPEVoiceFlowDirector.NotifyPpeLearningModeSelected()`도 `Education`만 처리하며 `Training`과 `Test`에 대한 상태 전이나 전용 AudioClip 슬롯이 없다.
- 따라서 버튼 입력 이벤트는 존재하지만 신규 훈련·테스트 음원 11개는 아직 Voice Flow와 씬에 직렬화 연결되지 않은 상태다.

### 14.4 확인된 근본 원인과 영향 범위

- 훈련·테스트 버튼이 동작하지 않는 원인은 입력 버튼이나 XR Ray가 없는 것이 아니라, 선택 이후의 상태 전이와 음원 슬롯이 교육 모드에만 구현된 현재 구조다.
- 새 음원 파일을 폴더에 임포트한 사실만으로는 `PPEVoiceFlowDirector`, `AudioManager`, 씬 직렬화 참조에 자동 연결되지 않는다.
- 훈련·테스트를 기존 교육용 `FlowState`와 AudioClip 필드에 그대로 덮어쓰면 교육 모드의 음성 순서와 완료 조건까지 함께 변경될 수 있다.
- 직접 영향 대상은 `Assets/Scripts/PPEVoiceFlowDirector.cs`, `Assets/Scripts/ScenarioDetailModal.cs`, `Assets/Scenes/3_PPE_Room_Train_Test.unity`다.
- 훈련 종료 또는 테스트 종료 판정에 퀴즈, 거울, 태블릿 조건이 사용될 경우 `PPEFinaleController`, `PPEQuizController`, `PPETabletChecklistController`도 소비자 범위에 포함된다.

### 14.5 확정된 훈련 모드 흐름

1. 모드 선택 전까지 기존 공통 루트와 공통 음원을 사용한다.
2. 훈련 모드 버튼을 선택하면 `MODAL_002_TrainSelect`를 재생한다.
3. 모달 선택 음성이 끝나면 모달을 닫고 `TRAIN_001_PPE_MoveToPPE`를 재생하여 PPE 구역 이동을 허용한다.
4. PPE 진열장 앞 텔레포트 포인트 도착 시 `TRAIN_002_CheckPPE_Tablet`를 재생하여 태블릿 작업정보 확인과 PPE 착용 미션을 지시한다.
5. PPE Action Panel에서 잘못된 버튼을 선택하면 오답 아이콘, 오답 SFX, `TRAIN_003_WrongButton`과 `올바른 답을 찾아주세요.` TMP를 표시·재생한다. 정답 버튼이나 정답 문구는 직접 노출하지 않는다.
6. 필수 PPE를 모두 착용하면 `TRAIN_004_MirrorCheckPPE`를 재생하여 거울로 이동해 복장을 확인하게 한다.
7. PPE 전체 착용과 태블릿 체크 중 하나라도 미완료라면 `TRAIN_005_UnEnoughPpeTablet`를 재생하고 미완료 미션을 수행하게 한다.
8. 거울 복장 확인을 통과하면 `TRAIN_006_Quiz`를 재생하여 퀴즈를 진행하게 한다.
9. 최종 퀴즈를 통과하면 훈련 전용 종료 음성 `TRAIN_007_TrainEnd`를 재생한다. 종료 음성 뒤 기존 `돌아가기` 버튼 입력을 기다리고, 버튼 선택 시 기존 모달 복귀 위치로 이동해 카드를 표시하지 않고 `2_Mode`를 표시한다.

#### 훈련 모드 Action Feedback 정책

- 올바른 `사용` 선택은 초록 `Pass Icon`, `Correct Answer` SFX, `사용 선택 완료` TMP를 표시한다.
- 올바른 `폐기` 선택은 초록 `Pass Icon`, `Correct Answer` SFX, `폐기 선택 완료` TMP를 표시한다.
- 두 완료 문구는 정답을 미리 알려주는 설명이 아니라 사용자가 선택한 행동이 정상 승인됐음을 알리는 확인 피드백으로 유지한다.
- 잘못된 선택은 빨간 `Error Icon`, `Wrong Answer` SFX, `TRAIN_003_WrongButton`과 `올바른 답을 찾아주세요.` TMP를 표시·재생한다. 정답 설명은 표시하지 않는다.
- 검사 완료, 정책 설명, 사용 불가, 깨끗한 PPE 폐기 정책 등 `사용 선택 완료`·`폐기 선택 완료`·`올바른 답을 찾아주세요.` 이외의 Action Feedback TMP는 표시하지 않는다.

### 14.6 확정된 테스트 모드 흐름

1. 모드 선택 전까지 기존 공통 루트와 공통 음원을 사용한다.
2. 테스트 모드 버튼을 선택하면 `MODAL_003_TestSelect`를 재생한다.
3. 모달 선택 음성이 끝나면 모달을 닫고 `TEST_001_PPE_MoveToPPE`를 재생하여 PPE 구역 이동과 테스트 시작을 알린다.
4. 테스트 진행 중 잘못된 PPE Action Panel 버튼을 선택하면 오답 아이콘과 SFX만 표시·재생한다. 오답 음성, Action Feedback TMP, 정답 유도 안내는 제공하지 않는다.
5. PPE, 태블릿, 거울, 최종 퀴즈까지 완료하면 `TEST_002_TestEnd`를 재생해 테스트 종료를 알린다.
6. 종료 음성이 끝나면 씬에 작성된 `Result Canvas`를 표시하고, 사용자가 결과를 확인한 뒤 새 `돌아가기` 버튼을 Ray Trigger로 선택할 때 기존 모달 복귀 위치로 이동시킨다.
7. 복귀 위치에서는 카드를 다시 표시하지 않고 `Modal Canvas/Scenario Detail Modal`의 `2_Mode`를 직접 표시한다.

#### 테스트 모드 Action Feedback 정책

- 올바른 `사용` 선택은 초록 `Pass Icon`, `Correct Answer` SFX, `사용 선택 완료` TMP를 표시한다.
- 올바른 `폐기` 선택은 초록 `Pass Icon`, `Correct Answer` SFX, `폐기 선택 완료` TMP를 표시한다.
- 잘못된 선택은 빨간 `Error Icon`과 `Wrong Answer` SFX만 표시·재생한다. 음성, Action Feedback TMP, 정답 설명은 표시하지 않는다.
- 검사 완료, 정책 설명, 사용 불가, 깨끗한 PPE 폐기 정책 등 `사용 선택 완료`·`폐기 선택 완료` 이외의 Action Feedback TMP는 표시하지 않는다.

### 14.7 훈련·테스트 모드 설계 의도

- 훈련 모드는 단계별 정답을 알려주는 튜토리얼이 아니라, 최소한의 미션 지시만 제공하고 사용자가 직접 틀리면서 올바른 행동을 찾아가는 모드다.
- 훈련 음성은 `이동`, `작업정보 확인과 PPE 착용`, `오답 재시도`, `거울 확인`, `미완료 조건 보완`, `퀴즈`, `종료`처럼 미션 경계와 진행 차단 조건에서만 제공한다.
- 잘못된 선택을 했을 때 정답 버튼이나 정답 문구를 직접 노출하지 않고, 훈련 모드에서는 오답이라는 사실과 다시 선택하라는 최소 안내만 제공한다.
- 테스트 모드는 훈련보다 피드백을 더 제한한다. 오답 아이콘과 SFX만 남기고 음성·TMP·정답 유도 없이 사용자의 독립 수행 결과를 평가한다.
- 훈련·테스트 모두 올바른 `사용`·`폐기` 뒤에는 초록 아이콘, SFX, 짧은 완료 문구를 남긴다. 이는 정답 설명이 아니라 선택한 행동의 승인 여부를 확인시키는 피드백이다.
- 교육 모드는 상세 설명, 훈련 모드는 최소 미션 지시와 시행착오, 테스트 모드는 최소 피드백 평가라는 세 모드의 역할을 서로 섞지 않는다.

### 14.8 구현 원칙

- 훈련·테스트 전용 AudioClip은 `PPEVoiceFlowDirector`의 직렬화 필드로 노출하고 `3_PPE_Room_Train_Test.unity` Inspector에서 배정한다.
- 별도 `AudioSource`나 런타임 오브젝트를 만들지 않고 기존 `AudioManager` Voice Source를 재사용한다.
- 필수 Clip이나 참조가 비어 있으면 자동 검색·자동 수리하지 않고, 대상 필드와 씬 경로를 포함한 명확한 오류로 중단한다.
- 먼저 모드 상태 소유권을 추가하고, 다음으로 음원 슬롯을 배정한 뒤, 이벤트 소비자를 한 종류씩 연결한다.
- 입력 경로, 모달 닫힘, 텔레포트 해제, PPE 진행, 종료 판정을 한 패치에서 동시에 변경하지 않는다.
- Editor 설정 도구를 추가할 경우 최초 배정만 수행하며, 이후 Inspector에서 수정한 AudioClip과 UI 값을 반복 실행으로 덮어쓰지 않는다.

### 14.9 현재 검증 수준

#### 정적 확인

- `TRAIN` 8개와 `TEST` 3개, 총 11개 음원 파일이 현재 폴더에 존재함을 확인했다.
- `MODAL_002_TrainSelect`, `MODAL_003_TestSelect`, `TRAIN_004_MirrorCheckPPE`의 변경된 파일명과 Unity import 상태를 확인했다.
- `ScenarioDetailModal`이 세 모드 이벤트를 모두 발생시키지만 교육 모드만 진행시키는 코드를 확인했다.
- `PPEVoiceFlowDirector`가 교육 모드만 처리하며 훈련·테스트 전용 직렬화 필드를 갖지 않는 상태를 확인했다.

#### 적용한 변경

- 이 회의 시점에는 훈련·테스트 런타임 코드, 씬 직렬화 참조, UI, 입력 경로를 수정하지 않았다.
- 중단된 직전 대화에서도 코드와 씬은 읽기 전용으로 조사했으며 구현 패치는 시작하지 않았다.

#### 아직 필요한 검증

- 구현 후 Unity C# 컴파일과 Console 오류를 확인한다.
- `3_PPE_Room_Train_Test.unity`의 `PPE Voice Flow` Inspector에서 11개 AudioClip 참조와 Missing Clip 여부를 확인한다.
- Play Mode에서 교육·훈련·테스트 선택이 서로의 음성·상태·텔레포트 게이트를 침범하지 않는지 모드별로 비교한다.
- Quest/OpenXR에서 각 모드 버튼 Trigger, 음성 순서, 모달 닫힘, 텔레포트 해제, 양안 UI 표시를 확인한다.
- Quest/OpenXR 검증 전에는 훈련·테스트 흐름이 정상이라고 완료 보고하지 않는다.

## 15. Codex 세션 모델 전환 기록

### 확인된 현상

- 세션 중 Codex 창에 HTTP 400 `invalid_request_error`가 표시됐으며, 메시지는 `The 'gpt-5.6-sol' model is not supported when using Codex with a ChatGPT account.`였다.
- 직후 클라이언트가 `gpt-5.6-terra high`로 자동 전환됐고, 대화와 프로젝트 작업은 계속 가능한 상태였다.

### 영향과 판단

- 이 오류는 Unity 프로젝트의 코드, 씬, 음원, 직렬화 참조 또는 컴파일 오류가 아니다.
- 오류 확인과 문서화 과정에서는 프로젝트 파일을 변경하지 않았다.
- 사용자가 제시한 계정 화면에서 ChatGPT Pro 구독과 자동 갱신일이 확인됐다. 따라서 플랜 등급 부족이 이번 `gpt-5.6-sol` 오류의 원인은 아니다.
- Pro 계정에서 `gpt-5.6-sol` 선택이 계속 거부되면 결제 반영 시점, Codex 로그인 계정 일치 여부, 클라이언트 세션의 모델 권한 동기화 상태를 확인한다.

### 후속 확인

- Codex를 다시 열거나 ChatGPT Pro가 결제된 동일 계정으로 재로그인한 뒤 `gpt-5.6-sol` 모델 선택 상태를 다시 확인한다.
- 자동 전환된 `gpt-5.6-terra high`에서 작업이 정상 진행되면 Unity 구현 작업은 해당 모델로 계속할 수 있다.

## 16. 훈련·테스트 퀴즈 및 평가 규칙 확정

### 16.1 적용 대상과 보존 범위

- 대상 씬은 `Assets/Scenes/3_PPE_Room_Train_Test.unity`이며, 기존 교육 모드의 흐름과 피드백은 변경하지 않는다.
- 본 절은 훈련·테스트 모드의 PPE Action Panel, 미니퀴즈, 테스트 결과 오버레이에만 적용한다.
- 테스트 결과를 보기 전까지 정답 문구, 정답 번호, 해설, 재응시 전용 음성은 노출하지 않는다.

### 16.2 훈련 모드

- 훈련은 최소한의 미션 지시를 제공하고 사용자가 시행착오로 올바른 PPE 상태를 찾는 모드다.
- PPE Action Panel의 올바른 `사용` 또는 `폐기` 선택은 초록 `Pass Icon`, `Correct Answer` SFX와 각각 `사용 선택 완료`, `폐기 선택 완료` 문구로 선택 성공만 확인한다.
- PPE Action Panel의 오선택은 빨간 `Error Icon`, `Wrong Answer` SFX, `TRAIN_003_WrongButton` 음성과 `올바른 답을 찾아주세요.` 문구를 표시한다. 정답 버튼이나 정답 문구는 직접 노출하지 않는다.
- 훈련 퀴즈는 오답 선택 뒤 같은 문항에서 다시 선택할 수 있다. 정답 선택 시 초록 아이콘/SFX와 해당 문항의 상세 피드백을 표시한 뒤 다음 문항으로 진행한다.

### 16.3 테스트 모드

- 테스트는 무힌트 수행 평가다. PPE Action Panel 오선택 시 빨간 `Error Icon`과 `Wrong Answer` SFX만 재생하며, 텍스트 피드백·정답·음성 안내는 제공하지 않는다.
- PPE에서 오선택해도 테스트를 즉시 종료하지 않는다. 사용자가 올바른 PPE 처리로 진행을 계속할 수 있게 하되, 오선택 횟수와 각 PPE의 첫 선택 결과를 기록한다.
- 테스트 퀴즈는 문항별 한 번의 선택만 점수에 반영한다. 선택 직후 정오답 아이콘·SFX·해설·정답을 모두 표시하지 않고 다음 문항으로 자동 진행한다.
- 테스트 종료는 PPE 단계의 오선택 시점이 아니라 5개 퀴즈 문항을 모두 푼 뒤에만 발생한다.
- 퀴즈 완료 후 테스트 결과 오버레이를 표시한다. 기본 표시 항목은 `클리어 시간`, `퀴즈 점수(문항당 20점, 총 100점)`, `퀴즈 정답 수`, `PPE 오선택 횟수`다.
- 5개 퀴즈 완료 직후 `TEST_002_TestEnd` 종료 안내를 먼저 재생한다.
- 종료 음성이 끝나면 새 `Result Canvas`를 표시한다. `Result Canvas`는 사용자가 기존 모달 Canvas를 복사해 `Assets/Scenes/3_PPE_Room_Train_Test.unity`에 준비한 시각 작성본이며, 현재 퀴즈 패널과 같은 위치로 이동해 배치했다.
- 이 `Result Canvas`는 이후 결과 로직을 연결하기 위한 UI 원본이다. 현재는 Canvas 복사와 위치·계층·TMP·`돌아가기` 버튼 작성만 완료됐으며, 표시 전환, 결과값 갱신, 버튼 복귀를 처리하는 런타임 로직은 아직 없다.
- 퀴즈 패널과 결과 Canvas의 현재 Inspector 직렬화 위치·크기·색·폰트·배치를 기준값으로 보존한다. 구현 시 퀴즈 종료 뒤 퀴즈 패널을 먼저 숨기고 같은 위치의 결과 오버레이를 표시하며, 런타임 코드에서 두 UI의 Transform이나 표현값을 다시 지정하지 않는다.
- 런타임 코드는 `클리어 시간`, `퀴즈 점수(문항당 20점, 총 100점)`, `퀴즈 정답 수`, `PPE 오선택 횟수`의 동적 내용과 표시 상태만 갱신하며 작성된 UI 표현값을 덮어쓰지 않는다.
- 결과 확인 뒤 새 `돌아가기` 버튼을 Ray Trigger로 선택하면 기존 모달 복귀 위치로 이동하고, 카드를 다시 표시하지 않은 채 `Modal Canvas/Scenario Detail Modal`의 `2_Mode`를 직접 표시한다. 별도의 재응시 음성은 사용하지 않는다.

### 16.4 모드 차이와 교육 의도

| 구분 | 훈련 모드 | 테스트 모드 |
| --- | --- | --- |
| 오선택 직후 | 재시도 안내와 음성 제공 | 아무 피드백 없이 첫 선택만 기록 |
| 정답 선택 뒤 | 상세 피드백 제공 | 정답·해설 미노출 |
| 퀴즈 진행 | 오답 뒤 재선택 가능 | 문항당 첫 선택만 채점, 자동 다음 문항 |
| 결과 | 학습 과정 중심 | 퀴즈 점수·PPE 오선택·시간으로 수행 평가 |

- 두 모드는 같은 PPE 진행 경로를 공유하지만, 훈련은 즉시 교정 학습이고 테스트는 최종 결과로만 평가한다.

### 16.5 공통 복귀 위치와 모드 선택 화면

- 교육·훈련·테스트의 복귀 위치는 현재 사용 중인 기존 모달 복귀 위치로 통일한다. 별도의 복귀 지점이나 모달 Transform을 새로 만들지 않는다.
- 세 모드는 하나의 교육 종료 음성을 공용으로 사용하지 않는다. 교육은 기존 교육 전용 종료 음성, 훈련은 `TRAIN_007_TrainEnd`, 테스트는 `TEST_002_TestEnd`를 각각 사용한다.
- 모든 모드에서 복귀 뒤에는 시나리오 카드를 다시 표시하지 않고 `Modal Canvas/Scenario Detail Modal`의 `2_Mode` 모드 선택 화면을 직접 표시한다.
- 교육 모드는 교육 전용 종료 음성이 끝나면 별도 완료 화면이나 버튼 대기 없이 공용 복귀 연출을 시작한다.
- 훈련 모드는 `TRAIN_007_TrainEnd`가 끝나면 별도 완료 화면이나 버튼 대기 없이 교육과 같은 공용 복귀 연출을 시작한다.
- 테스트 모드는 `퀴즈 완료 → TEST_002_TestEnd → Result Canvas → Back Button` 뒤 같은 공용 복귀 연출을 시작한다.
- 세 모드의 공용 복귀 연출은 `0.75초 페이드아웃 → 암전 중 기존 모달 복귀 위치 이동 → 0.75초 페이드인 → 카드 미표시 → 2_Mode 표시`다.
- `2_Mode`가 표시된 동안에는 기존 모달 상태 규칙에 따라 텔레포트를 차단한다.
- 돌아가기 버튼이 선택된 시점에 직전 모드의 음성·퀴즈·PPE 선택·결과 상태를 한 번만 초기화한다. 초기 진입 시의 카드 선택 흐름은 변경하지 않는다.

### 16.6 변경 전 필수 확인과 검증 계획

- 상태 소유자는 `PPEVoiceFlowDirector`로 유지하고, `PPEQuizController`는 퀴즈 문항·결과 오버레이 표시를 담당한다.
- Action Panel 선택 이벤트에서 모드별 피드백, PPE 오선택 누적, 기존 장비 진행 상태가 서로 충돌하지 않는지 확인한다.
- 테스트 선택부터 퀴즈 종료까지의 시간 기준과 5문항 점수 산정을 Unity Play Mode에서 수동 검증한다.
- `퀴즈 완료 → TEST_002_TestEnd → Result Canvas → 돌아가기 버튼 → 기존 모달 복귀 위치 → 2_Mode` 순서를 Unity Play Mode에서 검증한다.
- 같은 위치를 사용하는 퀴즈 패널과 `Result Canvas`가 동시에 표시되지 않고, 퀴즈 패널 비활성화 뒤 결과 오버레이가 표시되는지 확인한다.
- 교육·훈련의 기존 돌아가기 버튼도 카드를 거치지 않고 동일한 모달 복귀 위치의 `2_Mode`로 연결되는지 확인한다.
- Quest/OpenXR에서 아이콘·SFX·월드 공간 `Result Canvas` 표시, 돌아가기 버튼 Ray Trigger, 모달 복귀와 양안 UI 표시를 별도로 수동 검증한다.

### 16.7 다음 작업 시 후속 조치

- 오늘은 늦은 시간이므로 훈련·테스트 런타임 로직 구현을 시작하지 않는다. 다음 작업 시 아래 순서로 기준 상태를 다시 확인한 뒤 진행한다.
- 작업 대상은 `Assets/Scenes/3_PPE_Room_Train_Test.unity` 하나로 고정하고, 현재 Unity에서 열린 씬과 디스크에 저장된 씬이 같은지 먼저 확인한다.
- 사용자가 준비한 `Result Canvas`의 현재 위치·크기·계층·TMP·버튼과 퀴즈 패널 위치를 기준값으로 기록하고, 구현 과정에서 Transform이나 UI 표현값을 변경하지 않는다.
- `PPEVoiceFlowDirector`의 모드 상태와 종료 음성 선택을 확인한다. 현재 코드에 교육 종료 음성으로 고정된 경로가 있다면 이는 확정 동작이 아니라 수정 대상이다. 교육은 기존 교육 전용 종료 음성, 훈련은 `TRAIN_007_TrainEnd`, 테스트는 `TEST_002_TestEnd`를 선택하도록 분리한다.
- `PPEFinaleController`의 완료 직후 자동 텔레포트와 카드 재표시 경로를 확인하고, 교육·훈련·테스트 각각의 종료 화면과 버튼 대기 흐름으로 교체한다. 세 모드 모두 복귀 뒤 카드를 표시하지 않고 `2_Mode`를 표시한다.
- `ScenarioDetailModal.ShowPpeModeChoices()`가 모달 전체 활성화, 카드 숨김, `2_Mode` 표시, 텔레포트 차단을 모두 보장하는지 확인한다. 복수 `ScenarioDetailModal`이 존재하므로 런타임 검색으로 임의 선택하지 않고 Inspector 직렬화 참조를 사용한다.
- `PPEQuizController`의 교육용 오답 재선택·해설 흐름과 테스트용 첫 선택 채점·자동 다음 문항 흐름을 분리한다. 테스트 결과용 클리어 시간, 퀴즈 점수, 정답 수, PPE 오선택 횟수의 소유자와 초기화 시점을 먼저 확정한다.
- `PPEFinaleController`의 `Complete` 상태와 방호복·태블릿·퀴즈·음성·결과 상태를 다음 모드 진입 전에 한 번만 초기화할 공개 경로를 설계한다. 런타임 자동 수리나 누락 참조 자동 검색은 추가하지 않는다.
- `Result Canvas`에는 표시 상태와 동적 결과 TMP만 직렬화 연결하고, 새 `Back Button`에는 `기존 모달 복귀 위치 이동 → 직전 세션 상태 초기화 → 카드 숨김 → 2_Mode 표시` 순서의 명시적 복귀 처리를 연결한다.
- 구현은 모드 상태와 음원 선택, 퀴즈 규칙, 결과 표시, 복귀·초기화를 한 번에 바꾸지 않고 소비자별로 나눠 적용한다. 각 단계마다 `git diff --stat`과 직렬화 참조를 확인한다.
- 정적 확인과 Unity C# 컴파일 뒤 Play Mode에서 교육 기준 흐름을 먼저 비교한다. 이후 훈련, 테스트 순으로 확인하고, 마지막에 Quest/OpenXR Trigger·양안 UI·음성·텔레포트 게이트를 별도 검증한다.
- Unity Play Mode와 Quest/OpenXR 검증 전에는 훈련·테스트 로직을 완료로 보고하지 않는다.

### 16.8 2026-08-15 훈련·테스트 활성화 적용

#### 적용한 변경

- `PPEVoiceFlowDirector`를 모드 상태와 종료 판정의 단일 소유자로 유지하면서 `Education`, `Training`, `Test`별 선택·이동·PPE·거울·퀴즈·종료 음성을 분리했다.
- `3_PPE_Room_Train_Test.unity`의 Director에 훈련 8개, 테스트 3개 AudioClip을 Inspector 직렬화 참조로 배정했다. 필수 Clip 또는 모달·퀴즈·종료 참조가 비어 있으면 런타임 자동 검색이나 자동 수리 없이 오류로 중단한다.
- 훈련·테스트 모드 선택 음성이 끝난 뒤 기존 모달을 닫고 모드별 이동 음성을 재생한 다음 기존 텔레포트 단계로 진입하도록 연결했다.
- PPE Action Panel은 교육의 기존 상세 피드백을 유지하고, 훈련 오선택에는 빨간 아이콘·오답 SFX·`TRAIN_003_WrongButton`·`올바른 답을 찾아주세요.`만 제공한다. 테스트 오선택에는 빨간 아이콘과 오답 SFX만 제공하고 TMP와 음성을 숨긴다.
- 테스트는 `사용/폐기` 오선택 횟수와 PPE별 첫 `사용/폐기` 결과를 별도로 기록한다. 검사 버튼은 첫 선택 평가에 포함하지 않는다.
- 훈련 퀴즈는 별도 상황형 5문항을 사용하고, 오답 뒤 같은 문항에서 재선택하며 정답 때 문항별 상세 설명 뒤 진행한다. 테스트 퀴즈는 별도 4지선다 5문항의 첫 선택만 채점하고 정오답 아이콘·SFX·해설 없이 자동 진행한다.
- `Result Canvas`의 작성된 Transform·크기·색·폰트·배치는 변경하지 않았다. 런타임은 완료 제목, 지표 표시 여부, 테스트의 시간·점수·정답 수·PPE 오선택 수만 갱신한다.
- `Result Canvas`는 테스트 전용이다. 교육·훈련은 각 종료 음성이 끝나면 버튼 입력 없이 자동으로 공용 복귀 연출을 시작하고, 테스트만 결과 화면의 Back 입력 뒤 같은 연출을 시작한다.
- 공용 복귀 연출은 `0.75초 페이드아웃 → 암전 중 기존 복귀 위치 이동 및 상태 초기화 → 0.75초 페이드인 → 카드 미표시 → 2_Mode 표시` 순서다.
- `PPETrainTestModeValidationHarness`를 추가해 대상 씬의 11개 Clip, 모드 버튼, Director·Quiz·Finale 연결, Result Canvas의 초기 비활성 상태·XR Raycaster·버튼 계층, 작성 위치·스케일·크기, 복귀 참조를 검사한다.

#### 수정이 필요했던 근본 원인

- 훈련·테스트 버튼과 선택 이벤트는 존재했지만 `ScenarioDetailModal`이 교육 모드만 모달 종료와 이동 허용으로 연결했고, `PPEVoiceFlowDirector`에도 훈련·테스트 상태와 음원 슬롯이 없었다.
- 퀴즈는 교육용 오답 재시도만 지원했고, `PPEFinaleController`는 완료 뒤 자동 복귀와 카드 재표시로 고정되어 테스트 결과 화면과 버튼 대기 흐름을 수용할 수 없었다.
- `Result Canvas`는 시각 작성본만 존재하고 결과값·표시 전환·복귀 버튼의 런타임 소비자가 없었다.

#### 영향 범위

- 변경 대상 씬은 `Assets/Scenes/3_PPE_Room_Train_Test.unity` 하나다. Build Settings의 시작 씬과 다른 PPE 씬 variant는 이번 활성화 대상으로 변경하지 않았다.
- 런타임 영향 소비자는 모달, 모드 음성, Action Panel, PPE 진행, 태블릿, 거울 완료, 퀴즈, Result Canvas, 완료 복귀 및 텔레포트 게이트다.
- 새 UI 오브젝트나 런타임 생성 UI, 마우스 fallback, 일반 Pointer 허용, 자동 선택·자동 완료는 추가하지 않았다. 기존 XR UI 입력 경로를 그대로 사용한다.

#### 완료한 검증

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 보조 빌드에서 C# 오류 0개를 확인했다. 기존 패키지의 어셈블리 버전 및 SourceGenerator 경고는 별도 기존 경고로 남아 있다.
- Unity 6000.4.8f1 GUI에서 대상 씬을 열고 변경 코드와 Editor 하네스가 포함된 `Assembly-CSharp.dll`, `Assembly-CSharp-Editor.dll` 생성까지 확인했다.
- 훈련·테스트 문항 적용 전 단계에서는 새 오브젝트나 임의 FileID를 추가하지 않았다. 이후 테스트의 4지선다 요구를 반영할 때는 YAML에 ID를 직접 만들지 않고 `Tools > PPE > Apply Training Test Quiz Content`를 명시적으로 실행해 Unity가 각 페이지의 `Option 4`와 직렬화 FileID를 생성·저장하도록 했다.
- `Tools > PPE > Validate Train Test Modes`를 실행해 11개 모드 음원, 모드 버튼, 퀴즈·결과 참조, 훈련·테스트 문항 은행, 페이지별 4개 버튼·라벨, 테스트 전용 UI 초기 상태, 거울 재검사와 종료 복귀 조건의 PASS를 확인했다.

#### 아직 필요한 수동 검증

- Unity Play Mode에서 교육 기준 흐름을 먼저 비교하고, 세 모드 모두 `페이드아웃 → 암전 중 기존 복귀 위치 이동 → 페이드인 → 2_Mode`가 동일한지 확인한다. 교육·훈련은 종료 음성 직후 자동 진입하고, 테스트는 Result Canvas의 Back 입력 뒤 진입해야 한다.
- 훈련 오선택의 최소 피드백과 재시도, 테스트 PPE 오선택 누적, 테스트 퀴즈 첫 선택 채점·자동 진행, 결과 시간·점수·정답 수·PPE 오선택 수를 실제 조작으로 확인한다.
- Play Mode 진입 전후 Result Canvas와 퀴즈 패널의 RectTransform 및 Inspector 작성값이 변경되지 않고 두 화면이 동시에 표시되지 않는지 비교한다.
- Quest/OpenXR에서 모드 버튼과 결과 `돌아가기`의 Ray Trigger, 음성·SFX, 텔레포트 차단/해제, Result Canvas 양안 표시를 별도로 확인한다.

### 16.9 2026-08-15 사용자 확인으로 변경된 종료 규칙

#### 확정된 변경

- 이 절은 16.5와 16.8의 완료 화면·돌아가기 대기 설명 중 충돌하는 부분보다 우선한다.
- `Result Canvas`는 테스트 모드 전용이다. 교육·훈련 완료에서는 표시하지 않는다.
- 교육은 교육 종료 음성, 훈련은 `TRAIN_007_TrainEnd`가 끝난 뒤 별도 완료 모달이나 버튼 대기 없이 자동으로 페이드아웃한다.
- 페이드가 가려진 동안 기존 직렬화된 복귀 위치로 이동하고 세션 상태를 초기화한다. 예전 교육모드 복귀와 같은 페이드인으로 공간을 먼저 표시한 뒤 `Modal Canvas/Scenario Detail Modal/2_Mode`를 표시한다. 시나리오 카드는 다시 표시하지 않는다.
- 테스트는 `TEST_002_TestEnd → Result Canvas → Back Button → 페이드아웃 → 암전 중 기존 복귀 위치 이동 → 페이드인 → 2_Mode 표시` 순서를 사용한다.
- `PPEFinaleController`에서 테스트만 `ShowTestResultAfterVoice()`로 분기하고, 교육·훈련은 종료 음성 대기가 끝나면 곧바로 공용 복귀 코루틴을 실행하도록 변경했다.
- `PPEQuizController`는 테스트 모드에서만 Result Canvas 참조 완전성을 요구하고 결과값을 갱신한다. 교육·훈련 퀴즈 종료는 결과 UI를 소비하지 않는다.
- 검증 하네스에 Result Canvas가 테스트 분기에서만 열리고 교육·훈련은 자동 복귀 경로를 갖는지 검사하는 회귀 조건을 추가했다.

#### 퀴즈 작성본 확인

- 대상 씬에는 `PPE Quiz World Canvas(Clone)`과 `PPEQuizController`가 각각 하나뿐이지만, 데이터는 교육 작성본·훈련 5문항·테스트 5문항으로 분리한다.
- 새벽 세션에서 전달된 훈련·테스트 문항 원문이 기존 회의록에 기록되지 않아, 구현 시 교육용 5문항을 공용으로 유지한다는 잘못된 판단이 발생했다. 2026-08-15에 사용자가 원문 이미지를 다시 제공했고, 아래 16.10에 문항·선택지·정답·훈련 피드백을 기준 자료로 기록했다.
- `PPEQuizController`는 교육용 TMP 작성값을 보존하고, 훈련·테스트 진입 때 씬에 직렬화된 해당 문항 은행만 표시한다. 훈련은 3지선다, 테스트는 4지선다이며, 테스트용 네 번째 버튼은 교육·훈련에서 비활성화한다.
- 종료 복귀 효과는 사용자가 지목한 예전 교육모드 기준으로 복원했다. 작성값은 `0.75`초이며 `0.75초 페이드아웃 → 암전 중 복귀 이동 → 0.75초 페이드인 → 2_Mode 표시` 순서로 전체 약 `1.5`초다.

#### 검증 상태

- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 순차 보조 빌드에서 오류 0개를 확인했다. 병렬 실행 때 발생했던 Unity 샘플 DLL 잠금 오류는 동일 출력 경로를 동시에 사용한 보조 빌드 충돌이며 순차 빌드에서는 재현되지 않았다.
- Unity 재컴파일 뒤 `Tools > PPE > Validate Train Test Modes`를 실행해 PASS를 확인했다.
- 최종 Play Mode 검증은 훈련 3지선다·문항별 피드백, 테스트 4지선다·무즉시피드백, 교육·훈련 자동 복귀와 테스트 Result Canvas 복귀를 한 번의 통합 실행으로 확인한다.

### 16.10 훈련·테스트 문항 기준본과 적용 기록

#### 변경 전 필수 질문 답변

1. 기존 Inspector/씬 작성값은 교육용 질문·보기와 Quiz Canvas의 Transform·색·폰트를 원본으로 보존한다.
2. 모드 상태 소유자는 `PPEVoiceFlowDirector`, 문항 데이터와 표시 소유자는 `PPEQuizController`다.
3. XR 입력 경로는 변경하지 않고 기존 `Button → onClick → PPEQuizController.ChooseOption()` 연결을 그대로 사용한다.
4. 문항·TMP·버튼 참조가 누락되면 런타임 자동 생성이나 검색으로 수리하지 않고 명확한 오류로 중단한다. 네 번째 버튼 생성은 명시적 Editor 메뉴로만 수행한다.
5. 영향 소비자는 Quiz Canvas, 훈련 피드백, 테스트 채점과 Result Canvas다. 텔레포트·PPE Grab·거울·XR Raycaster 경로는 변경하지 않는다.
6. 변경 전 기준은 교육용 5페이지·페이지당 3버튼이며, 변경 후에는 교육 내용 보존, 훈련 3지선다, 테스트 4지선다와 테스트 전용 네 번째 버튼을 비교한다.
7. 정적 씬 참조와 Runtime/Editor 보조 빌드는 확인했으며, Unity 하네스와 Quest/OpenXR 시각·입력 검증은 별도로 구분한다.

#### 훈련 모드 — 5문항

| 문항 | 상황·질문 | 선택지 | 정답 | 정답 피드백 |
| --- | --- | --- | --- | --- |
| Q1. 방호복 점검 | 방호복을 착용했습니다. 몸을 숙였을 때 지퍼 부분이 팽팽하게 당겨지고 팔을 앞으로 뻗기가 어렵습니다. 어떻게 해야 할까요? | ① 그대로 작업한다.<br>② 지퍼를 조금 열어 움직임을 편하게 한다.<br>③ 몸에 맞는 크기의 방호복으로 교체한다. | ③ | 방호복이 너무 작으면 움직이는 동안 봉제선과 지퍼가 계속 당겨질 수 있습니다. 지퍼를 여는 것이 아니라 몸에 맞는 크기의 방호복으로 교체해야 합니다. |
| Q2. 안전대 점검 | 안전대를 착용했지만 걸을 때마다 장비가 좌우로 흔들리고 송기호스가 몸 주변에서 움직입니다. 가장 먼저 확인해야 할 것은 무엇일까요? | ① 안전모의 턱끈<br>② 안전대의 어깨끈과 허리 고정끈<br>③ 방호복의 지퍼 | ② | 안전대는 양쪽 어깨끈과 허리 고정끈을 고르게 조절해 몸에 밀착시켜야 합니다. 너무 느슨하면 장비가 흔들리고 호스가 주변 구조물에 걸릴 수 있습니다. |
| Q3. 송기마스크 점검 | 송기마스크를 착용했습니다. 마스크와 뺨 사이에 방호복 후드 끝부분이 조금 끼어 있습니다. 어떻게 해야 할까요? | ① 작은 틈이므로 그대로 작업한다.<br>② 마스크를 더 강하게 눌러 착용한다.<br>③ 마스크를 다시 착용하여 후드가 밀착면에 끼지 않도록 한다. | ③ | 머리카락이나 방호복 후드가 마스크의 밀착면에 끼면 틈이 생길 수 있습니다. 얼굴에 고르게 밀착되도록 다시 확인해야 합니다. |
| Q4. 안전모 점검 | 안전모를 착용하고 고개를 숙였더니 안전모가 앞으로 움직이며 시야를 가렸습니다. 올바른 조치는 무엇일까요? | ① 작업 중에는 고개를 숙이지 않는다.<br>② 안전모의 위치와 턱끈 고정 상태를 다시 조절한다.<br>③ 안전모를 조금 뒤로 젖혀 착용한다. | ② | 안전모는 머리에 수평으로 맞추고 턱끈을 고정해야 합니다. 고개를 움직였을 때 흔들리거나 시야를 가린다면 고정 상태를 다시 조절해야 합니다. |
| Q5. 장갑·테이핑 점검 | 장갑과 방호복 소매를 테이프로 연결했습니다. 하지만 손목이 거의 움직이지 않을 정도로 테이프를 강하게 감았습니다. 어떻게 해야 할까요? | ① 강하게 고정됐으므로 가장 안전하다.<br>② 테이프를 한 겹 더 감는다.<br>③ 테이프를 다시 조절하여 틈은 막되 손목 움직임을 방해하지 않게 한다. | ③ | 테이프는 연결 부분을 보조적으로 고정하는 역할입니다. 너무 느슨하면 틈이 벌어지고, 너무 세게 감으면 손목 움직임을 방해할 수 있습니다. |

훈련 오답은 정답을 노출하지 않고 `오답입니다. 다시 선택해주세요.`를 표시하며 같은 문항에서 재선택하게 한다. 정답 뒤에는 위 문항별 피드백과 초록 아이콘·정답 SFX를 표시한 다음 진행한다.

#### 테스트 모드 — 5문항

| 문항 | 질문 | 선택지 | 정답 |
| --- | --- | --- | --- |
| Q1 | 다음 중 작업을 시작하기 전에 반드시 수정해야 하는 방호복 상태는 무엇입니까? | ① 목과 지퍼가 끝까지 닫혀 있다.<br>② 몸을 움직여도 봉제선이 과도하게 당겨지지 않는다.<br>③ 목 부분이 조금 열려 피부가 드러나 있다.<br>④ 몸에 맞는 크기의 방호복을 착용했다. | ③ |
| Q2 | 다음 중 송기식 호흡보호구의 정상 착용 상태로 보기 어려운 것은 무엇입니까? | ① 마스크가 얼굴에 고르게 밀착되어 있다.<br>② 송기호스가 눌리지 않았다.<br>③ 공기가 정상적으로 공급된다.<br>④ 후드 일부가 마스크와 얼굴 사이에 끼어 있다. | ④ |
| Q3 | 다음 PPE 점검 결과 중 가장 적절한 상태는 무엇입니까? | ① 장갑이 짧아 손목 부분의 피부가 보인다.<br>② 안전모 턱끈이 느슨해 고개를 숙이면 안전모가 움직인다.<br>③ 안전대가 느슨해 몸을 움직이면 장비가 흔들린다.<br>④ 장갑의 긴 손목 부분이 방호복 소매를 충분히 덮고 있다. | ④ |
| Q4 | 장갑과 방호복 소매를 연결하여 테이핑하려고 합니다. 다음 중 올바른 방법은 무엇입니까? | ① 소매와 장갑 사이에 틈을 남긴 후 테이프로 고정한다.<br>② 손목이 움직이지 않을 정도로 단단하게 감는다.<br>③ 테이프만으로 장갑과 방호복을 연결한다.<br>④ 장갑과 소매를 충분히 겹친 후 연결 부분을 적절한 강도로 고정한다. | ④ |
| Q5. 종합문제 | 방호복 지퍼와 목 부분은 닫혔고, 장화와 바지단에 빈틈이 없으며, 안전대·송기마스크·송기호스·턱끈·장갑·테이프 상태도 정상이다. 단, 고개를 숙이자 안전모가 앞으로 움직여 시야를 가렸다. 이 작업자는 어떻게 해야 합니까? | ① 나머지 PPE가 정상이므로 작업을 시작한다.<br>② 안전모를 벗고 작업한다.<br>③ 안전모의 위치와 고정 상태를 다시 조절한 후 작업한다.<br>④ 안전모를 손으로 잡은 채 작업한다. | ③ |

테스트는 문항별 첫 선택만 채점하고 정오답 아이콘·SFX·텍스트·해설을 표시하지 않은 채 다음 문항으로 진행한다. Q5의 씬 표시 문장은 원문의 모든 정상 항목을 유지하면서 제한된 Quiz Canvas 안에서 읽을 수 있도록 슬래시 구분 형식으로 압축했다.

#### 적용 및 정적 검증

- `PPEQuizController`에 씬 직렬화 `trainingQuestions`와 `testQuestions`를 추가했다. 런타임 코드에는 문항 문구나 UI 배치를 하드코딩하지 않는다.
- `Tools > PPE > Apply Training Test Quiz Content`를 실행해 각 페이지에 Unity 생성 FileID를 가진 비활성 `Option 4`를 한 개씩 추가하고 질문·보기 TMP 참조와 문항 은행을 씬에 저장했다.
- 교육 모드는 시작 시 캡처한 기존 씬 작성 질문·보기 3개를 복원한다. 훈련은 첫 3개 버튼, 테스트는 4개 버튼을 활성화한다.
- 훈련 정답 인덱스는 0 기준 `2, 1, 2, 1, 2`, 테스트는 `2, 3, 3, 3, 2`로 저장됐다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 순차 보조 빌드에서 오류 0개를 확인했다.
- Unity GUI에서 `Tools > PPE > Validate Train Test Modes`를 실행해 PASS를 확인했다.

## 17. 상세기획서 v0.9 프로젝트 현행화

### 17.1 적용한 변경

- 대상 문서 `Docs/화학물질_안전훈련_VR_사전기획서_V0.9.html`의 기능명세와 R&D를 현재 Unity 프로젝트 구현 기준으로 현행화했다.
- 기능명세에는 기존 항목 수정과 신규 항목을 구분하는 `v0.9 기능명세 현행화 구분` 안내를 추가했다.
- 신규 기능명세로 다음 네 항목을 추가했다.
  - `FS-19`: Welcome·한글 이름 입력
  - `FS-20`: 비동기 로딩·장면 활성화
  - `FS-21`: PPE VOICE 상태 흐름·조건부 안내
  - `FS-22`: 앱 종료
- 기존 기능명세 `FS-00~03`, `FS-05~06`, `FS-13~14`, `FS-16`, `FS-18`을 실제 구현에 맞게 수정했다.
  - 실제 실행 경로를 `0_App → 1_Title → 2_Intro → 6_LoadingScene_0 → 3_PPE_Room_HandTest_scale_0`으로 명시했다.
  - 현재 작동 범위는 PPE 교육모드이며 훈련·테스트는 선택 UI와 이벤트까지만 존재하는 것으로 구분했다.
  - Trigger 입력 우선순위를 `UI 선택 → 태블릿 확인 → VOICE 건너뛰기`로 정리했다.
  - 태블릿을 B 페이지 방식이 아닌 Grip으로 잡은 뒤 Trigger 1회로 체크·서명·도장 시퀀스를 실행하는 방식으로 수정했다.
  - PPE는 마커 대상 직접 Grab, 공용 Action Panel 사용·폐기 판정, 동일 검사 오브젝트의 정상 상태 복원 방식으로 정리했다.
  - 물리적 몸 슬롯 설명을 제거하고 `PPE Body Anchor → PPE_A_SuitWear` 아래 풀장착 자식을 활성화하는 방식으로 수정했다.
  - 거울은 약 1.2m 접근, 시선 조건, 5초 확인, Mirror Only 레이어, 640×1472 RenderTexture 기준을 반영했다.
  - PPE 교육 퀴즈는 5페이지·3지선다, 오답 재시도, 정답 자동 다음 문제 방식이며 현재 점수 결과 화면은 없는 것으로 수정했다.
  - 교육 완료는 완료 VOICE, 손·Ray 숨김, 페이드, PPE 카드 위치 복귀와 상태 복원 순서로 정리했다.
- 신규 R&D로 `R-51~R-57`을 추가했다.
  - 비동기 로딩·XR 장면 전환 안정성
  - PPE VOICE 상태 전이·조건부 안내
  - 마커 전용 PPE Grab·씬 작성 포즈 복원
  - 공용 Action Panel 소유권
  - 실물 HMD·Game View 입력 경로 분리
  - 씬 작성 Transform·월드 UI 보존
  - 한글 키보드·이름 제출
- 기존 R&D `R-01`, `R-03`, `R-04`, `R-06`, `R-14`, `R-15`, `R-20`, `R-38`, `R-39`, `R-40`, `R-50`의 낡은 구현 가정을 현재 구조로 교체했다.
- 문서 전역의 명백한 불일치도 함께 수정했다.
  - Unity 6.5 전환 문구를 제거하고 Unity 6000.4.8f1 기준으로 통일했다.
  - `TTS: 미정`을 ElevenLabs 박현미 모델, Starter 월 6달러 정책으로 수정했다.
  - PPE의 A·B·몸 슬롯 조작 표현을 현재 Grip·Ray·Trigger·풀장착 자식 전환 방식으로 수정했다.
  - 프로토타입 범위를 PPE 착용 교육모드 한 사이클로 한정했다.
  - 후속 개발계획을 PPE 훈련모드, PPE 테스트모드, 관리자 기능 구현 순서로 수정했다.
  - 밀폐공간 진입 전 교육과 사고 체험은 PPE 프로토타입과 분리된 후속 시나리오로 정리했다.
- 문서 버전은 `v0.9`, 개정일은 `2026-08-14`로 유지하고 개정 내용에 기능명세·R&D 현행화를 추가했다.

### 17.2 수정이 필요했던 원인

- 상세기획서 일부 내용이 현재 프로젝트보다 이전 설계를 기준으로 작성되어 있었다.
- 실제 프로젝트에는 이름 입력, 비동기 로딩, 상태 기반 VOICE, 공용 Action Panel, 마커 전용 Grab 등이 구현됐지만 기능명세와 R&D에는 독립 항목이 없었다.
- 반대로 문서에는 Unity 6.5 전환, 태블릿 B 페이지 이동, 물리적 몸 슬롯, 새 PPE 인스턴스 재생성, 4지선다·점수형 교육 퀴즈와 전체 초기화 같은 현재 구현과 다른 설명이 남아 있었다.
- 프로토타입과 후속 본 개발의 범위가 혼재되어 PPE 교육모드, PPE 훈련·테스트모드, 관리자 기능, 밀폐공간 시나리오의 개발 순서를 구분할 필요가 있었다.

### 17.3 영향 범위

- 상세기획서 기능명세는 `FS-00~FS-22` 총 23개 항목으로 정리됐다.
- 상세기획서 R&D는 기존 항목에 `R-51~R-57`을 추가해 총 55개 항목으로 정리됐다.
- 기능명세와 R&D 연결용 JavaScript의 제목, 검토 프롬프트, 연결 ID, 분류 그룹도 함께 갱신했다.
- 교육 시나리오의 퀴즈·완료 설명, 프로젝트 개요의 Unity·TTS 정보, MVP 범위, 개발 일정과 QA 완료 기준이 함께 영향을 받았다.
- Unity 코드, 씬, Prefab, AudioClip과 Inspector 작성값은 이번 문서 작업에서 변경하지 않았다.

### 17.4 완료한 검증

- 기능명세 ID가 `FS-00~FS-22`까지 중복 없이 23개 존재하는지 확인했다.
- R&D ID가 중복 없이 55개 존재하고 신규 `R-51~R-57`이 모두 포함됐는지 확인했다.
- 기능명세와 R&D 표의 셀 구조, 주요 HTML 태그 수, 기능명세-R&D 연결 ID를 정적으로 검사했다.
- 내장 JavaScript 1개 블록을 Node.js로 구문 검사해 오류가 없음을 확인했다.
- `Unity 6.5`, `6000.5.5f1`, `TTS: 미정`, `몸 슬롯 접근`, PPE의 `Trigger, A, B`, 기존 점수형 교육 퀴즈 문구가 남아 있지 않은지 검색했다.
- v0.9 개정일, Unity 6000.4.8f1, OpenXR 1.17.1, ElevenLabs 박현미 TTS, PPE 훈련·테스트와 관리자 후속 일정이 문서에 반영됐는지 확인했다.

### 17.5 아직 필요한 수동 검증

- 브라우저에서 상세기획서를 열어 기능명세와 R&D 표의 가로폭, 체크박스 열, R&D 연결 배지, 모바일 반응형 레이아웃을 시각적으로 확인한다.
- Google Sheet 체크리스트 동기화가 신규 `FS-19~FS-22`, `R-51~R-57` ID를 정상 저장·불러오는지 실제 연결 환경에서 확인한다.
- 문서에 적은 거울, Quest 입력, 양안 렌더링, XR 오디오 완료 조건은 문서 정적 검증과 별개로 Unity Play Mode와 Quest/OpenXR 실기기에서 확인한다.
- `Docs/화학물질_안전훈련_VR_사전기획서_V0.9.html`은 현재 Git 기준 미추적 파일이므로 저장소에 포함할 경우 명시적으로 추가해야 한다.

## 18. 세로형 프로토타입 실무 기획서 PPTX 작성

### 적용한 변경

- 기존 `Docs/화학물질_안전훈련_VR_상세기획서_개발전계획_v0.7_세로형_14pt.pptx`는 수정하지 않고 별도 파일을 작성했다.
- 신규 파일은 `Docs/PPT/화학물질_안전훈련_VR_PPE착용교육_프로토타입기획서_v0.7_실무형.pptx`다.
- 원본과 동일한 A4 세로형 페이지 크기와 `v0.7` 표기를 유지했다.
- 발표용 슬로건과 메시지 중심 구성을 줄이고 요구사항, 상태 흐름, 입력 우선순위, 완료 조건, 예외 처리, 데이터 소유권, QA와 인수 기준 중심의 26페이지 실무 기획서로 재구성했다.
- 실제 장면 경로, PPE 교육모드 범위, 컨트롤러 조작, 박현미 TTS, 태블릿 단일 확인, 마커 Grab, 공용 Action Panel, 풀장착 모델, 거울 5초 확인, 3지선다 퀴즈와 완료 복귀를 현재 프로젝트 기준으로 반영했다.
- 후속 일정은 PPE 훈련모드, 테스트모드, 관리자 기능 순으로 구분하고 밀폐공간 교육은 별도 후속 시나리오로 분리했다.

### 영향 범위

- 신규 PPTX와 재생성용 `Tools/create_prototype_planning_ppt.py`만 추가했다.
- Unity 코드, 씬, Prefab, 원본 PPTX는 변경하지 않았다.

### 완료한 검증

- 원본과 신규 PPTX가 모두 동일한 세로 페이지 크기로 정상 재개방되는지 확인했다.
- 신규 PPTX가 26페이지이고 각 페이지에 `v0.7`이 유지되는지 확인했다.
- 슬라이드 객체의 페이지 경계 이탈, PPTX ZIP 패키지 손상, 임시 렌더러 워터마크 포함 여부를 검사했다.
- 26페이지를 PNG로 렌더링해 전체 연락시트와 표·텍스트 밀도가 높은 페이지를 시각 검수했으며 잘린 텍스트는 확인되지 않았다.

### 아직 필요한 수동 검증

- 실제 PowerPoint 또는 제출 환경에서 글꼴 대체, 표 행 높이, 인쇄 여백과 PDF 변환 결과를 확인한다.
- 내용 검토 뒤 빈 이름 정책, 거울 조건 이탈, 교육 완료 Reset, 훈련·테스트 결과와 관리자 조회 항목을 확정한다.

## 19. 2026-08-16 키보드 A 버튼 컨트롤러 상세교육 진입 논의

### 19.1 현재 확인된 상태

- 대상 흐름은 `Assets/Scenes/3_PPE_Room_Train_Test.unity`의 이름 입력용 `Modal  Keyboard Canvas`와 `Keyboard Canvas Root`다.
- 현재 `Controller Education Entry (Visual Only)`는 이름 그대로 시각 전용이다. 복제된 `XRKeyboardKey`, Poke/Follow 계열 컴포넌트와 Collider가 비활성이고, 루트와 자식 `Graphic.raycastTarget`도 꺼져 있어 A 표시 위로 XR Ray가 통과한다.
- 키보드 Canvas에는 `TrackedDeviceGraphicRaycaster`가 있고 양손 Ray Visual의 `Stop Line At First Raycast Hit`도 켜져 있다. 따라서 A 표시와 키보드의 비상호작용 구역에 UI Raycast 대상이 없다는 점이 현재 레이 통과의 직접 조건이다.
- 기존 긴 `Controller Edu` 음원 그룹과 `ControllerGuide` 시각물은 보존돼 있다. 이름 제출 뒤에는 별도 `Controller Simp` 그룹을 사용하고 마지막에 `CardIntro`로 전이한다.

### 19.2 확정한 입력과 상태 흐름

- 컨트롤러 상세교육은 다음 두 입력을 모두 지원한다.
  - 오른손 메타 퀘스트 컨트롤러의 물리 A 버튼
  - 키보드 Canvas에 표시된 UI A 버튼을 XR Ray로 가리킨 뒤 Trigger 선택
- 두 입력은 별도 진행 로직을 만들지 않고 하나의 `상세 컨트롤러 가이드 시작` 처리로 합친다.
- UI A는 문자 `a`를 입력하는 `XRKeyboardKey`로 사용하지 않는다. 일반 UI Button으로 기능화하여 Ray Trigger 선택이 이름 입력에 문자를 추가하지 않게 한다.
- 진입은 `NameInput` 상태에서만 허용하고, 같은 프레임의 물리 A·UI Trigger 동시 입력과 상세교육 재생 중 재진입은 한 번의 공유 가드로 차단한다.
- 확정 흐름은 `NameInput 키보드 → 물리 A 또는 UI A → 기존 ControllerGuide 시각물 + Controller Edu 상세 음원 → 상세 음원 종료 → NameInput 키보드 복귀`다.
- 상세교육 중에는 키보드 표시만 숨기고 입력 중인 이름은 제출·초기화하지 않는다. 현재 `HangulKeyboardController.OnDisable()`은 한글 조합을 확정할 뿐 XRI Keyboard의 표시 텍스트를 지우지 않으므로, 복귀 시 기존 입력 문자열을 유지하는 방향으로 구현한다.
- 상세교육 마지막 단계는 기존 기본값인 `CardIntro`로 전이하면 안 된다. A 진입 경로에서만 반환 목적지를 `NameInput`으로 유지해야 한다.
- Enter 제출의 기존 `NameInput → Controller Simp → CardIntro` 흐름은 변경하지 않는다.
- 오른손 `PrimaryButton`은 Starter Assets의 `Jump`에도 바인딩돼 있지만 이 씬은 `XRStartPoseAligner`가 `Jump` 오브젝트를 시작 시 비활성화한다. 그래도 새 A 입력 소비자는 `NameInput` 상태에서만 반응하도록 제한해 다른 단계의 입력 소비 가능성을 열지 않는다.

### 19.3 레이 차단 기준

- A 버튼의 실제 Button Graphic에는 `raycastTarget`을 켜고, 키보드에서 레이가 뒤로 넘어가면 안 되는 비상호작용 구역에는 씬 작성 UI Ray Blocker를 배치한다.
- Ray Blocker는 클릭 동작이 없는 `Graphic`으로 두되 Raycast 대상은 활성화한다. 키와 A Button보다 뒤의 형제 순서를 사용하여 버튼 입력은 유지하고 빈 영역에서만 첫 UI Hit를 제공한다.
- Blocker의 RectTransform, 크기, 앵커와 투명도는 씬/Inspector 작성값을 기준으로 하며 런타임에서 생성하거나 키보드 전체 레이아웃을 덮어쓰지 않는다.
- 실제 수정 전 `Controller Education Entry (Visual Only)`와 통과 구역을 각각 선택해 `Tools > PPE > Diagnose Selected Object Spatial Context`로 Canvas, 월드 코너, Raycaster, Graphic과 계층 활성 상태를 기록한다.

### 19.4 상세교육 음원 원고 방향

- 원고의 대상에는 메타 퀘스트 컨트롤러가 익숙하지 않은 사용자와 고령 사용자가 포함된다.
- 설명은 일반 XR 용어로 축약하지 않고 `버튼 위치 → 누르는 손가락 → 이 씬의 실제 사용 예 → 사용 동작` 순서를 유지한다.
- 버튼 위치는 사용자가 컨트롤러를 손에 든 방향을 기준으로 `컨트롤러 바깥쪽 위` 또는 필요한 경우 `뒤쪽`이라고 설명한다. 화면의 컨트롤러 강조 이미지와 함께 제공한다.
- 트리거 사용 예인 `버튼 선택`, `마스크 확인`, `키보드 글자 입력`은 삭제하지 않는다. 레이는 처음 접하는 사용자를 위해 `얇고 푸른 빛의 선`으로 함께 설명한다.
- 실제 임포트 경로는 `Assets/Audio/Voice/1_2_ContDetail/`이며 다음 다섯 MP3 구간으로 구성한다.
  - `VO_PPE_CTRL_DETAIL_001_Start.mp3`: 상세 컨트롤러 교육 시작과 진행 안내
  - `VO_PPE_CTRL_DETAIL_002_RayTrigger.mp3`: 트리거 위치, Ray와 씬 사용 예
  - `VO_PPE_CTRL_DETAIL_003_GripGrab_Release.mp3`: Grip Toggle 방식의 물체 잡기·놓기
  - `VO_PPE_CTRL_DETAIL_004_Joystick.mp3`: 조이스틱과 위치 마커 텔레포트
  - `VO_PPE_CTRL_DETAIL_005_GuideFollow.mp3`: 상황별 안내와 조이스틱 클릭 미니 가이드
- 001·002의 현재 내용 기준은 다음과 같다.

> 컨트롤러 교육에 들어오셨습니다.
>
> 메타퀘스트 컨트롤러가 익숙하지 않은 사용자를 위해 교육 진행에 필요한 기본 조작 방법을 안내해 드리겠습니다.
>
> 안내 음성과 화면 가이드를 따라 천천히 진행해 주세요.

> 정면에 보이는 컨트롤러 이미지를 확인해 주세요.
>
> 먼저 무언가를 선택할 때 사용하는 트리거 버튼과 레이에 대해 설명하겠습니다.
>
> 컨트롤러 바깥쪽 위에 돌출된 버튼이 트리거 버튼입니다. 검지로 눌러 사용합니다.
>
> 이 교육에서는 버튼을 선택할 때, 마스크를 확인할 때, 키보드로 글자를 입력할 때 사용합니다.
>
> 먼 곳의 화면이나 물체를 선택하려면 컨트롤러로 대상을 가리켜 조준해 주세요.
>
> 이때 컨트롤러에서 레이라는 얇고 푸른 빛의 선이 나타납니다.
>
> 레이가 원하는 대상에 닿은 것을 확인한 뒤 트리거 버튼을 누르면 선택할 수 있습니다.

- TTS의 문장 분리, 발음, 호흡과 간격은 사용자가 음원 생성 단계에서 직접 조절한다. 문서의 줄바꿈이나 TTS용 재작성안을 런타임 요구사항으로 고정하지 않는다.

### 19.5 구현 및 검증 상태

- `PPEVoiceFlowDirector`에 오른손 `primaryButton`/Oculus `buttonSouth` 입력과 UI A가 함께 호출하는 `NotifyControllerEducationRequested()`를 추가했다. 진입은 `NameInput`으로 제한하고 같은 프레임 중복과 재생 중 재진입을 차단한다.
- 상세교육의 자연 종료, Trigger 음성 Skip, 누락 단계 건너뛰기는 모두 같은 반환 판정을 거쳐 `NameInput`으로 복귀한다. 상세교육을 아직 완료하지 않은 세션의 이름 제출은 `Controller Simp → CardIntro`를 유지하고, 상세교육을 완료한 세션의 이름 제출은 중복 간단교육을 건너뛰어 `CardIntro`로 바로 전이한다. 키보드의 물리 A와 UI A를 이용한 상세교육 재진입은 완료 뒤에도 유지한다.
- UI A 전용 `PPEControllerEducationEntry`를 추가했다. `TrackedDeviceEventData`만 받아 일반 마우스 입력을 XR Trigger 검증으로 오인하지 않으며 문자 `a` 입력 기능은 사용하지 않는다.
- `Tools > PPE > Configure Controller Education Entry` Editor 메뉴를 추가했다. 이 메뉴는 대상 씬을 `Assets/Scenes/3_PPE_Room_Train_Test.unity`로 제한하고, 변경 전에 A와 키보드 Canvas의 공간 진단을 실행한다. 이후 기존 A의 RectTransform·색·전환값을 보존한 일반 Button 변환, 다섯 MP3 연결, 작성형 `Keyboard Ray Blocker` 생성을 수행한다.
- `Tools > PPE > Validate Train Test Modes`에는 다섯 상세 음원, 상세교육 이수 전·후의 이름 제출 분기, 표준 A Button과 Director 참조, Raycast Graphic, 비활성 Collider, Ray Blocker의 부모·형제 순서·Stretch, 물리 A 바인딩과 키보드 복귀 경로 검사를 추가했다. Preview Scene에서 실제 표시 함수를 호출해 상세 Trigger의 상단·하단 이미지와 컨트롤러 이미지, Grip·Joystick의 양쪽 이미지, 상세교육 뒤 간단교육 재진입 시의 표시 상태도 검사한다.
- Unity에서 설정 메뉴 실행과 씬 저장을 완료했다. `Controller Education Entry`의 표준 Button 변환, Director 참조, A Graphic Raycast, `Keyboard Ray Blocker`, 다섯 MP3 GUID가 디스크 씬에 직렬화된 것을 확인했다.
- Runtime/Editor 보조 빌드는 오류 0개로 완료됐다. Unity의 `Tools > PPE > Validate Train Test Modes`도 컨트롤러 상세교육 검사를 포함해 PASS했다.
- 최초 Play Mode 확인에서 상세교육의 기존 직렬화 참조가 왼쪽 컨트롤러 강조 이미지만 가리켜 오른쪽 안내 이미지가 표시되지 않았다. 상세교육의 MP3는 유지하고 각 단계의 시각 참조를 기본 `Controller Simp`와 동일한 좌우 쌍으로 변경했다. 디스크 씬에서 `1_Ray + 1_Ctrl_Trigger`, `2_Marker + 2_Ctrl_Grip`, `3_Ray_T + 3_Ctrl_Joystick` 참조와 재검증 PASS를 확인했다.
- 상세 Trigger에서 오른쪽 하단 이미지가 보이지 않고 이후 간단교육 이미지도 사라진 근본 원인은 `ApplyPresentation()`의 기존 상태별 자식 활성화가 작성된 `1_Ray` 그룹의 `Panel` 자식을 다시 끄고, 그 런타임 상태를 다음 교육까지 남긴 것이었다. 상세 A 진입과 간단교육은 작성된 단계별 시각 그룹을 단일 기준으로 사용하게 하여 기존 자식 활성화가 덮어쓰지 않도록 수정했다. 씬·Inspector의 레이아웃과 이미지 참조는 변경하지 않았다.
- 설정 메뉴를 Play Mode에서 잘못 실행했을 때 XRI `CurveVisualController` 배열 예외와 UI Raycaster 캐시 예외가 반복됐다. 디스크 씬에는 추가 변경이 없었고 Play Mode 종료로 멈췄다. 설정 메뉴와 검증 메뉴 모두 `EditorApplication.isPlayingOrWillChangePlaymode`일 때 한 번의 명확한 오류로 중단하도록 보강했다.
- 상세교육 완료 플래그, 이름 제출 분기, 작성 시각 그룹 보존 및 Preview Scene 동작 검증 하네스까지 반영한 뒤 Runtime/Editor 보조 빌드 오류 0개를 확인했다. Unity Editor에서 갱신된 `Tools > PPE > Validate Train Test Modes`를 실행해 `detailed-completion skip`과 `detailed/Simple paired guide presentation`을 포함한 PASS를 확인했다. 실제 Play Mode·Quest/OpenXR 동작은 아직 수동 검증이 필요하다.
- 구현 후 Unity Play Mode에서 UI A의 Hover·PointerDown·PointerUp·PointerClick과 물리 A 입력을 각각 구분해 확인한다.
- A 위와 Ray Blocker 구역에서 레이가 첫 UI Hit에 멈추고, 뒤쪽 UI나 월드 오브젝트가 선택되지 않는지 확인한다.
- 물리 A와 UI A를 같은 프레임에 입력해도 상세교육이 한 번만 시작되는지 확인한다.
- 상세교육 재생 중 A 재입력이 무시되고, 마지막 음원 또는 음성 Skip 뒤 키보드와 입력 중인 이름이 복원되는지 확인한다.
- 상세교육을 보지 않은 새 세션에서는 Enter 뒤 `Controller Simp → CardIntro`가 한 번만 진행되고, 상세교육을 완료하고 키보드로 복귀한 뒤에는 Enter가 간단교육을 건너뛰고 `CardIntro`로 바로 전이하는지 확인한다.
- Quest/OpenXR에서 물리 A, UI Ray Trigger, 양안 가이드, 레이 정지, 자막과 음성 재생을 별도로 검증한다.

### 19.6 후속 작업: Quest UI 시머링·앨리어싱 이동

- 컨트롤러 교육 이미지 누락과 별개로, Quest에서 월드 스페이스 UI의 가는 선과 외곽선이 긴
  계단 형태로 보이고 머리 이동 시 좌우로 기어가듯 움직여 UI가 울렁거리는 증상이 보고됐다.
- 현재 Quest Link가 `XR_ERROR_SESSION_LOST` 뒤 연결·해제를 반복하며 Meta 상태에
  `remoteRenderingTerminatedFlakyCable`이 기록돼, 오늘은 안정적인 렌더링 기준 실행을
  확보하지 못했다.
- 다음 세션에는 Link 안정화를 먼저 확인한 뒤 현재 URP/Render Scale/MSAA/Dynamic
  Resolution/FFR 값을 기록하고, 동일 UI와 Pose에서 Game View·Quest 양안을 비교한다.
- `MSAA → Render Scale → Dynamic Resolution/FFR → Texture Filter/Mip → Canvas 스케일·선 두께
  → z-fighting → XR Single Pass 셰이더` 순서로 한 항목씩만 비교한다.
- 원인 확정 전에는 렌더링 설정이나 씬 작성 UI 값을 일괄 변경하지 않는다.
