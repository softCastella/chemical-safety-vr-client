# 2026-07-29 PPE룸 손 표현·위치·텔레포트 입력 정리

## 기본 정보

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / XRI 3.4.1 |
| 대상 브랜치 | `260729_손추가_인터렉션킷추가` |
| 관련 PR | PR #35 `핸드모델 xrinterationKit 추가` |
| 상태 | 구현 및 씬 저장 완료, Quest/OpenXR 양안 실기기 확인 필요 |

## 작업 범위

이번 작업은 PPE룸 컨트롤러 `Visuals` 아래의 손 모델 표시와 텔레포트 조작을 다음과 같이 정리했다.

1. 맨손 모델을 조명의 영향을 받지 않는 색으로 표시한다.
2. Inspector에서 좌·우 손 색상과 입체 표현 강도를 조절할 수 있게 한다.
3. 완전한 평면 Unlit에서 사라진 손가락 경계와 손의 부피감을 복구한다.
4. `BareHand_Suit` 모델에 같은 처리를 적용할 때 피부·방호복·커프를 구분한다.
5. 컨트롤러보다 수 미터 앞에 배치된 손 모델을 컨트롤러 원점으로 되돌린다.
6. 텔레포트 실행 입력을 Grip에서 Trigger로 변경하되 Grip 잡기 입력은 보존한다.

## 1. 맨손 Unlit 및 색상 조절

### 증상

일반 Unlit 머테리얼을 적용하면 원하는 피부색은 일정하게 표시됐지만, 조명과 노멀에 의한 명암도 모두 사라져 손가락 사이 경계와 손의 부피가 거의 보이지 않았다.

### 원인

평면 Unlit은 표면 방향과 시선 방향을 이용한 형태 정보를 계산하지 않는다. 피부색만 출력하면 서로 인접한 손가락 면이 같은 색으로 합쳐져 보인다.

### 수정

프로젝트 전용 셰이더 `3D UI Test/XR/Hand Form Unlit`을 추가했다.

- 조명에는 의존하지 않는다.
- 카메라를 향한 표면 방향을 이용해 약한 형태 명암을 만든다.
- 시선에 비스듬한 손가락 가장자리를 추가로 어둡게 한다.
- 피부색과 형태 강도는 Inspector 직렬화 값으로 조절한다.
- Meta Quest/OpenXR Single Pass Instanced 렌더링 매크로를 포함한다.

관련 파일:

- `Assets/Shaders/XRHandFormUnlit.shader`
- `Assets/Materials/XR Hands/PPE_LeftBareHand_LightSkin_Unlit.mat`
- `Assets/Materials/XR Hands/PPE_RightBareHand_LightSkin_Unlit.mat`
- `Assets/Scripts/PPEBareHandAppearance.cs`
- `Assets/Editor/PPEBareHandUnlitSetup.cs`

씬의 `Camera Offset`에 연결된 `PPEBareHandAppearance` 기본값은 다음과 같다.

| 속성 | 기본값 | 역할 |
| --- | ---: | --- |
| `Left Hand Color` | `(0.9453, 0.8434, 0.7736, 1)` | 왼손 피부색 |
| `Right Hand Color` | `(0.9445, 0.8425, 0.7741, 1)` | 오른손 피부색 |
| `Form Shading` | `0.32` | 손바닥·손가락 면의 완만한 형태 명암 |
| `Edge Darkening` | `0.38` | 손가락 외곽과 사이 경계 강조 |
| `Edge Power` | `3` | 어두워지는 가장자리 폭 |

`PPEBareHandAppearance`는 `MaterialPropertyBlock`을 사용하므로 좌·우 Renderer가 같은 셰이더를 사용해도 각 손의 색을 독립적으로 조절할 수 있다. 씬에 저장된 값이 기준이며 `OnEnable`과 `OnValidate`는 해당 값을 Renderer에 전달만 한다.

초기 적용 또는 재연결이 필요할 때 사용하는 명시적 에디터 메뉴:

```text
Tools > XR Hands > Apply PPE Bare Hand Form-Shaded Unlit Materials
```

이 도구는 최초 설정을 위한 것이며 이미 작성된 색상·형태 값을 반복 실행으로 덮어쓰지 않는다.

## 2. `BareHand_Suit` 적용 기준

좌·우 Suit FBX는 피부와 보호복을 별도 Renderer/머테리얼 슬롯으로 구분한다.

- 맨손 피부 Renderer: 피부 머테리얼 1개
- `Yellow_Protective_Sleeve`: 방호복과 검은 커프 머테리얼 2개

따라서 같은 형태 보정은 가능하지만 반드시 피부 Renderer에만 선택 적용해야 한다.

| 부분 | 권장 표현 |
| --- | --- |
| 맨손 피부 | `XRHandFormUnlit`과 피부색/형태 보정 적용 |
| 노란 방호복 | 일반 URP Unlit 또는 피부보다 약한 형태 보정 |
| 검은 커프 | 기존 전용 머테리얼 유지 |

Suit 루트 전체에 피부 머테리얼 하나를 할당하면 방호복과 커프 슬롯까지 덮어쓰므로 금지한다. 현재 `PPEBareHandAppearance`의 직렬화 연결 대상은 독립된 `LeftHand_BareHand`와 `RightHand_BareHand`이며, Suit 피부 Renderer는 아직 연결하지 않았다. PPE 착용 상태에 실제 적용할 때는 Suit 피부 Renderer용 참조와 색상 설정을 별도로 추가해야 한다.

## 3. 손 모델 위치 보정

### 증상과 원인

컨트롤러 추적은 정상인데 손 모델이 몸 앞이 아니라 멀리 전방에 표시됐다. 원인은 컨트롤러 `Visuals` 아래 네 손 루트에 저장된 큰 로컬 좌표였다.

| 모델 | 수정 전 주요 오프셋 |
| --- | --- |
| `RightHand_BareHand` | Z 약 `2.43m` |
| `LeftHand_BareHand` | Z 약 `2.40m` |
| `RightHand_BareHand_Suit` | Z 약 `4.71m` |
| `LeftHand_BareHand_Suit` | Z 약 `4.71m` |

### 수정

네 손 루트의 `localPosition`을 모두 `(0, 0, 0)`으로 맞췄다. PR #35에 저장된 현재 루트 스케일은 독립 맨손과 Suit 모델 모두 `1`이다.

여러 손 모델을 동시에 활성화하면 같은 컨트롤러 원점에서 겹쳐 보일 수 있으므로 PPE 상태 전환 시 필요한 모델만 활성화해야 한다. 크기가 맞지 않는 문제는 위치 문제와 분리해 각 모델 루트 스케일로 조절한다.

`XRControllerHandAnimator`는 씬에 작성된 `LeftHand_BareHand`와 `RightHand_BareHand`를 유지하고 애니메이션 대상으로 사용한다. 이 모델을 숨긴 뒤 런타임 대체 손을 생성하는 경로는 사용하지 않는다.

## 4. 텔레포트 입력을 Trigger로 변경

### 기존 동작

`XRLocationTeleportTarget`이 `OnSelectEntered`에서 이동을 실행했다. XRI 기본 입력에서 `Select`는 Grip에 연결되어 있으므로 레이로 지점을 선택하고 Grip을 눌러야 텔레포트됐다.

### 수정 동작

텔레포트 실행 콜백을 `OnActivated(ActivateEventArgs)`로 변경했다. XRI 기본 입력에서 `Activate`는 Trigger에 연결된다.

좌·우 `NearFarInteractor`의 `Allow Hovered Activate`도 활성화했다.

```text
m_AllowHoveredActivate: 1
```

이에 따라 텔레포트 지점을 레이로 가리키기만 한 상태에서 Trigger를 누르면 이동한다. Grip/Select 바인딩 자체는 변경하지 않았으므로 일반 물체 잡기에는 계속 Grip을 사용할 수 있다.

관련 파일:

- `Assets/Scripts/XRLocationTeleportTarget.cs`
- `Assets/Scenes/3_PPE_Room.unity`

## 5. 캡처 결과

씬 정면 캡처:

- `Captures/FrontViews_2026-07-29/PPE_Room_Front.png`
- `Captures/FrontViews_2026-07-29/PPE_Room_Front_02.png`
- `Captures/FrontViews_2026-07-29/Mixer_Room_Front.png`
- `Captures/FrontViews_2026-07-29/Inside_Mixer_Front.png`

손 형태 셰이더 프리뷰:

- `Captures/HandFormPreview_2026-07-29/Left_Back.png`
- `Captures/HandFormPreview_2026-07-29/Left_Forward.png`
- `Captures/HandFormPreview_2026-07-29/Left_Top.png`
- `Captures/HandFormPreview_2026-07-29/Right_Back.png`
- `Captures/HandFormPreview_2026-07-29/Right_Forward.png`
- `Captures/HandFormPreview_2026-07-29/Right_Top.png`

## 6. PR #35 충돌 해결

PR 작성 후 최신 `main`과 `Assets/Scripts/PPEBackgroundRoom.cs`에서 충돌이 발생했다. 다음 두 의도를 결합했다.

- 현재 브랜치: `Rear Wall (1)`, `Floor (1)`, `Ceiling (1)`처럼 정확한 Unity 번호 접미사를 가진 연장 면도 복구한다.
- `main`: 정상적인 저장 머테리얼은 덮어쓰지 않고, 머테리얼이 없거나 `HideFlags.DontSave`인 임시 머테리얼일 때만 프로젝트 머테리얼을 복구한다.

최종 병합 로직은 `MatchesSurfaceName`으로 정확한 원본 및 `이름 (n)`만 고르고, 기존 머테리얼이 없거나 임시 상태인 Renderer의 `sharedMaterial`만 갱신한다. Mesh, 계층, Transform은 재생성하거나 변경하지 않는다.

### 병합 후 패키지 타입 이름 충돌

텍스트 충돌을 해결한 뒤 전체 컴파일에서 프로젝트 소유 타입과 `VRDemo.HandPoseFire.Runtime` 패키지의 전역 타입 이름이 겹치는 문제가 추가로 확인됐다.

- `HandPoseData`
- `ControllerGrabHandPose`

가져온 패키지는 수정하지 않고 프로젝트 소유 두 타입을 `ThreeDUI.HandPoses` 네임스페이스로 이동했다. 기존 스크립트 `.meta` GUID는 유지했으며 `MovedFrom` 속성, 프로젝트 코드의 명시적 별칭, 기존 포즈 에셋과 테스트 씬의 `m_EditorClassIdentifier`를 함께 갱신했다.

이 조치로 프로젝트 포즈 데이터의 `hasAttachPose`, `attachLocalPosition`, `attachLocalRotation` 직렬화 구조를 패키지의 다른 포즈 데이터 구조와 혼동하지 않고 그대로 유지한다.

## 검증 체크리스트

- [x] 손 형태 셰이더에 OpenXR Single Pass Instanced 필수 매크로 포함
- [x] 독립 맨손 좌·우 Renderer와 색상 직렬화 연결 확인
- [x] 네 손 루트 `localPosition == (0, 0, 0)` 확인
- [x] 좌·우 `NearFarInteractor.m_AllowHoveredActivate == 1` 확인
- [x] PPE 씬 Unity 재임포트 시 YAML/직렬화 오류 없음
- [x] 병합 후 Unity 배치 모드 전체 스크립트 컴파일 오류 없음 확인
- [x] `PPERoomMaterialRecoveryHarness.Validate` 통과
- [ ] Quest/OpenXR에서 좌·우 눈 렌더링 동일 여부 확인
- [ ] 레이 Hover 후 좌·우 Trigger 텔레포트 확인
- [ ] Grip으로 일반 Select/Grab 동작 유지 확인
- [ ] PPE 상태 전환 시 네 손 모델이 동시에 활성화되지 않는지 확인

## 회귀 시 우선 확인 사항

1. 손이 멀리 보이면 컨트롤러 포즈보다 먼저 손 루트의 로컬 위치를 확인한다.
2. 손가락 경계가 사라지면 머테리얼 셰이더와 `Form Shading`, `Edge Darkening` 값을 확인한다.
3. Suit 전체가 피부색이 되면 피부 Renderer가 아니라 Suit 루트 또는 전체 머테리얼 배열을 덮어썼는지 확인한다.
4. Trigger가 반응하지 않으면 대상의 `OnActivated` 경로와 좌·우 Interactor의 `Allow Hovered Activate`를 확인한다.
5. Grip이 텔레포트를 실행하면 `XRLocationTeleportTarget`에 `OnSelectEntered` 호출이 다시 추가됐는지 확인한다.
