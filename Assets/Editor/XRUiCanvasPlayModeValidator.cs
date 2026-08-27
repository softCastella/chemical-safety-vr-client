using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class XRUiCanvasPlayModeValidator
{
    static readonly string[] CanvasNames =
    {
        "XR UI Canvas",
        "Modal Canvas",
    };

    const string SnapshotKey = "WorldCanvasPlayModeValidator.Snapshots";

    [Serializable]
    struct TransformSnapshot
    {
        public string canvasName;
        public string scenePath;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 anchoredPosition3D;
    }

    [Serializable]
    struct SnapshotList
    {
        public TransformSnapshot[] items;
    }

    static XRUiCanvasPlayModeValidator()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/UI/Validate XR UI Canvas Play Mode Transform")]
    static void ValidateCurrentTransform()
    {
        int found = 0;
        foreach (string canvasName in CanvasNames)
        {
            if (!TryFindCanvas(canvasName, out Canvas canvas))
            {
                Debug.LogWarning($"Could not find {canvasName} in the active scene.");
                continue;
            }

            found++;
            Debug.Log($"{canvasName} authored transform is valid: local={canvas.transform.localPosition}, "
                + $"rotation={canvas.transform.localEulerAngles}, scale={canvas.transform.localScale}.", canvas);
        }

        if (found == 0)
            Debug.LogWarning("No watched world canvases found in the active scene.");
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            CaptureEditModeSnapshots();
        else if (state == PlayModeStateChange.EnteredPlayMode)
            CompareWithEditModeSnapshots();
    }

    static void CaptureEditModeSnapshots()
    {
        var items = new System.Collections.Generic.List<TransformSnapshot>();
        foreach (string canvasName in CanvasNames)
        {
            if (!TryFindCanvas(canvasName, out Canvas canvas) || canvas.transform is not RectTransform rectTransform)
                continue;

            items.Add(new TransformSnapshot
            {
                canvasName = canvasName,
                scenePath = canvas.gameObject.scene.path,
                localPosition = rectTransform.localPosition,
                localRotation = rectTransform.localRotation,
                localScale = rectTransform.localScale,
                anchoredPosition3D = rectTransform.anchoredPosition3D,
            });
        }

        SessionState.SetString(SnapshotKey, JsonUtility.ToJson(new SnapshotList { items = items.ToArray() }));
    }

    static void CompareWithEditModeSnapshots()
    {
        string json = SessionState.GetString(SnapshotKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        SnapshotList list = JsonUtility.FromJson<SnapshotList>(json);
        if (list.items == null)
            return;

        const float positionTolerance = 0.0001f;
        const float rotationTolerance = 0.01f;

        foreach (TransformSnapshot snapshot in list.items)
        {
            if (!TryFindCanvas(snapshot.canvasName, out Canvas canvas) || canvas.transform is not RectTransform rectTransform)
                continue;

            bool changed = snapshot.scenePath != canvas.gameObject.scene.path
                || Vector3.Distance(snapshot.localPosition, rectTransform.localPosition) > positionTolerance
                || Quaternion.Angle(snapshot.localRotation, rectTransform.localRotation) > rotationTolerance
                || Vector3.Distance(snapshot.localScale, rectTransform.localScale) > positionTolerance
                || Vector3.Distance(snapshot.anchoredPosition3D, rectTransform.anchoredPosition3D) > positionTolerance;

            if (changed)
            {
                Debug.LogError($"{snapshot.canvasName} transform changed while entering Play Mode. "
                    + $"Edit local={snapshot.localPosition}, Play local={rectTransform.localPosition}, "
                    + $"Edit anchored={snapshot.anchoredPosition3D}, Play anchored={rectTransform.anchoredPosition3D}.",
                    canvas);
            }
            else
            {
                Debug.Log($"{snapshot.canvasName} transform remained unchanged after entering Play Mode.", canvas);
            }
        }
    }

    static bool TryFindCanvas(string canvasName, out Canvas targetCanvas)
    {
        targetCanvas = null;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return false;

        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.name != canvasName)
                    continue;

                targetCanvas = canvas;
                return true;
            }
        }

        return false;
    }
}
