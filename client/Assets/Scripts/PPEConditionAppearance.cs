using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEInspectionState))]
public sealed class PPEConditionAppearance : MonoBehaviour
{
    static readonly int ContaminationStrengthId =
        Shader.PropertyToID("_ContaminationStrength");

    [SerializeField]
    PPEInspectionState inspectionState;

    [SerializeField]
    Renderer[] targetRenderers;

    [SerializeField]
    [Range(0f, 1f)]
    float cleanStrength;

    [SerializeField]
    [Range(0f, 1f)]
    float contaminatedStrength = 1f;

    MaterialPropertyBlock propertyBlock;

    public PPEInspectionState InspectionState => inspectionState;
    public Renderer[] TargetRenderers => targetRenderers;
    public float CleanStrength => cleanStrength;
    public float ContaminatedStrength => contaminatedStrength;

    void OnEnable()
    {
        if (inspectionState == null || targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogError(
                "PPEConditionAppearance requires serialized state and renderer references.",
                this);
            enabled = false;
            return;
        }

        inspectionState.ConditionChanged += ApplyCondition;
        ApplyCondition(inspectionState.CurrentCondition);
    }

    void OnDisable()
    {
        if (inspectionState != null)
            inspectionState.ConditionChanged -= ApplyCondition;
    }

    void ApplyCondition(PPEItemCondition condition)
    {
        float strength = condition == PPEItemCondition.Contaminated
            ? contaminatedStrength
            : cleanStrength;

        propertyBlock ??= new MaterialPropertyBlock();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(ContaminationStrengthId, strength);
            targetRenderer.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEInspectionState configuredInspectionState,
        Renderer[] configuredTargetRenderers,
        float configuredCleanStrength,
        float configuredContaminatedStrength)
    {
        inspectionState = configuredInspectionState;
        targetRenderers = configuredTargetRenderers;
        cleanStrength = configuredCleanStrength;
        contaminatedStrength = configuredContaminatedStrength;
    }
#endif
}
