using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Poses scene-authored Shadows Only meshes from the tracked head and controller transforms.
/// The component never creates or repairs scene objects at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class XRPlayerShadowProxy : MonoBehaviour
{
    [Header("Tracked Sources")]
    [SerializeField] private Transform m_Head;
    [SerializeField] private Transform m_LeftController;
    [SerializeField] private Transform m_RightController;
    [SerializeField] private Renderer m_FloorRenderer;

    [Header("Authored Shadow Parts")]
    [SerializeField] private Transform m_HeadShadow;
    [SerializeField] private Transform m_TorsoShadow;
    [SerializeField] private Transform m_LeftArmShadow;
    [SerializeField] private Transform m_RightArmShadow;
    [SerializeField] private Transform m_LeftLegShadow;
    [SerializeField] private Transform m_RightLegShadow;

    [Header("Body Proportions")]
    [SerializeField, Min(0.01f)] private float m_HeadDiameter = 0.24f;
    [SerializeField, Min(0.01f)] private float m_ShoulderWidth = 0.42f;
    [SerializeField, Min(0.01f)] private float m_ShoulderDrop = 0.25f;
    [SerializeField, Min(0.01f)] private float m_TorsoRadius = 0.18f;
    [SerializeField, Min(0.01f)] private float m_ArmRadius = 0.065f;
    [SerializeField, Min(0.01f)] private float m_LegRadius = 0.085f;
    [SerializeField, Range(0.2f, 0.8f)] private float m_HipHeightRatio = 0.5f;
    [SerializeField, Min(0f)] private float m_LegSpread = 0.1f;
    [SerializeField, Min(0f)] private float m_FootClearance = 0.08f;

    [Header("Tracking Bounds")]
    [SerializeField, Min(0f)] private float m_MinimumHeadHeight = 0.65f;
    [SerializeField, Min(0f)] private float m_MaximumHeadHeight = 2.8f;
    [SerializeField, Min(0f)] private float m_MinimumControllerDistance = 0.08f;
    [SerializeField, Min(0f)] private float m_MaximumControllerDistance = 2f;

    private Renderer[] m_PartRenderers;
    private bool m_IsConfigured;
    private bool m_HasLoggedConfigurationError;

    public Transform Head => m_Head;
    public Transform LeftController => m_LeftController;
    public Transform RightController => m_RightController;
    public Renderer FloorRenderer => m_FloorRenderer;

    public Transform[] ShadowParts => new[]
    {
        m_HeadShadow,
        m_TorsoShadow,
        m_LeftArmShadow,
        m_RightArmShadow,
        m_LeftLegShadow,
        m_RightLegShadow,
    };

    private void Awake()
    {
        m_IsConfigured = TryCacheAuthoredParts();
        if (!m_IsConfigured)
            SetAllPartsEnabled(false);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (m_PartRenderers == null)
            m_IsConfigured = TryCacheAuthoredParts();
    }

    private void LateUpdate()
    {
        if (!m_IsConfigured)
            return;

        UpdateShadowPose();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            SetAllPartsEnabled(false);
    }

    private bool TryCacheAuthoredParts()
    {
        if (m_Head == null ||
            m_LeftController == null ||
            m_RightController == null ||
            m_FloorRenderer == null ||
            m_HeadShadow == null ||
            m_TorsoShadow == null ||
            m_LeftArmShadow == null ||
            m_RightArmShadow == null ||
            m_LeftLegShadow == null ||
            m_RightLegShadow == null)
        {
            LogConfigurationErrorOnce(
                "requires authored head, controller, floor, and six shadow-part references.");
            return false;
        }

        Transform[] parts = ShadowParts;
        m_PartRenderers = new Renderer[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            Renderer partRenderer = parts[index].GetComponent<Renderer>();
            if (partRenderer == null ||
                partRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
            {
                LogConfigurationErrorOnce(
                    $"requires '{parts[index].name}' to have a Shadows Only Renderer.");
                return false;
            }

            m_PartRenderers[index] = partRenderer;
        }

        return true;
    }

    private void UpdateShadowPose()
    {
        float floorY = m_FloorRenderer.bounds.max.y;
        Vector3 headPosition = m_Head.position;
        float trackedHeight = headPosition.y - floorY;
        if (trackedHeight < m_MinimumHeadHeight || trackedHeight > m_MaximumHeadHeight)
        {
            SetAllPartsEnabled(false);
            return;
        }

        Vector3 horizontalForward = Vector3.ProjectOnPlane(m_Head.forward, Vector3.up);
        if (horizontalForward.sqrMagnitude < 0.0001f)
            horizontalForward = Vector3.forward;
        else
            horizontalForward.Normalize();

        Vector3 horizontalRight = Vector3.Cross(Vector3.up, horizontalForward).normalized;
        float shoulderY = Mathf.Max(floorY + trackedHeight * 0.58f, headPosition.y - m_ShoulderDrop);
        float hipY = floorY + trackedHeight * m_HipHeightRatio;
        Vector3 shoulderCenter = new(headPosition.x, shoulderY, headPosition.z);
        Vector3 hipCenter = new(headPosition.x, hipY, headPosition.z);

        SetRendererEnabled(m_HeadShadow, true);
        m_HeadShadow.position = headPosition + Vector3.down * (m_HeadDiameter * 0.12f);
        m_HeadShadow.rotation = Quaternion.identity;
        m_HeadShadow.localScale = Vector3.one * m_HeadDiameter;

        SetCapsuleBetween(m_TorsoShadow, hipCenter, shoulderCenter, m_TorsoRadius);

        Vector3 leftShoulder = shoulderCenter - horizontalRight * (m_ShoulderWidth * 0.5f);
        Vector3 rightShoulder = shoulderCenter + horizontalRight * (m_ShoulderWidth * 0.5f);
        SetTrackedArm(m_LeftArmShadow, leftShoulder, m_LeftController.position, headPosition);
        SetTrackedArm(m_RightArmShadow, rightShoulder, m_RightController.position, headPosition);

        Vector3 leftHip = hipCenter - horizontalRight * m_LegSpread;
        Vector3 rightHip = hipCenter + horizontalRight * m_LegSpread;
        Vector3 leftFoot = new(leftHip.x, floorY + m_FootClearance, leftHip.z);
        Vector3 rightFoot = new(rightHip.x, floorY + m_FootClearance, rightHip.z);
        SetCapsuleBetween(m_LeftLegShadow, leftFoot, leftHip, m_LegRadius);
        SetCapsuleBetween(m_RightLegShadow, rightFoot, rightHip, m_LegRadius);
    }

    private void SetTrackedArm(
        Transform arm,
        Vector3 shoulder,
        Vector3 controllerPosition,
        Vector3 headPosition)
    {
        float controllerDistance = Vector3.Distance(controllerPosition, headPosition);
        bool controllerPoseIsPlausible =
            controllerDistance >= m_MinimumControllerDistance &&
            controllerDistance <= m_MaximumControllerDistance;
        if (!controllerPoseIsPlausible)
        {
            SetRendererEnabled(arm, false);
            return;
        }

        SetCapsuleBetween(arm, shoulder, controllerPosition, m_ArmRadius);
    }

    private static void SetCapsuleBetween(
        Transform capsule,
        Vector3 start,
        Vector3 end,
        float radius)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        if (distance <= radius * 2f)
        {
            SetRendererEnabled(capsule, false);
            return;
        }

        SetRendererEnabled(capsule, true);
        capsule.position = (start + end) * 0.5f;
        capsule.rotation = Quaternion.FromToRotation(Vector3.up, delta / distance);

        // Unity's built-in capsule is two units tall along local Y.
        capsule.localScale = new Vector3(radius * 2f, distance * 0.5f, radius * 2f);
    }

    private void SetAllPartsEnabled(bool enabled)
    {
        if (m_PartRenderers == null)
            return;

        foreach (Renderer partRenderer in m_PartRenderers)
        {
            if (partRenderer != null)
                partRenderer.enabled = enabled;
        }
    }

    private static void SetRendererEnabled(Transform part, bool enabled)
    {
        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
            partRenderer.enabled = enabled;
    }

    private void LogConfigurationErrorOnce(string detail)
    {
        if (m_HasLoggedConfigurationError)
            return;

        Debug.LogError(
            $"{nameof(XRPlayerShadowProxy)} on '{name}' {detail} " +
            "Run Tools/PPE/Configure Room Shadows in Edit Mode and save the scene.",
            this);
        m_HasLoggedConfigurationError = true;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Transform head,
        Transform leftController,
        Transform rightController,
        Renderer floorRenderer,
        Transform headShadow,
        Transform torsoShadow,
        Transform leftArmShadow,
        Transform rightArmShadow,
        Transform leftLegShadow,
        Transform rightLegShadow)
    {
        m_Head = head;
        m_LeftController = leftController;
        m_RightController = rightController;
        m_FloorRenderer = floorRenderer;
        m_HeadShadow = headShadow;
        m_TorsoShadow = torsoShadow;
        m_LeftArmShadow = leftArmShadow;
        m_RightArmShadow = rightArmShadow;
        m_LeftLegShadow = leftLegShadow;
        m_RightLegShadow = rightLegShadow;
    }

    public void UpdatePreviewPoseForEditor()
    {
        if (TryCacheAuthoredParts())
            UpdateShadowPose();
    }
#endif
}
