using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runMultiplier = 2f;
    [SerializeField] private float maxTimeBetweenTaps = 0.3f;
    [SerializeField] private float jumpForce = 5f;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    private bool isGrounded;
    private bool canDoubleJump;
    private bool isSprinting;

    private float lastShiftTapTime = -1f;

    //private Animator animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleSprintInput();
        HandleMovement();
        HandleJump();
        UpdateAnimation();
    }

    private void HandleSprintInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            float timeSinceLastTap = Time.time - lastShiftTapTime;

            // Nhấn Shift lần hai trong thời gian cho phép để bật chạy nhanh.
            isSprinting = timeSinceLastTap <= maxTimeBetweenTaps;

            lastShiftTapTime = Time.time;
        }
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = moveSpeed * (isSprinting ? runMultiplier : 1f);

        rb.linearVelocity = new Vector2(
            moveInput * currentSpeed,
            rb.linearVelocity.y
        );

        if (anim != null)
            anim.SetBool("isRunning", moveInput != 0);

        if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
    }

    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            0.2f,
            groundLayer
        );

        if (isGrounded)
            canDoubleJump = true;

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                rb.linearVelocity =
                    new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (canDoubleJump)
            {
                rb.linearVelocity =
                    new Vector2(rb.linearVelocity.x, jumpForce);

                canDoubleJump = false;
            }
        }
    }

    private void UpdateAnimation() 
    { 
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        bool isJumping = !isGrounded;
        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isJumping", isJumping);
    }
}