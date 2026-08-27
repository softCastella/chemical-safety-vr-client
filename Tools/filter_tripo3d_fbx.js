const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "..");
const targetPath = path.join(projectRoot, "output", "html", "3D_모델링_목록.html");
const html = fs.readFileSync(targetPath, "utf8");

const tbodyMatch = html.match(/(<tbody id="resourceBody">)([\s\S]*?)(<\/tbody>)/i);
if (!tbodyMatch) throw new Error("resourceBody 표 본문을 찾지 못했습니다.");

const tableRows = tbodyMatch[2].match(/<tr\b[\s\S]*?<\/tr>/gi) || [];
const outputRows = [];
let currentSection = null;
let currentDataRows = [];
let number = 0;

function flushSection() {
  if (!currentSection || currentDataRows.length === 0) return;
  outputRows.push(currentSection, ...currentDataRows);
}

function normalizeRow(row) {
  number += 1;
  return row
    .replace(/aria-label="\d+번 행"/, `aria-label="${number}번 행"`)
    .replace(/<span class="row-number">\d+<\/span>/, `<span class="row-number">${number}</span>`)
    .replace(/aria-label="\d+번 행 삭제"/, `aria-label="${number}번 행 삭제"`)
    .replace(
      /(<td class="editable center-cell production-cell" contenteditable="true" data-field="production">)[\s\S]*?(<\/td>)/,
      "$1Tripo3D FBX$2",
    );
}

for (const row of tableRows) {
  if (row.includes('class="section-row"')) {
    flushSection();
    currentSection = row;
    currentDataRows = [];
    continue;
  }

  if (!row.includes('class="data-row"')) continue;
  const production = row.match(/data-field="production">([\s\S]*?)<\/td>/i)?.[1] || "";
  if (/Tripo/i.test(production)) currentDataRows.push(normalizeRow(row));
}
flushSection();

let result = html.replace(
  tbodyMatch[0],
  `${tbodyMatch[1]}\n${outputRows.join("\n\n")}\n        ${tbodyMatch[3]}`,
);

result = result
  .replace(/<span class="chip" id="resourceCount">\d+개 리소스<\/span>/, `<span class="chip" id="resourceCount">${number}개 리소스</span>`)
  .replace(
    /<p class="subtitle">[\s\S]*?<\/p>/,
    `<p class="subtitle">PPE룸 · 혼합기동 · 혼합기 내부에서 사용하는 Tripo3D 제작 FBX 모델 목록</p>`,
  )
  .replace(
    `row.querySelector('[data-field="production"]').textContent = "Tripo";`,
    `row.querySelector('[data-field="production"]').textContent = "Tripo3D FBX";`,
  );

fs.writeFileSync(targetPath, result, "utf8");
console.log(`Tripo3D FBX 행 ${number}개로 필터링했습니다.`);
