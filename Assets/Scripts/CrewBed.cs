using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GreenhouseCanvas;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class CrewBed : MonoBehaviour
{
    [SerializeField] private MarsClock clock;
    [SerializeField] private AstronautController astronaut;
    [SerializeField] private Transform astronautVisual;
    [SerializeField] private Canvas canvas;
    [SerializeField, Min(0f)] private float reach = .85f;

    private const double WakeHour = 8d;
    private BoxCollider2D bed;
    private Rigidbody2D player;
    private InputAction interact;
    private Image prompt;
    private Text status;
    private Coroutine sleep;
    private bool lyingDown;
    private Vector2 standingPosition;
    private Quaternion standingRotation;
    private bool clockWasPaused;

    public void Bind(MarsClock colonyClock, AstronautController playerController, Canvas hud)
    {
        clock = colonyClock;
        astronaut = playerController;
        astronautVisual = playerController.Visual;
        canvas = hud;
    }

    private void Start()
    {
        if (!clock || !astronaut || !astronautVisual || !canvas) return;
        bed = GetComponent<BoxCollider2D>();
        player = astronaut.GetComponent<Rigidbody2D>();
        interact = InputSystem.actions?.FindAction("Player/Interact");
        prompt = Image(canvas.transform, "Sleep prompt", new Rect(0, 0, 256, 40), Panel);
        RectTransform rect = prompt.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 26);
        status = Label(rect, "Sleep status", new Rect(0, 0, 256, 40), "", 18, TextAnchor.MiddleCenter);
        prompt.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!prompt || !player || sleep != null) return;
        bool near = astronaut.isActiveAndEnabled && (player.position - bed.ClosestPoint(player.position)).sqrMagnitude <= reach * reach;
        prompt.gameObject.SetActive(near);
        if (!near) return;
        status.text = "E · Sleep until 08:00";
        if (interact?.WasPressedThisFrame() == true) sleep = StartCoroutine(Sleep());
    }

    private IEnumerator Sleep()
    {
        standingPosition = player.position;
        standingRotation = astronautVisual.localRotation;
        lyingDown = true;
        clockWasPaused = clock.paused;
        clock.paused = true;
        astronaut.enabled = false;
        player.linearVelocity = Vector2.zero;
        player.simulated = false;
        MovePlayer(bed.bounds.center);
        astronautVisual.localRotation = standingRotation * Quaternion.Euler(0f, 0f, -12f);
        status.text = "Sleeping…";

        yield return new WaitForSecondsRealtime(1.25f);
        double hours = WakeHour - clock.Hour;
        if (hours <= 1e-8) hours += 24d;
        // Advance the clock so batteries and crops also process the time spent asleep.
        clock.Advance(hours / 24d * MarsClock.SecondsPerSol);
        Wake();
        status.text = "Morning · 08:00";
        yield return new WaitForSecondsRealtime(.6f);
        sleep = null;
    }

    private void MovePlayer(Vector2 position)
    {
        player.position = position;
        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
        Physics2D.SyncTransforms();
    }

    private void Wake()
    {
        lyingDown = false;
        MovePlayer(standingPosition);
        astronautVisual.localRotation = standingRotation;
        player.simulated = true;
        astronaut.enabled = true;
        clock.paused = clockWasPaused;
    }

    private void OnDisable()
    {
        if (sleep != null)
        {
            StopCoroutine(sleep);
            sleep = null;
            if (lyingDown && player && astronaut && astronautVisual && clock) Wake();
        }
        if (prompt) prompt.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (prompt) Destroy(prompt.gameObject);
    }
}
