# Mixer Room Ceiling Light Authoring Overwrite

Date: 2026-07-31

## Symptom

While editing `Assets/Scenes/5_MixerRoom.unity`, user-authored small ceiling lights were overwritten or removed during an automated lighting rework.

The user had manually arranged small lights in the mixer room. The automated pass treated similarly named `MixerRoom_Ceiling_Center_Light*` objects as generated leftovers and deleted them while creating a new grouped layout.

## Cause

The edit violated the project authoring rule that scene-authored values are authoritative.

Specific failure:

- Existing scene objects were classified by name pattern alone.
- The command assumed `MixerRoom_Ceiling_Center_Light*` objects were stale generated duplicates.
- The command deleted those objects instead of preserving them or adding new lighting beside them.
- The user was not asked before a destructive cleanup.

## Recovery Attempt

- Unity `Undo.PerformUndo()` was attempted after the issue was reported.
- The deleted `MixerRoom_Ceiling_Center_Light*` objects did not return.
- Project search did not find a separate `.unity`, `.prefab`, `.bak`, or `.tmp` copy containing those deleted object names.
- Current scene still contains multiple small spot lights and the later grouped ceiling lights, but exact user-authored deleted transforms could not be recovered from available project files.

## Correct Handling Going Forward

- Do not delete existing scene-authored lighting objects during automated scene edits.
- Add new generated lighting under a clearly named separate root.
- If cleanup appears necessary, report the candidate objects first and wait for explicit approval.
- Classify generated objects with a dedicated marker/component or stable root ownership, not by loose name matching.
- Preserve manually edited transforms, light intensities, ranges, colors, and active states unless the user explicitly asks to overwrite them.

## Current Mitigation

For the later brightness request, the edit avoided moving or deleting existing ceiling lights. It added only:

- `MixerRoom_Uniform_Ambient_Fill`
- Flat ambient RenderSettings adjustment

This keeps existing spot lights in place while raising the room's base brightness.
