# 2026-08-08 PPE HandTest XR UI·렌더링 회귀 보고

## 대상

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 실행 환경: Unity 6000.4.8f1, OpenXR, Meta Quest Link
- 보고 일자: 2026-08-08

## 증상

1. 시작 직후 키보드 및 일부 안내 UI가 잠시 보였다가 사라진다.
2. 좌측 상단에 배치한 `ControllerGuide_mini`가 조이스틱 클릭 시 보이지 않는다.
3. `Window Canvas/Place`가 교육 시작 안내로 보인 뒤 즉시 사라져, 약 10초 노출 후 페이드 아웃이 필요하다.
4. 텔레포트 도착 음원이 같은 도착 지점을 다시 지날 때 반복 재생된다.
5. PPE를 선택 해제했을 때 원래 작성 위치로 돌아가지 않거나, 헬멧이 공중에 남는 경우가 있다.
6. 테이프 사용 조건이 장갑·장화 양쪽 전체 장착을 모두 요구한다. 방호복을 입고 해당 쪽 장갑 또는 장화를 착용한 경우에는 그 쪽 테이프만 표시되어야 한다.
7. PPE 착용 중 HMD 화면이 깨지거나 멈추는 현상이 간헐적으로 발생한다. 관찰 게이지도 직사각형이며 시작 시 가득 차 보인다.
8. 교육 선택 모달 버튼의 hover 상태가 눈에 띄지 않는다.
9. 문 뒤 개구부가 벽처럼 보이며, 거울은 성능 조정 후 검게 보인 적이 있다.
10. Game View와 Scene View에서도 문 개구부가 막힌 것으로 보인다.
11. 방호복 외 PPE의 action panel이 장비마다 다른 높이·방향에 표시되거나 PPE mesh 내부에 꽂힌다. 의도는 방호복 패널과 같이 대상 PPE의 정면 중앙에서 약간 위에 떠 있는 표시이다.
12. PPE action panel에서 선택 결과를 알려 주는 성공·오류 아이콘이 노출되지 않는다. 퀴즈 화면은 전날 정상 동작을 확인했으므로, 퀴즈 UI가 아닌 PPE panel 피드백 표시 범위로 한정한다.

## 확인된 원인 및 가설

| 항목 | 판단 | 근거 |
| --- | --- | --- |
| 시작 UI 노출 | 확인됨 | `Modal Keyboard Canvas`가 씬 시작 시 활성이고, `PPEVoiceFlowDirector`가 첫 프레임 이후에 숨기고 있었다. |
| Place 즉시 숨김 | 확인됨 | 기존에는 상태 전환만으로 표시 여부를 제어했고, 노출 시간·알파 전환이 없었다. |
| 텔레포트 음원 반복 | 확인됨 | 도착 relay가 같은 interactable의 재도착을 구분하지 않았고, center/mirror 음성에 1회 재생 보호가 없었다. |
| 테이프 전체 조건 | 확인됨 | 사용 가능 조건이 방호복과 좌·우 장갑, 좌·우 장화를 모두 요구하는 AND 조건이었다. |
| 게이지 모양·초기 표시 | 확인됨 | Fill Image가 Horizontal 방식의 직사각형이었고, 배경이 가득 찬 바 형태라 완료 상태처럼 보일 수 있었다. 코드상 Fill은 이미 0에서 5초 동안 1로 증가한다. |
| 컨트롤러 미니 가이드 | 미확정 | 코드상 `Primary2DAxisClick` 입력 경로와 활성화 참조는 있다. 단, Quest에서 실제 클릭 이벤트가 들어왔는지와 authored 위치에서 가려졌는지는 런타임 증거가 없다. |
| PPE 원위치 복귀 | 미확정 | `selectExited` 뒤 지연 복귀가 있었으나 XRI 선택 상태 갱신 순서와 비활성화 상황에서 복귀 요청이 유실될 가능성이 있다. |
| HMD 멈춤 | 가설 | Planar Mirror가 HMD Game 카메라에 대해 고해상도 반사 카메라를 매 프레임 추가 렌더한다. PPE 착용 애니메이션·hand crossfade와 동시에 GPU/CPU spike가 날 수 있으나 Quest Profiler로 확정하지 못했다. |
| 문 개구부가 벽처럼 보임 | 미확정 | 디스크 씬에는 `PPE Front Wall Octagonal Glass Hole` 및 문 개구부 mesh가 존재한다. 투명 유리/겹침/런타임 표시를 실제 HMD에서 확인해야 한다. |
| 거울 검은 화면 | 미확정 | `maxSourceDistance` 제한 등을 적용한 직후 관찰되었으며 해당 성능 설정은 즉시 원복했다. 재시작 Play Mode와 Quest에서 원복 결과를 확인해야 한다. |
| 개구부가 Game/Scene View에서 막힘 | 미확정 | `Door Window Glass` renderer를 비활성화했지만, 두 View에서 막혀 보인다는 새 관찰이 있다. 앞벽·문 mesh·투명도·depth/겹침을 live View 기준으로 분리 확인해야 한다. |
| 방호복 외 action panel의 불일치 | 확인됨 | `PPEActionPanelController.ApplyPanelPose()`는 방호복처럼 `panelRoot` 자체를 pose로 쓰는 경우만 authored transform을 유지한다. 그 외 장비는 Bounds 최상단과 viewer-facing 회전을 매 프레임 계산하므로 장비별 Bounds·pivot 차이에 따라 위치와 방향이 달라진다. |
| PPE panel 피드백 아이콘 미표시 | 조사 중 | 코드상 `Use/Discard/Inspect` 결과가 성공·오류 icon root를 활성화한다. 씬 참조, icon root의 Graphic/Image·CanvasGroup·부모 활성 상태, 결과 이벤트 도달 여부를 Unity Play Mode에서 분리 확인해야 한다. 퀴즈 아이콘 문제와 혼합하지 않는다. |

## 적용한 변경

- 시작 노출 방지
  - `Modal Keyboard Canvas`와 기본 controller guide를 시작 비활성 상태로 설정했다.
  - 이름 입력 상태에서만 키보드 UI가 상태 흐름에 따라 표시된다.

- 컨트롤러 미니 가이드
  - 사용자가 지정한 좌측 상단 authored 위치는 보존했다.
  - Generic XR controller binding 외에 Oculus Touch의 `primary2DAxisClick` binding을 보조로 추가했다.
  - 시작 상태는 숨김이며 클릭으로 토글된다.

- Place 표시
  - `PPE Voice Flow`에 `CanvasGroup` 기반의 표시 제어를 연결했다.
  - Welcome 시작 후 10초를 유지하고, Inspector 작성값 `0.75`초로 페이드 아웃한다.

- 텔레포트 도착 음성
  - 같은 도착 interactable은 활성 훈련 흐름 동안 한 번만 relay한다.
  - center marker와 mirror marker의 음성에도 각각 1회 재생 보호를 추가했다.

- PPE 원위치 복귀
  - 선택 해제 시 authored parent/local pose 복귀 요청이 XRI의 end-of-frame 갱신 또는 일시 비활성화에 의해 취소되지 않도록 보완했다.
  - 기존 Toggle 상호작용은 유지했다. 따라서 선택 해제는 실제 `selectExited`가 발생하는 동작(예: 두 번째 grip 토글)에 대응한다.

- 테이프
  - 사용 가능 조건을 `방호복 착용 + 장갑 또는 장화 중 하나 이상 장착`으로 변경했다.
  - `hazmat_suit_on_10` 아래의 `taped_hand_R1`, `taped_hand_L1`, `taped_boot_R1`, `taped_boot_L1`을 각 장비 상태에 연결했다.
  - 한쪽 장갑/장화만 사용됐으면 그 쪽 모델만 표시하도록 확장했다.

- 관찰 게이지
  - 정사각형 150×150 ring sprite와 `Radial360` Fill로 변경했다.
  - Fill 시작값은 0이며 기존 `PPEFinaleController`의 5초 0→100% 진행 로직을 유지했다.

- 모달 hover
  - 두 교육 선택 버튼의 Highlighted/Pressed/Selected ColorTint 대비를 높였다.

- 문 및 거울
  - 문 중심의 `Door Window Glass` renderer를 비활성화하여 개구부를 가리지 않도록 했다.
  - 성능을 위해 시도했던 거울 갱신 주기·거리 제한은 검은 화면 관찰 뒤 원복했다. 픽셀화 유발 설정은 유지하지 않았다.

- action panel 위치
  - 방호복 외 모든 action panel의 동적 Bounds 상단·viewer-facing 배치를 제거했다.
  - 공용 `Hazmat Action Panel`은 선택한 PPE의 scene-authored `Action Panel Pose`에 자식으로 붙고, pose의 local 원점·정면 회전을 사용한다.
  - 선택 종료 시 공용 패널은 원래 방호복 아래의 authored parent·sibling·local pose로 복귀한다.

## 영향 범위

- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- `Assets/Scripts/ControllerGuideMiniActivator.cs`
- `Assets/Scripts/PPEEquipmentVisualController.cs`
- `Assets/Scripts/PPEMarkerToggleGrab.cs`
- `Assets/Scripts/PPEVoiceFlowDirector.cs`
- `Assets/Scripts/PPEVoiceTeleportEventRelay.cs`
- `Assets/Generated/PPE/PPE_ObservationGaugeRing.png`

기존 씬에 있던 사용자 작성 PPE 모델·테이프 계층 및 `m_StartAtQuizForTesting` 변경은 이 작업의 변경 범위에 포함하지 않는다.

## 완료한 검증

- 정적 확인
  - 씬 참조, UI 활성 상태, Place `CanvasGroup`, 테이프 4개 모델 참조, 게이지 Fill 방식, 텔레포트 및 음성 보호 경로를 확인했다.
  - `ControllerGuide_mini`의 authored 위치는 `anchoredPosition=(-1633, 554)`, local Z `4252`로 유지됨을 확인했다.
  - 문 앞벽과 Door Image의 개구부 mesh가 씬에 존재함을 확인했다.
  - `dotnet build Assembly-CSharp.csproj --no-restore -v:q`: 오류 0개. 기존 경고 11개.
  - `git diff --check`: 통과.

- Unity Editor 확인
  - Unity Editor의 현재 열린 대상 씬에 UI, 게이지, 문 renderer 설정을 저장했다.
  - 코드 컴파일과 씬 저장은 확인했으나, 아래 기능의 Play Mode/Quest 실제 동작은 아직 완료하지 않았다.

## 남은 수동 검증

1. Play Mode를 완전히 재시작한 뒤 거울이 검지 않고 양안에 정상 표시되는지 확인한다.
2. 시작 직후 키보드·기본 안내 UI가 한 프레임도 노출되지 않는지 확인한다.
3. Place가 정확히 약 10초 유지된 뒤 0.75초 동안 페이드 아웃하는지 확인한다.
4. Quest 양쪽 컨트롤러의 조이스틱 클릭이 `ControllerGuide_mini`를 지정된 좌측 상단 위치에서 토글하는지 확인한다.
5. 동일 텔레포트 도착 지점을 반복 통과해도 첫 도착 음성만 한 번 재생되는지 확인한다.
6. 각 PPE를 선택한 뒤 실제 선택 해제하여 authored 위치로 돌아오는지, 특히 헬멧이 공중에 남지 않는지 확인한다.
7. 방호복 착용 후 장갑/장화 한쪽씩 조합하여 해당 쪽 테이프만 표시되는지 확인한다.
8. 관찰 게이지가 원형으로 0%에서 5초 동안 100%까지 진행하는지 확인한다.
9. HMD에서 문 개구부가 투명하게 보이는지 확인한다.
10. Game View와 Scene View 모두에서 문 개구부를 가리는 renderer/material/depth 겹침이 없는지 확인한다.
11. 방호복 외 모든 PPE를 잡아 action panel이 정면 중앙·PPE 위쪽에 일관되게 표시되고, PPE를 움직이거나 회전할 때 함께 따라가는지 확인한다.
12. HMD 멈춤은 Quest Profiler로 거울 Off/On 조건을 비교해 mirror reflection render spike 여부를 확인한다.
13. PPE panel에서 Use/Discard/Inspect 결과마다 대응하는 성공 또는 오류 icon이 1개만 노출되는지 확인한다. 퀴즈 화면은 이 항목의 판정 범위에서 제외한다.

## 판정 기준

이 보고서는 정적 확인과 Unity Editor 저장까지의 기록이다. Quest/OpenXR 입력, 양안 렌더링, 오디오 및 성능은 위 수동 검증을 완료하기 전까지 정상으로 판정하지 않는다.

## 2026-08-09 후속: Play 시작 직후 HMD 주변 시야 깨짐·정지와 Meta Runtime IPC 실패

### 증상

- Quest Link로 진입한 뒤 Unity Play Mode를 시작하면 처음부터 HMD 주변 시야가 깨지고 화면이 멈췄다.
- 같은 시점에 Unity Game View도 검은 화면이 되었으며, Unity Editor 내부 진단 명령이 응답하지 않았다.
- Meta Quest Link 대기 공간(보라색 격자/공간) 진입 자체와 Unity/OpenXR Play 시작 실패는 구분한다. 전자는 Link 대기 상태일 수 있으나, 후자는 이 항목의 실패 조건이다.

### 확인된 근거

- `%LOCALAPPDATA%\\Unity\\Editor\\Editor.log`에 Unity 프로세스와 Meta `OVRServer_x64` 사이의 다음 반복 오류가 기록됐다.

  ```text
  [RIPC_CONNECT_LOG] RipcLocalProxyServerMgr::EnableProxyServer FAILED HandInputDataServer
  TryToGetIPCResourcesFromServer FAILED
  CallServerRPC FAILED: EnableRemoteServer. Error Code: 1
  ```

- 오류 시점의 런타임 프로세스는 `OVRService` 실행 중, `OVRServer_x64` 실행 중이었지만 HandInput/RIPC 서버 연결이 반복 실패했다.
- 이 상태에서는 Unity/OpenXR 초기화가 정상 렌더 프레임까지 진행하지 못할 수 있다. 이번 증상은 패널 RectTransform이나 하자 헬멧의 Rigidbody가 직접 만든 오류라는 근거는 아직 없다.

### 적용한 복구 조치

- 관리자 권한으로 `OVRService`를 재시작했다.
- 재시작 전 `OVRServer_x64` PID는 `23908`이었고, 재시작 후 새 PID `8108`과 새 Meta Client 프로세스가 확인됐다. 즉 Meta 런타임 프로세스 재기동은 완료됐다.
- 이미 멈춰 있던 Unity Play Mode는 런타임 재시작 뒤에도 내부 명령에 응답하지 않았다. 저장하지 않은 Editor 변경을 보호하기 위해 Unity 프로세스를 강제 종료하지 않았다.

### 최근 변경과의 관계

- 이번 세션의 코드/씬 변경은 송기마스크 패널의 공용 레이아웃 덮어쓰기 비활성화와 하자 헬멧의 시각 전용 자식화다.
- 두 변경은 새 카메라, RenderTexture, shader 또는 OpenXR 입력 소비자를 추가하지 않았다. 또한 비헤드셋 Play Mode에서 마스크 레이아웃과 헬멧 상태 전환은 통과했다.
- 그러나 실제 Quest/OpenXR 재현 비교가 끝나지 않았으므로, 위 변경이 HMD 실패와 무관하다고 최종 판정하지 않는다. 먼저 OVRService 재시작 직후의 기준 실행과 현재 씬 실행을 비교해야 한다.

### 검증 상태

| 구분 | 결과 |
| --- | --- |
| 정적 확인 | `Editor.log`의 Meta HandInput/RIPC 연결 실패 확인 |
| Unity Editor 확인 | Meta Runtime 재시작 후 기존 Play Mode가 응답하지 않아 종료/재시작 전까지 보류 |
| Quest/OpenXR 확인 | 실패 재현됨. 런타임 재시작 후 재검증은 아직 수행하지 않음 |

### 다음 수동 검증

1. Unity에서 멈춘 Play Mode를 정상 종료한다. 종료가 불가능하면 저장 여부를 확인한 뒤 Unity Editor를 재시작한다.
2. Quest Link를 다시 연결해 Meta Dash가 정상 표시되는지 확인한다.
3. 대상 씬 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`를 연 뒤, 컴파일/도메인 리로드가 끝난 상태에서 Play를 시작한다.
4. 시작 10초 동안 HMD 양안, 주변 시야, Game View를 함께 관찰한다.
5. 재현되면 같은 시각의 `Editor.log`에서 `HandInputDataServer`, `RIPC_CONNECT_LOG`, `XR_ERROR_*`, `ovrError_DisplayLost`를 함께 보관한다. 거울 Off/On 비교나 PPE 상호작용 변경은 이 기준 실행이 안정화된 뒤 한 항목씩 수행한다.

## 2026-08-11 추가: Game View 버튼은 되지만 HMD 조이스틱 클릭은 가이드를 열지 못함

### 근본 원인

- Game View 버튼은 `ControllerGuideMiniActivator`가 직접 만든 가상 Gamepad
  `<Gamepad>/leftStickPress` 액션을 실행하므로 Play 중에도 정상 동작했다.
- HMD 경로는 `Awake`에서 새 standalone `InputAction`을 만들어
  `<XRController>{LeftHand/RightHand}/Primary2DAxisClick`을 바인딩했다. 이 액션은 씬의
  `InputActionManager`가 OpenXR 세션 시작 전에 등록·활성화하는 authored XRI 액션 자산에
  속하지 않아 Quest/OpenXR 액션 세트 경로가 보장되지 않았다.
- 따라서 PC 버튼 성공은 HMD OpenXR 클릭 경로의 성공 증거가 아니었다.

### 적용한 변경

- 씬의 `ControllerGuideMiniActivator.m_XriInputActions`에 이미 `InputActionManager`가 사용하는
  `XRI Default Input Actions`를 직렬화했다.
- HMD 클릭은 해당 자산의 `XRI Left Interaction/Scale Toggle`과
  `XRI Right Interaction/Scale Toggle`을 구독한다. 두 액션은 각각 authored
  `Primary2DAxisClick` 바인딩을 가진다.
- 공유 XRI 액션의 Enable/Disable 생명주기는 계속 `InputActionManager`가 소유한다.
  Activator는 콜백만 구독·해제하며 다른 소비자의 액션 상태를 바꾸지 않는다.
- Game View용 가상 Gamepad 액션은 별도 경로로 유지한다.

### 검증 구분

- 정적 확인: `_scale_0` 씬의 InputActionAsset 참조, 좌우 Scale Toggle 액션과
  `Primary2DAxisClick` 바인딩을 확인했다.
- Unity Editor 확인: 컴파일 후 Game View `StickClick`으로 토글이 유지되는지 확인이 필요하다.
- Quest/OpenXR 확인: 왼쪽과 오른쪽 조이스틱 클릭이 각각 한 번만 가이드를 토글하는지는
  실기기에서 확인해야 한다.

## 2026-08-14 추가: Controller Simp 기본 컨트롤러·오른쪽 이미지 일부 누락

### 근본 원인

- 현재 이름 제출 뒤 실행되는 새 흐름은 `Controller Simp`이며, Clip별 시각 기준은
  `1_Ray → 2_Marker → 3_Ray_T` 부모 그룹이다.
- 새 그룹 매핑을 적용한 뒤에도 `PPEVoiceFlowDirector.ApplyPresentation()`의 기존 상태별
  `m_ControllerRayStep`, `m_ControllerMarkerStep`, `m_ControllerRayTStep`,
  `m_ControllerPanelStep` 토글이 계속 실행됐다.
- 이 기존 참조가 `1_Ray/Card`와 `1_Ray/Panel` 자식을 직접 가리켜, Simple 001·002에서
  부모 `1_Ray`가 활성이어도 `Panel`을 다시 비활성화했다.
- `Assets/UIs/Guide/controller.png`는 임포트돼 있었지만 씬 Image에서 사용되지 않아
  새 가이드 왼쪽의 기본 컨트롤러가 표시되지 않았다.

### 적용한 변경

- `Controller Simp`일 때는 기존 상태별 자식 토글을 적용하지 않고 Clip별 부모 그룹 매핑만
  사용하도록 변경했다. 별도 `Controller Edu`의 기존 단계별 동작은 유지한다.
- `ControllerGuide/Context/Controller_Base` Image를 씬에 작성하고 `controller.png`를
  연결했다. RectTransform은 기존 왼쪽 컨트롤러 Image의 작성값을 기준으로 저장했으며
  런타임 위치·크기·Sprite 할당은 추가하지 않았다. 표시 전용 Image의 `Raycast Target`은
  비활성화해 기존 XR UI 입력 경로를 가로채지 않는다.
- `PPEVoiceFlowSetup`과 `PPERoomCardRaySelectionHarness`가 `Controller_Base`, Sprite 경로,
  시작 활성 상태를 검증하도록 확장했다.

### 영향 범위

- 이름 제출 뒤 실행되는 전체 화면 `Controller Simp` 표시만 변경한다.
- `Controller Edu`, `ControllerGuide_mini`, XR 입력, 음성 재생 순서, 카드·모달·텔레포트
  상태 전이는 변경하지 않는다.

### 완료한 검증

- Unity 동적 컴파일 성공과 `_scale_0` 씬 저장을 확인했다.
- `Tools > PPE > Validate Scale 0 Controller Test Inputs` 및
  `Tools > PPE > Voice Flow > Validate HandTest Scale 0` 검증을 통과했다.
- 씬 직렬화에서 `Controller_Base`가 활성이고 `controller.png`를 참조하며,
  `1_Ray/Card`와 `1_Ray/Panel`이 모두 작성 상태로 활성임을 확인했다.
- 전체 씬 ObjectReference 8,779개를 검사해 깨진 참조가 0개임을 확인했다.

### 아직 필요한 수동 검증

1. Unity Play Mode에서 이름 제출 후 왼쪽 기본 컨트롤러와 오른쪽 `Card`·`Panel`이 함께
   표시되는지 확인한다.
2. 이후 `2_Marker`, `3_Ray_T`가 음성과 맞춰 순서대로 전환되는지 확인한다.
3. Quest/OpenXR 양안에서 이미지 누락·잘림이 없고 기존 입력 흐름이 유지되는지 확인한다.

## 2026-08-16 추가: Train/Test 상세교육 하단 이미지 및 후속 간단교육 누락

### 재현 증상

- 대상 씬은 `Assets/Scenes/3_PPE_Room_Train_Test.unity`이다.
- 키보드 A 또는 오른손 물리 A로 상세 컨트롤러 교육에 진입하면 Trigger 단계의 오른쪽 하단
  이미지가 표시되지 않았다.
- 상세교육을 빠져나와 간단교육을 다시 확인하면 간단교육의 가이드 이미지도 표시되지 않았다.
- 초기 참조 보강 뒤에는 왼쪽 컨트롤러 이미지만 보이거나 오른쪽 안내 이미지가 누락되는 상태가
  단계별로 달라져, 직렬화 참조 존재 여부만으로 정상이라고 판단할 수 없었다.

### 근본 원인

- 상세교육과 간단교육은 작성된 동일 가이드 그룹을 사용하지만,
  `PPEVoiceFlowDirector.ApplyPresentation()`의 기존 상태별 자식 토글도 함께 실행됐다.
- 이 토글이 `1_Ray` 부모를 활성화한 뒤 자식 `Panel`을 다시 비활성화했다.
- 상세교육에서 바뀐 자식 `activeSelf`가 남아 이후 간단교육이 같은 `1_Ray` 부모를 활성화해도
  `Card`·`Panel`이 모두 정상 표시되지 않았다.
- 기존 검증은 참조와 부모 활성 상태 중심이어서 실제 전이 뒤 자식 Graphic과 Sprite가
  렌더 가능한 상태인지 확인하지 못했다.

### 적용한 변경

- 상세 A 진입과 `Controller Simp`는 Clip별 작성 가이드 그룹을 표시 상태의 단일 기준으로
  사용한다. 기존 상태별 `m_ControllerRayStep`, `m_ControllerMarkerStep`,
  `m_ControllerRayTStep`, `m_ControllerPanelStep` 토글이 작성된 자식 활성 상태를 덮어쓰지 않는다.
- 상세·간단교육의 시각 참조를 다음 동일 쌍으로 고정했다.

| 단계 | 오른쪽 안내 그룹 | 왼쪽 컨트롤러 이미지 |
|---|---|---|
| Trigger | `1_Ray` (`Card` + `Panel`) | `1_Ctrl_Trigger` |
| Grip | `2_Marker` | `2_Ctrl_Grip` |
| Joystick | `3_Ray_T` | `3_Ctrl_Joystick` |

- 씬과 Inspector의 RectTransform, 크기, 위치, 색, Sprite는 변경하지 않았다.
- 상세교육 완료 여부를 세션 상태로 보관한다. 미이수 상태의 Enter는 기존
  `Controller Simp → CardIntro`를 유지하고, 상세교육을 완료해 키보드로 돌아온 뒤 Enter는
  중복 간단교육을 건너뛰어 `CardIntro`로 바로 전이한다.
- 상세교육 완료 뒤에도 물리 A와 UI A로 상세교육을 다시 볼 수 있다. 새 세션 시작 시 완료
  상태는 초기화한다.

### 영향 범위

- `3_PPE_Room_Train_Test`의 키보드 A/물리 A 상세교육, 이름 제출 뒤 간단교육 분기 및
  전체 화면 컨트롤러 가이드 표시.
- 퀴즈, PPE 모드, 카드·모달·텔레포트, 미니 가이드 및 다른 씬의 Inspector 작성값은
  변경하지 않는다.

### 완료한 검증

- 디스크 씬에서 상세·간단교육의 세 단계가 동일 GameObject FileID를 사용하는지 대조했다.
- `1_Ray/Card`, `1_Ray/Panel`과 Grip·Joystick 자식 Image가 활성, 알파 1이며 Sprite GUID가
  실제 `Assets/UIs/Guide/*.png` 파일로 해석되는 것을 확인했다.
- Runtime/Editor 보조 빌드 오류 0개를 확인했다.
- `Tools > PPE > Validate Train Test Modes`가 Preview Scene에서 실제
  `ApplyPresentation()`과 `ApplyControllerGuideVisual()`을 호출하도록 확장했다. 상세 Trigger의
  상단·하단·컨트롤러 이미지, Grip·Joystick 양쪽 이미지, 상세 종료 뒤 이름 제출 분기와
  간단교육 재표시를 검사하며 활성 Graphic뿐 아니라 Image Sprite/RawImage Texture도 요구한다.
- Unity Editor에서 최신 검증을 실행해 다음 PASS를 확인했다.

```text
[PPE Train/Test Validation] PASS 'Assets/Scenes/3_PPE_Room_Train_Test.unity': ... detailed-completion skip, and detailed/Simple paired guide presentation are valid.
```

### 아직 필요한 수동 검증

1. Play Mode에서 상세 Trigger의 `Card`·`Panel`과 `1_Ctrl_Trigger`가 동시에 보이는지 확인한다.
2. 상세교육을 끝낸 뒤 키보드 Enter가 간단교육 없이 카드로 바로 전이하는지 확인한다.
3. 상세교육 미이수 새 세션에서는 Enter 뒤 간단교육이 유지되는지 확인한다.
4. Quest/OpenXR 양안에서 세 단계의 좌우 이미지가 누락·잘림 없이 표시되는지 확인한다.

## 2026-08-16 후속 작업: Quest 월드 스페이스 UI 시머링·계단 이동

### 사용자 관찰 증상

- 이 항목은 컨트롤러 교육 이미지 누락과 별개의 렌더링 문제다.
- Quest에서 월드 스페이스 UI의 외곽선과 가는 선이 앨리어싱처럼 긴 계단 형태로 보인다.
- 머리를 움직이면 계단 형태가 좌우로 이동하며 UI 전체가 울렁거리거나 떨리는 것처럼 느껴진다.
- 현재는 Quest Link가 연결·해제를 반복해 동일 조건 재현과 양안 비교를 완료하지 못했다.

### 조사 전제

- 먼저 Quest Link가 최소 수 분 동안 안정적으로 유지되는지 확인한다.
- 2026-08-16 01:55에는 Unity가 Quest 2를 정상 인식했으나 01:57에
  `XR_ERROR_SESSION_LOST`가 발생했고, 이후 `XR_ERROR_FORM_FACTOR_UNAVAILABLE`로 이어졌다.
- Meta 장치 상태에는 `remoteRenderingTerminatedFlakyCable`, SDK `-4503`, `Flaky cable`이
  기록됐다. 연결이 불안정한 상태의 화면을 UI 렌더링 회귀 증거로 사용하지 않는다.

### 다음 세션의 기준 조사

1. 변경 전 현재 URP Asset, Render Scale, MSAA, 카메라 Anti-aliasing, Dynamic Resolution,
   Foveated Rendering/FFR 값을 기록한다.
2. 동일 UI, 동일 거리·각도·조명에서 머리를 고정한 화면과 좌우 이동한 화면을 비교한다.
3. Game View, Quest 왼쪽 눈, 오른쪽 눈에서 증상 유무를 각각 구분한다.
4. 얇은 선 Sprite/Texture의 Filter Mode, Mip Map, 압축, Pixels Per Unit과 World Space Canvas
   스케일을 확인한다.
5. UI 면 또는 선이 겹쳐 있는 경우 깊이 차이와 z-fighting 여부를 먼저 확인한다.
6. 프로젝트 소유 셰이더가 사용된 UI는 Single Pass Instanced 매크로와 양안 위치 일치를
   확인한다.

### 단일 변수 비교 순서

- 기준 실행을 저장한 뒤 `MSAA → Render Scale → Dynamic Resolution/FFR → Texture 필터링·Mip →
  Canvas/선 두께 → 깊이 겹침 → 셰이더` 순서로 한 항목씩만 변경한다.
- 각 변경마다 같은 위치에서 정지 화면과 머리 이동 화면을 비교하고, 효과가 없으면 기준값으로
  복구한 뒤 다음 항목으로 넘어간다.
- Game View 개선만으로 완료 처리하지 않는다. Quest 양안에서 계단 이동과 울렁거림이 줄었는지
  확인해야 한다.

### 현재 상태

- 후속 조사 항목으로 등록했다.
- 아직 근본 원인 확정이나 렌더링 설정 변경은 하지 않았다.
- 다음 작업 시작 조건은 Quest Link 안정화와 변경 전 기준 실행 확보다.
## 2026-09-02 후속: 미니 컨트롤러 가이드 오른쪽 이미지 시머링

### 사용자 관찰과 최근 변경 대조

- Quest 독립 실행에서 `ControllerGuide_mini` 오른쪽 컨트롤러 이미지가 아지랑이처럼 흔들려 보였다.
- 최근 품질 변경은 상세 단계용 `Controller_tri.png`, `Controller_gri.png`, `Controller_joy.png`만
  mipmap·Trilinear·Android 고품질 대상으로 포함했고, 미니 가이드의 `controller.png`는 mipmap 비활성,
  Bilinear, aniso 1, Android 기본 압축 품질 50으로 남아 있었다.
- `ControllerGuideMiniActivator`는 가이드 활성 상태만 전환하며 Image, RectTransform, Material을 런타임에
  덮어쓰지 않는다. 따라서 먼저 누락된 Importer 설정을 단일 원인 후보로 수정한다.

### 변경 전 필수 질문

1. 씬의 `ControllerGuide_mini/Context/Controller_Image` RectTransform 100×100, 앵커, 피벗, Sprite와
   활성화 순서를 보존한다.
2. 화질의 단일 기준은 `Assets/UIs/Guide/controller.png`의 `TextureImporter`, 표시 상태 소유자는 기존
   `ControllerGuideMiniActivator`다.
3. 이미지 자체는 `Raycast Target`이 아니며 XR 입력, Interactor, Raycaster, EventSystem, Select action을
   변경하지 않는다.
4. 누락 참조를 런타임 자동 수리하지 않고 Importer 누락·설정 회귀는 하네스 오류로 중단한다.
5. 이번 단계의 소비자는 미니 가이드 오른쪽 이미지뿐이다. 상세 컨트롤러 교육, 타이틀, 거울, 진열장,
   PPE 상태 전이는 변경하지 않는다.
6. 변경 전 기준은 Quest 관찰과 mipmap Off/Bilinear/aniso 1/압축 품질 50이다. 변경 후 같은 거리와
   머리 이동에서 윤곽·버튼 선의 시간축 안정성을 비교한다.
7. 정적 Importer·C# 컴파일·Unity Import와 Quest/OpenXR 양안 검증을 구분한다.

### 적용 내용과 검증 상태

- `controller.png`에 mipmap, Trilinear, aniso 8을 적용했다.
- Android에는 기존 상세 컨트롤러 이미지와 같은 `CompressedHQ`, 품질 100 override를 적용했다.
- `PPELocomotionPpeRegressionValidationHarness`가 해당 이미지의 샘플링과 Android 고품질 override를
  함께 검사하도록 확장했다.
- 씬과 런타임 표시 코드에는 변경이 없다. 정적 설정 대조와 Runtime/Editor C# 빌드는 오류 0개로
  통과했다.
- Unity 배치 하네스는 `com.unity.editor.headless` 라이선스 부재로 Editor 초기화 전에 종료되어 실행하지
  못했다. 일반 Unity Import·메뉴 하네스와 새 APK의 Quest 양안 비교는 아직 필요하다.

## 2026-09-02 후속: 컨트롤러 버튼 파란 점선 강조

### 사용자 요청과 기준

- Trigger, Grip, Joystick 안내 이미지에서 해당 버튼 위치를 파란 점선의 속이 투명한 원으로 강조하고,
  안내 중 부드럽게 밝아졌다 어두워지는 효과를 추가한다.
- 원본 `Controller_tri.png`, `Controller_gri.png`, `Controller_joy.png`의 선화·한글·현재 녹색 표시와
  씬의 이미지 배치는 보존한다.

### 변경 전 필수 질문

1. `4_PPE_Room`의 기존 ControllerGuide RectTransform, Sprite, 크기, 위치와 단계별 활성 상태를 보존한다.
2. 표시 상태의 단일 소유자는 기존 `PPEVoiceFlowDirector`이며, 강조 원은 각 단계 이미지의 씬 작성 자식
   UI로 두어 부모 표시 상태만 그대로 상속한다.
3. 강조 원은 `Raycast Target`을 끄고 XR Interactor, Caster, Raycaster, EventSystem, 입력 action과
   Trigger·Grip·Joystick 판정 코드를 변경하지 않는다.
4. 런타임 자동 생성·자동 수리는 하지 않는다. 명시적 Editor 메뉴가 최초 자식 UI를 만들고, 이후 실행은
   기존 Inspector 작성값을 덮어쓰지 않는다.
5. 영향 소비자는 상세·간단 컨트롤러 교육에서 함께 쓰는 세 ControllerGuide 이미지뿐이다. 미니 가이드,
   카드·모달·텔레포트·PPE Grab·거울은 변경하지 않는다.
6. 변경 전 기준은 세 PNG에 포함된 녹색 정적 원과 현재 단계별 표시 동작이다. 변경 후 같은 안내 단계에서
   좌우 버튼 위 점선 원의 위치, 가독성, 맥동과 기존 입력 진행을 비교한다.
7. 정적 C#·씬 직렬화·Unity 메뉴 하네스와 Quest/OpenXR 양안 표시를 구분해 검증한다.

### 구현 방향

- 표준 Unity UI `MaskableGraphic` 메시로 점선 원을 그려 별도 PNG와 커스텀 XR 셰이더를 추가하지 않는다.
- 색, 점선 수, 두께, 맥동 속도·알파·크기 범위는 직렬화하고, RectTransform 위치·크기는 씬 작성값으로 둔다.
- Editor 생성기는 현재 빌드 대상인 `Assets/Scenes/4_PPE_Room.unity`가 열린 경우에만 누락된 강조 자식을
  최초 생성하고 씬을 자동 저장하지 않는다.

### 적용 내용과 검증 상태

- `ControllerGuideDashedRing`은 표준 UI 메시로 12개의 파란 점선을 그리고, 내부는 투명하게 유지하면서
  초당 1.4회 알파와 반지름만 맥동시킨다. RectTransform, 색과 표시 단계는 런타임에서 덮어쓰지 않는다.
- `1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick` 아래에 좌우 강조 자식을 각각 2개씩, 총 6개 작성했다.
  각 자식은 `CanvasRenderer`를 포함하고 `Raycast Target`이 꺼져 있어 기존 XR 입력을 소비하지 않는다.
- 공통 고가독성 이미지의 버튼 윤곽 중심과 다시 대조해 X와 크기는 유지하고 Y만 내렸다. Trigger는 좌우
  `(0.335, 0.530)`·`(0.665, 0.530)`, Grip은 `(0.207, 0.462)`·`(0.793, 0.462)`, Joystick은
  `(0.2, 0.602)`·`(0.8, 0.602)`다.
- Unity Editor에서 `PPELocomotionPpeRegressionValidationHarness.Validate()`가 PASS했다. 이 검증은 강조 자식
  6개, 필수 `CanvasRenderer`, 표준 UI 재질, 비입력 상태, 좌우 앵커와 맥동 직렬화 범위를 포함한다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드는 오류 0개로 통과했다. 새 APK의
  Quest/OpenXR 양안에서 실제 점선 위치·맥동·가독성과 기존 단계 진행을 확인하는 수동 검증은 남아 있다.

### 공통 고가독성 컨트롤러 이미지 시험

- 사용자 요청에 따라 `1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick`의 Sprite를 모두 글자를 키운
  `Assets/UIs/Guide/controller.png`로 통일했다.
- 세 Image의 기존 RectTransform, 활성 상태와 6개 점선 링의 앵커·크기는 변경하지 않았다. 따라서 단계별로
  공통 바탕 이미지가 표시되며, 해당 설명의 좌우 버튼 점선 한 쌍만 부모 활성 상태를 따라 표시된다.
- 공통 Sprite 참조를 기준으로 `PPELocomotionPpeRegressionValidationHarness.Validate()`를 다시 실행해 PASS했다.
  실제 Quest/OpenXR에서 확대된 글자의 가독성과 점선 위치를 비교한 뒤 유지 또는 원복을 결정한다.

## 2026-09-06 후속: 미러링 영상의 파란 주변 시야 계단 깨짐과 정지감

### 영상 정보

- 분석 파일은 `C:\Users\lanoc\Downloads\oculus_cast_video_09_06_2026_10_13_03.mp4`다.
  사용자가 전달한 `oculus\_cast\_video\_09\_06\_2026\_10\_13\_03` 표기는 Markdown escape가 포함된
  축약 표기이며, 실제 MP4 파일명은 `_`를 그대로 포함한다.
- 원본 MP4는 수정하거나 삭제하지 않았다. 프레임 확인은 임시 폴더
  `C:\Users\lanoc\AppData\Local\Temp\codex_video_analysis_20260906_101303`에 추출한 이미지로만 수행했다.
- 영상 메타데이터는 재생 시간 `195.52초`, 해상도 `2336x1312`, FPS `29.99`, 비디오 코덱 `HEVC`,
  오디오 코덱 `AAC`다.

### 증상

- 사용자는 약 `2:41`부터 HMD 주변 시야가 계단식으로 깨지면서 거의 멈추는 현상을 보고했다.
- 영상 프레임 기준으로는 최소 `2:10-2:42` 구간에서 이미 화면 대부분이 검은 배경과 파란 곡선 격자로
  유지되며, 움직임이 매우 작다.
- `2:41-2:42` 대표 프레임에서는 검은 화면 위에 파란 격자선이 크게 보이고, 오른쪽 가장자리에는 노란
  오브젝트 경계가 계단식 또는 톱니형으로 잘려 보인다.
- `2:45`에는 여전히 검은 격자 화면이지만 시야 방향 변화가 조금 커진다.
- `2:46-2:47`에는 노란 방호복 또는 노란 PPE 표면이 화면 가까이 들어오며, 검은 격자 영역과 노란 오브젝트가
  함께 보인다.

### 영상 분석

- 1초 간격 저해상도 프레임 변화량을 비교하면 `2:10-2:42`는 평균 변화가 낮고, `2:45` 이후부터 변화량이
  커진다. 따라서 영상만으로는 앱이 완전히 정지했다고 확정하기보다, 검은 격자·계단형 주변 화면이 오래
  유지되고 HMD 시야 변화가 매우 작았다고 기록한다.
- 검은 배경과 파란 곡선 격자는 Unity PPE Room의 일반 룸 화면과 다르게 보인다. 다만 이것이 Quest Link,
  Guardian/경계, Meta 런타임 오버레이, Unity 내부 fade/clip, 카메라 near clip, 또는 성능 저하로 인한
  compositor 현상인지는 영상만으로 확인할 수 없다.
- 계단식 깨짐은 오른쪽 또는 대각선 가장자리의 노란 오브젝트 경계에서 특히 뚜렷하다. 이는 HMD가 노란
  방호복·진열장·근접 오브젝트 내부 또는 표면에 과도하게 가까워진 상태와 동시에 관찰된다.
- 이 증상은 같은 날 기록한 HMD·손 환경 관통과 시간·위치상 관련될 수 있지만, 관통 대응과 동일 원인으로
  확정하지 않는다. 파란 주변 시야 깨짐은 별도 XR 렌더링·런타임·성능 문제로 분리해 조사한다.

### 확인할 수 없는 사항

- 실제 HMD 양안에서 어느 눈에 먼저 발생했는지, Game View에도 동일하게 보였는지, Quest Link 대기 화면인지,
  Guardian 표시인지, Unity 렌더링 결과인지, Meta compositor 재투영 또는 프레임 드랍인지는 영상만으로
  확인 불가다.
- Unity 로그, Meta/OpenXR 로그, Profiler CPU/GPU frame time, RenderTexture 상태, 반사 카메라 렌더링 여부,
  카메라 near/far clip 설정, FFR/Dynamic Resolution/MSAA 설정은 영상만으로 확인 불가다.

### 후속 읽기 전용 조사 항목

1. 같은 재현 위치에서 Game View, Quest 왼쪽 눈, Quest 오른쪽 눈의 증상 유무를 분리해 기록한다.
2. 증상 직전 10초와 직후 10초의 Unity Console, Editor.log, Meta/OpenXR 런타임 로그를 수집한다.
3. Quest Profiler 또는 OVR Metrics Tool로 `CPU/GPU frame time`, dropped frame, app/compositor frame rate,
   thermal/throttling 상태를 확인한다.
4. HMD Camera의 near/far clip, URP Render Scale, MSAA, Dynamic Resolution, FFR, Vulkan/OpenXR 관련 설정을
   변경 없이 기록한다.
5. Planar Mirror, 반사 RenderTexture, 추가 Camera 렌더링이 켜진 상태인지 확인하되, 거울을 원인으로
   확정하기 전에는 런타임 Off/On 단일 변수 비교를 수행한다.
6. 관통 재현 위치의 노란 방호복·진열장·프레임 Renderer/Collider Bounds와 HMD Camera 위치를 대조하되,
   파란 주변 시야 깨짐은 관통 문제와 별도 결함으로 추적한다.

### 수정 경계

- 이번 기록 단계에서는 Unity 씬, Collider, 렌더링 설정, 코드, ProjectSettings, Prefab, Material, Asset을
  수정하지 않았다.
- 후속 구현은 HMD·손 관통 대응 패치와 파란 주변 시야 깨짐 대응 패치를 섞지 않는다.
- Quest/OpenXR 실기 검증 전에는 렌더링 안정화 완료로 보고하지 않는다.

## 2026-09-06 추가 정정: Game View 검정화면 동반 기준 재분류

### 추가 사용자 확인

- 사용자는 미러링 영상의 파란 주변 시야 깨짐이 관통 상황에서만 발생하는 것이 아니며, Play Mode를 계속
  유지하면 Unity Game View도 대체로 검정화면이 된다고 추가 확인했다.
- 따라서 위 `2026-09-06 후속` 섹션의 “Game View에도 동일하게 보였는지 영상만으로 확인 불가” 항목은
  영상 분석 당시의 한계로 유지하되, 최신 사용자 재현 정보 기준으로는 Game View 검정화면 동반 증상을
  별도 사실로 추가 기록한다.

### 재분류

- HMD 주변 시야 계단 깨짐과 장시간 검정화면은 Quest Link 또는 HMD 전송 문제만으로 단정하지 않는다.
- Game View도 함께 검정화면이 되면 Unity가 제출하는 앱 렌더 프레임, Play Mode 상태, 카메라 출력,
  렌더링 부하, RenderTexture·추가 Camera, OpenXR 세션 상태가 함께 무너지는 경로를 우선 조사한다.
- 과거 문서에 기록된 Quest Link·Meta Runtime IPC 실패 사례는 실제 로그가 동반된 별도 실패 모드다.
  동일 로그가 없는 최신 재현에 그대로 원인으로 적용하지 않는다.
- 관통 재현 위치에서 증상이 더 잘 보일 수는 있지만, 관통이 없는 대기 또는 일반 플레이 상태에서도
  Game View가 검정화면으로 진행된다면 환경 관통 대응과 렌더링 안정성 대응은 다른 결함으로 추적한다.

### 다음 읽기 전용 조사 항목

1. Play 시작 후 검정화면까지의 경과 시간을 관통 동작 없음, 관통 위치 접근, PPE 착용·거울 표시 조건으로
   나누어 기록한다.
2. Game View가 검정화면이 되는 정확한 시점의 Unity Console, Editor.log, Meta/OpenXR 로그를 수집한다.
3. 검정화면 직전 활성 Camera 목록, Main Camera enabled 상태, targetTexture, cullingMask, clearFlags,
   near/far clip, XR DisplaySubsystem running 상태를 읽기 전용으로 기록한다.
4. Planar Mirror와 반사 RenderTexture, 추가 Camera 렌더링은 원인으로 확정하기 전에 Off/On 단일 변수로
   비교한다.
5. Quest Link 로그가 `DisplayLost`, `XR_ERROR_SESSION_LOST`, RIPC 실패를 실제로 기록한 경우에만
   Link/HMD 런타임 장애로 분류한다.

## 2026-09-07 code 5 Alpha 기준 수집 차단 게이트

- 사용자는 집 환경에서 HMD 주변 시야 깨짐이 계속 발생한다고 재확인했다. code 5 실플레이 기준 데이터와
  Alpha 최종 실기에서는 이 증상을 제출 차단 결함으로 취급한다.
- Codex가 Quest 미러링 또는 녹화, ADB logcat과 필요한 성능 관찰을 먼저 시작하기 전에는 사용자에게
  기준 플레이를 요청하지 않는다.
- 깨짐, 검은 화면, 정지감 또는 Game View 동반 검정이 한 번이라도 발생하면 해당 회차를 즉시 중단한다.
  생성된 JSONL과 서버 이벤트는 삭제하지 않고 비기준 후보로 분리하며 다음 조합으로 진행하지 않는다.
- 룸스케일 헤드·손 관통 Known Issue와 시야 깨짐을 같은 원인이나 같은 허용 결함으로 합치지 않는다.
- 원인 후보는 최근 code 5 변경, 씬·직렬화·카메라·RenderTexture 경로, OpenXR·Quest Link 실행 환경 순으로
  증거를 분리한다. 실제 로그 없이 Link/HMD 문제로 단정하지 않는다.
- 기준 수집은 Quest Link Play Mode가 아니라 Quest에 설치한 Development APK 독립 실행으로 한다. 과거 약
  `2:41` 재현보다 긴 5분 안정성 스모크가 먼저 통과해야 한다.
- 6개 조합 도중 증상이 발생해도 이미 정상 완료되고 JSONL·DB 대조가 끝난 `modeSessionId`는 보존한다.
  증상 중 진행하던 회차만 비기준 후보로 분리하고, 복구 뒤 아직 승인되지 않은 조합부터 재개한다.
