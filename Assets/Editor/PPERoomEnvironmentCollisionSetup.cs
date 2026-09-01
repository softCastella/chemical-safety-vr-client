using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class PPERoomEnvironmentCollisionSetup
{
    public const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";
    public const string CollisionRootName = "PPE Environment Collision";
    public const int CollisionLayer = 2;
    public const string PpeMarkerName = "XR Item Marker_small";
    public const int PpeMarkerLayer = 6;
    public const float PrimaryPpeMarkerSelectionRadius = 0.125f;
    public const string InteractiveShelfPath = "interiorObjects/Bg/PPE_B_MetalShelving";
    public const float InteractiveShelfBaseStopHeight = 0.12f;
    public const float InteractiveShelfBaseStopDepth = 0.3f;
    internal static readonly string[] InteractiveShelfBoardPaths =
    {
        InteractiveShelfPath + "/tripo_part_15",
        InteractiveShelfPath + "/tripo_part_4",
        InteractiveShelfPath + "/tripo_part_5",
    };
    public const string InteractiveShelfHelmetOuterPostOwnerPath =
        InteractiveShelfPath + "/tripo_part_6";
    internal static readonly string[] InteractiveShelfHelmetOuterPostRendererPaths =
    {
        InteractiveShelfHelmetOuterPostOwnerPath,
        InteractiveShelfPath + "/tripo_part_17",
        InteractiveShelfPath + "/tripo_part_17 (1)",
    };
    internal static readonly string[] InteractiveShelfOtherVerticalPartPaths =
    {
        InteractiveShelfPath + "/tripo_part_0",
        InteractiveShelfPath + "/tripo_part_2",
        InteractiveShelfPath + "/tripo_part_3",
    };

    public const float CharacterRadius = 0.1f;
    public const float CharacterHeight = 1.7f;
    public const float CharacterSkinWidth = 0.01f;
    public const float CharacterStepOffset = 0.2f;
    public const float CharacterSlopeLimit = 45f;
    private const float MinimumWallThickness = 0.12f;

    internal readonly struct CollisionTarget
    {
        public CollisionTarget(
            string blockerName,
            string targetPath,
            bool useOwnRenderer,
            bool applyMinimumThickness,
            float fixedBottomHeight = 0f,
            float fixedDepth = 0f,
            float frontInset = 0f)
        {
            BlockerName = blockerName;
            TargetPath = targetPath;
            UseOwnRenderer = useOwnRenderer;
            ApplyMinimumThickness = applyMinimumThickness;
            FixedBottomHeight = fixedBottomHeight;
            FixedDepth = fixedDepth;
            FrontInset = frontInset;
        }

        public string BlockerName { get; }
        public string TargetPath { get; }
        public bool UseOwnRenderer { get; }
        public bool ApplyMinimumThickness { get; }
        public float FixedBottomHeight { get; }
        public float FixedDepth { get; }
        public float FrontInset { get; }
    }

    internal static readonly CollisionTarget[] Targets =
    {
        new("Wall - Front", "PPE Room/Room/Front Wall", true, true),
        new("Wall - Left", "PPE Room/Room/Left Wall", true, true),
        new("Wall - Right", "PPE Room/Room/Right Wall", true, true),
        new("Wall - Rear", "PPE Room/Room/Rear Wall", true, true),
        new("Wall - Rear 1", "PPE Room/Room/Rear Wall (1)", true, true),
        new("Wall - Rear 2", "PPE Room/Room/Rear Wall (2)", true, true),
        new("Wall - Rear 3", "PPE Room/Room/Rear Wall (3)", true, true),
        new("Furniture - Yellow Trash Bin 01", "interiorObjects/Bg/PPE_B_YellowTrashBin_01", false, false),
        new("Furniture - Yellow Trash Bin 02", "interiorObjects/Bg/PPE_B_YellowTrashBin_02", false, false),
        new("Furniture - Cleaning Cart", "interiorObjects/Bg/PPE_B_CleaningCart", false, false),
        new("Furniture - Mask Locker", "interiorObjects/Bg/PPE_B_MaskLocker", false, false),
        new("Furniture - Suit Hanger", "interiorObjects/Bg/PPE_B_SuitHanger", false, false),
        new("Furniture - Safety Cabinet", "interiorObjects/Bg/PPE_B_SafetyCabinet", false, false),
        new("Furniture - Metal Locker", "interiorObjects/Bg/PPE_B_MetalLocker", false, false),
        new("Furniture - Metal Locker 1", "interiorObjects/Bg/metal_locker (1)", false, false),
        new("Furniture - Bench", "interiorObjects/Bg/PPE_B_Bench", false, false),
        new("Furniture - Storage Wall Rack", "interiorObjects/Bg/PPE_B_StorageWallRack", false, false),
        new("Furniture - Wooden Crate 02", "interiorObjects/Bg/PPE_B_WallHanger/PPE_B_WoodenCrate_02", false, false),
        new("Furniture - Fire Extinguisher 01", "interiorObjects/Bg/PPE_B_FireExtinguisher_01", false, false),
        new("Furniture - Fire Extinguisher 02", "interiorObjects/Bg/PPE_B_FireExtinguisher_02", false, false),
    };

    private static readonly string[] MovedFurnitureBlockerNames =
    {
        "Furniture - Yellow Trash Bin 01",
        "Furniture - Yellow Trash Bin 02",
        "Furniture - Cleaning Cart",
        "Furniture - Suit Hanger",
        "Furniture - Safety Cabinet",
        "Furniture - Bench",
        "Furniture - Storage Wall Rack",
        "Furniture - Wooden Crate 02",
        "Furniture - Fire Extinguisher 01",
    };

    internal static readonly CollisionTarget InteractiveShelfTarget = new(
        "PPE_B_MetalShelving Base Collider",
        InteractiveShelfPath,
        false,
        false,
        InteractiveShelfBaseStopHeight,
        InteractiveShelfBaseStopDepth);

    [MenuItem("Tools/PPE/Configure Room Environment Collision")]
    public static void Configure()
    {
        Scene scene = RequireTargetScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before configuring room collision.");

        XROrigin xrOrigin = RequireComponentAtPath<XROrigin>(scene, "XR Origin (VR)");
        ConfigureCharacterController(xrOrigin.gameObject);

        GameObject collisionRoot = FindByPath(scene, CollisionRootName);
        if (collisionRoot == null)
        {
            collisionRoot = new GameObject(CollisionRootName);
            Undo.RegisterCreatedObjectUndo(collisionRoot, "Create PPE environment collision root");
            SceneManager.MoveGameObjectToScene(collisionRoot, scene);
        }

        Undo.RecordObject(collisionRoot, "Configure PPE environment collision root");
        collisionRoot.layer = CollisionLayer;
        SetWorldIdentity(collisionRoot.transform);

        ConfigurePpeMarkerSelectionColliders(scene);
        ConfigurePpeMarkerTriggerQueries(scene);
        RemoveObsoleteBlockers(collisionRoot.transform);
        foreach (CollisionTarget target in Targets)
            ConfigureBlocker(scene, collisionRoot.transform, target);
        ConfigureInteractiveShelfCollider(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        PPERoomEnvironmentCollisionValidationHarness.Validate();
    }

    [MenuItem("Tools/PPE/Realign Moved Furniture Colliders")]
    public static void RealignMovedFurnitureColliders()
    {
        Scene scene = RequireTargetScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before realigning furniture colliders.");
        if (scene.isDirty)
        {
            throw new InvalidOperationException(
                "The active PPE scene has unsaved changes. Review or save them before realigning furniture colliders.");
        }

        GameObject collisionRoot = FindByPath(scene, CollisionRootName);
        if (collisionRoot == null)
            throw new InvalidOperationException("PPE environment collision root is missing.");

        CollisionTarget[] targets = Targets
            .Where(target => MovedFurnitureBlockerNames.Contains(target.BlockerName, StringComparer.Ordinal))
            .ToArray();
        if (targets.Length != MovedFurnitureBlockerNames.Length)
        {
            throw new InvalidOperationException(
                $"Expected {MovedFurnitureBlockerNames.Length} approved furniture targets, found {targets.Length}.");
        }

        var entries = new List<(
            CollisionTarget Target,
            Transform Blocker,
            BoxCollider Collider,
            Bounds Expected)>();
        foreach (CollisionTarget target in targets)
        {
            Transform blocker = FindDirectChild(collisionRoot.transform, target.BlockerName);
            if (blocker == null)
                throw new InvalidOperationException($"Authored furniture blocker is missing: {target.BlockerName}");

            BoxCollider[] colliders = blocker.GetComponents<BoxCollider>();
            if (colliders.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one authored BoxCollider on {target.BlockerName}, found {colliders.Length}.");
            }

            if (Quaternion.Angle(blocker.rotation, Quaternion.identity) > 0.001f ||
                Vector3.Distance(blocker.lossyScale, Vector3.one) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"Furniture blocker has an unexpected authored rotation or scale: {target.BlockerName}");
            }

            entries.Add((target, blocker, colliders[0], CalculateTargetBounds(scene, target)));
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Realign moved PPE furniture colliders");
        foreach ((CollisionTarget target, Transform blocker, BoxCollider collider, Bounds expected) in entries)
        {
            Undo.RecordObjects(
                new UnityEngine.Object[] { blocker, collider },
                $"Realign {target.BlockerName}");
            blocker.position = expected.center;
            collider.center = Vector3.zero;
            collider.size = expected.size;
            EditorUtility.SetDirty(blocker);
            EditorUtility.SetDirty(collider);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Physics.SyncTransforms();
        PPERoomEnvironmentCollisionValidationHarness.ValidateFurnitureColliderAlignment();
        Debug.Log(
            $"[PPE Furniture Collision] Realigned {targets.Length} approved furniture colliders to current Renderer Bounds.");
    }

    [MenuItem("Tools/PPE/Apply Primary PPE Marker Selection Radius")]
    public static void ApplyPrimaryPpeMarkerSelectionRadius()
    {
        Scene scene = RequireTargetScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before changing PPE marker selection radii.");
        if (scene.isDirty)
        {
            throw new InvalidOperationException(
                "The active PPE scene has unsaved changes. Review or save them before using the marker radius menu.");
        }

        SphereCollider[] markers = FindPrimaryPpeSelectionMarkers(scene);
        if (markers.Length == 0)
            throw new InvalidOperationException("No primary PPE selection markers were found.");

        foreach (SphereCollider marker in markers)
        {
            Undo.RecordObject(marker, "Reduce primary PPE marker selection radius");
            marker.radius = PrimaryPpeMarkerSelectionRadius;
            marker.isTrigger = true;
            EditorUtility.SetDirty(marker);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(
            $"[PPE Marker Selection] Applied radius {PrimaryPpeMarkerSelectionRadius:F3} to " +
            $"{markers.Length} primary PPE marker colliders without changing marker visuals.");
    }

    [MenuItem("Tools/PPE/Apply Helmet-Side Shelf Outer Post Collider")]
    public static void ApplyHelmetSideShelfOuterPostCollider()
    {
        Scene scene = RequireTargetScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before changing the shelf post Collider.");

        ConfigureInteractiveShelfHelmetOuterPostCollider(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "[PPE Room Collision] Applied one thin Collider to the helmet-side outer shelf post. " +
            "The active scene was not saved; review existing unsaved changes before saving.");
    }

    internal static SphereCollider[] FindPrimaryPpeSelectionMarkers(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SphereCollider>(true))
            .Where(marker =>
                marker.gameObject.name == PpeMarkerName &&
                marker.transform.parent != null &&
                marker.transform.parent.parent != null &&
                marker.transform.parent.parent.name == "PPE" &&
                marker.transform.parent.GetComponent<PPEMarkerToggleGrab>() != null)
            .ToArray();
    }

    internal static Bounds CalculateInteractiveShelfHelmetOuterPostBounds(Scene scene)
    {
        Renderer[] renderers = InteractiveShelfHelmetOuterPostRendererPaths
            .Select(path => FindByPath(scene, path)?.GetComponent<Renderer>())
            .ToArray();
        if (renderers.Any(renderer => renderer == null))
        {
            throw new InvalidOperationException(
                "The helmet-side outer shelf post is missing one or more authored Renderer parts.");
        }

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    internal static Bounds CalculateTargetBounds(Scene scene, CollisionTarget target)
    {
        Bounds bounds = CalculateRendererBounds(scene, target);
        if (target.FixedBottomHeight > 0f)
        {
            float height = Mathf.Min(bounds.size.y, target.FixedBottomHeight);
            Vector3 size = bounds.size;
            size.y = height;
            Vector3 center = bounds.center;
            center.y = bounds.min.y + height * 0.5f;
            bounds.SetMinMax(center - size * 0.5f, center + size * 0.5f);
        }

        if (target.FixedDepth > 0f)
        {
            float depth = Mathf.Min(bounds.size.z - target.FrontInset, target.FixedDepth);
            if (depth <= 0f)
                throw new InvalidOperationException($"Invalid recessed blocker depth: {target.BlockerName}");

            float front = bounds.min.z + target.FrontInset;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            min.z = front;
            max.z = front + depth;
            bounds.SetMinMax(min, max);
        }

        return bounds;
    }

    internal static Bounds CalculateRendererBounds(Scene scene, CollisionTarget target)
    {
        GameObject source = FindByPath(scene, target.TargetPath);
        if (source == null)
            throw new InvalidOperationException($"Collision source is missing: {target.TargetPath}");

        Renderer[] renderers = target.UseOwnRenderer
            ? new[] { source.GetComponent<Renderer>() }
            : source.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();

        if (renderers.Length == 0 || renderers.Any(renderer => renderer == null))
            throw new InvalidOperationException($"No valid Renderer bounds found at: {target.TargetPath}");

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);

        if (target.ApplyMinimumThickness)
        {
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, MinimumWallThickness);
            size.y = Mathf.Max(size.y, MinimumWallThickness);
            size.z = Mathf.Max(size.z, MinimumWallThickness);
            bounds.size = size;
        }

        return bounds;
    }

    internal static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open the exact target scene before running this tool. Expected '{ScenePath}', current '{scene.path}'.");
        }

        return scene;
    }

    internal static GameObject FindByPath(Scene scene, string hierarchyPath)
    {
        string[] parts = hierarchyPath.Split('/');
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == parts[0]);
        if (root == null)
            return null;

        Transform current = root.transform;
        for (int index = 1; index < parts.Length; index++)
        {
            current = FindDirectChild(current, parts[index]);
            if (current == null)
                return null;
        }

        return current.gameObject;
    }

    private static void ConfigureCharacterController(GameObject xrOrigin)
    {
        CharacterController controller = xrOrigin.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(xrOrigin);
        }
        else
        {
            Undo.RecordObject(controller, "Configure XR Origin CharacterController");
        }

        controller.enabled = true;
        controller.radius = CharacterRadius;
        controller.height = CharacterHeight;
        controller.center = new Vector3(0f, CharacterHeight * 0.5f, 0f);
        controller.skinWidth = CharacterSkinWidth;
        controller.stepOffset = CharacterStepOffset;
        controller.slopeLimit = CharacterSlopeLimit;
        controller.minMoveDistance = 0f;
        controller.detectCollisions = true;
        controller.enableOverlapRecovery = true;
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureBlocker(Scene scene, Transform collisionRoot, CollisionTarget target)
    {
        Bounds bounds = CalculateTargetBounds(scene, target);
        Transform blockerTransform = FindDirectChild(collisionRoot, target.BlockerName);
        GameObject blocker;
        if (blockerTransform == null)
        {
            blocker = new GameObject(target.BlockerName);
            Undo.RegisterCreatedObjectUndo(blocker, $"Create {target.BlockerName}");
            blocker.transform.SetParent(collisionRoot, false);
        }
        else
        {
            blocker = blockerTransform.gameObject;
            Undo.RecordObject(blocker, $"Configure {target.BlockerName}");
            Undo.RecordObject(blocker.transform, $"Configure {target.BlockerName} Transform");
        }

        blocker.layer = CollisionLayer;
        blocker.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
        blocker.transform.localScale = Vector3.one;

        BoxCollider collider = blocker.GetComponent<BoxCollider>();
        if (collider == null)
            collider = Undo.AddComponent<BoxCollider>(blocker);
        else
            Undo.RecordObject(collider, $"Configure {target.BlockerName} BoxCollider");

        collider.enabled = true;
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = bounds.size;
        EditorUtility.SetDirty(blocker);
        EditorUtility.SetDirty(collider);
    }

    private static void ConfigureInteractiveShelfCollider(Scene scene)
    {
        GameObject shelf = FindByPath(scene, InteractiveShelfPath);
        if (shelf == null)
            throw new InvalidOperationException($"Interactive shelving is missing: {InteractiveShelfPath}");

        BoxCollider[] colliders = shelf.GetComponents<BoxCollider>();
        if (colliders.Length > 1)
            throw new InvalidOperationException(
                $"'{InteractiveShelfPath}' has more than one direct BoxCollider. Keep one Inspector-authored base collider.");

        Undo.RecordObject(shelf, "Configure interactive shelving collision layer");
        shelf.layer = CollisionLayer;

        BoxCollider collider;
        if (colliders.Length == 0)
        {
            collider = Undo.AddComponent<BoxCollider>(shelf);
            Bounds worldBounds = CalculateTargetBounds(scene, InteractiveShelfTarget);
            Bounds localBounds = ConvertWorldBoundsToLocal(shelf.transform, worldBounds);
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }
        else
        {
            collider = colliders[0];
            Undo.RecordObject(collider, "Configure interactive shelving BoxCollider");
        }

        // Center and Size are deliberately preserved once authored so they remain adjustable in the Inspector.
        collider.enabled = true;
        collider.isTrigger = false;
        EditorUtility.SetDirty(shelf);
        EditorUtility.SetDirty(collider);

        ConfigureInteractiveShelfBoardColliders(scene);
        ConfigureInteractiveShelfHelmetOuterPostCollider(scene);
    }

    private static void ConfigureInteractiveShelfBoardColliders(Scene scene)
    {
        foreach (string boardPath in InteractiveShelfBoardPaths)
        {
            GameObject board = FindByPath(scene, boardPath);
            if (board == null)
                throw new InvalidOperationException($"Interactive shelf board is missing: {boardPath}");

            MeshFilter meshFilter = board.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                throw new InvalidOperationException($"Interactive shelf board mesh is missing: {boardPath}");

            BoxCollider[] colliders = board.GetComponents<BoxCollider>();
            if (colliders.Length > 1)
                throw new InvalidOperationException($"Interactive shelf board has multiple BoxColliders: {boardPath}");

            Undo.RecordObject(board, "Configure interactive shelf board collision layer");
            board.layer = CollisionLayer;

            BoxCollider boardCollider;
            if (colliders.Length == 0)
            {
                boardCollider = Undo.AddComponent<BoxCollider>(board);
                boardCollider.center = meshFilter.sharedMesh.bounds.center;
                boardCollider.size = meshFilter.sharedMesh.bounds.size;
            }
            else
            {
                boardCollider = colliders[0];
                Undo.RecordObject(boardCollider, "Configure interactive shelf board BoxCollider");
            }

            boardCollider.enabled = true;
            boardCollider.isTrigger = false;
            EditorUtility.SetDirty(board);
            EditorUtility.SetDirty(boardCollider);
        }
    }

    private static void ConfigureInteractiveShelfHelmetOuterPostCollider(Scene scene)
    {
        GameObject owner = FindByPath(scene, InteractiveShelfHelmetOuterPostOwnerPath);
        if (owner == null)
        {
            throw new InvalidOperationException(
                $"Helmet-side outer shelf post owner is missing: {InteractiveShelfHelmetOuterPostOwnerPath}");
        }

        BoxCollider[] colliders = owner.GetComponents<BoxCollider>();
        if (colliders.Length > 1)
        {
            throw new InvalidOperationException(
                "Helmet-side outer shelf post must own exactly one thin BoxCollider.");
        }

        Undo.RecordObject(owner, "Configure helmet-side shelf outer post collision layer");
        owner.layer = CollisionLayer;

        BoxCollider collider;
        if (colliders.Length == 0)
        {
            collider = Undo.AddComponent<BoxCollider>(owner);
            Bounds worldBounds = CalculateInteractiveShelfHelmetOuterPostBounds(scene);
            Bounds localBounds = ConvertWorldBoundsToLocal(owner.transform, worldBounds);
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }
        else
        {
            collider = colliders[0];
            Undo.RecordObject(collider, "Configure helmet-side shelf outer post BoxCollider");
        }

        collider.enabled = true;
        collider.isTrigger = false;
        EditorUtility.SetDirty(owner);
        EditorUtility.SetDirty(collider);
    }

    private static Bounds ConvertWorldBoundsToLocal(Transform owner, Bounds worldBounds)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Bounds localBounds = new(owner.InverseTransformPoint(min), Vector3.zero);
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);
                    localBounds.Encapsulate(owner.InverseTransformPoint(corner));
                }
            }
        }

        return localBounds;
    }

    private static void RemoveObsoleteBlockers(Transform collisionRoot)
    {
        var expectedNames = new HashSet<string>(Targets.Select(target => target.BlockerName), StringComparer.Ordinal);
        for (int index = collisionRoot.childCount - 1; index >= 0; index--)
        {
            Transform child = collisionRoot.GetChild(index);
            if (!expectedNames.Contains(child.name))
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static void ConfigurePpeMarkerSelectionColliders(Scene scene)
    {
        Collider[] markerColliders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Collider>(true))
            .Where(collider => collider.gameObject.name == PpeMarkerName)
            .ToArray();
        if (markerColliders.Length == 0)
            throw new InvalidOperationException($"No '{PpeMarkerName}' colliders were found in the target scene.");

        var primaryMarkers = new HashSet<SphereCollider>(FindPrimaryPpeSelectionMarkers(scene));
        foreach (Collider collider in markerColliders)
        {
            Undo.RecordObject(collider, "Configure PPE marker as selection trigger");
            collider.isTrigger = true;
            if (collider is SphereCollider sphere && primaryMarkers.Contains(sphere))
                sphere.radius = PrimaryPpeMarkerSelectionRadius;
            EditorUtility.SetDirty(collider);
        }
    }

    private static void ConfigurePpeMarkerTriggerQueries(Scene scene)
    {
        NearFarInteractor[] interactors = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<NearFarInteractor>(true))
            .Where(interactor =>
                interactor.gameObject.activeInHierarchy &&
                (interactor.name == "Left_NearFarInteractor" ||
                 interactor.name == "Right_NearFarInteractor"))
            .ToArray();
        if (interactors.Length != 2)
            throw new InvalidOperationException("Expected the active left and right NearFarInteractor components.");

        foreach (NearFarInteractor interactor in interactors)
        {
            SerializedObject serializedInteractor = new(interactor);
            ConfigureTriggerQuery(
                serializedInteractor.FindProperty("m_NearInteractionCaster")?.objectReferenceValue,
                "m_PhysicsTriggerInteraction",
                interactor);
        }
    }

    private static void ConfigureTriggerQuery(
        UnityEngine.Object caster,
        string propertyName,
        NearFarInteractor owner)
    {
        if (caster == null)
            throw new InvalidOperationException($"'{owner.name}' is missing its authored caster reference.");

        Undo.RecordObject(caster, "Allow PPE marker trigger queries");
        SerializedObject serializedCaster = new(caster);
        SerializedProperty triggerInteraction = serializedCaster.FindProperty(propertyName);
        if (triggerInteraction == null)
            throw new MissingFieldException(caster.GetType().Name, propertyName);

        triggerInteraction.enumValueIndex = (int)QueryTriggerInteraction.Collide;
        serializedCaster.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
        EditorUtility.SetDirty(caster);
    }

    private static T RequireComponentAtPath<T>(Scene scene, string hierarchyPath) where T : Component
    {
        GameObject gameObject = FindByPath(scene, hierarchyPath);
        if (gameObject == null || !gameObject.TryGetComponent(out T component))
            throw new InvalidOperationException($"Required {typeof(T).Name} is missing at: {hierarchyPath}");
        return component;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static void SetWorldIdentity(Transform transform)
    {
        Undo.RecordObject(transform, "Reset PPE environment collision root Transform");
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
    }
}

public static class PPERoomEnvironmentCollisionValidationHarness
{
    private const float BoundsTolerance = 0.02f;

    [MenuItem("Tools/PPE/Validate Furniture Collider Alignment")]
    public static void ValidateFurnitureColliderAlignment()
    {
        Scene scene = PPERoomEnvironmentCollisionSetup.RequireTargetScene();
        GameObject root = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.CollisionRootName);
        if (root == null)
            throw new InvalidOperationException("PPE environment collision root is missing.");

        List<string> failures = new();
        int validatedFurniture = 0;
        foreach (PPERoomEnvironmentCollisionSetup.CollisionTarget target in
            PPERoomEnvironmentCollisionSetup.Targets.Where(candidate =>
                candidate.BlockerName.StartsWith("Furniture -", StringComparison.Ordinal)))
        {
            Transform blocker = root.transform.Find(target.BlockerName);
            BoxCollider[] colliders = blocker != null
                ? blocker.GetComponents<BoxCollider>()
                : Array.Empty<BoxCollider>();
            if (blocker == null || colliders.Length != 1)
            {
                failures.Add($"{target.BlockerName}: expected one BoxCollider, found {colliders.Length}.");
                continue;
            }

            Bounds expected = PPERoomEnvironmentCollisionSetup.CalculateTargetBounds(scene, target);
            Bounds actual = colliders[0].bounds;
            float centerDelta = Vector3.Distance(actual.center, expected.center);
            float sizeDelta = Vector3.Distance(actual.size, expected.size);
            if (centerDelta > BoundsTolerance || sizeDelta > BoundsTolerance)
            {
                failures.Add(
                    $"{target.BlockerName}: center delta={centerDelta:F3}m, size delta={sizeDelta:F3}m, " +
                    $"actual center={actual.center}, size={actual.size}, " +
                    $"expected center={expected.center}, size={expected.size}.");
            }

            validatedFurniture++;
        }

        try
        {
            ValidateInteractiveShelfCollider(scene);
            ValidateInteractiveShelfBoardColliders(scene);
            ValidateInteractiveShelfHelmetOuterPostCollider(scene);
        }
        catch (Exception exception)
        {
            failures.Add("Interactive metal shelf: " + exception.Message);
        }

        Collider[] ppeMarkers = scene.GetRootGameObjects()
            .SelectMany(sceneRoot => sceneRoot.GetComponentsInChildren<Collider>(true))
            .Where(collider => collider.gameObject.name == PPERoomEnvironmentCollisionSetup.PpeMarkerName)
            .ToArray();
        GameObject shelf = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.InteractiveShelfPath);
        Collider[] furnitureSolids = root.GetComponentsInChildren<Collider>(true)
            .Concat(shelf != null ? shelf.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>())
            .Where(collider => collider.enabled && !collider.isTrigger)
            .Distinct()
            .ToArray();
        var markerOverlapNotes = new List<string>();
        foreach (Collider marker in ppeMarkers)
        {
            foreach (Collider solid in furnitureSolids)
            {
                if (marker.bounds.Intersects(solid.bounds))
                {
                    markerOverlapNotes.Add(
                        $"{GetPath(marker.transform)} -> {GetPath(solid.transform)}");
                }
            }
        }

        if (markerOverlapNotes.Count > 0)
        {
            Debug.Log(
                "[PPE Furniture Collision] Informational Marker/solid Bounds overlaps " +
                $"(Layer isolation means these are not Grab blockers):\n- " +
                string.Join("\n- ", markerOverlapNotes));
        }

        if (failures.Count > 0)
        {
            string message = "Furniture Collider alignment validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Furniture Collision] PASS: {validatedFurniture} furniture blockers, " +
            "the metal shelf base, three boards, and the helmet-side outer post match current authored geometry.");
    }

    [MenuItem("Tools/PPE/Validate Primary PPE Marker Selection Radius")]
    public static void ValidatePrimaryPpeMarkerSelectionRadius()
    {
        Scene scene = PPERoomEnvironmentCollisionSetup.RequireTargetScene();
        SphereCollider[] markers =
            PPERoomEnvironmentCollisionSetup.FindPrimaryPpeSelectionMarkers(scene);

        Require(markers.Length > 0, "No primary PPE selection marker colliders were found.");
        foreach (SphereCollider marker in markers)
        {
            Require(Approximately(
                    marker.radius,
                    PPERoomEnvironmentCollisionSetup.PrimaryPpeMarkerSelectionRadius),
                $"Primary PPE marker selection radius changed: {GetPath(marker.transform)}");
            Require(marker.isTrigger,
                $"Primary PPE marker must remain a trigger: {GetPath(marker.transform)}");
            Require(marker.GetComponent<Renderer>() != null,
                $"Primary PPE visual marker Renderer is missing: {GetPath(marker.transform)}");
        }

        Debug.Log(
            $"[PPE Marker Selection] PASS: {markers.Length} primary selection colliders use radius " +
            $"{PPERoomEnvironmentCollisionSetup.PrimaryPpeMarkerSelectionRadius:F3}; visual marker objects remain present.");
    }

    [MenuItem("Tools/PPE/Validate Interactive Shelf Access Collision")]
    public static void ValidateInteractiveShelfAccess()
    {
        Scene scene = PPERoomEnvironmentCollisionSetup.RequireTargetScene();
        GameObject root = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.CollisionRootName);
        Require(root != null, "Collision root is missing.");
        Require(root.transform.Find("Furniture - Wall Hanger") == null,
            "Obsolete Wall Hanger blocker overlaps the interactive metal shelving approach area.");

        BoxCollider shelfCollider = ValidateInteractiveShelfCollider(scene);
        Require(shelfCollider != null, "Interactive metal shelving base Collider is missing.");
        ValidateInteractiveShelfBoardColliders(scene);
        ValidateInteractiveShelfHelmetOuterPostCollider(scene);
        ValidateWoodenCrate02Blocker(scene, root.transform);

        ValidateInputIsolation(scene);

        GameObject frontWall = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.CollisionRootName + "/Wall - Front");
        BoxCollider frontWallCollider = frontWall != null ? frontWall.GetComponent<BoxCollider>() : null;
        Require(frontWallCollider != null && frontWallCollider.enabled && !frontWallCollider.isTrigger,
            "Wall - Front must remain a solid boundary after removing the overlapping Wall Hanger blocker.");

        Debug.Log(
            "[PPE Room Collision] PASS: the interactive shelf owns horizontal board Colliders outside the grab layer, " +
            "the overlapping Wall Hanger blocker is absent, and Wall - Front remains solid.");
    }

    [MenuItem("Tools/PPE/Validate Room Environment Collision")]
    public static void Validate()
    {
        Scene scene = PPERoomEnvironmentCollisionSetup.RequireTargetScene();
        GameObject xrOriginObject = PPERoomEnvironmentCollisionSetup.FindByPath(scene, "XR Origin (VR)");
        if (xrOriginObject == null || !xrOriginObject.activeInHierarchy)
            throw new InvalidOperationException("[PPE Room Collision] Active XR Origin (VR) is missing.");

        CharacterController[] controllers = xrOriginObject.GetComponents<CharacterController>();
        if (controllers.Length != 1)
            throw new InvalidOperationException($"[PPE Room Collision] Expected one CharacterController, found {controllers.Length}.");

        CharacterController controller = controllers[0];
        Require(controller.enabled, "CharacterController is disabled.");
        Require(Approximately(controller.radius, PPERoomEnvironmentCollisionSetup.CharacterRadius), "CharacterController radius changed.");
        Require(Approximately(controller.skinWidth, PPERoomEnvironmentCollisionSetup.CharacterSkinWidth), "CharacterController skin width changed.");
        Require(Approximately(controller.stepOffset, PPERoomEnvironmentCollisionSetup.CharacterStepOffset), "CharacterController step offset changed.");
        Require(Approximately(controller.slopeLimit, PPERoomEnvironmentCollisionSetup.CharacterSlopeLimit), "CharacterController slope limit changed.");
        Require(controller.detectCollisions, "CharacterController collision detection is disabled.");

        XRBodyTransformer transformer = xrOriginObject.GetComponentInChildren<XRBodyTransformer>(true);
        Require(transformer != null && transformer.useCharacterControllerIfExists,
            "XRBodyTransformer is not configured to use the CharacterController.");

        Require(!Physics.GetIgnoreLayerCollision(xrOriginObject.layer, PPERoomEnvironmentCollisionSetup.CollisionLayer),
            "Physics collision matrix ignores XR Origin and Ignore Raycast layers.");

        ValidateInputIsolation(scene);
        ValidatePpeMarkerSelectionColliders(scene);
        ValidatePpeMarkerTriggerQueries(scene);
        ValidatePpeGrabColliderBindings(scene);

        GameObject root = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.CollisionRootName);
        Require(root != null, "Collision root is missing.");
        Require(root.transform.parent == null, "Collision root must remain at scene root.");
        Require(root.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer, "Collision root layer changed.");
        Require(Vector3.Distance(root.transform.position, Vector3.zero) <= BoundsTolerance &&
                Quaternion.Angle(root.transform.rotation, Quaternion.identity) <= 0.1f &&
                Vector3.Distance(root.transform.localScale, Vector3.one) <= BoundsTolerance,
            "Collision root Transform is not world identity.");
        Require(root.transform.childCount == PPERoomEnvironmentCollisionSetup.Targets.Length,
            $"Expected {PPERoomEnvironmentCollisionSetup.Targets.Length} blockers, found {root.transform.childCount}.");
        var blockerNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (PPERoomEnvironmentCollisionSetup.CollisionTarget target in PPERoomEnvironmentCollisionSetup.Targets)
        {
            Require(blockerNames.Add(target.BlockerName), $"Duplicate blocker definition: {target.BlockerName}");
            Transform blockerTransform = root.transform.Cast<Transform>()
                .SingleOrDefault(child => child.name == target.BlockerName);
            Require(blockerTransform != null, $"Blocker is missing: {target.BlockerName}");

            GameObject blocker = blockerTransform.gameObject;
            BoxCollider[] boxColliders = blocker.GetComponents<BoxCollider>();
            Require(boxColliders.Length == 1, $"{target.BlockerName} must have exactly one BoxCollider.");
            Require(blocker.GetComponents<Collider>().Length == 1, $"{target.BlockerName} has an unexpected Collider.");
            Require(blocker.GetComponent<Rigidbody>() == null, $"{target.BlockerName} must not have a Rigidbody.");
            Require(blocker.GetComponent<Renderer>() == null, $"{target.BlockerName} must not have a Renderer.");
            Require(blocker.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer,
                $"{target.BlockerName} must use Ignore Raycast layer.");

            BoxCollider collider = boxColliders[0];
            Require(collider.enabled && !collider.isTrigger, $"{target.BlockerName} must be an enabled solid collider.");
            Bounds expected = PPERoomEnvironmentCollisionSetup.CalculateTargetBounds(scene, target);
            Require(Vector3.Distance(collider.bounds.center, expected.center) <= BoundsTolerance,
                $"{target.BlockerName} center does not match its source bounds.");
            Require(Vector3.Distance(collider.bounds.size, expected.size) <= BoundsTolerance,
                $"{target.BlockerName} size does not match its source bounds.");

        }

        BoxCollider shelfCollider = ValidateInteractiveShelfCollider(scene);
        ValidateInteractiveShelfBoardColliders(scene);
        ValidateInteractiveShelfHelmetOuterPostCollider(scene);
        GameObject shelf = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.InteractiveShelfPath);
        BoxCollider[] blockers = root.GetComponentsInChildren<BoxCollider>(true)
            .Concat(shelf.GetComponentsInChildren<BoxCollider>(true))
            .Distinct()
            .ToArray();
        ValidateTeleportDestinations(scene, blockers);

        Debug.Log(
            $"[PPE Room Collision] PASS: CharacterController wired, {PPERoomEnvironmentCollisionSetup.Targets.Length} " +
            "environment blockers match their bounds, and PPE_B_MetalShelving owns its Inspector-adjustable base BoxCollider. " +
            "Quest room-scale leaning still requires headset validation.");
    }

    private static BoxCollider ValidateInteractiveShelfCollider(Scene scene)
    {
        GameObject shelf = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene, PPERoomEnvironmentCollisionSetup.InteractiveShelfPath);
        Require(shelf != null, "PPE_B_MetalShelving is missing.");
        if (shelf == null)
            return null;

        Require(shelf.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer,
            "PPE_B_MetalShelving root must use Ignore Raycast so its base collider cannot consume Grab rays.");
        BoxCollider[] colliders = shelf.GetComponents<BoxCollider>();
        Require(colliders.Length == 1,
            $"PPE_B_MetalShelving must directly own one Inspector-adjustable BoxCollider, found {colliders.Length}.");
        if (colliders.Length == 0)
            return null;

        BoxCollider collider = colliders[0];
        Require(collider.enabled && !collider.isTrigger,
            "PPE_B_MetalShelving base BoxCollider must be an enabled solid collider.");
        Require(shelf.GetComponents<Collider>().Length == 1,
            "PPE_B_MetalShelving root has an unexpected additional Collider.");
        Require(shelf.GetComponent<Rigidbody>() == null,
            "PPE_B_MetalShelving root must not have a Rigidbody.");

        Bounds shelfBounds = PPERoomEnvironmentCollisionSetup.CalculateRendererBounds(
            scene, PPERoomEnvironmentCollisionSetup.InteractiveShelfTarget);
        Require(collider.bounds.size.y <= PPERoomEnvironmentCollisionSetup.InteractiveShelfBaseStopHeight + BoundsTolerance,
            "PPE_B_MetalShelving BoxCollider is too tall and can block displayed PPE.");
        Require(collider.bounds.size.z <= PPERoomEnvironmentCollisionSetup.InteractiveShelfBaseStopDepth + BoundsTolerance,
            "PPE_B_MetalShelving BoxCollider is too deep and can stop the player too far from the shelf.");
        Require(collider.bounds.size.y < collider.bounds.size.z,
            "PPE_B_MetalShelving BoxCollider must lie horizontally to preserve the PPE grab approach space.");
        Require(Mathf.Abs(collider.bounds.min.y - shelfBounds.min.y) <= BoundsTolerance,
            "PPE_B_MetalShelving BoxCollider must remain anchored to the furniture bottom.");
        Require(Mathf.Abs(collider.bounds.min.z - shelfBounds.min.z) <= BoundsTolerance,
            "PPE_B_MetalShelving BoxCollider must remain aligned with the visible front surface.");
        return collider;
    }

    private static void ValidateInteractiveShelfBoardColliders(Scene scene)
    {
        Collider[] markerColliders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Collider>(true))
            .Where(collider => collider.gameObject.name == PPERoomEnvironmentCollisionSetup.PpeMarkerName)
            .ToArray();

        foreach (string boardPath in PPERoomEnvironmentCollisionSetup.InteractiveShelfBoardPaths)
        {
            GameObject board = PPERoomEnvironmentCollisionSetup.FindByPath(scene, boardPath);
            Require(board != null, $"Interactive shelf board is missing: {boardPath}");
            Require(board.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer,
                $"Interactive shelf board must use the collision-only layer: {boardPath}");

            Renderer renderer = board.GetComponent<Renderer>();
            BoxCollider[] colliders = board.GetComponents<BoxCollider>();
            Require(renderer != null, $"Interactive shelf board Renderer is missing: {boardPath}");
            Require(colliders.Length == 1,
                $"Interactive shelf board must own one BoxCollider: {boardPath}");
            if (renderer == null || colliders.Length == 0)
                continue;

            BoxCollider collider = colliders[0];
            Require(collider.enabled && !collider.isTrigger,
                $"Interactive shelf board Collider must be solid: {boardPath}");
            Require(Vector3.Distance(collider.bounds.center, renderer.bounds.center) <= BoundsTolerance,
                $"Interactive shelf board Collider center does not match its Renderer: {boardPath}");
            Require(Vector3.Distance(collider.bounds.size, renderer.bounds.size) <= BoundsTolerance,
                $"Interactive shelf board Collider size does not match its Renderer: {boardPath}");
            foreach (Collider markerCollider in markerColliders)
            {
                if (!collider.bounds.Intersects(markerCollider.bounds))
                    continue;

                Bounds markerBounds = markerCollider.bounds;
                Bounds boardBounds = collider.bounds;
                float penetrationX = Mathf.Min(markerBounds.max.x, boardBounds.max.x) -
                    Mathf.Max(markerBounds.min.x, boardBounds.min.x);
                float penetrationY = Mathf.Min(markerBounds.max.y, boardBounds.max.y) -
                    Mathf.Max(markerBounds.min.y, boardBounds.min.y);
                float penetrationZ = Mathf.Min(markerBounds.max.z, boardBounds.max.z) -
                    Mathf.Max(markerBounds.min.z, boardBounds.min.z);
                float minimumPenetration = Mathf.Min(penetrationX, penetrationY, penetrationZ);
                Require(minimumPenetration <= BoundsTolerance,
                    $"Interactive shelf board deeply overlaps PPE grab marker: {boardPath} -> " +
                    GetPath(markerCollider.transform));
            }
        }
    }

    private static void ValidateInteractiveShelfHelmetOuterPostCollider(Scene scene)
    {
        GameObject owner = PPERoomEnvironmentCollisionSetup.FindByPath(
            scene,
            PPERoomEnvironmentCollisionSetup.InteractiveShelfHelmetOuterPostOwnerPath);
        Require(owner != null, "Helmet-side outer shelf post owner is missing.");
        if (owner == null)
            return;

        Require(owner.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer,
            "Helmet-side outer shelf post Collider must use the collision-only layer.");
        BoxCollider[] colliders = owner.GetComponents<BoxCollider>();
        Require(colliders.Length == 1,
            "Helmet-side outer shelf post must own exactly one thin BoxCollider.");
        if (colliders.Length == 0)
            return;

        BoxCollider collider = colliders[0];
        Bounds expected =
            PPERoomEnvironmentCollisionSetup.CalculateInteractiveShelfHelmetOuterPostBounds(scene);
        Require(collider.enabled && !collider.isTrigger,
            "Helmet-side outer shelf post Collider must be solid.");
        Require(Vector3.Distance(collider.bounds.center, expected.center) <= BoundsTolerance,
            "Helmet-side outer shelf post Collider center does not match its visible parts.");
        Require(Vector3.Distance(collider.bounds.size, expected.size) <= BoundsTolerance,
            "Helmet-side outer shelf post Collider size does not match its visible parts.");
        Require(collider.bounds.size.x <= 0.07f && collider.bounds.size.z <= 0.11f,
            "Helmet-side outer shelf post Collider is wider or deeper than the thin visible column.");
        Require(collider.bounds.size.y >= 2f,
            "Helmet-side outer shelf post Collider does not cover the full visible column height.");
        Require(owner.GetComponentInParent<Rigidbody>() == null,
            "The static shelf post must not have a Rigidbody that can compete with shelf-board Colliders.");

        Collider[] markerColliders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Collider>(true))
            .Where(candidate => candidate.gameObject.name == PPERoomEnvironmentCollisionSetup.PpeMarkerName)
            .ToArray();
        foreach (Collider marker in markerColliders)
        {
            Require(!collider.bounds.Intersects(marker.bounds),
                $"Helmet-side outer shelf post overlaps PPE grab marker: {GetPath(marker.transform)}");
        }

        foreach (string path in PPERoomEnvironmentCollisionSetup.InteractiveShelfOtherVerticalPartPaths)
        {
            GameObject otherPart = PPERoomEnvironmentCollisionSetup.FindByPath(scene, path);
            Require(otherPart != null, $"Shelf vertical part is missing: {path}");
            if (otherPart != null)
            {
                Require(otherPart.GetComponents<BoxCollider>().Length == 0,
                    $"Only the helmet-side outer post may receive a new vertical BoxCollider: {path}");
            }
        }
    }

    private static void ValidateWoodenCrate02Blocker(Scene scene, Transform collisionRoot)
    {
        PPERoomEnvironmentCollisionSetup.CollisionTarget target =
            PPERoomEnvironmentCollisionSetup.Targets.Single(item =>
                item.BlockerName == "Furniture - Wooden Crate 02");
        Transform blockerTransform = collisionRoot.Find(target.BlockerName);
        Require(blockerTransform != null, "Wooden Crate 02 blocker is missing.");
        if (blockerTransform == null)
            return;

        BoxCollider[] colliders = blockerTransform.GetComponents<BoxCollider>();
        Require(colliders.Length == 1, "Wooden Crate 02 blocker must own one BoxCollider.");
        if (colliders.Length == 0)
            return;

        BoxCollider collider = colliders[0];
        Bounds expected = PPERoomEnvironmentCollisionSetup.CalculateTargetBounds(scene, target);
        Require(collider.enabled && !collider.isTrigger,
            "Wooden Crate 02 blocker must be an enabled solid Collider.");
        Require(blockerTransform.gameObject.layer == PPERoomEnvironmentCollisionSetup.CollisionLayer,
            "Wooden Crate 02 blocker must use the collision-only layer.");
        Require(Vector3.Distance(collider.bounds.center, expected.center) <= BoundsTolerance,
            "Wooden Crate 02 blocker center does not match its Renderer Bounds.");
        Require(Vector3.Distance(collider.bounds.size, expected.size) <= BoundsTolerance,
            "Wooden Crate 02 blocker size does not match its Renderer Bounds.");
    }

    private static void ValidateInputIsolation(Scene scene)
    {
        foreach (SphereInteractionCaster caster in Resources.FindObjectsOfTypeAll<SphereInteractionCaster>()
                     .Where(item => item.gameObject.scene == scene))
        {
            Require((caster.physicsLayerMask.value & (1 << PPERoomEnvironmentCollisionSetup.CollisionLayer)) == 0,
                $"SphereInteractionCaster includes the collision-only layer: {GetPath(caster.transform)}");
        }

        foreach (XRRayInteractor ray in Resources.FindObjectsOfTypeAll<XRRayInteractor>()
                     .Where(item => item.gameObject.scene == scene))
        {
            Require((ray.raycastMask.value & (1 << PPERoomEnvironmentCollisionSetup.CollisionLayer)) == 0,
                $"XRRayInteractor includes the collision-only layer: {GetPath(ray.transform)}");
        }

        foreach (TrackedDeviceGraphicRaycaster raycaster in Resources.FindObjectsOfTypeAll<TrackedDeviceGraphicRaycaster>()
                     .Where(item => item.gameObject.scene == scene && item.gameObject.activeInHierarchy))
        {
            var serialized = new SerializedObject(raycaster);
            SerializedProperty checkFor3DOcclusion = serialized.FindProperty("m_CheckFor3DOcclusion");
            Require(checkFor3DOcclusion != null && !checkFor3DOcclusion.boolValue,
                $"Active XR UI raycaster enables 3D occlusion: {GetPath(raycaster.transform)}");
        }
    }

    private static void ValidatePpeMarkerSelectionColliders(Scene scene)
    {
        Require(Physics.queriesHitTriggers,
            "Physics queries must include triggers so the Near caster can select PPE markers.");

        Collider[] markerColliders = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Collider>(true))
            .Where(collider => collider.gameObject.name == PPERoomEnvironmentCollisionSetup.PpeMarkerName)
            .ToArray();
        Require(markerColliders.Length > 0, "No PPE item marker colliders were found.");
        foreach (Collider collider in markerColliders)
        {
            if (collider.gameObject.activeInHierarchy)
                Require(collider.enabled, $"Active PPE marker collider is disabled: {GetPath(collider.transform)}");
            Require(collider.gameObject.layer == PPERoomEnvironmentCollisionSetup.PpeMarkerLayer,
                $"PPE marker must remain on the near-grab-only layer: {GetPath(collider.transform)}");
            Require(collider.isTrigger,
                $"PPE marker must be a selection trigger, not a solid player obstacle: {GetPath(collider.transform)}");
        }

        SphereCollider[] primaryMarkers =
            PPERoomEnvironmentCollisionSetup.FindPrimaryPpeSelectionMarkers(scene);
        Require(primaryMarkers.Length > 0, "No primary PPE selection marker colliders were found.");
        foreach (SphereCollider marker in primaryMarkers)
        {
            Require(Approximately(
                    marker.radius,
                    PPERoomEnvironmentCollisionSetup.PrimaryPpeMarkerSelectionRadius),
                $"Primary PPE marker selection radius changed: {GetPath(marker.transform)}");
            Require(marker.GetComponent<Renderer>() != null,
                $"Primary PPE visual marker Renderer is missing: {GetPath(marker.transform)}");
        }
    }

    private static void ValidatePpeGrabColliderBindings(Scene scene)
    {
        PPEMarkerToggleGrab[] toggles = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEMarkerToggleGrab>(true))
            .Where(toggle => toggle.enabled)
            .ToArray();
        Require(toggles.Length > 0, "No enabled PPE marker-grab bindings were found.");

        foreach (PPEMarkerToggleGrab toggle in toggles)
        {
            XRGrabInteractable grab = toggle.GetComponent<XRGrabInteractable>();
            Require(grab != null && grab.enabled,
                $"PPE marker binding has no enabled XRGrabInteractable: {GetPath(toggle.transform)}");
            if (grab == null)
                continue;

            Transform marker = toggle.InteractionMarker;
            if (marker == null)
            {
                marker = toggle.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(candidate => candidate.name == PPERoomEnvironmentCollisionSetup.PpeMarkerName);
            }

            Collider markerCollider = marker != null ? marker.GetComponent<Collider>() : null;
            Require(markerCollider != null,
                $"PPE marker binding has no marker Collider: {GetPath(toggle.transform)}");
            if (markerCollider == null)
                continue;

            Require(markerCollider.isTrigger,
                $"PPE Grab marker must remain a Trigger: {GetPath(markerCollider.transform)}");
            Require(grab.colliders.Count == 1 && grab.colliders[0] == markerCollider,
                $"XRGrabInteractable must use only its authored marker Collider: {GetPath(toggle.transform)}");
        }
    }

    private static void ValidatePpeMarkerTriggerQueries(Scene scene)
    {
        NearFarInteractor[] interactors = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<NearFarInteractor>(true))
            .Where(interactor =>
                interactor.gameObject.activeInHierarchy &&
                (interactor.name == "Left_NearFarInteractor" ||
                 interactor.name == "Right_NearFarInteractor"))
            .ToArray();
        Require(interactors.Length == 2,
            "Expected the active left and right NearFarInteractor components.");

        foreach (NearFarInteractor interactor in interactors)
        {
            SerializedObject serializedInteractor = new(interactor);
            ValidateTriggerQuery(
                serializedInteractor.FindProperty("m_NearInteractionCaster")?.objectReferenceValue,
                "m_PhysicsTriggerInteraction",
                interactor);

            UnityEngine.Object farCasterObject = serializedInteractor
                .FindProperty("m_FarInteractionCaster")?.objectReferenceValue;
            Require(farCasterObject is CurveInteractionCaster,
                $"'{interactor.name}' is missing its authored far CurveInteractionCaster.");
            if (farCasterObject is CurveInteractionCaster farCaster)
            {
                Require((farCaster.raycastMask.value & (1 << PPERoomEnvironmentCollisionSetup.PpeMarkerLayer)) == 0,
                    $"'{interactor.name}' far caster must not target the near-grab-only PPE marker layer.");
            }
        }
    }

    private static void ValidateTriggerQuery(
        UnityEngine.Object caster,
        string propertyName,
        NearFarInteractor owner)
    {
        Require(caster != null, $"'{owner.name}' is missing its authored caster reference.");
        if (caster == null)
            return;

        SerializedProperty property = new SerializedObject(caster).FindProperty(propertyName);
        Require(property != null && property.enumValueIndex == (int)QueryTriggerInteraction.Collide,
            $"'{owner.name}' {caster.GetType().Name} must query trigger colliders so PPE markers remain selectable.");
    }

    private static void ValidateTeleportDestinations(Scene scene, IReadOnlyCollection<BoxCollider> blockers)
    {
        foreach (BaseTeleportationInteractable target in Resources.FindObjectsOfTypeAll<BaseTeleportationInteractable>()
                     .Where(item => item.gameObject.scene == scene))
        {
            Vector3 destination = target.transform.position;
            foreach (BoxCollider blocker in blockers)
            {
                Bounds expandedFootprint = blocker.bounds;
                expandedFootprint.Expand(new Vector3(
                    PPERoomEnvironmentCollisionSetup.CharacterRadius * 2f,
                    0f,
                    PPERoomEnvironmentCollisionSetup.CharacterRadius * 2f));
                Require(
                    destination.x < expandedFootprint.min.x || destination.x > expandedFootprint.max.x ||
                    destination.z < expandedFootprint.min.z || destination.z > expandedFootprint.max.z,
                    $"Teleport destination '{GetPath(target.transform)}' overlaps '{blocker.name}' after player-radius expansion.");
            }
        }
    }

    [MenuItem("Tools/PPE/Validate Room Environment Collision Play Mode Probe")]
    public static void ValidatePlayModeProbe()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Enter Play Mode and pause before running the collision probe.");
        if (!EditorApplication.isPaused)
            throw new InvalidOperationException("Pause Play Mode before running the collision probe.");

        Scene scene = PPERoomEnvironmentCollisionSetup.RequireTargetScene();
        GameObject xrOrigin = PPERoomEnvironmentCollisionSetup.FindByPath(scene, "XR Origin (VR)");
        CharacterController controller = xrOrigin != null ? xrOrigin.GetComponent<CharacterController>() : null;
        Require(controller != null && controller.enabled, "Play Mode CharacterController is unavailable.");

        Vector3 originalPosition = xrOrigin.transform.position;
        Quaternion originalRotation = xrOrigin.transform.rotation;
        Vector3 probeStart = new Vector3(6f, -0.9f, 9.5f);
        try
        {
            controller.enabled = false;
            xrOrigin.transform.SetPositionAndRotation(probeStart, Quaternion.identity);
            controller.height = PPERoomEnvironmentCollisionSetup.CharacterHeight;
            controller.center = new Vector3(0f, PPERoomEnvironmentCollisionSetup.CharacterHeight * 0.5f, 0f);
            controller.radius = PPERoomEnvironmentCollisionSetup.CharacterRadius;
            controller.enabled = true;
            Physics.SyncTransforms();

            CollisionFlags flags = controller.Move(Vector3.forward * 2f);
            Physics.SyncTransforms();
            Require((flags & CollisionFlags.Sides) != 0, "Front wall probe did not report a side collision.");
            Require(xrOrigin.transform.position.z < 10.4f,
                $"CharacterController crossed the front wall. Result z={xrOrigin.transform.position.z:0.###}.");

            Debug.Log(
                $"[PPE Room Collision] PLAY MODE PASS: front-wall move stopped at z={xrOrigin.transform.position.z:0.###}, " +
                $"flags={flags}.");
        }
        finally
        {
            controller.enabled = false;
            xrOrigin.transform.SetPositionAndRotation(originalPosition, originalRotation);
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }

    private static bool Approximately(float actual, float expected)
    {
        return Mathf.Abs(actual - expected) <= 0.001f;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"[PPE Room Collision] {message}");
    }

    private static string GetPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
