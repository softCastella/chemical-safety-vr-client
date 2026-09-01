# Quest APK 2D 패널 실행 및 빌드 차단

## 증상

- 첫 번째 `Build And Run`은 `UnityException: No Android devices connected`로 중단되었다.
- 두 번째 시도는 Player 컴파일 오류로 APK를 생성하지 못했다.
- 컴파일 오류를 수정한 뒤 생성한 APK는 Quest 2에 설치되고 OpenXR 세션과 음성은 시작됐지만, 몰입형 화면 대신 작은 검은 2D 패널로 표시됐다.
- Quest Link를 함께 실행하면 독립 실행 APK가 포커스를 잃고 일시정지됐다.
- Quest VR manifest를 수정한 새 APK는 몰입형으로 실행됐지만, Unity 시작 로고 다음의 씬 콘텐츠가 사용자의 시작 시선 반대편에 나타났다.

## 근본 원인

### Player 컴파일 오류

`PPEGloveVisualAppearance.Reset()`과 `PPEHangSuitVisualAppearance.Reset()`은 Player에도 컴파일되지만, 두 메서드가 호출한 `CaptureRendererAndMaterialDefaults()`는 `UNITY_EDITOR` 조건부 영역 안에만 있었다. Editor에서는 숨겨졌던 오류가 Android Player 컴파일에서 드러났다.

### 2D 패널 실행

`Assets/XR/Settings/OpenXR Package Settings.asset`의 Android `Meta Quest Support` 기능이 비활성화돼 있었다. 실패 APK의 manifest를 `aapt dump badging`으로 확인한 결과 다음 Quest VR 선언이 없었다.

- `android.hardware.vr.headtracking`
- `com.oculus.intent.category.VR`

그 결과 Quest OS는 앱을 몰입형 VR 앱이 아닌 일반 Android 2D 앱처럼 표시했다. 태블릿 Game View 보조 입력이나 실제 XR 리그 비활성화가 원인은 아니었다.

### 시작 방향 불일치

공간 진단 결과 Title Canvas는 `Z=+200`, Intro Canvas는 `Z=+937`, PPE 활성 Renderer Bounds 중심은 `Z=+12.5`였고 세 씬의 작성 카메라 정면은 모두 `+Z`였다. 따라서 임포트 모델이나 씬 전체가 180도 뒤집힌 것이 아니다.

Quest의 추적 공간 수평 방향이 앱 시작 시 사용자의 현재 시선과 일치한다고 가정했지만, Title·Intro·PPE의 새 XR Origin은 그 추적 방향을 세션 기준 정면으로 맞추지 않았다. 시스템 Unity 로고는 머리 기준으로 정면에 표시되지만, 이후 월드 콘텐츠는 기존 추적 공간 방향을 사용해 반대편에 나타날 수 있었다.

## 적용 변경

- 두 외형 컴포넌트의 `Reset()`에서 Editor 전용 기본값 캡처 호출을 `UNITY_EDITOR`로 격리했다.
- Android OpenXR의 최신 `Meta Quest Support` 기능을 활성화했다.
- `Tools > XR > Validate Meta Quest Android Build` 정적 검증 메뉴를 추가했다.
- Title 진입 시 유효한 HMD 수평 회전을 한 번 캡처하고, Intro와 PPE에서도 같은 세션 방향을 적용하는 `XRSessionForwardAlignment`를 추가했다.
- `Tools > XR > Validate Session Forward Alignment` 정적 검증 메뉴를 추가했다.

## 영향 범위

- Android/Meta Quest Player 빌드와 Quest manifest에만 영향을 준다.
- 씬 작성값, XR Origin, 컨트롤러 입력 구성, Game View 테스트 동작은 변경하지 않는다.
- 씬의 모델·Canvas·XR Origin 작성 위치와 회전은 유지하고, 실행 중 XR Origin의 세션 수평 회전만 적용한다.
- 서버 API, DB, 텔레메트리 계약 변경은 없다.

## 검증

### 완료

- 수정 전 APK 생성 실패 로그에서 두 `CS0103` 오류를 확인했다.
- 수정 후 Unity Editor 스크립트 컴파일 성공을 확인했다.
- 수정 전 APK가 Quest 2에 설치되고 OpenXR 세션을 시작했지만 Quest Link 포커스 전환 시 일시정지되는 로그를 확인했다.
- 수정 전 APK manifest에 Quest VR 선언 두 항목이 없음을 확인했다.
- 변경 후 APK manifest에 `android.hardware.vr.headtracking`과 `com.oculus.intent.category.VR`이 생성된 것을 확인했다.
- Title·Intro·PPE의 작성 카메라 정면과 주요 콘텐츠가 모두 `+Z` 방향임을 공간 진단으로 확인했다.
- `MetaQuestAndroidBuildValidationHarness`와 `XRSessionForwardAlignmentValidationHarness` 정적 검증을 통과했다.

### 아직 필요한 수동 검증

- 세션 방향 정렬 변경 후 APK를 다시 빌드한다.
- Quest Link 없이 Quest 2의 `알 수 없는 출처`에서 실행해 최초 시선 정면에 Title·Intro·PPE가 연속해서 유지되는지 확인한다.
- 몰입형 화면, 양안 렌더링, 컨트롤러 레이와 입력을 확인한다.
- PPE 방에서 태블릿 잡기·놓기와 종료 텔레메트리 업로드를 확인한다.

## 실패 시 후속 작업

- 씬이나 임포트 모델을 180도 회전하지 않는다. 정적 공간 진단에서 Title·Intro·PPE 콘텐츠가 모두 작성 카메라의 `+Z` 앞쪽임을 확인했다.
- 새 APK에서도 콘텐츠가 뒤에 나타나면 Android 로그의 `[XR Session Forward]` 적용 yaw, 씬 이름, OpenXR 세션 상태를 수집한다.
- Title에서 캡처한 세션 yaw가 Intro와 PPE에 동일하게 적용되는지 비교한다. 값이 달라지면 씬 전환 중 OpenXR 추적 공간 재초기화 여부를 조사한다.
- 방향은 정상인데 레이만 없으면 `입력 장치 → Interactor → Caster/Ray → Layer/Collider 또는 Graphic → Raycaster → XRUIInputModule → press/select action → handler` 경로를 별도로 검증한다.
- 앱 표시 이름은 현재 `ProjectSettings/ProjectSettings.asset`의 `Prototype_Tyche_Jinyoung`이다. 배포 이름은 사용자 확정 후 별도 변경한다.
- Quest/OpenXR 실기 검증 전에는 방향 정렬과 컨트롤러 입력을 완료로 보고하지 않는다.

## 2026-09-01 Editor 무 HMD 추적 대기 로그 보정

### 변경 전 필수 확인

1. Inspector와 씬 작성값은 변경하지 않고 보존한다.
2. 세션 방향 상태의 단일 소유자는 `XRSessionForwardAlignment`의 정적 세션 정렬값이다.
3. 입력 경로는 `XRNode.Head -> InputDevice -> CommonUsages.deviceRotation`이며 Interactor, Raycaster, Collider, PPE Grab 경로는 변경하지 않는다.
4. 실제 Player 또는 실행 중인 XR Display에서 머리 회전을 읽지 못하면 자동 수리하지 않고 기존 Error를 유지한다.
5. 영향 소비자는 Title, Intro, PPE의 XR Origin 방향과 HMD 없는 Editor/Game View 로그 등급뿐이다.
6. 변경 전 기준은 HMD 없는 Editor에서 10초 뒤 Error였고, 변경 후 기준은 동일 상황에서 authored rotation을 보존하는 일반 Log이다. 실행 중인 XR Display의 timeout은 계속 Error다.
7. 정적 컴파일과 `XRSessionForwardAlignmentValidationHarness`까지 확인하고, Quest/OpenXR 방향과 양안은 별도 수동 검증으로 남긴다.

### 근본 원인과 적용 변경

- 머리 회전 timeout이 실행 환경과 무관하게 항상 `Debug.LogError`를 호출해, XR Display가 없는 Editor/Game View의 정상 fallback도 오류로 표시됐다.
- timeout 시 실행 중인 `XRDisplaySubsystem`을 확인한다. Editor에서 실행 중인 XR Display가 없을 때만 일반 Log로 기록하고 authored XR Origin rotation을 보존한다.
- Android Player와 Quest Link 등 실제 XR Display가 실행 중인 경우에는 기존 Error를 유지한다.
- 씬, Inspector 직렬화값, PPE 선택·잡기·착용·내려놓기 동작은 변경하지 않았다.

### 검증 상태

- `XRSessionForwardAlignmentValidationHarness`에 Editor/Player와 XR Display 실행 여부 조합별 timeout 등급 검증을 추가했다.
- 실제 Quest/OpenXR 시작 방향과 양안 결과는 헤드셋 연결 후 수동 확인이 필요하다.

## 2026-09-01 Unity 6 Console 정리

### 적용 변경과 근본 원인

- 부모 GUID가 없는 미사용 Prefab Variant `construction_helmet_3d_model_Clone1.prefab`가 Asset import Error를 발생시켰다. 해당 자산 GUID의 참조가 없음을 확인한 뒤 Prefab과 meta를 제거했다. 현재 PPE 헬멧은 `PPE_A_Helmet_Strap.fbx`를 계속 사용한다.
- 프로젝트 소유 코드가 Unity 6에서 폐기된 `FindObjectsSortMode` 및 `FindFirstObjectByType` API를 사용해 반복 컴파일 Warning을 만들었다. 정렬하지 않는 기존 검색 의미를 유지하는 새 overload와 `FindAnyObjectByType`으로 교체했다.
- taped FBX의 legacy External material location을 `InPrefab`으로 마이그레이션했다. 기존 external object remap은 유지했으며, 재임포트 후 helmet은 9개 remap/9개 Renderer, taped는 6개 remap/1개 Renderer에 null 재질이 없음을 확인했다.
- `PPEScenarioQuizValidationHarness`가 Preview Scene 오브젝트를 전역 검색해 `PPEQuizController`를 0개로 오판했다. 대상 Preview Scene root에서 직접 검색하도록 수정했다.
- `4_PPE_Room`의 ControllerGuide Image 6개는 하네스 계약과 달리 `m_PreserveAspect: 0`이었다. 런타임 `preserveAspect` 대입이 없음을 확인하고 해당 Image의 씬 작성값만 `1`로 변경했다. 다른 RectTransform, 색상, Sprite, 입력 및 표시 상태는 변경하지 않았다.

### 변경 전 필수 확인

1. 씬 작성값은 ControllerGuide Image 6개의 의도된 `preserveAspect`만 변경하고 나머지는 보존한다.
2. 안내 이미지 표시의 기준은 `4_PPE_Room`에 직렬화된 각 `Image`이며 런타임 코드는 활성 상태만 전환한다.
3. XR 입력, Interactor, Raycaster, Layer, Collider 경로는 변경하지 않는다.
4. 누락 참조나 잘못된 씬 값을 런타임에서 자동 수리하지 않고 Editor 하네스 실패로 중단한다.
5. 영향 소비자는 Unity 컴파일, FBX import, ControllerGuide 표시, Quiz/PPE 정적 검증이며 PPE Grab·착용·내려놓기 상태는 보존한다.
6. 변경 전 기준은 Prefab import Error, 폐기 API Warning 및 PPE/Quiz 하네스 실패이고 변경 후 기준은 재컴파일과 동일 하네스 PASS이다.
7. Unity 정적 컴파일과 하네스까지 확인했으며 Quest/OpenXR 양안, 실제 PPE 상호작용, Build And Run은 수동 검증으로 남긴다.

### 완료한 검증과 남은 검증

- `PPELocomotionPpeRegressionValidationHarness`, `PPEScenarioQuizValidationHarness`, `MetaQuestAndroidBuildValidationHarness`, `XRSessionForwardAlignmentValidationHarness`, `PPETrainingDataContractHarness`를 통과했다.
- 삭제 후 orphan helmet Prefab import Error가 새로 기록되지 않음을 확인했다. 기존 Console 항목은 `Clear` 또는 Editor 재시작 전까지 남는다.
- XRI sample의 `GetInstanceID` 폐기 경고, 빈 animation clip/중복 mesh 같은 imported model 경고, Unity AI/MCP 및 관리자 실행 경고는 프로젝트 소유 런타임 결함과 분리한다.
- Quest가 연결되지 않아 Build And Run, 실제 DB 업로드, `quiz_answer_resolved`와 `mode_session_completed`의 동일 `modeSessionId` 검증은 아직 수행하지 않았다.

## 2026-09-01 Quest LAN 설정 주입 Windows 인수 분리 결함

### 증상과 근본 원인

- Android Development APK를 Quest 2에 설치한 뒤 `Tools > PPE > Inject Quest Development LAN Configuration`을 실행하면 `mkdir: Needs 1 argument`로 중단됐다.
- 주소와 token 검증 및 APK의 `DEBUGGABLE` 상태는 정상이었다. 실패 원인은 Editor 도구가 Windows의 `ProcessStartInfo.Arguments` 문자열에 `adb shell run-as ... sh -c "mkdir -p files && cat ..."`를 전달하면서 Android shell의 `-c` 명령 문자열이 한 인수로 보존되지 않은 것이다.
- 이 실패는 Quest USB 승인, 서버 3000번 포트, MySQL 인증 또는 PPE 런타임 상태와 무관하다.

### 적용 변경과 영향 범위

- 앱 내부 `files` 디렉터리 생성은 `adb shell run-as <package> mkdir -p files`로 분리했다.
- JSON은 기존처럼 adb 표준입력으로만 전달하고 `adb shell run-as <package> tee files/.tyche-development-lan.json`으로 기록한다. 주소와 token을 명령행·문서·Git에 포함하지 않는 보안 경계는 유지한다.
- `PPETrainingDataContractHarness`에 `mkdir` 분리와 표준입력 `tee` 경로를 확인하는 회귀 검사를 추가했다.
- 씬, Inspector 작성값, PPE 입력·Grab·착용·내려놓기, 런타임 상태 전이와 서버 API 계약은 변경하지 않았다.

### 검증

- Quest 2의 `run-as`에서 분리된 `mkdir`와 표준입력 `tee`가 실제로 성공함을 확인했다.
- Unity 스크립트 컴파일과 `PPETrainingDataContractHarness`가 PASS했다.
- 수정한 Editor 메뉴가 PASS했고, 앱 실행 전 일회성 파일이 124바이트로 생성된 뒤 앱 시작 약 15초 후 삭제됨을 확인했다. 파일 내용과 token은 출력하지 않았다.
- Development APK `com.softcastella.prototype.tyche.jinyoung`은 Quest에서 `DEBUGGABLE`, PID 10438 및 포그라운드 `UnityPlayerGameActivity`로 확인됐다.
- Quest JSONL 18건 생성은 확인했지만 Quest에서 PC의 TCP 3000 연결은 실패했다. 이번 실행의 서버·DB 수신, 퀴즈 응답 및 모드 완료는 아직 PASS가 아니다.
- 최종 Unity Console 재확인에서 Error는 수정 전 23:05:32의 `mkdir` 실패 기록 1건뿐이었다. 수정 후 23:07 주입 PASS 이후 새 Error는 없으며, 남은 Warning은 빌드 중 Sentis·URP 셰이더, 압축 아이콘 품질 및 Unity MCP 자동화 기록으로 분리했다.
