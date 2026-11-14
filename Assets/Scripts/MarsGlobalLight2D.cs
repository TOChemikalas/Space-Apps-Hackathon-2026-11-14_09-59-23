using UnityEngine;
using UnityEngine.Rendering.Universal;

// Bridges MarsClock + MarsLighting's solar calculations to Unity URP's Global Light 2D.
// Leave MarsClock.cs, MarsLighting.cs and MarsSun.cs unchanged.
[RequireComponent(typeof(MarsClock))]
[RequireComponent(typeof(MarsLighting))]
public sealed class MarsGlobalLight2D : MonoBehaviour
{
    [Tooltip("Drag the scene's single Global Light 2D here.")]
    public Light2D globalLight;

    private MarsClock clock;
    private MarsLighting settings;

    private void Awake()
    {
        clock = GetComponent<MarsClock>();
        settings = GetComponent<MarsLighting>();
    }

    private void LateUpdate()
    {
        if (globalLight == null) return;

        var sun = MarsSun.At(clock.Seconds, settings.startingLs);
        float daylight = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(-8f, 18f, sun.elevation));
        float dawnAmount = Mathf.Clamp01(1f - Mathf.Abs(sun.elevation - 2f) / 16f);

        globalLight.intensity = Mathf.Lerp(settings.nightBrightness, 1f, daylight);
        globalLight.color = Color.Lerp(
            Color.Lerp(settings.night, settings.day, daylight),
            settings.dawn,
            dawnAmount * 0.65f);
    }
}
