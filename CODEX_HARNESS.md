# Codex Harness Guide for Unity VR Safety Training

## 1. Purpose

This document defines how Codex should work inside this Unity VR project.

The project may be edited through several Codex environments, including the VS Code extension, Codex CLI, and other code-assistant workflows. Regardless of environment, Codex must follow the same project rules.

The main goal is not to write clever code. The main goal is to write code that a beginner Unity developer can understand, inspect, test, and safely modify later.

## 2. Project Context

Project title:

- Chemical Safety Training VR
- Korean title: 화학물질 안전훈련 VR

Target platform:

- Unity
- XR / VR
- Meta Quest 2
- Android build target

Main scene flow:

```text
0_App
1_Title
2_Intro
3_Loading
4_PPE_Room
5_MixerRoom
6_InsideMixer
```

Scene names used in Unity, code, and documentation must stay consistent with Build Settings. If a scene name changes, update this document, scene-name constants, and any serialized scene references together.

Primary project-owned locations:

- Runtime scripts: `Assets/Scripts`
- Editor tools and validation harnesses: `Assets/Editor`
- Project scenes: `Assets/Scenes`
- Imported samples and package examples should be treated as third-party unless the task explicitly targets them.

## 3. Top Priority Rules

Codex must follow these rules before making risky or project-shaping code changes. For tiny, obvious fixes, Codex may act directly after checking the relevant local context.

1. Inspect before editing scene, prefab, XR input, UI, shader, coroutine, or flow behavior.
2. For low-risk single-line fixes, inspect only the directly relevant file or serialized value.
3. Keep scripts small and role-based.
4. Do not create strong dependencies between scenes.
5. Do not place every feature inside one large `GameManager`.
6. Do not overwrite Inspector values that were manually adjusted.
7. Consider Unity Play Mode behavior, not only code syntax.
8. Treat coroutines as a possible source of flow bugs.
9. Make changes in small, testable steps.
10. Prefer simple code that the project owner can read later.

## 4. Required Codex Work Process

When Codex receives a modification request, it must follow this order.

### Step 1. Inspect First

Codex should inspect:

- Related scripts
- Related prefabs
- Scene objects
- Inspector-exposed fields
- Existing naming patterns
- Existing scene flow
- Current runtime assumptions

Codex should not assume that a script is safe to modify just because the code compiles.

### Step 2. Explain the Intended Change Briefly

Before editing, Codex should state:

- Which file or object will be changed
- Why the change is needed
- What runtime behavior may be affected
- Whether the change may affect other scenes

For very small fixes, this can be short. For scene-flow, prefab, coroutine, XR input, or state-management changes, this must be explicit.

### Step 3. Make a Small Change

Codex should avoid large mixed changes.

Good change unit:

- Add one controller
- Fix one interaction
- Add one scene transition
- Add one null guard
- Split one oversized script

Bad change unit:

- Rewrite the whole scene flow
- Add UI, input, audio, state, animation, and scene transition in one pass
- Refactor unrelated scripts while fixing one bug

### Step 4. Verify Runtime Risk

After editing, Codex should check:

- Compile risk
- Null-reference risk
- Inspector-link risk
- Coroutine duplicate-run risk
- Scene-transition risk
- Prefab side-effect risk
- XR input conflict risk
- Manual Inspector value overwrite risk

If Unity cannot be run directly, Codex must still reason through these runtime risks and clearly say what could not be tested.

## 5. Architecture Principles

### 5.1 Simple Code, Low Dependency

Code should be easy and simple, but not tightly coupled.

The project owner is still learning Unity, so avoid unnecessary advanced architecture. However, simple code does not mean tangled code.

Preferred style:

- Clear class names
- Clear method names
- Small scripts
- Inspector-assigned references
- Short comments only where useful
- Direct, readable control flow

Avoid:

- Overly abstract frameworks
- Unnecessary generic systems
- Hidden reflection-based logic
- Abbreviated variable names
- One script controlling many unrelated systems

Example naming:

```csharp
private GasMeasureController gasMeasureController;
private PPEChecklistController ppeChecklistController;
private ScenarioRoute selectedScenarioRoute;
```

Avoid:

```csharp
private GasMeasureController gm;
private PPEChecklistController ctrl;
private ScenarioRoute tmp;
```

### 5.2 One Script, One Main Role

Each script should have one clear responsibility.

Recommended examples:

| Script | Responsibility |
| --- | --- |
| `SceneFlowController` | Scene transition requests |
| `TrainingFlowController` | Current scene training flow |
| `PPEEquipController` | PPE equip interaction |
| `PPEChecklistController` | PPE checklist state |
| `TabletUIController` | Tablet UI display |
| `GasMeasureController` | Gas detector behavior |
| `NarrationPlayer` | Voice playback |
| `VRInputReader` | XR input reading |
| `StepState` | Current step and completion state |

Do not combine scene movement, narration, UI, object grabbing, scoring, and route branching in one script.

### 5.3 Avoid Oversized Managers

`GameManager` must not become the place where every feature lives.

Allowed responsibilities:

- Shared app-level state
- Selected scenario route
- Current global training state
- Common configuration

Not allowed:

- PPE equip logic
- Gas detector animation
- Tablet UI logic
- Narration timing
- Individual object interaction
- Detailed scene step progression

The manager should coordinate, not perform every action.

## 6. Scene Dependency Rules

### 6.1 No Direct Scene-to-Scene Script Dependency

A script in `PPE Room` must not directly control a script in `Mixer Building`.

A script in `Mixer Interior` must not directly reference `Title` or `Intro` scene objects.

Scene-specific scripts should communicate through:

- Shared state data
- Scene flow controller
- ScriptableObject configuration, only when useful
- Simple route/state objects
- Scene load parameters where appropriate

Do not create cross-scene object references unless the scene is intentionally loaded additively and the ownership is documented.

### 6.2 Scene Names in One Place

Do not scatter raw scene-name strings across the code.

Preferred:

```csharp
public static class SceneNames
{
    public const string App = "0_App";
    public const string Title = "1_Title";
    public const string Intro = "2_Intro";
    public const string Loading = "3_Loading";
    public const string PPERoom = "4_PPE_Room";
    public const string MixerRoom = "5_MixerRoom";
    public const string InsideMixer = "6_InsideMixer";
}
```

Avoid:

```csharp
SceneManager.LoadScene("PPE Room");
SceneManager.LoadScene("Mixer Building");
SceneManager.LoadScene("Mixer Interior");
```

repeated in many scripts.

Do not guess display names as scene names. Check `ProjectSettings/EditorBuildSettings.asset` or the scene asset path when uncertain.

## 7. Inspector Value Protection

Unity Inspector values often contain important manual adjustments.

Codex must not casually overwrite:

- Position
- Rotation
- Scale
- Audio clips
- Materials
- Prefab references
- UI object references
- XR controller references
- Collider sizes
- Animation references
- Manually tuned timing values

Preferred pattern:

```csharp
[SerializeField] private Transform targetPoint;
[SerializeField] private float moveDuration = 1.0f;
```

Codex should not force runtime values unless the feature truly requires it.

Avoid:

```csharp
transform.position = new Vector3(0, 0, 0);
transform.localScale = Vector3.one;
```

unless the purpose is clear and safe.

### 7.1 Play Mode Value Warning

Values changed during Play Mode are not automatically saved as real project values.

Codex should avoid workflows where the user tunes a value in Play Mode and assumes it is permanently applied.

If a runtime value needs to become final, Codex should guide the user to apply it manually in Edit Mode or create a safe serialized configuration field.

## 8. Prefab Rules

Before modifying a prefab, Codex must check:

- Is this prefab used in other scenes?
- Is it a shared prefab?
- Is this change only needed in one scene?
- Would a prefab variant be safer?
- Would a scene-local object be safer?

For scene-specific behavior, prefer:

- Scene object override
- Prefab Variant
- Scene-specific controller

Do not modify a shared prefab just to solve a one-scene problem unless that is intentionally the correct scope.

## 8.1 Unity Asset Safety Rules

Unity asset identity matters. Codex must preserve `.meta` files and GUIDs.

Do not manually edit generated or editor-owned folders:

- `Library`
- `Temp`
- `Logs`
- `UserSettings`

Do not treat generated `.csproj`, `.sln`, or `.slnx` files as authoritative project configuration.

When changing Unity YAML scene or prefab files directly:

- Keep changes small.
- Verify file IDs, prefab target IDs, GUIDs, and serialized object references.
- Prefer Unity Editor APIs for complex prefab-instance changes.
- Do not reorder or regenerate large scene sections unless Unity itself performs the save.

Imported packages, samples, and downloaded model assets should be left alone unless the task explicitly targets them. Prefer project-owned wrapper scripts under `Assets/Scripts` and editor tools under `Assets/Editor`.

## 9. XR Input Rules

XR input must be separated from feature behavior.

Input scripts read controller actions. Feature scripts decide what to do with those actions.

Recommended separation:

| Layer | Example |
| --- | --- |
| Input read | `VRInputReader` |
| Grab handling | `GrabInteractionController` |
| PPE behavior | `PPEEquipController` |
| UI behavior | `TabletUIController` |
| Flow decision | `TrainingFlowController` |

Project input policy:

| Input | Meaning |
| --- | --- |
| Trigger | Select / interact |
| Grip | Grab / release toggle |
| B button | Confirm / next |
| A button | Discard, only where the scene uses discard |
| Poke | Checkbox or direct UI touch |

In XRI scenes, Inspector-authored `InputActionReference` values on the active interactor are authoritative. Do not assume the left and right hands are correct just because the prefab name is correct.

For controller ray interactions, check the whole path:

- Active `NearFarInteractor` or ray interactor
- Handedness
- `Select`, `Activate`, and `UI Press` action references
- `Allow Hovered Activate` when hover-only activation is expected
- Interaction layers
- Actual ray caster distance, not only visual line length
- XR Interaction Manager and Input Action Manager

For teleport-style interactions, normally use one `TeleportationProvider` per active XR Origin/locomotion stack and many destination targets. Multiple teleport targets are normal; multiple providers are only needed when there are intentionally separate locomotion stacks.

Do not mix raw controller input checks directly into every feature script.

## 10. UI and Feature Separation

UI scripts should display state and send user requests.

UI scripts should not own the training logic.

Example:

```text
Tablet checkbox pressed
PPEChecklistController updates checklist state
TabletUIController refreshes display
TrainingFlowController decides whether the next step is available
```

Avoid:

```text
Tablet button directly equips PPE
Tablet button directly starts narration
Tablet button directly loads next scene
```

unless it is a very small and isolated prototype action.

### 10.1 UI Authoring Source of Truth

For UI, scene and prefab serialized values are the source of truth.

Runtime code must not silently replace authored UI presentation values such as:

- Position, size, anchors, pivot, scale, and spacing
- Colors, materials, fonts, font sizes, and labels
- Sorting order and canvas setup
- Serialized references

Runtime code may change UI state or dynamic content, but authored presentation should stay in the Inspector, scene, prefab, or a serialized configuration object.

When changing UI, inspect both the serialized UI objects and the runtime code path that may overwrite them in `Awake`, `OnEnable`, `Start`, scene-load callbacks, initialization helpers, cloned prefabs, layout components, and animations.

## 11. Object Reference Rules

Avoid overusing:

```csharp
GameObject.Find()
FindObjectOfType()
FindObjectsOfType()
```

Preferred:

- `[SerializeField]` Inspector references
- Explicit setup during scene initialization
- Small controller references
- Clear null checks

Runtime object search can break easily when:

- An object is renamed
- An object is inactive
- Multiple objects of the same type exist
- Scene loading order changes
- Prefab instances duplicate

## 12. Null Check and Logging Rules

Important references should be checked in `Awake()` or `Start()`.

Example:

```csharp
private void Awake()
{
    if (tabletUIController == null)
    {
        Debug.LogWarning($"{nameof(tabletUIController)} is not assigned.", this);
    }
}
```

Logging rules:

- Use warnings for missing setup.
- Use errors only when the feature cannot continue.
- Do not spam logs every frame.
- Include the object context when useful.

## 13. Defensive Code and Failure Handling Rules

Defensive code is useful, but too much defensive code can hide real problems.

Codex must not add guards that silently ignore broken setup, failed runtime state, or incorrect flow. A guard should make the failure safer and easier to find, not make the project appear to work while the feature is actually skipped.

### 13.1 Do Not Hide Broken Setup

Avoid silent returns for required references.

Risky pattern:

```csharp
if (gasMeasureController == null)
{
    return;
}
```

This prevents a crash, but it also hides why the gas measurement step never runs.

Preferred pattern:

```csharp
if (gasMeasureController == null)
{
    Debug.LogError($"{nameof(gasMeasureController)} is not assigned.", this);
    return;
}
```

If a missing reference means the feature cannot continue, log it as an error with the object context.

### 13.2 Use Guards Only When the Result Is Intentional

A guard is acceptable when skipping the action is a correct runtime result.

Acceptable examples:

- Ignore input while the step is locked.
- Ignore double-click or repeated button press.
- Ignore equip action if the item is already equipped.
- Stop interaction after the scene is leaving.

Unacceptable examples:

- Skip a required training step without warning.
- Ignore missing UI that should exist.
- Ignore missing narration required for the step.
- Ignore a failed state transition.
- Catch an exception and continue as if the feature worked.

### 13.3 Prefer Clear Failure Over Silent Failure

During development, it is better to expose a wrong setup clearly than to bury it.

Preferred development behavior:

- Log missing required references.
- Log invalid step transitions.
- Log impossible route states.
- Fail the current feature visibly when continuing would corrupt the flow.

Avoid:

- Empty `catch` blocks.
- Repeated `if (x == null) return;` without context.
- Returning from the middle of a step without updating state.
- Marking a step complete when its required action failed.

### 13.4 Do Not Overbuild Validation

Do not add large validation systems before the project needs them.

Preferred:

- Simple null check for required references.
- Simple state check for current route or step.
- Clear warning or error when setup is wrong.
- Small debug helper for scene validation when useful.

Avoid:

- Complex generic validator frameworks.
- Reflection-based auto validation.
- Many fallback paths that make the actual intended setup unclear.
- Code that tries to repair scene hierarchy automatically without user approval.

Explicit editor-only setup and validation tools are allowed when they are named, menu-driven, and scoped to one known scene or asset family. Examples include project-owned harnesses under `Assets/Editor`, such as PPE room validation and setup tools. These tools may create or repair scene setup only when deliberately invoked, not silently at runtime.

### 13.5 Defensive Code Must Not Change Manual Scene Setup

Defensive code must not create, move, resize, replace, or reconnect scene objects automatically unless that behavior is explicitly requested.

Do not solve missing setup by secretly creating new objects at runtime if the correct fix is assigning the object in the Inspector.

### 13.6 Every Guard Should Answer Three Questions

Before adding a guard, Codex should check:

- What failure does this guard prevent?
- What should the user or developer see when it happens?
- Is skipping the action safer than stopping the current feature?

If the answer is unclear, do not add the guard yet.

## 14. Coroutine and Flow Rules

This project is likely to use many coroutines because VR scenes include timed narration, UI display, object interaction, route branching, animation, and waiting conditions.

Coroutines are useful, but they can easily create hidden bugs when many interactions happen at once.

### 14.1 Flow Coroutines Belong to Flow Controllers

Major scene-flow coroutines should be started and controlled by a flow or step controller.

Allowed:

```text
PPEFlowController starts PPE room route flow
MixerFlowController starts gas-measurement flow
NarrationPlayer starts only narration playback
```

Avoid:

```text
Glove object starts next scene coroutine
Tablet checkbox starts full PPE route coroutine
Gas detector object directly moves to final scene
```

Individual interaction scripts should usually send completion signals only.

### 14.2 Do Not Start the Same Coroutine Repeatedly

Important coroutines must have duplicate-run protection.

Example:

```csharp
private bool isRunning;

public void StartTraining()
{
    if (isRunning)
    {
        return;
    }

    StartCoroutine(RunTraining());
}

private IEnumerator RunTraining()
{
    isRunning = true;

    yield return RunIntroStep();
    yield return WaitForPPEEquipStep();
    yield return RunConfirmStep();

    isRunning = false;
}
```

If the coroutine can end early, make sure `isRunning` is reset safely.

### 14.3 Prefer Coroutine Handles Over StopAllCoroutines

`StopAllCoroutines()` can accidentally stop unrelated routines running on the same component.

Avoid it unless the script owns only one simple routine and the risk is clear.

Preferred:

```csharp
private Coroutine currentFlow;

public void StartFlow()
{
    if (currentFlow != null)
    {
        StopCoroutine(currentFlow);
    }

    currentFlow = StartCoroutine(RunFlow());
}
```

### 14.4 Wait for Real Conditions, Not Only Time

Do not rely only on `WaitForSeconds()` when the next step depends on user action.

Preferred:

```csharp
yield return new WaitUntil(() => ppeState.IsGloveEquipped);
```

Use `WaitForSeconds()` for timing, but use state checks for completion.

Examples of real completion conditions:

- PPE item equipped
- Checklist item confirmed
- Narration finished
- Gas detector inserted
- Gas measurement completed
- B button confirmed
- User selected route

### 14.5 Separate Narration Timing From Feature Logic

Narration should be handled by `NarrationPlayer`.

Training flow can choose whether to wait for narration or allow interaction during narration.

Example:

```csharp
yield return narrationPlayer.PlayAndWait("PPE_Intro");
```

or:

```csharp
narrationPlayer.Play("PPE_Intro");
EnablePPEInteraction();
```

These are different behaviors and must be intentional.

### 14.6 Route Flows Should Be Separated

Do not place the entire education route and accident route inside one giant coroutine with many nested `if` statements.

Preferred:

```csharp
private IEnumerator RunSelectedRoute()
{
    if (selectedRoute == ScenarioRoute.Education)
    {
        yield return RunEducationRoute();
    }
    else if (selectedRoute == ScenarioRoute.Accident)
    {
        yield return RunAccidentRoute();
    }
}
```

Common steps can be extracted into small shared methods.

### 14.7 Every Major Coroutine Needs a Clear Contract

Before adding a coroutine, Codex should identify:

- Who starts it?
- What condition ends it?
- Can it run more than once?
- Can it be cancelled?
- What happens if the object is disabled?
- What happens if the scene changes?
- What state does it modify?
- What UI or narration does it depend on?

If these answers are unclear, do not add the coroutine yet.

## 15. Step-Based Scene Flow

Scene flow should be divided into named steps.

Example PPE room steps:

```text
EnterRoom
ControllerTutorial
ScenarioSelect
PPECardCheck
PPEEquip
MirrorCheck
FinalConfirm
MoveToMixerBuilding
```

Example mixer-building steps:

```text
EnterMixerArea
CheckWorkDocument
ConfirmPowerOff
InstallVentilationDuct
WaitVentilation
MeasureGasFirst
MeasureGasSecond
ReportToWatcher
MoveToMixerInterior
```

The current step should be visible in code, logs, or debug UI when needed. If a bug happens, the developer should be able to know where the flow stopped.

## 16. Runtime Result Review

Codex must consider Unity runtime behavior, not only whether the code looks correct.

Before and after a change, Codex should check these risks:

| Runtime Risk | Question |
| --- | --- |
| Null reference | Are all Inspector references assigned? |
| Inactive object | Is the target object active when called? |
| Scene load order | Does this run before the object exists? |
| Coroutine conflict | Can this start twice or stop too early? |
| Input conflict | Can Trigger, Grip, A, B, or Poke overlap unexpectedly? |
| Prefab side effect | Does this change affect other scenes? |
| Manual value overwrite | Does code overwrite hand-tuned Inspector values? |
| Route regression | Does education route or accident route break? |
| UI state mismatch | Does UI show a state different from actual training state? |
| Audio timing mismatch | Does narration block or overlap interaction incorrectly? |

Compile success is not enough.

Minimum verification target:

- Scene can be entered.
- Main UI appears.
- Controller input works.
- Object interaction works.
- Current step can progress.
- Double input does not break the flow.
- Console has no new errors.

If full Unity Play Mode testing is not available, Codex must say so and provide a runtime-risk checklist instead.

## 17. Feature Implementation Order

Implement features in small stages.

Recommended order:

1. Scene entry
2. UI display
3. Input detection
4. One object interaction
5. State update
6. Checklist or visual feedback
7. Narration
8. Step transition
9. Route branching
10. Error handling

Do not build the whole training sequence at once.

## 18. Test and Debug Features

Development-only shortcuts are allowed.

Examples:

- Jump directly to PPE Room
- Jump directly to Mixer Building
- Mark one PPE item as equipped
- Complete current step
- Reset current step
- Print current route and step

Rules:

- Test features must be clearly marked as development-only.
- Test features must not be mixed with final training logic.
- Test features must be removed, hidden, or disabled in release builds.

Suggested guard:

```csharp
#if UNITY_EDITOR
// Development-only test button or shortcut.
#endif
```

## 19. Comments

Use comments only when they help the beginner developer understand why something exists.

Good comment:

```csharp
// Keep the Inspector position. This object is manually aligned in the VR scene.
```

Bad comment:

```csharp
// Set the variable to true.
```

## 19.1 Generated Scene Content and XR Shader Rules

`[ExecuteAlways]`, `OnEnable`, and `OnValidate` code must not delete and recreate scene-authored geometry or overwrite child transforms automatically.

Procedural rebuilds should be explicit editor commands or context-menu actions. After generation, the serialized scene hierarchy and Inspector values are authoritative.

For project-owned hand-written shaders used by XR-visible scene geometry, hands, or world-space UI, Meta Quest/OpenXR Single Pass Instanced rendering must be treated as required. Custom shaders should include the necessary Unity stereo instancing macros and should be validated on headset when possible. Game view validation alone is not enough for XR shader correctness.

## 20. What Codex Must Not Do

Codex must not:

- Rewrite unrelated systems.
- Rename scene objects without need.
- Rename public serialized fields casually.
- Break existing Inspector references.
- Add complex architecture without explaining why.
- Add excessive defensive code that silently hides real setup or flow problems.
- Hide important behavior inside clever helper code.
- Use `StopAllCoroutines()` casually.
- Put many unrelated features into one script.
- Change shared prefabs without checking scope.
- Assume compile success means runtime success.
- Ignore scene state, inactive objects, or Play Mode behavior.
- Overwrite user-tuned transforms, materials, timings, or references.

## 21. Recommended Baseline Folder Structure

Follow the existing project structure first.

```text
Assets/
  Scripts/
    Common/
    Flow/
    Input/
    UI/
    PPE/
    Mixer/
    Narration/
    Debug/
  Editor/
  Prefabs/
  Scenes/
```

This is a grouping guideline, not a command to reorganize files. Do not create or move folders automatically unless the user asks or the project is ready for it.

## 22. Recommended Baseline Script Groups

| Group | Purpose |
| --- | --- |
| `Common` | Shared constants and small utilities |
| `Flow` | Scene and step progression |
| `Input` | XR input reading |
| `UI` | Tablet, tutorial, checklist, prompts |
| `PPE` | PPE equip and validation |
| `Mixer` | Mixer-area and mixer-interior interactions |
| `Narration` | Voice playback |
| `Debug` | Editor-only test helpers |

## 23. Codex Response Style for This Project

When working on this project, Codex should keep responses practical.

For code tasks, Codex should usually provide:

1. What was inspected
2. What was changed
3. Runtime risk considered
4. What was tested or could not be tested
5. Next safe step

For planning tasks, Codex should provide:

1. Recommended structure
2. Why it is safer
3. Simple implementation order
4. Beginner-friendly cautions

## 24. Core Rule Summary

Use this short summary when a Codex environment needs a compact instruction.

```text
This Unity VR project must prioritize beginner-readable code, low dependency, small scripts, Inspector value protection, and runtime-safe behavior.

Codex must inspect before editing scene, prefab, XR input, UI, shader, coroutine, or flow behavior. Do not directly couple scripts across scenes. Do not put all logic into GameManager. Do not overwrite manually adjusted Inspector values. Separate XR input, UI, narration, interaction, state, and scene flow.

Unity `.meta` files and GUIDs must be preserved. Do not manually edit generated folders such as Library, Temp, Logs, or UserSettings. Prefer project-owned runtime code in Assets/Scripts and editor-only setup or validation tools in Assets/Editor.

In XRI scenes, Inspector-authored InputActionReference, interactor handedness, Allow Hovered Activate, interaction layers, and actual ray caster distance are authoritative. Teleport scenes normally use one TeleportationProvider per active XR Origin/locomotion stack and many destination targets.

Defensive code must make failures visible, not silently hide them. Avoid excessive guards, empty catch blocks, runtime scene repair, and silent returns that skip required training steps. Explicit editor-only setup and validation harnesses are allowed when deliberately invoked.

Major coroutines must be managed by Flow/Step Controllers, not by individual object scripts. Every coroutine needs a clear owner, end condition, duplicate-run protection, and safe cancellation rule. Avoid StopAllCoroutines.

Codex must evaluate Unity Play Mode behavior, scene object references, prefab side effects, inactive objects, coroutine timing, XR input conflicts, generated scene content, XR shader stereo compatibility, and route regressions before deciding that a change is correct.
```

## 25. 중단된 Codex 세션 복구 하네스

업데이트, 앱 종료 또는 창 종료 뒤 기존 작업 세션을 찾아야 하면 추측하거나 바로 `찾을 수 없음`으로 답하지 않는다. 저장소 루트에서 다음 순서로 확인한다.

1. `node Tools/CodexSessionRecoveryHarness.mjs`를 실행한다.
2. 하네스가 현재 `CODEX_SESSION_ID`와 `CODEX_THREAD_ID`를 제외하고, 현재 저장소와 `session_meta.payload.cwd`가 같은 최신 세션을 선택했는지 확인한다.
3. 후보가 여러 개면 `node Tools/CodexSessionRecoveryHarness.mjs --limit 5`로 이름, 갱신 시각과 중단 상태를 비교한다.
4. 현재 터미널 재개는 `--resume`, Windows 새 창 재개는 `--open`으로 실행한다.
5. 하네스가 실패한 경우에만 `$CODEX_HOME`, 세션 디렉터리와 작업 경로 불일치를 조사한다.

하네스 자체 회귀 검증은 `node Tools/CodexSessionRecoveryHarness.mjs --self-test`로 실행한다. 실제 사용자 세션 파일, 대화 원문, 고정 세션 ID는 저장소에 추가하지 않는다.
