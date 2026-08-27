using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates BackgroundGradient.shadergraph via reflection (Shader Graph types are internal).
/// Menu: Tools / Generate Background Gradient Shader
/// Auto-runs on domain reload when the .shadergraph file is missing.
/// </summary>
public static class BackgroundGradientShaderGenerator
{
    const string ShaderPath = "Assets/Shaders/BackgroundGradient.shadergraph";
    const string MaterialPath = "Assets/Shaders/BackgroundGradient.mat";
    const string GeneratedPrefKey = "BackgroundGradientShaderGenerator_Generated_v1";

    static Assembly ShaderGraphAsm => GetAssembly("Unity.ShaderGraph.Editor");
    static Assembly UniversalEditorAsm => GetAssembly("Unity.RenderPipelines.Universal.Editor");

    static Assembly GetAssembly(string name)
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (asm == null)
            throw new InvalidOperationException($"Assembly not loaded: {name}");
        return asm;
    }

    static Type RequireType(Assembly asm, string fullName)
    {
        var type = asm.GetType(fullName);
        if (type == null)
            throw new TypeLoadException($"Type not found: {fullName} in {asm.GetName().Name}");
        return type;
    }

    [InitializeOnLoadMethod]
    static void AutoGenerateOnLoad() => ScheduleAutoGenerate();

    [UnityEditor.Callbacks.DidReloadScripts]
    static void AutoGenerateAfterCompile() => ScheduleAutoGenerate();

    static void ScheduleAutoGenerate()
    {
        EditorApplication.delayCall -= RunAutoGenerate;
        EditorApplication.delayCall += RunAutoGenerate;
    }

    static void RunAutoGenerate()
    {
        if (EditorPrefs.GetBool(GeneratedPrefKey, false) && IsGeneratedGradientGraph() && ShaderGraphImportsSuccessfully())
            return;

        if (System.IO.File.Exists(ShaderPath))
        {
            Debug.LogWarning("Replacing BackgroundGradient.shadergraph with generated gradient shader...");
            AssetDatabase.DeleteAsset(ShaderPath);
        }

        Debug.Log("Generating BackgroundGradient.shadergraph via Shader Graph API...");
        Generate();

        if (ShaderGraphImportsSuccessfully())
            EditorPrefs.SetBool(GeneratedPrefKey, true);
    }

    static bool IsGeneratedGradientGraph()
    {
        if (!System.IO.File.Exists(ShaderPath))
            return false;

        var text = System.IO.File.ReadAllText(ShaderPath);
        return text.Contains("_Top_Color") || text.Contains("\"m_Name\": \"Top Color\"");
    }

    static bool ShaderGraphImportsSuccessfully()
    {
        if (!System.IO.File.Exists(ShaderPath))
            return false;

        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) != null;
    }

    [MenuItem("Tools/Generate Background Gradient Shader")]
    public static void GenerateFromMenu() => Generate();

    public static void Generate()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(ShaderPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir)?.Replace('\\', '/');
                var folderName = System.IO.Path.GetFileName(dir);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
                    AssetDatabase.CreateFolder(parent, folderName);
            }

            var graph = BuildGraph();
            WriteGraph(ShaderPath, graph);
            AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError("Shader Graph import failed after generation. See Console for details.");
                return;
            }

            EnsureMaterial(shader);
            EditorPrefs.SetBool(GeneratedPrefKey, true);
            Debug.Log($"Created {ShaderPath} and {MaterialPath}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    static object CreateInstance(Type type) =>
        Activator.CreateInstance(type, nonPublic: true);

    static object BuildGraph()
    {
        var graphType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.GraphData");
        var graph = CreateInstance(graphType);
        Invoke(graph, "AddContexts");

        var universalTargetType = RequireType(UniversalEditorAsm, "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        var unlitSubTargetType = RequireType(UniversalEditorAsm, "UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget");
        var target = CreateInstance(universalTargetType);

        if (!(bool)Invoke(target, "TrySetActiveSubTarget", new object[] { unlitSubTargetType }))
            throw new InvalidOperationException("Failed to set Universal Unlit sub-target.");

        SetEnum(target, "surfaceType", UniversalEditorAsm, "UnityEditor.Rendering.Universal.ShaderGraph.SurfaceType", "Opaque");
        SetField(target, "m_CastShadows", false);
        SetField(target, "m_ReceiveShadows", false);

        var blockFieldsType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.BlockFields");
        var vertexDesc = blockFieldsType.GetNestedType("VertexDescription", BindingFlags.Public | BindingFlags.NonPublic);
        var surfaceDesc = blockFieldsType.GetNestedType("SurfaceDescription", BindingFlags.Public | BindingFlags.NonPublic);

        var targetArrayType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Target");
        var blockFieldType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.BlockFieldDescriptor");
        var targets = CreateTypedArray(targetArrayType, target);
        var blockDescriptors = CreateTypedArray(
            blockFieldType,
            GetStaticField(vertexDesc, "Position"),
            GetStaticField(vertexDesc, "Normal"),
            GetStaticField(vertexDesc, "Tangent"),
            GetStaticField(surfaceDesc, "BaseColor"));

        Invoke(graph, "InitializeOutputs", new object[] { targets, blockDescriptors });
        SetField(graph, "path", "Shader Graphs/Background");

        var topColor = AddColorProperty(graph, "Top Color", new Color(0.53f, 0.81f, 0.98f, 1f));
        var bottomColor = AddColorProperty(graph, "Bottom Color", new Color(0.12f, 0.28f, 0.55f, 1f));
        var flipGradient = AddBooleanProperty(graph, "Flip Gradient", false);
        var gradientPower = AddSliderProperty(graph, "Gradient Power", 1f, 0.1f, 5f);

        var categoryType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.CategoryData");
        var shaderInputType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Internal.ShaderInput");
        var propertyList = CreateGenericList(shaderInputType, topColor, bottomColor, flipGradient, gradientPower);
        var category = InvokeStatic(categoryType, "DefaultCategory", new object[] { propertyList });
        Invoke(graph, "AddCategory", new[] { category });

        var positionNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.PositionNode");
        SetGeometrySpace(positionNode, 0); // Object

        var splitNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode");
        var offsetNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.Vector1Node");
        SetSlotFloat(offsetNode, "UnityEditor.ShaderGraph.Vector1Node", "InputSlotXId", 0.5f);

        var addNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.AddNode");
        var saturateNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.SaturateNode");
        var powerNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.PowerNode");
        var oneMinusNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.OneMinusNode");
        var flipLerpNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.LerpNode");
        var colorLerpNode = CreateNode(ShaderGraphAsm, "UnityEditor.ShaderGraph.LerpNode");

        var topColorNode = AddPropertyNode(graph, topColor);
        var bottomColorNode = AddPropertyNode(graph, bottomColor);
        var flipNode = AddPropertyNode(graph, flipGradient);
        var powerPropertyNode = AddPropertyNode(graph, gradientPower);

        AddNode(graph, positionNode);
        AddNode(graph, splitNode);
        AddNode(graph, offsetNode);
        AddNode(graph, addNode);
        AddNode(graph, saturateNode);
        AddNode(graph, powerNode);
        AddNode(graph, oneMinusNode);
        AddNode(graph, flipLerpNode);
        AddNode(graph, colorLerpNode);

        Connect(graph, positionNode, 0, splitNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", "InputSlotId"));
        Connect(graph, splitNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.SplitNode", "OutputSlotGId"), addNode, 0);
        Connect(graph, offsetNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.Vector1Node", "OutputSlotId"), addNode, 1);
        Connect(graph, addNode, 2, saturateNode, 0);
        Connect(graph, saturateNode, 1, powerNode, 0);
        Connect(graph, powerPropertyNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.PropertyNode", "OutputSlotId"), powerNode, 1);
        Connect(graph, powerNode, 2, oneMinusNode, 0);
        Connect(graph, powerNode, 2, flipLerpNode, 0);
        Connect(graph, oneMinusNode, 1, flipLerpNode, 1);
        Connect(graph, flipNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.PropertyNode", "OutputSlotId"), flipLerpNode, 2);
        Connect(graph, bottomColorNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.PropertyNode", "OutputSlotId"), colorLerpNode, 0);
        Connect(graph, topColorNode, GetConst(ShaderGraphAsm, "UnityEditor.ShaderGraph.PropertyNode", "OutputSlotId"), colorLerpNode, 1);
        Connect(graph, flipLerpNode, 3, colorLerpNode, 2);

        var baseColorBlock = FindBlockNode(graph, "SurfaceDescription.BaseColor");
        Connect(graph, colorLerpNode, 3, baseColorBlock, 0);

        Invoke(graph, "ValidateGraph");
        return graph;
    }

    static void WriteGraph(string path, object graph)
    {
        var fileUtils = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.FileUtilities");
        fileUtils.GetMethod("WriteShaderGraphToDisk", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { path, graph });
    }

    static void EnsureMaterial(Shader shader)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "BackgroundGradient" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.SetColor("_Top_Color", new Color(0.53f, 0.81f, 0.98f, 1f));
        mat.SetColor("_Bottom_Color", new Color(0.12f, 0.28f, 0.55f, 1f));
        mat.SetFloat("_Flip_Gradient", 0f);
        mat.SetFloat("_Gradient_Power", 1f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        ApplyMaterialToSceneBackgrounds(mat);
    }

    static void ApplyMaterialToSceneBackgrounds(Material mat)
    {
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Include);

        var updated = 0;
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.name != "BG_Gradient")
                continue;

            renderer.sharedMaterial = mat;
            if (!renderer.gameObject.activeSelf)
                renderer.gameObject.SetActive(true);

            EditorUtility.SetDirty(renderer);
            updated++;
        }

        if (updated > 0)
            Debug.Log($"Applied {MaterialPath} to {updated} BG_Gradient object(s) in open scene(s).");
    }

    static object AddColorProperty(object graph, string displayName, Color value)
    {
        var propType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Internal.ColorShaderProperty");
        var prop = CreateInstance(propType);
        SetField(prop, "displayName", displayName);
        SetField(prop, "value", value);
        Invoke(graph, "AddGraphInput", new object[] { prop, -1 });
        return prop;
    }

    static object AddBooleanProperty(object graph, string displayName, bool value)
    {
        var propType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Internal.BooleanShaderProperty");
        var prop = CreateInstance(propType);
        SetField(prop, "displayName", displayName);
        SetField(prop, "value", value);
        Invoke(graph, "AddGraphInput", new object[] { prop, -1 });
        return prop;
    }

    static object AddSliderProperty(object graph, string displayName, float value, float min, float max)
    {
        var propType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty");
        var floatType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Internal.FloatType");
        var prop = CreateInstance(propType);
        SetField(prop, "displayName", displayName);
        SetField(prop, "value", value);
        SetField(prop, "floatType", Enum.Parse(floatType, "Slider"));
        SetField(prop, "rangeValues", new Vector2(min, max));
        Invoke(graph, "AddGraphInput", new object[] { prop, -1 });
        return prop;
    }

    static object AddPropertyNode(object graph, object property)
    {
        var nodeType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.PropertyNode");
        var node = CreateInstance(nodeType);
        AddNode(graph, node);
        nodeType.GetProperty("property", BindingFlags.Public | BindingFlags.Instance)?.SetValue(node, property);
        return node;
    }

    static object CreateNode(Assembly asm, string typeName)
    {
        return CreateInstance(RequireType(asm, typeName));
    }

    static void AddNode(object graph, object node) => Invoke(graph, "AddNode", new object[] { node, true });

    static void Connect(object graph, object fromNode, int fromSlot, object toNode, int toSlot)
    {
        var fromRef = Invoke(fromNode, "GetSlotReference", new object[] { fromSlot });
        var toRef = Invoke(toNode, "GetSlotReference", new object[] { toSlot });
        Invoke(graph, "Connect", new[] { fromRef, toRef });
    }

    static object FindBlockNode(object graph, string descriptor)
    {
        var blockNodeType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.BlockNode");

        foreach (var node in EnumerateContextBlockNodes(graph))
        {
            if (BlockNodeMatchesDescriptor(node, descriptor))
                return node;
        }

        var getNodes = graph.GetType().GetMethod("GetNodes", BindingFlags.Public | BindingFlags.Instance);
        var generic = getNodes.MakeGenericMethod(blockNodeType);
        var nodes = (System.Collections.IEnumerable)generic.Invoke(graph, null);
        foreach (var node in nodes)
        {
            if (BlockNodeMatchesDescriptor(node, descriptor))
                return node;
        }

        throw new InvalidOperationException($"Block not found: {descriptor}");
    }

    static System.Collections.Generic.IEnumerable<object> EnumerateContextBlockNodes(object graph)
    {
        var graphType = graph.GetType();
        foreach (var contextName in new[] { "vertexContext", "fragmentContext" })
        {
            var context = graphType.GetProperty(contextName)?.GetValue(graph);
            if (context == null)
                continue;

            var blocks = context.GetType().GetProperty("blocks")?.GetValue(context) as System.Collections.IEnumerable;
            if (blocks == null)
                continue;

            foreach (var blockRef in blocks)
            {
                var block = blockRef.GetType().GetProperty("value")?.GetValue(blockRef);
                if (block != null)
                    yield return block;
            }
        }
    }

    static bool BlockNodeMatchesDescriptor(object node, string descriptor)
    {
        var nodeType = node.GetType();

        var serializedDesc = nodeType.GetProperty("serializedDescriptor")?.GetValue(node) as string;
        if (!string.IsNullOrEmpty(serializedDesc) && serializedDesc == descriptor)
            return true;

        var nodeName = nodeType.GetProperty("name")?.GetValue(node) as string;
        if (nodeName == descriptor)
            return true;

        var blockDesc = nodeType.GetProperty("descriptor")?.GetValue(node);
        if (blockDesc != null)
        {
            var descType = blockDesc.GetType();
            var tag = descType.GetProperty("tag")?.GetValue(blockDesc) as string;
            var name = descType.GetProperty("name")?.GetValue(blockDesc) as string;
            if ($"{tag}.{name}" == descriptor)
                return true;
        }

        return false;
    }

    static void SetGeometrySpace(object positionNode, int space)
    {
        var geoType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.GeometryNode");
        var field = geoType.GetField("m_Space", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(positionNode, space);
    }

    static void SetSlotFloat(object node, string nodeTypeName, string slotConstName, float value)
    {
        var slotId = GetConst(ShaderGraphAsm, nodeTypeName, slotConstName);
        var abstractNodeType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.AbstractMaterialNode");
        var slotType = RequireType(ShaderGraphAsm, "UnityEditor.ShaderGraph.Vector1MaterialSlot");
        var findMethod = abstractNodeType.GetMethod("FindInputSlot", BindingFlags.Public | BindingFlags.Instance);
        var findGeneric = findMethod.MakeGenericMethod(slotType);
        var slot = findGeneric.Invoke(node, new object[] { slotId });
        if (slot == null)
            throw new InvalidOperationException($"Slot not found on {nodeTypeName}");

        var valueProp = slotType.GetProperty("value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProp != null)
            valueProp.SetValue(slot, value);
        else
            slotType.GetField("m_Value", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(slot, value);
    }

    static int GetConst(Assembly asm, string typeName, string fieldName)
    {
        var field = RequireType(asm, typeName).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        if (field == null)
            throw new MissingFieldException(typeName, fieldName);
        return (int)field.GetValue(null);
    }

    static Array CreateTypedArray(Type elementType, params object[] items)
    {
        var array = Array.CreateInstance(elementType, items.Length);
        for (var i = 0; i < items.Length; i++)
            array.SetValue(items[i], i);
        return array;
    }

    static object CreateGenericList(Type elementType, params object[] items)
    {
        var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType);
        var add = listType.GetMethod("Add");
        foreach (var item in items)
            add.Invoke(list, new[] { item });
        return list;
    }

    static object GetStaticField(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);

    static object InvokeStatic(Type type, string methodName, object[] args = null)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new MissingMethodException(type.FullName, methodName);
        return method.Invoke(null, args ?? Array.Empty<object>());
    }

    static object Invoke(object target, string methodName, object[] args = null)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return method.Invoke(target, args ?? Array.Empty<object>());
    }

    static void SetField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }

        throw new MissingMemberException(target.GetType().FullName, fieldName);
    }

    static void SetEnum(object target, string propertyName, Assembly assembly, string enumFullName, string enumValue)
    {
        var enumType = RequireType(assembly, enumFullName);
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            throw new MissingMemberException(target.GetType().FullName, propertyName);
        prop.SetValue(target, Enum.Parse(enumType, enumValue));
    }
}
