using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class SciFiCardGeometryRepair
{
    const string CanvasNamePrefix = "XR UI Canvas";

    [MenuItem("Tools/UI/Repair Missing Sci-Fi Card Geometry")]
    static void RepairMissingGeometry()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before repairing sci-fi card geometry.");
            return;
        }

        var cards = FindTargetCards();
        var rebuiltCards = 0;
        var rebuiltMeshes = 0;

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair Missing Sci-Fi Card Geometry");

        foreach (var card in cards)
        {
            var filters = GetFilters(card);
            var missingFilters = new List<MeshFilter>();
            foreach (var filter in filters)
            {
                if (filter != null && filter.sharedMesh == null)
                    missingFilters.Add(filter);
            }

            if (missingFilters.Count == 0)
                continue;

            foreach (var filter in missingFilters)
                Undo.RecordObject(filter, "Repair Missing Sci-Fi Card Geometry");

            var count = card.RebuildMissingMeshes();
            if (count == 0)
                continue;

            rebuiltCards++;
            rebuiltMeshes += count;

            foreach (var filter in missingFilters)
            {
                if (filter.sharedMesh == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(filter.sharedMesh, "Repair Missing Sci-Fi Card Geometry");
                EditorUtility.SetDirty(filter.sharedMesh);
                EditorUtility.SetDirty(filter);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (rebuiltMeshes == 0)
        {
            Debug.Log($"No missing sci-fi card meshes were found in {DescribeScope()}.");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Rebuilt {rebuiltMeshes} missing mesh(es) across {rebuiltCards} sci-fi card(s) "
            + $"in {DescribeScope()}. Authored transforms and materials were preserved. Save the scene to keep the repair.");
    }

    [MenuItem("Tools/UI/Validate Sci-Fi Card Geometry")]
    static void ValidateGeometry()
    {
        var cards = FindTargetCards();
        var failures = new List<string>();

        foreach (var card in cards)
        {
            foreach (var filter in GetFilters(card))
            {
                if (filter != null && filter.sharedMesh == null)
                    failures.Add($"{GetHierarchyPath(card.transform)} -> {filter.name}");
            }
        }

        if (failures.Count == 0)
        {
            Debug.Log($"Sci-fi card geometry validation passed for {cards.Count} card(s) in {DescribeScope()}.");
            return;
        }

        Debug.LogError($"Sci-fi card geometry validation failed with {failures.Count} missing mesh(es):\n- "
            + string.Join("\n- ", failures));
    }

    static List<SciFiCardVisual> FindTargetCards()
    {
        var activeScene = SceneManager.GetActiveScene();
        var selected = Selection.activeGameObject;
        if (selected != null && selected.scene == activeScene)
        {
            var selectedCards = new List<SciFiCardVisual>(selected.GetComponentsInChildren<SciFiCardVisual>(true));
            if (selectedCards.Count > 0)
                return selectedCards;
        }

        var cards = new List<SciFiCardVisual>();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            if (!root.name.StartsWith(CanvasNamePrefix))
                continue;

            cards.AddRange(root.GetComponentsInChildren<SciFiCardVisual>(true));
        }

        return cards;
    }

    static List<MeshFilter> GetFilters(SciFiCardVisual card)
    {
        var filters = new List<MeshFilter>(13);
        AddUnique(filters, card.shadowFilter);
        AddUnique(filters, card.backplateFilter);
        AddUnique(filters, card.glassFilter);
        AddUnique(filters, card.edgeFilter);
        AddUnique(filters, card.frameFilter);

        if (card.cornerMarkerFilters != null)
        {
            foreach (var filter in card.cornerMarkerFilters)
                AddUnique(filters, filter);
        }

        return filters;
    }

    static void AddUnique(List<MeshFilter> filters, MeshFilter filter)
    {
        if (filter != null && !filters.Contains(filter))
            filters.Add(filter);
    }

    static string DescribeScope()
    {
        var selected = Selection.activeGameObject;
        return selected != null ? $"'{GetHierarchyPath(selected.transform)}'" : "the active XR UI canvases";
    }

    static string GetHierarchyPath(Transform target)
    {
        var path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
