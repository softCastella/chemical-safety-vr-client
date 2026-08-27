#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class XRUIJitterProbeSetup
{
    [MenuItem("Tools/XR/Diagnostics/Add UI Jitter Probe To Selected")]
    static void AddToSelected()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            Debug.LogError("[XR UI Jitter] Select the Canvas or UI root to diagnose first.");
            return;
        }

        if (target.GetComponentInParent<Canvas>(true) == null && target.GetComponent<Canvas>() == null)
        {
            Debug.LogError("[XR UI Jitter] The selected object is not a Canvas or a child of one.", target);
            return;
        }

        var probe = target.GetComponent<XRUIJitterProbe>();
        if (probe == null)
            probe = Undo.AddComponent<XRUIJitterProbe>(target);

        Selection.activeObject = probe;
        EditorGUIUtility.PingObject(probe);
        Debug.Log($"[XR UI Jitter] Probe is ready on '{target.name}'. In Play Mode, use its Begin Jitter Capture context menu.", target);
    }
}
#endif
