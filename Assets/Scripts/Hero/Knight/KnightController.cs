using UnityEngine;

public class KnightController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpForce = 6f;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded;
    private bool isDefending;
    private Rigidbody2D rb;
    private Animator anim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {

    }

    void Update()
    {
        HandleDefend();
        HandleMovement();
        HandleJump();
        HandleAttack();
        UpdateAnimation();
    }

    private void HandleDefend()
    {
        // Giữ phím K hoặc Chuột phải để giơ khiên
        isDefending = Input.GetKey(KeyCode.K) || Input.GetMouseButton(1);
    }

    private void HandleMovement()
    {
        if (isDefending)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = sprintSpeed;
        }

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        if (moveInput > 0)
            transform.localScale = new Vector3((float)1.5, (float)1.5, (float)1.5);
        else if (moveInput < 0)
            transform.localScale = new Vector3((float)-1.5, (float)1.5, (float)1.5);
    }

    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (isDefending) return;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void HandleAttack()
    {
        if (isDefending) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            // Nếu đang chạy nhanh (Sprint) -> kích hoạt Run Attack
            if (Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(rb.linearVelocity.x) > 4.5f && isGrounded)
            {
                anim.SetTrigger("runAttack");
                return;
            }

            // Combo đánh 1-2-3 thông thường
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Knight_Attack1"))
            {
                anim.SetTrigger("attack2");
            }
            else if (state.IsName("Knight_Attack2"))
            {
                anim.SetTrigger("attack3");
            }
            else
            {
                anim.SetTrigger("attack1");
            }
        }
    }

    private void UpdateAnimation()
    {
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isDefending", isDefending);
    }

    // Các hàm nhận sát thương và chết (sẵn sàng kết nối với Quái vật)
    public void TakeDamage()
    {
        if (isDefending)
        {
            // Đang đỡ khiên -> chặn sát thương
            return;
        }

        anim.SetTrigger("hurt");
    }

    public void Die()
    {
        anim.SetTrigger("dead");
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }
}
