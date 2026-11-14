using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GreenhouseCanvas;

// Uses the existing equipment input/modal pattern and Canvas. Owns presentation, never energy state.
public sealed class ElectricalControls : MonoBehaviour
{
    enum Screen { Overview, Solar, Battery, Allocation, Communications }
    const float Width = 2752, Height = 1536;
    static readonly string[] SystemNames = { "Water", "Oxygen", "Irrigation", "Cooling", "Heating", "Suit Repair Station", "Comms" };
    [SerializeField] EnergyController energy;
    [SerializeField] AstronautController movement;
    [SerializeField] Rigidbody2D player;
    [SerializeField] Canvas hudCanvas;
    [SerializeField] Sprite dayArtwork, nightArtwork, batteryArtwork, allocationArtwork;
    [SerializeField] Sprite communicationsDayArtwork, communicationsNightArtwork;
    [SerializeField] SatelliteAlignmentMenu satelliteMenu;
    [SerializeField] DishAlignmentModel dish = new DishAlignmentModel();
    public DishAlignmentModel Dish => dish;
    public bool IsSatelliteAligned => dish.IsLocked;
    public event System.Action SatelliteAligned;
    [SerializeField] LayerMask obstacleMask = 1 << 6;
    readonly HashSet<ElectricalInteractable> candidates = new HashSet<ElectricalInteractable>();
    readonly RaycastHit2D[] obstruction = new RaycastHit2D[1];
    readonly List<Sprite> slices = new List<Sprite>();
    readonly GameObject[] pages = new GameObject[3];
    readonly Text[] pointValues = new Text[PowerAllocation.SystemCount];
    readonly Image[] pointBars = new Image[PowerAllocation.SystemCount];
    readonly Button[] systemRows = new Button[PowerAllocation.SystemCount];
    readonly Button[] addButtons = new Button[PowerAllocation.SystemCount];
    readonly Button[] removeButtons = new Button[PowerAllocation.SystemCount];
    ContactFilter2D interactionFilter;
    InputAction interact;
    ElectricalInteractable focused, selected;
    Battery activeBattery;
    RectTransform overlay, content, regular, allocationPage, communicationsPage;
    Image communicationsBackdrop;
    Text communicationsSignal, communicationsLink;
    Text roverStatus, chargerButtonLabel;
    Button chargerButton;
    Image backdrop, chargeBar, generationBar;
    Text badge, context, generation, demand, solarPower, efficiency, irradiance;
    Text charge, capacity, remaining, batterySummary, unassigned;
    bool isOpen, movementWasEnabled, day;
    float nextRefresh;
    int selectedSystem;
    MissionSurvival survival;
    Button recyclerUpgrade;
    Text recyclerLabel;
    Screen screen, equipmentScreen;

    void Awake()
    {
        dish.Normalize();
        if (movement) survival = movement.GetComponent<MissionSurvival>();
        interactionFilter.SetLayerMask(obstacleMask);
        interactionFilter.useTriggers = false;
    }

    void OnEnable()
    {
        if (energy && energy.clock) energy.clock.Advanced += OnClockAdvanced;
    }

    void Start() => interact = InputSystem.actions?.FindAction("Player/Interact");
    public bool IsPlayer(Collider2D other) => player && other.attachedRigidbody == player;

    internal Battery GetColonyBattery(int index) =>
        energy && index >= 0 && index < energy.batteries.Length ? energy.batteries[index] : null;
    public void AddCandidate(ElectricalInteractable item) => candidates.Add(item);
    public void RemoveCandidate(ElectricalInteractable item)
    {
        candidates.Remove(item);
        if (focused == item) Focus(null);
        if (selected == item) Close();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (isOpen)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (satelliteMenu && satelliteMenu.IsOpen) satelliteMenu.Back();
                else Close();
            }
            return;
        }
        if (!movement || !player || !movement.isActiveAndEnabled) { Focus(null); return; }
        ElectricalInteractable closest = null;
        float distance = float.PositiveInfinity;
        foreach (var item in candidates)
        {
            if (!item || !item.isActiveAndEnabled || !item.InRange(player.position)) continue;
            Vector2 target = item.ClosestPoint(player.position);
            float candidateDistance = (target - player.position).sqrMagnitude;
            if (candidateDistance >= distance) continue;
            int hits = Physics2D.Linecast(player.position, target, interactionFilter, obstruction);
            if (hits > 0 && obstruction[0].collider != item.SolidBoundary) continue;
            closest = item;
            distance = candidateDistance;
        }
        Focus(closest);
        if (focused && interact?.WasPressedThisFrame() == true) Open(focused);
    }

    void Focus(ElectricalInteractable item)
    {
        if (focused == item) return;
        if (focused) focused.Highlight(false);
        focused = item;
        if (focused) focused.Highlight(true);
    }

    void Open(ElectricalInteractable item)
    {
        if (!energy || !energy.clock || !hudCanvas || (item.OpensBattery && !item.Battery)) return;
        if (!overlay) Build();
        selected = item;
        activeBattery = item.Battery;
        isOpen = true;
        movementWasEnabled = movement.enabled;
        movement.enabled = false;
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(true);
        UpdatePhase();
        Show(item.OpensBattery ? Screen.Battery : Screen.Overview);
        CenterAndScale(overlay, content);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (satelliteMenu) satelliteMenu.Hide();
        if (overlay) overlay.gameObject.SetActive(false);
        selected = null;
        activeBattery = null;
        if (movement) movement.enabled = movementWasEnabled;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void OnDisable()
    {
        if (energy && energy.clock) energy.clock.Advanced -= OnClockAdvanced;
        Close();
        Focus(null);
    }

    void OnDestroy()
    {
        if (overlay) Destroy(overlay.gameObject);
        foreach (var sprite in slices) Destroy(sprite);
    }

    void LateUpdate()
    {
        if (!isOpen) return;
        CenterAndScale(overlay, content);
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .2f;
        Refresh();
    }

    void OnClockAdvanced(double from, double to)
    {
        if (isOpen) UpdatePhase(); // Includes seeks and immediate sunrise/sunset changes.
    }

    void UpdatePhase()
    {
        float latitude = MarsSun.Latitude;
        foreach (var array in energy.arrays)
            if (array && array.isActiveAndEnabled) { latitude = array.latitude; break; }
        float season = energy.lighting ? energy.lighting.startingLs : 90;
        day = MarsSun.At(energy.clock.Seconds, season, latitude).elevation > 0;
        Sprite artwork = day ? dayArtwork : nightArtwork;
        if (backdrop && backdrop.sprite != artwork) backdrop.sprite = artwork;
        if (communicationsBackdrop)
            communicationsBackdrop.sprite = day ? communicationsDayArtwork : communicationsNightArtwork;
    }

    void Build()
    {
        overlay = Overlay(hudCanvas, "Electrical equipment menu");
        var shield = overlay.gameObject.AddComponent<Image>();
        shield.color = new Color(0, 0, 0, .75f);
        content = Create(overlay, "Electrical artwork layout", new Rect(0, 0, Width, Height));
        regular = Create(content, "Electrical screens", new Rect(0, 0, Width, Height));
        backdrop = Image(regular, "Clock-controlled day or night", new Rect(0, 0, Width, Height), Color.white);
        BuildOverview();
        BuildSolar();
        BuildBattery();
        Button(regular, "Solar array tab", new Rect(35, 390, 580, 214), Color.clear, () => Show(Screen.Solar));
        Button(regular, "Battery tab", new Rect(35, 628, 580, 216), Color.clear, () => Show(Screen.Battery));
        Button(regular, "Power distributor tab", new Rect(35, 868, 580, 218), Color.clear, () => Show(Screen.Overview));
        Button(regular, "Close electrical menu", new Rect(25, 1408, 310, 118), Color.clear, Close);
        badge = Field(regular, "Live available power", new Rect(2278, 78, 425, 94), 43);
        context = Field(regular, "Equipment identity", new Rect(790, 168, 1150, 35), 26);
        BuildAllocation();
        BuildCommunications();
        Action(regular, "Allocate power", new Rect(1280, 73, 470, 76), () => Show(Screen.Allocation), size: 40);
        Action(regular, "Communications", new Rect(1770, 73, 478, 76), () => Show(Screen.Communications), size: 40);
        overlay.gameObject.SetActive(false);
    }

    RectTransform Page(Screen kind, Sprite artwork)
    {
        var page = Create(regular, kind.ToString(), new Rect(0, 0, Width, Height));
        pages[(int)kind] = page.gameObject;
        // Keep the phase-dependent center window; use the correct static tab and reading panels.
        Picture(page, "Equipment tabs", new Rect(0, 235, 640, 1135), Crop(artwork, new Rect(0, 235, 640, 1135)));
        Picture(page, "Equipment readings", new Rect(2080, 235, 672, 1135), Crop(artwork, new Rect(2080, 235, 672, 1135)));
        return page;
    }

    void BuildOverview()
    {
        var page = Page(Screen.Overview, nightArtwork);
        generation = Field(page, "Generation kW", new Rect(2460, 918, 245, 70), 47);
        demand = Field(page, "Demand kW", new Rect(2460, 1032, 245, 70), 47);
        Action(page, "Allocate power", new Rect(2110, 1190, 600, 140), () => Show(Screen.Allocation));
    }

    void BuildSolar()
    {
        var page = Page(Screen.Solar, dayArtwork);
        solarPower = Field(page, "Generation kW", new Rect(2460, 728, 245, 70), 47);
        efficiency = Field(page, "Cell efficiency percent", new Rect(2460, 908, 245, 70), 45);
        irradiance = Field(page, "Irradiance W per square metre", new Rect(2452, 980, 253, 65), 39);
        Set(Field(page, "Temperature unavailable", new Rect(2452, 1053, 253, 65), 45), "—");
        generationBar = Meter(page, "Live generation meter");
        Action(page, "Generation table", new Rect(2110, 1190, 600, 140), () => Show(Screen.Overview));
    }

    void BuildBattery()
    {
        var page = Page(Screen.Battery, batteryArtwork);
        charge = Field(page, "Charge level percent", new Rect(2470, 907, 235, 70), 46);
        capacity = Field(page, "Capacity kWh", new Rect(2450, 980, 255, 65), 43);
        remaining = Field(page, "Time remaining", new Rect(2490, 1050, 215, 65), 38);
        chargeBar = Meter(page, "Live charge meter");
        batterySummary = Field(page, "Stored energy and bus power", new Rect(700, 1280, 1270, 100), 30);
        batterySummary.alignment = TextAnchor.MiddleCenter;
        Action(page, "Power allocation", new Rect(2110, 1190, 600, 140), () => Show(Screen.Allocation));
    }

    void BuildAllocation()
    {
        allocationPage = Create(content, "Power allocation", new Rect(0, 0, Width, Height));
        Picture(allocationPage, "Supplied allocation artwork", new Rect(0, 0, Width, Height), allocationArtwork);
        unassigned = Field(allocationPage, "Unassigned points, not kW", new Rect(2270, 78, 435, 94), 40);
        if (survival)
        {
            recyclerUpgrade = Button(allocationPage, "Upgrade water recycler", new Rect(1490, 74, 700, 93), Panel,
                () => { survival.UpgradeWaterRecycling(); RefreshAllocation(); });
            recyclerLabel = Label(recyclerUpgrade.transform, "Recycling efficiency", new Rect(0, 0, 700, 93), "",
                35, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
        Mask(allocationPage, "Replace baked allocation examples", new Rect(1688, 390, 940, 850));
        for (int i = 0; i < PowerAllocation.SystemCount; i++)
        {
            int index = i;
            float y = 400 + i * 120;
            var row = Button(allocationPage, SystemNames[i], new Rect(1705, y, 910, 108), Background,
                () => { selectedSystem = index; RefreshAllocation(); });
            systemRows[i] = row;
            Label(row.transform, "System name", new Rect(12, 0, 345, 108), SystemNames[i], 41, style: FontStyle.Bold);
            pointValues[i] = Label(row.transform, "Allocated points", new Rect(360, 0, 130, 108), "", 38, TextAnchor.MiddleRight);
            Image(row.transform, "Budget bar track", new Rect(510, 42, 190, 25), Border);
            pointBars[i] = Image(row.transform, "Share of point budget", new Rect(510, 42, 0, 25), Green);
            removeButtons[i] = Action(row.transform, "−", new Rect(725, 10, 78, 84), () => Change(index, false), Orange, 56);
            addButtons[i] = Action(row.transform, "+", new Rect(815, 10, 78, 84), () => Change(index, true), Orange, 56);
        }
        Action(allocationPage, "Back to electricity", new Rect(2115, 1240, 493, 123), ShowEquipment);
        Button(allocationPage, "Close allocation", new Rect(2530, 263, 92, 94), Color.clear, ShowEquipment);
        Button(allocationPage, "Allocation back", new Rect(25, 1408, 310, 118), Color.clear, ShowEquipment);
        var note = Field(allocationPage, "Allocation applies immediately", new Rect(1720, 1370, 900, 35), 24);
        Set(note, "Changes apply immediately. Points are separate from kW.");
        allocationPage.gameObject.SetActive(false);
    }

    void ShowEquipment() => Show(equipmentScreen);

    void Show(Screen value)
    {
        screen = value;
        if (value <= Screen.Battery) equipmentScreen = value;
        bool allocating = value == Screen.Allocation;
        regular.gameObject.SetActive(!allocating && value != Screen.Communications);
        allocationPage.gameObject.SetActive(allocating);
        communicationsPage.gameObject.SetActive(value == Screen.Communications);
        for (int i = 0; i < pages.Length; i++) pages[i].SetActive(!allocating && i == (int)value);
        Refresh();
    }

    void Change(int index, bool add)
    {
        if (add) energy.Allocation.Add((ElectricalSystem)index);
        else energy.Allocation.Remove((ElectricalSystem)index);
        RefreshAllocation();
    }

    void RefreshAllocation()
    {
        var allocation = energy.Allocation;
        Set(unassigned, $"{allocation.Unassigned} points unassigned");
        if (recyclerUpgrade)
        {
            recyclerUpgrade.gameObject.SetActive(selectedSystem == (int)ElectricalSystem.Water);
            bool canUpgrade = survival.WaterRecycling < .95f;
            Set(recyclerLabel, canUpgrade ? "Water recycler · 90% → 95%" : $"Water recycler · {survival.WaterRecycling:P0}");
            recyclerUpgrade.interactable = canUpgrade;
        }
        for (int i = 0; i < PowerAllocation.SystemCount; i++)
        {
            int points = allocation.Get((ElectricalSystem)i);
            Set(pointValues[i], $"{points} pts");
            systemRows[i].targetGraphic.color = i == selectedSystem ? new Color(.10f, .25f, .13f) : Background;
            pointBars[i].rectTransform.sizeDelta = new Vector2(allocation.Budget > 0 ? 190f * points / allocation.Budget : 0, 25);
            addButtons[i].gameObject.SetActive(i == selectedSystem);
            removeButtons[i].gameObject.SetActive(i == selectedSystem);
            addButtons[i].interactable = allocation.Unassigned > 0;
            removeButtons[i].interactable = points > 0;
        }
    }

    void Refresh()
    {
        if (screen == Screen.Communications) { RefreshCommunications(); return; }
        if (screen == Screen.Allocation) { RefreshAllocation(); return; }
        Set(badge, $"{Math.Max(0, energy.GenerationKW - energy.DemandKW):0.00} kW available");
        Set(context, screen == Screen.Battery ? (activeBattery ? activeBattery.name : "Battery bank / all units") : selected.name);
        Set(generation, $"{energy.GenerationKW:0.00} kW");
        Set(demand, $"{energy.DemandKW:0.00} kW");
        Set(solarPower, $"{energy.GenerationKW:0.00} kW");
        double area = 0, cellEfficiency = 0, irradianceTotal = 0, peak = 0;
        foreach (var array in energy.arrays)
        {
            if (!array || !array.isActiveAndEnabled) continue;
            double weight = Math.Max(0, array.area);
            area += weight;
            cellEfficiency += weight * array.cellEfficiency;
            irradianceTotal += weight * array.irradiance;
            peak += array.PowerAtElevation(90);
        }
        Set(efficiency, area > 0 ? $"{100 * cellEfficiency / area:0.0}%" : "—");
        Set(irradiance, area > 0 ? $"{irradianceTotal / area:0} W/m²" : "—");
        generationBar.rectTransform.sizeDelta = new Vector2(570 * (float)(peak > 0 ? Math.Clamp(energy.GenerationKW / peak, 0, 1) : 0), 28);
        var stats = new BatteryReadings();
        if (activeBattery) stats.Add(activeBattery);
        else foreach (var battery in energy.batteries) if (battery) stats.Add(battery);
        double fraction = stats.Capacity > 0 ? stats.Stored / stats.Capacity : 0;
        Set(charge, $"{fraction * 100:0.0}%");
        Set(capacity, $"{stats.Capacity:0.0} kWh");
        double hours = stats.Discharge > .001 ? stats.Usable / stats.Discharge : double.NaN;
        Set(remaining, double.IsNaN(hours) ? "—" : $"{Math.Floor(hours):0}h {Math.Floor((hours % 1) * 60):00}m");
        Set(batterySummary, $"Stored energy: {stats.Stored:0.00} kWh / {stats.Capacity:0.0} kWh\nCharging: {stats.Charge:0.00} kW   |   Discharging: {stats.Discharge:0.00} kW");
        chargeBar.rectTransform.sizeDelta = new Vector2(570 * (float)Math.Clamp(fraction, 0, 1), 28);
    }

    void BuildCommunications()
    {
        communicationsPage = Create(content, "Communications", new Rect(0, 0, Width, Height));
        communicationsBackdrop = Image(communicationsPage, "Supplied Communications artwork", new Rect(0, 0, Width, Height), Color.white);
        communicationsSignal = Field(communicationsPage, "Actual signal strength", new Rect(2115, 80, 580, 90), 44);
        communicationsLink = Field(communicationsPage, "Actual link state", new Rect(2480, 918, 225, 70), 42);
        Set(Field(communicationsPage, "Comms power source unavailable", new Rect(2480, 1032, 225, 70), 44), "—");
        Action(communicationsPage, "Align Satellite Dish", new Rect(2110, 1190, 600, 140), OpenAlignment, Green, 43);
        roverStatus = Field(communicationsPage, "Rover charging status", new Rect(680, 1140, 1220, 75), 38);
        roverStatus.alignment = TextAnchor.MiddleCenter;
        chargerButton = Button(communicationsPage, "Rover charger", new Rect(884, 1224, 820, 105), Panel, () =>
        {
            if (RoverVehicle.Instance) RoverVehicle.Instance.ToggleCharger();
            RefreshCommunications();
        });
        chargerButtonLabel = Label(chargerButton.transform, "Charger action", new Rect(0, 0, 820, 105), "",
            39, TextAnchor.MiddleCenter, FontStyle.Bold);
        Button(communicationsPage, "Back to electricity", new Rect(25, 1408, 310, 118), Color.clear, ShowEquipment);
        communicationsPage.gameObject.SetActive(false);
    }

    void OpenAlignment()
    {
        if (!satelliteMenu) return;
        communicationsPage.gameObject.SetActive(false);
        satelliteMenu.Open(hudCanvas, () => Show(Screen.Communications));
    }

    void RefreshCommunications()
    {
        Set(communicationsSignal, $"{dish.Signal * 100:0.0}% Signal strength");
        Set(communicationsLink, dish.IsLocked ? "Locked" : "Offline");
        communicationsSignal.color = Color.Lerp(Orange, Green, (float)dish.Signal);
        communicationsLink.color = dish.IsLocked ? Green : Orange;
        var rover = RoverVehicle.Instance;
        Set(roverStatus, rover ? $"Rover {rover.Charge:P0} · {rover.ChargerStatus}" : "Rover unavailable");
        Set(chargerButtonLabel, rover && rover.IsConnected ? "Disconnect rover charger" : "Start rover charging");
        chargerButton.interactable = rover && (rover.IsDocked || rover.IsConnected);
    }

    public void NotifySatelliteAligned()
    {
        if (!dish.IsLocked) return;
        if (communicationsSignal) RefreshCommunications();
        SatelliteAligned?.Invoke();
    }

    struct BatteryReadings
    {
        public double Capacity, Stored, Usable, Charge, Discharge;
        public void Add(Battery battery)
        {
            Capacity += battery.capacityKWh;
            Stored += battery.StoredKWh;
            Usable += battery.StoredKWh * Math.Clamp(battery.dischargeEfficiency, .01, 1);
            Charge += Math.Max(0, battery.BusPowerKW);
            Discharge += Math.Max(0, -battery.BusPowerKW);
        }
    }

    Image Meter(Transform parent, string name)
    {
        Mask(parent, name + " track", new Rect(2118, 817, 582, 44));
        return Image(parent, name, new Rect(2123, 823, 0, 28), Green);
    }

    Text Field(Transform parent, string name, Rect rect, int size)
    {
        var mask = Mask(parent, name + " mask", rect);
        return Label(mask.transform, name, new Rect(6, 0, rect.width - 12, rect.height), "", size, TextAnchor.MiddleRight);
    }

    Image Mask(Transform parent, string name, Rect rect)
    {
        return Image(parent, name, rect, Background);
    }

    static void Set(Text label, string value) { if (label.text != value) label.text = value; }
    static void Picture(Transform parent, string name, Rect rect, Sprite sprite) => Image(parent, name, rect, Color.white).sprite = sprite;
    static Button Action(Transform parent, string label, Rect rect, UnityEngine.Events.UnityAction action,
        Color? color = null, int size = 48)
    {
        var result = Button(parent, label, rect, color ?? Green, action);
        var text = Label(result.transform, label, new Rect(0, 0, rect.width, rect.height), label, size, TextAnchor.MiddleCenter, FontStyle.Bold);
        text.color = Background;
        return result;
    }

    Sprite Crop(Sprite source, Rect topRect)
    {
        Rect full = source.textureRect;
        float sx = full.width / Width, sy = full.height / Height;
        var rect = new Rect(full.x + topRect.x * sx, full.y + full.height - (topRect.y + topRect.height) * sy,
            topRect.width * sx, topRect.height * sy);
        Sprite sprite = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        slices.Add(sprite);
        return sprite;
    }
}
