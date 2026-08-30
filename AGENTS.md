# Chemical Safety VR Client Repository Context

## Project purpose

This repository contains the Unity XR chemical safety training client together with project documentation, editor tools, and validation harnesses. The Unity application targets Meta Quest/OpenXR and includes controller education, PPE education/training/testing flows, spatial UI, voice guidance, hand/controller interaction, and training-record integration with the separately managed server.

The Express server is maintained in the private repository `softCastella/chemical-safety-vr-server`. This repository must not acquire a duplicate `server/` source tree.

## Environment

- Unity: 6000.4.8f1
- Render pipeline: Universal Render Pipeline (URP) 17.4.0
- XR runtime: OpenXR
- XR Interaction Toolkit: 3.4.1
- XR Hands: 1.7.3
- Input System: 1.19.0
- Primary targets: Meta Quest/Android and PC OpenXR

## Enabled build flow

`ProjectSettings/EditorBuildSettings.asset` currently enables these scenes in order:

1. `Assets/Scenes/0_App.unity`
2. `Assets/Scenes/1_Title.unity`
3. `Assets/Scenes/2_Intro.unity`
4. `Assets/Scenes/6_LoadingScene_0.unity`
5. `Assets/Scenes/3_PPE_Room_3mode_loco.unity`

`4_InsideMixer`, `5_MixerRoom_Unlit`, and the older loading scene remain disabled follow-up scenes. The disabled `Confined Space Scene_half` entry points to an absent asset and must not be treated as the current primary scene.

## Project-owned runtime code

- `Assets/Scripts/AppSceneBootstrap.cs`
  - Owns application-scene startup behavior.
- `Assets/Scripts/PPEVoiceFlowDirector.cs`
  - Coordinates the current PPE voice and onboarding flow.
- `Assets/Scripts/PPEActionPanelController.cs`
  - Owns PPE action-panel interaction and result handling.
- `Assets/Scripts/PhysicalHmdSimulatorGate.cs`
  - Separates physical HMD behavior from Editor/Game View simulation.
- `Assets/Scripts/SciFiCardVisual.cs`
  - Procedurally builds layered rounded-card meshes, frame, glass, glow, shadow, corner markers, and collider geometry.
- `Assets/Scripts/SciFiCardDepthResponse.cs`
  - Adds camera-relative parallax to card background, content, and foreground layers.

The sci-fi card scripts exist, but scene references must be verified before changing their behavior.

## Project-owned editor tools

- `Assets/Editor/SciFiCardMaker.cs`
- `Assets/Editor/PlaneMaker.cs`
- `Assets/Editor/XRHandMaterialMaker.cs`
- `Assets/Editor/BackgroundGradientShaderGenerator.cs`
- `Tools/generate_background_shadergraph.ps1`
- `Tools/generate_background_shadergraph.py`

## Assets and samples

Large parts of `Assets` are imported industrial models or Unity package samples. Treat these as third-party/sample content unless the task explicitly targets them:

- `Assets/Oculus`
- `Assets/RPG_FPS_game_assets_industrial`
- `Assets/Samples`
- `Assets/TextMesh Pro`
- `Assets/TripoModels`
- `Assets/XRI_Examples`

Prefer adding project-specific code under `Assets/Scripts` and editor-only code under `Assets/Editor`.

## Working rules

- Preserve `.meta` files and Unity asset GUIDs.
- Do not manually edit generated folders: `Library`, `Temp`, `Logs`, or `UserSettings`.
- Do not treat generated `.csproj` and `.slnx` files as authoritative project configuration.
- Check `ProjectSettings/EditorBuildSettings.asset` before changing assumptions about the startup scene.
- Keep runtime code out of `Assets/Editor`.
- When modifying a Unity YAML scene or prefab directly, make small changes and verify file IDs, GUIDs, and serialized references carefully.
- Prefer Unity-compatible C# APIs supported by the configured Unity version.
- For XR changes, account for both Android/Quest and Standalone OpenXR unless the requested target is explicit.
- Do not modify imported packages or samples when a project-owned wrapper or component is sufficient.

## Git 커밋·푸시 규칙

- 커밋 제목에는 한글을 포함한다.
- 커밋 본문에는 `1. `부터 시작하는 번호 목록으로 변경 내용과 검증 결과를 기록한다.
- 저장소의 `.githooks/commit-msg`, `.githooks/pre-push`와 `Tools/KoreanCommitMessageHarness.mjs`를 기준 하네스로 사용한다.
- 새 clone에서는 `git config core.hooksPath .githooks`로 Git 훅을 활성화한다.
- 커밋·푸시 결과를 사용자에게 보고할 때도 한글 번호 목록으로 커밋 SHA, 제목과 푸시 대상을 구분한다.

## 교차 저장소 인수인계 규칙

- 클라이언트와 서버의 Codex 대화는 자동으로 공유된다고 가정하지 않는다. 코드, 문서, 테스트 결과와 Git 커밋을 작업 사실의 기준으로 사용한다.
- 서버 연동 사실을 작성하거나 수정할 때는 `softCastella/chemical-safety-vr-server`의 대상 브랜치와 커밋 SHA를 확인한다. 대화 내용만으로 API, DB, 텔레메트리 또는 배포 상태를 확정하지 않는다.
- 클라이언트 변경이 API, 데이터 계약, 인증, 텔레메트리 또는 대시보드에 영향을 주면 관련 기존 문서에 영향과 필요한 서버 후속 작업을 기록한다. 관련 문서가 없을 때만 문서 작성 원칙에 따라 새 문서를 만든다.
- 최종 보고자료를 작성하기 전에 클라이언트와 서버 저장소의 최신 기준 커밋을 각각 기록하고, 양쪽 코드·씬·로그·테스트 근거를 필요한 범위에서 대조한다.
- 서버 저장소의 변경이 아직 클라이언트 저장소에 반영되지 않았으면 완료로 합쳐 쓰지 않고 `서버 반영`, `클라이언트 반영`, `통합 검증` 상태를 구분한다.
- `Docs/SharedDocumentManifest.md`에 지정된 공용 문서는 서버 저장소의 같은 상대 경로에 미러링한다. 최종 문서 기준본은 클라이언트 저장소에 두고, 서버에서 공용 사실을 변경한 경우 서버 커밋과 클라이언트 동기화 필요 여부를 함께 기록한다.

## UI authoring rules

Apply these rules to every UI element and UI system in the project, not only to a specific scene, modal, or component:

- When creating or modifying UI, treat scene, prefab, and Inspector-serialized values as the authoritative UI configuration.
- Do not hardcode UI presentation values in runtime code. This includes positions, sizes, anchors, pivots, scale, spacing, colors, fonts, font sizes, labels, sorting order, and other visual or layout values.
- Expose UI configuration through serialized fields when runtime access is required, and preserve the values authored in the Inspector, scene, or prefab.
- Runtime code may change UI state or dynamic content when required, but it must not silently replace authored presentation or layout values.
- Whenever a UI element is created, modified, or diagnosed, inspect all related runtime assignment code together with the scene or prefab changes. Do not complete a UI change after editing only the serialized asset while leaving conflicting runtime assignments unexamined.
- When diagnosing or changing UI, trace the complete runtime assignment path, including `Awake`, `OnEnable`, `Start`, scene-load callbacks, initialization helpers, instantiated clones, layout components, animation systems, and editor-generated setup code.
- Explicitly check whether entering Play Mode overwrites scene, prefab, RectTransform, Canvas, TMP, material, or component values. Remove or redesign unintended runtime overwrites instead of compensating for them with additional hardcoded assignments.
- Prefer scene- or prefab-authored UI objects with serialized references over runtime-created UI. If runtime creation is genuinely required, use a serialized prefab or serialized configuration as its source of truth.
- Editor builders and migration tools may provide initial defaults when creating an object, but must not repeatedly overwrite existing authored UI values. After creation, the serialized scene or prefab values are authoritative.

## XR shader authoring rules

Apply these rules to project-owned hand-written shaders used by world-space UI, scene geometry, hands, or other XR-visible objects:

- Treat Meta Quest/OpenXR Single Pass Instanced rendering as a required shader path. Do not consider a shader complete after validating only the Unity Game view or one eye.
- Vertex input structs must include `UNITY_VERTEX_INPUT_INSTANCE_ID`.
- Vertex output structs must include `UNITY_VERTEX_OUTPUT_STEREO`.
- At the start of the vertex function, call `UNITY_SETUP_INSTANCE_ID(input)` before `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)`.
- In fragment functions that require the stereo eye index, call `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)`.
- When instance data must reach the fragment stage, include the instance ID in the varyings and use `UNITY_TRANSFER_INSTANCE_ID(input, output)` plus `UNITY_SETUP_INSTANCE_ID(input)` in the fragment function.
- A symptom where an object disappears when the left eye is closed, renders in only one eye, or differs in position between eyes should first be investigated as missing Single Pass Instanced initialization.
- Prefer URP-provided shader libraries and XR-compatible URP/Shader Graph shaders. When importing a custom or third-party shader, explicitly verify its stereo instancing macros before assigning it to XR UI.
- After modifying a shader, wait for shader import and script compilation to finish before entering Play Mode. Do not start Quest/OpenXR Play Mode during compilation or domain reload because the OpenXR loader restart can destabilize or disconnect Quest Link.
- Validate custom XR shaders on both eyes of a Quest or OpenXR headset. Game view validation alone is insufficient.

## Generated scene content rules

- `[ExecuteAlways]`, `OnEnable`, and `OnValidate` code must not delete and recreate scene-authored geometry or overwrite child Transform values automatically.
- Procedural rebuild operations must be explicit editor commands or context-menu actions. After generation, the serialized scene hierarchy and Inspector values are authoritative.
- If runtime-only material recovery is necessary, separate it from geometry generation. Reapply materials to existing renderers without rebuilding meshes, hierarchy, positions, rotations, or scales.
- `PPEBackgroundRoom` intentionally creates room-surface materials with `HideFlags.DontSave`. A serialized `m_Materials` entry of `{fileID: 0}` on its generated room surfaces is therefore not sufficient evidence of a broken material; inspect the live Renderer after `OnEnable` has run.
- Scene-authored PPE room continuation surfaces named `Rear Wall (n)`, `Floor (n)`, or `Ceiling (n)` must participate in the same material recovery as their unnumbered source surface. Preserve their authored Transforms and do not rebuild the room to repair a missing material.
- After changing PPE room material recovery, run `Tools > PPE > Validate Room Surface Materials` or invoke `PPERoomMaterialRecoveryHarness.Validate` with Unity batch mode.

## Validation

For code changes, check for C# compilation errors and inspect Unity logs when available. For UI changes, compare relevant serialized values before and after entering Play Mode and verify that no unintended runtime assignment changes them. For scene, prefab, shader, XR, or rendering changes, explain any verification that still requires opening Unity or testing on a headset.

For a reproducible project-owned regression, add a dated report under `Docs/Bug` and add or extend an Editor validation harness when the failure can be checked deterministically.

## 문서 작성 원칙

- 사용자가 영문 작성을 명시적으로 요청하지 않으면 프로젝트 문서는 한국어로 작성한다.
- 코드 식별자, 파일 경로, API 이름 및 원문 확인이 필요한 오류 메시지는 정확성을 위해 원래 표기를 유지할 수 있다.
- 새 문서를 만들기 전에 `Docs`와 `Assets/Docs`에서 동일 작업, 기능 또는 오류를 다루는 기존 문서가 있는지 먼저 확인한다.
- 관련 기존 문서가 있으면 새 문서를 만들지 않고 해당 문서에 내용을 추가하거나 기존 내용을 수정한다.
- 관련 기존 문서가 없거나 사용자가 문서 분리를 명시적으로 요청한 경우에만 새 문서를 만든다.
- 같은 작업의 중복 문서를 실수로 만들었으면 내용을 기존 기준 문서에 통합하고 중복 문서를 제거한다.
- 날짜가 붙는 작업 기록은 `Docs/MeetingNotes`의 회의록 또는 `Docs/Bug`의 버그 리포트로만 작성한다.
- `Daily`, `Worklog`, `데일리로그`, `작업일지` 형식의 문서나 파일명은 만들지 않는다. 작업 내용·결정·후속 항목은 회의록에 기록하고, 재현 가능한 결함은 버그 리포트에 기록한다.
- 문서에는 적용한 변경, 근본 원인, 영향 범위, 완료한 검증 및 아직 필요한 수동 검증을 구분해 기록한다.
- 문서 원칙을 변경한 뒤에는 `Tools > Documentation > Validate Authoring Policy`를 실행하거나 `DocumentationPolicyHarness.Validate`를 호출한다.

## Session startup

At the beginning of a task, use this file as orientation, then inspect the files directly relevant to the request. Do not rescan `Library`, `Temp`, or the full imported asset collection unless necessary.

## 오늘 작업에서 확인된 판단 실패와 재발 방지 규칙 (2026-08-05)

오늘 PPE HandTest 작업의 문제는 단순 오타보다 요구사항을 상태·입력·렌더링·직렬화의 전체 흐름으로 분해하지 않고 바로 구현한 데서 발생했다. 다음 규칙을 이후 작업의 하네스 기준으로 사용한다.

### 판단 실패의 원인

- 카드·모달·텔레포트를 각각의 함수 문제로만 보고 상태 전이표를 먼저 만들지 않았다. 그 결과 Canvas를 먼저 숨겨 모달 호출이 끊기거나, 모달이 열린 상태에서 텔레포트가 살아 있는 순서 오류가 생겼다.
- PPE를 marker 기준으로 잡는 요구를 Grab 컴포넌트의 Collider 목록만 제한하는 것으로 축소했다. 실제 Interactor, 물리 레이어, UI Raycaster, Collider, Interaction Layer 전체를 함께 확인하지 않아 원거리 레이가 marker를 맞히는 위험을 남겼다.
- 누락된 Rigidbody·marker Collider·패널 참조를 Inspector에서 고쳐야 할 문제로 분류하지 않고 런타임 자동 수리·fallback으로 처리하려 했다. 이 때문에 반복 콘솔 오류와 초기화 부담이 생겼다.
- 풀장착 방호복의 부모 기준을 먼저 확정하지 않고 자식과 거울을 각각 보정하려 했다. 자식이 방호복과 다른 위치·방향으로 렌더링되고, 거울의 Y와 바닥 접지를 실제 Bounds로 검증하기 전에 해결된 것으로 추정했다.
- 거울 레이어·RenderTexture·반사 카메라·Play Mode 표시를 기준 실행 없이 함께 변경했다. 거울 검은 화면, 로드 지연, 픽셀화, 헤드셋 주변 시야 깨짐의 원인을 분리할 실험 조건과 Profiler 자료가 없었다.
- `dotnet build`와 씬 YAML 확인을 실제 Play Mode·Quest/OpenXR 성공으로 확장해서 해석할 위험이 있었다. Unity 라이선스 때문에 실행하지 못한 상태에서는 구현 완료라고 표현하지 않아야 한다.
- 헬멧 관련 직전 작업이 있었는데도 그 변경분을 첫 번째 원인 후보로 두지 않고, 현상만으로 카메라·Unity Editor·Quest Link 같은 실행 환경을 먼저 의심했다. 이 접근은 최근 변경으로 생긴 회귀를 놓치고 사용자가 환경 문제를 먼저 조사하게 만든다.

최근 작업 직후 발생한 문제는 반드시 다음 순서로 원인을 좁힌다.

1. 직전 세션의 변경 파일·씬 오브젝트·머티리얼·레이어·컴포넌트와 재현 시점을 대조한다.
2. 방금 추가·수정한 장비를 비활성화하거나 변경 전 상태와 비교해 회귀 여부를 확인한다.
3. 최근 변경으로 설명되지 않을 때만 카메라, Unity Editor, OpenXR, Quest Link, 드라이버를 환경 원인으로 확장한다.
4. 환경 원인을 언급할 때도 최근 변경을 제외했다는 근거와 별도 재현 결과를 함께 기록한다.

증상만 보고 인프라·카메라·링크 문제로 분류하지 않는다. “최근 변경 후보”, “씬/직렬화 후보”, “실행 환경 후보”를 구분하고, 최근 변경 후보를 먼저 제거·비교한 뒤 다음 범위로 이동한다.

### 변경 전 필수 질문

코드나 씬을 수정하기 전에 다음을 작업 기록에 답한다.

1. 기존 Inspector/씬 작성값을 보존하는가?
2. 단일 기준 오브젝트와 상태 소유자는 무엇인가?
3. 입력 이벤트, Interactor/Caster, Raycaster, Layer, Collider의 전체 경로는 무엇인가?
4. 실패 시 런타임 자동 수리 대신 명확한 오류로 멈춰야 하는가?
5. UI·텔레포트·PPE Grab·거울·XR 양안 중 어떤 소비자가 함께 영향을 받는가?
6. 변경 전 기준 실행과 변경 후 비교 실행은 무엇인가?
7. 정적 확인, Unity Editor 확인, Quest/OpenXR 확인 중 어디까지 실제로 검증했는가?

### 이후 구현·검증 규칙

- **요청 범위 불변조건:** 모든 코드·씬·입력 변경은 사용자가 명시한 동작 한 가지에 직접 대응해야 한다. 요청하지 않은 편의 기능, fallback, 자동 선택·완료, 입력 소비자 변경, 상태 전이, UI·오디오 재생 규칙을 추가하거나 기존 동작을 바꾸지 않는다. 기존 동작을 바꿔야만 요청을 충족할 수 있거나 더 나은 확장안이 보이면, 구현·수정하지 말고 제안만 한다. 사용자가 명시적으로 승인한 뒤에만 변경할 수 있다. 작업 전에는 “이번 변경이 대응하는 사용자 요청”과 “보존해야 하는 기존 동작”을 기록하고, 작업 후에는 둘 다 확인한다.

- 카드 흐름은 `카드 대기 → Trigger → 모달 표시/텔레포트 차단 → PPE 착용 선택 → 모달 닫힘/이동 허용` 상태표와 코드가 일치해야 한다.
- 장착 시각의 단일 기준은 `PPE Body Anchor -> hazmat_suit_on_10`이다. 자식은 로컬 Transform만 사용하고, 거울은 모델을 별도 위치에 재배치하지 않는다. 바닥은 루트 Pivot이 아니라 Renderer Bounds 최저점과 Floor 높이로 검증한다.
- 필수 참조가 없으면 런타임에서 자동 생성·자동 수정하지 않는다. 한 번의 명확한 오류와 대상 경로를 기록하고, 수리 작업은 명시적으로 실행하는 Editor 메뉴로 분리한다.
- 거울은 `Off 기준 → On 비교 → RenderTexture/갱신 주기/Culling Mask 단일 변경 → Profiler/Quest 양안 확인` 순서로 검증한다.
- 결과는 `정적 확인`, `Unity Editor 확인`, `Quest/OpenXR 확인`으로 구분한다. 상위 증거가 없으면 “정상 확인”이라고 보고하지 않는다.

### PPE HandTest 완료 조건

- 반복되는 참조·Collider·Rigidbody 자동 수리 오류가 없다.
- 카드 Trigger만 모달을 열고, 모달 중 텔레포트가 차단된다.
- PPE는 marker를 손으로 잡을 때만 선택되고 원거리 카드 레이가 PPE에 닿지 않는다.
- 사용 처리된 PPE 자식만 `hazmat_suit_on_10` 아래에서 표시된다.
- 거울에서 방향·회전·Y·바닥 접지가 본체와 일치한다.
- 거울 On/Off 성능, Quest 양안, 주변 시야 결과가 비교 기록되어 있다.

## 2026-08-05 추가: 문제 오브젝트의 공간 위치 선행 조사

모달, 카드, Canvas 또는 입력 이벤트가 보이지 않거나 반응하지 않는 현상은 해당 컴포넌트의 코드부터 수정하지 않는다. 먼저 사용자가 지목한 문제 오브젝트를 선택한 상태에서 다음 공간·직렬화 정보를 조사한다.

- 전체 계층 경로와 각 부모의 `activeSelf`, `activeInHierarchy`, 월드 위치, 회전, 스케일
- `RectTransform`의 월드 코너, 크기, 앵커, 피벗
- 부모 `Canvas`의 `renderMode`, `worldCamera`, 활성 상태, sorting order, 월드 스케일
- 자식 `Graphic`, `GraphicRaycaster`, `TrackedDeviceGraphicRaycaster`, `Renderer`, `Collider`의 활성 상태, Raycast 설정, 월드 Bounds
- 동일 씬의 룸 후보 Bounds와 문제 오브젝트의 월드 위치가 그 안에 있는지

`Assets/Editor/PPEObjectSpatialDiagnosticHarness.cs`의 `Tools > PPE > Diagnose Selected Object Spatial Context`를 이 선행 조사에 사용한다. 이 하네스는 진단 전용이며 Transform, 입력, 활성 상태, 씬 값을 자동 수정하지 않는다. 공간 진단 없이 카드/모달 입력 로직, Raycaster, 카메라 또는 XR 런타임을 원인으로 단정하지 않는다.

## 2026-08-06 추가: PPE 결함 시각물 표면·대비 검증

장화 오염과 마스크 균열을 생성할 때 모델의 앞면 방향이나 임의 고정 좌표를 가정하지 않는다. 결함 시각물은 대상 PPE의 실제 Renderer Bounds와 동일한 부모 기준에서 표면 오프셋을 계산하고, 자식의 로컬 좌표를 한 번만 적용한다. 생성된 오염/균열이 대상 Renderer Bounds 밖에 있거나 한쪽 면에서만 보이는 상태를 완료로 보고하지 않는다.

검정 장화의 오염은 검정 계열 재질을 사용하지 않고 장화와 구분되는 밝은 시멘트·황갈색 자갈 대비를 확보한다. 마스크 균열은 실제 Bounds 중심·크기와 표면 방향을 기준으로 배치하고, Scene View만이 아니라 Game View와 Quest 양안에서 균열 위치·두께·가시성을 확인한다. Editor 생성기를 수정한 뒤에는 반드시 생성기를 다시 실행해 씬에 적용하고, 컴파일 성공만으로 시각적 완료를 주장하지 않는다.

## 2026-08-05 재발 방지 추가: 카드/XR 입력 패치 판단 실패

이번 HandTest 카드 작업에서 다음 판단 실패가 발생했다. 이후 동일한 순서의 작업에서는 아래 규칙을 강제한다.

### 발생한 실패

- 기준 씬을 `_scale_0`으로 고정하지 않고 `_scale`, `_scale_0`, `_scale_1`을 함께 다루어 작업 대상과 Unity 메모리에서 열린 씬을 혼동했다.
- 카드 입력면을 직접 씬 YAML에 추가하면서 Unity signed 64-bit 범위를 넘는 임의 FileID(`9900000000000000000` 계열)를 사용했다. 그 결과 씬 파서 오류, Unity 메모리의 잘못된 계층 상태, PPE 위치 이상을 유발했다.
- 카드의 hover 시각 효과를 입력 경로가 정상이라는 증거로 오해했다. 실제로는 `XRUIInputModule`의 XR interactor/caster/press action부터 `TrackedDeviceGraphicRaycaster`, `EventSystem`, `PointerClick`, `ScenarioCardSelectProxy.Trigger()`까지의 전체 경로를 확인하지 않았다.
- Game View 마우스 fallback을 XR Trigger 연결의 해결책처럼 취급했다. 마우스 보조 경로와 Quest XR Trigger 경로는 서로 다른 입력 소비자이므로 하나를 켠 것이 다른 하나의 검증이 될 수 없다.
- `TrackedDeviceEventData` 타입 제한을 제거해 입력 범위를 넓혔지만, 실제 런타임 eventData 타입과 Quest 입력 재현을 확인하기 전에 적용했다. 입력 범위 확대는 회귀 위험이 있는 기능 변경으로 취급한다.
- BGM 리소스와 씬 YAML 참조만 확인하고 `AudioManager.Instance`, scene audio coroutine, 실제 `AudioSource.isPlaying`, `AudioListener`, XR/PC 출력 장치를 확인하지 않은 채 BGM 수정이 완료된 것처럼 보고했다.
- `dotnet build`와 정적 YAML 확인을 Unity Play Mode·Quest/OpenXR 성공으로 확장해서 해석했다.

### 이후 작업의 강제 게이트

1. 사용자가 지정한 단일 씬 경로를 먼저 확정한다. 지정하지 않은 variant 씬은 열거나 수정하지 않는다. 작업 시작 시 대상 씬, 현재 Unity 열린 씬, 디스크 씬을 각각 기록한다.
2. 입력 변경 전 반드시 다음 표를 먼저 만든다: `입력 장치 → Interactor → Caster/Ray → Layer/Collider 또는 Graphic → Raycaster → EventSystem/InputModule → press/select action → Pointer/Select event → handler → 상태 소유자`. 한 칸이라도 정적 또는 런타임 증거가 없으면 패치를 적용하지 않는다.
3. XR Trigger 문제에는 mouse fallback, GraphicRaycaster, 일반 Pointer 허용을 XR 해결책으로 간주하지 않는다. Quest Trigger는 `XRUIInputModule`의 등록 interactor와 `UIPress/Select` 입력을 별도로 검증한다.
4. `PointerEnter` 또는 hover 효과는 클릭/Trigger 성공 증거가 아니다. `PointerDown`, `PointerUp`, `PointerClick` 또는 XRI `SelectEntered` 각각의 실제 호출을 구분해 로그와 재현으로 확인한다.
5. 씬 YAML에는 임의 FileID를 만들지 않는다. 새 오브젝트·컴포넌트·참조가 필요하면 Unity Editor가 생성한 FileID를 사용하고, 실행 전후 `git diff --stat`, hierarchy/Transform 변경 여부, FileID 범위를 확인한다. 기존 씬에 대규모 재배열 diff가 생기면 즉시 중단한다.
6. 한 번에 한 소비자만 변경한다. 입력 경로, 모달 상태, 텔레포트, PPE Grab, 오디오를 같은 패치에서 함께 바꾸지 않는다. 각 변경 후 정적 확인 → Unity Editor 확인 → Quest/OpenXR 확인을 구분한다.
7. BGM은 리소스 참조만으로 완료 처리하지 않는다. 실제 씬 이름으로 `Resources.Load`되는지, `AudioManager.Instance`가 생성됐는지, clip load 상태, BGM `AudioSource` 재생 상태, `AudioListener`, XR/PC 출력 장치를 순서대로 확인한다.
8. 테스트하지 못한 기능은 “수정 완료”라고 보고하지 않는다. 특히 헤드셋 미연결 상태에서는 Quest Trigger, 양안 렌더링, XR 오디오를 정상이라고 말하지 않는다.
9. 최근 변경이 원인 후보인 경우 새 패치를 추가하기 전에 최근 변경을 비활성화하거나 변경 전 기준과 비교한다. 원인 미확정 상태에서 fallback·자동 수리·입력 범위 확대를 추가하지 않는다.

## 2026-08-26 추가: 보고자료 사실성 판단 실패와 제출 전 강제 게이트

대시보드 설명 PPT 작성 중 실제 종료 경로를 확인하지 않고 `EXIT Point`를 정상 복귀로 해석하고, 존재하지 않는 종료 원인 선택 UI를 제안했다. 이 판단은 프로젝트의 기존 중도 중단 수단과 보고자료의 신뢰성을 훼손했다.

이후 대시보드·CSV·보고서·PPT를 작성하거나 수정할 때 다음 게이트를 강제한다.

1. 화면 명칭이나 사용자 설명만으로 기능 의미를 추론하지 않는다. 대상 씬의 직렬화 참조, 실제 런타임 핸들러, 텔레메트리 원본 이벤트를 모두 확인한다.
2. `코드에 존재함`, `씬에 연결됨`, `실행 로그로 수집됨`, `서버·대시보드에서 조회됨`을 서로 다른 검증 단계로 구분한다. 앞 단계만 확인하고 뒤 단계가 완료됐다고 쓰지 않는다.
3. 보고자료의 모든 지표·원인·상태에는 `근거 파일 또는 이벤트`, `계산 규칙`, `상세 조회 경로`가 있어야 한다. 하나라도 없으면 확정 지표로 표시하지 않는다.
4. 사용자에게 없던 UI·ID·단계·원인·분류를 보고자료에 새로 만들지 않는다. 제안이 필요하면 현재 사실과 분리된 `제안` 영역에만 쓰고, 제출본에는 사용자 승인 전 포함하지 않는다.
5. 종료 관련 표현은 `EXIT Point 중도 중단`, `기존 종료 버튼`, `application_quitting`, `미종료 세션`을 구분한다. `application_quitting`만으로 전원 종료·충돌·실수 종료를 확정하지 않는다.
6. PPT·CSV·대시보드 전달 전 `Tools > PPE > Validate Training Data Contract`를 실행한다. 하네스 PASS는 정적 계약 확인일 뿐이며, 실제 수집 여부는 최신 JSONL 원본 이벤트를 별도로 확인한다.
7. 자동 하네스가 검사하지 못하는 문장은 코드·씬·로그의 세 근거를 수동 대조한다. 대조하지 못한 문장은 삭제하거나 `미검증`으로 표시한다.
8. 제출용 파일은 전달 전에 페이지 크기, 페이지 수, 한글 렌더링, 잘림, 수치·문구의 원본 근거를 최종 확인한다. 생성 성공만으로 제출 가능하다고 보고하지 않는다.
