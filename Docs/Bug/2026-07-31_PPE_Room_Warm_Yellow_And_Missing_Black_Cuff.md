# PPE Room Warm Yellow And Missing Black Cuff

Date: 2026-07-31

## Symptom

In `Assets/Scenes/3_PPE_Room.unity`, the active glove-and-suit hand models
rendered with a cool/green-shifted yellow sleeve. The black elastic rubber
cuff between the glove wrist and protective sleeve was no longer visible.

Reference images:

- `Img/장갑방호복손모델.png`
- `Img/방호복색깔.png`

## Cause

The active `LeftHand_Glove_Suit` and `RightHand_Glove_Suit` FBX assets still
contain the `Black_Integrated_Elastic_Cuff` submesh. Their
`Yellow_Protective_Sleeve` renderers expose two material slots:

1. integrated elastic cuff
2. protective sleeve

The previous scene-specific hand material normalization assigned
`PPE_HandVariant_Sleeve_Unlit` to both slots. The cuff geometry and skinning
were intact, but the second submesh was rendered yellow and visually merged
into the sleeve.

## Fix

- Retuned `PPE_HandVariant_Sleeve_Unlit` to a warmer protective-suit yellow
  derived from the supplied reference image.
- Added `PPE_HandVariant_Cuff_Black_Unlit` using the XR-compatible
  `3D UI Test/XR/Hand Form Unlit` shader.
- Preserved the active hand roots, transforms, meshes, and bone references.
- Assigned the black cuff material to slot 0 and the sleeve material to slot 1
  on both active glove-suit sleeve renderers. The imported FBX submesh order is
  cuff first, gathered sleeve second.
- Applied the same slot order to the `LeftHand_BareHand_Suit` and
  `RightHand_BareHand_Suit` sleeve renderers so the bare-hand variants do not
  render the wrist cuff yellow.
- Added `Tools > PPE > Apply Warm Glove Suit And Black Cuffs` and
  `Tools > PPE > Validate Glove Suit Appearance`.

## Verification

- [x] Both active glove-suit renderers use black cuff material in slot 0.
- [x] Both active glove-suit renderers use warm sleeve material in slot 1.
- [x] Sleeve root bones and all bone references remain intact.
- [x] Unity script compilation completes without errors.
- [ ] Compare both hands visually in Scene/Game view.
- [ ] Verify both eyes on Meta Quest/OpenXR.
