using UnityEngine;

public sealed class DroneCrashSite : MonoBehaviour
{
    public const string MessageId = "survey.drone_crash";
    [SerializeField] Transform mainBase;
    [SerializeField] Texture2D artwork;
    [SerializeField] SpriteRenderer terrain;
    [SerializeField] Vector2 offsetMeters = new Vector2(3000, 0);
    Sprite sprite;
    public static DroneCrashSite Instance { get; private set; }
    public Vector2 Coordinates => offsetMeters;
    public Vector3 BasePosition => mainBase ? mainBase.position : Vector3.zero;

    void Awake()
    {
        Instance = this;
        // This mission uses one Unity world unit per metre.
        transform.position = (mainBase ? mainBase.position : Vector3.zero) + (Vector3)offsetMeters;
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        if (!artwork) return;
        sprite = Sprite.Create(artwork, new Rect(0, 0, artwork.width, artwork.height), new Vector2(.5f, .5f), artwork.width / 18f);
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 0;
        var obstacle = gameObject.AddComponent<BoxCollider2D>();
        obstacle.size = new Vector2(5, 3);
        gameObject.layer = LayerMask.NameToLayer("Default");
        ExtendTerrain();
    }

    void ExtendTerrain()
    {
        if (!terrain || !terrain.sprite || terrain.bounds.size.x <= 0) return;
        Vector3 size = terrain.bounds.size, center = terrain.bounds.center;
        int columns = Mathf.CeilToInt((transform.position.x - center.x) / size.x);
        for (int x = 0; x <= columns; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                var tile = new GameObject("Crash site approach terrain");
                tile.transform.SetParent(transform, false);
                tile.transform.position = center + new Vector3(x * size.x, y * size.y, 0);
                tile.transform.rotation = terrain.transform.rotation;
                tile.transform.localScale = terrain.transform.lossyScale;
                var surface = tile.AddComponent<SpriteRenderer>();
                surface.sprite = terrain.sprite;
                surface.sharedMaterial = terrain.sharedMaterial;
                surface.color = terrain.color;
                surface.sortingLayerID = terrain.sortingLayerID;
                surface.sortingOrder = terrain.sortingOrder;
            }
    }

    public TransmissionDefinition DiscoveryMessage() => new TransmissionDefinition {
        id = MessageId, sender = "SURFACE SURVEY", subject = "Drone crash site located",
        body = $"A drone was located at ({offsetMeters.x:0}, {offsetMeters.y:0}) metres relative to the main base.\n\nOpen this message to track the crash site on your minimap.",
        fromEarth = false, trigger = TransmissionTrigger.MissionTime, sol = 1
    };

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (sprite) Destroy(sprite);
    }
}
