using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class ScenarioDetailModal : MonoBehaviour
{
    public enum PpeLearningMode
    {
        Education,
        Training,
        Test,
    }

    public enum PpeWorkPlan
    {
        None = 0,
        ConfinedSpace,
        LeakResponse,
    }

    [Serializable]
    public sealed class ScenarioDetail
    {
        [SerializeField] private Button[] selectionButtons;
        [SerializeField] private string number;
        [SerializeField, TextArea] private string title;
        [SerializeField, TextArea(3, 10)] private string description;
        [SerializeField] private string trainingButtonLabel;
        [SerializeField] private UnityEvent onTrainingSelected;
        [SerializeField] private UnityEvent onIncompletePpeScenarioSelected;
        [SerializeField] private UnityEvent onStandardTrainingScenarioSelected;

        public Button[] SelectionButtons => selectionButtons;
        public string Number => number;
        public string Title => title;
        public string Description => description;
        public string TrainingButtonLabel => trainingButtonLabel;
        public UnityEvent OnTrainingSelected => onTrainingSelected;
        public UnityEvent OnIncompletePpeScenarioSelected => onIncompletePpeScenarioSelected;
        public UnityEvent OnStandardTrainingScenarioSelected => onStandardTrainingScenarioSelected;
    }

    [Header("Scene References")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private GameObject scenarioSelectionRoot;
    [Tooltip("Extra selection UI under XR UI Canvas (e.g. Heading). Hidden with scenarioSelectionRoot; never disable the canvas itself.")]
    [SerializeField] private GameObject[] additionalSelectionUiRoots;
    [Tooltip("Optional nested panel under modalRoot. When assigned, Show forces it active.")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject[] scenarioTitleObjects;
    [SerializeField] private GameObject[] scenarioDescriptionObjects;
    [SerializeField] private TMP_Text trainingButtonText;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button backButton;

    [Header("Training Choice View")]
    [Tooltip("Open a selected card directly on the authored PPE/safety training choices, without the legacy Training Select step.")]
    [SerializeField] private bool openWithTrainingChoices;
    [Tooltip("Open a selected card directly on 학습/훈련/테스트. Skips the incomplete-PPE versus next-scenario choice.")]
    [SerializeField] private bool openWithPpeModeChoices;
    [SerializeField] private string trainingChoiceTitle;
    [SerializeField, TextArea] private string trainingChoiceDescription;
    [SerializeField] private Button incompletePpeButton;
    [SerializeField] private Button standardTrainingButton;
    [SerializeField] private Button trainingChoiceBackButton;
    [Tooltip("Authored root containing the direct PPE/safety education choices.")]
    [SerializeField] private GameObject educationChoiceRoot;

    [Header("PPE Mode Buttons")]
    [Tooltip("Authored education/training/test buttons shown after PPE education is selected.")]
    [SerializeField] private Button ppeEducationModeButton;
    [SerializeField] private Button ppeTrainingModeButton;
    [SerializeField] private Button ppeTestModeButton;
    [SerializeField] private Button ppeModeChoiceBackButton;
    [Tooltip("Authored education/training/test group shown after the PPE education choice.")]
    [SerializeField] private GameObject ppeModeChoiceRoot;
    [Tooltip("Authored description object paired with the PPE mode button group.")]
    [SerializeField] private GameObject ppeModeDescriptionRoot;

    [Header("PPE Work Plan Buttons")]
    [Tooltip("Authored test work-plan group cloned from 2_Mode. Education/Training reuse educationChoiceRoot.")]
    [SerializeField] private GameObject testWorkPlanChoiceRoot;
    [SerializeField] private Button testConfinedSpaceButton;
    [SerializeField] private Button testLeakResponseButton;
    [SerializeField] private Button testRandomTestButton;
    [SerializeField] private Button testWorkPlanBackButton;

    [Header("Training Audio")]
    [SerializeField] private bool fadeOutBgmOnIncompletePpeSelection = true;
    [SerializeField, Min(0f)] private float incompletePpeBgmFadeOutDuration = 2.5f;

    [Header("Inspector-authored Scenario Data")]
    [SerializeField] private ScenarioDetail[] scenarios;
    [SerializeField] private bool enableMousePhysicsFallback = true;
    [SerializeField] private Camera mouseRaycastCamera;
    [SerializeField] private LayerMask mouseRaycastMask = ~0;

    private int selectedScenario = -1;
    private PpeLearningMode? pendingLearningMode;
    private readonly List<(Button button, UnityAction action)> selectionListeners = new();
    private Canvas ownerCanvas;

    public event Action<int> ModalShown;
    public event Action TrainingSelected;
    public event Action IncompletePpeScenarioSelected;
    public event Action<PpeLearningMode> PpeLearningModeSelected;
    public event Action PpeEducationModeChoiceStarted;
    public event Action ScenarioSelectionRestored;

    public bool OpensWithPpeModeChoices => openWithPpeModeChoices;
    public PpeWorkPlan SelectedWorkPlan { get; private set; }

    private void OnEnable()
    {
        ownerCanvas = GetComponent<Canvas>();
        SetSelectionListeners(true);
        if (trainingButton != null)
            trainingButton.onClick.AddListener(SelectTraining);
        if (backButton != null)
            backButton.onClick.AddListener(HideAndRestoreSelection);
        if (incompletePpeButton != null)
            incompletePpeButton.onClick.AddListener(SelectConfinedSpaceOrIncompletePpe);
        if (standardTrainingButton != null)
            standardTrainingButton.onClick.AddListener(SelectLeakResponseOrStandardTraining);
        if (trainingChoiceBackButton != null)
            trainingChoiceBackButton.onClick.AddListener(ReturnToScenarioDetails);
        if (ppeEducationModeButton != null)
            ppeEducationModeButton.onClick.AddListener(SelectPpeEducationMode);
        if (ppeTrainingModeButton != null)
            ppeTrainingModeButton.onClick.AddListener(SelectPpeTrainingMode);
        if (ppeTestModeButton != null)
            ppeTestModeButton.onClick.AddListener(SelectPpeTestMode);
        if (ppeModeChoiceBackButton != null)
            ppeModeChoiceBackButton.onClick.AddListener(ReturnToPpeEducationChoices);
        if (testConfinedSpaceButton != null)
            testConfinedSpaceButton.onClick.AddListener(SelectConfinedSpaceWorkPlan);
        if (testLeakResponseButton != null)
            testLeakResponseButton.onClick.AddListener(SelectLeakResponseWorkPlan);
        if (testRandomTestButton != null)
            testRandomTestButton.onClick.AddListener(SelectRandomWorkPlan);
        if (testWorkPlanBackButton != null)
            testWorkPlanBackButton.onClick.AddListener(ReturnFromWorkPlanChoices);
    }

    private void Update()
    {
        if (!enableMousePhysicsFallback || !WasMousePressedThisFrame())
            return;

        if (modalRoot != null && modalRoot.activeInHierarchy)
            return;

        if (TryShowFromMouseRaycast())
            return;

        LogPointerRaycastDiagnostics();
    }

    private void OnDisable()
    {
        SetSelectionListeners(false);
        if (trainingButton != null)
        {
            trainingButton.onClick.RemoveListener(SelectTraining);
        }
        if (backButton != null)
            backButton.onClick.RemoveListener(HideAndRestoreSelection);
        if (incompletePpeButton != null)
            incompletePpeButton.onClick.RemoveListener(SelectConfinedSpaceOrIncompletePpe);
        if (standardTrainingButton != null)
            standardTrainingButton.onClick.RemoveListener(SelectLeakResponseOrStandardTraining);
        if (trainingChoiceBackButton != null)
            trainingChoiceBackButton.onClick.RemoveListener(ReturnToScenarioDetails);
        if (ppeEducationModeButton != null)
            ppeEducationModeButton.onClick.RemoveListener(SelectPpeEducationMode);
        if (ppeTrainingModeButton != null)
            ppeTrainingModeButton.onClick.RemoveListener(SelectPpeTrainingMode);
        if (ppeTestModeButton != null)
            ppeTestModeButton.onClick.RemoveListener(SelectPpeTestMode);
        if (ppeModeChoiceBackButton != null)
            ppeModeChoiceBackButton.onClick.RemoveListener(ReturnToPpeEducationChoices);
        if (testConfinedSpaceButton != null)
            testConfinedSpaceButton.onClick.RemoveListener(SelectConfinedSpaceWorkPlan);
        if (testLeakResponseButton != null)
            testLeakResponseButton.onClick.RemoveListener(SelectLeakResponseWorkPlan);
        if (testRandomTestButton != null)
            testRandomTestButton.onClick.RemoveListener(SelectRandomWorkPlan);
        if (testWorkPlanBackButton != null)
            testWorkPlanBackButton.onClick.RemoveListener(ReturnFromWorkPlanChoices);
    }

    public void Show(int scenarioIndex)
    {
        if (scenarios == null || scenarioIndex < 0 || scenarioIndex >= scenarios.Length)
        {
            Debug.LogWarning(
                $"ScenarioDetailModal.Show ignored invalid scenarioIndex={scenarioIndex}.",
                this);
            return;
        }

        ScenarioDetail detail = scenarios[scenarioIndex];
        if (detail == null || modalRoot == null)
        {
            Debug.LogError(
                "ScenarioDetailModal.Show failed because modalRoot or scenario detail is missing. " +
                "Check the ScenarioDetailModal on the active XR UI Canvas.",
                this);
            return;
        }

        selectedScenario = scenarioIndex;
        ShowScenarioContent(scenarioIndex);
        if (numberText != null) numberText.text = detail.Number;
        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = detail.Title;
            if (descriptionText != null) descriptionText.text = detail.Description;
        }
        if (openWithPpeModeChoices)
            ShowPpeModeChoices();
        else if (openWithTrainingChoices)
            ShowTrainingChoiceActions();
        else
            ShowPrimaryActions();
        SetSelectionUiVisible(false);
        SetModalHierarchyActive(true);
        modalRoot.SetActive(true);
        EnsureModalPanelActive();
        modalRoot.transform.SetAsLastSibling();
        Debug.Log($"ScenarioDetailModal showing scenario {scenarioIndex + 1} on {modalRoot.name}.", modalRoot);
        LogModalTransformDiagnostics();
        if (openWithPpeModeChoices)
            ppeEducationModeButton?.Select();
        else if (openWithTrainingChoices)
            incompletePpeButton?.Select();
        else
            trainingButton?.Select();
        ModalShown?.Invoke(scenarioIndex);
    }

    public void Hide()
    {
        selectedScenario = -1;
        pendingLearningMode = null;
        SetModalHierarchyActive(false);
    }

    public void HideAndRestoreSelection()
    {
        Hide();
        SetSelectionUiVisible(true);
        ScenarioSelectionRestored?.Invoke();
    }

    private void SelectTraining()
    {
        if (scenarios == null || selectedScenario < 0 || selectedScenario >= scenarios.Length)
            return;

        if (!HasTrainingChoiceConfiguration())
        {
            Debug.LogWarning("ScenarioDetailModal training choice references or serialized text are missing.", this);
            return;
        }

        ScenarioDetail detail = scenarios[selectedScenario];
        detail?.OnTrainingSelected?.Invoke();
        if (detail == null)
            return;

        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = trainingChoiceTitle;
            if (descriptionText != null) descriptionText.text = trainingChoiceDescription;
        }
        ShowTrainingChoiceActions();
        incompletePpeButton?.Select();
        TrainingSelected?.Invoke();
    }

    private bool HasTrainingChoiceConfiguration()
    {
        return !string.IsNullOrWhiteSpace(trainingChoiceTitle)
            && !string.IsNullOrWhiteSpace(trainingChoiceDescription)
            && incompletePpeButton != null
            && standardTrainingButton != null
            && trainingChoiceBackButton != null;
    }

    private void SelectIncompletePpeScenario()
    {
        if (selectedScenario >= 0 && selectedScenario < scenarios.Length)
            scenarios[selectedScenario]?.OnIncompletePpeScenarioSelected?.Invoke();

        // Hide HUD + Heading only. Do not disable XR UI Canvas — TrackedDeviceGraphicRaycaster
        // throws KeyNotFoundException when a canvas with null eventCamera is deactivated.
        ShowPpeModeChoices();
        IncompletePpeScenarioSelected?.Invoke();
    }

    private void SelectStandardTrainingScenario()
    {
        Debug.Log("ScenarioDetailModal standard training scenario button selected.", this);
        if (selectedScenario >= 0 && selectedScenario < scenarios.Length)
            scenarios[selectedScenario]?.OnStandardTrainingScenarioSelected?.Invoke();
    }

    private void ReturnToScenarioDetails()
    {
        Debug.Log("ScenarioDetailModal training choice back button selected.", this);

        if (pendingLearningMode.HasValue)
        {
            ReturnFromWorkPlanChoices();
            return;
        }

        if (openWithTrainingChoices)
        {
            HideAndRestoreSelection();
            return;
        }

        if (scenarios == null || selectedScenario < 0 || selectedScenario >= scenarios.Length)
            return;

        ScenarioDetail detail = scenarios[selectedScenario];
        if (detail == null)
            return;

        ShowScenarioContent(selectedScenario);
        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = detail.Title;
            if (descriptionText != null) descriptionText.text = detail.Description;
        }
        ShowPrimaryActions();
        trainingButton?.Select();
    }

    private void SelectPpeEducationMode()
    {
        BeginWorkPlanChoice(PpeLearningMode.Education);
        PpeEducationModeChoiceStarted?.Invoke();
    }

    private void SelectPpeTrainingMode()
    {
        BeginWorkPlanChoice(PpeLearningMode.Training);
    }

    private void SelectPpeTestMode()
    {
        BeginWorkPlanChoice(PpeLearningMode.Test);
    }

    private void BeginWorkPlanChoice(PpeLearningMode mode)
    {
        pendingLearningMode = mode;
        ShowWorkPlanChoices();
    }

    private void SelectConfinedSpaceOrIncompletePpe()
    {
        if (pendingLearningMode.HasValue)
        {
            SelectWorkPlan(PpeWorkPlan.ConfinedSpace);
            return;
        }

        SelectIncompletePpeScenario();
    }

    private void SelectLeakResponseOrStandardTraining()
    {
        if (pendingLearningMode.HasValue)
        {
            SelectWorkPlan(PpeWorkPlan.LeakResponse);
            return;
        }

        SelectStandardTrainingScenario();
    }

    private void SelectConfinedSpaceWorkPlan()
    {
        SelectWorkPlan(PpeWorkPlan.ConfinedSpace);
    }

    private void SelectLeakResponseWorkPlan()
    {
        SelectWorkPlan(PpeWorkPlan.LeakResponse);
    }

    private void SelectRandomWorkPlan()
    {
        SelectWorkPlan(UnityEngine.Random.Range(0, 2) == 0
            ? PpeWorkPlan.ConfinedSpace
            : PpeWorkPlan.LeakResponse);
    }

    private void SelectWorkPlan(PpeWorkPlan workPlan)
    {
        if (!pendingLearningMode.HasValue)
        {
            Debug.LogError(
                "ScenarioDetailModal work-plan selection requires a pending education/training/test mode.",
                this);
            return;
        }

        PpeLearningMode mode = pendingLearningMode.Value;
        pendingLearningMode = null;
        SelectedWorkPlan = workPlan;
        Debug.Log($"ScenarioDetailModal PPE work plan selected: {workPlan} for {mode}.", this);
        NotifyPpeLearningModeSelected(mode);
    }

    private void ReturnFromWorkPlanChoices()
    {
        pendingLearningMode = null;
        ShowPpeModeChoices();
    }

    private void NotifyPpeLearningModeSelected(PpeLearningMode mode)
    {
        Debug.Log($"ScenarioDetailModal PPE mode selected: {mode}.", this);
        PpeLearningModeSelected?.Invoke(mode);

        // Preserve the established education path. Training and Test let the
        // voice-flow owner finish their selection acknowledgement first, then
        // call CompletePpeModeSelection explicitly.
        if (mode == PpeLearningMode.Education)
            CompletePpeModeSelection();
    }

    public void CompletePpeModeSelection()
    {
        if (modalRoot == null)
        {
            Debug.LogError(
                "ScenarioDetailModal requires its authored modalRoot before completing a PPE mode selection.",
                this);
            return;
        }

        if (fadeOutBgmOnIncompletePpeSelection && AudioManager.Instance != null)
            AudioManager.Instance.FadeOutBgm(incompletePpeBgmFadeOutDuration);

        SetSelectionUiVisible(false);
        Hide();
        PPEControllerTeleportModeManager.NotifyScenarioReadyForMovement();
    }

    public void ShowPpeModeChoicesAfterCompletion()
    {
        if (modalRoot == null || educationChoiceRoot == null || ppeModeChoiceRoot == null)
        {
            Debug.LogError(
                "ScenarioDetailModal requires authored modal and PPE mode-choice references before completion return.",
                this);
            return;
        }

        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);
        SetSelectionUiVisible(false);
        SetModalHierarchyActive(true);
        EnsureModalPanelActive();
        ShowPpeModeChoices();
        modalRoot.transform.SetAsLastSibling();
    }

    public void ShowPpeModeChoices()
    {
        if (educationChoiceRoot == null || ppeModeChoiceRoot == null)
        {
            Debug.LogError(
                "ScenarioDetailModal requires authored educationChoiceRoot and ppeModeChoiceRoot references before showing future PPE modes.",
                this);
            return;
        }

        pendingLearningMode = null;
        HideWorkPlanChoiceRoots();
        educationChoiceRoot.SetActive(false);
        // The scenario description belongs to the previous step. Leaving it on
        // stacks it over the mode description in the same authored area.
        SetOnlyActive(scenarioDescriptionObjects, -1);
        if (ppeModeDescriptionRoot != null)
            ppeModeDescriptionRoot.SetActive(true);
        ppeModeChoiceRoot.SetActive(true);
        ppeEducationModeButton?.Select();
    }

    private void ShowWorkPlanChoices()
    {
        if (!pendingLearningMode.HasValue)
        {
            Debug.LogError(
                "ScenarioDetailModal requires a pending PPE learning mode before showing work-plan choices.",
                this);
            return;
        }

        if (ppeModeChoiceRoot != null)
            ppeModeChoiceRoot.SetActive(false);

        bool isTest = pendingLearningMode == PpeLearningMode.Test;
        if (isTest)
        {
            if (!HasTestWorkPlanConfiguration())
            {
                Debug.LogError(
                    "ScenarioDetailModal test work-plan buttons are missing. " +
                    "Run Tools > PPE > Wire Work Plan Choices on 3_PPE_Room_Train_Test_mask.",
                    this);
                pendingLearningMode = null;
                ShowPpeModeChoices();
                return;
            }

            if (educationChoiceRoot != null)
                educationChoiceRoot.SetActive(false);
            testWorkPlanChoiceRoot.SetActive(true);
            ShowWorkPlanDescription();
            testConfinedSpaceButton?.Select();
            return;
        }

        if (testWorkPlanChoiceRoot != null)
            testWorkPlanChoiceRoot.SetActive(false);

        if (educationChoiceRoot == null)
        {
            Debug.LogError(
                "ScenarioDetailModal requires authored educationChoiceRoot before showing work-plan choices.",
                this);
            return;
        }

        educationChoiceRoot.SetActive(true);
        ShowWorkPlanDescription();
        incompletePpeButton?.Select();
    }

    private void ShowWorkPlanDescription()
    {
        if (ppeModeDescriptionRoot != null)
            ppeModeDescriptionRoot.SetActive(false);

        // Both confined-space and leak descriptions are authored on the first
        // scenario description object (1_ senario).
        SetOnlyActive(scenarioDescriptionObjects, 0);
    }

    private bool HasTestWorkPlanConfiguration()
    {
        return testWorkPlanChoiceRoot != null
            && testConfinedSpaceButton != null
            && testLeakResponseButton != null
            && testWorkPlanBackButton != null;
    }

    private void HideWorkPlanChoiceRoots()
    {
        if (testWorkPlanChoiceRoot != null)
            testWorkPlanChoiceRoot.SetActive(false);
    }

    private void ReturnToPpeEducationChoices()
    {
        if (openWithPpeModeChoices)
        {
            HideAndRestoreSelection();
            return;
        }

        if (educationChoiceRoot == null || ppeModeChoiceRoot == null)
            return;

        ppeModeChoiceRoot.SetActive(false);
        if (ppeModeDescriptionRoot != null)
            ppeModeDescriptionRoot.SetActive(false);
        SetOnlyActive(scenarioDescriptionObjects, selectedScenario);
        educationChoiceRoot.SetActive(true);
        incompletePpeButton?.Select();
    }

    private void ShowScenarioContent(int scenarioIndex)
    {
        SetOnlyActive(scenarioTitleObjects, scenarioIndex);
        SetOnlyActive(scenarioDescriptionObjects, scenarioIndex);
    }

    private bool HasScenarioContentObjects()
    {
        return HasAnyAssigned(scenarioTitleObjects) || HasAnyAssigned(scenarioDescriptionObjects);
    }

    private static bool HasAnyAssigned(GameObject[] objects)
    {
        if (objects == null)
            return false;

        foreach (GameObject target in objects)
            if (target != null)
                return true;

        return false;
    }

    private static void SetOnlyActive(GameObject[] objects, int activeIndex)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
            if (objects[i] != null)
                objects[i].SetActive(i == activeIndex);
    }

    private void ShowPrimaryActions()
    {
        if (educationChoiceRoot != null)
            educationChoiceRoot.SetActive(false);
        else
        {
            SetButtonVisible(incompletePpeButton, false);
            SetButtonVisible(standardTrainingButton, false);
            SetButtonVisible(trainingChoiceBackButton, false);
        }

        if (ppeModeChoiceRoot != null)
            ppeModeChoiceRoot.SetActive(false);
        HideWorkPlanChoiceRoots();
        if (ppeModeDescriptionRoot != null)
            ppeModeDescriptionRoot.SetActive(false);
        SetButtonVisible(trainingButton, true);
        SetButtonVisible(backButton, true);
    }

    private void ShowTrainingChoiceActions()
    {
        SetButtonVisible(trainingButton, false);
        SetButtonVisible(backButton, false);

        if (educationChoiceRoot != null)
            educationChoiceRoot.SetActive(true);
        else
        {
            SetButtonVisible(incompletePpeButton, true);
            SetButtonVisible(standardTrainingButton, true);
            SetButtonVisible(trainingChoiceBackButton, true);
        }

        if (ppeModeChoiceRoot != null)
            ppeModeChoiceRoot.SetActive(false);
        HideWorkPlanChoiceRoots();
        if (ppeModeDescriptionRoot != null)
            ppeModeDescriptionRoot.SetActive(false);
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        if (!visible)
            return;

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            label.gameObject.SetActive(true);
    }

    private void SetSelectionListeners(bool add)
    {
        if (!add)
        {
            foreach ((Button button, UnityAction action) in selectionListeners)
                if (button != null)
                    button.onClick.RemoveListener(action);
            selectionListeners.Clear();
            return;
        }

        if (scenarios == null)
            return;

        for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
        {
            ScenarioDetail detail = scenarios[scenarioIndex];
            if (detail?.SelectionButtons == null)
                continue;

            int capturedIndex = scenarioIndex;
            foreach (Button button in detail.SelectionButtons)
            {
                if (button == null)
                    continue;

                UnityAction listener = () => Show(capturedIndex);
                button.onClick.AddListener(listener);
                selectionListeners.Add((button, listener));
            }
        }
    }

    private bool TryShowFromMouseRaycast()
    {
        Camera raycastCamera = ResolveMouseRaycastCamera();
        if (raycastCamera == null || scenarios == null)
            return false;

        Ray ray = raycastCamera.ScreenPointToRay(GetMousePosition());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mouseRaycastMask, QueryTriggerInteraction.Collide))
            return false;

        Transform hitTransform = hit.transform;
        for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
        {
            ScenarioDetail detail = scenarios[scenarioIndex];
            if (detail?.SelectionButtons == null)
                continue;

            foreach (Button button in detail.SelectionButtons)
            {
                if (button == null)
                    continue;

                if (MatchesSelectionTarget(hitTransform, button.transform))
                {
                    Show(scenarioIndex);
                    Debug.Log($"ScenarioDetailModal mouse physics selected scenario {scenarioIndex + 1} via {hitTransform.name}.", hit.collider);
                    return true;
                }
            }
        }

        Debug.Log($"ScenarioDetailModal mouse physics hit {hitTransform.name}, but it is not registered as a scenario selection button.", hit.collider);
        return false;
    }

    private static bool MatchesSelectionTarget(Transform hitTransform, Transform selectionTransform)
    {
        if (hitTransform == selectionTransform || hitTransform.IsChildOf(selectionTransform) || selectionTransform.IsChildOf(hitTransform))
            return true;

        if (hitTransform.parent != selectionTransform.parent)
            return false;

        if (hitTransform is not RectTransform hitRect || selectionTransform is not RectTransform selectionRect)
            return false;

        return Vector2.SqrMagnitude(hitRect.anchoredPosition - selectionRect.anchoredPosition) < 0.01f;
    }

    private Camera ResolveMouseRaycastCamera()
    {
        if (mouseRaycastCamera != null)
            return mouseRaycastCamera;

        if (ownerCanvas != null && ownerCanvas.worldCamera != null)
            return ownerCanvas.worldCamera;

        return Camera.main;
    }

    private void LogModalTransformDiagnostics()
    {
        if (modalRoot == null)
            return;

        RectTransform rectTransform = modalRoot.transform as RectTransform;
        Canvas parentCanvas = modalRoot.GetComponentInParent<Canvas>(true);
        string rectInfo = rectTransform == null
            ? "no RectTransform"
            : $"anchored={rectTransform.anchoredPosition}, local={rectTransform.localPosition}, world={rectTransform.position}, scale={rectTransform.lossyScale}, size={rectTransform.rect.size}";

        string canvasInfo = parentCanvas == null
            ? "no parent Canvas"
            : $"canvas={parentCanvas.name}, sortingOrder={parentCanvas.sortingOrder}, overrideSorting={parentCanvas.overrideSorting}, worldCamera={parentCanvas.worldCamera?.name ?? "null"}";

        Debug.Log($"ScenarioDetailModal modal diagnostics: active={modalRoot.activeInHierarchy}, {rectInfo}, {canvasInfo}", modalRoot);
    }

    private void SetModalHierarchyActive(bool visible)
    {
        if (modalRoot == null)
            return;

        if (visible)
        {
            // Enabling a saved-inactive Modal Canvas is required for the modal to render.
            // Do not disable that canvas on hide — XRI TrackedDeviceGraphicRaycaster can
            // throw KeyNotFoundException when a canvas with null eventCamera is deactivated.
            Canvas parentCanvas = modalRoot.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
                parentCanvas.gameObject.SetActive(true);
        }

        modalRoot.SetActive(visible);
    }

    private void EnsureModalPanelActive()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(true);
            return;
        }

        if (modalRoot == null)
            return;

        Transform panel = modalRoot.transform.Find("Modal Panel");
        if (panel != null)
            panel.gameObject.SetActive(true);
    }

    private void SetSelectionUiVisible(bool visible)
    {
        if (scenarioSelectionRoot != null)
            scenarioSelectionRoot.SetActive(visible);

        bool hidExtra = false;
        if (additionalSelectionUiRoots != null)
        {
            foreach (GameObject root in additionalSelectionUiRoots)
            {
                if (root == null)
                    continue;
                root.SetActive(visible);
                hidExtra = true;
            }
        }

        // Fallback when Heading was not wired in the scene save.
        if (!hidExtra && scenarioSelectionRoot != null && scenarioSelectionRoot.transform.parent != null)
        {
            foreach (Transform sibling in scenarioSelectionRoot.transform.parent)
            {
                if (sibling != null && sibling.name == "Heading")
                    sibling.gameObject.SetActive(visible);
            }
        }
    }

    private void LogPointerRaycastDiagnostics()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("ScenarioDetailModal pointer diagnostics: no active EventSystem.", this);
            return;
        }

        PointerEventData pointerEventData = new(EventSystem.current)
        {
            position = GetMousePosition()
        };
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerEventData, results);

        if (results.Count == 0)
        {
            Debug.Log("ScenarioDetailModal pointer diagnostics: no UI raycast hits.", this);
            return;
        }

        int count = Mathf.Min(results.Count, 5);
        for (int i = 0; i < count; i++)
        {
            RaycastResult result = results[i];
            Debug.Log($"ScenarioDetailModal pointer diagnostics hit {i}: {result.gameObject.name}", result.gameObject);
        }
    }

    private static bool WasMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }
}
