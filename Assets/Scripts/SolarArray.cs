using System;
using UnityEngine;

public sealed class SolarArray : MonoBehaviour
{
    [Min(0)] public float area = 100;
    [Min(0)] public float irradiance = 590;
    [Range(0, 1)] public float cellEfficiency = .32f;
    [Range(0, 1)] public float transmission = .70f;
    [Range(0, 1)] public float packing = .90f;
    [Range(0, 1)] public float electricalEfficiency = .90f;
    [Range(0, 1)] public float dustFactor = 1;
    public float latitude = MarsSun.Latitude;

    public double PowerKW(double seconds, float startingLs)
    {
        return PowerAtElevation(MarsSun.At(seconds, startingLs, latitude).elevation);
    }

    public double PowerAtElevation(double elevation)
    {
        double incidence = Math.Max(0, Math.Sin(elevation * Math.PI / 180));
        return Math.Max(0, irradiance) * Math.Max(0, area) * Mathf.Clamp01(cellEfficiency)
            * Mathf.Clamp01(transmission) * Mathf.Clamp01(packing)
            * Mathf.Clamp01(electricalEfficiency) * Mathf.Clamp01(dustFactor) * incidence / 1000;
    }
}
