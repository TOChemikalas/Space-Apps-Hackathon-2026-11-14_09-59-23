
using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class MarsClock : MonoBehaviour
{
    public const double SecondsPerSol = 88775.245;
    [Min(1)] public float realSecondsPerSol = 300;
    [Range(0, 24)] public float startHour = 6;
    [Min(0)] public float speed = 1;
    public bool paused;

    public double Seconds { get; private set; }
    public double Hour => Seconds / SecondsPerSol % 1 * 24;
    public int Sol => 1 + (int)(Seconds / SecondsPerSol);
    public event Action<double, double> Advanced; // fromSeconds, toSeconds; zero-length = seek

    void Awake() => Seconds = startHour / 24 * SecondsPerSol;
    void Update()
    {
        if (!paused && speed > 0) Advance(ScaleTime(Time.unscaledDeltaTime));
    }

    // Scales real seconds to Mars seconds so one full Mars day takes 5 real minutes at the default settings.
    public double ScaleTime(double realSeconds) => realSeconds * SecondsPerSol / realSecondsPerSol * speed;

    public void Advance(double delta)
    {
        if (delta <= 0 || double.IsNaN(delta) || double.IsInfinity(delta)) return;
        double before = Seconds;
        Seconds += delta;
        Advanced?.Invoke(before, Seconds);
    }

    public void SeekHour(double hour)
    {
        if (double.IsNaN(hour) || double.IsInfinity(hour)) return;
        Seconds = Math.Floor(Seconds / SecondsPerSol) * SecondsPerSol + ((hour % 24 + 24) % 24) / 24 * SecondsPerSol;
        Advanced?.Invoke(Seconds, Seconds);
    }

    // Restores an absolute save timestamp without integrating gameplay through the skipped interval.
    public void RestoreSeconds(double seconds)
    {
        if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
        Seconds = seconds;
        Advanced?.Invoke(Seconds, Seconds);
    }
}
