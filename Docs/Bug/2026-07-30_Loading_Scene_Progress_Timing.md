# Loading Scene Progress Timing Reversion

Date: 2026-07-30

## Scope

This report covers the scene transition and loading progress behavior between the intro scene and the PPE room.

The intended application flow is:

`0_App -> 1_Title -> 2_Intro -> 6_LoadingScene -> 3_PPE_Room`

## Symptom

The adaptive loading experiment completed too quickly when `3_PPE_Room` reached Unity's scene-ready state almost immediately. Although the first-load estimate was authored as four seconds, the adaptive completion path could begin as soon as `AsyncOperation.progress` reached `0.9` and finish after the shorter minimum-display and completion-animation durations.

This made the percentage and segmented loading bar advance too quickly to be useful as a transition screen.

## Cause

Unity does not provide an exact total loading time before a scene load begins. The adaptive implementation estimated the total duration from elapsed time and `LoadSceneAsync.progress`, then switched to a short completion animation when the operation became ready for activation.

For a locally cached or quickly loaded scene, the ready signal arrived before the estimate could stabilize. Learning a previous duration also did not guarantee a consistent presentation because device state, cache state, and scene initialization time can vary between runs.

## Resolution

The adaptive estimate and PlayerPrefs-based duration learning were removed. The loading bar now uses a deterministic, Inspector-authored fill duration:

- `LoadingSceneController.LoadTarget("3_PPE_Room")` opens `6_LoadingScene` from the intro transition.
- `6_LoadingScene` starts `3_PPE_Room` with `SceneManager.LoadSceneAsync` and keeps `allowSceneActivation` disabled.
- The displayed percentage follows Unity's normalized scene progress but cannot advance faster than `1 / Progress Fill Duration` per second.
- When the PPE room is ready early, the bar still takes the authored fill duration to reach `100%`.
- Scene activation is allowed only after the bar reaches `100%` and the completion hold has elapsed.

## Inspector Configuration

The authoritative timing values are serialized on:

`6_LoadingScene > Canvas > LoadingSceneController > Progress Timing`

- `Minimum Display Duration`: `1` second
- `Progress Fill Duration`: `12` seconds
- `Completion Hold Duration`: `0.15` seconds

`12` is the current scene-authored value and produces a twelve-second minimum fill when the target scene is already ready. Increase `Progress Fill Duration` to make the transition slower. The C# field initializer remains a four-second fallback for a newly added or otherwise unconfigured component; it does not replace the value serialized in `6_LoadingScene`.

The scene currently serializes `Prewarm Frames = 0` under `Scene Loading`. The loading UI is visible on the first frame so the logo rotation can begin immediately. This intentionally replaces the earlier two-frame XR Canvas prewarm; first-frame size and placement therefore require a Quest/OpenXR check.

## Validation

Verify the following after entering Play Mode from `0_App` or `2_Intro`:

- the intro scene fades out and opens `6_LoadingScene` instead of loading `3_PPE_Room` directly;
- the percentage begins at `0%` and uses no leading zero from `0%` through `99%`;
- the segmented gradient bar does not complete faster than the authored `Progress Fill Duration` when the PPE room is ready early;
- the eighteenth and final segment appears in the same frame that the percentage changes to `100%`;
- the percentage reaches `100%` before `3_PPE_Room` activates;
- `Progress Fill Duration` changes made in the Inspector remain authoritative after entering Play Mode.

The current progress source measures Unity scene and included-resource loading. Any future network, database, or application-specific initialization that runs after scene activation must expose its own progress source before it can be represented by this loading bar.

## Status

Code compilation and serialized-reference checks pass. Final timing, final-segment synchronization, and first-frame appearance still require a Quest/OpenXR headset run after Unity finishes script compilation.

## 후속 조치: 현재 작업 씬 경로 복구 (2026-08-04)

### 근본 원인

기존 `6_LoadingScene`의 직렬화된 `nextSceneName`과 `2_Intro`의 전환 대상은 `3_PPE_Room`이었지만, 현재 작업 트리에는 해당 씬 파일이 없고 실험용 복사본 `3_PPE_Room_HandTest_scale_0`이 실제 대상이었다. 로딩 컨트롤러는 대상 씬이 Build Settings에 없으면 비동기 로드를 시작하지 않고 종료하므로 로딩 화면이 진행되지 않았다.

### 적용한 변경

- 기존 로딩 씬의 UI 요소를 기반으로 `6_LoadingScene_0`을 추가했다.
- 새 씬의 대상 씬을 `3_PPE_Room_HandTest_scale_0`으로 직렬화했다.
- `2_Intro`, `6_LoadingScene`, `IntroSceneTransition`, `LoadingSceneController`의 전환 경로를 새 대상과 새 로딩 씬에 연결했다.
- Build Settings에서 실제 존재하는 PPE 씬을 활성 대상에 등록하고 `6_LoadingScene_0`을 추가했다.
- `LoadingSceneBuilder`의 기본 대상과 검증 대상도 새 로딩 씬 구조에 맞췄다.

### 검증 및 잔여 수동 검증

- 씬 파일·메타 GUID·직렬화된 오브젝트 및 스크립트 참조를 확인했다.
- `2_Intro → 6_LoadingScene_0 → 3_PPE_Room_HandTest_scale_0` 경로와 Build Settings 등록을 확인했다.
- Unity 배치 검증은 동일 프로젝트가 이미 Unity 에디터에서 열려 있어 실행하지 못했다.
- Unity 에디터에서 컴파일 완료 후 `Tools > Loading Scene > Validate Progress UI`를 실행하고, 이후 Quest/OpenXR에서 양안 전환과 로딩 진행을 확인해야 한다.

## 후속 조치: `3_PPE_Room_3mode_loco` 전환 (2026-08-24)

### 변경 전 판단

1. 기존 Inspector/씬 작성값을 보존한다. `2_Intro`와 `6_LoadingScene_0`의 `nextSceneName`, Build Settings의 PPE 대상 항목만 변경하며 로딩 UI의 RectTransform, 진행 시간, 색, Canvas와 참조는 변경하지 않는다.
2. 전환 대상의 단일 실행 소유자는 `LoadingSceneController`다. `IntroSceneTransition`은 대상명을 전달하고, 로딩 씬을 직접 열었을 때만 직렬화된 `nextSceneName`이 fallback이 된다.
3. 전체 경로는 `2_Intro/IntroSceneTransition.Start → LoadingSceneController.LoadTarget(target) → 6_LoadingScene_0 → LoadingSceneController.Start → LoadSceneAsync(target)`이다. XR Interactor, Raycaster, Collider와 입력 Layer는 이 전환에 관여하지 않는다.
4. 대상 씬이 Build Settings에 없으면 자동 대체하지 않고 기존 `Application.CanStreamedLevelBeLoaded` 오류 경로로 멈춘다.
5. 영향 소비자는 Intro 이후 로딩 전환과 로딩 씬 직접 실행 fallback이다. PPE 상태, UI 배치, 오디오 규칙, XR 입력, 텔레포트와 Collider는 변경하지 않는다.
6. 변경 전 기준은 Intro와 로딩 씬이 `3_PPE_Room_Train_Test_mask`를 가리키고 Build Settings도 해당 씬을 활성화한 상태다. 변경 후 네 경로가 모두 `3_PPE_Room_3mode_loco`로 일치하는지 하네스로 확인한다.
7. 정적 배선, Unity 컴파일, `LoadingSceneBuilder.Validate`와 Play Mode 비동기 전환을 확인한다. Quest 양안 전환은 별도 수동 검증으로 남긴다.

대응 요청은 로딩 씬 다음에 `Assets/Scenes/3_PPE_Room_3mode_loco.unity`가 열리게 하는 것이다. 기존 `0_App → 1_Title → 2_Intro → 6_LoadingScene_0` 순서와 로딩 UI 작성값은 보존한다.

### 적용한 변경

- `2_Intro`의 `IntroSceneTransition.nextSceneName`을 `3_PPE_Room_3mode_loco`로 변경했다.
- `6_LoadingScene_0`의 `LoadingSceneController.nextSceneName` fallback을 같은 씬으로 변경했다.
- 두 런타임 컴포넌트의 새 컴포넌트용 기본값과 `LoadingSceneBuilder`의 검증 기준을 같은 씬으로 통일했다.
- Build Settings의 활성 PPE 항목을 `3_PPE_Room_Train_Test_mask`에서 `3_PPE_Room_3mode_loco`로 교체했다.

### 근본 원인과 영향 범위

Intro에서 넘기는 대상, 로딩 씬 fallback, 코드 기본값, Build Settings가 이전 PPE 씬을 기준으로 남아 있었다. 네 지점을 한 대상명으로 맞췄으며 로딩 UI의 기존 RectTransform·진행 시간·색·참조, PPE 장착 로직, 오디오, XR 입력 및 환경 Collider에는 변경을 가하지 않았다.

### 완료한 검증

- 씬 YAML과 코드 기본값에서 대상명이 모두 `3_PPE_Room_3mode_loco`로 일치함을 확인했다.
- `Tools > Loading Scene > Validate Progress UI`와 동일한 `LoadingSceneBuilder.Validate`를 Unity Editor에서 실행해 통과했다.
- `2_Intro`에서 Play Mode를 시작해 비동기 로딩 후 활성 씬이 실제로 `3_PPE_Room_3mode_loco`로 전환됨을 확인했다.
- 전환 후 Unity Console의 오류와 경고는 각각 0개였다.
- Play Mode 종료 후 작업 씬을 `3_PPE_Room_3mode_loco`로 복구했고 씬이 dirty 상태가 아님을 확인했다.

### 아직 필요한 수동 검증

- Meta Quest/OpenXR 헤드셋에서 Intro와 로딩 UI의 양안 표시, XR 초기화 시간, 최종 씬 활성화를 확인해야 한다.

## 2026-08-31 후속: PPE 활성화 순간 전체 화면 에메랄드 플래시

### 증상과 원인 분리

- 사용자가 지목한 현상은 파란 룸 벽이나 태블릿·포스터 Renderer가 아니라, 로딩 씬에서 PPE 씬으로
  교체되는 순간 화면 전체에 밝은 에메랄드 단색이 한 번 노출되는 현상이다.
- PPE 씬의 Main Camera와 비활성 Hand Tracking Camera를 검정 Solid Color로 바꾸는 1차 비교를 했지만
  현상이 유지됐다. 따라서 PPE 카메라 배경이나 Skybox가 원인이 아니며 해당 비교 변경은 되돌렸다.
- 기존 `LoadingSceneController`는 PPE 로드를 `LoadSceneMode.Single`로 활성화했다. 이 방식은 로딩 씬의
  카메라가 제거된 뒤 PPE 카메라가 XR 프레임을 처음 제출하기 전까지 전환 공백이 생길 수 있으며,
  이때 XR 쪽 기본 단색이 전체 화면에 노출되는 것이 현재 원인 후보다.

### 비교 적용한 변경과 롤백

- PPE 대상 씬을 `LoadSceneMode.Additive`로 활성화하고, 로드 완료 뒤 PPE 씬을 Active Scene으로 지정한다.
- 로딩 씬의 검정 카메라는 depth `100`으로 작성해 PPE 카메라보다 마지막에 렌더링한다.
- 새 Inspector 작성값 `postActivationCoverFrames=2` 동안 로딩 씬을 유지한 뒤
  `SceneManager.UnloadSceneAsync`로 로딩 씬만 내린다. 따라서 PPE 카메라가 준비되기 전 XR 전환 공백은
  기존 로딩 화면이 가린다.
- 로딩 UI의 기존 RectTransform·색·진행 시간, PPE 씬의 카메라·룸·입력·상태 흐름은 변경하지 않았다.
- `LoadingSceneBuilder` 검증은 검정 Solid Color, 카메라 depth 100 이상, 활성화 후 커버 프레임 1 이상을
  요구하도록 확장했다.

실제 Unity Play Mode에서 이 Additive 비교안은 채택할 수 없었다. 로딩 씬의 XR 오브젝트가 unload된 뒤
PPE 씬의 `XRInteractionManager`와 `NearFarInteractor`가 파괴된 Attach GameObject를 계속 참조해
`InteractionAttachController.DoUpdate()`에서 `MissingReferenceException`이 매 프레임 반복됐다.
즉시 Play Mode와 Unity를 종료하고 다음 항목을 모두 기존 상태로 되돌렸다.

- 대상 PPE 로드: `LoadSceneMode.Additive` → 기존 `LoadSceneMode.Single`
- 로딩 카메라 depth: `100` → 기존 `0`
- `postActivationCoverFrames`, Active Scene 변경 및 로딩 씬 수동 unload 코드 제거
- `LoadingSceneBuilder`의 Additive 커버 전용 검증 제거

현재 저장소에는 반복 오류를 일으킨 Additive 전환이 남아 있지 않다. 에메랄드 플래시는 별도 안전한
전환 방식이 실제 XR 참조 수명주기를 보존한다는 근거를 확보하기 전까지 미해결로 유지한다.

### 실패 재현, 복구 검증과 남은 항목

- 실패 로그에서 파괴된 GameObject를 참조하는 XRI `InteractionAttachController` → `NearFarInteractor` →
  `XRInteractionManager.Update()` 반복 경로를 확인했다.
- 롤백 뒤 씬과 코드에서 Additive 대상 로드, depth `100`, `postActivationCoverFrames`, 수동 unload 경로가
  모두 제거됐는지 정적으로 확인한다.
- Unity 재실행 후 반복 `MissingReferenceException`이 없는지 먼저 확인해야 한다. 에메랄드 전체 화면은
  아직 미해결이며, 오류가 없는 기존 전환을 보존한 상태에서 후속 원인 분리를 진행한다.

## 2026-09-03 후속: Meta Alpha Quest 2에서 로딩 UI가 보이지 않는 검정 화면

### 재현과 최근 변경 대조

- Meta Alpha 채널로 설치한 앱을 Quest 2에서 실행했을 때 `2_Intro`가 끝난 뒤 검정 화면이 이어지고,
  로딩 로고·백분율·진행 막대가 보이지 않은 채 `4_PPE_Room`으로 전환됐다.
- ADB로 확인한 최초 재현 설치본은 `versionCode=1`, `targetSdk=36`인 이전 APK였다. 업로드한
  `versionCode=2`, `targetSdk=34` 설치 여부와 수정 후 실기 결과는 별도로 구분한다.
- `LoadingSceneController`의 전환 경로와 `3_Loading`의 직렬화된 6초 진행시간에는 최근 변경이 없었다.
- 직전 세션 방향 보정 커밋은 `1_Title`, `2_Intro`, `4_PPE_Room`에만
  `XRSessionForwardAlignment`를 연결했고, 동일한 월드 공간 UI 구조를 사용하는 `3_Loading`은 누락했다.

### 변경 전 필수 판단

1. 기존 Inspector/씬 작성값을 보존한다. 로딩 Canvas의 위치·크기·색·참조, `Prewarm Frames=2`,
   `Progress Fill Duration=6`을 변경하지 않는다.
2. 세션 수평 방향의 단일 소유자는 `XRSessionForwardAlignment`의 정적 세션 정렬값이고,
   로딩 진행과 씬 활성화의 단일 소유자는 `LoadingSceneController`다.
3. 입력 경로는 이 결함과 무관하다. 표시 경로는
   `XROrigin/Camera -> 월드 공간 Canvas(+Z) -> CanvasGroup 표시 -> LoadingSceneController 진행 갱신`이다.
4. 참조 누락을 런타임 자동 수리하지 않는다. `LoadingSceneBuilder`의 명시적 Editor 메뉴로 컴포넌트를
   씬에 추가하고 검증 실패로 누락을 알린다.
5. 영향 소비자는 로딩 UI의 Quest 양안 표시와 Intro/PPE 사이의 시선 방향 연속성이다. PPE 상태,
   XR 입력, 텔레포트, 오디오 규칙은 변경하지 않는다.
6. 변경 전 기준은 Quest 2 Alpha 코드 1에서 검정 화면과 로딩 UI 누락이다. 변경 후에는 코드 3 APK로
   같은 시작 경로를 실행해 약 6초 동안 로고·백분율·진행 막대가 양안 정면에 보이는지 비교한다.
7. 정적 확인과 Unity Editor 하네스는 로컬에서 수행하고, Quest/OpenXR 확인은 새 APK 설치 후 별도 기록한다.

### 근본 원인과 적용 방향

`3_Loading`의 Canvas는 Screen Space Overlay가 아니라 월드 공간 Canvas이며, 씬 기준 `+Z` 방향의
`(0, 0, 200)`에 배치돼 있다. 이전 씬에서 캡처한 HMD 수평 방향을 로딩 씬의 XROrigin에 다시 적용하지
않으면 카메라가 이 Canvas를 바라보지 않을 수 있다. 이 경우 로딩 코루틴과 6초 진행 제한은 실행되지만
사용자는 불투명 검정 카메라 배경만 보게 된다.

`LoadingSceneBuilder`가 `3_Loading`의 유일한 XROrigin에 `XRSessionForwardAlignment`를 명시적으로
연결하도록 보완하고, `LoadingSceneBuilder.Validate`와 `XRSessionForwardAlignmentValidationHarness`가
이 연결 누락을 실패로 검출하도록 확장한다. Additive 씬 전환이나 런타임 UI 재배치는 다시 도입하지 않는다.

### 검증 상태

- 정적 근거: `3_Loading`의 월드 공간 Canvas, `+Z` 배치, 불투명 검정 카메라, 6초 진행시간,
  방향 정렬 컴포넌트 누락을 확인했다.
- Unity Editor 확인: `Tools > Loading Scene > Build Progress UI` 실행 후 로딩 UI 검증과
  `Tools > XR > Validate Session Forward Alignment`이 모두 PASS했다.
- Quest/OpenXR 확인: Quest 2에 코드 3 APK를 설치해 사용자가 로딩 UI 표시가 반영됐음을 확인했다.
  기기 JSONL의 `scene_loaded` 시각은 `3_Loading` 진입 `07:11:01.647754Z`, `4_PPE_Room` 진입
  `07:11:07.924301Z`로 기록돼 로딩 씬이 약 6.28초 유지된 사실도 확인했다. 양안을 각각 가린
  분리 검사는 수행하지 않았으므로 별도 XR 양안 검증 항목은 유지한다.

## 2026-09-09 후속: 로딩 UI Scene View 편집 가시성

### 변경 전 필수 판단

1. 로딩 Canvas와 자식의 RectTransform·색·Sprite·진행시간 작성값을 보존하고 최상위 `loadingContentGroup` alpha만 편집 가시값 `1`로 둔다.
2. 편집 배치 기준은 생산 `3_Loading/Canvas`, 재생 중 표시 상태 소유자는 `LoadingSceneController`다.
3. 입력 소비자는 없고 표시 경로는 `World Space Canvas → loadingContentGroup → TitleLogo2d/LoadingProgressRoot`다.
4. 런타임 자동 생성·재배치·fallback을 추가하지 않는다.
5. 로딩 메인 로고·백분율·진행 막대의 편집 가시성만 영향받는다. `PartnerLogos` 비활성, 씬 전환, XR 방향과 PPE는 보존한다.
6. 변경 전 자식 로고 alpha는 `1`이지만 부모 `loadingContentGroup.alpha = 0`이라 전체가 숨겨진다. 변경 후 씬에서는 `1`이며,
   최종 `Prewarm Frames=0` 구성에서는 Play Mode의 `Awake()`도 첫 프레임 표시를 유지해야 한다.
7. 씬 diff와 컴파일을 정적으로 확인하고 Scene View 및 Play Mode는 Unity에서 비교한다. Quest/OpenXR 양안은 별도 검증이다.

## 2026-09-09 후속: 로딩 표시 시간을 12초로 조정

### 변경 전 필수 판단

1. 기존 Inspector/씬 작성값 가운데 `Progress Fill Duration`만 `6`에서 `12`로 바꾸고 UI 배치와 시각값은 보존한다.
2. 로딩 진행 시간의 단일 소유자는 `3_Loading/Canvas`의 `LoadingSceneController` 직렬화 값이다.
3. 입력 경로는 이 변경과 무관하며 표시 경로는 기존 `LoadingSceneController` 진행 코루틴을 유지한다.
4. 참조 누락이나 설정 오류를 런타임에서 자동 수리하지 않는다.
5. 영향 소비자는 로딩 막대와 다음 씬 활성화 시점이다. 로고 회전은 아래 후속 항목에서 로딩 종료까지 반복하도록 별도로 조정한다.
6. 변경 전 기준은 6초 진행이며, 변경 후 비교 실행은 로고가 최소 2회 이상 반복되는 동안 진행 막대가 약 12초에 걸쳐 완료되는지 확인한다.
7. 정적 직렬화와 Unity Editor Play Mode까지 확인하고 Quest/OpenXR의 체감 시간과 양안 표시는 별도로 확인한다.

## 2026-09-09 후속: 마우스 동작 기반 로고 회전 속도 조정

### 변경 전 필수 판단

1. `TitleLogo2d`의 기존 RectTransform과 로컬 회전 작성값은 보존하고 `LoadingLogoSpin`의 직렬화된 시간·곡선만 조정한다.
2. 회전 상태의 단일 소유자는 `TitleLogo2d`에 연결된 `LoadingLogoSpin`이며, 원래 로컬 회전은 시작·종료·비활성화 시 복원한다.
3. XR 입력 경로는 사용하지 않는다. 마우스는 원하는 속도 측정에만 사용하고 런타임 입력 소비자로 추가하지 않는다.
4. 참조가 누락되면 기존처럼 한 번의 명확한 오류 후 컴포넌트를 비활성화하며 자동 수리하지 않는다.
5. 영향 소비자는 로딩 메인 로고의 Y축 회전뿐이다. 진행 막대, 오디오, 파트너 로고 비활성, 씬 전환과 XR 방향은 보존한다.
6. 변경 전 기준은 `Spin Duration=0.55초`, 대칭 EaseInOut 곡선과 `Prewarm Frames=2`다. 변경 후에는 측정된 약 `1.11초`와 초반 가속·후반 감속을 반대 Y축 방향으로 적용하고, 첫 프레임에 첫 회전을 시작한 뒤 회전 사이 `0.5초` 정지를 두고 계속 반복하는지 비교한다.
7. 직렬화·컴파일·Unity Play Mode까지 확인하고 Quest/OpenXR 양안에서의 체감과 잔상 여부는 별도 확인한다.

### 측정 결과

- 방향과 이동 너비는 정규화하고 시간과 누적 이동 비율만 사용했다.
- 주 동작 지속시간은 `1.111초`였다.
- 누적 이동의 `10%`는 `0.084초`, `50%`는 `0.147초`, `90%`는 `0.727초`에 도달했다.
- 따라서 느리게 출발하는 무거운 회전이 아니라 초반에 빠르게 면을 넘긴 뒤 길게 감속해 정면으로 복귀하는 곡선으로 적용한다.

### 최종 적용값

- `Delay After Visible=0`: 로고가 표시되는 즉시 첫 회전을 시작한다.
- `Spin Duration=1.11`: 측정된 한 번의 동작 시간을 유지한다.
- 연속 반복: 횟수 제한이나 별도 런타임 체크값 없이 `LoadingLogoSpin`이 활성화된 동안 계속 반복한다.
- `Prewarm Frames=0`: 로딩 UI를 첫 프레임부터 표시해 첫 회전 대기를 제거한다.
- `Reverse Direction=true`: 측정된 속도 곡선은 유지하고 Y축 회전 방향만 반대로 적용한다.
- `Pause Between Spins=0.5`: `0.25초` 비교 후 회전 사이 호흡을 조금 늘려 0.5초로 조정한다.
- `Progress Fill Duration=12`: 로고가 계속 반복되는 동안 로딩 진행을 유지한다.
- `Max Animation Frame Step=0.033333335`: OpenXR/Editor 초기화로 첫 프레임 시간이 길어져도 한 프레임이 회전 전체를 소모하지 않게 하여 반복 회전을 화면에 표시한다.

`Prewarm Frames=0`은 사용자가 요청한 즉시 회전을 위해 기존 2프레임 XR Canvas 예열을 제거하는 변경이다. Unity Editor에서는 첫 프레임 표시를 비교하고, Quest/OpenXR에서는 로고 크기나 위치가 첫 프레임에 튀지 않는지 별도로 확인한다.

첫 프레임 즉시 회전으로 바꾼 뒤에는 초기화 프레임의 큰 `Time.unscaledDeltaTime`이 `Spin Duration` 전체를 한 번에 소모할 수 있었다. 이 경우 첫 회전이 렌더링 전에 끝나 두 번째 회전만 보이고, 사용자는 `Pause Between Spins=0.25`보다 훨씬 오래 기다린 것처럼 느낀다. 회전 코루틴의 프레임당 진행량만 직렬화된 상한으로 제한하고, 실제 대기시간과 12초 로딩 진행 계산은 기존 unscaled 시간 기준을 유지한다.

### 적용·검증 상태

- 사용자 피드백에 따라 2회 제한은 요구사항 오해로 판정하고 제거했다. 로고는 `LoadingLogoSpin`이 활성화된 동안 계속 반복하며 로딩 씬이 끝날 때 정지하고 작성된 로컬 회전으로 복원된다.
- `Pause Between Spins`는 `0.25초`와 `0.5초`를 Unity Play Mode에서 비교했고, 사용자가 최종 `0.5초` 호흡을 확인했다.
- Unity 씬 작성값은 `Delay After Visible=0`, `Spin Duration=1.11`, `Pause Between Spins=0.5`, `Reverse Direction=true`, `Max Animation Frame Step=1/30`, `Progress Fill Duration=12`, `Prewarm Frames=0`이다.
- 정적 확인에서 로고와 CanvasGroup의 명시적 참조, Loading 파트너 로고 비활성, 연속 반복 코루틴, 첫 프레임 진행량 상한을 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore`와 `dotnet build Assembly-CSharp-Editor.csproj --no-restore`의 최종 실행은 모두 오류 0개로 통과했다. 전체 런타임 빌드 출력에서는 기존 프로젝트 경고 64개가 한 차례 확인됐고 이번 로고 코드 오류는 없었다.
- Unity Editor Play Mode에서 즉시 시작, 반대 Y축 방향, 연속 반복과 0.5초 간격을 사용자가 확인했다.
- `LoadingSceneBuilder`는 컴포넌트를 새로 추가할 때만 현재 기본 참조·시간·측정 곡선을 작성하고, 기존 `LoadingLogoSpin`의 Inspector 값을 반복해서 덮어쓰지 않도록 보완했다. 검증 경로는 필수 참조와 최종 시간·방향·첫 프레임 상한을 검사한다.
- `SceneDependencyValidationHarness`는 로딩 씬의 직렬화 값과 연속 반복·프레임 상한 코드 계약을 정적으로 검사하도록 확장했다.
- Unity 하네스 직접 호출은 Play 종료 뒤 MCP relay가 도메인 재로딩 중 120초 제한을 반복해서 초과해 완료 결과를 받지 못했다. 따라서 하네스 PASS로 기록하지 않으며, Unity 연결 복구 후 `Tools > Build > Validate Scene Dependencies`와 `Tools > Loading Scene > Validate Progress UI`를 다시 실행해야 한다.
- 아직 필요한 수동 검증은 Quest/OpenXR 양안의 첫 프레임 크기·위치, 로고 잔상·시머링, 12초 체감 시간이다. Editor 확인을 Quest 정상으로 확대하지 않는다.
