using UnityEngine;

// Paired doorway triggers keep both areas in the same scene and preserve simulation state.
public sealed class GreenhouseEntrance : MonoBehaviour
{
    [SerializeField] GreenhouseEntrance pairedDoor;
    [SerializeField] GreenhouseInteractionUI greenhouse;
    [SerializeField] Rigidbody2D player;
    [SerializeField] Camera worldCamera;
    [SerializeField] Transform arrival;
    [SerializeField] bool entersInterior;
    [SerializeField] Vector2 interiorCenter = new Vector2(100, 0);
    [SerializeField] Vector2 interiorSize = new Vector2(25, 12.5f);
    [SerializeField, Min(0)] float transitionCooldown = .5f;
    float blockedUntil;
    Transform cameraParent;
    Vector3 cameraLocalPosition;
    float cameraSize;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody != player || Time.unscaledTime < blockedUntil) return;
        blockedUntil = pairedDoor.blockedUntil = Time.unscaledTime + transitionCooldown;
        greenhouse.SetInside(entersInterior);
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
        player.position = arrival.position;
        // Update the Transform immediately as well, before rendering and proximity checks.
        player.transform.position = new Vector3(arrival.position.x, arrival.position.y, player.transform.position.z);
        if (entersInterior)
        {
            cameraParent = worldCamera.transform.parent;
            cameraLocalPosition = worldCamera.transform.localPosition;
            cameraSize = worldCamera.orthographicSize;
            worldCamera.transform.SetParent(null, true);
            worldCamera.transform.position = new Vector3(interiorCenter.x, interiorCenter.y, -10);
            worldCamera.orthographicSize = Mathf.Max(interiorSize.y * .5f, interiorSize.x / (2 * worldCamera.aspect));
        }
        else
        {
            worldCamera.transform.SetParent(pairedDoor.cameraParent, false);
            worldCamera.transform.localPosition = pairedDoor.cameraLocalPosition;
            worldCamera.orthographicSize = pairedDoor.cameraSize;
        }
        Physics2D.SyncTransforms();
    }
}
