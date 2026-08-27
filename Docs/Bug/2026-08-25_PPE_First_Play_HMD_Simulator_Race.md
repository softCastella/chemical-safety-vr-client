# PPE 첫 Play HMD와 Game View 시뮬레이터 판정 경쟁

## 증상

- HMD가 연결된 상태에서 첫 Play Mode에 진입하면 HMD 머리 방향과 카메라가 함께 붙어 움직이는
  비정상 포즈가 발생한다.
- Play Mode를 종료하고 다시 시작하면 정상 HMD 추적으로 돌아온다.

## 최근 변경 후보

- `PhysicalHmdSimulatorGate`는 Game View 테스트를 위해 Play 시작 시 실제 `XRHMD`가 없으면
  XR Device Simulator를 자동 활성화한다.
- 최근 Game View 화살키·손 숨김 기능은 이 Gate가 Game View로 판정한 뒤에만 실행된다. 카메라
  포즈나 HMD 입력 자체를 HMD 세션에서 수정하는 코드는 추가하지 않았다.

## 확인한 실행 증거

- 첫 Play 진단 시 씬 작성값은 `m_ForceGameViewTestMode=False`,
  `m_EnableSimulatorWhenNoPhysicalHmd=True`였다.
- 같은 실행에서 `PhysicalHmdSimulatorGate.IsGameViewSimulatorActive=True`, Simulator 루트도
  활성 상태였다.
- Input System의 HMD 장치는 실제 HMD가 아니라 `XRSimulatedHMD` 하나만 남아 있었다.
- 같은 시점 OpenXR 진단 로그의 Runtime은 `Oculus`였다. 따라서 Oculus 런타임은 시작됐지만
  Gate가 `Awake()`에서 확인할 때 실제 HMD InputDevice 등록은 아직 완료되지 않은 상태였다.

## 근본 원인

- Gate는 `[DefaultExecutionOrder(-32000)]`의 `Awake()`에서 즉시 `InputSystem.devices`를 검사한다.
- 첫 Play에서는 OpenXR/Oculus의 실제 HMD 장치 등록보다 이 검사가 먼저 실행될 수 있다.
- 실제 HMD가 없다고 잘못 판단하면 Simulator 루트를 활성화한다. XRI Simulator는 활성화될 때
  실제 HMD 장치를 대신하고 `XRSimulatedHMD`를 등록하므로, 같은 Play 세션 안에서는 뒤늦게 실제
  HMD가 준비돼도 자동 복구할 수 없다.
- 두 번째 Play에서는 OpenXR 장치 준비가 이미 진행된 상태라 시작 판정이 달라져 정상처럼 보인다.

## 영향 범위

- HMD가 연결된 첫 Play의 카메라·머리 포즈와 컨트롤러 입력에 영향을 준다.
- Game View 시뮬레이터가 의도대로 선택된 세션에는 결함이 아니다.
- PPE, UI, 음성, Collider 자체가 근본 원인은 아니다.

## 적용한 변경

- 자동 Game View 판정을 `Awake()`에서 즉시 확정하지 않고 최대 3초 동안 실제 `XRHMD` 등록 또는
  실행 중인 `XRDisplaySubsystem`을 확인한 뒤 한 번만 확정하도록 변경했다.
- 명시적 `Game View Test Mode`만 Simulator를 즉시 켠다. 씬 작성값은 계속 꺼진 상태로 유지한다.
- Simulator가 실제 HMD를 제거한 뒤 재판정하는 방식은 사용하지 않는다. Simulator 활성화 전에
  판정을 완료해야 한다.
- 지연 판정 뒤 Game View가 활성화되더라도 이동 Provider가 활성화되는 시점에만 Game View 카메라를
  전방 기준으로 사용한다. Gate가 비활성이거나 Provider가 꺼질 때는 원래 작성값인
  `XR Origin (Hand Tracking)/Camera Offset/Hand Tracking Camera`를 복원한다.
- 화살키 입력 Reader도 Game View Gate가 활성일 때만 `leftHandMoveInput.bypass`에 연결하고,
  Gate가 비활성화되면 기존 bypass를 즉시 복원한다. HMD 경로에는 보조 Reader를 남기지 않는다.
- 화살키 이동·회전, 2.4배 이동 속도, 손 Renderer 숨김, 마우스 입력과 배경 클릭 음성 넘김은
  `IsGameViewSimulatorActive=True`인 세션에서만 실행된다.

## 검증 상태

- 정적 확인: Runtime과 Editor C# 빌드가 오류 0건으로 완료됐고, 씬에 대기 시간 3초,
  `m_ForceGameViewTestMode=False`, Game View 속도 2.4가 직렬화된 것을 확인했다.
- Unity Editor 확인: 현재 PC에 물리 HMD가 없는 첫 Play에서 3초 대기 뒤에만
  `XRSimulatedHMD`와 Game View Simulator가 활성화됐다.
- Unity Editor 확인: Game View에서 Provider 활성화 시 전방 기준이 `XR Origin (VR)/Main Camera`로
  전환되고, Provider 비활성화 시 원래 `Hand Tracking Camera`로 즉시 복원되는 것을 확인했다.
- Unity Editor 확인: 임시 `XRHMD` 장치를 이용한 격리 검사에서 물리 HMD 분기 조건이 참이고,
  Gate는 비활성, 작성된 이동 기준은 `Hand Tracking Camera`로 유지됐다. 임시 장치는 검사 직후 제거했다.
- Unity Editor 확인: Game View Gate 활성 시에만 화살키 bypass와 Game View 카메라 기준이 설치됐고,
  Gate 비활성 직후 `leftHandMoveInput.bypass=null`과 작성된 `Hand Tracking Camera`가 동시에
  복원됐다.
- Unity Console Error는 0건이고 `PPEGameViewControlSetup.Validate()`가 통과했다.
- Quest/OpenXR 확인: 실제 Quest를 연결한 첫 Play에서 Simulator 비활성, HMD 머리 포즈,
  스틱 이동, 손 표시를 다시 확인해야 한다. 격리 검사를 실제 헤드셋 성공으로 확대 해석하지 않는다.

## 수정 전 필수 판단

1. HMD용 카메라, 스틱, 손, UI와 씬 작성값은 변경하지 않는다.
2. Play 세션의 Simulator 사용 여부는 `PhysicalHmdSimulatorGate` 하나만 소유한다.
3. 판정 경로는 `명시적 Game View → 즉시 Simulator`, 그 외에는 `실제 XRHMD 또는 실행 중
   XRDisplaySubsystem 확인 → HMD 유지`, 둘 다 없으면 제한 시간 대기 후 Game View다.
4. 필수 Simulator·XR Origin 참조가 없으면 자동 생성하지 않고 명확한 오류로 멈춘다.
5. 영향 소비자는 카메라 포즈, Game View 입력, HMD 입력, 손 표시와 이동 방향이다.
6. 변경 전 기준은 첫 Play `Oculus Runtime + XRSimulatedHMD` 오선택이며, 변경 후 기준은 HMD가
   준비되는 동안 Simulator 루트를 비활성 상태로 유지하는 것이다.
7. 정적 확인, Unity 컴파일, Game View 지연 판정, HMD 첫 Play 장치 판정을 구분한다. 실제 Quest
   첫 Play는 헤드셋에서 다시 확인한다.

## 2026-08-26 추가 관찰: Quest Link 시스템 Ray 잔류

### 증상 구분

- Play 시작 뒤 한 손당 두 줄이 보인다.
- 안쪽 선은 PPE 플레이에서 사용하는 씬 작성 `NearFarInteractor/LineVisual`이다.
- 바깥쪽 선은 Play 전 Quest Link 공간에서 보이던 Meta 시스템 포인터와 같은 모양·기준으로
  남는다.

### 정적 조사 결과

- 대상 씬에는 좌·우 `NearFarInteractor`와 연결된 `LineVisual`이 손당 하나씩만 있다.
- 좌·우 `Teleport Interactor` 루트는 시작 시 비활성이고, 이동 모드 전환 전에는 선을 그리지 않는다.
- `XR Origin (Hand Tracking)`과 Game View용 XR Device Simulator 루트도 시작 시 비활성이다.
- Standalone과 Android XR Management에는 각각 `OpenXRLoader` 하나만 연결되어 있어 Oculus Loader와
  OpenXR Loader의 이중 시작 상태가 아니다.
- 해당 Quest Link 실행 로그는 OpenXR 세션이 `IDLE → READY → SYNCHRONIZED → VISIBLE → FOCUSED`로
  진입했음을 보여 준다.
- 씬에서 `LineRenderer` 외 방식으로 별도 선을 만드는 `OVRRayHelper`, `LaserPointer` 또는 두 번째
  Ray Visual 참조도 발견되지 않았다.

### 판단과 영향 범위

- 현재 증거로 바깥쪽 선은 Unity 씬의 중복 Ray가 아니라 Quest Link 시스템 메뉴·대시 또는
  컴포지터 계층이 앱 화면 위에 남긴 포인터로 분류한다.
- 씬의 `NearFarInteractor/LineVisual`을 끄면 안쪽의 정상 카드·UI Ray만 사라지고 바깥쪽 시스템
  포인터는 제거되지 않으므로, 이를 수정안으로 적용하지 않는다.
- PPE Grab marker, 카드 입력, 텔레포트 모드와 Ray 레이어 설정은 이번 조사에서 변경하지 않았다.

### 남은 실기 확인

1. 헤드셋에서 Meta Universal Menu와 Link Dash/데스크톱 패널을 모두 닫은 뒤 바깥쪽 Ray가
   사라지는지 확인한다.
2. 메뉴가 보이지 않는데도 바깥쪽 Ray만 남으면 Play를 종료하고 Quest Link 세션을 완전히 종료한
   뒤 재연결하여 같은 씬에서 비교한다.
3. 재연결 뒤에도 재현되면 당시 헤드셋 화면과 새 OpenXR 상태 로그를 함께 확보해 Meta Link
   컴포지터 회귀로 별도 추적한다.
