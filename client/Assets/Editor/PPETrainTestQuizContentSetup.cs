using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PPETrainTestQuizContentSetup
{
    private const VerticalAlignmentOptions AuthoredOptionVerticalAlignment = (VerticalAlignmentOptions)4096;
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test.unity";
    private const string QuizLayoutTrialScenePath =
        "Assets/Scenes/3_PPE_Room_Train_Test_1.unity";

    private static readonly Vector2[] BaselineOptionPositions =
    {
        new(0f, 42f),
        new(0f, -38f),
        new(0f, -118f),
        new(0f, -198f),
    };

    private sealed class QuestionDefinition
    {
        public readonly string Prompt;
        public readonly string[] Options;
        public readonly int CorrectOption;
        public readonly string Feedback;

        public QuestionDefinition(
            string prompt,
            string[] options,
            int correctOption,
            string feedback = "")
        {
            Prompt = prompt;
            Options = options;
            CorrectOption = correctOption;
            Feedback = feedback;
        }
    }

    private static readonly QuestionDefinition[] TrainingQuestions =
    {
        new(
            "Q1. 방호복 점검\n\n방호복을 착용했습니다.\n몸을 숙였을 때 지퍼 부분이 팽팽하게 당겨지고 팔을 앞으로 뻗기가 어렵습니다.\n\n어떻게 해야 할까요?",
            new[]
            {
                "1) 그대로 작업한다.",
                "2) 지퍼를 조금 열어 움직임을 편하게 한다.",
                "3) 몸에 맞는 크기의 방호복으로 교체한다.",
            },
            2,
            "방호복이 너무 작으면 움직이는 동안 봉제선과 지퍼가 계속 당겨질 수 있습니다.\n지퍼를 여는 것이 아니라 몸에 맞는 크기의 방호복으로 교체해야 합니다."),
        new(
            "Q2. 안전대 점검\n\n안전대를 착용했지만 걸을 때마다 장비가 좌우로 흔들리고 송기호스가 몸 주변에서 움직입니다.\n\n가장 먼저 확인해야 할 것은 무엇일까요?",
            new[]
            {
                "1) 안전모의 턱끈",
                "2) 안전대의 어깨끈과 허리 고정끈",
                "3) 방호복의 지퍼",
            },
            1,
            "안전대는 양쪽 어깨끈과 허리 고정끈을 고르게 조절해 몸에 밀착시켜야 합니다.\n너무 느슨하면 장비가 흔들리고 호스가 주변 구조물에 걸릴 수 있습니다."),
        new(
            "Q3. 송기마스크 점검\n\n송기마스크를 착용했습니다.\n마스크와 뺨 사이에 방호복 후드 끝부분이 조금 끼어 있습니다.\n\n어떻게 해야 할까요?",
            new[]
            {
                "1) 작은 틈이므로 그대로 작업한다.",
                "2) 마스크를 더 강하게 눌러 착용한다.",
                "3) 마스크를 다시 착용하여 후드가 밀착면에 끼지 않도록 한다.",
            },
            2,
            "머리카락이나 방호복 후드가 마스크의 밀착면에 끼면 틈이 생길 수 있습니다.\n얼굴에 고르게 밀착되도록 다시 확인해야 합니다."),
        new(
            "Q4. 안전모 점검\n\n안전모를 착용하고 고개를 숙였더니 안전모가 앞으로 움직이며 시야를 가렸습니다.\n\n올바른 조치는 무엇일까요?",
            new[]
            {
                "1) 작업 중에는 고개를 숙이지 않는다.",
                "2) 안전모의 위치와 턱끈 고정 상태를 다시 조절한다.",
                "3) 안전모를 조금 뒤로 젖혀 착용한다.",
            },
            1,
            "안전모는 머리에 수평으로 맞추고 턱끈을 고정해야 합니다.\n고개를 움직였을 때 흔들리거나 시야를 가린다면 고정 상태를 다시 조절해야 합니다."),
        new(
            "Q5. 장갑·테이핑 점검\n\n장갑과 방호복 소매를 테이프로 연결했습니다.\n하지만 손목이 거의 움직이지 않을 정도로 테이프를 강하게 감았습니다.\n\n어떻게 해야 할까요?",
            new[]
            {
                "1) 강하게 고정됐으므로 가장 안전하다.",
                "2) 테이프를 한 겹 더 감는다.",
                "3) 테이프를 다시 조절하여 틈은 막되 손목 움직임을 방해하지 않게 한다.",
            },
            2,
            "테이프는 연결 부분을 보조적으로 고정하는 역할입니다.\n너무 느슨하면 틈이 벌어지고, 너무 세게 감으면 손목 움직임을 방해할 수 있습니다."),
    };

    private static readonly QuestionDefinition[] TestQuestions =
    {
        new(
            "Q1. 다음 중 작업을 시작하기 전에 반드시 수정해야 하는 방호복 상태는 무엇입니까?",
            new[]
            {
                "1) 목과 지퍼가 끝까지 닫혀 있다.",
                "2) 몸을 움직여도 봉제선이 과도하게 당겨지지 않는다.",
                "3) 목 부분이 조금 열려 피부가 드러나 있다.",
                "4) 몸에 맞는 크기의 방호복을 착용했다.",
            },
            2),
        new(
            "Q2. 다음 중 송기식 호흡보호구의 정상 착용 상태로 보기 어려운 것은 무엇입니까?",
            new[]
            {
                "1) 마스크가 얼굴에 고르게 밀착되어 있다.",
                "2) 송기호스가 눌리지 않았다.",
                "3) 공기가 정상적으로 공급된다.",
                "4) 후드 일부가 마스크와 얼굴 사이에 끼어 있다.",
            },
            3),
        new(
            "Q3. 다음 PPE 점검 결과 중 가장 적절한 상태는 무엇입니까?",
            new[]
            {
                "1) 장갑이 짧아 손목 부분의 피부가 보인다.",
                "2) 안전모 턱끈이 느슨해 고개를 숙이면 안전모가 움직인다.",
                "3) 안전대가 느슨해 몸을 움직이면 장비가 흔들린다.",
                "4) 장갑의 긴 손목 부분이 방호복 소매를 충분히 덮고 있다.",
            },
            3),
        new(
            "Q4. 장갑과 방호복 소매를 연결하여 테이핑하려고 합니다.\n다음 중 올바른 방법은 무엇입니까?",
            new[]
            {
                "1) 소매와 장갑 사이에 틈을 남긴 후 테이프로 고정한다.",
                "2) 손목이 움직이지 않을 정도로 단단하게 감는다.",
                "3) 테이프만으로 장갑과 방호복을 연결한다.",
                "4) 장갑과 소매를 충분히 겹친 후 연결 부분을 적절한 강도로 고정한다.",
            },
            3),
        new(
            "Q5. 종합문제 — 작업자가 PPE 착용을 완료했습니다.\n방호복 지퍼·목 닫힘 / 장화·바지단 빈틈 없음 / 안전대 밀착\n송기마스크 얼굴 밀착·송기호스 정상 / 안전모 턱끈 고정\n장갑은 소매를 충분히 덮고 테이프 연결부도 고정됨\n단, 고개를 숙이자 안전모가 앞으로 움직여 시야를 가렸습니다.\n이 작업자는 어떻게 해야 합니까?",
            new[]
            {
                "1) 나머지 PPE가 정상이므로 작업을 시작한다.",
                "2) 안전모를 벗고 작업한다.",
                "3) 안전모의 위치와 고정 상태를 다시 조절한 후 작업한다.",
                "4) 안전모를 손으로 잡은 채 작업한다.",
            },
            2),
    };

    [MenuItem("Tools/PPE/Apply Training Test Quiz Content")]
    public static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE training/test quiz content setup was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PPEQuizController[] controllers =
            UnityEngine.Object.FindObjectsByType<PPEQuizController>(FindObjectsInactive.Include);
        if (controllers.Length != 1)
            throw new InvalidOperationException(
                $"Expected one PPEQuizController in '{ScenePath}', found {controllers.Length}.");

        PPEQuizController controller = controllers[0];
        SerializedObject serialized = new(controller);
        SerializedProperty pages = RequireProperty(serialized, "pages");
        if (!pages.isArray || pages.arraySize != 5)
            throw new InvalidOperationException("PPEQuizController.pages must contain five authored pages.");

        for (int pageIndex = 0; pageIndex < pages.arraySize; pageIndex++)
            ConfigurePage(pages.GetArrayElementAtIndex(pageIndex), pageIndex);

        PopulateBankIfEmpty(RequireProperty(serialized, "trainingQuestions"), TrainingQuestions);
        PopulateBankIfEmpty(RequireProperty(serialized, "testQuestions"), TestQuestions);
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(
            $"[PPE Quiz Content Setup] Applied separate 5-question Training/Test banks and authored Test option 4 buttons to '{ScenePath}'.");
    }

    [MenuItem("Tools/PPE/Quiz Layout Trial/Apply Adaptive Fixed Font (_1)")]
    public static void ApplyAdaptiveFixedFontLayout()
    {
        ApplyQuizLayoutProfile(adaptive: true);
    }

    [MenuItem("Tools/PPE/Quiz Layout Trial/Restore Baseline (_1)")]
    public static void RestoreQuizLayoutBaseline()
    {
        ApplyQuizLayoutProfile(adaptive: false);
    }

    [MenuItem("Tools/PPE/Quiz Layout Trial/Validate Adaptive Fixed Font (_1)")]
    public static void ValidateAdaptiveFixedFontLayout()
    {
        Scene previewScene = default;
        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(QuizLayoutTrialScenePath);
            PPEQuizController controller = FindSingleController(previewScene);
            SerializedObject serialized = new(controller);
            SerializedProperty pages = RequireProperty(serialized, "pages");
            TMP_Text title = RequireObject<TMP_Text>(
                RequireProperty(serialized, "quizTitleLabel"),
                "quizTitleLabel");
            TMP_Text feedback = RequireObject<TMP_Text>(
                RequireProperty(serialized, "feedbackLabel"),
                "feedbackLabel");
            GameObject quizRoot = RequireObject<GameObject>(
                RequireProperty(serialized, "quizRoot"),
                "quizRoot");
            RectTransform feedbackIcon = RequireObject<RectTransform>(
                RequireProperty(serialized, "feedbackIconRoot"),
                "feedbackIconRoot");
            if (!RequireProperty(serialized, "useAdaptiveContentLayout").boolValue)
                throw new InvalidOperationException("Adaptive quiz content layout is disabled.");
            RequireFloat(serialized, "questionVerticalPadding", 12f);
            RequireFloat(serialized, "optionVerticalPadding", 8f);
            RequireFloat(serialized, "feedbackVerticalPadding", 12f);

            RequireRect(controller.GetComponent<RectTransform>(), null, new Vector2(1200f, 600f), "Quiz Canvas");
            RequireRect(quizRoot.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1000f, 560f), "Quiz Panel");
            RequireRect(title.rectTransform, new Vector2(0f, 235f), new Vector2(900f, 56f), "Title");
            RequireRect(feedback.rectTransform, new Vector2(35f, -215f), new Vector2(780f, 90f), "Feedback");
            RequireFixedText(feedback, 20f, HorizontalAlignmentOptions.Left, AuthoredOptionVerticalAlignment, "Feedback");
            RequireRect(feedbackIcon, new Vector2(-420f, -215f), new Vector2(80f, 60f), "Feedback Icon");

            if (!pages.isArray || pages.arraySize != 5)
                throw new InvalidOperationException("PPEQuizController.pages must contain five authored pages.");

            SerializedProperty trainingQuestions = RequireProperty(serialized, "trainingQuestions");
            SerializedProperty testQuestions = RequireProperty(serialized, "testQuestions");
            for (int pageIndex = 0; pageIndex < pages.arraySize; pageIndex++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(pageIndex);
                GameObject pageRoot = RequireObject<GameObject>(
                    page.FindPropertyRelative("root"),
                    $"pages[{pageIndex}].root");
                TMP_Text question = RequireObject<TMP_Text>(
                    page.FindPropertyRelative("questionLabel"),
                    $"pages[{pageIndex}].questionLabel");
                TMP_Text progress = RequireObject<TMP_Text>(
                    page.FindPropertyRelative("progressLabel"),
                    $"pages[{pageIndex}].progressLabel");

                RequireRect(progress.rectTransform, new Vector2(0f, 190f), new Vector2(900f, 40f), $"Page {pageIndex + 1} Progress");
                RequireRect(question.rectTransform, new Vector2(0f, 120f), new Vector2(900f, 100f), $"Page {pageIndex + 1} Question");
                RequireFixedText(question, 24f, HorizontalAlignmentOptions.Left, VerticalAlignmentOptions.Top, $"Page {pageIndex + 1} Question");

                SerializedProperty buttons = page.FindPropertyRelative("optionButtons");
                SerializedProperty labels = page.FindPropertyRelative("optionLabels");
                if (buttons == null || labels == null || buttons.arraySize != 4 || labels.arraySize != 4)
                    throw new InvalidOperationException($"Page {pageIndex + 1} requires four authored options.");

                for (int optionIndex = 0; optionIndex < 4; optionIndex++)
                {
                    Button button = RequireObject<Button>(
                        buttons.GetArrayElementAtIndex(optionIndex),
                        $"pages[{pageIndex}].optionButtons[{optionIndex}]");
                    TMP_Text label = RequireObject<TMP_Text>(
                        labels.GetArrayElementAtIndex(optionIndex),
                        $"pages[{pageIndex}].optionLabels[{optionIndex}]");
                    RequireRect(
                        button.GetComponent<RectTransform>(),
                        BaselineOptionPositions[optionIndex],
                        new Vector2(860f, 64f),
                        $"Page {pageIndex + 1} Option {optionIndex + 1}");
                    RequireRect(
                        label.rectTransform,
                        Vector2.zero,
                        new Vector2(790f, 54f),
                        $"Page {pageIndex + 1} Option {optionIndex + 1} Label");
                    RequireFixedText(
                        label,
                        22f,
                        HorizontalAlignmentOptions.Left,
                        AuthoredOptionVerticalAlignment,
                        $"Page {pageIndex + 1} Option {optionIndex + 1} Label");
                    if (optionIndex < 3)
                    {
                        RequireTextMeasurable(
                            label,
                            label.text,
                            $"Education Q{pageIndex + 1} Option {optionIndex + 1}");
                    }
                }

                RequireTextMeasurable(question, question.text, $"Education Q{pageIndex + 1}");
                RequireTextMeasurable(
                    question,
                    trainingQuestions.GetArrayElementAtIndex(pageIndex).FindPropertyRelative("prompt").stringValue,
                    $"Training Q{pageIndex + 1}");
                RequireTextMeasurable(
                    question,
                    testQuestions.GetArrayElementAtIndex(pageIndex).FindPropertyRelative("prompt").stringValue,
                    $"Test Q{pageIndex + 1}");
                ValidateOptionsFit(labels, trainingQuestions.GetArrayElementAtIndex(pageIndex), $"Training Q{pageIndex + 1}");
                ValidateOptionsFit(labels, testQuestions.GetArrayElementAtIndex(pageIndex), $"Test Q{pageIndex + 1}");

                string trainingFeedback = trainingQuestions
                    .GetArrayElementAtIndex(pageIndex)
                    .FindPropertyRelative("feedback")
                    .stringValue;
                RequireTextMeasurable(feedback, trainingFeedback, $"Training Q{pageIndex + 1} Feedback");
            }

            Debug.Log(
                $"[PPE Quiz Layout Trial] PASS '{QuizLayoutTrialScenePath}': baseline spacing, serialized adaptive padding, fixed fonts, and all mode text are valid.");
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void ApplyQuizLayoutProfile(bool adaptive)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE quiz layout trial was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(QuizLayoutTrialScenePath, OpenSceneMode.Single);
        PPEQuizController controller = FindSingleController(scene);
        SerializedObject serialized = new(controller);
        SerializedProperty pages = RequireProperty(serialized, "pages");
        if (!pages.isArray || pages.arraySize != 5)
            throw new InvalidOperationException("PPEQuizController.pages must contain five authored pages.");

        GameObject quizRoot = RequireObject<GameObject>(RequireProperty(serialized, "quizRoot"), "quizRoot");
        TMP_Text title = RequireObject<TMP_Text>(RequireProperty(serialized, "quizTitleLabel"), "quizTitleLabel");
        TMP_Text feedback = RequireObject<TMP_Text>(RequireProperty(serialized, "feedbackLabel"), "feedbackLabel");
        Transform feedbackIcon = quizRoot.transform.Find("Feedback Icon");
        if (feedbackIcon == null)
            throw new InvalidOperationException("Quiz Panel/Feedback Icon is missing.");

        SetRectSize(controller.GetComponent<RectTransform>(), new Vector2(1200f, 600f));
        SetRect(
            quizRoot.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(1000f, 560f));
        SetRect(
            title.rectTransform,
            new Vector2(0f, 235f),
            new Vector2(900f, 56f));
        SetRect(
            feedback.rectTransform,
            new Vector2(35f, -215f),
            new Vector2(780f, 90f));
        SetText(
            feedback,
            adaptive ? 20f : 24f,
            false,
            adaptive ? 20f : 18f,
            adaptive ? 20f : 72f,
            HorizontalAlignmentOptions.Left,
            AuthoredOptionVerticalAlignment);
        SetRect(
            feedbackIcon as RectTransform,
            new Vector2(-420f, -215f),
            new Vector2(80f, 60f));

        RequireProperty(serialized, "useAdaptiveContentLayout").boolValue = adaptive;
        RequireProperty(serialized, "feedbackIconRoot").objectReferenceValue = feedbackIcon;
        RequireProperty(serialized, "questionVerticalPadding").floatValue = 12f;
        RequireProperty(serialized, "optionVerticalPadding").floatValue = 8f;
        RequireProperty(serialized, "feedbackVerticalPadding").floatValue = 12f;

        for (int pageIndex = 0; pageIndex < pages.arraySize; pageIndex++)
        {
            SerializedProperty page = pages.GetArrayElementAtIndex(pageIndex);
            GameObject pageRoot = RequireObject<GameObject>(
                page.FindPropertyRelative("root"),
                $"pages[{pageIndex}].root");
            TMP_Text progress = pageRoot.transform.Find("Progress")?.GetComponent<TMP_Text>();
            TMP_Text question = RequireObject<TMP_Text>(
                page.FindPropertyRelative("questionLabel"),
                $"pages[{pageIndex}].questionLabel");
            if (progress == null)
                throw new InvalidOperationException($"{pageRoot.name}/Progress is missing.");
            page.FindPropertyRelative("progressLabel").objectReferenceValue = progress;

            SetRect(
                progress.rectTransform,
                new Vector2(0f, 190f),
                new Vector2(900f, 40f));
            SetRect(
                question.rectTransform,
                new Vector2(0f, 120f),
                new Vector2(900f, 100f));
            SetText(
                question,
                adaptive ? 24f : 31f,
                !adaptive,
                adaptive ? 24f : 14f,
                adaptive ? 24f : 31f,
                adaptive ? HorizontalAlignmentOptions.Left : HorizontalAlignmentOptions.Center,
                adaptive ? VerticalAlignmentOptions.Top : VerticalAlignmentOptions.Middle);

            SerializedProperty buttons = page.FindPropertyRelative("optionButtons");
            SerializedProperty labels = page.FindPropertyRelative("optionLabels");
            if (buttons == null || labels == null || buttons.arraySize != 4 || labels.arraySize != 4)
                throw new InvalidOperationException($"Page {pageIndex + 1} requires four authored options.");

            for (int optionIndex = 0; optionIndex < 4; optionIndex++)
            {
                Button button = RequireObject<Button>(
                    buttons.GetArrayElementAtIndex(optionIndex),
                    $"pages[{pageIndex}].optionButtons[{optionIndex}]");
                TMP_Text label = RequireObject<TMP_Text>(
                    labels.GetArrayElementAtIndex(optionIndex),
                    $"pages[{pageIndex}].optionLabels[{optionIndex}]");
                SetRect(
                    button.GetComponent<RectTransform>(),
                    BaselineOptionPositions[optionIndex],
                    new Vector2(860f, 64f));
                SetRect(
                    label.rectTransform,
                    Vector2.zero,
                    new Vector2(790f, 54f));
                SetText(
                    label,
                    adaptive ? 22f : 25f,
                    !adaptive,
                    adaptive ? 22f : 18f,
                    adaptive ? 22f : 25f,
                    HorizontalAlignmentOptions.Left,
                    AuthoredOptionVerticalAlignment);
            }
        }

        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(
            adaptive
                ? $"[PPE Quiz Layout Trial] Applied adaptive fixed-font layout to '{QuizLayoutTrialScenePath}'."
                : $"[PPE Quiz Layout Trial] Restored the recorded baseline layout in '{QuizLayoutTrialScenePath}'.");
    }

    private static void ConfigurePage(SerializedProperty page, int pageIndex)
    {
        GameObject root = RequireObject<GameObject>(page.FindPropertyRelative("root"),
            $"pages[{pageIndex}].root");
        TMP_Text questionLabel = root.transform.Find("Question")?.GetComponent<TMP_Text>();
        TMP_Text progressLabel = root.transform.Find("Progress")?.GetComponent<TMP_Text>();
        if (questionLabel == null)
            throw new InvalidOperationException($"{root.name} has no direct Question TMP label.");
        if (progressLabel == null)
            throw new InvalidOperationException($"{root.name} has no direct Progress TMP label.");

        SerializedProperty progressProperty = page.FindPropertyRelative("progressLabel");
        if (progressProperty.objectReferenceValue == null)
            progressProperty.objectReferenceValue = progressLabel;
        else if (progressProperty.objectReferenceValue != progressLabel)
            throw new InvalidOperationException($"{root.name} progressLabel references another object.");

        SerializedProperty questionProperty = page.FindPropertyRelative("questionLabel");
        if (questionProperty.objectReferenceValue == null)
            questionProperty.objectReferenceValue = questionLabel;
        else if (questionProperty.objectReferenceValue != questionLabel)
            throw new InvalidOperationException($"{root.name} questionLabel references another object.");

        SerializedProperty buttonsProperty = page.FindPropertyRelative("optionButtons");
        if (buttonsProperty.arraySize != 3 && buttonsProperty.arraySize != 4)
            throw new InvalidOperationException(
                $"{root.name} must have three existing options or four configured options.");

        Button[] buttons = new Button[4];
        for (int index = 0; index < buttonsProperty.arraySize; index++)
            buttons[index] = RequireObject<Button>(
                buttonsProperty.GetArrayElementAtIndex(index),
                $"{root.name}.optionButtons[{index}]");

        if (buttonsProperty.arraySize == 3)
        {
            buttons[3] = CreateFourthOption(buttons[1], buttons[2]);
            buttonsProperty.arraySize = 4;
            buttonsProperty.GetArrayElementAtIndex(3).objectReferenceValue = buttons[3];
        }

        SerializedProperty labelsProperty = page.FindPropertyRelative("optionLabels");
        if (labelsProperty.arraySize != 0 && labelsProperty.arraySize != 4)
            throw new InvalidOperationException($"{root.name}.optionLabels is partially configured.");
        labelsProperty.arraySize = 4;

        for (int index = 0; index < buttons.Length; index++)
        {
            TMP_Text label = buttons[index].GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                throw new InvalidOperationException($"{buttons[index].name} has no TMP label.");
            SerializedProperty labelProperty = labelsProperty.GetArrayElementAtIndex(index);
            if (labelProperty.objectReferenceValue == null)
                labelProperty.objectReferenceValue = label;
            else if (labelProperty.objectReferenceValue != label)
                throw new InvalidOperationException($"{root.name}.optionLabels[{index}] references another object.");
        }
    }

    private static Button CreateFourthOption(Button second, Button third)
    {
        GameObject clone = UnityEngine.Object.Instantiate(third.gameObject, third.transform.parent);
        clone.name = "Option 4";
        Undo.RegisterCreatedObjectUndo(clone, "Create PPE Test Option 4");

        RectTransform secondRect = second.GetComponent<RectTransform>();
        RectTransform thirdRect = third.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = thirdRect.anchoredPosition +
            (thirdRect.anchoredPosition - secondRect.anchoredPosition);

        Button button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "4) 보기 4";
        clone.SetActive(false);
        return button;
    }

    private static PPEQuizController FindSingleController(Scene scene)
    {
        PPEQuizController found = null;
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PPEQuizController controller in
                     root.GetComponentsInChildren<PPEQuizController>(true))
            {
                found = controller;
                count++;
            }
        }

        if (count != 1)
        {
            throw new InvalidOperationException(
                $"Expected one PPEQuizController in '{scene.path}', found {count}.");
        }

        return found;
    }

    private static void SetRectSize(RectTransform rect, Vector2 size)
    {
        if (rect == null)
            throw new InvalidOperationException("Required authored RectTransform is missing.");

        Undo.RecordObject(rect, "Apply PPE quiz layout profile");
        rect.sizeDelta = size;
        EditorUtility.SetDirty(rect);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            throw new InvalidOperationException("Required authored RectTransform is missing.");

        Undo.RecordObject(rect, "Apply PPE quiz layout profile");
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        EditorUtility.SetDirty(rect);
    }

    private static void SetText(
        TMP_Text text,
        float fontSize,
        bool autoSize,
        float minimumFontSize,
        float maximumFontSize,
        HorizontalAlignmentOptions horizontalAlignment,
        VerticalAlignmentOptions verticalAlignment)
    {
        if (text == null)
            throw new InvalidOperationException("Required authored TMP text is missing.");

        Undo.RecordObject(text, "Apply PPE quiz layout profile");
        text.fontSize = fontSize;
        text.enableAutoSizing = autoSize;
        text.fontSizeMin = minimumFontSize;
        text.fontSizeMax = maximumFontSize;
        text.horizontalAlignment = horizontalAlignment;
        text.verticalAlignment = verticalAlignment;
        EditorUtility.SetDirty(text);
    }

    private static void RequireRect(
        RectTransform rect,
        Vector2? expectedPosition,
        Vector2 expectedSize,
        string label)
    {
        if (rect == null)
            throw new InvalidOperationException($"{label} RectTransform is missing.");
        if (expectedPosition.HasValue &&
            (rect.anchoredPosition - expectedPosition.Value).sqrMagnitude > 0.0001f)
        {
            throw new InvalidOperationException(
                $"{label} position is {rect.anchoredPosition}; expected {expectedPosition.Value}.");
        }
        if ((rect.sizeDelta - expectedSize).sqrMagnitude > 0.0001f)
        {
            throw new InvalidOperationException(
                $"{label} size is {rect.sizeDelta}; expected {expectedSize}.");
        }
    }

    private static void RequireFixedText(
        TMP_Text text,
        float fontSize,
        HorizontalAlignmentOptions horizontalAlignment,
        VerticalAlignmentOptions verticalAlignment,
        string label)
    {
        if (text == null)
            throw new InvalidOperationException($"{label} TMP is missing.");
        if (text.enableAutoSizing || Mathf.Abs(text.fontSize - fontSize) > 0.001f ||
            Mathf.Abs(text.fontSizeMin - fontSize) > 0.001f ||
            Mathf.Abs(text.fontSizeMax - fontSize) > 0.001f ||
            text.horizontalAlignment != horizontalAlignment ||
            text.verticalAlignment != verticalAlignment)
        {
            throw new InvalidOperationException(
                $"{label} must use fixed {fontSize}pt {horizontalAlignment}/{verticalAlignment} text.");
        }
    }

    private static void RequireFloat(
        SerializedObject serialized,
        string propertyName,
        float expected)
    {
        float actual = RequireProperty(serialized, propertyName).floatValue;
        if (Mathf.Abs(actual - expected) > 0.001f)
        {
            throw new InvalidOperationException(
                $"{propertyName} is {actual}; expected authored value {expected}.");
        }
    }

    private static void ValidateOptionsFit(
        SerializedProperty labels,
        SerializedProperty question,
        string label)
    {
        SerializedProperty options = question.FindPropertyRelative("options");
        if (options == null || !options.isArray || options.arraySize > labels.arraySize)
            throw new InvalidOperationException($"{label} option data is invalid.");

        for (int optionIndex = 0; optionIndex < options.arraySize; optionIndex++)
        {
            TMP_Text optionLabel = RequireObject<TMP_Text>(
                labels.GetArrayElementAtIndex(optionIndex),
                $"{label} option label {optionIndex + 1}");
            RequireTextMeasurable(
                optionLabel,
                options.GetArrayElementAtIndex(optionIndex).stringValue,
                $"{label} Option {optionIndex + 1}");
        }
    }

    private static void RequireTextMeasurable(TMP_Text text, string content, string label)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException($"{label} content is empty.");

        RectTransform rect = text.rectTransform;
        Vector2 preferred = text.GetPreferredValues(content, rect.rect.width, 10000f);
        if (preferred.x <= 0f || preferred.y <= 0f ||
            float.IsNaN(preferred.x) || float.IsNaN(preferred.y))
        {
            throw new InvalidOperationException(
                $"{label} could not produce a valid TMP preferred size.");
        }
    }

    private static void PopulateBankIfEmpty(
        SerializedProperty bank,
        QuestionDefinition[] definitions)
    {
        if (!bank.isArray)
            throw new InvalidOperationException($"{bank.propertyPath} is not an array.");
        if (bank.arraySize != 0)
        {
            if (bank.arraySize != definitions.Length)
                throw new InvalidOperationException($"{bank.propertyPath} is partially configured.");
            return;
        }

        bank.arraySize = definitions.Length;
        for (int questionIndex = 0; questionIndex < definitions.Length; questionIndex++)
        {
            QuestionDefinition definition = definitions[questionIndex];
            SerializedProperty question = bank.GetArrayElementAtIndex(questionIndex);
            question.FindPropertyRelative("prompt").stringValue = definition.Prompt;
            question.FindPropertyRelative("correctOption").intValue = definition.CorrectOption;
            question.FindPropertyRelative("feedback").stringValue = definition.Feedback;

            SerializedProperty options = question.FindPropertyRelative("options");
            options.arraySize = definition.Options.Length;
            for (int optionIndex = 0; optionIndex < definition.Options.Length; optionIndex++)
                options.GetArrayElementAtIndex(optionIndex).stringValue = definition.Options[optionIndex];
        }
    }

    private static SerializedProperty RequireProperty(SerializedObject serialized, string name)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException($"Missing serialized property: {name}.");
        return property;
    }

    private static T RequireObject<T>(SerializedProperty property, string label)
        where T : UnityEngine.Object
    {
        T value = property?.objectReferenceValue as T;
        if (value == null)
            throw new InvalidOperationException($"Missing authored reference: {label}.");
        return value;
    }
}
