# 2026-07-31 Unity XR 작업 회의록

## 오늘 확인·수정한 항목

- `5_MixerRoom_Unlit`의 Floor에 색상/밝기 조절용 Inspector 설정을 유지하도록 정리했다.
- Mixer Room 포스트 프로세싱과 정면 캡처용 에디터 메뉴를 확인했다.
- 컨트롤러 손 모델 4종에 Animator/좌우 손 포즈 클립 연결 경로를 보강했다.
- 손가락 본 캐시가 일부 모델과 맞지 않아도 Animator의 `Grip` 파라미터가 입력을 받도록 연결 로직을 보강했다.
- 컨트롤러에서 손 애니메이션이 나오지 않는 문제를 점검하고, Animator Controller·Grip 파라미터·아날로그 입력 fallback을 확인했다.
- PPE 손 모델의 방호복 색상과 손목 검정 고무줄 표현 문제를 점검했다.
- `traffic_cone_3d_model (1)` 밝기 조정 요청과 기존 오브젝트 위치 덮어쓰기 문제를 확인했다.
- OpenXR Meta Quest Pro/Plus controller interaction profile은 컨트롤러/핸드 입력 구성에 따라 필요 여부를 구분해야 함을 확인했다.
- Scene View 기즈모 표시 위치와 Overlay 메뉴 접근 방법을 안내했다.
- FBX 노멀맵/AO 제거 테스트를 위해 가역 스냅샷 에디터 도구와 사용 문서를 추가했다. 실제 테스트 실행은 아직 하지 않았다.

## 주요 문서/도구

- `Assets/Editor/FbxNormalOcclusionTest.cs`
- `Docs/FbxNormalOcclusionTest.md`
- `Docs/Bug/2026-07-31_FBX_Normal_Occlusion_Test_Scope.md`

## 다음 확인

- Unity 컴파일 완료 후 필요한 메뉴 작업을 사용자가 승인하고 실행한다.
- 손 애니메이션은 실제 컨트롤러 입력과 Quest/OpenXR 양쪽 눈에서 확인한다.
- FBX 테스트는 스냅샷을 삭제하지 않은 상태에서 품질/성능 비교 후 Restore한다.

## PPE 손 모델 복구 추가 기록

- GitHub 기준으로 `3_PPE_Room` 씬을 복구한 뒤 Material → Transform PPtr 캐스트 오류를 조사했다.
- 손 스키닝 렌더러 14개의 `m_Bones` 배열에 잘못 들어간 Material GUID를 `3_PPE_Room_Loco.unity` 기준 본 Transform으로 복구했다.
- 방호복 소매는 따뜻한 노랑, 손목 고무줄은 검정으로 재적용했다.
- Unity 재실행 후 해당 PPtr 캐스트 오류가 재발하지 않음을 확인했다.
- 복구 전 씬 백업: `Backups/2026-07-31_3_PPE_Room_before_hand_restore_181523`

상세 내용은 이 문서에 통합했으며, 별도 중복 문서는 정리한다.

---

## 2026-08-03 후속 작업 — 컨트롤러 핸드 Poke 포즈와 Poke_Box 테스트

### 작업 목적

- 대상 씬: `Assets/Scenes/3_PPE_Room_HandTest_scale.unity`
- 선생님의 XR 컨트롤러·핸드 애니메이션 시스템은 유지하고, 프로젝트의 자체 핸드 모델 4종을 사용한다.
- 핸드 트래킹으로 구운 좌·우 Poke 클립을 컨트롤러 핸드에 연결한다.
- 컨트롤러 버튼을 누르지 않고 손가락 끝이 표면에 접근하면 Poke 포즈가 자동 재생되는 `Poke_Box`를 시험한다.
- 첨부 자료 `핸드_애니메이션_정리.pdf`의 `Initialize XR Origin`, `Build Controller Hands`, 포즈 녹화 및 Controller Grab Pose 구성 흐름을 참고했다.

### 검토 결론

- 핸드 트래킹으로 녹화했는지는 재생 가능 여부를 결정하지 않는다. 애니메이션 클립의 본 경로와 컨트롤러용 핸드 모델의 Animator 아래 본 경로가 일치하면 같은 클립을 재생할 수 있다.
- 좌·우 자체 핸드 모델 4종은 각 손 안에서 동일한 26개 본 경로와 바인드 포즈를 공유한다.
- `Assets/HandPoses/HandPose_Poke_L.anim`과 `HandPose_Poke_R.anim`은 각각 `L_Wrist/...`, `R_Wrist/...` 아래의 손가락 본 19개를 사용하는 1프레임 포즈 클립이며 현재 핸드 리그와 호환된다.
- Poke는 버튼 입력이 아니라 검지 끝과 표면 사이의 거리로 구동한다. 표면 가까이 들어가면 Poke가 켜지고, 약간 더 멀어져야 해제되는 히스테리시스를 적용해 경계에서 포즈가 떨리지 않게 했다.

### 적용한 변경

#### 1. Animator 구성

- 다음 Animator Controller에 `Poke` float 파라미터와 중첩 Blend Tree를 추가했다.
  - `Assets/HandPoses/HandAnimator_L.controller`
  - `Assets/HandPoses/HandAnimator_R.controller`
- 최종 구조는 다음과 같다.

```text
Hand Pose Blend (Poke)
├─ Poke 0: Grip Blend (Grip)
│  ├─ Grip 0: HandPose_Open
│  └─ Grip 1: HandPose_Grip
└─ Poke 1: HandPose_Poke
```

- 기존 `Grip` 동작은 유지되며 `Poke`가 1에 가까워질수록 Poke 포즈가 우선한다.
- `Assets/Editor/HandAnimatorBuilder.cs`가 Open·Grip·Poke 클립을 로드해 위 구조를 반복 실행해도 안전하게 갱신하도록 보강했다.

#### 2. 런타임 Poke 판정

- `Assets/Scripts/HandGripAnimator.cs`
  - 기존 Grip 입력 처리를 유지했다.
  - 좌·우 손 구분, 검지 끝 Transform, 활성 Animator 목록을 제공하도록 확장했다.
  - 여러 Poke 표면이 독립적으로 포즈를 요청할 수 있게 Poke 요청 소스를 관리한다.
  - `Poke` 값을 부드럽게 보간하고, 파라미터나 검지 끝 본을 찾지 못하면 경고를 출력한다.
- `Assets/Scripts/PokeHandPoseSurface.cs`
  - `L_IndexTip` 또는 `R_IndexTip`과 표면 Collider 사이의 최단 거리를 검사한다.
  - 진입 거리 `0.02m`, 해제 거리 `0.035m`를 사용한다.
  - 씬 로드 후 이름이 `Poke_Box`인 오브젝트를 찾아 시험 구성을 자동 보완한다.
  - 기존 Rigidbody는 중력 해제·Kinematic으로 전환하고 속도를 0으로 만들며, `HandedGrabInteractable`은 비활성화한다.
  - 표면 컴포넌트가 없으면 런타임에 추가하고 `[HandPose] Runtime Poke_Box ready ...` 로그를 남긴다.

#### 3. 고정형 Poke_Box 제작 지원

- `Assets/Prefabs/Poke_Box.prefab`
  - Rigidbody 없이 Collider와 `PokeHandPoseSurface`를 가진 고정형 시험 프리팹을 추가했다.
- `Assets/Editor/PokeBoxTestSetup.cs`
  - `Tools > XR > Setup Poke Box Test` 메뉴를 추가했다.
  - 씬의 기존 `Poke_Box`를 고정형 프리팹으로 교체하면서 사용자가 설정한 local Position·Rotation·Scale을 보존한다.
- 현재 런타임 자동 보완은 테스트 실패를 막기 위한 안전망이다. 최종 씬 구성은 Play Mode를 끈 상태에서 위 메뉴로 교체한 뒤 씬을 저장하여, 씬·프리팹의 직렬화 값이 기준이 되게 하는 것이 권장된다.

### 근본 원인

#### 손이 닿아도 Poke가 나오지 않은 원인

- Animator Controller에 Poke 클립만 연결해도 표면 접근을 감지해 `Poke` 파라미터를 올려 주는 판정 로직은 자동으로 생기지 않는다.
- 저장된 씬의 `Poke_Box`는 기존 `Assets/Prefabs/Box.prefab` 인스턴스였고 `PokeHandPoseSurface`가 직렬화되어 있지 않았다.
- 따라서 검지 끝이 닿아도 Animator에 Poke 요청이 전달되지 않았다. 현재는 런타임 자동 보완과 고정형 프리팹 교체 도구를 함께 제공한다.

#### Poke_Box가 Play Mode에서 내려간 원인

- 기존 Box 프리팹은 중력이 켜진 Dynamic Rigidbody와 잡기 상호작용을 포함한다.
- Transform에 입력한 위치가 코드로 단순 덮어써진 것이 아니라, Play Mode의 물리 중력 또는 충돌로 Rigidbody가 이동한 현상이다.
- 중력만 꺼도 Dynamic Rigidbody는 충돌에 의해 이동할 수 있으므로 고정 시험 표면은 Rigidbody를 제거하거나 Kinematic으로 두어야 한다.

#### Android 1920×1080 설정인데 Windows처럼 보인 이유

- `androidDefaultWindowWidth: 1920`, `androidDefaultWindowHeight: 1080`은 Android 창의 기본 크기 설정이며 활성 빌드 플랫폼을 선택하는 값이 아니다.
- Unity Editor 자체는 Windows에서 실행되고, Quest Link Play Mode도 Windows/Standalone OpenXR 경로를 사용하므로 에디터에서 Windows 동작처럼 보일 수 있다.
- 확인 당시 활성 Build Target/Group은 Android였으며 리임포트는 플랫폼 전환용 에셋 재처리 과정이었다.
- Quest 헤드셋의 실제 눈별 렌더 해상도는 일반 창의 1920×1080 값이 아니라 OpenXR과 XR Eye Texture 설정의 영향을 받는다.

### 영향 범위

- 좌·우 컨트롤러 핸드의 Open, Grip, Poke 포즈 전환
- 동일한 좌·우 리그를 공유하는 자체 핸드 모델 4종
- 이름이 정확히 `Poke_Box`인 테스트 오브젝트의 런타임 물리·잡기 설정
- `3_PPE_Room_HandTest_scale` 씬의 Poke 표면 시험
- Android/Quest 빌드와 Windows Quest Link Play Mode를 구분하는 확인 절차

### 완료한 검증

- 좌·우 Poke 클립의 GUID, 본 경로 및 핸드 리그 호환성을 확인했다.
- Animator 모델 미리보기에서 사용자가 Poke 손가락 움직임을 확인했다.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 결과 컴파일 오류 0건을 확인했다.
- 새 코드에서 발생한 Unity 6 API 사용 중단 경고는 제거했으며, 남은 경고 7건은 기존 코드의 경고다.
- 프로젝트 설정과 Editor 로그에서 활성 빌드 대상이 Android이고 리임포트가 진행 중임을 확인했다.

### 남은 수동 검증

- 리임포트와 스크립트 컴파일이 모두 끝난 뒤에만 Play Mode에 진입한다. OpenXR 로더가 불안정해질 수 있으므로 컴파일 중 Quest Link Play Mode는 시작하지 않는다.
- Play Mode에서 `[HandPose] Runtime Poke_Box ready ...` 로그가 한 번 출력되는지 확인한다.
- 좌·우 검지 끝을 `Poke_Box` 표면에 접근시켜 버튼 입력 없이 Poke 포즈가 재생되고, 멀어지면 Open/Grip 상태로 복귀하는지 확인한다.
- `Poke_Box`가 중력이나 손 충돌로 이동하지 않는지 확인한다.
- 테스트가 통과하면 Play Mode를 종료하고 `Tools > XR > Setup Poke Box Test`를 실행해 고정형 프리팹을 씬에 직렬화한 뒤 저장한다.
- Build Profiles 창에서 리임포트 완료 후 Android 프로필의 `Active` 상태를 다시 확인하고, Quest 기기 빌드에서 양손과 양쪽 눈을 최종 확인한다.

---

## 2026-08-03 후속 작업 — 한글 XRI 공간 키보드와 재사용 패키지

### 작업 목적

- Unity XR 환경에서 컨트롤러나 손으로 직접 누를 수 있는 입력 UI와 한글 조합 기능을 함께 제공한다.
- 한글을 기본 입력 모드로 사용하고, `한/영` 키로 영문을 전환하며 숫자 키는 최상단 행에 유지한다.
- `ㅖ`처럼 Shift가 필요한 모음, 쌍자음, 복합 모음과 겹받침을 포함하는 두벌식 입력을 지원한다.
- 우선 각 키 입력 결과를 Console 로그로 확인할 수 있게 하고, 완성된 키보드를 다른 Unity 프로젝트에서도 재사용할 수 있도록 `.unitypackage`로 내보낸다.

### 검토 결론과 설계 판단

- XR Interaction Toolkit 3.4.1의 Spatial Keyboard 샘플은 XR로 누를 수 있는 키·레이아웃·입력 UI의 기반을 제공하지만, 한글 음절 조합기는 제공하지 않는다.
- 따라서 기존 XRI Spatial Keyboard의 상호작용과 UI 구조를 유지하고, 프로젝트 소유의 두벌식 조합기와 키 기능을 연결하는 방식으로 구현했다.
- 검토한 외부 저장소 `YeongJoo-Kim/UnityHangulKeybord`의 코드는 라이선스가 명시되지 않은 상태였으므로 코드를 복사하거나 패키지에 포함하지 않았다. 한글 조합 로직은 Unicode Hangul Syllable 조합식을 사용해 별도로 작성했다.
- UI의 위치·크기·색상·폰트·키 배치는 생성된 프리팹과 직렬화된 레이아웃 에셋을 기준으로 한다. 런타임 코드는 입력 모드와 텍스트 상태만 변경하며 프리팹의 Transform이나 표시 값을 임의로 덮어쓰지 않는다.

### 적용한 변경

#### 1. 두벌식 한글 조합

- `Assets/Scripts/HangulKeyboard/HangulComposer.cs`
  - 물리 QWERTY 키를 두벌식 자모로 변환하고 초성·중성·종성을 Unicode 완성형 음절로 조합한다.
  - 쌍자음, 복합 모음, 겹받침, 받침 뒤 모음 입력에 따른 음절 분리를 처리한다.
  - Backspace를 한 번 누를 때마다 겹받침 → 받침 → 복합 모음 → 기본 자모 순서로 조합 상태를 단계적으로 되돌릴 수 있게 했다.
  - Shift 입력 `P`를 `ㅖ`로 처리하므로 `dP`는 `예`, `rP`는 `계`로 조합된다.
- `Assets/Scripts/HangulKeyboard/HangulKeyboardController.cs`
  - XRI `XRKeyboard`와 한글 조합기를 연결한다.
  - 키보드를 열 때 한글 모드로 초기화하고 `한/영`, `기호`, Shift, Backspace 동작을 관리한다.
  - 키를 누를 때 `[Hangul Keyboard] Key pressed ...` 형식으로 레이블, 입력값, 현재 모드와 결과 텍스트를 Console에 기록한다.
- XRI 키 동작과 Shift 보조 표시를 위해 다음 프로젝트 소유 컴포넌트를 추가했다.
  - `HangulCharacterKeyFunction.cs`
  - `HangulBackspaceKeyFunction.cs`
  - `HangulLanguageToggleKeyFunction.cs`
  - `HangulSymbolToggleKeyFunction.cs`
  - `HangulShiftLegend.cs`
  - `Prototype.Tyche.HangulKeyboard.asmdef`

#### 2. 키보드 UI와 레이아웃

- 생성 프리팹: `Assets/Prefabs/UI/Hangul Keyboard/Hangul Spatial Keyboard.prefab`
- 한글·영문·기호 레이아웃을 각각 직렬화된 `XRKeyboardConfig` 에셋으로 구성했다.
  - `Layouts/LayoutHangul.asset`
  - `Layouts/LayoutEnglish.asset`
  - `Layouts/LayoutSymbols.asset`
- 한글 레이아웃을 기본값으로 설정했다.
- 숫자 `1`~`0` 행은 키보드 최상단에 유지했다.
- `한/영` 키는 한글과 영문 레이아웃을 전환하고, `기호` 키는 기호 레이아웃을 열며 다시 `문자` 키를 누르면 이전 언어로 돌아간다.
- Shift 대상 한글 키에는 `ㅃ`, `ㅉ`, `ㄸ`, `ㄲ`, `ㅆ`, `ㅒ`, `ㅖ` 보조 레이블을 표시한다.
- 한글 표시에는 `Assets/Font/Pretendard-Medium SDF.asset`을 사용한다.

#### 3. 제작·검증·배치 도구

- `Assets/Editor/HangulSpatialKeyboardBuilder.cs`에 다음 메뉴를 추가했다.
  - `Tools > XR > Hangul Keyboard > Build Project Prefab`
  - `Tools > XR > Hangul Keyboard > Validate Project Prefab`
  - `Tools > XR > Hangul Keyboard > Create Test Instance In Current Scene`
  - `Tools > XR > Hangul Keyboard > Export Package To Desktop`
- 프리팹 생성 도구는 기존 XRI Spatial Keyboard 샘플을 원본으로 사용하되, 프로젝트 전용 레이아웃과 키 기능을 연결해 `Assets/Prefabs/UI/Hangul Keyboard` 아래에 결과를 저장한다.
- 현재 씬 시험 인스턴스 생성 시 프리팹에 작성된 Transform 값을 그대로 사용한다. 위치 조정은 생성 후 Scene/Inspector에서 수행한다.
- `Assets/Editor/HangulComposerValidationHarness.cs`에 조합 및 단계별 Backspace 회귀 검증을 추가했다.

#### 4. 재사용 패키지

- 최종 출력 파일: `C:\Users\user\Desktop\Tyche_Hangul_XRI_Keyboard_20260803_181217.unitypackage`
- 파일 크기: 약 8.61MB
- 패키지에는 다음 항목과 Unity가 계산한 의존성을 포함했다.
  - `Assets/Scripts/HangulKeyboard`
  - `Assets/Editor/HangulComposerValidationHarness.cs`
  - `Assets/Editor/HangulSpatialKeyboardBuilder.cs`
  - `Assets/Prefabs/UI/Hangul Keyboard`
  - `Assets/Font/Pretendard-Medium SDF.asset`
  - XRI Spatial Keyboard 프리팹이 참조하는 샘플 에셋과 관련 의존성
- 임시 일회용 내보내기 스크립트와 요청 마커는 최종 패키지에서 제외하고 프로젝트에서도 제거했다.
- 이후에는 `Tools > XR > Hangul Keyboard > Export Package To Desktop`을 실행하면 타임스탬프가 붙은 새 패키지를 바탕화면에 만들 수 있다.
- 같은 날 생성된 `Tyche_Hangul_XRI_Keyboard_20260803_181019.unitypackage`는 최초 출력본이며, 재사용 기준은 `181217` 최종본이다.

### 근본 원인 및 해결

#### Unity/XRI 키보드만으로 한글이 조합되지 않는 이유

- XRI 키보드는 각 키의 문자열을 입력 대상으로 전달하지만, `ㅎ`·`ㅏ`·`ㄴ`을 `한`으로 묶는 한국어 IME 상태 머신은 포함하지 않는다.
- 프로젝트 전용 `HangulComposer`가 현재 조합 중인 QWERTY 키열을 관리하고, 렌더링된 한글 구간만 XRI 텍스트에 반영하도록 해결했다.

#### `ㅖ`와 복합 입력 처리

- 두벌식에서 `ㅖ`는 Shift+P이므로 Shift 상태의 `P` 입력을 별도 자모로 해석해야 한다.
- 기본 자판 문자와 Shift 문자를 레이아웃에 함께 저장하고, 조합기는 대문자 물리 키를 쌍자음 및 `ㅒ`·`ㅖ`로 구분한다.

#### 다른 프로젝트에서 계속 사용하는 방법

- 단순히 현재 씬 오브젝트만 복사하면 키 기능, 레이아웃, 폰트와 XRI 샘플 참조가 누락될 수 있다.
- `ExportPackageOptions.Recurse | IncludeDependencies`로 프리팹과 의존성을 함께 내보내는 전용 메뉴를 제공해 재사용 단위를 명확히 했다.

### 영향 범위

- XRI Spatial Keyboard를 사용하는 월드 스페이스 문자 입력 UI
- 한글·영문·기호 모드 전환과 숫자·Shift·Backspace 입력
- 키 입력 Console 진단 로그
- 키보드 프리팹 생성, 현재 씬 시험 배치, 자동 검증 및 바탕화면 패키지 내보내기
- 패키지를 가져오는 다른 Unity 프로젝트의 XRI Spatial Keyboard 샘플 및 TMP 폰트 의존성

### 완료한 검증

- 조합 검증 11건을 통과했다: `한글`, `안녕하세요`, `예`, `계`, `얘`, `까`, `와`, `워`, `가가`, `값`, `갑사`.
- `한 → 하 → ㅎ → 빈 문자열` 순서의 단계별 Backspace 검증을 통과했다.
- 생성 프리팹에서 한글 기본 레이아웃, 한글·영문·기호 전환, 전용 Backspace, Shift 보조 레이블과 Console 로그 활성화를 검증했다.
- Unity가 스크립트 어셈블리를 오류 없이 다시 로드한 것을 Editor 로그에서 확인했다.
- 최종 `.unitypackage`의 내용을 검사해 런타임 코드, Editor 도구, 프리팹, 레이아웃, 폰트와 의존성이 포함되고 임시 내보내기 파일은 포함되지 않았음을 확인했다.
- 바탕화면에서 최종 패키지 파일이 생성되었으며 크기가 약 8.61MB임을 확인했다.

### 남은 수동 검증

- `Hangul Spatial Keyboard.prefab`을 시험 씬에 배치하고 XRI 컨트롤러와 핸드의 Poke로 모든 키가 한 번씩 정상 입력되는지 확인한다.
- TMP 입력 필드에 `안녕하세요`, `예`, `계`, 겹받침이 포함된 문장을 입력하고 커서 위치 및 Backspace 결과를 확인한다.
- `한/영`, `기호/문자`, Shift 전환 뒤 키 레이블과 실제 입력값이 일치하는지 확인한다.
- Console에서 `[Hangul Keyboard] Key pressed ...` 로그가 키마다 한 번만 출력되는지 확인한다.
- Quest/OpenXR에서 키 가독성, 누름 범위, 양손 입력과 양쪽 눈 렌더링을 확인한다.
- 빈 Unity 테스트 프로젝트에 `181217` 최종 패키지를 가져와 누락된 에셋이나 GUID 충돌 없이 프리팹을 생성·배치할 수 있는지 확인한다.
