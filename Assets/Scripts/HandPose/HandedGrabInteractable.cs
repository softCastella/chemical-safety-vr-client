using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// An <see cref="XRGrabInteractable"/> that picks its attach transform by which hand is grabbing.
///
/// The stock component has one Attach Transform, which is fine when an object is held the same way by
/// either hand. It is not fine once a pose has been captured per hand: the left ghost's wrist and the right
/// ghost's wrist are different places, and a single slot means whichever was captured last wins and the
/// other hand grabs at the wrong spot.
///
/// Falls back to the inherited behaviour whenever the matching side is empty, so an object with only a left
/// pose still works in the right hand - it just grabs by the normal attach point rather than badly.
/// </summary>
[AddComponentMenu("XR/Handed Grab Interactable")]
public class HandedGrabInteractable : XRGrabInteractable
{
    [Header("Per-hand grab points")]
    [SerializeField]
    [Tooltip("Usually the L_Wrist bone of the left ghost hand parented under this object.")]
    Transform m_LeftAttachTransform;

    [SerializeField]
    [Tooltip("Usually the R_Wrist bone of the right ghost hand parented under this object.")]
    Transform m_RightAttachTransform;

    public Transform leftAttachTransform
    {
        get => m_LeftAttachTransform;
        set => m_LeftAttachTransform = value;
    }

    public Transform rightAttachTransform
    {
        get => m_RightAttachTransform;
        set => m_RightAttachTransform = value;
    }

    /// <summary>
    /// Called by the toolkit every time it needs to know where this object meets a given interactor, so the
    /// choice is made per grab rather than being baked in - the same object can be picked up left-handed
    /// now and right-handed a moment later.
    /// </summary>
    public override Transform GetAttachTransform(IXRInteractor interactor)
    {
        if (interactor is XRBaseInteractor baseInteractor)
        {
            switch (baseInteractor.handedness)
            {
                case InteractorHandedness.Left when m_LeftAttachTransform != null:
                    return m_LeftAttachTransform;

                case InteractorHandedness.Right when m_RightAttachTransform != null:
                    return m_RightAttachTransform;
            }
        }

        return base.GetAttachTransform(interactor);
    }
}
