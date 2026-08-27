using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHandBoneReferenceRepair
{
    const string ReferenceScenePath = "Assets/Scenes/3_PPE_Room_Loco.unity";
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room.unity";

    [MenuItem("Tools/PPE/Repair Hand Skinned Mesh Bone References")]
    public static void RepairFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Repair();
        ValidateTargetScene();
    }

    public static void RepairBatch()
    {
        Repair();
        ValidateTargetScene();
    }

    [MenuItem("Tools/PPE/Validate Hand Skinned Mesh Bone References")]
    public static void ValidateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ValidateTargetScene();
    }

    public static void ValidateBatch()
    {
        ValidateTargetScene();
    }

    static void Repair()
    {
        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Scene referenceScene =
            EditorSceneManager.OpenScene(ReferenceScenePath, OpenSceneMode.Additive);

        Dictionary<string, Transform> targetTransforms = IndexTransforms(targetScene);
        Dictionary<string, SkinnedMeshRenderer> targetRenderers =
            IndexSkinnedMeshRenderers(targetScene);
        Dictionary<string, SkinnedMeshRenderer> referenceRenderers =
            IndexSkinnedMeshRenderers(referenceScene);

        int repairedRendererCount = 0;
        int repairedBoneCount = 0;
        foreach ((string path, SkinnedMeshRenderer targetRenderer) in targetRenderers)
        {
            if (!HasMissingBones(targetRenderer))
                continue;

            if (!referenceRenderers.TryGetValue(path, out SkinnedMeshRenderer referenceRenderer))
            {
                throw new InvalidOperationException(
                    $"Reference renderer was not found for '{path}'.");
            }

            Transform[] referenceBones = referenceRenderer.bones;
            var mappedBones = new Transform[referenceBones.Length];
            for (int index = 0; index < referenceBones.Length; index++)
            {
                Transform referenceBone = referenceBones[index];
                if (referenceBone == null)
                {
                    throw new InvalidOperationException(
                        $"Reference renderer '{path}' has a missing bone at index {index}.");
                }

                string bonePath = GetPath(referenceBone);
                if (!targetTransforms.TryGetValue(bonePath, out Transform targetBone))
                {
                    throw new InvalidOperationException(
                        $"Target bone '{bonePath}' was not found while repairing '{path}'.");
                }

                mappedBones[index] = targetBone;
            }

            Undo.RecordObject(targetRenderer, "Repair PPE Hand Bone References");
            targetRenderer.bones = mappedBones;
            EditorUtility.SetDirty(targetRenderer);
            repairedRendererCount++;
            repairedBoneCount += mappedBones.Length;
        }

        if (repairedRendererCount == 0)
        {
            Debug.Log("PPE hand bone repair found no missing references; no changes were needed.");
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene))
                throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");

            Debug.Log(
                $"PPE hand bone repair complete. Scene='{TargetScenePath}', " +
                $"renderers={repairedRendererCount}, bones={repairedBoneCount}, " +
                $"reference='{ReferenceScenePath}'.");
        }

        EditorSceneManager.CloseScene(referenceScene, true);
    }

    static void ValidateTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        int rendererCount = 0;
        int boneCount = 0;
        var failures = new List<string>();
        foreach (SkinnedMeshRenderer renderer in GetSkinnedMeshRenderers(scene))
        {
            rendererCount++;
            Transform[] bones = renderer.bones;
            boneCount += bones.Length;
            if (renderer.rootBone == null)
                failures.Add($"{GetPath(renderer.transform)} has no root bone.");

            for (int index = 0; index < bones.Length; index++)
            {
                if (bones[index] == null)
                    failures.Add($"{GetPath(renderer.transform)} has a missing bone at index {index}.");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"PPE hand bone validation failed ({failures.Count} issue(s)):\n" +
                string.Join("\n", failures));
        }

        Debug.Log(
            $"PPE hand bone validation passed. Scene='{TargetScenePath}', " +
            $"skinnedRenderers={rendererCount}, bones={boneCount}.");
    }

    static Dictionary<string, Transform> IndexTransforms(Scene scene)
    {
        var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!string.Equals(root.name, "XR Origin (VR)", StringComparison.Ordinal))
                continue;

            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                string path = GetPath(transform);
                if (!result.TryAdd(path, transform))
                    throw new InvalidOperationException(
                        $"Duplicate transform path '{path}' in scene '{scene.path}'.");
            }
        }

        return result;
    }

    static Dictionary<string, SkinnedMeshRenderer> IndexSkinnedMeshRenderers(Scene scene)
    {
        var result =
            new Dictionary<string, SkinnedMeshRenderer>(StringComparer.Ordinal);
        foreach (SkinnedMeshRenderer renderer in GetSkinnedMeshRenderers(scene))
        {
            string path = GetPath(renderer.transform);
            if (!result.TryAdd(path, renderer))
                throw new InvalidOperationException(
                    $"Duplicate skinned renderer path '{path}' in scene '{scene.path}'.");
        }

        return result;
    }

    static IEnumerable<SkinnedMeshRenderer> GetSkinnedMeshRenderers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SkinnedMeshRenderer renderer in
                     root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                yield return renderer;
            }
        }
    }

    static bool HasMissingBones(SkinnedMeshRenderer renderer)
    {
        Transform[] bones = renderer.bones;
        if (bones.Length == 0)
            return true;

        foreach (Transform bone in bones)
        {
            if (bone == null)
                return true;
        }

        return false;
    }

    static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
