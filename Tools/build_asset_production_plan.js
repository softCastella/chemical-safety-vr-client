const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "..");
const samplePath = "C:\\Users\\user\\Downloads\\샘플_에셋제작기획서_게임디자인AI활용.html";
const planPath = "C:\\Users\\user\\Downloads\\누출사고_프로젝트_제안기획서.html";
const hmiPath = "C:\\Users\\user\\Downloads\\누출사고_HMI_시나리오_MVP.html";
const listPath = path.join(projectRoot, "output", "html", "3D_모델링_목록.html");
const outputPath = path.join(
  projectRoot,
  "output",
  "html",
  "에셋제작기획서_게임디자인AI활용_작성본.html",
);

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function field(row, name) {
  return (row.match(new RegExp(`data-field="${name}">([\\s\\S]*?)<\\/td>`))?.[1] || "")
    .replace(/<[^>]*>/g, "")
    .trim();
}

const ppeResources = new Set([
  "hazmat_suit",
  "hazmat_suit_3d_model",
  "hazmat_suit_hanger",
  "blue_rubber_gloves_3d_model_Clone1",
  "construction_helmet_3d_model_Clone1",
  "gas_mask_3d_model_Clone1",
  "rubber_boots_3d_model",
  "rubber_boots_3d_model_Clone1",
  "tactical_harness_3d_model",
  "orange_tape_roll_3d_model",
]);

function category(asset) {
  if (ppeResources.has(asset.resource)) return { code: "A", label: "PPE용품" };
  if (asset.scene === "PPE룸") return { code: "B", label: "PPE룸 배경" };
  if (asset.scene === "혼합기동") return { code: "C", label: "혼합기동 배경" };
  return { code: "D", label: "혼합기 내부 배경" };
}

function plannedMethod(asset) {
  return ppeResources.has(asset.resource) ? "Image-to-3D 권장" : "Text/Image-to-3D";
}

function promptKeywords(asset) {
  if (asset.resource === "tactical_harness_3d_model") {
    return "backpack-style supplied-air respirator blower harness / 공기 호스·전동 송풍기 착용 보호구 / low poly / game ready / single object";
  }
  const cleanName = asset.resource
    .replaceAll("_3d_model", "")
    .replaceAll("_Clone1", "")
    .replaceAll("_", " ")
    .replaceAll("+", " ");
  return `${cleanName} / ${asset.usage} / low poly / game ready / single object`;
}

function textureBudget(asset) {
  const high = ppeResources.has(asset.resource) || /4k|5k/.test(asset.poly);
  return high ? "2K PBR 권장" : "1K PBR 권장";
}

function interaction(asset) {
  if (asset.resource === "tactical_harness_3d_model") {
    return "Grab / 호스 연결부 점검 / 등지게 착용 확인";
  }
  if (ppeResources.has(asset.resource)) return "Grab / 착용 확인";
  return "고정 배치";
}

const sampleHtml = fs.readFileSync(samplePath, "utf8");
const sourceStyle = sampleHtml.match(/<style>[\s\S]*?<\/style>/i)?.[0];
if (!sourceStyle) throw new Error("샘플 문서의 CSS를 찾지 못했습니다.");

const planHtml = fs.readFileSync(planPath, "utf8");
const hmiHtml = fs.readFileSync(hmiPath, "utf8");
if (!planHtml.includes("화학물질 누출 초동대응") || !hmiHtml.includes("25% 수산화나트륨")) {
  throw new Error("프로젝트 기획서 또는 HMI 시나리오 내용을 확인하지 못했습니다.");
}

const listHtml = fs.readFileSync(listPath, "utf8");
const assets = [...listHtml.matchAll(/<tr class="data-row"[\s\S]*?<\/tr>/g)].map((match) => {
  const row = match[0];
  const asset = {
    resource: field(row, "resource"),
    scene: field(row, "scene"),
    usage: field(row, "usage"),
    production: field(row, "production"),
    poly: field(row, "poly"),
    purpose: field(row, "purpose"),
    interaction: field(row, "interaction"),
  };
  asset.category = category(asset);
  return asset;
});

if (assets.length !== 40) {
  throw new Error(`현재 목록이 40개가 아닙니다: ${assets.length}개`);
}

const totalTri = assets.reduce((sum, asset) => {
  const value = parseFloat(asset.poly);
  return sum + (/k/i.test(asset.poly) ? value * 1000 : value);
}, 0);

const categoryCounts = assets.reduce((counts, asset) => {
  counts[asset.category.code] = (counts[asset.category.code] || 0) + 1;
  return counts;
}, {});

const confirmedRows = assets.map((asset, index) => `
  <tr>
    <td class="num">${String(index + 1).padStart(2, "0")}</td>
    <td>${escapeHtml(asset.resource)}</td>
    <td><span class="chip chip-a">${asset.category.code} ${escapeHtml(asset.category.label)}</span></td>
    <td>${escapeHtml(asset.scene)}</td>
    <td>필수</td>
  </tr>`).join("");

const generationRows = assets.map((asset, index) => `
  <tr>
    <td class="num">${String(index + 1).padStart(2, "0")}</td>
    <td>${escapeHtml(asset.resource)}</td>
    <td>${plannedMethod(asset)}</td>
    <td>실제 기록 입력</td>
    <td>${escapeHtml(promptKeywords(asset))}</td>
  </tr>`).join("");

const interactionRows = assets.map((asset, index) => {
  const isPpe = ppeResources.has(asset.resource);
  return `
  <tr>
    <td class="num">${String(index + 1).padStart(2, "0")}<br />${escapeHtml(asset.resource)}</td>
    <td>${escapeHtml(asset.scene)} · ${escapeHtml(asset.usage)}</td>
    <td>${interaction(asset)}</td>
    <td>${isPpe ? "착용 상태 반영 및 다음 단계 진행" : "산업 현장 맥락과 공간 인지 보강"}</td>
    <td>${isPpe ? "미착용 시 착용 순서 안내" : "—"}</td>
  </tr>`;
}).join("");

const resourceRows = assets.map((asset, index) => `
  <tr>
    <td class="num">${String(index + 1).padStart(2, "0")}</td>
    <td>${escapeHtml(asset.resource)}.fbx</td>
    <td>${asset.category.code}</td>
    <td>FBX</td>
    <td class="num">${escapeHtml(asset.poly)}</td>
    <td>${textureBudget(asset)}</td>
    <td>${escapeHtml(asset.scene)} · ${escapeHtml(asset.usage)}</td>
    <td>${interaction(asset)}</td>
    <td>${escapeHtml(asset.purpose)}</td>
  </tr>`).join("");

const documentHtml = `<!DOCTYPE html>
<html lang="ko">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>화학물질 누출 초동대응 VR · 에셋 제작 기획서 · 작성본</title>
  ${sourceStyle}
</head>
<body>
  <main class="wrap">
    <section class="header">
      <div>
        <div class="eyebrow">비NCS 실기 · 게임디자인(AI활용) 능력단위 · 제출 산출물</div>
        <h1>에셋 제작 기획서<span class="badge">작성본</span></h1>
        <p class="desc">화학물질 누출 초동대응 VR 시뮬레이션 · <strong>Tripo3D 기반 FBX 에셋 40종 제작</strong><br />
          기존 프로젝트 기획과 HMI 시나리오의 안전관리요원 체험 흐름을 현재 PPE룸, 혼합기동, 혼합기 내부 씬 구성에 맞춰 정리했다.</p>
      </div>
      <div class="stamp">
        프로젝트 · <b>화학물질 누출 초동대응 VR 시뮬레이션</b><br />
        사고 설정 · <b>Line A-03 · 25% NaOH 누출</b><br />
        대상 씬 · <b>PPE룸 · 혼합기동 · 혼합기 내부</b><br />
        작성자 · <b>작성자 입력</b><br />
        작성일 · <b>2026.07.30</b><br />
        버전 · <b>v1.0</b>
      </div>
    </section>

    <div class="guide">
      <strong>작성 기준</strong> — <code>누출사고_프로젝트_제안기획서.html</code>과 <code>누출사고_HMI_시나리오_MVP.html</code>의 교육 목적·상황·체험 흐름을 사용하고, 샘플 문서는 CSS와 문1~문5 양식만 참고했다.<br />
      Unity 기본 도형, Quad, Camera, UI, 기존 프로젝트 FBX는 제외하고 <strong>Tripo3D에서 제작한 FBX 40종</strong>만 정리했다.
      생성 화면, 실제 시도 횟수, Import 직후 크기처럼 작업 증빙이 필요한 값은 임의로 만들지 않고 <strong>실제 기록 입력</strong>으로 표시했다.
    </div>

    <section class="meta-box">
      <div class="meta-item"><div class="meta-label">제작 도구</div><div class="meta-value">Tripo3D (Text-to-3D / Image-to-3D)</div></div>
      <div class="meta-item"><div class="meta-label">제작 에셋</div><div class="meta-value">40종 (A ${categoryCounts.A} · B ${categoryCounts.B} · C ${categoryCounts.C} · D ${categoryCounts.D})</div></div>
      <div class="meta-item"><div class="meta-label">타깃 플랫폼</div><div class="meta-value">Meta Quest / Android · Unity 6 · OpenXR</div></div>
      <div class="meta-item"><div class="meta-label">시나리오 · 폴리 예산</div><div class="meta-value">25% NaOH 누출 · 총 ${totalTri.toLocaleString("ko-KR")} tri</div></div>
    </section>

    <article class="card">
      <div class="card-top"><span class="no">문1</span><h2>에셋 도출 및 제작 목록 확정</h2></div>
      <p class="card-sub">교육 흐름에 필요한 오브젝트를 씬별로 도출하고 Tripo3D FBX 제작 범위를 확정한다.</p>

      <h3>1-1. 시나리오 단계별 필요 오브젝트</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>단계</th><th>사용자 행동</th><th>필요 오브젝트</th><th>없으면?</th></tr></thead>
        <tbody>
          <tr><td class="num">S1<br />PPE룸 준비</td><td>현장 안전관리요원으로서 보호구와 준비 공간을 확인한다</td><td>PPE용품 10종, 캐비닛, 로커, 벤치, 위생·안전 소품</td><td><span class="chip chip-bad">성립 불가</span> 작업 전 안전 준비 단계</td></tr>
          <tr><td class="num">S2<br />경보·상황 확인</td><td>태블릿에서 Line A-03 누출 경보, CCTV와 사고 원인을 확인한다</td><td>PPE룸 배경 소품과 태블릿 주변 환경</td><td><span class="chip chip-bad">성립 불가</span> 상황 인지 단계</td></tr>
          <tr><td class="num">S3<br />혼합기동 진입</td><td>현장 이동 후 작업 구역, 환기 장비, 접근 구조물을 확인한다</td><td>덕트, 계단, 안전 콘, 제어반</td><td><span class="chip chip-warn">축소 가능</span> 현장 맥락이 약해짐</td></tr>
          <tr><td class="num">S4<br />작업 전 안전 확인</td><td>LOTO 장치, 외부 감시인, 가스측정기와 무전 상태를 확인한다</td><td>자물쇠, 작업자, 산소·가스측정기, 무전기</td><td><span class="chip chip-bad">성립 불가</span> 안전 확인 흐름</td></tr>
          <tr><td class="num">S5<br />혼합기 내부 작업</td><td>손전등과 청소 도구를 확인하고 내부 점검·작업을 진행한다</td><td>브러시, 전술 손전등</td><td><span class="chip chip-warn">대체 가능</span> 기본 도구로 대체 가능</td></tr>
        </tbody>
      </table></div>

      <h3>1-2. 제작 확정 목록 (40종)</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>No</th><th>에셋명</th><th>분류</th><th>사용 단계</th><th>우선순위</th></tr></thead>
        <tbody>${confirmedRows}</tbody>
      </table></div>

      <h3>1-3. 제작 범위 · 제외 범위</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>제외 항목</th><th>제외 사유</th><th>대체 방안</th></tr></thead>
        <tbody>
          <tr><td>바닥·벽·천장·문</td><td>Tripo3D 제작 FBX가 아닌 씬 구조물</td><td>Unity Quad 또는 기존 씬 지오메트리 사용</td></tr>
          <tr><td>손 모델·스킨드 메시</td><td>XR Hands와 연결되는 프로젝트 전용 FBX</td><td>기존 손 추적 모델과 머티리얼 전환 사용</td></tr>
          <tr><td>문서·표지·거울 화면</td><td>3D 모델보다 Quad·Texture·Camera 방식이 적합</td><td>Unity 월드 스페이스 UI와 텍스처 사용</td></tr>
          <tr><td>혼합기 셸·밸브 기본 구조</td><td>프로젝트에 이미 존재하는 FBX 및 Primitive 조합</td><td>기존 프로젝트 모델 재사용</td></tr>
        </tbody>
      </table></div>
      <div class="note"><strong>판단 기준</strong> — 이 목록은 <strong>Tripo3D에서 새로 제작한 FBX</strong>만 포함한다. PPE용품은 교육 행동에 직접 사용하고, 나머지는 씬의 산업 현장 맥락을 형성하는 배경용 소품으로 사용한다.</div>
    </article>

    <article class="card">
      <div class="card-top"><span class="no">문2</span><h2>Tripo3D 프롬프트 설계 및 생성 과정</h2></div>
      <p class="card-sub">규격이 중요한 PPE는 참고 이미지 기반, 배경 소품은 텍스트 또는 이미지 기반 생성을 우선한다.</p>

      <h3>2-1. 프롬프트 기록표 (대표 에셋 · 방독면)</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>차수</th><th>방식</th><th>프롬프트 전문</th><th>결과</th><th>채택</th></tr></thead>
        <tbody>
          <tr><td class="num">1차</td><td>Text-to-3D</td><td>industrial full-face gas mask, two filter cartridges, transparent visor, black rubber, low poly, game ready, single object</td><td>실제 생성 결과 입력</td><td><span class="chip chip-warn">확인 필요</span></td></tr>
          <tr><td class="num">2차</td><td>Image-to-3D</td><td>산업용 전면형 방독면 참고 이미지 + keep two cartridges, clean visor and straps, isolated object</td><td>실제 생성 결과 입력</td><td><span class="chip chip-warn">확인 필요</span></td></tr>
          <tr><td class="num">최종</td><td>Refine</td><td>keep silhouette, simplify hidden geometry, clean topology, preserve PBR materials</td><td>채택 모델 스크린샷과 판단 근거 입력</td><td><span class="chip chip-warn">기록 입력</span></td></tr>
        </tbody>
      </table></div>

      <h3>2-2. 전체 에셋 생성 요약</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>No</th><th>에셋명</th><th>방식</th><th>시도 횟수</th><th>프롬프트 계획 키워드</th></tr></thead>
        <tbody>${generationRows}</tbody>
      </table></div>

      <h3>2-3. 생성 화면 스크린샷</h3>
      <div class="shot-grid">
        <div class="shot"><span><b>[스크린샷 ①]</b>Tripo3D 입력 프롬프트와 생성 결과<br />(실제 작업 화면으로 교체)</span></div>
        <div class="shot"><span><b>[스크린샷 ②]</b>재시도 전·후 비교<br />(형태가 개선된 부분 표시)</span></div>
        <div class="shot"><span><b>[스크린샷 ③]</b>채택 모델 정면·측면<br />(뷰어 회전 화면)</span></div>
      </div>
    </article>

    <article class="card">
      <div class="card-top"><span class="no">문3</span><h2>생성 결과 검수 · 목적 적합성 판정</h2></div>
      <p class="card-sub">실물 형태, 스케일, 실루엣, 폴리 예산을 확인한 뒤 채택 여부를 기록한다.</p>

      <h3>3-1. 분류별 형태 검수 기준</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>분류</th><th>대상</th><th>핵심 검수 항목</th><th>교육 영향</th><th>판정</th></tr></thead>
        <tbody>
          <tr><td>A PPE용품</td><td>10종</td><td>착용 방향, 주요 부품, 좌우·앞뒤 구분, 손에 잡히는 실루엣</td><td>높음 — 잘못된 형태는 착용 절차 오인 가능</td><td><span class="chip chip-warn">실물 대조 필요</span></td></tr>
          <tr><td>B PPE룸 배경</td><td>13종</td><td>크기 비례, 바닥 접촉, 실루엣, 재질 인지성</td><td>낮음 — 배경 맥락 중심</td><td><span class="chip chip-warn">씬 검수 필요</span></td></tr>
          <tr><td>C 혼합기동 배경</td><td>15종</td><td>산업 설비 형태, 배치 안정성, 원거리 가독성</td><td>중간 — 안전 절차 맥락 제공</td><td><span class="chip chip-warn">씬 검수 필요</span></td></tr>
          <tr><td>D 혼합기 내부 배경</td><td>2종</td><td>손전등·브러시 크기와 혼합기 내부 대비</td><td>중간 — 내부 작업 도구 인지</td><td><span class="chip chip-warn">씬 검수 필요</span></td></tr>
        </tbody>
      </table></div>

      <h3>3-2. 스케일 검증 계획 <span style="text-transform:none;color:var(--sub);font-weight:400;">— 기준: 플레이어 시점 높이 1.65m</span></h3>
      <div class="table-scroll"><table>
        <thead><tr><th>분류</th><th>비교 기준</th><th>검증 방법</th><th>기록할 값</th><th>완료 조건</th></tr></thead>
        <tbody>
          <tr><td>PPE용품</td><td>성인 머리·손·발·몸 치수</td><td>XR Origin 및 손 모델 옆에 배치</td><td>Import 크기 · 보정 배율</td><td>착용 위치와 실루엣이 자연스러움</td></tr>
          <tr><td>가구·캐비닛</td><td>성인 키 1.65m 캡슐</td><td>바닥 정렬 후 높이 비교</td><td>폭·높이·깊이</td><td>통행 및 시야를 방해하지 않음</td></tr>
          <tr><td>설비·계단·덕트</td><td>혼합기동 기존 구조물</td><td>씬 기준 모델과 나란히 배치</td><td>월드 스케일 · 간격</td><td>산업 설비 비례와 동선이 자연스러움</td></tr>
          <tr><td>소형 도구</td><td>XR 손 모델</td><td>손바닥 및 Grab 포즈와 비교</td><td>길이 · 그립 두께</td><td>한 손 조작 크기에 적합</td></tr>
        </tbody>
      </table></div>
      <div class="note"><strong>확인 방법</strong> — Unity 씬에 1.65m 기준 캡슐과 XR 손 모델을 배치해 육안 및 Inspector 수치로 대조한다. 실제 Import 크기와 보정 배율은 작업 후 이 문서에 입력한다.</div>

      <h3>3-3. 검수 스크린샷</h3>
      <div class="shot-grid">
        <div class="shot"><span><b>[스크린샷 ④]</b>PPE 실물 참고 ↔ 생성 결과 비교</span></div>
        <div class="shot"><span><b>[스크린샷 ⑤]</b>1.65m 기준 캡슐 옆 40종 스케일 대조</span></div>
        <div class="shot"><span><b>[스크린샷 ⑥]</b>Unity Inspector 스케일 및 Mesh 정보</span></div>
      </div>
    </article>

    <article class="card">
      <div class="card-top"><span class="no">문4</span><h2>VR 인터랙션 활용 설계</h2></div>
      <p class="card-sub">PPE용품은 착용 행동에 연결하고, 그 외 에셋은 배경용 소품으로 씬의 산업 현장 맥락을 보강한다.</p>

      <h3>4-1. 에셋별 활용 설계표</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>No</th><th>배치 단계 · 위치</th><th>상호작용</th><th>성공 시 반응</th><th>실패 시 피드백</th></tr></thead>
        <tbody>${interactionRows}</tbody>
      </table></div>

      <h3>4-2. 상호작용 흐름</h3>
      <div class="flow">[PPE룸]                         [혼합기동]                       [혼합기 내부]
PPE용품 확인·Grab ─→ 착용 상태 확인 ─→ 작업 구역·LOTO 맥락 확인 ─→ 가스 측정·감시 확인 ─→ 내부 작업 도구 확인
        │                    │
        └─ 미착용 ───────────┴─ 다음 단계 진행 제한 + 착용 순서 안내</div>

      <h3>4-3. 기획 의도 연결</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>학습 목표</th><th>담당 에셋</th><th>연결 방식</th></tr></thead>
        <tbody>
          <tr><td>작업 전 PPE 착용 순서 이해</td><td>A PPE용품 10종</td><td>Grab과 착용 확인을 통해 보호구 준비 절차를 행동으로 학습</td></tr>
          <tr><td>작업 전 준비 공간 인지</td><td>B PPE룸 배경 13종</td><td>캐비닛·벤치·안전 소품으로 준비 구역의 기능을 시각화</td></tr>
          <tr><td>혼합기 작업 구역의 안전 맥락 이해</td><td>C 혼합기동 배경 15종</td><td>환기, 접근, 구획, LOTO, 가스 측정 관련 소품을 현장에 배치</td></tr>
          <tr><td>밀폐공간 내부 작업 상황 인지</td><td>D 혼합기 내부 배경 2종</td><td>브러시와 손전등으로 청소·점검 작업 맥락을 보강</td></tr>
        </tbody>
      </table></div>

      <div class="shot-grid">
        <div class="shot"><span><b>[스크린샷 ⑦]</b>PPE Grab 및 착용 위치</span></div>
        <div class="shot"><span><b>[스크린샷 ⑧]</b>Collider·Socket 설정 Inspector</span></div>
        <div class="shot"><span><b>[스크린샷 ⑨]</b>PPE룸 → 혼합기동 → 내부 동선 전경</span></div>
      </div>
    </article>

    <article class="card">
      <div class="card-top"><span class="no">문5</span><h2>리소스 목록 및 산출물 패키지</h2></div>
      <p class="card-sub">Tripo3D에서 제작한 FBX 40종을 개발자가 바로 확인할 수 있도록 정리한다.</p>

      <h3>5-1. 리소스 목록표</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>No</th><th>파일명</th><th>분류</th><th>포맷</th><th>Tri 예산</th><th>텍스처 예산</th><th>사용 위치</th><th>상호작용</th><th>용도</th></tr></thead>
        <tbody>${resourceRows}</tbody>
      </table></div>

      <h3>5-2. 네이밍 규칙 및 폴더 구조</h3>
      <div class="flow">규칙: [분류접두사]_[오브젝트명]_[번호].fbx
A→PPE_ | B→PPEBG_ | C→MIXER_ | D→INNER_

/제출_게임디자인AI활용
   ├ 00_에셋제작기획서.pdf
   ├ 10_Models/        Tripo3D FBX 40종
   ├ 20_Textures/      1K·2K PBR 텍스처
   ├ 30_Screenshots/   생성과정 / 검수 / 인터랙션
   ├ 40_Prompts/       에셋별 실제 프롬프트 기록
   └ 50_ResourceList/  3D_모델링_목록.html · CSV</div>

      <h3>5-3. 최적화 검토 의견</h3>
      <ul>
        <li>40종의 목표 예산 합계는 <strong>${totalTri.toLocaleString("ko-KR")} tri</strong>이며, 개별 모델은 <strong>500~5,000 tri</strong> 범위로 계획했다.</li>
        <li>가장 큰 예산은 작업자 모델 <strong>5,000 tri</strong>, 대형 캐비닛·계단·제어반·방호복은 <strong>4,000 tri</strong>로 설정했다.</li>
        <li>자물쇠·테이프·행거처럼 작은 소품은 <strong>500~800 tri</strong>, 손전등·측정기·무전기는 <strong>1,000 tri</strong>를 기준으로 한다.</li>
        <li>PPE와 대형 모델은 2K, 작은 배경 소품은 1K 텍스처를 권장한다. 실제 Import 후 품질 차이를 확인해 낮출 수 있다.</li>
        <li>목표 예산을 초과한 Tripo3D 결과물은 Blender Decimate 또는 Unity Mesh 최적화를 적용하고 실루엣 손상 여부를 재검수한다.</li>
      </ul>

      <h3>5-4. AI 활용 기록</h3>
      <div class="table-scroll"><table>
        <thead><tr><th>문항</th><th>사용 도구</th><th>AI에게 맡긴 것</th><th>본인이 검증·수정할 것</th></tr></thead>
        <tbody>
          <tr><td>문1</td><td>대화형 AI</td><td>Unity 씬 요소에서 Tripo3D 제작 후보 분류</td><td>실제 씬 존재 여부, PPE용품과 배경 소품 구분, 제외 범위 확인</td></tr>
          <tr><td>문2</td><td>Tripo3D</td><td>Text/Image-to-3D 기반 형상 생성</td><td>실제 프롬프트, 시도 횟수, 채택 이유와 생성 화면 기록</td></tr>
          <tr><td>문3</td><td>—</td><td>—</td><td>실물 대조, Unity 스케일 측정, 폴리 수 확인과 최종 판정</td></tr>
          <tr><td>문4</td><td>대화형 AI</td><td>씬별 활용 정보와 상호작용 표 초안 정리</td><td>PPE 착용 흐름과 실제 XR 동작을 프로젝트에서 검증</td></tr>
          <tr><td>문5</td><td>대화형 AI</td><td>40종 리소스 목록과 폴리 예산 초안 정리</td><td>실제 FBX 파일명·텍스처 크기·최종 Tri 수를 Import 후 확정</td></tr>
        </tbody>
      </table></div>
      <div class="note"><strong>AI 활용 원칙</strong> — AI는 목록 정리와 초안 작성에 사용하고, 실제 모델 품질·스케일·폴리 수·교육 적합성 판정은 Unity 프로젝트와 실물 자료를 기준으로 직접 검증한다.</div>
    </article>

    <section class="footer">
      <strong style="color:var(--text)">평가표 대응 확인</strong><br />
      문1 → 에셋 도출 · 제작 목록 · 제외 사유 &nbsp;·&nbsp;
      문2 → 프롬프트 계획 · 생성 기록 자리 &nbsp;·&nbsp;
      문3 → 형태·스케일 검수 기준 &nbsp;·&nbsp;
      문4 → 배치·상호작용 · 기획 의도 &nbsp;·&nbsp;
      문5 → 40종 리소스 목록 · 최적화 · AI 기록<br /><br />
      에셋 제작 기획서 · 게임디자인(AI활용) 능력단위 · 2026.07.30 · v1.0
    </section>
  </main>
</body>
</html>`;

fs.writeFileSync(outputPath, documentHtml, "utf8");
console.log(`작성본 생성 완료: ${outputPath}`);
console.log(`에셋 ${assets.length}종 · 목표 ${totalTri.toLocaleString("ko-KR")} tri`);
