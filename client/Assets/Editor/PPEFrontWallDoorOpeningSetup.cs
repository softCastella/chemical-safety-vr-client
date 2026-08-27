using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEFrontWallDoorOpeningSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string RoomRootName = "PPE Background Room";
    const string GeneratedRoomName = "Generated Image Room";
    const string FrontWallName = "Front Wall";
    const string DoorGlassPath = "PPE Room Door Image/Door Window Glass";

    [MenuItem("Tools/PPE/Rebuild Front Wall Door Opening (HandTest Scale)")]
    public static void RepairActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before rebuilding the Front Wall opening.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before rebuilding. Current scene: '{scene.path}'.");
        }

        GameObject room = GameObject.Find(RoomRootName);
        if (room == null)
            throw new InvalidOperationException($"Missing '{RoomRootName}'.");

        Transform wall = room.transform.Find($"{GeneratedRoomName}/{FrontWallName}");
        Transform glass = room.transform.Find(DoorGlassPath);
        if (wall == null || glass == null)
            throw new InvalidOperationException("Front Wall or Door Window Glass was not found.");

        MeshFilter wallFilter = wall.GetComponent<MeshFilter>();
        DoorWindowGlass glassSize = glass.GetComponent<DoorWindowGlass>();
        if (wallFilter == null || glassSize == null)
        {
            throw new InvalidOperationException("Front Wall MeshFilter or Door Window Glass component is missing.");
        }

        SerializedObject serializedGlass = new SerializedObject(glassSize);
        float glassWidth = serializedGlass.FindProperty("width").floatValue;
        float glassHeight = serializedGlass.FindProperty("height").floatValue;
        Vector3 localCenter = wall.InverseTransformPoint(glass.position);
        float worldPerLocalX = wall.TransformVector(Vector3.right).magnitude;
        float worldPerLocalY = wall.TransformVector(Vector3.up).magnitude;
        if (worldPerLocalX <= 0.0001f || worldPerLocalY <= 0.0001f)
            throw new InvalidOperationException("Front Wall has an invalid scale.");

        float glassWorldWidth = glass.TransformVector(Vector3.right * glassWidth).magnitude;
        float glassWorldHeight = glass.TransformVector(Vector3.up * glassHeight).magnitude;
        float halfWidth = glassWorldWidth / (2f * worldPerLocalX);
        float halfHeight = glassWorldHeight / (2f * worldPerLocalY);
        if (halfWidth <= 0.001f || halfHeight <= 0.001f ||
            Mathf.Abs(localCenter.x) + halfWidth >= 0.5f ||
            Mathf.Abs(localCenter.y) + halfHeight >= 0.5f)
        {
            throw new InvalidOperationException(
                $"Door glass does not fit inside Front Wall: center={localCenter}, " +
                $"halfSize=({halfWidth}, {halfHeight}).");
        }

        float glassCornerX = Mathf.Min(0.042f, glassWidth * 0.225f);
        float glassCornerY = Mathf.Min(0.021f, glassHeight * 0.225f);
        float cornerX = halfWidth * (glassWidth > 0.0001f ? glassCornerX / (glassWidth * 0.5f) : 0.17f);
        float cornerY = halfHeight * (glassHeight > 0.0001f ? glassCornerY / (glassHeight * 0.5f) : 0.056f);

        Vector2[] inner =
        {
            new(localCenter.x - halfWidth + cornerX, localCenter.y + halfHeight),
            new(localCenter.x + halfWidth - cornerX, localCenter.y + halfHeight),
            new(localCenter.x + halfWidth, localCenter.y + halfHeight - cornerY),
            new(localCenter.x + halfWidth, localCenter.y - halfHeight + cornerY),
            new(localCenter.x + halfWidth - cornerX, localCenter.y - halfHeight),
            new(localCenter.x - halfWidth + cornerX, localCenter.y - halfHeight),
            new(localCenter.x - halfWidth, localCenter.y - halfHeight + cornerY),
            new(localCenter.x - halfWidth, localCenter.y + halfHeight - cornerY)
        };

        Vector3[] vertices =
        {
            new(-0.5f, 0.5f, 0f),
            new(0.5f, 0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(-0.5f, -0.5f, 0f),
            new(inner[0].x, inner[0].y, 0f),
            new(inner[1].x, inner[1].y, 0f),
            new(inner[2].x, inner[2].y, 0f),
            new(inner[3].x, inner[3].y, 0f),
            new(inner[4].x, inner[4].y, 0f),
            new(inner[5].x, inner[5].y, 0f),
            new(inner[6].x, inner[6].y, 0f),
            new(inner[7].x, inner[7].y, 0f)
        };

        int[] triangles =
        {
            0, 1, 5, 0, 5, 4,
            1, 6, 5,
            1, 2, 7, 1, 7, 6,
            2, 8, 7,
            2, 3, 9, 2, 9, 8,
            3, 10, 9,
            3, 0, 4, 3, 4, 10,
            0, 11, 4
        };

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            uvs[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f);

        Mesh wallMesh = wallFilter.sharedMesh;
        if (wallMesh == null)
        {
            wallMesh = new Mesh { name = "PPE Front Wall Octagonal Glass Hole" };
            wallFilter.sharedMesh = wallMesh;
        }

        Undo.RecordObject(wallMesh, "Rebuild Front Wall Door Opening");
        Undo.RecordObject(wallFilter, "Rebuild Front Wall Door Opening");
        wallMesh.Clear();
        wallMesh.name = "PPE Front Wall Octagonal Glass Hole";
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = uvs;
        wallMesh.RecalculateNormals();
        wallMesh.RecalculateBounds();
        EditorUtility.SetDirty(wallMesh);
        EditorUtility.SetDirty(wallFilter);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save scene '{ScenePath}'.");

        Debug.Log(
            $"Front Wall door opening rebuilt from Door Window Glass: center={localCenter}, " +
            $"halfSize=({halfWidth}, {halfHeight}).", wallFilter);
    }
}
