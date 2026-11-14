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
    }

    private void OnDestroy()
    {
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
        if (occupied) body.linearVelocity = driveInput * driveSpeed;
    }

    private void OnGUI()
    {
        if (!occupied && !nearby) return;
        GUI.Label(new Rect(12, Screen.height - 68, 340, 28),
            occupied ? "[E] Exit rover" : "[E] Enter rover");
    }
}
