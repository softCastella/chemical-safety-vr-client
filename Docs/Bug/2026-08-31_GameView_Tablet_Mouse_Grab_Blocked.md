# 2026-08-31 Game View 태블릿 마우스 Grab 오진 및 빌드 격리

## 증상

`Assets/Scenes/3_PPE_Room_3mode_loco.unity`를 HMD 없이 Game View 테스트 모드로 실행한 뒤
멀리 있는 `PPE/PPE_C_Tablet`의 마커를 클릭해도 태블릿이 선택되지 않는 것으로 보였다.
정상 단계까지 진행한 뒤에는 XRI 선택은 성공했지만 태블릿이 벽에 남아 마우스 포인터를
따라오지 않는 실제 결함도 확인됐다.

## 변경 요청과 보존 범위

- 이번 조사는 Game View에서 태블릿을 마우스로 선택하지 못한 원인을 확인하는 요청에 대응한다.
- 시나리오 카드, 모달, 텔레포트, PPE Grab의 기존 상태 순서와 입력 우선순위를 보존한다.
- Quest/OpenXR의 실제 컨트롤러 입력 경로를 Game View 마우스 입력으로 대체하지 않는다.
- Android/Quest 빌드에는 Editor 전용 XR Device Simulator가 개입하지 않아야 한다.

## 근본 원인

- 재현 당시 `PPEVoiceFlowDirector.CurrentState`는 `CardIntro`였다.
- 상태 소유자인 `PPEVoiceFlowDirector.ApplyPresentation`은 `CardIntro`에서
  `Scenario Card Canvas/Scenario Selection HUD`를 활성화한다.
- 카메라와 태블릿 마커 사이에는 활성 시나리오 카드
  `ScenarioCard3_Group/LeftColor_Card_1`의 `BoxCollider`가 있었다.
  카메라 기준 거리는 약 1.57m였고 태블릿 마커는 약 9.93m였다.
- 따라서 마우스 레이가 시나리오 카드를 먼저 맞힌 것은 입력 결함이 아니라
  현재 `CardIntro` 상태의 정상적인 입력 우선순위였다. 시나리오와 PPE 모드를 선택해
  태블릿 단계로 이동하기 전에는 뒤쪽 태블릿을 선택하면 안 된다.
- 초기 조사에서 태블릿 몸체 `BoxCollider`를 원인으로 추정했으나, 런타임
  `Physics.RaycastAll` 결과로 그 추정이 틀렸음을 확인했다. 이 추정을 바탕으로 한
  다중 Raycast 변경은 적용하지 않았다.
- `PpeArea`까지 정상 진행한 뒤 마커를 클릭하면 `Right_NearFarInteractor`의 선택 상태는
  `True`가 됐지만 태블릿의 월드 위치는 작성값 `(0.728, 0.218, 9.858)`에 그대로 남았다.
- 기존 `PhysicalHmdSimulatorGate`는 `PPEActionPanelController.ApproveUseByBodyProximity`인
  PPE에만 마우스 Grab Anchor를 연결했다. `PPETabletChecklistController`에는 해당 패널이
  없으므로 XRI 선택만 걸리고 마우스 위치를 따라가는 Anchor 갱신은 적용되지 않았다.

## 적용 변경

- `PhysicalHmdSimulatorGate.Awake`에서 `UNITY_EDITOR`가 아닌 Player는 시뮬레이터 루트를
  비활성화한 뒤 즉시 반환하도록 했다.
- 이 조건은 Android/Quest에서 OpenXR 장치 감지가 늦어져도 Game View용 가상 HMD와
  컨트롤러가 활성화되지 않게 한다.
- 태블릿 마커를 클릭하면 현재 카메라 방향과 작성된 마커 위치로 포인터 이동 평면을 만들고,
  선택 중에는 기존 Game View Mouse Grab Anchor가 그 평면 위의 마우스 위치를 따라가도록 했다.
- 태블릿의 작성된 Marker를 Grab Attach로 사용하므로 첫 클릭 순간 불필요한 위치 스냅을
  만들지 않으며, 해제 시 기존 Interactor/Grab Attach 설정을 복원한다.
- 태블릿 선택 Raycast와 시나리오 카드의 기존 차단 규칙은 변경하지 않았다.
- `PPELocomotionPpeRegressionValidationHarness`에 현재 태블릿의 작성된 마커 Collider 연결과
  포인터 평면 추적 경로, 비 Editor Player 격리 조건을 검사하는 항목을 추가했다.

## 영향 범위

- 변경됨: Game View 태블릿의 포인터 평면 추적, 비 Editor Player에서 시뮬레이터 활성화 분기 차단
- 보존됨: CardIntro 카드 입력 우선순위, 태블릿 Marker 전용 Grab, Quest/OpenXR 입력,
  텔레포트 및 교육 상태 전이, 서버 데이터 계약

## 별도 콘솔 오류 분리

- 정상적으로 새 Play Mode를 시작한 실행에서는 Meta XR Platform SDK와 PCLink/Skyline 런타임의
  버전 불일치 오류 1건만 재현됐다. 이 오류는 태블릿 마우스 입력과 별개이며 이번 작업에서
  Platform SDK 또는 PCLink 버전을 변경하지 않았다.
- 조사 도중 Play Mode가 켜진 상태에서 스크립트 재컴파일과 종료가 겹친 한 실행에서는
  `CurveVisualController.ComputeFallBackLine`의 `IndexOutOfRangeException`, `Invalid AABB`,
  `IsFinite(distanceForSort)`, `IsFinite(distanceAlongView)` 오류가 연속 발생했다.
- 해당 실행을 종료하고 컴파일 완료 후 새 Play Mode로 다시 검증했을 때 두 손의
  `CurveVisualController`와 `LineRenderer`는 Game View 시뮬레이터 경로에서 비활성 상태였고,
  LineRenderer 점과 Transform은 유한값이었다. 전체 태블릿 흐름을 다시 수행하는 동안
  AABB 계열 오류는 재발하지 않았다.
- AABB 오류의 기존 원인·증폭 경로는
  `Docs/Bug/2026-08-01_PPE_HandTest_Teleport_Ray_Input_Mediation.md`를 참고한다.

## 검증 상태

- 정적 확인: `PPE_C_Tablet`의 `XRGrabInteractable`에는 `XR Item Marker_small`의
  `SphereCollider` 한 개만 등록되어 있다.
- Unity Editor 확인: Game View 시뮬레이터, 태블릿, 양손 `NearFarInteractor`가 활성 상태이며
  런타임 상태가 `CardIntro`일 때 시나리오 카드 Collider가 태블릿보다 먼저 맞는 것을 확인했다.
- Unity Editor 확인: 수정 전 정상 흐름에서 태블릿 선택과 문서 완료는 성공했지만 태블릿이
  벽에서 움직이지 않는 것을 확인했다.
- Unity Editor 확인: 변경 후 `PpeArea / Education / ConfinedSpace` 흐름에서 작은 마커를
  클릭하고 마우스를 이동했을 때 태블릿이 포인터를 따라오는 것을 사용자가 직접 확인했다.
- Unity Editor 확인: 같은 실행에서 문서 완료 상태가 `DOCUMENT_COMPLETED=True`로 전환되고,
  해제 후 태블릿이 작성된 벽 위치로 복귀했으며 새 `Invalid AABB` 오류가 발생하지 않았다.
- Quest/OpenXR 확인 필요: APK 설치 후 시뮬레이터가 비활성 상태인지, 실제 컨트롤러로
  태블릿 Grab과 Trigger가 동작하는지 별도로 확인해야 한다.

## 현재 결론

- 클라이언트 Unity Editor: Game View 태블릿 포인터 추적과 문서 완료 검증 완료
- 클라이언트 정적 검증: 컴파일 오류 0건, 태블릿 전용 하네스 PASS, `git diff --check` 통과
- Android/Quest: Player에서 시뮬레이터가 실행되지 않는 컴파일 조건은 확인했으나 APK 실기 검증 전
- 서버/API/DB: 변경 영향 없음
- 공용 문서 미러링: Unity 전용 입력 결함이므로 `Docs/SharedDocumentManifest.md` 대상 아님
- Git: 문서와 코드 변경은 아직 커밋·푸시 전
