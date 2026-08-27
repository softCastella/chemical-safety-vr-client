# Mixer Room Ceiling Lighting and Ambient Fill

Date: 2026-07-31

## Scene

- `Assets/Scenes/5_MixerRoom.unity`

## Discussion

The mixer room gained a larger structural/room context, and the requested visual direction was:

- Place ceiling lighting in the mixer room.
- Move smaller lights left and right.
- Add a larger main light at the center ceiling.
- Make the room feel more evenly bright, not only spot-lit in small circular patches.

## Work Performed

Added/generated ceiling light materials under `Assets/Materials/MixerRoom`:

- `MixerRoom_CeilingLight_Housing.mat`
- `MixerRoom_CeilingLight_Diffuser.mat`
- `MixerRoom_MainCeilingLight_Housing.mat`
- `MixerRoom_MainCeilingLight_Diffuser.mat`

Added grouped ceiling lights under:

- `MixerRoom_Ceiling_Lights`

The intended grouped layout is:

- `Ceiling Side Light L`
- `Ceiling Side Light R`
- `Ceiling Main Center Light`

For more uniform room brightness, added:

- `MixerRoom_Uniform_Ambient_Fill`

Lighting settings applied:

- Ambient mode: `Flat`
- Ambient color: `(0.43, 0.47, 0.50, 1)`
- Ambient intensity: `1.12`
- Fill light type: `Directional`
- Fill light intensity: `0.22`
- Fill light shadows: `None`

## Important Authoring Note

An intermediate cleanup pass incorrectly deleted user-authored small lights by treating similarly named objects as generated leftovers. This is documented separately in:

- `Docs/Bug/2026-07-31_MixerRoom_Ceiling_Light_Authoring_Overwrite.md`

Going forward, mixer room lighting edits must preserve existing scene-authored lights and add new lighting under separate roots unless the user explicitly requests replacement.

## Verification

- Unity editor command confirmed the ambient settings were applied.
- Existing small spot lights were not moved or deleted during the ambient-fill pass.
- `git diff --check` passed for `Assets/Scenes/5_MixerRoom.unity`.
