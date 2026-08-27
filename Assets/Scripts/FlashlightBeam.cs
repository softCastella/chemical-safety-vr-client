using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Turns a held light on while the trigger is pulled, and dims it with how far it is pulled.
///
/// The on/off half of this can be done with no script at all by wiring Activated and Deactivated to
/// Light.enabled in the inspector. What needs code is the analog part: the interactable is only told
/// that a trigger was pulled, never how hard, so the amount has to be read back off whoever pulled it.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class FlashlightBeam : MonoBehaviour
{
    [SerializeField] Light m_Light;

    /// <summary>Intensity at a fully pulled trigger. Lower values simply never get there.</summary>
    [SerializeField] float m_MaxIntensity = 3f;

    XRGrabInteractable m_Grab;

    /// <summary>Whoever is currently holding the trigger down, or null. Owns the analog value.</summary>
    XRBaseInputInteractor m_Source;

    void Awake()
    {
        m_Grab = GetComponent<XRGrabInteractable>();

        if (m_Light == null)
            m_Light = GetComponentInChildren<Light>(true);
    }

    void OnEnable()
    {
        m_Grab.activated.AddListener(OnActivated);
        m_Grab.deactivated.AddListener(OnDeactivated);

        // Letting go of the flashlight has to switch it off too. Releasing the grip does not fire
        // Deactivated, so without this the beam would stay lit on a light lying on the floor.
        m_Grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        m_Grab.activated.RemoveListener(OnActivated);
        m_Grab.deactivated.RemoveListener(OnDeactivated);
        m_Grab.selectExited.RemoveListener(OnSelectExited);

        TurnOff();
    }

    void OnActivated(ActivateEventArgs args)
    {
        // The event says a trigger was pulled; the interactor is what knows how far.
        m_Source = args.interactorObject as XRBaseInputInteractor;

        if (m_Light != null)
            m_Light.enabled = true;
    }

    void OnDeactivated(DeactivateEventArgs args) => TurnOff();

    void OnSelectExited(SelectExitEventArgs args) => TurnOff();

    void Update()
    {
        if (m_Source == null || m_Light == null)
            return;

        // Read every frame rather than once on activation - the event fires when the trigger crosses
        // its threshold, which says nothing about where the finger goes afterwards.
        m_Light.intensity = m_MaxIntensity * m_Source.activateInput.ReadValue();
    }

    void TurnOff()
    {
        m_Source = null;

        if (m_Light != null)
            m_Light.enabled = false;
    }
}
