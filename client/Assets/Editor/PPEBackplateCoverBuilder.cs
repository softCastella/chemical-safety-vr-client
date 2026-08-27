using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class PPEBackplateCoverBuilder : EditorWindow
{
    const string DefaultMaterialPath = "Assets/FBX/mask_locker/mask_locker_backplate.mat";
    const string CoverName = "PPE_Backplate_Cover";

    enum CoverFace
    {
        NegativeZ,
        PositiveZ
    }

    GameObject targetObject;
    CoverFace face = CoverFace.NegativeZ;
    float padding = 0.002f;
    bool selectCoverWhenDone = true;

    [MenuItem("Tools/PPE/Backplate Cover Builder")]
    public static void Open()
    {
        var window = GetWindow<PPEBackplateCoverBuilder>("Backplate Cover");
        window.minSize = new Vector2(360f, 220f);
        window.Show();
    }

    void OnEnable()
    {
        if (targetObject == null && Selection.activeGameObject != null)
            targetObject = Selection.activeGameObject;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Backplate Cover Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object", targetObject, typeof(GameObject), true);
        face = (CoverFace)EditorGUILayout.EnumPopup("Face", face);
        padding = EditorGUILayout.Slider("Padding", padding, 0.0005f, 0.03f);
        selectCoverWhenDone = EditorGUILayout.ToggleLeft("Select cover after build", selectCoverWhenDone);

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "The cover is sized from the selected object's renderer bounds and uses the mask_locker backplate material when available.",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(targetObject == null))
        {
            if (GUILayout.Button("Build / Update Cover", GUILayout.Height(32f)))
                BuildOrUpdateCover();
        }
    }

    void BuildOrUpdateCover()
    {
        if (targetObject == null)
            return;

        if (!TryGetLocalBounds(targetObject.transform, out Bounds localBounds))
        {
            EditorUtility.DisplayDialog(
                "No Renderers",
                "Select a GameObject that has Renderer components in its hierarchy.",
                "OK");
            return;
        }

        Transform cover = targetObject.transform.Find(CoverName);
        GameObject coverObject;
        if (cover == null)
        {
            coverObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(coverObject, "Build Backplate Cover");
            coverObject.name = CoverName;
            coverObject.transform.SetParent(targetObject.transform, false);
            coverObject.layer = targetObject.layer;

            Collider collider = coverObject.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
        }
        else
        {
            coverObject = cover.gameObject;
            Undo.RecordObject(coverObject.transform, "Update Backplate Cover");
        }

        Vector3 size = localBounds.size;
        Vector3 center = localBounds.center;
        float zOffset = face == CoverFace.NegativeZ
            ? localBounds.min.z - padding
            : localBounds.max.z + padding;

        coverObject.transform.localPosition = new Vector3(center.x, center.y, zOffset);
        coverObject.transform.localRotation = Quaternion.identity;
        coverObject.transform.localScale = new Vector3(
            Mathf.Max(0.0001f, size.x),
            Mathf.Max(0.0001f, size.y),
            1f);

        MeshRenderer renderer = coverObject.GetComponent<MeshRenderer>();
        Material material = LoadBackplateMaterial();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        EditorUtility.SetDirty(coverObject);
        PrefabUtility.RecordPrefabInstancePropertyModifications(coverObject.transform);

        if (selectCoverWhenDone)
            Selection.activeGameObject = coverObject;

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"Backplate cover updated for {targetObject.name}.", coverObject);
    }

    static bool TryGetLocalBounds(Transform root, out Bounds localBounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Transform existingCover = root.Find(CoverName);
        localBounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (existingCover != null &&
                (renderer.transform == existingCover || renderer.transform.IsChildOf(existingCover)))
                continue;

            Bounds worldBounds = renderer.bounds;
            Vector3[] corners =
            {
                new(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
                new(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
                new(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
                new(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
                new(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
                new(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
                new(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
                new(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = root.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    static Material LoadBackplateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader == null)
            return null;

        material = new Material(shader)
        {
            name = "mask_locker_backplate_runtime"
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.18f, 1f));
        else
            material.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);

        return material;
    }
}
