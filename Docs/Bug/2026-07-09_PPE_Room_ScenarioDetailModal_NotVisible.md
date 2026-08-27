# 2026-07-09 PPE Room Scenario Detail Modal 미표시 버그

| 항목 | 내용 |
|---|---|
| 일자 | 2026-07-09 |
| 대상 | `Assets/Scenes/3_PPE_Room.unity` |
| 관련 스크립트 | `Assets/Scripts/ScenarioDetailModal.cs`, `Assets/Scripts/ScenarioCardSelectProxy.cs` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / XR Interaction Toolkit 3.4.1 |
| 상태 | 수정 완료, 재검증 필요 |

## 증상

- `Scenario Selection HUD (1)`의 `LeftColor_Group` 카드 버튼을 눌러도 상세 모달이 화면에 보이지 않았다.
- 마우스 클릭과 XR Ray 선택 모두 반응이 불안정하게 보였고, 일부 로그에서는 선택 콜백이 호출되는 흔적이 있었다.
- 콘솔에는 `ScenarioDetailModal showing scenario X on Scenario Detail Modal.` 로그가 출력되지만, 실제 화면에는 모달이 나타나지 않았다.

## 원인

- `ScenarioDetailModal.Show()`는 실행되고 있었지만, 실제 렌더 대상인 상위 `Modal Canvas`가 비활성 상태였다.
- `Scenario Detail Modal`만 활성화해도 부모 Canvas가 꺼져 있으면 World Space UI는 화면에 렌더되지 않는다.
- 진단 로그에서 `modalRoot.active=False`와 `no parent Canvas`가 보여도, 이는 단순히 `modalRoot` 기준 진단만으로는 상위 Canvas 상태를 보지 못했기 때문에 원인을 놓치기 쉬웠다.

## 조치

- `ScenarioDetailModal.Show()`에서 모달을 열 때 `modalRoot`뿐 아니라 상위 `Canvas`까지 함께 활성화하도록 변경했다.
- `Hide()`에서도 같은 경로로 Canvas와 모달을 함께 비활성화하도록 정리했다.
- 상위 Canvas 탐색은 비활성 부모까지 확인할 수 있도록 `GetComponentInParent<Canvas>(true)`를 사용했다.
- 카드 입력 경로는 유지했다.
  - 마우스: `OnMouseDown`
  - UI 클릭: `IPointerClickHandler`
  - XR 선택: `XRSimpleInteractable.selectEntered`

## 결과

- 카드 클릭 시 `ScenarioDetailModal.Show()`는 호출된다.
- 모달이 부모 Canvas와 함께 활성화되어 화면 표시가 가능해졌다.
- 빌드 확인은 완료했다.

## 확인 항목

- [ ] Play Mode에서 카드 1/2/3 클릭 시 각각 모달이 실제로 표시되는지 확인
- [ ] 마우스 클릭과 XR Ray 둘 다 동일하게 동작하는지 확인
- [ ] 모달 닫기 후 다시 열 때 Canvas 상태가 정상 복구되는지 확인
- [ ] `훈련 상세 선택지` 전환 후 `정상 교육` / `돌아가기` 버튼이 정상 표시되는지 확인

## 참고 로그

- `ScenarioDetailModal showing scenario 1 on Scenario Detail Modal.`
- `ScenarioDetailModal showing scenario 2 on Scenario Detail Modal.`
- `ScenarioDetailModal showing scenario 3 on Scenario Detail Modal.`
- `ScenarioCardSelectProxy:OnMouseDown`
- `ScenarioCardSelectProxy:OnPointerClick`
- `ScenarioCardSelectProxy:OnSelectEntered`

