using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The hit rectangle stays fixed; only drawn spokes rotate, avoiding transform feedback during dragging.
public sealed class SatelliteSteeringWheel : MaskableGraphic, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, ICanvasRaycastFilter
{
    public Action<float> Turned;
    public bool Interactable = true;
    [SerializeField, Min(0)] float centerDeadZone = 18;
    int activePointer = int.MinValue;
    bool hasAngle;
    float previousAngle;
    double wheelDegrees;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!Interactable || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) return false;
        return (local - rectTransform.rect.center).sqrMagnitude <= Mathf.Pow(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f, 2);
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (!Interactable || data.button != PointerEventData.InputButton.Left || activePointer != int.MinValue) return;
        activePointer = data.pointerId;
        hasAngle = false;
        ReadPointer(data);
    }

    public void OnDrag(PointerEventData data)
    {
        if (!Interactable || activePointer != data.pointerId) return;
        ReadPointer(data);
    }

    void ReadPointer(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out var local)) return;
        Vector2 direction = local - rectTransform.rect.center;
        if (direction.sqrMagnitude < centerDeadZone * centerDeadZone) { hasAngle = false; return; }
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (hasAngle)
        {
            float clockwiseDelta = -(float)DishAlignmentModel.ShortestDelta(previousAngle, angle);
            wheelDegrees += clockwiseDelta;
            SetVerticesDirty();
            Turned?.Invoke(clockwiseDelta);
        }
        previousAngle = angle;
        hasAngle = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (activePointer == data.pointerId) StopDrag();
    }

    public void StopDrag() { activePointer = int.MinValue; hasAngle = false; }
    protected override void OnDisable() { StopDrag(); base.OnDisable(); }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * .44f;
        SatelliteDrawing.Ring(vh, center, radius, radius, 20, new Color(.25f, .29f, .3f));
        SatelliteDrawing.Ring(vh, center, radius + 5, radius + 5, 3, GreenhouseCanvas.Cream);
        for (int i = 0; i < 3; i++)
        {
            double angle = (i * 120 - wheelDegrees % 360) * Math.PI / 180;
            Vector2 tip = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (radius - 10);
            SatelliteDrawing.Line(vh, center, tip, 13, new Color(.48f, .51f, .5f));
        }
        SatelliteDrawing.Ring(vh, center, 24, 24, 14, GreenhouseCanvas.Orange);
        SatelliteDrawing.Ring(vh, center, 12, 12, 10, GreenhouseCanvas.Border);
    }
}

// Shared vector geometry for the two wheels, radar, and independently animated dish schematic.
internal static class SatelliteDrawing
{
    internal static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 delta = b - a;
        if (delta.sqrMagnitude < .00001f) return;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized * width * .5f;
        int index = vh.currentVertCount;
        vh.AddVert(a - normal, color, Vector2.zero);
        vh.AddVert(a + normal, color, Vector2.zero);
        vh.AddVert(b + normal, color, Vector2.zero);
        vh.AddVert(b - normal, color, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    internal static void Ring(VertexHelper vh, Vector2 center, float rx, float ry, float width, Color color)
    {
        const int segments = 48;
        Vector2 previous = center + new Vector2(rx, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            Vector2 next = center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
            Line(vh, previous, next, width, color);
            previous = next;
        }
    }
}
