// Engine stand-ins for focused coroutine/state checks. No Unity rendering or physics is simulated.
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class MonoBehaviour : Object
    {
        public bool isActiveAndEnabled = true;
        public Coroutine StartCoroutine(IEnumerator steps) => Scheduler.Start(steps);
        public void StopCoroutine(Coroutine sequence) => Scheduler.Stop(sequence);
    }
    public class SerializeField : System.Attribute { }
    public class MinAttribute : System.Attribute { public MinAttribute(float value) {} }
    public class Rigidbody2D : Object { }
    public class Collider2D : Object { public Rigidbody2D attachedRigidbody; public bool enabled; }
    public class Sprite : Object { public int frame; }
    public class SpriteRenderer : Object
    {
        Sprite current;
        public readonly List<int> displayed = new List<int>();
        public Sprite sprite { get => current; set { current=value; displayed.Add(value.frame); } }
    }
    public class WaitForSecondsRealtime { public float seconds; public WaitForSecondsRealtime(float value) => seconds=value; }
    public static class Mathf { public static float Max(float a,float b) => System.Math.Max(a,b); }
    public class Coroutine
    {
        public Stack<IEnumerator> stack = new Stack<IEnumerator>();
        public double resumeAt;
    }
    public static class Scheduler
    {
        public static double Now;
        static readonly List<Coroutine> active = new List<Coroutine>();
        public static Coroutine Start(IEnumerator steps)
        {
            var sequence = new Coroutine(); sequence.stack.Push(steps); active.Add(sequence); Pump(sequence); return sequence;
        }
        public static void Stop(Coroutine sequence) => active.Remove(sequence);
        static void Pump(Coroutine sequence)
        {
            while(sequence.stack.Count>0)
            {
                var top=sequence.stack.Peek();
                if(!top.MoveNext()) { sequence.stack.Pop(); continue; }
                if(top.Current is IEnumerator nested) { sequence.stack.Push(nested); continue; }
                if(top.Current is WaitForSecondsRealtime wait) { sequence.resumeAt=Now+wait.seconds; return; }
                sequence.resumeAt=Now+.001; return;
            }
            active.Remove(sequence);
        }
        public static void Advance(double seconds)
        {
            double target=Now+seconds;
            while(true)
            {
                Coroutine next=null;
                foreach(var sequence in active) if(sequence.resumeAt<=target && (next==null || sequence.resumeAt<next.resumeAt)) next=sequence;
                if(next==null) break;
                Now=next.resumeAt; Pump(next);
            }
            Now=target;
        }
    }
}
public class BuildingPortal : UnityEngine.Object
{
    public int transitions;
    public bool CompleteAirlockTransition(UnityEngine.Collider2D other) { transitions++; return true; }
}
