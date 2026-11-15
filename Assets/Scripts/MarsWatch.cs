using UnityEngine;
using UnityEngine.UI;
using static GreenhouseCanvas;

public sealed class MarsWatch : MonoBehaviour
{
    [SerializeField] private MarsClock clock;
    [SerializeField] private Canvas canvas;
    [SerializeField] private EnergyController energy;
    [SerializeField] private MissionSurvival survival;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform astronaut;
    [SerializeField] private Sprite oxygenIcon, waterIcon, foodIcon, powerIcon;
    [SerializeField] private CommunicationsSession communications;
    [SerializeField] private Texture2D notificationArtwork;
    private GameObject messageNotification;
    private RectTransform crashArrow;
    private Text crashDistance;
    private Sprite notificationSprite;

    private const int Width = 218;
    private const int MapSize = 208;
    private const int Segments = 8;
    private static readonly Color32 WarningRed = new Color32(255, 79, 79, 255);
    private static readonly Color32 Amber = new Color32(255, 172, 52, 255);
    private static readonly Color32 Metal = new Color32(124, 139, 150, 255);
    private static readonly Color32 Face = new Color32(22, 26, 30, 255);
    private static readonly Color32 Edge = new Color32(52, 63, 71, 255);
    private static readonly Color32 Shadow = new Color32(8, 10, 13, 255);

    private RectTransform status, map;
    private Text readout;
    private Meter oxygen, water, food, power;

    private sealed class Meter
    {
        public Text Readout;
        public Image[] Segments;
        public Color Tint;

        public void Set(float value, bool warning = false)
        {
            int percent = value > 0 ? Mathf.Max(1, Mathf.RoundToInt(value * 100)) : 0;
            Readout.text = percent + "%";
            Readout.color = warning ? WarningRed : Cream;
            Color tint = warning ? WarningRed : Tint;
            for (int i = 0; i < Segments.Length; i++)
                Segments[i].color = Color.Lerp(Edge, tint, Mathf.Clamp01(value * Segments.Length - i));
        }
    }
    private Sprite clockFrame, rowFrame, mapRing, mapDisc;
    private Camera mapCamera;
    private RenderTexture mapTexture;
    private int lastSol, lastMinute = -1;

    private void Start()
    {
        if (!clock || !canvas) return;

        clockFrame = MakeSprite("Clock frame", Width, 48, (x, y) => PlatePixel(x, y, 48));
        rowFrame = MakeSprite("Status frame", Width, 40, (x, y) => PlatePixel(x, y, 40));
        status = Create(canvas.transform, "Mars HUD", new Rect(12, 12, Width, 240));
        PanelImage(status, "Clock frame", clockFrame, new Rect(0, 0, Width, 48));
        Image(status, "Clock accent", new Rect(11, 9, 6, 30), Amber);
        readout = Label(status, "Local time", new Rect(25, 0, 181, 48), "", 23,
            TextAnchor.MiddleLeft, FontStyle.Bold);

        oxygen = AddMeter("Oxygen", oxygenIcon, new Color32(96, 196, 255, 255), 55);
        water = AddMeter("Water", waterIcon, new Color32(67, 215, 240, 255), 100);
        food = AddMeter("Food", foodIcon, new Color32(250, 157, 54, 255), 145);
        power = AddMeter("Power", powerIcon, Amber, 190);
        if (worldCamera && astronaut) BuildMap();
        if (notificationArtwork)
        {
            notificationSprite = Sprite.Create(notificationArtwork, new Rect(0, 0, notificationArtwork.width, notificationArtwork.height), new Vector2(.5f, .5f));
            var icon = Image(status, "Unread communications notification", new Rect(180, 242, 34, 34), Color.white);
            icon.sprite = notificationSprite;
            icon.preserveAspect = true;
            messageNotification = icon.gameObject;
            messageNotification.SetActive(false);
        }
    }

    private Meter AddMeter(string title, Sprite icon, Color tint, int top)
    {
        var row = PanelImage(status, title + " status", rowFrame, new Rect(0, top, Width, 40));
        var symbol = Image(row.transform, title + " icon", new Rect(9, 3, 35, 34), Color.white);
        symbol.sprite = icon;
        symbol.preserveAspect = true;
        Image(row.transform, title + " meter track", new Rect(53, 10, 101, 20), Shadow);
        var segments = new Image[Segments];
        for (int i = 0; i < Segments; i++)
            segments[i] = Image(row.transform, title + " segment", new Rect(55 + 12 * i, 12, 10, 16), Color.Lerp(Shadow, tint, .27f));
        var value = Label(row.transform, title + " reading", new Rect(158, 0, 55, 40), "—",
            17, TextAnchor.MiddleCenter, FontStyle.Bold);
        return new Meter { Readout = value, Segments = segments, Tint = tint };
    }

    private void BuildMap()
    {
        map = Create(canvas.transform, "Mars minimap", new Rect(0, 0, MapSize, MapSize + 8));
        map.anchorMin = map.anchorMax = map.pivot = Vector2.one;
        map.anchoredPosition = new Vector2(-12, -12);
        mapRing = MakeSprite("Map bezel", MapSize, MapSize, MapRingPixel);
        mapDisc = MakeSprite("Map mask", MapSize, MapSize, (x, y) => Radius(x, y) <= 97 ? Color.white : Color.clear);
        var window = PanelImage(map, "Circular map window", mapDisc, new Rect(0, 8, MapSize, MapSize));
        window.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        mapTexture = new RenderTexture(256, 256, 24) { name = "Mars minimap", filterMode = FilterMode.Point };
        var view = Create(window.transform, "Live world map", new Rect(0, 0, MapSize, MapSize)).gameObject.AddComponent<RawImage>();
        view.texture = mapTexture;
        view.raycastTarget = false;
        PanelImage(map, "Metal bezel", mapRing, new Rect(0, 8, MapSize, MapSize));
        for (int i = 0; i < 4; i++)
        {
            bool horizontal = i < 2;
            var tick = horizontal
                ? new Rect(i == 0 ? 4 : MapSize - 16, 110, 12, 3)
                : new Rect(102, i == 2 ? 12 : MapSize - 3, 4, 12);
            Image(map, "Compass tick", tick, Cream);
        }
        var north = Image(map, "North badge", new Rect(88, 0, 32, 25), Shadow);
        Label(north.transform, "North", new Rect(0, 0, 32, 25), "N", 17, TextAnchor.MiddleCenter, FontStyle.Bold);
        Label(map, "Player marker", new Rect(91, 99, 26, 26), "▲", 19, TextAnchor.MiddleCenter, FontStyle.Bold).color = Amber;

        mapCamera = new GameObject("Minimap Camera").AddComponent<Camera>();
        mapCamera.CopyFrom(worldCamera);
        mapCamera.cullingMask &= ~LayerMask.GetMask(MarsDustStormVisuals.EffectsLayer);
        mapCamera.allowHDR = false;
        mapCamera.allowMSAA = false;
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 14;
        mapCamera.rect = new Rect(0, 0, 1, 1);
        mapCamera.targetTexture = mapTexture;
        mapCamera.aspect = 1;
        mapCamera.depth = worldCamera.depth - 1;
        var arrow = Label(map, "Crash site direction", new Rect(0, 0, 24, 24), "▲", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        arrow.color = WarningRed;
        arrow.raycastTarget = false;
        crashArrow = arrow.rectTransform;
        crashArrow.pivot = new Vector2(.5f, .5f);
        crashArrow.gameObject.SetActive(false);
        crashDistance = Label(map, "Crash site distance", new Rect(0, 220, MapSize, 26), "", 16, TextAnchor.MiddleCenter);
        crashDistance.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!readout) return;
        var size = ((RectTransform)canvas.transform).rect.size;
        float scale = Mathf.Min(size.y / 600f, size.x / 530f);
        status.localScale = new Vector3(scale, scale, 1);
        if (map)
        {
            map.localScale = status.localScale;
            Vector3 position = RoverVehicle.Instance && RoverVehicle.Instance.IsOccupied ? RoverVehicle.Instance.transform.position : astronaut.position;
            mapCamera.transform.position = new Vector3(position.x, position.y, worldCamera.transform.position.z);
        }
        UpdateDiscovery();

        int minute = Mathf.Clamp((int)(clock.Hour * 60d + 1e-8), 0, 1439);
        int sol = clock.Sol;
        if (lastMinute != minute || lastSol != sol)
        {
            lastMinute = minute;
            lastSol = sol;
            readout.text = $"{minute / 60:00}:{minute % 60:00}  ·  Sol {sol}";
        }

        if (survival)
        {
            const float critical = .09f;
            bool lowOxygen = survival.Oxygen < critical;
            bool lowWater = survival.Hydration < critical;
            bool lowFood = survival.Nutrition < critical;
            oxygen.Set(survival.Oxygen, lowOxygen);
            water.Set(survival.Hydration, lowWater);
            food.Set(survival.Nutrition, lowFood);
        }
        if (energy && energy.TryGetBatteryCharge(out float charge)) power.Set(charge, charge < .2f);
    }

    private static Image PanelImage(Transform parent, string label, Sprite sprite, Rect rect)
    {
        var image = Image(parent, label, rect, Color.white);
        image.sprite = sprite;
        return image;
    }

    private void UpdateDiscovery()
    {
        bool unread = false, discovered = false;
        if (communications && communications.Model != null)
            foreach (var message in communications.Model.State.messages)
            {
                if (message.status != DeliveryStatus.Received) continue;
                unread |= !message.read;
                if (message.id == DroneCrashSite.MessageId) discovered = message.read;
            }
        if (messageNotification) messageNotification.SetActive(unread);
        if (!crashArrow) return;
        var site = DroneCrashSite.Instance;
        bool show = discovered && site;
        crashArrow.gameObject.SetActive(show);
        crashDistance.gameObject.SetActive(show);
        if (!show) return;
        Vector3 origin = survival && survival.Economy != null && !survival.Economy.InRover && (survival.Economy.InHabitat || survival.Economy.InGreenhouse)
            ? site.BasePosition : mapCamera.transform.position;
        Vector2 offset = site.transform.position - origin;
        Vector2 direction = mapCamera.transform.InverseTransformDirection(new Vector3(offset.x, offset.y, 0));
        crashArrow.anchoredPosition = new Vector2(104, -112) + direction.normalized * Mathf.Min(80, direction.magnitude / mapCamera.orthographicSize * 94);
        crashArrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        crashDistance.text = offset.magnitude < 10 ? "Crash site reached" : $"Drone · {offset.magnitude / 1000f:0.00} km";
    }

    private static Sprite MakeSprite(string name, int width, int height, System.Func<int, int, Color32> pixel)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var colors = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                colors[y * width + x] = pixel(x, y);
        texture.SetPixels32(colors);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f));
    }

    private static Color32 PlatePixel(int x, int y, int height)
    {
        int across = Mathf.Min(x, Width - 1 - x);
        int vertical = Mathf.Min(y, height - 1 - y);
        int edge = Mathf.Min(Mathf.Min(across, vertical), (across + vertical - 7) / 2);
        if (across + vertical < 7) return Color.clear;
        if (edge < 2) return Shadow;
        if (edge < 4) return y > height / 2 ? Metal : Edge;
        return edge < 6 ? Shadow : Face;
    }

    private static float Radius(int x, int y) =>
        new Vector2(x + .5f - MapSize * .5f, y + .5f - MapSize * .5f).magnitude;

    private static Color32 MapRingPixel(int x, int y)
    {
        float radius = Radius(x, y);
        if (radius >= 104 || radius <= 94) return Color.clear;
        if (radius >= 102 || radius <= 96) return Shadow;
        if (radius >= 100) return y > MapSize / 2 ? Metal : Edge;
        return (x + y) % 85 < 8 ? Amber : Metal;
    }

    private void OnDestroy()
    {
        if (status) Destroy(status.gameObject);
        if (map) Destroy(map.gameObject);
        if (mapCamera) Destroy(mapCamera.gameObject);
        if (mapTexture) Destroy(mapTexture);
        if (notificationSprite) Destroy(notificationSprite);
        foreach (var sprite in new[] { clockFrame, rowFrame, mapRing, mapDisc })
        {
            if (!sprite) continue;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
