import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { TextDecoder } from "node:util";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..");
const utf8Decoder = new TextDecoder("utf-8", { fatal: true });

const requiredFiles = [
  "AGENTS.md",
  "CLAUDE.md",
  "Docs/ValidationHarnessGuide.md",
  "Tools/KoreanCommitMessageHarness.mjs",
  "Assets/Editor/DocumentationPolicyHarness.cs",
  "Assets/Editor/SceneDependencyValidationHarness.cs",
  "Assets/Editor/PPELocomotionPpeRegressionValidationHarness.cs",
  "Assets/Editor/PPERoomCardRaySelectionHarness.cs",
  "Assets/Editor/PPETrainTestModeValidationHarness.cs",
  "Assets/Editor/PPERoomEnvironmentCollisionSetup.cs",
  "Assets/Editor/PPEObjectSpatialDiagnosticHarness.cs",
  "Assets/Editor/PPETrainingDataContractHarness.cs",
  "Assets/Editor/XRNearFarReticleSafetyHarness.cs",
  "Assets/Editor/XRSessionForwardAlignmentValidationHarness.cs",
  "Assets/Editor/MetaQuestAndroidBuildValidationHarness.cs",
];

function readUtf8(relativePath) {
  const absolutePath = path.join(repositoryRoot, relativePath);
  return utf8Decoder.decode(fs.readFileSync(absolutePath));
}

const failures = [];

for (const relativePath of requiredFiles) {
  const absolutePath = path.join(repositoryRoot, relativePath);
  if (!fs.existsSync(absolutePath)) {
    failures.push(`필수 파일이 없습니다: ${relativePath}`);
    continue;
  }

  try {
    readUtf8(relativePath);
  } catch (error) {
    failures.push(`UTF-8로 읽을 수 없습니다: ${relativePath} (${error.message})`);
  }
}

if (failures.length === 0) {
  const claudeInstructions = readUtf8("CLAUDE.md");
  const agentsInstructions = readUtf8("AGENTS.md");
  const routingGuide = readUtf8("Docs/ValidationHarnessGuide.md");

  for (const requiredImport of ["@AGENTS.md", "@Docs/ValidationHarnessGuide.md"]) {
    if (!claudeInstructions.split(/\r?\n/u).some((line) => line.trim() === requiredImport))
      failures.push(`CLAUDE.md 가져오기가 없습니다: ${requiredImport}`);
  }

  for (const requiredReference of [
    "Docs/ValidationHarnessGuide.md",
    "node Tools/AgentHandoffHarness.mjs",
  ]) {
    if (!agentsInstructions.includes(requiredReference))
      failures.push(`AGENTS.md 공통 인수인계 참조가 없습니다: ${requiredReference}`);
  }

  for (const requiredHarness of [
    "DocumentationPolicyHarness.Validate",
    "SceneDependencyValidationHarness.Validate",
    "PPELocomotionPpeRegressionValidationHarness",
    "PPERoomCardRaySelectionHarness.Validate",
    "PPETrainTestModeValidationHarness",
    "PPERoomEnvironmentCollisionValidationHarness.Validate",
    "PPETrainingDataContractHarness.Validate",
    "XRNearFarReticleSafetyHarness.Validate",
    "XRSessionForwardAlignmentValidationHarness.Validate",
  ]) {
    if (!routingGuide.includes(requiredHarness))
      failures.push(`하네스 라우팅 참조가 없습니다: ${requiredHarness}`);
  }

  if (!routingGuide.includes("정적 확인") || !routingGuide.includes("Quest/OpenXR 확인"))
    failures.push("검증 수준 분리 규칙이 없습니다.");
}

if (failures.length > 0) {
  const numberedFailures = failures
    .map((failure, index) => `${index + 1}. ${failure}`)
    .join("\n");
  process.stderr.write(
    `[Agent Handoff Harness] FAIL\n${numberedFailures}\n`,
  );
  process.exitCode = 1;
} else {
  process.stdout.write(
    `[Agent Handoff Harness] PASS: ${requiredFiles.length}개 파일, Claude 가져오기, ` +
    "공통 지침 참조, 핵심 Unity 하네스 라우팅과 검증 수준 분리를 확인했습니다.\n",
  );
}
