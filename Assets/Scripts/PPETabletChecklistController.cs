using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class PPETabletChecklistController : MonoBehaviour
{
    [SerializeField]
    XRGrabInteractable grabInteractable;

    [SerializeField]
    HandwrittenSignatureSequence signatureSequence;

    [SerializeField]
    GameObject confinedDocumentRoot;

    [SerializeField]
    GameObject leakDocumentRoot;

    [SerializeField]
    HandwrittenSignatureSequence confinedSignatureSequence;

    [SerializeField]
    HandwrittenSignatureSequence leakSignatureSequence;

    [SerializeField]
    bool requireHoldingHandTrigger = true;

    [SerializeField]
    bool resetSequenceOnEnable = true;

    [SerializeField, Min(0)]
    [Tooltip("Checklist steps revealed immediately when the held Tablet receives its check Trigger.")]
    int instantChecklistStepCount = 7;

    [SerializeField]
    bool logProgress;

    InputAction leftTriggerAction;
    InputAction rightTriggerAction;
    bool runtimeInitialized;
    readonly System.Collections.Generic.List<XRGrabInteractable> blockedSuitGrabs = new();

    public XRGrabInteractable GrabInteractable => grabInteractable;
    public HandwrittenSignatureSequence SignatureSequence => signatureSequence;
    public bool RequireHoldingHandTrigger => requireHoldingHandTrigger;
    public int InstantChecklistStepCount => instantChecklistStepCount;
    public bool IsDocumentCompleted { get; private set; }
    public event Action DocumentCompleted;

    public void ResetForNewSession()
    {
        RestoreSuitGrabs();
        confinedSignatureSequence?.ResetSignatures();
        leakSignatureSequence?.ResetSignatures();
        if (signatureSequence != null &&
            signatureSequence != confinedSignatureSequence &&
            signatureSequence != leakSignatureSequence)
        {
            signatureSequence.ResetSignatures();
        }

        IsDocumentCompleted = false;
        ApplyWorkPlan(ScenarioDetailModal.PpeWorkPlan.None);
    }

    public void ApplyWorkPlan(ScenarioDetailModal.PpeWorkPlan workPlan)
    {
        bool showLeak = workPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse;
        HandwrittenSignatureSequence nextSequence = showLeak
            ? leakSignatureSequence
            : confinedSignatureSequence != null ? confinedSignatureSequence : signatureSequence;

        if (showLeak && !HasUsableSequence(nextSequence))
        {
            Debug.LogError(
                "PPETabletChecklistController requires an authored leak signature sequence before showing the leak work plan.",
                this);
            return;
        }

        UnsubscribeFromActiveSequence();

        if (confinedDocumentRoot != null)
            confinedDocumentRoot.SetActive(!showLeak);
        if (leakDocumentRoot != null)
            leakDocumentRoot.SetActive(showLeak);

        if (nextSequence != null)
            signatureSequence = nextSequence;

        signatureSequence?.ResetSignatures();
        IsDocumentCompleted = false;
        SubscribeToActiveSequence();
    }

    void OnEnable()
    {
        if (!HasCompleteReferences())
        {
            Debug.LogError(
                "PPETabletChecklistController requires authored Grab and complete signature-sequence references.",
                this);
            enabled = false;
            return;
        }

        CreateAndEnableInputActions();
        grabInteractable.activated.AddListener(OnActivated);
        grabInteractable.selectEntered.AddListener(OnTabletSelected);
        grabInteractable.selectExited.AddListener(OnTabletReleased);

        if (resetSequenceOnEnable)
            signatureSequence.ResetSignatures();

        IsDocumentCompleted = false;
        runtimeInitialized = true;
        SubscribeToActiveSequence();
    }

    void OnDisable()
    {
        if (!runtimeInitialized)
            return;

        DisableAndDisposeInputActions();
        grabInteractable.activated.RemoveListener(OnActivated);
        grabInteractable.selectEntered.RemoveListener(OnTabletSelected);
        grabInteractable.selectExited.RemoveListener(OnTabletReleased);
        UnsubscribeFromActiveSequence();
        RestoreSuitGrabs();
        runtimeInitialized = false;
    }

    void CreateAndEnableInputActions()
    {
        leftTriggerAction = new InputAction(
            "PPE Tablet Check Left Trigger",
            InputActionType.Button,
            "<XRController>{LeftHand}/trigger");
        rightTriggerAction = new InputAction(
            "PPE Tablet Check Right Trigger",
            InputActionType.Button,
            "<XRController>{RightHand}/trigger");

        leftTriggerAction.AddBinding("<OculusTouchController>{LeftHand}/trigger");
        rightTriggerAction.AddBinding("<OculusTouchController>{RightHand}/trigger");

        leftTriggerAction.performed += OnLeftTrigger;
        rightTriggerAction.performed += OnRightTrigger;
        leftTriggerAction.Enable();
        rightTriggerAction.Enable();
    }

    void DisableAndDisposeInputActions()
    {
        if (leftTriggerAction != null)
        {
            leftTriggerAction.performed -= OnLeftTrigger;
            leftTriggerAction.Dispose();
            leftTriggerAction = null;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.performed -= OnRightTrigger;
            rightTriggerAction.Dispose();
            rightTriggerAction = null;
        }
    }

    void OnLeftTrigger(InputAction.CallbackContext context)
    {
        TryStartSequence(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left);
    }

    void OnRightTrigger(InputAction.CallbackContext context)
    {
        TryStartSequence(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right);
    }

    void OnActivated(ActivateEventArgs args)
    {
        if (IsDocumentCompleted)
            return;

        // Activation is emitted by the selected grab interactable; do not add a
        // second handedness/selection gate here (hand tracking may report none).
        if (!signatureSequence.IsPlaying)
            StartChecklistSequence();
    }

    void OnTabletSelected(SelectEnterEventArgs args)
    {
        if (IsDocumentCompleted)
            return;

        BlockHazmatSuitGrabs();
        // Do not start the checklist/signature sequence on grab. Playback must wait
        // for Trigger / Activate so SFX stays locked to the reveal animation.
    }

    void BlockHazmatSuitGrabs()
    {
        if (blockedSuitGrabs.Count > 0)
            return;

        blockedSuitGrabs.Clear();
        foreach (XRGrabInteractable candidate in FindObjectsByType<XRGrabInteractable>())
        {
            if (candidate == grabInteractable)
                continue;

            Transform scope = candidate.transform;
            bool isHazmatInspection = false;
            for (; scope != null; scope = scope.parent)
            {
                PPEItemIdentity identity = scope.GetComponent<PPEItemIdentity>();
                if (identity != null && identity.ItemType == PPEItemType.HazmatSuit)
                {
                    isHazmatInspection = true;
                    break;
                }
            }

            if (isHazmatInspection && candidate.enabled)
            {
                candidate.enabled = false;
                blockedSuitGrabs.Add(candidate);
            }
        }
    }

    void OnTabletReleased(SelectExitEventArgs args) => RestoreSuitGrabs();

    void OnSequenceCompleted(int startIndex, int count)
    {
        if (startIndex == 0 && count == signatureSequence.StepCount)
            MarkDocumentCompleted();
    }

    /// <summary>Game View mouse test path: one tablet-center click completes the checklist and signature.</summary>
    public void CompleteForTesting()
    {
        if (IsDocumentCompleted)
            return;

        if (!signatureSequence.IsPlaying)
            StartChecklistSequence();

        MarkDocumentCompleted();
    }

    void MarkDocumentCompleted()
    {
        if (IsDocumentCompleted)
            return;

        IsDocumentCompleted = true;
        DocumentCompleted?.Invoke();
    }

    void RestoreSuitGrabs()
    {
        foreach (XRGrabInteractable candidate in blockedSuitGrabs)
            if (candidate != null)
                candidate.enabled = true;
        blockedSuitGrabs.Clear();
    }

    void TryStartSequence(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness triggerHand)
    {
        if (IsDocumentCompleted || signatureSequence.IsPlaying)
            return;

        // Hand-tracking interactors can report handedness as None even while the
        // tablet is visibly held.  The grab itself is the authoritative gate.
        if (requireHoldingHandTrigger && !IsSelected())
            return;

        if (!StartChecklistSequence())
            return;

        if (logProgress)
        {
            Debug.Log(
                "[PPE Tablet] Trigger accepted: the complete checklist and signature sequence started.",
                this);
        }
    }

    bool IsSelected()
    {
        return grabInteractable.isSelected && grabInteractable.interactorsSelecting.Count > 0;
    }

    bool StartChecklistSequence()
    {
        return signatureSequence.PlayRangeWithInstantPrefix(
            0,
            signatureSequence.StepCount,
            Mathf.Min(instantChecklistStepCount, signatureSequence.StepCount),
            true);
    }

    void SubscribeToActiveSequence()
    {
        if (!runtimeInitialized || signatureSequence == null)
            return;

        signatureSequence.PlaybackCompleted -= OnSequenceCompleted;
        signatureSequence.PlaybackCompleted += OnSequenceCompleted;
    }

    void UnsubscribeFromActiveSequence()
    {
        if (signatureSequence == null)
            return;

        signatureSequence.PlaybackCompleted -= OnSequenceCompleted;
    }

    bool HasCompleteReferences()
    {
        return grabInteractable != null && HasUsableSequence(signatureSequence);
    }

    static bool HasUsableSequence(HandwrittenSignatureSequence sequence)
    {
        return sequence != null &&
            sequence.HasCompleteTargetReferences &&
            sequence.StepCount > 0;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        XRGrabInteractable configuredGrabInteractable,
        HandwrittenSignatureSequence configuredSequence)
    {
        grabInteractable = configuredGrabInteractable;
        signatureSequence = configuredSequence;
        requireHoldingHandTrigger = true;
        resetSequenceOnEnable = true;
        instantChecklistStepCount = 0;
        if (configuredSequence != null)
        {
            Renderer[] authoredRenderers =
                configuredSequence.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < authoredRenderers.Length; index++)
            {
                if (authoredRenderers[index].name.StartsWith(
                    "Checklist_Check_",
                    StringComparison.Ordinal))
                {
                    instantChecklistStepCount++;
                }
            }
        }
    }
#endif
}
