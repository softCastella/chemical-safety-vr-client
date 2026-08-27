using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Puts a small world-space panel with Prev/Next into a lesson scene so a build is not a one-way trip.
///
/// A shared panel rather than an index scene: the panel reads the build settings at runtime, so lessons can
/// be added or reordered without anything here or in the already-placed panels needing an edit. An index
/// scene would have to be revised every time.
///
/// Running this repeatedly is safe; an existing panel is left alone rather than duplicated.
/// </summary>
static class LessonNavigatorBuilder
{
    const string k_RootName = "Lesson Navigator";

    /// <summary>Front-left of the start position, out of the way of whatever the lesson itself uses.</summary>
    static readonly Vector3 k_Position = new(-0.85f, 1.15f, 0.75f);
    static readonly Vector3 k_Rotation = new(0f, -35f, 0f);

    // Canvas units are pixels; the 0.001 scale turns them into millimetres. 700 x 220 is a 70 x 22 cm panel.
    const float k_CanvasScale = 0.001f;
    static readonly Vector2 k_CanvasSize = new(700f, 220f);
    static readonly Vector2 k_ButtonSize = new(200f, 90f);

    [MenuItem("Tools/XR/Build Lesson Navigator", priority = 27)]
    static void BuildHere()
    {
        var existing = Object.FindAnyObjectByType<LessonNavigator>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[Nav] This scene already has a navigator.", existing);
            return;
        }

        var navigator = Create();
        EditorSceneManager.MarkSceneDirty(navigator.gameObject.scene);
        Selection.activeGameObject = navigator.gameObject;
        Debug.Log($"[Nav] Added a lesson navigator to '{navigator.gameObject.scene.name}'.", navigator);
    }

    /// <summary>
    /// The same thing across every enabled scene in the build settings, opening and saving each in turn.
    /// Worth its own entry because the panel is only useful when it is in all of them - a lesson without
    /// one is a dead end in the build.
    /// </summary>
    [MenuItem("Tools/XR/Build Lesson Navigator In All Lessons", priority = 28)]
    static void BuildEverywhere()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scenes = EditorBuildSettings.scenes;
        var added = 0;
        var skipped = 0;

        foreach (var entry in scenes)
        {
            if (!entry.enabled)
                continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<LessonNavigator>(FindObjectsInactive.Include) != null)
            {
                skipped++;
                continue;
            }

            Create();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            added++;
        }

        Debug.Log($"[Nav] Lesson navigator added to {added} scene(s); {skipped} already had one.");
    }

    static LessonNavigator Create()
    {
        EnsureEventSystem();

        var root = new GameObject(k_RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Lesson Navigator");
        root.transform.SetPositionAndRotation(k_Position, Quaternion.Euler(k_Rotation));

        var canvas = CreateCanvas(root.transform);
        var label = CreateLabel(canvas.transform);

        var navigator = root.AddComponent<LessonNavigator>();

        var serialized = new SerializedObject(navigator);
        serialized.FindProperty("m_Label").objectReferenceValue = label;
        serialized.ApplyModifiedProperties();

        // ASCII only, and deliberately. The project ships no font asset with Korean glyphs - only
        // LiberationSans and Inter, both Latin - so anything else renders as tofu boxes and TMP warns once
        // per character. Change these once a font asset that covers the glyphs is in the project.
        CreateButton(canvas.transform, "Prev", "< Prev", new Vector2(-160f, -55f), navigator, nameof(LessonNavigator.Previous));
        CreateButton(canvas.transform, "Next", "Next >", new Vector2(160f, -55f), navigator, nameof(LessonNavigator.Next));

        return navigator;
    }

    static Canvas CreateCanvas(Transform parent)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(TrackedDeviceGraphicRaycaster));
        go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = k_CanvasSize;
        rect.localScale = Vector3.one * k_CanvasScale;
        rect.anchoredPosition3D = Vector3.zero;

        // A backing image, so the panel reads as a surface rather than as text floating in the air.
        var background = go.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        return canvas;
    }

    static TMP_Text CreateLabel(Transform parent)
    {
        var go = new GameObject("Label", typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = "Lesson";
        label.fontSize = 44f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(k_CanvasSize.x, 110f);
        rect.anchoredPosition = new Vector2(0f, 50f);

        return label;
    }

    static void CreateButton(Transform parent, string name, string caption, Vector2 position,
        LessonNavigator navigator, string method)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = k_ButtonSize;
        rect.anchoredPosition = position;

        var textGo = new GameObject("Text", typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = caption;
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var textRect = (RectTransform)textGo.transform;
        textRect.sizeDelta = k_ButtonSize;
        textRect.anchoredPosition = Vector2.zero;

        // A persistent listener rather than a runtime AddListener, so the wiring is visible in the
        // inspector and survives in the saved scene.
        var button = go.GetComponent<Button>();
        var call = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), navigator, method)
            as UnityEngine.Events.UnityAction;
        UnityEventTools.AddPersistentListener(button.onClick, call);
    }

    /// <summary>
    /// The half that cannot ride along on the panel - one per scene, and without it the buttons are just
    /// coloured rectangles. Mirrors what Build UI Lesson does, because the failure looks identical.
    /// </summary>
    static void EnsureEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(eventSystem.gameObject, "Build Lesson Navigator");
        }

        var go = eventSystem.gameObject;

        var standalone = go.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            Object.DestroyImmediate(standalone);

        if (go.GetComponent<XRUIInputModule>() == null)
            go.AddComponent<XRUIInputModule>();
    }
}
