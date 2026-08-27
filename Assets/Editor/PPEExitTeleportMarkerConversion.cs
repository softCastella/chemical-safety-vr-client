using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Converts the exit teleport marker from the legacy <see cref="XRLocationTeleportTarget"/>
/// activate-button component to the same XRI <see cref="TeleportationAnchor"/> setup the
/// PPE route markers already use.
///
/// The legacy component only reacted to the XRI activate action and sat on the Default
/// physics layer, while the teleport ray raycasts the "Teleport Target" layer alone. Both
/// had to be true for the joystick to reach the marker, so it was unreachable in play.
/// </summary>
public static class PPEExitTeleportMarkerConversion
{
    const string ExitMarkerName = "Teleport_3_Exit";
    const string TeleportLayerName = "Teleport Target";
    const string RouteMarkerNamePrefix = "XR Location Marker_big_PPE";
    const string AnchorTransformProperty = "m_TeleportAnchorTransform";
    const string ReturnDestinationProperty = "m_ReturnDestination";
    const string RelayMarkerProperty = "m_ExitMarker";
    const string RelayFinaleProperty = "m_FinaleController";
    const string DirectorExitMarkerProperty = "m_ExitTeleportMarker";

    static readonly HashSet<string> IdentityProperties = new()
    {
        "m_ObjectHideFlags",
        "m_CorrespondingSourceObject",
        "m_PrefabInstance",
        "m_PrefabAsset",
        "m_GameObject",
        "m_Script",
        "m_Name",
        "m_EditorClassIdentifier",
        "m_EditorHideFlags",
    };

    [MenuItem("Tools/PPE/Report Exit Teleport Marker Wiring")]
    public static void Report() => Run(false);

    [MenuItem("Tools/PPE/Convert Exit Teleport Marker To Teleportation Anchor")]
    public static void Convert() => Run(true);

    static void Run(bool applyChanges)
    {
        StringBuilder report = new("[PPE Exit Teleport] ");
        report.AppendLine(applyChanges ? "Conversion." : "Report only.");

        GameObject exitMarker = FindExitMarker();
        if (exitMarker == null)
        {
            report.AppendLine($"  no GameObject named '{ExitMarkerName}' in the open scene(s)");
            Debug.LogError(report.ToString());
            return;
        }

        TeleportationAnchor template = FindRouteAnchorTemplate(exitMarker);
        PPEFinaleController finale = Object.FindAnyObjectByType<PPEFinaleController>(FindObjectsInactive.Include);
        Transform returnDestination = finale != null
            ? new SerializedObject(finale).FindProperty(ReturnDestinationProperty).objectReferenceValue as Transform
            : null;
        int teleportLayer = LayerMask.NameToLayer(TeleportLayerName);

        report.AppendLine($"  exit marker: {Describe(exitMarker.transform)}");
        report.AppendLine($"    active={exitMarker.activeInHierarchy} layer={LayerMask.LayerToName(exitMarker.layer)}");
        report.AppendLine($"    components: {string.Join(", ", exitMarker.GetComponents<Component>().Select(c => c == null ? "<missing>" : c.GetType().Name))}");
        report.AppendLine($"  route anchor template: {(template == null ? "NOT FOUND" : Describe(template.transform))}");
        report.AppendLine($"  finale controller: {(finale == null ? "NOT FOUND" : Describe(finale.transform))}");
        report.AppendLine($"  return destination: {(returnDestination == null ? "NOT FOUND" : Describe(returnDestination))}");
        report.AppendLine($"  '{TeleportLayerName}' layer index: {teleportLayer}");

        if (template == null || finale == null || returnDestination == null || teleportLayer < 0)
        {
            report.Append("  Cannot convert until every reference above resolves.");
            Debug.LogError(report.ToString());
            return;
        }

        if (!applyChanges)
        {
            report.Append("  Run 'Tools > PPE > Convert Exit Teleport Marker To Teleportation Anchor' to apply.");
            Debug.Log(report.ToString());
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Convert Exit Teleport Marker");

        if (exitMarker.layer != teleportLayer)
        {
            Undo.RecordObject(exitMarker, "Convert Exit Teleport Marker");
            exitMarker.layer = teleportLayer;
            report.AppendLine($"  layer -> {TeleportLayerName}");
        }
        else
        {
            report.AppendLine($"  layer already {TeleportLayerName}");
        }

        // XRBaseInteractable derivatives cannot coexist on one GameObject, so the
        // legacy target has to go before the anchor is added.
        XRLocationTeleportTarget legacy = exitMarker.GetComponent<XRLocationTeleportTarget>();
        if (legacy != null)
        {
            Undo.DestroyObjectImmediate(legacy);
            report.AppendLine($"  removed {nameof(XRLocationTeleportTarget)}");
        }

        TeleportationAnchor anchor = exitMarker.GetComponent<TeleportationAnchor>();
        if (anchor == null)
        {
            anchor = Undo.AddComponent<TeleportationAnchor>(exitMarker);
            CopySerializedValues(template, anchor);
            report.AppendLine($"  added {nameof(TeleportationAnchor)} copied from the route marker template");
        }
        else
        {
            report.AppendLine($"  kept existing {nameof(TeleportationAnchor)}");
        }

        SerializedObject serializedAnchor = new(anchor);
        SerializedProperty anchorTransform = serializedAnchor.FindProperty(AnchorTransformProperty);
        if (anchorTransform.objectReferenceValue != returnDestination)
        {
            anchorTransform.objectReferenceValue = returnDestination;
            serializedAnchor.ApplyModifiedProperties();
            report.AppendLine($"  anchor transform -> {Describe(returnDestination)}");
        }

        PPEExitTeleportMarkerRelay relay = exitMarker.GetComponent<PPEExitTeleportMarkerRelay>();
        if (relay == null)
        {
            relay = Undo.AddComponent<PPEExitTeleportMarkerRelay>(exitMarker);
            report.AppendLine($"  added {nameof(PPEExitTeleportMarkerRelay)}");
        }

        SerializedObject serializedRelay = new(relay);
        AssignIfEmpty(serializedRelay, RelayMarkerProperty, anchor, report);
        AssignIfEmpty(serializedRelay, RelayFinaleProperty, finale, report);
        serializedRelay.ApplyModifiedProperties();

        PPEVoiceFlowDirector director = Object.FindAnyObjectByType<PPEVoiceFlowDirector>(FindObjectsInactive.Include);
        if (director == null)
        {
            report.AppendLine($"  {nameof(PPEVoiceFlowDirector)} not found: assign '{DirectorExitMarkerProperty}' manually");
        }
        else
        {
            SerializedObject serializedDirector = new(director);
            AssignIfEmpty(serializedDirector, DirectorExitMarkerProperty, exitMarker, report);
            serializedDirector.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(exitMarker);
        EditorSceneManager.MarkSceneDirty(exitMarker.scene);
        Undo.CollapseUndoOperations(undoGroup);

        report.Append("  Save the scene to persist these changes.");
        Debug.Log(report.ToString());
    }

    static void AssignIfEmpty(SerializedObject owner, string propertyPath, Object value, StringBuilder report)
    {
        SerializedProperty property = owner.FindProperty(propertyPath);
        if (property == null)
        {
            report.AppendLine($"  '{propertyPath}' not found on {owner.targetObject.GetType().Name}");
            return;
        }

        if (property.objectReferenceValue != null)
        {
            report.AppendLine($"  kept authored '{propertyPath}' = {property.objectReferenceValue.name}");
            return;
        }

        property.objectReferenceValue = value;
        report.AppendLine($"  '{propertyPath}' -> {value.name}");
    }

    /// <summary>
    /// Mirrors every authored field of the route marker template, including its
    /// interaction layers and teleport provider, so the exit marker behaves
    /// identically to the markers the player already uses. The template itself is
    /// only read.
    /// </summary>
    static void CopySerializedValues(Component source, Component destination)
    {
        SerializedObject from = new(source);
        SerializedObject to = new(destination);
        SerializedProperty iterator = from.GetIterator();

        if (!iterator.NextVisible(true))
            return;

        do
        {
            if (IdentityProperties.Contains(iterator.propertyPath))
                continue;

            to.CopyFromSerializedProperty(iterator);
        }
        while (iterator.NextVisible(false));

        to.ApplyModifiedProperties();
    }

    static GameObject FindExitMarker()
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t.name == ExitMarkerName)
            .Select(t => t.gameObject)
            .FirstOrDefault();
    }

    static TeleportationAnchor FindRouteAnchorTemplate(GameObject exitMarker)
    {
        return Object.FindObjectsByType<TeleportationAnchor>(FindObjectsInactive.Include)
            .Where(a => a.gameObject != exitMarker && a.name.StartsWith(RouteMarkerNamePrefix))
            .OrderBy(a => Describe(a.transform))
            .FirstOrDefault();
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
