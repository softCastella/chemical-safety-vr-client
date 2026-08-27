# 2026-07-16 PPE Room XR 체크포인트·텔레포트·컨트롤러 손 회의록

- 일자: 2026-07-16
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`
- 환경: Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / XR Interaction Toolkit 3.4.1 / XR Hands 1.7.3
- 대상 플랫폼: Meta Quest/Android, PC OpenXR
- 상태: 1차 구현 완료, Quest 실기기 검증 필요

## 1. 목적

PPE Room에서 사용자가 멀리 있는 작업 지점을 명확하게 인식하고 컨트롤러 레이로 선택해 이동할 수 있도록 홀로그램 체크포인트와 텔레포트 기능을 구성한다. 컨트롤러 외형 대신 손 모델을 표시하고 Trigger/Grip 입력이 손가락 움직임으로 보이도록 한다.

## 2. 홀로그램 위치 마커

### 구성

- `XR Location Marker_big`: 텔레포트 목적지를 표시하는 큰 링
- `XR Location Marker_small`: 작은 링 비교안
- `Default Location Marker_`: 구체형 발광 마커 비교안
- Quest Single Pass Instanced 렌더링을 지원하는 전용 URP 셰이더 사용
- 씬의 Transform과 머티리얼 Inspector 값을 표현의 원본으로 유지

### 결정 사항

- 큰 링은 크기를 확대·축소하지 않는다.
- 큰 링은 알파 변화로 가장자리가 줄어드는 착시가 발생하지 않도록 알파를 고정한다.
- 반짝임은 투명도가 아니라 RGB 발광 밝기로 표현한다.
- 구체형 마커는 중심 코어와 외곽 글로우를 가진 `Orb` 모드로 표현한다.

## 3. 텔레포트

### 입력 흐름

```text
Left/Right NearFarInteractor
  → XR Location Marker_big의 BoxCollider 감지
  → Select 입력
  → XRLocationTeleportTarget.Teleport()
  → XR Origin 이동
```

### 구현 원칙

- 체크포인트는 일반 `BoxCollider`를 사용한다.
- 체크포인트 Interaction Layer는 `Default`로 유지한다.
- 양손 레이의 Raycast Mask에 `Default`가 포함되어야 한다.
- 텔레포트 후 머리 방향과 추적 자세를 유지한다.
- `Preserve Current Height`를 기본 활성화해 X/Z 평면만 이동한다.
- 높이가 다른 층으로 이동하는 목적지에서는 해당 옵션을 끄고 목적지 Y를 사용할 수 있다.
- 링 중심에 그대로 도착하면 수납장과 거리가 멀어질 수 있으므로 마커의 수평 Forward 방향으로 도착점을 보정한다.
- `Arrival Forward Offset`은 현장 확인 결과 `0.7m`로 확정했다.
- `Align View To Destination`을 활성화해 도착 시 사용자가 마커 Forward 방향을 바라보게 한다.
- 앞·뒤 방향이 반대인 목적지는 마커의 Yaw를 수정하는 것을 우선하며, 필요한 경우 오프셋 부호를 반대로 설정한다.

## 4. 컨트롤러 레이

- 양손 Near/Far Cast Distance를 `10m`에서 `40m`로 확대했다.
- 기본 흰색 Line 머티리얼을 시안 계열 홀로그램 머티리얼로 교체했다.
- HDR 발광색, 밝기, 투명도, 맥동 속도와 강도는 머티리얼에서 조절한다.
- 레이 판정 거리와 화면에 표시되는 LineRenderer가 같은 Near/Far 경로를 사용한다.

관련 에셋:

- `Assets/Shaders/XRHologramRay.shader`
- `Assets/Materials/XR Markers/Hologram Controller Ray.mat`

## 5. 컨트롤러 손 모델

### 목표

- 컨트롤러 추적, Near/Far 레이, Select 기능은 유지한다.
- 기존 컨트롤러 메시만 숨기고 좌우 XR Hands 모델을 표시한다.
- Trigger는 검지, Grip은 중지·약지·소지를 굽힌다.
- 엄지는 Grip 값에 약하게 연동한다.

### 구현

- LeftController: `LeftHand.fbx`
- RightController: `RightHand.fbx`
- 정확한 FBX 루트 GameObject fileID `919132149155446097` 사용
- 각 손가락 본의 다음 관절 방향과 Palm 방향으로 실제 굽힘 축을 계산
- Animator 갱신 이후에도 회전이 유지되도록 `LateUpdate`에서 본 회전 적용
- 손 위치, 회전, 크기, 관절 각도, 보간 속도는 Inspector 직렬화 값으로 관리

관련 코드:

- `Assets/Scripts/XRControllerHandAnimator.cs`

## 6. Main Camera와 Game View

- Main Camera의 Camera, Culling Mask, Target Display와 Target Texture 설정은 정상으로 확인됐다.
- 기존 커스텀 `XRHeadTrackedCamera`는 OpenXR 표준 Tracked Pose Driver로 인식되지 않았다.
- Starter Assets와 같은 `Tracked Pose Driver (Input System)`으로 교체했다.
- Center Eye Position, Center Eye Rotation과 Tracking State 입력을 연결했다.
- OpenXR 세션이 즉시 `STOPPING`으로 전환되면 Quest Link 연결 및 HMD 활성 상태를 추가 확인해야 한다.

## 7. PPE Room 오브젝트 원본 색상

파란 벽 조명의 영향을 받지 않고 Base Color 텍스처 색을 유지해야 하는 에셋에 XR Unlit 머티리얼을 적용했다.

| 대상 | 머티리얼 | 비고 |
|---|---|---|
| `wall_hanger` | `Wall Hanger Original Color.mat` | 씬 Renderer 한 곳에만 적용 |
| Wooden Crate | `Wooden Crate Original Color.mat` | 밝기 1.25, FBX 재매핑 |
| Yellow Trash Bin | `Yellow Trash Bin Original Color.mat` | 밝기 1.25, FBX 재매핑 |

공통 셰이더: `Assets/Shaders/XRUnlitBaseColor.shader`

## 8. 검증 체크리스트

- [ ] Quest에서 홀로그램 링이 양쪽 눈에 같은 위치와 크기로 보인다.
- [ ] 양손 레이가 10m 이상 떨어진 체크포인트까지 도달한다.
- [ ] 레이가 큰 링 Collider를 감지한다.
- [ ] Select 입력 한 번에 텔레포트가 한 번만 실행된다.
- [ ] 텔레포트 후 사용자 머리 높이가 유지된다.
- [ ] 도착 위치가 링 중심보다 Forward 방향으로 `0.7m` 앞에 형성된다.
- [ ] 도착 직후 수납장과 대상 물체가 사용자의 정면에 있다.
- [ ] 컨트롤러 또는 손으로 수납장 앞 물체에 무리 없이 도달할 수 있다.
- [ ] Trigger 입력에 검지만 움직인다.
- [ ] Grip 입력에 중지·약지·소지가 자연스럽게 움직인다.
- [ ] 좌우 손목 위치와 회전이 실제 컨트롤러 그립과 일치한다.
- [ ] Game View와 HMD 양쪽에 Main Camera 영상이 표시된다.
- [ ] `wall_hanger`, Wooden Crate, Yellow Trash Bin이 파란 조명에 물들지 않는다.
- [ ] Unity Console에 C#·셰이더·직렬화 예외가 없다.

## 9. 후속 작업

1. Quest 실기기에서 손목 오프셋과 좌우 손가락 굽힘 방향 조정
2. 체크포인트 Hover 시 레이 또는 링의 색상 피드백 추가 검토
3. 텔레포트 시 페이드 인·아웃과 이동 쿨다운 추가 검토
4. 높이가 다른 층을 위한 목적지별 `Preserve Current Height` 정책 정의
5. Game View 검은 화면 재발 시 새 Play Mode 로그와 Quest Link 상태 기록

## 10. 텔레포트 도착 위치 최종 조정

### 확인된 문제

- 링 중심 좌표로만 이동하면 사용자가 마커 뒤쪽에 선 것처럼 느껴졌다.
- 텔레포트 직후 수납장 앞의 물체가 손 또는 컨트롤러 도달 범위에서 멀어졌다.
- 마커는 바닥 위치뿐 아니라 작업 대상 쪽을 나타내는 방향 기준도 필요했다.

### 최종 설정

| 설정 | 값 | 의미 |
|---|---:|---|
| `Preserve Current Height` | 활성 | 현재 사용자 높이 유지 |
| `Arrival Forward Offset` | `0.7m` | 마커 Forward 방향으로 앞쪽 도착 |
| `Align View To Destination` | 활성 | 도착 후 마커 Forward 방향을 바라봄 |
| `Floor Offset` | `0.03m` | 목적지 높이 사용 시 바닥 여유값 |

도착 위치는 씬에 저장된 `_big` 마커 Transform의 위치와 Forward 방향을 기준으로 계산한다. 런타임 코드에 수납장 좌표를 별도로 고정하지 않는다.
