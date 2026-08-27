using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Clears <c>inspectAnimation</c> references on live PPE action panels when the referenced
/// <see cref="PPEMaskInspectionAnimation"/> lives on a deactivated object.
///
/// A component reference on an inactive GameObject is not null, so the panel still calls
/// <c>TryPlay</c>. That call cannot run its coroutine, yet it reports success, so the inspect
/// completion callback never fires and "use" and "discard" stay blocked. Clearing the reference
/// makes the panel complete the inspect step immediately, which is the intended behaviour when
/// the inspection animation is intentionally switched off.
///
/// Also carries the one-shot helmet use SFX assignment, because that is the other PPE action panel
/// field the demo scene left unset compared to the remaining PPE items.
/// </summary>
public static class PPEActionPanelInspectAnimationRepair
{
    const string InspectAnimationProperty = "inspectAnimation";
    const string UseSfxProperty = "useSfxId";
    const string HelmetObjectName = "PPE_A_Helmet_Strap";
    const string HelmetUseSfxId = "harness";

    [MenuItem("Tools/PPE/Report Blocked Inspect Animation References")]
    public static void Report()
    {
        Run(false);
    }

    [MenuItem("Tools/PPE/Clear Blocked Inspect Animation References")]
    public static void Clear()
    {
        Run(true);
    }

    /// <summary>
    /// Fills the helmet's empty <c>useSfxId</c> so it plays a wear sound like the other PPE items.
    /// The scene stays authoritative: an already authored id is reported and left untouched.
    /// </summary>
    [MenuItem("Tools/PPE/Assign Helmet Use SFX If Empty")]
    public static void AssignHelmetUseSfx()
    {
        PPEActionPanelController[] panels =
            Object.FindObjectsByType<PPEActionPanelController>(FindObjectsInactive.Include);

        StringBuilder report = new("[PPE Action Panel] Helmet use SFX assignment.");
        report.AppendLine();
        int assigned = 0;
        int found = 0;

        foreach (PPEActionPanelController panel in panels)
        {
            if (panel.gameObject.name != HelmetObjectName)
                continue;

            found++;
            string path = Describe(panel);

            SerializedObject serializedPanel = new(panel);
            SerializedProperty property = serializedPanel.FindProperty(UseSfxProperty);
            if (property == null)
            {
                report.AppendLine($"    {path}: '{UseSfxProperty}' not found");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(property.stringValue))
            {
                report.AppendLine($"    {path}: kept authored id '{property.stringValue}'");
                continue;
            }

            Undo.RecordObject(panel, "Assign Helmet Use SFX");
            property.stringValue = HelmetUseSfxId;
            serializedPanel.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            assigned++;
            report.AppendLine($"    {path}: set to '{HelmetUseSfxId}'");
        }

        if (found == 0)
            report.AppendLine($"    no '{HelmetObjectName}' action panel in the open scene(s)");
        else if (assigned > 0)
            report.Append("  Save the scene to persist this change.");

        Debug.Log(report.ToString());
    }

    static void Run(bool applyChanges)
    {
        PPEActionPanelController[] panels =
            Object.FindObjectsByType<PPEActionPanelController>(FindObjectsInactive.Include);

        List<string> blocked = new();
        List<string> healthy = new();
        List<string> skipped = new();

        foreach (PPEActionPanelController panel in panels)
        {
            string path = Describe(panel);

            // Panels that are themselves switched off cannot block the demo.
            if (!panel.isActiveAndEnabled)
            {
                skipped.Add($"{path} (panel is inactive or disabled)");
                continue;
            }

            SerializedObject serializedPanel = new(panel);
            SerializedProperty property = serializedPanel.FindProperty(InspectAnimationProperty);
            if (property == null)
            {
                skipped.Add($"{path} ('{InspectAnimationProperty}' not found)");
                continue;
            }

            if (property.objectReferenceValue is not PPEMaskInspectionAnimation animation)
                continue;

            if (animation.isActiveAndEnabled)
            {
                healthy.Add($"{path} -> {Describe(animation)}");
                continue;
            }

            blocked.Add($"{path} -> {Describe(animation)}");

            if (!applyChanges)
                continue;

            property.objectReferenceValue = null;
            serializedPanel.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        }

        Undo.SetCurrentGroupName("Clear Blocked Inspect Animation References");

        StringBuilder report = new();
        report.AppendLine(applyChanges
            ? $"[PPE Action Panel] Cleared {blocked.Count} blocked inspect animation reference(s)."
            : $"[PPE Action Panel] Found {blocked.Count} blocked inspect animation reference(s).");

        Append(report, "blocked", blocked);
        Append(report, "healthy", healthy);
        Append(report, "not checked", skipped);

        if (applyChanges && blocked.Count > 0)
            report.Append("  Save the scene to persist this change.");

        if (blocked.Count > 0 && !applyChanges)
            Debug.LogWarning(report.ToString());
        else
            Debug.Log(report.ToString());
    }

    static void Append(StringBuilder report, string label, List<string> entries)
    {
        if (entries.Count == 0)
            return;

        report.AppendLine($"  {label}:");
        foreach (string entry in entries)
            report.AppendLine($"    {entry}");
    }

    static string Describe(Component component)
    {
        Transform current = component.transform;
        StringBuilder path = new(current.name);
        while (current.parent != null)
        {
            current = current.parent;
            path.Insert(0, current.name + "/");
        }

        return path.ToString();
    }
}
