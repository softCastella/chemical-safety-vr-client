# 2026-07-30 PPE Room Teleport, Floor Grain, and Codex Harness Follow-up

## Basic Info

| Item | Content |
| --- | --- |
| Scene | `Assets/Scenes/3_PPE_Room.unity` |
| Environment | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / XRI 3.4.1 |
| Target | Meta Quest/Android, PC OpenXR |
| Status | Scene and editor-harness updates made; Quest headset verification still required |

## Topics

1. PPE teleport Trigger did not reliably execute after ray hover.
2. The scene needed a clear XRI teleport provider strategy.
3. Lesson scenes were clarified as staging/reference scenes, not the active PPE locomotion source.
4. Arrival orientation did not match the floor-marker arrow direction.
5. Quest headset view showed possible floor-grain cloudiness near the upper lens/view.
6. `CODEX_HARNESS.md` needed to keep useful guardrails without blocking reasonable implementation freedom.

## Decisions

### Teleport Provider Strategy

- Use one active `TeleportationProvider` per active XR Origin/locomotion stack.
- Multiple teleport targets are normal; multiple providers are only needed when there are intentionally separate active locomotion stacks.
- For `3_PPE_Room`, the provider lives under:

```text
XR Origin (VR)
└─ PPE Teleport-Only Locomotion
   ├─ XRBodyTransformer
   ├─ LocomotionMediator
   └─ Teleportation Provider
```

- The PPE room should not depend on `Lessons/Lesson_03.unity` for runtime locomotion. Lesson scenes may receive or stage prefabs, but they should not be treated as the active PPE room provider source unless loaded intentionally.

### Teleport Input and Ray Reach

- Trigger should execute teleport through `Activate`, while Grip remains available for Select/Grab.
- Both active Near-Far Interactors must have:
  - matching left/right `Activate` and `Activate Value` action references;
  - `Allow Hovered Activate` enabled;
  - actual far `CurveInteractionCaster` distance at least as long as the visible ray distance.
- The validation harness must check the active caster, not just reticle visuals.

### Teleport Arrival Direction

- The visible PPE floor marker remains a floor-rotated visual.
- Arrival direction is controlled by the separate scene-authored arrival anchor.
- Arrival anchors must face the marker's arrow/front direction, not a hardcoded world axis.
- Because the marker is rotated flat on the floor, validation uses the marker's horizontal `forward` if available and falls back to horizontal `-up`.
- The current PPE markers resolve to world `+Z` toward the PPE stands.
- `Arrival Forward Offset` remains the authored way to land on the arrow-tip/front side.

### Floor Grain on Quest

- The likely issue is not a transparent/blur shader.
- The risk comes from high-frequency floor grain combined with Quest peripheral optics/rendering, Mobile URP render scale `0.8`, mipmaps, Android texture compression, and Aniso Level `4`.
- Do not reduce render scale as the first response.
- First test should reduce floor grain visual strength and brightness, then increase anisotropic filtering.

### Codex Harness

- `CODEX_HARNESS.md` should describe constraints that prevent repeated mistakes, but it should not force unnecessary planning for tiny local fixes.
- The harness should distinguish between:
  - risky/project-shaping changes, which need inspection and explicit reasoning;
  - narrow local fixes, where implementation can proceed after reading the relevant local context.
- XRI and Unity rules were added or clarified:
  - scene names must come from actual build settings or assets, not guessed display names;
  - Inspector-authored references are authoritative;
  - UI values should not be silently overwritten at runtime;
  - generated scene geometry should not be rebuilt automatically from `OnEnable`/`OnValidate`;
  - Quest/OpenXR Single Pass Instanced shader compatibility must be treated as required for XR-visible custom shaders.

## Documents Created or Updated

- `Docs/Bug/2026-07-30_PPE_Room_Teleportation_Provider_Regression.md`
- `Docs/Bug/2026-07-30_PPE_Room_Teleport_Arrival_Direction.md`
- `Docs/Bug/2026-07-30_PPE_Room_Floor_Grain_Quest_Blur.md`
- `CODEX_HARNESS.md`

## Follow-up Checklist

- [ ] Quest headset: left and right PPE markers hover and Trigger teleport.
- [ ] Quest headset: arrival lands on the arrow-tip/front side.
- [ ] Quest headset: arrival faces the PPE stands.
- [ ] Quest headset: Grip still works for normal Select/Grab.
- [ ] Quest headset: floor grain is compared before/after the reduced-strength material or texture test.
- [ ] Unity Editor: run `Tools > PPE > Validate Teleport-Only Locomotion` after Unity MCP/domain reload stabilizes.
- [ ] Unity Editor: run `Tools > PPE > Validate Card Ray Selection`.
