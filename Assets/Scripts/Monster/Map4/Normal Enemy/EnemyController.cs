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

    [Header("Patrol Boundaries")]
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
    private Rigidbody2D rb;
    private bool isDead = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip takeDamageSound;
    public AudioClip dieSound;

    [Header("Drop Item")]
    public GameObject healSmallPrefab;
    [Range(0f, 1f)] public float dropChance = 0.3f;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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

        if (distanceToPlayer <= attackRange)
        {
            FacePlayer();
            animator.SetBool("IsWalking", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                animator.SetTrigger("IsATK");
                lastAttackTime = Time.time;

                if (attackSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(attackSound);
                }
            }
        }
        else if (distanceToPlayer <= agroRange)
        {
            FacePlayer();
            isWaiting = false;
            animator.SetBool("IsWalking", true);

            float direction = player.position.x > transform.position.x ? 1f : -1f;
            transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

            if (spriteRenderer != null)
                spriteRenderer.flipX = (direction < 0);
        }
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

            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeAtPoint)
            {
                isWaiting = false;
            }
            return;
        }

        animator.SetBool("IsWalking", true);

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
        if (player == null) return;
        float directionX = player.position.x - transform.position.x;

        if (spriteRenderer != null)
        {
            if (directionX > 0) spriteRenderer.flipX = false;
            else if (directionX < 0) spriteRenderer.flipX = true;
        }
    }

    public void AnimationAttackHit()
    {
        if (isDead) return;

        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        animator.SetTrigger("IsHurt");

        if (takeDamageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(takeDamageSound);
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.ResetTrigger("IsATK");
            animator.ResetTrigger("IsHurt");
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("IsDie");
        }

        if (dieSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSound);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        TryDropHealSmall();

        Destroy(gameObject, 2f);
    }

    private void TryDropHealSmall()
    {
        if (healSmallPrefab == null) return;

        if (Random.value <= dropChance)
        {
            Vector3 dropPos = transform.position;
            if (spriteRenderer != null)
            {
                dropPos.y = spriteRenderer.bounds.min.y + 0.6f;
            }

            GameObject dropped = Instantiate(healSmallPrefab, dropPos, Quaternion.identity);
            SpriteRenderer itemSr = dropped.GetComponent<SpriteRenderer>();
            if (itemSr != null && spriteRenderer != null)
            {
                itemSr.sortingLayerID = spriteRenderer.sortingLayerID;
                itemSr.sortingOrder = spriteRenderer.sortingOrder + 1;
            }
        }
    }
}
