using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PPEMaskInspectionAnimation : MonoBehaviour
{
    [SerializeField] Transform visualRoot;
    [SerializeField] Transform wearAnchor;
    [SerializeField, Min(0.05f)] float duration = 0.9f;
    [SerializeField] AnimationCurve movementCurve;
    [SerializeField] bool parentToWearAnchorOnComplete = true;
    Coroutine activeRoutine;
    Action completion;
    public bool IsPlaying => activeRoutine != null;
    public Transform WearAnchor => wearAnchor;
    public bool TryPlay(Action onComplete)
    {
        if (IsPlaying || visualRoot == null || wearAnchor == null) return false;
        completion = onComplete;
        activeRoutine = StartCoroutine(PlayRoutine());
        return true;
    }
    System.Collections.IEnumerator PlayRoutine()
    {
        Vector3 startPosition = visualRoot.position;
        Quaternion startRotation = visualRoot.rotation;
        Vector3 endPosition = wearAnchor.position;
        Quaternion endRotation = wearAnchor.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = movementCurve != null ? movementCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
            visualRoot.SetPositionAndRotation(Vector3.LerpUnclamped(startPosition, endPosition, eased), Quaternion.SlerpUnclamped(startRotation, endRotation, eased));
            yield return null;
        }
        visualRoot.SetPositionAndRotation(endPosition, endRotation);
        if (parentToWearAnchorOnComplete)
        {
            visualRoot.SetParent(wearAnchor, true);
            visualRoot.SetPositionAndRotation(wearAnchor.position, wearAnchor.rotation);
        }
        activeRoutine = null;
        Action callback = completion;
        completion = null;
        callback?.Invoke();
    }
    void OnDisable()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = null;
        completion = null;
    }
}
