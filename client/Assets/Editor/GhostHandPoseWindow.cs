using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ProjectHandPoseData = ThreeDUI.HandPoses.HandPoseData;

/// <summary>
/// Captures a grab pose from hand tracking: freezes the shape your hand is currently making, drops a ghost
/// hand into the grabbed object as a child, and writes the joint rotations out to a HandPoseData asset.
///
/// The ghost is an instance of the GhostLeftHand / GhostRightHand prefabs rather than a copy of the tracked
/// hand. Cloning the live hand means stripping tracking components, re-freezing skinned meshes and fixing
/// up the world frame that Instantiate drops - and the result still has a different skeleton from whatever
/// hand the game actually grabs with. Posing the same prefab the game uses avoids all of it.
///
/// The UI is deliberately three fields and one button. Everything else has a working default and sits
/// behind a foldout.
/// </summary>
public class GhostHandPoseWindow : EditorWindow
{
    const string k_GhostLeftGuid = "97f3f8cc3b12dc64387d4f76f7952233";
    const string k_GhostRightGuid = "3be0926ccca085e429614776250544d3";

    [SerializeField] GameObject m_Target;
    [SerializeField] string m_PoseName = "GrabPose";
    [SerializeField] Handedness m_Handedness = Handedness.Left;

    [SerializeField] bool m_ShowOptions;
    [SerializeField] XRHandSkeletonDriver m_DriverOverride;
    [SerializeField] GameObject m_GhostOverride;
    [SerializeField] string m_SaveFolder = "Assets/HandPoses";
    [SerializeField] string m_PrefabFolder = "Assets/HandPoses/Prefabs";
    [SerializeField] ProjectHandPoseData m_ApplySource;

    string PoseName => string.IsNullOrWhiteSpace(m_PoseName) ? "GrabPose" : m_PoseName;

    [MenuItem("Tools/Hand Pose/Ghost Hand Recorder")]
    static void Open() => GetWindow<GhostHandPoseWindow>("Ghost Hand Recorder");

    void OnEnable() => EditorApplication.update += OnEditorUpdate;
    void OnDisable() => EditorApplication.update -= OnEditorUpdate;

    void OnEditorUpdate()
    {
        // Tracking state changes every frame while playing; keep the status line honest.
        if (Application.isPlaying)
            Repaint();
    }

    void OnGUI()
    {
        var driver = Application.isPlaying ? ResolveDriver() : null;
        var blocker = Blocker(driver);

        DrawStatus(driver, blocker);

        EditorGUILayout.Space(6);
        m_Target = (GameObject)EditorGUILayout.ObjectField("Grab This", m_Target, typeof(GameObject), true);
        m_Handedness = (Handedness)EditorGUILayout.EnumPopup("Hand", m_Handedness);
        m_PoseName = EditorGUILayout.TextField("Pose Name", m_PoseName);

        EditorGUILayout.Space(10);
        DrawCapture(blocker);

        EditorGUILayout.Space(10);
        DrawApply();

        EditorGUILayout.Space(10);
        DrawOptions();
    }

    /// <summary>
    /// The one thing worth reading at a glance: whether a capture would work right now, and if not, what to
    /// fix. Returned as the message so the buttons can disable on exactly the same condition.
    /// </summary>
    string Blocker(XRHandSkeletonDriver driver)
    {
        if (!Application.isPlaying)
            return "Enter Play mode - hand tracking is off until then.";

        if (driver == null)
            return $"No {m_Handedness} hand tracked yet. Hold it up where the headset can see it.";

        if (m_Target == null)
            return "Set 'Grab This' to the object the hand should hold.";

        return null;
    }

    void DrawStatus(XRHandSkeletonDriver driver, string blocker)
    {
        if (blocker != null)
        {
            EditorGUILayout.HelpBox(blocker, Application.isPlaying ? MessageType.Warning : MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox($"Ready - {m_Handedness} hand tracked, will attach to '{m_Target.name}'.", MessageType.Info);
    }

    void DrawCapture(string blocker)
    {
        using (new EditorGUI.DisabledScope(blocker != null))
            if (GUILayout.Button("Capture", GUILayout.Height(44)))
                Capture();
    }

    /// <summary>
    /// The half of the workflow that has to happen outside Play mode.
    ///
    /// Capture writes the pose asset and the ghost prefab to disk, and those survive - but the ghost in the
    /// scene, the attach transform and the reference from XRGrabInteractable are scene objects, and Unity
    /// throws away every scene change made during Play. Without this step the object is still grabbed by
    /// its centre and nothing about the console output says why.
    /// </summary>
    void DrawApply()
    {
        EditorGUILayout.LabelField("Apply to scene (after Play)", EditorStyles.boldLabel);

        m_ApplySource = (ProjectHandPoseData)EditorGUILayout.ObjectField(
            "Pose", m_ApplySource, typeof(ProjectHandPoseData), false);

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Scene changes made in Play mode are discarded. Exit Play, then apply.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(Application.isPlaying || m_ApplySource == null || m_Target == null))
            if (GUILayout.Button("Apply", GUILayout.Height(28)))
                Apply(m_ApplySource, m_Target.transform);

        DrawConvert();
    }

    /// <summary>
    /// Offered only when it would change something: the target still has the stock interactable, which has
    /// a single attach slot and therefore cannot hold a left and a right grip at once.
    /// </summary>
    void DrawConvert()
    {
        if (Application.isPlaying || m_Target == null)
            return;

        var grab = m_Target.GetComponentInParent<XRGrabInteractable>();
        if (grab == null || grab is HandedGrabInteractable)
            return;

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("XRGrabInteractable has one attach slot, so only one hand can be right.", MessageType.Info);

        if (GUILayout.Button("Convert to Handed Grab Interactable"))
            ConvertToHanded(grab);
    }

    /// <summary>
    /// Swaps the stock interactable for the handed one, keeping every setting.
    ///
    /// Routed through a throwaway object because XRGrabInteractable is [DisallowMultipleComponent]: the
    /// replacement cannot sit alongside the original, and reading values off the original after destroying
    /// it is not possible either. So the values are parked on a temporary object first.
    /// </summary>
    static void ConvertToHanded(XRGrabInteractable grab)
    {
        var go = grab.gameObject;

        var clipboard = new GameObject("~HandPoseClipboard") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var parked = clipboard.AddComponent<HandedGrabInteractable>();
            CopySerializedValues(grab, parked);

            Undo.DestroyObjectImmediate(grab);
            var handed = Undo.AddComponent<HandedGrabInteractable>(go);
            CopySerializedValues(parked, handed);

            EditorUtility.SetDirty(handed);
            Selection.activeObject = go;
            Debug.Log($"[GhostHandPose] '{go.name}' now uses HandedGrabInteractable. Capture each hand and " +
                "Apply to fill both slots.", handed);
        }
        finally
        {
            DestroyImmediate(clipboard);
        }
    }

    /// <summary>
    /// Copies every serialized field except the script reference, which must keep pointing at the type that
    /// actually sits on the object - overwriting it would leave a component claiming to be something it is not.
    /// </summary>
    static void CopySerializedValues(Object source, Object destination)
    {
        var from = new SerializedObject(source);
        var to = new SerializedObject(destination);

        var iterator = from.GetIterator();
        var enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            // False after the first step so each top-level property is copied whole, children included.
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
                continue;

            to.CopyFromSerializedProperty(iterator);
        }

        to.ApplyModifiedProperties();
    }

    void DrawOptions()
    {
        m_ShowOptions = EditorGUILayout.Foldout(m_ShowOptions, "Options", true);
        if (!m_ShowOptions)
            return;

        EditorGUI.indentLevel++;

        m_SaveFolder = EditorGUILayout.TextField("Pose Folder", m_SaveFolder);
        m_PrefabFolder = EditorGUILayout.TextField("Prefab Folder", m_PrefabFolder);

        m_DriverOverride = (XRHandSkeletonDriver)EditorGUILayout.ObjectField("Driver", m_DriverOverride, typeof(XRHandSkeletonDriver), true);
        m_GhostOverride = (GameObject)EditorGUILayout.ObjectField("Ghost Prefab", m_GhostOverride, typeof(GameObject), false);
        EditorGUILayout.LabelField(" ", "Both auto-resolve from the hand above when empty.", EditorStyles.miniLabel);

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// Rebuilds both halves of the grab in one press: the object gets its ghost and its grab point, and the
    /// controller hands get the component and the wrist alignment that make the live hand match.
    ///
    /// Both, because either alone looks broken in a way that does not point at the missing half - without
    /// the object half the hand poses in the wrong place, without the hand half the ghost is right and the
    /// real hand ignores it.
    /// </summary>
    void Apply(ProjectHandPoseData data, Transform target)
    {
        var ghost = SpawnGhost(data, target);
        var attach = EnsureAttachTransform(data, target, ghost);
        var hands = ControllerGrabHandPoseInstaller.Setup();

        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        Selection.activeObject = ghost != null ? (Object)ghost : attach?.gameObject;

        Debug.Log($"[GhostHandPose] Applied '{data.name}' to '{target.name}' and wired {hands} controller " +
            "hand(s). Save the scene to keep it.", target);
    }

    XRHandSkeletonDriver ResolveDriver()
    {
        if (m_DriverOverride != null)
            return m_DriverOverride;

        foreach (var d in FindObjectsByType<XRHandSkeletonDriver>(FindObjectsInactive.Include))
        {
            var events = d.handTrackingEvents;
            if (events != null && events.handedness == m_Handedness)
                return d;
        }

        return null;
    }

    void Capture()
    {
        var driver = ResolveDriver();
        var blocker = Blocker(driver);
        if (blocker != null)
        {
            Debug.LogWarning($"[GhostHandPose] Capture skipped: {blocker}");
            return;
        }

        var data = BuildPoseData(driver, m_Target.transform);
        if (data == null)
            return;

        // Pose asset first: the prefab saved below has to reference something that already exists on disk.
        var asset = SavePoseAsset(data);
        var ghost = SpawnGhost(asset, m_Target.transform);
        if (ghost == null)
            return;

        var prefab = SavePrefab(ghost);
        EnsureAttachTransform(asset, m_Target.transform, ghost);

        // Queued up so the Apply button is one click away the moment Play ends - which is required, because
        // the ghost and attach point just created are scene objects and Unity discards those on exit.
        m_ApplySource = asset;

        Selection.activeObject = ghost;
        EditorGUIUtility.PingObject(prefab != null ? (Object)prefab : asset);
        Debug.Log($"[GhostHandPose] Captured {asset.joints.Count} joints onto '{m_Target.name}'. " +
            "Exit Play and press Apply - scene changes made in Play mode do not survive." +
            (prefab != null ? $"\nPrefab: {AssetDatabase.GetAssetPath(prefab)}" : ""), asset);
    }

    /// <summary>
    /// Reads the driver's current joint rotations, and the wrist's pose expressed in the target's local
    /// space. Local space rather than world, so the pose still holds after the object is moved.
    /// </summary>
    ProjectHandPoseData BuildPoseData(XRHandSkeletonDriver driver, Transform target)
    {
        var references = driver.jointTransformReferences;
        if (references == null || references.Count == 0)
        {
            Debug.LogWarning("[GhostHandPose] The driver has no joint references yet.");
            return null;
        }

        var data = CreateInstance<ProjectHandPoseData>();
        data.handedness = m_Handedness;

        foreach (var reference in references)
        {
            var joint = reference.jointTransform;
            if (joint == null)
                continue;

            data.joints.Add(new ProjectHandPoseData.JointPose
            {
                jointID = reference.xrHandJointID,
                localPosition = joint.localPosition,
                localRotation = joint.localRotation,
            });
        }

        var wrist = driver.rootTransform;
        if (wrist != null)
        {
            data.hasAttachPose = true;
            data.attachLocalPosition = target.InverseTransformPoint(wrist.position);
            data.attachLocalRotation = Quaternion.Inverse(target.rotation) * wrist.rotation;
        }

        return data;
    }

    ProjectHandPoseData SavePoseAsset(ProjectHandPoseData data)
    {
        EnsureFolder(m_SaveFolder);
        var name = string.IsNullOrWhiteSpace(m_PoseName) ? "GrabPose" : m_PoseName;
        var path = AssetDatabase.GenerateUniqueAssetPath($"{m_SaveFolder}/{name}_{m_Handedness}.asset");
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    /// <summary>
    /// Instantiates the ghost prefab under the target, hands it the pose asset and lets
    /// <see cref="GhostHandPose"/> do the posing - the same path a ghost takes when it is re-applied later,
    /// so a captured hand and a re-spawned one cannot drift apart.
    /// </summary>
    GameObject SpawnGhost(ProjectHandPoseData data, Transform target)
    {
        var prefab = m_GhostOverride != null ? m_GhostOverride : LoadGhostPrefab(data.handedness);
        if (prefab == null)
        {
            Debug.LogError("[GhostHandPose] Ghost hand prefab not found. Assign one under Options.");
            return null;
        }

        var ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(ghost, "Capture Grab Pose");
        ghost.name = $"{prefab.name}_{PoseName}";

        ghost.transform.SetParent(target, false);
        ghost.transform.localPosition = Vector3.zero;
        ghost.transform.localRotation = Quaternion.identity;

        var poser = ghost.GetComponent<GhostHandPose>();
        if (poser == null)
            poser = ghost.AddComponent<GhostHandPose>();

        poser.pose = data;
        poser.Apply();

        EditorUtility.SetDirty(ghost);
        return ghost;
    }

    /// <summary>
    /// Writes the posed ghost out as its own prefab and relinks the scene object to it, so the thing left
    /// selected is an instance of the asset rather than a loose copy that would be lost on scene reload.
    ///
    /// Saved after the pose asset exists and is assigned - a prefab can only serialise a reference to an
    /// asset on disk, so doing this in the other order would store a null and silently lose the link.
    /// </summary>
    GameObject SavePrefab(GameObject ghost)
    {
        EnsureFolder(m_PrefabFolder);
        var path = AssetDatabase.GenerateUniqueAssetPath($"{m_PrefabFolder}/{ghost.name}.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ghost, path, InteractionMode.AutomatedAction, out var success);

        if (!success || prefab == null)
        {
            Debug.LogError($"[GhostHandPose] Could not save the prefab: {path}");
            return null;
        }

        AssetDatabase.SaveAssets();
        return prefab;
    }

    /// <summary>
    /// Points the object's XRGrabInteractable at the ghost's own wrist bone, which is what makes the live
    /// hand arrive where the ghost is. Without it the ghost shows the intended grip and the real hand still
    /// holds the object by its origin - usually its centre.
    ///
    /// The bone itself, not a copy of its pose in a helper object. Both sides of the grab then reference "a
    /// wrist": this one on the object, the live hand's on the interactor's Transform To Follow. Nudge the
    /// ghost afterwards and the grab point follows; a copied pose would have to be re-captured.
    /// </summary>
    static Transform EnsureAttachTransform(ProjectHandPoseData data, Transform target, GameObject ghost)
    {
        if (ghost == null)
            return null;

        Transform wrist = null;
        var wristName = data.BonePrefix + XRHandJointID.Wrist;

        foreach (var bone in ghost.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name == wristName)
            {
                wrist = bone;
                break;
            }
        }

        if (wrist == null)
        {
            Debug.LogWarning($"[GhostHandPose] No '{wristName}' under {ghost.name}, so the grab point was " +
                "not set.", ghost);
            return null;
        }

        var grab = target.GetComponentInParent<XRGrabInteractable>();
        if (grab == null)
        {
            Debug.LogWarning($"[GhostHandPose] '{target.name}' has no XRGrabInteractable. Add one and point " +
                $"its Attach Transform at '{wristName}' under the ghost.", target);
            return wrist;
        }

        var serialized = new SerializedObject(grab);

        // A HandedGrabInteractable keeps a slot per hand, so capturing the second hand no longer evicts the
        // first. Written by field name rather than through the typed property so the stock component and the
        // handed one go down the same path.
        var field = grab is HandedGrabInteractable
            ? (data.handedness == Handedness.Right ? "m_RightAttachTransform" : "m_LeftAttachTransform")
            : "m_AttachTransform";

        var property = serialized.FindProperty(field);
        var previous = property.objectReferenceValue as Transform;

        if (!(grab is HandedGrabInteractable) && previous != null && previous != wrist)
            Debug.LogWarning($"[GhostHandPose] Attach Transform on '{grab.name}' moved from '{previous.name}' " +
                $"to the {data.handedness} ghost's wrist. XRGrabInteractable has a single slot, so only one " +
                "hand can be right. Swap the component for HandedGrabInteractable to keep both.", grab);

        property.objectReferenceValue = wrist;
        serialized.ApplyModifiedProperties();

        return wrist;
    }

    static GameObject LoadGhostPrefab(Handedness handedness)
    {
        var guid = handedness == Handedness.Left ? k_GhostLeftGuid : k_GhostRightGuid;
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

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
