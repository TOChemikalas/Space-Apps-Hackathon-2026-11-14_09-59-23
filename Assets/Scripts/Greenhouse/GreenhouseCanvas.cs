using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal static class GreenhouseCanvas
{
    internal static readonly Color Background = new Color(.045f, .085f, .10f);
    internal static readonly Color Panel = new Color(.075f, .13f, .15f);
    internal static readonly Color Border = new Color(.32f, .44f, .47f);
    internal static readonly Color Cream = new Color(.95f, .91f, .77f);
    internal static readonly Color Green = new Color(.58f, .88f, .30f);
    internal static readonly Color Orange = new Color(1, .56f, .08f);

    internal static RectTransform Overlay(Canvas canvas, string name)
    {
        RectTransform root = Create(canvas.transform, name, default);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(.5f, .5f);
        root.offsetMin = root.offsetMax = Vector2.zero;
        return root;
    }

    internal static RectTransform Create(Transform parent, string name, Rect layout)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(layout.x, -layout.y);
        rect.sizeDelta = layout.size;
        return rect;
    }

    internal static Image Image(Transform parent, string name, Rect layout, Color color)
    {
        var image = Create(parent, name, layout).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    internal static Text Label(Transform parent, string name, Rect layout, string content, int size,
        TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
    {
        var text = Create(parent, name, layout).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Cream;
        text.raycastTarget = false;
        return text;
    }

    internal static Button Button(Transform parent, string name, Rect layout, Color color, UnityAction action)
    {
        Image target = Image(parent, name, layout, color);
        target.raycastTarget = true;
        var button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = target;
        button.onClick.AddListener(action);
        return button;
    }

    internal static void CenterAndScale(RectTransform overlay, RectTransform content)
    {
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, .5f);
        content.anchoredPosition = Vector2.zero;
        Vector2 available = overlay.rect.size;
        Vector2 size = content.sizeDelta;
        float scale = Mathf.Min(available.x / size.x, available.y / size.y);
        content.localScale = new Vector3(scale, scale, 1);
    }
}
