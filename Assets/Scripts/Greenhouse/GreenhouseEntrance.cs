using UnityEngine;

// Greenhouse-specific UI hook; all teleport and camera behavior lives in BuildingEntrance.
public sealed class GreenhouseEntrance : BuildingEntrance
{
    [SerializeField] GreenhouseInteractionUI greenhouse;

    protected override void OnTransition(bool inside)
    {
        if (greenhouse) greenhouse.SetInside(inside);
    }
}
