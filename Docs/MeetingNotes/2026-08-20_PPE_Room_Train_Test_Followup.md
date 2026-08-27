# 2026-08-20 PPE Train/Test 착용 피봇·룸 소품·인체 idle 후속 회의록

- 날짜: 2026-08-20
- 대상 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`
- 관련 기획: `Docs/MeetingNotes/2026-08-19_MainProject_Pivot_Extension_Ideas.md`
- 음성·거울 흐름: `Docs/PPE_Room_Voice_Narration_Flow_Design.md`
- 지정하지 않은 variant 씬은 열거나 수정하지 않았다
- 이른 세션의 색·idle 배치는 사용자가 확인하고 씬을 저장했다. 이후 음성·거울·이름 예외는 정적 반영만 했고 Unity 저장·Play Mode·Quest는 아직이다

## 오늘 작업 요약

하루 작업은 착용 피봇(패널 없이 몸에 대고 입기)에서 시작해, 진열 PPE·핸드·모달·FS-16/19 예외까지 이어졌다. 상세는 아래 절 번호와 같다.

| 구분 | 내용 | 절 |
| --- | --- | --- |
| 착용 피봇 | 사용/폐기 패널 없이 몸+트리거. Clean만 입고 Contam은 거절. 장화는 가슴–하반신 구간 | 1–3, 13 |
| 단상·시각 | 하자/정품 따로 배치. 장화 오염, 선반·장갑 Inspector 색, SCBA·나무판 Unlit, idle 인체 | 4–9, 14 |
| 머리 장비 | 페이스실드·고글 Unlit와 착용 연결. 유리 파츠는 `PPE_Mask_Glass` | 10, 12 |
| 니트릴 | 방호복과 순서 자유. 맨손은 `InnerGlove`, 방호복 후는 `InnerGloveSuit`. 바깥 장갑은 니트릴 다음, 테이프는 바깥 장갑만 | 15, 17, 19 |
| 카드·모달 | 카드 다음 학습/훈련/테스트. CARD_001/002. 작업계획 선택(밀폐공간/누출, 테스트는 랜덤 포함) | 16, 18 |
| 작업계획 PPE | 모달 밀폐공간/누출에 따라 착용 세트만 승인. 진열은 숨기지 않음. 안전대와 SCBA는 같은 세트에 없음 | 23 |
| 왼손 | `Left_NearFarInteractor`의 Interaction Manager를 씬의 XR Interaction Manager에 연결 | 23 |
| 교육 체크리스트 | 태블릿 서명 완료 후 밀폐/누출 HUD 패널. 착용 성공 SFX와 같은 시점에 Check 활성화 | 24 |
| 음성 | 하자 PPE 착용 거절은 Wrong Answer 뒤 `EDU_202 WrongUse`. EDU_203·TRAIN_003은 유지 | 20 |
| FS-16 | 거울 5초는 1.2m+시선일 때만 진행. 시선 이탈은 게이지 정지, 반경 이탈은 측정 취소. 완료 후 그 자리 재측정 없음 | 20 |
| FS-19 | 이름 빈 값·공백만이면 Enter 제출 차단. 한글 입력과 값이 있는 제출은 유지 | 21 |
| 로코모션 시험 | 복사 씬에서 EDU_001 뒤 Teleport_0 자동 이동 후 스틱 Move. idle 하반신 walk | 25, 26 |
| 고글 Contam 렌즈 | `Glass_Lens`만 투명 유리+황갈색 오염 자국. Clean 유리는 유지 | 27 |
| Horizon 상세 | 밀폐공간 진입 전 PPE / 화학물질 누출 PPE 두 시나리오를 넣은 스토어 문구 | 28 |
| 노출 PPE | 누출 착용 세트에 안전모. 진열은 숨기지 않음. 태블릿은 산성 세정제 누출 계획서 | 29, 30 |

아직 넣지 않은 것: 입은 부위로 몸 메시 가리기. 입은 PPE를 본에 스키닝하는 작업은 하지 않았다. 로코모션 시험과 idle walk는 복사 씬에서만. 시나리오 카드·모달 문구를 Horizon 상세와 맞추는 작업, Play Mode·Quest 확인은 남는다.

## 1. 이번 세션에서 대응하는 요청

1. 사용/폐기 패널 없이, 잡은 PPE를 몸에 대고 트리거로 입는다. Clean만 착용되고, 오염·하자품은 잡을 수 있으나 착용은 거절한다.
2. 장화·마스크·헬멧을 Clean/Contam(헬멧은 Strap/NoStrap)으로 단상에 따로 둔다. 폐기 후 정상품이 나타나는 교체는 쓰지 않는다.
3. 착용 실패 때 기존 Wrong Answer SFX와 거절 음성을 낸다. 착용 성공 음성도 나와야 한다.
4. 장화 오염 얼룩이 보이게 한다. 마스크·헬멧 하자 자식은 사용자가 켠 상태를 유지한다.
5. `PPE_A_SCBA`, `PPE_B_MetalShelving`, `PPE_B_WoodenPlank` 색이 나오게 한다.
6. 금속 선반 색·밝기를 Inspector에서 조절할 수 있게 한다.
7. 플레이어 모델이 걷기 자세로 들어온 문제를 고치고, idle을 `PPE_A_SuitWear`에 넣는다. idle에 색을 연결한다.

보존한 기존 동작: Grab, `UseApproved` 이후 흡착·풀장착, 장화·장갑·테이프의 방호복 선행 검사, 텔레포트, 거울 레이어, `hazmat_suit_on` 계층, 공유 방호복 재질 파일.

## 2. 착용 승인

### 적용한 변경

- `PPEActionPanelController.approveUseByBodyProximity`를 켠다. Grab 때 사용/폐기 패널은 띄우지 않는다.
- 착용 판정은 PPE 루트가 아니라 잡는 Interactor 위치다. 거리 0.25, 전방 0.18. 멀리서 트리거만 누르면 입지 않는다.
- 장갑은 반대 손으로 잡아 끼울 손 모델에 대고 트리거한다. 같은 손으로 든 채 그 손에 붙는 것은 막는다.
- 이름 규칙: `*_Clean`, `PPE_A_Helmet_Strap`은 Clean. `*_Contam`, `*_Ripped`, `PPE_A_Helmet_NoStrap`은 Contaminated. 메뉴 `Tools > PPE > Apply Named Variant Conditions`.
- 복사본이 Contaminated를 물려받은 Clean 항목(`Boots_L/R_Clean`, `Mask_Clean`, `Helmet_Strap`)은 Clean(0)으로 고쳤다.

입력 경로: `손 → XRGrabInteractable → PPEInspectionState → PPEActionPanelController 거리·트리거 → UseApproved → 기존 흡착·풀장착`

### 근본 원인

손에 든 채 트리거만으로 입혀지던 것은 거리 0.5m가 팔 길이까지 포함했고, 숨긴 Use 버튼도 트리거에 반응했기 때문이다.

## 3. 착용 실패·성공 음성

### 적용한 변경

- 범위 안에서 Contaminated에 트리거하면 `UseRejectedContaminated`. 패널 UI는 띄우지 않는다. SFX `Wrong Answer`와 지연된 `contaminatedUseRejectedVoice`.
- 이 트리거는 음성 스킵에 쓰이지 않게 예약한다. 멀리서 트리거하면 착용도 거절 음성도 없다.
- 성공 음성은 패널 clip이 아니라 `PPEVoiceFlowDirector`의 방호복 Equipped / 다음 PPE 음성이다. 디렉터가 씬의 모든 `PPEActionPanelController`를 모아 구독한다.

### 근본 원인

직렬화 구독 목록이 Contam/Ripped/NoStrap 복사본만 가리켜, Clean 착용 때 Equipped 음성이 나가지 않았다.

## 4. 방호복 걸이 색과 장화 오염

### 적용한 변경

- 걸린 방호복 색은 `PPE_A_SuitHang_Material`을 직접 수정하지 않는다. 인스턴스 `PPEHangSuitVisualAppearance`의 Color / Brightness만 쓴다. 메뉴 `Tools > PPE > Configure Hang Suit Color Controls`.
- 장화 오염은 켤 자식이 없다. 얼룩은 `PPE_boots_L/R_ChemicalContamination`의 `_ContaminationStrength`다.
- Contam 재질 강도를 1로 두고, Clean 장화는 원본 Unlit로 분리했다.

### 근본 원인

Clean과 Contam이 같은 오염 재질을 쓰고 강도가 0으로 저장돼 있어, Edit Mode에서 얼룩이 보이지 않았다.

## 5. 룸 소품 Unlit 색

### 적용한 변경

FBX 임베디드 Standard/Lit를 URP Unlit로 리맵했다. 텍스처는 기존 basecolor를 쓴다.

| 오브젝트 | 처리 |
| --- | --- |
| `PPE_A_SCBA_Cylinder` | Prefab 인스턴스. FBX `externalObjects` → `Scene Unlit/SCBA` |
| `PPE_B_WoodenPlank` (2개) | Prefab 인스턴스. FBX 리맵 → `Scene Unlit/WoodenPlank` |
| `PPE_B_MetalShelving` | 언팩된 `tripo_part_0`~`29`. 씬 MeshRenderer가 Unlit을 직접 참조. 메시·Transform 유지 |
| `PPE_D_Player_Idle` | Prefab 인스턴스. FBX 리맵 → `Scene Unlit/PlayerIdle/PPE_D_Player_Idle_Unlit.mat` + `PPE_D_Player_Idle.jpg` |

메뉴: `Tools > PPE > Convert SCBA Shelving Plank Materials to URP Unlit`

### 근본 원인

URP Unlit 방에서 Built-in Standard 임베디드 재질을 쓰면 핑크·검정·색 없음이 된다. 애니메이터가 없어도 걷기 자세가 나온 것은 `PPE_D_Player.fbx` 본 기본값이 `preset:biped:walk`이기 때문이다. idle FBX에는 걷기 클립이 없고 서 있는 자세만 있다.

## 6. 금속 선반 색·밝기

### 적용한 변경

- `PPEMetalShelvingVisualAppearance`를 선반 루트에 둔다. Inspector Color / Brightness를 MaterialPropertyBlock으로 `tripo_part_*`에만 적용한다.
- 공유 Unlit 재질, 메시, Transform, 자식 나무판은 바꾸지 않는다.
- 메뉴: `Tools > PPE > Configure Metal Shelving Color Controls`. 씬 YAML에 임의 FileID를 넣지 않았다. Unity에서 메뉴를 한 번 실행한 뒤 선반을 골라 조절하고 저장한다.

## 7. 인체 모델 배치 결정

- `PPE_A_SuitWear` 안에는 사람 메시를 **하나만** 둔다. 지금 기준은 idle(`PPE_D_Player_Idle`).
- 걷기 `PPE_D_Player` 메시를 idle 옆에 또 두지 않는다. walk는 나중에 같은 리그의 클립으로 붙인다.
- 1인칭 카메라 앞에는 전신을 두지 않는다. SuitWear 안 인체는 거울에서 입힌 모습용이다.
- 하반신 idle/walk 전환 애니메이터는 아직 하지 않았다.

## 8. 영향 범위

- 런타임: Grab 때 사용/폐기 패널이 없다. 폐기 버튼 경로도 닫힌다.
- `PPEHelmetDefectReplacement`는 폐기 승인 뒤에만 정상 헬멧을 켠다. 패널이 없으면 이 교체 경로는 필요 없다. 단상에 하자·정품을 따로 둔 배치와 맞다.
- Grab HowTo 음성은 예전 직렬화 슬롯만 구독한다. 이번 요청 범위가 아니라 바꾸지 않았다.
- 유지: `hazmat_suit_on`, 손 모델, 퀴즈·피날레, 텔레포트.

## 9. 완료한 검증

- 정적 확인: 착용 근접 코드, 음성 구독, 장화 오염 재질 분리, Unlit 재질·FBX 리맵, 선반 30개 MeshRenderer 참조, idle Unlit 텍스처 연결. 임의 FileID 추가 없음.
- Unity Editor 확인: 사용자가 색·idle 배치를 확인하고 씬을 저장했다. Play Mode 착용 루프는 이 세션에서 로그로 재현하지 않았다.
- Quest/OpenXR 확인: 아직 없음. 양안·헤드셋 트리거·XR 오디오는 정상이라고 말하지 않는다.

## 10. 페이스실드·고글 프레임 Unlit

### 적용한 변경

FBX 임베디드 Standard/Lit를 프레임만 URP Unlit로 리맵한다. 유리 파츠는 기존 `PPE_Mask_Glass` 투명 재질을 유지한다.

| 오브젝트 | 처리 |
| --- | --- |
| `PPE_A_FaceShield_Head` | 바이저 메시. `PPE_Mask_Glass` |
| `PPE_A_FaceShield_Glass` | 프레임 메시(part_24). `PPE_A_FaceShield_part_24_Unlit.mat` |

2026-08-20 수정: 처음에는 프레임 텍스처를 Head에 넣어 UV가 깨진 노이즈가 났다. Head는 part_14 바이저, Glass 이름의 자식이 part_24 프레임이다.

사용자가 고글을 다시 넣고 씬을 저장한 뒤, `tripo_part_1/2/6/8/13/15`만 각 part Unlit로 바꿨다. `Glass_Lens`는 `PPE_Mask_Glass`를 유지한다.

메뉴: `Tools > PPE > Convert FaceShield Goggle Materials to URP Unlit`

### 근본 원인

URP Unlit 방에서 Built-in Standard 임베디드 재질은 색이 빠진다. 페이스실드 헤드는 유리 투명 재질을 쓰고 있어 프레임 텍스처가 보이지 않았다.

## 11. 후속 항목

1. Play Mode에서 Clean은 몸 앞 트리거로 입고, Contam·Helmet_NoStrap은 잡을 수만 있는지 확인한다.
2. Clean 착용 때 Equipped/다음 PPE 음성, Contam 근접 트리거 때 Wrong Answer + 거절 음성을 확인한다.
3. Contam 장화 얼룩과 Clean 장화 Unlit가 Play Mode에서 구분되는지 확인한다.
4. 선반 Color / Brightness 메뉴가 씬에 붙어 있는지, 나무판이 따라 변하지 않는지 확인한다.
5. idle 인체와 통짜 `PPE_A_SuitWear`가 겹쳐 메시가 뚫고 나오지 않는지 거울에서 본다. 입은 부위는 몸 메시를 가리거나 끄는 작업은 아직이다.
6. walk 클립이 생기면 같은 리그에 idle 기본 + 실제 이동 때만 걷기를 붙인다. 텔레포트만 할 때 제자리 걷기를 틀지 않는다.
7. Quest에서 트리거 착용, Unlit 양안, 선반·idle 색을 확인한다.
8. `PPE_A_FaceShield_Head` 프레임 색과 `PPE_A_FaceShield_Glass` 투명을 확인한다. 고글을 씬에 둔 뒤 렌즈만 유리이고 프레임에 색이 있는지 확인한다.

## 12. 페이스실드·고글 착용 연결

### 이번 변경이 대응하는 요청

진열된 고글 정상/하자, 페이스실드 정상/하자를 다른 PPE와 같은 경로로 입을 수 있게 한다. 풀장착 모델과 진열 위치는 사용자가 이미 붙인 값을 유지한다.

보존한 기존 동작: 기존 PPE Grab, 몸+트리거 착용, Clean만 `UseApproved`, Contam 거절, 헬멧/마스크 머리 부착, 방호복 선행 검사, 텔레포트, 거울 레이어, 진열·풀장착 Transform.

### 적용한 변경

- `PPEItemType` 끝에 `FaceShield`, `SafetyGoggles`를 추가했다. 기존 `itemType` 정수는 그대로다.
- 머리 착용 판정과 Mirror Only 레이어에 두 종류를 넣었다. 1인칭에서 얼굴 장비가 시야를 가리지 않게 헬멧/마스크와 같다.
- 씬 YAML에 FileID를 만들지 않는다. 메뉴 `Tools > PPE > Wire FaceShield And Goggle Wear`가 Unity FileID로 연결한다.
- 진열 루트 `PPE_A_FaceShield_Clean/Contam`, `PPE_A_Goggle_Clean/Contam`에 Grab·Identity·Binding·Inspection·ActionPanel을 붙인다. 마커가 `(1)`/`(2)`로 복제된 이름은 `XR Item Marker_small`로 맞춘다.
- 풀장착 자식 `PPE_A_SuitWear/PPE_A_FaceShield`, `PPE_A_SuitWear/PPE_A_Goggle`은 시각 전용으로 두고, Clean 바인딩만 `PPEEquipmentVisualController` 슬롯에 넣는다. Contam은 같은 종류라 착용 거절만 하고 슬롯을 하나 더 만들지 않는다.
- 진열·풀장착 Transform은 읽기만 하고 쓰지 않는다.

입력 경로: `손 → XR Item Marker_small → XRGrabInteractable → PPEInspectionState → PPEActionPanelController 머리 앞 거리·트리거 → UseApproved → 풀장착 자식 표시`

### 근본 원인

진열 오브젝트는 메시와 마커만 있고 Identity/Grab/슬롯이 없어, 잡아도 다른 PPE처럼 몸에 붙지 않았다.

### 검증

- 정적 확인: 타입 추가, 머리 부착·Mirror Only 분기, 에디터 메뉴. 임의 FileID 없음.
- Unity Editor 확인: 메뉴를 실행하고 씬을 저장해야 컴포넌트가 붙는다. 이 세션에서 Play Mode·헤드셋 재현은 없다.
- Quest/OpenXR 확인: 아직 없음.

### 후속

1. Unity에서 대상 씬을 연 뒤 `Tools > PPE > Wire FaceShield And Goggle Wear`를 실행하고 저장한다.
2. Play Mode에서 Clean 페이스실드·고글을 잡아 머리 앞에 대고 트리거하면 풀장착 자식이 켜지는지 확인한다. Contam은 잡혀도 입히지 않는지 확인한다.
3. 피날레는 작성된 슬롯을 모두 기다리므로, 정상 페이스실드·고글도 입어야 완료로 넘어간다.
4. Quest 양안에서 착용 뒤 1인칭 시야와 거울 반영을 확인한다.

## 13. 장화 착용·거절 SFX

### 근본 원인

페이스실드·고글 착용 연결이 `PPE_A_Boots_L_Clean` 액션 패널 UI를 그대로 복사해 버튼·피드백·sharedPresentation을 장화와 공유했다. 몸+트리거 착용은 정적 `activePanelOwner`가 있어야 해서, 다른 패널이 소유권을 가져가면 장화는 트리거해도 `UseApproved`/`UseRejectedContaminated`가 나가지 않고 Wrong Answer SFX도 없다.

방호복을 입기 전 장화 부착 지점은 가슴(머리에서 0.35m)이라, 발 쪽에 대고 눌러도 0.25m 범위에 안 들어 거절 SFX가 나지 않았다.

### 적용한 변경

- 몸+트리거 착용은 패널 소유권 없이, 잡고 있는 그 PPE만 거리·트리거로 판정한다.
- 다른 PPE 자식이 아닌 UI는 OnEnable/피드백에서 건드리지 않는다.
- 장화는 가슴과 하반신 모두에서 판정한다. 가슴 지점(`bodyAttachChestDropMeters`)과 하반신 지점(`bodyAttachBootDropMeters`, 방호복 착용 후에는 Body Anchor)을 잇는 구간에서 `bodyAttachDistance` 이내면 통과한다. 장갑·헬멧·마스크는 기존 단일 지점이다.

보존: Clean만 착용, Contam 거절, 장화·장갑의 방호복 선행 검사, 장화 Transform.

### 검증

- 정적 확인: `CanResolveChoice`, 장화 가슴–하반신 구간 판정, UI 소유 가드.
- Unity Editor / Quest 확인: 아직 없음. Play Mode에서 하자 장화를 가슴 또는 하반신에 대고 트리거하면 Wrong Answer, 방호복 후 정상 장화는 가슴·하반신 모두에서 입히는지 확인한다.

## 14. 진열장·핸드 장갑 Inspector 색

### 적용한 변경

- `PPEGloveVisualAppearance`를 진열장 `PPE/PPE_A_Glove_L/R`과 핸드 `PPE_A_Hand_GloveSuit_*`, `PPE_A_Hand_GloveTape_*`의 `Chemical_Glove`에 둔다.
- Inspector Color / Brightness만 MaterialPropertyBlock으로 적용한다. FBX 텍스처, Unlit 재질 파일, 메시, Transform은 바꾸지 않는다.
- 진열장과 핸드 모델은 서로 다른 컴포넌트라 색을 따로 맞출 수 있다. 왼·오른, GloveSuit·GloveTape도 인스턴스마다 따로다.
- 소매·테이프 메시와 `PPE_A_SuitWear` 몸 장갑은 대상이 아니다.
- 씬 YAML에 FileID를 만들지 않는다. 메뉴 `Tools > PPE > Configure Glove Color Controls`가 Unity FileID로 연결한다. 다시 실행해도 이미 작성한 Color / Brightness는 덮어쓰지 않는다.

보존: Grab, 착용 판정, 공유 장갑 재질 파일, 진열장·핸드 Transform.

### 검증

- 정적 확인: 컴포넌트·에디터 메뉴. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 대상 씬을 연 뒤 메뉴를 실행하고, 진열장 장갑과 `Chemical_Glove` 핸드 색이 서로 독립인지 확인한 다음 씬을 저장한다.

## 15. 니트릴 내부 장갑

### 적용한 변경

- `PPE_A_InnerGlove_L/R`를 바깥 화학장갑(`RubberGlove`)과 분리해 `NitrileInnerGloveLeft/Right`로 둔다. enum 값은 맨 뒤에만 추가했다.
- 표시 이름은 `니트릴 내부 장갑(좌/우)`다.
- 착용 시 `PPE_A_Hand_InnerGloveSuit_L/R`로 손이 바뀐다. 바깥 장갑을 입으면 내부 장갑 손 모델은 숨긴다.
- 방호복 선행 검사는 당시 바깥 장갑과 같게 두었다. 2026-08-20 후속(17절)에서 니트릴은 방호복 전과도 입을 수 있게 바꿨다. 테이프 대상은 바깥 장갑·장화만 유지한다.
- 씬 YAML에 FileID를 만들지 않는다. 메뉴 `Tools > PPE > Wire Nitrile Inner Glove Wear`가 Unity FileID로 연결한다. `PPE_A_Hand_InnerGloveSuit_L` 이름 끝 공백은 이 메뉴가 정리한다.

보존: 진열장·핸드 Transform, 바깥 장갑 슬롯, 색 Inspector 작성값.

### 검증

- 정적 확인: 타입 추가, 손 전환에 innerGloveHand 포함, 에디터 메뉴. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 대상 씬에서 메뉴를 실행한 뒤, 내부 장갑을 반대 손에 대고 입으면 InnerGloveSuit가 켜지고, 바깥 장갑을 입으면 Chemical Glove Suit로 바뀌는지 확인한다.

## 16. 카드 직행·이름 표시·CARD_001/002

### 이번 변경이 대응하는 요청

- 불완전 PPE vs 다음 시나리오 선택을 건너뛰고, 카드 다음에 학습/훈련/테스트 모달을 연다.
- 잡은 PPE의 이름만 약 2초 보여 준다. 사용/폐기 패널은 그대로 숨긴다.
- 교체한 `CARD_001`, `CARD_002`를 빠진 단계에 연결한다.

보존한 기존 동작: 몸에 대고 트리거로 입히기, 학습/훈련/테스트 버튼과 그 이후 Education/Training/Test 음성, Grab·텔레포트·거울.

### 적용한 변경

- `ScenarioDetailModal.openWithPpeModeChoices`를 대상 씬 모달에 켠다. 카드 Trigger는 바로 `ppeModeChoiceRoot`를 연다. 모드 뒤로가기는 카드 선택으로 돌아간다.
- `PPEVoiceFlowDirector`는 카드 선택 시 `CardIntro`에서 `PpeEducationSelected`로 간다. `ModalDetail`의 기존 `CARD_002`+`MODAL_001` 단계는 이 직행 경로에서 재생하지 않는다.
- `card_intro`는 기존처럼 `3_VO_PPE_CARD_001_CheckCard.mp3`다. `ppe_education_selected`의 첫 클립을 `3_VO_PPE_CARD_002_CardSelected.mp3`로 바꿨다. 불완전 PPE 선택 안내(`MODAL_003`)와 이전 모드 안내(`MODAL_002`)는 이 단계에서 빼 두었다. 파일 GUID는 기존 `.meta`를 유지한다.
- 몸 근접 착용 PPE는 Grab 때 이름 라벨만 켠다. `grabNameVisibleSeconds` 기본 2초 뒤 끄고, 그전에 놓으면 바로 끈다. 버튼·피드백 RectTransform은 건드리지 않는다.
- 이름만 보일 때는 `PPEActionPanelSharedPresentation.nameOnlyLayout`(320×114, 이름 중앙)을 쓴다. 2/3버튼 높이 310/420과 Action Panel Pose Transform은 그대로다.

### 근본 원인

- 카드 선택 음성 `CARD_002`가 `card_detail`(`ModalDetail`)에만 연결되어 있었다. 모드 모달로 직행하면 그 단계가 건너뛰어져 `CARD_002`가 나가지 않는다.
- `openWithTrainingChoices`는 불완전 PPE vs 다음 시나리오 UI를 연다. 학습/훈련/테스트와는 다른 단계다.

### 영향 범위

- 대상 씬 `Scenario Card Canvas`의 `ScenarioDetailModal`과 `PPE Voice Flow` 클립 배열.
- 몸 근접 착용 패널의 이름 표시. 착용 판정·트리거 예약은 그대로다.

### 완료한 검증

- 정적 확인: `CARD_001` GUID `da572eeb74cddfc41bd5993ac9e0173b`는 `card_intro`, `CARD_002` GUID `eb4e48e461a26b04a92a7a13abece246`는 `ppe_education_selected`. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. Play Mode에서 카드 선택 후 `CARD_002`와 학습/훈련/테스트 모달이 함께 나오는지, Grab 이름 2초 후 소멸을 확인해야 한다.

## 17. 니트릴 내부 장갑 순서·핸드 모델

### 이번 변경이 대응하는 요청

- 니트릴을 입으면 `PPE_A_Hand_InnerGloveSuit`로 손이 바뀌어야 한다.
- 니트릴은 방호복 전에도 입을 수 있다. 방호복과 니트릴 순서는 자유롭다.
- 바깥 화학장갑은 같은 손의 니트릴을 입은 뒤에만 입을 수 있다.
- 테이프는 니트릴 위에는 붙지 않고, 바깥 장갑 다음에만 손 테이핑이 된다.

보존한 기존 동작: 바깥 장갑·장화의 방호복 선행 검사, 장화 테이프, 몸에 대고 트리거로 입히기, 바깥 장갑 핸드 `GloveSuit`/`GloveTape`.

### 적용한 변경

- `PPE_A_InnerGlove_L/R` 타입을 `NitrileInnerGloveLeft/Right`로 바꾸고 이름을 `니트릴 내부 장갑(좌/우)`로 둔다. 표시·핸드 Transform은 그대로다.
- 슬롯을 기존 `InnerGloveSuit` 핸드에 연결하고, `handModelSwaps`에 니트릴 항목과 `innerGloveHand`를 넣었다. 왼쪽 핸드 이름 끝 공백을 제거했다. 새 FileID는 만들지 않았다.
- `CanApprovePpeUse`에서 니트릴은 방호복 선행 검사에서 뺀다. 바깥 장갑은 같은 손 니트릴이 `IsItemUsed`일 때만 승인한다. 테이프 대상은 계속 바깥 장갑·장화만이다.
- 방호복을 니트릴 다음에 입으면 `BareSuit`가 니트릴 손을 덮지 않도록, 방호복 착용 완료 후 `RefreshWornHandModels`로 현재 장갑 층을 다시 맞춘다.

### 근본 원인

- 니트릴 진열품이 아직 `RubberGlove` 타입이라 바깥 장갑 슬롯·`GloveSuit` 손을 타고 있었다. `Wire Nitrile Inner Glove Wear` 메뉴는 실행되지 않은 상태였다.
- 방호복 선행 검사가 니트릴까지 `IsGloveOrBootPanel`로 묶고 있었다.

### 영향 범위

- 대상 씬 InnerGlove 타입·이름·슬롯·핸드 스왑.
- 바깥 장갑 착용 승인, 방호복 후 핸드 갱신.
- 장화 테이프와 방호복→장화/바깥 장갑 선행 검사는 그대로다.

### 완료한 검증

- 정적 확인: L=13/`InnerGloveSuit_L`, R=12/`InnerGloveSuit_R`, 테이프 required는 4/5만. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 방호복 전 니트릴 → InnerGlove, 방호복 후 니트릴 → InnerGloveSuit, 그 다음 바깥 장갑 → GloveSuit, 니트릴만 있는 손에는 테이프가 안 붙는지 확인해야 한다.

## 18. 모드 다음 작업계획 선택

### 이번 변경이 대응하는 요청

- 카드 → 학습/훈련/테스트는 그대로 둔다.
- 교육·훈련 다음에는 같은 모달에 밀폐공간 작업전 피피이 착용, 누출사고 대응 피피이 착용을 띄운다.
- 테스트 다음에는 밀폐공간, 누출 사고 대응 피피이, 랜덤 테스트 세 개를 띄운다.

보존한 기존 동작: 학습/훈련/테스트 버튼 라벨, 모드 선택 이후 Education/Training/Test 음성·텔레포트. 작업계획에 따른 PPE 진열 숨김은 넣지 않는다. 착용 세트 분기는 23절.

### 적용한 변경

- 모드 클릭은 모달을 닫지 않는다. 작업계획을 고른 뒤에만 `PpeLearningModeSelected`가 나간다.
- 교육·훈련은 기존 `1_EduChoice` 두 버튼이다. 라벨은 씬에 적어 두었다. 현재 씬 문구는 `밀폐공간 대응 PPE착용`, `누출사고 대응 PPE착용`이다. 설명은 `1_ senario`에 밀폐공간/누출사고 시나리오를 함께 적어 두었다.
- 학습/훈련/테스트 화면에서는 그 설명을 끄고, 작업계획 화면에서만 다시 켠다. 런타임은 문구를 덮지 않는다.
- 테스트 세 버튼은 `2_Mode`를 `3_WorkPlan`으로 복제한다. Unity FileID를 YAML에 만들지 않기 위해 `Tools > PPE > Wire Work Plan Choices`가 복제·연결한다. 메뉴 전에는 테스트 클릭이 오류로 멈춘다.
- 작업계획 뒤로가기는 학습/훈련/테스트로 돌아간다. 랜덤은 클릭 시 밀폐공간 또는 누출 중 하나로 정해진다.
- `PPEVoiceFlowDirector.ActiveWorkPlan`에 복사한다. 착용 세트 분기는 23절. 태블릿 작업계획서 문서 분기는 다음 작업이다.

### 근본 원인

- 모드 클릭이 바로 모달을 닫고 학습/훈련/테스트를 시작해, 그 다음 화면을 넣을 자리가 없었다.
- 테스트의 세 번째 버튼은 씬에 없어, 임의 FileID 없이 에디터 복제가 필요하다.

### 영향 범위

- 대상 씬 `ScenarioDetailModal`의 `1_EduChoice` 라벨과 테스트용 `3_WorkPlan` 연결.
- 모드 선택 시점. Grab·텔레포트·거울·퀴즈는 그대로다.

### 완료한 검증

- 정적 확인: 교육/훈련 라벨은 `1_EduChoice` TMP. `2_Mode`의 교육 모드/훈련 모드/테스트 모드 문구는 코드가 덮지 않는다. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. Unity에서 `Tools > PPE > Wire Work Plan Choices`를 실행한 뒤, 교육·훈련에서 두 버튼, 테스트에서 세 버튼이 나오는지 확인해야 한다.

## 19. 맨손 니트릴 핸드 `PPE_A_Hand_InnerGlove`

### 이번 변경이 대응하는 요청

- 맨손에서 니트릴만 입으면 `PPE_A_Hand_InnerGlove_L/R`로 바꾼다.
- 방호복을 입은 뒤의 니트릴은 기존 `InnerGloveSuit`를 쓴다.

보존한 기존 동작: 바깥 장갑 `GloveSuit`, 테이프 `GloveTape`, 방호복 후 핸드 갱신, 표시 Transform.

### 적용한 변경

- `PPEHandModelSwap.bareInnerGloveHand`에 씬에 있는 `PPE_A_Hand_InnerGlove_L`(`1292914635`) / `_R`(`1729772667`)을 연결한다. 새 FileID는 만들지 않았다.
- 니트릴 착용 시 방호복이 없으면 InnerGlove, 있으면 InnerGloveSuit. 방호복 착용 완료 시 이미 있는 `RefreshWornHandModels`가 InnerGloveSuit로 맞춘다.
- 왼쪽 InnerGlove의 `Chemical_Glove` SkinnedMeshRenderer를 색 대상으로 넣었다. Transform은 그대로다.

### 근본 원인

- 니트릴 핸드가 InnerGloveSuit만 가리켜, 맨손 상태에서 슈트 위 니트릴 손이 나왔다.

### 영향 범위

- 대상 씬 handModelSwaps와 InnerGlove 색 대상.
- 핸드 전환 선택. 착용 순서 승인·테이프 규칙은 그대로다.

### 완료한 검증

- 정적 확인: 니트릴/바깥/테이프 스왑 6곳에 bareInnerGloveHand가 L/R InnerGlove다. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 맨손→니트릴이 InnerGlove인지, 그 다음 방호복이 InnerGloveSuit인지 확인해야 한다.

## 20. 하자 PPE 착용 거절 음성 `EDU_202`와 거울 재측정

### 이번 변경이 대응하는 요청

- 하자 PPE를 잡고 몸에 대어 입으려 하면 Wrong Answer SFX 뒤에 `4_VO_PPE_EDU_202_WrongPPE_WrongUse`가 나와야 한다. 기존 `WrongButton` 클립은 쓰지 않는다.
- 거울 5초 측정은 한 번 끝나면 그 자리에서 반복하지 않는다. 반경을 나갔다가 돌아와야 다시 측정한다.
- FS-16 예외: 5초 진행 중 1.2m를 벗어나거나 거울에서 시선을 떼면 게이지를 중단한다.

보존한 기존 동작: 몸 근접 착용 거절 경로(`UseRejectedContaminated`), SFX 후 0.46초 음성 지연, 미완료 거울 재검사, 퀴즈·텔레포트·완료 복귀.

### 적용한 변경

- 대상 씬의 `contaminatedUseRejectedVoice`와 AudioManager Voice 항목을 `4_VO_PPE_EDU_202_WrongPPE_WrongUse` GUID로 바꿨다. 패널 코드의 SFX→지연→음성 순서는 그대로다.
- `PPEFinaleController`는 측정이 끝나면 관찰 반경을 벗어날 때까지 다음 측정을 무장하지 않는다. 위치 도착과 거울 마커 도착 모두 이 규칙을 따른다.
- 5초 게이지는 1.2m 안에 있고 `Mirror_Surface`를 바라볼 때만 채운다. 시선을 떼면 게이지는 그 값에서 멈춘다. 반경을 벗어나면 게이지를 숨기고 이번 측정을 취소한다. 완료 판정 음성은 내지 않는다.

### 근본 원인

- 씬이 삭제된 `WrongButton` GUID를 가리켜 거절 음성이 비어 있었다.
- 미완료 측정 뒤 `WaitingForEquipment`→`WaitingForMirror`가 반경 안에 남아 있으면 같은 자리에서 5초 게이지를 다시 시작했다.
- 5초 타이머는 시작 뒤 거리·시선을 다시 보지 않아, 벗어나도 게이지가 끝까지 찼다.

### 영향 범위

- 대상 씬 패널/AudioManager 클립 참조, `PPEFinaleController` 재진입 무장.
- EDU_203 정상 폐기 음성, 훈련 `TRAIN_003`, 퀴즈·페이드·텔레포트는 바꾸지 않았다.

### 완료한 검증

- 정적 확인: 씬에 남은 `WrongButton` GUID 없음. 거울 재측정은 반경 이탈 뒤에만 무장된다. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 하자 PPE를 몸에 대면 SFX 후 WrongUse 음성이 나오는지, 거울 5초 중 시선을 떼면 게이지가 멈추는지, 1.2m를 나가면 게이지가 사라지는지, 5초를 채운 뒤 그 자리에 서 있으면 다시 돌지 않는지를 확인해야 한다.

## 21. FS-19 이름 필수 입력

### 이번 변경이 대응하는 요청

- 이름 필드가 비어 있으면 제출을 막는다. 한글 입력·Enter 제출 자체는 유지한다.

보존한 기존 동작: Welcome 후 `NameInput` 키보드, 한글 조합, Enter로 이름 제출, 제출 후 Controller Simp/카드 전이, 상세교육 A 분기.

### 적용한 변경

- `HangulKeyboardController`는 공백뿐인 이름에서 `TextSubmitted`를 보내지 않고, XRI `closeOnSubmit`이 키보드를 닫지 않게 한 프레임 막는다.
- `PPEVoiceFlowDirector.NotifyNameSubmitted`도 빈 이름이면 `NameInput`에 남는다.

### 근본 원인

- Enter가 빈 문자열도 제출 이벤트로 넘겨 다음 단계로 넘어갔다.

### 영향 범위

- 한글 키보드 제출과 이름 상태 전이. 키보드 레이아웃, A 상세교육, 카드·모달은 그대로다.

### 완료한 검증

- 정적 확인: 빈 이름은 컨트롤러와 디렉터 모두에서 거절한다. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 이름 없이 Enter를 누르면 키보드가 남고 다음 단계로 안 넘어가는지, 한글 이름을 넣은 뒤 Enter는 기존처럼 진행되는지 확인해야 한다.

## 22. 마감 시점의 후속·검증

### 적용한 변경과 보존

- 오늘 코드·씬 변경은 위 요약 표의 요청에만 대응한다. 착용 승인 계약(`UseApproved` / `UseRejectedContaminated`), `hazmat_suit_on` 계층, 텔레포트, 퀴즈·완료 복귀, EDU_203, TRAIN_003은 유지했다.
- 작업계획 선택은 `ActiveWorkPlan`에 복사하고, 23절에서 착용 승인·완료 슬롯만 분기한다. 태블릿 문서와 진열 숨김은 다음 작업이다.

### 완료한 검증

- 정적 확인: 착용 근접, 니트릴 순서, 모달 작업계획, `EDU_202` GUID, 거울 거리·시선 재검사, 빈 이름 제출 거절. 씬 YAML에 임의 FileID를 만들지 않았다.
- Unity Editor 확인: 이른 세션의 색·idle은 사용자가 저장했다. 페이스실드/고글·니트릴·작업계획 메뉴와 늦은 세션의 음성·거울·이름은 Unity에서 연 뒤 저장·Play Mode 확인이 남는다.
- Quest/OpenXR 확인: 없다. 양안, 헤드셋 트리거, XR 오디오는 정상이라고 말하지 않는다.

### 아직 필요한 수동 검증

1. Unity에서 대상 씬을 열고 저장한다. Reload 대화가 뜨면, 에디터에 저장하지 않은 씬 편집이 없을 때만 Reload한다.
2. `Tools > PPE > Wire FaceShield And Goggle Wear`, `Wire Nitrile Inner Glove Wear`, `Wire Work Plan Choices`, `Configure Glove Color Controls`가 아직이면 실행하고 저장한다.
3. Play Mode: Clean은 몸 앞 트리거로 입고, Contam은 Wrong Answer 뒤 `WrongUse` 음성이 나오며 입지 않는다.
4. 맨손→니트릴은 InnerGlove, 방호복 후 니트릴은 InnerGloveSuit, 바깥 장갑은 니트릴 다음, 니트릴만 있는 손에는 테이프가 붙지 않는다.
5. 카드 → 학습/훈련/테스트 → 작업계획. 교육·훈련은 두 버튼, 테스트는 세 버튼.
6. 거울 5초 중 시선을 떼면 게이지가 멈추고, 1.2m를 나가면 게이지가 사라진다. 5초를 채운 뒤 그 자리에 서 있으면 다시 돌지 않는다.
7. 이름 없이 Enter는 키보드가 남고, 한글 이름 후 Enter는 기존처럼 진행한다.
8. Quest 양안에서 착용 뒤 1인칭 시야, 거울, Unlit 색을 확인한다.
9. 왼손 NearFar가 오른손과 같이 잡기·UI 레이가 되는지 확인한다.
10. 밀폐 선택 시 밀폐 계획서와 안전대·송기마스크·안전모가 나오고, 고글·페이스실드는 단상에서 사라진다. 누출은 그 반대다. 공통 방호복·장화·장갑은 남고, 정품/하자 중에서 올바른 것을 고른다.

### 다음 작업으로 미룬 항목

- 누출 호흡보호구(SCBA) 착용 슬롯. 현재 씬에는 `ScubaGear` identity가 없다
- 입은 부위로 idle 몸 메시 가리기
- walk 클립을 같은 리그에 붙이기. 텔레포트만 할 때 제자리 걷기를 틀지 않는다

## 23. 왼손 컨트롤러와 작업계획 PPE 세트

### 이번 변경이 대응하는 요청

1. 왼손 컨트롤러가 동작해야 한다.
2. 상세시나리오 모달의 밀폐공간/누출대응에 따라 작업계획서와 고를 수 있는 PPE가 갈려야 한다. 두 세트를 한꺼번에 입는 복합 착용은 막는다.

보존한 기존 동작: 오른손 NearFar, 텔레포트 인터랙터, 공통 PPE(방호복·장화·니트릴·네오프렌) 진열, 잡기·검사, 방호복 선행·니트릴 다음 바깥 장갑·테이프 보조 경로, `UseApproved` / `UseRejectedContaminated`, 새 음성 클립 없음. 태블릿 ConfinedSpace/Leak 자식 Transform은 그대로다.

### 적용한 변경

- `Left_NearFarInteractor`(`1779072863`)의 `m_InteractionManager`를 씬의 `XR Interaction Manager`(`458763857`)에 연결했다. 오른손 NearFar와 같다. 새 FileID는 만들지 않았다.
- 모달에서 밀폐를 고르면 태블릿 `ConfinedSpace` 문서와 밀폐 서명 시퀀스(`1603400733`)를 켠다. 누출을 고르면 이미 씬에 있던 `Leak` 문서와 누출 서명 시퀀스(`572508689`)를 켠다.
- 작업계획 선택 시 진열 PPE는 숨기지 않는다(30절). 풀장착 `equipmentRoot` 자식은 건드리지 않는다.
- `CanApprovePpeUse`는 활성 세트 밖 타입의 착용을 거절한다. 테이프는 보조라서 착용은 허용하고 완료 필수에서는 뺀다.
- 완료 슬롯은 활성 세트에 있는 기존 슬롯만 본다. 없는 타입(누출 `ScubaGear`)은 완료를 막지 않는다.

밀폐공간 7종: 내화학성 방호복, 내화학성 장화, 안전대, 송기마스크, 안전모, 내화학성 내부장갑, 내화학성 외부장갑.

누출 7종(29절에서 갱신): 내화학성 방호복, 내화학성 장화, 니트릴 내부장갑, 네오프렌 외부장갑, 화학보안경, 안면보호구, 안전모. 호흡보호구(SCBA)는 최종 세트에서 빠졌다.

### 근본 원인

- 왼손 NearFar만 Interaction Manager 참조가 비어 있어, 오른손과 등록 경로가 달랐다.
- 태블릿에 밀폐/누출 문서가 이미 있었는데 밀폐만 켜 두고, 착용·진열은 전 종류를 그대로 썼다.

### 영향 범위

- 대상 씬 왼손 NearFar 참조, 태블릿 ConfinedSpace/Leak 활성 전환, `PPEVoiceFlowDirector` 착용 승인·진열 숨김, `PPEEquipmentVisualController` 완료 슬롯.
- 공통 PPE 진열, 오른손, 텔레포트, 하자 착용 거절, 퀴즈는 그대로다.

### 완료한 검증

- 정적 확인: 왼손 NearFar manager는 `458763857`. 태블릿 문서 루트 `436817076`/`75888988`, 서명 `1603400733`/`572508689`. 임의 FileID 추가 없음.
- Unity Editor / Quest 확인: 아직 없음. 밀폐 선택 시 밀폐 계획서·안전대/마스크/헬멧만 추가 진열되는지, 누출 선택 시 누출 계획서·고글/페이스실드가 나오고 안전대/마스크/헬멧이 사라지는지 확인해야 한다.

## 24. 교육모드 PPE 체크리스트 패널

### 이번 변경이 대응하는 요청

교육모드에서만, 태블릿 작업계획서 확인(서명 시퀀스 완료) 뒤에 `Window Canvas/CheckList` 패널을 띄운다. 각 `List_N/CheckBox/Check`는 기본 비활성이고, 해당 PPE 착용 성공(`UseApproved`)과 기존 Correct Answer SFX와 같은 시점에 켠다. 좌·우 쌍은 한 행으로 묶는다.

보존한 기존 동작: 태블릿 서명, 착용 SFX·음성, Grab, 텔레포트, 작업계획 착용 세트, Window Canvas의 안내/컨트롤러 가이드 자식, 씬에 작성된 라벨·레이아웃.

### 적용한 변경

- `PPEEducationWearChecklist`가 `CheckList`의 `CanvasGroup`으로 패널을 숨기거나 보인다. Window Canvas 자체는 끄지 않는다.
- 표시 조건: 학습모드 Education + 작업계획 Confined/Leak + `PPETabletChecklistController.IsDocumentCompleted`.
- 밀폐/누출 루트는 활성 작업계획으로만 전환한다. 누출 HUD는 지금 Title만 있어서 행 체크는 없다.
- 밀폐 `List_1`–`List_7` 연결(라벨 기준, 인덱스가 아님):

| 행 | 씬 라벨 | 체크되는 타입 |
| --- | --- | --- |
| List_1 | 내화학성 방호복 | `HazmatSuit` |
| List_2 | 내화학성 장화 | `RubberBootLeft` + `RubberBootRight` |
| List_3 | 안전모 | `ConstructionHelmet` |
| List_4 | 송기 마스크 | `GasMask` |
| List_5 | 안전화 | `TacticalHarness` |
| List_6 | 내부 니트릴 장갑 | `NitrileInnerGloveLeft` + `Right` |
| List_7 | 내화학성 외부 장갑 | `RubberGloveLeft` + `Right` |

List_5 씬 라벨은 `안전화`이고, 새로 넣은 밀폐 계획서 지정 보호구는 `안전대`다. 착용 슬롯은 안전대라서 그 행을 하네스에 연결했다. 라벨을 `안전대`로 고치는 것은 씬 작성 값이라 이번에는 바꾸지 않았다.

- 새 SFX는 넣지 않는다. `PPEActionPanelController.PlayFeedbackSfx`의 Correct Answer가 그대로다.
- 씬 YAML에 FileID를 만들지 않았다. Unity에서 `Tools > PPE > Wire Education Wear Checklist`를 실행해야 `CheckList`에 컴포넌트가 붙고 참조가 저장된다.

### 태블릿 계획서 2종

같은 파일명으로 교체된 `WorkPlan.png`, `WorkPlan_Leak.png`다. GUID는 이전과 같다.

- `WorkPlan.png` `cb7106968166eb34cbf964610b5200d2` → `WorkPlanPlane_Unlit`
- `WorkPlan_Leak.png` `1cefcdf78ccd7dd49be84de8bf06cc63` → `WorkPlanLeakPlane_Unlit`

재임포트가 스프라이트 설정을 Default 텍스처·밉맵으로 바꿨다. 태블릿 Unlit 평면용으로 이전 importer(클램프, 밉맵 꺼짐, Sprite)를 되돌렸다. 픽셀은 새 PNG를 쓴다.

### 완료한 검증

- 정적 확인: Confined 7행과 비활성 Check, Leak Title만, 계획서 GUID·머티리얼 참조. 런타임 스크립트는 작성값 레이아웃을 덮어쓰지 않는다.
- Unity Editor 확인: 메뉴 실행·Play Mode 미실시.
- Quest/OpenXR 확인: 없음.

## 25. 로코모션 시험 복사 씬

원본 `3_PPE_Room_Train_Test_mask.unity`는 그대로 둔다. 시험은 `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity`에서만 한다. 빌드 씬 목록에는 넣지 않았다.

요청: 텔레포트를 잠시 끄고, `4_VO_PPE_EDU_001_PPE_MoveToPPE`가 끝난 뒤 `Teleport_0`(PPE_1 Arrival Anchor)로 자동 이동한 다음 스틱 걷기를 시험한다. 음성 파일은 바꾸지 않는다.

### 적용한 변경

- `PPEVoiceFlowDirector`에 `m_AutoMoveToPpeThenLocomotion`을 넣었다. 기본은 꺼짐이라 원본 씬 동작은 유지된다.
- 복사 씬에서 `Tools > PPE > Setup Locomotion Trial Scene`을 실행하면 플래그를 켜고 Move 프로바이더를 `PPE Teleport-Only Locomotion/Move`에 만든다. 새 FileID는 Unity가 만든다.
- TeleportInstruction에 들어가면 텔레포트 입력 게이트는 닫고, 도착 후 `PpeArea`로 넘어간다. EDU_002 텔레포트 안내는 이 경로에서 재생하지 않는다.

### 검증

- 정적 확인: 복사 씬 파일 생성, 원본 씬 경로 미변경.
- Unity Editor / Quest 확인: 복사 씬을 연 뒤 메뉴 실행과 Play Mode가 남는다.

## 26. 로코모션 복사 씬 idle 하반신 애니메이터

### 이번 변경이 대응하는 요청

로코모션 시험 복사 씬의 `PPE_D_Player_Idle`에 걷기 애니메이션에 필요한 리그 연결을 넣는다. 원본 `3_PPE_Room_Train_Test_mask.unity`와 idle Transform·착용 PPE 부모는 유지한다.

### 적용한 변경

- idle과 `PPE_D_Player` walk FBX는 같은 Generic 본 이름(`Pelvis`, `L_Thigh`/`R_Thigh` 등)을 쓴다. walk FBX에는 `preset:biped:walk` 테이크가 있다. idle FBX에는 클립이 없고 선 자세만 있다.
- 메뉴 `Tools > PPE > Setup Idle Locomotion Animator`가 복사 씬 idle에만 `Animator`와 `PPEIdleLocomotionAnimator`를 붙인다. FileID는 Unity가 만든다.
- walk FBX 임포터만 Generic Avatar를 만들고 walk 클립을 추출한다. idle FBX 임포터·원본 씬 idle은 바꾸지 않는다.
- 컨트롤러는 `Speed` 0=idle 하반신 포즈, 1=walk. 상체·손·머리는 마스크로 제외한다. 스틱 Move가 켜진 뒤 실제로 움직일 때만 걷는다. 자동 텔레포트 점프는 무시한다.
- `Tools > PPE > Setup Locomotion Trial Scene`을 다시 실행하면 같은 idle 연결도 이어서 한다.
- 착용 PPE는 본에 스키닝되어 있지 않아 다리가 움직여도 장화·방호복은 따라가지 않는다. 이 범위는 이번에 바꾸지 않았다.

### 검증

- 정적 확인: 대상은 복사 씬 경로만. 원본 mask 씬 YAML은 이 절에서 수정하지 않았다.
- Unity Editor 확인: 복사 씬을 연 뒤 위 메뉴를 실행하고 저장해야 Animator가 붙는다. Play Mode·거울 하반신·Quest는 아직 없다.
- Quest/OpenXR 확인: 아직 없음.

## 27. 고글 Contam 렌즈 오염 재질

### 이번 변경이 대응하는 요청

`PPE_A_Goggle_Contam`의 `Glass_Lens`에, 유리는 비치되되 표면에 오염 자국이 보이게 한다.

보존한 기존 동작: `PPE_A_Goggle_Clean`의 `Glass_Lens`는 `PPE_Mask_Glass`. 프레임 Unlit, Grab·착용 판정, 진열 Transform.

### 적용한 변경

- 셰이더 `Tyche/PPE/Glass Contamination` (`Assets/Shaders/PPEGlassContamination.shader`). Single Pass Instanced 매크로 포함. 투명 블렌드, Cull Off.
- 얼룩 마스크 `Assets/Textures/PPE/GoggleGlass_ChemicalStainMask.png`. 물때 고리·흘러내린 자국·튄 점. 장화 오염과 같이 황갈색 대비.
- 머티리얼 `Assets/Materials/PPE/Defects/PPE_A_Goggle_Glass_Contam.mat`. 대상 씬 `PPE_A_Goggle_Contam/Glass_Lens`만 이 재질을 쓴다.
- `Tools > PPE > Repair FaceShield Goggle Scene Materials`가 Contam 유리를 깨끗한 `PPE_Mask_Glass`로 되돌리지 않게, Contam 루트는 이 재질로 유지한다.

### 근본 원인

Contam 고글 렌즈가 Clean과 같은 `PPE_Mask_Glass`라서, 프레임 하자와 렌즈 오염이 구분되지 않았다.

### 영향 범위

- Contam 고글 렌즈 Renderer와 유리 오염 셰이더. Clean 유리, 페이스실드 유리, 착용 슬롯은 그대로다.

### 완료한 검증

- 정적 확인: Contam `Glass_Lens`만 `PPE_A_Goggle_Glass_Contam` GUID. Clean·페이스실드는 `PPE_Mask_Glass`.
- Unity Editor / Quest 확인: 아직 없음. Scene/Game View에서 Contam 렌즈 얼룩과 Clean 투명 구분을 확인하고, Quest 양안은 별도로 본다.

## 28. Meta Horizon 상세 페이지 문구

코드·씬이 아니라 스토어 App Metadata용 문구다. 모달·카드 UI는 이 절에서 바꾸지 않았다.

### 확정한 범위

- 플레이 가능한 내용은 PPE 착용 교육·훈련·테스트다. 밀폐공간 LOTO·가스측정 모듈과 화재 진압·대피는 넣지 않았다.
- 시나리오 이름은 두 개다. `밀폐공간 진입 전 PPE`, `화학물질 누출 PPE`.
- Specs(Education, Single player, Standing, Comfortable, Korean, 오프라인, Touch/Touch Plus)는 설명 문구를 넣은 뒤에도 그대로다. Quest 브랜드명은 설명에 쓰지 않는다.

### Short description (한국어, 231/500)

화학물질 취급 현장에 들어가기 전, 개인보호구를 직접 고르고 점검하고 입어 보는 VR 안전교육입니다. 시나리오는 두 가지입니다. 밀폐공간 진입 전 PPE 시나리오와 화학물질 누출 PPE 시나리오. 방호복, 안전모, 호흡보호구, 장갑, 장화, 안전대를 손으로 잡아 몸에 입으며, 정상품과 오염·하자품을 스스로 가립니다. 교육·훈련·테스트 세 모드로 안내 수준이 달라지고, 거울 확인과 퀴즈로 착용 결과를 점검합니다.

### Long description (한국어, 796/1500)

화학물질 안전훈련 VR은 화학물질 취급 현장에 들어가기 전, 개인보호구(PPE)를 올바른 순서로 고르고 입어 보는 1인 VR 안전교육입니다. 헤드셋을 쓰면 이름을 입력하고 컨트롤러 사용법을 익힌 뒤, 카드에서 시나리오를 고릅니다. 같은 PPE 착용 절차를 상황만 바꿔 연습합니다.

- 밀폐공간 진입 전 PPE 시나리오: 밀폐공간에 들어가기 전, 작업 위험에 맞는 보호구를 고르고 손상·오염 여부를 확인한 뒤 올바른 순서로 입습니다.
- 화학물질 누출 PPE 시나리오: 화학물질이 누출된 상황에서 필요한 보호구를 가려 입고, 오염되었거나 하자 있는 장비는 착용하지 않습니다.

단상 위에는 방호복, 안전모, 호흡보호구, 장갑, 장화, 안전대(등지게)와 밀봉 테이프가 놓여 있습니다. 같은 종류의 정상품과 오염·하자품이 함께 있어, 상태를 확인하고 몸에 직접 갖다 대어 입습니다. 오염되었거나 하자 있는 보호구는 잡을 수는 있지만 착용되지 않습니다. 장화·장갑·테이프는 방호복을 먼저 입은 뒤에만 진행됩니다.

학습 강도는 세 가지입니다.
- 교육: 음성 안내와 단계별 설명으로 점검·착용 순서를 익힙니다.
- 훈련: 최소한의 미션만 주고, 틀린 선택은 다시 고르게 합니다.
- 테스트: 힌트 없이 수행한 뒤 소요 시간, 퀴즈 점수, PPE 오선택을 결과로 확인합니다.

착용 결과는 룸 거울에서 확인하고, 작업 정보 태블릿과 종료 퀴즈로 핵심 판단을 점검합니다. 이동은 컨트롤러 텔레포트를 사용합니다.

본 콘텐츠는 현장 배치 전 체험형 안전교육용 시뮬레이션입니다. 법정 자격 교육이나 실제 작업 허가를 대체하지 않습니다.

### 영어 로케일

- Name: `Chemical Safety Training VR`
- Search Keywords: `safety training, PPE, chemical safety, confined space, chemical leak, hazmat, VR training, workplace safety, donning, respirator`
- Short/Long는 한국어와 같은 두 시나리오·세 모드·착용 규칙·면책 구조를 쓴다. Long에 Quest 표기는 넣지 않는다.

### 한국어 Search Keywords

`안전교육, PPE, 개인보호구, 밀폐공간, 화학물질 누출, 방호복, VR훈련, 산업안전, 착용훈련, 안전모`

## 29. 노출/누출 시나리오 최종 PPE

### 이번 변경이 대응하는 요청

노출 시나리오(모달 누출 분기)의 최종 PPE를 다음 7종으로 맞춘다. 밀폐공간 세트는 유지한다.

1. 내화학성 방호복
2. 내화학성 장화
3. 니트릴 내부장갑
4. 네오프렌 내화학성 외부장갑
5. 화학보안경
6. 안면보호구
7. 안전모

보존한 기존 동작: 밀폐공간 필수 세트, 태블릿 Confined/Leak 문서 전환, 하자 착용 거절, Grab, 텔레포트. 태블릿 `WorkPlan_Leak.png` 픽셀은 이번에 바꾸지 않았다.

### 적용한 변경

- `PPEVoiceFlowDirector.m_LeakResponseRequiredItemTypes`에서 `ScubaGear`를 빼고 `ConstructionHelmet`을 넣었다. 씬 직렬화 값도 mask·로코모션 복사 씬에 같이 반영했다.
- 착용 승인·완료 슬롯이 이 배열을 쓴다. 진열 숨김은 30절에서 제거했다.
- 교육 체크리스트 Leak `List_5_Helmet`이 꺼져 있어, 안전모 행이 보이도록 켰다. 라벨·레이아웃은 그대로다.

### 검증

- 정적 확인: 누출 필수 타입이 방호복·장화·니트릴·네오프렌·고글·페이스실드·안전모. `ScubaGear` 없음.
- Unity Editor / Quest 확인: 아직 없음.

## 30. 마감 정리 (진열 유지·태블릿 문서·분기)

### 확정한 동작

모달에서 밀폐공간 / 누출(노출)을 고르면 태블릿 문서와 **착용 승인·완료 판정**만 갈린다. 단상에 진열된 PPE는 작업계획과 상관없이 **모두 활성**이어야 한다. 오염·하자품과 다른 시나리오 장비도 보이게 두고, 학습자가 올바른 것을 고르는지 교육한다.

### 적용한 변경

- `PPEVoiceFlowDirector`가 활성 세트 밖 진열 오브젝트를 `SetActive(false)` 하던 경로를 제거했다. 이미 숨긴 항목은 작업계획 적용·리셋 때 다시 켠다.
- 잘못된 종류를 몸에 대면 기존처럼 착용 거절. 잡은 뒤 맞는지 가리는 교육은 유지한다.
- 누출 태블릿 `WorkPlan_Leak.png`(GUID `1cefcdf78ccd7dd49be84de8bf06cc63`)를 `산성 세정제 누출 대응 작업계획서`(CS-MIX-A-SPILL-001) 픽셀로 교체했다. importer·머티리얼 참조는 그대로다.

### 착용 세트 (코드 기준)

밀폐공간: 방호복, 장화, 안전대, 송기마스크, 안전모, 니트릴 내부장갑, 네오프렌 외부장갑.

누출/노출: 방호복, 장화, 니트릴 내부장갑, 네오프렌 외부장갑, 화학보안경, 안면보호구, 안전모.

태블릿 누출 계획서 지정 보호구 7번째는 **호흡보호구**로 적혀 있다. 착용 로직 7번째는 **안전모**다. 문서 그림과 착용 세트가 한 줄 다르다. 학원 마감 전 어느 쪽을 맞출지는 다음 작업이다.

### 아직 필요한 수동 확인

- Unity에서 mask 씬을 다시 연 뒤, 누출 선택해도 단상 PPE가 전부 보이는지.
- 태블릿에 산성 세정제 누출 계획서가 나오는지. 해상도가 423×483이라 흐리면 원본을 더 큰 PNG로 갈아끼운다.
- 밀폐/누출 각각 올바른 세트만 입고, 다른 종류·Contam은 거절되는지.
- 로코모션 복사 씬 idle walk 메뉴, 교육 체크리스트 와이어 메뉴, Play Mode·Quest.

## 31. 로코모션 복사 씬 입력·자동 이동 회귀 수정

### 적용한 변경

- 대상 씬은 `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity`로 고정했다.
- `Left_NearFarInteractor`의 근거리 물리 마스크를 `Default(1)`에서 `Default + PPE marker layer(65)`로 맞췄다. 오른손과 같은 Near caster 경로로 PPE marker를 탐색한다.
- 활성 `PPEInspectionState`의 오른손 전용 필터를 해제했다. 페이스실드·고글을 포함해 왼손·오른손 Grip을 허용한다.
- `PPEVoiceFlowDirector`에 `TeleportInstruction` 자동 이동 경로의 도착 이벤트 중복 처리를 막았다. 자동 텔레포트 코루틴이 `Teleport_0` 도착 후 `Move`를 활성화하고 `PpeArea`로 전환하는 단일 소유자가 된다.
- `PPE_D_Player_Idle` Animator, Idle/Walk 클립, `DynamicMoveProvider`와 XR Origin 참조를 로코모션 복사 씬에 연결했다.
- 원본 `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`도 같은 왼손 Near caster 마스크와 활성 PPE 양손 Grip 기준으로 수정했다.

### 근본 원인과 영향

- 시작 UI 클릭은 UI Raycaster와 Trigger 경로를 사용하므로 정상이어도, 왼손 PPE Grip은 왼손 Near caster가 marker layer 6을 제외하고 있어 도달하지 못했다.
- 자동 텔레포트는 요청 이후 relay가 먼저 상태를 전환해, 기존 코루틴의 버전 검사가 실패하고 Move 활성화까지 도달하지 못할 수 있었다.

### 검증

- 정적/Unity Editor 확인: 대상 씬의 자동 이동 플래그와 `Teleportation Provider`·`PPE_1 Arrival Anchor`·`Move` 참조가 존재한다. 양쪽 Near caster 마스크는 65이며 활성 PPE의 `rightHandGrabOnly` 수는 0이다.
- 원본 mask 씬 확인: 양쪽 Near caster 마스크는 65이며 활성 PPE의 `rightHandGrabOnly` 수는 0이다.
- 컴파일 확인: Unity `RunCommand` 컴파일 성공, 기존 C# 오류 0건.
- Editor Play Mode 스모크: `Teleport_0` 좌표가 XR Origin에 적용되는 것을 확인했고 Play 종료 후 프로젝트 오류는 없었다. MCP 연결 검증 경고만 남았다.
- Quest/OpenXR 확인: 실제 왼손 Grip/Trigger, 양안 렌더링, 스틱 이동은 아직 수동 확인이 필요하다.

## 32. 밀폐공간 SCBA 차단·풀장착 기준 Pose 복원

### 이번 변경이 대응하는 요청

밀폐공간 시나리오에서 안전대와 SCBA가 함께 착용되는 문제를 차단하고, 몸에서 왼쪽으로 밀려 있던 풀장착 모델을 문서의 단일 기준 Pose로 복원한다.

### 근본 원인

- `PPE/TFGTTT`의 `PPEItemIdentity`가 `TacticalHarness`로 잘못 저장되어 밀폐공간 허용 목록에 포함되어 있었다.
- 같은 오브젝트의 `PPEActionPanelController.voiceFlowDirector`가 비어 있어 `PPEVoiceFlowDirector.CanApprovePpeUse` 작업계획 검사를 우회했다.
- `PPE_A_SuitWear`의 로컬 X가 문서 기준 `-0.017`이 아니라 원본 mask 씬에서 `-0.853`으로 저장되어 몸 기준 왼쪽으로 약 0.836m 이동해 있었다. 거울은 동일 렌더 모델을 반사하므로 본체의 잘못된 위치가 반대 방향으로 보이는 현상까지 함께 만들었다.

### 적용한 변경

- 원본 `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`와 로코모션 복사 씬의 `TFGTTT` 타입을 `ScubaGear`로 수정했다.
- 두 씬의 `TFGTTT` 패널에 각 씬의 `PPEVoiceFlowDirector` 참조를 연결했다. 밀폐공간 필수 배열에는 `ScubaGear`를 추가하지 않았다.
- 두 씬의 `PPE_A_SuitWear` 로컬 Pose를 문서 기준 `Position (-0.017, 0.054, -0.169)`, 기존 회전·스케일 유지로 복원했다.
- 런타임 자동 보정이나 거울 전용 위치 복제는 추가하지 않았다. `PPE Body Anchor → PPE_A_SuitWear`와 동일 모델 반사 경로를 유지한다.

### 검증

- Unity Editor 확인: 두 씬 모두 수트 기준 위치 일치, `TFGTTT=ScubaGear`, SCBA 패널 Director 참조 연결, 밀폐공간·누출 필수 배열의 `ScubaGear` 부재를 확인했다.
- 대상 씬 저장 후 dirty 상태가 해소됐고 Unity Console 오류 0건이다.
- Quest/OpenXR 확인: 실제 밀폐공간 선택 후 SCBA 거부, 안전대 착용, 수트 본체·거울 위치 및 양안 렌더링은 헤드셋 수동 확인이 필요하다.

## 33. 로코모션 이동 후 걷기 입력 진단

### 확인 결과

- 자동 이동 테스트에서 `Teleport_0` 도착 후 `Move`가 `enabled=true`로 전환되고 XR Origin이 `PPE_1 Arrival Anchor` 위치로 이동했다.
- `DynamicMoveProvider`의 Mediator, `Main Camera` 기준 방향, Head Transform이 모두 연결되어 있다.
- 왼손 입력은 `XRI Left Locomotion/Move`, 오른손 입력은 `XRI Right Locomotion/Move`이며 각 액션의 바인딩은 `<XRController>{LeftHand}/{Primary2DAxis}`, `<XRController>{RightHand}/{Primary2DAxis}`다.
- 현재 Unity Play Mode 장치 목록에는 `OpenXR HMD`만 있고 왼손·오른손 XR Controller가 없어, Move 액션의 연결 컨트롤 수가 0으로 보고됐다. 따라서 현재 환경에서는 실제 스틱 걷기 성공 여부를 확인할 수 없다.
- 별도 런타임 점검에서 `DynamicMoveProvider`와 `PPE_D_Player_Idle` Animator의 Mediator·Head Transform·`Speed` 파라미터·Idle/Walk Blend Tree는 정상 연결됐다. 키보드 fallback이나 일반 마우스 입력은 추가하지 않았다.
- 추가 재현에서 `StartFlowAfterFirstRenderedFrame()`의 `m_StartDelayAfterFirstFrame=2` 대기 중 `StartTeleportForTesting()`가 먼저 실행되면, 자동 텔레포트 코루틴이 `Move`를 켜고 `PpeArea`로 전환한 뒤 지연 시작 코루틴이 다시 `StartFlow()`를 호출한다. `StartFlowAtState()`의 초기화 코드가 `Move`를 다시 끄는 상태 경쟁이 확인됐다. 이 로직 수정은 아직 적용하지 않았다.

### 아직 필요한 수동 확인

- Quest/Quest Link를 연결한 상태에서 자동 이동 완료 후 왼쪽 스틱을 움직여 XR Origin 이동과 `PPE_D_Player_Idle` 걷기 애니메이션을 함께 확인한다.
- 컨트롤러가 연결됐는데도 액션의 active control이 생기지 않으면 OpenXR 입력 장치·컨트롤러 바인딩을 별도 환경 문제로 분리해 조사한다.


## 34. 거울 앞 관찰 게이지 미표시 진단

### 확인 결과

- `3_PPE_Room_Train_Test_mask_locomotion.unity`의 `PPEFinaleController` 참조와 게이지 UI 계층은 존재한다.
- `IsWithinMirrorRadius()`는 관찰 지점과 카메라의 수평 거리가 `0.0001`보다 작으면 `false`를 반환한다. XR Origin/카메라가 관찰 Marker 중심에 정확히 겹치면 반경 안이어도 거울 관찰이 중단되어 게이지가 켜지지 않는다.
- 런타임 진단에서 관찰 지점에 정확히 배치한 경우 게이지가 표시되지 않았고, 같은 지점에서 수평으로 `0.2 m` 벗어나면 `NotifyMirrorMarkerArrived()` 직후 게이지 루트가 활성화되었다.

### 판정 및 미적용 사항

- 현재 증상은 게이지 UI 참조 누락보다 거울 관찰 반경의 0 거리 예외 조건이 직접 원인 후보로 확인됐다.
- `PPEFinaleController.cs`의 반경 판정 수정은 아직 적용하지 않았다. Unity Editor·Quest/OpenXR 실기 검증도 아직 필요하다.
## 35. 로코모션 씬 거울 관찰 위치 기반 전환 적용

- `PPEFinaleController.IsWithinMirrorRadius()`의 0거리 예외를 제거해 관찰 Marker 중심에서도 위치 기반 관찰이 시작되도록 수정했다.
- `3_PPE_Room_Train_Test_mask_locomotion.unity`에서 `PPEVoiceTeleportEventRelay`를 비활성화하고 중심/거울 Marker 참조를 제거했다.
- 같은 씬의 `Markers/Teleport_1`, `Markers/Teleport_2`를 비활성화했다. 거울 관찰은 `PPE_2 Arrival Anchor` 위치와 `Mirror_Surface` 시선 조건으로만 진행한다.
- 원본 `3_PPE_Room_Train_Test_mask.unity`는 변경하지 않았다.
- 정적 확인과 Unity Editor 저장까지 완료했다. Quest/OpenXR 실기에서 걷기 진입, 거울 음성 후 게이지 0% 시작, 5초 완료는 아직 확인하지 않았다.
## 36. 2026-08-21 작업 종료 기록

### 오늘 적용한 변경

- `3_PPE_Room_Train_Test_mask_locomotion.unity`를 로코모션 전용 씬으로 정리했다. 거울 관찰은 Marker 이벤트가 아니라 `PPE_2 Arrival Anchor` 위치와 `Mirror_Surface` 시선 조건으로 시작하도록 유지했다.
- 로코모션 씬의 `PPEVoiceTeleportEventRelay`를 비활성화하고 중심/거울 Marker 참조를 제거했다.
- 로코모션 씬의 `Markers/Teleport_1`, `Markers/Teleport_2`를 비활성화했다. 다른 `_mask` 씬의 Marker 구성은 이번 전환에서 변경하지 않았다.
- `PPEFinaleController.IsWithinMirrorRadius()`에서 관찰 지점 중심을 거부하던 0거리 예외를 제거했다.
- SCBA 분기, 풀장착 모델 기준 Pose, 왼손 PPE 입력 및 로코모션 Idle/Walk 구성은 앞선 작업 내용과 함께 기존 기준 문서에 반영했다.

### 오늘 확인한 원인

- 거울 게이지 UI 참조나 Fill 설정이 누락된 것이 아니라, 카메라가 관찰 지점 중심에 정확히 도착할 때 거리 판정이 `false`가 되는 것이 게이지 미표시의 직접 원인이었다.
- 로코모션 씬의 이동 입력은 Inspector 바인딩이 존재하지만, 당시 Unity/OpenXR 세션에는 `OpenXR HMD`만 장치로 노출되고 좌우 Move 액션의 연결 컨트롤 수가 0이었다. 실제 컨트롤러 입력 브리지 문제는 별도 미해결 항목이다.
- 자동 텔레포트 직후 지연된 `StartFlowAfterFirstRenderedFrame()`가 `Move`를 다시 끌 수 있는 상태 경쟁도 확인했으며, 해당 로직 수정은 아직 적용하지 않았다.

### 완료한 검증

- Unity Editor에서 로코모션 씬 저장 및 참조 상태를 확인했다.
- 거울 관찰 지점 중심에 런타임 플레이어를 배치한 회귀 테스트에서 게이지 루트가 활성화되고 Marker 릴레이가 비활성 상태임을 확인했다.
- 임시 XR 진단 오브젝트는 테스트 종료 후 씬에서 제거했다.
- 테스트 중 C# 오류는 없었다. XR 오디오 출력 fallback 및 MCP 연결 경고는 기존 환경 경고로 남아 있다.

### 다음 수동 검증

- Quest/OpenXR에서 컨트롤러가 Unity Input System 장치로 노출되는지 확인한다.
- 자동 이동 후 왼쪽 스틱 로코모션과 `PPE_D_Player_Idle` Walk 애니메이션을 확인한다.
- 실제 PPE 착용 완료 후 거울 앞에서 안내 음성이 끝난 뒤 게이지가 0%에서 시작해 5초 동안 100%까지 진행되는지 확인한다.
- 거울에서 벗어났을 때 게이지가 중단되고 다시 진입하면 재시작되는지 확인한다.

같은 날 오후 후속(음성 재연결, 발소리, 체크리스트, 중도 엑시트 206)은 `Docs/MeetingNotes/2026-08-21_PPE_Room_Train_Test_Followup.md`에 기록했다. QA 추적 문서에는 넣지 않았다.

## 37. 씬 기반 3D 복셀 영역 식별 미완료

### 확인 결과

- 대시보드 공간 분석의 기준 씬은 `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`로 확인했다.
- 씬 YAML에서 `Floor`, `Ceiling`, PPE marker, `PPE_Room_Planar_Mirror`, `PPE_2 Arrival Anchor`, `PPE Room Door Image`의 World 배치를 조사했다.
- Floor 기준 주요 범위는 X `-4.1208~7.8792`, Z `-1.4879~10.7437`이며, PPE 선택 오브젝트는 대략 X `-1.4~2.9`, Z `9.45~10.75`에 모여 있다.
- 거울은 `(4.040, 0.588, 10.501)`, 복귀 앵커는 `(1.836, -0.813, 8.945)`와 `(4.045, -0.813, 8.945)`, 출입구는 `(5.867, 0.265, 10.540)`이다.
- 현재 대시보드의 3D 구조는 이 좌표를 반영한 `1.5m × 8 × 4 × 9` scene-bounds grid snapshot이다. 고정 3×3×3 컨셉은 기준으로 사용하지 않는다.

### 미해결 문제

- 3D 복셀을 보았을 때 각 셀이 PPE 선택·거울·복귀·출구·주 이동 공간 중 어느 영역인지 한눈에 식별되지 않는다.
- 셀을 클릭해야 좌표와 일부 Anchor 정보가 나타나며, 투명 큐브 내부의 영역명·축·범위·근거 오브젝트가 지속적으로 보이지 않는다.
- 따라서 현재 시각화는 씬 배치를 조사한 기록으로는 사용할 수 있지만, UX 분석용 공간 맵으로는 완료로 판단하지 않는다.

### 다음 작업에서 수정할 방향

1. 각 복셀에 X/Y/Z 인덱스와 World AABB 범위를 표시하고, 외부 축 눈금과 기준 원점을 함께 표시한다.
2. PPE 선택, 거울 확인, 중단 복귀, 출구·다음 이동, 주 이동 공간의 영역 경계를 큐브 내부에서 지속적으로 구분한다. 영역명과 근거 오브젝트 범례를 별도로 둔다.
3. Floor·Ceiling Bounds 밖으로 패딩된 셀과 실제 Collider/NavMesh 기준 유효 셀을 다른 상태로 표시한다.
4. 복셀 수와 유효 마스크는 HTML에 임의로 고정하지 않고 Unity에서 씬 Bounds·Collider/NavMesh를 샘플링해 생성한다.
5. 이번 작업에서는 위 문제를 수정하지 않는다. 다음 대시보드 작업에서 수정 후, 정적 확인·브라우저 확인·Unity 씬 좌표 대조를 다시 수행한다.

## 38. 2026-08-24 Meta Horizon 제출 이미지 출력 및 PPE_D_Face 색상 복구

### 이번 작업이 대응하는 요청

- Meta Horizon 제출용 타이틀·히어로·상세 스크린샷을 지정된 PNG 해상도로 출력한다.
- `Assets/Scenes/3_PPE_Room_3mode_loco_cam.unity`의 `PPE_D_Face`에 제공된 JPG 텍스처 색상이 표시되도록 한다.
- 원본 이미지의 구도·문자·로고·PPE 배치와 씬의 Transform·Mesh·입력·상태 흐름은 보존한다.

### 적용한 변경

#### Meta Horizon 제출 이미지

- 원본을 덮어쓰지 않고 다음 8개 결과물을 `C:\Users\lanoc\Downloads`에 별도 저장했다.

| 원본 | 출력 파일 | 출력 규격 |
| --- | --- | --- |
| `safety_training_vr_0.png` | `safety_training_vr_0_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `safety_training_vr_1.png` | `safety_training_vr_1_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `hero.png` | `hero_4000w.png` | 가로 4000px, 4000×2242, 원본 비율 유지, PNG |
| `metahorizon_title.png` | `metahorizon_title_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `2_2_metahorizon_screenshot_1.png` | `2_2_metahorizon_screenshot_1_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `2_3_metahorizon_screenshot_2.png` | `2_3_metahorizon_screenshot_2_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `2_4_metahorizon_screenshot_3.png` | `2_4_metahorizon_screenshot_3_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |
| `2_5_metahorizon_screenshot_4.png` | `2_5_metahorizon_screenshot_4_2560x1440.png` | 2560×1440, 16:9, 24-bit RGB PNG |

- 16:9와 미세하게 다른 원본은 화면을 늘리지 않고 중앙 기준 최소 대칭 크롭 후 고품질 보간으로 확대했다.
- ImageGen에도 해상도·선명도만 보완하고 문자·UI·장비·구도를 보존하도록 요청했지만, 작은 문자와 장비 형태 및 프레이밍이 재해석된 후보는 모두 제외했다. 최종 제출 파일은 원본 픽셀을 기준으로 한 결정론적 리사이즈 결과다.

#### PPE_D_Face 머티리얼

- 프로젝트 소유 머티리얼 `Assets/Materials/PPE/Scene Unlit/PPE_D_Face_Unlit.mat`과 해당 `.meta` 파일을 생성했다.
- URP `Universal Render Pipeline/Unlit` 셰이더를 사용하고 `Assets/FBX/PPE_D_Face/PPE_D_Face.jpg`를 `_BaseMap`과 `_MainTex`에 연결했다.
- 머티리얼 GUID는 `e93ba15e70d347009fa12b243d6a9605`, 텍스처 GUID는 `5a4ce1dd21340534bbd610c53c147fa4`다.
- 대상 씬의 `PPE_D_Face` MeshRenderer가 새 머티리얼을 직렬화 참조하도록 변경하고, 현재 열린 Unity 씬에도 같은 머티리얼을 적용해 저장했다.
- 런타임 자동 수리나 머티리얼 재생성 경로는 추가하지 않았다.

### 근본 원인과 판단

- 제출 이미지 일부는 원본 종횡비가 16:9에 아주 가깝지만 정확히 일치하지 않았다. 단순 강제 스케일은 화면 비율을 왜곡하므로 최소 크롭과 고품질 보간을 사용했다.
- AI 확대 후보는 원본에 없는 디테일을 만들거나 텍스트와 PPE 형태를 바꿀 수 있어 제출용 원본 보존 조건을 충족하지 못했다. 따라서 최종본에는 생성형 보정을 사용하지 않았다.
- `PPE_D_Face.fbx`의 임베디드 머티리얼에는 외부 JPG가 Base Map으로 연결되어 있지 않아 모델이 원본 색상 없이 표시됐다. 씬 직렬화 값이 기준이 되도록 명시적인 프로젝트 머티리얼을 만들어 JPG 참조를 고정했다.

### 영향 범위

- 이미지 작업은 `Downloads`의 새 출력 파일에만 적용했으며 원본 PNG와 PSD는 변경하지 않았다.
- Unity 변경은 새 `PPE_D_Face_Unlit` 머티리얼과 `3_PPE_Room_3mode_loco_cam.unity`의 해당 MeshRenderer 참조로 제한했다.
- `PPE_D_Face`의 Transform, Mesh, 활성 상태는 보존했다. UI, 텔레포트, Grab, Collider, 거울, 입력 소비자 및 런타임 상태 전이는 변경하지 않았다.

### 완료한 검증

- 8개 이미지 결과물의 픽셀 크기와 PNG 픽셀 형식을 프로그램으로 확인했다. 2560×1440 대상은 모두 24-bit RGB이며, 히어로 이미지는 4000×2242로 원본 비율을 유지했다.
- 최종 이미지 전체를 육안으로 확인해 원본의 제목, 로고, 인물, UI 및 PPE 배치가 유지되는지 점검했다.
- Unity AssetDatabase 새로고침과 머티리얼 적용 명령은 컴파일·실행에 성공했다. 적용 결과는 `Shader=[Universal Render Pipeline/Unlit]`, `Texture=[PPE_D_Face]`, 저장 후 `SceneIsDirty=[False]`였다.
- Unity Scene View에서 얼굴·머리카락·파란 의상 색상 텍스처가 마스크와 헬멧 뒤에 정상 표시되는 것을 확인했다.

### 아직 필요한 수동 검증

- Meta Horizon 업로더에서 각 이미지의 해상도·색상 형식 승인 여부와 업로드 후 압축 미리보기를 확인한다.
- 스토어 썸네일과 모바일 크기에서 제목·로고·작은 UI 문자의 가독성 및 안전 영역을 확인한다.
- Quest/OpenXR 헤드셋 양안에서 `PPE_D_Face`의 얼굴·머리카락·의상 색상과 URP Unlit 렌더링을 확인한다. Scene View 확인만으로 양안 검증 완료로 판단하지 않는다.
