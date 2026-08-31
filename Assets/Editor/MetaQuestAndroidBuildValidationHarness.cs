using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class MetaQuestAndroidBuildValidationHarness
{
    [MenuItem("Tools/XR/Validate Meta Quest Android Build")]
    public static void Validate()
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
            throw new InvalidOperationException("[Meta Quest Android Build] Android OpenXR Settings is missing.");

        MetaQuestFeature[] features = settings
            .GetFeatures<MetaQuestFeature>()
            .OfType<MetaQuestFeature>()
            .ToArray();
        if (features.Length != 1)
        {
            throw new InvalidOperationException(
                $"[Meta Quest Android Build] Expected one Meta Quest Support feature, found {features.Length}.");
        }

        if (!features[0].enabled)
        {
            throw new InvalidOperationException(
                "[Meta Quest Android Build] Meta Quest Support must be enabled for an immersive Quest manifest.");
        }

        Debug.Log(
            "[Meta Quest Android Build] PASS: Android Meta Quest Support is enabled. " +
            "A rebuilt APK manifest must still be checked for android.hardware.vr.headtracking and " +
            "com.oculus.intent.category.VR, followed by a standalone headset run.");
    }
}
