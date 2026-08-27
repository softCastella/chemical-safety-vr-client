# PPE Room Hand Model Dull Materials

Date: 2026-07-31

## Symptom

In `Assets/Scenes/3_PPE_Room.unity`, four hand model variants are present, but only the bare-hand variant appears with the intended brighter hand color. The remaining three PPE hand variants look dull or muddy:

- `BareHand_Suit`
- `Glove_Suit`
- `Glove_Suit_Tape`

## Cause

The previous bare-hand fix only covered the independent `LeftHand_BareHand` and `RightHand_BareHand` renderers.

The scene `PPEBareHandAppearance` component references only:

- `leftHandRenderer`
- `rightHandRenderer`

Those references point to the independent bare-hand model path. The suit and glove variants were not part of that color/shape correction path.

The first diagnosis looked at similarly named PPE room materials, but the screenshot confirmed those were not the actual materials driving the selected hand model in `3_PPE_Room`.

The visible suit/glove hand variants were using a mixture of FBX-embedded material references and shared PPE room prop materials directly on their scene renderers. Because those references were not owned by the hand appearance path, changing one or two shared `.mat` files did not reliably affect every hand variant and could also affect non-hand PPE props.

## Fix

The fix is scene-specific and only changes renderer material overrides under the hand model roots in `Assets/Scenes/3_PPE_Room.unity`.

Created dedicated hand-variant materials:

- `Assets/Materials/XR Hands/PPE_HandVariant_Glove_Unlit.mat`
- `Assets/Materials/XR Hands/PPE_HandVariant_Sleeve_Unlit.mat`
- `Assets/Materials/XR Hands/PPE_HandVariant_Tape_Unlit.mat`

The first dedicated-material pass used `3D UI Test/XR/Unlit Base Color`. That made the colors visible but removed all model form, producing flat cyan/yellow silhouettes. The corrected materials use `3D UI Test/XR/Hand Form Unlit`, which remains independent of scene lighting while preserving view-relative form shading from mesh normals.

After visual review, the first corrected pass was still too flat and shifted the glove toward cyan. The final material tuning lowered the glove back toward green-teal and increased `_FormShading` / `_EdgeDarkening` on glove, sleeve, and tape materials so folds and finger/sleeve silhouette changes read more clearly.

Applied scene renderer overrides to these roots:

- `LeftHand_BareHand_Suit`
- `RightHand_BareHand_Suit`
- `LeftHand_Glove_Suit`
- `RightHand_Glove_Suit`
- `LeftHand_Glove_Suit_Tape`
- `RightHand_Glove_Suit_Tape`

Renderer assignment policy:

- Sleeve renderers use `PPE_HandVariant_Sleeve_Unlit`.
- Glove renderers use `PPE_HandVariant_Glove_Unlit`.
- `Orange_Wrist_Packing_Tape` renderers use `PPE_HandVariant_Tape_Unlit`.
- Bare-hand suit skin renderers use the existing left/right bare-hand unlit skin materials.
- Shared PPE room prop materials under `Assets/Materials/PPE/Scene Unlit` were left as shared props and are not the final fix path.

## Verification

- [x] `3_PPE_Room.unity` has renderer overrides under the six hand variant roots.
- [x] 14 hand variant renderers were classified and updated: glove, sleeve, tape, left skin, and right skin.
- [x] Dedicated PPE materials use `XR/Hand Form Unlit` instead of flat base-color unlit.
- [ ] Compare all four hand variants in Unity Scene/Game view.
- [ ] Verify the result on Meta Quest/OpenXR headset, because the original complaint was headset-visible dullness.

## Notes

This is intentionally a scene renderer override, not a global PPE material correction. A broader follow-up could extend `PPEBareHandAppearance` with explicit suit/glove renderer groups, but that should only be done if runtime switching needs per-variant color controls from the Inspector.
