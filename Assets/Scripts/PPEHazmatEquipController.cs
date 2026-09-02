using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PPEHazmatEquipController : MonoBehaviour
{
    [Header("Hazmat flow")]
    [SerializeField]
    PPEActionPanelController actionPanelController;

    [SerializeField]
    PPEItemPresentationBinding presentationBinding;

    [Header("Body reference")]
    [SerializeField]
    Transform headTransform;

    [SerializeField]
    [Tooltip("hazmat_suit_on의 부모입니다. 이 Transform이 HMD의 위치와 수평 Yaw를 따라갑니다.")]
    Transform bodyAnchor;

    [SerializeField]
    [Tooltip("착용 연출이 지나가는 중간 지점입니다. 최종 위치는 hazmat_suit_on의 Transform을 직접 사용합니다.")]
    Transform approachAnchor;

    [SerializeField]
    [Tooltip("착용 연출이 시작되는 몸 앞쪽 지점입니다. 위치와 회전은 씬 Inspector 값이 기준입니다.")]
    Transform frontStartAnchor;

    [SerializeField]
    [Min(0f)]
    float bodyYawFollowSmoothTime = 0.2f;

    [Header("Equipped visual")]
    [SerializeField]
    [Tooltip("Edit Mode에서 Pose를 조정할 수 있도록 씬에서 보이는 hazmat_suit_on의 표시 대상입니다.")]
    Renderer[] equippedRenderers;

    [Header("Hand model swap")]
    [SerializeField]
    [Tooltip("착용 전 표시하는 PPE_A_Hand_Bare_L과 PPE_A_Hand_Bare_R입니다.")]
    GameObject[] unequippedHandModels;

    [SerializeField]
    [Tooltip("방호복 착용 완료 뒤 표시하는 PPE_A_Hand_Suit_L과 PPE_A_Hand_Suit_R입니다.")]
    GameObject[] equippedHandModels;

    [SerializeField]
    [Min(0f)]
    [Tooltip("BareHand ↔ BareHand_Suit opaque luminance crossfade duration. Uses Hand Form Unlit _Fade only.")]
    float handSwapFadeDuration = 0.4f;

    [Header("Equip animation")]
    [SerializeField]
    [Min(0f)]
    float animationDuration = 1.25f;

    [SerializeField]
    bool useUnscaledTime = true;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    float approachPhaseEnd = 0.65f;

    [SerializeField]
    AnimationCurve motionCurve;

    [SerializeField]
    bool logStateChanges = true;

    Coroutine equipRoutine;
    readonly List<PPEActionPanelController> subscribedHazmatPanels = new();
    PPEActionPanelController approvedSourcePanel;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    Vector3 authoredLocalScale;
    Vector3 authoredBodyAnchorLocalPosition;
    Quaternion authoredBodyAnchorLocalRotation;
    bool hasAuthoredBodyAnchorPosition;
    float bodyYaw;
    float bodyYawVelocity;
    bool hasBodyYaw;
    bool hasAuthoredPose;
    bool runtimeInitialized;
    bool equippedVisualVisible;
    Transform gameViewHeadOverride;

    public PPEActionPanelController ActionPanelController => actionPanelController;
    public PPEItemPresentationBinding PresentationBinding => presentationBinding;
    public Transform HeadTransform => gameViewHeadOverride != null ? gameViewHeadOverride : headTransform;
    public Transform BodyAnchor => bodyAnchor;
    public Transform ApproachAnchor => approachAnchor;
    public Transform FrontStartAnchor => frontStartAnchor;
    public Renderer[] EquippedRenderers => equippedRenderers;
    public GameObject[] UnequippedHandModels => unequippedHandModels;
    public GameObject[] EquippedHandModels => equippedHandModels;

    public bool TryGetWearHandModel(bool rightHand, out Transform hand)
    {
        hand = null;
        GameObject[] models = IsEquipped ? equippedHandModels : unequippedHandModels;
        if (models == null)
            return false;

        Transform named = FindHandModelByName(models, rightHand);
        if (named != null)
        {
            hand = named;
            return true;
        }

        int index = rightHand ? 1 : 0;
        if (index < 0 || index >= models.Length || models[index] == null)
            return false;

        hand = models[index].transform;
        return true;
    }
    public float HandSwapFadeDuration => handSwapFadeDuration;
    public float BodyYawFollowSmoothTime => bodyYawFollowSmoothTime;
    public float AnimationDuration => animationDuration;
    public bool UseUnscaledTime => useUnscaledTime;
    public float ApproachPhaseEnd => approachPhaseEnd;
    public AnimationCurve MotionCurve => motionCurve;
    public bool IsAnimating => equipRoutine != null;
    public bool IsEquipped { get; private set; }
    public GameObject RuntimeEquippedVisual => equippedVisualVisible ? gameObject : null;

    public event Action EquipAnimationCompleted;

    public void SetGameViewHeadOverride(Transform overrideHead)
    {
        gameViewHeadOverride = overrideHead;
    }

    public void ClearGameViewHeadOverride()
    {
        gameViewHeadOverride = null;
    }

    public void ResetForNewSession()
    {
        if (equipRoutine != null)
        {
            StopCoroutine(equipRoutine);
            equipRoutine = null;
        }

        SetEquippedVisualVisible(false);
        SnapHandModelsEquipped(false);
        RestoreAuthoredPose();
        RestoreAuthoredBodyAnchorPose();
        IsEquipped = false;
        hasBodyYaw = false;
        bodyYawVelocity = 0f;
        PPEHandModelCrossfade.ClearCache();
    }

    void Awake()
    {
        if (Application.isPlaying)
            SetEquippedVisualVisible(false);
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (!HasCompleteReferences())
        {
            Debug.LogError(
                "PPEHazmatEquipController must be authored on the visible hazmat_suit_on object " +
                "with panel, presentation, head, body, approach, renderer, hand-model, and animation references.",
                this);
            enabled = false;
            return;
        }

        CaptureAuthoredPose();
        CaptureAuthoredBodyAnchorPosition();
        // Edit Mode에서는 최종 Pose를 눈으로 조정할 수 있도록 Renderer를 작성 활성 상태로 둡니다.
        // Play Mode의 첫 렌더 전에 숨기고 UseApproved에서만 다시 표시합니다.
        SetEquippedVisualVisible(false);
        SnapHandModelsEquipped(false);
        SubscribeHazmatPanels();
        runtimeInitialized = true;
    }

    void OnDisable()
    {
        if (!runtimeInitialized)
            return;

        UnsubscribeHazmatPanels();
        approvedSourcePanel = null;

        if (equipRoutine != null)
        {
            StopCoroutine(equipRoutine);
            equipRoutine = null;
        }

        SetEquippedVisualVisible(false);
        SnapHandModelsEquipped(false);
        RestoreAuthoredPose();
        RestoreAuthoredBodyAnchorPose();
        IsEquipped = false;
        hasBodyYaw = false;
        hasAuthoredBodyAnchorPosition = false;
        bodyYawVelocity = 0f;
        PPEHandModelCrossfade.ClearCache();
        runtimeInitialized = false;
    }

    void LateUpdate()
    {
        // Before UseApproved, PPE Body Anchor and PPE_A_SuitWear must remain at
        // their scene-authored PPE-room position. Follow the player only while
        // the equip animation is running and after equip has completed.
        if (runtimeInitialized && (equipRoutine != null || IsEquipped))
            UpdateBodyAnchor();
    }

    void OnHazmatChoiceResolved(
        PPEActionPanelController panel,
        PPEActionChoice choice,
        PPEActionResult result)
    {
        if (choice != PPEActionChoice.Use || result != PPEActionResult.UseApproved)
            return;

        if (!IsHazmatSuitPanel(panel))
            return;

        if (equipRoutine != null || IsEquipped)
            return;

        approvedSourcePanel = panel;
        InitializeBodyYaw();
        UpdateBodyAnchor();
        RestoreAuthoredPose();
        SetEquippedVisualVisible(true);
        SnapHandModelsEquipped(true);
        IsEquipped = true;
        EquipAnimationCompleted?.Invoke();

        if (logStateChanges)
        {
            Debug.Log(
                "[PPE Equip] Hazmat equipped by body proximity trigger. " +
                "The worn pose comes from the hazmat_suit_on Transform Inspector.",
                this);
        }
    }

    void InitializeBodyYaw()
    {
        bodyYaw = HeadTransform.eulerAngles.y;
        bodyYawVelocity = 0f;
        hasBodyYaw = true;
    }

    void UpdateBodyAnchor()
    {
        if (!hasBodyYaw)
        {
            InitializeBodyYaw();
        }
        else if (bodyYawFollowSmoothTime <= 0f)
        {
            bodyYaw = HeadTransform.eulerAngles.y;
        }
        else
        {
            bodyYaw = Mathf.SmoothDampAngle(
                bodyYaw,
                HeadTransform.eulerAngles.y,
                ref bodyYawVelocity,
                bodyYawFollowSmoothTime,
                Mathf.Infinity,
                useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        // Body Anchor는 Camera Offset이 아니라 XR Origin 아래에 둔다.
        // 예전처럼 월드 Y를 고정하면 Camera Offset의 CameraYOffset과 싸워
        // local Y가 음수로 밀리며 몸이 바닥에 처박힌다.
        // The authored X/Z locate the unworn suit in the PPE room. They are not
        // a body-space offset and must not be added after UseApproved. Once worn,
        // X/Z follow the tracked head while the authored local Y grounds the suit.
        if (!hasAuthoredBodyAnchorPosition)
            CaptureAuthoredBodyAnchorPosition();

        Transform anchorParent = bodyAnchor.parent;
        Vector3 headPosition = HeadTransform.position;
        Vector3 localPosition = bodyAnchor.localPosition;
        if (anchorParent != null)
        {
            Vector3 headLocal = anchorParent.InverseTransformPoint(headPosition);
            localPosition.x = headLocal.x;
            localPosition.z = headLocal.z;
        }
        else
        {
            localPosition.x = headPosition.x;
            localPosition.z = headPosition.z;
        }

        localPosition.y = authoredBodyAnchorLocalPosition.y;
        Quaternion worldRotation = Quaternion.Euler(0f, bodyYaw, 0f);
        Quaternion localRotation = anchorParent != null
            ? Quaternion.Inverse(anchorParent.rotation) * worldRotation
            : worldRotation;
        bodyAnchor.SetLocalPositionAndRotation(localPosition, localRotation);
    }

    void CaptureAuthoredBodyAnchorPosition()
    {
        if (bodyAnchor == null)
            return;

        authoredBodyAnchorLocalPosition = bodyAnchor.localPosition;
        authoredBodyAnchorLocalRotation = bodyAnchor.localRotation;
        hasAuthoredBodyAnchorPosition = true;
    }

    void RestoreAuthoredBodyAnchorPose()
    {
        if (!hasAuthoredBodyAnchorPosition || bodyAnchor == null)
            return;

        bodyAnchor.SetLocalPositionAndRotation(
            authoredBodyAnchorLocalPosition,
            authoredBodyAnchorLocalRotation);
    }

    void GetAuthoredBodyPose(
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 worldScale)
    {
        position = bodyAnchor.TransformPoint(authoredLocalPosition);
        rotation = bodyAnchor.rotation * authoredLocalRotation;
        worldScale = Vector3.Scale(bodyAnchor.lossyScale, authoredLocalScale);
    }

    void CaptureAuthoredPose()
    {
        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        authoredLocalScale = transform.localScale;
        hasAuthoredPose = true;
    }

    void RestoreAuthoredPose()
    {
        if (!hasAuthoredPose)
            return;

        if (transform.parent != bodyAnchor)
            transform.SetParent(bodyAnchor, false);

        transform.SetLocalPositionAndRotation(authoredLocalPosition, authoredLocalRotation);
        transform.localScale = authoredLocalScale;
    }

    void SetEquippedVisualVisible(bool visible)
    {
        foreach (Renderer targetRenderer in equippedRenderers ?? Array.Empty<Renderer>())
        {
            if (targetRenderer != null)
                targetRenderer.enabled = visible;
        }

        equippedVisualVisible = visible;
    }

    IEnumerator FadeHandModelsEquipped(bool equipped)
    {
        GameObject[] modelsToShow = equipped ? equippedHandModels : unequippedHandModels;
        GameObject[] modelsToHide = equipped ? unequippedHandModels : equippedHandModels;
        if (modelsToShow == null || modelsToHide == null)
            yield break;

        yield return PPEHandModelCrossfade.Crossfade(
            modelsToHide,
            modelsToShow,
            handSwapFadeDuration,
            useUnscaledTime);
    }

    void SnapHandModelsEquipped(bool equipped)
    {
        GameObject[] modelsToShow = equipped ? equippedHandModels : unequippedHandModels;
        GameObject[] modelsToHide = equipped ? unequippedHandModels : equippedHandModels;
        PPEHandModelCrossfade.Snap(modelsToHide, modelsToShow);
    }

    void SubscribeHazmatPanels()
    {
        UnsubscribeHazmatPanels();

        if (actionPanelController != null)
            TryAddHazmatPanel(actionPanelController);

        PPEActionPanelController[] panels = FindObjectsByType<PPEActionPanelController>(
            FindObjectsInactive.Include);
        foreach (PPEActionPanelController panel in panels)
            TryAddHazmatPanel(panel);
    }

    void TryAddHazmatPanel(PPEActionPanelController panel)
    {
        if (!IsHazmatSuitPanel(panel) || subscribedHazmatPanels.Contains(panel))
            return;

        panel.ChoiceResolvedWithSource += OnHazmatChoiceResolved;
        subscribedHazmatPanels.Add(panel);
    }

    void UnsubscribeHazmatPanels()
    {
        foreach (PPEActionPanelController panel in subscribedHazmatPanels)
        {
            if (panel != null)
                panel.ChoiceResolvedWithSource -= OnHazmatChoiceResolved;
        }

        subscribedHazmatPanels.Clear();
    }

    static bool IsHazmatSuitPanel(PPEActionPanelController panel)
    {
        return panel != null &&
            panel.InspectionState != null &&
            panel.InspectionState.PresentationBinding != null &&
            panel.InspectionState.PresentationBinding.ItemIdentity != null &&
            panel.InspectionState.PresentationBinding.ItemIdentity.ItemType == PPEItemType.HazmatSuit;
    }

    PPEItemPresentationBinding ResolvePresentationBinding()
    {
        if (presentationBinding != null &&
            presentationBinding.InspectionVisual != null &&
            presentationBinding.EquippedVisual == gameObject)
        {
            return presentationBinding;
        }

        PPEItemPresentationBinding[] bindings = FindObjectsByType<PPEItemPresentationBinding>(
            FindObjectsInactive.Include);
        foreach (PPEItemPresentationBinding binding in bindings)
        {
            if (binding != null &&
                binding.InspectionVisual != null &&
                binding.EquippedVisual == gameObject)
            {
                return binding;
            }
        }

        return presentationBinding;
    }

    bool HasCompleteReferences()
    {
        PPEItemPresentationBinding resolvedBinding = ResolvePresentationBinding();
        bool hasHazmatPanel = actionPanelController != null ||
            subscribedHazmatPanels.Count > 0 ||
            HasAnyHazmatPanel();

        if (!hasHazmatPanel ||
            resolvedBinding == null ||
            resolvedBinding.InspectionVisual == null ||
            resolvedBinding.EquippedVisual != gameObject ||
            headTransform == null ||
            bodyAnchor == null ||
            approachAnchor == null ||
            frontStartAnchor == null ||
            transform.parent != bodyAnchor ||
            approachAnchor.parent != bodyAnchor ||
            frontStartAnchor.parent != bodyAnchor ||
            motionCurve == null ||
            motionCurve.length < 2 ||
            equippedRenderers == null ||
            equippedRenderers.Length == 0 ||
            !HasCompleteHandModelReferences())
        {
            return false;
        }

        foreach (Renderer targetRenderer in equippedRenderers)
        {
            if (targetRenderer == null ||
                (!targetRenderer.transform.IsChildOf(transform) && targetRenderer.transform != transform))
            {
                return false;
            }
        }

        return true;
    }

    static bool HasAnyHazmatPanel()
    {
        PPEActionPanelController[] panels = FindObjectsByType<PPEActionPanelController>(
            FindObjectsInactive.Include);
        foreach (PPEActionPanelController panel in panels)
        {
            if (IsHazmatSuitPanel(panel))
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    public void ConfigureFrontStartForEditor(
        Transform configuredFrontStartAnchor,
        float configuredAnimationDuration)
    {
        frontStartAnchor = configuredFrontStartAnchor;
        animationDuration = configuredAnimationDuration;
    }
#endif

    bool HasCompleteHandModelReferences()
    {
        if (unequippedHandModels == null ||
            equippedHandModels == null ||
            unequippedHandModels.Length != 2 ||
            equippedHandModels.Length != 2)
        {
            return false;
        }

        for (int index = 0; index < 2; index++)
        {
            GameObject unequipped = unequippedHandModels[index];
            GameObject equipped = equippedHandModels[index];
            if (unequipped == null ||
                equipped == null ||
                unequipped == equipped)
            {
                return false;
            }
        }

        return true;
    }

    static Transform FindHandModelByName(GameObject[] models, bool rightHand)
    {
        foreach (GameObject model in models)
        {
            if (model == null)
                continue;

            if (IsRightHandModelName(model.name) == rightHand)
                return model.transform;
        }

        return null;
    }

    static bool IsRightHandModelName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return name.EndsWith("_R", StringComparison.Ordinal) ||
            name.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
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
}

/// <summary>
/// Lightweight opaque hand-model crossfade via MaterialPropertyBlock _Fade.
/// Keeps hands on the opaque Hand Form Unlit path; no Transparent queue, cameras, or RenderTextures.
/// </summary>
public static class PPEHandModelCrossfade
{
    static readonly int FadeId = Shader.PropertyToID("_Fade");
    static readonly MaterialPropertyBlock SharedBlock = new();
    static readonly Dictionary<GameObject, Renderer[]> RendererCache = new();

    public static IEnumerator Crossfade(
        GameObject[] hideRoots,
        GameObject[] showRoots,
        float duration,
        bool useUnscaledTime)
    {
        if (showRoots == null || hideRoots == null)
            yield break;

        Renderer[] showRenderers = CollectRenderers(showRoots);
        Renderer[] hideRenderers = CollectRenderers(hideRoots);

        foreach (GameObject root in showRoots)
        {
            if (root != null)
                root.SetActive(true);
        }

        SetFade(showRenderers, 0f);
        SetFade(hideRenderers, 1f);

        if (duration <= 0f)
        {
            Finish(hideRoots, showRenderers, hideRenderers);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = Mathf.SmoothStep(0f, 1f, t);
            SetFade(hideRenderers, 1f - curved);
            SetFade(showRenderers, curved);
            yield return null;
        }

        Finish(hideRoots, showRenderers, hideRenderers);
    }

    public static void Snap(
        GameObject[] hideRoots,
        GameObject[] showRoots)
    {
        if (showRoots != null)
        {
            foreach (GameObject root in showRoots)
            {
                if (root != null)
                    root.SetActive(true);
            }

            SetFade(CollectRenderers(showRoots), 1f);
        }

        if (hideRoots != null)
        {
            SetFade(CollectRenderers(hideRoots), 1f);
            foreach (GameObject root in hideRoots)
            {
                if (root != null)
                    root.SetActive(false);
            }
        }
    }

    public static void ClearCache()
    {
        RendererCache.Clear();
    }

    static void Finish(
        GameObject[] hideRoots,
        Renderer[] showRenderers,
        Renderer[] hideRenderers)
    {
        SetFade(showRenderers, 1f);
        SetFade(hideRenderers, 1f);

        if (hideRoots == null)
            return;

        foreach (GameObject root in hideRoots)
        {
            if (root != null)
                root.SetActive(false);
        }
    }

    static Renderer[] CollectRenderers(GameObject[] roots)
    {
        if (roots == null || roots.Length == 0)
            return Array.Empty<Renderer>();

        List<Renderer> combined = null;
        for (int index = 0; index < roots.Length; index++)
        {
            GameObject root = roots[index];
            if (root == null)
                continue;

            if (!RendererCache.TryGetValue(root, out Renderer[] cached) || cached == null)
            {
                cached = root.GetComponentsInChildren<Renderer>(true);
                RendererCache[root] = cached;
            }

            if (cached.Length == 0)
                continue;

            combined ??= new List<Renderer>(cached.Length * roots.Length);
            combined.AddRange(cached);
        }

        return combined == null ? Array.Empty<Renderer>() : combined.ToArray();
    }

    static void SetFade(Renderer[] renderers, float fade)
    {
        if (renderers == null)
            return;

        float clamped = Mathf.Clamp01(fade);
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer target = renderers[index];
            if (target == null)
                continue;

            target.GetPropertyBlock(SharedBlock);
            SharedBlock.SetFloat(FadeId, clamped);
            target.SetPropertyBlock(SharedBlock);
        }
    }
}
