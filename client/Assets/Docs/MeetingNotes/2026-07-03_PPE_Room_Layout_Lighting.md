# 2026-07-03 PPE Room 배치 및 VR 조명 회의록

- 날짜: 2026-07-03
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 대상: Meta Quest/Android 및 PC OpenXR
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`

## 오늘의 목표

- PPE Room의 위치와 높이를 확정한다.
- 문과 덕트의 원래 크기를 유지한다.
- VR에서 천장 조명이 인식되도록 개선한다.
- 파란 환경 반사와 천장 몰딩 가림 현상을 완화한다.

## 결정 사항

### 방 Transform 및 높이

- `PPE Background Room` 위치는 `(0, -2.79, 11.05)`로 확정했다.
- 방 루트 Scale로 높이를 조절하지 않는다.
- 방 생성 파라미터 `roomHeight`를 `11.25`로 설정해 원래 `7.5`의 1.5배 높이를 사용한다.
- 문과 덕트는 부모 Scale의 영향을 받지 않도록 방 루트 Scale `(1, 1, 1)`을 유지한다.

### 천장 조명 표현

- 벽과 바닥은 원본 색을 유지하기 위해 URP Unlit을 사용한다.
- 천장만 URP Lit을 사용한다.
- 천장 Lit 재질에 약한 백색 Emission을 추가한다.
- 천장 광원 두 개에 별도의 Lit+Emission 패널을 사용한다.
- 발광 패널 뒤에는 짙은 Unlit 테두리를 배치해 밝은 천장에서도 경계를 유지한다.
- PPE Room 전용 저강도 Bloom을 사용한다.
- VR 카메라 Post Processing은 PPE Room 실행 시 자동 활성화한다.
- Quest 성능을 위해 조명 그림자와 고품질 Bloom은 사용하지 않는다.

### 광원 편집값 보존

- 광원을 `Generated Image Room`의 일회성 생성물로 두지 않는다.
- 편집된 광원 위치와 발광 패널 크기를 우선 사용한다.
- 플레이 모드에서 동일 이름의 광원을 삭제하거나 기본 Transform으로 재설정하지 않는다.
- 씬 편집 후 Unity에서 씬을 저장해야 한다.

### 덕트 재질

- 파란 반사를 줄이기 위해 덕트 재질의 환경 반사를 끈다.
- Metallic과 Smoothness를 낮추고 Specular Highlights를 비활성화한다.
- 방 전체 환경광을 변경하는 대신 문제가 있는 FBX 재질을 개별 보정한다.

### 환경광 변경 시도 및 롤백

- 중성 회색 Flat Ambient와 Reflection Intensity `0.3`을 시험했다.
- 일부 물체가 검게 보여 해당 씬 전체 환경광 변경은 롤백했다.
- 기존 Skybox Ambient와 Reflection Intensity `1`을 유지한다.

### 천장 몰딩 노출

- 문 반대쪽 천장 끝이 흰색 천장 면에 덮이는 현상을 확인했다.
- 천장 뒤쪽 깊이를 `0.35m` 줄이고 천장 중심을 문 쪽으로 `0.175m` 이동한다.
- 환경광 롤백과 관계없이 이 형상 보정은 유지한다.

## 구현 파일

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/UIs/Facilities/PPE_Room/Duct/PPE_Duct_LightMetal.mat`
- `Assets/Docs/Bug/2026-07-03_PPE_Room_Height_Lighting.md`

## 검증 결과

- 방 높이를 부모 Scale이 아닌 생성 파라미터로 변경해 문과 덕트의 비정상적인 세로 확대를 제거했다.
- 벽과 바닥을 Unlit으로 복원해 방 전체가 파랗게 물드는 현상을 줄였다.
- 씬 전체 환경광 중성화는 물체가 검게 보여 롤백했다.
- C# 변경 후 Unity Editor 로그에서 컴파일 오류는 확인되지 않았다.

## 후속 확인

- [ ] Quest에서 천장 조명, Bloom, 프레임 대비 확인
- [ ] 광원 위치와 패널 크기의 플레이 모드 유지 확인
- [ ] 천장 몰딩 노출량 확인 후 `RearCeilingInset` 미세 조정
- [ ] 덕트 재질의 밝기와 금속 질감 균형 확인
- [ ] 필요 시 FBX별 중성 Lit 재질 프리셋 제작 검토
