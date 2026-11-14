# Main building navigation

Open `Assets/Scenes/SampleScene.unity` and enter Play Mode. The existing Habitat at approximately world (-11.9, -7.9) is the main building. Approach its bottom doorway, near (-11.57, -11.12), to enter. Use the existing WASD/arrow movement. Walk down through the interior airlock doorway to return outside. No additional Inspector setup is required for the authored scene.

## Files and configuration

- `Assets/Scripts/BuildingEntrance.cs`: reusable paired doorway trigger. Configured references select the existing player Rigidbody2D, existing camera, opposite trigger, arrival point, interior root/center, interior size, zoom policy, and cooldown. Only the configured player activates it. An entry-state guard and 0.5-second cooldown prevent repeat transitions. Camera parent, local position, and zoom are saved/restored; the interior camera is fixed to its root center. It retains exterior zoom unless a wider view is necessary to fit the whole interior. No camera-follow loop or new camera is added.
- `Assets/Scripts/Greenhouse/GreenhouseEntrance.cs`: the existing component is now a small subclass that retains its greenhouse UI callback and serialized scene references. It reuses BuildingEntrance for transitions. Crop growth, clock, solar power, batteries, player movement, and camera component code were not changed.
- `Assets/Scenes/SampleScene.unity`: assigns the supplied habitat exterior, adds compound exterior collision around the shell/modules/machinery while leaving the front entrance open, and adds paired triggers/arrival points. The separate Main Building Interior root at (200, 0) contains the supplied living-quarters art and static wall/furniture/equipment colliders. The airlock, crew quarters, hygiene, life support, EVA prep, storage frontage, operations, and power area have tested geometric access. Painted door panels are traversable doorway openings; no door animation or new interaction controls are introduced.
- `Assets/Art/MarsColony/Living_Quarters/Interiorofliving.png.meta`: imports the original 1536x1024 PNG as a single centered sprite, point-filtered, uncompressed, with mipmaps disabled and a maximum size of 4096. At 40 pixels per world unit and uniform scale, it is not stretched. The player retains its existing scale and 1x1 collider. The source PNG itself is unchanged.
- `Assets/Art/MarsColony/habitat_day.png.meta`: explicitly centers the existing cropped sprite pivot so its artwork and measured collider coordinates agree. The existing sprite GUID and subasset ID are retained. The exterior scene transform is uniformly scaled to 2.
- `Tools/GreenhouseChecks/`: extended the existing standalone checks with production doorway tests and navigation checks for the new building. No new test packages are required.

For another building, add BuildingEntrance to each of its two trigger objects, pair them, assign the same existing player/camera, set separate safe arrival points outside the opposing trigger, mark the exterior trigger as entering, and assign the interior root and dimensions. Set the interior root's position independently of other interiors. The roots stay active so simulation continues during visits.

## Executed checks

```
dotnet run --project Tools/GreenhouseChecks/GreenhouseChecks.csproj -p:UseAppHost=false
dotnet run --project Tools/EnergyChecks/EnergyChecks.csproj -p:UseAppHost=false
```

- 30 production crop/clock assertions passed.
- 1,839 scene/asset assertions passed, including one authoritative clock/camera, resolving local references/assets, safe arrival points, an unobstructed exterior approach, and grid-based routes using the actual serialized collision rectangles expanded by the existing player's 1x1 footprint. Routes cover all seven room approaches and both existing greenhouse navigation and the new main-building exit.
- 17 production doorway assertions passed with minimal Unity stand-ins: player-only activation, invalid exit rejection, correct spawn positions, stopped velocity, camera centering/depth/fitting/restoration, repeated visits, entry/exit cooldowns, zoom retention, and the existing greenhouse UI hooks.
- 27 existing energy assertions passed.

These are standalone C# and geometry checks, **not Unity Play Mode or engine physics tests**. Unity Editor is unavailable in this environment. Actual sprite import/rendering, collision feel, Rigidbody interpolation during transitions, camera rendering, and greenhouse/UI/gameplay regression checks still require Play Mode verification. Scene and Inspector references are already configured.

The supplied interior PNG has an opaque checkerboard baked into its background (corner alpha 255). It is displayed as supplied; the image was not edited or replaced. Walls and objects remain solid; logical doorway openings are left free even where the artwork depicts a closed door.
