using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Removes MonoBehaviours whose script reference is gone, from the selection and everything under it.
///
/// The reason this needs more than one call to GameObjectUtility is where the broken component lives. On a
/// prefab instance the components come from the asset, and Unity will not let an instance delete one - the
/// removal has to happen in the prefab itself, or it silently does nothing and the warning comes straight
/// back on the next reload. So the scene walk sorts what it finds into plain scene objects, cleaned in
/// place, and prefab assets, opened and cleaned at the source.
/// </summary>
static class MissingScriptCleaner
{
    [MenuItem("Tools/XR/Remove Missing Scripts In Selection", priority = 60)]
    static void Clean()
    {
        var roots = Selection.gameObjects;
        if (roots.Length == 0)
        {
            Debug.LogWarning("[Cleanup] Nothing selected. Pick the parent object to clean under.");
            return;
        }

        var broken = Broken(roots).ToList();
        if (broken.Count == 0)
        {
            Debug.Log("[Cleanup] No missing scripts found under the selection.");
            return;
        }

        // Pass one: the prefabs. Sorting by object rather than by component would be wrong here - a single
        // GameObject can carry both a broken component that came from its prefab and another added on the
        // instance, and the two have to be deleted in different places.
        var prefabPaths = new HashSet<string>(broken.Select(OwningPrefabPath).Where(path => !string.IsNullOrEmpty(path)));

        // Editing a prefab asset reaches every other scene that uses it, so it is worth a look before it
        // happens. The scene pass below just runs - that one is undoable.
        if (prefabPaths.Count > 0 && !EditorUtility.DisplayDialog(
                "Remove missing scripts",
                $"{prefabPaths.Count} prefab asset(s) may own broken components and will be modified on disk:\n\n" +
                string.Join("\n", prefabPaths.OrderBy(path => path)) +
                "\n\nThis affects every scene using them and cannot be undone.",
                "Clean them", "Scene objects only"))
        {
            prefabPaths.Clear();
        }

        var fromPrefabs = prefabPaths.Sum(CleanPrefabAsset);

        // Pass two: whatever the prefabs did not account for. Anything still broken after the assets were
        // saved was added on the instance - or was never prefab-owned at all - and Unity does allow those
        // to be deleted in place. Persistent objects are skipped; those only change through pass one.
        var fromScene = 0;
        var stubborn = new List<string>();

        foreach (var go in Broken(roots))
        {
            if (!EditorUtility.IsPersistent(go))
                fromScene += CleanSceneObject(go);

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                stubborn.Add(Path(go));
        }

        Debug.Log($"[Cleanup] Removed {fromPrefabs + fromScene} missing script(s): " +
            $"{fromPrefabs} from {prefabPaths.Count} prefab asset(s), {fromScene} from scene objects.");

        if (stubborn.Count > 0)
        {
            Debug.LogWarning($"[Cleanup] {stubborn.Count} object(s) still carry a missing script and could not " +
                "be cleaned from here - open the owning prefab and remove it there:\n" + string.Join("\n", stubborn));
        }
    }

    /// <summary>Every GameObject under the roots that has at least one missing script, inactive ones included.</summary>
    static IEnumerable<GameObject> Broken(IEnumerable<GameObject> roots) =>
        roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Distinct()
            .Where(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0);

    static string Path(GameObject go)
    {
        var path = go.name;
        for (var parent = go.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;

        return path;
    }

    [MenuItem("Tools/XR/Remove Missing Scripts In Selection", validate = true)]
    static bool CanClean() => Selection.gameObjects.Length > 0;

    /// <summary>
    /// The prefab asset that owns <paramref name="go"/>, or null when it is a plain scene object that owns
    /// itself. Handles the three cases the menu can be pointed at: an instance in the scene, a prefab
    /// picked in the Project window, and an object inside a prefab that came from a nested one.
    /// </summary>
    static string OwningPrefabPath(GameObject go)
    {
        // Non-null for anything instanced from a prefab, and it resolves to the innermost source - which
        // for a nested prefab is the inner asset, exactly where the component has to be deleted.
        var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
        if (source != null)
            return AssetDatabase.GetAssetPath(source);

        // No source but persistent means the object is part of a prefab asset itself, which happens when
        // the menu is used on a selection made in the Project window rather than the hierarchy.
        return EditorUtility.IsPersistent(go) ? AssetDatabase.GetAssetPath(go) : null;
    }

    static int CleanSceneObject(GameObject go)
    {
        // RemoveMonoBehavioursWithMissingScript does not register anything itself, so the undo entry has to
        // be opened first - and it has to be a complete object undo, because there is no live component
        // left for the property-level recording to describe.
        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
        var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        if (removed > 0)
            EditorUtility.SetDirty(go);

        return removed;
    }

    static int CleanPrefabAsset(string path)
    {
        var contents = PrefabUtility.LoadPrefabContents(path);
        var removed = 0;

        try
        {
            foreach (var transform in contents.GetComponentsInChildren<Transform>(true))
            {
                // Objects belonging to a nested prefab are skipped for the same reason instances were:
                // this asset does not own them. Their own path is already in the set, collected from the
                // scene walk, so they get cleaned when that one is opened.
                if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject))
                    continue;

                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }

            if (removed > 0)
                PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        return removed;
    }
}
