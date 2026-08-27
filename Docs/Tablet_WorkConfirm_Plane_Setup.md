# Tablet Work Confirm Plane Setup

## 목적

PPE Room 씬의 Tablet 화면에 작업 확인서를 표시하기 위한 구성 기록이다.

작은 원본 이미지(`work_confirm.png`)를 Tablet atlas 텍스처에 직접 합성하면 글자가 뭉개져서 읽기 어렵다. 그래서 Tablet 화면 앞에 별도 Plane을 아주 얇게 붙이고, 그 Plane에 고해상도 확인서 이미지를 Unlit 머티리얼로 표시하는 방식으로 구성했다.

## 적용 씬

- `Assets/Scenes/3_PPE_Room.unity`

## Hierarchy 구성

- `Tablet`
  - `GeneratedPlane`

`GeneratedPlane`은 Tablet의 자식으로 배치되어 있으며, Tablet 화면 바로 앞에 붙어 보이도록 설정되어 있다.

주요 Transform 값:

- Parent: `Tablet`
- Local Position: `{x: 0, y: 0, z: 0.0072}`
- Local Scale: `{x: 0.000041, y: 0.00015, z: 0.001}`

기존 안내용 텍스트 자식들은 표시되지 않도록 비활성화했다.

- `GeneratedPlane/Text (TMP)`
- `GeneratedPlane/Text (TMP) (1)`

## 사용 Asset

Plane mesh:

- `Assets/Generated/Planes/GeneratedPlane_Mesh 4.asset`

Plane material:

- `Assets/Materials/PPE/Tablet/WorkConfirmPlane_Unlit.mat`

Tablet용 확인서 이미지:

- `Assets/Materials/PPE/Tablet/work_confirm_tablet_readable.png`

참고 원본 이미지:

- `Assets/UIs/Things/Docs/work_confirm.png`

## 머티리얼 설정

`WorkConfirmPlane_Unlit.mat`은 URP Unlit shader를 사용한다.

이유:

- Tablet 화면 UI처럼 보이게 하기 위해 조명 영향을 줄임
- 방 색/조명 반사 때문에 이미지가 어둡거나 푸르게 변하는 현상을 줄임
- 문서 글자 가독성을 우선함

현재 `_BaseMap` / `_MainTex`는 아래 이미지를 참조한다.

- `work_confirm_tablet_readable.png`

## Texture Import 설정

`work_confirm_tablet_readable.png`는 글자 선명도를 위해 다음 설정을 사용한다.

- Mip Map: Off
- Texture Compression: None
- Aniso Level: 16
- Max Texture Size: 2048

작은 원본 `work_confirm.png`도 압축을 꺼 두었지만, 실제 Tablet 표시에는 `work_confirm_tablet_readable.png`를 사용한다.

## 수정 방법

문서 내용을 바꾸려면 `work_confirm_tablet_readable.png`를 교체하거나 다시 생성하면 된다.

화면에서 위치가 살짝 어긋나면 `GeneratedPlane`의 Transform만 조정한다.

- 좌우/상하 위치: Local Position `x`, `y`
- 화면에서 떠 보이거나 겹치면: Local Position `z`
- 크기: Local Scale `x`, `y`

주의:

- Tablet atlas 텍스처에 다시 합성하지 않는다.
- `GeneratedPlane`을 삭제하지 않는다.
- 글자 가독성이 중요하면 작은 원본 PNG를 확대해서 쓰지 말고, Tablet용 고해상도 이미지를 새로 만든다.

## 현재 결론

가독성 문제는 Plane 방식 자체보다 원본 이미지 해상도 문제였다. 최종 구성은 원본 확인서 이미지를 그대로 확대하는 대신, Tablet에서 읽히도록 재구성한 고해상도 확인서 이미지를 Plane에 붙이는 방식이다.
