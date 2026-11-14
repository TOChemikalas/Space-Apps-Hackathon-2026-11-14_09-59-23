using UnityEngine;

// Local proximity/outline only. The shared electrical UI owns focus and modal input.
public sealed class ElectricalInteractable : MonoBehaviour
{
    [SerializeField] ElectricalControls controls;
    [SerializeField] Collider2D solidBoundary;
    [SerializeField] Vector2 outlineSize = Vector2.one;
    [SerializeField] BoxCollider2D interactionRange;
    [SerializeField] Battery battery;
    [SerializeField] bool opensBattery;
    public Battery Battery => battery;
    public bool OpensBattery => opensBattery;
    public Collider2D SolidBoundary => solidBoundary;
    LineRenderer outline;
    Material outlineMaterial;

    void Awake()
    {
        outline = gameObject.AddComponent<LineRenderer>();
        outlineMaterial = new Material(Shader.Find("Sprites/Default"));
        outline.sharedMaterial = outlineMaterial;
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = 4;
        outline.startWidth = outline.endWidth = .035f;
        outline.startColor = outline.endColor = Color.white;
        outline.sortingLayerName = "Effects";
        outline.sortingOrder = 50;
        Vector2 half = outlineSize * .5f;
        outline.SetPosition(0, new Vector3(-half.x, -half.y, -.1f));
        outline.SetPosition(1, new Vector3(half.x, -half.y, -.1f));
        outline.SetPosition(2, new Vector3(half.x, half.y, -.1f));
        outline.SetPosition(3, new Vector3(-half.x, half.y, -.1f));
        outline.enabled = false;
    }

    public Vector2 ClosestPoint(Vector2 playerPosition)
    {
        Vector3 local = transform.InverseTransformPoint(playerPosition);
        local.x = Mathf.Clamp(local.x, -outlineSize.x * .5f, outlineSize.x * .5f);
        local.y = Mathf.Clamp(local.y, -outlineSize.y * .5f, outlineSize.y * .5f);
        local.z = 0;
        return transform.TransformPoint(local);
    }

    public void Highlight(bool visible) => outline.enabled = visible;

    public bool InRange(Vector2 position)
    {
        Bounds bounds = interactionRange.bounds;
        return position.x >= bounds.min.x - .1f && position.x <= bounds.max.x + .1f
            && position.y >= bounds.min.y - .1f && position.y <= bounds.max.y + .1f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (controls && controls.IsPlayer(other)) controls.AddCandidate(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (controls && controls.IsPlayer(other)) controls.RemoveCandidate(this);
    }

    void OnDisable()
    {
        if (controls) controls.RemoveCandidate(this);
        if (outline) outline.enabled = false;
    }

    void OnDestroy()
    {
        if (outlineMaterial) Destroy(outlineMaterial);
    }
}
