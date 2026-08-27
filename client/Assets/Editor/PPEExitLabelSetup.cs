using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the authored EXIT label once on the locomotion scene's exit marker.
/// Existing authored values are preserved on subsequent runs.
/// </summary>
[InitializeOnLoad]
public static class PPEExitLabelSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity";
    const string ExitMarkerName = "Teleport_3_Exit";
    const string LabelName = "EXIT";
    const string FontGuid = "b6215b980cc39504ca6af8141ee7c242";
    const string ExitTextMaterialPath = "Assets/Materials/XR Markers/EXIT Text Marker.mat";
    const string SessionSetupKey = "PPEExitLabelSetup_v3";

    static PPEExitLabelSetup()
    {
        EditorApplication.delayCall += SetupOpenTargetSceneOnce;
    }

    static void SetupOpenTargetSceneOnce()
    {
        if (SessionState.GetBool(SessionSetupKey, false))
            return;

        SessionState.SetBool(SessionSetupKey, true);
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            return;

        bool wasDirty = scene.isDirty;
        if (!TryCreateLabel(scene, out bool changed) || !changed)
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty)
            EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[{nameof(PPEExitLabelSetup)}] Created authored EXIT label on '{ExitMarkerName}'. " +
            (wasDirty ? "The scene was already dirty; save it from Unity to persist the label." : "The scene was saved."));
    }

    [MenuItem("Tools/PPE/Setup EXIT Label On Teleport Marker")]
    public static void SetupActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"[{nameof(PPEExitLabelSetup)}] Open only '{ScenePath}' before setup.");
            return;
        }

        if (TryCreateLabel(scene, out bool changed) && changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[{nameof(PPEExitLabelSetup)}] Created authored EXIT label on '{ExitMarkerName}'. Save the scene to persist it.");
        }
    }

    public static void BuildBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!TryCreateLabel(scene, out bool changed))
            throw new InvalidOperationException($"Could not create the EXIT label in '{ScenePath}'.");

        if (changed)
            EditorSceneManager.SaveScene(scene);

        Debug.Log($"[{nameof(PPEExitLabelSetup)}] Batch setup complete. changed={changed}");
    }

    static bool TryCreateLabel(Scene scene, out bool changed)
    {
        changed = false;
        GameObject exitMarker = FindSceneObject(scene, ExitMarkerName);
        if (exitMarker == null)
        {
            Debug.LogError($"[{nameof(PPEExitLabelSetup)}] Required '{ExitMarkerName}' was not found.");
            return false;
        }

        Transform labelTransform = exitMarker.transform.Find(LabelName);
        PPEBlinkingWorldText pulse;
        TextMeshPro label;

        if (labelTransform == null)
        {
            GameObject labelObject = new(LabelName);
            labelObject.transform.SetParent(exitMarker.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.24f, 0.035f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one;

            label = labelObject.AddComponent<TextMeshPro>();
            pulse = labelObject.AddComponent<PPEBlinkingWorldText>();
            Undo.RegisterCreatedObjectUndo(labelObject, "Create EXIT Teleport Label");
            changed = true;
        }
        else
        {
            label = labelTransform.GetComponent<TextMeshPro>();
            pulse = labelTransform.GetComponent<PPEBlinkingWorldText>();
            if (label == null || pulse == null)
            {
                Debug.LogError($"[{nameof(PPEExitLabelSetup)}] Existing '{exitMarker.name}/{LabelName}' is missing its authored TMP/pulse component.");
                return false;
            }

        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            AssetDatabase.GUIDToAssetPath(FontGuid));
        if (font == null)
        {
            Debug.LogError($"[{nameof(PPEExitLabelSetup)}] TMP font GUID '{FontGuid}' could not be resolved.");
            return false;
        }

        if (label.font != font)
        {
            label.font = font;
            changed = true;
        }

        if (label.text != LabelName)
        {
            label.text = LabelName;
            changed = true;
        }

        if (label.fontSize != 0.4f)
        {
            label.fontSize = 0.4f;
            changed = true;
        }

        if (label.fontStyle != FontStyles.Bold)
        {
            label.fontStyle = FontStyles.Bold;
            changed = true;
        }

        if (label.alignment != TextAlignmentOptions.Center)
        {
            label.alignment = TextAlignmentOptions.Center;
            changed = true;
        }

        if (label.textWrappingMode != TextWrappingModes.NoWrap)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            changed = true;
        }

        if (label.raycastTarget)
        {
            label.raycastTarget = false;
            changed = true;
        }

        Material exitTextMaterial = EnsureExitTextMaterial(label.fontSharedMaterial);
        if (exitTextMaterial != null && label.fontSharedMaterial != exitTextMaterial)
        {
            label.fontSharedMaterial = exitTextMaterial;
            changed = true;
        }

        Color markerColor = new Color(0.07475026f, 0.9257383f, 0.06550034f, 1f);
        if (label.color != markerColor)
        {
            label.color = markerColor;
            changed = true;
        }

        if (pulse.TargetText != label)
        {
            pulse.TargetText = label;
            EditorUtility.SetDirty(pulse);
            changed = true;
        }

        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(exitMarker);
        return true;
    }

    static Material EnsureExitTextMaterial(Material sourceMaterial)
    {
        Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (overlayShader == null || sourceMaterial == null)
        {
            Debug.LogError(
                $"[{nameof(PPEExitLabelSetup)}] Could not create the EXIT text material. " +
                "The TMP Distance Field Overlay shader or source font material is missing.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(ExitTextMaterialPath);
        if (material == null)
        {
            material = new Material(sourceMaterial)
            {
                name = "EXIT Text Marker"
            };
            AssetDatabase.CreateAsset(material, ExitTextMaterialPath);
        }

        if (material.shader != overlayShader)
        {
            material.shader = overlayShader;
            EditorUtility.SetDirty(material);
        }

        if (material.HasProperty("_FaceColor") && material.GetColor("_FaceColor") != Color.white)
        {
            material.SetColor("_FaceColor", Color.white);
            EditorUtility.SetDirty(material);
        }

        if (material.HasProperty("_OutlineWidth") && material.GetFloat("_OutlineWidth") != 0f)
        {
            material.SetFloat("_OutlineWidth", 0f);
            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        return material;
    }

    static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform target in transforms)
            {
                if (target.name == objectName)
                    return target.gameObject;
            }
        }

        return null;
    }
}
