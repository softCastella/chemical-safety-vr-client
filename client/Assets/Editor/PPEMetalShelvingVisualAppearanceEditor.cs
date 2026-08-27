using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(PPEMetalShelvingVisualAppearance))]
[CanEditMultipleObjects]
public sealed class PPEMetalShelvingVisualAppearanceEditor : Editor
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string ShelvingRootName = "PPE_B_MetalShelving";
    const int ExpectedPartCount = 30;

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
            "이 선반 인스턴스에만 색과 밝기를 적용합니다. 원본 Unlit 재질, 메시, Transform, 나무판은 바꾸지 않습니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(color, new GUIContent("Color", "선반 색. HDR로 더 밝게 올릴 수 있습니다."));
        EditorGUILayout.Slider(brightness, 0f, 2f, new GUIContent("Brightness", "1이 현재 텍스처 색 기준입니다. 내릴수록 어두워집니다."));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("Target Renderers"), true);

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (changed)
            ApplyAndRepaint();

        EditorGUILayout.Space();
        if (GUILayout.Button("선택한 선반에 다시 적용"))
            ApplyAndRepaint();
    }

    void ApplyAndRepaint()
    {
        foreach (Object inspectedTarget in targets)
        {
            var appearance = (PPEMetalShelvingVisualAppearance)inspectedTarget;
            Undo.RecordObject(appearance, "Metal Shelving Appearance");
            appearance.Apply();
            EditorUtility.SetDirty(appearance);
            if (appearance.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(appearance.gameObject.scene);
        }

        SceneView.RepaintAll();
    }

    [MenuItem("Tools/PPE/Configure Metal Shelving Color Controls")]
    public static void ConfigureFromMenu()
    {
        Scene scene = ResolveTargetScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Transform root = FindNamed(scene, ShelvingRootName);
        if (root == null)
        {
            Debug.LogError($"Loaded scene has no object named '{ShelvingRootName}'.");
            return;
        }

        Renderer[] partRenderers = PPEMetalShelvingVisualAppearance.CollectPartRenderers(root);
        if (partRenderers.Length != ExpectedPartCount)
        {
            Debug.LogError(
                $"'{ShelvingRootName}' should have {ExpectedPartCount} tripo_part renderers, found {partRenderers.Length}.");
            return;
        }

        PPEMetalShelvingVisualAppearance appearance =
            root.GetComponent<PPEMetalShelvingVisualAppearance>();
        Color startColor = Color.white;
        float startBrightness = 1f;
        if (appearance != null)
        {
            startColor = appearance.Color;
            startBrightness = appearance.Brightness;
        }
        else
        {
            appearance = Undo.AddComponent<PPEMetalShelvingVisualAppearance>(root.gameObject);
        }

        Undo.RecordObject(appearance, "Configure metal shelving appearance");
        appearance.ConfigureForEditor(partRenderers, startColor, startBrightness);
        EditorUtility.SetDirty(appearance);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "Metal shelving color controls are on PPE_B_MetalShelving. " +
            "Select it and adjust Color / Brightness in the Inspector, then save the scene. " +
            "Wooden planks are not included.");
    }

    static Scene ResolveTargetScene()
    {
        Scene loaded = SceneManager.GetSceneByPath(TargetScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            return loaded;

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.path == TargetScenePath && active.isLoaded)
            return active;

        Debug.LogError($"Open '{TargetScenePath}' before configuring metal shelving colors.");
        return default;
    }

    static Transform FindNamed(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindNamed(root.transform, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    static Transform FindNamed(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        foreach (Transform child in current)
        {
            Transform match = FindNamed(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }
}
