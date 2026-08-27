# 브랜드 홈 작품 캐러셀 2·3번 배너 잘림

## 현상

브랜드 홈의 `FEATURED RELEASES` 캐러셀에서 첫 번째 화학물질 안전훈련 VR 배너만 보이고, 다음 버튼이나 자동 전환 후 두 번째 SPARK 및 세 번째 LOOP 배너가 표시되지 않았다.

## 근본 원인

`server/public/site/index.html`에는 세 배너가 모두 존재했고 `carousel.js`도 인덱스와 `transform`을 정상적으로 변경하고 있었다. 그러나 `server/public/site/styles.css`의 후반부에 추가된 `.release-track` 규칙이 이동 트랙 자체에 `overflow:hidden`을 적용했다. 트랙 너비 밖에 나란히 배치된 두 번째와 세 번째 슬라이드가 트랙의 클리핑 영역에서 제거되어, 트랙을 이동해도 화면에 나타나지 않았다.

## 적용한 변경

- `.release-track`의 `overflow:hidden`을 제거했다.
- 외부 `.release-carousel`의 기존 클리핑은 유지해 현재 배너 한 장만 뷰포트에 표시되게 했다.
- 각 `.release-slide`의 둥근 모서리 클리핑도 유지했다.
- `server/test/site-carousel.test.js`에 세 슬라이드 존재 여부와 이동 트랙의 자체 클리핑 금지 검사를 추가했다.

## 영향 범위

- 브랜드 홈 `FEATURED RELEASES` 캐러셀에만 영향을 준다.
- 배너 내용, 자동 전환 간격, 이전·다음 버튼과 링크는 변경하지 않았다.
- 별도 시안인 `brand-v2`의 원칙 카드 여백 조정과는 독립된 수정이다.

## 완료한 검증

- 정적 확인: 홈 HTML에 `.release-slide` 세 개가 존재한다.
- 정적 확인: `.release-carousel`의 외부 클리핑은 유지되고 `.release-track`에는 `overflow:hidden`이 없다.
- 자동 확인: `npm test`로 서버 및 정적 사이트 테스트를 실행한다.

## 아직 필요한 수동 검증

- 데스크톱 브라우저에서 다음·이전 버튼으로 `01 / 03`, `02 / 03`, `03 / 03`이 순서대로 표시되는지 확인한다.
- 5초 자동 전환과 세 번째에서 첫 번째로 돌아가는 무한 전환을 확인한다.
- 모바일 너비에서 배너 이미지, 문구와 조작 버튼이 잘리지 않는지 확인한다.
