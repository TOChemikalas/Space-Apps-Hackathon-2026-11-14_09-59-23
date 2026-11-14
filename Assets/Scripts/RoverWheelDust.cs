using UnityEngine;

[RequireComponent(typeof(RoverVehicle))]
public sealed class RoverWheelDust : MonoBehaviour
{
    [SerializeField] private SpriteRenderer roverVisual;
    [SerializeField] private Material particleMaterial;
    [SerializeField, Min(0f)] private float dustStartSpeed = 2.5f;
    [SerializeField, Min(0.01f)] private float fullDustSpeed = 7f;
    [SerializeField, Min(0f)] private float maxParticlesPerWheel = 42f;
    [SerializeField, Min(0f)] private float rearAxleDistance = .78f;
    [SerializeField, Min(0f)] private float wheelHalfTrack = .58f;

    private RoverVehicle rover;
    private Rigidbody2D body;
    private ParticleSystem leftWheel;
    private ParticleSystem rightWheel;
    private Texture2D dustTexture;
    private Material dustMaterial;

    private void Awake()
    {
        rover = GetComponent<RoverVehicle>();
        body = GetComponent<Rigidbody2D>();
        if (!roverVisual || !particleMaterial)
        {
            enabled = false;
            return;
        }

        dustTexture = CreateDustTexture();
        dustMaterial = new Material(particleMaterial) { name = "Rover dust" };
        dustMaterial.SetTexture("_BaseMap", dustTexture);
        leftWheel = CreateEmitter("Left rear wheel dust");
        rightWheel = CreateEmitter("Right rear wheel dust");
    }

    private void LateUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        float speed = velocity.magnitude;
        float strength = rover.IsOccupied && speed > dustStartSpeed
            ? Mathf.InverseLerp(dustStartSpeed, Mathf.Max(dustStartSpeed + .01f, fullDustSpeed), speed)
            : 0f;

        if (strength > 0f)
        {
            float angle = RoverDrivingVisual.DirectionSector(velocity) * Mathf.PI / 4f;
            Vector2 forward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 axleCenter = -forward * rearAxleDistance;
            Vector2 left = new Vector2(-forward.y, forward.x) * wheelHalfTrack;
            Quaternion spray = Quaternion.FromToRotation(Vector3.forward, -(Vector3)velocity.normalized);
            Transform visual = roverVisual.transform;
            leftWheel.transform.SetPositionAndRotation(visual.TransformPoint(axleCenter + left), spray);
            rightWheel.transform.SetPositionAndRotation(visual.TransformPoint(axleCenter - left), spray);
        }

        SetEmission(leftWheel, strength);
        SetEmission(rightWheel, strength);
    }

    private void SetEmission(ParticleSystem wheel, float strength)
    {
        var emission = wheel.emission;
        emission.rateOverTime = maxParticlesPerWheel * strength * strength;
        if (strength <= 0f) return;

        var main = wheel.main;
        float spraySpeed = Mathf.Lerp(.8f, 2.9f, strength);
        main.startSpeed = new ParticleSystem.MinMaxCurve(spraySpeed * .6f, spraySpeed);
        if (!wheel.isPlaying) wheel.Play(false);
    }

    private ParticleSystem CreateEmitter(string emitterName)
    {
        var emitterObject = new GameObject(emitterName);
        emitterObject.layer = roverVisual.gameObject.layer;
        emitterObject.transform.SetParent(transform, false);
        var wheel = emitterObject.AddComponent<ParticleSystem>();
        wheel.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = wheel.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 96;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .85f);
        main.startSize = new ParticleSystem.MinMaxCurve(.14f, .40f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color32(161, 88, 52, 183), new Color32(207, 138, 92, 202));

        var emission = wheel.emission;
        emission.rateOverTime = 0f;

        var shape = wheel.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = .075f;

        var colors = wheel.colorOverLifetime;
        colors.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(.30f, 0f), new GradientAlphaKey(.9f, .14f), new GradientAlphaKey(0f, 1f) });
        colors.color = fade;

        var size = wheel.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .55f, 1f, 1.8f));

        var renderer = wheel.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = dustMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerID = roverVisual.sortingLayerID;
        renderer.sortingOrder = roverVisual.sortingOrder - 1;
        return wheel;
    }

    private static Texture2D CreateDustTexture()
    {
        const int resolution = 32;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Dust puff",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + .5f) / resolution;
                float v = (y + .5f) / resolution;
                float radius = new Vector2(u * 2f - 1f, v * 2f - 1f).magnitude;
                float cloud = Mathf.PerlinNoise(3f + u * 4f, 9f + v * 4f);
                float alpha = Mathf.Clamp01(1f - radius) * (.4f + cloud * .6f);
                pixels[y * resolution + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDisable()
    {
        if (leftWheel) leftWheel.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (rightWheel) rightWheel.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (leftWheel) Destroy(leftWheel.gameObject);
        if (rightWheel) Destroy(rightWheel.gameObject);
        if (dustMaterial) Destroy(dustMaterial);
        if (dustTexture) Destroy(dustTexture);
    }
}
