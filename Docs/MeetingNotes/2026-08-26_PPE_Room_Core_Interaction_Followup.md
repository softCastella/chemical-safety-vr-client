# PPE 룸 핵심 상호작용 후속 회의록

- 일자: 2026-08-26
- 대상 씬: `client/Assets/Scenes/3_PPE_Room_3mode_loco.unity`
- 범위: PPE Grab, 진열장 접근, 로코모션 방향, 거울 관찰, Exit 복귀, 카드 음성 및 정상 종료 복귀

## 회의 목적

PPE 룸에서 동시에 확인된 여러 회귀를 핵심 사용자 흐름 기준으로 다시 정리하고, 각 기능의
최종 요구사항과 보존 조건을 확정한다. 재현 절차·근본 원인·직렬화 값과 같은 기술 상세는 기존
버그 리포트에 기록하고, 이 회의록에는 합의한 동작과 현재 상태를 남긴다.

## 확정한 요구사항

1. 정상 방호복 Grab 안내는 교육 모드의 한 세션에서 최초 1회만 재생한다.
2. 거울 앞에 들어가면 PPE 완비 여부와 관계없이 5초 관찰 게이지를 먼저 표시한다. 관찰 뒤에
   PPE·태블릿 완료 여부를 판정한다.
3. 플레이어는 가구 내부로 들어갈 수 없어야 하지만, 진열장 안에 전시된 PPE 마커에는 손이
   닿아야 한다.
4. PPE는 손 근접 Grab으로 선택하고, 카드용 원거리 Ray가 PPE 마커를 선택해서는 안 된다.
5. 몸과 HMD를 돌린 뒤 조이스틱을 앞으로 밀면 현재 HMD 정면으로 이동해야 한다.
6. 활성 PPE 세션에서 Exit 영역 안으로 걸어 들어오면 중도 복귀를 시작한다.
7. 카드 앞 도착 안내는 애플리케이션 시작 후 최초 1회만 재생하고 반복하지 않는다.
8. 중도 복귀뿐 아니라 교육·훈련의 정상 종료와 테스트 결과 화면의 뒤로가기도 모두 카드 목록이
   아닌 학습/훈련/테스트 모드 선택 모달로 복귀한다.

## 주요 판단과 결정

### PPE 진열장과 아이템 마커

- 상호작용 진열장 루트는 `PPE_B_MetalShelving`이며, `PPE_A_*` 장비와
  `XR Item Marker_small`은 진열장 안의 선택 대상이다.
- 마커를 BoxCollider로 바꾸지 않는다. 기존 Sphere 범위와 Transform을 보존한 채 Trigger로
  사용한다.
- 양손 Near caster만 Trigger를 조회한다. Marker Layer 6은 Far caster mask에서 제외해
  원거리 카드 Ray와 PPE Grab의 역할을 분리한다.
- 진열장 전체 높이를 덮는 차단체는 사용하지 않는다. 최종 차단체는 가구 진입만 막는 낮고 가로로
  놓인 베이스 `BoxCollider`이며, 높이 `0.12m`, 깊이 `0.3m`, 시각적 앞면 정렬을 기준으로 한다.
- 베이스 `BoxCollider`는 별도 숨은 환경 오브젝트가 아니라 `PPE_B_MetalShelving` 루트가 직접
  소유한다. 따라서 사용자는 해당 셸브를 선택해 Inspector에서 `Center`와 `Size`를 조정할 수 있다.
- Trigger 전환 직후 Near caster가 Trigger를 무시해 모든 PPE가 잡히지 않는 회귀가 있었고,
  Near query를 `Collide`로 맞춘 뒤 사용자가 Grab 가능 상태를 확인했다.

### 이동과 Ray

- 연속 이동 방향 기준은 비활성 Hand Tracking Camera가 아니라 활성
  `XR Origin (VR)/Camera Offset/Main Camera`다.
- 좌·우 Move는 모두 `HeadRelative`를 유지한다.
- 시작 시 보이는 Ray는 좌·우 손에 하나씩, 총 두 개이면 정상 구성이다. 한 손에서 두 개가
  겹쳐 보이는 현상은 별도 재현 증거가 있을 때 버그로 분류한다.

### 거울과 복귀 상태

- 거울 관찰 시작 조건의 소유자는 `PPEFinaleController`다. 활성 PPE 세션이면
  `WaitingForMirror`로 진입할 수 있고, PPE 완료 검사는 관찰 종료 뒤 수행한다.
- Exit 걸어서 진입은 작성된 Exit Collider의 XZ 영역과 플레이어 위치를 사용하는
  `PPEExitTeleportMarkerRelay`가 담당한다. 시작 지점 근접만으로 세션을 종료하지 않는다.
- 정상 완료와 중도 종료는 모두 `PPEFinaleController.ReturnToModeChoices()`로 합류한다.
  교육·훈련은 완료 음성 뒤 자동 복귀하고, 테스트는 결과 화면의 뒤로가기 후 복귀한다.
- 호환용 구형 완료 메서드도 카드 목록을 직접 열지 않고 모드 선택 복귀 메서드에 위임한다.

### 음성 재생 정책

- 방호복 Grab 음성의 1회 상태는 모드 세션 단위이며 다음 모드 세션 시작 때 초기화한다.
- 카드 도착 음성의 1회 상태는 애플리케이션 실행 단위이며 모드 복귀 때 초기화하지 않는다.
- `CardIntro`는 카드 선택을 기다리는 상태로 유지하되, 최초 음성 종료 뒤 반복 음성 루틴은
  시작하지 않는다.

## 현재 상태

| 항목 | 구현/정적 확인 | 사용자·실기 확인 |
|---|---|---|
| PPE Marker 근접 Grab | Marker Trigger, Near `Collide`, Far Layer 6 제외 | Grab 가능 확인 |
| 진열장 접근과 가구 진입 차단 | 단스 가로 배치·앞면 정렬, 플레이어 캡슐 반경 0.1m | 최종 거리·밀림·PPE Grab 재확인 필요 |
| HMD 기준 조이스틱 전진 | 활성 Main Camera 참조 및 HeadRelative 작성 확인 | Quest에서 뒤돌아 전진 재확인 필요 |
| 방호복 음성 세션당 1회 | 소비 상태와 초기화 경로 구현 | Play Mode 재확인 필요 |
| 거울 5초 게이지 | 상태 진입·씬 참조·하네스 조건 확인 | 미완료/완료 양쪽 재확인 필요 |
| Exit 걸어서 중도 복귀 | Player·EnterVolume·Finale 참조 확인 | 음성·페이드·모달 순서 재확인 필요 |
| 카드 안내 실행당 1회 | 재진입 및 반복 재생 차단 구현 | Play Mode 재확인 필요 |
| 정상/중도 종료 모드 모달 복귀 | 모든 완료 진입점을 단일 경로로 통합 | 세 모드별 재확인 필요 |
| Play 시작 시 손당 두 Ray | 씬 Ray는 손당 하나이며 바깥쪽은 Play 전 Quest Link 시스템 Ray로 구분 | Link 메뉴 종료·재연결 비교 필요 |

## 완료한 검증

- `Assembly-CSharp-Editor.csproj --no-restore` 빌드 오류 0개를 확인했다. 기존 deprecated API
  경고는 남아 있다.
- `git diff --check`에서 공백 오류는 없고 저장소 기존 줄바꿈 경고만 확인했다.
- 씬과 코드에서 Marker 25개 Trigger, 좌·우 Near trigger query, Far Layer 6 제외,
  진열장 하단 단스 차단체의 시각적 앞면 정렬과 뒤쪽 `Wall - Front` 유지,
  활성 Main Camera 이동 참조, 거울·Exit 작성 참조를 정적으로 확인했다.
- `PPELocomotionPpeRegressionValidationHarness`, `PPERoomEnvironmentCollisionValidationHarness`,
  `PPETrainTestModeValidationHarness`에 관련 회귀 조건을 반영했다.

## 남은 수동 검증

1. Unity가 외부 스크립트와 씬 변경을 임포트하고 컴파일을 마친 뒤 Play Mode를 새로 시작한다.
2. 교육 세션에서 방호복을 여러 번 잡아 안내가 한 번만 나오는지 확인한다.
3. 진열장 앞까지 전진해 뒤로 밀리지 않는지, 가구 안으로는 들어가지 않는지, 헬멧을 포함한 각
   PPE를 손으로 잡을 수 있는지 확인한다.
4. HMD를 180도 돌린 상태에서 조이스틱 전진 방향을 확인한다.
5. PPE 미완료와 완료 상태 각각에서 거울 게이지 0% 시작, 5초 진행, 이탈·재진입을 확인한다.
6. Exit 영역 걸어서 진입 시 중도 안내, 페이드, 시작 위치, 모드 선택 모달 순서를 확인한다.
7. 교육·훈련 정상 종료와 테스트 결과 뒤로가기에서 모두 모드 선택 모달로 돌아가는지 확인한다.
8. Quest/OpenXR에서 양안 거울 표시, 컨트롤러 Grab, 이동 충돌과 실제 음성 출력을 최종 확인한다.
9. Meta Universal Menu와 Link Dash를 닫은 상태에서 손당 안쪽 플레이 Ray만 남는지 확인한다. 바깥쪽
   시스템 Ray가 계속 남으면 Quest Link 세션을 종료·재연결한 뒤 비교한다.

## 관련 버그 리포트

- [PPE 착용 로직·시나리오 분기 QA](../Bug/2026-08-21_PPE_WearLogic_ScenarioBranch_QA.md)
- [거울 관찰 게이지 미표시](../Bug/2026-08-21_PPE_Mirror_Observation_Gauge_NotShown.md)
- [PPE 룸 환경 충돌](../Bug/2026-08-24_PPE_Room_Environment_Collision.md)
- [Exit 텔레포트와 복귀](../Bug/2026-08-18_PPE_Exit_Teleport_Marker_And_Teleport_Cancel_Return.md)
- [카드 음성 및 정상 종료 복귀](../Bug/2026-08-15_PPE_Training_Quiz_Mode_And_Completion_Fade.md)
- [첫 Play HMD/Simulator와 Quest Link 시스템 Ray](../Bug/2026-08-25_PPE_First_Play_HMD_Simulator_Race.md)

## 진단 도구 관련 메모

`PPEObjectSpatialDiagnosticHarness`는 선택된 GameObject가 있을 때만 동작하는 진단 전용 도구다.
선택 없이 실행해 출력된 `PPE spatial diagnosis requires a selected scene GameObject.` 오류는 씬이나
오브젝트를 수정하지 않으며 이번 결함의 원인이 아니다.
