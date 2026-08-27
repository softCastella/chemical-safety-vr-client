# PPE hand skinned-mesh bone references serialized as materials

Date: 2026-07-31

## Symptom

Opening `Assets/Scenes/3_PPE_Room.unity` logged repeated errors:

`PPtr cast failed when dereferencing! Casting from Material to Transform at FileID 2100000!`

## Cause

Fourteen scene-authored PPE hand `SkinnedMeshRenderer` components had material
asset references serialized inside `m_Bones`. The affected renderers covered the
bare-hand suit, glove suit, and taped-glove suit variants. The material references
could not deserialize as `Transform` objects, leaving their skinning bone entries
missing.

The equivalent renderer hierarchy in `Assets/Scenes/3_PPE_Room_Loco.unity`
retained the correct bone references and provides a deterministic recovery source.

## Repair and regression check

`Assets/Editor/PPEHandBoneReferenceRepair.cs` maps the reference scene's bone
paths to the matching transforms in `3_PPE_Room.unity`, replaces only missing
`SkinnedMeshRenderer.bones` entries, and validates every skinned renderer for a
non-null root bone and non-null bone array.

Menu commands:

- `Tools > PPE > Repair Hand Skinned Mesh Bone References`
- `Tools > PPE > Validate Hand Skinned Mesh Bone References`

The repair intentionally does not copy materials, transforms, active state, or
other scene presentation values from the reference scene.
