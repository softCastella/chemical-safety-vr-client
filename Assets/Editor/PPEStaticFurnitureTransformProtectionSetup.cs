using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEStaticFurnitureTransformProtectionSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";

    static readonly string[] ProtectedObjectNames =
    {
        "safety_cabinet",
        "mask_locker",
        "ppe_room_bench_2",
        "hazmat_suit_hanger",
        "metal_locker",
    };

    readonly struct TransformSnapshot
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformSnapshot(Transform transform)
        {
            Parent = transform.parent;
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }

        public bool Matches(Transform transform)
        {
            return transform.parent == Parent &&
                transform.localPosition == LocalPosition &&
                transform.localRotation == LocalRotation &&
                transform.localScale == LocalScale;
        }
    }

    [MenuItem("Tools/PPE/Protect Static Furniture Transforms")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before protecting authored furniture transforms.");

        Scene scene = RequireActiveScene();
        Transform[] targets = ProtectedObjectNames
            .Select(name => RequireUniqueTransform(scene, name))
            .ToArray();
        TransformSnapshot[] snapshots = targets
            .Select(target => new TransformSnapshot(target))
            .ToArray();

        Undo.SetCurrentGroupName("Protect static furniture transforms");
        int undoGroup = Undo.GetCurrentGroup();

        for (int index = 0; index < targets.Length; index++)
        {
            Transform target = targets[index];
            if (target.GetComponent<Rigidbody>() != null || target.GetComponent<Animator>() != null)
            {
                throw new InvalidOperationException(
                    $"'{target.name}' is not static furniture and cannot receive a Transform lock.");
            }

            if (target.GetComponent<AuthoredTransformRuntimeLock>() == null)
                Undo.AddComponent<AuthoredTransformRuntimeLock>(target.gameObject);

            if (!snapshots[index].Matches(target))
            {
                throw new InvalidOperationException(
                    $"'{target.name}' changed while adding Transform protection. The scene was not saved.");
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateScene(scene, true);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        Selection.objects = targets.Select(target => (UnityEngine.Object)target.gameObject).ToArray();
        Debug.Log(
            "Static furniture Transform protection configured without changing any authored pose: " +
            string.Join(", ", ProtectedObjectNames));
    }

    [MenuItem("Tools/PPE/Validate Static Furniture Transform Protection")]
    public static void Validate()
    {
        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateScene(Scene scene, bool logSuccess)
    {
        foreach (string objectName in ProtectedObjectNames)
        {
            Transform target = RequireUniqueTransform(scene, objectName);
            AuthoredTransformRuntimeLock runtimeLock =
                target.GetComponent<AuthoredTransformRuntimeLock>();
            if (runtimeLock == null ||
                !runtimeLock.PreserveParent ||
                !runtimeLock.PreserveLocalPosition ||
                !runtimeLock.PreserveLocalRotation ||
                !runtimeLock.PreserveLocalScale)
            {
                throw new InvalidOperationException(
                    $"'{objectName}' does not have complete authored Transform protection.");
            }
        }

        if (logSuccess)
        {
            Debug.Log(
                "Static furniture Transform protection validation passed: " +
                string.Join(", ", ProtectedObjectNames));
        }
    }

    static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before configuring furniture protection. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static Transform RequireUniqueTransform(Scene scene, string objectName)
    {
        Transform[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name == objectName)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected exactly one '{objectName}', found {matches.Length}.");

        return matches[0];
    }
}
