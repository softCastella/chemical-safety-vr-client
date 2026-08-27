using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds portable prefabs from the left/right controller Visuals hierarchies in the active scene
/// and exports them with their referenced Assets dependencies.
/// </summary>
internal static class ControllerModelPackageExporter
{
    const string OutputFolder = "Assets/Generated/ControllerModelPackage";
    const string PrefabFolder = OutputFolder + "/Prefabs";
    const string LeftPrefabPath = PrefabFolder + "/LeftControllerVisual.prefab";
    const string RightPrefabPath = PrefabFolder + "/RightControllerVisual.prefab";
    const string ReadmePath = OutputFolder + "/README_ControllerVisuals.txt";
    const string ManifestPath = OutputFolder + "/ControllerVisualDependencies.txt";
    const string ExporterPath = "Assets/Editor/ControllerModelPackageExporter.cs";
    const string PackageName = "PPE_Controller_Visuals_LeftRight.unitypackage";

    [MenuItem("Tools/XR/Export Current Controller Visuals Package to Desktop %#k")]
    public static void ExportToDesktop()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[XR Controller Export] Exit Play Mode before exporting controller visuals.");
            return;
        }

        if (!TryFindControllerVisuals(out var leftVisuals, out var rightVisuals))
            return;

        EnsureFolder(PrefabFolder);

        if (!BuildPrefab(leftVisuals, LeftPrefabPath, "LeftControllerVisual")
            || !BuildPrefab(rightVisuals, RightPrefabPath, "RightControllerVisual"))
        {
            Debug.LogError("[XR Controller Export] Failed to create one or both controller visual prefabs.");
            return;
        }

        WriteReadme();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var dependencies = CollectDependencies();
        WriteManifest(dependencies);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var exportAssets = new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase)
        {
            LeftPrefabPath,
            RightPrefabPath,
            ReadmePath,
            ManifestPath,
            ExporterPath,
        };

        exportAssets.RemoveWhere(path => !path.StartsWith("Assets/", StringComparison.Ordinal)
            || (!File.Exists(path) && !Directory.Exists(path)));

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");

        Directory.CreateDirectory(desktop);
        var outputPath = Path.Combine(desktop, PackageName);

        // Asset dependencies were collected recursively above. Export only that explicit Assets set so
        // immutable Package Manager sources (URP/XRI/Input System) are not copied into the unitypackage.
        AssetDatabase.ExportPackage(exportAssets.OrderBy(path => path).ToArray(), outputPath,
            ExportPackageOptions.Default);

        if (!File.Exists(outputPath))
        {
            Debug.LogError($"[XR Controller Export] Unity did not create the package at '{outputPath}'.");
            return;
        }

        var packageInfo = new FileInfo(outputPath);
        Debug.Log($"[XR Controller Export] Exported {exportAssets.Count} asset path(s), including dependencies, "
            + $"to '{outputPath}' ({packageInfo.Length / (1024f * 1024f):0.##} MB).\n"
            + "Use LeftControllerVisual.prefab and RightControllerVisual.prefab under the target project's tracked controller transforms.");
        EditorUtility.RevealInFinder(outputPath);
    }

    static bool TryFindControllerVisuals(out Transform leftVisuals, out Transform rightVisuals)
    {
        leftVisuals = FindControllerVisuals("LeftController", "Left Controller");
        rightVisuals = FindControllerVisuals("RightController", "Right Controller");

        if (leftVisuals != null && rightVisuals != null)
            return true;

        Debug.LogError("[XR Controller Export] Could not find both controller Visuals hierarchies. "
            + "Open the scene containing LeftController/RightController (or Left Controller/Right Controller) "
            + "with a direct child named Visuals, then run the exporter again.");
        return false;
    }

    static Transform FindControllerVisuals(params string[] controllerNames)
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!controllerNames.Contains(transform.name))
                    continue;

                var visuals = transform.Find("Visuals");
                if (visuals != null)
                    return visuals;
            }
        }

        return null;
    }

    static bool BuildPrefab(Transform source, string prefabPath, string prefabName)
    {
        var previewScene = EditorSceneManager.NewPreviewScene();
        try
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(clone, previewScene);
            clone.name = prefabName;
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            var prefab = PrefabUtility.SaveAsPrefabAsset(clone, prefabPath, out var success);
            return success && prefab != null;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    static string[] CollectDependencies()
    {
        return AssetDatabase.GetDependencies(new[] { LeftPrefabPath, RightPrefabPath }, true)
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path)
            .ToArray();
    }

    static void WriteReadme()
    {
        var contents =
            "PPE Controller Visuals - Left / Right\n"
            + "=====================================\n\n"
            + "Contents\n"
            + "- Prefabs/LeftControllerVisual.prefab\n"
            + "- Prefabs/RightControllerVisual.prefab\n"
            + "- ControllerVisualDependencies.txt\n\n"
            + "Usage\n"
            + "1. Import the unitypackage into the target Unity project.\n"
            + "2. Place LeftControllerVisual under the target rig's left tracked controller Transform.\n"
            + "3. Place RightControllerVisual under the target rig's right tracked controller Transform.\n"
            + "4. Keep each visual prefab's local position/rotation at zero and local scale at one.\n\n"
            + "Asset dependencies\n"
            + "All dependencies located under Assets, including referenced FBX models, materials, "
            + "animation assets, and project-owned scripts, are embedded in the unitypackage.\n\n"
            + "Package Manager prerequisites for full behaviour (install separately)\n"
            + "- Universal Render Pipeline 17.4.x\n"
            + "- Input System 1.19.x\n"
            + "- XR Interaction Toolkit 3.4.x\n"
            + "- XR Hands 1.7.x\n"
            + "Package Manager source files are intentionally not embedded in the unitypackage. "
            + "The visual meshes can still be reused without the tracking rig, but any imported component "
            + "whose package is absent will require that package to be installed.\n";

        File.WriteAllText(ReadmePath, contents);
    }

    static void WriteManifest(IEnumerable<string> dependencies)
    {
        var contents = "Asset dependencies included by the controller visual prefabs:\n\n"
            + string.Join("\n", dependencies);
        File.WriteAllText(ManifestPath, contents);
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
