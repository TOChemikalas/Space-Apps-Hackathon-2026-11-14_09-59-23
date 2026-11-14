using System;

static class Checks
{
    static int passed;
    static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        passed++;
    }
    static void Equal(double actual, double expected, string label) => Check(Math.Abs(actual - expected) < 1e-7, label);
    static void Main()
    {
        Equal(DishAlignmentModel.ShortestDelta(359, 1), 2, "Forward pointer wrap");
        Equal(DishAlignmentModel.ShortestDelta(1, 359), -2, "Reverse pointer wrap");
        var dish = new DishAlignmentModel(); dish.Normalize();
        double start = dish.Azimuth, startElevation = dish.Elevation;
        for (int i = 0; i < 108; i++) dish.TurnAzimuth(10);
        Equal(dish.Azimuth, start, "Three full revolutions without snapping");
        Equal(dish.Elevation, startElevation, "Azimuth wheel cannot change elevation");
        dish.TurnAzimuth(-1081);
        Equal(dish.Azimuth, DishAlignmentModel.Wrap(start - 1), "Negative multi-turn wrap");
        start = dish.Azimuth;
        dish.TurnElevation(1000);
        Equal(dish.Elevation, 90, "Upper mechanical stop");
        dish.TurnElevation(-1000);
        Equal(dish.Elevation, 0, "Lower mechanical stop");
        Equal(dish.Azimuth, start, "Elevation wheel cannot change azimuth");
        dish.SetTarget(359, 30);
        dish.TurnAzimuth(DishAlignmentModel.ShortestDelta(dish.Azimuth, 1));
        dish.TurnElevation(30);
        Equal(dish.AzimuthError, -2, "Target shortest-path azimuth error");
        Check(dish.CanAcquire, "Tolerance boundary meets 95-percent threshold");
        double nearSignal = dish.Signal;
        dish.TurnAzimuth(10);
        Check(dish.Signal < nearSignal, "Signal weakens away from target");
        dish.TurnAzimuth(DishAlignmentModel.ShortestDelta(dish.Azimuth, dish.TargetAzimuth));
        Equal(dish.Signal, 1, "Exact target has full signal");
        Check(!dish.Hold(1), "Lock is not instant");
        dish.TurnElevation(10);
        Equal(dish.HeldSeconds, 0, "Lost alignment resets hold");
        dish.TurnElevation(-10);
        Check(!dish.Hold(1), "New continuous hold starts from zero");
        dish.CancelHold();
        Equal(dish.Azimuth, 359, "Back preserves azimuth");
        Equal(dish.Elevation, 30, "Back preserves elevation");
        Equal(dish.HeldSeconds, 0, "Back cancels incomplete acquisition only");
        Check(!dish.Hold(1.49), "Must maintain full 1.5 seconds");
        Check(dish.Hold(.02) && dish.IsLocked, "Successful sustained lock");
        Check(!dish.Hold(1), "Lock success reported only once");
        dish.CancelHold();
        Check(dish.IsLocked, "Locked state survives leaving and reopening");
        dish.TurnAzimuth(20); dish.TurnElevation(20);
        Equal(dish.Azimuth, 359, "Successful orientation protected");
        Equal(dish.Elevation, 30, "Successful elevation protected");
        dish.SetTarget(359, 30);
        Check(dish.IsLocked, "Same target does not restart puzzle");
        int changes = 0; dish.Changed += () => changes++;
        dish.SetTarget(20, 45);
        Check(!dish.IsLocked && changes == 1, "Future target changes invalidate lock and notify UI");
        Console.WriteLine($"PASS: {passed} focused assertions against production DishAlignmentModel.");
    }
}
