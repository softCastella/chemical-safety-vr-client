using System.Collections.Generic;
using UnityEngine;

public sealed class SciFiCardVisual : MonoBehaviour
{
    [Header("Shape")]
    public Vector2 size = new Vector2(0.72f, 0.28f);
    [Min(0f)] public float cornerRadius = 0.045f;
    [Range(1, 24)] public int cornerSegments = 10;

    [Header("Layer Depth")]
    public float glassDepth = 0f;
    public float edgeDepth = -0.008f;
    public float cornerDepth = -0.014f;
    public float backplateDepth = 0.018f;
    public float shadowDepth = 0.045f;

    [Header("3D Frame")]
    public bool showFrame = true;
    [Min(0.002f)] public float frameWidth = 0.018f;
    [Min(0.002f)] public float frameThickness = 0.018f;
    public float frameDepth = -0.006f;
    public Color frameColor = new Color(0.08f, 0.22f, 0.27f, 1f);

    [Header("Glass")]
    public Color glassColor = new Color(0.17f, 0.76f, 1f, 0.34f);
    [Range(0f, 1f)] public float glassOpacity = 0.34f;

    [Header("Backplate")]
    public bool showBackplate = true;
    public Color backplateColor = new Color(0.025f, 0.055f, 0.08f, 0.58f);
    [Range(0f, 1f)] public float backplateOpacity = 0.58f;
    [Min(0f)] public float backplateExpansion = 0.018f;

    [Header("Edge Glow")]
    public bool showEdgeGlow = true;
    public Color edgeColor = new Color(0.36f, 1f, 0.92f, 0.72f);
    [Range(0f, 1f)] public float edgeOpacity = 0.72f;
    [Min(0f)] public float edgeExpansion = 0.026f;

    [Header("Shadow")]
    public bool showShadow = true;
    public Color shadowColor = new Color(0f, 0.02f, 0.04f, 0.38f);
    [Range(0f, 1f)] public float shadowOpacity = 0.38f;
    public Vector2 shadowOffset = new Vector2(0.025f, -0.026f);
    [Min(1f)] public float shadowScale = 1.12f;

    [Header("Corner Markers")]
    public bool showCornerMarkers = true;
    public Color cornerColor = new Color(0.62f, 1f, 0.92f, 0.9f);
    [Range(0f, 1f)] public float cornerOpacity = 0.9f;
    [Min(0f)] public float cornerLength = 0.075f;
    [Min(0.001f)] public float cornerThickness = 0.006f;
    [Min(0f)] public float cornerInset = 0.012f;

    [Header("Interaction")]
    public bool updateCollider = true;
    [Min(0.001f)] public float colliderDepth = 0.035f;

    [Header("References")]
    public MeshFilter shadowFilter;
    public MeshFilter backplateFilter;
    public MeshFilter glassFilter;
    public MeshFilter edgeFilter;
    public MeshFilter frameFilter;
    public MeshFilter[] cornerMarkerFilters = new MeshFilter[8];
    public MeshRenderer shadowRenderer;
    public MeshRenderer backplateRenderer;
    public MeshRenderer glassRenderer;
    public MeshRenderer edgeRenderer;
    public MeshRenderer frameRenderer;
    public MeshRenderer[] cornerMarkerRenderers = new MeshRenderer[8];
    public BoxCollider interactionCollider;

    public void Apply()
    {
        var safeSize = new Vector2(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y));
        var safeRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(safeSize.x, safeSize.y) * 0.5f);

        ApplyPlate(shadowFilter, shadowRenderer, showShadow, safeSize * shadowScale, safeRadius * shadowScale,
            new Vector3(shadowOffset.x, shadowOffset.y, shadowDepth), shadowColor, shadowOpacity);
        ApplyPlate(backplateFilter, backplateRenderer, showBackplate, safeSize + Vector2.one * backplateExpansion,
            safeRadius + backplateExpansion * 0.5f, new Vector3(0f, 0f, backplateDepth), backplateColor, backplateOpacity);
        ApplyPlate(glassFilter, glassRenderer, true, safeSize, safeRadius, new Vector3(0f, 0f, glassDepth), glassColor, glassOpacity);
        ApplyPlate(edgeFilter, edgeRenderer, showEdgeGlow, safeSize + Vector2.one * edgeExpansion,
            safeRadius + edgeExpansion * 0.5f, new Vector3(0f, 0f, edgeDepth), edgeColor, edgeOpacity);
        ApplyFrame(safeSize, safeRadius);

        ApplyCornerMarkers(safeSize);

        if (updateCollider && interactionCollider != null)
        {
            interactionCollider.size = new Vector3(safeSize.x, safeSize.y, colliderDepth);
            interactionCollider.center = new Vector3(0f, 0f, colliderDepth * 0.5f);
        }
    }

    /// <summary>
    /// Recreates only missing generated meshes. Authored transforms, materials, colors, visibility,
    /// and collider values are left unchanged.
    /// </summary>
    public int RebuildMissingMeshes()
    {
        var safeSize = new Vector2(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y));
        var safeRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(safeSize.x, safeSize.y) * 0.5f);
        var rebuiltCount = 0;

        rebuiltCount += RebuildMissingRoundedMesh(shadowFilter, safeSize * shadowScale, safeRadius * shadowScale);
        rebuiltCount += RebuildMissingRoundedMesh(backplateFilter, safeSize + Vector2.one * backplateExpansion,
            safeRadius + backplateExpansion * 0.5f);
        rebuiltCount += RebuildMissingRoundedMesh(glassFilter, safeSize, safeRadius);
        rebuiltCount += RebuildMissingRoundedMesh(edgeFilter, safeSize + Vector2.one * edgeExpansion,
            safeRadius + edgeExpansion * 0.5f);
        rebuiltCount += RebuildMissingFrameMesh(frameFilter, safeSize, safeRadius,
            Mathf.Min(frameWidth, Mathf.Min(safeSize.x, safeSize.y) * 0.2f), frameThickness);

        var markerData = BuildMarkerData(safeSize);
        if (cornerMarkerFilters != null)
        {
            for (var i = 0; i < cornerMarkerFilters.Length && i < markerData.Length; i++)
                rebuiltCount += RebuildMissingQuadMesh(cornerMarkerFilters[i], markerData[i].size);
        }

        return rebuiltCount;
    }

    [ContextMenu("Rebuild Missing Card Meshes")]
    void RebuildMissingMeshesFromContextMenu()
    {
        var rebuiltCount = RebuildMissingMeshes();
        Debug.Log($"Rebuilt {rebuiltCount} missing mesh(es) for '{name}'.", this);
    }

    void ApplyFrame(Vector2 safeSize, float safeRadius)
    {
        if (frameFilter != null)
        {
            frameFilter.transform.localPosition = new Vector3(0f, 0f, frameDepth);
            BuildRoundedFrameMesh(EnsureMesh(frameFilter, frameFilter.name + "_Mesh"), safeSize,
                safeRadius, Mathf.Min(frameWidth, Mathf.Min(safeSize.x, safeSize.y) * 0.2f), frameThickness);
        }

        if (frameRenderer != null)
        {
            frameRenderer.gameObject.SetActive(showFrame);
            ApplyColor(frameRenderer.sharedMaterial, frameColor, 1f);
        }
    }

    void ApplyPlate(MeshFilter filter, MeshRenderer renderer, bool visible, Vector2 plateSize, float radius,
        Vector3 localPosition, Color color, float opacity)
    {
        if (filter != null)
        {
            filter.transform.localPosition = localPosition;
            BuildRoundedMesh(EnsureMesh(filter, filter.name + "_Mesh"), plateSize, radius);
        }

        if (renderer != null)
        {
            renderer.gameObject.SetActive(visible);
            ApplyColor(renderer.sharedMaterial, color, opacity);
        }
    }

    void ApplyCornerMarkers(Vector2 safeSize)
    {
        if (cornerMarkerFilters == null)
            cornerMarkerFilters = new MeshFilter[0];

        if (cornerMarkerRenderers == null)
            cornerMarkerRenderers = new MeshRenderer[0];

        var markerData = BuildMarkerData(safeSize);

        for (var i = 0; i < cornerMarkerFilters.Length; i++)
        {
            var filter = cornerMarkerFilters[i];
            var renderer = i < cornerMarkerRenderers.Length ? cornerMarkerRenderers[i] : null;
            var hasMarker = i < markerData.Length;

            if (filter != null && hasMarker)
            {
                filter.transform.localPosition = new Vector3(markerData[i].position.x, markerData[i].position.y, cornerDepth);
                BuildQuadMesh(EnsureMesh(filter, filter.name + "_Mesh"), markerData[i].size);
            }

            if (renderer != null)
            {
                renderer.gameObject.SetActive(showCornerMarkers && hasMarker);
                ApplyColor(renderer.sharedMaterial, cornerColor, cornerOpacity);
            }
        }
    }

    Marker[] BuildMarkerData(Vector2 safeSize)
    {
        var halfX = safeSize.x * 0.5f;
        var halfY = safeSize.y * 0.5f;
        var length = Mathf.Min(cornerLength, Mathf.Min(safeSize.x, safeSize.y) * 0.45f);
        var thickness = Mathf.Min(cornerThickness, length);
        var inset = cornerInset;

        return new[]
        {
            new Marker(new Vector2(-halfX + inset + length * 0.5f, halfY - inset), new Vector2(length, thickness)),
            new Marker(new Vector2(-halfX + inset, halfY - inset - length * 0.5f), new Vector2(thickness, length)),
            new Marker(new Vector2(halfX - inset - length * 0.5f, halfY - inset), new Vector2(length, thickness)),
            new Marker(new Vector2(halfX - inset, halfY - inset - length * 0.5f), new Vector2(thickness, length)),
            new Marker(new Vector2(-halfX + inset + length * 0.5f, -halfY + inset), new Vector2(length, thickness)),
            new Marker(new Vector2(-halfX + inset, -halfY + inset + length * 0.5f), new Vector2(thickness, length)),
            new Marker(new Vector2(halfX - inset - length * 0.5f, -halfY + inset), new Vector2(length, thickness)),
            new Marker(new Vector2(halfX - inset, -halfY + inset + length * 0.5f), new Vector2(thickness, length))
        };
    }

    int RebuildMissingRoundedMesh(MeshFilter filter, Vector2 meshSize, float radius)
    {
        if (filter == null || filter.sharedMesh != null)
            return 0;

        BuildRoundedMesh(EnsureMesh(filter, filter.name + "_Mesh"), meshSize, radius);
        return 1;
    }

    int RebuildMissingFrameMesh(MeshFilter filter, Vector2 meshSize, float radius, float width, float thickness)
    {
        if (filter == null || filter.sharedMesh != null)
            return 0;

        BuildRoundedFrameMesh(EnsureMesh(filter, filter.name + "_Mesh"), meshSize, radius, width, thickness);
        return 1;
    }

    static int RebuildMissingQuadMesh(MeshFilter filter, Vector2 meshSize)
    {
        if (filter == null || filter.sharedMesh != null)
            return 0;

        BuildQuadMesh(EnsureMesh(filter, filter.name + "_Mesh"), meshSize);
        return 1;
    }

    static Mesh EnsureMesh(MeshFilter filter, string meshName)
    {
        if (filter.sharedMesh == null)
        {
            filter.sharedMesh = new Mesh
            {
                name = meshName
            };
        }

        return filter.sharedMesh;
    }

    void BuildRoundedMesh(Mesh mesh, Vector2 plateSize, float radius)
    {
        mesh.Clear();

        var halfWidth = plateSize.x * 0.5f;
        var halfHeight = plateSize.y * 0.5f;
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(halfWidth, halfHeight));

        if (radius <= 0.0001f)
        {
            BuildQuadMesh(mesh, plateSize);
            return;
        }

        var boundary = new List<Vector2>();
        AddCorner(boundary, new Vector2(halfWidth - radius, halfHeight - radius), 0f, 90f, radius);
        AddCorner(boundary, new Vector2(-halfWidth + radius, halfHeight - radius), 90f, 180f, radius);
        AddCorner(boundary, new Vector2(-halfWidth + radius, -halfHeight + radius), 180f, 270f, radius);
        AddCorner(boundary, new Vector2(halfWidth - radius, -halfHeight + radius), 270f, 360f, radius);

        var vertices = new Vector3[boundary.Count + 1];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[boundary.Count * 3];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.back;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (var i = 0; i < boundary.Count; i++)
        {
            var point = boundary[i];
            vertices[i + 1] = new Vector3(point.x, point.y, 0f);
            normals[i + 1] = Vector3.back;
            uvs[i + 1] = new Vector2(point.x / plateSize.x + 0.5f, point.y / plateSize.y + 0.5f);
        }

        var triangleIndex = 0;
        for (var i = 0; i < boundary.Count; i++)
        {
            var current = i + 1;
            var next = i == boundary.Count - 1 ? 1 : i + 2;
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void BuildRoundedFrameMesh(Mesh mesh, Vector2 outerSize, float outerRadius, float width, float thickness)
    {
        mesh.Clear();

        var innerSize = new Vector2(Mathf.Max(0.001f, outerSize.x - width * 2f),
            Mathf.Max(0.001f, outerSize.y - width * 2f));
        var innerRadius = Mathf.Max(0f, outerRadius - width);
        var outer = BuildBoundary(outerSize, outerRadius);
        var inner = BuildBoundary(innerSize, innerRadius);
        var count = Mathf.Min(outer.Count, inner.Count);
        var halfDepth = thickness * 0.5f;
        var vertices = new Vector3[count * 4];
        var uvs = new Vector2[vertices.Length];

        for (var i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(outer[i].x, outer[i].y, -halfDepth);
            vertices[count + i] = new Vector3(inner[i].x, inner[i].y, -halfDepth);
            vertices[count * 2 + i] = new Vector3(outer[i].x, outer[i].y, halfDepth);
            vertices[count * 3 + i] = new Vector3(inner[i].x, inner[i].y, halfDepth);
            var u = i / (float)count;
            uvs[i] = new Vector2(u, 1f);
            uvs[count + i] = new Vector2(u, 0f);
            uvs[count * 2 + i] = new Vector2(u, 1f);
            uvs[count * 3 + i] = new Vector2(u, 0f);
        }

        var triangles = new List<int>(count * 24);
        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            AddQuad(triangles, i, next, count + i, count + next);
            AddQuad(triangles, count * 2 + next, count * 2 + i, count * 3 + next, count * 3 + i);
            AddQuad(triangles, count * 2 + i, count * 2 + next, i, next);
            AddQuad(triangles, count + next, count + i, count * 3 + next, count * 3 + i);
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    List<Vector2> BuildBoundary(Vector2 plateSize, float radius)
    {
        var halfWidth = plateSize.x * 0.5f;
        var halfHeight = plateSize.y * 0.5f;
        radius = Mathf.Clamp(radius, 0.0001f, Mathf.Min(halfWidth, halfHeight));
        var boundary = new List<Vector2>();
        AddCorner(boundary, new Vector2(halfWidth - radius, halfHeight - radius), 0f, 90f, radius);
        AddCorner(boundary, new Vector2(-halfWidth + radius, halfHeight - radius), 90f, 180f, radius);
        AddCorner(boundary, new Vector2(-halfWidth + radius, -halfHeight + radius), 180f, 270f, radius);
        AddCorner(boundary, new Vector2(halfWidth - radius, -halfHeight + radius), 270f, 360f, radius);
        return boundary;
    }

    static void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(b);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(d);
    }

    void AddCorner(List<Vector2> points, Vector2 center, float startDegrees, float endDegrees, float radius)
    {
        for (var i = 0; i <= cornerSegments; i++)
        {
            if (points.Count > 0 && i == 0)
                continue;

            var t = i / (float)cornerSegments;
            var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    static void BuildQuadMesh(Mesh mesh, Vector2 quadSize)
    {
        mesh.Clear();

        var halfWidth = quadSize.x * 0.5f;
        var halfHeight = quadSize.y * 0.5f;
        mesh.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f)
        };
        mesh.normals = new[]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateBounds();
    }

    static void ApplyColor(Material material, Color color, float opacity)
    {
        if (material == null)
            return;

        color.a = opacity;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", color);
    }

    readonly struct Marker
    {
        public readonly Vector2 position;
        public readonly Vector2 size;

        public Marker(Vector2 position, Vector2 size)
        {
            this.position = position;
            this.size = size;
        }
    }
}
