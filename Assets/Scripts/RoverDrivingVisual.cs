using UnityEngine;

// Attach to Rover ROOT (the object with Rigidbody2D and RoverVehicle).
// Assign the SpriteRenderer on a CHILD object named Visual.
[RequireComponent(typeof(Rigidbody2D))]
public class RoverDrivingVisual : MonoBehaviour
{
    [Header("Child visual (not the Rigidbody / collider)")]
    [SerializeField] private SpriteRenderer visual;

    [Header("Eight direction sprites")]
    [SerializeField] private Sprite north;
    [SerializeField] private Sprite northEast;
    [SerializeField] private Sprite east;
    [SerializeField] private Sprite southEast;
    [SerializeField] private Sprite south;
    [SerializeField] private Sprite southWest;
    [SerializeField] private Sprite west;
    [SerializeField] private Sprite northWest;

    [Header("Suspension motion (world units)")]
    [SerializeField, Min(0f)] private float bounceHeight = 0.055f;
    [SerializeField, Min(0f)] private float sideSway = 0.022f;
    [SerializeField, Min(0f)] private float bodyRollDegrees = 1.2f;
    [SerializeField, Min(0.01f)] private float bumpsPerUnit = 0.65f;
    [SerializeField, Min(0.01f)] private float fullBounceAtSpeed = 7f;
    [SerializeField, Min(0.01f)] private float smoothing = 12f;

    private Rigidbody2D body;
    private Transform visualTransform;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private float distanceTravelled;
    private float bounceStrength;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        if (visual == null)
        {
            Transform child = transform.Find("Visual");
            if (child != null) child.TryGetComponent(out visual);
        }

        if (visual == null || visual.transform == transform || !visual.transform.IsChildOf(transform))
        {
            Debug.LogError("RoverDrivingVisual: Create a child named Visual with a SpriteRenderer, " +
                           "then assign that renderer in the Inspector.", this);
            enabled = false;
            return;
        }

        visualTransform = visual.transform;
        restPosition = visualTransform.localPosition;
        restRotation = visualTransform.localRotation;
        if (south != null) visual.sprite = south;
    }

    private void LateUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        float speed = velocity.magnitude;

        if (speed > 0.08f)
        {
            Sprite facing = SpriteForDirection(velocity);
            if (facing != null) visual.sprite = facing;
            distanceTravelled += speed * Time.deltaTime;
        }

        // No motion when parked; smoothly start and stop bouncing with speed.
        float targetStrength = speed > 0.08f ? Mathf.Clamp01(speed / fullBounceAtSpeed) : 0f;
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        bounceStrength = Mathf.Lerp(bounceStrength, targetStrength, t);

        // Multiple frequencies feel less mechanical than a simple looping up/down sine.
        float phase = distanceTravelled * Mathf.PI * 2f * bumpsPerUnit;
        float vertical = 0.75f * Mathf.Sin(phase)
                       + 0.25f * Mathf.Sin(phase * 1.73f + 0.7f);
        float lateral = Mathf.Sin(phase * 0.53f + 0.5f);

        Vector3 offset = new Vector3(lateral * sideSway, vertical * bounceHeight, 0f)
                         * bounceStrength;
        Quaternion roll = restRotation * Quaternion.Euler(
            0f, 0f, lateral * bodyRollDegrees * bounceStrength);

        visualTransform.localPosition = Vector3.Lerp(
            visualTransform.localPosition, restPosition + offset, t);
        visualTransform.localRotation = Quaternion.Slerp(
            visualTransform.localRotation, roll, t);
    }

    private Sprite SpriteForDirection(Vector2 velocity)
    {
        // 0=E, 1=NE, 2=N, 3=NW, 4=W, 5=SW, 6=S, 7=SE.
        switch (DirectionSector(velocity))
        {
            case 0: return east;
            case 1: return northEast;
            case 2: return north;
            case 3: return northWest;
            case 4: return west;
            case 5: return southWest;
            case 6: return south;
            default: return southEast;
        }
    }

    internal static int DirectionSector(Vector2 velocity)
    {
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        return (Mathf.RoundToInt(angle / 45f) + 8) % 8;
    }
}
