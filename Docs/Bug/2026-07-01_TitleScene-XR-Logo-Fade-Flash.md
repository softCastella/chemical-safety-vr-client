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

## 2026-09-02 후속: 파트너사 로고 시머링

### 사용자 관찰과 최근 변경 대조

- Quest 독립 실행에서 메인 타이틀 아래 파트너사 로고 3개가 계속 반짝이고 아지랑이처럼 보였다.
- 2026-08-30 품질 변경은 `VrLogo_2d.png`만 비압축·mipmap·Trilinear 대상으로 삼았고,
  `to21_logo.png`, `seoulit_logo.png`, `Immersa_Lineup_Logo_nuki.png`는 mipmap 비활성,
  Bilinear, aniso 1, Android 기본 압축 품질 50으로 남아 있었다.
- 따라서 이번 증상은 Quest Link 단절보다 최근 품질 패치에서 파트너 로고가 누락된 회귀 후보로
  먼저 분류한다.

### 변경 전 필수 질문

1. `1_Title`의 파트너 로고 RectTransform, 앵커, 크기, 색, 페이드와 원본 PNG를 보존한다.
2. 단일 기준은 각 파트너 PNG의 `TextureImporter`이며 상태 소유자는 기존 `TitleSplashController`다.
3. 자동 전환 화면이므로 입력 경로는 없고 XR Interactor, Raycaster, EventSystem을 변경하지 않는다.
4. 누락 참조를 런타임에서 자동 수리하지 않으며 Importer 누락은 회귀 하네스의 명확한 오류로 중단한다.
5. 이번 단계의 소비자는 파트너 로고 3개뿐이다. 메인 로고, 컨트롤러 가이드, 거울, 진열장과 PPE 흐름은
   변경하지 않는다.
6. 변경 전 기준은 사용자의 Quest 관찰과 mipmap Off/Bilinear/aniso 1/압축 품질 50이다. 변경 후 같은
   거리와 머리 움직임에서 외곽선 반짝임을 비교한다.
7. 정적 Importer·회귀 하네스·Unity Import와 Quest/OpenXR 양안 검증을 서로 구분한다.

### 적용 내용과 검증 상태

- 세 파트너 로고에 mipmap, Trilinear, aniso 8을 적용했다.
- 넓은 투명 경계와 작은 글자의 블록 압축 흔들림을 제거하도록 Android/Standalone을 메인 타이틀과 같은
  `RGBA32 + Uncompressed`로 맞췄다. 소스 크기가 작아 메모리 증가는 제한적이다.
- `PPELocomotionPpeRegressionValidationHarness`가 세 로고의 샘플링 및 양 플랫폼 비압축 설정을 검사하도록
  확장했다.
- 정적 설정 대조와 Runtime/Editor C# 빌드는 오류 0개로 통과했다.
- Unity 배치 하네스는 `com.unity.editor.headless` 라이선스 부재로 Editor 초기화 전에 종료되어 실행하지
  못했다. 일반 Unity Import·메뉴 하네스와 새 APK의 Quest 양안 비교는 아직 필요하다.

## 2026-09-08 후속: Render Scale 1.0과 파트너 로고 선명도 비교

### 사용자 요청과 보존 범위

- Quest 2 독립 실행에서 글자와 이미지가 전반적으로 뭉개져 보인다는 사용자 관찰에 따라 Android용
  `Mobile_RPAsset`의 Render Scale만 `0.9`에서 `1.0`으로 올렸다.
- UI RectTransform, Canvas, 로고 원본, 페이드 순서·시간, 오디오, 입력, PPE 상태 전이는 변경하지 않았다.
- 동적 해상도, Android Foveated Rendering과 업스케일링은 비활성 상태를 유지하고 MSAA 4x도 보존했다.

### 비교 결과와 원인 범위

- Render Scale 0.9 APK의 Quest OpenXR eye texture는 `1296x1426`, Render Scale 1.0 APK는
  `1440x1584`로 생성됐다. 따라서 새 APK에 1.0 설정이 실제 반영된 사실까지 확인했다.
- 1.0에서도 파트너 로고가 부드럽고 뭉개져 보인다는 사용자 관찰이 남았다. Android/Standalone 로고는
  이미 `RGBA32 + Uncompressed`였으므로 블록 압축을 이번 증상의 직접 원인으로 보지 않는다.
- `to21_logo.png`는 `186x75`, `seoulit_logo.png`는 `283x120`으로 원본 자체가 작다.
  `Immersa_Lineup_Logo_nuki.png`는 `756x375`, 메인 `VrLogo_2d.png`는 `1536x1024`다. 메인 로고의
  Glow와 반투명 표현은 원본 이미지에 포함되어 있어 Importer 설정만으로 제거할 수 없다.
- 파트너 로고 3개를 단일 비교 변수로 `mipmap Off + Bilinear + aniso 1`로 바꾼 APK를 설치했다.
  사용자는 정지 시 선명도보다 머리 이동 중 로고가 약간 번쩍이는 시머링을 관찰했다. 이 결과로 전체
  안티앨리어싱과 밉맵을 제거하는 방향은 채택하지 않는다.
- 최종 후속 후보는 파트너 로고 3개의 `mipmap + Trilinear + aniso 8`을 복구하면서 `mipBias=-0.5`만
  적용한 균형 설정이다. 해당 APK는 빌드·설치됐지만 Quest 앱 라이브러리 미표시로 사용자 양안 평가는
  아직 완료하지 못했다.

### 완료한 검증과 후속 작업

- `PPELocomotionPpeRegressionValidationHarness`가 Render Scale 1.0과 파트너 로고의
  mipmap/Trilinear/aniso 8/mip bias -0.5를 검사하도록 갱신했다.
- `Assembly-CSharp-Editor.csproj --no-restore`는 경고 0개, 오류 0개로 통과했다. Unity 빌드 전 하네스도
  중단 없이 통과했고 Development APK 생성이 완료됐다.
- 최종 Development APK는 277,724,175 bytes, SHA-256
  `81C59E9375AD347330992230A8382BECF88B8D021DDEE56FD7FE5D77A8E84DAF`이며 package
  `com.tycheworks.immersa.safetyvr`, version code 5, ARM64, Target SDK 34, zipalign과 APK Signing v2를
  확인했다.
- 후속 실기에서는 같은 거리와 머리 이동 조건으로 mip bias -0.5 버전의 정지 선명도와 시간축 안정성을
  양안 비교한다. 시머링이 남으면 MSAA를 제거하지 않고 고해상도 공식 PNG 또는 SVG 원본을 먼저 확보한다.
- Quest/OpenXR 사용자 평가 전에는 균형 설정을 최종 화질 해결로 보고하지 않는다.

## 2026-09-09 후속: 파트너 로고 거리 단일 변수 비교

### 실행 전 필수 판단

1. **기존 Inspector/씬 작성값 보존:** `Assets/Scenes/1_Title.unity`의 `PartnerLogos` 로컬 Z만
   `0`에서 `-20`으로 변경한다. RectTransform 크기·앵커·피벗, 자식 로고 크기, Canvas Z와 다른 UI 작성값은
   보존한다.
2. **단일 기준과 상태 소유자:** 위치 기준은 생산 씬의 `Canvas/PartnerLogos` RectTransform이다.
   `TitleSplashController`는 alpha만 변경하며 Z를 런타임에 덮어쓰지 않는다.
3. **입력·렌더링 경로:** 입력 소비자는 없다. 렌더링 경로는 `Main Camera → World Space Canvas →
   PartnerLogos → 자식 Image/Sprite`이며 Android Mobile URP 설정을 사용한다.
4. **실패 처리:** 런타임 자동 보정이나 fallback을 추가하지 않는다. 비교 실패 시 씬 작성 Z만 `0`으로
   복구한다.
5. **영향 소비자:** Title 씬의 파트너 로고 3종과 XR 양안 깊이·정렬만 직접 영향을 받는다. 메인 로고,
   PPE, 입력, 오디오, 텔레메트리와 다른 씬은 변경하지 않는다.
6. **비교 실행:** 변경 전은 Canvas 작성 Z `200`, PartnerLogos 로컬 Z `0`이다. 변경 후에는 로컬 Z `-20`
   한 값만 달리해 같은 HMD, 시작 자세와 머리 이동에서 정지 선명도·시머링·화면 점유율·메인 로고와의
   깊이 정렬을 비교한다.
7. **검증 수준:** 현재는 씬·런타임 대입 경로의 정적 조사만 완료했다. 적용 후 Unity Import와 Game View를
   확인하고, 최종 채택은 Quest/OpenXR 양안 비교 뒤에만 결정한다.

`-20`은 현재 Canvas 작성 Z `200` 대비 약 10%를 카메라 방향으로 이동시키는 첫 비교값이다. 실제 월드 거리와
카메라 방향은 Unity에서 `PartnerLogos`를 선택한 공간 진단 결과로 다시 확인하며, 방향이 예상과 다르면
씬 값을 추가 변경하지 않고 기준값으로 복구한다. 이 단계에서는 Render Scale, mipmap, filter, aniso,
mip bias와 Android texture format을 변경하지 않는다.

### 적용 및 인수인계 상태

- `Assets/Scenes/1_Title.unity`의 `PartnerLogos` 로컬 Z만 `0`에서 `-20`으로 적용했다.
- **정적 확인:** Title 씬 diff는 1개 값의 1줄 변경이며 `git diff --check`를 통과했다.
- **Unity Editor 확인:** 배치 Import에서 `1_Title`이 정상 로드됐고
  `AppStartupSynchronizationHarness.Validate`가 PASS했다. 이는 씬 로드와 시작 계약 확인이며 시각 품질
  확인을 대신하지 않는다.
- **Play Mode 확인:** 미실행이다. 다음 세션에 같은 자세에서 선명도·점유율·깊이 정렬을 비교한다.
- **Quest/OpenXR 확인:** 미실행이다. 같은 Quest에서 시머링·양안·주변 시야를 비교한 뒤 채택 여부를 정한다.

### 적용 상태

- `Assets/Scenes/1_Title.unity`의 `Canvas/PartnerLogos` 로컬 Z를 `0`에서 `-20`으로 변경했다.
- 변경 후 diff에서 해당 RectTransform 위치 한 줄만 바뀐 것을 정적으로 확인했다.
- 현재 Unity Editor에는 `4_PPE_Room`이 열려 있으므로 Title 씬 Game View와 Quest/OpenXR 양안 결과는
  아직 확인하지 않았다. 이 비교가 끝날 때까지 텍스처 설정은 변경하지 않는다.

## 2026-09-09 후속: 사용자 거리 효과 확인과 위치 미세 조정

### 사용자 확인

- 사용자는 `PartnerLogos`를 로컬 Z `0`에서 `-20`으로 당긴 뒤 타이틀 로고 렌더링 품질에 효과가 있는
  것으로 확인했다.
- 확인한 실행 환경이 Game View인지 Quest/OpenXR 양안인지는 이번 보고만으로 확정하지 않는다. 위치
  미세 조정 뒤 같은 환경에서 다시 비교하고, Quest/OpenXR 완료 여부는 별도로 기록한다.

### 변경 전 필수 판단

1. **기존 Inspector/씬 작성값 보존:** 생산 대상 `Assets/Scenes/1_Title.unity`의
   `Canvas/PartnerLogos` RectTransform에서 로컬 Y와 Z만 조정한다. 크기, 앵커, 피벗, 자식 로고,
   Canvas와 TextureImporter 값은 보존한다.
2. **단일 기준과 상태 소유자:** 위치의 단일 기준은 `PartnerLogos` RectTransform이다.
   `TitleSplashController`는 해당 Transform을 변경하지 않고 CanvasGroup alpha만 제어한다.
3. **입력·렌더링 경로:** 입력 소비자는 없다. 렌더링 경로는 `Main Camera → World Space Canvas →
   PartnerLogos → Image/Sprite`다.
4. **실패 처리:** 런타임 자동 보정이나 fallback을 추가하지 않는다. 조정 결과가 나쁘면 로컬 Y `-4.9000015`,
   Z `-20`의 직전 사용자 확인값으로 복구한다.
5. **영향 소비자:** Title 파트너 로고 3종의 위치·깊이·선명도만 영향을 받는다. 메인 로고, 페이드, 오디오,
   입력, PPE와 텔레메트리는 변경하지 않는다.
6. **변경 전후 비교:** 현재 Y `-4.9000015`, Z `-20`에서 Y `-3.9`, Z `-25`로 한 단계만 이동한다.
   같은 시작 자세에서 화면 점유율, 메인 로고와의 정렬, 시머링, 깊이 불편과 잘림을 비교한다.
7. **검증 수준:** 씬 작성값과 런타임 대입 경로의 정적 확인까지 완료했다. 변경 후 Unity Import와 사용자
   시각 비교가 필요하며, Quest/OpenXR 양안 확인 전에는 최종 완료로 확대하지 않는다.

### 적용 상태

- `PartnerLogos`의 로컬 Z를 `-20`에서 `-25`로, anchored Y를 `-4.9000015`에서 `-3.9`로 변경했다.
- 씬 diff는 대상 RectTransform의 두 값만 변경됐고 크기·앵커·피벗·자식 Transform과 렌더링 설정은
  그대로 유지됐다.
- `git diff --check`는 오류 없이 통과했다. Unity Editor가 생산 `1_Title` 씬을 열고 있지만, 변경 후
  시각 비교와 Quest/OpenXR 양안 확인은 아직 사용자의 재실행이 필요하다.

## 2026-09-09 후속: 메인 로고 깊이와 파트너 로고 한 높이 이동

### 변경 전 필수 판단

1. **기존 Inspector/씬 작성값 보존:** 생산 `1_Title` 씬에서 `TitleLogo2d`의 로컬 Z와
   `PartnerLogos`의 로컬 Z·anchored Y만 변경한다. 두 요소의 크기, 앵커, 피벗, X 위치, 자식 로고와
   렌더링 설정은 보존한다.
2. **단일 기준과 상태 소유자:** 메인 로고는 `TitleLogo2d`, 파트너 로고는 `PartnerLogos` RectTransform이
   각각 위치 기준이다. `TitleSplashController`는 두 Transform을 변경하지 않고 alpha만 제어한다.
3. **입력·렌더링 경로:** 입력 소비자는 없고 `Main Camera → World Space Canvas → 각 로고 Image` 경로만
   영향을 받는다.
4. **실패 처리:** 런타임 자동 보정은 추가하지 않는다. 결과가 나쁘면 메인 Z `0`, 파트너 Y `-3.9`,
   Z `-25`의 직전 값으로 복구한다.
5. **영향 소비자:** Title 메인 로고와 파트너 로고 3종의 깊이·배치만 영향을 받는다. 페이드, 오디오,
   입력, PPE, 텔레메트리는 보존한다.
6. **변경 전후 비교:** 메인 로고는 Z `0 → -10`으로 조금 당긴다. 파트너 로고는 현재 높이 `12`만큼
   Y `-3.9 → 8.1`로 올리고 Z `-25 → -35`로 더 당긴다. 같은 자세에서 겹침·잘림·선명도·시머링과
   깊이 불편을 비교한다.
7. **검증 수준:** 현재 씬 작성값과 런타임 대입 경로를 정적으로 확인했다. 적용 후 Unity와 Quest/OpenXR
   사용자 시각 비교가 필요하다.

### 적용 상태

- `TitleLogo2d`의 로컬 Z를 `0`에서 `-10`으로 변경했다.
- `PartnerLogos`의 로컬 Z를 `-25`에서 `-35`로, anchored Y를 현재 높이 `12`만큼
  `-3.9`에서 `8.1`로 변경했다.
- 최종 씬 diff에서 대상 두 RectTransform의 세 값만 변경됐고 `git diff --check`는 오류 없이 통과했다.
- Unity Editor와 Quest/OpenXR 시각 비교는 아직 필요하다. 파트너 로고가 메인 로고와 겹치거나 잘리거나
  깊이 불편이 생기면 직전 값으로 복구한다.

## 2026-09-09 후속: 파트너 로고 추가 전진과 버전 깊이 정렬

### 사용자 확인과 변경 전 필수 판단

1. 사용자는 파트너 로고를 앞당긴 것이 렌더링 품질 개선에 효과가 있다고 확인했다.
2. 기존 Inspector 작성값 중 `TitleLogo2d` Z `-10`, `PartnerLogos` anchored Y `8.1`, 크기·앵커·피벗과
   자식 로고 값은 보존한다.
3. 위치 기준은 `PartnerLogos`와 기존 씬 작성 `Version` RectTransform이다. `TitleSplashController`는
   기존 `Version`의 Transform을 덮어쓰지 않고 텍스트·alpha만 갱신한다.
4. 입력 소비자는 없으며 Title의 파트너 로고와 버전 표시에만 영향을 준다. 오디오, 페이드 순서, PPE와
   텔레메트리는 변경하지 않는다.
5. 런타임 자동 보정은 추가하지 않는다. 결과가 나쁘면 파트너 로고 Z `-35`, 버전 Z `0`으로 복구한다.
6. 파트너 로고는 Z `-35 → -50`으로 더 당기고 `Version`도 동일한 Z `-50`으로 맞춘다. 같은 자세에서
   선명도·시머링·깊이 불편, 버전 글자의 가독성과 다른 요소와의 겹침을 비교한다.
7. 현재 검증은 씬 직렬화와 런타임 대입 경로의 정적 확인까지다. 변경 후 Unity와 Quest/OpenXR 사용자
   시각 확인이 필요하다.

### 적용 상태

- 사용자의 정정에 따라 추가 전진 대상은 메인 타이틀 로고가 아니라 `PartnerLogos`로 확정했다.
- `PartnerLogos`의 로컬 Z를 `-35`에서 `-50`으로 변경하고, `Version`도 같은 로컬 Z `-50`으로 맞췄다.
- `TitleLogo2d`는 직전 작성값 Z `-10`, `PartnerLogos`의 anchored Y는 한 높이 올린 `8.1`을 보존했다.
- 최종 씬 diff에서 `Version`, `TitleLogo2d`, `PartnerLogos`의 의도한 값만 확인했고
  `git diff --check`는 오류 없이 통과했다. 사용자 시각 비교는 아직 필요하다.

## 2026-09-09 후속: 메인 로고 잔상 회귀와 파트너·버전 추가 전진

### 사용자 재현과 변경 전 필수 판단

1. 사용자는 직전 변경 후 기동 시 `TitleLogo2d`에 투명 셀로판 같은 잔상이 나타났지만, 로고를 당긴 결과
   선명도는 더 좋아졌다고 확인했다.
2. 추가 사용자 확인에 따라 메인 로고 로컬 Z `-10`은 선명도 개선값으로 보존한다. 잔상은 현재 변경에서
   카메라, Shader, Importer나 XR 설정을 함께 바꾸지 않고 별도 미검증 현상으로 남긴다.
3. 위치 기준은 씬 작성 `TitleLogo2d`, `PartnerLogos`, `Version` RectTransform이다.
   `TitleSplashController`는 기존 Transform을 덮어쓰지 않고 alpha와 버전 문자열만 갱신한다.
4. 입력 소비자는 없다. 직접 영향은 Title 메인 로고의 잔상과 파트너 로고·버전의 깊이·가독성이다.
   오디오, 페이드 순서, PPE와 텔레메트리는 변경하지 않는다.
5. 런타임 자동 보정은 추가하지 않는다. 잔상을 후속 진단할 때는 새 패치를 추가하기 전에 Z `0` 기준과
   현재 Z `-10`의 선명도·잔상을 같은 조건에서 비교한다.
6. 파트너 로고와 버전은 서로 같은 깊이를 유지하며 Z `-50 → -70`으로 이동한다. 파트너 anchored Y `8.1`,
   크기·앵커·피벗·자식 로고는 보존한다.
7. 정적 씬 diff 뒤 Unity 기동에서 메인 로고의 선명도와 잔상 변화, 파트너·버전 선명도,
   겹침·잘림·시머링과 Quest/OpenXR 양안 깊이를 별도로 확인한다.

### 적용 상태

- 추가 사용자 확인에 따라 `TitleLogo2d`는 선명도가 개선된 Z `-10`을 유지했다. 투명 셀로판 같은 잔상은
  이번 변경에서 다른 렌더링 값을 섞지 않고 후속 비교 대상으로 남겼다.
- `PartnerLogos`와 `Version`의 로컬 Z를 함께 `-50`에서 `-70`으로 변경했다.
- 파트너 로고의 anchored Y `8.1`과 모든 크기·앵커·피벗·자식 값은 보존했다.

## 2026-09-09 후속: 편집 모드 Scene View 로고 표시

### 이번 변경이 대응하는 사용자 요청

- 사용자가 생산 `1_Title`과 `3_Loading`의 메인 이미지를 Scene View에서 직접 보며 비율과 배치를 조정할 수 있게 한다.
- `3_Loading/Canvas/PartnerLogos`는 필요하지 않아 비활성인 것이 정상이며, 활성 상태나 알파를 변경하지 않는다.

### 변경 전 필수 판단

1. **기존 Inspector/씬 작성값 보존:** 메인 로고의 RectTransform, Sprite, 색상과 Loading의 모든 작성값을 보존한다.
   Title 메인 로고의 편집 모드 가시성을 막는 `CanvasGroup.alpha`만 작성값 `1`로 둔다.
2. **단일 기준과 상태 소유자:** 편집 배치의 기준은 각 생산 씬의 `Canvas/TitleLogo2d`이다. 재생 중 페이드 상태는
   `TitleSplashController`가 소유한다.
3. **입력·렌더링 경로:** 입력 소비자는 없다. 표시 경로는 `World Space Canvas → TitleLogo2d CanvasGroup → Image/Sprite`이다.
4. **실패 처리:** 누락 참조 자동 생성이나 편집 모드 자동 보정을 추가하지 않는다. 필수 참조 오류는 기존 런타임 경로에서 드러나게 둔다.
5. **함께 영향받는 소비자:** Title의 편집 모드 로고 표시와 재생 시 페이드만 확인한다. Loading 파트너 로고,
   입력, 오디오, PPE, 텔레메트리는 변경하지 않는다.
6. **변경 전후 비교:** 변경 전 Title은 `TitleLogo2d` 활성 상태지만 `CanvasGroup.alpha = 0`이고,
   `TitleSplashController.Awake()`가 편집 모드에서도 이를 덮어썼다. 변경 후 편집 모드는 alpha `1`을 유지하고,
   Play Mode 진입 시에만 기존처럼 alpha `0`에서 페이드를 시작해야 한다.
7. **검증 수준:** 씬·코드 정적 확인과 C# 컴파일까지 수행한다. Scene View의 실제 표시와 조정 결과는 사용자가 Unity에서 확인하고,
   Play Mode 및 Quest/OpenXR의 페이드·양안 표시는 별도 수동 검증으로 구분한다.

### 적용 및 검증 상태

- `Assets/Scenes/1_Title.unity`의 `Canvas/TitleLogo2d` 작성 alpha를 `1`로 변경했다.
- `TitleSplashController.Awake()`는 Play Mode일 때만 페이드용 alpha `0` 초기화를 수행하므로 편집 모드 작성값을 덮어쓰지 않는다.
- `Assets/Scenes/3_Loading.unity`는 변경하지 않았다. 메인 `TitleLogo2d`는 기존대로 활성·alpha `1`,
  `PartnerLogos`는 기존대로 비활성·alpha `0`이다.
- `SceneDependencyValidationHarness`에 위 네 계약의 정적 재발 검사를 추가했다.
- `dotnet build Assembly-CSharp.csproj`은 기존 경고 68개와 오류 0개로 통과했고,
  `dotnet build Assembly-CSharp-Editor.csproj`은 경고·오류 0개로 통과했다.
- 실행 중인 Unity는 생산 `1_Title`을 열고 있고 Asset Pipeline Refresh를 완료했다. 자동 UI 연결이 Unity 창을 제공하지 않아
  Scene View 화면 자체의 가시성은 사용자가 확인해야 한다. Play Mode와 Quest/OpenXR 검증은 아직 수행하지 않았다.

## 2026-09-09 후속: 파트너 로고와 버전 Scene View 동시 표시

### 변경 전 필수 판단

1. `PartnerLogos`와 `Version`의 기존 RectTransform·자식·색·글자 크기·깊이 작성값을 보존하고 CanvasGroup alpha만 `1`로 둔다.
2. 편집 배치 기준은 씬의 `Canvas/PartnerLogos`와 `Canvas/Version`, 재생 페이드 소유자는 `TitleSplashController`다.
3. 입력 소비자는 없고 표시 경로는 `World Space Canvas → 각 CanvasGroup → Image/TMP`다.
4. 편집 모드 자동 보정이나 누락 참조 fallback을 추가하지 않는다.
5. Title의 편집 가시성과 기존 재생 페이드만 영향받으며 Loading, 입력, 오디오, PPE와 텔레메트리는 보존한다.
6. 변경 전 두 CanvasGroup은 alpha `0`이라 Scene View에서 보이지 않는다. 변경 후 작성 alpha `1`, Play Mode 진입 시 기존처럼 `0`부터 페이드한다.
7. 씬 diff와 컴파일을 정적으로 확인하고, Scene View 배치와 Play Mode 표시는 Unity에서 이어서 비교한다. Quest/OpenXR 검증은 별도다.

### 2026-09-09 작업 종료 시점 정리

- 생산 `1_Title`의 최종 작성값은 메인 `TitleLogo2d` 로컬 Z `-10`, `PartnerLogos` 로컬 Z `-70`·anchored position `(1.9, 7.8)`, `Version` 로컬 Z `-70`·anchored position `(-11.5, -14.9)`·TMP font size `1`이다.
- `TitleLogo2d`, `PartnerLogos`, `Version`의 CanvasGroup 작성 alpha를 `1`로 두어 Scene View에서 함께 편집할 수 있게 했다.
- `TitleSplashController.Awake()`는 편집 모드에서 즉시 반환하므로 Scene View 작성 alpha를 덮어쓰지 않는다. Play Mode에서는 기존 페이드 흐름을 유지한다.
- 사용자는 메인 로고와 파트너 로고를 카메라 쪽으로 당긴 결과 선명도가 개선됐다고 확인했다. 기동 시 메인 로고의 투명 셀로판 같은 잔상은 별도 Quest/OpenXR 비교가 필요한 미해결 항목이다.
- `Version`의 TMP font size를 씬에서 `1`로 조정했지만 현재 `TitleSplashController.EnsureVersionLabel()`이 Play Mode에서 `versionFontSize=3`과 폰트·색·정렬을 다시 대입한다. 따라서 버전의 Inspector 작성값이 재생에서 그대로 유지된다고 확인하지 않았으며, 런타임 표현 덮어쓰기를 제거하는 후속 수정이 필요하다.
- 정적 씬·코드와 Unity Scene View까지 확인했다. 타이틀 페이드, 잔상, 파트너 로고·버전의 깊이와 가독성은 Quest/OpenXR 양안에서 별도로 검증해야 한다.
