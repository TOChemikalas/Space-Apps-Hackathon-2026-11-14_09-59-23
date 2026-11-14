using System;
using System.Reflection;

static class Checks
{
    static int passed;
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        passed++;
    }
    static void Equal(double actual, double expected, string label) => Check(Math.Abs(actual - expected) < 1e-8, $"{label}: {actual} != {expected}");
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    static void Main()
    {
        double day = 86400;
        var clock = new MarsClock();
        Invoke(clock, "Awake");
        var lettuce = new CropProgress();
        var radish = new CropProgress();
        var potato = new CropProgress();
        var wheat = new CropProgress();
        Check(lettuce.Plant(28 * day), "Plant lettuce");
        Check(radish.Plant(30 * day), "Plant radishes");
        Check(potato.Plant(90 * day), "Plant potatoes");
        Check(wheat.Plant(80 * day), "Plant wheat");
        Check(!lettuce.Plant(day), "Reject occupied bed");
        Check(!lettuce.Harvest(), "Reject immature harvest");
        foreach (var state in new[] { lettuce, radish, potato, wheat }) clock.Advanced += (from, to) => state.Advance(from, to, 1);
        for (int stage = 0; stage < 6; stage++)
        {
            Equal(lettuce.Stage, stage, "Six ordered stages");
            clock.Advance(28 * day / 6);
        }
        Check(lettuce.IsReady, "Lettuce matures at 28 Earth days");
        Check(!radish.IsReady && !potato.IsReady && !wheat.IsReady, "Independent crop durations");
        Equal(radish.Fraction, 28.0 / 30, "Radish progress independent");
        Check(lettuce.Harvest(), "Mature harvest succeeds");
        Check(!lettuce.Harvest(), "Double harvest prevented");
        Equal(lettuce.Stage, -1, "Harvest clears visual stage");
        Equal(radish.Fraction, 28.0 / 30, "Harvest does not alter other bed");
        Check(lettuce.Plant(day), "Replant after harvest");
        double previous = radish.Fraction;
        clock.SeekHour(22);
        Equal(radish.Fraction, previous, "Seeking adds no growth");
        clock.paused = true;
        UnityEngine.Time.unscaledDeltaTime = 5;
        Invoke(clock, "Update");
        Equal(radish.Fraction, previous, "Pause stops clock-driven growth");
        clock.paused = false;
        clock.speed = 100;
        double expected = clock.ScaleTime(UnityEngine.Time.unscaledDeltaTime) / (30 * day);
        Invoke(clock, "Update");
        Equal(radish.Fraction, previous + expected, "Acceleration uses simulated time");
        var slow = new CropProgress(); var fast = new CropProgress();
        slow.Plant(90 * day); fast.Plant(90 * day);
        for (int i = 0; i < 1000; i++) slow.Advance(i * 1000, (i + 1) * 1000, 1);
        fast.Advance(0, 1000000, 1);
        Equal(slow.Fraction, fast.Fraction, "Large versus incremental advances");
        var fifth = new CropProgress(); fifth.Plant(day);
        fifth.Advance(0, day, .5);
        Equal(fifth.Fraction, .5, "Fifth independent bed and multiplier");
        fifth.Advance(0, day, 0);
        Equal(fifth.Fraction, .5, "Environment can stop growth");
        fifth.Advance(day, day, 1);
        fifth.Advance(day, 0, 1);
        Equal(fifth.Fraction, .5, "Zero or backward seek ignored");
        fifth.Advance(0, 100 * day, 2);
        Equal(fifth.Fraction, 1, "Growth clamps at 100 percent");
        Check(!new CropProgress().Plant(double.NaN), "Invalid duration rejected");
        var persistent = new CropProgress(); persistent.Plant(day);
        persistent.Advance(0, .5 * day, 1);
        // No proximity or rendering input exists: the same subscriber advances outside the greenhouse.
        persistent.Advance(.5 * day, day, 1);
        Check(persistent.IsReady, "Growth independent of proximity");
        Console.WriteLine($"PASS: {passed} crop and clock assertions.");
        SceneChecks.Run();
    }
}
