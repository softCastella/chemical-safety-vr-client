# 2026-07-28 PPE 룸 문 유리·앞벽 개구부 작업 기록

- 날짜: 2026-07-28
- 프로젝트: `3D_UI_Test_home`
- Unity: 6000.4.8f1
- 대상 씬: `Assets/Scenes/3_PPE_Room.unity`
- 상태: 씬 저장 및 Unity Scene View 검증 완료

## 최종 결과

PPE 룸 문 이미지와 그 뒤의 `Front Wall`에 유리 윤곽과 같은 개구부를 만들었다. 현재는 별도의 가짜 외부 배경 면을 사용하지 않고, 문과 앞벽의 실제 구멍을 통해 뒤쪽 씬이 보인다.

- 문 개구부는 원본 문 이미지의 유리 픽셀 경계를 기준으로 작성했다.
- 문, 유리, 앞벽 모두 위·아래 모서리가 꺾인 동일한 윤곽을 사용한다.
- 앞벽은 문보다 뒤에 있으므로 낮은 시점에서 벽 하단이 다시 보이지 않도록 개구부 아래쪽에 추가 여유를 두었다.
- 외부 배경은 사용자가 문 바깥쪽 월드 공간에 직접 구성하며, 별도의 가짜 외부 뷰 면은 사용하지 않는다.
- 메시와 Transform은 씬에 저장된 값이 기준이며 `OnEnable` 또는 `OnValidate`에서 자동으로 다시 만들지 않는다.

## 최종 씬 구조

```text
PPE Background Room
├── Generated Image Room
│   └── Front Wall
│       └── Mesh: PPE Front Wall Octagonal Glass Hole
└── PPE Room Door Image
    ├── Mesh: PPE Door Image Octagonal Glass Hole
    ├── Door Window Glass
    │   ├── DoorWindowGlass
    │   └── Mesh: PPE Door Octagonal Glass
```

## 기준 치수

원본 텍스처:

- 파일: `Assets/UIs/Facilities/PPE_Room/Door/PPE_Room_Door.png`
- 해상도: `239 x 481`
- 감지된 유리 영역: X `59..176`, Y `61..418` (Unity bottom-origin 기준)

문 Quad 로컬 좌표로 환산한 개구부 기준값:

| 항목 | 값 |
|---|---:|
| 중심 X | `-0.006276` |
| 중심 Y | `-0.001040` |
| 너비 | `0.493724` |
| 높이 | `0.744283` |
| 상·하단 가로 모서리 절개 기준 | 약 `0.042` |
| 상·하단 세로 모서리 절개 기준 | 약 `0.021` |

`DoorWindowGlass`의 기본 `width`, `height`도 각각 `0.493724`, `0.744283`으로 맞췄다.

## 앞벽 하단 추가 절개

문과 앞벽 사이에는 깊이 차이가 있다. 문 개구부와 앞벽 개구부를 완전히 같은 투영 크기로 만들면 낮은 시점에서 앞벽의 아래쪽이 유리 안으로 다시 보일 수 있다.

최종 앞벽 메시에는 아래 설정을 적용했다.

| 항목 | 값 |
|---|---:|
| 추가 절개 대상 | 앞벽 개구부 하단 4개 꼭짓점 |
| 앞벽 로컬 여유 | `0.012` |
| 월드 기준 여유 | 약 `0.086m` |

문 이미지의 개구부와 유리 크기는 유지하고, `Front Wall`의 아래쪽만 더 내려 잘랐다. 따라서 문 테두리 모양은 변하지 않으면서 낮은 시점에서도 바닥이 하단 꺾인 테두리까지 보인다.

## 관련 오브젝트와 에셋

| 대상 | 역할 |
|---|---|
| `PPE Room Door Image` | 문 이미지 렌더러. 중앙에 유리 윤곽의 실제 메시 구멍이 있다. |
| `Door Window Glass` | 투명 유리 한 장. 문 앞쪽에 위치한다. |
| `Front Wall` | 문 뒤쪽 벽. 문과 같은 윤곽의 구멍과 하단 추가 여유가 있다. |
| `Assets/Generated/PPE/PPE_Door_Glass.mat` | 투명 유리 머티리얼. |

## 자동 덮어쓰기 방지

### `DoorWindowGlass`

- `OnEnable` 메시 재생성 제거
- `OnValidate` 메시 재생성 제거
- 메시 재생성은 컴포넌트 컨텍스트 메뉴의 `Rebuild Glass Mesh`를 명시적으로 실행할 때만 수행
- 생성된 메시의 `HideFlags`는 `None`이며 씬에 직렬화됨

### `PPEBackgroundRoom`

- 일반 `OnEnable` 경로에서 `CreateImageDoor(transform)` 호출 제거
- 기존 문과 유리의 Transform 및 메시를 씬 로드 시 자동으로 덮어쓰지 않음
- `CreateImageDoor(transform)`는 명시적인 `Rebuild Room` 흐름에서만 사용

## 유지보수 주의사항

1. `PPEBackgroundRoom > Rebuild Room`은 `Generated Image Room`을 다시 만들기 때문에 `Front Wall`의 개구부 메시가 일반 Quad로 돌아갈 수 있다. 실행 후에는 앞벽 개구부를 다시 적용해야 한다.
2. `Door Window Glass > Rebuild Glass Mesh`는 유리 메시만 갱신한다. 문 이미지와 앞벽의 개구부는 자동으로 따라가지 않는다.
3. 유리 크기나 위치를 바꿀 때는 아래 세 요소를 함께 맞춰야 한다.
   - `Door Window Glass`
   - `PPE Room Door Image`의 개구부
   - `Front Wall`의 개구부
4. 외부 배경은 문과 앞벽 구멍 너머의 월드 공간에 직접 구성한다. 문 유리 크기의 별도 배경 Quad나 가짜 풍경 셰이더를 추가하지 않는다.
5. 씬을 강제로 다시 열어 수정하지 않는다. 활성 씬의 저장되지 않은 변경을 보존한 상태에서 작업해야 한다.
6. `[ExecuteAlways]`, `OnEnable`, `OnValidate`에서 메시, 계층, Transform을 삭제·재생성하지 않는다.

## 문제 원인과 해결 요약

| 문제 | 원인 | 최종 해결 |
|---|---|---|
| 유리 뒤가 단색 파란색으로 보임 | 투명 유리 면이 두 장 겹침 | 중복 면 제거, 투명 유리 한 장만 유지 |
| 앞벽 전체 또는 문 양옆이 잘림 | 벽 전체 Quad를 잘못된 사각 개구부로 교체 | 외곽 Quad를 유지한 채 꺾인 내부 윤곽만 제거 |
| 벽 구멍 중앙이 막힘 | Y축 180도 회전된 벽에서 꼭짓점 순서가 반전되어 삼각형이 중앙을 가로지름 | 벽 로컬 좌표에서 상·하·좌·우 꼭짓점을 다시 분류해 메시 생성 |
| 유리와 개구부 크기가 미세하게 다름 | 초기 수동 치수와 실제 문 텍스처 유리 픽셀 경계가 다름 | 원본 텍스처 픽셀을 측정해 중심과 크기 결정 |
| 낮은 각도에서 하단 벽이 남음 | 문과 앞벽의 깊이 차이로 인한 시차 | 앞벽 개구부 하단만 월드 기준 약 `0.086m` 추가 절개 |
| Inspector 수정값이 되돌아감 | `OnEnable`/`OnValidate` 자동 메시 재생성 | 자동 재생성 제거, 컨텍스트 메뉴 방식으로 변경 |

## 검증 체크리스트

- [x] 문 이미지 중앙이 실제로 비어 있음
- [x] 앞벽 중앙이 실제로 비어 있음
- [x] 문 개구부가 원본 유리 픽셀 경계와 일치함
- [x] 유리 메시가 꺾인 윤곽을 유지함
- [x] 앞벽 하단이 낮은 시점에서도 유리 안으로 보이지 않음
- [x] 별도 외부 뷰 면 없이 사용자가 구성한 바깥 배경이 실제 구멍 너머로 보임
- [x] Unity Console 컴파일 오류 없음
- [ ] Quest/OpenXR 양안에서 최종 시차와 투명 유리 렌더링 확인

## 관련 파일

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/Scripts/DoorWindowGlass.cs`
- `Assets/UIs/Facilities/PPE_Room/Door/PPE_Room_Door.png`
- `Assets/Generated/PPE/PPE_Room_Door.mat`
- `Assets/Generated/PPE/PPE_Door_Glass.mat`
