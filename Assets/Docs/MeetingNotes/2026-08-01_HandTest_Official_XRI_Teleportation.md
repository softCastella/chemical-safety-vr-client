# 2026-08-01 HandTest 공식 XRI 텔레포트 전환 회의록

- 날짜: 2026-08-01
- 프로젝트: `Prototype_Tyche_Jinyoung`
- Unity: 6000.4.8f1
- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest.unity`
- 상태: 씬 적용 및 Editor 자동 검증 완료, Quest/OpenXR 실기 검증 대기

## 배경

왼쪽 컨트롤러 계층에 기존 `NearFarInteractor`와 별도로 일반 `Ray Interactor`, `Teleport Interactor`가 추가되어 있었다. 일반 Ray와 Teleport Ray 모두 손 기준 로컬 위치가 약 12 m 떨어져 있었고, 컨트롤러별 Interactor 활성 상태를 중재하는 Starter Assets의 `ControllerInputActionManager`도 없었다.

PPE 텔레포트 목적지는 `PPE_1 Arrival Anchor`, `PPE_2 Arrival Anchor`라는 Transform만 존재했다. 실제 이동은 큰 마커의 프로젝트 전용 `XRLocationTeleportTarget`이 Near-Far의 Activate 입력을 받아 `TeleportationProvider`에 직접 요청하는 방식이었다. 이 마커는 Default Interaction Layer를 사용했으므로 Teleport 전용 Interaction Layer를 사용하는 공식 Teleport Interactor와 호환되지 않았다.

## 결정 사항

1. 카드, UI, 일반 원거리 상호작용에는 기존 `NearFarInteractor`를 유지한다.
2. Near-Far와 역할이 중복되는 별도 일반 `Ray Interactor`는 사용하지 않는다.
3. 텔레포트 조준에는 XRI Starter Assets의 공식 `Teleport Interactor`를 양손에 사용한다.
4. `ControllerInputActionManager`가 Near-Far와 Teleport Interactor의 활성 상태를 입력에 따라 전환한다.
5. 자유 이동 영역인 `TeleportationArea` 대신 지정 위치 이동에 적합한 `TeleportationAnchor`를 PPE 마커 두 개에 사용한다.
6. 이번 변경은 `3_PPE_Room_HandTest`에만 적용한다. 본 씬 적용은 실기 검증 후 별도 진행한다.

## 적용 구조

```text
LeftController
├─ Visuals
├─ Left_NearFarInteractor
└─ Teleport Interactor (초기 비활성)

RightController
├─ Visuals
├─ Right_NearFarInteractor
└─ Teleport Interactor (초기 비활성)
```

각 컨트롤러 루트에는 `ControllerInputActionManager`를 추가했다.

- `Ray Interactor`: None
- `Near-Far Interactor`: 해당 손 NearFarInteractor
- `Teleport Interactor`: 해당 손 공식 Teleport Interactor
- `Teleport Mode`: 해당 손 `XRI ... Locomotion/Teleport Mode`
- `Teleport Mode Cancel`: 해당 손 `XRI ... Locomotion/Teleport Mode Cancel`
- `Smooth Motion Enabled`: false
- `Smooth Turn Enabled`: false

## 입력 동작

- 스틱을 앞으로 밀면 Near-Far가 비활성화되고 Teleport Interactor가 활성화된다.
- 스틱을 놓으면 선택된 TeleportationAnchor로 이동한 뒤 Teleport Interactor가 비활성화된다.
- 텔레포트 모드가 끝나면 Near-Far가 다시 활성화된다.
- Grip 입력은 텔레포트 모드를 취소한다.
- Teleport Interactor가 Hierarchy에서 초기 비활성 상태로 보이는 것은 정상이다.

## 목적지 구성

활성 PPE 마커 두 개의 `XRLocationTeleportTarget`을 공식 `TeleportationAnchor`로 교체했다.

| 마커 | 목적지 Transform | Interaction Layer | 방향 일치 |
|---|---|---|---|
| `XR Location Marker_big_PPE_1` | `PPE_1 Arrival Anchor` | `Teleport` | Target Up And Forward |
| `XR Location Marker_big_PPE_2` | `PPE_2 Arrival Anchor` | `Teleport` | Target Up And Forward |

기존 도착 방향과 0.7 m 전방 오프셋은 Arrival Anchor Transform에 반영했다. 두 Anchor는 기존 `PPE Teleport-Only Locomotion`의 `TeleportationProvider`를 참조한다.

Hierarchy에서 비활성 상태였던 나머지 레거시 위치 마커 7개는 이번 범위에서 변경하지 않았다.

## 자동 검증

`PPEOfficialTeleportationSetup` 도구를 추가하고 설정 직후 검증을 실행했다.

검증 항목:

- 양손 공식 Teleport Interactor 존재 여부
- 별도 일반 Ray Interactor 제거 여부
- Left/Right Handedness
- 컨트롤러 원점 기준 Transform 정렬
- Teleport Mode 및 Cancel 입력 참조
- ControllerInputActionManager의 Near-Far/Teleport 참조
- Teleport Interactor 초기 비활성 상태
- Teleport Interaction Layer
- TeleportationProvider 및 Arrival Anchor 연결
- 활성 PPE 마커의 레거시 `XRLocationTeleportTarget` 제거 여부

결과:

- Unity C# 컴파일 오류 없음
- `HandTest official teleportation validation passed.`

## 남은 실기 검증

- [ ] 왼손 스틱 전진 시 공식 포물선 텔레포트 레이 표시
- [ ] 오른손 스틱 전진 시 공식 포물선 텔레포트 레이 표시
- [ ] 스틱 해제 시 선택 Anchor로 1회만 이동
- [ ] Grip 입력 시 텔레포트 취소
- [ ] 텔레포트 종료 후 Near-Far 레이 복구
- [ ] PPE_1/PPE_2 도착 위치와 정면 방향 확인
- [ ] Quest/OpenXR 양안에서 레이와 Reticle 위치 일치
- [ ] 카드 및 UI의 기존 Near-Far 상호작용 회귀 확인

## 관련 파일

- `Assets/Scenes/3_PPE_Room_HandTest.unity`
- `Assets/Editor/PPEOfficialTeleportationSetup.cs`
- `Assets/Docs/Bug/2026-08-01_HandTest-Official-XRI-Teleportation.md`
- `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/Interactors/Teleport Interactor.prefab`
- `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Scripts/ControllerInputActionManager.cs`
