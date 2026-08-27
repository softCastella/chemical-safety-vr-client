using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MixerRoomFrontCapture
{
    const string ScenePath = "Assets/Scenes/5_MixerRoom_Unlit.unity";
    const string OutputPath = "Captures/5_MixerRoom_Unlit_Front.png";

    [MenuItem("Tools/Mixer Room/Capture Front Camera")]
    public static void CaptureFrontCamera()
    {
        var scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            Debug.LogError("Mixer room front capture failed: no camera found.");
            return;
        }

        const int width = 1920;
        const int height = 1080;
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            string absolutePath = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.Refresh();
            Debug.Log($"Mixer room front capture saved: {absolutePath}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }
}
