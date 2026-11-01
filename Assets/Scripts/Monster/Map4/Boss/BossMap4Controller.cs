using UnityEngine;

public class BossMap4Controller : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("References")]
    public Animator animator;
    public Transform player;
    private Rigidbody2D rb;
    private bool isDead = false;

    [Header("Audio (Dùng 1 Audio Source)")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;
    public AudioClip idleSound;

    // 🔒 CỜ KHÓA TUYỆT ĐỐI: Đảm bảo Idle Sound chỉ kêu đúng 1 LẦN DUY NHẤT
    private bool hasPlayedIdleSound = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // 1. Kiểm tra nếu đang trong thời gian hồi chiêu (Cooldown) thì khựng lại chờ
            if (Time.time < lastAttackTime + attackCooldown)
            {
                StopMoving();
                return;
            }

            // 2. Logic di chuyển & tấn công
            if (distanceToPlayer <= attackRange)
            {
                FacePlayer();
                Attack();
            }
            else if (distanceToPlayer <= detectionRange)
            {
                FacePlayer();
                PlayIdleSoundOnce();
                MoveTowardsPlayer();
            }
            else
            {
                SetIdle();
            }
        }
    }
    void FacePlayer()
    {
        if (player == null) return;
        float directionX = player.position.x - transform.position.x;

        if (directionX > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (directionX < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        animator.SetBool("IsWalking", true);
    }

    void Attack()
    {
        StopMoving();

        // Đặt lại thời gian tấn công trước khi gọi Trigger để khóa hành động di chuyển
        lastAttackTime = Time.time;

        // Kích hoạt animation tấn công
        animator.SetTrigger("IsATK");

        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("IsWalking", false);
    }

    // 🔊 PHÁT ĐÚNG 1 LẦN KHI NGƯỜI CHƠI TIẾN VÀO GẦN BOSS
    void PlayIdleSoundOnce()
    {
        if (audioSource == null || idleSound == null || player == null) return;

        // Chỉ phát khi người chơi đã tiến vào phạm vi gần Boss (detectionRange)
        if (!hasPlayedIdleSound && Vector2.Distance(transform.position, player.position) <= detectionRange)
        {
            audioSource.PlayOneShot(idleSound);
            hasPlayedIdleSound = true; // Khóa cờ ngay lập tức, không bao giờ mở lại
        }
    }

    public void DealDamageToPlayer()
    {
        if (isDead) return;

        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= attackRange + 0.5f)
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void SetIdle()
    {
        StopMoving();
    }

    // Hàm nhận sát thương chính của Boss
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        animator.SetTrigger("IsTakeDmg");
        StopMoving();

        if (takeDamageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(takeDamageSound);
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        StopMoving();

        // 1. Reset sạch toàn bộ Trigger tấn công/nhận đòn và ép chuyển sang BossDie
        if (animator != null)
        {
            animator.ResetTrigger("IsATK");
            animator.ResetTrigger("IsTakeDmg");
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("IsDie");
        }

        if (dieSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSound);
        }

        // Vô hiệu hóa va chạm để Player có thể đi xuyên qua xác
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Triệt tiêu vận tốc rơi và khóa trọng lực, chống rơi xuyên bản đồ
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, 3f);
    }
}
