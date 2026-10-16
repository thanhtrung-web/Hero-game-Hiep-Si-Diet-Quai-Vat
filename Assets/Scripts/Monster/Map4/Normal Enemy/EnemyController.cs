using UnityEngine;

public class GroupNormalEnemy : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 2f;
    public float attackRange = 1.8f;
    public float agroRange = 6f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Health")]
    public float maxHealth = 30f;
    private float currentHealth;

    [Header("Patrol Boundaries (Kéo LeftPoint & RightPoint vào đây)")]
    public Transform leftLimit;
    public Transform rightLimit;
    public float idleTimeAtPoint = 1.5f;
    private float idleTimer;
    private bool isWaiting = false;
    private bool movingRight = true;

    [Header("References")]
    private Transform player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Tự động tìm điểm LeftPoint và RightPoint từ Object cha (Group_Golem) nếu bạn quên kéo thả
        Transform groupParent = transform.parent;
        if (groupParent != null)
        {
            if (leftLimit == null) leftLimit = groupParent.Find("LeftPoint");
            if (rightLimit == null) rightLimit = groupParent.Find("RightPoint");
        }

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
    }

    void Update()
    {
        if (isDead) return;

        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        // --- 1. TRẠNG THÁI TẤN CÔNG ---
        if (distanceToPlayer <= attackRange)
        {
            FacePlayer();
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsATK", true);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                TriggerAttackEvent();
                lastAttackTime = Time.time;
            }
        }
        // --- 2. TRẠNG THÁI RƯỢT ĐUỔI PLAYER ---
        else if (distanceToPlayer <= agroRange)
        {
            FacePlayer();
            isWaiting = false;
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsATK", false);

            float direction = player.position.x > transform.position.x ? 1f : -1f;
            transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

            if (spriteRenderer != null)
                spriteRenderer.flipX = (direction < 0);
        }
        // --- 3. TRẠNG THÁI TUẦN TRA GIỮA A VÀ B ---
        else
        {
            PatrolLogic();
        }
    }

    void PatrolLogic()
    {
        if (leftLimit == null || rightLimit == null)
        {
            animator.SetBool("IsWalking", false);
            return;
        }

        if (isWaiting)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsATK", false);

            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeAtPoint)
            {
                isWaiting = false;
            }
            return;
        }

        animator.SetBool("IsWalking", true);
        animator.SetBool("IsATK", false);

        if (movingRight)
        {
            // Di chuyển sang phải bằng Kinematic (Translate)
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
            // Di chuyển sang trái
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
        if (player == null) return;
        float directionX = player.position.x - transform.position.x;

        if (spriteRenderer != null)
        {
            if (directionX > 0) spriteRenderer.flipX = false;
            else if (directionX < 0) spriteRenderer.flipX = true;
        }
    }

    void TriggerAttackEvent()
    {
        if (player != null)
        {
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("<color=yellow>Golem tấn công trúng Player gây " + attackDamage + " sát thương!</color>");
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        animator.SetTrigger("IsHurt");

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("IsDie");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f);
    }
}