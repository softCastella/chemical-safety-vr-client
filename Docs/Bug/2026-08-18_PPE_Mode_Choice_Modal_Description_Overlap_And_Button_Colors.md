# PPE 모드 선택 모달의 설명 중복 노출과 버튼 색상 불일치

Date: 2026-08-18

대상 씬: `Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`

## 증상

- 교육모드·학습모드·테스트모드 버튼이 나올 때 왼쪽 설명 영역에 텍스트 두 개가 동시에 겹쳐 보인다.
- "PPE 착용교육" 버튼은 연회색인데, 그 다음 단계의 모드 버튼들은 청록·보라라서 한 모달 안에서 색이 튄다.

## 원인

### 1. 이전 단계의 시나리오 설명이 꺼지지 않음

`ScenarioDetailModal`의 왼쪽 설명 영역에는 두 오브젝트가 같은 자리를 공유한다.

- `1_ senario`: `scenarioDescriptionObjects[0]`. `Show()`가 `ShowScenarioContent()`를 통해 켠다.
- `2_mode`: `ppeModeDescriptionRoot`. `ShowPpeModeChoices()`가 켠다.

`ShowPpeModeChoices()`는 `educationChoiceRoot`를 끄고 `ppeModeDescriptionRoot`와 `ppeModeChoiceRoot`를 켜지만, 이전 단계에서 켜둔 `scenarioDescriptionObjects`를 끄지 않았다. 그래서 시나리오 설명 위에 모드 설명이 그대로 겹쳐 그려졌다.

### 2. 버튼 색은 Button의 ColorBlock이 아니라 Image 작성값 차이

다섯 버튼의 `m_Colors`(ColorBlock)는 모두 동일하다. 실제 차이는 각 버튼 GameObject의 `Image.m_Color`에 있었다.

| 버튼 | Image `m_Color` |
| --- | --- |
| `PPE Education Scenario Button` (기준) | `0.43, 0.46, 0.48` |
| `Mode Choice Back` | `0.43, 0.46, 0.48` |
| `PPE Edu Mode` | `0.25, 0.42, 0.52` |
| `PPE Training Mode` | `0.42, 0.30, 0.48` |
| `PPE Test Mode` | `0.42, 0.30, 0.48` |

`ScenarioDetailModal`에는 색상을 대입하는 런타임 코드가 없다. 따라서 씬 작성값이 유일한 출처이며, 수정도 씬에서 이루어져야 한다.

## 적용한 변경

### 코드

- `Assets/Scripts/ScenarioDetailModal.cs`
  - `ShowPpeModeChoices()`에서 `SetOnlyActive(scenarioDescriptionObjects, -1)`로 이전 단계 설명을 끈다. `ShowPpeModeChoicesAfterCompletion()`도 이 메서드를 거치므로 완료 복귀 경로에 함께 적용된다.
  - `ReturnToPpeEducationChoices()`에서 `SetOnlyActive(scenarioDescriptionObjects, selectedScenario)`로 되돌린다. 완료 복귀 후에는 `selectedScenario`가 `-1`이라 설명이 꺼진 상태로 남으며, 이는 이전 시나리오 설명이 잔상으로 남는 것보다 의도에 맞다.
  - 색상, 위치, 크기 등 표현값은 코드에 넣지 않았다.

### 씬

`Assets/Scenes/3_PPE_Room_Train_Test_mask.unity`에서 세 Image의 `m_Color`를 기준 버튼 값 `0.43, 0.46, 0.48`로 직접 수정했다. 값만 바꿨고 FileID, GUID, 계층, 컴포넌트 구성은 건드리지 않았다.

| Image fileID | 대상 | 변경 |
| --- | --- | --- |
| `890381145` | `PPE Edu Mode` | `0.25, 0.42, 0.52` → `0.43, 0.46, 0.48` |
| `1318549812` | `PPE Training Mode` | `0.42, 0.30, 0.48` → `0.43, 0.46, 0.48` |
| `1567379218` | `PPE Test Mode` | `0.42, 0.30, 0.48` → `0.43, 0.46, 0.48` |

Button의 ColorBlock(hover, press, disabled)은 다섯 버튼이 이미 동일해서 건드리지 않았다.

### Editor 도구

- `Assets/Editor/PPEModalButtonColorUnification.cs` (신규, 선택 사항)
  - `Tools > PPE > Report Modal Button Colors`: 기준 버튼과 각 모드 버튼의 현재 색을 출력한다.
  - `Tools > PPE > Unify PPE Mode Button Colors`: `incompletePpeButton`의 target graphic 색을 읽어 모드 버튼들에 적용한다. 기준색을 상수로 박지 않고 씬에서 읽으므로, 나중에 기준 버튼 색을 바꾸면 재실행만으로 팔레트를 맞출 수 있다.
  - 이번 변경은 씬을 직접 수정해 적용했으므로 이 도구를 실행할 필요는 없다.

### 도구 임포트 실패 기록

Unity가 열려 있는 상태에서 외부 편집한 두 Editor 스크립트가 다음 오류로 임포트에 실패해 메뉴가 등록되지 않았다. 코드 오류가 아니라 AssetDatabase의 수정 시각 불일치다.

```
ERROR: Build asset version error: assets/editor/ppemodalbuttoncolorunification.cs
  in SourceAssetDB has modification time of '2026-08-17T16:33:17Z'
  while content on disk has modification time of '2026-08-17T16:58:43Z'
Import Error Code:(4)
```

`Assets/Editor` 폴더를 Reimport하면 해소된다. 이후 Unity가 열린 상태에서 Editor 스크립트를 외부 편집할 때는 임포트 성공 여부를 Editor.log에서 확인한다.

## 영향 범위

- `standardTrainingButton`(일반 안전교육)은 사용자가 지적하지 않아 변경 대상에서 제외했다. 보고 메뉴에는 나오지 않으므로 필요하면 별도로 지정해야 한다.
- 씬에는 `ScenarioDetailModal`이 두 개 있고, 두 번째 인스턴스는 PPE 모드 버튼 참조가 모두 비어 있다. 도구는 참조가 채워진 모달만 처리한다.

## 완료한 검증

- 정적 확인: `dotnet build Assembly-CSharp.csproj`, `dotnet build Assembly-CSharp-Editor.csproj` 모두 오류 0개.
- 정적 확인: 씬 YAML에서 다섯 버튼의 ColorBlock이 동일하고 Image 색만 다르다는 점, `1_ senario`와 `2_mode`가 각각 `scenarioDescriptionObjects[0]`과 `ppeModeDescriptionRoot`에 연결된 점을 대조했다.
- 정적 확인: 씬 수정 후 `git diff`에서 바뀐 `m_Color` 줄이 위 표의 세 개뿐임을 확인했다. 치환 전 `0.25, 0.42, 0.52`는 파일 전체에서 1회, `0.42, 0.3, 0.48`은 대상 두 버튼에서만 2회 나타나 오치환 위험이 없었다.

## 아직 필요한 검증

- [ ] Unity에서 씬을 다시 열어(File > Open Scene) 디스크의 색상 변경을 메모리에 반영한다. 저장하지 않은 변경이 있으면 먼저 처리한다.
- [ ] Inspector에서 세 모드 버튼의 Image 색이 `0.43, 0.46, 0.48`인지 확인한다.
- [ ] Play Mode에서 PPE 착용교육 → 모드 선택으로 넘어갈 때 왼쪽 설명이 `2_mode` 하나만 보이는지 확인한다.
- [ ] 모드 선택에서 뒤로가기를 눌렀을 때 `1_ senario` 설명이 다시 보이는지 확인한다.
- [ ] 교육 완료 후 모드 선택으로 복귀했을 때도 설명이 겹치지 않는지 확인한다.
- [ ] Quest/OpenXR 양안에서 네 버튼의 색이 동일하게 보이는지 확인한다.
