using UnityEngine;

public class BossMap4Controller : EnemyHealth
{
    [Header("Stats")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public float attackRange = 3.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 2.2f;
    private float lastAttackTime = -999f;

    [Header("References")]
    public Transform player;
    private Rigidbody2D rb;
    private CapsuleCollider2D bossCollider;

    [Header("Audio (Dùng 1 Audio Source)")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;
    public AudioClip idleSound;

    // 🔒 CỜ KHÓA TUYỆT ĐỐI: Đảm bảo Idle Sound chỉ kêu đúng 1 LẦN DUY NHẤT
    private bool hasPlayedIdleSound = false;

    protected override void Awake()
    {
        if (maxHealth <= 3)
        {
            maxHealth = 100;
        }

        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        bossCollider = GetComponent<CapsuleCollider2D>();

        // Đảm bảo Rigidbody2D bật Simulated để nhận va chạm đòn đánh từ Player
        if (rb != null)
        {
            rb.simulated = true;
        }

        // Thu gọn chiều ngang Collider nếu đang quá bè (4.93m) để Player tiếp cận sát thân Boss
        if (bossCollider != null && bossCollider.size.x > 3.0f)
        {
            bossCollider.size = new Vector2(2.5f, bossCollider.size.y);
        }

        // Đảm bảo tầm đánh đủ lớn so với kích thước Collider cao lớn của Boss Frost
        if (attackRange < 3.2f)
        {
            attackRange = 3.5f;
        }

        if (attackCooldown < 2.1f)
        {
            attackCooldown = 2.2f;
        }
    }

    void Start()
    {
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

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 1. Kiểm tra nếu đang trong thời gian hồi chiêu (Cooldown) thì khựng lại chờ
        if (Time.time < lastAttackTime + attackCooldown)
        {
            StopMoving();
            return;
        }

        // 2. Logic di chuyển & tấn công
        if (IsPlayerInAttackRange(attackRange))
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

    private bool IsPlayerInAttackRange(float range)
    {
        if (player == null) return false;

        float dist2D = Vector2.Distance(transform.position, player.position);
        float distX = Mathf.Abs(transform.position.x - player.position.x);
        float distY = Mathf.Abs(transform.position.y - player.position.y);

        // Do Boss Frost rất cao (size Y ~ 5.8), tâm của Boss nằm cao hơn tâm Player ~1.8m
        // Nên kiểm tra khoảng cách ngang + biên Collider giúp Boss đánh chuẩn xác khi áp sát
        if (bossCollider != null)
        {
            float edgeDist = Mathf.Sqrt(bossCollider.bounds.SqrDistance(player.position));
            if (edgeDist <= 1.2f) return true;
        }

        return dist2D <= range || (distX <= range && distY <= 3.5f);
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
        animator.ResetTrigger("IsTakeDmg");
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
        if (animator != null)
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
            if (IsPlayerInAttackRange(attackRange + 0.8f))
            {
                player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    void SetIdle()
    {
        StopMoving();
    }

    // ==========================================
    // NHẬN SÁT THƯƠNG TỪ PLAYER (Ghi đè EnemyHealth)
    // ==========================================
    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log("<color=cyan>Boss Frost nhận " + damage + " DMG! Máu còn: " + currentHealth + "</color>");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("IsTakeDmg");
        StopMoving();

        if (takeDamageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(takeDamageSound);
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.RoundToInt(damage));
    }

    protected override void Die()
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
