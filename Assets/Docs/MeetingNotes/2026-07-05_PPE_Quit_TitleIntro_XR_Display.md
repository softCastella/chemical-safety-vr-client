# 2026-07-05 PPE 종료 버튼 및 타이틀·인트로 XR 화면 수정 회의록

- 날짜: 2026-07-05
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 대상 씬: `1_TitleScene`, `2_Intro`, `3_PPE_Room`

## 작업 내용

### PPE Room 종료 버튼

- `3_PPE_Room` UI에 `종료하기` 버튼을 추가했다.
- 버튼에는 `QuitApplicationButton` 컴포넌트를 연결했다.
- 컴포넌트는 `Awake`에서 같은 오브젝트의 `Button.onClick`에 종료 처리를 등록하고, `OnDestroy`에서 자신이 등록한 리스너만 해제한다.
- Unity Editor에서 버튼을 누르면 `EditorApplication.isPlaying = false`를 실행해 Play Mode를 종료한다.
- Editor 이외의 빌드에서는 `Application.Quit()`을 실행해 애플리케이션 종료를 요청한다.
- 종료 동작은 버튼의 직렬화된 `On Click()` 목록을 덮어쓰지 않으며, 버튼 위치·크기·색상·문구는 씬 직렬화값을 사용한다.

### 타이틀·인트로 XR 화면

- 타이틀 씬과 인트로 씬에서 UI가 좌우 눈 화면의 상단에 두 개로 보이던 현상을 수정했다.
- 두 씬의 UI Canvas는 XR 카메라를 사용하는 `Screen Space - Camera` 구성으로 확인했다.
- 인트로 씬의 Canvas는 `XR Origin (VR)` 자식에서 씬 루트로 분리해 HMD 이동이 UI Transform에 중복 반영되지 않도록 했다.
- 인트로 Canvas에 `IntroSceneTransition`을 직접 연결하고, 표시 3초·페이드 아웃 0.8초 후 `3_PPE_Room`으로 전환하도록 직렬화했다.
- 타이틀 씬의 파트너 로고 기준점은 중앙 앵커·피벗으로 정리해 XR 화면에서 레이아웃 기준이 불안정하게 밀리는 문제를 줄였다.

## 검토 결과

- `QuitApplicationButton`은 Editor 전용 API가 `#if UNITY_EDITOR`로 분리되어 플레이어 빌드 컴파일 경로에 포함되지 않는다.
- `RequireComponent(typeof(Button))`으로 필수 버튼 컴포넌트가 보장된다.
- PPE 씬의 `Quit Button`에는 `Button`, `Image`, `QuitApplicationButton` 및 `종료하기` TMP 라벨 참조가 정상 직렬화되어 있다.
- 스크립트 `.meta` GUID와 씬의 MonoBehaviour GUID가 일치한다.
- `3_PPE_Room`은 Build Settings에 활성 씬으로 포함되어 있다.
- 코드 및 YAML 정적 검토에서 종료 기능을 막는 참조 누락은 발견되지 않았다.

## 검증 범위 및 후속 확인

- [ ] Unity Play Mode에서 `종료하기` 선택 시 즉시 Play Mode가 종료되는지 확인
- [ ] PC Standalone 빌드에서 버튼 선택 시 프로세스가 정상 종료되는지 확인
- [ ] Quest/Android 빌드에서 버튼 선택 시 앱이 종료되고 Quest 홈으로 복귀하는지 확인
- [ ] HMD 양쪽 눈에서 타이틀과 인트로 UI가 각각 한 화면으로 정렬되는지 실기기 확인
- [ ] 타이틀 → 인트로 → PPE Room 전환 후 UI 중복 표시가 재발하지 않는지 확인

## 관련 파일

- `Assets/Scripts/QuitApplicationButton.cs`
- `Assets/Scenes/1_TitleScene.unity`
- `Assets/Scenes/2_Intro.unity`
- `Assets/Scenes/3_PPE_Room.unity`
- `ProjectSettings/EditorBuildSettings.asset`

