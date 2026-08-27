using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEGloveSuitAppearanceSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string MaterialFolder = "Assets/Materials/XR Hands";
    const string SleeveMaterialPath =
        MaterialFolder + "/PPE_HandVariant_Sleeve_Unlit.mat";
    const string CuffMaterialPath =
        MaterialFolder + "/PPE_HandVariant_Cuff_Black_Unlit.mat";
    const string HandShaderName = "3D UI Test/XR/Hand Form Unlit";
    const string SleeveRendererName = "Yellow_Protective_Sleeve";

    static readonly string[] ActiveGloveSuitRootNames =
    {
        "PPE_A_Hand_GloveSuit_L",
        "PPE_A_Hand_GloveSuit_R",
    };

    static readonly string[] BareHandSuitRootNames =
    {
        "PPE_A_Hand_Suit_L",
        "PPE_A_Hand_Suit_R",
    };

    static readonly Color WarmSleeveColor =
        new(0.88f, 0.60f, 0.035f, 1f);
    static readonly Color BlackRubberCuffColor =
        new(0.025f, 0.028f, 0.025f, 1f);

    [MenuItem("Tools/PPE/Apply Warm Glove Suit And Black Cuffs")]
    public static void ApplyFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying the glove suit appearance.");
            return;
        }

        Apply(scene);
    }

    [MenuItem("Tools/PPE/Validate Glove Suit Appearance")]
    public static void ValidateFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before validating the glove suit appearance.");
            return;
        }

        Validate(scene);
    }

    static void Apply(Scene scene)
    {
        Material sleeveMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(SleeveMaterialPath);
        if (sleeveMaterial == null)
            throw new MissingReferenceException(
                $"Sleeve material was not found: {SleeveMaterialPath}");
        if (sleeveMaterial.shader == null ||
            sleeveMaterial.shader.name != HandShaderName)
        {
            throw new InvalidOperationException(
                $"'{SleeveMaterialPath}' must use '{HandShaderName}'.");
        }

        Undo.RecordObject(sleeveMaterial, "Warm PPE Glove Suit Yellow");
        SetColor(sleeveMaterial, WarmSleeveColor);
        EditorUtility.SetDirty(sleeveMaterial);

        Material cuffMaterial = GetOrCreateCuffMaterial(sleeveMaterial.shader);

        foreach (string rootName in ActiveGloveSuitRootNames)
        {
            GameObject handRoot = FindUnique(scene, rootName);
            SkinnedMeshRenderer sleeveRenderer =
                FindUniqueRenderer(handRoot, SleeveRendererName);

            Material[] materials = sleeveRenderer.sharedMaterials;
            if (materials.Length != 2)
            {
                throw new InvalidOperationException(
                    $"'{GetPath(sleeveRenderer.transform)}' must have two material slots " +
                    $"(sleeve and integrated cuff), but has {materials.Length}.");
            }

            Undo.RecordObject(sleeveRenderer, "Restore PPE Black Wrist Cuff");
            // The imported Glove_Suit mesh exposes the integrated cuff first,
            // followed by the gathered yellow sleeve submesh.
            materials[0] = cuffMaterial;
            materials[1] = sleeveMaterial;
            sleeveRenderer.sharedMaterials = materials;
            EditorUtility.SetDirty(sleeveRenderer);
        }

        foreach (string rootName in BareHandSuitRootNames)
        {
            GameObject handRoot = FindUnique(scene, rootName);
            SkinnedMeshRenderer sleeveRenderer =
                FindUniqueRenderer(handRoot, SleeveRendererName);
            Material[] materials = sleeveRenderer.sharedMaterials;
            if (materials.Length != 2)
                throw new InvalidOperationException(
                    $"'{GetPath(sleeveRenderer.transform)}' must have two material slots, " +
                    $"but has {materials.Length}.");

            Undo.RecordObject(sleeveRenderer, "Restore Bare PPE Black Wrist Cuff");
            materials[0] = cuffMaterial;
            materials[1] = sleeveMaterial;
            sleeveRenderer.sharedMaterials = materials;
            EditorUtility.SetDirty(sleeveRenderer);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"PPE glove suit scene dirty before save: {scene.path} = {scene.isDirty}");
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{TargetScenePath}'.");
        // SaveOpenScenes flushes the same serialized scene through Unity's active
        // scene pipeline as well; this avoids leaving a corrected in-memory order
        // behind when the scene was loaded during an asset refresh.
        if (!EditorSceneManager.SaveOpenScenes())
            throw new InvalidOperationException("Unity failed to flush the open PPE scene.");

        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log(
            "PPE glove suit appearance updated: warm protective yellow retained, " +
            "and the integrated black rubber cuff was restored on both active hands.");
    }

    static Material GetOrCreateCuffMaterial(Shader shader)
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(CuffMaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "PPE_HandVariant_Cuff_Black_Unlit",
            };
            AssetDatabase.CreateAsset(material, CuffMaterialPath);
        }
        else
        {
            Undo.RecordObject(material, "Configure PPE Black Wrist Cuff");
            material.shader = shader;
        }

        SetColor(material, BlackRubberCuffColor);
        SetFloat(material, "_FormShading", 0.22f);
        SetFloat(material, "_EdgeDarkening", 0.18f);
        SetFloat(material, "_EdgePower", 3f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void Validate(Scene scene)
    {
        Material sleeveMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(SleeveMaterialPath);
        Material cuffMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CuffMaterialPath);
        if (sleeveMaterial == null || cuffMaterial == null)
            throw new MissingReferenceException("Glove suit appearance materials are missing.");

        if (!Approximately(GetColor(sleeveMaterial), WarmSleeveColor))
        {
            throw new InvalidOperationException(
                $"Sleeve material color does not match the warm-yellow reference target.");
        }
        if (!Approximately(GetColor(cuffMaterial), BlackRubberCuffColor))
        {
            throw new InvalidOperationException(
                "Integrated cuff material is not the authored black rubber color.");
        }
        if (cuffMaterial.shader == null ||
            cuffMaterial.shader.name != HandShaderName)
        {
            throw new InvalidOperationException(
                $"Black cuff material must use '{HandShaderName}'.");
        }

        var failures = new List<string>();
        foreach (string rootName in ActiveGloveSuitRootNames)
        {
            GameObject handRoot = FindUnique(scene, rootName);
            if (!handRoot.activeSelf)
                failures.Add($"'{rootName}' is not active.");

            SkinnedMeshRenderer sleeveRenderer =
                FindUniqueRenderer(handRoot, SleeveRendererName);
            Material[] materials = sleeveRenderer.sharedMaterials;
            if (materials.Length != 2)
            {
                failures.Add(
                    $"'{GetPath(sleeveRenderer.transform)}' has {materials.Length} material slots.");
                continue;
            }

            if (materials[0] != cuffMaterial)
                failures.Add($"'{rootName}' slot 0 is not the black cuff material.");
            if (materials[1] != sleeveMaterial)
                failures.Add($"'{rootName}' slot 1 is not the warm sleeve material.");
            if (sleeveRenderer.rootBone == null)
                failures.Add($"'{rootName}' sleeve renderer has no root bone.");

            Transform[] bones = sleeveRenderer.bones;
            for (int index = 0; index < bones.Length; index++)
            {
                if (bones[index] == null)
                    failures.Add($"'{rootName}' sleeve bone {index} is missing.");
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"PPE glove suit appearance validation failed:\n" +
                string.Join("\n", failures));
        }

        Debug.Log(
            "PPE glove suit appearance validation passed: warm yellow sleeve, " +
            "black integrated cuff, two active hands, and intact sleeve bones.");
    }

    static GameObject FindUnique(Scene scene, string objectName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName)
                    continue;
                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"More than one '{objectName}' exists in '{scene.path}'.");
                }

                match = candidate.gameObject;
            }
        }

        if (match == null)
            throw new MissingReferenceException(
                $"'{objectName}' was not found in '{scene.path}'.");
        return match;
    }

    static SkinnedMeshRenderer FindUniqueRenderer(
        GameObject root,
        string rendererObjectName)
    {
        SkinnedMeshRenderer match = null;
        foreach (SkinnedMeshRenderer renderer in
                 root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.name != rendererObjectName)
                continue;
            if (match != null)
            {
                throw new InvalidOperationException(
                    $"More than one '{rendererObjectName}' renderer exists under '{root.name}'.");
            }

            match = renderer;
        }

        if (match == null)
            throw new MissingReferenceException(
                $"'{rendererObjectName}' renderer was not found under '{root.name}'.");
        return match;
    }

    static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    static Color GetColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        throw new InvalidOperationException(
            $"Material '{material.name}' has no supported color property.");
    }

    static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    static bool Approximately(Color left, Color right)
    {
        const float tolerance = 0.0001f;
        return Mathf.Abs(left.r - right.r) <= tolerance
            && Mathf.Abs(left.g - right.g) <= tolerance
            && Mathf.Abs(left.b - right.b) <= tolerance
            && Mathf.Abs(left.a - right.a) <= tolerance;
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
