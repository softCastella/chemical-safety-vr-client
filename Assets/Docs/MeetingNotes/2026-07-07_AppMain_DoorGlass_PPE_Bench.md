# 2026-07-07 App 시작 구조·도어 유리·PPE 벤치 색상 수정 회의록

- 날짜: 2026-07-07
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 대상 씬: `0_App`, `3_PPE_Room`

## 작업 요약

1. 앱 시작 씬에 `AppMain` 오브젝트를 추가하고 전역 스크립트를 이쪽으로 모았다.
2. `DoorWindowGlass`의 `OnValidate` 경고를 제거했다.
3. PPE Room 오른쪽 `ppe_room_bench` 색상이 너무 어둡게 보이는 문제를 수정했다.

---

## 1. App 씬 — `AppMain` 오브젝트 구성

### 배경

- 애플리케이션 최초 실행 씬은 `Assets/Scenes/0_App.unity`이다.
- 기존에는 `AppSceneBootstrap`이 `Main Camera`에 붙어 있었다.
- 앱 시작 시 필요한 전역 컴포넌트를 한 곳에서 관리하기 위해 `AppMain` 루트 오브젝트를 추가했다.

### 변경 내용

**`AppMain` 오브젝트 (신규)**

| 컴포넌트 | 역할 |
|---|---|
| `AppSceneBootstrap` | 1프레임 대기 후 `1_TitleScene` 비동기 로드 |
| `AudioManager` | 씬 전환 후에도 유지되는 전역 오디오 매니저 |

**`Main Camera`**

- `AppSceneBootstrap` 제거
- `Camera`, `AudioListener`, URP 카메라 컴포넌트만 유지

### 씬 계층 구조

```
0_App
├── AppMain          ← AppSceneBootstrap, AudioManager
├── Main Camera      ← Camera, AudioListener, URP
└── Directional Light
```

### 동작 참고

- `AudioManager.Awake()`에서 `DontDestroyOnLoad`가 호출되므로 `AppMain` 오브젝트는 타이틀 씬 이후에도 유지된다.
- `AppSceneBootstrap`의 `Start()` 코루틴은 최초 1회만 실행된다.

### 관련 파일

- `Assets/Scenes/0_App.unity`
- `Assets/Scripts/AppSceneBootstrap.cs`
- `Assets/Scripts/AudioManager.cs`

---

## 2. `DoorWindowGlass` — `OnValidate` 경고 수정

> 2026-07-28 후속 변경: `OnEnable`/`OnValidate` 메시 재생성은 완전히 제거되었고, 명시적인 `Rebuild Glass Mesh` 컨텍스트 메뉴 방식으로 변경되었다. 현재 문·앞벽 개구부 구조와 치수는 `2026-07-28_PPE_Door_Glass_Wall_Opening.md`를 기준으로 한다.

### 증상

```
Destroying Mesh assets immediately is not permitted during physics trigger/contact,
animation event callbacks, rendering callbacks or OnValidate.
You must use Destroy instead.
```

- 발생 위치: `DoorWindowGlass.RebuildMesh()` → `OnValidate()`
- 원인: Inspector 값 변경 시 `OnValidate`가 호출되고, 그 안에서 새 Mesh를 만들고 `DestroyImmediate`로 이전 Mesh를 삭제했다.

### 수정 내용

1. **Mesh 재사용** — 프로시저럴 Mesh(`PPE Door Octagonal Glass`)가 이미 있으면 새로 만들지 않고 `Clear()` 후 정점·삼각형만 갱신한다.
2. **삭제 시점 분리** — Mesh 정리는 `OnDestroy`에서만 수행한다. `OnValidate` 경로에서는 `DestroyImmediate`를 호출하지 않는다.

### 관련 파일

- `Assets/Scripts/DoorWindowGlass.cs`

---

## 3. PPE Room — `ppe_room_bench` 색상 조정

### 증상

- `3_PPE_Room` 오른쪽 벤치(`ppe_room_bench`, 3개 인스턴스)가 왼쪽 벤치(`mask_locker` 소속)보다 훨씬 어둡게 보였다.
- 중간 톤이 없이 검정에 가깝게 눌려 보였다.

### 원인 분석

| 항목 | 왼쪽 벤치 (`mask_locker`) | 오른쪽 벤치 (`ppe_room_bench`) |
|---|---|---|
| 셰이더 | Simple Lit (`650dd952…`) | 처음에는 동일, 이후 URP Lit으로 변경됨 |
| `_BaseColor` 틴트 | `(1.35, 1.35, 1.35)` | `(1.35, 1.35, 1.35)` → 이후 여러 차례 조정 |
| 베이스 텍스처 | 밝은 영역과 어두운 영역이 혼재 | 전체적으로 매우 어두운 톤만 존재 |

- `ppe_room_bench_basecolor.jpg` 텍스처 자체가 어두운 값 위주라, 왼쪽과 같은 `1.35` 틴트만으로는 충분히 밝아지지 않았다.
- 중간 시도에서 **URP Lit**으로 바꾸면서 그림자·조명 영향이 커져 오히려 더 까맣게 보이는 회귀가 발생했다.

### 최종 수정

1. **셰이더 복구** — 왼쪽 벤치와 동일한 **Simple Lit**으로 되돌렸다.
2. **벤치 머티리얼 틴트** — 어두운 텍스처를 보정하고 왼쪽보다 약간 밝게 보이도록 설정했다.
   - `Assets/FBX/ppe_room_bench/tripo_node_2a0ef0a3-fe60-47cf-b0c0-98212291dfaf_material.mat`
   - `_BaseColor`: `(2.0, 1.96, 1.92)`
3. **`PPEBackgroundRoom`에 `benchColor` 추가** — 덕트 색(`ductColor`)과 같은 방식으로 Inspector에서 조정 가능하게 했다.
   - `ApplyBenchColor()`가 이름이 `ppe_room_bench`로 시작하는 오브젝트의 렌더러에 `MaterialPropertyBlock`으로 색을 적용한다.
   - `roomBrightness`와 곱해져 최종 색이 결정된다.

### Inspector 조정 방법

`3_PPE_Room` 씬에서 **PPE Background Room** 오브젝트 선택:

| 필드 | 설명 | 현재 기본값 |
|---|---|---|
| **Bench Color** | 벤치 틴트 색 (HDR 가능) | `(2.0, 1.96, 1.92)` |
| **Room Brightness** | 벤치·벽·바닥 등 밝기 배율 | `1.05` |

- 더 밝게: `Bench Color` R/G/B를 소폭 올린다. 예: `(2.1, 2.05, 2.0)`
- 더 어둡게: 값을 내린다. 예: `(1.8, 1.75, 1.7)`

### 관련 파일

- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/FBX/ppe_room_bench/tripo_node_2a0ef0a3-fe60-47cf-b0c0-98212291dfaf_material.mat`
- `Assets/FBX/ppe_room_bench/ppe_room_bench_basecolor.jpg`
- `Assets/FBX/mask_locker/tripo_node_38071929-f8b8-4da5-b2dc-6c3f8a58e779_material.mat` (왼쪽 벤치 참조)

---

## 검증 체크리스트

### App 씬

- [ ] `0_App` Hierarchy에 `AppMain` 오브젝트가 보이는지 확인
- [ ] Play Mode에서 `1_TitleScene`으로 정상 전환되는지 확인
- [ ] 타이틀 이후 씬에서도 `AudioManager`가 유지되는지 확인

### DoorWindowGlass

- [ ] `3_PPE_Room`에서 `Door Window Glass` Inspector 값 변경 시 `OnValidate` 경고가 재발하지 않는지 확인
- [ ] Play Mode 전후로 유리 Mesh 형태가 정상인지 확인

### PPE 벤치 색상

- [ ] 오른쪽 `ppe_room_bench` 3개가 왼쪽 벤치보다 약간 밝은 회색 톤으로 보이는지 확인
- [ ] HMD/Quest에서도 과도하게 검게 눌리지 않는지 확인
- [ ] `Bench Color` Inspector 조정이 실시간으로 반영되는지 확인

---

## 후속 검토 사항

- 벤치 텍스처 자체의 다이나믹 레인지가 낮아 틴트만으로 한계가 있을 수 있다. 필요 시 `ppe_room_bench_basecolor.jpg`를 밝기·대비 보정한 버전으로 교체하는 것도 고려할 수 있다.
- `AppMain`에 추가 전역 매니저가 생기면 같은 오브젝트에 붙이되, `DontDestroyOnLoad` 대상 여부를 씬 전환 요구사항에 맞게 검토한다.
