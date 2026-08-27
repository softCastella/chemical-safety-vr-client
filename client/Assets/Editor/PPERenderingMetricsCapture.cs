using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time Editor-only rendering snapshot for the PPE room. It never changes the scene.
/// On the next Play Mode run it waits for the user to frame the PPE, then averages the
/// Game View statistics for five seconds and writes one result to the Unity Console.
/// </summary>
[InitializeOnLoad]
public static class PPERenderingMetricsCapture
{
    const string PendingKey = "PPE.RenderingMetricsCapture.Pending";
    const string CompletedKey = "PPE.RenderingMetricsCapture.Completed";
    const double SettleSeconds = 12d;
    const double SampleSeconds = 5d;
    const double SampleInterval = 0.25d;

    static bool measuring;
    static double sampleStartTime;
    static double nextSampleTime;
    static int sampleCount;
    static long drawCallTotal;
    static long triangleTotal;
    static long vertexTotal;

    static PPERenderingMetricsCapture()
    {
        if (!SessionState.GetBool(CompletedKey, false))
            SessionState.SetBool(PendingKey, true);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode ||
            !SessionState.GetBool(PendingKey, false))
            return;

        measuring = true;
        sampleCount = 0;
        drawCallTotal = 0;
        triangleTotal = 0;
        vertexTotal = 0;
        sampleStartTime = EditorApplication.timeSinceStartup + SettleSeconds;
        nextSampleTime = sampleStartTime;
        Debug.Log("[PPE METRICS] Next Play Mode capture armed. Frame the PPE in Game View; sampling starts in 12 seconds.");
    }

    static void Update()
    {
        if (!measuring || !EditorApplication.isPlaying)
            return;

        double now = EditorApplication.timeSinceStartup;
        if (now < sampleStartTime || now < nextSampleTime)
            return;

        drawCallTotal += UnityStats.drawCalls;
        triangleTotal += UnityStats.triangles;
        vertexTotal += UnityStats.vertices;
        sampleCount++;
        nextSampleTime = now + SampleInterval;

        if (now < sampleStartTime + SampleSeconds)
            return;

        measuring = false;
        SessionState.SetBool(PendingKey, false);
        SessionState.SetBool(CompletedKey, true);
        Debug.Log($"[PPE METRICS] Editor Game View, {sampleCount} samples: " +
            $"DrawCalls={Average(drawCallTotal)}, " +
            $"Triangles={Average(triangleTotal)}, Vertices={Average(vertexTotal)}. " +
            "This is an Editor/Quest Link baseline, not an Android Quest device result.");
    }

    static long Average(long total) => sampleCount > 0 ? total / sampleCount : 0;
}
