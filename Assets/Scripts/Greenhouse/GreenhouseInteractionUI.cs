using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GreenhouseCanvas;

// Coordinates proximity, modal movement, and shared harvest totals; each bed owns its menu.
public sealed class GreenhouseInteractionUI : MonoBehaviour
{
    [SerializeField] MarsClock clock;
    [SerializeField] AstronautController movement;
    [SerializeField] Rigidbody2D player;
    [SerializeField] Canvas hudCanvas;
    [SerializeField] Sprite cropMenuArtwork;
    [SerializeField] CropDefinition[] crops;
    [SerializeField] LayerMask obstacleMask = 1 << 6;
    public MarsClock Clock => clock;
    internal MissionSurvival Survival { get; private set; }
    public Canvas HudCanvas => hudCanvas;
    public Sprite CropMenuArtwork => cropMenuArtwork;
    public bool IsInside { get; private set; }

    readonly HashSet<GrowingBed> candidates = new HashSet<GrowingBed>();
    readonly Dictionary<string, int> harvest = new Dictionary<string, int>();
    readonly RaycastHit2D[] obstruction = new RaycastHit2D[1];
    ContactFilter2D interactionFilter;
    GrowingBed focused, selected;
    BuildingPortal nearbyDoor;
    InputAction interact;
    public IReadOnlyList<CropDefinition> Crops => crops;
    bool movementWasEnabled;
    string harvestSummary = "Harvest: none";
    RectTransform hud, hudContent;
    Text harvestLabel, prompt;
    GameObject statusPanel;

    public void Bind(MarsClock colonyClock, Rigidbody2D body, Canvas canvas)
    {
        clock = colonyClock;
        player = body;
        movement = body.GetComponent<AstronautController>();
        Survival = body.GetComponent<MissionSurvival>();
        if (Survival) Survival.SetGreenhouseSupply(IsInside);
        hudCanvas = canvas;
    }

    void Awake()
    {
        interactionFilter.SetLayerMask(obstacleMask);
        interactionFilter.useTriggers = false;
    }

    void Start()
    {
        interact = InputSystem.actions?.FindAction("Player/Interact");
        if (!hudCanvas) return;

        hud = Overlay(hudCanvas, "Greenhouse HUD");
        hudContent = Create(hud, "Status and prompts", new Rect(0, 0, 900, 650));
        var statusBackground = Image(hudContent, "Greenhouse status", new Rect(12, 12, 876, 59), Panel);
        statusPanel = statusBackground.gameObject;
        Label(statusBackground.transform, "Controls", new Rect(12, 3, 850, 25), "Greenhouse   |   WASD / arrows to move   |   E to interact   |   Esc to close", 19);
        harvestLabel = Label(statusBackground.transform, "Harvest ledger", new Rect(12, 27, 850, 24), harvestSummary, 18);
        var promptBackground = Image(hudContent, "Interaction prompt", new Rect(300, 595, 300, 40), Panel);
        prompt = Label(promptBackground.transform, "Prompt", new Rect(0, 0, 300, 40), "", 20, TextAnchor.MiddleCenter);
        hud.gameObject.SetActive(IsInside);
    }

    public bool IsPlayer(Collider2D other) => player && other.attachedRigidbody == player;
    public int HarvestCount(string cropId) => harvest.TryGetValue(cropId, out int count) ? count : 0;
    public void AddCandidate(GrowingBed bed) => candidates.Add(bed);
    public void DoorEntered(BuildingPortal door) => nearbyDoor = door;
    public void DoorExited(BuildingPortal door)
    {
        if (nearbyDoor == door) nearbyDoor = null;
    }

    public void RemoveCandidate(GrowingBed bed)
    {
        candidates.Remove(bed);
        if (focused == bed) SetFocus(null);
        if (selected == bed) Close();
    }

    public void SetInside(bool inside)
    {
        Close();
        IsInside = inside;
        if (Survival) Survival.SetGreenhouseSupply(inside);
        candidates.Clear();
        SetFocus(null);
    }

    public void RecordHarvest(CropDefinition crop)
    {
        harvest[crop.Id] = HarvestCount(crop.Id) + crop.HarvestQuantity;
        if (Survival) Survival.AddHarvest(crop.HarvestQuantity);
        harvestSummary = "Harvest:";
        foreach (var definition in crops)
            harvestSummary += $"  {definition.DisplayName} {HarvestCount(definition.Id)}";
        if (harvestLabel) harvestLabel.text = harvestSummary;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (selected)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
            return;
        }

        bool pressed = interact?.WasPressedThisFrame() == true;
        if (nearbyDoor && nearbyDoor.CanUse)
        {
            SetFocus(null);
            if (pressed) nearbyDoor.Interact();
            return;
        }

        if (!IsInside) return;
        GrowingBed nearest = null;
        float bestDistance = float.PositiveInfinity;
        foreach (var bed in candidates)
        {
            if (!bed || !bed.isActiveAndEnabled) continue;
            Vector2 target = bed.Boundary.ClosestPoint(player.position);
            float distance = (target - player.position).sqrMagnitude;
            if (distance >= bestDistance) continue;
            int hits = Physics2D.Linecast(player.position, target, interactionFilter, obstruction);
            if (hits > 0 && obstruction[0].collider != bed.Boundary) continue;
            nearest = bed;
            bestDistance = distance;
        }
        SetFocus(nearest);
        if (focused && pressed) Open(focused);
    }

    void SetFocus(GrowingBed bed)
    {
        if (focused == bed) return;
        if (focused) focused.Highlight(false);
        focused = bed;
        if (focused) focused.Highlight(true);
    }

    void Open(GrowingBed bed)
    {
        if (!bed.Menu.Open()) return;
        selected = bed;
        movementWasEnabled = movement.enabled;
        movement.enabled = false;
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
    }

    void Close()
    {
        if (!selected) return;
        selected.Menu.Hide();
        EventSystem.current?.SetSelectedGameObject(null);
        selected = null;
        movement.enabled = movementWasEnabled;
    }

    void OnDisable()
    {
        SetInside(false);
        if (hud) hud.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (hud) Destroy(hud.gameObject);
    }

    void LateUpdate()
    {
        if (!hud) return;

        bool showDoor = !selected && nearbyDoor && nearbyDoor.CanUse;
        bool showBed = !selected && focused;
        hud.gameObject.SetActive(IsInside || showDoor);
        if (!hud.gameObject.activeSelf) return;

        CenterAndScale(hud, hudContent);
        statusPanel.SetActive(IsInside);
        prompt.transform.parent.gameObject.SetActive(showDoor || showBed);
        if (showDoor)
            prompt.text = nearbyDoor.Prompt;
        else if (showBed)
            prompt.text = "E: Open " + focused.name;
    }

    public void CloseMenu(GrowingBed bed)
    {
        if (selected == bed) Close();
    }
}
