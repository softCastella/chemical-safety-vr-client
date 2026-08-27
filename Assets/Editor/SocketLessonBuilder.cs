using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Builds the socket lesson: a fixed point in the world that holds a grab interactable placed into it.
///
/// A socket is only two components, so this exists for the two details that are easy to get wrong and
/// give no feedback when they are. The collider must be a trigger, because that is how the socket
/// detects anything at all, and it has to be roomier than the object or lining it up becomes a chore.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class SocketLessonBuilder
{
    const string k_SocketName = "Socket";
    const string k_AttachName = "Attach";
    const string k_MarkerName = "Marker";
    const string k_SampleName = "Socket Sample";

    /// <summary>Translucent box that shows where the otherwise invisible trigger sits.</summary>
    const string k_MarkerMaterialGuid = "3b7e05c1d24f48a0ae91836b5c7f02d9";

    /// <summary>
    /// How much larger than its contents the trigger is. A socket exists to say "near enough is good
    /// enough", so a snug fit defeats the point - the user ends up threading a needle.
    /// </summary>
    const float k_TriggerMargin = 1.6f;

    /// <summary>Used when the scene has nothing grabbable to measure.</summary>
    const float k_FallbackSize = 0.16f;

    // Side by side at the same depth, about 28cm apart. Both stay within one comfortable reach, so the
    // lesson is a short sideways move rather than a lean forward - worth repeating many times over.
    static readonly Vector3 k_SocketPosition = new Vector3(0.216f, 1.2f, 0.457f);
    static readonly Vector3 k_SamplePosition = new Vector3(-0.068f, 1.2f, 0.457f);

    [MenuItem("Tools/XR/Build Socket Lesson", priority = 22)]
    static void Build()
    {
        var target = FindGrabbable();
        if (target == null)
            target = BuildSample();

        // Only the cube this builder owns gets moved. Anything else grabbable was placed deliberately by
        // whoever built the scene, and the socket adapts to it instead.
        if (target.name == k_SampleName)
            target.transform.position = k_SamplePosition;

        var socket = BuildSocket(MeasureSize(target));

        EditorSceneManager.MarkSceneDirty(socket.gameObject.scene);
        Selection.activeGameObject = socket.gameObject;
        Debug.Log("[XR] Socket built. Grab the cube and release it inside the socket.");
    }

    static XRSocketInteractor BuildSocket(Vector3 contentSize)
    {
        var existing = GameObject.Find(k_SocketName);
        var socket = existing != null ? existing : new GameObject(k_SocketName);

        // Re-applied every run rather than only on creation. These positions are the lesson's layout, so
        // running the menu again is how you pick up a change to them - unlike the hand offsets, which
        // can only be found in a headset and must never be overwritten.
        socket.transform.position = k_SocketPosition;

        // OnTriggerEnter/OnTriggerStay is the socket's only way of noticing anything, so a solid collider
        // does not merely fail - it turns the socket into a wall the object bounces off, which looks like
        // the socket actively rejecting it. There is no RequireComponent and no warning either way.
        var trigger = GetOrAdd<BoxCollider>(socket);
        trigger.isTrigger = true;
        trigger.size = contentSize * k_TriggerMargin;

        var interactor = GetOrAdd<XRSocketInteractor>(socket);

        // Splitting the pose off the trigger lets the detection volume stay generous while the object
        // still lands in an exact place, the same split the grab interactors use.
        var attach = socket.transform.Find(k_AttachName);
        if (attach == null)
        {
            attach = new GameObject(k_AttachName).transform;
            attach.SetParent(socket.transform, false);
        }

        interactor.attachTransform = attach;
        BuildMarker(socket.transform, trigger.size);

        EditorUtility.SetDirty(interactor);
        return interactor;
    }

    /// <summary>
    /// A trigger draws nothing at runtime, so without this the socket is an invisible spot the user is
    /// expected to find by waving the object around. The box is translucent to keep the blue preview
    /// mesh readable through it, and its collider is removed so it never blocks what it is advertising.
    /// </summary>
    static void BuildMarker(Transform socket, Vector3 size)
    {
        var marker = socket.Find(k_MarkerName);
        if (marker == null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = k_MarkerName;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(socket, false);
            marker = cube.transform;
        }

        marker.localPosition = Vector3.zero;
        marker.localRotation = Quaternion.identity;
        marker.localScale = size;

        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(k_MarkerMaterialGuid));
        if (material == null)
        {
            Debug.LogWarning("[XR] Assets/Materials/Socket.mat is missing; the marker will be opaque and hide the preview.");
            return;
        }

        marker.GetComponent<Renderer>().sharedMaterial = material;
    }

    /// <summary>A socket with nothing to hold cannot be tested, so provide something to hold.</summary>
    static XRGrabInteractable BuildSample()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = k_SampleName;
        cube.transform.position = k_SamplePosition;
        cube.transform.localScale = Vector3.one * 0.1f;

        // Adding the interactable brings the Rigidbody with it via RequireComponent.
        var grab = cube.AddComponent<XRGrabInteractable>();

        // Same choice the earlier lessons make: with gravity and throwing off, a miss leaves the cube
        // hanging in reach instead of on the floor behind you. Socketing is worth repeating many times
        // over, and fetching a dropped cube between attempts is what stops people from repeating it.
        grab.GetComponent<Rigidbody>().useGravity = false;
        grab.throwOnDetach = false;
        return grab;
    }

    static XRGrabInteractable FindGrabbable() => Object.FindAnyObjectByType<XRGrabInteractable>();

    /// <summary>
    /// World-space size of what the socket will hold, measured from renderers so a nested visual mesh is
    /// accounted for rather than just the root transform.
    /// </summary>
    static Vector3 MeasureSize(XRGrabInteractable target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return Vector3.one * k_FallbackSize;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // A flat object would otherwise get a trigger with no depth on one axis.
        return Vector3.Max(bounds.size, Vector3.one * k_FallbackSize * 0.5f);
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
