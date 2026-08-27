using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class PPEMarkerToggleGrab : MonoBehaviour
{
    const string MarkerObjectName = "XR Item Marker_small";

    [SerializeField]
    [Tooltip("실제 Grab 판정에 사용할 아이템 마커입니다. 비워두면 자식에서 XR Item Marker_small을 찾습니다.")]
    Transform interactionMarker;

    [SerializeField]
    [Tooltip("마커를 가리키는 동안 그립을 한 번 눌러 잡고, 다시 눌러 놓는 Toggle 방식으로 전환합니다.")]
    bool useToggleGrip = true;

    [SerializeField]
    [Tooltip("슈트를 놓으면 Play Mode 시작 당시 씬에 작성된 부모와 Local Transform으로 즉시 돌려놓습니다.")]
    bool returnToAuthoredPoseOnRelease = true;

    [SerializeField]
    [Tooltip("Toggle Grab 선택이 유지되는 동안 컨트롤러 손 모델의 Grip 자세도 유지합니다.")]
    bool holdHandGripWhileSelected = true;

    readonly Dictionary<XRBaseInputInteractor, XRBaseInputInteractor.InputTriggerType>
        originalTriggerTypes = new();
    readonly HashSet<XRBaseInputInteractor> selectingInteractors = new();
    readonly Dictionary<XRBaseInputInteractor, List<HandGripAnimator>> heldHandAnimators = new();

    XRGrabInteractable grabInteractable;
    Rigidbody targetRigidbody;
    Transform authoredParent;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    Vector3 authoredLocalScale;
    bool pendingAuthoredPoseRestore;
    Coroutine deferredAuthoredPoseRestore;

    public bool UseToggleGrip => useToggleGrip;
    public bool ReturnToAuthoredPoseOnRelease => returnToAuthoredPoseOnRelease;
    public bool HoldHandGripWhileSelected => holdHandGripWhileSelected;
    public Transform InteractionMarker => interactionMarker;
    public event Action Released;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // The full-suit visual contains disabled copies of the original PPE
        // components. They are render-only children and must not validate or
        // configure grab colliders during scene initialization.
        if (!enabled)
            return;

        EnsureMarkerOnlyCollider();
        targetRigidbody = GetComponent<Rigidbody>();
        authoredParent = transform.parent;
        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        authoredLocalScale = transform.localScale;
    }

    void EnsureMarkerOnlyCollider()
    {
        if (interactionMarker == null)
        {
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == MarkerObjectName)
                {
                    interactionMarker = candidate;
                    break;
                }
            }
        }

        Collider markerCollider = interactionMarker != null
            ? interactionMarker.GetComponent<Collider>()
            : null;
        if (markerCollider == null)
        {
            Debug.LogError(
                $"{nameof(PPEMarkerToggleGrab)} requires a '{MarkerObjectName}' child with a Collider. " +
                "The PPE mesh will not be used as a fallback grab target.",
                this);
            grabInteractable.enabled = false;
            return;
        }

        grabInteractable.colliders.Clear();
        grabInteractable.colliders.Add(markerCollider);
    }

    void OnEnable()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);

        if (pendingAuthoredPoseRestore)
            RequestAuthoredPoseRestore();
    }

    void OnDisable()
    {
        if (deferredAuthoredPoseRestore != null)
        {
            StopCoroutine(deferredAuthoredPoseRestore);
            deferredAuthoredPoseRestore = null;
        }

        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        // XRGrabInteractable.OnDisable can fire SelectExit on this same stack.
        // Never SetParent here — mark restore for the next safe enable/LateUpdate.
        if (returnToAuthoredPoseOnRelease && NeedsAuthoredPoseRestore())
            pendingAuthoredPoseRestore = true;

        ReleaseAllHeldHandGrips();
        selectingInteractors.Clear();
        RestoreAllInteractors();
    }

    void LateUpdate()
    {
        // Some direct-hand runtimes do not issue SelectExit reliably when a
        // kinematic object is released.  Never leave a marker item at the
        // hand's last attach rotation; its Inspector transform is the source
        // of truth whenever it is not selected.
        if (returnToAuthoredPoseOnRelease &&
            grabInteractable != null &&
            !grabInteractable.isSelected &&
            (pendingAuthoredPoseRestore || NeedsAuthoredPoseRestore()))
        {
            ReturnToAuthoredPose();
        }
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        ApplyToggle(args.interactorObject as XRBaseInputInteractor);
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInputInteractor;
        if (!IsSelecting(interactor))
            RestoreInteractor(interactor);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInputInteractor;
        if (interactor == null)
            return;

        pendingAuthoredPoseRestore = false;
        if (deferredAuthoredPoseRestore != null)
        {
            StopCoroutine(deferredAuthoredPoseRestore);
            deferredAuthoredPoseRestore = null;
        }

        selectingInteractors.Add(interactor);
        ApplyToggle(interactor);
        HoldHandGrip(interactor);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInputInteractor;
        if (interactor != null)
        {
            selectingInteractors.Remove(interactor);
            ReleaseHandGrip(interactor);
        }

        // SelectExit often runs inside XRBaseInteractable.OnDisable. Calling
        // Transform.SetParent on that stack throws:
        // "GameObject is already being activated or deactivated."
        RequestAuthoredPoseRestore();
        Released?.Invoke();

        if (!IsHovering(interactor))
            RestoreInteractor(interactor);
    }

    void RequestAuthoredPoseRestore()
    {
        if (!returnToAuthoredPoseOnRelease)
            return;

        // Keep the request alive through XRI's end-of-frame selection update.
        // LateUpdate performs the restore as soon as selection has actually ended.
        pendingAuthoredPoseRestore = true;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (deferredAuthoredPoseRestore != null)
            StopCoroutine(deferredAuthoredPoseRestore);

        deferredAuthoredPoseRestore = StartCoroutine(RestoreAuthoredPoseAfterSelectExit());
    }

    IEnumerator RestoreAuthoredPoseAfterSelectExit()
    {
        // Leave the XR disable / select-exit call stack before reparenting.
        yield return null;

        deferredAuthoredPoseRestore = null;
        if (!isActiveAndEnabled || !returnToAuthoredPoseOnRelease)
        {
            pendingAuthoredPoseRestore = true;
            yield break;
        }

        if (grabInteractable != null && grabInteractable.isSelected)
            yield break;

        ReturnToAuthoredPose();
    }

    bool NeedsAuthoredPoseRestore()
    {
        return transform.parent != authoredParent ||
            transform.localPosition != authoredLocalPosition ||
            transform.localRotation != authoredLocalRotation ||
            transform.localScale != authoredLocalScale;
    }

    void ReturnToAuthoredPose()
    {
        if (!returnToAuthoredPoseOnRelease)
            return;

        if (!gameObject.activeInHierarchy)
        {
            pendingAuthoredPoseRestore = true;
            return;
        }

        if (targetRigidbody != null && !targetRigidbody.isKinematic)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
        }

        if (transform.parent != authoredParent)
            transform.SetParent(authoredParent, false);

        transform.SetLocalPositionAndRotation(authoredLocalPosition, authoredLocalRotation);
        transform.localScale = authoredLocalScale;
        pendingAuthoredPoseRestore = false;

        if (targetRigidbody != null && !targetRigidbody.isKinematic)
            targetRigidbody.Sleep();
    }

    void ApplyToggle(XRBaseInputInteractor interactor)
    {
        if (!useToggleGrip || interactor == null)
            return;

        if (!originalTriggerTypes.ContainsKey(interactor))
            originalTriggerTypes.Add(interactor, interactor.selectActionTrigger);

        interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;
    }

    bool IsHovering(XRBaseInputInteractor interactor)
    {
        return interactor != null && grabInteractable.interactorsHovering.Contains(interactor);
    }

    bool IsSelecting(XRBaseInputInteractor interactor)
    {
        return interactor != null && selectingInteractors.Contains(interactor);
    }

    void HoldHandGrip(XRBaseInputInteractor interactor)
    {
        if (!holdHandGripWhileSelected || interactor == null)
            return;

        ReleaseHandGrip(interactor);

        var animators = FindActiveHandAnimators(interactor);
        if (animators.Count == 0)
            return;

        foreach (var animator in animators)
            animator.SetGripActive(this, true);

        heldHandAnimators.Add(interactor, animators);
    }

    static List<HandGripAnimator> FindActiveHandAnimators(XRBaseInputInteractor interactor)
    {
        var matches = new List<HandGripAnimator>();
        for (Transform scope = interactor.transform; scope != null; scope = scope.parent)
        {
            foreach (var animator in scope.GetComponentsInChildren<HandGripAnimator>(true))
            {
                if (animator.isActiveAndEnabled && !matches.Contains(animator))
                    matches.Add(animator);
            }

            if (matches.Count > 0)
                break;
        }

        return matches;
    }

    void ReleaseHandGrip(XRBaseInputInteractor interactor)
    {
        if (interactor == null || !heldHandAnimators.TryGetValue(interactor, out var animators))
            return;

        foreach (var animator in animators)
        {
            if (animator != null)
                animator.SetGripActive(this, false);
        }

        heldHandAnimators.Remove(interactor);
    }

    void ReleaseAllHeldHandGrips()
    {
        foreach (var pair in heldHandAnimators)
        {
            foreach (var animator in pair.Value)
            {
                if (animator != null)
                    animator.SetGripActive(this, false);
            }
        }

        heldHandAnimators.Clear();
    }

    void RestoreInteractor(XRBaseInputInteractor interactor)
    {
        if (interactor == null || !originalTriggerTypes.TryGetValue(interactor, out var originalTriggerType))
            return;

        interactor.selectActionTrigger = originalTriggerType;
        originalTriggerTypes.Remove(interactor);
    }

    void RestoreAllInteractors()
    {
        foreach (var pair in originalTriggerTypes)
        {
            if (pair.Key != null)
                pair.Key.selectActionTrigger = pair.Value;
        }

        originalTriggerTypes.Clear();
    }
}
