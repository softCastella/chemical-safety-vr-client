# 2026-08-19 본프로젝트 전 피봇 및 확장 아이디어 회의

- 일자: 2026-08-19
- 프로젝트: `Prototype_Tyche_Jinyoung` (본프로젝트 진입 전 프로토타입)
- 주제: 본프로젝트 전 피봇 및 확장 아이디어
- 상태: 아이디어 합의 후, 같은 날 후속 세션에서 열린 질문과 교수자 피드백을 확정. 2026-08-20 구현 정리는 `Docs/MeetingNotes/2026-08-20_PPE_Room_Train_Test_Followup.md`

## 1. 회의 목적

본프로젝트로 넘어가기 전에, 현재 PPE 착용 로직이 교육 목표와 교수자 피드백에 맞는지 점검한다. 오늘은 코드를 바꾸지 않고 피봇 방향과 확장 아이디어만 정리한다.

관련 기존 문서:

- `Docs/MeetingNotes/2026-07-16_PPE_Grab_Body_Equip_Interaction.md` — 잡아 신체 부위에 가져가면 흡착·장착하는 1차 설계
- `Docs/MeetingNotes/2026-08-09_PPE_Room_HandTest_Meeting.md` — 하자·정상 헬멧을 같은 자리에서 폐기 후 교체하는 현재 작성 방식

## 2. 현재 기준 동작

현재 착용 흐름은 UI 선택에 의존한다.

```text
단상 PPE 잡기
→ 관찰 패널 표시
→ 사용 / 폐기 / (마스크) 확인하기
→ Clean + 사용 = UseApproved
→ Front→Approach→Body 연출
→ PPE Body Anchor / hazmat_suit_on_10 표시
```

PPE는 종류마다 월드 오브젝트가 하나다. 초기 하자(Contaminated)를 폐기하면 같은 오브젝트 또는 같은 자리의 정상 시각으로 바뀐 뒤, 그 정상품을 사용해야 착용된다. 헬멧은 `helmet_wrong`과 정상이 같은 Pose에 겹쳐 있고, 폐기 후에야 정상이 활성화된다.

장착 시각의 단일 기준은 기존과 같다. `PPE Body Anchor → hazmat_suit_on_10`. 자식 슬롯, 손 모델 전환, 거울 표시는 이 루트를 따른다.

## 3. 교수자 피드백

### 3.1 착용 표현

UI에서 사용을 고르는 방식 대신, PPE를 손으로 잡고 몸쪽으로 직접 갖다 대서 입는 동작을 표현해야 한다.

### 3.2 하자·정상 배치

다양한 PPE를 단상에 모두 깔고, 종류마다 하자품과 정상품을 처음부터 둘 다 두어도 된다. 한 물건을 폐기해서 정상품이 나타나는 교체 연출은 요구하지 않는다.

후속 세션에서 보완된 교수자 취지는 다음과 같다. 하자품과 정품을 섞고, 다른 시나리오에 필요한 3D까지 뽑아 단상에 둔다. 패널 없이 골라 입게 하고, 폐기는 없으며 내려놓으면 그만이다. UI 패널은 입는 느낌이 적다. 리깅·세밀한 애니메이션보다 내가 입는 느낌이 우선이다.

## 4. 합의한 피봇 방향

풀장착 모델은 바꾸지 않는다. 바꿀 대상은 착용 승인 경로와 단상 배치 규칙이다.

| 유지 | 피봇 |
| --- | --- |
| `hazmat_suit_on_10` 및 슬롯 자식 Pose | `UseApproved`를 패널 사용 버튼이 아니라 몸 근접에서 발행 |
| 기존 흡착 연출, 손 모델 전환, 거울 레이어 | 종류마다 하자·정상 월드 오브젝트를 시작부터 따로 배치 |
| 음성, 퀴즈, 피날레가 구독하는 장착 완료 계약 | 폐기 후 청결 교체 상태 머신, 사용/폐기 패널 |

같은 날 후속 세션에서 목표 흐름을 고쳤다. 최종 해석은 6절을 따른다.

## 5. 확장 아이디어 (미구현)

오늘은 제안만 했고 코드·씬은 변경하지 않았다.

### 5.1 몸을 사용 버튼으로 쓰기

- 잡고 있음 + 해당 PPE의 착용 존 + 0.4~0.6초 유지 + 정상 상태 + (장화·장갑·테이프만) 방호복 착용 여부
- 통과 시 손에서 해제하고, 현재 손 위치에서 기존 흡착을 이어 간다
- 존 밖에서 놓으면 지금처럼 단상 작성 Pose로 복귀
- 스치기만 해서는 입지 않는다

PPE별 존 초안:

| PPE | 착용 존 |
| --- | --- |
| 방호복 | 가슴 캡슐 |
| 헬멧 | 머리 위 |
| 마스크 | 얼굴 앞 |
| 장갑 | 해당 손목. 몸통이 아님 |
| 장화 | 발 트래커가 없으므로 고개 숙인 바닥 앞 추정 존 |
| 테이프 | 장갑 착용 후 손목 |
| 등지게 | 등/가슴 |

### 5.2 패널의 역할

후속 세션에서 사용/폐기/확인하기 패널은 착용 승인 경로에서 빼는 쪽으로 확정했다. 기본은 패널 없이 몸에 대는 행동만 인정한다.

### 5.3 훈련/시험 존 표시

기존 훈련/시험 모드를 쓰면, 훈련에서만 희미한 착용 존을 보여주고 시험에서는 숨길 수 있다.

### 5.4 단상 배치

하자 PPE와 정품 PPE만 쌍으로 두지 않는다. 다른 시나리오에 필요한 3D도 뽑아 같은 단상에 섞는다. 배치 목록은 사용자가 시나리오를 지정한 뒤에 확정한다.

### 5.5 구현 순서 초안

1. 방호복만 가슴 존 → `UseApproved` 연결로 제스처를 검증한다
2. 헬멧, 장갑으로 존을 넓힌다
3. 장화는 발 추정이 가장 어색하므로 나중에 둔다
4. 하자·정품 배치와 다른 시나리오 3D 혼입은 제스처 검증과 함께, 또는 직후 씬 작성으로 적용한다. 다른 시나리오 목록은 사용자가 지정한 뒤에만 고른다.

## 6. 후속 세션에서 확정한 답

같은 날 후속 세션에서 사용자 확인으로 아래를 닫았다. 구현은 하지 않았다.

### 6.1 착용 순서

현재도 사실상 자유 순서다. 강제하는 것은 장화·장갑·테이프가 방호복을 입기 전에는 사용할 수 없다는 점뿐이다. 방호복, 헬멧, 마스크, 등지게 등은 서로 순서를 강제하지 않는다. 런타임 근거는 `PPEVoiceFlowDirector.m_EnforceHazmatBeforeGlovesAndBoots`다.

피봇 후에도 이 제약만 유지한다. 전체 PPE를 방호복부터 한 줄로 강제하지 않는다.

### 6.2 하자·정품·다른 시나리오 3D

교수자 피드백의 핵심은 폐기 교체가 아니다. 단상에 **하자품과 정품을 섞어 두고**, **다른 시나리오에 필요한 3D까지 뽑아 함께 배치**하라는 것이다. 학습자는 필요한 정상 PPE를 골라 바로 입는다.

폐기 동작은 두지 않는다. 쓰지 않을 물건은 내려놓으면 그만이다. 하자품을 몸에 대도 입히지 않고, 단상 오브젝트는 유지한다.

### 6.3 패널과 마스크 확인하기

UI식 패널은 입는 느낌이 적다는 피드백이 원인이다. 하자·정품·다른 시나리오 3D가 단상에 깔리면 패널 없이 한 번에 골라 입을 수 있다. 사용/폐기 패널은 착용 승인 경로에서 제거하는 쪽으로 확정한다.

마스크 확인하기 패널도 같은 이유면 유지하지 않는다. 확인이 필요하면 물건을 들고 보는 행동으로 대체한다. 별도 확인 버튼을 다시 넣을지는 구현 범위가 잡힌 뒤에만 검토한다.

### 6.4 착용 감각의 우선순위

리깅이나 오브젝트의 세밀한 애니메이션보다, 학습자가 **내가 입는 느낌**을 느끼는 것이 목표다.

- 손으로 집어 해당 부위에 갖다 대는 행동이 착용이다
- 기존 흡착·풀장착 표시는 그 행동의 결과로만 이어진다
- 새 본 리깅, 입는 클립, 패널 연출을 먼저 만들지 않는다

해석한 목표 흐름을 아래로 고친다.

```text
단상에 하자 PPE + 정상 PPE + 다른 시나리오 3D를 섞어 배치
필요한 정상품을 잡아 해당 신체 부위에 갖다 댐
→ UseApproved → 기존 흡착·풀장착 표시
쓰지 않을 물건은 내려놓으면 그만
장화·장갑·테이프만 방호복 착용 후에 사용 가능
```

### 6.5 시연 기기와 “움직이는 몸”

실사용·시연 기기는 Quest 2다. 상무님 피드백은 학습자가 입은 몸이 움직이는 것을 보고 싶다는 것이다.

Quest 2는 머리와 양손(컨트롤러 또는 핸드) 세 점만 추적한다. Quest 3의 카메라 상체 추적(IOBT)은 없다. 팔꿈치·어깨·허리·다리는 추정이다.

보이는 장소는 1인칭 가슴 메시가 아니라 기존 거울이 맞다. 거울에 Humanoid를 두고 머리·손 세 점으로 상체를 움직이면 “몸이 움직인다”는 요구는 충족할 수 있다. 다리·장화 자세까지 실제와 같다고 약속하지 않는다.

해석은 “풀장착 껍데기 안에 사람을 숨긴다”가 아니다. 현재 `PPE_A_SuitWear`는 입은 모습의 통짜(또는 고정 Pose) 모델이라, 그 안에 인체를 넣어도 방호복 팔다리가 구부러지지 않는다.

Quest 2에서 거울 몸이 움직이려면 인체 본이 뼈대가 되고, 기존 풀장착 자식(헬멧·마스크·장갑·장화 등)이 그 본에 붙어야 한다. 계층은 계속 `PPE Body Anchor → hazmat_suit_on` 아래다. 거울용으로 모델을 다른 위치에 복제하지 않는다.

같은 날 후속 확인: 사용자는 1인칭에서 몸이 많이 보일 필요는 없다고 했다가, 이어서 **거울에는 몸이 보이고 그 몸이 입고 있는 화면**을 원한다고 확정했다.

착용 연출 해석:

```text
단상 PPE를 잡아 몸 앞으로 가져간다
월드 오브젝트는 사라진다
옆의 플레이어 거울에, 보이는 인체(PPE_D_Player)가 그 PPE를 입은 상태로 나타난다
```

1인칭 카메라 앞에는 전신 메시를 두지 않는다. 손과 가져가는 물건만 보인다. 입은 결과는 옆 거울의 몸에서 본다.

거울에서는 몸이 보여야 한다. 빈 방호복이 떠 있는 화면이 아니라, 사람이 입고 있는 화면이다. 방호복이 몸통을 가리면 얼굴·목·손목만 피부가 보이면 된다. `PPE_D_Player` 몸과 통짜 `PPE_A_SuitWear`를 둘 다 두껍게 겹치면 메시가 뚫고 나오므로, 입은 부위는 몸 메시를 가리거나 끄고 PPE를 본에 붙인다.

하반신은 추적이 아니라 속임으로 가기로 했다. 걷기·서기 클립을 재생해 거울에서 다리가 움직이게 보이면 충분하다. 내 다리 자세와 일치한다고 말하지 않는다. 서 있을 때는 서기(idle), 실제로 걸을 때만 걷기 클립이어야 한다. 텔레포트만 할 때 제자리 걷기를 틀면 어색하다.

이 항목은 착용 제스처 피봇과 별개로 구현할 수 있으나, 연출 순서는 같다. 몸 앞에 대면 사라지고, 거울 속 몸에 입혀진다.

## 7. 적용한 변경

구현 상세·검증·후속은 `Docs/MeetingNotes/2026-08-20_PPE_Room_Train_Test_Followup.md`에 모았다. 아래는 08-19~08-20 작업의 요약이다.

시간 부족으로 거울 인체·PPE_D_Player·단상 이중 배치는 보류하고, 착용 승인만 바꿨다.

- 이번 변경이 대응하는 사용자 요청: 패널을 띄우지 말고, 잡은 PPE를 몸에 갖다 대면 붙게 한다
- 보존한 기존 동작: Grab, `UseApproved` 이후 흡착·풀장착 표시, 장화·장갑·테이프의 방호복 선행 검사, 하자품 Use 거부, 텔레포트, 거울 레이어, `hazmat_suit_on` 계층
- `approveUseByBodyProximity` 기본값 true. 패널 없이, 잡은 손을 착용 지점에 대고 트리거를 누르면 `UseApproved`. 장갑은 반대 손으로 잡아 끼울 손 모델에 대고 트리거. 같은 손으로 든 채 그 손에 붙는 것은 막는다
- 거리 판정은 PPE 루트가 아니라 잡는 Interactor 위치를 쓴다
- 걸린 방호복 색은 공유 재질을 직접 수정하지 않는다. 오염 벌은 기존 `HazmatSuit_Inspection`을 쓰고, 색·밝기는 인스턴스 `PPEHangSuitVisualAppearance`의 Inspector 값만 바꾼다
- 찢어진 방호복은 재질 파일을 다시 건드리지 않는다. 기본 Brightness 0.7에서 사용자가 직접 맞춘다
- `PPE_A_SuitHang_Material`과 `PPEHazmatContamination` 셰이더는 원본으로 되돌렸다. 장화 오염 경로를 깨지 않기 위함이다
- Unity에서 `Tools > PPE > Configure Hang Suit Color Controls`를 한 번 실행한 뒤, 각 방호복을 선택해 Color / Brightness를 조절하고 씬을 저장한다
- 손에 든 채 트리거만 눌러 입혀지던 것은 거리 0.5m가 팔 길이까지 포함했고, 숨긴 Use 버튼도 트리거에 반응했기 때문이다. 이제 부착 지점 0.25m 안에서만 입고, 몸 모델은 방 쪽에서 날아오지 않고 작성 Pose에 바로 붙는다
- 2026-08-20: 오염·하자 PPE를 몸에 대고 트리거해도 입지 않을 때, 기존 `Wrong Answer` SFX와 `contaminatedUseRejectedVoice`를 재생한다. 패널 UI는 띄우지 않는다. 멀리서 트리거하는 것은 이전과 같이 입히지 않고 거절 음성도 내지 않는다
- 착용 성공 음성은 패널 clip이 아니라 `PPEVoiceFlowDirector`의 방호복 Equipped / 다음 PPE 음성이다. Clean 복사본이 직렬화 구독 목록에 없어 재생되지 않았다. 이제 씬의 모든 `PPEActionPanelController`가 `UseApproved`를 구독한다
- 장화 오염은 마스크 균열처럼 켤 자식이 없다. `PPE_Defect_Visuals`는 비어 있고, 얼룩은 `PPE_boots_L/R_ChemicalContamination`의 `_ContaminationStrength`다. 이 값이 0으로 저장돼 있고 Clean과 Contam이 같은 재질을 써서 오염이 안 보였다. Contam 재질 강도를 1로 두고, Clean 장화는 원본 Unlit 재질로 분리했다
- 2026-08-20: `PPE_A_SCBA_Cylinder`, `PPE_B_MetalShelving`, `PPE_B_WoodenPlank`는 FBX 임베디드 Standard/Lit를 써서 URP Unlit 방에서 색이 깨졌다. URP Unlit 재질을 `Assets/Materials/PPE/Scene Unlit/SCBA|WoodenPlank|MetalShelving`에 두고 FBX `externalObjects`로 리맵했다. 언팩된 선반 30개 `tripo_part_*` MeshRenderer만 씬에서 Unlit을 직접 참조하게 바꿨다. Transform·메시·계층은 유지한다. 다시 적용하려면 `Tools > PPE > Convert SCBA Shelving Plank Materials to URP Unlit`
- 2026-08-20: `PPE_B_MetalShelving` 색·밝기는 공유 Unlit 재질을 수정하지 않는다. `PPEMetalShelvingVisualAppearance`의 Inspector Color / Brightness만 MaterialPropertyBlock으로 `tripo_part_*`에 적용한다. 자식 나무판은 대상에서 제외한다. Unity에서 `Tools > PPE > Configure Metal Shelving Color Controls`를 한 번 실행한 뒤 선반을 선택해 조절하고 씬을 저장한다
- 2026-08-20: 진열장 `PPE_A_Glove_L/R`과 핸드 `Chemical_Glove` 색은 공유 재질을 직접 수정하지 않는다. `PPEGloveVisualAppearance`의 Inspector Color / Brightness만 MaterialPropertyBlock으로 적용한다. Unity에서 `Tools > PPE > Configure Glove Color Controls`를 한 번 실행한 뒤 각 장갑을 선택해 조절하고 씬을 저장한다. `PPE_A_SuitWear` 몸 장갑·소매·테이프는 대상이 아니다
- 2026-08-20: `PPE_D_Player_Idle`은 FBX 임베디드 Standard를 써서 색이 없었다. URP Unlit `Assets/Materials/PPE/Scene Unlit/PlayerIdle/PPE_D_Player_Idle_Unlit.mat`에 `PPE_D_Player_Idle.jpg`를 연결하고 FBX `externalObjects`로 리맵했다. Transform·본은 바꾸지 않는다. Unity가 FBX를 재임포트하면 Prefab 인스턴스에 색이 붙는다

입력 경로: `손 → XRGrabInteractable → PPEInspectionState → PPEActionPanelController.Update 거리·유지 → UseApproved → 기존 흡착·풀장착`

## 8. 영향 범위

- 런타임: 사용/폐기 패널이 Grab 때 표시되지 않는다. 폐기 버튼 경로도 닫힌다
- 알려진 제한: `PPEHelmetDefectReplacement`는 폐기 승인 뒤에만 정상 헬멧을 켠다. 패널이 없으면 하자 헬멧을 내려놓아도 정상 헬멧이 나오지 않을 수 있다. 단상에 하자·정품을 따로 두면 이 교체 경로가 필요 없다
- 유지: `hazmat_suit_on`, 손 모델, 퀴즈·피날레, 텔레포트
- 아직 안 함: `PPE_D_Player` 본, 거울 옆 인체
- 2026-08-20: 사용자가 장화·마스크·헬멧을 Clean/Contam(헬멧은 Strap/NoStrap)으로 복사해 `PPE` 아래에 배치했다. 복사본이 Contaminated `initialCondition`을 물려받은 Clean 항목(`Boots_L/R_Clean`, `Mask_Clean`, `Helmet_Strap`)은 Clean(0)으로 고쳤다. `Helmet_NoStrap`과 `*_Contam`은 Contaminated로 유지한다. 이후 복사는 `Tools > PPE > Apply Named Variant Conditions`로 이름 규칙을 다시 적용할 수 있다

## 9. 완료한 검증

- 정적 확인: `PPEActionPanelController`, `PPEVoiceFlowDirector` 편집. SCBA/선반/나무판 Unlit 재질·FBX 리맵·선반 씬 MeshRenderer 30개 참조 확인. 임의 FileID는 추가하지 않음
- Unity Editor 확인: 아직 없음. Unity가 FBX를 재임포트한 뒤 SCBA 실린더·나무판 2개·금속 선반 색을 Scene/Game View에서 확인해야 한다
- Quest/OpenXR 확인: 아직 없음

## 10. 후속 세션에서 할 일

1. Play Mode에서 Clean 장화·마스크·헬멧_Strap은 몸 앞에서 트리거로 입고, Contam·Helmet_NoStrap은 잡을 수만 있는지 확인한다
2. Clean 복사본에 오염 자식이 보이면 해당 오브젝트의 `PPE_Defect_Visuals` / `Mask_Crack_Visual`이 Play Mode에서 꺼져 있는지 Inspector에서 확인한다
3. `PPE_D_Player`를 거울 옆 인체에 연결하는 작업은 별 패치로 둔다
4. Unity 임포트가 끝난 뒤 `PPE_A_SCBA_Cylinder`, `PPE_B_WoodenPlank`, `PPE_B_MetalShelving` 색이 핑크/검정이 아닌지 Editor에서 확인한다. 남아 있으면 `Tools > PPE > Convert SCBA Shelving Plank Materials to URP Unlit`를 한 번 실행한다
5. `Tools > PPE > Configure Metal Shelving Color Controls` 실행 후 `PPE_B_MetalShelving` Inspector에서 Color / Brightness가 선반만 바꾸는지, 나무판은 그대로인지 확인한다

대상 씬은 빌드 씬 `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`다. 지정하지 않은 variant 씬은 열거나 수정하지 않았다.
