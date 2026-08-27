using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PPERoomSignatureDiagnostics
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string OutputFolder = "Logs/PPESignatureDiagnostics";
    const int Width = 804;
    const int Height = 1180;

    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory(OutputFolder);

        GameObject tablet = GameObject.Find("Tablet (1)");
        Transform documentPlane = tablet != null ? tablet.transform.Find("GeneratedPlane") : null;
        HandwrittenSignatureSequence sequence = documentPlane != null
            ? documentPlane.GetComponentInChildren<HandwrittenSignatureSequence>(true)
            : null;

        if (documentPlane == null || sequence == null)
        {
            Debug.LogError($"PPE signature diagnostics missing references. document={documentPlane != null} sequence={sequence != null}");
            return;
        }

        Camera camera = CreateDocumentCamera(documentPlane);
        try
        {
            SetAllSteps(sequence, 0f);
            CaptureCamera("signature_00_empty", camera);

            for (int i = 0; i < 5; i++)
                sequence.SetStepReveal(i, i < 3 ? 1f : 0f);
            sequence.SetStepReveal(3, 0.62f);
            CaptureCamera("signature_01_checklist_writing", camera);

            for (int i = 0; i < 5; i++)
                sequence.SetStepReveal(i, 1f);
            sequence.SetStepReveal(5, 0.62f);
            CaptureCamera("signature_02_player_writing", camera);

            sequence.SetStepReveal(5, 1f);
            sequence.SetStepReveal(6, 0.58f);
            CaptureCamera("signature_03_conductor_writing", camera);

            sequence.SetStepReveal(6, 1f);
            sequence.SetStepReveal(7, 0.58f);
            CaptureCamera("signature_04_checker_writing", camera);

            SetAllSteps(sequence, 1f);
            CaptureCamera("signature_05_complete", camera);
        }
        finally
        {
            Object.DestroyImmediate(camera.gameObject);
        }
    }

    static void SetAllSteps(HandwrittenSignatureSequence sequence, float reveal)
    {
        for (int i = 0; i < 8; i++)
            sequence.SetStepReveal(i, reveal);
    }

    static Camera CreateDocumentCamera(Transform documentPlane)
    {
        Renderer renderer = documentPlane.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        Vector3 front = -documentPlane.forward.normalized;

        GameObject cameraObject = new("PPE_Signature_Diagnostics_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.aspect = (float)Width / Height;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.2f, 0.21f, 1f);
        camera.cullingMask = -1;
        camera.transform.SetPositionAndRotation(
            bounds.center + front * 0.5f,
            Quaternion.LookRotation(-front, documentPlane.up));

        Vector3[] corners =
        {
            documentPlane.TransformPoint(new Vector3(-5f, -2f, 0f)),
            documentPlane.TransformPoint(new Vector3(5f, -2f, 0f)),
            documentPlane.TransformPoint(new Vector3(-5f, 2f, 0f)),
            documentPlane.TransformPoint(new Vector3(5f, 2f, 0f))
        };
        float halfWidth = 0f;
        float halfHeight = 0f;
        foreach (Vector3 corner in corners)
        {
            Vector3 cameraCorner = camera.transform.InverseTransformPoint(corner);
            halfWidth = Mathf.Max(halfWidth, Mathf.Abs(cameraCorner.x));
            halfHeight = Mathf.Max(halfHeight, Mathf.Abs(cameraCorner.y));
        }

        camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / camera.aspect) * 1.01f;
        return camera;
    }

    static void CaptureCamera(string name, Camera camera)
    {
        RenderTexture renderTexture = new(Width, Height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();

            string path = Path.Combine(OutputFolder, $"{name}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"PPE signature diagnostics wrote {path}");
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);
        }
    }
}
