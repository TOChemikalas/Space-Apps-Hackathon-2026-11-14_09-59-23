# Airlock door sequence

## Status / missing assets

The four supplied door-opening images were not found in the project or the task attachment. No replacement images were created and no sprites were guessed. The logic and references are prepared in SampleScene, but the new AirlockCycle component is deliberately **disabled** and its new door barriers are initially disabled. The existing building entrance continues to work until the airlock is configured. The animated airlock is not complete or playable yet.

## Required Inspector setup once images are available

1. Select Main Building Interior / Airlock inner door. On AirlockDoor, assign Opening Frames in exact order: closed, beginning to open, mostly open, fully open. Do the same on Airlock outer door using sprites matching that doorway's perspective. Existing renderer and barrier references are already assigned.
2. Keep the supplied sprites' original pivot, alignment, and scale. The code changes only SpriteRenderer.sprite. Their visual placement cannot be verified until the actual images are available. The current sprite renderers are empty; surrounding interior/exterior art is unchanged.
3. On Main Building Interior / Airlock cycle - assign door sprites before enabling, enable AirlockCycle **before starting Play mode**, with the astronaut outside. Default stabilization and depressurization delays are 3 real seconds each; both are editable. Animation duration defaults to 0.8 real seconds per door and is editable on each AirlockDoor.

All player, portal, renderer, barrier, and crossing-sensor references are wired. The sequence uses the existing player Rigidbody2D and existing BuildingEntrance teleport/camera behavior. It does not change movement code or camera code.

## Behavior

Approaching the exterior doorway starts the outer door's four-frame opening. Once fully open, the existing building transition places the astronaut inside the chamber. The outer door closes, pressure stabilizes for the configured delay, then the inner door opens. Moving fully clear of the inner door into the habitat closes it.

Approaching the inner door from the habitat opens it while keeping the outer door closed. Crossing fully into the chamber closes the inner door, waits for depressurization, then opens the outer door. The existing exit portal returns the astronaut outside, and the outer door closes. An exit request already touching the exit trigger is retained until opening finishes; leaving that trigger cancels the request. Both doors cannot be opened by conflicting requests, and only the configured astronaut activates the sensors.

Closed and animating doors block movement. Collision is enabled as closing starts and disabled only after all four opening frames finish. Barrier placement bridges each chamber doorway so the astronaut cannot walk around it. The outer door controls both the chamber's outer barrier and a new exterior threshold barrier; existing exterior hitboxes and interior object colliders are unchanged.

`AirlockCycle.SafeToProceed` is the single future safe-to-proceed audio hook. No audio assets or system are added.

## Modified files

- Added AirlockDoor.cs (+ metadata): non-looping ascending/reverse sprite animation and door-only collision control.
- Added AirlockCycle.cs (+ metadata): event-driven entry/exit sequence and two configurable pressure delays.
- Added AirlockSensor.cs (+ metadata): player-only approach/crossing callbacks.
- BuildingEntrance.cs: optional airlock routing and a completion method that retains the existing transition guards. Existing portals without an enabled/configured airlock keep their prior behavior.
- SampleScene.unity: optional controller, empty door renderers, initially disabled barriers, crossing triggers, and references on the existing two main-building portals. No surrounding sprites, existing colliders, rendering order, camera configuration, or layout were changed.
- Tools/AirlockChecks/: focused checks compiling the production airlock scripts with minimal engine stand-ins.
- Tools/GreenhouseChecks/UnityStubs.cs: a small optional-airlock stand-in keeps the pre-existing standalone transition harness compatible with BuildingEntrance's new optional reference. That unrelated suite was not run for this task.

## Focused verification

`dotnet run --project Tools/AirlockChecks/AirlockChecks.csproj`

The C# test harness compiled with the production airlock scripts. Execution was blocked by Windows Application Control (0x800711C7), including an attempt outside the sandbox. **No assertions ran and none are reported as passed.** The focused checks cover frame order, overlapping-request rejection, closed/animating gate collision flags, pressure timing, queued exit, player identity, and entry/exit interlocks. There are no packages to install.

A focused serialized-reference check confirmed the optional cycle is disabled and all three new solid barriers are disabled while sprites are missing. Code paths were reviewed for entry/exit ordering. Unity Play Mode, actual trigger contacts, door alignment/rendering, and runtime sequencing have not been executed. No broad gameplay test suite was run.
