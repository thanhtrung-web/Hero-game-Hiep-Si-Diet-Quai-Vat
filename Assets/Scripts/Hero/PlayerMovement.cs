using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float jumpSpeed = 10f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.08f;
    [SerializeField] private LayerMask groundLayer;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;
    private float moveInput;
    private bool jumpRequested;
    private bool isRunning;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            moveInput = 0f;
            isRunning = false;
            jumpRequested = false;
            return;
        }
        Keyboard keyboard = Keyboard.current;
        moveInput = 0f;

        if (keyboard == null)
        {
            isRunning = false;
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsRunning", false);
            return;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            moveInput -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            moveInput += 1f;

        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        bool shiftHeld = keyboard.leftShiftKey.isPressed ||
                         keyboard.rightShiftKey.isPressed;
        isRunning = Mathf.Abs(moveInput) > 0.01f && shiftHeld;

        if (moveInput != 0f)
            spriteRenderer.flipX = moveInput < 0f;

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsRunning", isRunning);
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            Vector2 deadVelocity = rb.linearVelocity;
            deadVelocity.x = 0f;
            rb.linearVelocity = deadVelocity;
            isRunning = false;
            jumpRequested = false;
            return;
        }
        bool isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            ) != null;

        Vector2 velocity = rb.linearVelocity;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        velocity.x = moveInput * currentSpeed;

        if (jumpRequested && isGrounded && velocity.y <= 0.01f)
        {
            velocity.y = jumpSpeed;

            // Switch to Jump immediately when taking off.
            isGrounded = false;
        }

        // The ground-check circle may still touch the floor during takeoff.
        if (velocity.y > 0.01f)
        {
            isGrounded = false;
        }

        rb.linearVelocity = velocity;
        animator.SetBool("IsGrounded", isGrounded);

        jumpRequested = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
