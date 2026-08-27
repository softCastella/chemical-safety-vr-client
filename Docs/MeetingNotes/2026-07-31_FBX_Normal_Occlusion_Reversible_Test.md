# FBX 노멀맵/AO 가역 테스트 준비

## 오늘 작업

- FBX 원본과 `.meta`를 수정하지 않는 에디터 테스트 도구를 추가했다.
- `Assets/FBX`와 `Assets/Materials`의 머티리얼을 대상으로 `_BumpMap`/`_NormalMap`, `_OcclusionMap`, AO 강도와 관련 키워드를 처리하도록 했다.
- 실행 전에 기존 텍스처 참조, AO 강도, 셰이더 키워드를 JSON 스냅샷으로 저장하도록 했다.
- `Restore Original Normal Maps + Occlusion` 메뉴로 원복할 수 있도록 했다.
- `Clear Snapshot`은 복구 확인 후에만 삭제 확인 창을 거치도록 했다.
- 실행/복구 절차와 주의사항을 `Docs/FbxNormalOcclusionTest.md`에 문서화했다.

## 결정 사항

- 테스트 범위는 특정 씬이 아니라 지정된 프로젝트 머티리얼 전체다.
- 공유 머티리얼을 사용하는 여러 씬에 영향을 줄 수 있으므로, 테스트 중에는 스냅샷을 유지한다.
- 현재 세션에서는 Unity 메뉴를 실행하지 않았으며, 실제 변경은 에디터에서 사용자가 승인한 뒤 실행한다.

## 다음 확인

- Unity 컴파일 완료 후 테스트 메뉴를 실행한다.
- 주요 씬의 시각 품질과 조명 변화를 확인한다.
- Quest/OpenXR 양쪽 눈에서 이상 여부를 확인한다.
- 이상이 있으면 즉시 Restore를 실행하고, 정상 확인 후 스냅샷을 정리한다.

## 오늘 작업 전체 기록

이 테스트 준비 외에도 Mixer Room Floor/포스트 프로세싱, 컨트롤러 손 Animator 연결, PPE 손 색상·고무줄 표현, OpenXR 프로필 및 Scene View 기즈모 안내를 함께 점검했다. 세부 작업은 `Docs/MeetingNotes/2026-07-31_Unity_XR_Meeting.md`에 통합 기록했다.
