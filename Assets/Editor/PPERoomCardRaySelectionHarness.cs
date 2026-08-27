using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class PPERoomCardRaySelectionHarness
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    private const string ScaleZeroScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    private const string CanvasName = "XR UI Canvas (1)";
    private const string LeftNearFarInteractorName = "Left_NearFarInteractor";
    private const string RightNearFarInteractorName = "Right_NearFarInteractor";
    private const string PpeMarkerName = "XR Item Marker_small";
    private const float RequiredRayDistance = 20f;

    [MenuItem("Tools/PPE/Validate Card Ray Selection")]
    public static void Validate()
    {
        if (!Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE card ray selection validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> failures = new();
        GameObject canvasObject = FindSceneObject(scene, CanvasName);
        if (canvasObject == null)
        {
            failures.Add($"Missing '{CanvasName}'.");
        }
        else
        {
            ValidateCanvas(canvasObject, failures);
        }

        ValidateControllerRays(scene, failures);
        ValidateLocationMarker(scene, "XR Location Marker_big_PPE_1", failures);
        ValidateLocationMarker(scene, "XR Location Marker_big_PPE_2", failures);

        if (failures.Count > 0)
        {
            string message = "PPE card ray selection validation failed:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log($"PPE card ray selection validation passed for '{scene.path}': "
            + "controller ray + trigger only, Canvas hide targets, 40 m rays, and location markers are valid.");
    }

    [MenuItem("Tools/PPE/Validate Scale 0 Controller Test Inputs")]
    public static void ValidateScaleZeroControllerTestInputs()
    {
        if (!Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE scale-0 controller test input validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScaleZeroScenePath, OpenSceneMode.Single);
        List<string> failures = new();
        ValidatePpeMarkerReachability(scene, LeftNearFarInteractorName, failures);
        ValidatePpeMarkerReachability(scene, RightNearFarInteractorName, failures);
        ValidateBothHandInspection(scene, failures);
        ValidateTeleportManagers(scene, failures);
        ValidateMiniGuideActions(scene, failures);
        ValidateKeyboardControllerEducationEntry(scene, failures);
        ValidateMirrorGaugeHierarchy(scene, failures);
        ValidateVoiceFlowTriggerSkip(scene, failures);
        ValidateControllerHandStartupRegression(scene, failures);
        ValidateFinaleCompletionRequirements(scene, failures);
        ValidateControllerGuideAndDirectPpeModal(scene, failures);
        ValidateFullSuitOrigPose(failures);

        if (failures.Count > 0)
        {
            string message = "PPE scale-0 controller test input validation failed:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"PPE scale-0 controller test input validation passed for '{scene.path}': " +
            "both Near casters reach near-only PPE markers, PPE grab is not right-only, " +
            "teleport skips the omitted scenario gate, the mini guide uses authored XRI actions, " +
            "Trigger voice skip uses the authored left/right UI interactors, and the authored " +
            "Trigger/Grip/Joystick guide enters the direct PPE training modal flow.");
    }

    private static void ValidateControllerGuideAndDirectPpeModal(Scene scene, List<string> failures)
    {
        PPEVoiceFlowDirector[] directors = FindComponents<PPEVoiceFlowDirector>(scene);
        if (directors.Length != 1)
            return;

        SerializedObject serializedDirector = new(directors[0]);
        SerializedProperty eduVoiceSteps = serializedDirector.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpVoiceSteps = serializedDirector.FindProperty("m_ControllerSimpVoiceSteps");
        ValidateGuideStep(
            eduVoiceSteps,
            "controller_edu_trigger",
            new[] { "2_VO_PPE_CTRL_001_Start", "2_VO_PPE_CTRL_002_RayTrigger" },
            "1_Ctrl_Trigger",
            PPEVoiceFlowDirector.FlowState.ControllerMarker,
            failures);
        ValidateGuideStep(
            eduVoiceSteps,
            "controller_edu_grip",
            new[] { "2_VO_PPE_CTRL_003_GripGrab_Release" },
            "2_Ctrl_Grip",
            PPEVoiceFlowDirector.FlowState.ControllerRayT,
            failures);
        ValidateGuideStep(
            eduVoiceSteps,
            "controller_edu_joystick",
            new[] { "2_VO_PPE_CTRL_004_Joystick_Marker", "2_VO_PPE_CTRL_005_Joystick_Ray_T" },
            "3_Ctrl_Joystick",
            PPEVoiceFlowDirector.FlowState.CardIntro,
            failures);

        ValidateGuideStep(
            simpVoiceSteps,
            "controller_simp_trigger",
            new[] { "VO_PPE_CTRL_SIMP_001_Start", "VO_PPE_CTRL_SIMP_002_RayTrigger" },
            "1_Ray",
            PPEVoiceFlowDirector.FlowState.ControllerMarker,
            failures);
        ValidateGuideStep(
            simpVoiceSteps,
            "controller_simp_grip",
            new[] { "VO_PPE_CTRL_SIMP_003_GripGrab_Release" },
            "2_Marker",
            PPEVoiceFlowDirector.FlowState.ControllerRayT,
            failures);
        ValidateGuideStep(
            simpVoiceSteps,
            "controller_simp_joystick",
            new[] { "VO_PPE_CTRL_SIMP_004_Joystick", "VO_PPE_CTRL_SIMP_005_GuideFollow" },
            "3_Ray_T",
            PPEVoiceFlowDirector.FlowState.CardIntro,
            failures);
        ValidateGuideCompanion(simpVoiceSteps, "controller_simp_trigger", "1_Ctrl_Trigger", failures);
        ValidateGuideCompanion(simpVoiceSteps, "controller_simp_grip", "2_Ctrl_Grip", failures);
        ValidateGuideCompanion(simpVoiceSteps, "controller_simp_joystick", "3_Ctrl_Joystick", failures);

        AudioClip tabletReleasedVoice = serializedDirector.FindProperty("m_TabletReleasedVoice")
            ?.objectReferenceValue as AudioClip;
        if (tabletReleasedVoice == null || tabletReleasedVoice.name != "4_VO_PPE_EDU_004_HazmatSuit")
        {
            failures.Add(
                "Tablet release is not assigned to the authored 4_VO_PPE_EDU_004_HazmatSuit voice.");
        }

        SerializedProperty narrationAfterName =
            serializedDirector.FindProperty("m_ControllerNarrationAfterName");
        if (narrationAfterName == null || narrationAfterName.enumValueIndex !=
            (int)PPEVoiceFlowDirector.ControllerGuideNarration.Simple)
        {
            failures.Add(
                "The current name-submit route does not use the assigned Controller Simp narration.");
        }

        GameObject controllerGuide = FindSceneObject(scene, "ControllerGuide");
        Transform controllerContext = controllerGuide != null
            ? controllerGuide.transform.Find("Context")
            : null;
        Transform simpleRay = controllerContext != null
            ? controllerContext.Find("1_Ray")
            : null;
        Transform simpleCard = simpleRay != null ? simpleRay.Find("Card") : null;
        Transform simplePanel = simpleRay != null ? simpleRay.Find("Panel") : null;
        if (simpleCard == null
            || simplePanel == null
            || !simpleCard.gameObject.activeSelf
            || !simplePanel.gameObject.activeSelf)
        {
            failures.Add(
                "Controller Simp 1_Ray must preserve its authored active Card and Panel children.");
        }

        ScenarioDetailModal modal = serializedDirector.FindProperty("m_ScenarioDetailModal")
            ?.objectReferenceValue as ScenarioDetailModal;
        if (modal == null)
        {
            failures.Add("PPEVoiceFlowDirector has no authored ScenarioDetailModal for the direct PPE training flow.");
            return;
        }

        SerializedObject serializedModal = new(modal);
        if (serializedModal.FindProperty("openWithTrainingChoices")?.boolValue != true)
            failures.Add("ScenarioDetailModal does not open directly on the PPE/safety training choices.");

        GameObject modalPanel = ValidateObjectReference(
            serializedModal, "modalPanel", "Modal Panel_1", true, failures);
        GameObject educationChoiceRoot = ValidateObjectReference(
            serializedModal, "educationChoiceRoot", "1_EduChoice", true, failures);
        GameObject ppeModeChoiceRoot = ValidateObjectReference(
            serializedModal, "ppeModeChoiceRoot", "2_Mode", false, failures);

        if (modalPanel != null && educationChoiceRoot != null &&
            educationChoiceRoot.transform.parent != modalPanel.transform)
        {
            failures.Add("ScenarioDetailModal '1_EduChoice' is not authored directly under 'Modal Panel_1'.");
        }

        if (modalPanel != null && ppeModeChoiceRoot != null &&
            ppeModeChoiceRoot.transform.parent != modalPanel.transform)
        {
            failures.Add("ScenarioDetailModal '2_Mode' is not authored directly under 'Modal Panel_1'.");
        }

        ValidateComponentUnderRoot(serializedModal, "numberText", modalPanel, failures);
        ValidateComponentUnderRoot(serializedModal, "titleText", modalPanel, failures);
        ValidateComponentUnderRoot(serializedModal, "descriptionText", modalPanel, failures);
        ValidateComponentUnderRoot(serializedModal, "trainingButtonText", modalPanel, failures);
        ValidateObjectArrayUnderRoot(serializedModal, "scenarioTitleObjects", modalPanel, failures);
        ValidateObjectArrayUnderRoot(serializedModal, "scenarioDescriptionObjects", modalPanel, failures);

        ValidateButtonReference(serializedModal, "incompletePpeButton", "PPE Education Scenario Button", true, educationChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "standardTrainingButton", "Safety Training Scenario Button", true, educationChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "trainingChoiceBackButton", "Training Choice Back Button", true, educationChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "trainingButton", "Training Select Button", false, modalPanel, failures);
        ValidateButtonReference(serializedModal, "backButton", "Back Button", false, modalPanel, failures);
        ValidateButtonReference(serializedModal, "ppeEducationModeButton", "PPE Edu Mode", true, ppeModeChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "ppeTrainingModeButton", "PPE Training Mode", true, ppeModeChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "ppeTestModeButton", "PPE Test Mode", true, ppeModeChoiceRoot, failures);
        ValidateButtonReference(serializedModal, "ppeModeChoiceBackButton", "Mode Choice Back", true, ppeModeChoiceRoot, failures);
    }

    private static void ValidateFullSuitOrigPose(List<string> failures)
    {
        try
        {
            PPEFullSuitFloorGrounding.Validate();
        }
        catch (Exception exception)
        {
            failures.Add($"Full-suit _Orig pose validation failed: {exception.Message}");
        }
    }


    private static void ValidateGuideStep(
        SerializedProperty voiceSteps,
        string stepId,
        string[] expectedClipNames,
        string expectedVisualName,
        PPEVoiceFlowDirector.FlowState expectedNextState,
        List<string> failures)
    {
        SerializedProperty step = FindVoiceStep(voiceSteps, stepId);
        if (step == null)
        {
            failures.Add($"PPEVoiceFlowDirector is missing authored guide step '{stepId}'.");
            return;
        }

        SerializedProperty clips = step.FindPropertyRelative("clips");
        SerializedProperty visuals = step.FindPropertyRelative("controllerGuideVisuals");
        if (clips == null || clips.arraySize != expectedClipNames.Length)
        {
            failures.Add($"Guide step '{stepId}' does not have {expectedClipNames.Length} authored voice clip(s).");
            return;
        }

        if (visuals == null || visuals.arraySize != expectedClipNames.Length)
        {
            failures.Add($"Guide step '{stepId}' does not have one '{expectedVisualName}' visual per clip.");
            return;
        }

        for (int i = 0; i < expectedClipNames.Length; i++)
        {
            AudioClip clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
            if (string.IsNullOrEmpty(expectedClipNames[i]))
            {
                if (clip != null)
                    failures.Add($"Guide step '{stepId}' clip {i} should remain unassigned for now.");
            }
            else if (clip == null || clip.name != expectedClipNames[i])
            {
                failures.Add($"Guide step '{stepId}' clip {i} is not '{expectedClipNames[i]}'.");
            }

            GameObject visual = visuals.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (visual == null || visual.name != expectedVisualName)
                failures.Add($"Guide step '{stepId}' visual {i} is not '{expectedVisualName}'.");
        }

        SerializedProperty nextState = step.FindPropertyRelative("nextState");
        if (nextState == null || nextState.enumValueIndex != (int)expectedNextState)
            failures.Add($"Guide step '{stepId}' does not continue to {expectedNextState}.");
    }

    private static void ValidateGuideCompanion(
        SerializedProperty voiceSteps,
        string stepId,
        string expectedVisualName,
        List<string> failures)
    {
        SerializedProperty step = FindVoiceStep(voiceSteps, stepId);
        GameObject companion = step?.FindPropertyRelative("controllerGuideCompanionVisual")
            ?.objectReferenceValue as GameObject;
        if (companion == null || companion.name != expectedVisualName)
        {
            failures.Add(
                $"Guide step '{stepId}' companion is not '{expectedVisualName}'.");
        }
    }

    private static SerializedProperty FindVoiceStep(SerializedProperty voiceSteps, string stepId)
    {
        if (voiceSteps == null || !voiceSteps.isArray)
            return null;

        for (int i = 0; i < voiceSteps.arraySize; i++)
        {
            SerializedProperty step = voiceSteps.GetArrayElementAtIndex(i);
            if (step.FindPropertyRelative("stepId")?.stringValue == stepId)
                return step;
        }

        return null;
    }

    private static void ValidateButtonReference(
        SerializedObject serializedModal,
        string propertyName,
        string expectedName,
        bool expectedActiveSelf,
        GameObject expectedParentRoot,
        List<string> failures)
    {
        Button button = serializedModal.FindProperty(propertyName)?.objectReferenceValue as Button;
        if (button == null || button.name != expectedName)
        {
            failures.Add($"ScenarioDetailModal '{propertyName}' is not authored as '{expectedName}'.");
            return;
        }

        if (button.gameObject.activeSelf != expectedActiveSelf)
        {
            failures.Add(
                $"ScenarioDetailModal button '{expectedName}' activeSelf is {button.gameObject.activeSelf}; " +
                $"expected {expectedActiveSelf} for the current direct PPE flow.");
        }

        if (expectedParentRoot != null && !button.transform.IsChildOf(expectedParentRoot.transform))
        {
            failures.Add(
                $"ScenarioDetailModal button '{expectedName}' is not authored under '{expectedParentRoot.name}'.");
        }
    }

    private static GameObject ValidateObjectReference(
        SerializedObject serializedModal,
        string propertyName,
        string expectedName,
        bool expectedActiveSelf,
        List<string> failures)
    {
        GameObject target = serializedModal.FindProperty(propertyName)?.objectReferenceValue as GameObject;
        if (target == null || target.name != expectedName)
        {
            failures.Add($"ScenarioDetailModal '{propertyName}' is not authored as '{expectedName}'.");
            return null;
        }

        if (target.activeSelf != expectedActiveSelf)
        {
            failures.Add(
                $"ScenarioDetailModal object '{expectedName}' activeSelf is {target.activeSelf}; " +
                $"expected {expectedActiveSelf} for the current direct PPE flow.");
        }

        return target;
    }

    private static void ValidateComponentUnderRoot(
        SerializedObject serializedModal,
        string propertyName,
        GameObject expectedRoot,
        List<string> failures)
    {
        Component component = serializedModal.FindProperty(propertyName)?.objectReferenceValue as Component;
        if (component == null || expectedRoot == null ||
            !component.transform.IsChildOf(expectedRoot.transform))
        {
            failures.Add(
                $"ScenarioDetailModal '{propertyName}' is not authored under 'Modal Panel_1'.");
        }
    }

    private static void ValidateObjectArrayUnderRoot(
        SerializedObject serializedModal,
        string propertyName,
        GameObject expectedRoot,
        List<string> failures)
    {
        SerializedProperty objects = serializedModal.FindProperty(propertyName);
        if (objects == null || !objects.isArray || objects.arraySize != 3 || expectedRoot == null)
        {
            failures.Add($"ScenarioDetailModal '{propertyName}' does not contain three authored Panel_1 objects.");
            return;
        }

        for (int i = 0; i < objects.arraySize; i++)
        {
            GameObject target = objects.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (target == null || !target.transform.IsChildOf(expectedRoot.transform))
            {
                failures.Add(
                    $"ScenarioDetailModal '{propertyName}' element {i} is not authored under 'Modal Panel_1'.");
            }
        }
    }

    private static void ValidateVoiceFlowTriggerSkip(Scene scene, List<string> failures)
    {
        PPEVoiceFlowDirector[] directors = FindComponents<PPEVoiceFlowDirector>(scene);
        if (directors.Length != 1)
        {
            failures.Add($"Expected one PPEVoiceFlowDirector, found {directors.Length}.");
            return;
        }

        SerializedObject serializedDirector = new(directors[0]);
        NearFarInteractor leftInteractor = serializedDirector
            .FindProperty("m_LeftVoiceSkipInteractor")?.objectReferenceValue as NearFarInteractor;
        NearFarInteractor rightInteractor = serializedDirector
            .FindProperty("m_RightVoiceSkipInteractor")?.objectReferenceValue as NearFarInteractor;

        if (leftInteractor == null || leftInteractor.name != LeftNearFarInteractorName)
            failures.Add("PPEVoiceFlowDirector has no authored left Near-Far Interactor for Trigger voice-skip UI exclusion.");
        if (rightInteractor == null || rightInteractor.name != RightNearFarInteractorName)
            failures.Add("PPEVoiceFlowDirector has no authored right Near-Far Interactor for Trigger voice-skip UI exclusion.");

        SerializedProperty controllerModels = serializedDirector.FindProperty("m_ControllerModelVisuals");
        if (controllerModels == null || controllerModels.arraySize != 2 ||
            controllerModels.GetArrayElementAtIndex(0).objectReferenceValue == null ||
            controllerModels.GetArrayElementAtIndex(1).objectReferenceValue == null)
        {
            failures.Add("PPEVoiceFlowDirector requires two authored controller model visuals for the guide-to-bare-hand transition.");
        }

        if (serializedDirector.FindProperty("m_EquipmentVisualController")?.objectReferenceValue == null)
            failures.Add("PPEVoiceFlowDirector has no PPEEquipmentVisualController for the controller-to-bare-hand transition.");
    }

    private static void ValidateControllerHandStartupRegression(
        Scene scene,
        List<string> failures)
    {
        MethodInfo startMethod = typeof(PPEVoiceFlowDirector).GetMethod(
            "Start",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo applyMethod = typeof(PPEVoiceFlowDirector).GetMethod(
            "ApplyControllerHeldVisuals",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (!MethodCalls(startMethod, applyMethod))
        {
            failures.Add(
                "PPEVoiceFlowDirector.Start must reassert controller-only startup visuals after every OnEnable initializer has run.");
        }

        PPEVoiceFlowDirector[] directors = FindComponents<PPEVoiceFlowDirector>(scene);
        if (directors.Length != 1)
            return;

        SerializedObject serializedDirector = new(directors[0]);
        SerializedProperty startOnEnable =
            serializedDirector.FindProperty("m_StartOnEnable");
        if (startOnEnable == null || !startOnEnable.boolValue)
        {
            failures.Add(
                "PPEVoiceFlowDirector must start on enable so controller-only startup presentation is applied.");
        }

        GameObject probeRoot = new("[PPE Controller-Hand Startup Probe]")
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        probeRoot.SetActive(false);
        try
        {
            PPEVoiceFlowDirector probeDirector =
                probeRoot.AddComponent<PPEVoiceFlowDirector>();
            PPEEquipmentVisualController probeEquipment =
                probeRoot.AddComponent<PPEEquipmentVisualController>();

            GameObject leftController = CreateHiddenProbe("Left Controller", probeRoot.transform);
            GameObject rightController = CreateHiddenProbe("Right Controller", probeRoot.transform);
            GameObject[] handModels =
            {
                CreateHiddenProbe("Bare L", probeRoot.transform),
                CreateHiddenProbe("Suit L", probeRoot.transform),
                CreateHiddenProbe("Glove L", probeRoot.transform),
                CreateHiddenProbe("Tape L", probeRoot.transform),
                CreateHiddenProbe("Bare R", probeRoot.transform),
                CreateHiddenProbe("Suit R", probeRoot.transform),
                CreateHiddenProbe("Glove R", probeRoot.transform),
                CreateHiddenProbe("Tape R", probeRoot.transform),
            };

            PPEHandModelSwap leftSwap = new();
            SetPrivateField(leftSwap, "bareHand", handModels[0]);
            SetPrivateField(leftSwap, "bareSuitHand", handModels[1]);
            SetPrivateField(leftSwap, "gloveHand", handModels[2]);
            SetPrivateField(leftSwap, "tapedHand", handModels[3]);
            PPEHandModelSwap rightSwap = new();
            SetPrivateField(rightSwap, "bareHand", handModels[4]);
            SetPrivateField(rightSwap, "bareSuitHand", handModels[5]);
            SetPrivateField(rightSwap, "gloveHand", handModels[6]);
            SetPrivateField(rightSwap, "tapedHand", handModels[7]);

            SetPrivateField(
                probeEquipment,
                "handModelSwaps",
                new[] { leftSwap, rightSwap });
            SetPrivateField(
                probeDirector,
                "m_ControllerModelVisuals",
                new[] { leftController.transform, rightController.transform });
            SetPrivateField(
                probeDirector,
                "m_EquipmentVisualController",
                probeEquipment);

            leftController.SetActive(false);
            rightController.SetActive(false);
            for (int index = 0; index < handModels.Length; index++)
                handModels[index].SetActive(index == 0 || index == 4);

            applyMethod.Invoke(probeDirector, new object[] { true });

            // Reproduce the real startup ordering regression: equipment OnEnable
            // turns a hand back on after the voice director already hid it.
            handModels[0].SetActive(true);
            handModels[7].SetActive(true);
            applyMethod.Invoke(probeDirector, new object[] { true });

            if (!leftController.activeSelf || !rightController.activeSelf)
                failures.Add("Controller-only startup did not activate both controller model visuals.");
            foreach (GameObject handModel in handModels)
            {
                if (handModel.activeSelf)
                {
                    failures.Add(
                        "Controller-only startup left a PPE hand model active alongside the controller model.");
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            failures.Add($"Controller/hand startup regression probe failed: {exception.Message}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probeRoot);
        }
    }

    private static void ValidateFinaleCompletionRequirements(
        Scene scene,
        List<string> failures)
    {
        PPEFinaleController[] finales = FindComponents<PPEFinaleController>(scene);
        if (finales.Length != 1)
            return;

        SerializedObject serializedFinale = new(finales[0]);
        if (serializedFinale.FindProperty("m_HazmatEquipController")?.objectReferenceValue == null ||
            serializedFinale.FindProperty("m_EquipmentVisualController")?.objectReferenceValue == null ||
            serializedFinale.FindProperty("m_TabletChecklistController")?.objectReferenceValue == null ||
            serializedFinale.FindProperty("m_VoiceFlowDirector")?.objectReferenceValue == null)
        {
            failures.Add(
                "PPEFinaleController requires authored hazmat, equipment, tablet checklist, and voice-flow references.");
        }

        MethodInfo completionMethod = typeof(PPEFinaleController).GetMethod(
            "HasCompletionRequirements",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo tabletCompletedGetter = typeof(PPETabletChecklistController)
            .GetProperty(nameof(PPETabletChecklistController.IsDocumentCompleted))
            ?.GetGetMethod();
        if (!MethodCalls(completionMethod, tabletCompletedGetter))
        {
            failures.Add(
                "PPE finale completion must require PPETabletChecklistController.IsDocumentCompleted before quiz/end narration.");
        }

        PPEVoiceFlowDirector[] directors = FindComponents<PPEVoiceFlowDirector>(scene);
        if (directors.Length != 1)
            return;

        SerializedObject serializedDirector = new(directors[0]);
        AudioClip completeVoice = serializedDirector.FindProperty("m_EduEndVoice")
            ?.objectReferenceValue as AudioClip;
        AudioClip incompleteVoice = serializedDirector.FindProperty("m_IncompletePpeVoice")
            ?.objectReferenceValue as AudioClip;
        if (completeVoice == null || completeVoice.name != "4_VO_PPE_EDU_015_EduEnd")
            failures.Add("PPE complete finale voice must remain 4_VO_PPE_EDU_015_EduEnd.");
        if (incompleteVoice == null || incompleteVoice.name != "4_VO_PPE_EDU_013_UnEnoughPpe")
            failures.Add("PPE incomplete finale voice must remain 4_VO_PPE_EDU_013_UnEnoughPpe.");
    }

    private static GameObject CreateHiddenProbe(string name, Transform parent)
    {
        GameObject probe = new(name)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        probe.transform.SetParent(parent, false);
        return probe;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException($"Missing regression field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static bool MethodCalls(MethodInfo caller, MethodInfo callee)
    {
        if (caller == null || callee == null)
            return false;

        byte[] il = caller.GetMethodBody()?.GetILAsByteArray();
        if (il == null || il.Length < 5)
            return false;

        int targetToken = callee.MetadataToken;
        for (int index = 0; index <= il.Length - 5; index++)
        {
            if ((il[index] == 0x28 || il[index] == 0x6f) &&
                BitConverter.ToInt32(il, index + 1) == targetToken)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidatePpeMarkerReachability(
        Scene scene,
        string interactorName,
        List<string> failures)
    {
        GameObject interactorObject = FindSceneObject(scene, interactorName);
        if (interactorObject == null)
        {
            failures.Add($"Missing '{interactorName}'.");
            return;
        }

        SphereInteractionCaster nearCaster = interactorObject.GetComponent<SphereInteractionCaster>();
        CurveInteractionCaster farCaster = interactorObject.GetComponent<CurveInteractionCaster>();
        if (nearCaster == null)
            failures.Add($"'{interactorName}' has no SphereInteractionCaster.");
        if (farCaster == null)
            failures.Add($"'{interactorName}' has no CurveInteractionCaster.");

        int activeMarkerCount = 0;
        foreach (GameObject marker in FindSceneObjects(scene, PpeMarkerName))
        {
            if (!marker.activeInHierarchy || marker.GetComponent<Collider>() == null)
                continue;

            activeMarkerCount++;
            int markerBit = 1 << marker.layer;
            if (nearCaster != null && !SerializedMaskContainsLayer(
                    nearCaster,
                    "m_PhysicsLayerMask",
                    markerBit))
            {
                failures.Add(
                    $"'{interactorName}' Near caster cannot reach active marker " +
                    $"'{GetPath(marker.transform)}' on physics layer {marker.layer}.");
            }

            if (farCaster != null && SerializedMaskContainsLayer(
                    farCaster,
                    "m_RaycastMask",
                    markerBit))
            {
                failures.Add(
                    $"'{interactorName}' Far caster can hit PPE marker " +
                    $"'{GetPath(marker.transform)}'; PPE markers must remain near-only.");
            }
        }

        if (activeMarkerCount == 0)
            failures.Add($"No active '{PpeMarkerName}' objects with Colliders were found.");
    }

    private static void ValidateBothHandInspection(Scene scene, List<string> failures)
    {
        int activeStateCount = 0;
        foreach (PPEInspectionState state in FindComponents<PPEInspectionState>(scene))
        {
            if (!state.isActiveAndEnabled)
                continue;

            activeStateCount++;
            SerializedProperty rightOnly = new SerializedObject(state)
                .FindProperty("rightHandGrabOnly");
            if (rightOnly == null || rightOnly.boolValue)
                failures.Add($"'{GetPath(state.transform)}' still restricts PPE grab to the right hand.");
        }

        if (activeStateCount == 0)
            failures.Add("No active PPEInspectionState components were found.");
    }

    private static void ValidateTeleportManagers(Scene scene, List<string> failures)
    {
        PPEControllerTeleportModeManager[] managers =
            FindComponents<PPEControllerTeleportModeManager>(scene);
        if (managers.Length != 2)
            failures.Add($"Expected two controller teleport managers, found {managers.Length}.");

        foreach (PPEControllerTeleportModeManager manager in managers)
        {
            SerializedObject serializedManager = new(manager);
            SerializedProperty requireScenario = serializedManager
                .FindProperty("m_RequireScenarioSelection");
            if (requireScenario == null || requireScenario.boolValue)
            {
                failures.Add(
                    $"'{GetPath(manager.transform)}' still requires the scenario selection skipped by the PpeArea test entry.");
            }

            SerializedProperty teleportCancel = serializedManager
                .FindProperty("m_TeleportModeCancel");
            if (teleportCancel?.objectReferenceValue != null)
            {
                failures.Add(
                    $"'{GetPath(manager.transform)}' still consumes Grip as Teleport Mode Cancel; Grip must remain PPE Select only in this test scene.");
            }
        }
    }

    private static void ValidateMiniGuideActions(Scene scene, List<string> failures)
    {
        ControllerGuideMiniActivator[] activators =
            FindComponents<ControllerGuideMiniActivator>(scene);
        if (activators.Length != 1)
        {
            failures.Add($"Expected one ControllerGuideMiniActivator, found {activators.Length}.");
            return;
        }

        SerializedObject serializedActivator = new(activators[0]);
        InputActionAsset actionAsset = serializedActivator.FindProperty("m_XriInputActions")
            ?.objectReferenceValue as InputActionAsset;
        if (actionAsset == null)
        {
            failures.Add("ControllerGuideMiniActivator has no authored XRI InputActionAsset.");
            return;
        }

        if (actionAsset.FindAction("XRI Left Interaction/Scale Toggle", false) == null)
            failures.Add("Authored XRI input asset is missing the left Scale Toggle action.");
        if (actionAsset.FindAction("XRI Right Interaction/Scale Toggle", false) == null)
            failures.Add("Authored XRI input asset is missing the right Scale Toggle action.");

        GameObject miniGuide = serializedActivator.FindProperty("m_ControllerGuideMini")
            ?.objectReferenceValue as GameObject;
        if (miniGuide == null)
        {
            failures.Add("ControllerGuideMiniActivator has no authored ControllerGuide_mini reference.");
            return;
        }

        Canvas miniCanvas = miniGuide.GetComponent<Canvas>();
        Camera parentCamera = miniGuide.transform.parent != null
            ? miniGuide.transform.parent.GetComponent<Camera>()
            : null;
        if (parentCamera == null)
            failures.Add("ControllerGuide_mini must be parented to the authored XR camera so it remains view-fixed.");
        if (miniCanvas == null || miniCanvas.renderMode != RenderMode.WorldSpace)
            failures.Add("ControllerGuide_mini requires its own authored World Space Canvas.");
        else if (parentCamera != null && miniCanvas.worldCamera != parentCamera)
            failures.Add("ControllerGuide_mini World Space Canvas is not assigned to its parent XR camera.");

        Vector3 cameraLocalPosition = miniGuide.transform.localPosition;
        if (cameraLocalPosition.x >= 0f || cameraLocalPosition.z <= 0f)
            failures.Add("ControllerGuide_mini must remain authored in front of and to the left of the XR camera.");
        if (miniGuide.activeSelf)
            failures.Add("ControllerGuide_mini must start inactive and only toggle from the authored thumbstick click.");
    }

    private static void ValidateKeyboardControllerEducationEntry(Scene scene, List<string> failures)
    {
        GameObject entry = FindSceneObject(scene, "Controller Education Entry (Visual Only)");
        if (entry == null)
        {
            failures.Add("The visual-only A controller-education entry beside Enter is missing.");
            return;
        }

        XRKeyboardKey key = entry.GetComponent<XRKeyboardKey>();
        if (key == null || key.enabled)
            failures.Add("The controller-education A entry must keep its XRKeyboardKey disabled (visual only).");
        if (entry.transform.parent != null && entry.transform.parent.name == "KeyboardLayout")
            failures.Add("The controller-education A entry must remain outside KeyboardLayout.");

        foreach (Collider collider in entry.GetComponentsInChildren<Collider>(true))
        {
            if (collider.enabled)
            {
                failures.Add("The visual-only controller-education A entry still has an enabled Collider.");
                break;
            }
        }
    }

    private static void ValidateMirrorGaugeHierarchy(Scene scene, List<string> failures)
    {
        PPEFinaleController[] controllers = FindComponents<PPEFinaleController>(scene);
        if (controllers.Length != 1)
        {
            failures.Add($"Expected one PPEFinaleController, found {controllers.Length}.");
            return;
        }

        SerializedObject serializedController = new(controllers[0]);
        GameObject gaugeRoot = serializedController.FindProperty("m_ObservationGaugeRoot")
            ?.objectReferenceValue as GameObject;
        Image gaugeFill = serializedController.FindProperty("m_ObservationGaugeFill")
            ?.objectReferenceValue as Image;
        if (gaugeRoot == null || gaugeFill == null)
        {
            failures.Add("PPEFinaleController is missing its authored mirror gauge root or fill reference.");
            return;
        }

        Canvas gaugeCanvas = gaugeRoot.GetComponentInParent<Canvas>(true);
        if (gaugeCanvas == null || !gaugeCanvas.gameObject.activeSelf)
            failures.Add("The authored PPE Mirror Gauge Canvas must stay active so the finale can reveal its child gauge.");
        if (gaugeRoot.activeSelf)
            failures.Add("Mirror Observation Gauge must start inactive until mirror observation begins.");
        if (!gaugeFill.transform.IsChildOf(gaugeRoot.transform))
            failures.Add("The authored mirror gauge fill must remain under Mirror Observation Gauge.");
    }

    private static bool SerializedMaskContainsLayer(
        UnityEngine.Object target,
        string propertyName,
        int layerBit)
    {
        SerializedObject serializedTarget = new(target);
        SerializedProperty maskProperty = serializedTarget.FindProperty(propertyName);
        SerializedProperty bitsProperty = maskProperty?.FindPropertyRelative("m_Bits");
        return bitsProperty != null && (bitsProperty.intValue & layerBit) != 0;
    }

    private static void ValidateCanvas(GameObject canvasObject, List<string> failures)
    {
        GraphicRaycaster mouseRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (mouseRaycaster != null && mouseRaycaster.enabled)
            failures.Add($"'{CanvasName}' still has an enabled mouse GraphicRaycaster.");

        TrackedDeviceGraphicRaycaster trackedRaycaster =
            canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (trackedRaycaster == null || !trackedRaycaster.enabled)
            failures.Add($"'{CanvasName}' is missing an enabled TrackedDeviceGraphicRaycaster.");

        ScenarioDetailModal modal = canvasObject.GetComponent<ScenarioDetailModal>();
        if (modal != null)
        {
            SerializedObject serializedModal = new(modal);
            if (serializedModal.FindProperty("enableMousePhysicsFallback").boolValue)
                failures.Add($"'{CanvasName}' still enables the mouse physics fallback.");
        }

        ScenarioCardSelectProxy[] proxies =
            canvasObject.GetComponentsInChildren<ScenarioCardSelectProxy>(true);
        if (proxies.Length == 0)
            failures.Add($"'{CanvasName}' has no ScenarioCardSelectProxy components.");

        foreach (ScenarioCardSelectProxy proxy in proxies)
        {
            SerializedObject serializedProxy = new(proxy);
            GameObject hideTarget = serializedProxy.FindProperty("hideAfterSelection")
                .objectReferenceValue as GameObject;
            if (hideTarget != canvasObject)
                failures.Add($"'{GetPath(proxy.transform)}' does not hide '{CanvasName}' after selection.");

            if (proxy.TryGetComponent(out XRSimpleInteractable _))
                failures.Add($"'{GetPath(proxy.transform)}' still has an XRSimpleInteractable bypass.");
        }
    }

    private static void ValidateControllerRays(Scene scene, List<string> failures)
    {
        XRNearFarReticleVisual[] visuals = FindComponents<XRNearFarReticleVisual>(scene);
        if (visuals.Length < 2)
            failures.Add("Expected left and right XRNearFarReticleVisual components.");

        foreach (XRNearFarReticleVisual visual in visuals)
        {
            SerializedObject serializedVisual = new(visual);
            float distance = serializedVisual.FindProperty("rayDistance").floatValue;
            bool extends = serializedVisual.FindProperty("extendRayToEmptyHit").boolValue;
            if (distance < RequiredRayDistance)
                failures.Add($"'{GetPath(visual.transform)}' ray distance is only {distance:0.##} m.");
            if (!extends)
                failures.Add($"'{GetPath(visual.transform)}' retracts to a short resting line on an empty hit.");
        }

        foreach (NearFarInteractor interactor in FindComponents<NearFarInteractor>(scene))
        {
            if (!interactor.gameObject.activeInHierarchy
                || (interactor.name != "Left_NearFarInteractor" && interactor.name != "Right_NearFarInteractor"))
            {
                continue;
            }

            SerializedObject serializedInteractor = new(interactor);
            UnityEngine.Object farCasterObject = serializedInteractor.FindProperty("m_FarInteractionCaster")?.objectReferenceValue;
            if (farCasterObject is not CurveInteractionCaster)
            {
                failures.Add($"'{GetPath(interactor.transform)}' has no far CurveInteractionCaster.");
                continue;
            }

            SerializedObject serializedCaster = new(farCasterObject);
            float castDistance = serializedCaster.FindProperty("m_CastDistance")?.floatValue ?? 0f;
            if (castDistance < RequiredRayDistance)
                failures.Add($"'{GetPath(interactor.transform)}' far cast distance is only {castDistance:0.##} m.");
        }
    }

    private static void ValidateLocationMarker(Scene scene, string markerName, List<string> failures)
    {
        GameObject marker = FindSceneObject(scene, markerName);
        if (marker == null || !marker.activeSelf)
        {
            failures.Add($"Missing active '{markerName}'.");
            return;
        }

        Collider collider = marker.GetComponent<Collider>();
        if (collider == null || !collider.enabled)
            failures.Add($"'{markerName}' has no enabled Collider for the ray.");
        if (!marker.TryGetComponent(out XRLocationTeleportTarget _))
            failures.Add($"'{markerName}' has no XRLocationTeleportTarget.");
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }

    private static IEnumerable<GameObject> FindSceneObjects(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    yield return child.gameObject;
            }
        }
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            matches.AddRange(root.GetComponentsInChildren<T>(true));
        return matches.ToArray();
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
