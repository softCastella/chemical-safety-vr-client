using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

internal static class Quest2ControllerVisualSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string LeftModelPath = "Assets/Oculus/Core/OculusTouchForQuest2_Left.prefab";
    const string RightModelPath = "Assets/Oculus/Core/OculusTouchForQuest2_Right.prefab";
    const string InputActionsPath =
        "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";

    const string MenuRoot = "Tools/XR/Quest 2 Controller/";

    [MenuItem(MenuRoot + "Install Models In PPE Scene")]
    static void InstallModelsInPpeScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!ValidateSceneContext(scene, out var context))
            return;

        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install Quest 2 Controller Models");

        var changed = false;
        changed |= InstallModel(context.LeftModelParent, context.LeftModel, LeftModelPath);
        changed |= InstallModel(context.RightModelParent, context.RightModel, RightModelPath);

        Undo.CollapseUndoOperations(undoGroup);

        if (!changed)
        {
            Debug.Log("[Quest 2 Controller] Both authored model instances already exist. No scene values were changed.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = context.LeftModelParent.gameObject;
        Debug.Log(
            "[Quest 2 Controller] Installed the left/right Quest 2 model prefabs under the existing Hand Offset objects. " +
            "Existing hand, interaction, teleport, and guide input components were not changed. " +
            "Review the authored transforms in the Scene view, then save the scene manually.");
    }

    [MenuItem(MenuRoot + "Validate PPE Scene Setup")]
    static void ValidatePpeSceneSetup()
    {
        var scene = SceneManager.GetActiveScene();
        if (!ValidateSceneContext(scene, out var context))
            return;

        var errors = new List<string>();
        ValidateInstalledModel(context.LeftModelParent, LeftModelPath, errors);
        ValidateInstalledModel(context.RightModelParent, RightModelPath, errors);

        if (errors.Count > 0)
        {
            Debug.LogError("[Quest 2 Controller] Validation failed:\n- " + string.Join("\n- ", errors));
            return;
        }

        Debug.Log(
            "[Quest 2 Controller] Static scene validation passed: both model instances, tracked pose inputs, " +
            "XRI action manager, meshes, and materials are present. Quest/OpenXR tracking and stereo rendering " +
            "still require headset validation.");
    }

    static bool ValidateSceneContext(Scene scene, out SetupContext context)
    {
        context = default;
        var errors = new List<string>();

        if (!scene.IsValid() || !scene.isLoaded || scene.path != TargetScenePath)
        {
            Debug.LogError(
                $"[Quest 2 Controller] Open the single target scene '{TargetScenePath}' before running this command. " +
                $"Active scene: '{scene.path}'.");
            return false;
        }

        var leftModel = AssetDatabase.LoadAssetAtPath<GameObject>(LeftModelPath);
        var rightModel = AssetDatabase.LoadAssetAtPath<GameObject>(RightModelPath);
        if (leftModel == null)
            errors.Add($"Missing model prefab: {LeftModelPath}");
        if (rightModel == null)
            errors.Add($"Missing model prefab: {RightModelPath}");

        var leftController = FindUniqueSceneObject(scene, "Left Controller", errors);
        var rightController = FindUniqueSceneObject(scene, "Right Controller", errors);
        var leftVisual = FindDirectChild(leftController, "Visual", errors);
        var rightVisual = FindDirectChild(rightController, "Visual", errors);
        var leftModelParent = FindDirectChild(leftVisual, "Hand Offset", errors);
        var rightModelParent = FindDirectChild(rightVisual, "Hand Offset", errors);

        ValidateTrackedPoseDriver(leftController, errors);
        ValidateTrackedPoseDriver(rightController, errors);
        ValidateInputActionManager(scene, errors);

        if (errors.Count > 0)
        {
            Debug.LogError("[Quest 2 Controller] Setup prerequisites failed:\n- " + string.Join("\n- ", errors));
            return false;
        }

        context = new SetupContext(leftModelParent, rightModelParent, leftModel, rightModel);
        return true;
    }

    static GameObject FindUniqueSceneObject(Scene scene, string objectName, ICollection<string> errors)
    {
        var matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name == objectName)
            .Select(transform => transform.gameObject)
            .ToArray();

        if (matches.Length == 1)
            return matches[0];

        errors.Add(matches.Length == 0
            ? $"Scene object not found: {objectName}"
            : $"Expected one scene object named '{objectName}', found {matches.Length}.");
        return null;
    }

    static Transform FindDirectChild(GameObject parent, string childName, ICollection<string> errors)
    {
        if (parent == null)
            return null;

        var matches = parent.transform.Cast<Transform>()
            .Where(transform => transform.name == childName)
            .ToArray();
        if (matches.Length == 1)
            return matches[0];

        errors.Add(matches.Length == 0
            ? $"Direct child not found: {GetPath(parent.transform)}/{childName}"
            : $"Expected one direct child named '{childName}' under {GetPath(parent.transform)}, found {matches.Length}.");
        return null;
    }

    static Transform FindDirectChild(Transform parent, string childName, ICollection<string> errors)
    {
        return FindDirectChild(parent != null ? parent.gameObject : null, childName, errors);
    }

    static void ValidateTrackedPoseDriver(GameObject controller, ICollection<string> errors)
    {
        if (controller == null)
            return;

        var driver = controller.GetComponent<TrackedPoseDriver>();
        if (driver == null)
        {
            errors.Add($"TrackedPoseDriver is missing: {GetPath(controller.transform)}");
            return;
        }

        if (driver.positionInput.action == null)
            errors.Add($"Position input is missing: {GetPath(controller.transform)}");
        if (driver.rotationInput.action == null)
            errors.Add($"Rotation input is missing: {GetPath(controller.transform)}");
        if (!driver.ignoreTrackingState && driver.trackingStateInput.action == null)
            errors.Add($"Tracking State input is missing: {GetPath(controller.transform)}");
    }

    static void ValidateInputActionManager(Scene scene, ICollection<string> errors)
    {
        var expectedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
        if (expectedAsset == null)
        {
            errors.Add($"Input action asset is missing: {InputActionsPath}");
            return;
        }

        var managers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<InputActionManager>(true))
            .ToArray();
        if (!managers.Any(manager => manager.actionAssets.Contains(expectedAsset)))
            errors.Add("No InputActionManager in the target scene enables XRI Default Input Actions.");
    }

    static bool InstallModel(Transform parent, GameObject modelPrefab, string modelPath)
    {
        if (FindPrefabInstance(parent, modelPath) != null)
            return false;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, parent.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(instance, $"Install {modelPrefab.name}");
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        return true;
    }

    static void ValidateInstalledModel(Transform parent, string modelPath, ICollection<string> errors)
    {
        var instance = FindPrefabInstance(parent, modelPath);
        if (instance == null)
        {
            errors.Add($"Model instance is missing under {GetPath(parent)}: {modelPath}");
            return;
        }

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            errors.Add($"No Renderer found: {GetPath(instance.transform)}");
            return;
        }

        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(material => material == null))
                errors.Add($"Missing material on Renderer: {GetPath(renderer.transform)}");
        }
    }

    static GameObject FindPrefabInstance(Transform parent, string prefabPath)
    {
        foreach (Transform child in parent)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
            if (source != null && AssetDatabase.GetAssetPath(source) == prefabPath)
                return child.gameObject;
        }

        return null;
    }

    static string GetPath(Transform transform)
    {
        if (transform == null)
            return "<missing>";

        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    readonly struct SetupContext
    {
        public SetupContext(
            Transform leftModelParent,
            Transform rightModelParent,
            GameObject leftModel,
            GameObject rightModel)
        {
            LeftModelParent = leftModelParent;
            RightModelParent = rightModelParent;
            LeftModel = leftModel;
            RightModel = rightModel;
        }

        public Transform LeftModelParent { get; }
        public Transform RightModelParent { get; }
        public GameObject LeftModel { get; }
        public GameObject RightModel { get; }
    }
}
