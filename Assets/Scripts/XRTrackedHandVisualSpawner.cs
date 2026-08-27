using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

[DisallowMultipleComponent]
public sealed class XRTrackedHandVisualSpawner : MonoBehaviour
{
    [SerializeField] Handedness handedness = Handedness.Left;
    [SerializeField] GameObject handModelPrefab;
    [SerializeField] Transform fallbackTransform;
    [SerializeField] bool useFallbackWhenTrackingLost = true;
    [SerializeField] string instanceName = "Left PPE Tracked Hand Visual";
    [SerializeField] Vector3 localPosition;
    [SerializeField] Vector3 localEulerAngles;
    [SerializeField] Vector3 localScale = Vector3.one;

    static readonly List<string> MissingJointNames = new();
    GameObject spawnedHand;
    XRHandTrackingEvents handTrackingEvents;
    Renderer[] handRenderers;

    void Awake()
    {
        if (handModelPrefab == null)
            return;

        var hand = Instantiate(handModelPrefab, transform);
        spawnedHand = hand;
        hand.name = instanceName;
        var wasActive = hand.activeSelf;
        hand.SetActive(false);
        hand.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEulerAngles));
        hand.transform.localScale = localScale;

        handTrackingEvents = hand.GetComponent<XRHandTrackingEvents>();
        if (handTrackingEvents == null)
            handTrackingEvents = hand.AddComponent<XRHandTrackingEvents>();

        handTrackingEvents.handedness = handedness;
        handTrackingEvents.updateType = XRHandTrackingEvents.UpdateTypes.Dynamic | XRHandTrackingEvents.UpdateTypes.BeforeRender;

        var wrist = FindJointTransform(hand.transform, XRHandJointID.Wrist.ToString());
        var skeletonDriver = hand.GetComponent<XRHandSkeletonDriver>();
        if (skeletonDriver == null)
            skeletonDriver = hand.AddComponent<XRHandSkeletonDriver>();

        if (skeletonDriver.jointTransformReferences == null)
            skeletonDriver.jointTransformReferences = new List<JointToTransformReference>();

        skeletonDriver.handTrackingEvents = handTrackingEvents;
        skeletonDriver.rootTransform = wrist != null ? wrist : hand.transform;
        MissingJointNames.Clear();
        skeletonDriver.FindJointsFromRoot(MissingJointNames);
        skeletonDriver.InitializeFromSerializedReferences();

        Renderer handRenderer = hand.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (handRenderer == null)
            handRenderer = hand.GetComponentInChildren<MeshRenderer>(true);
        handRenderers = hand.GetComponentsInChildren<Renderer>(true);

        if (handRenderer != null)
        {
            var meshController = hand.GetComponent<XRHandMeshController>();
            if (meshController == null)
                meshController = hand.AddComponent<XRHandMeshController>();

            meshController.handTrackingEvents = handTrackingEvents;
            meshController.handMeshRenderer = handRenderer;
            meshController.showMeshWhenTrackingIsAcquired = true;
            meshController.hideMeshWhenTrackingIsLost = !useFallbackWhenTrackingLost;
        }

        if (MissingJointNames.Count > 0)
            Debug.LogWarning($"{name} could not map XR hand joints: {string.Join(", ", MissingJointNames)}", hand);

        hand.SetActive(wasActive);
        ApplyFallbackIfNeeded();
    }

    void LateUpdate()
    {
        ApplyFallbackIfNeeded();
    }

    void ApplyFallbackIfNeeded()
    {
        if (!useFallbackWhenTrackingLost || spawnedHand == null || fallbackTransform == null)
            return;

        if (handTrackingEvents != null && handTrackingEvents.handIsTracked)
            return;

        spawnedHand.transform.SetPositionAndRotation(
            fallbackTransform.TransformPoint(localPosition),
            fallbackTransform.rotation * Quaternion.Euler(localEulerAngles));
        spawnedHand.transform.localScale = localScale;

        if (handRenderers == null)
            return;

        foreach (var handRenderer in handRenderers)
        {
            if (handRenderer != null)
                handRenderer.enabled = true;
        }
    }

    static Transform FindJointTransform(Transform root, string jointName)
    {
        if (StartsOrEndsWith(root.name, jointName))
            return root;

        foreach (Transform child in root)
        {
            var match = FindJointTransform(child, jointName);
            if (match != null)
                return match;
        }

        return null;
    }

    static bool StartsOrEndsWith(string value, string searchTerm)
    {
        return value.StartsWith(searchTerm, System.StringComparison.InvariantCultureIgnoreCase)
            || value.EndsWith(searchTerm, System.StringComparison.InvariantCultureIgnoreCase);
    }
}
