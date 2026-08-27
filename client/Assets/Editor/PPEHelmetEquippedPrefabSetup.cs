using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHelmetEquippedPrefabSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string EquippedPrefabPath =
        "Assets/TripoModels/construction_helmet_3d_model_Clone1/" +
        "construction_helmet_3d_model_Clone1.fbx";

    [MenuItem("Tools/PPE/Repair Helmet Equipped Prefab Reference")]
    public static void RepairActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before repairing the Helmet prefab reference.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before repairing. Current scene: '{scene.path}'.");
        }

        PPEHelmetEquipController[] controllers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEHelmetEquipController>(true))
            .ToArray();
        if (controllers.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one Helmet equip controller, found {controllers.Length}.");
        }

        GameObject equippedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EquippedPrefabPath);
        if (equippedPrefab == null ||
            equippedPrefab.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            throw new InvalidOperationException(
                $"Helmet equipped prefab could not be loaded from '{EquippedPrefabPath}'.");
        }

        PPEHelmetEquipController controller = controllers[0];
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty prefabProperty =
            serializedController.FindProperty("equippedVisualPrefab");
        if (prefabProperty == null)
            throw new InvalidOperationException("Helmet equippedVisualPrefab property was not found.");

        Undo.RecordObject(controller, "Repair Helmet Equipped Prefab Reference");
        prefabProperty.objectReferenceValue = equippedPrefab;
        serializedController.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save scene '{ScenePath}'.");

        Debug.Log(
            $"Helmet equipped prefab reference repaired: {EquippedPrefabPath}",
            controller);
    }
}
