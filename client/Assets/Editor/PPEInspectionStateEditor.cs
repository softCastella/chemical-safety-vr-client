using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PPEInspectionState))]
public sealed class PPEInspectionStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PPEInspectionState inspectionState = (PPEInspectionState)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Condition Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Current Condition", inspectionState.CurrentCondition.ToString());
        EditorGUILayout.LabelField("Is Being Inspected", inspectionState.IsBeingInspected.ToString());

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to preview Clean or Contaminated appearance.",
                MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Set Contaminated"))
                SetCondition(inspectionState, PPEItemCondition.Contaminated);

            if (GUILayout.Button("Set Clean"))
                SetCondition(inspectionState, PPEItemCondition.Clean);
        }

        bool isHazmat =
            inspectionState.PresentationBinding != null &&
            inspectionState.PresentationBinding.ItemIdentity != null &&
            inspectionState.PresentationBinding.ItemIdentity.ItemType == PPEItemType.HazmatSuit;
        if (isHazmat)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hazmat Authored Setup", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Validate Hazmat Inspection Setup"))
                {
                    PPEHazmatRoleBindingSetup.Validate();
                    PPEHazmatActionPanelValidation.Validate();
                }
            }
        }

        bool isHelmet =
            inspectionState.PresentationBinding != null &&
            inspectionState.PresentationBinding.ItemIdentity != null &&
            inspectionState.PresentationBinding.ItemIdentity.ItemType ==
                PPEItemType.ConstructionHelmet;
        if (!isHelmet)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Helmet Authored Setup", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Validate Helmet Inspection Setup"))
                PPEHelmetEquipValidation.Validate();
        }
    }

    static void SetCondition(
        PPEInspectionState inspectionState,
        PPEItemCondition condition)
    {
        inspectionState.SetCondition(condition);
        SceneView.RepaintAll();
    }
}
