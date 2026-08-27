using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Builds a minimal scene dedicated to exercising gaze interaction: the Starter Assets rig with the
/// gaze interactor forced on (no trigger zone to walk into) and four targets covering each gaze mode.
///
/// This one REPLACES whatever scene is open. To put the same targets into a lesson scene instead, use
/// Tools > XR > Build Gaze & Focus Lesson, which adds them alongside the focus half of Lesson_06.
/// </summary>
static class GazeTestSceneBuilder
{
    /// <summary>GUID of the Starter Assets "XR Origin (XR Rig)" prefab.</summary>
    const string k_RigPrefabGuid = "f6336ac4ac8b4d34bc5072418cdc62a0";
    const string k_SceneFolder = "Assets/Scenes";
    const string k_ScenePath = "Assets/Scenes/GazeTestScene.unity";

    static readonly Vector3 k_TargetOrigin = new Vector3(0f, 1.5f, 2.5f);

    [MenuItem("Tools/Gaze/Build Gaze Test Scene")]
    static void BuildScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Replace the open scene?",
                "This builds a new scene and saves it over " + k_ScenePath + ".\n\n" +
                "Whatever is open now will be closed. To add gaze targets to the scene you already " +
                "have, cancel and use Tools > XR > Build Gaze & Focus Lesson instead.",
                "Build gaze scene", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var rigPrefab = LoadRigPrefab();
        if (rigPrefab == null)
            return;

        if (!Directory.Exists(k_SceneFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // The rig brings its own XR camera, so the default one would fight with it.
        var defaultCamera = GameObject.Find("Main Camera");
        if (defaultCamera != null)
            Object.DestroyImmediate(defaultCamera);

        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.position = Vector3.zero;
        GazeTargetBuilder.ForceAlwaysOn(rig);

        BuildFloor();
        GazeTargetBuilder.BuildTargets("Gaze Targets", k_TargetOrigin);
        GazeTargetBuilder.BuildTools();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, k_ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GAZE] Built gaze test scene at {k_ScenePath}. " +
            "The gaze interactor is always active here - press Play and look at the spheres.");
    }

    static GameObject LoadRigPrefab()
    {
        var path = AssetDatabase.GUIDToAssetPath(k_RigPrefabGuid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[GAZE] Could not find the XR Origin (XR Rig) prefab. " +
                "Import the XR Interaction Toolkit Starter Assets sample first.");
            return null;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogError($"[GAZE] Could not load the rig prefab at {path}.");

        return prefab;
    }

    static void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(2f, 1f, 2f);
        floor.GetComponent<Renderer>().sharedMaterial =
            GazeTargetBuilder.GetOrCreateMaterial("Mat_GazeFloor", new Color(0.22f, 0.24f, 0.28f));
    }
}
