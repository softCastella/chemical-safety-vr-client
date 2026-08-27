using UnityEngine;

/// <summary>
/// Scene-authored layout presets for the shared PPE action panel.
/// Runtime only switches between the serialized two-button and three-button values.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEActionPanelSharedPresentation : MonoBehaviour
{
    [System.Serializable]
    public struct LayoutPreset
    {
        public Vector2 panelSizeDelta;
        public Vector2 nameLabelAnchoredPosition;
        public Vector2 useButtonAnchoredPosition;
        public Vector2 discardButtonAnchoredPosition;
        public Vector2 inspectButtonAnchoredPosition;
        public Vector2 usePassIconAnchoredPosition;
        public Vector2 useErrorIconAnchoredPosition;
        public Vector2 discardPassIconAnchoredPosition;
        public Vector2 discardErrorIconAnchoredPosition;
        public Vector2 inspectPassIconAnchoredPosition;
    }

    [Header("Shared UI references")]
    [SerializeField]
    RectTransform panelRect;

    [SerializeField]
    RectTransform nameLabelRect;

    [SerializeField]
    RectTransform useButtonRect;

    [SerializeField]
    RectTransform discardButtonRect;

    [SerializeField]
    RectTransform inspectButtonRect;

    [SerializeField]
    RectTransform usePassIconRect;

    [SerializeField]
    RectTransform useErrorIconRect;

    [SerializeField]
    RectTransform discardPassIconRect;

    [SerializeField]
    RectTransform discardErrorIconRect;

    [SerializeField]
    RectTransform inspectPassIconRect;

    [Header("Authored presets")]
    [SerializeField]
    LayoutPreset twoButtonLayout;

    [SerializeField]
    LayoutPreset threeButtonLayout;

    [SerializeField]
    [Tooltip("몸 근접 착용에서 이름만 보여줄 때 쓰는 씬 작성 크기입니다. 2/3버튼 패널 높이를 쓰지 않습니다.")]
    LayoutPreset nameOnlyLayout;

    [SerializeField]
    bool hasPresets;

    public bool HasPresets => hasPresets;
    public LayoutPreset TwoButtonLayout => twoButtonLayout;
    public LayoutPreset ThreeButtonLayout => threeButtonLayout;
    public LayoutPreset NameOnlyLayout => nameOnlyLayout;

    public void Apply(bool showInspectChoice)
    {
        ApplyPreset(showInspectChoice ? threeButtonLayout : twoButtonLayout);
    }

    public void ApplyNameOnly()
    {
        if (nameOnlyLayout.panelSizeDelta.sqrMagnitude <= 0f)
            return;

        ApplyPreset(nameOnlyLayout);
    }

    void ApplyPreset(LayoutPreset preset)
    {
        if (!hasPresets || panelRect == null)
            return;

        panelRect.sizeDelta = preset.panelSizeDelta;

        if (nameLabelRect != null)
            nameLabelRect.anchoredPosition = preset.nameLabelAnchoredPosition;
        if (useButtonRect != null)
            useButtonRect.anchoredPosition = preset.useButtonAnchoredPosition;
        if (discardButtonRect != null)
            discardButtonRect.anchoredPosition = preset.discardButtonAnchoredPosition;
        if (inspectButtonRect != null)
            inspectButtonRect.anchoredPosition = preset.inspectButtonAnchoredPosition;

        ApplyStatusIconPositions(preset);
    }

    /// <summary>
    /// When no dedicated inspect pass icon exists, temporarily align the shared use-pass
    /// icon to the inspect button before showing 확인하기 feedback.
    /// </summary>
    public void PlaceFallbackInspectIcon(GameObject iconRoot)
    {
        if (!hasPresets || iconRoot == null)
            return;

        ApplyIconPosition(
            iconRoot.transform as RectTransform,
            threeButtonLayout.inspectPassIconAnchoredPosition);
    }

    public void RestoreStatusIconPositions(bool showInspectChoice)
    {
        if (!hasPresets)
            return;

        ApplyStatusIconPositions(showInspectChoice ? threeButtonLayout : twoButtonLayout);
    }

    void ApplyStatusIconPositions(LayoutPreset preset)
    {
        ApplyIconPosition(usePassIconRect, preset.usePassIconAnchoredPosition);
        ApplyIconPosition(useErrorIconRect, preset.useErrorIconAnchoredPosition);
        ApplyIconPosition(discardPassIconRect, preset.discardPassIconAnchoredPosition);
        ApplyIconPosition(discardErrorIconRect, preset.discardErrorIconAnchoredPosition);
        ApplyIconPosition(inspectPassIconRect, preset.inspectPassIconAnchoredPosition);
    }

    static void ApplyIconPosition(RectTransform iconRect, Vector2 anchoredPosition)
    {
        if (iconRect == null)
            return;

        iconRect.anchoredPosition = anchoredPosition;
    }

}
