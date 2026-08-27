# PPE 검사 마스크 프록시 초기 노출

## 증상

`3_PPE_Room_Train_Test_mask.unity`에서 Play Mode를 시작하면 `PPE_A_Mask_Check`가 검사 버튼을 누르기 전부터 화면에 노출된다. 검사 완료 후 숨김·착용 흐름도 아직 기준이 확정되지 않았다.

## 원인 후보

검사 애니메이션 대상이 에디터에서 활성 상태로 저장되어 있으며, 시작 시각물 가시성과 검사 상태의 소유자가 분리되어 있지 않다. 마스크 프록시는 애니메이션을 위해 활성 상태가 필요하지만, Renderer 노출 시점은 별도로 제어해야 한다.

## 현재 상태

마스크 프록시의 잘못된 패널·Grab·Marker 상호작용은 비활성화했으나, 초기 Renderer 노출 정책은 미완료다.

## 후속 검증

1. 씬 시작 시 프록시 Renderer가 숨겨져 있는지 확인한다.
2. 실제 `PPE_A_Mask`의 확인 버튼 입력 시에만 프록시를 표시하고 애니메이션을 시작한다.
3. 애니메이션 완료 후 얼굴 앵커 고정, 재검사, 씬 재시작 상태를 Quest/OpenXR에서 확인한다.

## 2026-08-17 추가: 시연용 프록시 비활성화 후 사용·폐기 차단

### 증상

시연을 위해 `PPE_A_Mask_Check`를 비활성화한 뒤, `PPE_A_Mask`에서 확인하기를 눌러도 사용과 폐기가 계속 거부된다.

### 근본 원인

`PPE_A_Mask`의 `PPEActionPanelController.inspectAnimation`이 비활성 오브젝트 `PPE_A_Mask_Check`의 `PPEMaskInspectionAnimation`을 그대로 가리키고 있다. 비활성 오브젝트의 컴포넌트 참조는 null이 아니므로 패널은 `TryPlay`를 호출하지만, 비활성 상태에서는 `StartCoroutine`이 코루틴을 시작하지 못한다. 그 결과 완료 콜백이 돌아오지 않아 `inspectCompletedForCurrentCondition`이 계속 false로 남고, `requireInspectBeforeUseOrDiscard: 1`과 겹쳐 사용·폐기가 영구 차단된다.

### 영향 범위

`3_PPE_Room_Train_Test_mask.unity`의 `PPE_A_Mask` 한 개다. 씬의 다른 PPE 액션 패널은 모두 `inspectAnimation`이 비어 있어 영향이 없다.

### 적용한 변경

`Assets/Editor/PPEActionPanelInspectAnimationRepair.cs`에 다음 메뉴를 추가했다. 씬 YAML을 직접 편집하면 Unity 메모리의 씬이 저장 시 덮어쓰므로 Editor 명령으로 처리한다.

- `Tools > PPE > Report Blocked Inspect Animation References` — 대상만 보고한다.
- `Tools > PPE > Clear Blocked Inspect Animation References` — 비활성 대상을 가리키는 참조를 비운다.
- `Tools > PPE > Assign Helmet Use SFX If Empty` — 아래 SFX 항목에 사용한다. 이미 작성된 id가 있으면 보존하고 보고만 한다.

### 커밋본 대비 PPE 아이템 배선 비교

`Tools/inspect_ppe_item_wiring.py`로 `HEAD`의 `3_PPE_Room_Train_Test.unity`와 현재 씬을 비교했다. 헬멧과 마스크의 잡기, 패널, 폐기 후 정상 모델 교체, 정오답 SFX, 거부 보이스는 다른 PPE 아이템과 동일하다. 차이는 세 가지였다.

| 항목 | 상태 | 조치 |
| --- | --- | --- |
| `PPE_A_Mask.inspectAnimation` | 비활성 대상을 가리키는 죽은 참조 (이번 씬에서만 발생) | 위 Clear 명령으로 제거 |
| `PPE_A_Helmet_Strap.useSfxId` | 비어 있음. 커밋본에서도 동일 | 기존 `harness` 재사용으로 결정 |
| `PPE_A_Mask.useSfxId` | 비어 있음. 커밋본에서도 동일 | 확인하기 호흡음으로 충분하다고 판단해 유지 |

`Assets/Audio/SFX`에는 헬멧·마스크 전용 착용음이 없다. 등록된 id는 `cloth`, `Boots`, `harness`, `Gloves`, `Taping`, `mask_breathing_right`, `mask_breathing_wrong`, `Correct Answer`, `Wrong Answer`뿐이다.

`PPE_A_Helmet_Strap.voiceFlowDirector`는 연결하지 않은 상태로 유지하기로 했다. 이 참조는 보이스 재생이 아니라 사용 승인 시 착용 순서 검사 여부를 결정하며, `PPE_A_Backplate`와 `PPE_A_SuitHang`도 동일하게 비어 있다.

### 검증 상태

- 정적 확인: `dotnet build Assembly-CSharp-Editor.csproj` 오류 0개. 씬 YAML 비교로 위 표의 차이를 확인했다.
- Unity Editor 확인: 미완료. 두 메뉴 실행과 씬 저장이 필요하다.
- Quest/OpenXR 확인: 미완료. 확인하기 후 사용·폐기 진행과 헬멧 착용음 재생을 헤드셋에서 확인해야 한다.
