using System;
using System.Reflection;

static class Checks
{
    static int passed;
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    static void Equal(double actual, double expected, string name, double tolerance = 1e-7)
    {
        if (Math.Abs(actual - expected) > tolerance) throw new Exception($"{name}: {actual} != {expected}");
        passed++;
    }
    static Battery Unit(double fraction = .5)
    {
        var unit = new Battery { initialCharge = fraction };
        Call(unit, "Awake");
        return unit;
    }
    static EnergyController System(out MarsClock clock, double demand = 5)
    {
        clock = new MarsClock();
        Call(clock, "Awake");
        var system = new EnergyController {
            clock = clock, arrays = new[] { new SolarArray() },
            batteries = new[] { Unit(), Unit() }, consumers = new[] {new ElectricalConsumer { demandKW = demand }}
        };
        Call(system, "OnEnable");
        return system;
    }
    static void Main()
    {
        var solar = new SolarArray();
        Equal(solar.PowerAtElevation(-30), 0, "Night");
        Equal(solar.PowerAtElevation(0), 0, "Horizon");
        Equal(solar.PowerAtElevation(90), 10.70496, "Peak", 1e-5);
        Equal(solar.PowerAtElevation(30) * 2, solar.PowerAtElevation(90), "Elevation scaling");
        var units = new[] { Unit(), Unit() };
        Equal(EnergyController.Distribute(10 - 5, 1, units), 0, "Surplus served");
        Equal(units[0].StoredKWh, 22.375, "Only surplus charges");
        Equal(units[1].StoredKWh, units[0].StoredKWh, "Fair charge");
        EnergyController.Distribute(-5, 1, units);
        Equal(units[0].StoredKWh, 22.375 - 2.5 / .95, "Discharge losses");
        Equal(units[0].StoredKWh, units[1].StoredKWh, "Fair discharge");
        units = new[] { Unit(0), Unit(0) };
        Equal(EnergyController.Distribute(-8, 1, units), -8, "Depleted load exceeds generation");
        Equal(units[0].StoredKWh + units[1].StoredKWh, 0, "No negative energy");
        units = new[] { Unit(.99), Unit(0) };
        Equal(EnergyController.Distribute(30, .1, units), 30 - (0.4 / .095 + 20), "Redistribute capped charge");
        Equal(units[0].StoredKWh, 40, "Full capacity");
        Equal(units[1].BusPowerKW, 20, "Charge power limit");
        units = new[] { Unit(1), Unit(1) };
        Equal(EnergyController.Distribute(-100, .01, units), -20, "Discharge power limits");
        Equal(units[0].BusPowerKW, -40, "Unit discharge limit");
        units = new[] { Unit(1), Unit(1) };
        Equal(EnergyController.Distribute(5, 1, units), 5, "Full batteries curtail surplus");
        var slow = System(out var slowClock);
        var fast = System(out var fastClock);
        for (int i = 0; i < 86400; i++) slowClock.Advance(1);
        fastClock.Advance(86400);
        Equal(slow.batteries[0].StoredKWh, fast.batteries[0].StoredKWh, "Accelerated storage");
        Equal(slow.GeneratedEnergyKWh, fast.GeneratedEnergyKWh, "Accelerated generation");
        Equal(slow.UnmetEnergyKWh, fast.UnmetEnergyKWh, "Accelerated unmet load");
        double stored = fast.batteries[0].StoredKWh;
        fastClock.SeekHour(12);
        Equal(fast.batteries[0].StoredKWh, stored, "Seek creates no energy");
        fastClock.paused = true;
        UnityEngine.Time.unscaledDeltaTime = 10;
        Call(fastClock, "Update");
        Equal(fast.batteries[0].StoredKWh, stored, "Paused clock");
        double finalStored = slow.batteries[0].StoredKWh + slow.batteries[1].StoredKWh;
        Equal(40 + slow.GeneratedEnergyKWh,
            finalStored + slow.DemandEnergyKWh - slow.UnmetEnergyKWh + slow.CurtailedEnergyKWh
            + slow.batteries[0].ConversionLossKWh + slow.batteries[1].ConversionLossKWh,
            "Full-day conservation including conversion losses");
        Equal(slow.DemandEnergyKWh, 120, "24-hour load energy");
        var dark = System(out var darkClock, 100);
        darkClock.SeekHour(0);
        darkClock.Advance(3600);
        Equal(dark.batteries[0].StoredKWh + dark.batteries[1].StoredKWh, 0, "System depletion");
        Equal(dark.UnmetEnergyKWh, 62, "Depletion energy balance");
        Equal(dark.UnmetDemandKW, 100, "Depleted system reports unmet power");
        Console.WriteLine($"PASS: {passed} assertions against production calculation scripts.");
    }
}
