
using System;
using UnityEngine;

/// <summary>
/// Dust, equipment condition and faults for ONE
/// SolarArray simulation.
/// In PROJECT SOL this is a combined colony solar
/// array attached to Mars Energy.
/// </summary>
public sealed class MarsSolarPanelWeather : MonoBehaviour
{
    [Header("References")]
    public MarsClock clock;
    public MarsDustStorm storm;

    [Header("Persistent panel state (0 to 1)")]
    [Range(0f, 1f)]
    public float dustCoverage;

    [Range(0f, 1f)]
    public float integrity = 1f;

    public bool faulted;

    [Header("Gameplay tuning")]
    [Range(0f, 1f)]
    public float dustAccumulationPerStormSol = 0.45f;

    [Range(0f, 1f)]
    public float integrityLossPerStormSol = 0.02f;

    [Min(0f)]
    // Keep unrecoverable random outages disabled until player repair is implemented.
    public float peakFaultChancePerSol = 0f;

    [Header("Simulation")]
    [Min(0.05f)]
    public float sampleStepHours = 0.25f;

    public float OutputMultiplier =>
        OutputMultiplierAt(
            clock ? clock.Seconds : 0.0);

    public float OutputMultiplierAt(
        double absoluteSeconds)
    {
        float sunlight =
            storm && storm.isActiveAndEnabled
                ? Mathf.Clamp01(
                    1f - storm.maxSunlightLoss *
                    storm.IntensityAt(absoluteSeconds))
                : 1f;

        float condition = faulted
            ? 0f
            : Mathf.Clamp01(integrity);

        return Mathf.Clamp01(
            sunlight *
            (1f - Mathf.Clamp01(dustCoverage)) *
            condition);
    }

    void OnEnable()
    {
        if (!clock)
            clock = FindFirstObjectByType<MarsClock>();

        if (!storm)
            storm = FindFirstObjectByType<MarsDustStorm>();

        if (clock)
            clock.Advanced += OnClockAdvanced;
    }

    void OnDisable()
    {
        if (clock)
            clock.Advanced -= OnClockAdvanced;
    }

    void OnClockAdvanced(
        double fromSeconds,
        double toSeconds)
    {
        // Seeking time must not age the panels.
        if (!storm ||
            !storm.isActiveAndEnabled ||
            toSeconds <= fromSeconds)
            return;

        // Subdivide time jumps so fast-forward
        // doesn't skip the storm.
        double sampleSeconds =
            Math.Max(0.05, sampleStepHours)
            * MarsClock.SecondsPerSol / 24.0;

        double position = fromSeconds;

        while (position < toSeconds)
        {
            double end = Math.Min(
                toSeconds,
                position + sampleSeconds);

            float intensity = storm.IntensityAt(
                (position + end) * 0.5);

            if (intensity > 0f)
            {
                float durationSols = (float)(
                    (end - position) /
                    MarsClock.SecondsPerSol);

                dustCoverage = Mathf.Clamp01(
                    dustCoverage +
                    intensity *
                    dustAccumulationPerStormSol *
                    durationSols);

                integrity = Mathf.Clamp01(
                    integrity -
                    intensity * intensity *
                    integrityLossPerStormSol *
                    durationSols);

                if (!faulted &&
                    peakFaultChancePerSol > 0f)
                {
                    float exposure =
                        peakFaultChancePerSol *
                        intensity * intensity *
                        durationSols;

                    float probability =
                        1f - Mathf.Exp(-exposure);

                    if (UnityEngine.Random.value <
                        probability)
                    {
                        faulted = true;
                    }
                }
            }

            position = end;
        }
    }

    // Call after astronaut maintenance tasks.
    public void Clean(float amount = 1f)
    {
        dustCoverage = Mathf.Clamp01(
            dustCoverage - Mathf.Max(0f, amount));
    }

    // Restores integrity and removes the fault.
    // Does NOT remove dust.
    public void Repair(float restoredIntegrity = 0.5f)
    {
        integrity = Mathf.Clamp01(
            integrity + Mathf.Max(0f, restoredIntegrity));

        faulted = false;
    }

    [ContextMenu("TEST — Clean solar array")]
    void DebugClean()
    {
        if (Application.isPlaying)
            Clean();
    }

    [ContextMenu("TEST — Cause solar fault")]
    void DebugFault()
    {
        if (Application.isPlaying)
            faulted = true;
    }

    [ContextMenu("TEST — Repair solar fault")]
    void DebugRepair()
    {
        if (Application.isPlaying)
            Repair();
    }
}
