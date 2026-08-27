using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ChemicalMixerInterior : MonoBehaviour
{
    const string GeneratedRootName = "Generated_MixerInterior";

    [Header("Vessel Dimensions")]
    [Min(0.5f)] public float radius = 3f;
    [Min(0.5f)] public float wallHeight = 4f;
    [Min(0.1f)] public float domeHeight = 1.45f;
    [Min(0.05f)] public float bottomDishDepth = 0.55f;
    [Range(16, 128)] public int radialSegments = 64;
    [Range(3, 24)] public int domeSegments = 12;

    [Header("Mixer Assembly")]
    [Min(0.03f)] public float shaftRadius = 0.12f;
    [Min(0f)] public float shaftBottomClearance = 0.9f;
    [Min(0f)] public float shaftTopInset = 0.08f;
    [Range(1, 6)] public int bladeLevels = 3;
    [Min(0.1f)] public float bladeLength = 2.15f;
    [Min(0.03f)] public float bladeThickness = 0.11f;
    public float bladeTwistDegrees = 32f;
    public float lowestBladeHeight = -1.15f;
    public float highestBladeHeight = 0.2f;

    [Header("Floor Drainage")]
    public bool createFloorDrainage = true;
    public Vector2 drainPosition = new Vector2(0.56f, 0.3f);
    [Min(0.05f)] public float drainRadius = 0.2f;
    [Min(0.01f)] public float drainRimThickness = 0.045f;
    public Vector2 valvePosition = new Vector2(0f, 0.3f);
    [Min(0.1f)] public float valveBodyRadius = 0.3f;
    [Min(0.05f)] public float valveBodyHeight = 0.16f;
    [Min(0.1f)] public float valveWheelRadius = 0.25f;
    [Min(0.01f)] public float valveWheelThickness = 0.035f;

    [Header("Appearance")]
    public Color steelColor = new Color(0.42f, 0.48f, 0.5f, 1f);
    public Color darkSteelColor = new Color(0.08f, 0.11f, 0.12f, 1f);
    [Range(0f, 1f)] public float smoothness = 0.62f;
    [Range(0f, 0.3f)] public float brushStrength = 0.08f;

    [Header("Rear Manhole")]
    public bool createManhole = true;
    [Min(0.3f)] public float manholeDiameter = 1.1f;
    public float manholeHeight = 0.15f;
    [Range(45f, 150f)] public float manholeOpenAngle = 105f;
    [Min(0.1f)] public float manholeMoveDuration = 1.2f;

    Material generatedMaterial;
    Material darkMaterial;

    void OnEnable()
    {
        CreateMaterial();
        ApplyMaterialsToExistingGeometry();
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.5f, radius);
        wallHeight = Mathf.Max(0.5f, wallHeight);
        domeHeight = Mathf.Max(0.1f, domeHeight);
        bladeLength = Mathf.Clamp(bladeLength, 0.1f, radius * 0.9f);
    }

    void ApplyMaterialsToExistingGeometry()
    {
        var generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot == null || generatedMaterial == null)
            return;

        foreach (var renderer in generatedRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool usesDarkMaterial = renderer.name == "Drain_Opening" || renderer.name == "Opening_Dark";
            renderer.sharedMaterial = usesDarkMaterial && darkMaterial != null
                ? darkMaterial
                : generatedMaterial;
        }
    }

    [ContextMenu("Rebuild Mixer Interior")]
    public void Rebuild()
    {
        RemoveGeneratedRoot();
        CreateMaterial();

        var generatedRoot = new GameObject(GeneratedRootName);
        generatedRoot.transform.SetParent(transform, false);

        CreateVessel(generatedRoot.transform);
        CreateShaft(generatedRoot.transform);
        CreateBlades(generatedRoot.transform);
        if (createFloorDrainage)
            CreateFloorDrainage(generatedRoot.transform);
        CreateStructuralRings(generatedRoot.transform);
        if (createManhole)
            CreateManhole(generatedRoot.transform);
    }

    void CreateMaterial()
    {
        if (generatedMaterial != null)
            SafeDestroy(generatedMaterial);

        var shader = Shader.Find("Project/Chemical Mixer Interior");
        if (shader == null)
        {
            Debug.LogError("Chemical Mixer Interior shader was not found.", this);
            return;
        }

        generatedMaterial = new Material(shader)
        {
            name = "ChemicalMixerInterior_RuntimeMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };
        generatedMaterial.SetColor("_BaseColor", steelColor);
        generatedMaterial.SetColor("_DarkColor", darkSteelColor);
        generatedMaterial.SetFloat("_Smoothness", smoothness);
        generatedMaterial.SetFloat("_BrushStrength", brushStrength);

        if (darkMaterial != null)
            SafeDestroy(darkMaterial);
        darkMaterial = new Material(generatedMaterial)
        {
            name = "ChemicalMixerManhole_DarkMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };
        darkMaterial.SetColor("_BaseColor", new Color(0.015f, 0.02f, 0.022f, 1f));
        darkMaterial.SetColor("_DarkColor", Color.black);
        darkMaterial.SetFloat("_Smoothness", 0.08f);
    }

    void CreateVessel(Transform parent)
    {
        var vessel = new GameObject("Vessel_InsideShell");
        vessel.transform.SetParent(parent, false);
        var filter = vessel.AddComponent<MeshFilter>();
        var renderer = vessel.AddComponent<MeshRenderer>();
        filter.sharedMesh = BuildVesselMesh();
        filter.sharedMesh.name = "ChemicalMixer_InsideShell";
        renderer.sharedMaterial = generatedMaterial;
    }

    Mesh BuildVesselMesh()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        var bottomY = -wallHeight * 0.5f;
        var topY = wallHeight * 0.5f;

        AddRing(vertices, normals, uvs, radius, bottomY, 0f, -1f, 0f);
        AddRing(vertices, normals, uvs, radius, topY, 0.58f, -1f, 0f);

        for (var ring = 1; ring <= domeSegments; ring++)
        {
            var t = ring / (float)domeSegments;
            var angle = t * Mathf.PI * 0.5f;
            var ringRadius = radius * Mathf.Cos(angle);
            var y = topY + domeHeight * Mathf.Sin(angle);
            AddRing(vertices, normals, uvs, Mathf.Max(0.001f, ringRadius), y,
                Mathf.Lerp(0.58f, 1f, t),
                -Mathf.Cos(angle) / radius,
                -Mathf.Sin(angle) / domeHeight);
        }

        ConnectRings(triangles, domeSegments + 1);

        var bottomStart = vertices.Count;
        AddRing(vertices, normals, uvs, radius, bottomY, 0f, -1f, 0f);
        for (var ring = 1; ring <= domeSegments; ring++)
        {
            var t = ring / (float)domeSegments;
            var angle = t * Mathf.PI * 0.5f;
            AddRing(vertices, normals, uvs, Mathf.Max(0.001f, radius * Mathf.Cos(angle)),
                bottomY - bottomDishDepth * Mathf.Sin(angle), Mathf.Lerp(0f, 0.16f, t),
                -Mathf.Cos(angle) / radius,
                Mathf.Sin(angle) / bottomDishDepth);
        }
        // The bottom rings progress downward, opposite to the wall and top dome.
        // Reverse their winding so the dish faces the vessel interior.
        ConnectRings(triangles, domeSegments, bottomStart / (radialSegments + 1), true);

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddRing(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
        float ringRadius, float y, float v, float radialNormal, float verticalNormal)
    {
        for (var segment = 0; segment <= radialSegments; segment++)
        {
            var u = segment / (float)radialSegments;
            var angle = u * Mathf.PI * 2f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices.Add(radial * ringRadius + Vector3.up * y);
            normals.Add((radial * radialNormal + Vector3.up * verticalNormal).normalized);
            uvs.Add(new Vector2(u, v));
        }
    }

    void ConnectRings(List<int> triangles, int connectionCount, int startRing = 0, bool reverseWinding = false)
    {
        var stride = radialSegments + 1;
        for (var ring = 0; ring < connectionCount; ring++)
        {
            var current = (startRing + ring) * stride;
            var next = current + stride;
            for (var segment = 0; segment < radialSegments; segment++)
            {
                var a = current + segment;
                var b = current + segment + 1;
                var c = next + segment;
                var d = next + segment + 1;
                // Wind the shell toward the vessel interior. With back-face
                // culling this makes the enclosure invisible from outside.
                if (reverseWinding)
                {
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
                else
                {
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            }
        }
    }

    void CreateShaft(Transform parent)
    {
        var bottomY = -wallHeight * 0.5f - bottomDishDepth + shaftBottomClearance;
        var topY = wallHeight * 0.5f + domeHeight - shaftTopInset;
        var height = Mathf.Max(0.1f, topY - bottomY);
        var shaft = CreatePrimitive(PrimitiveType.Cylinder, "Central_Shaft", parent);
        shaft.transform.localPosition = new Vector3(0f, (topY + bottomY) * 0.5f, 0f);
        shaft.transform.localScale = new Vector3(shaftRadius * 2f, height * 0.5f, shaftRadius * 2f);
    }

    void CreateBlades(Transform parent)
    {
        for (var level = 0; level < bladeLevels; level++)
        {
            var t = bladeLevels == 1 ? 0.5f : level / (float)(bladeLevels - 1);
            var y = Mathf.Lerp(lowestBladeHeight, highestBladeHeight, t);
            var assembly = new GameObject($"Blade_Level_{level + 1}");
            assembly.transform.SetParent(parent, false);
            assembly.transform.localPosition = Vector3.up * y;
            assembly.transform.localRotation = Quaternion.Euler(0f, level * 67f, 0f);

            for (var side = 0; side < 2; side++)
            {
                var direction = side == 0 ? 1f : -1f;
                var blade = CreatePrimitive(PrimitiveType.Cube, $"Blade_{side + 1}", assembly.transform);
                blade.transform.localPosition = Vector3.right * direction * bladeLength * 0.5f;
                blade.transform.localScale = new Vector3(bladeLength, bladeThickness, 0.38f);
                blade.transform.localRotation = Quaternion.Euler(direction * bladeTwistDegrees, 0f, direction * 8f);
            }
        }
    }

    void CreateFloorDrainage(Transform parent)
    {
        var floorY = -wallHeight * 0.5f - bottomDishDepth;

        var drain = new GameObject("Floor_Drain");
        drain.transform.SetParent(parent, false);
        drain.transform.localPosition = new Vector3(drainPosition.x, floorY + 0.012f, drainPosition.y);

        var opening = CreatePrimitive(PrimitiveType.Cylinder, "Drain_Opening", drain.transform);
        opening.transform.localScale = new Vector3(drainRadius * 2f, 0.012f, drainRadius * 2f);
        opening.GetComponent<MeshRenderer>().sharedMaterial = darkMaterial;

        var rim = new GameObject("Drain_Rim");
        rim.transform.SetParent(drain.transform, false);
        rim.transform.localPosition = Vector3.up * 0.018f;
        var rimFilter = rim.AddComponent<MeshFilter>();
        rimFilter.sharedMesh = BuildTorusMesh(drainRadius + drainRimThickness * 0.45f,
            drainRimThickness, 40, 8);
        rimFilter.sharedMesh.name = "Mixer_FloorDrainRim";
        rim.AddComponent<MeshRenderer>().sharedMaterial = generatedMaterial;

        var valve = new GameObject("Floor_DrainValve");
        valve.transform.SetParent(parent, false);
        valve.transform.localPosition = new Vector3(valvePosition.x, floorY, valvePosition.y);

        var body = CreatePrimitive(PrimitiveType.Cylinder, "Valve_Base", valve.transform);
        body.transform.localPosition = Vector3.up * (valveBodyHeight * 0.5f + 0.015f);
        body.transform.localScale = new Vector3(valveBodyRadius * 2f, valveBodyHeight * 0.5f,
            valveBodyRadius * 2f);

        var stemHeight = valveBodyHeight + 0.16f;
        var stem = CreatePrimitive(PrimitiveType.Cylinder, "Valve_Stem", valve.transform);
        stem.transform.localPosition = Vector3.up * (valveBodyHeight + stemHeight * 0.5f);
        stem.transform.localScale = new Vector3(0.045f, stemHeight * 0.5f, 0.045f);

        var wheel = new GameObject("Valve_Handwheel");
        wheel.transform.SetParent(valve.transform, false);
        wheel.transform.localPosition = Vector3.up * (valveBodyHeight + stemHeight);
        var wheelFilter = wheel.AddComponent<MeshFilter>();
        wheelFilter.sharedMesh = BuildTorusMesh(valveWheelRadius, valveWheelThickness, 32, 8);
        wheelFilter.sharedMesh.name = "Mixer_DrainValveHandwheel";
        wheel.AddComponent<MeshRenderer>().sharedMaterial = generatedMaterial;

        for (var spokeIndex = 0; spokeIndex < 4; spokeIndex++)
        {
            var spoke = CreatePrimitive(PrimitiveType.Cube, $"Handwheel_Spoke_{spokeIndex + 1}", wheel.transform);
            spoke.transform.localRotation = Quaternion.Euler(0f, spokeIndex * 45f, 0f);
            spoke.transform.localScale = new Vector3(valveWheelRadius * 1.7f,
                valveWheelThickness * 0.8f, valveWheelThickness * 0.8f);
        }
    }

    void CreateStructuralRings(Transform parent)
    {
        var ringYs = new[] { -wallHeight * 0.5f + 0.08f, 0f, wallHeight * 0.5f - 0.08f };
        foreach (var y in ringYs)
        {
            var ring = new GameObject("Reinforcement_Ring");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = Vector3.up * y;
            var filter = ring.AddComponent<MeshFilter>();
            var renderer = ring.AddComponent<MeshRenderer>();
            filter.sharedMesh = BuildTorusMesh(radius * 0.995f, 0.035f, radialSegments, 8);
            filter.sharedMesh.name = "ChemicalMixer_ReinforcementRing";
            renderer.sharedMaterial = generatedMaterial;
        }
    }

    void CreateManhole(Transform parent)
    {
        var manhole = new GameObject("Rear_Manhole");
        manhole.transform.SetParent(parent, false);

        var doorRadius = manholeDiameter * 0.5f;
        // The viewer starts near the vessel's -Z wall. Place the manhole on
        // that rear wall and offset its parts toward the vessel interior (+Z).
        var wallZ = -radius + 0.045f;

        var opening = CreatePrimitive(PrimitiveType.Cylinder, "Opening_Dark", manhole.transform);
        opening.transform.localPosition = new Vector3(0f, manholeHeight, wallZ + 0.012f);
        opening.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        opening.transform.localScale = new Vector3(doorRadius * 0.94f, 0.012f, doorRadius * 0.94f);
        opening.GetComponent<MeshRenderer>().sharedMaterial = darkMaterial;

        var frame = new GameObject("Manhole_Frame");
        frame.transform.SetParent(manhole.transform, false);
        frame.transform.localPosition = new Vector3(0f, manholeHeight, wallZ + 0.018f);
        frame.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var frameFilter = frame.AddComponent<MeshFilter>();
        frameFilter.sharedMesh = BuildTorusMesh(doorRadius, 0.055f, 48, 10);
        frameFilter.sharedMesh.name = "Mixer_ManholeFrame";
        frame.AddComponent<MeshRenderer>().sharedMaterial = generatedMaterial;

        var hinge = new GameObject("Hinge_Pivot");
        hinge.transform.SetParent(manhole.transform, false);
        hinge.transform.localPosition = new Vector3(-doorRadius, manholeHeight, wallZ + 0.055f);

        var controller = hinge.AddComponent<MixerManholeController>();
        controller.openAngle = manholeOpenAngle;
        controller.moveDuration = manholeMoveDuration;

        var plate = CreatePrimitive(PrimitiveType.Cylinder, "Hatch_Door", hinge.transform, true);
        plate.transform.localPosition = new Vector3(doorRadius, 0f, 0f);
        plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        plate.transform.localScale = new Vector3(doorRadius * 0.96f, 0.04f, doorRadius * 0.96f);
        plate.AddComponent<ManholeToggleTarget>().controller = controller;

        var hingeBar = CreatePrimitive(PrimitiveType.Cylinder, "Hinge_Bar", hinge.transform);
        hingeBar.transform.localPosition = Vector3.zero;
        hingeBar.transform.localScale = new Vector3(0.055f, doorRadius * 0.72f, 0.055f);

        CreateVerticalHandle(hinge.transform, controller, doorRadius);
    }

    void CreateVerticalHandle(Transform hinge, MixerManholeController controller, float doorRadius)
    {
        var handleRoot = new GameObject("Vertical_Handle");
        handleRoot.transform.SetParent(hinge, false);
        handleRoot.transform.localPosition = new Vector3(doorRadius * 1.62f, 0f, 0.13f);

        var grip = CreatePrimitive(PrimitiveType.Cylinder, "Grip", handleRoot.transform, true);
        grip.transform.localScale = new Vector3(0.032f, 0.19f, 0.032f);
        grip.AddComponent<ManholeToggleTarget>().controller = controller;

        for (var side = -1; side <= 1; side += 2)
        {
            var mount = CreatePrimitive(PrimitiveType.Cube,
                side < 0 ? "Bottom_Mount" : "Top_Mount", handleRoot.transform);
            mount.transform.localPosition = new Vector3(0f, side * 0.19f, -0.065f);
            mount.transform.localScale = new Vector3(0.065f, 0.045f, 0.16f);
        }
    }

    static Mesh BuildTorusMesh(float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
    {
        var vertices = new Vector3[(majorSegments + 1) * (tubeSegments + 1)];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[majorSegments * tubeSegments * 6];

        for (var major = 0; major <= majorSegments; major++)
        {
            var u = major / (float)majorSegments;
            var majorAngle = u * Mathf.PI * 2f;
            var radial = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));

            for (var tube = 0; tube <= tubeSegments; tube++)
            {
                var v = tube / (float)tubeSegments;
                var tubeAngle = v * Mathf.PI * 2f;
                var normal = radial * Mathf.Cos(tubeAngle) + Vector3.up * Mathf.Sin(tubeAngle);
                var index = major * (tubeSegments + 1) + tube;
                vertices[index] = radial * majorRadius + normal * tubeRadius;
                normals[index] = normal;
                uvs[index] = new Vector2(u, v);
            }
        }

        var triangleIndex = 0;
        var stride = tubeSegments + 1;
        for (var major = 0; major < majorSegments; major++)
        {
            for (var tube = 0; tube < tubeSegments; tube++)
            {
                var a = major * stride + tube;
                var b = a + 1;
                var c = a + stride;
                var d = c + 1;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = d;
                triangles[triangleIndex++] = c;
            }
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent, bool keepCollider = false)
    {
        var primitive = GameObject.CreatePrimitive(type);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        var collider = primitive.GetComponent<Collider>();
        if (collider != null && !keepCollider)
            SafeDestroy(collider);
        primitive.GetComponent<MeshRenderer>().sharedMaterial = generatedMaterial;
        return primitive;
    }

    void RemoveGeneratedRoot()
    {
        var existing = transform.Find(GeneratedRootName);
        if (existing != null)
            SafeDestroy(existing.gameObject);
    }

    static void SafeDestroy(Object target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
