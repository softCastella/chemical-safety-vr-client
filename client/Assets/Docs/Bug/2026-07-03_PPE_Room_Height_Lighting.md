# 버그 리포트: PPE Room 높이·조명·환경 반사

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-03 |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / Meta Quest·PC VR |
| 관련 코드 | `Assets/Scripts/PPEBackgroundRoom.cs` |
| 상태 | 수정 진행, HMD 실기 검증 필요 |

## 요약

PPE Room의 높이와 위치를 조정하는 과정에서 방 루트 Transform을 Y축으로 확대하면 문과 덕트까지 늘어나는 문제가 확인됐다. 방 생성 파라미터를 사용하는 방식으로 수정했다. 이후 Unity Scene 뷰에서는 밝아 보이는 천장 조명이 VR에서 잘 보이지 않았고, Lit 재질 적용 시 파란 환경광과 반사가 방 전체 및 FBX에 강하게 나타났다. 조명 패널, Bloom, 재질 범위, 반사 설정을 단계적으로 조정했으며 씬 전체 환경광 변경은 물체가 검게 보여 롤백했다.

## BUG-001: 방 높이 변경 시 문과 덕트가 함께 늘어남

### 증상

- 방 루트의 Y Scale을 `2`로 변경하면 문과 덕트도 세로로 늘어났다.
- 플레이 모드 종료 시 외부에서 수정한 루트 Scale이 Unity에 의해 다시 저장되기도 했다.

### 원인

- 문과 덕트가 `PPE Background Room`의 자식이어서 부모 Scale을 상속했다.
- 방 구조는 `PPEBackgroundRoom.roomHeight`를 기준으로 절차적으로 생성되므로 루트 Scale 변경이 적절하지 않았다.

### 조치

- 방 루트 Scale을 `(1, 1, 1)`로 복원했다.
- `roomHeight`를 `7.5`에서 `11.25`로 변경해 원래 높이의 1.5배로 만들었다.
- 방 위치는 `(0, -2.79, 11.05)`로 유지했다.
- 문과 덕트는 원래 크기를 유지했다.

## BUG-002: Scene 뷰에서는 밝은 조명이 VR에서 잘 보이지 않음

### 증상

- Unity Scene 뷰에서는 천장 등이 밝게 보였지만 VR 카메라에서는 발광감이 거의 없었다.

### 원인

- VR 카메라의 Post Processing이 비활성 상태였다.
- 방 표면 재질이 URP Unlit이어서 Spot Light의 영향을 받지 않았다.
- 천장 등은 발광 재질이 아니라 텍스처에 그려진 밝은 영역이었다.
- 기본 Bloom 프로필은 존재했지만 강도가 `0`이었다.

### 조치

- PPE Room 활성화 시 VR 카메라의 Post Processing을 코드에서 활성화한다.
- PPE Room 전용 저강도 Bloom Volume을 런타임 생성한다.
- 두 천장 광원에 Lit+Emission 발광 패널을 추가했다.
- Quest 성능을 고려해 조명 그림자와 고품질 Bloom은 사용하지 않는다.

## BUG-003: 방 전체를 Lit로 변경하면 파란색으로 물듦

### 증상

- 벽·바닥·천장을 Lit로 변경하자 방 전체가 파랗게 보였다.

### 원인

- Lit 재질이 씬의 Skybox 환경광과 반사를 받았다.

### 조치

- 벽과 바닥은 URP Unlit으로 복원했다.
- 천장만 URP Lit을 사용한다.
- 천장에는 Bloom 임계값 이하의 약한 백색 Emission을 추가해 암회색을 완화했다.

## BUG-004: 덕트에 파란 환경 반사가 강하게 나타남

### 증상

- 덕트 표면이 파란색으로 반사되어 보였다.

### 원인

- `PPE_Duct_LightMetal.mat`의 환경 반사, Metallic, Smoothness, Specular가 활성화되어 있었다.

### 조치

- Environment Reflections를 비활성화했다.
- Metallic을 `0.08`에서 `0.02`로 낮췄다.
- Smoothness를 `0.28`에서 `0.12`로 낮췄다.
- Specular Highlights를 비활성화했다.

## BUG-005: 발광 패널과 천장 사이의 등 테두리가 흐려짐

### 증상

- 천장을 밝게 만든 뒤 등 테두리까지 회색으로 보여 발광 패널의 경계가 잘 보이지 않았다.

### 조치

- 발광 패널 뒤에 짙은 무광 Unlit 프레임을 추가했다.
- 프레임은 현재 패널 위치와 크기를 기준으로 자동 갱신된다.

## BUG-006: 플레이 모드 진입 시 광원 위치와 패널 크기가 초기화됨

### 증상

- 편집 모드에서 변경한 광원 위치와 패널 크기가 플레이 모드 실행 후 기본값으로 돌아갔다.

### 원인

- 광원이 `Generated Image Room` 아래에서 `DontSaveInEditor` 상태로 생성됐고 `BuildRoom()` 실행 때 삭제·재생성됐다.

### 조치

- 기존 광원을 자동 생성 루트에서 방 루트로 이동해 일반 씬 오브젝트로 보존한다.
- 동일한 이름의 광원이 있으면 재생성하지 않고 기존 Transform, 패널 크기, Light 설정을 재사용한다.

## BUG-007: 씬 전체 환경광 중성화 시 물체가 검게 보임

### 시도

- Ambient Mode를 중성 회색 Flat으로 변경했다.
- Reflection Intensity를 `1`에서 `0.3`으로 낮췄다.

### 결과

- 일부 FBX가 지나치게 어둡거나 검게 보였다.

### 조치

- Ambient Mode, Ambient Sky Color, Reflection Intensity를 기존 Skybox 설정으로 롤백했다.
- FBX별 재질 조정 방식으로 유지한다.

## 천장 형상 보정

- 문 반대편에서 천장 흰색 면이 몰딩을 덮는 문제를 줄이기 위해 뒤쪽 천장 길이를 `0.35m` 줄였다.
- 천장 중심을 문 쪽으로 `0.175m` 이동했다.
- 이 조정은 환경광 롤백 이후에도 유지한다.

## 검증 필요 항목

- [ ] Quest 실기에서 발광 패널과 Bloom의 밝기 및 비용 확인
- [ ] 편집한 광원 위치와 패널 크기가 씬 저장 후 플레이 모드에서도 유지되는지 확인
- [ ] 천장 뒤쪽 몰딩이 양쪽 시야에서 자연스럽게 노출되는지 확인
- [ ] 덕트가 지나치게 무광이거나 어둡지 않은지 확인
- [ ] 천장 백색 Emission이 텍스처 디테일을 과도하게 씻어내지 않는지 확인

## 관련 파일

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/UIs/Facilities/PPE_Room/Duct/PPE_Duct_LightMetal.mat`

## 2026-08-02 추가: 1.65m 눈높이 기준 3D 모델 스케일 시험

### 목적

- 원본 `3_PPE_Room_HandTest` 씬은 유지하고 복사본
  `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`에서만 실물 크기를 시험한다.
- 플레이어의 기준 눈높이는 바닥에서 `1.65m`로 고정한다. 이 값은 모델 스케일을
  계산하는 분모가 아니라, 실제 성인이 물건을 바라보는 시점을 재현하기 위한 기준이다.
- 각 모델은 표에 기록된 Import 직후 크기에 보정 배율을 적용해 실물 목표 치수로
  축소한다. Import 크기 모델을 비교 전시하지 않는다.
- 경고 표지판은 판면 중심을 바닥에서 `1.50m` 높이에 배치한다.

### 시험 기준과 계산 결과

| 시험물 | Import 기준 | 보정 배율 | 보정 결과 |
|---|---:|---:|---:|
| 방독면 | 최대 치수 1.02m | ×0.275 | 0.281m |
| 내화학성 장갑 | 길이 0.94m | ×0.404 | 0.380m |
| 차단 밸브 | 핸들 지름 1.00m | ×0.300 | 0.300m |
| 균열 배관 | 지름 0.31m · 길이 2.48m | ×0.806 | 지름 0.250m · 길이 1.999m |
| 흡착포 롤 | 지름 0.55m · 폭 1.24m | ×0.363 | 지름 0.200m · 폭 0.450m |
| 경고 표지판 | 판면 1.30m × 1.74m | ×0.346 | 판면 0.450m × 0.602m |

### 구성 방식

- `Real Scale Test - 1.65m Eye Height` 루트 아래에 실물 크기로 보정한 모델만 둔다.
- 1.65m 수직 기준자와 눈높이 수평선은 플레이어 시점을 확인하는 용도다.
- 방독면, 장갑, 밸브, 흡착포 롤, 경고 표지판은 프로젝트의 실제 자산을 사용한다.
- 현재 프로젝트에서 시험표와 일치하는 균열 배관 자산을 찾지 못했으므로,
  배관은 지름과 길이가 정확한 Cylinder 프록시로 크기만 검증한다.
- 스케일 시험 씬의 XR Origin에만 `XRScaleTestEyeHeightAligner`를 추가한다.
  추적 포즈가 안정된 뒤 카메라의 X/Z는 유지하고 Y만 바닥+1.65m로 맞춘다.
- 잘못 생성된 원본/보정 비교 랩은 제거한다.
- 생성 도구는 실물 크기 시험 루트가 이미 있으면 Inspector 값을 덮어쓰지 않고
  검증만 수행한다.

### 실행 및 검증

- 생성: `Tools > PPE > Build Real Scale Test`
- 재검증: `Tools > PPE > Validate Real Scale Test`
- 자동 검증 허용 오차: 최대 치수 기준 `±0.006m`
- C# 프로젝트 빌드 결과: 오류 0개. Unity.AI 계열 참조의 기존 버전 충돌 경고는
  이번 스케일 시험 코드와 무관하다.
- 최종 체감 평가는 Quest/OpenXR 양안에서 1.65m 기준선과 각 보정 모델을 함께 보고
  실시한다. 배관 프록시는 형상·재질 판정 대상이 아니다.

### 관련 파일

- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`
- `Assets/Editor/PPERoomScaleValidationSetup.cs`
- `Assets/Scripts/XRScaleTestEyeHeightAligner.cs`

### 2026-08-02 작업 종료 시점 상태

- 시험 목적은 **1.65m 성인 눈높이에서 실물 크기로 보정된 3D 모델의 체감 크기를
  확인하는 것**으로 확정했다.
- `1.65m`는 모델 크기와 나누어 상대 비율을 구하는 값이 아니다. XR 카메라를
  실제 성인의 눈높이에 두기 위한 시점 기준이다.
- 모델 크기는 `Import 직후 실측 치수 × 보정 배율`로 계산하며, 결과가 표의 실물
  목표 치수와 일치해야 한다.
- 최초 해석 오류로 `Scale Validation Lab`이라는 원본/보정본 비교 전시물이 복사
  씬에 한 차례 생성됐다. 이 구성은 최종 시험안이 아니며 제거 대상이다.
- 수정한 `Build Real Scale Test` 도구는 실행 시 잘못된 비교 전시물을 먼저 제거하고,
  실물 크기로 보정된 모델만 `Real Scale Test - 1.65m Eye Height` 아래에 구성한다.
- 자동 실행 요청은 제거했다. 다음 작업 전까지 Unity 씬을 추가로 변경하지 않는다.
- 에디터 코드의 외부 C# 빌드는 오류 0개로 통과했다. Unity 씬 적용과 bounds 검증,
  Quest/OpenXR 양안 체감 검증은 아직 완료하지 않았다.

### 다음 작업 시작 순서

1. Unity를 활성화하고 스크립트 컴파일이 끝날 때까지 기다린다.
2. Play Mode가 켜져 있으면 종료한다.
3. `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`를 연다.
4. `Tools > PPE > Build Real Scale Test`를 실행한다.
5. Hierarchy에서 잘못된 `Scale Validation Lab`이 제거됐는지 확인한다.
6. `Real Scale Test - 1.65m Eye Height` 아래에 실물 크기 모델 6개와 1.65m
   기준자만 있는지 확인한다.
7. `Tools > PPE > Validate Real Scale Test`를 실행해 최대 치수 오차가
   `±0.006m` 이내인지 확인한다.
8. Play Mode에서 카메라 Y가 `바닥 Y + 1.65m`로 정렬되는지 로그로 확인한다.
9. Quest/OpenXR 양안에서 방독면·장갑·밸브·배관·흡착포 롤·경고 표지판의
   체감 크기를 확인한다.

### 다음 검증 시 주의점

- 균열 배관은 현재 실제 자산이 아니라 치수 검증용 Cylinder 프록시이므로 형상과
  재질은 평가하지 않는다.
- 경고 표지판은 판면 중심 높이 `1.50m`를 별도로 확인한다.
- 원본 `Assets/Scenes/3_PPE_Room_HandTest.unity`는 수정하지 않는다.
- 실물 크기가 시각적으로 작아 보여도 임의로 확대하기 전에 실제 bounds와 1.65m
  카메라 기준이 정확한지 먼저 확인한다.

### 2026-08-02 후속 적용 및 검증 결과

#### 적용한 변경

- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`에서
  `Tools > PPE > Build Real Scale Test`를 실행했다.
- 잘못 생성됐던 `Scale Validation Lab`을 제거했다.
- `Real Scale Test - 1.65m Eye Height` 루트 아래에 실물 크기로 보정한 모델
  6개와 1.65m 눈높이 기준자를 구성하고 씬에 저장했다.
- 원본 `Assets/Scenes/3_PPE_Room_HandTest.unity`는 이번 적용 과정에서 수정하지
  않았다.

#### 완료한 검증

- 빌더가 실행한 Bounds 자동 검증이 통과했다.
  - 방독면: `1.020m × 0.275 = 0.281m`
  - 내화학성 장갑: `0.940m × 0.404 = 0.380m`
  - 차단 밸브: `1.000m × 0.300 = 0.300m`
  - 균열 배관: `2.480m × 0.806 = 1.999m`
  - 흡착포 롤: `1.240m × 0.363 = 0.450m`
  - 경고 표지판: `1.740m × 0.346 = 0.602m`
- Play Mode에서 바닥 `Y=-0.840m`, 목표 눈높이 `1.65m`, 카메라
  `Y=0.810m`가 기록되어 `바닥 Y + 1.65m` 정렬을 확인했다.
- 같은 실행 세션에서 `Invalid worldAABB`, `Invalid localAABB`,
  `Invalid AABB` 및 `IsFinite` 계열 오류는 발생하지 않았다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`은 오류
  0개로 완료됐다. 기존 Unity.AI 참조 충돌과 source generator 경고는 남아 있다.

#### 2026-08-03 Quest/OpenXR 재검증

- 최초 실패의 근본 원인은 중지된 `OVRService`와 시작되지 않은 Quest Link
  세션이었다. 이 상태에서는 `XR_ERROR_RUNTIME_UNAVAILABLE`가 발생했다.
- 관리자 권한으로 `OVRService`를 시작한 뒤 Meta 런타임이 Quest 2의 USB 3 연결을
  인식했다. Unity가 Link 시작을 요청한 뒤 Meta 로그에서 원격 렌더링 시작과
  `3136x1600` 첫 프레임 전송을 확인했다.
- Link 연결 전에 실행한 중간 검증에서는 런타임은 열렸지만 HMD가 준비되지 않아
  `XR_ERROR_FORM_FACTOR_UNAVAILABLE`가 발생했다. Link가 준비된 뒤 Play Mode를
  다시 시작해 이 오류가 해소되는 것을 확인했다.
- 재시작한 Play Mode에서 `OpenXR Display`와 `OpenXR Input`이 모두 로드됐다.
  OpenXR 세션은 `READY -> SYNCHRONIZED -> VISIBLE -> FOCUSED` 순서로 진입했다.
- 같은 실행에서 눈높이 정렬은 바닥 `Y=-0.840m`, 목표 눈높이 `1.65m`, 카메라
  `Y=0.810m`로 다시 기록됐다.
- 재시작 이후 `XR_ERROR_*`, C# 컴파일 오류, `Invalid worldAABB`,
  `Invalid localAABB`, `Invalid AABB` 및 `IsFinite` 계열 오류는 발생하지 않았다.
- Play Mode 진입 전후 XR UI Canvas의 authored Transform 값이 유지되는 것도
  검증 로그로 확인했다.

#### 아직 필요한 수동 검증

- Quest/OpenXR 양안에서 1.65m 기준선과 모델 6개의 체감 크기를 최종 확인해야 한다.
- 경고 표지판 판면 중심 높이 `1.50m`가 착용 시 자연스러운지 확인해야 한다.
- 균열 배관 프록시는 치수만 검증하고 형상과 재질은 평가하지 않는다.

#### 2026-08-03 착용 평가

- 내화학성 장갑의 체감 크기는 적절했다.
- 방독면은 조금 작게 느껴졌다.
- 테이프처럼 보이는 흡착포 롤은 너무 크게 느껴졌다.
- 방독면과 흡착포 롤의 배율은 즉시 변경하지 않는다. 착용 자세와 재센터에 따른
  시점 높이 변화를 제거한 동일 조건에서 한 번 더 비교한 뒤 조정한다.

#### 착용 자세에 따른 높이 변화 원인

- 현재 `XROrigin`의 Requested Tracking Origin Mode는 `NotSpecified`, Camera Y
  Offset은 `0`이다.
- `XRScaleTestEyeHeightAligner`는 Play Mode 시작 때 현재 카메라를 1.65m 기준으로
  한 번만 이동한다. 이후 앉거나 서서 발생하는 실제 머리 높이 변화는 정상적으로
  카메라에 반영되므로 물체가 상대적으로 높거나 낮아 보일 수 있다.
- 이번 착용 로그에는 `XR_OCULUS_recenter_event`가 두 차례 기록됐다. 현재 정렬기는
  재센터 뒤 1.65m 기준을 다시 적용하지 않으므로, 재센터 시점의 자세에 따라 시험
  기준 높이가 달라질 수 있다.
- 다음 체감 비교는 바닥 경계를 확인하고 선 자세에서 Play Mode를 시작한 뒤,
  평가가 끝날 때까지 자세를 유지하고 재센터하지 않는 조건으로 실시한다.

## 2026-08-03 추가: 1.56m 눈높이 비교 기준물

### 요청 목적

- 기존 `1.65m` 성인 눈높이 기준을 유지하면서, 키 156cm 수준의 시점을 비교할 수
  있는 별도 기준물을 같은 스케일 시험 씬에 추가한다.
- 두 기준물은 모델의 실제 크기와 관찰 높이의 관계를 비교하기 위한 시각 기준이다.
  이번 변경은 XR 카메라의 자동 정렬 높이를 `1.56m`로 변경하는 작업이 아니다.

### 적용한 변경

- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`의
  `Real Scale Test - 1.65m Eye Height` 아래에
  `Eye Height Reference - 1.56m`을 추가했다.
- 새 기준물은 다음 오브젝트로 구성했다.
  - `1.56m Vertical Ruler`: 높이 `1.56m`의 수직 기준자
  - `1.56m Eye Line`: 바닥에서 `1.56m` 높이의 수평선
  - `1.56m Eye Point`: 수평선과 같은 높이의 시점 표시점
- 기존 1.65m 기준자의 바닥과 Z 위치를 재사용하고, 겹치지 않도록 X축으로
  `1.5m` 옆에 배치했다.
- 기존 1.65m 기준자의 재질을 재사용해 두 기준물이 같은 시각 규칙을 유지하도록
  했다.
- `Assets/Editor/PPERoomScaleValidationSetup.cs`에
  `Tools > PPE > Add 1.56m Eye Height Reference` 메뉴를 추가했다. 이미 기준물이
  존재하면 다시 생성하거나 Inspector 값을 덮어쓰지 않고 해당 기준물만 검증한다.
- 이후 `Build Real Scale Test`로 시험 구성을 새로 만드는 경우에도 1.56m 기준물이
  함께 생성되도록 빌더를 확장했다.

### 근본 원인

- 기존 시험 구성에는 `1.65m` 기준만 있어서 156cm 수준의 관찰 높이를 같은 공간에서
  직접 비교할 수 없었다.
- 기존 1.65m 기준 또는 스케일 계산이 잘못된 것은 아니며, 비교 대상 높이가 하나 더
  필요해 별도 기준물을 추가한 것이다.

### 영향 범위

- 변경 대상은 스케일 시험 복사본 씬과 프로젝트 소유 Editor 도구뿐이다.
- 기존 `Eye Height Reference - 1.65m`, 실물 스케일 시험 모델, XR Origin,
  텔레포트·레이·Poke 설정은 변경하지 않았다.
- `XRScaleTestEyeHeightAligner`의 목표 높이는 계속 `1.65m`다.
- 원본 `Assets/Scenes/3_PPE_Room_HandTest.unity`와
  `Assets/Prefabs/Real Scale Test - 1.65m Eye Height.prefab`은 이번 작업에서
  수정하지 않았다.

### 완료한 검증

- 저장된 씬 YAML에서 `Eye Height Reference - 1.56m`과 세 하위 오브젝트가 모두
  존재하는 것을 확인했다.
- 수직 기준자의 직렬화된 Y Scale이 `1.56`인지 확인했다.
- 현재 씬의 기준 바닥 `Y=-0.835m`에서 눈높이 선과 시점 표시점의
  `Y=0.725m`까지 차이가 정확히 `1.56m`인지 확인했다.
- 1.56m 전용 검증은 통과했다. 전체 `Validate Real Scale Test`에서는 새 기준물과
  무관한 기존 방독면 최대 치수 불일치(`0.500m`, 목표 `0.281m`)만 남아 있다.
- Unity Editor 로그에서 이번 Editor 코드 변경에 대한 C# 컴파일 오류는 확인되지
  않았다.

### 아직 필요한 수동 검증

- Scene 뷰와 Game 뷰에서 1.65m 및 1.56m 기준물이 서로 겹치지 않고 쉽게 구분되는지
  확인한다.
- Quest/OpenXR 양안에서 두 기준선의 높이 차이와 모델 체감 크기를 확인한다.
- 실제 카메라를 1.56m 기준으로 자동 정렬하는 시험이 필요하면
  `XRScaleTestEyeHeightAligner`의 동작을 별도 모드로 설계하고 검증해야 한다.

