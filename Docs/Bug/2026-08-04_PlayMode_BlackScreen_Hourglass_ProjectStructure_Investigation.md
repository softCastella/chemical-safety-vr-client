# Play Mode 검정 화면·모래시계 및 프로젝트 구조 종합 조사

## 조사 목적

여러 차례 Unity와 Quest Link를 다시 시작해도 Play Mode에서 검정 화면과 모래시계가 반복되는 증상을 대상으로, 이전에 확인된 증상과 현재 프로젝트 구조상의 문제 후보를 한 문서로 정리한다.

이번 문서는 단일 버그의 수정 기록이 아니라 다음 문제를 함께 분리해서 판단하기 위한 종합 조사 보고서다.

- XR 런타임과 Quest Link의 연결 실패
- 개발 기준 씬을 직접 Play Mode로 실행할 때 발생하는 무거운 초기화 경로
- 씬에 집중된 오브젝트·렌더러·런타임 컴포넌트 수
- 프로젝트 전체 에셋 임포트 규모와 FBX 파편화
- 런타임 코드의 초기화 및 프레임별 처리 부하
- 이전 오류가 현재 검정 화면과 같은 원인인지 여부

## 조사 범위와 한계

다른 대화방의 원문 대화 기록에는 직접 접근할 수 없었다. 대신 프로젝트 내부에 남아 있는 `Docs`, `Assets/Docs`, Unity 로그 및 현재 씬·스크립트를 기준으로 이전 세션의 증상을 대조했다. 따라서 이 문서의 “이전 세션” 기록은 프로젝트에 저장된 문서와 로그에 기록된 범위다.

## 현재 실행 경로

`ProjectSettings/EditorBuildSettings.asset`의 첫 번째 활성 씬은 다음과 같다.

```text
Assets/Scenes/0_App.unity
```

`0_App.unity`는 `AppSceneBootstrap`이 한 프레임 뒤 `1_Title`을 로드하는 가벼운 부트 씬이다. 따라서 정상적인 앱 실행 경로는 `0_App → 1_Title → 2_Intro → 3_PPE_Room` 순서로 시작한다.

그러나 Unity Editor의 Play 버튼은 일반적으로 Build Settings의 첫 씬이 아니라 현재 열려 있는 씬을 직접 실행한다. 현재 조사 당시에는 다음 작업 씬이 직접 실행되고 있었다.

```text
Assets/Scenes/3_PPE_Room_HandTest_scale.unity
```

`3_PPE_Room_HandTest_scale.unity`는 현재 개발 기준 씬이므로 이 씬에서 직접 시작하는 것은 의도된 작업 경로다. 따라서 이를 “잘못된 진입 씬”이나 “씬 오염”으로 분류하지 않는다. 다만 앱 전체 실행 경로(`0_App`)와 개발 씬 직접 실행 경로가 서로 다른 초기화 조건을 사용하므로, 두 경로의 차이를 명시적으로 관리해야 한다. 현재 문제의 핵심은 씬 선택 자체가 아니라, 개발 기준 씬이 이미 무거운 XR 테스트 씬이라는 점이다.

## 확인된 증상 기록

### 1. 검정 화면과 모래시계

- Play Mode 진입 후 헤드셋에 검정 화면과 모래시계가 표시된다.
- Unity를 여러 번 다시 켜도 같은 증상이 반복된다.
- 실행할 때마다 증상의 세부 원인이 달라지는 양상이 있다.
- 어떤 세션에서는 Unity Editor가 첫 프레임에서 멈추거나 일시정지 상태에 남았다.

### 2. Meta Quest Link·OpenXR 런타임 오류

이전 기록에서 다음 오류가 확인되었다.

- `XR_ERROR_RUNTIME_UNAVAILABLE`
- `XR_ERROR_FORM_FACTOR_UNAVAILABLE`
- `XR_ERROR_SESSION_LOST`
- `ovrError_DisplayLost`
- OpenXR `InitializeLoaderSync` 대기
- Meta Runtime IPC, Highwind, DISCO, RIPC 연결 재시도

현재 조사 세션에서도 OpenXR Display와 XR 세션이 초기화된 뒤 Meta Runtime의 `HandInputDataServer` 및 `RipcRemoteTransportServer_link` 재연결이 반복되었고, Unity 프로세스 CPU 사용률이 높게 유지되었다. 이 경우 Unity 씬 자체가 정상이어도 Quest Link 영상이 검정 화면으로 남을 수 있다.

### 3. 손 추적 데이터 이상

이전 `HandTest` 기록에서 손 또는 컨트롤러 추적값에 `NaN` 또는 `Infinity`가 들어오면서 다음 오류가 발생했다.

- `Invalid worldAABB`
- `Invalid localAABB`
- `Invalid AABB`
- 유효하지 않은 거리 계산 및 정렬값

이 문제는 레이와 레티클 입력값에 유한값 검사를 추가한 뒤 관련 새 오류가 발생하지 않는 것으로 검증되었다. 다만 Quest Link가 불안정할 때 추적값이 다시 비정상화될 가능성은 남아 있다.

### 4. GameObject 활성화 재진입 예외

Hazmat 선택 종료 흐름에서 다음 예외가 발생한 기록이 있다.

```text
GameObject is already being activated or deactivated.
```

선택 중인 오브젝트를 `SetActive(false)`로 즉시 비활성화하면서 XRI의 `selectExited` 및 Transform 복원 흐름과 활성 상태 변경이 재진입한 것이 원인이었다. 선택 취소 후 다음 프레임에 상태를 적용하도록 변경한 별도 수정 기록이 있다. 이 문제는 과거 검정 화면·모래시계의 원인 중 하나였지만, 모든 세션의 공통 원인은 아니다.

### 5. 현재 씬의 참조 구성 오류

현재 작업 씬에는 `PPEHazmatEquipController`가 두 개 존재한다.

- `hazmat_suit_on_1`
- `hazmat_suit_on_0`

`hazmat_suit_on_1`의 컨트롤러는 자신이 표시 대상 오브젝트에 부착되어 있지 않은 중복 구성으로 판단되며, 다음 오류를 출력한다.

```text
PPEHazmatEquipController must be authored on the visible hazmat_suit_on object ...
```

이 오류는 현재 씬의 구성 오류로 확인되었지만, 현재까지의 로그만으로 검정 화면 전체를 발생시킨 직접 원인이라고 단정할 수는 없다.

## 현재 작업 씬 규모

`3_PPE_Room_HandTest_scale.unity`를 YAML 기준으로 조사한 결과는 다음과 같다.

| 항목 | 수량 |
|---|---:|
| GameObject | 1,103 |
| MeshFilter | 641 |
| MeshRenderer | 641 |
| SkinnedMeshRenderer | 16 |
| MonoBehaviour | 313 |
| Animator | 10 |
| Camera | 3 |
| Light | 2 |
| Rigidbody | 4 |
| BoxCollider | 24 |

비교 대상으로 확인한 `3_PPE_Room.unity`도 GameObject 약 1,032개, MeshRenderer 612개 규모였다. 즉 현재 문제는 단순히 특정 오브젝트 하나가 아니라, XR 입력·UI·PPE 상호작용·시각 효과·거울 렌더링이 하나의 씬에 집중된 구조와 관련이 있다.

## 프로젝트 에셋 구조 문제

### 확인된 규모

- FBX 358개, 총 약 90.9MB
- 50KB 미만의 작은 FBX 214개
- Prefab 545개
- `TripoModels` 약 2,876개 파일
- `FreeIndustrialModels`, `RPG_FPS_game_assets_industrial`, `3D Models`, `Samples` 등 대형 에셋 묶음이 모두 `Assets` 아래에 존재

### 문제 분류

이 구조는 다음 문제로 부를 수 있다.

- **Asset Import Bloat**: 실제 실행에 사용하지 않는 에셋까지 Unity가 임포트·검색하는 문제
- **FBX Asset Fragmentation**: 하나의 모델이 지나치게 많은 작은 FBX로 분할된 문제
- **Third-party Asset Contamination**: 샘플·외부 모델·실험 에셋이 프로덕션 에셋 영역과 분리되지 않은 문제
- **Runtime Scene Bloat**: 작업 씬에 시각·상호작용·검증 컴포넌트가 계속 누적된 문제

에셋 파일 총량 자체가 매 프레임 CPU 부하를 직접 만든다고 볼 수는 없다. 하지만 다음 비용은 확실히 증가한다.

- 프로젝트 최초 열기 및 AssetDatabase 갱신 시간
- FBX·머티리얼·셰이더 임포트 시간
- 도메인 리로드 및 스크립트 컴파일 대기
- 중복 프리팹·머티리얼·메시 참조 관리 비용
- 어떤 에셋이 실제 씬에 사용되는지 추적하기 어려운 비용

작은 FBX 214개는 모델이 과도하게 파편화되었을 가능성을 보여주는 강한 구조적 신호다. 다만 원본 FBX를 확인하지 않고 삭제하거나 합치는 것은 프리팹과 GUID 참조를 깨뜨릴 수 있으므로 별도 검증이 필요하다.

## 런타임 코드 부하 후보

### 1. PlanarMirrorRenderer — 가장 우선순위가 높은 후보

현재 씬에는 `PlanarMirrorRenderer`가 1개 존재한다. 이 컴포넌트는 `[ExecuteAlways]`이며 `RenderPipelineManager.beginCameraRendering`에 등록된다. 원본 카메라가 거울을 볼 수 있으면 반사 카메라를 별도로 렌더링한다.

현재 경로는 다음과 같다.

```text
XR/Game/Scene 카메라 렌더링
    → beginCameraRendering
    → 거울 가시성 검사
    → reflectionCamera 설정
    → 별도 전체 씬 렌더링
```

XR 카메라와 함께 동작하면 한 프레임에 추가 카메라 렌더링이 발생할 수 있다. 이는 현재 씬의 641개 MeshRenderer와 결합될 때 가장 큰 런타임 부하 후보다. 검정 화면의 직접 원인으로 확정된 것은 아니지만, XR 런타임 연결이 불안정한 상태에서 초기화와 렌더링 부담을 키우는 요인으로 판단한다.

### 2. XR 레티클·손·카드 시스템

현재 씬에는 다음 매 프레임 또는 프레임 직전 처리 시스템이 집중되어 있다.

- `XRNearFarReticleVisual`: `LateUpdate` 및 `Application.onBeforeRender`
- `HandGripAnimator`: 손 입력 액션과 Animator 파라미터 갱신
- `XRLocationMarkerPulse`: 21개 인스턴스의 `Update`
- `SciFiCardDepthResponse`: 카드 레이어의 `LateUpdate`
- `AuthoredTransformRuntimeLock`: Transform을 매 프레임 원래 값으로 보정
- `PPEMarkerToggleGrab`: 선택 상태와 손 그립 상태 보정

각 컴포넌트 하나만으로 치명적이라고 할 수는 없지만, 이들이 작업 씬 하나에 동시에 활성화되어 있다. 특히 Transform 잠금, 손 추적, 레티클, XRI 상호작용이 같은 프레임에 실행되면 입력 상태 변경과 Transform 복원이 서로 경쟁할 가능성이 있다.

### 3. `[ExecuteAlways]` 및 생성 코드

다음 구성요소는 Editor와 Play Mode 양쪽에서 활성화될 수 있다.

- `PPEBackgroundRoom`
- `ChemicalMixerInterior`
- `PlanarMirrorRenderer`

현재 `PPEBackgroundRoom`은 `OnEnable`에서 기존 생성 룸의 머티리얼을 복구하는 경로를 사용하고, 명시적인 `BuildRoom` 호출에서만 룸을 재생성하도록 분리되어 있다. `ChemicalMixerInterior`도 `OnEnable`에서는 기존 생성 geometry에 머티리얼을 적용하고, `Rebuild` 컨텍스트 메뉴에서만 geometry를 재생성한다.

따라서 현재 조사 범위에서는 자동 geometry 삭제·재생성이 검정 화면의 주원인으로 확인되지는 않았다. 다만 `[ExecuteAlways]` 컴포넌트가 씬을 열거나 컴파일할 때 반복 작업을 일으킬 수 있으므로 지속적인 에디터 부하 후보로 남긴다.

## 원인 후보 판정

| 문제 후보 | 판정 | 근거 |
|---|---|---|
| Quest Link·OVRService·Meta Runtime 미준비 | 확인됨 | 여러 세션에서 `XR_ERROR_*`, DisplayLost, RIPC 재연결 확인 |
| 개발 기준 씬 직접 Play | 의도된 경로이며 부하 요인 | 현재 개발 기준 씬이 `3_PPE_Room_HandTest_scale`이고, 직접 실행 시 전체 XR 테스트 구성이 한 번에 초기화됨 |
| 작업 씬 자체의 과대화 | 확인됨 | 1,103 GameObject, 641 MeshRenderer, 313 MonoBehaviour |
| PlanarMirror 추가 렌더링 | 가능성 높음 | 카메라 렌더 이벤트마다 반사 카메라 렌더 |
| FBX 파편화와 에셋 임포트 비대화 | 구조 문제 확인, 직접 원인은 추가 검증 | FBX 358개, 소형 FBX 214개, 외부 에셋 대량 포함 |
| 손 추적 NaN/Infinity | 과거 원인 확인, 현재 직접 원인은 미확정 | 이전 `Invalid AABB` 기록 및 유한값 방어 적용 |
| 활성 상태 변경 재진입 | 과거 원인 확인, 수정 기록 있음 | `GameObject is already being activated or deactivated` |
| 중복 `PPEHazmatEquipController` | 현재 구성 오류 확인 | `hazmat_suit_on_1`의 참조 조건 불충족 |
| `[ExecuteAlways]` 자동 생성 부작용 | 현재 주원인으로는 미확정 | 현재 주요 생성 경로는 명시적 명령으로 분리됨 |

## 현재까지의 근본 원인 해석

이번 증상은 하나의 고정된 버그라기보다 다음 세 층이 겹쳐 발생하는 구조적 문제로 보는 것이 타당하다.

```text
프로젝트 구조 문제
  ├─ 작업 씬과 부트 씬의 Play 진입 경로 분리 부족
  ├─ 외부·샘플·실험 에셋의 임포트 영역 미분리
  └─ FBX와 프리팹의 누적·파편화

씬 구성 문제
  ├─ 1,000개 이상의 GameObject
  ├─ 600개 이상의 MeshRenderer
  ├─ 중복 PPE 컨트롤러
  └─ XR/UI/상호작용/검증 시스템 집중

실행 환경 문제
  ├─ Quest Link 및 Meta Runtime 세션 불안정
  ├─ 손 추적 데이터 이상
  └─ XR 카메라 외 반사 카메라 추가 렌더링
```

따라서 현재 검정 화면·모래시계는 **개발 기준 씬에서 무거운 XR 초기화를 시작하고, Meta 런타임이 불안정할 때 추가 렌더링과 입력 처리까지 동시에 수행하는 현상**으로 요약할 수 있다.

## 영향 범위

- Unity Editor의 Play Mode 진입 안정성
- Quest Link 영상 출력과 OpenXR 세션 유지
- 작업 씬의 첫 프레임 초기화 시간
- AssetDatabase 임포트 및 프로젝트 재개 시간
- XR 손 추적 및 Near-Far 레이 입력
- PPE 장착·선택·검사 패널 상태 전환
- 거울·카드·위치 마커 등 시각 효과의 프레임 시간

## 권장 조치 순서

### 1단계: 실행 경로 구분과 안정화

- `0_App`를 정상 앱 진입점으로 유지한다.
- `3_PPE_Room_HandTest_scale`을 현재 개발 기준 씬으로 명시하고, 직접 Play하는 경로를 정상 개발 경로로 취급한다.
- 작업 씬 테스트와 앱 전체 테스트를 명확히 구분한다.
- 두 경로의 XR 초기화·씬 로드·Quest Link 요구 조건을 각각 기록한다.
- 컴파일과 XR loader 초기화가 끝나기 전에 Play Mode에 진입하지 않는다.

### 2단계: 가장 큰 런타임 부하 격리

- `PlanarMirrorRenderer`를 기본 Play Mode에서 비활성화해 프레임 시간과 XR 안정성을 비교한다.
- 거울을 반드시 사용해야 한다면 카메라별 중복 렌더링을 제한하고, 가시성·갱신 주기·해상도를 별도 설정한다.
- XR 레티클, 손 애니메이터, 카드 깊이 반응, 위치 마커를 기능별로 선택 비활성화하여 부하를 분리 측정한다.

### 3단계: 씬 구성 정리

- `hazmat_suit_on_1`의 중복 `PPEHazmatEquipController`를 씬 참조 검증 후 정리한다.
- 작업 씬에서 사용하지 않는 UI·검증·실험용 오브젝트를 별도 테스트 씬으로 이동한다.
- PPE 룸을 하나의 거대한 테스트 씬으로 유지하지 말고, 입력 테스트·UI 테스트·렌더링 테스트를 분리한다.

### 4단계: 에셋 저장소 정리

- 실제 사용 에셋과 샘플·외부 원본을 목록화한다.
- 참조가 확인된 FBX만 통합하거나 LOD·메시 병합을 적용한다.
- 원본 삭제나 폴더 이동은 GUID와 프리팹 참조를 확인한 뒤 수행한다.
- 장기적으로는 프로덕션 프로젝트와 에셋 탐색·실험 프로젝트를 분리한다.

## 완료한 검증

- Build Settings의 첫 번째 활성 씬 확인
- `0_App`의 부트스트랩 동작 확인
- 현재 Unity 활성 씬 확인
- 주요 PPE 씬의 GameObject·Renderer·Animator 규모 집계
- 프로젝트 FBX·Prefab·대형 에셋 폴더 규모 집계
- 런타임 `Update`, `LateUpdate`, `onBeforeRender`, 카메라 렌더 이벤트 코드 조사
- 기존 검정 화면·모래시계·XR 오류 문서 대조
- 현재 작업 씬의 중복 `PPEHazmatEquipController` 확인

## 아직 필요한 수동 검증

- `PlanarMirrorRenderer` 비활성화 전후의 CPU/GPU 프레임 시간 비교
- `0_App`에서 시작한 앱 실행과 작업 씬 직접 실행의 XR 초기화 성공률 비교
- Quest Link가 정상 연결된 상태에서 작업 씬의 첫 프레임 대기 시간 측정
- Quest 헤드셋 양쪽 눈에서 거울·손·카드가 정상 렌더링되는지 확인
- 중복 Hazmat 컨트롤러 제거 후 Console 오류와 Play Mode 재진입 확인
- 실제 사용하지 않는 FBX를 분리했을 때 AssetDatabase 임포트 시간 비교

## 2026-08-04 22시 추가 조사: Editor.log 기반 세션별 XR 상태 분석

같은 날 저녁 Editor.log(`%LOCALAPPDATA%\Unity\Editor\Editor.log`)를 기준으로 Play Mode 시도별 OpenXR 세션 상태 전이를 재구성했다.

| 시각 | 실행 씬 | XR 세션 도달 상태 | 결과 |
|---|---|---|---|
| 20:58 | `3_PPE_Room_HandTest_scale` 직접 Play | `SYNCHRONIZED`까지, `VISIBLE` 미도달 | 약 17초간 검정 화면·모래시계 후 수동 종료 |
| 21:46 | `0_App` 경로 | `SYNCHRONIZED`까지, `VISIBLE` 미도달 | 약 3초 후 종료 |
| 21:47 | `0_App → 1_Title → 2_Intro` | `FOCUSED` 도달(정상 표시) | 약 8초 만에 정상 렌더링, 21:47:44 종료 |
| 21:50 | `Pipeline/Example/Scene.unity`(샘플 씬) | `IDLE`에서 정지, `READY` 미도달 | 30초 후 종료, `XR_META_performance_metrics: Attempted to stop before session ready` |

핵심 판독은 다음과 같다.

- 21:47 세션이 `FOCUSED`까지 정상 도달했으므로 Quest Link·OpenXR 런타임 자체는 살아 있다. 증상은 런타임 고장이 아니라 **에디터 부하 조건에 따라 재현되는 문제**다.
- 검정 화면·흰 모래시계는 Quest Link의 로딩 표시이며, 로그상으로는 세션이 `SYNCHRONIZED`(또는 `IDLE`)에서 멈춰 Unity가 표시 가능한 프레임을 제때 제출하지 못한 상태와 일치한다.
- 실패 세션마다 Play 진입 직전·직후에 씬 백업 로드 3~4초, 도메인 리로드 3~5초, 스크립트 컴파일 12~18초, ShaderGraph 임포트가 겹쳐 있었다.

### 이날 새로 확인된 최대 부하 요인: 대량 에셋 삭제 작업

이날 하루 동안 Asset Pipeline Refresh 로그에 삭제 파일 수가 115 → 97 → 462 → 13 → 141 → 473건으로 연속 기록되었다. git 작업 트리 기준으로 삭제된 `.meta`가 646건이며, 대부분 `Assets/3D Models` 계열이다. 각 삭제 물결마다 다음이 반복되었다.

- AssetDatabase 재스캔 및 재임포트 (AssetImportWorker 3개가 22:01까지 계속 동작)
- asmdef 변경으로 인한 강제 스크립트 재컴파일(최대 17.9초)과 도메인 리로드
- `Prefab Variant problem. Missing Prefab Variant parent: 'Hangar_v2_6 Variant (Missing Prefab with guid: 9df406dc401707343b6396bd2ba69f93)'` — 삭제로 인한 프리팹 참조 파손 발생

이 임포트·컴파일 창과 Play 진입이 겹치는 것은 본 문서와 AGENTS.md가 이미 경고한 "컴파일·도메인 리로드 중 Quest/OpenXR Play 진입 금지" 조건에 정확히 해당하며, 21:50 세션이 `IDLE`에서 정지한 직접적인 정황이다.

한편 삭제된 646개 GUID를 `3_PPE_Room_HandTest_scale.unity` 본문과 대조한 결과 직접 참조는 0건이었다. 즉 씬 자체가 삭제로 파손된 것은 아니고, 삭제 작업이 만든 **에디터 부하와 불안정한 임포트 상태**가 문제다.

### 판정 갱신

- 근본 원인(이날 세션 기준): ① 대량 에셋 삭제로 인한 연속 재임포트·재컴파일 중 Play 진입, ② 기존에 확인된 무거운 작업 씬의 첫 프레임 초기화 지연이 결합되어 XR 세션이 `VISIBLE`로 승격되지 못함.
- Quest Link 런타임 고장, 씬 참조 파손은 이날 증상의 직접 원인이 아님을 로그로 확인.
- 재현 회피 절차: 에셋 삭제·임포트 작업이 완전히 끝나고(우하단 스피너와 컴파일 종료 확인) 에디터가 유휴 상태일 때만 Play 진입. 첫 검증은 `0_App` 경로로 수행.

### 2026-08-04 22:35 사용자 실측 검증 결과

사용자가 `3_PPE_Room_HandTest_scale`에서 다음 두 조치를 적용한 뒤 Play Mode가 정상 동작함을 확인했다.

- 배경(배경등) 관련 에셋 다수 제거
- 거울 크기 축소

이로써 "무거운 씬 렌더링 부하 + `PlanarMirrorRenderer` 반사 카메라의 이중 렌더링이 XR 첫 프레임 제출을 지연시켜 세션이 `VISIBLE`로 승격되지 못했다"는 진단이 실측으로 확인되었다. 거울이 있는 씬에서는 렌더러 하나를 줄일 때 본 렌더와 반사 렌더 양쪽에서 비용이 감소하므로, 배경 정리의 효과가 배가된다.

남은 권장 사항: 거울을 계속 사용할 경우 반사 렌더의 갱신 주기·해상도 제한을 별도 설정으로 두고, 씬 부하가 다시 누적되지 않도록 배경·검증용 오브젝트를 별도 테스트 씬으로 분리한다.

이 씬에서 거울이 필수라는 확인에 따라 `PlanarMirrorRenderer`에 부하 제어용 직렬화 옵션 3종을 추가했다. 기본값은 모두 기존 동작과 동일해 씬에 저장된 값을 변경하지 않으며, Inspector에서 필요할 때만 조정한다.

- `renderFrameInterval`: Play Mode에서 반사를 N프레임마다 한 번만 갱신 (기본 1 = 매 프레임)
- `maxSourceDistance`: 카메라가 거울에서 이 거리보다 멀면 반사 갱신 생략 (기본 0 = 무제한)
- `skipSceneViewWhilePlaying`: Play Mode 중 Scene 뷰 카메라용 반사 렌더 생략 (기본 꺼짐)

수동 검증 필요: 위 옵션을 실제 값(예: interval 2, 거리 6m, Scene 뷰 생략 켬)으로 설정한 뒤 Quest 헤드셋에서 거울 반사 지연이 허용 범위인지, Play 진입이 안정적인지 확인한다.

### 2026-08-04 23시 최종 판정: 헤드셋 미착용·디스플레이 절전으로 인한 세션 보류

거울 비활성화 상태에서도 증상이 재현되어 GPU 실측(2초 간격 `nvidia-smi` 샘플링)과 세션 상태 전이를 함께 대조한 결과, 결정적인 원인이 확인되었다.

- 22:57:12 Play 세션은 XR 세션 생성 후 `IDLE`에서 2분 46초간 보류되다가 22:59:58에야 `READY`를 받았고, 23:01:10에 `FOCUSED`까지 정상 도달했다.
- 보류 구간 동안 GPU 사용률은 7~9%, VRAM은 3.8/6GB로 유휴 상태였다. 따라서 GPU 과부하·VRAM 부족은 이 실패 모드의 원인이 아니다.
- `READY`와 `VISIBLE`은 Meta 런타임이 헤드셋 상태(착용·디스플레이 활성)를 기준으로 부여한다. 헤드셋을 벗고 모니터에서 Play를 누르면 근접 센서로 디스플레이가 잠들고, 런타임이 세션 승격을 보류하여 헤드셋 착용 시 검정 화면·모래시계로 보인다.
- 같은 날 성공 세션(21:47, 22:43, 22:48)은 모두 직전 착용 직후라 `READY`가 1ms 내에 부여되었다.
- 가벼운 씬이 문제없는 이유: Play 클릭 후 1~2초 만에 세션이 생성되어 헤드셋이 잠들기 전에 연결된다. 이 씬은 도메인 리로드와 씬 로드(백업 씬 통합 3.4~4.2초 포함, 총 10초 이상)가 길어 세션 생성 시점에 헤드셋이 이미 잠들어 있을 확률이 높다. 장소·헤드셋·PC가 달라도 같은 작업 순서로 재현되는 이유이기도 하다.

#### 재발 방지 절차

- Play를 누른 직후 바로 헤드셋을 착용하거나, 헤드셋을 쓴 채 Link 가상 데스크톱에서 Play를 누른다.
- 헤드셋 전원 설정에서 디스플레이 끄기 시간을 늘린다.
- 검정 화면·모래시계 상태여도 1~3분 내 세션이 승격될 수 있으므로 즉시 Play를 종료하지 않는다.
- 씬 경량화(배경 정리, 거울 부하 옵션)는 Play 진입~세션 생성 사이의 취약 구간을 줄이는 효과가 있으므로 병행한다.

### 추가로 확인된 부수 문제

- 21:50 세션에서 `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class...` 발생. 구형 `StandaloneInputModule` EventSystem을 가진 샘플 씬(`Pipeline/Example/Scene.unity`)을 Play한 것이 원인이며, 프로젝트 씬 문제가 아니다.
- 에디터 시작 시 `Assets/_Recovery/0 (3).unity` 복구 씬이 열린 기록이 있어 이전 세션에 크래시가 있었음을 시사한다.
- 프로젝트가 OneDrive 동기화 경로(`OneDrive\문서`) 아래에 있어 대량 파일 삭제·재임포트 시 동기화 I/O가 부하를 가중할 수 있다.

### 2026-08-05 00시: 오늘 PPE 추가분 이진 탐색 — 헬멧이 Play 동결 유발

`6_LoadingScene` → `3_PPE_Room_HandTest_scale_1`(오늘 PPE 없음)은 정상.  
`scale_0`에 커밋 `40ea6f1`(ppe 장착·거울 성공 시점)을 넣은 뒤 오브젝트를 단계적으로 켠 결과:

| 단계 | 상태 | 결과 |
|---|---|---|
| 오늘 PPE 전부 끔 | 거울만 | 정상 |
| `hazmat_suit_on_0` + `PPEConditionAppearance` | 오염 외형 | 정상 |
| + `PPEHazmatEquipController` + body 앵커 | 방호복 장착 | 정상 |
| + 헬멧(`helmet_on`, `PPEHelmetEquipController`, `helmet_suit`, 헬멧 inspection 스크립트) | 헬멧 | 주변시야 깨짐 → 검정 → Game 뷰 검정 → 에디터 동결 |

재현: 헬멧 그룹이 켜진 상태에서 방호복을 잡고 흔들면 GPU/에디터가 멈춤. 거울을 보지 않아도 재현. 헬멧만 끄면 동일 조작에서 안정.

동결 시점 실측: Unity PID Not Responding, VRAM 약 5032/6144 MiB.  
로그에 `PPEMarkerToggleGrab.ReturnToAuthoredPose`가 kinematic Rigidbody에 `linearVelocity`/`angularVelocity`를 매 프레임 설정하는 오류가 함께 찍힘(헬멧·방호복 inspection 공통). 헬멧 On/Off 판정과 별개로 수정 후보.

현재 조치: `3_PPE_Room_HandTest_scale_0`에서 헬멧 관련 오브젝트·스크립트는 비활성 유지. 방호복 오염/Equip은 켠 상태.

#### 2026-08-05 00:55 헬멧 내부 원인

공식 관찰 호스트는 `helmet`(Renderer 약 41개)이다. 같은 씬에 `helmet_suit`라는 **중복 잡기 호스트**가 더 있다.

- `helmet_suit`도 `equippedVisual → helmet_on`, `XRGrabInteractable`, non-kinematic `Rigidbody`, `PPEMarkerToggleGrab`을 가진다.
- 이진 탐색 때 헬멧 그룹과 함께 `helmet_suit`를 켜 버렸고, 이 오브젝트는 설계상 활성 대상이 아니다(`Validate Helmet Equip`은 inspection 이름을 `helmet`으로 요구).
- `PPEMarkerToggleGrab.LateUpdate`는 미선택 동안 매 프레임 `SetParent` + pose 강제 + (구버전) kinematic에도 velocity 대입을 수행했다. 중량 메시 계층이 두 개 동시에 돌면 CPU/GPU가 붕괴하기 쉽다.

적용한 조치:

1. `PPEMarkerToggleGrab`: 이미 authored pose면 early-out, non-kinematic일 때만 velocity/`Sleep` 호출.
2. `scale_0`에서 공식 경로만 재활성: `helmet` + `helmet_on` + `PPEHelmetEquipController` + 헬멧 ActionPanel. **`helmet_suit`는 비활성 유지**하고 그 Grab/MarkerToggle도 비활성.

수동 검증: 로딩 → `scale_0`에서 방호복 잡고 흔들기, 이어서 안전모 잡기·패널·착용. 동결이 재현되면 `helmet_on`(카메라 자식)만 따로 끈 대조가 다음 단계다.

## 2026-08-05 오늘 작업 기록: `helmet.fbx` 선홍색 머티리얼 및 에디터 컴파일 정지

### 작업 범위

- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`에서 새로 추가한 `helmet` 오브젝트의 시각 문제를 조사했다.
- 헬멧 끈은 정상 색상으로 보이지만 모자 본체가 선홍색/분홍색으로 표시되는 현상을 확인했다.
- `PPE` 계층의 PPE 아이템 마커 오브젝트에 Grab 구성을 적용한 상태를 유지했다.

### 근본 원인

- 선홍색은 의도한 색상이 아니라 Unity에서 셰이더 또는 머티리얼 참조가 유효하지 않을 때 나타나는 오류 표시였다.
- `Assets/FBX/helmet/helmet.fbx`의 메시 파츠가 FBX 내부 머티리얼 서브에셋을 참조하고 있었고, 일부 내부 참조가 정상적인 외부 URP Unlit 머티리얼로 해석되지 않았다.
- 텍스처 자체에는 선홍색을 만들 만한 색상이 없었으므로 텍스처 색상 문제가 아니었다.

### 적용한 변경

- `Assets/FBX/helmet/helmet.fbx.meta`의 머티리얼 위치를 외부 머티리얼 사용으로 변경했다(`materialLocation: 1`).
- `3_PPE_Room_HandTest_scale_0.unity`의 새 헬멧 9개 Renderer가 각 파츠별 `Assets/Materials/PPE/Scene Unlit/Helmet/` 머티리얼을 직접 참조하도록 수정했다.
- 헬멧 본체, 끈 및 부속 파츠를 모두 같은 방식으로 교체했으며, 기존 헬멧 호스트/장착 컨트롤러는 해당 `_0` 씬에서 제거된 상태를 유지했다.

### 컴파일 정지 조사

- Unity Editor 로그에서 C# 컴파일은 `exitcode: 0`으로 완료되었고, 실제 스크립트 컴파일 시간은 약 30초였다.
- 초기 AssetDatabase 갱신은 약 93초였지만, 이후 16분 이상 UI가 컴파일 중으로 남았다.
- 프로세스와 보조 컴파일러의 CPU 사용량 및 로그 갱신이 멈춰 있어 Unity UI가 사실상 정지한 상태로 판단했다.
- Unity AI/MCP 계정 API 30초 타임아웃 경고가 있었으나 C# 컴파일 오류는 확인되지 않았다.
- 사용자 승인 후 Unity 본체 프로세스만 강제 종료했다. 프로젝트 파일과 씬 파일은 삭제하지 않았다.

### 완료한 검증

- Unity 종료 후 씬 파일 크기와 수정 시간이 정상이며, 헬멧 9개 Renderer의 머티리얼 참조가 모두 외부 `type: 2` 머티리얼로 유지되는 것을 확인했다.
- C# 컴파일 오류는 확인되지 않았다.

### 아직 필요한 수동 검증

- Unity를 다시 열고 `3_PPE_Room_HandTest_scale_0` 씬에서 헬멧 모자 본체와 끈의 실제 색상 및 텍스처 표시를 확인해야 한다.
- Play Mode에서 헬멧 Grab/장착 흐름과 Quest/OpenXR 양쪽 눈 렌더링을 확인해야 한다.
- FBX를 재임포트한 뒤에도 외부 머티리얼 참조가 유지되는지 확인해야 한다.

### 하네스 보강

재발 방지를 위해 `Assets/Editor/PPEHelmetFbxUnlitSetup.cs`에 다음 검증·복구 경로를 추가했다.

- `Tools > PPE > Validate Helmet FBX Unlit Materials`: FBX 외부 머티리얼 설정, 파츠별 URP Unlit 머티리얼, basecolor 텍스처, 씬 Renderer 참조를 읽기 전용으로 검증한다.
- `Tools > PPE > Repair Helmet Scene Material References`: `3_PPE_Room_HandTest_scale_0`의 `helmet` 하위 파츠 Renderer가 올바른 외부 머티리얼을 직접 참조하도록 복구하고 씬을 저장한다.
- `Tools > PPE > Convert Helmet FBX Materials to URP Unlit`: 기존 FBX 변환 뒤 씬 머티리얼 참조 복구와 전체 검증까지 이어서 실행한다.
- 배치 검증 진입점 `PPEHelmetFbxUnlitSetup.ValidateBatch`도 추가했다.

코드 변경 후 Unity 컴파일은 오류 없이 완료되었고, Unity 6의 `ModelImporterMaterialLocation.External` obsolete 경고만 남았다. 메뉴 실행과 실제 씬/Quest 시각 검증은 Unity 재실행 후 진행해야 한다.

### 2026-08-05 후속: 헬멧 본체 색 밝기

- 본체는 **흰색** 안전모다. 텍스처가 회색대라 `_BaseColor` `(1,1,1)`만으로는 어둡게 보였다.
- PPE 장식장 흰 판넬과 본체가 비슷한 회색대라 윤곽이 잘 안 보였다.
- `helmet_part_0_Unlit` `_BaseColor`를 `(1.3, 1.3, 1.3)`, 보조 `helmet_part_22_Unlit`는 `(1.2, 1.2, 1.2)`로 맞춤. `(1.4)`는 과하고 `(1.15)`는 판넬과 구분이 약해 중간값으로 확정.
- 끈·하드웨어용 어두운 파츠 머티리얼은 그대로 둔다.


## 2026-08-05 회귀 기록: 거울 검은 화면·Play Mode 지연·헤드셋 주변 시야 깨짐

### 증상

- Play Mode 전후로 거울이 완전히 검게 보이는 현상이 보고되었다.
- 거울과 Game View가 한동안 검은 상태였다가 늦게 켜지고 Play Mode 로드가 느려졌다.
- 헤드셋 주변 시야가 깨지거나, 아래를 볼 때 로비로 이탈하는 증상이 보고되었다.
- 거울이 픽셀화되어 보일 가능성도 제기되었다.

### 원인 후보

- `PlanarMirrorRenderer`의 반사 카메라·RenderTexture·Culling Mask·거울 레이어 조합 불일치.
- 매 프레임 반사 렌더링과 풀장착 모델의 여러 Renderer가 만드는 GPU/메모리 부하.
- 장착 PPE에 남은 패널 호출, marker/Collider/Rigidbody 자동 보정 로그가 만드는 CPU 부하와 초기화 재진입.
- OpenXR 로더가 컴파일·도메인 리로드 중 재시작되는 실행 순서 문제.

현재 로그와 정적 검사만으로 위 후보 중 하나를 확정하지 않았다. 거울을 끈 기준 실행, 거울을 켠 실행, Quest 양안 실행을 각각 비교해야 한다.

### 영향 범위

- 거울 표시, Game View 시작 시간, Quest Link/OpenXR 세션 안정성, 양안 렌더링 및 주변 시야에 영향을 줄 수 있다.

### 완료한 검증과 남은 검증

- 풀장착 모델의 Play 전용 레이어와 거울 `reflectedLayers`, RenderTexture 참조를 정적으로 확인했다.
- C# 빌드는 오류 0개였지만 Unity batchmode는 유효한 Unity Editor 라이선스를 찾지 못해 씬 로드와 Play Mode를 실행하지 못했다.
- 따라서 거울이 현재 실제로 바닥에 서 있는지, 검은 화면이 해결되었는지, 픽셀화·주변 시야 깨짐·로비 이탈이 재현되는지는 아직 미검증이다.
- 다음 검증에서는 Unity Profiler의 CPU/GPU/메모리와 RenderTexture 크기·업데이트 주기, OpenXR 로그를 함께 기록해야 한다.

## 관련 기존 기록

- [Meta Quest Link 오디오만 출력되고 영상이 없는 문제](../../Assets/Docs/Bug/2026-07-05_Meta-Quest-Link-Audio-Only-No-Video.md)
- [PPE Room 높이·조명 및 XR 런타임 문제](../../Assets/Docs/Bug/2026-07-03_PPE_Room_Height_Lighting.md)
- [HandTest Teleport·Ray·Invalid AABB 문제](2026-08-01_PPE_HandTest_Teleport_Ray_Input_Mediation.md)
- [Hazmat Toggle Grip·검사 패널 활성화 재진입 문제](2026-08-04_PPE_Hazmat_Toggle_Grip_Inspection_Panel.md)
## 2026-08-17 추가 재현 사례: PPE 프록시 참조 오류

### 증상

`3_PPE_Room_Train_Test_mask.unity`에서 Play Mode 진입 직후 검은 화면과 콘솔 오류가 발생했다.

### 원인

`PPE_A_Mask_Check`에 패널·버튼 참조가 비어 있는 `PPEActionPanelController`와 Marker Collider가 없는 `PPEMarkerToggleGrab`이 남아 있었다. 검사 프록시를 별도 시각물로 만들면서 기존 상호작용 컴포넌트를 제거하지 않은 것이 원인이었다.

### 조치 및 상태

실제 검사 버튼을 가진 `PPE_A_Mask`의 패널에 애니메이션을 연결하고, `PPE_A_Mask_Check`의 패널·Grab·Marker 컴포넌트를 비활성화했다. 오류 재현 후 조치까지는 완료했으나 Quest/OpenXR 실기 검증은 후속 작업이다.

## 2026-08-26 추가 재현 사례: 직접 PPE 씬 실행과 Meta 등록 타임아웃

### 증상

- `Assets/Scenes/3_PPE_Room_3mode_loco.unity`를 직접 Play한 뒤 약 180초에 HMD 화면이 검게 변하고 모래시계가 표시되었다.
- Unity Console의 `Error Pause`가 활성화된 상태에서 Play Mode가 일시정지되었다.

### 근본 원인

- `MetaPlatformIdentityProbe`는 정상 앱 진입점인 `0_App.unity`에만 작성되어 있다.
- 개발용 `TycheLocalTrainingRegistrationClient`는 `BeforeSceneLoad`에서 모든 직접 씬 실행에 자동 설치되었다.
- PPE 씬 직접 실행에서는 identity probe가 존재하지 않아 앱 범위 Meta ID를 얻을 수 없는데도 등록 클라이언트가 180초간 대기한 뒤 `Debug.LogError`를 발생시켰다.
- 같은 프레임에 Console의 `Error Pause`가 Play Mode를 멈춰 OpenXR 프레임 제출이 끊겼다. 당시 OpenXR는 그 전까지 `FOCUSED` 상태였고 GPU device lost, XR session lost, 메모리 부족 오류는 없었다.

### 적용한 변경

- `TycheLocalTrainingRegistrationClient` 설치 시점을 `AfterSceneLoad`로 옮겼다.
- 초기 씬에 활성 `MetaPlatformIdentityProbe`가 있을 때만 개발용 등록 클라이언트를 설치한다.
- PPE 씬 직접 실행에서는 등록 클라이언트를 자동 생성하거나 identity probe를 자동 수리하지 않고 등록 기능만 명시적으로 건너뛴다.
- `PPETrainingDataContractHarness`에 설치 시점과 identity probe 게이트를 검사하는 정적 계약을 추가했다.
- 같은 하네스의 니트릴 장갑 좌·우 기대 순서를 기존 `PPEVoiceFlowDirector` 기본값과 씬 직렬화 값에 맞췄다. 정상 씬 배열은 변경하지 않았다.

### 영향 범위

- 개발용 Meta 사용자 등록 및 로컬 서버 왕복 검증에만 영향을 준다.
- 정상 빌드 흐름의 `0_App`에서 시작하면 기존 identity probe와 등록 클라이언트가 계속 동작한다.
- PPE UI, 음성 흐름, 텔레포트, Grab, 거울 및 XR 렌더링 상태는 변경하지 않는다.

### 검증 구분

- 정적 확인: 직접 PPE 씬에는 `MetaPlatformIdentityProbe` 직렬화 참조가 없고 `0_App`에만 있는 것을 확인했다.
- Unity Editor 확인: 스크립트 재컴파일이 오류 없이 완료되었고 `PPETrainingDataContractHarness.Validate()`가 PASS했다.
- Quest/OpenXR 확인: 직접 PPE 씬을 180초 이상 실행해 일시정지와 검은 화면이 재발하지 않는지 수동 확인이 필요하다.

## 2026-08-27 후속: HMD 첫 프레임 전 타이틀·음원 선행 방지

### 근본 원인

- `0_App`의 `AudioManager`가 `m_PlayBgmOnStart=true`, 지연 0초로 타이틀 BGM을 즉시 재생했다.
- `AppSceneBootstrap`은 일반 렌더 프레임 하나만 기다린 뒤 `1_Title`을 활성화했으며, 물리 HMD 입력 장치,
  실행 중인 `XRDisplaySubsystem`, 실제 XR 렌더 콜백을 확인하지 않았다.
- 따라서 Quest Link가 아직 HMD에 앱 첫 프레임을 제출하지 못한 동안에도 PC Game View와 음원은 먼저 진행할 수 있었다.

### 적용한 변경

- `1_Title`을 백그라운드에서 90%까지 미리 로드하되 HMD 준비 전에는 씬 활성화를 막는다.
- 유효한 Head XR 장치와 실행 중인 `XRDisplaySubsystem`이 있는 상태에서 서로 다른 XR 렌더 프레임 콜백을
  2회 확인한 뒤에만 타이틀 씬을 활성화한다.
- 15초 동안 준비되지 않으면 한 번의 명확한 오류를 기록하며, 타이틀·음원을 먼저 진행하는 fallback은 사용하지 않는다.
- `0_App`의 BGM 자동 재생을 끄고, 게이트를 통과해 활성화된 `1_Title`의 `TitleSplashController`가
  `title` BGM 시작을 소유하도록 변경했다.
- `Tools > XR > Validate App Startup Synchronization` 하네스를 추가해 빌드 씬 순서, XR 준비 게이트,
  App 씬 BGM 지연, Title 씬 BGM 소유권을 함께 검사한다.

### 영향 범위

- `0_App → 1_Title` 최초 전환과 타이틀 BGM 시작 시점만 변경한다.
- Intro 이후 씬 전환, PPE 음성·SFX, 텔레포트, Grab, 거울 및 렌더 설정은 변경하지 않는다.

### 검증

- 정적 확인: `0_App`의 BGM 자동 재생 비활성화와 두 씬의 직렬화값을 확인했다.
- Unity Editor 확인: 스크립트 컴파일 성공 및 `AppStartupSynchronizationHarness.Validate()` PASS.
- Quest/OpenXR 확인: 아직 필요하다. Link 콜드 스타트에서 HMD 타이틀·Game View·BGM 시작 순서를 함께 기록하고,
  Android APK 콜드 스타트에서도 같은 순서를 별도로 확인해야 한다.
- Link의 모래시계 UI는 PC Link compositor 경로이므로 APK에서 동일 UI가 표시된다고 확정하지 않는다. 다만 APK에서도
  첫 프레임 지연·검정 화면이 없는지는 실제 빌드로 확인해야 한다.

## 2026-08-28 후속: Meta SDK 없는 Editor·특정 씬 Play Mode

### 변경 전 필수 판단

1. 이번 변경이 대응하는 요청은 Unity Game View/Editor Play Mode에서 Meta App ID·SDK 계정 설정이 없어도
   `0_App` 또는 현재 연 특정 PPE 씬을 실행할 수 있게 하는 것이다. Quest 빌드의 Meta 인증, 테스트 계정으로
   얻은 앱 범위 사용자 ID, PPE 흐름·입력·UI·오디오·텔레포트는 보존한다.
2. 기존 Inspector/씬 작성값은 보존한다. Editor의 Meta SDK 사용 여부는 `MetaPlatformIdentityProbe`, 물리 HMD
   준비 대기 여부는 `AppSceneBootstrap`의 새 직렬화 옵션이 각각 소유하며 런타임에서 씬 값을 덮어쓰지 않는다.
3. 계정 경로는 `0_App의 MetaPlatformIdentityProbe → Meta Platform SDK 초기화 → entitlement →
   Users.GetLoggedInUser → TycheLocalTrainingRegistrationClient → /api/training-registrations`다. 직접 PPE 씬은
   Probe와 등록 클라이언트 없이 `PPETrainingTelemetryCapture`의 로컬 익명 세션만 기록한다.
4. SDKless Editor 실행은 가짜 Meta ID를 생성하거나 Meta 인증 성공으로 표시하지 않는다. 상태를 명시적으로
   구분하고 로컬 텔레메트리의 `metaAppScopedUserId`는 비워 둔다. 누락 참조나 서버 응답은 자동 수리하지 않는다.
5. 영향 소비자는 Meta Welcome 판정, 개발용 로컬 등록, App 씬의 HMD 준비 게이트와 텔레메트리의 Meta 진단
   필드다. UI·XR 입력·Collider·PPE Grab·거울·Quest 양안 렌더링은 변경하지 않는다.
6. 변경 전 기준은 `0_App` Editor 실행에서 Meta SDK 오류가 발생하고 물리 HMD가 없으면 Title 활성화가 계속
   대기하는 상태다. 변경 후에는 SDKless App 씬과 직접 PPE 씬 Play Mode, 명시적 SDK Editor 테스트와 Quest
   기본 SDK 사용 조건을 각각 검증한다.
7. 정적 C# 빌드와 Editor 하네스, Game View Play Mode까지 실제로 확인한다. Meta 테스트 계정의 실제 앱 범위
   ID, 서버 저장, Quest/OpenXR 양안은 계정과 HMD가 준비된 환경의 별도 수동 검증으로 남긴다.

### 근본 원인

- `MetaPlatformIdentityProbe`가 Editor에서도 Meta Platform SDK 초기화와 entitlement를 무조건 요청해 App ID 또는
  로그인된 Meta 계정이 없는 개발 환경에서 오류가 발생했다.
- `AppSceneBootstrap`의 물리 HMD 준비 게이트가 빌드와 Editor Game View에 동일하게 적용되어, HMD 없이
  `0_App`에서 시작하면 다음 씬 활성화가 계속 대기했다.
- 개발용 등록 클라이언트의 Meta ID·세션 대기 만료가 `Debug.LogError`여서 Console `Error Pause`가 켜진
  테스트 환경을 다시 멈출 수 있었다.

### 적용한 변경

- `MetaPlatformIdentityProbe`에 Inspector 직렬화 옵션 `useMetaPlatformSdkInEditor`를 추가하고 `0_App` 기본값을
  껐다. 이 상태의 Editor에서는 SDK 콜백을 시작하지 않고 `SkippedForEditorTesting` 상태로 진행한다.
- SDKless 상태에서는 `TycheLocalTrainingRegistrationClient`를 생성하지 않는다. Meta 식별 경로가 중간에
  실패하거나 제한 시간이 끝나도 개발용 서버 등록만 경고와 함께 건너뛰며 로컬 텔레메트리는 유지한다.
- `AppSceneBootstrap`에 `waitForPhysicalXrDisplayInEditor`를 추가하고 `0_App` 기본값을 껐다. Editor Game View는
  HMD를 기다리지 않지만 Editor가 아닌 Quest/Standalone 빌드는 기존 물리 XR 준비 게이트를 계속 사용한다.
- 실제 Meta 계정을 Editor에서 검증할 때는 `0_App`의 `MetaPlatformIdentityProbe > Use Meta Platform SDK In Editor`를
  명시적으로 켠다. Editor 외 빌드는 이 옵션 값과 관계없이 Meta SDK 경로를 사용한다.
- 가짜 Meta ID, 자동 테스트 계정, 직접 PPE 씬용 identity probe는 만들지 않았다.
- `AppStartupSynchronizationHarness`와 `PPETrainingDataContractHarness`에 두 실행 모드의 정적 계약을 추가했다.

### 영향 범위

- SDKless Editor의 앱 시작, 직접 PPE 씬 시작, 개발용 Meta 사용자 등록과 로컬 텔레메트리 식별 필드에만 영향을 준다.
- SDKless 로컬 기록은 `metaAppScopedUserId`가 없는 익명 세션이다. 테스트 계정으로 SDK 인증에 성공한 경우에는
  기존 앱 범위 Meta 사용자 ID를 사용해 서버 등록 경로로 진행한다.
- PPE UI, 음성, 텔레포트, Grab, Collider, 거울, 입력 소비자와 Quest 양안 렌더 설정은 변경하지 않았다.

### 완료한 검증

- 정적 확인: `Assembly-CSharp-Editor.csproj --no-restore` 빌드 오류 0개.
- Unity Editor 확인: `AppStartupSynchronizationHarness.Validate()`와
  `PPETrainingDataContractHarness.Validate()` 모두 PASS.
- `0_App` Game View Play Mode: 일시정지 없이 앱 흐름이 PPE 씬까지 진행했고,
  Meta probe 1개가 `SkippedForEditorTesting`, 등록 클라이언트 0개, 텔레메트리 캡처 1개였다.
- `3_PPE_Room_3mode_loco` 직접 Play Mode: Meta probe 0개, 등록 클라이언트 0개,
  텔레메트리 캡처 1개였으며 Play Mode가 일시정지되지 않았다.
- 두 Play Mode 실행 직후 Unity Console Error 조회 결과는 각각 0건이었다.

### 아직 필요한 수동 검증

- Meta 테스트 계정으로 `Use Meta Platform SDK In Editor`를 켠 뒤 실제 앱 범위 사용자 ID 획득과
  `/api/training-registrations` 서버 저장을 확인해야 한다.
- Quest/OpenXR 기기에서 entitlement, 계정 식별, 양안 렌더링과 빌드의 물리 HMD 준비 게이트를 확인해야 한다.
