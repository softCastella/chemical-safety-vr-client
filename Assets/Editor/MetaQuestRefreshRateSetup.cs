using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public static class MetaQuestRefreshRateSetup
{
    private const float RequiredRefreshRate = 72f;

    [MenuItem("Tools/XR/Configure Meta Quest 72 Hz")]
    public static void Configure()
    {
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);

        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
            throw new InvalidOperationException("Android OpenXR Settings asset was not found.");

        MetaQuestDisplayRefreshRateFeature feature = settings.GetFeature<MetaQuestDisplayRefreshRateFeature>();
        if (feature == null)
            throw new InvalidOperationException("Meta Quest 72 Hz OpenXR feature was not registered for Android.");

        Undo.RecordObject(feature, "Configure Meta Quest 72 Hz");
        feature.enabled = true;

        var serializedFeature = new SerializedObject(feature);
        SerializedProperty targetRate = serializedFeature.FindProperty("m_TargetRefreshRate");
        if (targetRate == null)
            throw new MissingFieldException(typeof(MetaQuestDisplayRefreshRateFeature).Name, "m_TargetRefreshRate");

        targetRate.floatValue = RequiredRefreshRate;
        serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();

        MetaQuestRefreshRateValidationHarness.Validate();
    }
}

public static class MetaQuestRefreshRateValidationHarness
{
    private const float RequiredRefreshRate = 72f;
    private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

    [MenuItem("Tools/XR/Validate Meta Quest 72 Hz")]
    public static void Validate()
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
            throw new InvalidOperationException("[Meta Quest 72 Hz Validation] Android OpenXR Settings is missing.");

        MetaQuestDisplayRefreshRateFeature[] features = settings
            .GetFeatures<MetaQuestDisplayRefreshRateFeature>()
            .OfType<MetaQuestDisplayRefreshRateFeature>()
            .ToArray();
        if (features.Length != 1)
        {
            throw new InvalidOperationException(
                $"[Meta Quest 72 Hz Validation] Expected one feature instance, found {features.Length}.");
        }

        MetaQuestDisplayRefreshRateFeature feature = features[0];
        if (!feature.enabled)
            throw new InvalidOperationException("[Meta Quest 72 Hz Validation] Android feature is disabled.");

        if (!Mathf.Approximately(feature.TargetRefreshRate, RequiredRefreshRate))
        {
            throw new InvalidOperationException(
                $"[Meta Quest 72 Hz Validation] Target is {feature.TargetRefreshRate:0.##} Hz, expected 72 Hz.");
        }

        if (!File.Exists(QualitySettingsPath))
            throw new FileNotFoundException("QualitySettings.asset is missing.", QualitySettingsPath);

        string qualitySettings = File.ReadAllText(QualitySettingsPath);
        int qualityLevelCount = CountOccurrences(qualitySettings, "vSyncCount:");
        int disabledVSyncCount = CountOccurrences(qualitySettings, "vSyncCount: 0");
        if (qualityLevelCount == 0 || qualityLevelCount != disabledVSyncCount)
        {
            throw new InvalidOperationException(
                "[Meta Quest 72 Hz Validation] Every quality level must keep vSyncCount at 0 for XR runtime pacing.");
        }

        Debug.Log(
            "[Meta Quest 72 Hz Validation] PASS: Android OpenXR feature enabled, target=72 Hz, " +
            $"and vSyncCount=0 in all {qualityLevelCount} quality levels. " +
            "A Quest build/runtime check is still required to confirm the headset accepted 72 Hz.");
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int startIndex = 0;
        while ((startIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
    }
}
