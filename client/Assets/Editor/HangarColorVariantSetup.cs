using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HangarColorVariantSetup
{
    const string TargetScenePath = "Assets/Scenes/5_MixerRoom_Unlit.unity";
    const string TargetObjectName = "Hangar_v2_6 Variant";

    [MenuItem("Tools/Mixer Room/Setup Hangar Color Variants")]
    public static void SetupFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before setting up Hangar colors.");
            return;
        }

        Setup(scene);
    }

    [MenuItem("Tools/Mixer Room/Validate Hangar Color Variants")]
    public static void ValidateFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before validating Hangar colors.");
            return;
        }

        Validate(scene);
    }

    static void Setup(Scene scene)
    {
        GameObject hangar = FindRoot(scene);
        HangarColorVariantController controller =
            hangar.GetComponent<HangarColorVariantController>();

        if (controller == null)
            controller = Undo.AddComponent<HangarColorVariantController>(hangar);

        Undo.RecordObject(controller, "Configure Hangar Color Variants");
        controller.CollectChildRenderers();
        controller.ApplyNow();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

        Validate(scene);
        Debug.Log(
            "Hangar color variants are ready on 'Hangar_v2_6 Variant'. " +
            "Use Original, White, Blue, or Custom in its Inspector.");
    }

    static void Validate(Scene scene)
    {
        GameObject hangar = FindRoot(scene);
        HangarColorVariantController controller =
            hangar.GetComponent<HangarColorVariantController>();

        if (controller == null)
            throw new MissingComponentException(
                $"'{TargetObjectName}' has no HangarColorVariantController.");
        Renderer[] renderers = hangar.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(
                $"'{TargetObjectName}' has no renderers.");
        if (controller.TargetRendererCount != renderers.Length)
            throw new InvalidOperationException(
                $"The controller targets {controller.TargetRendererCount} renderers, " +
                $"but the Hangar hierarchy contains {renderers.Length}.");

        Debug.Log(
            $"Hangar color variant validation passed: {controller.TargetRendererCount} renderers, " +
            $"preset {controller.Variant}.");
    }

    static GameObject FindRoot(Scene scene)
    {
        GameObject hangar = Array.Find(
            scene.GetRootGameObjects(),
            candidate => candidate.name == TargetObjectName);
        if (hangar == null)
            throw new MissingReferenceException(
                $"Root object '{TargetObjectName}' was not found in '{TargetScenePath}'.");
        return hangar;
    }
}
