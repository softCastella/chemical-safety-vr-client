using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MixerRoomXRSetup
{
    const string ScenePath = "Assets/Scenes/5_MixerRoom.unity";
    const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    const string LeftHandPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/Left Hand Tracking.prefab";
    const string RightHandPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/Right Hand Tracking.prefab";
    static readonly Vector3 DefaultEyePosition = new Vector3(0f, 1f, -10f);

    [MenuItem("Tools/XR/Setup Mixer Room XR Origin and Hands")]
    public static void SetupFromMenu()
    {
        SetupScene();
    }

    // Public entry point used by Unity batch mode as well as the editor menu.
    public static void SetupScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureScene(scene);
    }

    static void ConfigureScene(Scene scene)
    {
        GameObject existingRig = FindInScene(scene, "XR Origin (XR Rig)");
        if (existingRig != null)
        {
            // Once authored, the scene value is authoritative. Re-running this
            // setup must not replace the saved start pose with a tracked pose.
            if (existingRig.GetComponent<XRStartPoseAligner>() == null)
                EnsureStartPoseAligner(existingRig, existingRig.GetComponentInChildren<Camera>(true));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        GameObject rigPrefab = LoadRequiredPrefab(RigPath);
        GameObject leftHandPrefab = LoadRequiredPrefab(LeftHandPath);
        GameObject rightHandPrefab = LoadRequiredPrefab(RightHandPath);
        if (rigPrefab == null || leftHandPrefab == null || rightHandPrefab == null)
            throw new MissingReferenceException("Mixer Room XR setup is missing one or more required prefabs.");

        Camera oldCamera = FindCameraInScene(scene);
        Vector3 targetEyePosition = oldCamera != null
            ? oldCamera.transform.position
            : DefaultEyePosition;
        Vector3 targetForward = oldCamera != null
            ? oldCamera.transform.forward
            : Vector3.forward;

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
        rig.name = "XR Origin (XR Rig)";

        Camera rigCamera = rig.GetComponentInChildren<Camera>(true);
        Transform cameraOffset = FindChildRecursive(rig.transform, "Camera Offset");
        Transform leftController = FindChildRecursive(rig.transform, "Left Controller");
        Transform rightController = FindChildRecursive(rig.transform, "Right Controller");
        if (rigCamera == null || cameraOffset == null || leftController == null || rightController == null)
            throw new MissingReferenceException("The Starter Assets XR Origin prefab hierarchy is incomplete.");

        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        GameObject leftHand = InstantiateHand(leftHandPrefab, cameraOffset, "Left Hand");
        GameObject rightHand = InstantiateHand(rightHandPrefab, cameraOffset, "Right Hand");
        AssignInputModalityObjects(
            rig,
            leftHand,
            rightHand,
            leftController.gameObject,
            rightController.gameObject);
        EnsureStartPoseAligner(rig, rigCamera, targetEyePosition, targetForward);

        if (oldCamera != null && !oldCamera.transform.IsChildOf(rig.transform))
            Object.DestroyImmediate(oldCamera.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new System.InvalidOperationException($"Failed to save scene: {ScenePath}");

        Selection.activeGameObject = rig;
        Debug.Log($"Configured Mixer Room XR at eye position {targetEyePosition}.", rig);
    }

    static void EnsureStartPoseAligner(
        GameObject rig,
        Camera rigCamera,
        Vector3? targetEyePosition = null,
        Vector3? targetForward = null)
    {
        XRStartPoseAligner aligner = rig.GetComponent<XRStartPoseAligner>();
        if (aligner == null)
            aligner = rig.AddComponent<XRStartPoseAligner>();

        Vector3 eyePosition = targetEyePosition
            ?? (rigCamera != null ? rigCamera.transform.position : DefaultEyePosition);
        Vector3 forward = targetForward
            ?? (rigCamera != null ? rigCamera.transform.forward : Vector3.forward);

        aligner.targetEyePosition = eyePosition;
        aligner.targetForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (aligner.targetForward.sqrMagnitude < 0.001f)
            aligner.targetForward = Vector3.forward;
        aligner.settleFrames = 3;
        EditorUtility.SetDirty(aligner);
    }

    static GameObject InstantiateHand(GameObject prefab, Transform parent, string objectName)
    {
        GameObject hand = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        hand.name = objectName;
        hand.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        hand.transform.localScale = Vector3.one;
        return hand;
    }

    static void AssignInputModalityObjects(
        GameObject rig,
        GameObject leftHand,
        GameObject rightHand,
        GameObject leftController,
        GameObject rightController)
    {
        foreach (MonoBehaviour behaviour in rig.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
                continue;

            SerializedObject serialized = new SerializedObject(behaviour);
            SerializedProperty leftHandProperty = serialized.FindProperty("m_LeftHand");
            SerializedProperty rightHandProperty = serialized.FindProperty("m_RightHand");
            SerializedProperty leftControllerProperty = serialized.FindProperty("m_LeftController");
            SerializedProperty rightControllerProperty = serialized.FindProperty("m_RightController");
            if (leftHandProperty == null || rightHandProperty == null ||
                leftControllerProperty == null || rightControllerProperty == null)
                continue;

            leftHandProperty.objectReferenceValue = leftHand;
            rightHandProperty.objectReferenceValue = rightHand;
            leftControllerProperty.objectReferenceValue = leftController;
            rightControllerProperty.objectReferenceValue = rightController;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        throw new MissingComponentException("XR Input Modality Manager was not found on the XR Origin.");
    }

    static GameObject LoadRequiredPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogError($"Required XR prefab was not found: {path}");
        return prefab;
    }

    static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindChildRecursive(root.transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    static Camera FindCameraInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera camera = root.GetComponentInChildren<Camera>(true);
            if (camera != null && camera.CompareTag("MainCamera"))
                return camera;
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform match = FindChildRecursive(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }
}
