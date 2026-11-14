using System;
using System.Reflection;
using UnityEngine;

static class DoorChecks
{
    sealed class TestDoor : BuildingEntrance { public void Enter(Collider2D other) => OnTriggerEnter2D(other); }
    static int passed;
    static void Check(bool condition,string name) { if(!condition) throw new Exception(name); passed++; }
    static void Set(object target,string name,object value) => typeof(BuildingEntrance).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    public static void Run()
    {
        var exterior=new TestDoor(); var interior=new TestDoor();
        var player=new Rigidbody2D();
        var camera=new Camera(); var originalParent=new Transform();
        camera.transform.parent=originalParent;
        camera.transform.position=new Vector3(0,0,-10);
        camera.transform.localPosition=new Vector3(0,0,-10);
        var inside=new Transform { position=new Vector3(200,-5,0) };
        var outside=new Transform { position=new Vector3(-12,-13,0) };
        foreach(var door in new[]{exterior,interior})
        {
            Set(door,"player",player); Set(door,"worldCamera",camera);
        }
        Set(exterior,"pairedDoor",interior);Set(interior,"pairedDoor",exterior);
        Set(exterior,"arrival",inside);Set(interior,"arrival",outside);
        Set(exterior,"entersInterior",true);Set(interior,"entersInterior",false);
        Set(exterior,"interiorRoot",new Transform {position=new Vector3(200,0,0)});
        Set(exterior,"interiorSize",new Vector2(39.2f,26.4f));
        Set(exterior,"preserveExteriorZoom",true);
        Time.unscaledTime=1;
        exterior.Enter(new Collider2D {attachedRigidbody=new Rigidbody2D()});
        Check(player.position.x==0,"Unrelated body ignored");
        var playerCollider=new Collider2D { attachedRigidbody=player };
        interior.Enter(playerCollider);
        Check(player.position.x==0,"Exit ignored before entry");
        player.linearVelocity=new Vector2(4,1);
        exterior.Enter(playerCollider);
        Check(player.position.x==200 && player.position.y==-5,"Correct interior spawn");
        Check(player.linearVelocity.x==0 && player.linearVelocity.y==0,"Transition clears velocity");
        Check(camera.transform.parent==null && camera.transform.position.x==200,"Camera detaches and centers");
        Check(camera.transform.position.z==-10,"Camera depth retained");
        Check(camera.orthographicSize>=13.2f,"Camera fits interior");
        interior.Enter(playerCollider);
        Check(player.position.x==200,"Rapid exit blocked");
        Time.unscaledTime=2;
        interior.Enter(playerCollider);
        Check(player.position.x==-12 && player.position.y==-13,"Correct exterior return");
        Check(camera.transform.parent==originalParent,"Original follow parent restored");
        Check(camera.transform.localPosition.z==-10 && camera.orthographicSize==10,"Camera offset and zoom restored");
        exterior.Enter(playerCollider);
        Check(player.position.x==-12,"Rapid reentry blocked");
        Time.unscaledTime=3;
        exterior.Enter(playerCollider);
        Check(player.position.x==200,"Reentry works after guard");
        Time.unscaledTime=4;
        interior.Enter(playerCollider);
        Check(camera.transform.parent==originalParent && camera.orthographicSize==10,"Repeated visits restore camera");
        Set(exterior,"interiorSize",new Vector2(5,5));
        Time.unscaledTime=5;
        exterior.Enter(playerCollider);
        Check(camera.orthographicSize==10,"Existing zoom retained when it already fits");
        var greenhouseEntry = new GreenhouseEntrance(); var greenhouseExit = new GreenhouseEntrance();
        var ui = new GreenhouseInteractionUI();
        foreach (var door in new[] { greenhouseEntry, greenhouseExit })
        {
            Set(door,"player",player); Set(door,"worldCamera",camera);
            typeof(GreenhouseEntrance).GetField("greenhouse",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(door,ui);
        }
        Set(greenhouseEntry,"pairedDoor",greenhouseExit); Set(greenhouseExit,"pairedDoor",greenhouseEntry);
        Set(greenhouseEntry,"arrival",inside); Set(greenhouseExit,"arrival",outside);
        Set(greenhouseEntry,"entersInterior",true);
        var callback = typeof(BuildingEntrance).GetMethod("OnTriggerEnter2D",BindingFlags.NonPublic|BindingFlags.Instance);
        Time.unscaledTime=10;
        callback.Invoke(greenhouseEntry,new object[] {playerCollider});
        Check(ui.IsInside,"Existing greenhouse entry UI hook retained");
        Time.unscaledTime=11;
        callback.Invoke(greenhouseExit,new object[] {playerCollider});
        Check(!ui.IsInside,"Existing greenhouse exit UI hook retained");
        Console.WriteLine($"PASS: {passed} production doorway assertions with engine stand-ins (not Play Mode).");
    }
}
