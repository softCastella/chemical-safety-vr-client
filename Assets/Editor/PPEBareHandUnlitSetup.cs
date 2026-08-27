using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEBareHandUnlitSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string MaterialFolder = "Assets/Materials/XR Hands";
    const string LeftMaterialPath = MaterialFolder + "/PPE_LeftBareHand_LightSkin_Unlit.mat";
    const string RightMaterialPath = MaterialFolder + "/PPE_RightBareHand_LightSkin_Unlit.mat";
    const string HandShaderName = "3D UI Test/XR/Hand Form Unlit";

    [MenuItem("Tools/XR Hands/Apply PPE Bare Hand Form-Shaded Unlit Materials")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        Shader unlitShader = Shader.Find(HandShaderName);
        if (unlitShader == null)
        {
            Debug.LogError($"'{HandShaderName}' shader was not found. PPE bare hand materials were not changed.");
            CloseIfNeeded(scene, openedForSetup);
            return;
        }

        Transform leftHand = FindUnique(scene, "PPE_A_Hand_Bare_L");
        Transform rightHand = FindUnique(scene, "PPE_A_Hand_Bare_R");
        if (leftHand == null || rightHand == null)
        {
            Debug.LogError($"PPE bare hand roots were not found. left={leftHand != null} right={rightHand != null}");
            CloseIfNeeded(scene, openedForSetup);
            return;
        }

        EnsureFolder(MaterialFolder);
        Material leftSource = GetFirstMaterial(leftHand);
        Material rightSource = GetFirstMaterial(rightHand);
        Material leftUnlit = CreateOrUpdateMaterial(LeftMaterialPath, unlitShader, leftSource);
        Material rightUnlit = CreateOrUpdateMaterial(RightMaterialPath, unlitShader, rightSource);

        int leftRendererCount = AssignMaterial(leftHand, leftUnlit);
        int rightRendererCount = AssignMaterial(rightHand, rightUnlit);
        PPEBareHandAppearance appearance = ConfigureAppearance(
            leftHand, rightHand, leftSource, rightSource);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        string report =
            $"PPE bare hand Unlit setup complete.\n" +
            $"Left renderers: {leftRendererCount}, material: {LeftMaterialPath}, color: {GetBaseColor(leftUnlit)}\n" +
            $"Right renderers: {rightRendererCount}, material: {RightMaterialPath}, color: {GetBaseColor(rightUnlit)}\n" +
            $"Appearance component: {GetPath(appearance.transform)}\n" +
            $"Shader: {unlitShader.name}";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/PPEBareHandUnlitSetup.txt", report);
        Debug.Log(report);

        CloseIfNeeded(scene, openedForSetup);
    }

    static Material CreateOrUpdateMaterial(string path, Shader shader, Material source)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool initializeDefinition = material == null || material.shader != shader;
        if (material == null)
        {
            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        Color color = source != null ? GetBaseColor(source) : new Color(0.945f, 0.843f, 0.774f, 1f);
        Texture texture = GetBaseTexture(source);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);
        if (initializeDefinition)
        {
            material.SetFloat("_FormShading", 0.32f);
            material.SetFloat("_EdgeDarkening", 0.38f);
            material.SetFloat("_EdgePower", 3f);
        }
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    static int AssignMaterial(Transform root, Material material)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Undo.RecordObject(renderer, "Apply PPE Bare Hand Unlit Material");
            Material[] slots = renderer.sharedMaterials;
            for (int index = 0; index < slots.Length; index++)
                slots[index] = material;
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
        }
        return renderers.Length;
    }

    static PPEBareHandAppearance ConfigureAppearance(
        Transform leftHand, Transform rightHand, Material leftSource, Material rightSource)
    {
        Transform owner = FindCommonAncestor(leftHand, rightHand);
        PPEBareHandAppearance appearance = owner.GetComponent<PPEBareHandAppearance>();
        bool isNew = appearance == null;
        if (isNew)
            appearance = Undo.AddComponent<PPEBareHandAppearance>(owner.gameObject);

        Renderer leftRenderer = leftHand.GetComponentInChildren<Renderer>(true);
        Renderer rightRenderer = rightHand.GetComponentInChildren<Renderer>(true);
        SerializedObject serialized = new(appearance);
        serialized.FindProperty("leftHandRenderer").objectReferenceValue = leftRenderer;
        serialized.FindProperty("rightHandRenderer").objectReferenceValue = rightRenderer;
        if (isNew)
        {
            serialized.FindProperty("leftHandColor").colorValue = GetBaseColor(leftSource);
            serialized.FindProperty("rightHandColor").colorValue = GetBaseColor(rightSource);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        appearance.Apply();
        EditorUtility.SetDirty(appearance);
        return appearance;
    }

    static Transform FindCommonAncestor(Transform left, Transform right)
    {
        var leftAncestors = new HashSet<Transform>();
        for (Transform current = left; current != null; current = current.parent)
            leftAncestors.Add(current);

        for (Transform current = right; current != null; current = current.parent)
        {
            if (leftAncestors.Contains(current))
                return current;
        }

        return left.root;
    }

    static Material GetFirstMaterial(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null)
                    return material;
            }
        }
        return null;
    }

    static Color GetBaseColor(Material material)
    {
        if (material != null && material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material != null && material.HasProperty("_Color"))
            return material.GetColor("_Color");
        return Color.white;
    }

    static Texture GetBaseTexture(Material material)
    {
        if (material != null && material.HasProperty("_BaseMap"))
            return material.GetTexture("_BaseMap");
        if (material != null && material.HasProperty("_MainTex"))
            return material.GetTexture("_MainTex");
        return null;
    }

    static Transform FindUnique(Scene scene, string objectName)
    {
        var matches = new List<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == objectName)
                    matches.Add(transform);
            }
        }

        if (matches.Count != 1)
            Debug.LogError($"Expected one '{objectName}' in {ScenePath}, found {matches.Count}.");
        return matches.Count == 1 ? matches[0] : null;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }

    static void CloseIfNeeded(Scene scene, bool openedForSetup)
    {
        if (openedForSetup && scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);
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
