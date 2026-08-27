# XR UI Canvas Play Mode Transform Overwrite

Date: 2026-07-30

## Symptom

The scene-authored position of `XR UI Canvas` appeared to be replaced after entering Play Mode.

## Cause

- The root `RectTransform` serialized different X/Y values in `m_LocalPosition` and `m_AnchoredPosition`.
- `XRWorldCanvasPlacement` performed an unconditional camera-relative placement from `Start` whenever attached.
- `ScenarioDetailModal` also changed the active state of the entire parent Canvas while showing or hiding only its modal content.

## Fix

- Synchronized the root Canvas local and anchored positions in `3_PPE_Room_Loco.unity`.
- Changed `XRWorldCanvasPlacement` to run only through the explicit `PlaceFromCamera` method.
- Limited `ScenarioDetailModal` visibility changes to its serialized modal root.
- Added `XRUiCanvasPlayModeValidator` to compare the Canvas transform immediately before and after entering Play Mode.

## Validation

Run `Tools > UI > Validate XR UI Canvas Play Mode Transform`, then enter Play Mode. The Console should report that the transform remained unchanged.

## 2026-08-05 추가: Modal Canvas 작성 포즈 보호

### 증상

- `Modal Canvas`를 원하는 월드 위치에 맞춘 뒤 씬을 저장해도, Play Mode 진입이나
  리로드 과정에서 포즈가 이전 값/어긋난 RectTransform 값으로 보이거나 덮어써질 수 있다.

### 원인

- 루트 World Canvas `RectTransform`이 `m_LocalPosition` XY와 `m_AnchoredPosition` XY를
  다르게 직렬화하면 Unity가 로드 시 한쪽을 기준으로 재계산한다.
- `XR UI Canvas` / `Modal Canvas`는 Play 중 캔버스 루트를 끄면 안 되므로, 포즈는
  런타임 배치 스크립트가 아니라 씬 작성 값이 권위여야 한다.

### 조치

- `AuthoredWorldCanvasPose`를 `Modal Canvas`에 추가했다. 직렬화된 작성 포즈를
  Play `Awake`/`Start`에서 다시 적용한다.
- 씬 저장 시(`AuthoredWorldCanvasPoseSaver`) 현재 Transform을 스냅샷에 캡처한다.
- `_scale_0`의 `Modal Canvas` local/anchored XYZ를 `(0.02, 1.43, 1.6)`으로 동기화했다.
- Play Mode 검증기에 `Modal Canvas`를 포함했다.
