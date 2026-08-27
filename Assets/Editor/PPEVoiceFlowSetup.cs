using Prototype.Tyche.UI.Keyboard;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Editor-only setup and validation for the single PPE voice-flow target scene.
/// It uses Unity serialization APIs so no scene FileID is authored manually.
/// </summary>
public static class PPEVoiceFlowSetup
{
    private const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    private const string AudioManagerRootName = "AudioManager";
    private const string VoiceFlowRootName = "PPE Voice Flow";
    private const string KeyboardControllerTypeName =
        "Prototype.Tyche.UI.Keyboard.HangulKeyboardController";
    private const string ControllerGuideRootName = "ControllerGuide";
    private const string ControllerGuideContextName = "Context";
    private const string TriggerVisualName = "1_Ctrl_Trigger";
    private const string GripVisualName = "2_Ctrl_Grip";
    private const string JoystickVisualName = "3_Ctrl_Joystick";
    private const string SimpleRayVisualName = "1_Ray";
    private const string SimpleMarkerVisualName = "2_Marker";
    private const string SimpleRayTVisualName = "3_Ray_T";
    private const string KeyboardLayoutName = "KeyboardLayout";
    private const string ControllerEducationEntryName = "Controller Education Entry (Visual Only)";
    private const string ControllerEducationLabelName = "Controller Education Label";

    private static readonly string[] ControllerRayClipPaths =
    {
        "Assets/Audio/Voice/1_2_ContDetail/2_VO_PPE_CTRL_001_Start.ogg",
        "Assets/Audio/Voice/1_2_ContDetail/2_VO_PPE_CTRL_002_RayTrigger.ogg",
    };

    private static readonly string[] ControllerMarkerClipPaths =
    {
        "Assets/Audio/Voice/1_2_ContDetail/2_VO_PPE_CTRL_003_GripGrab_Release.ogg",
    };

    private static readonly string[] ControllerRayTClipPaths =
    {
        "Assets/Audio/Voice/1_2_ContDetail/2_VO_PPE_CTRL_004_Joystick_Marker.ogg",
        "Assets/Audio/Voice/1_2_ContDetail/2_VO_PPE_CTRL_005_Joystick_Ray_T.ogg",
    };

    private static readonly string[] ControllerSimpTriggerClipPaths =
    {
        "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_001_Start.mp3",
        "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_002_RayTrigger.mp3",
    };

    private static readonly string[] ControllerSimpGripClipPaths =
    {
        "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_003_GripGrab_Release.mp3",
    };

    private static readonly string[] ControllerSimpJoystickClipPaths =
    {
        "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_004_Joystick.mp3",
        "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_005_GuideFollow.mp3",
    };

    private static readonly string[] CardIntroClipPaths =
    {
        "Assets/Audio/Voice/2_Card/3_VO_PPE_CARD_001_CheckCard.mp3",
    };

    private static readonly string[] CardDetailClipPaths =
    {
        "Assets/Audio/Voice/2_Card/3_VO_PPE_CARD_002_CardSelected.mp3",
        "Assets/Audio/Voice/3_Modal/VO_PPE_MODAL_001_SelectEdu.mp3",
    };

    private static readonly string[] PpeChoiceClipPaths =
    {
        "Assets/Audio/Voice/3_Modal/VO_PPE_MODAL_003_PPE_Select.mp3",
        "Assets/Audio/Voice/3_Modal/VO_PPE_MODAL_002_SelectMode.mp3",
    };

    private static readonly string[] EducationModeClipPaths =
    {
        "Assets/Audio/Voice/3_Modal/VO_PPE_MODAL_004_EduSelect.mp3",
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_001_PPE_MoveToPPE.mp3",
    };

    private static readonly string[] PpeAreaStartClipPaths =
    {
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_002_PPE_Start.mp3",
        "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_003_Table.mp3",
    };

    private const string TabletReleasedVoicePath =
        "Assets/Audio/Voice/PPE/4_VO_PPE_EDU_004_HazmatSuit.mp3";

    [MenuItem("Tools/PPE/Keyboard/Create Controller Education Visual Entry")]
    private static void CreateControllerEducationVisualEntry()
    {
        if (!ValidateTargetSceneIsOpen())
            return;

        Scene scene = SceneManager.GetActiveScene();
        GameObject existing = FindSceneObject(scene, ControllerEducationEntryName);

        GameObject layoutObject = FindSceneObject(scene, KeyboardLayoutName);
        RectTransform layoutRect = layoutObject != null
            ? layoutObject.GetComponent<RectTransform>()
            : null;
        XRKeyboardKey sourceA = FindKeyboardKey(layoutObject, "A");
        XRKeyboardKey enterKey = FindKeyboardKey(layoutObject, "Enter");
        RectTransform sourceARect = sourceA != null ? sourceA.GetComponent<RectTransform>() : null;
        RectTransform enterRect = enterKey != null ? enterKey.GetComponent<RectTransform>() : null;
        RectTransform contentRect = layoutRect != null ? layoutRect.parent as RectTransform : null;
        Canvas keyboardCanvas = layoutRect != null ? layoutRect.GetComponentInParent<Canvas>() : null;
        RectTransform placementRoot = keyboardCanvas != null
            ? keyboardCanvas.transform as RectTransform
            : null;

        if (sourceARect == null || enterRect == null || contentRect == null || placementRoot == null)
        {
            Debug.LogError(
                "Controller education entry requires authored A and Enter keys under KeyboardLayout and the nearest keyboard Canvas RectTransform.",
                layoutObject);
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);

        bool created = existing == null;
        GameObject entry = created ? Object.Instantiate(sourceA.gameObject) : existing;
        if (created)
        {
            entry.name = ControllerEducationEntryName;
            Undo.RegisterCreatedObjectUndo(entry, "Create Controller Education Visual Entry");
        }

        RectTransform entryRect = entry.GetComponent<RectTransform>();
        Undo.RecordObject(entryRect, "Align Controller Education Visual Entry");
        if (entryRect.parent != placementRoot)
        {
            Undo.SetTransformParent(
                entryRect,
                placementRoot,
                "Move Controller Education Visual Entry Outside Keyboard Layout");
        }
        entryRect.SetSiblingIndex(contentRect.GetSiblingIndex() + 1);

        Vector2 sourceSize = sourceARect.rect.size;
        if (sourceSize.x <= 0f)
            sourceSize.x = 64f;
        if (sourceSize.y <= 0f)
            sourceSize.y = enterRect.rect.height > 0f ? enterRect.rect.height : 64f;

        Vector3 enterWorldCenter = enterRect.TransformPoint(enterRect.rect.center);
        Vector3 enterCenterInPlacementRoot = placementRoot.InverseTransformPoint(enterWorldCenter);
        float horizontalGap = Mathf.Max(16f, sourceSize.x * 0.375f);
        float horizontalOffset = (enterRect.rect.width + sourceSize.x) * 0.5f + horizontalGap;

        entryRect.localRotation = Quaternion.identity;
        entryRect.localScale = Vector3.one;
        entryRect.anchorMin = placementRoot.pivot;
        entryRect.anchorMax = placementRoot.pivot;
        entryRect.pivot = new Vector2(0.5f, 0.5f);
        entryRect.sizeDelta = sourceSize;
        entryRect.anchoredPosition3D = new Vector3(
            enterCenterInPlacementRoot.x + horizontalOffset,
            enterCenterInPlacementRoot.y,
            enterCenterInPlacementRoot.z);

        XRKeyboardKey entryKey = entry.GetComponent<XRKeyboardKey>();
        TMP_Text keyLabel = entryKey != null ? entryKey.textComponent : null;
        if (keyLabel != null)
            keyLabel.text = "A";
        DisableVisualEntryInteraction(entry);
        if (created)
        {
            CreateControllerEducationLabel(entryRect, keyLabel, sourceSize);
        }

        EditorUtility.SetDirty(entry);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = entry;
        Debug.Log(
            created
                ? "Created a visual-only A entry beside Enter, outside all keyboard LayoutGroups, with the TMP label '컨트롤러 교육'."
                : "Updated the existing controller-education entry to A, kept it outside all keyboard LayoutGroups, and aligned its depth with Enter.",
            entry);
    }

    private static XRKeyboardKey FindKeyboardKey(GameObject layoutObject, string keyName)
    {
        if (layoutObject == null)
            return null;

        XRKeyboardKey[] keys = layoutObject.GetComponentsInChildren<XRKeyboardKey>(true);
        foreach (XRKeyboardKey key in keys)
        {
            if (key != null && key.name == keyName)
                return key;
        }

        return null;
    }

    private static void DisableVisualEntryInteraction(GameObject entry)
    {
        foreach (XRKeyboardKey key in entry.GetComponentsInChildren<XRKeyboardKey>(true))
            key.enabled = false;
        foreach (XRPokeFollowAffordance affordance in entry.GetComponentsInChildren<XRPokeFollowAffordance>(true))
            affordance.enabled = false;
        foreach (KeyboardBatchFollow follow in entry.GetComponentsInChildren<KeyboardBatchFollow>(true))
            follow.enabled = false;
        foreach (HangulShiftLegend legend in entry.GetComponentsInChildren<HangulShiftLegend>(true))
            legend.enabled = false;
        foreach (Graphic graphic in entry.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        foreach (Collider collider in entry.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (AudioSource audioSource in entry.GetComponentsInChildren<AudioSource>(true))
            audioSource.enabled = false;
    }

    private static void CreateControllerEducationLabel(
        RectTransform entryRect,
        TMP_Text keyLabel,
        Vector2 sourceSize)
    {
        GameObject labelObject;
        TMP_Text label;
        if (keyLabel != null)
        {
            labelObject = Object.Instantiate(keyLabel.gameObject, entryRect);
            label = labelObject.GetComponent<TMP_Text>();
        }
        else
        {
            labelObject = new GameObject(
                ControllerEducationLabelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(entryRect, false);
            label = labelObject.GetComponent<TMP_Text>();
        }

        labelObject.name = ControllerEducationLabelName;
        Undo.RegisterCreatedObjectUndo(labelObject, "Create Controller Education TMP Label");

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.localRotation = Quaternion.identity;
        labelRect.localScale = Vector3.one;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -sourceSize.y * 0.65f);
        labelRect.sizeDelta = new Vector2(Mathf.Max(180f, sourceSize.x * 2.8f), 40f);

        label.text = "컨트롤러 교육";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = keyLabel != null ? Mathf.Max(18f, keyLabel.fontSize * 0.55f) : 22f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
    }

    [MenuItem("Tools/PPE/Voice Flow/Setup HandTest Scale 0")]
    private static void SetupHandTestScale0()
    {
        if (!ValidateTargetSceneIsOpen())
            return;

        Scene scene = SceneManager.GetActiveScene();
        AudioManager audioManager = FindSceneComponent<AudioManager>(scene);
        if (audioManager == null || audioManager.gameObject.name != AudioManagerRootName)
        {
            Debug.LogError(
                "PPE voice flow setup requires the scene-root AudioManager. Run Tools > Audio > Setup HandTest Scale 0 Scene Audio Manager first.",
                null);
            return;
        }

        PPEVoiceFlowDirector director = FindSceneComponent<PPEVoiceFlowDirector>(scene);
        Transform existingHost = audioManager.transform.Find(VoiceFlowRootName);
        GameObject directorHost = existingHost != null ? existingHost.gameObject : null;
        if (directorHost == null && director != null && director.gameObject.name == VoiceFlowRootName)
        {
            directorHost = director.gameObject;
            Undo.SetTransformParent(
                directorHost.transform,
                audioManager.transform,
                "Move PPE Voice Flow under AudioManager");
        }

        if (directorHost == null)
        {
            directorHost = new GameObject(VoiceFlowRootName);
            Undo.RegisterCreatedObjectUndo(directorHost, "Create PPE Voice Flow Root");
            Undo.SetTransformParent(
                directorHost.transform,
                audioManager.transform,
                "Parent PPE Voice Flow under AudioManager");
        }

        if (director == null || director.gameObject != directorHost)
        {
            PPEVoiceFlowDirector previousDirector = director;
            director = Undo.AddComponent<PPEVoiceFlowDirector>(directorHost);
            if (previousDirector != null)
            {
                EditorJsonUtility.FromJsonOverwrite(
                    EditorJsonUtility.ToJson(previousDirector),
                    director);
                Undo.DestroyObjectImmediate(previousDirector);
            }
        }

        Undo.RecordObject(director, "Configure PPE Voice Flow");
        director.EnsureDefaultVoiceSteps();

        SerializedObject serializedDirector = new(director);
        SetReference(serializedDirector, "m_WindowPresentationRoot", FindSceneObject(scene, "Place"));
        GameObject keyboardCanvas = FindSceneObject(scene, "Modal  Keyboard Canvas");
        GameObject keyboardPresentation = keyboardCanvas != null
            ? FindTransform(keyboardCanvas.transform, "Scenario Detail Modal")?.gameObject
            : null;
        SetReference(
            serializedDirector,
            "m_KeyboardPresentationRoot",
            keyboardPresentation != null ? keyboardPresentation : keyboardCanvas);
        SetReference(serializedDirector, "m_ControllerGuideRoot", FindSceneObject(scene, "ControllerGuide"));
        SetReference(serializedDirector, "m_ScenarioCardCanvas", FindSceneObject(scene, "Scenario Card Canvas"));
        SetReference(serializedDirector, "m_ScenarioSelectionRoot", FindSceneObject(scene, "Scenario Selection HUD"));
        GameObject simpleRayGroup = FindSceneObject(scene, SimpleRayVisualName);
        Transform simpleRayTransform = simpleRayGroup != null ? simpleRayGroup.transform : null;
        SetReference(
            serializedDirector,
            "m_ControllerRayStep",
            simpleRayTransform != null
                ? FindTransform(simpleRayTransform, "Card")?.gameObject
                : null);
        SetReference(serializedDirector, "m_ControllerMarkerStep", FindSceneObject(scene, "2_Marker"));
        SetReference(serializedDirector, "m_ControllerRayTStep", FindSceneObject(scene, "3_Ray_T"));
        SetReference(
            serializedDirector,
            "m_ControllerPanelStep",
            simpleRayTransform != null
                ? FindTransform(simpleRayTransform, "Panel")?.gameObject
                : null);
        SetReference(
            serializedDirector,
            "m_TabletReleasedVoice",
            AssetDatabase.LoadAssetAtPath<AudioClip>(TabletReleasedVoicePath));
        SetControllerModelReferencesIfEmpty(serializedDirector, scene);

        GameObject cardCanvas = FindSceneObject(scene, "Scenario Card Canvas");
        SetReference(
            serializedDirector,
            "m_ScenarioDetailModal",
            cardCanvas != null ? cardCanvas.GetComponent<ScenarioDetailModal>() : null);

        serializedDirector.ApplyModifiedProperties();
        EditorUtility.SetDirty(director);

        Component keyboardController = FindSceneComponentByTypeName(scene, KeyboardControllerTypeName);
        if (keyboardController == null)
        {
            Debug.LogError("PPE voice flow setup could not find HangulKeyboardController.", director);
        }
        else
        {
            SerializedObject serializedKeyboardController = new(keyboardController);
            SerializedProperty disableCanvasOnSubmit =
                serializedKeyboardController.FindProperty("m_DisableCanvasOnSubmit");
            if (disableCanvasOnSubmit != null)
                disableCanvasOnSubmit.boolValue = false;
            serializedKeyboardController.ApplyModifiedProperties();
            EditorUtility.SetDirty(keyboardController);

            PPEVoiceKeyboardEventRelay keyboardRelay =
                keyboardController.GetComponent<PPEVoiceKeyboardEventRelay>();
            if (keyboardRelay == null)
                keyboardRelay = Undo.AddComponent<PPEVoiceKeyboardEventRelay>(keyboardController.gameObject);

            Undo.RecordObject(keyboardRelay, "Configure PPE Voice Keyboard Relay");
            SerializedObject serializedKeyboardRelay = new(keyboardRelay);
            SetReference(serializedKeyboardRelay, "m_KeyboardController", keyboardController);
            SetReference(serializedKeyboardRelay, "m_Director", director);
            serializedKeyboardRelay.ApplyModifiedProperties();
            EditorUtility.SetDirty(keyboardRelay);
        }

        TeleportationProvider teleportationProvider = FindSceneComponent<TeleportationProvider>(scene);
        if (teleportationProvider == null)
        {
            Debug.LogError("PPE voice flow setup could not find Teleportation Provider.", null);
        }
        else
        {
            PPEVoiceTeleportEventRelay relay =
                teleportationProvider.GetComponent<PPEVoiceTeleportEventRelay>();
            if (relay == null)
                relay = Undo.AddComponent<PPEVoiceTeleportEventRelay>(teleportationProvider.gameObject);

            Undo.RecordObject(relay, "Configure PPE Voice Teleport Relay");
            SerializedObject serializedRelay = new(relay);
            SetReference(serializedRelay, "m_TeleportationProvider", teleportationProvider);
            SetReference(serializedRelay, "m_Director", director);
            serializedRelay.ApplyModifiedProperties();
            EditorUtility.SetDirty(relay);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "PPE voice flow setup completed. Review the director references and save the scene manually.",
            director);
    }

    [MenuItem("Tools/PPE/Voice Flow/Configure Controller Guide Narration")]
    private static void ConfigureControllerGuideNarration()
    {
        if (!ValidateTargetSceneIsOpen())
            return;

        Scene scene = SceneManager.GetActiveScene();
        PPEVoiceFlowDirector director = FindSceneComponent<PPEVoiceFlowDirector>(scene);
        GameObject guideRoot = FindSceneObject(scene, ControllerGuideRootName);
        Transform context = guideRoot != null
            ? FindTransform(guideRoot.transform, ControllerGuideContextName)
            : null;

        GameObject triggerVisual = FindGuideVisual(context, TriggerVisualName);
        GameObject gripVisual = FindGuideVisual(context, GripVisualName);
        GameObject joystickVisual = FindGuideVisual(context, JoystickVisualName);
        GameObject simpleRayVisual = FindGuideVisual(context, SimpleRayVisualName);
        GameObject simpleMarkerVisual = FindGuideVisual(context, SimpleMarkerVisualName);
        GameObject simpleRayTVisual = FindGuideVisual(context, SimpleRayTVisualName);

        if (director == null
            || context == null
            || triggerVisual == null
            || gripVisual == null
            || joystickVisual == null
            || simpleRayVisual == null
            || simpleMarkerVisual == null
            || simpleRayTVisual == null)
        {
            Debug.LogError(
                "Controller guide narration requires PPEVoiceFlowDirector and all authored long/simple visuals " +
                $"under ControllerGuide/Context, including '{SimpleRayVisualName}', " +
                $"'{SimpleMarkerVisualName}', and '{SimpleRayTVisualName}'.",
                director);
            return;
        }

        Undo.RecordObject(director, "Configure Controller Guide Narration");
        SerializedObject serializedDirector = new(director);
        SerializedProperty steps = serializedDirector.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpleSteps = serializedDirector.FindProperty("m_ControllerSimpVoiceSteps");
        if (steps == null
            || !ConfigureControllerGuideStep(
                steps,
                "controller_edu_trigger",
                ControllerRayClipPaths,
                new[] { triggerVisual, triggerVisual },
                null,
                director)
            || !ConfigureControllerGuideStep(
                steps,
                "controller_edu_grip",
                ControllerMarkerClipPaths,
                new[] { gripVisual },
                null,
                director)
            || !ConfigureControllerGuideStep(
                steps,
                "controller_edu_joystick",
                ControllerRayTClipPaths,
                new[] { joystickVisual, joystickVisual },
                null,
                director)
            || simpleSteps == null
            || !ConfigureControllerGuideStep(
                simpleSteps,
                "controller_simp_trigger",
                ControllerSimpTriggerClipPaths,
                new[] { simpleRayVisual, simpleRayVisual },
                triggerVisual,
                director)
            || !ConfigureControllerGuideStep(
                simpleSteps,
                "controller_simp_grip",
                ControllerSimpGripClipPaths,
                new[] { simpleMarkerVisual },
                gripVisual,
                director)
            || !ConfigureControllerGuideStep(
                simpleSteps,
                "controller_simp_joystick",
                ControllerSimpJoystickClipPaths,
                new[] { simpleRayTVisual, simpleRayTVisual },
                joystickVisual,
                director))
        {
            return;
        }

        // This configuration is for the normal startup sequence. Focused card/modal/teleport
        // test overrides would skip every controller narration step.
        serializedDirector.FindProperty("m_InitialState").enumValueIndex =
            (int)PPEVoiceFlowDirector.FlowState.Welcome;
        serializedDirector.FindProperty("m_StartAtCardIntroForTesting").boolValue = false;
        serializedDirector.FindProperty("m_StartAtModalDetailForTesting").boolValue = false;
        serializedDirector.FindProperty("m_StartAtTeleportForTesting").boolValue = false;
        serializedDirector.FindProperty("m_ControllerNarrationAfterName").enumValueIndex =
            (int)PPEVoiceFlowDirector.ControllerGuideNarration.Simple;
        serializedDirector.ApplyModifiedProperties();
        EditorUtility.SetDirty(director);

        GameObject[] visuals =
        {
            triggerVisual,
            gripVisual,
            joystickVisual,
            simpleRayVisual,
            simpleMarkerVisual,
            simpleRayTVisual,
        };
        Undo.RecordObjects(visuals, "Set Initial Controller Guide Visual");
        for (int index = 0; index < visuals.Length; index++)
        {
            visuals[index].SetActive(
                visuals[index] == triggerVisual || visuals[index] == simpleRayVisual);
            EditorUtility.SetDirty(visuals[index]);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = director;
        Debug.Log(
            "Configured Controller Edu and Controller Simp narration; name submission now uses Controller Simp.",
            director);
    }

    [MenuItem("Tools/PPE/Voice Flow/Validate HandTest Scale 0")]
    private static void ValidateHandTestScale0()
    {
        if (!ValidateTargetSceneIsOpen())
            return;

        Scene scene = SceneManager.GetActiveScene();
        PPEVoiceFlowDirector director = FindSceneComponent<PPEVoiceFlowDirector>(scene);
        if (director == null)
        {
            Debug.LogError("PPE voice flow validation could not find PPEVoiceFlowDirector.", null);
            return;
        }

        AudioManager audioManager = FindSceneComponent<AudioManager>(scene);
        if (audioManager == null)
        {
            Debug.LogError(
                "PPE voice flow validation could not find the scene AudioManager.",
                director);
        }
        else if (director.gameObject.name != VoiceFlowRootName || director.transform.parent != audioManager.transform)
        {
            Debug.LogError(
                "PPEVoiceFlowDirector must be on the 'PPE Voice Flow' child of AudioManager, not on XR Origin or UI.",
                director);
        }

        SerializedObject serializedDirector = new(director);
        CheckReference(serializedDirector, "m_WindowPresentationRoot", "Window presentation root");
        CheckReference(serializedDirector, "m_KeyboardPresentationRoot", "Keyboard presentation root");
        CheckReference(serializedDirector, "m_ControllerGuideRoot", "Controller guide root");
        CheckReference(serializedDirector, "m_ScenarioCardCanvas", "Scenario Card Canvas");
        CheckReference(serializedDirector, "m_ScenarioSelectionRoot", "Scenario selection root");
        CheckReference(serializedDirector, "m_ControllerRayStep", "1_Ray/Card");
        CheckReference(serializedDirector, "m_ControllerMarkerStep", "2_Marker");
        CheckReference(serializedDirector, "m_ControllerRayTStep", "3_Ray_T");
        CheckReference(serializedDirector, "m_ControllerPanelStep", "1_Ray/Panel");
        CheckControllerModelReferences(serializedDirector, director);
        CheckReference(serializedDirector, "m_ScenarioDetailModal", "ScenarioDetailModal");
        CheckReference(serializedDirector, "m_TabletReleasedVoice", "PPE 004 HazmatSuit");
        CheckReference(serializedDirector, "m_CenterMarkerArrivedVoice", "PPE 007 OrderGuide");
        CheckReference(serializedDirector, "m_CenterMarkerCheckVoice", "PPE 008 CheckPPE");

        GameObject keyboardPresentationRoot = serializedDirector
            .FindProperty("m_KeyboardPresentationRoot")?.objectReferenceValue as GameObject;
        if (keyboardPresentationRoot != null && keyboardPresentationRoot.activeSelf)
        {
            Debug.LogError(
                "Keyboard presentation root must be inactive in the authored scene so Welcome appears before NameInput.",
                keyboardPresentationRoot);
        }

        SerializedProperty steps = serializedDirector.FindProperty("m_VoiceSteps");
        SerializedProperty eduSteps = serializedDirector.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpSteps = serializedDirector.FindProperty("m_ControllerSimpVoiceSteps");
        if (steps == null || steps.arraySize == 0 ||
            eduSteps == null || eduSteps.arraySize == 0 ||
            simpSteps == null || simpSteps.arraySize == 0)
        {
            Debug.LogError("PPE voice flow requires main, Controller Edu, and Controller Simp voice-step groups.", director);
        }
        else
        {
            ValidateHandTestVoiceAssignments(steps, eduSteps, simpSteps, director);
        }

        PPEVoiceTeleportEventRelay relay = FindSceneComponent<PPEVoiceTeleportEventRelay>(scene);
        if (relay == null)
            Debug.LogError("PPE voice flow validation could not find PPEVoiceTeleportEventRelay.", director);

        PPEVoiceKeyboardEventRelay keyboardRelay = FindSceneComponent<PPEVoiceKeyboardEventRelay>(scene);
        if (keyboardRelay == null)
            Debug.LogError("PPE voice flow validation could not find PPEVoiceKeyboardEventRelay.", director);

        Debug.Log("PPE voice flow static validation completed. Quest/OpenXR validation is still required.", director);
    }

    private static bool ValidateTargetSceneIsOpen()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == TargetScenePath)
            return true;

        Debug.LogError(
            $"PPE voice flow setup is restricted to '{TargetScenePath}'. " +
            $"Current scene is '{scene.path}'.", null);
        return false;
    }

    private static void CheckReference(SerializedObject serializedObject, string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
            Debug.LogError($"PPE voice flow reference is missing: {label} ({propertyName}).", serializedObject.targetObject);
    }

    private static void ValidateHandTestVoiceAssignments(
        SerializedProperty steps,
        SerializedProperty eduSteps,
        SerializedProperty simpSteps,
        PPEVoiceFlowDirector director)
    {
        Scene scene = director.gameObject.scene;
        GameObject guideRoot = FindSceneObject(scene, ControllerGuideRootName);
        Transform context = guideRoot != null
            ? FindTransform(guideRoot.transform, ControllerGuideContextName)
            : null;
        GameObject triggerVisual = FindGuideVisual(context, TriggerVisualName);
        GameObject gripVisual = FindGuideVisual(context, GripVisualName);
        GameObject joystickVisual = FindGuideVisual(context, JoystickVisualName);
        GameObject simpleRayVisual = FindGuideVisual(context, SimpleRayVisualName);
        GameObject simpleMarkerVisual = FindGuideVisual(context, SimpleMarkerVisualName);
        GameObject simpleRayTVisual = FindGuideVisual(context, SimpleRayTVisualName);

        if (triggerVisual == null
            || gripVisual == null
            || joystickVisual == null
            || simpleRayVisual == null
            || simpleMarkerVisual == null
            || simpleRayTVisual == null)
        {
            Debug.LogError(
                "ControllerGuide/Context is missing one or more authored controller narration visuals.",
                director);
        }
        else
        {
            Transform simpleCard = FindTransform(simpleRayVisual.transform, "Card");
            Transform simplePanel = FindTransform(simpleRayVisual.transform, "Panel");
            if (simpleCard == null
                || simplePanel == null
                || !simpleCard.gameObject.activeSelf
                || !simplePanel.gameObject.activeSelf)
            {
                Debug.LogError(
                    "Controller Simp 1_Ray must preserve its authored active Card and Panel children.",
                    simpleRayVisual);
            }

            ValidateStepClips(eduSteps, "controller_edu_trigger", ControllerRayClipPaths, director);
            ValidateStepVisuals(
                eduSteps,
                "controller_edu_trigger",
                new[] { triggerVisual, triggerVisual },
                director);
            ValidateStepClips(eduSteps, "controller_edu_grip", ControllerMarkerClipPaths, director);
            ValidateStepVisuals(
                eduSteps,
                "controller_edu_grip",
                new[] { gripVisual },
                director);
            ValidateStepClips(eduSteps, "controller_edu_joystick", ControllerRayTClipPaths, director);
            ValidateStepVisuals(
                eduSteps,
                "controller_edu_joystick",
                new[] { joystickVisual, joystickVisual },
                director);

            ValidateStepVisuals(
                simpSteps,
                "controller_simp_trigger",
                new[] { simpleRayVisual, simpleRayVisual },
                director);
            ValidateStepCompanion(
                simpSteps, "controller_simp_trigger", triggerVisual, director);
            ValidateStepClips(
                simpSteps,
                "controller_simp_trigger",
                ControllerSimpTriggerClipPaths,
                director);
            ValidateStepVisuals(
                simpSteps,
                "controller_simp_grip",
                new[] { simpleMarkerVisual },
                director);
            ValidateStepCompanion(
                simpSteps, "controller_simp_grip", gripVisual, director);
            ValidateStepClips(
                simpSteps,
                "controller_simp_grip",
                ControllerSimpGripClipPaths,
                director);
            ValidateStepVisuals(
                simpSteps,
                "controller_simp_joystick",
                new[] { simpleRayTVisual, simpleRayTVisual },
                director);
            ValidateStepCompanion(
                simpSteps, "controller_simp_joystick", joystickVisual, director);
            ValidateStepClips(
                simpSteps,
                "controller_simp_joystick",
                ControllerSimpJoystickClipPaths,
                director);

            if (!triggerVisual.activeSelf
                || gripVisual.activeSelf
                || joystickVisual.activeSelf
                || !simpleRayVisual.activeSelf
                || simpleMarkerVisual.activeSelf
                || simpleRayTVisual.activeSelf)
            {
                Debug.LogError(
                    "Controller guide initial state must show 1_Ctrl_Trigger and the simple 1_Ray group only.",
                    director);
            }
        }

        SerializedObject serializedDirector = steps.serializedObject;
        if (serializedDirector.FindProperty("m_ControllerNarrationAfterName").enumValueIndex
            != (int)PPEVoiceFlowDirector.ControllerGuideNarration.Simple)
        {
            Debug.LogError(
                "The name-submit route must use the assigned Controller Simp narration.",
                director);
        }
        if (serializedDirector.FindProperty("m_InitialState").enumValueIndex
            != (int)PPEVoiceFlowDirector.FlowState.Welcome)
        {
            Debug.LogError(
                "Normal PPE voice flow must begin at Welcome.",
                director);
        }

        if (serializedDirector.FindProperty("m_StartAtCardIntroForTesting").boolValue
            || serializedDirector.FindProperty("m_StartAtModalDetailForTesting").boolValue
            || serializedDirector.FindProperty("m_StartAtTeleportForTesting").boolValue)
        {
            Debug.LogError(
                "Focused card/modal/teleport test override skips part of the normal startup flow and must be disabled.",
                director);
        }

        ValidateStepClips(steps, "name", System.Array.Empty<string>(), director);
        ValidateStepClips(steps, "card_intro", CardIntroClipPaths, director);
        ValidateStepClips(steps, "card_detail", CardDetailClipPaths, director);
        ValidateStepClips(steps, "ppe_education_selected", PpeChoiceClipPaths, director);
        ValidateStepClips(steps, "education_selected", EducationModeClipPaths, director);
        ValidateStepClips(
            steps,
            "teleport_instruction",
            System.Array.Empty<string>(),
            director);
        ValidateStepClips(steps, "ppe_area", PpeAreaStartClipPaths, director);
        ValidateClipReference(
            serializedDirector,
            "m_TabletReleasedVoice",
            TabletReleasedVoicePath,
            director);
    }

    private static void ValidateClipReference(
        SerializedObject serializedObject,
        string propertyName,
        string expectedPath,
        Object context)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        string actualPath = property == null
            ? string.Empty
            : AssetDatabase.GetAssetPath(property.objectReferenceValue);
        if (actualPath != expectedPath)
        {
            Debug.LogError(
                $"PPE voice reference '{propertyName}' is '{actualPath}', expected '{expectedPath}'.",
                context);
        }
    }

    private static bool ConfigureControllerGuideStep(
        SerializedProperty steps,
        string stepId,
        string[] clipPaths,
        GameObject[] visuals,
        GameObject companionVisual,
        PPEVoiceFlowDirector director)
    {
        if (clipPaths.Length != visuals.Length)
        {
            Debug.LogError(
                $"Controller guide step '{stepId}' has mismatched clip and visual counts.",
                director);
            return false;
        }

        SerializedProperty step = FindStep(steps, stepId);
        SerializedProperty clips = step?.FindPropertyRelative("clips");
        SerializedProperty guideVisuals = step?.FindPropertyRelative("controllerGuideVisuals");
        SerializedProperty guideCompanion = step?.FindPropertyRelative("controllerGuideCompanionVisual");
        if (clips == null || guideVisuals == null || guideCompanion == null)
        {
            Debug.LogError($"PPE voice step is missing: {stepId}.", director);
            return false;
        }

        AudioClip[] loadedClips = new AudioClip[clipPaths.Length];
        for (int index = 0; index < clipPaths.Length; index++)
        {
            loadedClips[index] = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPaths[index]);
            if (loadedClips[index] == null)
            {
                Debug.LogError(
                    $"Controller guide narration clip is missing: '{clipPaths[index]}'.",
                    director);
                return false;
            }
        }

        clips.arraySize = loadedClips.Length;
        guideVisuals.arraySize = visuals.Length;
        for (int index = 0; index < loadedClips.Length; index++)
        {
            clips.GetArrayElementAtIndex(index).objectReferenceValue = loadedClips[index];
            guideVisuals.GetArrayElementAtIndex(index).objectReferenceValue = visuals[index];
        }

        guideCompanion.objectReferenceValue = companionVisual;

        return true;
    }

    private static void ValidateStepCompanion(
        SerializedProperty steps,
        string stepId,
        GameObject expectedVisual,
        PPEVoiceFlowDirector director)
    {
        SerializedProperty step = FindStep(steps, stepId);
        SerializedProperty companion = step?.FindPropertyRelative("controllerGuideCompanionVisual");
        if (companion == null || companion.objectReferenceValue != expectedVisual)
        {
            Debug.LogError(
                $"PPE voice step '{stepId}' companion is '{companion?.objectReferenceValue?.name}', expected '{expectedVisual.name}'.",
                director);
        }
    }

    private static void ValidateStepClips(
        SerializedProperty steps,
        string stepId,
        string[] expectedPaths,
        PPEVoiceFlowDirector director)
    {
        SerializedProperty step = FindStep(steps, stepId);
        SerializedProperty clips = step?.FindPropertyRelative("clips");
        if (clips == null)
        {
            Debug.LogError($"PPE voice step is missing: {stepId}.", director);
            return;
        }

        if (clips.arraySize != expectedPaths.Length)
        {
            Debug.LogError(
                $"PPE voice step '{stepId}' has {clips.arraySize} clip slots; expected {expectedPaths.Length}. " +
                $"Review its Inspector assignment.", director);
            return;
        }

        for (int index = 0; index < expectedPaths.Length; index++)
        {
            string actualPath = AssetDatabase.GetAssetPath(clips.GetArrayElementAtIndex(index).objectReferenceValue);
            if (actualPath != expectedPaths[index])
            {
                Debug.LogError(
                    $"PPE voice step '{stepId}' clip {index + 1} is '{actualPath}', expected '{expectedPaths[index]}'.",
                    director);
            }
        }
    }

    private static void ValidateStepVisuals(
        SerializedProperty steps,
        string stepId,
        GameObject[] expectedVisuals,
        PPEVoiceFlowDirector director)
    {
        SerializedProperty step = FindStep(steps, stepId);
        SerializedProperty visuals = step?.FindPropertyRelative("controllerGuideVisuals");
        if (visuals == null)
        {
            Debug.LogError($"PPE voice step guide visuals are missing: {stepId}.", director);
            return;
        }

        if (visuals.arraySize != expectedVisuals.Length)
        {
            Debug.LogError(
                $"PPE voice step '{stepId}' has {visuals.arraySize} guide visual slots; expected {expectedVisuals.Length}.",
                director);
            return;
        }

        for (int index = 0; index < expectedVisuals.Length; index++)
        {
            Object actual = visuals.GetArrayElementAtIndex(index).objectReferenceValue;
            if (actual != expectedVisuals[index])
            {
                Debug.LogError(
                    $"PPE voice step '{stepId}' guide visual {index + 1} is '{actual?.name}', expected '{expectedVisuals[index].name}'.",
                    director);
            }
        }
    }

    private static void ValidateRepeatClip(
        SerializedProperty steps,
        string stepId,
        string expectedPath,
        PPEVoiceFlowDirector director)
    {
        SerializedProperty step = FindStep(steps, stepId);
        SerializedProperty repeat = step?.FindPropertyRelative("repeatClip");
        string actualPath = repeat == null
            ? string.Empty
            : AssetDatabase.GetAssetPath(repeat.objectReferenceValue);
        if (actualPath != expectedPath)
        {
            Debug.LogError(
                $"PPE voice step '{stepId}' repeat clip is '{actualPath}', expected '{expectedPath}'.",
                director);
        }
    }

    private static SerializedProperty FindStep(SerializedProperty steps, string stepId)
    {
        for (int index = 0; index < steps.arraySize; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            if (step.FindPropertyRelative("stepId")?.stringValue == stepId)
                return step;
        }

        return null;
    }

    private static GameObject FindGuideVisual(Transform context, string objectName)
    {
        return context != null
            ? FindTransform(context, objectName)?.gameObject
            : null;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetControllerModelReferencesIfEmpty(SerializedObject serializedDirector, Scene scene)
    {
        SerializedProperty property = serializedDirector.FindProperty("m_ControllerModelVisuals");
        if (property == null)
            return;

        for (int index = 0; index < property.arraySize; index++)
        {
            if (property.GetArrayElementAtIndex(index).objectReferenceValue != null)
                return;
        }

        GameObject left = FindSceneObject(scene, "OculusTouchForQuest2_Left");
        GameObject right = FindSceneObject(scene, "OculusTouchForQuest2_Right");
        if (left == null || right == null)
            return;

        property.arraySize = 2;
        property.GetArrayElementAtIndex(0).objectReferenceValue = left.transform;
        property.GetArrayElementAtIndex(1).objectReferenceValue = right.transform;
    }

    private static void CheckControllerModelReferences(
        SerializedObject serializedDirector,
        PPEVoiceFlowDirector director)
    {
        SerializedProperty property = serializedDirector.FindProperty("m_ControllerModelVisuals");
        if (property == null || property.arraySize != 2 ||
            property.GetArrayElementAtIndex(0).objectReferenceValue == null ||
            property.GetArrayElementAtIndex(1).objectReferenceValue == null)
        {
            Debug.LogError(
                "PPE voice flow requires exactly two authored Quest 2 controller model Transform references.",
                director);
        }
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

    private static Transform FindTransform(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindTransform(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Component FindSceneComponentByTypeName(Scene scene, string fullTypeName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component != null && component.GetType().FullName == fullTypeName)
                    return component;
            }
        }

        return null;
    }
}
