# PPE 룸 벽·가구 보행 관통 방지

## 증상

`Assets/Scenes/3_PPE_Room_3mode_loco.unity`에서 연속 이동 중 플레이어가 벽, 진열장, 캐비닛, 선반과 같은 환경 메시 내부로 들어갈 수 있다.

## 변경 전 기준 조사

- 대상 씬: `Assets/Scenes/3_PPE_Room_3mode_loco.unity`
- Unity에서 열린 씬과 디스크 대상 씬이 일치하며 변경 전 `Scene.isDirty=False`를 확인했다.
- 활성 `XR Origin (VR)`에는 `CharacterController`가 없다.
- `XR Origin (VR)/PPE Teleport-Only Locomotion`의 `XRBodyTransformer`는 `useCharacterControllerIfExists=True`이므로, 현재는 제약 조작기 없이 Origin을 직접 이동할 수 있다.
- `PPE Room/Room`의 벽 7개와 `interiorObjects/Bg`의 주요 바닥형 가구에는 보행 차단용 Collider가 없다.
- 기존 Floor 2개에는 solid Collider가 있으며 이번 작업에서 변경하지 않는다.
- 양손 `SphereInteractionCaster`의 Physics Layer Mask는 Layer 0과 6이고, 텔레포트 `XRRayInteractor`는 Layer 31만 사용한다. Layer 2 `Ignore Raycast`는 두 물리 입력 경로 모두에서 제외된다.
- 활성 `TrackedDeviceGraphicRaycaster`의 3D Occlusion은 꺼져 있어 Layer 2 환경 차단체가 UI Ray를 가로막는 경로도 현재 없다.

## 변경 전 필수 질문

1. 기존 Inspector/씬 작성값을 보존하는가?
   - 벽·가구·Floor·텔레포트 Marker의 Transform, Renderer, Material, 기존 Collider와 모든 입력 마스크를 변경하지 않는다. 별도 환경 차단체와 활성 XR Origin의 `CharacterController`만 추가한다.
2. 단일 기준 오브젝트와 상태 소유자는 무엇인가?
   - 보행 충돌의 단일 이동 주체는 활성 `XR Origin (VR)`의 `CharacterController`다. 환경 차단체의 단일 작성 루트는 최상위 `PPE Environment Collision`이다.
3. 입력 이벤트, Interactor/Caster, Raycaster, Layer, Collider의 전체 경로는 무엇인가?
   - 연속 이동은 `컨트롤러 Move 입력 → PPEConfigurableDynamicMoveProvider → XRBodyTransformer → CharacterControllerBodyManipulator → CharacterController.Move → Layer 2 solid BoxCollider`다.
   - PPE 근거리 입력은 `NearFarInteractor → SphereInteractionCaster(Layer 0·6)`이고, 텔레포트는 `Teleport Interactor → XRRayInteractor(Layer 31)`이므로 Layer 2 차단체를 입력 대상으로 삼지 않는다.
   - UI는 `NearFarInteractor UI Model → TrackedDeviceGraphicRaycaster`이며 활성 Canvas의 3D Occlusion이 꺼져 있어 이번 차단체로 UI 상태를 바꾸지 않는다.
4. 실패 시 런타임 자동 수리 대신 명확한 오류로 멈춰야 하는가?
   - 그렇다. 런타임 생성·자동 수리는 추가하지 않는다. 명시적 Editor 메뉴가 대상 씬·오브젝트 누락 시 예외로 멈추고, 별도 검증 메뉴가 배선을 검사한다.
5. 함께 영향을 받는 소비자는 무엇인가?
   - 활성 XR Origin의 연속 이동과 중력 이동이 영향을 받는다. UI, 텔레포트 대상 선택, PPE Grab/착용, 오디오, 거울, 비활성 Hand Tracking Origin은 변경하지 않는다.
6. 변경 전 기준 실행과 변경 후 비교 실행은 무엇인가?
   - 변경 전에는 활성 Origin의 `CharacterController=없음`, 대상 벽·가구 Collider 0개다. 변경 후에는 작성 차단체 수, Layer, Bounds, solid 상태, Rigidbody 부재와 XRI CharacterController 사용 상태를 하네스로 비교한다.
7. 어디까지 실제로 검증하는가?
   - 정적 씬·코드 확인, Unity Editor 하네스, Play Mode에서 CharacterController가 차단체를 통과하지 않는 이동 시험까지 수행한다. 실제 신체 이동과 Quest/OpenXR 양안·성능은 헤드셋 수동 검증으로 남긴다.

## 이번 변경이 대응하는 사용자 요청과 보존 동작

- 대응 요청: 벽과 진열장·가구 내부로 플레이어가 들어가지 않도록 한다.
- 보존 동작: 텔레포트와 PPE Grab Ray의 Layer/Collider 경로, 카드·모달 UI, PPE 상태 전이, 오디오 재생 규칙, 기존 환경 시각과 Transform을 유지한다.

## 적용 계획

- 활성 `XR Origin (VR)`에 씬 작성 `CharacterController`를 추가한다.
- 벽은 각 Renderer의 월드 Bounds를 기준으로 최소 0.12m 두께의 BoxCollider를 만든다.
- 주요 바닥형 가구는 복잡한 자식 메시마다 MeshCollider를 붙이지 않고 결합 Renderer Bounds 기반의 단순 BoxCollider 한 개로 차단한다.
- 차단체는 Renderer와 Rigidbody 없이 Layer 2 `Ignore Raycast`, `isTrigger=False`로 작성한다.
- 설정과 검증은 `Tools > PPE > Configure Room Environment Collision` 및 `Tools > PPE > Validate Room Environment Collision`로 분리한다.

## 적용한 변경

- 활성 `XR Origin (VR)`에 작성형 `CharacterController`를 추가했다.
  - Radius `0.25m`, Height `1.7m`, Skin Width `0.03m`, Step Offset `0.2m`, Slope Limit `45°`
  - `XRBodyTransformer.useCharacterControllerIfExists=True`인 기존 배선을 그대로 사용한다.
- 최상위 `PPE Environment Collision` 아래에 solid `BoxCollider` 차단체 21개를 작성했다.
  - 벽 7개: Front, Left, Right, Rear 및 Rear 연장 3개
  - 주요 가구 14개: 금속 선반, 휴지통 2개, 청소 카트, 마스크 락커, 방호복 행거, 안전 캐비닛, 금속 락커 2개, 벤치, 수납 랙, 벽 행거, 소화기 2개
- 모든 차단체는 Layer 2 `Ignore Raycast`, `isTrigger=False`, Renderer·Rigidbody 없음으로 설정했다.
- 원본 벽·가구에는 Collider를 직접 추가하지 않았고, 복잡한 수납 랙의 107개 Renderer에도 MeshCollider를 붙이지 않았다.
- `PPERoomEnvironmentCollisionSetup`에 명시적 Configure 메뉴와 결정적 Validation/Play Mode Probe를 추가했다.

## 근본 원인과 영향 범위

- 원인은 환경 메시의 시각 Renderer만 존재하고 보행 이동을 제한할 Collider가 없었던 점과, 활성 XR Origin에 `CharacterController`가 없어 XRI 이동 변환이 충돌 제약 없이 Origin을 이동시킬 수 있었던 점이다.
- 이번 변경은 스틱 기반 연속 이동과 XRI가 `CharacterController.Move`로 적용하는 중력 이동을 제한한다.
- 사용자가 실제 플레이 공간에서 머리만 벽 너머로 기울이는 room-scale 이동은 Origin 이동이 아니므로 Capsule만으로 완전히 막을 수 없다. 필요하면 별도의 HMD 침범 감지·페이드 기능으로 분리해야 한다.

## 완료한 검증

### 정적 확인

- 씬 변경은 Unity Editor가 생성한 FileID만 사용했고, 기존 씬 YAML 재배열 없이 새 오브젝트·컴포넌트 추가만 발생했다.
- `CharacterController` 1개, 차단체 21개, 각 차단체 BoxCollider 1개, solid 상태, Layer 2, Rigidbody·Renderer 부재를 확인했다.
- 각 BoxCollider의 월드 Center/Size가 기준 Renderer Bounds와 0.02m 허용오차 안에서 일치함을 확인했다.
- 양손 `SphereInteractionCaster`와 좌우 `XRRayInteractor`가 Layer 2를 포함하지 않음을 확인했다.
- 활성 XR UI Raycaster의 3D Occlusion이 꺼져 있음을 확인했다.
- 텔레포트 목적지 4곳을 플레이어 반경 0.25m만큼 확장해 검사했고 차단체와 겹치는 목적지가 없음을 확인했다.

### Unity Editor 확인

- `Tools > PPE > Validate Room Environment Collision`과 동일한 하네스 PASS:
  - blockers `21`
  - CharacterController radius `0.25`, height `1.7`
  - collision layer `2`
  - 검증 후 `Scene.isDirty=False`
- Play Mode를 일시정지한 상태에서 활성 XR Origin을 원래 값으로 복원하는 스냅샷 시험을 수행했다. 전면 벽 방향으로 `CharacterController.Move(Vector3.forward * 2)`를 호출했을 때 `CollisionFlags.Sides`가 발생하고 벽 앞에서 정지해 PASS했다.
- Play Mode 종료 후 Origin 위치·회전을 복원했고 씬은 저장된 비오염 상태다.
- Console Error 0건을 확인했다.
- 2026-08-25 사용자 Play Mode 수동 시험에서 환경 Collider가 플레이어 이동을 차단해
  벽·가구 내부로 이동할 수 없음을 확인했다.

## 아직 필요한 수동 검증

- Quest에서 각 텔레포트 지점에 도착한 뒤 스틱 이동으로 벽, 캐비닛, 락커, 벤치, 카트,
  진열 랙의 동일한 차단 결과를 확인한다.
- 가구 앞에서 PPE Grab, 카드·모달 UI Ray와 텔레포트 Ray가 기존처럼 동작하는지 확인한다.
- 앉기·서기와 서로 다른 사용자 키에서 XRI가 Capsule 높이·중심을 HMD에 맞춘 뒤에도 바닥·벽 충돌이 자연스러운지 확인한다.
- 실제 몸을 기울여 HMD가 벽을 넘는 room-scale 침범은 이번 범위 밖이다.
- Play Mode에서 OpenXR가 XR 오디오 출력 드라이버를 설정하지 못해 기본 출력으로 대체한다는 경고가 2건 발생했다. Collider 오류는 아니지만 Quest Link 연결 상태에서 SFX·Voice 출력 장치를 별도로 확인해야 한다.

## 2026-08-26 후속: 상호작용 진열장 중복 차단 제거

- 대상: 씬 루트 이름은 `PPE_B_MetalShelving`이며 내부에 진열된 `PPE_A_*` 장비와 마커가 있는 상호작용 진열장이다.
- 1차로 전체 Bounds 차단체를 후면 0.12m 면으로 줄였고 Editor 하네스는 PASS했지만, Quest 수동 확인에서 전진할수록 플레이어가 뒤로 밀려 장비 마커와 멀어지는 현상이 남았다.
- 진열장 전체 높이를 덮는 solid 차단체는 상호작용 접근을 방해하므로 제거 대상이다. 다만 사용자가 가구 내부로 걸어 들어가지 못하게 하는 바닥 베이스 차단은 유지해야 한다.
- `Furniture - Metal Shelving` BoxCollider를 전체 높이 `2.184587m`에서 바닥 기준 높이 `0.3m`의 낮은 베이스 차단체로 변경했다. 이는 CharacterController의 `stepOffset=0.2m`보다 높아 보행 진입을 막지만 위쪽 PPE와 marker 앞 공간은 막지 않는다.
- PPE 모델, `XR Item Marker_small`, 장비 Collider, Interaction Layer, 진열장 Renderer/Transform은 변경하지 않았다.
- 정적 확인: 기존 Unity 생성 FileID를 보존한 진열장 베이스 차단체 1개와 `Assembly-CSharp-Editor` 빌드 오류 0개를 확인했다. 기존 패키지/API 경고만 남아 있다.
- Unity Editor 확인: 2차 수정 임포트 후 `Tools > PPE > Validate Room Environment Collision` 재실행이 필요하다.
- Quest/OpenXR 확인: 진열장으로 전진할 때 뒤로 밀리지 않고 헬멧을 포함한 마커를 손으로 잡을 수 있는지 다시 확인해야 한다.
- 추가 원인 확인: 진열장 차단체 제거 후에도 `XR Item Marker_small` 25개의 `SphereCollider.isTrigger=false`와 전체 허용 Layer Collision Matrix 때문에 플레이어 CharacterController가 마커와 solid 충돌했다. 마커 반경·Transform·Grab 등록은 유지하고 25개를 Trigger로 전환했다.

## 2026-08-26 후속: Marker Trigger 전환 후 Grab 회귀

### 근본 원인

- Marker를 Trigger로 전환했지만 활성 양손 `NearFarInteractor`의 근거리 `SphereInteractionCaster`가 `QueryTriggerInteraction.Ignore`로 작성되어 있었다.
- 따라서 Marker는 더 이상 플레이어를 밀지 않았지만 손 근접 Grab query에서도 제외되어 모든 PPE를 잡을 수 없었다.
- `PPE_B_MetalShelving` 모델 계층 34개 Transform을 정적으로 조사한 결과 모델 자체에는 Collider가 없다. 별도 환경 차단체는 PPE 높이를 가리지 않는 바닥 베이스 전용 BoxCollider 한 개만 사용한다.

### 적용한 변경

- Marker 25개의 Trigger 상태는 유지해 CharacterController와의 밀림을 차단했다.
- 활성 좌우 `SphereInteractionCaster.m_PhysicsTriggerInteraction`만 `Collide`로 변경해 Trigger Marker를 손 근접 범위에서 다시 선택할 수 있게 했다.
- Marker는 전용 Layer 6을 유지하고 원거리 `CurveInteractionCaster` mask에서는 Layer 6을 계속 제외해, PPE가 원거리 카드 레이에 잡히지 않는 기존 입력 범위를 보존했다.
- 진열장 차단체는 전체 Bounds가 아니라 바닥 기준 높이 `0.3m`만 차단하도록 제한해 보행 진입 방지와 PPE 접근을 함께 만족시켰다.
- 1차 베이스 차단체도 전체 깊이 `0.42775726m`를 사용해 CharacterController 반경만큼 앞에서 멈추는 거리가 남았다. 최종적으로 깊이를 `0.12m`로 줄이고 진열장 앞면에서 안쪽으로 `0.25m` 후퇴시켰다. 따라서 플레이어 몸 중심은 시각적 앞면 부근에서 멈추며 손은 marker까지 접근할 수 있다.
- `PPERoomEnvironmentCollisionSetup`과 검증 하네스가 Marker Trigger와 좌우 Near/Far caster의 Trigger query 설정을 함께 적용·검증하도록 보강했다.

### 검증 범위

- 정적 확인: 양손 Near caster 두 설정이 `Collide`, Far caster 두 mask가 Marker Layer 6 제외, Marker Collider 25개가 Trigger, 진열장 베이스 차단체가 높이 `0.3m`·깊이 `0.12m`이며 앞면에서 `0.25m` 후퇴한 것을 확인했다.
- 컴파일 확인: `Assembly-CSharp-Editor.csproj` 오류 0개(기존 obsolete 경고만 존재).
- Unity Editor 및 Quest/OpenXR에서 실제 Hover/Select와 진열장 접근 재검증은 아직 필요하다.

## 2026-08-26 검토안(대체됨): 진열장 전용 베이스 차단체 제거

### 재현 결과와 근본 원인

- Marker Trigger 전환 뒤 PPE Grab은 가능해졌지만, 사용자가 진열장 바로 앞까지 전진하지 못하는
  거리가 계속 남았다.
- 낮은 `Furniture - Metal Shelving` 베이스도 solid Collider이므로, 앞면에서 0.25m 후퇴시켜도
  반경 0.25m인 플레이어 CharacterController가 시각적 진열장 앞면에서 정지한다.
- `PPE_B_MetalShelving` 모델 자체에는 Collider가 없고, 진열장 바로 뒤에는 방 전체를 막는 solid
  `Wall - Front`가 있다. 따라서 전용 베이스는 통과 방지에 중복이며 접근만 제한한다.

### 적용한 변경

- 씬의 `PPE Environment Collision/Furniture - Metal Shelving` GameObject와 BoxCollider를 제거했다.
- `PPERoomEnvironmentCollisionSetup.Targets`에서도 해당 대상을 제거해 설정 도구를 다시 실행해도
  진열장 앞 차단체가 재생성되지 않게 했다.
- 검증 하네스는 전용 진열장 차단체가 없어야 하며, 진열장 뒤의 `Wall - Front`가 solid 상태로
  직접 배치되어 통과를 막는지 검사한다.
- Marker 25개의 Trigger, 양손 Near caster의 `Collide`, Far caster의 Layer 6 제외, 플레이어
  CharacterController와 다른 가구 차단체는 변경하지 않았다.

### 검증 상태

- 정적 확인: 진열장 전용 GameObject·BoxCollider·부모 자식 참조가 씬에서 제거됐고 설정기에서도
  재생성 경로가 제거됐다.
- Unity Editor 확인: 스크립트 임포트 완료 후 `Tools > PPE > Validate Room Environment Collision`
  실행이 필요하다.
- Quest/OpenXR 확인: 진열장 앞까지 전진 가능, 뒤로 밀림 없음, PPE 직접 Grab 가능, 뒤쪽 벽 통과
  불가를 다시 확인해야 한다.

위 제거안은 사용자의 “하단 단스가 있으므로 선반 앞에서 보행이 막혀야 한다”는 최종 요구와 맞지
않아 최종 상태로 사용하지 않는다. 아래의 앞면 정렬 베이스 차단체로 대체했다.

## 2026-08-26 최종 수정: 단스 앞면 정렬 차단과 PPE Grab 분리

- `Furniture - Metal Shelving` BoxCollider를 복원하되, 진열장 전체가 아니라 바닥 기준 높이 0.3m,
  깊이 0.12m의 하단 단스 차단체만 사용한다.
- 차단체 앞면은 `PPE_B_MetalShelving` Renderer Bounds의 시각적 앞면과 일치한다. 앞쪽으로 확장하거나
  CharacterController 반경만큼 임의 후퇴시키지 않는다.
- 차단체는 Layer 2 `Ignore Raycast`이므로 양손 PPE Grab caster가 입력 대상으로 사용하지 않는다.
- PPE는 기존 Layer 6의 Trigger `XR Item Marker_small`과 Near caster `Collide` 경로로 선택한다.
  따라서 플레이어 몸은 단스 앞에서 멈추지만 손의 Grab 판정은 안쪽 PPE marker까지 도달한다.
- 검증 하네스는 차단체의 높이·깊이·바닥 접지·시각적 앞면 정렬과 입력 Layer 격리를 함께 확인한다.
- 정적 확인과 C# 컴파일 뒤 Unity Editor 및 Quest/OpenXR에서 실제 접근·Grab을 재확인해야 한다.

## 2026-08-26 후속: 플레이어 캡슐 접근 반경 축소

- 단스 BoxCollider를 시각적 선반 앞면에 맞춰도 기존 플레이어 CharacterController 반경 0.25m만큼
  HMD/몸 중심이 앞에서 정지해 접근 거리가 크게 느껴졌다.
- 가구 BoxCollider를 뒤로 숨기지 않고 시각적 앞면 정렬을 유지한다.
- 플레이어 CharacterController 반경을 0.1m, skin width를 0.01m로 줄여 캡슐 표면 충돌은 유지하면서
  몸 중심이 선반 앞 약 0.1m까지 접근할 수 있게 했다.
- 높이 1.7m, step offset 0.2m, slope limit 45도, 다른 벽·가구 차단체와 XRI
  `useCharacterControllerIfExists` 경로는 유지한다.
- PPE Grab은 계속 Layer 2 단스 차단체를 무시하고 Layer 6 Trigger Marker를 사용한다.
- Unity Editor 및 Quest/OpenXR에서 선반 진입 불가, 앞면 접근, 다른 벽·가구 통과 불가를 다시 확인해야 한다.

## 2026-08-26 후속: 메탈 셸브 직접 소유 Collider로 전환

### 근본 원인

- 베이스 `BoxCollider`가 `PPE Environment Collision/Furniture - Metal Shelving`이라는 별도
  GameObject에 있어, 사용자가 실제 `PPE_B_MetalShelving`을 선택해도 Inspector에서 충돌 범위를
  확인하거나 조정할 수 없었다.
- 셸브 루트에는 X축 -90도 회전과 비균일 스케일 `(2, 2, 4.5)`이 적용되어 있어 기존 월드 좌표의
  `Center/Size`를 그대로 복사하면 콜라이더 방향과 크기가 달라진다.

### 적용한 변경

- 기존 Unity 생성 `BoxCollider` FileID는 유지하면서 소유 GameObject를 `PPE_B_MetalShelving`으로
  옮겼고, 별도 `Furniture - Metal Shelving` GameObject와 Transform은 제거했다.
- 기존 월드 Bounds를 셸브 로컬 좌표로 환산해 동일한 시각적 앞면·높이 `0.3m`·깊이 `0.12m`를
  유지했다.
- 셸브 루트만 Layer 2 `Ignore Raycast`로 설정했다. 자식 PPE Marker는 Layer 6 Trigger를 유지하므로
  손 Near Grab 경로는 바꾸지 않았다.
- `PPERoomEnvironmentCollisionSetup`은 직접 Collider가 없을 때만 기본 Bounds로 생성한다. 이미
  존재하는 Collider의 Inspector 작성 `Center/Size`는 설정 도구를 다시 실행해도 덮어쓰지 않는다.
- 검증 하네스는 셸브 루트가 직접 `BoxCollider` 하나를 소유하는지와 높이·깊이·바닥·앞면 안전 범위를
  확인하도록 변경했다.

### 검증 상태

- 정적 확인: 별도 GameObject/Transform 참조 제거, 기존 Collider의 셸브 루트 연결, Layer 2와 로컬
  `Center/Size` 직렬화를 확인했다.
- C# 컴파일: `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 오류 0개. 기존 deprecated API
  경고 29개만 남아 있다.
- Unity Editor 하네스는 외부 씬 변경을 Editor가 다시 읽고 컴파일을 끝낸 뒤 실행해야 한다.
- Quest/OpenXR에서는 Inspector 조정 후 선반 앞 정지 거리, 뒤로 밀림 여부, PPE Grab을 다시 확인해야 한다.

## 2026-08-26 후속: 메탈 셸브 베이스 Collider 가로 배치

### 근본 원인

- 직접 소유 `BoxCollider`는 월드 높이 `0.3m`, 깊이 `0.12m`인 낮은 세로 스토퍼 형상이었다.
- 전면 캡처에서 얇은 면이 바닥에서 위로 서 있는 것이 확인됐으며, 선반 판처럼 가로로 놓으라는
  요구와 일치하지 않았다.
- 셸브 루트의 X축 -90도 회전과 비균일 스케일 `(2, 2, 4.5)` 때문에 로컬 `Size` 축만 단순히
  맞바꾸면 월드 높이와 깊이가 의도한 값이 되지 않는다.

### 적용한 변경

- 동일한 Collider와 FileID를 유지하면서 로컬 `Center/Size`를 다시 환산했다.
- 월드 형상은 높이 `0.12m`, 깊이 `0.3m`로 가로 배치했다. 기존 바닥 최저점과 시각적 앞면은
  그대로 유지하므로 플레이어의 전면 정지 기준은 바뀌지 않는다.
- `Ignore Raycast`, PPE Marker, Near/Far caster, 장비 Collider와 선반 모델 Transform은 변경하지 않았다.
- 검증 하네스의 높이·깊이 한계를 새 형상에 맞추고, 월드 높이가 깊이보다 작아야 한다는 가로 배치
  조건을 추가했다.

### 검증 상태

- 정적 확인: 씬 로컬값을 셸브 Transform에 적용하면 월드 크기는 약
  `1.961797m × 0.12m × 0.3m`이며, 기존 월드 바닥·앞면 기준점을 유지한다.
- Unity Editor 확인: 외부 씬 변경 임포트 후 `Tools > PPE > Validate Room Environment Collision`을
  실행하고 Scene View에서 Collider가 선반 판처럼 가로인지 확인해야 한다.
- Quest/OpenXR 확인: 선반 앞 접근, 가구 진입 차단, PPE 근접 Grab을 다시 확인해야 한다.

## 2026-08-27 수정: Wall Hanger 중복 차단 제거와 셸브 가로 Collider 유지

### 근본 원인

- 저장된 씬에서 `PPE_B_MetalShelving` 자체 Collider 외에
  `PPE Environment Collision/Furniture - Wall Hanger` BoxCollider가 선반 접근 영역을 함께 덮고 있었다.
- 해당 차단체의 앞면은 선반 Renderer 앞면보다 0.329m 돌출되어 있었다. 플레이어 CharacterController 반경
  0.1m를 합치면 카메라와 Origin이 정렬된 경우 약 0.429m 간격에서 이동이 정지할 수 있었다.
- 따라서 메탈 셸브 Collider를 조정해도 체감 정지 위치가 바뀌지 않았다. 카메라의 room-scale 오프셋은 보이는
  간격에 영향을 줄 수 있지만 이번 고정된 30cm 이상 차단 거리의 근본 원인은 아니었다.
- 메탈 셸브 베이스 Collider의 월드 기준 높이 0.12m, 깊이 0.30m 가로 배치는 PPE Grab 접근 공간을
  보존하기 위해 사용자가 확정한 상태다. 이 값은 중복 차단 원인이 아니므로 유지한다.

### 적용한 변경

- 원본 `PPE_B_WallHanger` 모델은 유지하고, 중복된 `Furniture - Wall Hanger` 환경 차단체만 제거했다.
- `PPERoomEnvironmentCollisionSetup.Targets`에서도 Wall Hanger 대상을 제거해 설정 도구 재실행 시 재생성되지 않게 했다.
- 메탈 셸브가 직접 소유한 기존 BoxCollider FileID와 월드 기준 높이 0.12m, 깊이 0.30m 가로 배치를 유지했다.
- 셸브 뒤의 `Wall - Front` solid Collider는 유지해 방 밖 통과를 계속 차단한다.
- `Tools > PPE > Validate Interactive Shelf Access Collision`을 추가해 중복 차단체 부재, 셸브 Collider의 가로 배치,
  바닥·앞면 정렬과 뒤쪽 벽 차단을 독립적으로 검사한다.

### 검증

- 정적 확인: Wall Hanger 환경 차단체가 없고 셸브 Collider 월드 크기가 `(1.962, 0.120, 0.300)`임을 확인했다.
- Unity Editor 확인: `ValidateInteractiveShelfAccess()` PASS.
- 전체 `Validate Room Environment Collision`은 이번 변경과 무관한 기존
  `PPE/PPE_A_SuitHang_Ripped` XRGrabInteractable Collider 바인딩 오류에서 중단됐다. 이 작업에서는 해당 입력 소비자를
  변경하지 않았다.
- Quest/OpenXR 확인: 셸브 앞 체감 정지 거리, 단스 진입 차단, PPE 근접 Grab을 실제 HMD에서 확인해야 한다.

### 2026-08-27 추가: 선반 판별 Collider와 Wooden Crate 02 독립 차단

- 높이 0.12m인 셸브 바닥 Collider만으로는 `CharacterController.stepOffset=0.2m`에 의해 단차를 올라가듯
  셸브 내부로 진입할 수 있었다.
- 전역 stepOffset을 낮추거나 셸브 앞에 보이지 않는 세로 벽을 두지 않고, 실제 수평 선반 Renderer
  `tripo_part_15`, `tripo_part_4`, `tripo_part_5`에 각각 Bounds와 일치하는 BoxCollider를 추가했다.
- 기존 바닥 Collider와 세 선반 판 Collider는 모두 Layer 2를 사용한다. 양손 Near/Far 입력은 Layer 2를
  제외하고 Layer 6 Trigger Marker를 사용하므로 Grab 입력 경로를 변경하지 않는다.
- 하네스에서 각 선반 Collider와 모든 `XR Item Marker_small` Bounds가 겹치지 않는 것을 검사한다.
- `PPE_B_WoodenCrate_02`에는 자체 Collider가 없었고, 제거된 Wall Hanger 대형 차단체가 과거에 우연히
  해당 영역을 덮고 있었다. 크레이트 02 Renderer Bounds와 일치하는 전용 Layer 2 차단체를 추가하고
  `PPERoomEnvironmentCollisionSetup.Targets`에 등록했다.
- Unity Editor 확인: 선반 판 3개, Marker 비중첩, 입력 Layer 격리, Wooden Crate 02 Bounds 및 셸브 전용
  접근 검증이 PASS했다. 실제 HMD 이동과 PPE Grab은 Quest/OpenXR에서 수동 확인해야 한다.

## 2026-08-27 추가: PPE 선택 마커 판정 반경 축소

### 근본 원인

- `XR Item Marker_small`의 발광 Mesh 크기와 선택용 `SphereCollider`가 같은 오브젝트에 있었고, 주요 PPE의 로컬 반경이 `0.25`로 설정되어 있었다.
- 오른쪽 장갑 기준 콜라이더 월드 지름은 약 `0.0793m`였으며, Near caster의 반경 `0.1m`와 합쳐져 가까운 다른 PPE가 선택될 가능성이 있었다.

### 적용한 변경

- `PPE` 루트 바로 아래의 실제 선택 대상 23개에 한해 `SphereCollider.radius`를 `0.125`로 줄였다.
- 발광 마커의 Transform, Renderer Bounds, Mesh, Material은 변경하지 않았다.
- 헬멧 결함 표시용으로 중첩된 마커 2개는 선택 대상 반경 변경에서 제외했다.
- `Tools > PPE > Apply Primary PPE Marker Selection Radius`는 명시적인 Editor 작업으로만 반경을 적용하며, 기존 미저장 씬 변경을 함께 저장하지 않도록 dirty 씬에서는 중단한다.
- `Tools > PPE > Validate Primary PPE Marker Selection Radius` 검증 메뉴를 추가했다.

### 검증

- 정적 확인: 주요 PPE 마커 23개의 로컬 반경 `0.125`, Trigger 유지, Renderer 존재를 확인했다.
- Unity Editor 확인: 오른쪽 장갑의 콜라이더 월드 지름이 약 `0.0396m`로 감소했다. 적용 전후 23개 발광 마커의 Transform, Renderer Bounds, Mesh, Material이 동일함을 비교했다.
- 저장 상태: 작업 시점에 장갑·마스크·SCBA 등의 기존 미저장 Transform 변경이 있어 씬을 자동 저장하지 않았다. 사용자가 해당 변경을 검토한 후 씬을 저장해야 한다.
- Quest/OpenXR 확인: 실제 손 접근 시 인접 PPE 오선택 감소와 잡기 편의성은 HMD에서 수동 확인이 필요하다.

## 2026-08-27 추가: 헬멧 쪽 바깥 기둥 단일 Collider

- 헬멧이 배치된 셸브 오른쪽 바깥 기둥은 `tripo_part_6`, `tripo_part_17`,
  `tripo_part_17 (1)` 세 Renderer 조각으로 이어진 구조다.
- 세 조각의 합산 Bounds와 일치하는 BoxCollider 하나를 대표 조각 `tripo_part_6`에 추가했다.
  월드 크기는 약 `(0.042, 2.071, 0.092)m`로 얇은 세로 기둥 범위만 막는다.
- 다른 세로 부재에는 BoxCollider를 추가하지 않았으며, 기존 상판 3개와 바닥 Collider는 유지했다.
- 셸브 계층에 Rigidbody가 없으므로 상판과 기둥의 정적 Collider가 서로 밀어내지 않는다.
- Unity Editor 검증에서 합산 Bounds 일치, Layer 2, PPE Marker 비중첩, 다른 기둥 무변경,
  Rigidbody 부재가 PASS했다. 실제 HMD에서 기둥 통과 차단과 가장자리 걸림 여부를 수동 확인해야 한다.

## 2026-08-27 추가 진단: 가구 이동 후 차단체 정렬 차이

### 진단 방법

- `Tools > PPE > Validate Furniture Collider Alignment` 읽기 전용 검증을 추가했다.
- 환경 차단 루트 아래 각 Furniture BoxCollider의 월드 중심·크기를 현재 대응 Renderer Bounds와
  비교했다. 허용 오차는 `0.02m`다.
- 메탈 셸브는 별도 작성 정책에 따라 베이스, 상판 3개, 헬멧 쪽 기둥을 각각 검증했다.

### 결과

가구 차단체 13개 중 다음 9개가 현재 형상과 어긋났다.

| 차단체 | 중심 차이 | 크기 차이 | 실제 중심 / 기대 중심 |
|---|---:|---:|---|
| `Furniture - Yellow Trash Bin 01` | `0.126m` | `0.000m` | `(7.56,-0.25,-0.31)` / `(7.45,-0.21,-0.32)` |
| `Furniture - Yellow Trash Bin 02` | `0.126m` | `0.000m` | `(7.58,-0.26,-0.94)` / `(7.46,-0.21,-0.94)` |
| `Furniture - Cleaning Cart` | `0.111m` | `0.000m` | `(7.40,0.03,1.34)` / `(7.30,-0.02,1.34)` |
| `Furniture - Suit Hanger` | `0.057m` | `0.000m` | `(7.37,0.17,3.38)` / `(7.37,0.11,3.38)` |
| `Furniture - Safety Cabinet` | `0.032m` | `0.000m` | `(-3.50,0.10,9.70)` / `(-3.47,0.10,9.70)` |
| `Furniture - Bench` | `0.141m` | `0.000m` | `(-3.44,-0.66,3.79)` / `(-3.58,-0.66,3.79)` |
| `Furniture - Storage Wall Rack` | `0.174m` | `0.000m` | `(7.28,-0.01,5.76)` / `(7.45,-0.01,5.76)` |
| `Furniture - Wooden Crate 02` | `0.059m` | `0.117m` | `(-0.51,0.13,10.17)` / `(-0.51,0.13,10.23)` |
| `Furniture - Fire Extinguisher 01` | `0.076m` | `0.000m` | `(7.43,-0.57,10.34)` / `(7.45,-0.57,10.41)` |

- Mask Locker, Metal Locker, Metal Locker 1, Fire Extinguisher 02는 현재 Renderer Bounds와 정렬됐다.
- 메탈 셸브 베이스, 상판 3개, 헬멧 쪽 오른쪽 기둥도 현재 형상과 정렬됐다.
- 크기가 같은 8개는 가구 Transform 변경 뒤 독립 차단체가 따라오지 않은 것으로 추정된다.
- Wooden Crate 02는 현재 Renderer 깊이가 기존 차단체보다 약 `0.11m` 커 중심과 크기를 모두
  다시 맞춰야 한다.

### 현재 조치 상태

- 이 단계에서는 진단만 요청받았으므로 Collider를 이동·재생성하거나 씬을 저장하지 않았다.
- 후속 승인 시 어긋난 9개만 현재 Renderer Bounds에 맞추고, 정상 4개와 메탈 셸브 전용
  Collider는 보존한다.

### PPE 위치 변경 후 Marker 겹침

- 주요 PPE Marker 23개는 반경 `0.125`와 발광 Renderer를 정상 유지하고, Marker 상호 간에는
  겹침이 없다. 최소 표면 간격은 오른쪽 장갑과 Tape 사이 약 `0.111m`다.
- `PPE_A_FaceShield_Clean/XR Item Marker_small`은 셸브 `tripo_part_4` 상판과 Y축
  `0.0007m`만 겹친다. Marker 중심은 상판 밖이다.
- `PPE_C_Tablet/XR Item Marker_small`은 `Furniture - Wooden Crate 02` 차단체 안에 중심까지
  포함된다. 월드 Marker 크기 약 `0.0375m` 중 Z축 겹침은 `0.0349m`다.
- 사용자가 Tablet Marker를 뒤로 이동한 것은 의도한 작성값이라고 확인했다. Crate solid는
  Layer 2, Marker는 Layer 6이며 손 Near/Far 입력은 Layer 2를 선택 대상으로 소비하지 않는다.
  따라서 해당 Bounds 겹침은 정보로 기록하되 자동 실패나 Marker 이동 근거로 사용하지 않는다.
- `Validate Furniture Collider Alignment`는 PPE Marker와 solid Furniture Collider의 겹침을
  정보 로그로 함께 보고한다. 셸브 상판은 `0.02m`를 넘는 깊은 침투만 실패 처리한다.
- 진단만 수행했으며 PPE, Marker, 상판, 크레이트 차단체의 Transform과 Bounds는 변경하지 않았다.
- 후속 수정 대상은 의도한 Tablet Marker가 아니라, 현재 Renderer와 중심·크기가 달라진
  Wooden Crate 02 차단체다.

## 2026-08-28 수정: 이동된 가구 Collider 9개 재정렬

- 2026-08-27 진단에서 확인한 9개만 현재 Renderer Bounds에 맞췄다. 8개는 차단체 중심만,
  `Furniture - Wooden Crate 02`는 중심과 크기를 함께 조정했다.
- 정상 가구 차단체 4개와 메탈 셸브 베이스·상판 3개·헬멧 쪽 바깥 기둥 Collider, PPE·Tablet·Marker
  Transform, Layer와 입력 경로는 변경하지 않았다.
- 명시적 Editor 메뉴 `Tools > PPE > Realign Moved Furniture Colliders`를 추가했으며, 미저장 씬이나
  누락·중복·예상 밖 작성값에서는 중단한다. 런타임 자동 보정은 추가하지 않았다.
- 저장 직후 검증 전에 `Physics.SyncTransforms()`를 호출해 새 Transform에 대한 Collider Bounds를
  동기화한다. 이 호출이 없으면 씬 값은 저장됐어도 같은 프레임의 하네스가 이전 Bounds를 읽었다.
- Unity Editor에서 Furniture 정렬, PPE Marker 반경, 메탈 셸브 접근 검증이 모두 PASS했고 씬은
  `dirty=false`였다. 씬 diff는 차단체 Transform 9개와 Wooden Crate 02 크기 1개만 포함한다.
- Quest/OpenXR의 실제 통과 차단·체감 정지 위치와 PPE 손 도달은 수동 검증이 남아 있다.

## 2026-09-04 수정: 가운데 가로판과 오른쪽 긴 기둥 보행 차단체 보완

### 근본 원인

- code 4 Quest 2 실기 피드백을 처음에는 가운데 가로 선반과 오른쪽 기둥의 손 관통으로 기록했다.
  이후 사용자가 조이스틱 이동이 아니라 현실에서 몸을 움직일 때 HMD 시점과 손 모델이 함께 환경 표면을
  넘어가는 현상이라고 명확히 정정했다.
- Unity 읽기 전용 Bounds 진단에서 가운데 `tripo_part_7`은 기존 `tripo_part_15` BoxCollider가
  Renderer AABB 체적의 `18.7%`만 덮었고, 오른쪽 긴 기둥 `tripo_part_3`은 기존 Collider가
  `5.6%`만 덮었다.
- 기존 하네스는 `tripo_part_3`에 Collider가 없어야 한다고 오히려 강제했고, 가운데
  `tripo_part_7`을 검증 대상에 포함하지 않아 이 빈 구간을 놓쳤다.
- 이 Bounds 누락은 XR Origin의 스틱 이동 차단 범위 문제이지만, HMD와 손의 로컬 추적 좌표가 표면을
  넘어가는 룸스케일 관통의 근본 원인은 아니다. 따라서 아래 Collider 보완은 보행 충돌 수정으로 유지하되
  실제 신체 추적 관통 해결로 확대 해석하지 않는다.

### 적용한 변경

- `tripo_part_3`에는 해당 Mesh Bounds와 일치하는 얇은 BoxCollider를 Unity Editor가 생성했다.
- `tripo_part_7` 전체 AABB BoxCollider는 장갑·마스크·테이프 선택 마커 영역을 깊게 막아 첫 검증에서
  즉시 폐기했다. 대신 시각 Mesh 자체를 공유하는 정적 비볼록 MeshCollider를 작성해 실제 선반 형상만
  충돌하게 했다.
- 두 오브젝트는 Layer 2를 사용해 PPE Grab Ray 선택 대상이 되지 않는다. Transform, Renderer,
  Mesh, Material, PPE와 Marker 작성 위치는 변경하지 않았다.
- `PPERoomEnvironmentCollisionSetup`의 명시적 code 5 적용 명령과 읽기 전용 Bounds 진단을 추가하고,
  가운데 MeshCollider·오른쪽 BoxCollider·Rigidbody 부재·레이어·Mesh/Bounds 일치를 회귀 검사한다.

### 검증

- `PPE Room Collision` Unity 배치 하네스 PASS: 기존 바닥·가로판·헬멧 쪽 기둥과 함께 새 가운데 판,
  오른쪽 기둥, Wall Hanger 제거, Front Wall 차단 및 입력 레이어 분리를 확인했다.
- 씬에는 Unity가 생성한 두 Collider FileID와 두 Layer 변경만 남겼고, Unity 저장 중 생긴 무관한
  `m_Name` 공백 정규화 diff는 기준 씬 복구 후 선별 재적용해 제거했다.
- Quest/OpenXR에서 스틱 이동 시 해당 선반·기둥의 보행 차단과 PPE 잡기 편의는 code 5 기기 확인이 남아 있다.
  현실에서 몸을 움직일 때의 HMD·손 관통은 아래 별도 결함으로 검증한다.

## 2026-09-04 정정: 실제 신체 추적에 의한 헤드·손 환경 관통

### 현상 정의

- 사용자는 조이스틱으로 대상 앞까지 이동한다. 관통이 발생하는 직접 동작은 도착 후 조이스틱 입력을 놓고
  현실에서 몸·머리·손을 움직이는 룸스케일 추적이다.
- 사용자가 현실에서 상체와 머리를 움직이거나 손을 뻗으면 HMD 시점과 추적 손 모델이 가상 벽·문·단스·진열장
  표면을 넘어간다.
- PPE를 벽 너머에서 선택하는 현상은 관찰되지 않았다. 다만 진열장 각 단 위에 배치된 PPE를 손으로
  Hover/Select/Grab하는 기존 동작은 관통 대응 뒤에도 반드시 보존해야 하는 회귀 검증 항목이다.
- 이 현상은 정상 처리로 수용하지 않는다. 기존 `CharacterController`와 환경 Collider는 XR Origin 이동을
  제한하지만 HMD와 손의 로컬 추적 Pose를 막지 못하므로 별도 XR 침범 대응이 필요하다.

### 영향 범위와 수정 경계

- 헤드 관통은 사용자의 시야가 환경 메시 내부나 반대편으로 이동해 몰입 저하와 멀미를 일으킬 수 있다.
- 손 관통은 추적 손 모델이 환경 표면을 시각적으로 통과하는 표시 결함이다.
- 카메라나 손 Transform에 Rigidbody·Collider를 직접 추가해 추적 Pose와 물리를 경쟁시키지 않는다.
- 헤드는 HMD 침범 감지 후 시야 페이드·비네트 또는 XR Origin 보정 후보를 비교한다. 손은 접촉 시 렌더 숨김,
  접촉 표현 또는 시각 손 프록시 제한 후보를 비교한다. Inspector 작성값을 기준으로 구성하며 구현 방식은
  실제 재현 위치와 침범 범위를 확인한 뒤 확정한다.

### 진열장 PPE 접근 불변조건

- 헤드의 환경 침범 제한과 손의 진열장 PPE 접근은 서로 다른 소비자로 분리한다.
- 손은 진열장 앞의 열린 공간을 통해 각 단 위에 놓인 PPE까지 도달하고 기존 방식으로 Grab할 수 있어야 한다.
- 진열장 전체 Renderer Bounds를 덮는 단일 BoxCollider, 단 전체의 빈 공간을 막는 차단체 또는 PPE 앞을
  가로막는 넓은 손 충돌 볼륨을 추가하지 않는다.
- 선반판과 기둥처럼 실제로 고체인 표면만 현재 Mesh/Renderer Bounds에 맞춰 취급하고, 단 사이의 열린 공간과
  PPE Marker·Grab Collider·Interaction Layer 경로는 보존한다.
- 헤드 침범을 막는 방식이 손 추적 Pose, Near/Far Interactor, PPE Collider 또는 착용 상태 전이를
  비활성화하거나 소비해서는 안 된다.

### 사용자 작성 씬 보존 기준

- 사용자는 현재 PPE Room에 새 구조물을 배치했고 일부 기존 벽의 위치도 변경했다.
- 해당 구조물과 벽의 현재 씬·Inspector Transform은 보존해야 하는 사용자 작성값이자 이후 진단의 기준이다.
- 이전 커밋의 벽 위치, 과거 환경 차단체 Bounds 또는 Editor 생성기의 예전 기본값으로 되돌리지 않는다.
- 이후 충돌·침범 대응은 현재 구조물과 벽의 실제 Renderer/Collider Bounds를 다시 읽어 작성한다.
- 씬을 checkout·restore하거나 기존 생성 명령을 다시 실행해 구조물, 벽, 자식 Transform 또는 Collider를
  일괄 재생성하지 않는다. 변경이 필요하면 대상 오브젝트를 먼저 식별하고 사용자 작성 Transform을 유지한
  최소 변경만 적용한다.

### 사용자가 할 수동 확인

1. Quest 안전 경계 안에서 실제 장애물과 부딪히지 않도록 주변을 비우고, 큰 걸음 없이 상체와 손만 천천히 움직인다.
2. Codex가 PC에서 Quest 미러링 또는 녹화와 필요한 로그 수집을 먼저 시작한다. 사용자는 영상을 별도로
   촬영·전송하지 않고 헤드셋 연결·착용과 실제 신체 움직임만 담당한다.
3. 사용자는 조이스틱으로 가상 벽·문·단스·진열장 앞의 안전한 거리까지 이동한다.
4. 대상 앞에 도착하면 조이스틱 이동 입력을 놓고, 머리만 앞으로 기울여 시점이 표면 내부 또는 반대편으로
   넘어가는 위치를 기록한다.
5. 왼손과 오른손을 각각 뻗어 손 모델이 일부 또는 완전히 통과하는 위치를 확인한다.
6. Codex는 미러링 영상에서 `접근 전 → 접촉 → 관통` 구간을 캡처하고, 결과를 `헤드`, `왼손`, `오른손`,
   `컨트롤러 사용 또는 손 추적 모드`, `재현 오브젝트/위치`로 구분해 문서에 기록한다.
7. 관통 재현과 별도로 진열장 각 단의 PPE에 손을 뻗어 Hover/Select/Grab이 유지되는지 확인한다. Codex는
   각 단별 성공·실패와 손이 막힌 표면을 미러링 화면에서 기록한다.

### 검증 경계

- 현재 문서 정정 단계에서는 런타임 코드, 씬, Collider, 입력과 렌더링을 변경하지 않았다.
- 기존 Collider 하네스 PASS는 스틱 기반 XR Origin 이동 계약의 정적·Editor 검증이다.
- 후속 검증은 과거 씬 좌표가 아니라 새 구조물과 이동된 벽을 포함한 현재 씬 상태를 기준으로 수행한다.
- 실제 신체 추적 관통은 Quest/OpenXR에서 위 수동 확인을 재현하고, 후속 구현 뒤 같은 위치에서 헤드·양손과
  양안 렌더링을 다시 비교해야 한다. 진열장 각 단의 PPE Grab까지 유지돼야 완료 처리한다.

### Codex 업데이트 중단 후 재개 지점

- Codex 앱 업데이트로 작업 창이 종료됐으며 Unity/Quest 런타임 실패로 분류하지 않는다.
- 현재 씬의 새 구조물과 이동된 벽, 사용자 미커밋 Transform을 되돌리지 않는다.
- 재개 후 첫 작업은 Quest 미러링·녹화 확보와 현재 씬 기준 헤드·양손 관통 재현이다.
- 그다음 HMD Camera·손 추적 Pose·XR Origin·현재 환경 Collider의 전체 경로를 읽기 전용으로 조사한다.
- 진열장 각 단 PPE Grab 보존안을 확정하기 전에는 넓은 차단체, 손 Collider 또는 자동 씬 재생성을 적용하지 않는다.

## 2026-09-06 미러링 영상 분석: 몸통 차단과 헤드·손 관통 분리

### 영상 정보

- 분석 파일은 `C:\Users\lanoc\Downloads\oculus_cast_video_09_06_2026_09_32_11.mp4`다.
  사용자가 처음 전달한 `C:\Users\lanoc\Downloads\oculus\_cast\_video\_09\_06\_2026\_09\_32\_11.mp4`
  표기는 Markdown escape로 인한 경로 오해이며, 실제 파일명은 `_`를 그대로 포함한다.
- 원본 MP4는 수정하거나 삭제하지 않았다. 프레임 확인은 임시 폴더
  `C:\Users\lanoc\AppData\Local\Temp\codex_video_analysis_20260906_093211`에 추출한 이미지로만 수행했다.
- 영상 메타데이터는 재생 시간 `120.91초`, 해상도 `2336x1312`, FPS `13.73`, 비디오 코덱 `HEVC`,
  오디오 코덱 `AAC`다.
- `ffprobe`는 로컬 PATH에서 찾을 수 없어 실패했으며, 임시 Python 패키지 `imageio`, `imageio-ffmpeg`로
  읽기 전용 메타데이터와 프레임을 확인했다.

### 관통 재현 여부

- 영상 기준으로 관통은 재현됐다.
- `26-32초` 구간에서는 노란 방호복 또는 진열장 표면이 화면 대부분을 채워 HMD 시점이 대상 표면 내부나
  매우 깊게 겹친 상태로 보인다.
- `64-71초` 구간에서는 회색 프레임, 문틀 또는 진열장 측면 프레임과 손·시점이 가까이 겹쳐 보인다.
- `91-93초` 구간에서는 회색 문틀·프레임 표면 안쪽에 오른손과 HMD 시점이 함께 들어간 것처럼 보인다.

### HMD 관통

- HMD 관통은 확인됐다.
- `26초` 대표 프레임에서는 노란 방호복 표면과 표식이 화면 전체에 가깝게 표시되어 시점이 방호복 모델
  표면 안쪽 또는 표면에 과도하게 겹친 것으로 판단한다.
- `32초` 대표 프레임에서는 베이지 또는 목재 계열 표면이 화면 전체를 채워 선반·가구 표면 내부 관통으로
  판단한다.
- `64초` 대표 프레임에서는 회색 프레임과 파란 벽·문틀 사이가 화면을 크게 차지하며 시점이 프레임 사이로
  깊게 들어간 것으로 보인다.

### 왼손 관통

- 왼손 관통은 부분 확인으로 기록한다.
- `56-58초` 구간에서 화면 왼쪽 손이 흰 PPE와 선반 표면 위에 깊게 겹쳐 보인다.
- `84-85초` 구간에서 왼손이 장갑·마스크가 놓인 진열장 선반 안쪽으로 들어가며 PPE 및 선반과 겹친다.
- 다만 이 구간은 손이 표면을 완전히 통과했는지, 가까이 접촉한 상태인지 영상만으로 완전히 분리하기 어렵다.

### 오른손 관통

- 오른손 관통은 확인됐다.
- `70-71초` 구간에서 오른손이 진열장 상단 목재 선반과 회색 측면 프레임에 겹쳐 보인다.
- `91-92초` 구간에서 오른손은 회색 문틀 또는 프레임 안쪽에 들어간 상태로 보이며, 프레임의 앞뒤 깊이와
  손 메시가 시각적으로 충돌하지 않고 겹친다.

### 관통 대상 오브젝트

- 영상에서 식별 가능한 대상은 노란 방호복 모델, PPE 진열장 목재 선반, 흰색 선반 후면 또는 벽면,
  회색 금속 프레임·문틀형 구조물이다.
- 정확한 Unity 계층 경로와 오브젝트명은 영상만으로 확인 불가다. 후속 작업은 씬의 현재 Renderer와
  Collider Bounds를 읽어 실제 오브젝트를 식별해야 한다.

### 컨트롤러 또는 손 추적 모드

- 영상에는 컨트롤러 모델이 보이지 않고 양손 손 모델과 청록색 레이가 표시된다.
- 영상만으로 실제 입력 장치가 Quest 손 추적인지, 컨트롤러 입력을 손 모델로 렌더링한 상태인지는 확정할 수 없다.
- 후속 조사에서는 `XRHandSubsystem`, Controller Action, Near/Far Interactor, 손 모델 렌더 Transform의
  활성 경로를 분리해 확인한다.

### PPE Hover, Select, Grab 결과

- Hover는 확인됐다. `51초`, `69초` 부근에서 마스크 또는 고글 PPE에 초록색 Hover 링이 표시된다.
- Select와 Grab은 영상만으로 확인 불가다. PPE가 손에 붙어 이동하거나 선택 완료 상태로 전환되는 장면은
  명확히 확인되지 않는다.
- 관통 대응 후에도 진열장 각 단의 PPE Hover, Select, Grab은 반드시 별도 회귀 항목으로 보존한다.

### 원인 판단

- 사용자가 추가로 확인한 것처럼 몸 부분의 차단벽과 Collider는 동작한다. 이 사실은 XR Origin 또는
  `CharacterController` 기반 보행 이동 차단이 어느 정도 적용되고 있음을 뜻한다.
- 그러나 HMD 카메라와 손 모델은 실제 룸스케일 추적 Pose를 직접 따라가므로, 사용자가 현실에서 머리나 손을
  앞으로 움직이면 보행용 Collider와 별개로 환경 표면을 통과할 수 있다.
- 따라서 이 결함은 보행 차단 Collider 두께나 Bounds만의 문제가 아니라, `HMD/head intrusion`과
  `tracked hand visual intrusion`을 별도 소비자로 다뤄야 하는 문제다.
- HMD는 추적 장치의 실제 위치를 물리 Collider로 완전히 막을 수 없으므로, 침범 감지 후 페이드·비네트·경고
  또는 XR Origin 보정 후보를 비교한다.
- 손은 원본 추적 Pose와 표시용 손 모델을 분리해 접촉 표면 앞에서 멈추는 시각 프록시, 접촉 시 렌더 숨김,
  또는 접촉 피드백 후보를 비교한다.
- 카메라나 손 Transform에 Rigidbody를 직접 붙여 추적 Pose와 물리를 경쟁시키는 방식은 사용하지 않는다.

### 영상에서 확인할 수 없는 사항

- Unity Collider 설정, `CharacterController` 설정, XR Rig 충돌 처리 여부, 실제 Interaction Layer 구성은
  영상만으로 확인 불가다.
- 손 또는 컨트롤러 입력 소스, Hover 이후 Select와 Grab 이벤트 호출 여부, PPE 착용 상태 전이 여부는
  영상만으로 확인 불가다.
- 2026-09-04 보행용 Collider 보완이 스틱 이동 관통을 해결했는지 여부와, 이번 HMD·손 추적 관통을
  해결했는지 여부는 서로 구분해야 한다. 영상은 후자를 여전히 재현하는 근거로만 사용한다.

### 다음 읽기 전용 조사 항목

1. 현재 PPE Room 씬에서 노란 방호복, 진열장 선반, 회색 프레임·문틀 구조물의 실제 계층 경로,
   Renderer Bounds, Collider Bounds, Layer를 기록한다.
2. XR Origin, `CharacterController`, HMD Camera, 손 추적 루트, 손 렌더 모델, Near/Far Interactor의
   Transform 업데이트 경로를 읽기 전용으로 추적한다.
3. 몸통 이동 차단이 동작하는 경로와 HMD·손 추적 Pose가 이를 우회하는 경로를 표로 분리한다.
4. `입력 장치 -> Interactor -> Caster/Ray -> Layer/Collider 또는 Graphic -> Raycaster -> EventSystem/InputModule
   -> press/select action -> handler -> 상태 소유자` 경로를 Hover, Select, Grab 별로 확인한다.
5. 진열장 PPE 접근은 열린 선반 공간과 Marker·Grab Collider를 보존하는 조건에서만 대응안을 설계한다.
6. 구현 전에는 HMD 침범 감지, 손 표시 제한, PPE Grab 보존을 한 패치에 섞지 않고 각각의 검증 기준을 먼저 정한다.

### 검증 경계

- 이번 단계는 영상 분석과 문서 기록만 수행했다. Unity 씬, Collider, 코드, ProjectSettings, Prefab,
  Material, Asset은 수정하지 않았다.
- 정적 문서 확인과 영상 프레임 판정은 수행했지만, Unity Editor Play Mode와 Quest/OpenXR 후속 구현 검증은
  아직 수행하지 않았다.
