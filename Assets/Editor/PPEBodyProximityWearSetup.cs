using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEBodyProximityWearSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";

    [MenuItem("Tools/PPE/Apply Body Proximity Wear To All PPE")]
    public static void ApplyFromMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying body-proximity wear.");
            return;
        }

        int panels = 0;
        foreach (PPEActionPanelController panel in Object.FindObjectsByType<PPEActionPanelController>(
                     FindObjectsInactive.Include))
        {
            SerializedObject so = new SerializedObject(panel);
            so.Update();
            SerializedProperty proximity = so.FindProperty("approveUseByBodyProximity");
            SerializedProperty distance = so.FindProperty("bodyAttachDistance");
            if (proximity == null)
                continue;

            Undo.RecordObject(panel, "Enable body-proximity wear");
            proximity.boolValue = true;
            if (distance != null && distance.floatValue > 0.25f)
                distance.floatValue = 0.25f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
            panels++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Body+trigger wear is on {panels} PPEActionPanelController(s). " +
            "Grab the item, hold it to the body, press Trigger. Contaminated items still reject Use.");
    }

    [MenuItem("Tools/PPE/Apply Named Variant Conditions")]
    public static void ApplyNamedVariantConditions()
    {
        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open '{TargetScenePath}' before applying named variant conditions.");
            return;
        }

        int changed = 0;
        int skipped = 0;
        foreach (PPEItemPresentationBinding binding in Object.FindObjectsByType<PPEItemPresentationBinding>(
                     FindObjectsInactive.Include))
        {
            if (!TryResolveNamedCondition(binding.gameObject.name, out PPEItemCondition condition))
            {
                skipped++;
                continue;
            }

            SerializedObject so = new SerializedObject(binding);
            so.Update();
            SerializedProperty initial = so.FindProperty("initialCondition");
            if (initial == null || initial.enumValueIndex == (int)condition)
                continue;

            Undo.RecordObject(binding, "Apply named PPE variant condition");
            initial.enumValueIndex = (int)condition;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(binding);
            changed++;
            Debug.Log(
                $"Set {binding.gameObject.name} Initial Condition to {condition}.",
                binding);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"Named variant conditions: {changed} updated, {skipped} skipped (no Clean/Contam/Ripped/Helmet_Strap/Helmet_NoStrap suffix).");
    }

    static bool TryResolveNamedCondition(string objectName, out PPEItemCondition condition)
    {
        condition = PPEItemCondition.Clean;
        if (string.IsNullOrEmpty(objectName) || objectName.Contains("_Part_"))
            return false;

        if (objectName.IndexOf("_Clean", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName == "PPE_A_Helmet_Strap")
        {
            condition = PPEItemCondition.Clean;
            return true;
        }

        if (objectName.IndexOf("_Contam", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("_Ripped", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName == "PPE_A_Helmet_NoStrap")
        {
            condition = PPEItemCondition.Contaminated;
            return true;
        }

        return false;
    }
}
