using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SegmentedGradientProgressGraphic : MaskableGraphic
{
    public const float CompleteProgressThreshold = 0.999f;

    [SerializeField, Min(1)] int segmentCount = 18;
    [SerializeField, Min(0f)] float spacing = 0.6f;
    [SerializeField] Gradient segmentGradient = new();
    [SerializeField, Range(0f, 1f)] float currentProgress;

    public int segments
    {
        get => segmentCount;
        set
        {
            int clamped = Mathf.Max(1, value);
            if (segmentCount == clamped)
                return;
            segmentCount = clamped;
            SetVerticesDirty();
        }
    }

    public float segmentSpacing
    {
        get => spacing;
        set
        {
            float clamped = Mathf.Max(0f, value);
            if (Mathf.Approximately(spacing, clamped))
                return;
            spacing = clamped;
            SetVerticesDirty();
        }
    }

    public Gradient gradient
    {
        get => segmentGradient;
        set
        {
            segmentGradient = value ?? new Gradient();
            SetVerticesDirty();
        }
    }

    public float progress
    {
        get => currentProgress;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(currentProgress, clamped))
                return;
            currentProgress = clamped;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (currentProgress <= 0f || segmentCount <= 0)
            return;

        Rect rect = GetPixelAdjustedRect();
        float totalSpacing = spacing * (segmentCount - 1);
        float segmentWidth = (rect.width - totalSpacing) / segmentCount;
        if (segmentWidth <= 0f || rect.height <= 0f)
            return;

        int visibleSegments = CalculateVisibleSegmentCount(currentProgress, segmentCount);
        float x = rect.xMin;
        for (int index = 0; index < visibleSegments; index++)
        {
            float gradientPosition = segmentCount == 1
                ? 0f
                : index / (float)(segmentCount - 1);
            AddSegmentQuad(
                vertexHelper,
                new Rect(x, rect.yMin, segmentWidth, rect.height),
                segmentGradient.Evaluate(gradientPosition) * color);
            x += segmentWidth + spacing;
        }
    }

    public static int CalculateVisibleSegmentCount(float progress, int segments)
    {
        int validSegmentCount = Mathf.Max(1, segments);
        float clampedProgress = Mathf.Clamp01(progress);
        if (clampedProgress >= CompleteProgressThreshold)
            return validSegmentCount;

        return Mathf.Clamp(
            Mathf.FloorToInt(clampedProgress * validSegmentCount),
            0,
            validSegmentCount - 1);
    }

    static void AddSegmentQuad(VertexHelper vertexHelper, Rect rect, Color segmentColor)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = segmentColor;

        int startIndex = vertexHelper.currentVertCount;
        vertex.position = new Vector3(rect.xMin, rect.yMin);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMin, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMax, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMax, rect.yMin);
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
