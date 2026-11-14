using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Coordinates proximity, modal movement, and shared harvest totals; each bed owns its menu.
public sealed class GreenhouseInteractionUI : MonoBehaviour
{
    [SerializeField] MarsClock clock;
    [SerializeField] AstronautController movement;
    [SerializeField] Rigidbody2D player;
    [SerializeField] CropDefinition[] crops;
    [SerializeField] LayerMask obstacleMask = 1 << 6;
    public MarsClock Clock => clock;
    public bool IsInside { get; private set; }

    readonly HashSet<GrowingBed> candidates = new HashSet<GrowingBed>();
    readonly Dictionary<string, int> harvest = new Dictionary<string, int>();
    readonly RaycastHit2D[] obstruction = new RaycastHit2D[1];
    ContactFilter2D interactionFilter;
    GrowingBed focused, selected;
    public IReadOnlyList<CropDefinition> Crops => crops;
    bool movementWasEnabled;
    string harvestSummary = "Harvest: none";

    void Awake()
    {
        interactionFilter.SetLayerMask(obstacleMask);
        interactionFilter.useTriggers = false;
    }

    public bool IsPlayer(Collider2D other) => other.attachedRigidbody == player;
    public int HarvestCount(string cropId) => harvest.TryGetValue(cropId, out int count) ? count : 0;
    public void AddCandidate(GrowingBed bed) => candidates.Add(bed);
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
        candidates.Clear();
        SetFocus(null);
    }

    public void RecordHarvest(CropDefinition crop)
    {
        harvest[crop.Id] = HarvestCount(crop.Id) + crop.HarvestQuantity;
        harvestSummary = "Harvest:";
        foreach (var definition in crops)
            harvestSummary += $"  {definition.DisplayName} {HarvestCount(definition.Id)}";
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (selected)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
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
        if (focused && keyboard != null && keyboard.eKey.wasPressedThisFrame) Open(focused);
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
        selected = bed;
        bed.Menu.Open();
        movementWasEnabled = movement.enabled;
        movement.enabled = false;
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
    }

    void Close()
    {
        if (!selected) return;
        selected.Menu.Hide();
        selected = null;
        movement.enabled = movementWasEnabled;
    }

    void OnDisable()
    {
        Close();
        SetFocus(null);
    }

    void OnGUI()
    {
        // The project enables legacy input for the IMGUI crop menu.
        float scale = Mathf.Min(Screen.width / 900f, Screen.height / 650f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, height = Screen.height / scale;
        if (IsInside)
        {
            GUI.Box(new Rect(12, 12, width - 24, 52), "Greenhouse | WASD / arrows to move | E to interact | Esc to close");
            GUI.Label(new Rect(24, 38, width - 48, 24), harvestSummary);
            if (focused && !selected)
                GUI.Box(new Rect(width / 2 - 150, height - 56, 300, 34), focused.Progress.IsReady
                    ? "E: Harvest crop" : focused.Progress.IsPlanted ? "E: Inspect crop" : "E: Plant crop");
        }
        GUI.matrix = previous;
    }

    public void CloseMenu(GrowingBed bed)
    {
        if (selected == bed) Close();
    }
}
