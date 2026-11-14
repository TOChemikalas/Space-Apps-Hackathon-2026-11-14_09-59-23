using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GreenhouseCanvas;

// Each bed remembers its crop selection. One menu is built lazily for each used bed.
public sealed class GrowingBedMenu : MonoBehaviour
{
    private GrowingBed bed;
    private RectTransform overlay, content;
    private Button[] seedButtons;
    private readonly Button[] slotButtons = new Button[GrowingBed.SlotCount];
    private readonly Image[] slotCrops = new Image[GrowingBed.SlotCount];
    private readonly Text[] slotLabels = new Text[GrowingBed.SlotCount];
    private Button actionButton;
    private Text title, harvested, slotHeading, slotState, cropName, duration, yield, actionLabel, remaining;
    private Image plotPreview;
    private int selectedCrop, selectedSlot;

    private void Awake() => bed = GetComponent<GrowingBed>();

    public bool Open()
    {
        if (!bed || !bed.Greenhouse || !bed.Greenhouse.HudCanvas || bed.Greenhouse.Crops.Count == 0)
            return false;

        if (!overlay)
            Build(bed.Greenhouse.HudCanvas);

        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(true);
        CenterAndScale(overlay, content);
        Refresh();
        EventSystem.current?.SetSelectedGameObject(null);
        return true;
    }

    public void Hide()
    {
        if (overlay)
            overlay.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Hide();
        if (bed && bed.Greenhouse)
            bed.Greenhouse.CloseMenu(bed);
    }

    private void OnDestroy()
    {
        if (overlay)
            Destroy(overlay.gameObject);
    }

    private void LateUpdate()
    {
        if (overlay && overlay.gameObject.activeSelf)
        {
            CenterAndScale(overlay, content);
            Refresh();
        }
    }

    private void Build(Canvas canvas)
    {
        overlay = Overlay(canvas, bed.name + " menu");
        var shield = overlay.gameObject.AddComponent<Image>();
        shield.color = new Color(0, 0, 0, .65f);
        var artwork = Image(overlay, "Crop menu artwork", new Rect(0, 0, 1672, 941), Color.white);
        artwork.sprite = bed.Greenhouse.CropMenuArtwork;
        if (!artwork.sprite) artwork.color = Background;
        content = artwork.rectTransform;

        var titlePanel = Image(content, "Menu title", new Rect(23, 43, 600, 79), Background);
        title = Label(titlePanel.transform, "Title", new Rect(12, 0, 575, 79), "", 60, style: FontStyle.Bold);
        var totalPanel = Image(content, "Harvest total background", new Rect(1371, 55, 263, 50), Background);
        harvested = Label(totalPanel.transform, "Harvest total", new Rect(8, 0, 247, 50), "", 27, TextAnchor.MiddleCenter);

        Image(content, "Seed inventory", new Rect(16, 239, 359, 590), Background);
        var crops = bed.Greenhouse.Crops;
        seedButtons = new Button[crops.Count];
        float rowStep = Mathf.Min(140, 576f / crops.Count);
        for (int i = 0; i < crops.Count; i++)
        {
            int index = i;
            CropDefinition choice = crops[i];
            var row = Button(content, choice.DisplayName, new Rect(22, 249 + i * rowStep, 347, rowStep - 16), Panel,
                () => { selectedCrop = index; Refresh(); });
            seedButtons[i] = row;
            Preview(row.transform, "Seed preview", new Rect(10, 8, 150, rowStep - 32)).sprite = choice.SpriteAt(5);
            Label(row.transform, "Crop name", new Rect(175, 12, 167, 39), choice.DisplayName, 31, style: FontStyle.Bold);
            Label(row.transform, "Crop duration", new Rect(175, 55, 167, 42), $"{choice.EarthDays:0} Earth days", 23);
        }

        var summary = Image(content, "Selected bed background", new Rect(434, 193, 790, 39), Panel);
        Label(summary.transform, "Selected bed", new Rect(10, 0, 770, 39), "Managing " + bed.name, 26);
        BuildSlots(artwork.sprite);

        var detailsHeader = Image(content, "Plot heading background", new Rect(1296, 160, 347, 49), Panel);
        slotHeading = Label(detailsHeader.transform, "Slot heading", new Rect(10, 0, 334, 49), "", 32, style: FontStyle.Bold);
        Image(content, "Plot detail background", new Rect(1273, 224, 378, 603), Background);
        slotState = Label(content, "Slot status", new Rect(1295, 232, 329, 42), "", 27);
        plotPreview = Preview(content, "Plot crop preview", new Rect(1302, 285, 325, 225));
        cropName = Label(content, "Selected crop", new Rect(1286, 512, 350, 57), "", 36, TextAnchor.MiddleCenter, FontStyle.Bold);
        Image(content, "Details divider", new Rect(1293, 574, 334, 2), Border);
        duration = Label(content, "Duration", new Rect(1294, 587, 336, 46), "", 25);
        yield = Label(content, "Yield", new Rect(1294, 646, 336, 46), "", 25);
        actionButton = Button(content, "Plant or harvest", new Rect(1284, 728, 360, 78), Green, PlantOrHarvest);
        actionLabel = Label(actionButton.transform, "Action", new Rect(0, 0, 360, 78), "", 33, TextAnchor.MiddleCenter, FontStyle.Bold);
        var back = Button(content, "Back", new Rect(17, 861, 185, 62), Color.clear, () => bed.Greenhouse.CloseMenu(bed));
        if (!artwork.sprite)
            Label(back.transform, "Back label", new Rect(0, 0, 185, 62), "Back", 29, TextAnchor.MiddleCenter);
        var time = Image(content, "Time background", new Rect(225, 884, 780, 36), Background);
        remaining = Label(time.transform, "Time remaining", new Rect(9, 0, 762, 36), "", 24);
    }

    private void BuildSlots(Sprite artwork)
    {
        var area = Image(content, "Plant slots", new Rect(483, 394, 685, 407), Background).rectTransform;
        const float cellWidth = 214;
        for (int i = 0; i < GrowingBed.SlotCount; i++)
        {
            int index = i;
            var tile = Button(area, $"Slot {i + 1}", new Rect(7 + i % GrowingBed.SlotColumns * 227,
                7 + i / GrowingBed.SlotColumns * 202, cellWidth, 188), Border,
                () => { selectedSlot = index; Refresh(); });
            slotButtons[i] = tile;
            var soil = Image(tile.transform, "Bed surface", new Rect(4, 4, cellWidth - 8, 138), new Color(.20f, .14f, .10f));
            if (artwork)
            {
                var tray = Create(soil.transform, "Tray artwork", new Rect(0, 0, cellWidth - 8, 138)).gameObject.AddComponent<RawImage>();
                Rect source = artwork.textureRect;
                tray.texture = artwork.texture;
                // Sample the soil around the empty upper-right tray's baked-in label.
                tray.uvRect = new Rect((source.x + source.width * 968 / 1672) / tray.texture.width,
                    (source.y + source.height * 371 / 941) / tray.texture.height,
                    source.width * 147 / 1672 / tray.texture.width, source.height * 117 / 941 / tray.texture.height);
                tray.raycastTarget = false;
            }
            slotCrops[i] = Preview(tile.transform, "Growing crop", new Rect(13, 8, cellWidth - 26, 130));
            Image(tile.transform, "Status background", new Rect(4, 146, cellWidth - 8, 38), Panel);
            slotLabels[i] = Label(tile.transform, "Slot status", new Rect(8, 146, cellWidth - 16, 38), "", 23, TextAnchor.MiddleCenter);
        }
    }

    private void Refresh()
    {
        var slot = bed.GetSlot(selectedSlot);
        bool planted = slot.Progress.IsPlanted;
        bool ready = slot.Progress.IsReady;
        CropDefinition crop = planted ? slot.Crop : bed.Greenhouse.Crops[selectedCrop];
        string state = ready ? "Ready to harvest" : planted ? "Growing" : "Empty";
        int stage = planted ? slot.Progress.Stage : 5;
        Sprite preview = crop.SpriteAt(stage);

        title.text = planted ? "Manage crops" : "Plant crops";
        harvested.text = $"Harvested: {bed.Greenhouse.HarvestCount(crop.Id)}";
        slotHeading.text = $"Slot {selectedSlot + 1}";
        slotState.text = planted ? $"{state}  |  {slot.Progress.Fraction:P0}" : state;
        cropName.text = crop.DisplayName;
        plotPreview.sprite = preview;
        duration.text = $"Growth: {crop.EarthDays:0} Earth days";
        yield.text = $"Harvest yield: {crop.HarvestQuantity}";
        actionButton.interactable = !planted || ready;
        actionLabel.text = ready ? "Harvest" : planted ? "Growing…" : "Plant " + crop.DisplayName;
        actionLabel.color = actionButton.interactable ? Background : Cream;
        actionButton.targetGraphic.color = actionButton.interactable ? Green : Panel;
        double seconds = bed.RemainingSeconds(selectedSlot);
        remaining.text = ready ? "Ready to harvest" : !planted ? "Choose a crop for this slot"
            : double.IsInfinity(seconds) ? "Growth stopped by conditions" : $"Remaining: {seconds / 86400:0.00} Earth days";

        for (int i = 0; i < seedButtons.Length; i++)
        {
            seedButtons[i].interactable = !planted;
            seedButtons[i].targetGraphic.color = bed.Greenhouse.Crops[i] == crop ? Border : Background;
        }

        for (int i = 0; i < GrowingBed.SlotCount; i++)
        {
            var plant = bed.GetSlot(i);
            slotButtons[i].targetGraphic.color = i == selectedSlot ? Green : Border;
            slotCrops[i].sprite = plant.Sprite;
            slotCrops[i].enabled = slotCrops[i].sprite;
            string status = plant.Progress.IsReady ? "Ready" : plant.Progress.IsPlanted ? $"{plant.Progress.Fraction:P0}" : "Empty";
            slotLabels[i].text = $"{i + 1}  |  {status}";
            slotLabels[i].color = plant.Progress.IsReady ? Orange : Cream;
        }
    }

    private void PlantOrHarvest()
    {
        if (bed.GetSlot(selectedSlot).Progress.IsReady)
            bed.Harvest(selectedSlot);
        else
            bed.Plant(selectedSlot, bed.Greenhouse.Crops[selectedCrop]);
        Refresh();
    }

    private static Image Preview(Transform parent, string name, Rect area)
    {
        var image = Image(parent, name, area, Color.white);
        image.preserveAspect = true;
        return image;
    }
}
