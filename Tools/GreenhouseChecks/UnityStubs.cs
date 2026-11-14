namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class MonoBehaviour : Object { }
    public class MinAttribute : System.Attribute { public MinAttribute(float min) {} }
    public class RangeAttribute : System.Attribute { public RangeAttribute(float min, float max) {} }
    public class DefaultExecutionOrder : System.Attribute { public DefaultExecutionOrder(int order) {} }
    public class SerializeField : System.Attribute { }
    public static class Time { public static float unscaledDeltaTime; public static float unscaledTime; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x=x; this.y=y; }
        public static Vector2 zero => new Vector2();
        public static implicit operator Vector2(Vector3 value) => new Vector2(value.x,value.y);
        public static implicit operator Vector3(Vector2 value) => new Vector3(value.x,value.y,0);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
    }
    public class Transform : Object
    {
        public Transform parent;
        public Vector3 position, localPosition;
        public void SetParent(Transform value, bool worldPositionStays) => parent=value;
    }
    public class Rigidbody2D : Object
    {
        public Vector2 position, linearVelocity;
        public float angularVelocity;
        public Transform transform = new Transform();
    }
    public class Collider2D : Object { public Rigidbody2D attachedRigidbody; }
    public class Camera : Object
    {
        public Transform transform = new Transform();
        public float orthographicSize=10, aspect=16f/9;
    }
    public static class Mathf { public static float Max(float a,float b) => System.Math.Max(a,b); }
    public static class Physics2D { public static void SyncTransforms() {} }
}
// Only the UI transition hook is represented here; greenhouse growth uses its production model.
public class GreenhouseInteractionUI : UnityEngine.Object
{
    public bool IsInside { get; private set; }
    public void SetInside(bool inside) => IsInside = inside;
}
