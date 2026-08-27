using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(HangarColorVariantController))]
[CanEditMultipleObjects]
public sealed class HangarColorVariantControllerEditor : Editor
{
    SerializedProperty variant;
    SerializedProperty whiteColor;
    SerializedProperty blueColor;
    SerializedProperty customColor;
    SerializedProperty preserveSourceTexture;
    SerializedProperty targetRenderers;

    void OnEnable()
    {
        variant = serializedObject.FindProperty("variant");
        whiteColor = serializedObject.FindProperty("whiteColor");
        blueColor = serializedObject.FindProperty("blueColor");
        customColor = serializedObject.FindProperty("customColor");
        preserveSourceTexture = serializedObject.FindProperty("preserveSourceTexture");
        targetRenderers = serializedObject.FindProperty("targetRenderers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "이 Hangar 인스턴스에만 색상을 적용합니다. 원본 재질과 Transform은 변경하지 않습니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(variant, new GUIContent("Color Version"));

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPresetButton("Original", HangarColorVariantController.ColorVariant.Original);
            DrawPresetButton("White", HangarColorVariantController.ColorVariant.White);
            DrawPresetButton("Blue", HangarColorVariantController.ColorVariant.Blue);
            DrawPresetButton("Custom", HangarColorVariantController.ColorVariant.Custom);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Colors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(whiteColor, new GUIContent("White"));
        EditorGUILayout.PropertyField(blueColor, new GUIContent("Blue"));
        EditorGUILayout.PropertyField(customColor, new GUIContent("Custom"));
        EditorGUILayout.PropertyField(
            preserveSourceTexture,
            new GUIContent("Preserve Source Texture"));

        if (preserveSourceTexture.boolValue)
        {
            EditorGUILayout.HelpBox(
                "원본 텍스처 색과 선택 색이 곱해집니다. 선명한 흰색/파란색 구조물을 보려면 이 옵션을 끄세요.",
                MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(
            targetRenderers,
            new GUIContent("Target Renderers"),
            true);

        bool valuesChanged = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (valuesChanged)
            ApplyAndRepaint();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Collect Child Renderers"))
                CollectChildRenderers();
            if (GUILayout.Button("Reapply Preview"))
                ApplyAndRepaint();
        }
    }

    void DrawPresetButton(
        string label,
        HangarColorVariantController.ColorVariant preset)
    {
        bool selected = !variant.hasMultipleDifferentValues
            && variant.enumValueIndex == (int)preset;

        if (GUILayout.Toggle(selected, label, EditorStyles.miniButton) && !selected)
            variant.enumValueIndex = (int)preset;
    }

    void CollectChildRenderers()
    {
        foreach (Object inspectedTarget in targets)
        {
            var controller = (HangarColorVariantController)inspectedTarget;
            Undo.RecordObject(controller, "Collect Hangar Renderers");
            controller.CollectChildRenderers();
            controller.ApplyNow();
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        serializedObject.Update();
        SceneView.RepaintAll();
    }

    void ApplyAndRepaint()
    {
        foreach (Object inspectedTarget in targets)
        {
            var controller = (HangarColorVariantController)inspectedTarget;
            controller.ApplyNow();
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
