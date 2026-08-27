using UnityEngine;

/// <summary>
/// Builds the PPE room shell from dedicated wall, floor, and ceiling references.
/// The four walls share the same orthographic wall treatment while the horizontal
/// surfaces retain their own square texture layouts.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PPEBackgroundRoom : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Image Room";

    [Header("Room Materials")]
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material ceilingMaterial;
    [SerializeField] private Texture2D doorTexture;
    [SerializeField] private Shader doorCutoutShader;
    [SerializeField, Min(1f)] private float roomWidth = 25f;
    [SerializeField, Min(1f)] private float roomDepth = 25f;
    [SerializeField, Min(1f)] private float roomHeight = 7.5f;
    [SerializeField] private Color floorColor = Color.white;
    [SerializeField] private Color ceilingColor = Color.white;
    [SerializeField] private Color wallColor = Color.white;
    [Header("Duct Appearance")]
    [SerializeField] private Color ductColor = Color.white;
    [Header("Bench Appearance")]
    [SerializeField] private Color benchColor = new(2f, 1.96f, 1.92f, 1f);
    [Header("Room Lighting")]
    [SerializeField, Range(0f, 2f)] private float roomBrightness = 1f;
    [SerializeField, Range(0f, 2f)] private float floorBrightness = 1f;
    [Header("Door Appearance")]
    [SerializeField] private Color doorColor = new(0.72f, 0.72f, 0.72f, 1f);
    [SerializeField, Min(0.5f)] private float doorHeight = 2.3f;
    [SerializeField, Min(0.01f)] private float doorWallInset = 0.08f;

    private void OnEnable()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot != null)
        {
            SetHideFlagsRecursively(generatedRoot, HideFlags.None);
            RestoreMissingRoomMaterials(generatedRoot);
        }

        ApplyRoomBrightness();
        ApplyDuctColor();
        ApplyBenchColor();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EnsurePersistentDoorMaterial();
#endif
    }

    private void OnValidate()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot != null)
            RestoreMissingRoomMaterials(generatedRoot);

        ApplyRoomBrightness();
        ApplyDuctColor();
        ApplyBenchColor();
    }

    private void RestoreMissingRoomMaterials(Transform root)
    {
#if UNITY_EDITOR
        // An already-open scene can temporarily retain its old in-memory serialization
        // after these persistent assets are added on disk. Resolve only missing fields;
        // authored Inspector references remain authoritative.
        wallMaterial ??= UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/PPE/PPE_Room_Wall.mat");
        floorMaterial ??= UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/PPE/PPE_Room_Floor.mat");
        ceilingMaterial ??= UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/PPE/PPE_Room_Ceiling.mat");
#endif

        AssignMissingMaterial(root, "Front Wall", wallMaterial);
        AssignMissingMaterial(root, "Rear Wall", wallMaterial);
        AssignMissingMaterial(root, "Left Wall", wallMaterial);
        AssignMissingMaterial(root, "Right Wall", wallMaterial);
        AssignMissingMaterial(root, "Floor", floorMaterial);
        AssignMissingMaterial(root, "Ceiling", ceilingMaterial);
    }

    private static void AssignMissingMaterial(Transform root, string objectName, Material material)
    {
        if (material == null)
            return;

        foreach (Transform surface in root)
        {
            if (!MatchesSurfaceName(surface.name, objectName)
                || !surface.TryGetComponent(out MeshRenderer renderer))
                continue;

            Material currentMaterial = renderer.sharedMaterial;
            bool materialIsTransient = currentMaterial != null
                && (currentMaterial.hideFlags & HideFlags.DontSave) != 0;
            if (currentMaterial != null && !materialIsTransient)
                continue;

            renderer.sharedMaterial = material;
        }
    }

    private static bool MatchesSurfaceName(string candidateName, string baseName)
    {
        if (candidateName == baseName)
            return true;

        string copyPrefix = baseName + " (";
        if (!candidateName.StartsWith(copyPrefix) || !candidateName.EndsWith(")"))
            return false;

        string copyNumber = candidateName.Substring(
            copyPrefix.Length, candidateName.Length - copyPrefix.Length - 1);
        return int.TryParse(copyNumber, out int copyIndex) && copyIndex > 0;
    }

    private static void SetHideFlagsRecursively(Transform target, HideFlags flags)
    {
        target.gameObject.hideFlags = flags;
        foreach (Transform child in target)
            SetHideFlagsRecursively(child, flags);
    }

    [ContextMenu("Rebuild Room")]
    public void BuildRoom()
    {
        if (wallMaterial == null || floorMaterial == null || ceilingMaterial == null)
        {
            Debug.LogError("Cannot rebuild PPE room because its serialized room materials are missing.", this);
            return;
        }

        Transform previous = transform.Find(GeneratedRootName);
        if (previous != null)
        {
            if (Application.isPlaying)
                Destroy(previous.gameObject);
            else
                DestroyImmediate(previous.gameObject);
        }

        GameObject root = new(GeneratedRootName);
        root.transform.SetParent(transform, false);

        CreateWall(root.transform, "Front Wall", new Vector3(0f, roomHeight * 0.5f, roomDepth * 0.5f),
            new Vector3(0f, 180f, 0f), new Vector2(roomWidth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Rear Wall", new Vector3(0f, roomHeight * 0.5f, -roomDepth * 0.5f),
            Vector3.zero, new Vector2(roomWidth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Left Wall", new Vector3(-roomWidth * 0.5f, roomHeight * 0.5f, 0f),
            new Vector3(0f, 90f, 0f), new Vector2(roomDepth, roomHeight), wallMaterial);
        CreateWall(root.transform, "Right Wall", new Vector3(roomWidth * 0.5f, roomHeight * 0.5f, 0f),
            new Vector3(0f, -90f, 0f), new Vector2(roomDepth, roomHeight), wallMaterial);

        CreateSurface(root.transform, "Floor", new Vector3(0f, -0.03f, 0f),
            new Vector3(roomWidth, 0.05f, roomDepth), floorMaterial);
        CreateSurface(root.transform, "Ceiling", new Vector3(0f, roomHeight + 0.03f, 0f),
            new Vector3(roomWidth, 0.05f, roomDepth), ceilingMaterial);

        // Match the two luminaires painted into the ceiling texture.
        CreateCeilingLight(root.transform, "Ceiling Light Front", 2.1f);
        CreateCeilingLight(root.transform, "Ceiling Light Rear", -2.1f);
        // The image door lives outside Generated Image Room so rebuilding the room
        // never deletes or resets a scene-authored door Transform.
        CreateImageDoor(transform);

        // Keep the door's scene-authored Transform. PositionDoor remains available
        // from the component context menu when an automatic reset is explicitly wanted.
    }

    private void ApplyRoomBrightness()
    {
        Transform root = transform.Find(GeneratedRootName);
        if (root == null)
            return;

        ApplySurfaceBrightness(root, "Front Wall", wallColor);
        ApplySurfaceBrightness(root, "Rear Wall", wallColor);
        ApplySurfaceBrightness(root, "Left Wall", wallColor);
        ApplySurfaceBrightness(root, "Right Wall", wallColor);
        ApplySurfaceBrightness(root, "Floor", GetFloorColor());
        ApplySurfaceBrightness(root, "Ceiling", ceilingColor);
    }

    private void ApplyDuctColor()
    {
        Transform duct = FindSceneObject("PPE_B_Duct");
        if (duct == null || !duct.TryGetComponent(out MeshRenderer renderer))
            return;

        MaterialPropertyBlock propertyBlock = new();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", ductColor);
        propertyBlock.SetColor("_Color", ductColor);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyBenchColor()
    {
        Color adjustedColor = benchColor * roomBrightness;
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
        {
            if (!MatchesBenchName(candidate.name) || !candidate.gameObject.scene.IsValid())
                continue;

            Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                MaterialPropertyBlock propertyBlock = new();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", adjustedColor);
                propertyBlock.SetColor("_Color", adjustedColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

    private static bool MatchesBenchName(string candidateName)
    {
        return candidateName.StartsWith("ppe_room_bench")
            || candidateName == "PPE_B_Bench"
            || candidateName.StartsWith("PPE_B_Bench_");
    }

    private Color GetFloorColor()
    {
        return floorColor * floorBrightness;
    }

    private void ApplySurfaceBrightness(Transform root, string objectName, Color baseColor)
    {
        Color adjustedColor = baseColor * roomBrightness;
        foreach (Transform surface in root)
        {
            bool matchesSurface = surface.name == objectName
                || surface.name.StartsWith(objectName + " (");
            if (!matchesSurface || !surface.TryGetComponent(out MeshRenderer renderer))
                continue;

            MaterialPropertyBlock propertyBlock = new();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", adjustedColor);
            propertyBlock.SetColor("_Color", adjustedColor);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void CreateImageDoor(Transform parent)
    {
        if (doorTexture == null)
            return;

        Transform existingDoor = FindSceneObject("PPE Room Door Image");
        bool isNewDoor = existingDoor == null;
        GameObject imageDoor = isNewDoor
            ? GameObject.CreatePrimitive(PrimitiveType.Quad)
            : existingDoor.gameObject;

        imageDoor.name = "PPE Room Door Image";
        if (isNewDoor)
        {
            Transform doorReference = FindSceneObject("PPE_Room_Door");
            if (doorReference == null)
            {
                if (Application.isPlaying)
                    Destroy(imageDoor);
                else
                    DestroyImmediate(imageDoor);
                return;
            }

            imageDoor.transform.SetParent(parent, true);
            imageDoor.transform.SetPositionAndRotation(doorReference.position, doorReference.rotation);
            const float imageHeight = 4f;
            float imageAspect = (float)doorTexture.width / doorTexture.height;
            imageDoor.transform.localScale = new Vector3(imageHeight * imageAspect, imageHeight, 1f);
        }

        MeshRenderer doorRenderer = imageDoor.GetComponent<MeshRenderer>();
        Material currentMaterial = doorRenderer.sharedMaterial;
        bool materialMissing = currentMaterial == null || currentMaterial.shader == null
            || currentMaterial.shader.name == "Hidden/InternalErrorShader";
        if (materialMissing)
        {
            Shader doorShader = doorCutoutShader;
            if (doorShader == null)
                doorShader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new(doorShader) { name = "PPE Image Door" };
            material.hideFlags = HideFlags.DontSave;
            material.SetTexture("_BaseMap", doorTexture);
            material.SetColor("_BaseColor", doorColor);
            material.SetFloat("_Cutoff", 0.9f);
            doorRenderer.sharedMaterial = material;
        }

        CreateDoorGlass(imageDoor.transform);

        Collider collider = imageDoor.GetComponent<Collider>();
        if (Application.isPlaying)
            Destroy(collider);
        else
            DestroyImmediate(collider);
    }

#if UNITY_EDITOR
    private void EnsurePersistentDoorMaterial()
    {
        Transform door = FindSceneObject("PPE Room Door Image");
        if (door == null || !door.TryGetComponent(out MeshRenderer renderer)
            || renderer.sharedMaterial == null)
            return;

        const string folder = "Assets/Generated/PPE";
        const string assetPath = folder + "/PPE_Room_Door.mat";
        Material savedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (savedMaterial == null)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Generated"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Generated");
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder("Assets/Generated", "PPE");

            savedMaterial = new Material(renderer.sharedMaterial)
            {
                name = "PPE_Room_Door",
                hideFlags = HideFlags.None
            };
            UnityEditor.AssetDatabase.CreateAsset(savedMaterial, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        if (renderer.sharedMaterial != savedMaterial)
        {
            renderer.sharedMaterial = savedMaterial;
            UnityEditor.EditorUtility.SetDirty(renderer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    private void CreateDoorGlass(Transform door)
    {
        Transform existingGlass = door.Find("Door Window Glass");
        bool isNewGlass = existingGlass == null;
        GameObject glass = isNewGlass
            ? new GameObject("Door Window Glass")
            : existingGlass.gameObject;

        if (isNewGlass)
        {
            glass.transform.SetParent(door, false);
            glass.transform.localPosition = new Vector3(0.002f, -0.002f, -0.003f);
        }

        if (!glass.TryGetComponent(out MeshFilter _))
            glass.AddComponent<MeshFilter>();
        MeshRenderer renderer = glass.TryGetComponent(out MeshRenderer existingRenderer)
            ? existingRenderer
            : glass.AddComponent<MeshRenderer>();
        if (!glass.TryGetComponent(out DoorWindowGlass glassSize))
        {
            glassSize = glass.AddComponent<DoorWindowGlass>();
            glassSize.Configure(0.493724f, 0.744283f);
        }

        Material material = CreateMaterial("PPE Door Glass",
            new Color(0.52f, 0.76f, 0.86f, 0.28f), null);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", 5f);
        material.SetFloat("_DstBlend", 10f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_Smoothness", 0.92f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        renderer.sharedMaterial = material;
    }

    private void CreateCeilingLight(Transform parent, string objectName, float zPosition)
    {
        GameObject lightObject = new(objectName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, roomHeight - 0.08f, zPosition);
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light ceilingLight = lightObject.AddComponent<Light>();
        ceilingLight.type = LightType.Spot;
        ceilingLight.color = new Color(1f, 0.96f, 0.88f);
        ceilingLight.intensity = zPosition > 0f ? 180f : 300f;
        ceilingLight.range = roomHeight + 2f;
        ceilingLight.spotAngle = 90f;
        ceilingLight.innerSpotAngle = 55f;
        ceilingLight.shadows = LightShadows.None;
    }

    [ContextMenu("Position PPE Room Door")]
    private void PositionDoor()
    {
        Transform door = FindSceneObject("PPE_Room_Door");
        if (door == null)
            return;

        door.SetParent(transform, false);
        door.localPosition = Vector3.zero;
        // The door sits on the right wall and faces inward toward the room.
        door.localRotation = Quaternion.Euler(0f, -90f, 0f);
        door.localScale = Vector3.one;

        if (!TryGetRendererBounds(door, out Bounds bounds) || bounds.size.y <= Mathf.Epsilon)
            return;

        float scale = doorHeight / bounds.size.y;
        door.localScale = Vector3.one * scale;

        if (!TryGetRendererBounds(door, out bounds))
            return;

        Vector3 desiredWallCenterBottom = transform.TransformPoint(new Vector3(
            roomWidth * 0.5f - doorWallInset, 0f, 0f));

        door.position += new Vector3(
            desiredWallCenterBottom.x - bounds.max.x,
            desiredWallCenterBottom.y - bounds.min.y,
            desiredWallCenterBottom.z - bounds.center.z);
    }

    private static Transform FindSceneObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                return candidate;
        }

        return null;
    }

    private static bool TryGetRendererBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static Material CreateMaterial(string materialName, Color color, Texture texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        Material material = new(shader) { name = materialName };
        material.hideFlags = HideFlags.DontSave;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
        material.mainTexture = texture;
        return material;
    }

    private static void CreateWall(Transform parent, string objectName, Vector3 position,
        Vector3 rotation, Vector2 size, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = objectName;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localEulerAngles = rotation;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        wall.GetComponent<MeshRenderer>().sharedMaterial = material;

        Collider collider = wall.GetComponent<Collider>();
        if (collider == null)
            return;

        if (Application.isPlaying)
            Destroy(collider);
        else
            DestroyImmediate(collider);
    }

    private static void CreateSurface(Transform parent, string objectName, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = objectName;
        surface.transform.SetParent(parent, false);
        surface.transform.localPosition = position;
        surface.transform.localScale = scale;
        surface.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
