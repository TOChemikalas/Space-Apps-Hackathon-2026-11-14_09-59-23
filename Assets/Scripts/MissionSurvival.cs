using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GreenhouseCanvas;

public sealed class MissionSurvival : MonoBehaviour
{
    public const double WaterPerSol = 2.57;
    public const double FoodPerSol = .637;
    private const double OxygenCapacity = MarsClock.SecondsPerSol * 8 / 24;
    private const double WaterCapacity = 2 * WaterPerSol;
    private const double FoodCapacity = 5 * FoodPerSol;

    [SerializeField] MarsClock clock;
    [SerializeField] Canvas canvas;
    [SerializeField] EnergyController energy;
    [SerializeField] Sprite kneelStandingSprite;
    [SerializeField, Range(0, 1)] float waterRecycling = .90f;
    [SerializeField, Min(0)] float initialHabitatWaterLiters = 5.14f;
    [SerializeField, Min(0)] float initialHabitatFoodKg = 3.185f;
    [SerializeField, Min(0)] float harvestKgPerUnit = .10616667f;
    [SerializeField] SurvivalEconomy.Tuning economyTuning = new SurvivalEconomy.Tuning();

    double oxygen = OxygenCapacity, water = WaterCapacity, food = FoodCapacity;
    double habitatWater, habitatFood;
    bool inHabitat;
    InputActionMap playerInput;
    bool playerInputWasEnabled;
    RectTransform failureOverlay, failureCard;
    double missionStart;
    SpriteRenderer kneelLegs, kneelTorso;
    public double MissionElapsed { get; private set; }
    public bool Failed { get; private set; }
    public float Oxygen => (float)(oxygen / OxygenCapacity);
    public float Hydration => (float)(water / WaterCapacity);
    public float Nutrition => (float)(food / FoodCapacity);
    public float WaterRecycling => waterRecycling;
    public SurvivalEconomy Economy { get; private set; }

    void Awake()
    {
        habitatWater = initialHabitatWaterLiters;
        habitatFood = initialHabitatFoodKg;
        if (clock) missionStart = clock.Seconds;
        Economy = new SurvivalEconomy { WaterLiters = initialHabitatWaterLiters, FoodKg = initialHabitatFoodKg,
            Recycling = waterRecycling, Settings = economyTuning };
        if (energy) energy.Survival = Economy;
    }

    void OnEnable()
    {
        if (energy) energy.SurvivalStepCompleted += AdvanceEconomy;
    }

    void OnDisable()
    {
        if (energy) energy.SurvivalStepCompleted -= AdvanceEconomy;
    }

    public void UpgradeWaterRecycling()
    {
        if (!Failed) waterRecycling = Mathf.Max(waterRecycling, .95f);
        if (Economy != null) Economy.Recycling = waterRecycling;
    }

    public void SetHabitatSupply(bool available)
    {
        if (Failed || inHabitat == available) return;
        inHabitat = available;
        Economy.InHabitat = available;
    }

    public void SetGreenhouseSupply(bool available)
    {
        if (Economy != null) Economy.InGreenhouse = available;
    }

    public void SetRoverSupply(bool occupied, bool powered)
    {
        if (Economy == null) return;
        Economy.InRover = occupied;
        Economy.RoverPowered = occupied && powered;
    }

    public double TimeUntilStarterHarvest(int sol) =>
        Math.Max(0, (sol - 1) * MarsClock.SecondsPerSol - missionStart - Economy.GrowthIntegralSeconds);

    public void AddHarvest(int units)
    {
        if (Failed || units <= 0) return;
        Economy.FoodKg += units * harvestKgPerUnit * Economy.CropCondition;
    }

    void AdvanceEconomy(double seconds)
    {
        MissionElapsed = Economy.ElapsedSeconds;
        oxygen = Economy.OxygenHours / 8 * OxygenCapacity;
        water = Economy.SuitWaterLiters;
        food = Economy.SuitFoodKg;
        habitatWater = Economy.WaterLiters;
        habitatFood = Economy.FoodKg;
        if (Economy.Health <= 0) Fail(Economy.FailureReason);
    }

    void LateUpdate()
    {
        if (failureCard) CenterAndScale(failureOverlay, failureCard);
    }

    void Fail(string reason)
    {
        if (Failed) return;
        Failed = true;
        clock.enabled = false;
        Time.timeScale = 0;
        playerInput = InputSystem.actions?.FindActionMap("Player");
        playerInputWasEnabled = playerInput?.enabled == true;
        playerInput?.Disable();
        StartCoroutine(PlayFailure(reason));
    }

    IEnumerator PlayFailure(string reason)
    {
        yield return null;
        if (RoverVehicle.Instance && RoverVehicle.Instance.IsOccupied)
            RoverVehicle.Instance.enabled = false;
        var movement = GetComponent<AstronautController>();
        if (movement) movement.enabled = false;
        if (TryGetComponent(out Rigidbody2D body)) body.linearVelocity = Vector2.zero;
        if (movement && movement.Visual && movement.Visual.TryGetComponent(out SpriteRenderer renderer) && kneelStandingSprite)
        {
            if (movement.Visual.TryGetComponent(out Animator animator)) animator.enabled = false;
            yield return StandToKneel(renderer);
        }
        if (!canvas) yield break;

        failureOverlay = Overlay(canvas, "Mission failure");
        var layer = failureOverlay.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true;
        layer.sortingOrder = 120;
        failureOverlay.gameObject.AddComponent<GraphicRaycaster>();
        failureOverlay.gameObject.AddComponent<Image>().color = new Color(.16f, .04f, .02f, .78f);
        failureCard = Create(failureOverlay, "Failure card", new Rect(0, 0, 520, 236));
        Image(failureCard, "Panel rim", new Rect(0, 0, 520, 236), Border);
        Image(failureCard, "Panel", new Rect(3, 3, 514, 230), Background);
        Image(failureCard, "Warning stripe", new Rect(19, 22, 6, 53), Orange);
        Label(failureCard, "Mission failed", new Rect(0, 23, 520, 52), "MISSION FAILED", 32,
            TextAnchor.MiddleCenter, FontStyle.Bold).color = Orange;
        Label(failureCard, "Cause", new Rect(0, 83, 520, 40), reason, 21, TextAnchor.MiddleCenter);
        var button = Button(failureCard, "Restart mission", new Rect(41, 151, 206, 51), Panel, Restart);
        Label(button.transform, "Restart label", new Rect(0, 0, 206, 51), "Restart mission", 20,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        var menu = Button(failureCard, "Main menu", new Rect(273, 151, 206, 51), Panel, () =>
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("CinematicIntro", LoadSceneMode.Single);
        });
        Label(menu.transform, "Menu label", new Rect(0, 0, 206, 51), "Main menu", 20,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        CenterAndScale(failureOverlay, failureCard);
    }

    IEnumerator StandToKneel(SpriteRenderer standing)
    {
        Sprite pose = kneelStandingSprite;
        Rect rect = pose.rect;
        float waist = Mathf.Round(rect.height * .38f);
        float pixelsPerUnit = pose.pixelsPerUnit;
        Vector2 posePivot = pose.pivot;
        Vector3 boot = new Vector3((rect.width * .5f - posePivot.x) / pixelsPerUnit, -posePivot.y / pixelsPerUnit, 0);
        Rect legs = new Rect(rect.x, rect.y, rect.width, waist);
        Rect torso = new Rect(rect.x, rect.y + waist, rect.width, rect.height - waist);
        kneelLegs = PosePart("Kneeling boots", standing, Sprite.Create(pose.texture, legs, new Vector2(.5f, 0), pixelsPerUnit));
        kneelTorso = PosePart("Kneeling torso", standing, Sprite.Create(pose.texture, torso, new Vector2(.5f, 0), pixelsPerUnit));
        standing.enabled = false;

        const int frames = 8;
        for (int frame = 0; frame < frames; frame++)
        {
            float weight = Mathf.SmoothStep(0, 1, frame / (frames - 1f));
            float legHeight = Mathf.Lerp(1, .43f, weight);
            kneelLegs.transform.localPosition = boot;
            kneelLegs.transform.localScale = new Vector3(1 + weight * .09f, legHeight, 1);
            kneelTorso.transform.localPosition = boot + new Vector3(weight * .03f, waist / pixelsPerUnit * legHeight, 0);
            kneelTorso.transform.localRotation = Quaternion.Euler(0, 0, -weight * 22);
            yield return new WaitForSecondsRealtime(.1f);
        }
        yield return new WaitForSecondsRealtime(.5f);
    }

    static SpriteRenderer PosePart(string name, SpriteRenderer standing, Sprite sprite)
    {
        var part = new GameObject(name).AddComponent<SpriteRenderer>();
        part.transform.SetParent(standing.transform, false);
        part.sprite = sprite;
        part.sharedMaterial = standing.sharedMaterial;
        part.sortingLayerID = standing.sortingLayerID;
        part.sortingOrder = standing.sortingOrder;
        part.color = standing.color;
        return part;
    }

    void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(gameObject.scene.buildIndex, LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (kneelLegs) Destroy(kneelLegs.sprite);
        if (kneelTorso) Destroy(kneelTorso.sprite);
        if (!Failed) return;
        Time.timeScale = 1;
        if (playerInputWasEnabled) playerInput.Enable();
        if (failureOverlay) Destroy(failureOverlay.gameObject);
    }
}
