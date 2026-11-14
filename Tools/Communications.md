# Communications implementation

## Integration with pulled gameplay changes — 2026-11-15

Restored the saved communications work onto `4d713c2` using a three-way merge. Kept the pulled HUD, survival, rover charging, water-recycler upgrade and scene objects. Only communications bindings were added to the updated scene. Outgoing messages now submit with Enter or numpad Enter; there is no Transmit button, and losing focus does not submit.

Fresh validation: 94 communications assertions, 26 dish assertions, and all existing energy, airlock and greenhouse checks passed. The merged scene passed 33 native Play Mode assertions in an isolated copy using the exact project editor, Unity 6000.5.10f1, with graphics enabled. A first attempt with `-nographics` crashed in rendering; the graphics-enabled run passed. Visual appearance and mouse ergonomics still require manual review. Earlier editor/version limitations below describe the original implementation run.

Operations → supplied system selection → Electricity or Communications. The existing interaction range, outline, E action, Canvas, movement lock, battery entry, electricity screens and satellite puzzle are retained. Back/Escape returns to the parent screen; Back from selection closes the modal. A directly opened battery still closes directly.

## Ownership and integration

- `ElectricalControls.cs`: existing interaction/modal owner; adds artwork hotspots and terminal navigation. New selection/terminal artwork uses its native 1672×941 proportions with uniform scaling.
- `TransmissionClassifier.cs`: local deterministic English intent rules, evaluated once on submission. Ambiguous, negated and unrelated phrases are neutral. This is intentionally conservative phrase recognition, not general language understanding.
- `CommunicationsModel.cs`: UI-independent narrative, outgoing history, unique-ID message records, read state, conditions and delivery scheduling. Uses timestamps supplied by the existing clock. Scheduling wakes at the next relevant simulated timestamp; reconnect, conditions, submissions and events also process eligibility. Equal due times are ordered by ordinal ID.
- `ScriptedTransmission.cs`: ScriptableObject authoring wrapper; runtime records are separate from definitions.
- `CommunicationsSession.cs`: scene binding, dish/clock subscriptions and scoped JSON persistence. Supplies event APIs without exposing UI dependencies.
- `CommunicationsTerminalUI.cs`: editable outgoing channel and read-only scrollable inbox; refreshed on state changes. No invented incoming story messages are installed.
- `MarsClock.cs`: adds only `RestoreSeconds`, an absolute seek notification that does not integrate skipped energy/growth. Existing day length, Update and acceleration are unchanged.
- `DishAlignmentModel.cs`: adds validated pose/lock restoration. Existing steering, signal and acquisition logic are unchanged.

Physical availability is `dish.IsLocked && hardwareOperational`. `SetHardwareOperational(bool)` is available to future damage/operational systems. Power allocation currently has no kW/availability rule for Comms, so no new point-to-power rule was invented. Story termination is independent of this physical link; it prevents outgoing Earth transmissions and permanently suppresses pending/future Earth messages while preserving received history. Other authored senders can remain available.

## Narrative and persistence

One distress call sets `MissionSupported`, records absolute Sol 250 and schedules `earth.emergency_ack`. The default response delay is 60 **simulated Mars seconds**, configurable on the scene's CommunicationsSession. Repeated distress does not duplicate the acknowledgment or supply commitment. Neutral messages only enter outgoing history. Clear abandonment/targeted abuse sets `CommunicationTerminated`.

The default late-date policy is **AwaitRevisedDate**: after Sol 250, no past arrival date is confirmed (`supplyArrivalSol = 0`, `supplyDateNeedsRevision = true`), and the acknowledgment explains that Earth is reviewing a revised date. The alternative Inspector policy **NextSol** explicitly confirms the next current Sol. The policy is checked at submission and again when a delayed/offline acknowledgment actually arrives. No supplies are spawned.

No general save/load service exists in this project. The small communications adapter saves `communications-v1.json` under `Application.persistentDataPath`, using a temporary file and atomic replacement. It saves narrative/inbox/outgoing mutations, menu close, application pause/quit and scene-object destruction. A static state snapshot carries data across scene recreation; the authoritative MarsClock and dish pose are restored from the checkpoint. It does **not** claim to save batteries, inventory or the rest of the colony.

`CaptureJson()` and `RestoreJson(json)` provide an integration boundary for a future game-wide save service. Loading validates the version/data/unique records before replacing live state. Unreadable disk saves are preserved and disk writes disabled for that session, with a warning. Save timestamps and physical dish state are included. Read flags, pending/received/suppressed IDs, delays and outgoing history round-trip with the narrative. Backward clock movement does not remove delivered IDs or reschedule them.

## Authoring future messages

1. In Unity choose **Assets → Create → Mars → Scripted transmission**.
2. Set `message.id` to a unique stable ID, sender, subject and body. Do not use reserved ID `earth.emergency_ack`. Set `fromEarth = false` for a sender unaffected by Earth Control termination.
3. Choose the trigger:
   - **MissionTime**: set absolute `sol` (starts at 1), `hour` (0–24), and optional `delaySeconds`. Due time is the configured absolute timestamp plus delay, even if the clock advances over it in one step.
   - **Event**: set `eventId`; gameplay calls `session.RaiseEvent("event_id")`. Alternatively `session.QueueMessage("message_id")` explicitly queues any authored definition by ID. Both are idempotent per message ID.
   - **Condition**: set a nonempty `conditionId`; gameplay calls `session.SetCondition("condition_id", true)` when satisfied. The delay starts when it first becomes eligible.
4. Optionally set `requirement` to a narrative state. `conditionId` can also gate a timed/event message. Delivery waits until both gates match. A false condition pauses eligibility, not the simulated delay.
5. Add the asset to **Electrical controls → CommunicationsSession → Messages** in SampleScene. The default list is deliberately empty.

Due messages remain pending while offline. Reconnection processes them in due-time/ID order and stamps the actual reception time. Read messages remain in the inbox. Do not change released message IDs to resend content: create a new ID for a new transmission.

## Assets and missing sources

Imported/referenced artwork (image contents unchanged):

- `Assets/Art/MarsColony/Living_Quarters/Menu/Air_Pressure)/Sci-Fi Habitat System Selection UI.png` — selection, new `.meta`.
- `Assets/Art/MarsColony/Living_Quarters/Menu/Air_Pressure)/Mars Habitat Transmission Channel.png` — existing full-image sprite import and GUID retained.
- `Assets/Art/MarsColony/Living_Quarters/Menu/Air_Pressure)/ChatGPT Image Nov 14, 2026, 10_47_00 PM.png` — supplied empty three-column inbox, new `.meta`.

Both supplied incoming-terminal PNGs show online artwork. A distinct offline variant was not found. The integrated terminal uses live orange offline labels/footer over the supplied three-column image. Assign a same-layout offline sprite to **ElectricalControls → Incoming Offline Artwork** when provided. The two-column `Habitat Communications Terminal.png` is not substituted because it lacks the message-list panel. Data rate and measured one-way latency have no simulation sources; baked examples are covered with dashes. These are not fabricated from the acknowledgment delay.

## Checks

Run `dotnet run --project Tools/CommunicationsChecks/CommunicationsChecks.csproj`: 94 assertions pass against the production classifier, model, clock and dish. Covers the task's distress/hostile/neutral examples, negation, punctuation/case/whitespace, practical spelling variants, repetition, empty/offline sends, absolute milestone, both late policies, late reconnect, event/condition/narrative gates, deterministic queues, other senders, read/history retention, isolated save-data round trips, suppression, acceleration, pause and rewind. An explicit manual queue request cannot bypass a future authored mission date.

Run `dotnet run --project Tools/SatelliteChecks/SatelliteChecks.csproj`: 26 existing dish regression assertions pass.

`Tools/CommunicationsUnityChecks.cs` contains the native Unity Play Mode harness. Copy it into `Assets/Editor` of an isolated project copy and invoke `CommunicationsUnityChecks.Run` using Unity batch mode without `-quit`. It uses a unique company/product save directory, never the normal gameplay save. It checks actual scene bindings, outline/range and E input, artwork navigation, existing alignment entry/Back, transmission availability, live inbox refresh, full body/read state, Unity JsonUtility round trips, narrative/hardware separation, battery entry and modal movement restoration. Headless checks cannot establish final visual appearance or mouse ergonomics.

The installed editor is 6000.6.9f1; the original project is 6000.5.10f1. **30 native Play Mode assertions passed** in the copied project, including scene recreation, actual scoped disk saving and disk JSON reload. Results are recorded in `Tools/CommunicationsChecks/UnityResults.txt`. The temporary Unity project was removed after testing. Native execution with the project's exact 6000.5.10f1 editor and final rendered appearance/mouse ergonomics were not tested. The newer editor emitted unrelated Search-index startup exceptions; these did not prevent the final feature checks from passing. The original project, package manifest and image pixels were not upgraded or rewritten.

Testing corrections: the harness now loads the interior on demand and pumps the player input buffer instead of the headless Editor buffer. A temporary harness call to a nonpublic Input System overload was corrected. Classification was tightened for unrelated quitting/negated contact requests. An additional delivery-rule check exposed that manually requesting a timed message could bypass its date; the scheduler now preserves the authored due time, and all affected model checks pass.

## Exact gameplay files

Modified: `Assets/Scripts/ElectricalControls.cs`, `Assets/Scripts/MarsClock.cs`, `Assets/Scripts/DishAlignmentModel.cs`, `Assets/Scenes/SampleScene.unity` (only component/reference additions).

Added: `Assets/Scripts/TransmissionClassifier.cs`, `CommunicationsModel.cs`, `CommunicationsSession.cs`, `CommunicationsTerminalUI.cs`, `ScriptedTransmission.cs`, and their five `.meta` files; the two artwork `.meta` files above. No image pixels, habitat layouts, prefabs, electricity calculations, movement, airlock or pressure scripts were changed.

Checks/documentation: `Tools/CommunicationsChecks/CommunicationsChecks.csproj`, `Tools/CommunicationsChecks/Program.cs`, `Tools/CommunicationsChecks/UnityResults.txt`, `Tools/CommunicationsChecks/.gitignore` (keeps the test project visible and build artifacts excluded), `Tools/CommunicationsUnityChecks.cs`, this document and the corrected navigation notes in `Tools/ElectricalControls.md`.
