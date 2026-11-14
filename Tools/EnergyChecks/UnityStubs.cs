// Minimal engine stand-ins: the harness compiles the production calculation scripts.
namespace UnityEngine
{
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled = true;
        public static implicit operator bool(MonoBehaviour value) => value != null;
    }
    public class SerializeField : System.Attribute { }
    public class MinAttribute : System.Attribute { public MinAttribute(float value) {} }
    public class RangeAttribute : System.Attribute { public RangeAttribute(float min, float max) {} }
    public class DefaultExecutionOrder : System.Attribute { public DefaultExecutionOrder(int value) {} }
    public static class Time { public static float unscaledDeltaTime; }
    public struct Vector3
    {
        public Vector3(float x, float y, float z) {}
        public Vector3 normalized => this;
    }
    public static class Mathf
    {
        public static float Repeat(float x, float length) => x - (float)System.Math.Floor(x / length) * length;
        public static float Max(float x, float y) => System.Math.Max(x, y);
        public static float Clamp01(float x) => System.Math.Clamp(x, 0, 1);
    }
}
public class MarsLighting : UnityEngine.MonoBehaviour { public float startingLs = 90; }
