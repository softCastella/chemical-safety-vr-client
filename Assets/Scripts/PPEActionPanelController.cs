using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public enum PPEActionChoice
{
    None,
    Use,
    Discard,
    Inspect
}

public enum PPEActionResult
{
    None,
    UseApproved,
    UseRejectedContaminated,
    DiscardApprovedContaminated,
    DiscardCleanPolicyPending,
    InspectCompleted
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEInspectionState))]
public sealed class PPEActionPanelController : MonoBehaviour
{
    [Header("State")]
    [SerializeField]
    PPEInspectionState inspectionState;

    [Tooltip("Use 승인 전에 PPE 순서 조건을 확인하는 씬 작성 Voice Flow 참조입니다. 필요한 PPE에만 연결합니다.")]
    [SerializeField] PPEVoiceFlowDirector voiceFlowDirector;

    [Header("Body proximity use")]
    [Tooltip("켜면 Grab 후 사용/폐기 패널을 띄우지 않고, 몸에 갖다 대면 기존 Use 승인을 발행합니다.")]
    [SerializeField]
    bool approveUseByBodyProximity = true;

    [Tooltip("비우면 씬의 PPEHazmatEquipController Head/Body Anchor를 사용합니다.")]
    [SerializeField]
    Transform bodyAttachAnchor;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("잡은 손이 부착 지점에서 이 거리 안에 있을 때만 트리거로 입습니다. 손에 든 채 멀리서 누르면 입혀지지 않습니다.")]
    float bodyAttachDistance = 0.25f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("레거시 유지 시간입니다. 현재는 트리거 입력만 사용합니다.")]
    float bodyAttachHoldSeconds = 0.4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("헬멧·마스크가 아닌 PPE는 머리에서 이만큼 내린 가슴 높이를 부착 기준으로 씁니다.")]
    float bodyAttachChestDropMeters = 0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("장화 하반신 부착 높이입니다. 머리에서 이만큼 내린 지점과 가슴 지점을 잇는 구간에서 판정합니다. 방호복 착용 후에는 아래쪽이 Body Anchor입니다.")]
    float bodyAttachBootDropMeters = 1.2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("가슴·머리 부착 지점을 몸에서 이만큼 앞으로 둡니다. 몸 안에 넣지 않고 약간 앞에서 입히기 위함입니다.")]
    float bodyAttachForwardMeters = 0.18f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("왼/오른 장갑은 가슴에서 이만큼 해당 손 쪽으로 치우친 몸통 지점에 갖다 댑니다.")]
    float bodyAttachHandSideOffsetMeters = 0.22f;

    [Header("Scene-authored UI")]
    [SerializeField]
    GameObject panelRoot;

    [SerializeField]
    [Tooltip("이 PPE를 관찰할 때 공용 패널이 따라갈 씬 작성 Pose입니다.")]
    Transform panelPose;

    [SerializeField]
    [Tooltip("공용 패널의 2버튼/3버튼 레이아웃과 월드 배치 설정을 담은 씬 작성 컴포넌트입니다.")]
    PPEActionPanelSharedPresentation sharedPresentation;

    [SerializeField]
    [Tooltip("켜면 공용 2/3버튼 프리셋이 RectTransform과 상태 아이콘 위치를 적용합니다. 끄면 이 패널의 씬 작성 레이아웃을 그대로 유지합니다.")]
    bool applySharedPresentationLayout = true;

    [SerializeField]
    TMP_Text itemNameLabel;

    [SerializeField]
    string itemDisplayName;

    [SerializeField]
    Button useButton;

    [SerializeField]
    Button discardButton;

    [SerializeField]
    [Tooltip("마스크처럼 사용/폐기 외에 확인하기가 필요할 때만 연결합니다.")]
    Button inspectButton;

    [SerializeField]
    [Tooltip("켜면 확인하기 버튼을 표시합니다. 버튼 선택은 양손 컨트롤러 Trigger의 XRI UI Press로 처리합니다.")]
    bool enableInspectChoice;
    [SerializeField] PPEMaskInspectionAnimation inspectAnimation;

    [SerializeField]
    [Tooltip("켜면 현재 PPE 상태에서 확인하기를 완료하기 전까지 사용과 폐기를 승인하지 않습니다.")]
    bool requireInspectBeforeUseOrDiscard;

    [SerializeField]
    string inspectRequiredMessage;

    [SerializeField]
    GameObject feedbackRoot;

    [SerializeField]
    TMP_Text feedbackLabel;

    [SerializeField]
    GameObject usePassIconRoot;

    [SerializeField]
    GameObject useErrorIconRoot;

    [SerializeField]
    GameObject discardPassIconRoot;

    [SerializeField]
    GameObject discardErrorIconRoot;

    [SerializeField]
    GameObject inspectPassIconRoot;

    [Header("Presentation timing")]
    [SerializeField]
    [Min(0f)]
    float showDelaySeconds;

    [SerializeField]
    [Min(0f)]
    [Tooltip("몸에 대고 입히는 PPE를 잡았을 때 이름을 보여줄 시간입니다. 0이면 놓을 때까지 유지합니다.")]
    float grabNameVisibleSeconds = 2f;

    [SerializeField]
    bool useUnscaledTime;

    [SerializeField]
    bool hideWhenInspectionEnds;

    [SerializeField]
    [Min(0f)]
    float approvedRemovalDelaySeconds = 0.25f;

    [Tooltip("Use 승인 뒤 검사 대상 PPE를 씬에서 숨깁니다. 재사용해야 하는 내화학 테이프만 끕니다.")]
    [SerializeField]
    bool hideInspectionVisualAfterUse = true;

    [Header("Authored feedback")]
    [SerializeField]
    string useApprovedMessage;

    [SerializeField]
    string contaminatedUseMessage;

    [SerializeField]
    string contaminatedDiscardMessage;

    [SerializeField]
    string cleanDiscardPendingMessage;

    [SerializeField]
    string inspectCompletedMessage;

    [SerializeField]
    Color acceptedFeedbackColor;

    [SerializeField]
    Color rejectedFeedbackColor;

    [SerializeField]
    Color pendingFeedbackColor;

    [SerializeField]
    bool logChoices = true;

    [Header("Action SFX")]
    [SerializeField]
    string useSfxId;

    [SerializeField]
    string inspectCleanSfxId;

    [SerializeField]
    string inspectContaminatedSfxId;

    [SerializeField]
    string correctFeedbackSfxId = "Correct Answer";

    [SerializeField]
    string wrongFeedbackSfxId = "Wrong Answer";

    [Header("Wear haptics")]
    [SerializeField]
    [Tooltip("정상 PPE의 착용 승인이 확정된 순간, 현재 PPE를 잡고 있는 컨트롤러에 진동을 한 번 보냅니다.")]
    bool playWearHaptics = true;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("착용 성공 진동 강도입니다.")]
    float wearHapticAmplitude = 0.45f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("착용 성공 진동 지속시간(초)입니다.")]
    float wearHapticDuration = 0.08f;

    [Header("Wrong-choice voice")]
    [Tooltip("Defective PPE에서 사용을 선택해 X 피드백이 표시될 때 재생할 음성입니다.")]
    [SerializeField]
    AudioClip contaminatedUseRejectedVoice;

    [Tooltip("정상 PPE에서 폐기를 선택해 잘못된 피드백이 표시될 때 재생할 음성입니다.")]
    [SerializeField]
    AudioClip cleanDiscardRejectedVoice;

    [Tooltip("아이콘과 Wrong Answer SFX를 표시한 뒤 음성을 시작하기까지의 시간입니다.")]
    [SerializeField, Min(0f)]
    float wrongChoiceVoiceDelaySeconds = 0.46f;

    Coroutine pendingShow;
    Coroutine pendingGrabNameHide;
    Coroutine pendingApprovedRemoval;
    Coroutine pendingWrongChoiceVoice;
    Renderer[] inspectionRenderers;
    bool[] authoredRendererEnabled;
    Collider[] inspectionColliders;
    bool[] authoredColliderEnabled;
    bool authoredGrabEnabled;
    bool hasInspectionVisibilitySnapshot;
    bool inspectionVisualHidden;
    bool inspectCompletedForCurrentCondition;
    bool inspectAnimationPlaying;
    static PPEActionPanelController activePanelOwner;
    PPEHazmatEquipController cachedHazmatEquip;
    bool loggedMissingBodyAttach;

    public static PPEActionPanelController ActivePanelOwner => activePanelOwner;

    public PPEInspectionState InspectionState => inspectionState;
    public GameObject PanelRoot => panelRoot;
    public Transform PanelPose => panelPose;
    public PPEActionPanelSharedPresentation SharedPresentation => sharedPresentation;
    public bool ApplySharedPresentationLayout => applySharedPresentationLayout;
    public TMP_Text ItemNameLabel => itemNameLabel;
    public string ItemDisplayName => itemDisplayName;
    public Button UseButton => useButton;
    public Button DiscardButton => discardButton;
    public Button InspectButton => inspectButton;
    public bool EnableInspectChoice => enableInspectChoice;
    public PPEMaskInspectionAnimation InspectAnimation => inspectAnimation;
    public bool RequireInspectBeforeUseOrDiscard => requireInspectBeforeUseOrDiscard;
    public bool InspectCompletedForCurrentCondition => inspectCompletedForCurrentCondition;
    public GameObject FeedbackRoot => feedbackRoot;
    public TMP_Text FeedbackLabel => feedbackLabel;
    public GameObject UsePassIconRoot => usePassIconRoot;
    public GameObject UseErrorIconRoot => useErrorIconRoot;
    public GameObject DiscardPassIconRoot => discardPassIconRoot;
    public GameObject DiscardErrorIconRoot => discardErrorIconRoot;
    public GameObject InspectPassIconRoot => inspectPassIconRoot;
    public float ShowDelaySeconds => showDelaySeconds;
    public bool UseUnscaledTime => useUnscaledTime;
    public bool HideWhenInspectionEnds => hideWhenInspectionEnds;
    public float ApprovedRemovalDelaySeconds => approvedRemovalDelaySeconds;
    public bool HideInspectionVisualAfterUse => hideInspectionVisualAfterUse;
    public bool ApproveUseByBodyProximity => approveUseByBodyProximity;
    public Transform BodyAttachAnchor => bodyAttachAnchor;
    public float BodyAttachDistance => bodyAttachDistance;
    public float BodyAttachHoldSeconds => bodyAttachHoldSeconds;
    public float BodyAttachChestDropMeters => bodyAttachChestDropMeters;
    public float BodyAttachHandSideOffsetMeters => bodyAttachHandSideOffsetMeters;
    public bool PlayWearHaptics => playWearHaptics;
    public float WearHapticAmplitude => wearHapticAmplitude;
    public float WearHapticDuration => wearHapticDuration;
    public PPEActionChoice LastChoice { get; private set; }
    public PPEActionResult LastResult { get; private set; }

    public event Action<PPEActionChoice, PPEActionResult> ChoiceResolved;
    public event Action<PPEActionPanelController, PPEActionChoice, PPEActionResult> ChoiceResolvedWithSource;

    /// <summary>
    /// PPE Voice Flow calls this when a newer PPE narration supersedes a
    /// delayed wrong-choice narration. It intentionally does not alter the
    /// panel's selection, feedback, or item state.
    /// </summary>
    public void CancelPendingWrongChoiceVoiceForNewInteraction()
    {
        CancelPendingWrongChoiceVoice();
    }

    public void ApplyLearningModeFeedback(
        ScenarioDetailModal.PpeLearningMode mode,
        PPEActionChoice choice,
        PPEActionResult result,
        string trainingWrongMessage)
    {
        if (mode == ScenarioDetailModal.PpeLearningMode.Education)
            return;

        CancelPendingWrongChoiceVoice();
        ResetFeedback();
    }

    public void ShowModeRejectedUseFeedback(
        ScenarioDetailModal.PpeLearningMode mode,
        string trainingWrongMessage)
    {
        if (mode == ScenarioDetailModal.PpeLearningMode.Education)
            return;

        CancelPendingWrongChoiceVoice();
        ResetFeedback();
        PlayFeedbackSfx(PPEActionResult.UseRejectedContaminated);
    }

    public void PlayWrongChoiceFeedbackSfx()
    {
        if (!AllowsPpeActionSfx() || AudioManager.Instance == null ||
            string.IsNullOrWhiteSpace(wrongFeedbackSfxId))
            return;

        AudioManager.Instance.PlaySfx(wrongFeedbackSfxId);
    }

    public void ResetForNewSession()
    {
        CancelPendingShow();
        CancelPendingGrabNameHide();
        CancelPendingApprovedRemoval();
        CancelPendingWrongChoiceVoice();
        LastChoice = PPEActionChoice.None;
        LastResult = PPEActionResult.None;
        inspectCompletedForCurrentCondition = false;
        if (feedbackLabel != null)
            feedbackLabel.gameObject.SetActive(true);
        ResetFeedback();
        RestoreInspectionVisibility();
        if (panelRoot != null)
            panelRoot.SetActive(false);
        if (activePanelOwner == this)
            activePanelOwner = null;
    }

    void OnEnable()
    {
        if (!HasCompleteReferences())
        {
            Debug.LogError(
                "PPEActionPanelController requires serialized state, panel, button, and feedback references.",
                this);
            enabled = false;
            return;
        }

        inspectionState.InspectionChanged += OnInspectionChanged;
        inspectionState.ConditionChanged += OnConditionChanged;
        inspectCompletedForCurrentCondition = false;
        CaptureInspectionVisibility();
        WireButtonListeners(!approveUseByBodyProximity);

        panelRoot.SetActive(false);
        ResetFeedback();
        RefreshInspectButtonVisibility();

        if (inspectionState.IsBeingInspected)
            OnInspectionChanged(true);
    }

    void OnDisable()
    {
        if (activePanelOwner == this)
        {
            panelRoot.SetActive(false);
            activePanelOwner = null;
        }

        if (inspectionState != null)
        {
            inspectionState.InspectionChanged -= OnInspectionChanged;
            inspectionState.ConditionChanged -= OnConditionChanged;
        }

        CancelPendingShow();
        CancelPendingGrabNameHide();
        CancelPendingApprovedRemoval();
        CancelPendingWrongChoiceVoice();
        WireButtonListeners(false);
        RestoreInspectionVisibility();
    }

    void Update()
    {
        if (!approveUseByBodyProximity)
            return;

        if (!inspectionState.IsBeingInspected || pendingApprovedRemoval != null)
            return;

        if (!WasSelectingActivatePressedThisFrame())
            return;

        if (!IsBodyProximityActivateAttempt())
            return;

        TryApproveUseFromBodyProximity();
    }

    void OnInspectionChanged(bool isBeingInspected)
    {
        CancelPendingShow();
        CancelPendingGrabNameHide();

        if (isBeingInspected)
        {
            TakePanelOwnership();
            LastChoice = PPEActionChoice.None;
            LastResult = PPEActionResult.None;
            if (itemNameLabel != null)
                itemNameLabel.text = itemDisplayName;
            ResetFeedback();
            RefreshInspectButtonVisibility();

            if (approveUseByBodyProximity)
            {
                ShowGrabNameOnly();
                return;
            }

            ApplyAuthoredLayout();

            if (showDelaySeconds <= 0f)
            {
                panelRoot.SetActive(true);
                return;
            }

            pendingShow = StartCoroutine(ShowPanelAfterDelay());
            return;
        }

        if (activePanelOwner != this)
            return;

        if (hideWhenInspectionEnds)
            panelRoot.SetActive(false);

        activePanelOwner = null;
    }

    void ShowGrabNameOnly()
    {
        if (applySharedPresentationLayout &&
            sharedPresentation != null &&
            OwnsUi(sharedPresentation.gameObject))
            sharedPresentation.ApplyNameOnly();

        SetActionChoiceUiVisible(false);
        if (itemNameLabel != null)
            itemNameLabel.gameObject.SetActive(true);

        panelRoot.SetActive(true);

        if (grabNameVisibleSeconds <= 0f)
            return;

        pendingGrabNameHide = StartCoroutine(HideGrabNameAfterDelay());
    }

    IEnumerator HideGrabNameAfterDelay()
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(grabNameVisibleSeconds);
        else
            yield return new WaitForSeconds(grabNameVisibleSeconds);

        pendingGrabNameHide = null;
        if (activePanelOwner == this)
            panelRoot.SetActive(false);
    }

    void SetActionChoiceUiVisible(bool visible)
    {
        if (useButton != null)
            useButton.gameObject.SetActive(visible);
        if (discardButton != null)
            discardButton.gameObject.SetActive(visible);
        if (inspectButton != null)
            inspectButton.gameObject.SetActive(visible && enableInspectChoice);
        if (feedbackRoot != null)
            feedbackRoot.SetActive(visible);
        if (usePassIconRoot != null)
            usePassIconRoot.SetActive(visible);
        if (useErrorIconRoot != null)
            useErrorIconRoot.SetActive(visible);
        if (discardPassIconRoot != null)
            discardPassIconRoot.SetActive(visible);
        if (discardErrorIconRoot != null)
            discardErrorIconRoot.SetActive(visible);
        if (inspectPassIconRoot != null)
            inspectPassIconRoot.SetActive(visible);
    }

    IEnumerator ShowPanelAfterDelay()
    {
        if (showDelaySeconds > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(showDelaySeconds);
            else
                yield return new WaitForSeconds(showDelaySeconds);
        }

        pendingShow = null;
        if (approveUseByBodyProximity)
            yield break;

        if (activePanelOwner == this && inspectionState.IsBeingInspected)
        {
            ApplyAuthoredLayout();
            panelRoot.SetActive(true);
        }
    }

    void ChooseUse()
    {
        if (!CanResolveChoice() || RejectChoiceUntilInspect(PPEActionChoice.Use))
            return;

        if (approveUseByBodyProximity && !IsBodyProximityActivateAttempt())
            return;

        ResolveUseChoice();
    }

    void TryApproveUseFromBodyProximity()
    {
        if (!approveUseByBodyProximity || !CanResolveChoice())
            return;

        ResolveUseChoice();
    }

    void ResolveUseChoice()
    {
        if (voiceFlowDirector != null &&
            voiceFlowDirector.RejectUseBeforeConditionCheck(this))
        {
            return;
        }

        if (inspectionState.CurrentCondition != PPEItemCondition.Clean)
        {
            ResolveChoice(PPEActionChoice.Use, PPEActionResult.UseRejectedContaminated);
            return;
        }

        if (voiceFlowDirector != null && !voiceFlowDirector.CanApprovePpeUse(this))
            return;

        ResolveChoice(PPEActionChoice.Use, PPEActionResult.UseApproved);
    }

    void ChooseDiscard()
    {
        if (!CanResolveChoice() || RejectChoiceUntilInspect(PPEActionChoice.Discard))
            return;

        PPEActionResult result = inspectionState.CurrentCondition == PPEItemCondition.Contaminated
            ? PPEActionResult.DiscardApprovedContaminated
            : PPEActionResult.DiscardCleanPolicyPending;

        ResolveChoice(PPEActionChoice.Discard, result);
    }

    void ChooseInspect()
    {
        if (!CanResolveChoice() || !enableInspectChoice || inspectAnimationPlaying)
            return;

        if (inspectAnimation != null)
        {
            inspectAnimationPlaying = true;
            if (inspectAnimation.TryPlay(CompleteInspectAfterAnimation))
                return;
            inspectAnimationPlaying = false;
        }

        CompleteInspectAfterAnimation();
    }

    void CompleteInspectAfterAnimation()
    {
        inspectAnimationPlaying = false;
        if (!CanResolveChoice() || inspectCompletedForCurrentCondition)
            return;
        inspectCompletedForCurrentCondition = true;
        ResolveChoice(PPEActionChoice.Inspect, PPEActionResult.InspectCompleted);
    }

    void OnConditionChanged(PPEItemCondition _)
    {
        inspectCompletedForCurrentCondition = false;
    }

    bool RejectChoiceUntilInspect(PPEActionChoice choice)
    {
        if (!requireInspectBeforeUseOrDiscard || inspectCompletedForCurrentCondition)
            return false;

        CancelPendingWrongChoiceVoice();
        LastChoice = choice;
        LastResult = PPEActionResult.None;

        GameObject icon = choice == PPEActionChoice.Use
            ? useErrorIconRoot
            : discardErrorIconRoot;
        string message = string.IsNullOrWhiteSpace(inspectRequiredMessage)
            ? "호흡 상태 확인 필요"
            : inspectRequiredMessage;
        if (AllowsPpeFeedbackPresentation())
            ApplyFeedback(message, rejectedFeedbackColor, icon);
        else
            ResetFeedback();

        if (AllowsPpeActionSfx())
            AudioManager.Instance?.PlaySfx(wrongFeedbackSfxId);
        return true;
    }

    void ResolveChoice(PPEActionChoice choice, PPEActionResult result)
    {
        LastChoice = choice;
        LastResult = result;
        CancelPendingWrongChoiceVoice();

        if (!approveUseByBodyProximity && AllowsPpeFeedbackPresentation())
            ApplyChoicePresentation(result);

        PlayActionSfx(choice, result);
        PlayFeedbackSfx(result);
        PlayWearHaptic(choice, result);
        QueueWrongChoiceVoice(result);

        ChoiceResolved?.Invoke(choice, result);
        ChoiceResolvedWithSource?.Invoke(this, choice, result);

        if (logChoices)
        {
            Debug.Log(
                $"[PPE Action] item={name}, condition={inspectionState.CurrentCondition}, " +
                $"choice={choice}, result={result}",
                this);
        }

        if (result == PPEActionResult.UseApproved ||
            result == PPEActionResult.DiscardApprovedContaminated)
        {
            BeginApprovedResolution(result);
        }
    }

    void PlayWearHaptic(PPEActionChoice choice, PPEActionResult result)
    {
        if (!playWearHaptics ||
            choice != PPEActionChoice.Use ||
            result != PPEActionResult.UseApproved ||
            wearHapticAmplitude <= 0f ||
            wearHapticDuration <= 0f ||
            inspectionState == null ||
            !inspectionState.TryGetSelectingHandedness(out InteractorHandedness handedness))
        {
            return;
        }

        HapticsUtility.Controller controller;
        switch (handedness)
        {
            case InteractorHandedness.Left:
                controller = HapticsUtility.Controller.Left;
                break;
            case InteractorHandedness.Right:
                controller = HapticsUtility.Controller.Right;
                break;
            default:
                return;
        }

        HapticsUtility.SendHapticImpulse(
            Mathf.Clamp01(wearHapticAmplitude),
            wearHapticDuration,
            controller);
    }

    void PlayActionSfx(PPEActionChoice choice, PPEActionResult result)
    {
        if (!AllowsPpeActionSfx() || AudioManager.Instance == null)
            return;

        if (choice == PPEActionChoice.Use && result == PPEActionResult.UseApproved)
        {
            AudioManager.Instance.PlaySfx(useSfxId);
            return;
        }

        if (choice != PPEActionChoice.Inspect ||
            result != PPEActionResult.InspectCompleted)
            return;

        string sfxId = inspectionState.CurrentCondition == PPEItemCondition.Contaminated
            ? inspectContaminatedSfxId
            : inspectCleanSfxId;
        AudioManager.Instance.PlaySfx(sfxId);
    }

    void PlayFeedbackSfx(PPEActionResult result)
    {
        if (!AllowsPpeActionSfx() || AudioManager.Instance == null)
            return;

        bool isCorrect = result == PPEActionResult.UseApproved ||
            result == PPEActionResult.DiscardApprovedContaminated ||
            result == PPEActionResult.InspectCompleted;
        bool isWrong = result == PPEActionResult.UseRejectedContaminated ||
            result == PPEActionResult.DiscardCleanPolicyPending;

        if (isCorrect)
            AudioManager.Instance.PlaySfx(correctFeedbackSfxId);
        else if (isWrong)
            AudioManager.Instance.PlaySfx(wrongFeedbackSfxId);
    }

    void QueueWrongChoiceVoice(PPEActionResult result)
    {
        AudioClip clip = result switch
        {
            PPEActionResult.UseRejectedContaminated => contaminatedUseRejectedVoice,
            PPEActionResult.DiscardCleanPolicyPending => cleanDiscardRejectedVoice,
            _ => null,
        };

        if (clip == null || AudioManager.Instance == null || !AllowsPpeChoiceVoice())
            return;

        AudioManager.Instance.StopVoice();
        pendingWrongChoiceVoice = StartCoroutine(PlayWrongChoiceVoiceAfterFeedbackSfx(clip));
    }

    IEnumerator PlayWrongChoiceVoiceAfterFeedbackSfx(AudioClip clip)
    {
        if (wrongChoiceVoiceDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(wrongChoiceVoiceDelaySeconds);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVoice(clip);

        pendingWrongChoiceVoice = null;
    }

    void CancelPendingWrongChoiceVoice()
    {
        if (pendingWrongChoiceVoice == null)
            return;

        StopCoroutine(pendingWrongChoiceVoice);
        pendingWrongChoiceVoice = null;
    }

    bool AllowsPpeFeedbackPresentation()
    {
        return voiceFlowDirector == null ||
            voiceFlowDirector.ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education;
    }

    bool AllowsPpeActionSfx()
    {
        return voiceFlowDirector == null || voiceFlowDirector.AllowsPpeActionSfx;
    }

    bool AllowsPpeChoiceVoice()
    {
        return voiceFlowDirector == null || voiceFlowDirector.AllowsPpeChoiceVoice;
    }

    public bool IsBodyProximityActivateAttempt()
    {
        if (!approveUseByBodyProximity ||
            inspectionState == null ||
            !inspectionState.IsBeingInspected ||
            pendingApprovedRemoval != null)
        {
            return false;
        }

        if (!IsWithinBodyAttachRange())
            return false;

        return inspectionState.CurrentCondition != PPEItemCondition.Clean ||
            !IsGrabbingGloveWithWearHand();
    }

    public bool TryGetGameViewBodyAttachTarget(out Vector3 target)
    {
        target = default;
        return approveUseByBodyProximity && TryGetBodyAttachTarget(out target);
    }

    public bool TryActivateBodyProximityFromGameView(IXRSelectInteractor interactor)
    {
        if (!approveUseByBodyProximity ||
            !IsSelectedByInteractor(interactor) ||
            !CanResolveChoice() ||
            !IsWithinBodyAttachRange())
        {
            return false;
        }

        // Game View intentionally drives both glove sides through the authored
        // right-hand NearFarInteractor. Do not reuse the physical-XR handedness
        // suppression in IsBodyProximityActivateAttempt(): it would block only
        // the right glove while letting the left glove pass by mismatch.
        ResolveUseChoice();
        return true;
    }

    public bool IsSelectedByInteractor(IXRSelectInteractor interactor)
    {
        XRGrabInteractable grabInteractable = inspectionState != null
            ? inspectionState.GrabInteractable
            : null;
        return grabInteractable != null &&
            interactor != null &&
            grabInteractable.interactorsSelecting.Contains(interactor);
    }

    bool CanResolveChoice()
    {
        if (inspectionState == null ||
            !inspectionState.IsBeingInspected ||
            pendingApprovedRemoval != null)
        {
            return false;
        }

        // Body-proximity wear does not show the shared action panel, so it must
        // not depend on static panel ownership. Extra PPE copies can steal that
        // owner without grabbing the boot and then swallow trigger + SFX.
        if (approveUseByBodyProximity)
            return true;

        return activePanelOwner == this;
    }

    void ApplyChoicePresentation(PPEActionResult result)
    {
        switch (result)
        {
            case PPEActionResult.UseApproved:
                ApplyFeedback(useApprovedMessage, acceptedFeedbackColor, usePassIconRoot);
                break;
            case PPEActionResult.UseRejectedContaminated:
                ApplyFeedback(contaminatedUseMessage, rejectedFeedbackColor, useErrorIconRoot);
                break;
            case PPEActionResult.DiscardApprovedContaminated:
                ApplyFeedback(
                    contaminatedDiscardMessage,
                    acceptedFeedbackColor,
                    discardPassIconRoot);
                break;
            case PPEActionResult.DiscardCleanPolicyPending:
                ApplyFeedback(
                    cleanDiscardPendingMessage,
                    pendingFeedbackColor,
                    discardErrorIconRoot);
                break;
            case PPEActionResult.InspectCompleted:
                GameObject inspectIcon =
                    inspectPassIconRoot != null ? inspectPassIconRoot : usePassIconRoot;
                ApplyFeedback(inspectCompletedMessage, acceptedFeedbackColor, inspectIcon);
                if (inspectPassIconRoot == null && applySharedPresentationLayout && sharedPresentation != null)
                    sharedPresentation.PlaceFallbackInspectIcon(inspectIcon);
                break;
        }
    }

    bool WasSelectingActivatePressedThisFrame()
    {
        XRGrabInteractable grabInteractable = inspectionState.GrabInteractable;
        if (grabInteractable == null)
            return false;

        foreach (IXRSelectInteractor interactor in grabInteractable.interactorsSelecting)
        {
            if (interactor is not XRBaseInputInteractor inputInteractor)
                continue;

            XRInputButtonReader activateInput = inputInteractor.activateInput;
            if (activateInput != null && activateInput.ReadWasPerformedThisFrame())
                return true;
        }

        return false;
    }

    bool IsWithinBodyAttachRange()
    {
        if (TryGetBootAttachSegment(out Vector3 chest, out Vector3 lower))
        {
            bool handIsNear = TryGetAttachProbePosition(out Vector3 bootProbe) &&
                DistancePointToSegment(bootProbe, chest, lower) <= bodyAttachDistance;
            return handIsNear || IsGrabColliderNearSegment(chest, lower);
        }

        if (!TryGetBodyAttachTarget(out Vector3 target))
            return false;

        bool handIsNearTarget = TryGetAttachProbePosition(out Vector3 probe) &&
            Vector3.Distance(probe, target) <= bodyAttachDistance;
        return handIsNearTarget || IsGrabColliderNearPoint(target);
    }

    bool IsGrabColliderNearPoint(Vector3 target)
    {
        XRGrabInteractable grabInteractable = inspectionState?.GrabInteractable;
        if (grabInteractable == null)
            return false;

        foreach (Collider grabCollider in grabInteractable.colliders)
        {
            if (IsPointWithinColliderRange(grabCollider, target, bodyAttachDistance))
                return true;
        }

        return false;
    }

    bool IsGrabColliderNearSegment(Vector3 a, Vector3 b)
    {
        XRGrabInteractable grabInteractable = inspectionState?.GrabInteractable;
        if (grabInteractable == null)
            return false;

        foreach (Collider grabCollider in grabInteractable.colliders)
        {
            if (IsSegmentWithinColliderRange(grabCollider, a, b, bodyAttachDistance))
                return true;
        }

        return false;
    }

    static bool IsPointWithinColliderRange(Collider collider, Vector3 point, float range)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;

        Vector3 closestPoint = collider.ClosestPoint(point);
        return (closestPoint - point).sqrMagnitude <= range * range;
    }

    static bool IsSegmentWithinColliderRange(
        Collider collider,
        Vector3 a,
        Vector3 b,
        float range)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;

        Vector3 segmentPoint = ClosestPointOnSegment(collider.bounds.center, a, b);
        Vector3 colliderPoint = collider.ClosestPoint(segmentPoint);
        segmentPoint = ClosestPointOnSegment(colliderPoint, a, b);
        colliderPoint = collider.ClosestPoint(segmentPoint);
        return (colliderPoint - segmentPoint).sqrMagnitude <= range * range;
    }

    bool TryGetBootAttachSegment(out Vector3 chest, out Vector3 lower)
    {
        chest = default;
        lower = default;

        if (bodyAttachAnchor != null)
            return false;

        PPEItemType? itemType = inspectionState.PresentationBinding?.ItemIdentity?.ItemType;
        bool isBoot = itemType == PPEItemType.RubberBootLeft ||
            itemType == PPEItemType.RubberBootRight;
        if (!isBoot)
            return false;

        PPEHazmatEquipController hazmat = ResolveHazmatEquip();
        if (hazmat == null || hazmat.HeadTransform == null)
            return false;

        Transform head = hazmat.HeadTransform;
        chest = OffsetInFrontOfBody(
            GetChestPoint(head, bodyAttachChestDropMeters),
            head,
            hazmat);
        Vector3 lowerSource = hazmat.IsEquipped && hazmat.BodyAnchor != null
            ? hazmat.BodyAnchor.position
            : GetChestPoint(head, bodyAttachBootDropMeters);
        lower = OffsetInFrontOfBody(lowerSource, head, hazmat);
        return true;
    }

    static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        return Vector3.Distance(point, ClosestPointOnSegment(point, a, b));
    }

    static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float lengthSq = ab.sqrMagnitude;
        if (lengthSq < 0.0001f)
            return a;

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
        return a + ab * t;
    }

    bool TryGetAttachProbePosition(out Vector3 probe)
    {
        if (inspectionState.TryGetSelectingInteractorTransform(out Transform interactorTransform))
        {
            probe = interactorTransform.position;
            return true;
        }

        probe = default;
        return false;
    }

    bool TryGetBodyAttachTarget(out Vector3 target)
    {
        target = default;

        if (bodyAttachAnchor != null)
        {
            target = bodyAttachAnchor.position;
            return true;
        }

        PPEHazmatEquipController hazmat = ResolveHazmatEquip();
        if (hazmat == null || hazmat.HeadTransform == null)
        {
            if (!loggedMissingBodyAttach)
            {
                loggedMissingBodyAttach = true;
                Debug.LogError(
                    "PPEActionPanelController cannot approve by body proximity because " +
                    "bodyAttachAnchor is empty and PPEHazmatEquipController.HeadTransform is missing.",
                    this);
            }

            return false;
        }

        Transform head = hazmat.HeadTransform;
        PPEItemType? itemType = inspectionState.PresentationBinding?.ItemIdentity?.ItemType;
        bool wearRightGlove = itemType == PPEItemType.RubberGloveRight ||
            itemType == PPEItemType.NitrileInnerGloveRight;
        bool wearLeftGlove = itemType == PPEItemType.RubberGloveLeft ||
            itemType == PPEItemType.NitrileInnerGloveLeft;
        if (wearRightGlove || wearLeftGlove)
        {
            if (!hazmat.TryGetWearHandModel(wearRightGlove, out Transform wearHand))
            {
                if (!loggedMissingBodyAttach)
                {
                    loggedMissingBodyAttach = true;
                    Debug.LogError(
                        "PPEActionPanelController cannot fit a glove because the wear-hand model is missing.",
                        this);
                }

                return false;
            }

            target = wearHand.position;
            return true;
        }

        if (itemType == PPEItemType.ConstructionHelmet ||
            itemType == PPEItemType.GasMask ||
            itemType == PPEItemType.ScubaGear ||
            itemType == PPEItemType.FaceShield ||
            itemType == PPEItemType.SafetyGoggles)
        {
            target = OffsetInFrontOfBody(head.position, head, hazmat);
            return true;
        }

        target = OffsetInFrontOfBody(
            GetChestPoint(head, bodyAttachChestDropMeters),
            head,
            hazmat);
        return true;
    }

    bool IsGrabbingGloveWithWearHand()
    {
        PPEItemType? itemType = inspectionState.PresentationBinding?.ItemIdentity?.ItemType;
        if (itemType != PPEItemType.RubberGloveLeft &&
            itemType != PPEItemType.RubberGloveRight &&
            itemType != PPEItemType.NitrileInnerGloveLeft &&
            itemType != PPEItemType.NitrileInnerGloveRight)
        {
            return false;
        }

        if (!inspectionState.TryGetSelectingHandedness(out InteractorHandedness handedness))
            return false;

        bool wearRight = itemType == PPEItemType.RubberGloveRight ||
            itemType == PPEItemType.NitrileInnerGloveRight;
        return wearRight
            ? handedness == InteractorHandedness.Right
            : handedness == InteractorHandedness.Left;
    }

    static Vector3 GetChestPoint(Transform head, float chestDropMeters)
    {
        Vector3 chest = head.position;
        chest.y -= chestDropMeters;
        return chest;
    }

    Vector3 OffsetInFrontOfBody(
        Vector3 origin,
        Transform head,
        PPEHazmatEquipController hazmat)
    {
        if (bodyAttachForwardMeters <= 0f)
            return origin;

        return origin + GetTorsoForward(head, hazmat) * bodyAttachForwardMeters;
    }

    static Vector3 GetTorsoForward(Transform head, PPEHazmatEquipController hazmat)
    {
        Transform yawSource = hazmat != null && hazmat.IsEquipped && hazmat.BodyAnchor != null
            ? hazmat.BodyAnchor
            : head;
        Vector3 forward = yawSource.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

    PPEHazmatEquipController ResolveHazmatEquip()
    {
        if (cachedHazmatEquip != null)
            return cachedHazmatEquip;

        if (voiceFlowDirector != null)
            cachedHazmatEquip = voiceFlowDirector.HazmatEquipController;

        if (cachedHazmatEquip == null)
            cachedHazmatEquip = FindAnyObjectByType<PPEHazmatEquipController>();

        return cachedHazmatEquip;
    }

    void WireButtonListeners(bool bind)
    {
        if (useButton != null && OwnsUi(useButton.gameObject))
        {
            if (bind)
                useButton.onClick.AddListener(ChooseUse);
            else
                useButton.onClick.RemoveListener(ChooseUse);
        }

        if (discardButton != null && OwnsUi(discardButton.gameObject))
        {
            if (bind)
                discardButton.onClick.AddListener(ChooseDiscard);
            else
                discardButton.onClick.RemoveListener(ChooseDiscard);
        }

        if (inspectButton != null && OwnsUi(inspectButton.gameObject))
        {
            if (bind)
                inspectButton.onClick.AddListener(ChooseInspect);
            else
                inspectButton.onClick.RemoveListener(ChooseInspect);
        }
    }

    void RefreshInspectButtonVisibility()
    {
        if (inspectButton == null)
            return;

        inspectButton.gameObject.SetActive(enableInspectChoice);
    }

    void ApplyAuthoredLayout()
    {
        if (applySharedPresentationLayout &&
            sharedPresentation != null &&
            OwnsUi(sharedPresentation.gameObject))
            sharedPresentation.Apply(enableInspectChoice);
    }

    void BeginApprovedResolution(PPEActionResult result)
    {
        CancelPendingApprovedRemoval();
        pendingApprovedRemoval = StartCoroutine(ResolveApprovedAfterDelay(result));
    }

    IEnumerator ResolveApprovedAfterDelay(PPEActionResult result)
    {
        if (approvedRemovalDelaySeconds > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(approvedRemovalDelaySeconds);
            else
                yield return new WaitForSeconds(approvedRemovalDelaySeconds);
        }

        if (!TryCancelInspectionSelection())
        {
            pendingApprovedRemoval = null;
            yield break;
        }

        // Let XRI finish selectExited and PPEMarkerToggleGrab restore the authored pose
        // before the inspection visual is hidden or its condition is changed.
        yield return null;

        pendingApprovedRemoval = null;

        if (result == PPEActionResult.DiscardApprovedContaminated)
        {
            inspectionState.SetCondition(PPEItemCondition.Clean);
            yield break;
        }

        if (result == PPEActionResult.UseApproved && hideInspectionVisualAfterUse)
            HideInspectionVisual();
    }

    bool TryCancelInspectionSelection()
    {
        XRGrabInteractable grabInteractable = inspectionState.GrabInteractable;
        if (grabInteractable.interactorsSelecting.Count == 0)
            return true;

        XRInteractionManager interactionManager = grabInteractable.interactionManager;
        if (interactionManager == null)
        {
            Debug.LogError(
                "Cannot resolve the approved PPE action because the selected XRGrabInteractable " +
                "has no XRInteractionManager.",
                this);
            return false;
        }

        interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);

        if (grabInteractable.interactorsSelecting.Count == 0)
            return true;

        Debug.LogError(
            "Cannot resolve the approved PPE action because XRI did not finish cancelling selection.",
            this);
        return false;
    }

    void ApplyFeedback(string message, Color color, GameObject statusIconRoot)
    {
        if (!OwnsUi(feedbackLabel != null ? feedbackLabel.gameObject : null) ||
            !OwnsUi(feedbackRoot))
            return;

        feedbackLabel.text = message;
        feedbackLabel.color = color;
        feedbackRoot.SetActive(true);
        HideAllStatusIcons();
        if (statusIconRoot != null && OwnsUi(statusIconRoot))
            statusIconRoot.SetActive(true);
    }

    void ResetFeedback()
    {
        if (OwnsUi(feedbackRoot))
            feedbackRoot.SetActive(false);
        HideAllStatusIcons();
    }

    void HideAllStatusIcons()
    {
        SetOwnedActive(usePassIconRoot, false);
        SetOwnedActive(useErrorIconRoot, false);
        SetOwnedActive(discardPassIconRoot, false);
        SetOwnedActive(discardErrorIconRoot, false);
        SetOwnedActive(inspectPassIconRoot, false);

        if (applySharedPresentationLayout &&
            sharedPresentation != null &&
            OwnsUi(sharedPresentation.gameObject))
            sharedPresentation.RestoreStatusIconPositions(enableInspectChoice);
    }

    void SetOwnedActive(GameObject target, bool active)
    {
        if (OwnsUi(target))
            target.SetActive(active);
    }

    bool OwnsUi(GameObject target)
    {
        return target != null && target.transform.IsChildOf(transform);
    }

    void CancelPendingShow()
    {
        if (pendingShow == null)
            return;

        StopCoroutine(pendingShow);
        pendingShow = null;
    }

    void CancelPendingGrabNameHide()
    {
        if (pendingGrabNameHide == null)
            return;

        StopCoroutine(pendingGrabNameHide);
        pendingGrabNameHide = null;
    }

    void CancelPendingApprovedRemoval()
    {
        if (pendingApprovedRemoval == null)
            return;

        StopCoroutine(pendingApprovedRemoval);
        pendingApprovedRemoval = null;
    }

    void TakePanelOwnership()
    {
        if (activePanelOwner == this)
            return;

        if (activePanelOwner != null)
            activePanelOwner.ReleasePanelOwnership();

        activePanelOwner = this;
    }

    void ReleasePanelOwnership()
    {
        CancelPendingShow();
        CancelPendingGrabNameHide();
        panelRoot.SetActive(false);

        if (activePanelOwner == this)
            activePanelOwner = null;
    }

    void CaptureInspectionVisibility()
    {
        var renderers = new List<Renderer>();
        foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
        {
            if (candidate != null && !candidate.transform.IsChildOf(panelRoot.transform))
                renderers.Add(candidate);
        }

        inspectionRenderers = renderers.ToArray();
        authoredRendererEnabled = new bool[inspectionRenderers.Length];
        for (int index = 0; index < inspectionRenderers.Length; index++)
            authoredRendererEnabled[index] = inspectionRenderers[index].enabled;

        var colliders = new List<Collider>();
        foreach (Collider candidate in GetComponentsInChildren<Collider>(true))
        {
            if (candidate != null && !candidate.transform.IsChildOf(panelRoot.transform))
                colliders.Add(candidate);
        }

        inspectionColliders = colliders.ToArray();
        authoredColliderEnabled = new bool[inspectionColliders.Length];
        for (int index = 0; index < inspectionColliders.Length; index++)
            authoredColliderEnabled[index] = inspectionColliders[index].enabled;

        authoredGrabEnabled = inspectionState.GrabInteractable.enabled;
        hasInspectionVisibilitySnapshot = true;
    }

    void HideInspectionVisual()
    {
        if (!hasInspectionVisibilitySnapshot || inspectionVisualHidden)
            return;

        foreach (Renderer targetRenderer in inspectionRenderers)
        {
            if (targetRenderer != null &&
                IsInspectionTransform(targetRenderer.transform))
                targetRenderer.enabled = false;
        }

        foreach (Collider targetCollider in inspectionColliders)
        {
            if (targetCollider != null &&
                IsInspectionTransform(targetCollider.transform))
                targetCollider.enabled = false;
        }

        inspectionState.GrabInteractable.enabled = false;
        inspectionVisualHidden = true;
    }

    /// <summary>
    /// Reusable PPE keeps its authored inspection visual after ordinary Use actions.
    /// Its state owner calls this only when no further authored use remains.
    /// </summary>
    public void HideInspectionVisualAfterReusableUse()
    {
        HideInspectionVisual();
    }

    void RestoreInspectionVisibility()
    {
        if (!hasInspectionVisibilitySnapshot)
            return;

        for (int index = 0; index < inspectionRenderers.Length; index++)
        {
            if (inspectionRenderers[index] != null &&
                IsInspectionTransform(inspectionRenderers[index].transform))
                inspectionRenderers[index].enabled = authoredRendererEnabled[index];
        }

        for (int index = 0; index < inspectionColliders.Length; index++)
        {
            if (inspectionColliders[index] != null &&
                IsInspectionTransform(inspectionColliders[index].transform))
                inspectionColliders[index].enabled = authoredColliderEnabled[index];
        }

        if (inspectionState != null && inspectionState.GrabInteractable != null)
            inspectionState.GrabInteractable.enabled = authoredGrabEnabled;

        inspectionVisualHidden = false;
    }

    bool IsInspectionTransform(Transform candidate)
    {
        return candidate == inspectionState.transform ||
            candidate.IsChildOf(inspectionState.transform);
    }

    bool HasCompleteReferences()
    {
        bool inspectReady = !enableInspectChoice || inspectButton != null;

        return inspectionState != null &&
            panelRoot != null &&
            itemNameLabel != null &&
            !string.IsNullOrWhiteSpace(itemDisplayName) &&
            useButton != null &&
            discardButton != null &&
            inspectReady &&
            feedbackRoot != null &&
            feedbackLabel != null &&
            usePassIconRoot != null &&
            useErrorIconRoot != null &&
            discardPassIconRoot != null &&
            discardErrorIconRoot != null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Connects a panel authored in the scene without changing its layout, visuals,
    /// labels, or other presentation values.
    /// </summary>
    public void BindSceneAuthoredPanelForEditor(
        GameObject configuredPanelRoot,
        Transform configuredPanelPose,
        PPEActionPanelSharedPresentation configuredSharedPresentation,
        TMP_Text configuredItemNameLabel,
        Button configuredUseButton,
        Button configuredDiscardButton,
        Button configuredInspectButton,
        bool configuredEnableInspectChoice,
        GameObject configuredFeedbackRoot,
        TMP_Text configuredFeedbackLabel,
        GameObject configuredUsePassIconRoot,
        GameObject configuredUseErrorIconRoot,
        GameObject configuredDiscardPassIconRoot,
        GameObject configuredDiscardErrorIconRoot,
        GameObject configuredInspectPassIconRoot)
    {
        panelRoot = configuredPanelRoot;
        panelPose = configuredPanelPose;
        sharedPresentation = configuredSharedPresentation;
        itemNameLabel = configuredItemNameLabel;
        useButton = configuredUseButton;
        discardButton = configuredDiscardButton;
        inspectButton = configuredInspectButton;
        enableInspectChoice = configuredEnableInspectChoice;
        feedbackRoot = configuredFeedbackRoot;
        feedbackLabel = configuredFeedbackLabel;
        usePassIconRoot = configuredUsePassIconRoot;
        useErrorIconRoot = configuredUseErrorIconRoot;
        discardPassIconRoot = configuredDiscardPassIconRoot;
        discardErrorIconRoot = configuredDiscardErrorIconRoot;
        inspectPassIconRoot = configuredInspectPassIconRoot;
    }

    public void ConfigureSharedPanelForEditor(
        PPEInspectionState configuredInspectionState,
        GameObject configuredPanelRoot,
        Transform configuredPanelPose,
        PPEActionPanelSharedPresentation configuredSharedPresentation,
        TMP_Text configuredItemNameLabel,
        string configuredItemDisplayName,
        Button configuredUseButton,
        Button configuredDiscardButton,
        Button configuredInspectButton,
        bool configuredEnableInspectChoice,
        GameObject configuredFeedbackRoot,
        TMP_Text configuredFeedbackLabel,
        GameObject configuredUsePassIconRoot,
        GameObject configuredUseErrorIconRoot,
        GameObject configuredDiscardPassIconRoot,
        GameObject configuredDiscardErrorIconRoot,
        GameObject configuredInspectPassIconRoot,
        string configuredInspectCompletedMessage)
    {
        inspectionState = configuredInspectionState;
        panelRoot = configuredPanelRoot;
        panelPose = configuredPanelPose;
        sharedPresentation = configuredSharedPresentation;
        itemNameLabel = configuredItemNameLabel;
        itemDisplayName = configuredItemDisplayName;
        useButton = configuredUseButton;
        discardButton = configuredDiscardButton;
        inspectButton = configuredInspectButton;
        enableInspectChoice = configuredEnableInspectChoice;
        feedbackRoot = configuredFeedbackRoot;
        feedbackLabel = configuredFeedbackLabel;
        usePassIconRoot = configuredUsePassIconRoot;
        useErrorIconRoot = configuredUseErrorIconRoot;
        discardPassIconRoot = configuredDiscardPassIconRoot;
        discardErrorIconRoot = configuredDiscardErrorIconRoot;
        inspectPassIconRoot = configuredInspectPassIconRoot;
        inspectCompletedMessage = configuredInspectCompletedMessage;
        showDelaySeconds = 0f;
        useUnscaledTime = true;
        hideWhenInspectionEnds = true;
        approvedRemovalDelaySeconds = 0.25f;
        useApprovedMessage = "사용 선택 완료";
        contaminatedUseMessage = "사용할 수 없습니다";
        contaminatedDiscardMessage = "폐기 선택 완료";
        cleanDiscardPendingMessage = "깨끗한 PPE 폐기 정책 확인 필요";
        if (string.IsNullOrWhiteSpace(inspectCompletedMessage))
            inspectCompletedMessage = "확인 완료";
        acceptedFeedbackColor = new Color(0.35f, 1f, 0.7f, 1f);
        rejectedFeedbackColor = new Color(1f, 0.25f, 0.2f, 1f);
        pendingFeedbackColor = new Color(1f, 0.75f, 0.2f, 1f);
        logChoices = true;
    }
#endif
}
