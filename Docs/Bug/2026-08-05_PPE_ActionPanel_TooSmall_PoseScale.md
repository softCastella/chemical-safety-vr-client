# 2026-08-05 PPE Action Panel 크기·Pose·2/3버튼 레이아웃

## 개요

| 항목 | 내용 |
| --- | --- |
| 상태 | 수정 적용 — Unity/Quest 수동 검증 대기 (표시명·아이콘 정렬 후속 포함) |
| 심각도 | 중간 |
| 대상 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` |
| 관련 작업 | 회의록 §32, 일일 회의록 §6 |

## 증상 (사용자 보고)

1. 다른 PPE에 붙인 패널이 처음에는 방호복보다 훨씬 작게 보였음 → 방호복 원본 사이즈로 복원함.
2. 레이블이 너무 아래 → 원본 패널에서 레이블을 위로 조정함.
3. PPE 쪽 복사 Pose는 오브젝트에 패널이 **꽂혀** 있고 방향도 제각각임. 원래는 놓인 위치 **위쪽·정면**에 떠야 함.
4. 마스크만 `확인하기`가 있어 3버튼 세로 높이가 맞고, 나머지는 2버튼이라 **세로가 더 짧은 패널**이 맞음.

## 근본 원인

| 문제 | 원인 |
| --- | --- |
| 작아짐 | `ApplyPanelPose`가 `panelPose.lossyScale`을 써 PPE 모델 스케일이 곱해짐 |
| 꽂힘·방향 제각각 | Setup이 방호복 패널의 local Pos/Rot을 그대로 각 PPE 로컬 축에 복사. PPE마다 import 축이 달라 월드에서 뚫고 들어감 |
| 2버튼도 긴 패널 | 공용 패널을 3버튼 높이(420)로만 두고 Inspect만 숨김 → 빈 중간 슬롯이 남음 |

## 해결 방법 (적용)

### 1) 스케일

- 공용 패널의 씬 작성 월드 스케일(`authoredPanelWorldScale`)을 캡처해 유지.
- Pose의 lossyScale은 쓰지 않음.

### 2) 위치·방향 (방호복 제외)

- `panelPose == panelRoot`인 방호복: 씬에 달린 자식 패널 Transform 유지.
- 그 외 PPE: 매 프레임 검사 대상 Renderer Bounds의 **위쪽**(max.y + clearance)에 배치하고, 수평으로 **시청자(카메라)를 향하도록** 회전.
- 직렬화: `placePanelAboveInspectionBounds`, `panelHeightClearanceMeters`, `faceViewer`.

### 3) 2버튼 / 3버튼 레이아웃

- 새 컴포넌트 `PPEActionPanelSharedPresentation`을 `Hazmat Action Panel`에 둔다.
- 씬에 직렬화된 두 프리셋:
  - **threeButton**: 현재 원본(레이블 위·사이즈 복원본) 기준
  - **twoButton**: 더 짧은 세로(기본 height 310) + 사용/폐기만 배치
- 표시 시 `enableInspectChoice`에 따라 프리셋만 전환. 런타임이 레이아웃 숫자를 새로 발명하지 않음.

### 4) Editor

- `Tools > PPE > Repair Action Panel Layout And Placement`  
  기존 컨트롤러에 shared presentation·배치 플래그를 다시 연결하고 2/3버튼 프리셋을 캡처한다.
- `Tools > PPE > Configure Grab Action Panels`  
  최초 연결용(이미 있으면 Repair 권장).

## 관련 코드

- `Assets/Scripts/PPEActionPanelController.cs`
- `Assets/Scripts/PPEActionPanelSharedPresentation.cs`
- `Assets/Editor/PPEGrabActionPanelSetup.cs`

## 수동 검증

1. Play 종료 후 `Tools > PPE > Repair Action Panel Layout And Placement` 실행
2. 방호복: 복원한 레이블/크기 느낌 유지, 사용·폐기 2버튼 짧은 패널
3. 송기 마스크: 사용·확인하기·폐기 3버튼 긴 패널, 마스크 **위**에서 카메라를 향함
4. 장갑/장화/헬멧 등: 짧은 2버튼 패널이 모델에 꽂히지 않고 위·정면에 뜸
5. 잡은 뒤 물체를 돌려도 패널이 대략 위를 유지하고 시청자를 향하는지 확인

## 메모

- 방호복 원본 레이아웃을 더 손봤으면 Repair를 다시 실행해 threeButton 프리셋을 재캡처한다.
- twoButton height(310) 등은 Editor가 프리셋으로 씬에 저장한 뒤가 권위 값이다. 더 짧게/길게 조정한 뒤 Inspector에서 twoButton preset을 수정하거나 Repair 로직의 유도값을 바꾸면 된다.
- 레이블이 패널 천장에 붙는 문제: 2버튼 프리셋이 `panelHeight/2`에 너무 가깝게 잡혀 있었다. Repair 시 상단 패딩(~28px, Background inset 포함)을 강제하도록 `EnsureTopPaddedLabelY`를 추가했다. 현재 2버튼 레이블 Y 기본은 `98`이다.

## 후속: 표시명 확정 (2026-08-05)

| PPE | `itemDisplayName` |
| --- | --- |
| 방호복 | 내화학성 방호복 |
| 마스크 | 송기 마스크 |
| 안전모 | 안전모 |
| 장갑 | 내화학성 장갑(우) / (좌) |
| 장화 | 내화학성 장화(우) / (좌) |
| 테이프 | 내화학 테이프 |
| 등판 | 등지게 |

- Editor `PPEGrabActionPanelSetup` Targets 기본값도 동일하게 맞춤.
- 범주 표기는 **내화학성**(내산성 대신). 마스크는 공기공급식 설명 없이 **송기 마스크**.

## 후속: 3버튼 상태 아이콘이 버튼 사이에 끼는 문제 (2026-08-05)

### 증상

- 2버튼 PPE는 Pass/Error 아이콘이 버튼 오른쪽에 잘 보였음.
- 송기 마스크(3버튼)만 버튼이 벌어지는데 아이콘 Y는 예전 ±47.5에 고정되어 **버튼 사이**에 끼어 보임.
- 아이콘은 버튼 텍스트 **중앙에 겹치면 안 되고**, 텍스트 **오른쪽**에 세로만 맞춰야 함.

### 근본 원인

- Pass/Error 아이콘은 버튼 자식이 아니라 `Icons` 스트립(`anchoredPosition.x ≈ 75`) 아래 형제.
- `PPEActionPanelSharedPresentation`이 버튼 Y만 2/3버튼으로 바꾸고 아이콘 Y는 갱신하지 않음.

### 해결 방법 (적용)

1. 레이아웃 프리셋에 `use/discard/inspect` Pass·Error 아이콘 `anchoredPosition` 추가.
2. Editor가 버튼 Y − `Icons` 부모 Y로 **세로 중앙** 값을 캡처. **가로 X는 작성값 유지**(오른쪽 정렬).
3. 패널 표시 시 `Apply`가 아이콘 위치도 함께 전환.
4. 확인하기 전용 Pass 아이콘이 없으면, 피드백 순간에 공유 Use Pass 아이콘을 확인하기 버튼 높이로 잠깐 맞추고 숨길 때 프리셋으로 복구.

### 수동 검증 (추가)

6. 송기 마스크: 사용/확인하기/폐기 각각 결과 아이콘이 **해당 버튼 오른쪽 세로 중앙**에 붙는지
7. 다른 PPE(2버튼): 아이콘이 텍스트와 겹치지 않고 오른쪽에 유지되는지
8. 패널 표시명이 위 표와 일치하는지

