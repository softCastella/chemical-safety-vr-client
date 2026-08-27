# Sci-Fi Card Duplicate Missing Meshes

Date: 2026-07-30

## Symptom

After duplicating `XR UI Canvas` in `3_PPE_Room`, the duplicated cards retained their
serialized colors and material references but their colored surfaces were not visible.

## Cause

The sci-fi cards use generated, scene-owned meshes. Unity duplicated each `MeshFilter`
component and its material reference, but did not duplicate the non-asset `sharedMesh`.
All duplicated card layers therefore serialized `m_Mesh: {fileID: 0}`.

`SciFiCardVisual.Apply` can regenerate the meshes, but it also reapplies authored layer
positions, visibility, collider values, and shared-material colors. It was not suitable as
a targeted repair for an already-authored duplicated Canvas.

## Fix

- Added `SciFiCardVisual.RebuildMissingMeshes`, which recreates only null generated meshes.
- Added `Tools > UI > Repair Missing Sci-Fi Card Geometry` to repair all cards under the
  selected hierarchy without changing authored transforms or materials.
- Added `Tools > UI > Validate Sci-Fi Card Geometry` to report any remaining null card meshes.

## Validation

1. Select the duplicated `XR UI Canvas (1)` root.
2. Run `Tools > UI > Repair Missing Sci-Fi Card Geometry`.
3. Run `Tools > UI > Validate Sci-Fi Card Geometry` and confirm that it reports no missing meshes.
4. Save the scene, enter Play Mode after compilation completes, and confirm the card surfaces
   remain visible with their authored colors.
