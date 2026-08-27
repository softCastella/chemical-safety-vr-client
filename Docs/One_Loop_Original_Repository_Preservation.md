# One Loop 원본 보존 리포지토리

## 목적

이 문서는 앱 시작부터 코어 루프 진입, PPE 교육, 완료 후 복귀까지 한 바퀴가 확인된 프로젝트를 원본 기준으로 보존하기 위한 기록이다. 이후 컨트롤러 교육과 안전교육을 모듈로 분리하는 실험은 원본 보존본과 분리된 작업본에서 진행한다.

## 보존 기준

| 항목 | 값 |
| --- | --- |
| 기준 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` |
| 원격 원본 | `https://github.com/softCastella/Prototype_Tyche_Jinyoung.git` |
| 보존 브랜치 | `backup/one-loop-original` |
| 보존 기준 커밋 | `c2e9e73` — `Preserve one-loop original baseline` |
| 보존 문서 포함 최신 커밋 | `063437b` — `Document one-loop original preservation` |
| 보존 태그 | `one-loop-original-2026-08-09` |
| 보존 리포지토리 | `https://github.com/softCastella/One_Loop_Original` |

기준 커밋에는 보존 시점의 작업 파일과 문서를 포함한다. 기존 Git 커밋 이력은 기준 커밋의 부모 이력으로 이어지며, 기존 브랜치와 태그는 새 보존 리포지토리에 함께 push한다.

## 보존 시점에 확인한 상태

- 송기마스크 패널은 씬 작성 RectTransform을 유지하고 공용 레이아웃 프리셋의 런타임 덮어쓰기를 사용하지 않는다.
- `helmet`은 유일한 Grab/상태 소유자이며 `helmet_wrong`은 물리·상호작용 컴포넌트가 없는 시각 전용 자식이다.
- 비헤드셋 Play Mode에서 마스크 레이아웃 유지와 헬멧 정상/하자 시각 전환을 확인했다.
- Quest Link는 Meta Runtime 재시작 후 연결을 복구했다. 실제 Quest/OpenXR 전체 코어 루프의 재실행 결과는 보존 리포지토리 생성 후 별도 기록한다.

## 원본과 실험본의 운영 원칙

### 원본 보존본

- 기준 커밋과 태그를 삭제하거나 재작성하지 않는다.
- 모듈 분기 작업, 타이틀 허브 변경, 진입 흐름 변경을 직접 적용하지 않는다.
- 원본에서 발견한 회귀는 이슈 문서로 기록하고, 수정은 실험본에서 먼저 검증한다.

### 모듈 실험본

- 스플래시/타이틀 이후 모듈 선택 허브를 둔다.
- `안전교육`과 `컨트롤러 연습`을 선택한 모듈의 시작 상태로 라우팅한다.
- 안전교육의 시나리오 선택은 `카드 → 상세 모달 → 시작`으로 단순화한다.
- 원본에서 검증된 버그 수정이 필요할 때는 커밋 단위로 cherry-pick하고, 모듈 흐름 변경과 섞지 않는다.

## 복제 및 추적 절차

보존 리포지토리는 파일을 새로 복사하거나 `git init`으로 이력을 끊지 않고, 기존 저장소의 모든 브랜치·태그·커밋을 push해 만든다.

```text
기존 Prototype_Tyche_Jinyoung
    └─ backup/one-loop-original
        └─ one-loop-original-2026-08-09
            └─ GitHub One_Loop_Orig
```

대용량 Unity 파일이 Git LFS로 추적되는 경우에는 일반 Git refs와 함께 LFS 객체도 push해야 한다. 복제 후에는 다음을 확인한다.

1. 보존 리포지토리의 기준 커밋이 `c2e9e73`인지 확인한다.
2. `one-loop-original-2026-08-09` 태그가 존재하는지 확인한다.
3. 기존 브랜치와 태그 수가 원본과 같은지 비교한다.
4. `git lfs ls-files` 결과와 LFS 객체가 누락되지 않았는지 확인한다.
5. Unity에서 기준 씬을 열고 보존 문서의 상태와 실제 씬 직렬화가 일치하는지 확인한다.

## 변경 이력

- 2026-08-09: One Loop 원본 보존 기준 커밋과 태그를 만들고, 모듈 실험본과 분리하는 운영 원칙을 기록했다.
