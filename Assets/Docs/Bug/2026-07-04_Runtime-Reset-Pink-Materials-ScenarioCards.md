# 버그 리포트: 플레이 모드 초기화·핑크 재질·시나리오 카드 투명도

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-04 |
| 대상 | `3_PPE_Room`, `4_InsideMixer`, 타이틀 및 시나리오 HUD |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR |
| 상태 | 일부 수정, Quest 및 Unity Editor 재검증 필요 |

## BUG-001: 플레이 모드 진입 시 시각 설정이 이전 값으로 돌아감

### 증상

- 문 색상과 크기, HUD 위치, 카드 크기, TMP 내용이 플레이 모드 진입 후 이전 값으로 돌아갔다.
- 동일한 수정 요청을 반복해야 하는 문제가 발생했다.

### 원인

- `ExecuteAlways`, `OnValidate`, `Awake`, `OnEnable`에서 생성 스크립트가 Transform, 재질, 색상, TMP 값을 다시 적용했다.
- 생성용 설정값과 씬에서 직접 편집한 값이 동시에 존재해 생성용 설정값이 우선했다.

### 조치

- `ScenarioSelectionHud`의 HUD 재생성·배치·텍스트 갱신 코드를 제거했다.
- `SciFiCardVisual`의 플레이 진입 자동 적용 경로를 제거했다.
- 카드 그룹은 일회성 Editor 도구로 생성하고, 생성된 기존 객체는 다시 갱신하지 않도록 했다.
- 문은 기존 재질과 Transform이 유효하면 다시 지정하지 않도록 분기했다.

## BUG-002: 자동 복구 코드 제거 후 PPE Room 전체가 핑크색이 됨

### 증상

- 벽이 사라져 보이고 문, 천장, 바닥이 핑크색으로 표시됐다.
- 플레이 모드 종료 후에도 핑크색이 재발했다.

### 원인

- PPE Room Renderer의 씬 재질 참조가 `{fileID: 0}`인 상태였다.
- 방 생성 코드가 `HideFlags.DontSave` 임시 재질을 만들고 `OnEnable`에서 재할당하는 구조였다.
- 자동 재질 복구를 제거하면서 Renderer에 유효한 URP 재질이 남지 않았다.

### 조치

- PPE Room의 자동 재질 생성 및 복구 경로를 삭제 전 상태로 롤백했다.
- 문은 기존 정상 재질을 유지하고, 재질이 없거나 오류 셰이더일 때만 복구하도록 분리했다.
- 문 재질의 플레이 모드 폐기를 막기 위해 영구 재질 에셋 생성 처리를 추가했다.

## BUG-003: `4_InsideMixer`가 핑크색으로 표시됨

### 증상

- 믹서 내부 셸, 축, 블레이드 등 생성 오브젝트가 핑크색으로 표시됐다.

### 원인

- `ChemicalMixerInterior`가 커스텀 셰이더 기반 재질을 `HideAndDontSave`로 생성했다.
- 자동 재생성을 제거하면서 플레이 전후 재질 복구가 중단됐다.

### 조치

- `ChemicalMixerInterior`의 `ExecuteAlways`, `OnEnable` 재빌드, `OnValidate` 갱신을 삭제 전 상태로 롤백했다.
- 커스텀 셰이더 `Project/Chemical Mixer Interior` 사용을 유지했다.

## BUG-004: 카드 그룹이 계층창에 생성되지 않음

### 증상

- 카드 그룹 생성 스크립트를 추가했지만 `Scenario Selection HUD (1)` 아래에 그룹이 나타나지 않았다.

### 원인

- 최초 구현이 스크립트 컴파일 직후 한 번만 활성 씬을 검사했다.
- 대상 씬이 비활성이거나 실행 타이밍이 맞지 않으면 생성이 누락됐다.

### 조치

- 스크립트 리로드, 씬 열기, 수동 메뉴 실행 시 생성하도록 보완했다.
- 대상 HUD와 원본 카드를 재귀적으로 검색하도록 변경했다.
- 메뉴 경로를 `Tools > Scenario HUD > Create Overlapping Card Groups`로 제공했다.

## BUG-005: 카드 그룹 배치 의도 오해

### 증상

- 최초에는 카드가 세로로 배치되거나 카드 세 장이 같은 위치에 겹쳐 생성됐다.
- 요구사항은 디자인별 카드 3장 세트를 한 줄로 만들고, 두 세트 그룹을 같은 위치에서 교대로 활성화하는 것이었다.

### 조치

- 각 그룹 내부 카드 위치를 X `-440`, `0`, `440`으로 배치했다.
- `LeftColor_Group`과 `RightColor_Group`은 같은 위치를 사용한다.
- 그룹 단위 활성/비활성 비교 방식으로 수정했다.
- 그룹 Y 위치는 최종 `0`으로 보정했다.

## BUG-006: `ScenarioCard3_Group (1)` 투명도 변경 시 단색처럼 보임

### 증상

- 메인 패널 알파를 낮춰도 뒤 배경이 보이기보다 청색만 흐려진 단색처럼 보였다.

### 확인된 원인

- 카드 루트 패널 알파 외에도 같은 형상을 복제하는 UI `Shadow`가 알파 `0.72`, 오프셋 `(10, -12)`로 겹친다.
- `Glass Tint` 레이어도 알파 `0.18`로 추가 합성된다.
- 메인 패널만 `0.52`로 낮춰도 Shadow와 Glass Tint가 합성되어 실질 불투명도가 높게 유지된다.

### 후속 조치

- [ ] 복제본 그룹에서 Shadow 알파를 낮추거나 비활성화
- [ ] Glass Tint와 메인 패널 알파를 함께 조정
- [ ] 카드 텍스트와 외곽선은 불투명 상태 유지
- [ ] 실제 배경이 비치는지 Game View와 Quest에서 확인

## 검증

- Runtime 및 Editor C# 프로젝트 빌드에서 오류 0개를 확인했다.
- Unity Editor의 계층 생성 결과와 씬 저장 여부는 추가 확인이 필요하다.
- Quest 실기기 렌더링과 손 상호작용은 미검증 상태다.

## BUG-007: 시나리오 상세 모달 오버레이 크기와 Quest 컨트롤러 입력

### 증상

- 시나리오 카드를 선택해도 상세 모달 기능이 없었다.
- 검정 오버레이가 기존 XR UI Canvas 크기 `1200 × 600`에만 맞춰져 Quest 시야 전체를 덮지 못했다.
- 생성 도구가 기존 모달을 보호하도록 설계되어 있어 스크립트 Refresh만으로 오버레이 크기가 변경되지 않았다.
- PPE Room XR Origin에 좌우 컨트롤러 인터랙터가 없어 Quest 컨트롤러로 카드와 모달 버튼을 선택할 수 없었다.

### 원인

- 모달 루트가 Canvas Stretch 앵커와 `SizeDelta (0, 0)`을 사용해 부모 Canvas 범위에 제한됐다.
- 최초 생성 이후에는 인스펙터/씬 직렬화값을 최종 기준으로 사용하기 위해 생성 도구가 기존 오브젝트를 덮어쓰지 않는다.
- 씬에 `XRUIInputModule`과 `TrackedDeviceGraphicRaycaster`는 있었지만 컨트롤러 측 `NearFarInteractor`, `XRInteractionManager`, 입력 액션 활성화 구성이 빠져 있었다.
- 기존 `ScenarioSelectionHud`가 `RemoveAllListeners()`를 사용해 인스펙터에 연결된 이벤트까지 제거할 수 있었다.

### 조치

- `ScenarioDetailModal`을 추가해 카드별 번호, 제목, 설명, 훈련 선택 이벤트를 씬 직렬화값으로 관리한다.
- `돌아가기` 버튼은 `ScenarioDetailModal.Hide()`를 호출해 모달 루트를 비활성화한다.
- 우측 상단 X 버튼은 추가하지 않았다.
- 검정 오버레이 RectTransform을 씬에서 직접 `4000 × 3000`, 중앙 앵커로 저장해 Quest 시야를 덮도록 확장했다.
- 오버레이 Image의 Raycast Target을 유지해 모달 뒤 카드 입력을 차단한다.
- `Camera Offset` 아래에 XRI Starter Assets의 좌우 `NearFarInteractor` 프리팹을 연결했다.
- XR Origin에 `XRInteractionManager`와 `InputActionManager`를 추가하고 `XRI Default Input Actions`를 직렬화했다.
- 런타임은 자신이 추가한 리스너만 추적해 해제하며 인스펙터 이벤트를 삭제하지 않도록 변경했다.
- 카드 그룹 에디터 도구의 씬 열기/스크립트 리로드 자동 실행을 제거해 플레이 전후 인스펙터 값을 덮어쓰지 않도록 했다.

### 검증

- `3_PPE_Room.unity`에 오버레이 `4000 × 3000` 값이 직렬화된 것을 확인했다.
- 좌우 `NearFarInteractor`, `XRInteractionManager`, `XRI Default Input Actions` 참조가 씬에 직렬화된 것을 확인했다.
- Runtime 및 Editor Assembly 빌드 오류 0개를 확인했다.
- [ ] Quest 실기기에서 오버레이의 전체 시야 커버 범위 확인
- [ ] 좌우 컨트롤러 레이, 트리거 선택, 버튼 호버와 햅틱 확인
- [ ] `돌아가기` 선택 시 모달이 닫히고 카드 선택 화면으로 복귀하는지 확인
