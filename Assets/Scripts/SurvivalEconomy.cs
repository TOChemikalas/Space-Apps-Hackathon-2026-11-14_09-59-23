using System;

// Gameplay engineering model. Units: kW, hours, litres, kg, degrees C.
// No rescue resources, grace periods or battery-triggered death.
[Serializable]
public sealed class SurvivalEconomy
{
    [Serializable]
    public sealed class Tuning
    {
        public double OxygenProductionHoursPerHour = 1.1;
        public double HabitatHeatCapacityKWhPerC = 2;
        public double HeatLossKWPerC = .02;
        public double OutsideTemperatureC = -40;
        public double WaterDemandLitersPerSol = 2.57;
        public double FoodDemandKgPerSol = .637;
    }
    public Tuning Settings = new Tuning();
    public double WaterLiters = 5.14, FoodKg = 3.185;
    public double SuitWaterLiters = 5.14, SuitFoodKg = 3.185;
    public double OxygenHours = 8, TemperatureC = 20, Health = 1;
    public double SoilMoisture = 1, CropCondition = 1, PipeDamage;
    public double SuitCondition = 1;
    public double Recycling = .90;
    public bool InHabitat;
    public bool InGreenhouse;
    public bool InRover, RoverPowered, BasePowered;
    public bool IsSheltered => InHabitat || InGreenhouse || InRover;
    public bool LowBattery;
    public readonly double[] Requested = new double[7], Delivered = new double[7];
    public double GrowthIntegralSeconds { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public double FirstWarningSeconds = -1, FirstCriticalSeconds = -1, DeathSeconds = -1;
    public double MinimumOxygenHours = 8, MinimumTemperatureC = 20;
    public double Communications => Math.Clamp(Delivered[6] / .7, 0, 1);
    public double SuitService => Math.Clamp(Delivered[5], 0, 1);
    public double Irrigation => Math.Clamp(Delivered[2] / .8, 0, 1);
    // Allocation reference at full bus delivery. Water/irrigation/suit thresholds
    // preserve normal performance; oxygen/heating/comms preserve essential function.
    public int MinimumAllocation(ElectricalSystem system)
    {
        switch (system)
        {
            case ElectricalSystem.Water: return 6;
            case ElectricalSystem.Oxygen: return (int)Math.Ceiling(7 / Math.Max(.001, Settings.OxygenProductionHoursPerHour));
            case ElectricalSystem.Irrigation: return 3;
            case ElectricalSystem.Cooling: return (int)Math.Ceiling(Math.Max(0, Settings.HeatLossKWPerC * (Settings.OutsideTemperatureC - 24)) / .3);
            case ElectricalSystem.Heating: return (int)Math.Ceiling(Math.Max(0, Settings.HeatLossKWPerC * (20 - Settings.OutsideTemperatureC)) / .3 - 1e-9);
            case ElectricalSystem.SuitRepairStation: return 3;
            case ElectricalSystem.Comms: return 3;
            default: return 0;
        }
    }
    public string FailureReason => OxygenHours <= 0 ? "Oxygen depleted" : SuitWaterLiters <= 0 ? "Critical dehydration" :
        SuitFoodKg <= 0 ? "Critical malnutrition" : "Unsafe habitat temperature";
    public string Warning => Health <= .5 ? "CRITICAL: " + FailureReason : OxygenHours < 2 ? "Oxygen reserve below 2 hours" :
        WaterLiters < 1 ? "Habitat water below 1 litre" : TemperatureC < 12 ? "Habitat cold: increase heating" :
        TemperatureC > 30 ? "Habitat hot: increase cooling" : SoilMoisture < .3 ? "Crops dry: increase irrigation" : CropCondition < .8 ? "Crops damaged" : SuitCondition < .5 ? "Suit worn: restore suit servicing" : LowBattery ? "Battery below 20%: reduce demand" : "";

    public double Request(PowerAllocation allocation, double seconds)
    {
        double hour = seconds / MarsClock.SecondsPerSol % 1 * 24;
        bool workShift = hour >= 8 && hour < 16;
        // Allocation caps power; thermostats and work schedules determine actual demand.
        Requested[0] = Math.Min(.5, allocation.Get(ElectricalSystem.Water) * .5 / 6);
        Requested[1] = Math.Min(1.6, allocation.Get(ElectricalSystem.Oxygen) * .8 / 7);
        if (InGreenhouse) Requested[1] = Math.Max(.8, Requested[1]);
        Requested[2] = Math.Min(.8, allocation.Get(ElectricalSystem.Irrigation) * .8 / 3);
        Requested[3] = Math.Min(allocation.Get(ElectricalSystem.Cooling) * .3, Math.Max(0, TemperatureC - 24) * .5);
        // Heat loss UA=0.02 kW/K, ambient=-40 C, capacity=2 kWh/K.
        Requested[4] = Math.Min(allocation.Get(ElectricalSystem.Heating) * .3,
            Math.Max(0, Settings.HeatLossKWPerC * (TemperatureC - Settings.OutsideTemperatureC) + (20 - TemperatureC) * .5));
        Requested[5] = workShift ? Math.Min(1, allocation.Get(ElectricalSystem.SuitRepairStation) / 3d) : 0;
        Requested[6] = .7 * Math.Min(1, allocation.Get(ElectricalSystem.Comms) / 6d);
        double total = 0;
        foreach (double value in Requested) total += value;
        return total;
    }

    public void Step(double seconds, double busFraction)
    {
        if (seconds <= 0 || Health <= 0) return;
        double hours = seconds / 3600, sols = seconds / MarsClock.SecondsPerSol;
        ElapsedSeconds += seconds;
        for (int i = 0; i < 7; i++) Delivered[i] = Requested[i] * Math.Clamp(busFraction, 0, 1);
        bool independentOxygen = InRover ? RoverPowered : InGreenhouse && BasePowered;
        double oxygenSupply = !InRover && InHabitat ? Math.Clamp(Delivered[1] / .8, 0, 2) : 0;
        double production = independentOxygen ? Math.Max(1, Settings.OxygenProductionHoursPerHour) : oxygenSupply * Settings.OxygenProductionHoursPerHour;
        SuitCondition = Math.Clamp(SuitCondition + hours * (InHabitat && !InRover ? SuitService * .02 : IsSheltered ? 0 : -.002), 0, 1);
        OxygenHours = Math.Clamp(OxygenHours + hours * (production - 1 - (IsSheltered ? 0 : 1 - SuitCondition)), 0, 8);
        double recycling = Recycling * Math.Clamp(Delivered[0] / .5, 0, 1) * (1 - PipeDamage);
        double waterDemand = Settings.WaterDemandLitersPerSol * sols, foodDemand = Settings.FoodDemandKgPerSol * sols;
        if (InHabitat)
        {
            double recovered = waterDemand * recycling;
            double supplied = Math.Min(WaterLiters, waterDemand - recovered);
            WaterLiters -= supplied;
            waterDemand -= supplied + recovered;
            double fed = Math.Min(FoodKg, foodDemand);
            FoodKg -= fed;
            foodDemand -= fed;
        }
        SuitWaterLiters = Math.Max(0, SuitWaterLiters - waterDemand);
        SuitFoodKg = Math.Max(0, SuitFoodKg - foodDemand);
        TemperatureC += hours * (Delivered[4] - Delivered[3] - Settings.HeatLossKWPerC * (TemperatureC - Settings.OutsideTemperatureC)) / Settings.HabitatHeatCapacityKWhPerC;
        // Freezing damage is persistent; restoring power thaws but does not repair cracked pipes.
        PipeDamage = Math.Clamp(PipeDamage + Math.Max(0, -TemperatureC) * hours / 1000, 0, 1);
        SoilMoisture = Math.Clamp(SoilMoisture + hours * (Irrigation - .35) / 24, 0, 1);
        CropCondition = Math.Clamp(CropCondition - hours * (SoilMoisture <= 0 ? .01 : 0)
            - Math.Max(0, 5 - TemperatureC) * hours / 1000, 0, 1);
        GrowthIntegralSeconds += seconds * Irrigation * CropCondition * Math.Clamp(TemperatureC / 20, 0, 1);
        double injury = (OxygenHours <= 0 ? 1 : 0) + (SuitWaterLiters <= 0 ? .1 : 0)
            + (SuitFoodKg <= 0 ? .02 : 0) + (!InRover && (InHabitat || InGreenhouse) ? (Math.Max(0, 5 - TemperatureC) + Math.Max(0, TemperatureC - 35)) / 100 : 0);
        Health = Math.Clamp(Health - injury * hours, 0, 1);
        if (FirstWarningSeconds < 0 && Warning.Length > 0) FirstWarningSeconds = ElapsedSeconds;
        MinimumOxygenHours = Math.Min(MinimumOxygenHours, OxygenHours);
        MinimumTemperatureC = Math.Min(MinimumTemperatureC, TemperatureC);
        if (FirstCriticalSeconds < 0 && (injury > 0 || SoilMoisture <= 0)) FirstCriticalSeconds = ElapsedSeconds;
        if (Health <= 0 && DeathSeconds < 0) DeathSeconds = ElapsedSeconds;
    }
}
