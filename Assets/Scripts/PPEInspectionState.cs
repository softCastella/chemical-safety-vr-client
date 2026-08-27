using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEItemPresentationBinding))]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class PPEInspectionState : MonoBehaviour, IXRSelectFilter
{
    [SerializeField]
    PPEItemPresentationBinding presentationBinding;

    [SerializeField]
    XRGrabInteractable grabInteractable;

    [SerializeField]
    [Tooltip("PPE 관찰은 기본적으로 오른손 Grip/Select만 시작합니다. 장갑은 반대 손으로 잡아 끼울 손 모델에 대기 위해 양손을 허용합니다.")]
    bool rightHandGrabOnly = true;

    [SerializeField]
    [Tooltip("관찰 및 상태 전환을 Console에 기록합니다. 수동 검증용입니다.")]
    bool logStateChanges;

    readonly HashSet<IXRSelectInteractor> selectingInteractors = new();

    public PPEItemCondition CurrentCondition { get; private set; }
    public bool IsBeingInspected { get; private set; }
    public PPEItemPresentationBinding PresentationBinding => presentationBinding;
    public XRGrabInteractable GrabInteractable => grabInteractable;
    public bool canProcess => isActiveAndEnabled && (rightHandGrabOnly || IsGloveItem());

    public event Action<PPEItemCondition> ConditionChanged;
    public event Action<bool> InspectionChanged;

    void Awake()
    {
        if (presentationBinding != null)
            CurrentCondition = presentationBinding.InitialCondition;
    }

    void OnEnable()
    {
        if (presentationBinding == null || grabInteractable == null)
        {
            Debug.LogError(
                "PPEInspectionState requires serialized presentation and Grab references.",
                this);
            enabled = false;
            return;
        }

        CurrentCondition = presentationBinding.InitialCondition;
        if (rightHandGrabOnly || IsGloveItem())
            grabInteractable.selectFilters.Add(this);

        // Component enable order is not deterministic. A condition visual can
        // subscribe before this state restores its Inspector-authored initial
        // value, so notify even when the backing value was already assigned in
        // Awake. Later subscribers still apply CurrentCondition in OnEnable.
        ConditionChanged?.Invoke(CurrentCondition);
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            if (rightHandGrabOnly || IsGloveItem())
                grabInteractable.selectFilters.Remove(this);

            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        selectingInteractors.Clear();
        SetInspectionState(false);
    }

    public void SetCondition(PPEItemCondition condition)
    {
        if (CurrentCondition == condition)
            return;

        CurrentCondition = condition;
        ConditionChanged?.Invoke(condition);

        if (logStateChanges)
            Debug.Log($"[PPE Condition] {name}: {condition}", this);
    }

    public void ResetToInitialCondition()
    {
        if (presentationBinding != null)
            SetCondition(presentationBinding.InitialCondition);
    }

#if UNITY_EDITOR
    [ContextMenu("Set Clean (Debug)")]
    void DebugSetClean()
    {
        SetCondition(PPEItemCondition.Clean);
    }

    [ContextMenu("Set Contaminated (Debug)")]
    void DebugSetContaminated()
    {
        SetCondition(PPEItemCondition.Contaminated);
    }
#endif

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject != null)
            selectingInteractors.Add(args.interactorObject);

        SetInspectionState(selectingInteractors.Count > 0);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject != null)
            selectingInteractors.Remove(args.interactorObject);

        SetInspectionState(selectingInteractors.Count > 0);
    }

    void SetInspectionState(bool isBeingInspected)
    {
        if (IsBeingInspected == isBeingInspected)
            return;

        IsBeingInspected = isBeingInspected;
        InspectionChanged?.Invoke(isBeingInspected);

        if (logStateChanges)
        {
            string state = isBeingInspected ? "Started" : "Ended";
            Debug.Log(
                $"[PPE Inspection] {state}: {name}, condition={CurrentCondition}",
                this);
        }
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor == null)
            return false;

        if (IsGloveItem())
        {
            return interactor.handedness == InteractorHandedness.Left ||
                interactor.handedness == InteractorHandedness.Right;
        }

        return !rightHandGrabOnly || interactor.handedness == InteractorHandedness.Right;
    }

    public bool TryGetSelectingHandedness(out InteractorHandedness handedness)
    {
        foreach (IXRSelectInteractor interactor in selectingInteractors)
        {
            if (interactor == null)
                continue;

            handedness = interactor.handedness;
            if (handedness == InteractorHandedness.Left ||
                handedness == InteractorHandedness.Right)
            {
                return true;
            }
        }

        handedness = InteractorHandedness.None;
        return false;
    }

    public bool TryGetSelectingInteractorTransform(out Transform interactorTransform)
    {
        foreach (IXRSelectInteractor interactor in selectingInteractors)
        {
            if (interactor == null || interactor.transform == null)
                continue;

            interactorTransform = interactor.transform;
            return true;
        }

        interactorTransform = null;
        return false;
    }

    bool IsGloveItem()
    {
        PPEItemType? itemType = presentationBinding?.ItemIdentity?.ItemType;
        return itemType == PPEItemType.RubberGloveLeft ||
            itemType == PPEItemType.RubberGloveRight ||
            itemType == PPEItemType.NitrileInnerGloveLeft ||
            itemType == PPEItemType.NitrileInnerGloveRight;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEItemPresentationBinding configuredPresentationBinding,
        XRGrabInteractable configuredGrabInteractable)
    {
        presentationBinding = configuredPresentationBinding;
        grabInteractable = configuredGrabInteractable;
    }
#endif
}
