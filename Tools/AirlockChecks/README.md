# Living quarters and airlock

Implemented in `Assets/Scenes/SampleScene.unity`. The existing **Life systems** building is the living-quarters entrance. Its Transform and surrounding buildings are preserved; its sprite reference now points to the supplied individual `Living_Quarters/life_support_day.png` rather than an unavailable atlas reference.

Approach the front/bottom doorway to enter automatically. Wait inside the chamber for the doors and pressure cycle, then walk north through the inner door. To leave, approach the inner airlock from the habitat, enter the chamber, wait for depressurization, and walk south through the outer doorway. No additional E command or Inspector wiring is needed.

## Scene configuration

- Living Quarters Interior is at world (200, 0), using the unchanged `Interiorofliving.png` full sprite at its native aspect ratio and 40 pixels/unit.
- A continuous EdgeCollider2D perimeter follows the wall outline. Individual box colliders cover beds, desks, crates, hygiene fixtures, EVA equipment, tanks, consoles, and airlock frames, with open room approaches.
- The existing Life systems building has compound shell/machinery colliders and a clear doorway, plus paired BuildingPortal triggers and SceneSpawn destinations.
- The existing camera is centered on the interior for this visit. The return spawn restores its original player parent, local transform, and zoom. The astronaut's root scale, capsule, movement, and animation controller are unchanged.
- Inner and outer AirlockDoor components have all four sprite references assigned. The outer door also animates a linked exterior door renderer. Their collision barriers start closed, and the AirlockCycle component is enabled.

## Four images / alignment

Opening order is closed -> slightly open -> mostly open -> fully open; closing reverses the same sequence.

1. Closed: door-only slice of existing `Interiorofliving.png`.
2. Slightly open: `Airlock_door/Gemini_Generated_Image_akrwcgakrwcgakrw.jpg`.
3. Mostly open: `Airlock_door/Gemini_Generated_Image_k04xcjk04xcjk04x.jpg`.
4. Fully open: `Airlock_door/Gemini_Generated_Image_k04xcjk04xcjk04px.jpg`.

The new files are full-building images. Importer metadata slices only the door regions; the surrounding building is never swapped or animated. The original full interior sprite keeps its texture GUID and fileID 21300000. All four door sprites have centered pivots and matching 2.5 x 2 world-unit bounds without stretching. The JPEGs use 76 pixels/unit to account for their higher resolution. No image pixels were regenerated or edited. The supplied door views differ slightly in their internal perspective/proportions, so visual alignment still needs a Unity check.

## Timing and interlocks

Each AirlockDoor's Animation Seconds defaults to 0.8 real seconds. Stabilization Seconds and Depressurization Seconds on Living airlock sequence default to 3 real seconds each.

Entry: outer opens -> existing portal places the astronaut in the chamber -> outer closes fully -> stabilization timer -> inner opens -> astronaut clears the inner door -> inner closes.

Exit: approach opens inner while outer stays closed -> astronaut clears the inner threshold into the chamber -> inner closes fully -> depressurization timer -> outer opens -> existing portal returns the astronaut outside -> outer closes.

Closed and animating doors are solid; only a fully opened door permits passage. Crossing sensors are positioned beyond the thresholds so doors do not close on an astronaut still passing through them. The outer door owns separate barriers in the chamber and exterior view. Pending exit contact is retained while the outer door animates, avoiding lost trigger events. Input-based portal interaction cannot bypass an enabled, configured airlock. `AirlockCycle.SafeToProceed` remains the single future confirmation-audio hook; no audio or atmosphere system was added.

## Changed files

- `SampleScene.unity`: restored living interior, continuous wall boundary, furniture/exterior hitboxes, spawn points, current BuildingPortal components, enabled airlock, door renderers/barriers, and crossing sensors.
- `BuildingPortal.cs`: optional automatic airlock routing and a guarded local-transition completion method. Existing greenhouse and unrelated portals retain their existing interaction path.
- `SceneSpawn.cs`: optional interior camera focus and paired camera restoration. Existing spawns without these options behave as before.
- `AirlockCycle.cs`: uses the current BuildingPortal architecture, checks transition success, and retains exit contact during outer-door opening.
- `AirlockDoor.cs`: optional linked exterior renderer uses the same four-frame sequence.
- `BuildingEntrance.cs`: disconnected its obsolete optional airlock hook so the new cycle uses only the project's current portals; legacy transition behavior remains available.
- `AirlockDoor.prefab`: assigned door sprites and its own sorting layer for placement above interior artwork.
- `Living_Quarters/life_support_day.png.meta`: explicitly centered the existing cropped exterior sprite pivot to match its measured hitboxes.
- `Tools/AirlockChecks/`: focused production-script compile checks and current implementation documentation.

MarsClock, solar arrays, batteries, greenhouse growth, astronaut movement, and original image files were not changed.

## Verification and limits

- Focused harness build: `dotnet build Tools/AirlockChecks/AirlockChecks.csproj --no-restore` succeeded with zero warnings/errors, compiling production AirlockDoor, AirlockCycle, and AirlockSensor with minimal engine stand-ins.
- 731 focused static scene/geometry checks passed: new local references, safe spawn/approach positions, routes to all seven room areas and the exit, continuous perimeter containment, and inner/outer barrier routes under closed and open configurations. These used the actual scene's scaled capsule bounding footprint (0.1225 x 0.09 units) and conservative wall clearance. They do not simulate Unity physics.
- `dotnet run --no-build --project Tools/AirlockChecks/AirlockChecks.csproj` was attempted normally and outside the sandbox. Windows Application Control blocked the compiled test assembly (0x800711C7). **No runtime assertions are claimed as passed.**
- Unity Editor is unavailable here. Unity compilation, actual trigger/physics contacts, frame alignment, animation timing, camera appearance, and full entry/exit gameplay have not been exercised in Play Mode. All scene references are assigned; no manual setup is outstanding.

The provided interior has an opaque checkerboard background. It remains as supplied. Painted doorway panels are traversable only when their matching airlock barrier opens; other painted room-door openings remain navigable without adding unrelated door mechanics.
