using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PPEPlatformGrabSetup
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest.unity";
    private const string PPERootPath = "interiorObjects/PPE";
    private const string PlatformPath = "interiorObjects/PPE_wall_hanger_set/wooden crate";
    private const float SurfaceClearance = 0.005f;
    private sealed class ItemSpec
    {
        public readonly string ObjectName;
        public readonly PPEItemType ItemType;
        public readonly float InitialScaleMultiplier;

        public bool ShouldResize => InitialScaleMultiplier > 0f;

        public ItemSpec(
            string objectName,
            PPEItemType itemType,
            float initialScaleMultiplier = 0f)
        {
            ObjectName = objectName;
            ItemType = itemType;
            InitialScaleMultiplier = initialScaleMultiplier;
        }
    }

    private static readonly ItemSpec[] Specs =
    {
        new("construction_helmet", PPEItemType.ConstructionHelmet),
        new("orange_tape", PPEItemType.PackingTape, 0.363f),
        new("scuba_gear", PPEItemType.ScubaGear),
        new("tactical_harness", PPEItemType.TacticalHarness),
        new("blue_rubber_gloves_R", PPEItemType.RubberGloveRight, 0.404f),
        new("blue_rubber_gloves_L", PPEItemType.RubberGloveLeft, 0.404f),
        new("rubber_boots_R", PPEItemType.RubberBootRight),
        new("rubber_boots_L", PPEItemType.RubberBootLeft),
        new("gas_mask", PPEItemType.GasMask, 0.300f),
    };

    [MenuItem("Tools/PPE/Configure Platform Grab Items")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 PPE Grab 설정을 적용해야 합니다.");

        Scene scene = RequireScene();
        Transform ppeRoot = RequireTransform(scene, PPERootPath);
        Transform platform = RequireTransform(scene, PlatformPath);
        float platformTopY = CalculateRendererBounds(platform).max.y;

        Undo.SetCurrentGroupName("Configure PPE platform grab items");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (ItemSpec spec in Specs)
        {
            Transform item = ppeRoot.Find(spec.ObjectName);
            if (item == null)
                throw new InvalidOperationException($"'{PPERootPath}/{spec.ObjectName}'을 찾지 못했습니다.");

            PPEItemIdentity identity = item.GetComponent<PPEItemIdentity>();
            bool isFirstSetup = identity == null || !identity.PlatformGrabSetupApplied;
            if (identity == null)
                identity = Undo.AddComponent<PPEItemIdentity>(item.gameObject);

            Undo.RecordObject(identity, "Configure PPE item identity");
            identity.ConfigureForEditor(spec.ItemType);
            EditorUtility.SetDirty(identity);

            if (isFirstSetup && spec.ShouldResize)
            {
                Undo.RecordObject(item, "Resize PPE platform item");
                item.localScale *= spec.InitialScaleMultiplier;
                RestOnPlatform(item, platformTopY);
            }

            ConfigureGrabComponents(item.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateScene(scene, true);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

        Selection.activeTransform = ppeRoot;
        Debug.Log("PPE 단상 Grab 설정 완료: 9종 Grab, 검증 완료 PPE 4개 크기 보정", ppeRoot);
    }

    [MenuItem("Tools/PPE/Validate Platform Grab Items")]
    public static void Validate()
    {
        ValidateScene(RequireScene(), true);
    }

    private static void ConfigureGrabComponents(GameObject item)
    {
        BoxCollider collider = item.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(item);
            FitColliderToRenderers(item.transform, collider);
        }

        Rigidbody rigidbody = item.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(item);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.mass = 0.25f;
            EditorUtility.SetDirty(rigidbody);
        }

        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            grab = Undo.AddComponent<XRGrabInteractable>(item);
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            grab.retainTransformParent = true;
            EditorUtility.SetDirty(grab);
        }
    }

    private static void RestOnPlatform(Transform item, float platformTopY)
    {
        Bounds bounds = CalculateRendererBounds(item);
        float verticalOffset = platformTopY + SurfaceClearance - bounds.min.y;
        item.position += Vector3.up * verticalOffset;
    }

    private static void FitColliderToRenderers(Transform root, BoxCollider collider)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException($"'{GetPath(root)}'에 Collider를 맞출 Renderer가 없습니다.");

        Matrix4x4 worldToLocal = root.worldToLocalMatrix;
        bool initialized = false;
        Bounds localBounds = default;

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 worldCorner = new(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(worldCorner);
                if (!initialized)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        collider.center = localBounds.center;
        collider.size = localBounds.size;
        collider.isTrigger = false;
        EditorUtility.SetDirty(collider);
    }

    private static void ValidateScene(Scene scene, bool logSuccess)
    {
        List<string> failures = new();
        Transform ppeRoot = FindTransform(scene, PPERootPath);
        if (ppeRoot == null)
        {
            failures.Add($"'{PPERootPath}' 루트가 없습니다.");
        }
        else
        {
            foreach (ItemSpec spec in Specs)
            {
                Transform item = ppeRoot.Find(spec.ObjectName);
                if (item == null)
                {
                    failures.Add($"'{spec.ObjectName}' 오브젝트가 없습니다.");
                    continue;
                }

                PPEItemIdentity identity = item.GetComponent<PPEItemIdentity>();
                if (identity == null || identity.ItemType != spec.ItemType)
                    failures.Add($"'{spec.ObjectName}' PPE 식별 정보가 올바르지 않습니다.");
                if (item.GetComponent<BoxCollider>() == null)
                    failures.Add($"'{spec.ObjectName}' BoxCollider가 없습니다.");
                if (item.GetComponent<Rigidbody>() == null)
                    failures.Add($"'{spec.ObjectName}' Rigidbody가 없습니다.");

                XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
                if (grab == null)
                {
                    failures.Add($"'{spec.ObjectName}' XRGrabInteractable이 없습니다.");
                }
                else if (grab.throwOnDetach)
                {
                    failures.Add($"'{spec.ObjectName}' Throw On Detach가 켜져 있습니다.");
                }
            }
        }

        if (failures.Count > 0)
        {
            string message = "PPE 단상 Grab 검증 실패:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
            Debug.Log("PPE 단상 Grab 검증 통과: PPE 9종 Collider/Rigidbody/XR Grab 구성 확인", ppeRoot);
    }

    private static Scene RequireScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"'{ScenePath}'을 연 상태에서 실행해야 합니다. 현재 씬: '{scene.path}'");
        }

        return scene;
    }

    private static Transform RequireTransform(Scene scene, string path)
    {
        Transform found = FindTransform(scene, path);
        if (found == null)
            throw new InvalidOperationException($"'{path}'을 찾지 못했습니다.");
        return found;
    }

    private static Transform FindTransform(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == parts[0]);
        if (root == null)
            return null;

        Transform current = root.transform;
        for (int index = 1; index < parts.Length; index++)
        {
            current = current.Find(parts[index]);
            if (current == null)
                return null;
        }

        return current;
    }

    private static Bounds CalculateRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException($"'{GetPath(root)}'에 Renderer가 없습니다.");

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static string GetPath(Transform transform)
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
