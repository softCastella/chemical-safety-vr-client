# Unity 프로젝트에서 Codex·Claude 병렬 작업 안전 가이드

- 작성일: 2026-08-04
- 대상 프로젝트: `Prototype_Tyche_Jinyoung`
- 대상 도구: Codex, Claude 및 동일 저장소를 수정하는 기타 코딩 에이전트
- 대상 환경: Unity 6000.4.8f1, Git, Windows, OneDrive 작업 폴더

## 1. 문서 목적

하나의 Unity 프로젝트에서 여러 코딩 에이전트를 동시에 사용할 때 발생할 수 있는
씬 덮어쓰기, Unity YAML 충돌, `.meta` GUID 손상, Editor 상태 경쟁 및 공용 코드
충돌을 예방하기 위한 운영 규칙을 정의한다.

이 문서는 다음 작업 방식을 구분한다.

- 같은 작업 폴더와 같은 씬을 여러 에이전트가 수정하는 경우
- 같은 작업 폴더에서 서로 다른 씬 또는 파일을 수정하는 경우
- Git branch와 worktree 또는 별도 복사본을 사용하는 경우
- 여러 Unity Editor를 사용해야 하는 경우
- 분리된 변경을 최종 작업 폴더에 통합하는 경우

## 2. 결론

- 같은 Unity 씬, Prefab 또는 Material을 두 에이전트가 동시에 수정하지 않는다.
- 하나의 작업 폴더에서는 Unity Editor와 Unity MCP를 한 에이전트만 제어한다.
- 단순 코드·문서 작업은 파일 담당 범위가 겹치지 않을 때 같은 작업 폴더에서도
  가능하지만, 변경 파일 목록과 작업 상태를 서로 공유해야 한다.
- 두 에이전트가 각각 Unity Editor를 사용해야 한다면 별도 Git worktree 또는 별도
  프로젝트 폴더를 사용한다.
- 가장 안전한 방식은 에이전트별 branch와 작업 폴더를 만든 뒤, 통합 담당자가 한
  변경씩 병합하고 단일 Unity Editor에서 검증하는 방식이다.

## 3. 작업 방식별 위험도

| 작업 방식 | 위험도 | 허용 기준 |
|---|---:|---|
| 같은 작업 폴더·같은 씬 동시 수정 | 매우 높음 | 금지 |
| 같은 작업 폴더·같은 Prefab 또는 Material 수정 | 매우 높음 | 금지 |
| 같은 작업 폴더·다른 씬 수정 | 중간 | 공용 파일이 겹치지 않고 Unity 저장 담당자가 한 명일 때만 허용 |
| 같은 작업 폴더·서로 다른 새 C# 파일 작성 | 낮음~중간 | 파일 소유권과 공용 API 변경 범위를 합의한 뒤 허용 |
| 같은 Unity Editor를 여러 에이전트가 제어 | 높음 | 원칙적으로 금지 |
| 별도 branch·별도 worktree에서 작업 | 낮음 | 권장 |
| 별도 worktree의 같은 씬 수정 후 병합 | 중간~높음 | 가능하지만 통합 담당자가 한쪽 씬을 기준으로 다른 변경을 재적용하는 방식 권장 |

## 4. 문제가 발생하는 기술적 이유

### Unity 씬과 Prefab의 직렬화

- Unity 씬과 Prefab은 YAML 텍스트이지만 일반 소스 코드처럼 줄 단위 병합이 항상
  안전하지 않다.
- 오브젝트 추가, 삭제, 부모 변경 및 컴포넌트 추가는 여러 FileID와 직렬화 참조를
  동시에 변경한다.
- 두 에이전트가 같은 씬을 각각 저장하면 마지막 저장이 앞선 변경을 덮어쓰거나,
  병합 후 존재하지 않는 FileID를 참조할 수 있다.
- Unity Editor는 저장할 때 관련 직렬화 블록의 순서와 값도 함께 변경할 수 있어
  실제 변경보다 diff가 커질 수 있다.

### 디스크 파일과 Unity 메모리 상태의 차이

- Unity Editor에 저장하지 않은 씬 변경이 있으면 디스크의 `.unity` 파일과 Editor
  메모리의 활성 씬 상태가 다르다.
- 한 에이전트가 디스크 YAML을 읽는 동안 다른 에이전트가 Unity MCP로 활성 씬을
  수정하면 서로 다른 기준을 보고 작업하게 된다.
- Play Mode에서 생성하거나 수정한 오브젝트는 일반적으로 종료 시 사라진다. 이를
  Edit Mode의 저장 대상이라고 오해하면 잘못된 검증 결과나 중복 오브젝트가 생길
  수 있다.

### Asset Database와 `.meta` 파일

- Unity는 `.meta` GUID로 씬, Prefab, Script, Material 및 Texture 참조를 연결한다.
- 에이전트가 Asset을 이동하거나 다시 만들면서 기존 `.meta`를 잃으면 다른 씬의
  참조가 끊어진다.
- 동일 프로젝트 폴더를 두 Unity Editor가 동시에 열면 Asset Database와 `Library`
  잠금 및 Import 상태가 충돌할 수 있다.

### 공용 C# 코드와 컴파일 상태

- 서로 다른 씬을 수정하더라도 공용 런타임 스크립트를 한쪽이 변경하면 프로젝트
  전체가 다시 컴파일된다.
- 컴파일 오류, Domain Reload 및 Assembly Reload는 다른 에이전트가 실행 중이던
  Play Mode 또는 Unity MCP 명령을 중단시킬 수 있다.
- 공용 enum, serialized field 또는 API 이름 변경은 담당하지 않은 씬과 Prefab에도
  직렬화 및 컴파일 영향을 준다.

### OneDrive 동기화

- 현재 프로젝트는 OneDrive 아래에 있어 Git 외에도 파일 동기화가 개입한다.
- Unity가 씬이나 Asset을 저장하는 순간 OneDrive가 파일을 동기화하면 잠금, 지연,
  충돌 사본 또는 불필요한 타임스탬프 변경이 발생할 수 있다.
- 병렬 작업용 worktree는 가능하면 OneDrive 밖의 로컬 개발 폴더에 두고, Git을
  변경 전달 수단으로 사용하는 편이 안전하다.

## 5. 금지 사항

- 같은 `.unity` 씬을 두 에이전트가 동시에 수정하고 각각 저장하지 않는다.
- 같은 Prefab, Material, Shader Graph, Input Actions 또는 `ProjectSettings` 파일을
  동시에 수정하지 않는다.
- 같은 프로젝트 경로를 두 Unity Editor에서 강제로 열지 않는다.
- 다른 에이전트가 만든 `.meta` 파일을 삭제하거나 Asset만 다시 생성하지 않는다.
- 통합 과정에서 `git reset --hard`, 무분별한 전체 파일 복원 또는 상대 에이전트의
  변경 삭제를 사용하지 않는다.
- `Library`, `Temp`, `Logs`, `UserSettings` 및 생성된 `.csproj`를 작업 전달 또는
  병합 대상으로 사용하지 않는다.
- Unity가 컴파일, Import 또는 Domain Reload 중일 때 다른 에이전트가 Play Mode를
  시작하거나 씬 저장 명령을 실행하지 않는다.
- Play Mode의 임시 오브젝트를 Edit Mode 씬 오브젝트로 간주해 저장하지 않는다.

## 6. 운영 방식 A: 하나의 작업 폴더를 공유하는 경우

작업 규모가 작고 한 Unity Editor만 사용할 때 적용한다.

### 필수 규칙

1. 작업 시작 전에 에이전트별 담당 파일을 선언한다.
2. 씬, Prefab, Material 및 ProjectSettings에는 단일 소유자를 지정한다.
3. Unity Editor와 Unity MCP에는 단일 제어자를 지정한다.
4. 다른 에이전트는 담당 파일 외에는 읽기만 수행한다.
5. 공용 스크립트 변경이 필요하면 현재 작업을 멈추고 소유권을 넘긴다.
6. 작업 완료 시 변경 파일, 검증 결과와 남은 수동 검증을 공유한다.

### 허용 가능한 예

```text
Codex
├─ Assets/Scripts/PPEScenarioManager.cs
├─ Assets/Scripts/PPEInspectionPanelController.cs
└─ Docs/MeetingNotes/...

Claude
├─ Assets/Shaders/PPEContamination.shadergraph
├─ Assets/Materials/PPE/Contamination/...
└─ Assets/Textures/PPE/Contamination/...

통합 담당자
└─ Assets/Scenes/3_PPE_Room_HandTest_scale.unity
```

이 경우에도 Shader Graph가 사용하는 Material과 씬 연결은 통합 담당자가 순차적으로
수행한다.

### 작업 상태 공유 형식

각 에이전트는 최소한 다음 내용을 전달한다.

```text
작업 상태: 진행 중 / 완료 / 중단
담당 파일:
- Assets/...

공용 파일 변경:
- 없음 또는 구체적인 경로

검증:
- 컴파일 결과
- Unity Console 결과
- 아직 필요한 Play Mode 또는 Quest 검증
```

## 7. 운영 방식 B: 별도 Git worktree 사용

두 에이전트가 독립적으로 코드를 수정하거나 각각 Unity Editor를 사용해야 할 때
권장한다.

### 예시 구조

```text
Prototype_Tyche_Integration/   최종 통합 및 Unity 검증
Prototype_Tyche_Codex/         Codex 전용 branch
Prototype_Tyche_Claude/        Claude 전용 branch
```

- 각 폴더는 서로 다른 branch를 사용한다.
- 각 폴더는 자체 `Library`, `Temp`와 Unity Import 상태를 가진다.
- `Library`를 worktree 간에 복사하거나 공유하지 않는다.
- 디스크 사용량과 최초 Import 시간은 증가하지만 Unity 상태 충돌은 크게 줄어든다.

### 생성 예시

기준 저장소에서 branch 이름과 대상 경로가 기존 항목과 겹치지 않는지 확인한 뒤
다음과 같이 만들 수 있다.

```powershell
git worktree add C:\UnityWorktrees\Prototype_Tyche_Codex -b agent/codex-ppe
git worktree add C:\UnityWorktrees\Prototype_Tyche_Claude -b agent/claude-ppe-art
```

실제 경로와 branch 이름은 작업 환경에 맞게 정한다. worktree 생성 전에 현재 변경을
커밋하거나 안전하게 보존하여 각 branch의 시작 기준을 명확히 한다.

### Unity 사용 규칙

- Codex와 Claude가 각각 Unity를 열어야 한다면 반드시 서로 다른 worktree 경로를
  연다.
- 두 Editor가 동일한 씬 이름을 열 수는 있지만, 같은 씬을 양쪽에서 수정하면 최종
  병합 위험은 그대로 남는다.
- 각 branch에서 별도 테스트 씬을 사용하고, 최종 기준 씬 연결은 통합 branch에서
  한 번만 수행하는 방식을 권장한다.

## 8. 씬 작업 분리 전략

### 같은 씬을 수정해야 하는 경우

- 한 에이전트만 씬을 수정한다.
- 다른 에이전트는 새 Script, Shader, Material 또는 Prefab을 준비한다.
- 준비된 Asset을 씬에 연결하는 작업은 씬 소유자가 수행한다.
- 두 branch에서 같은 씬을 이미 수정했다면 복잡한 YAML 자동 병합보다 한쪽 씬을
  기준으로 선택하고 다른 변경을 Unity Editor에서 다시 적용하는 편이 안전하다.

### 서로 다른 씬을 수정하는 경우

- 각 씬의 소유자를 지정한다.
- 공용 Prefab과 Script 수정 권한은 별도로 지정한다.
- `ProjectSettings/EditorBuildSettings.asset` 변경은 통합 담당자만 수행한다.
- 시작 씬이나 Build Scene 순서 변경은 다른 씬 작업과 독립적이지 않으므로 마지막
  통합 단계에서 처리한다.

### 장기적인 충돌 감소 방법

- 대형 씬을 환경, XR Rig, UI, 시나리오처럼 Additive Scene으로 분리하는 방식을
  검토할 수 있다.
- 공용 오브젝트를 무조건 Prefab으로 분리하면 Prefab 자체가 새 충돌 지점이 될 수
  있으므로, 실제 소유권과 재사용 경계를 기준으로 분리한다.
- 씬 분리는 별도 기능 변경이므로 현재 작업 중 임의로 도입하지 않고 팀 합의 후
  진행한다.

## 9. 파일 유형별 소유권 권장표

| 파일 유형 | 동시 수정 | 권장 담당 방식 |
|---|---|---|
| `.unity` | 금지 | 씬별 단일 소유자 |
| `.prefab` | 금지 | Prefab별 단일 소유자 |
| `.mat`, `.shadergraph` | 금지 | Material 또는 Shader별 단일 소유자 |
| 새 `.cs` 파일 | 조건부 허용 | 에이전트별 별도 파일 |
| 기존 공용 `.cs` | 순차 작업 | 단일 소유자 후 인계 |
| `.inputactions` | 금지 | XR 입력 담당자 한 명 |
| `ProjectSettings/*` | 금지 | 통합 담당자 한 명 |
| 문서 `.md` | 조건부 허용 | 문서별 단일 소유자 |
| `.meta` | Asset과 함께 처리 | Asset 소유자가 보존 |

## 10. 권장 병합 순서

1. 각 에이전트가 자신의 branch에서 변경 파일 목록과 diff를 확인한다.
2. C# 컴파일과 에이전트 전용 테스트를 완료한다.
3. 변경을 기능 단위의 작은 커밋으로 정리한다.
4. 통합 branch에 공용 데이터 구조와 런타임 Script를 먼저 병합한다.
5. Unity를 열어 컴파일과 Domain Reload가 끝날 때까지 기다린다.
6. Shader, Material, Texture 및 Prefab 변경을 병합한다.
7. 최종적으로 씬 변경을 한 작업씩 병합하거나 Unity에서 다시 연결한다.
8. Build Settings와 ProjectSettings 변경은 마지막에 통합 담당자가 적용한다.
9. Unity Console, Play Mode, 씬 직렬화 참조 및 Quest/OpenXR 동작을 확인한다.

## 11. Unity YAML 충돌 처리

- 간단한 독립 블록 충돌은 Unity의 Smart Merge 도구를 보조 수단으로 사용할 수
  있다.
- Smart Merge가 성공했다고 해서 씬 참조가 정상이라는 뜻은 아니다.
- 다음 충돌은 자동 병합보다 기준 씬을 선택하고 변경을 재적용한다.
  - 같은 GameObject의 Transform 수정
  - 같은 컴포넌트 필드 수정
  - 부모·자식 Hierarchy 변경
  - 컴포넌트 추가·삭제
  - Prefab Override 변경
  - 동일 FileID 주변의 충돌
- 병합 후에는 Missing Script, Missing Prefab, null Material, 중복 컴포넌트와
  잘못된 부모를 Unity Hierarchy와 Inspector에서 확인한다.

## 12. 충돌 또는 덮어쓰기 발생 시 대응

1. Unity와 자동 저장 작업을 멈춘다.
2. `git status`와 `git diff`로 실제 변경 파일을 확인한다.
3. 양쪽 변경을 별도 branch 또는 커밋으로 보존한다.
4. 어떤 에이전트도 상대 변경을 임의로 삭제하거나 전체 복원하지 않는다.
5. 씬 충돌은 기준 버전을 결정한 뒤 다른 쪽 작업을 Unity Editor에서 재적용한다.
6. `.meta` GUID가 변경됐다면 기존 참조가 사용하던 GUID를 우선 복구한다.
7. Unity Import와 컴파일이 끝난 뒤 Console과 씬 참조를 다시 확인한다.

## 13. 현재 PPE 프로젝트 권장 분업

### 통합 담당자

- `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`
- PPE 월드 패널의 씬 연결
- XR Interaction Layer와 입력 연결
- Body Anchor, 착용 Anchor 및 거울 Culling Mask
- Play Mode와 Quest 최종 검증

### 런타임 코드 담당자

- PPE Clean/Contaminated 상태 머신
- 관찰, 사용, 폐기 및 Clean 교체 규칙
- 착용 애니메이션 프로필
- 좌우 손·장갑·장화 상태
- 시나리오 순서와 결과 기록

### 셰이더·아트 담당자

- PPE 오염 마스크 텍스처
- URP Clean/Contaminated 셰이더
- 마스크 유리 오염 표현
- 착용 페이드용 셰이더 속성
- 별도 테스트 씬 또는 Material 검증 Prefab

### 문서·검증 담당자

- 요구사항과 결정 사항 갱신
- 결정적 Editor 검증 Harness
- Quest 수동 검증 체크리스트
- 변경 파일 및 영향 범위 기록

씬 연결은 각 담당자가 직접 동시에 수행하지 않고 통합 담당자에게 전달한다.

## 14. 통합 후 필수 검증

### Git 및 파일 검증

- [ ] 의도하지 않은 씬, Prefab, Material 또는 ProjectSettings 변경이 없다.
- [ ] 새 Asset에 대응하는 `.meta` 파일이 존재한다.
- [ ] 기존 `.meta` GUID가 불필요하게 변경되지 않았다.
- [ ] `Library`, `Temp`, `Logs` 또는 생성된 프로젝트 파일이 변경 대상에 없다.
- [ ] 병합 충돌 표시가 파일에 남아 있지 않다.

### Unity 검증

- [ ] Unity Import와 Script Compilation이 완료됐다.
- [ ] Console에 C# 오류와 런타임 예외가 없다.
- [ ] 씬에 Missing Script와 Missing Prefab이 없다.
- [ ] 씬·Prefab의 직렬화 참조가 올바르다.
- [ ] Edit Mode의 작성값이 Play Mode 진입 후 의도치 않게 덮어써지지 않는다.
- [ ] Play Mode 종료 후 임시 오브젝트가 씬에 중복 저장되지 않는다.

### XR 검증

- [ ] 왼손과 오른손 입력이 모두 동작한다.
- [ ] Quest/OpenXR에서 양안 렌더링이 정상이다.
- [ ] Shader와 Material이 한쪽 눈에서 사라지지 않는다.
- [ ] 씬 변경이 텔레포트, UI Ray, Grab 또는 Hand Visual을 손상시키지 않았다.
- [ ] 거울과 Main Camera의 Culling Mask가 의도한 Visual을 각각 표시한다.

## 15. 문서 적용 범위와 검증 상태

### 적용한 변경

- 본 가이드를 `Docs/Unity_Multi_Agent_Collaboration_Guide.md`에 추가했다.
- 프로젝트 런타임 코드, 씬, Prefab, Material 및 ProjectSettings는 본 문서 작성으로
  변경하지 않았다.

### 근본 원인

- 여러 에이전트가 같은 Unity 프로젝트를 수정할 때 일반 코드 저장소보다 씬,
  Prefab, Asset Database 및 Unity Editor 메모리 상태에서 더 큰 충돌 위험이 있다.
- 기존 문서에는 Codex·Claude의 동시 작업 범위, worktree 분리와 최종 통합 절차를
  함께 다루는 기준 문서가 없었다.

### 영향 범위

- 향후 Codex, Claude 또는 다른 에이전트에게 작업을 병렬 배정할 때 파일 소유권,
  Unity 제어권, branch 및 검증 기준으로 사용한다.
- 이 문서는 운영 지침이며 자동 잠금이나 기술적 충돌 방지 기능을 제공하지 않는다.

### 완료한 검증

- `Docs`와 `Assets/Docs`에서 동일한 멀티 에이전트 Unity 협업 문서가 없는 것을
  확인했다.
- 문서를 한국어로 작성하고 기존 프로젝트의 씬·Prefab·XR·문서 작성 규칙을
  반영했다.

### 아직 필요한 수동 검증

- 실제 Codex·Claude 병렬 작업을 시작하기 전에 사용할 Git branch, worktree 경로와
  통합 담당자를 지정한다.
- 첫 병렬 작업은 같은 씬을 피하고 서로 다른 새 파일과 별도 테스트 씬으로 시험한다.
- worktree를 OneDrive 밖에 생성할 수 있는지 디스크 용량과 Unity 라이선스 환경을
  확인한다.
