# XR 타이틀/인트로 UI 수정 롤백 포인트

이 문서는 `2026-07-05` 기준으로 적용한 XR 타이틀/인트로 UI 수정의 되돌리기 기준을 남긴다.

## 수정 대상

- `Assets/Scenes/2_Intro.unity`
- `Assets/Scenes/1_TitleScene.unity`

## 적용한 변경

### 1. `2_Intro.unity`

- `Canvas`를 `XR Origin (VR)`의 자식에서 분리해 씬 루트로 옮겼다.
- 목적: 머리 움직임을 따라가는 현상을 제거하고, 인트로 로고가 월드에 고정되도록 하기 위함.

되돌릴 때는 다음을 복원한다.

- `RectTransform`의 부모를 `XR Origin (VR)`로 되돌린다.
- `XR Origin (VR)`의 자식 목록에 `Canvas`를 다시 넣는다.

### 2. `1_TitleScene.unity`

- `PartnerLogos`의 앵커와 피벗을 중앙 기준으로 바꿨다.
- 목적: 레이아웃 조정 시 아래로 끌려 내려가는 느낌을 줄이고, 기준점을 안정화하기 위함.

되돌릴 때는 다음을 복원한다.

- `PartnerLogos` `RectTransform`
  - `AnchorMin = {x: 0.5, y: 0}`
  - `AnchorMax = {x: 0.5, y: 0}`
  - `Pivot = {x: 0.5, y: 0}`
  - `AnchoredPosition.y = -4.9000015`

## 롤백 순서

1. Unity에서 해당 씬을 연다.
2. `2_Intro`는 `Canvas`가 `XR Origin (VR)`의 자식인지 확인한다.
3. `1_TitleScene`은 `PartnerLogos`의 앵커/피벗을 확인한다.
4. 씬 저장 후 Play Mode에서 결과를 다시 확인한다.

## 주의

- 이 수정은 UI 기준점을 안정화하는 쪽이다.
- 롤백하면 예전처럼 머리 추적 또는 아래로 밀림이 다시 나타날 수 있다.
