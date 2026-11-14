// Copy into Assets/Editor in an ISOLATED project copy, then run:
// Unity -batchmode -nographics -projectPath <copy> -executeMethod CommunicationsUnityChecks.Run -logFile <log>
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CommunicationsUnityChecks
{
    const string Key = "CommunicationsUnityChecksRunning";
    static IEnumerator checks;
    static double deadline;
    static int passed;
    static CommunicationsUnityChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            checks = PlayChecks();
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        PlayerSettings.companyName = "CodexIsolatedChecks";
        PlayerSettings.productName = "CommunicationsCheck-" + Guid.NewGuid().ToString("N");
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Unity checks timed out.");
            if (checks.MoveNext()) return;
            Debug.Log("COMMUNICATIONS UNITY PASS: " + passed + " Play Mode assertions.");
            Finish(0);
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(code);
    }
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        passed++;
        Debug.Log("CHECK PASS: " + label);
    }
    static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    static void Call(object target, string method, params object[] arguments) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    static Button Button(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == name && b.gameObject.activeInHierarchy);
    static void Click(string name) => Button(name).onClick.Invoke();
    static string Screen(ElectricalControls controls) => Field(controls, "screen").ToString();
    static int Received(CommunicationsSession session) => session.Model.State.messages.Count(m => m.status == DeliveryStatus.Received);

    static IEnumerator PlayChecks()
    {
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        // Give Start and additive interior loading time to finish.
        double until = EditorApplication.timeSinceStartup + 5;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        var controls = UnityEngine.Object.FindFirstObjectByType<ElectricalControls>();
        var session = controls.GetComponent<CommunicationsSession>();
        var clock = UnityEngine.Object.FindFirstObjectByType<MarsClock>();
        clock.paused = true;
        Check(session && session.Model != null, "Scene has initialized communications binding");
        var loading = SceneManager.LoadSceneAsync("HabitatInterior", LoadSceneMode.Additive);
        while (!loading.isDone) yield return null;
        var items = UnityEngine.Object.FindObjectsByType<ElectricalInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items) item.Bind(controls);
        var computer = items.Single(item => !item.OpensBattery);
        Transform ancestor = computer.transform;
        while (ancestor) { ancestor.gameObject.SetActive(true); ancestor = ancestor.parent; }
        yield return null;
        var movement = (AstronautController)Field(controls, "movement");
        var player = (Rigidbody2D)Field(controls, "player");
        movement.enabled = true;
        var range = (BoxCollider2D)Field(computer, "interactionRange");
        var solid = computer.SolidBoundary.bounds;
        player.position = new Vector2(solid.center.x, solid.min.y - .25f);
        Physics2D.SyncTransforms();
        Check(computer.InRange(player.position), "Computer approach is within configured interaction range");
        controls.AddCandidate(computer);
        Call(controls, "Update");
        Check(computer.GetComponent<LineRenderer>().enabled, "Computer white outline in range");
        player.position += Vector2.down * 20;
        Physics2D.SyncTransforms(); Call(controls, "Update");
        Check(!computer.GetComponent<LineRenderer>().enabled, "Outline removed outside range");
        player.position = new Vector2(solid.center.x, solid.min.y - .25f);
        Physics2D.SyncTransforms(); controls.AddCandidate(computer);
        if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(UnityEngine.InputSystem.Key.E));
        PumpPlayerInput();
        var interact = (InputAction)Field(controls, "interact");
        Debug.Log($"Input diagnostic: enabled={interact?.enabled}, pressed={interact?.WasPressedThisFrame()}, key={Keyboard.current.eKey.isPressed}");
        Call(controls, "Update");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); PumpPlayerInput();
        Check(Screen(controls) == "Selection" && !movement.enabled && ((RectTransform)Field(controls, "overlay")).gameObject.activeSelf,
            "E opens selection artwork and blocks movement");
        Check(Button("Electricity").GetComponent<RectTransform>().rect.size == new Vector2(519, 101), "Selection hotspot matches artwork coordinates");
        Click("Electricity"); Check(Screen(controls) == "Overview", "Electricity chain preserved");
        Click("Electrical back"); Check(Screen(controls) == "Selection", "Electricity Back returns to selection");
        Click("Communications"); Check(Screen(controls) == "Communications", "Communications selection opens existing menu");
        Click("Satellite alignment tab");
        var satellite = controls.GetComponent<SatelliteAlignmentMenu>();
        Check(satellite.IsOpen, "Existing dish minigame opens");
        satellite.Back(); Check(!satellite.IsOpen && Screen(controls) == "Communications", "Satellite Back returns to Communications");
        Click("Transmission channel tab"); Check(Screen(controls) == "Transmission", "Transmission opens");
        var input = UnityEngine.Object.FindFirstObjectByType<InputField>();
        input.text = "SOS";
        Check(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(b => b.name == "Transmit"), "Transmission uses Enter without a Transmit button");
        Check(input.lineType == InputField.LineType.MultiLineSubmit, "Enter submits instead of adding a newline");
        input.onEndEdit.Invoke(input.text);
        Check(session.Model.State.narrative == CommunicationNarrative.Unassessed, "Losing focus does not transmit");
        Check(!session.Transmit("SOS"), "Physical offline blocks outgoing transmission");
        controls.Dish.Restore(controls.Dish.TargetAzimuth, controls.Dish.TargetElevation, controls.Dish.TargetAzimuth, controls.Dish.TargetElevation, true);
        Check(session.Model.Online, "Physical link refreshes outgoing UI without reopening");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(UnityEngine.InputSystem.Key.Enter));
        PumpPlayerInput();
        input.onEndEdit.Invoke(input.text);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); PumpPlayerInput();
        Check(session.Model.State.narrative == CommunicationNarrative.MissionSupported && session.Model.State.supplyArrivalSol == 250, "UI distress establishes absolute milestone");
        clock.Advance(60);
        Check(Received(session) == 1, "Clock delivers acknowledgment");
        Click("Back"); Click("Incoming messages tab");
        Check(Screen(controls) == "Inbox" && Button(CommunicationsModel.AcknowledgmentId), "Inbox list dynamically populated");
        Click(CommunicationsModel.AcknowledgmentId);
        var body = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t => t.name == "Selected transmission");
        Check(body.text.Contains(CommunicationsModel.AcknowledgmentBody) && session.Model.State.messages[0].read, "Selection displays full read-only acknowledgment and marks read");
        Check(!body.GetComponent<InputField>(), "Inbox does not accept typing");
        string json = session.CaptureJson(); session.RestoreJson(json);
        Check(Received(session) == 1 && session.Model.State.messages[0].read, "Unity JsonUtility save restore preserves read inbox without duplicates");
        session.SetHardwareOperational(false);
        var status = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t => t.name == "Physical link");
        Check(status.text == "LINK OFFLINE" && Received(session) == 1 && body.text.Contains("Outpost-12"), "Open inbox refreshes offline status and preserves received body");
        session.SetHardwareOperational(true);
        session.Transmit("I abandon the mission.");
        Check(session.Model.Online && !session.Model.CanTransmit, "Narrative termination does not break hardware");
        session.RestoreJson(session.CaptureJson());
        Check(session.Model.State.narrative == CommunicationNarrative.CommunicationTerminated && Received(session) == 1, "Unity save/load preserves permanent isolation and history");
        Click("Back"); Click("Back to system selection"); Click("Back");
        Check(movement.enabled && !((RectTransform)Field(controls, "overlay")).gameObject.activeSelf, "Final Back restores gameplay input");
        Call(controls, "Open", items.First(item => item.OpensBattery));
        Check(Screen(controls) == "Battery", "Battery still opens its own equipment screen");
        Call(controls, "Close");
        Check(movement.enabled, "Battery close restores movement");
        session.SaveNow();
        string savePath = Path.Combine(Application.persistentDataPath, "communications-v1.json");
        Check(savePath.Contains("CommunicationsCheck-") && File.Exists(savePath), "Scoped disk save exists only in isolated test profile");
        double savedSeconds = clock.Seconds;
        var reload = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        while (!reload.isDone) yield return null;
        yield return null;
        controls = UnityEngine.Object.FindFirstObjectByType<ElectricalControls>();
        session = controls.GetComponent<CommunicationsSession>();
        clock = UnityEngine.Object.FindFirstObjectByType<MarsClock>();
        clock.paused = true;
        Check(session.Model.State.narrative == CommunicationNarrative.CommunicationTerminated && Received(session) == 1 && session.Model.State.messages[0].read,
            "Scene recreation preserves narrative, inbox and read state");
        Check(clock.Seconds >= savedSeconds && controls.IsSatelliteAligned, "Scene recreation restores authoritative clock and physical alignment");
        session.RestoreJson(File.ReadAllText(savePath));
        Check(!session.Model.CanTransmit && Received(session) == 1, "Actual disk JSON reload preserves termination without duplicate delivery");
        yield return null;
    }

    static void PumpPlayerInput()
    {
        // EditorApplication callbacks otherwise process the Editor input buffer in headless mode.
        typeof(InputSystem).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
    }
}
#endif
