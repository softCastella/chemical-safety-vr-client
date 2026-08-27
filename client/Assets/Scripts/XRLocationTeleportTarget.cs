using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class XRLocationTeleportTarget : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [Header("Teleport Destination")]
    [SerializeField] XROrigin xrOrigin;
    [SerializeField] TeleportationProvider teleportationProvider;
    [SerializeField] Transform destination;
    [SerializeField, Min(0f)] float floorOffset = 0.03f;
    [SerializeField] bool preserveCurrentHeight = true;
    [SerializeField] float arrivalForwardOffset = 0.65f;
    [SerializeField] bool alignViewToDestination = true;

    protected override void Awake()
    {
        base.Awake();

        if (destination == null)
            destination = transform;
        if (xrOrigin == null)
            xrOrigin = FindAnyObjectByType<XROrigin>();
        if (teleportationProvider == null)
            teleportationProvider = FindAnyObjectByType<TeleportationProvider>();
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        TryTeleport();
    }

    public void Teleport()
    {
        TryTeleport();
    }

    public bool TryTeleport()
    {
        if (teleportationProvider == null || !TryGetDesiredFeetPosition(out var desiredFeetPosition, out var destinationForward))
        {
            Debug.LogWarning("XR teleport request is missing its provider, origin, camera, or destination.", this);
            return false;
        }

        var originTransform = xrOrigin.transform;
        var up = originTransform.up;
        var shouldAlignView = alignViewToDestination && destinationForward.sqrMagnitude > 0.5f;
        var request = new TeleportRequest
        {
            destinationPosition = desiredFeetPosition,
            destinationRotation = shouldAlignView
                ? Quaternion.LookRotation(destinationForward, up)
                : originTransform.rotation,
            matchOrientation = shouldAlignView
                ? MatchOrientation.TargetUpAndForward
                : MatchOrientation.None,
            requestTime = Time.time,
        };

        return teleportationProvider.QueueTeleportRequest(request);
    }

    bool TryGetDesiredFeetPosition(out Vector3 desiredFeetPosition, out Vector3 destinationForward)
    {
        desiredFeetPosition = default;
        destinationForward = default;

        if (xrOrigin == null || xrOrigin.Camera == null || destination == null)
            return false;

        var originTransform = xrOrigin.transform;
        var up = originTransform.up;
        var cameraPosition = xrOrigin.Camera.transform.position;
        var cameraHeight = Vector3.Dot(cameraPosition - originTransform.position, up);
        var currentFeetPosition = cameraPosition - up * cameraHeight;
        destinationForward = Vector3.ProjectOnPlane(destination.forward, up).normalized;
        desiredFeetPosition = destination.position + up * floorOffset;
        if (destinationForward.sqrMagnitude > 0.5f)
            desiredFeetPosition += destinationForward * arrivalForwardOffset;

        if (preserveCurrentHeight)
            desiredFeetPosition = currentFeetPosition + Vector3.ProjectOnPlane(desiredFeetPosition - currentFeetPosition, up);

        return true;
    }
}
