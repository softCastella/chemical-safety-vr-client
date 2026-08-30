# 2026-08-04~05 PPE 태블릿 체크·서명 미표시 및 재생 타이밍

## 2026-08-17 추가: 태블릿 재배치 후 시퀀스 단계 불일치

### 대상

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity` |
| 대상 오브젝트 | `PPE/PPE_C_Tablet/GeneratedPlane/Signature_Test_Sequence` |
| 활성 시퀀스 | `HandwrittenSignatureSequence` (fileID 1603400733) |
| 상태 | Editor 도구 추가 완료 — Unity 실행·저장·Play Mode 검증 전 |

### 증상

태블릿 문서 레이아웃이 변경되어 체크리스트가 좌우 2열(총 6개)로 나뉘고 서명란이 2개로
줄었다. 그러나 활성 시퀀스의 단계 목록은 이전 배치 기준이라 실제 배치와 어긋났다.

- `Checklist_Check_06`은 씬에 오브젝트·MeshRenderer(fileID 950192994)·머티리얼까지
  갖췄지만 단계 목록에 없어 애니메이션되지 않는다.
- 단계 목록 8번째가 `Checker_Signature`를 가리키지만 해당 GameObject는 비활성이다.

### 현재 씬에 작성된 배치 (변경하지 않음)

| 오브젝트 | 로컬 X, Y | 활성 | 머티리얼 텍스처 |
| --- | --- | --- | --- |
| `Checklist_Check_01~03` | -3.87 / -0.068, -0.179, -0.285 | O | `sign_check` |
| `Checklist_Check_04~06` | 0.34 / -0.068, -0.175, -0.284 | O | `sign_check` |
| `Player_Signature` | 2.848, -1.14 | O | `sign_player_rm` |
| `Conductor_Signature` | 2.85, -1.36 | O | `sign_conductor_mixer` |
| `Checker_Signature` | 2.580, -1.637 | **X** | `sign_conductor_ppe` |

이름과 실제 내용이 어긋나 있다. 화면에서 "이미 서명된 칸"으로 보이는 것은
`Conductor_Signature`이며 `sign_conductor_mixer` 텍스처를 쓴다. `Checker_Signature`라는
이름의 오브젝트는 `sign_conductor_ppe`를 쓰지만 비활성이라 화면에 나오지 않는다.

### 의도한 동작

1. 체크 6개가 왼쪽 열 위→아래, 이어서 오른쪽 열 위→아래 순서로 reveal된다.
2. `Player_Signature`가 이어서 reveal된다.
3. `Conductor_Signature`는 `animateReveal = false`로 시작부터 표시된 상태를 유지한다.
4. 비활성 `Checker_Signature` 단계는 목록에서 제거한다.
5. Transform·머티리얼·기존 단계의 sound·delay·duration·curve는 변경하지 않는다.

### 코덱스 세션에서 수정이 유실된 원인

이전 세션에서 씬 YAML을 직접 편집해 `Checklist_Check_06`을 삽입했으나 디스크에 남지 않았다.
원인은 파일 잠금이 아니라 **Unity 메모리의 씬이 더 최신이어서 저장 시 외부 편집을 덮어쓴 것**이다.
`git diff`에서 `--- !u!23 &950192994`가 신규 추가분으로 존재하는데 단계 목록에는 그 fileID가
없다는 점이 근거다.

재발 방지: Unity가 씬을 연 상태에서는 씬 YAML을 외부에서 직접 편집하지 않는다. 열린 씬을
대상으로 동작하는 Editor 메뉴 명령으로 적용한다.

### 적용한 변경

| 파일 | 변경 |
| --- | --- |
| `Assets/Editor/PPETabletSignatureSequenceSync.cs` | 신규. `Tools > PPE > Sync Tablet Signature Sequence Steps`와 `Tools > PPE > Validate Tablet Signature Sequence Steps` 추가 |

동작 규칙:

- `Signature_Test_Sequence`의 자식에서 이름으로 Renderer를 찾아 정해진 순서로 단계를 재구성한다.
- 이미 목록에 있는 단계는 `SerializedProperty`로 읽어 그대로 다시 쓴다. 작성된 sound, volume,
  `animateReveal`, delay, duration, curve를 덮어쓰지 않는다.
- 새로 추가되는 단계(`Checklist_Check_06`)만 `Checklist_Check_05`의 작성값을 복사하고
  `animateReveal = true`로 둔다.
- 목록에 없는 이름의 오브젝트가 씬에 없으면 오류 한 번을 남기고 중단한다. 오브젝트를 생성하거나
  Transform을 수정하지 않는다.
- `PPETabletChecklistController`는 항상 `PlayRange(0, StepCount)`를 호출하므로 단계 수가 바뀌어도
  인덱스 의존성이 깨지지 않는다.

### 완료한 검증

- [x] 정적 확인: 씬 YAML에서 단계 8개 구성, `Checklist_Check_06` 미연결, `Checker_Signature`
      비활성을 확인
- [x] 정적 확인: 각 Renderer의 머티리얼 GUID와 텍스처를 대조해 서명 주체 확인
- [x] 정적 확인: `dotnet build Assembly-CSharp-Editor.csproj` 오류 0개

### 남은 수동 검증

- [ ] Unity에서 `Tools > PPE > Sync Tablet Signature Sequence Steps` 실행 후 콘솔 로그의 순서 확인
- [ ] 씬 저장 후 `Tools > PPE > Validate Tablet Signature Sequence Steps`로 재확인
- [ ] Play Mode에서 태블릿 Trigger 1회 → 체크 6개 → 플레이어 서명 순서와 SFX 동기 확인
- [ ] 컨덕터 믹서 서명이 시작 시점부터 표시되는지 확인
- [ ] Quest/OpenXR 양안 확인

## 2026-08-16 통합 테스트 잔여 관찰

- 교육 모드에서 태블릿 체크·서명 시퀀스가 여러 번 실행되는 현상이 관찰됐다.
- 태블릿을 놓으면 체크·서명 완료 여부와 관계없이 방호복 착용 안내가 다시 재생되는 현상이
  관찰됐다.
- 기존 음성 설계는 태블릿 최초 해제에서 방호복 안내를 한 번만 재생하는 정책이다. Trigger 및
  Release 이벤트 중복 수신, 완료 상태 보존, 교육 세션 1회 플래그의 초기화 시점을 함께 확인한다.
- 이 절은 사용자 통합 테스트 관찰 기록이며 아직 원인 추적·수정·Play Mode 재검증 전이다.

## 개요

| 항목 | 내용 |
| --- | --- |
| 대상 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale*.unity` |
| 대상 오브젝트 | `PPE/Tablet/GeneratedPlane/Signature_Test_Sequence` |
| 상태 | 수정 완료 — Quest/Play Mode에서 동작 확인됨 |
| 심각도 | 높음 (수정 전) — 작업 확인서 완료 피드백 누락 |
| 관련 회의록 | `Docs/MeetingNotes/2026-07-16_PPE_Grab_Body_Equip_Interaction.md` §30–§31 |

## 증상 (수정 전)

1. 태블릿 문서 면만 보이고 체크·서명 **선이 그려지지 않았다**.
2. 태블릿을 **잡는 순간** SFX가 바로 재생되었다. 의도한 동작은 **Trigger 이후** reveal 애니메이션과 맞춰 소리가 나는 것이다.

## 재현 절차 (수정 전)

1. `3_PPE_Room_HandTest_scale_0`(또는 `scale`)에서 Play Mode 진입
2. 태블릿을 잡음 → SFX가 즉시 시작됨
3. Trigger를 눌러도 체크·서명 선이 보이지 않음

## 근본 원인

### 1) 시각 — 잘못된 머티리얼

활성 시퀀스 8개 Renderer가 `_MainTex`가 비어 있는 Scene Unlit Handwrite 머티리얼을 참조했다.

- `Assets/Materials/PPE/Scene Unlit/ChecklistCheck_Handwrite_Unlit.mat`
- `Assets/Materials/PPE/Scene Unlit/PlayerSignature_Handwrite_Unlit.mat`
- `Assets/Materials/PPE/Scene Unlit/ConductorSignature_Handwrite_Unlit.mat`

`_UseTextureAlpha`가 켜진 상태에서 텍스처가 없으면 `_Reveal`이 올라가도 출력 알파가 0이다.

### 2) 시각 — `_Reveal`이 화면에 반영되지 않음

머티리얼 GUID를 정상 자산으로 교체한 뒤에도 선이 안 보였다.

- `HandwrittenSignatureSequence`가 `MaterialPropertyBlock`으로 `_Reveal`만 갱신했다.
- Handwrite 머티리얼에 GPU Instancing이 켜져 있으면, 인스턴싱되지 않은 `_Reveal` 경로에서 MPB 값이 무시되고 머티리얼 기본값 `0`에 머물 수 있다.

### 3) 오디오 — grab 시 시퀀스 자동 시작

`PPETabletChecklistController.OnTabletSelected`가 선택(잡기) 시 `PlayRange`를 호출했다. Trigger 전에 전체 SFX가 재생되고, 이후 Trigger는 `IsPlaying` 때문에 무시될 수 있었다.

## 적용한 수정

### A. 씬 머티리얼 교체 (2026-08-05)

| 잘못된 머티리얼 | 교체 대상 |
| --- | --- |
| `ChecklistCheck_Handwrite_Unlit.mat` (×5) | `Tablet/Signatures/ChecklistCheck_Handwrite.mat` |
| `PlayerSignature_Handwrite_Unlit.mat` | `Tablet/Signatures/PlayerSignature_Handwrite.mat` |
| `ConductorSignature_Handwrite_Unlit.mat` (×2) | `Tablet/Signatures/ConductorSignature_Handwrite.mat` |

적용 씬: `3_PPE_Room_HandTest_scale.unity`, `_scale_0.unity`, `_scale_1.unity`.

### B. 재생 트리거·reveal·SFX (2026-08-05)

| 파일 | 변경 |
| --- | --- |
| `Assets/Scripts/PPETabletChecklistController.cs` | grab(`selectEntered`)에서는 시퀀스를 시작하지 않음. Trigger / Activate만 시작 |
| `Assets/Scripts/HandwrittenSignatureSequence.cs` | Renderer마다 런타임 머티리얼 인스턴스를 만들어 `_Reveal`을 직접 설정. SFX pitch = 클립 길이 ÷ reveal duration |
| `Assets/Shaders/HandwrittenSignatureReveal.shader` | `_Reveal`을 instanced prop으로 분리해 MPB/인스턴싱 경로에서도 갱신 가능 |
| Handwrite 머티리얼 3종 | GPU Instancing 비활성화 |

### C. 시각·속도 튜닝 (2026-08-05, 사용자 확인 후)

| 항목 | 변경 |
| --- | --- |
| 잉크 농도 | 체크·서명 머티리얼 `_InkColor`를 검정에 가깝게, `_AlphaBoost`·`_InkExpansion` 상향, `_InkThreshold` 하향 |
| 체크 속도 | 체크 5단계 `duration` `1.4889796` → `0.85`, 단계 간 `delayBefore` `0.12` → `0.05` (`scale` / `_0` / `_1`) |
| 서명 속도 | 기존 `1.7240816` / `delayBefore 0.35` 유지 |
| SFX | pitch 동기화로 체크 가속에 맞춰 소리도 짧아짐 |

## 영향 범위

- 체크 5개 + 서명 3단계 시각 reveal
- 태블릿 완료 SFX 시작 시점(잡기 → Trigger)
- Handwrite 머티리얼/셰이더를 공유하는 씬

## 완료한 검증

- [x] 씬 YAML에서 잘못된 Handwrite Unlit GUID 잔존 0, 정상 GUID(체크 5 / 플레이어 1 / 지휘·확인 2) 확인
- [x] grab 경로 `PlayRange` 제거를 코드에서 확인
- [x] Quest/Play Mode에서 사용자 확인: 선이 그려지고, Trigger 기준으로 소리·애니메이션이 맞음
- [x] 잉크 진하기·체크 속도 조정 후 사용자 수락

## 의도한 최종 동작

1. 태블릿을 **잡기만** 하면 체크/서명 SFX가 나지 않는다.
2. 잡은 뒤 **Trigger 1회** → 체크 5개(빠른 reveal) → 서명 3개가 순차 재생된다.
3. 각 단계의 선 reveal와 SFX가 같은 타이밍으로 진행된다.
4. 체크·서명 잉크는 문서 면에서 충분히 진하게 보인다.

## 참고

- 비활성/빈 참조용 `Signature_Test_Sequence`(컴포넌트 disabled, targetRenderer 없음)는 씬에 남아 있을 수 있으나 활성 시퀀스(`fileID: 1603400733` 계열)와 혼동하지 않는다.
- 셰이더·스크립트 컴파일이 끝난 뒤에 Play Mode / Quest Link를 시작해야 한다.

## 2026-08-27 후속: Trigger 순간 체크리스트 즉시 표시

- 현재 로코모션 씬의 활성 태블릿 시퀀스는 체크 6개, 플레이어 서명 애니메이션 1개,
  처음부터 보이는 확인자 고정 서명 1개로 구성된다.
- 기존에는 체크 6개가 각각 `0.85초`와 단계 간 지연으로 순차 표시되어, Trigger 입력 뒤 체크리스트
  전체가 보이기까지 약 5초 이상 걸렸다.
- 이제 잡은 손의 Trigger/Activate가 승인되는 순간 체크 6개를 모두 표시하고 체크 SFX는 한 번만
  재생한다. 이후 플레이어 서명과 확인 서명은 기존 작성 순서와 애니메이션을 유지한다.
- `PPETabletChecklistController.instantChecklistStepCount`를 Inspector 직렬화 값으로 노출했고 현재
  작성값은 `6`이다.
- Unity Editor 전용 검증에서 양 작업계획 시퀀스의 앞 6단계 순서와 즉시 표시 경로가 PASS했다.
- 실제 Quest Trigger, 체크 표시 양안 가시성, 서명 완료 이벤트는 HMD에서 수동 확인이 필요하다.

## 2026-08-31 후속: 7번째 체크 표시 누락

### 변경 전 필수 질문

1. 기존 두 문서의 `Checklist_Check_01~07`, 서명 오브젝트, Transform·Renderer·머티리얼 작성값을 보존한다.
2. 단일 상태 소유자는 `PPETabletChecklistController`이며, 작업계획별 시각 순서는 각
   `HandwrittenSignatureSequence.signatures` 배열이 소유한다.
3. 입력 경로는 잡은 태블릿의 XRI Activate/Trigger → `StartChecklistSequence()` →
   `PlayRangeWithInstantPrefix()` → 각 Renderer reveal이다. 입력 Action·Interactor·Collider는 변경하지 않는다.
4. 누락 Renderer를 런타임에서 자동 탐색하지 않는다. 기존 씬의 `Checklist_Check_07` Renderer를 명시적으로
   직렬화하고 하네스에서 순서를 검사한다.
5. 영향 소비자는 밀폐공간·누출대응 두 작업계획의 체크/서명 재생과 문서 완료 이벤트다. PPE 착용·모달·
   텔레포트·오디오는 기존 동작을 보존한다.
6. 변경 전 기준은 두 시퀀스 모두 체크 6+플레이어 서명 애니메이션 1+확인자 고정 서명 1, 즉시 표시 상한
   6이다. 변경 후 체크 7+플레이어 서명 애니메이션 1+확인자 고정 서명 1과 상한 7을 비교한다.
7. 씬 직렬화·코드·컴파일을 정적으로 확인한다. Unity Trigger 재생과 Quest 양안 표시는 수동 확인한다.

### 근본 원인

- 두 작업계획 문서 모두 `Checklist_Check_07` GameObject와 MeshRenderer는 씬에 작성돼 있었다.
- 그러나 두 `HandwrittenSignatureSequence.signatures` 배열은 `Checklist_Check_06` 다음에 바로
  `Player_Signature`로 이어져 7번째 Renderer를 재생하지 않았다.
- `PPETabletChecklistController.instantChecklistStepCount`와 동기화 도구의 `OrderedStepNames`도 6에서
  끝나 동일 누락을 정상으로 간주했다.

### 적용한 변경

- 누출대응 시퀀스(fileID `572508689`)의 7번째 단계에 기존 `Checklist_Check_07` Renderer
  (fileID `463784345`)를 연결했다.
- 밀폐공간 시퀀스(fileID `1603400733`)의 7번째 단계에 기존 `Checklist_Check_07` Renderer
  (fileID `1433562491`)를 연결했다.
- 새 체크 단계는 `Checklist_Check_06`과 같은 체크 SFX, `delayBefore=0.05`, `duration=0.85`, reveal curve를
  사용한다. 뒤의 플레이어·확인 서명 순서와 작성값은 그대로 유지했다.
- 현재 씬의 즉시 표시 상한과 새 컴포넌트 기본값을 7로 변경하고 `PPETabletSignatureSequenceSync`의 순서를
  `Checklist_Check_01~07 → Player_Signature → Conductor_Signature`로 확장했다. 레거시 Editor 설정 함수는
  대상 시퀀스 아래 실제 `Checklist_Check_*` Renderer 수를 세어 5체크 문서의 서명을 즉시 표시하지 않는다.
- 회귀 하네스와 즉시 표시 검증이 양 작업계획 시퀀스 모두 정확히 9단계인지 확인하도록 확장했다.
  회귀 하네스는 `Player_Signature`만 `animateReveal=true`이고 `Conductor_Signature`는
  `animateReveal=false`인 것도 검사한다.

### 검증 결과와 남은 항목

- 정적 씬 검사에서 두 시퀀스 모두 `Checklist_Check_01~07`, `Player_Signature`,
  `Conductor_Signature` 순서의 9개 Renderer 참조를 확인했다.
- `instantChecklistStepCount` 씬 작성값과 코드 기본값은 모두 7이며 `git diff --check`를 통과했다.
- Runtime·Editor C# 빌드는 기존 경고만 있고 오류 0개로 통과했다.
- Unity Editor가 대상 씬을 연 상태이므로 외부 변경을 다시 불러온 뒤
  `Tools > PPE > Validate Immediate Tablet Checklist Reveal` 및
  `Tools > PPE > Validate Locomotion PPE Regressions`를 실행해야 한다.
- 실제 Trigger 1회에서 체크 7개가 모두 즉시 보이고 체크 SFX가 한 번만 재생되는지, 그 뒤 플레이어 서명
  애니메이션 1개만 재생되며 확인자 서명은 고정 표시를 유지하고 문서 완료 이벤트가 발생하는지는 Game View와
  Quest/OpenXR에서 수동 확인한다.

## 2026-08-31 후속: 원거리에서 태블릿 파란 면이 문서 위로 사선 비침

### 변경 전 필수 질문

1. 태블릿 루트, 두 작업계획 문서의 XY 위치·회전·크기, 체크·서명 Renderer와 재생 순서를 보존한다.
2. 문서 표시의 단일 기준은 `PPE_C_Tablet` 아래의 `ConfinedSpace`와 `Leak` 문서 루트다.
3. 입력·Grab·Trigger·서명 재생 경로는 변경하지 않는다.
4. 런타임 자동 위치 보정은 추가하지 않고 씬에 작성된 문서 루트의 깊이만 수정한다.
5. 영향 소비자는 태블릿 표면과 두 문서의 깊이 판정이다. 모달, PPE 착용, 완료 이벤트는 보존한다.
6. 변경 전과 후에 태블릿 Canvas 및 두 문서 루트의 로컬 Z와 실제 월드 깊이 간격을 비교한다.
7. 씬 YAML은 정적으로 확인하고 원거리 Game View 및 Quest/OpenXR 표시는 수동 검증한다.

### 근본 원인과 적용

- 태블릿의 파란 표면 위에 표시되는 Canvas의 로컬 Z는 `0.0069`였고, `ConfinedSpace`와 `Leak` 문서 루트는
  모두 `0.007`이었다. 두 표면의 차이가 거의 없어 거리가 멀어질수록 깊이 버퍼가 앞뒤를 안정적으로
  구분하지 못하는 Z-fighting이 발생했고, 파란 면이 문서 위로 사선 형태로 비쳤다.
- 두 문서 루트의 로컬 Z만 `0.007 → 0.018`로 변경했다. 태블릿 루트의 로컬 Z 스케일
  `0.27530435`를 적용하면 Canvas와 문서 사이의 실제 깊이 간격은 약 `3.06mm`다.
- 두 문서에 동일한 값을 적용했으며 태블릿 루트 Transform, 문서 XY·회전·크기, 체크·서명 자식 Transform,
  머티리얼, 입력과 재생 코드는 변경하지 않았다.

### 완료한 검증과 남은 수동 검증

- 정적 씬 검사에서 `ConfinedSpace`와 `Leak` 모두 로컬 Z `0.018`, 이전 값 `0.007` 잔존 0개를 확인했다.
- `git diff --check`를 통과했다. 이번 후속은 씬 Transform 작성값만 변경해 별도 런타임 코드는 추가하지 않았다.
- Unity에서 외부 변경을 Refresh/Reload한 뒤 저장했다. 문서를 정면과 사선에서 멀리 보았을 때 파란 면이
  다시 비치지 않는지는 Game View와 Quest/OpenXR 양안에서 수동 확인해야 한다.
