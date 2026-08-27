using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PPEHelmetEquipController))]
public sealed class PPEHelmetEquipControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PPEHelmetEquipController controller = (PPEHelmetEquipController)target;

        EditorGUILayout.HelpBox(
            "이 오브젝트(helmet_on)의 Transform이 착용 완료 Pose입니다. " +
            "착용 헬멧은 HMD 본 화면에서는 숨고 거울에서만 표시됩니다.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Animating", controller.IsAnimating.ToString());
        EditorGUILayout.LabelField("Equipped", controller.IsEquipped.ToString());

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Validate Helmet Equip Setup"))
                PPEHelmetEquipValidation.Validate();
        }
    }
}
