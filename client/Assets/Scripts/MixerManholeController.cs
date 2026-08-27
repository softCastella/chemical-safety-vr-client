using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MixerManholeController : MonoBehaviour
{
    [Range(45f, 150f)] public float openAngle = 105f;
    [Min(0.1f)] public float moveDuration = 1.2f;
    public bool startOpen;

    bool isOpen;
    bool isMoving;
    Coroutine movement;

    void Awake()
    {
        isOpen = startOpen;
        transform.localRotation = RotationFor(isOpen);
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        if (isMoving || open == isOpen)
            return;

        isOpen = open;
        if (movement != null)
            StopCoroutine(movement);
        movement = StartCoroutine(AnimateTo(RotationFor(open)));
    }

    IEnumerator AnimateTo(Quaternion target)
    {
        isMoving = true;
        var start = transform.localRotation;
        var elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / moveDuration);
            t = t * t * (3f - 2f * t);
            transform.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        movement = null;
        isMoving = false;
    }

    Quaternion RotationFor(bool open)
    {
        return Quaternion.Euler(0f, open ? -openAngle : 0f, 0f);
    }
}
