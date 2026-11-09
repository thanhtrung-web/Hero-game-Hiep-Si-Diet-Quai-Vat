using System.Collections;
using UnityEngine;

public class TrollController : EnemyHealth
{
    [Header("Stats")]
    public float moveSpeed = 2f;
    public float attackRange = 2.6f;
    public float agroRange = 6f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = -99f;

    [Header("Patrol Boundaries (Tùy chọn: Kéo LeftPoint & RightPoint)")]
    public Transform leftLimit;
    public Transform rightLimit;
    public float idleTimeAtPoint = 1.5f;
    private float idleTimer;
    private bool isWaiting = false;
    private bool movingRight = true;

    [Header("Audio (Tùy chọn)")]
    public AudioSource audioSource;
    [Range(0f, 1f)] public float soundVolume = 0.3f;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;

    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D col;

    protected override void Awake()
    {
        if (maxHealth <= 3)
            maxHealth = 120;

        // Nếu trong Inspector vẫn lưu giá trị attackRange = 1.8 cũ (nhỏ hơn kích thước Collider của Troll),
        // tự động nâng lên 2.6 để Troll không bị vướng Collider của chính nó khi tiến lại đánh Player
        if (attackRange < 2.5f)
            attackRange = 2.6f;

        base.Awake();

        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Bật Simulated cho Rigidbody2D để nhận sát thương
        if (rb != null)
        {
            rb.simulated = true;
        }

        // Thu gọn CapsuleCollider2D nếu đang bị mặc định quá to (40x42 bao cả khoảng trống quanh chùy)
        CapsuleCollider2D capsule = col as CapsuleCollider2D;
        if (capsule != null && capsule.size.x > 30f)
        {
            capsule.size = new Vector2(22f, 33.6f);
        }
    }

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Tự động tìm LeftPoint và RightPoint hoặc PointA và PointB từ Object cha
        Transform groupParent = transform.parent;
        if (groupParent != null)
        {
            if (leftLimit == null)
            {
                leftLimit = groupParent.Find("LeftPoint");
                if (leftLimit == null) leftLimit = groupParent.Find("PointA");
            }
            if (rightLimit == null)
            {
                rightLimit = groupParent.Find("RightPoint");
                if (rightLimit == null) rightLimit = groupParent.Find("PointB");
            }
        }

        // Tự động tìm Player
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // Đang trong thời gian ra đòn / hồi chiêu thì đứng yên hoàn thành đòn đánh
        if (Time.time < lastAttackTime + attackCooldown)
        {
            SetWalking(false);
            return;
        }

        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;
        bool inAttackRange = IsPlayerInAttackRange(attackRange);

        // 1. TRẠNG THÁI TẤN CÔNG
        if (inAttackRange)
        {
            FacePlayer();
            SetWalking(false);

            if (animator != null)
                animator.SetTrigger("Attack");
            lastAttackTime = Time.time;

            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound, soundVolume);
        }
        // 2. TRẠNG THÁI ĐUỔI THEO PLAYER
        else if (distanceToPlayer <= agroRange)
        {
            FacePlayer();
            isWaiting = false;
            SetWalking(true);

            float direction = player.position.x > transform.position.x ? 1f : -1f;
            transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

            if (spriteRenderer != null)
                spriteRenderer.flipX = (direction < 0);
        }
        // 3. TRẠNG THÁI TUẦN TRA HOẶC ĐỨNG YÊN
        else
        {
            PatrolLogic();
        }
    }

    bool IsPlayerInAttackRange(float range)
    {
        if (player == null) return false;

        float dist2D = Vector2.Distance(transform.position, player.position);
        if (dist2D <= range) return true;

        float dx = Mathf.Abs(transform.position.x - player.position.x);
        float dy = Mathf.Abs(transform.position.y - player.position.y);
        return (dx <= range && dy <= 2.5f);
    }

    void PatrolLogic()
    {
        if (leftLimit == null || rightLimit == null)
        {
            SetWalking(false);
            return;
        }

        if (isWaiting)
        {
            SetWalking(false);
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeAtPoint)
            {
                isWaiting = false;
            }
            return;
        }

        SetWalking(true);

        if (movingRight)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
            if (spriteRenderer != null) spriteRenderer.flipX = false;

            if (transform.position.x >= rightLimit.position.x)
            {
                movingRight = false;
                StartWaiting();
            }
        }
        else
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
            if (spriteRenderer != null) spriteRenderer.flipX = true;

            if (transform.position.x <= leftLimit.position.x)
            {
                movingRight = true;
                StartWaiting();
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        idleTimer = 0f;
    }

    void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        float directionX = player.position.x - transform.position.x;
        spriteRenderer.flipX = (directionX < 0);
    }

    void SetWalking(bool walking)
    {
        if (animator != null)
            animator.SetBool("isMoving", walking);
    }

    // ==========================================
    // GỌI BẰNG ANIMATION EVENT Ở FRAME ĐÁNH
    // ==========================================
    public void AnimationAttackHit()
    {
        if (isDead) return;

        if (IsPlayerInAttackRange(attackRange + 0.8f))
        {
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("<color=yellow>Troll đánh trúng Player gây " + attackDamage + " sát thương!</color>");
        }
    }

    // ==========================================
    // NHẬN SÁT THƯƠNG TỪ PLAYER (Ghi đè EnemyHealth)
    // ==========================================
    public override void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log("<color=cyan>Troll nhận " + damage + " DMG! Máu còn: " + currentHealth + "</color>");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hurt");

        if (takeDamageSound != null && audioSource != null)
            audioSource.PlayOneShot(takeDamageSound, soundVolume);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hurt");
            animator.SetBool("isMoving", false);
            animator.SetBool("isDead", true);
        }

        if (dieSound != null && audioSource != null)
            audioSource.PlayOneShot(dieSound, soundVolume);

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Destroy(gameObject, 3.5f);
    }
}
