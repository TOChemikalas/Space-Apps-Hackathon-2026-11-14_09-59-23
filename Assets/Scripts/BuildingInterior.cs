using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the shared colony running while only the visited interior is active.
public sealed class BuildingInterior : MonoBehaviour
{
    [SerializeField] SceneSpawn arrival;
    [SerializeField] BuildingPortal exit;
    [SerializeField] Vector2 runtimePosition;
    [SerializeField] GreenhouseInteractionUI greenhouse;
    [SerializeField] AirlockCycle airlock;

    public SceneSpawn Arrival => arrival;
    public BuildingPortal Exit => exit;
    public AirlockCycle Airlock => airlock;

    void Awake()
    {
        // In an additive editor layout, keep the interior out of the exterior until a portal opens it.
        if (SceneManager.sceneCount > 1) gameObject.SetActive(false);
    }

    internal void Bind(BuildingPortal entrance, SceneSpawn outside, MarsClock clock, Camera camera,
        Canvas hud, Collider2D exteriorGate, SpriteRenderer exteriorGateRenderer)
    {
        gameObject.SetActive(false);
        transform.position = runtimePosition;
        var astronaut = camera.GetComponentInParent<AstronautController>();
        var player = astronaut.GetComponent<Rigidbody2D>();
        arrival.BindCamera(camera);
        outside.SetRestoreViewFrom(arrival);
        exit.BindExit(entrance, outside, this);

        if (greenhouse) greenhouse.Bind(clock, player, hud);
        foreach (var bed in GetComponentsInChildren<CrewBed>(true)) bed.Bind(clock, astronaut, hud);
        foreach (var door in GetComponentsInChildren<RoomDoor>(true)) door.BindPlayer(player);
        foreach (var effect in GetComponentsInChildren<AirlockDecontamination>(true)) effect.BindCanvas(hud);
        if (!airlock) return;
        airlock.BindExterior(entrance, player, exteriorGate, exteriorGateRenderer);
        airlock.OutsideSettled += Close;
    }

    internal void Open() => gameObject.SetActive(true);

    internal void Entered()
    {
        if (greenhouse) greenhouse.SetInside(true);
    }

    internal void Close()
    {
        if (greenhouse) greenhouse.SetInside(false);
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (airlock) airlock.OutsideSettled -= Close;
    }
}

