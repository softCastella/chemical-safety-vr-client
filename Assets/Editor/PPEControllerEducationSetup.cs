using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

public static class PPEControllerEducationSetup
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test.unity";
    private const string LegacyEntryName = "Controller Education Entry (Visual Only)";
    private const string EntryName = "Controller Education Entry";
    private const string KeyboardCanvasName = "Modal  Keyboard Canvas";
    private const string RayBlockerName = "Keyboard Ray Blocker";
    private const string DetailedVoiceRoot = "Assets/Audio/Voice/1_2_ContDetail/";

    private static readonly string[][] DetailedClipPaths =
    {
        new[]
        {
            DetailedVoiceRoot + "VO_PPE_CTRL_DETAIL_001_Start.mp3",
            DetailedVoiceRoot + "VO_PPE_CTRL_DETAIL_002_RayTrigger.mp3",
        },
        new[]
        {
            DetailedVoiceRoot + "VO_PPE_CTRL_DETAIL_003_GripGrab_Release.mp3",
        },
        new[]
        {
            DetailedVoiceRoot + "VO_PPE_CTRL_DETAIL_004_Joystick.mp3",
            DetailedVoiceRoot + "VO_PPE_CTRL_DETAIL_005_GuideFollow.mp3",
        },
    };

    [MenuItem("Tools/PPE/Configure Controller Education Entry")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "Controller education setup cannot run during Play Mode. Stop Play Mode, wait for compilation, and run the menu again.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError(
                $"Controller education setup only supports the active scene '{ScenePath}'. " +
                $"Current scene: '{scene.path}'.");
            return;
        }

        PPEVoiceFlowDirector director = FindSingleSceneComponent<PPEVoiceFlowDirector>(scene);
        GameObject entry = FindSceneObject(scene, EntryName) ?? FindSceneObject(scene, LegacyEntryName);
        GameObject keyboardCanvas = FindSceneObject(scene, KeyboardCanvasName);
        if (director == null || entry == null || keyboardCanvas == null)
        {
            Debug.LogError(
                "Controller education setup requires one PPEVoiceFlowDirector, the authored A entry, " +
                $"and '{KeyboardCanvasName}' in '{ScenePath}'.");
            return;
        }

        AudioClip[][] detailedClips = LoadDetailedClips();
        if (detailedClips == null)
            return;

        // Required read-only spatial/serialization baseline before changing UI.
        PPEObjectSpatialDiagnosticHarness.Diagnose(entry);
        PPEObjectSpatialDiagnosticHarness.Diagnose(keyboardCanvas);

        Undo.SetCurrentGroupName("Configure PPE Controller Education");
        int undoGroup = Undo.GetCurrentGroup();

        ConfigureDetailedVoiceSteps(director, detailedClips);
        ConfigureEntry(entry, director);
        EnsureRayBlocker(keyboardCanvas);

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = entry;
        Debug.Log(
            $"[PPE Controller Education Setup] Configured XR UI/physical A entry, " +
            $"five detailed clips, reused two-sided Simple guide visuals, and keyboard ray blocker " +
            $"in '{ScenePath}'. Save the scene after validation.",
            entry);
    }

    private static AudioClip[][] LoadDetailedClips()
    {
        AudioClip[][] clips = new AudioClip[DetailedClipPaths.Length][];
        for (int stepIndex = 0; stepIndex < DetailedClipPaths.Length; stepIndex++)
        {
            clips[stepIndex] = new AudioClip[DetailedClipPaths[stepIndex].Length];
            for (int clipIndex = 0; clipIndex < DetailedClipPaths[stepIndex].Length; clipIndex++)
            {
                string path = DetailedClipPaths[stepIndex][clipIndex];
                clips[stepIndex][clipIndex] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[stepIndex][clipIndex] == null)
                {
                    Debug.LogError($"Controller education audio clip is missing or not imported: '{path}'.");
                    return null;
                }
            }
        }

        return clips;
    }

    private static void ConfigureDetailedVoiceSteps(
        PPEVoiceFlowDirector director,
        AudioClip[][] detailedClips)
    {
        Undo.RecordObject(director, "Assign PPE Controller Education Audio");
        director.EnsureDefaultVoiceSteps();
        SerializedObject serialized = new(director);
        SerializedProperty steps = serialized.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpleSteps = serialized.FindProperty("m_ControllerSimpVoiceSteps");
        if (steps == null || !steps.isArray || steps.arraySize != detailedClips.Length ||
            simpleSteps == null || !simpleSteps.isArray || simpleSteps.arraySize != detailedClips.Length)
        {
            throw new InvalidOperationException(
                "PPEVoiceFlowDirector controller steps must contain the authored Trigger, Grip, and Joystick groups.");
        }

        for (int stepIndex = 0; stepIndex < detailedClips.Length; stepIndex++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(stepIndex);
            SerializedProperty simpleStep = simpleSteps.GetArrayElementAtIndex(stepIndex);
            SerializedProperty clips = step.FindPropertyRelative("clips");
            SerializedProperty visuals = step.FindPropertyRelative("controllerGuideVisuals");
            SerializedProperty companion = step.FindPropertyRelative("controllerGuideCompanionVisual");
            SerializedProperty simpleVisuals = simpleStep.FindPropertyRelative("controllerGuideVisuals");
            SerializedProperty simpleCompanion = simpleStep.FindPropertyRelative("controllerGuideCompanionVisual");
            if (visuals == null || companion == null || simpleVisuals == null ||
                !simpleVisuals.isArray || simpleVisuals.arraySize == 0 ||
                simpleCompanion == null || simpleCompanion.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    $"Controller guide step {stepIndex} has no authored two-sided Simple visual pair to reuse.");
            }

            for (int visualIndex = 0; visualIndex < simpleVisuals.arraySize; visualIndex++)
            {
                if (simpleVisuals.GetArrayElementAtIndex(visualIndex).objectReferenceValue is GameObject visual)
                    PPEObjectSpatialDiagnosticHarness.Diagnose(visual);
            }
            if (simpleCompanion.objectReferenceValue is GameObject companionVisual)
                PPEObjectSpatialDiagnosticHarness.Diagnose(companionVisual);

            clips.arraySize = detailedClips[stepIndex].Length;
            for (int clipIndex = 0; clipIndex < detailedClips[stepIndex].Length; clipIndex++)
            {
                clips.GetArrayElementAtIndex(clipIndex).objectReferenceValue =
                    detailedClips[stepIndex][clipIndex];
            }

            visuals.arraySize = simpleVisuals.arraySize;
            for (int visualIndex = 0; visualIndex < simpleVisuals.arraySize; visualIndex++)
            {
                visuals.GetArrayElementAtIndex(visualIndex).objectReferenceValue =
                    simpleVisuals.GetArrayElementAtIndex(visualIndex).objectReferenceValue;
            }
            companion.objectReferenceValue = simpleCompanion.objectReferenceValue;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(director);
    }

    private static void ConfigureEntry(GameObject entry, PPEVoiceFlowDirector director)
    {
        XRKeyboardKey keyboardKey = entry.GetComponent<XRKeyboardKey>();
        Button button = entry.GetComponent<Button>();
        if (keyboardKey != null)
        {
            Navigation navigation = keyboardKey.navigation;
            Selectable.Transition transition = keyboardKey.transition;
            ColorBlock colors = keyboardKey.colors;
            SpriteState spriteState = keyboardKey.spriteState;
            AnimationTriggers animationTriggers = keyboardKey.animationTriggers;
            bool interactable = keyboardKey.interactable;
            Graphic targetGraphic = keyboardKey.targetGraphic;

            Undo.DestroyObjectImmediate(keyboardKey);
            button = Undo.AddComponent<Button>(entry);
            button.navigation = navigation;
            button.transition = transition;
            button.colors = colors;
            button.spriteState = spriteState;
            button.animationTriggers = animationTriggers;
            button.interactable = interactable;
            button.targetGraphic = targetGraphic;

            Image rootImage = entry.GetComponent<Image>();
            if (rootImage != null)
                rootImage.raycastTarget = true;
            if (targetGraphic != null)
                targetGraphic.raycastTarget = true;
        }

        if (button == null)
            throw new InvalidOperationException("The authored controller education A entry has no Button.");

        PPEControllerEducationEntry route = entry.GetComponent<PPEControllerEducationEntry>();
        if (route == null)
            route = Undo.AddComponent<PPEControllerEducationEntry>(entry);

        SerializedObject routeSerialized = new(route);
        SerializedProperty directorProperty = routeSerialized.FindProperty("m_VoiceFlowDirector");
        if (directorProperty.objectReferenceValue == null)
        {
            Undo.RecordObject(route, "Assign PPE Controller Education Director");
            directorProperty.objectReferenceValue = director;
            routeSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(route);
        }

        if (entry.name == LegacyEntryName)
        {
            Undo.RecordObject(entry, "Rename PPE Controller Education Entry");
            entry.name = EntryName;
        }

        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(entry);
    }

    private static void EnsureRayBlocker(GameObject keyboardCanvas)
    {
        Transform existing = keyboardCanvas.transform.Find(RayBlockerName);
        if (existing != null)
            return;

        GameObject blocker = new(RayBlockerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(blocker, "Create Keyboard Ray Blocker");
        blocker.layer = keyboardCanvas.layer;
        Undo.SetTransformParent(blocker.transform, keyboardCanvas.transform, "Parent Keyboard Ray Blocker");

        RectTransform rect = (RectTransform)blocker.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition3D = Vector3.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.SetAsFirstSibling();

        Image image = blocker.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
    }

    private static T FindSingleSceneComponent<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (found != null)
                    return null;
                found = candidate;
            }
        }

        return found;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindTransform(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindTransform(root.GetChild(index), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
