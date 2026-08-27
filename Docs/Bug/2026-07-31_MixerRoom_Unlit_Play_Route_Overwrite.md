# Mixer Room Unlit Play Route Appeared To Revert Authored Transforms

Date: 2026-07-31

## Symptom

Objects moved in `Assets/Scenes/5_MixerRoom_Unlit.unity` appeared at their old
positions after entering the application Play flow or navigating to build scene
index 5.

## Cause

The edited Unlit scene was not the scene registered at build index 5.
`ProjectSettings/EditorBuildSettings.asset` still pointed to
`Assets/Scenes/5_MixerRoom.unity`, so the application loaded the source scene
with its older transforms. The serialized transforms in the Unlit scene had not
been replaced.

In addition, `MixerRoomXRSetup` used an `InitializeOnLoadMethod` that could open
and save the source Mixer Room scene after a script/domain reload. Although it
did not target the Unlit scene, automatic scene writes made the authoring path
unsafe and difficult to reason about.

## Fix

- Build scene index 5 now points to `Assets/Scenes/5_MixerRoom_Unlit.unity`.
- The source `Assets/Scenes/5_MixerRoom.unity` entry is no longer enabled.
- `MixerRoomXRSetup` no longer runs on editor/domain reload. Its setup remains
  available only through the explicit `Tools > XR` menu command.
- `MixerRoomUnlitSceneConverter` validation now fails if the build route points
  back to the source scene.
- The two blue drums and red bucket are excluded from subsequent bulk Unlit
  conversion so their authored material choices remain authoritative.

## Validation

Run:

`Tools > Mixer Room > Validate 5_MixerRoom_Unlit Material Scope`

The validation checks both the renderer material scope and the build scene
route. A headset/Game view visual pass is still required for final brightness
and shading approval.
