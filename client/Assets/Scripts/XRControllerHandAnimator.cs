using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRControllerHandAnimator : MonoBehaviour
{
    [Header("Hand Source")]
    [SerializeField] XRNode controllerNode = XRNode.LeftHand;
    [SerializeField] GameObject handModelPrefab;
    [SerializeField] GameObject controllerVisual;

    [Header("Authored Hand Alignment")]
    [SerializeField] Vector3 localPosition;
    [SerializeField] Vector3 localEulerAngles;
    [SerializeField] Vector3 localScale = Vector3.one;

    [Header("Finger Motion")]
    [SerializeField] RuntimeAnimatorController handAnimatorController;
    [SerializeField] bool useAnimatorClips = true;
    [SerializeField, Range(0f, 120f)] float proximalCurl = 65f;
    [SerializeField, Range(0f, 120f)] float intermediateCurl = 80f;
    [SerializeField, Range(0f, 120f)] float distalCurl = 55f;
    [SerializeField, Range(-1f, 1f)] float curlDirection = -1f;
    [SerializeField, Min(0.01f)] float smoothing = 14f;

    readonly List<HandRig> handRigs = new();
    bool animatorClipsActive;

    InputDevice controller;
    float smoothedTrigger;
    float smoothedGrip;

    void Awake()
    {
        var authoredHands = FindAuthoredHandVisuals();
        if (authoredHands.Count == 0)
        {
            if (controllerVisual != null)
                controllerVisual.SetActive(false);

            if (handModelPrefab == null)
                return;

            // Never instantiate a scene hand back into itself. This can happen
            // when the component is accidentally placed on a hand child and
            // its Hand Model Prefab field is filled with that same scene object,
            // causing recursive clones every frame/domain reload.
            if (handModelPrefab == gameObject)
            {
                Debug.LogError($"[{nameof(XRControllerHandAnimator)}] '{name}' references itself as Hand Model Prefab. Clear that field and keep the Animator on the authored hand root.", this);
                return;
            }

            GameObject hand = Instantiate(handModelPrefab, transform);
            hand.name = controllerNode == XRNode.LeftHand ? "Left Controller Hand Visual" : "Right Controller Hand Visual";
            hand.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEulerAngles));
            hand.transform.localScale = localScale;
            authoredHands.Add(hand);
        }

        foreach (var hand in authoredHands)
        {
            Animator animator = hand.GetComponent<Animator>();
            if (handAnimatorController != null)
            {
                animator ??= hand.AddComponent<Animator>();
                animator.runtimeAnimatorController = handAnimatorController;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorClipsActive = useAnimatorClips;
            }

            var rig = new HandRig(hand.transform);
            CacheFinger(hand.transform, "Index", rig.triggerBones);
            CacheFinger(hand.transform, "Middle", rig.gripBones);
            CacheFinger(hand.transform, "Ring", rig.gripBones);
            CacheFinger(hand.transform, "Little", rig.gripBones);
            CacheFinger(hand.transform, "Thumb", rig.thumbBones);
            // Animator-driven clips do not depend on the procedural bone cache.
            // Keep the rig whenever an Animator exists so the Grip parameter is
            // still forwarded even when a model uses a different bone layout.
            if (rig.Animator != null || rig.HasBones)
                handRigs.Add(rig);

            if (rig.Animator != null && handAnimatorController != null)
            {
                rig.Animator.enabled = true;
                rig.Animator.Rebind();
                rig.Animator.Update(0f);
                if (rig.Animator.parameters.Length > 0 && rig.Animator.HasParameter("Grip"))
                    rig.Animator.SetFloat("Grip", 0f);
            }
        }
    }

    List<GameObject> FindAuthoredHandVisuals()
    {
        var hands = new List<GameObject>();
        if (controllerVisual == null)
            return hands;

        string prefix = controllerNode == XRNode.LeftHand ? "LeftHand_" : "RightHand_";
        foreach (Transform child in controllerVisual.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith(prefix, System.StringComparison.Ordinal) ||
                (!child.name.EndsWith("BareHand", System.StringComparison.Ordinal) &&
                 !child.name.EndsWith("Glove_Suit", System.StringComparison.Ordinal) &&
                 !child.name.EndsWith("Glove_Suit_Tape", System.StringComparison.Ordinal)))
                continue;

            hands.Add(child.gameObject);
        }

        if (hands.Count == 0)
            return hands;

        // Scene-authored hand hierarchy, Transform, and materials are authoritative.
        // Keep the controller visuals active and animate every authored variant in place,
        // including inactive variants that may be switched on later.
        controllerVisual.SetActive(true);
        return hands;
    }

    void Update()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);

        var trigger = ReadAxis(CommonUsages.trigger);
        var grip = ReadAxis(CommonUsages.grip);
        var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedTrigger = Mathf.Lerp(smoothedTrigger, trigger, blend);
        smoothedGrip = Mathf.Lerp(smoothedGrip, grip, blend);

        if (animatorClipsActive)
        {
            foreach (var rig in handRigs)
                rig.Animator?.SetFloat("Grip", smoothedGrip);
        }
    }

    void LateUpdate()
    {
        if (animatorClipsActive)
            return;

        foreach (var rig in handRigs)
        {
            ApplyCurl(rig.triggerBones, smoothedTrigger);
            ApplyCurl(rig.gripBones, smoothedGrip);
            ApplyCurl(rig.thumbBones, smoothedGrip * 0.55f);
        }
    }

    float ReadAxis(InputFeatureUsage<float> usage)
    {
        if (controller.TryGetFeatureValue(usage, out var value))
            return Mathf.Clamp01(value);

        // Some OpenXR controller profiles expose only the digital button feature
        // even though the XRI action map presents the control as a float.
        var buttonUsage = usage == CommonUsages.grip
            ? CommonUsages.gripButton
            : CommonUsages.triggerButton;
        return controller.TryGetFeatureValue(buttonUsage, out var pressed) && pressed ? 1f : 0f;
    }

    void CacheFinger(Transform root, string fingerName, List<FingerBone> destination)
    {
        var palm = FindChild(root, "Palm");
        CacheBone(root, palm, fingerName + "Metacarpal", destination, proximalCurl * 0.35f);
        CacheBone(root, palm, fingerName + "Proximal", destination, proximalCurl);
        CacheBone(root, palm, fingerName + "Intermediate", destination, intermediateCurl);
        CacheBone(root, palm, fingerName + "Distal", destination, distalCurl);
    }

    void CacheBone(Transform root, Transform palm, string boneName, List<FingerBone> destination, float angle)
    {
        var bone = FindChild(root, boneName);
        if (bone == null || bone.childCount == 0 || palm == null)
            return;

        var segmentDirection = (bone.GetChild(0).position - bone.position).normalized;
        var towardPalm = Vector3.ProjectOnPlane(palm.position - bone.position, segmentDirection).normalized;
        var bendAxisWorld = Vector3.Cross(segmentDirection, towardPalm).normalized;
        if (bendAxisWorld.sqrMagnitude < 0.5f)
            return;

        var bendAxisLocal = bone.InverseTransformDirection(bendAxisWorld).normalized;
        destination.Add(new FingerBone(bone, bone.localRotation, bendAxisLocal, angle));
    }

    void ApplyCurl(List<FingerBone> bones, float amount)
    {
        foreach (var bone in bones)
            bone.Transform.localRotation = bone.AuthoredRotation * Quaternion.AngleAxis(bone.Angle * curlDirection * amount, bone.BendAxis);
    }

    static Transform FindChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            var match = FindChild(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    readonly struct FingerBone
    {
        public readonly Transform Transform;
        public readonly Quaternion AuthoredRotation;
        public readonly Vector3 BendAxis;
        public readonly float Angle;

        public FingerBone(Transform transform, Quaternion authoredRotation, Vector3 bendAxis, float angle)
        {
            Transform = transform;
            AuthoredRotation = authoredRotation;
            BendAxis = bendAxis;
            Angle = angle;
        }
    }

    sealed class HandRig
    {
        public readonly List<FingerBone> triggerBones = new();
        public readonly List<FingerBone> gripBones = new();
        public readonly List<FingerBone> thumbBones = new();

        public HandRig(Transform root) { Root = root; Animator = root.GetComponent<Animator>(); }
        public Transform Root { get; }
        public Animator Animator { get; }
        public bool HasBones => triggerBones.Count + gripBones.Count + thumbBones.Count > 0;
    }
}

static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string parameterName)
    {
        foreach (var parameter in animator.parameters)
            if (parameter.name == parameterName)
                return true;
        return false;
    }
}
