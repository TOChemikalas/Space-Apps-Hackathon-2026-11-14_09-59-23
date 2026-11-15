using System;
using UnityEngine;

public sealed class EnergyController : MonoBehaviour
{
    public MarsClock clock;
    public MarsLighting lighting;
    public SolarArray[] arrays = Array.Empty<SolarArray>();
    public Battery[] batteries = Array.Empty<Battery>();
    public ElectricalConsumer[] consumers = Array.Empty<ElectricalConsumer>();
    [SerializeField] PowerAllocation allocation = new PowerAllocation();
    public PowerAllocation Allocation => allocation;
    public SurvivalEconomy Survival { get; set; }
    public event Action<double> SurvivalStepCompleted;
    public event Action BatteryBankDepleted;
    public event Action<double> PowerStepCompleted;
    public double GenerationKW { get; private set; }
    public double DemandKW { get; private set; }
    public double UnmetDemandKW { get; private set; }
    public double UnmetEnergyKWh { get; private set; }
    public double CurtailedEnergyKWh { get; private set; }
    public double GeneratedEnergyKWh { get; private set; }
    public double DemandEnergyKWh { get; private set; }
    public double DeliveredEnergyKWh => DemandEnergyKWh - UnmetEnergyKWh;

    // Fixed midpoint integration makes results independent of frame rate and clock speed.
    // At most five simulated seconds remain pending; seeks discard that remainder.
    const double StepSeconds = 5;
    double pending, cursor;

    public bool TryGetBatteryCharge(out float fraction)
    {
        double stored = 0, capacity = 0;
        foreach (var battery in batteries)
        {
            if (!battery || battery.capacityKWh <= 0) continue;
            stored += battery.StoredKWh;
            capacity += battery.capacityKWh;
        }
        fraction = capacity > 0 ? Mathf.Clamp01((float)(stored / capacity)) : 0;
        return capacity > 0;
    }

    void Awake() => allocation.Normalize();
    void OnValidate() => allocation.Normalize();

    void OnEnable()
    {
        if (clock) clock.Advanced += OnAdvanced;
    }

    void OnDisable()
    {
        if (clock) clock.Advanced -= OnAdvanced;
    }

    void OnAdvanced(double from, double to)
    {
        if (to <= from)
        {
            pending = 0;
            cursor = to;
            return;
        }
        if (pending == 0) cursor = from;
        pending += to - from;
        while (pending >= StepSeconds)
        {
            GenerationKW = 0;
            float season = lighting ? lighting.startingLs : 90;
            foreach (var array in arrays)
                if (array && array.isActiveAndEnabled)
                    GenerationKW += array.PowerKW(cursor + StepSeconds * .5, season);
            DemandKW = 0;
            foreach (var consumer in consumers)
                if (consumer && consumer.isActiveAndEnabled && !(Survival != null && consumer.replacedBySurvivalEconomy))
                    DemandKW += Math.Max(0, consumer.demandKW);
            if (Survival != null) DemandKW += Survival.Request(allocation, cursor + StepSeconds * .5);
            double hours = StepSeconds / 3600;
            GeneratedEnergyKWh += GenerationKW * hours;
            DemandEnergyKWh += DemandKW * hours;
            double residual = Distribute(GenerationKW - DemandKW, hours, batteries);
            UnmetDemandKW = Math.Max(0, -residual);
            // Update rover charge before determining this step's oxygen availability.
            PowerStepCompleted?.Invoke(StepSeconds);
            if (Survival != null)
            {
                Survival.LowBattery = TryGetBatteryCharge(out float reserveFraction) && reserveFraction < .2f;
                Survival.BasePowered = GenerationKW > 0 || (TryGetBatteryCharge(out float storedFraction) && storedFraction > 0);
                Survival.Step(StepSeconds, DemandKW > 0 ? 1 - UnmetDemandKW / DemandKW : 0);
                SurvivalStepCompleted?.Invoke(StepSeconds);
            }
            if (TryGetBatteryCharge(out float charge) && charge <= 0) BatteryBankDepleted?.Invoke();
            UnmetEnergyKWh += UnmetDemandKW * hours;
            CurtailedEnergyKWh += Math.Max(0, residual) * hours;
            pending -= StepSeconds;
            cursor += StepSeconds;
            if (Survival != null && Survival.Health <= 0) { pending = 0; break; }
        }
    }

    internal double DrawBatteryEnergy(double requestedKWh, double hours)
    {
        if (requestedKWh <= 0 || hours <= 0 || UnmetDemandKW > 0) return 0;
        double available = 0;
        foreach (var battery in batteries)
            if (battery && battery.isActiveAndEnabled) available += battery.SpareDischargeKW(hours);
        if (available <= 0) return 0;

        double delivered = Math.Min(requestedKWh, available * hours);
        double share = delivered / (available * hours);
        foreach (var battery in batteries)
            if (battery && battery.isActiveAndEnabled)
                battery.DischargeAdditional(battery.SpareDischargeKW(hours) * share, hours);
        return delivered;
    }

    // Equal bus-power sharing, redistributing shares that exceed a unit's limits.
    // A capped water-fill needs no temporary arrays or per-step allocations.
    public static double Distribute(double netKW, double hours, Battery[] units)
    {
        if (hours <= 0) return netKW;
        bool charging = netKW >= 0;
        double remaining = Math.Abs(netKW), floor = 0;
        for (int pass = 0; pass <= units.Length; pass++)
        {
            int count = 0;
            double nextLimit = double.PositiveInfinity;
            foreach (var unit in units)
            {
                if (!unit || !unit.isActiveAndEnabled) continue;
                double limit = unit.AvailableKW(charging, hours);
                if (limit > floor)
                {
                    count++;
                    nextLimit = Math.Min(nextLimit, limit);
                }
            }
            if (count == 0 || remaining <= 0) break;
            double increment = Math.Min(remaining / count, nextLimit - floor);
            floor += increment;
            remaining = Math.Max(0, remaining - increment * count);
        }
        foreach (var unit in units)
            if (unit && unit.isActiveAndEnabled)
                unit.Transfer((charging ? 1 : -1) * Math.Min(floor, unit.AvailableKW(charging, hours)), hours);
        return charging ? remaining : -remaining;
    }
}
