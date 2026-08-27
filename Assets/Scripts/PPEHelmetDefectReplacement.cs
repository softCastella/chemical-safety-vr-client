using System.Collections;
using UnityEngine;

/// <summary>
/// Replaces the defective table helmet with its clean sibling after a valid
/// discard.  The two scene objects deliberately stay as siblings so their
/// authored table poses and interaction hierarchies remain independent.
/// </summary>
[DisallowMultipleComponent]
public sealed class PPEHelmetDefectReplacement : MonoBehaviour
{
    [Header("Scene-authored helmet pair")]
    [SerializeField] PPEActionPanelController defectiveHelmetPanel;
    [SerializeField] GameObject defectiveHelmetRoot;
    [SerializeField] GameObject normalHelmetRoot;

    Coroutine pendingReplacement;

    void OnEnable()
    {
        if (defectiveHelmetPanel == null ||
            defectiveHelmetRoot == null ||
            normalHelmetRoot == null)
        {
            Debug.LogError(
                "PPEHelmetDefectReplacement requires serialized defective panel and both helmet roots.",
                this);
            enabled = false;
            return;
        }

        defectiveHelmetPanel.ChoiceResolved += OnChoiceResolved;
    }

    void OnDisable()
    {
        if (defectiveHelmetPanel != null)
            defectiveHelmetPanel.ChoiceResolved -= OnChoiceResolved;

        if (pendingReplacement != null)
        {
            StopCoroutine(pendingReplacement);
            pendingReplacement = null;
        }
    }

    void OnChoiceResolved(PPEActionChoice choice, PPEActionResult result)
    {
        if (choice != PPEActionChoice.Discard ||
            result != PPEActionResult.DiscardApprovedContaminated ||
            pendingReplacement != null)
        {
            return;
        }

        pendingReplacement = StartCoroutine(ReplaceAfterDiscardFeedback());
    }

    IEnumerator ReplaceAfterDiscardFeedback()
    {
        float delay = defectiveHelmetPanel.ApprovedRemovalDelaySeconds;
        if (delay > 0f)
        {
            if (defectiveHelmetPanel.UseUnscaledTime)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);
        }

        // Let PPEActionPanelController finish its own selection/panel cleanup
        // before the sibling becomes an interactable target.
        yield return null;

        normalHelmetRoot.SetActive(true);
        defectiveHelmetRoot.SetActive(false);
        pendingReplacement = null;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEActionPanelController configuredDefectiveHelmetPanel,
        GameObject configuredDefectiveHelmetRoot,
        GameObject configuredNormalHelmetRoot)
    {
        defectiveHelmetPanel = configuredDefectiveHelmetPanel;
        defectiveHelmetRoot = configuredDefectiveHelmetRoot;
        normalHelmetRoot = configuredNormalHelmetRoot;
    }
#endif
}
