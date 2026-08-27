# 2026-07-01 타이틀·앱 시작 흐름·혼합기 내부 환경 회의록

- 날짜: 2026-07-01
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 대상 플랫폼: Meta Quest/Android 및 PC OpenXR

## 오늘의 목표

- VR 애플리케이션 시작 씬과 타이틀 씬의 흐름을 확정한다.
- 타이틀 로고와 파트너 로고의 표시 방식 및 배치를 확정한다.
- 화학 혼합기 내부를 VR에서 360도로 확인할 수 있는 공간으로 구현한다.

## 합의 및 결정 사항

### 애플리케이션 시작 흐름

- 애플리케이션 최초 실행 씬은 `Assets/Scenes/0_App.unity`로 한다.
- Build Settings의 씬 순서는 다음과 같이 구성한다.
  1. `0_App`
  2. `1_TitleScene`
  3. `2_Intro`
  4. `Confined Space Scene_half`
- `AppSceneBootstrap`이 App 씬 시작 후 한 프레임을 기다리고 `1_TitleScene`을 비동기로 로드한다.
- App 씬의 Main Camera 배경은 검정색 Solid Color로 사용한다.
- 타이틀 연출이 끝나면 로고를 페이드 아웃하고 `2_Intro` 씬으로 자동 전환한다.

### 타이틀 씬 로고 배치

- `Canvas/Logos`의 파트너 로고는 화면 하단 중앙에 배치한다.
- 로고 순서는 왼쪽부터 `to21 → hongdae → team`으로 한다.
- 자동 정렬에는 `Horizontal Layout Group`을 사용한다.
- 각 로고는 원본 비율을 유지하고 동일한 시각 높이로 정렬한다.

### 타이틀 로고 연출

- 타이틀 로고가 먼저 페이드 인되고, 설정된 지연 후 파트너 로고가 페이드 인된다.
- 두 로고 그룹은 설정된 시간 동안 유지한 뒤 함께 페이드 아웃한다.
- 페이드 시간과 로고 사이 지연은 `TitleSplashController` Inspector에서 조정 가능하게 한다.
- XR 시작 직후 타이틀 로고가 번쩍이는 현상을 방지하기 위해 다음 방식을 사용한다.
  - 알파 0 상태에서 Canvas 강제 갱신
  - 기본 3프레임 GPU/Canvas 예열
  - 타이틀 로고의 `CanvasRenderer.Cull Transparent Mesh` 비활성화
  - 예열 이후 실제 페이드 시작

### 화학 혼합기 내부 환경

- 일반 큐브맵 스카이박스 대신 실제 깊이와 머리 위치 이동이 반영되는 3D 내부 공간을 사용한다.
- 기본 구조는 다음과 같다.
  - 원통형 스테인리스 내벽
  - 원형으로 성형된 상단 돔
  - 오목한 하단
  - 중앙 회전축
  - 다단 혼합 블레이드
  - 원형 보강링
- 내부 벽의 면 방향은 안쪽을 향하게 하며, 외부에서는 탱크 벽이 렌더링되지 않도록 한다.
- 전용 URP 셰이더로 브러시드 스테인리스, 패널 이음선과 금속 하이라이트를 표현한다.
- 생성 도구 메뉴는 `Tools > Environment > Create Chemical Mixer Interior`를 사용한다.
- 기존 혼합기를 카메라 중심으로 옮길 때는 `Tools > Environment > Center Selected Mixer On Main Camera`를 사용한다.
- VR 카메라는 혼합기 중앙에 놓고, Root Scale은 `(1, 1, 1)`, 반경은 기본 3~5m 범위를 권장한다.
- 현재 확인용 씬은 `Assets/Scenes/4_InsideMixer.unity`이다.

### 혼합기 맨홀 구성

- 카메라 시작 위치 바로 뒤쪽인 혼합기 로컬 `-Z` 벽에 맨홀을 배치한다.
- 맨홀은 원형 개구부, 고정 프레임, 왼쪽 세로 힌지, 원형 문으로 구성한다.
- 문에는 힌지 반대쪽 세로형 손잡이를 부착한다.
- 맨홀 문은 약 105도로 열리고 1.2초 동안 자동 보간하여 움직인다.
- 초기 프로토타입에서는 문 또는 손잡이 선택 시 자동으로 열고 닫는 방식을 사용한다.
- 문과 손잡이는 `XRSimpleInteractable` 선택 이벤트를 사용해 XR 컨트롤러와 손 핀치 입력에서 동일하게 열고 닫는다.
- 현재 개구부는 어두운 내부면을 사용한 시각적 표현이며, 실제 관통 구멍이 필요하면 셰이더 Alpha Clip 또는 벽 메시 절개를 추가한다.

### insideMixer XR 구성

- 단독 카메라에 직접 Center Eye 포즈를 적용하는 방식은 사용하지 않는다.
- 공식 Starter Assets의 `XR Origin (XR Rig)`을 기준 XR 구조로 사용한다.
- 기존 `XRHeadTrackedCamera`는 XR Origin 카메라와 포즈가 중복될 수 있으므로 씬에서 제거한다.
- XR Origin에는 공식 `Left Controller`, `Right Controller`와 XR Hands의 `Left Hand`, `Right Hand` 시각화를 구성한다.
- 룸스케일 및 Quest Link 원점 차이는 카메라 Transform을 직접 고정하지 않고 `XROrigin.MoveCameraToWorldLocation`으로 보정한다.
- 혼합기 내부 시작 눈 위치는 월드 좌표 `(-0.472, 1.0, -12.14)`, 기본 전방은 `+Z`로 한다.
- 혼합기 바닥에 보행용 Collider가 없으므로 현재 씬에서는 XR Rig의 `Gravity`와 `Jump`를 비활성화한다.
- OpenXR Hand Tracking Subsystem과 Meta Hand Tracking Aim을 Android 및 Standalone에서 활성화한다.
- Quest의 실제 손 추적과 모션 컨트롤러는 입력 모달리티에 따라 전환한다.

## 구현 파일

- `Assets/Scenes/0_App.unity`
- `Assets/Scenes/1_TitleScene.unity`
- `Assets/Scenes/2_Intro.unity`
- `Assets/Scenes/4_InsideMixer.unity`
- `Assets/Scripts/AppSceneBootstrap.cs`
- `Assets/Scripts/TitleSplashController.cs`
- `Assets/Scripts/ChemicalMixerInterior.cs`
- `Assets/Scripts/MixerManholeController.cs`
- `Assets/Scripts/ManholeToggleTarget.cs`
- `Assets/Scripts/XRStartPoseAligner.cs`
- `Assets/Editor/ChemicalMixerInteriorMaker.cs`
- `Assets/Editor/InsideMixerXRSetup.cs`
- `Assets/Shaders/ChemicalMixerInterior.shader`
- `Docs/Bug/2026-07-01_TitleScene-XR-Logo-Fade-Flash.md`

## 검증 결과

- 타이틀 로고의 초기 번쩍임이 사전 렌더링 방식으로 해결됨을 확인했다.
- 파트너 로고가 하단 중앙에서 지정 순서로 자연스럽게 페이드 인됨을 확인했다.
- 혼합기 내부에서 벽, 상단 돔, 축과 블레이드가 보이는 것을 확인했다.
- Main Camera 중심 배치를 통해 VR에서 고개를 돌렸을 때 전 방향을 볼 수 있는 구조로 구성했다.
- Play Mode에서 XR Origin이 바닥 Collider 없이 중력에 의해 Y 약 -4878까지 낙하하는 원인을 확인했다.
- Gravity와 Jump를 비활성화하고 실행 중 카메라를 혼합기 내부 시작 좌표로 복구했다.
- XR Origin, 좌·우 컨트롤러, 좌·우 XR Hands 및 입력 모달리티 연결을 씬에 적용했다.

## 후속 확인 사항

- 실제 Quest 기기에서 혼합기 내부 360도 시야와 근거리 클리핑을 확인한다.
- Quest에서 컨트롤러 모드와 실제 손 추적 모드가 정상 전환되는지 확인한다.
- 맨홀 문과 손잡이의 XR 선택 범위 및 핀치 조작감을 Quest에서 확인한다.
- 금속 셰이더의 반사 강도와 밝기가 HMD에서 과도하지 않은지 조정한다.
- App 씬에서 TitleScene으로의 전환 시 검정 프레임과 XR 초기화 타이밍을 확인한다.
- 타이틀 이후 `2_Intro` 씬으로 자동 전환되는 타이밍을 HMD에서 확인한다.
