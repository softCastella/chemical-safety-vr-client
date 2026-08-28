# 2026-08-21 PPE 착용 로직·시나리오 분기 QA

- 작성일: 2026-08-21
- 프로젝트: `Final_VR_Tyche_Pivot`
- 대상 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity` (사용자 확정. 이번 QA 16건 전부 이 씬)
- 빌드 활성 씬과는 별개: `EditorBuildSettings`의 PPE 룸은 `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`다. 로코모션 씬은 빌드 목록에 없다. 패치는 대상 씬만 연다.
- 선행 변경: 착용 피봇(몸+트리거), 작업계획 밀폐공간/누출 분기. 기준 기록은 `Docs/MeetingNotes/2026-08-20_PPE_Room_Train_Test_Followup.md`
- 같은 날 별건: `Docs/Bug/2026-08-21_PPE_Mirror_Observation_Gauge_NotShown.md`
- 검증 주체: 사용자 자체 QA
- 상태: 미수정. 본 문서는 결과 기록이며 패치를 포함하지 않는다.

## 목적과 범위

착용 로직과 상세 시나리오 분기 수정 이후, 교육·훈련·테스트 한 사이클에서 모달 문구, 태블릿 문서, PPE 순서 규칙, 음원, 로코모션, 텔레포트가 시나리오와 맞는지 확인한다.

이번 회차는 사용자 Play 재현이다. Unity Editor 메뉴 하네스와 Quest/OpenXR 실기기 통과는 아직 없다. 아래 “정적 후보”는 이 저장소 디스크 코드·씬 대조이며, 원인 확정이 아니다.

## 검증 구분

| 구분 | 이번 회차 |
| --- | --- |
| 사용자 Play QA | 수행. 아래 16건 |
| 정적 코드·씬 대조 | 일부 후보만 기록 |
| Unity Editor 확인 | 미실시 |
| Quest/OpenXR 확인 | 미실시 |

## 결과 요약

| ID | 영역 | 결과 | 수정 주 경로 |
| --- | --- | --- | --- |
| QA-01 | 모달 문구 | 실패 | 씬 작성 텍스트 |
| QA-02 | 태블릿 작업계획서 레이아웃 | 패치 적용. Unity·Quest 확인 남음 | `WorkPlan_Leak.png`를 밀폐와 같은 1055×1491로 재작성. GUID·머티리얼 유지 |
| QA-03 | 작업계획서 2부 체크·사인 | 실패 | 씬 작성 UI |
| QA-04 | 방호복 중복 착용 음원 | 패치 적용. Unity·Quest 확인 남음 | `4_VO_PPE_EDU_204_SuitAlready` + Wrong Answer SFX |
| QA-05 | 시나리오 불필요 PPE 음원 | 패치 적용. Unity·Quest 확인 남음 | `4_VO_PPE_EDU_205_PPE_forScenario` + Wrong Answer SFX |
| QA-06 | 송기마스크 패널 크기 | 패치 적용. Unity·Quest 확인 남음 | `Mask Action Panel` 작성 크기 310, `applySharedPresentationLayout` 켜기 |
| QA-07 | 니트릴 미착용 후 외부장갑 | 패치 적용. Unity·Quest 확인 남음 | 선행 조건 + `Wrong Answer` SFX + EDU 207 보이스 |
| QA-08 | 외부장갑·장화 없이 테이프 사용 | 패치 적용. Unity·Quest 확인 남음 | 외부장갑 또는 장화 선행 조건 + EDU 201 보이스 |
| QA-09 | 태블릿 해제 음원 | 패치 적용. Unity·Quest 확인 남음 | 방호복 착용 중이면 `m_TabletReleasedVoice` 생략 |
| QA-10 | 로코모션 FOOT STEP | 패치 적용. Unity·Quest 확인 남음 | `PPEIdleLocomotionAnimator` + AudioManager `Foot Step` |
| QA-11 | 로코모션 속도 | 패치 적용. Unity·Quest 확인 남음 | Inspector 속도·입력 감도·가속·감속 |
| QA-12 | Teleport_0 잔존 | 실패 | 자동 텔레포트 후 마커 비표시. 음원은 후속 |
| QA-13 | 후면 중도 엑시트 | 패치 적용. Unity·Quest 확인 남음 | `Teleport_3_Exit` 영역 진입 복귀. 위치는 사용자 작성값 |
| QA-14 | 교육모드 시나리오 불일치 PPE 음원 | 패치 적용. Unity·Quest 확인 남음 | QA-05와 동일. `EDU_205` |
| QA-15 | 교육모드 체크리스트 패널 | 실패 | Window Canvas 체크리스트가 비활성 |
| QA-16 | 테스트모드 진입 | 패치 적용. Unity·Quest 확인 남음 | 테스트 버튼에서 밀폐공간/누출사고 작업계획 선택 후 테스트 진입 |

열린 항목: 16. 닫힌 항목: 0.

## 상세

### QA-01 모달 누출 시나리오 문구

- 재현: 시나리오 상세 모달을 연다.
- 관찰: 누출 시나리오 텍스트가 경량 누출 내용으로 들어가 있지 않다.
- 기대: 누출 시나리오 본문을 경량 누출로 기입한다.
- 정적 후보: `ScenarioDetailModal`의 시나리오 `description`은 씬 작성값이다. 런타임이 문구를 덮어쓰지 않는다. 08-20 회의록에도 Horizon 상세와 모달 문구 맞춤은 잔여로 남아 있다.

### QA-02 태블릿 누출 작업계획서가 태블릿 아래로 넘침

- 재현: 누출 시나리오에서 태블릿 작업계획서를 본다.
- 관찰: 누출 관련 작업계획서가 태블릿 밑까지 내려가 아래 내용이 보이지 않는다. 밀폐공간 작업계획서는 정상 크기다.
- 기대: 누출 부도 밀폐공간 부와 같이 태블릿 면 안에 들어간다.
- 원인: `Leak`와 `ConfinedSpace`의 메시·Transform은 같다. 머티리얼 Tiling/Offset도 `(1,1)/(0,0)`으로 같다. 다른 것은 PNG 비율이다. 밀폐 `WorkPlan.png`는 1055×1491(0.71), 누출 `WorkPlan_Leak.png`는 423×483(0.88)이라 같은 평면에 세로로 늘어났다.
- 적용: 같은 파일·GUID `1cefcdf78ccd7dd49be84de8bf06cc63`로 누출 PNG를 1055×1491로 다시 그렸다. `WorkPlanLeakPlane_Unlit` 참조와 `Leak` Transform은 바꾸지 않았다. 작업 내용 문구는 모달과 같이 외부구역·소량 누출을 반영했다. 잘려 있던 5절·6절을 밀폐와 같은 섹션 높이에 채워 넣었다.
- 정적 확인: 두 PNG 모두 1055×1491, 하단 여백 72px. 머티리얼 GUID 유지.
- Unity Editor / Quest 확인: 남음. 누출 서명 시퀀스 체크·사인은 밀폐에서 사용자가 맞춘 작업과 별개이며, 새 A4 레이아웃 기준으로 Inspector에서 다시 맞춰야 할 수 있다.

### QA-03 작업계획서 2부 체크박스·사인 영역

- 재현: 작업계획서 2부를 모두 확인한다.
- 관찰: 체크박스와 사인 영역이 현재 레이아웃·시나리오에 맞지 않다.
- 기대: 2부 모두 체크박스와 사인 영역을 교체한다.
- 비고: 구체 배치값은 이번 QA에서 지정되지 않았다. 수정 시 씬 작성 UI만 바꾸고 런타임 좌표 대입은 하지 않는다.

### QA-04 방호복 착용 중 다른 방호복 착용 시도

- 재현: 방호복을 입은 뒤 다른 방호복을 입으려 한다.
- 관찰: 이미 입었음을 알리는 음원이 없다.
- 기대: 이미 방호복을 입었음을 알리는 음원을 재생한다.
- 원인: `CanApprovePpeUse`가 중복 착용을 거절하지 않아 `UseApproved` 뒤 `PPEHazmatEquipController`만 조용히 return했다.
- 적용: 교육모드에서 이미 입은 뒤 다시 입으려 하면 패널의 Wrong Answer SFX와 `4_VO_PPE_EDU_204_SuitAlready`를 재생하고 착용을 거절한다. 로코모션 씬만 연결했다.
- Unity Editor / Quest 확인: 남음.

### QA-05 시나리오에 없는 PPE를 골랐을 때

- 재현: 교육모드에서 시나리오에 없는 PPE(예: SCBA)를 고른다. 그외 모드에서도 틀린 PPE를 고른다.
- 관찰: 교육모드는 SCBA가 시나리오에 없는데도, 작업에 필요한 PPE를 착용해 달라는 음원이 없다. 그외 모드는 올바른 PPE를 착용해 달라는 음원이 없다.
- 기대:
  - 교육모드: 작업에 필요한 PPE를 착용해 달라는 음원
  - 훈련·테스트: 올바른 PPE를 착용해 달라는 음원
- 적용: 교육모드 작업계획 불일치는 Wrong Answer SFX와 `4_VO_PPE_EDU_205_PPE_forScenario`를 재생한다. 훈련·테스트 거절 경로는 기존 `TrainingWrongButtonVoice`/테스트 기록을 유지한다.
- Unity Editor / Quest 확인: 남음.

### QA-06 송기마스크 패널 크기

- 재현: 송기마스크를 잡아 패널을 연다.
- 관찰: 다른 PPE 패널은 이름에 맞춘 크기로 바뀌었는데, 송기마스크만 이전 긴 패널 크기다.
- 기대: 송기마스크 패널도 이름에 맞춘 크기로 맞춘다.
- 원인: 활성 `Mask Action Panel` 두 개의 `applySharedPresentationLayout`이 꺼져 있어 `ShowGrabNameOnly`가 `nameOnlyLayout`(320×114)을 적용하지 못했다. 작성 `sizeDelta`도 `{320, 417.7577}`로 예전 3버튼 높이가 남아 있었다. 다른 PPE는 플래그가 켜져 있고 작성 높이는 310이다.
- 적용: 로코모션 씬의 송기마스크 컨트롤러만 플래그를 켜고, 패널 작성 높이를 310으로 맞췄다. 런타임 좌표 대입은 넣지 않았다. variant `3_PPE_Room_Train_Test_mask.unity`는 열지 않았다.
- 정적 확인: 마스크 `applySharedPresentationLayout: 1`, `sizeDelta.y: 310`. 다른 패널 플래그는 원래 1이었다.
- Unity Editor / Quest 확인: 남음.

### QA-07 니트릴 장갑 없이 외부 장갑 사용

- 재현: 니트릴 장갑을 끼지 않은 채 외부 장갑 사용을 시도한다.
- 관찰: 틀린 선택 SFX가 나지 않는다.
- 기대: 선행 조건 실패로 사용이 거절되고, 틀린 선택 SFX가 난다.
- 원인: `CanApprovePpeUse`의 니트릴 선행조건 실패가 `RejectUseForMissingPrerequisite`를 사용해 교육모드에서 보이스만 재생하고 `PPEActionPanelController.PlayWrongChoiceFeedbackSfx()`를 호출하지 않았다. 씬의 `m_OuterGloveUseWithoutNitrileVoice`도 비어 있었다.
- 적용: 대상 로코모션 씬에서 같은 쪽 니트릴이 미착용이면 `RejectUseWithWrongSfx`로 거절하도록 변경했다. 교육모드에서는 `Wrong Answer` SFX 후 `4_VO_PPE_EDU_207_InnerGloveFirst`를 재생한다. 훈련·테스트 모드의 기존 피드백 경로는 유지했다.
- 정적 확인: `m_OuterGloveUseWithoutNitrileVoice`와 AudioManager Voice 목록에 207 GUID `1aec12d0cae3a244c87ce78f5211114c`를 연결했고, SFX ID `Wrong Answer`는 기존 씬 등록값을 사용한다.
- Unity Editor / Quest 확인: 남음. 실제 외부장갑 Use 시 Wrong Answer SFX와 207 보이스가 순서대로 들리는지 확인해야 한다.

### QA-08 외부 장갑·장화 없이 테이프 사용 처리

- 재현: 외부 장갑을 끼지 않은 상태에서 테이프를 사용한다.
- 관찰: 테이프가 사용 처리된다.
- 기대: 외부 장갑 또는 장화 중 하나도 없으면 테이프 사용이 거절된다. 니트릴 내부장갑만으로는 사용되지 않아야 한다.
- 원인: 니트릴 내부장갑이 외부장갑 복사본처럼 동작해도 테이프 선행조건은 실제 사용 처리된 외부장갑·장화 타입만 확인해야 한다.
- 적용: `PPEVoiceFlowDirector`의 테이프 조건을 기존 `HasAnyTappableEquipment()` 범위로 복원했다. 외부장갑 좌·우 또는 장화 좌·우 중 하나라도 사용되고 방호복이 착용된 경우 테이프를 사용할 수 있으며, 니트릴만 사용된 경우에는 `m_TapeUseBeforeGlovesAndBootsVoice`를 통해 `4_VO_PPE_EDU_201_TapeOrder`를 재생하고 거절한다.
- 정적 확인: `HasAnyTappableEquipment()`에는 `RubberGloveLeft/Right`와 `RubberBootLeft/Right`만 포함되고 `NitrileInnerGloveLeft/Right`는 포함되지 않는다. 201 GUID `b259c8014e714294bb27efc6f641f37d`는 대상 씬 AudioManager Voice 목록에 등록되어 있다.
- Unity Editor / Quest 확인: 남음. 니트릴만 착용, 부츠만 착용, 외부장갑만 착용의 세 경우를 각각 재현해야 한다.

### QA-09 태블릿을 놓았을 때 방호복 안내가 남음

- 재현: 태블릿을 놓는다. 방호복 착용 여부와 관계없이 확인한다.
- 관찰: 방호복을 입었는지와 관계없이 방호복을 입으라는(오른쪽 팔 마커를 잡으라는) 음원이 나온다.
- 기대: 태블릿을 놓은 시점의 방호복 착용 판정으로 다음 순서 음원 재생 여부를 가른다. 이미 입었으면 해당 안내를 재생하지 않는다.
- 적용: 교육모드 첫 해제에서만 안내한다. `m_HazmatEquipController.IsEquipped`이면 방호복 착용 안내를 재생하지 않는다.

### QA-10 로코모션 중 FOOT STEP

- 재현: 로코모션으로 이동한다.
- 관찰: FOOT STEP SFX가 나지 않는다.
- 기대: 로코모션 동안 FOOT STEP SFX를 재생한다.
- 원인: `Assets/Audio/SFX/Foot Step.ogg`는 있었으나 AudioManager SFX 목록에 없었고, 이동 중 재생 경로도 없었다.
- 적용: 로코모션 씬 AudioManager에 `Foot Step`을 등록하고, `PPEIdleLocomotionAnimator`가 Move가 켜진 채 평면 이동할 때만 0.4초 간격으로 `PlaySfx`한다. 텔레포트 점프는 기존처럼 무시한다. 작업 중 꺼져 있던 `PPE_D_Player_Idle`은 이 컴포넌트가 붙어 있어 다시 켰다.
- 정적 확인: SFX GUID `117904f8108d7d64abe9fa00e7d4d285`, 애니메이터 필드 `m_FootstepSfxId`.
- Unity Editor / Quest 확인: 남음. 헤드셋 출력은 아직 확인하지 않았다.

### QA-11 로코모션 속도가 급변하고 버벅임

- 재현: 로코모션으로 이동한다.
- 관찰: 속도가 확- 확- 바뀌고, 어떤 때는 버벅인다.
- 기대: 속도 변화가 급하지 않고, 이동 중 버벅임이 없다.
- 원인: XRI `DynamicMoveProvider`는 지상 이동 입력을 즉시 이동량으로 적용해 스틱 입력 시작·해제 시 속도가 바로 바뀐다. 기존 `m_MoveSpeed`만으로는 입력 감도와 가속·감속을 조절할 수 없었다.
- 적용: 대상 로코모션 씬의 `Move` 컴포넌트를 `PPEConfigurableDynamicMoveProvider`로 교체했다. 기존 이동 속도와 입력 참조는 보존하고, Inspector에 `Input Sensitivity`, `Acceleration`, `Deceleration`을 추가했다. 현재 작성값은 각각 `1`, `8`, `12`이며 `Move Speed`는 기존 `1`이다. `PPEVoiceFlowDirector`와 `PPEIdleLocomotionAnimator` 참조도 새 컴포넌트로 연결했다.
- 정적 확인: Unity Editor에서 새 컴포넌트 컴파일 성공, 씬 저장값과 두 참조를 확인했고 콘솔 Error/Exception은 0건이다.
- Unity Editor / Quest 확인: 남음. 스틱 입력 시작·유지·해제 시 속도 변화와 버벅임을 실제 로코모션 씬 및 Quest/OpenXR에서 확인해야 한다.

### QA-12 Teleport_0이 계속 보임

- 재현: 자동 텔레포트 이후 시작 쪽을 본다.
- 관찰: `Teleport_0` 마커가 아직 보인다.
- 기대: 자동 텔레포트이므로 마커는 필요 없다. 표시하지 않는다. 음원 변경은 나중에 한다.
- 정적 후보: 대상 로코모션 씬은 EDU_001 뒤 `Teleport_0` 자동 이동이다. 이동 후 마커를 끄는 처리는 별도 확인이 남는다.

### QA-13 뒷쪽 중도 엑시트 텔레포트가 사라짐

- 재현: 후면 중도 종료 마커를 찾는다.
- 관찰: 뒷쪽 중도 엑시트 텔레포트가 없다. 로코모션이 있으면 마커가 있어도 동작할지 불명확하다.
- 기대: 텔레포트 없이, 걷거나 로코모션으로 그 영역 안에 들어가면 복귀한다.
- 원인: 로코모션 시험에서는 `PPEVoiceFlowDirector`가 `PpeArea`여도 `Teleport_3_Exit`를 꺼 두었다. 기존 복귀는 마커 텔레포트 이벤트에만 연결돼 있었다.
- 적용: 사용자가 옮긴 `Teleport_3_Exit` Transform은 유지했다. PPE 구역에서 마커를 다시 보이게 하고, `TeleportationAnchor`는 껐다. 플레이어 XZ가 작성 콜라이더 발자국 안에 들어가면 `RequestExitReturn()`을 호출한다. 원본 mask 씬의 텔레포트 엑시트는 그대로다.
- Unity Editor / Quest 확인: 남음.

### QA-14 교육모드에서 시나리오에 맞지 않는 PPE

- 재현: 교육모드에서 해당 시나리오에 없는 PPE를 고른다.
- 관찰: 시나리오 불일치 PPE 음원이 없다.
- 기대: 교육모드 시나리오에 맞지 않는 PPE를 골랐을 때 전용 음원이 난다.
- 적용: QA-05와 같은 `EDU_205` 경로.

### QA-15 교육모드 태블릿 체크 후 체크리스트 패널

- 재현: 교육모드에서 태블릿 체크를 마친다.
- 관찰: 체크리스트 패널이 뜨지 않는다. Window Canvas의 체크리스트이며, 현재 비활성화되어 있다.
- 기대: 태블릿 체크 후 해당 체크리스트 패널이 표시된다.
- 원인: `Window Canvas/CheckList`의 `m_IsActive`가 0이다. `PPEEducationWearChecklist`는 `CanvasGroup` 알파만 바꾸고 `SetActive(true)`를 호출하지 않는다. 오브젝트가 꺼져 있으면 `OnEnable`/`Update`가 돌지 않아 태블릿 완료(`IsDocumentCompleted`) 뒤에도 패널을 켤 수 없다. 컴포넌트 참조(director·tablet·hazmat)는 연결되어 있다. 설계는 GameObject를 켠 뒤 Awake에서 알파를 0으로 숨기는 쪽이다.
- 패치: 이번 요청은 원인 확인만. 고치려면 씬에서 CheckList를 켜 두거나, `ApplyVisibility`에서 GameObject를 켜면 된다. 둘 중 하나를 승인하면 적용한다.

### QA-16 모드 선택 모달에서 테스트모드 진입 불가

- 재현: 모드 선택 모달에서 테스트모드를 고른다.
- 관찰: 테스트모드로 들어가지 못한다.
- 기대: 테스트모드 버튼 뒤에 `밀폐공간 대응 PPE착용`과 `누출사고 대응 PPE착용` 버튼이 표시되고, 선택 후 이동 준비로 이어진다.
- 원인: `SelectPpeTestMode`가 테스트모드에서도 작업계획 선택 UI를 열도록 되어 있었지만, 대상 로코모션 씬에는 테스트 작업계획 그룹과 5개 참조가 없었다. `HasTestWorkPlanConfiguration`에서 실패해 모드 선택 화면으로 돌아갔다.
- 적용: `SelectPpeTestMode`의 기존 작업계획 선택 흐름을 유지하고, `HasTestWorkPlanConfiguration`은 테스트에 필요한 밀폐공간·누출사고·돌아가기 참조만 요구하도록 정리했다. 대상 씬의 `1_EduChoice`를 `3_TestWorkPlan`으로 복제해 테스트 모달에 Unity 생성 참조로 연결했다. 랜덤 테스트 버튼은 만들지 않았다.
- 정적 확인: 대상 씬에서 `3_TestWorkPlan`이 비활성 상태로 저장되어 있고, 두 작업계획 버튼과 돌아가기 버튼이 연결되어 있다. 테스트 선택·이동·종료 음원 참조도 대상 씬에 연결되어 있다.
- Unity Editor / Quest 확인: 남음. 모달에서 테스트 버튼을 누른 뒤 두 작업계획 버튼이 표시되고, 각각 선택 시 테스트 이동 안내가 시작되는지 확인해야 한다.

## 소비자 영향

| 소비자 | 영향 ID |
| --- | --- |
| 시나리오 모달·모드 선택 | QA-01, QA-16 |
| 태블릿·작업계획서·체크리스트 | QA-02, QA-03, QA-09, QA-15 |
| PPE Grab·착용 순서·패널 | QA-04, QA-05, QA-06, QA-07, QA-08, QA-14 |
| 로코모션 | QA-10, QA-11, QA-13 |
| 텔레포트 마커 | QA-12, QA-13 |

한 패치에서 모달 문구, 착용 규칙, 로코모션, 텔레포트를 같이 바꾸지 않는다. 수정은 ID 단위로 승인받은 뒤 적용한다. 작업 저장소는 `Final_VR_Tyche_Pivot`만 사용한다.

## 아직 하지 않은 것

- 코드·씬 패치
- 신규 음원 생성·연결
- FOOT STEP, 로코모션 속도의 Profiler 측정
- Quest/OpenXR 재현
- 로코모션 씬을 빌드 목록에 넣을지는 이번 QA에서 요청되지 않음. 패치 시 variant 씬(`3_PPE_Room_Train_Test_mask.unity`)은 열거나 수정하지 않는다.

## 후속

1. 이 문서의 ID를 수정 순서로 쓴다.
2. 씬 작성 항목(QA-01, 02, 03, 06, 12, 15)은 Inspector 값을 바꾸고, 런타임에 크기·문구를 덮어쓰지 않는다.
3. 음원 항목(QA-04, 05, 10, 14)은 클립이 준비된 뒤에만 직렬화 필드를 연결한다. 없으면 자동 생성하지 않고 오류로 남긴다.
4. 착용 선행 조건(QA-07, 08, 09)은 상태 소유자(`PPEHazmatEquipController`, `PPEEquipmentVisualController`, `PPEVoiceFlowDirector`)만 변경한다.
5. 각 수정 후 정적 확인 → Unity Editor 확인 → Quest/OpenXR 확인을 이 문서에 추가한다.
6. `Teleport_3_Exit/EXIT` 라벨의 위치·로테이션은 사용자 작성값을 기준으로 보존한다. 자동 `Y=180°` 보정은 제거했다. 현재 라벨은 글자를 보존하기 위해 TMP 전용 overlay 머티리얼을 사용하지만, 마커와의 실제 색상·발광 외관은 아직 일치하지 않아 후속 시각 검증 항목으로 남긴다. 기존 마커 전용 머티리얼을 글자에 직접 넣으면 글리프가 사라지는 문제가 있어, 다음 작업에서 별도 TMP 셰이더의 blend/glow를 비교한다.

## 2026-08-26 후속: 로코모션 PPE 룸 음성·거울·진열장·중도 복귀 회귀

### 이번 변경이 대응하는 사용자 요청

- 정상 방호복을 반복해서 잡아도 같은 Grab 안내 음원이 매번 다시 시작되지 않게 한다.
- 거울 관찰 구역에 들어가면 작성된 게이지가 표시되고 기존 5초 관찰 흐름으로 진행되게 한다.
- 방호복 행거를 제외한 PPE 진열장 앞에서 환경 충돌체가 플레이어를 과도하게 밀어내지 않고, 기존 아이템 마커를 손으로 잡을 수 있게 한다.
- 활성 PPE 세션 중 `Teleport_3_Exit` 영역에 걸어서 들어가면 중도 복귀가 실행되게 한다.
- 작업 대상은 마지막 Unity 로그에서 열린 단일 씬 `Assets/Scenes/3_PPE_Room_3mode_loco.unity`이다. `_cam`을 포함한 다른 variant 씬은 수정하지 않는다.

### 보존해야 하는 기존 동작

- PPE별 작성 Transform, 마커 Collider, `XRGrabInteractable` Interaction Layer, 패널 UI와 음원 참조는 유지한다.
- 방호복 외 PPE의 기존 Grab 안내 규칙, 오염/정상 판정, 착용 순서, Wrong Answer SFX는 바꾸지 않는다.
- 거울 게이지의 작성 위치·크기·색상·Fill·5초 시간과 거울 시선 각도는 바꾸지 않는다.
- Exit 마커의 작성 Transform·표시·점멸과 완료 복귀 경로는 유지한다.
- 환경 충돌 루트의 벽·다른 가구 차단과 XR 입력 레이어 격리는 유지한다.

### 변경 전 필수 질문

1. 기존 Inspector/씬 작성값을 보존하는가?
   - 보존한다. 음원·UI·마커·PPE Transform은 변경하지 않는다. 진열장 환경 차단체만 가구 전체 Bounds가 아닌 접근 가능한 후면 차단면으로 다시 산출하며, 그 규칙은 Editor 구성 도구와 검증 하네스의 단일 기준으로 둔다.
2. 단일 기준 오브젝트와 상태 소유자는 무엇인가?
   - 음성 소비 상태는 `PPEVoiceFlowDirector`, 거울 관찰 상태는 `PPEFinaleController`, 환경 충돌 형상은 `PPERoomEnvironmentCollisionSetup`, 중도 복귀 상태는 `PPEFinaleController.RequestExitReturn()`이 소유한다.
3. 입력 이벤트, Interactor/Caster, Raycaster, Layer, Collider의 전체 경로는 무엇인가?
   - PPE Grab은 `Controller Grip/Select -> NearFarInteractor -> SphereInteractionCaster(Physics Layer 0/6) -> XR Item Marker_small Collider -> XRGrabInteractable(Interaction Layer) -> selectEntered -> PPEInspectionState/PPEVoiceFlowDirector`이다. 환경 차단체는 Layer 2 `Ignore Raycast`이고 Caster/Ray에서 제외되어 직접 입력을 소비하지 않지만, `CharacterController`를 진열장 앞에서 멈춰 손과 마커의 실제 거리를 늘린다. Exit는 `XR Origin 이동 -> Exit 작성 BoxCollider XZ 진입 -> PPEExitTeleportMarkerRelay.Update -> PPEFinaleController.RequestExitReturn()`이다.
4. 실패 시 런타임 자동 수리 대신 명확한 오류로 멈춰야 하는가?
   - 그렇다. 누락 참조·Collider·음원을 런타임 생성하거나 검색하지 않는다. 기존 한 번의 명확한 오류 경로를 유지하고 씬 구성은 명시적 Editor 도구로만 갱신한다.
5. 함께 영향을 받는 소비자는 무엇인가?
   - 방호복 Grab 음성, 거울 게이지/퀴즈 전환, CharacterController와 진열장 접근 거리, PPE 마커 직접 Grab, Exit 중도 복귀가 영향 대상이다. 카드·모달·텔레포트 레이·다른 가구·거울 렌더링은 비대상이다.
6. 변경 전 기준 실행과 변경 후 비교 실행은 무엇인가?
   - 변경 전 로그에서 `PpeArea` 음성 뒤 `Completed`로 자동 전이되고, 이후 PPE Grab `selectEntered/selectExited`가 반복된 사실을 기준으로 삼는다. 변경 후에는 `PpeArea` 유지, 방호복 Grab 안내 세션당 1회, 진열장 후면 차단면 Bounds, 마커 접근 여유, Exit 복귀 호출, 거울 게이지 0% 시작을 각각 비교한다.
7. 어디까지 실제로 검증했는가?
   - 변경 전에는 씬 YAML·코드·Editor.log 정적/실행 기록까지 확인했다. 변경 후 정적 컴파일과 Editor 하네스를 수행하며, 실제 손 추적·Quest/OpenXR 양안·공간 이동은 수동 검증으로 분리한다.

### 변경 전 근본 원인 증거

- `PPEVoiceFlowDirector.OnHazmatGrabbed()`는 방호복 Grab마다 `PlayPpeConditionalVoice(m_HazmatPanelVoice)`를 호출하고 기존 Voice를 중단한 뒤 같은 클립을 다시 시작한다. 장갑·부츠·테이프에 있는 세션 소비 상태가 방호복 Grab에는 없다.
- 대상 씬의 `ppe_area` VoiceStep은 `waitForSignal: 0`, `nextState: Completed`로 직렬화되어 있고 실제 로그도 `PpeArea -> Completed`를 기록한다. `Completed`는 `IsActivePpeModeSession`에 포함되지 않아 Exit의 `RequestExitReturn()`이 즉시 거절되고, PPE 완료 후 거울 안내를 시작하는 `PPEVoiceFlowDirector.Update()`의 `PpeArea` 조건도 더 이상 성립하지 않는다.
- `PPEFinaleController`는 `WaitingForEquipment`에서 모든 PPE가 이미 완료된 경우에만 `WaitingForMirror`로 넘어가므로, 거울에서 완료 여부를 판정하고 부족 항목을 안내하는 기존 incomplete 분기가 사실상 도달 불가능하다.
- `PPE Environment Collision/Furniture - Metal Shelving`은 진열장 Renderer 전체 Bounds(`size.z=0.42775726`)를 solid BoxCollider로 덮고 있다. 활성 `CharacterController.radius=0.25`가 이 전면에서 밀려나며, Layer 2가 Grab Caster에서 제외돼도 손과 아이템 마커 사이의 접근 거리가 커진다.

### 적용한 변경

- `PPEVoiceFlowDirector`에 교육 모드 세션 단위 방호복 Grab 음성 소비 상태를 추가했다. 첫 Grab만 음성을 재생하고 같은 세션의 후속 Grab은 재생하지 않으며, 다음 모드 세션 시작 시 상태를 초기화한다.
- `PpeArea`를 카드와 같은 명시적 상호작용 경계로 유지하고 대상 씬의 `ppe_area.waitForSignal`을 활성화했다. 음성 종료 뒤 `Completed`로 자동 전이하지 않으므로 거울 안내와 Exit 중도 복귀가 동일한 활성 PPE 세션을 참조한다.
- `PPEFinaleController`가 활성 PPE 세션이면 거울 진입 관찰을 시작하도록 변경했다. PPE 완비 여부는 기존 설계대로 작성된 5초 관찰 뒤 판정하며, 미완료이면 기존 부족 항목 안내와 재진입 대기 상태로 돌아간다.
- PPE 진열장 `Furniture - Metal Shelving` 환경 충돌체는 후속 Quest 확인에서도 플레이어 캡슐을 계속 밀어내는 것이 확인되어 제거했다. 진열장 뒤의 기존 `Wall - Front`가 방 경계를 계속 차단한다. 마커 Collider와 Grab Interaction Layer는 변경하지 않았다.
- `PPERoomEnvironmentCollisionSetup`에서 상호작용 진열장을 차단 대상에서 제외하고, 명시적 Configure 실행 시 기존의 불필요한 차단체를 제거하도록 했다. 검증 하네스는 진열장 차단체가 존재하지 않는 상태를 요구한다.
- `PPELocomotionPpeRegressionValidationHarness`에 방호복 음성 세션당 1회, `PpeArea` 지속, 거울 참조, Exit 걸어서 진입 참조 검사를 추가했다.

### 영향 범위

- 변경한 런타임 소비자는 방호복 Grab 음성, PPE 세션 유지, 거울 관찰 시작 조건이다.
- 변경한 씬 값은 `3_PPE_Room_3mode_loco.unity`의 `ppe_area.waitForSignal`이며, Metal Shelving용 환경 차단 GameObject/BoxCollider는 제거했다.
- `_cam` 등 다른 씬 variant, PPE 마커, 아이템 Collider, XR Interactor/Caster, UI 작성값, 음원 참조는 변경하지 않았다.

### 완료한 검증

- `git diff --check`: 오류 없음. 저장소 기존 줄바꿈 변환 경고만 확인했다.
- 대상 씬 diff: 새 GameObject/Component/FileID 없이 직렬화 값 3개만 변경됨을 확인했다.
- `Assembly-CSharp.csproj` 정적 컴파일: 경고 0개, 오류 0개.
- `Assembly-CSharp-Editor.csproj` 정적 컴파일: 경고 0개, 오류 0개.
- 입력 경로 정적 확인: 환경 충돌체는 Layer 2이고 Grab Caster 대상에서 제외되므로 마커 선택을 직접 가로채지 않는다. 기존 체감 문제는 전면 solid 차단으로 생긴 접근 거리 증가와 일치한다.
- 1차 후면 차단면 적용 뒤 Unity 검증 메뉴는 PASS했지만, Quest 수동 확인에서 전진 시 뒤로 밀리는 현상이 남았다. 진열장 뒤 `Wall - Front` 차단체가 별도로 존재함을 확인하여 중복 진열장 차단체를 완전히 제거하는 2차 수정으로 교정했다.

### 아직 필요한 수동 검증

- 1차 수정 상태에서 `Tools > PPE > Validate Locomotion PPE Regressions`와 `Tools > PPE > Validate Room Environment Collision`은 PASS했다. 진열장 차단체를 제거한 2차 수정이 Unity에 임포트된 뒤 두 메뉴를 다시 실행해야 한다.
- Play Mode에서 교육 세션 첫 방호복 Grab만 안내가 재생되고 반복 Grab에는 재생되지 않으며, 새 세션에서는 다시 1회 재생되는지 확인한다.
- 진열장 앞에서 플레이어가 과도하게 밀리지 않고 헬멧을 포함한 각 `XR Item Marker_small`을 손으로 직접 잡을 수 있는지 확인한다.
- PPE 미완료·완료 양쪽에서 거울 게이지가 0%부터 5초 동안 표시되고, 미완료 안내 또는 완료 결과로 각각 전이되는지 확인한다.
- 활성 PPE 세션 중 Exit 영역에 걸어서 진입했을 때 기존 음성·페이드·원위치 복귀·모달 복귀가 순서대로 실행되는지 확인한다.
- Quest/OpenXR에서 컨트롤러/손 Grab, 양안 거울 표시, 이동 중 충돌 체감을 별도로 확인한다.

### 2026-08-26 3차 후속: PPE 마커 물리 충돌과 조이스틱 기준 방향

#### 대응 요청과 보존 동작

- 진열장 앞으로 계속 이동할 수 있고 PPE 마커 가까이 접근할 수 있어야 한다.
- 사용자가 몸과 HMD를 돌린 뒤 조이스틱을 앞으로 밀면 현재 시선 방향으로 이동해야 한다.
- 마커의 작성 Transform·반경·표시·Grab Collider 등록, PPE 판정과 진열장/벽 시각은 유지한다.

#### 전체 경로와 근본 원인

- 접근 충돌 경로는 `Move 입력 -> PPEConfigurableDynamicMoveProvider -> XRBodyTransformer -> CharacterController(layer 0) -> XR Item Marker_small SphereCollider(layer 6, isTrigger=false)`였다. `DynamicsManager`의 Layer Collision Matrix가 전체 허용이라 마커 25개가 선택 영역이면서 solid 장애물로도 작동했다.
- Grab 경로는 `NearFarInteractor -> SphereInteractionCaster(layer 0/6) -> XR Item Marker_small -> XRGrabInteractable`이다. `DynamicsManager.m_QueriesHitTriggers=1`이므로 마커를 Trigger로 바꿔도 근거리 선택 Query는 유지된다.
- 이동 방향 경로는 `Left/Right Move Input -> PPEConfigurableDynamicMoveProvider(HeadRelative) -> m_HeadTransform -> forward`이다. 대상 씬의 `m_HeadTransform`과 작성 `m_ForwardSource`가 활성 `XR Origin (VR)/Camera Offset/Main Camera`가 아니라 비활성 `XR Origin (Hand Tracking)/Hand Tracking Camera`를 참조했다. 비활성 카메라 회전이 고정되어 조이스틱 전진이 월드 정면에 묶였다.

#### 적용한 변경

- 대상 씬의 `XR Item Marker_small` Collider 25개를 `isTrigger=true`로 변경했다. Sphere 반경과 Transform은 변경하지 않았고 BoxCollider로 교체하지 않았다. Sphere Trigger가 현재 원형 마커 선택 범위를 가장 정확히 보존한다.
- 이동 Provider의 `m_HeadTransform`과 `m_ForwardSource`를 활성 `XR Origin (VR)/Camera Offset/Main Camera`로 교체했다. 좌·우 이동 방향은 모두 `HeadRelative`로 유지했다.
- `PPERoomEnvironmentCollisionSetup`의 명시적 Configure/Validate에 마커 Trigger 작성과 검사를 추가했다. 런타임 자동 수리는 추가하지 않았다.
- `PPEGameViewControlSetup`과 `PPELocomotionPpeRegressionValidationHarness`가 활성 XR 카메라 참조와 좌·우 HeadRelative 설정을 검증하도록 보강했다.

#### 검증

- 정적 씬 확인: 이름이 `XR Item Marker_small`인 Collider 25개 전부 Trigger이며, 이동 Provider 두 카메라 참조는 활성 Main Camera FileID를 사용한다.
- `Assembly-CSharp-Editor` 정적 빌드: 오류 0개. 기존 패키지/API 경고만 존재한다.
- Unity Editor: 외부 변경 임포트와 컴파일 완료 후 두 검증 메뉴를 다시 실행해야 한다.
- Quest/OpenXR: 뒤돌아 조이스틱 전진 시 현재 HMD 정면으로 이동하는지, PPE 마커를 통과해 가까이 접근하면서도 근거리 Grab이 유지되는지 확인해야 한다.

### 2026-08-26 4차 후속: 진열장 차단체 최종 결정

- 위 2차 후속의 진열장 차단체 제거는 원인 분리를 위한 중간 상태였다.
- 사용자가 “가구로는 들어가지지 않되 안에 장식된 PPE는 잡혀야 한다”는 최종 불변조건을
  확정했으므로, 전체 높이 차단체를 되살리지 않고 바닥 베이스 전용 BoxCollider를 사용한다.
- 최종 작성값은 높이 `0.3m`, 깊이 `0.12m`, 진열장 앞면에서 안쪽 `0.25m` 후퇴다.
- Marker Trigger, Near caster `Collide`, Far caster Layer 6 제외와 함께 적용한다. 상세 수치와
  검증 조건은 `2026-08-24_PPE_Room_Environment_Collision.md`를 기준 문서로 삼는다.

## 2026-08-28 Game View 오른쪽 니트릴 내부장갑 승인 불가

### 변경 전 필수 질문

1. Inspector/씬 작성값은 보존한다. 좌·우 내부장갑의 Transform, Collider, ItemType, 패널 참조와
   착용 손 모델은 변경하지 않는다.
2. 착용 판정 소유자는 `PPEActionPanelController`, Game View 마우스 선택 소유자는
   `PhysicalHmdSimulatorGate`이다.
3. 입력 경로는 `Game View 왼쪽 클릭 → 작성된 오른손 NearFarInteractor → XRGrabInteractable Select
   → 마우스 Grab Anchor → 두 번째 클릭 → TryActivateBodyProximityFromGameView()`이다.
4. 누락 참조를 런타임 자동 생성하지 않고 기존 명시적 Editor 구성·검증 경로를 유지한다.
5. Game View 좌·우 장갑과 물리 XR 손별 승인 경로가 영향 소비자다. Quest 손 구분 규칙은 보존한다.
6. 변경 전에는 오른쪽 내부장갑만 두 번째 클릭 승인이 실패하고 왼쪽은 통과하는 비대칭을 기준으로
   삼는다. 변경 후에는 같은 Game View 조작으로 좌·우를 각각 비교한다.
7. 정적 코드·씬과 C# 컴파일까지 확인했으며, 변경 후 Unity Play Mode 및 Quest 확인은 남아 있다.

### 근본 원인

- 현재 씬의 좌·우 내부장갑 ItemType, Action Panel, Collider, 장착 Slot과 손 모델 참조는 모두
  대칭으로 연결되어 있었다.
- Game View는 PPE 양쪽을 하나의 작성된 오른손 `NearFarInteractor`로 조작한다.
- Game View 승인 메서드가 물리 XR용 `IsBodyProximityActivateAttempt()`를 재사용했다. 이 메서드는
  깨끗한 장갑을 실제 착용할 손으로 잡았을 때 근접 자동승인을 억제한다. 따라서 오른쪽 장갑은
  `오른쪽 장갑 + 오른손 Interactor`라 차단되고, 왼쪽은 손 불일치 때문에 우연히 통과했다.

### 적용한 변경과 영향 범위

- Game View 전용 두 번째 클릭 승인은 작성된 Body Attach 거리, 선택 상태와 기존
  `CanResolveChoice()`만 확인하고 물리 손별 자동승인 억제를 재사용하지 않도록 분리했다.
- 일반 XR의 `IsBodyProximityActivateAttempt()`와 좌·우 실제 손 판정은 변경하지 않았다.
- `PPENitrileInnerGloveWearSetup`의 대상 씬을 현재 로코모션 씬으로 갱신했다.
- 같은 하네스에 Game View 전용 메서드가 Body Attach 거리를 유지하고 물리 손 억제를 다시 호출하지
  않는지 검사하는 회귀 조건을 추가했다.

### 검증

- 새 파일을 포함한 Runtime/Editor C# 빌드 오류 0개를 확인했다.
- Unity Play Mode에서 오른쪽 내부장갑을 첫 클릭으로 잡고 착용 손 위치로 이동한 뒤 두 번째 클릭했을
  때 `UseApproved`와 오른손 착용 시각이 나타나는지 확인해야 한다. 같은 순서로 왼쪽도 비교한다.
- Quest/OpenXR에서는 기존대로 오른쪽 장갑은 오른손, 왼쪽 장갑은 왼손으로 잡는 손별 규칙이 유지되는지
  별도 확인해야 한다.
