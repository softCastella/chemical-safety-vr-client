# 2026-07-16 PPE Room XR 체크포인트·손 모델·Game View 버그 리포트

| 항목 | 내용 |
|---|---|
| 대상 | `Assets/Scenes/3_PPE_Room.unity` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / XRI 3.4.1 / XR Hands 1.7.3 |
| 상태 | 코드 및 씬 설정 수정 완료, Quest 재검증 필요 |

## 버그 1. 큰 체크포인트가 크기 변화처럼 보임

### 증상

- 큰 링이 반짝일 때 원이 작아졌다 커지는 것처럼 보였다.
- 알파가 약해질 때 가장자리부터 사라졌다.

### 원인

- 초기 펄스가 Transform Scale을 변경했다.
- 이후 알파와 글로우 그라데이션이 가장자리 가시성을 먼저 낮췄다.

### 조치

- Transform Scale 펄스를 비활성화했다.
- 큰 링의 알파 펄스를 비활성화했다.
- `Uniform Ring Alpha`를 적용했다.
- 반짝임은 RGB 발광 밝기에만 적용했다.

## 버그 2. 레이가 체크포인트에 반응하지 않을 수 있음

### 증상

- 홀로그램 레이가 큰 링을 통과하거나 Select가 전달되지 않을 가능성이 있었다.

### 원인

- 체크포인트 Collider가 Trigger였고 Near/Far Raycast는 Trigger를 무시하는 설정이었다.

### 조치

- 체크포인트를 일반 `BoxCollider`로 변경했다.
- Interaction Layer와 Raycast Mask의 `Default` 포함 여부를 확인했다.
- 양손 Cast Distance를 `40m`로 확대했다.

## 버그 3. 텔레포트 후 플레이어가 바닥 높이로 내려감

### 증상

- 체크포인트 선택 후 기존 머리 높이가 유지되지 않고 바닥에 붙은 것처럼 이동했다.

### 원인

- 텔레포트 코드가 체크포인트의 Y 좌표까지 XR Origin 이동량에 포함했다.
- 큰 링의 저장 Y 좌표는 `-2.44`였다.

### 조치

- `Preserve Current Height` 옵션을 추가하고 기본 활성화했다.
- 현재 XR Origin의 수직 위치는 유지하고 X/Z 이동만 적용한다.

## 버그 4. 손 모델 Instantiate 시 InvalidCastException

### 증상

```text
InvalidCastException: Specified cast is not valid.
XRControllerHandAnimator.Awake()
```

### 원인

- FBX 참조에 `fileID 100100000`을 사용해 GameObject가 아닌 모델 서브 에셋이 연결됐다.

### 조치

- XR Hands 샘플이 사용하는 실제 루트 GameObject fileID `919132149155446097`로 좌우 참조를 교체했다.

## 버그 5. Trigger/Grip 입력에도 손가락이 움직이지 않음

### 증상

- 손 모델은 표시되지만 Trigger와 Grip 입력에 손가락 메시가 반응하지 않았다.

### 원인

- 모든 손가락 관절을 고정된 로컬 X축으로 회전했다.
- LeftHand/RightHand FBX의 관절별 실제 굽힘 축과 일치하지 않았다.
- `Update`에서 적용한 회전이 Animator 갱신에 덮일 가능성이 있었다.

### 조치

- 다음 관절 방향과 Palm 방향으로 각 본의 굽힘 축을 계산한다.
- 본 회전을 `LateUpdate`에서 적용한다.
- 좌우 모델의 굽힘 방향을 Inspector 값으로 유지한다.

### 남은 검증

- Quest 컨트롤러에서 Trigger/Grip 축 값 수신 여부
- 좌우 본 굽힘 방향과 손목 정렬
- 엄지 움직임 범위

## 버그 6. Scene View는 보이지만 Game View가 검은 화면

### 증상

- Scene View에는 PPE Room이 보이지만 Play Mode Game View는 검은 화면이었다.
- 로그에 Main Camera가 Tracked Pose Driver를 사용하지 않는다는 경고가 있었다.
- OpenXR 세션이 `READY → SYNCHRONIZED → STOPPING`으로 빠르게 종료됐다.

### 원인 후보

1. Main Camera가 커스텀 `XRHeadTrackedCamera`만 사용해 OpenXR 표준 추적 컴포넌트로 인식되지 않음
2. Quest Link 또는 HMD 포커스가 해제되어 OpenXR 세션 종료

### 조치

- Main Camera의 커스텀 헤드 추적 컴포넌트를 표준 `Tracked Pose Driver (Input System)`으로 교체했다.
- Starter Assets와 동일한 Center Eye Position, Rotation과 Tracking State 입력을 연결했다.

### 재현 및 확인 절차

1. Play Mode를 완전히 종료한다.
2. 스크립트 및 셰이더 컴파일이 끝날 때까지 기다린다.
3. Quest Link 연결 및 HMD 활성 상태를 확인한다.
4. Play Mode를 다시 시작한다.
5. Game View와 HMD 출력을 확인한다.
6. 재발하면 해당 시점의 `Editor.log`에서 OpenXR 상태 전환과 새 예외를 수집한다.

## 버그 7. 텔레포트 후 마커 뒤쪽에 도착해 물체를 잡기 어려움

### 증상

- 텔레포트 자체는 실행되지만 사용자가 작업 지점의 뒤쪽에 선 것처럼 느껴졌다.
- 수납장 앞의 물체가 손 또는 컨트롤러 도달 범위보다 멀었다.
- 마커 중심 도착만으로는 작업 대상에 대한 앞·뒤 방향이 반영되지 않았다.

### 원인

- 목적지 계산이 `_big` 마커의 중심 위치만 사용했다.
- 마커 Transform의 Forward 방향과 사용자의 도착 시선 방향을 사용하지 않았다.

### 조치

- 마커 Forward를 XR Origin Up 기준의 수평 벡터로 투영한다.
- 도착점을 마커 중심에서 Forward 방향으로 이동한다.
- `Arrival Forward Offset`을 사용자 조정 결과 `0.7m`로 저장했다.
- 도착 시 사용자의 수평 시선을 마커 Forward 방향으로 정렬한다.
- 높이 보존 동작은 그대로 유지한다.

### 최종 직렬화 값

```text
Preserve Current Height: true
Arrival Forward Offset: 0.7
Align View To Destination: true
Floor Offset: 0.03
```

### 검증 항목

- [ ] 텔레포트 후 수납장 앞쪽에 선다.
- [ ] 수납장과 작업 물체가 사용자 정면에 보인다.
- [ ] 손 또는 컨트롤러로 물체를 잡을 수 있다.
- [ ] 반복 텔레포트에서도 높이와 방향이 누적해서 어긋나지 않는다.

## 관련 파일

- `Assets/Scripts/XRLocationMarkerPulse.cs`
- `Assets/Scripts/XRLocationTeleportTarget.cs`
- `Assets/Scripts/XRControllerHandAnimator.cs`
- `Assets/Shaders/XRLocationMarker.shader`
- `Assets/Shaders/XRHologramRay.shader`
- `Assets/Shaders/XRUnlitBaseColor.shader`
- `Assets/Materials/XR Markers/Default Location Marker_big.mat`
- `Assets/Materials/XR Markers/Hologram Controller Ray.mat`
- `Assets/Scenes/3_PPE_Room.unity`

## 최종 검증 상태

- C# 컴파일 오류: 확인되지 않음
- 셰이더 임포트 오류: 확인되지 않음
- 씬 직렬화 참조: 수정 후 로드 확인
- Quest 양안·입력·텔레포트·Game View: 실기기 재검증 필요
