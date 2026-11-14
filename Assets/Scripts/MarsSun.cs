
using System;
using UnityEngine;

// Pure solar calculations: usable by visuals, electricity, AI, or automated tests.
public static class MarsSun
{
    public const float Latitude = 39.8f; // Arcadia AP-1; longitude 202.1 E is encoded in LOCAL solar time.
    public const double SolsPerYear = 668.6; // Uniform-season approximation.

    public struct State
    {
        public float elevation, azimuth, strength, ls;
        public Vector3 direction; // X=east, Y=north, Z=up
    }

    public static State At(double seconds, float initialLs = 90, float latitude = Latitude)
    {
        double ls = (initialLs + seconds / MarsClock.SecondsPerSol * 360 / SolsPerYear) % 360;
        double season = ls * Math.PI / 180, lat = latitude * Math.PI / 180;
        double decl = Math.Asin(0.42565 * Math.Sin(season)) + 0.25 * Math.Sin(season) * Math.PI / 180;
        double hour = ((seconds / MarsClock.SecondsPerSol % 1) * 24 - 12) * Math.PI / 12;
        double up = Math.Sin(lat) * Math.Sin(decl) + Math.Cos(lat) * Math.Cos(decl) * Math.Cos(hour);
        double east = -Math.Cos(decl) * Math.Sin(hour);
        double north = Math.Cos(lat) * Math.Sin(decl) - Math.Sin(lat) * Math.Cos(decl) * Math.Cos(hour);
        float elevation = (float)(Math.Asin(Math.Max(-1, Math.Min(1, up))) * 180 / Math.PI);

        return new State
        {
            ls = (float)ls,
            elevation = elevation,
            azimuth = Mathf.Repeat((float)(Math.Atan2(east, north) * 180 / Math.PI), 360),
            strength = Mathf.Max(0, (float)up),
            direction = new Vector3((float)east, (float)north, (float)up).normalized
        };
    }
}
