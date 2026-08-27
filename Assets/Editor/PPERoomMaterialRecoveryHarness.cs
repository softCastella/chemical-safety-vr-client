using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPERoomMaterialRecoveryHarness
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    private const string RoomRootName = "PPE Background Room";
    private const string GeneratedRootName = "Generated Image Room";

    [MenuItem("Tools/PPE/Validate Room Surface Materials")]
    public static void Validate()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE room material validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> failures = new();
        ValidateBenchNameRouting(failures);

        GameObject roomObject = GameObject.Find(RoomRootName);
        if (roomObject == null)
        {
            failures.Add($"Missing '{RoomRootName}'.");
        }
        else
        {
            if (!roomObject.TryGetComponent(out PPEBackgroundRoom _))
                failures.Add($"'{RoomRootName}' has no PPEBackgroundRoom component.");

            Transform generatedRoot = roomObject.transform.Find(GeneratedRootName);
            if (generatedRoot == null)
            {
                failures.Add($"Missing '{RoomRootName}/{GeneratedRootName}'.");
            }
            else
            {
                ValidateMaterialPair(generatedRoot, "Rear Wall", "Rear Wall (1)", failures);
                ValidateMaterialPair(generatedRoot, "Floor", "Floor (1)", failures);
                ValidateMaterialPair(generatedRoot, "Ceiling", "Ceiling (1)", failures);
            }
        }

        if (failures.Count > 0)
        {
            string message = "PPE room surface material validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"PPE room surface material validation passed for '{scene.path}': " +
            "Rear Wall (1), Floor (1), and Ceiling (1) recovered valid materials.");
    }

    [MenuItem("Tools/PPE/Validate Bench Brightness Name Routing")]
    public static void ValidateBenchBrightnessNameRouting()
    {
        List<string> failures = new();
        ValidateBenchNameRouting(failures);
        if (failures.Count > 0)
        {
            string message = "PPE bench brightness name validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "PPE bench brightness name validation passed: current, suffixed, and legacy " +
            "bench names are routed to ApplyBenchColor.");
    }

    private static void ValidateBenchNameRouting(List<string> failures)
    {
        MethodInfo matcher = typeof(PPEBackgroundRoom).GetMethod(
            "MatchesBenchName",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (matcher == null)
        {
            failures.Add("PPEBackgroundRoom.MatchesBenchName was not found.");
            return;
        }

        ValidateBenchName(matcher, "PPE_B_Bench", true, failures);
        ValidateBenchName(matcher, "PPE_B_Bench_01", true, failures);
        ValidateBenchName(matcher, "ppe_room_bench_2", true, failures);
        ValidateBenchName(matcher, "PPE_B_Benchmark", false, failures);
    }

    private static void ValidateBenchName(
        MethodInfo matcher,
        string candidateName,
        bool expected,
        List<string> failures)
    {
        bool actual = (bool)matcher.Invoke(null, new object[] { candidateName });
        if (actual != expected)
        {
            failures.Add(
                $"Bench name routing mismatch for '{candidateName}': " +
                $"expected {expected}, actual {actual}.");
        }
    }

    private static void ValidateMaterialPair(
        Transform root,
        string sourceName,
        string continuationName,
        List<string> failures)
    {
        MeshRenderer sourceRenderer = GetValidRenderer(root, sourceName, failures);
        MeshRenderer continuationRenderer = GetValidRenderer(root, continuationName, failures);
        if (sourceRenderer == null || continuationRenderer == null)
            return;

        if (sourceRenderer.sharedMaterial != continuationRenderer.sharedMaterial)
        {
            failures.Add(
                $"'{continuationName}' does not share the recovered material used by " +
                $"'{sourceName}'.");
        }
    }

    private static MeshRenderer GetValidRenderer(
        Transform root,
        string objectName,
        List<string> failures)
    {
        Transform surface = root.Find(objectName);
        if (surface == null)
        {
            failures.Add($"Missing '{GeneratedRootName}/{objectName}'.");
            return null;
        }

        if (!surface.TryGetComponent(out MeshRenderer renderer))
        {
            failures.Add($"'{objectName}' has no MeshRenderer.");
            return null;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            failures.Add($"'{objectName}' has no recovered material.");
            return null;
        }

        if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
        {
            failures.Add($"'{objectName}' uses a missing or error shader.");
            return null;
        }

        return renderer;
    }
}
