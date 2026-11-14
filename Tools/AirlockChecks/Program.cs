using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

static class Checks
{
    static int passed;
    static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void Check(bool result,string label) { if(!result) throw new Exception(label); passed++; }
    static (AirlockDoor door,SpriteRenderer visual,Collider2D gate) Door()
    {
        var door=new AirlockDoor(); var visual=new SpriteRenderer(); var gate=new Collider2D();
        Set(door,"doorRenderer",visual); Set(door,"barriers",new[]{gate});
        Set(door,"openingFrames",Enumerable.Range(1,4).Select(frame=>new Sprite{frame=frame}).ToArray());
        Set(door,"animationSeconds",.8f);
        door.CloseImmediately(); visual.displayed.Clear();
        return(door,visual,gate);
    }
    static void Main()
    {
        var single=Door();
        Scheduler.Start(single.door.Animate(true));
        Check(single.gate.enabled,"Opening remains blocked");
        Scheduler.Start(single.door.Animate(true));
        Scheduler.Advance(.81);
        Check(single.visual.displayed.SequenceEqual(new[]{1,2,3,4}),"Opening order and overlapping request ignored");
        Check(single.door.IsOpen && !single.gate.enabled,"Only fully open door is traversable");
        single.visual.displayed.Clear();
        Scheduler.Start(single.door.Animate(false));
        Check(single.gate.enabled,"Closing immediately blocks crossing");
        Scheduler.Advance(.81);
        Check(single.visual.displayed.SequenceEqual(new[]{4,3,2,1}),"Closing order");
        Check(!single.door.IsOpen && single.gate.enabled,"Closed door blocks crossing");

        var outer=Door(); var inner=Door(); var player=new Rigidbody2D();
        var outside=new BuildingPortal(); var inside=new BuildingPortal();
        var cycle=new AirlockCycle();
        Set(cycle,"player",player);Set(cycle,"exteriorPortal",outside);Set(cycle,"interiorPortal",inside);
        Set(cycle,"outerDoor",outer.door);Set(cycle,"innerDoor",inner.door);
        typeof(AirlockCycle).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cycle,null);
        var astronaut=new Collider2D {attachedRigidbody=player};
        cycle.RequestTransition(outside,new Collider2D {attachedRigidbody=new Rigidbody2D()});
        Check(cycle.State==AirlockCycle.Phase.Outside,"Only astronaut starts sequence");
        cycle.RequestTransition(outside,astronaut);
        cycle.RequestTransition(outside,astronaut);
        Scheduler.Advance(.81);
        Check(outside.transitions==1,"Entry transitions once after outer opening");
        Check(outer.gate.enabled && inner.gate.enabled,"Both closed while closing outer door");
        Scheduler.Advance(.81);
        Check(cycle.State==AirlockCycle.Phase.Stabilizing,"Stabilization follows full outer closure");
        Scheduler.Advance(2.9);
        Check(!inner.door.IsOpen && inner.gate.enabled,"Inner locked for full pressure timer");
        Scheduler.Advance(.9);
        Check(cycle.State==AirlockCycle.Phase.EnterHabitat && inner.door.IsOpen && !outer.door.IsOpen,"Safe entry interlock");
        cycle.CrossSensor(AirlockSensor.Position.HabitatSide,astronaut);
        Scheduler.Advance(.81);
        Check(cycle.State==AirlockCycle.Phase.Habitat && inner.gate.enabled,"Inner closes after astronaut clears it");
        cycle.CrossSensor(AirlockSensor.Position.InnerApproach,astronaut);
        Scheduler.Advance(.81);
        Check(cycle.State==AirlockCycle.Phase.EnterChamber && inner.door.IsOpen && outer.gate.enabled,"Exit approach opens only inner door");
        cycle.CrossSensor(AirlockSensor.Position.ChamberSide,astronaut);
        cycle.RequestTransition(inside,astronaut);
        Check(inside.transitions==0 && inner.gate.enabled && outer.gate.enabled,"No exit during inner closure");
        Scheduler.Advance(.81);
        Check(cycle.State==AirlockCycle.Phase.Depressurizing,"Depressurization starts after closure");
        Scheduler.Advance(2.9);
        Check(!outer.door.IsOpen && inside.transitions==0,"No exit before depressurization finishes");
        Scheduler.Advance(.9);
        Check(inside.transitions==1 && !inner.door.IsOpen,"Queued exit proceeds after outer opening, inner remains closed");
        Scheduler.Advance(.81);
        Check(cycle.State==AirlockCycle.Phase.Outside && outer.gate.enabled && inner.gate.enabled,"Exit finishes with both doors shut");
        Console.WriteLine($"PASS: {passed} focused animation/interlock/timer assertions with engine stand-ins.");
    }
}
