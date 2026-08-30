import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const hangulPattern = /[가-힣]/u;
const numberedListStartPattern = /^1\.\s+\S/u;

function meaningfulLines(message) {
  return message
    .replace(/\r\n?/gu, "\n")
    .split("\n")
    .filter((line) => !line.trimStart().startsWith("#"));
}

export function validateKoreanCommitMessage(message) {
  const lines = meaningfulLines(message);
  const subjectIndex = lines.findIndex((line) => line.trim().length > 0);
  const errors = [];

  if (subjectIndex < 0) {
    return ["커밋 메시지가 비어 있습니다."];
  }

  const subject = lines[subjectIndex].trim();
  if (!hangulPattern.test(subject)) {
    errors.push("커밋 제목에 한글을 포함해야 합니다.");
  }

  const bodyLines = lines.slice(subjectIndex + 1);
  if (!bodyLines.some((line) => numberedListStartPattern.test(line.trim()))) {
    errors.push("커밋 본문에 '1. '부터 시작하는 번호 목록을 추가해야 합니다.");
  }

  return errors;
}

function runSelfTest() {
  const cases = [
    {
      name: "한글 제목과 번호 목록 허용",
      message: "커밋 메시지 하네스 추가\n\n1. 한글 제목을 검사한다.\n2. 번호 목록을 검사한다.\n",
      valid: true,
    },
    {
      name: "영문 전용 제목 거부",
      message: "Add commit message harness\n\n1. 검증 규칙을 추가한다.\n",
      valid: false,
    },
    {
      name: "번호 목록 없는 본문 거부",
      message: "커밋 메시지 하네스 추가\n\n검증 규칙을 추가한다.\n",
      valid: false,
    },
    {
      name: "Git 주석 제외",
      message: "한글 커밋 제목\n\n1. 검증 결과를 기록한다.\n# 변경 파일 목록\n",
      valid: true,
    },
  ];

  for (const testCase of cases) {
    const errors = validateKoreanCommitMessage(testCase.message);
    const actual = errors.length === 0;
    if (actual !== testCase.valid) {
      throw new Error(`${testCase.name} 실패: ${errors.join(" ")}`);
    }
  }

  process.stdout.write(`[Korean Commit Message Harness] PASS: ${cases.length}개 사례\n`);
}

function runGit(arguments_) {
  const result = spawnSync("git", arguments_, {
    encoding: "utf8",
    windowsHide: true,
  });
  if (result.status !== 0) {
    const detail = result.stderr?.trim() || result.stdout?.trim() || "Git 명령 실패";
    throw new Error(detail);
  }
  return result.stdout.trim();
}

function gitCommitExists(commitId) {
  const result = spawnSync("git", ["cat-file", "-e", `${commitId}^{commit}`], {
    encoding: "utf8",
    windowsHide: true,
  });
  return result.status === 0;
}

function outgoingCommitIds(input, remoteName) {
  const zeroOid = /^0+$/u;
  const commits = new Set();

  for (const line of input.replace(/\r\n?/gu, "\n").split("\n")) {
    if (!line.trim()) continue;
    const [, localOid, , remoteOid] = line.trim().split(/\s+/u);
    if (!localOid || zeroOid.test(localOid)) continue;

    if (remoteOid && !zeroOid.test(remoteOid) && !gitCommitExists(remoteOid)) {
      throw new Error(
        `원격 기준 커밋 ${remoteOid.slice(0, 12)}이 로컬에 없습니다. ` +
        `git fetch ${remoteName || "<remote>"} 실행 후 다시 푸시하세요.`,
      );
    }

    const revisionArguments = remoteOid && !zeroOid.test(remoteOid)
      ? [`${remoteOid}..${localOid}`]
      : [localOid, "--not", remoteName ? `--remotes=${remoteName}` : "--remotes"];
    const output = runGit(["rev-list", ...revisionArguments]);
    for (const commitId of output.split("\n")) {
      if (commitId) commits.add(commitId);
    }
  }

  return [...commits];
}

function runPrePush(remoteName) {
  const input = fs.readFileSync(0, "utf8");
  const failures = [];

  let commitIds;
  try {
    commitIds = outgoingCommitIds(input, remoteName);
  } catch (error) {
    process.stderr.write(`푸시 하네스가 원격 전송을 중단했습니다.\n1. ${error.message}\n`);
    process.exitCode = 1;
    return;
  }

  for (const commitId of commitIds) {
    const message = runGit(["show", "-s", "--format=%B", commitId]);
    const errors = validateKoreanCommitMessage(message);
    if (errors.length > 0) {
      failures.push({ commitId, errors });
    }
  }

  if (failures.length === 0) return;

  process.stderr.write("푸시 하네스가 원격 전송을 거부했습니다.\n");
  for (const [index, failure] of failures.entries()) {
    process.stderr.write(`${index + 1}. ${failure.commitId.slice(0, 12)}: ${failure.errors.join(" ")}\n`);
  }
  process.exitCode = 1;
}

function runCommitHook(messagePath) {
  if (!messagePath) {
    process.stderr.write("커밋 메시지 파일 경로가 필요합니다.\n");
    process.exitCode = 2;
    return;
  }

  const message = fs.readFileSync(messagePath, "utf8");
  const errors = validateKoreanCommitMessage(message);
  if (errors.length === 0) {
    return;
  }

  process.stderr.write("커밋 메시지 하네스가 커밋을 거부했습니다.\n");
  for (const [index, error] of errors.entries()) {
    process.stderr.write(`${index + 1}. ${error}\n`);
  }
  process.stderr.write("예시:\n\n한글 커밋 제목\n\n1. 변경 내용을 기록한다.\n2. 검증 결과를 기록한다.\n");
  process.exitCode = 1;
}

const executedPath = process.argv[1] ? path.resolve(process.argv[1]) : "";
if (executedPath === fileURLToPath(import.meta.url)) {
  if (process.argv[2] === "--self-test") {
    runSelfTest();
  } else if (process.argv[2] === "--pre-push") {
    runPrePush(process.argv[3]);
  } else {
    runCommitHook(process.argv[2]);
  }
}
