using UnityEngine;
using UnityEngine.UI;
using static GreenhouseCanvas;

public sealed class AirlockDecontamination : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private AirlockCycle airlock;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Vector2 chamberCenter = new Vector2(-.45f, -5.55f);

    private ParticleSystem mist;
    private Image card;
    private Text status;
    private string phase;
    private float endsAt;
    private Texture2D fogTexture;
    private Material fogMaterial;

    public void BindCanvas(Canvas hud) => canvas = hud;

    private void BeginCycle(float duration, string message)
    {
        if (!isActiveAndEnabled) return;
        if (!card && canvas) CreateStatus();
        if (!mist && particleMaterial) CreateMist();

        phase = message;
        endsAt = Time.unscaledTime + duration;
        if (card) card.gameObject.SetActive(true);
        if (mist) mist.Play(false);
    }

    private void EndCycle()
    {
        if (card) card.gameObject.SetActive(false);
        if (mist) mist.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Update()
    {
        float duration = airlock ? airlock.ActivePressureDuration : 0f;
        if (duration <= 0f)
        {
            if (phase != null) EndCycle();
            phase = null;
            return;
        }

        if (phase == null)
            BeginCycle(duration, airlock.State == AirlockCycle.Phase.Stabilizing ? "Decontaminating" : "Depressurizing");
        if (status && card.gameObject.activeSelf)
            status.text = $"{phase}  ·  {Mathf.Max(0, endsAt - Time.unscaledTime):0.0} s";
    }

    private void CreateStatus()
    {
        card = Image(canvas.transform, "Airlock cycle status", new Rect(0, 0, 300, 42), Panel);
        RectTransform rect = card.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 26);
        status = Label(rect, "Cycle countdown", new Rect(0, 0, 300, 42), "", 18, TextAnchor.MiddleCenter);
    }

    private void CreateMist()
    {
        var emitter = new GameObject("Decontamination mist");
        emitter.transform.SetParent(transform, false);
        emitter.transform.localPosition = chamberCenter;
        mist = emitter.AddComponent<ParticleSystem>();
        mist.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = mist.main;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.loop = true;
        main.maxParticles = 140;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.45f, .95f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.35f, .95f);
        main.startColor = new Color(.67f, .92f, 1f, .57f);

        var shape = mist.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.35f, 2.8f, 0f);
        var emission = mist.emission;
        emission.rateOverTime = 115f;
        var drift = mist.velocityOverLifetime;
        drift.enabled = true;
        drift.x = new ParticleSystem.MinMaxCurve(-.26f, .26f);
        drift.y = new ParticleSystem.MinMaxCurve(.1f, .5f);
        drift.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.85f, .26f), new GradientAlphaKey(0f, 1f) });
        var colors = mist.colorOverLifetime;
        colors.enabled = true;
        colors.color = fade;

        const int size = 24;
        fogTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            var position = new Vector2(i % size + .5f, i / size + .5f) * (2f / size) - Vector2.one;
            float alpha = Mathf.Clamp01(1f - position.sqrMagnitude);
            pixels[i] = new Color32(255, 255, 255, (byte)(255f * alpha * alpha));
        }
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply(false, true);
        fogMaterial = new Material(particleMaterial);
        fogMaterial.SetTexture("_BaseMap", fogTexture);
        var renderer = mist.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = fogMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = "Effects";
    }

    private void OnDisable()
    {
        EndCycle();
        phase = null;
        if (mist) mist.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDestroy()
    {
        if (card) Destroy(card.gameObject);
        if (fogMaterial) Destroy(fogMaterial);
        if (fogTexture) Destroy(fogTexture);
    }
}
