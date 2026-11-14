using System;
using UnityEngine;

public sealed class Greenhouse : MonoBehaviour
{
    private const double SecondsPerMarsHour = MarsClock.SecondsPerSol / 24;
    private const double GrowthSampleInterval = SecondsPerMarsHour / 4;

    [SerializeField] private MarsClock clock;
    [SerializeField] private SolarPanel solarPanel;
    [SerializeField, Min(0.01f)] private float requiredKilowatts = 0.5f;
    [SerializeField, Min(0.25f)] private float growthHours = 3f;

    private double poweredSeconds;
    private double RequiredGrowthSeconds => growthHours * SecondsPerMarsHour;

    public float Growth => (float)(poweredSeconds / RequiredGrowthSeconds);
    public bool IsPowered => solarPanel && solarPanel.CurrentKilowatts >= requiredKilowatts;
    public bool IsReady => poweredSeconds >= RequiredGrowthSeconds;
    public int Harvests { get; private set; }

    private void OnEnable()
    {
        if (clock)
            clock.Advanced += Grow;
    }

    private void OnDisable()
    {
        if (clock)
            clock.Advanced -= Grow;
    }

    public bool Harvest()
    {
        if (!IsReady)
            return false;

        Harvests++;
        poweredSeconds = 0;
        return true;
    }

    private void Grow(double fromSeconds, double toSeconds)
    {
        if (!solarPanel || !solarPanel.isActiveAndEnabled)
            return;

        while (fromSeconds < toSeconds && !IsReady)
        {
            // Sample across larger time steps so a step through dusk can slow growth.
            double end = Math.Min(fromSeconds + GrowthSampleInterval, toSeconds);
            if (end <= fromSeconds)
                break;

            if (solarPanel.KilowattsAt((fromSeconds + end) / 2) >= requiredKilowatts)
            {
                poweredSeconds += end - fromSeconds;
                if (poweredSeconds >= RequiredGrowthSeconds - 1e-6)
                    poweredSeconds = RequiredGrowthSeconds;
            }

            fromSeconds = end;
        }
    }
}
