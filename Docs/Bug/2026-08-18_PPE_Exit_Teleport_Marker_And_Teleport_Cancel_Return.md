# PPE 텔레포트 진행 불가와 종료 마커(_3 Exit) 오설정

Date: 2026-08-18

대상 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`

## 증상

- 모드 선택 화면을 넘긴 뒤 조이스틱으로 텔레포트하면 곧바로 모드 선택 모달로 되돌아오고, 이후 이동이 잠긴다.
- `Markers/Teleport_3_Exit`를 조준해도 아무 일도 일어나지 않는다.
- 의도한 동작은 `_0~_2`(PPE 착용 루트)에서 `_3`으로 쏘면 교육이 종료되고 시작 지점으로 복귀하는 것이다.

## 원인

### 1. 조이스틱 릴리즈가 "취소"로 해석되어 세션이 종료됨

`PPEControllerTeleportModeManager`는 텔레포트 모드 액션의 `canceled`에 `OnCancelTeleport`를 연결한다. 이 콜백은 취소 버튼이 아니라 **조이스틱을 놓는 순간**, 즉 텔레포트가 확정되는 시점에 발생한다. `_0~_2`의 `TeleportationAnchor`도 `m_TeleportTrigger: 0`(OnSelectExited)이라 같은 릴리즈에서 이동을 실행한다.

작업 트리에만 존재하던 `PPEVoiceFlowDirector.NotifyTeleportCanceled` 변경분이 이 이벤트에 `ShowModeChoicesAfterCompletionReturn()`을 연결해 두었다. 조건도 `ShouldReturnToModeSelectionOnTeleportCancel()`로 넓혀 `EducationSelected`, `PpeEducationSelected`, `TeleportInstruction`, `PpeArea`, `TrainingSelected`, `TestSelected` 여섯 상태 전부를 포함했다. 결과적으로 정상 텔레포트를 할 때마다 모드 선택 모달이 다시 뜨고, 그 안의 `SetVoiceMovementGate(false)`가 텔레포트 인터랙터를 비활성화해 이동이 잠겼다.

### 2. `_3 Exit`가 조이스틱 레이에 물리적으로 닿지 않음

두 가지가 겹쳐 있었다.

- **물리 레이어**: 텔레포트 인터랙터의 `m_RaycastMask.m_Bits`는 `2147483648`(비트 31 단독)이고, 레이어 31은 `Teleport Target`이다. `_0~_2`의 마커는 레이어 31인데 `Teleport_3_Exit`만 레이어 0(Default)이었다.
- **컴포넌트 종류**: `_0~_2`는 XRI `TeleportationAnchor`를 쓰는데 `Teleport_3_Exit`만 프로젝트 자체 `XRLocationTeleportTarget`을 쓴다. 이 컴포넌트는 `OnActivated`(Activate 액션)에서만 텔레포트하므로 조이스틱 텔레포트에는 대응하는 입력이 없다. 또한 `BaseTeleportationInteractable`이 아니라서 `PPEVoiceTeleportEventRelay`의 구독 대상에서도 빠진다.

씬의 `XRLocationTeleportTarget`은 7개이며 `Teleport_3_Exit`만 활성이고, `Markers/XR Location Marker_big (2)~(7)`은 비활성 잔재다.

### 3. 종료 판정이 근접 감지에 의존

`PPEFinaleController.CheckForEarlyStartReturn()`이 매 프레임 `PPE Lesson Start Return Anchor`(월드 `(0, 0, 0)`) 반경 0.8 m를 검사해, 한 번 벗어났다가 다시 들어오면 세션을 종료했다. `Teleport_3_Exit`가 월드 `(0, -0.85, 0.024)`로 같은 자리에 있어 "`_3`으로 이동 → 근접 감지 → 복귀"가 되도록 설계돼 있었다. 그러나 `_3`이 동작하지 않는 상태에서는 이 근접 감지가 유일한 종료 수단이었고, 룸스케일 이동이나 다른 경로로 원점 부근에 들어가도 교육이 종료되는 오작동 위험이 있었다.

## 적용한 변경

### 코드

- `Assets/Scripts/PPEVoiceFlowDirector.cs`
  - `NotifyTeleportCanceled`를 커밋된 원본 동작(상태를 바꾸지 않는 no-op)으로 되돌리고 `ShouldReturnToModeSelectionOnTeleportCancel()`을 제거했다.
  - `m_ExitTeleportMarker` 직렬화 필드를 추가하고, `ApplyPresentation`에서 `FlowState.PpeArea`일 때만 활성화한다. `PpeArea`는 라우트 마커 텔레포트가 실제로 착지해야 진입하는 상태이므로, 시작 시점과 모드 선택 복귀 후에는 `_3`이 꺼진다.
- `Assets/Scripts/PPEFinaleController.cs`
  - `CheckForEarlyStartReturn()`, `NotifyEarlyStartReturnArrived()`, `m_HasLeftStartReturnZone`, `m_StartReturnTriggerRadius`를 제거했다. 시작 지점 근처에 서 있는 것만으로는 세션이 끝나지 않는다.
  - 외부에서 호출하는 `RequestExitReturn()`을 공개했다. `m_ReturnInProgress`와 `IsActivePpeModeSession`으로 중복 실행을 막고 기존 `ReturnToModeChoices()`를 그대로 사용한다.
  - 정상 완료 복귀(`CompleteRoutine`)와 테스트 모드 뒤로가기(`NotifyCompletionBackRequested`) 경로는 변경하지 않았다.
- `Assets/Scripts/PPEExitTeleportMarkerRelay.cs` (신규)
  - `_3` 마커의 `teleporting` 이벤트를 `PPEFinaleController.RequestExitReturn()`으로 연결한다. 참조는 Inspector 작성값이며, 없으면 한 번만 오류를 남기고 멈춘다.

### Editor 도구

- `Assets/Editor/PPEExitTeleportMarkerConversion.cs` (신규)
  - `Tools > PPE > Report Exit Teleport Marker Wiring`: 현재 배선만 출력한다.
  - `Tools > PPE > Convert Exit Teleport Marker To Teleportation Anchor`: `Teleport_3_Exit`의 레이어를 `Teleport Target`으로 바꾸고, `XRLocationTeleportTarget`을 제거한 뒤 라우트 마커(`XR Location Marker_big_PPE*`)의 `TeleportationAnchor` 값을 복사해 부착한다. 템플릿은 읽기만 하므로 `_0~_2`는 변경되지 않는다.
  - 새 앵커의 `m_TeleportAnchorTransform`은 `PPEFinaleController.m_ReturnDestination`(= `PPE Lesson Start Return Anchor`)로 지정한다. `_3`의 자체 Transform은 X축으로 약 88.5° 눕혀져 있어 앵커로 쓸 수 없다.
  - `PPEExitTeleportMarkerRelay`를 부착하고 참조를 채우며, `PPEVoiceFlowDirector.m_ExitTeleportMarker`가 비어 있을 때만 채운다. 이미 작성된 값은 유지하고 로그로 알린다.

## 설계 결정

`_3`은 다른 장소로 가는 이동이 아니라 종료·복귀에 텔레포트 UX를 씌운 것이다. 이동 자체를 없애는 방식(선택 시 종료 시퀀스만 호출)도 검토했으나, 텔레포트 레이의 레티클·유효 판정이 라우트 마커와 동일하게 동작하는 것이 검증 부담이 적다고 판단해 `TeleportationAnchor`를 유지했다. 도착 지점이 복귀 지점 그 자체이므로 불필요한 이중 이동은 없고, 페이드가 착지 직후에 시작된다는 차이만 남는다.

## 영향 범위

- `_0~_2` 라우트 마커의 컴포넌트, 레이어, 도착 앵커는 변경하지 않았다.
- `PPEVoiceTeleportEventRelay`는 변환 후 `_3`도 `BaseTeleportationInteractable`로 구독하게 되지만, `_3`은 `m_CenterMarker`/`m_MirrorMarker`가 아니고 `NotifyTeleportArrived()`는 `PpeArea`에서 무시되므로 부작용이 없다.
- `PPEFinaleController`에서 제거한 `m_StartReturnTriggerRadius`는 씬 YAML에 잔여 값으로 남지만 Unity가 로드 시 폐기한다.

## 완료한 검증

- 정적 확인: `dotnet build Assembly-CSharp.csproj` 오류 0개, `dotnet build Assembly-CSharp-Editor.csproj` 오류 0개.
- 정적 확인: `git diff -- Assets/Scripts/PPEVoiceFlowDirector.cs`가 되돌리기 직후 빈 diff임을 확인해 원본 동작 복원을 검증했다.
- 정적 확인: 씬 YAML에서 텔레포트 인터랙터의 `m_RaycastMask` 비트, 라우트 마커의 레이어 31, `_3`의 레이어 0, `TeleportationAnchor` 3개와 `XRLocationTeleportTarget` 7개의 분포를 대조했다.

## 아직 필요한 검증

- [ ] Unity에서 `Tools > PPE > Report Exit Teleport Marker Wiring`으로 참조가 모두 해석되는지 확인한다.
- [ ] `Tools > PPE > Convert Exit Teleport Marker To Teleportation Anchor`를 실행하고 씬을 저장한다.
- [ ] `PPEVoiceFlowDirector`의 `m_ExitTeleportMarker`에 `Teleport_3_Exit`가 채워졌는지 Inspector에서 확인한다.
- [ ] Play Mode에서 모드 선택 후 `_0~_2` 텔레포트가 정상 동작하고 모달로 되돌아오지 않는지 확인한다.
- [ ] 시작 시점과 모드 선택 복귀 후 `_3`이 비활성이고, `_0~_2` 도착 후 활성화되는지 확인한다.
- [ ] `_3`을 쏘면 페이드 후 시작 지점으로 복귀하고 모드 선택 모달이 뜨는지 확인한다.
- [ ] 시작 지점 근처를 걸어다녀도 세션이 종료되지 않는지 확인한다.
- [ ] Quest/OpenXR 양안에서 `_3` 마커의 레이 조준과 레티클이 `_0~_2`와 동일하게 보이는지 확인한다.

## 2026-08-26 `3_PPE_Room_3mode_loco` 걸어서 진입 후속

### 변경된 요구사항

현재 로코모션 씬에서는 Exit를 Ray로 선택하는 것뿐 아니라 활성 PPE 세션 중 작성된 Exit 영역으로
걸어 들어와도 중도 복귀해야 한다. 시작 지점 근접이나 일반 조이스틱 릴리즈는 종료 조건이 아니다.

### 적용한 변경

- `PPEExitTeleportMarkerRelay`에 작성값 `m_ReturnOnWalkEnter`를 추가했다.
- 대상 씬에서는 이 값을 활성화하고, `m_Player`를 활성 `XR Origin (VR)`에,
  `m_EnterVolume`을 기존 Exit Collider에 연결했다.
- 매 프레임 플레이어의 XZ 위치가 작성된 Exit Bounds 안에 있는지만 검사하며, 조건을 만족하면
  기존 `PPEFinaleController.RequestExitReturn()`을 호출한다.
- 복귀 중복 실행 방지, 중도 종료 음성, 페이드, 원위치 이동, 세션 초기화 및 모드 선택 모달 표시는
  기존 단일 `ReturnToModeChoices()` 경로를 유지한다.

### 영향 범위와 검증 상태

- Exit Collider의 Transform·크기, PPE Grab, 일반 이동 입력 및 시작 지점 판정은 변경하지 않았다.
- 정적 확인: 단일 Relay, `m_ReturnOnWalkEnter=true`, Player·EnterVolume·Finale 참조를 확인했다.
- `PPELocomotionPpeRegressionValidationHarness`가 이 작성 참조와 걸어서 진입 모드를 검사한다.
- Unity Play Mode와 Quest/OpenXR에서는 실제 진입 시 중도 음성, 페이드, 시작 위치 복귀, 모드 선택
  모달 표시 순서를 아직 확인해야 한다.

## 2026-08-28 중도 퇴장 Voice 채널 독점

### 근본 원인

`AudioManager.PlayVoice()` 자체는 기존 Voice를 정지한 뒤 하나의 Voice Source에서 재생하지만,
중도 퇴장 시 진행 중인 모드·PPE 조건부 코루틴과 지연 오답 Voice 생산자를 모두 취소하지 않았다.
따라서 퇴장 Voice가 시작된 뒤 다른 생산자가 Voice Source를 다시 교체할 수 있었다.

### 적용한 변경

- 중도 퇴장 진입 시 모드 선택·PPE 조건부·지연 오답 Voice 생산자를 취소한다.
- 복귀가 완료될 때까지 새 PPE Voice 재생을 차단하는 독점 상태를 유지한다.
- Voice만 정지·교체하고 별도 SFX Source는 정지하지 않아 `SFX + Voice` 동시 재생 규칙을 보존한다.
- 새 모드 세션이 시작될 때 독점 상태를 초기화한다.

### 검증

- `PPELocomotionPpeRegressionValidationHarness`가 중도 퇴장 독점 상태에서 새 PPE SFX/Voice 요청이
  차단되고, `StopFlowPlayback(stopSfx: false)` 경로가 사용되는지 검사한다.
- Runtime과 Editor C# 빌드는 오류 0개로 통과했다.
- Unity Play Mode에서 일반 Voice, How-To, 지연 오답 Voice 각각의 재생 도중 Exit에 들어가 중도 퇴장
  Voice 하나만 끝까지 유지되는지 확인해야 한다. Quest/OpenXR 실제 오디오 검증도 아직 필요하다.
