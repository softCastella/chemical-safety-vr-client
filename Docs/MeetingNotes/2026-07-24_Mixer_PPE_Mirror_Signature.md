# 2026-07-24 혼합기동·PPE룸 연출 구현 회의록

## 1. 기본 정보

- 일자: 2026-07-24
- 프로젝트: `3D_UI_Test_home`
- 개발 환경: Unity 6000.4.8f1 / URP 17.4.0 / OpenXR
- 대상 씬:
  - `Assets/Scenes/5_MixerRoom.unity`
  - `Assets/Scenes/3_PPE_Room.unity`
- 관련 커밋: `89dfb3c`
- 종합 상태: PC/Unity 배치 검증 완료, Quest/OpenXR 실기기 검증 필요

## 2. 회의 목적

1. 최신 Git Pull 이후 발생한 Unity 패키지 의존성 오류를 복구한다.
2. 혼합기동 뒤쪽에 배경 이미지와 부분 포그를 이용한 공간 연출을 구현한다.
3. PPE룸의 하얀 판넬과 문 사이에 실시간 평면 거울을 구현한다.
4. PPE룸 태블릿 작업확인서에 작업자와 감독자의 손글씨 서명 연출을 시험 적용한다.

## 3. 안건별 결과

| 안건 | 요구 사항 | 결과 | 상태 |
| --- | --- | --- | --- |
| Git/패키지 | 최신 Pull 내용을 유지하면서 Unity 프로젝트 정상 개방 | Tripo3D 로컬 패키지 경로를 현재 PC 경로로 변경 | 로컬 복구 |
| 혼합기동 | 혼합기 뒤에 얇은 배경 박스, 배경 이미지, 부분 포그 배치 | 배경 박스 1개, 이미지 표면 1개, 포그 레이어 3개 구현 | 완료 |
| PPE룸 거울 | 방과 보호복을 정상 반사하는 대형 거울 구현 | 동적 반사 카메라와 오프축 투영 방식으로 전면 재구축 | 완료 |
| 태블릿 서명 | 작업자·감독자 서명이 직접 쓰이는 연출 | PNG 서명이 왼쪽에서 오른쪽으로 순차 노출 | 완료 |

## 4. Git 패키지 오류

### 확인된 문제

`Packages/manifest.json`이 다른 사용자 PC의 절대 경로를 참조하고 있었다.

```text
file:C:/Users/lanoc/Downloads/Tripo3d_Unity_Bridge-latest/Tripo3d_Unity_Bridge
```

현재 PC에는 해당 경로와 `package.json`이 없어 Unity Package Manager가 프로젝트 의존성을 해석하지 못했다.

### 적용한 조치

다음 두 파일의 Tripo3D 경로를 현재 PC의 설치 위치로 변경했다.

- `Packages/manifest.json`
- `Packages/packages-lock.json`

```text
file:C:/Users/user/Desktop/Tripo3d_Unity_Bridge
```

### 잔여 위험

현재 수정값도 사용자별 절대 경로이므로 다른 개발자 PC나 CI 환경에서는 다시 실패할 수 있다.

완전한 해결을 위해 다음 중 하나로 전환해야 한다.

1. 저장소의 `Packages/` 아래에 임베디드 패키지로 포함
2. 접근 가능한 Git URL 사용
3. 사내 Unity Package Registry 사용

## 5. 혼합기동 배경·부분 포그

### 연출 구조

```text
배경 박스
  └─ 배경 이미지
      └─ 후면 포그
          └─ 중간 하단 포그
              └─ 전면 소프트 포그
                  └─ 혼합기 영역
```

### 생성 오브젝트

- `MixerRoom_Backdrop_Fog_Stage`
- `Backdrop_Thin_Box`
- `Background_Image_Surface`
- `Fog_Back_Wide`
- `Fog_Mid_LowBand`
- `Fog_Front_SoftVeil`

### 구현 결정

- 제공된 `Img/혼합기동 화면.png`를 배경으로 사용한다.
- 단일 화면 효과가 아니라 서로 다른 깊이를 가진 Quad 레이어로 구성한다.
- 포그 레이어마다 위치와 크기를 다르게 해 국소적인 깊이감을 만든다.
- 포그 셰이더는 URP 투명 렌더링과 OpenXR Single Pass Instanced 경로를 지원한다.

### 관련 파일

- `Assets/Editor/MixerRoomAtmosphereBuilder.cs`
- `Assets/Shaders/MixerRoomLocalFog.shader`
- `Assets/Materials/MixerRoom/`
- `Assets/Scenes/5_MixerRoom.unity`

## 6. PPE룸 평면 거울

### 요구 사항

- PPE단스의 하얀 판넬과 문 사이에 거울 배치
- 초기 크기보다 가로 약 1/3, 세로 약 1/4 확대
- 카메라 이동에 맞춰 방 내부가 자연스럽게 반사
- 방 밖, 스카이박스, 파란 벽이 과도하게 보이지 않도록 제한
- 거울 앞 보호복의 앞쪽 시점이 정상적으로 보이도록 구성

### 초기 구현에서 발생한 문제

- 회푸른 단색만 표시됨
- 스카이박스 또는 파란 벽만 표시됨
- 카메라 방향을 반대로 바꿔도 결과 차이가 거의 없음
- 카메라 가시 범위가 길어 방 밖 공간이 포함됨
- 보호복의 앞면이 아니라 뒤쪽 시점이 렌더링됨

### 최종 구현

기존 거울을 제거하고 `PPE_Room_Planar_Mirror`를 처음부터 재구축했다.

- 원본 Game/Scene 카메라 위치를 거울 평면 뒤로 반사
- 거울 표면 네 모서리를 이용해 오프축 투영 프러스텀 계산
- 거울 레이어를 반사 카메라 Culling Mask에서 제외
- 표면 클립 오프셋: `0.04m`
- 반사 깊이: `12m`
- 거울 크기: `1.44 × 3.3125m`
- RenderTexture: `640 × 1472`
- Game View와 Scene View에서 거울이 보일 때만 렌더링

### 검증

- PPE룸 내부와 보호복이 거울에 표시됨
- 거울 표면의 단색·스카이박스 증상 해소
- 하얀 판넬과 문 사이 배치 확인
- 사용자 최종 육안 확인 완료

검증 이미지:

- `Logs/PPEMirrorDiagnostics/mirror_close_composited.png`
- `Logs/PPEMirrorDiagnostics/reflection_texture.png`

### 관련 파일

- `Assets/Scripts/PlanarMirrorRenderer.cs`
- `Assets/Editor/PPERoomMirrorBuilder.cs`
- `Assets/Editor/PPERoomMirrorDiagnostics.cs`
- `Assets/Materials/PPE/Mirror/`
- `Assets/Scenes/3_PPE_Room.unity`

## 7. 태블릿 손글씨 서명

### 적용 위치

```text
Tablet (1)
  └─ GeneratedPlane
      └─ Signature_Test_Sequence
          ├─ Player_Signature
          └─ Conductor_Signature
```

### 사용 이미지

- 작업자: `Assets/UIs/sign_stamp/sign_player_rm.png`
- 감독자: `Assets/UIs/sign_stamp/sign_conductor.png`

### 재생 순서

| 단계 | 대기 | 재생 시간 |
| --- | ---: | ---: |
| 초기 상태 | 0.8초 | 서명 숨김 |
| 작업자 서명 | 0초 | 1.6초 |
| 감독자 서명 | 0.35초 | 1.5초 |

### 구현 결정

- 서명 위치와 크기는 씬의 직렬화 값으로 관리한다.
- 런타임 코드는 레이아웃을 변경하지 않고 셰이더의 `_Reveal` 값만 변경한다.
- 알파와 밝기 임계값을 함께 사용해 PNG 배경을 제거한다.
- 전용 셰이더에 Quest/OpenXR Single Pass Instanced 매크로를 적용한다.

### 검증

- 초기, 작업자 작성 중, 감독자 작성 중, 완료 상태를 각각 캡처했다.
- 작업자와 감독자 서명이 문서 하단 칸 안에 배치됨을 확인했다.
- 기존 확인 도장은 그대로 유지된다.
- C# 컴파일 오류와 전용 셰이더 오류는 발견되지 않았다.

검증 이미지:

- `Logs/PPESignatureDiagnostics/signature_00_empty.png`
- `Logs/PPESignatureDiagnostics/signature_01_player_writing.png`
- `Logs/PPESignatureDiagnostics/signature_02_conductor_writing.png`
- `Logs/PPESignatureDiagnostics/signature_03_complete.png`

### 관련 파일

- `Assets/Scripts/HandwrittenSignatureSequence.cs`
- `Assets/Shaders/HandwrittenSignatureReveal.shader`
- `Assets/Editor/PPERoomSignatureTestBuilder.cs`
- `Assets/Editor/PPERoomSignatureDiagnostics.cs`
- `Assets/Materials/PPE/Tablet/Signatures/`
- `Assets/Scenes/3_PPE_Room.unity`

## 8. 의사결정

1. 최신 Pull 내용은 유지하고 로컬 패키지 참조만 복구한다.
2. 혼합기동 포그는 실제 깊이를 가진 여러 레이어로 구성한다.
3. PPE룸 거울은 고정 보조 카메라가 아닌 동적 평면 반사 방식으로 구현한다.
4. 거울 투영 범위는 거울 네 모서리를 기준으로 제한한다.
5. UI 위치·크기·색상은 씬과 Inspector 값을 기준으로 관리한다.
6. 런타임 서명 코드는 시각 상태만 변경한다.
7. XR용 프로젝트 셰이더에는 Single Pass Instanced 처리를 포함한다.
8. 시각 기능은 전용 진단 캡처를 남겨 회귀 확인이 가능하게 한다.

## 9. 후속 작업

- [ ] Tripo3D 패키지를 절대 경로가 아닌 공유 가능한 방식으로 전환
- [ ] Quest/OpenXR 실기기에서 거울 양안 렌더링 확인
- [ ] Quest에서 거울 RenderTexture 성능 측정
- [ ] 혼합기동 포그의 실기기 투명도와 깊이 간격 조정
- [ ] 서명 재생을 문서 확인 또는 승인 이벤트와 연결
- [ ] 태블릿을 다시 확인할 때 서명 재생 여부와 초기화 정책 결정

## 10. 혼합기룸 안전작업 현황판 적용

### 적용 대상

```text
5_MixerRoom
  └─ yellow_sign_board_3d_model
      └─ GeneratedPlane
```

### 적용 내용

- `Assets/UIs/Poster_guide/floor-standing safety board.png` 이미지를 안전 표지판의 `GeneratedPlane`에 적용했다.
- 기존 Plane의 위치·회전·크기는 유지하고 비어 있던 MeshRenderer 머티리얼 슬롯만 연결했다.
- 전용 URP Unlit 머티리얼 `Assets/Materials/MixerRoom/FloorStandingSafetyBoard_Unlit.mat`을 생성했다.
- 원본 PNG에 포함된 짙은 회색 외곽 여백 때문에 이미지가 작게 표시되어, 머티리얼의 타일링과 오프셋으로 보드 영역만 확대했다.
- 1차 크롭 후 좌우와 상단에 얇은 경계선이 보여 크롭 범위를 조금 더 안쪽으로 조정했다.
- 텍스처 Wrap Mode를 `Clamp`로 변경해 필터링 시 외곽 배경색이 가장자리에 번지는 현상을 방지했다.

### 최종 설정

| 항목 | 값 |
| --- | --- |
| Base Map / Main Texture | `floor-standing safety board.png` |
| Tiling | X `0.58`, Y `0.82` |
| Offset | X `0.21`, Y `0.08` |
| Wrap Mode | Clamp |
| 셰이더 | URP Unlit |

### 확인 결과

- 안전작업 현황판 이미지가 노란 표지판 전면을 충분히 채우도록 표시된다.
- 좌우와 상단에 보이던 얇은 외곽선이 제거된 것을 Scene View에서 확인했다.
- 사용자 육안 확인 결과 현재 상태로 적용 완료했다.

### 고해상도 이미지 적용

- 기존 `535 × 594` 원본을 바탕으로 구성, 색상, 한글 문구를 유지한 `1190 × 1322` 고해상도 버전을 제작했다.
- 고해상도 이미지는 `Assets/UIs/Poster_guide/floor-standing safety board_hd.png`로 추가했다.
- 기존 원본 파일은 비교와 복구가 가능하도록 그대로 보존했다.
- `FloorStandingSafetyBoard_Unlit.mat`의 Base Map과 Main Texture를 고해상도 이미지로 교체했다.
- 기존 Tiling, Offset 및 Clamp 설정은 유지했다.
- Scene View에서 확인한 결과 글자와 경계선의 선명도가 개선되어 실제 표시 품질 향상 효과가 있음을 확인했다.

