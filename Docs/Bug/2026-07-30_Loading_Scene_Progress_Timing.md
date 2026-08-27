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
- `Progress Fill Duration`: `6` seconds
- `Completion Hold Duration`: `0.15` seconds

`6` is the current scene-authored value and produces a six-second minimum fill when the target scene is already ready. Increase `Progress Fill Duration` to make the transition slower. The C# field initializer remains a four-second fallback for a newly added or otherwise unconfigured component; it does not replace the value serialized in `6_LoadingScene`.

The scene also serializes `Prewarm Frames = 2` under `Scene Loading`. During these two frames the root loading `CanvasGroup` remains hidden while the XR Canvas resolves its render size. The percentage and bar timing begin after the complete loading UI is revealed.

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
