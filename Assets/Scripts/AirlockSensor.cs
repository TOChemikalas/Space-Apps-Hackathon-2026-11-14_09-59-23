using UnityEngine;

// Narrow, player-only crossing sensors keep passage detection separate from the door barriers.
public sealed class AirlockSensor : MonoBehaviour
{
    public enum Position { InnerApproach, ChamberSide, HabitatSide }
    [SerializeField] AirlockCycle airlock;
    [SerializeField] Position position;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (airlock && airlock.isActiveAndEnabled && airlock.IsConfigured)
            airlock.CrossSensor(position, other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (airlock) airlock.LeaveSensor(position, other);
    }
}
