# PPE Room Teleport Arrival Direction Mismatch

Date: 2026-07-30

## Symptom

- In `Assets/Scenes/3_PPE_Room.unity`, the two PPE teleport markers visually pointed toward the PPE stands.
- After teleporting, the left marker placed the user facing the rear wall.
- The right marker placed the user facing toward the right-side trash bin area.
- Expected behavior: after arrival, the user should stand at the arrow-tip side of the marker and face the PPE stands.

## Cause

`XRLocationTeleportTarget` uses its serialized `destination` Transform to calculate both arrival position and facing direction:

- `destination.forward` becomes the requested forward direction.
- `arrivalForwardOffset` moves the feet position forward from the destination Transform.

The scene markers are flat floor visuals, and the arrival anchors are separate scene-authored Transforms. The visible floor arrow direction and the two arrival anchors had diverged. Both `PPE_1 Arrival Anchor` and `PPE_2 Arrival Anchor` were authored with yaw `180`, so the teleport destination faced the opposite horizontal direction from the marker arrow.

The marker mesh's `transform.forward` points vertically because the marker is rotated onto the floor. For validation, the usable horizontal marker front must fall back to `-transform.up`, which corresponds to the floor arrow direction.

## Fix

- Updated both arrival anchors in `3_PPE_Room.unity`:
  - `PPE_1 Arrival Anchor`
  - `PPE_2 Arrival Anchor`
- Their local rotation is now identity/yaw `0`, aligning their horizontal forward direction with the marker arrow direction.
- With the existing `arrivalForwardOffset` of about `0.7 m`, the final feet position is now on the arrow-tip/front side of each marker.
- Updated `PPERoomTeleportationSetup` so future setup runs align arrival anchors to the marker's horizontal front direction instead of hardcoding yaw `180`.
- Updated validation so it checks anchor forward against the marker-authored forward direction, not a fixed world axis.

## Files

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Editor/PPERoomTeleportationSetup.cs`

## Validation

File-based verification confirmed:

- `PPE_1 Arrival Anchor` marker front and anchor forward dot product: `1.00`
- `PPE_2 Arrival Anchor` marker front and anchor forward dot product: `1.00`
- Calculated feet position with `0.7 m` forward offset lands on the marker-front side.
- `git diff --check` passed for the changed scene and setup script.

Unity MCP validation timed out while multiple Unity Editor processes were running, so headset validation is still required:

- [ ] Left PPE marker teleports to the marker-front/arrow-tip side.
- [ ] Right PPE marker teleports to the marker-front/arrow-tip side.
- [ ] On arrival, the user faces the PPE stands.
- [ ] Current head height is preserved.
- [ ] Repeated teleports do not accumulate yaw or position drift.
