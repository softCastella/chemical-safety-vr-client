using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Captures one real PPE play session as JSONL from existing runtime state and
/// events. It does not calculate scores or invent user identity. The output is
/// intended for inspection before a server/dashboard contract is implemented.
/// </summary>
public sealed class PPETrainingTelemetryCapture : MonoBehaviour
{
    public const int CurrentSchemaVersion = 1;
    public const string SourceProject = "chemical-safety-vr-client";
    const float GrabAttemptResolutionWindowSeconds = 0.5f;

    public static string CurrentSessionId => instance?.sessionId;
    public static string CurrentScenePath => SceneManager.GetActiveScene().path;
    public static string CurrentMode => instance?.director?.ActiveLearningMode.ToString();
    public static string CurrentWorkPlan => instance?.director?.ActiveWorkPlan.ToString();
    public static string CurrentFlowState => instance?.director?.CurrentState.ToString();
    public static bool HasActivePpeModeSession =>
        instance?.director != null && instance.director.IsActivePpeModeSession;
    internal static event Action TelemetryRecordAppended;

    [Serializable]
    sealed class Record
    {
        public int schemaVersion;
        public string sourceProject;
        public string sessionId;
        public string eventId;
        public int sequence;
        public string timestampUtc;
        public string eventType;
        public string appVersion;
        public string scene;
        public string mode;
        public string workPlan;
        public string modeSessionId;
        public string flowState;
        public string itemType;
        public string itemName;
        public string condition;
        public string choice;
        public string result;
        public string note;
        public string metaProbeState;
        public string metaWelcomeState;
        public string metaAppScopedUserId;
        public string requiredPpeCheck;
        public string missingRequiredPpe;
        public string audioClip;
        public float audioLengthSec;
        public float audioElapsedSec;
        public string attemptId;
        public string hand;
        public string inputControl;
        public int hoveredPpeCount;
        public string hoveredPpeItems;
        public string attemptOutcome;
        public float attemptElapsedSec;
        public string quizTopic;
        public int quizQuestionIndex;
        public int quizQuestionCount;
        public int quizSelectedOptionIndex;
        public bool quizCorrect;
        public int quizCorrectCount;
        public int ppeWrongCount;
        public float modeElapsedSec;
    }

    sealed class PendingGrabAttempt
    {
        public string attemptId;
        public InteractorHandedness handedness;
        public string inputControl;
        public string hoveredPpeItems;
        public int hoveredPpeCount;
        public string candidateItemType;
        public float startedAt;
    }

    static PPETrainingTelemetryCapture instance;
    readonly HashSet<PPEActionPanelController> panels = new();
    readonly HashSet<PPEInspectionState> inspections = new();
    readonly HashSet<XRGrabInteractable> observedGrabInteractables = new();
    readonly Dictionary<InteractorHandedness, PendingGrabAttempt> pendingGrabAttempts = new();
    PPEVoiceFlowDirector director;
    string outputPath;
    string sessionId;
    int nextSequence;
    string lastFlowState;
    string lastMode;
    string lastWorkPlan;
    string lastMetaIdentityState;
    ulong lastMetaAppScopedUserId;
    string lastRequiredPpeStatus;
    AudioClip activeVoiceClip;
    float activeVoiceStartedAt;
    bool applicationPaused;
    bool sessionEnding;
    bool sessionEnded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticNotifications()
    {
        TelemetryRecordAppended = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (instance != null)
            return;

        GameObject root = new("PPE Training Telemetry Capture");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<PPETrainingTelemetryCapture>();
    }

    public static string RecordModeSessionStarted(
        ScenarioDetailModal.PpeLearningMode mode,
        ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        if (instance == null)
            return null;

        string modeSessionId = Guid.NewGuid().ToString("N");
        instance.Write(
            "mode_session_started",
            mode:mode.ToString(),
            workPlan:workPlan.ToString(),
            modeSessionId:modeSessionId);
        return modeSessionId;
    }

    public static void RecordQuizAnswer(
        string modeSessionId,
        ScenarioDetailModal.PpeLearningMode mode,
        ScenarioDetailModal.PpeWorkPlan workPlan,
        int questionIndex,
        int questionCount,
        string quizTopic,
        int selectedOptionIndex,
        bool correct)
    {
        if (instance == null || string.IsNullOrEmpty(modeSessionId))
            return;

        instance.Write(
            "quiz_answer_resolved",
            mode:mode.ToString(),
            workPlan:workPlan.ToString(),
            modeSessionId:modeSessionId,
            quizTopic:quizTopic,
            quizQuestionIndex:questionIndex,
            quizQuestionCount:questionCount,
            quizSelectedOptionIndex:selectedOptionIndex,
            quizCorrect:correct);
    }

    public static void RecordModeSessionCompleted(
        string modeSessionId,
        ScenarioDetailModal.PpeLearningMode mode,
        ScenarioDetailModal.PpeWorkPlan workPlan,
        int quizCorrectCount,
        int quizQuestionCount,
        int ppeWrongCount,
        float modeElapsedSec)
    {
        if (instance == null || string.IsNullOrEmpty(modeSessionId))
            return;

        instance.Write(
            "mode_session_completed",
            mode:mode.ToString(),
            workPlan:workPlan.ToString(),
            modeSessionId:modeSessionId,
            result:"completed",
            quizCorrectCount:quizCorrectCount,
            quizQuestionCount:quizQuestionCount,
            ppeWrongCount:ppeWrongCount,
            modeElapsedSec:modeElapsedSec);
    }

    public static string RecordApplicationExitRequested()
    {
        if (instance == null)
            return null;

        instance.RecordSessionEnded();
        return instance.sessionId;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        sessionId = Guid.NewGuid().ToString("N");
        string directory = Path.Combine(Application.persistentDataPath, "tyche-training-telemetry");
        Directory.CreateDirectory(directory);
        outputPath = Path.Combine(directory, $"session-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{sessionId}.jsonl");
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += OnApplicationQuitting;
        Application.logMessageReceived += OnUnityLog;
        InputSystem.onActionChange += OnInputActionChange;
        Write("session_started", note:"local_jsonl_created");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnApplicationQuitting;
        Application.logMessageReceived -= OnUnityLog;
        InputSystem.onActionChange -= OnInputActionChange;
    }

    void Update()
    {
        BindRuntimeObjects();
        if (director == null)
            return;

        string flowState = director.CurrentState.ToString();
        string mode = director.ActiveLearningMode.ToString();
        string workPlan = director.ActiveWorkPlan.ToString();
        if (flowState != lastFlowState || mode != lastMode || workPlan != lastWorkPlan)
        {
            lastFlowState = flowState;
            lastMode = mode;
            lastWorkPlan = workPlan;
            Write("flow_state_changed", flowState:flowState, mode:mode, workPlan:workPlan);
        }

        MetaPlatformIdentityProbe probe =
            FindAnyObjectByType<MetaPlatformIdentityProbe>(FindObjectsInactive.Include);
        string metaState = probe == null ? "probe_missing" : probe.State.ToString();
        ulong metaUserId = MetaPlatformIdentityProbe.CurrentAppScopedUserId;
        if (metaState != lastMetaIdentityState || metaUserId != lastMetaAppScopedUserId)
        {
            lastMetaIdentityState = metaState;
            lastMetaAppScopedUserId = metaUserId;
            Write("meta_identity_state_changed", note:metaState);
        }

        string requiredStatus = BuildRequiredPpeStatus(director);
        if (requiredStatus != lastRequiredPpeStatus)
        {
            lastRequiredPpeStatus = requiredStatus;
            string[] parts = requiredStatus.Split('|');
            Write(
                "required_ppe_status_changed",
                note:parts.Length > 1 ? $"missing={parts[1]}" : null,
                requiredPpeCheck:parts.Length > 0 ? parts[0] : "unknown",
                missingRequiredPpe:parts.Length > 1 ? parts[1] : null);
        }

        TrackVoicePlayback();
        ResolveExpiredGrabAttempts();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        director = null;
        panels.Clear();
        inspections.Clear();
        observedGrabInteractables.Clear();
        pendingGrabAttempts.Clear();
        lastFlowState = null;
        lastMode = null;
        lastWorkPlan = null;
        Write("scene_loaded", note:scene.path);
    }

    void BindRuntimeObjects()
    {
        if (director == null)
            director = FindAnyObjectByType<PPEVoiceFlowDirector>(FindObjectsInactive.Include);

        PPEActionPanelController[] currentPanels =
            FindObjectsByType<PPEActionPanelController>(FindObjectsInactive.Include);
        foreach (PPEActionPanelController panel in currentPanels)
        {
            if (panel == null || !panels.Add(panel))
                continue;

            panel.ChoiceResolvedWithSource += OnChoiceResolved;
        }

        PPEInspectionState[] currentInspections =
            FindObjectsByType<PPEInspectionState>(FindObjectsInactive.Include);
        foreach (PPEInspectionState inspection in currentInspections)
        {
            if (inspection == null || !inspections.Add(inspection))
                continue;

            inspection.InspectionChanged += isInspecting => OnInspectionChanged(inspection, isInspecting);
            inspection.ConditionChanged += condition => OnConditionChanged(inspection, condition);

            XRGrabInteractable grabInteractable = inspection.GrabInteractable;
            if (grabInteractable != null && observedGrabInteractables.Add(grabInteractable))
            {
                grabInteractable.selectEntered.AddListener(
                    args => OnGrabSelectEntered(inspection, args));
                grabInteractable.selectExited.AddListener(
                    args => OnGrabSelectExited(inspection, args));
            }
        }
    }

    void OnInputActionChange(object changedObject, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed ||
            changedObject is not InputAction action ||
            director == null ||
            director.CurrentState != PPEVoiceFlowDirector.FlowState.PpeArea ||
            !TryGetControllerGripHand(action, out InteractorHandedness handedness, out string controlPath))
        {
            return;
        }

        if (TryGetSelectedPpe(handedness, out PPEInspectionState selectedInspection))
        {
            Write(
                "ppe_grab_release_input",
                itemType:GetItemType(selectedInspection),
                note:"grip_pressed_while_selected",
                hand:handedness.ToString(),
                inputControl:controlPath,
                attemptOutcome:"release_requested");
            return;
        }

        if (pendingGrabAttempts.TryGetValue(handedness, out PendingGrabAttempt previous))
            ResolveGrabAttempt(previous, "repeated_before_select", previous.candidateItemType);

        List<PPEInspectionState> hovered = GetHoveredPpe(handedness);
        string hoveredItems = string.Join(",", hovered
            .ConvertAll(GetItemType)
            .FindAll(value => !string.IsNullOrEmpty(value)));
        string candidateItemType = hovered.Count == 1 ? GetItemType(hovered[0]) : null;
        PendingGrabAttempt attempt = new()
        {
            attemptId = Guid.NewGuid().ToString("N"),
            handedness = handedness,
            inputControl = controlPath,
            hoveredPpeItems = hoveredItems,
            hoveredPpeCount = hovered.Count,
            candidateItemType = candidateItemType,
            startedAt = Time.realtimeSinceStartup,
        };
        pendingGrabAttempts[handedness] = attempt;

        Write(
            "ppe_grab_attempted",
            itemType:candidateItemType,
            note:hovered.Count == 0 ? "no_ppe_hover_at_input" : "ppe_hover_present_at_input",
            attemptId:attempt.attemptId,
            hand:handedness.ToString(),
            inputControl:controlPath,
            hoveredPpeCount:hovered.Count,
            hoveredPpeItems:hoveredItems);
    }

    static bool TryGetControllerGripHand(
        InputAction action,
        out InteractorHandedness handedness,
        out string controlPath)
    {
        handedness = InteractorHandedness.None;
        controlPath = action?.activeControl?.path;
        if (action == null ||
            !string.Equals(action.name, "Select", StringComparison.Ordinal) ||
            string.IsNullOrEmpty(controlPath) ||
            controlPath.IndexOf("grip", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        string mapName = action.actionMap?.name;
        if (string.Equals(mapName, "XRI Left Interaction", StringComparison.Ordinal))
            handedness = InteractorHandedness.Left;
        else if (string.Equals(mapName, "XRI Right Interaction", StringComparison.Ordinal))
            handedness = InteractorHandedness.Right;

        return handedness != InteractorHandedness.None;
    }

    List<PPEInspectionState> GetHoveredPpe(InteractorHandedness handedness)
    {
        List<PPEInspectionState> hovered = new();
        foreach (PPEInspectionState inspection in inspections)
        {
            XRGrabInteractable grabInteractable = inspection?.GrabInteractable;
            if (grabInteractable == null || !grabInteractable.isActiveAndEnabled)
                continue;

            foreach (IXRHoverInteractor interactor in grabInteractable.interactorsHovering)
            {
                if (interactor is XRBaseInputInteractor inputInteractor &&
                    inputInteractor.handedness == handedness)
                {
                    hovered.Add(inspection);
                    break;
                }
            }
        }
        return hovered;
    }

    bool TryGetSelectedPpe(
        InteractorHandedness handedness,
        out PPEInspectionState selectedInspection)
    {
        foreach (PPEInspectionState inspection in inspections)
        {
            XRGrabInteractable grabInteractable = inspection?.GrabInteractable;
            if (grabInteractable == null)
                continue;

            foreach (IXRSelectInteractor interactor in grabInteractable.interactorsSelecting)
            {
                if (interactor is XRBaseInputInteractor inputInteractor &&
                    inputInteractor.handedness == handedness)
                {
                    selectedInspection = inspection;
                    return true;
                }
            }
        }

        selectedInspection = null;
        return false;
    }

    void OnGrabSelectEntered(PPEInspectionState inspection, SelectEnterEventArgs args)
    {
        if (args.interactorObject is not XRBaseInputInteractor interactor)
            return;

        string itemType = GetItemType(inspection);
        if (pendingGrabAttempts.TryGetValue(interactor.handedness, out PendingGrabAttempt attempt))
        {
            ResolveGrabAttempt(attempt, "selected", itemType);
            return;
        }

        Write(
            "ppe_grab_selected_without_attempt",
            itemType:itemType,
            note:"selectEntered_without_observed_controller_grip",
            hand:interactor.handedness.ToString(),
            attemptOutcome:"selected_without_attempt");
    }

    void OnGrabSelectExited(PPEInspectionState inspection, SelectExitEventArgs args)
    {
        string handedness = args.interactorObject is XRBaseInputInteractor interactor
            ? interactor.handedness.ToString()
            : null;
        Write(
            "ppe_grab_released",
            itemType:GetItemType(inspection),
            hand:handedness,
            attemptOutcome:"released");
    }

    void ResolveExpiredGrabAttempts()
    {
        if (pendingGrabAttempts.Count == 0)
            return;

        List<PendingGrabAttempt> expired = new();
        foreach (PendingGrabAttempt attempt in pendingGrabAttempts.Values)
        {
            if (Time.realtimeSinceStartup - attempt.startedAt >= GrabAttemptResolutionWindowSeconds)
                expired.Add(attempt);
        }

        foreach (PendingGrabAttempt attempt in expired)
        {
            string outcome = attempt.hoveredPpeCount == 0
                ? "no_ppe_hover"
                : "hover_without_select";
            ResolveGrabAttempt(attempt, outcome, attempt.candidateItemType);
        }
    }

    void ResolveGrabAttempt(PendingGrabAttempt attempt, string outcome, string itemType)
    {
        if (attempt == null ||
            !pendingGrabAttempts.TryGetValue(attempt.handedness, out PendingGrabAttempt current) ||
            !ReferenceEquals(current, attempt))
        {
            return;
        }

        pendingGrabAttempts.Remove(attempt.handedness);
        Write(
            "ppe_grab_attempt_resolved",
            itemType:itemType,
            note:outcome,
            attemptId:attempt.attemptId,
            hand:attempt.handedness.ToString(),
            inputControl:attempt.inputControl,
            hoveredPpeCount:attempt.hoveredPpeCount,
            hoveredPpeItems:attempt.hoveredPpeItems,
            attemptOutcome:outcome,
            attemptElapsedSec:Mathf.Max(0f, Time.realtimeSinceStartup - attempt.startedAt));
    }

    static string GetItemType(PPEInspectionState inspection)
    {
        return inspection?.PresentationBinding?.ItemIdentity?.ItemType.ToString();
    }

    void OnChoiceResolved(
        PPEActionPanelController panel,
        PPEActionChoice choice,
        PPEActionResult result)
    {
        PPEItemIdentity identity = panel?.InspectionState?.PresentationBinding?.ItemIdentity;
        Write(
            "ppe_choice_resolved",
            itemType:identity?.ItemType.ToString(),
            // Some localized ItemDisplayName values are not serialized safely
            // by Unity's JsonUtility. The enum is the stable PPE identity and
            // is already used by the dashboard contract.
            itemName:identity?.ItemType.ToString(),
            condition:panel?.InspectionState?.CurrentCondition.ToString(),
            choice:choice.ToString(),
            result:result.ToString());
    }

    void OnInspectionChanged(PPEInspectionState inspection, bool isInspecting)
    {
        PPEItemIdentity identity = inspection?.PresentationBinding?.ItemIdentity;
        Write(
            isInspecting ? "ppe_inspection_started" : "ppe_inspection_stopped",
            itemType:identity?.ItemType.ToString(),
            itemName:identity != null ? inspection.name : null,
            condition:inspection?.CurrentCondition.ToString());
    }

    void OnConditionChanged(PPEInspectionState inspection, PPEItemCondition condition)
    {
        PPEItemIdentity identity = inspection?.PresentationBinding?.ItemIdentity;
        Write("ppe_condition_changed", itemType:identity?.ItemType.ToString(), condition:condition.ToString());
    }

    void OnUnityLog(string message, string stackTrace, LogType type)
    {
        if (!message.StartsWith("[PPE", StringComparison.Ordinal) ||
            message.StartsWith("[PPE Telemetry]", StringComparison.Ordinal))
            return;

        Write("unity_ppe_log", note:message);
    }

    void OnApplicationQuitting()
    {
        RecordSessionEnded();
    }

    void RecordSessionEnded()
    {
        if (sessionEnding || sessionEnded)
            return;

        sessionEnding = true;
        try
        {
            foreach (PendingGrabAttempt attempt in new List<PendingGrabAttempt>(pendingGrabAttempts.Values))
                ResolveGrabAttempt(attempt, "session_ended_before_resolution", attempt.candidateItemType);
            Write("session_ended", note:"application_quitting");
            sessionEnded = true;
        }
        finally
        {
            sessionEnding = false;
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused == applicationPaused)
            return;

        applicationPaused = paused;
        Write(paused ? "application_paused" : "application_resumed");
    }

    void Write(
        string eventType,
        string itemType = null,
        string itemName = null,
        string condition = null,
        string choice = null,
        string result = null,
        string note = null,
        string flowState = null,
        string mode = null,
        string workPlan = null,
        string modeSessionId = null,
        string requiredPpeCheck = null,
        string missingRequiredPpe = null,
        string audioClip = null,
        float audioLengthSec = 0f,
        float audioElapsedSec = 0f,
        string attemptId = null,
        string hand = null,
        string inputControl = null,
        int hoveredPpeCount = 0,
        string hoveredPpeItems = null,
        string attemptOutcome = null,
        float attemptElapsedSec = 0f,
        string quizTopic = null,
        int quizQuestionIndex = 0,
        int quizQuestionCount = 0,
        int quizSelectedOptionIndex = 0,
        bool quizCorrect = false,
        int quizCorrectCount = 0,
        int ppeWrongCount = 0,
        float modeElapsedSec = 0f)
    {
        if (sessionEnded || string.IsNullOrEmpty(outputPath))
            return;

        int sequence = ++nextSequence;
        Record record = new()
        {
            schemaVersion = CurrentSchemaVersion,
            sourceProject = SourceProject,
            sessionId = sessionId,
            eventId = $"{sessionId}:{sequence:D8}",
            sequence = sequence,
            timestampUtc = DateTime.UtcNow.ToString("O"),
            eventType = eventType,
            appVersion = eventType == "session_started" ? Application.version : null,
            scene = SceneManager.GetActiveScene().path,
            mode = mode ?? director?.ActiveLearningMode.ToString(),
            workPlan = workPlan ?? director?.ActiveWorkPlan.ToString(),
            modeSessionId = SanitizeTelemetryText(
                modeSessionId ?? director?.ActiveModeSessionId),
            flowState = flowState ?? director?.CurrentState.ToString(),
            itemType = SanitizeTelemetryText(itemType),
            itemName = SanitizeTelemetryText(itemName),
            condition = SanitizeTelemetryText(condition),
            choice = SanitizeTelemetryText(choice),
            result = SanitizeTelemetryText(result),
            note = SanitizeTelemetryText(note),
            metaProbeState = FindAnyObjectByType<MetaPlatformIdentityProbe>(FindObjectsInactive.Include)?.State.ToString() ?? "probe_missing",
            metaWelcomeState = MetaPlatformIdentityProbe.CurrentWelcomeState.ToString(),
            metaAppScopedUserId = MetaPlatformIdentityProbe.CurrentAppScopedUserId == 0
                ? null
                : MetaPlatformIdentityProbe.CurrentAppScopedUserId.ToString(),
            requiredPpeCheck = requiredPpeCheck,
            missingRequiredPpe = missingRequiredPpe,
            audioClip = SanitizeTelemetryText(audioClip),
            audioLengthSec = audioLengthSec,
            audioElapsedSec = audioElapsedSec,
            attemptId = attemptId,
            hand = hand,
            inputControl = SanitizeTelemetryText(inputControl),
            hoveredPpeCount = hoveredPpeCount,
            hoveredPpeItems = SanitizeTelemetryText(hoveredPpeItems),
            attemptOutcome = SanitizeTelemetryText(attemptOutcome),
            attemptElapsedSec = attemptElapsedSec,
            quizTopic = SanitizeTelemetryText(quizTopic),
            quizQuestionIndex = quizQuestionIndex,
            quizQuestionCount = quizQuestionCount,
            quizSelectedOptionIndex = quizSelectedOptionIndex,
            quizCorrect = quizCorrect,
            quizCorrectCount = quizCorrectCount,
            ppeWrongCount = ppeWrongCount,
            modeElapsedSec = modeElapsedSec,
        };

        File.AppendAllText(outputPath, JsonUtility.ToJson(record) + Environment.NewLine);
        try
        {
            TelemetryRecordAppended?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        Debug.Log($"[PPE Telemetry] {eventType} -> {outputPath}");
    }

    static string SanitizeTelemetryText(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character == '\r' || character == '\n' || character == '\t')
            {
                builder.Append(' ');
                continue;
            }

            if (char.IsControl(character) || char.IsSurrogate(character))
                continue;

            builder.Append(character);
        }

        return builder.ToString().Trim();
    }

    void TrackVoicePlayback()
    {
        AudioClip currentClip = AudioManager.Instance?.CurrentVoiceClip;
        bool isPlaying = AudioManager.Instance != null && AudioManager.Instance.IsVoicePlaying;

        if (isPlaying && currentClip != null && activeVoiceClip != currentClip)
        {
            EndVoicePlayback("replaced");
            activeVoiceClip = currentClip;
            activeVoiceStartedAt = Time.realtimeSinceStartup;
            Write("voice_playback_started", audioClip:currentClip.name, audioLengthSec:currentClip.length);
            return;
        }

        if (!isPlaying && activeVoiceClip != null)
        {
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - activeVoiceStartedAt);
            string reason = currentClip == null
                ? "stopped"
                : elapsed >= activeVoiceClip.length - 0.05f ? "completed" : "stopped";
            EndVoicePlayback(reason);
        }
    }

    void EndVoicePlayback(string endReason)
    {
        if (activeVoiceClip == null)
            return;

        float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - activeVoiceStartedAt);
        Write(
            "voice_playback_ended",
            note:endReason,
            audioClip:activeVoiceClip.name,
            audioLengthSec:activeVoiceClip.length,
            audioElapsedSec:elapsed);
        activeVoiceClip = null;
        activeVoiceStartedAt = 0f;
    }

    static string BuildRequiredPpeStatus(PPEVoiceFlowDirector currentDirector)
    {
        PPEItemType[] required = currentDirector?.ActiveRequiredPpeItemTypes;
        if (required == null || required.Length == 0)
            return "not_applicable|";

        List<string> missing = new();
        foreach (PPEItemType itemType in required)
        {
            if (!currentDirector.IsPpeItemWorn(itemType))
                missing.Add(itemType.ToString());
        }

        return $"{(missing.Count == 0 ? "complete" : "incomplete")}|{string.Join(",", missing)}";
    }
}
