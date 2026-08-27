# 2026-07-09 PPE Room Scenario Detail Modal 후속 회의록

- 일자: 2026-07-09
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`
- 주제: Scenario 카드 선택, 상세 모달 표시, XR/마우스 입력 공통 처리

## 논의 내용

- 카드 클릭은 동작하는데 모달이 보이지 않는 문제가 반복되었다.
- 콘솔 로그를 확인한 결과, 카드 입력 자체는 `ScenarioDetailModal.Show()`까지 전달되고 있었다.
- 최종 원인은 `Scenario Detail Modal`이 아니라 상위 `Modal Canvas`의 비활성 상태였다.
- World Space UI는 자식 오브젝트만 켜도 부모 Canvas가 꺼져 있으면 화면에 나오지 않는다.

## 결정 사항

- 카드 입력은 하나의 공통 진입점으로 유지한다.
  - 마우스 클릭
  - UI Pointer Click
  - XR Ray Select
- `ScenarioCardSelectProxy`는 각 카드에 붙여서 카드별로 `scenarioIndex`만 넘긴다.
- `ScenarioDetailModal`은 모달 표시와 숨김을 책임지고, 부모 Canvas까지 함께 토글한다.
- 카드별 상세 텍스트 구조는 `Scenario Title` / `Scenario Description`의 개별 오브젝트를 직접 활성화하는 방식으로 유지한다.

## 작업 결과

- `ScenarioDetailModal.cs`
  - `Show()`에서 부모 Canvas와 모달 루트를 함께 활성화하도록 수정
  - `Hide()`에서 같은 경로로 함께 비활성화하도록 수정
  - 비활성 부모까지 포함해 Canvas 진단 가능하도록 보강
- `ScenarioCardSelectProxy.cs`
  - `OnMouseDown`, `OnPointerClick`, `OnSelectEntered` 모두 같은 `Trigger()`로 연결

## 재확인 항목

- [ ] Game View에서 마우스로 카드 선택 시 모달 표시 확인
- [ ] XR Ray로 카드 선택 시 모달 표시 확인
- [ ] 모달 열림 후 `정상 교육` 선택 시 하위 선택지 표시 확인
- [ ] `돌아가기` 선택 시 카드 상세 화면으로 복귀 확인
- [ ] 모달 닫기 후 재오픈 시 Canvas 상태 복구 확인

## 비고

- 이번 이슈는 클릭 이벤트가 아니라 렌더 트리 상태 문제였다.
- 로그에 `Show()`가 찍혀도 실제 표시 여부는 부모 Canvas 활성 상태까지 확인해야 한다.

