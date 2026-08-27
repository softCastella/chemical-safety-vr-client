using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Matches the PPE education/training/test mode buttons to the authored colour of
/// the "PPE 착용교육" button that opens them, so one modal does not mix a neutral
/// grey step with saturated ones.
///
/// The reference colour is read from the scene rather than declared here: the
/// authored button stays the single source of truth for the modal palette.
/// </summary>
public static class PPEModalButtonColorUnification
{
    const string ReferenceProperty = "incompletePpeButton";

    static readonly string[] TargetProperties =
    {
        "ppeEducationModeButton",
        "ppeTrainingModeButton",
        "ppeTestModeButton",
        "ppeModeChoiceBackButton",
    };

    [MenuItem("Tools/PPE/Report Modal Button Colors")]
    public static void Report() => Run(false);

    [MenuItem("Tools/PPE/Unify PPE Mode Button Colors")]
    public static void Unify() => Run(true);

    static void Run(bool applyChanges)
    {
        ScenarioDetailModal[] modals =
            Object.FindObjectsByType<ScenarioDetailModal>(FindObjectsInactive.Include);

        StringBuilder report = new("[PPE Modal Colors] ");
        report.AppendLine(applyChanges ? "Unification." : "Report only.");

        int configuredModals = 0;
        int changed = 0;

        foreach (ScenarioDetailModal modal in modals)
        {
            SerializedObject serializedModal = new(modal);
            Button reference = serializedModal.FindProperty(ReferenceProperty)?.objectReferenceValue as Button;
            List<Button> targets = new();
            foreach (string property in TargetProperties)
            {
                if (serializedModal.FindProperty(property)?.objectReferenceValue is Button button)
                    targets.Add(button);
            }

            if (reference == null || targets.Count == 0)
                continue;

            configuredModals++;
            report.AppendLine($"  modal: {Describe(modal.transform)}");

            Graphic referenceGraphic = ResolveGraphic(reference);
            if (referenceGraphic == null)
            {
                report.AppendLine($"    reference '{reference.name}' has no target graphic");
                continue;
            }

            Color referenceColor = referenceGraphic.color;
            report.AppendLine($"    reference: {reference.name} = {Describe(referenceColor)}");

            foreach (Button target in targets)
            {
                Graphic graphic = ResolveGraphic(target);
                if (graphic == null)
                {
                    report.AppendLine($"    {target.name}: no target graphic");
                    continue;
                }

                if (graphic.color == referenceColor)
                {
                    report.AppendLine($"    {target.name}: already {Describe(referenceColor)}");
                    continue;
                }

                report.AppendLine($"    {target.name}: {Describe(graphic.color)} -> {Describe(referenceColor)}");
                if (!applyChanges)
                    continue;

                Undo.RecordObject(graphic, "Unify PPE Mode Button Colors");
                graphic.color = referenceColor;
                EditorUtility.SetDirty(graphic);
                EditorSceneManager.MarkSceneDirty(graphic.gameObject.scene);
                changed++;
            }
        }

        if (configuredModals == 0)
        {
            report.Append($"  no {nameof(ScenarioDetailModal)} with both '{ReferenceProperty}' and mode buttons in the open scene(s)");
            Debug.LogWarning(report.ToString());
            return;
        }

        report.Append(applyChanges
            ? changed > 0 ? "  Save the scene to persist these changes." : "  Nothing to change."
            : "  Run 'Tools > PPE > Unify PPE Mode Button Colors' to apply.");
        Debug.Log(report.ToString());
    }

    static Graphic ResolveGraphic(Button button)
    {
        return button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
    }

    static string Describe(Color color)
    {
        return $"({color.r:0.##}, {color.g:0.##}, {color.b:0.##}, {color.a:0.##})";
    }

    static string Describe(Transform target)
    {
        List<string> parts = new();
        for (Transform current = target; current != null; current = current.parent)
            parts.Add(current.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}
