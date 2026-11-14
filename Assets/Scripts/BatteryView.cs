using UnityEngine;

// Presentation only: a shared pixel sprite and cached world-space readout.
[RequireComponent(typeof(Battery))]
public sealed class BatteryView : MonoBehaviour
{
    Battery battery;
    TextMesh label;
    int lastPercent = -1, lastTenths = -1;
    string lastState;
    static Sprite sprite;

    void Start()
    {
        battery = GetComponent<Battery>();
        if (!sprite) sprite = CreateSprite();
        var body = gameObject.AddComponent<SpriteRenderer>();
        body.sprite = sprite;
        body.sortingOrder = 20;
        var textObject = new GameObject("Charge readout");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0, -1.1f, -.1f);
        label = textObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.UpperCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = .11f;
        label.fontSize = 48;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = label.font.material;
        textRenderer.sortingOrder = 21;
    }

    void LateUpdate()
    {
        int percent = Mathf.RoundToInt((float)battery.Fraction * 100);
        int tenths = Mathf.RoundToInt((float)battery.StoredKWh * 10);
        string state = battery.StoredKWh <= .000001 ? "EMPTY"
            : battery.Fraction >= .999999 ? "FULL"
            : battery.BusPowerKW > .001 ? "CHARGING"
            : battery.BusPowerKW < -.001 ? "DISCHARGING" : "IDLE";
        if (percent == lastPercent && tenths == lastTenths && state == lastState) return;
        label.text = $"{percent}%  {tenths / 10f:0.0} kWh\n{state}";
        label.color = state == "EMPTY" ? new Color(1, .25f, .2f)
            : state == "DISCHARGING" ? new Color(1, .7f, .15f)
            : state == "IDLE" ? Color.white : new Color(.3f, 1, .4f);
        lastPercent = percent;
        lastTenths = tenths;
        lastState = state;
    }

    static Sprite CreateSprite()
    {
        var texture = new Texture2D(20, 28, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 28; y++)
            for (int x = 0; x < 20; x++)
            {
                bool shell = x >= 2 && x <= 17 && y >= 1 && y <= 24;
                bool terminal = x >= 7 && x <= 12 && y >= 25;
                bool edge = x <= 3 || x >= 16 || y <= 2 || y >= 23;
                // Stepped lightning bolt, drawn directly in the pixel-art body.
                bool bolt = (y >= 14 && y <= 21 && x >= 11 - (21 - y) / 2 && x <= 13 - (21 - y) / 2)
                    || (y >= 11 && y <= 14 && x >= 7 && x <= 12)
                    || (y >= 5 && y <= 11 && x >= 7 + (y - 5) / 2 && x <= 9 + (y - 5) / 2);
                Color color = terminal ? new Color(.65f, .7f, .72f)
                    : !shell ? Color.clear : bolt ? new Color(1, .82f, .2f)
                    : edge ? new Color(.12f, .16f, .2f) : new Color(.36f, .43f, .46f);
                texture.SetPixel(x, y, color);
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 20, 28), new Vector2(.5f, .5f), 14);
    }
}
