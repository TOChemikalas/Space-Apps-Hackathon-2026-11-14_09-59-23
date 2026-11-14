using UnityEngine;

// Reusable paired doorway: identity is the configured player Rigidbody2D, not an object name.
public class BuildingEntrance : MonoBehaviour
{
    [SerializeField] BuildingEntrance pairedDoor;
    [SerializeField] Rigidbody2D player;
    [SerializeField] Camera worldCamera;
    [SerializeField] Transform arrival;
    [SerializeField] bool entersInterior;
    [SerializeField] Transform interiorRoot;
    [SerializeField] Vector2 interiorCenter;
    [SerializeField] Vector2 interiorSize = new Vector2(25, 12.5f);
    [SerializeField] bool preserveExteriorZoom;
    [SerializeField, Min(0)] float transitionCooldown = .5f;
    float blockedUntil;
    bool hasEntered;
    Transform cameraParent;
    Vector3 cameraLocalPosition;
    float cameraSize;

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (!pairedDoor || !arrival || !worldCamera || !player || other.attachedRigidbody != player
            || Time.unscaledTime < blockedUntil) return;
        if (entersInterior ? hasEntered : !pairedDoor.hasEntered) return;
        blockedUntil = pairedDoor.blockedUntil = Time.unscaledTime + transitionCooldown;
        OnTransition(entersInterior);
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0;
        player.position = arrival.position;
        player.transform.position = new Vector3(arrival.position.x, arrival.position.y, player.transform.position.z);
        if (entersInterior)
        {
            hasEntered = true;
            cameraParent = worldCamera.transform.parent;
            cameraLocalPosition = worldCamera.transform.localPosition;
            cameraSize = worldCamera.orthographicSize;
            Vector2 center = interiorRoot ? (Vector2)interiorRoot.position : interiorCenter;
            float depth = worldCamera.transform.position.z;
            worldCamera.transform.SetParent(null, true);
            worldCamera.transform.position = new Vector3(center.x, center.y, depth);
            float fitSize = Mathf.Max(interiorSize.y * .5f, interiorSize.x / (2 * worldCamera.aspect));
            worldCamera.orthographicSize = preserveExteriorZoom ? Mathf.Max(cameraSize, fitSize) : fitSize;
        }
        else
        {
            pairedDoor.hasEntered = false;
            worldCamera.transform.SetParent(pairedDoor.cameraParent, false);
            worldCamera.transform.localPosition = pairedDoor.cameraLocalPosition;
            worldCamera.orthographicSize = pairedDoor.cameraSize;
        }
        // Roots stay active: crops, energy, and the authoritative clock never reset on transitions.
        Physics2D.SyncTransforms();
    }

    protected virtual void OnTransition(bool inside) { }
}
