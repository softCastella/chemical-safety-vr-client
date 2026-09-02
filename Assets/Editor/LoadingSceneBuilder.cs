using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadingSceneBuilder
{
    const string ScenePath = "Assets/Scenes/3_Loading.unity";
    const string ProgressRootName = "LoadingProgressRoot";
    const string PercentageName = "LoadingPercentText";
    const string BarName = "LoadingBar";
    const string DefaultTargetSceneName = "4_PPE_Room";
    const int SegmentCount = 18;

    static readonly Color GradientStart = new(0.08f, 0.56f, 1f, 1f);
    static readonly Color GradientMiddle = new(0.08f, 0.88f, 0.88f, 1f);
    static readonly Color GradientEnd = new(0.68f, 0.94f, 0.28f, 1f);

    [MenuItem("Tools/Loading Scene/Build Progress UI")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Loading scene progress UI build was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = FindSingle<Canvas>(scene);
        if (canvas == null)
            throw new InvalidOperationException($"'{ScenePath}' must contain exactly one Canvas.");

        LoadingSceneController controller = GetOrAdd<LoadingSceneController>(canvas.gameObject, out bool controllerWasAdded);
        CanvasGroup loadingContentGroup = GetOrAdd<CanvasGroup>(canvas.gameObject, out bool contentGroupWasAdded);
        if (contentGroupWasAdded)
        {
            Undo.RecordObject(loadingContentGroup, "Author loading content prewarm state");
            loadingContentGroup.alpha = 0f;
            loadingContentGroup.interactable = false;
            loadingContentGroup.blocksRaycasts = false;
            loadingContentGroup.ignoreParentGroups = false;
            EditorUtility.SetDirty(loadingContentGroup);
        }
        TitleSplashController splash = canvas.GetComponent<TitleSplashController>();
        if (splash != null)
            Undo.DestroyObjectImmediate(splash);

        GameObject logo = FindSceneObject(scene, "TitleLogo2d");
        if (logo == null)
            throw new InvalidOperationException("3_Loading is missing TitleLogo2d.");
        CanvasGroup logoGroup = logo.GetComponent<CanvasGroup>();
        if (logoGroup == null)
            throw new InvalidOperationException("TitleLogo2d is missing its CanvasGroup.");
        Undo.RecordObject(logoGroup, "Show loading scene logo");
        logoGroup.alpha = 1f;
        EditorUtility.SetDirty(logoGroup);

        RectTransform progressRoot = FindDirectChild(canvas.transform, ProgressRootName) as RectTransform;
        bool progressRootWasCreated = progressRoot == null;
        if (progressRootWasCreated)
        {
            progressRoot = CreateUIObject(ProgressRootName, canvas.transform).GetComponent<RectTransform>();
            SetRect(progressRoot, new Vector2(0.5f, 0.5f), new Vector2(60f, 14f), new Vector2(0f, -40f));
        }

        RectTransform percentageRect = FindDirectChild(progressRoot, PercentageName) as RectTransform;
        bool percentageWasCreated = percentageRect == null;
        if (percentageWasCreated)
        {
            percentageRect = CreateUIObject(PercentageName, progressRoot).GetComponent<RectTransform>();
            SetRect(percentageRect, new Vector2(0.5f, 0.5f), new Vector2(24f, 5.5f), new Vector2(0f, 4f));
        }

        TextMeshProUGUI percentageText = GetOrAdd<TextMeshProUGUI>(percentageRect.gameObject, out bool textWasAdded);
        if (percentageWasCreated || textWasAdded)
            ApplyPercentageDefaults(percentageText);

        RectTransform barRect = FindDirectChild(progressRoot, BarName) as RectTransform;
        bool barWasCreated = barRect == null;
        if (barWasCreated)
        {
            barRect = CreateUIObject(BarName, progressRoot).GetComponent<RectTransform>();
            SetRect(barRect, new Vector2(0.5f, 0.5f), new Vector2(54f, 4f), new Vector2(0f, -2.5f));
        }

        SegmentedGradientProgressGraphic progressBar = GetOrAdd<SegmentedGradientProgressGraphic>(
            barRect.gameObject,
            out bool progressBarWasAdded);
        if (barWasCreated || progressBarWasAdded)
        {
            Undo.RecordObject(progressBar, "Author segmented loading bar appearance");
            progressBar.color = Color.white;
            progressBar.raycastTarget = false;
            progressBar.segments = SegmentCount;
            progressBar.segmentSpacing = 0.6f;
            progressBar.gradient = CreateLogoGradient();
            progressBar.progress = 0f;
            EditorUtility.SetDirty(progressBar);
        }

        SerializedObject serializedController = new(controller);
        SerializedProperty targetSceneProperty = serializedController.FindProperty("nextSceneName");
        if (controllerWasAdded
            || string.IsNullOrWhiteSpace(targetSceneProperty.stringValue)
            || targetSceneProperty.stringValue == "2_Intro")
        {
            targetSceneProperty.stringValue = DefaultTargetSceneName;
        }
        serializedController.FindProperty("loadingContentGroup").objectReferenceValue = loadingContentGroup;
        serializedController.FindProperty("percentageText").objectReferenceValue = percentageText;
        serializedController.FindProperty("progressBar").objectReferenceValue = progressBar;
        serializedController.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidateScene(scene);
        Debug.Log("3_Loading progress UI built: real async percentage and an authored 18-segment gradient bar are connected.");
    }

    [MenuItem("Tools/Loading Scene/Validate Progress UI")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Loading scene progress UI validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);
    }

    static void ValidateScene(Scene scene)
    {
        List<string> failures = new();
        Canvas canvas = FindSingle<Canvas>(scene);
        LoadingSceneController controller = FindSingle<LoadingSceneController>(scene);
        TitleSplashController splash = FindSingle<TitleSplashController>(scene);
        Camera sceneCamera = FindSingle<Camera>(scene);

        if (canvas == null)
            failures.Add("Expected exactly one Canvas.");
        if (controller == null || !controller.enabled)
            failures.Add("LoadingSceneController is missing or disabled.");
        if (splash != null)
            failures.Add("TitleSplashController must not remain in 3_Loading because its Awake hides the logo.");
        if (sceneCamera == null
            || sceneCamera.clearFlags != CameraClearFlags.SolidColor
            || sceneCamera.backgroundColor != Color.black)
        {
            failures.Add("Loading camera must clear to opaque black to prevent an XR compositor color flash.");
        }

        GameObject logo = FindSceneObject(scene, "TitleLogo2d");
        CanvasGroup logoGroup = logo != null ? logo.GetComponent<CanvasGroup>() : null;
        if (logoGroup == null || logoGroup.alpha < 0.999f)
            failures.Add("TitleLogo2d must be authored visible.");

        if (controller != null)
        {
            SerializedObject serializedController = new(controller);
            string targetSceneName = serializedController.FindProperty("nextSceneName")?.stringValue;
            CanvasGroup loadingContentGroup = serializedController
                .FindProperty("loadingContentGroup")
                ?.objectReferenceValue as CanvasGroup;
            TMP_Text percentageText = serializedController.FindProperty("percentageText")?.objectReferenceValue as TMP_Text;
            SegmentedGradientProgressGraphic progressBar = serializedController
                .FindProperty("progressBar")
                ?.objectReferenceValue as SegmentedGradientProgressGraphic;

            if (loadingContentGroup == null || loadingContentGroup.gameObject != canvas.gameObject)
                failures.Add("LoadingContentGroup must reference the Canvas CanvasGroup.");
            else if (loadingContentGroup.alpha > 0.001f
                || loadingContentGroup.interactable
                || loadingContentGroup.blocksRaycasts)
            {
                failures.Add("LoadingContentGroup must be authored hidden and non-interactive during XR prewarm.");
            }

            if (percentageText == null || percentageText.text != "0%")
                failures.Add("LoadingPercentText is missing or does not author '0%'.");
            if (progressBar == null)
                failures.Add("Segmented loading bar reference is missing.");
            else if (progressBar.segments != SegmentCount)
                failures.Add($"Expected {SegmentCount} loading bar segments.");
            else if (progressBar.gradient.Evaluate(0f) == progressBar.gradient.Evaluate(1f))
                failures.Add("Loading bar does not contain an authored color gradient.");
            else if (SegmentedGradientProgressGraphic.CalculateVisibleSegmentCount(0.99f, SegmentCount) != SegmentCount - 1
                || SegmentedGradientProgressGraphic.CalculateVisibleSegmentCount(1f, SegmentCount) != SegmentCount)
            {
                failures.Add("The final loading segment must appear only when the percentage reaches 100%.");
            }

            bool targetIsInBuild = EditorBuildSettings.scenes.Any(buildScene =>
                buildScene.enabled
                && string.Equals(Path.GetFileNameWithoutExtension(buildScene.path), targetSceneName, StringComparison.Ordinal));
            if (!targetIsInBuild)
                failures.Add($"Target scene '{targetSceneName}' is not enabled in Build Settings.");
            else if (targetSceneName != DefaultTargetSceneName)
                failures.Add($"Loading scene target must be '{DefaultTargetSceneName}', not '{targetSceneName}'.");
        }

        if (failures.Count > 0)
        {
            string message = "3_Loading progress UI validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log("3_Loading progress UI validation passed: real loader, 0% text, 18 gradient segments, and build target are valid.");
    }

    static void ApplyPercentageDefaults(TextMeshProUGUI text)
    {
        Undo.RecordObject(text, "Author loading percentage appearance");
        text.text = "0%";
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 4.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            GradientStart,
            GradientEnd,
            GradientStart,
            GradientEnd);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        EditorUtility.SetDirty(text);
    }

    static Gradient CreateLogoGradient()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(GradientStart, 0f),
                new GradientColorKey(GradientMiddle, 0.5f),
                new GradientColorKey(GradientEnd, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
        return gradient;
    }

    static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
        Undo.SetTransformParent(gameObject.transform, parent, $"Parent {objectName}");
        gameObject.transform.localScale = Vector3.one;
        return gameObject;
    }

    static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    static T GetOrAdd<T>(GameObject gameObject, out bool wasAdded) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        wasAdded = component == null;
        return wasAdded ? Undo.AddComponent<T>(gameObject) : component;
    }

    static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    static T FindSingle<T>(Scene scene) where T : Component
    {
        List<T> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            matches.AddRange(root.GetComponentsInChildren<T>(true));
        return matches.Count == 1 ? matches[0] : null;
    }
}
