# 2026-08-21 PPE 로코모션 씬 후속 회의록

- 날짜: 2026-08-21
- 대상 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity`
- 선행: `Docs/MeetingNotes/2026-08-20_PPE_Room_Train_Test_Followup.md` 절 25–36
- 같은 날 별건: `Docs/MeetingNotes/2026-08-21_PPE_Training_Dashboard_Concept.md`
- 지정하지 않은 variant `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`는 이번 후속에서 열거나 수정하지 않았다
- idle 하반신 리깅·walk 본 연결 작업은 사용자가 중단했다. 이 문서 범위에 넣지 않는다

## 오늘 작업 요약

로코모션 복사 씬에서 교육모드 음성·체크리스트·발소리·중도 엑시트를 맞췄다. 원본 mask 씬 동작과 퀴즈 완료 복귀는 유지했다.

| 구분 | 내용 | 절 |
| --- | --- | --- |
| 음성 재연결 | 교육 버튼 006, 밀폐 004, 누출 005, 이동 EDU_001, 방호복 완료 EDU_006 | 1 |
| 착용 거절 음성 | 방호복 중복 204, 작업계획 불일치 205, Wrong Answer SFX | 2 |
| 태블릿 해제 | 방호복 착용 중이면 팔 마커 안내를 재생하지 않음 | 3 |
| 송기마스크 패널 | 다른 PPE와 같은 이름 전용 레이아웃 | 4 |
| 발소리 | Move가 켜진 평면 이동 중 `Foot Step` | 5 |
| 중도 엑시트 | `Teleport_3_Exit` 영역 진입 복귀. 도착 시 EDU_206 후 페이드 | 6 |
| 체크리스트 | 태블릿 서명 완료 즉시 표시. 착용해도 행 체크 | 7 |

보존한 기존 동작: 태블릿 서명 시퀀스, Grab·몸 근접 착용, 작업계획 착용 세트, 원본 mask 씬 텔레포트 엑시트, 퀴즈 완료 복귀, `Teleport_3_Exit` Transform(사용자가 옮긴 위치).

## 1. 교육모드 음성 재연결

### 이번 변경이 대응하는 요청

교육 모드 버튼, 밀폐/누출 선택, PPE 구역 이동, 방호복 착용 완료에 새 음원을 연결한다.

### 적용한 변경

로코모션 씬 `PPEVoiceFlowDirector`만 연결했다. 클립 GUID는 아래와 같다.

| 시점 | 필드 | 파일 | GUID |
| --- | --- | --- | --- |
| 교육모드 버튼 | `m_EducationModeSelectedVoice` | `VO_PPE_MODAL_006_EduSelect` | `54bc11974a539814596d94f7fb62847c` |
| 밀폐공간 선택 | `m_ConfinedSpaceSelectedVoice` | `VO_PPE_MODAL_004_ConfinedSpace_Select` | `8050b9c03319ffc4b9b3d4c2ea040baf` |
| 누출 선택 | `m_LeakResponseSelectedVoice` | `VO_PPE_MODAL_005_Leak_Selec` | `d0d506a1f0ccc8b4797faaa1b085a836` |
| `education_selected` 이동 | 스텝 클립 | `4_VO_PPE_EDU_001_PPE_MoveToPPE` | `eb6788cefe9bd8b439d16906a20f3f10` |
| 방호복 착용 완료 | `m_HazmatEquippedVoice` | `4_VO_PPE_EDU_006_Suit_End` | `d0995ee7231600141bc9d520e8ee54a1` |

교육모드 버튼은 `ScenarioDetailModal.PpeEducationModeChoiceStarted`에서 `NotifyPpeEducationModeChoiceStarted()`로 006을 재생한다. 밀폐/누출 선택은 `PlayStepRoutine`의 `EducationSelected` 앞에 시나리오 클립을 붙인 뒤 EDU_001로 이동한다.

이전 `VO_PPE_MODAL_004_EduSelect`는 디스크에서 제거됐다. 교육 선택 음원은 006이다.

### 근본 원인

같은 파일명의 EDU_001·EDU_006을 새 테이크로 교체했고, 모달 004 파일명은 밀폐 선택용으로 바뀌었다. Unity가 씬을 중간 저장하면 디렉터 클립 필드가 `{fileID: 0}`으로 비워질 수 있다.

### 영향 범위

`PPEVoiceFlowDirector`, `ScenarioDetailModal`, 로코모션 씬 직렬화 필드, `Assets/Audio/Voice/3_Modal`, `Assets/Audio/Voice/4_PPE`. 훈련·테스트 전용 음성 필드는 바꾸지 않았다.

## 2. 방호복 중복·작업계획 불일치 거절

### 이번 변경이 대응하는 요청

이미 입은 방호복을 다시 입거나, 선택한 작업계획에 없는 PPE를 입으려 하면 틀린 선택 SFX와 전용 음성을 낸다.

### 적용한 변경

- `CanApprovePpeUse`가 교육모드에서 거절하면 `RejectUseWithWrongSfx`로 패널 Wrong Answer SFX와 클립을 재생하고 착용하지 않는다.
- 중복 방호복: `m_HazmatAlreadyEquippedVoice` → `4_VO_PPE_EDU_204_SuitAlready` (`a36108644d6c9b049a2c0395ab903e7b`)
- 작업계획 불일치: `m_WorkPlanMismatchVoice` → `4_VO_PPE_EDU_205_PPE_forScenario` (`7a067432c37b0fc49a77536389d936f7`)
- 훈련·테스트 거절 경로는 기존 `TrainingWrongButtonVoice`를 유지한다.

### 근본 원인

중복 착용은 `PPEHazmatEquipController`가 `UseApproved` 이후 조용히 return해서 음성이 나가지 않았다. 작업계획 불일치는 거절만 있고 교육 전용 클립이 없었다.

## 3. 태블릿 해제 안내

### 이번 변경이 대응하는 요청

태블릿을 놓을 때, 이미 방호복을 입었으면 오른쪽 팔 마커를 잡으라는 안내를 재생하지 않는다.

### 적용한 변경

`NotifyTabletReleased`는 교육모드 첫 해제만 안내한다. `m_HazmatEquipController.IsEquipped`이면 `m_TabletReleasedVoice`를 재생하지 않는다. 첫 해제 한 번만 허용하는 기존 플래그는 유지한다.

## 4. 송기마스크 패널 크기

### 이번 변경이 대응하는 요청

송기마스크 Grab 패널을 다른 PPE와 같은 이름 전용 크기로 맞춘다.

### 적용한 변경

로코모션 씬의 활성 `Mask Action Panel`만 `applySharedPresentationLayout`을 켜고 작성 `sizeDelta.y`를 310으로 맞췄다. Grab 시 기존 `nameOnlyLayout`(320×114)을 쓴다. 런타임 좌표 대입은 넣지 않았다.

### 근본 원인

마스크 패널만 공유 레이아웃 플래그가 꺼져 있어 예전 3버튼 높이 `{320, 417.7577}`가 그대로 보였다.

## 5. 로코모션 발소리

### 이번 변경이 대응하는 요청

스틱 로코모션으로 이동하는 동안 FOOT STEP SFX를 재생한다.

### 적용한 변경

- 로코모션 씬 AudioManager SFX 목록에 `Foot Step`을 등록했다. 클립 GUID `117904f8108d7d64abe9fa00e7d4d285`.
- `PPEIdleLocomotionAnimator`가 Move 프로바이더가 켜진 채 평면 속도가 문턱 이상일 때만 0.4초 간격으로 `AudioManager.PlaySfx`한다.
- 텔레포트 점프(`m_TeleportDeltaIgnore`)는 기존처럼 걷기·발소리를 무시한다.
- 이 컴포넌트가 붙은 `PPE_D_Player_Idle`은 작업 중 꺼져 있어 다시 켰다.

### 근본 원인

`Assets/Audio/SFX/Foot Step.ogg`는 있었으나 AudioManager 목록과 이동 중 재생 경로가 없었다.

헤드셋에서 들리지 않으면 Move가 꺼진 상태, idle 오브젝트 비활성, Unity 중간 저장으로 SFX 항목이 빠진 상태, AudioManager 미생성 순서로 본다. 환경 원인(Quest Link, 출력 장치)은 그 다음이다.

## 6. 중간 엑시트 영역 복귀와 EDU_206

### 이번 변경이 대응하는 요청

텔레포트 없이 중도 엑시트 영역에 들어가면 세션을 종료한다. 도착 시 `4_VO_PPE_EDU_206_StopScenario`를 재생한다.

### 적용한 변경

- 사용자가 옮긴 `Teleport_3_Exit` Transform은 유지했다. 마지막 확인 위치 `(-10.655, 0.429, -3.035)`.
- 로코모션 시험에서는 `TeleportationAnchor`를 끄고 `PPEExitTeleportMarkerRelay.m_ReturnOnWalkEnter`를 켠다. XR Origin과 작성 BoxCollider의 XZ 발자국이 겹치면 `PPEFinaleController.RequestExitReturn()`을 호출한다.
- 디렉터는 로코모션 시험에서 엑시트 마커를 씬 시작부터 보이게 둔다. 걷어 들어가 복귀하는 것은 `IsActivePpeModeSession`(TeleportInstruction / PpeArea / Training / Test)일 때만이다. `EducationSelected`에서는 복귀하지 않는다.
- 도착 시 `NotifyMidExitArrived()`가 `m_MidExitStopVoice`(`73db906f0969c194da2d5b75f3d15758`)를 재생한다. 대사가 끝난 뒤에 기존 페이드 복귀가 이어진다.
- 퀴즈 완료 후 `ReturnToModeChoices()`에는 206을 넣지 않았다. 원본 mask 씬의 마커 텔레포트 엑시트는 그대로다.

### 근본 원인

로코모션 시험은 `PpeArea`에서도 엑시트 마커를 꺼 두었고, 복귀는 텔레포트 이벤트에만 연결되어 있었다.

## 7. 교육모드 체크리스트 패널

### 이번 변경이 대응하는 요청

태블릿 체크(서명 시퀀스 완료) 직후 Window Canvas 체크리스트를 띄운다. PPE를 입으면 해당 행에 체크가 들어간다.

### 적용한 변경

- `Window Canvas/CheckList`는 활성으로 둔다. 스크립트는 GameObject를 켜지 않고 `CanvasGroup` 알파와 Confined/Leak 루트만 바꾼다. Play 시작 때 꺼져 있으면 `OnEnable`이 돌지 않는다.
- `PPETabletChecklistController.DocumentCompleted`를 구독해 서명 완료 프레임에 바로 `ApplyVisibility`한다.
- 패널을 숨길 때 `approvedItemTypes`를 지우지 않는다. 표시 중에는 매 프레임 `RefreshCheckMarks`한다.
- `PPEEquipmentVisualController.IsItemUsed`는 같은 타입의 **어느** used 슬롯이든 true다. 이전에는 첫 슬롯만 봐서 Clean/Contam 중복 진열에서 착용해도 false가 나왔다.
- 행 체크는 `UseApproved` 집합, 방호복 `IsEquipped`, `IsItemUsed`를 함께 본다. 장화·장갑처럼 좌우 쌍은 둘 다 입어야 한 행이 켜진다.

표시 조건은 교육모드 + 작업계획 Confined/Leak + `IsDocumentCompleted`다. 레이아웃·라벨은 씬 작성값을 덮어쓰지 않는다.

### 근본 원인

CheckList가 비활성이면 컴포넌트가 돌지 않았다. 숨김 시 착용 집합을 지워서 태블릿 완료 전에 입은 항목이 사라졌다. `IsItemUsed`가 첫 슬롯만 반환해 착용 시각과 HUD 체크가 어긋났다.

## 디스크 변경 주의

이번 후속의 권한 범위는 로코모션 씬과 위 스크립트·음원이다. 작업 트리에 같이 보이는 아래 항목은 이 회의록의 적용 변경이 아니다.

- `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity` 대량 diff
- `Assets/Materials/PPE/PPE_Room_Floor.mat`
- `LiberationSans SDF - Fallback.asset`
- `output/html/PPE_Training_Dashboard.html`, `Tools/Launch-CodexAdmin.ps1`

Unity가 로코모션 씬을 연 채로 저장하면 디렉터 클립이 다시 `{fileID: 0}`이 될 수 있다. YAML을 고친 뒤에는 디스크에서 다시 로드한다.

## 영향 범위

| 소비자 | 절 |
| --- | --- |
| 모드·작업계획 선택 음성 | 1 |
| 착용 승인·거절 SFX/VO | 2 |
| 태블릿 해제 음성 | 3 |
| 마스크 Action Panel | 4 |
| idle 걷기 애니메이터·SFX | 5 |
| 엑시트 마커·피날레 복귀 | 6 |
| Window Canvas 체크리스트 | 7 |

한 패치에서 입력 경로, 모달 상태, 텔레포트, PPE Grab을 함께 바꾸지 않았다. 엑시트는 텔레포트 소비자에서 영역 진입 소비자로만 로코모션 씬을 바꿨다.

## 완료한 검증

- 정적 확인: 로코모션 씬 디렉터 클립 GUID, AudioManager `Foot Step`, CheckList 활성, 엑시트 `m_ReturnOnWalkEnter`, 마스크 패널 높이 310.
- Unity Editor Play Mode: 이 후속 구간에서는 라이선스·중간 저장 때문에 완료로 보고하지 않는다.
- Quest/OpenXR: 없음. 발소리, 206, 체크리스트 체크, 양안은 헤드셋 재현이 남는다.

## 아직 필요한 검증

- 교육 버튼 → 006, 밀폐 → 004, 누출 → 005, 이동 → EDU_001, 방호복 완료 → EDU_006이 실제 출력 장치에서 나는지
- 204/205가 Wrong Answer와 함께 나고 착용이 거절되는지
- 태블릿 서명 완료 즉시 체크리스트가 보이는지, 좌우 쌍을 모두 입으면 행이 켜지는지
- Move가 켜진 뒤 스틱 이동 중 `Foot Step`이 0.4초 간격인지. 안 들리면 최근 씬 저장으로 SFX 항목이 지워졌는지부터 본다
- 엑시트 영역 진입 시 206이 끝까지 들린 뒤 페이드 복귀하는지. 퀴즈 완료 복귀에는 206이 없는지를 대조한다
- `Teleport_3_Exit` 위치는 사용자가 옮긴 값을 유지했는지

## 2026-08-24 후속 적용: 로코모션 발소리 입력 경로

### 이번 후속이 대응하는 사용자 요청

- 발소리는 `PPE_D_Player_Idle` 모델의 활성 상태나 실제 이동 거리로 판정하지 않는다.
- LOCOMOTION 씬에서 좌·우 Move 입력이 들어오고 이동 제공자가 활성화된 동안 `Foot Step` SFX를 재생한다.
- 기존 PPE 착용 SFX 연결을 함께 확인하되, 발소리 수정 과정에서 착용 승인·거절음과 교육 음성의 기존 동작은 바꾸지 않는다.

### 2026-08-24 정적 확인 결과

- `Foot Step`은 `AudioManager` SFX 라이브러리에 `volume: 1`, `enabled: 1`로 등록되어 있다.
- 현재 발소리 호출은 `PPE_D_Player_Idle`에 붙은 `PPEIdleLocomotionAnimator.Update()`가 담당한다.
- 해당 모델이 비활성이면 `Update()`가 실행되지 않아 `AudioManager.PlaySfx("Foot Step")`까지 도달하지 않는다. 따라서 현재 증상은 음원 볼륨보다 입력 소비자 배치 문제다.
- LOCOMOTION 이동 입력의 소유자는 `PPEConfigurableDynamicMoveProvider`이며, 좌·우 `leftHandMoveInput`/`rightHandMoveInput`을 읽어 이동을 계산한다.
- PPE 착용 성공은 각 `PPEActionPanelController.useSfxId`를 통해 공용 `AudioManager/SFX` AudioSource에 재생된다. 방호복=`cloth`, 장화=`Boots`, 장갑=`Gloves`, 헬멧·등판·SCBA=`harness`, 테이프=`Taping`이다.
- `UseApproved`에서는 장비별 `useSfxId`와 `Correct Answer`가 같은 처리에서 연속 호출된다. 마스크·고글·페이스실드는 `useSfxId`가 비어 있어 전용 착용음 없이 `Correct Answer`만 호출된다.
- 교육 모드 음성은 SFX와 별도 경로다. 방호복 완료 후 `4_VO_PPE_EDU_006_Suit_End`, 다음 PPE 완료 후 `4_VO_PPE_EDU_009_NextPPE`를 사용한다.

### 적용한 변경

- 기능 대상은 `Assets/Scenes/3_PPE_Room_3mode_loco.unity` 하나로 확정했다. 스크린샷용 `3_PPE_Room_3mode_loco_cam.unity`는 변경하지 않았다.
- `PPEConfigurableDynamicMoveProvider`가 좌·우 `leftHandMoveInput`/`rightHandMoveInput`을 각각 읽고, 어느 한쪽이라도 0이 아닌 Move 입력이면 `Foot Step` 타이머를 진행한다. 별도 발소리 문턱은 추가하지 않고 XRI Move 액션의 작성된 데드존 결과를 따른다.
- 작성값 `m_FootstepSfxId: Foot Step`, `m_FootstepInterval: 0.4`를 로코모션 Move 컴포넌트에 직렬화했다.
- 입력을 놓으면 감속 이동값이 남아 있어도 발소리는 즉시 멈춘다. Move 프로바이더가 비활성이면 `OnDisable()`에서 타이머를 초기화한다.
- `PPEIdleLocomotionAnimator`에서는 SFX 필드와 `AudioManager.PlaySfx` 호출을 제거했다. 선택적 플레이어 모델과 Animator는 발소리 재생 여부에 관여하지 않는다.
- `PPELocomotionPpeRegressionValidationHarness`의 대상 경로를 현재 로코 씬으로 바꾸고, 입력 소유자·직렬화값·AudioManager 음원·플레이어 모델 비의존성을 검사하도록 확장했다.

### 근본 원인

발소리 상태 소유자가 실제 입력 소유자인 Move 프로바이더가 아니라 사용하지 않는 `PPE_D_Player_Idle` 자식에 있었다. 이 오브젝트가 비활성이면 `PPEIdleLocomotionAnimator.Update()`가 호출되지 않아, 정상 Move 입력과 실제 XR Origin 이동이 있어도 발소리 호출까지 도달할 수 없었다.

### 영향 범위와 보존한 동작

- 변경 소비자는 로코모션 입력 중 `Foot Step` SFX 하나다.
- 이동 가속·감속, 텔레포트, 자동 이동, PPE 착용 승인·거절 SFX, `Correct Answer`, 교육 음성 호출 순서는 변경하지 않았다.
- `AudioManager`가 없을 때 런타임 자동 생성하지 않고 Move 컴포넌트 경로를 포함한 오류를 한 번 기록한다.

### 완료한 검증

- 정적 확인: 좌·우 Move 입력을 각각 읽고, 입력 중에만 타이머가 진행되며, 입력 해제·Move 비활성 시 타이머가 초기화되는 것을 확인했다.
- 씬 직렬화 확인: 작성값 3개가 Move 컴포넌트로 이동했고 `PPEIdleLocomotionAnimator`에는 발소리 필드가 남지 않았다. `Foot Step` 음원은 `enabled: 1`, `volume: 1` 상태를 유지한다.
- C# 빌드: `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj` 모두 오류 0개. 기존 패키지·deprecated API 경고만 남았다.
- Unity는 변경된 스크립트와 로코 씬 에셋을 감지해 import했다. 라이브 진단 연결이 도메인 재로드 뒤 복구되지 않아 Preview Scene 회귀 하네스 실행 완료 로그는 얻지 못했다.

### 아직 필요한 수동 검증

- Quest/OpenXR 연결 상태에서 좌·우 컨트롤러 각각의 Move 입력 시작·유지·해제에 따라 발소리가 시작·반복·정지하는지 확인한다.
- 실제 출력 장치에서 0.4초 간격과 음량을 확인한다.
- 텔레포트·HMD 물리 이동만으로 발소리가 재생되지 않고, PPE 착용 SFX와 교육 음성이 기존 순서를 유지하는지 확인한다.

## 2026-08-24 후속: 신규 PPE 착용 SFX 연결

### 변경 전 필수 판단

1. 기존 Inspector/씬 작성값은 보존한다. 대상 PPE의 `useSfxId`와 씬 `AudioManager.m_Sfx`의 신규 세 항목만 추가·변경하며, 착용 거리·상태·UI·보이스·기존 SFX 볼륨은 변경하지 않는다.
2. 착용 성공 상태의 소유자는 `PPEActionPanelController.ResolveChoice`이고, 실제 음원 조회·재생 소유자는 씬의 단일 `AudioManager`다.
3. 입력 경로는 `PPE Grab → 몸 근접 Trigger → CanApprovePpeUse → UseApproved → PlayActionSfx(useSfxId) → AudioManager.m_Sfx → SFX AudioSource.PlayOneShot`이다. Interactor·Caster·Collider·Interaction Layer는 이번 연결에서 변경하지 않는다.
4. 필수 음원이나 씬 참조가 없으면 런타임 자동 생성·대체하지 않고 Editor 적용 도구와 검증에서 명확한 오류로 멈춘다.
5. 함께 영향을 받는 소비자는 마스크·고글·페이스실드·안전모·니트릴 내부 장갑의 착용 성공 SFX다. `Correct Answer`, Wrong Answer, 교육 보이스, 체크리스트, 텔레포트, 거울, XR 양안은 보존한다.
6. 변경 전 기준은 마스크·고글·페이스실드의 `useSfxId`가 비어 있고, 안전모는 `harness`, 니트릴 내부 장갑은 `Gloves`를 사용하며 신규 세 클립은 AudioManager에 미등록인 상태다. 변경 후에는 요청된 PPE만 신규 ID를 사용하고 기존 장비의 ID는 유지되는지 비교한다.
7. 정적 확인, Unity Editor 씬 직렬화·오디오 로드·착용 성공 호출 검증까지 수행한다. 실제 음질·음량과 Quest/OpenXR 출력은 헤드셋 수동 검증으로 구분한다.

이번 변경이 대응하는 요청은 `Wearing Mask Glass Shield`를 정상 마스크·보안경(고글)·안면보호대(페이스실드), `Helmet`을 정상 안전모, `Nitril InnerGlove`를 좌·우 니트릴 내부 장갑의 착용 성공 시점에 재생하고, `PPE_A_SCBA`의 표시명을 `SCBA`로 바꾸는 것이다. 불량 마스크에는 착용음 ID를 넣지 않으며 오염 PPE 거절과 시나리오 불일치 거절은 기존 Wrong Answer 경로를 유지한다.

### 적용한 변경

- 씬 `AudioManager.m_Sfx`에 `Wearing Mask Glass Shield`, `Helmet`, `Nitril InnerGlove`를 각 원본 OGG 클립, `volume=1`, `enabled=true`로 추가했다.
- 정상 마스크·고글·페이스실드의 `useSfxId`를 `Wearing Mask Glass Shield`로 연결했다.
- 정상 스트랩 안전모의 `useSfxId`를 `Helmet`으로 연결했다.
- 좌·우 니트릴 내부 장갑의 `useSfxId`를 `Nitril InnerGlove`로 연결했다.
- `PPE/PPE_A_SCBA`의 `itemDisplayName`을 `송기 마스크`에서 `SCBA`로 변경했다. 해당 장비의 기존 `useSfxId=harness`는 유지했다.
- 명시적 적용·검증 메뉴 `Tools > PPE > Audio > Apply/Validate New Wear SFX and SCBA Label`을 추가했다. 필수 클립이나 씬 패널이 없으면 자동 대체하지 않고 오류로 중단한다.

### 불량 PPE 보존

- 불량 마스크·고글·페이스실드는 새 착용음 ID를 받지 않았고 기존 빈 값을 유지한다.
- 특히 불량 마스크는 `initialCondition=Contaminated`, `useSfxId` 빈 값, `wrongFeedbackSfxId=Wrong Answer`를 검증한다.
- `PPEActionPanelController`는 `UseApproved`일 때만 `useSfxId`를 재생한다. 불량 마스크의 `UseRejectedContaminated`에서는 착용음을 건너뛰고 기존 Wrong Answer 피드백만 재생한다.
- 스트랩 없는 불량 안전모도 `initialCondition=Contaminated`이므로 기존 `harness` 작성값이 남아 있어도 `UseApproved` 경로에 진입하지 않아 착용음이 재생되지 않는다.

### 완료한 검증

- Unity Editor 컴파일과 `PPEWearSfxSetup.Validate`를 통과했다.
- 씬 직렬화에서 정상 착용 대상 6개 패널의 ID, 신규 AudioManager 항목 3개, SCBA 표시명과 기존 `harness`를 확인했다.
- Play Mode에서 신규 세 ID를 각각 `AudioManager.PlaySfx`로 호출했을 때 씬 SFX AudioSource가 실제 재생 상태로 전환됐다.
- Play Mode에서 불량 마스크의 착용음 ID가 비어 있음을 확인하고, 공개 Wrong Answer 호출이 SFX AudioSource를 재생하는 것을 확인했다.
- Play Mode 종료 후 대상 씬은 dirty가 아니며 Unity Console 오류·경고는 각각 0개였다.

### 아직 필요한 수동 검증

- Quest/OpenXR 실제 출력 장치에서 정상 마스크·고글·페이스실드, 안전모, 좌·우 니트릴 내부 장갑을 몸 근접 Trigger로 착용해 음질·음량과 `Correct Answer` 동시 재생의 체감을 확인한다.
- 불량 마스크를 착용 시도했을 때 착용음 없이 Wrong Answer만 들리고 장착 처리되지 않는지 확인한다.
- SCBA를 Grab했을 때 패널 표시명이 `SCBA`로 보이는지 양안에서 확인한다.
