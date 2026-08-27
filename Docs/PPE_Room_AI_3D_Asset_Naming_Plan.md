# PPE 룸 AI 3D 자산 네이밍 기준

## 기준

2026-08-11부터 `E:\3D_Asset_Tyche\_Named_Assets`의 34개 분류를 PPE 룸 3D 자산 이름의 기준으로 사용한다.

- 외부 폴더의 앞 번호 `01_`~`34_`는 정렬과 식별을 위한 번호이며 자산 이름에 포함하지 않는다.
- FBX와 직접 대응하는 Project 자산은 `PPE_[분류]_[이름]` 형식을 사용하고, 자산이 한 종류뿐이면 `_01`을 붙이지 않는다.
- 같은 모델을 씬에 여러 번 배치할 때만 Hierarchy 인스턴스 이름에 `_01`, `_02`, `_03`을 붙인다.
- 좌우 구분은 `_L`, `_R`을 사용한다.
- `SuitHang`은 후드가 접힌 진열·착용 전 모델이고, `SuitWear`는 후드가 펴진 착용 모델이다. 두 자산을 같은 FBX로 취급하지 않는다.
- 모델, 프리팹, 컨트롤러 시각물의 GUID와 직렬화 참조를 유지하기 위해 Project 자산 이름 변경은 `AssetDatabase.MoveAsset`으로만 수행한다.
- 관련 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 한 개만 변경한다. 다른 씬 variant의 Hierarchy 이름은 이 작업 범위가 아니다.

## 34개 기준과 프로젝트 자산 경로

| 번호 | 기준 이름 | 프로젝트 자산 경로 | 비고 |
|---:|---|---|---|
| 01 | `PPE_B_Bench` | `Assets/FBX/ppe_room_bench/PPE_B_Bench.fbx` | |
| 02 | `PPE_B_MetalLocker` | `Assets/FBX/metal_locker/PPE_B_MetalLocker.fbx` | |
| 03 | `PPE_B_MaskLocker` | `Assets/FBX/mask_locker/PPE_B_MaskLocker.fbx` | |
| 04 | `PPE_B_SafetyCabinet` | `Assets/FBX/safety_cabinet/PPE_B_SafetyCabinet.fbx` | |
| 05 | `PPE_B_WoodenCrate` | `Assets/FBX/wooden crate/PPE_B_WoodenCrate.fbx` | 씬 배치 2개는 `_01`, `_02` |
| 06 | `PPE_B_WallHanger` | `Assets/FBX/hanger piece/PPE_B_WallHanger.fbx` | |
| 07 | `PPE_B_HandSanitizer` | `Assets/FBX/hand sanitizer dispenser/PPE_B_HandSanitizer.fbx` | |
| 08 | `PPE_B_FireExtinguisher` | `Assets/PPE_B_FireExtinguisher.fbx` | 씬 배치 2개는 `_01`, `_02` |
| 09 | `PPE_B_YellowTrashBin` | `Assets/FBX/yellow trash bin/PPE_B_YellowTrashBin.fbx` | 씬 배치 3개는 `_01`~`_03` |
| 10 | `PPE_B_StorageWallRack` | `Assets/FBX/storage wall rack/PPE_B_StorageWallRack.fbx` | 기존 `PPE_B_Hanger_01` 분류 수정 |
| 11 | `PPE_B_SuitHanger` | `Assets/FBX/hazmat_suit_hanger/PPE_B_SuitHanger.fbx` | |
| 12 | `PPE_B_CleaningCart` | `Assets/FBX/cleaning_cart/PPE_B_CleaningCart.fbx` | |
| 13 | `PPE_B_Duct` | `Assets/UIs/Facilities/PPE_Room/Duct/PPE_B_Duct.fbx` | |
| 14 | `PPE_B_CCTV` | `Assets/TripoModels/outdoor_security_camera_3d_model/PPE_B_CCTV.fbx` | 씬 배치 2개는 `_01`, `_02` |
| 15 | `PPE_C_Tablet` | `Assets/FBX/Tablet/PPE_C_Tablet.fbx` | |
| 16 | `PPE_A_SuitHang` | `Assets/FBX/hazmat suit/PPE_A_SuitHang.fbx` | 후드 접힘, 진열·착용 전 |
| 17 | `PPE_A_SuitWear` | `Assets/TripoModels/hazmat_suit_3d_model/PPE_A_SuitWear.fbx` | 후드 펴짐, 착용용 |
| 18 | `PPE_A_Boots_L` | `Assets/TripoModels/rubber_boots_3d_model_Clone1/PPE_A_Boots_L.fbx` | |
| 19 | `PPE_A_Boots_R` | `Assets/TripoModels/rubber_boots_3d_model/PPE_A_Boots_R.fbx` | |
| 20 | `PPE_A_Backplate` | `Assets/TripoModels/tactical_harness_3d_model/PPE_A_Backplate.fbx` | Harness가 아니라 Backplate |
| 21 | `PPE_A_Mask` | `Assets/TripoModels/gas_mask_3d_model_Clone1/PPE_A_Mask.fbx` | GasMask가 아니라 Mask |
| 22 | `PPE_A_Helmet_Strap` | `Assets/FBX/helmet/PPE_A_Helmet_Strap.fbx` | |
| 23 | `PPE_A_Glove_L` | `Assets/FBX/glove_Left/PPE_A_Glove_L.fbx` | |
| 24 | `PPE_A_Glove_R` | `Assets/FBX/glove_Right/PPE_A_Glove_R.fbx` | |
| 25 | `PPE_A_Tape` | `Assets/TripoModels/orange_tape_roll_3d_model/PPE_A_Tape.fbx` | PPE A 분류 |
| 26 | `PPE_A_Taped` | `Assets/FBX/taped/PPE_A_Taped.fbx` | 동일 바이너리 4개 중 `_0` 씬 사용 GUID를 기준으로 선택 |
| 27 | `PPE_A_Hand_Bare_L` | `Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_L.fbx` | 외부 FBX와 바이너리가 다르며 프로젝트 실사용 모델 유지 |
| 28 | `PPE_A_Hand_Bare_R` | `Assets/FBX/hand/BareHand/PPE_A_Hand_Bare_R.fbx` | |
| 29 | `PPE_A_Hand_Suit_L` | `Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_L.fbx` | |
| 30 | `PPE_A_Hand_Suit_R` | `Assets/FBX/hand/BareHand_Suit/PPE_A_Hand_Suit_R.fbx` | |
| 31 | `PPE_A_Hand_GloveSuit_L` | `Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_L.fbx` | |
| 32 | `PPE_A_Hand_GloveSuit_R` | `Assets/FBX/hand/Glove_Suit/PPE_A_Hand_GloveSuit_R.fbx` | |
| 33 | `PPE_A_Hand_GloveTape_L` | `Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_L.fbx` | |
| 34 | `PPE_A_Hand_GloveTape_R` | `Assets/FBX/hand/Glove_Suit_Tape/PPE_A_Hand_GloveTape_R.fbx` | |

## Prefab 이름 정책

FBX를 직접 감싸는 다음 프로젝트 소유 Prefab도 같은 기준으로 변경한다.

- `PPE_A_Boots_L.prefab`
- `PPE_A_Boots_R.prefab`
- `PPE_A_Backplate.prefab`
- `PPE_A_Tape.prefab`
- `PPE_A_Helmet_Strap.prefab`
- `PPE_A_Hand_Bare_L.prefab`
- `PPE_A_Hand_Bare_R.prefab`

`LeftController.prefab`, `RightController.prefab`, 생성된 Controller Visual Prefab, HandPose Prefab은 기능 단위 자산이므로 파일 이름을 PPE 모델명으로 바꾸지 않는다. 이 Prefab들의 내부 FBX 참조는 GUID로 유지된다. 사용하지 않는 Tripo 변형 Prefab은 내용이 현재 `_0` 씬 모델과 다르므로 이름만 보고 기준 자산으로 오인해 변경하지 않는다.

## 씬 조각 이름 정책

- `_0` 씬에서 전체 이름이 정확히 `tripo_part_숫자`인 GameObject만 조각 이름 변경 대상으로 삼는다.
- 번호는 유지하고 `소속부모_Part_숫자` 형식으로 변경한다. 예: `tripo_part_25` → `PPE_A_Mask_Part_25`.
- 별도 `mask`, `mask1`과 Real Scale Test 내부 조각도 루트·부모 이름은 유지하고 조각 이름에만 가장 가까운 부모 접두사를 사용한다.
- Bone, Rig, 중요한 부모 이름, FBX 내부 노드, Transform, 활성 상태, Layer, Tag, Component, Renderer, Material, Collider와 직렬화 참조는 변경하지 않는다.
- `Assets/Editor/PPETripoPartSceneNamingMigration.cs`의 `Tools > PPE > Rename Scene Tripo Parts Only` 메뉴가 애니메이션 경로·형제 이름 충돌을 사전 확인한 뒤 명시적으로 실행한다.

## 적용 방식

`Assets/Editor/PPE3DAssetNamingMigration.cs`의 `Tools > PPE > Apply External 34-Asset Naming` 메뉴가 다음 순서로 작업한다.

1. `_0` 씬이 열려 있고 Play Mode가 아닌지 확인한다.
2. 모든 이전·새 Asset 경로의 충돌 여부와 GUID를 기록한다.
3. 이름이 바뀌는 Hierarchy 경로를 참조하는 `.anim` 바인딩이 있으면 실제 변경 전에 중단한다.
4. `AssetDatabase.MoveAsset`으로 FBX 34개와 직접 대응 Prefab 7개를 이동한다.
5. 씬 오브젝트 44개의 이름만 변경하고 Transform, 활성 상태, 입력, 컴포넌트, 직렬화 참조는 바꾸지 않는다.
6. 이동 전후 GUID가 같은지 확인하고 씬을 저장한다.
7. `PPE3DAssetRenameDependencyValidation.ValidateScene`을 실행한다.

런타임 자동 수리나 `OnEnable`·`OnValidate` 자동 이름 변경은 사용하지 않는다.

## 적용 결과

### 적용한 변경

- 외부 17번에 잘못 복제되어 있던 `SuitHang` 세트를 실제 후드가 펴진 `SuitWear` FBX·텍스처·재질로 교체했다.
- 잘못된 외부 17번 원본은 `E:\3D_Asset_Tyche\_Named_Assets_Backup_20260811_SuitWear`에 보관했다.
- Project 창의 FBX 34개와 직접 대응 Prefab 7개의 이름을 GUID 보존 방식으로 변경했다.
- `_0` 씬의 환경, 진열 PPE, 착용 PPE, 양손 컨트롤러 시각물 이름을 새 기준에 맞췄다.
- `_0` 씬의 `tripo_part_숫자` 조각 590개를 부모 기반 `*_Part_숫자` 이름으로 변경했다. 이 중 535개는 씬 직접 직렬화 오브젝트이고 55개는 Prefab override 조각이다.
- 하드코딩된 씬 이름과 Prefab 경로를 사용하는 Editor·runtime 코드를 함께 갱신했다.

### 근본 원인

- 기존 문서는 모든 자산에 `_01`을 붙이고 `GasMask`, `GripTape`, `Boot_L/R`을 사용했으나, 새 외부 34개 기준은 단일 자산 번호를 제거하고 `Mask`, `Tape`, `Boots_L/R`을 사용한다.
- 동일 모델의 복사본과 실제 씬 사용 GUID가 `Assets/FBX`, `Assets/TripoModels`, `Assets/UIs`, `Assets/Prefabs`에 흩어져 있어 파일명만으로는 기준 자산을 결정할 수 없었다.
- 외부 `SuitWear`에는 `SuitHang` 파일이 들어 있어 역할과 파일 내용이 불일치했다.

### 영향 범위

- Project 자산 GUID는 유지되므로 씬, Prefab, Controller Visual, HandPose의 직렬화 참조는 유지된다.
- `_0` 씬의 오브젝트 이름과 이름 기반 Editor 도구·검증기만 새 이름을 사용한다.
- 입력, Grab, Ray, UI, 텔레포트, 음성 흐름, Transform, Renderer, Material 값은 이 작업에서 변경하지 않는다.
- 다른 PPE 씬 variant의 Hierarchy 이름은 변경하지 않았다. Project 자산 경로 변경은 GUID 기반 참조이므로 다른 씬의 참조 자체는 유지된다.
- 사용자가 별도로 작성한 루트 `mask`와 `Assets/FBX/mask/mask.fbx`는 34개 기준 밖의 자산으로 그대로 유지한다. 검증 예외는 정확한 루트 경로 `mask` 한 곳에만 적용한다.

### 완료한 검증

- 외부 폴더 34개, 번호 01~34 연속, 중복 번호 0, 폴더명·FBX명·동봉 파일 접두사 오류 0을 확인했다.
- `SuitHang`은 `Assets/FBX/hazmat suit`와, `SuitWear`는 `Assets/TripoModels/hazmat_suit_3d_model`과 각각 별도 해시로 일치함을 확인했다.
- 이동 전 Git의 `.meta`와 이동 후 `.meta`를 비교해 자산 41개 모두 GUID가 동일함을 확인했다.
- Unity Editor에서 `PPE3DAssetRenameDependencyValidation` 통과를 확인했다.
- 소화기 2개가 동일 Mesh 패키지를 공유하고, 장갑·장화·마스크의 진열·착용 시각물이 같은 제출 Mesh 집합을 유지함을 확인했다.

### 아직 필요한 수동 검증

- Unity Hierarchy와 Project 창에서 새 이름이 의도한 정렬과 가독성으로 표시되는지 확인한다.
- Play Mode에서 PPE 진열·착용 전환과 컨트롤러 손 시각물이 정상인지 확인한다.
- Quest/OpenXR에서 양손 모델과 PPE 장착 시각을 최종 확인한다. 이름 변경의 정적·Editor 검증을 Quest 성공으로 확대 해석하지 않는다.

## 2026-08-12 추가: 34개 FBX Mesh 하위 자산 이름 정렬

### 적용한 변경

- 기준표의 정확한 34개 FBX 경로만 대상으로 실제 `Mesh.name`을 기준 이름에 맞췄다.
- 존재하지 않는 기준 자산은 생성·대체하지 않고 건너뛰도록 구현했다. 적용 시점에는 34개가 모두 존재했다.
- `tripo_part_숫자`는 `기준이름_Part_숫자`, `tripo_node_GUID`는
  `기준이름_Node_GUID`, `meshes[숫자]`는 `기준이름_Mesh_숫자`로 변경했다.
- 손 모델의 `Chemical_Glove`, `Yellow_Protective_Sleeve`,
  `Orange_Wrist_Packing_Tape` 같은 의미 있는 하위 이름은 기준 이름 뒤에 그대로 보존했다.
- 단일 Mesh의 일반 이름은 해당 기준 이름으로 정리했다.
- `PPEMeshSubAssetNamePostprocessor`가 정확한 34개 경로의 FBX 재import 때 같은 규칙을
  다시 적용하므로 원본 FBX 내부 이름이 `tripo...`여도 Project의 Mesh 하위 자산 이름은 유지된다.

예시:

- `meshes[0]` → `PPE_B_Bench_Mesh_0`
- `tripo_node_7c300fa3` → `PPE_B_MetalLocker_Node_7c300fa3`
- `tripo_part_25` → `PPE_A_Mask_Part_25`
- `Chemical_Glove` → `PPE_A_Hand_GloveTape_L_Chemical_Glove`

### 근본 원인

기존 작업은 FBX 파일명, Prefab 이름, 씬 GameObject 이름을 정리했지만 Unity가 import한
FBX의 `Mesh` 하위 자산 이름은 원본의 `tripo_part_...`, `tripo_node_...`,
`meshes[0]` 값을 계속 사용했다. 따라서 Hierarchy 이름은 기준에 맞아도 MeshFilter 또는
SkinnedMeshRenderer의 Mesh 이름에는 이전 생성기 이름이 남았다.

### 영향 범위

- 정확한 34개 기준 FBX의 Mesh 하위 자산 이름만 변경한다.
- 34개 밖의 `Assets/FBX/mask/mask.fbx`, 사용자 작성 모델 및 존재하지 않는 기준 항목은 변경하지 않는다.
- FBX 바이너리, `.meta`, GUID, Mesh local file ID, Transform, Bone/Rig, Renderer,
  Material, Collider, Animation, 씬·Prefab 직렬화 참조는 변경하지 않는다.
- `_0` 씬을 열거나 저장하지 않으며 다른 씬 variant도 변경하지 않는다.

### 완료한 검증

- 적용 전 34개 기준 자산 전체가 존재하고 Mesh 하위 자산이 총 259개임을 확인했다.
- Unity batch mode에서 34개 FBX를 재import하고 259개 Mesh 이름을 적용했다.
- 적용 전후 34개 자산 GUID와 모든 Mesh local file ID 집합이 동일하며 변경 0건임을 확인했다.
- 별도 Unity batch session의 `Validate 34-Asset Mesh Names`가
  `baseline=34`, `existing=34`, `missing=0`, `meshes=259`로 통과했다.
- 최종 전체 감사에서 34개 자산의 `tripo_` Mesh 이름 잔여가 0건임을 확인했다.
- 적용 전후 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` SHA-256이
  `6254333FB4D6A8B1D55C1CE5BE9E42360C7B23D1261B612B6B172A86DBAFE91D`로 동일했다.
- C# 외부 빌드는 오류 0개로 통과했다. 기존 Unity.AI/.NET 참조 충돌 및 obsolete 경고는 남아 있다.

### 아직 필요한 수동 검증

- Unity Project 창에서 34개 FBX를 펼쳐 Mesh 이름의 정렬과 가독성을 확인한다.
- 대표 MeshFilter와 SkinnedMeshRenderer의 참조가 Missing 없이 유지되는지 Inspector에서 확인한다.
- Play Mode와 Quest/OpenXR에서 모델 표시와 착용 시각이 변경 전과 동일한지 확인한다.

## 2026-08-12 추가: PPE 머티리얼 이름 정리

### 적용한 변경

- `tripo`가 남아 있던 프로젝트 머티리얼 160개의 파일명과 `Material.name`을 PPE 기준으로 변경했다.
- 뒤늦게 확인된 Backplate 머티리얼 2개도 추가로 정리해 총 162개를 적용했다.
- Backplate 머티리얼은 `tactical_harness_3d_model`에서 `PPE_A_Backplate_Material`과
  `PPE_A_Backplate_Unlit`으로 변경했다.
- Storage Wall Rack 자동 생성기는 새 `PPE_B_StorageWallRack_Part_*` 이름을 사용하며,
  기존 레거시 머티리얼이 남아 있을 때 자동 재생성하지 않도록 보호 조건을 추가했다.

### 영향 범위와 보존 사항

- 모든 이동은 `AssetDatabase.MoveAsset`으로 수행해 `.meta`와 GUID를 보존했다.
- Unity batch 적용 결과 `renamed=162`, `identityChanges=0`이었다.
- 최종 Unity batch 검증 결과 머티리얼 레거시 이름은 `legacy=0`이며, `.mat` 파일에서
  `tripo`와 `tactical_harness` 문자열도 0건이다.
- FBX 원본 폴더명, 임포터 remap 식별자, 기능 코드의 `tactical_harness` 키와
  다른 씬 variant의 Hierarchy 이름은 이 머티리얼 작업 범위에서 변경하지 않았다.
- 씬 Transform, Renderer 값, UI 설정, 입력 동작은 변경하지 않았다.

### 아직 필요한 수동 검증

- Unity Project 창에서 `PPE_A_Backplate_Material`, `PPE_A_Backplate_Unlit` 및 대표 PPE
  Renderer의 Material 슬롯이 Missing 없이 표시되는지 확인한다.
- Play Mode에서 Backplate가 기존 색상·셰이더·표시 상태로 렌더링되는지 확인한다.
