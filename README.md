# Tyche Chemical Safety Training VR Client

Meta Quest/OpenXR 기반 화학 안전 교육 Unity XR 클라이언트와 프로젝트 문서, 제작 도구 및 검증 하네스를 관리하는 비공개 저장소입니다.

Express 서버는 별도 비공개 저장소인 `softCastella/chemical-safety-vr-server`에서 관리합니다. 이 저장소에는 서버 소스의 복사본을 두지 않습니다.

## 저장소 구성

- Unity 프로젝트: 저장소 루트(`Assets`, `Packages`, `ProjectSettings`)
- 프로젝트 문서와 회귀 보고서: `Docs`
- 저장소 공용 제작·검증 스크립트: `Tools`
- Codex 및 에이전트 지침: `.codex`, `AGENTS.md`

## 개발 환경

- Unity 6000.4.8f1
- Universal Render Pipeline 17.4.0
- OpenXR
- XR Interaction Toolkit 3.4.1
- XR Hands 1.7.3
- Input System 1.19.0
- 대상 플랫폼: Meta Quest/Android 및 PC OpenXR

## 빌드 씬

활성 빌드 흐름은 `ProjectSettings/EditorBuildSettings.asset`을 기준으로 합니다. 현재 `0_App`, `1_Title`, `2_Intro`, `3_Loading`, `4_PPE_Room` 순서이며 `5_MixerRoom`과 `6_InsideMixer`는 후속 콘텐츠로 비활성화되어 있습니다.

## 주요 경로

- 런타임 코드: `Assets/Scripts`
- Editor 도구와 검증 하네스: `Assets/Editor`
- 프로젝트 문서와 버그 리포트: `Docs`
- 지원 스크립트: `Tools`

Unity 프로젝트를 열 때는 저장소 루트를 선택합니다. `Library`, `Temp`, `Logs`, `obj`, `UserSettings`와 같은 Unity 생성 폴더는 버전 관리하지 않습니다.

서버 연동을 확인하거나 최종 문서를 작성할 때는 서버 저장소의 대상 브랜치와 커밋 SHA를 함께 확인합니다. 서로 다른 Codex 대화의 기억이 아니라 양쪽 저장소의 코드, 문서 및 검증 결과를 작업 사실의 기준으로 사용합니다.
