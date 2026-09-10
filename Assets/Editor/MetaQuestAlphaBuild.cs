using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MetaQuestAlphaBuild
{
    private const string BuildDirectory = "Builds/MetaHorizonAlpha";

    [MenuItem("Tools/XR/Build Meta Quest Alpha Release")]
    public static void BuildRelease()
    {
        IDisposable signingScope = MetaQuestSigningConfiguration.ApplyForBuild();
        try
        {
            BuildReleaseWithConfiguredSigning();
        }
        finally
        {
            signingScope.Dispose();
        }
    }

    private static void BuildReleaseWithConfiguredSigning()
    {
        SceneDependencyValidationHarness.Validate();
        MetaQuestAndroidBuildValidationHarness.Validate();
        ValidateSigningConfiguration();

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("[Meta Quest Alpha Build] Project root could not be resolved.");
        string outputDirectory = Path.Combine(projectRoot, BuildDirectory);
        Directory.CreateDirectory(outputDirectory);

        string version = Application.version.Replace('.', '_');
        int versionCode = PlayerSettings.Android.bundleVersionCode;
        string outputPath = Path.Combine(
            outputDirectory,
            $"ChemicalSafetyVR_Alpha_{version}_{versionCode}.apk");

        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            {
                throw new InvalidOperationException(
                    $"[Meta Quest Alpha Build] Build failed: {report.summary.result}, " +
                    $"errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}.");
            }

            Debug.Log(
                $"[Meta Quest Alpha Build] PASS: versionCode={versionCode}, " +
                $"size={report.summary.totalSize} bytes, output='{outputPath}'.");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
        }
    }

    public static void BuildBatch()
    {
        try
        {
            BuildRelease();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateSigningConfiguration()
    {
        if (!PlayerSettings.Android.useCustomKeystore)
            throw new InvalidOperationException("[Meta Quest Alpha Build] Custom Android keystore is not enabled.");
        if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName))
            throw new InvalidOperationException("[Meta Quest Alpha Build] Android keystore path is missing.");
        if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
            throw new InvalidOperationException("[Meta Quest Alpha Build] Android key alias is missing.");
        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
            string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
        {
            throw new InvalidOperationException(
                "[Meta Quest Alpha Build] Keystore passwords are not available in this Unity session. " +
                "Enter them in Android Player Settings and run the build again without closing Unity.");
        }
    }
}
