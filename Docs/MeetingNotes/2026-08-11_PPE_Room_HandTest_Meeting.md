# 2026-08-11 PPE Room HandTest 회의록

## 문서 목적

2026년 8월 11일 진행한 PPE Room HandTest의 컨트롤러 입력·가이드·시작 지점 테스트와 3D 에셋 네이밍 정리 내역을 한 문서에서 확인하기 위한 일일 통합 기록이다. 세부 구현과 검증 근거는 하단의 관련 문서를 기준으로 한다.

## 작업 기준

- 작업 브랜치: `controller_test_bothHand`
- 컨트롤러 테스트 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 3D 에셋 원본 정리 위치: `E:\3D_Asset_Tyche\_Named_Assets`
- 기존 Inspector·씬 작성값과 Unity 에셋 GUID를 보존한다.
- `_scale`, `_scale_1` 등 다른 씬 variant는 이번 컨트롤러 테스트 대상으로 사용하지 않는다.

## 오늘 확정한 동작

### 컨트롤러 입력

| 입력 | 최종 동작 |
|---|---|
| 왼손 Grip | PPE 잡기·놓기 |
| 오른손 Grip | PPE 잡기·놓기 |
| 왼손 Trigger | 패널/UI 선택 |
| 오른손 Trigger | 패널/UI 선택 |

- PPE marker는 Near 상호작용 대상으로 유지하고, Far Ray가 PPE marker를 원거리 선택하지 않도록 입력 경로를 분리했다.
- 사용자가 Play Mode에서 오른손 Grip 동작을 확인했다.
- 선생님 피드백을 반영해 최종 테스트 입력을 양손 Grip/Trigger 방식으로 정리했다.

### 음성 흐름과 컨트롤러 가이드

- 앞 단계 음성 안내가 컨트롤러 가이드를 가리거나 비활성화하지 않도록 흐름을 조정했다.
- HMD의 가이드 열기 입력은 좌·우 `Scale Toggle`의 `Primary2DAxisClick` 경로를 사용한다.
- Game View의 버튼 입력은 PC 테스트용 보조 경로이며, HMD 조이스틱 클릭과는 별도의 입력 경로로 구분한다.

### 테스트 시작 지점

- `PPEVoiceFlowDirector`에 `Start At Teleport For Testing` Inspector 체크박스를 두었다.
- 기본값 `false`: 기존처럼 처음 Welcome 단계부터 시작한다.
- 체크 시: 텔레포트 도착 이후의 PPE 구역 단계부터 테스트한다.
- 테스트 편의를 위한 옵션이므로 기본 사용자 흐름은 변경하지 않았다.

## 컨트롤러 시각물 및 참조

- 기존 좌·우 Hand Offset 아래의 Quest 2 컨트롤러 모델을 사용하도록 정리했다.
- 컨트롤러 모델을 `PPE Voice Flow` 하위에 새로 생성하지 않고, 씬에 작성된 좌·우 모델 참조를 `PPEVoiceFlowDirector`가 사용하도록 했다.
- 필수 모델 참조가 누락된 경우 런타임에서 자동 생성하거나 임의 선택하지 않고 명확한 오류로 확인하도록 유지했다.
- 컨트롤러 표시용 Unlit 머티리얼과 Inspector 작성값을 기준으로 시각물을 조정했다.

## PPE 표시 및 명칭 정리

- Backplate 패널 표기를 안전대 기준으로 정리하고 `PPE_A_Backplate` 명칭을 사용하도록 맞췄다.
- `4_PPE_B_FireExtinguisher_01`의 표시 머티리얼을 Unlit 계열로 적용했으며, 사용자가 씬에서 결과를 확인했다.
- 방호복 모델의 의미는 다음과 같이 유지했다.
  - `PPE_A_SuitHang`: 착용 전 걸린 모델, 후드가 접힌 상태
  - `PPE_A_SuitWear`: 착용용 모델, 후드가 펴진 상태

## 외부 3D 에셋 34종 정리

`E:\3D_Asset_Tyche\_Named_Assets`를 사용자가 정리한 폴더명을 기준으로 다시 대조했다.

- 폴더 번호는 01~34의 정렬·식별 용도로만 유지했다.
- 하나만 존재하는 에셋의 폴더 내부 FBX·Prefab 이름에서는 `_01` 같은 인스턴스 번호를 제거했다.
- `GasMask`를 `Mask`로 변경했다.
- Tape는 `PPE_A_Tape` 분류로 정리했다.
- 잘못 중복돼 있던 `SuitWear` 원본을 후드가 펴진 실제 착용 모델로 교체했다.
- 교체 전 외부 17번 데이터는 `E:\3D_Asset_Tyche\_Named_Assets_Backup_20260811_SuitWear`에 별도 보존했다.
- 외부 폴더 번호 누락·중복 및 폴더/FBX 접두어 불일치가 없음을 확인했다.

## Unity 프로젝트 네이밍 대입

외부 34종과 프로젝트 에셋을 FBX SHA-256 및 Unity GUID 참조를 기준으로 대응시킨 뒤 이름을 적용했다.

- 34개 FBX와 프로젝트 소유 Prefab 7개의 이름을 `AssetDatabase.MoveAsset`으로 변경해 `.meta` GUID를 보존했다.
- `_scale_0` 씬의 관련 루트·계층 오브젝트 44개를 기준 명칭에 맞췄다.
- Fire Extinguisher, CCTV, Trash Bin, Crate처럼 씬에 여러 번 배치된 인스턴스에만 `_01`, `_02`, `_03` 번호를 유지했다.
- `SuitHang`과 `SuitWear`는 서로 다른 모델로 유지했다.
- 장착용 `PPE_A_SuitWear`는 `PPE Body Anchor` 하위의 원래 위치 `(0.143, 0.22, -9.217)`로 복원해 저장했으며, 작성된 회전·스케일과 부모는 변경하지 않았다.
- 프로젝트에서 실제 사용 중인 왼손 모델은 외부 원본과 바이너리가 달라, 파일 교체 없이 현재 바이너리를 보존하고 이름만 정리했다.
- `PPE_A_Taped`는 중복 사본 중 `_scale_0` 씬이 참조하는 GUID의 에셋을 기준으로 삼았다.
- 별도 사용자 작성 에셋인 루트 `mask`와 `Assets/FBX/mask/mask.fbx`는 34종 기준 밖의 자산으로 보고 그대로 유지했다.

## `tripo_part_*` 조각 이름 정리

사용자 요청대로 정확히 `tripo_part_숫자` 형식인 조각 오브젝트만 변경했다.

- 변경 형식: `tripo_part_25` → `PPE_A_Mask_Part_25`
- 씬 직렬화 오브젝트 535개와 Prefab override 오브젝트 55개, 총 590개의 조각 이름을 변경했다.
- 루트·부모 이름, Bone, Rig, Transform, 활성 상태, Layer, Tag, 컴포넌트, Renderer, Material, Collider 및 참조는 변경하지 않았다.
- Project 창에서 FBX를 펼쳤을 때 보이는 imported source node 이름은 local ID와 참조 안정성을 위해 변경하지 않았다.
- 변경 전후 스냅샷 비교에서 이름 외 속성 변경 0건을 확인했다.
- 씬에 남은 정확한 `tripo_part_숫자` 이름은 0건이다.

## 근본 원인과 영향 범위

### 근본 원인

- 외부 폴더 번호와 실제 에셋 이름의 번호 정책이 혼재돼 있었다.
- 같은 모델의 사본과 Tripo 기본 조각 이름이 여러 위치에 남아 계층창과 Project 창에서 에셋의 용도를 식별하기 어려웠다.
- 컨트롤러 입력은 PC fallback, HMD joystick click, XR Trigger/Grip이 서로 다른 소비자를 사용하므로 한 경로의 성공을 다른 경로의 성공으로 볼 수 없었다.
- 테스트 시작 지점을 코드에 고정하면 기존 Welcome 흐름까지 바뀌므로 별도의 Inspector 옵션이 필요했다.

### 영향 범위

- 컨트롤러 입력·가이드·테스트 시작 옵션은 `_scale_0` HandTest 흐름과 관련 프로젝트 소유 스크립트에 한정된다.
- 에셋 네이밍은 34종 기준 FBX·Prefab, `_scale_0`의 관련 계층 이름, 그리고 경로를 직접 참조하던 프로젝트 소유 Editor/Runtime 코드에 반영됐다.
- Unity GUID는 보존했으므로 이름 변경만으로 기존 직렬화 참조가 교체되지 않도록 했다.

## 검증 현황

| 구분 | 결과 |
|---|---|
| 정적 확인 | 34종 표준 FBX 누락 0건, 중복 표준 basename 0건 |
| GUID 확인 | 이동한 FBX·Prefab 41/41 GUID 보존 |
| 조각 이름 확인 | 대상 590개 변경, 정확한 legacy 이름 0건 |
| 조각 속성 비교 | 이름 외 직렬화 속성 변경 0건 |
| Unity Editor 검증 | PPE 3D Asset Naming Dependency Validation 통과 |
| C# 컴파일 로그 | 최근 컴파일 오류 0건 |
| 사용자 확인 | 오른손 Grip 동작 및 Fire Extinguisher Unlit 표시 확인 |
| Quest/OpenXR | 양손 Grip/Trigger, joystick guide, 양안 표시의 최종 HMD 회귀 테스트 필요 |

정적 검사나 Editor 검증을 Quest/OpenXR 실기기 성공으로 확대 해석하지 않는다.

## Git 기록

오늘 컨트롤러 테스트 브랜치에 반영된 주요 커밋은 다음과 같다.

| 커밋 | 내용 |
|---|---|
| `c1b8ba3` | 현재 PPE 프로젝트 테스트 스냅샷 |
| `4fb866c` | PPE Room 컨트롤러 패널 입력 구성 |
| `d21bb67` | 음성 단계 중 컨트롤러 가이드 표시 유지 |
| `67e20c8` | 텔레포트 도착 이후 흐름 시작 테스트 |
| `19dabbc` | PPE marker에 대한 오른손 Grip 도달 경로 복원 |
| `9974b70` | 양손 PPE 컨트롤러 테스트 입력 활성화 |
| `3cd4e6c` | 컨트롤러 Grip을 PPE Grab 용도로 분리 |
| `005de00` | 텔레포트 음성 흐름 테스트 시작 옵션 추가 |

3D 에셋 네이밍, `tripo_part_*` 조각 이름 변경 및 본 회의록은 오늘의 통합 네이밍 커밋에 포함한다.

## 남은 수동 확인

- Quest/OpenXR에서 양손 Grip으로 PPE를 잡고 놓을 수 있는지 확인한다.
- 좌·우 Trigger로 패널의 사용·확인하기·폐기 버튼을 선택할 수 있는지 확인한다.
- HMD 좌·우 joystick click으로 컨트롤러 가이드가 열리고 닫히는지 확인한다.
- `Start At Teleport For Testing`의 체크 해제 시 Welcome부터, 체크 시 텔레포트 도착 이후부터 시작하는지 각각 확인한다.
- Quest 양안에서 컨트롤러 모델, Ray 방향, 패널, Mask/Glass 시각물이 동일하게 보이는지 확인한다.
- 이름이 변경된 34종을 실제 씬과 Project 창에서 열어 Prefab·Material·Animation·Collider 참조가 유지되는지 최종 표본 검사한다.

## 관련 문서

- [PPE Room AI 3D 에셋 네이밍 계획 및 적용 기록](../PPE_Room_AI_3D_Asset_Naming_Plan.md)
- [PPE Room 음성 내레이션 흐름 설계](../PPE_Room_Voice_Narration_Flow_Design.md)
- [PPE Room 카드 Ray 선택 문제](../Bug/2026-07-30_PPE_Room_Card_Ray_Selection.md)
- [PPE HandTest XR UI 렌더링 회귀](../Bug/2026-08-08_PPE_HandTest_XR_UI_Rendering_Regression.md)
- [PPE 방호복 Toggle Grip 및 점검 패널](../Bug/2026-08-04_PPE_Hazmat_Toggle_Grip_Inspection_Panel.md)
