using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[DisallowMultipleComponent]
public sealed class ScenarioCardSelectProxy : MonoBehaviour, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ScenarioDetailModal modal;
    [SerializeField, Min(0)] private int scenarioIndex;
    [SerializeField] private GameObject hideAfterSelection;

    [Header("Selection Audio")]
    [SerializeField] private bool fadeOutBgmOnSelection;
    [SerializeField, Min(0f)] private float bgmFadeOutDuration;

    [Header("Top Layer Feedback")]
    [SerializeField] private Graphic interactionOverlay;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color hoverColor;
    [SerializeField] private Color pressedColor;
    [SerializeField] private Color selectedColor;
    [SerializeField, Min(0f)] private float visualTransitionDuration;
    [SerializeField, Min(0f)] private float selectedFeedbackDuration;

    private bool pointerInside;
    private bool selectionPending;
    private Coroutine selectionRoutine;

    private void OnEnable()
    {
        pointerInside = false;
        selectionPending = false;
        SetOverlayColor(normalColor, true);
    }

    private void OnDisable()
    {
        if (selectionRoutine != null)
        {
            StopCoroutine(selectionRoutine);
            selectionRoutine = null;
        }

        pointerInside = false;
        selectionPending = false;
        SetOverlayColor(normalColor, true);
    }

    public void Trigger()
    {
        if (selectionPending || (hideAfterSelection == null && modal == null))
            return;

        selectionPending = true;
        SetOverlayColor(selectedColor, false);

        if (fadeOutBgmOnSelection && AudioManager.Instance != null)
            AudioManager.Instance.FadeOutBgm(bgmFadeOutDuration);

        if (hideAfterSelection != null && interactionOverlay != null
            && selectedFeedbackDuration > 0f)
        {
            selectionRoutine = StartCoroutine(CompleteSelectionAfterFeedback());
            return;
        }

        CompleteSelection();
    }

    private IEnumerator CompleteSelectionAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(selectedFeedbackDuration);
        selectionRoutine = null;
        CompleteSelection();
    }

    private void CompleteSelection()
    {
        if (modal != null)
        {
            // Show() hides HUD/Heading. Keep XR UI Canvas itself active — disabling it
            // breaks TrackedDeviceGraphicRaycaster (null eventCamera KeyNotFoundException).
            ScenarioDetailModal targetModal = modal;
            int index = scenarioIndex;
            selectionPending = false;
            targetModal.Show(index);
            return;
        }

        if (hideAfterSelection != null)
        {
            PPEControllerTeleportModeManager.NotifyScenarioSelected(scenarioIndex);
            if (!WouldDisableTrackedDeviceCanvas(hideAfterSelection))
                hideAfterSelection.SetActive(false);
            else
                Debug.LogWarning(
                    "ScenarioCardSelectProxy skipped hideAfterSelection because it is a " +
                    "TrackedDeviceGraphicRaycaster canvas. Hide selection children instead.",
                    hideAfterSelection);
        }

        selectionPending = false;
    }

    private static bool WouldDisableTrackedDeviceCanvas(GameObject target)
    {
        if (target == null)
            return false;

        return target.GetComponent<TrackedDeviceGraphicRaycaster>() != null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Trigger();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        if (!selectionPending)
            SetOverlayColor(hoverColor, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        if (!selectionPending)
            SetOverlayColor(normalColor, false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!selectionPending)
            SetOverlayColor(pressedColor, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!selectionPending)
            SetOverlayColor(pointerInside ? hoverColor : normalColor, false);
    }

    private void SetOverlayColor(Color color, bool immediate)
    {
        if (interactionOverlay == null)
            return;

        interactionOverlay.CrossFadeColor(color,
            immediate ? 0f : visualTransitionDuration, true, true);
    }
}
