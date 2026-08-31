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
