const fs = require("fs");
const path = require("path");

const targetPath = path.join(
  path.resolve(__dirname, ".."),
  "output",
  "html",
  "3D_모델링_목록.html",
);

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

let html = fs.readFileSync(targetPath, "utf8");
let backgroundCount = 0;
let ppeCount = 0;

html = html.replace(/<tr class="data-row"[\s\S]*?<\/tr>/g, (row) => {
  const resource = row.match(/data-field="resource">([\s\S]*?)<\/td>/)?.[1]?.trim();
  let updated = row.replaceAll("수납/대기 가구", "대기 공간 가구");

  if (resource === "tactical_harness_3d_model") {
    ppeCount += 1;
    updated = updated.replace(
      /(<td class="editable center-cell" contenteditable="true" data-field="usage">)[\s\S]*?(<\/td>)/,
      "$1등지게형 송기마스크 하네스$2",
    );
    updated = updated.replace(
      /(<td class="editable" contenteditable="true" data-field="purpose">)[\s\S]*?(<\/td>)/,
      "$1송기마스크와 전동 송풍기를 공기 호스로 연결하고 송풍기와 호스를 몸에 지지·고정$2",
    );
    return updated.replace(
      /(<td class="editable" contenteditable="true" data-field="interaction">)[\s\S]*?(<\/td>)/,
      "$1Grab/호스 연결부 점검/등지게 착용 확인$2",
    );
  }

  if (ppeResources.has(resource)) {
    ppeCount += 1;
    return updated;
  }

  backgroundCount += 1;
  return updated.replace(
    /(<td class="editable" contenteditable="true" data-field="purpose">)[\s\S]*?(<\/td>)/,
    "$1배경용 소품$2",
  );
});

fs.writeFileSync(targetPath, html, "utf8");
console.log(`PPE용품 ${ppeCount}개 유지, 배경용 소품 ${backgroundCount}개 적용`);
