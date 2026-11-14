using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AirlockDoor))]
public sealed class RoomDoor : MonoBehaviour
{
    [SerializeField] private BoxCollider2D approach;
    [SerializeField] private Rigidbody2D player;

    private AirlockDoor door;
    private Collider2D playerCollider;
    private Coroutine animation;

    private void Awake()
    {
        door = GetComponent<AirlockDoor>();
        BindPlayer(player);
    }

    public void BindPlayer(Rigidbody2D body)
    {
        player = body;
        playerCollider = body ? body.GetComponent<Collider2D>() : null;
    }

    private void OnEnable() => door.CloseImmediately();

    private void Update()
    {
        if (animation != null || !door.IsConfigured || !approach || !playerCollider) return;
        bool near = PlayerIsNear();
        if (near != door.IsOpen) animation = StartCoroutine(Animate(near));
    }

    private IEnumerator Animate(bool open)
    {
        yield return door.Animate(open, true, open ? null : PlayerIsNear);
        animation = null;
    }

    private bool PlayerIsNear() => playerCollider && approach.bounds.Intersects(playerCollider.bounds);

    private void OnDisable()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
        if (door) door.CloseImmediately();
    }
}
