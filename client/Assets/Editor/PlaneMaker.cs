using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class PlaneMaker : EditorWindow
{
    const string DefaultMeshFolder = "Assets/Generated/Planes";

    enum PlaneOrientation
    {
        XZFloor,
        XYVertical,
        YZVertical
    }

    string planeName = "GeneratedPlane";
    string meshFolder = DefaultMeshFolder;
    PlaneOrientation orientation = PlaneOrientation.XZFloor;
    Vector2 size = new Vector2(4f, 4f);
    int subdivisionsX = 1;
    int subdivisionsY = 1;
    float cornerRadius;
    int cornerSegments = 8;
    bool centerPivot = true;
    bool createDoubleSided;
    bool placeAtSelection = true;
    Material material;

    [MenuItem("Tools/Geometry/Plane Maker")]
    public static void Open()
    {
        var window = GetWindow<PlaneMaker>("Plane Maker");
        window.minSize = new Vector2(350f, 360f);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Plane Maker", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        planeName = EditorGUILayout.TextField("Name", planeName);

        using (new EditorGUILayout.HorizontalScope())
        {
            meshFolder = EditorGUILayout.TextField("Mesh Folder", meshFolder);
            if (GUILayout.Button("Pick", GUILayout.Width(56f)))
                PickFolder();
        }

        EditorGUILayout.Space(8f);
        orientation = (PlaneOrientation)EditorGUILayout.EnumPopup("Orientation", orientation);
        size = EditorGUILayout.Vector2Field("Size", size);
        cornerRadius = EditorGUILayout.Slider("Corner Radius", cornerRadius, 0f, GetMaxCornerRadius());

        if (cornerRadius > 0f)
        {
            cornerSegments = EditorGUILayout.IntSlider("Corner Segments", cornerSegments, 1, 24);
            EditorGUILayout.HelpBox("Rounded planes use a center fan mesh, so subdivisions are ignored while Corner Radius is above 0.", MessageType.Info);
        }
        else
        {
            subdivisionsX = EditorGUILayout.IntSlider("Subdivisions X", subdivisionsX, 1, 64);
            subdivisionsY = EditorGUILayout.IntSlider("Subdivisions Y", subdivisionsY, 1, 64);
        }

        EditorGUILayout.Space(8f);
        centerPivot = EditorGUILayout.ToggleLeft("Center pivot", centerPivot);
        createDoubleSided = EditorGUILayout.ToggleLeft("Double-sided mesh", createDoubleSided);
        placeAtSelection = EditorGUILayout.ToggleLeft("Place at selected GameObject", placeAtSelection);
        material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Plane", GUILayout.Height(32f)))
                CreatePlane();
        }
    }

    void PickFolder()
    {
        var selected = EditorUtility.OpenFolderPanel("Plane Mesh Folder", "Assets", string.Empty);
        if (string.IsNullOrEmpty(selected))
            return;

        var projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/');
        selected = selected.Replace('\\', '/');

        if (!selected.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Choose a folder inside this Unity project.", "OK");
            return;
        }

        meshFolder = selected.Substring(projectPath.Length + 1);
    }

    bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(planeName)
            && !string.IsNullOrWhiteSpace(meshFolder)
            && meshFolder.StartsWith("Assets")
            && size.x > 0f
            && size.y > 0f;
    }

    void CreatePlane()
    {
        EnsureFolder(meshFolder);

        var mesh = BuildPlaneMesh();
        mesh.name = planeName + "_Mesh";

        var meshPath = AssetDatabase.GenerateUniqueAssetPath($"{meshFolder}/{SanitizeFileName(mesh.name)}.asset");
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();

        var gameObject = new GameObject(planeName);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Plane");

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;

        if (material != null)
            meshRenderer.sharedMaterial = material;

        if (placeAtSelection && Selection.activeTransform != null)
            gameObject.transform.position = Selection.activeTransform.position;

        Selection.activeGameObject = gameObject;
        EditorGUIUtility.PingObject(gameObject);

        Debug.Log($"Created plane GameObject and mesh: {meshPath}");
    }

    Mesh BuildPlaneMesh()
    {
        if (cornerRadius > 0f)
            return BuildRoundedPlaneMesh();

        var xCount = subdivisionsX + 1;
        var yCount = subdivisionsY + 1;
        var vertexCount = xCount * yCount;

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];

        var width = size.x;
        var height = size.y;
        var xOffset = centerPivot ? -width * 0.5f : 0f;
        var yOffset = centerPivot ? -height * 0.5f : 0f;
        var normal = GetNormal();

        for (var y = 0; y < yCount; y++)
        {
            for (var x = 0; x < xCount; x++)
            {
                var u = x / (float)subdivisionsX;
                var v = y / (float)subdivisionsY;
                var px = xOffset + u * width;
                var py = yOffset + v * height;
                var index = y * xCount + x;

                vertices[index] = ToPlanePosition(px, py);
                normals[index] = normal;
                uvs[index] = new Vector2(u, v);
            }
        }

        var triangleMultiplier = createDoubleSided ? 12 : 6;
        var triangles = new int[subdivisionsX * subdivisionsY * triangleMultiplier];
        var triangleIndex = 0;

        for (var y = 0; y < subdivisionsY; y++)
        {
            for (var x = 0; x < subdivisionsX; x++)
            {
                var bottomLeft = y * xCount + x;
                var bottomRight = bottomLeft + 1;
                var topLeft = bottomLeft + xCount;
                var topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = bottomRight;

                if (!createDoubleSided)
                    continue;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topRight;
            }
        }

        var mesh = new Mesh
        {
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh BuildRoundedPlaneMesh()
    {
        var radius = Mathf.Min(cornerRadius, GetMaxCornerRadius());
        var width = size.x;
        var height = size.y;
        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;
        var originOffset = centerPivot ? Vector2.zero : new Vector2(halfWidth, halfHeight);

        var boundary = new System.Collections.Generic.List<Vector2>();
        AddCorner(boundary, new Vector2(halfWidth - radius, halfHeight - radius), 0f, 90f, radius, originOffset);
        AddCorner(boundary, new Vector2(-halfWidth + radius, halfHeight - radius), 90f, 180f, radius, originOffset);
        AddCorner(boundary, new Vector2(-halfWidth + radius, -halfHeight + radius), 180f, 270f, radius, originOffset);
        AddCorner(boundary, new Vector2(halfWidth - radius, -halfHeight + radius), 270f, 360f, radius, originOffset);

        var vertexCount = boundary.Count + 1;
        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var normal = GetNormal();
        var center = centerPivot ? Vector2.zero : new Vector2(halfWidth, halfHeight);

        vertices[0] = ToPlanePosition(center.x, center.y);
        normals[0] = normal;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (var i = 0; i < boundary.Count; i++)
        {
            var point = boundary[i];
            vertices[i + 1] = ToPlanePosition(point.x, point.y);
            normals[i + 1] = normal;
            uvs[i + 1] = new Vector2(point.x / width + (centerPivot ? 0.5f : 0f), point.y / height + (centerPivot ? 0.5f : 0f));
        }

        var triangleMultiplier = createDoubleSided ? 6 : 3;
        var triangles = new int[boundary.Count * triangleMultiplier];
        var triangleIndex = 0;

        for (var i = 0; i < boundary.Count; i++)
        {
            var current = i + 1;
            var next = i == boundary.Count - 1 ? 1 : i + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;

            if (!createDoubleSided)
                continue;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;
        }

        var mesh = new Mesh
        {
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateBounds();
        return mesh;
    }

    void AddCorner(System.Collections.Generic.List<Vector2> points, Vector2 center, float startDegrees, float endDegrees, float radius, Vector2 offset)
    {
        for (var i = 0; i <= cornerSegments; i++)
        {
            if (points.Count > 0 && i == 0)
                continue;

            var t = i / (float)cornerSegments;
            var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
            var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + offset;
            points.Add(point);
        }
    }

    float GetMaxCornerRadius()
    {
        return Mathf.Max(0f, Mathf.Min(size.x, size.y) * 0.5f);
    }

    Vector3 ToPlanePosition(float x, float y)
    {
        switch (orientation)
        {
            case PlaneOrientation.XZFloor:
                return new Vector3(x, 0f, y);
            case PlaneOrientation.XYVertical:
                return new Vector3(x, y, 0f);
            case PlaneOrientation.YZVertical:
                return new Vector3(0f, y, x);
            default:
                return new Vector3(x, 0f, y);
        }
    }

    Vector3 GetNormal()
    {
        switch (orientation)
        {
            case PlaneOrientation.XZFloor:
                return Vector3.up;
            case PlaneOrientation.XYVertical:
                return Vector3.back;
            case PlaneOrientation.YZVertical:
                return Vector3.right;
            default:
                return Vector3.up;
        }
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').Trim('/');
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Trim();
    }
}
