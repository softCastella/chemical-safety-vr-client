using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(PPEGloveVisualAppearance))]
[CanEditMultipleObjects]
public sealed class PPEGloveVisualAppearanceEditor : Editor
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string DisplayParentName = "PPE";
    const string ChemicalGloveChildName = "Chemical_Glove";

    static readonly string[] DisplayGloveNames =
    {
        "PPE_A_Glove_L",
        "PPE_A_Glove_R",
        "PPE_A_InnerGlove_L",
        "PPE_A_InnerGlove_R",
    };

    static readonly string[] RequiredHandRootNames =
    {
        "PPE_A_Hand_GloveSuit_L",
        "PPE_A_Hand_GloveSuit_R",
        "PPE_A_Hand_GloveTape_L",
        "PPE_A_Hand_GloveTape_R",
    };

    static readonly string[] OptionalHandRootNames =
    {
        "PPE_A_Hand_InnerGloveSuit_L",
        "PPE_A_Hand_InnerGloveSuit_R",
        "PPE_A_Hand_InnerGlove_L",
        "PPE_A_Hand_InnerGlove_R",
    };

    SerializedProperty targetRenderers;
    SerializedProperty color;
    SerializedProperty brightness;

    void OnEnable()
    {
        targetRenderers = serializedObject.FindProperty("targetRenderers");
        color = serializedObject.FindProperty("color");
        brightness = serializedObject.FindProperty("brightness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "이 장갑 인스턴스에만 색과 밝기를 적용합니다. 원본 재질 파일, 메시, Transform은 바꾸지 않습니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(color, new GUIContent("Color", "장갑 색. HDR로 더 밝게 올릴 수 있습니다."));
        EditorGUILayout.Slider(brightness, 0f, 2f, new GUIContent("Brightness", "1이 현재 색 기준입니다. 내릴수록 어두워집니다."));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("Target Renderers"), true);

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (changed)
            ApplyAndRepaint();

        EditorGUILayout.Space();
        if (GUILayout.Button("선택한 장갑에 다시 적용"))
            ApplyAndRepaint();
    }

    void ApplyAndRepaint()
    {
        foreach (UnityEngine.Object inspectedTarget in targets)
        {
            var appearance = (PPEGloveVisualAppearance)inspectedTarget;
            Undo.RecordObject(appearance, "Glove Appearance");
            appearance.Apply();
            EditorUtility.SetDirty(appearance);
            if (appearance.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(appearance.gameObject.scene);
        }

        SceneView.RepaintAll();
    }

    [MenuItem("Tools/PPE/Configure Glove Color Controls")]
    public static void ConfigureFromMenu()
    {
        Scene scene = ResolveTargetScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        int wired = 0;
        foreach (string displayName in DisplayGloveNames)
        {
            GameObject display = FindChildOfNamedParent(scene, DisplayParentName, displayName);
            if (display == null)
            {
                if (displayName.Contains("InnerGlove", StringComparison.Ordinal))
                    continue;

                Debug.LogError(
                    $"Loaded scene has no '{DisplayParentName}/{displayName}'. " +
                    "Open the Train/Test mask scene before configuring glove colors.");
                return;
            }

            Renderer renderer = display.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError($"'{GetPath(display.transform)}' has no Renderer on the root.");
                return;
            }

            Wire(display, new[] { renderer });
            wired++;
        }

        foreach (string handRootName in RequiredHandRootNames)
        {
            if (!TryWireHandRoot(scene, handRootName, required: true))
                return;
            wired++;
        }

        foreach (string handRootName in OptionalHandRootNames)
        {
            if (TryWireHandRoot(scene, handRootName, required: false))
                wired++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Glove color controls are on {wired} display/hand glove objects. " +
            "Select PPE_A_Glove_L/R for the stand, or PPE_A_Hand_GloveSuit/Tape for the hands, " +
            "then adjust Color / Brightness and save the scene. " +
            "SuitWear body gloves, sleeves, and tape meshes are not included.");
    }

    static bool TryWireHandRoot(Scene scene, string handRootName, bool required)
    {
        GameObject handRoot = FindUniqueNamed(scene, handRootName);
        if (handRoot == null)
        {
            if (!required)
                return false;

            Debug.LogError($"Loaded scene has no object named '{handRootName}'.");
            return false;
        }

        Renderer gloveRenderer = FindUniqueChildRenderer(handRoot, ChemicalGloveChildName);
        if (gloveRenderer == null)
        {
            Debug.LogError(
                $"'{GetPath(handRoot.transform)}' has no unique '{ChemicalGloveChildName}' renderer.");
            return false;
        }

        Wire(handRoot, new[] { gloveRenderer });
        return true;
    }

    static void Wire(GameObject owner, Renderer[] renderers)
    {
        PPEGloveVisualAppearance appearance = owner.GetComponent<PPEGloveVisualAppearance>();
        Color startColor = PPEGloveVisualAppearance.ReadMaterialColor(renderers[0], Color.white);
        float startBrightness = 1f;
        if (appearance != null)
        {
            startColor = appearance.Color;
            startBrightness = appearance.Brightness;
        }
        else
        {
            appearance = Undo.AddComponent<PPEGloveVisualAppearance>(owner);
        }

        Undo.RecordObject(appearance, "Configure glove appearance");
        appearance.ConfigureForEditor(renderers, startColor, startBrightness);
        EditorUtility.SetDirty(appearance);
    }

    static Scene ResolveTargetScene()
    {
        Scene loaded = SceneManager.GetSceneByPath(TargetScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            return loaded;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.path == TargetScenePath && active.isLoaded)
            return active;

        Debug.LogError($"Open '{TargetScenePath}' before configuring glove colors.");
        return default;
    }

    static GameObject FindChildOfNamedParent(Scene scene, string parentName, string childName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != childName ||
                    candidate.parent == null ||
                    candidate.parent.name != parentName)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"More than one '{parentName}/{childName}' exists in '{scene.path}'.");
                }

                match = candidate.gameObject;
            }
        }

        return match;
    }

    static GameObject FindUniqueNamed(Scene scene, string objectName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName &&
                    candidate.name.Trim() != objectName)
                    continue;
                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"More than one '{objectName}' exists in '{scene.path}'.");
                }

                match = candidate.gameObject;
            }
        }

        return match;
    }

    static Renderer FindUniqueChildRenderer(GameObject root, string childName)
    {
        Renderer match = null;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.gameObject.name != childName)
                continue;
            if (match != null)
                return null;

            match = renderer;
        }

        return match;
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
