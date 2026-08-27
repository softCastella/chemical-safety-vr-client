# Generates Assets/Shaders/BackgroundGradient.shadergraph
# Note: negative coordinates must use parentheses e.g. (-1200) — bare -1200 is parsed as a PS switch.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "Assets\Shaders\BackgroundGradient.shadergraph"
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

function Gid { [guid]::NewGuid().ToString("N") }
function NewGuidStr { [guid]::NewGuid().ToString() }

$I = @{}
@(
    'graph','category','top_color_prop','bottom_color_prop','flip_prop','power_prop',
    'block_v_pos','block_v_norm','block_v_tan','block_f_base','block_f_emit','block_f_alpha',
    'urp_target','urp_unlit','pos_node','pos_out','split_node','split_in','split_r','split_g','split_b','split_a',
    'float_node','float_in','float_out','add_node','add_a','add_b','add_out',
    'sat_node','sat_in','sat_out','pow_node','pow_a','pow_b','pow_out',
    'om_node','om_in','om_out','flip_lerp','flip_la','flip_lb','flip_lt','flip_lo',
    'color_lerp','color_la','color_lb','color_lt','color_lo',
    'top_pn','top_ps','bot_pn','bot_ps','flip_pn','flip_ps','power_pn','power_ps',
    'slot_base_color','slot_emit','slot_alpha','slot_v_pos','slot_v_norm','slot_v_tan'
) | ForEach-Object { $I[$_] = Gid }

$parts = New-Object System.Collections.Generic.List[string]
function Format-UnityJson([string]$json) {
    $json = $json -replace "`r`n", "`n" -replace "`r", ""
    $json = [regex]::Replace($json, '"(x|y|z|w|r|g|b|a)":\s*(-?\d+)(?=[,\n}])', '"$1": $2.0')
    $json = [regex]::Replace($json, '"m_Value":\s*(-?\d+)(?=[,\n}])', '"m_Value": $1.0')
    return $json
}
function Add-Obj($o) { $parts.Add((Format-UnityJson (($o | ConvertTo-Json -Depth 30)))) }

Add-Obj ([ordered]@{
    m_SGVersion = 3
    m_Type = "UnityEditor.ShaderGraph.GraphData"
    m_ObjectId = $I.graph
    m_Properties = @(
        @{ m_Id = $I.top_color_prop }
        @{ m_Id = $I.bottom_color_prop }
        @{ m_Id = $I.flip_prop }
        @{ m_Id = $I.power_prop }
    )
    m_Keywords = @()
    m_Dropdowns = @()
    m_CategoryData = @(@{ m_Id = $I.category })
    m_Nodes = @(
        @{ m_Id = $I.block_v_pos }, @{ m_Id = $I.block_v_norm }, @{ m_Id = $I.block_v_tan },
        @{ m_Id = $I.block_f_base }, @{ m_Id = $I.block_f_emit }, @{ m_Id = $I.block_f_alpha },
        @{ m_Id = $I.pos_node }, @{ m_Id = $I.split_node }, @{ m_Id = $I.float_node }, @{ m_Id = $I.add_node },
        @{ m_Id = $I.sat_node }, @{ m_Id = $I.pow_node }, @{ m_Id = $I.om_node }, @{ m_Id = $I.flip_lerp }, @{ m_Id = $I.color_lerp },
        @{ m_Id = $I.top_pn }, @{ m_Id = $I.bot_pn }, @{ m_Id = $I.flip_pn }, @{ m_Id = $I.power_pn }
    )
    m_GroupDatas = @()
    m_StickyNoteDatas = @()
    m_Edges = @(
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.pos_node }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.split_node }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.split_node }; m_SlotId = 2 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.add_node }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.float_node }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.add_node }; m_SlotId = 1 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.add_node }; m_SlotId = 2 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.sat_node }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.sat_node }; m_SlotId = 1 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.pow_node }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.power_pn }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.pow_node }; m_SlotId = 1 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.pow_node }; m_SlotId = 2 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.om_node }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.pow_node }; m_SlotId = 2 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.flip_lerp }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.om_node }; m_SlotId = 1 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.flip_lerp }; m_SlotId = 1 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.flip_pn }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.flip_lerp }; m_SlotId = 2 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.bot_pn }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.color_lerp }; m_SlotId = 0 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.top_pn }; m_SlotId = 0 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.color_lerp }; m_SlotId = 1 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.flip_lerp }; m_SlotId = 3 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.color_lerp }; m_SlotId = 2 } }
        @{ m_OutputSlot = @{ m_Node = @{ m_Id = $I.color_lerp }; m_SlotId = 3 }; m_InputSlot = @{ m_Node = @{ m_Id = $I.block_f_base }; m_SlotId = 0 } }
    )
    m_VertexContext = @{ m_Position = @{ x = 0; y = 0 }; m_Blocks = @(@{ m_Id = $I.block_v_pos }, @{ m_Id = $I.block_v_norm }, @{ m_Id = $I.block_v_tan }) }
    m_FragmentContext = @{ m_Position = @{ x = 0; y = 200 }; m_Blocks = @(@{ m_Id = $I.block_f_base }, @{ m_Id = $I.block_f_emit }, @{ m_Id = $I.block_f_alpha }) }
    m_PreviewData = @{ serializedMesh = @{ m_SerializedMesh = '{"mesh":{"instanceID":0}}'; m_Guid = "" }; preventRotation = $false }
    m_Path = "Shader Graphs/Background"
    m_GraphPrecision = 1
    m_PreviewMode = 2
    m_OutputNode = @{ m_Id = "" }
    m_SubDatas = @()
    m_ActiveTargets = @(@{ m_Id = $I.urp_target })
})

function NodeBase($id, $name, $x, $y, $w = 208, $h = 120) {
    $x = [double]$x; $y = [double]$y; $w = [double]$w; $h = [double]$h
    return [ordered]@{
        m_SGVersion = 0
        m_Type = ""
        m_ObjectId = $id
        m_Group = @{ m_Id = "" }
        m_Name = $name
        m_DrawState = @{ m_Expanded = $true; m_Position = @{ serializedVersion = "2"; x = $x; y = $y; width = $w; height = $h } }
        m_Slots = @()
        synonyms = @()
        m_Precision = 0
        m_PreviewExpanded = $true
        m_DismissedVersion = 0
        m_PreviewMode = 0
        m_CustomColors = @{ m_SerializableColors = @() }
    }
}

function Add-Dyn($id, $sid, $name, $st) {
    Add-Obj ([ordered]@{
        m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.DynamicVectorMaterialSlot"; m_ObjectId = $id; m_Id = $sid
        m_DisplayName = $name; m_SlotType = $st; m_Hidden = $false; m_ShaderOutputName = $name; m_StageCapability = 3
        m_Value = @{ x = 0; y = 0; z = 0; w = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0; w = 0 }
    })
}

function Add-V1($id, $sid, $name, $st, $val = 0, $stage = 3) {
    Add-Obj ([ordered]@{
        m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.Vector1MaterialSlot"; m_ObjectId = $id; m_Id = $sid
        m_DisplayName = $name; m_SlotType = $st; m_Hidden = $false; m_ShaderOutputName = $name; m_StageCapability = $stage
        m_Value = $val; m_DefaultValue = $val; m_Labels = @()
    })
}

$g1 = NewGuidStr
Add-Obj ([ordered]@{
    m_SGVersion = 3; m_Type = "UnityEditor.ShaderGraph.Internal.ColorShaderProperty"; m_ObjectId = $I.top_color_prop
    m_Guid = @{ m_GuidSerialized = $g1 }; m_Name = "Top Color"; m_DefaultRefNameVersion = 1
    m_RefNameGeneratedByDisplayName = "Top Color"; m_DefaultReferenceName = "_Top_Color"; m_OverrideReferenceName = ""
    m_GeneratePropertyBlock = $true; m_UseCustomSlotLabel = $false; m_CustomSlotLabel = ""; m_DismissedVersion = 0; m_Precision = 0
    overrideHLSLDeclaration = $false; hlslDeclarationOverride = 0; m_Hidden = $false; m_PerRendererData = $false; m_customAttributes = @()
    m_Value = @{ r = 0.53; g = 0.81; b = 0.98; a = 1 }; isMainColor = $false; m_ColorMode = 0
})
$g2 = NewGuidStr
Add-Obj ([ordered]@{
    m_SGVersion = 3; m_Type = "UnityEditor.ShaderGraph.Internal.ColorShaderProperty"; m_ObjectId = $I.bottom_color_prop
    m_Guid = @{ m_GuidSerialized = $g2 }; m_Name = "Bottom Color"; m_DefaultRefNameVersion = 1
    m_RefNameGeneratedByDisplayName = "Bottom Color"; m_DefaultReferenceName = "_Bottom_Color"; m_OverrideReferenceName = ""
    m_GeneratePropertyBlock = $true; m_UseCustomSlotLabel = $false; m_CustomSlotLabel = ""; m_DismissedVersion = 0; m_Precision = 0
    overrideHLSLDeclaration = $false; hlslDeclarationOverride = 0; m_Hidden = $false; m_PerRendererData = $false; m_customAttributes = @()
    m_Value = @{ r = 0.12; g = 0.28; b = 0.55; a = 1 }; isMainColor = $false; m_ColorMode = 0
})
$g3 = NewGuidStr
Add-Obj ([ordered]@{
    m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.Internal.BooleanShaderProperty"; m_ObjectId = $I.flip_prop
    m_Guid = @{ m_GuidSerialized = $g3 }; m_Name = "Flip Gradient"; m_DefaultRefNameVersion = 1
    m_RefNameGeneratedByDisplayName = "Flip Gradient"; m_DefaultReferenceName = "_Flip_Gradient"; m_OverrideReferenceName = ""
    m_GeneratePropertyBlock = $true; m_UseCustomSlotLabel = $false; m_CustomSlotLabel = ""; m_DismissedVersion = 0; m_Precision = 0
    overrideHLSLDeclaration = $false; hlslDeclarationOverride = 0; m_Hidden = $false; m_Value = $false
})
$g4 = NewGuidStr
Add-Obj ([ordered]@{
    m_SGVersion = 1; m_Type = "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty"; m_ObjectId = $I.power_prop
    m_Guid = @{ m_GuidSerialized = $g4 }; m_Name = "Gradient Power"; m_DefaultRefNameVersion = 1
    m_RefNameGeneratedByDisplayName = "Gradient Power"; m_DefaultReferenceName = "_Gradient_Power"; m_OverrideReferenceName = ""
    m_GeneratePropertyBlock = $true; m_UseCustomSlotLabel = $false; m_CustomSlotLabel = ""; m_DismissedVersion = 0; m_Precision = 0
    overrideHLSLDeclaration = $false; hlslDeclarationOverride = 0; m_Hidden = $false; m_Value = 1.0; m_FloatType = 1; m_RangeValues = @{ x = 0.1; y = 5.0 }
})

Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.CategoryData"; m_ObjectId = $I.category; m_Name = ""; m_ChildObjectList = @(@{ m_Id = $I.top_color_prop }, @{ m_Id = $I.bottom_color_prop }, @{ m_Id = $I.flip_prop }, @{ m_Id = $I.power_prop }) })
Add-Obj ([ordered]@{ m_SGVersion = 2; m_Type = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget"; m_ObjectId = $I.urp_unlit })
Add-Obj ([ordered]@{
    m_SGVersion = 1; m_Type = "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget"; m_ObjectId = $I.urp_target
    m_Datas = @(); m_ActiveSubTarget = @{ m_Id = $I.urp_unlit }; m_AllowMaterialOverride = $false; m_SurfaceType = 0
    m_ZTestMode = 4; m_ZWriteControl = 0; m_AlphaMode = 0; m_RenderFace = 2; m_AlphaClip = $false
    m_CastShadows = $false; m_ReceiveShadows = $false; m_DisableTint = $false; m_AdditionalMotionVectorMode = 0
    m_AlembicMotionVectors = $false; m_SupportsLODCrossFade = $false; m_CustomEditorGUI = ""; m_SupportVFX = $false
})

Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot"; m_ObjectId = $I.slot_base_color; m_Id = 0; m_DisplayName = "Base Color"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "BaseColor"; m_StageCapability = 2; m_Value = @{ x = 0.5; y = 0.5; z = 0.5 }; m_DefaultValue = @{ x = 0.5; y = 0.5; z = 0.5 }; m_Labels = @(); m_ColorMode = 0; m_DefaultColor = @{ r = 0.5; g = 0.5; b = 0.5; a = 1 } })
Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.ColorRGBMaterialSlot"; m_ObjectId = $I.slot_emit; m_Id = 0; m_DisplayName = "Emission"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "Emission"; m_StageCapability = 2; m_Value = @{ x = 0; y = 0; z = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0 }; m_Labels = @(); m_ColorMode = 1; m_DefaultColor = @{ r = 0; g = 0; b = 0; a = 1 } })
Add-V1 $I.slot_alpha 0 "Alpha" 0 1 2
Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.PositionMaterialSlot"; m_ObjectId = $I.slot_v_pos; m_Id = 0; m_DisplayName = "Position"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "Position"; m_StageCapability = 1; m_Value = @{ x = 0; y = 0; z = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0 }; m_Labels = @(); m_Space = 0 })
Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.NormalMaterialSlot"; m_ObjectId = $I.slot_v_norm; m_Id = 0; m_DisplayName = "Normal"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "Normal"; m_StageCapability = 1; m_Value = @{ x = 0; y = 0; z = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0 }; m_Labels = @(); m_Space = 0 })
Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.TangentMaterialSlot"; m_ObjectId = $I.slot_v_tan; m_Id = 0; m_DisplayName = "Tangent"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "Tangent"; m_StageCapability = 1; m_Value = @{ x = 0; y = 0; z = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0 }; m_Labels = @(); m_Space = 0 })

function Add-Block($id, $slot, $desc, $name) {
    $n = NodeBase $id $name 0 0 0 0
    $n.m_Type = "UnityEditor.ShaderGraph.BlockNode"
    $n.m_Slots = @(@{ m_Id = $slot })
    $n.m_SerializedDescriptor = $desc
    Add-Obj $n
}
Add-Block $I.block_v_pos $I.slot_v_pos "VertexDescription.Position" "VertexDescription.Position"
Add-Block $I.block_v_norm $I.slot_v_norm "VertexDescription.Normal" "VertexDescription.Normal"
Add-Block $I.block_v_tan $I.slot_v_tan "VertexDescription.Tangent" "VertexDescription.Tangent"
Add-Block $I.block_f_base $I.slot_base_color "SurfaceDescription.BaseColor" "SurfaceDescription.BaseColor"
Add-Block $I.block_f_emit $I.slot_emit "SurfaceDescription.Emission" "SurfaceDescription.Emission"
Add-Block $I.block_f_alpha $I.slot_alpha "SurfaceDescription.Alpha" "SurfaceDescription.Alpha"

Add-Dyn $I.pos_out 0 "Out" 1
$p = NodeBase $I.pos_node "Position" (-1200) 200 208 130
$p.m_SGVersion = 1; $p.m_Type = "UnityEditor.ShaderGraph.PositionNode"; $p.m_Slots = @(@{ m_Id = $I.pos_out }); $p.m_Space = 0; $p.m_PositionSource = 0; $p.m_PreviewMode = 2
Add-Obj $p

Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.DynamicVectorMaterialSlot"; m_ObjectId = $I.split_in; m_Id = 0; m_DisplayName = "In"; m_SlotType = 0; m_Hidden = $false; m_ShaderOutputName = "In"; m_StageCapability = 3; m_Value = @{ x = 0; y = 0; z = 0; w = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0; w = 0 } })
Add-V1 $I.split_r 1 "R" 1; Add-V1 $I.split_g 2 "G" 1; Add-V1 $I.split_b 3 "B" 1; Add-V1 $I.split_a 4 "A" 1
$s = NodeBase $I.split_node "Split" (-960) 200 120 150; $s.m_Type = "UnityEditor.ShaderGraph.SplitNode"; $s.m_Slots = @(@{ m_Id = $I.split_in }, @{ m_Id = $I.split_r }, @{ m_Id = $I.split_g }, @{ m_Id = $I.split_b }, @{ m_Id = $I.split_a }); Add-Obj $s

Add-V1 $I.float_in 1 "X" 0 0.5; Add-V1 $I.float_out 0 "Out" 1 0
$f = NodeBase $I.float_node "Float" (-960) 400 126 77; $f.m_Type = "UnityEditor.ShaderGraph.Vector1Node"; $f.m_Slots = @(@{ m_Id = $I.float_in }, @{ m_Id = $I.float_out }); $f.synonyms = @("Vector 1", "1", "v1", "vec1", "scalar"); Add-Obj $f

Add-Dyn $I.add_a 0 "A" 0; Add-Dyn $I.add_b 1 "B" 0; Add-Dyn $I.add_out 2 "Out" 1
$a = NodeBase $I.add_node "Add" (-720) 200; $a.m_Type = "UnityEditor.ShaderGraph.AddNode"; $a.m_Slots = @(@{ m_Id = $I.add_a }, @{ m_Id = $I.add_b }, @{ m_Id = $I.add_out }); $a.synonyms = @("addition", "sum", "plus"); Add-Obj $a
Add-Dyn $I.sat_in 0 "In" 0; Add-Dyn $I.sat_out 1 "Out" 1
$sn = NodeBase $I.sat_node "Saturate" (-520) 200; $sn.m_Type = "UnityEditor.ShaderGraph.SaturateNode"; $sn.m_Slots = @(@{ m_Id = $I.sat_in }, @{ m_Id = $I.sat_out }); $sn.synonyms = @("clamp"); Add-Obj $sn
Add-Dyn $I.pow_a 0 "A" 0; Add-Dyn $I.pow_b 1 "B" 0; Add-Dyn $I.pow_out 2 "Out" 1
$pw = NodeBase $I.pow_node "Power" (-320) 200; $pw.m_Type = "UnityEditor.ShaderGraph.PowerNode"; $pw.m_Slots = @(@{ m_Id = $I.pow_a }, @{ m_Id = $I.pow_b }, @{ m_Id = $I.pow_out }); Add-Obj $pw
Add-Dyn $I.om_in 0 "In" 0; Add-Dyn $I.om_out 1 "Out" 1
$om = NodeBase $I.om_node "One Minus" (-120) 360; $om.m_Type = "UnityEditor.ShaderGraph.OneMinusNode"; $om.m_Slots = @(@{ m_Id = $I.om_in }, @{ m_Id = $I.om_out }); $om.synonyms = @("complement", "invert", "opposite"); Add-Obj $om
Add-Dyn $I.flip_la 0 "A" 0; Add-Dyn $I.flip_lb 1 "B" 0; Add-Dyn $I.flip_lt 2 "T" 0; Add-Dyn $I.flip_lo 3 "Out" 1
$fl = NodeBase $I.flip_lerp "Lerp" (-120) 200; $fl.m_Type = "UnityEditor.ShaderGraph.LerpNode"; $fl.m_Slots = @(@{ m_Id = $I.flip_la }, @{ m_Id = $I.flip_lb }, @{ m_Id = $I.flip_lt }, @{ m_Id = $I.flip_lo }); $fl.synonyms = @("mix", "blend", "linear interpolate"); Add-Obj $fl
Add-Dyn $I.color_la 0 "A" 0; Add-Dyn $I.color_lb 1 "B" 0; Add-Dyn $I.color_lt 2 "T" 0; Add-Dyn $I.color_lo 3 "Out" 1
$cl = NodeBase $I.color_lerp "Lerp" 120 200; $cl.m_Type = "UnityEditor.ShaderGraph.LerpNode"; $cl.m_Slots = @(@{ m_Id = $I.color_la }, @{ m_Id = $I.color_lb }, @{ m_Id = $I.color_lt }, @{ m_Id = $I.color_lo }); $cl.synonyms = @("mix", "blend", "linear interpolate"); Add-Obj $cl

function Add-PropNode($nid, $sid, $propId, $display, $kind, $x, $y) {
    if ($kind -eq 'color') {
        Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.Vector4MaterialSlot"; m_ObjectId = $sid; m_Id = 0; m_DisplayName = $display; m_SlotType = 1; m_Hidden = $false; m_ShaderOutputName = "Out"; m_StageCapability = 3; m_Value = @{ x = 0; y = 0; z = 0; w = 0 }; m_DefaultValue = @{ x = 0; y = 0; z = 0; w = 0 }; m_Labels = @() })
    }
    elseif ($kind -eq 'bool') {
        Add-Obj ([ordered]@{ m_SGVersion = 0; m_Type = "UnityEditor.ShaderGraph.BooleanMaterialSlot"; m_ObjectId = $sid; m_Id = 0; m_DisplayName = $display; m_SlotType = 1; m_Hidden = $false; m_ShaderOutputName = "Out"; m_StageCapability = 3; m_Value = $false; m_DefaultValue = $false })
    }
    else { Add-V1 $sid 0 $display 1 1 }
    $pn = NodeBase $nid "Property" $x $y 140 36
    $pn.m_Type = "UnityEditor.ShaderGraph.PropertyNode"; $pn.m_Slots = @(@{ m_Id = $sid }); $pn.m_Property = @{ m_Id = $propId }
    Add-Obj $pn
}
Add-PropNode $I.top_pn $I.top_ps $I.top_color_prop "Top Color" color 120 40
Add-PropNode $I.bot_pn $I.bot_ps $I.bottom_color_prop "Bottom Color" color 120 100
Add-PropNode $I.flip_pn $I.flip_ps $I.flip_prop "Flip Gradient" bool (-120) 520
Add-PropNode $I.power_pn $I.power_ps $I.power_prop "Gradient Power" float (-320) 400

$content = ($parts -join "`n`n") + "`n"
$content = $content -replace "`r", ""
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($out, $content, $utf8NoBom)
Write-Host "Wrote $out"
