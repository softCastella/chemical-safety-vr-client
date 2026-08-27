# 2026-07-23 PPE Room LeftController XRGlove 손 애니메이션 미반응

## 개요

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 대상 오브젝트 | `XR Origin (VR)/Camera Offset/LeftController/Visuals/LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve` |
| 관련 Animator Controller | `Assets/Animations/XRGlove_L.controller` |
| 관련 클립 | `Assets/Animations/LeftGlove_Open.anim`, `Assets/Animations/LeftGlove_Grip.anim` |
| 상태 | Play Mode에서 왼손 그립 입력에 손 애니메이션 반응 없음 |

## 확인된 연결 구조

PPE Room 씬의 왼손 모델은 `LeftController` 자체에 붙은 런타임 생성형 손 애니메이터가 아니라, `LeftController/Visuals` 아래에 배치된 PPE 왼손 FBX 오브젝트를 통해 구성되어 있다.

```text
XR Origin (VR)
└─ Camera Offset
   └─ LeftController
      └─ Visuals
         └─ LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve
            ├─ Animator
            │  └─ XRGlove_L.controller
            └─ VRDemo.HandPoseFire.Runtime::Hand
               └─ _handAnimator -> 위 Animator 참조
```

현재 의도한 XRGlove 방식은 `VRDemo.HandPoseFire.Runtime::Hand` 컴포넌트가 입력 디바이스를 읽고 `_handAnimator`에 연결된 Animator의 `Grip` 파라미터를 제어하는 구조로 추정된다. 이 방식에서는 별도로 만든 `XRControllerAnimatorParameterDriver`를 사용하지 않는다.

## 비활성화되어 있는 대체 경로

씬에는 다음 컴포넌트도 존재하지만 현재 XRGlove 방식의 주 경로로 사용하지 않는다.

- `LeftController`의 `XRControllerHandAnimator`
  - `m_Enabled: 0`
  - 런타임에 손 모델 prefab을 생성하고 본을 직접 회전시키는 별도 방식이다.
  - 왼손 설정에서 `proximalCurl`, `intermediateCurl`, `distalCurl` 값이 모두 `0`으로 저장되어 있어 켜도 grip 애니메이션 목적과 맞지 않는다.
- `LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve`의 `XRControllerAnimatorParameterDriver`
  - `m_Enabled: 0`
  - 직접 `Animator.SetFloat("Grip", value)`를 호출하는 테스트용 드라이버였으나 실제 Play Mode에서 반응하지 않았다.
  - 최종 방향에서는 사용하지 않는다.

## 발견한 문제와 수정

`XRGlove_L.controller`의 BlendTree는 `Grip` float 파라미터로 open/closed 포즈를 블렌딩하는 구조다. 그러나 이전 상태에서는 BlendTree가 PPE 왼손 글러브용 클립이 아닌 다른 본 이름 체계의 클립을 참조하고 있었다.

문제가 된 점:

- `Open_L.anim`은 `L_Wrist/L_Index...` 경로를 사용한다.
- `Grip_L.anim`은 `b_l_wrist/b_l_index...` 경로를 사용한다.
- 서로 다른 본 경로 체계가 같은 BlendTree에 섞여 있으면 현재 PPE 왼손 모델에 정상 적용되기 어렵다.

수정한 내용:

- `XRGlove_L.controller`
  - `Grip = 0` 클립을 `LeftGlove_Open.anim`으로 변경
  - `Grip = 1` 클립을 `LeftGlove_Grip.anim`으로 변경

두 클립은 모두 `L_Wrist/...` 계열 경로를 사용한다.

## 정적 검증 결과

Unity 에디터를 통한 Play Mode 검증은 성공하지 못했지만, YAML 기준으로 다음을 확인했다.

- `LeftGlove_Open.anim`
  - 애니메이션 path 19개
  - `LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve` 하위 실제 Transform 경로와 모두 일치
  - missing path: 0
- `LeftGlove_Grip.anim`
  - 애니메이션 path 19개
  - `LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve` 하위 실제 Transform 경로와 모두 일치
  - missing path: 0

즉, 현재 클립이 모델 본을 못 찾아서 손가락이 움직이지 않는 문제는 아닌 것으로 판단된다.

## Unity 배치 검증 시도

다음 Unity 배치 실행을 시도했다.

```text
C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Unity.exe
-batchmode
-quit
-projectPath C:\Users\user\Documents\Workspace\3D_UI_Test_home
-logFile Logs\XRGloveBatchCheck.log
```

결과:

- 종료 코드: `1`
- 프로젝트 로드 및 스크립트 컴파일 로그가 나오기 전에 종료됨
- 로그에는 Unity Licensing Client IPC 관련 메시지와 `Application will terminate with return code 1`만 기록됨
- `error CS`, `Compiler Error`, `Asset import failed` 등 프로젝트 오류 로그는 확인되지 않음

따라서 이 배치 실행 결과만으로는 코드/애니메이션 import 성공 여부를 확정할 수 없다.

## 현재 결론

현재까지 확인된 바로는 애니메이션 클립과 왼손 PPE 모델의 본 경로는 맞다. 그럼에도 Play Mode에서 반응이 없다면 남은 가능성은 `VRDemo.HandPoseFire.Runtime::Hand`가 현재 XR 입력 디바이스를 잡지 못하거나, 해당 컴포넌트가 기대하는 Animator 파라미터/입력 조건이 현재 프로젝트 설정과 맞지 않는 쪽이다.

특히 `VRDemo.HandPoseFire.Runtime::Hand`의 구현 소스는 `Assets` 아래 `.cs`로 존재하지 않고, 씬에는 타입 식별자와 serialized 필드만 남아 있다. 따라서 내부에서 어떤 입력 특성값과 Animator 파라미터를 사용하는지 직접 확인하기 어렵다.

## 권장 보존 상태

현재 씬은 아래 상태를 유지한다.

- `LeftHand_PPE_Final_RedMarkedTape_ExtraPuffySleeve`
  - `Animator`: enabled
  - `Animator Controller`: `XRGlove_L.controller`
  - `VRDemo.HandPoseFire.Runtime::Hand`: enabled
  - `_handAnimator`: 위 Animator 참조
- `XRControllerAnimatorParameterDriver`: disabled
- `LeftController`의 `XRControllerHandAnimator`: disabled

## 후속 작업 후보

이 이슈를 재개한다면 다음 순서로 보는 것이 좋다.

1. Unity Editor Play Mode에서 `LeftHand_PPE_Final...`의 Animator 창을 열고 `Grip` 파라미터 값이 변하는지 확인한다.
2. `Grip` 값이 변하지 않으면 `VRDemo.HandPoseFire.Runtime::Hand`의 입력 디바이스 탐색 조건을 확인한다.
3. `Grip` 값이 변하는데 손이 움직이지 않으면 `XRGlove_L.controller`의 BlendTree와 클립 import 상태를 Unity Editor에서 재저장한다.
4. 가능하면 `VRDemo.HandPoseFire.Runtime::Hand` 대신 프로젝트 소유 C# 스크립트로 XR 입력을 읽어 Animator `Grip`만 구동하는 단순 경로를 새로 만든다.

