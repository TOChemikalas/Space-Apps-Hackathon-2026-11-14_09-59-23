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
    public double GenerationKW { get; private set; }
    public double DemandKW { get; private set; }
    public double UnmetDemandKW { get; private set; }
    public double UnmetEnergyKWh { get; private set; }
    public double CurtailedEnergyKWh { get; private set; }
    public double GeneratedEnergyKWh { get; private set; }
    public double DemandEnergyKWh { get; private set; }

    // Fixed midpoint integration makes results independent of frame rate and clock speed.
    // At most five simulated seconds remain pending; seeks discard that remainder.
    const double StepSeconds = 5;
    double pending, cursor;

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
                if (consumer && consumer.isActiveAndEnabled)
                    DemandKW += Math.Max(0, consumer.demandKW);
            double hours = StepSeconds / 3600;
            GeneratedEnergyKWh += GenerationKW * hours;
            DemandEnergyKWh += DemandKW * hours;
            double residual = Distribute(GenerationKW - DemandKW, hours, batteries);
            UnmetDemandKW = Math.Max(0, -residual);
            UnmetEnergyKWh += UnmetDemandKW * hours;
            CurtailedEnergyKWh += Math.Max(0, residual) * hours;
            pending -= StepSeconds;
            cursor += StepSeconds;
        }
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
