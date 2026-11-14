
using System;
using UnityEngine;

/// <summary>
/// A single scheduled Mars dust storm driven by the existing simulation clock.
/// All storm rates are GAME BALANCING, not NASA-measured weather forecasts.
/// </summary>
public sealed class MarsDustStorm : MonoBehaviour
{
    [Header("References")]
    public MarsClock clock;

    [Header("Storm Schedule (local Mars time)")]
    [Min(1)] public int startSol = 4;
    [Range(0f, 24f)] public float startHour = 10f;
    [Min(0.1f)] public float durationHours = 14f;
    [Min(0.1f)] public float fadeHours = 2f;
    [Min(0f)] public float warningLeadHours = 4f;

    [Header("Intensity / Solar Attenuation (gameplay values)")]
    [Range(0f, 1f)] public float peakIntensity = 0.95f;
    [Range(0f, 1f)] public float maxSunlightLoss = 0.85f;

    public float Intensity { get; private set; }

    public bool IsActive =>
        isActiveAndEnabled && Intensity > 0.001f;

    public float SolarTransmission => isActiveAndEnabled
        ? Mathf.Clamp01(1f - maxSunlightLoss * Intensity)
        : 1f;

    public double HoursUntilStart => !clock
        ? double.PositiveInfinity
        : (StartSeconds - clock.Seconds)
          / MarsClock.SecondsPerSol * 24.0;

    public bool IsApproaching =>
        isActiveAndEnabled &&
        HoursUntilStart >= 0.0 &&
        HoursUntilStart <= warningLeadHours;

    double StartSeconds =>
        ((Math.Max(1, startSol) - 1) + startHour / 24.0)
        * MarsClock.SecondsPerSol;

    void OnEnable()
    {
        if (!clock)
            clock = FindFirstObjectByType<MarsClock>();

        if (clock)
        {
            clock.Advanced += OnClockAdvanced;
            Intensity = IntensityAt(clock.Seconds);
        }
    }

    void OnDisable()
    {
        if (clock)
            clock.Advanced -= OnClockAdvanced;

        Intensity = 0f;
    }

    void OnClockAdvanced(
        double fromSeconds,
        double toSeconds)
    {
        Intensity = IntensityAt(toSeconds);
    }

    /// <summary>
    /// Pure, timestamped lookup. SolarArray can sample
    /// a 5-second midpoint without relying on the order
    /// of MarsClock event subscribers.
    /// </summary>
    public float IntensityAt(double absoluteSeconds)
    {
        if (!isActiveAndEnabled)
            return 0f;

        double elapsedHours =
            (absoluteSeconds - StartSeconds)
            / MarsClock.SecondsPerSol * 24.0;

        if (elapsedHours < 0.0 ||
            elapsedHours >= durationHours)
            return 0f;

        float fade = Mathf.Clamp(
            fadeHours,
            0.1f,
            durationHours * 0.5f);

        float inFactor = Mathf.SmoothStep(
            0f, 1f,
            Mathf.Clamp01(
                (float)(elapsedHours / fade)));

        float outFactor = Mathf.SmoothStep(
            0f, 1f,
            Mathf.Clamp01(
                (float)((durationHours - elapsedHours) / fade)));

        return Mathf.Clamp01(peakIntensity)
            * inFactor * outFactor;
    }
}
