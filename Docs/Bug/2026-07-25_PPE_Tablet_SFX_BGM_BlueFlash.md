# 2026-07-25 PPE 태블릿 SFX 음량 및 하늘색 번쩍임

## 개요

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 대상 오브젝트 | `Tablet (1)/GeneratedPlane/Signature_Test_Sequence` |
| 상태 | 미해결 — BGM 덕킹과 태블릿 Emission 비활성화만으로 해결되지 않음 |
| 영향 | 체크·서명 SFX 식별이 어렵고 Play Mode 진입 시 태블릿이 하늘색으로 번쩍임 |

## 관련 기능

- 작업 확인서 체크 표시 5개 Reveal 애니메이션
- 작업자, 감시인, 확인자 서명 Reveal 애니메이션
- 체크 시작 시 `Assets/Audio/SFX/check.ogg` 재생
- 서명 시작 시 `Assets/Audio/SFX/sign.ogg` 재생
- PPE룸 BGM: `Assets/Resources/Audio/Scenes/3_PPE_Room.asset`

## 증상 1 — SFX가 BGM에 묻힘

- 체크 및 서명 표시와 SFX 재생 타이밍은 동기화돼 있다.
- `check.ogg`의 실제 길이는 약 `1.489초`, `sign.ogg`는 약 `1.724초`다.
- SFX AudioSource Volume은 `1`로 설정돼 있지만 PPE룸 BGM이 상대적으로 크게 들린다.
- `Ducked Bgm Volume`을 `0`으로 설정해도 기대한 수준으로 문제가 해결되지 않는다고 사용자 확인됐다.

### 확인한 설정

`Assets/Resources/Audio/Scenes/3_PPE_Room.asset`:

```text
bgmVolume: 1
bgmFadeInDuration: 0.5
ambience: none
```

`Signature_Test_Sequence` AudioSource:

```text
Volume: 1
Play On Awake: false
Spatial Blend: 사용자 확인 시점 0
```

Spatial Blend가 `0`인 시점에도 SFX가 작게 들렸으므로 현재 증상은 단순 거리 감쇠만으로 설명되지 않는다.

### 적용해 본 조치

1. 체크·서명 단계별 AudioClip과 AudioSource 연결
2. 각 애니메이션 Duration을 실제 AudioClip 길이에 동기화
3. 태블릿 SFX AudioSource의 Min Distance와 Max Distance 확대
4. 애니메이션 재생 중 BGM을 낮추고 종료 후 복구하는 덕킹 추가
5. 수동 BGM 볼륨 변경 시 기존 BGM Fade In 코루틴을 중단하도록 수정

### 현재 판정

- 위 조치만으로 체감 음량 문제가 완전히 해결되지 않았다.
- 다음 조사에서는 Play Mode의 실제 출력 소스를 동시에 계측해야 한다.
- `AudioManager/BGM` 외의 AudioSource, Quest Link 시스템 오디오, 원본 클립의 정규화 수준 및 헤드셋 출력 믹스를 확인해야 한다.

## 증상 2 — Play Mode 진입 시 태블릿이 하늘색으로 번쩍임

- Play Mode에 진입할 때마다 태블릿이 하늘색으로 번쩍이는 현상이 보인다.
- 태블릿 계층에는 별도 XR Interactable 또는 Hover 색상 컴포넌트가 확인되지 않았다.
- 태블릿 본체는 `Tablet_URP.mat`, 문서 화면은 `WorkConfirmPlane_Unlit.mat`을 사용한다.

### 확인된 머티리얼 상태

`Assets/FBX/Tablet/Tablet_URP.mat`에는 다음 발광 설정이 있었다.

```text
Keyword: _EMISSION
Emission Color: white
Emission Map: assigned
```

### 적용해 본 조치

- `_EMISSION` 키워드 비활성화
- Emission Color를 검정으로 변경
- Emission Map 연결 해제
- `EmissiveIsBlack` 플래그 적용

### 현재 판정

- Emission 비활성화만으로 하늘색 번쩍임이 해결되지 않았다고 사용자 확인됐다.
- 따라서 발광 머티리얼 하나만을 원인으로 단정하지 않는다.
- 다음 후보를 추가 조사해야 한다.
  - Play Mode 첫 프레임의 카메라 Clear Color 또는 URP 노출 변화
  - XR/OpenXR 초기 프레임의 디스플레이 전환
  - 다른 태블릿 Renderer 또는 Canvas Image
  - MaterialPropertyBlock의 런타임 초기화
  - 셰이더 Variant 준비 전후의 임시 렌더링
  - 에디터 선택·Gizmo 표시와 실제 Game View 출력의 차이

## 다음 진단 계획

- [ ] 번쩍임이 Game View, Scene View, HMD 중 어디에 나타나는지 분리 확인
- [ ] Play Mode 진입 직전과 첫 프레임 이후 태블릿 Renderer별 머티리얼·색상 캡처
- [ ] `Tablet_URP`, `WorkConfirmPlane_Unlit`, 체크·서명 머티리얼을 순차적으로 비활성화해 원인 Renderer 분리
- [ ] Play Mode에서 모든 활성 AudioSource의 Clip, Volume, Mute, Spatial Blend 및 출력 경로 기록
- [ ] BGM AudioSource를 직접 Mute한 A/B 테스트
- [ ] `AudioListener.volume`, Quest Link Windows 볼륨 및 Meta 앱 출력 장치 확인
- [ ] `check.ogg`, `sign.ogg`, PPE BGM의 실제 피크·RMS 음량 비교
- [ ] 필요하면 Audio Mixer를 추가해 BGM과 SFX를 별도 그룹으로 라우팅하고 SFX 게인·Duck Snapshot 적용

## 관련 파일

- `Assets/Scripts/AudioManager.cs`
- `Assets/Scripts/HandwrittenSignatureSequence.cs`
- `Assets/Editor/PPERoomSignatureTestBuilder.cs`
- `Assets/Editor/PPERoomSignatureDiagnostics.cs`
- `Assets/Shaders/HandwrittenSignatureReveal.shader`
- `Assets/FBX/Tablet/Tablet_URP.mat`
- `Assets/Materials/PPE/Tablet/`
- `Assets/Audio/SFX/check.ogg`
- `Assets/Audio/SFX/sign.ogg`
- `Assets/Resources/Audio/Scenes/3_PPE_Room.asset`
- `Assets/Scenes/3_PPE_Room.unity`
