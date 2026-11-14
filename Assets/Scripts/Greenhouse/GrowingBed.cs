using System;
using UnityEngine;

// Owns its crop state. Duplicated prefab instances bind to their greenhouse once.
[RequireComponent(typeof(GrowingBedMenu))]
public sealed class GrowingBed : MonoBehaviour
{
    [SerializeField] string bedId;
    [SerializeField] BoxCollider2D solidBoundary;
    [SerializeField] SpriteRenderer cropRenderer;
    [SerializeField] Vector2 cropVisualSize = new Vector2(5.4f, 1.9f);
    [SerializeField, Min(0)] float growthMultiplier = 1;
    public float EnvironmentMultiplier { get; set; } = 1;
    public string Id => bedId;
    public CropDefinition Crop { get; private set; }
    public CropProgress Progress { get; } = new CropProgress();
    public double RemainingSeconds => growthMultiplier * EnvironmentMultiplier > 0
        ? (Progress.DurationSeconds - Progress.ElapsedSeconds) / (growthMultiplier * EnvironmentMultiplier)
        : double.PositiveInfinity;
    public BoxCollider2D Boundary => solidBoundary;
    public GreenhouseInteractionUI Greenhouse { get; private set; }
    public GrowingBedMenu Menu { get; private set; }

    LineRenderer outline;
    Material outlineMaterial;
    int shownStage = -2;

    void Awake()
    {
        // Runtime identity stays unique when a configured prefab is duplicated.
        bedId = Guid.NewGuid().ToString("N");
        Greenhouse = GetComponentInParent<GreenhouseInteractionUI>();
        Menu = GetComponent<GrowingBedMenu>();
        outline = gameObject.AddComponent<LineRenderer>();
        outlineMaterial = new Material(Shader.Find("Sprites/Default"));
        outline.sharedMaterial = outlineMaterial;
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 4;
        outline.startWidth = outline.endWidth = .045f;
        outline.startColor = outline.endColor = Color.white;
        outline.sortingLayerName = "Effects";
        outline.sortingOrder = 50;
        Vector2 half = solidBoundary.size * .5f;
        Vector2 center = solidBoundary.offset;
        outline.SetPosition(0, new Vector3(center.x - half.x, center.y - half.y, -.1f));
        outline.SetPosition(1, new Vector3(center.x + half.x, center.y - half.y, -.1f));
        outline.SetPosition(2, new Vector3(center.x + half.x, center.y + half.y, -.1f));
        outline.SetPosition(3, new Vector3(center.x - half.x, center.y + half.y, -.1f));
        outline.enabled = false;
        RefreshSprite();
    }

    void OnEnable()
    {
        if (Greenhouse && Greenhouse.Clock) Greenhouse.Clock.Advanced += OnAdvanced;
    }

    void OnDisable()
    {
        if (Greenhouse && Greenhouse.Clock) Greenhouse.Clock.Advanced -= OnAdvanced;
        if (Greenhouse) Greenhouse.RemoveCandidate(this);
        if (outline) outline.enabled = false;
    }

    void OnDestroy()
    {
        if (outlineMaterial) Destroy(outlineMaterial);
    }

    void OnAdvanced(double from, double to)
    {
        Progress.Advance(from, to, growthMultiplier * EnvironmentMultiplier);
        RefreshSprite();
    }

    public bool Plant(CropDefinition crop)
    {
        if (!crop || !crop.IsValid || !Progress.Plant(crop.DurationSeconds)) return false;
        Crop = crop;
        RefreshSprite();
        return true;
    }

    public bool Harvest()
    {
        if (!Greenhouse || !Progress.Harvest()) return false;
        Greenhouse.RecordHarvest(Crop);
        Crop = null;
        RefreshSprite();
        return true;
    }

    public void Highlight(bool visible) => outline.enabled = visible;

    void RefreshSprite()
    {
        int stage = Progress.Stage;
        if (stage == shownStage) return;
        shownStage = stage;
        cropRenderer.sprite = Crop ? Crop.SpriteAt(stage) : null;
        if (!cropRenderer.sprite) return;
        Vector2 size = cropRenderer.sprite.bounds.size;
        float scale = Mathf.Min(cropVisualSize.x / size.x, cropVisualSize.y / size.y);
        cropRenderer.transform.localScale = new Vector3(scale, scale, 1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Greenhouse && Greenhouse.IsPlayer(other)) Greenhouse.AddCandidate(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (Greenhouse && Greenhouse.IsPlayer(other)) Greenhouse.RemoveCandidate(this);
    }
}
