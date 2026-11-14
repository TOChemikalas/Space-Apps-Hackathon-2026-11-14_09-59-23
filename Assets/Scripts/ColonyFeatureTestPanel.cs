using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UI;
#endif

public sealed class ColonyFeatureTestPanel : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private MarsClock clock;
    [SerializeField] private SolarPanel solarPanel;
    [SerializeField] private Greenhouse greenhouse;
    [SerializeField] private Canvas canvas;

    private Text status;
    private Text pauseLabel;
    private Button harvestButton;

    private void Start()
    {
        if (!clock || !solarPanel || !greenhouse || !canvas)
        {
            enabled = false;
            return;
        }

        var card = CreateRect("Colony feature test", canvas.transform, 12, 66, 314, 167);
        card.gameObject.AddComponent<Image>().color = new Color32(24, 29, 36, 245);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        status = AddText(card, font, "", 12, 8, 290, 80, TextAnchor.UpperLeft);
        AddButton(card, font, "Noon", 12, 94, 84, () => PauseAt(12));
        AddButton(card, font, "Midnight", 104, 94, 84, () => PauseAt(0));
        pauseLabel = AddButton(card, font, "Pause clock", 196, 94, 106, () => clock.paused = !clock.paused)
            .GetComponentInChildren<Text>();
        AddButton(card, font, "Advance 3 h", 12, 126, 128, () =>
        {
            clock.paused = true;
            clock.Advance(MarsClock.SecondsPerSol / 8);
        });
        harvestButton = AddButton(card, font, "Harvest", 148, 126, 154, () => greenhouse.Harvest());
    }

    private void LateUpdate()
    {
        string greenhouseStatus = greenhouse.IsReady ? "Ready to harvest" : greenhouse.IsPowered ? "Powered" : "Waiting for power";
        status.text = $"Sol {clock.Sol}  |  {clock.Hour:00.0} h\n"
            + $"Solar panel  {solarPanel.CurrentKilowatts:0.00} kW\n"
            + $"Greenhouse  {greenhouseStatus}  |  {greenhouse.Growth:0%}\n"
            + $"Harvests  {greenhouse.Harvests}";
        pauseLabel.text = clock.paused ? "Resume clock" : "Pause clock";
        harvestButton.interactable = greenhouse.IsReady;
    }

    private static Button AddButton(RectTransform parent, Font font, string label,
        int x, int y, int width, UnityEngine.Events.UnityAction clicked)
    {
        var rect = CreateRect(label, parent, x, y, width, 28);
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = new Color32(58, 73, 89, 255),
            highlightedColor = new Color32(76, 96, 115, 255),
            selectedColor = new Color32(76, 96, 115, 255),
            pressedColor = new Color32(44, 58, 71, 255),
            disabledColor = new Color32(44, 49, 56, 255),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        AddText(rect, font, label, 0, 0, width, 28, TextAnchor.MiddleCenter);
        button.onClick.AddListener(clicked);
        return button;
    }

    private static Text AddText(RectTransform parent, Font font, string label,
        int x, int y, int width, int height, TextAnchor alignment)
    {
        var text = CreateRect("Label", parent, x, y, width, height).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 15;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;
        return text;
    }

    private static RectTransform CreateRect(string objectName, Transform parent, int x, int y, int width, int height)
    {
        var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private void PauseAt(double hour)
    {
        clock.paused = true;
        clock.SeekHour(hour);
    }
#endif
}
