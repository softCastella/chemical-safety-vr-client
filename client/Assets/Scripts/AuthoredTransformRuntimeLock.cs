using UnityEngine;

[DefaultExecutionOrder(32000)]
[DisallowMultipleComponent]
public sealed class AuthoredTransformRuntimeLock : MonoBehaviour
{
    [SerializeField]
    bool preserveParent = true;

    [SerializeField]
    bool preserveLocalPosition = true;

    [SerializeField]
    bool preserveLocalRotation = true;

    [SerializeField]
    bool preserveLocalScale = true;

    [SerializeField]
    bool logFirstCorrection;

    Transform authoredParent;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    Vector3 authoredLocalScale;
    bool captured;
    bool correctionLogged;

    public bool PreserveParent => preserveParent;
    public bool PreserveLocalPosition => preserveLocalPosition;
    public bool PreserveLocalRotation => preserveLocalRotation;
    public bool PreserveLocalScale => preserveLocalScale;

    void Awake()
    {
        authoredParent = transform.parent;
        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        authoredLocalScale = transform.localScale;
        captured = true;
    }

    void LateUpdate()
    {
        if (!captured)
            return;

        bool corrected = false;
        if (preserveParent && transform.parent != authoredParent)
        {
            transform.SetParent(authoredParent, false);
            corrected = true;
        }

        if (preserveLocalPosition && transform.localPosition != authoredLocalPosition)
        {
            transform.localPosition = authoredLocalPosition;
            corrected = true;
        }

        if (preserveLocalRotation && transform.localRotation != authoredLocalRotation)
        {
            transform.localRotation = authoredLocalRotation;
            corrected = true;
        }

        if (preserveLocalScale && transform.localScale != authoredLocalScale)
        {
            transform.localScale = authoredLocalScale;
            corrected = true;
        }

        if (corrected && logFirstCorrection && !correctionLogged)
        {
            correctionLogged = true;
            Debug.Log(
                $"[Authored Transform Lock] Restored the Play Mode pose captured from '{name}'.",
                this);
        }
    }
}
