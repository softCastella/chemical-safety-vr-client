const fs = require("fs");
const path = require("path");
const vm = require("vm");

const projectRoot = path.resolve(__dirname, "..");
const outputDir = path.join(projectRoot, "output", "html");
const targetPath = path.join(outputDir, "3D_모델링_목록.html");
const legacyPath = path.join(outputDir, "3D_모델링_목록_이전구조_백업.html");

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function extractSections(html) {
  const scriptMatch = html.match(/<script>([\s\S]*)<\/script>/i);
  if (!scriptMatch) throw new Error("기존 문서에서 데이터 스크립트를 찾지 못했습니다.");

  const script = scriptMatch[1];
  const start = script.indexOf("var sourceSections =");
  const end = script.indexOf("function expandSourceSections");
  if (start < 0 || end < 0 || end <= start) {
    throw new Error("기존 문서에서 sourceSections 데이터를 찾지 못했습니다.");
  }

  const context = {};
  vm.runInNewContext(
    script.slice(start, end) + "; this.sourceSections = sourceSections;",
    context,
  );
  return context.sourceSections;
}

function sceneClass(scene) {
  if (scene === "PPE룸") return "scene-ppe";
  if (scene === "혼합기동") return "scene-mixer";
  if (scene === "혼합기 내부") return "scene-inside";
  return "scene-other";
}

function buildRows(sections) {
  let number = 0;
  const rows = [];

  for (const section of sections) {
    rows.push(
      `<tr class="section-row" data-section-name="${escapeHtml(section.name)}">` +
        `<td colspan="9">${escapeHtml(section.name)}</td></tr>`,
    );

    for (const group of section.groups) {
      for (const resource of group.resources) {
        number += 1;
        const isSuppliedAirHarness = resource === "tactical_harness_3d_model";
        const usage = isSuppliedAirHarness
          ? "등지게형 송기마스크 하네스"
          : group.usage;
        const purpose = isSuppliedAirHarness
          ? "송기마스크와 전동 송풍기를 공기 호스로 연결하고 송풍기와 호스를 몸에 지지·고정"
          : group.purpose;
        const interaction = isSuppliedAirHarness
          ? "Grab/호스 연결부 점검/등지게 착용 확인"
          : group.interaction;
        rows.push(`
          <tr class="data-row" draggable="true">
            <td class="number-cell" aria-label="${number}번 행">
              <button class="insert-before" type="button" title="이 위에 행 추가" aria-label="이 위에 행 추가">+</button>
              <span class="row-number">${number}</span>
              <button class="delete-row" type="button" title="행 삭제" aria-label="${number}번 행 삭제">−</button>
            </td>
            <td class="resource-cell editable" contenteditable="true" data-field="resource">${escapeHtml(resource)}</td>
            <td class="scene-cell editable ${sceneClass(group.scene)}" contenteditable="true" data-field="scene">${escapeHtml(group.scene)}</td>
            <td class="editable center-cell" contenteditable="true" data-field="usage">${escapeHtml(usage)}</td>
            <td class="editable center-cell production-cell" contenteditable="true" data-field="production">${escapeHtml(group.production)}</td>
            <td class="editable center-cell" contenteditable="true" data-field="poly">미정</td>
            <td class="editable" contenteditable="true" data-field="purpose">${escapeHtml(purpose)}</td>
            <td class="editable" contenteditable="true" data-field="interaction">${escapeHtml(interaction)}</td>
            <td class="priority-cell">
              <select class="priority-select priority-required" aria-label="우선순위">
                <option value="필수" selected>필수</option>
                <option value="권장">권장</option>
                <option value="옵션">옵션</option>
              </select>
            </td>
          </tr>`,
        );
      }
    }
  }

  return { html: rows.join("\n"), count: number };
}

const sourcePath = fs.existsSync(legacyPath) ? legacyPath : targetPath;
const existingHtml = fs.readFileSync(sourcePath, "utf8");
const sections = extractSections(existingHtml);
const built = buildRows(sections);

const documentHtml = `<!DOCTYPE html>
<html lang="ko">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>화학물질 안전교육 VR - 3D 모델링 목록</title>
  <style>
    :root {
      --bg: #f7f7f4;
      --paper: #ffffff;
      --text: #292824;
      --muted: #77746d;
      --line: #e5e2da;
      --line-strong: #d6d1c6;
      --header: #f5f3ec;
      --section: #fff3bf;
      --accent: #6c5ce7;
      --accent-soft: #efedff;
      --danger: #df535a;
      --green: #4f9f68;
      --yellow: #e9b949;
      --shadow: 0 18px 48px rgba(42, 40, 34, 0.08);
    }

    * { box-sizing: border-box; }
    html { background: var(--bg); }
    body {
      margin: 0;
      min-width: 320px;
      color: var(--text);
      background: var(--bg);
      font-family: Pretendard, "Noto Sans KR", "Malgun Gothic", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      -webkit-font-smoothing: antialiased;
    }

    button, select { font: inherit; }
    button { color: inherit; }

    .app {
      width: min(1180px, calc(100% - 40px));
      margin: 44px auto 72px;
    }

    .document-head {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: 24px;
      align-items: end;
      margin-bottom: 22px;
    }

    .eyebrow {
      margin-bottom: 10px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 800;
      letter-spacing: 0.12em;
    }

    h1 {
      margin: 0;
      font-size: clamp(29px, 4vw, 44px);
      line-height: 1.16;
      letter-spacing: -0.045em;
    }

    .subtitle {
      margin: 12px 0 0;
      color: var(--muted);
      font-size: 14px;
      line-height: 1.7;
    }

    .status {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: 8px;
      padding-bottom: 2px;
    }

    .chip {
      padding: 8px 12px;
      border: 1px solid var(--line);
      border-radius: 999px;
      background: rgba(255,255,255,.84);
      color: var(--muted);
      font-size: 12px;
      white-space: nowrap;
    }

    .toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 14px;
      margin-bottom: 14px;
      padding: 9px 10px;
      border: 1px solid var(--line);
      border-radius: 15px;
      background: var(--paper);
      box-shadow: var(--shadow);
    }

    .toolbar-group { display: flex; align-items: center; gap: 4px; }
    .toolbar button {
      min-height: 38px;
      padding: 0 13px;
      border: 0;
      border-radius: 9px;
      background: transparent;
      font-size: 13px;
      font-weight: 760;
      cursor: pointer;
    }
    .toolbar button:hover { background: #f3f2ee; }
    .toolbar .primary {
      color: #fff;
      background: var(--accent);
    }
    .toolbar .primary:hover { background: #5f50d8; }

    .hint {
      margin: 0 0 13px;
      padding: 11px 14px;
      border: 1px solid var(--line);
      border-radius: 13px;
      background: rgba(255,255,255,.72);
      color: var(--muted);
      font-size: 12px;
      line-height: 1.55;
    }

    .table-shell {
      position: relative;
      overflow: visible;
      border: 1px solid var(--line);
      border-radius: 15px;
      background: var(--paper);
      box-shadow: var(--shadow);
    }

    table {
      width: 100%;
      border-collapse: separate;
      border-spacing: 0;
      table-layout: fixed;
      font-size: 12px;
    }

    col.number { width: 58px; }
    col.resource { width: 148px; }
    col.scene { width: 92px; }
    col.usage { width: 130px; }
    col.production { width: 112px; }
    col.poly { width: 88px; }
    col.purpose { width: 170px; }
    col.interaction { width: 190px; }
    col.priority { width: 90px; }

    thead th {
      position: sticky;
      top: 0;
      z-index: 20;
      height: 49px;
      padding: 10px 8px;
      border-bottom: 1px solid var(--line-strong);
      background: var(--header);
      color: #5c5951;
      text-align: center;
      font-weight: 800;
      box-shadow: 0 2px 0 rgba(255,255,255,.86);
    }
    thead th:first-child { border-radius: 14px 0 0 0; }
    thead th:last-child { border-radius: 0 14px 0 0; }

    tbody td {
      min-height: 58px;
      padding: 16px 10px;
      border-right: 1px solid #eeece6;
      border-bottom: 1px solid var(--line);
      background: #fff;
      vertical-align: middle;
      line-height: 1.55;
      overflow-wrap: anywhere;
    }
    tbody td:last-child { border-right: 0; }
    tbody tr:last-child td { border-bottom: 0; }

    .section-row td {
      height: 48px;
      padding: 13px 12px;
      background: var(--section);
      color: #3d3a32;
      font-size: 14px;
      font-weight: 850;
      letter-spacing: -0.015em;
    }

    .data-row { position: relative; }
    .data-row:hover td { background: #fffefa; }
    .data-row.selected td { background: var(--accent-soft); }
    .data-row.dragging { opacity: .48; }
    .data-row.drop-before td { box-shadow: inset 0 3px 0 var(--accent); }

    .editable { outline: none; transition: background .14s, box-shadow .14s; }
    .editable:focus {
      position: relative;
      z-index: 2;
      background: #fffdf4 !important;
      box-shadow: inset 0 0 0 2px rgba(108,92,231,.32);
    }

    .number-cell {
      position: relative;
      padding: 0 6px;
      color: #67645d;
      text-align: center;
      font-weight: 800;
      cursor: grab;
      user-select: none;
    }
    .number-cell:active { cursor: grabbing; }
    .delete-row, .insert-before {
      position: absolute;
      left: 50%;
      border: 0;
      border-radius: 999px;
      opacity: 0;
      cursor: pointer;
      transform: translateX(-50%);
      transition: opacity .14s, transform .14s;
    }
    .delete-row {
      top: 50%;
      width: 27px;
      height: 27px;
      color: #fff;
      background: var(--danger);
      font-size: 18px;
      line-height: 26px;
      transform: translate(-50%, -50%);
    }
    .insert-before {
      top: -10px;
      z-index: 5;
      width: 22px;
      height: 22px;
      color: #fff;
      background: var(--accent);
      font-size: 15px;
      font-weight: 900;
      line-height: 20px;
    }
    .data-row:hover .delete-row,
    .data-row:hover .insert-before { opacity: 1; }
    .data-row:hover .row-number { opacity: 0; }
    .data-row:hover td { border-top-color: var(--accent); }

    .resource-cell { text-align: left; font-weight: 750; }
    .center-cell, .scene-cell { text-align: center; }
    .production-cell { color: #4f4b44; font-weight: 700; }
    .scene-cell { font-weight: 850; }
    .scene-ppe { color: #9a7618; }
    .scene-mixer { color: #d96f24; }
    .scene-inside { color: #347ab8; }
    .scene-other { color: #88847c; }

    .priority-cell { padding: 11px 8px; text-align: center; }
    .priority-select {
      width: 76px;
      min-height: 32px;
      padding: 0 24px 0 12px;
      border: 0;
      border-radius: 999px;
      color: #fff;
      text-align: center;
      font-size: 12px;
      font-weight: 850;
      cursor: pointer;
      appearance: auto;
    }
    .priority-select option { color: #292824; background: #fff; }
    .priority-required { background: var(--danger); }
    .priority-recommended { background: var(--green); }
    .priority-optional { color: #56420a; background: var(--yellow); }

    .empty-note {
      display: none;
      padding: 34px;
      color: var(--muted);
      text-align: center;
    }

    @media (max-width: 900px) {
      .app { width: min(1180px, calc(100% - 20px)); margin-top: 22px; }
      .document-head { grid-template-columns: 1fr; }
      .status { justify-content: flex-start; }
      .toolbar { align-items: stretch; flex-direction: column; }
      .toolbar-group { flex-wrap: wrap; }
      .table-shell { overflow-x: auto; }
      table { min-width: 1080px; }
      thead th { position: sticky; }
    }

    @media print {
      body { background: #fff; }
      .app { width: 100%; margin: 0; }
      .toolbar, .hint, .status { display: none !important; }
      .document-head { margin-bottom: 14px; }
      h1 { font-size: 24px; }
      .table-shell { border-radius: 0; box-shadow: none; }
      thead th { position: static; }
      .delete-row, .insert-before { display: none !important; }
      tbody td { padding: 8px 6px; }
    }
  </style>
</head>
<body>
  <main class="app">
    <header class="document-head">
      <div>
        <div class="eyebrow">RESOURCE PLAN · EDITABLE DOCUMENT</div>
        <h1>화학물질 안전교육 VR — 3D 모델링 목록</h1>
        <p class="subtitle">PPE룸 · 혼합기동 · 혼합기 내부의 실제 3D 제작 요소와 구성 방식을 정리한 편집형 문서</p>
      </div>
      <div class="status" aria-label="문서 상태">
        <span class="chip" id="resourceCount">${built.count}개 리소스</span>
        <span class="chip">셀 직접 편집</span>
      </div>
    </header>

    <nav class="toolbar" aria-label="목록 도구">
      <div class="toolbar-group">
        <button type="button" class="primary" id="addBottom">+ 맨 아래 행 추가</button>
        <button type="button" id="duplicateRow">복제</button>
        <button type="button" id="moveUp">위로</button>
        <button type="button" id="moveDown">아래로</button>
        <button type="button" id="resetList">초기 목록</button>
      </div>
      <div class="toolbar-group">
        <button type="button" id="downloadCsv">CSV</button>
        <button type="button" id="printDocument">인쇄 / PDF</button>
      </div>
    </nav>

    <p class="hint">셀을 클릭하면 바로 수정할 수 있습니다. 번호 칸을 잡아 행을 이동하고, 행 왼쪽의 +와 −로 추가하거나 삭제할 수 있습니다. 우선순위는 뱃지를 눌러 선택합니다.</p>

    <section class="table-shell" aria-label="3D 모델링 목록 표">
      <table>
        <colgroup>
          <col class="number" /><col class="resource" /><col class="scene" />
          <col class="usage" /><col class="production" /><col class="poly" />
          <col class="purpose" /><col class="interaction" /><col class="priority" />
        </colgroup>
        <thead>
          <tr>
            <th scope="col">번호</th>
            <th scope="col">리소스명</th>
            <th scope="col">씬</th>
            <th scope="col">쓰이는 장면</th>
            <th scope="col">제작</th>
            <th scope="col">폴리 예산</th>
            <th scope="col">용도</th>
            <th scope="col">인터랙션 / 연출</th>
            <th scope="col">우선순위</th>
          </tr>
        </thead>
        <tbody id="resourceBody">
${built.html}
        </tbody>
      </table>
      <div class="empty-note" id="emptyNote">목록이 비어 있습니다. 위의 행 추가 버튼을 눌러 시작하세요.</div>
    </section>
  </main>

  <script>
    (() => {
      "use strict";

      const body = document.getElementById("resourceBody");
      const initialMarkup = body.innerHTML;
      const countLabel = document.getElementById("resourceCount");
      const emptyNote = document.getElementById("emptyNote");
      let selectedRow = null;
      let draggedRow = null;
      let dragAllowed = false;

      function dataRows() {
        return Array.from(body.querySelectorAll("tr.data-row"));
      }

      function updateNumbers() {
        const rows = dataRows();
        rows.forEach((row, index) => {
          const number = index + 1;
          row.querySelector(".row-number").textContent = number;
          row.querySelector(".number-cell").setAttribute("aria-label", number + "번 행");
          row.querySelector(".delete-row").setAttribute("aria-label", number + "번 행 삭제");
        });
        countLabel.textContent = rows.length + "개 리소스";
        emptyNote.style.display = rows.length ? "none" : "block";
      }

      function applySceneColor(cell) {
        if (!cell) return;
        cell.classList.remove("scene-ppe", "scene-mixer", "scene-inside", "scene-other");
        const value = cell.textContent.trim();
        if (value === "PPE룸") cell.classList.add("scene-ppe");
        else if (value === "혼합기동") cell.classList.add("scene-mixer");
        else if (value === "혼합기 내부") cell.classList.add("scene-inside");
        else cell.classList.add("scene-other");
      }

      function applyPriority(select) {
        select.classList.remove("priority-required", "priority-recommended", "priority-optional");
        if (select.value === "권장") select.classList.add("priority-recommended");
        else if (select.value === "옵션") select.classList.add("priority-optional");
        else select.classList.add("priority-required");
      }

      function selectRow(row) {
        if (selectedRow) selectedRow.classList.remove("selected");
        selectedRow = row || null;
        if (selectedRow) selectedRow.classList.add("selected");
      }

      function makeRow(sourceRow) {
        const row = sourceRow ? sourceRow.cloneNode(true) : document.querySelector("tr.data-row").cloneNode(true);
        row.classList.remove("selected", "dragging", "drop-before");
        if (!sourceRow) {
          row.querySelector('[data-field="resource"]').textContent = "새 리소스";
          row.querySelector('[data-field="scene"]').textContent = "기타";
          row.querySelector('[data-field="usage"]').textContent = "";
          row.querySelector('[data-field="production"]').textContent = "Tripo";
          row.querySelector('[data-field="poly"]').textContent = "미정";
          row.querySelector('[data-field="purpose"]').textContent = "";
          row.querySelector('[data-field="interaction"]').textContent = "";
          row.querySelector(".priority-select").value = "옵션";
        }
        applySceneColor(row.querySelector(".scene-cell"));
        applyPriority(row.querySelector(".priority-select"));
        return row;
      }

      function nearestSectionEnd() {
        const sections = Array.from(body.querySelectorAll(".section-row"));
        return sections.length ? sections[sections.length - 1] : null;
      }

      body.addEventListener("click", (event) => {
        const row = event.target.closest("tr.data-row");
        if (!row) return;

        if (event.target.closest(".delete-row")) {
          row.remove();
          if (selectedRow === row) selectedRow = null;
          updateNumbers();
          return;
        }

        if (event.target.closest(".insert-before")) {
          const newRow = makeRow();
          row.before(newRow);
          selectRow(newRow);
          updateNumbers();
          newRow.querySelector('[data-field="resource"]').focus();
          return;
        }

        selectRow(row);
      });

      body.addEventListener("input", (event) => {
        if (event.target.matches(".scene-cell")) applySceneColor(event.target);
      });

      body.addEventListener("change", (event) => {
        if (event.target.matches(".priority-select")) applyPriority(event.target);
      });

      body.addEventListener("pointerdown", (event) => {
        dragAllowed = Boolean(event.target.closest(".number-cell")) && !event.target.closest("button");
      });

      body.addEventListener("dragstart", (event) => {
        const row = event.target.closest("tr.data-row");
        if (!row || !dragAllowed) {
          event.preventDefault();
          return;
        }
        draggedRow = row;
        row.classList.add("dragging");
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", "row");
      });

      body.addEventListener("dragover", (event) => {
        const row = event.target.closest("tr.data-row");
        if (!draggedRow || !row || row === draggedRow) return;
        event.preventDefault();
        dataRows().forEach((item) => item.classList.remove("drop-before"));
        row.classList.add("drop-before");
      });

      body.addEventListener("drop", (event) => {
        const row = event.target.closest("tr.data-row");
        if (!draggedRow || !row || row === draggedRow) return;
        event.preventDefault();
        const rect = row.getBoundingClientRect();
        if (event.clientY > rect.top + rect.height / 2) row.after(draggedRow);
        else row.before(draggedRow);
        updateNumbers();
      });

      body.addEventListener("dragend", () => {
        dataRows().forEach((row) => row.classList.remove("dragging", "drop-before"));
        draggedRow = null;
        dragAllowed = false;
      });

      document.getElementById("addBottom").addEventListener("click", () => {
        const row = makeRow();
        body.append(row);
        selectRow(row);
        updateNumbers();
        row.querySelector('[data-field="resource"]').focus();
        row.scrollIntoView({ behavior: "smooth", block: "center" });
      });

      document.getElementById("duplicateRow").addEventListener("click", () => {
        if (!selectedRow) return;
        const copy = makeRow(selectedRow);
        selectedRow.after(copy);
        selectRow(copy);
        updateNumbers();
      });

      document.getElementById("moveUp").addEventListener("click", () => {
        if (!selectedRow) return;
        let previous = selectedRow.previousElementSibling;
        while (previous && !previous.classList.contains("data-row")) previous = previous.previousElementSibling;
        if (previous) previous.before(selectedRow);
        updateNumbers();
      });

      document.getElementById("moveDown").addEventListener("click", () => {
        if (!selectedRow) return;
        let next = selectedRow.nextElementSibling;
        while (next && !next.classList.contains("data-row")) next = next.nextElementSibling;
        if (next) next.after(selectedRow);
        updateNumbers();
      });

      document.getElementById("resetList").addEventListener("click", () => {
        if (!window.confirm("편집한 내용을 지우고 처음 목록으로 되돌릴까요?")) return;
        body.innerHTML = initialMarkup;
        selectedRow = null;
        updateNumbers();
      });

      document.getElementById("downloadCsv").addEventListener("click", () => {
        const headers = ["번호", "리소스명", "씬", "쓰이는 장면", "제작", "폴리 예산", "용도", "인터랙션 / 연출", "우선순위"];
        const lines = [headers];
        dataRows().forEach((row, index) => {
          lines.push([
            index + 1,
            row.querySelector('[data-field="resource"]').textContent.trim(),
            row.querySelector('[data-field="scene"]').textContent.trim(),
            row.querySelector('[data-field="usage"]').textContent.trim(),
            row.querySelector('[data-field="production"]').textContent.trim(),
            row.querySelector('[data-field="poly"]').textContent.trim(),
            row.querySelector('[data-field="purpose"]').textContent.trim(),
            row.querySelector('[data-field="interaction"]').textContent.trim(),
            row.querySelector(".priority-select").value,
          ]);
        });
        const csv = lines.map((line) => line.map((value) => '"' + String(value).replaceAll('"', '""') + '"').join(",")).join("\\r\\n");
        const blob = new Blob(["\ufeff" + csv], { type: "text/csv;charset=utf-8" });
        const link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = "3D_모델링_목록.csv";
        link.click();
        URL.revokeObjectURL(link.href);
      });

      document.getElementById("printDocument").addEventListener("click", () => window.print());
      updateNumbers();
    })();
  </script>
</body>
</html>
`;

fs.writeFileSync(targetPath, documentHtml, "utf8");
console.log(`새 HTML 생성 완료: ${targetPath}`);
console.log(`정적 데이터 행: ${built.count}개`);
