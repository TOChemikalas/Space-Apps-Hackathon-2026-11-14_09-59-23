# Electrical survival economy — implementation and validation

Oxygen location rule (updated): an occupied rover supplies oxygen while its own battery has charge, independently of colony power. Exit or depleted rover charge resumes suit-reserve use. Rover charge is updated before each survival step. The greenhouse supplies oxygen while the base has stored battery energy or positive generation, regardless of allocation cuts; its oxygen request has a .8 kW floor. This explicit greenhouse gameplay rule overrides the proportional-delivery oxygen rule described for the habitat below. It ends on leaving the greenhouse or losing all base power. Neither location grants habitat food/water or remote suit servicing. Automated checks cover charged/empty rover, base independence, exit, powered greenhouse with reduced allocation, and total power loss. All scripts compile against Unity assemblies; these changes have not been separately exercised in Play Mode.

UI revision: removed the extra red status-text block beneath the HUD and the duplicate green allocation shortcut in the electrical header. Existing HUD meters and the main allocation entry remain. Allocation bars now show red reference lines and minimum-point labels: water 6, oxygen 7, irrigation 3, heating 4, suit servicing 3, communications 3; cooling has no minimum at the default cold ambient. Thermal/oxygen thresholds follow the tuning settings. Below-line bars turn red and the selected row explains the consequence. These are warnings, not allocation locks; reference lines assume sufficient bus delivery. Water, irrigation and suit markers preserve normal performance, while oxygen/heating/communications markers preserve essential function.

## Baseline measured before balancing

The actual SampleScene configuration is one 150 m² solar array, latitude 39.8°, initial solar longitude 90°, irradiance 590 W/m², cell efficiency .32, atmospheric transmission .7, packing .9, electrical efficiency .9, undamaged dust factor 1. Solar output uses the existing sun elevation and seasonal calculation; it is not constant peak output. There are two enabled 40 kWh batteries, initially full, each limited to 20 kW charging and 40 kW discharging. Charge and discharge efficiencies are each .95 (round trip .9025). The old aggregate load was a continuous 5 kW; allocation points had no effect on it.

A sol is 88,775.245 seconds / 24.659790 Earth hours. Measured daylight for the initial sol is 15.531 Earth hours. The old system produces 148.851 kWh and requests 123.299 kWh in the first sol. Battery energy at successive 06:00 boundaries is 22.372, 22.369, 22.366 kWh: the large first-day fall from 80 kWh is the initial phase settling into its daily charge/discharge cycle, not repeated loss of 58 kWh/day. Tiny subsequent variation is seasonal. Surplus is curtailed when full; stored energy cannot simply be inferred by subtracting daily generation and demand.

Existing initial consumables are retained: habitat water 5.14 L and food 3.185 kg, plus astronaut water 5.14 L and food 3.185 kg. Oxygen is represented as an eight-Earth-hour consumption-equivalent reserve in the new model (the previous suit timer used eight Martian clock hours). No emergency inventory is added. Three existing beds have six pre-grown slots each, initially due at sols 2, 4 and 6. Each harvest unit provides the existing 0.10616667 kg, multiplied by surviving crop condition. The test harvests available starter crops once at each sol boundary in every scenario; these are not newly supplied rescue crops.

## Implementation

`EnergyController` integrates generation, requested demand, actual delivery, batteries, losses, and the shared `SurvivalEconomy` in five-second steps. The existing aggregate consumer is explicitly marked as replaced by the economy; unrelated consumers still count. Allocations cap power requests. All requested loads share the available bus proportionally when generation and battery discharge cannot meet demand. The model receives delivered power, not nominal allocation points. The same model instance supplies MissionSurvival, the HUD, greenhouse growth/yield and communications availability.

Battery depletion no longer directly kills the astronaut. Health decreases only with exhausted oxygen/water/food or unsafe habitat temperature. Finite injury rates integrate exposure; there is no scenario ID, protected player, scripted death deadline or rescue injection in production code. Returning power does not reset health, pipe damage or crop condition.

The rover is parked and disconnected in the reference scenarios. Its existing charger remains a separate, lower-priority battery draw; charging it would spend additional reserves and is not assumed in these budgets. Greenhouse irrigation represents a recirculating growing system with a normalized moisture buffer, not a newly invented tank of emergency drinking water. Thermal coefficients and physiological rates below are explicit simplified gameplay assumptions, not measured spacecraft performance or medical predictions.

## Operating rules and consequences

| System | Demand / delivered-power performance | Reserves, warnings and critical effects | Damage and recovery |
|---|---|---|---|
| Water | Six points cap the recycler at .5 kW. Recycling = configured .90 or upgraded .95 × delivered/.5 × (1−pipe damage), capped at full performance. | Consumption 2.57 L/sol; functioning recycling reduces net habitat draw to .257 L/sol at 90%. Habitat water below 1 L warns. After habitat water is exhausted, the astronaut's reserve pays the remaining loss. At zero astronaut water, health loses .1/hour. | Pipes accumulate damage below 0°C: freezing degree-hours/1000. Rewarming stops further damage but cannot repair cracks. More water is lost thereafter; existing reserves and restored recycling provide the recovery buffer. No pipe-repair tool is claimed. |
| Oxygen | Seven points request .8 kW; fourteen allow 1.6 kW for recovery. Production is 1.1 consumption-equivalent reserve-hours/hour at .8 kW. Neutral operation needs .7273 kW while inside. Outside there is no habitat supply. | Eight-hour reserve. Warn below two hours. Delivered production minus consumption changes the reserve continuously. Empty reserve causes health loss of 1/hour while empty. | Restore oxygen allocation and shed discretionary demand before injury becomes fatal. Temporary extra oxygen allocation replenishes reserves faster. Electricity does not resurrect the astronaut or erase injury. |
| Irrigation | Three points request .8 kW. Delivered/.8 multiplies growth. One crisis point gives one-third growth at full delivery. | Moisture changes by `(delivered/.8 − .35) × hours/24`; .28 kW maintains moisture. Warn below .3 moisture. Dry plants lose .01 condition/hour; below 5°C also lose cold degree-hours/1000. | Crop condition multiplies both growth and harvested food. Restoring irrigation refills moisture but does not reverse crop losses. Remaining live crops and existing food are the recovery resources. Zero crop condition is a permanent greenhouse loss in this prototype; no unimplemented repair/replant reset is assumed. |
| Heating | Five points cap at 1.5 kW; thermostat normally requests 1.2 kW to hold 20°C. Heat loss is .02 kW/K to −40°C; habitat heat capacity is 2 kWh/K. | `dT/dh = (heating−cooling−.02×(T+40))/2`. Warn below 12°C; health loses cold degree-hours below 5°C /100. | Below 0°C pipes freeze and crops are damaged. Additional available allocation can rewarm the habitat. A living freeze-damage case is tested through rewarming; damage remains. |
| Cooling | Thermostat requests .5 kW per degree above 24°C, limited to .3 kW per allocated point. At normal cold ambient it draws zero, rather than its maximum rating continuously. | Warn above 30°C; heat degree-hours above 35°C /100 reduce health. | Restore sufficient cooling for positive thermal input. Normal cold scenarios have no cooling burden or invented cooling damage. |
| Suit station | Three points allow 1 kW during the 08:00–16:00 service/diagnostic shift only. | Outside, suit condition wears by .002/hour; each lost fraction adds that fraction to oxygen consumption. Below 50% condition warns. Inside, powered service restores .02 condition/hour at 1 kW. | Postponing service saves energy but removes repair capacity. Returning to the habitat and restoring this existing station repairs wear. No automatic physical damage is assigned merely because the station was powered off. |
| Communications | Six points cap at .7 kW, continuous listening/link operation. Delivered power must be at least .35 kW for a usable aligned link. | Underpowered communications go offline; queued messages wait. | Restore allocation/power and dish alignment. Electronic messages are retained; switching off a radio does not invent physical hardware damage. |

Health is normalized 0–1, warnings escalate at 50%, death occurs at zero. Battery warning is below 20%. Health does not automatically regenerate. The HUD exposes temperature, health, water/food stores, pipe damage, suit/crop condition and warnings. Allocation detail shows actual delivered/requested kW. Crop ETA follows effective growth rather than ignoring power shortages. Pre-grown beds loaded later use accumulated effective growth, preventing late scene loading from bypassing irrigation shortages.

Core coefficients are editable in `MissionSurvival`'s Economy Tuning inspector fields. Other rates are named and documented in `SurvivalEconomy`. All scenarios use the same coefficients; only sunlight and player allocations differ.

## Comparison after three complete sols

All cases start at 06:00 with full batteries, inside the habitat, without driving/charging the rover. Undamaged panels and fixed sunlight multipliers isolate electrical decisions from the separate scheduled dust storm and its random faults. Six-sol safe cases also check for a repeating battery cycle and zero unexplained damage. Generation's small daily change comes from the existing seasonal model.

| Metric, end of third elapsed sol | A: normal | B: managed 45% solar reduction | C: unchanged allocations, same reduction |
|---|---:|---:|---:|
| Generated, third sol (kWh) | 148.84 | 81.86 | 81.86 |
| Requested, third sol (kWh) | 106.86 | 68.23 | 108.69 |
| Actually delivered, third sol (kWh) | 106.86 | 68.23 | 82.27 |
| Battery energy (kWh / 80) | 35.37 | 48.08 | 0 |
| Battery state of charge | 44.2% | 60.1% | 0% |
| Oxygen reserve (hours) | 8 | 8 | 1.87 |
| Habitat + astronaut water (L) | 4.369 + 5.140 | 4.369 + 5.140 | 3.789 + 5.140 |
| Habitat + astronaut food (kg) | 2.548 + 3.185 | 1.911 + 3.185 | 1.911 + 3.185 |
| Greenhouse condition | 100% | 100% | 100% |
| Growing-medium moisture | 100% | 94.9% | 92.2% |
| Temperature | 20°C | 20°C | 16.49°C |
| Pipe damage | 0% | 0% | 0% |
| Astronaut health | 100% | 100% | 100% |

B keeps water=6, oxygen=7, heating=5, irrigation=1; communications, cooling and suit servicing are set to zero using the existing allocation controls. Its cost is an unavailable Earth link, postponed suit servicing, slower food production and slowly declining moisture. C keeps the default 6/7/3/4/5/3/6 allocation. Restoring a cold habitat raises its requested heating load, so late demand is higher than the normal 106.86 kWh.

C first warns about its battery at 45.68 elapsed Earth hours. With continued neglect, oxygen exhaustion starts health loss at 93.72 hours and death occurs at 94.72 hours (3.84 elapsed sols). At death water and food still remain: the failure is caused by inadequate oxygen production under an electrical deficit, not a starvation deadline. The fourth-sol log is a **partial interval ending at death**; its 79.94 kWh generation and 97.39 kWh demand must not be described as a full-sol budget.

## Recovery and persistent damage checks

Random total solar-array faults are disabled by default in both the component and the scene because the game currently has no player repair interaction for them. Dust accumulation and weather-related degradation remain. This removes a chance-based unavoidable failure without protecting players from shortages caused by their allocations.

The recovery replay follows C for three sols, including an empty battery and only 1.87 hours of oxygen. Through existing allocation controls it switches to B's reductions and temporarily raises oxygen to fourteen points for one sol, then returns oxygen to seven. Under the same 45% solar reduction, by elapsed sol six it has 14.99 kWh stored, eight hours of oxygen, 20°C and 100% health. No stock, free power or repairs are injected. Early intervention is therefore possible even after the battery empties.

B can also restore full irrigation for a measured sol while remaining healthy; its battery falls to 40.92 kWh. That extra load is not indefinitely sustainable at reduced sunlight: return to the crisis setting or await environmental recovery. This is a costed recovery action, not a free permanent upgrade.

A separate cold exposure test turns off heating, stops at −1°C while alive, and uses available unassigned points to raise heating. Temperature recovers above 10°C and the astronaut survives, while at least the measured 0.13% pipe damage remains. No repair interaction is invented. This is a separate deliberate heating-neglect case; C dies of hypoxia before its habitat freezes.

## Verification and boundaries

Run `dotnet run --project Tools/SurvivalEconomyChecks/SurvivalEconomyChecks.csproj` from the repository root. It compiles production clock, sun, solar, battery, energy controller, allocation, crop-progress and economy code with small engine stand-ins. Scene fixture assertions catch changes to the inspected starting configuration. Each scenario interval checks generation = delivered load + conversion losses + curtailment + change in stored energy, within 0.000001 kWh. At most five simulated seconds remain pending at a clock boundary; all reported counters use integrated time. Detailed results are in `SurvivalEconomyChecks/Results.txt`.

`SurvivalEconomyUnityChecks.cs` runs the scene and actual MissionSurvival/energy bindings in a copied project using Unity 6000.5.10f1 and an isolated save profile. It checks A/B survival, C failure, actual HUD oxygen values, discretionary system loss, allocation-based irrigation recovery and recovery from an empty battery. See `SurvivalEconomyChecks/UnityResults.txt` for the final native run.

The communications calculation suite passed 94 assertions. Greenhouse checks passed 30 crop/clock, 3204 scene/asset and 17 doorway assertions. The separate legacy EnergyChecks executable was blocked by Windows Application Control, including on an approved retry outside the sandbox; it is **not reported as passing**. The new conservation/scenario suite and native Unity checks exercise the modified production energy path.

This is a validated finite-mission economy, not an indefinitely self-sufficient colony. Water recycling still loses water; food inventories and the three starter harvests are finite, and newly planted crops retain their existing long growth durations. No scenario claims infinite food or water, arbitrary new supplies, or a new maintenance tool. The controlled 45% reduction is not a validation of every scheduled-storm/random-fault combination. Physical pipe/greenhouse losses currently have no player repair interaction; avoiding them is part of survival, and sufficiently severe prior neglect can make survival impossible. Visual layout and mouse ergonomics require manual review beyond the automated scene checks.
