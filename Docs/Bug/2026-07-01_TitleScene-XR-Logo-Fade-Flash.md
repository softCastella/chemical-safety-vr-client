# 버그 리포트: TitleScene 타이틀 로고 페이드 인 플래시

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-01 |
| 프로젝트 | 3D_UI_Test_home |
| Unity | 6000.4.8f1 |
| 대상 환경 | XR / OpenXR, Meta Quest·Android 및 PC OpenXR |
| 대상 씬 | `Assets/Scenes/TitleScene.unity` |
| 관련 스크립트 | `Assets/Scripts/TitleSplashController.cs` |
| 상태 | 해결 및 사용자 확인 완료 |

## 요약

`TitleScene` 시작 시 타이틀 로고에 `CanvasGroup` 페이드 인을 적용했지만, 파트너 로고와 달리 타이틀 로고가 하늘색으로 잠깐 보인 뒤 불투명 상태로 번쩍이며 나타났다. 타이틀 로고를 실제 페이드 전에 알파 0 상태로 3프레임 사전 렌더링하여 문제를 해결했다.

## 증상

- 타이틀 로고가 자연스럽게 페이드 인되지 않고 순간적으로 나타남
- 로고가 나타나기 직전에 하늘색 영역이 번쩍이는 것처럼 보임
- 같은 `CanvasGroup` 페이드 로직을 사용하는 파트너 로고는 정상 동작
- 타이틀 이미지를 흰 배경 이미지로 변경해도 동일하게 재현됨

## 재현 절차

1. Unity에서 `Assets/Scenes/TitleScene.unity`를 연다.
2. `Canvas`의 `TitleSplashController`가 활성화되어 있는지 확인한다.
3. Play Mode 또는 XR 기기에서 씬을 실행한다.
4. 첫 번째 타이틀 로고가 표시되는 순간을 관찰한다.

## 조사 과정

다음 항목을 순차적으로 확인했지만 단독으로는 해결되지 않았다.

1. `CanvasGroup.alpha`를 씬에서 0으로 저장
2. 초기 투명도 설정을 `Start()`에서 `Awake()`로 이동
3. 타이틀 로고 페이드 시간을 0.2초에서 1초로 증가
4. 선형 보간 대신 `Mathf.SmoothStep` 적용
5. XR 첫 프레임의 큰 `unscaledDeltaTime`에 대비해 프레임당 페이드 진행량 제한
6. 페이드 시작 전 1초 초기 지연 추가
7. 원본 이미지의 배경 광원 및 알파 구성 확인

원본 이미지의 광원이 시각적 현상을 강조할 수는 있었지만, 흰 배경 이미지에서도 동일하게 재현되어 이미지 자체만의 문제는 아닌 것으로 판단했다.

## 원인

타이틀 로고는 XR 씬 시작 후 처음 사용되는 비교적 큰 UI 텍스처다. 첫 표시 시점에 Canvas 갱신, 텍스처 업로드 또는 UI 렌더링 준비가 함께 발생하면서 초기 페이드 프레임이 정상적으로 보이지 않았다.

파트너 로고는 타이틀 로고 이후에 표시되므로 해당 시점에는 Canvas와 GPU 렌더링 경로가 이미 준비되어 자연스럽게 페이드 인되었다.

## 해결 방법

### 1. 알파 0 상태 사전 렌더링

실제 페이드 시작 전에 Canvas를 강제로 갱신하고, 타이틀 로고를 알파 0 상태로 여러 프레임 렌더링한다.

```csharp
Canvas.ForceUpdateCanvases();
for (int frame = 0; frame < prewarmFrames; frame++)
    yield return new WaitForEndOfFrame();
```

기본값은 다음과 같다.

```text
Prewarm Frames: 3
Initial Delay: 1초
Primary Fade In Duration: 1초
```

### 2. 투명 UI 메시지 컬링 해제

타이틀 로고의 `CanvasRenderer.Cull Transparent Mesh`를 비활성화했다. 이를 통해 알파가 0이어도 사전 렌더링 준비가 수행된다.

```text
logo2d > Canvas Renderer > Cull Transparent Mesh: Off
```

### 3. 기존 페이드 방식 유지

타이틀과 파트너 로고 모두 기본 UI 머티리얼과 `CanvasGroup`을 사용한다. 별도의 커스텀 셰이더나 이미지 교체는 최종 해결책에 포함하지 않았다.

## 검증 결과

- 타이틀 로고가 번쩍이지 않고 자연스럽게 페이드 인됨
- 파트너 로고의 기존 페이드 동작 유지
- 두 로고 모두 페이드 완료 후 화면에 유지
- 다음 씬 자동 전환 없음
- 사용자 확인 결과: 해결 완료

## 관련 파일

- `Assets/Scenes/TitleScene.unity`
- `Assets/Scripts/TitleSplashController.cs`
- `Assets/Scripts/TitleSplashController.cs.meta`
- `Assets/UIs/Logo/VrLogo_2d.png`

## 회귀 방지 참고

- 첫 화면에서 큰 UI 텍스처를 페이드할 때는 실제 노출 전에 알파 0 상태로 2~3프레임 예열한다.
- 예열 대상의 `CanvasRenderer.Cull Transparent Mesh`가 활성화되어 있으면 알파 0 상태에서 렌더 준비가 생략될 수 있다.
- XR 초기화 직후의 첫 렌더링은 일반 UI보다 프레임 지연 가능성이 높으므로, 시간 지연만 추가하는 것보다 사전 렌더링이 더 직접적인 해결책이다.
- 이미지 교체 후에도 동일 증상이 발생하면 원본 알파보다 첫 사용 시점의 렌더링 준비 비용을 우선 점검한다.
