using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRControllerAnimatorParameterDriver : MonoBehaviour
{
    [SerializeField] XRNode controllerNode = XRNode.LeftHand;
    [SerializeField] Animator animator;
    [SerializeField] string gripParameter = "Grip";
    [SerializeField] string triggerParameter = "Trigger";
    [SerializeField, Min(0.01f)] float smoothing = 18f;

    InputDevice controller;
    int gripParameterHash;
    int triggerParameterHash;
    float smoothedGrip;
    float smoothedTrigger;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        gripParameterHash = Animator.StringToHash(gripParameter);
        triggerParameterHash = Animator.StringToHash(triggerParameter);
    }

    void Update()
    {
        if (animator == null)
            return;

        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);

        var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedGrip = Mathf.Lerp(smoothedGrip, ReadAxis(CommonUsages.grip), blend);
        smoothedTrigger = Mathf.Lerp(smoothedTrigger, ReadAxis(CommonUsages.trigger), blend);

        if (HasFloatParameter(gripParameterHash))
            animator.SetFloat(gripParameterHash, smoothedGrip);

        if (HasFloatParameter(triggerParameterHash))
            animator.SetFloat(triggerParameterHash, smoothedTrigger);
    }

    float ReadAxis(InputFeatureUsage<float> usage)
    {
        return controller.TryGetFeatureValue(usage, out var value) ? Mathf.Clamp01(value) : 0f;
    }

    bool HasFloatParameter(int parameterHash)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Float)
                return true;
        }

        return false;
    }
}
