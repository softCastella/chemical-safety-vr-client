using System;
using System.Collections.Generic;
using System.IO;
using Prototype.Tyche.UI.Keyboard;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Creates a reusable modal Hangul keyboard Canvas from the authoritative scene-authored UI.
/// The source scene is never modified, and an existing output prefab is never overwritten.
/// </summary>
public static class ModalHangulKeyboardPackageBuilder
{
    const string AuthoritativeScenePath = "Assets/Scenes/3_PPE_Room_3mode_loco.unity";
    const string SourceRootName = "Modal  Keyboard Canvas";
    const string OutputRoot = "Assets/Prefabs/UI/Hangul Keyboard";
    const string ModalPrefabPath = OutputRoot + "/Modal Hangul Keyboard Canvas.prefab";
    const string KeyboardPrefabPath = OutputRoot + "/Hangul Spatial Keyboard.prefab";
    const string KoreanFontPath = "Assets/Font/Pretendard-Medium SDF.asset";
    const string SpatialKeyboardSampleRoot = "Assets/Samples/XR Interaction Toolkit/3.4.1/Spatial Keyboard";
    const string StarterAssetsRoot = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets";

    static readonly HashSet<string> s_SceneSpecificAdapterTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "PPEVoiceKeyboardEventRelay",
        "PPEControllerEducationEntry",
    };

    static readonly string[] s_RequiredBundledAssets =
    {
        SpatialKeyboardSampleRoot + "/Unity.XR.Interaction.Toolkit.Samples.SpatialKeyboard.asmdef",
        SpatialKeyboardSampleRoot + "/Scripts/XRKeyboard.cs",
        SpatialKeyboardSampleRoot + "/Scripts/XRKeyboardLayout.cs",
        SpatialKeyboardSampleRoot + "/Scripts/XRKeyboardKey.cs",
        StarterAssetsRoot + "/StarterAssets.asmdef",
        StarterAssetsRoot + "/Scripts/RotationAxisLockGrabTransformer.cs",
        StarterAssetsRoot + "/Scripts/XRPokeFollowAffordance.cs",
        "Assets/Scripts/HangulKeyboard/Prototype.Tyche.HangulKeyboard.asmdef",
        "Assets/Scripts/HangulKeyboard/HangulShiftLegend.cs",
        "Assets/Scripts/AuthoredWorldCanvasPose.cs",
        KoreanFontPath,
    };

    [MenuItem("Tools/XR/Hangul Keyboard/Modal Canvas/Build Reusable Modal Canvas Prefab")]
    public static void BuildReusableModalCanvasPrefab()
    {
        HangulSpatialKeyboardBuilder.BuildProjectPrefab();

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ModalPrefabPath);
        if (existing != null)
        {
            ValidateModalCanvasPrefab(false);
            Selection.activeObject = existing;
            Debug.Log(
                $"[Modal Hangul Keyboard] Existing authored prefab preserved: {ModalPrefabPath}",
                existing);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AuthoritativeScenePath) == null)
            throw new InvalidOperationException(
                $"The authoritative source scene was not found at '{AuthoritativeScenePath}'. " +
                "Import the already-generated modal prefab instead of attempting a rebuild in another project.");

        EnsureFolder(OutputRoot);

        var activeScene = SceneManager.GetActiveScene();
        var useActiveScene = activeScene.IsValid()
            && activeScene.isLoaded
            && string.Equals(activeScene.path, AuthoritativeScenePath, StringComparison.Ordinal);
        var sourceScene = useActiveScene
            ? activeScene
            : EditorSceneManager.OpenPreviewScene(AuthoritativeScenePath);

        try
        {
            var sourceRoot = FindRoot(sourceScene, SourceRootName);
            if (sourceRoot == null)
                throw new InvalidOperationException(
                    $"The scene-authored root '{SourceRootName}' was not found in '{AuthoritativeScenePath}'.");

            var clone = UnityEngine.Object.Instantiate(sourceRoot);
            try
            {
                clone.name = "Modal Hangul Keyboard Canvas";
                StripSceneSpecificAdapters(clone);
                ApplyCurrentShiftPresentation(clone);
                RequireNoExternalSceneReferences(clone);

                var saved = PrefabUtility.SaveAsPrefabAsset(clone, ModalPrefabPath, out var success);
                if (!success || saved == null)
                    throw new InvalidOperationException($"Failed to save the modal prefab at '{ModalPrefabPath}'.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }
        finally
        {
            if (!useActiveScene && sourceScene.IsValid())
                EditorSceneManager.ClosePreviewScene(sourceScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateModalCanvasPrefab(false);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModalPrefabPath);
        Selection.activeObject = prefab;
        Debug.Log(
            $"[Modal Hangul Keyboard] Reusable modal Canvas prefab created without modifying the source scene: {ModalPrefabPath}",
            prefab);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Modal Canvas/Validate Reusable Modal Canvas Prefab")]
    public static void ValidateReusableModalCanvasPrefab()
    {
        HangulSpatialKeyboardBuilder.ValidateProjectPrefab();
        ValidateModalCanvasPrefab(true);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Modal Canvas/Create Modal Canvas In Current Scene")]
    public static void CreateModalCanvasInCurrentScene()
    {
        BuildReusableModalCanvasPrefab();

        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<HangulKeyboardController>(true) == null)
                continue;

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                continue;

            Selection.activeGameObject = root;
            Debug.Log("[Modal Hangul Keyboard] The active scene already contains a modal Hangul keyboard Canvas.", root);
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModalPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"The reusable modal prefab was not found at '{ModalPrefabPath}'.");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Create Modal Hangul Keyboard Canvas");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = instance;
        Debug.Log(
            "[Modal Hangul Keyboard] Modal Canvas instance created with its prefab-authored Transform and inactive state. " +
            "Wire project-specific submit and guide actions in the Inspector.",
            instance);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Modal Canvas/Export Self-Contained Package To Desktop")]
    public static void ExportSelfContainedPackageToDesktop()
    {
        BuildReusableModalCanvasPrefab();
        ValidateModalCanvasPrefab(false);
        ValidateBundledDependencies();

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopPath) || !Directory.Exists(desktopPath))
            throw new InvalidOperationException("The current Windows Desktop folder could not be resolved.");

        var outputPath = Path.Combine(
            desktopPath,
            $"Tyche_Modal_Hangul_Keyboard_Canvas_{DateTime.Now:yyyyMMdd_HHmmss}.unitypackage");
        var exportRoots = new[]
        {
            "Assets/Scripts/HangulKeyboard",
            "Assets/Scripts/AuthoredWorldCanvasPose.cs",
            "Assets/Editor/HangulComposerValidationHarness.cs",
            "Assets/Editor/HangulSpatialKeyboardBuilder.cs",
            "Assets/Editor/ModalHangulKeyboardPackageBuilder.cs",
            OutputRoot,
            KoreanFontPath,
            SpatialKeyboardSampleRoot,
            StarterAssetsRoot + "/StarterAssets.asmdef",
            StarterAssetsRoot + "/Scripts/RotationAxisLockGrabTransformer.cs",
            StarterAssetsRoot + "/Scripts/XRPokeFollowAffordance.cs",
        };

        AssetDatabase.ExportPackage(
            exportRoots,
            outputPath,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

        if (!File.Exists(outputPath))
            throw new InvalidOperationException($"Unity did not create the package at '{outputPath}'.");

        var sizeMegabytes = new FileInfo(outputPath).Length / (1024d * 1024d);
        Debug.Log(
            $"[Modal Hangul Keyboard] Exported self-contained package with bundled Spatial Keyboard sources: " +
            $"{outputPath} ({sizeMegabytes:0.##} MB)");
    }

    static void ValidateModalCanvasPrefab(bool logSuccess)
    {
        ValidateBundledDependencies();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModalPrefabPath);
        Require(prefab != null, $"The modal prefab was not found at '{ModalPrefabPath}'.");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(KeyboardPrefabPath) != null,
            $"The project keyboard prefab was not found at '{KeyboardPrefabPath}'.");

        var root = PrefabUtility.LoadPrefabContents(ModalPrefabPath);
        try
        {
            Require(!root.activeSelf, "The reusable modal root must preserve the authored inactive initial state.");

            var canvas = root.GetComponent<Canvas>();
            Require(canvas != null && canvas.renderMode == RenderMode.WorldSpace,
                "The reusable modal root must have a World Space Canvas.");
            Require(root.GetComponent<CanvasScaler>() != null,
                "The reusable modal root has no CanvasScaler.");
            Require(root.GetComponent<GraphicRaycaster>() != null,
                "The reusable modal root has no GraphicRaycaster.");
            Require(root.GetComponent<TrackedDeviceGraphicRaycaster>() != null,
                "The reusable modal root has no TrackedDeviceGraphicRaycaster.");

            var blocker = FindChild(root.transform, "Keyboard Ray Blocker");
            var blockerImage = blocker != null ? blocker.GetComponent<Image>() : null;
            Require(blockerImage != null && blockerImage.raycastTarget,
                "The modal Canvas has no raycast-target Keyboard Ray Blocker.");
            Require(FindChild(root.transform, "Overlay") != null,
                "The modal Canvas has no authored Overlay.");

            Require(root.GetComponentsInChildren<XRKeyboard>(true).Length == 1,
                "The modal Canvas must contain exactly one XRKeyboard.");
            Require(root.GetComponentsInChildren<XRKeyboardLayout>(true).Length == 1,
                "The modal Canvas must contain exactly one XRKeyboardLayout.");
            Require(root.GetComponentsInChildren<HangulKeyboardController>(true).Length == 1,
                "The modal Canvas must contain exactly one HangulKeyboardController.");
            Require(root.GetComponentsInChildren<TMP_InputField>(true).Length >= 1,
                "The modal Canvas has no TMP input field.");

            var legends = root.GetComponentsInChildren<HangulShiftLegend>(true);
            Require(legends.Length == 7,
                $"Expected 7 Hangul Shift label controllers, but found {legends.Length}.");
            foreach (var legend in legends)
            {
                var serializedLegend = new SerializedObject(legend);
                var shiftTextProperty = serializedLegend.FindProperty("m_ShiftLegendText");
                var shiftText = shiftTextProperty != null
                    ? shiftTextProperty.objectReferenceValue as TMP_Text
                    : null;
                Require(shiftText != null && !shiftText.gameObject.activeSelf,
                    $"'{legend.name}' is not serialized for primary-label Shift replacement.");
            }

            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                Require(component != null, "The reusable modal prefab contains a missing script.");
                Require(!s_SceneSpecificAdapterTypes.Contains(component.GetType().Name),
                    $"The reusable modal prefab still contains scene-specific adapter '{component.GetType().Name}'.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (logSuccess)
            Debug.Log(
                "[Modal Hangul Keyboard] Prefab, Shift presentation, XR raycasters, and bundled dependency validation passed.",
                prefab);
    }

    static void ValidateBundledDependencies()
    {
        foreach (var path in s_RequiredBundledAssets)
            Require(AssetDatabase.LoadMainAssetAtPath(path) != null, $"Required bundled dependency is missing: {path}");
    }

    static void StripSceneSpecificAdapters(GameObject root)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null || !s_SceneSpecificAdapterTypes.Contains(component.GetType().Name))
                continue;

            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    static void ApplyCurrentShiftPresentation(GameObject root)
    {
        foreach (var legend in root.GetComponentsInChildren<HangulShiftLegend>(true))
        {
            var serializedLegend = new SerializedObject(legend);
            var shiftTextProperty = serializedLegend.FindProperty("m_ShiftLegendText");
            var shiftText = shiftTextProperty != null
                ? shiftTextProperty.objectReferenceValue as TMP_Text
                : null;
            if (shiftText != null)
                shiftText.gameObject.SetActive(false);
        }
    }

    static void RequireNoExternalSceneReferences(GameObject root)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            Require(component != null, "The source modal Canvas contains a missing script.");
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference
                    || property.objectReferenceValue == null
                    || EditorUtility.IsPersistent(property.objectReferenceValue))
                    continue;

                var referencedObject = property.objectReferenceValue as GameObject;
                if (referencedObject == null && property.objectReferenceValue is Component referencedComponent)
                    referencedObject = referencedComponent.gameObject;

                if (referencedObject != null && referencedObject.transform.IsChildOf(root.transform))
                    continue;

                throw new InvalidOperationException(
                    $"External scene reference remains at '{GetPath(component.transform, root.transform)}' " +
                    $"({component.GetType().Name}.{property.propertyPath} -> {property.objectReferenceValue.name}).");
            }
        }
    }

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (string.Equals(root.name, name, StringComparison.Ordinal))
                return root;
        }

        return null;
    }

    static Transform FindChild(Transform root, string name)
    {
        if (string.Equals(root.name, name, StringComparison.Ordinal))
            return root;

        for (var index = 0; index < root.childCount; ++index)
        {
            var match = FindChild(root.GetChild(index), name);
            if (match != null)
                return match;
        }

        return null;
    }

    static string GetPath(Transform current, Transform root)
    {
        var path = current.name;
        while (current != root && current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var normalized = folderPath.Replace('\\', '/');
        var slash = normalized.LastIndexOf('/');
        if (slash <= 0)
            throw new InvalidOperationException($"Cannot create Unity asset folder '{folderPath}'.");

        var parent = normalized.Substring(0, slash);
        var name = normalized.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
