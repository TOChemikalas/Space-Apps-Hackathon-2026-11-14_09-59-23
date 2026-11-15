using System;
using System.Reflection;
using System.IO;

static class Program
{
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, null);
    static void Main()
    {
        string scene = File.ReadAllText("Assets/Scenes/SampleScene.unity");
        foreach (string expected in new[] { "area: 150", "irradiance: 590", "cellEfficiency: 0.32", "transmission: 0.7",
            "packing: 0.9", "electricalEfficiency: 0.9", "latitude: 39.8", "capacityKWh: 40", "initialCharge: 1",
            "initialHabitatWaterLiters: 5.14", "initialHabitatFoodKg: 3.185", "replacedBySurvivalEconomy: 1" })
            if (!scene.Contains(expected)) throw new Exception("Scene fixture changed; update scenario inputs: " + expected);
        var clock = new MarsClock { startHour = 6 };
        Call(clock, "Awake");
        double daylight = 0;
        for (double t = 0; t < MarsClock.SecondsPerSol; t += 5)
            if (MarsSun.At(clock.Seconds + t + 2.5).elevation > 0) daylight += 5;
        Console.WriteLine($"Daylight {daylight/3600:F3} Earth hours; sol {MarsClock.SecondsPerSol/3600:F6} Earth hours");
        var batteries = new[] { new Battery { initialCharge = 1 }, new Battery { initialCharge = 1 } };
        foreach (var b in batteries) Call(b, "Awake");
        var energy = new EnergyController { clock = clock, arrays = new[] { new SolarArray { area = 150 } }, batteries = batteries,
            consumers = new[] { new ElectricalConsumer { demandKW = 5 } } };
        Call(energy, "OnEnable");
        for (int sol = 1; sol <= 3; sol++)
        {
            double g = energy.GeneratedEnergyKWh, d = energy.DemandEnergyKWh;
            clock.Advance(MarsClock.SecondsPerSol);
            Console.WriteLine($"Baseline sol {sol}: generated {energy.GeneratedEnergyKWh-g:F3}, demand {energy.DemandEnergyKWh-d:F3} kWh; stored {batteries[0].StoredKWh+batteries[1].StoredKWh:F3}/80 kWh; unmet cumulative {energy.UnmetEnergyKWh:F3}");
        }
        foreach (string scenario in new[] { "A", "B", "C", "R" }) Run(scenario);
        var greenhouse = new SurvivalEconomy { InGreenhouse = true, BasePowered = true, OxygenHours = 4 };
        var normal = new PowerAllocation();
        for (int i = 0; i < 720; i++) { greenhouse.Request(normal, i * 5); greenhouse.Step(5, 1); }
        if (greenhouse.OxygenHours < 4 || greenhouse.WaterLiters != 5.14 || greenhouse.SuitCondition != 1)
            throw new Exception("Greenhouse must supply oxygen without habitat consumables or outdoor suit wear");
        double indoorOxygen = greenhouse.OxygenHours;
        greenhouse.InGreenhouse = false;
        greenhouse.Request(normal, 3600); greenhouse.Step(5, 1);
        if (greenhouse.OxygenHours >= indoorOxygen) throw new Exception("Leaving greenhouse must resume outdoor oxygen use");
        greenhouse.InGreenhouse = true;
        greenhouse.BasePowered = false;
        double beforeOutage = greenhouse.OxygenHours;
        greenhouse.Request(normal, 3605); greenhouse.Step(5, 0);
        if (greenhouse.OxygenHours >= beforeOutage) throw new Exception("Greenhouse oxygen still needs electricity");
        greenhouse.BasePowered = true;
        while (normal.Get(ElectricalSystem.Oxygen) > 0) normal.Remove(ElectricalSystem.Oxygen);
        double beforePoweredGreenhouse = greenhouse.OxygenHours;
        for (int i = 0; i < 720; i++) { greenhouse.Request(normal, i * 5); greenhouse.Step(5, .01); }
        if (greenhouse.OxygenHours < beforePoweredGreenhouse) throw new Exception("Powered greenhouse must protect oxygen even with reduced allocations");
        var roverOxygen = new SurvivalEconomy { InRover = true, RoverPowered = true, OxygenHours = 4 };
        for (int i = 0; i < 720; i++) { roverOxygen.Request(normal, i * 5); roverOxygen.Step(5, 0); }
        if (roverOxygen.OxygenHours < 4 || roverOxygen.SuitCondition != 1) throw new Exception("Charged rover must provide oxygen independently of the base");
        double beforeEmptyRover = roverOxygen.OxygenHours;
        roverOxygen.RoverPowered = false;
        roverOxygen.BasePowered = true;
        roverOxygen.Step(5, 1);
        if (roverOxygen.OxygenHours >= beforeEmptyRover) throw new Exception("Empty rover must not borrow base oxygen");
        roverOxygen.RoverPowered = true; roverOxygen.InRover = false;
        double beforeExit = roverOxygen.OxygenHours;
        roverOxygen.Step(5, 1);
        if (roverOxygen.OxygenHours >= beforeExit) throw new Exception("Leaving charged rover must resume suit consumption");
        Console.WriteLine("PASS: charged/empty rover, base independence, exit, greenhouse power and allocation independence.");
        Console.WriteLine("PASS: greenhouse oxygen, exit, power outage and consumable separation.");
        var cold = new SurvivalEconomy { InHabitat = true };
        var off = new PowerAllocation();
        while (off.Get(ElectricalSystem.Heating) > 0) off.Remove(ElectricalSystem.Heating);
        for (int i = 0; i < 40000 && cold.TemperatureC > -1; i++) { cold.Request(off, i * 5); cold.Step(5, 1); }
        double damage = cold.PipeDamage;
        if (damage <= 0 || cold.Health <= 0) throw new Exception("Need a living damaged recovery case");
        var repairHeat = new PowerAllocation();
        while (repairHeat.Unassigned > 0) repairHeat.Add(ElectricalSystem.Heating);
        for (int i = 0; i < 12000; i++) { cold.Request(repairHeat, i * 5); cold.Step(5, 1); }
        if (cold.PipeDamage < damage) throw new Exception("Power repaired physical damage");
        if (cold.Health <= 0 || cold.TemperatureC < 10) throw new Exception("Available heating could not recover survivor");
        Console.WriteLine($"Persistent freezing damage verified: {damage:P2}");
    }

    static void Run(string scenario)
    {
        var clock = new MarsClock { startHour = 6 }; Call(clock, "Awake");
        var batteries = new[] { new Battery { initialCharge = 1 }, new Battery { initialCharge = 1 } };
        foreach (var b in batteries) Call(b, "Awake");
        var state = new SurvivalEconomy { InHabitat = true };
        // Three existing pre-grown beds, six slots each; same harvest actions in every case.
        var crops = new[] { new CropProgress(), new CropProgress(), new CropProgress() };
        for (int i = 0; i < crops.Length; i++) crops[i].Plant((.75 + i * 2) * MarsClock.SecondsPerSol);
        var solar = new SolarArray { area = 150, dustFactor = scenario == "A" ? 1 : .55f };
        var energy = new EnergyController { clock = clock, arrays = new[] { solar }, batteries = batteries, Survival = state };
        if (scenario == "B")
        {
            foreach (var system in new[] { ElectricalSystem.SuitRepairStation, ElectricalSystem.Comms, ElectricalSystem.Cooling })
                while (energy.Allocation.Get(system) > 0) energy.Allocation.Remove(system);
            while (energy.Allocation.Get(ElectricalSystem.Irrigation) > 1) energy.Allocation.Remove(ElectricalSystem.Irrigation);
        }
        Call(energy, "OnEnable");
        double previousCharge = 0;
        for (int sol = 1; sol <= (scenario == "C" ? 15 : 6); sol++)
        {
            if (scenario == "R" && sol == 4) {
                foreach (var system in new[] { ElectricalSystem.Comms, ElectricalSystem.SuitRepairStation, ElectricalSystem.Cooling })
                    while (energy.Allocation.Get(system) > 0) energy.Allocation.Remove(system);
                while (energy.Allocation.Get(ElectricalSystem.Irrigation) > 1) energy.Allocation.Remove(ElectricalSystem.Irrigation);
                while (energy.Allocation.Get(ElectricalSystem.Oxygen) < 14) energy.Allocation.Add(ElectricalSystem.Oxygen);
            }
            if (scenario == "R" && sol == 5)
                while (energy.Allocation.Get(ElectricalSystem.Oxygen) > 7) energy.Allocation.Remove(ElectricalSystem.Oxygen);
            double g = energy.GeneratedEnergyKWh, d = energy.DemandEnergyKWh;
            double delivered = energy.DeliveredEnergyKWh, growth = state.GrowthIntegralSeconds;
            double beforeStored = batteries[0].StoredKWh + batteries[1].StoredKWh;
            double beforeLoss = batteries[0].ConversionLossKWh + batteries[1].ConversionLossKWh;
            double beforeCurtail = energy.CurtailedEnergyKWh;
            clock.Advance(MarsClock.SecondsPerSol);
            foreach (var crop in crops) {
                crop.Advance(0, state.GrowthIntegralSeconds - growth, 1);
                if (state.Health > 0 && crop.Harvest()) state.FoodKg += 6 * .10616667 * state.CropCondition;
            }
            double stored = batteries[0].StoredKWh + batteries[1].StoredKWh;
            double balance = energy.GeneratedEnergyKWh - g - (energy.DeliveredEnergyKWh - delivered)
                - (energy.CurtailedEnergyKWh - beforeCurtail)
                - (batteries[0].ConversionLossKWh + batteries[1].ConversionLossKWh - beforeLoss) - (stored - beforeStored);
            if (Math.Abs(balance) > 1e-6) throw new Exception("Energy not conserved: " + balance);
            if (scenario != "C" && (state.Health < .999 || state.PipeDamage > 0 || state.CropCondition < .999)) throw new Exception("Safe scenario harmed astronaut/equipment");
            if ((scenario == "A" || scenario == "B") && sol > 1 && stored < previousCharge - .1) throw new Exception("Persistent battery drain");
            previousCharge = stored;
            Console.WriteLine($"  delivered={energy.DeliveredEnergyKWh-delivered:F2} kWh; elapsed={state.ElapsedSeconds/3600:F2} h; growth-equivalent={(state.GrowthIntegralSeconds-growth)/MarsClock.SecondsPerSol:F3} sol; warning={(state.FirstWarningSeconds < 0 ? "none" : (state.FirstWarningSeconds/3600).ToString("F2") + " h")}");
            Console.WriteLine($"{scenario} sol {sol}: gen={energy.GeneratedEnergyKWh-g:F2} demand={energy.DemandEnergyKWh-d:F2} battery={batteries[0].StoredKWh+batteries[1].StoredKWh:F2} O2h={state.OxygenHours:F2} water={state.WaterLiters:F3}+{state.SuitWaterLiters:F3} food={state.FoodKg:F3}+{state.SuitFoodKg:F3} crop={state.CropCondition:F3} soil={state.SoilMoisture:F3} temp={state.TemperatureC:F2} damage={state.PipeDamage:F3} health={state.Health:F3} criticalHours={state.FirstCriticalSeconds/3600:F2} deathHours={state.DeathSeconds/3600:F2}");
            if (state.Health <= 0) break;
        }
        if (scenario == "C" && state.Health > 0) throw new Exception("Neglected crisis never produced failure");
        if (scenario == "B")
        {
            // Recovery uses the existing allocation buttons, with the same reduced sunlight.
            while (energy.Allocation.Get(ElectricalSystem.Irrigation) < 3) energy.Allocation.Add(ElectricalSystem.Irrigation);
            clock.Advance(MarsClock.SecondsPerSol);
            if (state.Health < 1 || state.Irrigation < .99) throw new Exception("Recovery failed");
            Console.WriteLine($"B recovery: full irrigation restored, health={state.Health:F2}, battery={batteries[0].StoredKWh+batteries[1].StoredKWh:F2} kWh");
        }
    }
}
