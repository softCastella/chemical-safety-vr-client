using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports the gaze test scene and its supporting scripts as a .unitypackage for course distribution.
/// </summary>
static class GazePackageExporter
{
    const string k_OutputDirectory = "Export";
    const string k_PackageName = "GazeInteraction.unitypackage";

    /// <summary>
    /// Assets that belong to this package. The Starter Assets rig the scene references is
    /// deliberately excluded - see <see cref="LogPrerequisites"/>.
    /// </summary>
    static readonly string[] k_Assets =
    {
        "Assets/Scenes/GazeTestScene.unity",
        "Assets/Scenes/GazeTest",
        "Assets/Scripts/GazeReticle.cs",
        "Assets/Scripts/GazeDebugLogger.cs",
        "Assets/Scripts/GazeTestTarget.cs",
        "Assets/Editor/GazeTestSceneBuilder.cs",
        "Assets/Editor/GazePackageExporter.cs",
    };

    [MenuItem("Tools/Gaze/Export Gaze Package")]
    static void Export()
    {
        var present = new List<string>();
        var missing = new List<string>();

        foreach (var asset in k_Assets)
        {
            if (File.Exists(asset) || Directory.Exists(asset))
                present.Add(asset);
            else
                missing.Add(asset);
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning($"[GAZE] Skipping {missing.Count} missing asset(s):\n  " + string.Join("\n  ", missing));
        }

        if (present.Count == 0)
        {
            Debug.LogError("[GAZE] Nothing to export. Run Tools > Gaze > Build Gaze Test Scene first.");
            return;
        }

        Directory.CreateDirectory(k_OutputDirectory);
        var outputPath = Path.Combine(k_OutputDirectory, k_PackageName);

        // Recurse pulls in folder contents; dependencies are intentionally left out so the package
        // does not swallow (and redistribute) the entire Starter Assets sample.
        AssetDatabase.ExportPackage(present.ToArray(), outputPath, ExportPackageOptions.Recurse);

        var info = new FileInfo(outputPath);
        Debug.Log($"[GAZE] Exported {present.Count} item(s) to {outputPath} ({info.Length / 1024f:0.#} KB).");
        LogPrerequisites();

        EditorUtility.RevealInFinder(outputPath);
    }

    static void LogPrerequisites()
    {
        Debug.Log("[GAZE] Import prerequisites for the target project:\n" +
            "  1. com.unity.xr.interaction.toolkit 3.4.1\n" +
            "  2. Its 'Starter Assets' sample imported via Package Manager " +
            "(the test scene references the XR Origin (XR Rig) prefab from it)\n" +
            "  3. TextMeshPro essentials, for the target labels");
    }
}
