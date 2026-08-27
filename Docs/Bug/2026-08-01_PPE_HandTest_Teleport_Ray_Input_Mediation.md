# PPE HandTest 텔레포트 레이 입력 중재 회귀 기록

작성일: 2026-08-01

## 최초 증상

`Assets/Scenes/3_PPE_Room_HandTest.unity`에서 시나리오 카드를 가리키거나 선택한 뒤 컨트롤러 손에 짧고 잘린 것처럼 보이는 Near-Far 레이가 남았다. 조이스틱을 앞으로 밀어도 곡선형 텔레포트 레이가 간헐적으로만 나타나서 PPE 텔레포트 마커 두 곳을 거의 선택할 수 없었다.

## 최초 원인

XRI Starter Assets의 `ControllerInputActionManager`는 연결된 `NearFarInteractor`가 Far 선택 영역을 보고하면 Teleport Mode 액션을 비활성화한다. 이 씬의 Near-Far 인터랙터는 최대 40m까지 캐스팅하기 때문에 카드와 월드 스페이스 UI가 카드 선택 이후에도 Far 영역을 활성 상태로 유지했고, 텔레포트 모드의 `performed` 콜백을 막았다.

공식 Teleport Interactor 프리팹의 레이캐스트에는 Default와 UI 물리 레이어도 포함되어 있었다. 그 결과 카드 및 방 콜라이더가 PPE 텔레포트 마커보다 먼저 곡선 레이를 끊을 수 있었다.

## 텔레포트 입력 문제 해결

- 각 컨트롤러의 샘플 입력 관리자를 프로젝트 소유 `PPEControllerTeleportModeManager`로 교체했다.
- 카드나 UI를 가리키고 있어도 Teleport Mode 입력 액션은 유지한다.
- 조이스틱을 앞으로 밀면 Near-Far 인터랙터를 끄고 곡선형 Teleport Interactor만 활성화한다.
- 조이스틱을 놓으면 텔레포트-on-release가 끝날 수 있도록 `LateUpdate`까지 Teleport Interactor 종료를 지연한 뒤 Near-Far 인터랙터를 복원한다.
- Grip 입력으로 텔레포트를 취소할 수 있도록 유지했다.
- 활성 PPE 마커 두 개를 `Teleport Target` 물리 레이어에 두고 텔레포트 곡선 레이캐스트가 해당 레이어만 검사하도록 제한했다.
- 기존 XRI `TeleportationAnchor`, Teleport 인터랙션 레이어, 도착 위치와 방향, 텔레포트-on-release 동작은 유지했다.

## 왼손 레이 위치 보정

처음에는 왼손 레이가 손 모델의 오른쪽 공중에 떠 있었다. 왼쪽 장갑 루트에는 씬에서 작성된 `x = -0.141m` 오프셋이 있었는데, 양손 레이를 모두 보정 전 컨트롤러 원점에 강제로 배치한 것이 원인이었다.

각 Near-Far 및 Teleport Interactor의 Transform을 씬에서 작성된 실제 장갑 루트에 맞췄다. 따라서 왼손의 일반 레이와 텔레포트 레이는 보이는 왼손에서 시작하며, 오른손의 기존 정렬에는 영향을 주지 않는다.

## 텔레포트 레이 색상과 유효 상태

이 씬은 Unity XRI의 공식 Directional Teleport 흐름을 사용한다.

- `NearFarInteractor`: 카드, UI 및 일반 포인팅 담당
- 공식 Teleport Interactor: 곡선형 포물선 레이 담당
- `TeleportationAnchor`: 실제 텔레포트 가능 지점 담당
- 조이스틱 해제 시 텔레포트 실행

조이스틱 입력은 동시에 Select 입력이기도 하다. 따라서 조이스틱을 누르고 있는 동안 유효한 마커가 이미 선택 상태가 될 수 있는데, `XRInteractorLineVisual.treatSelectionAsValidState`가 꺼져 있으면 실제로 텔레포트 가능한 상태에서도 레이가 빨간색 무효 그라디언트로 보일 수 있다.

양손 텔레포트 레이에 `treatSelectionAsValidState`를 활성화했다. 유효한 마커를 선택 중일 때는 유효 그라디언트를 사용하고, 실제 무효 지점에는 기존 무효 그라디언트를 사용한다.

레이 색상은 각 `XRInteractorLineVisual`의 유효/무효 그라디언트와 `XRNearFarReticleVisual`의 씬 직렬화 값에서 변경할 수 있다. 런타임 코드가 해당 색상을 하드코딩하지 않는다.

## 레티클의 역할과 디자인

레이 끝의 파란 원은 텔레포트 시스템 자체가 아니라 레티클 시각 요소다. 프로젝트 소유 `XRNearFarReticleVisual`이 `Assets/Prefabs/XRControllerRayReticle.prefab`을 인스턴스화한다.

따라서 레티클의 메시, 머티리얼, 기본 크기 등 디자인은 프리팹에서 관리한다. 레티클은 별도의 텔레포트 목적지나 이동 시스템이 아니며 XRI가 계산한 레이 끝점을 시각적으로 표시할 뿐이다.

### 2026-08-06: 텔레포트 레티클 크기 1/3

- `3_PPE_Room_HandTest_scale_0` 양손 Teleport Interactor가 샘플 `Directional`/`Blocking Teleport Reticle` 대신 프로젝트 소유 `Assets/Prefabs/PPEDirectionalTeleportReticle.prefab`, `PPEBlockingTeleportReticle.prefab`을 사용한다.
- 루트 localScale을 `0.33333334`로 두어 기존 대비 약 1/3 크기다. 샘플 프리팹은 수정하지 않았다.

### 2026-08-16: 패널 Hover 시 레티클 과소 표시 관찰

- 사용자 통합 테스트에서 패널에 레이가 닿으면 레티클이 최초 상태보다 매우 작아져 식별하기
  어렵다는 현상이 관찰됐다.
- 대상 패널 명칭은 음성 입력에서 `PP 패널`로 전달됐으며 PPE Action Panel인지 재확인이 필요하다.
- UI Hit 전·중·후의 `XRNearFarReticleVisual`, 인스턴스화된 레티클 Transform과 프리팹 스케일을
  비교하고, UI용 거리 보정 또는 Hover 상태가 작성 스케일을 덮어쓰는지 확인한다.
- 아직 재현 로그·수정·Quest 양안 검증 전이다.

### 2026-08-16: 중간 이탈용 EXIT 존 후속 제안

- 사용자 가까이에 `EXIT` 전용 텔레포트 존을 추가해 PPE 시나리오 진행 중 중도 이탈할 수 있도록
  하는 동선 제안이 기록됐다.
- 일반 텔레포트 목적지와 별도 의미를 가지므로 기존 Anchor를 단순 복제하지 않고, 선택 확인과
  종료 상태 전이의 단일 소유자를 먼저 정해야 한다.
- EXIT 승인 시 진행 중 Voice·SFX를 즉시 정리하고, 모달·퀴즈·PPE 상호작용·텔레포트 입력을
  일관된 순서로 닫은 뒤 확정된 복귀 위치로 이동해야 한다.
- 아직 위치, 시각 디자인, 확인 방식, 복귀 목적지와 세션 초기화 범위는 결정되지 않았으며 씬과
  런타임에는 적용하지 않았다.

## 시나리오 선택 이후 텔레포트 활성화

맥락상 시나리오 카드를 선택한 다음에만 텔레포트 레이가 나타나도록 변경했다. 유효한 시나리오를 선택하기 전에는 `PPEControllerTeleportModeManager`가 양손의 텔레포트 모드 입력을 거부한다.

다음 두 선택 경로가 동일한 선택 상태를 알린다.

- `ScenarioCardSelectProxy`: 카드를 선택하고 HUD를 바로 숨기는 경로
- `ScenarioDetailModal`: 상세 모달을 사용하는 구성 경로

두 경로 중 하나에서 정상적으로 카드를 선택하면 다음 조이스틱-forward 입력부터 공식 곡선 텔레포트 레이가 활성화된다. 이 게이트는 텔레포트 모드의 사용 가능 여부만 바꾸며 카드 레이아웃, 라벨, 레이 색상, 마커 도착 위치 및 XRI Locomotion Provider에는 영향을 주지 않는다.

## Invalid AABB 오류 폭증

### 관찰된 오류

Play Mode 실행 중 Unity Console에 다음 렌더링 오류가 1만 건 이상 계속 쌓였다.

- `Invalid worldAABB. Object is too large or too far away from the origin.`
- `Invalid localAABB. Object transform is corrupt.`
- `Invalid AABB a`
- `IsFinite(distanceForSort)` 및 `IsFinite(distanceAlongView)` assertion

C# 컴파일 오류는 없었다. 저장된 HandTest 씬 YAML에도 `NaN`, `Infinity` 또는 비정상적으로 큰 Transform 값은 없었다. 첫 AABB 오류는 미러 패스보다 먼저 Game View 카메라에서 발생했다.

이후 `PlanarMirrorRenderer.RenderForCamera`가 이미 깨진 오브젝트를 미러 카메라와 Scene View에서 다시 렌더링하면서 오류 수를 크게 늘렸다. 미러 렌더러는 최초 원인이 아니라 오류 증폭 요소였다.

### 근본 원인

양손 컨트롤러 루트에는 각각 `XRNearFarReticleVisual`과 XRI `CurveVisualController`가 있다. 기존 `UpdateReticle`은 XRI가 반환하는 곡선 끝점과 표면 법선이 항상 유효하다고 가정했다.

OpenXR 추적 초기화, 일시적인 추적 손실 또는 도메인 리로드 순간에는 끝점 데이터에 `NaN`이나 `Infinity`가 잠시 포함될 수 있다. 기존 코드는 이 값을 검사하지 않고 `Transform.SetPositionAndRotation`과 거리 기반 스케일에 전달했다. 그 결과 양손에서 생성된 레티클 Renderer의 Transform과 AABB가 깨졌다.

1차로 레티클 Transform만 보호한 뒤 Unity를 재컴파일했지만 동일 오류가 계속됐다. XRI 3.4.1의 `CurveVisualController`를 추가 확인한 결과, 이 컴포넌트도 `onBeforeRender`에서 곡선 원점, 끝점, 거리 및 폭을 유한성 검사 없이 `LineRenderer.SetPositions`와 폭 계산에 전달할 수 있었다. 양손 LineRenderer의 곡선 점 또는 폭에 남은 비정상 값이 레티클 안전장 이후에도 각각 하나의 잘못된 AABB를 계속 만들었다.

OpenXR가 활성화된 상태의 도메인 리로드는 문제를 촉발하는 조건이었지만 근본 결함은 아니었다. 근본 결함은 추적 데이터의 유효성을 확인하지 않고 렌더링 Transform에 적용한 것이었다.

### 다른 시스템과 충돌하지 않도록 제한한 수정

`Assets/Scripts/XRNearFarReticleVisual.cs`가 레티클 Transform을 변경하기 전에 다음 데이터를 모두 검사하도록 수정했다.

- 곡선 끝점과 곡선 원점
- 인터랙터 방향과 끝점 표면 법선
- 법선에 직교하는 Up 방향과 최종 회전값
- 표면 오프셋, 거리 기반 스케일 범위 및 최종 로컬 스케일
- XRI가 렌더 직전에 생성한 모든 LineRenderer 곡선 점
- 씬에서 작성된 최대 레이 거리 대비 과도하게 먼 곡선 점
- LineRenderer의 시작 폭, 끝 폭 및 폭 배율

값이 유한하지 않거나 안정적인 방향을 만들 수 없으면 해당 프레임에만 레티클을 숨긴다. 위치, 회전, 스케일 전체가 검증을 통과한 경우에만 Transform을 한 번에 갱신한다. 추적 데이터가 다시 정상으로 돌아오면 레티클도 자동으로 다시 표시된다.

곡선 LineRenderer 검사는 XRI의 `k_BeforeRenderLineVisual` 다음 순서에 실행한다. 잘못된 점이 있으면 position count를 변경하지 않고 모든 점을 유효한 원점의 0길이 선으로 치환한다. 다음 프레임에는 XRI가 정상 곡선을 그대로 다시 작성할 수 있다. 폭 값이 잘못된 경우에는 런타임 상수를 사용하지 않고 컴포넌트 초기화 시 보존한 씬/프리팹 작성 값을 복원한다.

이번 안전장 수정에서 변경하지 않은 요소는 다음과 같다.

- XRI 입력 액션, 컨트롤러 handedness 및 텔레포트 모드 중재
- 곡선 캐스팅, 텔레포트 선택, Locomotion Provider 및 도착 위치
- 씬에서 작성한 레이 머티리얼, 그라디언트, 레티클 프리팹 스케일 및 UI 표현 값
- Planar Mirror 설정

## 결정론적 검증

### 공식 텔레포트 구성

Unity에서 `Tools > PPE > Validate Official Teleportation (HandTest)`를 실행하거나 배치 모드에서 `PPEOfficialTeleportationSetup.ValidateBatch`를 호출한다.

검증기는 다음 항목을 확인한다.

- 프로젝트 소유 입력 중재 컴포넌트 및 액션 참조
- 텔레포트 포물선 레이캐스트 마스크
- 텔레포트 대상 물리 및 인터랙션 레이어
- Teleportation Provider와 도착 Transform
- 양손 레이 원점 정렬
- 시나리오 선택 게이트 연결

### 레티클 유효성 안전장

`Tools > XR > Validate Near-Far Reticle Safety`를 실행한다. `Assets/Editor/XRNearFarReticleSafetyHarness.cs`가 정상 벡터와 회전 및 안전 거리 안의 곡선 점은 허용하고, `NaN`, `Infinity` 및 안전 거리를 벗어난 곡선 점은 Renderer에 도달하기 전에 거부하는지 확인한다.

`dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`은 오류 0개로 완료됐다. 기존 패키지/source-generator 및 가져온 샘플 코드의 경고는 남아 있다.

2차 LineRenderer 안전장 적용 후 기존 Play Mode를 종료하고 Unity 도메인 리로드 및 컴파일 완료를 기다린 다음 새 Play Mode에 진입했다. 회귀 하네스 확장 전후의 새 실행 로그를 두 차례, 총 55초 이상 분리 관찰한 결과 `Invalid worldAABB`, `Invalid localAABB`, `Invalid AABB`, `IsFinite(distanceForSort)`, `IsFinite(distanceAlongView)` 및 `PlanarMirrorRenderer.RenderForCamera` 관련 새 오류는 0건이었다.

## 현재 상태와 헤드셋 검증 절차

수정 당시 이미 실행 중이던 Unity Play Mode에는 이전 코드로 생성된 런타임 오브젝트가 남아 있었다. 따라서 다음 순서로 새 실행 세션을 만들어야 한다.

1. Play Mode를 종료한다.
2. Unity의 스크립트 가져오기와 컴파일이 완전히 끝날 때까지 기다린다.
3. Console을 Clear한다.
4. 다시 Play Mode에 진입한다.
5. 시나리오 선택 전에는 양손 조이스틱을 밀어도 텔레포트 곡선 레이가 나오지 않는지 확인한다.
6. 시나리오 카드를 선택한 뒤 양손에서 곡선 레이가 매번 나타나는지 확인한다.
7. PPE 마커 두 곳에서 유효 색상과 레티클이 표시되고, 조이스틱을 놓았을 때 정확히 한 번 텔레포트되는지 확인한다.
8. Grip으로 취소했을 때 Near-Far 레이가 복원되는지 확인한다.
9. 손 또는 컨트롤러 추적을 끊었다가 복원하고 새로운 Invalid AABB 및 finite-distance 오류가 발생하지 않는지 확인한다.
10. Quest/OpenXR 헤드셋 양쪽 눈에서 레이, 레티클 및 손 기준 위치를 확인한다.

## 2026-08-01 추가 조치 — Quest 오디오 출력과 시나리오 카드 피드백

### OpenXR 오디오 출력 경고

Play Mode 진입 시 다음 경고와 함께 Quest 헤드셋에서 소리가 들리지 않는 현상을 확인했다.

`XR: Error setting active audio output driver. Falling back to default.`

Windows의 `Audiosrv`, `AudioEndpointBuilder`, `OVRService`는 모두 실행 중이었고 `Oculus Virtual Audio Device`와 `헤드폰(Oculus Virtual Audio Device)`도 장치 오류 없이 존재했다. 드라이버 누락이 아니라 OpenXR이 Oculus 출력 GUID를 Unity의 활성 출력으로 전환하는 단계에서 실패한 경우였다.

확인 당시 Windows의 일반 및 멀티미디어 기본 출력은 `LG FHD (NVIDIA High Definition Audio)`였고 Oculus 헤드폰은 통신용 기본 출력에만 지정돼 있었다. 따라서 OpenXR이 폴백하면 헤드셋이 아니라 LG 출력으로 소리가 전달됐다. 일반, 멀티미디어, 통신 역할의 기본 출력을 모두 Oculus 헤드폰 엔드포인트로 변경했다. 이후 경고가 다시 나타나더라도 폴백 대상은 Oculus 헤드폰이다. 실제 Quest 출력 음량은 헤드셋에서 최종 확인한다.

### 카드 효과가 보이지 않았던 원인

시나리오 카드의 `Button`은 알파가 0인 `RoundedRectangleGraphic`을 Target Graphic으로 사용했다. `Highlighted`, `Pressed`, `Selected` 색상 값은 존재했지만 원본 Graphic이 완전히 투명하므로 색 전환 결과도 보이지 않았다. 또한 이 Graphic은 다층 Sci-Fi 카드의 최상단 표시 레이어가 아니었다.

### 카드 선택 피드백과 BGM 페이드

`ScenarioCardSelectProxy`에 XR UI 포인터의 진입, 이탈, 누름, 해제 상태 처리를 추가했다. HandTest 씬의 `XR UI Canvas/Scenario Selection HUD/ScenarioCard3_Group` 아래 세 카드에는 `Interaction Feedback Overlay`를 마지막 자식으로 직렬화했다. 오버레이는 카드보다 카메라 쪽인 로컬 Z `-0.022`에 있고 Raycast Target은 꺼져 있어 기존 선택 입력을 막지 않는다.

씬에서 작성한 효과 값은 다음과 같다.

- 호버: 청록색 알파 `0.20`
- 누름: 검정 오버레이 알파 `0.21568626`
- 선택: 검정 오버레이 알파 `0.21568626`
- 색 전환 시간: `0.10초`
- 선택 표시 유지 시간: `0.28초`
- BGM 페이드 아웃 시간: `1.25초`

누름과 선택 색은 같은 씬의 `종료하기` 버튼 `Pressed Color` 밝기 `0.78431374`와 시각적으로 일치하도록 정했다. 흰색 카드 위에 검정 알파 `1 - 0.78431374 = 0.21568626`을 합성해, 호버의 청록 강조는 유지하면서 클릭 순간에는 카드 전체가 어두워진다.

카드 선택 시 선택 효과와 BGM 페이드를 즉시 시작하고, 선택 표시를 `0.28초` 보여 준 뒤 기존 시나리오 선택 알림과 HUD 숨김을 실행한다. 선택 중 중복 입력은 무시한다. BGM 페이드가 끝나면 `AudioSource.Stop()`으로 재생을 종료한다.

`AudioManager`에는 카드가 BGM 로딩보다 먼저 선택되는 경합도 보강했다. 명시적인 `StopBgm` 또는 `FadeOutBgm` 요청 뒤에는 현재 씬에서 아직 대기 중인 BGM 시작을 억제한다. 새 씬을 큐에 넣거나 BGM을 명시적으로 재생하면 억제 상태를 해제한다. Ambience와 SFX 경로는 변경하지 않았다.

다른 씬의 기존 `ScenarioCardSelectProxy`에는 오버레이 참조와 BGM 페이드 활성 값이 없으므로 기존 즉시 선택 동작을 유지한다.

### 추가 검증

`Tools > PPE > Validate HandTest Scenario Card Feedback`은 다음 항목을 검사한다.

- 대상 시나리오 카드 3개 존재
- 각 카드의 최상단 자식 오버레이 참조
- 오버레이의 Raycast Target 비활성
- 호버가 보이고 누름·선택이 `종료하기` 버튼과 같은 양으로 어두워지는지
- 선택 표시 유지 시간과 BGM 페이드 시간

설정 직후 하네스가 통과했고 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`은 오류 0개로 완료됐다. 새 Play Mode 진입에서 카드 피드백 코드의 예외와 기존 Invalid AABB 오류는 발생하지 않았다. 최종적으로 Quest에서 레이 호버 시 오버레이가 카드 내용보다 위에 보이는지, 선택 효과가 표시된 뒤 HUD가 닫히는지, BGM이 1.25초 동안 줄어든 뒤 정지하는지 확인한다.

설정 명령은 오버레이 참조가 없는 최초 구성에서만 위 기본값을 기록한다. 이미 구성된 카드에는 색상, 위치, 크기, 시간 값을 다시 쓰거나 씬을 다시 저장하지 않으므로 이후 Inspector에서 조정한 값이 유지된다.

## 2026-08-02 추가 조치 — HandTest 직접 실행 오디오와 Quest Link 재초기화

### HandTest 씬에서 BGM이 시작되지 않았던 프로젝트 설정 원인

`AudioManager`는 `Resources/Audio/Scenes/{현재 씬 이름}`의 `SceneAudioSettings`를 정확한 이름으로 조회한다. 기존에는 `3_PPE_Room.asset`만 있고 현재 테스트 씬 이름과 일치하는 `3_PPE_Room_HandTest.asset`이 없었다. 따라서 HandTest 씬을 Unity에서 직접 실행하면 오디오 장치가 정상이어도 재생할 BGM 설정을 찾지 못해 무음이 됐다.

`Assets/Resources/Audio/Scenes/3_PPE_Room_HandTest.asset`을 추가하고 기존 PPE 룸과 동일한 직렬화 설정을 기록했다.

- BGM: `Safe-Horizons-_VR-Training-Theme_.ogg`
- 시작 지연: `0.5초`
- 음량: `1.0`
- 페이드 인: `0.5초`
- 이전 BGM이 비어 있으면 정지: 활성

이 값은 런타임 코드에 하드코딩하지 않고 씬 이름에 대응하는 `SceneAudioSettings` 에셋에 보관한다. 따라서 HandTest 씬을 직접 Play해도 BGM이 시작되고, 카드를 선택하면 기존 `1.25초` 페이드 아웃 경로가 실제 재생 중인 소스에 적용된다.

### Quest Link 연결 실패와 오디오 경고의 관계

Unity를 완전히 재시작한 뒤 최초 Play Mode 진입에서는 다음 오류가 오디오 경고보다 먼저 발생했다.

- `xrGetSystem: XR_ERROR_FORM_FACTOR_UNAVAILABLE`
- `[Subsystems] Failed to initialize subsystem OpenXR Display [error: 1]`
- `XR: Error setting active audio output driver. Falling back to default.`

이 시점에는 Oculus OpenXR 런타임 프로세스와 서비스는 살아 있었지만 실제 Quest Link HMD 세션과 Oculus Dash가 종료된 상태였다. 따라서 OpenXR이 헤드셋 폼 팩터를 얻지 못했고, 같은 초기화 과정의 Oculus 오디오 GUID 전환도 함께 실패했다. Unity 오디오 드라이버만의 독립적인 고장이 아니었다.

Quest Link를 다시 연결한 뒤 Meta 로그에서 `AudioPlayer::initialize() success`, `Entered VrMode`, 비디오 스트림 시작을 확인했고 Oculus Dash가 다시 실행됐다. 그 상태에서 Unity Play Mode를 완전히 종료 후 재진입했다. 새 세션에서는 OpenXR Display와 Input 플러그인이 모두 로드되고 `XR_ERROR_FORM_FACTOR_UNAVAILABLE`, 활성 오디오 출력 드라이버 경고 및 Display 초기화 실패가 다시 발생하지 않았다.

Windows의 Console, Multimedia, Communications 기본 출력은 모두 Oculus 헤드폰 엔드포인트 `{0.0.0.00000000}.{0dd286e3-cc74-43aa-978b-72eddd41a294}`이다. 새 Play Mode에서 Core Audio 세션을 조회한 결과 Unity 프로세스가 이 엔드포인트에 활성 오디오 세션으로 등록됐고 Oculus Dash 세션도 활성 상태였다. 실제 착용 상태에서의 최종 청취는 헤드셋에서 확인한다.

재현 시 복구 순서는 다음과 같다.

1. Quest Link 홈 화면과 Oculus Dash가 헤드셋에서 실제로 실행 중인지 확인한다.
2. Unity Console에 `XR_ERROR_FORM_FACTOR_UNAVAILABLE`가 있으면 먼저 Link를 재연결한다.
3. Link가 복구된 뒤 기존 Play Mode를 종료한다.
4. 스크립트·에셋 가져오기가 끝난 다음 새 Play Mode에 진입한다.
5. 새 로그에 OpenXR Display/Input 로드 성공과 오디오 출력 경고 부재를 확인한다.
6. HandTest BGM이 들리고 카드 선택 시 `1.25초` 동안 줄어든 뒤 정지하는지 확인한다.

### 카드 클릭 표현 일관성 검증

HandTest의 세 카드 모두 호버는 기존 청록색 알파 `0.20`을 유지하고, 누름과 선택은 검정 알파 `0.21568626`으로 직렬화했다. `Tools > PPE > Validate HandTest Scenario Card Feedback`은 이제 최상단 오버레이, Raycast Target 비활성, 호버 가시성, `종료하기` 버튼과 같은 어두워짐 양, 선택 지연 및 BGM 페이드 값을 함께 검사한다. `Tools > PPE > Apply Quit-Style Dark Card Selection`은 명시적으로 실행할 때만 누름·선택 색을 맞추며 호버와 나머지 Inspector 작성 값은 보존한다.

## 2026-08-06 추가: CurveVisualController fallback 배열 오류 재발 관찰

### 관찰

`3_PPE_Room_HandTest_scale_0` Play 중 `CurveVisualController.ComputeFallBackLine`에서 다음 예외가 반복됐다.

```text
IndexOutOfRangeException: Index 0 is out of range of '0' Length.
... CurveVisualController.ComputeFallBackLine
... CurveVisualController.OnBeforeRenderLineVisual
```

같은 실행에서 `Invalid AABB a`, `IsFinite(distanceForSort)`, `IsFinite(distanceAlongView)`가 대량 발생했다. 스택상 `PlanarMirrorRenderer`는 비정상 LineRenderer Bounds를 반사·Scene View에 다시 렌더링하면서 오류를 증폭했으며 최초 예외는 아니다.

### 조사 결과

- 양손 `CurveVisualController`는 `m_VisualPointCount: 20`이며 LineRenderer·Curve Visual·원점 참조가 끊기지 않았다.
- XRI 패키지 구현은 fallback 점 배열을 `Awake`에서 길이 3으로 생성한다. 이번 예외는 BeforeRender 콜백이 길이 0 배열을 받은 상태로 실행된 사실을 의미한다.
- `XR UI Canvas`, `Modal Canvas`의 `TrackedDeviceGraphicRaycaster.OnDisable` `KeyNotFoundException`은 백업 씬 로드 중 별도로 기록됐다.

### 조치와 상태

- 패키지 소스, 레이 입력, 레이 머티리얼·그라디언트, 씬의 곡선 점 수는 수정하지 않았다.
- Play Mode 종료 후 `_scale_0` 씬을 다시 열자 오류 폭주는 멈췄다.
- 이는 도메인/씬 전환 시점의 XRI lifecycle 문제 관찰이며 영구 해결로 판정하지 않는다. 새 Quest/OpenXR 실행에서 추적 손실·도메인 리로드·Play 재진입 후 동일 예외와 AABB 오류가 재발하는지 확인해야 한다.

## 2026-08-16 추가: Play Mode 중 Controller Education 설정 실행 재현

### 재현과 원인

- `3_PPE_Room_Train_Test` Play Mode에서 `Tools > PPE > Configure Controller Education Entry`를 실행하자 `CurveVisualController.ComputeFallBackLine`의 길이 0 배열 `IndexOutOfRangeException`과 여러 `TrackedDeviceGraphicRaycaster`의 `KeyNotFoundException`이 반복됐다.
- 실행 중인 XR Ray·Canvas 계층의 직렬화 참조를 Editor 설정 도구가 변경하면서 XRI BeforeRender와 Raycaster 캐시 수명주기 중간에 구성이 바뀐 것이 직접 재현 조건이다.
- 디스크 씬 diff는 직전 Edit Mode 저장본과 동일해 Play Mode 실행분이 추가 저장된 흔적은 없었다. Play Mode 종료 후 오류 반복도 멈췄다.

### 조치와 검증

- `PPEControllerEducationSetup.Configure()`와 `PPETrainTestModeValidationHarness.Validate()`에 `EditorApplication.isPlayingOrWillChangePlaymode` 가드를 추가했다. 이후 Play Mode에서는 씬·UI 참조를 변경하거나 검증 씬을 다시 열지 않고 한 번의 명확한 오류로 중단한다.
- Editor 보조 빌드는 오류 0개로 완료했다.
- Edit Mode에서 설정을 다시 적용해 상세교육이 기본 가이드의 좌우 시각물 쌍을 재사용하도록 저장했고, `Tools > PPE > Validate Train Test Modes` PASS를 확인했다.
