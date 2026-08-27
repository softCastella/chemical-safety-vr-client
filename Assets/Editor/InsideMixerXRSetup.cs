using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InsideMixerXRSetup
{
    const string ScenePath = "Assets/Scenes/4_InsideMixer.unity";
    const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    const string LeftHandPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/Left Hand Tracking.prefab";
    const string RightHandPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/Right Hand Tracking.prefab";
    const string SessionKey = "InsideMixerXRSetup.AutoRun.v1";

    [InitializeOnLoadMethod]
    static void QueueAutomaticSetup()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += SetupIfInsideMixerIsOpen;
    }

    static void SetupIfInsideMixerIsOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.path == ScenePath && FindInScene(scene, "XR Origin (XR Rig)") == null)
            SetupCurrentScene();
    }

    [MenuItem("Tools/XR/Setup Inside Mixer XR Origin and Hands")]
    public static void SetupFromMenu()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        SetupCurrentScene();
    }

    static void SetupCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        var existingRig = FindInScene(scene, "XR Origin (XR Rig)");
        if (existingRig != null)
        {
            EnsureStartPoseAligner(existingRig);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = existingRig;
            Debug.Log("Updated the existing Inside Mixer XR Origin start alignment.", existingRig);
            return;
        }

        var rigPrefab = LoadRequiredPrefab(RigPath);
        var leftHandPrefab = LoadRequiredPrefab(LeftHandPath);
        var rightHandPrefab = LoadRequiredPrefab(RightHandPath);
        if (rigPrefab == null || leftHandPrefab == null || rightHandPrefab == null)
            return;

        var oldCamera = Camera.main;
        var desiredEyePosition = oldCamera != null
            ? oldCamera.transform.position
            : new Vector3(-0.472f, 1f, -12.14f);
        var desiredYaw = oldCamera != null ? oldCamera.transform.eulerAngles.y : 0f;

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
        Undo.RegisterCreatedObjectUndo(rig, "Create insideMixer XR Origin");

        var rigCamera = rig.GetComponentInChildren<Camera>(true);
        var cameraOffset = FindChildRecursive(rig.transform, "Camera Offset");
        var leftController = FindChildRecursive(rig.transform, "Left Controller");
        var rightController = FindChildRecursive(rig.transform, "Right Controller");
        if (rigCamera == null || cameraOffset == null || leftController == null || rightController == null)
        {
            Debug.LogError("The Starter Assets XR Origin prefab hierarchy is incomplete.", rig);
            Undo.DestroyObjectImmediate(rig);
            return;
        }

        // Preserve the old editor eye point without writing tracked pose directly
        // onto the XR camera. At runtime OpenXR owns the camera's local pose.
        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, desiredYaw, 0f));
        var editorEyeOffset = rigCamera.transform.position - rig.transform.position;
        rig.transform.position = desiredEyePosition - editorEyeOffset;
        EnsureStartPoseAligner(rig);

        var leftHand = InstantiateHand(leftHandPrefab, cameraOffset, "Left Hand");
        var rightHand = InstantiateHand(rightHandPrefab, cameraOffset, "Right Hand");
        AssignInputModalityObjects(rig, leftHand, rightHand, leftController.gameObject, rightController.gameObject);

        if (oldCamera != null && !oldCamera.transform.IsChildOf(rig.transform))
            Undo.DestroyObjectImmediate(oldCamera.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = rig;
        EditorGUIUtility.PingObject(rig);
        Debug.Log("Configured Inside Mixer with XR Origin, Left/Right Controllers, and XR Hands.", rig);
    }

    static void EnsureStartPoseAligner(GameObject rig)
    {
        var aligner = rig.GetComponent<XRStartPoseAligner>();
        if (aligner == null)
            aligner = Undo.AddComponent<XRStartPoseAligner>(rig);

        aligner.targetEyePosition = new Vector3(-0.472f, 1f, -12.14f);
        aligner.targetForward = Vector3.forward;
        aligner.settleFrames = 3;
        EditorUtility.SetDirty(aligner);
    }

    static GameObject InstantiateHand(GameObject prefab, Transform parent, string objectName)
    {
        var hand = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(hand, $"Create {objectName}");
        hand.name = objectName;
        hand.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        hand.transform.localScale = Vector3.one;
        return hand;
    }

    static void AssignInputModalityObjects(GameObject rig, GameObject leftHand, GameObject rightHand,
        GameObject leftController, GameObject rightController)
    {
        foreach (var behaviour in rig.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
                continue;

            var serialized = new SerializedObject(behaviour);
            var leftHandProperty = serialized.FindProperty("m_LeftHand");
            var rightHandProperty = serialized.FindProperty("m_RightHand");
            var leftControllerProperty = serialized.FindProperty("m_LeftController");
            var rightControllerProperty = serialized.FindProperty("m_RightController");
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

        Debug.LogWarning("XR Input Modality Manager was not found on the XR Origin.", rig);
    }

    static GameObject LoadRequiredPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            Debug.LogError($"Required XR prefab was not found: {path}");
        return prefab;
    }

    static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var match = FindChildRecursive(root.transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            var match = FindChildRecursive(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }
}
