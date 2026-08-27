# XR Hands 및 VR Sci-Fi UI 제작 도구 회의록

- 일자: 2026-06-28
- 주제: XR Hands 머테리얼과 VR용 Sci-Fi UI 제작 워크플로 구성
- 대상 프로젝트: 3D_UI_Test

## 목표

- Unity XR Hands 모델에 적용할 밝은 피부색 및 홀로그램 손 표현을 준비한다.
- 바닥, 벽, UI 기판에 사용할 플레인을 에디터에서 생성할 수 있게 한다.
- VR 공간에서 깊이감과 상호작용성을 전달하는 Sci-Fi 카드 UI를 빠르게 제작한다.
- 생성 이후에도 크기, 라운드, 색상, 투명도, 발광, 그림자 등을 개별 조정할 수 있게 한다.

## 논의 및 결정 사항

### XR Hands 머테리얼

- 셰이더는 표면의 렌더링 방식을 정의하고, 머테리얼은 셰이더의 색상과 수치 설정을 저장한다.
- 실제 손 모델에 씌우기 위해서는 머테리얼이 필요하며, 표현에 따라 적절한 셰이더를 선택한다.
- 기본 손 표현으로 밝은 피부색의 URP Lit 머테리얼을 사용한다.
- 보조 손, 추적 상태 또는 가상 손 표현으로 반투명 홀로그램 머테리얼을 사용한다.
- 반복 제작을 줄이기 위해 전용 Hand Material Maker를 제공한다.

### 플레인 제작 도구

- 플레인은 바닥, 벽, 패널처럼 평평한 면을 구성하는 메시다.
- XY, XZ, YZ 방향과 크기, 분할 수, 양면 여부, 피벗을 조정할 수 있게 한다.
- 카드 버튼과 UI 기판 제작을 위해 Corner Radius와 Corner Segments를 지원한다.
- 라운드가 적용된 경우 둥근 외곽선을 정확히 만들기 위해 전용 메시를 생성한다.

### VR Sci-Fi 카드 UI

- 단순 투명 패널보다 여러 깊이 레이어를 사용해 공간감을 명확히 전달한다.
- 기본 구성은 Glass Panel, Backplate, Edge Glow, Shadow Plate, Corner Markers와 실제 두께가 있는 Solid Frame으로 한다.
- 프레임은 URP Lit 기반의 높은 Metallic/Smoothness 값을 사용해 조명과 시점 변화가 드러나게 한다.
- 콘텐츠는 BackgroundContent, MainContent, ForegroundContent의 3단 깊이 앵커로 분리한다.
- HMD 위치를 카드 로컬 좌표로 변환해 각 콘텐츠 레이어가 서로 다른 비율로 이동하는 제한된 패럴랙스를 적용한다.
- 패럴랙스에는 최대 이동량과 지수 스무딩을 적용해 VR에서 과도한 시차와 떨림을 방지한다.
- 그림자는 실제 조명 그림자보다 오프셋된 반투명 패널 방식으로 구성해 VR에서 안정적으로 보이게 한다.
- Collider를 카드 크기에 맞춰 자동 갱신해 향후 XR 인터랙션 연결에 활용한다.
- 형태와 프레임은 `SciFiCardVisual`, 패럴랙스는 `SciFiCardDepthResponse`에서 개별 조정할 수 있게 한다.

## 제공 프리셋

1. Glass Basic: 투명 기판 중심의 기본 카드
2. Neon Edge: 외곽 발광을 강조한 카드
3. Floating Shadow: 그림자 오프셋으로 부유감을 강조한 카드
4. Hologram Panel: 높은 투명도와 발광을 사용하는 홀로그램 패널
5. Dense Control Button: 조작 버튼에 적합한 밀도 높은 카드
6. Warning Alert Card: 경고 및 중요 상태 표시용 카드
7. Corner Marker Card: 모서리 마커를 강조한 조준형 카드
8. Layered Depth Card: 다중 레이어의 깊이감을 강조한 카드

## 구현 결과

- `Tools/XR Hands/Hand Material Maker`
  - Skin, Ghost Hologram, Glow 머테리얼 생성
  - 선택한 Renderer에 생성 머테리얼 적용 가능
- `Tools/Geometry/Plane Maker`
  - 방향, 크기, 분할, 라운드, 양면 및 머테리얼 설정 지원
- `Tools/XR UI/Sci-Fi Card Maker`
  - 선택 프리셋 한 개 생성
  - 전체 예제 프리셋을 4 x 2 배치로 일괄 생성
  - 생성된 카드별 Inspector 세부 조정 지원
  - 모든 프리셋에 둥근 3D Solid Frame 및 URP Lit 금속 머테리얼 생성
  - 카드 크기에 비례한 프레임 폭과 두께 자동 설정
  - 3단 콘텐츠 앵커와 HMD 기반 패럴랙스 컴포넌트 자동 연결
  - 기존 생성 카드는 자동 마이그레이션하지 않으며, 개선 구조 확인 시 프리셋을 다시 생성해야 함

## 주요 산출물

- `Assets/Shaders/HandSkin_Light.mat`
- `Assets/Shaders/GhostHand_Hologram.mat`
- `Assets/Editor/XRHandMaterialMaker.cs`
- `Assets/Editor/PlaneMaker.cs`
- `Assets/Editor/SciFiCardMaker.cs`
- `Assets/Scripts/SciFiCardVisual.cs`
- `Assets/Scripts/SciFiCardDepthResponse.cs`

## 사용 흐름

1. Unity 상단 메뉴에서 필요한 Maker 창을 연다.
2. 손 머테리얼, 플레인 또는 Sci-Fi 카드 프리셋을 생성한다.
3. 생성된 오브젝트의 `SciFiCardVisual`에서 형태와 시각 효과를 조정한다.
4. 배경 장식은 `BackgroundContent`, 일반 UI와 텍스트는 `MainContent`, 강조 아이콘과 선택 효과는 `ForegroundContent` 아래에 배치한다.
5. `SciFiCardDepthResponse`에서 Parallax Strength, Max Offset, Smoothing을 실제 HMD 기준으로 조정한다.
6. 카드의 Collider에 XR Interaction Toolkit 컴포넌트를 연결해 버튼 동작을 구성한다.

## 후속 확인 사항

- Unity Editor에서 스크립트 컴파일 오류 여부를 확인한다.
- `Create All Example Cards`로 8개 프리셋을 다시 생성하고 각 카드에 SolidFrame 및 3단 콘텐츠 앵커가 생성되는지 확인한다.
- SolidFrame의 앞/뒤 면과 안쪽/바깥쪽 면 노멀 및 컬링이 올바른지 비스듬한 시점에서 확인한다.
- URP 환경에서 투명도, 렌더 큐, 발광 표현을 실제 HMD로 점검한다.
- 콘텐츠를 각 앵커에 배치한 뒤 실제 HMD 이동 시 패럴랙스 방향, 이동량, 멀미 유발 여부를 점검한다.
- 카드가 카메라를 향하는 방향과 양면 표시 필요 여부를 확인한다.
- XR Ray 및 Direct Interaction의 Collider 판정 범위를 조정한다.
- 필요 시 시점 반응형 Fresnel, 반사 하이라이트, 스캔라인을 포함한 전용 VR Shader Graph를 추가한다.

## 2026-06-28 추가 구현 기록

- 기존 카드가 여러 평면을 작은 Z 간격으로 쌓고 모두 Unlit 머테리얼을 사용해 VR에서 2D 이미지에 가깝게 보이는 문제를 확인했다.
- `SciFiCardVisual`에 라운드 외곽선을 따라 앞면, 뒷면, 바깥면, 안쪽면을 생성하는 입체 프레임 메시 로직을 추가했다.
- `SciFiCardMaker`가 프레임용 URP Lit 머테리얼을 별도로 만들고 Metallic 0.72, Smoothness 0.82를 설정하도록 변경했다.
- `SciFiCardDepthResponse`를 추가해 `Camera.main` 기준의 헤드 패럴랙스를 런타임에 계산하도록 구현했다.
- `Create Selected Preset`과 `Create All Example Cards`는 모두 공통 `CreateCard()` 경로를 사용하므로 새 기능이 동일하게 적용된다.
- 외부 명령 기반 Unity 컴파일 검증은 완료하지 못했다. Unity Editor Console 확인과 실제 HMD 시각 검증이 필요하다.
