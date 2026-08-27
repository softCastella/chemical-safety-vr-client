# FBX 노멀맵/AO 테스트 범위 및 복구 안전성

## 분류

- 상태: 테스트 준비 완료, 실제 재현 전
- 영향: 공유 머티리얼을 사용하는 씬 전체에 전파될 수 있음
- 관련 도구: `Assets/Editor/FbxNormalOcclusionTest.cs`

## 관찰 사항

FBX 관련 머티리얼은 여러 씬에서 공유될 수 있다. 노멀맵과 AO 텍스처를 머티리얼 파일에서 직접 제거하면 활성 씬 하나만이 아니라 같은 머티리얼을 참조하는 다른 씬에도 시각적 변화가 나타날 수 있다.

## 대응

- FBX 원본/메타는 변경하지 않는다.
- 변경 전 머티리얼 텍스처 GUID, AO 강도, 관련 키워드를 `Assets/Editor/FbxNormalOcclusionTestSnapshot.json`에 저장한다.
- `Tools > FBX Texture Test > Restore Original Normal Maps + Occlusion`으로 복구한다.
- 복구 확인 전에는 스냅샷을 삭제하지 않는다.

## 검증 계획

1. Unity 컴파일 완료 후 메뉴에서 스냅샷 생성 및 비활성화를 승인한다.
2. 관련 씬을 순회하며 노멀 디테일, 접촉 음영, 조명 차이를 확인한다.
3. Quest/OpenXR 양쪽 눈과 Game 뷰를 확인한다.
4. 이상 발생 시 Restore 후 머티리얼 참조와 씬을 재확인한다.

## 현재 결론

도구와 복구 경로는 준비되었지만, 2026-07-31 현재 실제 머티리얼 변경 테스트는 실행하지 않았다.

## 관련 당일 이슈

- Mixer Room 씬의 authored 값이 Play Mode/Refresh에서 덮어써지지 않는지 확인이 필요하다.
- 컨트롤러 손 애니메이션은 Animator Controller와 실제 입력 파라미터가 연결된 상태에서 별도 검증이 필요하다.
- 손가락 본 캐시 실패 시 Animator 자체가 업데이트 목록에서 빠질 수 있었던 문제를 수정했으며, 실제 컨트롤러 입력 검증이 필요하다.
- PPE 손 모델은 방호복 노랑과 손목 검정 고무줄이 참조 이미지와 일치하는지 시각 검증이 필요하다.
