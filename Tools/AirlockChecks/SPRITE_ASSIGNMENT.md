# Airlock sprite assignment update

The new assets are in `Assets/Art/MarsColony/Living_Quarters/Airlock_door/`.

Four door-only sprite slices are assigned in `Assets/Prefabs/AirlockDoor.prefab`:

1. Closed: the inner door from existing `Interiorofliving.png`.
2. Slightly open: `Gemini_Generated_Image_akrwcgakrwcgakrw.jpg`.
3. Mostly open: `Gemini_Generated_Image_k04xcjk04xcjk04x.jpg`.
4. Fully open: `Gemini_Generated_Image_k04xcjk04xcjk04px.jpg`.

Only importer metadata slices the source images. No image pixels are modified, no full-building image is swapped during animation, and the SLIGHTLY OPEN label is excluded. All four slices have a centered pivot and identical 2.5 x 2 world-unit bounds with uniform scale. The new JPEGs use 76 pixels/unit to account for their larger source resolution; the existing interior remains at 40 pixels/unit. Source artwork proportions/perspective vary slightly, so the door leaf itself may still shift visually between frames. Verify alignment in Unity before considering the visuals final.

The existing interior's full sprite is retained at fileID 21300000 with the same texture GUID. A second sub-sprite (21300001) supplies the closed door. The prefab has its renderer, solid door barrier, four animation slots, and default 0.8-second animation configured.

**Scene integration is complete:** the user authorized recreating the living interior. SampleScene now connects the existing Life systems building to that interior and has both four-frame doors and the pressure sequence enabled through the current BuildingPortal components. See [current implementation and verification](README.md).

A focused check passed for all four ordered sprite references and preservation of the original full-interior sprite. Unity sprite import, visual alignment, and Play Mode were not executed. The prior runtime test execution was blocked by Windows Application Control. No broad diagnostics or unrelated tests were run for this update.
