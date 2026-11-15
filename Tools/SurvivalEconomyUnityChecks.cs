#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SurvivalEconomyUnityChecks
{
    static IEnumerator run;
    static int passed;
    static double deadline;
    static SurvivalEconomyUnityChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("EconomyCheck", false)) return;
            run = Checks(); deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        PlayerSettings.companyName = "CodexIsolatedChecks";
        PlayerSettings.productName = "EconomyCheck-" + Guid.NewGuid().ToString("N");
        SessionState.SetBool("EconomyCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        try {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout");
            if (run.MoveNext()) return;
            Debug.Log("ECONOMY UNITY PASS: " + passed);
            Finish(0);
        } catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool("EconomyCheck", false); EditorApplication.update -= Tick; EditorApplication.Exit(code); }
    static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); passed++; Debug.Log("CHECK PASS: " + name); }
    static IEnumerator Checks()
    {
        foreach (string scenario in new[] { "A", "B", "C", "R" })
        {
            var clock = UnityEngine.Object.FindFirstObjectByType<MarsClock>();
            clock.paused = true;
            clock.RestoreSeconds(MarsClock.SecondsPerSol / 4);
            var energy = UnityEngine.Object.FindFirstObjectByType<EnergyController>();
            var survival = UnityEngine.Object.FindFirstObjectByType<MissionSurvival>();
            survival.SetHabitatSupply(true);
            Check(energy.Survival == survival.Economy, scenario + " shared production economy binding");
            foreach (var weather in UnityEngine.Object.FindObjectsByType<MarsSolarPanelWeather>(FindObjectsSortMode.None)) weather.enabled = false;
            foreach (var solar in energy.arrays) solar.dustFactor = scenario == "A" ? 1 : .55f;
            if (scenario == "B") {
                foreach (var system in new[] { ElectricalSystem.Comms, ElectricalSystem.SuitRepairStation, ElectricalSystem.Cooling })
                    while (energy.Allocation.Get(system) > 0) energy.Allocation.Remove(system);
                while (energy.Allocation.Get(ElectricalSystem.Irrigation) > 1) energy.Allocation.Remove(ElectricalSystem.Irrigation);
            }
            // Allow the live HUD and interior startup callbacks to run before simulation.
            yield return null;
            survival.SetHabitatSupply(true);
            bool rescued = false, oxygenRestored = false;
            for (int i = 0; i < (scenario == "R" ? 6 : 4) * MarsClock.SecondsPerSol / 5 && !survival.Failed; i++) {
                if (scenario == "R" && !rescued && i * 5 >= 3 * MarsClock.SecondsPerSol) {
                    Check(energy.TryGetBatteryCharge(out float empty) && empty < .001f, "Recovery starts with depleted batteries");
                    foreach (var system in new[] { ElectricalSystem.Comms, ElectricalSystem.SuitRepairStation, ElectricalSystem.Cooling })
                        while (energy.Allocation.Get(system) > 0) energy.Allocation.Remove(system);
                    while (energy.Allocation.Get(ElectricalSystem.Irrigation) > 1) energy.Allocation.Remove(ElectricalSystem.Irrigation);
                    while (energy.Allocation.Get(ElectricalSystem.Oxygen) < 14) energy.Allocation.Add(ElectricalSystem.Oxygen);
                    rescued = true;
                }
                if (scenario == "R" && !oxygenRestored && i * 5 >= 4 * MarsClock.SecondsPerSol) {
                    while (energy.Allocation.Get(ElectricalSystem.Oxygen) > 7) energy.Allocation.Remove(ElectricalSystem.Oxygen);
                    oxygenRestored = true;
                }
                clock.Advance(5);
            }
            var state = survival.Economy;
            Check(scenario == "C" ? survival.Failed && state.Health == 0 : !survival.Failed && state.Health == 1,
                scenario + " native survival outcome");
            Check(Math.Abs(survival.Oxygen - state.OxygenHours / 8) < .001, scenario + " live oxygen HUD value");
            if (scenario == "B") {
                Check(state.Irrigation < .34 && state.Communications == 0 && state.SuitService == 0, "Crisis sacrifices affect actual systems");
                while (energy.Allocation.Get(ElectricalSystem.Irrigation) < 3) energy.Allocation.Add(ElectricalSystem.Irrigation);
                clock.Advance(5);
                Check(state.Irrigation > .99 && !survival.Failed, "Allocation recovery restores irrigation");
            }
            Debug.Log($"ECONOMY {scenario}: time={state.ElapsedSeconds/3600:F2}h health={state.Health:F3} oxygen={state.OxygenHours:F3} temp={state.TemperatureC:F2}");
            // Communications saves only its own checkpoint; reset that isolated snapshot for the next case.
            typeof(CommunicationsSession).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            var load = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
        }
    }
}
#endif
