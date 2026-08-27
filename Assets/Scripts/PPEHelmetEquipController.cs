using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PPEHelmetEquipController : MonoBehaviour
{
    [Header("Helmet flow")]
    [SerializeField]
    PPEActionPanelController actionPanelController;

    [SerializeField]
    PPEItemPresentationBinding presentationBinding;

    [Header("Head reference")]
    [SerializeField]
    Transform headTransform;

    [SerializeField]
    Transform approachAnchor;

    [Header("Equipped visual")]
    [SerializeField]
    GameObject equippedVisualPrefab;

    [SerializeField]
    Vector3 equippedVisualLocalPosition;

    [SerializeField]
    Vector3 equippedVisualLocalEulerAngles;

    [SerializeField]
    Vector3 equippedVisualLocalScale = Vector3.one;

    [SerializeField]
    [Range(0, 31)]
    int mirrorOnlyLayer = 30;

    [Header("Equip animation")]
    [SerializeField]
    [Min(0f)]
    float animationDuration = 0.9f;

    [SerializeField]
    bool useUnscaledTime = true;

    [SerializeField]
    [Range(0.01f, 0.99f)]
    float approachPhaseEnd = 0.6f;

    [SerializeField]
    AnimationCurve motionCurve;

    [SerializeField]
    bool logStateChanges = true;

    Coroutine equipRoutine;
    GameObject equippedVisualInstance;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    Vector3 authoredLocalScale;
    bool hasAuthoredPose;
    bool runtimeInitialized;
    Renderer[] authoredRenderers;

    public PPEActionPanelController ActionPanelController => actionPanelController;
    public PPEItemPresentationBinding PresentationBinding => presentationBinding;
    public Transform HeadTransform => headTransform;
    public Transform ApproachAnchor => approachAnchor;
    public GameObject EquippedVisualPrefab => equippedVisualPrefab;
    public int MirrorOnlyLayer => mirrorOnlyLayer;
    public float AnimationDuration => animationDuration;
    public bool UseUnscaledTime => useUnscaledTime;
    public float ApproachPhaseEnd => approachPhaseEnd;
    public AnimationCurve MotionCurve => motionCurve;
    public bool IsAnimating => equipRoutine != null;
    public bool IsEquipped { get; private set; }
    public GameObject RuntimeEquippedVisual => equippedVisualInstance;

    public event Action EquipAnimationCompleted;

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (!HasCompleteReferences())
        {
            Debug.LogError(
                "PPEHelmetEquipController requires serialized helmet panel, binding, head, " +
                "approach, prefab, layer, and animation references.",
                this);
            enabled = false;
            return;
        }

        CaptureAuthoredPose();
        authoredRenderers = GetComponentsInChildren<Renderer>(true);
        SetAuthoredRenderersVisible(false);
        actionPanelController.ChoiceResolved += OnChoiceResolved;
        runtimeInitialized = true;
    }

    void OnDisable()
    {
        if (!runtimeInitialized)
            return;

        if (actionPanelController != null)
            actionPanelController.ChoiceResolved -= OnChoiceResolved;

        if (equipRoutine != null)
        {
            StopCoroutine(equipRoutine);
            equipRoutine = null;
        }

        if (equippedVisualInstance != null)
            Destroy(equippedVisualInstance);

        equippedVisualInstance = null;
        SetAuthoredRenderersVisible(true);
        RestoreAuthoredPose();
        IsEquipped = false;
        runtimeInitialized = false;
    }

    void OnChoiceResolved(PPEActionChoice choice, PPEActionResult result)
    {
        if (choice != PPEActionChoice.Use || result != PPEActionResult.UseApproved)
            return;

        if (equipRoutine != null || IsEquipped)
            return;

        CaptureAuthoredPose();
        RestoreAuthoredPose();
        if (equippedVisualInstance == null && equippedVisualPrefab != null)
        {
            equippedVisualInstance = Instantiate(equippedVisualPrefab, transform, false);
            equippedVisualInstance.name = "Helmet Equipped Visual";
            equippedVisualInstance.transform.SetLocalPositionAndRotation(
                equippedVisualLocalPosition,
                Quaternion.Euler(equippedVisualLocalEulerAngles));
            equippedVisualInstance.transform.localScale = equippedVisualLocalScale;
            SetLayerRecursively(equippedVisualInstance, mirrorOnlyLayer);
        }

        IsEquipped = true;
        EquipAnimationCompleted?.Invoke();
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

        if (transform.parent != headTransform)
            transform.SetParent(headTransform, false);

        transform.SetLocalPositionAndRotation(authoredLocalPosition, authoredLocalRotation);
        transform.localScale = authoredLocalScale;
    }

    bool HasCompleteReferences()
    {
        return actionPanelController != null &&
            actionPanelController.InspectionState != null &&
            presentationBinding != null &&
            presentationBinding.ItemIdentity != null &&
            presentationBinding.ItemIdentity.ItemType == PPEItemType.ConstructionHelmet &&
            presentationBinding.InspectionVisual != null &&
            presentationBinding.EquippedVisual == gameObject &&
            actionPanelController.InspectionState.PresentationBinding == presentationBinding &&
            headTransform != null &&
            headTransform.GetComponent<Camera>() != null &&
            transform.parent == headTransform &&
            approachAnchor != null &&
            approachAnchor.parent == headTransform &&
            equippedVisualPrefab != null &&
            mirrorOnlyLayer >= 0 &&
            mirrorOnlyLayer <= 31 &&
            gameObject.layer == mirrorOnlyLayer &&
            motionCurve != null &&
            motionCurve.length >= 2;
    }

    void SetAuthoredRenderersVisible(bool visible)
    {
        if (authoredRenderers == null)
            return;

        foreach (Renderer renderer in authoredRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
