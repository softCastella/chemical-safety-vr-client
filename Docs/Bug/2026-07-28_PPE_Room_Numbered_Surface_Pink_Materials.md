# 2026-07-28 PPE룸 복제 면 핑크 머테리얼 회귀

## 기본 정보

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 대상 오브젝트 | `Rear Wall (1)`, `Floor (1)`, `Ceiling (1)` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR |
| 심각도 | P1 렌더링 회귀 |
| 상태 | 코드 수정 및 자동 검증 하네스 추가, Scene/Game View 육안 확인 필요 |

## 증상

문 개구부 너머의 공간을 구성하기 위해 추가된 다음 세 면이 선홍색으로 렌더링됐다.

- `PPE Background Room/Generated Image Room/Rear Wall (1)`
- `PPE Background Room/Generated Image Room/Floor (1)`
- `PPE Background Room/Generated Image Room/Ceiling (1)`

세 오브젝트의 `MeshRenderer.m_Materials`는 씬 YAML에서 `{fileID: 0}`으로 직렬화돼 있었다.

## 재현 절차

1. `Assets/Scenes/3_PPE_Room.unity`를 연다.
2. 스크립트 도메인 리로드 또는 Play Mode 진입으로 `PPEBackgroundRoom.OnEnable`을 실행한다.
3. `Generated Image Room` 아래의 원본 면과 `(1)` 면을 비교한다.
4. 원본은 복구된 방 머테리얼을 사용하지만 `(1)` 면에는 유효한 머테리얼이 할당되지 않아 핑크색으로 표시되는 것을 확인한다.

## 원인

`PPEBackgroundRoom`은 벽·바닥·천장 머테리얼을 런타임에 생성하며 `HideFlags.DontSave`를 사용한다. 따라서 해당 임시 머테리얼은 씬 파일에 영구 GUID로 저장되지 않고, 씬을 열 때 `OnEnable -> RefreshRoomMaterials`가 기존 Renderer에 다시 할당한다.

기존 복구 코드는 다음과 같이 정확한 이름 하나만 찾았다.

```csharp
Transform child = root.Find(objectName);
```

이 때문에 `Rear Wall`, `Floor`, `Ceiling`은 복구됐지만 문 개구부 작업 중 씬에 추가된 `Rear Wall (1)`, `Floor (1)`, `Ceiling (1)`은 복구 대상에서 빠졌다. `{fileID: 0}` 자체보다 실제 원인은 복제 면과 복구 코드 사이의 이름 범위 불일치였다.

## 수정

`Assets/Scripts/PPEBackgroundRoom.cs`의 머테리얼 할당 경로를 변경했다.

- `Generated Image Room`의 직계 자식만 순회한다.
- 정확한 원본 이름과 Unity 번호 접미사 형식인 `이름 (n)`을 같은 표면 그룹으로 취급한다.
- `n`은 1 이상의 정수만 허용한다.
- 기존 Renderer에 머테리얼만 재할당한다.
- Mesh, 계층, 위치, 회전, 스케일은 변경하거나 재생성하지 않는다.

현재 수정으로 다음 연결이 보장된다.

| 원본 | 연장 면 | 복구 머테리얼 |
| --- | --- | --- |
| `Rear Wall` | `Rear Wall (1)` | `PPE Wall` |
| `Floor` | `Floor (1)` | `PPE Floor` |
| `Ceiling` | `Ceiling (1)` | `PPE Ceiling` |

## 회귀 검증 하네스

`Assets/Editor/PPERoomMaterialRecoveryHarness.cs`를 추가했다.

검사 항목:

1. PPE 씬과 `PPE Background Room/Generated Image Room` 존재 여부
2. 원본 및 `(1)` 면의 `MeshRenderer` 존재 여부
3. `sharedMaterial` 누락 여부
4. 셰이더 누락 및 `Hidden/InternalErrorShader` 사용 여부
5. 각 `(1)` 면이 해당 원본과 동일한 복구 머테리얼을 공유하는지 여부

Unity 메뉴 실행:

```text
Tools > PPE > Validate Room Surface Materials
```

배치 실행:

```powershell
Unity.exe -batchmode -quit -projectPath <PROJECT_PATH> `
  -executeMethod PPERoomMaterialRecoveryHarness.Validate `
  -logFile Logs/PPERoomMaterialRecoveryHarness.log
```

검사 실패 시 상세 실패 항목을 로그에 남기고 `InvalidOperationException`을 발생시켜 배치 실행이 실패하도록 한다. 검사 자체는 씬을 저장하거나 표면 Transform을 변경하지 않는다.

## 검증 결과

- `Assembly-CSharp.csproj` 빌드: 오류 0개
- 수정 파일 `git diff --check`: 공백 오류 없음
- Unity Editor 하네스 실행: 실행 중인 에디터가 다른 씬을 열고 있어 아직 미실행
- Scene/Game View 및 Quest/OpenXR 육안 확인: 필요

## 재발 방지 규칙

- 씬 YAML의 `{fileID: 0}`만 보고 머테리얼 손상으로 단정하지 않는다. `PPEBackgroundRoom.OnEnable` 이후의 live Renderer를 확인한다.
- `Rear Wall (n)`, `Floor (n)`, `Ceiling (n)` 같은 씬 작성 연장 면은 원본과 같은 복구 그룹에 포함한다.
- 머테리얼 누락을 고치기 위해 `Rebuild Room`을 실행하지 않는다. 씬에 작성된 Transform과 문 개구부 배치를 보존한다.
- PPE룸 머테리얼 복구 변경 후 반드시 회귀 검증 하네스를 실행한다.

## 관련 파일

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/Editor/PPERoomMaterialRecoveryHarness.cs`
- `AGENTS.md`

## 2026-08-11 추가: `PPE_B_Bench` 밝기 보정 누락

### 적용한 변경

- `Assets/Scripts/PPEBackgroundRoom.cs`의 벤치 이름 판정을 `MatchesBenchName`으로 분리했다.
- 현재 기준 이름인 `PPE_B_Bench`, 번호가 붙는 `PPE_B_Bench_...`, 기존 이름인
  `ppe_room_bench...`를 같은 밝기 보정 대상으로 처리한다.
- 씬의 벤치 Transform, Renderer, 머티리얼, 텍스처 및 Inspector의 `benchColor`,
  `roomBrightness` 값은 변경하지 않았다.
- `PPERoomMaterialRecoveryHarness`에
  `Tools > PPE > Validate Bench Brightness Name Routing` 검사를 추가했다.

### 근본 원인

2026-08-11 이름 마이그레이션에서 씬 오브젝트가 `PPE_B_Bench_01`에서
`PPE_B_Bench`로 변경됐다. 그러나 `ApplyBenchColor`의 이름 필터는
`PPE_B_Bench_` 접두사만 허용해 현재 이름을 제외했다. 그 결과
`benchColor * roomBrightness`를 담은 `MaterialPropertyBlock`이 적용되지 않았고,
어두운 원본 텍스처와 머티리얼 Base Color만 표시되어 벤치가 검게 보였다.

### 영향 범위

- 변경 대상은 `PPEBackgroundRoom.ApplyBenchColor`의 벤치 선택 조건뿐이다.
- 방 표면 머티리얼 복구, Duct 색상, Transform, 입력, PPE Grab, UI, 텔레포트 및
  오디오는 변경하지 않는다.
- 기존 `ppe_room_bench...`와 `PPE_B_Bench_...` 이름도 계속 지원한다.

### 완료한 검증

- `_0` 씬의 현재 벤치 이름이 `interiorObjects/Bg/PPE_B_Bench`임을 확인했다.
- 벤치 머티리얼, URP 셰이더 및 베이스컬러 텍스처 GUID가 유효함을 확인했다.
- 이름 검증 하네스가 현재 이름, 번호 접미사 이름, 기존 이름 및 비대상 유사 이름을
  서로 구분하도록 추가했다.
- Unity Editor에서 변경 스크립트 import 후 C# 컴파일 오류가 없음을 확인했다.

### 아직 필요한 수동 검증

- Scene View와 Game View에서 벤치 밝기가 기존 의도대로 복구됐는지 확인한다.
- Play Mode 진입 전후에 동일한 `MaterialPropertyBlock` 밝기가 유지되는지 확인한다.
- Quest/OpenXR 양안에서 벤치의 밝기와 색을 확인한다.
