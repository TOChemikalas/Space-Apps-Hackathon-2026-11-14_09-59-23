using System;
using System.Collections.Generic;

public enum CommunicationNarrative { Unassessed, MissionSupported, CommunicationTerminated }
public enum TransmissionTrigger { MissionTime, Event, Condition }
public enum NarrativeRequirement { Any, Unassessed, MissionSupported, CommunicationTerminated }
public enum LateSupplyPolicy { AwaitRevisedDate, NextSol }
public enum DeliveryStatus { Pending, Received, Suppressed }

[Serializable]
public sealed class TransmissionDefinition
{
    public string id, sender, subject, body, eventId, conditionId;
    public bool fromEarth = true;
    public TransmissionTrigger trigger;
    public int sol = 1;
    public double hour, delaySeconds;
    public NarrativeRequirement requirement;
}

[Serializable]
public sealed class TransmissionRecord
{
    public string id, sender, subject, body, conditionId;
    public bool fromEarth, read;
    public NarrativeRequirement requirement;
    public double queuedAt, dueAt, receivedAt;
    public DeliveryStatus status;
}

[Serializable]
public sealed class OutgoingTransmission
{
    public string body;
    public double sentAt;
    public TransmissionIntent intent;
}

[Serializable]
public sealed class CommunicationsState
{
    public int version = 1;
    public CommunicationNarrative narrative;
    public int supplyArrivalSol;
    public bool acknowledgmentScheduled, supplyDateNeedsRevision;
    public double clockSeconds;
    public List<TransmissionRecord> messages = new List<TransmissionRecord>();
    public List<OutgoingTransmission> outgoing = new List<OutgoingTransmission>();
    public List<string> conditions = new List<string>();
}

// Pure state machine. Its caller supplies the authoritative MarsClock timestamp, never real time.
public sealed class CommunicationsModel
{
    public const string AcknowledgmentId = "earth.emergency_ack";
    public const string AcknowledgmentBody = "Outpost-12, your transmission has been received.\n\nWe acknowledge your situation and confirm that emergency supplies have been authorized.\n\nEstimated supply arrival: Sol 250.\n\nMaintain essential operations and preserve your remaining resources.\n\nEarth Control will continue monitoring your mission.";
    readonly Dictionary<string, TransmissionDefinition> definitions = new Dictionary<string, TransmissionDefinition>(StringComparer.Ordinal);
    readonly Dictionary<string, TransmissionRecord> records = new Dictionary<string, TransmissionRecord>(StringComparer.Ordinal);
    readonly double secondsPerSol, responseDelay;
    readonly LateSupplyPolicy latePolicy;
    double now, nextDue = double.PositiveInfinity;
    public CommunicationsState State { get; }
    public bool Online { get; private set; }
    public bool CanTransmit => Online && State.narrative != CommunicationNarrative.CommunicationTerminated;
    public event Action Changed;

    public CommunicationsModel(CommunicationsState state, IEnumerable<TransmissionDefinition> authored,
        double secondsPerSol, double responseDelay = 60, LateSupplyPolicy latePolicy = LateSupplyPolicy.AwaitRevisedDate)
    {
        State = state ?? new CommunicationsState();
        if (State.version != 1 || State.messages == null || State.outgoing == null || State.conditions == null)
            throw new ArgumentException("Unsupported communications save.");
        this.secondsPerSol = secondsPerSol;
        this.responseDelay = Math.Max(0, responseDelay);
        this.latePolicy = latePolicy;
        now = State.clockSeconds;
        foreach (var record in State.messages) records.Add(record.id, record);
        if (authored != null) foreach (var definition in authored)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || definition.id == AcknowledgmentId)
                throw new ArgumentException("Message definitions need unique, nonreserved IDs.");
            definitions.Add(definition.id, definition);
        }
        RecalculateNextDue();
    }

    public bool Submit(string body, double timestamp)
    {
        if (!CanTransmit || string.IsNullOrWhiteSpace(body)) return false;
        now = State.clockSeconds = timestamp;
        var intent = TransmissionClassifier.Classify(body);
        State.outgoing.Add(new OutgoingTransmission { body = body.Trim(), sentAt = now, intent = intent });
        if (intent == TransmissionIntent.Hostile)
        {
            State.narrative = CommunicationNarrative.CommunicationTerminated;
            foreach (var record in State.messages)
                if (record.fromEarth && record.status == DeliveryStatus.Pending) record.status = DeliveryStatus.Suppressed;
        }
        else if (intent == TransmissionIntent.Committed && State.narrative == CommunicationNarrative.Unassessed)
        {
            State.narrative = CommunicationNarrative.MissionSupported;
            State.supplyArrivalSol = 250;
            ResolveLateSupplyDate();
            State.acknowledgmentScheduled = true;
            Add(new TransmissionDefinition { id = AcknowledgmentId, sender = "EARTH CONTROL", subject = "INCOMING TRANSMISSION",
                body = AcknowledgmentBody, delaySeconds = responseDelay }, now);
        }
        Process();
        Changed?.Invoke();
        return true;
    }

    public void Advance(double timestamp)
    {
        if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) || timestamp < 0) return;
        now = State.clockSeconds = timestamp;
        if (now < nextDue) return;
        if (Process()) Changed?.Invoke();
    }

    public void SetOnline(bool value, double timestamp)
    {
        now = State.clockSeconds = timestamp;
        if (Online == value) return;
        Online = value;
        Process();
        Changed?.Invoke();
    }

    public bool QueueMessage(string id, double timestamp)
    {
        if (string.IsNullOrWhiteSpace(id) || !definitions.TryGetValue(id, out var definition) || records.ContainsKey(id)) return false;
        now = State.clockSeconds = timestamp;
        Add(definition, definition.trigger == TransmissionTrigger.MissionTime ? Math.Max(now, TriggerTime(definition)) : now);
        Process();
        Changed?.Invoke();
        return true;
    }

    public void RaiseEvent(string eventId, double timestamp)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return;
        now = State.clockSeconds = timestamp;
        bool changed = false;
        foreach (var definition in definitions.Values)
            if (definition.trigger == TransmissionTrigger.Event && definition.eventId == eventId && !records.ContainsKey(definition.id))
            { Add(definition, now); changed = true; }
        if (Process() || changed) Changed?.Invoke();
    }

    public void SetCondition(string id, bool value, double timestamp)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (value == State.conditions.Contains(id)) return;
        if (value) State.conditions.Add(id); else State.conditions.Remove(id);
        now = State.clockSeconds = timestamp;
        Process();
        Changed?.Invoke();
    }

    public void MarkRead(string id)
    {
        if (!records.TryGetValue(id, out var record) || record.status != DeliveryStatus.Received || record.read) return;
        record.read = true;
        Changed?.Invoke();
    }

    bool Matches(NarrativeRequirement requirement) => requirement == NarrativeRequirement.Any ||
        (int)requirement == (int)State.narrative + 1;
    bool ConditionMet(string id) => string.IsNullOrEmpty(id) || State.conditions.Contains(id);
    double TriggerTime(TransmissionDefinition definition) => (Math.Max(1, definition.sol) - 1 + Math.Clamp(definition.hour, 0, 24) / 24) * secondsPerSol;

    void Add(TransmissionDefinition definition, double triggerAt)
    {
        var record = new TransmissionRecord { id = definition.id, sender = definition.sender, subject = definition.subject,
            body = definition.body, fromEarth = definition.fromEarth, requirement = definition.requirement,
            conditionId = definition.conditionId, queuedAt = triggerAt, dueAt = triggerAt + Math.Max(0, definition.delaySeconds) };
        records.Add(record.id, record);
        State.messages.Add(record);
    }

    bool Process()
    {
        bool changed = false;
        foreach (var definition in definitions.Values)
        {
            if (records.ContainsKey(definition.id)) continue;
            if (definition.trigger == TransmissionTrigger.MissionTime && TriggerTime(definition) <= now)
            { Add(definition, TriggerTime(definition)); changed = true; }
            else if (definition.trigger == TransmissionTrigger.Condition && ConditionMet(definition.conditionId) && !string.IsNullOrEmpty(definition.conditionId))
            { Add(definition, now); changed = true; }
        }
        State.messages.Sort((a, b) => { int order = a.dueAt.CompareTo(b.dueAt); return order != 0 ? order : string.CompareOrdinal(a.id, b.id); });
        foreach (var record in State.messages)
        {
            if (record.status != DeliveryStatus.Pending) continue;
            if (record.fromEarth && State.narrative == CommunicationNarrative.CommunicationTerminated)
            { record.status = DeliveryStatus.Suppressed; changed = true; continue; }
            if (!Online || record.dueAt > now || !Matches(record.requirement) || !ConditionMet(record.conditionId)) continue;
            if (record.id == AcknowledgmentId)
            {
                ResolveLateSupplyDate();
                if (State.supplyDateNeedsRevision) record.body = "Outpost-12, your transmission has been received.\n\nThe Sol 250 supply window has passed. Earth Control is reviewing a revised arrival date.\n\nMaintain essential operations. Earth Control will continue monitoring your mission.";
                else if (State.supplyArrivalSol != 250) record.body = AcknowledgmentBody.Replace("Sol 250", "Sol " + State.supplyArrivalSol);
            }
            record.status = DeliveryStatus.Received;
            record.receivedAt = now;
            changed = true;
        }
        RecalculateNextDue();
        return changed;
    }

    void ResolveLateSupplyDate()
    {
        int sol = 1 + (int)(now / secondsPerSol);
        if (sol <= 250 || State.supplyArrivalSol > sol) return;
        State.supplyDateNeedsRevision = latePolicy == LateSupplyPolicy.AwaitRevisedDate;
        State.supplyArrivalSol = State.supplyDateNeedsRevision ? 0 : sol + 1;
    }

    void RecalculateNextDue()
    {
        nextDue = double.PositiveInfinity;
        foreach (var definition in definitions.Values)
            if (definition.trigger == TransmissionTrigger.MissionTime && !records.ContainsKey(definition.id))
                nextDue = Math.Min(nextDue, TriggerTime(definition));
        foreach (var record in State.messages)
            if (record.status == DeliveryStatus.Pending && Online && Matches(record.requirement) && ConditionMet(record.conditionId))
                nextDue = Math.Min(nextDue, record.dueAt);
    }
}
