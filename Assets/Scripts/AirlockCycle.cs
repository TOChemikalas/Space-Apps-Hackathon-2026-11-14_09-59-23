using System.Collections;
using UnityEngine;

// One event-driven cycle for the existing building's two physical doorways.
public sealed class AirlockCycle : MonoBehaviour
{
    public enum Phase { Outside, OpeningOuter, ClosingOuter, Stabilizing, OpeningInner,
        EnterHabitat, ClosingInner, Habitat, EnterChamber, Depressurizing, ExitChamber }
    [SerializeField] Rigidbody2D player;
    [SerializeField] BuildingPortal exteriorPortal;
    [SerializeField] BuildingPortal interiorPortal;
    [SerializeField] AirlockDoor outerDoor;
    [SerializeField] AirlockDoor innerDoor;
    [SerializeField, Min(0)] float stabilizationSeconds = 3;
    [SerializeField, Min(0)] float depressurizationSeconds = 3;
    public Phase State { get; private set; }
    public event System.Action OutsideSettled;
    public float ActivePressureDuration { get; private set; }
    public bool IsConfigured => player && exteriorPortal && interiorPortal && outerDoor && innerDoor
        && outerDoor.IsConfigured && innerDoor.IsConfigured;
    Collider2D waitingOutside, waitingToExit, approachingInner;
    Coroutine sequence;
    bool running;

    public void BindExterior(BuildingPortal entrance, Rigidbody2D body, Collider2D gate, SpriteRenderer gateRenderer)
    {
        player = body;
        exteriorPortal = entrance;
        outerDoor.BindExterior(gate, gateRenderer);
    }

    void OnEnable()
    {
        if (!IsConfigured) return;
        outerDoor.CloseImmediately();
        innerDoor.CloseImmediately();
        State = Phase.Outside;
    }

    void OnDisable()
    {
        if (sequence != null) StopCoroutine(sequence);
        sequence = null;
        running = false;
        waitingOutside = waitingToExit = approachingInner = null;
        ActivePressureDuration = 0f;
        // Never leave a partly animated gate traversable if the component is disabled mid-cycle.
        if (outerDoor && outerDoor.IsConfigured) outerDoor.CloseImmediately();
        if (innerDoor && innerDoor.IsConfigured) innerDoor.CloseImmediately();
    }

    public void RequestTransition(BuildingPortal portal, Collider2D other)
    {
        if (other.attachedRigidbody != player) return;
        if (portal == exteriorPortal && State == Phase.Outside && !running)
        {
            waitingOutside = other;
            Run(Enter());
        }
        else if (portal == interiorPortal && (State == Phase.EnterChamber || State == Phase.ClosingInner
            || State == Phase.Depressurizing || State == Phase.OpeningOuter || State == Phase.ExitChamber))
        {
            waitingToExit = other;
            if (State == Phase.ExitChamber && !running) Run(Leave());
        }
    }

    public void LeaveTrigger(BuildingPortal portal, Collider2D other)
    {
        if (portal == exteriorPortal && waitingOutside == other) waitingOutside = null;
        if (portal == interiorPortal && waitingToExit == other) waitingToExit = null;
    }

    public void CrossSensor(AirlockSensor.Position position, Collider2D other)
    {
        if (other.attachedRigidbody != player) return;
        if (position == AirlockSensor.Position.InnerApproach) approachingInner = other;
        if (running) return;
        if (position == AirlockSensor.Position.HabitatSide && State == Phase.EnterHabitat)
            Run(FinishEntry());
        else if (position == AirlockSensor.Position.InnerApproach && State == Phase.Habitat)
            Run(BeginExit());
        else if (position == AirlockSensor.Position.ChamberSide && State == Phase.EnterChamber)
            Run(Depressurize());
    }

    public void LeaveSensor(AirlockSensor.Position position, Collider2D other)
    {
        if (position == AirlockSensor.Position.InnerApproach && approachingInner == other) approachingInner = null;
    }

    void Run(IEnumerator steps)
    {
        if (running) return;
        running = true;
        sequence = StartCoroutine(Perform(steps));
    }

    IEnumerator Perform(IEnumerator steps)
    {
        yield return steps;
        running = false;
        sequence = null;
        // The player may already be touching the exit trigger when the outer gate finishes opening.
        if (State == Phase.ExitChamber && waitingToExit) Run(Leave());
        else if (State == Phase.Habitat && approachingInner) Run(BeginExit());
        else if (State == Phase.Outside) OutsideSettled?.Invoke();
    }

    IEnumerator Enter()
    {
        State = Phase.OpeningOuter;
        yield return outerDoor.Animate(true);
        if (!waitingOutside)
        {
            State = Phase.ClosingOuter;
            yield return outerDoor.Animate(false);
            State = Phase.Outside;
            yield break;
        }
        if (!exteriorPortal.CompleteAirlockTransition(waitingOutside))
        {
            waitingOutside = null;
            State = Phase.ClosingOuter;
            yield return outerDoor.Animate(false);
            State = Phase.Outside;
            yield break;
        }
        waitingOutside = null;
        State = Phase.ClosingOuter;
        yield return outerDoor.Animate(false);
        State = Phase.Stabilizing;
        yield return PressureCycle(stabilizationSeconds);
        State = Phase.OpeningInner;
        yield return innerDoor.Animate(true);
        SafeToProceed(Phase.EnterHabitat);
    }

    IEnumerator FinishEntry()
    {
        State = Phase.ClosingInner;
        yield return innerDoor.Animate(false);
        State = Phase.Habitat;
    }

    IEnumerator BeginExit()
    {
        State = Phase.OpeningInner;
        yield return innerDoor.Animate(true);
        State = Phase.EnterChamber;
    }

    IEnumerator Depressurize()
    {
        State = Phase.ClosingInner;
        yield return innerDoor.Animate(false);
        State = Phase.Depressurizing;
        yield return PressureCycle(depressurizationSeconds);
        State = Phase.OpeningOuter;
        yield return outerDoor.Animate(true);
        SafeToProceed(Phase.ExitChamber);
    }

    void SafeToProceed(Phase phase)
    {
        State = phase;
        // Single future safe-to-proceed audio hook; pressure and door animation have both finished.
    }

    IEnumerator PressureCycle(float seconds)
    {
        ActivePressureDuration = Mathf.Max(0f, seconds);
        yield return new WaitForSecondsRealtime(ActivePressureDuration);
        ActivePressureDuration = 0f;
    }

    IEnumerator Leave()
    {
        if (!interiorPortal.CompleteAirlockTransition(waitingToExit))
        {
            waitingToExit = null;
            yield break;
        }
        waitingToExit = null;
        State = Phase.ClosingOuter;
        yield return outerDoor.Animate(false);
        State = Phase.Outside;
    }
}
