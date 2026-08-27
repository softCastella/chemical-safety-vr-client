# 2026-07-16 PPE 잡기 및 신체 장착 상호작용 회의록

- 일자: 2026-07-16
- 프로젝트: `3D_UI_Test_home`
- 대상 플랫폼: Meta Quest/Android, PC OpenXR
- 관련 기술: OpenXR, XR Interaction Toolkit, XR Hands
- 주제: PPE FBX를 손으로 잡아 신체 부위에 가져가면 몸 쪽으로 흡착된 뒤 장착 처리하는 기능
- 상태: 설계 논의 완료, 2026-08-03 단상 PPE Grab 1차 구현 적용

## 1. 논의 목적

산업 안전 교육 시나리오에서 사용자가 PPE 오브젝트를 직접 손으로 집어 올바른 신체 부위에 가져가는 장착 동작을 구현한다.

목표 사용자 경험은 다음과 같다.

1. 사용자가 PPE FBX 오브젝트를 손으로 잡는다.
2. 잡은 PPE를 머리, 가슴, 허리 등 지정된 착용 부위로 가져간다.
3. 유효한 착용 영역에 도달하면 PPE가 몸 쪽 장착 지점으로 짧게 흡착된다.
4. 흡착이 끝나면 손에서 해제되고 원본 오브젝트가 사라지거나 장착 표시 모델로 전환된다.
5. 시스템이 해당 PPE의 장착 상태를 기록한다.

## 2. 신체 위치 인식 방식

### 직접 추적 가능한 위치

일반적인 Quest/OpenXR 구성에서 안정적으로 직접 얻을 수 있는 위치는 다음과 같다.

| 신체 부위 | 위치 기준 | 비고 |
|---|---|---|
| 머리 | XR 카메라 또는 Center Eye Pose | HMD가 직접 추적함 |
| 왼손 | Left Hand/Controller Pose | 컨트롤러 또는 손 추적 사용 |
| 오른손 | Right Hand/Controller Pose | 컨트롤러 또는 손 추적 사용 |

### 추정이 필요한 위치

별도의 신체 트래커가 없다면 가슴과 허리 위치는 직접 측정할 수 없으므로 머리 위치와 사용자 키를 기준으로 추정한다.

| 착용 영역 | 기본 추정 방식 |
|---|---|
| 머리 | XR 카메라 주변에 Equip Zone 배치 |
| 가슴 | 머리 기준 아래쪽 오프셋에 Equip Zone 배치 |
| 허리 | 머리 기준 아래쪽 오프셋에 Equip Zone 배치 |

오프셋, 영역 크기, 회전 보정값은 런타임 코드에 고정하지 않고 Inspector 직렬화 필드 또는 씬/프리팹 값으로 관리한다.

## 3. 몸 방향 추정

- 카메라의 전방 벡터에서 상하 기울기를 제거한 수평 방향을 기본 몸 방향으로 사용한다.
- 사용자가 고개만 좌우로 돌릴 수 있으므로 Body Anchor가 카메라 회전을 즉시 그대로 따라가게 하지 않는다.
- 방향 변화에는 완만한 추종 또는 회전 임계값을 적용해 가슴·허리 영역이 흔들리지 않도록 한다.
- 필요하면 머리와 양손의 상대 위치를 보조 정보로 사용하되, 손을 모으거나 한 손만 추적되는 상황에서도 동작해야 한다.

## 4. 제안 Hierarchy

```text
XR Origin
├─ Main Camera                 (머리 추적)
├─ Left Hand                  (왼손 추적/잡기)
├─ Right Hand                 (오른손 추적/잡기)
└─ Body Anchor                (추정 몸 중심 및 수평 방향)
   ├─ Head Equip Zone
   ├─ Chest Equip Zone
   └─ Waist Equip Zone
```

각 Equip Zone은 보이지 않는 Trigger Collider와 착용 위치를 나타내는 별도 Anchor를 가진다. 감지 영역과 최종 흡착 위치를 분리하여 Inspector에서 각각 조절할 수 있게 한다.

## 5. PPE 오브젝트 구성안

PPE FBX의 프로젝트 소유 래퍼 또는 프리팹에 다음 구성을 적용한다.

| 구성 요소 | 역할 |
|---|---|
| Collider | 손 잡기 및 착용 영역 진입 감지 |
| Rigidbody | XR 물리 상호작용 |
| XR Grab Interactable | 손 또는 컨트롤러로 잡기 |
| PPE 식별 컴포넌트 | PPE 종류와 허용 착용 부위 정의 |
| 장착 상태 데이터 | 미착용, 흡착 중, 장착 완료 상태 관리 |

가져온 FBX 원본이나 패키지 파일을 직접 수정하기보다 프로젝트 소유 프리팹 또는 래퍼 오브젝트에 상호작용 컴포넌트를 추가한다.

## 6. 장착 판정 및 처리 흐름

```text
PPE 잡기
  → 올바른 Equip Zone 진입
  → 잡고 있는 상태인지 확인
  → PPE 종류와 착용 부위 일치 확인
  → 일정 시간 유지 또는 놓기 입력 확인
  → 손의 선택 해제
  → 장착 Anchor로 흡착 이동
  → 원본 비활성화 또는 착용 모델 표시
  → 장착 상태 및 시나리오 진행 상태 기록
```

### 오작동 방지 조건

- PPE가 영역을 스치기만 했을 때 즉시 사라지지 않도록 한다.
- 기본 판정은 `잡은 상태 + 올바른 영역 + 짧은 유지 시간` 조합으로 한다.
- 잘못된 신체 부위에서는 장착하지 않고 필요한 경우 시각·음향 피드백만 제공한다.
- 한 번 장착 처리가 시작되면 중복 Trigger 이벤트를 무시한다.
- 흡착 중에는 Grab, Rigidbody, Collider 상태를 명시적으로 관리한다.

## 7. 흡착 및 사라짐 연출

- Equip Zone 진입이 확정되면 PPE를 손에서 안전하게 해제한다.
- PPE는 현재 위치에서 장착 Anchor까지 설정된 시간 동안 이동 및 회전한다.
- 이동 곡선, 소요 시간, 최종 위치·회전은 Inspector에서 설정한다.
- 흡착 완료 후 처리 방식은 PPE별 설정으로 선택한다.
  - 원본 오브젝트 비활성화
  - 착용된 별도 모델 활성화
  - 시각 모델만 숨기고 장착 상태 유지
- 선택적으로 사운드, 햅틱, 파티클 피드백을 연결한다.

## 8. 결정 사항

1. 1차 구현은 별도 전신 트래커 없이 XR 카메라 기준 Body Anchor 방식으로 진행한다.
2. 머리는 직접 추적하고, 가슴과 허리는 직렬화된 오프셋으로 추정한다.
3. 감지 영역과 최종 장착 Anchor를 분리한다.
4. PPE 종류별로 허용 착용 부위를 지정한다.
5. 단순 Trigger 진입만으로 장착하지 않고 잡기 상태와 유지 조건을 함께 검사한다.
6. 모든 위치, 크기, 시간, 색상 및 연출 값은 씬·프리팹·Inspector를 설정 원본으로 사용한다.
7. 런타임 코드는 Inspector에서 작성된 UI 및 오브젝트 표현 값을 임의로 덮어쓰지 않는다.
8. Quest와 PC OpenXR에서 같은 핵심 로직을 사용하되 입력 및 추적 방식 차이를 검증한다.

## 9. 캘리브레이션 고려 사항

- 사용자 키 차이 때문에 고정 거리만 사용하면 가슴·허리 판정이 어긋날 수 있다.
- 1차 버전에서는 Inspector 기본 오프셋과 충분한 Trigger 범위를 사용한다.
- 후속 버전에서는 시작 시 바닥과 머리 높이를 측정해 사용자 키 비율로 가슴·허리 높이를 계산하는 캘리브레이션을 검토한다.
- 앉은 자세와 서 있는 자세가 모두 필요하다면 자세 전환 정책을 별도로 정의해야 한다.
- 전신 또는 허리 트래커가 추가되면 추정 Anchor 대신 실제 추적 Pose를 선택적으로 사용할 수 있게 확장한다.

## 10. 구현 전 확인 사항

- 실제 PPE FBX 및 프리팹 경로와 종류 확인
- 대상 PPE별 착용 부위 정의
- 현재 XR Origin, Camera, Left/Right Hand Hierarchy 확인
- 각 PPE에 기존 Collider, Rigidbody, XR Grab Interactable이 있는지 확인
- 씬 로드 또는 초기화 코드가 Inspector 값을 덮어쓰는지 확인
- 장착 완료 시 연결할 시나리오 상태 관리 컴포넌트 확인
- 원본을 숨길지, 몸에 장착된 모델을 별도로 표시할지 결정

## 11. 검증 체크리스트

### 기능 검증

- [ ] 왼손과 오른손 모두 PPE를 잡을 수 있다.
- [ ] 올바른 착용 부위에서만 장착된다.
- [ ] 잘못된 영역을 통과해도 PPE가 사라지지 않는다.
- [ ] 영역을 스치기만 했을 때 장착되지 않는다.
- [ ] 장착 확정 시 손에서 정상적으로 해제된다.
- [ ] PPE가 설정된 Anchor 방향으로 자연스럽게 흡착된다.
- [ ] 흡착 완료 후 중복 장착 이벤트가 발생하지 않는다.
- [ ] 장착 상태가 시나리오 진행 로직에 정상 반영된다.

### 추적 및 사용성 검증

- [ ] 고개를 좌우로 돌려도 가슴·허리 Equip Zone이 과도하게 회전하지 않는다.
- [ ] 키가 다른 사용자도 장착하기 어렵지 않다.
- [ ] 서 있는 자세에서 머리·가슴·허리 영역이 자연스럽다.
- [ ] 손 추적이 잠시 끊기거나 Grab이 취소되어도 오류가 발생하지 않는다.

### 플랫폼 검증

- [ ] Meta Quest 빌드에서 손 추적 또는 컨트롤러 Grab이 동작한다.
- [ ] PC OpenXR에서 Grab과 장착 판정이 동작한다.
- [ ] 장착 순간 프레임 저하, 물리 튕김 또는 위치 순간이동 문제가 없다.
- [ ] Unity Console에 C# 컴파일 오류와 런타임 예외가 없다.

## 12. 후속 작업

1. PPE 프리팹과 현재 XR Rig 구조 조사
2. Body Anchor 추정 컴포넌트 설계
3. Equip Zone 및 PPE 식별 컴포넌트 구현
4. 흡착·장착 상태 머신 구현
5. 대표 PPE 1종으로 Play Mode 검증
6. Quest와 PC OpenXR 실기기 검증
7. 사용자 키 캘리브레이션 필요성 평가

## 13. 추가 요구사항: 장갑 및 잘린 팔뚝 방호복 표시

### 요구사항

- 방호복 장착 완료 후 사용자 시점에서 방호 장갑이 보여야 한다.
- 손목부터 팔뚝 일부까지 이어지는 짧은 방호복 소매가 보여야 한다.
- 소매는 전신 방호복을 모두 표시하지 않고 팔 중간에서 잘린 1인칭 전용 형태로 사용한다.
- 장갑과 소매는 왼손·오른손의 추적 움직임을 따라야 한다.

### 구현 범위 결정

교수자 요구사항을 만족하기 위한 1차 범위에서는 전신 아바타, 다리 및 장화 표현보다 손과 팔의 1인칭 시각 표현을 우선한다.

```text
Left Hand Tracking
├─ Default Hand Visual
└─ PPE Hand Visual
   ├─ Rigged Glove FBX
   └─ Forearm Sleeve FBX

Right Hand Tracking
├─ Default Hand Visual
└─ PPE Hand Visual
   ├─ Rigged Glove FBX
   └─ Forearm Sleeve FBX
```

- 방호복 장착 전: 기존 Hand Visual 표시, PPE Hand Visual 숨김
- 방호복 장착 후: 기존 Hand Visual 숨김 또는 필요한 부분만 유지, PPE Hand Visual 표시
- 방호복 해제 기능이 추가되면 위 표시 상태를 다시 원복

## 14. 핸드 모델에 FBX를 적용하는 방법

핸드 추적 오브젝트의 자식으로 FBX를 배치하는 것은 가능하다. 그러나 손가락까지 자연스럽게 움직이려면 장갑 FBX의 리깅 및 본 구조를 확인해야 한다.

| FBX 상태 | 적용 가능 범위 | 처리 방법 |
|---|---|---|
| XR Hand와 호환되는 리깅 장갑 | 손목 및 손가락 추적 | 장갑 본을 XR Hands 관절에 매핑하거나 동일 스켈레톤 사용 |
| 별도 스켈레톤으로 리깅된 장갑 | 본 이름과 구조에 따라 가능 | 손가락 본 대응표와 관절 추종 컴포넌트 필요 |
| 리깅되지 않은 장갑 | 손 전체의 위치·회전 추종 | 손목 Transform 자식으로 배치하며 손가락 개별 움직임은 표현하지 못함 |
| 짧은 소매 FBX | 손목·팔뚝 시각 표현 | 손목 또는 별도 Forearm Anchor 아래 배치 |

### 권장 제작 방식

가장 안정적인 방식은 Blender 등의 DCC 도구에서 장갑 메시와 짧은 소매를 현재 XR Hand 스켈레톤에 맞춰 스킨 및 웨이트 작업한 뒤 FBX로 가져오는 것이다.

- 장갑은 손가락 관절 변형을 따라갈 수 있도록 손 스켈레톤에 스킨한다.
- 소매는 손목 움직임을 따라가되 손목 주변 변형이 깨지지 않도록 웨이트를 조정한다.
- 소매 끝은 팔꿈치까지 연결하지 않고 팔뚝 중간에서 마감한다.
- 잘린 단면이 보이지 않도록 안쪽 마감 메시, 커프 또는 어두운 내부 재질을 준비한다.
- 왼손과 오른손은 미러링 결과와 본 축 방향을 각각 확인한다.

Unity는 서로 다른 형태의 장갑 FBX를 기존 손에 자동으로 맞춰 입히지 않는다. 장갑의 본 구조가 기존 XR Hand와 다르면 단순히 자식으로 추가하는 것만으로 손가락 변형까지 따라가지 않으므로 리깅 수정 또는 관절 매핑 작업이 필요하다.

## 15. 장갑·소매 런타임 처리 원칙

- 방호복 장착 상태가 변경될 때 장갑 및 소매 Visual의 활성 상태만 전환한다.
- 장갑과 소매의 위치, 회전, 크기, 재질 및 손별 보정값은 프리팹과 Inspector에서 작성한다.
- 런타임 코드가 FBX의 작성된 Transform이나 재질 값을 매 프레임 임의로 초기화하지 않도록 한다.
- 기존 XR Hands Visualizer 또는 프로젝트 소유 Hand Visual이 활성 상태를 다시 덮어쓰는지 `Awake`, `OnEnable`, `Start` 및 추적 갱신 경로를 함께 확인한다.
- 장갑 FBX가 손 메시 위에 겹치는 경우 기존 손 메시를 숨기거나 장갑 내부로 손 메시가 뚫고 나오지 않도록 렌더링 정책을 정한다.
- 손 추적과 컨트롤러 입력을 모두 지원한다면 각 모드에 맞는 장갑 시각 모델 또는 고정 손 자세를 준비한다.

## 16. 장갑·소매 추가 검증 체크리스트

- [ ] 방호복 장착 전에는 기본 손 모델이 정상적으로 보인다.
- [ ] 방호복 장착 후 좌우 장갑과 짧은 소매가 표시된다.
- [ ] 장갑의 손목 위치, 회전 및 크기가 실제 추적 손과 일치한다.
- [ ] 손가락을 굽히고 펼 때 장갑 메시가 관절을 정상적으로 따라간다.
- [ ] 엄지와 각 손가락의 본 매핑이 뒤바뀌지 않았다.
- [ ] 손목을 크게 회전해도 소매가 분리되거나 심하게 찌그러지지 않는다.
- [ ] 소매 끝의 잘린 단면이나 빈 내부가 사용자 시점에 부자연스럽게 노출되지 않는다.
- [ ] 기존 손 메시가 장갑 밖으로 뚫고 나오지 않는다.
- [ ] 왼손과 오른손의 메시, 본 축 및 재질이 올바르게 적용된다.
- [ ] 손 추적이 끊겼다가 복구되어도 장갑·소매 표시 상태가 유지된다.
- [ ] Quest 손 추적 모드에서 손가락 동작과 성능을 확인한다.
- [ ] PC OpenXR 컨트롤러 모드에서 장갑 표시와 고정 손 자세를 확인한다.

## 17. 수정된 후속 작업 우선순위

1. 사용할 장갑 및 소매 FBX의 리깅 여부와 본 구조 확인
2. 현재 XR Hand Visual의 스켈레톤, 본 이름 및 활성화 경로 조사
3. 대표 한쪽 손에 장갑 FBX를 적용해 손가락 추종 가능 여부 검증
4. 왼손·오른손용 PPE Hand Visual 프리팹 구성
5. 방호복 장착 상태와 Hand Visual 전환 로직 연결
6. 짧은 팔뚝 소매의 길이, 손목 연결부 및 잘린 단면 조정
7. Quest 손 추적 및 PC OpenXR 컨트롤러 환경 검증
8. 이후 필요할 때 몸통, 장화 또는 전신 표현 범위를 별도로 평가

## 비고

- 본 문서는 기능 설계 회의록이며 실제 씬, 프리팹, 런타임 코드는 아직 변경하지 않았다.
- 최종 착용 위치는 HMD 추정만으로 실제 신체와 완전히 일치하지 않을 수 있으므로, 실기기 테스트를 통해 Equip Zone 크기와 오프셋을 조절해야 한다.
- 교수자 요청에 따라 1차 시각 표현의 우선순위는 전신 아바타보다 장갑과 잘린 팔뚝 방호복으로 조정하였다.

## 18. 2026-08-03 단상 PPE Grab 1차 구현

### 적용한 변경

- 대상 씬은 `Assets/Scenes/3_PPE_Room_HandTest.unity`이다.
- 텔레포트 도착 지점 앞 `interiorObjects/PPE` 아래 PPE 9종에 다음 직렬화
  컴포넌트를 추가했다.
  - `PPEItemIdentity`
  - `BoxCollider`
  - `Rigidbody`
  - `XRGrabInteractable`
- `PPEItemIdentity`에는 이후 착용 부위 판정과 상태 연결에 사용할 PPE 종류를
  기록했다.
- 단상 `wooden crate`에는 물리 Collider가 없으므로, 1차 Grab 시험에서는 PPE가
  시작 직후 떨어지거나 멀리 날아가지 않게 `Use Gravity`와 `Throw On Detach`를
  껐다. Grab 이동 방식은 `Kinematic`, 동적 Attach는 활성화했다.
- 가져온 FBX와 원본 프리팹은 수정하지 않고 원본 HandTest 씬의 프로젝트 소유
  래퍼 오브젝트에만 상호작용 컴포넌트를 추가했다.

### 크기 보정

- 이전 실물 스케일 시험과 착용 피드백을 바탕으로 다음 단상 인스턴스만 우선
  보정했다.
  - `gas_mask`: Local Scale `1.0 -> 0.30`
  - `blue_rubber_gloves_L`: Local Scale `1.0 -> 0.404`
  - `blue_rubber_gloves_R`: Local Scale `1.0 -> 0.404`
  - `orange_tape`: Local Scale `0.20 -> 0.0726` (`0.20 x 0.363`)
- 축소 후 Renderer Bounds의 아래쪽이 나무 단상 상단보다 `0.005m` 위에 오도록
  최초 위치를 다시 맞췄다.
- 헬멧, 호흡기, 하네스, 좌우 장화는 아직 확정된 실물 목표 치수가 없어 기존
  Transform 크기를 유지했다. 임의 추정값은 적용하지 않았다.

### 재실행 및 작성값 보존 정책

- `Tools > PPE > Configure Platform Grab Items`는 최초 구성에만 크기와 Collider
  기본값을 적용한다.
- 다시 실행할 때 기존 `PPEItemIdentity`의 적용 마커를 확인하여 Inspector에서
  조정한 Transform 값을 반복해서 덮어쓰지 않는다.
- `Tools > PPE > Validate Platform Grab Items`로 9종의 식별자, Collider,
  Rigidbody, XR Grab 구성을 다시 확인할 수 있다.

### 완료한 검증

- 설정 도구 내부의 결정적 검증이 통과한 뒤 씬을 저장했다.
- 저장된 Unity YAML에서 PPE 식별자 9개, `XRGrabInteractable` 9개,
  `platformGrabSetupApplied` 마커 9개를 확인했다.
- 저장된 값에서 9종 모두 `Use Gravity=0`, `Throw On Detach=0`임을 확인했다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`은 오류 0개로
  완료됐다. 기존 Unity.AI 참조 충돌 및 source generator 경고는 남아 있다.

### 아직 필요한 수동 검증

- `3_PPE_Room_HandTest.unity`를 열고 Quest/OpenXR Play Mode에서 왼손과 오른손
  Near-Far Interactor로 PPE 9종을 선택할 수 있는지 확인한다.
- 마스크, 좌우 장갑, 테이프의 Near Grab Collider가 손 크기에 비해 자연스러운지
  확인한다.
- 잡는 순간 오브젝트가 손에서 튀거나 비정상 회전하지 않는지 확인한다.
- 놓은 뒤 중력 없이 해당 위치에 유지되는 1차 정책이 시나리오 진행에 적합한지
  확인한다.
- 나머지 헬멧, 호흡기, 하네스, 좌우 장화는 실물 목표 치수를 확정한 뒤 별도로
  보정한다.

## 19. 2026-08-04 PPE 오염 판정·폐기·순차 착용 시나리오 후속 회의

### 회의 목적

- 사용자가 PPE를 단순히 집는 데서 끝나지 않고, 직접 관찰하여 오염 또는 하자를
  판단한 뒤 `사용` 또는 `폐기`를 선택하는 교육 흐름을 정의한다.
- 깨끗한 PPE의 착용 연출, 좌우 신체 부위별 착용 상태, 손 모델 전환 및 전체
  착용 순서를 하나의 시나리오 상태로 연결한다.
- 현재 구현된 방호복 Grab 시험을 전체 PPE 시나리오로 확장할 때 필요한 런타임,
  UI, 셰이더 및 검증 범위를 합의한다.

### 현재 적용된 기준 동작

- 대상 씬 `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`의
  `hazmat_suit_off`는 `XR Item Marker_ball`과 `XR Item Marker_small`을 잡기
  영역으로 사용한다.
- 동적 Attach를 사용하여 사용자가 처음 잡은 위치와 방향을 유지한 채 방호복을
  들고 회전하여 관찰할 수 있다.
- 방호복을 가리키는 동안에만 그립 입력을 Toggle 방식으로 전환한다. 첫 번째 짧은
  그립으로 잡고, 두 번째 그립으로 놓는다.
- 방호복을 놓으면 Rigidbody의 이동·회전 속도를 제거하고 Play Mode 시작 시 씬에
  작성되어 있던 부모, Local Position, Local Rotation 및 Local Scale로 즉시
  돌아간다.
- 위 동작은 `Assets/Scripts/PPEMarkerToggleGrab.cs`에서 담당한다. 복귀 위치 숫자는
  코드에 고정하지 않고 씬의 작성값을 기준으로 사용한다.

### 확장이 필요한 근본 원인

- 현재 Grab 시험만으로는 PPE가 깨끗한지, 오염되었는지, 사용 가능한지 또는
  폐기해야 하는지를 표현하거나 기록할 수 없다.
- 오염 상태를 패널 문구로 직접 알려주면 사용자가 모델을 관찰하고 판단하는 교육
  목적이 약해진다. 따라서 상태는 PPE 외형의 시각적 단서로 전달하고, 패널은 행동
  선택을 제공하는 역할로 제한해야 한다.
- PPE마다 착용 위치와 연출이 다르고 장갑·장화는 좌우 상태를 개별 관리해야 하므로,
  단일 활성/비활성 처리보다 PPE 정의, 배치 슬롯, 착용 Anchor 및 시나리오 상태를
  분리한 구조가 필요하다.

### 확정된 관찰·평가·행동 흐름

```text
PPE 잡기
→ 모델을 돌려 외형 관찰
→ PPE 위에 행동 패널 표시
→ 사용자가 사용 또는 폐기 선택
   ├─ 깨끗한 PPE + 사용: 착용 연출 및 다음 단계 진행
   ├─ 오염 PPE + 사용: 빨간 원형 X 오류 표시, 착용하지 않음
   └─ 오염 PPE + 폐기: 손에서 해제·제거 후 깨끗한 동일 PPE 배치
```

- 패널에는 맨 위에 PPE 이름을 표시하고 그 아래에 `사용`, `폐기` 버튼을 배치한다.
- 패널에는 오염 여부를 정답처럼 직접 표시하지 않는다.
- 오염품을 폐기하면 해당 PPE가 있던 씬 작성 위치에 깨끗한 동일 PPE가 나타난다.
- 한 번 오염품이 등장한 슬롯에서는 다음 제품이 반드시 깨끗하도록 결정적으로
  구성한다. 깨끗한 제품을 올바르게 사용하기 전까지 같은 시나리오 단계를 유지한다.
- 오염품에 `사용`을 선택하면 빨간 원 안에 X가 있는 오류 아이콘을 표시하고,
  착용 상태 및 다음 단계는 변경하지 않는다.

### 오염 및 하자 외형 표현 원칙

- 동일 PPE 모델에서 `Clean`과 `Contaminated` 상태를 전환할 수 있도록 URP용
  오염 셰이더 또는 셰이더 그래프를 우선 사용한다.
- 방호복은 마스크 텍스처 기반 화학물질 얼룩, 마스크는 유리 부분의 먼지·기름때
  또는 균열을 대표 시각 단서로 사용한다.
- 헬멧, 장갑, 장화 및 나머지 PPE의 정확한 하자 유형과 위치는 아트 기준을 확인한
  뒤 확정한다.
- 오염 색상, 강도, 마스크 텍스처 및 깨끗함 상태는 Material을 런타임에 복제하지
  않고 가능한 경우 `MaterialPropertyBlock`으로 인스턴스별 제어한다.
- 찢어짐이나 실제 형상 파손처럼 표면 셰이더만으로 표현하기 어려운 하자는 별도
  메시, 데칼 또는 모델 변형을 사용한다.
- 프로젝트 소유 셰이더는 Quest/OpenXR Single Pass Instanced 렌더링을 지원해야
  하며, 양안에서 위치와 외형이 동일한지 실기기로 확인한다.

### PPE 패널 작성 원칙

- 패널은 월드 스페이스 Canvas 프리팹 또는 씬 작성 오브젝트로 구성한다.
- 패널 위치, 크기, 앵커, 글꼴, 색상, 버튼 간격, 오류 아이콘 및 표시 시간은
  프리팹과 Inspector의 직렬화 값을 원본으로 사용한다.
- 런타임 코드는 패널의 표시 상태, PPE 이름, 버튼 활성 상태와 오류 피드백만
  변경하며 작성된 레이아웃 값을 덮어쓰지 않는다.
- PPE를 잡은 즉시 표시할지, 일정 관찰 시간 뒤 표시할지는 Inspector에서 선택할 수
  있도록 한다. 관찰을 유도하기 위해 짧은 지연을 두는 방식을 우선 검토한다.
- 잡기 Collider와 UI 상호작용 레이어를 분리하여, 반대 손의 Ray 또는 Poke로 버튼을
  눌러도 잡은 PPE가 의도치 않게 떨어지지 않도록 한다.

### 깨끗한 PPE 사용 연출

- `사용`이 승인되면 현재 XR 선택을 안전하게 해제하고 Rigidbody와 Collider의
  상호작용을 잠근 뒤, 손에 들린 현재 월드 Pose에서 착용 Anchor까지 이동한다.
- 이동 도중 PPE는 서서히 사라지고, 연출 완료 시 월드 PPE를 숨긴 뒤 신체 착용
  Visual 또는 손 상태를 활성화한다.
- 이동 시간, 페이드 시간, 경로, 중간 Anchor 및 Animation Curve는 PPE별 직렬화
  착용 프로필에서 설정한다.
- 방호복, 마스크 및 등지게는 손 위치에서 몸 또는 얼굴·등 Anchor로 이동하며
  사라지는 기본 착용 연출을 사용한다.
- 헬멧은 손 위치에서 머리 위 중간 Anchor로 이동한 뒤 위에서 아래로 내려와 머리에
  씌워지는 전용 연출을 사용한다.
- 왼쪽·오른쪽 장갑과 장화는 해당 손 또는 발 Anchor로 이동한 뒤 한쪽씩 사라지고,
  대응하는 착용 Visual과 상태만 갱신한다.

### 착용 완료 후 신체 부착 방향 및 거울 표현

- 한 번 사용 처리된 PPE는 착용 애니메이션이 끝난 뒤에도 신체 착용 Visual로
  유지한다. 사용자가 고개를 숙여 자신의 몸을 볼 때 방호복, 하네스, 장갑 및 확인
  가능한 장화가 해당 신체 부위에 붙어 있어야 한다.
- 관찰 중인 월드 PPE와 착용 완료 Visual은 역할을 분리한다. 월드 PPE는 사용자가
  정면을 보며 잡고 관찰하기 좋은 방향을 사용하고, 착용 Visual은 신체 표면에 맞춘
  별도의 프리팹 Pose를 사용한다.
- 방호복과 하네스는 착용 과정에서 앞면이 계속 몸을 향하는 방식으로 단순 축소하지
  않는다. 손에 들린 정면 Pose에서 회전하며 PPE의 뒷면이 사용자의 몸 앞쪽 표면을
  향하도록 붙는 동작을 기본으로 한다.
- 회전값을 런타임 코드에서 일괄적으로 180도로 고정하지 않는다. PPE마다 메시의
  전방 축이 다를 수 있으므로 `Equip Target` 또는 착용 Visual 프리팹의 작성된
  Rotation을 최종 방향의 원본으로 사용한다.
- 이동 중에는 현재 손의 월드 Rotation에서 착용 목표 Rotation까지 Animation Curve에
  따라 보간한다. 필요하면 몸 앞의 중간 Approach Anchor를 거쳐 뒷면을 몸 쪽으로
  돌린 뒤 흡착되는 두 구간 연출을 사용한다.
- 월드 PPE가 페이드아웃되는 마지막 구간과 착용 Visual이 페이드인되는 구간을
  겹쳐, 손에 든 모델이 사라지고 몸에 붙은 모델이 나타날 때 위치나 방향이 튀지
  않도록 한다.

```text
Body Anchor
├─ Head Equip Anchor
├─ Face Equip Anchor
├─ Torso Visual Root
│  ├─ Suit Equip Anchor
│  ├─ Chest Equip Anchor
│  └─ Harness Equip Anchor
├─ Left Foot Equip Anchor
└─ Right Foot Equip Anchor
```

- `Torso Visual Root`는 HMD의 상하 기울기를 그대로 따라가지 않고 수평 몸 방향을
  기준으로 유지한다. 사용자가 고개만 좌우로 돌릴 때 몸통 PPE가 즉시 같은 각도로
  돌아가지 않도록 회전 임계값 또는 완만한 추종을 적용한다.
- 사용자가 아래를 내려다볼 때 착용 Visual이 Main Camera의 Near Clip 안쪽으로
  잘리거나 시야 전체를 막지 않도록 가슴·허리 Anchor와 1인칭 메시 범위를
  Inspector에서 조정한다.
- 거울에는 별도의 가짜 상태를 만들지 않고 동일한 착용 상태가 구동하는 월드 공간
  착용 Visual을 우선 렌더링한다. 이렇게 해야 직접 내려다본 몸과 거울 속 몸의 PPE
  상태가 서로 달라지지 않는다.
- 현재 `PPE_Room_Planar_Mirror`의 `reflectedLayers`는 선택된 레이어만 반사하므로,
  신체 착용 Visual을 해당 반사 레이어에 배치하거나 Inspector에서 마스크를
  명시적으로 확장해야 한다. 거울 표면 자체의 Mirror 레이어는 계속 제외한다.
- 헬멧과 마스크처럼 HMD 카메라 가까이에 있어 1인칭 시야를 가릴 수 있는 외부
  착용 모델은 `Mirror Only` 레이어 또는 1인칭 전용 대체 Visual을 사용한다. Main
  Camera에서는 내부 메시를 숨기고 거울 카메라에서는 외부 착용 형태가 보이게 한다.
- 방호복 몸통과 하네스처럼 사용자가 직접 내려다봐야 하는 Visual은 Main Camera와
  거울 카메라 양쪽에 표시한다. 카메라별 Culling Mask와 Renderer Layer는 씬 및
  프리팹 직렬화 값으로 관리한다.
- 좌우 장갑과 테이프는 별도의 거울 전용 착용 모델을 추가하지 않는다. 장갑·테이프
  사용 결과로 전환되는 추적 손 모델 자체를 착용 상태의 원본으로 사용하고, 그 손
  모델이 거울 카메라에도 보이도록 레이어 마스크만 유지한다.
- 거울 속 방향은 실제 신체 부착 방향을 반사한 결과여야 하며, 거울 전용 모델을
  임의로 뒤집어 보정하지 않는다. 방호복의 앞·뒤, 하네스의 가슴·등 방향 및 좌우
  장갑·장화가 반사 화면에서 올바른지 확인한다.

### 손 모델 상태 및 좌우 독립 처리

왼손과 오른손은 다음 네 상태를 각각 독립적으로 가진다.

```text
Bare
BareWithSuit
GloveWithSuit
TapedGloveWithSuit
```

- 방호복 사용 완료 시 양손을 `BareWithSuit`로 전환한다.
- 왼쪽 장갑 사용 완료 시 왼손만 `GloveWithSuit`로 전환하고, 오른쪽 장갑은 별도
  사용이 완료될 때까지 기존 상태를 유지한다.
- 오른쪽 장갑도 같은 방식으로 오른손만 전환한다.
- 테이프는 아이템 하나를 한 번 사용한다. 왼손 손목에 테이핑이 먼저 나타난 뒤
  짧은 직렬화 지연 시간을 두고 오른손 손목에 나타나는 좌→우 연출을 사용한다.
- 테이프 연출이 끝나면 양손 상태를 `TapedGloveWithSuit`로 확정한다.
- 기존 맨손, 방호복 소매, 장갑, 테이프 Visual의 Transform과 Material은 프리팹 및
  Inspector 작성값을 유지하고, 런타임에서는 필요한 상태의 활성 여부만 전환한다.

### 확정된 착용 순서

```text
방호복
→ 헬멧
→ 왼쪽 장갑
→ 오른쪽 장갑
→ 왼쪽 장화
→ 오른쪽 장화
→ 마스크
→ 테이프(왼손 → 오른손)
→ 등지게
→ 전체 착용 완료
```

- 오염품 폐기와 깨끗한 교체품 사용은 같은 단계 안에서 처리한다.
- 깨끗한 PPE의 착용 연출과 상태 갱신이 완료되어야 다음 PPE 단계가 활성화된다.
- 순서 위반, 오염품 사용 및 필요한 좌우 부위 미완료 상태는 시나리오 진행을
  변경하지 않고 오류 피드백과 결과 기록만 남긴다.

### 장화 및 신체 Anchor 제약

- 기본 OpenXR 구성은 머리와 손을 직접 추적하지만 발 위치는 직접 제공하지 않는다.
- 별도 신체 추적을 사용하지 않는 1차 구현에서는 HMD 기반 Body Anchor 아래에
  Inspector로 작성한 왼발·오른발 추정 Anchor를 사용한다.
- 장화 착용 상태와 좌우 연출은 구현할 수 있으나 실제 사용자 발과의 정밀 일치는
  Quest 실기기 테스트로 오프셋을 조정해야 한다.
- 이후 Meta Body Tracking 등을 도입하면 동일 착용 슬롯이 실제 발 Anchor를
  선택적으로 사용하도록 확장한다.

### 영향 범위

- 런타임: PPE 정의 및 상태, 시나리오 순서, 배치 슬롯, 관찰 판정, 사용·폐기,
  착용 애니메이션, 손별 외형 상태 및 오류 피드백 관리가 추가된다.
- 씬·프리팹: PPE별 Clean/Contaminated Visual, 원위치 Anchor, 신체 착용 Anchor,
  월드 패널, 오류 아이콘 및 착용 완료 Visual 구성이 필요하다.
- 렌더링: 오염 마스크와 페이드가 가능한 XR 호환 URP 셰이더가 필요하다.
- XR 입력: 잡기와 UI 선택이 충돌하지 않도록 Interaction Layer와 UI 입력 경로를
  함께 검증해야 한다.
- 시나리오: PPE별 등장 상태와 `오염품 다음은 깨끗한 제품` 규칙을 결정적으로
  관리하고 선택 결과를 기록해야 한다.

### 완료한 검증

- 현재 방호복 Grab은 양손 Near-Far Interactor의 기존 `StateChange`를 방호복을
  가리키는 동안에만 `Toggle`로 전환하고 원복하는 것을 Play Mode에서 확인했다.
- 방호복을 임의 위치·회전·크기로 옮기고 Rigidbody 속도를 준 뒤 놓기 처리를
  실행했을 때 부모, Local Transform, 이동·회전 속도가 모두 원래 씬 작성값으로
  복구되는 것을 확인했다.
- `Assembly-CSharp.csproj` 빌드는 오류 0개로 완료됐다. 기존 패키지 참조 및 source
  generator 경고는 남아 있다.
- 본 절에서 합의한 오염 셰이더, 행동 패널, Clean 교체, 착용 연출 및 전체 상태
  머신은 아직 구현 전이며 설계 합의만 완료된 상태다.

### 아직 필요한 결정 및 수동 검증

- 헬멧, 장갑, 장화 등 각 PPE의 정확한 오염·하자 종류와 아트 기준을 확정한다.
- 깨끗한 PPE에 `폐기`를 선택했을 때 오류로 처리할지, 허용하되 감점할지 결정한다.
- 패널 등장 지연 시간과 사용자 시야를 가리지 않는 PPE별 위치를 Quest에서
  조정한다.
- 얼굴, 머리, 가슴, 등, 왼발 및 오른발 착용 Anchor의 위치를 사용자 키가 다른
  조건에서 검증한다.
- 방호복과 하네스가 손의 정면 Pose에서 회전하여 뒷면이 몸 쪽을 향한 상태로
  자연스럽게 부착되는지 확인한다.
- 사용자가 고개를 숙였을 때 착용된 방호복과 하네스가 몸에 남아 보이고, 머리의
  상하 기울기나 좌우 회전에 몸통 전체가 부자연스럽게 붙어 돌지 않는지 확인한다.
- 거울에서 방호복, 헬멧, 좌우 장화, 마스크 및 등지게가 현재 착용 상태와 동일하게
  표시되는지 확인한다. 장갑과 테이프는 별도 외부 Visual이 아니라 전환된 좌우 손
  모델이 반사되고 손의 좌우 방향이 올바른지 확인한다.
- 헬멧과 마스크의 거울용 외부 Visual이 Main Camera 시야를 가리지 않으며, 거울
  카메라의 Culling Mask에는 포함되는지 확인한다.
- 페이드 및 오염 셰이더가 Quest 양안에서 정상 렌더링되고 목표 성능을 유지하는지
  확인한다.
- 한 손으로 PPE를 들고 반대 손으로 `사용`과 `폐기` 버튼을 안정적으로 누를 수
  있는지 손 추적과 컨트롤러 모드에서 각각 확인한다.
- 좌우 장갑·장화 및 테이프 좌→우 연출 도중 추적 손실이나 시나리오 재시작이
  발생해도 상태가 중복 적용되지 않는지 확인한다.

### 구현 우선순위

1. 방호복 1종으로 Clean/Contaminated 외형, 관찰 패널, 사용 오류 및 폐기 후 Clean
   교체까지 수직 흐름을 완성한다.
2. 방호복 사용 연출과 양손 `BareWithSuit` 전환을 연결한다.
3. 왼쪽·오른쪽 장갑의 개별 사용과 손별 `GloveWithSuit` 전환을 구현한다.
4. 헬멧, 마스크 및 등지게의 착용 Anchor와 전용 이동·페이드 연출을 구현한다.
5. 왼쪽·오른쪽 장화와 추정 발 Anchor를 연결한다.
6. 테이프 한 개 사용 후 왼손→오른손 순차 테이핑 연출을 구현한다.
7. 전체 착용 순서, 오류 결과 기록, 재시작 및 결정적 오염→Clean 교체 규칙을
   통합 검증한다.

---

## 20. 2026-08-04 방호복 관찰·장착 Visual 역할 연결

### 적용한 변경

- `PPEItemType`에 `HazmatSuit`를 추가했다.
- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`의 `hazmat_suit_off`에
  `PPEItemIdentity`와 `PPEItemPresentationBinding`을 직렬화했다.
- 현재 씬의 역할 오브젝트는 `PPE/hazmat_suit_off`와
  `PPE/hazmat_suit_on` 아래에 함께 정리되어 있다.
- `inspectionVisual`은 사용자가 잡고 관찰하는 `hazmat_suit_off`,
  `equippedVisual`은 모자가 펴진 장착·거울 확인용 `hazmat_suit_on`을 참조한다.
- 초기 상태는 현재 시나리오 검증 기준인 `Contaminated`로 저장했다. 이번 단계에서는 상태 전환,
  오염 표현, UI 또는 착용 애니메이션을 실행하지 않는다.
- `PPEHazmatRoleBindingSetup`에 최초 연결 메뉴와 검증 메뉴를 추가했다. 기존 연결이
  있으면 작성값을 다시 덮어쓰지 않고 검증만 수행한다.

### 근본 원인과 역할 구분

- 현재 씬의 `PPE` 루트 아래 PPE와 `hazmat_suit_off`는 사용자가 직접 잡고 관찰하는
  월드 오브젝트다.
- `hazmat_suit_on`은 Grab 또는 제자리 복귀 대상이 아니라, 사용 승인 후 신체에
  장착하고 직접 시야와 거울에 표시할 Visual이다.
- 두 모델을 이름 검색이나 런타임 생성으로 연결하지 않고 씬의 직렬화 참조를
  원본으로 사용해 역할이 뒤바뀌지 않도록 했다.
- 검증도 고정 Hierarchy 경로 대신 `PPEItemPresentationBinding`의 직렬화 참조를
  직접 읽으므로, 작성자가 PPE 루트를 이동해도 역할 검증이 유지된다.

### 영향 범위

- 방호복 종류 식별과 `hazmat_suit_off/on` 역할 참조만 추가됐다.
- 기존 `PPEMarkerToggleGrab`, 사용자가 저장한 PPE 아이템 마커, Transform 및 다른
  PPE 오브젝트는 변경하지 않는다.

### 완료한 검증

- `hazmat_suit_off`는 활성 상태이며 기존 `XRGrabInteractable`과
  `PPEMarkerToggleGrab`을 유지하는 것을 확인했다.
- `hazmat_suit_on`은 초기 비활성 상태이고 Grab·복귀 컴포넌트가 없는 것을 확인했다.
- 씬 YAML에서 `HazmatSuit`, inspection 및 equipped 참조의 fileID와 GUID가 각각
  한 번만 연결된 것을 확인했다.
- 새 런타임 및 Editor 소스를 포함한 `Assembly-CSharp-Editor.csproj` 빌드는 오류
  0개로 완료됐다. 경고 11개는 기존 패키지와 기존 코드에서 발생한다.

### 아직 필요한 수동 검증

- Play Mode를 완전히 종료하고 Unity의 스크립트 가져오기와 도메인 리로드가 끝날
  때까지 기다린다.
- `Tools > PPE > Validate Hazmat Role Binding (HandTest Scale)`을 실행해 실제 씬
  오브젝트 참조가 통과하는지 확인한다.
- 검증이 끝날 때까지 새 Play Mode 또는 Quest Link 실행을 시작하지 않는다.

---

## 21. 2026-08-04 방호복 관찰 상태 연결

### 적용한 변경

- `hazmat_suit_off`에 `PPEInspectionState`를 추가했다.
- 초기 `CurrentCondition`은 `PPEItemPresentationBinding`의 씬 작성값을 사용한다.
- `XRGrabInteractable`의 선택 시작과 종료에 따라 `IsBeingInspected`를 전환한다.
- 두 손 선택과 선택 해제 순서에 의존하지 않도록 선택 중인 Interactor를 별도로
  추적한다.
- `SetCondition`과 `ResetToInitialCondition`을 제공하되, 이번 단계에서는 Material,
  오염 외형, UI 또는 착용 상태를 변경하지 않는다.
- 수동 검증을 위해 `logStateChanges`를 씬에서 활성화했다. 이 값은 Transform이나
  Material을 변경하지 않고 관찰 시작·종료만 Console에 기록한다.

### 근본 원인

- Grab 성공 여부만으로는 사용자가 현재 PPE를 관찰 중인지, Clean 또는
  Contaminated 상태인지 다음 UI 단계에서 안정적으로 판단할 수 없다.
- XRI 내부 선택 목록의 이벤트 갱신 순서에 의존하지 않는 별도 상태 계층이 필요하다.

### 영향 범위

- `PPE/hazmat_suit_off`의 런타임 관찰·오염 상태 데이터에만 영향을 준다.
- imported FBX 재질, 아이템 마커, Grab 복귀, `hazmat_suit_on` 및 다른 PPE에는
  영향을 주지 않는다.

### 완료한 검증

- Unity가 스크립트를 가져와 도메인 리로드를 완료했으며 컴파일 오류가 없었다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드는 오류 0개로 완료됐다. 경고
  7개는 기존 코드에서 발생한다.
- 씬 YAML에서 `PPEInspectionState`, presentation 및 Grab 참조가 각각 한 번만
  연결된 것을 확인했다.
- Unity Play Mode에서 방호복을 잡고 놓았을 때 관찰 시작·종료 로그와 기존 작성
  위치 복귀가 정상 동작하는 것을 사용자가 확인했다.
- 검증 후 사용자가 `hazmat_suit_off`와 자식 아이템 마커의 위치를 다시 작성했다.
  숫자를 코드에 복사하지 않고 현재 씬의 직렬화 Transform을 새 복귀 기준으로
  유지한다.
- 새 위치 작성 후 시작한 Play Mode에서도 방호복을 잡고 놓았을 때 이동된 현재 씬
  위치로 정상 복귀하는 것을 사용자가 확인했다.

### 아직 필요한 수동 검증

- 오염 외형을 적용한 뒤 Clean/Contaminated 상태 전환과 관찰 로그를 함께 확인한다.

---

## 22. 2026-08-04 방호복 Clean/Contaminated 외형 연결

### 적용한 변경

- `Assets/Textures/PPE/HazmatSuit_ChemicalStainMask.png`에 방호복용 흑백 화학 액체
  얼룩 마스크를 추가했다. 생성 원본은 1254×1254이며 Unity 가져오기 최대 크기는
  Quest를 고려해 1024로 직렬화했다.
- imported FBX 재질을 수정하지 않고 프로젝트 전용
  `Assets/Materials/PPE/HazmatSuit_Inspection.mat`을 만들었다.
- `Assets/Shaders/PPEHazmatContamination.shader`는 기존 노란 BaseMap 위에 마스크의
  농도에 따라 올리브색과 짙은 갈색 얼룩을 합성한다.
- `PPEConditionAppearance`는 `PPEInspectionState.ConditionChanged`를 구독하고
  `MaterialPropertyBlock`의 `_ContaminationStrength`만 변경한다.
- 씬의 `PPE/hazmat_suit_off` Renderer와 상태 컴포넌트를 직렬화 참조로 연결했다.
  초기 `Clean` 강도는 0, `Contaminated` 강도는 1이다.
- Play Mode 수동 확인을 위해 `PPEInspectionState` Inspector에 항상 보이는
  `Set Clean`과 `Set Contaminated` 버튼을 추가했다. 컴포넌트 메뉴에는
  `Set Clean (Debug)`와 `Set Contaminated (Debug)`도 제공한다.
- 선택 상태와 무관하게 실행할 수 있도록 `Tools > PPE > Set Hazmat Clean
  (Play Mode)`과 `Set Hazmat Contaminated (Play Mode)` 메뉴도 추가했다.
- `Tools > PPE > Validate Hazmat Condition Appearance (HandTest Scale)` 검증 메뉴를
  추가했다.

### 근본 원인

- Clean/Contaminated 판정 데이터만으로는 사용자가 관찰 중인 방호복의 상태를
  시각적으로 구분할 수 없다.
- imported 재질 자체를 바꾸거나 상태마다 Material 인스턴스를 만들면 다른 모델에
  영향이 전파되거나 런타임 재질이 누적될 수 있다. 전용 재질과 Renderer별
  `MaterialPropertyBlock`으로 상태값만 분리했다.
- Transform, 아이템 마커 및 복귀 기준은 외형 상태와 무관해야 하므로 외형
  컴포넌트는 위치·회전·크기·활성 상태를 변경하지 않는다.

### 영향 범위

- 이번 단계는 `PPE/hazmat_suit_off`의 Clean/Contaminated 외형에만 영향을 준다.
- `hazmat_suit_on`, 장착 연출, 사용·폐기 UI, 교체품 생성 및 다른 PPE는 변경하지
  않는다.
- 셰이더의 Vertex 입력·출력에는 Single Pass Instanced용 인스턴스·스테레오 매크로를
  적용했다.

### 완료한 검증

- Unity가 새 텍스처, 셰이더, 재질 및 스크립트를 가져오고 도메인 리로드를 완료했다.
- Editor 로그에서 `PPEHazmatContamination.shader` 가져오기가 성공했으며 관련 Shader
  오류와 C# 컴파일 오류가 없음을 확인했다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드는 오류 0개로 완료됐다. 경고
  7개는 기존 코드에서 발생한다.
- 씬 YAML에서 `PPEConditionAppearance`, 상태, 대상 Renderer 및 전용 재질 참조가
  각각 한 번만 연결된 것을 확인했다.

### 아직 필요한 수동 검증

- Unity Play Mode에서 `PPEInspectionState` Inspector와 `Tools > PPE` 전환 경로를
  사용해 `Clean`과 `Contaminated` 외형이 모두 정상 표시되는 것을 사용자가
  확인했다.
- 두 상태에서 방호복을 잡고 놓아 현재 씬 작성 위치와 자식 아이템 마커 위치가
  그대로 복구되는지 확인한다.
- Quest/OpenXR 양안에서 얼룩이 같은 위치에 표시되고 한쪽 눈에서 사라지지 않는지
  확인한다.

---

## 23. 2026-08-04 방호복 사용·폐기 선택 UI 연결

### 적용한 변경

- `PPE/hazmat_suit_off` 아래에 초기 비활성 상태의 `Hazmat Action Panel`을 씬
  오브젝트로 작성했다.
- 패널은 World Space Canvas, `TrackedDeviceGraphicRaycaster`, 한글
  `Pretendard-Medium SDF`, PPE 이름 `방호복`, `사용` 및 `폐기` 버튼으로 구성했다.
- 패널의 Local Transform, Canvas 크기, 배율, 정렬 순서, 글꼴, 글자 크기, 색상,
  버튼 배치 및 피드백 문구는 씬 직렬화 값으로 저장했다.
- `PPEActionPanelController`는 방호복을 잡아 관찰하기 시작하면 씬 작성 지연값
  0초에 따라 즉시 패널을 표시하고, 관찰이 끝나면 패널을 숨긴다.
- 최초 수동 확인에서 패널이 너무 크고 가로형으로 보였으며 방호복 뒤쪽 하단에
  나타났다. 이후 사용자가 `IMG/선택패널위치상태.png`를 기준으로 패널과 내부
  요소의 최종 위치를 직접 작성했다.
- 현재 패널은 320×348.8938 세로형, 배율 0.00085이며 `사용`과 `폐기` 버튼을
  위아래로 배치했다. 패널 회전, 위치 및 모든 자식 RectTransform은 현재 씬 작성값을
  원본으로 유지한다.
- 사용자가 추가한 `Icons` 부모 아래에는 사용 행과 폐기 행 각각의 `Pass Icon`과
  `Error Icon`이 배치되어 있다.
- 패널 배경, PPE 이름, 사용·폐기 버튼과 `Icons` 부모는 고정 콘텐츠로 활성화하고,
  패널 부모, 결과 문구 및 네 Pass/Error 루트만 초기 비활성 상태로 저장했다.
- 결과별 아이콘 연결은 `Clean + 사용 → 사용 Pass`,
  `Contaminated + 사용 → 사용 Error`,
  `Contaminated + 폐기 → 폐기 Pass`,
  `Clean + 폐기 → 폐기 Error`로 구성했다. 결과가 바뀔 때 네 아이콘을 모두 숨긴
  뒤 해당 아이콘 하나만 표시한다.
- `PPEMarkerToggleGrab`은 선택 중인 Interactor를 별도로 추적해 방호복 이동으로 Hover가
  종료돼도 Toggle 입력 방식을 유지한다. 두 번째 Grip으로 실제 선택이 끝날 때만 원래
  입력 방식과 씬 작성 위치로 복귀한다.
- Toggle 선택 중에는 같은 컨트롤러 아래의 활성 `HandGripAnimator`에 Grip 유지 소스를
  등록해 물리 Grip 버튼을 놓아도 손 모델이 잡은 자세를 유지한다.
- 선택 Interactor의 handedness와 같은 손의 Trigger는 `사용`, 오른손 A 버튼은
  `폐기`로 직접 처리한다. 화면의 `Button`은 위치와 시각 표현을 유지하지만
  Ray/Poke 클릭을 결과 입력의 필수 경로로 사용하지 않는다.
- `UseApproved`와 `DiscardApprovedContaminated`는 PASS 피드백을 0.25초 표시한 뒤
  XRI 선택을 먼저 정상 해제하고, `selectExited`에서 씬 작성 위치 복귀가 끝난 다음
  프레임에 결과를 적용한다.
- 버튼 선택 결과는 `PPEActionChoice`와 `PPEActionResult`로 구분하고
  `ChoiceResolved` 이벤트와 Console 로그로 제공한다.
- 오염 방호복에서 `사용`을 선택하면 `UseRejectedContaminated`와 빨간 원형 X
  피드백을 표시한다.
- Clean 방호복의 `사용`은 `UseApproved`로 판정하고 선택 해제 뒤 관찰용 모델을
  비활성화한다. 오염 방호복의 `폐기`는 `DiscardApprovedContaminated`로 판정하고
  같은 관찰용 모델을 원위치로 복귀시킨 뒤 `Clean` 상태로 전환한다.
- 아직 정책이 확정되지 않은 Clean 방호복의 `폐기`는
  `DiscardCleanPolicyPending`으로 기록하고 폐기 Error 아이콘을 표시하되 시나리오
  상태를 변경하지 않는다.
- `Tools > PPE > Validate Hazmat Action Panel (HandTest Scale)` 검증 메뉴를
  추가했다. 검증은 표시 지연 0초, 세로형 패널 및 `사용`-`폐기`의 수직 배치도
  확인한다. 또한 네 아이콘 참조가 서로 다르고 패널 아래에 존재하며 각 행의
  Pass/Error 위치가 정렬됐는지 확인한다.

### 근본 원인

- 오염 외형만으로는 사용자가 판단한 행동을 시나리오가 받을 수 없으므로, 잡은
  상태에서 반대 손으로 선택할 수 있는 명시적인 행동 UI와 결과 계약이 필요하다.
- 패널을 런타임에 생성하면 위치와 레이아웃이 코드 기본값에 종속되므로 씬 작성
  오브젝트로 구성하고 런타임은 활성 상태와 동적 피드백만 변경하도록 분리했다.
- 최초 패널의 지연값과 RectTransform이 실제 잡기 흐름 및 방호복 로컬 좌표계에
  맞지 않아 표시가 늦고 크기·방향·위치가 의도와 달랐다. 런타임 Transform 보정은
  추가하지 않고 사용자가 최종 작성한 씬 값을 유지했다.
- 아이콘은 씬에 배치된 것만으로는 선택 결과에 따라 바뀌지 않으므로, 각 행의
  Pass/Error 오브젝트를 컨트롤러에 개별 직렬화하고 활성 상태만 전환하도록 했다.
- XRI의 Hover 종료 이벤트와 선택 목록 갱신 순서가 겹치면 선택 중에도 원래
  `StateChange` 입력으로 복원될 수 있었다. 컴포넌트 자체 선택 집합을 기준으로
  복원 시점을 결정해 패널의 관찰 상태도 두 번째 Grip까지 유지되도록 했다.
- 손 모델은 물리 Grip 축만 읽고 있었으므로 Toggle 선택의 논리 상태와 시각 자세가
  달라졌다. 물리 입력은 그대로 유지하면서 선택 중 외부 Grip 소스를 우선 반영한다.
- 선택 중인 `hazmat_suit_off`를 바로 비활성화하면 XRI의 강제 `selectExited` 처리
  안에서 원위치 복귀용 `Transform.SetParent()`가 실행돼 GameObject 활성 상태 변경이
  재진입한다. 승인 처리 순서를 XRI 선택 취소, 원위치 복귀 완료, 결과 적용 순으로
  분리해 이 예외를 방지한다.

### 영향 범위

- `PPE/hazmat_suit_off`의 관찰 중 패널 표시와 사용·폐기 선택 결과에만 영향을 준다.
- 기존 방호복 Transform, 아이템 마커, 복귀 동작, 오염 외형 및
  `hazmat_suit_on`은 변경하지 않는다.
- 오염 폐기 후 Clean 교체는 동일한 `hazmat_suit_off`의 상태 전환으로 이번 단계에
  연결했다. Clean 사용 후 `hazmat_suit_on` 장착 연출은 다음 구현 단계다.

### 완료한 검증

- 새 패널 UI의 모든 씬 fileID 정의가 중복 없이 존재하고 내부 참조에 누락이 없음을
  텍스트 검증했다.
- 새 UI fileID 범위를 기존 씬 오브젝트 뒤와 `SceneRoots` 앞에 오름차순으로 배치해
  Unity 씬 역직렬화 순서를 맞췄다.
- 방호복과 기존 두 아이템 마커의 Transform 값이 패널 수정 전 작성값과 동일함을
  다시 확인했다.
- 패널 수정 후 씬 YAML에서 UI fileID 정의 중복 0개, 내부 참조 누락 0개 및
  수정 대상 직렬화 값이 각각 한 번만 존재함을 다시 확인했다.
- 사용자가 작성한 패널, 내부 요소, `Icons` 부모 및 네 아이콘의 Transform을 바꾸지
  않고 결과별 GameObject 참조만 연결했음을 확인했다.
- 네 아이콘 참조가 각각 한 번만 직렬화됐고 참조 대상 GameObject가 모두 존재하며
  패널 초기 비활성 상태가 유지됨을 확인했다.
- 초기 상태가 `Contaminated`이고 Toggle Grab 및 손 Grip 유지 설정이 활성화됐는지
  `PPEHazmatRoleBindingSetup` 검증에 추가했다.
- 패널 표시 지연 0에서는 관찰 시작 이벤트와 같은 프레임에 `SetActive(true)`가
  실행되며 상태 분기와 무관함을 코드 경로에서 확인했다.
- 왼쪽·오른쪽 Near-Far Interactor의 handedness가 각각 Left·Right로 저장돼 있어
  선택한 손 Trigger 입력을 구분할 수 있음을 확인했다.
- 고정 패널 자식 활성 상태, 동적 피드백·아이콘 초기 비활성 상태 및 PASS 제거 지연
  0.25초를 씬 YAML에서 확인했다.
- `PPEActionPanelController`와 보강한 Editor 검증 코드를 포함한
  `Assembly-CSharp-Editor.csproj --no-restore` 빌드는 오류 0개로 완료됐다. 경고
  7개는 기존 샘플 및 프로젝트 코드에서 발생한다.
- 폐기 PASS에서 발생한 GameObject 활성 상태 변경 재진입 예외의 호출 경로를 확인하고,
  선택 중 루트에 직접 `SetActive(false)` 하던 경로를 제거했다. 수정 후 같은 전체
  빌드를 오류 0개로 다시 완료했다.
- 사용자가 Quest/OpenXR Play Mode에서 `오염 → 폐기 → Clean 원위치 재생성 → 사용 →
  손에서 제거`까지 연속 수행했으며, 승인 처리 재진입 예외와 모래시계가 재발하지
  않았음을 확인했다.

### 아직 필요한 수동 검증

- Unity에서 `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`를 디스크 기준으로 다시
  열고 `Tools > PPE > Validate Hazmat Action Panel (HandTest Scale)`을 실행한다.
- Play Mode에서 방호복을 잡는 즉시 패널이 표시되고 놓으면 숨는지
  확인한다.
- 물리 Grip을 한 번 눌렀다가 놓아도 방호복 선택, 손 모델 Grip 자세 및 패널 표시가
  유지되고 두 번째 Grip에서만 모두 종료되는지 확인한다.
- Play Mode 최초 프레임부터 방호복이 Contaminated 외형인지 확인한다.
- 왼손으로 잡았을 때 왼손 Trigger, 오른손으로 잡았을 때 오른손 Trigger에서만
  `사용`이 실행되는지 확인한다.
- 잡은 손과 무관하게 오른손 A에서 `폐기`가 실행되는지 확인한다.
- Clean + 사용, Contaminated + 사용, Contaminated + 폐기, Clean + 폐기의 Console
  결과가 각각 `UseApproved`, `UseRejectedContaminated`,
  `DiscardApprovedContaminated`, `DiscardCleanPolicyPending`과 일치하는지 확인한다.
- 네 경우에 사용 Pass, 사용 Error, 폐기 Pass, 폐기 Error가 각각 하나씩만 표시되고
  방호복을 새로 잡을 때 모든 아이콘이 초기화되는지 확인한다.
- 실패 결과에서는 방호복이 계속 손에 남는지 확인한다.
- 패널이 `IMG/선택패널위치상태.png`와 같은 위치와 크기로 표시되는지 확인한다.
- Quest/OpenXR 양안에서 패널과 Pass/Error 아이콘이 동일하게 표시되는지 확인한다.

---

## 24. 2026-08-04 방호복 착용 연출 1단계

### 적용한 변경

- `PPEHazmatEquipController`를 활성 `PPE` 루트에 배치하고 기존
  `PPEActionPanelController.UseApproved` 결과를 구독하도록 연결했다.
- Clean 방호복의 사용 PASS가 발생하면 비활성 씬 원본 `hazmat_suit_on`을 직접
  이동하거나 활성화하지 않고 런타임 복제본을 생성한다.
- 런타임 복제본은 잡고 있던 `hazmat_suit_off`의 월드 Pose와 크기에서 시작해,
  직렬화된 Approach Offset을 거쳐 직렬화된 몸쪽 Pose로 이동·회전·크기 보간한다.
- 착용이 끝난 런타임 Visual은 `PPE Body Anchor`가 Main Camera의 위치와 수평 Yaw를
  기준으로 따라가고, 자식 `Hazmat Suit Equip Anchor`의 작성 Pose를 최종값으로 사용한다.
  HMD Pitch와 Roll은 몸통 방향에 적용하지 않고 Yaw에는 직렬화된 완화 시간을 적용한다.
- 연출 시간, 시간 기준, Approach/Equip Anchor Transform, 두 구간 분리 시점 및
  Animation Curve는 모두 씬 직렬화 값으로 저장했다. Play Mode에서도 Equip Anchor의
  Position·Rotation·Scale을 바꾸면 착용 Visual이 즉시 따라오므로 사용자가 직접 조정할
  수 있다.
- 첫 구현의 몸 Pose는 숫자 필드로 머리에서 0.85m 아래, 0.28m 앞을 사용했으나 모델
  원점이 발 쪽이라 방호복 전체가 시야 앞쪽 위에 나타났다. 초기 Equip Anchor를 머리
  기준 1.6m 아래, 0.08m 뒤로 보정했다.
- 사용 PASS 직후 큰 `hazmat_suit_on`이 잡은 모델과 겹쳐 이동이 보이지 않던 문제를
  줄이기 위해 PASS 지연 동안 시작 Pose를 보관하고, 선택 해제 다음 프레임부터 런타임
  Visual을 생성해 1.25초 연출을 시작한다.
- 이번 1단계에서는 기존 `LeftHand_BareHand`와 `RightHand_BareHand`를 유지하며,
  `BareHand_Suit` 양손 전환은 착용 Visual의 위치·방향 확인 뒤 연결한다.
- `Tools > PPE > Validate Hazmat Equip Stage 1 (HandTest Scale)` 읽기 전용 검증을
  추가했다.

### 근본 원인

- 기존 `hazmat_suit_on`은 장착용 외형으로만 역할이 구분됐고, Clean 사용 결과와
  착용 이동 또는 몸쪽 추종 상태가 연결되지 않았다.
- `hazmat_suit_on` 씬 오브젝트를 직접 이동하면 Play Mode 최종값 유지 환경에서 벽 쪽
  작성 Transform과 초기 비활성 상태가 저장될 위험이 있다. 따라서 씬 원본은 템플릿으로
  유지하고 런타임 복제본만 연출한다.
- 전체 방호복 메시의 원점을 몸 중심으로 잘못 가정하고 머리 기준 앞쪽 오프셋을 적용한
  것이 시야 차단의 원인이었다. 또한 계산된 Pose를 매 프레임 적용해 사용자가 Transform을
  직접 조절할 대상도 없었다. Yaw 추종 부모와 작성 가능한 Equip Anchor를 분리했다.
- 씬에는 양손 방호복 소매 모델이 이미 있으므로 새 손 모델 생성보다 기존 작성 모델의
  활성 상태 전환이 맞지만, 착용 연출과 동시에 연결하면 위치·방향 문제와 손 전환 문제를
  분리하기 어렵다. 두 검증 단계를 나눴다.

### 영향 범위

- Clean 방호복 사용 PASS 이후 `hazmat_suit_on` 런타임 착용 Visual에만 영향을 준다.
- 원본 `hazmat_suit_on`, `hazmat_suit_off`, 패널, 아이콘 및 양손 모델의 작성 Transform과
  초기 활성 상태는 변경하지 않는다.
- 런타임에서 갱신하는 것은 빈 `PPE Body Anchor`뿐이며 Play Mode 종료 시 작성 Pose로
  복구한다. 사용자가 조정하는 Equip/Approach Anchor의 로컬 Transform은 덮어쓰지 않는다.
- 런타임 Visual은 기본 레이어 0을 유지하므로 현재 Main Camera 전체 마스크와 거울
  `reflectedLayers` 마스크 51에 모두 포함된다.

### 완료한 검증

- `PPEHazmatEquipController`와 `PPEHazmatEquipValidation`을 포함한
  `Assembly-CSharp-Editor.csproj --no-restore` 빌드를 오류 0개로 완료했다. 경고
  7개는 기존 샘플 및 프로젝트 코드에서 발생한다.
- 씬에서 착용 컨트롤러, 기존 Action Panel, Hazmat Presentation Binding 및 XR Main
  Camera 참조가 각각 한 번 연결됐음을 확인했다.
- 원본 `hazmat_suit_on`은 초기 비활성 상태와 기존 Transform을 유지하며, 양손 기본
  모델은 활성, `BareHand_Suit` 모델은 비활성 상태임을 확인했다.
- 첫 Play Mode에서 사용자는 몸쪽 방호복이 눈앞 위쪽에 나타나 시야를 가리고 연출이
  보이지 않음을 `Img/입은방호복.png`로 확인했다. 로그에는 연출 완료가 남아 있어 입력
  연결 문제가 아니라 잘못된 최종 Pose와 겹침 시점 문제임을 확인했다.
- Anchor 기반 보정과 PASS 이후 시작 순서로 변경한 뒤 전체 Editor 빌드를 다시 오류
  0개로 완료했다.

### 아직 필요한 수동 검증

- Unity가 외부 변경된 씬을 디스크 기준으로 다시 연 뒤
  `Tools > PPE > Validate Hazmat Equip Stage 1 (HandTest Scale)`을 실행한다.
- `오염 → 폐기 → Clean 재생성 → 사용` 뒤 `hazmat_suit_on (Runtime Equipped)`이 손
  위치에서 몸 쪽으로 이동·회전하는지 확인한다.
- 연출 완료 후 고개를 아래로 향하면 몸쪽 방호복이 보이고 정면 시야를 가리지 않으며,
  Yaw 회전에는 완만하게 따라오고 Pitch에 붙어 같이 기울지 않는지 확인한다.
- Play Mode에서 `Hazmat Suit Equip Anchor`의 로컬 Position·Rotation·Scale을 변경했을
  때 착용 Visual이 즉시 따라오고 최종 조정값을 저장할 수 있는지 확인한다.
- 거울에서 같은 착용 Visual이 보이는지 확인한다.
- 몸쪽 위치·회전·크기는 Quest에서 사용자가 확정했으며, 양손 `BareHand_Suit`
  전환 결과는 다음 절에 기록한다.

---

## 25. 2026-08-04 방호복 착용 후 손 모델 전환

### 적용한 변경

- 방호복 착용 연출이 완료되면 `LeftHand_BareHand`와 `RightHand_BareHand`를 숨기고
  `LeftHand_BareHand_Suit`와 `RightHand_BareHand_Suit`를 활성화하도록 연결했다.
- 새 손 모델을 먼저 활성화한 뒤 기존 손 모델을 숨겨 전환 프레임에 손이 통째로
  사라지는 현상을 방지했다.
- 손 모델 참조와 초기 활성 상태는 씬의 직렬화 값으로 관리하며, 런타임 코드는
  착용 상태에 따라 활성 여부만 전환한다.

### 근본 원인

- 방호복 몸통 착용 Visual만 연결된 상태에서는 손이 계속 맨손으로 남아 착용 상태와
  시각 상태가 일치하지 않았다.
- 이미 씬에 방호복 소매가 포함된 양손 모델이 있으므로 별도 메시를 생성하거나 손
  Transform을 코드에서 보정할 필요가 없었다.

### 영향 범위

- 방호복 착용 완료 시점의 좌우 손 모델 활성 상태에만 영향을 준다.
- 장갑과 테이프 단계는 이후 해당 손 모델 상태를 다시 교체하는 방식으로 연결한다.
- 장갑과 테이프를 위한 별도 거울 전용 Visual은 만들지 않으며, 최종 손 모델 자체가
  거울에 반사되도록 한다.

### 완료한 검증

- 사용자가 Quest에서 방호복 착용과 양손 모델 전환이 정상 동작함을 확인했다.
- 거울에서는 손 모델이 약간 아래로 보이는 느낌이 있으나 현재 단계에서 허용 가능한
  수준으로 확인했다.
- 방호복 최종 Transform은 사용자가 확정한 씬 작성값을 유지했다.

### 아직 필요한 수동 검증

- 장갑 단계 구현 시 현재 손 모델의 손목 위치를 기준으로 좌우 장갑 모델 전환이
  튀지 않는지 다시 확인한다.

---

## 26. 2026-08-04 안전모 관찰·착용 1단계

### 적용한 변경

- 기존 `helmet` 오브젝트에 `ConstructionHelmet` 역할, 관찰 상태, Toggle Grab,
  Rigidbody, 오염 색상 표현 및 행동 패널 연결을 추가했다.
- 초기 상태는 `Contaminated`이며, 안전모의 41개 모델 Renderer에
  `MaterialPropertyBlock`의 `_BaseColor`를 적용해 Clean과 Contaminated를 구분한다.
  아이템 마커 Renderer는 색상 변경 대상에서 제외했다.
- 기존 방호복 Action Panel을 공유하되 안전모를 관찰할 때 패널 이름을 `안전모`로
  바꾸고, 안전모 자식의 `Helmet Action Panel Pose`를 따라가도록 연결했다.
- 공용 패널을 여러 PPE가 안전하게 사용할 수 있도록 활성 패널 소유자를 한 번에
  하나로 제한했다. Clean 사용 완료 시 PPE 루트 전체를 비활성화하지 않고 관찰용
  Renderer, Collider 및 `XRGrabInteractable`만 숨겨 공용 패널 호스트를 유지한다.
- Contaminated 상태에서 `사용`은 거부하고 `폐기`는 Clean 상태로 복구한다. Clean
  상태에서 `사용`이 승인되면 선택을 해제하고 관찰용 안전모를 숨긴 뒤 착용 연출을
  시작한다.
- 씬 작성 `helmet_on`을 Main Camera 자식의 최종 착용 Pose로 추가했다. 손에 든
  안전모 위치에서 `Helmet Approach Anchor`를 거쳐 `helmet_on`으로 이동하며,
  최종 위치·회전·크기는 런타임 상수가 아니라 해당 Transform의 Inspector 값을
  사용한다.
- 착용 안전모는 `Mirror Only` 레이어 30의 런타임 복제본으로 표시한다. Main Camera와
  Hand Tracking Camera에서는 이 레이어를 제외하고, 평면 거울의 `reflectedLayers`에는
  포함해 1인칭 시야를 가리지 않으면서 거울에서는 보이게 구성했다.
- `Tools > PPE > Validate Helmet Equip (HandTest Scale)` 읽기 전용 검증과 안전모
  컨트롤러 전용 Inspector 검증 버튼을 추가했다.

### 근본 원인

- 안전모는 모델과 마커만 존재하고 관찰·상태 판정·행동 결과·착용 목표가 연결되지
  않아 방호복 다음 단계로 진행할 수 없었다.
- 방호복 아래에 있던 기존 Action Panel까지 방호복 루트와 함께 비활성화하면 다음
  PPE인 안전모가 같은 패널을 사용할 수 없다. 따라서 사용 완료 처리를 루트 비활성화가
  아닌 관찰 Visual 비활성화로 분리했다.
- 머리에 직접 붙는 전체 안전모 모델을 HMD 카메라에도 렌더링하면 내부 면이 시야를
  가릴 수 있으므로 거울 전용 외부 Visual 경로가 필요했다.

### 영향 범위

- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`의 안전모 관찰 및 착용 흐름과 공용
  행동 패널 소유권 처리에 영향을 준다.
- 사용자가 옮긴 안전모 관찰 Transform
  `Position (-12.015, 1.415, -0.409)`과 다른 PPE 단스 배치는 변경하지 않았다.
- `helmet_on`의 초기 로컬 Pose는 `Position (0, 0.11, 0)`, Yaw `90.848`,
  `Scale (0.3, 0.3, 0.3)`이며, Quest 확인 후 Inspector에서 직접 조정할 수 있다.
- 장갑과 테이프의 거울 표현에는 별도 착용 모델을 추가하지 않는다. 헬멧, 마스크 및
  등지게처럼 외부 형상이 필요한 PPE만 거울용 착용 Visual 경로를 사용한다.

### 완료한 검증

- 프로젝트 Editor 어셈블리를 `--no-restore`로 빌드해 오류 0개를 확인했다. 남은
  7개 경고는 기존 코드와 패키지에서 발생한다.
- 안전모 Renderer 41개, Toggle Grab의 마커 Collider, 공용 패널 참조, 안전모 이름,
  착용 프리팹, 머리·중간 Anchor 및 Mirror Only 레이어 참조가 씬에 연결됐음을
  정적 검증했다.
- Main Camera와 Hand Tracking Camera가 레이어 30을 제외하고, 평면 거울 마스크는
  레이어 30을 포함하는 것을 확인했다.
- 씬 YAML의 신규 fileID 중복이 없고, 마지막 Unity 씬 재로드 이후 Editor 로그에
  깨진 PPtr, 스크립트 컴파일 오류 또는 누락 참조가 없음을 확인했다.

### 아직 필요한 수동 검증

- Unity에서 `Tools > PPE > Validate Helmet Equip (HandTest Scale)`을 실행한다.
- Play Mode에서 `Contaminated 사용 거부 → 폐기 → Clean → 사용` 순서와 공용 패널의
  이름·위치 전환이 정상인지 확인한다.
- 안전모가 손 위치에서 머리 위 중간 Anchor를 거쳐 착용 위치로 이동하는지 확인한다.
- 헤드셋을 착용한 상태에서 `helmet_on`의 Position·Rotation·Scale을 조정해 머리와
  거울 속 안전모가 맞는지 확정한다.
- 안전모가 HMD 양안 시야에는 나타나지 않고 거울 양안에는 정상적으로 보이는지
  Quest/OpenXR에서 확인한다.

---

## 27. 2026-08-04 가구 Transform 보호·태블릿 체크·수트 전면 착용 연출

### 적용한 변경

- 사용자가 조정한 `safety_cabinet`, `mask_locker`, `ppe_room_bench_2`,
  `hazmat_suit_hanger`, `metal_locker`의 현재 Transform을 변경하지 않은 채
  `AuthoredTransformRuntimeLock`을 연결했다.
- 보호 대상은 Edit Mode에서 계속 자유롭게 조정할 수 있다. Play Mode 진입 시점의 부모,
  Local Position, Local Rotation, Local Scale을 캡처하고 런타임 변경만 복구한다.
- `Tablet`에는 `BoxCollider`, `Rigidbody`, `XRGrabInteractable`,
  `PPEMarkerToggleGrab`을 연결했다. 놓으면 현재 씬에 저장된 부모·Local Transform으로 돌아간다.
- 태블릿을 잡은 손의 Trigger를 한 번 누르면 체크 표시 다섯 개가 순차 재생되고,
  기존 서명 세 단계까지 중단 없이 자동으로 이어진다. 시작 시 자동 재생되던 기존 흐름은 해제했다.
- `hazmat_suit_on`의 최종 Inspector Transform은 유지하고, `PPE Body Anchor` 아래에
  `Hazmat Suit Front Start Anchor`를 추가했다. 착용 연출은 손에 든 `hazmat_suit_off` 위치가
  아니라 몸 앞쪽 앵커에서 시작해 기존 Approach Anchor를 거쳐 최종 수트 Pose로 1.8초 동안 이동한다.

### 근본 원인

- 가구 값이 되돌아간 직접 원인은 가구 이름을 찾아 덮어쓰는 런타임 코드가 아니라, Unity에
  저장되지 않은 Edit Mode 변경이 있는 동안 외부 씬 수정과 강제 새로고침을 수행한 것이었다.
- 태블릿 Item Marker는 시각 표시만 존재했고 태블릿 본체에는 Grab·원위치 복귀 컴포넌트가 없었다.
- 서명 시퀀스는 `playOnEnable`로 체크와 서명을 모두 자동 재생해 Trigger 진행 상태를 구분할 수 없었다.
- 기존 수트 연출은 시작점을 잡고 있던 관찰용 수트의 월드 Pose로 사용해, 시야 밖이나 몸 가까이에서
  시작하면 애니메이션이 순간 전환처럼 보일 수 있었다.

### 영향 범위

- 가구 보호는 명시한 다섯 오브젝트에만 적용한다. 태블릿과 다른 PPE Transform은 포함하지 않는다.
- 태블릿의 현재 위치·회전·스케일, Item Marker와 문서 UI의 Transform은 변경하지 않았다.
- 수트 변경은 착용 애니메이션 시작 기준과 재생 시간에만 적용하며 `hazmat_suit_on`의 최종 Pose와
  장갑 손 모델 전환은 그대로 유지한다.

### 완료한 검증

- `Tools > PPE > Validate Static Furniture Transform Protection`과 동일한 검증에서 다섯 대상의
  부모·위치·회전·스케일 보호 설정을 모두 확인했다.
- 태블릿 검증에서 Toggle Grab, 원위치 복귀, 활성 Item Marker, 완전한 8단계 시퀀스,
  `Trigger 1회 → 체크 5개 → 서명 3개` 연결을 확인했다.
- 수트 검증에서 전면 시작 앵커가 최종 Pose보다 0.9m 앞에 있고, 최종 수트 Transform을 유지하며
  재생 시간이 1.8초로 직렬화되었음을 확인했다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`를 오류 0개로 완료했다.
  남은 경고 1개는 기존 `MixerRoomFrontCapture.cs`의 폐기 예정 API 사용이다.
- Unity Editor 로그에서 이번 변경과 관련된 C# 오류 및 예외가 없음을 확인했다.

### 아직 필요한 수동 검증

- Quest/OpenXR에서 지정한 다섯 가구가 Play Mode 진입·종료 전후에 이동하거나 스케일이 바뀌지 않는지 확인한다.
- 태블릿을 좌우 손으로 각각 잡고 해당 손 Trigger를 한 번 눌러 체크 표시 다섯 개와 서명 세 단계가 계속 재생되는지 확인한다.
- 태블릿을 놓았을 때 방호복 오른쪽의 현재 걸이 위치로 정확히 복귀하는지 확인한다.
- Clean 방호복 사용 승인 후 `hazmat_suit_on`이 몸 앞쪽에서 확실히 보인 뒤 몸으로 들어오며,
  최종 위치·회전·스케일과 장갑 손 모델 전환이 유지되는지 헤드셋에서 확인한다.

---

## 28. 2026-08-04 안전모 Prefab Variant 참조 손상 및 Quest Link 종료

### 적용한 변경

- `PPEHelmetEquipValidation`이 착용 안전모 프리팹을 특정 에셋 경로 문자열과 정확히
  비교하던 조건을 제거했다.
- 착용 프리팹 참조가 실제 Prefab Asset인지, 유효한 에셋 경로를 가지는지, 자식에
  Renderer가 존재하는지를 기준으로 검증하도록 변경했다.
- 착용 시간, Approach 구간 및 Motion Curve 중 어느 값이 실패했는지 예외 메시지에
  개별 값이 표시되도록 진단 내용을 구체화했다.
- 씬에 저장된 Variant GUID와 원본 FBX fileID의 혼합 참조를 제거하고, 동일한 안전모
  원본 FBX `GameObject`를 `AssetDatabase.LoadAssetAtPath<GameObject>`로 로드해
  `equippedVisualPrefab`에 다시 직렬화했다.
- 이후 같은 문제가 발생했을 때 Transform을 건드리지 않고 참조만 복구할 수 있도록
  `Tools > PPE > Repair Helmet Equipped Prefab Reference` 메뉴를 추가했다.

### 근본 원인

- 씬 YAML에는 Variant GUID가 있었지만 fileID는 원본 FBX의 루트 GameObject를 가리키고
  있었다. Unity가 이 조합을 실제 `GameObject`로 복원하지 못해 Inspector 직렬화 값은
  런타임에서 `null`이 되었고 `AssetDatabase.GetAssetPath`도 빈 문자열을 반환했다.
- 해당 Variant는 원본 안전모의 이름만 변경하고 메시·재질·Transform 오버라이드는
  포함하지 않으므로 런타임 복제 원본으로 FBX를 직접 참조해도 표현 결과가 동일하다.
- 검증 예외 직후 스크립트 강제 동기 컴파일과 Play Mode 진입이 연속으로 발생했다.
  이후 OpenXR 세션이 `STOPPING`, `IDLE`, `System Shutdown` 순서로 종료되어 헤드셋에는
  Unity 렌더링이 아닌 Quest Link 대기용 검정 화면과 모래시계가 남았다.

### 영향 범위

- 안전모 착용 프리팹 참조와 Editor 검증·복구 도구에 영향을 준다.
- 안전모의 씬 작성 Transform, 착용 시간, Motion Curve 및 런타임 착용 동작은 변경하지
  않는다.

### 완료한 검증

- 현재 씬의 착용 프리팹 참조가 원본 FBX GUID `eb29cbb97ed1bb6439805f0729546666`와
  루트 GameObject fileID `919132149155446097` 조합으로 저장됐음을 확인했다.
- 직렬화된 착용 시간 `0.9`, Approach 구간 `0.6`, Motion Curve의 시작값 `0`과 끝값
  `1`을 확인했다.
- Unity의 수정본 도메인 리로드가 정상 완료됐고, 참조 복구 직후
  `Helmet equip validation passed` 및 `CODEX_HELMET_PREFAB_REPAIR_PASS` 로그를 확인했다.

### 아직 필요한 수동 검증

- Quest Link를 종료해 헤드셋을 독립 실행 홈 화면으로 복귀시킨다.
- Unity에서 `Assets > Refresh`를 한 번 실행해 제거된 1회용 복구 실행 파일을 생성된
  프로젝트 목록에서도 정리하고, 컴파일·도메인 리로드가 끝날 때까지 기다린다.
- Quest Link를 다시 연결한 뒤 Play Mode에서 안전모의 위쪽 Approach Anchor를 거친
  하강 착용 연출을 확인한다.

---

## 29. 2026-08-04 Front Wall 문 유리 개구부 복구 도구

### 진단 결과

- `3_PPE_Room_HandTest_scale`, `3_PPE_Room_HandTest`, `3_PPE_Room_Loco`,
  `3_PPE_Room`은 모두 `Generated Image Room/Front Wall`에 동일한
  `PPE Front Wall Octagonal Glass Hole` 메시를 저장하고 있다. 따라서 다른 씬에서
  Front Wall을 복사해도 같은 문제가 반복된다.
- 문 유리의 크기와 위치는 `Door Window Glass`의 `DoorWindowGlass` 직렬화 값과
  Transform이 기준이어야 한다. 벽의 기존 메시를 단순 복사하는 방식은 현재 씬의
  부모 Scale·문 위치와 일치하지 않을 수 있다.

### 적용한 변경

- `PPEFrontWallDoorOpeningSetup` Editor 도구를 추가했다. 이 도구는 현재 씬의
  `Door Window Glass` 폭·높이·월드 위치를 읽어 `Front Wall` 메시의 8각 개구부를
  다시 만들고, 벽과 문 Transform은 변경하지 않은 채 씬을 저장한다.
- 실행 메뉴는 `Tools > PPE > Rebuild Front Wall Door Opening (HandTest Scale)`이다.

### 아직 필요한 수동 검증

- Unity에서 먼저 `Assets > Refresh`를 실행해 새 Editor 도구를 컴파일한다.
- Play Mode가 아닌 상태에서 위 메뉴를 한 번 실행하고, Scene View에서 개구부가
  `Door Window Glass`와 겹치는지 확인한다.
- 저장 후 Quest Link에서 벽의 개구부를 통해 문 유리 뒤가 보이는지 확인한다.

---

## 30. 2026-08-04 태블릿 체크·서명 미표시 진단

### 증상

- 태블릿을 잡고 Trigger를 눌러도 체크 표시와 작업자·감시인·확인자 서명이 화면에 나타나지 않았다.

### 근본 원인

- `HandwrittenSignatureSequence`가 참조하는 8개 Renderer는 `_Reveal` 값을 정상적으로 변경할 수 있는 상태였다.
- 그러나 활성 씬의 8개 Renderer가 `Assets/Materials/PPE/Scene Unlit/` 아래의
  `*_Handwrite_Unlit.mat`을 참조하고 있었다.
- 해당 머티리얼은 전용 `Project/Handwritten Signature Reveal` 셰이더의 `_MainTex`가 비어 있고
  `_UseTextureAlpha`가 켜져 있다. 셰이더는 텍스처의 잉크·알파를 기준으로 출력 알파를 만들므로,
  빈 텍스처에서는 `_Reveal`이 0에서 1로 바뀌어도 출력 알파가 0으로 유지된다.

### 정상 참조 대상

- 체크 5개: `Assets/Materials/PPE/Tablet/Signatures/ChecklistCheck_Handwrite.mat`
- 작업자 서명: `Assets/Materials/PPE/Tablet/Signatures/PlayerSignature_Handwrite.mat`
- 감시인·확인자 서명: `Assets/Materials/PPE/Tablet/Signatures/ConductorSignature_Handwrite.mat`

위 세 머티리얼은 각각의 `_MainTex`에 `sign_check.png`, `sign_player_rm.png`,
`sign_conductor_ppe.png`를 직렬화된 참조로 보유한다.

### 영향 범위

- 입력 수신 여부와 관계없이 체크·서명 오버레이가 모두 투명하게 표시된다.
- `HandwrittenSignatureSequence`, Trigger 입력, 오디오 재생 순서 자체가 이 증상의 직접 원인은 아니다.

### 완료한 검증

- `3_PPE_Room_HandTest_scale.unity`의 `Signature_Test_Sequence`에서 8개 대상 Renderer와
  `HandwrittenSignatureSequence` 참조가 존재함을 확인했다.
- 현재 참조 중인 `*_Handwrite_Unlit.mat` 세 종류의 `_MainTex`가 모두 비어 있음을 확인했다.
- 정상 머티리얼 세 종류의 `_MainTex`가 필요한 PNG를 참조함을 확인했다.

### 후속 조치 및 수동 검증

- → §31에서 수정·검증 완료. 상세는
  `Docs/Bug/2026-08-04_PPE_Tablet_CheckSignature_Invisible_MaterialReference.md` 참고.

---

## 31. 2026-08-05 태블릿 체크·서명 표시·타이밍·튜닝 완료

### 배경

§30에서 진단한 미표시 문제에 더해, 머티리얼만 교체한 뒤에도 선이 안 보이고
태블릿을 잡는 즉시 SFX가 재생되는 문제가 남았다. 오늘 이를 수정하고
잉크 농도·체크 속도를 조정한 뒤 사용자 확인을 받았다.

### 적용한 변경

1. **씬 머티리얼 교체**  
   `scale` / `scale_0` / `scale_1`의 8개 Renderer를
   `Assets/Materials/PPE/Tablet/Signatures/*_Handwrite.mat`으로 교체했다.

2. **grab 시 자동 재생 제거**  
   `PPETabletChecklistController.OnTabletSelected`의 `PlayRange` 호출을 제거했다.
   시퀀스는 Trigger / Activate에서만 시작한다.

3. **`_Reveal` 반영 보장**  
   - `HandwrittenSignatureSequence`가 Renderer마다 런타임 머티리얼 인스턴스를 만들고
     `_Reveal`을 직접 설정한다.  
   - `HandwrittenSignatureReveal.shader`에서 `_Reveal`을 instanced prop으로 분리했다.  
   - Handwrite 머티리얼 3종의 GPU Instancing을 끌었다.

4. **SFX ↔ 애니메이션 동기화**  
   `PlayOneShot` 전에 `pitch = clip.length / duration`을 적용해
   reveal 길이에 맞춰 필기 소리가 재생되도록 했다.

5. **시각·속도 튜닝**  
   - 체크·서명 잉크를 더 진하게(`_InkColor` 검정 근접, AlphaBoost·Expansion 상향).  
   - 체크 5단계 `duration` `1.49s` → `0.85s`, 간격 `0.12s` → `0.05s`.  
   - 서명 3단계 속도는 유지.

### 근본 원인 요약

| 증상 | 원인 |
| --- | --- |
| 선이 안 보임 (1차) | Scene Unlit Handwrite 머티리얼의 `_MainTex` 비어 있음 |
| 선이 안 보임 (2차) | MPB `_Reveal`이 GPU Instancing 경로에서 무시됨 |
| 잡자마자 소리 | grab(`selectEntered`)에서 시퀀스 시작 |

### 영향 범위

- 태블릿 체크 5 + 서명 3의 시각·오디오 타이밍
- Handwrite 셰이더/머티리얼을 쓰는 씬 공통
- 방호복 착용, 헬멧, 미러 등 다른 PPE 경로는 이 항목에서 변경하지 않음

### 완료한 검증

- 씬 YAML에서 잘못된 Unlit Handwrite GUID 잔존 0 확인
- Quest/Play Mode에서 사용자 확인: 선 표시·Trigger 기준 소리·애니메이션 일치
- 잉크 진하기·체크 속도 조정 후 사용자 수락

### 의도한 최종 동작

1. 잡기만 → SFX 없음  
2. Trigger 1회 → 빠른 체크 5개 → 서명 3개  
3. 각 단계 선 reveal와 SFX 동기  
4. 문서 면에서 잉크가 충분히 진하게 보임

### 관련 문서

- `Docs/Bug/2026-08-04_PPE_Tablet_CheckSignature_Invisible_MaterialReference.md`

---

## 32. 2026-08-05 PPE Grab 시 공용 Action Panel·마스크 확인하기

### 배경

방호복을 잡으면 상단 중앙에 `사용` / `폐기` Action Panel이 뜬다. 같은 흐름을
다른 PPE Grab에도 확장하고, 송기 마스크만 `확인하기`를 하나 더 둔다.

### 적용한 변경

1. `PPEActionPanelController`
   - `PPEActionChoice.Inspect`, `PPEActionResult.InspectCompleted` 추가
   - 직렬화 필드: `inspectButton`, `enableInspectChoice`, `inspectPassIconRoot`,
     `inspectCompletedMessage`
   - 송기 마스크만 `enableInspectChoice`로 확인하기 표시
   - UI Button `onClick`과 Right Secondary(B)로 확인하기 선택
   - 사용/폐기 로직은 기존과 동일(Clean 사용 승인, Contaminated 사용 거부,
     Contaminated 폐기→Clean, Clean 폐기 보류)
   - 확인하기는 피드백만 표시하고 PPE를 숨기거나 상태를 바꾸지 않음

2. Editor 도구 `Tools > PPE > Configure Grab Action Panels` /
   `Repair Action Panel Layout And Placement`
   - 활성 씬 `3_PPE_Room_HandTest_scale_0` 또는 `scale`
   - 공용 `Hazmat Action Panel`에 `Inspect Button`(라벨 확인하기) 생성
   - 버튼 배치: 사용 / 확인하기 / 폐기
   - Grab PPE(`mask`, `helmet`, 장갑, 장화, `tape`, `backplate`)에
     Identity·Binding·InspectionState·ActionPanelController·Action Panel Pose 연결
   - 마스크만 확인하기 활성, 표시명 `송기 마스크`, 초기 상태 Contaminated

3. `PPEActionPanelSharedPresentation`
   - 2버튼/3버튼 패널 크기·버튼·레이블·**상태 아이콘** 프리셋
   - 표시 시 `enableInspectChoice`에 따라 프리셋만 전환
   - Pass/Error는 `Icons` 오른쪽 스트립 유지(X 고정), Y만 버튼 세로 중앙

### 의도한 동작

| PPE | 패널 버튼 |
| --- | --- |
| 방호복·안전모·장갑·장화·테이프·등지게 | 사용, 폐기 |
| 송기 마스크(mask) | 사용, 확인하기, 폐기 |

### 알려진 문제 → 후속 수정

규모·Pose·2/3버튼·아이콘 정렬은
`Docs/Bug/2026-08-05_PPE_ActionPanel_TooSmall_PoseScale.md`에 수정 적용.
Unity/Quest 수동 검증만 남음.

### 수동 검증

1. Play Mode 전 Repair/Configure로 씬이 최신 프리셋을 갖고 있는지 확인
2. 송기 마스크: 사용 / 확인하기 / 폐기 3버튼, 아이콘이 각 버튼 **오른쪽** 세로 중앙
3. 다른 PPE: 사용 / 폐기 2버튼, 아이콘이 텍스트와 겹치지 않음
4. 사용·폐기 결과가 방호복과 동일하게 나오는지 확인
5. 확인하기는 확인 완료 피드백만 보이고 마스크가 사라지지 않는지 확인
6. 패널이 모델에 꽂히지 않고 위·정면에 뜨는지, 2/3버튼 높이가 맞는지 확인
7. 표시명이 아래 표와 일치하는지 확인

### 관련 코드

- `Assets/Scripts/PPEActionPanelController.cs`
- `Assets/Scripts/PPEActionPanelSharedPresentation.cs`
- `Assets/Editor/PPEGrabActionPanelSetup.cs`
- `Docs/Bug/2026-08-05_PPE_ActionPanel_TooSmall_PoseScale.md`
- `Docs/MeetingNotes/2026-08-05_PPE_Room_HandTest_Meeting.md` §6

### 표시명 확정 (2026-08-05)

인산계 세정제 혼합기 청소 시나리오 기준으로 패널 `itemDisplayName`을 아래처럼 통일했다.
범주 표기는 **내화학성**(내산성 대신), 마스크는 **송기 마스크**, 등판은 **등지게**.

| PPE | 표시명 | 버튼 |
| --- | --- | --- |
| 방호복 | 내화학성 방호복 | 사용, 폐기 |
| 마스크 | 송기 마스크 | 사용, 확인하기, 폐기 |
| 안전모 | 안전모 | 사용, 폐기 |
| 장갑 | 내화학성 장갑(우) / (좌) | 사용, 폐기 |
| 장화 | 내화학성 장화(우) / (좌) | 사용, 폐기 |
| 테이프 | 내화학 테이프 | 사용, 폐기 |
| 등판 | 등지게 | 사용, 폐기 |

### 상태 아이콘 정렬 (2026-08-05)

Pass/Error 아이콘은 버튼 자식이 아니라 `Icons` 스트립(`x ≈ 75`)의 고정 Y라서,
송기 마스크 3버튼에서 버튼만 벌어지면 아이콘이 버튼 사이에 남았다.
프리셋에 아이콘 위치를 추가해 **세로만** 각 버튼 중앙에 맞추고, **가로는 오른쪽
작성값을 유지**해 버튼 텍스트와 겹치지 않게 했다. 확인하기 피드백은 전용 아이콘이
없을 때 공유 Pass 아이콘을 확인하기 버튼 높이로 잠깐 맞춘다.

---

## 2026-08-06 풀장착 앞→몸 연출 유지 + 가벼운 핸드 크로스페이드

### 변경 전 필수 질문

1. Inspector/씬 작성값 보존: Front Start·Approach·최종 `hazmat_suit_on_10` Pose·기존 1.8초 장착 연출은 유지. 핸드 페이드 시간만 직렬화 기본값 `0.4`초 추가.
2. 단일 기준: 장착 시각은 `PPE Body Anchor → hazmat_suit_on_10`. 핸드 전환은 `PPEHazmatEquipController` / `PPEEquipmentVisualController`.
3. 입력 경로: 변경 없음. `UseApproved` 이후 연출만 변경.
4. 실패 시: 기존처럼 참조 누락 시 컨트롤러 비활성·명확한 오류. 런타임 자동 수리 없음.
5. 영향 소비자: 방호복 장착 연출, Bare↔Suit/Glove/Tape 핸드 시각. 거울·카메라·RenderTexture·헬멧 경로 미변경.
6. 기준 실행: 기존 Front→Approach→Body Transform Lerp. 변경 후 동일 경로 + 핸드 `_Fade` 크로스페이드.
7. 검증: 정적 컴파일. Unity Play Mode·Quest 양안·주변 시야는 수동 확인 필요.

### 적용한 변경

- 방호복 사용 승인 후 기존 `PPEHazmatEquipController` Front Start → Approach → 최종 Pose 이동을 그대로 사용한다. 새 카메라·RT·메시 복제 없음.
- `XRHandFormUnlit`에 opaque `_Fade`만 추가했다. Transparent queue를 쓰지 않고 luminance 크로스페이드한다.
- `PPEHandModelCrossfade`가 MaterialPropertyBlock으로 짧은 구간만 `_Fade`를 보간한다.
- 방호복 장착 완료 직후 BareHand → BareHand_Suit 페이드.
- 장갑·장화 등은 **한쪽 Use마다** 해당 슬롯만 **최종 Pose 바로 앞 → 회전 흡착**(`animateChildrenOnUse`). 공용 Front/Approach 경로를 쓰지 않아 장화가 바닥에서 올라오는 연출을 피한다. 방호복 Front→Body는 `PPEHazmatEquipController`가 유지한다.
- 테이프만 예외로, 한 번 Use에 양손목 taped-hand 슬롯이 함께 페이드되도록 작성값을 유지한다.

### 근본 원인 / 의도

- 헬멧·거울 이슈는 중복 호스트·매프레임 반사·RT였고, 장착 “애니메이션” 자체가 아니었다.
- 핸드 전환을 `SetActive` 즉시 교체에서 짧은 opaque 페이드로 바꿔 팝만 줄인다.

### 완료한 검증

- 정적: 기존 Front Start 앵커·1.8초 duration·핸드 참조 경로 확인.
- 컴파일: `dotnet build Assembly-CSharp.csproj` 오류 0.

### 아직 필요한 수동 검증

- Play Mode에서 Clean 방호복 Use → 앞쪽 출현 → 몸 부착 → 손 모델 페이드.
- 헤드셋으로 내려봤을 때 `hazmat_suit_on_10`이 몸에 붙어 보이는지.
- Quest 양안·주변 시야가 헬멧/거울 회귀처럼 깨지지 않는지.
- 셰이더 임포트·도메인 리로드가 끝난 뒤에만 Play Mode / Quest Link를 시작할 것.

> 같은 날 룸스케일 바닥·눈높이·키보드 Grab Handle 포함 전체 세션 요약:
> `Docs/MeetingNotes/2026-08-05_PPE_Room_HandTest_Meeting.md` §2026-08-06 세션 요약

