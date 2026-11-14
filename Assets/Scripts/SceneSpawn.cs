using UnityEngine;

public sealed class SceneSpawn : MonoBehaviour
{
    internal static string PendingId;

    [SerializeField] private string spawnId;
    [SerializeField, Min(.01f), Tooltip("Multiplier for the astronaut sprite at this destination. Use 1 for its original size.")]
    private float astronautVisualScale = 1f;

    public string Id => spawnId;

    public void PlacePlayer(Rigidbody2D player)
    {
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
        Vector3 destination = transform.position;
        player.position = destination;
        player.transform.position = new Vector3(destination.x, destination.y, player.transform.position.z);
        if (player.TryGetComponent<AstronautController>(out var astronaut))
            astronaut.SetVisualScale(astronautVisualScale);
        Physics2D.SyncTransforms();
    }
}
