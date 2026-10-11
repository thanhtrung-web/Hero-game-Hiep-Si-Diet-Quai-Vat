using UnityEngine;

public class BossMap4Controller : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float attackRange = 2f;

    [Header("References")]
    public Animator animator;
    public Transform player; // Kéo thả Player vào đây trong Inspector
    private Rigidbody2D rb;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        // Ví dụ logic đơn giản: Kiểm tra khoảng cách tới Player để tấn công hoặc di chuyển
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= attackRange)
            {
                // Gọi hàm tấn công
                Attack();
            }
            else if (distanceToPlayer < 10f)
            {
                // Gọi hàm di chuyển theo Player
                MoveTowardsPlayer();
            }
            else
            {
                // Đứng yên (Idle)
                SetIdle();
            }
        }
    }

    void MoveTowardsPlayer()
    {
        // Di chuyển hướng về phía Player
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        // Kích hoạt animation đi bộ
        animator.SetBool("IsWalking", true);
        animator.SetBool("IsATK", false);

        // Lật mặt Boss hướng về phía Player
        if (direction.x > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void Attack()
    {
        rb.linearVelocity = Vector2.zero; // Dừng di chuyển khi tấn công
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsATK", true);
    }

    void SetIdle()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsATK", false);
    }

    // Hàm nhận sát thương (gọi từ vũ khí của Player khi đánh trúng Boss)
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Kích hoạt animation nhận sát thương
        animator.SetTrigger("IsTakeDmg");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // Kích hoạt animation chết (Trigger "Die" mà chúng ta vừa tạo)
        animator.SetTrigger("Die");

        // Vô hiệu hóa Collider để không tương tác nữa
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Hủy đối tượng Boss sau vài giây khi chạy xong animation chết
        Destroy(gameObject, 3f);
    }
}