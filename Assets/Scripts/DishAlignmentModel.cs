using System;
using UnityEngine;

// Owned by the Operations menu's existing game state; independent of UI coordinates and frame rate.
[Serializable]
public sealed class DishAlignmentModel
{
    [SerializeField] double azimuth = 118.9;
    [SerializeField] double elevation = 24.7;
    [SerializeField] double targetAzimuth = 132.6;
    [SerializeField] double targetElevation = 37.4;
    [SerializeField, Min(.01f)] float toleranceDegrees = 2;
    [SerializeField, Range(1, 99.99f)] float successSignalPercent = 95;
    [SerializeField, Min(.01f)] float holdSeconds = 1.5f;
    double heldSeconds;
    public event Action Changed;
    public double Azimuth => azimuth;
    public double Elevation => elevation;
    public double TargetAzimuth => targetAzimuth;
    public double TargetElevation => targetElevation;
    public double AzimuthError => ShortestDelta(azimuth, targetAzimuth);
    public double ElevationError => targetElevation - elevation;
    public double Error => Math.Sqrt(AzimuthError * AzimuthError + ElevationError * ElevationError);
    public double Tolerance => toleranceDegrees;
    public double RequiredHoldSeconds => holdSeconds;
    public double RequiredSignalPercent => successSignalPercent;
    public double HeldSeconds => heldSeconds;
    public bool IsLocked { get; private set; }
    // The configured success percentage occurs at the tolerance boundary; accuracy stays monotonic.
    public double Signal => Math.Exp(Math.Log(successSignalPercent / 100.0) * Math.Pow(Error / toleranceDegrees, 2));
    public bool CanAcquire => Error <= toleranceDegrees + 1e-9 && Signal * 100 >= successSignalPercent - 1e-7;

    public static double Wrap(double degrees) => (degrees % 360 + 360) % 360;
    public static double ShortestDelta(double from, double to) => Wrap(to - from + 180) - 180;

    public void Normalize()
    {
        azimuth = Wrap(azimuth);
        elevation = Math.Clamp(elevation, 0, 90);
        targetAzimuth = Wrap(targetAzimuth);
        targetElevation = Math.Clamp(targetElevation, 0, 90);
        toleranceDegrees = Math.Max(.01f, toleranceDegrees);
        successSignalPercent = Math.Clamp(successSignalPercent, 1, 99.99f);
        holdSeconds = Math.Max(.01f, holdSeconds);
    }

    public void TurnAzimuth(double degrees)
    {
        if (IsLocked) return;
        azimuth = Wrap(azimuth + degrees);
        OrientationChanged();
    }

    public void TurnElevation(double degrees)
    {
        if (IsLocked) return;
        elevation = Math.Clamp(elevation + degrees, 0, 90);
        OrientationChanged();
    }

    void OrientationChanged()
    {
        if (!CanAcquire) heldSeconds = 0;
        Changed?.Invoke();
    }

    public void SetTarget(double azimuthDegrees, double elevationDegrees)
    {
        double nextAzimuth = Wrap(azimuthDegrees), nextElevation = Math.Clamp(elevationDegrees, 0, 90);
        if (nextAzimuth == targetAzimuth && nextElevation == targetElevation) return;
        targetAzimuth = nextAzimuth;
        targetElevation = nextElevation;
        IsLocked = false;
        heldSeconds = 0;
        Changed?.Invoke();
    }

    public bool Hold(double realSeconds)
    {
        if (IsLocked) return false;
        if (!CanAcquire) { heldSeconds = 0; return false; }
        if (realSeconds <= 0) return false;
        heldSeconds = Math.Min(holdSeconds, heldSeconds + realSeconds);
        if (heldSeconds < holdSeconds) return false;
        IsLocked = true;
        return true;
    }

    public void CancelHold() { if (!IsLocked) heldSeconds = 0; }

    public void Restore(double savedAzimuth, double savedElevation, double savedTargetAzimuth, double savedTargetElevation, bool locked)
    {
        azimuth = savedAzimuth;
        elevation = savedElevation;
        targetAzimuth = savedTargetAzimuth;
        targetElevation = savedTargetElevation;
        Normalize();
        IsLocked = locked && CanAcquire;
        heldSeconds = 0;
        Changed?.Invoke();
    }
}
