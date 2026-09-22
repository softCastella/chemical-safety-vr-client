using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Inspector-authored voice and presentation flow for the PPE Room hand-test scene.
/// The director owns progression state, while existing UI and XR components remain
/// responsible for their own interaction behavior.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEVoiceFlowDirector : MonoBehaviour
{
    public enum FlowState
    {
        Welcome,
        NameInput,
        ControllerRay,
        ControllerMarker,
        ControllerRayT,
        ControllerPanel,
        CardIntro,
        ModalDetail,
        EducationSelected,
        PpeEducationSelected,
        TeleportInstruction,
        PpeArea,
        Completed,
        TrainingSelected,
        TestSelected,
    }

    public enum ControllerGuideNarration
    {
        Education,
        Simple,
    }

    public enum ControllerGuideInput
    {
        None,
        Trigger,
        Grip,
        Joystick,
    }

    [Serializable]
    public sealed class VoiceStep
    {
        [Tooltip("Stable semantic identifier used in diagnostics and Inspector authoring.")]
        public string stepId;

        public FlowState state;

        [Tooltip("Clips are played in order. Leave slots empty until the final voice files are available.")]
        public AudioClip[] clips;

        [Tooltip("Optional scene-authored controller guide visual shown for each corresponding clip. Null entries keep the currently shown visual.")]
        public GameObject[] controllerGuideVisuals;

        [Tooltip("Optional scene-authored controller image shown together with this step's clip visual.")]
        public GameObject controllerGuideCompanionVisual;

        [Tooltip("Detailed controller education only. Wait for this right-controller input after the explanation clips finish.")]
        public ControllerGuideInput controllerExpectedInput;

        [Tooltip("Played when another controller-guide input is pressed while this step is waiting.")]
        public AudioClip controllerWrongInputClip;

        [Tooltip("Played when the expected controller-guide input is pressed.")]
        public AudioClip controllerCorrectInputClip;

        [Tooltip("Optional final clip played after the correct-input clip and before the next state.")]
        public AudioClip controllerCompletionClip;

        [Tooltip("Optional repeat clip used while this state waits for an input event.")]
        public AudioClip repeatClip;

        [Min(0f)] public float volume = 1f;
        [Min(0f)] public float repeatDelay = 6f;
        public bool waitForSignal;
        public FlowState nextState;
    }

    [Header("TEST OVERRIDE - Disable For Normal Play")]
    [Tooltip("When enabled, Play Mode begins at CardIntro so the scenario card narration and card selection UI can be tested without completing earlier steps. This takes precedence over Modal Detail and Teleport testing.")]
    [SerializeField] private bool m_StartAtCardIntroForTesting;
    [Tooltip("When enabled, Play Mode begins at ModalDetail for the serialized initial scenario. This takes precedence over Teleport testing. Disable this after focused modal testing.")]
    [SerializeField] private bool m_StartAtModalDetailForTesting;
    [Tooltip("When enabled, Play Mode begins at TeleportInstruction and then follows the regular teleport-to-PPE-area flow. Card and Modal test overrides take precedence when they are also enabled.")]
    [SerializeField] private bool m_StartAtTeleportForTesting;

    [Header("Flow")]
    [SerializeField] private bool m_StartOnEnable = true;
    [SerializeField, Min(0f)] private float m_StartDelayAfterFirstFrame = 1f;
    [SerializeField] private FlowState m_InitialState = FlowState.Welcome;
    [SerializeField] private VoiceStep[] m_VoiceSteps;

    [Header("Meta Account Welcome")]
    [Tooltip("Played when the Meta account has no local Welcome playback record, or when Meta identity is unavailable.")]
    [SerializeField] private AudioClip m_FirstMetaUserWelcomeClip;
    [Tooltip("Played only when the current app-scoped Meta account has a local Welcome playback record.")]
    [SerializeField] private AudioClip m_ReturningMetaUserWelcomeClip;
    [Tooltip("Maximum additional wait for an in-flight Meta identity request before Welcome safely falls back to the first-user clip.")]
    [SerializeField, Min(0f)] private float m_MetaWelcomeIdentityWaitSeconds = 2f;

    [Header("Controller Guide Narration")]
    [Tooltip("Selects which authored controller narration group runs after keyboard name submission.")]
    [SerializeField] private ControllerGuideNarration m_ControllerNarrationAfterName = ControllerGuideNarration.Education;
    [Tooltip("Long controller education narration. This is reserved for the keyboard A entry once that button receives behavior.")]
    [SerializeField] private VoiceStep[] m_ControllerEduVoiceSteps;
    [Tooltip("Short controller guide narration intended to run after keyboard name submission.")]
    [SerializeField] private VoiceStep[] m_ControllerSimpVoiceSteps;
    [Tooltip("Assign the authored head-fixed ControllerGuide_mini/Context content. Right A may reopen detailed education only while this guide content is visible in CardIntro or PpeArea.")]
    [SerializeField] private GameObject m_ControllerGuideMini;

    [Header("Flow Options")]
    [Tooltip("Temporarily bypasses the authored NameInput keyboard while preserving its scene object and references.")]
    [SerializeField] private bool m_SkipKeyboardNameInput;
    [SerializeField] private bool m_SkipUnassignedClips = true;
    [SerializeField] private bool m_LogTransitions = true;
    [SerializeField] private bool m_AutoShowFirstScenarioModal = true;
    [SerializeField, Min(0)] private int m_InitialScenarioIndex;

    [Header("Presentation Roots")]
    [Tooltip("Assign a visual/content child. Do not assign Window Canvas itself when it has a TrackedDeviceGraphicRaycaster.")]
    [SerializeField] private GameObject m_WindowPresentationRoot;
    [Tooltip("When greater than zero, the authored window prompt is hidden after this many seconds from the Welcome state.")]
    [SerializeField, Min(0f)] private float m_WindowPresentationAutoHideSeconds;
    [Tooltip("Assign a CanvasGroup on the authored window prompt root to fade it out before it is hidden.")]
    [SerializeField] private CanvasGroup m_WindowPresentationCanvasGroup;
    [SerializeField, Min(0f)] private float m_WindowPresentationFadeOutSeconds;
    [Tooltip("Assign the actual keyboard presentation root used by this scene.")]
    [SerializeField] private GameObject m_KeyboardPresentationRoot;
    [SerializeField] private GameObject m_ControllerGuideRoot;
    [SerializeField] private GameObject m_ScenarioCardCanvas;
    [Tooltip("Scenario Selection HUD or another child under Scenario Card Canvas. The XR Canvas root remains active after the modal opens.")]
    [SerializeField] private GameObject m_ScenarioSelectionRoot;
    [Tooltip("Optional exit teleport marker. It is shown only after the player has reached the PPE route markers, and is hidden again on the return to mode selection.")]
    [SerializeField] private GameObject m_ExitTeleportMarker;

    [Header("Locomotion Trial")]
    [Tooltip("복사 씬 전용. 켜면 MoveToPPE 음성 뒤에 Teleport_0으로 자동 이동하고, 이후 스틱 걷기만 허용한다. 원본 씬에서는 꺼 둔다.")]
    [SerializeField] private bool m_AutoMoveToPpeThenLocomotion;
    [SerializeField] private TeleportationProvider m_LocomotionTeleportProvider;
    [SerializeField] private Transform m_LocomotionStartDestination;
    [SerializeField] private Behaviour m_LocomotionMoveProvider;


    [Header("Controller Guide Steps")]
    [SerializeField] private GameObject m_ControllerRayStep;
    [SerializeField] private GameObject m_ControllerMarkerStep;
    [SerializeField] private GameObject m_ControllerRayTStep;
    [SerializeField] private GameObject m_ControllerPanelStep;
    [Tooltip("Quest 2 controller model roots shown from Welcome through the final controller-guide step. The tracked controller roots themselves remain active.")]
    [SerializeField] private Transform[] m_ControllerModelVisuals;

    [Header("Trigger Voice Skip")]
    [Tooltip("Assign the authored left Near-Far Interactor. Trigger voice skip is ignored while this interactor points at clickable UI.")]
    [SerializeField] private NearFarInteractor m_LeftVoiceSkipInteractor;
    [Tooltip("Assign the authored right Near-Far Interactor. Trigger voice skip is ignored while this interactor points at clickable UI.")]
    [SerializeField] private NearFarInteractor m_RightVoiceSkipInteractor;

    [Header("Existing Interaction Components")]
    [SerializeField] private ScenarioDetailModal m_ScenarioDetailModal;

    [Header("PPE Conditional Narration")]
    [Tooltip("방호복 착용 완료 여부를 판정하는 기존 컨트롤러입니다.")]
    [SerializeField] private PPEHazmatEquipController m_HazmatEquipController;
    [Tooltip("장갑·부츠 좌우의 실제 사용 처리 완료 여부를 읽는 기존 컨트롤러입니다.")]
    [SerializeField] private PPEEquipmentVisualController m_EquipmentVisualController;
    [SerializeField] private PPEActionPanelController m_MaskActionPanel;
    [SerializeField] private PPEActionPanelController m_HelmetActionPanel;
    [SerializeField] private PPEActionPanelController[] m_BootActionPanels;
    [SerializeField] private PPEActionPanelController m_TapeActionPanel;
    [Tooltip("켜면 장갑과 장화를 사용 처리하기 전에 방호복 착용을 필수로 검사합니다.")]
    [SerializeField] private bool m_EnforceHazmatBeforeGlovesAndBoots;
    [Tooltip("송기마스크를 잡았을 때 재생할 음성입니다.")]
    [SerializeField] private AudioClip m_MaskGrabVoice;
    [SerializeField] private AudioClip m_HelmetGrabVoice;
    [Tooltip("방호복 미착용 상태에서 헬멧을 사용 처리했을 때 재생하는 음성입니다.")]
    [SerializeField] private AudioClip m_HelmetUseWithoutHazmatVoice;
    [Tooltip("방호복 미착용 상태에서 부츠를 사용 처리했을 때 재생할 음성입니다.")]
    [SerializeField] private AudioClip m_BootUseWithoutHazmatVoice;
    [Tooltip("장갑·부츠 좌우 착용 전 테이프를 사용 처리했을 때 재생할 음성입니다.")]
    [SerializeField] private AudioClip m_TapeUseBeforeGlovesAndBootsVoice;
    [Tooltip("니트릴 내부 장갑을 입기 전에 바깥 화학장갑을 입으려 했을 때 재생할 음성입니다. 비워 두면 거절만 하고 이 클립은 재생하지 않습니다.")]
    [SerializeField] private AudioClip m_OuterGloveUseWithoutNitrileVoice;

    [Header("PPE Normal Grab Narration")]
    [Tooltip("세 방호복 진열 패널입니다. 어느 변형을 잡아도 같은 005 안내를 재생합니다.")]
    [SerializeField] private PPEActionPanelController[] m_HazmatActionPanels;
    [SerializeField] private PPEActionPanelController m_HarnessActionPanel;
    [SerializeField] private PPEActionPanelController m_SuppliedAirMaskActionPanel;
    [SerializeField] private PPEActionPanelController[] m_GloveActionPanels;
    [SerializeField] private PPEActionPanelController m_GoggleActionPanel;
    [SerializeField] private PPEActionPanelController m_FaceShieldActionPanel;
    [SerializeField] private PPEActionPanelController[] m_NitrileInnerGloveActionPanels;
    [Tooltip("켜면 좌·우 장갑이 공유하는 잡기 안내를 교육 모드 세션에서 최초 한 번만 재생합니다.")]
    [SerializeField] private bool m_PlayGloveGrabVoiceOncePerSession;
    [Tooltip("PPE 조건부 음성이 시작될 때 지연된 오답 음성을 취소할 모든 PPE 패널입니다.")]
    [SerializeField] private PPEActionPanelController[] m_AllPpeActionPanels;
    [SerializeField] private AudioClip m_HazmatGrabVoice;
    [SerializeField] private AudioClip m_BootGrabVoice;
    [SerializeField] private AudioClip m_HarnessGrabVoice;
    [SerializeField] private AudioClip m_SuppliedAirMaskGrabVoice;
    [SerializeField] private AudioClip m_GloveGrabVoice;
    [SerializeField] private AudioClip m_TapeGrabVoice;
    [SerializeField] private AudioClip m_GoggleGrabVoice;
    [SerializeField] private AudioClip m_FaceShieldGrabVoice;
    [SerializeField] private AudioClip m_NitrileInnerGloveGrabVoice;

    [Header("Work Plan Required PPE")]
    [Tooltip("밀폐공간 작업계획에서 입어야 하는 PPE 타입입니다. 좌·우 쌍은 각각 넣습니다. 진열은 숨기지 않습니다.")]
    [SerializeField] private PPEItemType[] m_ConfinedSpaceRequiredItemTypes =
    {
        PPEItemType.HazmatSuit,
        PPEItemType.RubberBootLeft,
        PPEItemType.RubberBootRight,
        PPEItemType.TacticalHarness,
        PPEItemType.GasMask,
        PPEItemType.ConstructionHelmet,
        PPEItemType.NitrileInnerGloveLeft,
        PPEItemType.NitrileInnerGloveRight,
        PPEItemType.RubberGloveLeft,
        PPEItemType.RubberGloveRight,
    };
    [Tooltip("노출/누출 시나리오에서 입어야 하는 PPE 타입입니다. 좌·우 쌍은 각각 넣습니다.")]
    [SerializeField] private PPEItemType[] m_LeakResponseRequiredItemTypes =
    {
        PPEItemType.HazmatSuit,
        PPEItemType.RubberBootLeft,
        PPEItemType.RubberBootRight,
        PPEItemType.NitrileInnerGloveLeft,
        PPEItemType.NitrileInnerGloveRight,
        PPEItemType.RubberGloveLeft,
        PPEItemType.RubberGloveRight,
        PPEItemType.SafetyGoggles,
        PPEItemType.FaceShield,
        PPEItemType.ConstructionHelmet,
    };

    [Header("PPE Progress Narration")]
    [SerializeField] private PPEFinaleController m_FinaleController;
    [SerializeField] private AudioClip m_TabletReleasedVoice;
    [SerializeField] private AudioClip m_HazmatPanelVoice;
    [SerializeField] private AudioClip m_HazmatEquippedVoice;
    [SerializeField] private AudioClip m_CenterMarkerArrivedVoice;
    [SerializeField] private AudioClip m_CenterMarkerCheckVoice;
    [SerializeField] private AudioClip m_NextPpeEquippedVoice;
    [SerializeField] private AudioClip m_AllPpeCompleteMoveMirrorVoice;
    [SerializeField] private AudioClip m_MirrorCheckVoice;
    [SerializeField] private AudioClip m_QuizVoice;
    [SerializeField] private AudioClip m_EduEndVoice;
    [SerializeField] private AudioClip m_IncompletePpeVoice;
    [SerializeField] private AudioClip m_IncompleteTabletVoice;
    [Tooltip("교육 모드의 PPE/태블릿 미완료 음성을 부족 조건별로 분리해 재생합니다.")]
    [SerializeField] private bool m_UseSeparateEducationIncompleteVoices;
    [SerializeField] private PPEQuizController m_QuizController;

    [Header("Training Mode Voice")]
    [SerializeField] private AudioClip m_TrainingModeSelectedVoice;
    [SerializeField] private AudioClip m_TrainingMoveToPpeVoice;
    [SerializeField] private AudioClip m_TrainingCheckPpeTabletVoice;
    [SerializeField] private AudioClip m_TrainingWrongButtonVoice;
    [SerializeField] private AudioClip m_TrainingMirrorCheckVoice;
    [SerializeField] private AudioClip m_TrainingIncompletePpeVoice;
    [SerializeField] private AudioClip m_TrainingQuizVoice;
    [SerializeField] private AudioClip m_TrainingEndVoice;
    [SerializeField] private string m_TrainingWrongFeedbackMessage = "올바른 답을 찾아주세요.";

    [Header("Test Mode Voice")]
    [SerializeField] private AudioClip m_TestModeSelectedVoice;
    [SerializeField] private AudioClip m_TestMoveToPpeVoice;
    [SerializeField] private AudioClip m_TestEndVoice;

    [Header("Education Work Plan Voice")]
    [Tooltip("교육 모드 버튼을 눌렀을 때 재생합니다. 예전 MODAL_004 EduSelect입니다.")]
    [SerializeField] private AudioClip m_EducationModeSelectedVoice;
    [Tooltip("밀폐공간 작업계획 버튼을 눌렀을 때 재생합니다.")]
    [SerializeField] private AudioClip m_ConfinedSpaceSelectedVoice;
    [Tooltip("누출 시나리오 버튼을 눌렀을 때 재생합니다.")]
    [SerializeField] private AudioClip m_LeakResponseSelectedVoice;
    [Tooltip("방호복을 입은 뒤 다른 방호복을 입으려 할 때 재생합니다.")]
    [SerializeField] private AudioClip m_HazmatAlreadyEquippedVoice;
    [Tooltip("선택한 작업계획에 없는 PPE를 입으려 할 때 재생합니다.")]
    [SerializeField] private AudioClip m_WorkPlanMismatchVoice;
    [Tooltip("중간 엑시트 포인트에 도착했을 때 재생합니다.")]
    [SerializeField] private AudioClip m_MidExitStopVoice;

    private FlowState m_State;
    private Coroutine m_StepRoutine;
    private Coroutine m_RepeatRoutine;
    private Coroutine m_PpeConditionalVoiceRoutine;
    private Coroutine m_ModeSelectionRoutine;
    private Coroutine m_WindowPresentationAutoHideRoutine;
    private int m_TransitionVersion;
    private bool m_TeleportInputStarted;
    private bool m_BootGrabVoicePlayed;
    private bool m_GloveGrabVoicePlayed;
    private bool m_NitrileInnerGloveGrabVoicePlayed;
    private bool m_TapeGrabVoicePlayed;
    private bool m_HazmatGrabVoicePlayed;
    private bool m_AllPpeMoveMirrorVoicePlayed;
    private bool m_TabletReleasedVoicePlayed;
    private bool m_HazmatEquippedVoicePlayed;
    private bool m_CenterMarkerArrivedVoicePlayed;
    private bool m_MirrorMarkerArrivedVoicePlayed;
    private bool m_MidExitVoiceExclusive;
    private bool m_HasLoggedMissingVoicePlayer;
    private bool m_HasLoggedMissingControllerModelVisuals;
    private bool m_WindowPresentationAutoHidden;
    private bool m_CardIntroVoicePlayedThisRun;
    private bool m_ModeSelectionFailed;
    private float m_ModeSessionStartedAt;
    private float m_ModeSessionElapsed;
    private string m_ModeSessionId;
    private int m_TestPpeWrongChoiceCount;
    private int m_TestQuizCorrectCount;
    private int m_TestQuizQuestionCount;
    private bool m_ModeSessionCompletionRecorded;
    private InputAction m_LeftTriggerSkipAction;
    private InputAction m_RightTriggerSkipAction;
    private InputAction m_RightControllerEducationAction;
    private InputAction m_RightControllerGuideTriggerAction;
    private InputAction m_RightControllerGuideGripAction;
    private InputAction m_RightControllerGuideJoystickAction;
    private ControllerGuideNarration m_ActiveControllerNarration;
    private bool m_ReturnToNameInputAfterControllerEducation;
    private bool m_ControllerEducationCompleted;
    private bool m_IsAwaitingControllerGuideInput;
    private VoiceStep m_ControllerGuideInputStep;
    private Coroutine m_ControllerGuideInputFeedbackRoutine;
    private int m_LastControllerEducationRequestFrame = -1;
    private FlowState m_ControllerEducationResumeState;
    private bool m_HasControllerEducationResumeState;
    private bool m_ControllerEducationSilentResumePending;
    private readonly HashSet<string> m_WarnedMissingVoiceSteps = new();
    private bool m_HasLoggedMissingMetaWelcomeClip;
    private readonly Dictionary<PPEActionPanelController, bool> m_TestFirstPpeSelections = new();
    private readonly List<GameObject> m_HiddenWorkPlanInspectionItems = new();
    private Coroutine m_LocomotionTrialRoutine;
    private PPEActionPanelController[] m_LivePpeActionPanels;

    public FlowState CurrentState => m_State;
    public bool IsAwaitingControllerGuideInput => m_IsAwaitingControllerGuideInput;
    public ControllerGuideInput ExpectedControllerGuideInput =>
        m_IsAwaitingControllerGuideInput && m_ControllerGuideInputStep != null
            ? m_ControllerGuideInputStep.controllerExpectedInput
            : ControllerGuideInput.None;
    public bool IsActivePpeModeSession =>
        m_State == FlowState.TeleportInstruction ||
        m_State == FlowState.PpeArea ||
        m_State == FlowState.TrainingSelected ||
        m_State == FlowState.TestSelected;
    public ScenarioDetailModal.PpeLearningMode ActiveLearningMode { get; private set; } =
        ScenarioDetailModal.PpeLearningMode.Education;
    public string ActiveModeSessionId => m_ModeSessionId;
    public bool AllowsPpeActionSfx =>
        ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Test &&
        !m_MidExitVoiceExclusive;
    public bool AllowsPpeChoiceVoice =>
        ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education &&
        !m_MidExitVoiceExclusive;
    public ScenarioDetailModal.PpeWorkPlan ActiveWorkPlan { get; private set; } =
        ScenarioDetailModal.PpeWorkPlan.None;
    public PPEItemType[] ActiveRequiredPpeItemTypes =>
        ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.None
            ? Array.Empty<PPEItemType>()
            : GetRequiredWearItemTypes(ActiveWorkPlan);

    public bool IsPpeItemWorn(PPEItemType itemType)
    {
        if (itemType == PPEItemType.HazmatSuit)
            return m_HazmatEquipController != null && m_HazmatEquipController.IsEquipped;

        return m_EquipmentVisualController != null &&
            m_EquipmentVisualController.IsItemUsed(itemType);
    }
    public string SubmittedName { get; private set; } = string.Empty;

    public void EnsureDefaultVoiceSteps()
    {
        if (m_VoiceSteps == null || m_VoiceSteps.Length == 0)
            m_VoiceSteps = CreateDefaultVoiceSteps();
        if (m_ControllerEduVoiceSteps == null || m_ControllerEduVoiceSteps.Length == 0)
            m_ControllerEduVoiceSteps = CreateDefaultControllerEduVoiceSteps();
        if (m_ControllerSimpVoiceSteps == null || m_ControllerSimpVoiceSteps.Length == 0)
            m_ControllerSimpVoiceSteps = CreateDefaultControllerSimpVoiceSteps();
    }

    private void Reset()
    {
        EnsureDefaultVoiceSteps();
    }

    private void OnEnable()
    {
        m_ActiveControllerNarration = m_ControllerNarrationAfterName;
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        m_ControllerEducationCompleted = false;
        m_LastControllerEducationRequestFrame = -1;
        PreloadHazmatVoiceClips();
        SubscribeToSignals(true);
        CreateAndEnableTriggerSkipActions();
        CreateAndEnableControllerEducationAction();
        CreateAndEnableControllerGuideInputActions();

        if (m_StartOnEnable)
            ApplyControllerHeldVisuals(UsesControllerModels(GetConfiguredStartupState()));
    }

    private void Start()
    {
        if (!m_StartOnEnable)
            return;

        // Every scene OnEnable has completed before Start. Reassert the authored
        // startup presentation here so equipment initializers cannot turn bare
        // hands back on after the voice director selected controller models.
        FlowState startupState = GetConfiguredStartupState();
        ApplyControllerHeldVisuals(UsesControllerModels(startupState));
        StartCoroutine(StartFlowAfterFirstRenderedFrame());
    }

    private void Update()
    {
        if (m_AllPpeMoveMirrorVoicePlayed || m_MirrorMarkerArrivedVoicePlayed ||
            m_State != FlowState.PpeArea ||
            m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped ||
            m_EquipmentVisualController == null || !m_EquipmentVisualController.AreAllRequiredSlotsUsed)
        {
            return;
        }

        m_AllPpeMoveMirrorVoicePlayed = true;
        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
            PlayPpeConditionalVoice(m_AllPpeCompleteMoveMirrorVoice);
        else if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training)
            PlayPpeConditionalVoice(m_TrainingMirrorCheckVoice);
    }

    private IEnumerator StartFlowAfterFirstRenderedFrame()
    {
        // Let the XR camera submit at least one frame before voice playback begins.
        yield return null;
        yield return new WaitForEndOfFrame();

        if (m_StartDelayAfterFirstFrame > 0f)
            yield return new WaitForSecondsRealtime(m_StartDelayAfterFirstFrame);

        if (GetConfiguredStartupState() == FlowState.Welcome &&
            m_MetaWelcomeIdentityWaitSeconds > 0f)
        {
            float elapsed = 0f;
            while (MetaPlatformIdentityProbe.IsIdentityRequestInFlight &&
                   MetaPlatformIdentityProbe.CurrentWelcomeState ==
                       MetaPlatformIdentityProbe.AccountWelcomeState.Unknown &&
                   elapsed < m_MetaWelcomeIdentityWaitSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (isActiveAndEnabled && m_StartOnEnable)
        {
            if (m_StartAtCardIntroForTesting)
                StartCardIntroForTesting();
            else if (m_StartAtModalDetailForTesting)
                StartModalDetailForTesting();
            else if (m_StartAtTeleportForTesting)
                StartTeleportForTesting();
            else
                StartFlow();
        }
    }

    private void OnDisable()
    {
        SubscribeToSignals(false);
        DisableAndDisposeTriggerSkipActions();
        DisableAndDisposeControllerEducationAction();
        DisableAndDisposeControllerGuideInputActions();
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        StopModeSelectionRoutine();
        StopFlowPlayback();
        HideAvailableControllerModelsOnDisable();
        if (m_LocomotionTrialRoutine != null)
        {
            StopCoroutine(m_LocomotionTrialRoutine);
            m_LocomotionTrialRoutine = null;
        }
        SetLocomotionMoveEnabled(false);
        if (m_WindowPresentationAutoHideRoutine != null)
        {
            StopCoroutine(m_WindowPresentationAutoHideRoutine);
            m_WindowPresentationAutoHideRoutine = null;
        }
        PPEControllerTeleportModeManager.SetVoiceMovementGate(true);
    }

    private void CreateAndEnableTriggerSkipActions()
    {
        if (m_LeftVoiceSkipInteractor == null || m_RightVoiceSkipInteractor == null)
        {
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} on '{name}' requires the authored left/right Near-Far Interactor references for interaction-safe Trigger voice skip.",
                this);
            return;
        }

        m_LeftTriggerSkipAction = new InputAction(
            "PPE Voice Skip Left Trigger",
            InputActionType.Button,
            "<XRController>{LeftHand}/trigger");
        m_RightTriggerSkipAction = new InputAction(
            "PPE Voice Skip Right Trigger",
            InputActionType.Button,
            "<XRController>{RightHand}/trigger");

        m_LeftTriggerSkipAction.AddBinding("<OculusTouchController>{LeftHand}/trigger");
        m_RightTriggerSkipAction.AddBinding("<OculusTouchController>{RightHand}/trigger");
        m_LeftTriggerSkipAction.performed += OnTriggerSkipPerformed;
        m_RightTriggerSkipAction.performed += OnTriggerSkipPerformed;
        m_LeftTriggerSkipAction.Enable();
        m_RightTriggerSkipAction.Enable();
    }

    private void DisableAndDisposeTriggerSkipActions()
    {
        DisposeTriggerSkipAction(ref m_LeftTriggerSkipAction);
        DisposeTriggerSkipAction(ref m_RightTriggerSkipAction);
    }

    private void CreateAndEnableControllerEducationAction()
    {
        m_RightControllerEducationAction = new InputAction(
            "PPE Controller Education Right A",
            InputActionType.Button,
            "<XRController>{RightHand}/primaryButton");
        m_RightControllerEducationAction.AddBinding(
            "<OculusTouchController>{RightHand}/buttonSouth");
        m_RightControllerEducationAction.performed += OnControllerEducationActionPerformed;
        m_RightControllerEducationAction.Enable();
    }

    private void DisableAndDisposeControllerEducationAction()
    {
        if (m_RightControllerEducationAction == null)
            return;

        m_RightControllerEducationAction.performed -= OnControllerEducationActionPerformed;
        m_RightControllerEducationAction.Dispose();
        m_RightControllerEducationAction = null;
    }

    private void OnControllerEducationActionPerformed(InputAction.CallbackContext context)
    {
        NotifyControllerEducationRequested();
    }

    private void CreateAndEnableControllerGuideInputActions()
    {
        m_RightControllerGuideTriggerAction = new InputAction(
            "PPE Controller Guide Right Trigger",
            InputActionType.Button,
            "<XRController>{RightHand}/trigger");
        m_RightControllerGuideTriggerAction.AddBinding(
            "<OculusTouchController>{RightHand}/trigger");

        m_RightControllerGuideGripAction = new InputAction(
            "PPE Controller Guide Right Grip",
            InputActionType.Button,
            "<XRController>{RightHand}/grip");
        m_RightControllerGuideGripAction.AddBinding(
            "<OculusTouchController>{RightHand}/grip");

        m_RightControllerGuideJoystickAction = new InputAction(
            "PPE Controller Guide Right Joystick",
            InputActionType.Button,
            "<XRController>{RightHand}/primary2DAxis");
        m_RightControllerGuideJoystickAction.AddBinding(
            "<OculusTouchController>{RightHand}/primary2DAxis");

        m_RightControllerGuideTriggerAction.performed += OnControllerGuideTriggerPerformed;
        m_RightControllerGuideGripAction.performed += OnControllerGuideGripPerformed;
        m_RightControllerGuideJoystickAction.performed += OnControllerGuideJoystickPerformed;
        m_RightControllerGuideTriggerAction.Enable();
        m_RightControllerGuideGripAction.Enable();
        m_RightControllerGuideJoystickAction.Enable();
    }

    private void DisableAndDisposeControllerGuideInputActions()
    {
        DisposeControllerGuideInputAction(
            ref m_RightControllerGuideTriggerAction,
            OnControllerGuideTriggerPerformed);
        DisposeControllerGuideInputAction(
            ref m_RightControllerGuideGripAction,
            OnControllerGuideGripPerformed);
        DisposeControllerGuideInputAction(
            ref m_RightControllerGuideJoystickAction,
            OnControllerGuideJoystickPerformed);
    }

    private static void DisposeControllerGuideInputAction(
        ref InputAction action,
        Action<InputAction.CallbackContext> callback)
    {
        if (action == null)
            return;

        action.performed -= callback;
        action.Dispose();
        action = null;
    }

    private void OnControllerGuideTriggerPerformed(InputAction.CallbackContext context)
    {
        NotifyControllerGuideInput(ControllerGuideInput.Trigger);
    }

    private void OnControllerGuideGripPerformed(InputAction.CallbackContext context)
    {
        NotifyControllerGuideInput(ControllerGuideInput.Grip);
    }

    private void OnControllerGuideJoystickPerformed(InputAction.CallbackContext context)
    {
        NotifyControllerGuideInput(ControllerGuideInput.Joystick);
    }

    private void OnTriggerSkipPerformed(InputAction.CallbackContext context)
    {
        if (IsControllerGuideInputPracticeActive())
            return;

        if (AudioManager.Instance == null ||
            (!AudioManager.Instance.IsVoicePlaying && !AudioManager.Instance.IsVoiceLoading))
        {
            return;
        }

        if (IsTriggerReservedForClickableUi(context.action) ||
            IsTriggerReservedForPpeBodyProximityWear(context.action))
            return;

        SkipCurrentVoice();
    }

    private void PreloadHazmatVoiceClips()
    {
        if (m_HazmatPanelVoice != null &&
            m_HazmatPanelVoice.loadState == AudioDataLoadState.Unloaded)
        {
            m_HazmatPanelVoice.LoadAudioData();
        }

        if (m_HazmatEquippedVoice != null &&
            m_HazmatEquippedVoice.loadState == AudioDataLoadState.Unloaded)
        {
            m_HazmatEquippedVoice.LoadAudioData();
        }

        if (m_HazmatAlreadyEquippedVoice != null &&
            m_HazmatAlreadyEquippedVoice.loadState == AudioDataLoadState.Unloaded)
        {
            m_HazmatAlreadyEquippedVoice.LoadAudioData();
        }
    }

    public bool TrySkipVoiceFromGameViewBackgroundClick()
    {
        if (IsControllerGuideInputPracticeActive())
            return false;

        if (AudioManager.Instance == null ||
            (!AudioManager.Instance.IsVoicePlaying && !AudioManager.Instance.IsVoiceLoading))
        {
            return false;
        }

        SkipCurrentVoice();
        return true;
    }

    private void SkipCurrentVoice()
    {
        if (AudioManager.Instance == null)
            return;

        VoiceStep step = FindStep(m_State);

        // Interactive detailed-controller steps must finish their explanation
        // and feedback sequence before accepting input or changing state.
        if (step != null && step.controllerExpectedInput != ControllerGuideInput.None)
        {
            AudioManager.Instance.StopVoice();
            return;
        }

        // The PPE area also plays conditional narrations that do not belong to
        // the state's step coroutine. Stop both kinds without completing the
        // PPE lesson state early.
        if (m_State == FlowState.PpeArea)
        {
            CancelPendingPpeWrongChoiceVoices();
            StopRepeatPlayback();
            AudioManager.Instance.StopVoice();
            StopCurrentStepRoutine();
            return;
        }

        // Selection-gated states still require their authored keyboard, card,
        // or teleport event. Trigger skips only the narration there.
        if (step == null || step.waitForSignal || step.state == FlowState.CardIntro)
        {
            AudioManager.Instance.StopVoice();
            return;
        }

        // Non-gated instructional steps use the same next-state transition as
        // natural voice completion, so skipping cannot leave the flow stranded.
        FlowState nextState = ResolveControllerEducationNextState(step.state, step.nextState);
        if (nextState != m_State)
            TransitionTo(nextState);
        else
            AudioManager.Instance.StopVoice();
    }

    public void NotifyControllerGuideInput(ControllerGuideInput input)
    {
        if (input == ControllerGuideInput.None ||
            !m_IsAwaitingControllerGuideInput ||
            m_ControllerGuideInputStep == null ||
            m_ControllerGuideInputFeedbackRoutine != null ||
            FindStep(m_State) != m_ControllerGuideInputStep)
        {
            return;
        }

        VoiceStep step = m_ControllerGuideInputStep;
        bool isCorrect = input == step.controllerExpectedInput;
        m_IsAwaitingControllerGuideInput = false;
        m_ControllerGuideInputFeedbackRoutine = StartCoroutine(
            PlayControllerGuideInputFeedback(step, isCorrect, m_TransitionVersion));
    }

    private bool IsControllerGuideInputPracticeActive()
    {
        VoiceStep step = FindStep(m_State);
        return step != null &&
            step.controllerExpectedInput != ControllerGuideInput.None;
    }

    private void StopCurrentStepRoutine()
    {
        if (m_StepRoutine == null)
            return;

        StopCoroutine(m_StepRoutine);
        m_StepRoutine = null;
    }

    private void DisposeTriggerSkipAction(ref InputAction action)
    {
        if (action == null)
            return;

        action.performed -= OnTriggerSkipPerformed;
        action.Dispose();
        action = null;
    }

    private bool IsTriggerReservedForClickableUi(InputAction sourceAction)
    {
        NearFarInteractor interactor = sourceAction == m_LeftTriggerSkipAction
            ? m_LeftVoiceSkipInteractor
            : sourceAction == m_RightTriggerSkipAction
                ? m_RightVoiceSkipInteractor
                : null;
        if (interactor == null || !interactor.isActiveAndEnabled ||
            !interactor.TryGetCurrentUIRaycastResult(out RaycastResult raycastResult) ||
            raycastResult.gameObject == null)
        {
            return false;
        }

        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(raycastResult.gameObject) != null;
    }

    private bool IsTriggerReservedForPpeBodyProximityWear(InputAction sourceAction)
    {
        NearFarInteractor interactor = sourceAction == m_LeftTriggerSkipAction
            ? m_LeftVoiceSkipInteractor
            : sourceAction == m_RightTriggerSkipAction
                ? m_RightVoiceSkipInteractor
                : null;
        PPEActionPanelController panel = PPEActionPanelController.ActivePanelOwner;
        return interactor != null &&
            panel != null &&
            panel.IsBodyProximityActivateAttempt() &&
            panel.IsSelectedByInteractor(interactor);
    }

    private FlowState GetConfiguredStartupState()
    {
        return m_StartAtCardIntroForTesting
            ? FlowState.CardIntro
            : m_StartAtModalDetailForTesting
                ? FlowState.ModalDetail
                : m_StartAtTeleportForTesting
                    ? FlowState.TeleportInstruction
                    : m_InitialState;
    }

    public void StartFlow()
    {
        StartFlowAtState(m_InitialState);
    }

    /// <summary>
    /// Explicit focused-test entry point for the authored teleport instruction.
    /// It uses the same presentation, voice, movement gate, and state transition
    /// path as the regular flow after PPE education selection.
    /// </summary>
    public void StartTeleportForTesting()
    {
        StartFlowAtState(FlowState.TeleportInstruction);
    }

    private void StartFlowAtState(FlowState initialState)
    {
        StopModeSelectionRoutine();
        StopFlowPlayback();
        if (m_WindowPresentationAutoHideRoutine != null)
        {
            StopCoroutine(m_WindowPresentationAutoHideRoutine);
            m_WindowPresentationAutoHideRoutine = null;
        }
        m_WindowPresentationAutoHidden = false;
        SetWindowPresentationAlpha(1f);
        m_TransitionVersion++;
        m_WarnedMissingVoiceSteps.Clear();
        m_State = initialState;
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        m_ControllerEducationCompleted = false;
        m_LastControllerEducationRequestFrame = -1;
        m_TeleportInputStarted = false;
        m_BootGrabVoicePlayed = false;
        m_HazmatGrabVoicePlayed = false;
        m_AllPpeMoveMirrorVoicePlayed = false;
        m_TabletReleasedVoicePlayed = false;
        m_HazmatEquippedVoicePlayed = false;
        m_CenterMarkerArrivedVoicePlayed = false;
        m_MirrorMarkerArrivedVoicePlayed = false;
        ActiveLearningMode = ScenarioDetailModal.PpeLearningMode.Education;
        ActiveWorkPlan = ScenarioDetailModal.PpeWorkPlan.None;
        ApplyActiveWorkPlan();
        ResetModeSessionTracking();
        SubmittedName = string.Empty;
        SetLocomotionMoveEnabled(false);

        // Authored test entry points at or beyond TeleportInstruction have already
        // passed the movement tutorial. Earlier states keep the regular voice gate.
        PPEControllerTeleportModeManager.SetVoiceMovementGate(
            initialState == FlowState.TeleportInstruction
            || initialState == FlowState.PpeArea
            || initialState == FlowState.Completed);

        if (m_ScenarioDetailModal != null)
            m_ScenarioDetailModal.Hide();

        ApplyPresentation(m_State);
        LogTransition(m_State);
        EnterState(m_State);
    }

    /// <summary>
    /// Explicit focused-test entry point. It establishes the same ModalDetail
    /// state ownership as the regular card-selection path without simulating a
    /// card click or unlocking later teleport progression.
    /// </summary>
    public void StartModalDetailForTesting()
    {
        StopFlowPlayback();
        m_TransitionVersion++;
        m_WarnedMissingVoiceSteps.Clear();
        m_State = FlowState.ModalDetail;
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        m_ControllerEducationCompleted = false;
        m_TeleportInputStarted = false;
        m_TabletReleasedVoicePlayed = false;
        m_HazmatEquippedVoicePlayed = false;
        SubmittedName = string.Empty;

        // Preserve the regular progression gate: only the actual modal choice
        // may later open movement through its existing event path.
        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);

        if (m_ScenarioDetailModal != null)
            m_ScenarioDetailModal.Hide();

        ApplyPresentation(m_State);
        LogTransition(m_State);
        EnterState(m_State, forceShowModal: true);
    }

    /// <summary>
    /// Explicit focused-test entry point for the scenario-card sequence. It uses
    /// the regular CardIntro state, so card narration, card visibility, modal
    /// transition, and subsequent movement gate remain identical to the flow.
    /// </summary>
    public void StartCardIntroForTesting()
    {
        StopFlowPlayback();
        m_TransitionVersion++;
        m_WarnedMissingVoiceSteps.Clear();
        m_State = FlowState.CardIntro;
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        m_ControllerEducationCompleted = false;
        m_TeleportInputStarted = false;
        m_TabletReleasedVoicePlayed = false;
        m_HazmatEquippedVoicePlayed = false;
        SubmittedName = string.Empty;

        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);

        if (m_ScenarioDetailModal != null)
            m_ScenarioDetailModal.Hide();

        ApplyPresentation(m_State);
        LogTransition(m_State);
        EnterState(m_State);
    }

    public void StopFlow()
    {
        StopFlowPlayback();
        PPEControllerTeleportModeManager.SetVoiceMovementGate(true);
        m_State = FlowState.Completed;
        ApplyPresentation(m_State);
    }

    public void NotifyNameSubmitted()
    {
        NotifyNameSubmitted(string.Empty);
    }

    public void NotifyNameSubmitted(string submittedName)
    {
        if (m_State != FlowState.NameInput)
            return;

        if (string.IsNullOrWhiteSpace(submittedName))
            return;

        SubmittedName = submittedName;
        m_ReturnToNameInputAfterControllerEducation = false;
        ClearControllerEducationResumeRoute();
        m_ActiveControllerNarration = m_ControllerNarrationAfterName;
        TransitionTo(GetNameSubmissionNextState());
    }

    /// <summary>
    /// Starts the detailed controller guide from the keyboard state. Physical
    /// right-A and the authored XR UI entry both call this single state owner.
    /// </summary>
    public void NotifyControllerEducationRequested()
    {
        bool isKeyboardEntry = m_State == FlowState.NameInput;
        bool isBypassedKeyboardControllerEntry = m_SkipKeyboardNameInput
            && IsControllerGuideState(m_State)
            && m_ActiveControllerNarration == ControllerGuideNarration.Simple;
        bool isVisibleMiniGuideEntry = IsVisibleMiniControllerEducationEntry();
        if ((!isKeyboardEntry && !isBypassedKeyboardControllerEntry && !isVisibleMiniGuideEntry)
            || m_ReturnToNameInputAfterControllerEducation
            || m_LastControllerEducationRequestFrame == Time.frameCount)
        {
            return;
        }

        m_LastControllerEducationRequestFrame = Time.frameCount;
        if (isVisibleMiniGuideEntry)
        {
            m_ControllerEducationResumeState = m_State;
            m_HasControllerEducationResumeState = true;
            SetActive(m_ControllerGuideMini, false);
        }
        else
        {
            ClearControllerEducationResumeRoute();
        }

        m_ReturnToNameInputAfterControllerEducation = true;
        m_ActiveControllerNarration = ControllerGuideNarration.Education;
        if (isBypassedKeyboardControllerEntry)
            RestartControllerGuideAtRayStep();
        else
            TransitionTo(FlowState.ControllerRay);
    }

    private bool IsVisibleMiniControllerEducationEntry()
    {
        if (m_ControllerGuideMini == null || !m_ControllerGuideMini.activeInHierarchy)
            return false;

        return m_State == FlowState.CardIntro || m_State == FlowState.PpeArea;
    }

    private void ClearControllerEducationResumeRoute()
    {
        m_HasControllerEducationResumeState = false;
        m_ControllerEducationSilentResumePending = false;
    }

    private void RestartControllerGuideAtRayStep()
    {
        StopFlowPlayback();
        m_TransitionVersion++;
        m_State = FlowState.ControllerRay;
        ApplyPresentation(m_State);
        LogTransition(m_State);
        EnterState(m_State);
    }

    public void NotifyScenarioModalShown(int scenarioIndex)
    {
        if (scenarioIndex < 0)
            return;

        if (m_ScenarioDetailModal != null && m_ScenarioDetailModal.OpensWithPpeModeChoices)
        {
            if (m_State == FlowState.CardIntro || m_State == FlowState.ModalDetail)
                TransitionTo(FlowState.PpeEducationSelected, showScenarioModal: false);
            return;
        }

        if (m_State == FlowState.CardIntro)
        {
            // The card proxy has already opened the selected scenario modal.
            // Preserve that authored selection instead of reopening the initial
            // scenario while updating the voice-flow state.
            TransitionTo(FlowState.ModalDetail, showScenarioModal: false);
        }
    }

    public void NotifyTrainingSelected()
    {
        if (m_State == FlowState.ModalDetail)
            TransitionTo(FlowState.EducationSelected);
    }

    public void NotifyPpeEducationSelected()
    {
        // The authored modal now opens directly on the PPE/safety choices.
        // Keep EducationSelected accepted for older scene configurations, but
        // allow the active direct-card flow to bypass that removed UI step.
        if (m_State == FlowState.CardIntro
            || m_State == FlowState.ModalDetail
            || m_State == FlowState.EducationSelected)
            TransitionTo(FlowState.PpeEducationSelected);
    }

    private void NotifyPpeEducationModeChoiceStarted()
    {
        if (m_State != FlowState.PpeEducationSelected)
            return;

        StopRepeatPlayback();
        PlayPpeConditionalVoice(m_EducationModeSelectedVoice);
    }

    public void NotifyScenarioSelectionRestored()
    {
        if (m_State == FlowState.ModalDetail
            || m_State == FlowState.EducationSelected
            || m_State == FlowState.PpeEducationSelected)
            TransitionTo(FlowState.CardIntro);
    }

    private void NotifyPpeLearningModeSelected(ScenarioDetailModal.PpeLearningMode mode)
    {
        if (m_State != FlowState.PpeEducationSelected)
            return;

        ActiveWorkPlan = m_ScenarioDetailModal != null
            ? m_ScenarioDetailModal.SelectedWorkPlan
            : ScenarioDetailModal.PpeWorkPlan.None;
        ApplyActiveWorkPlan();

        if (mode == ScenarioDetailModal.PpeLearningMode.Education)
        {
            ActiveLearningMode = mode;
            TransitionTo(FlowState.EducationSelected);
            BeginModeSessionTracking();
            return;
        }

        if (!HasRequiredModeConfiguration(mode))
            return;

        StopModeSelectionRoutine();
        StopFlowPlayback();
        CancelPendingPpeWrongChoiceVoices();
        m_TransitionVersion++;
        ActiveLearningMode = mode;
        m_ModeSelectionFailed = false;
        m_State = mode == ScenarioDetailModal.PpeLearningMode.Training
            ? FlowState.TrainingSelected
            : FlowState.TestSelected;
        BeginModeSessionTracking();
        m_TeleportInputStarted = false;
        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);
        ApplyPresentation(m_State);
        LogTransition(m_State);
        m_ModeSelectionRoutine = StartCoroutine(PlayModeSelectionRoutine(mode, m_TransitionVersion));
    }

    private IEnumerator PlayModeSelectionRoutine(
        ScenarioDetailModal.PpeLearningMode mode,
        int version)
    {
        AudioClip selectionClip = mode == ScenarioDetailModal.PpeLearningMode.Training
            ? m_TrainingModeSelectedVoice
            : m_TestModeSelectedVoice;
        AudioClip movementClip = mode == ScenarioDetailModal.PpeLearningMode.Training
            ? m_TrainingMoveToPpeVoice
            : m_TestMoveToPpeVoice;

        yield return PlayRequiredModeVoice(selectionClip, mode, "selection", version);
        if (version != m_TransitionVersion || m_ModeSelectionFailed)
            yield break;

        m_ScenarioDetailModal.CompletePpeModeSelection();

        yield return PlayRequiredModeVoice(movementClip, mode, "move-to-PPE", version);
        if (version != m_TransitionVersion || m_ModeSelectionFailed)
            yield break;

        m_ModeSelectionRoutine = null;
        TransitionTo(FlowState.TeleportInstruction);
    }

    private IEnumerator PlayRequiredModeVoice(
        AudioClip clip,
        ScenarioDetailModal.PpeLearningMode mode,
        string purpose,
        int version)
    {
        if (clip == null)
        {
            m_ModeSelectionFailed = true;
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} on '{name}' is missing the {mode} {purpose} AudioClip in Assets/Scenes/3_PPE_Room_Train_Test.unity.",
                this);
            yield break;
        }

        if (AudioManager.Instance == null)
        {
            m_ModeSelectionFailed = true;
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} cannot play the {mode} {purpose} AudioClip because AudioManager is missing.",
                this);
            yield break;
        }

        if (!AudioManager.Instance.PlayVoice(clip))
        {
            m_ModeSelectionFailed = true;
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} could not start the {mode} {purpose} AudioClip '{clip.name}'.",
                this);
            yield break;
        }

        while (version == m_TransitionVersion && AudioManager.Instance != null &&
               (AudioManager.Instance.IsVoiceLoading || AudioManager.Instance.IsVoicePlaying))
        {
            yield return null;
        }
    }

    public void NotifyTeleportStarted()
    {
        if (m_State != FlowState.TeleportInstruction)
            return;

        m_TeleportInputStarted = true;
        CancelPendingPpeWrongChoiceVoices();
        // Cancelling only the AudioSource is not sufficient here: the original
        // step routine can observe the stopped source, finish, and schedule the
        // 002 repeat before the marker queues its teleport.  End both routines
        // as soon as this teleport attempt starts. The marker/lifecycle event
        // still owns the later state transition to PpeArea.
        StopFlowPlayback();
    }

    public void NotifyTeleportCanceled()
    {
        if (m_State != FlowState.TeleportInstruction || !m_TeleportInputStarted)
            return;

        m_TeleportInputStarted = false;
        // Teleport mode is released before a selected marker has necessarily
        // finished moving the XR Origin. Do not restart the instruction here;
        // the next explicit flow state owns any subsequent narration.
    }

    public void NotifyTeleportArrived()
    {
        // The locomotion trial owns both sides of its authored teleport:
        // AutoTeleportThenEnableLocomotion must enable Move before advancing
        // to PpeArea. The generic provider relay can report the queued request
        // one or more frames earlier, which would invalidate that coroutine
        // before it enables the move provider.
        if (m_State == FlowState.TeleportInstruction && UsesLocomotionTrial)
            return;

        // The EducationSelected narration opens the voice movement gate only
        // after Modal 004 and MoveToPPE finish. Arrival then starts PPE Start.
        if (m_State == FlowState.PpeEducationSelected ||
            m_State == FlowState.TeleportInstruction)
            TransitionTo(FlowState.PpeArea);
    }

    public void NotifyTabletReleased()
    {
        if (m_TabletReleasedVoicePlayed)
            return;

        m_TabletReleasedVoicePlayed = true;
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
            return;

        if (m_HazmatEquipController != null && m_HazmatEquipController.IsEquipped)
            return;

        PlayPpeConditionalVoice(m_TabletReleasedVoice);
    }

    /// <summary>
    /// Plays EDU 206 when the player arrives at the mid-exit point before the
    /// session returns to mode choice. Missing clip is an authored error.
    /// </summary>
    public void NotifyMidExitArrived()
    {
        if (m_MidExitStopVoice == null)
        {
            Debug.LogError(
                "PPEVoiceFlowDirector requires an authored mid-exit stop voice (EDU 206).",
                this);
            return;
        }

        // Mid-exit owns the Voice channel until the return completes. Cancel
        // every producer that could resume or replace it, but keep the separate
        // SFX channel available.
        m_MidExitVoiceExclusive = true;
        StopModeSelectionRoutine();
        m_TransitionVersion++;
        CancelPendingPpeWrongChoiceVoices();
        StopFlowPlayback(stopSfx: false);
        AudioManager.Instance?.PlayVoice(m_MidExitStopVoice);
    }

    public void NotifyCenterMarkerArrived()
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
            return;

        if (!m_CenterMarkerArrivedVoicePlayed &&
            m_HazmatEquipController != null && m_HazmatEquipController.IsEquipped)
        {
            m_CenterMarkerArrivedVoicePlayed = true;
            PlayPpeConditionalVoiceSequence(
                m_CenterMarkerArrivedVoice,
                m_CenterMarkerCheckVoice);
        }
    }

    public void NotifyMirrorMarkerArrived()
    {
        AudioManager.Instance?.StopSfx();

        if (!m_MirrorMarkerArrivedVoicePlayed &&
            ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
        {
            m_MirrorMarkerArrivedVoicePlayed = true;
            PlayPpeConditionalVoice(m_MirrorCheckVoice);
        }

        m_FinaleController?.NotifyMirrorMarkerArrived();
    }

    public void NotifyFinaleResult(bool ppeComplete, bool tabletComplete)
    {
        if (ppeComplete && tabletComplete)
            return;

        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
        {
            if (!m_UseSeparateEducationIncompleteVoices)
            {
                PlayPpeConditionalVoice(m_IncompletePpeVoice);
            }
            else if (!ppeComplete && !tabletComplete)
            {
                PlayPpeConditionalVoiceSequence(
                    m_IncompletePpeVoice,
                    m_IncompleteTabletVoice);
            }
            else
            {
                PlayPpeConditionalVoice(
                    ppeComplete ? m_IncompleteTabletVoice : m_IncompletePpeVoice);
            }
        }
        else if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training)
            PlayPpeConditionalVoice(m_TrainingIncompletePpeVoice);
    }

    public void NotifyQuizStart()
    {
        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
            PlayPpeConditionalVoice(m_QuizVoice);
        else if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training)
            PlayPpeConditionalVoice(m_TrainingQuizVoice);
        StartCoroutine(ShowQuizAfterVoice());
    }

    public void NotifyQuizCompleted(int correctAnswerCount, int questionCount)
    {
        m_TestQuizCorrectCount = correctAnswerCount;
        m_TestQuizQuestionCount = questionCount;
        m_ModeSessionElapsed = Mathf.Max(0f, Time.unscaledTime - m_ModeSessionStartedAt);

        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
            PlayPpeConditionalVoice(m_EduEndVoice);
        else if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training)
            PlayPpeConditionalVoice(m_TrainingEndVoice);
        else
            PlayPpeConditionalVoice(m_TestEndVoice);
        m_FinaleController?.NotifyQuizCompleted();
    }

    private IEnumerator ShowQuizAfterVoice()
    {
        while (IsVoicePlaying)
            yield return null;

        m_QuizController?.BeginQuiz(ActiveLearningMode, ActiveWorkPlan);
        m_FinaleController?.NotifyQuizPresented();
    }

    public bool IsVoicePlaying => AudioManager.Instance != null &&
        (AudioManager.Instance.IsVoiceLoading || AudioManager.Instance.IsVoicePlaying);

    /// <summary>
    /// Legacy completion entry point retained for existing serialized UnityEvents.
    /// Every completion path now returns to the PPE learning-mode choices rather
    /// than reopening the scenario-card list.
    /// </summary>
    public void ShowScenarioCardsAfterCompletionReturn()
    {
        ShowModeChoicesAfterCompletionReturn();
    }

    public PPEHazmatEquipController HazmatEquipController => m_HazmatEquipController;

    public bool RejectUseBeforeConditionCheck(PPEActionPanelController panel)
    {
        PPEItemType? itemType = panel?.InspectionState?.PresentationBinding?.ItemIdentity?.ItemType;
        if (itemType == PPEItemType.HazmatSuit &&
            m_HazmatEquipController != null &&
            m_HazmatEquipController.IsEquipped)
        {
            RejectUseWithWrongSfx(panel, m_HazmatAlreadyEquippedVoice);
            return true;
        }

        // Work-plan relevance owns the decision before the candidate's defect
        // condition. Otherwise an out-of-scenario contaminated PPE would play
        // EDU 202 (defect use) instead of EDU 205 (required PPE for scenario).
        if (itemType.HasValue && !IsPpeTypeAllowedForActiveWorkPlan(itemType.Value))
        {
            RejectUseWithWrongSfx(panel, m_WorkPlanMismatchVoice);
            return true;
        }

        return false;
    }

    public bool CanApprovePpeUse(PPEActionPanelController panel)
    {
        if (RejectUseBeforeConditionCheck(panel))
            return false;

        PPEItemType? itemType = panel?.InspectionState?.PresentationBinding?.ItemIdentity?.ItemType;

        if (itemType == PPEItemType.ConstructionHelmet &&
            (m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped))
        {
            return RejectUseWithWrongSfx(
                panel,
                m_HelmetUseWithoutHazmatVoice);
        }

        if (m_EnforceHazmatBeforeGlovesAndBoots &&
            RequiresHazmatBeforeUse(itemType) &&
            (m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped))
        {
            return RejectUseForMissingPrerequisite(
                panel,
                m_BootUseWithoutHazmatVoice);
        }

        if (IsOuterRubberGlove(itemType) &&
            (m_EquipmentVisualController == null ||
             !m_EquipmentVisualController.IsItemUsed(MatchingNitrileInnerGlove(itemType.Value))))
        {
            return RejectUseWithWrongSfx(
                panel,
                m_OuterGloveUseWithoutNitrileVoice);
        }

        if (panel == m_TapeActionPanel &&
            (!HasAnyTappableEquipment() ||
             m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped))
        {
            return RejectUseForMissingPrerequisite(
                panel,
                m_TapeUseBeforeGlovesAndBootsVoice);
        }

        return true;
    }

    private bool RejectUseWithWrongSfx(
        PPEActionPanelController panel,
        AudioClip educationVoice)
    {
        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
        {
            if (educationVoice == null)
            {
                Debug.LogError(
                    "PPEVoiceFlowDirector is missing the authored education reject voice for this PPE use.",
                    this);
                return false;
            }

            if (panel == null)
            {
                Debug.LogError(
                    "PPEVoiceFlowDirector cannot play authored wrong-choice SFX because the source PPE panel is missing.",
                    this);
            }
            else
            {
                panel.PlayWrongChoiceFeedbackSfx();
            }

            PlayPpeConditionalVoice(educationVoice);
            return false;
        }

        return RejectUseForMissingPrerequisite(panel, educationVoice);
    }

    private bool RejectUseForMissingPrerequisite(
        PPEActionPanelController panel,
        AudioClip educationVoice)
    {
        if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education)
        {
            PlayPpeConditionalVoice(educationVoice);
        }
        else
        {
            panel?.ShowModeRejectedUseFeedback(
                ActiveLearningMode,
                m_TrainingWrongFeedbackMessage);
            if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Test)
                RecordTestPpeChoice(panel, true);
        }

        return false;
    }

    public void ShowTestResultAfterVoice()
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Test)
            return;

        m_QuizController?.ShowTestResult(
            m_ModeSessionElapsed,
            m_TestQuizCorrectCount,
            m_TestPpeWrongChoiceCount);
    }

    public void HideQuizUiBeforeReturn()
    {
        m_QuizController?.HideForCompletionReturn();
    }

    public void NotifyCompletionBackRequested()
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Test)
            return;

        m_FinaleController?.NotifyCompletionBackRequested();
    }

    public void ShowModeChoicesAfterCompletionReturn()
    {
        ShowModeChoicesAfterCompletionReturn(true);
    }

    public void ShowModeChoicesAfterCompletionReturn(bool completedModeSession)
    {
        string completedModeSessionId = m_ModeSessionId;
        ScenarioDetailModal.PpeLearningMode completedMode = ActiveLearningMode;
        ScenarioDetailModal.PpeWorkPlan completedWorkPlan = ActiveWorkPlan;

        StopModeSelectionRoutine();
        StopFlowPlayback();
        m_TransitionVersion++;
        m_State = FlowState.PpeEducationSelected;
        ActiveWorkPlan = ScenarioDetailModal.PpeWorkPlan.None;
        ApplyActiveWorkPlan();
        m_TeleportInputStarted = false;
        SetLocomotionMoveEnabled(false);
        SetActive(m_ScenarioCardCanvas, true);
        SetActive(m_ScenarioSelectionRoot, false);
        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);
        ApplyPresentation(m_State);
        if (m_ScenarioDetailModal == null)
        {
            Debug.LogError(
                "PPEVoiceFlowDirector cannot present the authored mode choices because the scenario detail modal reference is missing.",
                this);
        }
        else
        {
            m_ScenarioDetailModal.ShowPpeModeChoicesAfterCompletion();
            if (completedModeSession)
            {
                RecordCompletedModeSessionAtReturn(
                    completedModeSessionId,
                    completedMode,
                    completedWorkPlan);
            }
        }
        LogTransition(m_State);
    }

    private void RecordCompletedModeSessionAtReturn(
        string completedModeSessionId,
        ScenarioDetailModal.PpeLearningMode completedMode,
        ScenarioDetailModal.PpeWorkPlan completedWorkPlan)
    {
        if (m_ModeSessionCompletionRecorded)
            return;

        if (string.IsNullOrEmpty(completedModeSessionId))
        {
            Debug.LogError(
                "[PPE Telemetry] Cannot record normal mode completion because the active mode session ID is missing.",
                this);
            return;
        }

        m_ModeSessionElapsed = Mathf.Max(0f, Time.unscaledTime - m_ModeSessionStartedAt);
        PPETrainingTelemetryCapture.RecordModeSessionCompleted(
            completedModeSessionId,
            completedMode,
            completedWorkPlan,
            m_TestQuizCorrectCount,
            m_TestQuizQuestionCount,
            m_TestPpeWrongChoiceCount,
            m_ModeSessionElapsed);
        MetaPlatformIdentityProbe.MarkScenarioCompletedForCurrentUser();
        m_ModeSessionCompletionRecorded = true;
    }

    public void ResetModeSessionForNextSelection()
    {
        ResetModeSessionTracking();
        m_BootGrabVoicePlayed = false;
        m_HazmatGrabVoicePlayed = false;
        m_AllPpeMoveMirrorVoicePlayed = false;
        m_TabletReleasedVoicePlayed = false;
        m_HazmatEquippedVoicePlayed = false;
        m_CenterMarkerArrivedVoicePlayed = false;
        m_MirrorMarkerArrivedVoicePlayed = false;

        foreach (PPEActionPanelController panel in CollectLivePpeActionPanels())
        {
            panel?.ResetForNewSession();
            panel?.InspectionState?.ResetToInitialCondition();
        }

        m_QuizController?.ResetForNewSession();
    }

    private void RecordTestPpeChoice(PPEActionPanelController panel, bool wrong)
    {
        if (panel == null)
            return;

        if (!m_TestFirstPpeSelections.ContainsKey(panel))
            m_TestFirstPpeSelections.Add(panel, !wrong);
        if (wrong)
            m_TestPpeWrongChoiceCount++;
    }

    private void ResetModeSessionTracking()
    {
        m_MidExitVoiceExclusive = false;
        m_ModeSessionId = null;
        m_ModeSessionStartedAt = Time.unscaledTime;
        m_ModeSessionElapsed = 0f;
        m_HazmatGrabVoicePlayed = false;
        m_GloveGrabVoicePlayed = false;
        m_NitrileInnerGloveGrabVoicePlayed = false;
        m_TapeGrabVoicePlayed = false;
        m_TestPpeWrongChoiceCount = 0;
        m_TestQuizCorrectCount = 0;
        m_TestQuizQuestionCount = 0;
        m_ModeSessionCompletionRecorded = false;
        m_TestFirstPpeSelections.Clear();
    }

    private void BeginModeSessionTracking()
    {
        ResetModeSessionTracking();
        m_ModeSessionId = PPETrainingTelemetryCapture.RecordModeSessionStarted(
            ActiveLearningMode,
            ActiveWorkPlan);
        if (string.IsNullOrEmpty(m_ModeSessionId))
        {
            Debug.LogError(
                $"[PPE Telemetry] {ActiveLearningMode}/{ActiveWorkPlan} 모드 실행 시작을 기록하지 못했습니다.",
                this);
        }
    }

    private bool HasRequiredModeConfiguration(ScenarioDetailModal.PpeLearningMode mode)
    {
        if (m_ScenarioDetailModal == null || m_QuizController == null || m_FinaleController == null)
        {
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} on '{name}' requires authored modal, quiz, and finale references before enabling {mode} mode.",
                this);
            return false;
        }

        AudioClip[] required = mode == ScenarioDetailModal.PpeLearningMode.Training
            ? new[]
            {
                m_TrainingModeSelectedVoice,
                m_TrainingMoveToPpeVoice,
                m_TrainingCheckPpeTabletVoice,
                m_TrainingWrongButtonVoice,
                m_TrainingMirrorCheckVoice,
                m_TrainingIncompletePpeVoice,
                m_TrainingQuizVoice,
                m_TrainingEndVoice,
            }
            : new[] { m_TestModeSelectedVoice, m_TestMoveToPpeVoice, m_TestEndVoice };

        foreach (AudioClip clip in required)
        {
            if (clip != null)
                continue;

            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} on '{name}' has an unassigned {mode} AudioClip in Assets/Scenes/3_PPE_Room_Train_Test.unity.",
                this);
            return false;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} cannot start {mode} mode because AudioManager is missing.",
                this);
            return false;
        }

        return true;
    }

    private void StopModeSelectionRoutine()
    {
        if (m_ModeSelectionRoutine == null)
            return;

        StopCoroutine(m_ModeSelectionRoutine);
        m_ModeSelectionRoutine = null;
    }

    private void SubscribeToSignals(bool subscribe)
    {
        if (m_ScenarioDetailModal != null)
        {
            if (subscribe)
            {
                m_ScenarioDetailModal.ModalShown += NotifyScenarioModalShown;
                m_ScenarioDetailModal.TrainingSelected += NotifyTrainingSelected;
                m_ScenarioDetailModal.IncompletePpeScenarioSelected += NotifyPpeEducationSelected;
                m_ScenarioDetailModal.PpeLearningModeSelected += NotifyPpeLearningModeSelected;
                m_ScenarioDetailModal.PpeEducationModeChoiceStarted += NotifyPpeEducationModeChoiceStarted;
                m_ScenarioDetailModal.ScenarioSelectionRestored += NotifyScenarioSelectionRestored;
            }
            else
            {
                m_ScenarioDetailModal.ModalShown -= NotifyScenarioModalShown;
                m_ScenarioDetailModal.TrainingSelected -= NotifyTrainingSelected;
                m_ScenarioDetailModal.IncompletePpeScenarioSelected -= NotifyPpeEducationSelected;
                m_ScenarioDetailModal.PpeLearningModeSelected -= NotifyPpeLearningModeSelected;
                m_ScenarioDetailModal.PpeEducationModeChoiceStarted -= NotifyPpeEducationModeChoiceStarted;
                m_ScenarioDetailModal.ScenarioSelectionRestored -= NotifyScenarioSelectionRestored;
            }
        }

        if (subscribe)
        {
            PPEControllerTeleportModeManager.TeleportModeStartedGlobal += NotifyTeleportStarted;
            PPEControllerTeleportModeManager.TeleportModeCanceledGlobal += NotifyTeleportCanceled;
        }
        else
        {
            PPEControllerTeleportModeManager.TeleportModeStartedGlobal -= NotifyTeleportStarted;
            PPEControllerTeleportModeManager.TeleportModeCanceledGlobal -= NotifyTeleportCanceled;
        }

        SubscribePpeConditionalNarration(subscribe);

    }

    private void SubscribePpeConditionalNarration(bool subscribe)
    {
        if (m_MaskActionPanel?.InspectionState?.GrabInteractable != null)
        {
            if (subscribe)
                m_MaskActionPanel.InspectionState.GrabInteractable.selectEntered.AddListener(OnMaskGrabbed);
            else
                m_MaskActionPanel.InspectionState.GrabInteractable.selectEntered.RemoveListener(OnMaskGrabbed);
        }

        if (m_BootActionPanels != null)
        {
            foreach (PPEActionPanelController panel in m_BootActionPanels)
            {
                if (panel == null)
                    continue;

                if (subscribe)
                    panel.ChoiceResolved += OnBootChoiceResolved;
                else
                    panel.ChoiceResolved -= OnBootChoiceResolved;
            }
        }

        if (m_TapeActionPanel != null)
        {
            if (subscribe)
                m_TapeActionPanel.ChoiceResolved += OnTapeChoiceResolved;
            else
                m_TapeActionPanel.ChoiceResolved -= OnTapeChoiceResolved;
        }

        foreach (PPEActionPanelController panel in CollectLivePpeActionPanels())
        {
            if (panel == null)
                continue;
            if (subscribe)
                panel.ChoiceResolvedWithSource += OnAnyPpeChoiceResolved;
            else
                panel.ChoiceResolvedWithSource -= OnAnyPpeChoiceResolved;
        }

        if (m_HazmatActionPanels != null)
        {
            foreach (PPEActionPanelController panel in m_HazmatActionPanels)
                SubscribeGrabNarration(panel, OnHazmatGrabbed, subscribe);
        }
        SubscribeGrabNarration(m_HarnessActionPanel, OnHarnessGrabbed, subscribe);
        SubscribeGrabNarration(m_SuppliedAirMaskActionPanel, OnSuppliedAirMaskGrabbed, subscribe);
        SubscribeGrabNarration(m_HelmetActionPanel, OnHelmetGrabbed, subscribe);
        SubscribeGrabNarration(m_TapeActionPanel, OnTapeGrabbed, subscribe);
        SubscribeGrabNarration(m_GoggleActionPanel, OnGoggleGrabbed, subscribe);
        SubscribeGrabNarration(m_FaceShieldActionPanel, OnFaceShieldGrabbed, subscribe);

        if (m_BootActionPanels != null)
            foreach (PPEActionPanelController panel in m_BootActionPanels)
                SubscribeGrabNarration(panel, OnBootGrabbed, subscribe);

        if (m_GloveActionPanels != null)
            foreach (PPEActionPanelController panel in m_GloveActionPanels)
                SubscribeGrabNarration(panel, OnGloveGrabbed, subscribe);

        if (m_NitrileInnerGloveActionPanels != null)
            foreach (PPEActionPanelController panel in m_NitrileInnerGloveActionPanels)
                SubscribeGrabNarration(panel, OnNitrileInnerGloveGrabbed, subscribe);
    }

    private static void SubscribeGrabNarration(
        PPEActionPanelController panel,
        UnityEngine.Events.UnityAction<SelectEnterEventArgs> listener,
        bool subscribe)
    {
        if (panel?.InspectionState?.GrabInteractable == null)
            return;

        if (subscribe)
            panel.InspectionState.GrabInteractable.selectEntered.AddListener(listener);
        else
            panel.InspectionState.GrabInteractable.selectEntered.RemoveListener(listener);
    }

    private void OnMaskGrabbed(SelectEnterEventArgs _)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
            return;

        // The first exposed mask is contaminated and instructs a breathing
        // check. Once its existing discard path has changed it to Clean, the
        // same normal PPE uses the regular mask guide instead.
        if (IsCleanPanel(m_MaskActionPanel))
            PlayNormalPpeGrabVoice(m_MaskActionPanel, m_SuppliedAirMaskGrabVoice);
        else
            PlayPpeConditionalVoice(m_MaskGrabVoice);
    }

    private void OnHazmatGrabbed(SelectEnterEventArgs _)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
            return;

        // The hazmat PPE panel uses the current 005 panel narration for its
        // grab interaction. Do not switch back to the legacy normal-PPE clip
        // based on the panel condition; that caused the removed A-button-era
        // narration to play again after the panel had been marked clean.
        if (m_HazmatPanelVoice == null)
        {
            // Do not leave a previously started narration playing when the
            // current panel has no assigned clip.
            AudioManager.Instance?.StopVoice();
            return;
        }

        if (!TryConsumeHazmatGrabVoice())
            return;

        PlayPpeConditionalVoice(m_HazmatPanelVoice);
    }

    private bool TryConsumeHazmatGrabVoice()
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education ||
            m_HazmatGrabVoicePlayed)
        {
            return false;
        }

        m_HazmatGrabVoicePlayed = true;
        return true;
    }

    private void OnHarnessGrabbed(SelectEnterEventArgs _) => PlayNormalPpeGrabVoice(m_HarnessActionPanel, m_HarnessGrabVoice);

    private void OnSuppliedAirMaskGrabbed(SelectEnterEventArgs _) => PlayNormalPpeGrabVoice(m_SuppliedAirMaskActionPanel, m_SuppliedAirMaskGrabVoice);

    private void OnHelmetGrabbed(SelectEnterEventArgs _) => PlayNormalPpeGrabVoice(m_HelmetActionPanel, m_HelmetGrabVoice);

    private void OnGoggleGrabbed(SelectEnterEventArgs _) => PlayNormalPpeGrabVoice(m_GoggleActionPanel, m_GoggleGrabVoice);

    private void OnFaceShieldGrabbed(SelectEnterEventArgs _) => PlayNormalPpeGrabVoice(m_FaceShieldActionPanel, m_FaceShieldGrabVoice);

    private void OnGloveGrabbed(SelectEnterEventArgs args)
    {
        if (m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped)
            return;

        PPEActionPanelController panel = FindPanelForGrab(args, m_GloveActionPanels);
        if (!CanPlayRequiredPpeHowTo(panel))
            return;

        if (!TryConsumeGloveGrabVoice(IsCleanPanel(panel)))
            return;

        PlayPpeConditionalVoice(m_GloveGrabVoice);
    }

    private void OnNitrileInnerGloveGrabbed(SelectEnterEventArgs args)
    {
        PPEActionPanelController panel = FindPanelForGrab(args, m_NitrileInnerGloveActionPanels);
        if (!CanPlayRequiredPpeHowTo(panel) || m_NitrileInnerGloveGrabVoicePlayed)
            return;

        m_NitrileInnerGloveGrabVoicePlayed = true;
        PlayPpeConditionalVoice(m_NitrileInnerGloveGrabVoice);
    }

    private bool TryConsumeGloveGrabVoice(bool isClean)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education || !isClean)
            return false;

        if (!m_PlayGloveGrabVoiceOncePerSession)
            return true;

        if (m_GloveGrabVoicePlayed)
            return false;

        m_GloveGrabVoicePlayed = true;
        return true;
    }

    private void OnTapeGrabbed(SelectEnterEventArgs _)
    {
        if (!CanPlayRequiredPpeHowTo(m_TapeActionPanel))
            return;

        if (!TryConsumeTapeGrabVoice(IsCleanPanel(m_TapeActionPanel)))
            return;

        PlayPpeConditionalVoice(m_TapeGrabVoice);
    }

    private bool TryConsumeTapeGrabVoice(bool isClean)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education || !isClean)
            return false;

        if (m_TapeGrabVoicePlayed)
            return false;

        m_TapeGrabVoicePlayed = true;
        return true;
    }

    private void OnBootGrabbed(SelectEnterEventArgs args)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education ||
            m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped)
            return;

        if (m_BootGrabVoicePlayed)
            return;

        PPEActionPanelController panel = FindPanelForGrab(args, m_BootActionPanels);
        if (!CanPlayRequiredPpeHowTo(panel))
            return;

        m_BootGrabVoicePlayed = true;
        PlayPpeConditionalVoice(m_BootGrabVoice);
    }

    private void OnBootChoiceResolved(PPEActionChoice choice, PPEActionResult result)
    {
        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
            return;

        if (choice == PPEActionChoice.Use && result == PPEActionResult.UseApproved &&
            (m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped))
        {
            PlayPpeConditionalVoice(m_BootUseWithoutHazmatVoice);
        }
    }

    private void OnTapeChoiceResolved(PPEActionChoice choice, PPEActionResult result)
    {
        // Tape prerequisites are checked before Use is resolved so a failed
        // attempt cannot alter the equipped hand or tape visuals.
    }

    private void OnAnyPpeChoiceResolved(
        PPEActionPanelController panel,
        PPEActionChoice choice,
        PPEActionResult result)
    {
        bool wrong = result == PPEActionResult.UseRejectedContaminated ||
            result == PPEActionResult.DiscardCleanPolicyPending;

        if (ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
        {
            bool proximityWear = panel != null && panel.ApproveUseByBodyProximity;
            if (!proximityWear)
            {
                panel?.ApplyLearningModeFeedback(
                    ActiveLearningMode,
                    choice,
                    result,
                    m_TrainingWrongFeedbackMessage);
            }

            if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Test &&
                (choice == PPEActionChoice.Use || choice == PPEActionChoice.Discard))
                RecordTestPpeChoice(panel, wrong);

            return;
        }

        if (choice != PPEActionChoice.Use || result != PPEActionResult.UseApproved)
            return;

        PPEItemType? itemType = panel?.InspectionState?.PresentationBinding?.ItemIdentity?.ItemType;
        if (!itemType.HasValue)
            return;

        if (itemType.Value == PPEItemType.HazmatSuit)
        {
            if (m_HazmatEquippedVoicePlayed)
                return;

            m_HazmatEquippedVoicePlayed = true;
            PlayPpeConditionalVoice(m_HazmatEquippedVoice);
            return;
        }

        // The final PPE must not briefly announce "wear the next PPE" before
        // its visual slot finishes the authored equip path and the move-to-
        // mirror narration takes over.
        if (m_EquipmentVisualController != null &&
            m_EquipmentVisualController.WillAllRequiredSlotsBeUsedAfter(panel))
            return;

        if ((itemType.Value == PPEItemType.RubberBootLeft || itemType.Value == PPEItemType.RubberBootRight) &&
            (m_HazmatEquipController == null || !m_HazmatEquipController.IsEquipped))
        {
            return;
        }

        PlayPpeConditionalVoice(m_NextPpeEquippedVoice);
    }

    private bool HasAnyTappableEquipment()
    {
        return m_EquipmentVisualController != null &&
            (m_EquipmentVisualController.IsItemUsed(PPEItemType.RubberGloveLeft) ||
             m_EquipmentVisualController.IsItemUsed(PPEItemType.RubberGloveRight) ||
             m_EquipmentVisualController.IsItemUsed(PPEItemType.RubberBootLeft) ||
             m_EquipmentVisualController.IsItemUsed(PPEItemType.RubberBootRight));
    }

    private void ApplyActiveWorkPlan()
    {
        if (m_EquipmentVisualController != null)
        {
            m_EquipmentVisualController.SetRequiredWearItemTypes(
                ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.None
                    ? null
                    : GetRequiredWearItemTypes(ActiveWorkPlan));
        }

        RestoreWorkPlanInspectionVisibility();
        m_FinaleController?.ApplyWorkPlanDocument(ActiveWorkPlan);
    }

    private void RestoreWorkPlanInspectionVisibility()
    {
        for (int index = 0; index < m_HiddenWorkPlanInspectionItems.Count; index++)
        {
            GameObject inspectionObject = m_HiddenWorkPlanInspectionItems[index];
            if (inspectionObject != null)
                inspectionObject.SetActive(true);
        }

        m_HiddenWorkPlanInspectionItems.Clear();
    }

    private PPEItemType[] GetRequiredWearItemTypes(ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        return workPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse
            ? m_LeakResponseRequiredItemTypes
            : m_ConfinedSpaceRequiredItemTypes;
    }

    private bool IsPpeTypeAllowedForActiveWorkPlan(PPEItemType itemType)
    {
        if (ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.None)
            return true;

        if (itemType == PPEItemType.PackingTape)
            return true;

        PPEItemType[] required = GetRequiredWearItemTypes(ActiveWorkPlan);
        return required != null && Array.IndexOf(required, itemType) >= 0;
    }

    private static bool RequiresHazmatBeforeUse(PPEItemType? itemType)
    {
        return itemType == PPEItemType.RubberGloveLeft ||
            itemType == PPEItemType.RubberGloveRight ||
            itemType == PPEItemType.RubberBootLeft ||
            itemType == PPEItemType.RubberBootRight;
    }

    private static bool IsOuterRubberGlove(PPEItemType? itemType)
    {
        return itemType == PPEItemType.RubberGloveLeft ||
            itemType == PPEItemType.RubberGloveRight;
    }

    private static PPEItemType MatchingNitrileInnerGlove(PPEItemType outerGlove)
    {
        return outerGlove == PPEItemType.RubberGloveLeft
            ? PPEItemType.NitrileInnerGloveLeft
            : PPEItemType.NitrileInnerGloveRight;
    }

    private static PPEActionPanelController FindPanelForGrab(
        SelectEnterEventArgs args,
        PPEActionPanelController[] panels)
    {
        if (args.interactableObject == null || panels == null)
            return null;

        foreach (PPEActionPanelController panel in panels)
        {
            if (panel?.InspectionState?.GrabInteractable != null &&
                ReferenceEquals(panel.InspectionState.GrabInteractable, args.interactableObject))
                return panel;
        }

        return null;
    }

    private static bool IsCleanPanel(PPEActionPanelController panel)
    {
        return panel?.InspectionState != null &&
            panel.InspectionState.CurrentCondition == PPEItemCondition.Clean;
    }

    private void PlayNormalPpeGrabVoice(PPEActionPanelController panel, AudioClip clip)
    {
        if (CanPlayRequiredPpeHowTo(panel))
            PlayPpeConditionalVoice(clip);
    }

    private bool CanPlayRequiredPpeHowTo(PPEActionPanelController panel)
    {
        PPEItemIdentity identity = panel?.InspectionState?.PresentationBinding?.ItemIdentity;
        return ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education &&
            ActiveWorkPlan != ScenarioDetailModal.PpeWorkPlan.None &&
            IsCleanPanel(panel) &&
            identity != null &&
            IsPpeTypeAllowedForActiveWorkPlan(identity.ItemType);
    }

    private void PlayPpeConditionalVoice(AudioClip clip)
    {
        if (clip == null || m_MidExitVoiceExclusive)
            return;

        StopPpeConditionalVoiceSequence();
        CancelPendingPpeWrongChoiceVoices();
        AudioManager.Instance?.StopVoice();
        AudioManager.Instance?.PlayVoice(clip);
    }

    private void PlayPpeConditionalVoiceSequence(params AudioClip[] clips)
    {
        if (m_MidExitVoiceExclusive)
            return;

        StopPpeConditionalVoiceSequence();
        CancelPendingPpeWrongChoiceVoices();
        AudioManager.Instance?.StopVoice();
        m_PpeConditionalVoiceRoutine = StartCoroutine(PlayPpeConditionalVoiceSequenceRoutine(clips));
    }

    private IEnumerator PlayPpeConditionalVoiceSequenceRoutine(AudioClip[] clips)
    {
        if (clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null || AudioManager.Instance == null)
                    continue;

                if (!AudioManager.Instance.PlayVoice(clip))
                    continue;

                while (AudioManager.Instance != null &&
                       (AudioManager.Instance.IsVoiceLoading || AudioManager.Instance.IsVoicePlaying))
                {
                    yield return null;
                }
            }
        }

        m_PpeConditionalVoiceRoutine = null;
    }

    private void StopPpeConditionalVoiceSequence()
    {
        if (m_PpeConditionalVoiceRoutine == null)
            return;

        StopCoroutine(m_PpeConditionalVoiceRoutine);
        m_PpeConditionalVoiceRoutine = null;
    }

    private void CancelPendingPpeWrongChoiceVoices()
    {
        foreach (PPEActionPanelController panel in CollectLivePpeActionPanels())
            panel?.CancelPendingWrongChoiceVoiceForNewInteraction();
    }

    private PPEActionPanelController[] CollectLivePpeActionPanels()
    {
        HashSet<PPEActionPanelController> panels = new HashSet<PPEActionPanelController>();
        if (m_AllPpeActionPanels != null)
        {
            foreach (PPEActionPanelController panel in m_AllPpeActionPanels)
            {
                if (panel != null)
                    panels.Add(panel);
            }
        }

        foreach (PPEActionPanelController panel in FindObjectsByType<PPEActionPanelController>(
                     FindObjectsInactive.Include))
        {
            if (panel != null)
                panels.Add(panel);
        }

        m_LivePpeActionPanels = new PPEActionPanelController[panels.Count];
        panels.CopyTo(m_LivePpeActionPanels);
        return m_LivePpeActionPanels;
    }

    private void TransitionTo(FlowState nextState, bool showScenarioModal = true)
    {
        if (m_State == nextState)
            return;

        bool resumeWithoutNarration = m_ControllerEducationSilentResumePending
            && nextState == m_ControllerEducationResumeState;
        m_ControllerEducationSilentResumePending = false;
        StopFlowPlayback();
        m_TransitionVersion++;
        m_State = nextState;
        m_TeleportInputStarted = false;
        ApplyPresentation(m_State);
        LogTransition(m_State);
        if (!resumeWithoutNarration)
            EnterState(m_State, showScenarioModal: showScenarioModal);
    }

    private IEnumerator AutoTeleportThenEnableLocomotion(int version)
    {
        PPEControllerTeleportModeManager.SetVoiceMovementGate(false);
        SetLocomotionMoveEnabled(false);

        if (m_LocomotionTeleportProvider == null || m_LocomotionStartDestination == null)
        {
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} locomotion trial requires authored teleport provider and Teleport_0 destination references.",
                this);
            yield break;
        }

        m_LocomotionTeleportProvider.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = m_LocomotionStartDestination.position,
            destinationRotation = m_LocomotionStartDestination.rotation,
            matchOrientation = MatchOrientation.TargetUpAndForward,
            requestTime = Time.time,
        });

        yield return null;
        yield return null;

        if (version != m_TransitionVersion)
            yield break;

        SetLocomotionMoveEnabled(true);
        m_LocomotionTrialRoutine = null;
        TransitionTo(FlowState.PpeArea);
    }

    private void SetLocomotionMoveEnabled(bool enabled)
    {
        if (m_LocomotionMoveProvider != null)
            m_LocomotionMoveProvider.enabled = enabled;
    }

    private bool UsesLocomotionTrial => m_AutoMoveToPpeThenLocomotion;


    private void EnterState(
        FlowState state,
        bool forceShowModal = false,
        bool showScenarioModal = true)
    {
        VoiceStep step = FindStep(state);

        if (state == FlowState.ModalDetail
            && showScenarioModal
            && (m_AutoShowFirstScenarioModal || forceShowModal))
        {
            if (m_ScenarioDetailModal != null)
                m_ScenarioDetailModal.Show(m_InitialScenarioIndex);
            else
                Debug.LogError("PPEVoiceFlowDirector requires ScenarioDetailModal for ModalDetail.", this);
        }

        if (state == FlowState.TeleportInstruction)
        {
            if (UsesLocomotionTrial)
            {
                PPEControllerTeleportModeManager.SetVoiceMovementGate(false);
                if (m_LocomotionTrialRoutine != null)
                    StopCoroutine(m_LocomotionTrialRoutine);
                m_LocomotionTrialRoutine = StartCoroutine(AutoTeleportThenEnableLocomotion(m_TransitionVersion));
                return;
            }

            PPEControllerTeleportModeManager.SetVoiceMovementGate(true);
            m_TeleportInputStarted = false;
        }

        if (state == FlowState.PpeArea &&
            ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Education)
        {
            if (ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Training)
                PlayPpeConditionalVoice(m_TrainingCheckPpeTabletVoice);
            return;
        }

        if (state == FlowState.Completed)
            return;

        // The card arrival narration is an application-start cue, not a mode
        // selection reminder. Returning from the modal or completing another
        // PPE mode keeps the authored card UI/state available without replaying
        // either the main clip or its repeat clip.
        if (state == FlowState.CardIntro)
        {
            if (m_CardIntroVoicePlayedThisRun)
                return;

            m_CardIntroVoicePlayedThisRun = true;
        }

        if (step == null)
        {
            HandleMissingStep(state);
            return;
        }

        m_StepRoutine = StartCoroutine(PlayStepRoutine(step, m_TransitionVersion));
    }

    private IEnumerator PlayStepRoutine(VoiceStep step, int version)
    {
        if (step.state == FlowState.EducationSelected)
        {
            AudioClip workPlanClip = GetEducationWorkPlanSelectedVoice();
            if (workPlanClip == null)
            {
                HandleMissingClip(step);
            }
            else
            {
                yield return PlayDirectedVoiceClip(workPlanClip, step.volume, version);
                if (version != m_TransitionVersion)
                    yield break;
            }
        }

        AudioClip[] clipsToPlay = step.clips;
        if (step.state == FlowState.Welcome)
        {
            AudioClip welcomeClip = ResolveMetaWelcomeClip();
            clipsToPlay = welcomeClip == null ? Array.Empty<AudioClip>() : new[] { welcomeClip };
        }

        if (clipsToPlay != null)
        {
            for (int clipIndex = 0; clipIndex < clipsToPlay.Length; clipIndex++)
            {
                AudioClip clip = clipsToPlay[clipIndex];
                if (version != m_TransitionVersion)
                    yield break;

                if (clip == null)
                {
                    HandleMissingClip(step);
                    continue;
                }

                if (AudioManager.Instance == null)
                {
                    HandleMissingVoicePlayer(step);
                    if (!m_SkipUnassignedClips)
                        yield break;
                    continue;
                }

                ApplyControllerGuideVisual(step, clipIndex);

                if (!AudioManager.Instance.PlayVoice(clip, step.volume))
                {
                    HandleMissingClip(step);
                    if (!m_SkipUnassignedClips)
                        yield break;
                    continue;
                }

                while (version == m_TransitionVersion
                    && AudioManager.Instance != null
                    && AudioManager.Instance.IsVoiceLoading)
                {
                    yield return null;
                }

                while (version == m_TransitionVersion
                    && AudioManager.Instance != null
                    && AudioManager.Instance.IsVoicePlaying)
                {
                    yield return null;
                }

            }
        }

        if (version != m_TransitionVersion)
            yield break;

        m_StepRoutine = null;

        if (step.controllerExpectedInput != ControllerGuideInput.None)
        {
            m_ControllerGuideInputStep = step;
            m_IsAwaitingControllerGuideInput = true;
            yield break;
        }

        // CardIntro is deliberately an interaction boundary. Its narration is a
        // one-shot startup cue, so finishing it must neither advance the state nor
        // start repeat playback; the detail modal still requires a real card pick.
        if (step.state == FlowState.CardIntro)
            yield break;

        if (step.waitForSignal || step.state == FlowState.PpeArea)
        {
            StartRepeatPlaybackForCurrentState();
            yield break;
        }

        TransitionTo(ResolveControllerEducationNextState(step.state, step.nextState));
    }

    private AudioClip ResolveMetaWelcomeClip()
    {
        MetaPlatformIdentityProbe.AccountWelcomeState welcomeState =
            MetaPlatformIdentityProbe.CurrentWelcomeState;

        AudioClip selected = welcomeState == MetaPlatformIdentityProbe.AccountWelcomeState.Returning
            ? m_ReturningMetaUserWelcomeClip
            : m_FirstMetaUserWelcomeClip;
        if (selected != null)
            return selected;

        if (!m_HasLoggedMissingMetaWelcomeClip)
        {
            string audience = welcomeState == MetaPlatformIdentityProbe.AccountWelcomeState.Returning
                ? "returning"
                : "first-user";
            Debug.LogError(
                $"{nameof(PPEVoiceFlowDirector)} on '{name}' is missing the authored {audience} Meta Welcome clip.",
                this);
            m_HasLoggedMissingMetaWelcomeClip = true;
        }

        return null;
    }

    private IEnumerator PlayControllerGuideInputFeedback(
        VoiceStep step,
        bool isCorrect,
        int version)
    {
        AudioClip feedbackClip = isCorrect
            ? step.controllerCorrectInputClip
            : step.controllerWrongInputClip;
        if (feedbackClip == null)
        {
            string missingRole = isCorrect ? "correct-input" : "wrong-input";
            Debug.LogError(
                $"PPEVoiceFlowDirector detailed controller step '{step.stepId}' requires its {missingRole} voice clip.",
                this);
            m_ControllerGuideInputFeedbackRoutine = null;
            yield break;
        }

        if (AudioManager.Instance == null)
        {
            HandleMissingVoicePlayer(step);
            m_ControllerGuideInputFeedbackRoutine = null;
            yield break;
        }

        yield return PlayDirectedVoiceClip(feedbackClip, step.volume, version);

        if (version != m_TransitionVersion || FindStep(m_State) != step)
        {
            m_ControllerGuideInputFeedbackRoutine = null;
            yield break;
        }

        if (!isCorrect)
        {
            m_ControllerGuideInputFeedbackRoutine = null;
            m_ControllerGuideInputStep = step;
            m_IsAwaitingControllerGuideInput = true;
            yield break;
        }

        if (step.controllerCompletionClip != null)
        {
            yield return PlayDirectedVoiceClip(
                step.controllerCompletionClip,
                step.volume,
                version);
        }

        if (version != m_TransitionVersion || FindStep(m_State) != step)
        {
            m_ControllerGuideInputFeedbackRoutine = null;
            yield break;
        }

        m_ControllerGuideInputFeedbackRoutine = null;
        m_ControllerGuideInputStep = null;
        TransitionTo(ResolveControllerEducationNextState(step.state, step.nextState));
    }

    private AudioClip GetEducationWorkPlanSelectedVoice()
    {
        if (ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse)
            return m_LeakResponseSelectedVoice;
        if (ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.ConfinedSpace)
            return m_ConfinedSpaceSelectedVoice;
        return null;
    }

    private IEnumerator PlayDirectedVoiceClip(AudioClip clip, float volume, int version)
    {
        if (clip == null || AudioManager.Instance == null)
            yield break;

        if (!AudioManager.Instance.PlayVoice(clip, volume))
            yield break;

        while (version == m_TransitionVersion
            && AudioManager.Instance != null
            && AudioManager.Instance.IsVoiceLoading)
        {
            yield return null;
        }

        while (version == m_TransitionVersion
            && AudioManager.Instance != null
            && AudioManager.Instance.IsVoicePlaying)
        {
            yield return null;
        }
    }

    private void ApplyControllerGuideVisual(VoiceStep step, int clipIndex)
    {
        if (step.controllerGuideVisuals == null
            || clipIndex < 0
            || clipIndex >= step.controllerGuideVisuals.Length)
        {
            return;
        }

        GameObject nextVisual = step.controllerGuideVisuals[clipIndex];
        if (nextVisual == null)
            return;

        GameObject companionVisual = step.controllerGuideCompanionVisual;
        SetControllerGuideVisualActive(m_ControllerEduVoiceSteps, nextVisual, companionVisual);
        SetControllerGuideVisualActive(m_ControllerSimpVoiceSteps, nextVisual, companionVisual);
    }

    private static void SetControllerGuideVisualActive(
        VoiceStep[] steps,
        GameObject nextVisual,
        GameObject companionVisual)
    {
        if (steps == null)
            return;

        foreach (VoiceStep configuredStep in steps)
        {
            if (configuredStep?.controllerGuideVisuals == null)
                continue;

            foreach (GameObject visual in configuredStep.controllerGuideVisuals)
            {
                if (visual != null)
                    SetActive(visual, visual == nextVisual || visual == companionVisual);
            }

            GameObject configuredCompanion = configuredStep.controllerGuideCompanionVisual;
            if (configuredCompanion != null)
                SetActive(
                    configuredCompanion,
                    configuredCompanion == nextVisual || configuredCompanion == companionVisual);
        }
    }

    private void StartRepeatPlaybackForCurrentState()
    {
        StopRepeatPlayback();

        VoiceStep step = FindStep(m_State);
        if (step == null || step.repeatClip == null || step.repeatDelay <= 0f)
            return;

        m_RepeatRoutine = StartCoroutine(RepeatPlaybackRoutine(step, m_TransitionVersion));
    }

    private IEnumerator RepeatPlaybackRoutine(VoiceStep step, int version)
    {
        while (version == m_TransitionVersion && m_State == FlowState.TeleportInstruction)
        {
            yield return new WaitForSecondsRealtime(step.repeatDelay);

            if (version != m_TransitionVersion || m_State != FlowState.TeleportInstruction)
                yield break;

            if (m_TeleportInputStarted || step.repeatClip == null || AudioManager.Instance == null)
                continue;

            AudioManager.Instance.PlayVoice(step.repeatClip, step.volume);
            while (version == m_TransitionVersion
                && !m_TeleportInputStarted
                && AudioManager.Instance != null
                && AudioManager.Instance.IsVoicePlaying)
            {
                yield return null;
            }
        }
    }

    private void ApplyPresentation(FlowState state)
    {
        bool isControllerStep = state == FlowState.ControllerRay
            || state == FlowState.ControllerMarker
            || state == FlowState.ControllerRayT
            || state == FlowState.ControllerPanel;
        bool isWindowStep = state == FlowState.Welcome
            || state == FlowState.NameInput
            || isControllerStep;
        bool showControllerModels = UsesControllerModels(state);

        ApplyControllerHeldVisuals(showControllerModels);

        bool showWindowPresentation = isWindowStep && !m_WindowPresentationAutoHidden;
        SetActive(m_WindowPresentationRoot, showWindowPresentation);
        if (showWindowPresentation && m_WindowPresentationAutoHideRoutine == null)
            SetWindowPresentationAlpha(1f);
        if (state == FlowState.Welcome && !m_WindowPresentationAutoHidden &&
            m_WindowPresentationAutoHideSeconds > 0f && m_WindowPresentationAutoHideRoutine == null)
        {
            m_WindowPresentationAutoHideRoutine = StartCoroutine(HideWindowPresentationAfterDelay());
        }
        // Keep the keyboard Canvas hierarchy alive, but show its presentation only
        // when the flow reaches name input after the welcome narration.
        SetActive(m_KeyboardPresentationRoot, state == FlowState.NameInput);
        SetActive(m_ControllerGuideRoot, isControllerStep);

        // Controller Simp and the keyboard-A detailed guide use the same
        // scene-authored visual groups assigned per clip. Do not overwrite
        // their child visibility with the legacy state mapping: 1_Ray
        // intentionally owns both its authored Card and Panel children.
        bool usesAuthoredGroupedGuide =
            m_ActiveControllerNarration == ControllerGuideNarration.Simple
            || m_ReturnToNameInputAfterControllerEducation;
        if (!usesAuthoredGroupedGuide)
        {
            SetActive(m_ControllerRayStep, state == FlowState.ControllerRay);
            SetActive(m_ControllerMarkerStep, state == FlowState.ControllerMarker);
            SetActive(m_ControllerRayTStep, state == FlowState.ControllerRayT);
            SetActive(m_ControllerPanelStep, state == FlowState.ControllerPanel);
        }

        bool isCardFlow = state == FlowState.CardIntro
            || state == FlowState.ModalDetail
            || state == FlowState.EducationSelected
            || state == FlowState.PpeEducationSelected
            || state == FlowState.TeleportInstruction
            || state == FlowState.PpeArea;

        if (isCardFlow)
            SetActive(m_ScenarioCardCanvas, true);
        else if (state == FlowState.Welcome || state == FlowState.NameInput || isControllerStep)
            SetActive(m_ScenarioCardCanvas, false);

        SetActive(m_ScenarioSelectionRoot, state == FlowState.CardIntro);

        // PpeArea is entered only when a route marker teleport actually lands,
        // so this is the exit marker's authored availability window.
        SetActive(
            m_ExitTeleportMarker,
            UsesLocomotionTrial || state == FlowState.PpeArea);
    }

    private static bool UsesControllerModels(FlowState state)
    {
        return state == FlowState.Welcome
            || state == FlowState.NameInput
            || state == FlowState.ControllerRay
            || state == FlowState.ControllerMarker
            || state == FlowState.ControllerRayT
            || state == FlowState.ControllerPanel;
    }

    private void ApplyControllerHeldVisuals(bool showControllerModels)
    {
        if (m_ControllerModelVisuals == null || m_ControllerModelVisuals.Length != 2 ||
            m_ControllerModelVisuals[0] == null || m_ControllerModelVisuals[1] == null)
        {
            if (!m_HasLoggedMissingControllerModelVisuals)
            {
                Debug.LogError(
                    $"{nameof(PPEVoiceFlowDirector)} on '{name}' requires the authored left/right Quest 2 controller model references.",
                    this);
                m_HasLoggedMissingControllerModelVisuals = true;
            }

            return;
        }

        foreach (Transform controllerModelVisual in m_ControllerModelVisuals)
            SetActive(controllerModelVisual.gameObject, showControllerModels);

        if (m_EquipmentVisualController == null)
        {
            if (showControllerModels && !m_HasLoggedMissingControllerModelVisuals)
            {
                Debug.LogError(
                    $"{nameof(PPEVoiceFlowDirector)} on '{name}' requires {nameof(PPEEquipmentVisualController)} " +
                    "to exchange the controller models with the authored PPE hand models.",
                    this);
                m_HasLoggedMissingControllerModelVisuals = true;
            }

            return;
        }

        m_EquipmentVisualController.SetHandModelsVisible(!showControllerModels);
    }

    // OnDisable is also invoked while a scene is unloading. At that point a serialized
    // model reference can already compare as null, so do not turn a normal teardown into
    // a missing-authoring error. The normal flow path above remains responsible for
    // reporting genuinely missing references while the scene is active.
    private void HideAvailableControllerModelsOnDisable()
    {
        if (m_ControllerModelVisuals == null)
            return;

        foreach (Transform controllerModelVisual in m_ControllerModelVisuals)
        {
            if (controllerModelVisual != null)
                SetActive(controllerModelVisual.gameObject, false);
        }

        if (m_EquipmentVisualController != null)
            m_EquipmentVisualController.SetHandModelsVisible(true);
    }

    private IEnumerator HideWindowPresentationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(m_WindowPresentationAutoHideSeconds);

        if (m_WindowPresentationFadeOutSeconds > 0f && m_WindowPresentationCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < m_WindowPresentationFadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetWindowPresentationAlpha(1f - Mathf.Clamp01(elapsed / m_WindowPresentationFadeOutSeconds));
                yield return null;
            }
        }

        SetWindowPresentationAlpha(0f);
        m_WindowPresentationAutoHidden = true;
        SetActive(m_WindowPresentationRoot, false);
        m_WindowPresentationAutoHideRoutine = null;
    }

    private void SetWindowPresentationAlpha(float alpha)
    {
        if (m_WindowPresentationCanvasGroup == null)
            return;

        m_WindowPresentationCanvasGroup.alpha = Mathf.Clamp01(alpha);
        m_WindowPresentationCanvasGroup.interactable = alpha > 0f;
        m_WindowPresentationCanvasGroup.blocksRaycasts = alpha > 0f;
    }

    private void StopFlowPlayback(bool stopSfx = true)
    {
        StopPpeConditionalVoiceSequence();

        m_IsAwaitingControllerGuideInput = false;
        m_ControllerGuideInputStep = null;
        if (m_ControllerGuideInputFeedbackRoutine != null)
        {
            StopCoroutine(m_ControllerGuideInputFeedbackRoutine);
            m_ControllerGuideInputFeedbackRoutine = null;
        }

        if (m_StepRoutine != null)
        {
            StopCoroutine(m_StepRoutine);
            m_StepRoutine = null;
        }

        StopRepeatPlayback();
        StopVoicePlayback(stopSfx);
    }

    private void StopRepeatPlayback()
    {
        if (m_RepeatRoutine == null)
            return;

        StopCoroutine(m_RepeatRoutine);
        m_RepeatRoutine = null;
    }

    private static void StopVoicePlayback(bool stopSfx)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.StopVoice();
        if (stopSfx)
            AudioManager.Instance.StopSfx();
    }

    private VoiceStep FindStep(FlowState state)
    {
        VoiceStep[] steps = IsControllerGuideState(state)
            ? GetActiveControllerVoiceSteps()
            : m_VoiceSteps;
        if (steps == null)
            return null;

        foreach (VoiceStep step in steps)
        {
            if (step != null && step.state == state)
                return step;
        }

        return null;
    }

    private VoiceStep[] GetActiveControllerVoiceSteps()
    {
        return m_ActiveControllerNarration == ControllerGuideNarration.Simple
            ? m_ControllerSimpVoiceSteps
            : m_ControllerEduVoiceSteps;
    }

    private static bool IsControllerGuideState(FlowState state)
    {
        return state == FlowState.ControllerRay
            || state == FlowState.ControllerMarker
            || state == FlowState.ControllerRayT
            || state == FlowState.ControllerPanel;
    }

    private FlowState GetNameSubmissionNextState()
    {
        if (MetaPlatformIdentityProbe.CurrentWelcomeState ==
            MetaPlatformIdentityProbe.AccountWelcomeState.Returning)
        {
            m_ControllerEducationCompleted = true;
            return FlowState.CardIntro;
        }

        return m_ControllerEducationCompleted
            ? FlowState.CardIntro
            : FlowState.ControllerRay;
    }

    private FlowState ResolveControllerEducationNextState(
        FlowState completedState,
        FlowState configuredNextState)
    {
        if (!m_ReturnToNameInputAfterControllerEducation
            || !IsControllerGuideState(completedState)
            || IsControllerGuideState(configuredNextState))
        {
            return ResolveKeyboardNameInputBypass(configuredNextState);
        }

        m_ReturnToNameInputAfterControllerEducation = false;
        m_ControllerEducationCompleted = true;
        m_ActiveControllerNarration = m_ControllerNarrationAfterName;
        if (m_HasControllerEducationResumeState)
        {
            m_HasControllerEducationResumeState = false;
            m_ControllerEducationSilentResumePending = true;
            return m_ControllerEducationResumeState;
        }

        return ResolveKeyboardNameInputBypass(FlowState.NameInput);
    }

    private FlowState ResolveKeyboardNameInputBypass(FlowState state)
    {
        if (!m_SkipKeyboardNameInput || state != FlowState.NameInput)
            return state;

        m_ActiveControllerNarration = m_ControllerNarrationAfterName;
        return GetNameSubmissionNextState();
    }

    private void HandleMissingStep(FlowState state)
    {
        if (!m_SkipUnassignedClips)
        {
            Debug.LogError($"PPEVoiceFlowDirector has no VoiceStep for state '{state}'.", this);
            return;
        }

        Debug.LogWarning($"PPEVoiceFlowDirector has no VoiceStep for state '{state}'. State was skipped.", this);
        FlowState next = GetDefaultNextState(state);
        if (next != state)
            TransitionTo(ResolveControllerEducationNextState(state, next));
    }

    private void HandleMissingClip(VoiceStep step)
    {
        if (m_SkipUnassignedClips)
        {
            string key = string.IsNullOrWhiteSpace(step.stepId)
                ? step.state.ToString()
                : step.stepId;
            if (m_WarnedMissingVoiceSteps.Add(key))
            {
                Debug.LogWarning(
                    $"PPEVoiceFlowDirector skipped unassigned voice clips in step '{key}'.", this);
            }
            return;
        }

        Debug.LogError(
            $"PPEVoiceFlowDirector cannot continue because a voice clip is missing in step '{step.stepId}'.", this);
    }

    private void HandleMissingVoicePlayer(VoiceStep step)
    {
        if (m_HasLoggedMissingVoicePlayer)
            return;

        m_HasLoggedMissingVoicePlayer = true;
        Debug.LogWarning(
            $"PPEVoiceFlowDirector could not find AudioManager while playing step '{step.stepId}'.", this);
    }

    private static FlowState GetDefaultNextState(FlowState state)
    {
        switch (state)
        {
            case FlowState.Welcome: return FlowState.NameInput;
            case FlowState.ControllerRay: return FlowState.ControllerMarker;
            case FlowState.ControllerMarker: return FlowState.ControllerRayT;
            case FlowState.ControllerRayT: return FlowState.CardIntro;
            case FlowState.ControllerPanel: return FlowState.CardIntro;
            case FlowState.CardIntro: return FlowState.ModalDetail;
            case FlowState.PpeEducationSelected: return FlowState.TeleportInstruction;
            case FlowState.PpeArea: return FlowState.Completed;
            default: return state;
        }
    }

    private void LogTransition(FlowState state)
    {
        if (m_LogTransitions)
            Debug.Log($"PPEVoiceFlowDirector state: {state}", this);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static VoiceStep[] CreateDefaultVoiceSteps()
    {
        return new[]
        {
            NewStep("welcome", FlowState.Welcome, 1, false, FlowState.NameInput),
            NewStep("name", FlowState.NameInput, 1, true, FlowState.ControllerRay),
            NewStep("card_intro", FlowState.CardIntro, 2, false, FlowState.ModalDetail),
            NewStep("card_detail", FlowState.ModalDetail, 1, true, FlowState.EducationSelected),
            NewStep("education_selected", FlowState.EducationSelected, 1, true, FlowState.PpeEducationSelected),
            NewStep("ppe_education_selected", FlowState.PpeEducationSelected, 1, false, FlowState.TeleportInstruction),
            NewStep("teleport_instruction", FlowState.TeleportInstruction, 1, true, FlowState.PpeArea),
            NewStep("ppe_area", FlowState.PpeArea, 1, true, FlowState.Completed),
        };
    }

    private static VoiceStep[] CreateDefaultControllerEduVoiceSteps()
    {
        VoiceStep[] steps =
        {
            NewStep("controller_edu_trigger", FlowState.ControllerRay, 2, false, FlowState.ControllerMarker),
            NewStep("controller_edu_grip", FlowState.ControllerMarker, 1, false, FlowState.ControllerRayT),
            NewStep("controller_edu_joystick", FlowState.ControllerRayT, 1, false, FlowState.CardIntro),
        };

        steps[0].controllerExpectedInput = ControllerGuideInput.Trigger;
        steps[1].controllerExpectedInput = ControllerGuideInput.Grip;
        steps[2].controllerExpectedInput = ControllerGuideInput.Joystick;
        return steps;
    }

    private static VoiceStep[] CreateDefaultControllerSimpVoiceSteps()
    {
        return new[]
        {
            NewStep("controller_simp_trigger", FlowState.ControllerRay, 2, false, FlowState.ControllerMarker),
            NewStep("controller_simp_grip", FlowState.ControllerMarker, 1, false, FlowState.ControllerRayT),
            NewStep("controller_simp_joystick", FlowState.ControllerRayT, 2, false, FlowState.CardIntro),
        };
    }

    private static VoiceStep NewStep(
        string stepId,
        FlowState state,
        int clipSlotCount,
        bool waitForSignal,
        FlowState nextState)
    {
        return new VoiceStep
        {
            stepId = stepId,
            state = state,
            clips = new AudioClip[clipSlotCount],
            controllerGuideVisuals = new GameObject[clipSlotCount],
            waitForSignal = waitForSignal,
            nextState = nextState,
        };
    }
}
