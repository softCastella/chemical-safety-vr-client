using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ControllerGuideDashedHighlightSetup
{
    private const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    private const string GuideRootName = "ControllerGuide";
    private const string ContextName = "Context";
    private const string LeftHighlightName = "Button Highlight Left";
    private const string RightHighlightName = "Button Highlight Right";

    private readonly struct HighlightLayout
    {
        public HighlightLayout(
            string parentName,
            Vector2 leftAnchor,
            Vector2 rightAnchor,
            Vector2 size)
        {
            ParentName = parentName;
            LeftAnchor = leftAnchor;
            RightAnchor = rightAnchor;
            Size = size;
        }

        public string ParentName { get; }
        public Vector2 LeftAnchor { get; }
        public Vector2 RightAnchor { get; }
        public Vector2 Size { get; }
    }

    private static readonly HighlightLayout[] Layouts =
    {
        new(
            "1_Ctrl_Trigger",
            new Vector2(0.335f, 0.530f),
            new Vector2(0.665f, 0.530f),
            new Vector2(10f, 10f)),
        new(
            "2_Ctrl_Grip",
            new Vector2(0.207f, 0.462f),
            new Vector2(0.793f, 0.462f),
            new Vector2(11f, 11f)),
        new(
            "3_Ctrl_Joystick",
            new Vector2(0.2f, 0.602f),
            new Vector2(0.8f, 0.602f),
            new Vector2(10f, 10f)),
    };

    [MenuItem("Tools/PPE/Controller Guide/Add Dashed Button Highlights")]
    public static void AddDashedButtonHighlights()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"Open '{ScenePath}' before adding controller button highlights.");
            return;
        }

        Transform[] guideRoots = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name == GuideRootName)
            .ToArray();
        if (guideRoots.Length != 1)
        {
            Debug.LogError($"Expected one '{GuideRootName}' in '{ScenePath}', found {guideRoots.Length}.");
            return;
        }

        Transform context = guideRoots[0].Find(ContextName);
        if (context == null)
        {
            Debug.LogError($"'{GuideRootName}/{ContextName}' is missing in '{ScenePath}'.");
            return;
        }

        int createdCount = 0;
        foreach (HighlightLayout layout in Layouts)
        {
            Transform parent = context.Find(layout.ParentName);
            if (parent == null)
            {
                Debug.LogError(
                    $"'{GuideRootName}/{ContextName}/{layout.ParentName}' is missing. No highlight was created.");
                continue;
            }

            createdCount += CreateHighlightIfMissing(
                parent,
                LeftHighlightName,
                layout.LeftAnchor,
                layout.Size);
            createdCount += CreateHighlightIfMissing(
                parent,
                RightHighlightName,
                layout.RightAnchor,
                layout.Size);
        }

        if (createdCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            createdCount > 0
                ? $"Created {createdCount} authored controller button highlight objects. Review and save the scene manually."
                : "Controller button highlight objects already exist; authored values were not overwritten.",
            guideRoots[0]);
    }

    public static void ApplyToSceneAndSave()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Wait for Unity compilation and asset import before applying highlights.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AddDashedButtonHighlights();
        if (!HasAllExpectedHighlights(scene))
            throw new InvalidOperationException("Controller button highlights were not created completely; the scene was not saved.");
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        Debug.Log($"Saved six authored controller button highlights in '{ScenePath}'.");
    }

    private static int CreateHighlightIfMissing(
        Transform parent,
        string childName,
        Vector2 anchor,
        Vector2 size)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            ControllerGuideDashedRing existingRing =
                existing.GetComponent<ControllerGuideDashedRing>();
            if (existingRing == null)
            {
                Debug.LogError(
                    $"Existing '{GetPath(existing)}' has no {nameof(ControllerGuideDashedRing)}; " +
                    "its authored object was not modified.",
                    existing);
                return 0;
            }

            if (existing.GetComponent<CanvasRenderer>() == null)
            {
                Undo.AddComponent<CanvasRenderer>(existing.gameObject);
                EditorUtility.SetDirty(existing.gameObject);
                return 1;
            }

            return 0;
        }

        GameObject highlight = new(
            childName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(ControllerGuideDashedRing));
        Undo.RegisterCreatedObjectUndo(highlight, "Create Controller Button Highlight");
        highlight.layer = parent.gameObject.layer;

        RectTransform rectTransform = highlight.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;

        ControllerGuideDashedRing ring = highlight.GetComponent<ControllerGuideDashedRing>();
        ring.color = new Color32(24, 166, 255, 230);
        ring.raycastTarget = false;
        EditorUtility.SetDirty(highlight);
        EditorUtility.SetDirty(rectTransform);
        EditorUtility.SetDirty(ring);
        return 1;
    }

    private static bool HasAllExpectedHighlights(Scene scene)
    {
        Transform guideRoot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .SingleOrDefault(transform => transform.name == GuideRootName);
        Transform context = guideRoot?.Find(ContextName);
        if (context == null)
            return false;

        foreach (HighlightLayout layout in Layouts)
        {
            Transform parent = context.Find(layout.ParentName);
            Transform left = parent?.Find(LeftHighlightName);
            Transform right = parent?.Find(RightHighlightName);
            if (left?.GetComponent<ControllerGuideDashedRing>() == null ||
                left.GetComponent<CanvasRenderer>() == null ||
                right?.GetComponent<ControllerGuideDashedRing>() == null ||
                right.GetComponent<CanvasRenderer>() == null)
            {
                return false;
            }
        }
        return true;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }
        return path;
    }
}
