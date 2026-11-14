using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Use a linked spawn in this scene or a named spawn in another scene.
public sealed class BuildingPortal : MonoBehaviour
{
    [SerializeField] BuildingPortal pairedDoor;
    [SerializeField] GreenhouseInteractionUI greenhouse;
    [SerializeField] SceneSpawn arrival;
    [SerializeField] string destinationScene;
    [SerializeField] string arrivalSpawnId;
    [SerializeField] string prompt = "Enter building";
    [SerializeField] Canvas promptCanvas;
    [SerializeField] bool entersInterior;
    [SerializeField, Min(0)] float transitionCooldown = .5f;
    [SerializeField] AirlockCycle airlock;
    float blockedUntil;
    Rigidbody2D nearbyPlayer;
    InputAction interact;
    GameObject promptOverlay;
    static int lastInteractionFrame = -1;
    static bool sceneTransitionInProgress;

    public string Prompt => "E: " + prompt;

    public bool CanUse => isActiveAndEnabled && nearbyPlayer && !sceneTransitionInProgress
        && (arrival || !string.IsNullOrWhiteSpace(destinationScene)) && Time.unscaledTime >= blockedUntil
        && (!greenhouse || greenhouse.IsInside != entersInterior);

    void Start() => interact = InputSystem.actions?.FindAction("Player/Interact");

    void Update()
    {
        // The greenhouse gives its doors priority over the nearby crop menu.
        if (!greenhouse && interact?.WasPressedThisFrame() == true) Interact();
    }

    void LateUpdate()
    {
        if (greenhouse || (airlock && airlock.isActiveAndEnabled) || !promptCanvas) return;
        if (!promptOverlay && CanUse)
        {
            RectTransform overlay = GreenhouseCanvas.Overlay(promptCanvas, name + " prompt");
            promptOverlay = overlay.gameObject;
            var tag = GreenhouseCanvas.Image(overlay, "Portal prompt", new Rect(0, 0, 320, 42), GreenhouseCanvas.Panel);
            RectTransform rect = tag.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            GreenhouseCanvas.Label(rect, "Prompt", new Rect(0, 0, 320, 42), Prompt, 19, TextAnchor.MiddleCenter);
        }
        if (promptOverlay) promptOverlay.SetActive(CanUse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D player = other.attachedRigidbody;
        if (!player || !player.TryGetComponent<AstronautController>(out var astronaut) || !astronaut.isActiveAndEnabled) return;
        nearbyPlayer = player;
        if (greenhouse) greenhouse.DoorEntered(this);
        if (airlock && airlock.isActiveAndEnabled && airlock.IsConfigured)
            airlock.RequestTransition(this, other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != nearbyPlayer) return;
        if (airlock) airlock.LeaveTrigger(this, other);
        ClearNearbyPlayer();
    }

    void OnDisable()
    {
        ClearNearbyPlayer();
        if (promptOverlay) promptOverlay.SetActive(false);
    }

    void OnDestroy()
    {
        if (promptOverlay) Destroy(promptOverlay);
    }

    void ClearNearbyPlayer()
    {
        nearbyPlayer = null;
        if (greenhouse) greenhouse.DoorExited(this);
    }

    public void Interact()
    {
        if (airlock && airlock.isActiveAndEnabled && airlock.IsConfigured) return;
        if (!CanUse || lastInteractionFrame == Time.frameCount) return;
        lastInteractionFrame = Time.frameCount;

        if (arrival)
        {
            PlaceNearbyPlayer();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError($"Add scene '{destinationScene}' to the Build Profiles scene list.", this);
            return;
        }

        sceneTransitionInProgress = true;
        SceneSpawn.PendingId = arrivalSpawnId;
        SceneManager.sceneLoaded += SceneFinishedLoading;
        if (SceneManager.LoadSceneAsync(destinationScene, LoadSceneMode.Single) == null)
            FinishTransition();
    }

    public bool CompleteAirlockTransition(Collider2D other)
    {
        if (!CanUse || !arrival || other.attachedRigidbody != nearbyPlayer) return false;
        PlaceNearbyPlayer();
        return true;
    }

    void PlaceNearbyPlayer()
    {
        blockedUntil = Time.unscaledTime + transitionCooldown;
        if (pairedDoor) pairedDoor.blockedUntil = blockedUntil;
        if (greenhouse) greenhouse.SetInside(entersInterior);
        arrival.PlacePlayer(nearbyPlayer);
    }

    private static void SceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        string spawnId = SceneSpawn.PendingId;
        FinishTransition();
        if (string.IsNullOrEmpty(spawnId)) return;

        foreach (var spawn in FindObjectsByType<SceneSpawn>())
        {
            if (spawn.gameObject.scene != scene || spawn.Id != spawnId) continue;
            foreach (var astronaut in FindObjectsByType<AstronautController>())
            {
                if (astronaut.gameObject.scene != scene || !astronaut.TryGetComponent<Rigidbody2D>(out var player)) continue;
                spawn.PlacePlayer(player);
                return;
            }
            Debug.LogError($"Scene '{scene.name}' needs an AstronautController and Rigidbody2D to use spawn '{spawnId}'.");
            return;
        }
        Debug.LogError($"Scene '{scene.name}' has no active spawn with ID '{spawnId}'.");
    }

    private static void FinishTransition()
    {
        sceneTransitionInProgress = false;
        SceneSpawn.PendingId = null;
        SceneManager.sceneLoaded -= SceneFinishedLoading;
    }
}
