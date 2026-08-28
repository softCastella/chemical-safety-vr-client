using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

public static class PPETrainTestModeValidationHarness
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test.unity";
    private const string GloveNarrationScenePath =
        "Assets/Scenes/3_PPE_Room_Train_Test_1.unity";

    private static readonly string[] TrainingVoiceFields =
    {
        "m_TrainingModeSelectedVoice",
        "m_TrainingMoveToPpeVoice",
        "m_TrainingCheckPpeTabletVoice",
        "m_TrainingWrongButtonVoice",
        "m_TrainingMirrorCheckVoice",
        "m_TrainingIncompletePpeVoice",
        "m_TrainingQuizVoice",
        "m_TrainingEndVoice",
    };

    private static readonly string[] TestVoiceFields =
    {
        "m_TestModeSelectedVoice",
        "m_TestMoveToPpeVoice",
        "m_TestEndVoice",
    };

    [MenuItem("Tools/PPE/Validate Train Test Modes")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "PPE train/test validation cannot run during Play Mode. Stop Play Mode before validating the authored scene.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE train/test validation was cancelled.");
            return;
        }

        ValidateScene();
    }

    [MenuItem("Tools/PPE/Validate Glove Grab Narration Once (_1)")]
    public static void ValidateGloveGrabNarrationOnce()
    {
        Scene previewScene = default;
        List<string> failures = new();

        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(GloveNarrationScenePath);
            PPEVoiceFlowDirector director = FindSingleInScene<PPEVoiceFlowDirector>(
                previewScene,
                failures,
                "glove narration work scene");
            if (director != null)
                ValidateGloveGrabNarrationOnce(director, failures);
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }

        if (failures.Count > 0)
        {
            string message = "PPE glove grab narration validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Glove Narration Validation] PASS '{GloveNarrationScenePath}': " +
            "the work scene opts into one shared education narration per mode session.");
    }

    [MenuItem("Tools/PPE/Validate PPE Requirement Policies (_1)")]
    public static void ValidatePpeRequirementPolicies()
    {
        Scene previewScene = default;
        List<string> failures = new();

        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(GloveNarrationScenePath);
            PPEVoiceFlowDirector director = FindSingleInScene<PPEVoiceFlowDirector>(
                previewScene,
                failures,
                "PPE requirement work scene");
            PPEFinaleController finale = FindSingleInScene<PPEFinaleController>(
                previewScene,
                failures,
                "PPE requirement work scene");
            List<PPEActionPanelController> panels = FindAllInScene<PPEActionPanelController>(
                previewScene);

            if (director != null)
                ValidatePpeRequirementScene(director, panels, failures);
            if (finale == null)
                failures.Add("The _1 work scene is missing PPEFinaleController.");
            ValidatePpeRequirementRouting(failures);
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }

        if (failures.Count > 0)
        {
            string message = "PPE requirement policy validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Requirement Policy Validation] PASS '{GloveNarrationScenePath}': " +
            "hazmat-first glove/boot use, mask inspection, panel feedback, helmet narration, " +
            "and Education-only split incomplete voices are configured.");
    }

    public static void ValidateBatch()
    {
        try
        {
            ValidateScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> failures = new();

        PPEVoiceFlowDirector director = FindSingle<PPEVoiceFlowDirector>(failures);
        PPEQuizController quiz = FindSingle<PPEQuizController>(failures);
        PPEFinaleController finale = FindSingle<PPEFinaleController>(failures);

        if (director != null)
            ValidateDirector(director, quiz, finale, failures);
        if (quiz != null)
            ValidateQuiz(quiz, failures);
        if (finale != null)
            ValidateFinale(finale, director, failures);
        ValidateMirrorRetryRouting(failures);
        ValidateCompletionRouting(failures);
        ValidateCardIntroOneShotRouting(failures);
        ValidateControllerEducation(director, failures);
        ValidateControllerGuidePresentationRuntime(failures);

        if (failures.Count > 0)
        {
            string message = "PPE train/test mode validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Train/Test Validation] PASS '{scene.path}': " +
            "11 mode clips, authored mode buttons, quiz/result references, " +
            "inactive Test-only Result Canvas, XR raycaster, mirror retry, " +
            "completion return routing, one-shot card intro, controller education entry, detailed-completion skip, " +
            "and detailed/Simple paired guide presentation are valid.");
    }

    private static void ValidateGloveGrabNarrationOnce(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type directorType = typeof(PPEVoiceFlowDirector);
        FieldInfo optionField = directorType.GetField(
            "m_PlayGloveGrabVoiceOncePerSession",
            flags);
        FieldInfo playedField = directorType.GetField("m_GloveGrabVoicePlayed", flags);
        PropertyInfo modeProperty = directorType.GetProperty(
            nameof(PPEVoiceFlowDirector.ActiveLearningMode),
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo consumeMethod = directorType.GetMethod("TryConsumeGloveGrabVoice", flags);
        MethodInfo resetMethod = directorType.GetMethod("ResetModeSessionTracking", flags);

        if (optionField == null || playedField == null || modeProperty == null ||
            consumeMethod == null || resetMethod == null)
        {
            failures.Add("Glove narration one-shot members could not be inspected.");
            return;
        }

        if (!(bool)optionField.GetValue(director))
            failures.Add("The _1 work scene must enable shared one-shot glove narration.");

        optionField.SetValue(director, true);
        modeProperty.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
        playedField.SetValue(director, false);

        bool firstCleanGrab = (bool)consumeMethod.Invoke(director, new object[] { true });
        bool secondCleanGrab = (bool)consumeMethod.Invoke(director, new object[] { true });
        resetMethod.Invoke(director, null);
        bool contaminatedGrab = (bool)consumeMethod.Invoke(director, new object[] { false });
        if (!firstCleanGrab || secondCleanGrab || contaminatedGrab)
        {
            failures.Add(
                "Education glove narration must accept the first clean grab only and reject later or contaminated grabs.");
        }

        resetMethod.Invoke(director, null);
        bool nextSessionFirstGrab = (bool)consumeMethod.Invoke(director, new object[] { true });
        if (!nextSessionFirstGrab)
            failures.Add("A new mode session must allow the first clean glove narration again.");

        resetMethod.Invoke(director, null);
        modeProperty.SetValue(director, ScenarioDetailModal.PpeLearningMode.Training);
        bool trainingGrab = (bool)consumeMethod.Invoke(director, new object[] { true });
        if (trainingGrab)
            failures.Add("Training mode must not consume or play the education glove narration.");
    }

    private static void ValidatePpeRequirementScene(
        PPEVoiceFlowDirector director,
        List<PPEActionPanelController> panels,
        List<string> failures)
    {
        SerializedObject directorSerialized = new(director);
        SerializedProperty enforceHazmat = directorSerialized.FindProperty(
            "m_EnforceHazmatBeforeGlovesAndBoots");
        if (enforceHazmat == null || !enforceHazmat.boolValue)
            failures.Add("The _1 work scene must require hazmat before glove/boot Use.");

        SerializedProperty separateEducationVoices = directorSerialized.FindProperty(
            "m_UseSeparateEducationIncompleteVoices");
        if (separateEducationVoices == null || !separateEducationVoices.boolValue)
            failures.Add("The _1 work scene must split incomplete voices in Education mode.");

        PPEActionPanelController helmet = RequireObject<PPEActionPanelController>(
            directorSerialized,
            "m_HelmetActionPanel",
            failures);
        if (helmet != null && (!helmet.enabled || helmet.ItemDisplayName != "안전모"))
            failures.Add("m_HelmetActionPanel must reference the active 안전모 action panel.");

        RequireClipName(
            directorSerialized,
            "m_HelmetGrabVoice",
            "4_VO_PPE_EDU_104_HowToHelmet",
            failures);
        RequireClipName(
            directorSerialized,
            "m_IncompleteTabletVoice",
            "4_VO_PPE_EDU_016_CheckTablet",
            failures);
        RequireClipName(
            directorSerialized,
            "m_TrainingIncompletePpeVoice",
            "4_VO_PPE_TRAIN_005_UnEnoughPpeTablet",
            failures);

        if (panels.Count != 10)
            failures.Add($"Expected 10 PPE action panels in the _1 scene, found {panels.Count}.");

        PPEActionPanelController mask = null;
        foreach (PPEActionPanelController panel in panels)
        {
            SerializedObject panelSerialized = new(panel);
            RequireExactString(panelSerialized, "useApprovedMessage", "사용 처리 완료", failures);
            RequireExactString(
                panelSerialized,
                "contaminatedUseMessage",
                "PPE 정상 여부 확인 필요",
                failures);
            RequireExactString(
                panelSerialized,
                "contaminatedDiscardMessage",
                "폐기 처리 완료",
                failures);
            RequireExactString(
                panelSerialized,
                "cleanDiscardPendingMessage",
                "PPE 정상 여부 확인 필요",
                failures);

            string displayName = panel.ItemDisplayName;
            bool isMask = displayName == "송기마스크";
            bool isGloveOrBoot = displayName.Contains("장갑") || displayName.Contains("장화");
            if (isMask || isGloveOrBoot)
                RequireSameObject(panelSerialized, "voiceFlowDirector", director, failures);
            if (isMask)
                mask = panel;
        }

        if (mask == null)
        {
            failures.Add("The _1 work scene is missing the 송기마스크 action panel.");
            return;
        }

        SerializedObject maskSerialized = new(mask);
        SerializedProperty inspectEnabled = maskSerialized.FindProperty("enableInspectChoice");
        SerializedProperty inspectRequired = maskSerialized.FindProperty(
            "requireInspectBeforeUseOrDiscard");
        if (inspectEnabled == null || !inspectEnabled.boolValue)
            failures.Add("The mask Inspect choice must remain enabled.");
        if (inspectRequired == null || !inspectRequired.boolValue)
            failures.Add("The mask must require Inspect before Use or Discard.");
        RequireExactString(
            maskSerialized,
            "inspectRequiredMessage",
            "호흡 상태 확인 필요",
            failures);
    }

    private static void ValidatePpeRequirementRouting(List<string> failures)
    {
        string actionPanelSource = File.ReadAllText(
            "Assets/Scripts/PPEActionPanelController.cs");
        if (!actionPanelSource.Contains(
                "inspectionState.ConditionChanged += OnConditionChanged;"))
        {
            failures.Add("Mask Inspect confirmation is not reset from the condition-change signal.");
        }
        if (!actionPanelSource.Contains(
                "RejectChoiceUntilInspect(PPEActionChoice.Use)"))
        {
            failures.Add("Use does not enforce the mask Inspect prerequisite.");
        }
        if (!actionPanelSource.Contains(
                "RejectChoiceUntilInspect(PPEActionChoice.Discard)"))
        {
            failures.Add("Discard does not enforce the mask Inspect prerequisite.");
        }

        string directorSource = File.ReadAllText("Assets/Scripts/PPEVoiceFlowDirector.cs");
        int finaleResultStart = directorSource.IndexOf(
            "public void NotifyFinaleResult(bool ppeComplete, bool tabletComplete)",
            StringComparison.Ordinal);
        int quizStart = directorSource.IndexOf(
            "public void NotifyQuizStart()",
            finaleResultStart >= 0 ? finaleResultStart : 0,
            StringComparison.Ordinal);
        if (finaleResultStart < 0 || quizStart <= finaleResultStart)
        {
            failures.Add("The split PPE/tablet finale result method could not be inspected.");
        }
        else
        {
            string finaleResultBody = directorSource.Substring(
                finaleResultStart,
                quizStart - finaleResultStart);
            if (!finaleResultBody.Contains(
                    "ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education") ||
                !finaleResultBody.Contains("PlayPpeConditionalVoiceSequence(") ||
                !finaleResultBody.Contains(
                    "ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training") ||
                !finaleResultBody.Contains(
                    "PlayPpeConditionalVoice(m_TrainingIncompletePpeVoice)"))
            {
                failures.Add(
                    "Incomplete voice routing must split only Education and retain Training's combined clip.");
            }
        }

        string finaleSource = File.ReadAllText("Assets/Scripts/PPEFinaleController.cs");
        if (!finaleSource.Contains(
                "NotifyFinaleResult(ppeComplete, tabletComplete);"))
        {
            failures.Add("PPEFinaleController does not report PPE and tablet completion separately.");
        }
    }

    private static void ValidateDirector(
        PPEVoiceFlowDirector director,
        PPEQuizController quiz,
        PPEFinaleController finale,
        List<string> failures)
    {
        SerializedObject serialized = new(director);
        ScenarioDetailModal modal = RequireObject<ScenarioDetailModal>(
            serialized,
            "m_ScenarioDetailModal",
            failures);
        RequireSameObject(serialized, "m_QuizController", quiz, failures);
        RequireSameObject(serialized, "m_FinaleController", finale, failures);

        foreach (string propertyName in TrainingVoiceFields)
            RequireObject<AudioClip>(serialized, propertyName, failures);
        foreach (string propertyName in TestVoiceFields)
            RequireObject<AudioClip>(serialized, propertyName, failures);

        if (modal == null)
            return;

        SerializedObject modalSerialized = new(modal);
        RequireObject<GameObject>(modalSerialized, "modalRoot", failures);
        RequireObject<GameObject>(modalSerialized, "educationChoiceRoot", failures);
        RequireObject<GameObject>(modalSerialized, "ppeModeChoiceRoot", failures);
        RequireObject<Button>(modalSerialized, "ppeEducationModeButton", failures);
        RequireObject<Button>(modalSerialized, "ppeTrainingModeButton", failures);
        RequireObject<Button>(modalSerialized, "ppeTestModeButton", failures);
    }

    private static void ValidateQuiz(PPEQuizController quiz, List<string> failures)
    {
        SerializedObject serialized = new(quiz);
        RequireObject<GameObject>(serialized, "quizRoot", failures);
        RequireObject<Component>(serialized, "quizTitleLabel", failures);
        GameObject resultRoot = RequireObject<GameObject>(serialized, "resultRoot", failures);
        RequireObject<Component>(serialized, "resultTitleLabel", failures);
        RequireObject<Component>(serialized, "clearMinuteLabel", failures);
        RequireObject<Component>(serialized, "clearSecondLabel", failures);
        RequireObject<Component>(serialized, "scoreLabel", failures);
        RequireObject<Component>(serialized, "correctCountLabel", failures);
        RequireObject<Component>(serialized, "ppeWrongCountLabel", failures);
        Button backButton = RequireObject<Button>(serialized, "resultBackButton", failures);
        RequireNonEmptyString(serialized, "educationQuizTitle", failures);
        RequireNonEmptyString(serialized, "trainingQuizTitle", failures);
        RequireNonEmptyString(serialized, "testQuizTitle", failures);
        RequireNonEmptyString(serialized, "trainingWrongFeedbackMessage", failures);

        SerializedProperty pages = serialized.FindProperty("pages");
        if (pages == null || !pages.isArray || pages.arraySize != 5)
        {
            failures.Add("PPEQuizController.pages must contain exactly five authored pages.");
        }
        else
        {
            for (int pageIndex = 0; pageIndex < pages.arraySize; pageIndex++)
                ValidateQuizPage(pages.GetArrayElementAtIndex(pageIndex), pageIndex, failures);
        }

        ValidateQuestionBank(
            serialized.FindProperty("trainingQuestions"),
            "trainingQuestions",
            3,
            new[] { 2, 1, 2, 1, 2 },
            requireFeedback: true,
            failures);
        ValidateQuestionBank(
            serialized.FindProperty("testQuestions"),
            "testQuestions",
            4,
            new[] { 2, 3, 3, 3, 2 },
            requireFeedback: false,
            failures);

        SerializedProperty metricRoots = serialized.FindProperty("resultMetricRoots");
        if (metricRoots == null || !metricRoots.isArray || metricRoots.arraySize != 4)
        {
            failures.Add("PPEQuizController.resultMetricRoots must contain four authored metric roots.");
        }
        else
        {
            for (int index = 0; index < metricRoots.arraySize; index++)
            {
                if (metricRoots.GetArrayElementAtIndex(index).objectReferenceValue == null)
                    failures.Add($"PPEQuizController.resultMetricRoots[{index}] is unassigned.");
            }
        }

        if (resultRoot == null)
            return;

        if (resultRoot.activeSelf)
            failures.Add("Result Canvas must be authored inactive before Play Mode.");
        if (resultRoot.GetComponent<Canvas>() == null)
            failures.Add("Result Canvas has no Canvas component.");
        if (resultRoot.GetComponent("TrackedDeviceGraphicRaycaster") == null)
            failures.Add("Result Canvas has no TrackedDeviceGraphicRaycaster.");
        if (backButton != null && !backButton.transform.IsChildOf(resultRoot.transform))
            failures.Add("Result Back Button is not a child of Result Canvas.");

        RectTransform rect = resultRoot.GetComponent<RectTransform>();
        if (rect == null)
        {
            failures.Add("Result Canvas has no RectTransform.");
            return;
        }

        RequireApproximately(rect.anchoredPosition3D, new Vector3(-1.567f, 1.43f, 10.024f),
            "Result Canvas anchoredPosition3D", failures);
        RequireApproximately(rect.localScale, Vector3.one * 0.0015f,
            "Result Canvas localScale", failures);
        RequireApproximately(rect.sizeDelta, new Vector2(1200f, 600f),
            "Result Canvas sizeDelta", failures);
    }

    private static void ValidateQuizPage(
        SerializedProperty page,
        int pageIndex,
        List<string> failures)
    {
        if (page.FindPropertyRelative("root")?.objectReferenceValue == null)
            failures.Add($"PPEQuizController.pages[{pageIndex}].root is unassigned.");
        if (page.FindPropertyRelative("questionLabel")?.objectReferenceValue == null)
            failures.Add($"PPEQuizController.pages[{pageIndex}].questionLabel is unassigned.");

        SerializedProperty buttons = page.FindPropertyRelative("optionButtons");
        SerializedProperty labels = page.FindPropertyRelative("optionLabels");
        if (buttons == null || !buttons.isArray || buttons.arraySize != 4)
            failures.Add($"PPEQuizController.pages[{pageIndex}] must have four authored option buttons.");
        if (labels == null || !labels.isArray || labels.arraySize != 4)
            failures.Add($"PPEQuizController.pages[{pageIndex}] must have four authored option labels.");

        if (buttons == null || labels == null || buttons.arraySize != 4 || labels.arraySize != 4)
            return;

        for (int optionIndex = 0; optionIndex < 4; optionIndex++)
        {
            if (buttons.GetArrayElementAtIndex(optionIndex).objectReferenceValue == null)
                failures.Add($"pages[{pageIndex}].optionButtons[{optionIndex}] is unassigned.");
            if (labels.GetArrayElementAtIndex(optionIndex).objectReferenceValue == null)
                failures.Add($"pages[{pageIndex}].optionLabels[{optionIndex}] is unassigned.");
        }

        Button fourth = buttons.GetArrayElementAtIndex(3).objectReferenceValue as Button;
        if (fourth != null && fourth.gameObject.activeSelf)
            failures.Add($"pages[{pageIndex}] Test-only option 4 must be authored inactive.");
    }

    private static void ValidateQuestionBank(
        SerializedProperty bank,
        string label,
        int expectedOptionCount,
        int[] expectedAnswers,
        bool requireFeedback,
        List<string> failures)
    {
        if (bank == null || !bank.isArray || bank.arraySize != 5)
        {
            failures.Add($"PPEQuizController.{label} must contain exactly five questions.");
            return;
        }

        for (int questionIndex = 0; questionIndex < bank.arraySize; questionIndex++)
        {
            SerializedProperty question = bank.GetArrayElementAtIndex(questionIndex);
            if (string.IsNullOrWhiteSpace(question.FindPropertyRelative("prompt")?.stringValue))
                failures.Add($"{label}[{questionIndex}].prompt is empty.");

            SerializedProperty options = question.FindPropertyRelative("options");
            if (options == null || !options.isArray || options.arraySize != expectedOptionCount)
            {
                failures.Add(
                    $"{label}[{questionIndex}] must contain {expectedOptionCount} options.");
            }
            else
            {
                for (int optionIndex = 0; optionIndex < options.arraySize; optionIndex++)
                    if (string.IsNullOrWhiteSpace(options.GetArrayElementAtIndex(optionIndex).stringValue))
                        failures.Add($"{label}[{questionIndex}].options[{optionIndex}] is empty.");
            }

            int answer = question.FindPropertyRelative("correctOption")?.intValue ?? -1;
            if (answer != expectedAnswers[questionIndex])
                failures.Add(
                    $"{label}[{questionIndex}].correctOption must be {expectedAnswers[questionIndex]}, actual {answer}.");
            if (requireFeedback &&
                string.IsNullOrWhiteSpace(question.FindPropertyRelative("feedback")?.stringValue))
            {
                failures.Add($"{label}[{questionIndex}].feedback is empty.");
            }
        }
    }

    private static void ValidateFinale(
        PPEFinaleController finale,
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        SerializedObject serialized = new(finale);
        RequireSameObject(serialized, "m_VoiceFlowDirector", director, failures);
        RequireObject<Component>(serialized, "m_TeleportationProvider", failures);
        RequireObject<Transform>(serialized, "m_ReturnDestination", failures);
        SerializedProperty fadeDuration = serialized.FindProperty("m_FadeDuration");
        if (fadeDuration == null || Mathf.Abs(fadeDuration.floatValue - 0.75f) > 0.0001f)
            failures.Add("PPEFinaleController.m_FadeDuration must match the original 0.75-second education return fade.");
    }

    private static void ValidateMirrorRetryRouting(List<string> failures)
    {
        const string directorPath = "Assets/Scripts/PPEVoiceFlowDirector.cs";
        string source = File.ReadAllText(directorPath);
        int methodStart = source.IndexOf(
            "public void NotifyMirrorMarkerArrived()",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "public void NotifyFinaleResult",
            methodStart >= 0 ? methodStart : 0,
            StringComparison.Ordinal);
        if (methodStart < 0 || methodEnd <= methodStart)
        {
            failures.Add("PPEVoiceFlowDirector mirror-arrival method could not be inspected.");
            return;
        }

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);
        if (!methodBody.Contains("m_FinaleController?.NotifyMirrorMarkerArrived();"))
            failures.Add("Mirror arrival is not forwarded to PPEFinaleController.");
        if (methodBody.Contains("return;"))
        {
            failures.Add(
                "Mirror arrival contains an early return that can block retry after an incomplete check.");
        }
    }

    private static void ValidateCompletionRouting(List<string> failures)
    {
        const string finalePath = "Assets/Scripts/PPEFinaleController.cs";
        string source = File.ReadAllText(finalePath);
        int methodStart = source.IndexOf(
            "private IEnumerator FinishAfterQuizVoice()",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "public void NotifyCompletionBackRequested()",
            methodStart >= 0 ? methodStart : 0,
            StringComparison.Ordinal);
        if (methodStart < 0 || methodEnd <= methodStart)
        {
            failures.Add("PPEFinaleController completion method could not be inspected.");
            return;
        }

        string methodBody = source.Substring(methodStart, methodEnd - methodStart);
        if (!methodBody.Contains(
                "ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Test"))
        {
            failures.Add("Result Canvas routing is not restricted to Test mode.");
        }
        if (!methodBody.Contains("ShowTestResultAfterVoice();"))
            failures.Add("Test completion does not open the authored Result Canvas.");
        if (!methodBody.Contains("yield return ReturnToModeChoices();"))
        {
            failures.Add(
                "Education/Training completion does not automatically return to mode choices.");
        }

        int returnMethodStart = source.IndexOf(
            "private IEnumerator ReturnToModeChoices()",
            StringComparison.Ordinal);
        int returnMethodEnd = source.IndexOf(
            "private bool HasCompletionRequirements()",
            returnMethodStart >= 0 ? returnMethodStart : 0,
            StringComparison.Ordinal);
        if (returnMethodStart < 0 || returnMethodEnd <= returnMethodStart)
        {
            failures.Add("PPEFinaleController return method could not be inspected.");
            return;
        }

        string returnMethodBody = source.Substring(
            returnMethodStart,
            returnMethodEnd - returnMethodStart);
        if (!returnMethodBody.Contains("yield return FadeTo(1f);"))
            failures.Add("Completion return does not fade out before moving.");
        if (!returnMethodBody.Contains("yield return FadeTo(0f);"))
            failures.Add("Completion return does not restore the original education fade-in after moving.");
        if (returnMethodBody.Contains("SetFadeAlpha(0f);"))
            failures.Add("Completion return still reveals mode choices immediately instead of fading in.");

        int fadeOutIndex = returnMethodBody.IndexOf("yield return FadeTo(1f);", StringComparison.Ordinal);
        int returnIndex = returnMethodBody.IndexOf("ReturnToStart();", StringComparison.Ordinal);
        int fadeInIndex = returnMethodBody.IndexOf("yield return FadeTo(0f);", StringComparison.Ordinal);
        int modalIndex = returnMethodBody.IndexOf(
            "ShowModeChoicesAfterCompletionReturn();",
            StringComparison.Ordinal);
        if (fadeOutIndex < 0 || returnIndex <= fadeOutIndex || fadeInIndex <= returnIndex ||
            modalIndex <= fadeInIndex)
        {
            failures.Add(
                "Completion return order must be fade-out, dark return move, fade-in, then mode modal.");
        }

        const string directorPath = "Assets/Scripts/PPEVoiceFlowDirector.cs";
        string directorSource = File.ReadAllText(directorPath);
        int legacyReturnStart = directorSource.IndexOf(
            "public void ShowScenarioCardsAfterCompletionReturn()",
            StringComparison.Ordinal);
        int legacyReturnEnd = directorSource.IndexOf(
            "public PPEHazmatEquipController HazmatEquipController",
            legacyReturnStart >= 0 ? legacyReturnStart : 0,
            StringComparison.Ordinal);
        if (legacyReturnStart < 0 || legacyReturnEnd <= legacyReturnStart)
        {
            failures.Add("Legacy scenario completion entry point could not be inspected.");
            return;
        }

        string legacyReturnBody = directorSource.Substring(
            legacyReturnStart,
            legacyReturnEnd - legacyReturnStart);
        if (!legacyReturnBody.Contains("ShowModeChoicesAfterCompletionReturn();") ||
            legacyReturnBody.Contains("SetActive(m_ScenarioSelectionRoot, true)") ||
            legacyReturnBody.Contains("m_ScenarioDetailModal?.Hide()"))
        {
            failures.Add(
                "A legacy normal-completion path can still reopen scenario cards instead of mode choices.");
        }
    }

    private static void ValidateCardIntroOneShotRouting(List<string> failures)
    {
        const string directorPath = "Assets/Scripts/PPEVoiceFlowDirector.cs";
        string source = File.ReadAllText(directorPath);

        if (!source.Contains("private bool m_CardIntroVoicePlayedThisRun;"))
            failures.Add("CardIntro has no application-run one-shot narration state.");

        int enterStateStart = source.IndexOf(
            "private void EnterState(",
            StringComparison.Ordinal);
        int enterStateEnd = source.IndexOf(
            "private IEnumerator PlayStepRoutine(",
            enterStateStart >= 0 ? enterStateStart : 0,
            StringComparison.Ordinal);
        if (enterStateStart < 0 || enterStateEnd <= enterStateStart)
        {
            failures.Add("CardIntro entry routing could not be inspected.");
            return;
        }

        string enterStateBody = source.Substring(enterStateStart, enterStateEnd - enterStateStart);
        if (!enterStateBody.Contains("if (m_CardIntroVoicePlayedThisRun)") ||
            !enterStateBody.Contains("m_CardIntroVoicePlayedThisRun = true;"))
        {
            failures.Add("CardIntro narration is not consumed on its first application-run entry.");
        }

        int playStepStart = enterStateEnd;
        int playStepEnd = source.IndexOf(
            "private AudioClip ResolveMetaWelcomeClip(",
            playStepStart,
            StringComparison.Ordinal);
        if (playStepEnd <= playStepStart)
        {
            failures.Add("CardIntro playback completion routing could not be inspected.");
            return;
        }

        string playStepBody = source.Substring(playStepStart, playStepEnd - playStepStart);
        if (!playStepBody.Contains("if (step.state == FlowState.CardIntro)") ||
            playStepBody.Contains(
                "step.waitForSignal || step.state == FlowState.CardIntro"))
        {
            failures.Add("CardIntro can still enter repeat playback after its startup narration.");
        }
    }

    private static void ValidateControllerEducation(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        if (director == null)
            return;

        SerializedObject serialized = new(director);
        SerializedProperty narration = serialized.FindProperty("m_ControllerNarrationAfterName");
        if (narration == null ||
            narration.enumValueIndex != (int)PPEVoiceFlowDirector.ControllerGuideNarration.Simple)
        {
            failures.Add("Name submission must retain the authored Simple controller narration.");
        }

        string[][] expectedClipNames =
        {
            new[] { "VO_PPE_CTRL_DETAIL_001_Start", "VO_PPE_CTRL_DETAIL_002_RayTrigger" },
            new[] { "VO_PPE_CTRL_DETAIL_003_GripGrab_Release" },
            new[] { "VO_PPE_CTRL_DETAIL_004_Joystick", "VO_PPE_CTRL_DETAIL_005_GuideFollow" },
        };
        SerializedProperty steps = serialized.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpleSteps = serialized.FindProperty("m_ControllerSimpVoiceSteps");
        if (steps == null || !steps.isArray || steps.arraySize != expectedClipNames.Length)
        {
            failures.Add("Detailed controller narration must contain three authored visual steps.");
        }
        else
        {
            for (int stepIndex = 0; stepIndex < expectedClipNames.Length; stepIndex++)
            {
                SerializedProperty clips = steps.GetArrayElementAtIndex(stepIndex)
                    .FindPropertyRelative("clips");
                if (clips == null || !clips.isArray ||
                    clips.arraySize != expectedClipNames[stepIndex].Length)
                {
                    failures.Add($"Detailed controller step {stepIndex} has the wrong clip count.");
                    continue;
                }

                for (int clipIndex = 0; clipIndex < expectedClipNames[stepIndex].Length; clipIndex++)
                {
                    AudioClip clip = clips.GetArrayElementAtIndex(clipIndex)
                        .objectReferenceValue as AudioClip;
                    if (clip == null || clip.name != expectedClipNames[stepIndex][clipIndex])
                    {
                        failures.Add(
                            $"Detailed controller step {stepIndex} clip {clipIndex} must be " +
                            $"'{expectedClipNames[stepIndex][clipIndex]}'.");
                    }
                }

                if (simpleSteps == null || !simpleSteps.isArray ||
                    simpleSteps.arraySize != expectedClipNames.Length)
                {
                    failures.Add("Simple controller narration must contain three authored visual steps.");
                    continue;
                }

                SerializedProperty detailedStep = steps.GetArrayElementAtIndex(stepIndex);
                SerializedProperty simpleStep = simpleSteps.GetArrayElementAtIndex(stepIndex);
                SerializedProperty detailedVisuals = detailedStep.FindPropertyRelative("controllerGuideVisuals");
                SerializedProperty simpleVisuals = simpleStep.FindPropertyRelative("controllerGuideVisuals");
                SerializedProperty detailedCompanion = detailedStep.FindPropertyRelative("controllerGuideCompanionVisual");
                SerializedProperty simpleCompanion = simpleStep.FindPropertyRelative("controllerGuideCompanionVisual");
                if (detailedVisuals == null || simpleVisuals == null ||
                    detailedVisuals.arraySize != simpleVisuals.arraySize ||
                    detailedVisuals.arraySize == 0)
                {
                    failures.Add(
                        $"Detailed controller step {stepIndex} does not reuse the authored Simple guide visual array.");
                }
                else
                {
                    for (int visualIndex = 0; visualIndex < simpleVisuals.arraySize; visualIndex++)
                    {
                        if (detailedVisuals.GetArrayElementAtIndex(visualIndex).objectReferenceValue !=
                            simpleVisuals.GetArrayElementAtIndex(visualIndex).objectReferenceValue)
                        {
                            failures.Add(
                                $"Detailed controller step {stepIndex} visual {visualIndex} does not reuse the Simple guide visual.");
                        }
                    }
                }

                if (simpleCompanion == null || simpleCompanion.objectReferenceValue == null ||
                    detailedCompanion == null ||
                    detailedCompanion.objectReferenceValue != simpleCompanion.objectReferenceValue)
                {
                    failures.Add(
                        $"Detailed controller step {stepIndex} does not reuse the Simple guide companion visual.");
                }
            }
        }

        GameObject entry = FindSceneObject("Controller Education Entry");
        if (entry == null)
        {
            failures.Add("Controller Education Entry is missing.");
        }
        else
        {
            Button button = entry.GetComponent<Button>();
            if (button == null || button is XRKeyboardKey)
                failures.Add("Controller Education Entry must use a standard Button, not XRKeyboardKey.");
            if (button != null && (button.targetGraphic == null || !button.targetGraphic.raycastTarget))
                failures.Add("Controller Education Entry target Graphic must block XR UI rays.");
            Image rootImage = entry.GetComponent<Image>();
            if (rootImage == null || !rootImage.raycastTarget)
                failures.Add("Controller Education Entry root Image must block XR UI rays.");

            PPEControllerEducationEntry route = entry.GetComponent<PPEControllerEducationEntry>();
            if (route == null)
            {
                failures.Add("Controller Education Entry has no PPEControllerEducationEntry route.");
            }
            else
            {
                SerializedObject routeSerialized = new(route);
                RequireSameObject(
                    routeSerialized,
                    "m_VoiceFlowDirector",
                    director,
                    failures);
            }

            if (entry.GetComponentInParent<XRKeyboardLayout>() != null)
                failures.Add("Controller Education Entry must remain outside the typing keyboard layout.");
            foreach (Collider collider in entry.GetComponentsInChildren<Collider>(true))
                if (collider.enabled)
                    failures.Add($"Controller Education Entry collider '{collider.name}' must remain disabled.");
        }

        GameObject blocker = FindSceneObject("Keyboard Ray Blocker");
        if (blocker == null)
        {
            failures.Add("Keyboard Ray Blocker is missing.");
        }
        else
        {
            Image blockerImage = blocker.GetComponent<Image>();
            if (blockerImage == null || !blockerImage.raycastTarget)
                failures.Add("Keyboard Ray Blocker Image must have Raycast Target enabled.");

            RectTransform rect = blocker.GetComponent<RectTransform>();
            if (rect == null || rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one ||
                rect.sizeDelta != Vector2.zero)
            {
                failures.Add("Keyboard Ray Blocker must stretch across the authored keyboard canvas.");
            }
            if (blocker.transform.GetSiblingIndex() != 0)
                failures.Add("Keyboard Ray Blocker must remain behind the authored keyboard controls.");
            if (blocker.transform.parent == null || blocker.transform.parent.name != "Modal  Keyboard Canvas")
                failures.Add("Keyboard Ray Blocker must be a direct child of Modal  Keyboard Canvas.");
        }

        const string directorPath = "Assets/Scripts/PPEVoiceFlowDirector.cs";
        string source = File.ReadAllText(directorPath);
        if (!source.Contains("<XRController>{RightHand}/primaryButton"))
            failures.Add("Physical right-controller A binding is missing.");
        if (!source.Contains("NotifyControllerEducationRequested();"))
            failures.Add("Physical and XR UI A inputs do not share the controller education handler.");
        if (!source.Contains("ResolveControllerEducationNextState"))
            failures.Add("Detailed controller education has no keyboard-return route.");

        int presentationStart = source.IndexOf(
            "private void ApplyPresentation(FlowState state)",
            StringComparison.Ordinal);
        int presentationEnd = source.IndexOf(
            "private static bool UsesControllerModels",
            presentationStart >= 0 ? presentationStart : 0,
            StringComparison.Ordinal);
        if (presentationStart < 0 || presentationEnd <= presentationStart)
        {
            failures.Add("Controller guide presentation method could not be inspected.");
        }
        else
        {
            string presentationBody = source.Substring(
                presentationStart,
                presentationEnd - presentationStart);
            if (!presentationBody.Contains(
                    "|| m_ReturnToNameInputAfterControllerEducation;"))
            {
                failures.Add(
                    "Keyboard-A detailed education does not preserve the authored grouped guide child visibility.");
            }
        }

        GameObject simpleRay = FindSceneObject("1_Ray");
        Transform card = simpleRay != null ? FindTransform(simpleRay.transform, "Card") : null;
        Transform panel = simpleRay != null ? FindTransform(simpleRay.transform, "Panel") : null;
        if (simpleRay == null || card == null || panel == null)
        {
            failures.Add("The authored 1_Ray guide group must contain Card and Panel children.");
        }
        else if (!card.gameObject.activeSelf || !panel.gameObject.activeSelf)
        {
            failures.Add(
                "The authored 1_Ray Card and Panel children must both remain active for detailed/simple guide reuse.");
        }
    }

    private static void ValidateControllerGuidePresentationRuntime(List<string> failures)
    {
        Scene previewScene = default;
        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);
            PPEVoiceFlowDirector director = FindSingleInScene<PPEVoiceFlowDirector>(
                previewScene,
                failures,
                "preview controller presentation");
            if (director == null)
                return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type directorType = typeof(PPEVoiceFlowDirector);
            FieldInfo narrationField = directorType.GetField("m_ActiveControllerNarration", flags);
            FieldInfo returnField = directorType.GetField(
                "m_ReturnToNameInputAfterControllerEducation",
                flags);
            FieldInfo completionField = directorType.GetField(
                "m_ControllerEducationCompleted",
                flags);
            FieldInfo autoHideSecondsField = directorType.GetField(
                "m_WindowPresentationAutoHideSeconds",
                flags);
            FieldInfo detailedStepsField = directorType.GetField("m_ControllerEduVoiceSteps", flags);
            FieldInfo simpleStepsField = directorType.GetField("m_ControllerSimpVoiceSteps", flags);
            MethodInfo applyPresentation = directorType.GetMethod("ApplyPresentation", flags);
            MethodInfo applyVisual = directorType.GetMethod("ApplyControllerGuideVisual", flags);
            MethodInfo getNameSubmissionNextState = directorType.GetMethod(
                "GetNameSubmissionNextState",
                flags);
            MethodInfo resolveControllerEducationNextState = directorType.GetMethod(
                "ResolveControllerEducationNextState",
                flags);
            if (narrationField == null || returnField == null || completionField == null ||
                autoHideSecondsField == null ||
                detailedStepsField == null || simpleStepsField == null ||
                applyPresentation == null || applyVisual == null ||
                getNameSubmissionNextState == null || resolveControllerEducationNextState == null)
            {
                failures.Add("Controller guide runtime presentation/route members could not be inspected.");
                return;
            }

            // Preview validation must not start the authored welcome auto-hide
            // coroutine while exercising the real presentation method in Edit Mode.
            autoHideSecondsField.SetValue(director, 0f);

            PPEVoiceFlowDirector.VoiceStep[] detailedSteps =
                detailedStepsField.GetValue(director) as PPEVoiceFlowDirector.VoiceStep[];
            PPEVoiceFlowDirector.VoiceStep[] simpleSteps =
                simpleStepsField.GetValue(director) as PPEVoiceFlowDirector.VoiceStep[];
            if (detailedSteps == null || detailedSteps.Length != 3 ||
                simpleSteps == null || simpleSteps.Length != 3)
            {
                failures.Add("Controller guide runtime presentation requires three detailed and Simple steps.");
                return;
            }

            completionField.SetValue(director, false);
            PPEVoiceFlowDirector.FlowState freshNameRoute =
                (PPEVoiceFlowDirector.FlowState)getNameSubmissionNextState.Invoke(director, null);
            if (freshNameRoute != PPEVoiceFlowDirector.FlowState.ControllerRay)
            {
                failures.Add(
                    "A fresh session must route keyboard submission to Simple controller education.");
            }

            narrationField.SetValue(
                director,
                PPEVoiceFlowDirector.ControllerGuideNarration.Education);
            returnField.SetValue(director, true);
            PPEVoiceFlowDirector.FlowState detailedCompletionRoute =
                (PPEVoiceFlowDirector.FlowState)resolveControllerEducationNextState.Invoke(
                    director,
                    new object[]
                    {
                        PPEVoiceFlowDirector.FlowState.ControllerRayT,
                        PPEVoiceFlowDirector.FlowState.CardIntro,
                    });
            if (detailedCompletionRoute != PPEVoiceFlowDirector.FlowState.NameInput ||
                !(bool)completionField.GetValue(director) ||
                (bool)returnField.GetValue(director))
            {
                failures.Add(
                    "Detailed controller education must mark completion and return to keyboard input.");
            }

            PPEVoiceFlowDirector.FlowState completedNameRoute =
                (PPEVoiceFlowDirector.FlowState)getNameSubmissionNextState.Invoke(director, null);
            if (completedNameRoute != PPEVoiceFlowDirector.FlowState.CardIntro)
            {
                failures.Add(
                    "After detailed education, keyboard submission must skip Simple education and open cards.");
            }

            narrationField.SetValue(
                director,
                PPEVoiceFlowDirector.ControllerGuideNarration.Education);
            returnField.SetValue(director, true);

            InvokePresentation(
                director,
                applyPresentation,
                applyVisual,
                PPEVoiceFlowDirector.FlowState.ControllerRay,
                detailedSteps[0]);
            RequireVisibleGuideObject(previewScene, "1_Ray", "detailed Trigger right guide", failures);
            RequireVisibleGuideChild(previewScene, "1_Ray", "Card", "detailed Trigger upper image", failures);
            RequireVisibleGuideChild(previewScene, "1_Ray", "Panel", "detailed Trigger lower image", failures);
            RequireVisibleGuideObject(previewScene, "1_Ctrl_Trigger", "detailed Trigger controller", failures);

            InvokePresentation(
                director,
                applyPresentation,
                applyVisual,
                PPEVoiceFlowDirector.FlowState.ControllerMarker,
                detailedSteps[1]);
            RequireVisibleGuideObject(previewScene, "2_Marker", "detailed Grip right guide", failures);
            RequireVisibleGuideObject(previewScene, "2_Ctrl_Grip", "detailed Grip controller", failures);

            InvokePresentation(
                director,
                applyPresentation,
                applyVisual,
                PPEVoiceFlowDirector.FlowState.ControllerRayT,
                detailedSteps[2]);
            RequireVisibleGuideObject(previewScene, "3_Ray_T", "detailed Joystick right guide", failures);
            RequireVisibleGuideObject(previewScene, "3_Ctrl_Joystick", "detailed Joystick controller", failures);

            narrationField.SetValue(
                director,
                PPEVoiceFlowDirector.ControllerGuideNarration.Simple);
            returnField.SetValue(director, false);
            applyPresentation.Invoke(
                director,
                new object[] { PPEVoiceFlowDirector.FlowState.NameInput });
            InvokePresentation(
                director,
                applyPresentation,
                applyVisual,
                PPEVoiceFlowDirector.FlowState.ControllerRay,
                simpleSteps[0]);
            RequireVisibleGuideObject(previewScene, "1_Ray", "Simple Trigger right guide after return", failures);
            RequireVisibleGuideChild(previewScene, "1_Ray", "Card", "Simple Trigger upper image after return", failures);
            RequireVisibleGuideChild(previewScene, "1_Ray", "Panel", "Simple Trigger lower image after return", failures);
            RequireVisibleGuideObject(previewScene, "1_Ctrl_Trigger", "Simple Trigger controller after return", failures);
        }
        catch (TargetInvocationException exception)
        {
            failures.Add(
                $"Controller guide runtime presentation threw: " +
                $"{exception.InnerException?.GetType().Name ?? exception.GetType().Name}: " +
                $"{exception.InnerException?.Message ?? exception.Message}");
        }
        catch (Exception exception)
        {
            failures.Add(
                $"Controller guide runtime presentation validation failed: " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void InvokePresentation(
        PPEVoiceFlowDirector director,
        MethodInfo applyPresentation,
        MethodInfo applyVisual,
        PPEVoiceFlowDirector.FlowState state,
        PPEVoiceFlowDirector.VoiceStep step)
    {
        applyPresentation.Invoke(director, new object[] { state });
        applyVisual.Invoke(director, new object[] { step, 0 });
    }

    private static void RequireVisibleGuideChild(
        Scene scene,
        string parentName,
        string childName,
        string label,
        List<string> failures)
    {
        GameObject parent = FindSceneObject(scene, parentName);
        Transform child = parent != null ? FindTransform(parent.transform, childName) : null;
        if (child == null)
        {
            failures.Add($"{label} is missing.");
            return;
        }

        RequireVisibleGuideObject(child.gameObject, label, failures);
    }

    private static void RequireVisibleGuideObject(
        Scene scene,
        string objectName,
        string label,
        List<string> failures)
    {
        GameObject target = FindSceneObject(scene, objectName);
        if (target == null)
        {
            failures.Add($"{label} '{objectName}' is missing.");
            return;
        }

        RequireVisibleGuideObject(target, label, failures);
    }

    private static void RequireVisibleGuideObject(
        GameObject target,
        string label,
        List<string> failures)
    {
        if (!target.activeSelf || !target.activeInHierarchy)
        {
            failures.Add(
                $"{label} is not active after the simulated runtime presentation transition.");
            return;
        }

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        bool hasVisibleGraphic = false;
        bool hasRenderableImageContent = false;
        foreach (Graphic graphic in graphics)
        {
            if (graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy &&
                graphic.color.a > 0.001f)
            {
                hasVisibleGraphic = true;

                if (graphic is Image image && image.sprite != null)
                    hasRenderableImageContent = true;
                else if (graphic is RawImage rawImage && rawImage.texture != null)
                    hasRenderableImageContent = true;
            }
        }

        if (!hasVisibleGraphic)
        {
            failures.Add($"{label} has no enabled, active, non-transparent Graphic.");
            return;
        }

        if (!hasRenderableImageContent)
            failures.Add($"{label} has no visible Image Sprite or RawImage Texture assigned.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = FindTransform(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
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

    private static T FindSingle<T>(List<string> failures) where T : Component
    {
        T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        if (found.Length != 1)
        {
            failures.Add($"Expected one {typeof(T).Name}, found {found.Length}.");
            return found.Length > 0 ? found[0] : null;
        }

        return found[0];
    }

    private static T FindSingleInScene<T>(
        Scene scene,
        List<string> failures,
        string label) where T : Component
    {
        List<T> found = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));

        if (found.Count != 1)
        {
            failures.Add(
                $"Expected one {typeof(T).Name} in {label}, found {found.Count}.");
            return found.Count > 0 ? found[0] : null;
        }

        return found[0];
    }

    private static List<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        List<T> found = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }

    private static T RequireObject<T>(
        SerializedObject serialized,
        string propertyName,
        List<string> failures) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        T value = property?.objectReferenceValue as T;
        if (value == null)
        {
            failures.Add(
                $"{serialized.targetObject.GetType().Name}.{propertyName} is missing or has the wrong type.");
        }

        return value;
    }

    private static void RequireSameObject(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object expected,
        List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != expected)
        {
            failures.Add(
                $"{serialized.targetObject.GetType().Name}.{propertyName} does not reference the authored {expected?.GetType().Name ?? "object"}.");
        }
    }

    private static void RequireNonEmptyString(
        SerializedObject serialized,
        string propertyName,
        List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || string.IsNullOrWhiteSpace(property.stringValue))
        {
            failures.Add(
                $"{serialized.targetObject.GetType().Name}.{propertyName} is empty or missing.");
        }
    }

    private static void RequireExactString(
        SerializedObject serialized,
        string propertyName,
        string expected,
        List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.stringValue != expected)
        {
            failures.Add(
                $"{serialized.targetObject.name}.{propertyName} must be '{expected}', " +
                $"actual '{property?.stringValue ?? "<missing>"}'.");
        }
    }

    private static void RequireClipName(
        SerializedObject serialized,
        string propertyName,
        string expectedName,
        List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        AudioClip clip = property?.objectReferenceValue as AudioClip;
        if (clip == null || clip.name != expectedName)
        {
            failures.Add(
                $"{serialized.targetObject.GetType().Name}.{propertyName} must reference " +
                $"'{expectedName}', actual '{clip?.name ?? "<missing>"}'.");
        }
    }

    private static void RequireApproximately(
        Vector3 actual,
        Vector3 expected,
        string label,
        List<string> failures)
    {
        if ((actual - expected).sqrMagnitude > 0.000001f)
            failures.Add($"{label} changed: expected {expected}, actual {actual}.");
    }
}
