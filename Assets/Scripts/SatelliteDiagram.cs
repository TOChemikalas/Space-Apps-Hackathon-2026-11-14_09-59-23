using UnityEngine;
using UnityEngine.UI;

// Two orthogonal 2D views show real azimuth and elevation without rotating baked habitat artwork.
public sealed class SatelliteDiagram : MaskableGraphic
{
    public DishAlignmentModel Model;
    public bool DrawDish;
    public Vector2 HalfSpan = new Vector2(45, 22.5f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (Model == null) return;
        if (DrawDish) Dish(vh); else Monitor(vh);
    }

    void Monitor(VertexHelper vh)
    {
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        Color grid = new Color(.10f, .30f, .34f);
        for (int i = 1; i < 10; i++)
        {
            float x = rect.xMin + rect.width * i / 10;
            SatelliteDrawing.Line(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), 1, grid);
            float y = rect.yMin + rect.height * i / 10;
            SatelliteDrawing.Line(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1, grid);
        }
        SatelliteDrawing.Line(vh, new Vector2(rect.xMin, center.y), new Vector2(rect.xMax, center.y), 2, GreenhouseCanvas.Border);
        SatelliteDrawing.Line(vh, new Vector2(center.x, rect.yMin), new Vector2(center.x, rect.yMax), 2, GreenhouseCanvas.Border);
        float halfWidth = rect.width * .5f - 18, halfHeight = rect.height * .5f - 18;
        SatelliteDrawing.Ring(vh, center, halfWidth * (float)Model.Tolerance / HalfSpan.x,
            halfHeight * (float)Model.Tolerance / HalfSpan.y, 3, GreenhouseCanvas.Green);
        Cross(vh, center, 12, GreenhouseCanvas.Green);
        Vector2 aim = center + new Vector2(Mathf.Clamp(-(float)Model.AzimuthError / HalfSpan.x, -1, 1) * halfWidth,
            Mathf.Clamp(-(float)Model.ElevationError / HalfSpan.y, -1, 1) * halfHeight);
        Cross(vh, aim, 14, Model.IsLocked ? GreenhouseCanvas.Green : GreenhouseCanvas.Orange);
        SatelliteDrawing.Ring(vh, aim, 20, 20, 2, Model.IsLocked ? GreenhouseCanvas.Green : GreenhouseCanvas.Orange);
    }

    static void Cross(VertexHelper vh, Vector2 center, float size, Color color)
    {
        SatelliteDrawing.Line(vh, center - Vector2.right * size, center + Vector2.right * size, 3, color);
        SatelliteDrawing.Line(vh, center - Vector2.up * size, center + Vector2.up * size, 3, color);
    }

    void Dish(VertexHelper vh)
    {
        Rect rect = GetPixelAdjustedRect();
        Vector2 plan = new Vector2(rect.xMin + rect.width * .27f, rect.center.y);
        Vector2 side = new Vector2(rect.xMin + rect.width * .76f, rect.center.y);
        float radius = Mathf.Min(rect.width * .20f, rect.height * .30f);
        float azimuth = (float)Model.Azimuth * Mathf.Deg2Rad;
        float elevation = (float)Model.Elevation * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(azimuth), Mathf.Cos(azimuth));
        Vector2 tangent = new Vector2(forward.y, -forward.x);
        Color dish = Model.IsLocked ? GreenhouseCanvas.Green : GreenhouseCanvas.Cream;
        SatelliteDrawing.Line(vh, plan + Vector2.down * radius * 1.3f, plan, 9, GreenhouseCanvas.Border);
        Vector2 previous = plan + tangent * radius;
        for (int i = 1; i <= 48; i++)
        {
            float angle = i * 2 * Mathf.PI / 48;
            Vector2 next = plan + tangent * (Mathf.Cos(angle) * radius)
                + forward * (Mathf.Sin(angle) * radius * Mathf.Lerp(.15f, 1, Mathf.Sin(elevation)));
            SatelliteDrawing.Line(vh, previous, next, 5, dish);
            previous = next;
        }
        SatelliteDrawing.Line(vh, plan, plan + forward * radius * 1.2f, 4, GreenhouseCanvas.Orange);
        SatelliteDrawing.Ring(vh, plan, 6, 6, 4, GreenhouseCanvas.Orange);
        Vector2 normal = new Vector2(Mathf.Cos(elevation), Mathf.Sin(elevation));
        Vector2 bowlTangent = new Vector2(-normal.y, normal.x);
        SatelliteDrawing.Line(vh, side + Vector2.down * radius * 1.3f, side, 9, GreenhouseCanvas.Border);
        previous = side - bowlTangent * radius + normal * radius * .3f;
        for (int i = 1; i <= 24; i++)
        {
            float t = -1 + 2f * i / 24;
            Vector2 next = side + bowlTangent * (t * radius) + normal * (radius * .3f * t * t);
            SatelliteDrawing.Line(vh, previous, next, 5, dish);
            previous = next;
        }
        Vector2 feed = side + normal * radius;
        SatelliteDrawing.Line(vh, side - bowlTangent * radius + normal * radius * .3f, feed, 2, dish);
        SatelliteDrawing.Line(vh, side + bowlTangent * radius + normal * radius * .3f, feed, 2, dish);
        SatelliteDrawing.Ring(vh, feed, 6, 6, 4, GreenhouseCanvas.Orange);
    }
}
