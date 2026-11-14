using System;

// Pure per-bed state. Clock seconds are SI seconds; an Earth day is 86,400 seconds.
[Serializable]
public sealed class CropProgress
{
    public bool IsPlanted { get; private set; }
    public double DurationSeconds { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public double Fraction => IsPlanted ? Math.Min(1, ElapsedSeconds / DurationSeconds) : 0;
    public bool IsReady => IsPlanted && Fraction >= 1;
    public int Stage => IsPlanted ? Math.Min(5, (int)(Fraction * 6)) : -1;

    public bool Plant(double durationSeconds)
    {
        if (IsPlanted || durationSeconds <= 0 || double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds)) return false;
        DurationSeconds = durationSeconds;
        ElapsedSeconds = 0;
        IsPlanted = true;
        return true;
    }

    public void Advance(double fromSeconds, double toSeconds, double multiplier)
    {
        double elapsed = toSeconds - fromSeconds;
        if (!IsPlanted || IsReady || elapsed <= 0 || multiplier <= 0
            || double.IsNaN(elapsed) || double.IsInfinity(elapsed)
            || double.IsNaN(multiplier) || double.IsInfinity(multiplier)) return;
        ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + elapsed * multiplier);
    }

    public bool Harvest()
    {
        if (!IsReady) return false;
        IsPlanted = false;
        ElapsedSeconds = 0;
        DurationSeconds = 0;
        return true;
    }
}
