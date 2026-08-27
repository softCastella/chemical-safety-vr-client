# FBX 노멀맵/AO 비활성화 테스트

## 목적

FBX 관련 머티리얼에서 노멀맵과 글로벌 오클루전(AO)을 임시로 제거해 조명과 성능 변화를 비교한다.

## 현재 상태

- `Assets/Editor/FbxNormalOcclusionTest.cs` 에디터 도구가 추가되어 있다.
- 도구를 추가했을 뿐, Unity에서 테스트 변경을 실행한 상태는 아니다.
- FBX 원본 파일과 `.meta` 파일은 수정하지 않는다.

## 대상 범위

기본 메뉴는 다음 폴더의 머티리얼을 대상으로 한다.

- `Assets/FBX`
- `Assets/Materials`

이 머티리얼을 여러 씬에서 공유하면 프로젝트 내 해당 씬들에 함께 영향을 준다. 특정 씬만 대상으로 하는 기능은 아니다.

## 테스트 실행

1. Unity 에디터에서 스크립트 컴파일이 끝날 때까지 기다린다.
2. `Tools > FBX Texture Test > Disable Normal Maps + Occlusion (Create Snapshot)`을 선택한다.
3. 확인 창에서 `스냅샷 저장 후 실행`을 선택한다.
4. 테스트할 씬을 열고 시각 품질, 조명, 성능을 확인한다.

실행 시 현재 텍스처 참조, AO 강도, 관련 셰이더 키워드를 다음 파일에 저장한다.

`Assets/Editor/FbxNormalOcclusionTestSnapshot.json`

## 원복

테스트 중에는 스냅샷 파일을 삭제하지 않는다.

`Tools > FBX Texture Test > Restore Original Normal Maps + Occlusion`

을 실행하면 스냅샷 기준으로 원래 머티리얼 참조와 설정을 복구한다.

복구가 확인된 후에만 다음 메뉴로 스냅샷을 삭제한다.

`Tools > FBX Texture Test > Clear Snapshot`

## 주의사항

- FBX를 재임포트하면 임포터가 머티리얼을 다시 생성하거나 덮어쓸 수 있다. 가능하면 복구 후 재임포트한다.
- 스냅샷이 이미 있으면 새 테스트를 시작할 수 없다. 먼저 `Restore`를 실행한 뒤 필요하면 `Clear Snapshot`을 실행한다.
- Unity 에디터에서 실제 씬/Game 뷰와 Quest/OpenXR 양쪽 눈을 확인해야 한다.
