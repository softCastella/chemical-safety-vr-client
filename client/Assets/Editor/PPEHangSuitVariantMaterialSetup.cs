using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEHangSuitVariantMaterialSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string CleanMaterialPath = "Assets/FBX/hazmat suit/PPE_A_SuitHang_Material.mat";
    const string ContamMaterialPath = "Assets/Materials/PPE/HazmatSuit_Inspection.mat";
    const float RippedBrightness = 0.7f;

    [MenuItem("Tools/PPE/Configure Hang Suit Color Controls")]
    public static void ConfigureFromMenu()
    {
        Scene scene = ResolveTargetScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Material clean = LoadMaterial(CleanMaterialPath);
        Material contam = LoadMaterial(ContamMaterialPath);
        if (clean == null || contam == null)
            return;

        int configured = 0;
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (renderer == null || !IsHangSuit(renderer.gameObject.name))
                continue;

            string objectName = renderer.gameObject.name;
            bool isContam = objectName.Contains("SuitHang_Contam");
            bool isRipped = IsRipped(objectName, renderer.sharedMaterial);

            if (isContam)
            {
                Undo.RecordObject(renderer, "Assign hang suit contamination material");
                renderer.sharedMaterial = contam;
                EditorUtility.SetDirty(renderer);
            }

            PPEHangSuitVisualAppearance appearance =
                renderer.GetComponent<PPEHangSuitVisualAppearance>();
            if (appearance == null)
                appearance = Undo.AddComponent<PPEHangSuitVisualAppearance>(renderer.gameObject);

            Color startColor = ReadMaterialColor(renderer.sharedMaterial, Color.white);
            float brightness = isRipped ? RippedBrightness : 1f;
            if (isRipped)
                startColor = ReadMaterialColor(clean, Color.white);

            Undo.RecordObject(appearance, "Configure hang suit appearance");
            appearance.ConfigureForEditor(
                new[] { renderer },
                startColor,
                brightness,
                isContam,
                1f);
            EditorUtility.SetDirty(appearance);
            configured++;
        }

        if (configured == 0)
        {
            Debug.LogError(
                "No SuitHang_Clean / SuitHang_Contam / SuitHang_Ripped renderers were found in the loaded scene.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Hang-suit color controls are on {configured} suit(s). " +
            "Select each suit and adjust Color / Brightness in the Inspector, then save the scene. " +
            "Ripped brightness starts at 0.7 because the tear albedo was too bright.");
    }

    [MenuItem("Tools/PPE/Apply Hang Suit Variant Materials")]
    public static void ApplyFromMenu()
    {
        ConfigureFromMenu();
    }

    static Scene ResolveTargetScene()
    {
        Scene loaded = SceneManager.GetSceneByPath(TargetScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            return loaded;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.isLoaded)
            return active;

        Debug.LogError($"Open '{TargetScenePath}' before configuring hang-suit colors.");
        return default;
    }

    static bool IsHangSuit(string objectName)
    {
        return objectName.Contains("SuitHang_Clean") ||
               objectName.Contains("SuitHang_Contam") ||
               objectName.Contains("SuitHang_Ripped");
    }

    static bool IsRipped(string objectName, Material material)
    {
        if (objectName.Contains("SuitHang_Ripped"))
            return true;

        return material != null && material.name.Contains("ArmTear");
    }

    static Color ReadMaterialColor(Material material, Color fallback)
    {
        if (material == null)
            return fallback;
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        return fallback;
    }

    static Material LoadMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            Debug.LogError($"Missing material '{path}'.");
        return material;
    }
}
