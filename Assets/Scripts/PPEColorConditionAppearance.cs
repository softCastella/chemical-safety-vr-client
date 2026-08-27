using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEInspectionState))]
public sealed class PPEColorConditionAppearance : MonoBehaviour
{
    [SerializeField]
    PPEInspectionState inspectionState;

    [SerializeField]
    Renderer[] targetRenderers;

    [SerializeField]
    string colorProperty = "_BaseColor";

    [SerializeField]
    Color cleanColor = Color.white;

    [SerializeField]
    Color contaminatedColor = new(0.55f, 0.42f, 0.16f, 1f);

    MaterialPropertyBlock propertyBlock;
    int colorPropertyId;

    public PPEInspectionState InspectionState => inspectionState;
    public Renderer[] TargetRenderers => targetRenderers;
    public string ColorProperty => colorProperty;
    public Color CleanColor => cleanColor;
    public Color ContaminatedColor => contaminatedColor;

    void OnEnable()
    {
        if (inspectionState == null ||
            targetRenderers == null ||
            targetRenderers.Length == 0 ||
            string.IsNullOrWhiteSpace(colorProperty))
        {
            Debug.LogError(
                "PPEColorConditionAppearance requires serialized state, renderer, and color-property references.",
                this);
            enabled = false;
            return;
        }

        colorPropertyId = Shader.PropertyToID(colorProperty);
        inspectionState.ConditionChanged += ApplyCondition;
        ApplyCondition(inspectionState.CurrentCondition);
    }

    void OnDisable()
    {
        if (inspectionState != null)
            inspectionState.ConditionChanged -= ApplyCondition;

        ClearOverrides();
    }

    void ApplyCondition(PPEItemCondition condition)
    {
        Color color = condition == PPEItemCondition.Contaminated
            ? contaminatedColor
            : cleanColor;

        propertyBlock ??= new MaterialPropertyBlock();
        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }
    }

    void ClearOverrides()
    {
        if (propertyBlock == null || targetRenderers == null)
            return;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, Color.white);
            targetRenderer.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }
    }
}
