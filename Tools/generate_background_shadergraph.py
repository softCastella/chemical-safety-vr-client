"""Generate BackgroundGradient.shadergraph for Unity URP Unlit vertical gradient."""
import json
import uuid
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "client" / "Assets" / "Shaders" / "BackgroundGradient.shadergraph"


def gid():
    return uuid.uuid4().hex


def dump(obj):
    return json.dumps(obj, indent=4)


def node_base(oid, name, x, y, w=208.0, h=120.0):
    return {
        "m_SGVersion": 0,
        "m_ObjectId": oid,
        "m_Group": {"m_Id": ""},
        "m_Name": name,
        "m_DrawState": {
            "m_Expanded": True,
            "m_Position": {
                "serializedVersion": "2",
                "x": x,
                "y": y,
                "width": w,
                "height": h,
            },
        },
        "synonyms": [],
        "m_Precision": 0,
        "m_PreviewExpanded": True,
        "m_DismissedVersion": 0,
        "m_PreviewMode": 0,
        "m_CustomColors": {"m_SerializableColors": []},
    }


def dynamic_slot(oid, sid, name, slot_type, stage=3):
    return {
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.DynamicVectorMaterialSlot",
        "m_ObjectId": oid,
        "m_Id": sid,
        "m_DisplayName": name,
        "m_SlotType": slot_type,
        "m_Hidden": False,
        "m_ShaderOutputName": name,
        "m_StageCapability": stage,
        "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0},
        "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0},
    }


def v1_slot(oid, sid, name, slot_type, value=0.0, stage=3):
    return {
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.Vector1MaterialSlot",
        "m_ObjectId": oid,
        "m_Id": sid,
        "m_DisplayName": name,
        "m_SlotType": slot_type,
        "m_Hidden": False,
        "m_ShaderOutputName": name,
        "m_StageCapability": stage,
        "m_Value": value,
        "m_DefaultValue": value,
        "m_Labels": [],
    }


def edge(out_node, out_slot, in_node, in_slot):
    return {
        "m_OutputSlot": {"m_Node": {"m_Id": out_node}, "m_SlotId": out_slot},
        "m_InputSlot": {"m_Node": {"m_Id": in_node}, "m_SlotId": in_slot},
    }


def main():
    ids = {k: gid() for k in [
        "graph", "category", "top_color_prop", "bottom_color_prop", "flip_prop", "power_prop",
        "block_v_pos", "block_v_norm", "block_v_tan", "block_f_base", "block_f_emit", "block_f_alpha",
        "urp_target", "urp_unlit", "pos_node", "pos_out", "split_node", "split_in", "split_r", "split_g", "split_b", "split_a",
        "float_node", "float_in", "float_out", "add_node", "add_a", "add_b", "add_out",
        "sat_node", "sat_in", "sat_out", "pow_node", "pow_a", "pow_b", "pow_out",
        "om_node", "om_in", "om_out", "flip_lerp", "flip_la", "flip_lb", "flip_lt", "flip_lo",
        "color_lerp", "color_la", "color_lb", "color_lt", "color_lo",
        "top_pn", "top_ps", "bot_pn", "bot_ps", "flip_pn", "flip_ps", "power_pn", "power_ps",
        "slot_base_color", "slot_emit", "slot_alpha", "slot_v_pos", "slot_v_norm", "slot_v_tan",
    ]}

    objects = []

    # --- Properties ---
    def color_prop(oid, display, color):
        return {
            "m_SGVersion": 3,
            "m_Type": "UnityEditor.ShaderGraph.Internal.ColorShaderProperty",
            "m_ObjectId": oid,
            "m_Guid": {"m_GuidSerialized": str(uuid.uuid4())},
            "m_Name": display,
            "m_DefaultRefNameVersion": 1,
            "m_RefNameGeneratedByDisplayName": display,
            "m_DefaultReferenceName": "_" + display.replace(" ", "_"),
            "m_OverrideReferenceName": "",
            "m_GeneratePropertyBlock": True,
            "m_UseCustomSlotLabel": False,
            "m_CustomSlotLabel": "",
            "m_DismissedVersion": 0,
            "m_Precision": 0,
            "overrideHLSLDeclaration": False,
            "hlslDeclarationOverride": 0,
            "m_Hidden": False,
            "m_PerRendererData": False,
            "m_customAttributes": [],
            "m_Value": {"r": color[0], "g": color[1], "b": color[2], "a": color[3]},
            "isMainColor": False,
            "m_ColorMode": 0,
        }

    objects.append(color_prop(ids["top_color_prop"], "Top Color", (0.53, 0.81, 0.98, 1.0)))
    objects.append(color_prop(ids["bottom_color_prop"], "Bottom Color", (0.12, 0.28, 0.55, 1.0)))

    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.Internal.BooleanShaderProperty",
        "m_ObjectId": ids["flip_prop"],
        "m_Guid": {"m_GuidSerialized": str(uuid.uuid4())},
        "m_Name": "Flip Gradient",
        "m_DefaultRefNameVersion": 1,
        "m_RefNameGeneratedByDisplayName": "Flip Gradient",
        "m_DefaultReferenceName": "_Flip_Gradient",
        "m_OverrideReferenceName": "",
        "m_GeneratePropertyBlock": True,
        "m_UseCustomSlotLabel": False,
        "m_CustomSlotLabel": "",
        "m_DismissedVersion": 0,
        "m_Precision": 0,
        "overrideHLSLDeclaration": False,
        "hlslDeclarationOverride": 0,
        "m_Hidden": False,
        "m_Value": False,
    })

    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty",
        "m_ObjectId": ids["power_prop"],
        "m_Guid": {"m_GuidSerialized": str(uuid.uuid4())},
        "m_Name": "Gradient Power",
        "m_DefaultRefNameVersion": 1,
        "m_RefNameGeneratedByDisplayName": "Gradient Power",
        "m_DefaultReferenceName": "_Gradient_Power",
        "m_OverrideReferenceName": "",
        "m_GeneratePropertyBlock": True,
        "m_UseCustomSlotLabel": False,
        "m_CustomSlotLabel": "",
        "m_DismissedVersion": 0,
        "m_Precision": 0,
        "overrideHLSLDeclaration": False,
        "hlslDeclarationOverride": 0,
        "m_Hidden": False,
        "m_Value": 1.0,
        "m_FloatType": 1,
        "m_RangeValues": {"x": 0.1, "y": 5.0},
    })

    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.CategoryData",
        "m_ObjectId": ids["category"],
        "m_Name": "",
        "m_ChildObjectList": [
            {"m_Id": ids["top_color_prop"]},
            {"m_Id": ids["bottom_color_prop"]},
            {"m_Id": ids["flip_prop"]},
            {"m_Id": ids["power_prop"]},
        ],
    })

    # --- Targets ---
    objects.append({
        "m_SGVersion": 2,
        "m_Type": "UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget",
        "m_ObjectId": ids["urp_unlit"],
    })
    objects.append({
        "m_SGVersion": 1,
        "m_Type": "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget",
        "m_ObjectId": ids["urp_target"],
        "m_Datas": [],
        "m_ActiveSubTarget": {"m_Id": ids["urp_unlit"]},
        "m_AllowMaterialOverride": False,
        "m_SurfaceType": 0,
        "m_ZTestMode": 4,
        "m_ZWriteControl": 0,
        "m_AlphaMode": 0,
        "m_RenderFace": 2,
        "m_AlphaClip": False,
        "m_CastShadows": False,
        "m_ReceiveShadows": False,
        "m_DisableTint": False,
        "m_AdditionalMotionVectorMode": 0,
        "m_AlembicMotionVectors": False,
        "m_SupportsLODCrossFade": False,
        "m_CustomEditorGUI": "",
        "m_SupportVFX": False,
    })

    # --- Block slots ---
    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
        "m_ObjectId": ids["slot_base_color"],
        "m_Id": 0,
        "m_DisplayName": "Base Color",
        "m_SlotType": 0,
        "m_Hidden": False,
        "m_ShaderOutputName": "BaseColor",
        "m_StageCapability": 2,
        "m_Value": {"x": 0.5, "y": 0.5, "z": 0.5},
        "m_DefaultValue": {"x": 0.5, "y": 0.5, "z": 0.5},
        "m_Labels": [],
        "m_ColorMode": 0,
        "m_DefaultColor": {"r": 0.5, "g": 0.5, "b": 0.5, "a": 1.0},
    })
    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.ColorRGBMaterialSlot",
        "m_ObjectId": ids["slot_emit"],
        "m_Id": 0,
        "m_DisplayName": "Emission",
        "m_SlotType": 0,
        "m_Hidden": False,
        "m_ShaderOutputName": "Emission",
        "m_StageCapability": 2,
        "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_Labels": [],
        "m_ColorMode": 1,
        "m_DefaultColor": {"r": 0.0, "g": 0.0, "b": 0.0, "a": 1.0},
    })
    objects.append(v1_slot(ids["slot_alpha"], 0, "Alpha", 0, 1.0, 2))
    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.PositionMaterialSlot",
        "m_ObjectId": ids["slot_v_pos"],
        "m_Id": 0,
        "m_DisplayName": "Position",
        "m_SlotType": 0,
        "m_Hidden": False,
        "m_ShaderOutputName": "Position",
        "m_StageCapability": 1,
        "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_Labels": [],
        "m_Space": 0,
    })
    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.NormalMaterialSlot",
        "m_ObjectId": ids["slot_v_norm"],
        "m_Id": 0,
        "m_DisplayName": "Normal",
        "m_SlotType": 0,
        "m_Hidden": False,
        "m_ShaderOutputName": "Normal",
        "m_StageCapability": 1,
        "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_Labels": [],
        "m_Space": 0,
    })
    objects.append({
        "m_SGVersion": 0,
        "m_Type": "UnityEditor.ShaderGraph.TangentMaterialSlot",
        "m_ObjectId": ids["slot_v_tan"],
        "m_Id": 0,
        "m_DisplayName": "Tangent",
        "m_SlotType": 0,
        "m_Hidden": False,
        "m_ShaderOutputName": "Tangent",
        "m_StageCapability": 1,
        "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0},
        "m_Labels": [],
        "m_Space": 0,
    })

    def block_node(oid, slot_oid, descriptor, name):
        n = node_base(oid, name, 0, 0, 0, 0)
        n["m_Type"] = "UnityEditor.ShaderGraph.BlockNode"
        n["m_Slots"] = [{"m_Id": slot_oid}]
        n["m_SerializedDescriptor"] = descriptor
        return n

    objects.append(block_node(ids["block_v_pos"], ids["slot_v_pos"], "VertexDescription.Position", "VertexDescription.Position"))
    objects.append(block_node(ids["block_v_norm"], ids["slot_v_norm"], "VertexDescription.Normal", "VertexDescription.Normal"))
    objects.append(block_node(ids["block_v_tan"], ids["slot_v_tan"], "VertexDescription.Tangent", "VertexDescription.Tangent"))
    objects.append(block_node(ids["block_f_base"], ids["slot_base_color"], "SurfaceDescription.BaseColor", "SurfaceDescription.BaseColor"))
    objects.append(block_node(ids["block_f_emit"], ids["slot_emit"], "SurfaceDescription.Emission", "SurfaceDescription.Emission"))
    objects.append(block_node(ids["block_f_alpha"], ids["slot_alpha"], "SurfaceDescription.Alpha", "SurfaceDescription.Alpha"))

    # --- Position ---
    objects.append(dynamic_slot(ids["pos_out"], 0, "Out", 1, 1))
    pos = node_base(ids["pos_node"], "Position", -1200, 200, 208, 130)
    pos.update({
        "m_SGVersion": 1,
        "m_Type": "UnityEditor.ShaderGraph.PositionNode",
        "m_Slots": [{"m_Id": ids["pos_out"]}],
        "m_Space": 0,
        "m_PositionSource": 0,
        "m_PreviewMode": 2,
    })
    objects.append(pos)

    # --- Split ---
    for sid, name, st in [
        (ids["split_in"], "In", 0), (ids["split_r"], "R", 1), (ids["split_g"], "G", 1),
        (ids["split_b"], "B", 1), (ids["split_a"], "A", 1),
    ]:
        objects.append({
            "m_SGVersion": 0,
            "m_Type": "UnityEditor.ShaderGraph.Vector1MaterialSlot" if name != "In" else "UnityEditor.ShaderGraph.DynamicVectorMaterialSlot",
            "m_ObjectId": sid,
            "m_Id": {"In": 0, "R": 1, "G": 2, "B": 3, "A": 4}[name],
            "m_DisplayName": name,
            "m_SlotType": st,
            "m_Hidden": False,
            "m_ShaderOutputName": name,
            "m_StageCapability": 3 if name == "In" else 2,
            **({"m_Value": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0}, "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0}} if name == "In" else {"m_Value": 0.0, "m_DefaultValue": 0.0, "m_Labels": []}),
        })
    split = node_base(ids["split_node"], "Split", -960, 200, 120, 150)
    split.update({
        "m_Type": "UnityEditor.ShaderGraph.SplitNode",
        "m_Slots": [{"m_Id": ids["split_in"]}, {"m_Id": ids["split_r"]}, {"m_Id": ids["split_g"]}, {"m_Id": ids["split_b"]}, {"m_Id": ids["split_a"]}],
    })
    objects.append(split)

    # --- Float 0.5 ---
    objects.append(v1_slot(ids["float_in"], 1, "X", 0, 0.5))
    objects.append(v1_slot(ids["float_out"], 0, "Out", 1, 0.0))
    fl = node_base(ids["float_node"], "Float", -960, 400, 126, 77)
    fl.update({
        "m_Type": "UnityEditor.ShaderGraph.Vector1Node",
        "m_Slots": [{"m_Id": ids["float_in"]}, {"m_Id": ids["float_out"]}],
        "synonyms": ["Vector 1", "1", "v1", "vec1", "scalar"],
    })
    objects.append(fl)

    # --- Math nodes ---
    def code_node(key, type_name, x, y, slots):
        n = node_base(ids[key], type_name.split(".")[-1].replace("Node", ""), x, y)
        n["m_Type"] = type_name
        n["m_Slots"] = [{"m_Id": ids[s]} for s in slots]
        objects.append(n)

    objects.append(dynamic_slot(ids["add_a"], 0, "A", 0))
    objects.append(dynamic_slot(ids["add_b"], 1, "B", 0))
    objects.append(dynamic_slot(ids["add_out"], 2, "Out", 1))
    code_node("add_node", "UnityEditor.ShaderGraph.AddNode", -720, 200, ["add_a", "add_b", "add_out"])

    objects.append(dynamic_slot(ids["sat_in"], 0, "In", 0))
    objects.append(dynamic_slot(ids["sat_out"], 1, "Out", 1))
    code_node("sat_node", "UnityEditor.ShaderGraph.SaturateNode", -520, 200, ["sat_in", "sat_out"])

    objects.append(dynamic_slot(ids["pow_a"], 0, "A", 0))
    objects.append(dynamic_slot(ids["pow_b"], 1, "B", 0))
    objects.append(dynamic_slot(ids["pow_out"], 2, "Out", 1))
    code_node("pow_node", "UnityEditor.ShaderGraph.PowerNode", -320, 200, ["pow_a", "pow_b", "pow_out"])

    objects.append(dynamic_slot(ids["om_in"], 0, "In", 0))
    objects.append(dynamic_slot(ids["om_out"], 1, "Out", 1))
    code_node("om_node", "UnityEditor.ShaderGraph.OneMinusNode", -120, 360, ["om_in", "om_out"])

    for s, n in [("flip_la", "A"), ("flip_lb", "B"), ("flip_lt", "T"), ("flip_lo", "Out")]:
        objects.append(dynamic_slot(ids[s], {"A": 0, "B": 1, "T": 2, "Out": 3}[n], n, 0 if n != "Out" else 1))
    code_node("flip_lerp", "UnityEditor.ShaderGraph.LerpNode", -120, 200, ["flip_la", "flip_lb", "flip_lt", "flip_lo"])

    for s, n in [("color_la", "A"), ("color_lb", "B"), ("color_lt", "T"), ("color_lo", "Out")]:
        objects.append(dynamic_slot(ids[s], {"A": 0, "B": 1, "T": 2, "Out": 3}[n], n, 0 if n != "Out" else 1))
    code_node("color_lerp", "UnityEditor.ShaderGraph.LerpNode", 120, 200, ["color_la", "color_lb", "color_lt", "color_lo"])

    # --- Property nodes ---
    def prop_node(oid, slot_oid, prop_oid, display, slot_type, x, y):
        if slot_type == "color":
            slot = {
                "m_SGVersion": 0,
                "m_Type": "UnityEditor.ShaderGraph.Vector4MaterialSlot",
                "m_ObjectId": slot_oid,
                "m_Id": 0,
                "m_DisplayName": display,
                "m_SlotType": 1,
                "m_Hidden": False,
                "m_ShaderOutputName": "Out",
                "m_StageCapability": 3,
                "m_Value": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0},
                "m_DefaultValue": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 0.0},
                "m_Labels": [],
            }
        elif slot_type == "bool":
            slot = {
                "m_SGVersion": 0,
                "m_Type": "UnityEditor.ShaderGraph.BooleanMaterialSlot",
                "m_ObjectId": slot_oid,
                "m_Id": 0,
                "m_DisplayName": display,
                "m_SlotType": 1,
                "m_Hidden": False,
                "m_ShaderOutputName": "Out",
                "m_StageCapability": 3,
                "m_Value": False,
                "m_DefaultValue": False,
            }
        else:
            slot = v1_slot(slot_oid, 0, display, 1, 1.0)
        objects.append(slot)
        pn = node_base(oid, "Property", x, y, 140, 36)
        pn.update({
            "m_Type": "UnityEditor.ShaderGraph.PropertyNode",
            "m_Slots": [{"m_Id": slot_oid}],
            "m_Property": {"m_Id": prop_oid},
        })
        objects.append(pn)

    prop_node(ids["top_pn"], ids["top_ps"], ids["top_color_prop"], "Top Color", "color", 120, 40)
    prop_node(ids["bot_pn"], ids["bot_ps"], ids["bottom_color_prop"], "Bottom Color", "color", 120, 100)
    prop_node(ids["flip_pn"], ids["flip_ps"], ids["flip_prop"], "Flip Gradient", "bool", -120, 520)
    prop_node(ids["power_pn"], ids["power_ps"], ids["power_prop"], "Gradient Power", "float", -320, 400)

    edges = [
        edge(ids["pos_node"], 0, ids["split_node"], 0),
        edge(ids["split_node"], 2, ids["add_node"], 0),
        edge(ids["float_node"], 0, ids["add_node"], 1),
        edge(ids["add_node"], 2, ids["sat_node"], 0),
        edge(ids["sat_node"], 1, ids["pow_node"], 0),
        edge(ids["power_pn"], 0, ids["pow_node"], 1),
        edge(ids["pow_node"], 2, ids["om_node"], 0),
        edge(ids["pow_node"], 2, ids["flip_lerp"], 0),
        edge(ids["om_node"], 1, ids["flip_lerp"], 1),
        edge(ids["flip_pn"], 0, ids["flip_lerp"], 2),
        edge(ids["bot_pn"], 0, ids["color_lerp"], 0),
        edge(ids["top_pn"], 0, ids["color_lerp"], 1),
        edge(ids["flip_lerp"], 3, ids["color_lerp"], 2),
        edge(ids["color_lerp"], 3, ids["block_f_base"], 0),
    ]

    graph = {
        "m_SGVersion": 3,
        "m_Type": "UnityEditor.ShaderGraph.GraphData",
        "m_ObjectId": ids["graph"],
        "m_Properties": [{"m_Id": ids[k]} for k in ["top_color_prop", "bottom_color_prop", "flip_prop", "power_prop"]],
        "m_Keywords": [],
        "m_Dropdowns": [],
        "m_CategoryData": [{"m_Id": ids["category"]}],
        "m_Nodes": [{"m_Id": ids[k]} for k in [
            "block_v_pos", "block_v_norm", "block_v_tan", "block_f_base", "block_f_emit", "block_f_alpha",
            "pos_node", "split_node", "float_node", "add_node", "sat_node", "pow_node", "om_node",
            "flip_lerp", "color_lerp", "top_pn", "bot_pn", "flip_pn", "power_pn",
        ]],
        "m_GroupDatas": [],
        "m_StickyNoteDatas": [],
        "m_Edges": edges,
        "m_VertexContext": {
            "m_Position": {"x": 0.0, "y": 0.0},
            "m_Blocks": [{"m_Id": ids[k]} for k in ["block_v_pos", "block_v_norm", "block_v_tan"]],
        },
        "m_FragmentContext": {
            "m_Position": {"x": 0.0, "y": 200.0},
            "m_Blocks": [{"m_Id": ids[k]} for k in ["block_f_base", "block_f_emit", "block_f_alpha"]],
        },
        "m_PreviewData": {
            "serializedMesh": {"m_SerializedMesh": "{\"mesh\":{\"instanceID\":0}}", "m_Guid": ""},
            "preventRotation": False,
        },
        "m_Path": "Shader Graphs/Background",
        "m_GraphPrecision": 1,
        "m_PreviewMode": 2,
        "m_OutputNode": {"m_Id": ""},
        "m_SubDatas": [],
        "m_ActiveTargets": [{"m_Id": ids["urp_target"]}],
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    parts = [dump(graph)] + [dump(o) for o in objects]
    OUT.write_text("\n\n".join(parts) + "\n", encoding="utf-8")
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
