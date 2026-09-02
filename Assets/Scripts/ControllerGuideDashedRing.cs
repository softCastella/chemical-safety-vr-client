using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws an authored dashed ring for a controller-guide button highlight.
/// The RectTransform owns placement and size; this component only animates
/// the generated vertex alpha and radius while the parent guide is visible.
/// </summary>
[AddComponentMenu("Tyche/UI/Controller Guide Dashed Ring")]
public sealed class ControllerGuideDashedRing : MaskableGraphic
{
    [Header("Dashed Ring")]
    [SerializeField, Min(3)] private int m_DashCount = 12;
    [SerializeField, Range(0.1f, 0.9f)] private float m_DashFill = 0.55f;
    [SerializeField, Min(0.1f)] private float m_Thickness = 0.7f;

    [Header("Pulse")]
    [SerializeField, Min(0.01f)] private float m_PulsesPerSecond = 1.4f;
    [SerializeField, Range(0f, 1f)] private float m_MinimumAlpha = 0.22f;
    [SerializeField, Range(0f, 1f)] private float m_MaximumAlpha = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float m_MinimumRadiusScale = 0.88f;
    [SerializeField, Range(0.5f, 1.5f)] private float m_MaximumRadiusScale = 1f;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float halfExtent = Mathf.Min(rect.width, rect.height) * 0.5f;
        if (halfExtent <= 0f)
            return;

        float pulse = Application.isPlaying
            ? (Mathf.Sin(Time.unscaledTime * m_PulsesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f
            : 1f;
        float radiusScale = Mathf.Lerp(m_MinimumRadiusScale, m_MaximumRadiusScale, pulse);
        float outerRadius = halfExtent * radiusScale;
        float innerRadius = Mathf.Max(0f, outerRadius - m_Thickness);
        int dashCount = Mathf.Max(3, m_DashCount);
        float dashFill = Mathf.Clamp(m_DashFill, 0.1f, 0.9f);
        float segmentAngle = Mathf.PI * 2f / dashCount;
        float dashAngle = segmentAngle * dashFill;
        Color vertexColor = color;
        vertexColor.a *= Mathf.Lerp(m_MinimumAlpha, m_MaximumAlpha, pulse);
        Vector2 center = rect.center;

        const int curveSegmentsPerDash = 2;
        for (int dashIndex = 0; dashIndex < dashCount; dashIndex++)
        {
            float dashStart = dashIndex * segmentAngle + (segmentAngle - dashAngle) * 0.5f;
            for (int segmentIndex = 0; segmentIndex < curveSegmentsPerDash; segmentIndex++)
            {
                float startT = segmentIndex / (float)curveSegmentsPerDash;
                float endT = (segmentIndex + 1f) / curveSegmentsPerDash;
                float startAngle = dashStart + dashAngle * startT;
                float endAngle = dashStart + dashAngle * endT;
                AddRingSegment(
                    vertexHelper,
                    center,
                    innerRadius,
                    outerRadius,
                    startAngle,
                    endAngle,
                    vertexColor);
            }
        }
    }

    private static void AddRingSegment(
        VertexHelper vertexHelper,
        Vector2 center,
        float innerRadius,
        float outerRadius,
        float startAngle,
        float endAngle,
        Color color)
    {
        int firstVertex = vertexHelper.currentVertCount;
        Vector2 startDirection = new(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
        Vector2 endDirection = new(Mathf.Cos(endAngle), Mathf.Sin(endAngle));

        vertexHelper.AddVert(center + startDirection * innerRadius, color, Vector2.zero);
        vertexHelper.AddVert(center + startDirection * outerRadius, color, Vector2.zero);
        vertexHelper.AddVert(center + endDirection * outerRadius, color, Vector2.zero);
        vertexHelper.AddVert(center + endDirection * innerRadius, color, Vector2.zero);
        vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
        vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
    }

    private void Update()
    {
        if (Application.isPlaying)
            SetVerticesDirty();
    }
}
