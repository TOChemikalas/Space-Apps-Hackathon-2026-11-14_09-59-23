using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(500)]
public sealed class MarsDustStormVisuals : MonoBehaviour
{
    public const string EffectsLayer = "StormFX";

    [Header("References")]
    public MarsDustStorm storm;
    public Light2D globalLight;
    public ParticleSystem flyingDust;
    [SerializeField] Camera worldCamera;
    [SerializeField] Canvas canvas;
    [SerializeField] Material particleMaterial;

    [Header("Storm lighting")]
    [Range(0, 1)] public float maxAdditionalDarkness = .45f;
    public Color stormTint = new Color(.96f, .59f, .38f);
    [Range(0, 1)] public float maxTintStrength = .24f;

    [Header("Atmospheric haze")]
    [SerializeField] Color hazeTint = new Color(.67f, .28f, .12f);
    [SerializeField, Range(0, 1)] float maxHazeOpacity = .28f;

    [Header("Flying dust")]
    [Min(0)] public float particlesPerSecondAtPeak = 80;
    [SerializeField, Range(1, 2)] float cameraCoverage = 1.3f;

    Image hazeOverlay;
    Material dustMaterial;
    Texture2D dustTexture;
    bool ownsParticles, lightModified;
    float clearIntensity, appliedIntensity;
    Color clearColor, appliedColor;

    void Awake()
    {
        if (!storm) storm = GetComponent<MarsDustStorm>();
        if (canvas)
        {
            var haze = GreenhouseCanvas.Overlay(canvas, "StormHaze");
            haze.SetAsFirstSibling();
            hazeOverlay = haze.gameObject.AddComponent<Image>();
            hazeOverlay.raycastTarget = false;
            SetHaze(0);
        }
        if (!flyingDust && worldCamera && particleMaterial) CreateDust();
    }

    void LateUpdate()
    {
        float strength = storm && storm.isActiveAndEnabled ? Mathf.Clamp01(storm.Intensity) : 0;
        SetHaze(strength);
        SetLighting(strength);
        SetDust(strength);
    }

    void SetHaze(float strength)
    {
        if (!hazeOverlay) return;
        Color color = hazeTint;
        color.a = strength * maxHazeOpacity;
        hazeOverlay.color = color;
        hazeOverlay.enabled = color.a > 0;
    }

    void SetLighting(float strength)
    {
        if (!globalLight) return;
        // Preserve the latest day/night pass. If it is unavailable, remove our own previous contribution.
        RestoreLighting();
        if (strength <= 0) return;
        clearIntensity = globalLight.intensity;
        clearColor = globalLight.color;
        appliedIntensity = clearIntensity * (1 - strength * maxAdditionalDarkness);
        appliedColor = Color.Lerp(clearColor, stormTint, strength * maxTintStrength);
        globalLight.intensity = appliedIntensity;
        globalLight.color = appliedColor;
        lightModified = true;
    }

    void RestoreLighting()
    {
        if (!globalLight || !lightModified) return;
        if (globalLight.intensity == appliedIntensity) globalLight.intensity = clearIntensity;
        if (globalLight.color == appliedColor) globalLight.color = clearColor;
        lightModified = false;
    }

    void SetDust(float strength)
    {
        if (!flyingDust) return;
        var emission = flyingDust.emission;
        emission.rateOverTime = particlesPerSecondAtPeak * strength;
        if (worldCamera && worldCamera.orthographic)
        {
            float height = 2 * worldCamera.orthographicSize * cameraCoverage;
            var shape = flyingDust.shape;
            shape.scale = new Vector3(height * worldCamera.aspect, height, .1f);
        }
        if (strength > 0 && !flyingDust.isPlaying) flyingDust.Play();
    }

    void CreateDust()
    {
        var emitter = new GameObject("StormParticles");
        int layer = LayerMask.NameToLayer(EffectsLayer);
        if (layer >= 0) emitter.layer = layer;
        emitter.transform.SetParent(worldCamera.transform, false);
        emitter.transform.localPosition = new Vector3(0, 0, 5);
        flyingDust = emitter.AddComponent<ParticleSystem>();
        ownsParticles = true;
        flyingDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = flyingDust.main;
        main.duration = 5;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(.06f, .14f);
        main.startColor = (Color)new Color32(213, 155, 112, 145);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 350;

        var shape = flyingDust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22, 14, .1f);
        var velocity = flyingDust.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 7;
        velocity.y = 1;
        velocity.z = 0;

        var colors = flyingDust.colorOverLifetime;
        colors.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f),
                new GradientAlphaKey(.8f, .8f), new GradientAlphaKey(0, 1) });
        colors.color = fade;

        dustTexture = MakeDustTexture();
        dustMaterial = new Material(particleMaterial) { name = "MarsDustParticle (runtime)" };
        dustMaterial.SetTexture("_BaseMap", dustTexture);
        var renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = dustMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2;
        renderer.velocityScale = .2f;
        renderer.sortingLayerName = "Effects";
        renderer.sortingOrder = 100;
        SetDust(0);
    }

    static Texture2D MakeDustTexture()
    {
        const int size = 24;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Soft storm dust",
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            var point = new Vector2(i % size + .5f, i / size + .5f) * (2f / size) - Vector2.one;
            float opacity = Mathf.Clamp01(1 - point.sqrMagnitude);
            pixels[i] = new Color32(255, 255, 255, (byte)(opacity * opacity * 255));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    void OnDisable()
    {
        SetHaze(0);
        RestoreLighting();
        if (!flyingDust) return;
        SetDust(0);
        flyingDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnDestroy()
    {
        if (hazeOverlay) Destroy(hazeOverlay.gameObject);
        if (ownsParticles && flyingDust) Destroy(flyingDust.gameObject);
        if (dustMaterial) Destroy(dustMaterial);
        if (dustTexture) Destroy(dustTexture);
    }
}
