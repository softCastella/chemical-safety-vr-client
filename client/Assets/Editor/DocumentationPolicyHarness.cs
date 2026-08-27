using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DocumentationPolicyHarness
{
    const string AgentsFileName = "AGENTS.md";

    static readonly string[] RequiredRules =
    {
        "사용자가 영문 작성을 명시적으로 요청하지 않으면 프로젝트 문서는 한국어로 작성한다.",
        "새 문서를 만들기 전에 `Docs`와 `client/Assets/Docs`에서 동일 작업, 기능 또는 오류를 다루는 기존 문서가 있는지 먼저 확인한다.",
        "관련 기존 문서가 있으면 새 문서를 만들지 않고 해당 문서에 내용을 추가하거나 기존 내용을 수정한다.",
        "관련 기존 문서가 없거나 사용자가 문서 분리를 명시적으로 요청한 경우에만 새 문서를 만든다.",
        "같은 작업의 중복 문서를 실수로 만들었으면 내용을 기존 기준 문서에 통합하고 중복 문서를 제거한다.",
        "날짜가 붙는 작업 기록은 `Docs/MeetingNotes`의 회의록 또는 `Docs/Bug`의 버그 리포트로만 작성한다.",
        "`Daily`, `Worklog`, `데일리로그`, `작업일지` 형식의 문서나 파일명은 만들지 않는다.",
    };

    static readonly string[] AllowedDatedDocumentDirectories =
    {
        "MeetingNotes",
        "Bug",
    };

    static readonly string[] ForbiddenWorklogTokens =
    {
        "Daily",
        "Worklog",
        "데일리로그",
        "작업일지",
        "작업 일지",
    };

    [MenuItem("Tools/Documentation/Validate Authoring Policy")]
    public static void Validate()
    {
        string unityProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string repositoryRoot = ResolveRepositoryRoot(unityProjectRoot);
        string agentsPath = Path.Combine(repositoryRoot, AgentsFileName);
        List<string> failures = new List<string>();

        if (!File.Exists(agentsPath))
        {
            failures.Add(
                "Unity 프로젝트 루트 또는 상위 저장소에서 '" + AgentsFileName + "'를 찾지 못했습니다.");
        }
        else
        {
            string contents = File.ReadAllText(agentsPath);
            if (!contents.Contains("## 문서 작성 원칙"))
                failures.Add("'문서 작성 원칙' 섹션이 없습니다.");

            foreach (string requiredRule in RequiredRules)
            {
                if (!contents.Contains(requiredRule))
                    failures.Add("필수 문서 원칙이 없습니다: " + requiredRule);
            }
        }

        ValidateDocumentClassification(repositoryRoot, failures);

        if (failures.Count > 0)
        {
            string message = "문서 작성 원칙 검증 실패:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "문서 작성 원칙 검증 통과: 기본 한국어 작성, 기존 문서 우선 갱신, "
            + "명시적 요청 시에만 문서 분리, 날짜 작업 기록은 회의록/버그 리포트만 허용, "
            + "Daily/Worklog 금지 원칙이 유지되어 있습니다.");
    }

    static string ResolveRepositoryRoot(string unityProjectRoot)
    {
        DirectoryInfo current = new DirectoryInfo(unityProjectRoot);
        string nearestAgentsRoot = null;

        while (current != null)
        {
            string candidateRoot = current.FullName;
            bool hasAgents = File.Exists(Path.Combine(candidateRoot, AgentsFileName));
            if (hasAgents)
            {
                if (nearestAgentsRoot == null)
                    nearestAgentsRoot = candidateRoot;

                bool isRepositoryRoot = Directory.Exists(Path.Combine(candidateRoot, ".git"))
                    || Directory.Exists(Path.Combine(candidateRoot, "Docs"));
                if (isRepositoryRoot)
                    return candidateRoot;
            }

            current = current.Parent;
        }

        return nearestAgentsRoot ?? unityProjectRoot;
    }

    static void ValidateDocumentClassification(string repositoryRoot, List<string> failures)
    {
        foreach (string root in EnumerateDocumentationRoots(repositoryRoot))
        {
            foreach (string path in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(path);
                string title = ReadFirstNonEmptyLine(path);

                foreach (string token in ForbiddenWorklogTokens)
                {
                    if (fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        title.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        failures.Add(
                            "Daily/Worklog 형식 문서는 허용되지 않습니다: "
                            + MakeProjectRelativePath(repositoryRoot, path));
                        break;
                    }
                }

                if (!IsDatedMarkdown(fileName))
                    continue;

                string parentDirectory = new DirectoryInfo(Path.GetDirectoryName(path)).Name;
                if (Array.IndexOf(AllowedDatedDocumentDirectories, parentDirectory) < 0)
                {
                    failures.Add(
                        "날짜 작업 문서는 MeetingNotes 또는 Bug 아래에 있어야 합니다: "
                        + MakeProjectRelativePath(repositoryRoot, path));
                }
            }
        }
    }

    static IEnumerable<string> EnumerateDocumentationRoots(string repositoryRoot)
    {
        HashSet<string> uniqueRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] candidates =
        {
            Path.Combine(repositoryRoot, "Docs"),
            Path.Combine(Application.dataPath, "Docs"),
            Path.Combine(repositoryRoot, "Assets", "Docs"),
        };

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath) && uniqueRoots.Add(fullPath))
                yield return fullPath;
        }
    }

    static bool IsDatedMarkdown(string fileName)
    {
        if (fileName.Length < 14 || fileName[4] != '-' || fileName[7] != '-' || fileName[10] != '_')
            return false;

        for (int index = 0; index < 10; index++)
        {
            if (index == 4 || index == 7)
                continue;
            if (!char.IsDigit(fileName[index]))
                return false;
        }

        return true;
    }

    static string ReadFirstNonEmptyLine(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            if (!string.IsNullOrWhiteSpace(line))
                return line.Trim();
        }

        return string.Empty;
    }

    static string MakeProjectRelativePath(string repositoryRoot, string path)
    {
        return path.Substring(repositoryRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(Path.DirectorySeparatorChar, '/');
    }
}
