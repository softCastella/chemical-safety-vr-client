using UnityEditor;
using UnityEngine;

public static class ChemicalMixerInteriorMaker
{
    [MenuItem("Tools/Environment/Create Chemical Mixer Interior")]
    public static void Create()
    {
        var root = new GameObject("ChemicalMixerInterior");
        Undo.RegisterCreatedObjectUndo(root, "Create Chemical Mixer Interior");

        root.transform.position = GetViewerPosition();

        root.AddComponent<ChemicalMixerInterior>();
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.SetDirty(root);

        Debug.Log("Created ChemicalMixerInterior. Move the Scene view inside the vessel to inspect the dome and mixer blades.");
    }

    [MenuItem("Tools/Environment/Center Selected Mixer On Main Camera")]
    public static void CenterSelectedOnMainCamera()
    {
        var mixer = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<ChemicalMixerInterior>()
            : null;

        if (mixer == null)
        {
            EditorUtility.DisplayDialog("Chemical Mixer Interior",
                "Hierarchy에서 ChemicalMixerInterior 오브젝트를 선택하세요.", "OK");
            return;
        }

        Undo.RecordObject(mixer.transform, "Center Mixer On Main Camera");
        mixer.transform.position = GetViewerPosition();
        EditorUtility.SetDirty(mixer.transform);
        SceneView.RepaintAll();
    }

    static Vector3 GetViewerPosition()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
            return mainCamera.transform.position;

        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            return SceneView.lastActiveSceneView.camera.transform.position;

        return Vector3.zero;
    }
}
