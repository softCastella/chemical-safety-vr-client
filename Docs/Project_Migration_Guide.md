# 3D_UI_Test_home 프로젝트 이식 가이드

## 목적

이 문서는 `3D_UI_Test_home`의 씬, Hierarchy 오브젝트, 프로젝트 에셋, 런타임 코드 및 에디터 도구를 다른 Unity 프로젝트로 이식할 때 참조 손실을 최소화하기 위한 기준을 정의한다.

현재 프로젝트의 기준 환경은 다음과 같다.

- Unity: `6000.4.8f1`
- Render Pipeline: URP `17.4.0`
- XR Runtime: OpenXR
- XR Interaction Toolkit: `3.4.1`
- XR Hands: `1.7.3`
- Input System: `1.19.0`
- 대상 플랫폼: Meta Quest/Android, PC OpenXR

## 핵심 원칙

1. Unity 에셋은 반드시 해당 `.meta` 파일과 함께 옮긴다.
2. GUID 보존이 필요한 파일은 운영체제 파일 복사 또는 Unity의 `Export Package`를 사용한다.
3. 씬이 직렬화해서 참조하는 에셋은 `Include dependencies`로 수집한다.
4. 코드에서 문자열로 찾는 에셋, 씬 및 셰이더는 자동 의존성 수집 결과를 별도로 보완한다.
5. `ProjectSettings`와 `Packages`는 대상이 새 프로젝트인지 기존 프로젝트인지에 따라 복사 또는 비교 병합한다.
6. Hierarchy 오브젝트는 독립된 Prefab으로 만든 후 Prefab 의존성을 내보내는 것을 기본으로 한다.
7. 씬 외부 오브젝트를 직접 참조하는 Hierarchy 오브젝트는 Prefab 단위가 아닌 씬 또는 기능 루트 단위로 이식한다.
8. 에디터 도구는 런타임 코드와 씬을 먼저 가져온 뒤 마지막에 가져온다.

## 이식 방식 선택

### 새 프로젝트로 전체 복제

대상 프로젝트가 비어 있고 현재 프로젝트 구성을 그대로 유지해야 한다면 다음 폴더를 통째로 복사하는 방식이 가장 안전하다.

```text
Assets/
Packages/
ProjectSettings/
```

다음 생성 폴더는 복사하지 않는다.

```text
Library/
Temp/
Logs/
UserSettings/
Obj/
```

대상 프로젝트는 가능하면 원본과 동일한 Unity `6000.4.8f1`로 연다. Unity가 `Library`를 새로 생성하고 에셋을 임포트하도록 한다.

### 기존 프로젝트에 기능 병합

기존 프로젝트의 렌더링, XR 또는 패키지 구성을 유지해야 한다면 다음과 같이 패키지를 분리한다.

```text
Migration/
├─ 00_Prerequisites/
├─ 01_CoreXR.unitypackage
├─ 02_Resources.unitypackage
├─ 03_Scenes/
├─ 04_Prefabs/
├─ 05_EditorTools.unitypackage
├─ 06_ProjectSettings/
└─ 07_ExternalToolData/
```

## 현재 Build Scene 구성

`ProjectSettings/EditorBuildSettings.asset` 기준 활성 씬 순서는 다음과 같다.

| 순서 | 씬 |
|---:|---|
| 0 | `Assets/Scenes/0_App.unity` |
| 1 | `Assets/Scenes/1_Title.unity` |
| 2 | `Assets/Scenes/2_Intro.unity` |
| 3 | `Assets/Scenes/3_PPE_Room.unity` |
| 4 | `Assets/Scenes/4_InsideMixer.unity` |
| 5 | `Assets/Scenes/5_MixerRoom.unity` |
| 6 | `Assets/Scenes/6_LoadingScene.unity` |

`Assets/Scenes/Confined Space Scene_half.unity`는 현재 Build Settings에 비활성 상태로 등록되어 있다.

씬을 가져온 뒤 대상 프로젝트의 Build Profiles 또는 Build Settings에 동일한 순서로 다시 등록한다. 씬 이름을 문자열로 로드하는 코드가 있으므로 씬 파일명 변경은 피한다.

## 패키지별 포함 범위

### 00_Prerequisites

다음 정보를 문서 또는 체크리스트로 전달한다.

- Unity 버전
- 대상 플랫폼
- 사용 패키지 이름과 버전
- URP 사용 여부
- Android 및 Standalone OpenXR 기능 설정
- Tags, Layers 및 Physics collision matrix
- Build Scene 순서

현재 `Packages/manifest.json`에는 다음 주요 의존성이 있다.

```text
com.unity.render-pipelines.universal 17.4.0
com.unity.inputsystem 1.19.0
com.unity.xr.interaction.toolkit 3.4.1
com.unity.xr.hands 1.7.3
com.unity.xr.management 4.6.0
com.unity.xr.openxr 1.16.1
com.smilejsu82.vr-base Git package
```

새 프로젝트에는 `Packages/manifest.json`과 `Packages/packages-lock.json`을 복사할 수 있다. 기존 프로젝트에는 파일 전체를 덮어쓰지 말고 의존성과 버전을 비교해서 병합한다.

### 01_CoreXR.unitypackage

XR 및 렌더링 기반 에셋을 포함한다.

```text
Assets/Settings/
Assets/XR/
Assets/XRI/
Assets/InputSystem_Actions.inputactions
Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/
Assets/Samples/XR Hands/1.7.3/HandVisualizer/
```

씬 의존성으로 포함되지 않은 공통 런타임 스크립트와 프로젝트 전용 셰이더도 이 패키지에 포함할 수 있다.

```text
Assets/Scripts/
Assets/Shaders/
```

다만 기능별로 배포해야 하는 경우에는 전체 폴더 대신 실제 씬 및 Prefab 의존성에 포함된 파일만 선택한다.

### 02_Resources.unitypackage

`Resources.Load` 호출로 로드되는 에셋은 씬의 직렬화 의존성에 나타나지 않을 수 있으므로 별도로 포함한다.

현재 필수 범위:

```text
Assets/Resources/Audio/
```

`AudioManager`는 다음 경로를 문자열로 사용한다.

```text
Audio/AudioManagerSettings
Audio/Scenes/{SceneName}
```

안전한 이식이 우선이면 `Assets/Resources/` 전체를 내보낸다.

### 03_Scenes

각 씬을 개별 `.unitypackage`로 내보낸다.

예시:

```text
Scene_0_App.unitypackage
Scene_1_Title.unitypackage
Scene_2_Intro.unitypackage
Scene_3_PPE_Room.unitypackage
Scene_4_InsideMixer.unitypackage
Scene_5_MixerRoom.unitypackage
Scene_6_LoadingScene.unitypackage
```

Unity Project 창에서 씬 파일을 선택하고 다음 명령을 실행한다.

```text
Assets > Export Package...
```

`Include dependencies`를 활성화한다. 이 옵션은 일반적으로 다음 참조를 함께 수집한다.

- Prefab 및 Nested Prefab
- MonoBehaviour 스크립트
- FBX, Mesh 및 Avatar
- Material, Texture 및 Shader
- Animation Clip 및 Animator Controller
- Audio Clip
- Font 및 TMP Font Asset
- Input Action Asset

### 04_Prefabs

Hierarchy 오브젝트 단위 이식은 다음 순서로 진행한다.

1. 대상 오브젝트의 최상위 기능 루트를 확인한다.
2. 기능에 필요한 자식 오브젝트를 모두 해당 루트 아래에 포함한다.
3. 프로젝트의 `Assets/Prefabs/Migration/` 등에 Prefab으로 저장한다.
4. Prefab 인스턴스를 다시 선택해 Missing Reference 여부를 확인한다.
5. Project 창의 Prefab 에셋을 `Include dependencies`와 함께 내보낸다.

Prefab 에셋은 씬 오브젝트를 직접 참조할 수 없다. 다음 참조가 있으면 Prefab만으로 완전한 이식이 되지 않는다.

- 씬의 XR Origin 또는 Main Camera
- 씬 전역 Manager
- 다른 Hierarchy 루트의 Transform 또는 Component
- 외부 오브젝트를 대상으로 하는 UnityEvent
- 씬에만 존재하는 AudioSource, Light 또는 Volume

이런 경우에는 다음 중 하나를 선택한다.

- 관련 오브젝트를 하나의 기능 루트 아래로 묶어 함께 Prefab화한다.
- 기능 전체를 Additive Scene으로 분리한다.
- 외부 참조가 많은 경우 원래 씬 단위로 이식한다.
- 대상 프로젝트에서 다시 연결해야 하는 외부 참조 목록을 작성한다.

### 05_EditorTools.unitypackage

다음 폴더를 별도 패키지로 관리한다.

```text
Assets/Editor/
```

현재 일부 에디터 도구는 다음과 같은 고정 경로를 사용한다.

```text
Assets/Scenes/3_PPE_Room.unity
Assets/Scenes/4_InsideMixer.unity
Assets/Scenes/5_MixerRoom.unity
Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/
Assets/Samples/XR Hands/1.7.3/HandVisualizer/
Assets/Font/Pretendard-Medium SDF.asset
Assets/UIs/sign_stamp/
Assets/Audio/SFX/
Assets/Materials/
```

에디터 도구가 사용하는 씬과 에셋을 먼저 가져오고 컴파일 오류가 없는 상태에서 `EditorTools.unitypackage`를 가져온다. 일부 도구는 씬 열기 콜백이나 자동 임포트 콜백을 사용할 수 있으므로 도구를 가져오기 전에 대상 프로젝트를 버전 관리 시스템에 커밋한다.

### 06_ProjectSettings

다음 설정은 `.unitypackage`의 `Include dependencies`만으로 완전히 전달되지 않는다.

```text
ProjectSettings/EditorBuildSettings.asset
ProjectSettings/GraphicsSettings.asset
ProjectSettings/QualitySettings.asset
ProjectSettings/TagManager.asset
ProjectSettings/ProjectSettings.asset
Packages/manifest.json
Packages/packages-lock.json
```

새 프로젝트에는 원본 설정을 복사할 수 있다. 기존 프로젝트에는 다음 항목을 중심으로 비교 병합한다.

- Build Scene 목록과 순서
- Graphics Settings의 URP Global Settings
- Quality별 Render Pipeline Asset
- Tags 및 Layers
- Physics 및 Physics 2D Layer Collision Matrix
- Input System 사용 설정
- Android Player Settings
- OpenXR loader와 interaction profile
- Standalone 및 Android별 XR Plug-in Management 설정

### 07_ExternalToolData

`Assets` 밖의 파일은 `.unitypackage`에 포함되지 않는다.

현재 다음 경로에는 에디터 도구 또는 생성 스크립트가 사용할 수 있는 파일이 있다.

```text
Img/
Tools/
```

예를 들어 `MixerRoomAtmosphereBuilder`는 다음 파일 경로를 사용한다.

```text
Img/혼합기동 화면.png
```

이 파일들은 별도 ZIP으로 전달하거나 장기적으로 `Assets/Editor` 하위의 프로젝트 에셋으로 이동하는 방식을 검토한다.

## 자동 의존성 수집에서 빠질 수 있는 항목

다음 항목은 별도 확인이 필요하다.

- `Resources.Load()` 경로
- `Shader.Find()`로만 검색하는 셰이더
- `SceneManager.LoadScene()`에 전달하는 문자열 씬 이름
- `AssetDatabase.LoadAssetAtPath()`의 고정 경로
- `GameObject.Find()`가 기대하는 Hierarchy 오브젝트 이름
- Tags 및 Layers
- Sorting Layer
- Addressables 또는 AssetBundle 설정
- StreamingAssets 파일
- `Assets` 외부의 원본 데이터
- 플랫폼별 OpenXR 설정
- 셰이더 변형과 XR Single Pass Instanced 호환성

## 권장 가져오기 순서

1. 대상 프로젝트를 Git 등으로 백업한다.
2. Unity 버전을 확인한다.
3. 패키지를 설치하고 버전을 맞춘다.
4. URP, XR, XRI 및 Input 기반 에셋을 가져온다.
5. 공통 런타임 스크립트, 셰이더 및 Resources 에셋을 가져온다.
6. Console의 컴파일 오류를 먼저 해결한다.
7. 씬 패키지를 가져온다.
8. 기능별 Prefab 패키지를 가져온다.
9. 에디터 도구 패키지를 마지막에 가져온다.
10. Build Scene 목록과 Project Settings를 비교 적용한다.
11. 모든 씬을 열어 Missing Script와 Missing Reference를 검사한다.
12. Play Mode 전후의 직렬화 값이 의도치 않게 변경되지 않는지 비교한다.
13. Standalone OpenXR와 Android/Quest를 각각 검증한다.

## 검증 체크리스트

### 임포트 및 컴파일

- [ ] Unity Console에 C# 컴파일 오류가 없다.
- [ ] 패키지 버전 충돌이 없다.
- [ ] 모든 `.meta` 파일과 GUID가 유지되었다.
- [ ] 씬과 Prefab에 Missing Script가 없다.
- [ ] Material에 Missing Shader가 없다.
- [ ] Texture, Mesh, Audio 및 Font 참조가 유지되었다.

### 씬 및 UI

- [ ] Build Scene 순서가 원본과 같다.
- [ ] 씬 전환 문자열과 씬 파일명이 일치한다.
- [ ] Hierarchy 오브젝트 이름을 사용하는 코드가 정상 동작한다.
- [ ] UnityEvent 대상 참조가 유지되었다.
- [ ] UI의 RectTransform, Canvas, TMP 및 Material 값이 Play Mode에서 덮어써지지 않는다.
- [ ] Resources 기반 오디오 및 씬별 설정이 로드된다.

### XR 및 렌더링

- [ ] PC Standalone에서 OpenXR Loader가 활성화되어 있다.
- [ ] Android에서 OpenXR Loader가 활성화되어 있다.
- [ ] 필요한 interaction profile이 활성화되어 있다.
- [ ] XR Origin, Controller 및 Hand Tracking Prefab이 정상이다.
- [ ] URP Asset과 Renderer가 Graphics/Quality Settings에 연결되어 있다.
- [ ] 커스텀 셰이더가 Single Pass Instanced 렌더링을 지원한다.
- [ ] Quest 또는 OpenXR 헤드셋의 양쪽 눈에서 위치와 렌더링이 일치한다.

## 작업 전 주의 사항

패키지를 만들기 전에 다음을 수행한다.

1. 열린 씬을 모두 저장한다.
2. Unity의 스크립트 컴파일과 셰이더 임포트가 끝날 때까지 기다린다.
3. Git 상태를 확인한다.
4. 신규 에셋의 `.meta`가 생성되었는지 확인한다.
5. 현재 상태를 커밋하거나 복구 가능한 백업을 만든다.

특히 씬이나 생성된 Material이 저장되지 않은 상태에서 패키지를 만들면 원하는 버전과 실제 내보낸 버전이 달라질 수 있다.

## 권장 장기 개선

반복적으로 이식할 예정이라면 프로젝트 전용 Migration Exporter를 만드는 것이 좋다. 이 도구는 다음 기능을 제공해야 한다.

- 선택한 씬의 재귀 의존성 수집
- 선택한 Prefab의 재귀 의존성 수집
- `Resources.Load`, `Shader.Find`, 씬 문자열 및 고정 에셋 경로 보완 목록
- `Assets/Editor` 도구 패키지 분리
- Build Scene 및 패키지 버전 보고서 생성
- 대상별 `.unitypackage` 일괄 생성
- 수집된 파일과 제외된 파일을 기록한 Manifest 생성

자동화 도구를 사용하더라도 `ProjectSettings`, Packages, 외부 파일 및 씬 외부 오브젝트 참조는 별도 검증해야 한다.
