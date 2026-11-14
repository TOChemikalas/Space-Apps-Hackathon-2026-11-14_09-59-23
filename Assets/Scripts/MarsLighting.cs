
using UnityEngine;

// One global shader update per frame, independent of the number of terrain sprites.
public sealed class MarsLighting : MonoBehaviour
{
    public MarsClock clock;
    [Range(0, 360)] public float startingLs = 90;
    [Range(0, 1)] public float nightBrightness = 0.12f;
    public Color night = new Color(.38f, .44f, .64f);
    public Color dawn = new Color(1f, .57f, .40f);
    public Color day = new Color(1f, .94f, .86f);

    static readonly int Ambient = Shader.PropertyToID("_MarsAmbient");
    static readonly int Tint = Shader.PropertyToID("_MarsTint");
    static readonly int Direction = Shader.PropertyToID("_MarsSunDirection");
    static readonly int Direct = Shader.PropertyToID("_MarsDirect");

    void Awake() { if (!clock) clock = GetComponent<MarsClock>(); }

    void LateUpdate()
    {
        if (!clock) return;
        var sun = MarsSun.At(clock.Seconds, startingLs);
        float light = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-8, 18, sun.elevation));
        float dawnAmount = Mathf.Clamp01(1 - Mathf.Abs(sun.elevation - 2) / 16);

        Shader.SetGlobalFloat(Ambient, Mathf.Lerp(nightBrightness, 1, light));
        Shader.SetGlobalColor(Tint, Color.Lerp(Color.Lerp(night, day, light), dawn, dawnAmount * .65f));
        Shader.SetGlobalVector(Direction, sun.direction);
        Shader.SetGlobalFloat(Direct, Mathf.Clamp01(sun.elevation / 15) * .7f);
    }
}
