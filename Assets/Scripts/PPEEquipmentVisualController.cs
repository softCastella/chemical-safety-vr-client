using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PPEEquipmentVisualSlot
{
    [SerializeField]
    PPEItemPresentationBinding itemBinding;

    [SerializeField]
    GameObject equippedChild;

    public PPEItemPresentationBinding ItemBinding => itemBinding;
    public GameObject EquippedChild => equippedChild;
}

[Serializable]
public sealed class PPEHandModelSwap
{
    [SerializeField]
    PPEItemType itemType;

    [SerializeField]
    GameObject bareHand;

    [SerializeField]
    GameObject bareSuitHand;

    [SerializeField]
    GameObject gloveHand;

    [SerializeField]
    GameObject tapedHand;

    [SerializeField]
    PPEItemType tapeRequiredItemType;

    [SerializeField]
    GameObject innerGloveHand;

    [SerializeField]
    GameObject bareInnerGloveHand;

    public PPEItemType ItemType => itemType;
    public GameObject BareHand => bareHand;
    public GameObject BareSuitHand => bareSuitHand;
    public GameObject GloveHand => gloveHand;
    public GameObject TapedHand => tapedHand;
    public PPEItemType TapeRequiredItemType => tapeRequiredItemType;
    public GameObject InnerGloveHand => innerGloveHand;
    public GameObject BareInnerGloveHand => bareInnerGloveHand;
}

[Serializable]
public sealed class PPEEquipmentTapeVisual
{
    [SerializeField]
    PPEItemType requiredItemType;

    [SerializeField]
    GameObject visual;

    public PPEItemType RequiredItemType => requiredItemType;
    public GameObject Visual => visual;
}

[DisallowMultipleComponent]
public sealed class PPEEquipmentVisualController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("풀장착 방호복 모델의 루트입니다. 슬롯의 자식 오브젝트는 이 루트 아래에 배치합니다.")]
    Transform equipmentRoot;

    [SerializeField]
    [Tooltip("레거시 Front→Approach 경로용. 자식 흡착 연출에서는 사용하지 않습니다. 방호복은 PPEHazmatEquipController가 담당합니다.")]
    Transform frontStartAnchor;

    [SerializeField]
    [Tooltip("레거시 Front→Approach 경로용. 자식 흡착 연출에서는 사용하지 않습니다.")]
    Transform approachAnchor;

    [SerializeField]
    [Min(0f)]
    [Tooltip("자식 PPE가 최종 Pose 앞에서 회전·흡착하는 시간입니다. 방호복 연출 시간과 별개입니다.")]
    float animationDuration = 0.85f;

    [SerializeField]
    bool useUnscaledTime = true;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    [Tooltip("레거시 Front→Approach 분리 시점. 자식 흡착 연출에서는 사용하지 않습니다.")]
    float approachPhaseEnd = 0.65f;

    [SerializeField]
    AnimationCurve motionCurve;

    [SerializeField]
    [Min(0f)]
    [Tooltip("최종 착용 Pose에서 몸 앞 방향으로 얼마나 앞에서 시작할지(미터).")]
    float attachStartForwardDistance = 0.28f;

    [SerializeField]
    [Tooltip("최종 착용 회전에 더하는 시작 회전(오일러). 앞면에서 돌아 붙는 연출용입니다.")]
    Vector3 attachStartLocalEulerOffset = new Vector3(0f, 180f, 0f);

    [SerializeField]
    [Tooltip("잡아서 사용 처리할 PPE와 풀장착 모델 안에서 켤 자식 오브젝트를 연결합니다.")]
    PPEEquipmentVisualSlot[] slots;

    PPEItemType[] requiredWearItemTypes;

    [SerializeField]
    bool hideUnusedChildrenOnEnable = true;

    [SerializeField]
    [Tooltip("켜면 자식을 최종 Pose 바로 앞에서 회전·흡착합니다. 끄면 방호복 안 최종 Pose에 바로 표시합니다. 방호복 Front→Body는 PPEHazmatEquipController가 유지합니다.")]
    bool animateChildrenOnUse;

    [SerializeField]
    [Range(0, 31)]
    [Tooltip("안전모·송기마스크·안면보호대·화학보안경처럼 1인칭 시야를 가릴 수 있는 자식만 Mirror Only로 올립니다. 풀장착 몸통은 Main Camera에 남겨 내려다볼 수 있게 합니다.")]
    int mirrorOnlyLayer = 30;

    [SerializeField]
    [Tooltip("켜면 예전처럼 equipmentRoot 전체를 Mirror Only로 올립니다. 끄면 몸통은 1인칭에 보이고, 안전모/송기마스크/안면보호대/화학보안경 자식만 Mirror Only입니다.")]
    bool applyMirrorOnlyLayerToEntireEquipmentRoot;

    [SerializeField]
    PPEHandModelSwap[] handModelSwaps;

    [SerializeField]
    PPEEquipmentTapeVisual[] tapeVisuals;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Hand model opaque luminance crossfade duration. Uses Hand Form Unlit _Fade only.")]
    float handSwapFadeDuration = 0.4f;

    readonly Dictionary<PPEActionPanelController, PPEEquipmentVisualSlot> subscribedPanels = new();
    readonly Dictionary<PPEActionPanelController, Action<PPEActionChoice, PPEActionResult>> panelHandlers = new();
    readonly Dictionary<PPEEquipmentVisualSlot, AuthoredPose> authoredPoses = new();
    readonly HashSet<PPEEquipmentVisualSlot> animatingSlots = new();
    readonly HashSet<PPEEquipmentVisualSlot> usedSlots = new();
    readonly HashSet<PPEItemType> tapedItemTypes = new();
    readonly List<Coroutine> pendingActivations = new();
    readonly Dictionary<GameObject, bool> handModelActiveStates = new();
    bool runtimeInitialized;
    bool handModelsTemporarilyHidden;
    PPEHazmatEquipController subscribedHazmat;

    public float HandSwapFadeDuration => handSwapFadeDuration;
    public Transform EquipmentRoot => equipmentRoot;
    public Transform FrontStartAnchor => frontStartAnchor;
    public Transform ApproachAnchor => approachAnchor;
    public float AnimationDuration => animationDuration;
    public bool UseUnscaledTime => useUnscaledTime;
    public float ApproachPhaseEnd => approachPhaseEnd;
    public AnimationCurve MotionCurve => motionCurve;
    public float AttachStartForwardDistance => attachStartForwardDistance;
    public Vector3 AttachStartLocalEulerOffset => attachStartLocalEulerOffset;
    public PPEEquipmentVisualSlot[] Slots => slots;
    public bool HideUnusedChildrenOnEnable => hideUnusedChildrenOnEnable;
    public bool AnimateChildrenOnUse => animateChildrenOnUse;
    public int MirrorOnlyLayer => mirrorOnlyLayer;
    public bool ApplyMirrorOnlyLayerToEntireEquipmentRoot => applyMirrorOnlyLayerToEntireEquipmentRoot;

    /// <summary>
    /// Temporarily hides every scene-authored hand-model variant while preserving
    /// the active variant. Restoring therefore returns the wearer to the same
    /// bare, suit, glove, or taped-hand state rather than assigning a new one.
    /// </summary>
    public void SetHandModelsVisible(bool visible)
    {
        if (!visible)
        {
            if (!handModelsTemporarilyHidden)
            {
                handModelActiveStates.Clear();
                foreach (PPEHandModelSwap swap in handModelSwaps ?? Array.Empty<PPEHandModelSwap>())
                {
                    if (swap == null)
                        continue;

                    CaptureHandModelActiveState(swap.BareHand);
                    CaptureHandModelActiveState(swap.BareSuitHand);
                    CaptureHandModelActiveState(swap.BareInnerGloveHand);
                    CaptureHandModelActiveState(swap.InnerGloveHand);
                    CaptureHandModelActiveState(swap.GloveHand);
                    CaptureHandModelActiveState(swap.TapedHand);
                }

                handModelsTemporarilyHidden = true;
            }

            // Another component can re-enable an authored hand model after the
            // first hide request. Preserve the original snapshot, but enforce
            // the requested hidden state every time this method is called.
            foreach (GameObject model in handModelActiveStates.Keys)
            {
                if (model != null)
                    model.SetActive(false);
            }

            return;
        }

        if (!handModelsTemporarilyHidden)
            return;

        foreach (KeyValuePair<GameObject, bool> state in handModelActiveStates)
        {
            if (state.Key != null)
                state.Key.SetActive(state.Value);
        }

        handModelActiveStates.Clear();
        handModelsTemporarilyHidden = false;
    }
    public bool AreAllRequiredSlotsUsed
    {
        get
        {
            if (slots == null || slots.Length == 0)
                return false;

            bool hasRequiredSlot = false;
            foreach (PPEEquipmentVisualSlot slot in slots)
            {
                if (!SlotCountsTowardCompletion(slot))
                    continue;

                hasRequiredSlot = true;
                if (!usedSlots.Contains(slot))
                    return false;
            }

            return hasRequiredSlot || requiredWearItemTypes == null || requiredWearItemTypes.Length == 0;
        }
    }

    public void SetRequiredWearItemTypes(PPEItemType[] itemTypes)
    {
        requiredWearItemTypes = itemTypes;
    }

    /// <summary>
    /// Returns whether the authored visual slot for this PPE type has completed
    /// its existing Use-approved equipment path. This only exposes state; it
    /// does not change the slot or its visual presentation.
    /// </summary>
    public bool IsItemUsed(PPEItemType itemType)
    {
        foreach (PPEEquipmentVisualSlot slot in slots ?? Array.Empty<PPEEquipmentVisualSlot>())
        {
            if (slot?.ItemBinding?.ItemIdentity == null ||
                slot.ItemBinding.ItemIdentity.ItemType != itemType)
            {
                continue;
            }

            if (usedSlots.Contains(slot))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Reports whether the supplied panel's already-approved Use action is the
    /// final required equipment slot. The actual used state is still committed
    /// after the authored equip animation completes.
    /// </summary>
    public bool WillAllRequiredSlotsBeUsedAfter(PPEActionPanelController approvedPanel)
    {
        if (approvedPanel == null || slots == null || slots.Length == 0)
            return false;

        foreach (PPEEquipmentVisualSlot slot in slots)
        {
            if (!SlotCountsTowardCompletion(slot))
                continue;

            if (usedSlots.Contains(slot))
                continue;

            PPEItemType? approvedType = GetPanelItemType(approvedPanel);
            PPEItemType? slotType = GetItemType(slot);
            if (!approvedType.HasValue || !slotType.HasValue || slotType.Value != approvedType.Value)
                return false;
        }

        return true;
    }

    /// <summary>Completion return clears worn child PPE and restores bare hand models only.</summary>
    public void ResetWornVisualsForCompletionReturn()
    {
        foreach (Coroutine pendingActivation in pendingActivations)
        {
            if (pendingActivation != null)
                StopCoroutine(pendingActivation);
        }

        pendingActivations.Clear();
        animatingSlots.Clear();
        usedSlots.Clear();
        tapedItemTypes.Clear();

        foreach (PPEEquipmentVisualSlot slot in slots ?? Array.Empty<PPEEquipmentVisualSlot>())
        {
            if (slot == null)
                continue;

            if (slot.EquippedChild != null &&
                !IsHandModelVisual(slot.EquippedChild) &&
                authoredPoses.TryGetValue(slot, out AuthoredPose pose))
            {
                SetAuthoredLocalPose(slot.EquippedChild.transform, pose);
            }
            SetChildVisible(slot, false);
        }

        SetTapeVisuals(false);

        foreach (PPEHandModelSwap swap in handModelSwaps ?? Array.Empty<PPEHandModelSwap>())
        {
            if (swap == null)
                continue;

            PPEHandModelCrossfade.Snap(
                new[] { swap.BareSuitHand, swap.BareInnerGloveHand, swap.InnerGloveHand, swap.GloveHand, swap.TapedHand },
                new[] { swap.BareHand });
        }
    }

    /// <summary>
    /// Restores only the authored bare-hand presentation for the card-selection
    /// return. PPE usage and tape state are intentionally preserved for result
    /// collection after the finale.
    /// </summary>
    public void ShowBareHandsForCardSelection()
    {
        handModelActiveStates.Clear();
        handModelsTemporarilyHidden = false;

        foreach (PPEHandModelSwap swap in handModelSwaps ?? Array.Empty<PPEHandModelSwap>())
        {
            if (swap == null)
                continue;

            PPEHandModelCrossfade.Snap(
                new[] { swap.BareSuitHand, swap.BareInnerGloveHand, swap.InnerGloveHand, swap.GloveHand, swap.TapedHand },
                new[] { swap.BareHand });
        }
    }

    void OnEnable()
    {
        if (equipmentRoot == null)
        {
            Debug.LogError(
                "PPEEquipmentVisualController requires an authored full-suit model root.",
                this);
            enabled = false;
            return;
        }

        // Full-suit body must stay on a Main Camera layer so looking down shows the worn model.
        // Only optionally promote the entire root (legacy), or later promote helmet/mask children.
        if (Application.isPlaying && applyMirrorOnlyLayerToEntireEquipmentRoot)
            SetLayerRecursively(equipmentRoot.gameObject, mirrorOnlyLayer);

        if (animateChildrenOnUse && (motionCurve == null || motionCurve.length < 2))
        {
            Debug.LogError(
                "Animated PPE child slots require an authored motion curve for attach-only equip.",
                this);
            enabled = false;
            return;
        }

        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning(
                "PPEEquipmentVisualController has no authored visual slots yet. Add each PPE child under the full-suit model and assign it here.",
                this);
        }

        foreach (PPEEquipmentVisualSlot slot in slots ?? Array.Empty<PPEEquipmentVisualSlot>())
        {
            if (slot == null || slot.ItemBinding == null)
                continue;

            // Hide the authored child before validating the panel binding. The
            // full-suit root may be visible in Edit Mode, but no optional PPE
            // child should leak into the initial Play Mode presentation.
            if (hideUnusedChildrenOnEnable)
                SetChildVisible(slot, false);

            PPEItemPresentationBinding binding = slot.ItemBinding;
            if (binding.ItemIdentity != null && binding.ItemIdentity.ItemType == PPEItemType.HazmatSuit)
            {
                Debug.LogWarning(
                    "HazmatSuit is controlled by PPEHazmatEquipController and should not be assigned as a child slot.",
                    binding);
                continue;
            }

            if (slot.EquippedChild == null)
            {
                Debug.LogWarning(
                    $"PPE visual slot '{binding.name}' has no equipped child assigned yet.",
                    binding);
            }
            else
            {
                authoredPoses[slot] = CaptureAuthoredPose(slot.EquippedChild.transform);
                if (!slot.EquippedChild.transform.IsChildOf(equipmentRoot) &&
                    !IsHandModelVisual(slot.EquippedChild))
                {
                    Debug.LogWarning(
                        $"PPE visual slot '{slot.EquippedChild.name}' is not a child of the authored full-suit model root '{equipmentRoot.name}'.",
                        slot.EquippedChild);
                }
            }

            PPEActionPanelController authoredPanel =
                binding.InspectionVisual == null
                    ? null
                    : binding.InspectionVisual.GetComponent<PPEActionPanelController>();
            if (authoredPanel != null)
                SubscribePanelToSlot(authoredPanel, slot);

            PPEItemType? slotType = GetItemType(slot);
            if (slotType.HasValue)
            {
                foreach (PPEActionPanelController panel in FindObjectsByType<PPEActionPanelController>(
                             FindObjectsInactive.Include))
                {
                    if (GetPanelItemType(panel) == slotType.Value)
                        SubscribePanelToSlot(panel, slot);
                }
            }
        }

        SubscribeHazmatHandRefresh();
        SetTapeVisuals(false);

        runtimeInitialized = true;
    }

    void OnDisable()
    {
        if (!runtimeInitialized)
            return;

        foreach (KeyValuePair<PPEActionPanelController, PPEEquipmentVisualSlot> pair in subscribedPanels)
        {
            if (pair.Key != null && panelHandlers.TryGetValue(pair.Key, out Action<PPEActionChoice, PPEActionResult> handler))
                pair.Key.ChoiceResolved -= handler;
        }

        foreach (Coroutine pendingActivation in pendingActivations)
        {
            if (pendingActivation != null)
                StopCoroutine(pendingActivation);
        }

        UnsubscribeHazmatHandRefresh();
        pendingActivations.Clear();
        subscribedPanels.Clear();
        panelHandlers.Clear();
        authoredPoses.Clear();
        animatingSlots.Clear();
        usedSlots.Clear();
        tapedItemTypes.Clear();
        handModelActiveStates.Clear();
        handModelsTemporarilyHidden = false;
        PPEHandModelCrossfade.ClearCache();
        runtimeInitialized = false;
    }

    void OnChoiceResolved(
        PPEActionPanelController panel,
        PPEEquipmentVisualSlot slot,
        PPEActionChoice choice,
        PPEActionResult result)
    {
        if (panel == null || slot == null ||
            choice != PPEActionChoice.Use || result != PPEActionResult.UseApproved ||
            slot.ItemBinding == null || slot.EquippedChild == null ||
            usedSlots.Contains(slot) || animatingSlots.Contains(slot))
            return;

        animatingSlots.Add(slot);
        usedSlots.Add(slot);
        Coroutine activation = StartCoroutine(ResolveChildAfterInspection(panel, slot));
        pendingActivations.Add(activation);
    }

    IEnumerator ResolveChildAfterInspection(
        PPEActionPanelController panel,
        PPEEquipmentVisualSlot slot)
    {
        yield return null;

        if (slot == null || slot.EquippedChild == null || !authoredPoses.TryGetValue(slot, out AuthoredPose authoredPose))
        {
            if (slot != null)
                animatingSlots.Remove(slot);
            yield break;
        }

        PPEItemType itemType = slot.ItemBinding != null && slot.ItemBinding.ItemIdentity != null
            ? slot.ItemBinding.ItemIdentity.ItemType
            : default;

        if (itemType == PPEItemType.PackingTape)
        {
            MarkAllTappableEquipmentAsTaped();
            RefreshWornHandModels();
            animatingSlots.Remove(slot);
            panel.HideInspectionVisualAfterReusableUse();
            SetTapeVisuals(true);
            yield break;
        }

        if (!IsHandModelVisual(slot.EquippedChild))
        {
            SetChildVisible(slot, true);
            ApplyMirrorOnlyIfNeeded(slot);
            if (authoredPoses.TryGetValue(slot, out authoredPose))
                SetAuthoredLocalPose(slot.EquippedChild.transform, authoredPose);
        }
        if (slot.ItemBinding.ItemIdentity != null)
            RefreshWornHandModels();
        animatingSlots.Remove(slot);
        RefreshTapeVisualsAfterEquipmentUse();
    }

    void SnapHandModelSwap(PPEItemType itemType)
    {
        SnapHandModelSwap(itemType, null);
    }

    public void RefreshWornHandModels()
    {
        ApplyWornHand(PPEItemType.RubberGloveLeft, PPEItemType.NitrileInnerGloveLeft);
        ApplyWornHand(PPEItemType.RubberGloveRight, PPEItemType.NitrileInnerGloveRight);
    }

    void ApplyWornHand(PPEItemType outerType, PPEItemType nitrileType)
    {
        if (IsItemUsed(PPEItemType.PackingTape) &&
            IsItemUsed(outerType) &&
            tapedItemTypes.Contains(outerType))
        {
            SnapHandModelSwap(PPEItemType.PackingTape, outerType);
            return;
        }

        if (IsItemUsed(outerType))
        {
            SnapHandModelSwap(outerType);
            return;
        }

        if (IsItemUsed(nitrileType))
            SnapHandModelSwap(nitrileType);
    }

    void SnapHandModelSwap(PPEItemType itemType, PPEItemType? tapeRequiredItemType)
    {
        if (handModelSwaps == null)
            return;

        List<GameObject> hideRoots = new();
        List<GameObject> showRoots = new();
        bool matched = false;
        foreach (PPEHandModelSwap swap in handModelSwaps)
        {
            if (swap == null || swap.ItemType != itemType)
                continue;

            bool isTape = itemType == PPEItemType.PackingTape;
            if (isTape && tapeRequiredItemType.HasValue &&
                swap.TapeRequiredItemType != tapeRequiredItemType.Value)
                continue;
            if (isTape && (!IsItemUsed(swap.TapeRequiredItemType) ||
                           !tapedItemTypes.Contains(swap.TapeRequiredItemType)))
                continue;

            matched = true;
            hideRoots.Add(swap.BareHand);
            hideRoots.Add(swap.BareSuitHand);
            hideRoots.Add(swap.BareInnerGloveHand);
            hideRoots.Add(swap.InnerGloveHand);
            hideRoots.Add(swap.GloveHand);
            hideRoots.Add(swap.TapedHand);

            bool showTapedVersion = !isTape &&
                IsItemUsed(PPEItemType.PackingTape) &&
                tapedItemTypes.Contains(itemType);
            GameObject show;
            if (isTape || showTapedVersion)
                show = swap.TapedHand;
            else if (IsNitrileInnerGlove(itemType))
                show = ResolveNitrileHand(swap);
            else
                show = swap.GloveHand;

            if (show != null)
            {
                hideRoots.RemoveAll(hidden => hidden == show);
                showRoots.Add(show);
            }

            if (!isTape)
                break;
        }

        if (!matched || (showRoots.Count == 0 && hideRoots.Count == 0))
            return;

        PPEHandModelCrossfade.Snap(hideRoots.ToArray(), showRoots.ToArray());
    }

    void SubscribeHazmatHandRefresh()
    {
        UnsubscribeHazmatHandRefresh();
        subscribedHazmat = FindAnyObjectByType<PPEHazmatEquipController>();
        if (subscribedHazmat != null)
            subscribedHazmat.EquipAnimationCompleted += RefreshWornHandModels;
    }

    void UnsubscribeHazmatHandRefresh()
    {
        if (subscribedHazmat == null)
            return;

        subscribedHazmat.EquipAnimationCompleted -= RefreshWornHandModels;
        subscribedHazmat = null;
    }

    GameObject ResolveNitrileHand(PPEHandModelSwap swap)
    {
        if (swap == null)
            return null;

        if (subscribedHazmat != null && subscribedHazmat.IsEquipped)
        {
            if (swap.InnerGloveHand == null)
            {
                Debug.LogError(
                    "PPEEquipmentVisualController requires authored InnerGloveSuit hands after the hazmat suit is worn.",
                    this);
            }

            return swap.InnerGloveHand;
        }

        if (swap.BareInnerGloveHand == null)
        {
            Debug.LogError(
                "PPEEquipmentVisualController requires authored PPE_A_Hand_InnerGlove models before the hazmat suit is worn.",
                this);
        }

        return swap.BareInnerGloveHand;
    }

    void SetTapeVisuals(bool tapeUsed)
    {
        foreach (PPEEquipmentTapeVisual tapeVisual in tapeVisuals ?? Array.Empty<PPEEquipmentTapeVisual>())
        {
            if (tapeVisual?.Visual == null)
                continue;

            tapeVisual.Visual.SetActive(
                tapeUsed &&
                tapedItemTypes.Contains(tapeVisual.RequiredItemType) &&
                IsItemUsed(tapeVisual.RequiredItemType));
        }
    }

    void MarkAllTappableEquipmentAsTaped()
    {
        foreach (PPEEquipmentVisualSlot slot in slots ?? Array.Empty<PPEEquipmentVisualSlot>())
        {
            PPEItemType? itemType = GetItemType(slot);
            if (itemType.HasValue && IsTappableEquipment(itemType.Value))
                tapedItemTypes.Add(itemType.Value);
        }
    }

    void RefreshTapeVisualsAfterEquipmentUse()
    {
        if (IsItemUsed(PPEItemType.PackingTape))
            SetTapeVisuals(true);
    }

    void CaptureHandModelActiveState(GameObject model)
    {
        if (model != null && !handModelActiveStates.ContainsKey(model))
            handModelActiveStates.Add(model, model.activeSelf);
    }

    void SubscribePanelToSlot(PPEActionPanelController panel, PPEEquipmentVisualSlot slot)
    {
        if (panel == null || slot == null || subscribedPanels.ContainsKey(panel))
            return;

        Action<PPEActionChoice, PPEActionResult> handler =
            (choice, result) => OnChoiceResolved(panel, slot, choice, result);
        panel.ChoiceResolved += handler;
        subscribedPanels.Add(panel, slot);
        panelHandlers.Add(panel, handler);
    }

    bool SlotCountsTowardCompletion(PPEEquipmentVisualSlot slot)
    {
        if (slot == null || slot.ItemBinding == null || slot.EquippedChild == null)
            return false;

        if (requiredWearItemTypes == null || requiredWearItemTypes.Length == 0)
            return true;

        PPEItemType? slotType = GetItemType(slot);
        return slotType.HasValue && Array.IndexOf(requiredWearItemTypes, slotType.Value) >= 0;
    }

    static PPEItemType? GetPanelItemType(PPEActionPanelController panel)
    {
        return panel?.InspectionState?.PresentationBinding?.ItemIdentity == null
            ? null
            : panel.InspectionState.PresentationBinding.ItemIdentity.ItemType;
    }

    static PPEItemType? GetItemType(PPEEquipmentVisualSlot slot)
    {
        return slot?.ItemBinding?.ItemIdentity == null
            ? null
            : slot.ItemBinding.ItemIdentity.ItemType;
    }

    static bool IsTappableEquipment(PPEItemType itemType)
    {
        return itemType == PPEItemType.RubberGloveLeft ||
            itemType == PPEItemType.RubberGloveRight ||
            itemType == PPEItemType.RubberBootLeft ||
            itemType == PPEItemType.RubberBootRight;
    }

    static bool IsNitrileInnerGlove(PPEItemType itemType)
    {
        return itemType == PPEItemType.NitrileInnerGloveLeft ||
            itemType == PPEItemType.NitrileInnerGloveRight;
    }

    bool IsHandModelVisual(GameObject visual)
    {
        if (visual == null)
            return false;

        foreach (PPEHandModelSwap swap in handModelSwaps ?? Array.Empty<PPEHandModelSwap>())
        {
            if (swap == null)
                continue;
            if (visual == swap.BareHand ||
                visual == swap.BareSuitHand ||
                visual == swap.BareInnerGloveHand ||
                visual == swap.InnerGloveHand ||
                visual == swap.GloveHand ||
                visual == swap.TapedHand)
            {
                return true;
            }
        }

        return false;
    }

    void ApplyMirrorOnlyIfNeeded(PPEEquipmentVisualSlot slot)
    {
        if (slot?.EquippedChild == null ||
            applyMirrorOnlyLayerToEntireEquipmentRoot ||
            slot.ItemBinding?.ItemIdentity == null)
            return;

        PPEItemType itemType = slot.ItemBinding.ItemIdentity.ItemType;
        if (itemType != PPEItemType.ConstructionHelmet &&
            itemType != PPEItemType.GasMask &&
            itemType != PPEItemType.FaceShield &&
            itemType != PPEItemType.SafetyGoggles)
            return;

        SetLayerRecursively(slot.EquippedChild, mirrorOnlyLayer);
    }

    static void SetChildVisible(PPEEquipmentVisualSlot slot, bool visible)
    {
        if (slot != null && slot.EquippedChild != null)
            slot.EquippedChild.SetActive(visible);
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    struct AuthoredPose
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    static AuthoredPose CaptureAuthoredPose(Transform target)
    {
        return new AuthoredPose
        {
            localPosition = target.localPosition,
            localRotation = target.localRotation,
            localScale = target.localScale
        };
    }

    void GetAuthoredWorldPose(
        AuthoredPose authoredPose,
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 worldScale)
    {
        position = equipmentRoot.TransformPoint(authoredPose.localPosition);
        rotation = equipmentRoot.rotation * authoredPose.localRotation;
        worldScale = GetAuthoredWorldScale(authoredPose);
    }

    Vector3 GetAttachStartPosition(Vector3 targetPosition)
    {
        // Prefer body-anchor forward (parent of hazmat root) so "in front" means in front of the wearer.
        Transform body = equipmentRoot != null ? equipmentRoot.parent : null;
        Vector3 forward = body != null ? body.forward : (equipmentRoot != null ? equipmentRoot.forward : Vector3.forward);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        return targetPosition + forward * attachStartForwardDistance;
    }

    Vector3 GetAuthoredWorldScale(AuthoredPose authoredPose)
    {
        return Vector3.Scale(equipmentRoot.lossyScale, authoredPose.localScale);
    }

    static void SetAuthoredLocalPose(Transform target, AuthoredPose authoredPose)
    {
        target.SetLocalPositionAndRotation(authoredPose.localPosition, authoredPose.localRotation);
        target.localScale = authoredPose.localScale;
    }

    static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.00001f ? value / divisor : value;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Transform configuredEquipmentRoot,
        PPEEquipmentVisualSlot[] configuredSlots,
        Transform configuredFrontStartAnchor,
        Transform configuredApproachAnchor)
    {
        equipmentRoot = configuredEquipmentRoot;
        slots = configuredSlots;
        frontStartAnchor = configuredFrontStartAnchor;
        approachAnchor = configuredApproachAnchor;
    }
#endif
}
