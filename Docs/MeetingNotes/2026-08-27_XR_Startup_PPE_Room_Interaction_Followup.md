# XR 기동 및 PPE 룸 상호작용 후속 회의록

- 일자: 2026-08-27
- Unity: `6000.4.8f1`
- XR: Meta Quest Link / OpenXR
- 시작 씬: `Assets/Scenes/0_App.unity`
- 주요 작업 씬: `Assets/Scenes/3_PPE_Room_3mode_loco.unity`
- 범위: HMD 기동 동기화, PPE 진열장 충돌, 선택 Marker, 발소리, 태블릿 체크,
  거울 관찰

## 회의 목적

실기 테스트에서 확인된 HMD 기동 지연과 PPE 룸 상호작용 문제를 최근 변경, 씬 직렬화,
실행 환경으로 분리해 조사한다. 사용자와 합의한 동작만 변경하고, 실제 Quest/OpenXR 검증이
필요한 항목을 정적·Unity Editor 검증과 구분해 다음 테스트 기준으로 남긴다.

## 오늘 확정한 요구사항

1. 앱 시작 시 HMD가 렌더링 준비되기 전에 Title 게임 화면과 음원이 먼저 진행하면 안 된다.
2. 메탈 셸브 베이스 Collider는 눕힌 가로 형태를 유지한다.
3. 플레이어는 셸브 상판과 헬멧 쪽 바깥 기둥, Wooden Crate 02를 통과하면 안 된다.
4. PPE 선택 판정은 발광 Marker 시각 크기와 분리해 줄이되, 발광 시각물 크기는 유지한다.
5. 발소리는 이동 속도와 무관하게 입력이 유지되는 동안 한 클립만 정속 루프하고, 입력이
   끊기면 즉시 멈춘다.
6. 태블릿을 잡고 Trigger로 체크하는 순간 체크리스트 전체가 바로 표시되어야 한다.
7. 거울 관찰이 시작된 뒤 고개를 돌려도 게이지는 멈추지 않아야 한다. 단, 작성된 관찰 반경을
   벗어나면 중단하는 거리 조건은 유지한다.

## 변경 전 판단 기록

1. **Inspector/씬 작성값 보존**
   - 메탈 셸브의 눕힌 베이스 Collider, PPE와 Marker Transform, 발광 Renderer, 태블릿 문서와
     서명 시각물, 거울 게이지 UI 작성값을 보존한다.
   - 사용자 요청에 직접 해당하는 Collider 반경·추가 Collider·전용 AudioSource·재생 상태만
     변경한다.
2. **단일 상태 소유자**
   - 앱 시작 준비: `AppSceneBootstrap`
   - Title 음원 시작: `TitleSplashController`와 씬 `AudioManager`
   - 이동 입력과 발소리: `PPEConfigurableDynamicMoveProvider`
   - 태블릿 체크: `PPETabletChecklistController`
   - 체크/서명 reveal: `HandwrittenSignatureSequence`
   - 거울 관찰: `PPEFinaleController`
   - 셸브/환경 Collider 작성: `PPERoomEnvironmentCollisionSetup`
3. **입력 전체 경로**
   - PPE Grab: 손 Near interactor → `SphereInteractionCaster` → Layer 6 Trigger
     `XR Item Marker_small` → `XRGrabInteractable`
   - 이동/발소리: 좌·우 Move 입력 → `PPEConfigurableDynamicMoveProvider` → 전용 Footsteps
     `AudioSource`
   - 태블릿: 잡은 Tablet → 좌·우 Trigger 또는 XRI Activate →
     `PPETabletChecklistController` → `HandwrittenSignatureSequence`
4. **누락 참조 정책**
   - 필수 Footsteps Source, Tablet 시퀀스, 거울 참조를 런타임에서 자동 생성하거나 검색해
     수리하지 않는다. 명확한 오류로 멈추고 명시적인 Editor 메뉴에서만 작성한다.
5. **함께 영향을 받는 소비자**
   - HMD 양안과 Title 음원, PPE Near Grab과 환경 충돌, 공용 SFX와 발소리, 태블릿 완료 이벤트와
     서명, 거울 게이지와 이후 퀴즈 전환을 각각 구분했다.
6. **비교 기준**
   - 시작 동기화 전후, Wall Hanger 대형 차단체 유무, Marker Collider 전후 Bounds,
     발소리 `PlayOneShot`과 전용 루프, 태블릿 순차 체크와 즉시 체크, 거울 시선 조건 유무를
     비교한다.
7. **검증 수준**
   - 코드·직렬화와 Unity Editor 전용 하네스까지 확인했다.
   - 실제 Quest 빌드, HMD 양안, XR 출력 장치의 음원, 물리 접촉 체감은 별도 수동 검증으로
     남긴다.

## 적용한 변경

### 1. Meta/Quest Link 현상과 앱 시작 동기화

#### 조사 결과

- 실기 테스트 중 검은 화면과 모래시계, Title 시야 어긋남, 주변 시야 깨짐, Game View와 음원이
  HMD보다 먼저 시작하는 현상이 보고됐다.
- 세션 중 검은 화면·멈춤의 직접 원인은 사용자 확인상 Meta/Quest Link 업데이트 문제로
  분류했다. 프로젝트 변경이 원인이라고 확정하지 않는다.
- 별개로 앱 시작 코드에는 Title 씬 활성화와 음원이 실제 HMD 렌더 준비보다 먼저 진행될 수 있는
  동기화 공백이 있었다.

#### 변경

- `AppSceneBootstrap`이 Title 씬을 비동기 preload하되 `allowSceneActivation=false`로 대기한다.
- 유효한 Head XR 장치, 실행 중인 `XRDisplaySubsystem`, 서로 다른 두 번의
  `Application.onBeforeRender` 콜백을 확인한 뒤 Title을 활성화한다.
- `0_App`의 시작 BGM 자동 재생을 끄고, Title이 활성화된 뒤 `TitleSplashController`가 Title
  BGM을 시작하도록 옮겼다.
- Game View 시뮬레이션과 물리 HMD 경로의 기존 분리는 유지했다.

#### 영향 범위

- `0_App → 1_Title` 시작 전환과 Title BGM 시작 시점
- Android/Quest 및 Standalone OpenXR 시작 경로
- PPE 룸 안의 입력·UI·거울 로직은 시작 동기화 변경 대상이 아니다.

### 2. 메탈 셸브와 Wooden Crate 02 충돌

#### 근본 원인

- `PPE Environment Collision/Furniture - Wall Hanger`가 메탈 셸브 앞면보다 약 `0.329m`
  돌출되어 있었다. 플레이어 반경을 포함하면 HMD/Origin이 셸브에서 약 30cm 이상 떨어져
  멈출 수 있었다.
- 높이 `0.12m`인 셸브 베이스만으로는 `CharacterController.stepOffset=0.2m` 때문에 플레이어가
  단차를 올라가듯 셸브 내부로 들어갈 수 있었다.
- `PPE_B_WoodenCrate_02`에는 자체 Collider가 없었다.
- 헬멧 쪽 오른쪽 바깥 기둥은 세 Renderer 조각으로 분리되어 있어 한 조각만으로는 전체 기둥을
  막을 수 없었다.

#### 변경

- 중복 대형 `Furniture - Wall Hanger` 환경 차단체를 제거했다.
- 셸브 루트의 눕힌 베이스 Collider는 월드 크기 약 `(1.962, 0.120, 0.300)m`로 유지했다.
- 실제 수평 상판 Renderer `tripo_part_15`, `tripo_part_4`, `tripo_part_5`에 각각 Bounds와
  일치하는 Layer 2 BoxCollider를 추가했다.
- Wooden Crate 02 Renderer Bounds와 일치하는 전용 Layer 2 차단체를 추가했다.
- 헬멧 쪽 오른쪽 바깥 기둥의 `tripo_part_6`, `tripo_part_17`,
  `tripo_part_17 (1)` 합산 Bounds를 대표하는 BoxCollider 하나를 `tripo_part_6`에 추가했다.
  월드 크기는 약 `(0.042, 2.071, 0.092)m`다.
- 다른 세로 부재에는 Collider를 추가하지 않았다.

#### 보존한 동작

- 베이스 Collider는 사용자가 요구한 눕힌 형태를 유지한다.
- 셸브·상판·기둥은 Rigidbody가 없는 정적 Collider이므로 서로 밀어내지 않는다.
- 환경 Collider는 Layer 2, PPE 선택 Marker는 Layer 6을 사용해 Grab 입력 소비자를 분리한다.

### 3. PPE 선택 Marker 판정 축소

#### 근본 원인

- 보이는 발광 사각형은 Renderer이고 실제 선택은 Marker의 `SphereCollider`와 손 Near caster
  반경의 겹침으로 결정된다. 두 범위는 같지 않다.
- 오른쪽 장갑 기준 기존 Marker Collider 월드 지름은 약 `7.93cm`였고, 손 Near caster 반경은
  `10cm`라 인접 PPE가 함께 후보가 될 수 있었다.

#### 변경

- `PPE` 루트 바로 아래 실제 선택 대상 23개의 `SphereCollider.radius`를 로컬
  `0.25 → 0.125`로 줄였다.
- 오른쪽 장갑 기준 월드 판정 지름은 약 `7.93cm → 3.96cm`로 줄었다.
- 발광 Marker의 Transform, Renderer Bounds, Mesh, Material은 변경하지 않았다.
- 헬멧 결함 표시에 사용되는 중첩 Marker 2개는 변경 대상에서 제외했다.
- 손 Near caster의 전역 `0.1m` 반경은 다른 근접 상호작용에 영향을 주므로 변경하지 않았다.

### 4. 발소리 입력 유지형 단일 루프

#### 근본 원인

- `Foot Step.ogg` 길이는 약 `4.225초`인데 기존 이동 입력 소유자가 `0.4초`마다 공용 SFX
  Source에 `PlayOneShot`을 호출했다. 빠른 입력이나 입력 유지 시 같은 클립이 여러 겹 재생됐다.

#### 변경

- `AudioManager/Footsteps` 전용 AudioSource를 씬에 작성하고 Move Provider에 직렬화했다.
- 이동 입력이 0이 아닌 동안 같은 발소리 클립 하나만 `pitch=1`로 루프한다.
- 이동 속도는 발소리 pitch나 재생 간격을 바꾸지 않는다.
- 입력이 0이 되면 해당 Source를 즉시 `Stop`하고 clip을 해제한다.
- 공용 SFX Source와 분리해 입력 해제 시 정답·오답·PPE 착용 효과음을 끊지 않는다.

### 5. 태블릿 체크리스트 즉시 표시

#### 근본 원인

- 현재 활성 Tablet 시퀀스는 체크 6개와 서명 2개로 구성된다.
- 체크 6개가 각각 `0.85초`와 단계 간 지연으로 순차 표시되어 Trigger 입력 뒤 전체
  체크리스트가 보이기까지 약 5초 이상 걸렸다.

#### 변경

- 잡은 Tablet의 Trigger/XRI Activate가 승인되는 순간 앞 6개 체크를 모두 표시한다.
- 체크 SFX는 겹치지 않도록 입력 순간 한 번 재생한다.
- 뒤의 플레이어 서명과 확인 서명은 기존 작성 순서와 reveal 애니메이션을 유지한다.
- 즉시 표시 개수는 `PPETabletChecklistController.instantChecklistStepCount` 직렬화 값으로
  노출했고 현재 값은 `6`이다.
- 잡기만 했을 때 자동 시작하지 않고 Trigger/Activate에서 시작하는 기존 입력 정책을 유지한다.

### 6. 거울 시선 이탈 시 게이지 정지 제거

#### 근본 원인

- 기존 `MirrorCheckRoutine()`은 관찰 반경 안에서도 `IsLookingAtMirror()`가 참일 때만 5초
  게이지를 증가시켰다. 사용자는 고개를 돌렸을 때 진행이 멈춘 사실을 알아채기 어려웠다.

#### 변경

- 거울 관찰이 시작된 뒤에는 시선 방향과 무관하게 `m_ObservationElapsed`를 계속 증가시킨다.
- 관찰 반경을 벗어나면 관찰을 중단하고 재진입을 요구하는 기존 거리 조건은 유지한다.
- PPE·태블릿 완료 판정과 이후 퀴즈 시작 순서는 변경하지 않았다.

## 적용 파일

### 런타임

- `Assets/Scripts/AppSceneBootstrap.cs`
- `Assets/Scripts/TitleSplashController.cs`
- `Assets/Scripts/AudioManager.cs`
- `Assets/Scripts/PPEConfigurableDynamicMoveProvider.cs`
- `Assets/Scripts/HandwrittenSignatureSequence.cs`
- `Assets/Scripts/PPETabletChecklistController.cs`
- `Assets/Scripts/PPEFinaleController.cs`

### Editor 및 회귀 검증

- `Assets/Editor/AppStartupSynchronizationHarness.cs`
- `Assets/Editor/PPELocomotionPpeRegressionValidationHarness.cs`
- `Assets/Editor/PPETabletSignatureSequenceSync.cs`
- `Assets/Editor/PPERoomEnvironmentCollisionSetup.cs`

### 씬

- `Assets/Scenes/0_App.unity`
- `Assets/Scenes/1_Title.unity`
- `Assets/Scenes/3_PPE_Room_3mode_loco.unity`

## 완료한 검증

### 정적 확인

- 시작 씬 순서는 `0_App → 1_Title → 2_Intro → 6_LoadingScene_0 → 3_PPE_Room_3mode_loco`로
  유지했다.
- 프로젝트 소유 활성 씬 셰이더 7개에서 Single Pass Instanced 매크로 구성을 확인했다.
- 발광 Marker 23개의 Transform, Renderer Bounds, Mesh, Material이 Collider 변경 전후 동일했다.
- 헬멧 쪽 기둥 이외의 세로 부재에 새 BoxCollider가 없음을 확인했다.
- `git diff --check` 공백 오류는 없고 저장소 줄바꿈 변환 경고만 확인했다.

### Unity Editor 확인

- `AppStartupSynchronizationHarness` PASS.
- `Validate Primary PPE Marker Selection Radius` PASS.
- `Validate Footstep Input Loop` PASS.
- `Validate Immediate Tablet Checklist Reveal` PASS.
- `Validate Continuous Mirror Observation` PASS.
- `Validate Interactive Shelf Access Collision` PASS.
- 최종 Unity Console Error `0`, Warning `0`.
- 요청 변경을 저장한 직후 `3_PPE_Room_3mode_loco.unity`가 `dirty=false`, Play Mode 종료
  상태임을 확인했다.

### 검증 한계

- 전체 Room Collision 하네스는 이번 변경과 별개인 기존
  `PPE/PPE_A_SuitHang_Ripped` XRGrabInteractable Collider 바인딩 문제를 별도 추적해야 한다.
- Unity Editor 검증은 실제 Quest/OpenXR 양안, Link 지연, HMD 출력 음원, 컨트롤러 체감 충돌을
  대신하지 않는다.

## 추가 진단: 현재 가구 위치와 차단 Collider 정렬

- 사용자 요청에 따라 씬을 수정하지 않고 현재 Furniture Renderer Bounds와 환경 차단
  BoxCollider를 다시 대조했다.
- 가구 차단체 13개 중 9개가 허용 오차 `0.02m`를 넘었다.
- 8개는 Collider 크기는 같고 중심만 달라, 가구 위치가 차단체 생성 뒤 변경된 것으로 추정된다.
- Wooden Crate 02는 중심과 크기가 모두 달라 현재 Renderer Bounds를 다시 반영해야 한다.
- 메탈 셸브 베이스, 상판 3개, 헬멧 쪽 오른쪽 기둥 Collider는 현재 형상과 일치했다.
- 이번 요청은 확인 범위이므로 어긋난 차단체를 자동 이동하거나 저장하지 않았다.
- 진단용 Editor 코드가 다시 컴파일된 뒤 씬은 `dirty=true`가 됐다. Dirty 목록은
  `PPE_A_SCBA`, `PPE_A_SCBA_Cylinder`, `PPE_A_Backplate`, Mirror Reflection Camera,
  런타임 이름의 Quiz Canvas Clone, Finale UI, 양손 Controller Renderer였으며 환경 차단
  Collider는 포함되지 않았다. 이 상태는 자동 저장하지 않았다.

| 대상 | 중심 차이 | 크기 차이 | 주요 방향 |
|---|---:|---:|---|
| Yellow Trash Bin 01 | `0.126m` | `0.000m` | X/Y |
| Yellow Trash Bin 02 | `0.126m` | `0.000m` | X/Y |
| Cleaning Cart | `0.111m` | `0.000m` | X/Y |
| Suit Hanger | `0.057m` | `0.000m` | Y |
| Safety Cabinet | `0.032m` | `0.000m` | X |
| Bench | `0.141m` | `0.000m` | X |
| Storage Wall Rack | `0.174m` | `0.000m` | X |
| Wooden Crate 02 | `0.059m` | `0.117m` | 중심 Z, 크기 Z |
| Fire Extinguisher 01 | `0.076m` | `0.000m` | Z |

정상 정렬된 독립 가구 차단체는 Mask Locker, Metal Locker, Metal Locker 1,
Fire Extinguisher 02다. 다음 수정 시에는 위 9개만 현재 Renderer Bounds로 다시 맞추고,
정상 4개와 메탈 셸브 전용 Collider는 변경하지 않는다.

### PPE 위치 변경 후 Marker 재확인

- 사용자가 PPE 위치도 조정했다고 알려 현재 주요 PPE Marker 23개를 다시 검사했다.
- 23개 모두 로컬 반경 `0.125`와 Renderer 참조를 유지했다.
- PPE Marker끼리는 겹치지 않았고 가장 가까운 오른쪽 장갑과 Tape Marker의 표면 간격은
  약 `0.111m`였다.
- `PPE_A_FaceShield_Clean` Marker가 셸브 `tripo_part_4` 상판과 Y축으로 약 `0.0007m`
  접촉했다. Marker 중심은 상판 밖이라 얕은 경계 접촉이지만 HMD Grab을 확인해야 한다.
- `PPE_C_Tablet` Marker는 Wooden Crate 02 차단체 안에 중심까지 들어갔다. Marker 월드 크기
  약 `0.0375m` 중 Z축 겹침은 약 `0.0349m`다.
- 사용자가 Tablet Marker를 뒤로 옮긴 것이 의도한 작성값이라고 확인했다. Crate solid는 Layer 2,
  Marker는 Layer 6이며 손 Near/Far 입력은 Layer 2를 선택 대상으로 사용하지 않으므로, Bounds
  겹침만으로 Grab 차단이라고 판정하지 않는다.
- 이 단계에서는 PPE나 Marker Transform, 상판, 크레이트 차단체를 이동하지 않았다.
- 후속 수정에서는 Tablet과 Marker 위치를 보존하고, 변경된 Wooden Crate 02 Renderer에 맞춰
  차단체 중심과 크기만 다시 작성한다. 이후 Quest에서 뒤쪽 Marker의 실제 손 도달 여부를 확인한다.

## 작업 종료 결정 및 다음 작업

- 2026-08-27 작업은 진단과 기록까지로 종료한다. 씬과 Collider는 이 시점에 추가 수정하거나
  자동 저장하지 않는다.
- Tablet Marker를 뒤로 옮긴 위치와 사용자가 조정한 PPE 배치는 의도된 씬 작성값으로 보존한다.
- 다음 작업에서는 현재 Renderer Bounds와 어긋난 가구 차단체 9개만 재정렬한다. 정상인 가구
  차단체 4개와 메탈 셸브 베이스·상판 3개·헬멧 쪽 바깥 기둥 Collider는 변경하지 않는다.
- Wooden Crate 02는 이동된 현재 모델을 기준으로 전용 차단체의 중심과 크기만 맞추며 Tablet과
  Marker Transform은 건드리지 않는다.
- 재정렬 후에는 Editor 하네스 결과와 Quest에서의 실제 통과 차단·PPE 손 도달 여부를 구분해
  검증하고, 사용자가 확인한 뒤 씬을 저장한다.

## 다음 Quest/OpenXR 수동 검증

1. Quest Link가 완전히 연결된 상태에서 앱을 새로 시작한다.
2. HMD의 `불러오는 중` 화면이 끝나기 전에 Title 게임 화면이나 BGM이 먼저 진행하지 않는지
   확인한다.
3. Title이 양안에서 같은 위치에 보이고 주변 시야가 깨지지 않는지 확인한다.
4. 스틱을 일정하게 유지했을 때 발소리 클립 하나만 정속으로 반복되는지 확인한다.
5. 스틱을 빠르게 눌렀다 놓아도 발소리가 겹치거나 뒤로 밀리지 않고, 입력 해제 즉시 멈추는지
   확인한다.
6. Tablet을 잡은 뒤 Trigger를 눌렀을 때 체크 6개가 즉시 나타나고 이후 서명이 정상 완료되는지
   확인한다.
7. 거울 게이지 진행 중 고개를 돌려도 계속 증가하고, 관찰 반경을 벗어날 때만 중단되는지
   확인한다.
8. 셸브 상판, Wooden Crate 02, 헬멧 쪽 오른쪽 바깥 기둥을 몸이 통과하지 않는지 확인한다.
9. 새 기둥 Collider 때문에 셸브 앞에서 과도하게 밀리거나 가장자리에 걸리지 않는지 확인한다.
10. 발광 Marker 크기는 그대로이고 인접 PPE 오선택이 감소했는지 확인한다.
11. 가구 Collider 재정렬을 승인·적용한 뒤 위 9개 가구의 통과 차단과 체감 정지 위치를 확인한다.
12. FaceShield와 뒤쪽 Tablet Marker를 실제 손으로 잡아 Layer 분리 상태에서 정상 선택되는지
    확인한다.

## 관련 버그 리포트

- [Play Mode 검은 화면·모래시계 및 시작 구조 조사](../Bug/2026-08-04_PlayMode_BlackScreen_Hourglass_ProjectStructure_Investigation.md)
- [PPE 룸 환경 Collider](../Bug/2026-08-24_PPE_Room_Environment_Collision.md)
- [PPE 커밋 후보 후속 QA 및 발소리](../Bug/2026-08-22_PPE_CommitCandidate_Followup_QA.md)
- [Tablet 체크·서명 표시](../Bug/2026-08-04_PPE_Tablet_CheckSignature_Invisible_MaterialReference.md)
- [거울 관찰 게이지](../Bug/2026-08-21_PPE_Mirror_Observation_Gauge_NotShown.md)

## 보안 및 계정 기록

- 테스트 계정은 사용자가 Unity 실행 중 직접 입력했다.
- 계정 ID, 비밀번호, 토큰은 문서와 코드에 기록하지 않았다.
