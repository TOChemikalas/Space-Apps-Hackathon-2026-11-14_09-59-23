using UnityEngine;

public sealed class SceneSpawn : MonoBehaviour
{
    internal static string PendingId;

    [SerializeField] private string spawnId;
    [SerializeField, Min(.01f), Tooltip("Multiplier for the astronaut sprite at this destination. Use 1 for its original size.")]
    private float astronautVisualScale = 1f;
    [SerializeField] Camera worldCamera;
    [SerializeField] Transform interiorFocus;
    [SerializeField] Vector2 interiorViewSize;
    [SerializeField] SceneSpawn restoreViewFrom;
    Transform savedParent;
    Vector3 savedLocalPosition, savedLocalScale;
    Quaternion savedLocalRotation;
    float savedZoom;
    bool savedView;

    public string Id => spawnId;

    public void BindCamera(Camera camera) => worldCamera = camera;
    public void SetRestoreViewFrom(SceneSpawn interiorSpawn) => restoreViewFrom = interiorSpawn;

    public void PlacePlayer(Rigidbody2D player)
    {
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
        Vector3 destination = transform.position;
        player.position = destination;
        player.transform.position = new Vector3(destination.x, destination.y, player.transform.position.z);
        if (player.TryGetComponent<AstronautController>(out var astronaut))
            astronaut.SetVisualScale(astronautVisualScale);
        if (worldCamera && interiorFocus)
        {
            Transform view = worldCamera.transform;
            savedParent = view.parent;
            savedLocalPosition = view.localPosition;
            savedLocalRotation = view.localRotation;
            savedLocalScale = view.localScale;
            savedZoom = worldCamera.orthographicSize;
            savedView = true;
            float depth = view.position.z;
            view.SetParent(null, true);
            view.position = new Vector3(interiorFocus.position.x, interiorFocus.position.y, depth);
            worldCamera.orthographicSize = Mathf.Max(savedZoom,
                Mathf.Max(interiorViewSize.y * .5f, interiorViewSize.x / (2 * worldCamera.aspect)));
        }
        else if (restoreViewFrom) restoreViewFrom.RestoreView();
        Physics2D.SyncTransforms();
    }

    void RestoreView()
    {
        if (!savedView || !worldCamera) return;
        Transform view = worldCamera.transform;
        view.SetParent(savedParent, false);
        view.localPosition = savedLocalPosition;
        view.localRotation = savedLocalRotation;
        view.localScale = savedLocalScale;
        worldCamera.orthographicSize = savedZoom;
        savedView = false;
    }
}
