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

## 2026-08-30 후속: 타이틀 로고 시머링과 시작 씬 전환 잔상

### 사용자 관찰 증상

- 활성 빌드 흐름의 `1_Title`에서 로고가 아지랑이처럼 흔들려 보인다.
- 최초 실행의 타이틀·인트로 구간에서 카메라 앞에 투명한 셀로판 면이 있는 것 같은 잔상이 간헐적으로 보인다.

### 최근 변경 우선 조사

- 직전 원격 커밋 `8b1b4300349529a3ebcb260f1da614769bef6b34`은 PPE 음원·음성 분기와
  `3_PPE_Room_3mode_loco`만 변경했다. `0_App`, `1_Title`, `2_Intro`, 타이틀 로고 원본과
  시작 전환 코드는 변경하지 않았으므로 이번 현상을 직전 음원 변경 회귀로 분류하지 않는다.
- 현재 `VrLogo_2d.png`에는 2026-08-28의 mipmap, Trilinear, aniso 8과 Android 고품질 압축이
  적용되어 있다. 사용자가 이 상태에서도 로고 시머링을 관찰했으므로 Android 블록 압축과 넓은
  반투명 Glow의 상호작용을 다음 단일 샘플링 후보로 둔다.
- 2026-08-30 Unity Editor/Quest Link 기준 실행 로그에서 `1_Title` 로드는 1.267초,
  `2_Intro` 로드는 1.082초가 걸렸다. 정지 화면 전환 중 이 정도의 새 씬 역직렬화·통합 정지는
  OpenXR 컴포지터가 이전 프레임을 재투영하는 구간을 만들 수 있으며, 머리 움직임에 따라 얇은
  투명 면이 휘는 것 같은 잔상으로 인식될 수 있다. 이는 로그와 증상을 대조한 원인 추론이며,
  Quest 양안 캡처로 아직 확정하지 않았다.

### 변경 전 필수 질문

1. `1_Title`과 `2_Intro`의 RectTransform, Canvas, 색, 페이드 시간과 원본 PNG는 보존한다.
2. 로고 화질의 단일 기준은 `VrLogo_2d.png`의 `TextureImporter`, 시작 전환의 상태 소유자는
   `TitleSplashController`와 `LoadingSceneController`다.
3. 시작 씬은 자동 전환이며 입력 소비자가 없다. XR 입력, `EventSystem`, Raycaster는 변경하지 않는다.
4. 대상 씬이 Build Settings에 없거나 비동기 로드를 시작하지 못하면 자동 생성·fallback 없이 기존처럼
   한 번의 명확한 오류로 중단한다.
5. 영향 소비자는 타이틀 로고 샘플링과 `Title → Intro → Loading` 전환이다. PPE, 음성, 텔레포트,
   거울과 훈련 상태는 변경하지 않는다.
6. 변경 전 기준은 위 실제 씬 로드 시간과 현재 Android 고품질 압축 상태다. 변경 후에는 같은 시작
   경로의 씬 로드 로그와 정면·머리 이동 화면을 비교한다.
7. 정적 설정, C# 컴파일과 Unity Import를 먼저 확인하고 Game View, Quest/OpenXR 양안 확인을 구분한다.

### 적용 방향

- 타이틀 로고의 넓은 반투명 Glow에 대한 Android/Standalone 블록 압축을 제거한다. mipmap,
  Trilinear, aniso 8, Clamp와 원본 이미지는 유지한다.
- `TitleSplashController`는 로고가 보이기 전에 `2_Intro` 비동기 로드를 시작하고 활성화만 기존
  페이드 종료 시점까지 보류한다.
- `IntroSceneTransition`이 호출하는 로딩 씬 진입도 동기 `LoadScene` 대신 비동기 로드를 사용한다.
  표시 시간, 페이드 순서와 최종 PPE 대상 씬은 유지한다.

### 검증 상태

- 정적 확인: `1_Title`의 `preloadNextScene=1`, 로고 `Preserve Aspect=On`, Android/Standalone
  `RGBA32 + Uncompressed`, mipmap/Trilinear/aniso 8 유지와 `TitleSplashController`의
  `allowSceneActivation=false → 페이드 종료 뒤 true` 순서를 대조했다.
- 회귀 하네스: `AppStartupSynchronizationHarness`는 타이틀의 인트로 사전 로드를 검사하고,
  `PPELocomotionPpeRegressionValidationHarness`는 타이틀 씬 종횡비와 양 플랫폼 비압축 로고 설정을
  검사하도록 확장했다.
- C# 정적 빌드: `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 각각 순차 실행해
  오류 0개를 확인했다. 기존 패키지 참조·obsolete 경고는 남아 있다.
- 열린 Unity Editor는 파일 변경 뒤 자동 Refresh/Import 로그를 아직 남기지 않아 Unity Import와
  메뉴 하네스 실행은 미확인이다. 첫 병렬 정적 빌드에서는 두 빌드가 같은 출력 DLL을 잡아 파일 잠금
  오류가 났으며, 순차 재실행에서는 두 빌드 모두 통과했다.
- Quest/OpenXR 양안에서 로고 외곽·얇은 영문선의 시간축 안정성과 전환 중 셀로판 잔상은 수동 비교가
  필요하다. Game View만으로 완료 판정하지 않는다.
