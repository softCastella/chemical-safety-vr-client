using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public sealed class DoorWindowGlass : MonoBehaviour
{
    [Header("Glass Size")]
    [SerializeField, Min(0.05f)] private float width = 0.493724f;
    [SerializeField, Min(0.05f)] private float height = 0.744283f;

    public void Configure(float newWidth, float newHeight)
    {
        width = newWidth;
        height = newHeight;
        RebuildMesh();
    }

    [ContextMenu("Rebuild Glass Mesh")]
    public void RebuildMesh()
    {
        if (!TryGetComponent(out MeshFilter meshFilter))
            return;

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float cornerX = Mathf.Min(0.042f, halfWidth * 0.45f);
        float cornerY = Mathf.Min(0.021f, halfHeight * 0.45f);
        Vector3[] vertices =
        {
            new(-halfWidth + cornerX, halfHeight, 0f),
            new(halfWidth - cornerX, halfHeight, 0f),
            new(halfWidth, halfHeight - cornerY, 0f),
            new(halfWidth, -halfHeight + cornerY, 0f),
            new(halfWidth - cornerX, -halfHeight, 0f),
            new(-halfWidth + cornerX, -halfHeight, 0f),
            new(-halfWidth, -halfHeight + cornerY, 0f),
            new(-halfWidth, halfHeight - cornerY, 0f),
            Vector3.zero
        };
        int[] triangles =
        {
            8, 0, 1, 8, 1, 2, 8, 2, 3, 8, 3, 4,
            8, 4, 5, 8, 5, 6, 8, 6, 7, 8, 7, 0
        };
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                vertices[i].x / width + 0.5f,
                vertices[i].y / height + 0.5f);
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null || !mesh.name.StartsWith("PPE Door Octagonal Glass"))
        {
            mesh = new() { name = "PPE Door Octagonal Glass" };
            mesh.hideFlags = HideFlags.None;
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(mesh);
            UnityEditor.EditorUtility.SetDirty(meshFilter);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
