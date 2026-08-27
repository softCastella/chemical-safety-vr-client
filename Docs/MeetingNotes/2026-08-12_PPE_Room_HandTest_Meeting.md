# 2026-08-12 PPE Room HandTest 회의록

## 문서 목적

2026년 8월 12일 진행한 `3_PPE_Room_HandTest_scale_0` 벤치 밝기 회귀 조사,
마스크 원본·보정본 배율 확인, 34개 PPE FBX의 Unity Mesh 하위 자산 이름 정렬을
한곳에서 추적하기 위한 통합 기록이다. 상세 구현과 결함 이력은 기존 기준 문서를
우선하며, 이 문서는 당일 작업의 범위와 검증 상태를 요약한다.

## 작업 기준

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- `_scale`, `_scale_1` 등 다른 scene variant는 작업 대상으로 삼지 않았다.
- 씬·Inspector 작성값과 Unity GUID·Mesh local file ID를 보존했다.
- 이름 변경은 기존에 합의한 정확한 34개 기준 FBX에만 적용했다.
- 존재하지 않는 기준 자산은 만들거나 대체하지 않고 건너뛰는 정책을 유지했다.

## 오늘 작업 요약

| 구분 | 결과 |
|---|---|
| 검게 보인 `PPE_B_Bench` | 현재 기준 이름도 밝기 보정 대상으로 인식하도록 이름 판정 복구 |
| 마스크 보정 배율 | `mask big` Scale 1.0 대비 `mask small` Scale 약 0.26, 따라서 `×0.260` |
| 마스크 자산 동일성 | 동일 계열 외형이지만 Unity 기준 FBX GUID와 Mesh 구성이 서로 다름 |
| 34개 FBX Mesh 이름 | 34개 자산, 총 259개 Mesh 정렬 완료, 대상 내 잔여 `tripo_` 이름 0건 |
| 참조 안정성 | 34개 FBX GUID와 Mesh local file ID 변경 0건 |
| 대상 씬 보존 | 이름 적용 전후 `_scale_0` 씬 SHA-256 동일 |

## `PPE_B_Bench` 밝기 복구

### 적용한 변경

- `Assets/Scripts/PPEBackgroundRoom.cs`의 벤치 이름 판정을 `MatchesBenchName`으로
  분리했다.
- 현재 기준 이름 `PPE_B_Bench`, 번호 접미사 이름 `PPE_B_Bench_...`, 이전 이름
  `ppe_room_bench...`를 모두 기존 밝기 보정 대상으로 유지했다.
- `Assets/Editor/PPERoomMaterialRecoveryHarness.cs`에
  `Tools > PPE > Validate Bench Brightness Name Routing` 검사를 추가했다.
- 벤치 Transform, Renderer, Material, Texture 및 Inspector의 `benchColor`,
  `roomBrightness` 작성값은 변경하지 않았다.

### 근본 원인

에셋 이름 정리 후 씬 벤치 이름이 `PPE_B_Bench`가 됐지만, 기존 필터는
`PPE_B_Bench_`처럼 밑줄 접미사가 있는 이름만 허용했다. 이 때문에
`benchColor * roomBrightness`를 전달하는 `MaterialPropertyBlock`이 현재 벤치에
적용되지 않았고, 어두운 원본 Base Color만 표시되어 검게 보였다.

### 영향 범위

- `PPEBackgroundRoom.ApplyBenchColor`가 고르는 벤치 이름 조건만 변경했다.
- 방 표면 복구, Duct 색상, Transform, 입력, PPE Grab, UI, 텔레포트 및 오디오는
  변경하지 않았다.

## 마스크 원본·보정본 확인

### 씬 직렬화 기준

| 오브젝트 | 참조 FBX | Unity GUID | 직접 MeshFilter | 루트 Scale |
|---|---|---|---:|---:|
| `mask big` | `Assets/TripoModels/gas_mask_3d_model_Clone1_Clone1_Clone1_Clone1_Clone1/gas_mask_3d_model_Clone1_Clone1_Clone1_Clone1_Clone1.fbx` | `aa404a37bd04f7748b476c40df3fcec5` | 15개 | `(1, 1, 1)` |
| `mask small` | `Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx` | `5bb8b165d820ad44682d9f3db6875503` | 기준 Mask Mesh 39개 | 약 `(0.26000017, 0.26000005, 0.26000005)` |
| `mask small (1)` | `Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx` | `5bb8b165d820ad44682d9f3db6875503` | 기준 Mask Mesh 39개 | 약 `(0.26000017, 0.26000005, 0.26000005)` |

### 제출용 배율 판단

- 씬 작성 Scale 비율은 `0.260 / 1.000`이므로 **보정 배율은 `×0.260`**이다.
- 보정 결과 치수는 `mask big`의 동일 축 원본 실측값에 `0.260`을 곱해 계산한다.
- `mask big`과 `mask small`은 같은 마스크 계열로 보이지만, 정확히 같은 FBX는 아니다.
  GUID가 다르고 직접 MeshFilter도 각각 15개와 39개이므로 “동일 원본을 단순 복제해
  Scale만 바꾼 비교물”이라고 확정해서 제출하면 안 된다.
- `mask big`은 합의한 34개 기준 FBX 밖의 이전 자산이므로 이번 Mesh 이름 변경 대상이
  아니다. 따라서 그 하위에 `tripo_part_*`가 남아 있는 것은 34개 대상 검증 실패가 아니다.

### 아직 확정하지 않은 값

- `Transform.localScale = 1`은 원본 배율일 뿐 실제 미터 치수가 아니다.
- 제출 표의 `Import 직후` 치수에는 `mask big` 전체 Renderer의 aggregate Bounds를 Unity에서
  직접 측정한 값을 사용해야 한다. `mask big`을 선택하고
  `Tools > PPE > Diagnose Selected Object Spatial Context`를 실행하면 로그의
  `Renderer aggregate ... size=`에서 해당 값을 확인할 수 있다.
- 현재 정적으로 확정된 값은 보정 배율 `×0.260`이며, 원본 가로·높이·깊이와 보정 후
  실측 치수는 live Renderer Bounds 측정 후 이 표에 추가한다.

## 34개 FBX Mesh 하위 자산 이름 정렬

### 적용한 규칙

- 정확한 34개 기준 FBX 경로만 대상으로 실제 `Mesh.name`을 기준 이름에 맞췄다.
- `tripo_part_숫자` → `기준이름_Part_숫자`
- `tripo_node_GUID` → `기준이름_Node_GUID`
- `meshes[숫자]` → `기준이름_Mesh_숫자`
- `Chemical_Glove`, `Yellow_Protective_Sleeve`,
  `Orange_Wrist_Packing_Tape` 같은 기능성 접미사는 기준 이름 뒤에 보존했다.
- 정확한 34개 경로에만 동작하는 `PPEMeshSubAssetNamePostprocessor`를 추가해 FBX
  재import 후에도 같은 이름을 복구하도록 했다.

예시:

- `meshes[0]` → `PPE_B_Bench_Mesh_0`
- `tripo_node_7c300fa3` → `PPE_B_MetalLocker_Node_7c300fa3`
- `tripo_part_25` → `PPE_A_Mask_Part_25`
- `Chemical_Glove` → `PPE_A_Hand_GloveTape_L_Chemical_Glove`

### 완료한 검증

- 기준 34개 자산이 모두 존재했으며 누락은 0개였다.
- Unity batch mode에서 총 259개 Mesh 이름을 적용했다.
- 적용 전후 34개 FBX GUID와 모든 Mesh local file ID 집합이 동일했다.
- 독립 Unity batch session의 `Validate 34-Asset Mesh Names`가
  `baseline=34`, `existing=34`, `missing=0`, `meshes=259`로 통과했다.
- 대상 34개 자산의 Mesh 이름에서 잔여 `tripo_`가 0건임을 확인했다.
- `_scale_0` 씬 SHA-256은 적용 전후 모두
  `6254333FB4D6A8B1D55C1CE5BE9E42360C7B23D1261B612B6B172A86DBAFE91D`였다.
- C# 외부 빌드는 오류 0개로 통과했다. 기존 Unity.AI/.NET 참조 경고는 남아 있다.

## 변경 파일

### 오늘 작업에서 추가·수정

- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/Editor/PPERoomMaterialRecoveryHarness.cs`
- `Assets/Editor/PPEMeshSubAssetNamingMigration.cs`
- `Assets/Editor/PPEMeshSubAssetNamingMigration.cs.meta`
- `Docs/Bug/2026-07-28_PPE_Room_Numbered_Surface_Pink_Materials.md`
- `Docs/PPE_Room_AI_3D_Asset_Naming_Plan.md`
- `Docs/MeetingNotes/2026-08-12_PPE_Room_HandTest_Meeting.md`

### 보존한 사용자 작업

- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`에는 작업 시작 전부터 사용자 변경이
  존재했다. Mesh 이름 적용 작업에서는 이 씬을 열거나 저장하지 않았으며 시작·종료
  SHA-256이 동일함을 확인했다.

## 검증 상태

| 구분 | 결과 |
|---|---|
| 정적 확인 | 벤치 현재 이름·Material 참조, 마스크 FBX GUID·Mesh 구성·Scale, 34개 대상 범위 확인 |
| C# 컴파일 | 오류 0개 |
| Unity batch | 34개 Mesh 적용·독립 검증 및 문서 작성 정책 검증 통과 |
| Unity Scene/Game View | 벤치 최종 밝기와 마스크 aggregate Renderer Bounds 수동 확인 필요 |
| Play Mode | 벤치 밝기 유지, 대표 Mesh 참조와 PPE 표시 확인 필요 |
| Quest/OpenXR | 벤치 양안 밝기와 PPE 모델·착용 시각 확인 필요 |

정적 확인이나 Unity batch 결과를 Play Mode 또는 Quest/OpenXR 성공으로 확대 해석하지 않는다.

## 다음 확인 순서

1. Unity에서 `mask big`을 선택하고
   `Tools > PPE > Diagnose Selected Object Spatial Context`를 실행해 전체 Renderer
   aggregate Bounds의 `size`를 미터 단위로 기록한다.
2. 동일 축 원본 치수에 `0.260`을 곱해 `mask small`의 보정 결과를 계산한다.
3. Scene View와 Game View에서 `PPE_B_Bench` 밝기가 기존 의도대로 복구됐는지 확인한다.
4. Project 창에서 대표 FBX를 펼쳐 Mesh 이름과 Inspector 참조가 정상인지 확인한다.
5. Play Mode와 Quest/OpenXR에서 벤치·마스크·PPE 장착 시각을 최종 확인한다.

## 2026-08-12 머티리얼 에셋 네이밍 적용

- `tripo`가 남아 있던 머티리얼 에셋 160개의 파일명과 `Material.name`을 PPE 기준 이름으로 변경했고, 뒤늦게 확인된 Backplate 머티리얼 2개도 추가 적용해 총 162개를 정리했다.
- 적용 범위는 Storage Wall Rack 106개, MaskGlass 36개, Helmet 9개, 추출 머티리얼 8개, MixerRoom 1개다.
- `AssetDatabase.MoveAsset`으로 이동해 GUID를 보존했으며, 적용 로그에서 `identityChanges=0`을 확인했다.
- FBX 임포터의 원본 remap 식별자(`tripo_part_*`)는 외부 소스 키이므로 변경하지 않았다.
- Unity batch 검증 결과 잔여 레거시 머티리얼은 `legacy=0`이다.
- Backplate는 `PPE_A_Backplate_Material`과 `PPE_A_Backplate_Unlit`으로 변경했으며 추가 적용에서도 `identityChanges=0`을 확인했다.
- Project 창에서 대표 머티리얼 참조와 Play Mode 시각 결과를 수동 확인해야 한다. 이번 작업에서는 씬 Transform과 UI 설정을 변경하지 않았다.

## 관련 기준 문서

- [PPE Room AI 3D 에셋 네이밍 계획 및 적용 기록](../PPE_Room_AI_3D_Asset_Naming_Plan.md)
- [PPE Room 번호 표면·벤치 머티리얼 회귀 기록](../Bug/2026-07-28_PPE_Room_Numbered_Surface_Pink_Materials.md)
- [PPE Room 높이·실물 크기 검증 기록](../../Assets/Docs/Bug/2026-07-03_PPE_Room_Height_Lighting.md)
