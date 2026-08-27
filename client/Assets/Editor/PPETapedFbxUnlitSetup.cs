using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts taped.fbx to URP Unlit, places it under the authored worn suit, and wires
/// a PPEEquipmentVisualController slot so it appears in the mirror on tape use.
/// </summary>
public static class PPETapedFbxUnlitSetup
{
    const string TapedFbxPath = "Assets/FBX/taped/PPE_A_Taped.fbx";
    const string TextureFolder = "Assets/FBX/taped";
    const string BaseColorTexturePath = "Assets/FBX/taped/taped_basecolor.JPEG";
    const string MaterialFolder = "Assets/Materials/PPE/Scene Unlit/Taped";
    const string UnlitMaterialPath = MaterialFolder + "/taped_Unlit.mat";
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    const string SuitRootName = "PPE_A_SuitWear";
    const string EquippedChildName = "taped";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    [MenuItem("Tools/PPE/Convert Taped FBX Materials to URP Unlit")]
    public static void Convert()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Taped FBX conversion was cancelled.");
            return;
        }

        ConvertInternal();
        int placed = EnsureEquippedInScene();
        EditorUtility.DisplayDialog(
            "Taped Unlit",
            "Taped FBX materials converted to URP Unlit.\n" +
            $"Scene slot/instance updates: {placed}",
            "OK");
    }

    public static void ConvertBatch()
    {
        ConvertInternal();
        EnsureEquippedInScene();
    }

    [MenuItem("Tools/PPE/Place Taped Under Full Suit And Wire Slot")]
    public static void PlaceAndWire()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Taped placement was cancelled.");
            return;
        }

        ConvertInternal();
        int placed = EnsureEquippedInScene();
        Debug.Log($"Taped placement complete. Updates: {placed}");
    }

    static void ConvertInternal()
    {
        GameObject tapedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TapedFbxPath);
        if (tapedPrefab == null)
            throw new InvalidOperationException($"Taped FBX was not found at '{TapedFbxPath}'.");

        Shader unlitShader = Shader.Find(UnlitShaderName);
        if (unlitShader == null)
            throw new InvalidOperationException($"Shader '{UnlitShaderName}' was not found.");

        EnsureFolder(MaterialFolder);

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);
        if (baseColor == null)
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder });
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path)
                        .IndexOf("basecolor", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    break;
                }
            }
        }

        if (baseColor == null)
            throw new InvalidOperationException($"No taped basecolor texture under '{TextureFolder}'.");

        Material unlit = CreateOrUpdateUnlitMaterial(UnlitMaterialPath, unlitShader, baseColor);

        ModelImporter importer = AssetImporter.GetAtPath(TapedFbxPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"ModelImporter was not found for '{TapedFbxPath}'.");

        var remaps = new List<AssetImporter.SourceAssetIdentifier>();
        var remapMaterials = new List<UnityEngine.Object>();
        var remappedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (Material source in CollectSourceMaterials(TapedFbxPath))
        {
            if (source == null || remappedNames.Contains(source.name))
                continue;

            remaps.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name));
            remapMaterials.Add(unlit);
            remappedNames.Add(source.name);
        }

        // Tripo / FBX embedded names may not be extracted yet.
        string[] fallbackNames =
        {
            "Material",
            "Material_tripo_part_0",
            "taped",
            "Taped",
            "No Name"
        };
        foreach (string fallbackName in fallbackNames)
        {
            if (remappedNames.Contains(fallbackName))
                continue;
            remaps.Add(new AssetImporter.SourceAssetIdentifier(typeof(Material), fallbackName));
            remapMaterials.Add(unlit);
            remappedNames.Add(fallbackName);
        }

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        for (int index = 0; index < remaps.Count; index++)
            importer.AddRemap(remaps[index], remapMaterials[index]);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report =
            $"Taped FBX Unlit conversion complete.\n" +
            $"FBX: {TapedFbxPath}\n" +
            $"Material: {UnlitMaterialPath}\n" +
            $"Remaps: {remaps.Count}\n" +
            $"Shader: {UnlitShaderName}";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/PPETapedFbxUnlitSetup.txt", report);
        Debug.Log(report);
    }

    static int EnsureEquippedInScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform suitRoot = FindNamed(scene, SuitRootName);
        if (suitRoot == null)
            throw new InvalidOperationException($"Scene has no '{SuitRootName}'.");

        // Prefer an existing authored instance (user may already have posed `taped`).
        GameObject tapedInstance =
            FindChildGameObject(suitRoot, EquippedChildName) ??
            FindChildGameObject(suitRoot, "taped_1");
        if (tapedInstance == null)
        {
            GameObject tapedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TapedFbxPath);
            tapedInstance = PrefabUtility.InstantiatePrefab(tapedPrefab, suitRoot) as GameObject;
            if (tapedInstance == null)
                throw new InvalidOperationException("Failed to instantiate taped FBX under the full suit.");

            tapedInstance.name = EquippedChildName;
            Undo.RegisterCreatedObjectUndo(tapedInstance, "Place taped under full suit");
            // Identity under suit — author can nudge pose in Editor after placement.
            tapedInstance.transform.localPosition = Vector3.zero;
            tapedInstance.transform.localRotation = Quaternion.identity;
            tapedInstance.transform.localScale = Vector3.one;
        }
        else if (tapedInstance.name != EquippedChildName)
        {
            tapedInstance.name = EquippedChildName;
        }

        // Start hidden; PPEEquipmentVisualController shows it after tape UseApproved.
        tapedInstance.SetActive(false);
        AssignUnlitMaterials(tapedInstance);

        PPEEquipmentVisualController visual = FindVisualController(scene);
        if (visual == null)
            throw new InvalidOperationException("PPEEquipmentVisualController was not found.");

        PPEItemPresentationBinding tapeBinding = FindPackingTapeBinding(scene);
        if (tapeBinding == null)
            throw new InvalidOperationException("PackingTape PPEItemPresentationBinding was not found.");

        SerializedObject so = new SerializedObject(visual);
        SerializedProperty slots = so.FindProperty("slots");
        if (slots == null)
            throw new InvalidOperationException("PPEEquipmentVisualController.slots was not found.");

        int slotIndex = FindSlotIndex(slots, tapeBinding);
        if (slotIndex < 0)
        {
            slotIndex = slots.arraySize;
            slots.arraySize++;
        }

        SerializedProperty slot = slots.GetArrayElementAtIndex(slotIndex);
        slot.FindPropertyRelative("itemBinding").objectReferenceValue = tapeBinding;
        slot.FindPropertyRelative("equippedChild").objectReferenceValue = tapedInstance;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        return 1;
    }

    static void AssignUnlitMaterials(GameObject root)
    {
        Material unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        if (unlit == null)
            throw new InvalidOperationException($"Missing '{UnlitMaterialPath}'.");

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterials = new[] { unlit };
            }
            else
            {
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = unlit;
                renderer.sharedMaterials = materials;
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    static Material CreateOrUpdateUnlitMaterial(string path, Shader unlitShader, Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(unlitShader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = unlitShader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 2f);

        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material[] CollectSourceMaterials(string fbxPath)
    {
        var materials = new List<Material>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Material material && !materials.Contains(material))
                materials.Add(material);
        }

        string materialsFolder = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/') + "/Materials";
        if (AssetDatabase.IsValidFolder(materialsFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolder });
            foreach (string guid in guids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && !materials.Contains(material))
                    materials.Add(material);
            }
        }

        return materials.ToArray();
    }

    static PPEEquipmentVisualController FindVisualController(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PPEEquipmentVisualController visual =
                root.GetComponentInChildren<PPEEquipmentVisualController>(true);
            if (visual != null)
                return visual;
        }

        return null;
    }

    static PPEItemPresentationBinding FindPackingTapeBinding(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PPEItemPresentationBinding binding in
                     root.GetComponentsInChildren<PPEItemPresentationBinding>(true))
            {
                if (binding != null &&
                    binding.ItemIdentity != null &&
                    binding.ItemIdentity.ItemType == PPEItemType.PackingTape)
                    return binding;
            }
        }

        return null;
    }

    static int FindSlotIndex(SerializedProperty slots, PPEItemPresentationBinding binding)
    {
        for (int i = 0; i < slots.arraySize; i++)
        {
            SerializedProperty itemBinding =
                slots.GetArrayElementAtIndex(i).FindPropertyRelative("itemBinding");
            if (itemBinding != null && itemBinding.objectReferenceValue == binding)
                return i;
        }

        return -1;
    }

    static Transform FindNamed(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindTransform(root.transform, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static Transform FindTransform(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        foreach (Transform child in current)
        {
            Transform match = FindTransform(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static GameObject FindChildGameObject(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}
