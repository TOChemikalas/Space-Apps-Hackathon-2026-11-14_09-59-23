# Modular greenhouse

Open `Assets/Scenes/SampleScene.unity` and enter Play mode. No additional packages or Inspector wiring are required for the greenhouse.

The greenhouse uses the supplied `Isometric Sci-Fi Greenhouse Facility-1.png` exterior and `Futuristic Glasshouse Hydroponics Lab-2.png` interior. The interior is at world (100, 0), with four beds aligned to the artwork. Exterior and interior remain in the same scene; transitions teleport the existing player and temporarily detach/center the existing camera. Returning restores its parent, position, and zoom. Arrival points are clear of their paired triggers, with an additional cooldown.

## Controls

- WASD / arrows: existing movement.
- Enter the lower-left exterior doorway to go inside. The exterior building is near world (-2.5, -9.1).
- Approach a bed: only the closest unobstructed bed gains a white outline.
- E: open that bed's own menu. Click a crop, then Plant. For planted beds the menu shows progress and remaining Earth days; mature beds offer Harvest.
- Close / Escape: return to movement. A modal menu stops player movement, not MarsClock.
- Walk through the bottom-center interior doorway to return outside.

The menu heading identifies the selected bed. Harvest totals appear at the top of the interior view. Every harvest yields one unit by default; edit each crop asset's quantity to change this.

## Configuration and responsibilities

- `Assets/Data/Crops/`: four editable CropDefinition assets, with IDs, names, Earth-day durations (28/30/90/80), six supplied sprites each, and yield quantities. Runtime access is read-only.
- `CropProgress`: pure independent per-bed state and state-transition rules.
- `GrowingBed`: binds its own state to the existing MarsClock event, renders only stage changes, handles local trigger membership and outline. IDs are unique per runtime instance; save/load is not implemented.
- `GreenhouseInteractionUI`: proximity candidate selection, line-of-sight checks that ignore triggers, modal movement locking, and a harvest ledger shared by this greenhouse's beds.
- `GrowingBedMenu`: a separate component on each bed, with its own selected crop and open state. It follows Cropmenu.png's dark three-column layout: seed selection, current bed preview/progress, and plot details with a green Plant/Harvest action and Back button. Only the approached bed's menu opens; each bed remembers its selection. The reference's decorative water/seed quantities are replaced with actual crop durations, yield, and harvest counts because water and seed resources are not implemented.
- `GreenhouseEntrance`: paired trigger transitions and camera restoration.
- `Assets/Prefabs/GrowingBed.prefab`: configured bed with solid boundary, interaction trigger, crop renderer, and component, including its own menu. Add more copies beneath Greenhouse Interior, align them to available floor space, and name them; no simulation-code change or per-bed clock/menu assignment is needed. The prefab binds to its parent GreenhouseInteractionUI once during Awake.

MarsClock tracks physical simulated seconds. Durations use **Earth days x 86,400 seconds**, not Martian sols (88,775.245 seconds). GrowingBed's Inspector multiplier and public EnvironmentMultiplier multiply elapsed simulated seconds. Set EnvironmentMultiplier to zero to stop only that bed. This is the extension point for future environmental/resource systems; those systems are not implemented.

Beds stay enabled outside the greenhouse, so clock events continue their growth. Pausing emits no advances; seeking emits a zero-length advance and adds no growth. Stages derive from progress in six equal intervals, clamp at the final sprite, and never loop. Harvest clears only the ready bed, rejects repeat attempts, and permits replanting.

## Existing-project repairs

The input scene and MarsClock/MarsSun/MarsLighting metadata contained unresolved Git conflict markers. Their working-tree contents are resolved, preserving the world hierarchy and batteries. EnergyController now references the existing DayNightSystem clock/lighting, rather than a second clock. Git's merge index was not staged or committed.

`Movement.cs` and its metadata were renamed to `AstronautMovement.cs` so Unity can associate the existing class with its script; movement code and GUID are unchanged. The player's missing sprite reference was changed to the supplied imported idle-south astronaut. The missing material referenced by the existing scene was restored as `Assets/Art/MarsTerrain.mat`, using the existing MarsTerrain shader and referenced GUID. Existing Sun, clock, lighting, energy, movement code, shader code, and resource artwork are otherwise unchanged.

There are pre-existing missing sprite GUIDs on unrelated outpost objects (for example the old building atlas reference). Those objects and missing exterior PNG files were not recreated or broadly replaced as part of this feature.

## Executed validation

From the repository root, with the .NET 11 SDK:

```
dotnet run --project Tools/GreenhouseChecks/GreenhouseChecks.csproj
dotnet run --project Tools/EnergyChecks/EnergyChecks.csproj
```

- 30 crop/clock assertions passed: all four durations, independent progress, six ordered stages, immature/double harvest rejection, replanting, multiplier behavior, pause, acceleration, seeking, large versus incremental advances, growth clamping, and a fifth independent bed. These compile the production CropProgress and MarsClock scripts with minimal engine stand-ins.
- 1,097 serialized scene/asset assertions passed: unique file IDs, resolving local references, a single authoritative clock, four beds with one menu instance each, 24 frame references, referenced greenhouse assets, clear arrival points, and grid-based routes for the existing 1x1 player collider to every interaction zone and the exit. The geometry checks use the actual serialized colliders; they do not run Unity physics.
- The existing 27 energy calculation assertions passed again.

Unity Editor is not installed/available in this environment. Unity compilation, actual Play Mode movement/collision/trigger behavior, camera rendering, sprite import, menu input, outlines, and lighting/gameplay regression tests were **not executed**. Verify these in Unity before treating the feature as Play Mode validated. Growth defaults are deliberately realistic; temporarily increase the existing clock's speed in the Inspector to inspect complete growth cycles quickly, then restore it. No crop state persists after stopping Play mode.
