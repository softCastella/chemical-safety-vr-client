# 2026-07-04 오디오·타이틀·PPE Room 시나리오 UI 작업 회의록

- 날짜: 2026-07-04
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 주요 씬: `1_TitleScene`, `2_Intro`, `3_PPE_Room`, `4_InsideMixer`

## 작업 목표

- 씬별 BGM과 타이틀 전환 연출을 정리한다.
- PPE Room의 크기, 밝기, 문과 유리 편집 기능을 개선한다.
- XR World Space Canvas에 시나리오 선택 카드 UI를 구성한다.
- 편집한 Transform, 색상, TMP 값이 플레이 진입 후 초기화되는 문제를 점검한다.

## 오디오 및 타이틀

- `AudioManager`를 싱글톤으로 구성하고 씬별 설정에서 음원을 지정하도록 구성했다.
- 타이틀용 BGM과 PPE Room용 BGM을 각각 연결했다.
- 타이틀 진입 시 BGM 페이드 인, 인트로 진입 전 페이드 아웃을 적용했다.
- 타이틀 연출 순서를 타이틀 로고 → `Application.version` → 파트너 로고로 구성했다.
- 버전 TMP는 우측 상단에 배치하고 타이틀 페이드 시퀀스에 포함했다.
- VR 화면이 준비되기 전에 BGM이 재생되거나 즉시 중단되는 타이밍 문제를 조정했다.

## PPE Room

- 방 높이를 최초 2배 요청에서 최종 1.5배로 조정했다.
- 방 밝기를 Inspector에서 조절할 수 있도록 구성했다.
- 문 유리의 가로·세로 크기를 Inspector에서 변경할 수 있도록 했다.
- 문 색상과 Transform/Scale 수정값 보존을 위해 문 재질 처리 경로를 분리했다.
- 문 재질은 플레이 진입 시 폐기되지 않도록 영구 `.mat` 에셋으로 전환하는 처리를 추가했다.
- 시나리오 HUD 전체를 카드 높이 절반만큼 아래로 이동한 뒤, 최종적으로 다시 절반만큼 위로 보정했다.

## 시나리오 선택 HUD

- XR World Space Canvas 아래에 시나리오 선택 HUD를 구성했다.
- 머리 추적 Canvas가 아닌 월드 고정 UI로 배치해 손 직접 상호작용을 전제로 했다.
- Sci-Fi 카드 두 종류의 색상과 디자인을 비교할 수 있도록 그룹화했다.
- 각 디자인 그룹은 카드 3장을 한 줄로 배치하며, 그룹을 같은 위치에 두고 활성/비활성으로 비교하도록 구성했다.
- `LeftColor_Group`은 기본 활성, `RightColor_Group`은 기본 비활성으로 설정했다.
- `Scenario Card 3` 디자인을 사용한 `ScenarioCard3_Group`을 추가했다.
- `ScenarioCard3_Group`의 카드 크기를 라이트 컬러 그룹과 유사한 `360 × 490`으로 맞췄다.
- 사용자가 만든 `ScenarioCard3_Group (1)` 복제본의 투명도 실험을 진행했다.

## 편집값 초기화 정책

- HUD 자동 생성, 자동 배치, TMP 자동 갱신 경로를 제거하거나 일회성 에디터 생성으로 분리했다.
- 카드 그룹 생성 도구는 그룹 생성 후 기존 카드의 위치·크기·텍스트를 다시 쓰지 않도록 구성했다.
- 방과 믹서가 임시 `DontSave` 재질에 의존한다는 점을 확인했다.
- 모든 자동 복구를 제거했을 때 PPE Room과 InsideMixer가 핑크색이 되는 회귀가 발생해 재질 복구 코드를 롤백했다.
- XR 추적, 페이드, 문 열기처럼 기능상 필요한 런타임 값 변경과 에디터 시각 설정 덮어쓰기를 구분하기로 했다.

## 구현 파일

- `Assets/Scripts/AudioManager.cs`
- `Assets/Scripts/AudioManagerSettings.cs`
- `Assets/Scripts/SceneAudioSettings.cs`
- `Assets/Scripts/TitleSplashController.cs`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/Scripts/DoorWindowGlass.cs`
- `Assets/Scripts/ScenarioSelectionHud.cs`
- `Assets/Scripts/SciFiCardVisual.cs`
- `Assets/Scripts/SciFiCardDepthResponse.cs`
- `Assets/Scripts/ChemicalMixerInterior.cs`
- `Assets/Editor/ScenarioCardGroupBuilder.cs`
- `Assets/Scenes/3_PPE_Room.unity`

## 검증 및 후속 작업

- C# 및 Editor Assembly 빌드에서 오류 0개를 확인했다.
- [ ] Quest 실기기에서 타이틀 로고 떨림과 글자 번짐 재확인
- [ ] 타이틀 → 인트로 → PPE Room BGM 전환과 페이드 길이 확인
- [ ] 문 색상과 크기가 플레이 전후 유지되는지 씬 저장 후 재확인
- [ ] 카드 그룹 활성/비활성 비교 및 손 직접 상호작용 확인
- [ ] `ScenarioCard3_Group (1)`의 실제 반투명 표현 개선

## 시나리오 상세 모달 및 Quest 입력 추가

- `imgForCodex/시나리오상세 모달.png`를 기준으로 시나리오 카드 선택 후 표시되는 상세 설명 모달을 구성했다.
- 모달은 시나리오 번호, 제목, 본문, `훈련 선택`, `돌아가기` 버튼으로 구성하며 우측 상단 X 버튼은 제외했다.
- `돌아가기` 버튼을 누르면 모달 루트가 비활성화되어 기존 카드 선택 화면으로 복귀한다.
- 카드별 텍스트, 버튼 참조, 훈련 선택 이벤트는 `ScenarioDetailModal` 인스펙터와 씬 직렬화값을 최종 기준으로 사용한다.
- 검정 오버레이는 중앙 패널과 분리하고 RectTransform을 `4000 × 3000`으로 저장해 XR 시야 전체를 덮도록 했다.
- 오버레이는 Raycast Target을 유지해 모달 표시 중 뒤쪽 카드가 선택되지 않도록 한다.
- 기존 `XRUIInputModule`과 `TrackedDeviceGraphicRaycaster`를 유지하고, XR Origin의 `Camera Offset` 아래에 좌우 `NearFarInteractor`를 연결했다.
- `XRInteractionManager`, `InputActionManager`, `XRI Default Input Actions`를 씬에 직렬화해 Meta Quest 컨트롤러 포즈와 트리거 입력을 사용한다.
- `ScenarioSelectionHud`의 `RemoveAllListeners()` 호출을 제거하고 각 컴포넌트가 추가한 런타임 리스너만 추적해 해제하도록 변경했다.
- `ScenarioCardGroupBuilder`의 자동 실행 경로를 제거해 씬 열기, 스크립트 리로드, 플레이 모드 전환 과정에서 인스펙터 값을 덮어쓰지 않게 했다.

### 관련 파일

- `Assets/Scripts/ScenarioDetailModal.cs`
- `Assets/Scripts/ScenarioSelectionHud.cs`
- `Assets/Editor/ScenarioDetailModalSceneBuilder.cs`
- `Assets/Editor/ScenarioCardGroupBuilder.cs`
- `Assets/Scenes/3_PPE_Room.unity`

### 검증 및 후속 작업

- Runtime 및 Editor Assembly 빌드 오류 0개를 확인했다.
- 씬에 오버레이 크기, 좌우 컨트롤러 인터랙터, 입력 액션 참조가 직렬화된 것을 확인했다.
- [ ] Meta Quest 실기기에서 오버레이 전체 시야 커버 확인
- [ ] 좌우 컨트롤러 레이와 트리거로 카드 및 모달 버튼 선택 확인
- [ ] `돌아가기` 동작과 모달 뒤 입력 차단 확인
