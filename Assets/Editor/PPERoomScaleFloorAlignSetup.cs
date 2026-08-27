using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aligns PPE room Floor tops to XR Origin room-scale floor (world Y = 0)
/// and switches Tracking Origin Mode to Floor. Does not raise XR Origin.
/// </summary>
public static class PPERoomScaleFloorAlignSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string OriginName = "XR Origin (VR)";
    const string RoomRootName = "PPE Background Room";
    const string BodyAnchorPath = "XR Origin (VR)/PPE Body Anchor";

    [MenuItem("Tools/PPE/Align Room Floor To Room-Scale (Scale 0)")]
    public static void Align()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before aligning the room-scale floor.");

        Scene scene = RequireScene();
        GameObject originObject = FindUniqueRoot(scene, OriginName);
        XROrigin origin = originObject.GetComponent<XROrigin>();
        if (origin == null)
            throw new InvalidOperationException($"'{OriginName}' has no XROrigin.");

        Transform roomRoot = FindUniqueRoot(scene, RoomRootName).transform;
        Renderer[] floors = roomRoot.GetComponentsInChildren<Renderer>(true)
            .Where(renderer =>
                renderer.gameObject.name == "Floor"
                || renderer.gameObject.name.StartsWith("Floor (", StringComparison.Ordinal))
            .ToArray();
        if (floors.Length == 0)
            throw new InvalidOperationException("No Floor / Floor (n) renderers under PPE Background Room.");

        float currentTop = floors.Max(renderer => renderer.bounds.max.y);
        float deltaY = -currentTop;
        if (Mathf.Abs(deltaY) < 0.0001f)
            deltaY = 0f;

        Undo.SetCurrentGroupName("Align room floor to room-scale");
        int undoGroup = Undo.GetCurrentGroup();

        SerializedObject originSerialized = new(origin);
        originSerialized.FindProperty("m_RequestedTrackingOriginMode").intValue =
            (int)XROrigin.TrackingOriginMode.Floor;
        originSerialized.FindProperty("m_CameraYOffset").floatValue = 0f;
        originSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(origin);

        XROrigin handOrigin = scene.GetRootGameObjects()
            .Select(root => root.name == "XR Origin (Hand Tracking)" ? root.GetComponent<XROrigin>() : null)
            .FirstOrDefault(candidate => candidate != null);
        if (handOrigin != null)
        {
            SerializedObject handSerialized = new(handOrigin);
            handSerialized.FindProperty("m_RequestedTrackingOriginMode").intValue =
                (int)XROrigin.TrackingOriginMode.Floor;
            handSerialized.FindProperty("m_CameraYOffset").floatValue = 0f;
            handSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(handOrigin);
        }

        if (deltaY != 0f)
        {
            Undo.RecordObject(roomRoot, "Raise PPE Background Room to Floor Y=0");
            Vector3 roomPosition = roomRoot.localPosition;
            roomPosition.y += deltaY;
            roomRoot.localPosition = roomPosition;
            EditorUtility.SetDirty(roomRoot);

            Transform bodyAnchor = RequirePath(scene, BodyAnchorPath);
            Undo.RecordObject(bodyAnchor, "Preserve Body Anchor height vs Floor");
            Vector3 bodyPosition = bodyAnchor.localPosition;
            bodyPosition.y += deltaY;
            bodyAnchor.localPosition = bodyPosition;
            EditorUtility.SetDirty(bodyAnchor);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        float alignedTop = floors.Max(renderer => renderer.bounds.max.y);
        Debug.Log(
            $"[PPE Room-Scale] Aligned Floor top {currentTop:F3} → {alignedTop:F3} " +
            $"(deltaY={deltaY:F3}). Tracking Origin=Floor, CameraYOffset=0, eye clamp removed.",
            roomRoot);
    }

    [MenuItem("Tools/PPE/Validate Room-Scale Floor Align (Scale 0)")]
    public static void Validate()
    {
        Scene scene = RequireScene();
        GameObject originObject = FindUniqueRoot(scene, OriginName);
        XROrigin origin = originObject.GetComponent<XROrigin>();
        if (origin == null)
            throw new InvalidOperationException($"'{OriginName}' has no XROrigin.");

        if (origin.RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor)
            throw new InvalidOperationException("XR Origin (VR) Tracking Origin Mode must be Floor.");

        if (!Mathf.Approximately(origin.CameraYOffset, 0f))
            throw new InvalidOperationException("XR Origin (VR) CameraYOffset must be 0 for room-scale floor.");

        Transform roomRoot = FindUniqueRoot(scene, RoomRootName).transform;
        Renderer[] floors = roomRoot.GetComponentsInChildren<Renderer>(true)
            .Where(renderer =>
                renderer.gameObject.name == "Floor"
                || renderer.gameObject.name.StartsWith("Floor (", StringComparison.Ordinal))
            .ToArray();
        if (floors.Length == 0)
            throw new InvalidOperationException("No Floor renderers found.");

        float top = floors.Max(renderer => renderer.bounds.max.y);
        if (Mathf.Abs(top) > 0.01f)
            throw new InvalidOperationException($"Floor top must be near world Y=0, but is {top:F3}.");

        Debug.Log(
            $"[PPE Room-Scale] Validation passed: Floor top={top:F3}, Tracking=Floor, CameraYOffset=0.",
            origin);
    }

    static Scene RequireScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before running this tool. Current: '{scene.path}'.");
        }

        return scene;
    }

    static GameObject FindUniqueRoot(Scene scene, string name)
    {
        GameObject[] matches = scene.GetRootGameObjects()
            .Where(root => root.name == name)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected one root '{name}', found {matches.Length}.");
        return matches[0];
    }

    static Transform RequirePath(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject root = FindUniqueRoot(scene, parts[0]);
        Transform current = root.transform;
        for (int index = 1; index < parts.Length; index++)
        {
            current = current.Find(parts[index]);
            if (current == null)
                throw new InvalidOperationException($"'{path}' was not found.");
        }

        return current;
    }
}
