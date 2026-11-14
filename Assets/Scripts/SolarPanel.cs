using UnityEngine;

public sealed class SolarPanel : MonoBehaviour
{
    [SerializeField] private MarsClock clock;
    [SerializeField] private MarsLighting lighting;
    [SerializeField, Min(0)] private float peakKilowatts = 2f;

    public float CurrentKilowatts => clock ? KilowattsAt(clock.Seconds) : 0f;

    public float KilowattsAt(double marsSeconds)
    {
        if (!isActiveAndEnabled || !lighting)
            return 0f;

        return peakKilowatts * MarsSun.At(marsSeconds, lighting.startingLs).strength;
    }
}
