# 한글 XRI 공간 키보드 패키지 제작 및 재사용 가이드

- 작성일: 2026-08-03
- 최종 갱신: 2026-08-25
- 기준 Unity: 6000.4.8f1
- 기준 XR Interaction Toolkit: 3.4.1
- 최종 패키지 형식: `Tyche_Modal_Hangul_Keyboard_Canvas_yyyyMMdd_HHmmss.unitypackage`
- 상태: 모달 Canvas 프리팹 생성·자동 검증·자체 포함 패키지 출력 완료, 빈 프로젝트 및 Quest 실기기 수동 검증 필요

## 1. 목적

Unity XR 환경에서 컨트롤러 또는 핸드 Poke로 누를 수 있는 한글 입력 UI와 월드 스페이스 모달 Canvas 전체를 다른 프로젝트에서도 재사용할 수 있도록 제작·검증·내보내는 방법을 정리한다.

이 패키지는 XR Interaction Toolkit의 Spatial Keyboard UI와 상호작용 구조를 사용하고, 프로젝트에서 별도로 작성한 두벌식 한글 조합기를 연결한다.

## 2. 키보드 사양

- 한글을 기본 입력 모드로 사용한다.
- `한/영` 키로 한글과 영문을 전환한다.
- 숫자 `1`~`0` 키를 최상단 행에 배치한다.
- `기호` 키로 기호 레이아웃을 열고 `문자` 키로 이전 언어 레이아웃에 복귀한다.
- Shift 입력으로 `ㅃ`, `ㅉ`, `ㄸ`, `ㄲ`, `ㅆ`, `ㅒ`, `ㅖ`를 지원한다. 작은 보조 글자를 병기하지 않고 기존 주 레이블 자체를 Shift 글자로 교체한다.
- 쌍자음, 복합 모음, 겹받침과 받침 뒤 모음 입력에 따른 음절 분리를 지원한다.
- Backspace는 완성된 음절을 즉시 모두 지우지 않고 현재 조합 상태를 단계별로 되돌린다.
- 키를 누를 때마다 `[Hangul Keyboard] Key pressed ...` 형식의 Console 로그를 출력한다.

두벌식에서 `ㅖ`는 Shift+P이다. 예를 들어 `dP`는 `예`, `rP`는 `계`로 조합된다.

## 3. 요구 환경

가져올 프로젝트에는 다음 패키지와 기능이 준비되어 있어야 한다.

- Unity 6 계열 권장
- XR Interaction Toolkit 3.4.1
- Input System 1.19.0
- TextMesh Pro 및 UGUI
- 실제 XR 실행 시 OpenXR와 대상 기기용 XR 설정

XR Interaction Toolkit의 **Spatial Keyboard 샘플 소스·프리팹·레이아웃·Sprite·Material은 이 `.unitypackage`에 포함**된다. 따라서 대상 프로젝트에서 Package Manager의 Samples 탭을 열어 Spatial Keyboard 샘플을 별도로 Import할 필요가 없다. 다만 XR Interaction Toolkit, Input System, UGUI/TMP 같은 Unity 기반 패키지는 대상 XR 프로젝트에 설치되어 있어야 한다.

런타임 어셈블리 `Prototype.Tyche.HangulKeyboard`는 다음 어셈블리를 직접 참조한다.

- `Unity.TextMeshPro`
- `Unity.XR.Interaction.Toolkit.Samples.SpatialKeyboard`

Spatial Keyboard 샘플 어셈블리 자체는 패키지에 같은 GUID로 포함된다. 대상 프로젝트에는 `com.unity.xr.interaction.toolkit` 3.4.1, `com.unity.inputsystem` 1.19.0, `com.unity.ugui` 2.0.0 이상을 준비한다.

## 4. 프로젝트 파일 구성

### 런타임 코드

`Assets/Scripts/HangulKeyboard`에 다음 코드가 있다.

- `HangulComposer.cs`: Unicode Hangul Syllable 조합식 기반 두벌식 조합기
- `HangulKeyboardController.cs`: XRI 키보드와 조합기 연결, 입력 모드 및 로그 관리
- `HangulCharacterKeyFunction.cs`: 한글 문자 키 입력
- `HangulBackspaceKeyFunction.cs`: 한글 조합 단계별 삭제
- `HangulLanguageToggleKeyFunction.cs`: 한글·영문 전환
- `HangulSymbolToggleKeyFunction.cs`: 문자·기호 전환
- `HangulShiftLegend.cs`: Shift 대상 한글 보조 레이블 표시
- `Prototype.Tyche.HangulKeyboard.asmdef`: 런타임 어셈블리 정의

### 프리팹과 설정 에셋

- 프리팹: `Assets/Prefabs/UI/Hangul Keyboard/Hangul Spatial Keyboard.prefab`
- 모달 Canvas 프리팹: `Assets/Prefabs/UI/Hangul Keyboard/Modal Hangul Keyboard Canvas.prefab`
- 한글 레이아웃: `Layouts/LayoutHangul.asset`
- 영문 레이아웃: `Layouts/LayoutEnglish.asset`
- 기호 레이아웃: `Layouts/LayoutSymbols.asset`
- 전용 Key Function 에셋: `Key Functions` 폴더
- 한글 폰트: `Assets/Font/Pretendard-Medium SDF.asset`

프리팹과 레이아웃에 저장된 Transform, 크기, 색상, 폰트와 키 배치를 UI 기준값으로 사용한다. 런타임 코드는 입력 상태를 처리하며 작성된 UI 배치 값을 임의로 덮어쓰지 않는다.

### Editor 도구

- `Assets/Editor/HangulSpatialKeyboardBuilder.cs`
- `Assets/Editor/HangulComposerValidationHarness.cs`
- `Assets/Editor/ModalHangulKeyboardPackageBuilder.cs`

Unity 메뉴 경로는 다음과 같다.

```text
Tools > XR > Hangul Keyboard
├─ Build Project Prefab
├─ Validate Project Prefab
├─ Apply Current Shift Presentation
├─ Apply Current Shift Input Mapping
├─ Create Test Instance In Current Scene
├─ Export Package To Desktop
└─ Modal Canvas
   ├─ Build Reusable Modal Canvas Prefab
   ├─ Validate Reusable Modal Canvas Prefab
   ├─ Create Modal Canvas In Current Scene
   └─ Export Self-Contained Package To Desktop
```

## 5. 패키지 제작 절차

### 5.1 프리팹 생성

1. Unity의 스크립트 컴파일과 에셋 임포트가 끝날 때까지 기다린다.
2. `Tools > XR > Hangul Keyboard > Build Project Prefab`을 실행한다.
3. `Assets/Prefabs/UI/Hangul Keyboard/Hangul Spatial Keyboard.prefab`이 생성되었는지 확인한다.
4. Console에서 조합기 및 생성 에셋 검증 통과 로그를 확인한다.

빌더는 XRI Spatial Keyboard 샘플 프리팹을 기반으로 프로젝트 전용 한글·영문·기호 레이아웃과 Key Function을 연결한다. 이미 생성된 프리팹의 Inspector 작성값은 보존하며, 현재 Shift 입력값과 주 레이블 표현에 필요한 항목만 명시적으로 마이그레이션한다.

### 5.2 모달 Canvas 전체 생성

`Tools > XR > Hangul Keyboard > Modal Canvas > Build Reusable Modal Canvas Prefab`을 실행한다.

- 기준 씬 `Assets/Scenes/3_PPE_Room_3mode_loco.unity`의 `Modal  Keyboard Canvas` 작성값을 최초 한 번 복제한다.
- 원본 씬과 원본 오브젝트는 수정하지 않는다.
- 이미 모달 프리팹이 있으면 덮어쓰지 않고 검증만 수행한다.
- `PPEVoiceKeyboardEventRelay`, `PPEControllerEducationEntry`처럼 특정 씬의 `PPE Voice Flow`를 참조하는 어댑터는 제거한다. 시각 요소는 유지되므로 대상 프로젝트의 제출·미니가이드 동작은 Inspector에서 연결한다.
- 생성 프리팹은 작성된 비활성 초기 상태, World Space Canvas, Overlay, 레이 블로커, XR Raycaster와 키보드 배치를 보존한다.

### 5.3 자동 검증

`Tools > XR > Hangul Keyboard > Validate Project Prefab`을 실행한다.

검증 항목은 다음과 같다.

- 한글 레이아웃이 기본 및 현재 레이아웃인지
- 한글·영문·기호 레이아웃 참조가 올바른지
- `한/영`, `기호/문자`, 전용 Backspace가 연결되었는지
- Shift 대상 보조 레이블이 모두 있는지
- Shift 입력값은 물리 QWERTY 대문자를 유지하고 주 레이블은 쌍자음·복합 모음으로 교체되는지
- 키 입력 Console 로그가 활성화되어 있는지
- 한글 조합과 단계별 Backspace가 정상 동작하는지

모달까지 포함한 검증은 `Modal Canvas > Validate Reusable Modal Canvas Prefab`을 사용한다. 누락 스크립트, World Space Canvas, `GraphicRaycaster`, `TrackedDeviceGraphicRaycaster`, `Keyboard Ray Blocker`, 외부 씬 전용 어댑터 제거 상태도 함께 확인한다.

### 5.4 현재 씬에 시험 배치

1. 시험할 씬을 연다.
2. Play Mode가 아닌 상태에서 `Create Test Instance In Current Scene`을 실행한다.
3. 생성된 `Hangul Spatial Keyboard Test`를 Scene 또는 Inspector에서 원하는 위치로 이동한다.
4. 씬을 저장하고 Play Mode에서 컨트롤러 또는 핸드 Poke 입력을 확인한다.

도구는 프리팹에 작성된 Transform을 임의로 변경하지 않는다. 월드 위치와 크기는 씬에 배치한 뒤 사용자가 결정한다.

### 5.5 바탕화면으로 내보내기

`Tools > XR > Hangul Keyboard > Modal Canvas > Export Self-Contained Package To Desktop`을 실행한다.

도구는 먼저 프리팹을 다시 생성·검증한 뒤 다음 형식으로 패키지를 만든다.

```text
Tyche_Modal_Hangul_Keyboard_Canvas_yyyyMMdd_HHmmss.unitypackage
```

내보내기는 `ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies`를 사용한다.

기본 내보내기 루트는 다음과 같다.

- `Assets/Scripts/HangulKeyboard`
- `Assets/Editor/HangulComposerValidationHarness.cs`
- `Assets/Editor/HangulSpatialKeyboardBuilder.cs`
- `Assets/Editor/ModalHangulKeyboardPackageBuilder.cs`
- `Assets/Prefabs/UI/Hangul Keyboard`
- `Assets/Font/Pretendard-Medium SDF.asset`
- `Assets/Samples/XR Interaction Toolkit/3.4.1/Spatial Keyboard`
- Spatial Keyboard 프리팹이 사용하는 Starter Assets 런타임 스크립트와 asmdef
- `Assets/Scripts/AuthoredWorldCanvasPose.cs`

Unity가 위 에셋에서 찾은 Sprite, Material, Mesh, Audio와 기타 참조 에셋도 의존성으로 함께 포함한다. Spatial Keyboard 샘플은 내보내기 루트에 명시되어 있어 별도 샘플 Import가 필요 없다. Package Manager의 Unity 기반 패키지 설치 상태 자체는 `.unitypackage`가 대신 설정하지 않는다.

## 6. 다른 프로젝트에서 사용하는 방법

1. 대상 프로젝트를 백업하거나 버전 관리 상태를 확인한다.
2. Package Manager에서 XR Interaction Toolkit 3.4.1, Input System, UGUI/TMP를 준비한다. Spatial Keyboard 샘플은 별도로 Import하지 않는다.
3. `Assets > Import Package > Custom Package`를 선택한다.
4. `Tyche_Modal_Hangul_Keyboard_Canvas_yyyyMMdd_HHmmss.unitypackage`를 선택한다.
5. 충돌하는 기존 에셋이 없는지 목록을 확인한 뒤 임포트한다.
6. 컴파일이 끝나면 `Tools > XR > Hangul Keyboard > Validate Project Prefab`을 실행한다.
7. `Modal Hangul Keyboard Canvas.prefab`을 배치하거나 `Create Modal Canvas In Current Scene` 메뉴를 실행한다.
8. 모달 활성화, 제출 완료, 미니가이드 같은 프로젝트별 이벤트를 Inspector에서 연결하고 실제 입력을 시험한다.

같은 GUID의 이전 버전을 이미 가져온 프로젝트에서는 덮어쓰기 전 변경 사항을 확인한다. 대상 프로젝트가 자체 수정한 한글 키보드 파일을 가지고 있다면 먼저 별도 백업하거나 브랜치를 만든다.

## 7. 최종 출력물

- 위치: 현재 Windows 계정의 바탕화면
- 파일명 형식: `Tyche_Modal_Hangul_Keyboard_Canvas_yyyyMMdd_HHmmss.unitypackage`
- 용도: 모달 Canvas, 한글 키보드, 변경된 Shift 주 레이블 표현과 Spatial Keyboard 샘플 의존성을 포함한 재사용본

바탕화면의 `.unitypackage`는 Git 저장소 안의 파일이 아니므로 프로젝트를 다른 PC로 옮길 때 별도로 보관해야 한다.

## 8. 근본 원인과 해결 방식

### XRI 키보드만으로 한글이 조합되지 않는 원인

XRI Spatial Keyboard는 키 상호작용과 문자열 전달을 제공하지만 `ㅎ`·`ㅏ`·`ㄴ`을 `한`으로 조합하는 한국어 IME 상태를 제공하지 않는다.

프로젝트 소유 `HangulComposer`가 현재 한글 입력 구간을 관리하고 Unicode 완성형 음절로 변환한 뒤 XRI 키보드의 텍스트에 반영하도록 해결했다.

### 프리팹만 복사할 때 기능이 누락되는 원인

프리팹은 레이아웃, ScriptableObject Key Function, 폰트, 런타임 어셈블리와 XRI 샘플을 참조한다. 프리팹 파일 하나만 복사하면 해당 참조가 빠질 수 있다.

전용 내보내기 메뉴가 프로젝트 소유 파일과 Unity가 계산한 의존성을 함께 패키지화하도록 구성했다.

## 9. 영향 범위

- 월드 스페이스 XR 문자 입력 UI
- 한글·영문·기호·숫자·Shift·Backspace 입력
- XRI 컨트롤러 및 핸드 Poke 상호작용
- TextMesh Pro 입력 필드와 한글 폰트
- 키 입력 Console 진단 로그
- 프리팹 생성, 검증, 시험 배치와 `.unitypackage` 내보내기

기존 핸드 모델, Poke 포즈 Animator 또는 `Poke_Box` 판정 코드는 이 키보드 패키지가 직접 수정하지 않는다. 키보드 입력 방식으로 Hand Poke를 사용할 경우 해당 씬의 XRI Poke Interactor 구성이 별도로 정상이어야 한다.

### 현재 PPE 씬에서 삭제할 때의 주의

`Assets/Scenes/3_PPE_Room_3mode_loco.unity`에서는 `AudioManager/PPE Voice Flow.m_KeyboardPresentationRoot`가 `Modal  Keyboard Canvas`를 참조하고 `NameInput` 상태에서 활성화한다. 패키지와 재사용 프리팹을 만들었다는 사실만으로 현재 씬 오브젝트를 바로 삭제하면 이름 입력 상태가 끊긴다.

기업 또는 Meta 계정 로그인으로 교체할 때 `FlowState.NameInput`, `m_KeyboardPresentationRoot`, 이름 제출 릴레이와 미니 컨트롤러 가이드 진입 위치를 함께 이전한 뒤 삭제한다. 그 전에는 작성된 비활성 상태로 유지한다.

## 10. 완료한 검증

- `한글`, `안녕하세요`, `예`, `계`, `얘`, `까`, `와`, `워`, `가가`, `값`, `갑사` 조합 검증을 통과했다.
- `한 → 하 → ㅎ → 빈 문자열` 단계별 Backspace 검증을 통과했다.
- 생성 프리팹의 기본 한글 모드, 레이아웃 전환, Backspace, Shift 레이블과 Console 로그 활성화를 확인했다.
- 모달 Canvas의 World Space 설정, XR/UI Raycaster, 레이 블로커, Overlay, 한글 키보드와 TMP 입력 필드를 확인했다.
- 모달 프리팹에서 특정 PPE 씬의 외부 참조를 제거하고 누락 스크립트가 없음을 확인했다.
- Spatial Keyboard 샘플 소스와 필요한 Starter Assets 스크립트를 패키지 내보내기 루트로 검증했다.

## 2026-08-16 Shift 레이블 UX 적용 결과

- 기존 `HangulShiftLegend`의 키 위쪽 보조 레이블은 비활성화했다.
- Shift 상태에서는 기존 키의 주 레이블을 Shift 문자로 직접 교체한다.
- 예시는 `ㄱ → ㄲ`, `ㅂ → ㅃ`, `ㅈ → ㅉ`, `ㄷ → ㄸ`, `ㅅ → ㅆ`, `ㅐ → ㅒ`,
  `ㅔ → ㅖ`이며 입력 매핑 자체는 변경하지 않는다.
- 입력값은 조합기에 필요한 물리 QWERTY 대문자(`R`, `Q`, `W` 등)를 유지하고, 표시값만 `ㄲ`, `ㅃ`, `ㅉ` 등으로 분리했다.
- 이 표현은 키보드 프리팹과 모달 Canvas 프리팹에 적용했으며 자동 검증 대상이다.
- Unity Editor에서 스크립트 어셈블리가 오류 없이 다시 로드되었다.
- 최종 패키지에 런타임 코드, Editor 도구, 프리팹, 레이아웃, 폰트와 관련 의존성이 포함되었음을 확인했다.
- 임시 내보내기 스크립트와 요청 마커가 최종 패키지에 포함되지 않았음을 확인했다.

## 11. 남은 수동 검증

- Quest/OpenXR에서 컨트롤러와 양손 Poke로 모든 키가 한 번씩 입력되는지 확인한다.
- `안녕하세요`, `예`, `계`와 겹받침이 포함된 문장을 실제 TMP 입력 필드에 입력한다.
- 입력 도중 커서를 이동하거나 영문·기호 모드로 전환했을 때 조합 중인 문자열이 안정적으로 확정되는지 확인한다.
- Console 로그가 한 번의 키 입력당 한 번만 기록되는지 확인한다.
- Quest에서 키 레이블 가독성, Collider 누름 범위, 양쪽 눈 렌더링을 확인한다.
- 빈 Unity 테스트 프로젝트에 최종 패키지를 가져와 누락된 패키지·에셋·GUID 충돌이 없는지 확인한다.

## 12. 외부 코드 및 라이선스 판단

검토한 `YeongJoo-Kim/UnityHangulKeybord` 저장소는 참고 대상으로만 확인했다. 라이선스가 명시되지 않은 코드를 복사하거나 최종 패키지에 포함하지 않았으며, 한글 조합 코드는 Unicode Hangul Syllable 공식을 바탕으로 프로젝트에서 별도로 작성했다.

## 13. 관련 기록

- `Docs/MeetingNotes/2026-07-31_Unity_XR_Meeting.md`
- 문서 내 `2026-08-03 후속 작업 — 한글 XRI 공간 키보드와 재사용 패키지` 절
