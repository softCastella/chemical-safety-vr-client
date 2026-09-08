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

## 2026-08-30 PPE 룸 원거리 아지랑이 및 가이드 축소 후속

### 변경 전 필수 질문

1. 기존 Inspector/씬 작성값은 사용자가 조정한 PPE 오브젝트 Transform을 포함해 보존한다. 이번 씬 변경은
   `ControllerGuide/Context/1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick`의 작성 RectTransform만
   대상으로 한다.
2. 원거리 렌더 해상도의 단일 기준은 Android 기본 `Mobile` 품질이 참조하는
   `Assets/Settings/Mobile_RPAsset.asset`이고, 가이드 크기의 단일 기준은 `_loco` 씬의 세 RectTransform이다.
3. 입력 경로는 `PPEVoiceFlowDirector`가 단계별 가이드 활성 상태만 전환하고,
   `ControllerGuideMiniActivator`가 별도 미니 가이드만 토글한다. 입력 Action, Raycaster, Layer, Collider는
   이번 변경에서 수정하지 않는다.
4. 실패 시 런타임 자동 수리나 크기 보정 fallback을 추가하지 않는다. 필수 설정은 Editor 하네스가 명확히
   실패하도록 한다.
5. 영향 소비자는 Android/Quest의 PPE 룸 장면 렌더 해상도와 메인 컨트롤러 가이드 세 이미지다. 미니 가이드,
   음성, 카드·모달·텔레포트, PPE Grab, 거울과 XR 양안 셰이더 경로는 보존한다.
6. 변경 전 기준은 Mobile Render Scale `0.8`, MSAA 4x와 정사각형 `100×100` 가이드 Rect다. 변경 후에는
   같은 PPE 룸 위치에서 원거리 가구 윤곽, 머리 이동 시 시간축 떨림, 앱 FPS/GPU frame time을 비교하고,
   메인 가이드의 세 이미지를 같은 거리에서 읽는다.
7. 정적 설정과 C# 컴파일은 이 작업에서 확인한다. Unity Editor Play Mode와 Quest/OpenXR 양안 화질·성능은
   실제 실행 증거가 있을 때만 완료로 기록한다.

### 근본 원인 판단

- Mobile Render Scale `0.8`은 MSAA 4x로 경계 샘플링을 보완해도 원거리 가구와 오브젝트의 실제 렌더 픽셀 수를
  늘리지 않는다. 멀수록 윤곽이 더 심하게 일렁인다는 재현은 해상도 부족에 따른 시간축 앨리어싱과 일치한다.
- 2026-08-28에 컨트롤러 PNG 세 개의 `Preserve Aspect`를 켰지만 기존 정사각형 `100×100` Rect를 유지했다.
  원본은 `1672×941`(약 16:9)이므로 표시 높이가 이전 정사각형 채움 대비 약 56%로 줄어 체감 크기가 작아졌다.
- 런타임 코드는 이 RectTransform의 위치·크기·스케일을 덮어쓰지 않고 활성 상태만 변경한다.

### 적용한 변경

- `Mobile_RPAsset`의 Render Scale을 `0.8`에서 `0.9`로 올렸다. MSAA 4x와 다른 렌더 기능은 유지했다.
- 메인 컨트롤러 가이드 세 이미지의 Rect를 원본 비율과 일치하는 `125×70.34928`로 작성하고 X 위치를
  `-200`으로 조정했다. 기존보다 가로·세로 표시 크기를 25% 키우면서 오른쪽 단계 안내와의 간격을 유지한다.
- PNG, Texture Importer의 mipmap/Trilinear/aniso 8, `Preserve Aspect`, 미니 가이드 `0.0008` 스케일은
  변경하지 않았다.
- 회귀 하네스에 Mobile Render Scale/MSAA와 세 가이드 Rect의 원본 비율·최소 작성 폭 검사를 추가했다.

### 영향 범위와 아직 필요한 수동 검증

- `0.8 → 0.9`는 눈당 렌더 픽셀 수를 약 26.6% 늘리므로 Quest GPU 부하도 함께 증가할 수 있다.
- 정적 YAML/렌더 설정 검사 `PPE_LOCO_RENDER_GUIDE_STATIC_VALIDATION_PASS`, Runtime 및 Editor 보조
  빌드 오류 0개를 확인했다. Unity 에셋 파이프라인 새로고침 이후 최근 로그에서도 C#·씬·Shader 오류는
  검출되지 않았다.
- 현재 Unity에 열린 씬은 `0_App`이므로 `Tools > PPE > Validate Locomotion PPE Regressions` 실행과
  `_loco` 씬의 실제 가이드 배치 확인은 완료로 기록하지 않는다.
- Unity Editor에서 `_loco` 씬을 열어 세 가이드가 겹치거나 시야 밖으로 나가지 않는지 확인한다.
- Quest/OpenXR 양안에서 같은 위치와 같은 머리 움직임으로 원거리 가구·오브젝트의 아지랑이 감소를 비교하고,
  72Hz에서 앱 FPS 72와 GPU frame time 13.89ms 이내 유지 여부를 기록한다.
- 프레임 타임이 기준을 넘으면 Render Scale을 추가로 올리지 않는다. 장면 전체가 물리적으로 흔들리면 해상도와
  별도로 재투영·Quest Link 전송 상태를 분리 조사한다.

## 2026-08-30 가이드 과대 폭 회귀 후속

### 사용자 재현

- `125×70.34928`로 확대한 컨트롤러 가이드가 실제 실행 화면에서 너무 넓어 시야를 과도하게 차지했다.
- 세 가이드 RectTransform에는 기존 작성값 `localScale=(5,5,5)`가 있으므로, 폭 125는 패널 기준 유효
  폭 625가 된다. 기존 폭 100의 유효 폭 500을 단순 Rect 값만 보고 25% 확대한 것이 근본 원인이다.

### 변경 전 필수 질문

1. 기존 Inspector/씬 값 중 세 컨트롤러 이미지의 `sizeDelta`만 조정하고 부모 Canvas, Context, 위치,
   `localScale`, 색과 활성 상태는 보존한다.
2. 단일 기준은 `_loco` 씬의 `ControllerGuide/Context` 아래 세 RectTransform이며 런타임 상태 소유자는
   `PPEVoiceFlowDirector`다.
3. 런타임 경로는 음성 단계가 `ApplyControllerGuideVisual`을 거쳐 대상 `GameObject.SetActive`만 바꾼다.
   입력 Action, Interactor, Raycaster, Layer와 Collider는 변경하지 않는다.
4. 런타임 자동 크기 보정은 추가하지 않고 씬 작성값과 Editor 하네스로만 수정·검출한다.
5. 영향 소비자는 상세·간단 컨트롤러 안내의 세 이미지다. 미니 가이드, 음성 순서, PPE 흐름, 렌더 스케일,
   텔레포트와 XR 양안 셰이더는 보존한다.
6. 변경 전 기준은 사용자 캡처의 폭 125·유효 폭 625다. 변경 후에는 폭 110·유효 폭 550에서 같은
   카메라 위치로 화면 점유율과 텍스트 가독성을 비교한다.
7. 씬 YAML·런타임 할당 경로·하네스와 C# 컴파일을 정적으로 확인하고 Unity Game View와 Quest 양안
   결과는 실제 실행 증거와 구분한다.

이번 변경은 사용자가 지적한 가이드 과대 폭만 줄인다. 기존보다 작다고 지적됐던 폭 100으로 완전히
되돌리지 않고, 원본 16:9 비율을 유지한 `110×61.907894`를 사용한다. 하네스는 Rect 폭뿐 아니라
`sizeDelta.x × localScale.x` 유효 폭의 하한과 상한을 함께 검사해 같은 과대 확대를 막는다.

### 적용 및 검증 결과

- `1_Ctrl_Trigger`, `2_Ctrl_Grip`, `3_Ctrl_Joystick` 세 RectTransform을 모두
  `110×61.907894`로 변경했다. `anchoredPosition=(-200,-61)`과 `localScale=(5,5,5)`는 보존했다.
- 유효 작성 폭은 `625→550`으로 감소했고 원본 `1672×941`과 Rect의 종횡비 차이는
  `0.00000003` 미만이다.
- 회귀 하네스에 Rect 폭 `100~110`, 유효 작성 폭 `500~550` 범위를 추가했다. 이 범위를 벗어나면
  작아서 읽기 어렵거나 패널을 과도하게 차지하는 것으로 실패한다.
- 정적 씬 검사에서 새 크기 3개, 이전 `125×70.34928` 0개를 확인했고 `git diff --check`를 통과했다.
- `Assembly-CSharp-Editor.csproj --no-restore` 빌드는 기존 경고 34개, 오류 0개로 통과했다.
- 서버 저장소와 실행 중인 서버 테스트는 변경하거나 제어하지 않았다.
- Unity Game View와 Quest/OpenXR 화면 점유율은 아직 미검증이다. 같은 위치에서 다시 실행해 폭과
  가독성을 확인해야 한다.

## 2026-08-30 컨트롤러 가이드 원본 선·글자 보정 적용

### 사용자 요청과 보존 범위

- 사용자가 지목한 시간축 번쩍임을 줄이기 위해 컨트롤러 도해의 검정 윤곽·연결선, 이미지에 포함된
  한글 획과 주황색 버튼 외곽을 약 15%만 굵게 보정했다.
- 기존 `1672×941` 캔버스, 흰 배경, 좌우 컨트롤러 배치, 화살표 방향, 버튼 위치와
  `조이스틱`·`트리거 버튼`·`그립 버튼` 문구를 보존했다. 새 문구·장식·이중선·물결선은 만들지 않았다.
- 씬의 최종 `110×61.907894`, `anchoredPosition=(-200,-61)`, `localScale=(5,5,5)` 작성값과
  Texture Importer·`.meta`는 이번 이미지 교체에서 변경하지 않았다.

### 적용한 변경

- 다음 세 기준 이미지를 같은 경로에서 교체했다.
  - `Assets/UIs/Guide/Controller_tri.png`
  - `Assets/UIs/Guide/Controller_gri.png`
  - `Assets/UIs/Guide/Controller_joy.png`
- 이미지 편집 방법은 기존 PNG를 기준 이미지로 사용한 `ImageGen` 정밀 편집이다. 최종 지시의 핵심은
  “구성과 정확한 한글·버튼 위치를 유지하고, 검정 윤곽·연결선·한글 획·주황 외곽만 약 15% 굵게 하며,
  재디자인·추가 텍스트·과장된 굵기·이중/물결선을 만들지 말 것”이었다.
- 세 결과물은 모두 `1672×941`이며 `.meta` GUID는 보존했다.

### 검증과 남은 항목

- 세 PNG의 해상도, 한글 문구, 컨트롤러 좌우 배치와 강조 버튼 종류를 시각 확인했다.
- 파일 경로와 씬 참조가 유지되므로 별도의 런타임 이미지 선택 코드나 UI 크기 덮어쓰기는 추가하지 않았다.
- 정적 확인만으로 실제 Quest의 시간축 번쩍임 감소를 확정하지 않는다. Unity Game View와 Quest 양안에서
  정면·사선·머리 이동 조건으로 선이 뭉개지지 않는지, 기존보다 반짝임이 줄었는지 확인해야 한다.

## 2026-08-31 미니 컨트롤러 가이드 정사각형 배경·하단 문구 잘림 보정

### 변경 전 필수 질문

1. 기존 Inspector/씬 작성값 중 `ControllerGuide_mini/Context/Controller_Image`의 `100×100`, scale 5와
   기존 sprite GUID를 보존한다. 사용자가 요청한 이미지 Y 위치와 `Context/Use`의 위치·높이만 조정한다.
2. 단일 기준은 `_loco` 씬의 `ControllerGuide_mini/Context` 작성값과
   `Assets/UIs/Guide/controller.png`이다. 활성 상태는 `ControllerGuideMiniActivator`가 소유한다.
3. 입력 경로는 XRI 좌우 `Scale Toggle`과 Simulator `leftStickPress`가 activator의 toggle만 호출한다.
   Interactor, Raycaster, Layer, Collider와 입력 Action은 변경하지 않는다.
4. 런타임 자동 생성·자동 크기 보정은 추가하지 않는다. 잘못된 이미지 비율과 문구 여백은 Editor 회귀
   하네스에서 명확히 실패하게 한다.
5. 영향 소비자는 카메라 고정 미니 컨트롤러 가이드의 이미지와 하단 온/오프 안내 문구뿐이다. 메인 가이드,
   음성, 카드·모달, PPE Grab, 텔레포트와 XR 셰이더는 보존한다.
6. 변경 전 기준은 사용자 캡처의 가로 원본 강제 정사각형 변형과 높이 50 문구의 하단 잘림이다. 변경 후에는
   흰색 정사각형 안에서 원본 비율이 유지되고 문구 상·하단 획이 모두 보이는지 같은 카메라 위치에서 비교한다.
7. PNG 해상도·씬 YAML·런타임 할당 경로와 C# 컴파일을 정적으로 확인한다. Unity Game View와 Quest 양안은
   실제 실행 증거와 구분한다.

### 근본 원인과 적용

- 미니 가이드는 `1672×941` 가로 PNG를 `100×100`, scale 5 Rect에 `Preserve Aspect` 없이 표시해 도해가
  세로로 늘어났다. 별도 런타임 덮어쓰기는 없고 `ControllerGuideMiniActivator`는 활성 상태만 바꾼다.
- `controller.png`를 흰색 `1254×1254` 정사각형 캔버스로 교체하고 기존 가로 도해 전체를 비율 유지한 채
  중앙에 배치했다. 기존 파일 경로와 `.meta` GUID는 유지했으며, 씬에 새 UI 오브젝트를 만들지 않았다.
- 편집 방법은 기존 PNG를 edit target으로 사용한 `ImageGen` 정밀 편집이다. 최종 지시는 기존 컨트롤러,
  연결선, 주황 강조와 `조이스틱`·`트리거 버튼`·`그립 버튼` 문구를 변경하지 않고, 캔버스만 1:1 흰색으로
  확장해 전체 원본을 중앙 배치하는 것이었다.
- 하단 `Use` TMP RectTransform을 `anchoredPosition=(8,-342)`, `700×50`에서
  `anchoredPosition=(8,-330)`, `700×70`으로 바꿨다. font size 30, 문구, 정렬과 색은 보존했고 패널
  아래쪽에 35 단위 여백을 확보했다.
- 후속 캡처에서 이미지가 낮게 보인다는 요청에 따라 `Controller_Image`의 Y를 `-31`에서 `-10`으로 올렸다.
  `100×100`, scale 5, 앵커·피벗과 sprite 참조는 유지했다.
- `PPELocomotionPpeRegressionValidationHarness`에 미니 sprite 경로, 정사각형 source, `100×100` 이미지
  Rect와 하단 문구의 최소 높이·하단 여백 검사를 추가했다.

### 남은 수동 검증

- Unity Game View에서 조이스틱 클릭으로 미니 가이드를 열고 컨트롤러가 찌그러지지 않는지, 흰색 정사각형
  여백이 의도대로 보이는지, 하단 문구의 모든 획이 표시되는지 확인한다.
- Quest/OpenXR 양안에서 미니 가이드의 체감 크기와 문구 가독성은 아직 미검증이다. 가로 도해를 정사각형에
  맞추면서 실제 도해 높이가 줄어드는 점도 헤드셋에서 확인해야 한다.

## 2026-08-31 메인 컨트롤러 가이드 100×100 흰색 배경 후속

### 변경 전 필수 질문

1. 기존 씬 작성값 중 세 메인 가이드의 앵커·피벗·scale 5와 sprite GUID를 보존하고, 사용자가 요청한
   `100×100` 크기와 정사각형 배경에 필요한 X 위치만 변경한다.
2. 단일 기준은 `_loco` 씬의 `ControllerGuide/Context`와 `Controller_tri.png`, `Controller_gri.png`,
   `Controller_joy.png` 세 파일이다. 활성 상태는 기존 `PPEVoiceFlowDirector`가 소유한다.
3. 입력·음성·단계 전환 경로는 변경하지 않고 세 Image의 작성값과 이미지 자산만 변경한다.
4. 런타임 자동 크기 보정이나 별도 흰색 UI 생성은 추가하지 않는다. 흰 배경은 이미지 자체에 포함한다.
5. 영향 소비자는 메인 트리거·그립·조이스틱 가이드다. 미니 가이드, 카드·모달 입력, 텔레포트와 PPE Grab은
   보존한다.
6. 변경 전 기준은 가로형 `110×61.907894`, X `-200`이다. 변경 후 기준은 정사각형 `100×100`, X `-166`이다.
7. PNG 해상도·씬 YAML·런타임 할당 경로와 C# 컴파일을 확인한다. Game View와 Quest 양안은 수동 검증한다.

### 적용과 원인

- 이전 가로형 PNG는 Rect 자체가 가로형이라 사용자가 요청한 `100×100` 흰색 판처럼 보이지 않았다.
- 세 PNG를 각각 `1254×1254` 순백색 캔버스로 편집하고 기존 도해·한글·주황색 강조를 중앙에 보존했다.
  파일 경로와 `.meta` GUID는 유지했다.
- ImageGen 정밀 편집 모드에서 “기존 도해·정확한 한글·강조 버튼은 변경하지 않고 1:1 순백색 캔버스만
  확장하며 재디자인·추가 텍스트·왜곡을 금지”하도록 지시한 결과를 적용했다.
- 씬의 세 RectTransform을 `100×100`, `anchoredPosition=(-166,-61)`로 맞췄다. 런타임 코드에는 UI 크기나
  위치를 덮어쓰는 경로가 없고 기존 활성 전환만 유지한다.
- 회귀 하네스는 세 원본의 정사각형 여부, Rect `100×100`, X `-166`, scale을 반영한 유효 폭 500을 검사한다.

## 2026-08-31 PPE_Poster_1 작은 글자 화질 및 모달 호버색 후속

### 변경 전 필수 질문

1. 포스터 원본 이미지·머티리얼·씬 Transform과 모달의 레이아웃·문구·클릭 이벤트를 보존한다.
2. 포스터 화질 기준은 `PPE_Poster_1.png.meta`, 모달 색 기준은 `_loco` 씬의 직렬화된 Button ColorBlock이다.
3. 모달 입력 경로는 XRI Ray → UI Raycaster → Button이며, 이번 변경은 `Highlighted Color`만 수정한다.
4. 런타임 자동 재생성·색 덮어쓰기를 추가하지 않는다. 포스터 글자가 바뀌는 생성 이미지는 적용하지 않는다.
5. 영향 소비자는 `PPE_Poster_1`을 사용하는 월드 포스터와 현재·레거시 모달 버튼의 호버 표시다.
6. 변경 전 포스터는 원본 `2381×3402`를 최대 2048, NPOT 리사이즈, Bilinear/aniso 1, 품질 50으로 가져왔다.
   변경 후 같은 거리에서 작은 글자 가독성을 비교한다. 호버는 노란색과 중립 회색을 비교한다.
7. 임포터·씬 YAML·C# 빌드는 정적으로 확인하고 Unity Game View와 Quest/OpenXR 양안은 수동 확인한다.

### 근본 원인과 적용

- 포스터 원본은 `2381×3402`로 작은 글자도 존재하지만, Unity가 최대 2048로 축소하고 NPOT 리사이즈와
  중간 압축 품질을 적용해 작은 획부터 뭉개질 조건이었다.
- 원본 PNG는 교체하지 않았다. 비교용 ImageGen 보정본은 일부 작은 한글이 변형·축약되고 출력 해상도도
  원본보다 낮아 채택하지 않았다. 사용한 정밀 편집 지시는 전체 구성·한글·숫자·로고·QR·색·위치를 정확히
  보존하면서 얇은 획과 압축 흔적만 완화하라는 것이었다.
- 임포터를 최대 4096, NPOT 리사이즈 없음, Trilinear, aniso 8, Clamp, 압축 품질 100으로 변경했다.
  따라서 원본 `2381×3402`를 축소하지 않고 mipmap을 유지한다.
- 현재 `PPEVoiceFlowDirector.m_ScenarioDetailModal`이 참조하는 12개 버튼과 동일 계열 레거시 모달 버튼의
  `Highlighted Color`를 노란색 `(1,0.96,0.86,1)`에서 중립 회색 `(0.82,0.84,0.86,1)`으로 바꿨다.
  Normal·Pressed·Selected·Disabled 색, 버튼 문구·배치·이벤트는 보존했다.
- `ScenarioDetailModal` 런타임 코드는 Button ColorBlock을 덮어쓰지 않으며, 기존 Builder도 현재 `_loco`
  씬을 반복 수정하지 않는다. 회귀 하네스는 실제 디렉터가 참조하는 12개 버튼의 회색 호버와 포스터 임포터를
  함께 검사한다.

### 완료한 검증과 남은 수동 검증

- 정적 확인: 포스터 원본 `2381×3402`, 메인·미니 가이드 네 장 `1254×1254`, 모달 노란 호버 잔존 0,
  회색 호버 17개, `git diff --check` 통과.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드는 기존 경고만 있고 오류 0개로 통과했다.
- Unity Editor가 `_loco` 씬을 연 상태이므로 별도 배치 Unity를 실행하지 않았다. 에디터에서 외부 변경을
  다시 불러온 뒤 `Tools > PPE > Validate Locomotion PPE Regressions`를 실행해야 한다.
- Game View와 Quest/OpenXR에서 포스터 작은 글자, 메인·미니 가이드 배치, 회색 호버의 체감 대비는 아직
  미검증이다. 특히 포스터 품질 상향에 따른 Android 텍스처 메모리 증가와 실제 가독성을 함께 확인한다.

## 2026-08-31 메인 가이드 교체 시 도해 크기 출렁임 및 미니 A 안내 후속

### 변경 전 필수 질문

1. 기존 Inspector/씬 작성값 중 메인 가이드 세 Image의 `100×100`, scale 5, 위치, sprite 참조와
   미니 가이드의 패널·하단 조이스틱 안내를 보존한다.
2. 메인 가이드 크기의 단일 기준은 세 PNG 안에 들어 있는 실제 도해의 경계와 중심이며, 표시 전환 상태는
   기존 `PPEVoiceFlowDirector`가 소유한다. 미니 상세 교육 안내의 기준은
   `ControllerGuide_mini/Context/Controller Education Hint`이다.
3. 입력 경로와 XRI 소비자는 변경하지 않는다. `PPEVoiceFlowDirector`는 단계 변경 시 대상 오브젝트의
   활성 상태만 바꾸며 RectTransform·폰트·정렬을 덮어쓰지 않는다.
4. 런타임 자동 크기 보정이나 UI 자동 생성은 추가하지 않는다. PNG 정규화와 씬 작성값으로만 수정한다.
5. 영향 소비자는 메인 트리거·그립·조이스틱 도해와 미니 가이드의 상세 교육 안내다. 음성 순서, 카드·모달,
   텔레포트, PPE Grab과 조이스틱 클릭 토글은 보존한다.
6. 변경 전에는 세 PNG가 모두 `1254×1254`였지만 실제 도해 경계가 각각 약 `1074×644`, `1054×626`,
   `1098×652`로 달랐다. 변경 후에는 폭·높이와 중심 차이를 3픽셀 이내로 맞춘다.
7. PNG 픽셀 경계·씬 YAML·Editor 하네스·C# 컴파일을 정적으로 확인한다. 실제 단계 전환 화면과 Quest 양안은
   Unity 수동 검증으로 구분한다.

### 근본 원인과 적용

- 세 메인 Image의 RectTransform은 모두 동일하고 관련 런타임 경로도 `SetActive`만 수행했다. 출렁임의
  근본 원인은 세 정사각형 PNG가 별도로 편집되면서 캔버스 안 실제 컨트롤러 도해의 폭·높이·세로 중심이
  서로 달라진 것이었다.
- 별도로 생성한 보정 후보는 기준 도해의 선과 강조 위치까지 다시 그려져 채택하지 않았다. 기존 세 PNG의
  한글·선·주황 윤곽·초록 강조를 그대로 사용하고, 실제 도해만 공통 경계 약 `1074×642`와 같은 중심으로
  정규화했다. 거의 흰색인 배경 픽셀은 순백색으로 정리해 재배치 경계가 남지 않게 했다.
- 화면의 원형 A 그림은 PNG 안의 요소가 아니라 씬에 별도로 작성된 `A Button Visual`이었다. 이 자식을
  비활성화하고 기존 `Controller Education Label`을 `A버튼 : 컨트롤러 상세 교육`으로 변경했다.
- 상세 교육 라벨은 하단 조이스틱 클릭 안내와 같은 font size 30, `500×70` 텍스트 영역, 왼쪽 정렬로
  작성했다. 라벨의 왼쪽 모서리는 제목 `컨트롤러 가이드`의 Rect 왼쪽 모서리와 같은 X `-150`에 맞췄다.
  입력 컴포넌트와 raycast 대상은 추가하지 않았다.
- `PPELocomotionPpeRegressionValidationHarness`에 세 PNG 실제 도해 경계의 폭·높이·중심 차이 3픽셀 이내
  검사와 A 그림 비활성, 정확한 문구, 하단 안내와 동일한 글자 크기, 왼쪽 정렬 검사를 추가했다.

### 완료한 검증과 남은 수동 검증

- 정적 픽셀 검사에서 세 도해 경계는 각각 `1074×642`, `1074×644`, `1072×642`이고 중심 차이는 1픽셀로
  정규화 기준 안에 들어온다. 세 파일 경로와 `.meta` GUID는 유지했다.
- 씬에는 A 그림 비활성, `A버튼 : 컨트롤러 상세 교육`, font size 30, 왼쪽 정렬이 직렬화되어 있으며
  런타임 UI 덮어쓰기 경로는 발견되지 않았다.
- Unity Editor가 `_loco` 씬을 연 상태이므로 별도 배치 Unity는 실행하지 않는다. 외부 변경을 다시 불러온 뒤
  `Tools > PPE > Validate Locomotion PPE Regressions`를 실행하고 트리거→그립→조이스틱 전환 시 도해 크기가
  고정되는지, 두 안내 문구가 겹치거나 잘리지 않는지 확인해야 한다.
- Quest/OpenXR 양안의 실제 가독성과 도해 전환 안정성은 아직 미검증이다.

### 모달 느린 누름 해제 시 노란 선택색 후속

- 느리게 눌렀다 떼면 EventSystem의 선택 상태가 잠깐 유지되며 Button의 `Selected Color`가 표시된다.
  이전 보정은 `Highlighted Color`만 회색으로 바꾸고 `Selected Color=(1,0.96,0.86,1)`를 남겨 노란색이
  계속 나타났다. `ScenarioDetailModal` 런타임은 Button ColorBlock을 덮어쓰지 않으므로 씬 작성값이 원인이었다.
- 현재 모달과 같은 계열 레거시 모달의 17개 버튼은 기본·호버·누름을 서로 구분하되 모두 중립 회색 계열을
  사용한다. `Normal=(1,1,1,1)`, `Highlighted=(0.84,0.84,0.84,1)`,
  `Pressed=(0.62,0.62,0.62,1)`이며, 느리게 뗀 뒤 남는 선택 상태는
  `Selected=(1,1,1,1)`로 기본색에 복귀한다. Target Graphic의 작성색 `(0.43,0.46,0.48,1)`과 곱해져
  기본 회색, 호버의 어두운 회색, 클릭의 더 진한 회색으로 표시된다.
- 관련 런타임 색 덮어쓰기 코드는 없었다. `ScenarioDetailModalSceneBuilder`가 새 버튼을 만들 때도 같은 초기
  ColorBlock을 작성하도록 변경했으며, 기존 씬 값을 반복 덮어쓰지는 않는다.
- 회귀 하네스는 현재 디렉터가 참조하는 모달 버튼의 Normal·Highlighted·Pressed·Selected 네 상태를 모두
  검사한다. 정적 씬 검사에서 노란 Selected 색 잔존 0개, 새 상태색 17개를 확인했고 Editor C# 빌드는
  기존 경고만 있고 오류 0개로 통과했다.
- Unity에서 외부 씬 변경을 Reload한 뒤 Game View와 Quest에서 Ray를 버튼 위에 둔 채 천천히
  Trigger Down→Up 했을 때 기본 회색으로 복귀하는지는 수동 확인이 필요하다.

## 2026-09-08 Quest 월드 공간 UI 선명도 1.0 단일 변수 비교

### 변경 전 기준과 보존 범위

1. 이번 변경은 Quest 독립 실행에서 글자와 그림이 함께 뭉개져 보인다는 사용자 보고에만 대응한다.
2. 선명도의 단일 비교 변수는 `Assets/Settings/Mobile_RPAsset.asset`의 `m_RenderScale`이다.
3. 72 Hz 요청, 4x MSAA, 씬·UI 작성값, 텍스처 임포터, TMP 폰트, 입력, 텔레포트, PPE, 거울과 텔레메트리
   동작은 변경하지 않는다.
4. 런타임 자동 화질 보정이나 동적 해상도, FSR, 포비에이션 설정을 추가하지 않는다.
5. 변경 전 code 5 Development APK의 실기기 로그에서 72 Hz 디스플레이 모드와 눈당 `1296×1426`
   swapchain을 확인했다. 이는 Mobile URP Render Scale `0.9` 기준이다.
6. 변경 후에는 같은 Quest 2와 같은 PPE Room 위치에서 글자·그림 선명도, 프레임 안정성, 양안과 주변 시야를
   비교한다.
7. 정적 설정과 하네스 동기화 후 새 Development APK를 빌드·설치했고, Quest 2 OpenXR 로그에서 눈당
   `1440×1584` swapchain과 72 Hz 디스플레이 모드를 확인했다. 사용자 체감 선명도와 장시간 안정성 비교는
   수동 검증으로 남긴다.

### 적용 변경과 예상 영향

- `Mobile_RPAsset.asset`의 Render Scale을 `0.9`에서 `1.0`으로 올렸다. 예상 swapchain은 눈당 약
  `1440×1584`이며, 변경 전보다 처리 픽셀이 약 23% 증가한다.
- `PPELocomotionPpeRegressionValidationHarness`의 고정 기준을 1.0으로 동기화했다. 4x MSAA와 Android Mobile
  quality 연결 검사는 그대로 유지한다.
- 이번 단계에서는 실기기 렌더 해상도 반영까지만 확인했으며 선명도 개선이나 시야 깨짐 미재현을 완료로
  판정하지 않는다. 같은 위치의 사용자 체감 비교가 통과한 뒤에만 1.0을 유지할지 결정한다.
