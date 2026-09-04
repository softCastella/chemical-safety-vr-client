import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawn, spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const sessionIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
const lifecycleEventTypes = new Set(["turn_aborted", "task_complete", "task_started"]);

function normalizePath(value, platform = process.platform) {
  const normalized = path.resolve(value).replace(/[\\/]+$/u, "");
  return platform === "win32" ? normalized.toLocaleLowerCase("en-US") : normalized;
}

function collectJsonlFiles(directory) {
  if (!fs.existsSync(directory)) return [];

  const files = [];
  const pending = [directory];
  while (pending.length > 0) {
    const current = pending.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const entryPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        pending.push(entryPath);
      } else if (entry.isFile() && entry.name.endsWith(".jsonl")) {
        files.push(entryPath);
      }
    }
  }
  return files;
}

function readFirstLine(filePath, maximumBytes = 1024 * 1024) {
  const descriptor = fs.openSync(filePath, "r");
  try {
    const chunks = [];
    let totalBytes = 0;
    while (totalBytes < maximumBytes) {
      const buffer = Buffer.alloc(Math.min(64 * 1024, maximumBytes - totalBytes));
      const bytesRead = fs.readSync(descriptor, buffer, 0, buffer.length, totalBytes);
      if (bytesRead === 0) break;
      const chunk = buffer.subarray(0, bytesRead);
      const newlineIndex = chunk.indexOf(0x0a);
      if (newlineIndex >= 0) {
        chunks.push(chunk.subarray(0, newlineIndex));
        return Buffer.concat(chunks).toString("utf8").replace(/\r$/u, "");
      }
      chunks.push(chunk);
      totalBytes += bytesRead;
    }
    return Buffer.concat(chunks).toString("utf8").replace(/\r$/u, "");
  } finally {
    fs.closeSync(descriptor);
  }
}

export function readSessionMetadata(filePath) {
  let record;
  try {
    record = JSON.parse(readFirstLine(filePath));
  } catch {
    return null;
  }

  if (
    record?.type !== "session_meta" ||
    !sessionIdPattern.test(record?.payload?.id ?? "") ||
    typeof record?.payload?.cwd !== "string"
  ) {
    return null;
  }

  return {
    id: record.payload.id,
    cwd: record.payload.cwd,
    source: record.payload.source ?? null,
    cliVersion: record.payload.cli_version ?? null,
  };
}

function readLatestLifecycleEvent(filePath, maximumBytes = 256 * 1024) {
  const descriptor = fs.openSync(filePath, "r");
  try {
    const size = fs.fstatSync(descriptor).size;
    const offset = Math.max(0, size - maximumBytes);
    const buffer = Buffer.alloc(size - offset);
    fs.readSync(descriptor, buffer, 0, buffer.length, offset);
    const text = buffer.toString("utf8");
    const lines = text.split(/\r?\n/u);
    if (offset > 0) lines.shift();

    for (let index = lines.length - 1; index >= 0; index -= 1) {
      if (!lines[index].trim()) continue;
      try {
        const record = JSON.parse(lines[index]);
        if (record?.type === "event_msg" && lifecycleEventTypes.has(record?.payload?.type)) {
          return record.payload.type;
        }
      } catch {
        // The first tail line can be a partial JSON record.
      }
    }
    return null;
  } finally {
    fs.closeSync(descriptor);
  }
}

function readSessionNames(indexPath) {
  const names = new Map();
  if (!fs.existsSync(indexPath)) return names;

  for (const line of fs.readFileSync(indexPath, "utf8").split(/\r?\n/u)) {
    if (!line.trim()) continue;
    try {
      const record = JSON.parse(line);
      if (sessionIdPattern.test(record?.id ?? "") && typeof record?.thread_name === "string") {
        names.set(record.id, record.thread_name);
      }
    } catch {
      // A damaged index line must not hide otherwise valid session transcripts.
    }
  }
  return names;
}

export function findRecoverableSessions({
  codexHome = process.env.CODEX_HOME || path.join(os.homedir(), ".codex"),
  cwd = process.cwd(),
  excludedSessionIds = [],
  platform = process.platform,
} = {}) {
  const excluded = new Set([...excludedSessionIds].filter(Boolean));
  const targetCwd = normalizePath(cwd, platform);
  const names = readSessionNames(path.join(codexHome, "session_index.jsonl"));
  const sessionDirectory = path.join(codexHome, "sessions");
  const candidates = [];

  for (const filePath of collectJsonlFiles(sessionDirectory)) {
    const metadata = readSessionMetadata(filePath);
    if (!metadata || excluded.has(metadata.id)) continue;
    if (normalizePath(metadata.cwd, platform) !== targetCwd) continue;

    const stats = fs.statSync(filePath);
    const lifecycleEvent = readLatestLifecycleEvent(filePath);
    candidates.push({
      ...metadata,
      name: names.get(metadata.id) ?? null,
      filePath,
      updatedAt: stats.mtime.toISOString(),
      lifecycleEvent,
      interrupted: lifecycleEvent === "turn_aborted",
    });
  }

  return candidates.sort((left, right) => {
    const timeDifference = Date.parse(right.updatedAt) - Date.parse(left.updatedAt);
    return timeDifference || right.id.localeCompare(left.id);
  });
}

function currentSessionIds(environment = process.env) {
  return new Set([
    environment.CODEX_SESSION_ID,
    environment.CODEX_THREAD_ID,
  ].filter(Boolean));
}

function parseArguments(arguments_) {
  const options = {
    cwd: process.cwd(),
    codexHome: process.env.CODEX_HOME || path.join(os.homedir(), ".codex"),
    excludedSessionIds: currentSessionIds(),
    limit: 1,
    json: false,
    resume: false,
    open: false,
  };

  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === "--cwd") {
      options.cwd = arguments_[index += 1];
    } else if (argument === "--codex-home") {
      options.codexHome = arguments_[index += 1];
    } else if (argument === "--exclude-session") {
      options.excludedSessionIds.add(arguments_[index += 1]);
    } else if (argument === "--limit") {
      options.limit = Number.parseInt(arguments_[index += 1], 10);
    } else if (argument === "--json") {
      options.json = true;
    } else if (argument === "--resume") {
      options.resume = true;
    } else if (argument === "--open") {
      options.open = true;
    } else if (argument === "--self-test") {
      options.selfTest = true;
    } else if (argument === "--help") {
      options.help = true;
    } else {
      throw new Error(`알 수 없는 인자입니다: ${argument}`);
    }
  }

  if (!options.cwd || !options.codexHome) {
    throw new Error("--cwd와 --codex-home에는 경로가 필요합니다.");
  }
  if (!Number.isInteger(options.limit) || options.limit < 1 || options.limit > 20) {
    throw new Error("--limit은 1부터 20 사이의 정수여야 합니다.");
  }
  if (options.resume && options.open) {
    throw new Error("--resume과 --open은 함께 사용할 수 없습니다.");
  }
  return options;
}

function resolveWindowsCodexScript(environment = process.env, explicitPath = null) {
  if (explicitPath) return explicitPath;
  const candidate = environment.APPDATA
    ? path.join(environment.APPDATA, "npm", "codex.ps1")
    : null;
  if (candidate && fs.existsSync(candidate)) return candidate;
  throw new Error("Codex 실행 파일을 찾지 못했습니다. APPDATA의 npm/codex.ps1 설치를 확인하세요.");
}

export function createResumePlan(session, {
  platform = process.platform,
  environment = process.env,
  codexScript = null,
} = {}) {
  if (!sessionIdPattern.test(session?.id ?? "")) {
    throw new Error("유효한 Codex 세션 ID가 아닙니다.");
  }

  if (platform === "win32") {
    return {
      command: "powershell.exe",
      arguments: [
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        resolveWindowsCodexScript(environment, codexScript),
        "resume",
        session.id,
      ],
      cwd: session.cwd,
    };
  }

  return {
    command: "codex",
    arguments: ["resume", session.id],
    cwd: session.cwd,
  };
}

function resumeInCurrentTerminal(session) {
  const plan = createResumePlan(session);
  const result = spawnSync(plan.command, plan.arguments, {
    cwd: plan.cwd,
    stdio: "inherit",
  });
  if (result.error) throw result.error;
  if (result.status !== 0) {
    throw new Error(`Codex 세션 재개가 종료 코드 ${result.status}로 실패했습니다.`);
  }
}

function openInNewWindow(session) {
  if (process.platform !== "win32") {
    throw new Error("--open은 Windows에서만 지원합니다. 현재 터미널에서는 --resume을 사용하세요.");
  }

  const plan = createResumePlan(session);
  const child = spawn(plan.command, ["-NoExit", ...plan.arguments], {
    cwd: plan.cwd,
    detached: true,
    windowsHide: false,
    stdio: "ignore",
  });
  child.unref();
  return child.pid;
}

function printHelp() {
  process.stdout.write(`Codex 이전 세션 복구 하네스\n\n`);
  process.stdout.write(`사용법:\n`);
  process.stdout.write(`  node Tools/CodexSessionRecoveryHarness.mjs [옵션]\n\n`);
  process.stdout.write(`옵션:\n`);
  process.stdout.write(`  --cwd <path>              세션 작업 경로(기본값: 현재 경로)\n`);
  process.stdout.write(`  --codex-home <path>       Codex 데이터 경로(기본값: $CODEX_HOME 또는 ~/.codex)\n`);
  process.stdout.write(`  --exclude-session <uuid>  검색에서 제외할 세션 ID\n`);
  process.stdout.write(`  --limit <1-20>            출력할 후보 수(기본값: 1)\n`);
  process.stdout.write(`  --json                    JSON으로 출력\n`);
  process.stdout.write(`  --resume                  찾은 세션을 현재 터미널에서 재개\n`);
  process.stdout.write(`  --open                    찾은 세션을 새 Windows 창에서 재개\n`);
  process.stdout.write(`  --self-test               임시 세션으로 선택 규칙을 자체 검증\n`);
}

function runSelfTest() {
  const temporaryHome = fs.mkdtempSync(path.join(os.tmpdir(), "codex-session-recovery-"));
  try {
    const testCwd = path.join(temporaryHome, "server");
    const sessionDirectory = path.join(temporaryHome, "sessions", "2026", "09", "04");
    fs.mkdirSync(testCwd, { recursive: true });
    fs.mkdirSync(sessionDirectory, { recursive: true });

    const currentId = "11111111-1111-4111-8111-111111111111";
    const previousId = "22222222-2222-4222-8222-222222222222";
    for (const [id, lifecycleEvent] of [[previousId, "turn_aborted"], [currentId, "task_complete"]]) {
      const filePath = path.join(sessionDirectory, `rollout-${id}.jsonl`);
      const records = [
        { type: "session_meta", payload: { id, cwd: testCwd, source: "cli" } },
        { type: "event_msg", payload: { type: lifecycleEvent } },
      ];
      fs.writeFileSync(filePath, `${records.map((record) => JSON.stringify(record)).join("\n")}\n`, "utf8");
    }

    const sessions = findRecoverableSessions({
      codexHome: temporaryHome,
      cwd: testCwd,
      excludedSessionIds: [currentId],
    });
    if (sessions.length !== 1 || sessions[0].id !== previousId || !sessions[0].interrupted) {
      throw new Error("현재 세션 제외 또는 중단 세션 판별 규칙이 일치하지 않습니다.");
    }
    process.stdout.write("[Codex Session Recovery Harness] SELF TEST PASS\n");
  } finally {
    fs.rmSync(temporaryHome, { recursive: true, force: true });
  }
}

function printSessions(sessions, cwd) {
  process.stdout.write(`[Codex Session Recovery Harness] FOUND: ${sessions.length}개\n`);
  process.stdout.write(`대상 저장소: ${cwd}\n`);
  for (const [index, session] of sessions.entries()) {
    process.stdout.write(`${index + 1}. ${session.id}\n`);
    process.stdout.write(`   이름: ${session.name ?? "(이름 없음)"}\n`);
    process.stdout.write(`   갱신: ${session.updatedAt}\n`);
    process.stdout.write(`   상태: ${session.interrupted ? "중단됨" : session.lifecycleEvent ?? "미확인"}\n`);
    process.stdout.write(`   재개: codex resume ${session.id}\n`);
  }
}

async function runCli() {
  const options = parseArguments(process.argv.slice(2));
  if (options.selfTest) {
    runSelfTest();
    return;
  }
  if (options.help) {
    printHelp();
    return;
  }

  const sessions = findRecoverableSessions(options).slice(0, options.limit);
  if (sessions.length === 0) {
    process.stderr.write("[Codex Session Recovery Harness] FAIL: 같은 저장소의 이전 세션을 찾지 못했습니다.\n");
    process.exitCode = 1;
    return;
  }

  if (options.json) {
    process.stdout.write(`${JSON.stringify(sessions, null, 2)}\n`);
  } else {
    printSessions(sessions, options.cwd);
  }

  if (options.resume) {
    resumeInCurrentTerminal(sessions[0]);
  } else if (options.open) {
    const processId = openInNewWindow(sessions[0]);
    process.stdout.write(`[Codex Session Recovery Harness] OPENED: PID ${processId}\n`);
  }
}

const executedPath = process.argv[1] ? path.resolve(process.argv[1]) : "";
if (executedPath === fileURLToPath(import.meta.url)) {
  runCli().catch((error) => {
    process.stderr.write(`[Codex Session Recovery Harness] FAIL: ${error.message}\n`);
    process.exitCode = 1;
  });
}
