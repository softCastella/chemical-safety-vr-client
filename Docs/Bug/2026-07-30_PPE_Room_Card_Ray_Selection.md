# PPE Room Card Ray Selection and Short Ray

Date: 2026-07-30

## Symptom

- `XR UI Canvas (1)` disappeared while merely moving a controller before an intentional click.
- Card selection accepted input paths other than a controller ray plus trigger.
- After selection, the Canvas did not consistently hide and the controller ray appeared cut short before the PPE location markers.

## Cause

- `ScenarioCardSelectProxy` created an `XRSimpleInteractable` at runtime and listened to its generic Select event in addition to mouse and UI pointer input.
- Only one of the three serialized card proxies referenced `XR UI Canvas (1)` as its post-selection hide target.
- The controller Near-Far prefabs cast and rendered only 10 m, while the PPE markers are roughly 12-15 m from the initial rig position.
- `CurveVisualController` used a 0.25 m resting line and did not extend the line when no valid hit was available, producing the cut-ray appearance.

## Fix

- Accept card selection only from an XR `TrackedDeviceEventData` produced by a `NearFarInteractor` ray click.
- Remove the mouse, generic pointer, and runtime-created `XRSimpleInteractable` selection paths.
- Disable the mouse `GraphicRaycaster` and mouse physics fallback on `XR UI Canvas (1)`.
- Point all three card proxies at `XR UI Canvas (1)` for post-selection hiding.
- Author both controller rays to cast/render for 40 m and extend to an empty hit.

## Validation

Run `Tools > PPE > Validate Card Ray Selection` or invoke
`PPERoomCardRaySelectionHarness.Validate` in Unity batch mode. Headset validation must still confirm both controllers, both eyes, trigger-only card selection, Canvas persistence before selection, Canvas hiding after selection, and ray/reticle contact with both PPE location markers.

## 2026-08-05 수정: PPE 원거리 레이 선택 분리

### 근본 원인

`PPEMarkerToggleGrab`이 `XRGrabInteractable`의 콜라이더 목록을 marker로 제한하고 있어도, `XR Item Marker_small` 자체가 Default 레이어에 남아 있으면 Near-Far의 원거리 물리 캐스터가 marker 콜라이더를 계속 적중시킬 수 있다. 원거리 캐스팅을 전역으로 끄면 카드 UI 레이가 함께 끊기므로 전역 설정 변경은 올바른 해결이 아니다.

### 적용한 변경

- 이 씬의 PPE marker 10개를 전용 빈 물리 레이어 6으로 이동했다.
- 양손 근거리 캐스터의 물리 레이어 마스크에 0과 6을 지정했다.
- 양손 원거리 캐스터는 기존 마스크를 유지해 레이어 6을 계속 제외한다.
- 카드 UI의 Canvas 레이캐스트 레이어와 Near-Far UI 상호작용은 변경하지 않았다.
- 미러의 씬 뷰 렌더링을 복구하고, Play 중 씬 뷰만 건너뛰도록 성능 옵션을 분리했다.

### 검증

- 씬 YAML에서 marker 10개, 근거리 마스크 2개, 원거리 마스크 2개를 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: 오류 0개.
- Unity 에디터가 해당 프로젝트를 점유 중이어서 batchmode 씬 로드는 아직 실행하지 못했다.

### 남은 수동 검증

Unity 컴파일 완료 후 씬을 다시 로드하고, 카드에는 원거리 레이 트리거가 동작하는지, PPE에는 marker에 손을 직접 가져갔을 때만 선택되는지, 미러가 씬 뷰와 Play Mode 양쪽에서 표시되는지 Quest 양쪽 눈으로 확인해야 한다.

## 2026-08-05 통합 회귀 기록: 카드 Trigger·시나리오 모달·PPE 입력 분리

### 증상

- 시나리오 카드를 선택해도 기존에 표시되어야 하는 시나리오 상세 모달이 열리지 않는 현상이 보고되었다.
- 모달 연결 이후 카드 Trigger 자체가 먹지 않는 것처럼 보이는 경우가 있었다.
- 시나리오 카드 UI가 열려 있는 동안 텔레포트가 가능해 입력 순서가 무너졌다.
- PPE는 아이템 Mesh가 아니라 `XR Item Marker_small` 기준으로만 잡혀야 하는데, 원거리 레이가 주변 PPE까지 선택할 위험이 있었다.

### 근본 원인 해석

- 카드 선택 완료 루틴에서 Canvas를 먼저 숨기고 반환하면 이후 `ScenarioDetailModal.Show()`가 호출되지 않는다.
- 카드 입력을 XRI 이벤트 타입·레이 포인트 조건까지 과도하게 제한하면 실제 Near-Far Trigger 이벤트가 프록시까지 도달해도 거부될 수 있다.
- PPE marker가 원거리 물리 레이 마스크에 들어가 있으면 Mesh fallback을 제거해도 멀리서 marker가 선택된다.

### 적용한 변경

- 카드 선택 시 유효한 `ScenarioDetailModal`을 먼저 호출하고, 시나리오 선택 루트는 모달 상태에 따라 닫도록 흐름을 조정했다.
- PPE 착용 선택이 끝난 뒤에만 `NotifyScenarioReadyForMovement()`를 호출하도록 카드/모달/텔레포트 상태를 분리했다.
- PPE marker를 전용 물리 레이어로 분리하고 근거리 캐스터만 해당 레이어를 사용하도록 씬 직렬화 값을 조정했다. 카드 UI 레이캐스트는 유지했다.

### 영향 범위

- 시나리오 카드 Trigger, 시나리오 상세 모달, PPE 근거리 Grab, 텔레포트 활성 상태, 카드 UI와 PPE marker의 레이 분리에 영향을 준다.

### 완료한 검증과 남은 검증

- 씬 YAML과 스크립트의 모달 참조, 선택 루트, marker 10개, 근거리/원거리 레이 마스크를 정적으로 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`는 오류 0개였다.
- Unity 라이선스 문제로 실제 Play Mode와 Quest/OpenXR Trigger 검증은 아직 완료하지 못했다. 카드 Trigger가 모달을 열고, 모달 중 텔레포트가 차단되며, PPE 선택 후 이동이 허용되는지 실기기에서 순서대로 확인해야 한다.

## 2026-08-05 수정: HandTest scale 씬 BGM 시작 및 PPE 교육 선택 페이드

### 증상과 근본 원인

- `3_PPE_Room_HandTest_scale`, `_scale_0`, `_scale_1`에는 씬 이름에 대응하는
  `SceneAudioSettings` 리소스가 없어 씬을 직접 시작할 때 BGM이 재생되지 않았다.
- 기존 `ScenarioCardSelectProxy`의 `fadeOutBgmOnSelection`은 카드 Trigger 시점에
  실행되어, 모달의 PPE 착용 교육 버튼을 누르기 전에 BGM이 사라질 수 있었다.

### 적용한 변경

- 세 HandTest scale 씬에 동일한 PPE Room BGM, 볼륨 `1`, 시작 지연 `0.5초`, Fade In
  `0.5초`를 씬 이름별 `SceneAudioSettings`로 추가했다.
- 세 씬의 시나리오 카드 페이드는 끄고, `ScenarioDetailModal`의
  `incompletePpeButton` 선택 시에만 씬 작성값 `1.25초`로 BGM을 조용히 페이드아웃하도록
  변경했다.
- 일반 훈련 선택과 카드 모달 표시 자체에서는 BGM을 페이드하지 않는다.

### 검증 상태

- 씬 YAML의 카드 페이드 비활성화, 모달 페이드 `1.25초`, 씬별 오디오 설정과 BGM
  클립 참조를 정적으로 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`는 오류 0개였다.
- Unity 에셋 임포트 완료 후 직접 Play Mode에서 시작 BGM, 카드 선택 후 유지, PPE 착용
  교육 버튼 선택 시 1.25초 페이드 순서를 확인해야 한다.

## 2026-08-05 추가: HandTest scale 카드 입력면 누락

### 증상

- `3_PPE_Room_HandTest_scale`, `_scale_0`, `_scale_1`에서 카드 Trigger를 눌러도 `ScenarioDetailModal`이 열리지 않았다.

### 원인

- 활성 `TrackedDeviceGraphicRaycaster`와 `XRUIInputModule`은 있었지만, 카드 외형은 `MeshRenderer` 기반이라 UI Graphic 입력 대상이 아니었다.
- 카드의 `Interaction Feedback Overlay`는 시각 피드백용으로 `raycastTarget: 0`이었고, 별도 Hitbox Button도 비활성 상태였다. 따라서 `ScenarioCardSelectProxy.OnPointerClick`까지 `TrackedDeviceEventData`가 도달하지 않았다.

### 적용한 변경

- 입력면 추가 시도에서 Unity `fileID` 허용 범위를 초과한 값을 사용해 씬 파서 오류가 발생했다.
- 해당 입력면과 참조는 즉시 세 scale 씬에서 제거했다. 현재 카드 입력면 수정은 적용하지 않은 상태다.

### 검증 상태

- 잘못된 입력면 직렬화 변경과 카드 자식 참조가 세 씬에서 제거된 것을 정적으로 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: 오류 0개.
- 현재 Unity에 남아 있는 잘못 로드된 씬 상태는 저장하지 말고 씬을 재로드한 뒤 PPE 계층/Transform을 먼저 확인해야 한다. 카드 수정의 Play Mode 및 Quest/OpenXR 검증은 보류한다.

## 2026-08-05 추가: `_scale_0` 카드 입력면 복구

### 적용한 변경

- 기준 씬을 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`로 고정했다.
- 기존 카드의 마지막 자식인 `Interaction Feedback Overlay` 3개를 입력면으로 재사용하고, 각 `RoundedRectangleGraphic`의 `m_RaycastTarget`만 `0`에서 `1`로 변경했다.
- 새 GameObject, 새 FileID, 런타임 자동 생성은 사용하지 않았다. 카드의 기존 `ScenarioCardSelectProxy`와 `ScenarioDetailModal` 참조는 보존했다.

### 검증 상태

- `_scale_0`의 기존 FileID와 카드/모달 참조를 유지한 채 세 오버레이만 변경된 것을 정적으로 확인했다.
- Unity에서 씬 재임포트 후 카드 Trigger가 모달을 여는지, 모달의 PPE 교육 버튼 선택 시 BGM이 페이드아웃되는지는 수동 확인이 필요하다.

## 2026-08-05 추가: 클릭 이벤트 타입 차단 및 XR 오디오 출력 분리

### 확인된 원인

- `ScenarioCardSelectProxy.OnPointerEnter`와 `OnPointerDown`은 실행되지만 `OnPointerClick`은 `TrackedDeviceEventData`가 아닐 경우 즉시 반환하고 있었다. 따라서 hover/press 시각 효과만 보이고 `Trigger()`가 실행되지 않을 수 있었다.
- Unity 로그에서 `ScenarioDetailModal.Show()` 호출은 확인되었으므로 카드 프록시와 모달 참조 자체는 연결되어 있다.
- `_scale_0`의 `Resources/Audio/Scenes/3_PPE_Room_HandTest_scale_0.asset`와 BGM 클립 참조는 정상이었다. 다만 헤드셋 미연결 상태에서 `XR: Error setting active audio output driver. Falling back to default.`가 기록되어 BGM 출력은 런타임 장치 연결 상태와 분리해 확인해야 한다.

### 적용한 변경 및 검증

- `ScenarioCardSelectProxy.OnPointerClick`의 과도한 `TrackedDeviceEventData` 타입 차단을 제거해 XR/일반 Pointer 이벤트 모두 `Trigger()`로 연결했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`: 오류 0개.
- 실제 Quest/OpenXR 클릭과 헤드셋 오디오 출력은 헤드셋 연결 후 수동 확인이 남아 있다.

## 2026-08-05 추가: `_scale` / `_scale_1` 모달 `modalRoot` 누락

### 증상

- `3_PPE_Room_HandTest_scale`(및 `_scale_1`)에서 시나리오 카드 Trigger 시 hover/press
  시각 효과는 나오지만 모달이 열리지 않고 카드만 남았다.
- BGM도 들리지 않는다고 보고되었다.

### 근본 원인 (모달)

1. 카드 `ScenarioCardSelectProxy.modal`이 활성 `XR UI Canvas`의
   `ScenarioDetailModal`(fileID `1047281581`)을 가리킨다.
2. 그 컴포넌트의 `modalRoot`가 `{fileID: 0}`이라 `Show()`가 즉시 return 한다.
   그래서 선택 피드백만 보이고 모달 UI는 켜지지 않는다.
3. 실제 `Scenario Detail Modal` 부모인 `Modal Canvas`도 씬에서 **비활성**이었다.
   `modalRoot`만 켜도 부모가 꺼져 있으면 화면에 안 보인다.
4. `_scale_0`에는 같은 참조가 이미 연결되어 있고 `Modal Canvas`도 활성이다.

### 근본 원인 (BGM) 후보

- `Resources/Audio/Scenes/3_PPE_Room_HandTest_scale.asset` 및 `_scale_0`/`_scale_1`
  설정과 클립 참조는 존재한다. `AudioManager`는 씬 이름 기준으로 로드한다.
- Build Settings에 등록된 PPE 씬은 `3_PPE_Room_HandTest_scale_0`뿐이다.
  Editor에서 `_scale`을 직접 Play하면 해당 씬 설정으로 BGM이 큐잉된다.
- 이전 로그에 헤드셋 미연결 시 `XR: Error setting active audio output driver`가
  있어, 리소스 누락이 아니라 **출력 장치/Listener** 쪽을 분리해 확인해야 한다.
- 모달이 안 열린 상태에서는 PPE 교육 버튼 → BGM 페이드 경로까지 도달하지 못한다.

### 적용한 변경

- `_scale`, `_scale_1`의 활성 Canvas `ScenarioDetailModal`에 `_scale_0`과 동일한
  `modalRoot`·버튼·텍스트·`scenarioSelectionRoot`·BGM 페이드 필드를 연결했다.
- `Modal Canvas`를 활성, `Scenario Detail Modal`은 초기 비활성으로 맞춤.
- `ScenarioDetailModal.Show`는 `modalRoot` 누락 시 Error 로그를 남긴다.
- 부모 Canvas를 런타임에 `SetActive` 하던 경로는 제거했다. `TrackedDeviceGraphicRaycaster`
  가 붙은 Canvas(`Modal Canvas`, `XR UI Canvas`)를 끄면 XRI `OnDisable`에서
  `KeyNotFoundException`(eventCamera 비어 있음)이 난다. 모달은 자식 `modalRoot`만
  토글한다.

### 수동 검증

1. `3_PPE_Room_HandTest_scale` Play → 시작 후 BGM 재생 여부
2. 카드 Trigger → 시나리오 상세 모달 표시
3. 훈련 선택 → PPE 착용 교육 → 모달/카드 닫힘 + BGM 페이드 + 텔레포트 가능
4. Quest Link 연결 상태에서 오디오 출력 재확인

## 2026-08-05 추가: PPE 씬 BGM 시작 / PPE 교육 페이드 아웃

### 의도

- `3_PPE_Room_HandTest_scale_0` 시작 시 `Safe-Horizons` BGM 재생
- 모달의 PPE 착용 교육(Incomplete PPE) 버튼에서 BGM을 천천히 페이드 아웃

### 원인 / 보강

- 씬별 `Resources/Audio/Scenes/3_PPE_Room_HandTest_scale_0.asset`과
  `ScenarioDetailModal.fadeOutBgmOnIncompletePpeSelection` 경로는 이미 있었다.
- DontDestroyOnLoad `AudioManager`에 이전 씬의 `FadeOutBgm` 코루틴이 남아 있으면
  다음 씬 지연 재생과 충돌할 수 있다. 씬 큐잉 시 페이드를 중단하도록 고쳤다.
- 씬 BGM은 `PlayLooping` 조기 return을 쓰지 않고 항상 다시 `Play`한다.
- 클립 import의 3D 플래그를 끄고, PPE 페이드 길이를 **2.5초**로 늘렸다.

### 수동 검증

1. `_scale_0` Play → Console에 `AudioManager: playing scene BGM` 로그 + BGM 청취
2. 카드 → 훈련 선택 → PPE 착용 교육 → 약 2.5초 페이드 아웃 후 무음
3. 헤드셋 미연결 시 XR audio driver 경고가 있으면 PC 스피커/Listener도 함께 확인

## 2026-08-06 추가: 저장으로 모달 Canvas 비활성 / Heading 참조 유실

### 증상 (사용자 재보고)

1. `_scale_0`에서 BGM이 안 들림
2. 카드 클릭 시 모달이 안 뜸
3. 카드만 사라지고 Heading(시나리오 선택 텍스트)만 남음

### 디스크 확인 결과

- `Modal Canvas`가 **`m_IsActive: 0`** 으로 저장되어 있었다. `Show()`가
  `Scenario Detail Modal`만 켜도 부모 Canvas가 꺼져 있으면 렌더되지 않는다.
- `additionalSelectionUiRoots` / `modalPanel`이 빈 값/`fileID: 0`으로 저장되어
  HUD만 숨기고 Heading이 남았다.
- 코드상 `SetSelectionUiVisible` → HUD 숨김은 동작하고 있었으므로, “카드만 사라짐”은
  모달 미표시와 같은 `Show()` 호출의 결과였다.

### 재적용

- `Modal Canvas` 활성, `Scenario Detail Modal` 초기 비활성
- Heading / Modal Panel 직렬화 재연결, 카드 2·3 활성
- `Show()`가 부모 Modal Canvas를 **켤 수는** 있게 보강 (끌 때는 자식만)
- Heading 미연결 시 형제 `Heading` 이름 fallback
- 씬 BGM fade-in 0으로 즉시 재생, Play 실패 시 Error 로그
- `ScenarioCardSelectProxy`는 `TrackedDeviceGraphicRaycaster`가 있는 Canvas 루트를
  끄지 않는다(`KeyNotFoundException` 방지). 선택 UI는 HUD/Heading 자식만 숨긴다.

### 영향 범위 / 남은 검증

- `_scale_0` 카드→모달→PPE 교육→이동 허용 흐름과 BGM 시작/페이드에 영향을 준다.
- 디스크 씬을 다시 로드한 뒤 Play Mode에서 모달 표시·Heading 소거·BGM을 확인한다.
- Unity에서 옛 Hierarchy 상태로 Don’t Save 하면 `Modal Canvas` 비활성이 재저장될 수 있다.
- 세션 요약: `Docs/MeetingNotes/2026-08-05_PPE_Room_HandTest_Meeting.md`의
  `2026-08-06 세션 요약` 절.

## 2026-08-07 재발 보고: Luna 오디오 경로 우회로 인한 씬 오디오 진단 불가

### 상태

| 항목 | 내용 |
| --- | --- |
| 상태 | 미해결 — 아키텍처 전환 전에는 실제 재생 여부를 신뢰할 수 없음 |
| 대상 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` |
| 영향 | BGM, Voice 나레이션, 키보드 클릭음, PPE SFX의 재생·음량·출력 장치 진단 |

### 관찰된 사실

- `XR Origin (VR)`에는 `PPEVoiceFlowDirector`가 있으나 `AudioSource`는 없다. Director가 XR Origin에 있다고 해서 XR Origin에서 음성이 직접 재생되는 구조가 아니다.
- 키보드 클릭음 Source는 `Hangul Spatial Keyboard Test/UI Root`에 있고, BGM·Voice Source는 런타임 `AudioManager`가 생성한다. 즉 Inspector에서 찾는 오브젝트와 실제 재생 Source가 분리돼 있다.
- XR 카메라의 `AudioListener`와 BGM Clip 참조는 정적으로 존재하지만, 이것만으로 실제 출력·`isPlaying`·헤드셋 장치를 확인할 수 없다.

### Luna 작업분의 근본 실패

1. 비활성 Canvas 아래 Director가 시작하지 않는 문제를 AudioManager 수명주기 설계로 풀지 않고 XR Origin 배치로 우회했다.
2. 테스트 중 관찰해야 할 BGM·Voice Source를 런타임 자동 생성으로 숨겼다. Hierarchy에서 Source별 Clip, Volume, Mute, Spatial Blend, `isPlaying`을 확인할 수 없었다.
3. 씬별 `SceneAudioSettings`의 직접 BGM 참조와 `AudioManagerSettings`의 BGM 라이브러리를 동시에 유지하면서 소유권과 우선순위를 명시하지 않았다.
4. 컴파일·YAML 참조 확인을 실제 Unity/Quest 재생 성공으로 확대 해석했다.

### 영향 범위

- BGM 활성/비활성 라이브러리 체크가 있어도, 씬 직접 참조·페이드 Coroutine·런타임 Source·출력 장치 중 어느 단계에서 실패하는지 한 번에 판별할 수 없다.
- `PPEVoiceFlowDirector`를 XR Origin에서 확인해도 Voice Source가 없으므로, Inspector 재생 확인이 실패하거나 잘못된 오브젝트를 점검하게 된다.
- `DontDestroyOnLoad` Manager의 이전 씬 BGM·Fade Coroutine이 다음 테스트 씬 실행에 남을 위험이 있다.

### 필수 수정 방향

1. 현재 테스트 씬에는 씬 최상위의 항상 활성 `AudioManager`를 직렬화한다.
2. BGM, Voice, UI/SFX, Ambience Source를 그 자식으로 명시적으로 두고 Inspector 참조로 연결한다.
3. BGM·Voice·UI는 2D, 위치 기반 효과음만 3D로 구분한다.
4. Voice Director는 XR Origin이 아닌 씬 전용 AudioManager 또는 별도 활성 Flow 루트에 둔다.
5. 앱 씬이 생기기 전에는 테스트 씬별 Manager를 유지하고, 그때만 전역 `DontDestroyOnLoad` 정책으로 이관한다.

### 검증 게이트

- Unity Play Mode에서 네 Source(BGM/Voice/UI-SFX/Ambience)의 Clip, Volume, Mute, Spatial Blend, `isPlaying`과 실제 청취를 각각 기록한다.
- BGM 라이브러리에서 한 곡을 비활성화했을 때 해당 곡만 멈추고 다른 채널은 유지되는지 확인한다.
- Quest/OpenXR에서 출력 장치, BGM, Voice, 키보드 클릭음, PPE SFX를 각각 확인한다.
- 이 검증 전에는 BGM 또는 XR Origin 나레이션이 정상이라고 보고하지 않는다.

### 2026-08-07 추가 재발: 열린 Unity 씬에 대한 자동 저장 충돌

- AudioManager 전환용 Editor 도구가 `PPEVoiceFlowDirector`가 붙은 `XR Origin (VR)`의 Transform을 재부모화하고 `SaveScene`까지 호출했다. Director 컴포넌트 이동과 호스트 GameObject 이동을 구분하지 못한 구현 오류였다.
- Unity가 열려 있는 상태에서 자동 저장된 씬 변경은 사용자의 메모리상 중간 작업과 충돌할 수 있으며, 이후 `Don't Save`로 재열어도 이미 디스크에 저장된 잘못된 계층은 복구되지 않는다.
- 재발 방지: AudioManager 설정 도구는 AudioManager와 그 AudioSource만 변경하고, XR·Voice Flow·UI·텔레포트는 조회·이동·저장하지 않는다. 도구는 `SaveScene`을 호출하지 않고 Unity Undo/dirty 상태만 사용한다.
- 사용자 수동 저장을 전역으로 차단하지 않는다. 대신 에이전트가 만든 도구의 변경 범위만 사전·사후 검사하며, 대상 씬이 열린 동안 직접 YAML 또는 별도 Unity 배치 저장을 수행하지 않는다.

## 2026-08-05 추가: 모달 Show 후 선택 UI/패널 상태

### 증상

- 카드 오버레이(hover/press)는 켜지지만 `Scenario Detail Modal` / `Modal Panel`이
  기대대로 안 보이거나, 모달이 떠도 `XR UI Canvas` 카드·Heading이 그대로 남는다.
- PPE 착용 교육 확정 후에도 `XR UI Canvas`의 Heading 레이블만 남는다.

### 근본 원인

1. `ScenarioCardSelectProxy.CompleteSelection`은 `modal`이 있으면 `Show`만 호출하고
   `hideAfterSelection`을 실행하지 않았다. 게다가 `hideAfterSelection`이
   `XR UI Canvas` 자체라서 끄면 `TrackedDeviceGraphicRaycaster` `KeyNotFoundException`
   이 난다. 캔버스가 아니라 HUD/Heading 자식을 숨겨야 한다.
2. `SelectIncompletePpeScenario`는 `scenarioSelectionRoot`(HUD)만 끄고 형제
   `Heading`은 남겼다.
3. 씬에 `Scenario Detail Modal`이 활성으로 저장되어 있었고, 버튼이
   훈련 선택지 상태(PPE/표준 버튼 ON, 훈련/뒤로 OFF)로 직렬화되어 있어
   `Show`의 primary 액션과 어긋났다.

### 적용한 변경

- `ScenarioDetailModal.Show`에서 selection HUD + `additionalSelectionUiRoots`(Heading)를
  숨기고, `Modal Panel` 자식을 강제로 켠다. 뒤로가기는
  `HideAndRestoreSelection`으로 선택 UI를 복구한다.
- PPE 확정 시에도 동일하게 HUD/Heading을 숨긴 채 모달만 닫는다. XR UI Canvas는
  끄지 않는다.
- `_scale_0` 씬: `Scenario Detail Modal` 초기 비활성, primary 버튼 상태 복구,
  `additionalSelectionUiRoots`에 Heading 연결.

## 2026-08-05 추가: 문제 오브젝트 공간 진단 하네스

이번 재현에서는 입력 경로를 보기 전에 문제 오브젝트의 월드 위치와 부모 계층을 확인하지 않아, 실제 오브젝트가 룸 밖에 있는 상태를 놓쳤다. 특정 `Modal Canvas`를 정상 기준으로 고정하지 않고, 사용자가 지목한 오브젝트를 먼저 조사해야 한다.

`Assets/Editor/PPEObjectSpatialDiagnosticHarness.cs`의 `Tools > PPE > Diagnose Selected Object Spatial Context` 메뉴를 추가했다. 선택 오브젝트와 자식의 계층, 활성 상태, Transform, `RectTransform` 월드 코너, 부모 Canvas, Graphic/Raycast 설정, Renderer/Collider Bounds 및 씬의 룸 후보 Bounds를 읽기 전용으로 출력한다. 위치 보정이나 입력 변경은 수행하지 않는다.

정적 컴파일은 `Assembly-CSharp-Editor.csproj` 기준 오류 0개를 확인했다. Unity 메뉴 실행과 실제 문제 오브젝트 선택 결과, Play Mode 및 Quest/OpenXR 검증은 아직 수동 확인 대상이다.

## 2026-08-07 추가 해결: Inspector 오디오 프리뷰 무음

### 증상과 영향 범위

- `AudioManager` Inspector의 BGM·Voice·SFX·Ambience `Play`와 `PPEVoiceFlowDirector` Inspector의 Voice Step `Play`가 모두 무음이었다.
- 런타임 AudioManager의 채널 구성과 별개로, 오디오 자산을 테스트할 수 없었다.

### 확인한 사실과 근본 원인

- 대상 씬의 `AudioListener` 1개와 BGM/Voice/SFX/Ambience `AudioSource` 4개는 활성 상태였고, `Mute=0`, `Volume=1`, `Spatialize=0`, 프로젝트 `m_DisableAudio=0`이었다.
- Inspector 프리뷰는 `UnityEditor.AudioUtil.PlayPreviewClip`을 정적 초기화에서 단 한 번만 Reflection 조회했다. Unity 6000에서 API 로드보다 이 조회가 먼저 발생하면 `null` 캐시가 고정되어 이후 버튼이 재생 호출을 하지 못했다.

### 적용 변경

- `Assets/Editor/AudioManagerEditor.cs`
- `Assets/Editor/PPEVoiceFlowDirectorEditor.cs`

두 파일의 Preview/Stop 호출을 버튼 실행 시점의 메서드 조회로 변경했다. 씬 YAML, AudioManager의 Source 참조, 음원 할당, XR Origin 및 Voice Flow 계층은 변경하지 않았다.

### 검증과 남은 수동 검증

- `Assembly-CSharp-Editor.csproj` 빌드 오류 0개.
- 변경 후 사용자가 Inspector 프리뷰가 실제로 출력되는 것을 확인했다.
- `Editor.log`의 `XR: Error setting active audio output driver. Falling back to default.`는 HMD 미연결 OpenXR 초기화의 출력 장치 전환 경고로 별도 관리한다. Inspector 프리뷰 성공만으로 Quest/OpenXR 런타임 출력까지 완료 처리하지 않는다.

## 2026-08-07 추가 해결: Game View 마우스 통합 입력

### 증상과 근본 원인

- 텔레포트 후 시뮬레이터 레이가 원하는 방향으로 나가지 않으면 Game View에서 카드, 모달, PPE 조작을 계속할 수 없었다.
- 프로젝트는 Input System 전용인데 씬의 `XRUIInputModule`에는 `UI/Point`와 `UI/Click` 참조가 없었다. 일부 카드만 `ScenarioDetailModal`의 물리 Raycast fallback으로 처리되어 마우스 입력 경로가 UI마다 달랐다.
- `PPE/hazmat_suit_off/Hazmat Action Panel`에는 `TrackedDeviceGraphicRaycaster`만 있어 일반 마우스 Pointer 이벤트를 받을 수 없었다.

### 적용한 변경

- `PhysicalHmdSimulatorGate`가 물리 `XRHMD`가 없을 때만 프로젝트 입력 자산의 `UI/Point`와 `UI/Click`을 `XRUIInputModule`에 연결한다. 물리 HMD가 있으면 기존 XR Interactor와 `UI Press` 경로를 그대로 사용한다.
- Game View의 마우스 왼쪽 클릭을 상황별로 분기했다. UI가 맞으면 EventSystem이 버튼 클릭을 처리하고, `TeleportationAnchor`가 맞으면 텔레포트하며, `XRGrabInteractable`이 맞으면 직렬화된 오른손 `NearFarInteractor`의 수동 Select를 토글한다.
- 모든 버튼 소유 Canvas에 일반 `GraphicRaycaster`가 있는지 검사하고, 누락된 Hazmat Action Panel에 Unity Editor를 통해 추가했다.
- 씬의 `ScenarioDetailModal` 2개에서 기존 물리 마우스 fallback을 모두 꺼 통합 UI 클릭과의 중복 호출을 차단했다.
- Game View 전용 `Right Controller Click Panel`에서는 Trigger, Grip, A, B, Activate Right, Teleport Stick을 제거하고 `StickClick`(`<Gamepad>/leftStickPress`)만 남겼다. 같은 버튼을 누르는 동안 시뮬레이터의 오른손 조작과 Primary 2D Axis Click을 함께 활성화하므로 별도 `Activate Right` 버튼이 필요하지 않다.
- 대상은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나로 제한했으며 다른 씬 variant는 수정하지 않았다.

### 영향 범위

- Game View: 카드·모달·키보드·PPE 액션 버튼, 텔레포트 Anchor, PPE Grab을 마우스 클릭으로 처리한다.
- HMD: 시뮬레이터 루트와 Game View 마우스 바인딩이 활성화되지 않으므로 Trigger, Grip, Teleport, A/B, 조이스틱 클릭의 기존 XR 로직을 유지한다.

### 완료한 검증과 남은 수동 검증

- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 빌드 오류 0개.
- Unity Edit Mode 직렬화 감사 통과: 입력 참조 4개, 버튼 Canvas 5개, 모달 2개 fallback 해제, `StickClick` 단일 패널을 확인했다.
- HMD가 없는 Play Mode에서 시뮬레이터 활성, `UI/Point`, `UI/Click`, 마우스 입력 활성 및 가상 Gamepad 생성을 확인했다.
- Play Mode에서 `StickClick` Pointer Down/Up을 실행해 누르는 동안 오른손 조작과 Primary 2D Axis Click이 함께 활성화되고, 뗄 때 둘 다 해제되는 것을 확인했다.
- 실제 Game View에서 각 대상 클릭 후 상태 전이와 텔레포트 후 연속 조작은 수동 확인이 필요하다.
- Quest/OpenXR에서 모든 물리 컨트롤러 버튼과 양안 동작은 실기기 확인 전까지 정상 완료로 판단하지 않는다.

## 2026-08-11 추가: `_scale_0` 오른손 Grip이 PPE 마커를 잡지 못함

### 근본 원인

`3_PPE_Room_HandTest_scale_0`의 활성 `XR Item Marker_small` Collider는 Physics Layer 6에
작성되어 있지만, `Right_NearFarInteractor`의 `NearInteractionCaster.m_PhysicsLayerMask`는
Layer 0만 포함하고 있었다. 따라서 오른손 Grip의 `RightHand/Select` 입력이 정상이어도
Select 이전 단계인 근거리 Hover 후보가 만들어지지 않았다.

### 적용한 변경

- 오른손 Near caster 마스크에 Layer 6을 추가해 `Default + Layer 6`(`m_Bits: 65`)로 저장했다.
- Far caster 마스크에는 Layer 6을 추가하지 않았다. PPE 마커는 손을 가까이 가져갔을 때만
  잡히며 원거리 카드 레이가 PPE를 선택하지 않는 기존 불변조건을 유지한다.
- 왼손 Near caster 마스크는 변경하지 않았다. 이번 테스트의 잡기 입력은 오른손 Grip만
  대상으로 한다.
- `Tools > PPE > Validate Scale 0 Right Grip Marker Reachability` 검증을 추가했다. 활성 PPE
  마커가 오른손 Near caster에는 포함되고 Far caster에는 제외되는지 검사한다.

### 검증 구분

- 정적 확인: 오른손 `Select`는 `RightHand/GripButton`, interactor handedness는 `Right`, 활성
  PPE 마커는 Layer 6이며 오른손 Near/Far 마스크가 각각 포함/제외 관계인지 확인했다.
- Unity Editor 확인: 새 하네스 실행과 Play Mode Grip 선택 확인이 필요하다.
- Quest/OpenXR 확인: 실제 오른손을 마커에 가까이 둔 상태에서 첫 Grip으로 Toggle Grab,
  두 번째 Grip으로 해제되는지 확인이 필요하다.

## 2026-08-11 추가: `_scale_0` 양손 Grip과 텔레포트 테스트

### 변경 전 필수 판단

- 이번 변경이 대응하는 요청: 양손 Grip으로 PPE 잡기/놓기, 양손 Trigger로 패널 선택,
  PPE 구역에서 양손 조이스틱 텔레포트를 사용한다.
- 보존할 동작: Trigger의 XRI UI 선택 경로, PPE marker의 근거리 전용 선택, 패널 상태와
  음성 재생 규칙, 씬 작성 Transform과 UI 값은 변경하지 않는다.
- 단일 기준: 잡기는 각 손 `NearFarInteractor`, UI 선택은 각 손 `UI Press`, 이동 허용은
  `PPEControllerTeleportModeManager`가 소유한다.
- 전체 경로: `GripButton -> XRI Left/Right Interaction/Select -> Near caster -> Layer 6 marker
  Collider -> XRGrabInteractable -> PPEInspectionState`; `TriggerButton -> UI Press ->
  NearFarInteractor -> TrackedDeviceGraphicRaycaster -> Button`; `Primary2DAxis -> XRI Left/Right
  Locomotion/Teleport Mode -> PPEControllerTeleportModeManager -> XRRayInteractor ->
  TeleportationAnchor`다.
- 실패 시 자동 수리하지 않고 하네스 실패와 명시적 참조 오류로 중단한다.
- 영향 소비자는 양손 PPE Grab, 패널 UI, 텔레포트, `PpeArea` 시작 이동 게이트다.
- 기준 비교는 직전 동작 확인 커밋 `19dabbc`와 이 변경 후 정적 하네스/Unity/Quest 실행을
  구분한다.

### 근본 원인과 적용한 변경

- 왼손 Near caster도 오른손과 동일하게 활성 PPE marker의 Layer 6을 포함하도록
  `m_PhysicsLayerMask`를 `65`로 작성했다. 양손 Far caster는 계속 Layer 6을 제외한다.
- `_scale_0`의 활성 `PPEInspectionState.rightHandGrabOnly`를 끄고, 다른 씬의 기본값은
  변경하지 않았다.
- 양손 Trigger는 이미 XRI `UI Press`에 연결되어 있어 새 입력 소비자를 추가하지 않았다.
- `_scale_0`은 `PpeArea`부터 시작해 이전 시나리오 선택 신호를 의도적으로 건너뛴다.
  따라서 이 씬의 양손 텔레포트 관리자에서만 `m_RequireScenarioSelection`을 끄고,
  `PPEVoiceFlowDirector`가 `TeleportInstruction`, `PpeArea`, `Completed`로 직접 시작할 때는
  이동 음성 게이트를 열린 상태로 초기화하도록 했다.
- XRI 기본 자산의 `Teleport Mode Cancel`도 Grip을 소비하므로 `_scale_0`의 양손 텔레포트
  관리자에서는 해당 참조를 제거했다. 텔레포트는 조이스틱 방향 입력과 조이스틱을 놓을 때
  발생하는 `Teleport Mode.canceled`만 사용하며 Grip은 PPE Select에만 남긴다.
- `Tools > PPE > Validate Scale 0 Controller Test Inputs`가 양손 Near/Far marker 범위,
  오른손 전용 필터 해제, 텔레포트 게이트 설정, authored XRI 가이드 액션을 함께 검사한다.

### 검증 구분

- 정적 확인: 씬과 XRI InputActionAsset의 위 경로를 확인했다.
- Unity Editor 확인: 하네스 실행, 양손 Grip 잡기/놓기, 양손 Trigger 버튼 선택,
  양손 조이스틱 텔레포트 확인이 필요하다.
- Quest/OpenXR 확인: 실제 컨트롤러 입력과 HMD 양안 상태는 실기기 확인 전까지 완료로
  판정하지 않는다.

## 2026-08-13 추가: 3버튼 컨트롤러 가이드와 PPE 교육 직접 선택 흐름

### 이번 변경이 대응하는 요청

- 컨트롤러 가이드를 `Trigger → Grip → Joystick` 세 단계 이미지로 진행한다.
- 카드 선택 직후 기존 `교육 선택/돌아가기` 중간 단계를 표시하지 않고,
  `PPE 착용교육`과 `밀폐공간 진입 전 안전 교육` 버튼이 있는 모달을 바로 표시한다.
- 현재는 `PPE 착용교육`을 선택하면 새 모드 선택 화면을 노출하지 않고 기존 텔레포트 안내로
  진행한다.
- 향후 사용할 `교육/훈련/테스트` 버튼은 씬에 작성된 비활성 상태를 유지하면서 선택 이벤트
  연결만 준비한다.

### 근본 원인

- 씬의 `Modal Panel`에는 기존 `Training Select Button`과 `Back Button`이 비활성이고 다음 단계
  버튼들이 활성인 상태로 저장되어 있었지만, `ScenarioDetailModal.Show()`가 실행될 때마다 버튼
  활성 상태를 이전 흐름으로 다시 덮어썼다.
- `PPEVoiceFlowDirector.NotifyPpeEducationSelected()`는 중간 `EducationSelected` 상태에서만
  전이를 허용했다. 따라서 카드를 선택한 직후 `ModalDetail`에서 PPE 버튼을 직접 누르는 새
  흐름은 `PpeEducationSelected`로 진행할 수 없었다.
- 가이드 씬 값에는 새 `1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick` 이미지가 있었지만,
  Grip 상태에 Joystick 음성이 섞여 있었고 이후 A/B 패널 상태로 계속 진입했다.

### 적용한 변경

- `_scale_0`의 `ScenarioDetailModal.openWithTrainingChoices`를 활성화했다. 카드 선택 시
  `PPE Education Scenario Button`, `Safety Training Scenario Button`,
  `Training Choice Back Button`을 바로 표시하고, 숨겨진 기존 버튼을 다시 켜지 않는다.
- 직접 선택 화면의 돌아가기는 제거된 중간 화면으로 돌아가지 않고 카드 선택 화면으로 복귀한다.
- 직접 선택 화면의 초기 EventSystem 선택 대상은 숨겨진 기존 버튼이 아니라
  `PPE Education Scenario Button`이다.
- `ModalDetail`과 기존 `EducationSelected` 양쪽에서 PPE 착용교육 선택을 받아
  `PpeEducationSelected → TeleportInstruction` 흐름으로 진행하도록 했다.
- 가이드 음성과 이미지를 다음과 같이 재배치했다.
  - Trigger: `001_Start`, `002_RayTrigger` + `1_Ctrl_Trigger`
  - Grip: `003_GripGrab_Release` + `2_Ctrl_Grip`
  - Joystick: `004_Joystick_Marker`, `005_Joystick_Ray_T` + `3_Ctrl_Joystick`
- Joystick 단계의 다음 상태를 `CardIntro`로 연결해 기존 `ControllerPanel` A/B 단계를 건너뛴다.
- 비활성 `Modal Panel (1)`의 `PPE Edu Mode`, `PPE Training Mode`, `PPE Test Mode`를
  직렬화 참조로 연결하고 `PpeLearningModeSelected` 이벤트를 준비했다. 이 변경에서는 해당
  패널이나 버튼을 활성화하지 않으며 선택 후 결과 화면·상태 전이도 임의로 추가하지 않았다.
- `Tools > PPE > Validate Scale 0 Controller Test Inputs`에 세 가이드의 음성·이미지·다음 상태,
  직접 PPE 모달 설정, 현재 버튼 활성 상태와 미래 모드 버튼 비활성 상태 검사를 추가했다.

### 영향 범위와 보존한 동작

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나다.
- 카드 Trigger 입력, 모달의 XRI UI Raycast, PPE 선택 후 BGM 페이드아웃, 텔레포트 음성 게이트,
  PPE Grab, Action Panel과 퀴즈 흐름은 기존 동작을 유지한다.
- `Safety Training Scenario Button`과 미래 교육/훈련/테스트의 후속 콘텐츠는 아직 정의하지
  않았으므로 자동 선택, fallback 또는 임의 상태 전이를 추가하지 않았다.

### 완료한 검증

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 순서대로 빌드해 오류 0개를
  확인했다. 출력된 경고는 기존 패키지·샘플 참조 경고다.
- `_scale_0` 씬에서 세 가이드 이미지, 음성 클립 순서, `ControllerRayT → CardIntro`, 직접 모달
  버튼과 미래 모드 버튼 FileID가 유효한 기존 오브젝트를 참조하는지 정적으로 확인했다.

### 아직 필요한 수동 검증

1. Unity Play Mode에서 가이드가 Trigger, Grip, Joystick 순으로 표시되고 A/B 패널 단계가
   나타나지 않는지 확인한다.
2. 카드를 Trigger로 선택하면 중간 `교육 선택/돌아가기` 화면 없이 PPE 착용교육과 안전 교육
   버튼이 바로 보이는지 확인한다.
3. `PPE 착용교육` Trigger 선택 시 새 교육/훈련/테스트 패널이 나타나지 않고 기존 텔레포트
   안내와 텔레포트존으로 진행하는지 확인한다.
4. 직접 선택 화면의 돌아가기로 카드 선택 화면에 복귀하는지 확인한다.
5. Quest/OpenXR에서 좌우 Trigger UI 선택, 세 가이드 표시와 텔레포트 이동을 확인한다.

## 2026-08-13 정정: 저장된 별도 상세 모달 계층 기준

- 실제 카드 선택 후 표시 대상은 `Modal Canvas/Scenario Detail Modal/Modal Panel_1`이다.
  이전 기록의 `Modal Panel` 및 `Modal Panel (1)` 표현은 현재 저장 계층을 정확히 가리키지
  못했으므로 이 항목을 기준으로 정정한다.
- `Modal Panel_0`은 비활성인 이전 패널이며 현재 흐름의 참조 대상으로 사용하지 않는다.
- `Modal Panel_1/1_EduChoice`는 활성 작성 상태이고 PPE 착용교육, 밀폐공간 진입 전 안전 교육,
  돌아가기 버튼을 포함한다.
- `Modal Panel_1/2_Mode`는 비활성 작성 상태이며 PPE 교육/훈련/테스트와 돌아가기 버튼을
  포함한다. 현재 PPE 착용교육 선택은 이 그룹을 표시하지 않고 기존 텔레포트 흐름으로 간다.
- `ScenarioDetailModal`의 번호, 제목, 설명, 버튼 및 두 그룹 참조를 모두 `Modal Panel_1`
  계층의 저장된 오브젝트로 다시 연결했다. 런타임은 위치·크기·색상·TMP 작성값을 덮어쓰지 않고
  두 그룹의 표시 상태만 전환한다.

## 2026-08-25 후속: Game View 화살키 이동·손 시각 숨김

### 변경 전 필수 판단

1. 기존 Inspector/씬 작성값은 보존한다. Quest용 `moveSpeed=1`, XR Trigger/Grip, UI 마우스 클릭, 텔레포트, 손 모델의 authored active 상태는 변경하지 않고 Game View 전용 배율·회전 속도·손 시각 참조만 추가한다.
2. 이동 상태 소유자는 `PPEConfigurableDynamicMoveProvider`, Game View 여부와 카메라 회전·손 숨김 소유자는 `PhysicalHmdSimulatorGate`, 미니 컨트롤러 토글 소유자는 `ControllerGuideMiniActivator`다.
3. 입력 경로는 `Keyboard 화살키 → Game View 전용 Move reader → PPEConfigurableDynamicMoveProvider → XRI Locomotion Mediator → CharacterController`, `←/→ → Game View gate → XR Origin yaw`, `Mouse click → 기존 XRUIInputModule/수동 Grab·Trigger 보조 경로`, `STICK CLICK → 가상 Gamepad leftStickPress → ControllerGuideMiniActivator`다.
4. 필수 Gate·Move Provider·XR Origin·손 시각 참조가 없으면 런타임 자동 검색·생성하지 않고 명확한 오류로 멈추며, 수리는 명시적 Editor 메뉴로만 수행한다.
5. 영향 소비자는 Editor Game View의 이동·회전·손 렌더링과 미니 컨트롤러 토글이다. Quest/OpenXR 이동 속도·양손 입력, PPE 장착 상태, UI, 오디오, 텔레포트와 Collider는 보존한다.
6. 변경 전 기준은 화살키가 이동이 아니라 Simulator Mouse Delta 조준에 연결되고, Move Provider 속도는 1m/s이며, Game View에서 활성 맨손과 컨트롤러 모델이 함께 보이는 상태다. 변경 후 `↑/↓` 이동, `←/→` 회전, Game View 4배 이동, PPE 손 Renderer 숨김을 비교한다.
7. 정적 배선, Unity 컴파일, Editor 하네스, Play Mode 키 입력·이동 거리·회전·손 Renderer·미니 컨트롤러 토글·Collider 차단을 확인한다. Quest/OpenXR 입력과 양안은 별도 수동 검증으로 남긴다.

이번 변경이 대응하는 요청은 Game View에서 기존 마우스 클릭 Trigger/Grip을 보존하고, 화살키로 전후 이동과 좌우 방향 조정, 빠른 이동, PPE 손 모델 숨김, 조이스틱 클릭으로 미니 컨트롤러 토글을 제공하는 것이다.

### 근본 원인과 적용한 변경

- 기존 Game View 보조 코드가 화살키를 XR Device Simulator의 `Mouse Delta` 조준 입력에 동적으로
  추가하고 있어, 사용자가 기대한 전후 이동과 좌우 방향 조정이 아니었다. 해당 화살키 조준
  추가만 제거하고 기존 마우스 클릭·Grab·Trigger 보조 경로는 유지했다.
- `↑/↓`는 Game View 시뮬레이터가 활성이고 기존 시나리오가 Move Provider를 허용한 동안에만
  XRI 이동 입력으로 읽는다. 이동은 기존 `Locomotion Mediator`와 `CharacterController`를 통과한다.
- Game View에서만 기존 작성 속도에 4배를 적용한다. Quest/HMD의 작성값 `moveSpeed=1`은
  변경하지 않았다.
- `←/→`는 Game View에서만 XR Origin을 초당 120도로 좌우 회전한다.
- PPE 양손의 작성된 시각 루트 14개 아래 Renderer 24개는 Game View 동안
  `forceRenderingOff`로 숨기고, Play Mode 종료 시 각 Renderer의 이전 값을 복원한다. 손 장착
  상태와 GameObject 활성 상태는 변경하지 않는다.
- `STICK CLICK → <Gamepad>/leftStickPress → ControllerGuideMiniActivator`의 기존 미니
  컨트롤러 토글 경로는 변경하지 않았다.
- 중간 검사에서 Game View용 Move Provider를 강제로 활성화하면 기존 시나리오의 이동 잠금
  시점을 우회할 수 있음을 확인했다. 강제 활성화 코드는 최종 변경에서 제거했으며,
  `PPEVoiceFlowDirector`가 계속 이동 허용 상태를 소유한다.

### 완료한 검증

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드는 모두 오류 0개다. 출력 경고는
  기존 Unity AI 패키지 참조 충돌과 폐기 예정 API 경고다.
- Unity가 런타임·에디터 어셈블리를 다시 생성했고 C# 컴파일 오류는 0개였다.
- `Tools > PPE > Game View > Apply Arrow Navigation and Hidden Hands`와 검증 하네스가 대상 씬
  `Assets/Scenes/3_PPE_Room_3mode_loco.unity`에서 통과했다.
- Play Mode에서 `gameView=True`, 기존 시나리오 시작 이동 잠금 `moveEnabled=False`, Quest 작성
  속도 `1`, Game View 배율 `4`, 손 시각 루트 `14`, 숨겨진 Renderer `24`, 표시된 손 Renderer
  `0`을 확인했다. 따라서 Game View 편의 기능이 HMD 작성값과 시나리오 이동 잠금을 덮어쓰지
  않는다.
- 테스트 중 임시로 변경한 Move Provider 상태와 키보드 장치는 원래 상태로 복원하고 Play Mode를
  종료했다.

### 아직 필요한 수동 검증

1. Game View를 클릭한 뒤 이동 허용 단계에서 `↑/↓` 전후 이동과 `←/→` 좌우 회전을 실제
   키보드로 확인한다. Windows 합성 키는 Unity Input System의 Raw Input 장치로 전달되지 않아
   자동 거리 측정의 성공 증거로 사용하지 않았다.
2. 이동 속도가 기존 Game View보다 체감상 충분히 빠른지 확인한다. 현재 배율은 4배다.
3. 조이스틱 클릭용 `STICK CLICK`을 눌러 미니 컨트롤러가 토글되는지 확인한다.
4. Quest/OpenXR에서 기존 스틱 속도, 양손 표시, Trigger/Grip, 양안 렌더링이 그대로인지 확인한다.

## 2026-08-25 후속: Game View 배경 클릭 음성 건너뛰기

### 변경 전 필수 판단

1. 기존 Inspector·씬·UI 작성값은 변경하지 않는다. Game View 마우스 클릭 판정과 기존 음성
   상태 처리 코드만 연결한다.
2. 음성 단계와 다음 상태 전이는 `PPEVoiceFlowDirector`, Game View 마우스의 UI·Physics 대상
   판정은 `PhysicalHmdSimulatorGate`가 계속 소유한다.
3. 입력 경로는 `Mouse left click → EventSystem UI raycast → Camera Physics.Raycast →
   Teleport/Tablet/XRGrabInteractable 처리 → 처리 대상이 아닐 때 PPEVoiceFlowDirector의 기존
   상태별 skip 규칙`이다.
4. `PPEVoiceFlowDirector` 참조가 없으면 런타임 검색·생성하지 않고 한 번의 명확한 오류로
   중단하며, 수리는 명시적 Editor 메뉴로 수행한다.
5. 영향 소비자는 Game View 음성 스킵, PPE Grab, 태블릿, 텔레포트, 주요 UI와 음성 단계
   전이다. HMD Trigger/Grip 입력과 Quest 표시·이동은 변경하지 않는다.
6. 변경 전 기준은 Game View에서 벽·가구·빈 공간을 클릭해도 음성이 계속 재생되는 상태다.
   변경 후에는 PPE나 주요 UI 클릭은 기존 상호작용을 우선하고, 그 밖의 클릭만 기존 Trigger
   스킵과 동일한 상태 전이를 사용한다.
7. 정적 배선과 C# 컴파일, Unity 하네스, Play Mode에서 UI/PPE 보호 및 배경 클릭 스킵을
   구분해 확인한다. Quest/OpenXR은 동작 변경 대상이 아니며 별도 회귀 확인으로 남긴다.

### 적용한 변경

- `PPEVoiceFlowDirector`의 기존 Trigger 음성 스킵 처리를 공통 상태 안전 메서드로 분리했다.
  Game View 배경 클릭도 같은 규칙을 사용하므로, 단순히 AudioSource만 정지해 다음 단계가
  걸리는 동작을 만들지 않는다.
- 클릭 가능한 UI가 마우스 Raycast를 소유하면 기존 UI 이벤트를 우선한다.
- 텔레포트, 태블릿, 활성 `XRGrabInteractable`이 Physics Raycast 대상이면 기존 상호작용을
  우선하고 배경 스킵을 호출하지 않는다.
- Physics Raycast가 빗나가거나 벽·바닥·가구처럼 위 대상이 아닌 Collider를 맞힐 때만 현재
  재생 중인 음성을 건너뛴다. 음성이 없으면 아무 상태도 바꾸지 않는다.
- 씬 작성 `PPEVoiceFlowDirector` 참조를 Gate에 연결했으며 런타임 자동 검색은 추가하지 않았다.

### 완료한 검증

- 런타임·에디터 C# 빌드는 오류 0개다. 최초 병렬 실행의 런타임 빌드 하나가 공유 출력 DLL
  파일 잠금으로 실패했으나 단독 재실행은 통과했다.
- Unity Editor 하네스가 Game View Gate의 작성된 음성 흐름 참조, 기존 마우스 UI/Grab 참조,
  `STICK CLICK`, CharacterController 배선을 함께 검증해 통과했다.
- Play Mode에서 `EducationSelected` 음성 재생 중 공통 스킵을 실행해 반환값 `True`, 상태
  `EducationSelected → TeleportInstruction`, 음성 재생 `True → False`를 확인했다. 이는 자연
  완료와 같은 상태 전이를 사용하며 음성만 정지해 흐름을 고립시키지 않는다.

### 아직 필요한 수동 검증

1. Game View에서 실제 마우스로 벽·가구·바닥과 빈 공간을 클릭해 음성이 건너뛰어지는지 확인한다.
2. PPE, 태블릿, 텔레포트와 주요 UI 버튼 클릭 시 음성 스킵보다 기존 상호작용이 우선하는지 확인한다.

### 2026-08-25 Game View 이동 속도 재조정

- 실제 키보드 이동이 너무 빨라 뒤쪽 Exit까지 빠지는 재현 결과에 따라 Game View 전용 배율을
  기존 4배의 60%인 `2.4배`로 낮춘다.
- Quest/HMD 작성 속도 `moveSpeed=1`, 시나리오 이동 잠금과 CharacterController 경로는
  변경하지 않는다.

### 2026-08-25 Game View 회전 후 전후 이동 방향 교정 전 판단

1. 씬의 HMD용 `Head Transform`, 이동 속도, UI와 XR 입력 작성값은 변경하지 않는다.
2. 이동 방향 상태는 `PPEConfigurableDynamicMoveProvider`, Game View의 실제 카메라 참조는
   `PhysicalHmdSimulatorGate`가 소유한다.
3. 입력 경로는 `↑/↓ → Game View arrow reader → Game View일 때만 XR Origin Camera 기준 →
   DynamicMoveProvider → Locomotion Mediator → CharacterController`다.
4. 작성된 XR Origin 또는 Camera 참조가 없으면 자동 검색하지 않고 명확한 오류로 멈춘다.
5. 영향 소비자는 Game View 화살키 이동 방향뿐이다. HMD 스틱 이동 기준과 속도는 보존한다.
6. 변경 전에는 Move Provider의 `Head Transform`이 별도 `Hand Tracking Camera`를 가리켜 XR Origin
   회전 뒤에도 이전 축으로 이동한다. 변경 후에는 Game View 동안만 실제 XR Origin Camera를
   사용하고 Provider 비활성화 시 원래 참조를 복원한다.
7. 정적 참조, C# 컴파일, Unity 하네스와 Play Mode에서 회전 전후 카메라 forward·복원 여부를
   확인한다. 실제 키보드 이동 체감과 Collider 통과 여부는 수동 확인한다.

#### 적용 및 검증 결과

- Game View에서 Move Provider가 활성화될 때만 `headTransform`을 Gate가 참조하는
  `XR Origin (VR)/Camera Offset/Main Camera`로 교체한다.
- Provider 비활성화 시 씬에 작성된 기존 `Hand Tracking Camera` 참조를 복원한다. HMD 세션에서는
  Gate가 비활성이므로 교체를 실행하지 않는다.
- 런타임·에디터 C# 빌드는 오류 0개이며 Unity 하네스가 Head Relative 설정과 작성된 XR Origin
  Camera 참조를 확인해 통과했다.
- Play Mode에서 Game View 이동 기준이 `Main Camera`로 교체된 상태를 확인했다. 실제
  `XR Origin (VR)`을 90도 회전했을 때 Camera forward도 `90.00001도` 회전했고 Move Provider
  forward와의 내적은 `1`이었다.
- Provider를 원래 비활성 상태로 돌린 뒤 `Hand Tracking Camera` 참조가 복원됐음을 확인했다.
- 실제 키보드로 회전 후 `↑/↓` 이동과 벽 Collider 차단을 한 번 더 확인해야 한다.

### 2026-08-25 후속 작업: Game View 그립 PPE의 몸 접촉·착용 미완료

- 사용자 Play Mode 시험에서 Game View 키보드·마우스 클릭으로 PPE가 그립 상태에는 들어가지만,
  장비가 몸 쪽까지 따라오지 않고 중간에 걸린 것처럼 머물러 몸 접촉 착용 처리가 완료되지 않는
  현상을 확인했다.
- 이번 저장에서는 원인을 확정하거나 입력·Grab·착용 로직을 추가 변경하지 않는다. 환경 Collider
  이동 차단 성공과 이 PPE 그립 문제를 서로 다른 결과로 기록한다.
- 후속 조사에서는 `Mouse click → NearFarInteractor 수동 Select → XRGrabInteractable Attach Transform
  → XR Device Simulator 컨트롤러 포즈 → PPE Body Anchor/몸 접촉 Collider → 착용 상태 소유자`의
  전체 경로를 순서대로 확인한다.
- 특히 새 환경 차단체가 PPE 이동을 막는지, Game View의 가상 컨트롤러 위치가 카메라·몸 이동을
  따라가는지, 그립된 PPE의 Rigidbody/Collider가 몸 접촉 Trigger에 도달하는지를 각각 분리해서
  재현한다.
- 완료 조건은 Game View에서 그립한 정상 PPE를 몸 내부까지 가져갔을 때 기존 HMD 착용 상태 전이와
  같은 착용 처리가 발생하고, 불량 PPE와 시나리오상 금지 PPE 규칙은 그대로 유지되는 것이다.

#### 2026-08-25 변경 전 판단 기록: 마우스 포인터 가상 손

- 이번 변경이 대응하는 사용자 요청은 Game View에서 옷 마커를 클릭하면 선택이 유지되고, 마우스
  포인터가 가상 손처럼 옷을 몸 부착면까지 옮기며, 몸 안쪽에서 다시 클릭하면 기존 착용 판정을
  실행하는 것이다.
- 보존해야 하는 기존 동작은 Quest/OpenXR의 실제 손·컨트롤러 Grab/Activate, 마커 전용 Collider,
  오염 PPE 거부, 시나리오상 착용 순서 제한, 텔레포트·UI·배경 클릭·음성 스킵이다.
- Inspector/씬 작성값은 유지한다. Game View 전용 가상 손 Anchor는 에디터 설정 명령이 씬에 한 번
  작성하고, 런타임은 해당 직렬화 참조와 기존 PPE Body Anchor만 사용한다.
- 단일 상태 소유자는 Select에 대해 기존 오른손 `NearFarInteractor`, 착용 결과에 대해
  `PPEActionPanelController`와 `PPEHazmatEquipController`이다. Gate가 별도의 착용 완료 상태를 만들지
  않는다.
- 입력 전체 경로는 `Mouse left click -> Main Camera screen ray -> PPE marker Collider ->
  PhysicalHmdSimulatorGate -> right NearFarInteractor manual Select -> Game View mouse grab Anchor ->
  XRGrabInteractable -> PPEActionPanelController body proximity -> second mouse click as Activate ->
  기존 ResolveUseChoice -> PPEHazmatEquipController`이다.
- 필수 Gate/Interactor/Anchor/marker 참조가 없으면 런타임 자동 생성·자동 수리하지 않고 한 번의 명확한
  오류를 기록한다. Anchor 생성과 참조 연결은 명시적인 에디터 설정 명령으로만 수행한다.
- 함께 영향을 받는 소비자는 Game View PPE Grab과 몸 접촉 착용이다. UI, 텔레포트, HMD 입력,
  거울/XR 양안 렌더링은 변경하지 않는다.
- 변경 전 기준 실행은 Game View에서 정상 방호복 Select 로그는 발생하지만 옷이 포인터를 따라오지 않고
  `UseApproved`/`[PPE Equip]`이 발생하지 않는 상태다. 변경 후에는 첫 클릭 Select 유지, 포인터 이동에
  따른 marker 이동, 몸 부착 범위 밖 두 번째 클릭의 선택 유지, 범위 안 두 번째 클릭의 기존 착용 판정을
  각각 비교한다.
- 검증 범위는 정적 C# 컴파일과 에디터 하네스, Unity Game View Play Mode까지로 구분한다.
  Quest/OpenXR 실제 손 입력과 양안 결과는 이번 Game View 전용 변경의 완료 증거로 확장하지 않는다.

#### Play Mode 추가 근본 원인

- 첫 구현 검증에서 정상 방호복 marker와 가상 손 Anchor의 거리는 `0.00000054m`로 일치했고 Select도
  오른손 `NearFarInteractor`에서 유지됐다. 따라서 첫 클릭 직후 선택 해제가 근본 원인은 아니었다.
- 그러나 방호복 몸 부착 목표는 Game View의 `XR Origin (VR)/Main Camera`가 아니라 HMD 경로의
  `Hand Tracking Camera`를 계속 기준으로 계산해 `(0, 1.21, 0.18)`에 남아 있었다. 현재 Game View에서
  이 지점은 화면 밖이므로 포인터가 몸 부착 범위에 도달할 수 없었다.
- Game View Simulator가 활성일 때만 `PPEHazmatEquipController`의 런타임 head override를 씬에 작성된
  Main Camera로 설정하고, Gate가 비활성화되면 기존 직렬화 `headTransform`으로 복원한다. HMD 경로의
  작성값 자체는 변경하지 않는다.

#### 적용 및 검증 결과

- 첫 클릭이 간헐적으로 무시된 직접 원인은 비활성 표시 상태의 공간 키보드 `D` 키였다. 해당 키의
  `Graphic.color.a`는 `0`이지만 `raycastTarget`과 클릭 핸들러가 남아 있어 옷 마커 앞에서 Game View
  마우스 입력을 가로챘다. Game View 직접 물리 입력 판정에서는 완전히 투명하거나 비활성인 `Graphic`을
  대화형 UI로 취급하지 않도록 제한했다. 보이는 UI 버튼의 우선권은 유지한다.
- 정상 방호복 마커를 첫 클릭한 뒤 선택은 오른손 `NearFarInteractor`에 유지됐고, 마커와 가상 손
  Anchor 거리는 `0.00000035m`였다.
- 포인터를 Game View 화면 아래쪽 몸 영역으로 옮겼을 때 마커 중심과 몸 부착 목표 거리는
  `0.2462679m`였으며 기존 몸 접촉 판정 `IsBodyProximityActivateAttempt()`가 `True`가 됐다.
- 같은 위치에서 두 번째 클릭한 결과 `LastChoice=Use`, `LastResult=UseApproved`, 수동 Select 해제,
  `PPEHazmatEquipController.IsEquipped=True`를 확인했다. 장착 방호복 Renderer도 `1/1` 활성 상태였다.
- `Assembly-CSharp`와 `Assembly-CSharp-Editor` 빌드는 오류 0개였고, 에디터 설정 하네스와 Play Mode
  실행 뒤 Unity Console 오류도 0개였다.
- Quest/OpenXR 실제 손 입력, 양안 렌더링과 헤드셋에서의 장착 위치는 이번 Game View 검증에 포함하지
  않았다. 해당 경로의 직렬화 작성값과 입력 소비자는 변경하지 않았다.

### 2026-08-25 방호복 3종 005·006 음성 연결 변경 전 판단

1. 기존 Inspector/씬 작성값은 유지하며, 사용자가 교체한 `4_VO_PPE_EDU_005_UsePPE.mp3`의 기존 GUID와
   006 음성 참조를 그대로 사용한다. 세 방호복 패널 목록만 씬에 명시적으로 직렬화한다.
2. 방호복 음성 상태 소유자는 `PPEVoiceFlowDirector`, 착용 승인 상태 소유자는 각
   `PPEActionPanelController`와 `PPEHazmatEquipController`이다.
3. 입력 경로는 `방호복 marker -> XRGrabInteractable.selectEntered -> 005`, 착용 경로는
   `UseApproved -> ChoiceResolvedWithSource -> HazmatSuit 판정 -> 006 -> AudioManager Voice AudioSource`이다.
4. 방호복 패널이나 005·006 참조가 누락되면 런타임에서 검색·생성하지 않고 에디터 검증이 실패하도록
   한다. 씬 연결은 명시적인 에디터 설정 명령으로 수행한다.
5. 영향을 받는 소비자는 교육 모드의 세 방호복 Grab 005와 정상 방호복 착용 006이다. 다른 PPE 음성,
   Training/Test 음성, SFX, UI, 텔레포트와 Quest 입력은 변경하지 않는다.
6. 변경 전 기준 실행에서 `_Clean` 첫 클릭은 Select되지만 Voice Source는 Welcome 음성을 유지했고,
   착용은 `UseApproved`와 006 clip 지정까지 됐지만 `AudioSource.isPlaying=False`, `timeSamples=0`이었다.
7. 변경 후 정적 참조·컴파일, 에디터 하네스, Unity Play Mode에서 `_Clean`의 005 재생과 착용 직후 006
   재생을 확인한다. Quest/OpenXR 출력 장치 청취는 별도 수동 검증으로 구분한다.

#### 2026-08-25 적용 결과: 005·006·SuitAlready

- 사용자가 교체하고 이름을 바꾼 `4_VO_PPE_EDU_005_UsePPE.mp3`는 기존 GUID를 유지하므로 씬의 005
  음성 참조가 해당 파일을 가리키도록 보존했다.
- 005가 `_Clean`에서 나오지 않은 원인은 `PPEVoiceFlowDirector`가 `_Ripped` 패널 하나만 Grab 신호에
  연결하고 있었기 때문이다. 단일 참조를 `_Clean`, `_Contam`, `_Ripped` 세 패널의 직렬화 배열로
  바꾸고 세 패널 모두 같은 005 안내를 사용하도록 연결했다.
- 첫 005·006 재생이 시작되지 않은 원인은 두 AudioClip의 `preloadAudioData`가 꺼진 상태에서 최초
  `PlayVoice` 시점에도 데이터가 `Unloaded`였기 때문이다. 씬에 작성된 005, 006과 `SuitAlready` 참조만
  `OnEnable`에서 `LoadAudioData()`로 준비하며, 누락 참조를 런타임에서 검색하거나 생성하지 않는다.
- 이미 방호복을 입은 뒤 다른 방호복을 사용하면 결함 판정보다 먼저 `SuitAlready`를 재생하는 기존
  선행 검사가 있었지만, 세 패널의 `voiceFlowDirector` 참조가 모두 비어 있어 해당 검사를 우회하고
  있었다. 세 패널에 씬의 `PPEVoiceFlowDirector`를 명시적으로 연결했다.
- `PPEHazmatVoiceSetup.Validate()`는 세 패널 배열, 각 패널의 흐름 참조, 005 `UsePPE`, 006 `Suit_End`,
  204 `SuitAlready`의 정확한 씬 참조를 검사해 통과했다.
- Unity Play Mode 교육/밀폐공간 작업계획에서 `_Clean` Grab 직후 005는 `Loaded`, `isPlaying=True`,
  몸 부착 사용 직후 결과는 `UseApproved`이며 006도 `Loaded`, `isPlaying=True`였다.
- 첫 방호복 장착 후 `_Ripped`를 몸 부착 사용한 회귀 검증에서 장착 상태는 유지됐고 패널 결과는
  결함 사용 거절로 확정되지 않은 `None`이었다. Voice Source는
  `4_VO_PPE_EDU_204_SuitAlready`, `Loaded`, `isPlaying=True`여서 결함 PPE 음성보다 선행함을 확인했다.
- Quest/OpenXR 헤드셋의 실제 스피커 출력 청취는 수행하지 않았다. 이번 결과는 정적 씬 검증과 Unity
  Editor Play Mode의 실제 AudioSource 상태까지의 확인이다.

### 2026-08-25 컨트롤러 가이드 이미지 구조 변경 전 판단

1. 사용자가 씬에서 수정한 이름, Sprite, `activeSelf`를 기준값으로 보존한다. 런타임이나 Editor 도구가
   이를 이전 `Panel`, `3_Ray_T`, `Ray_T_B/R` 구성으로 되돌리지 않게 한다.
2. 가이드 상태는 `PPEVoiceFlowDirector`가 소유하고, 표시 대상은 씬의
   `ControllerGuide/Context` 아래 `1_Ray`, `2_Marker`, `3_Exit_Marker` 세 그룹이다.
3. 경로는 `Controller voice clip index → VoiceStep.controllerGuideVisuals → 그룹 활성화`이며,
   컨트롤러 쪽 동반 이미지는 `controllerGuideCompanionVisual`이 소유한다. 런타임은 그룹 활성 상태만
   전환하고 사용자가 작성한 `Card/Item/Place` 자식의 Sprite와 활성 상태는 덮지 않아야 한다.
4. 필수 그룹이나 참조가 누락되면 런타임 검색·생성·이름 fallback을 추가하지 않고 Editor 회귀 검증이
   정확한 계층 경로와 참조를 보고하도록 한다.
5. 영향 소비자는 상세/간단 컨트롤러 가이드의 Trigger·Grip·Joystick 시각 전환이다. 음성, PPE Grab,
   UI 레이캐스트, 텔레포트, 방호복 착용과 Quest 입력은 변경하지 않는다.
6. 변경 전 기준에서 씬 Sprite와 자식 활성 상태는 새 값이지만, 상세/간단 Joystick VoiceStep의 두
   시각 참조가 모두 null이고 Director의 `m_ControllerRayTStep`도 null이다. 기존 검증 도구 일부는
   `3_Ray_T`와 활성 `1_Ray/Panel`을 계속 요구한다.
7. 변경 후 정적 직렬화·컴파일, Editor 회귀 하네스, Play Mode의 세 단계 시각 전환과 Play 전후 자식
   Sprite/활성 상태 보존을 구분해 검증한다. Quest/OpenXR 양안 표시는 별도 수동 검증이다.

#### 적용 및 검증 결과

- `3_PPE_Room_3mode_loco.unity`의 상세/간단 컨트롤러 음성 단계 모두를
  `Trigger -> 1_Ray`, `Grip -> 2_Marker`, `Joystick -> 3_Exit_Marker`로 연결했다. 동반 컨트롤러
  이미지는 각각 `1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick`을 사용한다.
- 이전 호환 참조는 `m_ControllerRayStep=1_Ray`, `m_ControllerMarkerStep=2_Marker`,
  `m_ControllerRayTStep=3_Exit_Marker`, `m_ControllerPanelStep=null`로 정리했다. 제거된 `3_Ray_T`,
  `Ray_T_B/R` 또는 Panel Sprite를 다시 만들거나 자동 검색하는 코드는 추가하지 않았다.
- 사용자가 작성한 `Card/Item/Place` 활성 상태와 Sprite를 보존했다. 특히 Exit 표시에는 씬이 실제로
  참조하는 `Assets/UIs/Guide/ExitMarker.png`를 유지했다.
- Editor 회귀 하네스에 새 계층, Sprite, 작성 `activeSelf`, Director 참조 및 EDU/SIMP VoiceStep 연결
  검증을 추가했고 전체 하네스가 PASS했다.
- Unity Play Mode에서 공개 `StartFlow()`로 세 상태를 각각 실행한 결과 Trigger, Grip, Joystick에서
  지정된 그룹과 컨트롤러 이미지만 활성화됐고 자식 활성 상태와 Sprite는 덮어써지지 않았다. Play 종료
  뒤 씬을 디스크에서 다시 열어 런타임 시험이 남긴 dirty 상태를 제거했다.
- PPE 진열장 도착 참조는 변경하지 않았다. 계속 `Teleport_0/PPE_1 Arrival Anchor`이며 월드 위치는
  `(-0.48, -0.84, 9.14)`이다.
- `Assembly-CSharp`와 `Assembly-CSharp-Editor` 빌드는 오류 0개였다. 기존 패키지/폐기 API 경고는
  각각 2개와 34개가 남아 있다.
- Quest/OpenXR 헤드셋에서의 실제 양안 표시와 컨트롤러 입력은 이번 검증에 포함하지 않았다.
