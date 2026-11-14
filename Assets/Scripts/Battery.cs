using System;
using UnityEngine;

// All power limits are measured at the electrical bus, before conversion losses.
public sealed class Battery : MonoBehaviour
{
    [Min(0)] public double capacityKWh = 40;
    [Min(0)] public double maxChargeKW = 20;
    [Min(0)] public double maxDischargeKW = 40;
    [Range(.01f, 1)] public double chargeEfficiency = .95;
    [Range(.01f, 1)] public double dischargeEfficiency = .95;
    [Range(0, 1)] public double initialCharge = .5;
    public double StoredKWh { get; private set; }
    public double BusPowerKW { get; private set; } // Positive when charging.
    public double ConversionLossKWh { get; private set; }
    public double Fraction => capacityKWh > 0 ? StoredKWh / capacityKWh : 0;

    void Awake() => StoredKWh = Math.Max(0, capacityKWh) * Math.Clamp(initialCharge, 0, 1);

    public double AvailableKW(bool charging, double hours)
    {
        if (hours <= 0) return 0;
        if (charging)
            return Math.Max(0, Math.Min(maxChargeKW,
                (capacityKWh - StoredKWh) / (Math.Clamp(chargeEfficiency, .01, 1) * hours)));
        return Math.Max(0, Math.Min(maxDischargeKW,
            StoredKWh * Math.Clamp(dischargeEfficiency, .01, 1) / hours));
    }

    internal double SpareDischargeKW(double hours) =>
        Math.Min(AvailableKW(false, hours), Math.Max(0, maxDischargeKW + Math.Min(0, BusPowerKW)));

    internal void DischargeAdditional(double power, double hours)
    {
        double delivered = Math.Min(power, SpareDischargeKW(hours));
        double stored = StoredKWh;
        StoredKWh = Math.Max(0, stored - delivered * hours / Math.Clamp(dischargeEfficiency, .01, 1));
        BusPowerKW -= delivered;
        ConversionLossKWh += stored - StoredKWh - delivered * hours;
    }

    public void Transfer(double busKW, double hours)
    {
        bool charging = busKW >= 0;
        double power = Math.Min(Math.Abs(busKW), AvailableKW(charging, hours));
        BusPowerKW = charging ? power : -power;
        double before = StoredKWh;
        StoredKWh += charging ? power * hours * Math.Clamp(chargeEfficiency, .01, 1)
            : -power * hours / Math.Clamp(dischargeEfficiency, .01, 1);
        StoredKWh = Math.Clamp(StoredKWh, 0, Math.Max(0, capacityKWh));
        ConversionLossKWh += charging ? power * hours - (StoredKWh - before)
            : before - StoredKWh - power * hours;
    }
}
