# 버그 리포트: insideMixer XR 시작 위치 및 손 추적

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-01 |
| 대상 씬 | `Assets/Scenes/4_InsideMixer.unity` |
| 환경 | Unity 6000.4.8f1 / OpenXR / Meta Quest·PC VR |
| 상태 | 원인 확인 및 수정 완료, 기기 재검증 필요 |

## 증상

1. VR 실행 시 혼합기 내부가 아닌 엉뚱한 위치에서 시작하며 환경이 보이지 않는다.
2. XR Hands의 좌·우 손 메시가 보이지 않고 모션 컨트롤러만 표시된다.
3. 기존 단독 카메라 방식에서는 XR Origin이 없어 XRI 컨트롤러와 손 구성을 정상적으로 연결하기 어렵다.

## 확인된 원인

### 시작 위치 이탈

- Starter Assets의 `XR Origin (XR Rig)`에는 `Gravity Provider`, `Jump Provider`, `CharacterController`가 기본 포함되어 있다.
- 혼합기 바닥은 시각 메시이며 보행용 Collider가 없다.
- 그 결과 Play Mode에서 XR Origin 전체가 계속 낙하했다.
- 실제 측정값은 Main Camera Y 약 `-4877`, XR Origin Y 약 `-4878`이었다.
- 고정 Transform 위치만 지정하는 방식은 룸스케일 및 Quest Link의 Tracking Origin 오프셋도 보정하지 못한다.

### 손이 표시되지 않음

- OpenXR의 Hand Tracking Subsystem은 활성화되어 있었지만 `Meta Hand Tracking Aim`은 Android와 Standalone 모두 비활성 상태였다.
- Quest에서는 모션 컨트롤러 사용 상태와 실제 손 추적 상태가 모달리티에 따라 전환된다. 컨트롤러를 잡고 있는 동안 XR Hands 메시가 나타나지 않는 것은 정상일 수 있다.

## 수정 내용

- 기존 단독 `Main Camera`와 `XRHeadTrackedCamera`를 제거했다.
- Unity Starter Assets의 `XR Origin (XR Rig)`을 씬에 배치했다.
- 공식 `Left Controller`, `Right Controller`를 유지했다.
- XR Hands HandVisualizer의 `Left Hand`, `Right Hand`를 Camera Offset 아래에 추가했다.
- XR Input Modality Manager에 좌·우 손과 컨트롤러를 연결했다.
- `XRStartPoseAligner`를 추가하여 다음을 수행한다.
  - 시작 시 `Gravity`와 `Jump` 오브젝트 비활성화
  - OpenXR 장치 활성화를 최대 10초간 대기
  - 실제 카메라를 월드 좌표 `(-0.472, 1.0, -12.14)`로 이동
  - 시작 시선 방향을 월드 `+Z`로 정렬
- Android 및 Standalone OpenXR의 `Meta Hand Tracking Aim`을 활성화했다.

## 실행 중 검증 결과

- 중력 비활성화 후 무한 낙하가 중단됐다.
- 실행 중 Main Camera를 `(-0.472, 1.000, -12.140)`으로 복구했다.
- 해당 좌표가 반지름 3m인 혼합기 내부임을 확인했다.
- Unity Console 컴파일 오류는 없었다.

## 기기 재검증 항목

- [ ] Play Mode 재시작 후 별도 수동 보정 없이 혼합기 내부에서 시작한다.
- [ ] 머리 회전과 위치 이동이 XR 카메라에 한 번만 적용된다.
- [ ] 실행 후 XR Origin의 Y 좌표가 계속 감소하지 않는다.
- [ ] Quest에서 컨트롤러를 내려놓고 손 추적으로 전환하면 좌·우 손 메시가 표시된다.
- [ ] 컨트롤러 모드에서는 좌·우 컨트롤러가 정상 추적된다.
- [ ] Android 기기 빌드에서도 동일한 시작 위치와 손 추적 전환을 확인한다.

## 관련 파일

- `Assets/Scenes/4_InsideMixer.unity`
- `Assets/Scripts/XRStartPoseAligner.cs`
- `Assets/Editor/InsideMixerXRSetup.cs`
- `Assets/XR/Settings/OpenXR Package Settings.asset`
