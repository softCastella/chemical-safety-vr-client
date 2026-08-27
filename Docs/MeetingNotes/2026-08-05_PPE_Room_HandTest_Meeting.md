# 2026-08-05 PPE Room HandTest 작업 회의록

## 회의 범위

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 대상 기능: PPE 수동 잡기·착용, 풀장착 방호복 모델, 거울, 시나리오 카드·모달, 텔레포트 입력, Quest/OpenXR 실행 안정성
- 이 문서는 2026-08-05 세션과 2026-08-06 후속 작업을 통합한 기록이다. 날짜별 상세는 아래 `2026-08-06` 절과 관련 버그 문서를 본다.

## 합의된 기준

- 장착 후 표시되는 모델의 기준은 `PPE Body Anchor` 아래의 풀장착 모델 `hazmat_suit_on_10`이다.
- 장화·마스크·등지게 등 장착품은 풀장착 모델의 자식으로 두고, 자식의 월드 위치를 런타임에 따로 보정하지 않는다. 자식 위치·회전은 방호복 모델에 맞춰 에디터에서 작성한다.
- 거울은 별도 복제 모델의 임의 위치가 아니라, 장착된 풀장착 모델의 작성된 Transform과 사용된 자식만 보여줘야 한다.
- PPE 상호작용 기준은 PPE 메시에 가까이 쏜 레이가 아니라 각 PPE의 `XR Item Marker_small`과 그 Collider다. PPE 원거리 레이로 인접 물품이 함께 선택되면 안 된다.
- 시나리오 카드는 기존처럼 Trigger 입력으로만 선택한다. 시나리오 카드 UI가 열린 동안에는 텔레포트·이동 입력을 막고, PPE 착용 선택을 시작하면 카드/모달을 닫고 이동을 다시 허용한다.
- UI 배치·크기·색상·레이어 등은 런타임 코드가 덮어쓰지 않고 씬/Inspector 직렬화 값을 기준으로 유지한다.

## 오늘 확인·적용된 작업

### 1. 풀장착 모델과 착용 표시

- `hazmat_suit_on_10`을 방호복 기준 모델로 사용하도록 구성했다.
- 장화, 마스크, 등지게 등 장착품을 풀장착 모델의 자식으로 정리했다. 따라서 각 자식이 별도 형제 오브젝트처럼 독립 이동·회전하지 않도록 하는 방향을 확정했다.
- `PPEEquipmentVisualController`에서 사용 처리된 PPE 자식만 표시하고, 사용하지 않은 자식은 표시하지 않는 구성을 추가했다.
- 착용 애니메이션 이후에도 사용자가 에디터에서 맞춘 방호복의 작성 Pose를 보존하도록 `PPEHazmatEquipController`의 Pose 처리 경로를 점검했다.
- 장화가 공중에 뜨거나 바닥에 박히는 문제는 풀장착 모델의 루트 Pivot·메시 Bounds·방호복 바닥 위치를 함께 확인해야 하며, 현재는 사용자가 `suit_on_10`을 에디터에서 바닥에 맞춘 상태를 기준으로 삼는다.

### 2. 거울 표시와 위치

- 거울은 장착 모델을 별도 위치에 재배치하는 방식이 아니라 반사 카메라가 작성된 모델을 바라보는 구조를 기준으로 점검했다.
- Play Mode에서 풀장착 모델을 거울 전용 레이어로 표시하고, 거울의 `reflectedLayers`가 해당 레이어를 포함하는지 확인했다.
- `PPE Body Anchor`의 HMD 추적은 X/Z와 Yaw만 갱신하고 Y는 기존 작성값을 보존하는 방향으로 정리했다. 이는 아래를 볼 때 HMD 높이에 따라 방호복이 떠오르는 것을 피하기 위한 것이다.
- 거울이 검게 보이거나 Play Mode 시작이 오래 걸리고, 거울·Game View가 늦게 켜지며, 헤드셋 주변 시야가 깨지거나 로비로 튕기는 증상이 보고됐다.

### 3. 시나리오 카드·모달·텔레포트 순서

- 시나리오 카드 선택 프록시와 `ScenarioDetailModal`의 연결 참조를 확인했다.
- 기존 선택 흐름에서 Canvas를 먼저 숨긴 뒤 `modal.Show()`에 도달하지 못하는 순서가 모달 미표시 원인이 될 수 있어, 모달을 먼저 표시하는 흐름으로 수정했다.
- PPE 착용 선택 시 시나리오 선택 루트를 비활성화하고 모달을 닫은 뒤 `NotifyScenarioReadyForMovement()`로 이동 허용을 알리는 상태 전환을 추가했다.
- 카드 입력은 `TrackedDeviceEventData`를 통한 Trigger 이벤트만 받도록 유지하는 방향으로 조정했다. 다만 이 입력 경로는 Quest/OpenXR 실기기에서 아직 재검증하지 못했다.

### 4. PPE 잡기와 패널 참조

- PPE 잡기는 아이템 전체 Mesh나 주변 오브젝트가 아니라 `XR Item Marker_small` 자식 Collider를 기준으로 제한해야 한다는 요구사항을 재확인했다.
- 콘솔에 `PPEMarkerToggleGrab requires an 'XR Item Marker_small' child with a Collider` 오류가 반복되었고, `PPEActionPanelController requires serialized state, panel, button, and feedback references` 오류도 반복되었다.
- `helmet_1`, `backplate_1`, `glove_R_1`, `boots_R_1` 등에서 Rigidbody가 없는데 런타임이 자동 보정하려는 로그가 다수 발생했다. 이는 모델 계층을 정리하는 과정에서 직렬화된 컴포넌트 참조와 런타임 자동 수리 로직이 충돌하는 위험 신호로 기록한다.
- 패널을 계속 호출하는 장착 PPE의 컴포넌트·참조를 정리해야 하며, 사용하지 않는 풀장착 모델의 개별 Grab/패널 컴포넌트는 남겨두지 않는 것을 원칙으로 한다.

### 5. 성능·XR 안정성

- 거울의 매 프레임 반사 렌더링, RenderTexture, 풀장착 모델과 자식 Renderer 수, PPE 패널의 반복 탐색·자동 보정 로그, 중복 헬멧/장착 모델은 GPU·CPU와 XR 로더 안정성에 영향을 줄 수 있는 후보로 분류했다.
- 현재 증상만으로 특정 원인을 확정하지 않았다. Unity Profiler의 CPU/GPU·메모리·RenderTexture, OpenXR 로그, Quest 양안 화면을 함께 측정해야 한다.
- 셰이더 수정이나 컴파일 중 Play Mode/Quest 실행을 시작하지 않는 원칙을 재확인했다.

### 6. 공용 Action Panel 확장·표시명·상태 아이콘 (후속)

방호복에만 있던 `사용`/`폐기` 패널을 Grab PPE 전반으로 확장하고, 송기 마스크만
`확인하기` 3버튼을 쓰도록 정리했다. 상세·버그 추적은 아래를 기준으로 한다.

- 회의록: `Docs/MeetingNotes/2026-07-16_PPE_Grab_Body_Equip_Interaction.md` §32
- 버그: `Docs/Bug/2026-08-05_PPE_ActionPanel_TooSmall_PoseScale.md`

#### 적용·합의

| 항목 | 내용 |
| --- | --- |
| 2/3버튼 레이아웃 | `PPEActionPanelSharedPresentation` 프리셋 전환. 마스크만 3버튼(높이 420), 나머지 2버튼(높이 310) |
| 패널 배치 | 방호복은 작성 Pose 유지. 그 외는 Bounds 위 + 시청자 향함. 월드 스케일은 방호복 작성값 유지 |
| 표시명 | 인산계 세정제 혼합기 청소 시나리오 기준 **내화학성** 통일. 마스크는 **송기 마스크**, 등판은 **등지게**, 테이프는 **내화학 테이프** |
| 상태 아이콘 | Pass/Error는 버튼 텍스트 **오른쪽**(`Icons` x≈75). 세로만 각 버튼 Y에 맞춤. 텍스트 중앙과 겹치지 않음 |

#### 확정 표시명

| PPE | 패널 표시명 | 버튼 |
| --- | --- | --- |
| 방호복 | 내화학성 방호복 | 사용, 폐기 |
| 마스크 | 송기 마스크 | 사용, 확인하기, 폐기 |
| 안전모 | 안전모 | 사용, 폐기 |
| 장갑 | 내화학성 장갑(우) / (좌) | 사용, 폐기 |
| 장화 | 내화학성 장화(우) / (좌) | 사용, 폐기 |
| 테이프 | 내화학 테이프 | 사용, 폐기 |
| 등판 | 등지게 | 사용, 폐기 |

#### 용어 결정 메모

- **내화학성 vs 내산성**: 제품 범주는 내화학성. 시나리오 설명에만 인산/내산을 적고 UI 라벨은 내화학성으로 통일.
- **테이프**: 포장용 패킹 테이프가 아니라 방호복 이음매용 **내화학 테이프**(ChemTape류).
- **등지게**: “등판”은 패널에 잘 안 읽혀 **등지게**로 확정.

#### 완료한 검증 / 남은 검증

- 씬 `itemDisplayName`·SharedPresentation 프리셋·아이콘 Y 직렬화 반영을 정적 확인했다.
- Play Mode에서 송기 마스크 3버튼 시 아이콘이 버튼 사이가 아니라 각 버튼 오른쪽 가운데에
  붙는지, 2버튼 PPE도 동일하게 유지되는지는 Unity/Quest 수동 확인이 남는다.

## 근본 원인 후보와 영향 범위

- 풀장착 모델의 부모 기준과 자식의 작성 Transform이 일치하지 않으면 옷·장화·마스크가 서로 다른 위치와 방향으로 보인다.
- 거울 전용 레이어, 반사 카메라 Culling Mask, RenderTexture가 한 곳이라도 어긋나면 거울 전체가 검게 보일 수 있다.
- 카드 모달을 표시하기 전에 선택 UI를 비활성화하거나, 입력 이벤트 타입을 과도하게 제한하면 카드 Trigger가 작동해도 모달이 열리지 않을 수 있다.
- PPE marker가 원거리 레이 물리 마스크에 포함되면 Mesh가 아니라 marker를 기준으로 하더라도 멀리서 PPE가 선택될 수 있다.
- 런타임에서 Rigidbody·패널 참조를 자동 생성하거나 반복 보정하면 콘솔 오류, Play Mode 진입 지연, 불필요한 객체·컴포넌트 증가로 이어질 수 있다.

## 완료한 검증

- 씬 YAML과 스크립트의 직렬화 참조, 풀장착 모델 계층, 거울 레이어·RenderTexture 참조, 카드/모달 참조, marker 레이어·레이 마스크 구성을 정적으로 확인했다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`를 실행해 C# 컴파일 오류 0개를 확인했다. 기존 경고는 남아 있다.
- 기존 관련 버그 문서와 중복되지 않도록 카드/입력, 거울/Play Mode, 방호복/장착 기록에 오늘의 회귀 내용을 추가하는 방식으로 문서화한다.

## 아직 완료되지 않은 검증

- Unity 라이선스 문제로 batchmode 씬 로드와 실제 Play Mode 실행을 완료하지 못했다. 따라서 현재 거울이 바닥에 정확히 서 있는지, 카드 모달이 실제 Trigger로 열리는지, PPE가 원거리 레이에 반응하지 않는지는 미확정이다.
- Quest/OpenXR에서 양손 Trigger 카드 선택, 모달 표시, 모달 중 텔레포트 차단, PPE 선택 후 이동 허용을 순서대로 확인해야 한다.
- 거울의 양안 표시, 검은 화면 재발, 픽셀화, 주변 시야 깨짐, 아래를 볼 때 로비 이탈 여부를 실기기에서 확인해야 한다.
- 풀장착 모델이 바닥에 처박히던 문제는 Body Anchor(Camera Offset + 월드 Y 고정) 버그로 정리했다. Scene/Play/거울 수동 확인은 남음.

## 2026-08-06 세션 요약

대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`로 고정했다. 상세는 아래 절과 버그 문서를 본다.

| 항목 | 적용·확인 | 상세 문서 |
| --- | --- | --- |
| 시나리오 모달 / Heading / Canvas | `Show`가 Modal Canvas 활성화·HUD+Heading 숨김. Canvas 루트는 끄지 않음. 씬 저장으로 끊긴 참조·활성 상태 재연결 | `Docs/Bug/2026-07-30_PPE_Room_Card_Ray_Selection.md` |
| Modal Canvas 포즈 | `AuthoredWorldCanvasPose`로 작성 포즈 잠금. 동기화 위치 ≈ `(0.02, 1.43, 1.6)` | `Docs/Bug/2026-07-30_XR_UI_Canvas_PlayMode_Transform_Overwrite.md` |
| BGM | `_scale_0` 씬 에셋으로 Safe-Horizons 재생. PPE 착용 교육 시 **2.5초** 페이드. AudioManager 씬 큐/재시작 보강 | 동일 Card Ray Selection 문서 BGM 절 |
| 본체 테이핑 `taped` | Unlit 머티리얼 + Editor 배치/슬롯 연결. 거울 레이어 30 | 아래 `taped` 절 |
| 풀장착 바닥·키 | **Body Anchor 버그**: Camera Offset + 월드 Y 고정 → local Y 음수 밀림 → 처박힘. BA를 XR Origin으로 분리, local Y=0.06, suit Y=−0.715 유지 | `Docs/Bug/2026-08-04_PPE_Hazmat_Toggle_Grip_Inspection_Panel.md` Body Anchor 절 |

### 공통 주의

- Unity에서 에이전트가 고친 `_scale_0`을 연 뒤 **오래된 에디터 상태로 Don’t Save 하지 말 것.** `Modal Canvas` 비활성·빈 Heading 참조·옛 Body Anchor 부모가 다시 저장되면 회귀한다.
- Hierarchy 기준: `XR Origin (VR)/PPE Body Anchor/hazmat_suit_on_10` (Camera Offset 아래가 아님).
- `taped` Editor 메뉴는 Unity에서 수동 실행이 필요하다(라이선스/점유 시 batchmode 불가).
- 키 맞춤(스케일 확대)과 Quest 실기기 발 접지 확인은 미완료.

## 2026-08-06 추가: 풀장착 테이핑 FBX (`taped`)

### 요청

- `hazmat_suit_on_10` 아래 추가된 `taped` FBX를 거울에 보이게 하고 URP Unlit 색으로 표시한다.

### 적용

- Unlit 머티리얼: `Assets/Materials/PPE/Scene Unlit/Taped/taped_Unlit.mat` (`taped_basecolor` 사용)
- Editor 도구: `Tools/PPE/Convert Taped FBX Materials to URP Unlit`,
  `Tools/PPE/Place Taped Under Full Suit And Wire Slot`
  (`Assets/Editor/PPETapedFbxUnlitSetup.cs`)
- 풀장착 루트 자식으로 두고 `PPEEquipmentVisualController` PackingTape 슬롯에 연결하면
  Play Mode에서 레이어 30(Mirror Only)으로 승격되어 거울에 반사된다.
- 기존 손목 손 모델 테이핑 경로는 유지한다. 이번 `taped`는 방호복 본체 테이핑 메시다.

### 수동 확인

1. Unity에서 `_scale_0` 씬을 연 뒤 위 메뉴를 실행한다 (이미 Hierarchy에 `taped`가 있으면 포즈를 보존하고 Unlit·슬롯만 연결).
2. Play Mode에서 테이프 사용 승인 후 `taped`가 켜지고 거울에 Unlit 색으로 보이는지 확인한다.

## 2026-08-06 추가: 풀장착 바닥 처박힘 → Body Anchor 수정

상세·잘못된 시도·최종 표는  
`Docs/Bug/2026-08-04_PPE_Hazmat_Toggle_Grip_Inspection_Panel.md`의  
**「2026-08-06 수정: Body Anchor가 바닥에 처박히던 원인」** 절을 본다.

### 요약

- 원인: Body Anchor가 `Camera Offset` 아래 + `UpdateBodyAnchor` 월드 Y 고정 → Play에서 local Y 음수 밀림.
- 수정: BA를 **`XR Origin (VR)`** 직속으로 이동, BA Y **0.06**, suit Y **−0.715** 유지, local X/Z만 머리 추종.
- suit Y만 올렸다 내렸다 하는 방식은 폐기. 이후 접지 미세 조정은 BA Y 또는 suit Y 중 하나만.

### 수동 확인

1. 씬 디스크 재로드 → Hierarchy `XR Origin (VR)/PPE Body Anchor/hazmat_suit_on_10`
2. Scene·Play·거울에서 발 접지 확인

## 다음 작업 우선순위 (2026-08-06 저녁 갱신)

1. 컨트롤러 가이드 등 **미저장 Hierarchy 작업은 씬 저장 후** 유지. 디스크 리로드로 날리지 않는다.
2. `helmet_wrong` 아래 marker / Action Panel Pose가 꺼져 있는지 Play에서 Grab 재확인.
3. 텔레포트 레티클: 씬이 `PPEDirectional`/`PPEBlocking` 프리팹을 가리키는지 확인 후 Prefab Scale로 미세 조정.
4. 자식 PPE 흡착·방호복 Front→Body·핸드 페이드를 Quest에서 확인.
5. 룸스케일 Floor 발이 PPE Floor에 붙는지, Meta 뷰 재설정 후에도 Floor 모드가 유지되는지 확인.
6. Enter→Quit fall-through Guard, UI 떨림(4× MSAA), 원본 taped 메시 비표시를 각각 수동 확인.
7. Action Panel·카드→모달→PPE 교육→이동 허용 순서 회귀.

---

## 2026-08-06 후반 세션 종합 (`3_PPE_Room_HandTest_scale_0`)

대상 씬을 `_scale_0`으로 고정한 오후~저녁 작업 정리. 기존 절과 중복되는 항목은 여기가 **최종 상태**다.

### 변경 전 필수 질문 (당일 적용)

1. Inspector/씬 작성값 보존: 앵커·레티클 프리팹 Scale·헬멧 호스트 Grab은 씬/프리팹이 기준.
2. 단일 기준: 착용 시각=`PPE Body Anchor→hazmat_suit_on_10`, Grab=`helmet`만, 연출 소유=`PPEHazmat` vs `PPEEquipmentVisual` 분리.
3. 입력 경로: NearFar/Teleport 원점, marker layer 6, `PPEMarkerToggleGrab` SelectExit→Pose 복원.
4. 실패 시 런타임 자동 수리 금지. `helmet_wrong` 정리는 Editor/`PPEDefectVisualSetup`.
5. 영향 소비자: UI 레이, 텔레포트 레티클, PPE Grab, 장착 연출, Floor 트래킹.
6. 기준: 정적 YAML·코드 → Unity Play → Quest(미완 항목은 미완으로 표기).

### 1. 풀장착 1인칭 미표시 (Mirror Only)

| 항목 | 내용 |
| --- | --- |
| 원인 | `equipmentRoot` 전체를 layer 30으로 승격 → Main Camera 컬링에서 제외, 거울만 표시 |
| 조치 | 기본은 루트 승격 안 함. 헬멧·마스크 자식만 Use 시 Mirror Only |
| 파일 | `PPEEquipmentVisualController.cs`, scale_0 |

### 2. 왼손 레이 분리·휨

| 항목 | 내용 |
| --- | --- |
| 원인 | NearFar/Teleport local X=`-0.141`(옛 글러브 보정). Left LineVisual 곡선 on |
| 조치 | 양 원점 `(0,0,0)`. Left `SmoothlyCurveLine=0`, `LineBendRatio=0` |
| 주의 | Unity 메모리 씬이 디스크를 덮어쓰면 `-0.141` 재발. 저장/리로드 시 Inspector 확인 |

### 3. 장착 연출: 방호복 Front→Body, 자식은 흡착만

| 대상 | 동작 |
| --- | --- |
| 방호복 | `PPEHazmatEquipController` Front→Approach→Body 유지. Front/Approach Y를 최종 suit와 맞춤(아래→위 경로 제거) |
| 장갑·장화 등 | 공용 Front/Approach 폐기. 최종 Pose 앞 0.28m + Y180° 회전 후 0.85초 흡착 |
| 파일 | `PPEEquipmentVisualController.cs`, scale_0 직렬화 필드 |

### 4. 텔레포트 레티클 과대

| 항목 | 내용 |
| --- | --- |
| 조치 | 프로젝트 소유 `Assets/Prefabs/PPEDirectionalTeleportReticle`, `PPEBlockingTeleportReticle` 루트 Scale `≈1/3` |
| 연결 | 양손 Teleport Interactor `Reticle`/`Blocked Reticle` → 위 프리팹 |
| Inspector | Interactor에 크기 슬라이더 없음. **프리팹 Transform Scale**로 조절 |
| 주의 | 씬이 다시 샘플 GUID(`893219…`)를 가리키면 큰 레티클이 복귀함. 연결 재확인 |
| 문서 | `Docs/Bug/2026-08-01_PPE_HandTest_Teleport_Ray_Input_Mediation.md` |

### 5. 룸스케일 Floor와 Meta 뷰 재설정

- `RequestedTrackingOriginMode=Floor`, `CameraYOffset=0`은 **뷰 재설정으로 풀리지 않음**.
- Meta 버튼 재설정은 수평/요 원점만 맞추고, Floor 모드·Guardian 자체를 끄지 않음.
- 체감 어긋남은 floor 추정 재정렬일 수 있음. 모드 해제가 아님.

### 6. 하자 헬멧(`helmet_wrong`) 부들거림·미Grab

| 원인 | 설명 |
| --- | --- |
| A | `helmet_wrong` 비키네마틱 Rigidbody ↔ 부모 `helmet` RB ↔ `LateUpdate` Pose 복원 충돌 |
| B | `helmet_wrong`에 marker/Action Panel Pose를 켜면 중복 marker가 near hit를 가로채고, 그쪽 Grab은 꺼져 있어 안 잡힘 |

| 올바른 구성 | |
| --- | --- |
| `helmet` marker / Action Panel Pose | **켜짐** (Grab·패널 소유) |
| `helmet_wrong` marker / Action Panel Pose | **꺼짐** (시각만) |
| `helmet_wrong` Rigidbody | kinematic |

- `PPEDefectVisualSetup` Configure/Validate에 kinematic·Collider off·자식 marker 비활성 검사 추가.
- 컨트롤러 가이드 등 미저장 작업이 있으면 **씬 리로드 금지**. 저장 후 Hierarchy에서 자식 marker/패널만 끌 것.

### 7. `PPEMarkerToggleGrab` SetParent 오류

```
GameObject is already being activated or deactivated.
… ReturnToAuthoredPose ← OnSelectExited ← XRBaseInteractable.OnDisable
```

- 원인: SelectExit가 Interactable `OnDisable` 스택에서 발생하는데 즉시 `SetParent` 호출.
- 조치: Pose 복원을 **한 프레임 뒤**로 미룸. 비활성 중이면 `pendingAuthoredPoseRestore` 후 `OnEnable`/`LateUpdate`에서 처리.
- 파일: `Assets/Scripts/PPEMarkerToggleGrab.cs`
- 풀장착 모델 Pose와 무관. Grab 관찰용 PPE만 작성 Pose로 되돌림.

### 8. 당일 오전에 이미 반영·유지된 항목 (요약)

- 방호복 Front→Body + 핸드 `_Fade` 크로스페이드
- 룸스케일 Floor 정렬, `CameraYOffset=0`, 눈높이 클램프 제거
- 한글 키보드 Grab/Scale Handle 비표시·Grab 비활성
- Enter 제출 후 Quit ray fall-through Guard (상세: `Docs/Bug/2026-08-06_PPE_Keyboard_Enter_Ray_Fallthrough_Quit.md`)
- UI 떨림 진단 Probe, PC_RPAsset 4× MSAA
- 원본 `taped_L_1`/`taped_R_1` 기본 비활성

### 9. XRI 레이 시각화 오류 폭주 (재진입으로 해소)

- 콘솔의 최초 예외는 `CurveVisualController.ComputeFallBackLine`의 `IndexOutOfRangeException: Index 0 is out of range of '0' Length.`였다. XRI `onBeforeRender`에서 fallback 점 배열이 길이 0인 상태로 접근했다.
- 이어서 비정상 LineRenderer Bounds가 `Invalid AABB a`, `IsFinite(distanceForSort)`, `IsFinite(distanceAlongView)`를 만들고, `PlanarMirrorRenderer`의 반사/Scene View 렌더링이 이를 반복 렌더링해 오류 수를 증폭했다.
- 두 손 `CurveVisualController`의 씬 작성 `m_VisualPointCount`는 20이며 필수 LineRenderer·Curve Visual·Origin 참조도 연결되어 있어, 직렬화 값 누락은 확인되지 않았다.
- 별도 `TrackedDeviceGraphicRaycaster.OnDisable`의 `KeyNotFoundException` 두 건은 `XR UI Canvas`, `Modal Canvas`가 백업 씬 로드 중 종료될 때 기록됐다.
- 조치/결과: Play Mode 종료 후 `_scale_0` 씬을 다시 연 뒤 오류 폭주가 멈췄다. 패키지 파일이나 입력·레이 표현값은 수정하지 않았다.
- 상태: Unity 재진입으로 해소된 관찰 결과이며, 추적 손실·도메인 리로드·Quest/OpenXR 새 실행에서 재발 여부를 다시 확인해야 한다.

### 주요 경로

- `Assets/Scripts/PPEEquipmentVisualController.cs`
- `Assets/Scripts/PPEMarkerToggleGrab.cs`
- `Assets/Editor/PPEDefectVisualSetup.cs`
- `Assets/Prefabs/PPEDirectionalTeleportReticle.prefab`
- `Assets/Prefabs/PPEBlockingTeleportReticle.prefab`
- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`

### 검증 구분

| 구분 | 상태 |
| --- | --- |
| 정적 | 씬 YAML·스크립트·프리팹 연결·헬멧 규칙 문서화 |
| Unity Editor | 레이 원점·레티클 연결·헬멧 Grab은 Play 재확인 필요(사용자 저장 중인 컨트롤러 가이드와 충돌 주의) |
| Quest/OpenXR | 장착 연출, Floor 발 접지, 레티클 크기, 헬멧 Grab, 양안·주변 시야 — **미완** |

### 운영 주의

- Unity가 열린 씬으로 디스크를 덮어쓰면 레이 오프셋·레티클 GUID·Front 앵커 Y가 되살아날 수 있다.
- 컨트롤러 가이드 등 진행 중 작업은 **먼저 씬 저장**. “디스크 리로드로 헬멧만 고친다”는 방식은 가이드를 날린다.

## 2026-08-05 추가 확정: PPE 하자 외형과 헬멧 구성

### 확정된 대상

이번 HandTest의 하자 외형 대상은 다음 세 종류로 확정한다.

| PPE | 하자 외형 | 표현 방식 |
| --- | --- | --- |
| 송기 마스크 | 유리 균열 | 새 파손 모델 대신 균열 오버레이 시각 구성 |
| 내화학성 장화 | 구멍 | 검은 내부 깊이와 찢어진 고무 테두리를 가진 시각 레이어 |
| 내화학성 장화 | 시멘트·자갈 부착 | 바닥 쪽 회색 시멘트 얼룩과 저폴리 자갈 부착 레이어 |
| 안전모 | 끈 떨어짐 | `helmet_wrong` 시각 모델을 초기 하자 상태로 사용 |

장화의 구멍과 시멘트·자갈은 같은 `Contaminated` 상태에서 함께 표시하는 구성으로 우선 적용한다. 두 하자를 별도 판정하거나 별도 폐기 선택으로 나누는 것은 현재 `Clean / Contaminated` 상태 모델의 범위를 넘으므로 후속 요구사항으로 분리한다.

### 헬멧 작성 기준

- 상호작용과 상태 소유자는 `helmet` 하나만 유지한다.
- `helmet_wrong`은 끈이 떨어진 시각 모델이며, 별도의 `XRGrabInteractable`, `PPEInspectionState`, `PPEActionPanelController`, marker Collider를 상호작용 경로로 사용하지 않는다.
- 초기에는 `helmet_wrong`을 표시하고, 오염품 폐기 승인 후 정상 헬멧 시각을 표시한다.

### 2026-08-06: 하자 헬멧 부들거림·미Grab 원인

- Grab 호스트는 `helmet` + `XR Item Marker_small`(layer 6)이다. `helmet_wrong`은 시각만이다.
- 그런데 `helmet_wrong`에 **비키네마틱 Rigidbody**가 남아, 부모 `helmet` Rigidbody와 `PPEMarkerToggleGrab.LateUpdate`의 작성 Pose 복원과 매 프레임 충돌했다 → 부들거림, marker Hover/Select 불안정.
- 조치: `helmet_wrong` Rigidbody를 kinematic(+ 에디터에서 collision off), 자식 Collider 비활성. `PPEDefectVisualSetup` Configure/Validate에 동일 규칙 추가.
- **추가:** `helmet_wrong` 아래 `XR Item Marker_small` / `Action Panel Pose`를 정상 헬멧처럼 켜면 안 된다. 활성 중복 marker는 근거리 캐스터 hit를 가로채지만 `helmet_wrong`의 Grab은 꺼져 있어 **잡히지 않는** 증상이 난다. 켜야 하는 것은 부모 `helmet`의 marker·Action Panel Pose뿐이다.
- 헬멧의 씬 작성 Y값은 사용자가 수정한 값을 기준으로 보존한다. 런타임 코드나 외형 구성 도구가 헬멧·장화·마스크의 작성 Transform을 자동 보정하지 않는다.

### 적용 방식과 영향 범위

- 마스크 균열은 마스크의 자식 시각 오버레이로 구성해 Grab, 장착, 거울 표시의 기준 Transform을 건드리지 않는다.
- 장화 하자는 각 장화의 자식 시각 레이어로 구성한다. 새 Collider나 Grab 호스트를 추가하지 않는다.
- 외형 전환은 `PPEInspectionState.ConditionChanged`를 소비하며, PPE 입력·패널·착용 Anchor 로직은 변경하지 않는다.
- Quest/OpenXR에서는 투명 오버레이와 추가 Renderer 수가 양안에 동일하게 표시되는지 확인한다.

### 완료한 확인

- 현재 대상 씬에서 `mask`, `boots_L`, `boots_R`, `helmet`, `helmet_wrong` 오브젝트를 확인했다.
- 대상 PPE의 `PPEInspectionState`, `PPEItemPresentationBinding`, `PPEActionPanelController` 직렬화 구조를 확인했다.
- 헬멧 작성값은 `helmet` Y `1.283`, `helmet_wrong` Y `1.285`로 확인했으며, 이 값은 외형 구현 시 보존 대상이다.

### 아직 필요한 확인

- Unity Scene View에서 마스크 균열의 실제 유리 표면 위치와 양안 깊이감을 확인한다.
- 장화 구멍과 시멘트·자갈 레이어가 발바닥 Bounds, 거울, 장착된 `hazmat_suit_on_10`에 겹치지 않는지 확인한다.
- 오염 장화·마스크·헬멧의 폐기 후 정상 외형 전환을 Play Mode와 Quest/OpenXR에서 순서대로 확인한다.

### 구현 파일과 실행 상태

- `Assets/Scripts/PPEConditionVisualAppearance.cs`를 추가해 `PPEInspectionState.ConditionChanged`에 따라 작성된 정상/오염 시각 그룹을 전환하도록 했다.
- `Assets/Editor/PPEDefectVisualSetup.cs`를 추가했다. `Tools > PPE > Apply Defect Visuals (HandTest Scale)`에서 마스크 균열, 양쪽 장화 구멍·시멘트·자갈 레이어, 헬멧 정상/`helmet_wrong` 전환을 명시적으로 생성하고 씬을 저장한다.
- 생성되는 시각 레이어에는 Grab, marker Collider, Action Panel 컴포넌트를 추가하지 않는다.
- 에디터 메뉴의 batchmode 중복 실행은 현재 같은 프로젝트를 열고 있는 Unity 인스턴스 때문에 차단되었지만, 이후 씬에는 마스크 균열 오브젝트와 장화/헬멧 관련 시각 레이어가 반영된 변경분이 존재한다. Unity에서 임포트 후 Scene View 위치 조정과 Play Mode 확인이 필요하다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`와 `dotnet build Assembly-CSharp-Editor.csproj --no-restore --nologo`는 오류 0개로 완료했다. 기존 참조 충돌 및 사용 중단 API 경고는 남아 있다.

## 카드 Trigger 입력면 시도 및 즉시 롤백

- 카드가 `BoxCollider`와 Mesh 기반 외형만 가지고 있어 `TrackedDeviceGraphicRaycaster` 입력 대상이 없다는 원인을 확인했다.
- 해결 시도 중 Unity `fileID` 허용 범위를 초과한 값을 씬에 직렬화해 콘솔 오류와 PPE 계층/Transform 이상을 유발했다.
- 해당 카드 입력면과 참조는 즉시 세 scale 씬에서 제거했다. 현재 카드 입력 수정은 적용하지 않았으며, Unity에서 잘못 로드된 씬을 저장하지 않고 재로드한 뒤 PPE 상태부터 확인해야 한다.
- 씬 파일 정리 후 `dotnet build Assembly-CSharp.csproj --no-restore --nologo`는 오류 0개였다. 실제 Unity/Quest 검증은 롤백 상태 확인 후 다시 진행한다.

## `_scale_0` 카드 입력면 복구

- 기준 씬을 `3_PPE_Room_HandTest_scale_0.unity`로 고정했다.
- 기존 `Interaction Feedback Overlay` 3개를 그대로 사용하고 `RoundedRectangleGraphic.raycastTarget`만 활성화했다. 새 오브젝트와 임의 FileID는 추가하지 않았다.
- 정적 씬 확인은 완료했다. Unity 재임포트 후 카드 Trigger → 모달, 모달 중 텔레포트 차단, PPE 교육 버튼 선택 시 BGM 페이드 순서는 수동 확인이 남아 있다.

## 클릭 이벤트와 BGM 출력 재확인

- `ScenarioCardSelectProxy`가 hover/down은 처리하면서 `OnPointerClick`에서 `TrackedDeviceEventData`가 아닌 이벤트를 버리던 조건을 제거했다. 이제 `Trigger()` 호출 경로가 이벤트 타입에 의해 끊기지 않는다.
- Unity 로그에서 `ScenarioDetailModal.Show()` 호출은 확인했다. 모달/카드 참조가 전혀 연결되지 않은 상태는 아니다.
- `_scale_0` BGM 리소스와 클립 참조는 정상이지만, 헤드셋 미연결 시 XR 오디오 출력 드라이버 오류가 발생했다. Quest Link 연결 후 BGM 시작과 PPE 교육 버튼 선택 시 페이드를 확인한다.
- `dotnet build Assembly-CSharp.csproj --no-restore --nologo`는 오류 0개로 완료했다.

## `_scale_0` Game View 마우스 경로 보완

- 활성 `XR UI Canvas`의 기존 `GraphicRaycaster`와 활성 `ScenarioDetailModal.enableMousePhysicsFallback`이 꺼져 있어 Game View에서 마우스 클릭이 소비되지 않는 것을 확인했다.
- `_scale_0`에서만 두 기존 설정을 켰다. 새 오브젝트와 FileID는 만들지 않았다.
- Unity 외부 변경 Reload 후 Game View 카드 클릭 → 모달 표시를 확인한다.

## 2026-08-06 추가: PPE 장착 손 모델·오디오·활성 상태 보완

### 대상

- 기준 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 기준 장착 계층: `PPE Body Anchor -> hazmat_suit_on_10`

### 적용한 변경

- `PPEEquipmentVisualController`에 PPE별 손 모델 전환 매핑을 추가했다.
  - 장갑: 왼손/오른손 `Glove_Suit` 표시
  - 테이프: 왼손/오른손 `Glove_Suit_Tape` 표시
  - 방호복 착용 후 남는 `LeftHand_BareHand_Suit`와 `RightHand_BareHand_Suit`도 전환 대상에서 함께 숨긴다.
- 테이프(`PPEItemType.PackingTape`)가 오른손만 전환하던 직렬화 매핑을 보완해 `LeftHand_Glove_Suit_Tape`를 추가 연결했다.
- 방호복 장착/폐기 및 PPE 사용 처리 오디오를 연결했다.
  - 방호복 `cloth`, 장화 `Boots`, 등지게 `harness`
  - 폐기 마스크 `mask_breathing_wrong`, 정상 마스크 `mask_breathing_right`
  - 장갑 `Gloves`, 테이프 `Taping`
  - 정답 `Correct Answer`, 오답 `Wrong Answer`
- `_0` 씬의 `PPE Body Anchor`가 `m_IsActive: 0`으로 꺼져 방호복과 손 모델 전체가 보이지 않던 회귀를 확인하고 기준값 `m_IsActive: 1`로 복원했다.

### 원인 및 영향 범위

- 방호복 착용 후 맨손이 남아 보이던 원인은 장갑 전환 시 일반 맨손만 숨기고 방호복용 맨손 모델은 숨기지 않았던 직렬화 매핑 누락이었다.
- 방호복·맨손·장갑이 모두 보이지 않던 원인은 `PPE Body Anchor` 비활성화로 자식 모델과 `PPEHazmatEquipController`가 함께 비활성화된 씬 상태였다.
- 왼손 테이핑 모델 누락은 테이프 손 모델 매핑이 오른손 한 건만 존재했던 문제였다. 런타임 입력 경로, PPE Grab Collider, 카드/UI 입력 경로는 이번 수정 범위에 포함하지 않았다.

### 완료한 검증

- `_0` 씬 YAML에서 `LeftHand_BareHand_Suit`, `RightHand_BareHand_Suit`, `LeftHand_Glove_Suit_Tape`의 실제 GameObject fileID와 매핑을 대조했다.
- `PPE Body Anchor`의 활성값을 기준 버전과 비교했다.
- `dotnet build Prototype_Tyche_Jinyoung.sln --no-restore` 결과 오류 0개를 확인했다. 기존 참조 충돌·사용 중단 API 관련 경고는 남아 있다.
- Unity Play Mode/Quest/OpenXR 실기에서 최종 테이핑 전환과 양안 표시를 아직 재검증하지 않았다. 정적 수정 완료와 실기 정상 확인을 구분한다.

## 2026-08-06 추가: 초기 이름 입력 키보드 안내 UI 결정

### 사용자 흐름

- 테스트용 토글이 꺼져 있으면 `Modal Keyboard Canvas`를 비활성 상태로 두고 씬 시작 시 시나리오 카드를 바로 표시한다.
- 테스트용 토글이 켜져 있으면 씬 시작 순서를 `Modal Keyboard Canvas 활성화 → 사용자 이름 입력 → 키보드 닫기/완료 → 시나리오 카드 표시`로 구성한다.
- `Modal Canvas` 전체를 끄지 않고 시나리오 카드의 `scenarioSelectionRoot`만 숨긴다. 기존 `ScenarioDetailModal`과 `TrackedDeviceGraphicRaycaster`의 동작을 보존하기 위한 결정이다.

### 키보드 안내 문구

이름 안내는 키보드의 기존 입력 출력 영역 Placeholder에 작성한다.

```text
이름을 입력해 주세요.

[기존 키보드 입력 출력 영역]
```

키보드 위에 추가하는 TMP는 사용법만 표시한다.

```text
[키보드 사용법]
자/모음 획수: Shift를 누른 후 원하는 자음자/모음자
예) Shift + ㅔ = ㅖ
```

- 별도의 이름 입력 TMP나 `[이름 입력창]`을 추가하지 않는다. 현재 키보드 Placeholder와 기존 입력 출력 영역을 사용한다.
- 추가 TMP 최종 문구는 `[키보드 사용법]`, `자/모음 획수: Shift를 누른 후 원하는 자음자/모음자`, `예) Shift + ㅔ = ㅖ`로 결정했다.
- `ㅖ` 입력은 두벌식 기준 `Shift + p(ㅔ)`이며, `예`는 `dP`, `계`는 `rP`로 조합된다.
- 사용법 TMP의 위치·크기·색상·폰트는 `Modal Keyboard Canvas` 아래에서 Inspector 작성값으로 관리한다.

### 구현 시 주의

- 초기 흐름 컨트롤러는 비활성화될 수 있는 키보드 Canvas 안이 아니라 항상 활성인 씬 오브젝트에 둔다.
- 키보드 완료 전환은 임의의 Update 감지가 아니라 XRI 키보드의 닫힘/제출 이벤트 또는 명시적인 완료 버튼 이벤트를 사용한다.
- `HangulComposer` 정적 조합 규칙에는 `dP → 예`, `rP → 계`가 포함되어 있으므로, 실제 입력이 안 될 경우 Shift 상태와 `GetEffectiveCharacter()`가 `P`를 전달하는지 먼저 확인한다.

### 검증 상태

- `HangulComposer` 소스와 `HangulComposerValidationHarness`에서 `ㅖ` 조합 규칙을 정적으로 확인했다.
- 실제 Quest/OpenXR에서 Shift 입력 후 `ㅔ` 입력, 기존 출력 영역 반영, 키보드 완료 후 시나리오 카드 전환은 아직 수동 검증하지 않았다.

---

## 2026-08-06 세션 요약 (`3_PPE_Room_HandTest_scale_0`)

대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
관련 상세: `Docs/MeetingNotes/2026-07-16_PPE_Grab_Body_Equip_Interaction.md` §2026-08-06 풀장착 연출

### 1. PPE 사용 → 풀장착 연출 + 핸드 페이드

**요구**
- Use 승인 후 앞쪽(뒷모습)에서 몸으로 붙는 연출(너무 빠르지 않게)
- 헤드셋으로 몸을 내려보면 풀장착 모델이 입혀진 상태
- 핸드모델은 페이드로 전환
- GPU 부담 큰 방식 금지(헬멧/거울 시야 깨짐 재발 방지)

**판단**
- 몸에 보이는 것은 별도 아바타가 아니라 `PPE Body Anchor → hazmat_suit_on_10`
- 새 카메라·RT·메시 복제 없이 기존 Transform Lerp 유지

**적용**
- 방호복: 기존 `PPEHazmatEquipController` Front→Approach→Body(약 1.8초)
- `XRHandFormUnlit`에 opaque `_Fade` 추가, `PPEHandModelCrossfade`(MPB)로 Bare↔Suit 약 0.4초 크로스페이드
- 자식 PPE: `animateChildrenOnUse: 1` — **장갑·장화는 한쪽 Use마다** 해당 슬롯만 연출
- 핸드 페이드도 `PPEItemType` 한쪽만(테이프만 양손목 예외)
- Transparent queue / 새 RT 없음

**검증**
- 정적·컴파일 완료
- Play/Quest 양안·주변 시야는 수동

### 2. 시작 눈높이 / 룸스케일 바닥

**증상·논의**
- 앱 시작 후 Y 뷰가 흔들림 → 처음엔 1.55 등신대 최저 클램프 검토
- 공용이라 키 고정이 아니라 **너무 낮아지는 것만 막는** 의도였음
- Play에서 1.55·1.65 등신대보다 **위에서** 시작
- 룸스케일로 바닥을 잡았는데 PPE 룸을 올려도 가디언 바닥과 뜨는 구간

**원인**
- `CameraYOffset=1.1176`이 Device/NotSpecified 높이에 다시 더해짐
- PPE Floor 윗면이 Origin Y=0이 아님(`PPE Background Room` Y=-1.336, Scale 0.8 → Floor top ≈ -0.84)
- Tracking Origin이 Floor가 아님

**최종 적용 (클램프는 폐기)**
- `CameraYOffset = 0`
- `RequestedTrackingOriginMode = Floor` (VR·Hand Tracking)
- `PPE Background Room` Y `-1.336 → -0.496` (Floor/Floor(1) 윗면 ≈ 월드 0)
- `PPE Body Anchor` Y `0.95 → 1.79` (동일 delta로 상대 유지)
- `XRMinimumEyeHeightClamp` 스크립트·씬 컴포넌트 **제거** (룸스케일이 바닥을 잡으면 불필요)
- Editor: `Tools > PPE > Align/Validate Room Floor To Room-Scale (Scale 0)`

**수동 확인**
- Quest Floor 트래킹 후 발이 PPE Floor에 붙는지
- 풀장착·걸이 높이
- OpenXR Floor origin 지원 여부

### 3. 한글 공간 키보드 Grab Handle

**대상**
- Space 키 아래 작은 주황/검정 바 = XRI Spatial Keyboard의 **Grab Handle Affordance**(이동용)
- 좌우 Scale Handle은 크기 조절용

**적용 (`Hangul Spatial Keyboard Test`)**
| 항목 | 조치 |
| --- | --- |
| `Grab Handle Affordance` | `m_IsActive: 0` (이미지 비표시) |
| `Left/Right Scale Handle` + Affordance | `m_IsActive: 0` |
| `XRGrabInteractable` | `m_Enabled: 0` (이동·스케일 불가) |

- 키 입력·Enter/제출 UI는 유지
- 프리팹 원본이 아니라 **scale_0 씬 인스턴스** 기준

**수동 확인**
- Play에서 바/스케일 핸들이 안 보이는지
- 잡아도 키보드가 안 움직이는지
- 키 입력은 되는지

### 세션에서 건드린 주요 경로

- `Assets/Scripts/PPEHazmatEquipController.cs` (+ `PPEHandModelCrossfade`)
- `Assets/Scripts/PPEEquipmentVisualController.cs`
- `Assets/Shaders/XRHandFormUnlit.shader`
- `Assets/Editor/PPERoomScaleFloorAlignSetup.cs`
- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- (제거) `XRMinimumEyeHeightClamp` 관련 스크립트

### 2026-08-06 후속: 풀장착이 1인칭에서 안 보이던 문제

- 원인: `PPEEquipmentVisualController`가 Play 시 `equipmentRoot`(hazmat_suit_on_10) 전체를 Mirror Only 레이어 30으로 올려, Main Camera 컬링(`3221225471`, layer 30 제외)에서 빠짐. 거울 반사 카메라만 layer 30을 포함해 거울에서만 보임.
- 조치: 기본은 **루트 전체 Mirror Only 승격 안 함**. 헬멧·마스크 자식만 Use 시 Mirror Only. `applyMirrorOnlyLayerToEntireEquipmentRoot`는 레거시 옵션(기본 끔).
- 수동: 방호복 Use 후 고개를 숙여 몸통이 보이는지, 거울에도 보이는지 확인.

### 2026-08-06 후속: 왼손 레이 오프셋 + 장착 연출 방향

**왼손 레이**
- 원인: `Left_NearFarInteractor` / 왼손 `Teleport Interactor` local X가 `-0.141`로 남아 손 모델(Visual/Hand Offset ≈ 0)보다 왼쪽으로 분리됨. 오른손은 `(0,0,0)`.
- 휨: Left `LineVisual`의 `SmoothlyCurveLine`/`LineBendRatio`가 유효·무효 hit에서 곡선을 만들어 각도가 꺾여 보임.
- 조치: NearFar·Teleport localPosition `(0,0,0)`. Left LineVisual 곡선 끔(`SmoothlyCurveLine=0`, `LineBendRatio=0`).
- 주의: Unity가 열린 씬 메모리로 디스크를 덮어쓰면 `-0.141`/낮은 Front Y가 되살아남. 수정 후 **디스크에서 씬 다시 로드**하고 Play 전에 Inspector에서 값 확인.

**장착 연출이 아래→위로 보이던 문제**
- 원인: Front Start Y=`-0.911`, Approach Y=`-1.25`, 최종 suit Y=`-0.016` → 기본 시작이 아래→위.
- 조치: Front/Approach를 최종 `hazmat_suit_on_10`과 같은 Y·회전으로 맞추고 Z만 앞→몸 (`z=0.7 → 0.25 → -0.17`).

### 2026-08-06 후속: 자식 PPE는 흡착만, 방호복 Front→Body 유지

- 원인: `PPEEquipmentVisualController` 자식(장갑·장화 등)이 방호복용 Front/Approach를 공유해, 발·손 최종 Pose로 이동할 때 바닥에서 올라오는 것처럼 보임.
- 조치: 자식은 최종 착용 Pose 앞(`attachStartForwardDistance`)에서 `attachStartLocalEulerOffset` 회전 후 흡착. 방호복은 `PPEHazmatEquipController` Front→Approach→Body 유지.
- 씬: 자식 `animationDuration` `0.85`, forward `0.28`, euler Y `180`.

---

## 2026-08-06 후속: 키보드 Enter 후 모달 종료와 종료 버튼 Ray Fall-through

### 대상·기대 동작

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`
- 대상 UI: `Modal Keyboard Canvas`
- 기대 동작: 한글 공간 키보드에서 Enter로 텍스트를 제출하면 **키보드 모달 Canvas만** 비활성화한다. BGM·씬·Play Mode·헤드셋 세션은 계속 유지되어야 한다.

### 관찰된 사실과 원인

- 재현 중 콘솔에 `[Quit Button] HandleQuit invoked`가 기록되었고, Play Mode가 종료되면서 헤드셋이 흰 공간으로 전환되었다.
- Enter 제출 과정에서 `Modal Keyboard Canvas`를 비활성화하면, 같은 XR UI 입력의 레이가 뒤쪽 UI까지 통과하여 Quit Button을 누를 수 있다. 즉, 키보드 Canvas의 종료 자체가 씬 종료가 아니라 **같은 입력의 후속 Ray hit가 종료 버튼 핸들러를 호출한 것**이다.
- 위치가 바뀐 종료 버튼이나 추측성 종료 경로가 아니라 실제 `HandleQuit` 로그를 근거로 판단했다.

### 적용한 경로

- `HangulKeyboardController`에 Inspector 직렬화 필드 `m_CanvasToDisableOnSubmit`, `m_DisableCanvasOnSubmit`을 두고, 씬에서는 대상 참조를 `Modal Keyboard Canvas` 루트로 연결했다. 키보드 시각 루트만 끄는 것이 아니라 모달 전체를 상태 전환 대상으로 한다.
- `OnTextSubmitted`에서 조합을 확정한 뒤 해당 모달 루트만 `SetActive(false)` 한다. 레이아웃·위치·색상 등 UI 표현값은 코드에서 덮어쓰지 않는다.
- `HangulKeyboardSubmitGuard`가 Enter 제출 직후 2프레임 동안 Quit 처리를 억제한다. `QuitApplicationButton`은 이 구간의 호출을 종료하지 않고 `[Quit Button] Ignored because keyboard Enter submission is closing its modal.` 로그로 남긴다.
- `Tools > XR > Hangul Keyboard > Validate Submit Safety` 정적 하네스(`HangulKeyboardSubmitSafetyHarness`)를 추가했다. 씬의 제출 설정, 모달 참조, 제출 시 비활성화 경로, Quit 구현을 확인하며 XR 런타임 이벤트 순서는 검증하지 않는다고 명시한다.

### 검증 상태

- 정적 확인: 씬의 모달 대상 참조와 코드의 제출·Guard 경로를 확인했다.
- Unity/Quest 미완료: 새 Guard 적용 후 Enter 재현에서 `Ignored...` 로그가 남고, `Modal Keyboard Canvas`만 사라지며 Play Mode/BGM/헤드셋 세션이 유지되는지 확인해야 한다.
- 회귀 확인: 일반 Quit Button은 Guard 기간 밖에서 기존처럼 종료하는지 별도로 확인해야 한다.

상세 회귀 기록: `Docs/Bug/2026-08-06_PPE_Keyboard_Enter_Ray_Fallthrough_Quit.md`

## 2026-08-06 후속: HMD 회전 시 월드 UI 떨림 QA 진단

### QA 내용과 현재 판단 범위

- QA: HMD를 좌우로 돌릴 때 UI가 떨려 보인다는 보고.
- 아직 Quest/OpenXR 양안에서 시각 재현·프레임 측정은 하지 않았으므로 카메라 갱신 주기, Canvas 설정, 부동소수점, VSync 중 하나로 단정하지 않는다.
- 정적 조사에서는 `SciFiCardDepthResponse`의 현재 씬 인스턴스가 `enableParallax: 0`이라 카드 parallax가 직접 원인일 가능성은 낮다. `AuthoredWorldCanvasPose`, `XRWorldCanvasPlacement`는 초기 1회 배치 성격이며 연속 추적 스크립트가 아니다. 실제 떨리는 UI별로 별도 확인이 필요하다.
- `Img/UI선떨림.png`에서는 UI 프레임 외곽이 계단처럼 깨지고, HMD 회전 시 샘플링이 흔들리는 양상이다. Transform 위치 이동보다 렌더링 샘플 부족을 우선 후보로 분리했다.
- 현재 Quest Link/OpenXR 테스트는 Standalone 품질의 `Assets/Settings/PC_RPAsset.asset`을 사용한다. 이 자산의 `m_MSAA`가 `1`(MSAA 미사용)이었으며, `2026-08-06`에 `4`(4× MSAA)로 변경했다. `m_RenderScale`은 1.0으로 유지했다.

### 추가한 비파괴 진단 수단

- `XRUIJitterProbe`는 선택한 Canvas/UI 루트의 Transform을 수정하지 않고 `Update → LateUpdate → Application.onBeforeRender` 사이의 위치·회전 변화, 카메라 회전 속도, 프레임 시간을 6초간 기록한다.
- `Tools > XR > Diagnostics > Add UI Jitter Probe To Selected`는 선택된 Canvas/UI 루트에 Probe를 추가한다. 선택이 없을 때 메뉴를 비활성화하지 않고 실행 시 명확한 오류를 남긴다.
- Play Mode에서 컴포넌트 Context Menu의 `Begin Jitter Capture`를 실행한 후 HMD를 약 6초 회전하고, `[XR UI Jitter] Capture complete` 로그를 수집한다.

### 판별 기준·검증 상태

- `Update → LateUpdate` 또는 `LateUpdate → BeforeRender` 변위가 계속 크면 해당 UI Transform을 런타임에서 쓰는 경로를 우선 추적한다.
- Transform 단계 변화가 작고 프레임 시간 피크가 크거나 씬 전체가 함께 흔들리면 XR 렌더링/성능 경로를 분리 조사한다.
- Unity 자산 리프레시는 확인했으나, 4× MSAA 적용 뒤의 Quest Link 양안 품질·프레임 시간은 아직 수동 검증하지 않았다. Game View만으로는 양안 떨림을 판정할 수 없다.
- Android/Quest 빌드용 `Mobile_RPAsset`은 성능 측정 전에는 변경하지 않는다. PC Quest Link 결과와 GPU 프레임 시간을 확인한 뒤 별도 결정한다.

## 2026-08-06 후속: 기본 상태에서 표시되는 원본 테이프 메시

- 제보 이미지 `Img/따라다니는 taped_L_1.png`와 씬 YAML을 대조했다. `PPE Body Anchor -> hazmat_suit_on_10 -> taped_L_1`은 `m_IsActive: 1`인 독립 `MeshRenderer`이며, PPE 상태 전환용 `LeftHand_Glove_Suit_Tape`과 다른 오브젝트다.
- 같은 계층의 `taped_R_1`도 기본 활성 상태였다. 두 원본 중복 메시만 `m_IsActive: 0`으로 변경했다.
- `LeftHand_Glove_Suit_Tape`와 `RightHand_Glove_Suit_Tape`의 상태 전환 경로·활성값은 변경하지 않았다.
- 정적 확인: 두 원본의 씬 활성값이 `0`임을 확인했다. Unity Play/Quest에서 기본 상태 미표시와 테이프 Use 후 양손 손모델 전환은 수동 확인이 필요하다.

---

## 문서 안내 (2026-08-06 저녁)

오늘(08-06) 후반 작업의 **종합본**은 위 절
`## 2026-08-06 후반 세션 종합 (`3_PPE_Room_HandTest_scale_0`)`
에 모았다. 개별 후속 절(레이·헬멧·레티클·키보드 Enter 등)은 상세·이력용이며, 최종 상태·미검증·운영 주의는 종합본을 우선한다.

## 2026-08-07 음성 흐름·시작 상태·렌더링 진단 기록

### 대상과 세션 상태

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`로 유지했다.
- Unity Play Mode 중간 종료와 씬 재오픈 과정에서 표시된 `PPEVoiceFlowDirector` 상태, Scene import 경고, `XR Origin (VR)`의 AudioSource 참조 오류를 확인했다.
- 이번 기록의 렌더링 진단은 설정을 바꾸지 않은 상태에서 정적 조사만 수행했다.

### 적용한 변경

- `Window Canvas`, `Modal  Keyboard Canvas`, `Place`의 시작 활성 상태를 유지하도록 확인했다. 키보드 부모는 시작부터 살아 있고, 실제 키보드 표시 루트는 `NameInput` 상태에서만 켜지도록 정리했다.
- Welcome 나레이션은 첫 렌더 프레임 이후 `1`초를 기다린 뒤 시작하도록 `PPEVoiceFlowDirector`를 조정했다. 따라서 헤드셋 화면이 뜨기 전에 음성이 먼저 시작되지 않도록 했다.
- 실제 BGM과 타이틀곡을 `Assets/Audio/BGM`으로 이동했다. `.meta` GUID는 유지했고, 씬별 볼륨 설정은 `Assets/Resources/Audio/Scenes`에 남겼다.
- 대상 씬 BGM 볼륨을 `1`에서 `0.3`으로 낮췄다.
- `PPEVoiceFlowDirector`를 비활성 UI Canvas 아래가 아닌 항상 활성인 `XR Origin (VR)`에 두도록 연결했다. `PPE Body Anchor`도 활성 상태로 복구했다.
- 기본 손 모델은 양손 맨손만 표시하고, 잘못 켜져 있던 `RightHand_Glove_Suit_Tape`는 시작 시 비활성으로 정리했다. 장착 후 손 모델 전환 매핑은 변경하지 않았다.
- 키보드 클릭용 `AudioSource`의 씬 소유 GameObject 참조를 실제 컴포넌트를 가진 `UI Root`로 바로잡았다. 음성 재생용 런타임 AudioSource와 키보드 클릭음을 분리했다.
- `Assets/Editor/PPEVoiceFlowDirectorEditor.cs`를 추가해 Inspector의 각 Voice Step 옆에 `Play`와 `Stop` 버튼을 제공했다. 한 Step에 여러 클립이 있으면 배열 순서대로 미리듣기한다.
- Unity 6000에서 미리듣기 API가 `UnityEditor.AudioUtil`에 있는 것을 확인하고 Editor 미리듣기 호출 경로를 수정했다.

### 정적 확인 및 빌드 확인

- `PPEVoiceFlowDirector`의 상태별 Voice Step, UI 표시 루트, 키보드 제출 이벤트, 텔레포트 이벤트 참조를 대상 씬에서 확인했다.
- BGM 두 곡의 이동 경로와 `.meta` GUID 유지 여부를 확인했다.
- 대상 씬의 시작 Canvas·Place·손 모델·BGM 볼륨 직렬화값을 확인했다.
- Unity 6000용 Editor 미리듣기 API 경로를 확인했다.
- C# Editor/Runtime 빌드는 오류 0개로 확인했으며 기존 경고는 남아 있다.
- `Mobile_RPAsset`의 Render Scale `0.8`, MSAA 비활성, Adaptive Performance 활성, 메인 XR 카메라의 URP 안티앨리어싱 비활성, 활성 균열 LineRenderer 폭 `0.008982447m`를 확인했다.
- 프로젝트 소유 커스텀 셰이더의 Single Pass Instanced 매크로 사용 여부를 정적으로 확인했다.

### 오늘 확인된 렌더링 문제와 판단

- 사선 라인 앨리어싱과 화면 전체의 흐릿함은 Android/Quest 경로의 낮은 Render Scale, MSAA 비활성, 카메라 안티앨리어싱 비활성 조합이 가장 강한 후보다.
- 활성 균열 LineRenderer가 매우 얇아 머리 움직임에 따라 서브픽셀 샘플 위치가 바뀌며 반짝일 가능성이 있다.
- Adaptive Performance의 실제 런타임 해상도 변경, 선과 표면의 깊이 겹침, HMD 재투영·Quest Link 프레임 타이밍은 아직 원인으로 확정하지 않았다.
- 해당 렌더링 진단에서는 설정이나 씬을 추가로 수정하지 않았다. 원인 분리를 위해 Render Scale, MSAA, Adaptive Performance, LineRenderer를 한 번에 하나씩 비교해야 한다.

### 아직 필요한 수동 검증

- Unity Editor에서 씬을 디스크 기준으로 다시 로드한 뒤 Play Mode에서 Welcome→NameInput→키보드 표시 순서를 확인한다.
- Voice Step Inspector의 `Play` 버튼이 실제로 음성을 재생하고 `Stop` 버튼이 중지하는지 확인한다.
- BGM 볼륨 `0.3`과 타이틀곡 재생 경로를 확인한다.
- Quest/OpenXR 양안에서 화면 선명도, 사선 라인, 전체 일렁임을 동일한 위치·머리 움직임으로 비교한다.
- Android/Quest에서 Adaptive Performance만 끈 상태, Render Scale `1.0`, MSAA `2x`를 각각 단독 비교하고 GPU 프레임 시간을 기록한다.
- 균열 LineRenderer를 임시 비활성화하거나 선 폭을 키운 비교로 라인 자체의 반짝임과 장면 전체의 렌더링 문제를 분리한다.

## 2026-08-07 Game View 가상 오른손 컨트롤러 및 HMD 게이트

### 목적과 대상

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` 하나다.
- Quest Link/HMD 연결이 불안정한 동안에도 Game View에서 기존 XR 입력·Interactor 경로를 확인할 수 있도록 XRI 3.4.1의 공식 `XR Device Simulator` 샘플을 사용한다.
- 이는 Game View 테스트 보조 경로이며, Quest Trigger·양안 렌더링·실제 오디오·OpenXR 세션 성공의 증거가 아니다.

### 적용한 구성

- XRI Package Manager의 `XR Device Simulator` 샘플을 프로젝트로 임포트했다. 패키지 원본은 수정하지 않는다.
- `Assets/Editor/RightControllerTestSimulatorSetup.cs`의 `Tools > XR > Add Right Controller Test Simulator`는 열린 대상 씬에 다음 구조를 Unity Undo 경로로 만들거나, 기존 구조를 같은 방식으로 갱신한다. 이 명령은 `SaveScene`을 호출하지 않는다.

  ```text
  Game View XR Test Input Gate
  └─ Right Controller Test Simulator
  ```

- `Right Controller Test Simulator`은 공식 `XR Device Simulator.prefab` 인스턴스다. Screen Space Game View UI는 Edit Mode에 상시 존재하는 씬 오브젝트가 아니라, Simulator가 Play 시작 시 자식으로 생성한다. 따라서 가상 컨트롤러 UI는 Play Mode에서만 보인다.
- 공식 Simulator UI는 내부적으로 좌·우 장치를 모두 포함한다. 이번 테스트 범위는 오른손 컨트롤러 선택·조작이며, 별도 왼손 입력 기능을 프로젝트 코드로 추가하지 않는다.
- `Assets/Scripts/PhysicalHmdSimulatorGate.cs`는 Play 시작 시 Input System에 등록된 `XRHMD`를 조사한다. XRI Simulator가 만드는 `XRSimulatedHMD`는 제외하고, 실제 HMD가 있으면 Simulator 자식을 비활성화하고 없으면 활성화한다.
- 전체 자동 테스트 경로를 끄려면 부모 `Game View XR Test Input Gate` GameObject를 비활성화한다. Inspector의 `Enable Simulator When No Physical Hmd`를 끄면 Gate는 남긴 채 자동 활성화만 중지할 수 있다.
- `Assets/Scripts/ControllerGuideMiniActivator.cs`는 활성 상태인 `Window Canvas`에 붙여 `ControllerGuide_mini`를 Inspector 직접 참조로 할당하는 컴포넌트다. 좌·우 `<XRController>{Hand}/Primary2DAxisClick`을 받아 대상만 활성화하며, 시작 시 가이드를 켜거나 기존 `Scale Toggle` 입력을 변경하지 않는다.

### 클릭 패널 보완

- 공식 `XR Device Simulator UI`의 컨트롤러 그림과 입력 표시는 상태 확인용이며 클릭 가능한 가상 컨트롤러가 아니다. 이를 클릭 입력 수단으로 안내한 것은 잘못이었다.
- 기존 `Tools > XR > Add Right Controller Test Simulator` 명령은 같은 `Right Controller Test Simulator` 안에 `Right Controller Click Panel`을 한 번 생성한다. 이 패널은 Game View 우하단에 표시되며 `ACTIVATE RIGHT`, `TRIGGER`, `GRIP`, `A / PRIMARY`, `B / SECONDARY`, `STICK CLICK`을 제공한다.
- 각 버튼은 Input System `OnScreenButton`으로 가상 Gamepad 제어 값을 내보내고, `Assets/Scripts/RightControllerClickPanelBindings.cs`가 이를 공식 Simulator의 기존 액션에만 추가 바인딩한다. 따라서 버튼 이벤트를 별도 XR 입력 처리로 우회하지 않고 Simulator의 오른손 컨트롤러 생성·입력 상태 경로를 그대로 사용한다.
- 패널과 바인딩은 Simulator 자식에만 있으며, HMD가 감지되면 Gate가 Simulator 전체를 비활성화하므로 실제 HMD 세션에는 생성·입력되지 않는다. 사용자가 테스트 경로 자체를 끄려면 부모 `Game View XR Test Input Gate`를 비활성화한다.

### 입력 결함 보완

- 공식 Simulator 프리팹의 `Mouse Delta`는 기본 FPS 또는 선택된 컨트롤러의 자세를 갱신하고, `Trigger`는 `<Mouse>/leftButton`에도 기본 바인딩돼 있다. 이 기본값을 그대로 두면 화면 패널을 클릭하는 마우스가 시점·컨트롤러와 Trigger까지 함께 조작한다.
- `RightControllerClickPanelBindings`는 테스트 Simulator가 활성인 동안 물리 `<Mouse>/delta`, `<Mouse>/rightButton`, `<Mouse>/leftButton` 바인딩을 런타임 override로 제외한다. 대신 `↑/↓/←/→`를 Simulator의 `Mouse Delta` 액션에 연결한다. `ACTIVATE RIGHT`를 누른 뒤 화살표 키로 오른손 Ray 방향을 조절하며, 마우스는 화면 버튼 클릭과 Stick 드래그만 담당한다. Simulator가 꺼질 때 원래 Mouse 바인딩과 Cursor 상태를 복원한다.
- `Teleport Stick`은 기존 패널의 왼쪽에 생성되는 실제 드래그 스틱이다. `OnScreenStick(<Gamepad>/leftStick)` → Simulator `Axis 2D` → 선택된 오른손 `Primary2DAxis` → 기존 `Teleport Mode` 액션 순서로 연결된다. 위로 끌어 유지하면 Teleport Ray가 켜지고, 놓으면 기존 `OnSelectExited` 처리로 이동한다.
- `ControllerGuideMiniActivator`의 참조와 활성 호출은 씬에 존재한다. 다만 `ControllerGuide_mini`와 표시 자식 `Context`가 각각 Canvas 기준 밖의 X/Y/Z 값(`-1544, 202, 3127` 및 `-130, 429, 1047`)을 가져 표시되지 않을 후보를 확인했다. 기존 설정 명령은 이 정확한 값일 때만 두 RectTransform을 중앙·Z=0으로 복구한다. 다른 Inspector 작성값은 변경하지 않는다.

### 모달 직접 테스트

- `PPEVoiceFlowDirector` Inspector에 `Start At Modal Detail For Testing` 토글을 추가했다. 기본값은 꺼짐이며, 테스트 후 다시 꺼야 한다.
- 토글이 켜진 Play Mode에서는 기존 시작 지연 뒤 `StartModalDetailForTesting()`이 실행된다. 이 진입점은 카드 클릭을 흉내 내지 않고, 기존 `m_InitialScenarioIndex`의 모달을 `ModalDetail` 상태로 정상 표시한다.
- 이때 텔레포트/Voice 이동 게이트는 기존 카드→모달 경로와 같이 닫힌 상태를 유지한다. 모달의 교육/미교육 선택은 기존 이벤트와 이후 흐름을 그대로 사용한다. 따라서 앞단 카드·이름 입력만 건너뛰며, 이동 허용을 강제로 열거나 상태를 이중 전환하지 않는다.

### 충돌 방지 판단

- 공식 XRI `SimulatedDeviceLifecycleManager`는 Simulator가 활성화될 때 `XRSimulatedHMD`를 제외한 다른 `XRHMD` 장치를 제거하는 기본 동작을 가진다. 따라서 HMD 연결/해제를 Play 중에 감지해 Simulator를 계속 켜고 끄는 방식은 사용하지 않는다.
- HMD 판정은 Play 시작 시 한 번만 한다. Link 연결 상태를 바꿨다면 Play Mode를 종료한 뒤 다시 시작한다.
- 기존 XRI Default Input Actions에서 좌·우 `Primary2DAxisClick`은 모두 `Scale Toggle`에도 연결되어 있다. `ControllerGuideMiniActivator`는 이를 소비하거나 해제하지 않으므로, 클릭 시 기존 Scale Toggle 동작도 함께 발생할 수 있다.
- Simulator와 실제 HMD를 동시에 활성화하지 않는 것이 우선이다. HMD 연결 상태의 자동 판정과 수동 Gate 비활성화는 이 충돌을 줄이기 위한 테스트 범위 제한이다.

### 사용 및 수동 검증

1. 대상 씬에서 `Tools > XR > Add Right Controller Test Simulator`를 실행하고, 씬 변경을 보존할 때만 사용자가 저장한다.
2. HMD를 연결하지 않은 상태로 Play를 시작한다. Hierarchy에서 `Game View XR Test Input Gate/Right Controller Test Simulator/XR Device Simulator UI(Clone)` 생성과 Game View UI 표시를 확인한다.
3. Game View 우하단의 `RIGHT CONTROLLER TEST`에서 `ACTIVATE RIGHT`를 한 번 누른 뒤 `↑/↓/←/→`로 오른손 Ray를 marker 쪽으로 맞춘다. 마우스로 `TELEPORT STICK`을 위로 끌어 유지하고 놓아 기존 Teleport Ray/이동 경로를 확인한다. 마우스 클릭은 화면 버튼만 조작하며 XR 자세나 Trigger로 들어가지 않는다.
4. `STICK CLICK`을 눌러 `ControllerGuide_mini`가 Window Canvas 중앙에 활성화되는지 확인한다.
5. 실제 HMD를 연결한 상태로 새 Play를 시작해 `Right Controller Test Simulator`가 비활성으로 남는지 확인한다.
6. HMD가 연결된 실제 Quest/OpenXR 테스트 전에는 `Game View XR Test Input Gate`를 비활성화하고, Simulator UI가 생성되지 않는지 확인한다.
7. 카드 이전 단계 없이 모달만 확인하려면 `PPEVoiceFlowDirector`의 `Start At Modal Detail For Testing`을 켠 뒤 Play Mode를 시작한다. 초기 시나리오 모달이 표시되고 선택 후에는 기존 흐름으로 이어져야 한다. 확인 후 토글을 끄고 일반 흐름으로 되돌린다.

### 검증 상태

- 정적 확인: XRI 샘플 프리팹·입력 액션·Simulator가 생성하는 `XRSimulatedHMD`와 실제 HMD 제거 동작을 패키지 소스에서 확인했다.
- 정적 확인: 현재 생성된 C# 프로젝트 전체 `dotnet build`는 오류 0개다. 이 확인은 C# 참조·문법 확인이며, 화살표 키의 실제 Simulator 자세 반응, Game View 버튼 Pointer 입력, 가상 Gamepad 생성, 모달 직접 진입과 선택 후 상태 전이는 증명하지 않는다.
- Unity Editor 확인: 설정 메뉴 실행, Gate/자식 직렬화, Play 중 UI 생성, 실제 HMD 유무별 활성 전환은 아직 확인하지 않았다.
- Quest/OpenXR 확인: 아직 수행하지 않았다.

## 2026-08-07 오디오 아키텍처 재정리 결정

### 이벤트 기반 연결 원칙

- `AudioManager`는 BGM·Voice·SFX·Ambience Source와 곡별 Clip·Volume·활성 상태, 그리고 `Play`·`Stop`·`Fade` 같은 재생 API만 소유한다. XR Origin, 시나리오 카드, 모달, PPE, 텔레포트의 상태·계층·참조를 찾거나 변경하지 않는다.
- 조건 판단은 해당 기능의 소유자가 담당한다. 예를 들어 시나리오 카드의 선택 완료는 카드 선택 컴포넌트가 판단하고, 그 순간에만 `AudioManager.FadeOutBgm(duration)`을 호출한다.
- 연결 방향은 `기능 컴포넌트 → AudioManager` 단방향이다. AudioManager가 카드·Voice Flow·UI를 역으로 호출하거나 부모 관계를 변경하지 않는다.
- 이 원칙으로 평상시 각 기능은 독립적으로 테스트할 수 있고, 카드 선택·정답 처리·씬 진입처럼 의미 있는 이벤트에서만 필요한 오디오 동작을 요청한다.

### AudioManager Inspector 테스트 요구

- AudioManager Inspector가 이 씬의 오디오 설정 단일 기준이다. BGM·Voice·SFX·Ambience를 섹션 순서로 표시하고, 각 Clip 항목에서 `Play`, `Stop`, `Enabled`, `Volume`을 제공한다.
- Voice Library에는 Enabled 상태인 등록 Voice Clip을 Library 순서대로 연속 미리듣기하는 `Play All Enabled Voice Clips`와 즉시 중지 기능을 제공한다. 이 기능은 Voice Flow 상태·UI·입력을 실행하지 않고 음성 파일 확인만 수행한다.
- Voice Flow에는 Clip이 남아 있지만 AudioManager의 Voice Library가 비어 있거나 일부만 등록된 경우가 확인됐다. AudioManager Inspector의 `모든 소스의 누락 오디오 가져오기`는 현재 씬 기준으로 다음 직렬화된 소스를 다시 읽는다: `AudioManagerSettings`의 BGM/SFX/Ambience, 같은 이름의 `SceneAudioSettings` BGM/Ambience, `PPEVoiceFlowDirector` Step/Repeat Voice, `HandwrittenSignatureSequence` SFX, 그리고 AudioManager가 소유하지 않는 직접 `AudioSource.clip`(loop면 Ambience, 아니면 SFX). 각 대상 Library에 없는 Clip만 추가한다.
- 이 동기화는 기존 ID·Clip·Enabled·Volume 값을 덮어쓰거나 삭제하지 않는다. 새 항목만 해당 소스의 ID·Volume(Voice는 `Enabled=true`, `Volume=1`)으로 생성하며, Undo 후 사용자가 씬을 저장한다.
- `This Scene: Start BGM`은 현재 씬을 처음 시작할 때만 요청하는 BGM이다. 전체 앱 시작 또는 이후 Voice Flow 단계의 시작으로 해석하지 않으며, Inspector에서 이 의미와 BGM ID·지연 시간을 명시한다.
- Voice Flow는 음성 순서와 상태 전이만 보유한다. 실제 Voice Clip의 재생 허용, 개별 볼륨, Inspector 미리듣기는 AudioManager의 Voice Library에서 관리한다.
- 기존 SceneAudioSettings·AudioManagerSettings·Voice Flow에 있던 Clip 참조는 먼저 읽어 목록으로 가져오되, 이미 AudioManager Inspector에서 작성한 Clip·Volume·Enabled 값은 이후 설정 실행으로 덮어쓰지 않는다.

### 2026-08-07 적용: 전체 오디오 동기화와 Voice Flow 소유 관계

- 적용 코드: `Assets/Editor/AudioManagerEditor.cs`의 `모든 소스의 누락 오디오 가져오기` 버튼과 `Assets/Editor/PPEVoiceFlowSetup.cs`의 기존 Voice Flow 설정 메뉴를 수정했다.
- 근본 원인: Voice, BGM, SFX, Ambience의 Clip 참조가 Voice Flow·레거시 설정·씬 설정·개별 시퀀스처럼 여러 직렬화 소유자에 흩어져 있어 AudioManager Library에서 누락될 수 있었다. 또한 Voice Flow는 항상 활성 상태를 위해 XR Origin에 임시 배치돼 있었으나, XR 기능을 소유하지 않는다.
- 영향 범위: 동기화 버튼은 현재 열린 같은 씬의 직렬화된 Clip 참조만 읽는다. 프로젝트 전체 Audio 폴더나 패키지 샘플을 추측해 추가하지 않는다. 계층 변경은 `PPE Voice Flow`와 그 Director만 AudioManager 자식으로 이동하며, XR Origin·AudioSource·UI Transform·입력 참조는 변경하지 않는다.
- 정적 확인: Director는 부모 경로를 읽지 않고, AudioManager는 자식 Transform 또는 활성 상태를 변경하지 않는다. C# 빌드는 오류 0개다.
- 수동 확인: `Tools > PPE > Voice Flow > Setup HandTest Scale 0` 실행 뒤 `AudioManager/PPE Voice Flow` 계층과 일반 Voice Flow·모달 직접 테스트를 Play Mode에서 확인한다. 전체 Clip 동기화는 AudioManager Inspector에서 실행 뒤 추가된 항목·개별 Volume·Enabled를 검토하고, 필요한 경우 저장한다.

### Unity 열린 씬과 디스크 변경의 운영 규칙

- Unity Editor가 씬을 열고 있는 동안 외부 도구·배치 모드·직접 YAML 편집으로 같은 `.unity` 파일을 저장하지 않는다. Unity 메모리에 있는 씬 상태와 디스크 상태가 서로 덮어써 사용자의 중간 저장 또는 도구 변경이 소실될 수 있다.
- 에이전트는 C# 및 Editor 메뉴 코드만 디스크에 작성한다. Unity가 컴파일을 마친 뒤, 실제 씬 변경은 사용자가 현재 열린 씬에서 메뉴를 실행해 Unity의 Undo 경로로 적용한다.
- Editor 설정 도구는 `SaveScene`을 호출하지 않는다. 변경 후 씬은 dirty 상태로 두고, Hierarchy·Inspector·검증 결과를 확인한 사용자가 원하는 시점에 저장한다.
- 리프레시 또는 씬 재열기 전, 사용자의 수동 변경이 없다면 도구가 만든 미검증 변경은 `Don't Save`로 버린다. 다만 이미 자동 저장된 변경은 재열기만으로 되돌아가지 않으므로, 원인을 직접 복구한 뒤 수동 저장해야 한다.

### 확인된 문제

- 이전 Luna 작업은 비활성 `Modal  Keyboard Canvas` 아래에 있던 `PPEVoiceFlowDirector`가 실행되지 않는 문제를 `XR Origin (VR)`으로 옮겨 우회했다. 하지만 나레이션 진행기는 HMD 추적·입력·카메라의 소유자가 아니므로 XR Origin에 둘 구조적 이유가 없다.
- `AudioManager`는 런타임에 자동 생성되고 BGM·Voice·SFX·Ambience `AudioSource`도 동적으로 만든다. 따라서 테스트 중 Hierarchy와 Inspector에서 실제 재생 Source, Clip, Volume, Mute, `isPlaying`을 바로 확인할 수 없다.
- BGM은 `SceneAudioSettings.bgm`의 직접 Clip 참조로 씬 시작을 제어하고, 별도의 `AudioManagerSettings.bgm` 라이브러리도 존재한다. 두 설정의 책임·우선순위가 명확하지 않아 라이브러리 토글, 씬 BGM, 페이드, 출력 문제를 한 경로로 추적하기 어렵다.
- 위 구조는 C# 컴파일과 YAML 참조 확인만으로는 실제 오디오 출력 성공을 보장하지 않는다. Unity Play Mode와 Quest/OpenXR 출력 검증이 없는 상태에서 정상으로 보고하면 안 된다.

### 합의된 방향

- 현재의 긴 개발·테스트 기간에는 각 테스트 씬에 항상 활성인 최상위 `AudioManager` 오브젝트를 직렬화해 둔다. 이 오브젝트는 월드 위치와 무관한 관리 루트이며 Renderer를 두지 않는다.
- BGM, Voice, UI/SFX, Ambience `AudioSource`를 `AudioManager`의 자식으로 명시적으로 배치하고, 각 Source의 Clip·Volume·Mute·Spatial Blend·출력 Mixer를 Inspector에서 확인 가능하게 한다.
- BGM·Voice·UI 클릭음은 2D Source로 유지한다. PPE·태블릿·기계 등 위치가 의미 있는 효과음만 해당 월드 오브젝트의 3D Source를 사용한다.
- `PPEVoiceFlowDirector`는 XR Origin이 아니라 이 씬 전용 `AudioManager/PPE Voice Flow` 자식에 둔다. Director는 상태 전이만 소유하고 재생 Source를 새로 만들지 않는다. `AudioManager`는 자식 Transform·활성 상태를 변경하지 않고, Director도 부모 경로를 읽지 않으므로 이 계층은 관리 목적의 소유 관계만 표현한다.
- `Tools > PPE > Voice Flow > Setup HandTest Scale 0`은 기존 `PPE Voice Flow`를 AudioManager 자식으로 Unity Undo 경로에서 이동한다. AudioManager가 없는 경우에는 이동하지 않고 명확한 오류로 멈춘다. 자동 저장하지 않는다.
- 추후 앱 진입 씬을 구축할 때만 단일 전역·`DontDestroyOnLoad` AudioManager로 전환한다. 그 전까지 테스트 씬 Manager는 씬 전용으로 유지해 이전 씬의 BGM과 Coroutine이 다음 테스트를 오염시키지 않게 한다.

### 아직 적용하지 않은 사항과 검증

- 이번 기록은 설계 결정을 남긴 것이며, 씬 Hierarchy·AudioSource 구성·`DontDestroyOnLoad` 정책을 아직 변경하지 않았다.
- 전환 후에는 Unity Play Mode에서 BGM, Voice, 키보드 클릭음, PPE SFX를 각각 단독 재생하고, 각 Source의 `isPlaying`과 출력 장치를 기록한다.
- Quest/OpenXR에서도 같은 순서로 실제 청취와 양안 세션 유지 여부를 확인한다. Game View 또는 C# 빌드만으로 오디오 완료를 판정하지 않는다.
