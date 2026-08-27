using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class PPETabletInteractionSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    const string TabletName = "Tablet";
    const string MarkerName = "XR Item Marker_small";
    const int ChecklistStepCount = 5;
    const int SignatureStepCount = 3;

    readonly struct TransformSnapshot
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformSnapshot(Transform transform)
        {
            Parent = transform.parent;
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }

        public bool Matches(Transform transform)
        {
            return transform.parent == Parent &&
                transform.localPosition == LocalPosition &&
                transform.localRotation == LocalRotation &&
                transform.localScale == LocalScale;
        }
    }

    [MenuItem("Tools/PPE/Configure Tablet Grab and Checklist")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before configuring the Tablet interaction.");

        Scene scene = RequireActiveScene();
        Transform tablet = RequireUniqueTransform(scene, TabletName);
        TransformSnapshot authoredTransform = new(tablet);

        Undo.SetCurrentGroupName("Configure Tablet Grab and Checklist");
        int undoGroup = Undo.GetCurrentGroup();

        BoxCollider collider = tablet.GetComponent<BoxCollider>();
        if (collider == null)
        {
            MeshFilter meshFilter = tablet.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                throw new InvalidOperationException("Tablet requires its authored MeshFilter to fit the Grab collider.");

            collider = Undo.AddComponent<BoxCollider>(tablet.gameObject);
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            collider.center = meshBounds.center;
            collider.size = meshBounds.size;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);
        }

        Rigidbody rigidbody = tablet.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(tablet.gameObject);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.mass = 0.25f;
            EditorUtility.SetDirty(rigidbody);
        }

        Undo.RecordObject(rigidbody, "Configure Tablet Rigidbody");
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        EditorUtility.SetDirty(rigidbody);

        XRGrabInteractable grab = tablet.GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = Undo.AddComponent<XRGrabInteractable>(tablet.gameObject);

        Transform marker = tablet.Find(MarkerName);
        SphereCollider markerCollider = marker != null ? marker.GetComponent<SphereCollider>() : null;
        if (markerCollider == null)
            throw new InvalidOperationException("Tablet requires its XR Item Marker_small SphereCollider as the only grab point.");

        Undo.RecordObject(grab, "Configure Tablet XR Grab");
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.useDynamicAttach = false;
        grab.matchAttachRotation = false;
        grab.trackRotation = false;
        grab.trackScale = false;
        grab.colliders.Clear();
        grab.colliders.Add(markerCollider);
        grab.throwOnDetach = false;
        grab.retainTransformParent = true;
        EditorUtility.SetDirty(grab);

        PPEMarkerToggleGrab toggleGrab = tablet.GetComponent<PPEMarkerToggleGrab>();
        if (toggleGrab == null)
            toggleGrab = Undo.AddComponent<PPEMarkerToggleGrab>(tablet.gameObject);

        SerializedObject serializedToggle = new(toggleGrab);
        serializedToggle.FindProperty("useToggleGrip").boolValue = false;
        serializedToggle.FindProperty("returnToAuthoredPoseOnRelease").boolValue = true;
        serializedToggle.FindProperty("holdHandGripWhileSelected").boolValue = false;
        serializedToggle.ApplyModifiedPropertiesWithoutUndo();

        HandwrittenSignatureSequence[] sequences =
            tablet.GetComponentsInChildren<HandwrittenSignatureSequence>(true);
        HandwrittenSignatureSequence[] completeSequences = sequences
            .Where(candidate =>
                candidate.HasCompleteTargetReferences &&
                candidate.StepCount == ChecklistStepCount + SignatureStepCount)
            .ToArray();
        if (completeSequences.Length != 1)
        {
            throw new InvalidOperationException(
                $"Tablet requires exactly one complete 8-step signature sequence; found {completeSequences.Length}.");
        }

        HandwrittenSignatureSequence sequence = completeSequences[0];
        foreach (HandwrittenSignatureSequence candidate in sequences)
        {
            Undo.RecordObject(candidate, "Disable automatic Tablet signature playback");
            candidate.SetPlayOnEnableForEditor(false);
            candidate.enabled = candidate == sequence;
            EditorUtility.SetDirty(candidate);
        }

        PPETabletChecklistController checklist =
            tablet.GetComponent<PPETabletChecklistController>();
        if (checklist == null)
            checklist = Undo.AddComponent<PPETabletChecklistController>(tablet.gameObject);

        Undo.RecordObject(checklist, "Configure Tablet checklist controller");
        checklist.ConfigureForEditor(grab, sequence);
        EditorUtility.SetDirty(checklist);

        if (!authoredTransform.Matches(tablet))
        {
            throw new InvalidOperationException(
                "Tablet Transform changed during interaction setup. The scene was not saved.");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        ValidateScene(scene, true);

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        Selection.activeGameObject = tablet.gameObject;
        Debug.Log(
            "[PPE Tablet] Grab return and Trigger checklist/signature flow configured. " +
            "The authored Tablet Transform was preserved.",
            tablet);
    }

    [MenuItem("Tools/PPE/Validate Tablet Grab and Checklist")]
    public static void Validate()
    {
        ValidateScene(RequireActiveScene(), true);
    }

    public static void ValidateScene(Scene scene, bool logSuccess)
    {
        Transform tablet = RequireUniqueTransform(scene, TabletName);
        Transform marker = tablet.Find(MarkerName);
        BoxCollider collider = tablet.GetComponent<BoxCollider>();
        Rigidbody rigidbody = tablet.GetComponent<Rigidbody>();
        XRGrabInteractable grab = tablet.GetComponent<XRGrabInteractable>();
        PPEMarkerToggleGrab toggleGrab = tablet.GetComponent<PPEMarkerToggleGrab>();
        PPETabletChecklistController checklist =
            tablet.GetComponent<PPETabletChecklistController>();

        if (marker == null || !marker.gameObject.activeSelf)
            throw new InvalidOperationException("Tablet must retain its authored active Item Marker child.");
        if (collider == null || collider.isTrigger ||
            collider.size.x <= 0f || collider.size.y <= 0f || collider.size.z <= 0f)
        {
            throw new InvalidOperationException("Tablet requires a non-trigger BoxCollider fitted to its mesh.");
        }
        if (rigidbody == null || rigidbody.useGravity)
            throw new InvalidOperationException("Tablet requires a no-gravity Rigidbody.");
        if (grab == null || grab.throwOnDetach || !grab.retainTransformParent)
            throw new InvalidOperationException("Tablet XR Grab must return without throwing and retain its parent.");
        if (toggleGrab == null || !toggleGrab.UseToggleGrip ||
            !toggleGrab.ReturnToAuthoredPoseOnRelease || !toggleGrab.HoldHandGripWhileSelected)
        {
            throw new InvalidOperationException(
                "Tablet requires Toggle Grab, held-hand grip, and authored-pose return.");
        }
        if (checklist == null || checklist.GrabInteractable != grab ||
            checklist.SignatureSequence == null ||
            !checklist.RequireHoldingHandTrigger ||
            checklist.SignatureSequence.StepCount != ChecklistStepCount + SignatureStepCount ||
            !checklist.SignatureSequence.HasCompleteTargetReferences ||
            checklist.SignatureSequence.PlayOnEnable)
        {
            throw new InvalidOperationException(
                "Tablet Trigger checklist and signature references are incomplete.");
        }

        HandwrittenSignatureSequence[] enabledSequences = tablet
            .GetComponentsInChildren<HandwrittenSignatureSequence>(true)
            .Where(candidate => candidate.enabled)
            .ToArray();
        if (enabledSequences.Length != 1 || enabledSequences[0] != checklist.SignatureSequence)
            throw new InvalidOperationException("Tablet must have exactly one enabled signature sequence.");

        if (logSuccess)
        {
            Debug.Log(
                "Tablet interaction validation passed: Toggle Grab returns to the authored pose; " +
                "one holding-hand Trigger starts all 5 checklist and 3 signature steps.",
                tablet);
        }
    }

    static Scene RequireActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before configuring Tablet interaction. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static Transform RequireUniqueTransform(Scene scene, string objectName)
    {
        Transform[] matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name == objectName)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException($"Expected exactly one '{objectName}', found {matches.Length}.");

        return matches[0];
    }
}
