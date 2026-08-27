using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Replaces the movable grab sample named Poke_Box with the fixed poke-test prefab while preserving
/// the exact Transform authored in the open scene.
/// </summary>
public static class PokeBoxTestSetup
{
    const string k_TargetName = "Poke_Box";
    const string k_PrefabPath = "Assets/Prefabs/Poke_Box.prefab";

    [MenuItem("Tools/XR/Setup Poke Box Test", priority = 26)]
    public static void Setup()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[HandPose] Exit Play Mode before setting up Poke_Box.");
            return;
        }

        var current = FindTarget();
        if (current == null)
        {
            Debug.LogError($"[HandPose] No active-scene object named '{k_TargetName}' was found.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[HandPose] Missing fixed poke prefab at {k_PrefabPath}.");
            return;
        }

        var source = PrefabUtility.GetCorrespondingObjectFromSource(current.gameObject);
        if (source == prefab)
        {
            Selection.activeGameObject = current.gameObject;
            Debug.Log("[HandPose] Poke_Box is already using the fixed poke-test prefab.");
            return;
        }

        var scene = current.gameObject.scene;
        var parent = current.parent;
        var siblingIndex = current.GetSiblingIndex();
        var localPosition = current.localPosition;
        var localRotation = current.localRotation;
        var localScale = current.localScale;
        var wasActive = current.gameObject.activeSelf;

        var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(replacement, "Setup Poke Box Test");
        if (parent != null)
            Undo.SetTransformParent(replacement.transform, parent, "Setup Poke Box Test");

        replacement.name = k_TargetName;
        replacement.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        replacement.transform.localScale = localScale;
        replacement.transform.SetSiblingIndex(siblingIndex);
        replacement.SetActive(wasActive);

        Undo.DestroyObjectImmediate(current.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = replacement;

        Debug.Log(
            $"[HandPose] Poke_Box fixed at authored local position {localPosition}. " +
            "Index-tip surface contact now drives the matching hand's Poke parameter.");
    }

    static Transform FindTarget()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (transform.gameObject.scene == activeScene && transform.name == k_TargetName)
                return transform;
        }

        return null;
    }
}
