namespace UnityEngine
{
    public class SerializeField : System.Attribute { }
    public class MinAttribute : System.Attribute { public MinAttribute(float minimum) {} }
    public class RangeAttribute : System.Attribute { public RangeAttribute(float minimum, float maximum) {} }
}
