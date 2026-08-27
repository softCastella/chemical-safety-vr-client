using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// Captures the hand shape currently produced by hand tracking and writes it out as a
/// single-frame AnimationClip, so a controller-held hand can be animated between poses
/// (open / grip) that were authored with a real hand instead of by hand in a DCC tool.
/// Hand tracking only reports joints in Play mode, so this window only works while playing.
/// </summary>
public class HandPoseClipRecorder : EditorWindow
{
    [SerializeField] Handedness m_Handedness = Handedness.Left;
    [SerializeField] XRHandSkeletonDriver m_Driver;
    [SerializeField] bool m_AutoFindDriver = true;

    [SerializeField] string m_ClipFolder = "Assets/HandPoses";
    [SerializeField] string m_ClipName = "HandPose_Grip";

    [MenuItem("Tools/Hand Pose/Clip Recorder")]
    static void Open() => GetWindow<HandPoseClipRecorder>("Hand Pose Clip Recorder");

    void OnEnable() => EditorApplication.update += OnEditorUpdate;
    void OnDisable() => EditorApplication.update -= OnEditorUpdate;

    void OnEditorUpdate()
    {
        // The driver state changes every frame while tracking; keep the status readout live.
        if (Application.isPlaying)
            Repaint();
    }

    XRHandSkeletonDriver ResolveDriver()
    {
        if (!m_AutoFindDriver)
            return m_Driver;

        var drivers = FindObjectsByType<XRHandSkeletonDriver>(FindObjectsInactive.Include);
        foreach (var driver in drivers)
        {
            var events = driver.handTrackingEvents;
            if (events != null && events.handedness == m_Handedness)
                return driver;
        }

        return null;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        m_Handedness = (Handedness)EditorGUILayout.EnumPopup("Handedness", m_Handedness);
        m_AutoFindDriver = EditorGUILayout.Toggle("Auto Find Driver", m_AutoFindDriver);

        using (new EditorGUI.DisabledScope(m_AutoFindDriver))
            m_Driver = (XRHandSkeletonDriver)EditorGUILayout.ObjectField("Driver", m_Driver, typeof(XRHandSkeletonDriver), true);

        EditorGUILayout.Space();
        DrawStatus(out var driver);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        m_ClipFolder = EditorGUILayout.TextField("Clip Folder", m_ClipFolder);
        m_ClipName = EditorGUILayout.TextField("Clip Name", m_ClipName);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(driver == null))
        {
            if (GUILayout.Button("Export Current Pose -> AnimationClip", GUILayout.Height(30)))
                ExportClip(driver);
        }
    }

    void DrawStatus(out XRHandSkeletonDriver driver)
    {
        driver = null;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode with hand tracking active.", MessageType.Info);
            return;
        }

        driver = ResolveDriver();
        if (driver == null)
        {
            EditorGUILayout.HelpBox(
                $"No XRHandSkeletonDriver found for {m_Handedness}. Add the 'Left/Right Hand Tracking' prefab " +
                "from the XR Hands HandVisualizer sample to the scene.",
                MessageType.Warning);
            return;
        }

        var jointCount = driver.jointTransformReferences?.Count ?? 0;
        EditorGUILayout.HelpBox($"Driver: {driver.name}   joints: {jointCount}", MessageType.None);
    }

    /// <summary>
    /// Bakes each tracked joint's current local rotation into a constant curve. Paths are relative to
    /// the hand model root, so the clip replays on any hierarchy with matching bone names.
    /// </summary>
    void ExportClip(XRHandSkeletonDriver driver)
    {
        var wrist = driver.rootTransform;
        var handRoot = wrist != null ? wrist.parent : null;
        if (wrist == null || handRoot == null)
        {
            Debug.LogWarning("[HandPose] The driver has no rootTransform (wrist) parent, so clip paths cannot be built.");
            return;
        }

        var clip = new AnimationClip { frameRate = 30f };
        var boneCount = 0;

        foreach (var reference in driver.jointTransformReferences)
        {
            var joint = reference.jointTransform;
            if (joint == null || SkipJoint(reference.xrHandJointID))
                continue;

            var path = AnimationUtility.CalculateTransformPath(joint, handRoot);
            var rotation = joint.localRotation;

            SetConstantCurve(clip, path, "localRotation.x", rotation.x);
            SetConstantCurve(clip, path, "localRotation.y", rotation.y);
            SetConstantCurve(clip, path, "localRotation.z", rotation.z);
            SetConstantCurve(clip, path, "localRotation.w", rotation.w);
            boneCount++;
        }

        if (boneCount == 0)
        {
            Debug.LogWarning("[HandPose] No joints were captured. Is the hand actually being tracked?");
            return;
        }

        EnsureFolder(m_ClipFolder);
        var fileName = string.IsNullOrWhiteSpace(m_ClipName) ? "HandPose" : m_ClipName;
        var assetPath = $"{m_ClipFolder}/{fileName}.anim";
        clip.name = fileName;

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Overwrite pose?",
                    $"{assetPath} already exists.\n\n" +
                    "Overwriting keeps the asset's GUID, so blend trees already pointing at it pick up the " +
                    "new pose. Saving under a new name would leave them on the old one.",
                    "Overwrite", "Cancel"))
                return;

            // Copying into the existing asset rather than replacing the file is what preserves the GUID.
            EditorUtility.CopySerialized(clip, existing);
            clip = existing;
            EditorUtility.SetDirty(clip);
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
        }

        AssetDatabase.SaveAssets();

        EditorGUIUtility.PingObject(clip);
        Debug.Log($"[HandPose] Saved {assetPath} ({boneCount} bones).");
    }

    /// <summary>
    /// The wrist would move the whole hand rather than shape it, and tips drive no skin weights.
    /// </summary>
    static bool SkipJoint(XRHandJointID id)
        => id == XRHandJointID.Wrist || id == XRHandJointID.Palm || id.ToString().EndsWith("Tip");

    static void SetConstantCurve(AnimationClip clip, string path, string property, float value)
        => clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(new Keyframe(0f, value)));

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        var parts = folder.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
