using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PPEHazmatRoleBindingSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string InspectionPath = "PPE/hazmat_suit_off";
    const string EquippedPath =
        "XR Origin (VR)/Camera Offset/PPE Body Anchor/hazmat_suit_on";
    const string InspectionMaterialPath =
        "Assets/Materials/PPE/HazmatSuit_Inspection.mat";
    const string ContaminationShaderPath =
        "Assets/Shaders/PPEHazmatContamination.shader";
    const string ContaminationMaskPath =
        "Assets/Textures/PPE/HazmatSuit_ChemicalStainMask.png";

    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 방호복 역할을 연결해야 합니다.");

        Scene scene = RequireActiveScene();
        Transform inspection = RequireTransform(scene, InspectionPath);
        Transform equipped = RequireTransform(scene, EquippedPath);

        Undo.SetCurrentGroupName("Configure hazmat suit role binding");
        int undoGroup = Undo.GetCurrentGroup();

        PPEItemIdentity identity = inspection.GetComponent<PPEItemIdentity>();
        if (identity == null)
        {
            identity = Undo.AddComponent<PPEItemIdentity>(inspection.gameObject);
            identity.ConfigureItemTypeForEditor(PPEItemType.HazmatSuit);
            EditorUtility.SetDirty(identity);
        }
        else if (identity.ItemType != PPEItemType.HazmatSuit)
        {
            throw new InvalidOperationException(
                $"'{InspectionPath}'의 기존 PPE 종류가 HazmatSuit가 아닙니다. 작성값을 자동 변경하지 않습니다.");
        }

        PPEItemPresentationBinding binding =
            inspection.GetComponent<PPEItemPresentationBinding>();
        if (binding == null)
        {
            binding = Undo.AddComponent<PPEItemPresentationBinding>(inspection.gameObject);
            binding.ConfigureForEditor(
                identity,
                inspection.gameObject,
                equipped.gameObject,
                PPEItemCondition.Contaminated);
            EditorUtility.SetDirty(binding);
        }

        XRGrabInteractable grabInteractable =
            inspection.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            throw new InvalidOperationException("관찰용 방호복에 XRGrabInteractable이 없습니다.");

        PPEInspectionState inspectionState = inspection.GetComponent<PPEInspectionState>();
        if (inspectionState == null)
        {
            inspectionState = Undo.AddComponent<PPEInspectionState>(inspection.gameObject);
            inspectionState.ConfigureForEditor(binding, grabInteractable);
            EditorUtility.SetDirty(inspectionState);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateScene(scene, true);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

        Selection.activeGameObject = inspection.gameObject;
    }

    public static void Validate()
    {
        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateConditionAppearance()
    {
        ValidateScene(RequireActiveScene(), true);
    }

    public static void SetHazmatContaminated()
    {
        SetHazmatCondition(PPEItemCondition.Contaminated);
    }

    public static void SetHazmatClean()
    {
        SetHazmatCondition(PPEItemCondition.Clean);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        PPEItemPresentationBinding[] bindings = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEItemPresentationBinding>(true))
            .Where(candidate =>
                candidate.ItemIdentity != null &&
                candidate.ItemIdentity.ItemType == PPEItemType.HazmatSuit)
            .ToArray();

        if (bindings.Length != 1)
        {
            throw new InvalidOperationException(
                $"HazmatSuit 역할 연결은 씬에 정확히 1개여야 합니다. 현재 개수: {bindings.Length}");
        }

        PPEItemPresentationBinding binding = bindings[0];
        if (!binding.HasCompleteReferences)
            throw new InvalidOperationException("HazmatSuit의 역할 참조가 완전하지 않습니다.");
        if (binding.InitialCondition != PPEItemCondition.Contaminated)
            throw new InvalidOperationException("관찰용 방호복의 초기 상태는 Contaminated여야 합니다.");

        PPEItemIdentity identity = binding.ItemIdentity;
        Transform inspection = binding.InspectionVisual.transform;
        Transform equipped = binding.EquippedVisual.transform;

        if (binding.gameObject != inspection.gameObject ||
            identity.gameObject != inspection.gameObject)
        {
            throw new InvalidOperationException(
                "HazmatSuit 역할 연결과 PPE 식별자는 관찰용 오브젝트에 있어야 합니다.");
        }

        if (!inspection.gameObject.activeSelf)
            throw new InvalidOperationException("관찰용 hazmat_suit_off의 초기 활성 상태가 꺼져 있습니다.");
        if (!equipped.gameObject.activeSelf)
            throw new InvalidOperationException("장착용 hazmat_suit_on 호스트는 초기 활성 상태여야 합니다.");

        PPEMarkerToggleGrab markerToggleGrab = inspection.GetComponent<PPEMarkerToggleGrab>();
        if (inspection.GetComponent<XRGrabInteractable>() == null ||
            markerToggleGrab == null ||
            !markerToggleGrab.UseToggleGrip ||
            !markerToggleGrab.HoldHandGripWhileSelected)
        {
            throw new InvalidOperationException(
                "관찰용 hazmat_suit_off의 Toggle Grab 또는 손 Grip 유지 구성이 없습니다.");
        }

        PPEInspectionState inspectionState = inspection.GetComponent<PPEInspectionState>();
        if (inspectionState == null ||
            inspectionState.PresentationBinding != binding ||
            inspectionState.GrabInteractable != inspection.GetComponent<XRGrabInteractable>())
        {
            throw new InvalidOperationException("관찰용 방호복의 상태·Grab 참조가 올바르지 않습니다.");
        }

        Renderer[] inspectionRenderers = inspection.GetComponents<Renderer>();
        PPEConditionAppearance conditionAppearance =
            inspection.GetComponent<PPEConditionAppearance>();
        if (conditionAppearance == null ||
            conditionAppearance.InspectionState != inspectionState ||
            inspectionRenderers.Length != 1 ||
            conditionAppearance.TargetRenderers == null ||
            conditionAppearance.TargetRenderers.Length != 1 ||
            conditionAppearance.TargetRenderers[0] != inspectionRenderers[0])
        {
            throw new InvalidOperationException(
                "HazmatSuit condition appearance references are incomplete or ambiguous.");
        }

        if (conditionAppearance.CleanStrength < 0f ||
            conditionAppearance.ContaminatedStrength <= conditionAppearance.CleanStrength ||
            conditionAppearance.ContaminatedStrength > 1f)
        {
            throw new InvalidOperationException(
                "HazmatSuit contamination strength values are invalid.");
        }

        Material expectedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(InspectionMaterialPath);
        Shader expectedShader =
            AssetDatabase.LoadAssetAtPath<Shader>(ContaminationShaderPath);
        Texture expectedMask =
            AssetDatabase.LoadAssetAtPath<Texture>(ContaminationMaskPath);

        if (expectedMaterial == null || expectedShader == null || expectedMask == null)
            throw new InvalidOperationException("HazmatSuit condition appearance assets are missing.");

        if (inspectionRenderers[0].sharedMaterial != expectedMaterial ||
            expectedMaterial.shader != expectedShader ||
            expectedMaterial.GetTexture("_ContaminationMask") != expectedMask)
        {
            throw new InvalidOperationException(
                "HazmatSuit renderer, material, shader, or stain-mask binding is invalid.");
        }

        if (equipped.GetComponent<XRGrabInteractable>() != null ||
            equipped.GetComponent<PPEMarkerToggleGrab>() != null ||
            equipped.GetComponent<PPEItemPresentationBinding>() != null ||
            equipped.GetComponent<PPEInspectionState>() != null ||
            equipped.GetComponent<PPEConditionAppearance>() != null)
        {
            throw new InvalidOperationException("장착용 hazmat_suit_on에 관찰·Grab 역할이 연결돼 있습니다.");
        }

        if (logSuccess)
        {
            Debug.Log(
                $"Hazmat role binding validation passed: " +
                $"inspection={GetPath(inspection)}, equipped={GetPath(equipped)}, " +
                $"condition={binding.InitialCondition}, appearance={expectedMaterial.name}.",
                inspection);
        }
    }

    static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"'{ScenePath}'을 연 상태에서 실행해야 합니다. 현재 씬: '{scene.path}'");
        }

        return scene;
    }

    static void SetHazmatCondition(PPEItemCondition condition)
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException(
                "Hazmat condition preview must be run in Play Mode.");
        }

        Scene scene = RequireActiveScene();
        PPEInspectionState[] inspectionStates = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEInspectionState>(true))
            .Where(state =>
                state.PresentationBinding != null &&
                state.PresentationBinding.ItemIdentity != null &&
                state.PresentationBinding.ItemIdentity.ItemType == PPEItemType.HazmatSuit)
            .ToArray();

        if (inspectionStates.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one HazmatSuit inspection state, found {inspectionStates.Length}.");
        }

        PPEInspectionState inspectionState = inspectionStates[0];
        inspectionState.SetCondition(condition);
        Selection.activeGameObject = inspectionState.gameObject;
        SceneView.RepaintAll();
        Debug.Log($"Hazmat condition preview: {condition}.", inspectionState);
    }

    static Transform RequireTransform(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == parts[0]);
        if (root == null)
            throw new InvalidOperationException($"'{path}'을 찾지 못했습니다.");

        Transform current = root.transform;
        for (int index = 1; index < parts.Length; index++)
        {
            current = current.Find(parts[index]);
            if (current == null)
                throw new InvalidOperationException($"'{path}'을 찾지 못했습니다.");
        }

        return current;
    }

    static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
