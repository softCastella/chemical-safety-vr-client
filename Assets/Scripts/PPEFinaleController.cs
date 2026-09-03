using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[DisallowMultipleComponent]
public sealed class PPEFinaleController : MonoBehaviour
{
    [Header("Completion requirements")]
    [SerializeField] private PPEHazmatEquipController m_HazmatEquipController;
    [SerializeField] private PPEEquipmentVisualController m_EquipmentVisualController;
    [SerializeField] private PPETabletChecklistController m_TabletChecklistController;
    [SerializeField] private PPEVoiceFlowDirector m_VoiceFlowDirector;

    [Header("Mirror observation")]
    [SerializeField] private XROrigin m_XrOrigin;
    [SerializeField] private Transform m_MirrorObservationPoint;
    [SerializeField, Min(0.1f)] private float m_MirrorObservationRadius = 1.2f;
    [SerializeField, Min(0f)] private float m_ObservationSeconds = 5f;

    [Header("Authored finale UI")]
    [SerializeField] private GameObject m_ObservationGaugeRoot;
    [SerializeField] private Image m_ObservationGaugeFill;
    [SerializeField] private CanvasGroup m_FadeCanvasGroup;
    [SerializeField, Min(0f)] private float m_FadeDuration = 0.75f;

    [Header("Completion return")]
    [SerializeField] private TeleportationProvider m_TeleportationProvider;
    [SerializeField] private Transform m_ReturnDestination;
    [SerializeField] private GameObject m_ScenarioCardCanvas;

    private enum FinaleState { WaitingForEquipment, WaitingForMirror, Observing, WaitingForQuiz, Finishing, Complete }
    private FinaleState m_State;
    private float m_ObservationElapsed;
    private Coroutine m_MirrorRoutine;
    private bool m_ReturnInProgress;
    private bool m_MirrorObservationArmed = true;
    private bool m_WasInsideMirrorRadius;

    private void Awake()
    {
        ResetGaugeFill();
        SetGaugeVisible(false);
        SetFadeAlpha(0f);
    }

    private void Update()
    {
        bool insideMirrorRadius = IsWithinMirrorRadius();
        UpdateMirrorReentryArm(insideMirrorRadius);

        if (m_State == FinaleState.WaitingForEquipment)
        {
            if (m_VoiceFlowDirector == null || !m_VoiceFlowDirector.IsActivePpeModeSession)
                return;

            m_State = FinaleState.WaitingForMirror;

            // Completion is evaluated after the authored mirror observation.
            // An incomplete result returns here, but re-entry remains armed only
            // after the player leaves the observation radius.
            if (CanStartMirrorObservation(insideMirrorRadius))
                NotifyMirrorArrivalFromPosition();
            return;
        }

        // The marker relay is the normal entry path, but the player can also
        // arrive inside the authored mirror observation radius through another
        // accepted teleport path. Keep the mirror presentation state owned by
        // this finale controller in that case as well.
        if (CanStartMirrorObservation(insideMirrorRadius))
            NotifyMirrorArrivalFromPosition();
    }

    /// <summary>Called only by the serialized mirror location marker relay.</summary>
    public void NotifyMirrorMarkerArrived()
    {
        if (!CanAcceptMirrorArrival())
            return;

        StartMirrorCheck();
    }

    /// <summary>Called by the voice flow immediately after the quiz UI becomes visible.</summary>
    public void NotifyQuizPresented()
    {
        m_EquipmentVisualController?.SetHandModelsVisible(true);
        XRNearFarReticleVisual.SetFinaleRayVisualsVisible(true);
    }

    private void NotifyMirrorArrivalFromPosition()
    {
        // Route position-based arrival through the same voice event as the
        // authored teleport marker. This stops the move-to-mirror instruction,
        // plays the mirror-check narration, then starts the observation once.
        if (m_VoiceFlowDirector != null)
            m_VoiceFlowDirector.NotifyMirrorMarkerArrived();
        else
            StartMirrorCheck();
    }

    private bool CanAcceptMirrorArrival()
    {
        return m_State != FinaleState.Observing &&
            m_State != FinaleState.WaitingForQuiz &&
            m_State != FinaleState.Finishing &&
            m_State != FinaleState.Complete &&
            m_MirrorRoutine == null &&
            m_MirrorObservationArmed;
    }

    private bool CanStartMirrorObservation(bool insideMirrorRadius)
    {
        return m_State == FinaleState.WaitingForMirror &&
            insideMirrorRadius &&
            CanAcceptMirrorArrival();
    }

    private void UpdateMirrorReentryArm(bool insideMirrorRadius)
    {
        if (m_WasInsideMirrorRadius && !insideMirrorRadius && m_MirrorRoutine == null)
            m_MirrorObservationArmed = true;

        m_WasInsideMirrorRadius = insideMirrorRadius;
    }

    private void ArmMirrorObservationIfOutsideRadius()
    {
        m_MirrorObservationArmed = !IsWithinMirrorRadius();
    }

    private void ResetMirrorObservationArm()
    {
        m_MirrorObservationArmed = true;
        m_WasInsideMirrorRadius = false;
    }

    private void StartMirrorCheck()
    {
        // The mirror check intentionally hides the current hand variant. Its
        // exact state is restored on an incomplete result or when the quiz UI
        // opens, so this does not replace scene-authored hand presentation.
        m_EquipmentVisualController?.SetHandModelsVisible(false);
        XRNearFarReticleVisual.SetFinaleRayVisualsVisible(false);

        m_MirrorObservationArmed = false;
        if (m_MirrorRoutine != null)
            StopCoroutine(m_MirrorRoutine);
        m_MirrorRoutine = StartCoroutine(MirrorCheckRoutine());
    }

    private bool IsWithinMirrorRadius()
    {
        if (m_XrOrigin == null || m_XrOrigin.Camera == null || m_MirrorObservationPoint == null)
            return false;

        Transform cameraTransform = m_XrOrigin.Camera.transform;
        Vector3 toMirror = m_MirrorObservationPoint.position - cameraTransform.position;
        Vector3 horizontalToMirror = Vector3.ProjectOnPlane(toMirror, Vector3.up);
        if (horizontalToMirror.magnitude > m_MirrorObservationRadius)
            return false;

        return true;
    }

    private IEnumerator MirrorCheckRoutine()
    {
        // 013 is started by PPE Voice Flow immediately before this method.
        while (m_VoiceFlowDirector != null && m_VoiceFlowDirector.IsVoicePlaying)
        {
            if (!IsWithinMirrorRadius())
            {
                AbortInterruptedMirrorCheck();
                yield break;
            }

            yield return null;
        }

        m_State = FinaleState.Observing;
        m_ObservationElapsed = 0f;
        ResetGaugeFill();
        SetGaugeVisible(true);
        while (m_ObservationElapsed < m_ObservationSeconds)
        {
            if (!IsWithinMirrorRadius())
            {
                AbortInterruptedMirrorCheck();
                yield break;
            }

            m_ObservationElapsed += Time.unscaledDeltaTime;
            if (m_ObservationGaugeFill != null)
                m_ObservationGaugeFill.fillAmount = m_ObservationSeconds <= 0f ? 1f : m_ObservationElapsed / m_ObservationSeconds;

            yield return null;
        }

        SetGaugeVisible(false);
        ResetGaugeFill();
        bool ppeComplete = HasPpeCompletionRequirements();
        bool tabletComplete = HasTabletCompletionRequirement();
        if (!ppeComplete || !tabletComplete)
        {
            // The player must be able to see their current hand state while
            // completing the missing PPE after the failed mirror check.
            m_EquipmentVisualController?.SetHandModelsVisible(true);
            XRNearFarReticleVisual.SetFinaleRayVisualsVisible(true);
            m_VoiceFlowDirector?.NotifyFinaleResult(ppeComplete, tabletComplete);
            while (m_VoiceFlowDirector != null && m_VoiceFlowDirector.IsVoicePlaying)
                yield return null;
            m_State = FinaleState.WaitingForEquipment;
            m_MirrorRoutine = null;
            ArmMirrorObservationIfOutsideRadius();
            yield break;
        }

        m_State = FinaleState.WaitingForQuiz;
        m_MirrorRoutine = null;
        ArmMirrorObservationIfOutsideRadius();
        m_VoiceFlowDirector?.NotifyQuizStart();
    }

    private void AbortInterruptedMirrorCheck()
    {
        SetGaugeVisible(false);
        ResetGaugeFill();
        m_EquipmentVisualController?.SetHandModelsVisible(true);
        XRNearFarReticleVisual.SetFinaleRayVisualsVisible(true);
        m_State = FinaleState.WaitingForMirror;
        m_MirrorRoutine = null;
        m_MirrorObservationArmed = true;
    }

    /// <summary>Called after all authored quiz pages finish under the active mode policy.</summary>
    public void NotifyQuizCompleted()
    {
        if (m_State == FinaleState.Finishing || m_State == FinaleState.Complete)
            return;

        // The quiz has closed, but the player is still facing the mirror while
        // the completion narration and return transition run.
        m_EquipmentVisualController?.SetHandModelsVisible(false);
        XRNearFarReticleVisual.SetFinaleRayVisualsVisible(false);
        StartCoroutine(FinishAfterQuizVoice());
    }

    private IEnumerator FinishAfterQuizVoice()
    {
        while (m_VoiceFlowDirector != null && m_VoiceFlowDirector.IsVoicePlaying)
            yield return null;

        if (m_VoiceFlowDirector != null &&
            m_VoiceFlowDirector.ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Test)
        {
            m_EquipmentVisualController?.SetHandModelsVisible(true);
            XRNearFarReticleVisual.SetFinaleRayVisualsVisible(true);
            m_VoiceFlowDirector.ShowTestResultAfterVoice();
            m_State = FinaleState.Complete;
            yield break;
        }

        yield return ReturnToModeChoices(true);
    }

    /// <summary>
    /// Called by the exit teleport marker once its teleport to the authored
    /// return anchor has been accepted. This is the only early-exit entry point:
    /// merely standing near the start anchor must not end an active session.
    /// </summary>
    public void RequestExitReturn()
    {
        if (m_ReturnInProgress || m_VoiceFlowDirector == null ||
            !m_VoiceFlowDirector.IsActivePpeModeSession)
            return;

        m_ReturnInProgress = true;
        if (m_MirrorRoutine != null)
        {
            StopCoroutine(m_MirrorRoutine);
            m_MirrorRoutine = null;
        }
        StartCoroutine(ReturnAfterMidExitVoice());
    }

    private IEnumerator ReturnAfterMidExitVoice()
    {
        m_VoiceFlowDirector.NotifyMidExitArrived();
        while (m_VoiceFlowDirector.IsVoicePlaying)
            yield return null;

        yield return ReturnToModeChoices(false);
    }

    public void NotifyCompletionBackRequested()
    {
        if (m_State != FinaleState.Complete || m_VoiceFlowDirector == null ||
            m_VoiceFlowDirector.ActiveLearningMode != ScenarioDetailModal.PpeLearningMode.Test)
        {
            return;
        }

        StartCoroutine(ReturnToModeChoices(true));
    }

    private IEnumerator ReturnToModeChoices(bool completedModeSession)
    {
        m_State = FinaleState.Finishing;
        yield return FadeTo(1f);
        ReturnToStart();

        m_HazmatEquipController?.ResetForNewSession();
        m_EquipmentVisualController?.ResetWornVisualsForCompletionReturn();
        m_TabletChecklistController?.ResetForNewSession();
        m_EquipmentVisualController?.ShowBareHandsForCardSelection();
        m_EquipmentVisualController?.SetHandModelsVisible(true);
        XRNearFarReticleVisual.SetFinaleRayVisualsVisible(true);
        yield return null;

        yield return FadeTo(0f);
        m_VoiceFlowDirector.ShowModeChoicesAfterCompletionReturn(completedModeSession);
        m_VoiceFlowDirector.ResetModeSessionForNextSelection();
        m_State = FinaleState.WaitingForEquipment;
        m_ReturnInProgress = false;
        ResetMirrorObservationArm();
    }

    private bool HasCompletionRequirements()
    {
        return HasPpeCompletionRequirements() && HasTabletCompletionRequirement();
    }

    private bool HasPpeCompletionRequirements()
    {
        return m_HazmatEquipController != null && m_HazmatEquipController.IsEquipped &&
            m_EquipmentVisualController != null && m_EquipmentVisualController.AreAllRequiredSlotsUsed;
    }

    public void ApplyWorkPlanDocument(ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        m_TabletChecklistController?.ApplyWorkPlan(workPlan);
    }

    private bool HasTabletCompletionRequirement()
    {
        return
            m_TabletChecklistController != null && m_TabletChecklistController.IsDocumentCompleted;
    }

    private void ReturnToStart()
    {
        if (m_TeleportationProvider == null || m_ReturnDestination == null)
        {
            Debug.LogError($"{nameof(PPEFinaleController)} requires authored teleport provider and return destination references.", this);
            return;
        }

        m_TeleportationProvider.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = m_ReturnDestination.position,
            destinationRotation = m_ReturnDestination.rotation,
            matchOrientation = MatchOrientation.TargetUpAndForward,
            requestTime = Time.time,
        });
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (m_FadeCanvasGroup == null)
            yield break;

        float startAlpha = m_FadeCanvasGroup.alpha;
        if (m_FadeDuration <= 0f)
        {
            SetFadeAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < m_FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / m_FadeDuration)));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetGaugeVisible(bool visible)
    {
        if (m_ObservationGaugeRoot != null)
            m_ObservationGaugeRoot.SetActive(visible);
    }

    private void ResetGaugeFill()
    {
        if (m_ObservationGaugeFill != null)
            m_ObservationGaugeFill.fillAmount = 0f;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (m_FadeCanvasGroup == null)
            return;

        m_FadeCanvasGroup.alpha = alpha;
        m_FadeCanvasGroup.blocksRaycasts = alpha > 0.001f;
    }
}
