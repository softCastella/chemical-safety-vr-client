using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Builds the flashlight for the Activate lesson: a grab interactable whose trigger turns on a light.
///
/// The assembly itself is ordinary. What this gets right without being asked is the spot light's
/// rotation - Unity's cylinder runs along Y, so a light left at identity shines out of the side of the
/// handle rather than the end - and switching the light off in the saved scene so the lesson starts
/// from dark.
/// Running this repeatedly is safe; existing objects are reused rather than duplicated.
/// </summary>
static class ActivateLessonBuilder
{
    const string k_RootName = "Flashlight";
    const string k_VisualsName = "Visuals";
    const string k_OffsetName = "Offset";
    const string k_BodyName = "Body";
    const string k_ColliderName = "Collider";
    const string k_LightName = "Spot Light";

    const string k_MaterialGuid = "4d81b60fa9c34e2b8f5701cd6a2e93b7";

    static readonly Vector3 k_Position = new Vector3(-0.068f, 1.2f, 0.457f);

    // Cylinder scale is half-height on Y, so 0.09 draws an 18cm barrel 8cm across.
    static readonly Vector3 k_BodyScale = new Vector3(0.04f, 0.09f, 0.04f);
    static readonly Vector3 k_ColliderSize = new Vector3(0.08f, 0.18f, 0.08f);

    const float k_Range = 10f;
    const float k_SpotAngle = 35f;
    const float k_Intensity = 3f;

    /// <summary>Just past the end of the barrel, so the cone starts outside the mesh.</summary>
    const float k_LightOffset = 0.1f;

    [MenuItem("Tools/XR/Build Activate Lesson", priority = 23)]
    static void Build()
    {
        var existing = GameObject.Find(k_RootName);
        var root = existing != null ? existing : new GameObject(k_RootName);
        root.transform.position = k_Position;

        var grab = GetOrAdd<XRGrabInteractable>(root);

        // Same choice as the earlier lessons: a flashlight that falls and rolls away turns a two second
        // experiment into a fetch quest, and this one is worth repeating many times.
        grab.GetComponent<Rigidbody>().useGravity = false;
        grab.throwOnDetach = false;

        BuildBody(root.transform);
        BuildCollider(root.transform);
        var light = BuildLight(root.transform);

        var beam = GetOrAdd<FlashlightBeam>(root);
        var serialized = new SerializedObject(beam);
        serialized.FindProperty("m_Light").objectReferenceValue = light;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[XR] Flashlight built. Grab it and pull the trigger.");
    }

    static void BuildBody(Transform root)
    {
        var visuals = EnsureChild(root, k_VisualsName);
        var offset = EnsureChild(visuals, k_OffsetName);

        var body = offset.Find(k_BodyName);
        if (body == null)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = k_BodyName;

            // The collider lives on its own object below, so this one would only be a duplicate.
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.transform.SetParent(offset, false);
            body = cylinder.transform;
        }

        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;
        body.localScale = k_BodyScale;

        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(k_MaterialGuid));
        if (material != null)
            body.GetComponent<Renderer>().sharedMaterial = material;
    }

    static void BuildCollider(Transform root)
    {
        var holder = EnsureChild(root, k_ColliderName);
        holder.localPosition = Vector3.zero;

        var box = GetOrAdd<BoxCollider>(holder.gameObject);
        box.size = k_ColliderSize;
        box.center = Vector3.zero;
    }

    static Light BuildLight(Transform root)
    {
        var holder = root.Find(k_LightName);
        if (holder == null)
        {
            holder = new GameObject(k_LightName).transform;
            holder.SetParent(root, false);
        }

        holder.localPosition = new Vector3(0f, k_LightOffset, 0f);

        // A cylinder's length runs along Y while a light shines along Z, so an unrotated spot light
        // points out of the side of the handle. Ninety degrees on X lines the beam up with the barrel.
        holder.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var light = GetOrAdd<Light>(holder.gameObject);
        light.type = LightType.Spot;
        light.range = k_Range;
        light.spotAngle = k_SpotAngle;
        light.intensity = k_Intensity;

        // Saved off, so the lesson starts dark and the trigger is what makes something happen.
        light.enabled = false;

        EditorUtility.SetDirty(light);
        return light;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
            return child;

        var created = new GameObject(name).transform;
        created.SetParent(parent, false);
        return created;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
