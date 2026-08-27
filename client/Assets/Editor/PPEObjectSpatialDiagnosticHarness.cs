using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Read-only diagnostic for the selected scene object. It deliberately does not
/// move, enable, disable, or repair anything. Use it before changing input or UI
/// logic so a spatial/serialization problem is not misclassified as an event bug.
/// </summary>
public static class PPEObjectSpatialDiagnosticHarness
{
    [MenuItem("Tools/PPE/Diagnose Selected Object Spatial Context")]
    public static void DiagnoseSelectedObject()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            Debug.LogError("PPE spatial diagnosis requires a selected scene GameObject.");
            return;
        }

        Diagnose(target);
    }

    public static string Diagnose(GameObject target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        StringBuilder report = new();
        Scene scene = target.scene;
        report.AppendLine("PPE object spatial diagnosis (read-only)");
        report.AppendLine($"Object: {GetPath(target.transform)}");
        report.AppendLine($"Scene: {scene.path}");
        report.AppendLine($"Active: self={target.activeSelf}, hierarchy={target.activeInHierarchy}");
        report.AppendLine($"Layer: {target.layer} ({LayerMask.LayerToName(target.layer)})");
        report.AppendLine($"Tag: {target.tag}");

        Transform transform = target.transform;
        report.AppendLine($"Local: position={transform.localPosition}, rotation={transform.localEulerAngles}, scale={transform.localScale}");
        report.AppendLine($"World: position={transform.position}, rotation={transform.eulerAngles}, lossyScale={transform.lossyScale}");
        AppendParentChain(report, transform);

        if (transform is RectTransform rectTransform)
        {
            report.AppendLine(
                $"RectTransform: rect={rectTransform.rect}, anchoredPosition={rectTransform.anchoredPosition}, " +
                $"sizeDelta={rectTransform.sizeDelta}, anchorMin={rectTransform.anchorMin}, " +
                $"anchorMax={rectTransform.anchorMax}, pivot={rectTransform.pivot}");
            report.AppendLine($"Rect world corners: {FormatWorldCorners(rectTransform)}");
        }

        AppendCanvasContext(report, target);
        AppendComponentContext(report, target);
        AppendBounds(report, target);
        AppendRoomCandidates(report, target, scene);

        report.AppendLine("Conclusion: this report is diagnostic only; no Transform, UI, input, or active state was changed.");
        string result = report.ToString();
        Debug.Log(result, target);
        return result;
    }

    private static void AppendParentChain(StringBuilder report, Transform transform)
    {
        report.AppendLine("Parent chain:");
        Transform current = transform;
        while (current != null)
        {
            report.AppendLine(
                $"  - {GetPath(current)} | activeSelf={current.gameObject.activeSelf} | " +
                $"worldPosition={current.position}");
            current = current.parent;
        }
    }

    private static void AppendCanvasContext(StringBuilder report, GameObject target)
    {
        Canvas[] canvases = target.GetComponentsInParent<Canvas>(true);
        if (canvases.Length == 0)
        {
            report.AppendLine("Canvas context: none in parent chain.");
            return;
        }

        report.AppendLine("Canvas context:");
        foreach (Canvas canvas in canvases)
        {
            report.AppendLine(
                $"  - {GetPath(canvas.transform)} | enabled={canvas.enabled} | active={canvas.isActiveAndEnabled} | " +
                $"renderMode={canvas.renderMode} | worldCamera={(canvas.worldCamera != null ? canvas.worldCamera.name : "<null>")} | " +
                $"sortingOrder={canvas.sortingOrder} | scale={canvas.transform.lossyScale}");
        }
    }

    private static void AppendComponentContext(StringBuilder report, GameObject target)
    {
        Component[] components = target.GetComponentsInChildren<Component>(true);
        report.AppendLine($"Component context: {components.Length} component(s) on target and descendants.");
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            string details = string.Empty;
            if (component is Graphic graphic)
                details = $" | raycastTarget={graphic.raycastTarget} | enabled={graphic.enabled}";
            else if (component is GraphicRaycaster graphicRaycaster)
                details = $" | enabled={graphicRaycaster.enabled}";
            else if (component is Renderer renderer)
                details = $" | enabled={renderer.enabled} | bounds={renderer.bounds}";
            else if (component is Collider collider)
                details = $" | enabled={collider.enabled} | bounds={collider.bounds}";

            report.AppendLine($"  - {GetPath(component.transform)} : {component.GetType().FullName}{details}");
        }
    }

    private static void AppendBounds(StringBuilder report, GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        report.AppendLine($"Bounds: rendererCount={renderers.Length}, colliderCount={colliders.Length}");

        if (TryGetRendererBounds(renderers, out Bounds rendererBounds))
            report.AppendLine($"  Renderer aggregate: center={rendererBounds.center}, size={rendererBounds.size}, min={rendererBounds.min}, max={rendererBounds.max}");
        if (TryGetColliderBounds(colliders, out Bounds colliderBounds))
            report.AppendLine($"  Collider aggregate: center={colliderBounds.center}, size={colliderBounds.size}, min={colliderBounds.min}, max={colliderBounds.max}");
    }

    private static void AppendRoomCandidates(StringBuilder report, GameObject target, Scene scene)
    {
        List<GameObject> candidates = new();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                string lowerName = child.name.ToLowerInvariant();
                if (lowerName.Contains("room") || lowerName.Contains("룸"))
                    candidates.Add(child.gameObject);
            }
        }

        report.AppendLine($"Room-bound candidates: {candidates.Count}");
        foreach (GameObject candidate in candidates)
        {
            Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
            if (!TryGetRendererBounds(renderers, out Bounds bounds))
                continue;

            bool containsOrigin = bounds.Contains(target.transform.position);
            report.AppendLine(
                $"  - {GetPath(candidate.transform)} | bounds center={bounds.center}, size={bounds.size} | " +
                $"targetWorldPositionInside={containsOrigin}");
        }
    }

    private static bool TryGetRendererBounds(Renderer[] renderers, out Bounds result)
    {
        result = default;
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                result = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool TryGetColliderBounds(Collider[] colliders, out Bounds result)
    {
        result = default;
        bool hasBounds = false;
        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            if (!hasBounds)
            {
                result = collider.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static string FormatWorldCorners(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return $"BL={corners[0]}, TL={corners[1]}, TR={corners[2]}, BR={corners[3]}";
    }

    private static string GetPath(Transform transform)
    {
        List<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
