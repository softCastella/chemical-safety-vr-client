using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PPERoomMirrorDiagnostics
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string OutputFolder = "Logs/PPEMirrorDiagnostics";
    const int Width = 960;
    const int Height = 720;

    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory(OutputFolder);

        Camera mainCamera = Camera.main;
        GameObject root = GameObject.Find("PPE_Room_Planar_Mirror");
        PlanarMirrorRenderer mirror = root != null
            ? root.GetComponentInChildren<PlanarMirrorRenderer>(true)
            : null;

        if (mainCamera == null || mirror == null || mirror.TargetTexture == null)
        {
            Debug.LogError($"Mirror diagnostics missing references. main={mainCamera != null} mirror={mirror != null} texture={mirror?.TargetTexture != null}");
            return;
        }

        Camera closeCamera = CreateCloseVerificationCamera(mainCamera, mirror.transform);
        try
        {
            mirror.RenderForCamera(closeCamera);
            SaveRenderTexture("reflection_texture", mirror.TargetTexture);
            CaptureCamera("mirror_close_composited", closeCamera, mirror);
            CaptureCamera("main_camera_composited", mainCamera, mirror);
        }
        finally
        {
            Object.DestroyImmediate(closeCamera.gameObject);
        }
    }

    static Camera CreateCloseVerificationCamera(Camera template, Transform mirrorSurface)
    {
        GameObject cameraObject = new("PPE_Mirror_Close_Verification_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.CopyFrom(template);
        camera.targetTexture = null;
        camera.fieldOfView = 48f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 40f;
        camera.cullingMask |= 1 << mirrorSurface.gameObject.layer;

        Vector3 normal = mirrorSurface.forward.normalized;
        float mainSide = Mathf.Sign(Vector3.Dot(normal, template.transform.position - mirrorSurface.position));
        if (Mathf.Approximately(mainSide, 0f))
            mainSide = -1f;

        Vector3 position = mirrorSurface.position +
                           normal * mainSide * 5.2f -
                           mirrorSurface.right * 3.1f +
                           mirrorSurface.up * 0.05f;
        Vector3 target = mirrorSurface.position + mirrorSurface.right * 0.15f;
        camera.transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation((target - position).normalized, mirrorSurface.up));
        return camera;
    }

    static void CaptureCamera(string name, Camera camera, PlanarMirrorRenderer mirror)
    {
        RenderTexture output = new(Width, Height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false);

        try
        {
            mirror.RenderForCamera(camera);
            camera.targetTexture = output;
            camera.Render();
            RenderTexture.active = output;
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();

            string path = Path.Combine(OutputFolder, $"{name}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"PPE mirror diagnostics wrote {path}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(texture);
        }
    }

    static void SaveRenderTexture(string name, RenderTexture source)
    {
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = new(source.width, source.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = source;
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            texture.Apply();

            string path = Path.Combine(OutputFolder, $"{name}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Debug.Log($"PPE mirror diagnostics wrote {path}");
        }
        finally
        {
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
        }
    }
}
