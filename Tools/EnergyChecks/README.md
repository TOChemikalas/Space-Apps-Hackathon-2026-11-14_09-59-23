# Mars energy system

Open `Assets/Scenes/SampleScene.unity` and enter Play mode. The scene includes a Mars Energy object and two battery objects at (-2, -2) and (2, -2). Battery bodies, lightning symbols, and world-space readouts are constructed at startup; no packages or imported art are required. Existing ground, astronaut, camera, and light objects are preserved.

Inspector configuration is already wired. Mars Energy contains the existing clock and lighting components, a horizontal SolarArray, a 5 kW ElectricalConsumer, and EnergyController. Array and battery defaults match the requested specifications. Solar generation uses MarsSun elevation, with the same starting season as MarsLighting. Add arrays, batteries, or consumers to the controller lists to expand the system. Each list should contain unique references.

The controller exposes generation, demand, unmet power, cumulative generated energy, demand energy, unmet energy, and curtailed energy as public read-only properties. Batteries expose stored energy, bus power (positive charging), and conversion losses. These are available to other scripts/debuggers; Unity's standard Inspector displays configuration fields only.

Power is measured at the electrical bus in kW. Storage is in kWh. Charging adds bus energy times charge efficiency; discharging removes delivered energy divided by discharge efficiency. All solar power supplies demand first. Only the surplus or deficit reaches batteries. Equal bus-power shares redistribute automatically when a battery reaches a capacity or power limit. Surplus with no storage space is explicitly counted as curtailment.

Integration uses five-simulated-second midpoint steps, independently of rendering and time acceleration. This is numerical integration of the existing Sun model, not a time-of-day curve. Up to five simulated seconds can remain pending between clock events. A seek discards that pending fraction and never transfers energy. Extremely large advances process every step synchronously and can stall a frame; ordinary accelerated play requires no allocations per energy step. Readouts change only at 1% / 0.1 kWh thresholds or state changes. Pausing freezes the last measured bus-power state along with storage.

## Validation

Run `dotnet run --project Tools/EnergyChecks/EnergyChecks.csproj` with the .NET 11 SDK. The standalone harness compiles the production clock, Sun, solar, battery, consumer, and controller scripts with minimal engine stand-ins. It adds no Unity dependency.

27 assertions passed: night/horizon production, peak output and elevation scaling, surplus-only charging, fair charging/discharging, conversion losses, capacity and power caps, redistribution, full-unit curtailment, depleted-unit unmet demand, pause, seek, accelerated versus incremental integration, full-day energy conservation, and a depleted two-battery system with 100 kW load and zero generation.

Unity Editor was not available in this environment. Unity compilation, Play mode, actual sprite/text rendering, and lighting/gameplay regression checks were not executed. No existing clock, Sun, lighting, movement, shader, material, or testing script was changed. Existing MarsSun assumptions (uniform seasons and fixed atmospheric transmission) remain in use.


Electrical-controls update: the harness now also compiles PowerAllocation and includes Water-to-Oxygen transfer, budget conservation, zero-allocation and empty-pool checks. The current build succeeds, but current execution is blocked by Windows Application Control, so those new assertions have not run. See [electrical controls](../ElectricalControls.md) for current bindings and Play Mode verification steps.
