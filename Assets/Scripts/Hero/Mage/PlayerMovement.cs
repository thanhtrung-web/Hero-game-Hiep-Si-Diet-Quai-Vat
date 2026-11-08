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
    private PlayerStamina stamina; // [MỚI] thể lực dùng chung

    // [MỚI] Lật collider/ground check theo hướng nhìn (flipX chỉ lật sprite, không lật collider)
    private CapsuleCollider2D bodyCollider;
    private Vector2 baseColliderOffset;   // offset lúc flipX = false
    private float baseGroundCheckX;       // localPosition.x lúc flipX = false
    private bool lastFlipX;

    // [MỚI] Điểm đánh cận chiến (PlayerMeleeHit dùng attackPoint.position) - tự tìm theo tên nếu để trống
    [Header("Facing")]
    [SerializeField] private Transform attackPoint;
    private float baseAttackPointX;       // localPosition.x lúc flipX = false
    private float moveInput;
    private bool jumpRequested;
    private bool isRunning;
    private bool isGrounded;
    private bool isJumping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        stamina = GetComponent<PlayerStamina>(); // [MỚI]

        // [MỚI] Lưu giá trị gốc (quy về trạng thái flipX = false)
        float sign = spriteRenderer.flipX ? -1f : 1f;
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (bodyCollider != null)
            baseColliderOffset = new Vector2(bodyCollider.offset.x * sign, bodyCollider.offset.y);
        if (groundCheck != null)
            baseGroundCheckX = groundCheck.localPosition.x * sign;
        if (attackPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "AttackPoint")
                {
                    attackPoint = child;
                    break;
                }
            }
        }
        if (attackPoint != null)
            baseAttackPointX = attackPoint.localPosition.x * sign;

        lastFlipX = spriteRenderer.flipX;
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
        {
            jumpRequested = true;

            // Chặn Run animation ngay khi bắt đầu một cú nhảy hợp lệ.
            // isRunning vẫn giữ nguyên để không làm tụt tốc độ ngang trên không.
            if (isGrounded)
                isJumping = true;
        }

        bool shiftHeld = keyboard.leftShiftKey.isPressed ||
                         keyboard.rightShiftKey.isPressed;

        // [MỚI] Nhả Shift thì mở khóa kiệt sức (muốn chạy lại phải bấm Shift một lần nữa)
        if (stamina != null &&
            (keyboard.leftShiftKey.wasReleasedThisFrame || keyboard.rightShiftKey.wasReleasedThisFrame))
        {
            stamina.ClearExhausted();
        }

        // [MỚI] Chạy nước rút tiêu tốn thể lực; hết thể lực -> DrainSprint trả về false -> quay về đi bộ
        bool wantsToRun = Mathf.Abs(moveInput) > 0.01f && shiftHeld;
        isRunning = wantsToRun && (stamina == null || stamina.DrainSprint(Time.deltaTime));

        if (moveInput != 0f)
        {
            spriteRenderer.flipX = moveInput < 0f;
            ApplyFacing(spriteRenderer.flipX);
        }

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsRunning", isRunning && isGrounded && !isJumping);
    }

    // [MỚI] Đảo offset collider và vị trí ground check sang phía nhân vật đang quay mặt
    private void ApplyFacing(bool flipX)
    {
        if (flipX == lastFlipX)
            return;

        lastFlipX = flipX;
        float sign = flipX ? -1f : 1f;

        // Không thay đổi offset của Body Collider.
        // Giữ collider cố định khi nhân vật quay đầu.

        // Đổi hướng GroundCheck nếu cần
        if (groundCheck != null)
        {
            Vector3 pos = groundCheck.localPosition;
            pos.x = baseGroundCheckX * sign;
            groundCheck.localPosition = pos;
        }

        // Đổi hướng điểm tấn công
        if (attackPoint != null)
        {
            Vector3 pos = attackPoint.localPosition;
            pos.x = baseAttackPointX * sign;
            attackPoint.localPosition = pos;
        }
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
        isGrounded = groundCheck != null &&
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
            isJumping = true;
        }

        // The ground-check circle may still touch the floor during takeoff.
        if (velocity.y > 0.01f)
        {
            isGrounded = false;
        }

        // Khi đã chạm đất thì cho phép Walk/Run animation hoạt động trở lại.
        if (isGrounded && velocity.y <= 0.01f)
        {
            isJumping = false;
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