# 2026-07-25 PPE 태블릿 체크·서명·SFX 점검 회의록

## 1. 기본 정보

- 일자: 2026-07-25
- 프로젝트: `3D_UI_Test_home`
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`
- 대상 문서: `Tablet (1)/GeneratedPlane`
- 현재 상태: 체크·서명 애니메이션 구현 완료, 오디오 밸런스와 Play 진입 번쩍임은 추가 조사 필요

## 2. 구현 내용

### 체크 애니메이션

- `Assets/UIs/sign_stamp/sign_check.png` 사용
- 작업 전 확인 체크박스 5개에 순차 Reveal 애니메이션 적용
- 작은 화면에서도 보이도록 체크 전용 획 확장과 알파 강화 적용
- 체크 위치가 한 행 아래로 밀린 문제를 수정하고 5개 모두 체크박스 안에 배치

### 서명 애니메이션

- 작업자: `sign_player_rm.png`
- 감시인: `sign_conductor_ppe.png`
- 확인자: `sign_conductor_ppe.png`
- 작업자 → 감시인 → 확인자 순서로 Reveal

### SFX

- 체크 시작: `Assets/Audio/SFX/check.ogg`
- 서명 시작: `Assets/Audio/SFX/sign.ogg`
- 각 단계 시작과 동시에 AudioSource `PlayOneShot` 실행
- 애니메이션 길이를 실제 클립 길이에 맞춤

| 구분 | 클립 길이 |
| --- | ---: |
| 체크 | 약 1.489초 |
| 서명 | 약 1.724초 |

## 3. 검증 완료 항목

- 체크 5개 최종 위치 확인
- 작업자·감시인·확인자 서명 위치 확인
- 체크 및 세 서명의 중간 Reveal 상태 캡처
- 시퀀스 8단계의 AudioClip 연결 확인
- AudioSource `Play On Awake` 비활성화 확인
- 체크·서명 관련 C# 및 셰이더 컴파일 오류 없음

진단 이미지:

- `Logs/PPESignatureDiagnostics/signature_01_checklist_writing.png`
- `Logs/PPESignatureDiagnostics/signature_02_player_writing.png`
- `Logs/PPESignatureDiagnostics/signature_03_conductor_writing.png`
- `Logs/PPESignatureDiagnostics/signature_04_checker_writing.png`
- `Logs/PPESignatureDiagnostics/signature_05_complete.png`

## 4. 미해결 문제

### SFX 체감 음량

- SFX Volume을 `1`로 설정하고 3D 거리 범위를 확장했지만 BGM에 묻히는 현상이 남았다.
- 애니메이션 중 BGM 덕킹을 적용하고 `Ducked Bgm Volume = 0`도 시험했다.
- BGM Fade In이 덕킹 값을 다시 덮어쓰는 경로를 수정했지만, 사용자 확인 결과 덕킹만으로 완전히 해결되지 않았다.
- 현재 Audio Mixer 라우팅은 설정돼 있지 않다.

### 태블릿 하늘색 번쩍임

- Play Mode에 들어갈 때 태블릿이 하늘색으로 번쩍인다.
- 태블릿 본체의 흰색 Emission, Emission Map 및 `_EMISSION` 키워드를 비활성화했다.
- 사용자 확인 결과 Emission만으로는 현상이 해결되지 않았다.
- 문서 Plane, 다른 Renderer, 카메라 및 OpenXR 첫 프레임을 포함한 추가 분리 진단이 필요하다.

## 5. 의사결정

1. BGM 덕킹과 Emission을 최종 해결로 기록하지 않는다.
2. 현재 상태는 부분 조치 후 미해결로 관리한다.
3. 오디오 문제는 Play Mode에서 실제로 재생 중인 모든 AudioSource를 계측해 출력 경로를 분리한다.
4. 필요하면 BGM과 SFX를 Audio Mixer 그룹으로 나누고 SFX 게인 및 Duck Snapshot을 사용한다.
5. 번쩍임은 Game View, Scene View, HMD를 분리해 어느 출력에서 발생하는지 먼저 확인한다.
6. UI·머티리얼 값은 씬과 Inspector 설정을 기준으로 유지하고 런타임에서 임의로 덮어쓰지 않는다.

## 6. 후속 작업

- [ ] Play Mode에서 모든 AudioSource의 실제 재생 상태 기록
- [ ] BGM Source 직접 Mute A/B 테스트
- [ ] SFX와 BGM 원본 클립의 피크·RMS 비교
- [ ] Audio Mixer 기반 BGM/SFX 라우팅 검토
- [ ] Play 첫 프레임 전후 태블릿 Renderer 캡처
- [ ] Tablet 본체, 문서 Plane, 서명 Overlay를 순차 비활성화해 번쩍임 원인 분리
- [ ] Game View와 Quest HMD에서 번쩍임 발생 위치 비교

## 7. 관련 버그 리포트

- `Docs/Bug/2026-07-25_PPE_Tablet_SFX_BGM_BlueFlash.md`
