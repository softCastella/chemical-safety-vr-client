# 2026-08-22 PPE 작업 커밋 후보 및 후속 검증 계획

## 목적

2026-08-22 현재 `3_PPE_Room_Train_Test_mask_locomotion.unity` 작업분을 커밋하기 전에 변경 내용을 정리하고, Unity Editor 및 Quest/OpenXR에서 재현해야 할 후속 테스트를 기록한다.

대상 씬은 `Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity` 하나로 한정한다. 포스터 해상도 업스케일 작업은 검토만 했으며 이번 변경에는 포함하지 않는다.

## 적용된 변경

### PPE 착용·테이프 사용 조건

- 니트릴 내부장갑 없이 외부장갑을 사용하면 사용을 거절하고 `Wrong Answer` SFX와 EDU 207 보이스를 재생하도록 연결했다.
- 테이프는 기존 의도대로 사용 처리된 외부장갑 또는 장화 한쪽이 있으면 사용할 수 있도록 유지했다.
- 니트릴 내부장갑만 착용한 상태에서는 테이프 사용을 거절하고 EDU 201 음원을 재생하도록 연결했다.
- 기존 201 OGG는 제거하고 201 MP3 및 207 MP3를 대상 씬의 음원 참조에 추가했다.

### 테스트 모드 작업계획 선택

- 테스트 모드 진입 후 랜덤 선택을 사용하지 않고 `밀폐공간 대응 PPE착용` 또는 `누출사고 대응 PPE착용`을 선택하도록 작업계획 UI를 추가했다.
- 테스트 작업계획 선택에 필요한 씬 참조를 연결하고, 기존 테스트 모드의 이동·종료 흐름은 유지했다.

### 로코모션

- `PPEConfigurableDynamicMoveProvider`를 추가하고 대상 씬의 `Move` 컴포넌트에 연결했다.
- Inspector에서 `Input Sensitivity`, `Acceleration`, `Deceleration`을 조절할 수 있도록 했다.
- 현재 작성값은 `Input Sensitivity = 1`, `Acceleration = 8`, `Deceleration = 12`, 기존 `Move Speed = 1`이다.
- 기존 `PPEVoiceFlowDirector` 및 `PPEIdleLocomotionAnimator` 참조를 새 이동 컴포넌트에 연결했다.

### EXIT 마커·라벨

- `Teleport_3_Exit`에 `ExitText.png` 이미지와 전용 이미지 머티리얼을 추가했다.
- EXIT 이미지와 위치 마커가 같은 펄스 기준을 공유하도록 연결했다.
- 점멸은 알파만 변경하고, EXIT의 크기·위치·로테이션은 작성값을 유지하도록 했다.
- EXIT 전용 점멸 컴포넌트에서 스케일 보간 처리를 제거했다.

### 서명 머티리얼

- 플레이어·감시인·믹서 감시인 서명 머티리얼의 잉크 강조·확장값을 기본에 가깝게 조정했다.
- 실제 서명 UI의 겹침 및 번쩍임 제거 여부는 Unity Editor/Quest에서 후속 확인이 필요하다.

## 정적 확인

- `dotnet build Assembly-CSharp.csproj --no-restore` 완료.
- 오류 0개, 기존 API 사용 관련 경고 26개.
- 포스터 파일은 확인만 했으며 이미지·머티리얼·씬 참조를 변경하지 않았다.

## 후속 테스트 계획

### 1. 외부장갑·니트릴·음원

1. 초기화 후 니트릴을 착용하지 않고 왼쪽·오른쪽 외부장갑을 각각 사용한다.
   - 외부장갑 사용이 거절되는지 확인한다.
   - `Wrong Answer` SFX 후 EDU 207 보이스가 재생되는지 확인한다.
2. 같은 쪽 니트릴을 먼저 착용한 뒤 외부장갑을 사용한다.
   - 정상 장착되는지 확인한다.
3. 음원 순서가 중복 재생되거나 이전 실패 음원과 겹치지 않는지 확인한다.

### 2. 테이프 선행조건

다음 세 경우를 각각 초기화해서 테스트한다.

| 상태 | 기대 결과 |
| --- | --- |
| 니트릴 내부장갑만 착용 | 테이프 거절, EDU 201 재생 |
| 외부장갑 한쪽만 착용 | 테이프 사용 가능 |
| 장화 한쪽만 착용 | 테이프 사용 가능 |

추가로 외부장갑과 장화를 모두 착용한 경우에도 정상 사용되는지, 니트릴 복사본이 외부장갑 조건으로 잘못 인식되지 않는지 확인한다.

### 3. 테스트 모드 작업계획

1. 모드 선택 모달에서 테스트 모드를 선택한다.
2. 랜덤 항목이 표시되지 않고 두 작업계획 버튼이 표시되는지 확인한다.
3. `밀폐공간 대응 PPE착용`을 선택해 해당 시나리오로 진입하는지 확인한다.
4. 초기화 후 `누출사고 대응 PPE착용`을 선택해 해당 시나리오로 진입하는지 확인한다.
5. 두 시나리오를 연속 선택했을 때 이전 선택이 남거나 같은 시나리오가 강제되지 않는지 확인한다.
6. 작업계획 뒤로가기와 테스트 모달 닫기가 정상인지 확인한다.

### 4. 로코모션·Footstep

1. Inspector에서 `Move Speed`, `Input Sensitivity`, `Acceleration`, `Deceleration`을 변경한다.
2. 스틱 입력 시작 시 즉시 최대 속도로 튀지 않고 가속되는지 확인한다.
3. 스틱을 놓았을 때 감속 후 멈추는지 확인한다.
4. 이동 중 `Foot Step`이 재생되는지, 정지 후에도 계속 재생되지 않는지 확인한다.
5. 이동 방향 전환, 짧은 입력 반복, 텔레포트 직후 이동에서 버벅임이 없는지 확인한다.
6. PC OpenXR와 Quest/OpenXR에서 속도와 발걸음 음원이 동일하게 동작하는지 확인한다.

### 5. 체크리스트 초기 상태 반영

1. 태블릿 체크리스트가 최초 표시되기 전에 PPE 일부를 먼저 착용한다.
2. 태블릿을 체크했을 때 이미 착용한 PPE가 체크된 상태로 표시되는지 확인한다.
3. 체크리스트가 비활성 상태로 남지 않고, 패널 표시·갱신·완료 상태가 모두 정상인지 확인한다.
4. 교육 모드와 훈련 모드에서 각각 확인한다.

### 6. 서명 UI

1. 플레이어 서명, 감시인 서명, 믹서 감시인 서명을 각각 실행한다.
2. 기본 밝기와 비교해 과도하게 진하거나 번쩍이지 않는지 확인한다.
3. 감시인 3겹 서명이 겹쳐 보이지 않는지 확인한다.
4. 서명 완료 전후로 알파가 반복해서 깜빡이거나 재생 시점마다 밝기가 변하지 않는지 확인한다.

### 7. EXIT 마커·라벨

1. `Teleport_3_Exit`의 기존 위치·로테이션과 EXIT 이미지의 작성값이 유지되는지 확인한다.
2. 위치 마커와 EXIT가 같은 순간에 나타나고 사라지는지 확인한다.
3. 점멸 중 EXIT 크기가 작아지거나 커지지 않고 고정되는지 확인한다.
4. EXIT 글자가 180도 뒤집히지 않는지 확인한다.
5. Game View가 아닌 Quest 양안에서도 마커와 EXIT의 위치·밝기·점멸 위상이 일치하는지 확인한다.

## 커밋 전 별도 확인 항목

- `Assets/UIs/Facilities/PPE_Room/Models.meta` 삭제가 이번 커밋에 포함되어야 하는지 확인한다.
- 201 OGG 삭제 및 201 MP3 교체가 의도한 변경인지 확인한다.
- `LiberationSans SDF - Fallback.asset`의 대규모 직렬화 변경이 필요한지 확인한다.
- 대상 씬에 추가된 테스트 작업계획·EXIT 오브젝트 외에 불필요한 계층·Transform 변경이 없는지 확인한다.
- Unity Editor에서 씬 저장 후 `git diff --stat`와 씬 FileID/계층을 다시 확인한다.

## 완료 기준

- 정적 빌드 오류가 없다.
- 위 1~7번 Unity Editor 테스트가 모두 통과한다.
- Quest/OpenXR 양안에서 로코모션, 음원, 체크리스트, 서명, EXIT 마커를 확인한다.
- 실패한 항목은 원인·재현 절차·수정 범위를 이 문서에 추가한 뒤 커밋한다.

## 2026-08-23 후속: PPE 음성·누출 분기·체크 상태 회귀

### 확정한 요구사항

- 니트릴 내부장갑만 착용한 상태에서는 테이프 사용을 거절하고 EDU 201을 재생한다.
- 외장 고무장갑 또는 고무장화의 좌·우 중 어느 한쪽이라도 착용했다면 테이프 사용을 허용하고, 한 번의 사용으로 네 부위의 테이핑 상태를 모두 기록한다.
- 다만 테이프 시각물은 해당 외장장갑 또는 장화를 실제 착용한 쪽에만 표시한다. 미착용 쪽은 나중에 해당 장비를 착용한 뒤 표시한다.
- 테이프 잡기 교육 음성은 교육 세션의 최초 정상 테이프 Grab에서 한 번만 재생한다.
- 정상 방호복을 이미 착용한 뒤 다른 방호복을 사용하려 하면 새 방호복의 하자 여부보다 `m_HazmatAlreadyEquippedVoice`를 우선 재생한다.
- 누출 작업계획에서 안전대(`TacticalHarness`)는 착용 승인하지 않는다.
- 새 작업계획을 시작할 때 이전 작업계획의 교육 체크 표시를 승계하지 않는다.

### 근본 원인과 적용 변경

- `OnTapeGrabbed()`에 재생 소비 상태가 없어 테이프를 다시 잡을 때마다 `m_TapeGrabVoice`가 반복됐다. `m_TapeGrabVoicePlayed`와 세션 초기화를 추가했다.
- `ResolveUseChoice()`가 하자 판정을 먼저 끝내 기존 방호복 착용 상태를 검사하지 못했다. 방호복 중복 착용 사전 검사를 하자 판정보다 앞에 두고, 다른 PPE의 기존 하자 판정 순서는 유지했다.
- `PPE_A_Backplate`의 `voiceFlowDirector` 씬 참조가 비어 있어 누출 작업계획 불일치 검사를 우회했다. 기존 씬 `PPE Voice Flow` 참조를 직렬화해 연결했다.
- `PPEEducationWearChecklist.approvedItemTypes`가 작업계획 전환 때 남아 새 누출 시나리오의 공통 항목이 이미 체크된 것처럼 표시됐다. `ActiveWorkPlan` 변경을 감지해 승인 기록과 양쪽 체크 표시를 초기화한다.
- `PPEEducationWearChecklist.OnEnable()`이 Edit Mode에서도 표시 상태를 적용할 수 있었다. Play Mode가 아닐 때는 구독·표시 갱신을 시작하지 않도록 막아 씬 작성 UI 활성값을 보존한다.
- 테이프 선행조건은 기존 `HasAnyTappableEquipment()` 정책을 유지한다. 니트릴 타입은 포함하지 않으며 외장 고무장갑·고무장화 네 타입만 검사한다.

### 영향 범위

- 변경 대상은 PPE Use 우선순위, 테이프 Grab 음성 소비 상태, 교육 체크 기록, 로코모션 씬의 안전대 패널 참조다.
- 장비별 테이프 시각물의 `IsItemUsed(...)` 조건, 착용 애니메이션, UI 작성값, XR 입력 경로 및 작업계획 필수 배열은 변경하지 않았다.

### 완료한 검증

- Unity 스크립트 컴파일 성공, Play Mode 아님을 확인했다.
- Unity Console의 Error/Exception/Assert가 0건임을 확인했다.
- `Tools > PPE > Validate Locomotion PPE Regressions` 하네스를 추가하고 실행해 다음을 확인했다.
  - 니트릴 내부장갑만으로는 테이프 선행조건 불충족
  - 외장 고무장갑 또는 고무장화 한쪽이면 선행조건 충족
  - 한 번의 테이프 사용 상태가 네 부위를 모두 포함
  - 테이프 시각물은 실제 착용된 부위만 활성
  - 테이프 잡기 교육 음성은 세션당 한 번이며 새 세션에서 재허용
  - 누출 필수 배열에 `TacticalHarness`가 없고 `PPE_A_Backplate`가 Director를 참조
  - 작업계획 변경 시 이전 체크 승인 기록 초기화
  - 방호복 중복 착용 검사가 후보 방호복 하자 판정보다 먼저 실행
- 메뉴 실행 결과를 Console 필터에 의존하지 않도록 `PASS/FAIL` Editor 팝업을 추가했다.
- 사용자가 `Tools > PPE > Validate Locomotion PPE Regressions`를 직접 실행해 `PASS` 팝업 표시를 확인했다.
- 이 PASS는 코드·씬 배선·상태 정책에 대한 Editor 검증이며 실제 Quest 음성 출력이나 양안 렌더링 성공을 의미하지 않는다.

### 남은 수동 검증

- Unity Play Mode에서 니트릴만 착용 후 테이프 Use 시 EDU 201과 거절 상태를 확인한다.
- 외장장갑 또는 장화 한쪽 착용 후 테이프를 사용하고, 미착용 세 부위의 테이프 시각물이 꺼져 있는지 확인한다. 이후 각 장비를 착용할 때 해당 시각물만 켜지는지 확인한다.
- 정상 방호복 착용 후 하자 방호복 Use 시 하자 음성 대신 이미 착용 음성이 출력되는지 확인한다.
- 누출 작업계획 최초 진입에서 안전대가 거절되고 체크리스트가 모두 미체크로 시작하는지 확인한다.
- Quest/OpenXR 양안·실제 출력 장치 검증은 아직 완료하지 않았다.

## 2026-08-24 후속: 몸 내부 접촉 착용·누출 안전대·PPE 시작 보이스와 SFX

### 변경 전 판단 기록

1. 기존 Inspector/씬 작성값은 보존한다. `bodyAttachDistance`, 몸 부위별 오프셋, PPE별 `useSfxId`, 작업계획 필수 배열은 변경하지 않고 착용 거리의 측정 대상을 보완한다.
2. 착용 승인 상태의 단일 소유자는 `PPEActionPanelController.ResolveUseChoice()`와 `PPEVoiceFlowDirector.CanApprovePpeUse()`이며, 작업계획은 `PPEVoiceFlowDirector.ActiveWorkPlan`이 소유한다.
3. 입력 경로는 `XR Controller Trigger → XRBaseInputInteractor.activateInput → 선택 중 XRGrabInteractable → PPEInspectionState → PPEActionPanelController.IsBodyProximityActivateAttempt() → ResolveUseChoice()`이다. 몸 접촉 판정은 선택 중인 PPE의 작성된 Grab Collider와 기존 손/컨트롤러 probe를 함께 검사한다. 이 경로는 UI Raycaster를 사용하지 않는다.
4. 필수 참조가 없을 때 런타임 자동 생성·자동 수리는 추가하지 않는다. 기존 명확한 오류 경로를 유지하고, 씬 음성 참조는 Unity Editor 직렬화로 명시적으로 복구한다.
5. 영향 소비자는 PPE 몸 근접 착용, 누출 작업계획 불일치 거절, Wrong Answer SFX, EDU 205 보이스, `ppe_area`의 EDU 002→003 순서다. 텔레포트·모달·PPE Grab 레이어·거울·UI 작성값은 보존한다.
6. 변경 전 기준은 현재 로코모션 씬의 `bodyAttachDistance=0.25`, 손 Transform 중심 거리 판정, 누출 필수 목록의 `TacticalHarness` 제외, 안전대 패널의 Director/Wrong Answer 연결, `ppe_area` 첫 clip의 유실 GUID다. 변경 후에는 회귀 하네스와 Play Mode에서 각각 비교한다.
7. 검증 수준은 정적 확인과 Unity Editor/Play Mode까지 수행한다. Quest/OpenXR 실제 컨트롤러 접촉감, 양안, 헤드셋 출력은 별도 수동 검증으로 남긴다.

### 이번 변경이 대응하는 사용자 요청과 보존 동작

- 대응 요청: 잡은 PPE가 몸 안쪽까지 닿았을 때도 Trigger 착용을 인정하고, 누출 작업계획에서 안전대를 거절하며 SFX를 확인하고, EDU 002 시작 보이스를 복구하고, 시나리오 불일치 PPE의 SFX를 점검한다.
- 보존 동작: PPE를 Grab한 상태에서 Trigger를 눌러야 하며, 오염 PPE·작업계획 불일치·착용 선행조건은 계속 거절한다. 기존 PPE별 착용음과 Correct/Wrong 피드백, 교육/훈련/테스트별 피드백 분기는 유지한다.

### 근본 원인과 적용 변경

- 몸 근접 착용은 기존에 몸 기준점과 선택 중 Interactor(손/컨트롤러) Transform 중심만 비교했다. 따라서 PPE Collider가 몸 기준점을 감싸거나 몸 안쪽에 닿아도 손 중심이 `bodyAttachDistance` 밖이면 착용 시도가 무시될 수 있었다.
- 기존 손 중심 판정은 유지하고, 선택 중 `XRGrabInteractable.colliders`의 `Collider.ClosestPoint`와 몸 기준점 또는 장화용 몸 세그먼트 사이 거리도 함께 검사하도록 변경했다. 몸 기준점이 PPE Collider 내부에 있으면 거리가 0이므로 착용 범위로 인정한다. 작성된 `bodyAttachDistance=0.25`와 모든 몸 부위 오프셋은 보존했다.
- `ppe_area` 첫 clip은 존재하지 않는 GUID `6e4b5132bdbffdd4488db46fa20891f6`를 가리키고 있었다. 실제 `Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_002_PPE_Start.mp3`를 Unity 직렬화로 연결하고, Editor 설정 도구의 이전 `Assets/Audio/Voice/PPE/...002-2...` 경로도 현재 자산 경로로 수정했다.
- 시나리오 불일치 거절은 `useSfxId`가 아니라 모든 관련 패널의 `wrongFeedbackSfxId=Wrong Answer`를 사용한다. 따라서 고글·페이스실드·마스크처럼 장비별 착용음이 비어 있어도 시나리오 불일치 SFX는 재생된다.
- 밀폐공간에서 불일치 가능한 SCBA·고글·페이스실드와 누출에서 불일치 가능한 안전대·방독마스크·SCBA를 전수 확인했다. 모든 활성 패널이 씬 Director를 참조하고 활성 `Wrong Answer` SFX를 사용한다.

### 영향 범위

- 변경: `PPEActionPanelController`의 몸 근접 거리 판정, 로코모션 회귀 하네스, `PPEVoiceFlowSetup`의 현재 음원 경로, 로코모션 씬 `ppe_area` 첫 clip 참조.
- 보존: Grab/Trigger 입력 소유권, PPE Collider·Layer, 작업계획 필수 배열, PPE별 착용음, 피드백 UI 작성값, 텔레포트·모달·거울 동작.

### 완료한 검증

- Unity 스크립트 컴파일 성공과 Console Error/Exception/Assert 0건을 확인했다.
- 회귀 하네스가 다음을 포함해 PASS했다.
  - 몸 기준점이 BoxCollider 내부에 있는 경우와 몸 세그먼트가 Collider 내부를 통과하는 경우를 착용 범위로 인정
  - Collider에서 범위 밖인 점은 거절
  - 누출 필수 배열에서 `TacticalHarness` 제외 및 누출 정책 함수의 안전대 거절
  - 작업계획 불일치 가능한 모든 PPE 패널의 Director, `Wrong Answer` id, AudioManager clip/enabled/volume 연결
  - `ppe_area`가 EDU 002 PPE Start 뒤 EDU 003 Tablet을 참조
- Play Mode에서 누출 안전대 `CanApprovePpeUse`가 교육·훈련·테스트 모두 `false`이고 세 모드 모두 SFX Source가 재생 상태임을 확인했다.
  - 교육: `Wrong Answer` + `4_VO_PPE_EDU_205_PPE_forScenario`
  - 훈련: `Wrong Answer` + 기존 `4_VO_PPE_TRAIN_003_WrongButton`
  - 테스트: `Wrong Answer`, 보이스 없음(기존 테스트 정책 유지)
- Play Mode의 실제 `PpeArea` 단계에서 `4_VO_PPE_EDU_002_PPE_Start`가 먼저 재생되고 완료 뒤 `4_VO_PPE_EDU_003_Tablet`이 이어지는 것을 AudioSource로 확인했다.
- Play Mode 진단 뒤 런타임 상태를 저장하지 않고 디스크 씬을 다시 열어 `dirty=false`를 확인했다.

### 아직 필요한 수동 검증

- Quest/OpenXR에서 실제 PPE 메시가 몸 안쪽에 닿고 손 중심은 기존 반경 밖인 자세로 Trigger를 눌러 착용 승인되는지 확인한다.
- Quest 출력 장치에서 `Wrong Answer`, EDU 205, EDU 002→003의 실제 음량·공간감·끊김을 확인한다.

## 2026-08-25 후속: EDU 001·002·003 교체 음원 연결

### 변경 전 판단 기록

1. 사용자가 교체한 음원과 `.meta`를 보존한다. `001`, `002`는 기존 경로와 GUID가 유지되므로 씬
   참조를 다시 작성하지 않고, 이름과 GUID가 바뀐 `003` 참조만 Unity 직렬화로 교체한다.
2. 교육 시작 음성 순서의 단일 소유자는 `PPEVoiceFlowDirector.m_VoiceSteps`이다.
3. 재생 경로는 `education_selected → EDU 001`, `PpeArea 진입 → EDU 002 → EDU 003 →
   AudioManager Voice AudioSource`이다.
4. 누락 음원을 런타임 검색이나 fallback으로 수리하지 않는다. Editor 회귀 검증에서 정확한 파일
   참조와 순서를 검사한다.
5. 영향 소비자는 교육 모드의 001·002·003 음성뿐이다. Training/Test 음성, PPE 선택·착용, UI,
   텔레포트와 Quest 입력은 변경하지 않는다.
6. 변경 전 기준에서 001과 002는 새 파일을 기존 GUID로 정상 참조하지만, `ppe_area` 두 번째 clip은
   삭제된 `4_VO_PPE_EDU_003_Tablet.mp3`의 GUID를 가리킨다.
7. 변경 후 정적 참조, 회귀 하네스, Unity Play Mode의 실제 `001 → 002 → 003` AudioSource 상태를
   구분해 확인한다. Quest/OpenXR 출력 장치 청취는 별도 수동 검증이다.

### 적용 및 검증 결과

- `001`과 `002`는 교체 전과 같은 경로와 GUID를 유지하므로 기존 씬 참조가 새 파일 내용을 그대로
  사용한다.
- `ppe_area`의 삭제된 `4_VO_PPE_EDU_003_Tablet.mp3` 참조를 새
  `4_VO_PPE_EDU_003_Table.mp3`로 Unity 직렬화하여 교체했다.
- `PPEVoiceFlowSetup`과 로코모션 회귀 하네스의 001·003 자산 경로를 현재 파일명과 폴더로 갱신했다.
  회귀 하네스는 `education_selected=001`, `ppe_area=002→003`의 정확한 개수·순서를 검사해 PASS했다.
- 새 Play Mode에서 시작 흐름과 분리한 뒤 작성된 교육 모드·밀폐공간 버튼을 사용해 AudioSource 상태를
  기록했다. `001`, `002`, `003_Table`이 차례로 모두 `Loaded`, `isPlaying=True`였고 각 클립 종료 뒤
  다음 음원으로 진행했다.
- 사용자가 현재 미재생이 정상이라고 확인한 `4_VO_PPE_EDU_010_Mask.mp3`는 이번 VoiceStep에 추가하거나
  참조를 변경하지 않았다. 위 001→002→003 전체 재생 기록에서도 EDU 010 재생은 0회였다.
- Quest/OpenXR 실제 출력 장치 청취는 수행하지 않았다.

## 2026-08-27 후속: 이동 입력 유지형 발소리 루프

### 근본 원인

- `Foot Step.ogg`는 약 `4.225초` 길이인데 이동 입력 소유자가 `0.4초`마다 공용 SFX Source에
  `PlayOneShot`을 호출했다. 따라서 입력이 빠르게 반복되거나 계속 유지되면 같은 클립이 여러 겹
  재생됐다.

### 적용한 변경

- 발소리는 이동 속도와 무관하게 입력이 0이 아닌 동안 하나의 클립만 정속 루프한다.
- 이동 입력이 0이 되는 즉시 루프를 정지하고 clip을 해제한다.
- 다른 정답·오답·착용 SFX를 끊지 않도록 공용 SFX Source와 분리된 씬 작성
  `AudioManager/Footsteps` AudioSource를 사용한다.
- 필수 Source가 없으면 런타임에서 자동 생성하지 않고 한 번의 명확한 오류를 기록한다.

### 검증

- Unity Editor 정적 검증: 전용 Source 참조, 공용 Source와의 분리, `playOnAwake=false`, `pitch=1`,
  입력 유지 시 루프 시작과 입력 해제 시 정지 경로가 PASS했다.
- 현재 씬에는 기존 미저장 변경이 있어 전용 Source 적용 뒤 자동 저장하지 않았다.
- Quest/OpenXR 확인: 실제 스틱 유지·빠른 재입력·해제 시 청감과 즉시 정지는 HMD에서 수동 확인이 필요하다.
