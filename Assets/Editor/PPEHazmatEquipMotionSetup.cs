using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHazmatEquipMotionSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string BodyAnchorPath = "XR Origin (VR)/PPE Body Anchor";
    const string SuitName = "hazmat_suit_on";
    const string FrontStartName = "Hazmat Suit Front Start Anchor";
    const float FrontOffsetMeters = 0.9f;
    const float AuthoredDurationSeconds = 1.8f;

    [MenuItem("Tools/PPE/Configure Hazmat Front Equip Motion")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before configuring Hazmat equip motion.");

        Scene scene = RequireActiveScene();
        Transform bodyAnchor = RequireTransform(scene, BodyAnchorPath);
        Transform suit = bodyAnchor.Find(SuitName);
        if (suit == null)
            throw new InvalidOperationException($"'{BodyAnchorPath}/{SuitName}' was not found.");

        PPEHazmatEquipController controller = suit.GetComponent<PPEHazmatEquipController>();
        if (controller == null)
            throw new InvalidOperationException("hazmat_suit_on has no PPEHazmatEquipController.");

        Transform authoredParent = suit.parent;
        Vector3 authoredLocalPosition = suit.localPosition;
        Quaternion authoredLocalRotation = suit.localRotation;
        Vector3 authoredLocalScale = suit.localScale;

        Undo.SetCurrentGroupName("Configure Hazmat front equip motion");
        int undoGroup = Undo.GetCurrentGroup();

        Transform frontStart = bodyAnchor.Find(FrontStartName);
        bool createdFrontStart = frontStart == null;
        if (createdFrontStart)
        {
            GameObject frontStartObject = new(FrontStartName);
            Undo.RegisterCreatedObjectUndo(frontStartObject, "Create Hazmat front-start anchor");
            frontStart = frontStartObject.transform;
            frontStart.SetParent(bodyAnchor, false);
            frontStart.localPosition = authoredLocalPosition + Vector3.forward * FrontOffsetMeters;
            frontStart.localRotation = authoredLocalRotation;
            frontStart.localScale = Vector3.one;
        }

        Undo.RecordObject(controller, "Configure Hazmat front-start anchor");
        controller.ConfigureFrontStartForEditor(
            frontStart,
            createdFrontStart ? AuthoredDurationSeconds : controller.AnimationDuration);
        EditorUtility.SetDirty(controller);

        if (suit.parent != authoredParent ||
            suit.localPosition != authoredLocalPosition ||
            suit.localRotation != authoredLocalRotation ||
            suit.localScale != authoredLocalScale)
        {
            throw new InvalidOperationException(
                "hazmat_suit_on final authored Transform changed during motion setup. The scene was not saved.");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        PPEHazmatEquipValidation.Validate();

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        Selection.activeGameObject = frontStart.gameObject;
        Debug.Log(
            "Hazmat front equip motion configured: the final suit Transform was preserved, " +
            "and the animation now starts from an authored body-front anchor.",
            frontStart);
    }

    static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before configuring Hazmat equip motion. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static Transform RequireTransform(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject root = Array.Find(scene.GetRootGameObjects(), candidate => candidate.name == parts[0]);
        if (root == null)
            throw new InvalidOperationException($"Root '{parts[0]}' was not found.");

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
