
    
  
using System;
using UnityEngine;

/// <summary>
/// Existing clear-sky photovoltaic formula, with optional dynamic dust/weather.
/// EnergyController already calls PowerKW() for every simulation step.
/// Replace the ORIGINAL SolarArray.cs file with this script; never create
/// another SolarArray class alongside it.
/// </summary>
public sealed class SolarArray : MonoBehaviour
{
    [Min(0)] public float area = 100;
    [Min(0)] public float irradiance = 590;
    [Range(0, 1)] public float cellEfficiency = .32f;
    [Range(0, 1)] public float transmission = .70f;
    [Range(0, 1)] public float packing = .90f;
    [Range(0, 1)] public float electricalEfficiency = .90f;
    [Range(0, 1)] public float dustFactor = 1f;
    public float latitude = MarsSun.Latitude;

    [Header("Dynamic weather (optional)")]
    public MarsSolarPanelWeather panelWeather;

    void Awake()
    {
        if (!panelWeather) panelWeather = GetComponent<MarsSolarPanelWeather>();
    }

    public double PowerKW(double seconds, float startingLs)
    {
        float elevation = MarsSun.At(seconds, startingLs, latitude).elevation;
        double clearSkyKW = ClearSkyPowerAtElevation(elevation);
        double weather = panelWeather && panelWeather.isActiveAndEnabled
            ? panelWeather.OutputMultiplierAt(seconds)
            : 1.0;
        return clearSkyKW * weather;
    }

    /// <summary>
    /// Maintains the old API. Includes CURRENT weather state for direct UI calls.
    /// Use ClearSkyPowerAtElevation for a stable full-output reference meter.
    /// </summary>
    public double PowerAtElevation(double elevation)
    {
        double weather = panelWeather && panelWeather.isActiveAndEnabled
            ? panelWeather.OutputMultiplier
            : 1.0;
        return ClearSkyPowerAtElevation(elevation) * weather;
    }

    public double ClearSkyPowerAtElevation(double elevation)
    {
        double incidence = Math.Max(0, Math.Sin(elevation * Math.PI / 180));
        return Math.Max(0, irradiance) * Math.Max(0, area)
            * Mathf.Clamp01(cellEfficiency) * Mathf.Clamp01(transmission)
            * Mathf.Clamp01(packing) * Mathf.Clamp01(electricalEfficiency)
            * Mathf.Clamp01(dustFactor) * incidence / 1000.0;
    }
}

