using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬과 프리팹이 참조하는 외부 모델 에셋을 확인하는 감사 도구입니다.
/// 이 도구는 에셋을 삭제하거나 이동하지 않습니다.
/// </summary>
public sealed class AssetDependencyAuditWindow : EditorWindow
{
    private const string MenuPath = "Tools/Asset Audit/Find Referenced Source Models";
    private const string ProblemScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    private static readonly string[] ModelExtensions =
    {
        ".fbx", ".obj", ".dae", ".gltf", ".glb", ".blend", ".3ds", ".ma", ".mb", ".max"
    };

    private readonly List<ModelAuditEntry> _referencedModels = new List<ModelAuditEntry>();
    private readonly List<string> _unreferencedModels = new List<string>();
    private Vector2 _scrollPosition;
    private string _status = "아직 검사를 실행하지 않았습니다.";
    private string _auditScope = "";
    private bool _includeUnreferencedCandidates;
    private int _scannedConsumers;
    private int _scannedModelCount;
    private int _errorCount;

    [MenuItem(MenuPath, priority = 300)]
    private static void OpenWindow()
    {
        var window = GetWindow<AssetDependencyAuditWindow>("Asset Dependency Audit");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("프로젝트 모델 에셋 의존성 감사", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assets 아래의 모든 .unity 및 .prefab을 검사해 실제로 참조되는 모델 원본과 사용처를 표시합니다. " +
            "삭제·이동은 수행하지 않으며, Resources.Load 문자열이나 Addressables 설정처럼 직렬화 의존성에 나타나지 않는 사용처는 별도 확인이 필요합니다.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("문제 씬만 검사", GUILayout.Height(28f)))
            {
                RunSceneAudit(ProblemScenePath);
            }

            if (GUILayout.Button("현재 열린 씬만 검사", GUILayout.Height(28f)))
            {
                RunActiveSceneAudit();
            }

            if (GUILayout.Button("전체 씬·프리팹 검사", GUILayout.Height(28f)))
            {
                RunAudit(FindConsumerPaths(), true, "Assets 아래 전체 씬·프리팹");
            }

            using (new EditorGUI.DisabledScope(_referencedModels.Count == 0 && _unreferencedModels.Count == 0))
            {
                if (GUILayout.Button("리포트 내보내기", GUILayout.Height(28f)))
                {
                    ExportReport();
                }
            }
        }

        EditorGUILayout.LabelField(_status, EditorStyles.miniLabel);

        if (_scannedConsumers > 0 || _scannedModelCount > 0)
        {
            EditorGUILayout.LabelField(
                string.Format(
                    "검사 범위: {0} / 씬·프리팹 {1}개 / 모델 {2}개 / 참조 모델 {3}개 / 미참조 후보 {4}개 / 오류 {5}개",
                    _auditScope,
                    _scannedConsumers,
                    _scannedModelCount,
                    _referencedModels.Count,
                    _unreferencedModels.Count,
                    _errorCount),
                EditorStyles.boldLabel);
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (_referencedModels.Count > 0)
        {
            EditorGUILayout.LabelField("참조 중인 모델 원본", EditorStyles.boldLabel);
            foreach (var entry in _referencedModels)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(entry.ModelPath, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("사용처 " + entry.Consumers.Count + "개", EditorStyles.miniLabel);
                    foreach (var consumer in entry.Consumers)
                    {
                        EditorGUILayout.LabelField("  · " + consumer);
                    }
                }
            }
        }

        if (_includeUnreferencedCandidates && _unreferencedModels.Count > 0)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("미참조 후보 모델", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "이 목록은 씬·프리팹의 직렬화 참조가 없는 후보일 뿐입니다. 코드 로드, Addressables, 패키지 설정, 다른 비직렬화 경로를 확인한 뒤 삭제해야 합니다.",
                MessageType.Warning);
            foreach (var modelPath in _unreferencedModels)
            {
                EditorGUILayout.LabelField("  · " + modelPath);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunActiveSceneAudit()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path))
        {
            EditorUtility.DisplayDialog("씬을 찾을 수 없습니다.", "현재 열린 씬을 먼저 저장한 뒤 다시 실행하세요.", "확인");
            return;
        }

        RunSceneAudit(activeScene.path);
    }

    private void RunSceneAudit(string scenePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            EditorUtility.DisplayDialog("씬을 찾을 수 없습니다.", scenePath, "확인");
            return;
        }

        RunAudit(FindSceneConsumerPaths(scenePath), false, scenePath);
    }

    private void RunAudit(List<string> consumerPaths, bool includeUnreferencedCandidates, string auditScope)
    {
        _referencedModels.Clear();
        _unreferencedModels.Clear();
        _scannedConsumers = 0;
        _scannedModelCount = 0;
        _errorCount = 0;
        _auditScope = auditScope;
        _includeUnreferencedCandidates = includeUnreferencedCandidates;
        _status = "검사 중...";
        Repaint();

        var modelConsumers = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var modelPaths = includeUnreferencedCandidates ? FindModelPaths() : new List<string>();
        _scannedConsumers = consumerPaths.Count;
        _scannedModelCount = modelPaths.Count;

        try
        {
            for (var index = 0; index < consumerPaths.Count; index++)
            {
                var consumerPath = consumerPaths[index];
                EditorUtility.DisplayProgressBar(
                    "Asset Dependency Audit",
                    consumerPath,
                    consumerPaths.Count == 0 ? 1f : (float)index / consumerPaths.Count);

                try
                {
                    var dependencies = AssetDatabase.GetDependencies(consumerPath, true);
                    foreach (var dependency in dependencies)
                    {
                        var normalizedDependency = NormalizePath(dependency);
                        if (!IsProjectModelPath(normalizedDependency))
                        {
                            continue;
                        }

                        HashSet<string> consumers;
                        if (!modelConsumers.TryGetValue(normalizedDependency, out consumers))
                        {
                            consumers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            modelConsumers.Add(normalizedDependency, consumers);
                        }

                        consumers.Add(consumerPath);
                    }
                }
                catch (Exception exception)
                {
                    _errorCount++;
                    Debug.LogWarning("Asset Dependency Audit: " + consumerPath + " 검사 실패\n" + exception);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!includeUnreferencedCandidates)
        {
            modelPaths = modelConsumers.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
            _scannedModelCount = modelPaths.Count;
        }

        foreach (var modelPath in modelPaths)
        {
            HashSet<string> consumers;
            if (modelConsumers.TryGetValue(modelPath, out consumers))
            {
                _referencedModels.Add(new ModelAuditEntry
                {
                    ModelPath = modelPath,
                    Consumers = consumers.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
                });
            }
            else
            {
                _unreferencedModels.Add(modelPath);
            }
        }

        _referencedModels.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ModelPath, right.ModelPath));
        _unreferencedModels.Sort(StringComparer.OrdinalIgnoreCase.Compare);
        _status = "검사 완료: 삭제·이동 작업은 수행하지 않았습니다.";
        Repaint();
    }

    private static List<string> FindSceneConsumerPaths(string scenePath)
    {
        var consumerPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizePath(scenePath)
        };

        try
        {
            foreach (var dependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                var normalizedDependency = NormalizePath(dependency);
                if (normalizedDependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    consumerPaths.Add(normalizedDependency);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Asset Dependency Audit: 씬 프리팹 의존성 수집 실패\n" + exception);
        }

        return consumerPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> FindConsumerPaths()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
            .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }));

        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(NormalizePath)
            .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                          path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> FindModelPaths()
    {
        return AssetDatabase.FindAssets("t:Model", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(NormalizePath)
            .Where(IsProjectModelPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ExportReport()
    {
        var defaultDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs"));
        Directory.CreateDirectory(defaultDirectory);
        var defaultName = "AssetDependencyAudit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md";
        var outputPath = EditorUtility.SaveFilePanel(
            "Asset Dependency Audit 리포트 저장",
            defaultDirectory,
            defaultName,
            "md");

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        File.WriteAllText(outputPath, BuildReport(), new UTF8Encoding(false));
        _status = "리포트 저장 완료: " + outputPath;
        Repaint();
        EditorUtility.RevealInFinder(outputPath);
    }

    private string BuildReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# 에셋 의존성 감사 리포트");
        builder.AppendLine();
        builder.AppendLine("- 검사 시각: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("- 검사 범위: `" + _auditScope + "`");
        builder.AppendLine("- 씬·프리팹 검사 수: " + _scannedConsumers);
        builder.AppendLine("- 모델 에셋 검사 수: " + _scannedModelCount);
        builder.AppendLine("- 참조 중인 모델 수: " + _referencedModels.Count);
        builder.AppendLine("- 미참조 후보 수: " + _unreferencedModels.Count);
        builder.AppendLine("- 검사 오류 수: " + _errorCount);
        builder.AppendLine();
        builder.AppendLine("> 이 리포트는 에셋을 삭제하거나 이동하지 않았습니다. 미참조 후보도 코드 로드, Addressables, 패키지 설정을 별도로 확인해야 합니다.");
        builder.AppendLine();
        builder.AppendLine("## 참조 중인 모델 원본");
        builder.AppendLine();

        foreach (var entry in _referencedModels)
        {
            builder.AppendLine("### `" + entry.ModelPath + "`");
            foreach (var consumer in entry.Consumers)
            {
                builder.AppendLine("- 사용처: `" + consumer + "`");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## 미참조 후보 모델");
        builder.AppendLine();
        if (!_includeUnreferencedCandidates)
        {
            builder.AppendLine("전체 씬·프리팹 검사에서만 미참조 후보를 계산합니다.");
        }
        else if (_unreferencedModels.Count == 0)
        {
            builder.AppendLine("미참조 후보가 없습니다.");
        }
        else
        {
            foreach (var modelPath in _unreferencedModels)
            {
                builder.AppendLine("- `" + modelPath + "`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 삭제 전 확인 사항");
        builder.AppendLine();
        builder.AppendLine("1. `Resources.Load`, Addressables, 런타임 문자열 로드 여부를 확인합니다.");
        builder.AppendLine("2. 삭제 대상 모델을 참조하는 애니메이션, 머티리얼, 다른 패키지 설정을 확인합니다.");
        builder.AppendLine("3. Git 커밋 또는 백업 후 하나씩 삭제하고 Unity Console의 Missing 참조를 확인합니다.");
        return builder.ToString();
    }

    private static bool IsProjectModelPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return ModelExtensions.Any(modelExtension =>
            string.Equals(modelExtension, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private sealed class ModelAuditEntry
    {
        public string ModelPath;
        public List<string> Consumers;
    }
}
