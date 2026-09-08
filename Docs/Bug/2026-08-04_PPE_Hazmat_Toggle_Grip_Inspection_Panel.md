# PPE 방호복 Toggle Grab·초기 오염·관찰 패널 회귀

## 2026-08-16 훈련·테스트 모드 장화 교육 음성 누출 관찰

- 훈련 모드에서 장화 상호작용 중 교육 모드 전용 멘트가 재생됐다.
- 테스트 모드에서도 같은 장화 교육 멘트가 재생됐다.
- 장화 Grab·판정 이벤트에서 교육 음성을 호출하기 전 활성 학습 모드를 검사하는지, 좌·우 장화가
  공유하는 1회 상태가 모드 전환 때 어떻게 초기화되는지 확인한다.
- 이 절은 사용자 통합 테스트 관찰 기록이며 아직 호출 로그 확인·수정·Play Mode 재검증 전이다.

## 2026-08-16 테이프 선사용 후 반대쪽 장갑 핸드 모델 미반영

- 방호복과 한쪽 장갑만 착용한 상태에서 테이프를 사용한 뒤 나머지 장갑을 착용하면, 나중에
  장착한 손의 핸드 모델에는 테이핑 시각이 적용되지 않았다.
- 테이프 처리 완료 상태와 장갑 장착 시각 갱신이 테이프 사용 순간에 착용 중인 손만 대상으로
  계산되는 결함 후보다.
- 테이프 상태 소유자는 완료 상태를 유지하고, 이후 좌·우 장갑 장착 이벤트가 들어올 때도 현재
  테이프 완료 상태를 기준으로 핸드 모델을 다시 계산해야 한다.
- 기존 최소 사용 조건과 좌·우 장갑 판정은 바꾸지 않으며, 반대쪽 장화 지연 장착에서도 같은
  상태 누락이 발생하는지 함께 확인한다.
- 아직 코드 경로 확인·수정·Play Mode·Quest/OpenXR 검증 전이다.

## 증상

- 방호복을 한 번 잡은 뒤 물리 Grip 입력을 놓으면 손 모델이 다시 펴졌다.
- Play Mode 최초 방호복이 오염 셰이더가 아닌 Clean 외형으로 표시됐다.
- 방호복을 잡아도 관찰 패널이 계속 활성화되지 않았다.
- 패널 부모만 활성화되고 배경, PPE 이름, 사용·폐기 버튼 및 `Icons` 부모가 씬에서
  비활성 상태라 내부 내용이 보이지 않았다.
- 사용·폐기 결과가 UI Button 클릭에만 연결돼 실제 컨트롤러 조작과 맞지 않았고,
  PASS 결과에서도 관찰용 방호복이 손에 남았다.
- 오염 방호복에서 폐기 PASS가 난 뒤 `GameObject is already being activated or
  deactivated.` 예외가 발생했다. 헤드셋에는 검은 화면과 모래시계가 표시됐고,
  오염 방호복은 사라졌지만 원래 위치에 Clean 방호복이 나타나지 않았다.

## 근본 원인

- `PPEMarkerToggleGrab`은 Hover 중 Interactor의 Select 입력을 `Toggle`로 바꾸지만,
  선택 직후 오브젝트가 이동하며 Hover가 종료될 때 XRI 선택 목록의 갱신 순서에
  따라 원래 입력 방식으로 너무 일찍 복원될 수 있었다. 이후 물리 Grip을 놓으면
  `selectExited`가 발생해 관찰 상태와 패널도 바로 종료됐다.
- `HandGripAnimator`는 컨트롤러의 물리 Grip 축만 읽었다. 따라서 논리적인 Toggle
  선택이 유지돼도 물리 입력을 놓으면 손 모델은 Open 자세로 돌아갔다.
- `PPEItemPresentationBinding.initialCondition`이 씬에 `Clean`으로 저장돼 있었다.
- 패널 위치 조정 후 부모뿐 아니라 고정 자식까지 비활성화된 작성 상태가 저장됐다.
- 시나리오 입력 계약인 `잡은 손 Trigger = 사용`, `오른손 A = 폐기`가 런타임에
  연결되지 않았으며 승인 결과는 피드백만 표시했다.
- PASS 지연 뒤 아직 선택 중인 `hazmat_suit_off`에 바로 `SetActive(false)`를 호출했다.
  이 비활성화 도중 XRI가 강제로 `selectExited`를 발생시켰고,
  `PPEMarkerToggleGrab.ReturnToAuthoredPose()`가 같은 활성 상태 변경 호출 스택 안에서
  `Transform.SetParent()`를 실행해 GameObject 활성화·비활성화 재진입 예외가 발생했다.

## 적용한 변경

- `PPEMarkerToggleGrab`이 자체 선택 Interactor 집합을 추적하도록 변경해 선택 중
  Hover 종료가 Select 입력 방식을 복원하지 않도록 했다.
- 선택 중인 Interactor와 같은 컨트롤러 아래에서 활성화된 `HandGripAnimator`를 찾아
  Toggle 선택이 끝날 때까지 외부 Grip 유지 소스를 등록한다.
- `HandGripAnimator`는 물리 Grip 축과 외부 Grip 유지 소스를 함께 반영하며, 컴포넌트
  비활성화 시 유지 소스와 Grip 값을 초기화한다.
- 방호복의 초기 상태를 `Contaminated`로 저장했다.
- 표시 지연이 0이면 `PPEActionPanelController`가 Coroutine 대기 없이 관찰 시작
  이벤트에서 즉시 패널을 활성화하도록 명시했다.
- 패널 배경, PPE 이름, 사용·폐기 버튼 및 `Icons` 부모는 씬에서 활성 상태로
  복구하고, 패널 부모와 동적 피드백·네 결과 아이콘만 초기 비활성 상태로 유지했다.
- XRI가 보고하는 선택 Interactor의 handedness에 따라 왼손 또는 오른손 Trigger를
  `사용`으로 받고, 오른손 `primaryButton`(A)을 `폐기`로 받도록 연결했다. UI Button은
  시각 표시로 유지하며 결과 실행을 위해 Ray/Poke 클릭에 의존하지 않는다.
- `UseApproved` 또는 `DiscardApprovedContaminated`이면 PASS 피드백을 0.25초 표시한
  뒤 `XRInteractionManager.CancelInteractableSelection()`으로 선택을 먼저 정상
  해제한다. `selectExited`와 씬 작성 위치 복귀가 끝난 다음 프레임에 결과를 적용한다.
- `DiscardApprovedContaminated`는 `hazmat_suit_off`를 비활성화하지 않고 같은 관찰용
  모델의 상태를 `Clean`으로 바꾼다. 따라서 오염 외형은 손에서 사라지고 Clean 외형이
  씬 작성 원위치에 다시 나타난다.
- `UseApproved`만 선택 해제와 원위치 복귀가 끝난 다음 프레임에 관찰용
  `hazmat_suit_off`를 비활성화한다. `hazmat_suit_on` 장착 연출은 다음 단계 범위다.

## 영향 범위

- `PPEMarkerToggleGrab`을 사용하는 Toggle Grab 오브젝트의 선택 유지와 손 모델 Grip
  표현에 영향을 준다.
- 방호복의 초기 오염 상태와 관찰 패널 활성 시점에 영향을 준다.
- 사용자가 작성한 방호복, 패널, 내부 UI 및 아이콘 Transform은 변경하지 않는다.
- 오염 폐기 PASS는 별도 복제본을 생성하지 않고 기존 관찰용 모델의 상태만 Clean으로
  전환한다. `hazmat_suit_on`과 착용 연출에는 영향을 주지 않는다.

## 완료한 검증

- Editor 검증이 방호복 초기 `Contaminated`, Toggle Grab 및 손 Grip 유지 설정을
  확인하도록 보강했다.
- 씬에서 `initialCondition: 1`, `useToggleGrip: 1`,
  `holdHandGripWhileSelected: 1`이 각각 한 번 저장됐음을 확인했다.
- 패널 초기 비활성 상태와 사용자 작성 RectTransform 값이 유지됨을 확인했다.
- 왼쪽 Near-Far Interactor의 handedness가 Left, 오른쪽이 Right로 저장돼 있음을
  확인했다.
- 고정 패널 자식은 활성, 동적 피드백과 네 결과 아이콘은 비활성으로 저장됐음을
  확인했다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드를 오류 0개로 완료했다. 경고
  7개는 기존 샘플 및 프로젝트 코드에서 발생한다.
- 디스크 씬에서 `hazmat_suit_off`가 활성, 초기 상태가 `Contaminated`, PASS 피드백
  지연이 0.25초로 유지됨을 다시 확인했다.
- 승인 결과 코드 경로에서 선택 중인 루트에 직접 `SetActive(false)` 하던 호출을
  제거하고, XRI 선택 취소 성공 여부를 확인한 뒤에만 결과를 적용하도록 변경했다.
- 사용자가 Quest/OpenXR Play Mode에서 `Contaminated → 폐기 PASS → Clean 원위치
  재생성 → Clean 사용 PASS → 손에서 제거` 흐름을 끝까지 확인했다.
- 위 전체 흐름에서 GameObject 활성 상태 변경 재진입 예외와 헤드셋 검은 화면·모래시계가
  재발하지 않았음을 확인했다.

## 아직 필요한 수동 검증

- 첫 Play Mode 진입 직후 방호복이 Contaminated 외형인지 확인한다.
- Grip을 한 번 눌러 잡은 뒤 물리 버튼을 놓아도 선택과 손 Grip 자세가 유지되는지
  확인한다.
- 잡는 즉시 패널이 표시되고 두 번째 Grip으로 놓을 때 패널이 숨고 방호복이 현재
  씬 작성 위치로 복귀하는지 확인한다.
- 잡은 손의 Trigger에서만 `사용`이 실행되고 오른손 A에서 `폐기`가 실행되는지
  좌·우 손 각각 확인한다.
- 좌우 컨트롤러와 Quest/OpenXR에서 같은 동작을 확인한다.

---

## 후속 회귀: 착용 Visual 시야 차단 및 위치 조정 불가

### 증상과 근본 원인

- Clean 방호복 사용 뒤 `hazmat_suit_on`이 몸에 붙지 않고 몸 앞쪽 위에 나타나 정면
  시야를 가렸으며, 사용자는 위치를 직접 조정할 수 없었다.
- `Img/입은방호복.png`에서 전체 방호복의 머리 부분이 HMD 높이보다 위에 있고 몸 전체가
  사용자 앞에 놓인 것을 확인했다.
- 최초 구현이 메시 원점을 몸 중심으로 가정해 머리 기준 `y=-0.85`, `z=0.28`을 적용했지만
  실제 모델 원점은 발 쪽이었다. 계산 Pose를 런타임에서 매 프레임 덮어써 별도 조정
  Transform도 없었다.
- PASS 직후 손의 관찰 모델과 큰 장착 모델이 겹친 상태에서 연출이 시작돼 이동이
  체감되지 않았다. Editor 로그에는 `[PPE Equip] ... animation completed`가 기록되어
  입력 누락이 아니라 위치와 표시 시점 문제임을 확인했다.

### 적용한 변경

- `Camera Offset/PPE Body Anchor` 아래에 `Hazmat Suit Approach Anchor`와
  `Hazmat Suit Equip Anchor`를 씬 오브젝트로 추가했다.
- 런타임은 빈 Body Anchor의 위치와 수평 Yaw만 갱신하고, 최종 위치·회전·크기는
  Equip Anchor의 씬 작성 Transform을 사용한다.
- Equip Anchor의 초기 위치를 머리 기준 `y=-1.6`, `z=-0.08`로 옮겼다. 이 값은 초기
  기준일 뿐이며 Play Mode에서 사용자가 Anchor Transform을 직접 조정할 수 있다.
- PASS 지연 경계에서 관찰 모델의 마지막 손 Pose를 보관한 뒤 선택 해제가 완료된 다음
  프레임에 장착 런타임 Visual을 생성하고 애니메이션을 시작하도록 순서를 변경했다.
- 원본 `hazmat_suit_on`은 계속 비활성 템플릿으로 유지하고 런타임 복제본만 이동한다.

### 영향 범위와 완료한 검증

- 방호복 사용 PASS 이후 장착 연출과 몸쪽 Pose에만 영향을 주며 패널, 아이콘, Grab,
  손 모델 및 원본 방호복 Transform은 변경하지 않는다.
- Body/Approach/Equip Anchor의 부모·자식 참조와 컨트롤러 직렬화 참조를 확인했다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드를 오류 0개로 완료했다. 경고 7개는
  기존 샘플 및 프로젝트 코드에서 발생한다.

### 아직 필요한 수동 검증

- PASS 뒤 장착 모델이 손 Pose에서 Approach Anchor를 거쳐 Equip Anchor까지 이동하는지
  확인한다.
- 정면 시야가 확보되고 아래를 내려다볼 때만 몸쪽 방호복이 자연스럽게 보이는지 확인한다.
- Play Mode에서 Equip Anchor Transform을 조정했을 때 런타임 Visual이 즉시 따라오는지
  확인하고 Quest 기준 최종 Pose를 확정한다.

---

## 후속 수정: 장착 Pose를 `hazmat_suit_on` Inspector로 통합

### 근본 원인

- 최종 착용 Pose가 `hazmat_suit_on`의 Transform이 아니라 별도 `Hazmat Suit Equip Anchor`에
  저장돼 있어, 사용자가 실제 장착 오브젝트를 조정해도 런타임 복제본의 최종 위치에는
  반영되지 않았다.
- 컨트롤러가 `PPE` 루트에 있고 검증·상태 전환이 `Tools > PPE`에 분산돼 있어 실제
  오브젝트와 설정의 관계를 Inspector에서 확인하기 어려웠다.

### 적용한 변경

- `hazmat_suit_on`을 `PPE Body Anchor`의 직접 자식으로 옮기고
  `PPEHazmatEquipController`도 해당 오브젝트에 배치했다.
- `hazmat_suit_on` GameObject와 Renderer는 Edit Mode에서 직접 Pose를 보며 조정할 수
  있도록 활성 상태로 저장했다. Play Mode의 첫 렌더 전에 컨트롤러가 Renderer를 숨기고,
  사용 PASS 뒤에는 런타임 복제본을 만들지 않고 이 Renderer만 다시 활성화해 애니메이션한다.
- 최종 위치·회전·크기는 `hazmat_suit_on`의 씬 작성 Transform을 직접 사용한다.
  별도 `Hazmat Suit Equip Anchor`와 해당 직렬화 참조는 제거했다.
- `PPEHazmatEquipController` 전용 Inspector에 최종 Pose 원본 안내, 실행 상태 및
  `Validate Hazmat Equip Setup` 버튼을 추가했다.
- 이번 방호복 작업에서 추가했던 역할 연결, 상태 전환, 패널 및 착용 검증용
  `Tools > PPE` 메뉴 항목을 제거했다. 상태 미리보기는 기존 `PPEInspectionState`
  Inspector를 사용하고, 같은 Inspector의 `Validate Hazmat Inspection Setup`과
  `hazmat_suit_on` Inspector의 `Validate Hazmat Equip Setup`으로 검증한다.

### 영향 범위

- Clean 방호복의 사용 PASS 이후 표시되는 `hazmat_suit_on` 착용 연출과 몸 추종 Pose에만
  영향을 준다.
- 관찰용 `hazmat_suit_off`, 선택 패널, 아이콘, Grab, 손 자세 및 다른 PPE의 작성값은
  변경하지 않는다.

### 완료한 검증

- `hazmat_suit_on`과 `PPEHazmatEquipController`가 동일 GameObject에 있고, 해당 Transform이
  `PPE Body Anchor`의 직접 자식이며 장착 Renderer가 Edit Mode 작성 활성 상태임을 씬 YAML에서
  확인했다.
- 제거한 Equip Anchor fileID와 이전 `EquipAnchor` 코드 참조가 남아 있지 않음을 확인했다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드를 오류 0개로 완료했다. 빌드 경고는
  기존 샘플 및 `MixerRoomFrontCapture`의 사용 중단 API 경고다.

### 아직 필요한 수동 검증

- Unity에서 `hazmat_suit_on`을 선택해 Transform을 조정한 뒤 Clean 방호복 사용 PASS 시
  조정한 위치·회전·크기로 착용되는지 확인한다.
- Quest/OpenXR에서 정면 시야를 가리지 않고 아래를 볼 때만 몸통 방호복이 자연스럽게
  보이는지 양안으로 확인한다.

---

## 2026-08-11 타이틀 테스트 씬 연출 작업

### 적용한 변경

- 원본 `Assets/Scenes/1_Title.unity`와 공용 `TitleSplashController`를 변경하지 않고,
  `Assets/Scenes/1_Title_Test.unity`와 전용 `TitleTestCanvasSequence`에서만 순차 연출을 구성했다.
- `Title_Logo_Canvas`를 먼저 표시한 뒤 페이드아웃하고, 이어서 메인 Canvas를 표시하도록 구성했다.
- 스플래시 Canvas를 비활성화하면 해당 오브젝트에 있던 코루틴도 중단되어 메인 Canvas가
  실행되지 않던 문제를 확인했다. 전용 시퀀스는 메인 Canvas에 연결해 스플래시 비활성화 뒤에도
  다음 단계를 계속 실행하도록 변경했다.
- 스플래시 로고의 첫 프레임 번쩍임을 줄이기 위해 초기 알파 0 처리, `Canvas.ForceUpdateCanvases()`,
  3프레임 예열과 프레임 진행량 제한을 전용 시퀀스에 적용했다.
- 메인 타이틀 로고와 파트너 로고는 각 최종 Y를 유지한 상태에서 오른쪽의 짧은 시작 위치에서
  왼쪽 최종 위치로 이동하며 페이드인하도록 구성했다.
- 메인 로고·파트너 로고 단계는 1.35초, 뒤따르는 `RightPanelArea` 단계는 0.75초 페이드로
  분리했다. `RightPanelArea`는 위치 이동 없이 제자리에서 나타난다.
- 사용자가 삭제한 `Title_Logo_Canvas`의 Version을 다시 만들거나 참조하지 않도록 전용 시퀀스에서
  스플래시 Version 필드, 초기화, 지연, 페이드인·페이드아웃 로직을 제거했다.
- `mainRightPanelRevealDuration`은 Inspector의 `Main Canvas Reveal` 섹션에서
  `RightPanelArea` 페이드 시간(초)으로 조정할 수 있게 정리했다.

### 우측 패널 디자인 제안

- 기존 `RightPanelArea`와 기존 버튼의 Transform, 스타일, 이벤트, 입력 경로는 수정하지 않았다.
- `Assets/Editor/TitleRightPanelProposalBuilder.cs`에 테스트 씬 전용 제안 오버레이 생성 메뉴를 추가했다.
  생성 대상은 기존 패널의 자식 `RightPanelProposal`이며, 모든 새 Graphic의 Raycast를 꺼 기존 버튼
  입력을 가로채지 않도록 했다.
- 제안 패널은 기존 디자인보다 과도한 정보 표식이 포함되어 현재 타이틀 화면의 단정한 밀도를 해친다는
  피드백을 받았다. 기본 기존 패널을 유지하며, 제안 오브젝트는 `Tools > Title > Remove Right Panel Design Proposal`로
  별도 제거할 수 있다.

### 근본 원인 및 영향 범위

- 초기 코루틴 중단의 근본 원인은 스플래시 Canvas와 시퀀스 코루틴의 소유자가 동일했고, 스플래시 종료를
  위해 그 오브젝트를 비활성화한 것이었다.
- 첫 프레임 로고 번쩍임의 원인은 XR/Canvas 초기화 중 큰 `unscaledDeltaTime`이 페이드 진행량으로 바로
  반영될 수 있던 점과 예열 절차 누락이었다.
- 변경 영향 범위는 테스트 씬과 전용 시퀀스, 디자인 제안 Editor 도구로 한정한다. 원본 타이틀 씬,
  공용 컨트롤러, Build Settings와 기존 우측 패널 오브젝트는 변경하지 않는다.

### 완료한 검증

- `TitleTestCanvasSequence.cs`, `1_Title_Test.unity`, `TitleRightPanelProposalBuilder.cs`의 정적 진단 오류가
  없음을 확인했다.
- 활성 메인 시퀀스가 메인 Canvas에 연결되고, `RightPanelArea` 페이드 시간이 `0.75`초로 저장됐음을 확인했다.
- 전용 시퀀스에 스플래시 Version 식별자와 노출 로직이 남아 있지 않음을 확인했다.
- `RightPanelProposal` 생성 도구의 새 Graphic은 모두 `raycastTarget = false`이며, 기존 버튼만 조회하고
  수정하지 않음을 정적 확인했다.
- 원본 `1_Title.unity` 및 공용 `TitleSplashController.cs`가 변경되지 않았음을 확인했다.

### 아직 필요한 수동 검증

- Unity에서 `1_Title_Test`를 열어 스플래시 후 메인 Canvas가 실제로 이어지는지 확인한다.
- 로고·파트너 로고의 수평 이동 및 1.35초 페이드, 이어지는 `RightPanelArea` 0.75초 페이드 감각을
  Game View에서 확인한다.
- 스플래시 Canvas에 Version이 표시되지 않는지 확인한다.
- `RightPanelProposal`을 생성해 비교할 경우, 기존 우측 버튼의 XR Ray/Poke 입력이 그대로 동작하는지 확인한다.
- Quest/OpenXR에서 양안 렌더링과 첫 프레임 번쩍임 여부를 확인한다.

---

## 후속 수정: 착용 완료 후 방호복 손 모델 전환

### 근본 원인

- 씬에는 일반 손인 `LeftHand_BareHand`/`RightHand_BareHand`와 방호복 손인
  `LeftHand_BareHand_Suit`/`RightHand_BareHand_Suit`가 모두 준비되어 있었지만,
  방호복 착용 완료 상태와 두 손 모델의 활성 상태를 연결하는 런타임 처리가 없었다.
- 따라서 방호복 몸통이 장착된 뒤에도 손은 일반 맨손 모델로 계속 표시되었다.

### 적용한 변경

- `PPEHazmatEquipController`에 착용 전·후 양손 GameObject 참조를 Inspector 직렬화 값으로
  추가하고, 씬에서 왼손/오른손 순서로 각 모델을 연결했다.
- Clean 방호복 사용 PASS 뒤 장착 애니메이션이 최종 Pose에 도착하면 일반 손 두 개를
  숨기고 `BareHand_Suit` 두 개를 활성화한다.
- Play Mode 시작과 컨트롤러 비활성화 시에는 일반 손을 활성화하고 수트 손을 비활성화해
  씬 작성 초기 상태를 복구한다.
- 새 모델을 먼저 활성화하고 기존 모델을 같은 프레임에 숨겨 전환 중 손이 비어 보이는
  프레임을 방지했다.
- Inspector 검증에서 네 손 참조의 중복·누락, 좌우 이름과 `HandGripAnimator` Side,
  `Animator`, 초기 활성 상태 및 대응 손끼리의 부모·로컬 위치·회전·스케일 일치를 확인한다.

### 영향 범위

- Clean 방호복 장착 애니메이션이 완료되는 순간의 왼손·오른손 표시 모델에만 영향을 준다.
- 두 모델에 이미 작성된 손 추적 Transform, `HandGripAnimator`, `Animator` 설정을 그대로
  사용하며, 사용자가 조정한 `hazmat_suit_on` Transform은 변경하지 않는다.

### 완료한 검증

- 씬 YAML에서 일반 손은 작성 활성, 수트 손은 작성 비활성이며 네 참조가
  `PPEHazmatEquipController`에 왼손/오른손 순서로 연결된 것을 확인했다.
- 일반 손과 대응 수트 손의 부모, 로컬 위치·회전·스케일이 일치하고 양쪽 모두
  `HandGripAnimator`와 `Animator`를 가진 것을 확인했다.
- Unity 씬 YAML의 중복 fileID가 0개이고, 장착 컨트롤러가 한 개이며 네 손 참조 블록이
  정확한 것을 확인했다. 이 과정에서 사용자가 조정한 `hazmat_suit_on` 로컬 Transform
  `(0.028, -0.911, -0.173)`과 회전·스케일은 변경하지 않았다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드를 오류 0개로 완료했다. 경고 7개는
  기존 XRI 샘플, SlimUI 및 `MixerRoomFrontCapture`의 사용 중단 API/미사용 필드 경고다.
- Unity Asset Pipeline에서 수정된 스크립트와 씬을 다시 불러왔으며 `error CS` 또는
  `Scripts have compiler errors`가 기록되지 않은 것을 Editor 로그에서 확인했다.

### 아직 필요한 수동 검증

- Quest/OpenXR에서 Clean 방호복 사용 PASS 뒤 착용 애니메이션이 끝나는 순간 양손이
  `BareHand_Suit` 외형으로 바뀌는지 확인한다.
- 전환 뒤 좌우 손 추적, Grip 자세와 Trigger 입력이 일반 손과 동일하게 동작하는지 확인한다.
- 양안에서 손이 한쪽 눈에만 보이거나 위치가 어긋나는 문제가 없는지 확인한다.

## 2026-08-05 회귀 기록: 풀장착 방호복 부모 기준·거울 Y 위치·패널 잔존

### 증상

- 풀장착 모델을 에디터에서 바닥에 맞췄지만 거울에서는 Y 위치가 적용되지 않고 공중에 뜨는 것처럼 보이는 현상이 보고되었다.
- 장화·옷·마스크가 부모 기준으로 함께 움직이지 않고 서로 다른 위치·방향으로 렌더링되는 현상이 있었다.
- 착용된 PPE가 이미 풀장착 모델에 포함된 뒤에도 패널을 계속 호출하는 것처럼 보였다.
- 장착 모델의 자식 오브젝트에서 `Rigidbody` 및 패널/marker 참조 관련 콘솔 오류가 반복되었고, 한때 43개 오류가 표시되었다.

### 근본 원인 해석

- 자식 장착품의 월드 Transform을 별도로 맞추거나 런타임에 재배치하면 방호복 기준의 작성 Pose와 분리된다.
- 거울 반사 카메라가 루트의 작성 Y가 아닌 HMD 높이 또는 별도 오프셋을 따라가면 에디터에서 맞춘 바닥 위치와 거울 위치가 달라진다.
- 풀장착 모델의 시각 자식과 개별 PPE 상호작용/패널 컴포넌트가 동시에 활성화되면 동일한 장비에 대해 중복 입력·패널 호출이 발생할 수 있다.
- 런타임이 없는 Rigidbody나 누락된 직렬화 참조를 자동으로 고치는 방식은 콘솔 오류를 숨기지 못하고 초기화 비용과 추가 회귀를 만든다.

### 적용한 변경

- `hazmat_suit_on_10`을 `PPE Body Anchor` 아래의 기준 부모로 두고, 장화·마스크·등지게 등을 자식으로 유지하는 방향으로 정리했다.
- 사용 처리된 PPE 자식만 보이도록 `PPEEquipmentVisualController`를 추가했다.
- 착용 애니메이션 이후 사용자가 작성한 Pose를 보존하도록 `PPEHazmatEquipController`의 authored pose 처리 경로를 조정했다.
- Body Anchor 추적에서 X/Z와 Yaw만 갱신하고 작성된 Y를 보존하는 방향을 적용했다.

### 영향 범위와 검증 상태

- 풀장착 PPE 시각 표시, 거울 반사, 장화의 바닥 접지, 착용 애니메이션, 개별 PPE 패널/Grab 컴포넌트에 영향을 준다.
- 씬 계층·직렬화 참조와 C# 빌드 오류 0개는 확인했다.
- Unity 라이선스 문제로 실제 거울 화면에서 바닥 접지, 자식 정렬, 회전 시 몸에 부착된 장비가 분리되지 않는지, 사용 PPE만 표시되는지는 아직 검증하지 못했다.
- 패널 참조·marker Collider·Rigidbody 오류는 Inspector/씬 직렬화 기준으로 정리한 뒤 콘솔 0 오류를 확인해야 한다.

## 2026-08-06 회귀 기록: 장화 오염 표면 이탈·마스크 균열 가시성 부족

### 증상

- 장화 시멘트·자갈 오염이 장화 표면이 아닌 바깥에 떠 보였다.
- 검정 장화에 검정 계열 오염 재질이 사용되어 오염이 거의 보이지 않았다.
- 마스크 균열이 고정된 좌표에 생성되어 실제 마스크 표면과 맞지 않거나 위치를 식별하기 어려웠다.

### 원인과 적용한 수정

- 기존 `PPEDefectVisualSetup`이 장화와 마스크의 실제 Bounds/표면 방향을 계산하지 않고 고정된 `z` 좌표를 사용했다.
- 장화 시각물은 실제 Bounds의 양면 표면 오프셋을 기준으로 생성하고, 자식 로컬 좌표를 중복 적용하지 않도록 수정했다.
- 오염 재질을 밝은 시멘트·황갈색 자갈로 변경해 검정 장화와 대비시켰다.
- 마스크 균열은 실제 Bounds 중심·크기와 양면 표면 기준으로 생성하고, 고대비 불투명 재질과 Cull Off를 적용했다.

### 검증 상태

- `Assembly-CSharp-Editor.csproj` 컴파일: 오류 0개.
- Unity batch 적용은 Licensing Client 연결 실패로 씬 저장 단계까지 도달하지 못했다. 따라서 현재 씬에 수정된 시각물이 적용됐다고 보고하지 않는다.
- Unity 라이선스가 정상화되면 `Tools > PPE > Apply Defect Visuals (HandTest Scale)` 실행 후 Scene View, Game View, Quest 양안에서 장화 표면 부착과 마스크 균열 가시성을 확인해야 한다.

## 2026-08-06 추가: 풀장착 본체 테이핑 FBX (`taped`)

### 요청

- `hazmat_suit_on_10` 아래 `taped` FBX를 거울에 보이게 하고 URP Unlit 색으로 표시한다.

### 적용

- Unlit 머티리얼: `Assets/Materials/PPE/Scene Unlit/Taped/taped_Unlit.mat` (`taped_basecolor`)
- Editor: `Assets/Editor/PPETapedFbxUnlitSetup.cs`
  - `Tools/PPE/Convert Taped FBX Materials to URP Unlit`
  - `Tools/PPE/Place Taped Under Full Suit And Wire Slot`
- PackingTape `PPEItemPresentationBinding` 슬롯에 연결. 초기 비활성, 사용 승인 후 표시.
- Play Mode에서 풀장착과 함께 Mirror Only 레이어(30)로 승격되어 거울에 반사된다.
- 기존 손목 손 모델 테이핑 경로는 유지한다. 이번 경로는 방호복 본체 테이핑 메시다.

### 수동 검증

1. Unity에서 `_scale_0`을 연 뒤 위 메뉴를 실행한다(이미 `taped`가 있으면 포즈 보존).
2. 테이프 사용 승인 후 `taped`가 켜지고 거울에 Unlit로 보이는지 확인한다.

## 2026-08-06 추가: 풀장착 모델 바닥 관통·키 불일치

### 증상

- 거울 앞에서 본 사용자 키와 풀장착 모델 키가 다르다.
- 풀장착 모델이 바닥에 꽂혀 있다.

### 확인 결과

- `Generated Image Room/Floor` 월드 상단면 ≈ **Y −0.840** (`PPE Background Room` scale 0.8 반영).
- `PPEHazmatEquipController.UpdateBodyAnchor`는 HMD의 **X/Z·Yaw만** 갱신하고 **Y는 작성값 유지**한다. 바닥에 꽂히는 원인은 런타임 추적이 아니라 `hazmat_suit_on_10` 작성 Pose다.
- local Y **−0.715**일 때 장화 피벗이 바닥과 거의 같다(좌 ≈ −0.841). 피벗보다 아래인 발바닥 메시가 Floor에 박힌다.
- 장화→헬멧 피벗 신장 ≈ **1.13 m**. 성인 눈높이(~1.6 m)보다 짧아 키 불일치가 난다. 바닥 Y만 올려서는 키가 맞춰지지 않는다.

### 적용

- `_scale_0` `hazmat_suit_on_10.localPosition.y`: **−0.715 → −0.635** (+0.08 m, 발 클리어런스 추정).
- 키 맞춤용 `localScale` 확대와 재접지는 아직 적용하지 않았다.

### 영향 범위

- 풀장착 모델·자식 PPE의 월드 높이, 거울 반사 접지, 사용자와의 상대 키에 영향을 준다.
- Body Anchor 추적 로직과 개별 PPE Grab/패널은 변경하지 않았다.

### 완료한 검증 / 남은 검증

- Floor 월드 Y, Body Anchor Y 보존, 장화/헬멧 피벗 상대 높이를 씬 YAML로 정적 확인했다.
- Scene View에서 발 클리어런스, Play Mode·거울에서 접지·키 비교, 필요 시 스케일 확대 후 Y 재접지는 수동 확인이 남는다.

## 2026-08-06 수정: Body Anchor가 바닥에 처박히던 원인

### 증상

- 풀장착 모델(`hazmat_suit_on_10`)이 Floor에 처박혀 보인다.
- 오전에는 걸린 방호복 앞에 세워 둔 높이와 거울 앞 높이가 같았는데, suit Y만 올리거나 내리면 한쪽은 뜨고 한쪽은 박히는 식으로 틀어졌다.

### 근본 원인

- `PPE Body Anchor`가 `Camera Offset` 자식인데, `UpdateBodyAnchor`가 **월드 Y를 매 프레임 고정**했다.
- Play에서 `CameraYOffset`(씬 값 1.1176)으로 Offset이 올라가면, 월드 Y 고정을 유지하려고 Body Anchor **local Y가 음수로 밀린다**.
- 그 결과 풀장착 모델이 Floor에 처박혀 보인다. **suit Y(−0.715)만의 문제가 아니라 Body Anchor 추종 버그**다.
- suit Y만 크게 올리면 Scene에서는 뜨고, 월드 Y 고정 경로에서는 거울/착용 높이와 또 어긋난다.

### 잘못된 시도 (기록)

- suit Y만 −0.715 → −0.635 → −0.09 → −0.704로 반복 조정: 뜨거나 박힘만 오갔다.
- 런타임 Floor Bounds 자동 접지: 증상 우회일 뿐 Body Anchor 부모/월드 Y 고정 문제를 남긴다.
- Camera Offset 아래로 되돌리기만 한 복구: 오전 Pose는 맞지만 월드 Y 고정 버그는 그대로였다.

### 적용 (최종)

| 항목 | 값 |
| --- | --- |
| 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` |
| Body Anchor 부모 | `Camera Offset` → **`XR Origin (VR)`** |
| Body Anchor local | **`(0.37, 0.06, 8.994)`** (Y 0→0.06, 발 클리어런스) |
| `hazmat_suit_on_10` local Y | **−0.715** (오전 작성값 유지) |
| `UpdateBodyAnchor` | 부모 local X/Z만 머리 추종, **작성 local Y 유지**, 월드 Y 고정 제거 |

- 코드: `Assets/Scripts/PPEHazmatEquipController.cs`
- 검증 경로: `Assets/Editor/PPEHazmatEquipValidation.cs` (BA 부모 = XR Origin 허용)
- 모션 설정 경로: `Assets/Editor/PPEHazmatEquipMotionSetup.cs` → `XR Origin (VR)/PPE Body Anchor`

### 영향 범위

- 풀장착 모델·거울 반사 접지, Body Anchor 추종, 착용 연출 최종 Pose.
- 개별 PPE Grab/패널, 거울 RenderTexture 로직은 변경하지 않았다.

### 완료한 검증 / 남은 검증

- 씬 YAML: BA father=`XR Origin`, CO 자식 목록에 BA 없음, suit Y=−0.715, BA Y=0.06.
- `PPEHazmatEquipController` 컴파일 오류 0.
- Scene·Play·거울·Quest에서 발 접지 수동 확인은 남는다. 미세 조정 시 Body Anchor Y 또는 suit Y 중 **하나만** 소량 변경한다.

## 2026-08-09 장화 오염 및 마스크 유리 균열 시각 복구

### 적용한 변경

- `boots_R`, `boots_L`의 오염은 생성된 구멍·시멘트·자갈 메쉬가 아니라, 기존 장화 Renderer에 오염 상태일 때만 적용되는 황갈색 표면 색으로 변경했다.
- 장화의 `PPE_Defect_Visuals`에는 생성 자식을 남기지 않아 Grab Collider, 원거리 Ray, 물리 상호작용에 영향을 주지 않는다.
- 마스크 균열은 렌즈 Bounds의 앞·뒤 표면에 배치하고, URP Unlit 투명 재질(알파 블렌드, ZWrite Off, Cull Off) 두 종류로 구성했다.

### 근본 원인

- 기존 장화 결함은 장화 표면과 독립된 오버레이 메쉬여서, 시점에 따라 떠 보이거나 이물질처럼 보였다.
- 균열 재질은 유리 렌즈의 투명도·양면 표시 설정이 명시되지 않아 유리 표면 효과가 약했다.

### 영향 범위

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 대상 코드: `Assets/Scripts/PPEConditionVisualAppearance.cs`, `Assets/Editor/PPEDefectVisualSetup.cs`
- PPE Grab, 카드, 모달, 텔레포트, 거울 갱신 설정은 변경하지 않았다.

### 완료한 검증 / 남은 검증

- Unity 컴파일 및 장화 표면 오염/마스크 유리 균열 Editor 검증을 통과했다.
- Scene View 미리보기에서 장화에 별도 메쉬가 없고, 균열이 청백색 반투명 선으로 렌즈에 표시됨을 확인했다.
- Game View, Play Mode, Quest/OpenXR 양안에서의 대비·두께·가시성은 수동 검증이 남는다.

## 2026-08-09 Game View 테스트 모드 및 장화 화학 오염 복구

### 입력 경로

| 모드 | 입력 장치 | Interactor / 처리 | 범위 |
| --- | --- | --- | --- |
| Game View Test Mode OFF | Quest/OpenXR 컨트롤러 | 기존 XR Interactor, XRUIInputModule, Select/Press action | 실제 헤드셋 실행 |
| Game View Test Mode ON | 마우스·키보드 | XR Device Simulator, Game View Mouse UI, 마우스 클릭 보조 경로 | Unity Game View 테스트 전용 |

- `Game View XR Test Input Gate` Inspector의 **Force Game View Test Mode** 체크박스를 켜면, Quest Link가 연결돼 있어도 다음 Play 시작 시 마우스·키보드 시뮬레이터를 강제로 사용한다.
- 이 모드에서는 시뮬레이터가 물리 HMD 입력을 대신하므로 헤드셋 검증에는 사용하지 않는다. 기본값은 OFF다.

### 장화 오염 변경

- 단색 표면 색조 방식은 제거했다. `boots_R`, `boots_L`은 각각의 원본 장화 베이스 텍스처를 유지하면서 `Tyche/PPE/Hazmat Contamination` 셰이더와 `HazmatSuit_ChemicalStainMask.png`를 사용한다.
- 화학 얼룩은 방호복 관찰용과 같은 마스크 방식이며, 장화 UV에 맞는 좌·우 재질을 따로 사용한다. 별도 얼룩 메쉬·Collider는 만들지 않는다.

### 완료한 검증 / 남은 검증

- Unity 컴파일 성공, Game View Test Mode 기본값 OFF, 양쪽 장화의 셰이더·원본 베이스 텍스처·화학 얼룩 마스크 바인딩을 확인했다.
- Scene View 오염 미리보기에서 검정 장화 위에 황갈색 불규칙 얼룩이 표시됨을 확인했다.
- 실제 Game View에서 화살키 이동/마우스 클릭, Quest/OpenXR 양안에서 얼룩의 가시성은 수동 검증이 남는다.

## 2026-08-09 PPE 최종 진행·장화 초기 오염·컨트롤러 가이드 점검

### 근본 원인과 적용

- `PPEVoiceTeleportEventRelay`의 `m_CenterMarker`와 `m_MirrorMarker`가 같은 `Teleport_2` Anchor를 가리켰다. 센터는 `Teleport_1`, 거울은 `Teleport_2`로 분리했다. 거울 도착에서만 013 음성 → 음성 종료 대기 → 관찰 게이지/완료 판정 → 퀴즈 복귀 흐름이 시작된다.
- 장화 초기 오염은 `PPEInspectionState`와 `PPEConditionAppearance`의 Enable 순서에 따라 화학 얼룩 강도 PropertyBlock이 Clean(0)으로 남을 수 있었다. 상태가 Inspector 초기값으로 복원될 때 조건 변경 이벤트를 한 번 발행하도록 수정했다.
- Quest Link가 일반 XRController와 OculusTouchController 입력을 한 프레임에 함께 보고하면 미니 가이드 토글이 두 번 실행될 수 있다. 동일 프레임 중복 콜백은 하나로 합쳤다.

### 입력 경로 확인

| 기능 | 입력 | 처리 |
| --- | --- | --- |
| 텔레포트 모드 | `Primary2DAxis` 방향축 | `PPEControllerTeleportModeManager` |
| 미니 가이드 | `Primary2DAxisClick` 클릭 | `ControllerGuideMiniActivator` |

- 텔레포트와 미니 가이드는 다른 입력을 사용하므로 텔레포트 영역 자체가 스틱 클릭을 소비하는 구조는 아니다.

### 검증

- Game View Play 시작 직후 양쪽 장화가 `Initial=Contaminated`, `Current=Contaminated`, 화학 얼룩 강도 `1`로 적용됨을 확인했다.
- 거울/센터 Anchor 참조가 `Teleport_1`/`Teleport_2`로 서로 다르게 저장됐음을 확인했다.
- 실제 XRI `SelectEnter → SelectExit` 재현에서 PPE `helmet` 호스트는 부모·위치·회전을 작성값으로 복귀했다. 실제 헤드셋에서 선택이 해제되지 않는 재현은 별도 수동 확인이 남는다.
- Quest/OpenXR에서 실제 스틱 클릭과 거울 도착 후 최종 퀴즈 표시는 수동 검증이 남는다.

## 2026-08-09 후속: 마지막 PPE 음성·테이프 반복·완료 카드 표시

### 재현된 원인

- `PPEVoiceFlowDirector.OnAnyPpeChoiceResolved`는 마지막 슬롯인지 확인하지 않고 모든 일반 PPE 사용 승인에 `m_NextPpeEquippedVoice`를 재생했다. 실제 장착 슬롯은 승인 지연 뒤에 완료되므로, 이후 `Update`의 012 이동 안내가 재생되기 전에 “다음 PPE” 음성이 먼저 나왔다.
- 테이프는 첫 사용 승인 뒤 `PPEActionPanelController`가 검사 물체를 숨기고, `PPEEquipmentVisualController`가 단일 PackingTape 슬롯을 즉시 완료 처리했다. 따라서 남은 장갑/장화를 다시 테이핑할 수 없었다.
- `PPEFinaleController`는 복귀 때 `Scenario Card Canvas` 루트만 켰다. `PPEVoiceFlowDirector`의 `PpeArea` 상태는 하위 `Scenario Selection HUD`를 끈 상태이므로, 카드 Canvas가 보이지 않았다.
- 방호복(`hazmat_suit_off`) 패널은 사용/폐기 pass·error 네 참조가 각각 하나의 하단 아이콘으로 중복 연결돼 오답 피드백이 반대 위치 또는 같은 위치로 보였다.

### 적용한 변경

- `PPEEquipmentVisualController.WillAllRequiredSlotsBeUsedAfter`로 현재 승인된 패널이 마지막 장착 슬롯인지 먼저 판정한다. 마지막 PPE에는 011 “다음 PPE”를 생략하고, 장착 애니메이션 완료 뒤 기존 012 “거울로 이동” 조건만 재생한다.
- 테이핑 상태를 장갑 좌/우·장화 좌/우별로 유지한다. 한 번의 테이프 사용은 이미 착용한 여러 짝을 함께 처리하며, 미테이핑 짝이 하나라도 있으면 테이프 물체를 유지한다. 마지막 짝까지 테이핑하면 PackingTape 슬롯을 완료하고 테이프를 숨긴다.
- `tape`만 `hideInspectionVisualAfterUse=false`로 씬에 작성했다. 다른 PPE의 사용 후 제거 동작은 유지한다.
- 완료 복귀 시 `PPEVoiceFlowDirector.ShowScenarioCardsAfterCompletionReturn`이 기존 Canvas와 `Scenario Selection HUD`를 함께 활성화한다.
- 방호복 패널의 use/discard pass/error 아이콘과 `PPEActionPanelSharedPresentation` 참조를 각기 분리된 작성 아이콘으로 복구했다.

### 검증과 남은 수동 확인

- Unity 컴파일 성공. Play Mode에서 장화·헬멧·마스크 패널의 오답 `Use` 아이콘은 Use 위치, 오답 `Discard` 아이콘은 Discard 위치에 각각 활성화됨을 확인했다. 중복 참조를 복구한 방호복 패널도 Play Mode에서 오답 `Use` 아이콘이 Use 위치에만 활성화됨을 확인했다.
- 실제 Quest/OpenXR에서 다음 순서의 수동 확인이 필요하다.
  1. 마지막 PPE 승인 때 011이 나오지 않고, 장착 완료 뒤 012만 나오는지 확인한다.
  2. 장갑/장화를 일부 먼저 착용한 뒤 테이프를 사용하고, 미테이핑 짝이 남아 있으면 테이프를 다시 잡을 수 있는지 확인한다.
  3. 네 짝을 모두 테이핑한 마지막 사용 뒤 테이프가 사라지는지 확인한다.
  4. 거울 판정·복귀 뒤 `Scenario Selection HUD`의 카드가 표시되는지 확인한다.
- `helmet_wrong`은 현재 비활성화된 보조 자식이며 실제 상호작용은 부모 `PPE/helmet` 하나가 소유한다. 별도 호출/배치 방식으로 바꾸려면 등장 시점과 위치가 필요하므로 이번 변경에서는 구조를 임의로 변경하지 않았다.

## 2026-08-09 후속: GitHub 퀴즈 브랜치 선별 통합

### 발견 경위와 근본 원인

- 현재 작업 브랜치 `260808_아이콘변경_QA/개선문서_버그수정`의 `ecc7625`와 퀴즈 브랜치 `260808_퀴즈추가`의 `6cd98f5`는 같은 기준 커밋 `73ba806`에서 분기되어 있었다.
- 두 브랜치를 합친 원격 커밋이 없어, 현재 브랜치에는 퀴즈 UI·`PPEQuizController`·016 퀴즈 음원·거울 이후 퀴즈 진입 흐름이 없었다. 파일 삭제가 아니라 미병합 상태였다.

### 적용한 변경

- 현재 버그 수정본을 기준으로 유지하고, 퀴즈 브랜치에서 다음 요소만 선별 통합했다.
  - `Assets/Scripts/PPEQuizController.cs`
  - `Assets/Audio/Voice/4_VO_PPE_EDU_016_Quiz.ogg`
  - `PPE Quiz World Canvas`의 씬 작성 5문제 UI와 각 3개 선택지
  - `PPEVoiceFlowDirector`의 퀴즈 시작/완료 음성 연결
  - `PPEFinaleController`의 `거울 판정 → 퀴즈 대기 → 퀴즈 완료 후 완료 음성·복귀` 순서
- 퀴즈 진입은 전체 PPE 장착 상태만 확인하며, 태블릿 체크리스트 완료 여부가 퀴즈를 막지 않도록 했다.
- 퀴즈 완료 뒤에는 기존 페이드·복귀·시나리오 카드 Canvas 표시 경로를 그대로 사용한다.
- 패널 아이콘, 반복 테이프, 마지막 PPE 음성, 복귀 카드 Canvas 등 같은 날의 현재 브랜치 수정은 덮어쓰지 않았다.

### 검증

- Unity 컴파일 성공.
- 현재 씬에서 `PPEQuizController`의 5개 페이지, 페이지 루트, Voice Flow 참조, 016 퀴즈 음원 참조가 모두 연결된 상태로 저장됐음을 확인했다.
- 실제 Quest/OpenXR에서 5문제의 정답/오답 입력, 정답 후 1초 다음 문제 전환, 완료 음성·복귀는 사용자 실행으로 정상 동작을 확인했다.

## 2026-08-09 신규 회귀: 하자 헬멧을 놓을 때마다 위로 누적 이동

### 증상

- `PPE/helmet_wrong`을 잡았다가 놓으면 테이블의 작성 위치로 돌아가지 않는다.
- 같은 동작을 반복할수록 하자 헬멧이 매번 위쪽으로 더 이동한다.
- 정상 헬멧은 하자 헬멧 폐기 성공 뒤에만 활성화되는 형제 오브젝트이며, 이 증상은 폐기 전 하자 헬멧의 Grab/Release에서 발생한다.

### 현재 구조와 확인한 사실

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- `PPE/helmet_wrong`와 `PPE/helmet`은 같은 부모 `PPE` 아래 형제이며, 시작 Local Position/Rotation/Scale은 동일하다.
- 하자 헬멧에는 `PPEMarkerToggleGrab`과 non-kinematic `Rigidbody`가 활성 상태로 연결돼 있다.
- `PPEMarkerToggleGrab.returnToAuthoredPoseOnRelease`는 활성화돼 있고, 코드상 `selectExited` 및 선택 해제 뒤 `LateUpdate`에서 시작 시 기록한 부모·Local Transform으로 복귀하도록 작성돼 있다.
- 에디터 정적 확인과 폐기 후 하자 Off → 정상 On 전환만 확인했다. 실제 헤드셋에서 Grab을 놓는 순서와 물리 결과는 이번 기록 시점에 재현됐으며, 아직 수정하거나 우회하지 않았다.

### 근본 원인 상태

- **미확정.** `PPEMarkerToggleGrab`의 작성 Pose 복귀와 `XRGrabInteractable`/`Rigidbody`의 Release 처리 순서가 충돌하는지가 첫 번째 후보이다.
- 하자 헬멧은 정상 헬멧을 복제해 형제로 분리한 직후이므로, 복제된 `XRGrabInteractable`의 attach transform, Rigidbody 제약, marker Collider, grab transformer 및 부모 변경 여부를 원본 정상 헬멧과 Play Mode 전후로 대조해야 한다.
- 헤드셋/XR 런타임 원인으로 확장하기 전에 위 최근 변경(형제 분리·복제)과 씬 직렬화 차이를 먼저 비교한다.

### 영향 범위

- 하자 헬멧을 잡고 놓는 동작과 그 뒤 폐기 패널 진입에 영향을 준다.
- 정상 헬멧 활성화, PPE 장착 슬롯, 음성/퀴즈/거울 흐름은 이 증상만으로 변경하지 않는다.

### 다음 재개 시 검증 순서

1. 하자 헬멧의 첫 Grab 전/Release 직후/다음 프레임의 부모·월드/Local Transform, `Rigidbody` 속도, `isSelected`를 기록한다.
2. 같은 항목을 정상 헬멧에 일시적으로 적용해 비교한다.
3. `PPEMarkerToggleGrab`의 authored pose 복귀를 끈 상태와 켠 상태를 각각 한 소비자만 바꿔 비교한다.
4. 원인이 확정되기 전에는 위치 보정, 자동 재배치, Rigidbody 자동 수리, 입력 fallback을 추가하지 않는다.

### 재개 작업 절차

- 진단은 수정 전에 읽기 전용 로그만 추가해 `잡기 전 → 놓는 프레임 → 다음 프레임`의 세 시점을 비교한다.
- `selectExited`가 발생하지 않으면 Toggle Grab 선택 해제 경로만 수정 후보로 삼는다.
- authored pose로 한 번 복귀한 뒤 다시 위로 이동하면 `Rigidbody`의 위치·속도와 XRI의 물리 처리 순서만 수정 후보로 삼는다.
- authored pose 복귀가 호출되지 않으면 하자 헬멧 복제본의 `PPEMarkerToggleGrab` 및 부모 참조를 정상 헬멧과 대조한다.
- Y 값을 강제로 낮추는 식의 위치 보정은 원인을 가리므로 적용하지 않는다.

### 검증 상태

- C# 컴파일 성공.
- Quest/OpenXR 수동 재현: **실패** — 반복 Release 때 하자 헬멧이 위로 이동.
- 수정 후 Quest/OpenXR 재검증: 아직 수행하지 않음.

## 2026-08-11 Quest 2 컨트롤러 가이드 및 마스크 유리 시각 개선

### 오늘 요청과 보존한 기존 동작

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나로 제한했다.
- 컨트롤러 가이드 동안 실제 Meta Quest 2 컨트롤러 모델을 보여 주고, 가이드가 끝난 뒤 기존 손 모델로 전환하도록 구성했다.
- `mask1`, `mask2`의 기존 루트 Transform, Collider, Grab 및 입력 컴포넌트는 유지하고 Renderer 시각만 교체했다.
- 기존 XRI Interactor, Tracked Pose, Input Action, 텔레포트, UI Ray 및 PPE 상태 흐름은 변경하지 않았다.
- 런타임 자동 생성이나 자동 위치 수리는 추가하지 않았다. 씬 작성은 명시적인 Editor 메뉴로만 수행한다.

### Quest 2 컨트롤러 모델 적용

- `Assets/Oculus/Core/OculusTouchForQuest2_Left.prefab`과 `OculusTouchForQuest2_Right.prefab`을 프로젝트로 가져왔다.
- 두 모델은 기존 좌우 `Hand Offset` 아래에서 손 시각물과 형제 관계가 되도록 배치했다. 추적과 입력은 새 모델이 소유하지 않고 기존 XR Controller 계층을 그대로 사용한다.
- `Assets/Materials/Quest2Controller_Guide_Unlit.mat`을 적용해 조명에 따라 지나치게 어두워지는 현상을 줄였다.
- `Assets/Editor/Quest2ControllerVisualSetup.cs`에 다음 명시적 메뉴를 추가했다.
  - `Tools > XR > Quest 2 Controller > Install Models In PPE Scene`
  - `Tools > XR > Quest 2 Controller > Validate PPE Scene Setup`
- 설치 도구는 기존 손, 상호작용, 텔레포트 및 가이드 입력 컴포넌트를 수정하지 않고 모델 Prefab과 작성 Transform만 연결한다.
- 입력 의존성은 기존 `XRI Default Input Actions.inputactions`, `InputActionManager`, 좌우 Tracked Pose 경로를 재사용한다. 별도의 중복 입력 소비자는 추가하지 않았다.

### 컨트롤러에서 손으로 전환하는 상태 로직

- `PPEVoiceFlowDirector`에 작성된 좌우 컨트롤러 모델 Transform 두 개를 직렬화 참조로 연결했다.
- 다음 상태에서는 컨트롤러 모델을 표시하고 기존 PPE 손 모델을 숨긴다.
  - `Welcome`
  - `NameInput`
  - `ControllerRay`
  - `ControllerMarker`
  - `ControllerRayT`
  - `ControllerPanel`
- 컨트롤러 가이드가 끝나 `CardIntro` 이후 상태로 이동하면 컨트롤러 모델을 숨기고 기존 손 모델을 다시 표시한다.
- `OnEnable`, 상태 프레젠테이션 적용, `OnDisable`에서 같은 상태 기준을 사용한다. 필수 좌우 모델 참조나 `PPEEquipmentVisualController`가 없으면 자동 검색·생성하지 않고 한 번의 명확한 오류를 남긴다.
- `PPEVoiceFlowSetup`은 기존 Inspector 참조가 비어 있을 때만 좌우 모델을 연결하며, 이미 작성된 값을 반복해서 덮어쓰지 않는다.

### 마스크 유리 모델 적용

- 원본 모델:
  - `Assets/FBX/mask_1/tripo_convert_0f854e67-35a1-4de2-b129-ab94b1eab4c1.fbx`
  - `Assets/FBX/mask_2/tripo_convert_d6eb9a22-af3b-46cd-a7fe-bc0d30c9a28a.fbx`
- 생성 결과:
  - `Assets/Generated/PPE/MaskGlass/Mask1_GlassReady.fbx`
  - `Assets/Generated/PPE/MaskGlass/Mask2_GlassReady.fbx`
  - `Assets/Generated/PPE/MaskGlass/PPE_Mask_Glass.mat`
- `mask1`은 원본에 존재하는 완전한 바이저 파츠 `tripo_part_5`, `tripo_part_29`에 공용 투명 유리 머티리얼을 적용했다.
- `mask2`는 몸체가 단일 결합 Mesh이고 별도 바이저 Mesh가 없어서, 프레임 개구부에 맞춘 곡면 렌즈 `Mask2_Glass_Lens`를 생성했다.
- `Tools/generate_mask_glass_models.py`로 두 FBX를 재현 가능하게 생성한다. Blender 카메라는 결과 확인용 미리보기 장치였으며 마스크 기능에는 필요하지 않다. FBX 내보내기 대상을 `EMPTY`, `MESH`로 제한해 카메라가 포함되지 않도록 수정했고, 설치된 두 마스크 하위 Camera 수가 모두 0임을 확인했다.
- `Assets/Editor/PPEMaskGlassSetup.cs`에 다음 명시적 메뉴를 추가했다.
  - `Tools > PPE > Mask Glass > Install In Open PPE Scene`
  - `Tools > PPE > Mask Glass > Validate Open PPE Scene`

### 마스크 시각 문제의 근본 원인과 수정

- `mask1` 표면이 어둡고 조명에 따라 누더기처럼 보이는 문제는 다수의 원본 파츠가 Lit 조명 반응을 각각 받는 상태에서 두드러졌다. 유리 파츠를 제외한 몸체 머티리얼을 원본 BaseMap과 BaseColor를 보존한 URP Unlit 자산으로 생성해 교체했다.
- `mask2` 유리가 프레임에서 떨어져 보인 직접 원인은 생성 FBX를 원본 루트 아래에 단순 Identity 자식으로 넣으면서 FBX 루트 축과 모델 단위 차이가 중복 적용된 것이었다.
- 설치 도구는 대응하는 원본/생성 Renderer의 행렬로 회전과 위치를 먼저 맞춘 뒤, 실제 Renderer Bounds 크기 비율로 균일 Scale을 적용하고 Bounds 중심 차이만큼 이동한다.
- 최종 직렬화 값에서 `Mask1 Glass Ready Visual`은 약 `100` 배 Scale, `Mask2 Glass Ready Visual`은 약 `1` 배 Scale로 저장됐다. 이는 두 FBX의 서로 다른 모델 단위를 원본 Renderer Bounds에 맞춘 결과이며 마스크 루트 자체의 작성 Transform은 바꾸지 않았다.
- 기존 원본 Renderer는 비활성화하고 생성 시각 Renderer만 활성화한다. Collider와 상호작용 컴포넌트는 기존 마스크 루트에 그대로 남는다.
- 유리는 URP Lit 투명 머티리얼을 사용하며 Alpha Blend, ZWrite Off, 양면 표시, 그림자 Off로 작성했다. 프로젝트 소유 커스텀 셰이더는 추가하지 않았다.

### 영향 범위

- 변경 대상은 컨트롤러 가이드의 손/컨트롤러 표시 상태와 `mask1`, `mask2`의 렌더링 시각이다.
- 카드, 모달, 텔레포트, PPE Grab, Action Panel, 음성 순서, 장착 슬롯, 거울 및 퀴즈 상태 전이는 변경하지 않았다.
- 마스크 원본 Collider와 물리 입력 경로를 유지하므로 새 유리 Mesh는 별도의 Grab 또는 Ray 대상이 아니다.
- 타이틀 테스트 중 `1_Title_Test` 씬이 Play Mode였을 때 PPE 씬을 Additive로 다시 열어 저장하려는 검증은 Unity가 차단했다. Play Mode를 중단하거나 타이틀 씬의 저장 상태를 변경하지 않았다.

### 완료한 검증

#### 정적 확인

- 대상 씬 YAML에 좌우 Quest 2 Prefab 인스턴스와 `m_ControllerModelVisuals` 두 참조가 저장돼 있음을 확인했다.
- 대상 씬 YAML에 `Mask1_GlassReady.fbx`, `Mask2_GlassReady.fbx`, `PPE_Mask_Glass.mat` 참조와 두 Glass Ready Visual 인스턴스가 저장돼 있음을 확인했다.
- Blender 생성 결과에서 `Mask2_Glass_Lens`가 마스크 프레임 전체 개구부를 덮는 곡면 형상임을 확인했다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo` 결과 오류 0개, 기존 경고 27개였다.

#### Unity Editor 확인

- 설치 직후 원본과 생성 몸체 Renderer Bounds를 비교했다.
  - `mask1`: 중심 오차 약 `0.000001`, 크기 비율 `1.000000`
  - `mask2`: 중심 오차 `0.000000`, 크기 비율 `1.000000`
- `mask1` 생성 몸체에서 URP Unlit이 아닌 머티리얼 수가 0임을 확인했다.
- `mask1` 다각도 Scene View에서 몸체와 유리의 정렬을 확인했다.
- 두 마스크 하위에 Camera가 남지 않았음을 확인했다.

### 아직 필요한 수동 검증

1. `3_PPE_Room_HandTest_scale_0`을 단독으로 열고 Game View에서 `mask2` 정면과 측면의 유리 테두리가 프레임을 벗어나거나 내부로 파고들지 않는지 확인한다.
2. 투명 유리의 밝기, 뒤쪽 얼굴/마스크 내부 가시성, 겹침 정렬을 실제 조명 조건에서 확인한다.
3. Quest/OpenXR 양안에서 두 유리가 같은 위치와 투명도로 보이는지 확인한다.
4. 앱 시작부터 컨트롤러 가이드 마지막 단계까지 좌우 Quest 2 모델이 추적되고, `CardIntro` 전환 시 손 모델로 한 번만 교체되는지 확인한다.
5. 컨트롤러 모델을 표시한 상태에서도 Ray, Trigger, Grip, 조이스틱 및 텔레포트 입력이 기존과 동일하게 작동하는지 확인한다.

### 오늘 추가·수정한 주요 파일

- `Assets/Editor/Quest2ControllerVisualSetup.cs`
- `Assets/Editor/PPEMaskGlassSetup.cs`
- `Assets/Editor/PPEVoiceFlowSetup.cs`
- `Assets/Scripts/PPEVoiceFlowDirector.cs`
- `Assets/Materials/Quest2Controller_Guide_Unlit.mat`
- `Assets/Oculus/Core/OculusTouchForQuest2_Left.prefab`
- `Assets/Oculus/Core/OculusTouchForQuest2_Right.prefab`
- `Assets/Generated/PPE/MaskGlass/`
- `Tools/generate_mask_glass_models.py`
- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`

## 2026-08-13 후속: 첫 착용 부착·거울 손 숨김·퀴즈 진입 회귀

### 적용한 변경

- `PPEHazmatEquipController`가 `UseApproved` 전에는 `PPE Body Anchor`를 추적하지 않고, 착용 애니메이션 중과 착용 완료 후에만 HMD의 X/Z와 Yaw를 추적하도록 변경했다.
- PPE 방에 모델을 배치하기 위한 `PPE Body Anchor`의 작성 X/Z를 착용 후 몸 위치에 더하지 않도록 했다. 작성 Y는 기존처럼 바닥 접지 기준으로 유지한다.
- 비활성화 시 `PPE Body Anchor`의 작성 위치와 회전을 복원하도록 했다.
- `PPEFinaleController`의 퀴즈 진입 조건을 기존 설계대로 `방호복 착용 완료 + 필수 PPE 슬롯 전체 사용`으로 복원했다. 태블릿 서명 완료 여부는 퀴즈 진입을 막지 않는다.
- 위치 반경으로 거울 도착을 감지한 경우에도 `PPEVoiceFlowDirector.NotifyMirrorMarkerArrived()`를 거치도록 통합했다. 이 경로가 기존 거울 이동 안내를 중단하고 거울 확인 음성을 재생한 뒤 손 숨김과 관찰을 시작한다.
- 거울 도착 플래그가 설정된 뒤에는 프레임 실행 순서와 관계없이 “거울 오른쪽으로 이동” 안내가 다시 재생되지 않도록 했다.
- `PPE Quiz World Canvas(Clone)` 루트는 작성 활성 상태로 복원하고, 시작 시에는 기존 `PPEQuizController.Awake()`가 하위 `PPE Quiz Panel`만 숨기도록 했다.
- 기존 `_scale_0` 읽기 전용 검증 하네스에 Quiz Canvas 활성 상태와 Quiz/Voice/Finale 상호 참조 검사를 추가했다.

### 근본 원인

- 착용 전후 상태 게이트 없이 `LateUpdate`에서 Body Anchor를 갱신했고, PPE 방 배치용 X/Z 오프셋까지 HMD 위치에 더해 풀착용 모델이 사용자 몸이 아닌 시작 위치 쪽에 남았다.
- 최근 `PPEFinaleController`에 추가된 태블릿 완료 조건이 기존 퀴즈 설계와 달라 `WaitingForMirror` 전환, 거울 손 숨김, 퀴즈 표시를 함께 차단했다.
- 위치 기반 거울 도착 fallback이 음성 도착 이벤트를 우회해 이전 이동 안내가 계속 재생될 수 있었다.
- `PPEQuizController`가 붙은 World Canvas 부모가 비활성인 상태에서 `BeginQuiz()`가 하위 패널만 활성화했다. 퀴즈는 부모 때문에 보이지 않았고, 이어진 `NotifyQuizPresented()`는 손 모델을 복원해 “손만 보이고 퀴즈는 없는” 상태를 만들었다.

### 영향 범위

- 풀착용 모델의 착용 전 위치 보존, 착용 후 신체 추적, 거울 관찰 중 손 모델 표시, 거울 안내 음성 순서, 5문제 퀴즈 진입.
- 개별 PPE Grab·Action Panel, 태블릿 Trigger 서명 기능, 거울 RenderTexture와 카메라 설정은 변경하지 않았다.

### 완료한 검증

- `Assembly-CSharp.csproj` 빌드 결과 오류 0개를 확인했다. 기존 샘플 경고 8개만 남았다.
- `_scale_0` 씬에서 Hazmat/장비/Voice/Finale/Quiz 컨트롤러, 거울 마커, 관찰 지점 및 좌우 Bare/Suit/Glove/Tape 손 모델 FileID가 각각 유효하게 존재함을 정적으로 확인했다.
- 퀴즈 컨트롤러와 5개 작성 페이지 참조는 기존 씬 값을 유지했다.
- Quiz World Canvas가 활성이고 `quizRoot`가 그 자식이며 Quiz/Voice/Finale 참조가 서로 일치하도록 하네스 검사를 추가했다.

### 아직 필요한 수동 검증

1. `_scale_0` Play Mode에서 방호복 `UseApproved` 전에는 풀착용 모델이 HMD를 따라오지 않는지 확인한다.
2. 첫 착용 애니메이션 완료 후 텔레포트와 HMD 회전에서 풀착용 모델이 사용자 몸을 계속 따라오는지 확인한다.
3. 거울 텔레포트 도착 즉시 이동 안내가 중단되고 거울 확인 음성으로 바뀌며, 관찰 중 손 모델이 숨겨지는지 확인한다.
4. 5초 관찰 후 퀴즈 UI가 노출되는지 확인한다.
5. Quest/OpenXR 양안에서 풀착용 모델의 위치·방향과 손 숨김을 확인한다.

## 2026-08-13 후속: Trigger 음성 스킵 상호작용 제외 및 시작 손 중복 방지

### 적용한 변경

- 좌우 Quest Trigger 음성 스킵 입력에 해당 손의 `NearFarInteractor`를 씬 작성 참조로 연결했다.
- Trigger 입력 시 현재 UI Raycast 대상 또는 그 부모가 `IPointerClickHandler`를 구현하면 음성 스킵을 실행하지 않도록 했다. 카드와 패널 버튼의 기존 클릭 처리는 그대로 유지한다.
- 클릭 가능한 UI를 가리키지 않는 동안에는 기존처럼 좌우 Trigger로 현재 음성을 스킵할 수 있다.
- 시작 상태를 적용한 뒤 다른 장비 초기화가 맨손 모델을 다시 활성화하더라도 `Start()`에서 컨트롤러 표시 상태를 한 번 재확정하도록 했다.
- 맨손 임시 숨김 상태가 이미 기록된 경우에도 실제 손 오브젝트를 다시 비활성화하되, 원래 활성 상태 스냅샷은 덮어쓰지 않도록 했다. 컨트롤러 안내가 끝나 `CardIntro`로 전환될 때 기존 스냅샷에 따라 맨손으로 복원한다.
- `_scale_0` 읽기 전용 검증 하네스에 좌우 음성 스킵 Interactor, 컨트롤러 모델 2개, 장비 시각 컨트롤러 참조 검사를 추가했다.

### 근본 원인

- 같은 Trigger 입력이 XRI UI 클릭과 별도의 음성 스킵 `InputAction`에 동시에 전달됐고, 음성 스킵 쪽에는 현재 가리키는 클릭 대상 검사 없이 상태 조건만 있었다. 이 때문에 카드나 패널을 선택하는 Trigger도 음성을 함께 중단할 수 있었다.
- `PPEVoiceFlowDirector.OnEnable()`이 맨손을 숨긴 뒤 `PPEHazmatEquipController.OnEnable()`이 장비 초기 상태를 적용하며 맨손을 다시 활성화할 수 있었다. 음성 흐름 쪽의 임시 숨김 플래그는 이미 설정되어 있어 이후 같은 숨김 요청이 실제 오브젝트를 다시 끄지 않았다.

### 영향 범위

- `_scale_0` 씬의 좌우 Trigger 음성 스킵, 카드·패널의 UI Trigger 선택, 시작 컨트롤러 표시와 안내 종료 후 맨손 전환.
- 카드 상태 전이, 모달, 텔레포트, PPE Grab, 음성 재생 순서와 기존 컨트롤러 입력 액션은 변경하지 않았다.
- 제외 범위는 실제 `IPointerClickHandler`가 있는 UI 대상이다. 단순 배경 Graphic이나 3D 상호작용 대상은 새 제외 대상으로 넓히지 않았다.

### 완료한 검증

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드 결과 모두 오류 0개를 확인했다. 출력된 경고는 기존 패키지·샘플 참조 경고다.
- `_scale_0` 씬에서 좌우 `NearFarInteractor` FileID와 음성 흐름의 직렬화 참조가 일치함을 정적으로 확인했다.
- 입력 경로를 `Quest Trigger → NearFarInteractor → TrackedDeviceGraphicRaycaster → IPointerClickHandler`와 별도 음성 스킵 `InputAction`으로 구분해 확인했다.
- Unity 배치 하네스 실행은 열려 있는 Editor와 별도로 시작했으나 라이선스 IPC 초기화 단계에서 반환 코드 1로 종료되어, 실제 하네스 성공 결과로 간주하지 않았다.

### 아직 필요한 수동 검증

1. `_scale_0` Play Mode 첫 렌더 프레임부터 맨손 없이 좌우 컨트롤러만 보이는지 확인한다.
2. 컨트롤러 안내 마지막 단계가 끝나 `CardIntro`로 전환될 때 컨트롤러가 꺼지고 맨손이 한 번만 나타나는지 확인한다.
3. 카드·모달·패널의 클릭 가능한 요소를 가리킨 채 좌우 Trigger를 눌렀을 때 해당 선택은 실행되고 현재 음성은 스킵되지 않는지 확인한다.
4. 클릭 가능한 UI를 가리키지 않은 상태에서 좌우 Trigger를 눌렀을 때 현재 음성이 스킵되는지 확인한다.
5. Quest/OpenXR에서 좌우 손 각각의 UI Raycast와 Trigger 입력으로 위 동작을 확인한다.

## 2026-08-15 후속: 훈련 미완료 뒤 거울 재검사 차단

### 재현 절차

1. `3_PPE_Room_Train_Test.unity`에서 훈련 모드로 진입한다.
2. 필수 PPE는 모두 착용하되 태블릿 체크를 완료하지 않은 상태로 거울 검사를 수행한다.
3. `TRAIN_005_UnEnoughPpeTablet` 미완료 음성을 확인한다.
4. 태블릿 체크를 완료한 뒤 거울 앞으로 다시 이동한다.

### 증상과 근본 원인

- 첫 미완료 검사 뒤 다시 거울에 도착해도 게이지와 퀴즈 진행에 변화가 없었다.
- 훈련·테스트의 거울 음성 중복을 막기 위해 `PPEVoiceFlowDirector.NotifyMirrorMarkerArrived()`에 추가한 조기 반환이 두 번째 도착 이벤트 자체를 `PPEFinaleController`에 전달하지 않았다.
- `PPEFinaleController`는 미완료 뒤 `WaitingForEquipment`로 복귀해 재검사를 지원하고 있었으므로, 최근 Director 변경이 만든 전달 차단 회귀였다.

### 적용한 변경

- 훈련·테스트 거울 도착의 조기 반환을 제거하고 모든 도착을 `PPEFinaleController.NotifyMirrorMarkerArrived()`로 전달한다.
- `TRAIN_004_MirrorCheckPPE`의 중복 재생은 기존 `m_AllPpeMoveMirrorVoicePlayed`가 계속 방지하므로 거울 재검사 허용과 음성 1회 정책을 분리했다.
- `PPETrainTestModeValidationHarness`에 거울 도착 메서드가 Finale 호출을 포함하고 조기 반환을 포함하지 않는지 검사하는 회귀 조건을 추가했다.

### 영향 범위

- 훈련·테스트에서 첫 거울 검사가 미완료였던 경우의 두 번째 마커 도착 및 위치 반경 기반 재도착 경로다.
- 교육 모드의 최초 거울 확인 음성 1회 정책, 전체 PPE 완료 음성, 거울 관찰 시간과 완료 조건은 변경하지 않았다.

### 완료한 검증과 남은 검증

- 첫 실행에서 `TrainingSelected → TeleportInstruction → PpeArea`, 훈련 Action Panel 피드백, 거울 미완료 음성까지 Quest/OpenXR에서 확인했다.
- 수정 뒤 동일 재현 절차에서 태블릿 완료 후 거울에 재도착했을 때 게이지와 `TRAIN_006_Quiz` 뒤 퀴즈까지 정상 진행되는 것을 사용자가 Quest/OpenXR에서 확인했다.
- 런타임 및 Editor 보조 C# 빌드에서 오류 0개를 확인했다.
- 종료 흐름 변경까지 포함한 `Tools > PPE > Validate Train Test Modes` PASS는 Unity 재컴파일 뒤 다시 확인해야 한다.

## 2026-08-16 후속: 좌·우 장갑 교육 안내 중복

### 증상과 근본 원인

- 교육 모드에서 왼쪽과 오른쪽 장갑을 각각 처음 잡을 때 같은 장갑 교육 안내가 총 두 번 재생됐다.
- 두 장갑이 같은 `OnGloveGrabbed()`를 사용했지만, `PPEVoiceFlowDirector`에는 좌·우가 공유하는 세션 단위 재생 상태가 없었다.

### 적용한 변경과 영향 범위

- 작업 대상 `Assets/Scenes/3_PPE_Room_Train_Test_1.unity`에서만 장갑 잡기 교육 안내의 세션 최초 1회 옵션을 켰다.
- 첫 Clean 장갑 잡기에서만 안내 재생 권한을 소비하며, 두 번째 장갑·같은 세션의 재잡기·오염 장갑·훈련 및 테스트 모드는 교육 안내를 다시 시작하지 않는다.
- 새 모드 세션을 선택하면 상태를 초기화한다.
- 원본 `3_PPE_Room_Train_Test.unity`와 다른 씬은 새 옵션의 기본값이 꺼져 있어 기존 동작을 유지한다.
- Grab 이벤트 구독, Action Panel, 장갑 SFX, 착용 슬롯 및 전체 PPE 완료 조건은 변경하지 않았다.

### 검증

- `_1` 씬에는 1회 옵션이 켜져 있고 원본 씬에는 새 직렬화값이 없어서 기본값이 꺼지는 것을 정적으로 대조했다.
- Unity Editor에서 `Tools > PPE > Validate Glove Grab Narration Once (_1)`와 같은 경로의 검증 메서드를 실행해 첫 Clean 잡기만 허용되는지와 세션 초기화·모드 제외 조건이 PASS임을 확인했다.
- Unity Console에서 이번 변경에 의한 C# 컴파일 오류가 없음을 확인했다.
- Unity Play Mode와 Quest/OpenXR에서는 한 세션에 좌·우 장갑을 순서대로 잡아 교육 안내가 최초 한 번만 들리는지 수동 확인이 필요하다.

## 2026-08-16 후속: PPE 순서·마스크 확인·완료 피드백 규칙

### 증상과 근본 원인

- 방호복 미착용 상태에서도 장갑과 장화의 `UseApproved`가 진행됐다. 기존 코드는 장화 교육 음성을
  재생하지 않는 조건만 두었고, 선택 승인 전에 방호복 상태를 검사하지 않았다.
- 송기 마스크는 `확인하기` 버튼이 있었지만 확인 완료 상태를 보관하지 않아, 호흡 확인 전에도
  사용 또는 폐기가 가능했다.
- 거울 미완료 판정은 PPE와 태블릿을 하나의 `complete` 값으로 합친 뒤 음성을 선택해 교육 모드에서
  부족 항목을 구분할 수 없었다.
- 안전모 교육 음원은 존재했지만 안전모 Action Panel의 Grab 이벤트와 Director 참조가 연결되지 않았다.
- Action Panel 성공 문구가 `선택 완료`, 하자 PPE 사용 문구가 `사용할 수 없습니다`여서 확정 문구와
  일치하지 않았다.

### 적용한 변경

- `3_PPE_Room_Train_Test_1.unity`에서만 방호복 미착용 시 장갑·장화 `Use` 승인을 차단하고 교육
  모드에서는 기존 `4_VO_PPE_EDU_200_GloveBootsOreder`를 재생한다.
- 송기 마스크는 정상·하자 모두 `확인하기` 완료 전 사용·폐기를 차단한다. PPE 상태 변경과 새 세션
  시작 시 확인 완료 상태를 초기화한다.
- 거울 판정을 PPE 완료와 태블릿 완료로 분리했다. 교육 모드는 부족 조건별 음원을 재생하고 둘 다
  부족하면 PPE 안내 뒤 태블릿 안내를 순차 재생한다. 훈련은 기존 통합 음성, 테스트는 기존 무교정
  정책을 유지한다.
- 기존 안전모 음원 `4_VO_PPE_EDU_104_HowToHelmet`을 활성 안전모의 교육·정상 Grab에 연결했다.
- 10개 Action Panel의 네 결과 문구를 `사용 처리 완료`, `폐기 처리 완료`,
  `PPE 정상 여부 확인 필요` 정책으로 통일했다.

### 영향 범위

- 작업 씬 `_1`의 장갑·장화 사용 승인, 송기 마스크 확인 상태, Action Panel 피드백, 안전모 Grab
  교육 음성, 거울 미완료 교육 음성이다.
- 원본 `3_PPE_Room_Train_Test.unity`, 훈련·테스트 교정 정책, 손 모델, 테이프, 고스트핸드, 장착 슬롯,
  XR 입력 경로는 변경하지 않았다.

### 완료한 검증

- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 `--no-dependencies`로 빌드해 오류
  0개를 확인했다. 출력 경고는 기존 분석기 및 사용 중단 API 경고다.
- `_1` 씬의 패널 10개 문구, 마스크 확인 게이트, 장갑·장화 Director 참조, 안전모·태블릿 음원 GUID,
  교육/훈련 분기 코드 및 원본 씬 무변경을 검사한 정적 검증이 PASS했다.
- `Tools > PPE > Validate PPE Requirement Policies (_1)` 회귀 메뉴를 추가했다.

### 아직 필요한 수동 검증

1. Unity Play Mode 교육 모드에서 방호복 전 장갑·장화 `Use`가 거부되고 안내 음성이 재생되는지 확인한다.
2. 정상·하자 마스크 모두 확인 전 사용·폐기가 거부되고, 확인 후 현재 상태에 맞는 선택이 가능한지 확인한다.
3. 마스크 상태를 바꾼 뒤 이전 확인 완료가 재사용되지 않는지 확인한다.
4. 안전모를 정상 상태로 잡았을 때 교육 모드에서 안내가 한 번 재생되는지 확인한다.
5. 거울에서 PPE만 부족, 태블릿만 부족, 둘 다 부족한 세 경우의 교육 음성을 각각 확인한다.
6. 훈련은 통합 미완료 음성 1회, 테스트는 교육 교정 음성 없음이 유지되는지 확인한다.
7. Quest/OpenXR에서 패널 Trigger, SFX, 음성 순서와 타이밍을 확인한다.

## 2026-09-08 후속: 현재 PPE Room의 정상 장화·안전모 최초 Grab 음원 누락

### 이번 요청과 보존할 기존 동작

- 이번 확인은 Alpha 제출 전 `Assets/Scenes/4_PPE_Room.unity`의 교육 모드에서 정상 장화와 정상 안전모를
  최초 Grab했을 때 전용 안내 음원이 재생되지 않는 현상을 기록하기 위한 것이다.
- 사용자가 먼저 처리할 작업이 있어 구현·씬 수정·Play 실행·빌드·DB 작업은 중단했다. 이 절에는 진단
  결과와 후속 수정 조건만 기록한다.
- 교육 이외의 Training/Test 음성 정책, PPE 사용·폐기 판정, 착용 슬롯, 카드·모달·텔레포트, 퀴즈와
  기준 JSONL 원본은 변경하지 않는다.

### 재현과 실행 원본 근거

- 사용자가 Unity Editor + Quest 2 USB3 Link에서 `ConfinedSpace/Education`과
  `LeakResponse/Education`을 각각 완료했고, 두 회차 모두 정상 장화와 정상 안전모의 최초 Grab 안내가
  들리지 않았다고 확인했다.
- 두 JSONL 모두 안전모의 `ppe_grab_attempted → ppe_inspection_started →
  ppe_grab_attempt_resolved(selected)`와 장화의 같은 입력·선택 이벤트를 기록했다. 따라서 장비 Grab 자체가
  실패한 현상은 아니다.
- 두 회차의 `voice_playback_started`를 대조한 결과 `4_VO_PPE_EDU_009_NextPPE`는 각각 9회와 8회
  기록됐지만 `4_VO_PPE_EDU_101_HowToBoots`와 `4_VO_PPE_EDU_104_HowToHelmet`은 두 회차 모두 0회였다.
  사용자의 청취 결과와 런타임 원본 이벤트가 일치한다.
- 이 편차는 2026-09-08 기준 데이터 문서에 이미 명시했으며, 두 교육 회차는 시간·행동·퀴즈·PPE 선택
  비교에는 사용하되 장화·안전모 최초 Grab 음원 정상 재생의 증거로 사용하지 않는다.

### 근본 원인

- 현재 씬의 `PPEVoiceFlowDirector`에는 `m_HelmetGrabVoice`와 `m_BootGrabVoice` AudioClip GUID가 각각
  기존 `HowToHelmet`, `HowToBoots` 원본으로 정상 연결돼 있다. 음원 파일 또는 AudioClip 참조 누락이 아니다.
- `SubscribePpeConditionalNarration()`은 `m_HelmetActionPanel`과 `m_BootActionPanels`에 직렬화된 패널의
  `XRGrabInteractable.selectEntered`에만 Grab 음성 콜백을 연결한다.
- 현재 `m_HelmetActionPanel`은 `PPE/PPE_A_Helmet_NoStrap` 하자 안전모를 가리킨다. 실제로 사용자가 잡은
  정상 안전모 `PPE/PPE_A_Helmet_Strap`의 Action Panel은 별도 활성 오브젝트이며 이 참조와 일치하지 않는다.
- 현재 `m_BootActionPanels` 두 항목도 `PPE_A_Boots_L_Contam`, `PPE_A_Boots_R_Contam`만 가리킨다.
  실제 정상 장화 `PPE_A_Boots_L_Clean`, `PPE_A_Boots_R_Clean`의 Action Panel은 배열에 없다.
- 정상 Grab 안내의 `CanPlayRequiredPpeHowTo()`는 Education, 활성 작업계획, Clean 상태와 작업계획 허용
  PPE를 모두 요구한다. 하자 패널만 구독한 현재 직렬화에서는 하자 패널은 Clean 조건을 통과하지 못하고,
  정상 패널은 애초에 콜백이 구독되지 않는다. 이것이 두 전용 음원이 0회였던 직접 원인이다.
- 2026-08-16의 이전 안전모 연결 기록은 당시 대상 씬 `3_PPE_Room_Train_Test_1.unity`를 기준으로 했다.
  현재 빌드 대상 `4_PPE_Room.unity`의 실제 직렬화 참조를 다시 확인하지 않고 과거 완료 판단을 확장하면
  안 된다.

### 영향 범위

- 직접 영향은 Education 모드의 정상 안전모·정상 좌우 장화 최초 Grab 전용 안내다.
- `NextPPE`, 선택 결과, 필수 PPE 판정, 최종 완료, JSONL `mode_session_completed`는 실제 두 회차에서
  계속 동작했다. 따라서 전체 Education 흐름이 중단된 결함으로 확대 해석하지 않는다.
- Training/Test는 정상 PPE How-To 음성을 재생하지 않는 기존 정책이므로 이 결함의 직접 수정 대상이 아니다.

### 변경 전 필수 판단과 후속 수정안

1. 기존 Inspector/씬 작성값 중 AudioClip, 위치, UI, 입력, 착용 값을 보존하고 잘못된 Action Panel 참조만
   최소 범위로 교정한다.
2. 상태 소유자는 `PPEVoiceFlowDirector`, 장비 상태 소유자는 각 `PPEInspectionState`이며 정상 장비의
   `PPEActionPanelController`를 구독 기준으로 사용한다.
3. 입력 경로는 `Quest Grip/Select → XR Interactor → 정상 PPE XRGrabInteractable.selectEntered →
   PPEInspectionState/PPEActionPanelController → PPEVoiceFlowDirector → AudioManager.PlayVoice`다.
4. 런타임 자동 탐색·자동 수리로 우회하지 않고 씬 직렬화 참조와 Editor 검증 하네스로 실패를 드러낸다.
5. 후속 변경의 소비자는 Education 정상 Grab 음성뿐이다. Training/Test, 선택 판정, UI, 텔레포트와 다른
   PPE 음성을 함께 변경하지 않는다.
6. 변경 전 기준은 위 두 JSONL의 대상 전용 음원 0회다. 변경 후에는 두 작업계획의 Education에서 정상
   안전모와 정상 장화 최초 Grab 각각 전용 음원 1회, 재잡기·반대쪽 장화 중복 0회를 비교한다.
7. 현재 완료 증거는 정적 씬·코드 대조와 Unity Editor + Quest Link 사용자 재현이다. 수정 후에는 정적
   하네스, Unity Play Mode와 Quest/OpenXR 양쪽의 실제 `voice_playback_started` 원본을 다시 확인한다.

후속 구현에서는 `m_HelmetActionPanel`을 정상 안전모 Action Panel로 교정하고,
`m_BootActionPanels`에 정상 좌우 장화 Action Panel을 포함하되 기존 하자 선택 처리에 필요한 참조는
보존한다. 기존 검증 하네스도 단순 non-null/AudioClip GUID 확인을 넘어 정상 패널의 ItemType, Clean 초기
상태와 배열 포함 여부를 검사하도록 보완한다. 셰이더 Import나 스크립트 컴파일 중에는 Quest/OpenXR Play를
시작하지 않는다.

### 2026-09-08 적용 결과

- `Assets/Scenes/4_PPE_Room.unity`의 `m_HelmetActionPanel`을 하자 안전모
  `PPE_A_Helmet_NoStrap`에서 정상 안전모 `PPE_A_Helmet_Strap`의 기존 Action Panel로 교정했다.
- `m_BootActionPanels`에는 기존 하자 좌·우 장화 패널을 그대로 보존하고 정상 좌·우 장화 패널을 추가했다.
  런타임 자동 탐색이나 자동 수리, AudioClip, 입력, 선택 판정, UI, 텔레포트와 Training/Test 정책은
  변경하지 않았다.
- `PPETrainTestModeValidationHarness`가 정상 안전모의 `ConstructionHelmet + Clean` 계약과 장화 배열의
  `좌·우 × Clean/Contaminated` 네 조합, 중복·누락 참조를 검사하도록 보완했다.
- **정적 확인:** `git diff --check`를 통과했고 `Assembly-CSharp-Editor.csproj --no-restore`는 기존
  assembly 충돌·source generator 경고 5개, 오류 0개로 완료됐다.
- **Unity Editor 확인:** Unity 6000.4.8f1 배치에서
  `PPETrainTestModeValidationHarness.ValidateBatch`가 PASS했다.
- **Play Mode 확인:** 아직 수행하지 않았다. Education에서 정상 안전모와 정상 장화 최초 Grab 시
  `4_VO_PPE_EDU_104_HowToHelmet`, `4_VO_PPE_EDU_101_HowToBoots`가 각각 1회 재생되고 재잡기와 반대쪽
  장화에서 중복되지 않는지 확인해야 한다.
- **Quest/OpenXR 확인:** 아직 수행하지 않았다. Quest 양안에서 실제 청취와
  `voice_playback_started` 원본 이벤트를 함께 대조해야 한다.

### 2026-09-09 Game View 1차 확인

- 사용자는 Game View Play Mode에서 방호복 미착용 상태의 선행조건 안내를 정상 장비 Grab 음성 반복으로
  처음 오인했으나, 방호복을 먼저 착용한 뒤 다시 확인해 정상 안전모·장화 음성 동작이 확인된 것으로
  보고했다.
- 이번 결과는 사용자 표현이 "확인된 것 같다"인 1차 확인이므로 `Play Mode 잠정 통과`로 기록한다.
  Quest/OpenXR 실제 Grip, 최초 1회와 재잡기 중복 여부, `voice_playback_started` 원본 대조는 최종 Release
  전 수동 게이트로 유지한다.
