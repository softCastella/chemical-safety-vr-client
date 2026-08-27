# 버그 리포트: BackgroundGradient Shader Graph 자동 생성

| 항목 | 내용 |
|------|------|
| **날짜** | 2026-06-25 |
| **프로젝트** | 3D_UI_Test |
| **Unity** | 6000.4.8f1 |
| **URP** | 17.4.0 |
| **대상 에셋** | `Assets/Shaders/BackgroundGradient.shadergraph` |
| **관련 스크립트** | `Assets/Editor/BackgroundGradientShaderGenerator.cs` |
| **관련 도구** | `Tools/generate_background_shadergraph.ps1` (사용 중단 권장) |
| **상태** | **생성기 동작 확인** (2026-06-26) — shader + material 생성 성공, 씬 연결 및 시각 검증 완료 |

---

## 요약

URP Unlit 세로 그라데이션 배경용 Shader Graph를 자동 생성하려 했으나, PowerShell JSON 생성 방식이 Unity import에 실패했고, 이후 C# Editor 생성기에서도 Shader Graph / URP 내부 API 접근 방식 문제로 연쇄 오류가 발생했다. **2026-06-26 기준 reflection 생성기로 shader + material 생성까지 성공**했으며, 씬 `BG_Gradient` 미연결 문제도 수정했다.

---

## 기대 동작

- Quad에 적용 가능한 URP Unlit Opaque 세로 그라데이션 셰이더
- Material 속성: Top Color, Bottom Color, Flip Gradient, Gradient Power
- `Assets/Shaders/BackgroundGradient.shadergraph` 및 `.mat` 자동 생성

---

## 실제 동작

1. PowerShell로 생성한 `.shadergraph` → Unity import 시 `JSON parse error: Invalid value` 반복
2. C# 직접 API 사용 시도 → `CS0122` (internal 타입 접근 불가)
3. Reflection 기반 생성기 도입 후에도 런타임에서 타입/배열/생성자/optional parameter 관련 예외 연속 발생
4. 생성 성공 후에도 **화면에 변화 없음** — 씬 `BG_Gradient` 비활성 + 잘못된 머티리얼 참조 (에셋만 생성되고 씬 미연결)

---

## 재현 절차

### A. PowerShell JSON 생성 (실패)

1. `Tools/generate_background_shadergraph.ps1` 실행
2. `Assets/Shaders/BackgroundGradient.shadergraph` 생성
3. Unity에서 프로젝트 열기 또는 Reimport
4. Console에 JSON parse 오류 출력

### B. Editor 메뉴 생성 (부분 성공 → 연쇄 오류)

1. Unity Editor에서 프로젝트 열기
2. `Tools → Generate Background Gradient Shader` 실행 (또는 스크립트 자동 생성 대기)
3. Console에서 아래 오류들이 순차적으로 발생 (수정 전 기준)

---

## 발견된 이슈 목록

### BUG-001: PowerShell `ConvertTo-Json` ↔ Unity `JsonUtility` 비호환

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `ArgumentException: JSON parse error: Invalid value` |
| **위치** | `MultiJsonInternal.Parse` → `ShaderGraphImporter` |
| **원인** | PowerShell이 출력한 MultiJson 형식이 Unity `JsonUtility` 규칙과 맞지 않음 |
| **부가 원인** | PS에서 `NodeBase ... -1200 200` 호출 시 `-1200`이 스위치로 해석되어 `"x": "-1200"` **문자열**로 직렬화됨 (float 필드에 문자열 → parse 실패) |
| **조치** | PS 스크립트 음수 좌표 `(-1200)` 수정했으나 import仍 실패. **PS 방식 폐기**, Unity API 생성으로 전환 |
| **상태** | 해결 방향 확정 (PS 미사용) |

---

### BUG-002: Shader Graph Editor 타입 `internal` — CS0122

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `GraphData`, `PropertyNode`, `PositionNode` 등 `CS0122: inaccessible due to protection level` |
| **원인** | `Unity.ShaderGraph.Editor` 어셈블리의 핵심 타입이 `internal` |
| **조치** | Reflection + `Activator.CreateInstance(type, nonPublic: true)` 로 전환 |
| **상태** | 해결 |

---

### BUG-003: `MultiJsonInternal` 직접 참조 — CS0122

| | |
|---|---|
| **심각도** | 중간 |
| **증상** | `MultiJsonInternal is inaccessible due to its protection level` |
| **원인** | 검증용으로 internal API 직접 `using` |
| **조치** | `MultiJsonInternal.Parse` 제거, import 성공 여부(`LoadAssetAtPath<Shader>`)로 검증 |
| **상태** | 해결 |

---

### BUG-004: URP 타입 어셈블리 오류

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `ArgumentNullException: Value cannot be null. Parameter name: type` (line 115) |
| **원인** | `UniversalTarget` / `UniversalUnlitSubTarget`을 `Unity.ShaderGraph.Editor`에서 `GetType` → **null** (실제 위치: `Unity.RenderPipelines.Universal.Editor`) |
| **조치** | `UniversalEditorAsm` 분리, `RequireType()` 헬퍼 추가 |
| **상태** | 해결 |

---

### BUG-005: Reflection 배열 타입 불일치

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `Object of type 'System.Object[]' cannot be converted to type 'UnityEditor.ShaderGraph.Target[]'` |
| **원인** | `InitializeOutputs(Target[], BlockFieldDescriptor[])`에 `new[] { target }` 전달 시 `object[]`로 추론 |
| **조치** | `Array.CreateInstance`로 typed array 생성 |
| **상태** | 해결 |

---

### BUG-006: `DefaultCategory` reflection 파라미터 개수

| | |
|---|---|
| **심각도** | 중간 |
| **증상** | `TargetParameterCountException: Number of parameters specified does not match` |
| **원인** | `DefaultCategory(List<ShaderInput> categoryChildren = null)` — optional이어도 reflection은 `Invoke(null, null)` 불가 |
| **조치** | `new object[] { propertyList }` 명시 전달, 속성 추가 후 카테고리 생성 |
| **상태** | 해결 |

---

### BUG-007: Shader Property internal 생성자

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `MissingMethodException: Default constructor not found for ColorShaderProperty` |
| **원인** | `ColorShaderProperty`, `BooleanShaderProperty`, `Vector1ShaderProperty` 생성자가 `internal` |
| **조치** | `CreateInstance(Type)` → `Activator.CreateInstance(type, nonPublic: true)` 통일 |
| **상태** | 해결 |

---

### BUG-008: Unity Batch Mode 프로젝트 잠금

| | |
|---|---|
| **심각도** | 낮음 (환경 제약) |
| **증상** | `-batchmode -executeMethod` 실행 시 `another Unity instance is running` |
| **원인** | Editor가 동일 프로젝트를 열고 있으면 batch 불가 |
| **조치** | Editor 내 `DidReloadScripts` + `delayCall` 자동 생성으로 대체 |
| **상태** | 우회 |

---

### BUG-009: `AddGraphInput` / `AddNode` optional parameter reflection

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `TargetParameterCountException` at `AddColorProperty` → `Invoke(graph, "AddGraphInput", ...)` |
| **위치** | `BackgroundGradientShaderGenerator.cs:255` |
| **원인** | `AddGraphInput(ShaderInput input, int index = -1)`, `AddNode(AbstractMaterialNode node, bool usePreviewPref = true)` — C# optional parameter 기본값이 reflection `Invoke`에 적용되지 않음 |
| **조치** | `AddGraphInput`에 `-1`, `AddNode`에 `true` 명시 전달 |
| **상태** | 해결 (2026-06-26) |

---

### BUG-010: `ShaderInput` 네임스페이스 오류

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `TypeLoadException: Type not found: UnityEditor.ShaderGraph.ShaderInput` |
| **위치** | `BuildGraph()` → `DefaultCategory`용 `List<ShaderInput>` 생성 시 |
| **원인** | `ShaderInput`은 `UnityEditor.ShaderGraph.Internal`에 정의 (`ColorShaderProperty` 등과 동일) |
| **조치** | `RequireType(..., "UnityEditor.ShaderGraph.Internal.ShaderInput")` 로 수정 |
| **상태** | 해결 (2026-06-26) |

---

### BUG-011: Block 노드 조회 — `serializedDescriptor` 미설정

| | |
|---|---|
| **심각도** | 높음 |
| **증상** | `InvalidOperationException: Block not found: SurfaceDescription.BaseColor` |
| **위치** | `FindBlockNode()` → `BuildGraph()` BaseColor 연결 단계 |
| **원인** | `BlockNode.serializedDescriptor`는 `OnBeforeSerialize()`에서만 설정됨. `InitializeOutputs` 직후에는 비어 있음. `Init()`에서 설정되는 `name` (`tag.name`) 또는 `descriptor`로 찾아야 함 |
| **조치** | `FindBlockNode`를 `fragmentContext`/`vertexContext` blocks 순회 + `name`/`descriptor`/`serializedDescriptor` 다중 매칭으로 개선 |
| **상태** | 해결 (2026-06-26) |

---

### BUG-012: 생성 성공해도 화면 변화 없음 (씬 미연결)

| | |
|---|---|
| **심각도** | 중간 (기능 누락) |
| **증상** | Console에 `Created ... shadergraph and ... mat` 성공 로그만 출력, Scene/Game 뷰 변화 없음 |
| **원인** | ① 씬 `BG_Gradient` 오브젝트가 **비활성** (`m_IsActive: 0`) ② MeshRenderer가 패키지 템플릿 `Unlit Simple.shadergraph` (guid `2476a2d151e824143af40dce1fc93a12`) 참조 ③ 생성기가 에셋만 만들고 씬 오브젝트에 머티리얼 미적용 |
| **영향 씬** | `Confined Space Scene.unity`, `Confined Space Scene 1.unity`, `close change.unity` |
| **조치** | ① 세 씬에서 `BG_Gradient` 활성화 + `BackgroundGradient.mat` (guid `dc0ec594616cd7b42a70d81df3d4c8c7`) 연결 ② 생성기에 `ApplyMaterialToSceneBackgrounds()` 추가 (열린 씬의 `BG_Gradient` 자동 적용) |
| **상태** | 해결 (2026-06-26) |

---

### BUG-013: `ApplyMaterialToSceneBackgrounds` 닫는 괄호 누락

| | |
|---|---|
| **심각도** | 낮음 (컴파일 오류) |
| **증상** | `CS1513: } expected` (line 499) |
| **원인** | `ApplyMaterialToSceneBackgrounds` 메서드 종료 `}` 누락 |
| **조치** | 닫는 괄호 추가 |
| **상태** | 해결 (2026-06-26) |

---

## Newtonsoft.Json 관련 (오해 정리)

- **CS0122 / ArgumentNullException / TargetParameterCountException** → Newtonsoft와 **무관** (C# reflection·어셈블리 문제)
- **JSON parse error** → Unity importer가 **JsonUtility만** 사용. Newtonsoft로 JSON을 만들어도 import 호환성은 보장되지 않음
- 올바른 경로: Unity `FileUtilities.WriteShaderGraphToDisk` 또는 Shader Graph UI 수동 작성

---

## 영향 범위

- 배경 Quad용 그라데이션 셰이더 자동화 지연
- Console 오류로 Shader Graph 창 열기/import 실패 (잘못된 `.shadergraph` 존재 시)
- XR/OpenXR 패키지 관련 별도 Editor 오류 로그 (본 이슈와 무관할 수 있음)

---

## 해결 / 우회 방법

1. **권장**: `BackgroundGradientShaderGenerator.cs` 사용  
   - 메뉴: `Tools → Generate Background Gradient Shader`  
   - 또는 스크립트 컴파일 후 자동 생성 (`DidReloadScripts`)
   - 생성 후 열린 씬의 `BG_Gradient`에 머티리얼 자동 적용
2. **하지 말 것**: `Tools/generate_background_shadergraph.ps1` 재실행
3. import 오류 지속 시: Project에서 깨진 `BackgroundGradient.shadergraph` 삭제 후 생성기 재실행
4. 자동 생성 재시도: `EditorPrefs` 키 `BackgroundGradientShaderGenerator_Generated_v1` 삭제 후 Unity 재컴파일
5. **시각 확인**: Hierarchy에서 `BG_Gradient` 활성 여부 및 Material = `BackgroundGradient` 확인. 씬을 이미 열어둔 상태면 **씬 재로드** 필요할 수 있음

---

## 관련 파일

| 경로 | 설명 |
|------|------|
| `Assets/Editor/BackgroundGradientShaderGenerator.cs` | Reflection 기반 생성기 (현재 방식) |
| `Assets/Shaders/BackgroundGradient.shadergraph` | 생성 대상 |
| `Assets/Shaders/BackgroundGradient.mat` | 기본 머티리얼 (guid: `dc0ec594616cd7b42a70d81df3d4c8c7`) |
| `Assets/Scenes/Confined Space Scene.unity` | `BG_Gradient` Quad 배경 |
| `Assets/Scenes/Confined Space Scene 1.unity` | `BG_Gradient` Quad 배경 |
| `Assets/Scenes/close change.unity` | `BG_Gradient` Quad 배경 |
| `Tools/generate_background_shadergraph.ps1` | 폐기 예정 JSON 생성기 |

---

## 후속 작업

- [x] 생성기 실행 후 Console 오류 없이 shader + material 생성 확인 (2026-06-26)
- [x] Quad에 머티리얼 적용 후 그라데이션 시각 확인 — 씬 `BG_Gradient` 연결 완료 (2026-06-26)
- [ ] Shader Graph에서 노드/속성/연결 정상 여부 육안 검증
- [ ] PS 스크립트 제거 또는 README에 deprecated 명시
- [ ] (선택) 수동 Shader Graph 제작 가이드 `Assets/Docs/`에 별도 문서화

---

## 작성자 메모

Shader Graph `.shadergraph`는 Unity 전용 MultiJson 포맷이라 외부에서 JSON을 “맞춰 쓰기”보다, Editor 내부 API로 쓰는 편이 안전하다. 다만 해당 API 대부분이 `internal`이라 reflection 없이는 `Assembly-CSharp-Editor`에서 접근할 수 없으며, reflection 사용 시 **어셈블리 분리**(ShaderGraph vs URP), **typed array**, **optional parameter**, **nonPublic 생성자**를 각각 처리해야 한다.

### Reflection 시 주의사항 (교훈 정리)

| 패턴 | 예시 | 올바른 처리 |
|------|------|-------------|
| Optional parameter | `AddGraphInput(input, index = -1)` | `Invoke(..., new object[] { input, -1 })` — 기본값 생략 불가 |
| Internal 네임스페이스 | `ShaderInput`, `ColorShaderProperty` | `UnityEditor.ShaderGraph.Internal.*` |
| 직렬화 전 필드 | `BlockNode.serializedDescriptor` | `name` 또는 `descriptor.tag.name`으로 조회 |
| 에셋 vs 씬 | shader/mat 생성 성공 | 씬 오브젝트 활성화 + 머티리얼 참조 별도 확인 필요 |

### 성공 로그 예시 (2026-06-26)

```
Generating BackgroundGradient.shadergraph via Shader Graph API...
Created Assets/Shaders/BackgroundGradient.shadergraph and Assets/Shaders/BackgroundGradient.mat
Applied Assets/Shaders/BackgroundGradient.mat to N BG_Gradient object(s) in open scene(s).
```
