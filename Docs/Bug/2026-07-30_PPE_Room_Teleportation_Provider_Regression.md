# PPE Room Teleportation Provider Regression

Date: 2026-07-30

## Symptom

In `Assets/Scenes/3_PPE_Room.unity`, both PPE location markers could be reached by the controller ray, but pulling Trigger no longer moved the XR Origin. The same markers had worked before the teleport implementation was migrated to XRI's provider pipeline.

## Cause

Two independent parts of the previous custom path were removed at the same time:

- `XRLocationTeleportTarget` stopped changing the XR Origin transform directly and began queueing a `TeleportRequest` through `TeleportationProvider`.
- The active hand-authored Near-Far Interactors, which allowed Activate events on hovered targets, were replaced by Starter Assets prefab instances whose inherited `Allow Hovered Activate` value was disabled.
- The left active Near-Far Interactor had Trigger Activate references pointing at the right-hand action map, and the visible ray distance was longer than the actual `CurveInteractionCaster` distance.

`3_PPE_Room` had no `TeleportationProvider`, `LocomotionMediator`, or `XRBodyTransformer`, so a request could not be executed even if the Trigger event reached the marker.

## Resolution

The scene now contains a teleport-only XRI locomotion stack under `XR Origin (VR)`:

- `PPE Teleport-Only Locomotion`
  - `XRBodyTransformer`, explicitly referencing the scene XR Origin
  - `LocomotionMediator`
  - `Teleportation Provider`
    - `TeleportationProvider`, explicitly referencing the mediator

No continuous move, turn, jump, climb, or gravity provider is present. Both active controller Near-Far Interactor prefab instances author `Allow Hovered Activate = true` as scene overrides, both use their matching left/right Trigger Activate action references, and both PPE location markers explicitly reference the scene TeleportationProvider. The active far `CurveInteractionCaster` distance is authored to at least 40 m, matching the visible reticle ray distance.

Each marker now uses a separate scene-authored arrival anchor. The visible marker remains rotated flat on the floor, while its arrival anchor faces the marker's authored floor-arrow direction. In the current PPE room layout this is world `+Z`, toward the PPE stands. The existing positive `Arrival Forward Offset` therefore places the user 0.7 m beyond the marker on the arrow-tip side and aligns the view toward the stands.

## Validation

Run `Tools > PPE > Validate Teleport-Only Locomotion` or invoke `PPERoomTeleportationSetup.Validate` in Unity batch mode. The harness checks the complete provider chain, both marker references, both active controller interactor overrides, matching left/right Trigger action references, active far caster distance, marker forward indicators, arrival anchor forward alignment, and the absence of other active locomotion providers.

Headset validation must still confirm:

- left and right controller rays hover both PPE markers;
- Trigger teleports while Grip remains available for Select/Grab;
- current head height is preserved;
- destination forward alignment and arrival offset match the authored marker values;
- both eyes render the ray, reticle, and markers consistently on Quest/OpenXR.
