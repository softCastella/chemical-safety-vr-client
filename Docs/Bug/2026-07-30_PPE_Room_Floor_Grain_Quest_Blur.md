# PPE Room Floor Grain Blur on Quest

Date: 2026-07-30

## Symptom

- In `3_PPE_Room`, the floor grain around `PPE_Room_Floor` can look slightly cloudy or smeared when viewed through a Meta Quest headset.
- The artifact is reported most noticeably near the upper part of the lens/view, and appears localized to the floor grain area rather than the entire scene.

## Current Findings

- `Assets/Materials/PPE/PPE_Room_Floor.mat` uses `PPE_Room_Floor.png` as its base texture.
- The floor material is opaque and is not using a transparent blur shader.
- `PPE_Room_Floor.mat` has `_BaseColor` and `_Color` set to approximately `1.166`, which can make the floor brighter than the authored texture.
- `Assets/PPE Room/PPE_Room_Floor.png` is `1254 x 1254`.
- The texture importer enables mipmaps and uses Aniso Level `4`.
- The Android texture platform override is enabled with texture format value `50` and normal compression quality.
- The Mobile URP asset uses `m_RenderScale: 0.8`.
- The Mobile quality level has project anti-aliasing set to `0`.
- `DefaultVolumeProfile` contains Film Grain, but its intensity is `0`, so Film Grain is not considered the main cause.

## Likely Cause

The likely cause is the combination of a high-frequency floor grain texture, Android texture compression, mipmap sampling at grazing floor angles, relatively low anisotropic filtering, and Quest peripheral lens/rendering characteristics.

The upper lens/view area is more sensitive to this because headset optics and XR rendering are sharpest near the center. With Mobile URP render scale at `0.8`, detailed floor grain is more likely to blend into a cloudy pattern in the peripheral field of view.

## Recommended First Test

Do not reduce render scale. Lower render scale would make the blur more likely.

Prefer a limited floor-only test:

1. Reduce the perceived floor grain contrast or brightness to about half of the current visual strength.
2. Lower `PPE_Room_Floor.mat` `_BaseColor` / `_Color` from `1.166` toward `0.9-1.0`.
3. Increase `PPE_Room_Floor.png` Aniso Level from `4` to `8` or `16`.
4. If the artifact remains, test a higher-quality Android texture compression setting or a less noisy floor texture variant.

## Verification

- Compare Quest headset view before and after the floor-only change from the same standing position.
- Check whether the artifact follows the headset lens area or appears in screen capture:
  - If it is visible only in the headset and strongest near the lens edge, lens/peripheral rendering is contributing.
  - If it is also visible in captured frames, texture sampling/compression/render-scale is the main source.
- Confirm that the floor remains readable on both Meta Quest/Android and PC OpenXR.

## Notes

- The safest first art direction is not to make the whole scene sharper globally, but to reduce the floor grain's visual strength.
- Raising global render scale may improve clarity but has broader Quest performance cost and should be tested separately.

## 2026-08-07 범위 확장: 사선 앨리어싱과 화면 전체 일렁임

### 추가 증상

- `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity`를 XR로 실행했을 때 사선 라인 계열이 계단처럼 깨져 보인다.
- 머리를 조금 움직이면 선과 장면의 세부 윤곽이 반짝이거나 일렁인다.
- 화면 전체가 또렷하지 않고 흐릿하게 느껴진다.

### 정적 조사 결과

- Android 기본 품질은 `Mobile`이며 [ProjectSettings/QualitySettings.asset](../../ProjectSettings/QualitySettings.asset)의 `Android: 0`으로 연결된다.
- `Assets/Settings/Mobile_RPAsset.asset`은 `m_RenderScale: 0.8`, `m_MSAA: 1`이다. URP의 `MsaaQuality.Disabled` 값이 `1`이므로 Android 경로에서는 MSAA가 꺼져 있다.
- XR 메인 카메라의 `UniversalAdditionalCameraData`는 `m_AllowXRRendering: 1`이지만 `m_Antialiasing: 0`이다. 카메라의 `m_AllowMSAA: 1`만으로는 Mobile 파이프라인 자산의 비활성 MSAA를 보완하지 못한다.
- Standalone용 `PC_RPAsset.asset`은 `m_RenderScale: 1`, `m_MSAA: 4`이지만, 메인 카메라의 URP 후처리 안티앨리어싱은 여전히 꺼져 있다. 따라서 Quest Link/Standalone과 Android 빌드의 증상이 같다고 가정하지 않는다.
- 씬에는 `Crack_Highlight_*`, `Crack_Edge_*` 등 활성 `LineRenderer`가 여러 개 있다. 일부 폭 곡선 값은 `0.008982447m`로 매우 얇다. 이 폭은 XR 렌더 해상도에서 한 픽셀 이하 또는 경계에 걸릴 수 있어 머리 움직임에 따라 샘플 위치가 바뀌기 쉽다.
- `LineVisual`이라는 XRI 레이 시각 오브젝트 자체는 현재 씬 YAML에서 비활성 컴포넌트로 확인되지만, PPE 균열용 LineRenderer들은 활성이다. 따라서 “레이가 화면을 흔든다”로 단정하지 않고 균열 라인과 레이 라인을 분리해 확인해야 한다.
- 프로젝트 소유 커스텀 셰이더에는 Single Pass Instanced용 입력·출력 매크로와 stereo 초기화 호출이 정적으로 확인된다. 이 결과만으로 모든 머티리얼의 양안 렌더링을 완료했다고 판단하지 않으며, 실제 Quest 양안 검증은 별도다.
- `Assets/Scripts`와 `Assets/Editor`에서 `renderScale`, `ScalableBufferManager`, `DynamicResolution`, `antiAliasing`을 런타임에 덮어쓰는 경로는 정적으로 찾지 못했다.

### 현재 판단

가장 강한 원인 후보는 다음 조합이다.

1. Android/Quest 경로의 렌더 스케일 `0.8`과 MSAA 비활성
2. XR 카메라의 후처리 안티앨리어싱 비활성
3. 화면 해상도에 비해 너무 얇은 활성 LineRenderer

이 조합은 사선 라인의 계단 현상과 머리 움직임에 따른 서브픽셀 반짝임을 설명한다. `m_UseAdaptivePerformance: 1`도 런타임 해상도 변화를 일으킬 수 있는 보조 후보지만, 현재 로그만으로 실제 해상도 변경이 발생했다고 확정하지 않는다.

균열 라인이 표면과 거의 같은 깊이에 겹쳐 있다면 깊이 정밀도 또는 z-fighting이 추가로 일렁임을 만들 수 있다. 또한 장면 전체가 물리적으로 흔들려 보이는 경우에는 렌더링 샘플 문제와 별도로 HMD 추적·재투영·Quest Link 프레임 타이밍을 분리해야 한다.

### 권장 단일 변수 검증 순서

1. 현재 설정을 기준으로 동일한 위치와 동일한 머리 움직임을 녹화한다.
2. Android/Quest에서 Adaptive Performance만 임시로 끄고 비교한다.
3. Mobile URP Render Scale만 `1.0`으로 올려 비교한다.
4. Mobile MSAA를 `2x`부터 적용해 비교한다. 성능 여유가 확인될 때만 `4x`를 별도 시험한다.
5. 마지막으로 활성 균열 LineRenderer를 임시 비활성화하거나 선 폭을 Inspector에서 키워, 전체 장면 일렁임과 선 자체의 반짝임을 분리한다.
6. 각 단계는 Game View가 아니라 Quest/OpenXR 양쪽 눈에서 확인하고 GPU 프레임 시간과 함께 기록한다.

### 검증 상태

- 정적 확인: 완료. 대상 씬, 품질 매핑, URP 자산, XR 카메라, 활성 LineRenderer, 런타임 렌더 설정 쓰기 경로를 확인했다.
- Unity Editor 확인: 설정 변경 후의 Play Mode 비교는 아직 하지 않았다.
- Quest/OpenXR 확인: 양안 선명도, Adaptive Performance 실제 동작, 재투영 및 프레임 타이밍은 아직 확인하지 않았다.
- 이번 진단에서는 렌더 설정과 씬을 수정하지 않았다. 원인 분리를 위해 위 검증 순서대로 한 번에 한 항목만 변경한다.

## 2026-08-25 갱신: Android MSAA 적용과 바닥 원인 제외

### 적용한 변경

- Android 기본 `Mobile` 품질의 `antiAliasing`을 `0`에서 `4`로 변경했다.
- `Assets/Settings/Mobile_RPAsset.asset`의 `m_MSAA`를 `1`에서 `4`로 변경했다.
- PC 품질과 `PC_RPAsset`의 기존 MSAA 4x는 유지했다.
- XR 카메라는 기존 `allowMSAA=true`, URP 후처리 Anti-aliasing `None` 상태를 유지했다.
- Mobile Render Scale은 GPU 부하 증가를 피하기 위해 `0.8`로 유지했다.

### 판단 갱신

사용자가 현재 PPE룸 바닥 자체는 정상이라고 확인했다. 따라서 이번 사선 앨리어싱·아지랑이 진단에서는 `PPE_Room_Floor.png`의 Texture Filtering, `PPE_Room_Floor.mat` 밝기와 바닥 무늬를 변경 대상으로 삼지 않는다. 기존 문서의 바닥 Grain 진단은 해당 증상이 다시 재현될 때 사용할 과거 근거로 유지한다.

현재 가장 먼저 확인할 항목은 MSAA 4x 적용 뒤의 Quest 양안 직선·UI 외곽선·가는 `LineRenderer`와 72fps 유지 여부다. 특정 선만 떨리면 화면 해상도보다 얇은 선과 거의 같은 깊이에 겹친 면의 z-fighting을 우선 조사한다. 화면 전체가 머리 움직임에 따라 일렁이면 GPU frame time 저하와 재투영을 별도 원인으로 분리한다.

Android OpenXR의 Automatic Viewport Dynamic Resolution, Foveated Rendering과 Application SpaceWarp는 현재 비활성이다. `Mobile_RPAsset`의 Adaptive Performance 허용은 켜져 있으나 실제 런타임 스케일 변경은 확인되지 않았으므로 보조 후보로만 유지한다.

### 완료한 검증

- Unity에서 `Mobile` 품질과 `Mobile_RPAsset`의 MSAA가 모두 4x로 저장된 것을 확인했다.
- 최종 직렬화 diff가 Mobile 품질 `antiAliasing`과 Mobile URP `m_MSAA` 두 값에 한정됨을 확인했다.
- Mixer Room Preview Scene 진단에서는 Renderer 357개에 누락 Material, 누락 Shader, 미지원 Shader가 없었으나 이는 PPE룸 양안 결과의 대체 증거로 사용하지 않는다.

### 아직 필요한 수동 검증

1. Quest Android 빌드에서 PPE룸의 동일 위치·동일 머리 움직임으로 MSAA 적용 전후를 비교한다.
2. 왼쪽·오른쪽 눈에서 직선, 월드 스페이스 UI, 균열 선과 방 표면이 동일하게 안정적인지 확인한다.
3. 72Hz와 앱 FPS 72 유지, CPU/GPU frame time 13.89ms 이내 여부를 기록한다.
4. 성능 여유가 있을 때만 Render Scale `0.9`를 단일 변수로 비교한다. `1.0`은 `0.8` 대비 렌더 픽셀 수가 약 56% 증가하므로 바로 적용하지 않는다.
5. SMAA/FXAA는 글자 흐림과 추가 비용이 있어 마지막 비교 항목으로 둔다.

현재 상태는 정적·Unity Editor 설정 검증까지이며 Quest 양안 화질과 성능은 아직 정상으로 확정하지 않는다.

## 2026-08-28 로고·컨트롤러 가이드·태블릿 문서 이미지별 진단

### 확인한 원인 후보

- `VrLogo_2d.png`는 세밀한 한글, 얇은 세로선과 Glow가 한 이미지에 구워져 있지만 mipmap이 꺼진
  상태로 약 `96 × 64` 크기의 World Space UI에 축소 표시된다. `Preserve Aspect`도 꺼져 있어 작은
  머리 움직임마다 내부 1픽셀 선의 샘플이 교대할 가능성이 크다.
- `Controller_tri.png`, `Controller_gri.png`, `Controller_joy.png`는 컨트롤러 도해와
  `조이스틱/트리거 버튼/그립 버튼` 글자가 각각 한 PNG에 합쳐진 이미지다. 별도 TMP 텍스트는 없다.
  얇은 사선과 글자가 함께 mipmap off, aniso 1, Android 일반 압축 상태이며, 씬에서는 정사각형
  `100 × 100`, scale 5와 `Preserve Aspect` off로 표시되어 원본 종횡비와 맞지 않는다.
- `work_confirm_tablet_readable.png`는 mipmap on, aniso 16, 비압축 상태라 기본 Import 설정은 이미
  비교적 안전하다. 남은 번쩍임은 실제 화면 픽셀보다 얇은 표 선·글자와 비스듬한 관찰 각도의 영향이
  우선 후보다.
- 플레이어 서명은 획을 두껍게 보정했을 때 형태가 어색해져 현재 `sign_player_rm.png` 한 장으로
  다시 만든 기준본이다. 이 이미지의 획 두께·형태·한 장 구성은 변경하지 않는다. mipmap on,
  aniso 8과 전용 셰이더 depth offset이 이미 적용되어 있으므로 남은 비교 대상은 Android 압축,
  투명 가장자리 처리와 문서 면과의 실제 깊이 간격이다.
- MSAA 4x는 UI 쿼드의 외곽선에는 도움을 주지만 PNG 내부에 그려진 1픽셀 글자·사선의 시간축
  반짝임을 직접 해결하지는 못한다.

### 권장 적용 순서

1. 로고와 컨트롤러 세 이미지에 mipmap, aniso 8~16, Clamp를 적용하고 Android는 비압축 또는 고품질
   ASTC로 한 항목씩 비교한다.
2. 씬/Prefab Inspector에서 `Preserve Aspect`를 켜고 RectTransform을 원본 비율에 맞춘다. 런타임
   코드에서 크기나 비율을 덮어쓰지 않는지 함께 확인한다.
3. 작은 로딩 로고는 세부 문구·Glow를 유지한 원본 축소본 대신 작은 화면용 단순 로고를 별도 사용한다.
   컨트롤러 이미지는 현재처럼 한 장 구성을 유지한다. Import 설정을 바꾼 뒤에도 떨리면 원본의 도해
   선과 포함된 글자 획을 최종 표시에서 최소 2~3픽셀이 되도록 함께 굵혀 다시 내보낸다.
4. 태블릿 원본의 표 선과 작은 글자를 굵히거나 실제 물리 크기/관찰 거리를 조정한다. 서명은 현재의
   한 장 이미지와 획 두께를 그대로 유지하고, `alphaIsTransparency`, 무압축/고품질 압축과 문서
   법선 방향의 미세 간격만 단일 변수로 비교한다.
5. 위 조치 뒤에도 화면 전체가 아지랑이처럼 움직일 때만 GPU frame time, 재투영과 Render Scale을
   별도 원인으로 조사한다.

이번 단계에서는 비교 기준을 잃지 않도록 로고·컨트롤러·태블릿의 Texture Importer, RectTransform,
원본 이미지를 변경하지 않았다. 각 변경은 Game View 정면/사선/이동과 Quest 양안을 같은 위치에서
비교한 뒤 채택한다.

### 2026-08-28 적용

#### 변경 전 필수 질문

1. 기존 Inspector/씬 작성값은 위치·크기·앵커·스케일을 유지하고 각 `Image`의
   `Preserve Aspect`만 켜서 보존한다.
2. 단일 기준은 각 PNG의 `TextureImporter`와 씬/Prefab에 직렬화된 `Image`이다.
3. 입력 경로는 변경하지 않는다. 이번 수정은 렌더링 샘플링만 대상으로 한다.
4. 런타임 자동 수리나 fallback을 추가하지 않는다.
5. 로고, 컨트롤러 가이드, 태블릿 문서와 플레이어 서명 렌더링에만 영향이 있고 음성, PPE Grab,
   텔레포트와 퀴즈 상태는 보존한다.
6. 변경 전 기준은 사용자가 제공한 정면 캡처와 기존 Import 설정이며, 변경 후에는 같은 거리의
   정면/사선/머리 이동을 비교한다.
7. 정적 설정과 Unity Import/컴파일을 먼저 확인하고 Quest/OpenXR 양안 확인은 별도로 구분한다.

#### 적용한 변경

- `VrLogo_2d.png`와 `Controller_tri/gri/joy.png`는 한 장짜리 원본 구성을 유지하고 mipmap,
  Trilinear, aniso 8, Android 고품질 압축을 적용했다.
- 현재 PPE 씬의 컨트롤러 이미지 3개, `6_LoadingScene_0`의 로고와
  `Title_Logo_Canvas.prefab`의 로고는 위치·크기를 바꾸지 않고 `Preserve Aspect`를 켰다.
- `work_confirm_tablet_readable.png`는 기존 mipmap/aniso 16/비압축을 유지하고 Trilinear만 적용했다.
- 플레이어 서명은 현재 `sign_player_rm.png` 한 장과 획을 그대로 유지했다. Unity가 이 NPOT 원본을
  강제로 정사각형 크기로 리사이즈하지 않도록 `nPOTScale: None`으로 바꾸고 Trilinear, Clamp,
  투명 가장자리 처리와 Android 고품질 압축을 적용했다. 서명 굵기·형태·이미지 파일은 변경하지 않았다.
- `PPELocomotionPpeRegressionValidationHarness`에 위 Import 설정, 컨트롤러/로고 종횡비 보존과
  서명 단일 이미지 샘플링 조건 검사를 추가했다.

#### 남은 수동 검증

- 정적 설정 검사 `THIN_IMAGE_STATIC_VALIDATION_PASS`와 `dotnet build --no-restore` 오류 0개를
  확인했다. Unity Editor 메뉴 하네스와 실제 렌더링 비교는 아직 실행 증거가 없어 완료로 확정하지 않는다.
- Game View에서 로고, 컨트롤러 도해, 태블릿 표 선과 서명을 같은 거리의 정면·사선·머리 이동으로
  비교한다.
- Quest/OpenXR 양안에서 컨트롤러 이미지가 찌그러지지 않는지, 서명 획 형태가 기존 기준본과 같은지,
  시간축 반짝임이 줄었는지 확인한다.
