using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PPEHangSuitVisualAppearance))]
[CanEditMultipleObjects]
public sealed class PPEHangSuitVisualAppearanceEditor : Editor
{
    SerializedProperty targetRenderers;
    SerializedProperty color;
    SerializedProperty brightness;
    SerializedProperty applyContaminationStrength;
    SerializedProperty contaminationStrength;

    void OnEnable()
    {
        targetRenderers = serializedObject.FindProperty("targetRenderers");
        color = serializedObject.FindProperty("color");
        brightness = serializedObject.FindProperty("brightness");
        applyContaminationStrength = serializedObject.FindProperty("applyContaminationStrength");
        contaminationStrength = serializedObject.FindProperty("contaminationStrength");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "이 방호복 인스턴스에만 색과 밝기를 적용합니다. 원본 재질 파일과 Transform은 바꾸지 않습니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(color, new GUIContent("Color", "방호복 색. HDR로 더 밝게 올릴 수 있습니다."));
        EditorGUILayout.Slider(brightness, 0f, 2f, new GUIContent("Brightness", "1이 현재 색 기준입니다. 내릴수록 어두워집니다."));
        EditorGUILayout.PropertyField(
            applyContaminationStrength,
            new GUIContent("Apply Contamination", "기존 하자 방호복 오염 재질의 얼룩 강도를 이 오브젝트에만 적용합니다."));
        if (applyContaminationStrength.boolValue)
        {
            EditorGUILayout.Slider(
                contaminationStrength,
                0f,
                1f,
                new GUIContent("Contamination Strength", "0은 얼룩 없음, 1은 기존 하자 방호복과 같은 오염 강도입니다."));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("Target Renderers"), true);

        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (changed)
            ApplyAndRepaint();

        EditorGUILayout.Space();
        if (GUILayout.Button("선택한 방호복에 다시 적용"))
            ApplyAndRepaint();
    }

    void ApplyAndRepaint()
    {
        foreach (Object inspectedTarget in targets)
        {
            var appearance = (PPEHangSuitVisualAppearance)inspectedTarget;
            Undo.RecordObject(appearance, "Hang Suit Appearance");
            appearance.Apply();
            EditorUtility.SetDirty(appearance);
            if (appearance.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(appearance.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
