using UnityEditor;
using UnityEngine;

public static class XRLocationMarkerMaker
{
    const string MaterialFolder = "Assets/Materials/XR Markers";
    const string MaterialPath = MaterialFolder + "/Default Location Marker.mat";

    [MenuItem("GameObject/XR/Location Marker", false, 10)]
    static void Create(MenuCommand command)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = "XR Location Marker";
        Undo.RegisterCreatedObjectUndo(marker, "Create XR Location Marker");

        var collider = marker.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        var parent = command.context as GameObject;
        if (parent != null)
        {
            GameObjectUtility.SetParentAndAlign(marker, parent);
            marker.transform.localPosition = Vector3.up * 0.003f;
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            marker.transform.position = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        marker.transform.localScale = Vector3.one;
        marker.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateDefaultMaterial();
        marker.AddComponent<XRLocationMarkerPulse>();

        Selection.activeGameObject = marker;
        EditorGUIUtility.PingObject(marker);
    }

    static Material GetOrCreateDefaultMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "XR Markers");

        var shader = Shader.Find("3D UI Test/XR/Location Marker");
        if (shader == null)
        {
            Debug.LogError("XR Location Marker shader has not finished importing.");
            return null;
        }

        var material = new Material(shader) { name = "Default Location Marker" };
        material.enableInstancing = true;
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }

    static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
