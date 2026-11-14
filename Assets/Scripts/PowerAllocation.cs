using System;
using UnityEngine;

public enum ElectricalSystem { Water, Oxygen, Irrigation, Cooling, Heating, SuitRepairStation, Comms }

// Points are configuration, not energy. The seven allocations plus the pool always equal Budget.
[Serializable]
public sealed class PowerAllocation
{
    public const int SystemCount = 7;
    [SerializeField, Min(0)] int budget = 40;
    [SerializeField] int[] points = { 6, 7, 3, 4, 5, 3, 6 };
    public int Budget => budget;
    public int Unassigned
    {
        get
        {
            int used = 0;
            foreach (int value in points) used += value;
            return budget - used;
        }
    }

    public int Get(ElectricalSystem system) => points[(int)system];

    public bool Add(ElectricalSystem system)
    {
        if (Unassigned == 0) return false;
        points[(int)system]++;
        return true;
    }

    public bool Remove(ElectricalSystem system)
    {
        int index = (int)system;
        if (points[index] == 0) return false;
        points[index]--;
        return true;
    }

    // Used when loading/editing Inspector configuration; lower budgets trim later rows first.
    public void Normalize()
    {
        budget = Math.Max(0, budget);
        if (points == null) points = new int[SystemCount];
        else if (points.Length != SystemCount) Array.Resize(ref points, SystemCount);
        int remaining = budget;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = Math.Clamp(points[i], 0, remaining);
            remaining -= points[i];
        }
    }
}
