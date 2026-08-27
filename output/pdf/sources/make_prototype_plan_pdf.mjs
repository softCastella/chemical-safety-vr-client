import { mkdir, rm, writeFile } from "node:fs/promises";
import { spawnSync } from "node:child_process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, "..");
const workDir = join(root, "tmp", "pdfs", "prototype_plan_0813");
const qaDir = join(workDir, "renders");
const outDir = join(root, "output", "pdf");
const htmlPath = join(workDir, "화학물질_안전교육_VR_프로토타입_기획서_0813.html");
const pdfPath = join(outDir, "화학물질_안전교육_VR_프로토타입_기획서_0813_v1.5_main.pdf");
const chrome = "C:/Program Files/Google/Chrome/Application/chrome.exe";

const img = (name) => pathToFileURL(join(root, "img", name)).href;

const css = String.raw`
  @page { size: A4; margin: 0; }
  * { box-sizing: border-box; }
  :root {
    --navy: #123844;
    --teal: #177b76;
    --teal2: #2b9790;
    --ink: #24363b;
    --muted: #63757b;
    --line: #cbd8da;
    --paper: #ffffff;
    --soft: #eff6f5;
    --soft2: #f6f8f8;
    --amber: #d99522;
    --amber-soft: #fff5df;
    --red: #a8493d;
    --red-soft: #fff0ed;
    --green: #2b7757;
    --green-soft: #edf7f1;
  }
  html, body { margin: 0; padding: 0; background: #e8edef; color: var(--ink); }
  body { font-family: "Malgun Gothic", "맑은 고딕", Arial, sans-serif; }
  .page {
    position: relative;
    width: 210mm;
    height: 297mm;
    margin: 0 auto 8px;
    padding: 14mm 16mm 15mm;
    background: var(--paper);
    overflow: hidden;
    page-break-after: always;
  }
  .page:last-child { page-break-after: auto; }
  .topline {
    display: flex; align-items: center; justify-content: space-between;
    padding-bottom: 3mm; margin-bottom: 5mm; border-bottom: 1px solid var(--line);
    color: var(--muted); font-size: 8pt; letter-spacing: .04em;
  }
  .topline strong { color: var(--teal); }
  .section-no { color: var(--teal); font-size: 8.5pt; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
  h1, h2, h3, h4, p { margin-top: 0; }
  h1 { margin-bottom: 4mm; color: var(--navy); font-size: 29pt; line-height: 1.25; letter-spacing: -.035em; }
  h2 { margin: 0 0 4mm; color: var(--navy); font-size: 20pt; line-height: 1.28; letter-spacing: -.025em; }
  h3 { margin: 4.2mm 0 2.3mm; color: var(--navy); font-size: 12.5pt; line-height: 1.32; }
  h4 { margin: 2.8mm 0 1.6mm; color: var(--teal); font-size: 10.2pt; }
  p { margin-bottom: 2.6mm; font-size: 9.3pt; line-height: 1.58; }
  .lead { color: #3f555b; font-size: 12pt; line-height: 1.55; }
  .small { color: var(--muted); font-size: 8pt; line-height: 1.5; }
  .micro { color: var(--muted); font-size: 7.2pt; line-height: 1.45; }
  .strong { font-weight: 800; color: var(--navy); }
  .accent { color: var(--teal); font-weight: 800; }
  .tag {
    display: inline-block; margin: 0 1.5mm 1.5mm 0; padding: 1.2mm 2.4mm;
    border-radius: 9px; background: var(--soft); color: var(--teal); font-size: 7.5pt; font-weight: 800;
  }
  .tag.amber { background: var(--amber-soft); color: #8b5a0b; }
  .tag.red { background: var(--red-soft); color: var(--red); }
  .tag.dark { background: var(--navy); color: white; }
  .rule { width: 24mm; height: 1.4mm; margin: 4mm 0 7mm; background: var(--teal); }
  .footer {
    position: absolute; left: 16mm; right: 16mm; bottom: 7.5mm;
    display: flex; justify-content: space-between; align-items: center;
    padding-top: 2.7mm; border-top: 1px solid var(--line); color: var(--muted); font-size: 7.2pt;
  }
  .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 4mm; }
  .grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 3mm; }
  .card { padding: 3.4mm; border: 1px solid var(--line); border-radius: 2.5mm; background: var(--soft2); }
  .card h3, .card h4 { margin-top: 0; }
  .card.teal { border-color: #9dc9c5; background: var(--soft); }
  .card.amber { border-color: #ead19b; background: var(--amber-soft); }
  .card.red { border-color: #e0b6af; background: var(--red-soft); }
  .callout {
    padding: 3mm 3.5mm; border-left: 1.4mm solid var(--teal); border-radius: 1.5mm;
    background: var(--soft); color: #274b4e; font-size: 9pt; line-height: 1.55;
  }
  .callout.amber { border-left-color: var(--amber); background: var(--amber-soft); color: #694b17; }
  .callout.red { border-left-color: var(--red); background: var(--red-soft); color: #693631; }
  .callout strong { color: inherit; }
  ul, ol { margin: 1.5mm 0 0; padding-left: 5mm; font-size: 8.8pt; line-height: 1.48; }
  li { margin: 0 0 1.5mm; }
  .tight li { margin-bottom: .9mm; }
  table { width: 100%; border-collapse: collapse; table-layout: fixed; margin: 2.4mm 0 4mm; }
  th, td { border: 1px solid var(--line); padding: 2mm 2.2mm; vertical-align: middle; word-break: keep-all; overflow-wrap: anywhere; }
  th { background: var(--navy); color: white; font-size: 7.8pt; line-height: 1.35; text-align: center; }
  td { color: #32474c; font-size: 7.7pt; line-height: 1.43; }
  tr:nth-child(even) td { background: #fbfcfc; }
  td.center { text-align: center; }
  td.label { background: var(--soft) !important; color: var(--navy); font-weight: 800; }
  .compact th, .compact td { padding: 1.55mm 1.8mm; font-size: 7.35pt; line-height: 1.38; }
  .steps td:first-child { width: 8%; text-align: center; color: var(--teal); font-size: 10pt; font-weight: 900; }
  .steps td:nth-child(2) { width: 18%; font-weight: 800; color: var(--navy); }
  .checklist { display: grid; grid-template-columns: 1fr 1fr; gap: 1.4mm 4mm; margin-top: 2mm; }
  .check { position: relative; padding-left: 5mm; font-size: 8.2pt; line-height: 1.45; }
  .check::before { content: "✓"; position: absolute; left: 0; top: 0; color: var(--teal); font-weight: 900; }
  .flow { display: flex; align-items: stretch; gap: 1.2mm; margin: 3mm 0 4mm; }
  .flow .node { flex: 1; min-height: 15mm; padding: 2mm 1mm; display: flex; align-items: center; justify-content: center; text-align: center; border: 1px solid #a9c8c6; border-radius: 2mm; background: var(--soft); color: var(--navy); font-size: 7.2pt; line-height: 1.35; font-weight: 800; }
  .flow .arrow { display: flex; align-items: center; color: var(--teal); font-size: 11pt; font-weight: 800; }
  .metric-strip { display: grid; grid-template-columns: repeat(4, 1fr); border: 1px solid var(--line); border-radius: 2.5mm; overflow: hidden; margin: 4mm 0 5mm; }
  .metric { padding: 3mm; background: var(--soft2); border-right: 1px solid var(--line); }
  .metric:last-child { border-right: 0; }
  .metric b { display: block; margin-bottom: 1mm; color: var(--teal); font-size: 12pt; }
  .metric span { color: var(--muted); font-size: 7.4pt; line-height: 1.35; }
  .asset-row { display: grid; grid-template-columns: 34mm 34mm 1fr; gap: 3mm; align-items: stretch; margin: 3mm 0 4mm; }
  .asset-card { height: 31mm; display: flex; align-items: center; justify-content: center; overflow: hidden; border: 1px solid var(--line); border-radius: 2.2mm; background: #3a3e40; }
  .asset-card img { width: 100%; height: 100%; object-fit: contain; }
  .asset-copy { padding: 3mm; border-radius: 2.2mm; background: var(--soft); border: 1px solid #b8d3d1; }
  .asset-copy strong { display: block; margin-bottom: 1mm; color: var(--navy); font-size: 10pt; }
  .timeline { display: grid; grid-template-columns: repeat(5, 1fr); gap: 2mm; margin: 3mm 0 4mm; }
  .milestone { min-height: 24mm; padding: 2.6mm; border-top: 1.4mm solid var(--teal); background: var(--soft2); }
  .milestone b { color: var(--teal); font-size: 8.4pt; }
  .milestone span { display: block; margin-top: 1.5mm; color: var(--ink); font-size: 7.2pt; line-height: 1.4; }
  .cover-page { padding-top: 15mm; background: linear-gradient(135deg, #ffffff 0%, #ffffff 68%, #edf6f5 100%); }
  .cover-kicker { display: inline-block; margin-bottom: 13mm; padding: 1.8mm 3mm; color: white; background: var(--teal); font-size: 8pt; font-weight: 800; letter-spacing: .08em; }
  .cover-hero { margin-top: 9mm; padding: 7mm; color: white; background: var(--navy); border-radius: 3.5mm; }
  .cover-hero h3 { margin: 0 0 3mm; color: white; font-size: 13pt; }
  .cover-scenarios { display: grid; grid-template-columns: 1fr 1fr; gap: 4mm; }
  .cover-scenario { padding: 4mm; border-radius: 2.5mm; background: rgba(255,255,255,.09); border: 1px solid rgba(255,255,255,.22); }
  .cover-scenario b { display: block; margin-bottom: 1.5mm; color: #8fe0d8; font-size: 11pt; }
  .cover-meta { display: grid; grid-template-columns: repeat(4, 1fr); gap: 2mm; margin-top: 6mm; }
  .cover-meta div { padding: 3mm; border: 1px solid var(--line); border-radius: 2mm; background: rgba(255,255,255,.88); }
  .cover-meta b { display: block; margin-bottom: 1mm; color: var(--teal); font-size: 7.2pt; }
  .cover-meta span { color: var(--navy); font-size: 8.5pt; font-weight: 800; line-height: 1.35; }
  .decision { margin-top: 6mm; padding: 4.5mm; background: var(--amber-soft); border: 1px solid #e6c986; border-radius: 2.5mm; }
  .decision b { color: #7a5110; font-size: 10.5pt; }
  .decision p { margin: 1.5mm 0 0; color: #684d1e; font-size: 9pt; }
  .signature { position: absolute; left: 16mm; bottom: 22mm; color: var(--muted); font-size: 8pt; }
  .status-p0 { color: white !important; background: var(--red) !important; font-weight: 800; }
  .status-p1 { color: white !important; background: var(--amber) !important; font-weight: 800; }
  .status-p2 { color: white !important; background: var(--teal) !important; font-weight: 800; }
  a { color: var(--teal); text-decoration: none; }
  @media print { body { background: white; } .page { margin: 0; } }
`;

const totalPages = 11;
const footer = (page, label = "화학물질 안전훈련 VR · 8/13 프로토타입 기획서") => `
  <div class="footer"><span>${label}</span><span>${page} / ${totalPages}</span></div>`;

const shell = (content, page, section, title) => `
  <section class="page">
    <div class="topline"><span><strong>PROTOTYPE PLAN</strong> · 2026.07.30</span><span>${section}</span></div>
    <div class="section-no">${section}</div>
    <h2>${title}</h2>
    ${content}
    ${footer(page)}
  </section>`;

const pages = [];

pages.push(`
  <section class="page cover-page">
    <div class="cover-kicker">VR SAFETY TRAINING · PROTOTYPE PLAN v1.5</div>
    <h1>화학물질 안전훈련 VR<br />프로토타입 기획서</h1>
    <p class="lead">두 상세 시나리오를 교육·훈련·테스트 3모드로 확장 설계하고, 8월 13일에는 상세 튜토리얼형 교육모드를 우선 구현한다.</p>
    <div class="rule"></div>
    <div class="cover-hero">
      <h3>이번 프로토타입에서 검증할 교육 경험</h3>
      <div class="cover-scenarios">
        <div class="cover-scenario"><b>01 · PPE 착용</b><span>작업정보 확인 → 보호구 점검·판정 → 좌·우 상태 착용 → 완료·퀴즈</span></div>
        <div class="cover-scenario"><b>02 · 밀폐공간 진입 전</b><span>문서 → 전원·LOTO → 가스측정 → 환기·재측정 → 감시인 승인</span></div>
      </div>
    </div>
    <div class="cover-meta">
      <div><b>완료 마감</b><span>2026.08.13</span></div>
      <div><b>개발 형태</b><span>1인 개발</span></div>
      <div><b>플랫폼</b><span>Meta Quest 2</span></div>
      <div><b>개발 환경</b><span>제작: Unity 6000.4.8f1<br />시연: Unity 6.5 (6000.5.5f1)<br />OpenXR · XRI</span></div>
    </div>
    <div class="decision">
      <b>구현 우선 원칙</b>
      <p>1차는 교육모드의 두 시나리오를 처음부터 결과까지 완성한다. 안정화 후 같은 절차를 재사용해 훈련모드, 테스트모드 순으로 지원 수준을 낮춰 확장한다.</p>
    </div>
    <div class="signature">개정 기준일 2026.07.30 · 교육모드 시연본 마감 2026.08.13</div>
    ${footer(1)}
  </section>`);

pages.push(shell(`
  <div class="grid-2">
    <div class="card teal">
      <h3>교육 문제</h3>
      <ul class="tight">
        <li>글·영상만으로는 절차의 순서와 실제 조작 판단이 분리되기 쉽다.</li>
        <li>실설비·보호구를 이용한 반복 교육은 준비 비용과 안전 부담이 크다.</li>
        <li>오조작 이유를 즉시 설명하고 같은 단계에서 다시 수행할 장치가 필요하다.</li>
      </ul>
    </div>
    <div class="card amber">
      <h3>이번 프로토타입의 답</h3>
      <ul class="tight">
        <li>Quest 2에서 PPE와 밀폐공간 두 핵심 교육을 독립 완주한다.</li>
        <li>교육모드의 보이스·자막·하이라이트·오류 교정을 실제 조작과 동기화한다.</li>
        <li>공통 ModeProfile을 먼저 두어 훈련·테스트모드 확장 기반을 검증한다.</li>
      </ul>
    </div>
  </div>
  <h3>교육 상황과 원래 기획의 관계</h3>
  <table class="compact">
    <tr><th style="width:19%">항목</th><th>프로토타입 기준</th></tr>
    <tr><td class="label">교육 대상</td><td>화학물질·밀폐공간 작업 전 안전교육이 필요한 신규 작업자와 현장 작업자</td></tr>
    <tr><td class="label">사용자 역할</td><td>혼합기 내부 청소 전 작업계획과 보호구를 확인하고 진입 전 안전절차를 수행하는 작업자</td></tr>
    <tr><td class="label">공통 상황</td><td>PPE룸에서 테이프를 포함한 PPE 7종을 점검·착용한 뒤 혼합기동에서 문서·LOTO·측정·환기·승인을 수행</td></tr>
    <tr><td class="label">원래 기획</td><td>PPE 교육·밀폐공간 교육·사고 체험 3개 시나리오를 교육·훈련·테스트 3모드로 운영</td></tr>
    <tr><td class="label">이번 범위</td><td>8/13까지 두 핵심 시나리오의 교육모드를 완주 가능하게 만들고 사고 체험과 후속 모드는 공통 코어 안정화 후 연결</td></tr>
  </table>
  <h3>검증 질문</h3>
  <div class="metric-strip">
    <div class="metric"><b>완주성</b><span>두 루트를 처음부터 결과까지 중단 없이 수행할 수 있는가</span></div>
    <div class="metric"><b>학습성</b><span>안내와 오류 교정이 다음 올바른 행동으로 연결되는가</span></div>
    <div class="metric"><b>일관성</b><span>문서·PPE·설비 상태와 HUD·VOICE가 같은 정보를 보이는가</span></div>
    <div class="metric"><b>안정성</b><span>Quest 2에서 재실행·연속 완주·목표 프레임을 만족하는가</span></div>
  </div>
  <div class="callout"><strong>판단 기준</strong> · 프로토타입은 최종 콘텐츠의 축소판이 아니라, 원래 기획의 공통 절차 코어와 교육모드 지원 정책이 실제 기기에서 성립하는지 검증하는 1차 실행본이다.</div>
`, 2, "01 · 배경과 검증", "왜 지금 이 프로토타입인가"));

pages.push(shell(`
  <p>최종 운영안은 각 상세 시나리오를 <span class="strong">교육·훈련·테스트 3모드</span>로 제공하는 것이다. 다만 1인 개발과 8월 13일 마감을 고려해, 이번 단계에서는 두 시나리오의 절차·상호작용·판정과 상세 안내를 포함한 <span class="strong">교육모드</span>를 먼저 완성한다.</p>
  <div class="metric-strip">
    <div class="metric"><b>2개</b><span>완주 가능한 교육 시나리오</span></div>
    <div class="metric"><b>3모드</b><span>교육 → 훈련 → 테스트 확장</span></div>
    <div class="metric"><b>1차</b><span>상세 튜토리얼형 교육모드</span></div>
    <div class="metric"><b>0건</b><span>시연 차단 P0 결함 목표</span></div>
  </div>
  <div class="grid-2">
    <div class="card teal">
      <h3>1차 구현 · 교육모드</h3>
      <ul class="tight">
        <li>PPE와 밀폐공간 시나리오의 처음-끝 완주</li>
        <li>단계별 목표·입력법·대상 위치 상세 안내</li>
        <li>VOICE·자막·하이라이트·위치 마커 동기화</li>
        <li>오조작 이유 설명, 올바른 행동 제시, 재시도</li>
        <li>퀴즈·해설·결과·초기화와 Quest 2 실행</li>
      </ul>
    </div>
    <div class="card amber">
      <h3>후속 구현 · 모드 확장</h3>
      <ul class="tight">
        <li>2차 훈련모드: 목표와 기본 지시만 제공</li>
        <li>상세 튜토리얼·정답 유도·대상 강조 제거</li>
        <li>3차 테스트모드: 수행 중 지원 UI·음성 없음</li>
        <li>도움 없이 수행한 순서·오류·시간·결과 기록</li>
        <li>공통 시나리오 상태와 완료 조건은 그대로 재사용</li>
      </ul>
    </div>
  </div>
  <h3>모드별 구현 순서와 완료 기준</h3>
  <table class="compact">
    <tr><th style="width:13%">단계</th><th style="width:22%">모드</th><th style="width:31%">지원 수준</th><th>다음 단계 진입 조건</th></tr>
    <tr><td class="center status-p0">1차</td><td><strong>교육모드</strong></td><td>상세 튜토리얼·오류 교정·퀴즈 해설</td><td>두 시나리오 P0 0건, Quest 2 2회 연속 완주</td></tr>
    <tr><td class="center status-p1">2차</td><td><strong>훈련모드</strong></td><td>현재 목표와 기본 지시만 제공</td><td>교육모드 회귀 이상 없이 기본 지시만으로 완주</td></tr>
    <tr><td class="center status-p2">3차</td><td><strong>테스트모드</strong></td><td>수행 중 도움 없음, 종료 후 결과만 제공</td><td>가이드 노출 0건, 수행 기록·채점 검증 완료</td></tr>
  </table>
  <div class="callout amber"><strong>범위 유지 원칙</strong> · 모드 확장은 안내 수준을 분리하는 작업이며 시나리오 절차 자체를 다시 만들지 않는다. 사고 체험·혼합기 내부 청소·서버 연동과 사실적 물리는 세 모드 공통으로 이번 프로토타입 범위에서 제외한다.</div>
`, 3, "02 · 목표와 범위", "프로토타입 목표·완료 정의"));

pages.push(shell(`
  <p>두 시나리오는 같은 혼합기 청소 작업 준비 상황을 공유하되 독립적으로 시작한다. 사용자는 <span class="strong">시나리오를 먼저 선택한 뒤 해당 시나리오의 플레이 모드</span>를 선택한다. 1차 빌드에서는 교육모드가 기본값이며, 훈련·테스트모드는 후속 빌드에서 순차 활성화한다.</p>
  <h3>공통 경험 흐름</h3>
  <div class="flow">
    <div class="node">앱 실행</div><div class="arrow">›</div>
    <div class="node">시나리오 선택</div><div class="arrow">›</div>
    <div class="node">모드 선택<br/>교육 기본</div><div class="arrow">›</div>
    <div class="node">모드별 안내</div><div class="arrow">›</div>
    <div class="node">절차 수행</div><div class="arrow">›</div>
    <div class="node">완료 판정</div><div class="arrow">›</div>
    <div class="node">퀴즈·결과</div>
  </div>
  <h3>모드별 지원 정책</h3>
  <table class="compact">
    <tr><th style="width:16%">모드</th><th style="width:27%">시작·단계 안내</th><th style="width:28%">수행 중 지원</th><th>오류·결과 처리</th></tr>
    <tr><td class="label">교육모드</td><td>목표·안전 의미·입력법을 단계별 VOICE와 자막으로 설명</td><td>대상 하이라이트, 위치 마커, 입력 힌트, 진행 잠금 제공</td><td>오류 이유와 올바른 행동을 알려 재시도. 퀴즈 즉시 해설</td></tr>
    <tr><td class="label">훈련모드</td><td>현재 작업 목표와 필요한 기본 행동만 짧게 표시</td><td>상세 순서·정답 유도·대상 강조 없음. 필수 시스템 상태만 표시</td><td>잘못된 수행은 짧게 재시도만 알림. 종료 후 점수 제공</td></tr>
    <tr><td class="label">테스트모드</td><td>시작 전 과제·종료 조건만 고지</td><td>수행 중 지시·힌트·하이라이트·정오답 피드백 없음</td><td>오류·순서·소요시간을 내부 기록하고 종료 후 결과만 제공</td></tr>
  </table>
  <h3>공통 시스템 완료 기준</h3>
  <div class="checklist">
    <div class="check">절차 상태·선행조건·완료 판정은 세 모드가 공유한다.</div>
    <div class="check">모드별 안내 수준은 데이터 설정으로 전환된다.</div>
    <div class="check">훈련·테스트에서 교육용 힌트가 노출되지 않는다.</div>
    <div class="check">시나리오 종료 후 모든 오브젝트·점수 상태가 초기화된다.</div>
    <div class="check">선택 모드와 시도·오류·소요시간이 결과에 전달된다.</div>
    <div class="check">시스템 오류·안전 고지는 모든 모드에서 예외적으로 표시한다.</div>
    <div class="check">Grip·Trigger·A·B 등 버튼 표기는 현재 입력안이며 개발·기기 테스트 과정에서 변경될 수 있다.</div>
  </div>
`, 4, "03 · 공통 경험", "공통 사용자 흐름·조작 설계"));

pages.push(shell(`
  <div class="asset-row">
    <div class="asset-card"><img src="${img("hazmat_suit.png")}" alt="방호복"></div>
    <div class="asset-card"><img src="${img("globes.png")}" alt="장갑"></div>
    <div class="asset-copy"><strong>상황 설정</strong><p>혼합기 A 내부 청소를 앞둔 작업자가 작업정보를 보이스·텍스트로 안내받고, 테이프를 포함한 교육용 PPE 7종을 점검·착용한다.</p><span class="tag dark">1차 교육모드</span><span class="tag">약 5분</span><span class="tag amber">PPE 7종</span></div>
  </div>
  <h3>교육모드 기준 상세 튜토리얼 절차</h3>
  <table class="steps compact">
    <tr><th>순서</th><th>교육 단계</th><th style="width:39%">사용자 수행</th><th>교육모드 안내·완료 조건</th></tr>
    <tr><td>1</td><td>작업정보 안내</td><td>보이스 또는 텍스트로 작업 내용과 필요한 PPE 안내를 확인한다.</td><td>안내가 1회 완료되면 PPE 진열장 위치 마커 활성화</td></tr>
    <tr><td>2</td><td>대상 이동</td><td>오른손 Ray로 PPE 진열장 앞 위치 마커를 선택한다.</td><td>텔레포트 페이드 후 현재 점검 대상 1개만 강조</td></tr>
    <tr><td>3</td><td>PPE 잡기·선택</td><td>Grip으로 보호구를 잡고 현재 선택한 장비를 확인한다.</td><td>선택한 PPE에만 사용·폐기 UI 활성화</td></tr>
    <tr><td>4</td><td>정상·불량 판정</td><td>정상품은 Trigger(사용), 불량품은 A(폐기)로 선택한다. 판정하지 않고 취소하려면 Grip을 다시 눌러 놓는다.</td><td>Trigger 사용 시 자동 착용 시작, A 폐기 시 정상품 재등장. 오판정은 이유 안내 후 재선택</td></tr>
    <tr><td>5</td><td>자동 착용·설명</td><td>PPE가 사용자 앞/위에서 몸의 해당 착용 위치로 이동하며 완전히 착용되는 연출을 본다.</td><td>입는 중 해당 PPE 착용 시 주의점을 보이스로 1회 설명</td></tr>
    <tr><td>6</td><td>착용 결과</td><td>몸을 내려다보거나 거울을 통해 지금까지 착용한 PPE와 적용한 테이프가 몸에 표시되는 것을 확인한다.</td><td>착용·적용된 PPE만 몸과 거울에 표시. PPE 7종 완료 시 자동으로 퀴즈 활성화</td></tr>
  </table>
  <h3>프로토타입에서 다루는 PPE</h3>
  <table class="compact">
    <tr><th style="width:22%">구분</th><th style="width:31%">구현 대상</th><th>프로토타입 표현</th></tr>
    <tr><td class="label">PPE 7종</td><td>방호복, 장갑, 장화, 호흡보호구, 안전모, 등지게, 테이프</td><td>6종의 장비 모델과 테이프 적용 상태를 구현하며, 테이프를 포함해 PPE 7종으로 계산한다.</td></tr>
    <tr><td class="label">상태 판정</td><td>장갑·장화 2개 그룹</td><td>정상/불량 프리팹, 찢김·오염 마커, 사용/폐기 후 정상품 재등장<br/><span class="micro">(구현중 불량의 구현 종류와 표현, 등장 종류 정책이 바뀔 여지가 있습니다.)</span></td></tr>
    <tr><td class="label">착용 상태</td><td>장갑·장화 좌우 + 나머지 4종 + 테이프</td><td>좌우 상태와 테이프 적용 상태는 별도 저장. 몸을 내려다보거나 거울을 확인하면 실제 착용·적용 완료된 PPE만 표시</td></tr>
  </table>
  <div class="callout amber"><strong>모드 확장 기준</strong> · 위 절차와 완료 조건은 세 모드가 공유한다. 훈련모드는 기본 목표만 남기고 단계별 설명·대상 강조를 제거하며, 테스트모드는 모든 수행 안내를 숨긴 채 선택·오류·소요시간만 기록한다. 실제 교육판의 PPE는 MSDS와 위험성평가에 따라 다시 확정한다.</div>
`, 5, "04 · 시나리오 1", "PPE 점검·판정·착용"));

pages.push(shell(`
  <h3>상호작용·상태 정의</h3>
  <table class="compact">
    <tr><th style="width:20%">대상</th><th style="width:27%">입력</th><th style="width:28%">저장 상태</th><th>오류 처리</th></tr>
    <tr><td class="label">작업정보 안내</td><td>VOICE·텍스트 자동 안내</td><td>workBriefingComplete</td><td>안내 완료 전 PPE 진열장 위치 마커 비활성</td></tr>
    <tr><td class="label">PPE 오브젝트</td><td>Grip(잡기)·재Grip(취소/놓기), Trigger(사용), A(폐기)</td><td>held, inspected, decision, accepted</td><td>오판정 사유를 보여주고 같은 장비에서 재선택</td></tr>
    <tr><td class="label">자동 착용</td><td>Trigger(사용) 선택</td><td>wearing → equipped, suit, respirator, helmet, backCarrier, gloveL/R, bootL/R</td><td>앞/위→착용 위치 이동, 착용 주의점 VOICE 종료 후 착용 모델만 활성화</td></tr>
    <tr><td class="label">착용 결과</td><td>몸쪽 시선 또는 거울 확인</td><td>scenarioComplete</td><td>미착용 장비는 숨기고 착용 완료된 PPE만 몸과 거울에 표시</td></tr>
  </table>
  <div class="grid-2">
    <div class="card teal">
      <h3>교육모드 · 상세 지원</h3>
      <ul class="tight">
        <li>작업정보와 필요한 PPE를 보이스·텍스트로 설명</li>
        <li>현재 점검 대상·결함 위치·입력 버튼을 강조</li>
        <li>오판정 시 “손상된 장갑은 폐기” 이유 안내</li>
        <li>올바른 정상품을 다시 제공하고 착용까지 유도</li>
      </ul>
    </div>
    <div class="card">
      <h3>훈련·테스트모드 · 후속</h3>
      <ul class="tight">
        <li><strong>훈련:</strong> “PPE 7종 점검·착용” 기본 목표만 표시</li>
        <li><strong>훈련:</strong> 결함 위치·판정 정답·다음 대상 강조 없음</li>
        <li><strong>테스트:</strong> 목표 외 수행 지시와 즉시 정오답 없음</li>
        <li><strong>테스트:</strong> 첫 선택·오판정·누락·완료시간 결과화</li>
      </ul>
    </div>
  </div>
  <h3>교육모드 1차 완료 판정(Definition of Done)</h3>
  <div class="checklist">
    <div class="check">작업정보 보이스·텍스트 안내 완료 전 PPE 단계로 건너뛸 수 없다.</div>
    <div class="check">장갑·장화 정상/불량 판정이 결과와 일치한다.</div>
    <div class="check">오판정 후 실패 종료 없이 같은 항목을 재시도한다.</div>
    <div class="check">gloveL/R, bootL/R가 각각 독립적으로 저장된다.</div>
    <div class="check">자동 착용 중 해당 PPE 착용 시 주의점 VOICE가 한 번 재생된다.</div>
    <div class="check">몸을 내려다보거나 거울을 확인할 때 착용 완료된 PPE만 보인다. PPE룸 조명과 전신 거울은 필수로 유지한다.</div>
    <div class="check">테이프를 포함한 PPE 7종 완료 후 5문항 퀴즈·해설이 열린다.</div>
    <div class="check">완료 후 허브 복귀 시 PPE와 점수가 초기화된다.</div>
  </div>
  <h3>구현 단순화</h3>
  <table class="compact">
    <tr><th style="width:24%">완성형 아이디어</th><th>8/13 프로토타입 방식</th></tr>
    <tr><td>천 물리·세밀한 착의</td><td>Trigger로 사용을 선택하면 PPE가 앞/위에서 몸의 착용 위치로 이동하고 착용 모델로 전환</td></tr>
    <tr><td>전신 거울(필수)</td><td>거울 오브젝트를 유지하고, 몸을 내려다보거나 거울을 확인할 때 착용된 PPE 모델만 보이도록 표시</td></tr>
    <tr><td>모든 PPE의 결함 분기</td><td>장갑·장화에서만 대표 결함을 검증하고 구조 재사용 가능하게 제작</td></tr>
    <tr><td>현장별 PPE 자동 추천</td><td>한 개 작업계획서와 고정된 교육용 샘플 목록 사용</td></tr>
  </table>
  <div class="callout"><strong>시연 핵심 장면</strong> · 교육모드에서 손상 장갑을 Trigger(사용)로 오판정 → 이유 안내 → A(폐기)·정상품 재등장 → Trigger(사용) → 자동 착용 → 착용 시 주의점 VOICE까지 연결한다. 판정 전 재Grip은 취소(놓기)로 처리한다. 훈련·테스트모드 추가 시 같은 상태 전이를 유지하고 안내만 단계적으로 제거한다.</div>
`, 6, "04 · 시나리오 1", "PPE 상세 동작·완료 기준"));

pages.push(shell(`
  <div class="asset-row">
    <div class="asset-card"><img src="${img("mixing_tank.png")}" alt="혼합기"></div>
    <div class="asset-card"><img src="${img("gas_detector.png")}" alt="가스측정기"></div>
    <div class="asset-copy"><strong>상황 설정</strong><p>오전 생산 후 혼합기 A 내부 청소를 준비한다. 사용자는 내부로 들어가지 않고, 작업허가부터 감시인 승인까지 진입 전 절차만 수행한다.</p><span class="tag dark">1차 교육모드</span><span class="tag">약 5분</span><span class="tag amber">측정값 TMP</span></div>
  </div>
  <h3>교육모드 기준 상세 튜토리얼 절차</h3>
  <table class="steps compact">
    <tr><th>순서</th><th>교육 단계</th><th style="width:39%">사용자 수행</th><th>교육모드 안내·완료 조건</th></tr>
    <tr><td>1</td><td>안전문서 확인</td><td>태블릿 한 대에서 작업허가서·작업 확인서·MSDS 요약 문서가 순서대로 바뀌어 표시된다. 표시된 문서를 확인하고 서명한다.</td><td>태블릿 문서 확인·서명 완료 전 전원·LOTO 단계로 진행하지 않음</td></tr>
    <tr><td>2</td><td>전원 차단·LOTO</td><td>VR 컨트롤러 입력으로 혼합기 제어반의 전원을 OFF하고 LOTO를 적용한다.</td><td>전원 OFF와 LOTO 적용 완료 전 1차 가스측정 단계로 진행하지 않음</td></tr>
    <tr><td>3</td><td>1차 가스측정</td><td>측정기를 지정 위치에서 한 번 측정해 상·중·하부 전체 측정을 완료한 것으로 처리한다.</td><td>O₂·LEL·H₂S·CO TMP 값 표시, 종합 FAIL 판정</td></tr>
    <tr><td>4</td><td>강제환기</td><td>팬 쪽과 혼합기 쪽 덕트를 연결하고 VR 컨트롤러 입력으로 팬을 작동한다.</td><td>양쪽 연결 후 환기 게이지 시작, 환기 완료 전 다음 단계로 진행하지 않음</td></tr>
    <tr><td>5</td><td>2차 재측정</td><td>환기 완료 뒤 지정 위치에서 한 번 재측정해 상·중·하부 전체 측정을 완료한 것으로 처리한다.</td><td>4채널 TMP 값과 종합 PASS 표시, 승인 단계 활성화</td></tr>
    <tr><td>6</td><td>무전 테스트</td><td>무전기를 잡고 송신 입력으로 진입 준비 완료를 전달한다.</td><td>송신음 → 감시인 응답 순차 재생, 중복 입력 차단</td></tr>
    <tr><td>7</td><td>감시인 승인</td><td>승인 메시지를 확인하고 완료 입력을 선택한다.</td><td>모든 선행조건 재검사 후 “진입 전 절차 완료”와 퀴즈 표시</td></tr>
  </table>
  <h3>프로토타입 가스 표시값</h3>
  <table class="compact">
    <tr><th>구분</th><th>O₂</th><th>LEL</th><th>H₂S</th><th>CO</th><th>종합</th><th style="width:29%">학습 메시지</th></tr>
    <tr><td class="label">1차</td><td class="center">17.6%</td><td class="center">03 %LEL</td><td class="center">002 ppm</td><td class="center">012 ppm</td><td class="center">FAIL</td><td>산소 부족. 진입 금지, 강제환기 실시</td></tr>
    <tr><td class="label">2차</td><td class="center">20.9%</td><td class="center">00 %LEL</td><td class="center">000 ppm</td><td class="center">003 ppm</td><td class="center">PASS</td><td>환기 유지, 감시인에게 승인 요청</td></tr>
  </table>
  <div class="callout red"><strong>안전·모드 고지</strong> · 표시값은 센서 연동값이 아닌 교육용 TMP다. 위 절차는 세 모드가 공유하되, 훈련모드는 기본 목표만 제공하고 테스트모드는 안내 없이 수행 기록만 남긴다. 실제 진입 판단에는 사용할 수 없으며 최종값은 안전보건 담당자 검수 후 확정한다.</div>
`, 7, "05 · 시나리오 2", "밀폐공간 진입 전 안전작업"));

pages.push(shell(`
  <h3>상호작용·선행조건 정의</h3>
  <table class="compact">
    <tr><th style="width:21%">기능</th><th style="width:26%">입력·처리</th><th style="width:28%">선행조건</th><th>완료 판정</th></tr>
    <tr><td class="label">문서</td><td>태블릿 1대에서 문서 3종 순차 전환, 확인·서명</td><td>시나리오 시작</td><td>태블릿 문서 확인·서명 완료</td></tr>
    <tr><td class="label">전원·LOTO</td><td>VR 컨트롤러 입력으로 제어반 전원 OFF, LOTO 적용</td><td>문서 확인·서명 완료</td><td>전원 차단·LOTO 적용 완료</td></tr>
    <tr><td class="label">1·2차 측정</td><td>측정기를 지정 위치에서 1회 측정</td><td>1차: LOTO / 2차: 환기 완료</td><td>상·중·하부 전체 측정 완료 처리 + 결과 확인</td></tr>
    <tr><td class="label">환기</td><td>고정형 덕트 양 끝 연결, 팬 작동</td><td>1차 FAIL 안내 확인</td><td>양쪽 연결 + 게이지 완료</td></tr>
    <tr><td class="label">무전·승인</td><td>무전기 송신</td><td>2차 PASS + 앞 단계 전체 완료</td><td>감시인 응답·승인 표시</td></tr>
  </table>
  <div class="grid-2">
    <div class="card teal">
      <h3>교육모드 · 상세 지원</h3>
      <ul class="tight">
        <li>태블릿 문서 → 전원 → LOTO 순서와 안전 의미 설명</li>
        <li>한 번의 측정으로 상·중·하부 전체 측정이 처리됨을 안내</li>
        <li>1차 FAIL 원인, 환기 필요성, 재측정을 안내</li>
        <li>오류 이유와 다음 올바른 행동을 즉시 제시</li>
      </ul>
    </div>
    <div class="card amber">
      <h3>훈련·테스트모드 · 후속</h3>
      <ul class="tight">
        <li><strong>훈련:</strong> “진입 전 절차 완료” 기본 목표만 표시</li>
        <li><strong>훈련:</strong> 상세 순서·PASS 조건 해설 없음</li>
        <li><strong>테스트:</strong> 수행 중 지시·강조·정오답 없음</li>
        <li><strong>테스트:</strong> 순서 오류·누락·재시도·시간 기록</li>
      </ul>
    </div>
  </div>
  <h3>교육모드 1차 완료 판정(Definition of Done)</h3>
  <div class="checklist">
    <div class="check">태블릿 문서 확인·서명 완료 전에는 전원·LOTO 단계로 진행하지 않는다.</div>
    <div class="check">전원 OFF와 LOTO 적용 완료 전에는 1차 가스측정 단계로 진행하지 않는다.</div>
    <div class="check">1차와 2차는 각각 한 번 측정으로 상·중·하부 전체 측정 완료 처리되며 결과는 분리 저장된다.</div>
    <div class="check">1차 FAIL 후 환기 없이 2차 측정으로 넘어갈 수 없다.</div>
    <div class="check">2차 PASS와 전체 선행조건 전에는 승인 음성이 나오지 않는다.</div>
    <div class="check">승인 후에도 혼합기 내부는 비활성 상태로 유지된다.</div>
  </div>
  <h3>모드별 주요 오류 처리</h3>
  <table class="compact">
    <tr><th style="width:25%">오류</th><th style="width:45%">교육모드</th><th>훈련 / 테스트모드</th></tr>
    <tr><td>전원·LOTO 미완료</td><td>현재 완료해야 할 행동을 안내하고 다음 단계 진행 차단</td><td>훈련: “순서 확인” / 테스트: 무안내·기록</td></tr>
    <tr><td>가스측정 미완료</td><td>한 번의 측정이 완료될 때까지 다음 단계 진행 차단</td><td>훈련: 기본 재시도 / 테스트: 무안내·기록</td></tr>
    <tr><td>덕트 한쪽 미연결</td><td>미설치 Snap 지점과 연결 방향 강조</td><td>훈련: 연결 미완료만 표시 / 테스트: 무안내·기록</td></tr>
    <tr><td>PASS 전 승인</td><td>환기 후 재측정을 지시하고 측정기 강조</td><td>훈련: 승인 불가만 표시 / 테스트: 무안내·기록</td></tr>
  </table>
`, 8, "05 · 시나리오 2", "밀폐공간 상세 동작·완료 기준"));

pages.push(shell(`
  <h3>8월 10일 프로토타입 시연 흐름</h3>
  <div class="flow" style="gap:1mm">
    <div class="node">콜드 실행</div><div class="arrow">›</div>
    <div class="node">PPE 시나리오<br>약 5분</div><div class="arrow">›</div>
    <div class="node">결과·초기화</div><div class="arrow">›</div>
    <div class="node">밀폐공간 시나리오<br>약 5분</div><div class="arrow">›</div>
    <div class="node">결과·초기화</div>
  </div>
  <table class="compact">
    <tr><th style="width:15%">시나리오</th><th style="width:15%">목표 시간</th><th style="width:42%">시연 행동</th><th>화면·검증 증빙</th></tr>
    <tr><td class="label">PPE 안전훈련</td><td class="center">약 5분</td><td>작업정보 안내, 정상/불량 판정, 폐기·재제공, 등지게·테이프를 포함한 PPE 7종 착용, 퀴즈·해설·결과·초기화</td><td>안내 확인, 오류 교정, 7종 완료, 결과 저장</td></tr>
    <tr><td class="label">밀폐공간 안전훈련</td><td class="center">약 5분</td><td>문서, 전원 OFF·LOTO, 1차 FAIL, 덕트·환기, 2차 PASS, 무전·승인, 퀴즈·해설·결과·초기화</td><td>선행조건, 수치, 환기 게이지, 승인·결과 저장</td></tr>
    <tr><td class="label">전체 시연</td><td class="center"><strong>약 10분</strong></td><td>두 시나리오를 각각 정상 수행 기준 약 5분으로 연속 시연</td><td>두 시나리오 각 1회 완주</td></tr>
  </table>
  <div class="grid-2">
    <div class="card teal">
      <h3>화면·영상 기록 목록</h3>
      <ul class="tight">
        <li>앱 실행·시나리오/모드 선택</li>
        <li>보이스·텍스트 작업정보 안내</li>
        <li>PPE 7종·불량 판정·등지게 착용·테이프 적용</li>
        <li>1차 FAIL·환기·2차 PASS</li>
        <li>퀴즈·결과·초기화</li>
      </ul>
    </div>
    <div class="card amber">
      <h3>시연 실패 대비</h3>
      <ul class="tight">
        <li>동일 APK 재설치본과 완충된 예비 컨트롤러 준비</li>
        <li>두 시나리오 각 5분, 총 10분 대체 영상과 주요 단계 PNG를 로컬 저장</li>
        <li>시연 전 콜드 실행·경계 재설정·오디오·화면 미러링 확인</li>
        <li>오류 발생 시 해당 기능의 QA 기록과 대체 화면으로 설명</li>
      </ul>
    </div>
  </div>
  <h3>구현 상태 기록 규칙</h3>
  <table class="compact">
    <tr><th>상태</th><th>판정 기준</th><th>발표자료 반영</th></tr>
    <tr><td class="label">검증 완료</td><td>Quest 2 실기기 2회 연속 완주·P0 0건</td><td>실제 캡처·측정 결과 사용</td></tr>
    <tr><td class="label">구현 중</td><td>Unity 동작은 있으나 실기기 완주 또는 회귀 미통과</td><td>현재 한계와 완료 예정일 병기</td></tr>
    <tr><td class="label">후속 범위</td><td>훈련·테스트모드, 사고 체험 등 8/13 이후 구현</td><td>완료 화면처럼 표시하지 않고 확장 계획으로 분리</td></tr>
  </table>
  <div class="callout"><strong>시연과 검수 분리</strong> · 시연 목표는 PPE와 밀폐공간을 각각 약 5분, 전체 약 10분으로 운영하는 것이다. 대기 게이지와 반복 설명만 단축하며, 절차 조건을 건너뛴 결과는 완주 증빙으로 사용하지 않는다.</div>
`, 9, "06 · 시연 계획", "시연 흐름·화면 기록 계획"));

pages.push(shell(`
  <div class="grid-2">
    <div>
      <h3>구현 구조</h3>
      <div class="flow" style="gap:1mm">
        <div class="node">Boot<br/>Hub</div><div class="arrow">›</div>
        <div class="node">Scenario<br/>Select</div><div class="arrow">›</div>
        <div class="node">Mode<br/>Profile</div><div class="arrow">›</div>
        <div class="node">Scenario<br/>Core</div><div class="arrow">›</div>
        <div class="node">Result</div>
      </div>
      <table class="compact">
        <tr><th style="width:32%">모듈</th><th>역할</th></tr>
        <tr><td class="label">ScenarioController</td><td>모드와 무관한 단계·선행조건·완료 상태 관리</td></tr>
        <tr><td class="label">ModeProfile</td><td>교육·훈련·테스트의 지시·힌트·오류 피드백 수준 정의</td></tr>
        <tr><td class="label">InteractionGate</td><td>핵심 상태 전이를 보호하고 모드별 재시도 정책 적용</td></tr>
        <tr><td class="label">GuidancePresenter</td><td>HUD·VOICE·하이라이트를 ModeProfile에 따라 표시·차단</td></tr>
        <tr><td class="label">ResultRecorder</td><td>선택 모드, 시도·오류·순서·시간·퀴즈 결과 기록</td></tr>
      </table>
    </div>
    <div>
      <h3>에셋 전략</h3>
      <table class="compact">
        <tr><th style="width:30%">분류</th><th>프로토타입 처리</th></tr>
        <tr><td class="label">기존 재사용</td><td>PPE 3D, PPE룸 조명·전신 거울, 혼합기, 제어반, 가스측정기, 팬·덕트, LOTO, 무전기, 안전문서 이미지</td></tr>
        <tr><td class="label">신규 최소 제작</td><td>모드 데이터 구조, 위치·아이템 마커, 자동 착용, 측정 처리, 단계 HUD, 결과 화면</td></tr>
        <tr><td class="label">1차 활성화</td><td>교육모드만 선택 가능. 훈련·테스트 리소스는 코어 안정화 후 연결</td></tr>
        <tr><td class="label">대체 가능</td><td>VOICE는 TTS, 감시인은 2D 패널, 고급 애니메이션은 페이드·Snap</td></tr>
        <tr><td class="label">성능 원칙</td><td>조명·전신 거울은 필수 유지. 정적 베이크 조명과 경량 거울 표현, 단순 셰이더로 Quest 2 안정성 우선</td></tr>
      </table>
    </div>
  </div>
  <h3>1차 교육모드 개발 일정 · 2026.07.29~08.13</h3>
  <table class="compact">
    <tr><th style="width:15%">기간</th><th style="width:23%">핵심 작업</th><th style="width:37%">구체 산출물</th><th>종료 조건</th></tr>
    <tr><td class="center">07.29~07.30</td><td>범위·모드 구조 동결</td><td>3모드 정책, 교육모드 P0/P1, ModeProfile, 에셋 점검</td><td>M0 범위 승인</td></tr>
    <tr><td class="center">07.30~07.31</td><td>공통 XR 기반</td><td>Quest 2 빌드, 허브, 교육모드 기본값, 이동·Ray·Grab, 초기화</td><td>M1 공통 루프</td></tr>
    <tr><td class="center">08.01~08.02</td><td>에셋 배치·상호작용 준비</td><td>PPE룸 조명·전신 거울과 혼합기동의 제어반·태블릿·측정기 등 주요 오브젝트 배치</td><td>두 시나리오의 필수 오브젝트 배치와 참조 점검 완료</td></tr>
    <tr><td class="center">08.03~08.05</td><td>PPE 교육모드</td><td>작업정보 보이스·텍스트 안내, 점검·판정, 좌우 착용, 상세 안내·오류 교정</td><td>M2 PPE 교육 완주</td></tr>
    <tr><td class="center">08.06</td><td>PPE 평가·기기 점검</td><td>5문항 퀴즈·해설, 결과·초기화, Quest 2 테스트</td><td>PPE P0 결함 제거</td></tr>
    <tr><td class="center">08.07~08.09</td><td>밀폐공간 교육모드</td><td>문서, OFF·LOTO, 측정·환기·승인, 상세 안내·교정</td><td>M3 밀폐 교육 완주</td></tr>
    <tr><td class="center"><strong>08.10</strong></td><td><strong>프로토타입 시연 발표</strong></td><td>Quest 2 실행 빌드, 시연 순서표, 피드백 수집</td><td>수정 우선순위 확정</td></tr>
    <tr><td class="center">08.11</td><td>교육모드 통합</td><td>두 루트 연결, HUD·VOICE·SFX, 퀴즈·해설·결과 통합</td><td>통합 빌드 RC0</td></tr>
    <tr><td class="center">08.12</td><td>QA·리허설</td><td>회귀 테스트, 성능·가독성 수정, APK·백업 영상</td><td>M4 RC1, P0 0건</td></tr>
    <tr><td class="center"><strong>08.13(목)</strong></td><td><strong>교육모드 패키징·시연</strong></td><td>교육모드 APK, 기획서, QA 체크, 두 시나리오 총 10분 대체 영상</td><td><strong>M5 1차 완료</strong></td></tr>
  </table>
  <div class="callout"><strong>후속 확장 게이트</strong> · 8/13 교육모드가 P0 0건으로 안정화된 뒤 2차 훈련모드, 3차 테스트모드를 순차 구현한다. 각 단계는 이전 모드 회귀 테스트를 통과해야 다음 단계로 넘어가며, 세 모드를 동시에 병행 개발하지 않는다.</div>
`, 10, "07 · 구현과 일정", "교육모드 우선 개발 구조·8/13 일정"));

pages.push(shell(`
  <h3>최종 QA 체크</h3>
  <table class="compact">
    <tr><th style="width:19%">영역</th><th style="width:41%">검증 항목</th><th>통과 기준</th></tr>
    <tr><td class="label">설치·모드</td><td>Quest 2 콜드 실행, 교육모드 기본값, 선택 모드 상태 전달</td><td>크래시 없이 교육모드 진입</td></tr>
    <tr><td class="label">PPE 교육모드</td><td>작업정보 VOICE·텍스트 안내 → PPE 7종 판정·착용·적용 → 퀴즈·해설 → 결과</td><td>7종 상태와 상세 안내·오류 교정 포함 2회 연속 완주</td></tr>
    <tr><td class="label">밀폐 교육모드</td><td>문서 → OFF·LOTO → 측정 → 환기 → 재측정 → 승인</td><td>상세 안내와 선행조건 포함 2회 연속 완주</td></tr>
    <tr><td class="label">안내 동기화</td><td>HUD·VOICE·자막·하이라이트의 목표·입력 일치 여부</td><td>누락·상충·중복 재생 0건</td></tr>
    <tr><td class="label">오류 복구</td><td>잘못된 버튼·대상·순서, 중복 입력, 오브젝트 놓침</td><td>이유·재시도 방법 안내 후 상태 정상 복구</td></tr>
    <tr><td class="label">초기화</td><td>허브 복귀 후 두 시나리오 재시작</td><td>이전 오브젝트·점수·단계 잔존 없음</td></tr>
    <tr><td class="label">가독성·성능</td><td>문서·HUD 글자, PPE룸 조명·전신 거울 표시, 주요 구간 프레임</td><td>조명·거울 누락 없음, 시연 중 읽기 가능, 목표 72fps</td></tr>
  </table>
  <p class="small"><strong>용어 설명</strong><br>- 콜드 : 앱을 완전히 종료한 상태에서 기존 실행 정보를 이어받지 않고 새로 실행하는 것.</p>
  <div class="grid-2">
    <div class="card red">
      <h3>상위 위험과 대응</h3>
      <ul class="tight">
        <li><strong>Quest 2 성능:</strong> 조명과 전신 거울은 제거하지 않는다. 동적 조명 수와 거울 갱신 비용을 제한하고 배경 LOD·정적 베이크를 우선한다.</li>
        <li><strong>모드 안내 누출:</strong> ModeProfile 단일 진입점과 자동화 체크로 훈련·테스트의 교육 힌트를 차단</li>
        <li><strong>오브젝트 분실:</strong> 범위 이탈 시 원위치 복귀, 핵심 물체는 설치 후 고정</li>
        <li><strong>콘텐츠 검수 지연:</strong> 모든 수치를 TMP로 표시하고 승인 전까지 외부 배포 금지</li>
      </ul>
    </div>
    <div class="card teal">
      <h3>후속 모드 인수 조건</h3>
      <ul class="tight">
        <li><strong>훈련:</strong> 기본 목표 외 상세 VOICE·하이라이트 0건</li>
        <li><strong>훈련:</strong> 두 시나리오 각 2회 완주·교육모드 회귀 통과</li>
        <li><strong>테스트:</strong> 수행 중 지시·정오답 피드백 0건</li>
        <li><strong>테스트:</strong> 오류·순서·소요시간·최종 결과 기록 일치</li>
      </ul>
    </div>
  </div>
  <h3>최종 산출물</h3>
  <div class="checklist">
    <div class="check">Unity 프로젝트 원본 및 버전 정보</div>
    <div class="check">Meta Quest 2 설치용 APK</div>
    <div class="check">본 프로토타입 기획서</div>
    <div class="check">3모드 ModeProfile·지원 정책 정의</div>
    <div class="check">QA 체크리스트·알려진 이슈</div>
    <div class="check">두 시나리오 각 5분·총 10분 대체 시연 영상</div>
  </div>
  <h3>안전 검수 참고</h3>
  <p class="small">밀폐공간의 적정공기 정의와 작업 전 측정·평가 원칙은 국가법령정보센터의 「산업안전보건기준에 관한 규칙」을 참고했다. 안전보건공단 자료는 작업 전 가스농도 측정·환기·보호구·교육의 중요성을 보조 근거로 사용했다. 최종 교육판은 현행 법령, 대상 물질 MSDS, 사업장 작업허가 절차를 다시 대조해야 한다.</p>
  <p class="micro">출처 1 · 국가법령정보센터, 산업안전보건기준에 관한 규칙 (시행 2026.03.02): <a href="https://www.law.go.kr/LSW/lsInfoP.do?lsiSeq=273603">law.go.kr/LSW/lsInfoP.do?lsiSeq=273603</a><br/>출처 2 · 한국산업안전보건공단, 질식재해예방 One-Call 안내: <a href="https://oshri.kosha.or.kr/kosha/report/notice.do?articleNo=421278&amp;attachNo=238174&amp;mode=download">kosha.or.kr 밀폐공간 예방 안내</a></p>
  <div class="callout amber"><strong>단계별 완료 선언</strong> · 2026년 8월 13일에는 Quest 2에서 두 시나리오의 교육모드가 상세 튜토리얼과 함께 완주되고 P0 결함이 0건이면 1차 완료로 판정한다. 이후 훈련모드와 테스트모드는 위 인수 조건을 각각 충족할 때 별도 완료 처리한다.</div>
`, 11, "08 · QA와 인수", "검수 기준·시연·위험 관리"));

const fullHtml = `<!doctype html><html lang="ko"><head><meta charset="utf-8"><title>화학물질 안전훈련 VR 프로토타입 기획서</title><style>${css}</style></head><body>${pages.join("\n")}</body></html>`;

await mkdir(workDir, { recursive: true });
await mkdir(qaDir, { recursive: true });
await mkdir(outDir, { recursive: true });
await writeFile(htmlPath, fullHtml, "utf8");

for (let i = 0; i < pages.length; i += 1) {
  const pageHtml = `<!doctype html><html lang="ko"><head><meta charset="utf-8"><style>${css} body{background:white}.page{margin:0}</style></head><body>${pages[i]}</body></html>`;
  const pagePath = join(workDir, `page-${String(i + 1).padStart(2, "0")}.html`);
  const pngPath = join(qaDir, `page-${String(i + 1).padStart(2, "0")}.png`);
  await writeFile(pagePath, pageHtml, "utf8");
  const shot = spawnSync(chrome, [
    "--headless=new", "--disable-gpu", "--no-sandbox", "--hide-scrollbars",
    "--allow-file-access-from-files", "--force-device-scale-factor=1",
    "--window-size=794,1123", `--screenshot=${pngPath}`,
    pathToFileURL(pagePath).href,
  ], { encoding: "utf8" });
  if (shot.status !== 0) throw new Error(`Screenshot ${i + 1} failed: ${shot.stderr || shot.stdout}`);
}

await rm(pdfPath, { force: true });
const pdf = spawnSync(chrome, [
  "--headless=new", "--disable-gpu", "--no-sandbox", "--allow-file-access-from-files",
  "--print-to-pdf-no-header", `--print-to-pdf=${pdfPath}`,
  pathToFileURL(htmlPath).href,
], { encoding: "utf8" });

if (pdf.status !== 0) throw new Error(`PDF export failed: ${pdf.stderr || pdf.stdout}`);

console.log(JSON.stringify({ htmlPath, pdfPath, qaDir, pages: pages.length }, null, 2));
