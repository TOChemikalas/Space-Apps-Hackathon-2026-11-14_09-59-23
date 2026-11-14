# Electrical controls

Implemented directly in `Assets/Scenes/SampleScene.unity`; no Inspector references remain to assign.

## Player flow

- Approach the Operations computer in the living-quarters Operations room. The closest reachable electrical device gets a white outline. Press **E** to open its Generation/Demand screen.
- Approach either battery in the interior Power room to open that battery's own readings directly. The two existing outside battery objects also open their corresponding unit's menu. Interior and exterior views reference the same two Battery simulation components; no extra stored-energy state is created.
- The left-hand equipment buttons switch between Solar array, Battery bank, and Power distributor. Operations' Battery bank tab shows bank totals; entering via a battery keeps that specific unit selected.
- Choose **Allocate power** to open the allocation screen. Select a consumer to reveal its + / minus controls. Changes apply immediately; Back/X returns to electricity. Escape or the electrical menu's Back closes the modal and restores the previous movement-enabled state. The simulation continues while a menu is open.

## Artwork and live fields

The supplied artwork is used directly, with non-destructive sidebar/reading-panel slices and clean covers over baked example readings and bars:

- Day center/window: `Gemini_Generated_Image_lvghyclvghyclvgh.jpg`.
- Night center/window and Generation/Demand table: `Gemini_Generated_Image_1cx14x1cx14x1cx1.jpg`.
- Battery layout: `Gemini_Generated_Image_pirydzpirydzpiry.jpg`.
- Allocation layout: `Gemini_Generated_Image_mci5ggmci5ggmci5.jpg`.
- Baked example numbers and meters are covered by solid panels matching the existing dark UI theme; live labels and fills are drawn over them. The duplicate night JPG is not used as a field cover.

The existing MarsSun calculation defines day as positive solar elevation at the configured array latitude and season. MarsClock.Advanced updates the selected day/night artwork immediately while open, including seeks. The center follows the phase; side panels stay appropriate to the selected equipment. Allocation has one supplied layout, and the underlying electrical phase remains up to date for returning to it.

Readings refresh every 0.2 real seconds while open, without reopening. They read the existing power simulation, whose energy integration advances in five-simulated-second steps:

| Artwork field | Data source / units |
| --- | --- |
| Generation | EnergyController.GenerationKW, kW |
| Demand | EnergyController.DemandKW, kW |
| kW available | max(0, GenerationKW - DemandKW), surplus before storage dispatch |
| Solar Generation | Same simulated generation, kW |
| Efficiency | Area-weighted configured solar-cell efficiency (32% beginning-of-life by default) |
| Irradiance | Area-weighted configured mean top-of-atmosphere irradiance (590 W/m² by default), not a local ground sensor |
| Temperature | **Dash: no array temperature sensor/model exists** |
| Charge level | Actual stored energy / capacity, % |
| Capacity | Selected battery's configured capacity, kWh; bank view sums units |
| Time remaining | Estimated simulated hours/minutes at the current measured discharge power, including discharge efficiency; dash while idle/charging because no discharge-time forecast is defined |
| Stored energy | Battery.StoredKWh, kWh; bank view sums units |
| Charging / discharging | Positive/negative portions of Battery.BusPowerKW, kW |

Meters use live charge fraction and generation relative to the existing array model's peak. Solar's unimplemented baked Toggle arrays action is replaced with Generation table; Battery's unimplemented Configure cells action opens Power allocation. No fake array controls or cell-configuration system is introduced.

## Allocation ownership and remaining equipment connections

EnergyController owns a serialized PowerAllocation object. Other equipment reads the same values through `energy.Allocation.Get(ElectricalSystem.Water)` (or another enum value). Only this model mutates point counts; the UI stores no allocations.

The configurable default budget is **40 points**. Initial allocations match the supplied artwork: Water 6, Oxygen 7, Irrigation 3, Cooling 4, Heating 5, Suit Repair Station 3, Comms 6; **6 points remain unassigned**. Allocations plus the pool always equal the budget. Zero allocation blocks subtraction and an empty pool blocks every addition. Closing/reopening preserves values for the current simulation session. Play Mode restarts restore configured defaults; no disk-save system is added.

There is **no existing points-to-output or points-to-kW rule** for Water, Oxygen, Irrigation, Cooling, Heating, Suit Repair Station, or Comms. All seven still need their own equipment behavior connected to these settings when those rules exist. Their allocations remain distinct configuration values. Existing aggregate habitat demand remains 5 kW by default; generation, battery dispatch, and conversion losses are unchanged.

## Changed files

- `PowerAllocation.cs`: shared seven-consumer model and budget/limit rules.
- `EnergyController.cs`: serialized allocation ownership and configuration normalization; existing power integration is unchanged.
- `ElectricalInteractable.cs`: trigger membership, white outline, bounded proximity, and equipment/battery identity.
- `ElectricalControls.cs`: reuses GreenhouseCanvas helpers, the existing Player/Interact action, Canvas/EventSystem, movement lock, Escape/Back behavior, and the existing clock/power sources. One shared menu selects the actual target battery. Line-of-sight filtering ignores triggers and rejects intervening walls/furniture.
- `SampleScene.unity`: assigned artwork, UI/controller references, Operations and four battery interaction targets, source battery references, and allocation configuration. Existing object hitboxes were not replaced.
- Menu image importer metadata: full-resolution, uncompressed sprites; original JPG pixels remain unchanged.
- `Tools/EnergyChecks/`: added production allocation tests and kept the existing energy checks compiling.

## Executed validation / limits

- `dotnet build Tools/EnergyChecks/EnergyChecks.csproj --no-restore -p:UseAppHost=false`: succeeded, zero warnings/errors. This compiles production power/allocation calculation scripts with minimal engine stand-ins; it is not a Unity UI compilation.
- **94 focused serialized configuration checks passed**: unique scene IDs, local references, correct unit mappings for interior/outside battery views, Operations menu routing, four artwork references, and the seven initial allocations.
- `dotnet run --no-build --project Tools/EnergyChecks/EnergyChecks.csproj -p:UseAppHost=false` was attempted normally and outside the sandbox. Windows Application Control blocked the compiled assembly (0x800711C7). The new Water-to-Oxygen conservation and zero/pool-limit assertions are provided, but **were not executed**; no runtime assertions are claimed as passed.
- Unity Editor/Play Mode was unavailable. Actual highlight contacts, E/Back/Escape UI input, panel alignment, live artwork/readings, and allocation-button behavior still require Play Mode verification.

### Play Mode checklist

1. Approach and leave Operations and each battery: only the nearest reachable device highlights, and moving away clears the outline. Walls must block interaction.
2. E on Operations opens Generation/Demand; E on each battery opens that unit's readings, including 40 kWh capacity by default. Escape/Back restores movement without pausing MarsClock.
3. While open, temporarily accelerate the existing MarsClock and cross sunset/sunrise. The center artwork must switch and numbers/meters must change without reopening. Restore clock speed afterward.
4. Open allocation. Remove one Water point and add one Oxygen point. Pool returns to its prior value and sum(allocations)+pool stays 40. Close/reopen and confirm the counts remain.
5. Subtract Water to zero: minus disables. Spend the final unassigned point: plus disables for every selected system. Neither action should change counts beyond its limit.
6. Temperature is a dash, not the artwork's sample temperature; stationary/charging battery endurance is also a dash. Stored energy uses kWh and bus power uses kW.
