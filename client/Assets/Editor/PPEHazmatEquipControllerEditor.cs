using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PPEHazmatEquipController))]
public sealed class PPEHazmatEquipControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PPEHazmatEquipController controller = (PPEHazmatEquipController)target;

        EditorGUILayout.HelpBox(
            "이 오브젝트(hazmat_suit_on)의 Transform이 착용 완료 위치·회전·크기입니다. " +
            "Scene View에서 이 오브젝트를 직접 조정하면 별도 Tools 메뉴 없이 최종 Pose가 바뀝니다.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Animating", controller.IsAnimating.ToString());
        EditorGUILayout.LabelField("Equipped", controller.IsEquipped.ToString());

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Validate Hazmat Equip Setup"))
                PPEHazmatEquipValidation.Validate();
        }
    }
}
