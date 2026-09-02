using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class PPEQuizPage
{
    public GameObject root;
    public TMPro.TMP_Text progressLabel;
    public TMPro.TMP_Text questionLabel;
    public Button[] optionButtons;
    public TMPro.TMP_Text[] optionLabels;
    [Range(0, 2)] public int correctOption;
    [TextArea(2, 5)] public string explanation;
}

[Serializable]
public sealed class PPEQuizQuestion
{
    public PPEQuizTopic topic;
    [TextArea(3, 12)] public string prompt;
    public string[] options;
    [Range(0, 3)] public int correctOption;
    [TextArea(2, 6)] public string feedback;
}

[DisallowMultipleComponent]
public sealed class PPEQuizController : MonoBehaviour
{
    [Header("Authored quiz UI")]
    [SerializeField] GameObject quizRoot;
    [SerializeField] TMPro.TMP_Text quizTitleLabel;
    [SerializeField] GameObject correctIconRoot;
    [SerializeField] GameObject wrongIconRoot;
    [SerializeField] TMPro.TMP_Text feedbackLabel;

    [Header("Authored adaptive content layout")]
    [SerializeField] bool useAdaptiveContentLayout;
    [SerializeField] RectTransform feedbackIconRoot;
    [SerializeField, Min(0f)] float questionVerticalPadding;
    [SerializeField, Min(0f)] float optionVerticalPadding;
    [SerializeField, Min(0f)] float feedbackVerticalPadding;

    [Header("Authored result UI")]
    [SerializeField] GameObject resultRoot;
    [SerializeField] TMPro.TMP_Text resultTitleLabel;
    [SerializeField] GameObject[] resultMetricRoots;
    [SerializeField] TMPro.TMP_Text clearMinuteLabel;
    [SerializeField] TMPro.TMP_Text clearSecondLabel;
    [SerializeField] TMPro.TMP_Text scoreLabel;
    [SerializeField] TMPro.TMP_Text correctCountLabel;
    [SerializeField] TMPro.TMP_Text ppeWrongCountLabel;
    [SerializeField] Button resultBackButton;

    [Header("Authored pages")]
    [SerializeField] PPEQuizPage[] pages;

    [Header("Authored behavior")]
    [SerializeField] PPEVoiceFlowDirector voiceFlowDirector;
    [SerializeField] string correctSfxId = "Correct Answer";
    [SerializeField] string wrongSfxId = "Wrong Answer";
    [SerializeField, Min(0f)] float correctAdvanceDelay = 1f;
    [SerializeField] Color correctFeedbackColor;
    [SerializeField] Color wrongFeedbackColor;

    [Header("Mode Quiz Information")]
    [Tooltip("Authored title used by Education mode.")]
    [SerializeField] string educationQuizTitle;
    [Tooltip("Authored title used by Training mode. Training retries wrong answers with short feedback.")]
    [SerializeField] string trainingQuizTitle;
    [Tooltip("Authored title used by Test mode. Test scores the first selection and advances without explanation text.")]
    [SerializeField] string testQuizTitle;
    [SerializeField] string trainingWrongFeedbackMessage;
    [SerializeField] string testResultTitle;

    [Header("Scenario And Mode Question Catalog")]
    [Tooltip("Authored question pools separated by work plan and learning mode.")]
    [SerializeField] PPEQuizQuestionCatalog questionCatalog;

    [Header("Authored Training Questions")]
    [SerializeField] PPEQuizQuestion[] trainingQuestions;

    [Header("Authored Test Questions")]
    [SerializeField] PPEQuizQuestion[] testQuestions;

    int currentPageIndex;
    int correctAnswerCount;
    bool isActive;
    bool hasAnswered;
    ScenarioDetailModal.PpeLearningMode activeMode;
    PPEQuizQuestion[] activeQuestions;
    bool usingCatalogQuestions;
    Coroutine advanceRoutine;
    string[] educationPrompts;
    string[][] educationOptions;
    bool adaptiveLayoutReady;
    RectTransform quizCanvasRect;
    RectTransform quizPanelRect;
    RectLayoutState canvasBaseline;
    RectLayoutState panelBaseline;
    RectLayoutState titleBaseline;
    RectLayoutState feedbackBaseline;
    RectLayoutState feedbackIconBaseline;
    PageLayoutState[] pageLayoutBaselines;
    float panelBottomPadding;

    readonly struct RectLayoutState
    {
        public readonly Vector2 AnchoredPosition;
        public readonly Vector2 SizeDelta;

        public RectLayoutState(RectTransform rect)
        {
            AnchoredPosition = rect.anchoredPosition;
            SizeDelta = rect.sizeDelta;
        }
    }

    sealed class PageLayoutState
    {
        public RectLayoutState Progress;
        public RectLayoutState Question;
        public RectLayoutState[] Buttons;
        public RectLayoutState[] Labels;
    }

    void Awake()
    {
        CaptureEducationContent();
        CaptureAdaptiveLayoutBaseline();
        WirePages();
        if (resultBackButton != null)
            resultBackButton.onClick.AddListener(OnResultBackSelected);
        SetActive(quizRoot, false);
        SetActive(resultRoot, false);
    }

    void OnDestroy()
    {
        if (resultBackButton != null)
            resultBackButton.onClick.RemoveListener(OnResultBackSelected);
    }

    public void BeginQuiz(ScenarioDetailModal.PpeLearningMode mode)
    {
        ScenarioDetailModal.PpeWorkPlan workPlan = voiceFlowDirector != null
            ? voiceFlowDirector.ActiveWorkPlan
            : ScenarioDetailModal.PpeWorkPlan.None;
        BeginQuiz(mode, workPlan);
    }

    public void BeginQuiz(
        ScenarioDetailModal.PpeLearningMode mode,
        ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        if (!TryPrepareActiveQuestions(mode, workPlan) ||
            !HasCompleteQuizReferences(mode) ||
            (mode == ScenarioDetailModal.PpeLearningMode.Test && !HasCompleteResultReferences()))
        {
            Debug.LogError(
                $"PPEQuizController requires a valid {workPlan}/{mode} pool, five authored quiz pages, and, for Test, complete Result Canvas references.",
                this);
            return;
        }

        ResetQuizProgress();
        activeMode = mode;
        isActive = true;
        currentPageIndex = 0;
        correctAnswerCount = 0;
        quizTitleLabel.text = GetQuizTitle(mode);
        SetActive(resultRoot, false);
        SetActive(quizRoot, true);
        ShowPage(currentPageIndex);
    }

    public void ChooseOption(int optionIndex)
    {
        if (!isActive || optionIndex < 0 || optionIndex >= GetOptionCount(currentPageIndex))
            return;

        PPEQuizPage page = pages[currentPageIndex];
        bool correct = optionIndex == GetCorrectOption(currentPageIndex);

        if (activeMode == ScenarioDetailModal.PpeLearningMode.Test)
        {
            if (hasAnswered)
                return;

            hasAnswered = true;
            SetButtonsInteractable(page, false);
            RecordQuizAnswer(optionIndex, correct);
            if (correct)
                correctAnswerCount++;
            SetActive(correctIconRoot, false);
            SetActive(wrongIconRoot, false);
            advanceRoutine = StartCoroutine(AdvanceAfterAnswer());
            return;
        }

        if (correct)
        {
            if (hasAnswered)
                return;

            hasAnswered = true;
            RecordQuizAnswer(optionIndex, correct);
            correctAnswerCount++;
            SetButtonsInteractable(page, false);
            string correctMessage = activeMode == ScenarioDetailModal.PpeLearningMode.Training
                ? activeQuestions[currentPageIndex].feedback
                : $"정답입니다. {activeQuestions[currentPageIndex].feedback}";
            ShowFeedback(correctMessage, true, showText: true);
            AudioManager.Instance?.PlaySfx(correctSfxId);
            advanceRoutine = StartCoroutine(AdvanceAfterAnswer());
            return;
        }

        string wrongMessage = activeMode == ScenarioDetailModal.PpeLearningMode.Training
            ? trainingWrongFeedbackMessage
            : $"오답입니다. {activeQuestions[currentPageIndex].feedback}";
        ShowFeedback(wrongMessage, false, showText: true);
        Button selectedButton = page.optionButtons[optionIndex];
        if (selectedButton == null || !selectedButton.interactable)
            return;

        RecordQuizAnswer(optionIndex, correct);
        selectedButton.interactable = false;
        AudioManager.Instance?.PlaySfx(wrongSfxId);
    }

    IEnumerator AdvanceAfterAnswer()
    {
        yield return new WaitForSecondsRealtime(correctAdvanceDelay);
        if (!isActive || !hasAnswered)
            yield break;

        advanceRoutine = null;
        currentPageIndex++;
        if (currentPageIndex < pages.Length)
        {
            ShowPage(currentPageIndex);
            yield break;
        }

        isActive = false;
        SetActive(quizRoot, false);
        voiceFlowDirector?.NotifyQuizCompleted(correctAnswerCount, pages.Length);
    }

    void RecordQuizAnswer(int optionIndex, bool correct)
    {
        PPEQuizQuestion question = activeQuestions != null &&
            currentPageIndex >= 0 &&
            currentPageIndex < activeQuestions.Length
            ? activeQuestions[currentPageIndex]
            : null;
        PPETrainingTelemetryCapture.RecordQuizAnswer(
            voiceFlowDirector?.ActiveModeSessionId,
            activeMode,
            voiceFlowDirector != null
                ? voiceFlowDirector.ActiveWorkPlan
                : ScenarioDetailModal.PpeWorkPlan.None,
            currentPageIndex + 1,
            pages?.Length ?? 0,
            question?.topic.ToString(),
            optionIndex + 1,
            correct);
    }

    void ShowPage(int pageIndex)
    {
        hasAnswered = false;
        for (int index = 0; index < pages.Length; index++)
            SetActive(pages[index].root, index == pageIndex);

        ApplyQuestionContent(pageIndex);

        SetActive(correctIconRoot, false);
        SetActive(wrongIconRoot, false);
        if (feedbackLabel != null)
        {
            feedbackLabel.gameObject.SetActive(activeMode != ScenarioDetailModal.PpeLearningMode.Test);
            feedbackLabel.text = string.Empty;
        }
        SetButtonsInteractable(pages[pageIndex], true);
        ApplyAdaptiveContentLayout(pageIndex);
    }

    void ApplyQuestionContent(int pageIndex)
    {
        PPEQuizPage page = pages[pageIndex];
        PPEQuizQuestion question = activeQuestions[pageIndex];
        string prompt = usingCatalogQuestions
            ? $"Q{pageIndex + 1}. {question.prompt}"
            : question.prompt;
        string[] options = question.options;

        page.questionLabel.text = prompt;
        for (int index = 0; index < page.optionButtons.Length; index++)
        {
            bool usedByMode = index < options.Length;
            SetActive(page.optionButtons[index].gameObject, usedByMode);
            if (usedByMode)
                page.optionLabels[index].text = options[index];
        }
    }

    void ShowFeedback(string message, bool correct, bool showText)
    {
        if (feedbackLabel != null)
        {
            feedbackLabel.gameObject.SetActive(showText);
            feedbackLabel.text = message;
            feedbackLabel.color = correct ? correctFeedbackColor : wrongFeedbackColor;
        }
        SetActive(correctIconRoot, correct);
        SetActive(wrongIconRoot, !correct);
        ApplyAdaptiveContentLayout(currentPageIndex);
    }

    public void ShowTestResult(
        float elapsedSeconds,
        int quizCorrectCount,
        int ppeWrongCount)
    {
        if (!HasCompleteResultReferences())
        {
            Debug.LogError(
                "PPEQuizController cannot show mode completion because Result Canvas references are incomplete.",
                this);
            return;
        }

        resultTitleLabel.text = testResultTitle;
        foreach (GameObject metricRoot in resultMetricRoots)
            SetActive(metricRoot, true);

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        clearMinuteLabel.text = (totalSeconds / 60).ToString();
        clearSecondLabel.text = (totalSeconds % 60).ToString();
        scoreLabel.text = (Mathf.Clamp(quizCorrectCount, 0, 5) * 20).ToString();
        correctCountLabel.text = Mathf.Clamp(quizCorrectCount, 0, 5).ToString();
        ppeWrongCountLabel.text = Mathf.Max(0, ppeWrongCount).ToString();

        SetActive(quizRoot, false);
        SetActive(resultRoot, true);
        resultBackButton.Select();
    }

    public void ResetForNewSession()
    {
        ResetQuizProgress();
        activeMode = ScenarioDetailModal.PpeLearningMode.Education;
        activeQuestions = null;
        usingCatalogQuestions = false;
        correctAnswerCount = 0;
        currentPageIndex = 0;
        SetActive(quizRoot, false);
        SetActive(resultRoot, false);
        SetActive(correctIconRoot, false);
        SetActive(wrongIconRoot, false);
        if (feedbackLabel != null)
        {
            feedbackLabel.gameObject.SetActive(true);
            feedbackLabel.text = string.Empty;
        }
    }

    void ResetQuizProgress()
    {
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        isActive = false;
        hasAnswered = false;
    }

    void OnResultBackSelected()
    {
        if (resultRoot == null || !resultRoot.activeSelf)
            return;

        voiceFlowDirector?.NotifyCompletionBackRequested();
    }

    void WirePages()
    {
        if (pages == null)
            return;

        for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            PPEQuizPage page = pages[pageIndex];
            if (page.optionButtons == null)
                continue;
            for (int optionIndex = 0; optionIndex < page.optionButtons.Length; optionIndex++)
            {
                Button button = page.optionButtons[optionIndex];
                if (button == null)
                    continue;
                int capturedOption = optionIndex;
                button.onClick.AddListener(() => ChooseOption(capturedOption));
            }
        }
    }

    static void SetButtonsInteractable(PPEQuizPage page, bool interactable)
    {
        if (page.optionButtons == null)
            return;
        foreach (Button button in page.optionButtons)
            if (button != null)
                button.interactable = interactable && button.gameObject.activeSelf;
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    string GetQuizTitle(ScenarioDetailModal.PpeLearningMode mode)
    {
        if (mode == ScenarioDetailModal.PpeLearningMode.Training)
            return trainingQuizTitle;
        if (mode == ScenarioDetailModal.PpeLearningMode.Test)
            return testQuizTitle;
        return educationQuizTitle;
    }

    int GetCorrectOption(int pageIndex)
    {
        return activeQuestions[pageIndex].correctOption;
    }

    int GetOptionCount(int pageIndex)
    {
        return activeQuestions[pageIndex].options.Length;
    }

    bool TryPrepareActiveQuestions(
        ScenarioDetailModal.PpeLearningMode mode,
        ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        activeQuestions = null;
        usingCatalogQuestions = questionCatalog != null;

        if (questionCatalog != null)
        {
            if (pages == null || pages.Length == 0 ||
                workPlan == ScenarioDetailModal.PpeWorkPlan.None ||
                !questionCatalog.TryGetQuestions(workPlan, mode, out PPEQuizQuestion[] pool) ||
                !HasCompleteQuestionBank(
                    pool,
                    ExpectedOptionCount(mode),
                    RequiresFeedback(mode),
                    minimumQuestionCount: pages?.Length ?? 5))
            {
                return false;
            }

            activeQuestions = SelectBalancedQuestions(pool, pages.Length);
            return activeQuestions.Length == pages.Length;
        }

        activeQuestions = BuildLegacyQuestions(mode);
        return activeQuestions != null && activeQuestions.Length == 5;
    }

    PPEQuizQuestion[] BuildLegacyQuestions(ScenarioDetailModal.PpeLearningMode mode)
    {
        if (mode == ScenarioDetailModal.PpeLearningMode.Training)
            return trainingQuestions;
        if (mode == ScenarioDetailModal.PpeLearningMode.Test)
            return testQuestions;
        if (pages == null || educationPrompts == null || educationOptions == null || pages.Length != 5)
            return null;

        PPEQuizQuestion[] questions = new PPEQuizQuestion[pages.Length];
        for (int index = 0; index < pages.Length; index++)
        {
            questions[index] = new PPEQuizQuestion
            {
                prompt = educationPrompts[index],
                options = educationOptions[index],
                correctOption = pages[index].correctOption,
                feedback = pages[index].explanation,
            };
        }
        return questions;
    }

    static PPEQuizQuestion[] SelectBalancedQuestions(PPEQuizQuestion[] pool, int count)
    {
        Dictionary<PPEQuizTopic, List<PPEQuizQuestion>> byTopic = new();
        foreach (PPEQuizQuestion question in pool)
        {
            if (!byTopic.TryGetValue(question.topic, out List<PPEQuizQuestion> topicQuestions))
            {
                topicQuestions = new List<PPEQuizQuestion>();
                byTopic.Add(question.topic, topicQuestions);
            }
            topicQuestions.Add(question);
        }

        List<PPEQuizTopic> topics = new(byTopic.Keys);
        Shuffle(topics);
        List<PPEQuizQuestion> selected = new(count);
        foreach (PPEQuizTopic topic in topics)
        {
            List<PPEQuizQuestion> topicQuestions = byTopic[topic];
            selected.Add(topicQuestions[UnityEngine.Random.Range(0, topicQuestions.Count)]);
            if (selected.Count == count)
                return selected.ToArray();
        }

        List<PPEQuizQuestion> remaining = new(pool.Length - selected.Count);
        foreach (PPEQuizQuestion question in pool)
            if (!selected.Contains(question))
                remaining.Add(question);
        Shuffle(remaining);
        for (int index = 0; selected.Count < count; index++)
            selected.Add(remaining[index]);
        return selected.ToArray();
    }

    static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    static int ExpectedOptionCount(ScenarioDetailModal.PpeLearningMode mode)
    {
        return mode == ScenarioDetailModal.PpeLearningMode.Test ? 4 : 3;
    }

    static bool RequiresFeedback(ScenarioDetailModal.PpeLearningMode mode)
    {
        return mode != ScenarioDetailModal.PpeLearningMode.Test;
    }

    void CaptureEducationContent()
    {
        if (pages == null)
            return;

        educationPrompts = new string[pages.Length];
        educationOptions = new string[pages.Length][];
        for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            PPEQuizPage page = pages[pageIndex];
            if (page == null || page.questionLabel == null || page.optionLabels == null)
                continue;

            educationPrompts[pageIndex] = page.questionLabel.text;
            int optionCount = Mathf.Min(3, page.optionLabels.Length);
            educationOptions[pageIndex] = new string[optionCount];
            for (int optionIndex = 0; optionIndex < optionCount; optionIndex++)
                educationOptions[pageIndex][optionIndex] = page.optionLabels[optionIndex]?.text;
        }
    }

    void CaptureAdaptiveLayoutBaseline()
    {
        if (!useAdaptiveContentLayout)
            return;

        quizCanvasRect = transform as RectTransform;
        quizPanelRect = quizRoot != null ? quizRoot.GetComponent<RectTransform>() : null;
        if (quizCanvasRect == null || quizPanelRect == null || quizTitleLabel == null ||
            feedbackLabel == null || feedbackIconRoot == null || pages == null)
        {
            Debug.LogError(
                "PPEQuizController adaptive layout requires authored Canvas, Panel, title, feedback, icon, and page references.",
                this);
            return;
        }

        canvasBaseline = new RectLayoutState(quizCanvasRect);
        panelBaseline = new RectLayoutState(quizPanelRect);
        titleBaseline = new RectLayoutState(quizTitleLabel.rectTransform);
        feedbackBaseline = new RectLayoutState(feedbackLabel.rectTransform);
        feedbackIconBaseline = new RectLayoutState(feedbackIconRoot);
        panelBottomPadding =
            feedbackBaseline.AnchoredPosition.y - feedbackBaseline.SizeDelta.y * 0.5f -
            (panelBaseline.AnchoredPosition.y - panelBaseline.SizeDelta.y * 0.5f);

        pageLayoutBaselines = new PageLayoutState[pages.Length];
        for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            PPEQuizPage page = pages[pageIndex];
            if (page == null || page.progressLabel == null || page.questionLabel == null ||
                page.optionButtons == null || page.optionLabels == null ||
                page.optionButtons.Length != page.optionLabels.Length)
            {
                Debug.LogError(
                    $"PPEQuizController adaptive layout page {pageIndex + 1} requires authored progress, question, button, and label references.",
                    this);
                pageLayoutBaselines = null;
                return;
            }

            PageLayoutState state = new()
            {
                Progress = new RectLayoutState(page.progressLabel.rectTransform),
                Question = new RectLayoutState(page.questionLabel.rectTransform),
                Buttons = new RectLayoutState[page.optionButtons.Length],
                Labels = new RectLayoutState[page.optionLabels.Length],
            };
            for (int optionIndex = 0; optionIndex < page.optionButtons.Length; optionIndex++)
            {
                Button button = page.optionButtons[optionIndex];
                TMPro.TMP_Text label = page.optionLabels[optionIndex];
                if (button == null || label == null)
                {
                    Debug.LogError(
                        $"PPEQuizController adaptive layout page {pageIndex + 1}, option {optionIndex + 1} has a missing authored reference.",
                        this);
                    pageLayoutBaselines = null;
                    return;
                }

                state.Buttons[optionIndex] = new RectLayoutState(button.GetComponent<RectTransform>());
                state.Labels[optionIndex] = new RectLayoutState(label.rectTransform);
            }
            pageLayoutBaselines[pageIndex] = state;
        }

        adaptiveLayoutReady = true;
    }

    void ApplyAdaptiveContentLayout(int pageIndex)
    {
        if (!useAdaptiveContentLayout || !adaptiveLayoutReady ||
            pageIndex < 0 || pageIndex >= pages.Length)
            return;

        PPEQuizPage page = pages[pageIndex];
        PageLayoutState baseline = pageLayoutBaselines[pageIndex];
        int activeOptionCount = GetOptionCount(pageIndex);

        float questionHeight = RequiredTextHeight(
            page.questionLabel,
            baseline.Question.SizeDelta,
            questionVerticalPadding);
        float questionTop =
            baseline.Question.AnchoredPosition.y + baseline.Question.SizeDelta.y * 0.5f;
        float questionCenter = questionTop - questionHeight * 0.5f;
        float previousBottom = questionCenter - questionHeight * 0.5f;

        Vector2[] buttonPositions = new Vector2[baseline.Buttons.Length];
        Vector2[] buttonSizes = new Vector2[baseline.Buttons.Length];
        Vector2[] labelSizes = new Vector2[baseline.Labels.Length];
        for (int optionIndex = 0; optionIndex < baseline.Buttons.Length; optionIndex++)
        {
            RectLayoutState buttonBaseline = baseline.Buttons[optionIndex];
            RectLayoutState labelBaseline = baseline.Labels[optionIndex];
            float labelHeight = RequiredTextHeight(
                page.optionLabels[optionIndex],
                labelBaseline.SizeDelta,
                optionVerticalPadding);
            float buttonHeight =
                buttonBaseline.SizeDelta.y + labelHeight - labelBaseline.SizeDelta.y;

            float gap;
            if (optionIndex == 0)
            {
                float baselineQuestionBottom =
                    baseline.Question.AnchoredPosition.y - baseline.Question.SizeDelta.y * 0.5f;
                float baselineButtonTop =
                    buttonBaseline.AnchoredPosition.y + buttonBaseline.SizeDelta.y * 0.5f;
                gap = baselineQuestionBottom - baselineButtonTop;
            }
            else
            {
                RectLayoutState previousBaseline = baseline.Buttons[optionIndex - 1];
                float previousBaselineBottom =
                    previousBaseline.AnchoredPosition.y - previousBaseline.SizeDelta.y * 0.5f;
                float currentBaselineTop =
                    buttonBaseline.AnchoredPosition.y + buttonBaseline.SizeDelta.y * 0.5f;
                gap = previousBaselineBottom - currentBaselineTop;
            }

            float buttonTop = previousBottom - gap;
            float buttonCenter = buttonTop - buttonHeight * 0.5f;
            buttonPositions[optionIndex] = new Vector2(
                buttonBaseline.AnchoredPosition.x,
                buttonCenter);
            buttonSizes[optionIndex] = new Vector2(buttonBaseline.SizeDelta.x, buttonHeight);
            labelSizes[optionIndex] = new Vector2(labelBaseline.SizeDelta.x, labelHeight);
            if (optionIndex < activeOptionCount)
                previousBottom = buttonCenter - buttonHeight * 0.5f;
        }

        bool feedbackIsActive = feedbackLabel.gameObject.activeSelf;
        float feedbackHeight = RequiredTextHeight(
            feedbackLabel,
            feedbackBaseline.SizeDelta,
            feedbackVerticalPadding);
        float feedbackCenter = feedbackBaseline.AnchoredPosition.y;
        if (feedbackIsActive && activeOptionCount > 0)
        {
            int lastOptionIndex = activeOptionCount - 1;
            RectLayoutState lastButtonBaseline = baseline.Buttons[lastOptionIndex];
            float lastBaselineBottom =
                lastButtonBaseline.AnchoredPosition.y - lastButtonBaseline.SizeDelta.y * 0.5f;
            float feedbackBaselineTop =
                feedbackBaseline.AnchoredPosition.y + feedbackBaseline.SizeDelta.y * 0.5f;
            float feedbackGap = lastBaselineBottom - feedbackBaselineTop;
            float feedbackTop = previousBottom - feedbackGap;
            feedbackCenter = feedbackTop - feedbackHeight * 0.5f;
            previousBottom = feedbackCenter - feedbackHeight * 0.5f;
        }

        float baselinePanelBottom =
            panelBaseline.AnchoredPosition.y - panelBaseline.SizeDelta.y * 0.5f;
        float desiredPanelBottom = previousBottom - panelBottomPadding;
        float extraHeight = Mathf.Max(0f, baselinePanelBottom - desiredPanelBottom);
        float childOffset = extraHeight * 0.5f;

        ApplyRect(
            quizCanvasRect,
            canvasBaseline.AnchoredPosition,
            new Vector2(canvasBaseline.SizeDelta.x, canvasBaseline.SizeDelta.y + extraHeight));
        ApplyRect(
            quizPanelRect,
            new Vector2(panelBaseline.AnchoredPosition.x, panelBaseline.AnchoredPosition.y - childOffset),
            new Vector2(panelBaseline.SizeDelta.x, panelBaseline.SizeDelta.y + extraHeight));
        ApplyRect(
            quizTitleLabel.rectTransform,
            OffsetY(titleBaseline.AnchoredPosition, childOffset),
            titleBaseline.SizeDelta);
        ApplyRect(
            page.progressLabel.rectTransform,
            OffsetY(baseline.Progress.AnchoredPosition, childOffset),
            baseline.Progress.SizeDelta);
        ApplyRect(
            page.questionLabel.rectTransform,
            new Vector2(baseline.Question.AnchoredPosition.x, questionCenter + childOffset),
            new Vector2(baseline.Question.SizeDelta.x, questionHeight));

        for (int optionIndex = 0; optionIndex < baseline.Buttons.Length; optionIndex++)
        {
            ApplyRect(
                page.optionButtons[optionIndex].GetComponent<RectTransform>(),
                OffsetY(buttonPositions[optionIndex], childOffset),
                buttonSizes[optionIndex]);
            ApplyRect(
                page.optionLabels[optionIndex].rectTransform,
                baseline.Labels[optionIndex].AnchoredPosition,
                labelSizes[optionIndex]);
        }

        ApplyRect(
            feedbackLabel.rectTransform,
            new Vector2(feedbackBaseline.AnchoredPosition.x, feedbackCenter + childOffset),
            new Vector2(feedbackBaseline.SizeDelta.x, feedbackHeight));
        ApplyRect(
            feedbackIconRoot,
            new Vector2(feedbackIconBaseline.AnchoredPosition.x, feedbackCenter + childOffset),
            feedbackIconBaseline.SizeDelta);
    }

    static float RequiredTextHeight(
        TMPro.TMP_Text text,
        Vector2 baselineSize,
        float verticalPadding)
    {
        Vector2 preferred = text.GetPreferredValues(text.text, baselineSize.x, Mathf.Infinity);
        return Mathf.Max(baselineSize.y, Mathf.Ceil(preferred.y + verticalPadding));
    }

    static Vector2 OffsetY(Vector2 value, float offset)
    {
        value.y += offset;
        return value;
    }

    static void ApplyRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    bool HasCompleteQuizReferences(ScenarioDetailModal.PpeLearningMode mode)
    {
        if (quizRoot == null || quizTitleLabel == null || feedbackLabel == null ||
            correctIconRoot == null || wrongIconRoot == null || voiceFlowDirector == null ||
            string.IsNullOrWhiteSpace(educationQuizTitle) ||
            string.IsNullOrWhiteSpace(trainingQuizTitle) ||
            string.IsNullOrWhiteSpace(testQuizTitle) ||
            string.IsNullOrWhiteSpace(trainingWrongFeedbackMessage) ||
            pages == null || pages.Length != 5 ||
            educationPrompts == null || educationPrompts.Length != 5 ||
            educationOptions == null || educationOptions.Length != 5 ||
            (useAdaptiveContentLayout && !adaptiveLayoutReady))
            return false;

        for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
        {
            PPEQuizPage page = pages[pageIndex];
            if (page == null || page.root == null || page.questionLabel == null ||
                (useAdaptiveContentLayout && page.progressLabel == null) ||
                page.optionButtons == null || page.optionButtons.Length != 4 ||
                page.optionLabels == null || page.optionLabels.Length != 4 ||
                string.IsNullOrWhiteSpace(educationPrompts[pageIndex]) ||
                educationOptions[pageIndex] == null || educationOptions[pageIndex].Length != 3)
                return false;
            for (int index = 0; index < 4; index++)
                if (page.optionButtons[index] == null || page.optionLabels[index] == null)
                    return false;
            for (int index = 0; index < educationOptions[pageIndex].Length; index++)
                if (string.IsNullOrWhiteSpace(educationOptions[pageIndex][index]))
                    return false;
        }

        return HasCompleteQuestionBank(
            activeQuestions,
            ExpectedOptionCount(mode),
            RequiresFeedback(mode),
            minimumQuestionCount: pages.Length);
    }

    static bool HasCompleteQuestionBank(
        PPEQuizQuestion[] questions,
        int expectedOptionCount,
        bool requireFeedback,
        int minimumQuestionCount = 5)
    {
        if (questions == null || questions.Length < minimumQuestionCount)
            return false;

        foreach (PPEQuizQuestion question in questions)
        {
            if (question == null || string.IsNullOrWhiteSpace(question.prompt) ||
                question.options == null || question.options.Length != expectedOptionCount ||
                question.correctOption < 0 || question.correctOption >= expectedOptionCount ||
                (requireFeedback && string.IsNullOrWhiteSpace(question.feedback)))
                return false;

            foreach (string option in question.options)
                if (string.IsNullOrWhiteSpace(option))
                    return false;
        }

        return true;
    }

    bool HasCompleteResultReferences()
    {
        if (resultRoot == null || resultTitleLabel == null || resultBackButton == null ||
            clearMinuteLabel == null || clearSecondLabel == null || scoreLabel == null ||
            correctCountLabel == null || ppeWrongCountLabel == null ||
            resultMetricRoots == null || resultMetricRoots.Length == 0)
        {
            return false;
        }

        foreach (GameObject metricRoot in resultMetricRoots)
            if (metricRoot == null)
                return false;
        return true;
    }
}
