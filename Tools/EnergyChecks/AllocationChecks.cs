using System;
using System.Reflection;

static class AllocationChecks
{
    static int passed;
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        passed++;
    }
    static void Budget(PowerAllocation allocation)
    {
        int sum = allocation.Unassigned;
        for (int i = 0; i < PowerAllocation.SystemCount; i++)
        {
            int points = allocation.Get((ElectricalSystem)i);
            Check(points >= 0, "No negative allocation");
            sum += points;
        }
        Check(sum == allocation.Budget, "Total point budget conserved");
    }
    public static void Run()
    {
        var allocation = new PowerAllocation();
        allocation.Normalize();
        Check(allocation.Budget == 40 && allocation.Unassigned == 6, "Artwork default budget and pool");
        Budget(allocation);
        int water = allocation.Get(ElectricalSystem.Water), oxygen = allocation.Get(ElectricalSystem.Oxygen);
        Check(allocation.Remove(ElectricalSystem.Water), "Water returns a point");
        Check(allocation.Add(ElectricalSystem.Oxygen), "Oxygen receives that point");
        Check(allocation.Get(ElectricalSystem.Water) == water - 1, "Water reading updates immediately");
        Check(allocation.Get(ElectricalSystem.Oxygen) == oxygen + 1, "Oxygen reading updates immediately");
        Check(allocation.Unassigned == 6, "Transfer preserves pool");
        Budget(allocation);
        while (allocation.Remove(ElectricalSystem.Water)) { }
        Check(allocation.Get(ElectricalSystem.Water) == 0, "Water reaches zero");
        Check(!allocation.Remove(ElectricalSystem.Water), "Zero blocks subtraction");
        while (allocation.Add(ElectricalSystem.Oxygen)) { }
        Check(allocation.Unassigned == 0, "Last point spends pool");
        Check(!allocation.Add(ElectricalSystem.Comms), "Empty pool blocks addition to every consumer");
        Budget(allocation);
        for (int i = 0; i < PowerAllocation.SystemCount; i++)
            while (allocation.Remove((ElectricalSystem)i)) { }
        Check(allocation.Unassigned == allocation.Budget, "All points returned");
        for (int i = 0; i < PowerAllocation.SystemCount; i++)
            Check(allocation.Add((ElectricalSystem)i), "Each of seven consumers receives points");
        Budget(allocation);
        // Inspector configuration is normalized before menus read it.
        typeof(PowerAllocation).GetField("budget", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(allocation, 0);
        allocation.Normalize();
        Check(allocation.Unassigned == 0 && !allocation.Add(ElectricalSystem.Water), "Zero configured budget");
        Budget(allocation);
        Console.WriteLine($"PASS: {passed} allocation assertions against production PowerAllocation.");
    }
}
