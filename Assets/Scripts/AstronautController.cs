
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class AstronautController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;

        float x = 0f;
        float y = 0f;

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                x -= 1f;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                x += 1f;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                y += 1f;

            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                y -= 1f;
        }

        moveInput = new Vector2(x, y).normalized;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Select one of four visual directions.
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                facing = new Vector2(Mathf.Sign(moveInput.x), 0);
            else
                facing = new Vector2(0, Mathf.Sign(moveInput.y));
        }

        if (animator != null)
        {
            animator.SetFloat("FaceX", facing.x);
            animator.SetFloat("FaceY", facing.y);
            animator.SetBool("IsMoving", isMoving);
        }
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        if (animator != null)
            animator.SetBool("IsMoving", false);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + moveInput * moveSpeed * Time.fixedDeltaTime
        );
    }
}
