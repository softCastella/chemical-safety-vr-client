using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class XRSessionForwardAlignmentValidationHarness
{
    private static readonly string[] RequiredScenePaths =
    {
        "Assets/Scenes/1_Title.unity",
        "Assets/Scenes/2_Intro.unity",
        "Assets/Scenes/3_PPE_Room_3mode_loco.unity"
    };

    [MenuItem("Tools/XR/Validate Session Forward Alignment")]
    public static void Validate()
    {
        var failures = new List<string>();

        foreach (string path in RequiredScenePaths)
            ValidateScene(path, failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "[XR Session Forward Validation] Failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log(
            "[XR Session Forward Validation] PASS: Title, Intro, and PPE scenes share one authored +Z " +
            "session-forward alignment contract. A standalone Quest launch is still required.");
    }

    private static void ValidateScene(string path, ICollection<string> failures)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
        if (openedForValidation)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            XROrigin[] activeOrigins = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<XROrigin>(true))
                .Where(origin => origin.gameObject.activeInHierarchy)
                .ToArray();
            if (activeOrigins.Length != 1)
            {
                failures.Add($"{path}: expected one active XROrigin, found {activeOrigins.Length}.");
                return;
            }

            XROrigin origin = activeOrigins[0];
            XRSessionForwardAlignment[] aligners = origin.GetComponents<XRSessionForwardAlignment>();
            if (aligners.Length != 1)
            {
                failures.Add($"{path}: active XROrigin must have exactly one XRSessionForwardAlignment.");
                return;
            }

            var serialized = new SerializedObject(aligners[0]);
            Vector3 authoredForward = serialized.FindProperty("m_AuthoredForward").vector3Value;
            int warmupFrames = serialized.FindProperty("m_TrackingWarmupFrames").intValue;
            float waitSeconds = serialized.FindProperty("m_TrackingWaitSeconds").floatValue;

            if (Vector3.Angle(authoredForward, Vector3.forward) > 0.01f)
                failures.Add($"{path}: authored forward must remain +Z.");
            if (warmupFrames < 1)
                failures.Add($"{path}: tracking warmup must be at least one frame.");
            if (waitSeconds <= 0f)
                failures.Add($"{path}: tracking wait must be greater than zero.");
            if (Quaternion.Angle(origin.transform.localRotation, Quaternion.identity) > 0.01f)
                failures.Add($"{path}: authored XROrigin rotation must remain identity.");
        }
        finally
        {
            if (openedForValidation)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
