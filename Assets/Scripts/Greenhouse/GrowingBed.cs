using System;
using UnityEngine;

// Owns six independent plant slots. Duplicated prefabs start with empty slots.
[RequireComponent(typeof(GrowingBedMenu))]
public sealed class GrowingBed : MonoBehaviour
{
    internal const int SlotColumns = 3;
    internal const int SlotRows = 2;
    internal const int SlotCount = SlotColumns * SlotRows;

    internal sealed class PlantSlot
    {
        public CropDefinition Crop { get; private set; }
        public CropProgress Progress { get; } = new CropProgress();
        public Sprite Sprite => Crop ? Crop.SpriteAt(Progress.Stage) : null;

        public bool Plant(CropDefinition crop)
        {
            if (!crop || !crop.IsValid || !Progress.Plant(crop.DurationSeconds)) return false;
            Crop = crop;
            return true;
        }

        public CropDefinition Harvest()
        {
            if (!Progress.Harvest()) return null;
            CropDefinition harvested = Crop;
            Crop = null;
            return harvested;
        }
    }

    [SerializeField] string bedId;
    [SerializeField] BoxCollider2D solidBoundary;
    [SerializeField] SpriteRenderer cropRenderer;
    [SerializeField] Vector2 cropVisualSize = new Vector2(5.4f, 1.9f);
    [SerializeField, Min(0)] float growthMultiplier = 1;
    [SerializeField] CropDefinition starterCrop;
    [SerializeField, Min(0)] int starterHarvestSol;
    public float EnvironmentMultiplier { get; set; } = 1;
    public string Id => bedId;
    public BoxCollider2D Boundary => solidBoundary;
    public GreenhouseInteractionUI Greenhouse { get; private set; }
    public GrowingBedMenu Menu { get; private set; }

    MarsClock growthClock;
    double lastGrowthIntegral;
    LineRenderer outline;
    Material outlineMaterial;
    readonly PlantSlot[] slots = new PlantSlot[SlotCount];
    readonly SpriteRenderer[] slotRenderers = new SpriteRenderer[SlotCount];

    internal PlantSlot GetSlot(int index) => slots[index];

    internal double RemainingSeconds(int index)
    {
        var progress = slots[index].Progress;
        float multiplier = growthMultiplier * EnvironmentMultiplier;
        var economy = Greenhouse.Survival?.Economy;
        if (economy != null) multiplier *= (float)(economy.Irrigation * economy.CropCondition * Math.Clamp(economy.TemperatureC / 20, 0, 1));
        return multiplier > 0 ? (progress.DurationSeconds - progress.ElapsedSeconds) / multiplier : double.PositiveInfinity;
    }

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
        CreateSlots();
    }

    void CreateSlots()
    {
        Vector3 center = cropRenderer.transform.localPosition;
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = new PlantSlot();
            SpriteRenderer renderer = i == 0 ? cropRenderer : Instantiate(cropRenderer, cropRenderer.transform.parent);
            renderer.name = $"Crop slot {i + 1}";
            renderer.sprite = null;
            renderer.transform.localPosition = center + new Vector3(
                (i % SlotColumns + .5f - SlotColumns * .5f) * cropVisualSize.x / SlotColumns,
                (SlotRows * .5f - i / SlotColumns - .5f) * cropVisualSize.y / SlotRows, 0);
            slotRenderers[i] = renderer;
        }
    }

    void OnEnable()
    {
        if (growthClock || !Greenhouse || !Greenhouse.Clock) return;
        growthClock = Greenhouse.Clock;
        // The colony clock continues advancing plants while their room is inactive.
        growthClock.Advanced += OnAdvanced;
        lastGrowthIntegral = Greenhouse.Survival?.Economy?.GrowthIntegralSeconds ?? 0;
        if (starterCrop && starterHarvestSol > 0 && Greenhouse.Survival)
        {
            double remaining = Greenhouse.Survival.TimeUntilStarterHarvest(starterHarvestSol);
            double multiplier = growthMultiplier * EnvironmentMultiplier;
            double elapsed = Math.Max(0, starterCrop.DurationSeconds - remaining * multiplier);
            for (int i = 0; i < SlotCount; i++)
            {
                if (!slots[i].Plant(starterCrop)) continue;
                slots[i].Progress.Advance(0, elapsed, 1);
                RefreshSprite(i);
            }
        }
    }

    void OnDisable()
    {
        if (Greenhouse) Greenhouse.RemoveCandidate(this);
        if (outline) outline.enabled = false;
    }

    void OnDestroy()
    {
        if (growthClock) growthClock.Advanced -= OnAdvanced;
        if (outlineMaterial) Destroy(outlineMaterial);
    }

    void OnAdvanced(double from, double to)
    {
        float multiplier = growthMultiplier * EnvironmentMultiplier;
        var economy = Greenhouse.Survival?.Economy;
        if (economy != null)
        {
            double elapsed = Math.Max(0, economy.GrowthIntegralSeconds - lastGrowthIntegral);
            lastGrowthIntegral = economy.GrowthIntegralSeconds;
            from = 0;
            to = elapsed;
        }
        for (int i = 0; i < SlotCount; i++)
        {
            PlantSlot slot = slots[i];
            int stage = slot.Progress.Stage;
            slot.Progress.Advance(from, to, multiplier);
            if (slot.Progress.Stage != stage) RefreshSprite(i);
        }
    }

    internal bool Plant(int index, CropDefinition crop)
    {
        if (!slots[index].Plant(crop)) return false;
        RefreshSprite(index);
        return true;
    }

    internal bool Harvest(int index)
    {
        if (!Greenhouse) return false;
        CropDefinition harvested = slots[index].Harvest();
        if (!harvested) return false;
        Greenhouse.RecordHarvest(harvested);
        RefreshSprite(index);
        return true;
    }

    public void Highlight(bool visible) => outline.enabled = visible;

    void RefreshSprite(int index)
    {
        SpriteRenderer renderer = slotRenderers[index];
        Sprite sprite = slots[index].Sprite;
        renderer.sprite = sprite;
        if (!sprite) return;
        Vector2 size = sprite.bounds.size;
        float scale = Mathf.Min(cropVisualSize.x / (SlotColumns * size.x), cropVisualSize.y / (SlotRows * size.y));
        renderer.transform.localScale = new Vector3(scale, scale, 1);
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
