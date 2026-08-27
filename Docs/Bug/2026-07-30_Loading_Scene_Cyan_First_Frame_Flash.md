# Loading Scene Cyan First-Frame Flash

Date: 2026-07-30

## Symptom

A light-blue or fluorescent cyan/green frame could appear briefly during the startup and intro-to-loading transition. In the remaining form of the defect, the logo, percentage, and loading bar could appear as oversized rectangular UI geometry for a frame.

## Cause

Two first-frame risks were present. The cameras in `0_App`, `1_Title`, `2_Intro`, and `6_LoadingScene` used solid-color clears whose background alpha was serialized as zero. In XR, a transparent camera clear can expose the OpenXR compositor or runtime background before the first complete Unity UI frame is submitted.

After the camera alpha correction, the flash could still occur because all loading UI graphics were visible while the Screen Space Camera Canvas was resolving its XR render size. Its serialized edit-time rect is `100 x 100`, so the logo, percentage, and loading bar could briefly render as oversized cyan/green UI quads before the first stable XR Canvas update. The title scene avoided the same problem by hiding its content during prewarm; the loading scene did not.

## Resolution

The four transition-scene camera background colors now use alpha `1` while preserving their existing RGB values:

- `0_App`: opaque black
- `1_Title`: opaque white
- `2_Intro`: opaque black
- `6_LoadingScene`: opaque black

No runtime script assigns camera clear flags or background colors. The scene-authored camera settings remain authoritative.

The loading Canvas now owns a scene-authored `CanvasGroup` with alpha `0`. `LoadingSceneController.Awake` keeps the complete loading UI hidden, performs the authored number of XR prewarm frames and a forced Canvas update, then changes only the visibility state to alpha `1`. The first frames therefore contain only the opaque black camera clear.

The authoritative scene currently uses `Prewarm Frames = 2`. This value is serialized in `6_LoadingScene`; runtime code does not replace it.

`LoadingSceneBuilder.Validate` now rejects a loading camera that is not configured as a solid, opaque black clear or a loading Canvas whose prewarm CanvasGroup is not authored hidden and non-interactive.

## Validation

Validate on both the Game view and a Quest/OpenXR headset:

- launch from `0_App` and observe the first submitted frame;
- watch the transitions from title to intro and from intro to loading;
- confirm that no light-blue compositor frame appears;
- confirm the first two loading-scene frames remain black before the complete UI appears at its stable size;
- confirm that the title retains its white background and the intro/loading scenes retain black backgrounds;
- confirm both eyes show the same background during the transition.

## Status

The opaque camera clears, hidden prewarm CanvasGroup, controller reference, and editor validation are implemented. Unity 6 assembly compilation succeeds. Because the symptom depends on the XR compositor's submitted frames, resolution must still be confirmed on a Quest/OpenXR headset after leaving Play Mode, waiting for compilation to finish, and starting a fresh run.
