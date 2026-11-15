using System;
using System.IO;
using UnityEngine;

// The project has no general save service. This scoped adapter also exposes JSON capture/restore
// for a future game save, and keeps the same state when scene-local UI objects are recreated.
public sealed class CommunicationsSession : MonoBehaviour
{
    [SerializeField] ScriptedTransmission[] messages = Array.Empty<ScriptedTransmission>();
    [SerializeField, Min(0)] float acknowledgmentDelaySeconds = 60;
    [SerializeField] LateSupplyPolicy lateSupplyPolicy;
    [SerializeField] bool hardwareOperational = true;
    [SerializeField] DroneCrashSite droneCrashSite;
    [Serializable] sealed class Save
    {
        public CommunicationsState communications = new CommunicationsState();
        public double azimuth, elevation, targetAzimuth, targetElevation;
        public bool dishLocked;
    }
    static Save shared;
    MarsClock clock;
    ElectricalControls owner;
    CommunicationsModel model;
    bool loading, persistenceAvailable = true;
    public CommunicationsModel Model => model;
    public double SignalPercent => owner.Dish.Signal * 100;
    public event Action Changed;
    string SavePath => Path.Combine(Application.persistentDataPath, "communications-v1.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() => shared = null;

    internal void Initialize(ElectricalControls controls, MarsClock authoritativeClock)
    {
        if (model != null) return;
        owner = controls;
        clock = authoritativeClock;
        loading = true;
        bool restoring = shared != null;
        if (shared == null && File.Exists(SavePath))
        {
            try
            {
                shared = JsonUtility.FromJson<Save>(File.ReadAllText(SavePath));
                Validate(shared);
                restoring = true;
            }
            catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
            {
                persistenceAvailable = false; // Preserve an unreadable save for recovery.
                shared = null;
                Debug.LogWarning("Communications save could not be loaded; the original file is preserved. " + error.Message, this);
            }
        }
        shared ??= new Save();
        if (restoring)
        {
            clock.RestoreSeconds(shared.communications.clockSeconds);
            owner.Dish.Restore(shared.azimuth, shared.elevation, shared.targetAzimuth, shared.targetElevation, shared.dishLocked);
        }
        CreateModel();
        clock.Advanced += OnClockAdvanced;
        owner.Dish.Changed += OnLinkChanged;
        owner.SatelliteAligned += OnLinkChanged;
        loading = false;
        OnLinkChanged();
        model.Advance(clock.Seconds);
    }

    void CreateModel()
    {
        var definitions = new TransmissionDefinition[messages.Length + (droneCrashSite ? 1 : 0)];
        for (int i = 0; i < messages.Length; i++) definitions[i] = messages[i] ? messages[i].message : null;
        if (droneCrashSite) definitions[messages.Length] = droneCrashSite.DiscoveryMessage();
        model = new CommunicationsModel(shared.communications, definitions, MarsClock.SecondsPerSol,
            acknowledgmentDelaySeconds, lateSupplyPolicy);
        model.Changed += OnChanged;
    }

    void OnClockAdvanced(double from, double to) { OnLinkChanged(); model.Advance(to); }
    void OnLinkChanged() => model.SetOnline(hardwareOperational && owner.IsSatelliteAligned && owner.CommunicationsPowered, clock.Seconds);
    public void SetHardwareOperational(bool operational) { hardwareOperational = operational; OnLinkChanged(); }
    public bool Transmit(string body) => model.Submit(body, clock.Seconds);
    public bool QueueMessage(string id) => model.QueueMessage(id, clock.Seconds);
    public void RaiseEvent(string id) => model.RaiseEvent(id, clock.Seconds);
    public void SetCondition(string id, bool value) => model.SetCondition(id, value, clock.Seconds);

    void OnChanged()
    {
        if (!loading) SaveNow();
        Changed?.Invoke();
    }

    void Capture()
    {
        shared.communications.clockSeconds = clock.Seconds;
        shared.azimuth = owner.Dish.Azimuth;
        shared.elevation = owner.Dish.Elevation;
        shared.targetAzimuth = owner.Dish.TargetAzimuth;
        shared.targetElevation = owner.Dish.TargetElevation;
        shared.dishLocked = owner.Dish.IsLocked;
    }

    public string CaptureJson() { Capture(); return JsonUtility.ToJson(shared, true); }

    public void RestoreJson(string json)
    {
        var restored = JsonUtility.FromJson<Save>(json);
        Validate(restored);
        // Validate message IDs and model data before replacing live state.
        loading = true;
        model.Changed -= OnChanged;
        shared = restored;
        CreateModel();
        clock.RestoreSeconds(shared.communications.clockSeconds);
        owner.Dish.Restore(shared.azimuth, shared.elevation, shared.targetAzimuth, shared.targetElevation, shared.dishLocked);
        OnLinkChanged();
        model.Advance(clock.Seconds);
        loading = false;
        OnChanged();
    }

    static void Validate(Save save)
    {
        if (save?.communications == null || save.communications.version != 1 || save.communications.messages == null ||
            save.communications.outgoing == null || save.communications.conditions == null ||
            double.IsNaN(save.communications.clockSeconds) || double.IsInfinity(save.communications.clockSeconds) || save.communications.clockSeconds < 0)
            throw new ArgumentException("Invalid communications save.");
        _ = new CommunicationsModel(save.communications, null, MarsClock.SecondsPerSol);
    }

    public void SaveNow()
    {
        if (model == null) return;
        Capture();
        if (!persistenceAvailable) return;
        try
        {
            string temp = SavePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(shared, true));
            if (File.Exists(SavePath)) File.Replace(temp, SavePath, null);
            else File.Move(temp, SavePath);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        { Debug.LogWarning("Communications could not be saved. " + error.Message, this); }
    }

    void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
    void OnApplicationQuit() => SaveNow();
    void OnDestroy()
    {
        if (model == null) return;
        SaveNow();
        clock.Advanced -= OnClockAdvanced;
        owner.Dish.Changed -= OnLinkChanged;
        owner.SatelliteAligned -= OnLinkChanged;
        model.Changed -= OnChanged;
    }
}
