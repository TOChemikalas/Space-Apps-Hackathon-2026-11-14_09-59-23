# PROJECT SOL: Mars Exploration

### Explore Mars. Manage an outpost. Learn by doing.

PROJECT SOL is a browser-oriented, 2D educational survival game for children aged 7–12, created for the **2026 NASA Space Apps Challenge — Build a Junior Astronaut Mission Trainer**.

Play as a solo astronaut at a pre-established research outpost on Mars. Explore the surface, operate equipment, grow food, manage limited power and supplies, communicate with Earth, and investigate scientific evidence. The challenge is not simply to stay alive: it is to understand how a decision affecting one system can endanger the rest of the mission.

The game combines a fictional mission with published NASA measurements and mission research. Its purpose is to make space science, resource planning, and engineering trade-offs understandable through play.

## Gameplay

- **Explore an outpost:** Move around the Martian surface and enter the habitat, greenhouse, and life-support areas. Use equipment and interact with objects in the world.
- **Manage survival systems:** Monitor oxygen, water, food, and stored electrical energy as time passes. Critical shortages can end the mission.
- **Balance power:** Solar arrays generate electricity; batteries support loads when sunlight is unavailable. Allocations and equipment operation create trade-offs between essential services and optional activities.
- **Respond to dust storms:** Dust reduces available sunlight and can soil or impair solar panels. Cleaning, repair, and careful power use help the station recover.
- **Grow food:** Plant, tend, and harvest greenhouse crops. Growth and resource consumption continue as the Martian clock advances, including during sleep.
- **Drive a rover:** Travel using a battery-powered vehicle. Charging it draws energy from the outpost.
- **Communicate with Earth:** Align the communications dish, send messages, and experience delayed responses and interrupted links.
- **Investigate a lost rover:** The research mission explores evidence associated with an earlier NASA rover. The local recovery scenario is fictional; historical NASA observations are presented as sourced evidence, not as events that literally happened at the player's outpost.

PROJECT SOL is a **solo scientific mission**, not a colony-construction simulator. The station has already been deployed when the astronaut arrives.

## Scientific foundation

NASA data informs the game's Mars clock, solar conditions, dust effects, environmental references, resource consumption, water recycling, crop timing, and communications model. The outpost and its equipment are fictional, so published reference values are combined with explicitly simplified engineering and gameplay assumptions.

| Game system | NASA resource | How it is used |
| --- | --- | --- |
| Landing-site map | [Mars Trek — NASA/JPL Solar System Treks](https://trek.nasa.gov/mars/) | The landing-site map displays Mars Global Surveyor MOLA color hillshade imagery for Arcadia Planitia. |
| Mars time and sunlight | [NASA GISS Mars24 algorithms](https://www.giss.nasa.gov/tools/mars24/help/algorithm.html) | Sol-length conversion and Sun-position calculations inform the mission clock and lighting. |
| Planetary environment | [NASA Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) | Reference solar irradiance, orbital characteristics, temperature, and Earth–Mars distance scenario. |
| Dust and solar generation | [NASA — On Mars, Light Is Energy](https://science.nasa.gov/resource/on-mars-light-is-energy/) | Opportunity's 2018 storm observations inform an idealized dust-attenuation scenario. |
| Solar-panel soiling | [NASA/JPL — InSight's Power Generation](https://www.jpl.nasa.gov/images/pia25284-insights-power-generation-after-landing-and-spring-2022/) | Long-term power loss informs a compressed gameplay soiling model. |
| Oxygen and food consumption | [NASA — Designing Flexible, Reliable Deep Space Life Support](https://ntrs.nasa.gov/citations/20160005782) | Published crew-consumable rates inform resource demand. |
| Water systems | [NASA — ECLSS](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/) and [ISS water recovery milestone](https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/) | Water recovery and an improved recycler reference. |
| Oxygen production | [NASA — MOXIE experiment](https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/) | Experimental oxygen-production performance informs a hypothetical scaled-up system. |
| Food production | [NASA — Crop Production for Advanced Life Support](https://ntrs.nasa.gov/citations/20030032422) and [Radish harvest study](https://www.nasa.gov/missions/station/astronauts-harvest-radish-crop-on-international-space-station/) | Crop growth-time references. |

The fictional landing site is designated **AP-1, Arcadia Planitia**. The reference candidate location is **39.8° N, 202.1° E**, drawn from a [Lunar and Planetary Science Conference landing-site study](https://www.hou.usra.edu/meetings/lpsc2021/pdf/2420.pdf). The game uses an original NASA [MOLA map tile](https://trek.nasa.gov/tiles/Mars/EQ/Mars_MGS_MOLA_ClrShade_merge_global_463m/1.0.0/default/default028mm/7/35/15.jpg) displayed in its map interface.

**Scientific scope:** NASA measurements are reference inputs, not proof that the fictional outpost is a validated NASA design. The 180 m² solar array, equipment capacities, storm scheduling, repair activities, and other scenario parameters are game-design choices. The simulated communications delay uses a fixed Earth–Mars distance scenario rather than live planetary ephemerides. The game does not represent NASA endorsement.

## Controls

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` | Move the astronaut or drive the rover |
| `E` | Interact with nearby equipment, entrances, and vehicles |
| Mouse | Operate menus and on-screen interfaces |

Other contextual controls and instructions are shown in-game.

## Running the project

### Unity Editor

1. Clone or download this repository.
2. Install **Unity Hub** and the Unity Editor version specified in the project (developed with Unity 6; project-reported editor version **6000.5.10f1**).
3. In Unity Hub, choose **Add project from disk** and select the repository folder.
4. Open the starting scene configured in **Build Profiles → Scene List** and press **Play**.

The repository should contain `Assets/`, `Packages/`, and `ProjectSettings/` with their Unity `.meta` files. Unity will regenerate local cache directories on first opening.

### Browser build

PROJECT SOL is designed for Unity WebGL and desktop keyboard controls. If a Web build is provided with the submission, open it through the published project link or a standard HTTP(S) host. Opening a WebGL `index.html` directly from disk may not work because of browser restrictions.

## Technologies

Unity 6, C#, Unity 2D, Universal Render Pipeline (URP), Unity Input System, Unity UI/TextMeshPro, WebGL, Git, and GitHub. Development and verification were assisted by OpenAI Codex and other AI tools as disclosed below.

## Team, credits, and AI disclosure

PROJECT SOL was conceived and developed by the hackathon team. The mission design, engineering choices, integration, and final submission are the team's responsibility.

- **Original music:** The menu soundtrack was composed by a team member.
- **AI-assisted development:** OpenAI Codex assisted with C# implementation, debugging, automated checks, documentation, and build preparation.
- **AI-generated visual assets:** Portions of the intro, environments, buildings, characters, menus, and effects were generated or transformed using ChatGPT and Google Gemini. AI-generated visuals are identified in-game with visible `AI` labels.
- **NASA imagery and scientific resources:** Credited above with links to the original sources. NASA material is not presented as original team photography, and NASA identifiers should not be inferred as project endorsement.
- **Third-party tools:** Unity and supporting packages are credited by their respective developers. Applicable third-party licenses remain in effect.

## Limitations and future direction

PROJECT SOL is an educational prototype. Some numerical models compress longer real-world processes into a playable mission; historical missions and the fictional recovery/exploration story are distinct. The communications responses use local, scripted rules rather than real messages from Earth. A complete scientific or medical risk model is outside the prototype's scope.

Future development could expand the scientific investigation system into multiple missions with an evidence registry, allowing players to retain discoveries from earlier expeditions and apply them to new engineering problems. The immediate focus is a clear and replayable mission in which observation, planning, and consequences are connected.

---

**PROJECT SOL — Don't just learn about Mars. Experience the challenges of exploring it.**
