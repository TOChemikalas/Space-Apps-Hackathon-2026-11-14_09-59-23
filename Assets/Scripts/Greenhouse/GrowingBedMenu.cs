using UnityEngine;

// Each bed owns this menu and its selected crop; no crop progress is stored here.
public sealed class GrowingBedMenu : MonoBehaviour
{
    const float Width = 1280, Height = 760;
    static readonly Color Background = new Color(.045f, .085f, .10f);
    static readonly Color PanelColor = new Color(.075f, .13f, .15f);
    static readonly Color Border = new Color(.32f, .44f, .47f);
    static readonly Color Cream = new Color(.95f, .91f, .77f);
    static readonly Color Green = new Color(.58f, .88f, .30f);
    static readonly Color Orange = new Color(1, .56f, .08f);
    GrowingBed bed;
    int selectedCrop;
    bool isOpen;
    GUIStyle title, heading, text, small, button;

    void Awake() => bed = GetComponent<GrowingBed>();
    public void Open() => isOpen = true;
    public void Hide() => isOpen = false;

    void OnDisable()
    {
        isOpen = false;
        if (bed && bed.Greenhouse) bed.Greenhouse.CloseMenu(bed);
    }

    void OnGUI()
    {
        if (!isOpen || !bed || !bed.Greenhouse) return;
        if (title == null) CreateStyles();
        Matrix4x4 previous = GUI.matrix;
        int previousDepth = GUI.depth;
        GUI.depth = -10;
        float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2,
            (Screen.height - Height * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        try { DrawMenu(); }
        finally
        {
            GUI.matrix = previous;
            GUI.depth = previousDepth;
        }
    }

    void CreateStyles()
    {
        title = Style(44, FontStyle.Bold);
        heading = Style(27, FontStyle.Bold);
        text = Style(21, FontStyle.Normal);
        small = Style(17, FontStyle.Normal);
        button = Style(23, FontStyle.Bold);
        button.alignment = TextAnchor.MiddleCenter;
        button.hover.textColor = Color.white;
        button.active.textColor = Cream;
    }

    static GUIStyle Style(int size, FontStyle weight)
    {
        var style = new GUIStyle { fontSize = size, fontStyle = weight, wordWrap = true };
        style.normal.textColor = Cream;
        return style;
    }

    void DrawMenu()
    {
        var crops = bed.Greenhouse.Crops;
        if (crops.Count == 0) return;
        selectedCrop = Mathf.Clamp(selectedCrop, 0, crops.Count - 1);
        CropDefinition crop = bed.Progress.IsPlanted ? bed.Crop : crops[selectedCrop];
        string state = bed.Progress.IsReady ? "Ready to harvest" : bed.Progress.IsPlanted ? "Growing" : "Empty";

        Fill(new Rect(0, 0, Width, Height), Background);
        GUI.Label(new Rect(28, 15, 800, 30), "> GREENHOUSE / " + bed.name, text);
        GUI.Label(new Rect(28, 48, 850, 65), bed.Progress.IsPlanted ? "Manage crops" : "Plant crops", title);
        Frame(new Rect(1000, 25, 250, 58), PanelColor, Border);
        GUI.Label(new Rect(1015, 40, 220, 35), $"Harvested: {bed.Greenhouse.HarvestCount(crop.Id)}", text);
        Fill(new Rect(0, 120, Width, 2), Border);

        // The reference's left / center / right hierarchy, scoped to this bed only.
        Frame(new Rect(16, 140, 280, 530), PanelColor, Border);
        Header(new Rect(28, 155, 250, 45), "Seed selection");
        for (int i = 0; i < crops.Count; i++)
        {
            CropDefinition choice = crops[i];
            Rect row = new Rect(28, 215 + i * 106, 256, 96);
            bool chosen = crop == choice;
            Frame(row, Background, chosen ? Green : Border);
            DrawPreview(new Rect(row.x + 5, row.y + 5, 90, 86), choice.SpriteAt(5));
            GUI.Label(new Rect(row.x + 100, row.y + 15, 150, 40), choice.DisplayName, text);
            GUI.Label(new Rect(row.x + 100, row.y + 57, 145, 30), $"{choice.EarthDays:0} Earth days", small);
            if (!bed.Progress.IsPlanted && GUI.Button(row, GUIContent.none, GUIStyle.none)) selectedCrop = i;
        }

        Frame(new Rect(310, 140, 580, 530), PanelColor, Border);
        Header(new Rect(328, 155, 540, 45), "Grow bed");
        GUI.Label(new Rect(332, 202, 530, 38), bed.name + " / " + state, text);
        Frame(new Rect(352, 264, 496, 280), new Color(.20f, .14f, .10f), bed.Progress.IsReady ? Orange : Green);
        int stage = bed.Progress.IsPlanted ? bed.Progress.Stage : 5;
        DrawPreview(new Rect(375, 278, 450, 245), crop.SpriteAt(stage));
        GUI.Label(new Rect(352, 554, 496, 34), bed.Progress.IsPlanted
            ? $"{bed.Crop.DisplayName} - {bed.Progress.Fraction:P1}" : "Preview - choose seeds to plant", text);
        Frame(new Rect(352, 604, 496, 19), Background, Border);
        if (bed.Progress.IsPlanted)
            Fill(new Rect(355, 607, 490 * (float)bed.Progress.Fraction, 13), bed.Progress.IsReady ? Orange : Green);

        Frame(new Rect(904, 140, 360, 530), PanelColor, Border);
        Header(new Rect(920, 155, 328, 45), "Plot details");
        GUI.Label(new Rect(926, 214, 310, 34), state, small);
        DrawPreview(new Rect(935, 252, 290, 160), crop.SpriteAt(stage));
        GUI.Label(new Rect(926, 416, 310, 64), crop.DisplayName, heading);
        Fill(new Rect(922, 483, 324, 2), Border);
        GUI.Label(new Rect(926, 496, 310, 32), $"Growth: {crop.EarthDays:0} Earth days", small);
        GUI.Label(new Rect(926, 533, 310, 32), $"Harvest yield: {crop.HarvestQuantity}", small);
        string action = bed.Progress.IsReady ? "Harvest" : bed.Progress.IsPlanted ? "Growing..." : "Plant " + crop.DisplayName;
        bool canAct = !bed.Progress.IsPlanted || bed.Progress.IsReady;
        if (ActionButton(new Rect(922, 586, 324, 62), action, canAct))
        {
            bool completed = bed.Progress.IsReady ? bed.Harvest() : bed.Plant(crop);
            if (completed) bed.Greenhouse.CloseMenu(bed);
        }

        if (ActionButton(new Rect(20, 695, 180, 48), "< Back", true, false)) bed.Greenhouse.CloseMenu(bed);
        double remaining = bed.RemainingSeconds;
        string status = bed.Progress.IsReady ? "Ready to harvest" : !bed.Progress.IsPlanted ? "Select a crop, then Plant"
            : double.IsInfinity(remaining) ? "Growth stopped by conditions" : $"Remaining: {remaining / 86400:0.00} Earth days";
        GUI.Label(new Rect(235, 705, 1000, 35), status, text);
    }

    void Header(Rect rect, string label)
    {
        Fill(new Rect(rect.x, rect.y + 5, 6, 28), Orange);
        GUI.Label(new Rect(rect.x + 18, rect.y, rect.width - 18, rect.height), label, heading);
    }

    bool ActionButton(Rect rect, string label, bool enabled, bool primary = true)
    {
        Color color = primary && enabled ? Green : PanelColor;
        if (enabled && rect.Contains(Event.current.mousePosition)) color = Color.Lerp(color, Color.white, .12f);
        Frame(rect, color, enabled && primary ? Green : Border);
        button.normal.textColor = primary && enabled ? Background : Cream;
        bool previous = GUI.enabled;
        GUI.enabled = previous && enabled;
        bool clicked = GUI.Button(rect, label, button);
        GUI.enabled = previous;
        return clicked;
    }

    static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static void Frame(Rect rect, Color fill, Color border)
    {
        Fill(rect, border);
        Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), fill);
    }

    static void DrawPreview(Rect rect, Sprite sprite)
    {
        if (!sprite) return;
        Rect source = sprite.textureRect;
        float aspect = source.width / source.height;
        float width = Mathf.Min(rect.width, rect.height * aspect), height = width / aspect;
        Rect target = new Rect(rect.center.x - width / 2, rect.center.y - height / 2, width, height);
        GUI.DrawTextureWithTexCoords(target, sprite.texture, new Rect(source.x / sprite.texture.width,
            source.y / sprite.texture.height, source.width / sprite.texture.width, source.height / sprite.texture.height));
    }
}
