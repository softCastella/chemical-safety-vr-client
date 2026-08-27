using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RoundedRectangleGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float cornerRadius = 32f;
    [SerializeField, Range(2, 16)] private int cornerSegments = 8;

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);

        List<Vector2> perimeter = new(cornerSegments * 4 + 4);
        AddCorner(perimeter, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f);
        AddCorner(perimeter, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f);
        AddCorner(perimeter, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f);
        AddCorner(perimeter, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = rect.center;
        vertexHelper.AddVert(vertex);

        foreach (Vector2 point in perimeter)
        {
            vertex.position = point;
            vertexHelper.AddVert(vertex);
        }

        for (int i = 0; i < perimeter.Count; i++)
            vertexHelper.AddTriangle(0, i + 1, (i + 1) % perimeter.Count + 1);
    }

    private void AddCorner(List<Vector2> points, Vector2 center, float radius,
        float startAngle, float endAngle)
    {
        for (int i = 0; i <= cornerSegments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)cornerSegments) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
