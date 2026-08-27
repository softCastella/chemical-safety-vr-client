using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PPEHandTestScenarioCardFeedbackSetup
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest.unity";
    private const string CanvasName = "XR UI Canvas";
    private const string HudPath = "Scenario Selection HUD/ScenarioCard3_Group";
    private const string OverlayName = "Interaction Feedback Overlay";
    private const float QuitPressedDarkenAlpha = 0.21568626f;

    [MenuItem("Tools/PPE/Configure HandTest Scenario Card Feedback %#&k")]
    public static void Configure()
    {
        Scene scene = RequireTargetScene();
        Transform cardGroup = FindCardGroup(scene);
        if (cardGroup == null)
            throw new InvalidOperationException($"Missing '{CanvasName}/{HudPath}' in '{ScenePath}'.");

        int configuredCount = 0;
        int changedCount = 0;
        foreach (ScenarioCardSelectProxy proxy in GetScenarioCards(cardGroup))
        {
            SerializedObject serializedProxy = new(proxy);
            SerializedProperty overlayProperty = serializedProxy.FindProperty("interactionOverlay");
            RoundedRectangleGraphic overlay = overlayProperty.objectReferenceValue as RoundedRectangleGraphic;
            bool requiresInitialConfiguration = overlay == null;
            if (overlay == null)
            {
                overlay = FindOrCreateOverlay(proxy.transform);
                overlayProperty.objectReferenceValue = overlay;
            }

            if (requiresInitialConfiguration)
            {
                SetBool(serializedProxy, "fadeOutBgmOnSelection", true);
                SetFloat(serializedProxy, "bgmFadeOutDuration", 1.25f);
                SetColor(serializedProxy, "normalColor", new Color(0.18f, 0.92f, 1f, 0f));
                SetColor(serializedProxy, "hoverColor", new Color(0.18f, 0.92f, 1f, 0.2f));
                SetColor(serializedProxy, "pressedColor", new Color(0f, 0f, 0f, QuitPressedDarkenAlpha));
                SetColor(serializedProxy, "selectedColor", new Color(0f, 0f, 0f, QuitPressedDarkenAlpha));
                SetFloat(serializedProxy, "visualTransitionDuration", 0.1f);
                SetFloat(serializedProxy, "selectedFeedbackDuration", 0.28f);
            }

            serializedProxy.ApplyModifiedPropertiesWithoutUndo();
            if (requiresInitialConfiguration)
            {
                EditorUtility.SetDirty(proxy);
                changedCount++;
            }
            configuredCount++;
        }

        if (changedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save '{ScenePath}'.");
        }

        ValidateScene(scene);
        Debug.Log($"Checked {configuredCount} HandTest scenario card(s); initialized {changedCount}. Existing scene-authored feedback values were preserved.");
    }

    [MenuItem("Tools/PPE/Apply Quit-Style Dark Card Selection %#&j")]
    public static void ApplyQuitStyleDarkSelection()
    {
        Scene scene = RequireTargetScene();
        Transform cardGroup = FindCardGroup(scene);
        if (cardGroup == null)
            throw new InvalidOperationException($"Missing '{CanvasName}/{HudPath}' in '{ScenePath}'.");

        int changedCount = 0;
        Color quitStyleDarken = new(0f, 0f, 0f, QuitPressedDarkenAlpha);
        foreach (ScenarioCardSelectProxy proxy in GetScenarioCards(cardGroup))
        {
            SerializedObject serializedProxy = new(proxy);
            SerializedProperty pressed = serializedProxy.FindProperty("pressedColor");
            SerializedProperty selected = serializedProxy.FindProperty("selectedColor");
            if (pressed.colorValue == quitStyleDarken && selected.colorValue == quitStyleDarken)
                continue;

            Undo.RecordObject(proxy, "Match scenario card selection to Quit Button");
            pressed.colorValue = quitStyleDarken;
            selected.colorValue = quitStyleDarken;
            serializedProxy.ApplyModifiedProperties();
            EditorUtility.SetDirty(proxy);
            changedCount++;
        }

        if (changedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Failed to save '{ScenePath}'.");
        }

        ValidateScene(scene);
        Debug.Log($"Applied Quit Button-equivalent dark press/selection feedback to {changedCount} HandTest scenario card(s). Existing hover feedback was preserved.");
    }

    [MenuItem("Tools/PPE/Validate HandTest Scenario Card Feedback")]
    public static void Validate()
    {
        Scene scene = RequireTargetScene();
        ValidateScene(scene);
    }

    private static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        if (!Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new OperationCanceledException("HandTest scenario card feedback setup was cancelled.");
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static Transform FindCardGroup(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != CanvasName)
                continue;

            return root.transform.Find(HudPath);
        }

        return null;
    }

    private static List<ScenarioCardSelectProxy> GetScenarioCards(Transform cardGroup)
    {
        List<ScenarioCardSelectProxy> cards = new();
        for (int index = 0; index < cardGroup.childCount; index++)
        {
            Transform child = cardGroup.GetChild(index);
            if (child.TryGetComponent(out ScenarioCardSelectProxy proxy))
                cards.Add(proxy);
        }

        return cards;
    }

    private static RoundedRectangleGraphic FindOrCreateOverlay(Transform card)
    {
        Transform existing = card.Find(OverlayName);
        if (existing != null && existing.TryGetComponent(out RoundedRectangleGraphic existingGraphic))
            return existingGraphic;

        GameObject overlayObject = new(OverlayName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RoundedRectangleGraphic));
        Undo.RegisterCreatedObjectUndo(overlayObject, "Create scenario card feedback overlay");
        overlayObject.layer = card.gameObject.layer;

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.SetParent(card, false);
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition3D = new Vector3(0f, 0f, -0.022f);
        overlayRect.sizeDelta = new Vector2(0.72f, 0.98f);
        overlayRect.localRotation = Quaternion.identity;
        overlayRect.localScale = Vector3.one;
        overlayRect.SetAsLastSibling();

        CanvasRenderer canvasRenderer = overlayObject.GetComponent<CanvasRenderer>();
        canvasRenderer.cullTransparentMesh = false;

        RoundedRectangleGraphic overlay = overlayObject.GetComponent<RoundedRectangleGraphic>();
        overlay.color = Color.white;
        overlay.raycastTarget = false;
        overlay.CornerRadius = 0.045f;
        EditorUtility.SetDirty(overlayObject);
        return overlay;
    }

    private static void ValidateScene(Scene scene)
    {
        Transform cardGroup = FindCardGroup(scene);
        List<string> failures = new();
        if (cardGroup == null)
        {
            failures.Add($"Missing '{CanvasName}/{HudPath}'.");
        }
        else
        {
            List<ScenarioCardSelectProxy> cards = GetScenarioCards(cardGroup);
            if (cards.Count != 3)
                failures.Add($"Expected 3 scenario cards, found {cards.Count}.");

            foreach (ScenarioCardSelectProxy proxy in cards)
                ValidateCard(proxy, failures);
        }

        if (failures.Count > 0)
        {
            string message = "HandTest scenario card feedback validation failed:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log("HandTest scenario card feedback validation passed: 3 top-layer overlays, visible hover, Quit Button-style dark selection, selection delay, and BGM fade-out are serialized.");
    }

    private static void ValidateCard(ScenarioCardSelectProxy proxy, List<string> failures)
    {
        SerializedObject serializedProxy = new(proxy);
        Graphic overlay = serializedProxy.FindProperty("interactionOverlay").objectReferenceValue as Graphic;
        string path = GetPath(proxy.transform);
        if (overlay == null)
        {
            failures.Add($"'{path}' has no interaction overlay.");
            return;
        }

        if (overlay.transform.parent != proxy.transform
            || overlay.transform.GetSiblingIndex() != proxy.transform.childCount - 1)
        {
            failures.Add($"'{path}' feedback overlay is not its topmost child layer.");
        }
        if (overlay.raycastTarget)
            failures.Add($"'{path}' feedback overlay blocks pointer raycasts.");
        if (!serializedProxy.FindProperty("fadeOutBgmOnSelection").boolValue
            || serializedProxy.FindProperty("bgmFadeOutDuration").floatValue <= 0f)
        {
            failures.Add($"'{path}' does not fade out BGM on selection.");
        }
        if (serializedProxy.FindProperty("selectedFeedbackDuration").floatValue <= 0f)
            failures.Add($"'{path}' has no visible selected-feedback interval.");

        float normalAlpha = serializedProxy.FindProperty("normalColor").colorValue.a;
        float hoverAlpha = serializedProxy.FindProperty("hoverColor").colorValue.a;
        Color pressedColor = serializedProxy.FindProperty("pressedColor").colorValue;
        Color selectedColor = serializedProxy.FindProperty("selectedColor").colorValue;
        if (hoverAlpha <= normalAlpha)
            failures.Add($"'{path}' hover color is not visible.");
        if (!IsQuitStyleDarken(pressedColor) || !IsQuitStyleDarken(selectedColor))
            failures.Add($"'{path}' press/selection feedback does not match the Quit Button darkening amount.");
    }

    private static bool IsQuitStyleDarken(Color color)
    {
        return color.r <= 0.001f && color.g <= 0.001f && color.b <= 0.001f
            && Mathf.Abs(color.a - QuitPressedDarkenAlpha) <= 0.001f;
    }

    private static void SetBool(SerializedObject target, string name, bool value)
    {
        target.FindProperty(name).boolValue = value;
    }

    private static void SetFloat(SerializedObject target, string name, float value)
    {
        target.FindProperty(name).floatValue = value;
    }

    private static void SetColor(SerializedObject target, string name, Color value)
    {
        target.FindProperty(name).colorValue = value;
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
