# Satellite alignment minigame

Implemented and assigned in `Assets/Scenes/SampleScene.unity`. No additional Inspector references or image assets are required.

## Flow and controls

1. Approach the existing Operations computer and press E as before.
2. Select the new **Communications >** button in the Operations menu header.
3. Select **Align Satellite Dish** on the supplied Communications page.
4. Left-click and hold the rim/spokes of the left wheel, then move the pointer around its center to steer azimuth. The right wheel controls elevation the same way. Clockwise increases the corresponding angle; counterclockwise decreases it. Release stops input. Multiple revolutions are supported; wheels do not spring back.
5. Bring the amber crosshair into the green target/tolerance marker. Keep signal at or above 95% and error within 2 degrees for 1.5 real seconds. The menu shows TARGET ACQUIRED while holding, then SIGNAL LOCKED.
6. Back or Escape from alignment returns to Communications with the astronaut still movement-locked for the modal menu. Communications Back returns to the electrical page; Escape closes the overall menu using its existing behavior. Orientation is retained throughout navigation.

Once acquired, steering is disabled to preserve the successful direction. Reopening shows the locked state immediately. `ElectricalControls.Dish.SetTarget(azimuth, elevation)` changes the future target, invalidates a prior lock only when the target actually changes, and refreshes an open alignment screen. No clock, camera, electrical allocation, atmosphere, or motor-damage behavior is added.

## State / Communications integration

The project had Communications artwork but no separate Communications gameplay component or menu navigation. Its supplied page is now connected inside the existing ElectricalControls owner rather than introducing another communications manager. Existing electrical pages, interaction targets, movement lock, Canvas, EventSystem, and day/night clock behavior are reused.

`ElectricalControls.Dish` owns the current and target angles, acquisition progress, and locked state. The steering UI, crosshair, signal calculation, dish visualization, and Communications status all read that same model. `ElectricalControls.IsSatelliteAligned` exposes the link state. On successful acquisition the alignment menu calls `NotifySatelliteAligned()`, which updates Communications and publishes the `SatelliteAligned` event once for that acquisition.

Current orientation and locked state persist across menu close/reopen during the current scene/session. Back cancels an incomplete hold, without resetting angles or wheel positions. No disk-save feature is implemented. Communications' baked 100% and Active examples are overlaid with actual signal/link status. Its baked 5 kW reading is a dash because no per-Communications power-draw source exists; future electrical requirements are deliberately not implemented.

## Drawing and artwork

- `Satellite Alignment Control Panel.png` is the supplied full-screen shell. Only its baked data fields, targeting plot, wheel faces, and dish-photo viewport receive dynamic overlays.
- Communications day/night artwork uses `Gemini_Generated_Image_98gfxt98gfxt98gf.jpg` and `Gemini_Generated_Image_geffp6geffp6geff.jpg` from the existing `Menu/Air_Pressure)` folder.
- The habitat/exterior dish is baked into larger images and has no separate movable sprite. The live viewport therefore uses an independent vector dish schematic: a top view rotates with actual azimuth and changes its bowl projection with elevation; a side view pivots the reflector/feed geometry with actual elevation. Pedestals remain fixed. The habitat/building artwork and camera are not rotated or replaced. No RenderTexture, 3D model, new camera, or generated raster artwork is required.
- Wheels are circular UI graphics with rotating rim/spoke geometry. Their pointer hit rectangles stay fixed, avoiding rotation-feedback errors in mouse-angle measurement. Each wheel captures only its own pointer and only accepts the left mouse button. A small center dead zone avoids unstable angles near the hub. Drag threshold is disabled for immediate response.
- Radar projection uses shortest wrapped azimuth error and elevation error relative to the fixed center target. Off-screen aim clamps to the plot edge and is labelled explicitly. Signal is a monotonic exponential of the same planar angular error, calibrated so the tolerance boundary matches the configured success threshold. This is a lightweight alignment puzzle, not a radio-propagation simulation.

## Inspector tuning

On **Electrical controls / ElectricalControls**, expand **Dish** for initial azimuth/elevation, target azimuth/elevation, angular tolerance, success percentage, and hold duration. Defaults match the supplied panel: current 118.9 / 24.7 degrees, target 132.6 / 37.4 degrees, tolerance 2 degrees, threshold 95%, hold 1.5 seconds.

On the same object's **SatelliteAlignmentMenu** component, tune azimuth/elevation sensitivity independently (default 0.1 dish degree per wheel degree) and monitor half-span (45 degrees horizontal, 22.5 vertical). Azimuth wraps 0–360; elevation clamps 0–90. The UI is created lazily on the existing Canvas. Input is event-driven; the only acquisition coroutine runs while a qualifying, unlocked alignment screen is open.

## Modified files

- `ElectricalControls.cs`: Communications navigation/artwork/status, persistent dish model, acquisition notification, and alignment-aware Back/Escape routing. Other equipment pages retain their existing behavior.
- `DishAlignmentModel.cs`: shared angles, shortest-angle math, signal, mechanical limits, continuous hold, lock persistence, and target-change API.
- `SatelliteSteeringWheel.cs`: pointer-angle steering and shared lightweight vector-drawing helpers.
- `SatelliteDiagram.cs`: radar and two-view dish graphics driven by model angles.
- `SatelliteAlignmentMenu.cs`: supplied alignment layout, wheel bindings, live readouts, hold timer, and return navigation.
- `SampleScene.unity` and three artwork importer metadata files: all references/tuning assigned. Original image pixels are unchanged.
- `Tools/SatelliteChecks/`: focused tests of the actual model. No unrelated test suite or project-wide refactor was performed.

## Executed verification

`dotnet run --no-build --project Tools/SatelliteChecks/SatelliteChecks.csproj` passed **26 assertions against the production DishAlignmentModel**, following a clean build with no warnings or errors. Tests cover forward/reverse 359/0 wrap, multiple turns, independent axes, elevation limits, target-relative error, signal strength, tolerance, sustained 1.5-second acquisition, lost-alignment reset, leaving/reopening without resetting angles or lock, protected winning orientation, and changing the target without rewriting UI.

**13 focused scene/configuration checks passed** for component registration, owner/menu references, three assigned artwork references, target angles, tolerance, threshold, and hold duration.

Unity Editor/Play Mode was unavailable. Actual pointer dragging, native Canvas rendering, wheel/radar/dish appearance, E/Back/Escape navigation, and existing-menu regression behavior have not been exercised in Play Mode. Source-level event wiring and scene references are configured, but the successful model checks are not reported as UI/Play Mode tests.
