# PPE HandTest 성능 검토

## 목적과 범위

최초 대상 씬 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`과 현재 실행 대상 `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity`에서 HMD 화면 멈춤·시야 깨짐 가능성이 있는 요소를, 현재 구현과 개선 구현(또는 검증 전 제안)으로 비교한다.

이 문서는 성능이 정상이라는 판정이 아니다. `적용`은 코드/씬에 반영된 상태, `가설`은 Quest Profiler로 검증 전인 상태, `미적용`은 검은 거울 같은 회귀를 피하기 위해 아직 적용하지 않은 제안이다.

## 비교 표

| 항목 | 기존 방식 | 개선 방식 | 주된 비용 | 상태·판정 |
| --- | --- | --- | --- | --- |
| 방호복 패널 | `Hazmat Action Panel`이 방호복의 authored pose 자식으로 표시된다. 매 프레임 위치 탐색은 하지 않는다. | 동일 방식 유지. | 거의 없음. 선택·표시 시 Transform 변경만 발생. | 적용 완료. |
| PPE 패널 (예: 헬멧) | 패널이 열린 동안 `LateUpdate`마다 모든 자식 Renderer를 검색하고 Bounds를 합산한 뒤, 카메라 바라보기 회전과 위치·스케일을 재계산했다. | 선택 시 공용 패널을 해당 PPE의 `Action Panel Pose` 자식으로 한 번만 붙인다. local 원점·정면을 사용하고 선택 해제 시 원래 parent로 복귀한다. | 기존: CPU·GC 가능성, Transform 갱신. 개선: 선택/해제 시 Transform 변경만 발생. | 적용 완료. Quest에서 이동·회전 추종 확인 필요. |
| Action Panel 방향 | 매 프레임 `Camera.main`과 필요 시 활성 Camera 전체를 찾아 viewer-facing 회전을 계산했다. | PPE pose의 authored 회전을 그대로 사용한다. | 기존: CPU, 위치·방향 불안정. 개선: 없음. | 적용 완료. |
| PPE 장착/hand crossfade | 짧은 crossfade 동안 활성 hand Renderer에 `MaterialPropertyBlock`을 반복 적용하고, 장비 자식을 약 1.8초 애니메이션한다. | 현 동작 유지. 먼저 Profiler로 장착 순간 비용을 분리 측정한다. 필요 시 Renderer 목록 캐시와 변경값만 적용을 별도 승인 후 검토한다. | CPU·렌더 상태 변경. | 가설. 이번 변경에 최적화는 포함하지 않음. |
| 거울 | Planar Mirror가 HMD Game 카메라의 반사 장면을 RenderTexture에 추가 렌더한다. 현재 설정은 `skipSceneViewWhilePlaying=true`, `renderFrameInterval=2`, 거리 제한 없음이다. | Play Mode의 Scene View 반사를 생략하고 Game/HMD 반사는 2프레임마다 갱신한다. 나머지 후보는 한 항목씩 비교한다. | GPU가 최우선, 추가 카메라 culling·draw·RenderTexture. | 두 설정 적용. Game View·Quest 양안·Profiler 비교 필요. |
| 관찰 게이지 | 직사각형 Horizontal Fill UI였다. | 단일 ring sprite의 `Radial360` Fill로 0→100%를 5초에 표시한다. | UI Image 하나의 Fill amount 갱신. | 적용 완료. 부하는 매우 낮을 것으로 예상되나 Quest에서 확인. |
| Place 페이드 | 상태 전환에 따라 즉시 표시·숨김됐다. | `CanvasGroup.alpha`를 10초 유지 후 0.75초 동안 보간하고, 종료 시 root를 숨긴다. | UI Canvas alpha 갱신. | 적용 완료. 일반적으로 미미한 비용. |
| 컨트롤러 미니 가이드 | 시작/클릭 경로의 표시가 불명확했고, 입력 binding은 generic XR controller에 의존했다. | 시작 숨김, 조이스틱 클릭 이벤트에서만 토글하며 Oculus Touch binding을 보조로 둔다. | 입력 이벤트 시 `SetActive` 1회. | 적용 완료. Quest 클릭 확인 필요. |
| 텔레포트 도착 음성 | 같은 arrival interactable 재통과 때 relay와 조건 Voice가 다시 실행될 수 있었다. | interactable별 1회 relay 및 marker Voice별 1회 보호를 둔다. | 오디오 재생·Coroutine 중복 방지. | 적용 완료. 중복은 성능보다 로직 정확성 문제다. |
| PPE 조건 음성 | 이전 선택의 지연 오답 음성이 다음 PPE 안내에 섞일 가능성이 있었다. | 새 PPE 조건 Voice 시작 전에 대기 오답 Voice Coroutine을 취소한다. | 불필요 AudioSource 재생·Coroutine 방지. | 기존 구현 유지/QA 확인 필요. |
| 테이프 시각물 | 장갑·장화 전체 장착 조건으로 한 번에 표시 경로가 제한됐다. | 방호복 + 해당 쪽 장비 상태에 따라 4개 테이프 모델을 event-driven으로 활성화한다. | 장비 상태 전환 시 `SetActive`; 매 프레임 탐색 없음. | 적용 완료. 조합 QA 필요. |
| 문 개구부 유리 | 투명 유리 renderer가 문 중심부를 가릴 가능성이 있었다. | `Door Window Glass` renderer를 비활성화했다. | 투명 draw call 하나 감소 가능. | 적용 완료이나 Game/Scene/HMD에서 개구부가 계속 막힘. 원인 미확정. |

## Quest Profiler 측정 순서

1. Quest Link를 연결하고 Play Mode를 새로 시작한다.
2. Profiler에서 CPU Usage, Rendering, GPU Usage를 기록한다. 가능하면 72Hz 기준 frame budget 약 13.9ms와 비교한다.
3. 아래 조건을 각각 10초 이상 기록한다.

   - 거울이 화면에 없는 기본 상태
   - 거울이 화면에 충분히 보이는 상태
   - 헬멧 등 PPE를 들고 패널을 열어 회전·이동하는 상태
   - PPE 장착 animation/crossfade가 진행되는 상태
   - 게이지가 0→100% 진행되는 상태

4. HMD 멈춤이나 시야 깨짐이 발생하면 직전 5초와 직후 5초의 CPU/GPU frame time, GC Alloc, Render Camera 수, 거울 가시 여부를 함께 저장한다.
5. 거울은 Off 기준 → On 비교 → 갱신 주기 또는 해상도 한 항목 변경 순서만 허용한다. 두 항목을 동시에 바꾸지 않는다.

## 통과 기준

- PPE 패널을 열고 물체를 움직여도 CPU spike와 반복 GC Alloc이 증가하지 않는다.
- panel 위치·회전이 PPE pose와 일치하며, HMD 회전에 따라 패널이 따로 회전하지 않는다.
- 거울 On이 frame budget을 반복적으로 초과시키거나 HMD 시야를 깨면 미통과로 기록한다. 원인은 Profiler 근거 없이 단정하지 않는다.
- 음성, 테이프, controller guide는 이벤트마다 한 번만 상태를 바꾸며, idle 상태에서 매 프레임 작업을 만들지 않는다.

## 이번 회차 검증 상태

- 정적 C# 빌드: 오류 0개.
- `PPEActionPanelController`에서 매 프레임 Bounds·Camera 탐색 경로가 제거됨을 코드로 확인했다.
- Quest Profiler, 양안 렌더링, 실제 HMD 성능은 아직 검증하지 않았다.

## 현재 로코모션 씬의 HMD 주변 시야 글리치 조사

### 조사 범위

- 현재 실행 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity`
- 관찰 증상: PPE 착용 중 HMD 주변 시야가 깨지거나 컴포지터가 불안정해 보이며, 손 모델이 비정상적으로 움직이는 현상
- 이번 조사는 읽기 전용으로 진행했다. 코드, 씬, Inspector, 거울 설정은 변경하지 않았다.

### 확인된 사실

- 증상 시점 전후에도 Meta `LinkClient`의 `RenderMetrics`와 `NetworkMetrics`가 계속 기록됐다.
- 활성 Quest 2는 `connectionState: connected`, `isHdmiConnected: true`, 유선 Link, USB 3 상태였다.
- 같은 구간에 USB 해제, Link `BYE`, OpenXR 세션 `STOPPING`/`EXITING`, DXGI 또는 그래픽 장치 유실 로그는 확인되지 않았다.
- Unity OpenXR 세션은 앞선 초기화 실패 뒤 복구되어 Play Mode 시작 전에 `FOCUSED` 상태에 도달했다.
- PPE 장착 로그는 정상 경로로 기록됐고 장착 직후 예외는 없었다. `PPEHazmatEquipController`의 장착 경로는 카메라, RenderTexture, OpenXR 또는 Quest Link 연결 상태를 직접 변경하지 않는다.

따라서 이번 증상을 **USB 해제 또는 Quest Link 단절로 확인할 근거는 없다.** 거울이 USB를 해제하거나 Link 자체를 끊었다고 판단하지도 않는다. 현재 가장 강한 후보는 Link 단절이 아니라, Play Mode에서 추가 반사 렌더링과 PPE 장착 순간의 렌더 상태 변화가 겹치며 HMD 컴포지터의 프레임 여유가 부족해지는 경우다. 다만 Profiler의 CPU/GPU 프레임 캡처가 없으므로 근본 원인으로 확정하지 않는다.

### 현재 거울 설정과 부하 후보

현재 `PlanarMirrorRenderer`와 `PPE_Room_Mirror_RT.renderTexture`에서 확인한 값은 다음과 같다.

| 항목 | 현재 값 | 영향 |
| --- | --- | --- |
| RenderTexture | `640 x 1472` | 메인 HMD 렌더와 별도로 반사 장면을 렌더하는 픽셀 비용이 발생한다. |
| `renderFrameInterval` | `2` | Game/HMD 반사를 2프레임마다 갱신한다. |
| `maxSourceDistance` | `0` | 거리 제한 없이 반사 갱신 후보가 된다. |
| `skipSceneViewWhilePlaying` | `true` | Play Mode 중 Scene View 카메라의 반사 렌더를 생략한다. |
| `renderInGameView` | `true` | Game View와 HMD 시연에 필요한 반사를 유지한다. |
| `renderInSceneView` | `true` | Scene View에서도 거울을 표시한다. |
| `reflectedDepth` | `20` | 반사 카메라가 비교적 먼 범위까지 렌더한다. |
| 반사 카메라 그림자 | 활성 | 반사 패스의 그림자 렌더 비용이 추가된다. |
| 반사 카메라 XR 렌더 | 비활성 | 반사 카메라 자체는 XR 카메라로 렌더하지 않는다. |

`PlanarMirrorRenderer`는 `RenderPipelineManager.beginCameraRendering`에서 반사 카메라를 추가로 렌더한다. 거울의 Culling Mask에는 착용 방호복이 사용하는 레이어도 포함되어 있어, PPE 착용 후에는 메인 HMD 카메라뿐 아니라 반사 카메라도 활성 장착물을 함께 그린다. 이것은 부하 증가 가능성을 설명하지만, 글리치의 단독 원인이라는 증거는 아니다.

### 손 모델 조사 결과

- Play Mode 계층에서 활성 `HandGripAnimator`는 왼손과 오른손의 `PPE_A_Hand_GloveTape` 두 개뿐이었다. 다른 손 변형 모델은 비활성 상태여서 중복 활성 모델이 서로 움직이는 문제는 확인되지 않았다.
- 활성 손 Animator의 `Grip`과 `Poke` 값은 모두 `0`이었고 Root Motion은 비활성이었다. 손 모델 코드나 Animator가 루트 Transform을 독립적으로 이동시킨다는 근거는 확인되지 않았다.
- 같은 시점의 `XRNode.LeftHand`와 `XRNode.RightHand` 장치는 모두 `valid=false`, `tracked=false`로 조회됐다.

따라서 손 모델의 불규칙한 움직임은 중복 모델이나 장착 Animator보다 입력/추적 유실 또는 `TrackedPoseDriver`에 남은 pose 값의 영향이 더 강한 후보다. 다만 이 프로젝트의 pose는 Input Action 경로를 사용하므로 `XRNode` 조회 결과만으로 정확한 입력 소비자를 확정하지 않는다. 손 추적 문제는 거울 부하와 분리해 검증한다.

## 부하 완화 적용 및 후속 후보

1번과 2번은 현재 대상 씬에 적용했다. 3번 이후는 미적용 후보이며, 거울 Off 기준 실행을 먼저 확보한 뒤 한 번에 하나만 변경한다.

| 우선순위 | 변경/제안 | 상태 | Game View/HMD 시연 영향 | 검증 조건 |
| --- | --- | --- | --- | --- |
| 1 | `skipSceneViewWhilePlaying = true` | 적용 | Game View와 HMD 거울은 유지되고, Play Mode의 Editor Scene View 반사만 생략된다. | `renderInGameView = true`를 유지한 채 Game View와 HMD에서 거울 표시를 확인한다. |
| 2 | 테스트 중 `renderFrameInterval = 2` | 적용 | 거울 움직임이 덜 부드러울 수 있다. 최종 시연 때 `1`로 복구할 수 있다. | 검은 화면이나 갱신 정지가 없는지 확인한다. |
| 3 | 반사 카메라의 그림자 렌더 비활성 | 미적용 | 거울은 유지되지만 반사 속 그림자 표현이 단순해질 수 있다. | 동일 위치에서 거울 품질과 GPU frame time을 비교한다. |
| 4 | `reflectedDepth`를 `8~10`으로 축소 | 미적용 | 거울 속 먼 물체가 일찍 잘릴 수 있다. | 시연 동선에서 필요한 배경이 유지되는지 확인한다. |
| 5 | `maxSourceDistance`를 `4~5m`로 제한 | 미적용 | 거울에서 멀어지면 갱신이 중단될 수 있다. | 과거 거리/갱신 최적화 뒤 검은 거울 회귀가 있었으므로 별도 단일 실험만 허용한다. |
| 6 | RenderTexture를 `480 x 1024`로 축소 | 미적용 | Game View 시연의 거울 선명도가 낮아진다. | 다른 완화안으로 부족할 때만 비교하며, 시연 품질이 중요하면 현재 해상도를 유지한다. |

Game View에 비친 거울을 시연해야 하므로 `renderInGameView = true`는 유지한다. 현재는 `skipSceneViewWhilePlaying = true`와 `renderFrameInterval = 2`를 함께 시험한다. 최종 시연에서 반사 움직임이 충분히 부드럽지 않으면 동일 조건 비교 후 `renderFrameInterval = 1` 복구 여부를 판단한다.

## 후속 검증 절차

1. Quest Link가 안정적으로 연결된 새 Play Mode에서 거울 Off 기준을 10초 이상 기록한다.
2. 동일한 시점과 동선에서 거울 On 상태를 기록한다.
3. 위 완화안 중 한 항목만 변경하고 같은 동선을 반복한다.
4. 각 실행에서 CPU/GPU frame time, GC Alloc, Render Camera 수, 거울 가시 여부와 PPE 장착 시점을 함께 저장한다.
5. 글리치가 발생하면 정확한 시각을 기록하고 Meta Link 로그의 연결 상태와 프레임 지표를 대조한다.
6. 손 모델은 좌우 Input Action의 `isTracked`, position, rotation 값과 `TrackedPoseDriver` 갱신 상태를 별도 캡처한다.

### 검증 상태

- **정적 확인:** 거울의 직렬화 설정, RenderTexture 크기, 반사 카메라 호출 경로, PPE 장착 코드의 영향 범위를 확인했다.
- **Unity Editor 실행 확인:** Play Mode에서 활성 손 모델 수, Animator 상태, `XRNode` 장치 상태를 읽기 전용으로 확인했다.
- **Quest/OpenXR 로그 확인:** 증상 구간에 USB/Link 단절을 나타내는 로그가 없고 Meta 전송 지표가 계속 기록된 것을 확인했다.
- **아직 필요한 수동 검증:** Quest Profiler 기반 거울 Off/On 비교, 위 완화안별 단일 변수 비교, Quest 양안에서의 주변 시야와 Game View 시연 품질 확인.

## 2026-08-23 후속: 씬 저장 실패, OpenXR 중단 및 거울 앞 주변 시야 깨짐

### 작업 대상과 보존 범위

- 실행·수정 대상은 `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity` 하나다.
- 같은 이름의 다른 씬 variant와 `_Recovery` 씬은 수정·삭제하지 않았다.
- 사용자가 이미 수정한 EXIT 머티리얼, 씬의 기존 대규모 변경, 복구 씬 및 기타 설정 파일은 이번 거울 조사에서 되돌리지 않았다.
- 현재 Unity 활성 씬은 마지막 Editor 검증 뒤 `dirty=True`로 표시됐다. 원인을 확정하지 못한 메모리 변경을 기존 사용자 변경과 함께 저장하거나 폐기하지 않았다.

### 씬 저장 실패와 복구

Unity가 임시 씬 파일을 대상 씬으로 이동하는 과정에서 다음 오류가 발생했다.

`Moving .../Temp/UnityTempFile-* to .../Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity: 액세스가 거부되었습니다.`

- 사용자에게 표시된 `Moving file failed` 창에서는 `Cancel`을 선택했다.
- 당시 임시 씬과 디스크 씬의 차이는 `skipSceneViewWhilePlaying: 0 → 1` 한 항목이었다.
- 임시 파일 내용을 기준으로 해당 직렬화 값만 반영한 뒤 파일 내용이 일치함을 확인했다.
- 이후 `renderFrameInterval: 1 → 2`를 별도 변경하고 대상 씬에 저장했다.
- 저장 실패 원인은 Unity/OneDrive 파일 잠금, 권한 또는 관리자·표준 사용자 실행 혼용 후보를 남겼다. 오류 메시지만으로 단일 원인을 확정하지 않는다.
- 관리자 권한으로 시작된 Unity는 경고 창에서 `Restart Unity as a standard user`를 선택해 표준 사용자로 재시작했다.

### 거울 부하 완화 적용값

| 항목 | 변경 전 | 현재 값 | 의미 |
| --- | --- | --- | --- |
| `skipSceneViewWhilePlaying` | `false` | `true` | Play Mode에서 Scene View용 반사 갱신만 생략한다. |
| `renderFrameInterval` | `1` | `2` | Game/HMD 반사를 2프레임마다 한 번 갱신한다. |
| `renderInGameView` | `true` | `true` | Game View와 HMD의 거울 표시는 유지한다. |
| `maxSourceDistance` | `0` | `0` | 거리 기반 갱신 제한은 적용하지 않았다. |

`skipSceneViewWhilePlaying=true`는 Scene View에서 거울 표면을 숨기는 설정이 아니다. Scene View용 새 반사 렌더만 건너뛰므로 거울 표면에는 마지막 RenderTexture가 계속 보일 수 있다.

### 첫 번째 Play 실패와 실행 환경 복구

- Play Mode 진입 뒤 Unity Game View가 검게 보이고 Quest Link 영상이 나오지 않았다.
- Windows 장치 상태에서는 Quest 2, USB Composite, ADB, XRSP, Commlib 및 Highwind 장치가 계속 `OK`였다. 물리 USB 해제로 확인되지는 않았다.
- 같은 실행의 `Editor.log`에서 OpenXR은 `READY`에 도달했으나 Meta Runtime IPC pipe가 끊기고 재연결을 반복했다. 실패 실행에서는 정상 표시 상태인 `VISIBLE/FOCUSED`까지 진행하지 못했다.
- 응답하지 않는 Unity 주 프로세스를 정확한 PID로 확인한 뒤 종료하고 `OVRService`를 재시작했다.
- Meta Quest Link를 다시 연결한 뒤 후속 Play에서는 PPE 테스트가 가능한 상태까지 진입했다.

### 후속 Play에서 관찰된 증상

- 거울 앞에서 HMD 주변 시야가 원형으로 파랗거나 다른 색이 섞인 것처럼 깨졌다.
- 이 현상이 발생한 뒤 Link 연결이 해제됐으며, Link를 재시작하지 않으면 다시 연결되지 않는다고 사용자가 보고했다.
- 앞선 “Link 단절 근거 없음” 기록은 다른 재현 구간의 관찰이다. 이번 후속 재현에서는 Meta/OpenXR 세션 중단과 사용자 체감 Link 해제가 함께 발생했으므로 두 구간을 같은 결과로 합치지 않는다.
- Play Mode에서 `PlanarMirrorRenderer.enabled=false`로 만든 런타임 전용 비교를 시작했으나, 사용자가 다른 PPE 회귀를 우선 확인해 OFF 상태의 주변 시야 결과를 확정하지 못했다. 이 값은 Play 종료 후 자동 복구됐고 씬에 저장하지 않았다.

### 현재 거울 구현 진단

현재 확인된 실행값은 다음과 같다.

| 항목 | 값 |
| --- | --- |
| 반사 카메라 | `Mirror_Reflection_Camera`, 평상시 `enabled=false` |
| `stereoTargetEye` | `None` |
| RenderTexture | `640 x 1472`, `Tex2D`, AA 1, `vrUsage=None` |
| HDR | 활성 |
| 반사 그림자 | 활성 |
| 반사 갱신 | Game/HMD 2프레임 간격, Play 중 Scene View 생략 |

`PlanarMirrorRenderer`는 `[ExecuteAlways]` 컴포넌트이며 `RenderPipelineManager.beginCameraRendering`에서 가시 카메라를 감지한 뒤 `UniversalRenderPipeline.RenderSingleCamera`로 반사 카메라를 추가 렌더한다. 거울이 HMD 카메라에 보이는 동안 메인 XR 렌더 안에서 별도 URP 카메라 렌더가 발생하므로 GPU 부하와 렌더 호출 안정성의 가장 강한 최근 변경 후보이다. 다만 거울 OFF 기준에서 동일 증상이 사라지는 비교 결과가 없으므로 근본 원인으로 확정하지 않는다.

### 현재 판단

- **최근 변경 후보:** 거울 추가 카메라 렌더 및 반사 그림자 비용. 가장 유력하지만 미확정이다.
- **씬·직렬화 후보:** 적용한 두 값은 디스크 씬에서 `renderFrameInterval=2`, `skipSceneViewWhilePlaying=true`로 확인됐다. 반사 카메라와 RenderTexture 참조도 유효하다.
- **실행 환경 후보:** 첫 실패에서는 Meta Runtime IPC가 정상 표시 상태 전에 중단됐다. 후속 실패에서는 거울 앞 글리치 뒤 Link 재시작이 필요했다. 물리 USB 해제와 Meta/OpenXR 세션 중단은 구분한다.

### 완료한 검증

- 거울 설정과 반사 카메라·RenderTexture 직렬화 값을 확인했다.
- `PlanarMirrorRenderer`의 카메라 이벤트부터 추가 반사 렌더까지 호출 경로를 확인했다.
- 첫 실패 실행의 Windows 장치 상태와 Unity/OpenXR 로그를 대조했다.
- 표준 사용자 Unity와 재시작한 `OVRService`에서 Link 재연결을 확인했다.
- 거울 설정 변경 외에 반사 그림자, 반사 거리, 깊이, RenderTexture 해상도는 변경하지 않았다.

### 아직 필요한 수동 검증

1. 변경이나 저장 없이 거울 컴포넌트만 런타임 OFF로 두고 같은 동선에서 10초 이상 HMD 주변 시야와 Link 상태를 확인한다.
2. 동일 세션·동선에서 거울 ON으로 바꾸고 증상 재현 여부를 비교한다.
3. OFF에서도 깨지면 거울 단독 원인을 제외하고 OpenXR/Meta Runtime 및 최근 PPE 렌더 변경으로 범위를 넓힌다.
4. OFF는 정상이고 ON에서만 깨지면 거울 렌더를 원인으로 확정한 뒤 반사 그림자, `reflectedDepth`, 거리 제한 또는 해상도 중 한 항목만 변경한다.
5. Quest Profiler에서 CPU/GPU frame time, Render Camera 수와 글리치 직전 시각을 기록한다.
6. Quest 양안과 Game View를 별도로 확인한다. Game View 정상만으로 양안 정상이라고 판정하지 않는다.

## 2026-08-24 후속: Meta Quest 72Hz 고정 요청

### 변경 전 판단 기록

1. 기존 Inspector/프로젝트 작성값을 보존한다. Quality의 `vSyncCount=0`, 렌더링·거울·URP·OpenXR 기존 Feature 설정은 바꾸지 않고 Android용 72Hz Feature만 추가한다.
2. 주사율 요청의 단일 소유자는 프로젝트 소유 `MetaQuestDisplayRefreshRateFeature`이며 목표값은 Android OpenXR Settings에 직렬화된 `72`다.
3. 실행 경로는 `Android OpenXR Loader → XR_FB_display_refresh_rate 활성화 → OpenXR Session Begin → 지원 주사율 열거 → 72Hz 지원 확인 → xrRequestDisplayRefreshRateFB(72)`이다. `Application.targetFrameRate=72`는 렌더 루프 목표도 맞추지만 헤드셋 주사율 요청을 대신하지 않는다.
4. 필수 확장·함수·72Hz 지원이 없으면 런타임 자동 fallback을 하지 않고 한 번의 명확한 오류를 기록한다.
5. 영향 소비자는 Android/Quest OpenXR 세션과 프레임 예산이다. Standalone OpenXR, UI, 입력, PPE, 텔레포트, 오디오, 거울 설정은 변경하지 않는다.
6. 변경 전 기준은 주사율 요청 코드가 없고 `vSyncCount=0`인 상태다. 변경 후 정적 Feature 배선, Unity 컴파일, Editor 하네스를 비교한다.
7. 정적 확인과 Unity Editor 검증까지 수행한다. 실제 72Hz 적용 여부와 13.89ms 프레임 예산 충족은 Quest 빌드/헤드셋에서 확인해야 한다.

### 이번 변경이 대응하는 사용자 요청과 보존 동작

- 대응 요청: Collider 작업 전에 Quest 주사율을 72Hz로 설정하고 커밋·푸시해 복구 지점을 만든다.
- 보존 동작: 런타임이 지원하지 않는 주사율을 임의 선택하지 않으며, 기존 OpenXR Feature와 PC OpenXR 동작을 변경하지 않는다.

### 적용한 변경

- Android 전용 `MetaQuestDisplayRefreshRateFeature`를 추가하고 OpenXR Settings에서 활성화했다.
- OpenXR 세션 시작 시 지원 주사율을 먼저 열거하고, 72Hz 지원이 확인될 때만 `xrRequestDisplayRefreshRateFB(72)`를 호출한다.
- 앱 렌더 루프 목표도 `Application.targetFrameRate=72`로 맞췄다.
- 지원 확장·함수·72Hz가 없거나 요청이 실패하면 임의 주사율로 대체하지 않고 원인을 포함한 오류를 한 번 기록한다.
- `MetaQuestRefreshRateValidationHarness`를 추가해 Android Feature의 유일성·활성 상태·직렬화된 72Hz 값과 모든 Quality 단계의 `vSyncCount=0`을 검사한다.

### 근본 원인과 영향 범위

- 기존 프로젝트에는 Quest 디스플레이 주사율을 요청하는 코드가 없었다. `Application.targetFrameRate`만으로는 헤드셋 주사율을 바꿀 수 없으므로 `XR_FB_display_refresh_rate` 경로가 필요했다.
- 변경은 Android OpenXR Feature에만 적용된다. 일반 모니터용 VSync를 켜는 변경이 아니며, XR 프레임 제출 동기화는 OpenXR 런타임이 담당한다.

### 완료한 검증

- Unity 스크립트 컴파일 성공, Console Error 0건.
- `Tools > XR > Validate Meta Quest 72 Hz`와 동일한 하네스를 실행해 PASS를 확인했다.
- Android OpenXR Settings에서 Feature `enabled=True`, target `72`, extension `XR_FB_display_refresh_rate`를 확인했다.
- `ProjectSettings/QualitySettings.asset`의 모든 Quality 단계가 `vSyncCount=0`임을 확인했다.

### 아직 필요한 수동 검증

- Quest Android 빌드에서 `[Meta Quest Refresh Rate] Requested 72 Hz` 로그와 런타임 보고 주사율을 확인한다.
- 헤드셋 성능 도구에서 실제 디스플레이 72Hz와 앱 프레임 속도, CPU/GPU frame time이 13.89ms 예산 안에 드는지 확인한다.
- 이 검증 전에는 Quest에서 실제 72Hz가 적용됐다고 확정하지 않는다.
