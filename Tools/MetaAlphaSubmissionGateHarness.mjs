import fs from "node:fs";
import path from "node:path";
import net from "node:net";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const clientRoot = path.resolve(scriptDirectory, "..");
const serverRoot = path.resolve(clientRoot, "..", "chemical-safety-vr-server");
const releasePlanRelativePath =
  "Docs/MeetingNotes/2026-08-25_Production_Server_Meta_Horizon_Release_Plan.md";
const releaseApkRelativePath =
  "Builds/MetaHorizonAlpha/ChemicalSafetyVR_Alpha_0_1_0_6.apk";
const developmentApkRelativePath =
  "Builds/MetaHorizonAlpha/ChemicalSafetyVR_TelemetryDev_0_1_0_5.apk";

const orderedGateMarkers = [
  "### 1단계 — code 5 정적·Release 아티팩트 고정",
  "### 2단계 — 기준 DB 격리 준비",
  "### 3단계 — Development APK 빌드·설치·일회성 LAN 주입",
  "### 4단계 — 첫 기준 회차 게이트",
  "### 5단계 — 6개 조합 실플레이 기준본 수집",
  "### 6단계 — 원본·DB·백업 승인",
  "### 7단계 — Meta Alpha Release 재설치·최종 실기",
  "### 8단계 — Alpha 제출 완료 판정",
];

const requiredPlayOrder =
  "ConfinedSpace/Test → ConfinedSpace/Training → ConfinedSpace/Education → " +
  "LeakResponse/Test → LeakResponse/Training → LeakResponse/Education";
const strategicAlphaMarkers = [
  "## 2026-09-09 결정: 심사 기간을 활용한 서버 우선 Alpha 전략",
  "대시보드 표시 항목은 Meta 심사 기간에 확정한다",
  "제출용 Release Alpha는 원본 텔레메트리를 서버로 전송할",
  "원본 이벤트를 삭제하거나 대시보드 지표에 맞춰 축약하지 않는다",
  "대시보드 완성 여부를 Alpha 제출 완료 조건으로 묶지 않는다",
  "서버 전송과 대시보드 완성을 하나의 완료 상태로 합치지 않는다",
];
const expectedEnabledScenes = [
  "Assets/Scenes/0_App.unity",
  "Assets/Scenes/1_Title.unity",
  "Assets/Scenes/2_Intro.unity",
  "Assets/Scenes/3_Loading.unity",
  "Assets/Scenes/4_PPE_Room.unity",
];

function read(relativePath, root = clientRoot) {
  return fs.readFileSync(path.join(root, relativePath), "utf8");
}

function normalizeLineEndings(value) {
  return value.replace(/\r\n?/gu, "\n");
}

function parseEnv(filePath) {
  const values = new Map();
  if (!fs.existsSync(filePath)) return values;
  for (const line of fs.readFileSync(filePath, "utf8").split(/\r?\n/u)) {
    const match = line.match(/^([A-Z0-9_]+)=(.*)$/u);
    if (match) values.set(match[1], match[2].trim());
  }
  return values;
}

function checkPort(host, port, timeoutMilliseconds = 1000) {
  return new Promise((resolve) => {
    const socket = net.createConnection({ host, port });
    const finish = (open) => {
      socket.destroy();
      resolve(open);
    };
    socket.setTimeout(timeoutMilliseconds);
    socket.once("connect", () => finish(true));
    socket.once("timeout", () => finish(false));
    socket.once("error", () => finish(false));
  });
}

async function checkHttp(url, { token, timeoutMilliseconds = 2000 } = {}) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);
  try {
    const headers = token ? { Authorization: `Bearer ${token}` } : undefined;
    const response = await fetch(url, {
      headers,
      redirect: "manual",
      signal: controller.signal,
    });
    return response.status;
  } catch {
    return null;
  } finally {
    clearTimeout(timeout);
  }
}

const failures = [];
const blockers = [];
const facts = [];
let baselineInfrastructureReady = true;
let developmentApkMissing = false;

const releasePlanPath = path.join(clientRoot, releasePlanRelativePath);
const serverReleasePlanPath = path.join(serverRoot, releasePlanRelativePath);
if (!fs.existsSync(releasePlanPath)) {
  failures.push(`기준 출시 문서가 없습니다: ${releasePlanRelativePath}`);
} else {
  const releasePlan = fs.readFileSync(releasePlanPath, "utf8");
  let previousIndex = -1;
  for (const marker of orderedGateMarkers) {
    const markerIndex = releasePlan.indexOf(marker);
    if (markerIndex < 0) failures.push(`출시 문서에 게이트가 없습니다: ${marker}`);
    else if (markerIndex <= previousIndex) failures.push(`출시 게이트 순서가 뒤바뀌었습니다: ${marker}`);
    previousIndex = markerIndex;
  }
  if (!releasePlan.includes(requiredPlayOrder))
    failures.push("출시 문서의 6개 실플레이 순서가 기준과 다릅니다.");
  if (!releasePlan.includes("HMD 주변 시야 깨짐은 제출 차단 결함"))
    failures.push("HMD 주변 시야 깨짐 제출 차단 게이트가 출시 문서에 없습니다.");
  if (!releasePlan.includes("USB는 ADB 로그·설정 주입에만 사용하고 Quest Link를 시작하지 않는다"))
    failures.push("집 환경 독립 실행과 Quest Link 분리 게이트가 출시 문서에 없습니다.");
  if (!releasePlan.includes("완료된 기준 회차를 처음부터 다시 수행하지 않는다"))
    failures.push("HMD 불안정 시 완료 기준 회차 보존·재개 규칙이 출시 문서에 없습니다.");
  if (!releasePlan.includes("## 2026-09-07 학원 PC·Workbench 환경 인수인계"))
    failures.push("학원 PC·Workbench 환경 인수인계 절이 출시 문서에 없습니다.");
  if (!releasePlan.includes("학원 PC의 기존 로컬 파일 보존"))
    failures.push("학원 로컬 .env 보존 규칙이 출시 문서에 없습니다.");
  if (!releasePlan.includes("기존 root나 앱 계정 암호를 변경하지 않는다"))
    failures.push("학원 DB 연결 실패 시 기존 계정 보호 규칙이 출시 문서에 없습니다.");
  if (!releasePlan.includes("TYCHE_TELEMETRY_UPLOAD_TOKEN"))
    failures.push("학원 Unity·서버 토큰 대응 규칙이 출시 문서에 없습니다.");
  if (!releasePlan.includes("## 2026-09-08 다른 PC 아침 재개 체크포인트"))
    failures.push("다른 PC 아침 재개 체크포인트가 출시 문서에 없습니다.");
  if (
    !releasePlan.includes(
      "PPE Room 진입 위치에서 5분 → PPE 진열장 앞으로 이동 → 진열장 위치에서 5분",
    )
  )
    failures.push("PPE Room 위치별 5분 비교 절차가 출시 문서에 없습니다.");
  if (!releasePlan.includes("Development APK는 Git에 포함되지 않는다"))
    failures.push("다른 PC의 Development APK 전달·재빌드 규칙이 출시 문서에 없습니다.");
  if (!releasePlan.includes("아침 검증은 **다른 Quest 기기**에서 수행"))
    failures.push("아침 검증 대상이 다른 Quest라는 인수인계 조건이 출시 문서에 없습니다.");
  for (const marker of strategicAlphaMarkers) {
    if (!releasePlan.includes(marker))
      failures.push(`서버 우선 Alpha 전략 계약이 출시 문서에 없습니다: ${marker}`);
  }
}

if (!fs.existsSync(serverReleasePlanPath)) {
  failures.push("서버 저장소의 공용 출시 문서 미러가 없습니다.");
} else if (
  fs.existsSync(releasePlanPath) &&
  normalizeLineEndings(fs.readFileSync(releasePlanPath, "utf8")) !==
    normalizeLineEndings(fs.readFileSync(serverReleasePlanPath, "utf8"))
) {
  failures.push("클라이언트 기준 출시 문서와 서버 미러가 일치하지 않습니다.");
}

const projectSettings = read("ProjectSettings/ProjectSettings.asset");
if (!/^\s*AndroidBundleVersionCode:\s*6\s*$/mu.test(projectSettings))
  failures.push("AndroidBundleVersionCode가 code 6이 아닙니다.");

const buildSettings = read("ProjectSettings/EditorBuildSettings.asset");
const enabledScenes = [];
for (const match of buildSettings.matchAll(/enabled:\s*1[\s\S]*?path:\s*([^\r\n]+)/gu))
  enabledScenes.push(match[1].trim());
if (JSON.stringify(enabledScenes) !== JSON.stringify(expectedEnabledScenes)) {
  failures.push(
    `활성 빌드 씬 순서가 다릅니다: ${enabledScenes.join(" → ") || "없음"}`,
  );
}

const releaseApkPath = path.join(clientRoot, releaseApkRelativePath);
if (!fs.existsSync(releaseApkPath) || fs.statSync(releaseApkPath).size === 0) {
  blockers.push(`code 6 Release APK가 없습니다: ${releaseApkRelativePath}`);
} else {
  facts.push(
    `Release APK=${releaseApkRelativePath} (${fs.statSync(releaseApkPath).size} bytes)`,
  );
}

const uploaderSource = read("Assets/Scripts/TycheTrainingTelemetryUploader.cs");
const metaAuthenticatorSource = read(
  "Assets/Scripts/TycheMetaSessionAuthenticator.cs",
);
const appSceneSource = read("Assets/Scenes/0_App.unity");
if (!uploaderSource.includes("UNITY_ANDROID && !DEVELOPMENT_BUILD && !UNITY_EDITOR"))
  failures.push(
    "Android Release 전송 경계를 찾지 못했습니다.",
  );
if (!uploaderSource.includes("productionServerBaseUrl"))
  failures.push("Release 서버 주소가 Inspector 작성값으로 분리되어 있지 않습니다.");
if (!metaAuthenticatorSource.includes("Users.GetUserProof()"))
  failures.push("Release 인증기가 Meta User Proof를 요청하지 않습니다.");
if (!metaAuthenticatorSource.includes("/api/training-telemetry/auth/meta"))
  failures.push("Release 인증기의 서버 교환 API 경로가 없습니다.");
if (!metaAuthenticatorSource.includes("uri.Scheme != Uri.UriSchemeHttps"))
  failures.push("Release 인증기가 공개 HTTPS를 강제하지 않습니다.");
if (!appSceneSource.includes("productionServerBaseUrl: https://immersa.tycheworks.com"))
  failures.push("0_App의 Release HTTPS 주소가 운영 호스트와 일치하지 않습니다.");
if (!appSceneSource.includes("logAppScopedUserId: 0"))
  failures.push("0_App가 Release에서 Meta 앱 범위 사용자 ID 원문을 로그에 남깁니다.");

if (fs.existsSync(serverRoot)) {
  const serverAppSource = read("src/app.js", serverRoot);
  const serverMetaAuthSource = read(
    "src/modules/training-telemetry/training-telemetry-meta-auth.js",
    serverRoot,
  );
  const serverAuthSource = read(
    "src/modules/training-telemetry/training-telemetry-auth.js",
    serverRoot,
  );
  const serverRoutesSource = read(
    "src/modules/training-telemetry/training-telemetry-routes.js",
    serverRoot,
  );
  const serverEnvExample = read(".env.example", serverRoot);
  const serverNginxTelemetrySource = read(
    "ops/nginx/tycheworks-immersa-training-telemetry.conf",
    serverRoot,
  );
  if (!serverRoutesSource.includes('router.post("/auth/meta"'))
    failures.push("서버의 Meta User Proof 교환 API가 연결되어 있지 않습니다.");
  if (!serverMetaAuthSource.includes("https://graph.oculus.com/user_nonce_validate"))
    failures.push("서버가 Meta 공식 User Proof 검증 엔드포인트를 사용하지 않습니다.");
  if (!serverMetaAuthSource.includes('method: "POST"') ||
      !serverMetaAuthSource.includes('"Content-Type": "application/x-www-form-urlencoded"'))
    failures.push("서버의 Meta User Proof 검증이 공식 POST 폼 계약을 사용하지 않습니다.");
  if (!serverMetaAuthSource.includes('tokenType = "training-telemetry-upload"'))
    failures.push("서버 단기 토큰의 용도 제한 claim이 없습니다.");
  if (!serverAuthSource.includes("authorizeRead"))
    failures.push("Release 업로드 토큰과 텔레메트리 조회 권한이 분리되어 있지 않습니다.");
  if (!serverAppSource.includes("enableMetaTrainingTelemetryAuth"))
    failures.push("서버의 Meta Release 인증 기능 플래그가 없습니다.");
  if (!serverNginxTelemetrySource.includes("location ^~ /api/training-telemetry/") ||
      !serverNginxTelemetrySource.includes("limit_except POST { deny all; }") ||
      !serverNginxTelemetrySource.includes("proxy_pass http://127.0.0.1:3000"))
    failures.push("IMMERSA 운영 Nginx의 POST 전용 텔레메트리 프록시 구성이 없습니다.");
  for (const requiredName of [
    "ENABLE_META_TRAINING_TELEMETRY_AUTH",
    "META_PLATFORM_APP_ACCESS_TOKEN",
    "TRAINING_TELEMETRY_SESSION_TOKEN_SECRET",
    "TRAINING_TELEMETRY_SESSION_TOKEN_TTL_SECONDS",
  ]) {
    if (!serverEnvExample.includes(`${requiredName}=`))
      failures.push(`서버 .env.example에 Release 인증 설정이 없습니다: ${requiredName}`);
  }
}
const setupSource = read("Assets/Editor/TycheTrainingTelemetryUploaderSetup.cs");
if (!setupSource.includes("Inject Quest Development LAN Configuration"))
  failures.push("Quest Development LAN 일회성 주입 메뉴를 찾지 못했습니다.");

if (!fs.existsSync(serverRoot)) {
  blockers.push(`서버 저장소를 찾지 못했습니다: ${serverRoot}`);
  baselineInfrastructureReady = false;
} else {
  const envPath = path.join(serverRoot, ".env");
  const env = parseEnv(envPath);
  if (env.get("DB_NAME") !== "tyche_training_baseline") {
    blockers.push("서버 .env의 DB_NAME을 tyche_training_baseline으로 명시해야 합니다.");
    baselineInfrastructureReady = false;
  }
  if (!env.get("DB_USER")) {
    blockers.push("서버 .env의 기준 DB 사용자 DB_USER가 없습니다.");
    baselineInfrastructureReady = false;
  }
  if (!env.get("DB_PASSWORD")) {
    blockers.push("서버 .env의 기준 DB 비밀번호 DB_PASSWORD가 없습니다.");
    baselineInfrastructureReady = false;
  }
  if (env.get("ENABLE_TRAINING_TELEMETRY_INGEST") !== "true") {
    blockers.push("서버 텔레메트리 수집 플래그가 true가 아닙니다.");
    baselineInfrastructureReady = false;
  }
  const token = env.get("TRAINING_TELEMETRY_UPLOAD_TOKEN") ?? "";
  if (!/^[!-~]{16,512}$/u.test(token)) {
    blockers.push("서버 업로드 토큰이 16~512자 공백 없는 ASCII 조건을 충족하지 않습니다.");
    baselineInfrastructureReady = false;
  }

  if (
    env.get("DB_NAME") === "tyche_training_baseline" &&
    env.get("DB_USER") &&
    env.get("DB_PASSWORD")
  ) {
    try {
      const requireFromServer = createRequire(path.join(serverRoot, "package.json"));
      const mysql = requireFromServer("mysql2/promise");
      const connection = await mysql.createConnection({
        host: env.get("DB_HOST") || "127.0.0.1",
        port: Number(env.get("DB_PORT") || 3306),
        user: env.get("DB_USER"),
        password: env.get("DB_PASSWORD"),
      });
      const [databaseRows] = await connection.query(
        "SELECT SCHEMA_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = ?",
        ["tyche_training_baseline"],
      );
      if (databaseRows.length !== 1) {
        blockers.push("tyche_training_baseline DB가 생성되어 있지 않습니다.");
        baselineInfrastructureReady = false;
      } else {
        const migrationFiles = fs
          .readdirSync(path.join(serverRoot, "db", "migrations"))
          .filter((name) => name.endsWith(".sql"))
          .sort();
        const [migrationRows] = await connection.query(
          "SELECT name FROM tyche_training_baseline.schema_migrations ORDER BY name",
        );
        const applied = migrationRows.map((row) => row.name);
        if (JSON.stringify(applied) !== JSON.stringify(migrationFiles)) {
          blockers.push("기준 DB migration이 현재 서버 소스 전체와 일치하지 않습니다.");
          baselineInfrastructureReady = false;
        } else {
          facts.push(`기준 DB migration=${applied.length}개`);
          const [[sessionCountRow]] = await connection.query(
            "SELECT COUNT(*) AS count FROM tyche_training_baseline.training_telemetry_sessions",
          );
          const [[eventCountRow]] = await connection.query(
            "SELECT COUNT(*) AS count FROM tyche_training_baseline.training_telemetry_events",
          );
          facts.push(
            `기준 DB 초기 수집량=session ${Number(sessionCountRow.count)} / event ${Number(eventCountRow.count)}`,
          );
        }
      }
      await connection.end();
    } catch (error) {
      blockers.push(`기준 DB 읽기 전용 확인 실패: ${error.code ?? error.message}`);
      baselineInfrastructureReady = false;
    }
  }

  if (await checkPort("127.0.0.1", 3000)) {
    const healthStatus = await checkHttp("http://127.0.0.1:3000/api/health");
    const testPageStatus = await checkHttp(
      "http://127.0.0.1:3000/telemetry-ingest-test/",
    );
    const sessionsStatus = await checkHttp(
      "http://127.0.0.1:3000/api/training-telemetry/sessions?limit=1",
      { token },
    );
    if (healthStatus === 200 && testPageStatus === 200 && sessionsStatus === 200) {
      facts.push("기준 서버 HTTP=health/test-page/authenticated-sessions 200");
    } else {
      blockers.push(
        `기준 서버 HTTP 확인 실패: health=${healthStatus ?? "연결 실패"}, test=${testPageStatus ?? "연결 실패"}, sessions=${sessionsStatus ?? "연결 실패"}`,
      );
      baselineInfrastructureReady = false;
    }
  } else {
    blockers.push("기준 Express 서버 TCP 3000이 열려 있지 않습니다.");
    baselineInfrastructureReady = false;
  }
}

if (!(await checkPort("127.0.0.1", 3306))) {
  blockers.push("로컬 MariaDB TCP 3306이 열려 있지 않습니다.");
  baselineInfrastructureReady = false;
}

const developmentApkPath = path.join(clientRoot, developmentApkRelativePath);
if (!fs.existsSync(developmentApkPath) || fs.statSync(developmentApkPath).size === 0) {
  blockers.push(`code 5 Development APK가 없습니다: ${developmentApkRelativePath}`);
  developmentApkMissing = true;
} else facts.push(`Development APK=${developmentApkRelativePath}`);

if (failures.length > 0) {
  process.stderr.write(
    `[Meta Alpha Submission Gate] FAIL\n${failures
      .map((failure, index) => `${index + 1}. ${failure}`)
      .join("\n")}\n`,
  );
  process.exitCode = 1;
} else if (blockers.length > 0) {
  const next = !baselineInfrastructureReady
    ? "NEXT: 2단계 기준 DB·서버 준비를 완료하십시오. 앱은 아직 실행하지 않습니다."
    : developmentApkMissing
      ? "NEXT: 3단계 Development Build만 생성하십시오. Build And Run과 플레이는 아직 하지 않습니다."
      : "NEXT: 표시된 대기 항목을 해결하십시오. 앱은 아직 실행하지 않습니다.";
  process.stdout.write(
    `[Meta Alpha Submission Gate] WAIT\n` +
      `${facts.map((fact, index) => `${index + 1}. 확인: ${fact}`).join("\n")}\n` +
      `${blockers.map((blocker, index) => `${index + 1}. 대기: ${blocker}`).join("\n")}\n` +
      `${next}\n`,
  );
  process.exitCode = 2;
} else {
  process.stdout.write(
    `[Meta Alpha Submission Gate] READY: 정적 계약, 공용 문서, Release/Development APK와 기준 DB 준비를 확인했습니다.\n` +
      `${facts.map((fact, index) => `${index + 1}. ${fact}`).join("\n")}\n` +
      "NEXT: Development APK 설치 후 일회성 LAN 설정을 주입하고 ConfinedSpace/Test 첫 기준 회차만 실행하십시오.\n",
  );
}
