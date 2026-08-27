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

- Android 기본 품질은 `Mobile`이며 [ProjectSettings/QualitySettings.asset](../../client/ProjectSettings/QualitySettings.asset)의 `Android: 0`으로 연결된다.
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
