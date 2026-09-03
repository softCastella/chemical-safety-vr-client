# PPE Room 보이스 나레이션 흐름 설계

- 작성일: 2026-08-06
- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 상태: 시스템 구현 및 컨트롤러 가이드 클립별 매핑 완료, Quest 검증 대기
- 적용 범위: 보이스 나레이션, 안내 UI, 키보드 입력, 시나리오 카드·모달, PPE 교육, 텔레포트 안내

## 1. 문서 목적

`3_PPE_Room_HandTest_scale_0` 씬에 진입한 뒤 보이스 나레이션과 XR UI가 정해진 순서로 진행되도록 하는 구조를 정의한다.

이 흐름은 단순히 AudioClip을 순서대로 재생하는 기능이 아니다. 다음 요소가 함께 상태를 변경한다.

- 보이스 재생 완료
- 키보드의 Enter 제출
- 컨트롤러 안내 화면의 단계 변경
- 시나리오 카드 선택
- 상세 모달과 교육 선택
- PPE 착용 교육 버튼 선택
- 텔레포트 시작 및 도착
- 일정 시간 입력이 없을 때의 반복 안내

따라서 전체 진행 상태의 소유자를 하나로 두고, 기존 UI와 XR 입력 시스템은 이벤트를 제공하는 소비자로 연결한다.

## 2. 현재 확인 결과

### 2.1 빌드 씬

`ProjectSettings/EditorBuildSettings.asset`의 빌드 씬은 다음과 같다.

```text
Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity
```

이번 설계의 대상은 이 씬 하나로 한정한다. 다른 PPE Room variant 씬은 수정 대상에 포함하지 않는다.

### 2.2 현재 씬 UI 상태

씬에 다음 오브젝트가 이미 존재한다.

| 오브젝트 | 현재 상태 | 비고 |
|---|---:|---|
| `Window Canvas` | 활성 | `TrackedDeviceGraphicRaycaster`를 사용하는 XR Canvas |
| `ControllerGuide` | 활성 | 컨트롤러 안내 부모 |
| `1_Ray` | 비활성 | 첫 번째 컨트롤러 안내 |
| `2_Marker` | 비활성 | Marker 안내 |
| `3_Ray_T` | 비활성 | Ray T 안내 |
| `4_Panel` | 활성 | 요구한 시작 상태와 불일치할 수 있음 |
| `Scenario Card Canvas` | 비활성 | 시나리오 카드와 `ScenarioDetailModal` 포함 |
| `Modal Canvas` | 활성 | 모달 루트만 별도로 표시하는 구조 |
| `Modal  Keyboard Canvas` | 비활성 | `HangulKeyboardController`의 제출 대상 |
| `Keyboard Canvas Root` | 활성 | `UI Root` 아래의 실제 키보드 화면 후보 |

`4_Panel`이 현재 활성화되어 있으므로, 보이스 흐름 구현 전에 Inspector에서 초기 상태를 요구사항과 일치시켜야 한다. 런타임 코드가 시작 시 무조건 상태를 덮어쓰는 방식으로 보정하지 않는다.

또한 `Modal  Keyboard Canvas`와 `Keyboard Canvas Root`가 분리되어 있으므로, 실제로 렌더링되고 XR 입력을 받는 키보드 루트를 Unity Editor에서 확정해야 한다.

### 2.3 기존 코드와의 연결 지점

- `AudioManager`는 현재 BGM, ambience, SFX 채널을 제공한다.
- `HangulKeyboardController`는 XR 키보드의 `onTextSubmitted`를 내부에서 처리한다.
- `HangulKeyboardController`는 현재 제출 시 `Modal  Keyboard Canvas`를 자동 비활성화하도록 설정되어 있다.
- `ScenarioCardSelectProxy.Trigger()`는 카드 선택의 공통 진입점이다.
- `ScenarioDetailModal.Show()`는 상세 모달을 표시한다.
- `ScenarioDetailModal`은 교육 선택과 PPE 교육 선택용 `UnityEvent`를 제공한다.
- `PPEControllerTeleportModeManager`는 시나리오 선택 후 텔레포트 사용 가능 여부를 관리한다.

현재 워크스페이스에서는 새로 지정된 `.ogg`, `.mp3`, `.wav` 보이스 파일을 확인하지 못했다. 기존 `AudioManagerSettings`에는 일부 AudioClip GUID 참조가 있으므로, 새 파일 추가 후 참조 유효성을 별도로 확인해야 한다.

## 3. 권장 아키텍처

### 3.1 `PPEVoiceFlowDirector`

대상 씬의 `UI Root` 또는 별도 `PPE Voice Flow` 오브젝트에 하나만 배치한다.

이 컴포넌트가 담당할 내용은 다음과 같다.

- 현재 진행 상태
- 다음 상태 전환
- 상태 진입·종료 시의 UI 표시 요청
- 음성 재생 완료 대기
- 키보드·카드·모달·텔레포트 이벤트 구독
- 반복 안내 타이머 취소와 재시작
- 씬 재진입 시 상태 초기화

모든 UI의 위치, 크기, 색상, 폰트, 레이아웃 값은 이 컴포넌트에서 설정하지 않는다. 이 컴포넌트는 Inspector에 연결된 오브젝트의 활성 상태와 상호작용 상태만 변경한다.

### 3.2 음성 데이터

음성 데이터는 다음 두 계층으로 나누는 것을 권장한다.

1. `PPEVoiceFlowAsset`
   - 단계 ID
   - AudioClip
   - 음량
   - 자동 진행 여부
   - 반복 AudioClip
   - 반복 간격

2. 씬의 `PPEVoiceFlowDirector`
   - Window 표시 루트
   - Keyboard 표시 루트
   - ControllerGuide 하위 단계 오브젝트
   - Scenario Card 표시 루트
   - 모달과 버튼
   - 키보드·텔레포트 이벤트 소스

AudioClip을 파일명 문자열로 검색하지 않고 Inspector의 직접 참조로 연결한다. 파일명 변경이나 `Resources.Load` 경로 변경으로 흐름 전체가 끊기는 문제를 줄일 수 있다.

### 3.3 음성 재생 채널

`AudioManager`에 전용 Voice 채널을 추가하거나, 씬에 `PPEVoicePlayer`를 두고 전용 `AudioSource`를 연결한다.

전용 채널은 다음 기능을 제공해야 한다.

- 한 번에 하나의 나레이션만 재생
- 현재 클립 재생 완료 확인
- 새 상태 진입 시 이전 클립과 반복 타이머 취소
- BGM과 ambience 음량 조절 또는 ducking
- Quest와 PC OpenXR에서 공통으로 동작

기존 `PlaySfx()`는 재생 완료를 알려주지 않으므로, 전체 흐름의 대기용으로 직접 사용하지 않는다.

## 4. 상태 전이안

```text
Welcome
  -> NameInput
  -> ControllerRay
  -> ControllerMarker
  -> ControllerRayT
  -> ControllerPanel
  -> CardIntro
  -> ModalDetail
  -> EducationSelected
  -> PpeEducationSelected
  -> TeleportInstruction
  -> PpeArea
```

| 상태 | UI 동작 | 보이스 | 전환 조건 |
|---|---|---|---|
| `Welcome` | Window 표시, 안내 화면 표시 | `0_vo_ppe_intro_001_welcome.ogg` | 음성 종료 |
| `NameInput` | 키보드 표시 | `1_vo_ppe_data_001_name.ogg` | XR 키보드 Enter 제출 |
| `ControllerRay` | `1_Ray` 활성, `1_Ctrl_Trigger` 표시 | `2_VO_PPE_CTRL_001_Start.ogg`, `2_VO_PPE_CTRL_002_RayTrigger.ogg` | 지정 음성 종료 |
| `ControllerMarker` | `2_Marker` 활성, `2_Ctrl_Grip` → `3_Ctrl_Joystick` 표시 | `2_VO_PPE_CTRL_003_GripGrab_Release.ogg`, `2_VO_PPE_CTRL_004_Joystick_Marker.ogg` | 지정 음성 종료 |
| `ControllerRayT` | `3_Ray_T` 활성, `3_Ctrl_Joystick` 유지 | `2_VO_PPE_CTRL_005_Joystick_Ray_T.ogg` | 음성 종료 |
| `ControllerPanel` | `4_Panel` 활성, `4_Ctrl_B` → `4_Ctrl_A` 표시 | `2_VO_PPE_CTRL_006_B_Button.ogg`, `2_VO_PPE_CTRL_006_A_Button.ogg`, `2_VO_PPE_CTRL_007_GuideFollow.ogg` | 마지막 음성 종료 |
| `CardIntro` | 시나리오 카드 표시 | 카드 안내 음성 2개 | 두 음성 종료 |
| `ModalDetail` | 기존 `ScenarioDetailModal.Show()` 사용 | `3_vo_card_003_selectdetailedu.ogg` | 교육 선택 버튼 |
| `EducationSelected` | 기존 모달 교육 선택 상태 유지 | `4_vo_ppe_modal_001_eduselected.ogg` | PPE 착용 교육 버튼 |
| `PpeEducationSelected` | 카드 표시 루트 숨김 준비 | `5_vo_ppe_edu_001_confirm.ogg` | 음성 종료 |
| `TeleportInstruction` | 텔레포트 입력 허용 | `5_vo_ppe_edu_002_teleport.ogg` | 텔레포트 시작 또는 도착 |
| `TeleportInstruction` 반복 | 화면 상태 유지 | `5_vo_ppe_edu_002_teleport_re.ogg` | 6초 동안 입력 없음 |
| `PpeArea` | 도착 상태 표시 | `5_vo_ppe_edu_003_ppeArea.ogg` | 텔레포트 도착 이벤트 |

음성 파일명에는 현재 입력 내용 중 오타 또는 표기 불일치로 보이는 항목이 있다. 예를 들어 `3_v_ppe_...`, `jostick`, `GuideFollew`, `joystick)marker` 등은 파일을 추가하기 전에 최종 명칭을 확정하고 manifest에 기록한다.

## 5. Inspector 연결 원칙

`PPEVoiceFlowDirector`에는 다음 참조를 직접 연결한다.

```text
Audio
 └─ Voice Player / AudioSource

Presentation Roots
 ├─ Window Presentation Root
 ├─ Keyboard Presentation Root
 ├─ ControllerGuide
 ├─ Scenario Card Presentation Root
 └─ Modal Presentation Root

Controller Guide Steps
 ├─ 1_Ray
 ├─ 2_Marker
 ├─ 3_Ray_T
 └─ 4_Panel

Interaction Events
 ├─ XR Keyboard Submit Source
 ├─ ScenarioCardSelectProxy 또는 카드 선택 이벤트
 ├─ ScenarioDetailModal
 ├─ 교육 선택 이벤트
 ├─ PPE 착용 교육 선택 이벤트
 ├─ Teleport Start Source
 └─ Teleport Arrival Source
```

기존 씬 UI의 authored Transform, Canvas 설정, TMP 설정, 이미지와 레이아웃은 그대로 보존한다. 런타임에서는 `SetActive`, 입력 잠금, 보이스 재생 상태만 관리한다.

## 6. 반드시 해결해야 할 충돌

### 6.1 XR Canvas 전체 비활성화

`Window Canvas`, `Scenario Card Canvas`, `Modal Canvas`는 `TrackedDeviceGraphicRaycaster`를 사용하는 Canvas다. 기존 코드 주석에도 XR Canvas 자체를 끄면 `null eventCamera`와 관련된 문제가 발생할 수 있다고 기록되어 있다.

따라서 시각적으로 숨길 때는 우선 다음 하위 오브젝트를 끄는 방식을 사용한다.

- `ControllerGuide`
- 시나리오 카드 선택 루트
- 키보드 표시 루트
- 모달 루트

Canvas 루트 자체를 끄는 요구가 꼭 필요하다면 Unity Editor와 Quest/OpenXR에서 별도 검증한다.

### 6.2 키보드 제출 시 중복 제어

현재 `HangulKeyboardController`가 Enter 제출 시 `Modal  Keyboard Canvas`를 자동으로 끈다. 새 상태 관리자가 같은 Canvas를 다시 켜거나 끄면 상태가 충돌한다.

키보드 제출 이벤트와 UI 비활성화의 소유자를 다음 중 하나로 확정해야 한다.

- `PPEVoiceFlowDirector`가 제출 이벤트와 UI 상태를 모두 소유
- 기존 `HangulKeyboardController`가 키보드 비활성화를 소유하고 Director는 다음 상태만 시작

두 컴포넌트가 같은 Canvas를 동시에 직접 제어해서는 안 된다.

### 6.3 텔레포트 조기 활성화

현재 `ScenarioDetailModal.SelectIncompletePpeScenario()`는 PPE 교육 선택 즉시 `PPEControllerTeleportModeManager.NotifyScenarioReadyForMovement()`를 호출한다. 이 시점은 새 요구사항의 `4_vo...`와 `5_vo...` 음성보다 빠르다.

따라서 다음 중 하나가 필요하다.

- 텔레포트 활성화를 `PPEVoiceFlowDirector`의 `TeleportInstruction` 진입 시점으로 지연
- `PPEControllerTeleportModeManager`에 보이스 진행 Gate를 추가
- 교육 선택 이벤트는 받되 `NotifyScenarioReadyForMovement()` 호출은 최종 상태에서 실행

## 7. 입력 이벤트 연결 기준

호버 효과를 입력 성공으로 간주하지 않는다. 각 단계는 실제 이벤트를 사용한다.

```text
키보드: XRKeyboard.onTextSubmitted
카드: ScenarioCardSelectProxy.Trigger() 또는 실제 PointerClick/XR Select
모달: ScenarioDetailModal.Show() 이후 교육 버튼 UnityEvent
PPE 교육: OnIncompletePpeScenarioSelected
텔레포트: 실제 Teleport Start 및 Teleport Arrival 이벤트
```

마우스 fallback은 Quest Trigger 검증을 대신하지 않는다. Quest/OpenXR에서는 XR Interactor, UI Press 또는 Select, Raycaster, EventSystem, handler까지 별도로 확인한다.

## 8. 검증 계획

### 정적 확인

- 대상 씬이 `3_PPE_Room_HandTest_scale_0.unity`인지 확인
- 모든 AudioClip 직접 참조가 유효한지 확인
- 각 상태의 UI 참조가 누락되지 않았는지 확인
- `4_Panel` 초기 활성 상태를 요구사항과 일치시켰는지 확인
- 키보드 자동 비활성화와 Director의 제어가 중복되지 않는지 확인
- 텔레포트 조기 활성화 경로가 제거되었는지 확인
- 보이스 파일명 manifest와 실제 파일명이 일치하는지 확인

### Unity Editor 확인

- 씬 진입 시 `Welcome` 상태로 시작
- 음성 종료 시에만 자동 상태 전환
- Enter 제출 시 `NameInput`이 한 번만 종료
- 카드 선택, 모달 표시, 교육 버튼 이벤트가 각각 한 번만 전파
- 반복 텔레포트 음성이 6초 간격으로 중복 재생되지 않음
- 씬 재진입 시 이전 상태와 Coroutine이 남지 않음
- UI Transform과 Canvas/TMP authored 값이 Play Mode에서 변경되지 않음

### Quest/OpenXR 확인

- 양안에서 안내 UI가 정상 표시되는지 확인
- Quest 컨트롤러 Trigger, Grip, Joystick 입력으로 각 이벤트가 진행되는지 확인
- 음성 재생 중 XR 입력이 막히거나 중복 소비되지 않는지 확인
- 텔레포트 시작·도착 시 반복 안내가 중지되는지 확인
- BGM·ambience와 나레이션의 음량 균형 및 출력 장치를 확인

## 9. 변경 기록

### 적용한 변경

- `PPEVoiceFlowDirector.cs`를 추가해 상태 전이, 음성 순차 재생, 입력 대기, 6초 반복 안내, UI 표시 루트 제어를 구현했다.
- `PPEVoiceKeyboardEventRelay.cs`와 `PPEVoiceTeleportEventRelay.cs`를 추가해 키보드 제출과 실제 XRI 텔레포트 수명주기를 연결했다.
- `AudioManager`에 BGM/SFX와 분리된 Voice 채널을 추가했다.
- `HangulKeyboardController`, `ScenarioDetailModal`, `PPEControllerTeleportModeManager`에 진행기용 이벤트와 텔레포트 Gate를 추가했다.
- `Tools > PPE > Voice Flow > Setup HandTest Scale 0` 및 `Validate HandTest Scale 0` Editor 메뉴를 추가했다.
- 실제 보이스 파일, AudioClip 참조, 대상 씬의 새 컴포넌트와 참조 직렬화는 아직 적용하지 않았다.

### 근본 원인 및 설계 배경

- 요구사항이 음성 순차 재생, 사용자 입력 대기, UI 활성 상태, 텔레포트 조건을 동시에 포함한다.
- 기존 오디오 시스템은 SFX 재생 중심이며, 상태 전환을 위한 재생 완료·취소·반복 제어가 없다.
- 기존 키보드와 모달·텔레포트 코드가 일부 UI와 이동 상태를 직접 변경하므로 단일 상태 소유자 없이 보이스만 추가하면 순서 충돌이 발생한다.

### 영향 범위

- 대상 씬의 Window, Keyboard, Scenario Card, Modal 표시 상태
- XR 키보드 제출 이벤트
- ScenarioCardSelectProxy와 ScenarioDetailModal의 선택 이벤트
- PPEControllerTeleportModeManager의 이동 허용 시점
- AudioManager 또는 별도 Voice AudioSource

### 완료한 검증

- 빌드 설정에서 대상 씬 경로 확인
- 대상 씬의 관련 Canvas와 ControllerGuide 하위 오브젝트 확인
- 기존 키보드 제출, 카드·모달 선택, 텔레포트 상태 코드 확인
- 기존 문서 중 동일한 보이스 나레이션 설계 문서가 없는지 확인
- 새 런타임·Editor 스크립트의 중괄호 구조와 참조 경로 정적 확인
- `git diff --check` 확인

### 아직 필요한 수동 검증

- 실제 보이스 파일의 import와 AudioClip 참조 연결
- Unity에서 `Tools > PPE > Voice Flow > Setup HandTest Scale 0` 실행 후 씬 저장
- Unity에서 `Tools > PPE > Voice Flow > Validate HandTest Scale 0` 실행
- Unity Play Mode에서 음성 종료·입력 이벤트 순서 확인
- Quest/OpenXR 양안 UI와 컨트롤러 입력 확인
- 텔레포트 시작·도착 이벤트와 반복 나레이션 중지 확인

## 10. 2026-08-07 시연 진입 차단 복구

### 적용한 변경

- `PPEVoiceFlowDirector`를 비활성 `Modal  Keyboard Canvas` 아래의 `UI Root`에서 항상 활성인 `XR Origin (VR)`으로 이동했다.
- `PPE Body Anchor`를 활성화했다. 기존 장비 Transform, 렌더러, 부모 계층은 변경하지 않았다.
- `PPEVoiceFlowSetup`도 `XR Origin (VR)`을 Director 호스트로 사용하도록 수정했다.

### 근본 원인

- Director가 비활성 Canvas 하위에 있어 `activeInHierarchy=false`였고, `Start()`와 음성 상태 전이가 실행되지 않았다.
- `PPE Body Anchor`가 비활성이라 `PPEHazmatEquipController`와 `PPEEquipmentVisualController`가 런타임 장착 이벤트를 받을 수 없었다.

### 확인 결과

- 대상 씬의 Director FileID와 `XR Origin (VR)` 참조를 정적 확인했다.
- `PPE Body Anchor.m_IsActive: 1`을 확인했다.
- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 오류 0개를 확인했다.

### 아직 필요한 수동 검증

- Unity Editor에서 대상 씬을 디스크 기준으로 다시 로드한 뒤 Play Mode 진입
- Welcome 음성·이름 입력·카드·PPE 장착·텔레포트 순서 확인
- 남아 있는 PPE 시각 이상 항목의 실제 이름과 위치를 Quest/OpenXR 양안에서 확인

## 11. 2026-08-07 시작 상태·BGM·손 모델 정리

### 적용한 변경

- `Window Canvas`와 `Modal  Keyboard Canvas`를 시작 시 활성화했다. `Place`는 기존처럼 활성 상태로 두어 시작 위치 표기가 Canvas 계층 안에서 표시되도록 했다.
- 대상 씬의 BGM 볼륨을 `1`에서 `0.3`으로 낮췄다. 씬별 Audio 설정이 `AudioManager`의 실제 시작 볼륨을 결정한다.
- 기본 시작 손은 양손 `LeftHand_BareHand`·`RightHand_BareHand`만 보이도록 하고, 씬에 잘못 켜져 있던 `RightHand_Glove_Suit_Tape`를 비활성화했다. 장갑·테이프 장착 시의 좌우 전환 매핑은 변경하지 않았다.

### 근본 원인

- Canvas 부모가 꺼져 있어 `Place`와 키보드 자식이 `activeSelf=true`여도 `activeInHierarchy=false`가 될 수 있었다.
- 오른손 테이프 손 모델 하나가 씬 기본값에서 활성화되어, Play 전에 한쪽만 장갑·방호복·테이프처럼 보였다.
- 대상 씬 BGM 설정이 `bgmVolume: 1`이었다.

### 정적 확인

- `Window Canvas`, `Modal  Keyboard Canvas`, `Place`의 시작 활성값을 확인했다.
- 양손 맨손 모델 활성값과 장갑·테이프 모델의 시작 활성값을 확인했다.
- 대상 씬 BGM 설정의 `bgmVolume: 0.3`을 확인했다.

### 아직 필요한 수동 검증

- Unity Editor에서 대상 씬을 디스크 기준으로 다시 로드한 뒤, Play 전에 Canvas·Place·양손 모델 상태를 Inspector에서 확인
- Play Mode에서 Place 표기, 이름 입력 시 키보드 표시, BGM 체감 음량 확인
- PPE 장착 절차에서 장갑·방호복·테이프가 해당 손만 전환되는지 Quest/OpenXR에서 확인

## 12. 2026-08-07 키보드 표시 시점과 씬 로드 오류 수정

### 적용한 변경

- `Modal  Keyboard Canvas` 부모는 시작부터 활성화된 상태로 유지하되, 키보드 표시 루트는 `NameInput` 상태에서만 활성화하도록 확정했다. 따라서 Welcome 음성이 끝난 뒤 이름 입력 단계에서 키보드가 나타난다.
- 키보드 클릭음용 `AudioSource`의 `m_GameObject` 참조를 `XR Origin (VR)`에서 실제 컴포넌트를 보유한 `UI Root`로 바로잡았다.
- 타이틀 음원은 `Assets/Audio/BGM/XR Horizon Interface (Remastered).mp3`를 사용한다. 이 리마스터 음원은 기존 `XR-Horizon-Interface.ogg`의 `.meta` GUID와 Import 설정을 승계해 `Resources/Audio/Scenes` 및 씬의 작성 참조를 유지한다. 기존 PPE BGM `Safe-Horizons-_VR-Training-Theme_.ogg`는 교체 음원 확정 전 제거된 상태이므로 타이틀 음원 교체와 별도 상태로 관리한다.
- `PPEVoiceFlowDirector`도 첫 렌더 프레임과 `1`초 대기 후 Welcome 나레이션을 시작하도록 조정했다. XR 화면이 표시된 뒤 음성이 시작되도록 하기 위한 설정이다.
- `PPEVoiceFlowDirectorEditor`를 추가해 Inspector의 각 `Voice Step` 옆에서 `Play`와 `Stop`으로 연결된 음성을 편집 모드에서 미리 들을 수 있도록 했다. 한 Step에 여러 클립이 있으면 배열 순서대로 재생한다.
- Unity 6000의 미리듣기 API가 `UnityEditor.AudioUtil`에 있는 것을 반영해 Editor 미리듣기 호출 경로를 수정했다.

### 근본 원인

- 키보드 Canvas 부모를 비활성화하면 NameInput에서 자식 표시 루트를 켜도 `activeInHierarchy=false`가 된다. 부모는 살리고 표시 루트만 상태에 따라 제어해야 한다.
- AudioSource YAML의 소유 GameObject와 `UI Root`의 컴포넌트 목록이 서로 달라 Unity가 씬을 열 때 중복 컴포넌트를 제거·수정했다.

### 아직 필요한 수동 검증

- 씬을 저장하지 않고 닫은 뒤 다시 열어 Scene import 오류가 사라지는지 확인
- Welcome 중에는 키보드가 숨겨지고, 음성 종료 후 NameInput에서 키보드가 보이며 클릭음이 정상 재생되는지 확인

## 13. 2026-08-07 AudioManager·Voice Flow Inspector 미리듣기 무음

### 증상

- `AudioManager`의 BGM·Voice·SFX·Ambience 각 항목 `Play`와 `PPEVoiceFlowDirector`의 Voice Step `Play`가 편집 모드에서 무음이었다.
- 이 문제는 런타임 `AudioSource` 재생 여부와 별개인 Editor 미리듣기 경로의 문제였다.

### 근본 원인

- 두 Inspector가 `UnityEditor.AudioUtil.PlayPreviewClip`을 정적 초기화 시점에 한 번만 Reflection으로 조회해 캐시했다.
- Unity 6000에서 해당 내부 Editor API가 그 시점에 아직 로드되지 않으면 캐시가 `null`로 고정되고, 이후 `Play` 버튼도 재조회 없이 무음으로 끝났다.

### 적용한 변경

- `Assets/Editor/AudioManagerEditor.cs`와 `Assets/Editor/PPEVoiceFlowDirectorEditor.cs`가 `Play` 또는 `Stop`을 누르는 순간 `AudioUtil` 메서드를 다시 조회하도록 변경했다.
- 이 변경은 Inspector 프리뷰 호출 경로만 다룬다. 씬의 AudioManager, AudioSource, BGM/Voice/SFX/Ambience 할당값, UI 및 XR 계층은 변경하지 않았다.

### 검증 결과와 범위

- `Assembly-CSharp-Editor.csproj` 빌드 오류 0개를 확인했다.
- 변경 후 사용자가 Inspector 오디오 출력이 다시 들리는 것을 확인했다.
- 별도로 `Editor.log`에는 HMD 미연결 OpenXR 초기화 과정의 `XR: Error setting active audio output driver. Falling back to default.`가 반복 기록됐다. 이는 Quest/OpenXR 출력 장치 전환 경고이며, 이번 Editor 미리듣기 무음의 직접 원인과 구분한다.

### 재발 방지

- Unity 내부·지연 로드 Editor API는 정적 초기화 캐시에만 의존하지 않는다. 실제 버튼 실행 시점에 조회·실패를 처리한다.
- Inspector 미리듣기 성공을 Play Mode 또는 Quest/OpenXR 런타임 오디오 성공 증거로 확대 해석하지 않는다. 런타임은 Source별 `isPlaying`, Clip, Volume, Mute, 출력 장치 및 실제 청취를 별도로 확인한다.

## 14. 2026-08-07 시나리오 카드 시작 테스트와 보이스 구간 검증

### 확인된 직렬화 문제

- 대상 씬 `3_PPE_Room_HandTest_scale_0.unity`의 `education_selected` 단계 첫 Clip이 앞 단계 `card_detail`과 동일한 `3_VO_PPE_CARD_003_SelectDetailEdu.ogg`로 중복 연결되어 있었다. 교육 선택 직후 상세 선택 안내가 한 번 더 재생되는 배정이다.
- `teleport_instruction`의 `repeatClip`은 비어 있었지만, 실제 음원 폴더에는 반복 안내용 `4_VO_PPE_EDU_002-2_Teleport_re.ogg`가 존재한다.

### 적용한 변경

- `PPEVoiceFlowDirector`에 Inspector용 `Start At Card Intro For Testing` 토글과 `StartCardIntroForTesting()` 진입점을 추가했다.
  - 켜면 Welcome·이름·컨트롤러 안내를 건너뛰고 일반 `CardIntro` 상태로 시작한다.
  - 카드 안내 음성, 카드 표시, 카드 선택 후 모달 전환, 이후 텔레포트 잠금은 일반 흐름과 동일한 상태 전이를 사용한다.
  - 기존 `Start At Modal Detail For Testing`도 켜져 있으면 카드 시작이 우선한다.
- `CardIntro`는 안내 음성이 끝나도 `ModalDetail`로 자동 전환하지 않고, 실제 `ScenarioCardSelectProxy` 선택으로 이미 열린 모달을 받은 뒤에만 상태를 전환한다. 이때 선택한 카드의 모달을 초기 시나리오로 다시 열어 덮어쓰지 않는다.
- 기존 `Tools > PPE > Voice Flow > Validate HandTest Scale 0`에 확인된 배정 규칙을 추가했다. 이 검증은 씬을 수정하지 않으며, 다음을 Console 오류로 표시한다.
  - `card_detail` → `3_VO_PPE_CARD_003_SelectDetailEdu.ogg` 한 개
  - `education_selected` → `3_VO_PPE_CARD_004_EduSelected.ogg` 한 개
  - `teleport_instruction` → `4_VO_PPE_EDU_002_Teleport.ogg` 한 개
  - `teleport_instruction.repeatClip` → `4_VO_PPE_EDU_002-2_Teleport_re.ogg`

### 남은 Inspector 작업

- `PPE Voice Flow`의 `education_selected` Clip 배열에서 첫 번째 중복 슬롯을 제거하고 `3_VO_PPE_CARD_004_EduSelected.ogg`만 남긴다.
- `teleport_instruction`의 `repeatClip`에 `4_VO_PPE_EDU_002-2_Teleport_re.ogg`를 연결한다.
- 이는 씬 작성값 수정이므로 자동으로 변경하거나 저장하지 않았다. 사용자가 Inspector에서 확인·저장한 뒤 검증 메뉴를 실행한다.

### 검증 범위

- 정적 대조: 현재 직렬화된 GUID와 실제 `Assets/Audio/Voice` 파일명을 대조했다.
- 아직 필요한 수동 검증: 위 두 Inspector 값 저장 후 Play Mode에서 카드 시작 토글을 켜 카드 안내부터 모달·교육 선택·텔레포트 반복 안내까지 청취한다. Quest/OpenXR 실제 입력·출력 검증은 별도로 필요하다.

## 15. 2026-08-07 컨트롤러 나레이션별 좌측 이미지 전환

### 근본 원인

- 기존 `PPEVoiceFlowDirector`는 상태가 바뀔 때 `1_Ray`, `2_Marker`, `3_Ray_T`, `4_Panel`만 전환했다.
- `controller_marker` 안의 Grip·Joystick 음성과 `controller_panel` 안의 B·A 음성은 같은 상태에서 연속 재생되므로 상태 단위 표시만으로는 음성 사이의 좌측 이미지를 바꿀 수 없었다.
- 기존 통합 `2_VO_PPE_CTRL_006_BA_Button.ogg`를 B와 A 파일로 분리한 뒤에는 `controller_panel`의 Clip 슬롯과 재생 순서도 함께 확장해야 했다.

### 적용한 변경

- `VoiceStep.controllerGuideVisuals`를 추가해 각 Clip 슬롯에 씬 작성 GameObject를 직접 연결했다. 런타임은 Clip 재생 직전에 해당 오브젝트만 활성화하며 Transform, 크기, 색, Sprite는 변경하지 않는다.
- `ControllerGuide/Context`에 작성된 다섯 Image를 다음과 같이 연결했다.

| 재생 Clip | 표시 오브젝트 |
|---|---|
| `2_VO_PPE_CTRL_001_Start.ogg` | `1_Ctrl_Trigger` |
| `2_VO_PPE_CTRL_002_RayTrigger.ogg` | `1_Ctrl_Trigger` |
| `2_VO_PPE_CTRL_003_GripGrab_Release.ogg` | `2_Ctrl_Grip` |
| `2_VO_PPE_CTRL_004_Joystick_Marker.ogg` | `3_Ctrl_Joystick` |
| `2_VO_PPE_CTRL_005_Joystick_Ray_T.ogg` | `3_Ctrl_Joystick` |
| `2_VO_PPE_CTRL_006_B_Button.ogg` | `4_Ctrl_B` |
| `2_VO_PPE_CTRL_006_A_Button.ogg` | `4_Ctrl_A` |
| `2_VO_PPE_CTRL_007_GuideFollow.ogg` | `4_Ctrl_A` 유지 |

- `controller_panel` 재생 순서를 `B → A → GuideFollow`로 변경했다.
- 초기 씬 상태는 `1_Ctrl_Trigger`만 활성화하고 나머지 네 이미지는 비활성화했다.
- 초반 컨트롤러 가이드를 건너뛰는 `Start At Card Intro For Testing`과 `Start At Modal Detail For Testing`을 해제해 정상 `Welcome` 시작 흐름을 복구했다.
- `Tools > PPE > Voice Flow > Configure Controller Guide Narration`은 이 참조를 명시적으로 설정하며, 기존 검증 메뉴는 Clip·시각물 배열과 초기 활성 상태를 함께 검사한다.

### 검증 결과와 남은 확인

- 런타임·Editor 어셈블리 빌드 오류 0개.
- Unity 직렬화 감사에서 컨트롤러 단계별 슬롯 수 `2/2/1/3`, B→A→GuideFollow 순서, 다섯 시각물 참조, Trigger 단독 초기 상태를 확인했다.
- Play Mode에서 `ControllerPanel`을 시작했을 때 `4_Ctrl_B`와 `2_VO_PPE_CTRL_006_B_Button.ogg`가 동시에 활성·재생되는 것을 확인했다.
- B 음성 종료 후 A 이미지·음성으로 넘어가는 전체 시간 경과 청취와 Quest/OpenXR 양안 표시는 수동 확인 대상으로 남긴다.

## 16. 2026-08-07 이동 시 음성 취소 및 다음 상태 음성 분리

### 적용 범위와 보존 규칙

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나다.
- 이번 변경이 대응하는 요청은 **위치마커로 이동하면 현재 텔레포트 지시 음성이 즉시 끝나고, 사용자가 빠르게 이동해도 이전 영역의 음성이 다음 영역에 남지 않는 것**이다.
- 기존 시나리오의 각 단계별 음성 순서와 Inspector에 작성된 Clip·반복 Clip·UI 배치는 보존한다. 런타임은 재생·중지 상태만 제어하며, UI Transform이나 작성값을 변경하지 않는다.

### 상태 소유와 재생 경로

- `PPEVoiceFlowDirector`가 현재 시나리오 상태, 단계별 재생 Coroutine, 반복 Coroutine, 상태 버전을 소유한다.
- 실제 음성 출력은 `AudioManager`의 Voice AudioSource 하나를 사용한다. 상태 전환 또는 취소 시 이전 Coroutine과 반복 재생을 중단하고 Voice를 정지한다.
- 새 상태가 시작될 때 상태 버전을 올린다. 이전 Coroutine은 버전이 달라진 것을 확인하면 다음 Clip이나 반복을 재생하지 않는다. 따라서 빠른 이동으로 이전 상태의 종료 시점이 늦게 도착해도 다음 영역의 음성을 다시 덮어쓰지 않는다.

### 이동과 취소의 입력 경로

| 입력/이벤트 | 처리 위치 | 음성 처리 |
|---|---|---|
| HMD 컨트롤러 텔레포트 입력 시작·취소 | `PPEVoiceTeleportEventRelay` | 현재 상태의 이동 입력을 추적한다. 취소만으로는 텔레포트 안내 반복을 다시 시작하지 않는다. |
| 위치마커가 텔레포트를 수락함 | `BaseTeleportationInteractable.teleporting` → `PPEVoiceTeleportEventRelay.OnTeleportQueued` | 즉시 `AudioManager.StopVoice()`를 호출하고, 이동 도착 상태를 알린다. |
| Locomotion 시작/종료 | `PPEVoiceTeleportEventRelay` | 도착 알림을 보완적으로 전달한다. 이미 처리된 이동은 상태 전이 중복 없이 무시한다. |
| Game View 마우스 위치마커 클릭 | `PhysicalHmdSimulatorGate` | `TeleportationAnchor.RequestTeleport()` 직전에 `AudioManager.StopVoice()`를 호출한다. |

### 002 텔레포트 안내와 다음 영역 음성의 구분

- `TeleportInstruction` 상태의 `4_VO_PPE_EDU_002_Teleport.ogg`와 반복 Clip은 위치마커가 수락되는 시점에 중지 대상이다.
- 이동 후 `PpeArea` 상태에서 재생되는 `4_VO_PPE_EDU_003_Tablet.ogg`는 002가 이어지는 것이 아니라 다음 상태의 별도 안내다.
- 태블릿을 내려놓는 이벤트는 `PPEVoiceTabletReleaseRelay`를 통해 `PPEVoiceFlowDirector.NotifyTabletReleased()`로 전달한다. 첫 해제 이벤트는 현재 음성을 정리하고 `4_VO_PPE_EDU_004_HazmatSuit.mp3`를 한 번 재생하며, 같은 교육 중 이후 해제에서는 반복하지 않는다.
- PPE 전체 장착·태블릿 문서 완료·거울 위치 도착 후의 종료 단계는 `PPEFinaleController`가 담당하며, 조건 충족 뒤 5초 게이지가 끝나면 `4_VO_PPE_EDU_014_EduEnd`를 재생한다. 이 종료 음성은 텔레포트 안내의 반복 경로와 분리되어 있다.

### 근본 원인과 수정 판단

- 이전에는 텔레포트 입력의 취소/해제 시점과 위치마커 수락 시점이 서로 달라, 버튼 해제가 먼저 들어온 경우 현재 단계의 반복 재생이 다시 예약될 수 있었다.
- 위치마커 수락 이벤트와 Game View의 직접 클릭 경로를 가장 이른 취소 지점으로 사용하고, 입력 취소에서는 반복 재시작을 하지 않도록 정리했다.
- 이 수정은 기존 이동·PPE 선택·태블릿 완료 규칙을 바꾸지 않는다. 음성의 이전 상태 잔류만 중지한다.

### 검증 범위와 남은 수동 확인

- 정적 확인: 이동 이벤트와 Voice 중지 호출의 연결, 상태 버전 기반 Coroutine 취소, 002/003/014 Clip 분리 경로를 코드 기준으로 대조했다.
- Unity Editor 확인: Game View에서 002 재생 중 위치마커를 클릭해 002가 즉시 멈추고, 도착 뒤 003만 새로 재생되는지 확인해야 한다.
- Quest/OpenXR 확인: 실제 컨트롤러로 002 재생 중 텔레포트를 빠르게 시작·해제·이동하여, 이전 002 또는 반복 Clip이 남지 않는지 확인해야 한다. 이 실제 HMD 재현은 아직 문서화 시점의 완료 증거에 포함하지 않는다.

## 17. 2026-08-07 PPE 조건부 음성 관리

### 관리 위치

- 마스크·부츠·테이프의 조건부 음성은 개별 Action Panel에 Clip을 분산해 두지 않고, 씬의 `PPE Voice Flow` (`PPEVoiceFlowDirector`) Inspector에 집중 배정한다.
- `PPE Voice Flow`는 기존 `PPEHazmatEquipController`, `PPEEquipmentVisualController`, 마스크·좌/우 부츠·테이프 Action Panel 참조를 직렬화해 사용한다. 런타임에서 참조를 생성하거나 다른 PPE를 자동 탐색하지 않는다.

### 조건과 음성

| 이벤트 | 조건 | 재생 Clip |
|---|---|---|
| 마스크 선택 | 마스크의 마커가 Select됨 | `4_VO_PPE_EDU_011_Mask.ogg` |
| 좌/우 부츠 사용 처리 | `UseApproved`이고 방호복이 아직 장착되지 않음 | `4_VO_PPE_EDU_200_GloveBootsOreder.ogg` |
| 테이프 사용 처리 | `UseApproved`이고 좌/우 장갑·좌/우 부츠 네 항목이 모두 장착 완료되지 않음 | `4_VO_PPE_EDU_201_TapeOrder.ogg` |

### 장착 완료 판정

- 테이프 조건은 `PPEEquipmentVisualController.IsItemUsed()`로 `RubberGloveLeft`, `RubberGloveRight`, `RubberBootLeft`, `RubberBootRight`를 각각 확인한다.
- 네 항목 모두 기존 Use 승인·장착 흐름을 끝낸 경우에만 장갑·부츠 선행 장착 완료로 판정한다. 이 확인은 기존 장착 연출이나 최종 PPE 완료 조건을 바꾸지 않는다.

### 검증 범위

- Unity 직렬화 확인: `PPE Voice Flow`의 방호복·장비 시각화·마스크·부츠 2개·테이프 참조와 011/200/201 Clip 참조가 모두 채워진 것을 확인했다.
- 남은 수동 확인: Play Mode에서 마스크 선택, 방호복 전 부츠 Use, 네 항목 중 하나라도 미장착인 상태의 테이프 Use를 각각 실행해 해당 음성이 재생되는지 확인해야 한다.

## 18. 2026-08-07 정상 PPE 선택 음성과 우선순위

### `PPE Voice Flow` 배정

| 정상 PPE 선택 | Clip | 재생 규칙 |
|---|---|---|
| 방호복 | `4_VO_PPE_EDU_100_HowToSuit.ogg` | Clean 상태 선택 시 |
| 좌/우 부츠 | `4_VO_PPE_EDU_101_HowToBoots.ogg` | Clean 상태에서 먼저 선택된 한 쪽에만 세션당 1회 |
| 안전대 | `4_VO_PPE_EDU_102_HowToBackPlate.ogg` | Clean 상태 선택 시 |
| 장갑 좌/우 | `4_VO_PPE_EDU_105_HowToGloves.ogg` | Clean 상태 선택 시 |
| 테이프 | `4_VO_PPE_EDU_106_HowToTape.ogg` | Clean 상태 선택 시 |

- 위 Clip과 대상 Action Panel은 모두 `PPE Voice Flow` Inspector에 직렬화한다.
- 새 PPE 선택 음성이 시작되면 `PPE Voice Flow`가 이전 Voice를 정지하고, 모든 PPE Action Panel의 대기 중인 오답 음성 Coroutine을 취소한다. 따라서 이전 상호작용의 오답 음성이 다음 PPE 안내 뒤에 늦게 재생되지 않는다.
- 위치마커 이동의 텔레포트 시작도 대기 중인 PPE 오답 음성을 취소한다.

### 텔레포트 단계

- `ppe_education_selected`에는 `4_VO_PPE_EDU_001_PPE_EduSelected.ogg`를 유지한다. 이 Clip에 텔레포트 안내가 포함되어 있다.
- `teleport_instruction`은 상태·이동 허용·도착 전이만 담당하며 Clip 배열과 repeatClip을 비운다. 따라서 다음 지점에서 002 또는 반복 Clip이 남아 재생되지 않는다.

### 초기 오염 마스크와 정상 마스크 분기

- 초기 노출 상태는 방호복·좌/우 부츠·마스크·헬멧 모두 `Contaminated`로 작성되어 있다.
- 마스크는 초기 오염 상태에서 잡으면 `4_VO_PPE_EDU_011_Mask.ogg`를 재생한다. 이는 호흡 확인 안내다.
- 기존 폐기 처리 후 같은 마스크가 `Clean` 상태가 되면, 이후 잡을 때 `4_VO_PPE_EDU_103_HowToMask.ogg`를 재생한다. 011과 103을 같은 시점에 겹쳐 재생하지 않는다.

## 19. 2026-08-28 시나리오 필수 PPE 전용 How-To 음성

### 적용한 변경

- 사용자 표시 용어는 `고글` 대신 `화학보안경`, `방독면` 또는 `송기 마스크` 대신
  `송기마스크`로 통일한다. 직렬화 호환성이 필요한 코드 식별자와 자산명인 `SafetyGoggles`,
  `GasMask`, `Goggle`, `PPE_A_Goggle`은 변경하지 않는다.
- 사용자가 추가한 다음 음원을 현재 기준 씬 `Assets/Scenes/4_PPE_Room.unity`의
  `PPE Voice Flow`에 직렬화했다.
  - 정상 화학보안경: `4_VO_PPE_EDU_107_HowToGoggle.mp3`
  - 정상 안면보호대: `4_VO_PPE_EDU_108_HowToFaceShield.mp3`
  - 좌·우 니트릴 내부장갑 공용: `4_VO_PPE_EDU_109_HowToInneerGlove.mp3`
- 107은 `PPE_A_Goggle_Clean`, 108은 `PPE_A_FaceShield_Clean`, 109는
  `PPE_A_InnerGlove_L/R`의 Grab 이벤트에 연결했다. 오염 화학보안경과 오염 안면보호대에는 연결하지 않았다.
- 좌·우 니트릴 내부장갑은 같은 안내를 공유하며, 한 교육 모드 세션에서 먼저 잡은 정상 장갑에만
  109를 한 번 재생한다. 새 모드 세션을 시작하면 이 1회 상태를 초기화한다.

### 재생 규칙과 단일 기준

- How-To 음성은 `Education` 모드에서 작업계획이 확정된 뒤, 현재 상태가 `Clean`이고 해당
  `PPEItemType`이 선택한 작업계획의 필수 목록에 포함된 경우에만 재생한다.
- 밀폐공간 필수 목록은 `m_ConfinedSpaceRequiredItemTypes`, 누출 대응 필수 목록은
  `m_LeakResponseRequiredItemTypes`를 그대로 사용한다. 음성 전용 PPE 목록을 중복 작성하지 않는다.
- 현재 필수 목록에 따라 107·108은 누출 대응 교육에서만 재생하고 밀폐공간 교육에서는 재생하지 않는다.
  109는 두 작업계획 모두 니트릴 내부장갑을 요구하므로 양쪽 교육에서 재생한다.
- 현재 실제 Grab 경로에서 사용하는 기존 101~106 How-To도 같은 필수 PPE 게이트를 거친다. 안전대·송기마스크는 밀폐공간에서만,
  화학보안경·안면보호대는 누출 대응에서만 안내하며 공통 장화·장갑·안전모는 양쪽에서 안내한다.
- 방호복 Grab은 최근 확정된 005 패널 안내를 사용하고 100으로 되돌리지 않는다. 방호복은 양 작업계획의
  공통 필수 PPE이므로 이번 시나리오 필터로 체감 동작이 달라지지 않는다.
- `PackingTape`는 PPE 필수 배열에는 없지만 기존 작업계획 정책에서 양 시나리오 공통 착용 절차로
  허용되는 항목이므로 106 안내를 유지한다.
- `Training`, `Test`, 작업계획 미선택 상태에서는 How-To를 재생하지 않는다. 오염 송기마스크의 011처럼
  How-To가 아닌 기존 상태 점검·오답·순서 안내는 이번 변경 대상이 아니다.

### PPE별 재생·비재생 판정표

아래 표는 `Education` 모드에서 작업계획을 선택하고 정상 PPE를 최초로 잡았을 때의 정적 판정이다.
`재생 안 함`은 해당 PPE가 선택한 작업계획의 필수 항목이 아니어서 How-To 게이트가 차단한다는 뜻이다.

| 대상 | 연결 음성 | 밀폐공간 | 누출 대응 | 비고 |
| --- | --- | --- | --- | --- |
| 방호복 | 005 패널 안내 | 재생 | 재생 | 현재 Grab 경로는 How-To 100이 아니라 기존 005를 유지한다. |
| 안전화 | 101 | 재생 | 재생 | 양쪽 작업계획의 공통 필수 PPE다. |
| 안전대 | 102 | 재생 | 재생 안 함 | 밀폐공간 필수 PPE다. |
| 정상 송기마스크 | 103 | 재생 | 재생 안 함 | 오염 송기마스크의 011 상태 안내와 별도다. |
| 안전모 | 104 | 재생 | 재생 | 양쪽 작업계획의 공통 필수 PPE다. |
| 외부 화학장갑 | 105 | 재생 | 재생 | 좌·우 모두 양쪽 작업계획의 공통 필수 PPE다. |
| 패킹 테이프 | 106 | 재생 | 재생 | 필수 PPE 배열 밖의 기존 양 시나리오 공통 착용 절차 예외다. |
| 정상 화학보안경 | 107 | 재생 안 함 | 재생 | 누출 대응 필수 PPE다. |
| 정상 안면보호대 | 108 | 재생 안 함 | 재생 | 누출 대응 필수 PPE다. |
| 내부 니트릴 장갑 | 109 | 재생 | 재생 | 좌·우가 음성을 공유하며 교육 세션당 최초 한 번만 재생한다. |

이 표는 코드와 씬 직렬화 연결을 대조한 결과다. 실제 음원 출력 성공, 음량, 음질 및 좌·우 장갑의
중복 방지는 아래 수동 Play Mode 검증을 완료한 뒤 실행 검증 결과로 확정한다.

### 검증

- `PPELocomotionPpeRegressionValidationHarness`에 107~109 Clip·정상 PPE 참조, 밀폐/누출 필수 여부,
  Education/Training 분리와 니트릴 1회 상태 초기화를 확인하는 검증을 추가했다.
- `dotnet build Assembly-CSharp.csproj --no-restore`와
  `dotnet build Assembly-CSharp-Editor.csproj --no-restore`는 오류 0개로 통과했다. 출력된 경고는 기존
  deprecated API와 미할당 DTO 필드 경고다.
- 열린 Unity Editor에서 외부 변경을 다시 읽은 뒤 Domain Reload가 완료됐고 최신 로그에 C# 컴파일 오류는
  없었다. Unity MCP named pipe가 다시 열리지 않아 이번 세션에서는 Preview Scene 하네스 실행 결과를
  확보하지 못했다.
- 아직 필요한 수동 검증은 Play Mode에서 밀폐공간/누출 대응 교육을 각각 시작해 정상 PPE를 잡고,
  필수 PPE만 해당 How-To가 재생되는지와 107~109의 음질·볼륨을 실제 출력으로 확인하는 것이다.

## 20. 2026-08-07 반복 안내와 게이지 초기값

- 태블릿을 놓을 때의 004 안내와 방호복 Use 승인 때의 006 안내는 각 교육 실행에서 최초 한 번만 재생한다. 이후 태블릿을 다시 잡았다 놓거나 같은 방호복 상호작용이 다시 전달되어도 이전 안내를 재시작하지 않는다.

### 2026-08-16 방호복 선행 차단 후 장갑·장화 재안내 제안

- 방호복 미착용 상태에서 장갑·장화를 처음 Grab하면 안내 음성은 들리지만 사용 승인은 차단된다.
- 이 최초 시도에서 교육 음성 1회 상태가 소비되면, 방호복을 착용하고 돌아온 정상 Grab에서는
  안내를 다시 들을 수 없는 학습 흐름 문제가 있다.
- 보완안은 선행 조건 차단 시 `방호복 착용 후 재안내 대기` 상태를 저장하고, 방호복 착용 완료 후
  해당 PPE의 다음 정상 Grab에서 같은 안내를 추가 1회 재생한 뒤 상태를 지우는 것이다.
- 무조건 재생 횟수를 늘리지 않으며 `미착용 최초 시도 → 방호복 착용 완료 → 다음 정상 Grab`
  순서가 성립할 때만 최대 1회 추가한다.
- 좌·우 및 장갑·장화 사이의 상태 공유 범위와 재생할 정확한 Clip은 후속 결정 사항이다. 아직
  런타임 로직에는 적용하지 않았다.
- 거울 관찰 게이지는 숨길 때와 관찰 시작 직전에 `fillAmount`를 0으로 초기화한다. 따라서 시작 위치나 거울 도착 직후에 100%로 채워진 게이지가 한 프레임이라도 표시되지 않는다.
- 종료 복귀 대상은 PPE_1 도착 앵커가 아니라 시작 시 XR Origin의 `(0, 0, 0)` 위치에 고정한 `PPE Lesson Start Return Anchor`다.

## 19. 2026-08-07 PPE 진행·거울 음성 순서

`PPE Voice Flow`가 새 조건 음성을 시작하기 전에 현재 Voice와 대기 중인 PPE 오답 음성을 취소한다.

| 조건 | Clip |
|---|---|
| 태블릿을 놓음 | 004 |
| 초기 오염 방호복을 잡아 패널 표시 | 005 |
| 정상 방호복 Use 승인 | 006 |
| 방호복 장착 후 가운데 위치마커 도착 | 008 |
| 방호복·테이프 이외 PPE Use 승인 | 010 |
| 모든 PPE 장착 후 거울 미도착 | 012 |
| 거울 위치마커 도착 | 013 |
| 013 뒤 5초 게이지 완료, 모든 완료 조건 충족 | 014 |
| 013 뒤 5초 게이지 완료, 완료 조건 미충족 | 015 |

- `teleport_instruction`은 Clip과 repeatClip을 비운 상태이며, 001에 포함된 텔레포트 안내와 중복 재생하지 않는다.
- 거울 5초 게이지는 위치마커 도착으로만 시작한다. 014 재생 후에는 기존 Fade/시작 위치 복귀를 수행하고, 015에서는 복귀하지 않는다.
- 2026-08-20: 5초 측정이 한 번 끝나면 같은 자리에서는 다시 측정하지 않는다. 관찰 반경을 벗어났다가 다시 들어와야 다음 측정이 시작된다. 미완료 후 재검사 자체는 유지한다.
- 2026-08-20 FS-19 예외: 이름 필드가 비어 있거나 공백만이면 Enter 제출을 막고 `NameInput`에 남는다. 한글 입력과 값이 있는 제출은 기존과 같다.

## 21. 2026-08-11 텔레포트 단계 테스트 시작점

### 요청과 보존 범위

- 이번 변경은 `PPE Voice Flow` Inspector에 텔레포트 단계 전용 테스트 체크박스를 추가하고,
  아무 체크박스도 선택하지 않은 기본 실행을 `Welcome`부터 시작하도록 되돌리는 요청에만 대응한다.
- 상태 소유자는 `PPEVoiceFlowDirector` 하나이며, 기존 Card/Modal 테스트 시작점, 음성 Clip,
  UI 작성값, 컨트롤러 입력, 텔레포트 도착 전이는 변경하지 않는다.
- 입력 경로는 기존 `Primary2DAxis -> Teleport Mode -> PPEControllerTeleportModeManager ->
  XRRayInteractor -> TeleportationAnchor`를 그대로 사용한다. 새 입력 fallback이나 런타임 참조
  자동 수리는 추가하지 않는다.

### 적용한 변경

- `Start At Teleport For Testing`을 켜면 `TeleportInstruction` 상태부터 시작하고, 이후에는
  기존 텔레포트 시작·도착 신호를 거쳐 `PpeArea`로 전이한다.
- 테스트 시작점 우선순위는 `CardIntro -> ModalDetail -> TeleportInstruction -> InitialState`다.
- `_scale_0` 씬의 `Initial State`는 `Welcome`, Card/Modal/Teleport 테스트 체크박스는 모두
  꺼진 상태로 저장했다.
- 명시적으로 실행하는 Voice Flow 설정 도구도 정상 구성 시 `Welcome`과 세 체크박스 해제를
  적용하며, 검증에서 이 기본 상태를 확인한다.

### 검증 구분

- 정적 확인: 시작 상태 선택, 텔레포트 이동 게이트, 기존 상태 전이 경로를 대조한다.
- Unity Editor 확인: 기본 Play에서 Welcome 음성이 시작되는지, Teleport 체크 후 Play에서
  `TeleportInstruction`부터 시작하는지 확인해야 한다.
- Quest/OpenXR 확인: 테스트 체크 후 실제 조이스틱 텔레포트와 `PpeArea` 도착 전이는 실기기
  확인 전까지 완료로 판정하지 않는다.

## 22. 2026-08-13~14 키보드의 컨트롤러 교육 진입점과 보이스 그룹 분리

### 이번 변경이 대응하는 요청

- 이름 입력 키보드의 Enter 오른쪽 바깥에 기존 A 키 외형을 복제한 진입 표시를 두고
  `컨트롤러 교육` TMP 문구를 함께 표시한다.
- 이번 단계의 A 표시는 기능이 없는 시각 요소다. 클릭, XRI Select, 키 입력, 상태 전이는
  연결하지 않는다.
- 컨트롤러 안내 보이스 할당을 긴 `Controller Edu`와 이름 제출 뒤 사용할 짧은
  `Controller Simp` 두 그룹으로 분리한다.

### 상태와 단일 소유자

- 흐름 상태 소유자는 계속 `PPEVoiceFlowDirector` 하나다.
- 일반 보이스는 `m_VoiceSteps`, 긴 컨트롤러 교육은 `m_ControllerEduVoiceSteps`, 짧은 안내는
  `m_ControllerSimpVoiceSteps`에 작성한다.
- 기존 Trigger 001/002, Grip 003, Joystick 004/005와 작성된 가이드 Image 참조는
  `Controller Edu`로 이동했다.
- `Controller Simp`에는 새 음원을 다음과 같이 할당했다.
  - Trigger: `VO_PPE_CTRL_SIMP_001_Start.mp3`, `VO_PPE_CTRL_SIMP_002_RayTrigger.mp3`
  - Grip: `VO_PPE_CTRL_SIMP_003_GripGrab_Release.mp3`
  - Joystick: `VO_PPE_CTRL_SIMP_004_Joystick.mp3`, `VO_PPE_CTRL_SIMP_005_GuideFollow.mp3`
- 현재 `m_ControllerNarrationAfterName`은 `Simple`이다. 이름 제출 후 위 다섯 Clip을
  `Trigger → Grip → Joystick` 화면과 함께 순차 재생한 뒤 카드 단계로 전이한다.
- 긴 기존 음성과 Image 참조는 `Controller Edu`에 그대로 보존한다.
- 키보드 A 진입 후 긴 교육을 어디로 복귀시킬지는 아직 정의되지 않았으므로 A 이벤트와
  `BeginControllerEducation` 같은 임의 상태 전이는 추가하지 않았다.

### UI 작성 방식

- 씬 YAML에 임의 FileID를 만들지 않는다. 열린 `_scale_0` 씬에서
  `Tools > PPE > Keyboard > Create Controller Education Visual Entry`를 명시적으로 실행해
  Unity가 오브젝트와 직렬화 ID를 생성한다.
- 생성기는 최초 생성에만 A 외형, Enter 기준 위치, TMP 초기값을 제공한다. 같은 이름의
  오브젝트가 이미 있으면 기존 TMP·시각 작성값은 보존하고 위치만 다시 정렬한다.
- `Content`에는 `VerticalLayoutGroup`이 있으므로 진입 표시를 그 자식으로 두지 않는다.
  가장 가까운 `Keyboard Canvas Root`의 직접 자식으로 두고, Enter의 월드 중심을 Canvas 좌표로
  변환해 오른쪽에 배치하며 `anchoredPosition3D.z`도 Enter와 같은 평면으로 맞춘다.
- 복제된 A의 `XRKeyboardKey`, Poke Follow, Keyboard Batch Follow, Collider, AudioSource와
  Graphic Raycast를 비활성화한다. 따라서 외형은 보이지만 글자 `a` 입력이나 UI 클릭은 없다.
- `ControllerGuide_mini`는 조이스틱 클릭으로 수시 표시하는 별도 기존 기능이므로 변경하지 않는다.

### 영향 범위와 검증

- `AudioManager`의 Voice 라이브러리 수집 도구가 일반, Edu, Simp 세 그룹을 모두 검사하도록
  확장했다.
- 정적 컴파일은 Runtime/Editor 어셈블리 모두 오류 0개다.
- Unity Editor에서는 위 생성 메뉴 실행 후 A 외형과 TMP 위치를 Inspector에서 조정하고 씬을
  저장해야 한다. Play Mode 전후 작성값이 바뀌지 않는지 비교한다.
- Unity Play Mode에서는 이름 제출 후 Simp 다섯 Clip이 끊김 없이 순차 재생되고 각 구간의
  Trigger/Grip/Joystick Image가 하나씩만 표시되는지 확인해야 한다.
- Quest/OpenXR에서는 Simp 음성·양안 가이드 표시를 확인한다. A의 긴 교육 진입은 아직 기능을
  연결하지 않았으므로 후속 작업에서 별도로 검증한다.

### 2026-08-14 Welcome 이전 키보드 선노출 수정

- 근본 원인은 `Modal  Keyboard Canvas`가 씬에서 활성 상태였고, `PPEVoiceFlowDirector`가 첫
  렌더 프레임과 2초 시작 지연을 지난 뒤에야 `Welcome` 표시 상태를 적용한 것이다. 그 사이
  키보드가 먼저 노출됐다.
- `_scale_0`의 `Modal  Keyboard Canvas` 시작 작성값을 비활성화했다. Director는 외부의
  항상 활성 오브젝트에 있으므로 `Welcome` 음성이 끝나 `NameInput`으로 전이할 때 해당 Canvas를
  정상적으로 활성화한다.
- 기대 순서는 `Welcome → 이름 입력 키보드 → Enter → Controller Simp → 카드`다.
- Voice Flow 정적 검증에 키보드 표시 루트의 시작 비활성 상태 검사를 추가했다.

## 23. 2026-08-14 단계별 음원 재구성과 PPE 모드 선택 단계

### 적용한 변경

- 대상은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나로 제한했다.
- `Assets/Audio/Voice`의 단계별 폴더 구조와 각 음원의 `.meta`를 확인하고, 새 경로를 Voice Flow 검증 도구에 반영했다.
- Welcome 음성에 이름 입력·Enter 안내가 포함되므로 `name` 단계의 Clip 배열은 비웠다. `NameInput`은 무음 상태로 키보드 Enter 제출만 기다린다.
- 시작부터 PPE 구역 도착까지의 음성 순서를 다음과 같이 재배정했다.
  - `Welcome` → 이름 입력 대기
  - Enter → `Controller Simp 001~005`
  - `CardIntro` → `CARD_001`
  - 카드 선택 → `CARD_002`, `MODAL_001`
  - PPE 착용교육 선택 → `MODAL_003`, `MODAL_002`
  - 교육 모드 선택 → `MODAL_004`, `PPE_001_MoveToPPE`
  - 텔레포트 도착 → `PPE_002-2_PPE_Start`, `PPE_003_Tablet`
- PPE 착용교육 선택은 더 이상 모달을 닫거나 이동을 즉시 허용하지 않는다. `1_EduChoice`를 숨기고 `2_Mode`를 표시한다.
- `PPE Edu Mode`를 선택했을 때만 모달을 닫고 시나리오 이동 준비 상태를 연다. `PPE Training Mode`와 `PPE Test Mode`는 선택 이벤트만 유지하며 아직 이동을 허용하지 않는다.
- 새 번호와 파일명으로 교체된 PPE 조건 음성을 기존 이벤트 필드에 다시 연결했다. 센터 위치마커 도착 시 `4_VO_PPE_EDU_007_OrderGuide.mp3`가 끝난 뒤 `4_VO_PPE_EDU_008_CheckPPE.ogg`를 연속 재생한다.
- 삭제된 B/A/GuideFollow 음원을 참조하던 도달 불가능한 `controller_edu_panel` 단계를 제거했다.

### 근본 원인

- 음원 교체 과정에서 일부 `.meta` GUID가 바뀌었고 씬은 삭제된 이전 GUID를 계속 참조했다. Welcome이 Missing으로 판단되어 곧바로 `NameInput`으로 전이한 것이 키보드와 키보드 음성이 먼저 나온 직접 원인이었다.
- 기존 `ScenarioDetailModal.SelectIncompletePpeScenario()`는 PPE 착용교육 버튼 선택 즉시 모달을 닫고 텔레포트를 허용했기 때문에 새 `2_Mode` 단계를 표시할 수 없었다.
- `PPEVoiceFlowSetup`의 컨트롤러 음원 경로가 단계별 폴더로 이동하기 전 위치에 고정되어 있었다.

### 영향 범위

- Welcome, 무음 NameInput, Controller Simp, 카드, 두 모달 선택 단계, 교육 모드 선택, PPE 구역 도착 음성.
- `ScenarioDetailModal`의 PPE 착용교육 및 교육/훈련/테스트 버튼 처리.
- PPE 조건 음성의 직렬화 참조와 Voice Flow 정적 검증 경로.
- 카드 Trigger, 모달 XRI 버튼 입력, 텔레포트 도착 이벤트 자체는 기존 입력 경로를 유지한다.

### 완료한 검증

- Runtime 및 Editor C# 빌드: 오류 0개.
- `Tools > PPE > Voice Flow > Validate HandTest Scale 0` 실행.
- `_scale_0` 씬의 AudioClip 참조 54개를 전체 GUID 대조했고 Missing 참조는 0개였다.
- Unity Console의 현재 오류는 0개였다. 남은 경고는 XR 오디오 출력 fallback 및 Unity AI 연결 경고로 이번 Voice Flow 변경과 무관하다.

### 필요한 수동 검증

- Play Mode에서 위 음성 순서와 각 버튼 입력 대기 시점이 정확한지 청취한다.
- PPE 착용교육 선택 후 `2_Mode`가 보이고 텔레포트가 아직 차단되는지 확인한다.
- 교육 모드 선택 후 `MODAL_004 → MoveToPPE`가 끝난 뒤 텔레포트가 허용되는지 확인한다.
- 텔레포트 도착 시 `PPE_002-2_PPE_Start → PPE_003_Tablet`이 한 번씩 순서대로 재생되고, 그 뒤 태블릿을 잡을 수 있는지 확인한다.
- Quest/OpenXR에서 Trigger 버튼 선택, 음성 스킵 제외, 양안 모달 표시를 확인한다.

## 24. 2026-08-14 A 표시 복구, 미니 가이드 고정 및 거울 게이지 복구

### 적용한 변경

- `_scale_0`의 키보드 Enter 옆에 기능이 없는 `Controller Education Entry (Visual Only)` A 표시와 `컨트롤러 교육` TMP를 복구했다. `XRKeyboardKey`, Collider 및 Graphic Raycast는 계속 비활성이다.
- A 표시 복구 뒤 다시 활성화된 `[Modal  Keyboard Canvas]`를 시작 비활성 상태로 저장했다. 따라서 시작 순서는 계속 `Welcome → NameInput 키보드`이다.
- `ControllerGuide_mini`를 기존 `Window Canvas`의 먼 월드 좌표에서 분리하고 자체 World Space Canvas로 구성했다. `Main Camera` 자식으로 두고 카메라 로컬 위치 `(-0.30, 0.02, 1.20)`에 고정했다. 최초 `0.0004` 스케일로는 식별하기 어려워 사용자 확인에 따라 정확히 2배인 `0.0008`을 씬 작성값으로 저장했다.
- 기존 조이스틱 클릭 토글과 시작 비활성 상태는 변경하지 않았다.
- 사용자가 새로 작성한 `ControllerGuide/Context/1_Ray` 그룹을 Simple 001·002 화면으로 연결했다. 이 그룹 아래의 기존 `1_Ray`는 `Card`, 기존 `4_Panel`은 `Panel`로 유지한다. Simple 003은 `2_Marker`, 004는 `3_Ray_T`를 표시하며 005 동안에도 `3_Ray_T`를 유지한다.
- `Controller Simp`는 공통 컨트롤러 이미지로 대체하지 않는다. 001·002는 `1_Ctrl_Trigger`와 `1_Ray`, 003은 `2_Ctrl_Grip`과 `2_Marker`, 004·005는 `3_Ctrl_Joystick`과 `3_Ray_T`를 동시에 표시한다. 각 이미지의 위치·크기·Sprite는 씬 작성값을 그대로 사용한다.
- `Controller Simp`에서는 Clip별 오른쪽 부모 그룹과 왼쪽 companion 매핑이 표시 상태의 단일 기준이다. 기존 상태별 `m_ControllerRayStep`, `m_ControllerMarkerStep`, `m_ControllerRayTStep`, `m_ControllerPanelStep` 토글은 적용하지 않으며, 특히 `1_Ray/Card`와 `1_Ray/Panel`의 작성된 활성 상태를 보존한다.
- `PPE Mirror Gauge Canvas`는 활성, 자식 `Mirror Observation Gauge`는 시작 비활성으로 저장했다. `PPEFinaleController`가 관찰 시작 시 자식만 표시하는 기존 로직을 그대로 사용한다.
- 풀장착 모델은 `3_PPE_Room_HandTest_scale_Orig`에서 확인한 `PPE Body Anchor`, `PPE_A_SuitWear` 및 직속 장비 자식 11개의 부모·localPosition·localRotation·localScale을 `PPEFullSuitFloorGrounding` 하네스의 고정 기준으로 보관한다. `Validate Orig Baseline`은 차이를 오류로 중단하고, `Restore Orig Baseline`만 명시적으로 씬 값을 복원·저장한다. 런타임 자동 보정은 하지 않는다.
- PPE 구역 도착 음성은 `4_VO_PPE_EDU_002-2_PPE_Start.mp3` 뒤 `4_VO_PPE_EDU_003_Tablet.mp3`가 이어지도록 구성했다. 태블릿을 처음 내려놓으면 별도 해제 이벤트에서 `4_VO_PPE_EDU_004_HazmatSuit.mp3`가 한 번 재생된다.

### 근본 원인

- 미니 가이드는 `Window Canvas` 아래에서 카메라 기준 약 7.1m 떨어진 월드 위치에 있어 사용자가 이동하거나 회전해도 시야에 고정되지 않았다.
- 거울 게이지는 표시 대상 자식이 활성이어도 상위 `PPE Mirror Gauge Canvas`가 비활성이어서 렌더링될 수 없었다.
- 키보드 시작 루트가 활성으로 저장되면 Voice Flow가 Welcome 상태를 적용하기 전 한 프레임 동안 키보드가 먼저 노출될 수 있다.

### 영향 범위

- 키보드의 시각 전용 A 안내, Welcome/NameInput 시작 표시 순서.
- 조이스틱 클릭으로 토글되는 `ControllerGuide_mini`의 공간 배치와 카메라 추적.
- 거울 도착 음성 종료 뒤 시작하는 5초 관찰 게이지.
- PPE 구역 도착 직후 002/003 음성 순서, 태블릿 해제 후 004 음성.

### 완료한 검증

- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 오류 0개.
- `Tools > PPE > Voice Flow > Validate HandTest Scale 0` 실행 후 씬 dirty 상태 없음.
- `Tools > PPE > Validate Scale 0 Controller Test Inputs`를 확장해 A 표시, 카메라 자식 World Space 미니 가이드, 거울 게이지 부모/자식 시작 상태를 검증했고 통과했다.
- 씬 직렬화에서 `ControllerGuide_mini` 시작 비활성, `Main Camera` 부모, World Space Canvas 참조를 확인했다.
- 씬 직렬화에서 `PPE Mirror Gauge Canvas` 활성 및 `Mirror Observation Gauge` 시작 비활성을 확인했다.

### 필요한 수동 검증

- Play Mode/Quest에서 조이스틱 클릭 시 미니 가이드가 시야 정면 왼쪽에 적절한 크기로 보이고, 머리를 돌려도 같은 상대 위치를 유지하는지 확인한다.
- 미니 가이드가 너무 가깝거나 크면 씬의 `ControllerGuide_mini` 로컬 위치와 현재 스케일 `0.0008`을 Inspector에서 조정한다. 런타임 코드는 이 값을 덮어쓰지 않는다.
- PPE 구역 도착 시 002와 003을 끝까지 들은 뒤 태블릿 잡기 단계가 자연스럽게 이어지는지 확인한다.
- 태블릿을 처음 내려놓았을 때 EDU 004가 한 번 재생되고, 이후 다시 잡았다 내려놓아도 반복되지 않는지 확인한다.
- 거울 위치마커 도착과 013 음성 종료 뒤 5초 게이지가 Game View와 Quest 양안에서 보이는지 확인한다.

## 25. 2026-08-14 풀장착 모델·시작 손·태블릿 종료 조건 회귀 하네스

### 적용한 변경

- `PPEFullSuitFloorGrounding`은 `_scale_0` 씬이 이미 열린 Edit Mode에서만 동작한다. `Validate Orig Baseline`은 읽기·검증만 수행하고, `Restore Orig Baseline`만 Undo 기록 후 명시적으로 부모와 로컬 Pose를 복원·저장한다. Play Mode 및 런타임 자동 보정은 추가하지 않는다.
- 고정 계층은 `XR Origin (VR) → PPE Body Anchor → PPE_A_SuitWear`이다.
- 핵심 작성 수치는 다음과 같다.

| 대상 | localPosition | localRotation (Quaternion) | localScale |
|---|---|---|---|
| `PPE Body Anchor` | `(0.37, 0.06, 8.994)` | `(0, 0, 0, 1)` | `(1, 1, 1)` |
| `PPE_A_SuitWear` | `(-0.017, 0.054, -0.169)` | `(0.7067602, 0.018519262, 0.018576182, -0.70696676)` | `(2, 1.7718132, 1.649093)` |

- 풀장착 직속 자식 11개(`PPE_A_Taped_Hand_R/L`, `PPE_A_Taped_Boot_R/L`, `PPE_A_Boots_R/L`, `PPE_A_Backplate`, `PPE_A_Mask`, `PPE_A_Helmet_Strap`, `PPE_A_Glove_R/L`)의 부모·localPosition·localRotation·localScale도 하네스의 `ChildPoses`에 `_Orig` 수치로 고정했다.
- `PPEHazmatEquipController`의 작성값도 함께 검증한다: `bodyYawFollowSmoothTime=0.2`, `handSwapFadeDuration=0.4`, `animationDuration=1.8`, `useUnscaledTime=true`, `approachPhaseEnd=0.65`, 2키 Motion Curve `(0,0)→(1,1)` 및 Loop Wrap.
- 임시 Editor 오브젝트를 사용하는 회귀 검증으로 다음 동작을 고정했다.
  - `UseApproved` 전에는 `PPE Body Anchor`가 HMD를 따라 이동하지 않는다.
  - 컨트롤러 비활성화 시 Body Anchor의 작성 위치·회전을 복원한다.
  - 손 모델의 `activeSelf`는 참조 완전성 판정 조건이 아니다. 컨트롤러 가이드가 손을 숨긴 상태도 정상 참조로 인정한다.
- 시작 화면에서는 `PPEVoiceFlowDirector.Start()`가 컨트롤러 전용 표시를 다시 확정해야 한다. 하네스는 좌우 컨트롤러 모델이 활성이고 Bare/Suit/Glove/Tape 손 모델 8개가 모두 비활성인지 임시 오브젝트로 검증한다.
- `PPEFinaleController.HasCompletionRequirements()`는 방호복 장착, 필수 PPE 전체 사용뿐 아니라 `PPETabletChecklistController.IsDocumentCompleted`도 반드시 요구한다. 태블릿 문서가 완료되지 않으면 퀴즈·종료 단계로 진행하지 않는다.
- 종료 음원 참조는 완료 `4_VO_PPE_EDU_015_EduEnd`, 미완료 `4_VO_PPE_EDU_013_UnEnoughPpe`로 하네스에서 고정한다.

### 근본 원인

- 구버전 전체 복원으로 오늘 수정된 착용 전 Body Anchor 상태 게이트와 손 모델 활성 상태 독립 검증이 다시 사라져 동일 회귀가 발생했다.
- `PPEVoiceFlowDirector.OnEnable()` 뒤 다른 장비 초기화가 손 모델을 다시 활성화할 수 있으므로 모든 `OnEnable`이 끝난 `Start()`에서 컨트롤러 전용 표시를 재확정해야 한다.
- `PPEFinaleController`에는 태블릿 참조가 직렬화돼 있었지만 기존 종료 조건에서 실제 `IsDocumentCompleted`를 읽지 않아 문서 확인이 완료 게이트에서 누락됐다.

### 영향 범위

- 풀장착 모델의 부모·위치·회전·스케일, 직속 장비 11개의 상대 Pose, HMD 추적 시작 시점과 비활성화 복원.
- Welcome/컨트롤러 가이드 시작 프레임의 컨트롤러 모델과 PPE 손 모델 상호 배타 표시.
- 태블릿 체크·서명 완료 여부, 거울 관찰 결과, 퀴즈 진입 및 완료·미완료 음성 재생 조건.

### 완료한 검증

- 정적 확인: `_scale_0`과 `_Orig`의 풀장착 계층 및 직렬화 수치를 대조했다.
- Editor 하네스: `PPEFullSuitFloorGrounding`에 Pose·참조·애니메이션 수치와 세 가지 임시 동작 회귀 검증을 추가했다.
- 통합 하네스: `PPERoomCardRaySelectionHarness`에 시작 컨트롤러/손 상호 배타 검증, 태블릿 완료 조건 호출 검증, 완료·미완료 음원 참조 검증을 추가했다.
- 이번 기록 시점에는 Unity MCP, Play Mode 및 Quest/OpenXR를 실행하지 않았다.

### 필요한 수동 검증

- Play Mode 첫 프레임부터 컨트롤러 가이드 종료 전까지 컨트롤러와 손 모델이 동시에 보이지 않는지 확인한다.
- 태블릿 체크·서명을 완료하지 않은 상태에서 거울 관찰을 끝내면 미완료 음성이 재생되고 퀴즈로 넘어가지 않는지 확인한다.
- 태블릿 문서와 모든 PPE를 완료한 상태에서는 거울 관찰 후 퀴즈를 거쳐 완료 음성이 한 번 재생되는지 확인한다.

## 26. 2026-08-16 Train/Test 컨트롤러 상세교육 진입·복귀 흐름

### 대상과 보존 조건

- 대상은 `Assets/Scenes/3_PPE_Room_Train_Test.unity`이다.
- 키보드의 UI A와 오른손 컨트롤러 물리 A는 동일한
  `PPEVoiceFlowDirector.NotifyControllerEducationRequested()`를 호출한다.
- 진입은 `NameInput` 상태에서만 허용하며, 같은 프레임 중복 입력과 상세교육 재생 중 재진입을
  차단한다.
- 카드·모달·텔레포트·PPE 모드 진행, 키보드에 입력 중인 이름, 씬 작성 UI 레이아웃은
  기존 동작을 보존한다.

### 확정 상태 전이

| 조건/입력 | 전이 |
|---|---|
| 새 세션에서 이름 제출 | `NameInput → Controller Simp → CardIntro` |
| 키보드에서 물리 A 또는 UI A | `NameInput → Controller Edu` |
| 상세교육 자연 종료 또는 음성 Skip | `Controller Edu → NameInput`, 상세교육 완료 기록 |
| 상세교육 완료 뒤 이름 제출 | `NameInput → CardIntro` |
| 상세교육 완료 뒤 물리 A 또는 UI A | 상세교육 재진입 허용 후 다시 `NameInput` 복귀 |

- 상세교육 완료 기록은 현재 실행 세션에서만 유지하며 `OnEnable`과 명시적 새 흐름 시작에서
  초기화한다.
- 상세교육을 보지 않은 사용자는 기존 간단교육을 받는다. 상세교육을 이미 본 사용자는 같은
  조작 내용을 반복하지 않고 카드 단계로 진행한다.

### 음원과 시각 기준

- 상세교육은 `Assets/Audio/Voice/1_2_ContDetail/`의
  `VO_PPE_CTRL_DETAIL_001_Start.mp3`부터 `005_GuideFollow.mp3`까지 다섯 음원을 사용한다.
- 상세교육과 간단교육은 다음 작성 가이드 쌍을 공유한다.

| 음성 구간 | 오른쪽 안내 | 왼쪽 컨트롤러 |
|---|---|---|
| 001·002 Trigger | `1_Ray`의 `Card`·`Panel` | `1_Ctrl_Trigger` |
| 003 Grip | `2_Marker` | `2_Ctrl_Grip` |
| 004·005 Joystick | `3_Ray_T` | `3_Ctrl_Joystick` |

- Clip별 `controllerGuideVisuals`와 `controllerGuideCompanionVisual`이 표시 상태의 단일 기준이다.
  런타임 상태 로직은 작성된 그룹 내부 자식의 활성 상태, RectTransform 또는 Sprite를
  덮어쓰지 않는다.

### 완료한 검증

- 디스크 씬의 상세·간단 시각 배열과 companion을 GameObject FileID로 대조했다.
- 각 Image의 활성 상태, 알파, Sprite GUID와 실제 `Assets/UIs/Guide/*.png` 자산 존재를
  확인했다.
- Runtime/Editor 보조 빌드 오류 0개를 확인했다.
- `Tools > PPE > Validate Train Test Modes`의 Preview Scene 동작 검증에서 상세·간단 가이드의
  양쪽 이미지, Trigger 상단·하단 자식, 렌더 가능한 Sprite, 상세 완료 뒤 간단교육 생략 분기를
  확인했고 Unity Console PASS를 기록했다.

### 아직 필요한 수동 검증

- Play Mode에서 상세교육 전체 재생, 키보드 복귀, Enter 뒤 카드 직행을 실제 음성과 함께
  확인한다.
- 새 세션에서는 Enter 뒤 간단교육이 유지되는지 확인한다.
- Quest/OpenXR 양안에서 모든 가이드 이미지와 자막이 정상 표시되고 물리 A와 UI Ray Trigger가
  각각 한 번만 진입시키는지 확인한다.

## 상세 컨트롤러 정오답 입력 흐름 재구성

### 변경 전 판단

1. 이번 변경은 `1_2_ContDetail` 상세교육만 대상으로 한다. 이름 제출 뒤 실행되는 기존
   `1_1_ContSimp` 간단교육, 컨트롤러 가이드 Sprite·RectTransform·작성 활성 상태, 사용자가 조정한
   `Teleport_0/PPE_1 Arrival Anchor` 위치는 보존한다.
2. 상태와 음성 순서의 단일 소유자는 `PPEVoiceFlowDirector`다. 상세교육의 각 설명이 끝난 뒤에만
   해당 단계 입력을 기다리고, 별도의 자동 완료 상태나 런타임 참조 검색을 추가하지 않는다.
3. 입력 경로는 `오른손 XRController/OculusTouchController Trigger·Grip·Primary2DAxis -> InputAction
   performed -> 현재 상세교육 VoiceStep의 expectedInput 비교 -> 오답/정답 Voice -> 다음 FlowState`다.
   오답은 세 교육 입력 중 현재 단계가 아닌 입력으로 한정해 기존 A 버튼 교육 진입, UI Ray,
   텔레포트와 미니 가이드 클릭 소비자를 바꾸지 않는다.
4. 설명·오답·정답 Voice 재생 중의 입력은 판정하지 않는다. 오답 Voice 종료 뒤 같은 입력 대기를 다시
   열고, 정답 Voice `008_Correct_Input` 종료 뒤에만 다음 설명으로 전이한다.
5. 상세교육 마지막 Joystick 정답은 `008_Correct_Input -> 009_GuideFollow`를 순서대로 재생한 뒤 기존
   상세교육 종료 경로로 진행한다. Trigger 오답은 003, Grip 오답은 005, Joystick 오답은 007을 쓴다.
6. 필수 001~009 AudioClip 또는 입력 설정이 누락되면 에디터 회귀 하네스가 정확한 단계와 필드를
   오류로 보고한다. 런타임에서 이름 기반 검색·자동 연결·대체 Clip을 만들지 않는다.
7. 변경 전 기준은 001·002, 004, 006·009가 입력 대기 없이 연속 재생되는 상태다. 변경 후에는 정적
   직렬화와 컴파일, Unity Play Mode의 오답 재대기·정답 전이·최종 008→009 순서를 구분해 검증하며,
   Quest/OpenXR 실제 입력은 별도 수동 검증으로 남긴다.

### 적용 결과

- `VoiceStep`에 상세교육용 `controllerExpectedInput`, `controllerWrongInputClip`,
  `controllerCorrectInputClip`, `controllerCompletionClip`을 추가했다. 값이 없는 `ContSimp` 단계는 기존
  자동 재생을 유지한다.
- 상세교육은 다음 직렬화 순서로 연결했다.

| 단계 | 설명 후 대기 | 오답 | 정답 | 정답 후 전이 |
|---|---|---|---|---|
| Trigger | 001 → 002 → Trigger | 003 → Trigger 재대기 | 008 | 004 Grip 설명 |
| Grip | 004 → Grip | 005 → Grip 재대기 | 008 | 006 Joystick 설명 |
| Joystick | 006 → Primary2DAxis | 007 → Joystick 재대기 | 008 | 009 → 상세교육 종료 |

- 상세교육 중에는 오른손 Trigger의 기존 Voice Skip 소비자가 같은 입력을 먼저 소비하지 않는다.
  설명·오답·정답 Voice 재생 중 입력은 무시하고, Voice 종료 뒤에만 다음 입력 대기를 연다.
- `AudioManagerEditor`의 Voice 라이브러리 수집 대상에도 세 피드백 필드를 포함했다.
- 사용자 교체 중 002 `.meta`가 임시로 잘린 상태여서 Unity import가 한 번 실패했다. Unity 재import로
  AudioImporter 내용을 복구했고 GUID `959b1649d1361b445aab5d660e5308b1`은 유지했다.
- 컨트롤러 가이드의 작성 Sprite·자식 활성 상태와 사용자가 조정한
  `Teleport_0/PPE_1 Arrival Anchor` 월드 위치 `(-0.477, -0.837, 8.734)`는 변경하지 않았다.

### 완료한 검증

- `PPELocomotionPpeRegressionValidationHarness`가 상세교육 001~009 경로, 세 expected input, 공통 008,
  Joystick 완료 009, 간단교육의 입력 Gate 미사용 및 새 가이드 작성값을 검사하며 PASS했다.
- Unity Play Mode에서 Trigger 오답 003, Grip 오답 005, Joystick 오답 007이 각각 재생된 뒤 같은 입력이
  다시 대기되는 것을 확인했다.
- 각 정답에서 008이 재생됐고, Joystick 정답 008 종료 뒤 17.37초 길이의 009가 실제 Voice Source에서
  재생 중인 상태와 009 종료 후 다음 상태 전이를 확인했다.
- 이름 제출 경로는 계속 `VO_PPE_CTRL_SIMP_001_Start`로 시작하며 입력 대기가 추가되지 않은 것을
  확인했다.
- Runtime/Editor 어셈블리 빌드와 Unity Console 기준 오류는 0개다. 기존 패키지·폐기 API·미사용 필드
  경고는 남아 있으며 이번 변경 파일에서 새 컴파일 경고는 확인되지 않았다.

### 아직 필요한 수동 검증

- Quest/OpenXR 오른손 컨트롤러에서 Trigger·Grip·Joystick 축이 각 단계에서 한 번씩 판정되는지 확인한다.
- 실제 컨트롤러로 오답 Voice 재생 중 연속 입력했을 때 중복 판정되지 않는지 확인한다.
- Quest 양안에서 Trigger·Grip·Exit Marker 가이드 이미지가 해당 입력 대기 동안 정상 표시되는지 확인한다.

## 2026-08-28 PPE 판정 피드백 모드 정책

PPE를 잡고 사용·폐기하는 구역의 How-To, 오답 안내 및 판정 효과는 다음 정책을 단일 기준으로 사용한다.
훈련·테스트의 모드 선택, 이동, 종료 등 일반 흐름 Voice는 `PPE 판정 Voice`와 별도로 유지한다.

| 모드 | 필수 PPE How-To | 잘못된 PPE Voice | PPE 판정 SFX | 빨간 판정 문구·구형 아이콘 |
|---|---:|---:|---:|---:|
| 교육 | 허용 | 허용 | 허용 | 기존 교육 피드백 유지 |
| 훈련 | 금지 | 금지 | 허용 | 숨김 |
| 테스트 | 금지 | 금지 | 금지 | 숨김 |

- How-To는 교육모드이면서 현재 시나리오 작업계획의 필수 PPE이고 `Clean` 상태일 때만 재생한다.
- 2026-08-28에 훈련 이동 음원 `Assets/Audio/Voice/5_TRAIN/4_VO_PPE_TRAIN_001_PPE_MoveToPPE.mp3`가
  같은 경로에서 교체됐다. `.meta` GUID `dc274f57e6440d34da9a32985bdae17c`와 대상 씬의
  `m_TrainingMoveToPpeVoice` 참조가 일치하므로 씬 재연결은 필요하지 않다.
- 기존 씬 계약과 `PPETrainTestModeValidationHarness`의 11개 Clip 참조 검증을 보존하기 위해
  `m_TrainingWrongButtonVoice`의 직렬화 참조는 유지하되 훈련 중에는 재생하지 않는다.
- 테스트모드는 기존 하네스 기준인 `m_TestModeSelectedVoice`, `m_TestMoveToPpeVoice`,
  `m_TestEndVoice`를 모두 유지한다. 무음 대상은 테스트 진행 자체가 아니라 PPE·퀴즈의 정오답 판정
  피드백이다. 발걸음·서명 SFX도 판정 SFX가 아니므로 기존 동작을 유지한다.
- 중도 퇴장 음성이 시작되면 대기 중인 오답 음성과 PPE 조건부 음성 코루틴을 취소하고, 복귀가 끝날
  때까지 새 PPE Voice가 Voice 채널을 교체하지 못하게 한다. 별도 SFX 채널은 중도 퇴장 Voice와
  동시에 사용할 수 있다.
- `PPEVoiceFlowDirector`가 모드 정책을 소유하고 `PPEActionPanelController`가 표시·SFX·오답 Voice
  직전에 해당 정책을 확인한다. 패널의 Inspector 작성값과 씬 UI 배치는 변경하지 않는다.

### 모드별 Voice/SFX 재생 목록

아래 목록은 현재 기준 씬 `Assets/Scenes/4_PPE_Room.unity`에서 학습 모드를 선택한
시점부터 퀴즈 완료 또는 중도 퇴장까지를 범위로 한다. 그 전에 재생되는 Welcome, 이름 입력,
컨트롤러 안내, 카드·시나리오 선택 Voice는 세 모드 공통 선행 흐름이므로 각 모드 목록에 중복해서
넣지 않는다.

| 모드 | 진행 Voice | PPE How-To·오답 Voice | PPE·퀴즈 판정 SFX | 비판정 SFX |
|---|---|---|---|---|
| 교육 | 선택·이동·PPE 시작·태블릿·착용 순서·거울·퀴즈·종료 | 시나리오 필수 PPE에 한해 허용 | 허용 | 발걸음, 태블릿 체크·서명 허용 |
| 훈련 | 선택·이동·태블릿 확인·거울·미완료·퀴즈·종료 | 금지 | 허용 | 발걸음, 태블릿 체크·서명 허용 |
| 테스트 | 선택·이동·종료 | 금지 | 금지 | 발걸음, 태블릿 체크·서명 허용 |

중도 퇴장 `4_VO_PPE_EDU_206_StopScenario.mp3`는 세 모드 공통 Voice다. 시작되면 기존 Voice와
대기 중인 PPE 조건 Voice를 취소하고 복귀가 끝날 때까지 Voice 채널을 독점한다.

#### 교육모드 Voice

| 구간 | 재생 Voice |
|---|---|
| 모드 선택 | `VO_PPE_MODAL_006_EduSelect.mp3` |
| PPE 구역 이동 | `4_VO_PPE_EDU_001_PPE_MoveToPPE.mp3` |
| PPE 구역 도착 | `4_VO_PPE_EDU_002_PPE_Start.mp3` → `4_VO_PPE_EDU_003_Table.mp3` |
| 태블릿 최초 해제 | 방호복 미착용 상태이면 `4_VO_PPE_EDU_004_HazmatSuit.mp3` 1회 |
| 방호복·착용 순서 | 005 사용 안내, 006 방호복 완료, 007 순서 안내, 008 PPE 확인, 009 다음 PPE 안내 |
| 정상 PPE Grab | 현재 시나리오의 필수 PPE만 How-To 101~109 재생. 상세 시나리오 표는 문서의 `19. 시나리오 필수 PPE 전용 How-To 음성`을 따른다. |
| 상태·오답·선행 조건 | 오염 송기마스크 상태 확인, 오염 PPE 사용, 정상 PPE 폐기, 방호복·내부장갑·장화·테이프 선행 조건 위반 등에 연결된 교육 Voice |
| 필수 PPE 완료 | `4_VO_PPE_EDU_011_MoveToMirror.ogg` |
| 거울 확인 | `4_VO_PPE_EDU_012_MirrorCheckPPE.ogg` |
| 미완료 | PPE 미완료 `4_VO_PPE_EDU_013_UnEnoughPpe.ogg`, 태블릿 미완료 `4_VO_PPE_EDU_016_CheckTablet.mp3`; 둘 다 미완료이면 순차 재생 |
| 퀴즈·종료 | `4_VO_PPE_EDU_014_Quiz.ogg` → 퀴즈 완료 후 `4_VO_PPE_EDU_015_EduEnd.ogg` |

#### 훈련모드 Voice

| 순서 | 재생 조건 | 재생 Voice |
|---:|---|---|
| 1 | 훈련모드 선택 | `4_VO_PPE_MODAL_002_TrainSelect.mp3` |
| 2 | PPE 구역 이동 | `4_VO_PPE_TRAIN_001_PPE_MoveToPPE.mp3` |
| 3 | PPE 구역 도착 | `4_VO_PPE_TRAIN_002_CheckPPE_Tablet.mp3` |
| 4 | 시나리오 필수 PPE 전체 착용 | `4_VO_PPE_TRAIN_004_MirrorCheckPPE.mp3` |
| 5 | 거울 최종 확인 시 PPE 또는 태블릿 미완료 | `4_VO_PPE_TRAIN_005_UnEnoughPpeTablet.mp3` |
| 6 | 퀴즈 시작 | `4_VO_PPE_TRAIN_006_Quiz.mp3` |
| 7 | 퀴즈 완료 | `4_VO_PPE_TRAIN_007_TrainEnd.mp3` |

`4_VO_PPE_TRAIN_003_WrongButton.mp3`는 씬 직렬화와 필수 Clip 검사에는 남아 있지만 현재 재생 호출은
없다. 훈련 PPE 구역에서는 How-To와 잘못된 PPE Voice를 재생하지 않는다.

#### 테스트모드 Voice

| 순서 | 재생 조건 | 재생 Voice |
|---:|---|---|
| 1 | 테스트모드 선택 | `4_VO_PPE_MODAL_003_TestSelect.mp3` |
| 2 | PPE 구역 이동 | `4_VO_PPE_TEST_001_PPE_MoveToPPE.mp3` |
| 3 | 퀴즈 완료·테스트 종료 | `4_VO_PPE_TEST_002_TestEnd.mp3` |

테스트에서는 PPE 구역 도착, How-To, 잘못된 PPE, 필수 PPE 완료, 미완료와 퀴즈 시작 Voice를
추가 재생하지 않는다. 테스트 시작과 이동, 종료 Voice는 반드시 유지한다.

#### 교육·훈련 PPE SFX

교육과 훈련은 다음 PPE SFX를 공통으로 허용한다. 선택한 시나리오에서 실제 사용하는 PPE에
해당하는 효과음만 발생한다.

| SFX ID/파일 | 발생 조건 |
|---|---|
| `cloth` / `Cloth.wav` | 방호복 사용 승인 |
| `Gloves` / `Gloves.ogg` | 외부 화학장갑 사용 승인 |
| `Nitril InnerGlove` / `Nitril InnerGlove.ogg` | 내부 니트릴 장갑 사용 승인 |
| `Boots` / `Boots.ogg` | 장화 사용 승인 |
| `Helmet` / `Helmet.ogg` | 안전모 사용 승인 |
| `harness` / `Harness.ogg` | 안전대·등판·송기장비 계열 사용 승인 |
| `Wearing Mask Glass Shield` / `Wearing Mask Glass Shield.ogg` | 정상 송기마스크·화학보안경·안면보호대 사용 승인 |
| `Taping` / `Taping.mp3` | 테이프 사용 승인 |
| `mask_breathing_right` / `mask_breathing_right.ogg` | 정상 마스크 호흡 검사 |
| `mask_breathing_wrong` / `mask_breathing_wrong.ogg` | 오염 마스크 호흡 검사 |
| `Correct Answer` / `Correct Answer.ogg` | 정상 사용, 오염 PPE 정상 폐기 또는 검사 완료 |
| `Wrong Answer` / `Wrong Answer.mp3` | 오염 PPE 사용, 정상 PPE 폐기, 검사 전 사용 등 잘못된 선택 |

정상 PPE 사용 승인에서는 장비별 착용 SFX와 `Correct Answer`가 같은 판정에서 함께 재생될 수 있다.
`Wrong Answer.mp3`는 “잘못된 PPE입니다” Voice가 아니라 SFX이므로 훈련모드에서도 유지된다.

#### 퀴즈·이동·태블릿 SFX

| 구간 | 교육 | 훈련 | 테스트 |
|---|---:|---:|---:|
| 이동 입력 `Foot Step.ogg` | 재생 | 재생 | 재생 |
| 태블릿 체크 `check.ogg` | 재생 | 재생 | 재생 |
| 플레이어 서명 `sign.ogg` | 재생 | 재생 | 재생 |
| 퀴즈 정답 `Correct Answer.ogg` | 재생 | 재생 | 재생 안 함 |
| 퀴즈 오답 `Wrong Answer.mp3` | 재생 | 재생 | 재생 안 함 |

Voice는 `AudioManager`의 단일 Voice 채널을 사용하므로 한 번에 하나만 재생한다. SFX는 별도 채널이어서
Voice와 동시에 재생할 수 있다. 다만 중도 퇴장 Voice가 채널을 독점하는 동안에는
`PPEActionPanelController`가 새 PPE 판정 SFX를 시작하지 않는다.

### 2026-08-28 시나리오 불일치 PPE의 205 우선 재생

#### 변경 전 필수 질문

1. Inspector와 씬의 AudioClip 참조는 변경하지 않고 기존 `m_WorkPlanMismatchVoice`의
   `4_VO_PPE_EDU_205_PPE_forScenario.mp3` 연결을 그대로 사용한다.
2. 작업계획과 판정 순서의 단일 소유자는 `PPEVoiceFlowDirector.ActiveWorkPlan`과
   `PPEVoiceFlowDirector`다.
3. 입력 경로는 `PPE Grab → Use → PPEActionPanelController.ResolveUseChoice() →
   RejectUseBeforeConditionCheck() → 작업계획 적합성 → 상태·선행 조건 → 최종 승인`이다.
4. 누락 Clip이나 참조를 런타임에서 자동 생성·수리하지 않는다.
5. 교육 Voice와 훈련 PPE SFX, 테스트 오답 기록에 영향을 주며 Grab, Discard, 장착 시각,
   텔레포트와 퀴즈는 변경하지 않는다.
6. 변경 전 기준은 시나리오 불일치 오염 PPE 사용 시 상태 판정이 먼저 실행되어 EDU 202가
   재생되는 것이고, 변경 후 기준은 하자 상태와 무관하게 작업계획 판정이 먼저 EDU 205를 선택하는 것이다.
7. 정적 순서·Clip 참조와 C# 컴파일을 확인하고 Unity Play Mode 및 Quest 실음성은 별도로 검증한다.

#### 확정 판정 우선순위

PPE `사용` 선택에서는 장비의 하자 여부보다 현재 시나리오 필요 여부를 먼저 판정한다.

| 현재 시나리오 필요 여부 | PPE 상태 | 교육 | 훈련 | 테스트 |
|---|---|---|---|---|
| 불필요 | 정상 또는 하자 | `Wrong Answer` SFX + EDU 205 | `Wrong Answer` SFX, Voice 없음 | SFX·Voice 없음, 오답 기록 |
| 필요 | 하자 | `Wrong Answer` SFX + EDU 202 | `Wrong Answer` SFX, Voice 없음 | SFX·Voice 없음, 오답 기록 |
| 필요 | 정상 | 기존 선행 조건을 통과하면 착용 SFX + `Correct Answer` | 기존 착용 SFX + `Correct Answer` | PPE 판정 SFX·Voice 없음 |

따라서 누출 대응에서 안전대처럼 현재 작업계획에 포함되지 않은 PPE를 사용하면, 그 PPE가 오염
상태이더라도 하자 PPE 사용 음성 EDU 202가 아니라 “시나리오에 필요한 PPE를 착용”하는 EDU 205를
우선 재생한다. How-To와 잘못된 PPE Voice는 교육 전용이라는 기존 모드 정책은 유지한다.

#### 구현·검증 기준

- `ResolveUseChoice()`가 상태 판정 전에 `RejectUseBeforeConditionCheck()`를 호출하는 기존 구조를
  유지하고, 이 선행 판정 안에 작업계획 적합성 검사를 배치했다.
- 상태 판정 뒤의 `CanApprovePpeUse()`에서는 중복 작업계획 검사를 제거해 판정 소유자를 하나로 만들었다.
- 회귀 하네스는 205 Clip 이름, 작업계획 판정이 상태 판정보다 앞서는 호출 순서와 선행 분기에서
  `m_WorkPlanMismatchVoice`를 사용하는지를 검사한다.
- Play Mode에서는 밀폐공간의 화학보안경·안면보호대와 누출 대응의 안전대를 각각 정상/하자 상태로
  사용해 교육은 모두 205, 훈련은 SFX만, 테스트는 판정 음향 없이 오답 기록으로 처리되는지 확인한다.

### 변경 판단 기준과 검증 범위

1. Inspector·씬 작성값은 보존하며 런타임에서 UI 위치·색·크기를 덮어쓰지 않는다.
2. Voice 단일 채널은 `AudioManager`, 모드 정책은 `PPEVoiceFlowDirector`, PPE 판정 표시는
   `PPEActionPanelController`가 소유한다.
3. PPE 입력 경로는 `Grab/Use/Discard → PPEActionPanelController → 판정 표시·SFX →
   PPEVoiceFlowDirector`이며, Exit는 `Exit Collider/Relay → PPEFinaleController →
   PPEVoiceFlowDirector → AudioManager`이다.
4. 누락 참조의 런타임 자동 생성·수리는 추가하지 않는다.
5. PPE Grab, 패널 표시, Voice와 SFX 소비자만 변경하며 텔레포트 입력·퀴즈 판정·장착 시각은 보존한다.
6. 변경 전 재현 증상과 변경 후 모드별 정책 하네스를 비교 기준으로 사용한다.
7. Runtime/Editor C# 정적 빌드는 통과했으며 Unity Play Mode와 Quest/OpenXR 실음성 검증은 별도이다.
