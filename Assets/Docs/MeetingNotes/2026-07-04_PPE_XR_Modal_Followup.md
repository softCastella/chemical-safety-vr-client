# 2026-07-04 PPE Room XR·시나리오 모달 후속 회의록

- 날짜: 2026-07-04
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`
- 환경: Unity 6000.4.8f1 / URP 17.4.0 / OpenXR

## XR 컨트롤러

- `Camera Offset` 아래에 `LeftController`, `RightController` 구성을 반영했다.
- 기존 독립형 `Left_NearFarInteractor`와 루트 `Directional Light` 직렬화 데이터를 제거했다.
- 컨트롤러를 얼굴 쪽으로 당길 때 표면이 사라지는 현상을 카메라 근거리 클리핑으로 확인했다.
- Main Camera Near Clip Plane을 `0.3m`에서 `0.05m`로 낮췄다.

## 시나리오 상세 모달

- 상세 모달 표시 중 배경 입력을 차단하는 오버레이를 유지한다.
- 월드 스페이스 HUD 영역만 덮던 오버레이 크기를 `6000 × 4000`으로 확장했다.
- 카드 내부 Canvas보다 앞에 렌더링되도록 모달 Canvas의 Sorting Order를 `1000`으로 설정했다.
- 모달을 열 때 오버레이 크기, Canvas 정렬, GraphicRaycaster를 런타임에서 보정한다.

## 훈련 시나리오 선택 흐름

- `훈련하기` 버튼을 선택해도 모달과 오버레이를 닫지 않는다.
- 기존 하단 버튼을 숨기고 다음 선택 버튼 두 개를 별도로 생성해 표시한다.
  - `PPE 불완전 착용 시나리오`
  - `정상 교육 시나리오`
- 두 선택 결과는 별도의 UnityEvent로 노출해 다음 교육 로직을 연결할 수 있게 했다.
- 상세 모달을 다시 열면 기존 `훈련하기`/`돌아가기` 상태로 복원한다.

## 관련 파일

- `Assets/Scripts/ScenarioDetailModal.cs`
- `Assets/Editor/ScenarioDetailModalSceneBuilder.cs`
- `Assets/Scenes/3_PPE_Room.unity`

## 검증 상태

- Runtime 및 Editor C# 빌드 오류 0개를 확인했다.
- [ ] Unity Play Mode에서 오버레이가 모든 카드와 HUD보다 앞에 표시되는지 확인
- [ ] `훈련하기` 선택 후 두 시나리오 버튼이 즉시 나타나는지 확인
- [ ] 두 시나리오 버튼의 XR Ray/Trigger 입력 확인
- [ ] Quest에서 컨트롤러를 HMD 가까이 이동했을 때 메시 절단 해소 확인
- [ ] 각 선택 UnityEvent에 실제 교육 흐름 연결
