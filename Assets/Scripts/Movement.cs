
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class AstronautMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            movement = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            x -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            x += 1f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            y += 1f;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            y -= 1f;

        movement = new Vector2(x, y).normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }
}
