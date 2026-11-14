using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GreenhouseCanvas;

public sealed class SatelliteAlignmentMenu : MonoBehaviour
{
    [SerializeField] ElectricalControls owner;
    [SerializeField] Sprite artwork;
    [SerializeField, Min(.001f)] float azimuthSensitivity = .1f;
    [SerializeField, Min(.001f)] float elevationSensitivity = .1f;
    [SerializeField] Vector2 monitorHalfSpan = new Vector2(45, 22.5f);
    RectTransform overlay, content;
    SatelliteSteeringWheel azimuthWheel, elevationWheel;
    SatelliteDiagram monitor, dish;
    Text target, current, offset, azimuth, elevation, signal, status, acquired, link;
    Image meter;
    Coroutine holdRoutine;
    bool holding;
    Action back;
    public bool IsOpen => overlay && overlay.gameObject.activeSelf;

    void OnEnable() { if (owner) owner.Dish.Changed += OnOrientationChanged; }
    void OnDisable()
    {
        if (owner) owner.Dish.Changed -= OnOrientationChanged;
        Hide();
    }

    public void Open(Canvas canvas, Action onBack)
    {
        if (!overlay) Build(canvas);
        back = onBack;
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(true);
        CenterAndScale(overlay, content);
        EventSystem.current?.SetSelectedGameObject(null);
        OnOrientationChanged();
    }

    public void Back()
    {
        Hide();
        back?.Invoke();
    }

    public void Hide()
    {
        CancelHold();
        if (azimuthWheel) azimuthWheel.StopDrag();
        if (elevationWheel) elevationWheel.StopDrag();
        if (overlay) overlay.gameObject.SetActive(false);
    }

    void OnDestroy() { if (overlay) Destroy(overlay.gameObject); }
    void LateUpdate() { if (IsOpen) CenterAndScale(overlay, content); }

    void OnOrientationChanged()
    {
        if (!IsOpen) return;
        Refresh();
        if (!owner.Dish.CanAcquire || owner.Dish.IsLocked) { CancelHold(); return; }
        if (holding) return;
        holding = true;
        holdRoutine = StartCoroutine(HoldAlignment());
    }

    IEnumerator HoldAlignment()
    {
        while (IsOpen && owner.Dish.CanAcquire && !owner.Dish.IsLocked)
        {
            yield return null;
            if (!IsOpen) break;
            bool locked = owner.Dish.Hold(Time.unscaledDeltaTime);
            Refresh();
            if (locked) owner.NotifySatelliteAligned();
        }
        holding = false;
        holdRoutine = null;
    }

    void CancelHold()
    {
        if (holdRoutine != null) StopCoroutine(holdRoutine);
        holdRoutine = null;
        holding = false;
        if (owner) owner.Dish.CancelHold();
    }

    void Build(Canvas canvas)
    {
        overlay = Overlay(canvas, "Satellite alignment menu");
        overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .8f);
        var background = Image(overlay, "Supplied satellite control panel", new Rect(0, 0, 1672, 941), Color.white);
        background.sprite = artwork;
        content = background.rectTransform;
        target = Field("Target direction", new Rect(97, 254, 230, 34));
        current = Field("Current direction", new Rect(97, 349, 230, 34));
        offset = Field("Angular offset", new Rect(97, 440, 230, 34));
        Mask("Replace baked radar", new Rect(381, 142, 766, 370));
        Label(content, "Radar legend", new Rect(403, 146, 720, 28), "GREEN: TARGET / TOLERANCE     AMBER: DISH AIM", 18);
        monitor = Create(content, "Live targeting monitor", new Rect(410, 181, 712, 300)).gameObject.AddComponent<SatelliteDiagram>();
        monitor.raycastTarget = false;
        monitor.Model = owner.Dish;
        monitor.HalfSpan = new Vector2(Mathf.Max(1, monitorHalfSpan.x), Mathf.Max(1, monitorHalfSpan.y));
        Label(content, "Relative projection", new Rect(410, 485, 710, 25), "Relative angular error — target at center", 18, TextAnchor.MiddleCenter);
        Mask("Independent live dish", new Rect(1205, 185, 434, 325));
        dish = Create(content, "Dish base / azimuth / elevation schematic", new Rect(1215, 205, 414, 265)).gameObject.AddComponent<SatelliteDiagram>();
        dish.raycastTarget = false;
        dish.DrawDish = true;
        dish.Model = owner.Dish;
        Label(content, "Dish views", new Rect(1215, 480, 414, 25), "TOP: AZIMUTH       SIDE: ELEVATION", 17, TextAnchor.MiddleCenter);
        azimuthWheel = Wheel("Horizontal steering wheel", new Rect(318, 576, 272, 272));
        elevationWheel = Wheel("Vertical steering wheel", new Rect(718, 576, 272, 272));
        azimuthWheel.Turned = delta => owner.Dish.TurnAzimuth(delta * azimuthSensitivity);
        elevationWheel.Turned = delta => owner.Dish.TurnElevation(delta * elevationSensitivity);
        azimuth = Field("Actual azimuth", new Rect(114, 725, 183, 58), 32);
        elevation = Field("Actual elevation", new Rect(1007, 725, 165, 58), 32);
        signal = Field("Signal strength", new Rect(1515, 554, 120, 54), 34);
        Mask("Replace example signal meter", new Rect(1238, 619, 390, 32));
        meter = Image(content, "Live signal meter", new Rect(1240, 622, 0, 26), Orange);
        status = Field("Alignment status", new Rect(1237, 677, 398, 63), 20);
        acquired = Field("Automatic signal acquisition", new Rect(1236, 759, 400, 69), 27);
        link = Field("Communications link", new Rect(1358, 43, 284, 49), 24);
        Button(content, "Back to Communications", new Rect(16, 865, 167, 64), Color.clear, Back);
        overlay.gameObject.SetActive(false);
    }

    SatelliteSteeringWheel Wheel(string name, Rect rect)
    {
        Mask(name + " background", rect);
        return Create(content, name, rect).gameObject.AddComponent<SatelliteSteeringWheel>();
    }

    Image Mask(string name, Rect rect) => Image(content, name, rect, Background);
    Text Field(string name, Rect rect, int size = 20)
    {
        var panel = Mask(name + " background", rect);
        return Label(panel.transform, name, new Rect(4, 0, rect.width - 8, rect.height), "", size, TextAnchor.MiddleCenter);
    }

    void Refresh()
    {
        var state = owner.Dish;
        target.text = $"AZ {state.TargetAzimuth:0.0}°  EL {state.TargetElevation:0.0}°";
        current.text = $"AZ {state.Azimuth:0.0}°  EL {state.Elevation:0.0}°";
        offset.text = $"AZ {state.AzimuthError:+0.0;-0.0;0.0}°  EL {state.ElevationError:+0.0;-0.0;0.0}°";
        azimuth.text = $"{state.Azimuth:0.0}°";
        elevation.text = $"{state.Elevation:0.0}°";
        signal.text = $"{state.Signal * 100:0.0}%";
        Color strength = Color.Lerp(Orange, Green, (float)state.Signal);
        signal.color = meter.color = strength;
        meter.rectTransform.sizeDelta = new Vector2(386 * (float)state.Signal, 26);
        bool offScreen = Math.Abs(state.AzimuthError) > monitor.HalfSpan.x || Math.Abs(state.ElevationError) > monitor.HalfSpan.y;
        status.text = state.IsLocked ? "Target acquired — SIGNAL LOCKED" : state.CanAcquire
            ? $"ALIGNING — hold {state.HeldSeconds:0.0} / {state.RequiredHoldSeconds:0.0} seconds"
            : offScreen ? "SEARCHING — aim outside monitor range" : $"SEARCHING — angular error {state.Error:0.0}°";
        acquired.text = state.IsLocked ? "SIGNAL LOCKED" : state.CanAcquire ? "TARGET ACQUIRED — HOLD" : "ALIGN DISH TO TARGET";
        acquired.color = link.color = state.IsLocked ? Green : Orange;
        link.text = state.IsLocked ? "SIGNAL LOCKED" : "LINK OFFLINE";
        azimuthWheel.Interactable = elevationWheel.Interactable = !state.IsLocked;
        if (state.IsLocked) { azimuthWheel.StopDrag(); elevationWheel.StopDrag(); }
        monitor.SetVerticesDirty();
        dish.SetVerticesDirty();
    }
}
