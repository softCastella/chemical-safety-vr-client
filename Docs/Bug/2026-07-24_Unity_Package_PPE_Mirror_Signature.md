# 2026-07-24 Unity 패키지·PPE룸 렌더링 버그리포트

## 1. 기본 정보

- 프로젝트: `3D_UI_Test_home`
- 환경: Unity 6000.4.8f1 / URP 17.4.0 / OpenXR
- 대상 씬:
  - `Assets/Scenes/3_PPE_Room.unity`
  - `Assets/Scenes/5_MixerRoom.unity`
- 관련 커밋: `89dfb3c`
- 보고 상태: 기능 복구 완료, Quest/OpenXR 실기기 QA 필요

## 2. 이슈 요약

| ID | 심각도 | 제목 | 현재 상태 | 잔여 위험 |
| --- | --- | --- | --- | --- |
| BUG-01 | P1 | Tripo3D 로컬 패키지가 다른 사용자 절대 경로를 참조 | 로컬 복구 | 높음 |
| BUG-02 | P1 | PPE룸 거울이 회푸른 단색·스카이박스·파란 벽만 표시 | 해결 | 중간 |
| BUG-03 | P1 | 보호복 앞면 대신 뒤쪽 시점이 거울에 렌더링 | 해결 | 낮음 |
| BUG-04 | P2 | 거울 크기·구도·가시 범위가 방 구조와 불일치 | 해결 | 낮음 |
| BUG-05 | P2 | 서명 컴포넌트에서 `UnityException` 발생 | 해결 | 낮음 |
| BUG-06 | P2 | 플레이어 서명 PNG 배경·크롭·배치 불일치 | 해결 | 낮음 |

## 3. BUG-01: Tripo3D 패키지 절대 경로 오류

### 증상

```text
Project has invalid dependencies:
com.tripo3d.unitybridge: The file
[C:\Users\lanoc\Downloads\Tripo3d_Unity_Bridge-latest\Tripo3d_Unity_Bridge\package.json]
cannot be found
```

### 재현 절차

1. 다른 사용자 환경에서 변경된 최신 브랜치를 Pull한다.
2. 동일한 절대 경로에 Tripo3D Unity Bridge가 없는 PC에서 Unity 프로젝트를 연다.
3. Unity Package Manager가 `manifest.json`의 `file:` 의존성을 해석하면서 실패한다.

### 원인

다음 파일에 특정 개발자 PC의 사용자명과 Downloads 경로가 저장되어 있었다.

- `Packages/manifest.json`
- `Packages/packages-lock.json`

저장소를 다른 PC에서 Pull해도 해당 절대 경로는 존재하지 않는다.

### 조치

현재 PC의 설치 위치로 패키지 경로를 변경했다.

```text
file:C:/Users/user/Desktop/Tripo3d_Unity_Bridge
```

### 결과

- 현재 PC에서 패키지 참조 복구
- 최신 Pull 내용 유지
- Unity 프로젝트 작업 재개 가능

### 잔여 위험

현재 경로 역시 사용자별 절대 경로다. 다른 개발자, CI 또는 빌드 머신에서는 같은 오류가 다시 발생할 수 있다.

### 권장 해결

1. `Packages/` 아래 임베디드 패키지로 포함
2. Git URL 의존성으로 전환
3. 사내 Unity Package Registry로 배포

## 4. BUG-02~04: PPE룸 평면 거울 반사 실패

### 증상

- 거울 표면에 회푸른 단색만 표시됨
- 스카이박스 또는 파란 벽만 표시됨
- 카메라 방향을 반대로 바꿔도 결과 차이가 거의 없음
- 카메라 가시 범위가 길어 방 밖 공간이 포함됨
- 보호복의 앞면이 아닌 뒤쪽 시점이 보임
- 초기 거울 크기가 요구보다 작음

### 원인 분석

초기 구현은 보조 카메라의 위치와 방향을 고정한 뒤 수동으로 조정하는 방식이었다.

이 방식에는 다음 문제가 있었다.

1. 원본 Game/Scene 카메라 이동과 반사 카메라가 기하학적으로 연결되지 않음
2. 카메라 Yaw 반전만으로 실제 거울 시점을 만들 수 없음
3. 일반 카메라 FOV가 거울 개구부보다 넓은 공간을 렌더링
4. near/far 범위가 넓어 방 밖 배경이 포함됨
5. 반사 대상과 거울 표면의 앞뒤 기준이 일치하지 않음

### 최종 조치

기존 거울 계층을 제거하고 다음 구조로 전면 재구축했다.

- 루트: `PPE_Room_Planar_Mirror`
- 표면: `Mirror_Surface`
- 카메라: `Mirror_Reflection_Camera`
- 런타임: `PlanarMirrorRenderer`

적용 내용:

- 원본 카메라 위치를 거울 평면 뒤로 반사
- 거울 네 모서리로 오프축 투영 프러스텀 계산
- 거울 레이어를 반사 카메라에서 제외
- 거울이 원본 카메라에 보일 때만 반사 렌더링
- 표면 클립 오프셋: `0.04m`
- 반사 깊이: `12m`
- 거울 크기: `1.44 × 3.3125m`
- RenderTexture: `640 × 1472`

### 검증 결과

- PPE룸 내부와 보호복 반사 확인
- 단색·스카이박스·파란 벽 증상 해소
- 보호복 반사 방향 개선
- 하얀 판넬과 문 사이 배치 확인
- 사용자 최종 육안 확인 완료

검증 이미지:

- `Logs/PPEMirrorDiagnostics/mirror_close_composited.png`
- `Logs/PPEMirrorDiagnostics/reflection_texture.png`

### 영향 파일

- `Assets/Scripts/PlanarMirrorRenderer.cs`
- `Assets/Editor/PPERoomMirrorBuilder.cs`
- `Assets/Editor/PPERoomMirrorDiagnostics.cs`
- `Assets/Materials/PPE/Mirror/`
- `Assets/Scenes/3_PPE_Room.unity`

### 잔여 위험

- Quest/OpenXR 양안 렌더링 미검증
- RenderTexture로 인한 Quest GPU 비용 미측정
- 다른 카메라 스택 또는 멀티 카메라 추가 시 재검증 필요

## 5. BUG-05: 서명 컴포넌트 UnityException

### 증상

초기 서명 구현 후 Unity 로그에서 `UnityException`이 발생했다.

### 원인

`MonoBehaviour` 필드 선언 시점에 `MaterialPropertyBlock`을 생성했다.

UnityEngine 객체는 `MonoBehaviour` 생성자 또는 필드 초기화 과정에서 생성하면 Unity 수명주기 규칙과 충돌할 수 있다.

### 조치

- 필드를 참조형으로만 선언
- `Awake` 또는 최초 사용 시점에 지연 생성
- `EnsurePropertyBlock()`을 통해 중복 생성 방지

### 결과

- 서명 빌더 재실행 성공
- 관련 `UnityException` 미검출
- C# 컴파일 오류 미검출

### 영향 파일

- `Assets/Scripts/HandwrittenSignatureSequence.cs`

## 6. BUG-06: 서명 이미지 배경·크롭·배치

### 증상

- 플레이어 서명 주변에 불필요한 배경이 포함됨
- 작업자와 감독자 서명이 문서 칸에 정확히 맞지 않음
- 두 원본 PNG의 해상도와 여백 차이로 크기가 달라 보임

### 원인

- 배경 제거 전 플레이어 이미지 사용
- 이미지별 알파와 밝기 임계값 차이
- 원본 이미지의 여백과 Quad 비율 불일치

### 조치

- 플레이어 이미지를 `sign_player_rm.png`로 교체
- 이미지별 Crop 범위와 밝기 임계값 조정
- 문서 하단 칸에 맞춰 Quad 위치와 스케일 조정
- 런타임에서는 `_Reveal`만 변경하도록 구성

최종 배치:

| 서명 | Local Position | Local Scale |
| --- | --- | --- |
| 작업자 | `(-2.64, -1.65, -0.25)` | `(2.0, 0.26, 1.0)` |
| 감독자 | `(-0.03, -1.65, -0.25)` | `(2.0, 0.26, 1.0)` |

최종 재생:

| 단계 | 대기 | 시간 |
| --- | ---: | ---: |
| 초기 대기 | 0.8초 | - |
| 작업자 | 0초 | 1.6초 |
| 감독자 | 0.35초 | 1.5초 |

### 검증 결과

- 서명 없음 상태 확인
- 작업자 작성 중 상태 확인
- 감독자 작성 중 상태 확인
- 두 서명 완료 상태 확인
- 기존 확인 도장 유지
- 전용 셰이더 오류 미검출

검증 이미지:

- `Logs/PPESignatureDiagnostics/signature_00_empty.png`
- `Logs/PPESignatureDiagnostics/signature_01_player_writing.png`
- `Logs/PPESignatureDiagnostics/signature_02_conductor_writing.png`
- `Logs/PPESignatureDiagnostics/signature_03_complete.png`

### 영향 파일

- `Assets/Scripts/HandwrittenSignatureSequence.cs`
- `Assets/Shaders/HandwrittenSignatureReveal.shader`
- `Assets/Editor/PPERoomSignatureTestBuilder.cs`
- `Assets/Editor/PPERoomSignatureDiagnostics.cs`
- `Assets/Materials/PPE/Tablet/Signatures/`
- `Assets/UIs/sign_stamp/`
- `Assets/Scenes/3_PPE_Room.unity`

## 7. 검증 매트릭스

| 항목 | 방법 | 결과 |
| --- | --- | --- |
| Unity 배치 실행 | Builder/Diagnostics 실행 | ExitCode 0 |
| 거울 표면·반사 텍스처 | 전용 진단 캡처 | 통과 |
| 거울 방향·투영 | 최종 합성 화면 확인 | 통과 |
| 서명 초기·중간·완료 | 4단계 진단 캡처 | 통과 |
| C# 오류·UnityException | 로그 검색 | 관련 오류 없음 |
| 전용 서명 셰이더 | 셰이더 로그 검색 | 오류 없음 |
| Quest/OpenXR 양안 | HMD 실기기 | 미검증 |
| Quest 성능 | GPU/프레임 측정 | 미검증 |

## 8. 재발 방지

1. 저장소에 사용자별 절대 `file:` 패키지 경로를 커밋하지 않는다.
2. 거울 카메라 Transform을 수동 튜닝하지 않고 원본 카메라와 거울 평면으로 계산한다.
3. 거울 표면, RenderTexture, 최종 합성 화면을 같은 카메라 조건에서 함께 검증한다.
4. `MonoBehaviour` 필드 초기화 과정에서 UnityEngine 객체를 생성하지 않는다.
5. UI 위치·크기·색상은 씬과 Inspector의 직렬화 값을 기준으로 한다.
6. 런타임 코드는 UI 상태만 변경한다.
7. 프로젝트 전용 XR 셰이더는 Single Pass Instanced 매크로를 유지한다.
8. 최종 판정은 Quest/OpenXR 실기기 양안 확인 후 완료한다.

