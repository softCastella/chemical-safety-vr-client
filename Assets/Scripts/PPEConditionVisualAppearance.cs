using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEInspectionState))]
public sealed class PPEConditionVisualAppearance : MonoBehaviour
{
    [SerializeField]
    PPEInspectionState inspectionState;

    [SerializeField]
    GameObject[] cleanVisuals;

    [SerializeField]
    GameObject[] contaminatedVisuals;

    [Header("Surface contamination")]
    [SerializeField]
    bool useSurfaceTint;

    [SerializeField]
    Color surfaceContaminatedColor = new(0.58f, 0.38f, 0.12f, 1f);

    [SerializeField]
    Material surfaceContaminatedMaterial;

    readonly List<Renderer> surfaceRenderers = new();
    readonly List<Material[]> authoredSurfaceMaterials = new();
    MaterialPropertyBlock surfacePropertyBlock;

    bool IsSurfaceTintMode => useSurfaceTint;

    public PPEInspectionState InspectionState => inspectionState;
    public GameObject[] CleanVisuals => cleanVisuals;
    public GameObject[] ContaminatedVisuals => contaminatedVisuals;
    public bool UseSurfaceTint => useSurfaceTint;

    void OnEnable()
    {
        if (inspectionState == null ||
            (!IsSurfaceTintMode &&
             (cleanVisuals == null || cleanVisuals.Length == 0) &&
             (contaminatedVisuals == null || contaminatedVisuals.Length == 0)))
        {
            Debug.LogError(
                "PPEConditionVisualAppearance requires a serialized state and at least one visual group.",
                this);
            enabled = false;
            return;
        }

        inspectionState.ConditionChanged += ApplyCondition;
        if (IsSurfaceTintMode)
            CacheSurfaceRenderers();
        ApplyCondition(inspectionState.CurrentCondition);
    }

    void OnDisable()
    {
        if (inspectionState != null)
            inspectionState.ConditionChanged -= ApplyCondition;

        if (IsSurfaceTintMode)
            ClearSurfaceTint();
    }

    void ApplyCondition(PPEItemCondition condition)
    {
        if (IsSurfaceTintMode)
        {
            // The old generated defect roots are deliberately kept out of the
            // state path. They were authored as floating overlays, not as part
            // of the boot surface.
            SetActive(cleanVisuals, false);
            SetActive(contaminatedVisuals, false);
            ApplySurfaceTint(condition == PPEItemCondition.Contaminated);
            return;
        }

        SetActive(cleanVisuals, condition == PPEItemCondition.Clean);
        SetActive(contaminatedVisuals, condition == PPEItemCondition.Contaminated);
    }

    void CacheSurfaceRenderers()
    {
        surfaceRenderers.Clear();
        authoredSurfaceMaterials.Clear();
        foreach (Renderer candidate in GetComponentsInChildren<Renderer>(true))
        {
            if (candidate == null || IsGeneratedOrInteractionVisual(candidate.transform))
                continue;

            if (!surfaceRenderers.Contains(candidate))
            {
                surfaceRenderers.Add(candidate);
                authoredSurfaceMaterials.Add(candidate.sharedMaterials);
            }
        }
    }

    void ApplySurfaceTint(bool contaminated)
    {
        if (!contaminated)
        {
            ClearSurfaceTint();
            return;
        }

        surfacePropertyBlock ??= new MaterialPropertyBlock();
        foreach (Renderer targetRenderer in surfaceRenderers)
        {
            if (targetRenderer == null)
                continue;

            if (surfaceContaminatedMaterial != null)
            {
                targetRenderer.sharedMaterial = surfaceContaminatedMaterial;
                continue;
            }

            targetRenderer.GetPropertyBlock(surfacePropertyBlock);
            if (targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                surfacePropertyBlock.SetColor("_BaseColor", surfaceContaminatedColor);
            }
            else if (targetRenderer.sharedMaterial != null &&
                     targetRenderer.sharedMaterial.HasProperty("_Color"))
            {
                surfacePropertyBlock.SetColor("_Color", surfaceContaminatedColor);
            }

            targetRenderer.SetPropertyBlock(surfacePropertyBlock);
            surfacePropertyBlock.Clear();
        }
    }

    void ClearSurfaceTint()
    {
        for (int index = 0; index < surfaceRenderers.Count; index++)
        {
            Renderer targetRenderer = surfaceRenderers[index];
            if (targetRenderer == null)
                continue;

            if (index < authoredSurfaceMaterials.Count)
                targetRenderer.sharedMaterials = authoredSurfaceMaterials[index];

            targetRenderer.SetPropertyBlock(null);
        }
    }

    static bool IsGeneratedOrInteractionVisual(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            string name = current.name;
            if (name == "PPE_Defect_Visuals" ||
                name == "Mask_Crack_Visual" ||
                name.Contains("XR Item Marker") ||
                name.Contains("Action Panel") ||
                name.Contains("Panel Pose") ||
                name.Contains("Canvas") ||
                name.Contains("equipped_placeholder"))
            {
                return true;
            }
        }

        return false;
    }

    static void SetActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;

        foreach (GameObject target in targets)
        {
            if (target != null)
                target.SetActive(active);
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEInspectionState configuredInspectionState,
        GameObject[] configuredCleanVisuals,
        GameObject[] configuredContaminatedVisuals)
    {
        inspectionState = configuredInspectionState;
        cleanVisuals = configuredCleanVisuals;
        contaminatedVisuals = configuredContaminatedVisuals;
    }

    public void ConfigureSurfaceTintForEditor(
        bool configuredUseSurfaceTint,
        Color configuredContaminatedColor)
    {
        useSurfaceTint = configuredUseSurfaceTint;
        surfaceContaminatedColor = configuredContaminatedColor;
        surfaceContaminatedMaterial = null;
    }
#endif
}
