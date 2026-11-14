namespace UnityEngine
{
    public class MonoBehaviour { }
    public class MinAttribute : System.Attribute { public MinAttribute(float min) {} }
    public class RangeAttribute : System.Attribute { public RangeAttribute(float min, float max) {} }
    public class DefaultExecutionOrder : System.Attribute { public DefaultExecutionOrder(int order) {} }
    public static class Time { public static float unscaledDeltaTime; }
}
