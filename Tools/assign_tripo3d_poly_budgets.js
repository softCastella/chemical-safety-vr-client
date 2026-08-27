const fs = require("fs");
const path = require("path");

const targetPath = path.join(
  path.resolve(__dirname, ".."),
  "output",
  "html",
  "3D_모델링_목록.html",
);

const budgets = new Map([
  ["metal_locker", "4k tri"],
  ["metal_locker (1)", "4k tri"],
  ["mask_locker", "4k tri"],
  ["safety_cabinet", "4k tri"],
  ["ppe_room_bench_0", "1.5k tri"],
  ["ppe_room_bench_1", "1.5k tri"],
  ["ppe_room_bench_2", "1.5k tri"],
  ["hazmat_suit", "4k tri"],
  ["hazmat_suit_3d_model", "4k tri"],
  ["hazmat_suit_hanger", "500 tri"],
  ["blue_rubber_gloves_3d_model_Clone1", "1.5k tri"],
  ["construction_helmet_3d_model_Clone1", "1.5k tri"],
  ["gas_mask_3d_model_Clone1", "2k tri"],
  ["rubber_boots_3d_model", "1.5k tri"],
  ["rubber_boots_3d_model_Clone1", "1.5k tri"],
  ["tactical_harness_3d_model", "2k tri"],
  ["orange_tape_roll_3d_model", "500 tri"],
  ["yellow_trash_bin_3d_model_Clone1", "1.5k tri"],
  ["yellow_waste_bin_3d_model", "1.5k tri"],
  ["fire_extinguisher_3d_model", "1.5k tri"],
  ["fire_extinguisher_3d_model_1", "1.5k tri"],
  ["hand+sanitizer+dispenser", "800 tri"],
  ["cleaning_cart", "3k tri"],
  ["flexible_duct_3d_model", "3k tri"],
  ["flexible_duct_3d_model_1", "3k tri"],
  ["industrial_stair_3d_model", "4k tri"],
  ["traffic_cone_3d_model", "800 tri"],
  ["traffic_cone_3d_model (1)", "800 tri"],
  ["safety_lock_3d_model", "800 tri"],
  ["lockout_padlock_3d_model", "500 tri"],
  ["red_lockout_padlock_3d_model", "500 tri"],
  ["padlock_3d_model", "500 tri"],
  ["red_padlock_3d_model", "500 tri"],
  ["industrial_control_panel_3d_model", "4k tri"],
  ["construction_worker_3d_model", "5k tri"],
  ["multi_gas_detector_3d_model", "1k tri"],
  ["multi_gas_detector_3d_model_Clone1", "1k tri"],
  ["rugged_two-way_radio_3d_model", "1k tri"],
  ["mixer+clean+brush", "1k tri"],
  ["tactical_flashlight_3d_model", "1k tri"],
]);

let html = fs.readFileSync(targetPath, "utf8");
const missing = [];
let updatedCount = 0;

html = html.replace(/<tr class="data-row"[\s\S]*?<\/tr>/g, (row) => {
  const resource = row.match(/data-field="resource">([\s\S]*?)<\/td>/)?.[1]?.trim();
  const budget = budgets.get(resource);
  if (!budget) {
    missing.push(resource || "(이름 없음)");
    return row;
  }
  updatedCount += 1;
  return row.replace(
    /(<td class="editable center-cell" contenteditable="true" data-field="poly">)[\s\S]*?(<\/td>)/,
    `$1${budget}$2`,
  );
});

if (missing.length) {
  throw new Error(`폴리 예산 매핑 누락: ${missing.join(", ")}`);
}

html = html
  .replace(
    `row.querySelector('[data-field="poly"]').textContent = "미정";`,
    `row.querySelector('[data-field="poly"]').textContent = "1k tri";`,
  )
  .replace("tripo3d-fbx-modeling-list-v2", "tripo3d-fbx-modeling-list-v3");

fs.writeFileSync(targetPath, html, "utf8");
console.log(`폴리 예산 ${updatedCount}개 적용 완료`);
