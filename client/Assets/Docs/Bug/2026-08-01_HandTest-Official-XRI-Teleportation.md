# HandTest Official XRI Teleportation Migration

## Symptom

The left controller in `3_PPE_Room_HandTest` had separate Near-Far, general Ray, and Teleport interactors active without the Starter Assets controller input mediator. The manually added ray prefabs also retained scene-relative local positions approximately 12 m away from the tracked controller.

The PPE arrival anchor objects were Transform-only destinations. The visible markers used the project-specific `XRLocationTeleportTarget` on the Default interaction layer, so the official Teleport Interactor, which targets the Teleport interaction layer, could not select them.

## Cause

The generic interactor prefabs were added directly below the custom controller hierarchy without the instance overrides normally supplied by the Starter Assets `XR Origin (XR Rig)` prefab. Handedness, select actions, controller mediation, initial active state, and compatible teleport interactables were therefore missing.

## Resolution

- Keep the existing NearFarInteractor for cards, UI, and normal object interaction.
- Remove the separate general Ray Interactor.
- Add one official Teleport Interactor per controller and align it to the tracked controller origin.
- Add and configure `ControllerInputActionManager` for each hand with teleport-only locomotion enabled.
- Replace the two `XRLocationTeleportTarget` components with official `TeleportationAnchor` components on the Teleport interaction layer.
- Preserve the authored arrival direction and the previous forward arrival offset in each destination Transform.

## Validation

Run `Tools > PPE > Validate Official Teleportation (HandTest)` or invoke `PPEOfficialTeleportationSetup.ValidateBatch` in Unity batch mode. The validation checks controller interactor exclusivity, action references, handedness, initial active states, local alignment, interaction layers, providers, and destination Transforms.

Quest/OpenXR headset validation is still required for both eyes and both controllers. Verify aiming while holding the stick forward, teleport on release, cancellation with Grip, and restoration of the Near-Far ray after teleport mode ends.

## Follow-up: card hover blocked teleport mode

Headset testing found that the Starter Assets `ControllerInputActionManager` disables Teleport Mode while its Near-Far interactor reports a Far selection region. Because the PPE scene uses a 40 m Near-Far cast, scenario cards and world-space UI made the curved ray appear only intermittently.

The scene now uses the project-owned `PPEControllerTeleportModeManager`, which switches directly between the authored Near-Far and Teleport interactors without allowing card hover to disable Teleport Mode. The two active PPE anchors use the `Teleport Target` physics layer, and the teleport projectile raycasts only against that layer so UI and room colliders cannot cut the arc short.
