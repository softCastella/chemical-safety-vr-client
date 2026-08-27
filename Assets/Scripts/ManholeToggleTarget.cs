using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public sealed class ManholeToggleTarget : MonoBehaviour
{
    public MixerManholeController controller;

    XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnMouseDown()
    {
        controller?.Toggle();
    }

    public void Toggle()
    {
        controller?.Toggle();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Toggle();
    }
}
