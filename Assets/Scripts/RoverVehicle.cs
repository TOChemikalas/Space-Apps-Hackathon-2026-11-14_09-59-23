using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class RoverVehicle : MonoBehaviour
{
    public static RoverVehicle Instance { get; private set; }
    public bool IsOccupied => occupied;

    [SerializeField] private string exteriorSceneName;
    [SerializeField] private float driveSpeed = 7f;
    [SerializeField] private float interactDistance = 2f;
    [SerializeField] private Vector2 dismountOffset = new Vector2(1.7f, 0f);
    [Header("Rover battery")]
    [SerializeField] private EnergyController energy;
    [SerializeField] private Transform chargingStation;
    [SerializeField, Min(.1f)] private float dockRadius = 1.7f;
    [SerializeField, Min(.1f)] private float batteryCapacityKWh = 20;

    private double charge = 1;
    private float warningUntil;
    private string warning;
    private Material dockMaterial;
    public float Charge => (float)charge;
    public bool IsConnected { get; private set; }
    public bool IsCharging { get; private set; }
    public bool IsDocked => chargingStation && !occupied
        && ((Vector2)chargingStation.position - body.position).sqrMagnitude <= dockRadius * dockRadius;

    public string ChargerStatus
    {
        get
        {
            if (!IsDocked) return "Park in the marked bay beside the habitat";
            if (IsCharging) return "Charging from habitat batteries";
            if (IsConnected) return charge >= 1 ? "Fully charged · connected" : "Charging stopped · habitat power unavailable";
            return "Docked · charger disconnected";
        }
    }

    public void ToggleCharger()
    {
        if (IsConnected)
        {
            IsCharging = IsConnected = false;
        }
        else if (IsDocked && energy)
        {
            IsConnected = true;
            IsCharging = charge < 1;
        }
    }

    private void StepBattery(double seconds)
    {
        double simulatedHours = seconds * 24 / MarsClock.SecondsPerSol;
        if (IsConnected)
        {
            if (!IsCharging) return;
            double requested = Math.Min(1 - charge, .20 * simulatedHours) * batteryCapacityKWh;
            double delivered = energy.DrawBatteryEnergy(requested, seconds / 3600);
            charge = Math.Min(1, charge + delivered / batteryCapacityKWh);
            if (charge >= 1 - 1e-9) charge = 1;
            IsCharging = charge < 1 && delivered >= requested - 1e-9
                && energy.TryGetBatteryCharge(out float reserve) && reserve > 0;
            return;
        }

        bool driving = occupied && driveInput.sqrMagnitude > 0;
        charge = Math.Max(0, charge - simulatedHours * (driving ? .12 : .002));
        if (charge == 0 && occupied) body.linearVelocity = Vector2.zero;
    }

    private Rigidbody2D body;
    private Collider2D roverCollider;
    private bool occupied;
    private Vector2 driveInput;
    private bool nearby;
    private AstronautController driver;
    private Rigidbody2D driverBody;
    private Vector2 driverClearance;
    private SpriteRenderer[] driverSprites;
    private bool[] driverSpriteRenderingWasOff;
    private InputAction move, interact;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (string.IsNullOrEmpty(exteriorSceneName)) exteriorSceneName = gameObject.scene.name;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
        body = GetComponent<Rigidbody2D>();
        roverCollider = GetComponent<Collider2D>();
        body.gravityScale = 0f;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        Park();
        SceneManager.sceneLoaded += SceneChanged;
    }

    private void Start()
    {
        move = InputSystem.actions?.FindAction("Player/Move");
        interact = InputSystem.actions?.FindAction("Player/Interact");
        if (energy) energy.PowerStepCompleted += StepBattery;
        if (chargingStation) DrawChargingBay();
    }

    private void OnDestroy()
    {
        if (energy) energy.PowerStepCompleted -= StepBattery;
        if (dockMaterial) Destroy(dockMaterial);
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= SceneChanged;
            Instance = null;
        }
    }

    private void OnDisable()
    {
        if (occupied) ExitRover();
    }

    private void SceneChanged(Scene scene, LoadSceneMode mode)
    {
        // Additive interiors share the exterior scene, including its parked rover.
        if (mode == LoadSceneMode.Additive) return;
        bool outside = scene.name == exteriorSceneName;
        if (!outside)
        {
            if (occupied) ExitRover();
            driver = null;
            driverBody = null;
        }
        gameObject.SetActive(outside);
    }

    private void Update()
    {
        if (!driver)
            driver = FindAnyObjectByType<AstronautController>();
        if (!driver || (!driverBody && !driver.TryGetComponent(out driverBody))) return;

        if (occupied)
        {
            driveInput = move?.ReadValue<Vector2>() ?? Vector2.zero;
            if (interact?.WasPressedThisFrame() == true)
                ExitRover();
            return;
        }

        Vector2 closestPoint = roverCollider.ClosestPoint(driverBody.position);
        nearby = (driverBody.position - closestPoint).sqrMagnitude <= interactDistance * interactDistance;
        if (nearby && driver.isActiveAndEnabled && interact?.WasPressedThisFrame() == true)
        {
            if (IsConnected || charge <= 0)
            {
                warning = IsConnected ? "Disconnect the rover charger at Communications first." : "Rover battery depleted.";
                warningUntil = Time.unscaledTime + 4;
                return;
            }
            occupied = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            driverClearance = Vector2.zero;
            if (driverBody.TryGetComponent<Collider2D>(out var driverCollider))
            {
                Bounds bounds = driverCollider.bounds;
                Vector2 offset = (Vector2)bounds.center - driverBody.position;
                driverClearance = (Vector2)bounds.extents + new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            }
            driver.enabled = false;
            HideDriverSprites();
            driverBody.linearVelocity = Vector2.zero;
            driverBody.simulated = false;
            driverBody.position = body.position;
            FollowDriverBody();
        }
    }

    private void HideDriverSprites()
    {
        driverSprites = driver.GetComponentsInChildren<SpriteRenderer>(true);
        driverSpriteRenderingWasOff = new bool[driverSprites.Length];
        for (int i = 0; i < driverSprites.Length; i++)
        {
            driverSpriteRenderingWasOff[i] = driverSprites[i].forceRenderingOff;
            driverSprites[i].forceRenderingOff = true;
        }
    }

    private void ExitRover()
    {
        Park();
        if (driverBody)
        {
            driverBody.position = DismountPosition();
            FollowDriverBody();
            driverBody.simulated = true;
        }

        for (int i = 0; i < driverSprites.Length; i++)
        {
            if (driverSprites[i]) driverSprites[i].forceRenderingOff = driverSpriteRenderingWasOff[i];
        }
        driverSprites = null;
        driverSpriteRenderingWasOff = null;
        if (driver) driver.enabled = true;
    }

    private void Park()
    {
        occupied = false;
        driveInput = Vector2.zero;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    private Vector2 DismountPosition()
    {
        Vector2 destination = body.position + dismountOffset;
        Bounds roverBounds = roverCollider.bounds;
        roverBounds.Expand((Vector3)(driverClearance * 2f));

        if (roverBounds.Contains(new Vector3(destination.x, destination.y, roverBounds.center.z)))
        {
            destination.x = dismountOffset.x < 0f ? roverBounds.min.x - .1f : roverBounds.max.x + .1f;
        }

        return destination;
    }

    private void LateUpdate()
    {
        if (!occupied || !driverBody) return;
        driverBody.position = transform.position;
        FollowDriverBody();
    }

    private void FollowDriverBody()
    {
        Vector2 position = driverBody.position;
        Transform driverTransform = driverBody.transform;
        driverTransform.position = new Vector3(position.x, position.y, driverTransform.position.z);
    }

    private void FixedUpdate()
    {
        if (occupied) body.linearVelocity = charge > 0 ? driveInput * driveSpeed : Vector2.zero;
    }

    private void DrawChargingBay()
    {
        var marker = chargingStation.gameObject.AddComponent<LineRenderer>();
        dockMaterial = new Material(Shader.Find("Sprites/Default"));
        marker.sharedMaterial = dockMaterial;
        marker.startColor = marker.endColor = new Color(1f, .67f, .2f, .85f);
        marker.startWidth = marker.endWidth = .075f;
        marker.sortingLayerName = "Effects";
        marker.useWorldSpace = false;
        marker.loop = true;
        marker.positionCount = 4;
        float half = dockRadius * .8f;
        marker.SetPositions(new[] {
            new Vector3(-half, -half), new Vector3(-half, half),
            new Vector3(half, half), new Vector3(half, -half)
        });
        var sign = new GameObject("Charging bay sign");
        sign.transform.SetParent(chargingStation, false);
        sign.transform.localPosition = new Vector3(0, -half - .2f, 0);
        var label = sign.AddComponent<TextMesh>();
        label.text = "ROVER CHARGING";
        label.anchor = TextAnchor.UpperCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 56;
        label.characterSize = .075f;
        label.color = marker.startColor;
        var renderer = sign.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = label.font.material;
        renderer.sortingLayerName = "Effects";
    }

    private void OnGUI()
    {
        Color original = GUI.contentColor;
        if (occupied || nearby)
        {
            GUI.Label(new Rect(12, Screen.height - 68, 340, 25), occupied ? "[E] Exit rover" : "[E] Enter rover");
            string state = charge <= 0 ? " · DEPLETED" : charge < .10 ? " · CRITICAL" : charge < .25 ? " · LOW" : "";
            int percent = charge > 0 ? Mathf.Max(1, Mathf.RoundToInt(Charge * 100)) : 0;
            if (charge < .10) GUI.contentColor = new Color(1, .29f, .29f);
            else if (charge < .25) GUI.contentColor = new Color(1, .65f, .20f);
            GUI.Label(new Rect(12, Screen.height - 45, 340, 25), $"Rover battery {percent}%{state}");
        }

        if (Time.unscaledTime < warningUntil)
        {
            GUI.contentColor = new Color(1, .29f, .29f);
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            float width = Mathf.Min(Screen.width - 20, 440);
            GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .78f, width, 38), warning, style);
        }
        GUI.contentColor = original;
    }
}
