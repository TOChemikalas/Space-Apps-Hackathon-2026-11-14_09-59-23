
using UnityEngine;

public class MarsTimeTest : MonoBehaviour
{
    public MarsClock clock;
    [Min(1)] public float testSpeed = 100f;

    float originalSpeed;

    void OnEnable()
    {
        if (!clock) clock = GetComponent<MarsClock>();
        if (!clock) { enabled = false; return; }

        originalSpeed = clock.speed;
        clock.speed = testSpeed;
    }

    void OnDisable()
    {
        if (clock) clock.speed = originalSpeed;
    }
}
