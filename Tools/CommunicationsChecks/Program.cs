using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) {} }
    public static class Time { public static float unscaledDeltaTime; }
}

static class Checks
{
    static int passed;
    const double Sol = MarsClock.SecondsPerSol;
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        passed++;
    }
    static CommunicationsModel Model(TransmissionDefinition[] definitions = null, double delay = 60,
        CommunicationsState state = null, LateSupplyPolicy policy = LateSupplyPolicy.AwaitRevisedDate)
        => new CommunicationsModel(state, definitions, Sol, delay, policy);
    static TransmissionDefinition Definition(string id, int sol = 2, bool earth = true) => new TransmissionDefinition
    { id = id, sender = earth ? "EARTH CONTROL" : "Local", subject = id, body = "Test fixture only", sol = sol, fromEarth = earth };
    static int Received(CommunicationsModel model) => model.State.messages.Count(m => m.status == DeliveryStatus.Received);
    static void Main()
    {
        foreach (var message in new[] { "SOS", "Mayday, we need assistance.", "Our oxygen supply is almost empty.",
            "Our life support is failing.", "We need emergency supplies to survive.",
            "We're in serious trouble, but we're continuing the mission.", "Our supplies are critically low. We need help.",
            "Earth Control, our oxygen system is failing.", "We cannot survive much longer without resupply.",
            "Our power systems are failing and we need assistance.", "We are running out of food, please send supplies.",
            "Mission report: several systems have failed, but we are continuing operations.",
            "We are stranded, and we need help to complete the mission.", "Our habitat has suffered serious damage. We require support.",
            "I have one day of oxygen left.", "This equipment is terrible and we desperately need help.",
            "We need help fixing this stupid machine.", "Our situation is terrible. This equipment is useless. We desperately need help.",
            " \t SOS!!! \n", "OUR OXIGEN SUPPLY IS LOW", "Our fucking power system is failing." })
            Check(TransmissionClassifier.Classify(message) == TransmissionIntent.Committed, "Distress: " + message);
        foreach (var message in new[] { "I abandon the mission.", "I refuse to continue.",
            "Earth Control, I quit and will not cooperate.", "Earth Control, fuck you, you bastards.",
            "I am abandoning this mission.", "I refuse to continue this mission.", "I quit. I do not care what happens to the mission.",
            "Earth Control, stop contacting me. I refuse to cooperate.", "I am not following orders anymore. The mission is over." })
            Check(TransmissionClassifier.Classify(message) == TransmissionIntent.Hostile, "Hostile: " + message);
        foreach (var message in new[] { "Hello Earth.", "Testing testing.", "Banana spaceship.", "123456789", "Everything is fine.",
            "@!qxxr", "help", "", "   ", "What is your favorite food?", "I am not abandoning the mission.",
            "I will not abandon the mission.", "I refuse to abandon the mission.", "Do not cancel our emergency request.",
            "Earth Control, do not stop contacting me.", "If I quit would Earth listen?", "I quit but I am not abandoning the mission.",
            "Our oxygen supply is not failing.", "I do not need emergency supplies.", "I quit smoking.",
            "I refuse to continue playing chess.", "Earth Control, never stop contacting me." })
            Check(TransmissionClassifier.Classify(message) == TransmissionIntent.Neutral, "Neutral/negation: " + message);

        var model = Model();
        Check(!model.Submit("SOS", 0), "Offline submission rejected");
        model.SetOnline(true, 0);
        Check(!model.Submit(" \t", 0) && model.State.outgoing.Count == 0, "Empty not recorded");
        Check(model.Submit("Hello", 0) && model.State.narrative == CommunicationNarrative.Unassessed && model.State.messages.Count == 0, "Neutral has no consequence");
        Check(model.Submit("SOS", 0) && model.State.narrative == CommunicationNarrative.MissionSupported, "Distress establishes support");
        Check(model.State.supplyArrivalSol == 250 && model.State.acknowledgmentScheduled, "Absolute Sol 250 milestone");
        model.Submit("SOS", 1);
        Check(model.State.messages.Count == 1, "No duplicate acknowledgment or supply commitment");
        model.Advance(59);
        Check(Received(model) == 0, "Delay not early");
        model.Advance(60);
        Check(Received(model) == 1 && model.State.messages[0].body == CommunicationsModel.AcknowledgmentBody, "Exact acknowledgment through inbox");
        model.MarkRead(CommunicationsModel.AcknowledgmentId);
        Check(model.State.messages[0].read, "Read state");
        model.SetOnline(false, 60);
        Check(Received(model) == 1, "History visible offline");
        model.SetOnline(true, 60);
        model.Submit("I abandon the mission", 61);
        Check(model.State.narrative == CommunicationNarrative.CommunicationTerminated, "Permanent termination");
        Check(model.Online && !model.CanTransmit && !model.Submit("SOS", 62), "Hardware independent, distress cannot undo termination");
        model.SetOnline(false, 62); model.SetOnline(true, 63);
        Check(!model.CanTransmit && Received(model) == 1, "Realignment preserves termination and history");

        var definitions = new[] { Definition("b"), Definition("a"), Definition("later", 5) };
        model = Model(definitions);
        model.Advance(Sol - 1);
        Check(model.State.messages.Count == 0, "Before scheduled Sol");
        model.Advance(Sol);
        Check(model.State.messages.Count == 2 && Received(model) == 0, "Offline timed messages queued");
        model.SetOnline(true, Sol + 10);
        Check(Received(model) == 2 && model.State.messages[0].id == "a" && model.State.messages[1].id == "b", "Reconnect deterministic ordering");
        model.Advance(10 * Sol);
        Check(Received(model) == 3, "Large accelerated advance skips no scheduled messages");
        model.Advance(0); model.Advance(10 * Sol);
        Check(Received(model) == 3, "Rewind cannot duplicate messages");
        model = Model(new[] { Definition("manual_timed", 3) }); model.SetOnline(true, 0);
        Check(model.QueueMessage("manual_timed", 0) && !model.QueueMessage(null, 0), "Manual queue accepts stable ID and rejects empty ID");
        model.Advance(Sol);
        Check(Received(model) == 0, "Manual request cannot bypass authored mission date");
        model.Advance(2 * Sol);
        Check(Received(model) == 1, "Manually queued timed message respects absolute due time");

        var eventMessage = Definition("event"); eventMessage.trigger = TransmissionTrigger.Event; eventMessage.eventId = "alarm"; eventMessage.delaySeconds = 20;
        var conditional = Definition("conditional"); conditional.trigger = TransmissionTrigger.Condition; conditional.conditionId = "repair"; conditional.requirement = NarrativeRequirement.MissionSupported;
        model = Model(new[] { eventMessage, conditional });
        model.SetOnline(true, 0); model.RaiseEvent("alarm", 10);
        Check(model.State.messages.Count == 1 && Received(model) == 0, "Event queued with delay");
        model.RaiseEvent("alarm", 10); model.Advance(29);
        Check(model.State.messages.Count == 1 && Received(model) == 0, "Event idempotent and not early");
        model.Advance(30);
        Check(Received(model) == 1, "Event delivery at simulated delay");
        model.SetCondition("repair", true, 30);
        Check(model.State.messages.Count == 2 && Received(model) == 1, "Narrative requirement blocks conditional delivery");
        model.Submit("SOS", 30);
        Check(Received(model) == 2, "Supported state unlocks conditional message");
        model.Submit("I quit", 31);
        Check(model.State.messages.Single(m => m.id == CommunicationsModel.AcknowledgmentId).status == DeliveryStatus.Suppressed, "Termination cancels pending acknowledgment");
        model.Advance(10000);
        Check(Received(model) == 2 && !model.QueueMessage("event", 10000), "Suppression and delivered IDs cannot be requeued");

        var otherSender = Definition("other", 3, false);
        model = Model(new[] { Definition("future", 2), otherSender }); model.SetOnline(true, 0); model.Submit("I quit", 0); model.Advance(3 * Sol);
        Check(model.State.messages.Single(m => m.id == "future").status == DeliveryStatus.Suppressed && Received(model) == 1, "Future Earth suppressed; other senders supported");

        model = Model(delay: 10); model.SetOnline(true, 250 * Sol); model.Submit("SOS", 250 * Sol);
        Check(model.State.supplyArrivalSol == 0 && model.State.supplyDateNeedsRevision, "Late distress has explicit no-past-date policy");
        model.Advance(250 * Sol + 10);
        Check(model.State.messages[0].body.Contains("window has passed"), "Late acknowledgment explains revised date");
        model = Model(delay: 0, policy: LateSupplyPolicy.NextSol); model.SetOnline(true, 260 * Sol); model.Submit("SOS", 260 * Sol);
        Check(model.State.supplyArrivalSol == 262 && model.State.messages[0].body.Contains("Sol 262"), "Configurable next-Sol fallback");
        model = Model(delay: 10); model.SetOnline(true, 249 * Sol); model.Submit("SOS", 249 * Sol); model.SetOnline(false, 249 * Sol); model.Advance(251 * Sol); model.SetOnline(true, 251 * Sol);
        Check(model.State.supplyDateNeedsRevision && Received(model) == 1, "Offline acknowledgment delayed beyond Sol 250 uses fallback");

        var options = new JsonSerializerOptions { IncludeFields = true };
        model.MarkRead(CommunicationsModel.AcknowledgmentId); model.Submit("I quit", 251 * Sol);
        var restored = JsonSerializer.Deserialize<CommunicationsState>(JsonSerializer.Serialize(model.State, options), options);
        model = Model(state: restored); model.SetOnline(true, restored.clockSeconds); model.Advance(300 * Sol);
        Check(model.State.narrative == CommunicationNarrative.CommunicationTerminated && Received(model) == 1 && model.State.messages[0].read,
            "Isolated save roundtrip preserves narrative, inbox, read state and IDs");
        Check(model.State.outgoing.Count == 2 && model.State.acknowledgmentScheduled, "Outgoing and acknowledgment persisted");
        model = Model(definitions); model.Advance(Sol);
        restored = JsonSerializer.Deserialize<CommunicationsState>(JsonSerializer.Serialize(model.State, options), options);
        model = Model(definitions, state: restored); model.SetOnline(true, Sol); model.Advance(Sol);
        Check(Received(model) == 2, "Pending IDs survive save/load without duplicates");

        var clock = new MarsClock();
        model = Model(delay: 100); model.SetOnline(true, 0); model.Submit("SOS", 0);
        clock.Advanced += (_, to) => model.Advance(to);
        clock.paused = true; UnityEngine.Time.unscaledDeltaTime = 1;
        var update = typeof(MarsClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        update.Invoke(clock, null);
        Check(clock.Seconds == 0 && Received(model) == 0, "Paused production clock stops timers");
        clock.paused = false; clock.speed = 10; update.Invoke(clock, null);
        Check(Received(model) == 1 && clock.Seconds > 100, "Production accelerated clock delivers acknowledgment");
        clock.RestoreSeconds(0); clock.Advance(10000);
        Check(Received(model) == 1, "Absolute clock restoration does not duplicate inbox");
        Check(Math.Abs(clock.ScaleTime(300) - Sol * 10) < 1e-6, "Existing clock scale preserved");
        var dish = new DishAlignmentModel(); dish.Normalize();
        dish.Restore(132.6, 37.4, 132.6, 37.4, true);
        Check(dish.IsLocked && dish.CanAcquire, "Physical lock restoration");
        dish.Restore(0, 0, 132.6, 37.4, true);
        Check(!dish.IsLocked, "Invalid saved pose cannot claim online");
        var discovery = new[] { new TransmissionDefinition { id = "survey.drone_crash", fromEarth = false,
            sender = "SURFACE SURVEY", subject = "Drone crash site located",
            body = "A drone was located at (3000, 0) metres relative to the main base.", sol = 1 } };
        model = Model(discovery); model.Advance(100);
        Check(Received(model) == 0, "Discovery waits for an available communications link");
        model.SetOnline(true, 100);
        Check(Received(model) == 1 && !model.State.messages[0].read, "Discovery arrives unread for notification");
        model.MarkRead("survey.drone_crash");
        Check(model.State.messages[0].read, "Opening discovery activates its persisted read marker");
        restored = JsonSerializer.Deserialize<CommunicationsState>(JsonSerializer.Serialize(model.State, options), options);
        model = Model(discovery, state: restored); model.SetOnline(true, 200); model.Advance(300);
        Check(Received(model) == 1 && model.State.messages[0].read, "Discovery tracking survives reload without duplicate notifications");
        Console.WriteLine($"PASS: {passed} production communications, classifier, clock and dish assertions. No gameplay saves accessed.");
    }
}
