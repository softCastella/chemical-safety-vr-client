# 2026-08-06 PPE 키보드 Enter 후 Ray Fall-through로 Quit 호출

## 개요

| 항목 | 내용 |
| --- | --- |
| 상태 | 수정 적용, Unity/Quest 재현 검증 대기 |
| 대상 씬 | `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` |
| 대상 UI | `Modal Keyboard Canvas` 및 Quit Button |
| 심각도 | 높음 — Enter 입력이 Play Mode/헤드셋 세션 종료로 이어짐 |

## 기대 동작

키보드 Enter는 텍스트를 제출하고 `Modal Keyboard Canvas`만 비활성화한다. BGM과 씬은 유지되고 Quit Button은 호출되지 않아야 한다.

## 재현 증상·근거

1. 키보드에서 Enter를 입력한다.
2. 키보드 모달이 닫히는 순간 Play Mode가 종료되고, 헤드셋은 흰 공간으로 전환된다.
3. 재현 로그에 `[Quit Button] HandleQuit invoked`가 남는다.

사용자 관찰에서 모달이 꺼진 직후 레이가 뒤쪽으로 넘어갔다. 따라서 이 문서는 종료 버튼의 화면상 위치 추측이 아니라 실제 `HandleQuit` 호출 로그와 입력 순서를 근거로 한다.

## 근본 원인

`HangulKeyboardController.OnTextSubmitted`가 Enter 입력을 처리하며 `Modal Keyboard Canvas`를 비활성화했다. 같은 XR UI 입력 처리 중 앞쪽 Graphic이 사라지면서 레이가 뒤쪽 Quit Button까지 도달했고, `QuitApplicationButton.HandleQuit()`이 호출되었다. Editor에서는 `EditorApplication.isPlaying = false`, 빌드에서는 `Application.Quit()`이 실행되는 경로다.

## 적용한 변경

| 경로 | 변경 |
| --- | --- |
| `Assets/Scripts/HangulKeyboard/HangulKeyboardController.cs` | Inspector 직렬화 제출 대상과 활성화 플래그를 사용하여 Enter 제출 후 `Modal Keyboard Canvas`만 비활성화. |
| `Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity` | 제출 대상 참조를 `Modal Keyboard Canvas` 루트에 연결하고 비활성화를 사용하도록 작성. |
| `Assets/Scripts/HangulKeyboard/HangulKeyboardSubmitGuard.cs` | Enter 제출 직후 2프레임의 공용 Quit 억제 상태 제공. |
| `Assets/Scripts/QuitApplicationButton.cs` | Guard가 활성인 호출은 종료하지 않고 무시 로그를 남김. |
| `Assets/Editor/HangulKeyboardSubmitSafetyHarness.cs` | 제출 설정·모달 참조·제출 비활성화·Quit 코드 경로의 정적 점검 메뉴 제공. |

UI 레이아웃·Transform·Canvas 표현값은 런타임 코드가 변경하지 않는다.

## 영향 범위

- Enter 제출로 닫히는 키보드 모달과, 그 뒤쪽에 XR UI Ray로 선택 가능한 Quit Button이 함께 존재하는 구성.
- 일반 Quit Button 동작은 Guard가 아닌 입력에서 유지되어야 한다.
- BGM, 텔레포트, PPE Grab 로직은 이 변경의 직접 수정 범위에 포함하지 않는다.

## 검증

### 완료한 정적 확인

- `Modal Keyboard Canvas`가 제출 시 비활성화 대상임을 확인했다.
- 제출 후 Guard를 설정하고 Quit 처리에서 Guard를 확인하는 경로를 확인했다.
- `Tools > XR > Hangul Keyboard > Validate Submit Safety` 하네스로 관련 씬·소스의 정적 구성을 점검할 수 있게 했다.

### 필요한 Unity Editor·Quest/OpenXR 수동 검증

1. 콘솔 오류가 없는 상태에서 `_scale_0` 씬을 Play 한다.
2. 키보드 Enter를 한 번 입력한다.
3. `[Hangul Keyboard] Text submitted...` 다음에 Quit 경로가 시도되면 `[Quit Button] Ignored because keyboard Enter submission is closing its modal.`가 기록되는지 확인한다.
4. `Modal Keyboard Canvas`만 비활성화되고 Play Mode, BGM, 헤드셋 세션이 유지되는지 확인한다.
5. Guard 기간이 지난 뒤 Quit Button을 직접 눌러 기존 종료 동작도 확인한다.

## 잔여 위험

- 실제 이벤트 순서가 Quit Button 호출이 제출 콜백보다 앞서는 구성에서는 2프레임 Guard가 충분하지 않을 수 있다. 이 경우 새 fallback을 추가하지 말고, `PointerDown`/`PointerUp`/`PointerClick`과 `OnTextSubmitted`의 로그 순서를 먼저 수집해 입력 해제 시점 기반 차단으로 재설계한다.
- Game View 마우스 결과는 Quest Trigger 입력 성공의 증거가 아니다. 최종 판정은 Quest/OpenXR 헤드셋에서 한다.
